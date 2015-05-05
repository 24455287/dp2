// #define DEBUG_LOCK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml;
using System.Data;

using System.Data.SqlClient;
using System.Data.SQLite;

using MySql.Data;
using MySql.Data.MySqlClient;

using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

using System.IO;
using System.Diagnostics;
using System.Web;
using System.Runtime.Serialization;

using DigitalPlatform;
using DigitalPlatform.ResultSet;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Xml;
using DigitalPlatform.Range;

namespace DigitalPlatform.rms
{
    // ���ݿ⼯��
    public class DatabaseCollection : List<Database>
    {
        public DelayTableCollection DelayTables = null;

        Hashtable m_logicNameTable = new Hashtable();

        // SQL����������
        public string SqlServerTypeString = "";
        // SQL��������
        public string SqlServerName = "";

        // SQL����������
        public SqlServerType SqlServerType
        {
            get
            {
                if (this.SqlServerTypeString == "SQLite")
                    return SqlServerType.SQLite;
                if (this.SqlServerTypeString == "MS SQL Server")
                    return SqlServerType.MsSqlServer;
                if (this.SqlServerTypeString == "MySQL Server")
                    return SqlServerType.MySql;
                if (this.SqlServerTypeString == "Oracle")
                    return SqlServerType.Oracle;
                if (string.Compare(this.SqlServerName, "~sqlite", true) == 0)
                    return SqlServerType.SQLite;

                return SqlServerType.MsSqlServer;
            }
        }

        bool m_bAllTailNoVerified = false;  // �Ƿ�ȫ�����ݿ��β�Ŷ���У�����

        public bool AllTailNoVerified
        {
            get
            {
                return this.m_bAllTailNoVerified;
            }
        }

        public KernelApplication KernelApplication = null;


        public void ActivateCommit()
        {
            if (this.KernelApplication != null)
                this.KernelApplication.ActivateCommit();
        }

        // �ʻ�����ָ��,�����޸��ʻ����¼ʱ��ˢ�µ�ǰ�ʻ�
        public UserCollection UserColl 
        {
            get
            {
                // ע����Ҫ��KernelApplication��ʼ����Users������Ա����ʹ��
                return this.KernelApplication.Users;
            }
        }

        public string DataDir
        {
            get
            {
                return this.KernelApplication.DataDir;
            }
        }

        public bool Changed = false;	//�����Ƿ����ı�

        // public XmlNode NodeDbs = null;  //<dbs>�ڵ�
        public XmlNode NodeDbs
        {
            get
            {
                if (this.m_dom == null)
                    return null;

                return this.m_dom.SelectSingleNode(@"/root/dbs");
                /*
                if (this.NodeDbs == null)
                {
                    strError = "databases.xml�����ļ��в�����<dbs>�ڵ㣬�ļ����Ϸ����������ٴ��ڵ�һ���û��⡣";
                    return -1;
                }
                 * */
            }
        }

        // public string SessionDir = "";  // session��ʱ����Ŀ¼
        public string InstanceName = ""; // ������ʵ����

        public string BinDir = "";//BinĿ¼��Ϊ�ű�����dll���� 2006/3/21��

        public string ObjectDir = "";   // �����ļ�Ŀ¼��2012/1/21

        public string TempDir = "";     // ��ʱ�ļ�Ŀ¼��2013/2/19

        // �����������
        private MyReaderWriterLock m_container_lock = new MyReaderWriterLock();
        private int m_nContainerLockTimeOut = 1000 * 60;	//1����


        // Ϊ�����ļ�ר�õ���
        private MyReaderWriterLock m_cfgfile_lock = new MyReaderWriterLock();
        private int m_nCfgFileLockTimeOut = 1000 * 60;	//1����

        private string m_strDbsCfgFilePath = "";	// �������ļ���
        private XmlDocument m_dom = null;	// �������ļ�dom

        public XmlDocument CfgDom
        {
            get
            {
                return this.m_dom;
            }
        }

        // parameter:
        //		strDataDir	dataĿ¼
        //		strError	out���������س�����Ϣ
        // return:
        //		-1	����
        //		0	�ɹ�
        // ��: ��ȫ��
        // ����д��
        public int Initial(
            KernelApplication app,
            // string strDataDir,
            string strBinDir,
            out string strError)
        {
            strError = "";

            this.m_logicNameTable.Clear();

            this.KernelApplication = app;

            if (String.IsNullOrEmpty(strBinDir) == true)
            {
                strError = "DatabaeCollection::Initial()��strBinDir����ֵ����Ϊnull����ַ�����";
                return -1;
            }
            this.BinDir = strBinDir;

            if (String.IsNullOrEmpty(this.DataDir) == true)
            {
                strError = "DatabaeCollection::Initial()��this.DataDir����ֵ����Ϊnull����ַ�����";
                return -1;
            }

            Debug.Assert(string.IsNullOrEmpty(this.DataDir) == false, "");
            // this.SessionDir = PathUtil.MergePath(this.DataDir, "session");


            // �����ļ�Ŀ¼
            string strObjectDir = this.DataDir + "\\object";
            try
            {
                PathUtil.CreateDirIfNeed(strObjectDir);
            }
            catch (Exception ex)
            {
                strError = "�������ݶ���Ŀ¼����: " + ex.Message;
                return -1;
            }
            this.ObjectDir = strObjectDir;

            // ��ʱ�ļ�Ŀ¼
            string strTempDir = Path.Combine(this.DataDir, "temp");

#if NO
            // ��ɾ�����Ŀ¼��Ȼ�󴴽�������������ǰ��������ʱ�ļ�
            try
            {
                PathUtil.DeleteDirectory(strTempDir);   // 2013/12/5
            }
            catch
            {
            }
#endif

            try
            {
                PathUtil.CreateDirIfNeed(strTempDir);
            }
            catch (Exception ex)
            {
                strError = "����(DatabaseCollection)��ʱ�ļ�Ŀ¼����: " + ex.Message;
                return -1;
            }

            if (PathUtil.ClearDir(strTempDir) == false)
                this.KernelApplication.WriteErrorLog("�����ʱ�ļ�Ŀ¼ " + strTempDir + " ʱ����");

            this.TempDir = strTempDir;

            //**********�Կ⼯�ϼ�д��****************
            m_container_lock.AcquireWriterLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
			this.WriteDebugInfo("Initial()���Կ⼯�ϼ�д����");
#endif
            try
            {


                // databases.xml�����ļ�
                this.m_strDbsCfgFilePath = this.DataDir + "\\databases.xml";

                this.m_dom = new XmlDocument();
                //this.m_dom.PreserveWhitespace = true; //����հ�
                try
                {
                    this.m_dom.Load(this.m_strDbsCfgFilePath);
                }
                catch (Exception ex)
                {
                    strError = "����" + this.m_strDbsCfgFilePath + "��domʱ���� " + ex.Message;
                    return -1;
                }

                // 2011/1/7
                bool bValue = false;
                DomUtil.GetBooleanParam(this.m_dom.DocumentElement,
                    "debugMode",
                    false,
                    out bValue,
                    out strError);
                this.KernelApplication.DebugMode = bValue;

                // ����
                {
                    XmlNode temp = m_dom.SelectSingleNode(@"/root/dbs");
                    if (temp == null)
                    {
                        strError = "databases.xml�����ļ��в�����<dbs>�ڵ㣬�ļ����Ϸ����������ٴ��ڵ�һ���û��⡣";
                        return -1;
                    }
                }
                /*
                this.NodeDbs = m_dom.SelectSingleNode(@"/root/dbs");
                if (this.NodeDbs == null)
                {
                    strError = "databases.xml�����ļ��в�����<dbs>�ڵ㣬�ļ����Ϸ����������ٴ��ڵ�һ���û��⡣";
                    return -1;
                }*/

                this.InstanceName = DomUtil.GetAttr(this.NodeDbs, "instancename");

                // 2012/2/18
                XmlNode nodeDataSource = this.m_dom.DocumentElement.SelectSingleNode("datasource");
                if (nodeDataSource == null)
                {
                    strError = "�����������ļ����Ϸ���δ�ڸ�Ԫ���¶���<datasource>Ԫ��";
                    return -1;
                }
                this.SqlServerTypeString = DomUtil.GetAttr(nodeDataSource, "servertype").Trim();
                if (string.IsNullOrEmpty(this.SqlServerTypeString) == false)
                {
                    if (this.SqlServerTypeString != "MS SQL Server"
                        && this.SqlServerTypeString != "MySQL Server"
                        && this.SqlServerTypeString != "Oracle"
                        && this.SqlServerTypeString != "SQLite")
                    {
                        strError = "�����������ļ����Ϸ�����Ԫ���¼���<datasource>Ԫ�ص�'servertype'����ֵ '" + this.SqlServerTypeString + "' ���Ϸ���Ӧ��Ϊ MS SQL Server/MySQL Server/Oracle SQL Server/SQLite ֮һ(ȱʡΪ 'MS SQL Server')��";
                        return -1;
                    }
                }

                this.SqlServerName = DomUtil.GetAttr(nodeDataSource, "servername").Trim();
                if (string.IsNullOrEmpty(this.SqlServerName) == true)
                {
                    strError = "�����������ļ����Ϸ���δ����Ԫ���¼���<datasource>����'servername'���ԣ���'servername'����ֵΪ�ա�";
                    return -1;
                }

                // �����
                this.Clear();

                // ����<database>�ڵ㴴��Database����
                int nRet = 0;
                XmlNodeList listDb = this.NodeDbs.SelectNodes("database");
                foreach (XmlNode nodeDb in listDb)
                {
                    // return:
                    //      -1  ����
                    //      0   �ɹ�
                    // �ߣ�����ȫ
                    nRet = this.AddDatabase(nodeDb,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

                this.KernelApplication.WriteErrorLog("��ʼ�����ݿ��ڴ������ϡ�");

                /*
                // ����������ݿ��¼β��
                // return:
                //      -1  ����
                //      0   �ɹ�
                // �ߣ�����ȫ
                nRet = this.CheckDbsTailNo(out strError);
                if (nRet == -1)
                    return -1;
                 * */

                return 0;
            }
            finally
            {
                //***********�Կ⼯�Ͻ�д��****************
                m_container_lock.ReleaseWriterLock();
#if DEBUG_LOCK
				this.WriteDebugInfo("Initial()���Կ⼯�Ͻ�д����");
#endif
            }
        }

        public string GetTempFileName()
        {
            Debug.Assert(string.IsNullOrEmpty(this.TempDir) == false, "");
            while (true)
            {
                string strFilename = PathUtil.MergePath(this.TempDir, Guid.NewGuid().ToString());
                if (File.Exists(strFilename) == false)
                {
                    using (FileStream s = File.Create(strFilename))
                    {
                    }
                    return strFilename;
                }
            }
        }

        // ����node�ڵ㴴��Database���ݿ���󣬼ӵ�������
        // parameters:
        //      node    <database>�ڵ�
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        // �ߣ�����ȫ
        public int AddDatabase(XmlNode node,
            out string strError)
        {
            Debug.Assert(node != null, "AddDatabase()���ô���node����ֵΪ��Ϊnull��");
            Debug.Assert(String.Compare(node.Name, "database", true) == 0, "AddDatabase()���ô���node����ֵ����Ϊ<database>�ڵ㡣");

            strError = "";

            string strType = DomUtil.GetAttr(node, "type").Trim();

            Database db = null;

            // file���ʹ���ΪFileDatabase������������ΪSqlDatabase����
            if (StringUtil.IsInList("file", strType, true) == true)
                db = new FileDatabase(this);
            else
                db = new SqlDatabase(this);

            // return:
            //		-1  ����
            //		0   �ɹ�
            int nRet = db.Initial(node,
                out strError);
            if (nRet == -1)
                return -1;

            this.Add(db);
            this.m_logicNameTable.Clear();
            return 0;
        }

        // ��������
        ~DatabaseCollection()
        {
            /*
            this.Close();
            this.WriteErrorLog("����DatabaseCollection������ɡ�");
             */
        }

        public void Commit()
        {
            // 2012/2/21
            foreach (Database db in this)
            {
                db.Commit();
            }
        }

        public void Close()
        {
            if (this.DelayTables != null && this.DelayTables.Count != 0)
            {
                try
                {
                    string strError = "";
                    List<RecordBody> results = null;
                    int nRet = this.API_WriteRecords(
                        null,
                        null,
                        "flushkeys",
                        out results,
                        out strError);
                    if (nRet == -1)
                    {
                        this.KernelApplication.WriteErrorLog("DatabaseCollection.Close() flushkeys ����" + strError);
                    }
                }
                catch (Exception ex)
                {
                    this.KernelApplication.WriteErrorLog("DatabaseCollection.Close() flushkeys �׳��쳣��" + ex.Message);
                }
            }

            // 2012/2/21
            foreach (Database db in this)
            {
                db.Close();
            }
            // �����ڴ�����ļ�
            this.SaveXmlSafety(true);
        }

        // �Ѵ�����Ϣд����־�ļ���
        public void WriteDebugInfo(string strText)
        {
            string strTime = DateTime.Now.ToString();

            StreamUtil.WriteText(this.DataDir + "\\debug.txt",
                 strTime + " " + strText + "\r\n");
        }

        // ����������ݿ��¼β��
        // return:
        //      -1  ����
        //      0   �ɹ�
        // �ߣ���ȫ
        // �쳣�����ܻ��׳��쳣
        public int CheckDbsTailNo(out string strError)
        {
            strError = "";

            if (this.m_bAllTailNoVerified == true)
                return 0;

            //**********�Կ⼯�ϼ�д��****************
            m_container_lock.AcquireWriterLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
			this.WriteDebugInfo("Initial()���Կ⼯�ϼ�д����");
#endif
            try
            {

                this.KernelApplication.WriteErrorLog("��ʼУ�����ݿ�β�š�");

                int nRet = 0;
                try
                {
                    int nFailCount = 0;
                    for (int i = 0; i < this.Count; i++)
                    {
                        Database db = (Database)this[i];
                        string strTempError = "";
                        nRet = db.CheckTailNo(out strTempError);
                        if (nRet == -1)
                        {
                            nFailCount++;
                            strError += strTempError + "; ";
                            // ����У���������ݿ�

                            // return -1;
                        }
                    }

                    if (nFailCount == 0)
                        this.m_bAllTailNoVerified = true;

                    // �����ڴ����
                    this.SaveXml();

                    if (nFailCount > 0)
                        return -1;
                }
                catch (Exception ex)
                {
                    strError = "CheckDbsTailNo()�׳��쳣��ԭ��" + ex.Message;
                    return -1;
                }

                return 0;
            }
            finally
            {
                //***********�Կ⼯�Ͻ�д��****************
                m_container_lock.ReleaseWriterLock();
#if DEBUG_LOCK
				this.WriteDebugInfo("Initial()���Կ⼯�Ͻ�д����");
#endif
            }
        }


        // ���ڴ�dom���浽databases.xml�����ļ�
        // һ���ֽڵ㲻�䣬һ���ֽڵ㱻����
        // ��: ����ȫ
        // �쳣�����ܻ��׳��쳣����ʱδ����ApplicationException��IOException
        public void SaveXml()
        {
            if (this.Changed == false)
                return;

            this.m_cfgfile_lock.AcquireWriterLock(this.m_nCfgFileLockTimeOut);
            try
            {
                // Ԥ�ȱ���һ�������ļ�
                string strBackupFilename = this.m_strDbsCfgFilePath + ".bak";

                if (FileUtil.IsFileExsitAndNotNull(this.m_strDbsCfgFilePath) == true)
                {
                    this.KernelApplication.WriteErrorLog("���� " + this.m_strDbsCfgFilePath + " �� " + strBackupFilename);
                    File.Copy(this.m_strDbsCfgFilePath, strBackupFilename, true);
                }

                XmlTextWriter w = new XmlTextWriter(this.m_strDbsCfgFilePath,
                    Encoding.UTF8);
                w.Formatting = Formatting.Indented;
                w.Indentation = 4;
                m_dom.WriteTo(w);
                w.Close();

                this.Changed = false;

                this.KernelApplication.WriteErrorLog("��ɱ����ڴ�dom�� '" + this.m_strDbsCfgFilePath + "' �ļ���");
            }
            finally
            {
                this.m_cfgfile_lock.ReleaseWriterLock();
            }
        }

        // SaveXml()�İ�ȫ�汾
        public void SaveXmlSafety(bool bNeedLock)
        {
            if (this.Changed == false)
                return;

            if (bNeedLock == true)
            {
                //******************�Կ⼯�ϼӶ���******
                m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
                this.WriteDebugInfo("SaveXmlSafety()���Կ⼯�ϼӶ�����");
#endif
            }

            try
            {
                this.SaveXml();
            }
            finally
            {
                if (bNeedLock == true)
                {

                    m_container_lock.ReleaseReaderLock();
                    //*************�Կ⼯�Ͻ����***********
#if DEBUG_LOCK
                    this.WriteDebugInfo("SaveXmlSafety()���Կ⼯�Ͻ������");
#endif
                }
            }
        }

        // ���һ���û�ӵ�е�(dbo)ȫ�����ݿ���
        public int GetOwnerDbNames(
            bool bNeedLock,
            string strUserName,
            out List<string> aOwnerDbName,
            out string strError)
        {
            strError = "";

            aOwnerDbName = new List<string>();

            if (bNeedLock == true)
            {
                //******************�Կ⼯�ϼӶ���******
                this.m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
                this.WriteDebugInfo("GetOwnerDbNames()���Կ⼯�ϼӶ�����");
#endif
            }

            try
            {

                foreach (Database db in this)
                {
                    if (db.DboSafety == strUserName)
                    {
                        aOwnerDbName.Add(db.GetCaptionSafety(null));
                    }
                }

                return 0;
            }
            finally
            {
                if (bNeedLock == true)
                {
                    this.m_container_lock.ReleaseReaderLock();
                    //*****************�Կ⼯�Ͻ����*************
#if DEBUG_LOCK
                    this.WriteDebugInfo("GetOwnerDbNames()���Կ⼯�Ͻ������");
#endif
                }
            }

        }

        // �½����ݿ�
        // parameter:
        //		user	            �ʻ�����
        //		logicNames	        LogicNameItem����
        //		strType	            ���ݿ�����,�Զ��ŷָ���������file,accout
        //		strSqlDbName    	ָ����Sql���ݿ�����,����Ϊnull��ϵͳ�Զ�����һ��,��������ݿ�Ϊ��Ϊ�ļ������ݿ⣬����������ԴĿ¼������
        //		strKeysDefault  	keys������Ϣ
        //		strBrowseDefault	browse������Ϣ
        // return:
        //      -3	���½����У������Ѿ�����ͬ�����ݿ�, ���β��ܴ���
        //      -2	û���㹻��Ȩ��
        //      -1	һ���Դ�����������������Ϸ���
        //      0	�����ɹ�
        // ������д��
        public int API_CreateDb(User user,
            LogicNameItem[] logicNames,
            string strType,
            string strSqlDbName,
            string strKeysDefault,
            string strBrowseDefault,
            out string strError)
        {
            strError = "";

            /*
            if (strKeysDefault == null)
                strKeysDefault = "";
            if (strBrowseDefault == null)
                strBrowseDefault = "";
             * */

            if (String.IsNullOrEmpty(strKeysDefault) == false)
            {
                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strKeysDefault);
                }
                catch (Exception ex)
                {
                    strError = "����keys�����ļ����ݵ�dom����(2)��ԭ��:" + ex.Message;
                    return -1;
                }
            }
            if (String.IsNullOrEmpty(strBrowseDefault) == false)
            {
                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strBrowseDefault);
                }
                catch (Exception ex)
                {
                    strError = "����browse�����ļ����ݵ�dom����ԭ��:" + ex.Message;
                    return -1;
                }
            }

            string strEnLoginName = "";

            // ����һ���߼�����Ҳû�У�������
            string strLogicNames = "";
            for (int i = 0; i < logicNames.Length; i++)
            {
                string strLang = logicNames[i].Lang;
                string strLogicName = logicNames[i].Value;

                // TODO: ����ж������⣬�����о�һ��
                if (strLang.Length != 2
                    && strLang.Length != 5)
                {
                    strError = "���԰汾�ַ�������ֻ����2λ����5λ,'" + strLang + "'���԰汾���Ϸ�";
                    return -1;
                }

                if (this.IsExistLogicName(strLogicName, null) == true)
                {
                    strError = "���ݿ����Ѵ��� '" + strLogicName + "' �߼�����";
                    return -3;  // �Ѵ�����ͬ���ݿ���
                }

                strLogicNames += "<caption lang='" + strLang + "'>" + strLogicName + "</caption>";
                if (String.Compare(logicNames[i].Lang.Substring(0, 2), "en", true) == 0)
                    strEnLoginName = strLogicName;
            }

            strLogicNames = "<logicname>" + strLogicNames + "</logicname>";

            // ��鵱ǰ�ʻ��Ƿ��д������ݿ��Ȩ��
            string strTempDbName = "test";
            if (logicNames.Length > 0)
                strTempDbName = logicNames[0].Value;
            string strExistRights = "";
            bool bHasRight = user.HasRights(strTempDbName,
                ResType.Database,
                "create",
                out strExistRights);
            if (bHasRight == false)
            {
                strError = "�����ʻ���Ϊ'" + user.Name + "'�������ݿ�û��'����(create)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                return -2;  // Ȩ�޲���
            }

            //**********�Կ⼯�ϼ�д��****************
            m_container_lock.AcquireWriterLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
			this.WriteDebugInfo("CreateDb()���Կ⼯�ϼ�д����");
#endif
            try
            {
                if (strType == null)
                    strType = "";

                // �õ����ID
                string strDbID = Convert.ToString(this.GetNewDbID());

                string strPureCfgsDir = "";
                string strTempSqlDbName = "";
                if (strEnLoginName != "")
                {
                    // TODO: ����Ҫע���Ƿ���SQL���ݿ����в�������ַ���

                    if (this.SqlServerType == rms.SqlServerType.Oracle)
                    {
                        if (strEnLoginName.Length > 3)
                            strEnLoginName = strEnLoginName.Substring(0, 3);
                        strTempSqlDbName = strEnLoginName;
                    }
                    else
                        strTempSqlDbName = strEnLoginName + "_db";

                    strPureCfgsDir = strEnLoginName + "_cfgs";
                }
                else
                {
                    if (this.SqlServerType == rms.SqlServerType.Oracle)
                        strTempSqlDbName = "db_" + strDbID;
                    else
                        strTempSqlDbName = "dprms_" + strDbID + "_db";

                    strPureCfgsDir = "dprms_" + strDbID + "_cfgs";
                }

                if (String.IsNullOrEmpty(strSqlDbName) == true)
                    strSqlDbName = strTempSqlDbName;
                else
                {
                    if (this.SqlServerType == rms.SqlServerType.Oracle
                        && strSqlDbName.Length > 3)
                    {
                        strError = "��ָ���� SQL���ݿ��� '"+strSqlDbName+"' ��Ӧ����3�ַ�";
                        return -1;
                    }
                }

                if (StringUtil.IsInList("file", strType, true) == false)
                {
                    // TODO: ������������Ӽ��SQL Sever���������ݿ����Ĺ���
                    strSqlDbName = this.GetFinalSqlDbName(strSqlDbName);

                    if (this.SqlServerType != rms.SqlServerType.Oracle)
                    {
                        // 2007/7/20
                        if (this.InstanceName != "")
                            strSqlDbName = this.InstanceName + "_" + strSqlDbName;
                    }

                    // TODO: ��һ���ƺ��Ƕ���ģ���ΪGetFinalSqlDbName()���Ѿ��жϹ���
                    if (this.IsExistSqlName(strSqlDbName) == true)
                    {
                        strError = "���������Ѵ���SQL���� '" + strSqlDbName + "'���������ݿ�ʧ�ܡ������һ���µ�SQL�������´�������ָ��һ���յ�SQL������������Զ�����SQL������";
                        return -1;
                    }
                }

                string strDataSource = "";
                if (StringUtil.IsInList("file", strType, true) == true)
                {
                    strDataSource = strSqlDbName;

                    strDataSource = this.GetFinalDataSource(strDataSource);

                    if (this.IsExistFileDbSource(strDataSource) == true)
                    {
                        strError = "�����ܵ���������ݿ����Ѵ��� '" + strDataSource + "' �ļ�����Ŀ¼";
                        return -1;
                    }

                    string strDataDir = this.DataDir + "\\" + strDataSource;
                    if (Directory.Exists(strDataDir) == true)
                    {
                        strError = "�����ܵ���������ز�����������Ŀ¼��";
                        return -1;
                    }

                    Directory.CreateDirectory(strDataDir);
                }

                strPureCfgsDir = this.GetFinalCfgsDir(strPureCfgsDir);
                // �������ļ�Ŀ¼�Զ�������
                string strCfgsDir = this.DataDir + "\\" + strPureCfgsDir + "\\cfgs";
                if (Directory.Exists(strCfgsDir) == true)
                {
                    strError = "�������Ѵ���'" + strPureCfgsDir + "'�����ļ�Ŀ¼����ָ��������Ӣ���߼�������";
                    return -1;
                }

                Directory.CreateDirectory(strCfgsDir);

                string strPureKeysLocalName = "keys.xml";
                string strPureBrowseLocalName = "browse.xml";

                int nRet = 0;

                // дkeys�����ļ�
                nRet = DatabaseUtil.CreateXmlFile(strCfgsDir + "\\" + strPureKeysLocalName,
                    strKeysDefault,
                    out strError);
                if (nRet == -1)
                    return -1;

                // дbrowse�����ļ�
                nRet = DatabaseUtil.CreateXmlFile(strCfgsDir + "\\" + strPureBrowseLocalName,
                    strBrowseDefault,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (StringUtil.IsInList("file", strType) == true)
                    strSqlDbName = "";

                // TODO: ���﷢��xmlƬ�Ͽ��ܻ���С���⣬Ӧ����XmlTextWriter������?
                string strDbXml = "<database type='" + strType + "' id='" + strDbID + "' localdir='" + strPureCfgsDir
                    + "' dbo='"+user.Name+"'>"  // dbo����Ϊ2006/7/4����
                    + "<property>"
                    + strLogicNames
                    + "<datasource>" + strDataSource + "</datasource>"
                    + "<seed>0</seed>"
                    + "<sqlserverdb name='" + strSqlDbName + "'/>"
                    + "</property>"
                    + "<dir name='cfgs' localdir='cfgs'>"
                    + "<file name='keys' localname='" + strPureKeysLocalName + "'/>"
                    + "<file name='browse' localname='" + strPureBrowseLocalName + "'/>"
                    + "</dir>"
                    + "</database>";

                this.NodeDbs.InnerXml = this.NodeDbs.InnerXml + strDbXml;

                XmlNodeList nodeListDb = this.NodeDbs.SelectNodes("database");
                if (nodeListDb.Count == 0)
                {
                    strError = "���½����ݿ⣬������һ�����ݿⶼ�����ڡ�";
                    return -1;
                }

                // ���һ����Ϊ�½������ݿ⣬�ӵ�������
                XmlNode nodeDb = nodeListDb[nodeListDb.Count - 1];
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = this.AddDatabase(nodeDb,
                    out strError);
                if (nRet == -1)
                    return -1;

                // ��ʱ����dbo����
                user.AddOwnerDbName(strTempDbName);

                // ��ʱ���浽database.xml
                this.Changed = true;
                this.SaveXml();
            }
            finally
            {
                m_container_lock.ReleaseWriterLock();
                //***********�Կ⼯�Ͻ�д��****************
#if DEBUG_LOCK
				this.WriteDebugInfo("CreateDb()���Կ⼯�Ͻ�д����");
#endif
            }
            return 0;
        }


        // �淶sql���ݿ����ƣ�ֻ�������֣���Сд���ߣ��»��ߡ�
        // ΪGetFinalSqlDbName()����ڲ�����
        private void CanonicalizeSqlDbName(ref string strSqlDbName)
        {
            if (strSqlDbName == null)
                strSqlDbName = "";

            for (int i = 0; i < strSqlDbName.Length; i++)
            {
                char myChar = strSqlDbName[i];
                if (myChar == '_')
                    continue;

                if (myChar <= '9' && myChar >= '0')
                    continue;

                if (myChar <= 'z' && myChar >= 'a')
                    continue;

                if (myChar <= 'Z' && myChar >= 'A')
                    continue;

                strSqlDbName = strSqlDbName.Remove(i, 1);
                i--;
            }
        }

        // �õ����յ�sql���ݿ�����
        private string GetFinalSqlDbName(string strSqlDbName)
        {
            if (strSqlDbName == null)
                strSqlDbName = "";

            string strRealSqlDbName = strSqlDbName;

            // �淶��Sql���ݿ�����
            this.CanonicalizeSqlDbName(ref strRealSqlDbName);


            for (int i = 0; ; i++)
            {
                if (strRealSqlDbName == "")
                {
                    strRealSqlDbName = "dprms_db_" + Convert.ToString(i);
                }

                // �����Ƿ�͵�ǰϵͳ�е����е�sql��������
                // ��������û�п�SQL Server�е�ʵ�����
                if (this.IsExistSqlName(strRealSqlDbName) == false)
                    return strRealSqlDbName;
                else
                    strRealSqlDbName = strRealSqlDbName + Convert.ToString(i);
            }
        }

        // �淶��DataSourceĿ¼��
        // ΪGetFinalDataSource()����ڲ�����
        private void CanonicalizeDir(ref string strDataSource)
        {
            if (strDataSource == null)
                strDataSource = "";

            for (int i = 0; i < strDataSource.Length; i++)
            {
                char myChar = strDataSource[i];

                if (myChar == '\\'
                    || myChar == '/'
                    || myChar == ':'
                    || myChar == '*'
                    || myChar == '?'
                    || myChar == '<'
                    || myChar == '>'
                    || myChar == '|')
                {
                    strDataSource = strDataSource.Remove(i, 1);
                    i--;
                }
            }
        }

        // �õ����յ��ļ���ʹ�õ�����Ŀ¼
        private string GetFinalDataSource(string strDataSource)
        {
            if (strDataSource == null)
                strDataSource = "";

            string strRealDataSource = strDataSource;

            this.CanonicalizeDir(ref strRealDataSource);

            for (int i = 0; ; i++)
            {
                if (strRealDataSource == "")
                {
                    strRealDataSource = "dprms_db_" + Convert.ToString(i);
                }

                if (this.IsExistFileDbSource(strRealDataSource) == false
                    && Directory.Exists(this.DataDir + "\\" + strRealDataSource) == false)
                {
                    return strRealDataSource;
                }
                else
                {
                    strRealDataSource = strRealDataSource + Convert.ToString(i);
                }
            }
        }

        // �õ����յ����ݿ�ʹ�õ�����Ŀ¼
        private string GetFinalCfgsDir(string strCfgsDir)
        {
            if (strCfgsDir == null)
                strCfgsDir = "";

            string strRealCfgsDir = strCfgsDir;

            this.CanonicalizeDir(ref strRealCfgsDir);

            for (int i = 0; ; i++)
            {
                if (strRealCfgsDir == "")
                {
                    strRealCfgsDir = "dprms_" + Convert.ToString(i) + "_cfgs";
                }

                if (this.IsExistCfgsDir(strRealCfgsDir, null) == false
                    && Directory.Exists(this.DataDir + "\\" + strRealCfgsDir) == false)
                {
                    return strRealCfgsDir;
                }
                else
                {
                    strRealCfgsDir = strRealCfgsDir + Convert.ToString(i);
                }
            }
        }

        // ����������Ƿ��Ѵ�����ͬ��sql������
        internal bool IsExistSqlName(string strSqlName)
        {
            for (int i = 0; i < this.Count; i++)
            {
                Database tempDb = (Database)this[i];
                if (!(tempDb is SqlDatabase))
                    continue;

                SqlDatabase sqlDb = (SqlDatabase)tempDb;
                string strDbSqlName = sqlDb.GetSourceName();// �õ�Sql���ݿ�����
                if (String.Compare(strSqlName, strDbSqlName, true) == 0)
                    return true;
            }
            return false;
        }

        // �µ�һ�����õ����ݿ�ID
        // return:
        //		��ID
        // ˵��: �ú����ڽ��ַ���IDת������ֵIDʱ�����ת�����ɹ������׳��쳣
        private int GetNewDbID()
        {
            int nId = 0;
            // �������е����ݿ�id��Ȼ��õ�һ�����ֵ
            for (int i = 0; i < this.Count; i++)
            {
                Database db = (Database)this[i];
                int nDbId = Convert.ToInt32(db.PureID);
                if (nId < nDbId)
                    nId = nDbId;
            }
            nId = nId + 1;
            return nId;
        }

        // ��������Ŀ��������԰汾���Ƿ������ͬ���߼���
        internal bool IsExistLogicName(string strLogicName,
            Database exceptDb)
        {
            for (int i = 0; i < this.Count; i++)
            {
                Database db = (Database)this[i];
                if (exceptDb != null)
                {
                    if (db == exceptDb)
                        continue;
                }
                string strDbAllLogicName = db.GetAllCaption();
                if (StringUtil.IsInList(strLogicName, strDbAllLogicName, true) == true)
                    return true;
            }
            return false;
        }

        // �������ݿ��Ӧ������Ŀ¼�Ƿ��ظ�
        // parameters:
        //      strCfgsDir  Ŀ¼�������Ŀ¼
        //      exceptDb    ���ο��Ƚϵ����ݿ����
        // return:
        //      true    ���ظ�
        //      false   ���ظ�
        internal bool IsExistCfgsDir(string strCfgsDir,
            Database exceptDb)
        {
            for (int i = 0; i < this.Count; i++)
            {
                Database db = (Database)this[i];
                if (exceptDb != null)
                {
                    if (db == exceptDb)
                        continue;
                }
                string strDbCfgsDir = DatabaseUtil.GetLocalDir(this.NodeDbs,
                    db.m_selfNode);

                if (String.Compare(strCfgsDir, strDbCfgsDir, true) == 0)
                    return true;
            }
            return false;
        }

        // ����Ƿ��Ѵ�����ͬ��sql������
        internal bool IsExistFileDbSource(string strSource)
        {
            for (int i = 0; i < this.Count; i++)
            {
                Database db = (Database)this[i];
                if (!(db is FileDatabase))
                    continue;
                string strDbSource = ((FileDatabase)db).m_strPureSourceDir;
                if (String.Compare(strSource, strDbSource, true) == 0)
                    return true;
            }
            return false;
        }


        // ɾ�����ݿ�
        // parameters:
        //		strDbName	���ݿ����ƣ������Ǹ������԰汾���߼�����Ҳ������id��
        //		strError	out���������س�����Ϣ
        // return:
        //		-1	����
        //      -4  ���ݿⲻ����  2008/4/27 new add
        //      -5  δ�ҵ����ݿ�
        //		-6	���㹻��Ȩ��
        //		0	�ɹ�
        // ������д��
        public int API_DeleteDb(User user,
            string strDbName,
            out string strError)
        {
            strError = "";

            if (user == null)
            {
                strError = "DeleteDb()���ô���user��������Ϊnull��";
                return -1;
            }
            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "DeleteDb()���ô���strDbName����ֵ����Ϊnull����ַ�����";
                return -1;
            }

            //**********�Կ⼯�ϼ�д��****************
            m_container_lock.AcquireWriterLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
			this.WriteDebugInfo("DeleteDb()���Կ⼯�ϼ�д����");
#endif
            try
            {
                Database db = this.GetDatabase(strDbName);
                if (db == null)
                {
                    strError = "δ�ҵ���Ϊ'" + strDbName + "'�����ݿ�";
                    return -4;
                }

                // ��鵱ǰ�ʻ��Ƿ���дȨ��
                string strExistRights = "";
                bool bHasRight = user.HasRights(db.GetCaption("zh-CN"),
                    ResType.Database,
                    "delete",
                    out strExistRights);
                if (bHasRight == false)
                {
                    strError = "�����ʻ���Ϊ'" + user.Name + "'����'" + strDbName + "'���ݿ�û��'ɾ��(delete)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                    return -6;
                }

                // ��database��Delete()������ɾ���ÿ�ʹ�õ������ļ������������ݿ�
                // return:
                //      -1  ����
                //      0   �ɹ�
                int nRet = db.Delete(out strError);
                if (nRet == -1)
                    return -1;

                //this.m_nodeDbs.RemoveChild(db.m_selfNode);
                List<XmlNode> nodes = DatabaseUtil.GetNodes(this.NodeDbs,
                    strDbName);
                if (nodes.Count != 1)
                {
                    strError = "δ�ҵ���Ϊ'" + db.GetCaption("zh") + "'�����ݿ⡣";
                    return -5;
                }
                this.NodeDbs.RemoveChild(nodes[0]);

                // ɾ���ڴ����
                this.Remove(db);
                this.m_logicNameTable.Clear();


                // ��ʱ��ȥdbo����
                user.RemoveOwerDbName(strDbName);


                // ��ʱ���浽database.xml
                this.Changed = true;
                this.SaveXml();

                return 0;
            }
            finally
            {
                m_container_lock.ReleaseWriterLock();
                //***********�Կ⼯�Ͻ�д��****************
#if DEBUG_LOCK
				this.WriteDebugInfo("DeleteDb()���Կ⼯�Ͻ�д����");
#endif
            }
        }

        // ������ݶ��巽�����Ϣ
        // parameters:
        //      strStyle            �����Щ�������? all��ʾȫ�� �ֱ�ָ������logicnames/type/sqldbname/keystext/browsetext
        // return:
        //      -1  һ���Դ���
        //      -5  δ�ҵ����ݿ����
        //      -6  û���㹻��Ȩ��
        //      0   �ɹ�
        public int API_GetDbInfo(
            bool bNeedLock,
            User user,
            string strDbName,
            string strStyle,
            out LogicNameItem[] logicNames,
            out string strType,
            out string strSqlDbName,
            out string strKeysText,
            out string strBrowseText,
            out string strError)
        {
            strError = "";

            logicNames = null;
            strType = "";
            strSqlDbName = "";
            strKeysText = "";
            strBrowseText = "";

            Debug.Assert(user != null, "GetDbInfo()���ô���user��������Ϊnull��");

            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "GetDbInfo()���ò��Ϸ���strDbName����ֵ����Ϊnull����ַ�����";
                return -1;
            }

            // ��鵱ǰ�ʻ��Ƿ�����ʾȨ��
            string strExistRights = "";
            bool bHasRight = user.HasRights(strDbName,
                ResType.Database,
                "read",
                out strExistRights);

            if (bNeedLock == true)
            {
                //******************�Կ⼯�ϼӶ���******
                this.m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
                this.WriteDebugInfo("GetDbInfo()���Կ⼯�ϼӶ�����");
#endif
            }

            try
            {
                Database db = this.GetDatabase(strDbName);
                if (db == null)
                {
                    strError = "δ�ҵ���Ϊ'" + strDbName + "'�����ݿ⡣";
                    return -5;
                }

                if (bHasRight == false)
                {
                    strError = "�����ʻ���Ϊ'" + user.Name + "'����'" + strDbName + "'���ݿ�û��'��(read)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                    return -6;
                }

                // return:
                //		-1	����
                //		0	����
                return db.GetInfo(
                    strStyle,
                    out logicNames,
                    out strType,
                    out strSqlDbName,
                    out strKeysText,
                    out strBrowseText,
                    out strError);
            }
            finally
            {
                if (bNeedLock == true)
                {
                    this.m_container_lock.ReleaseReaderLock();
                    //*****************�Կ⼯�Ͻ����*************
#if DEBUG_LOCK
                    this.WriteDebugInfo("GetDbInfo()���Կ⼯�Ͻ������");
#endif
                }
            }
        }




        // �������ݿ������Ϣ
        // parameter:
        //		strDbName	        ���ݿ�����
        //		strLang	            ��Ӧ�����԰汾��������԰汾Ϊnull����Ϊ���ַ�����������е����԰汾����
        //		logicNames	        LogicNameItem����
        //		strType	            ���ݿ�����,�Զ��ŷָ���������file,accout��Ŀǰ��Ч����Ϊ�漰�����ļ��⣬����sql�������
        //		strSqlDbName	    ָ������Sql���ݿ�����, Ŀǰ��Ч
        //		strKeysDefault	    keys������Ϣ
        //		strBrowseDefault	browse������Ϣ
        // return:
        //      -1  һ���Դ���
        //      -2  �Ѵ���ͬ�������ݿ�
        //      -5  δ�ҵ����ݿ����
        //      -6  û���㹻��Ȩ��
        //      0   �ɹ�
        // ����������
        public int API_SetDbInfo(User user,
            string strDbName,
            LogicNameItem[] logicNames,
            string strType,
            string strSqlDbName,
            string strKeysText,
            string strBrowseText,
            out string strError)
        {
            strError = "";

            Debug.Assert(user != null, "SetDbInfo()���ô���user��������Ϊnull��");

            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "SetDbInfo()���ô���strDbName����ֵ����Ϊnull����ַ�����";
                return -1;
            }

            this.m_logicNameTable.Clear();

            // Ϊ�������������⣬���鿴Ȩ�޵ĺ�������������
            // ��鵱ǰ�ʻ��Ƿ��и������ݿ�ṹ��Ȩ��
            string strExistRights = "";
            bool bHasRight = user.HasRights(strDbName,
                ResType.Database,
                "overwrite",
                out strExistRights);

            //******************�Կ⼯�ϼӶ���******
            this.m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
			this.WriteDebugInfo("SetDbInfo()���Կ⼯�ϼӶ�����");
#endif
            try
            {
                Database db = this.GetDatabase(strDbName);
                if (db == null)
                {
                    strError = "δ�ҵ���Ϊ'" + strDbName + "'�����ݿ⡣";
                    return -5;
                }

                if (bHasRight == false)
                {
                    strError = "�����ʻ���Ϊ'" + user.Name + "'����'" + strDbName + "'���ݿ�û��'����(overwrite)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                    return -6;
                }

                // return:
                //		-1	����
                //      -2  �Ѵ���ͬ�������ݿ�
                //		0	�ɹ�
                int nRet = db.SetInfo(logicNames,
                    strType,
                    strSqlDbName,
                    strKeysText,
                    strBrowseText,
                    out strError);
                if (nRet <= -1)
                    return nRet;

                // ��ʱ����databases.xml
                this.Changed = true;
                this.SaveXml();

                return 0;
            }
            finally
            {
                this.m_container_lock.ReleaseReaderLock();
                //*****************�Կ⼯�Ͻ����*************
#if DEBUG_LOCK
				this.WriteDebugInfo("SetDbInfo()���Կ⼯�Ͻ������");
#endif
            }

        }


        // ???�Կ⼯�ϼӶ���
        // ��ʼ�����ݿ�
        // parameters:
        //      user    �ʻ�����
        //      strDbName   ���ݿ�����
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      -5  ���ݿⲻ����
        //      -6  Ȩ�޲���
        //      0   �ɹ�
        // �ߣ���ȫ ����û���ϣ�����
        public int API_InitializePhysicalDatabase(User user,
            string strDbName,
            out string strError)
        {
            strError = "";
            Debug.Assert(user != null, "InitializeDb()���ô���user����ֵ����Ϊnull��");

            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "InitializeDb()���ô���strDbName����ֵ����Ϊnull����ַ�����";
                return -1;
            }

            // 1.�õ����ݿ�
            Database db = this.GetDatabaseSafety(strDbName);
            if (db == null)
            {
                strError = "û���ҵ���Ϊ'" + strDbName + "'�����ݿ�";
                return -5;
            }

            string strExistRights = "";
            bool bHasRight = user.HasRights(db.GetCaption("zh-CN"),
                ResType.Database,
                "clear",
                out strExistRights);
            if (bHasRight == false)
            {
                strError = "�����ʻ���Ϊ'" + user.Name + "'����'" + strDbName + "'���ݿ�û��'��ʼ��(clear)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                return -6;
            }

            // 3.��ʼ��
            // return:
            //		-1  ����
            //		0   �ɹ�
            return db.InitialPhysicalDatabase(out strError);
        }

        // ˢ�����ݿⶨ��
        // parameters:
        //      user    �ʻ�����
        //      strAction   ������beginΪ��ʼˢ�¡�endΪ����ˢ��
        //      strDbName   ���ݿ�����
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      -5  ���ݿⲻ����
        //      -6  Ȩ�޲���
        //      0   �ɹ�
        // �ߣ���ȫ ����û���ϣ�����
        public int API_RefreshPhysicalDatabase(
            // SessionInfo sessioninfo,
            User user,
            string strAction,
            string strDbName,
            bool bClearAllKeyTables,
            out string strError)
        {
            strError = "";
            Debug.Assert(user != null, "RefreshDb()���ô���user����ֵ����Ϊnull��");

#if NO
            if (strAction != "begin"
                && strAction != "end"
                && strAction != "beginfastappend"
                && strAction != "endfastappend"
                && strAction != "flushpendingkeys")
            {
                strError = "strAction����ֵ����Ϊ begin/end/beginfastappend/endfastappend/flushpendingkeys ֮һ";
                return -1;
            }
#endif

            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "RefreshDb()���ô���strDbName����ֵ����Ϊnull����ַ�����";
                return -1;
            }

            // 1.�õ����ݿ�
            Database db = this.GetDatabaseSafety(strDbName);
            if (db == null)
            {
                strError = "û���ҵ���Ϊ '" + strDbName + "' �����ݿ�";
                return -5;
            }

            string strExistRights = "";
            bool bHasRight = user.HasRights(db.GetCaption("zh-CN"),
                ResType.Database,
                "clear",
                out strExistRights);
            if (bHasRight == false)
            {
                strError = "�����ʻ���Ϊ '" + user.Name + "'���� '" + strDbName + "' ���ݿ�û��'��ʼ����ˢ�¶���(clear)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                return -6;
            }

            if (strAction == "begin")
            {
                // 2009/7/19
                if (bClearAllKeyTables == true)
                {
                    db.InRebuildingKey = true;
                }

                // 3.ˢ�¶���
                // return:
                //		-1  ����
                //		0   �ɹ�
                return db.RefreshPhysicalDatabase(bClearAllKeyTables, out strError);
            }
            else if (strAction == "end")
            {
                Debug.Assert(strAction == "end", "");

                db.InRebuildingKey = false;
                return 0;
            }
            else if (strAction == "deletekeysindex")
            {
                return db.ManageKeysIndex(
                    "delete",
                    out strError);
            }
            else if (strAction == "createkeysindex")
            {
                return db.ManageKeysIndex(
                    "create",
                    out strError);
            }
            else if (strAction == "disablekeysindex")
            {
                return db.ManageKeysIndex(
                    "disable",
                    out strError);
            }
            else if (strAction == "rebuildkeysindex")
            {
                return db.ManageKeysIndex(
                    "rebuild",
                    out strError);
            }
            else if (strAction == "beginfastappend")
            {
                db.FastAppendTaskCount++;
                if (db.FastAppendTaskCount > 1)
                    return 0;

                Debug.Assert(db.FastAppendTaskCount == 1, "");

                // ׼����ˢ�¼������ ID �洢����
                if (db.RebuildIDs == null)
                {
                    db.RebuildIDs = new RecordIDStorage();
                    if (db.RebuildIDs.Open(this.GetTempFileName(), out strError) == -1)
                        return -1;
                }

#if NO
                // ����keys���index
                // parameters:
                //      strAction   delete/create
                return db.ManageKeysIndex(
                    "delete",
                    out strError);
#endif
                return 0;
            }
            else if (strAction == "endfastappend")
            {
                int nRet = 0;

                if (db.FastAppendTaskCount == 0)
                {
                    strError = "�����ݿ� '"+db.GetCaption("zh-CN")+"' endfastappend �����Ĵ������� beginfastappend �Ĵ��������� endfastappend �������ܾ�";
                    return -1;
                }
                db.FastAppendTaskCount--;
                if (db.FastAppendTaskCount > 0)
                    return 0;

                Debug.Assert(db.FastAppendTaskCount == 0, "");

                // �����Ҫˢ�¼�����
                // ������Ϊ����ģʽ�м�����������ǵ��������ʱ�����㴦������㣬���Դ洢����ID�����
                if (db.RebuildIDs != null && db.RebuildIDs.Count > 0)
                {
                        nRet = db.RebuildKeys(
                            "fastmode", // ����Ҫ deletekeys����Ϊÿ���Ĺ������Ѿ��Ѿɼ�¼�� keys ��ɾ������
                            out strError);
                        if (nRet == -1)
                            return -1;

                        // �������ӳٶѻ����г���д����� keys ��
                        int nKeysCount = nRet;
                }

                // �������ӳٶѻ����г���д����� keys ��
                // TODO: �����Ƿ��� delaytable ������ Buikcopy �Ƿ���С���Ϊɾ�� B+ ��Ȼ�� Buikcopy �����ϴ�(�ر���ԭ�п��м�¼�ܶ൫����׷�ӵ���ʵ��������)���������Ӧ��������
                // ���Կ���һ���㷨������ת��ǰ���ݿ������еļ�¼�����ͱ���׷�ӵ� keys �������бȽϣ����׷�ӵ��������٣��Ͳ�ֵ��ɾ�� B+ ��Ȼ���ؽ�
                {
                    bool bNeedDropIndex = false;
                    long lSize = db.BulkCopy(
                        // sessioninfo,
                        "getdelaysize",
                        out strError);
                    if (lSize == -1)
                        return -1;

                    if (lSize > 10 * 1024 * 1024)   // 10 M
                        bNeedDropIndex = true;

                    // bNeedDropTree = true;   // testing

                    if (bNeedDropIndex == true)
                    {
                        nRet = db.ManageKeysIndex(
            "disable",
            out strError);
                        if (nRet == -1)
                            return -1;
                    }

                    long lRet = db.BulkCopy(
                        // sessioninfo,
                        "",
                        out strError);
                    if (lRet == -1)
                        return -1;  // TODO: �Ƿ����������ɺ���Ĳ���?

                    if (bNeedDropIndex == true)
                    {
                        // ����keys���index
                        // parameters:
                        //      strAction   delete/create
                        nRet = db.ManageKeysIndex(
                            "rebuild",
                            out strError);
                        if (nRet == -1)
                            return -1;
                    }
                    // db.IsDelayWriteKey = false;
                }

                return 0;
            }
            else if (strAction == "flushpendingkeys")
            {
                // �������ӳٶѻ����г���д����� keys ��
                long lRet = db.BulkCopy(
                    // sessioninfo,
                    "",
                    out strError);
                return (int)lRet;
            }

            strError = "API_RefreshPhysicalDatabase() δ֪�� strAction ����ֵ '" + strAction + "'";
            return -1;
        }

        // �õ�key�ĳ���
        // parameters:
        //      nKeySize    out���������ؼ����㳤��
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        // ��: ����ȫ
        public int InternalGetKeySize(
            out int nKeySize,
            out string strError)
        {
            nKeySize = 0;
            strError = "";

            Debug.Assert(this.m_dom != null, "InternalGetKeySize()�﷢��this.m_domΪnull���쳣");

            XmlNode nodeKeySize = this.m_dom.DocumentElement.SelectSingleNode("keysize");
            if (nodeKeySize == null)
            {
                strError = "�����������ļ����Ϸ�,δ�ڸ��¶���<keysize>Ԫ��";
                return -1;
            }

            string strKeySize = nodeKeySize.InnerText.Trim(); // 2012/2/16
            try
            {
                nKeySize = Convert.ToInt32(strKeySize);
            }
            catch (Exception ex)
            {
                strError = "�����������ļ����Ϸ������µ�<keysize>Ԫ�ص����ݲ���Ϊ'" + strKeySize + "',����Ϊ���ָ�ʽ��" + ex.Message;
                return -1;
            }

            return 0;
        }

        // �����������Զ��������ݿ����Ƹ�ʽ���ҵ���Ӧ���ݿ�
        // strName: ���ݿ��� ��ʽΪ"����" �� "@id" �� "@id[����]"
        // ��: ��ȫ��
        // ����������
        public Database GetDatabaseSafety(
            string strDbName)
        {
            //******************�Կ⼯�ϼӶ���******
            m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
			this.WriteDebugInfo("GetDatabaseSafety()���Կ⼯�ϼӶ�����");
#endif
            try
            {
                return this.GetDatabase(strDbName);
            }
            finally
            {
                m_container_lock.ReleaseReaderLock();
                //*****************�Կ⼯�Ͻ����*************
#if DEBUG_LOCK
				this.WriteDebugInfo("GetDatabaseSafety()���Կ⼯�Ͻ������");
#endif
            }
        }

        // ����ָ�������԰汾���߼��������ݿ�
        // parameters:
        //		strLogicName	�߼�����
        //		strLang	���԰汾
        // return:
        //		�ҵ�����Database����
        //		û�ҵ�����null
        // ��: ��ȫ��
        public Database GetDatabaseByLogicNameSafety(string strDbName,
            string strLang)
        {
            //******************�Կ⼯�ϼӶ���******
            m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
			this.WriteDebugInfo("GetDatabaseByLogicNameSafety()���Կ⼯�ϼӶ�����");
#endif
            try
            {
                return this.GetDatabaseByLogicName(strDbName,
                    strLang);
            }
            finally
            {
                m_container_lock.ReleaseReaderLock();
                //*****************�Կ⼯�Ͻ����*********
#if DEBUG_LOCK
				this.WriteDebugInfo("GetDatabaseByLogicNameSafety()���Կ⼯�Ͻ������");
#endif
            }
        }

        // �������Ƶõ�һ�����ݿ�
        // parameters:
        //		strName	���ݿ����ƣ�Ҳ������ID(ǰ���@)
        // ��: ����ȫ
        public Database GetDatabase(string strName)
        {
            if (String.IsNullOrEmpty(strName) == true)
            {
                return null;
                // throw new Exception("���ݿ�������Ϊ��");
            }

            Debug.Assert(String.IsNullOrEmpty(strName) == false, "GetDatabase()���ô���strName����ֵ����Ϊnull����ַ�����");

            string strFirst = "";
            string strSecond = "";
            int nPosition = strName.LastIndexOf("[");
            if (nPosition >= 0)
            {
                strFirst = strName.Substring(0, nPosition);
                strSecond = strName.Substring(nPosition + 1);
            }
            else
            {
                strFirst = strName;
            }
            Database db = null;
            if (string.IsNullOrEmpty(strFirst) == false)
            {
                // if (strFirst.Substring(0, 1) == "@")
                if (strFirst[0] == '@')
                    db = GetDatabaseByID(strFirst);
                else
                    db = GetDatabaseByLogicName(strFirst);
            }
            else if (string.IsNullOrEmpty(strSecond) == false)
            {
                // if (strSecond.Substring(0, 1) == "@")

                if (strSecond[0] == '@')
                    db = GetDatabaseByID(strSecond);
                else
                    db = GetDatabaseByLogicName(strSecond);
            }
            return db;
        }


        // �����߼��������ݿ⣬�κ����԰汾������
        // ��: ����ȫ
        private Database GetDatabaseByLogicName(string strLogicName)
        {
            Debug.Assert(String.IsNullOrEmpty(strLogicName) == false, "GetDatabaseByLogicName()���ô���strLogicName����ֵ����Ϊnull����ַ�����");

            // �ȴӻ�������
            Database database = (Database)this.m_logicNameTable[strLogicName];
            if (database != null)
                return database;

            foreach (Database db in this)
            {
                if (StringUtil.IsInList(strLogicName,
                    db.GetCaptionsSafety()) == true)
                {
                    this.m_logicNameTable[strLogicName] = db;   // ���뻺��
                    return db;
                }
            }
            return null;
        }

        // ����ָ�������԰汾���߼��������ݿ�
        // parameters:
        //		strLogicName	�߼�����
        //		strLang	���԰汾
        // return:
        //		�ҵ�����Database����
        //		û�ҵ�����null
        // ��: ����ȫ
        private Database GetDatabaseByLogicName(string strLogicName,
            string strLang)
        {
            // �ȴӻ�������
            Database database = (Database)this.m_logicNameTable[strLogicName + "|" + strLang];
            if (database != null)
                return database;

            foreach (Database db in this)
            {
                if (String.Compare(strLogicName, db.GetCaptionSafety(strLang)) == 0)
                {
                    this.m_logicNameTable[strLogicName + "|" + strLang] = db;   // ���뻺��
                    return db;
                }
            }
            return null;
        }

        // ͨ�����ݿ�ID�ҵ�ָ�������ݿ⣬ע�������ID��@
        // ��: ����ȫ
        private Database GetDatabaseByID(string strDbID)
        {
            foreach (Database db in this)
            {
                if (db.FullID == strDbID)
                {
                    return db;
                }
            }
            return null;
        }

        // ����
        // parameter:
        //		strQuery	����ʽXML�ַ���
        //		resultSet	�����,���ڴ�ż������
        //		oUser	    �ʻ�����,���ڼ������ʻ���ĳ���Ƿ��ж�Ȩ��
        //  				Ϊnull,�򲻽���Ȩ�޵ļ�飬������Ȩ����
        //		isConnected	delegate����,����ͨѶ�Ƿ���������
        //					Ϊnull���򲻵�delegate����
        //		strError	out���������س�����Ϣ
        // return:
        //		-1	����
        //      -6  Ȩ�޲���
        //		0	�ɹ�
        // ��: ��ȫ��
        public int API_Search(
            SessionInfo sessioninfo,
            string strQuery,
            ref DpResultSet resultSet,
            User oUser,
            // Delegate_isConnected isConnected,
            ChannelHandle handle,
            string strOutputStyle,
            out string strError)
        {
            strError = "";

            this.Commit();

            //�Կ⼯�ϼӶ���*********************************
            m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
			this.WriteDebugInfo("Search()���Կ⼯�ϼӶ�����");
#endif
            try
            {
                if (String.IsNullOrEmpty(strQuery) == true)
                {
                    strError = "Search()���ô���strQuery����Ϊnull����ַ���";
                    return -1;
                }

                // һ�����ȸ��������m_strQuery��Ա��ֵ��
                // �����Ƿ��ǺϷ���XML�����ý������ʱ�����ж�
                XmlDocument dom = new XmlDocument();
                dom.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue
                try
                {
                    dom.LoadXml(strQuery);
                }
                catch (Exception ex)
                {
                    strError += "����ʽXML���ص�DOMʱ����ԭ��" + ex.Message + "\r\n"
                        + "����ʽ��������:\r\n"
                        + strQuery;
                    return -1;
                }

                //����Query����
                Query query = new Query(this,
                    oUser,
                    dom);

                //���м���
                // return:
                //		-1	����
                //		-6	��Ȩ��
                //		0	�ɹ�
                int nRet = query.DoQuery(
                    sessioninfo,
                    strOutputStyle,
                    dom.DocumentElement,
                    ref resultSet,
                    handle,
                    // isConnected,
                    out strError);
                if (resultSet != null)
                    resultSet.m_strQuery = strQuery;

                if (nRet <= -1)
                    return nRet;
            }
            finally
            {
                //****************�Կ⼯�Ͻ����**************
                m_container_lock.ReleaseReaderLock();
#if DEBUG_LOCK
				this.WriteDebugInfo("Search()���Կ⼯�Ͻ������");
#endif
            }
            return 0;
        }

#region CopyRecord() �¼�����

        // �ж�һ��·���Ƿ�Ϊ׷�ӷ�ʽ��·��
        bool IsAppendPath(string strResPath)
        {
            string strPath = strResPath;
            string strDbName = StringUtil.GetFirstPartPath(ref strPath);
            //***********�Ե���1��*************
            // ����Ϊֹ��strPath�������ݿ�����,�����·�����������:cfgs;���඼��������¼id
            if (strPath == "")
                return false;

#if NO
            // �ҵ����ݿ����
            Database db = this.GetDatabase(strDbName);    // �����Ѽ���
            if (db == null)
            {
                strError = "��Ϊ '" + strDbName + "' �����ݿⲻ���ڡ�";
                return -5;
            }
#endif

            string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
            //***********�Ե���2��*************
            // ����Ϊֹ��strPath������¼�Ų��ˣ��¼�������ж�
            string strRecordID = strFirstPart;
            // ֻ����¼�Ų��·��
            if (strPath == "")
            {
                if (strRecordID == "?"
                    || string.IsNullOrEmpty(strRecordID) == true)
                    return true;
                return false;
            }

            return false;
        }

        static List<string> GetIdList(XmlDocument dom)
        {
            List<string> results = new List<string>();

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("dprms", DpNs.dprms);
            XmlNodeList fileList = dom.DocumentElement.SelectNodes("//dprms:file", nsmgr);
            foreach (XmlElement file in fileList)
            {
                string strObjectID = file.GetAttribute("id");
                if (string.IsNullOrEmpty(strObjectID) == false)
                    results.Add(strObjectID);
            }

            return results;
        }

        // ���һ��δ���ù��� id
        static string GetNewID(List<string> existing_ids)
        {
            for (int i = 0; ; i++)
            {
                string strID = i.ToString();
                if (existing_ids.IndexOf(strID) == -1)
                    return strID;
            }
        }

        // ������� file Ԫ�ص� OuterXml
        static List<string> GetFileOuterXmls(XmlDocument dom)
        {
            List<string> results = new List<string>();

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("dprms", DpNs.dprms);
            XmlNodeList fileList = dom.DocumentElement.SelectNodes("//dprms:file", nsmgr);
            foreach (XmlElement file in fileList)
            {
                results.Add(file.OuterXml);
            }

            return results;
        }

        // ���һ�� file Ԫ�ص� OuterXml
        static string GetFileOuterXml(XmlDocument dom, string strID)
        {
            List<string> results = new List<string>();

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("dprms", DpNs.dprms);
            XmlNode file = dom.DocumentElement.SelectSingleNode("//dprms:file[@id='"+strID+"']", nsmgr);
            if (file != null)
                return file.OuterXml;
            return null;
        }

        static void RemoveFiles(XmlDocument dom)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("dprms", DpNs.dprms);
            XmlNodeList fileList = dom.DocumentElement.SelectNodes("//dprms:file", nsmgr);
            foreach (XmlElement file in fileList)
            {
                file.ParentNode.RemoveChild(file);
            }
        }

        // �������� file Ԫ��
        static void AddFiles(XmlDocument dom,
            List<string> outerxmls)
        {
            if (dom.DocumentElement == null)
                dom.LoadXml("<root />");
            foreach (string outerxml in outerxmls)
            {
                XmlDocumentFragment frag = dom.CreateDocumentFragment();
                frag.InnerXml = outerxml;

                dom.DocumentElement.AppendChild(frag);
            }
        }

        // ����һ�� files Ԫ��
        static void AddFile(XmlDocument dom,
            string strFragment,
            string strNewID = null)
        {
            if (dom.DocumentElement == null)
                dom.LoadXml("<root />");
            {
                XmlDocumentFragment frag = dom.CreateDocumentFragment();
                frag.InnerXml = strFragment;

                XmlNode new_node = dom.DocumentElement.AppendChild(frag);
                if (strNewID != null)
                    (new_node as XmlElement).SetAttribute("id", strNewID);
            }
        }

        class ChangeID
        {
            public string OldID = "";
            public string NewID = "";

            public ChangeID(string strOldID, string strNewID)
            {
                this.OldID = strOldID;
                this.NewID = strNewID;
            }
        }

        // �� source_dom �� file Ԫ�ؼ��� target_dom����� ID �Ѿ����ڣ������ ID
        static void AddFiles(XmlDocument source_dom,
            ref XmlDocument target_dom,
            out List<ChangeID> change_list)
        {
            change_list = new List<ChangeID>();
            List<string> source_ids = GetIdList(source_dom);
            if (source_ids.Count != 0)
            {
                List<string> writed_ids = GetIdList(target_dom);
                foreach (string id in source_ids)
                {
                    string strFragment = GetFileOuterXml(source_dom, id);
                    Debug.Assert(string.IsNullOrEmpty(strFragment) == false, "");
                    if (string.IsNullOrEmpty(strFragment) == true)
                        continue;

                    if (writed_ids.IndexOf(id) != -1)
                    {
                        string newid = GetNewID(writed_ids);
                        writed_ids.Add(newid);
                        change_list.Add(new ChangeID(id, newid));
                        AddFile(target_dom, strFragment, newid);
                    }
                    else
                        AddFile(target_dom, strFragment);
                }
            }
        }

        // ����޸ĺ�� id �ַ���
        static string GetChangedID(List<ChangeID> change_list, string strID)
        {
            foreach(ChangeID changed in change_list)
            {
                if (changed.OldID == strID)
                    return changed.NewID;
            }

            return strID;
        }

#endregion

        // ����һ��Դ��¼��Ŀ���¼��Ҫ���Դ��¼�ж�Ȩ�ޣ���Ŀ���¼��дȨ��
        // �ؼ�������������
        // Parameter:
        //      user                    �û�����
        //		strOriginRecordPath	    Դ��¼·��
        //		strTargetRecordPath	    Ŀ���¼·��
        //		bDeleteOriginRecord	    �Ƿ�ɾ��Դ��¼
        //      strMergeStyle           ��κϲ�������¼��Ԫ���ݲ���? reserve_source / reserve_target�� �ձ�ʾ reserve_source
        //      strOutputRecordPath     ����Ŀ���¼��·��������Ŀ���¼���½�һ����¼
        //      baOutputRecordTimestamp ����Ŀ���¼��ʱ���
        //      strChangeList           ���� id �޸ĵ�״��
        //		strError	������Ϣ
        // return:
        //		-1	һ���Դ���
        //      -4  δ�ҵ���¼
        //      -5  δ�ҵ����ݿ�
        //      -6  û���㹻��Ȩ��
        //      -7  ·�����Ϸ�
        //		0	�ɹ�
        public int API_CopyRecord(User user,
            string strOriginRecordPath,
            string strTargetRecordPath,
            bool bDeleteOriginRecord,
            string strMergeStyle,
            out string strIdChangeList,
            out string strTargetRecordOutputPath,
            out byte[] baOutputRecordTimestamp,
            out string strError)
        {
            Debug.Assert(user != null, "CopyRecord()���ô���user������Ϊnull��");

            // this.WriteErrorLog("�ߵ�CopyRecord(),strOriginRecordPath='" + strOriginRecordPath + "' strTargetRecordPath='" + strTargetRecordPath + "'");
            strIdChangeList = "";
            strTargetRecordOutputPath = "";
            baOutputRecordTimestamp = null;
            strError = "";

            if (String.IsNullOrEmpty(strOriginRecordPath) == true)
            {
                strError = "CopyRecord() ���ô���strOriginRecordPath ����ֵ���ܿ�";
                return -1;
            }
            if (String.IsNullOrEmpty(strTargetRecordPath) == true)
            {
                strError = "CopyRecord() ���ô���strTargetRecordPath ����ֵ����Ϊ��";
                return -1;
            }

            // ���Ŀ��·���������Ǽ�¼·����̬�����������������������ļ���Դ����̬
            bool bRecordPath = this.IsRecordPath(strTargetRecordPath);
            if (bRecordPath == false)
            {
                strError = "���Ʋ������ܾ�����ΪĿ���¼·�� '" + strTargetRecordPath + "' ���Ϸ�(�����Ǽ�¼·����̬)";
                return -1;
            }

            long nRet = 0;

            // �õ�Դ��¼��xml
            string strOriginRecordStyle = "data,metadata,timestamp";
            byte[] baOriginRecordData = null;
            string strOriginRecordMetadata = "";
            string strOriginRecordOutputPath = "";
            byte[] baOriginRecordOutputTimestamp = null;

            int nAdditionError = 0;
            // return:
            //		-1	һ���Դ���
            //		-4	δ�ҵ�·��ָ������Դ
            //		-5	δ�ҵ����ݿ�
            //		-6	û���㹻��Ȩ��
            //		-7	·�����Ϸ�
            //		-10	δ�ҵ���¼xpath��Ӧ�Ľڵ�  // �˴ε��ò����ܳ����������
            //		>= 0	�ɹ���������󳤶�
            nRet = this.API_GetRes(strOriginRecordPath,
                0,
                -1,
                strOriginRecordStyle,
                user,
                -1,
                out baOriginRecordData,
                out strOriginRecordMetadata,
                out strOriginRecordOutputPath,
                out baOriginRecordOutputTimestamp,
                out nAdditionError,
                out strError);
            if (nRet <= -1)
                return (int)nRet;

            // ��ȡĿ���¼
            // Ҫ�˽�ԭ��Ŀ���¼���Ƿ��� <fprms:file>�����������Ҫ����

            XmlDocument existing_dom = null;    // �Ѿ�����ԭ��¼�� XMLDOM
            List<string> existing_ids = new List<string>();
            if (IsAppendPath(strTargetRecordPath) == false)
            {
                byte[] baTempRecordData = null;
                string strTempRecordMetadata = "";
                byte[] baTargetRecordOutputTimestamp = null;

                // return:
                //		-1	һ���Դ���
                //		-4	δ�ҵ�·��ָ������Դ
                //		-5	δ�ҵ����ݿ�
                //		-6	û���㹻��Ȩ��
                //		-7	·�����Ϸ�
                //		-10	δ�ҵ���¼xpath��Ӧ�Ľڵ�  // �˴ε��ò����ܳ����������
                //		>= 0	�ɹ���������󳤶�
                nRet = this.API_GetRes(strTargetRecordPath,
                    0,
                    -1,
                    "data,metadata,timestamp",
                    user,
                    -1,
                    out baTempRecordData,
                    out strTempRecordMetadata,
                    out strTargetRecordOutputPath,
                    out baTargetRecordOutputTimestamp,
                    out nAdditionError,
                    out strError);
                if (nRet == -4)
                {
                    // Ŀ���¼������
                }
                else if (nRet <= -1)
                    return (int)nRet;
                else
                {
                    existing_dom = new XmlDocument();
                    byte[] baPreamble;
                    string strXml = DatabaseUtil.ByteArrayToString(baTempRecordData,
                        out baPreamble);
                    existing_dom.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue
                    try
                    {
                        existing_dom.LoadXml(strXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "���ؼ�¼ '" + strTargetRecordPath + "' �� XMLDOM ʱ����ԭ��" + ex.Message;
                        return -1;
                    }

                    existing_ids = GetIdList(existing_dom);
                }
            }
            
            XmlDocument source_dom = new XmlDocument(); // Դ��¼�� XMLDOM
            {
                byte[] baPreamble;
                string strXml = "";
                strXml = DatabaseUtil.ByteArrayToString(baOriginRecordData,
                     out baPreamble);
                source_dom.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue
                try
                {
                    source_dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "���ؼ�¼ '" + strOriginRecordPath + "' �� XMLDOM ʱ����ԭ��" + ex.Message;
                    return -1;
                }
            }

            List<ChangeID> change_list = new List<ChangeID>();  // �������䶯�� id

            XmlDocument target_dom = null;  // ����д��Ŀ��λ�õļ�¼
            if (StringUtil.IsInList("reserve_source", strMergeStyle) == true
                || string.IsNullOrEmpty(strMergeStyle) == true
                || existing_dom == null)
            {
                target_dom = new XmlDocument();
                target_dom.LoadXml(source_dom.OuterXml);

                if (existing_dom == null)
                {
                }
                else
                {
                    // ��Ҫɾ��ȫ�� file Ԫ�أ����¼��� existing_dom �����ȫ�� file Ԫ�أ����¼��� source_dom �е� file Ԫ��(id������Ϊ��ͻ�������仯)
                    List<string> file_outerxmls = GetFileOuterXmls(existing_dom);
                    if (file_outerxmls.Count > 0)
                    {
                        RemoveFiles(target_dom);
                        AddFiles(target_dom, file_outerxmls);

                        // �� source_dom �� file Ԫ�ؼ��� target_dom����� ID �Ѿ����ڣ������ ID
                        AddFiles(source_dom,
                    ref target_dom,
                    out change_list);
                    }
                }
            }
            else
            {
                Debug.Assert(existing_dom != null, "");

                target_dom = new XmlDocument();
                target_dom.LoadXml(existing_dom.OuterXml);

                // existing_dom �����ȫ�� file Ԫ���Ѿ����ڣ���Ҫ�¼��� source_dom �е� file Ԫ��

                // �� source_dom �� file Ԫ�ؼ��� target_dom����� ID �Ѿ����ڣ������ ID
                AddFiles(source_dom,
            ref target_dom,
            out change_list);
            }

            Debug.Assert(target_dom != null, "");

            // дĿ���¼xml
#if NO
            long lTargetRecordTotalLength = baOriginRecordData.Length;
            byte[] baTargetRecordData = baOriginRecordData;
#endif
            byte[] baTargetRecordData = Encoding.UTF8.GetBytes(target_dom.OuterXml);
            long lTargetRecordTotalLength = baTargetRecordData.Length;
            string strTargetRecordRanges = "0-" + (lTargetRecordTotalLength - 1).ToString();

            string strTargetRecordMetadata = strOriginRecordMetadata;

            // TODO: ��Ҫ�޸� lastmodiefied ʱ��
            // return:
            //		-1	����
            //		0	�ɹ�
            nRet = DatabaseUtil.MergeMetadata(strOriginRecordMetadata,
                "",
                lTargetRecordTotalLength,
                out strTargetRecordMetadata,
                out strError);
            if (nRet == -1)
            {
                strError = "�޸� metadata ʱ��������: " + strError;
                return -1;
            }

            string strTargetRecordStyle = "ignorechecktimestamp";
            // byte[] baTargetRecordOutputTimestamp = null;
            string strTargetRecordOutputValue = "";

#if NO
            if (strTargetRecordPath == "test111/186769")
            {
                Debug.Assert(false, "");
            }
#endif

            // return:
            //		-1	һ���Դ���
            //		-2	ʱ�����ƥ��    // �˴����ò����ܳ����������
            //		-4	δ�ҵ�·��ָ������Դ
            //		-5	δ�ҵ����ݿ�
            //		-6	û���㹻��Ȩ��
            //		-7	·�����Ϸ�
            //		-8	�Ѿ�����ͬ��ͬ���͵���  // �˴����ò����ܳ����������
            //		-9	�Ѿ�����ͬ������ͬ���͵���  // �˴����ò����ܳ����������
            //		0	�ɹ�
            nRet = this.API_WriteRes(strTargetRecordPath,
                strTargetRecordRanges,
                lTargetRecordTotalLength,
                baTargetRecordData,
                // null, //streamSource
                strTargetRecordMetadata,
                strTargetRecordStyle,
                null, //baInputTimestamp
                user,
                out strTargetRecordOutputPath,
                out baOutputRecordTimestamp,    // out baTargetRecordOutputTimestamp,
                out strTargetRecordOutputValue,
                out strError);
            if (nRet <= -1)
                return (int)nRet;

            // ������Դ


#if NO
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("dprms", DpNs.dprms);
            XmlNodeList fileList = dom.DocumentElement.SelectNodes("//dprms:file", nsmgr);
#endif

            // ���ƶ�����Դ
            List<string> source_ids = GetIdList(source_dom);

            foreach (string strObjectID in source_ids)
            {
                string strOriginObjectPath = strOriginRecordPath + "/object/" + strObjectID;
                string strTargetObjectPath = strTargetRecordOutputPath + "/object/" + GetChangedID(change_list, strObjectID);

                int nStart = 0;
                int nChunkSize = 1024 * 100;    // 100K
                long lTotalLength = 0;

                // ��Ƭ��ȡ��д����Դ����
                for (; ; )
                {
                    // ��ȡԴ��Դ����
                    byte[] baOriginObjectData = null;
                    string strOriginObjectMetadata = "";
                    string strOriginObjectOutputPath = "";
                    byte[] baOriginObjectOutputTimestamp = null;

                    // int nAdditionError = 0;
                    // return:
                    //		-1	һ���Դ���
                    //		-4	δ�ҵ�·��ָ������Դ
                    //		-5	δ�ҵ����ݿ�
                    //		-6	û���㹻��Ȩ��
                    //		-7	·�����Ϸ�
                    //		-10	δ�ҵ���¼xpath��Ӧ�Ľڵ�
                    //		>= 0	�ɹ���������󳤶�
                    nRet = this.API_GetRes(strOriginObjectPath,
                        nStart,
                        nChunkSize,
                        "data,metadata",
                        user,
                        -1,
                        out baOriginObjectData,
                        out strOriginObjectMetadata,
                        out strOriginObjectOutputPath,
                        out baOriginObjectOutputTimestamp,
                        out nAdditionError,
                        out strError);
                    if (nRet <= -1)
                        return (int)nRet;

                    lTotalLength = nRet;

                    // дĿ����Դ����
                    long lTargetObjectTotalLength = baOriginObjectData.Length;
                    string strTargetObjectMetadata = strOriginObjectMetadata;
                    string strTargetObjectStyle = "ignorechecktimestamp";
                    string strTargetObjectOutputPath = "";
                    byte[] baTargetObjectOutputTimestamp = null;
                    string strTargetObjectOutputValue = "";

                    string strRange = nStart.ToString() + "-" + (nStart + baOriginObjectData.Length - 1).ToString();

                    if (lTotalLength == 0)
                        strRange = "";

                    // this.WriteErrorLog("�ߵ�CopyRecord(),д��Դ��Ŀ��·��='" + strTargetObjectPath + "'");

                    // return:
                    //		-1	һ���Դ���
                    //		-2	ʱ�����ƥ��
                    //		-4	δ�ҵ�·��ָ������Դ
                    //		-5	δ�ҵ����ݿ�
                    //		-6	û���㹻��Ȩ��
                    //		-7	·�����Ϸ�
                    //		-8	�Ѿ�����ͬ��ͬ���͵���
                    //		-9	�Ѿ�����ͬ������ͬ���͵���
                    //		0	�ɹ�
                    nRet = this.API_WriteRes(strTargetObjectPath,
                        strRange,
                        lTotalLength,
                        baOriginObjectData,
                        // null,
                        strTargetObjectMetadata,
                        strTargetObjectStyle,
                        null,
                        user,
                        out strTargetObjectOutputPath,
                        out baTargetObjectOutputTimestamp,
                        out strTargetObjectOutputValue,
                        out strError);
                    if (nRet <= -1)
                        return (int)nRet;

                    nStart += baOriginObjectData.Length;
                    if (nStart >= lTotalLength)
                        break;
                }
            }

            // ɾ��Դ��¼
            if (bDeleteOriginRecord == true)
            {
                // return:
                //      -1	һ���Դ�����������������Ϸ���
                //      -2	ʱ�����ƥ��    // �������ʱ�������Ӧ�����������
                //      -4	δ�ҵ�·����Ӧ����Դ
                //      -5	δ�ҵ����ݿ�
                //      -6	û���㹻��Ȩ��
                //      -7	·�����Ϸ�
                //      0	�����ɹ�
                nRet = this.API_DeleteRes(strOriginRecordPath,
                    user,
                    baOriginRecordOutputTimestamp,
                    "",
                    out baOriginRecordOutputTimestamp,
                    out strError);
                if (nRet <= -1)
                    return (int)nRet;
            }

#if NO
            // ȡ��Ŀ���¼������ʱ���
            // return:
            //		-1  ����
            //		-4  δ�ҵ���¼
            //      0   �ɹ�
            nRet = this.GetTimestampFromDb(
                strTargetRecordOutputPath,
                out baOutputRecordTimestamp,
                out strError);
            if (nRet <= -1)
            {
                strError = "������¼��ɣ�����ȡĿ���¼��ʱ���ʱ����" + strError;
                return -1;
            }
#endif

            return 0;
        }

#if NO
        // ��ȡ��¼��ʱ���
        // parameters:
        //      strRecordPath   ��¼·��
        //      baOutputTimestamp   out����������ʱ���
        //      strError    out���������س�����Ϣ
        // return:
        //		-1  ����
        //		-4  δ�ҵ���¼
        //      0   �ɹ�
        public int GetTimestampFromDb(string strRecordPath,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";
            Debug.Assert(strRecordPath != null && strRecordPath != "", "GetTimestampFromDb()���ô���strRecordPath����ֵ����Ϊnull����ַ�����");

            DbPath dbpath = new DbPath(strRecordPath);
            Database db = this.GetDatabase(dbpath.Name);
            if (db == null)
            {
                strError = "δ�ҵ���Ϊ'" + dbpath.Name + "'�����ݿ⡣";
                return -1;
            }

            // return:
            //		-1  ����
            //		-4  δ�ҵ���¼
            //      0   �ɹ�
            int nRet = db.GetTimestampFromDb(dbpath.ID,
                out baOutputTimestamp,
                out strError);

            return nRet;
        }
#endif

        // ���Ŀ¼��������
        // parameters:
        //		strDirCfgItemPath	����Ŀ¼��·��
        //		nodeDir	            dir�ڵ㣬���Ϊnull�������·������
        //		strError        	out���������س�����Ϣ
        // return:
        //		-1	����
        //      -4  δָ��·����Ӧ�Ķ���
        //		0	�ɹ�
        // ���dir����������������¼������ԣ�Ҳɾ���¼���Ӧ�������ļ�
        public int ClearDirCfgItem(string strDirCfgItemPath,
            XmlNode nodeDir,
            out string strError)
        {
            strError = "";
            if (nodeDir == null)
            {
                if (String.IsNullOrEmpty(strDirCfgItemPath) == true)
                {
                    strError = "ClearDirCfgItem()���ô���strDirCfgItemPath��������Ϊnull���߿��ַ�����";
                    return -1;
                }

                List<XmlNode> nodes = DatabaseUtil.GetNodes(this.NodeDbs,
                    strDirCfgItemPath);
                if (nodes.Count == 0)
                {
                    strError = "ClearDirCfgItem()��δ�ҵ�·��Ϊ'" + strDirCfgItemPath + "'���������";
                    return -4;
                }

                if (nodes.Count > 1)
                {
                    strError = "ClearDirCfgItem()��·��Ϊ'" + strDirCfgItemPath + "'������������'" + Convert.ToString(nodes.Count) + "'����databases.xml�����ļ����Ϸ���";
                    return -1;
                }

                nodeDir = nodes[0];
            }

            // ɾ������ı���Ŀ¼
            string strLocalDir = "";
            strLocalDir = DatabaseUtil.GetLocalDir(this.NodeDbs,
                nodeDir).Trim();

            string strDir = "";
            if (strLocalDir != "")
                strDir = this.DataDir + "\\" + strLocalDir + "\\";
            else
                strDir = this.DataDir + "\\";

            DirectoryInfo di = new DirectoryInfo(strDir);

            // ɾ�����е��¼�Ŀ¼
            DirectoryInfo[] dirs = di.GetDirectories();
            foreach (DirectoryInfo childDir in dirs)
            {
                Directory.Delete(childDir.FullName, true);
            }

            // ɾ�����е��¼��ļ�
            FileInfo[] files = di.GetFiles();
            foreach (FileInfo childFile in files)
            {
                File.Delete(childFile.FullName);
            }

            // �Ƴ��ڴ����
            nodeDir.RemoveAll();

            this.Changed = true;

            return 0;
        }


        // ���ڴ��������һ����������
        // parameters:
        //		strParentPath	����·�� ���Ϊnull����ַ�������ֱ����objects�¼��½�
        //		strName	�Լ������ƣ�����Ϊnull����ַ���
        //		bDir	�Ƿ���·��
        //		strError	out���������س�����Ϣ
        // return:
        //		-1	����
        //		0	�ɹ�
        public int SetFileCfgItem(
            bool bNeedLock,
            string strParentPath,
            XmlNode nodeParent,
            string strName,
            out string strError)
        {
            strError = "";

            if (bNeedLock == true)
            {
                //**********�����ݿ⼯�ϼ�д��**************
                this.m_container_lock.AcquireWriterLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
                this.WriteDebugInfo("SetCfgItem()�������ݼ��ϼ�д����");
#endif
            }

            try
            {
                if (String.IsNullOrEmpty(strName) == true)
                {
                    strError = "SetCfgItem()���ô���strName����ֵ����Ϊnull����ַ�����";
                    return -1;
                }

                if (nodeParent == null)
                {
                    if (strParentPath == "" || strParentPath == null)
                    {
                        nodeParent = this.NodeDbs;
                    }
                    else
                    {
                        List<XmlNode> parentNodes = DatabaseUtil.GetNodes(this.NodeDbs,
                            strParentPath);
                        if (parentNodes.Count > 1)
                        {
                            strError = "��<objects>�¼�·��Ϊ'" + strParentPath + "'����������'" + Convert.ToString(parentNodes.Count) + "'���������ļ����Ϸ�����";
                            return -1;
                        }
                        if (parentNodes.Count == 0)
                        {
                            strError = "��<objects>�¼�δ�ҵ�·��Ϊ'" + strParentPath + "'�������";
                            return -1;
                        }

                        nodeParent = parentNodes[0];
                    }
                }

                string strCfgItemOuterXml = "";
                string strLocalName = strName + ".xml";
                strCfgItemOuterXml = "<file name='" + strName + "' localname='" + strLocalName + "'/>";

                nodeParent.InnerXml = nodeParent.InnerXml + strCfgItemOuterXml;

                this.Changed = true;

                return 0;
            }
            finally
            {
                if (bNeedLock == true)
                {

                    //***********�����ݿ⼯�Ͻ�д��***************
                    this.m_container_lock.ReleaseWriterLock();
#if DEBUG_LOCK
                    this.WriteDebugInfo("SetCfgItem()�������ݿ⼯�Ͻ�д����");
#endif
                }
            }
        }


        // �Զ�����Ŀ¼��������
        // parameters:
        //		strParentPath	����·�� ���Ϊnull����ַ�������ֱ����objects�¼��½�
        //		strName	�Լ������ƣ�����Ϊnull����ַ���
        //		bDir	�Ƿ���·��
        //		strError	out���������س�����Ϣ
        // return:
        //		-1	����
        //		0	�ɹ�
        public int AutoCreateDirCfgItem(
            bool bNeedLock,
            string strDirCfgItemPath,
            out string strError)
        {
            strError = "";

            if (bNeedLock == true)
            {
                //**********�����ݿ⼯�ϼ�д��**************
                this.m_container_lock.AcquireWriterLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
                this.WriteDebugInfo("AutoCreateDirCfgItem()�������ݿ⼯�ϼ�д����");
#endif
            }
            try
            {
                if (String.IsNullOrEmpty(strDirCfgItemPath) == true)
                {
                    strError = "AutoCreateDirCfgItem()���ô���strDirCfgItemPath����ֵ����Ϊnull����ַ�����";
                    return -1;
                }

                List<XmlNode> nodes = DatabaseUtil.GetNodes(this.NodeDbs,
                    strDirCfgItemPath);
                if (nodes.Count > 1)
                {
                    strError = "·��Ϊ'" + strDirCfgItemPath + "'������������'" + Convert.ToString(nodes.Count) + "'���������������ļ����Ϸ���";
                    return -1;
                }
                if (nodes.Count == 1)
                {
                    strError = "AutoCreateDirCfgItem()���ô����Ѵ���·��Ϊ'" + strDirCfgItemPath + "'������Ŀ¼��";
                    return -1;
                }

                XmlDocument dom = this.NodeDbs.OwnerDocument;
                if (dom == null)
                {
                    strError = "AutoCreateDirCfgItem()�ﲻ�����Ҳ���dom��";
                    return -1;
                }

                //��strpath��'/'�ֿ�
                string[] paths = strDirCfgItemPath.Split(new char[] { '/' });
                if (paths.Length == 0)
                {
                    strError = "AutoCreateDirCfgItem()��paths���Ȳ�����Ϊ0��";
                    return -1;
                }

                int i = 0;
                if (paths[0] == "")
                    i = 1;
                XmlNode nodeCurrent = this.NodeDbs;
                XmlNode temp = null;
                for (; i < paths.Length; i++)
                {
                    string strDirName = paths[i];

                    if (nodeCurrent == this.NodeDbs)
                    {
                        //XmlNode temp = null;
                        foreach (XmlNode tempChild in nodeCurrent.ChildNodes)
                        {
                            if (tempChild.Name == "database")
                            {
                                string strAllCaption = DatabaseUtil.GetAllCaption(tempChild);
                                if (StringUtil.IsInList(strDirName, strAllCaption, true) == true)
                                {
                                    temp = tempChild;
                                    break;
                                }
                            }
                            else
                            {
                                string strTempName = DomUtil.GetAttr(tempChild, "name");
                                if (String.Compare(strTempName, strDirName, true) == 0)
                                {
                                    temp = tempChild;
                                    break;
                                }
                            }
                        }

                        if (temp == null)
                        {
                            temp = dom.CreateElement("dir");
                            DomUtil.SetAttr(temp, "name", strDirName);
                            DomUtil.SetAttr(temp, "localdir", strDirName);
                            nodeCurrent.AppendChild(temp);
                        }

                        nodeCurrent = temp;
                    }
                    else
                    {
                        string strTempXpath = "dir[@name='" + strDirName + "']";
                        temp = nodeCurrent.SelectSingleNode(strTempXpath);
                        if (temp == null)
                        {
                            temp = dom.CreateElement("dir");
                            DomUtil.SetAttr(temp, "name", strDirName);
                            DomUtil.SetAttr(temp, "localdir", strDirName);
                            nodeCurrent.AppendChild(temp);
                        }
                        nodeCurrent = temp;
                    }
                }

                nodes = DatabaseUtil.GetNodes(this.NodeDbs,
                    strDirCfgItemPath);
                if (nodes.Count > 1)
                {
                    strError = "�����Զ�������·��Ϊ'" + strDirCfgItemPath + "'������������'" + Convert.ToString(nodes.Count) + "'�������Բ����ܵ������";
                    return -1;
                }
                if (nodes.Count == 0)
                {
                    strError = "AutoCreateDirCfgItem()���Զ�����'" + strDirCfgItemPath + "'����Ŀ¼�ڴ������ϣ������ܻ��ǲ����ڡ�";
                    return -1;
                }
                XmlNode node = nodes[0];

                string strDir = DatabaseUtil.GetLocalDir(this.NodeDbs,
                    node);
                strDir = this.DataDir + "\\" + strDir;
                PathUtil.CreateDirIfNeed(strDir);

                this.Changed = true;

                return 0;
            }
            finally
            {
                if (bNeedLock == true)
                {

                    //***************�����ݿ⼯�Ͻ�д��************
                    this.m_container_lock.ReleaseWriterLock();
#if DEBUG_LOCK
                    this.WriteDebugInfo("AutoCreateDirCfgItem()�������ݿ⼯�Ͻ�д����");
#endif
                }
            }
        }

        int m_testCount = 0;

        // д������ XML ��¼
        public int API_WriteRecords(
            // SessionInfo sessioninfo,
            User user,
            RecordBody[] inputs,
            string strStyle,
            out List<RecordBody> results,
            out string strError)
        {
            strError = "";
            results = new List<RecordBody>();

            int nRet = 0;

            if (StringUtil.IsInList("flushkeys", strStyle) == true)
            {
                //**********�Կ⼯�ϼ�д��****************
                // flushkeys�����ǻ����ų�ģ����ܲ�������
                m_container_lock.AcquireWriterLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
			this.WriteDebugInfo("API_WriteRecords()���Կ⼯�ϼ�д����");
#endif
                try
                {
                    foreach (Database db in this)
                    {
                        long lRet = db.BulkCopy(// sessioninfo,
                            "",
                            out strError);
                        if (lRet == -1)
                            return -1;
                    }
                    if (inputs == null || inputs.Length == 0)
                        return 0;
                }
                finally
                {
                    //**********�Կ⼯�Ͻ�д��****************
                    m_container_lock.ReleaseWriterLock();
#if DEBUG_LOCK
			this.WriteDebugInfo("API_WriteRecords()���Կ⼯�Ͻ�д����");
#endif
                }
            }

            if (user == null)
            {
                strError = "API_WriteRecords()���ô���user������Ϊnull";
                return -1;
            }

            //**********�Կ⼯�ϼӶ���****************
            m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
			this.WriteDebugInfo("API_WriteRecords()���Կ⼯�ϼӶ�����");
#endif
            try
            {
                // ��Ҫд���������ݿ�ֿ������ɸ�����
                // Hashtable database_table = new Hashtable(); // ���ݿ���� --> List<RecordBody>
                Dictionary<Database, List<RecordBody>> database_table = new Dictionary<Database, List<RecordBody>>();

                foreach (RecordBody record in inputs)
                {
                    record.Result = new Result();

                    // ���·���Ƿ�Ϊ��
                    if (String.IsNullOrEmpty(record.Path) == true)
                    {
                        record.Result.Value = -1;
                        record.Result.ErrorString = "Path ����Ϊ��";
                        record.Result.ErrorCode = ErrorCodeValue.PathError; // -7;
                        continue;
                    }

                    // ���·������
                    bool bRecordPath = this.IsRecordPath(record.Path);
                    if (bRecordPath == false)
                    {
                        record.Result.Value = -1;
                        record.Result.ErrorString = "Path Ŀǰֻ����ʹ�����ݿ��¼���͵�·��";
                        record.Result.ErrorCode = ErrorCodeValue.CommonError;
                        continue;
                    }

                    // ����·�������ݿ�������

                    string strPath = record.Path;
                    string strDbName = StringUtil.GetFirstPartPath(ref strPath);
                    //***********�Ե���1��*************
                    // ����Ϊֹ��strPath�������ݿ�����,�����·�����������:cfgs;���඼��������¼id
                    if (strPath == "")
                    {
                        record.Result.Value = -1;
                        record.Result.ErrorString = "��Դ·�� '" + record.Path + "' ���Ϸ���δָ������¼�";
                        record.Result.ErrorCode = ErrorCodeValue.PathError; // -7;
                        continue;
                    }

                    // �ҵ����ݿ����
                    Database db = this.GetDatabase(strDbName);
                    if (db == null)
                    {
                        record.Result.Value = -1;
                        record.Result.ErrorString = "��Ϊ '" + strDbName + "' �����ݿⲻ���ڡ�";
                        record.Result.ErrorCode = ErrorCodeValue.NotFoundDb; // -5;
                        continue;
                    }

                    List<RecordBody> records = null;
                    if (database_table.ContainsKey(db) == true)
                        records = (List<RecordBody>)database_table[db];
                    if (records == null)
                    {
                        records = new List<RecordBody>();
                        database_table[db] = records;
                    }

                    records.Add(record);
                }

                // ��ÿ�����ݿ����һ����д��
                bool bError = false;
                List<RecordBody> temp_results = new List<RecordBody>();
                foreach (Database db in database_table.Keys)
                {
                    List<RecordBody> records = database_table[db];
                    List<RecordBody> outputs = null;
                    nRet = db.WriteRecords(
                        // sessioninfo,
                        user,
                        records,
                        strStyle,
                        out outputs,
                        out strError);
                    if (outputs != null)
                        temp_results.AddRange(outputs); // outputs �е�Ԫ��˳������� records �п����Ѿ����ң����� outputs ��Ԫ�ظ�������ƫ�٣���Щû�б�����
                    if (nRet == -1)
                    {
                        bError = true;
                        // ע��˺� strError ��Ӧ��ʹ��
                        break;
                    }
                }

                // ����ԭʼ inputs �е�˳�򣬴������ؽ����
                foreach (RecordBody record in inputs)
                {
                    if (temp_results.IndexOf(record) != -1)
                    {
                        results.Add(record);
                    }
                    else
                    {
                        record.Result = new Result();
                        record.Result.Value = -1;
                        record.Result.ErrorCode = ErrorCodeValue.CommonError;
                        record.Result.ErrorString = "û�д���";
                        record.Xml = "";
                        record.Metadata = "";
                        record.Timestamp = null;
                        results.Add(record);
                    }
                }
                // TODO: �Ѻ�������û�д����Ԫ�ض�ɾ��?


                if (bError == true)
                {
                    // �Ѿ������ results �����Է���
                    return -1;
                }

            }
            finally
            {
                //**********�Կ⼯�Ͻ����****************
                m_container_lock.ReleaseReaderLock();
#if DEBUG_LOCK
			this.WriteDebugInfo("API_WriteRecords()���Կ⼯�Ͻ������");
#endif
            }
            return 0;
        }

        // д��Դ
        // parameter:
        //		strResPath		��Դ·��,����Ϊnull����ַ���
        //						��Դ���Ϳ��������ݿ���������(Ŀ¼���ļ�)����¼�壬������Դ�����ּ�¼��
        //						��������: ����/��������·��
        //						��¼��: ����/��¼��
        //						������Դ: ����/��¼��/object/��ԴID
        //						���ּ�¼��: ����/��¼/xpath/<locate>hitcount</locate><action>AddInteger</action> ���� ����/��¼/xpath/@hitcount
        //		strRanges		Ŀ���λ��,���range�ö��ŷָ�,null��Ϊ�ǿ��ַ��������ַ�����Ϊ��0-(lTotalLength-1)
        //		lTotalLength	��Դ�ܳ���,����Ϊ0
        //		baContent		��byte[]���ݴ��͵���Դ���ݣ����Ϊnull���ʾ��0�ֽڵ�����
        //		streamContent	������
        //		strMetadata		Ԫ�������ݣ�null��Ϊ�ǿ��ַ�����ע:��ЩԪ������Ȼ�������������������ϣ����糤��
        //		strStyle		���,null��Ϊ�ǿ��ַ���
        //						ignorechecktimestamp ����ʱ���;
        //						createdir,����Ŀ¼,·����ʾ��������Ŀ¼·��
        //						autocreatedir	�Զ������м���Ŀ¼
        //						content	���ݷ���baContent������
        //						attachment	���ݷ��ڸ�����
        //		baInputTimestamp	�����ʱ���,���ڴ���Ŀ¼�������ʱ���
        //		user	�ʻ����󣬲���Ϊnull
        //		strOutputResPath	���ص���Դ·��
        //							����׷�Ӽ�¼ʱ������ʵ�ʵ�·��
        //							������Դ���ص�·���������·����ͬ
        //		baOutputTimestamp	����ʱ���
        //							��ΪĿ¼ʱ�����ص�ʱ���Ϊnull
        //		strOutputValue	���ص�ֵ���������ۼӼ���ʱ
        //		strError	������Ϣ
        // ˵����
        //		������ʵ�ʴ���������������½���Դ��������Դ
        //		baContent��strAttachmentIDֻ��ʹ��һ������strStyle����ʹ��
        // return:
        //		-1	һ���Դ���
        //		-2	ʱ�����ƥ��
        //		-4	δ�ҵ�·��ָ������Դ
        //		-5	δ�ҵ����ݿ�
        //		-6	û���㹻��Ȩ��
        //		-7	·�����Ϸ�
        //		-8	�Ѿ�����ͬ��ͬ���͵���
        //		-9	�Ѿ�����ͬ������ͬ���͵���
        //		0	�ɹ�
        // �ߣ���ȫ
        // ����������
        public int API_WriteRes(
            string strResPath,
            string strRanges,
            long lTotalLength,
            byte[] baSource,
            // Stream streamSource,
            string strMetadata,
            string strStyle,
            byte[] baInputTimestamp,
            User user,
            out string strOutputResPath,
            out byte[] baOutputTimestamp,
            out string strOutputValue,
            out string strError)
        {
            baOutputTimestamp = null;
            strOutputResPath = strResPath;
            strOutputValue = "";
            strError = "";
            int nRet = 0;

            // 2006/12/18 ��д����Ϊ����
            //**********�Կ⼯�ϼӶ���****************
            m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
			this.WriteDebugInfo("WriteRes()���Կ⼯�ϼӶ�����");
#endif
            try
            {

                //------------------------------------------------
                //�����������Ƿ�Ϸ������淶�������
                //---------------------------------------------------
                if (user == null)
                {
                    strError = "WriteRes()���ô���user������Ϊnull";
                    return -1;
                }
                if (String.IsNullOrEmpty(strResPath) == true)
                {
                    strError = "��Դ·��'" + strResPath + "'���Ϸ�������Ϊnull����ַ�����";
                    return -7;
                }
                if (lTotalLength < 0)
                {
                    strError = "WriteRes()��lTotalLength����Ϊ'" + Convert.ToString(lTotalLength) + "'������>=0��";
                    return -1;
                }
                if (strRanges == null) //����ĺ������ᴦ��ɴ���ķ�Χ
                    strRanges = "";
                if (strMetadata == null)
                    strMetadata = "";
                if (strStyle == null)
                    strStyle = "";

                /*
                if (baSource == null && streamSource == null)
                {
                    strError = "WriteRes()���ô���baSource������streamSource��������ͬʱΪnull��";
                    return -1;
                }
                if (baSource != null && streamSource != null)
                {
                    strError = "WriteRes()���ô���baSource������streamSource����ֻ����һ������ֵ��";
                    return -1;
                }
                 * */
                if (baSource == null)
                {
                    strError = "WriteRes()���ô���baSource��������Ϊnull��";
                    return -1;
                }


                //------------------------------------------------
                //��������Դ������
                //---------------------------------------------------

                bool bRecordPath = this.IsRecordPath(strResPath);
                if (bRecordPath == false)
                {
                    // ���·�����Ƿ��зǷ��ַ�
                    if (strResPath.IndexOfAny(new char[] {'?','*','��','��'}) != -1)
                    {
                        strError = "·�� '"+strResPath+"' ��ʽ���Ϸ�����ʾĿ¼���ļ���Դ��·���ַ����в��ܰ������� ? *";
                        return -1;
                    }

                    // ��������Ŀ¼
                    if (StringUtil.IsInList("createdir", strStyle, true) == true)
                    {
                        // return:
                        //      -1  һ���Դ���
                        //		-4	δָ��·����Ӧ�Ķ���
                        //		-6	Ȩ�޲���
                        //		-8	Ŀ¼�Ѵ���
                        //		-9	�����������͵�����
                        //		0	�ɹ�
                        nRet = this.WriteDirCfgItem(
                            false,
                            strResPath,
                            strStyle,
                            user,
                            out strError);
                    }
                    else
                    {
                        // return:
                        //      -1  һ���Դ���
                        //      -2  ʱ�����ƥ��
                        //      -4  �Զ�����Ŀ¼ʱ��δ�ҵ��ϼ�
                        //		-6	Ȩ�޲���
                        //		-9	�����������͵�����
                        //		0	�ɹ�
                        nRet = this.WriteFileCfgItem(
                            false,
                            strResPath,
                            strRanges,
                            lTotalLength,
                            baSource,
                            // streamSource,
                            strMetadata,
                            strStyle,
                            baInputTimestamp,
                            user,
                            out baOutputTimestamp,
                            out strError);
                    }

                    strOutputResPath = strResPath;

                    // ����database.xml�ļ�
                    if (this.Changed == true)
                        this.SaveXml();  // �����Ѿ�����
                }
                else
                {
                    bool bObject = false;
                    string strRecordID = "";
                    string strObjectID = "";
                    string strXPath = "";

                    string strPath = strResPath;
                    string strDbName = StringUtil.GetFirstPartPath(ref strPath);
                    //***********�Ե���1��*************
                    // ����Ϊֹ��strPath�������ݿ�����,�����·�����������:cfgs;���඼��������¼id
                    if (strPath == "")
                    {
                        strError = "��Դ·��'" + strResPath + "'·�����Ϸ���δָ������¼���";
                        return -7;
                    }
                    // �ҵ����ݿ����
                    Database db = this.GetDatabase(strDbName);    // �����Ѽ���
                    if (db == null)
                    {
                        strError = "��Ϊ '" + strDbName + "' �����ݿⲻ���ڡ�";
                        return -5;
                    }

                    string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    //***********�Ե���2��*************
                    // ����Ϊֹ��strPath������¼�Ų��ˣ��¼�������ж�


                    strRecordID = strFirstPart;
                    // ֻ����¼�Ų��·��
                    if (strPath == "")
                    {
                        bObject = false;
                        goto DOWRITE;
                    }

                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    //***********�Ե���2��*************
                    // ����Ϊֹ��strPath����object��xpath�� strFirstPart������object �� xpath

                    if (strFirstPart != "object"
                        && strFirstPart != "xpath")
                    {
                        strError = "��Դ·�� '" + strResPath + "' ���Ϸ�,��3��������'object'��'xpath'";
                        return -7;
                    }
                    if (strPath == "")  //object��xpath�¼�������ֵ
                    {
                        strError = "��Դ·�� '" + strResPath + "' ���Ϸ�,����3����'object'��'xpath'����4�����������ݡ�";
                        return -7;
                    }

                    if (strFirstPart == "object")
                    {
                        strObjectID = strPath;
                        bObject = true;
                    }
                    else
                    {
                        strXPath = strPath;
                        bObject = false;
                    }


                    //------------------------------------------------
                //��ʼ������Դ
                //---------------------------------------------------

                DOWRITE:

                    // ****************************************


                    string strOutputRecordID = "";
                    nRet = db.CanonicalizeRecordID(strRecordID,
                        out strOutputRecordID,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "��Դ·�� '" + strResPath + "' ���Ϸ���ԭ�򣺼�¼�Ų���Ϊ'" + strRecordID + "'";
                        return -1;
                    }


                    // ************************************
                    // �����¼�ͼ�¼��Ķ���
                    if (bObject == true)  //����
                    {
                        if (strOutputRecordID == "-1")
                        {
                            strError = "��Դ·�� '" + strResPath + "' ���Ϸ�,ԭ�򣺱��������Դʱ,��¼�Ų���Ϊ'" + strRecordID + "'��";
                            return -1;
                        }
                        strRecordID = strOutputRecordID;

                        // return:
                        //		-1  ����
                        //		-2  ʱ�����ƥ��
                        //      -4  ��¼�������Դ������
                        //      -6  Ȩ�޲���
                        //		0   �ɹ�
                        nRet = db.WriteObject(user,
                            strRecordID,
                            strObjectID,
                            strRanges,
                            lTotalLength,
                            baSource,
                            // streamSource,
                            strMetadata,
                            strStyle,
                            baInputTimestamp,
                            out baOutputTimestamp,
                            out strError);

                        strOutputResPath = strDbName + "/" + strRecordID + "/object/" + strObjectID;

                    }
                    else  // ��¼��
                    {
                        strRecordID = strOutputRecordID;

                        string strOutputID = "";
                        // return:
                        //		-1  ����
                        //		-2  ʱ�����ƥ��
                        //      -4  ��¼������
                        //      -6  Ȩ�޲���
                        //		0   �ɹ�
                        nRet = db.WriteXml(user,
                            strRecordID,
                            strXPath,
                            strRanges,
                            lTotalLength,
                            baSource,
                            // streamSource,
                            strMetadata,
                            strStyle,
                            baInputTimestamp,
                            out baOutputTimestamp,
                            out strOutputID,
                            out strOutputValue,
                            true,
                            out strError);

                        strRecordID = strOutputID;

                        if (strXPath == "")
                            strOutputResPath = strDbName + "/" + strRecordID;
                        else
                            strOutputResPath = strDbName + "/" + strRecordID + "/xpath/" + strXPath;

                    }
                }

                // return nRet;
            }
            finally
            {
                //**********�Կ⼯�Ͻ�д��****************
                m_container_lock.ReleaseReaderLock();
#if DEBUG_LOCK
			this.WriteDebugInfo("WriteRes()���Կ⼯�Ͻ������");
#endif
            }

            if (StringUtil.IsInList("flush", strStyle) == true)
            {
                this.Commit();
            }

            return nRet;
        }

        // дĿ¼��������
        // parameters:
        //		strResPath	��Դ·��������
        //					ԭ����û�����������Ϊʲô�����أ�
        //					��Ϊ����ʱ����ԭ·�������Ϊnull����ַ��������Ϊ:����·��/strCfgItemPath
        //		strStyle	��� null��Ϊ�ǿ��ַ���
        //					clear	��ʾ����¼�
        //					autocreatedir	��ʾ�Զ�����ȱʡ��Ŀ¼
        //		user	User���������ж��Ƿ���Ȩ�ޣ�����Ϊnull
        //		strCfgItemPath	��������·������������������Ϊnull����ַ�����???������strResPathһ���ã�������
        //		strError	out���������س�����Ϣ
        // return:
        //      -1  һ���Դ���
        //		-4	δָ��·����Ӧ�Ķ���
        //		-6	Ȩ�޲���
        //		-8	Ŀ¼�Ѵ���
        //		-9	�����������͵�����
        //		0	�ɹ�
        public int WriteDirCfgItem(
            bool bNeedLock,
            string strCfgItemPath,
            string strStyle,
            User user,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(strCfgItemPath) == true)
            {
                strError = "WriteDirCfgItem()�������strCfgItemPath����Ϊnull����ַ�����";
                return -1;
            }

            List<XmlNode> list = DatabaseUtil.GetNodes(this.NodeDbs,
                strCfgItemPath);
            if (list.Count > 1)
            {
                strError = "�������������ļ����Ϸ���·��Ϊ'" + strCfgItemPath + "'�����������Ӧ�Ľڵ���'" + Convert.ToString(list.Count) + "'����";
                return -1;
            }

            string strExistRights = "";
            bool bHasRight = false;

            // �Ѵ���ͬ��������������
            if (list.Count == 1)
            {
                XmlNode node = list[0];
                if (node.Name == "file")
                {
                    strError = "�������Ѵ���·��Ϊ'" + strCfgItemPath + "'�������ļ���������Ŀ¼�����ļ���";
                    return -9;
                }
                if (node.Name == "database")
                {
                    strError = "�������Ѵ�����Ϊ'" + strCfgItemPath + "'�����ݿ⣬������Ŀ¼�������ݿ⡣";
                    return -9;
                }

                if (StringUtil.IsInList("clear", strStyle) == true)
                {
                    // ������������Ѵ��ڣ�������Ƿ���clearȨ��
                    string strPathForRights = strCfgItemPath;
                    bHasRight = user.HasRights(strPathForRights,
                        ResType.Directory,
                        "clear",
                        out strExistRights);
                    if (bHasRight == false)
                    {
                        strError = "�����ʻ���Ϊ'" + user.Name + "'����·��Ϊ'" + strCfgItemPath + "'����������û��'����¼�(clear)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                        return -6;
                    }

                    // ���Ŀ¼
                    // return:
                    //		-1	����
                    //      -4  δָ��·����Ӧ�Ķ���
                    //		0	�ɹ�
                    return this.ClearDirCfgItem(strCfgItemPath,
                        node,
                        out strError);
                }
                else
                {
                    strError = "�������Ѵ���·��Ϊ'" + strCfgItemPath + "'������Ŀ¼��";
                    return -8;
                }
            }


            //***************************************

            bHasRight = user.HasRights(strCfgItemPath,
                ResType.Directory,
                "create",
                out strExistRights);
            if (bHasRight == false)
            {
                strError = "�����ʻ���Ϊ'" + user.Name + "'����·��Ϊ'" + strCfgItemPath + "'����������û��'����¼�(clear)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                return -6;
            }

            // return:
            //		-1	����
            //		0	�ɹ�
            nRet = this.AutoCreateDirCfgItem(
                bNeedLock,
                strCfgItemPath,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }


        // д�ļ���������
        // return:
        //      -1  һ���Դ���
        //      -2  ʱ�����ƥ��
        //      -4  �Զ�����Ŀ¼ʱ��δ�ҵ��ϼ�
        //		-6	Ȩ�޲���
        //		-9	�����������͵�����
        //		0	�ɹ�
        internal int WriteFileCfgItem(
            bool bNeedLock,
            string strCfgItemPath,
            string strRanges,
            long lTotalLength,
            byte[] baSource,
            // Stream streamSource,
            string strMetadata,
            string strStyle,
            byte[] baInputTimestamp,
            User user,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";
            int nRet = 0;

            Debug.Assert(user != null, "WriteFileCfgItem()���ô���user������Ϊnull");

            //------------------------------------------------
            // ���������������淶���������
            //--------------------------------------------------
            if (lTotalLength <= -1)
            {
                strError = "WriteFileCfgItem()���ô���lTotalLengthֵΪ'" + Convert.ToString(lTotalLength) + "'���Ϸ���������ڵ���0��";
                return -1;
            }
            if (strStyle == null)
                strStyle = "";
            if (strRanges == null)
                strRanges = null;
            if (strMetadata == null)
                strMetadata = "";

            /*
            if (baSource == null && streamSource == null)
            {
                strError = "WriteFileCfgItem()���ô���baSource������streamSource��������ͬʱΪnull��";
                return -1;
            }
            if (baSource != null && streamSource != null)
            {
                strError = "WriteFileCfgItem()���ô���baSource������streamSource����ֻ����һ������ֵ��";
                return -1;
            }
             * */
            if (baSource == null)
            {
                strError = "WriteFileCfgItem()���ô���baSource��������Ϊnull��";
                return -1;
            }

            if (strCfgItemPath == null || strCfgItemPath == "")
            {
                strError = "WriteFileCfgItem()���ô���strResPath����Ϊnull����ַ�����";
                return -1;
            }

            //------------------------------------------------
            // ��ʼ������
            //--------------------------------------------------

            List<XmlNode> list = DatabaseUtil.GetNodes(this.NodeDbs,
                strCfgItemPath);
            if (list.Count > 1)
            {
                strError = "�������������ļ����Ϸ���·��Ϊ'" + strCfgItemPath + "'�����������Ӧ�Ľڵ���'" + Convert.ToString(list.Count) + "'����";
                return -1;
            }

            string strExistRights = "";
            bool bHasRight = false;


            //------------------------------------------------
            // �Ѵ���ͬ��������������
            //--------------------------------------------------

            if (list.Count == 1)
            {
                XmlNode node = list[0];
                if (node.Name == "dir")
                {
                    strError = "�������Ѵ���·��Ϊ '" + strCfgItemPath + "' ������Ŀ¼���������ļ�����Ŀ¼��";
                    return -9;
                }
                if (node.Name == "database")
                {
                    strError = "�������Ѵ�����Ϊ '" + strCfgItemPath + "' �����ݿ⣬�������ļ��������ݿ⡣";
                    return -9;
                }

                // ������������Ѵ��ڣ�������Ƿ���overwriteȨ��
                string strPathForRights = strCfgItemPath;
                bHasRight = user.HasRights(strPathForRights,
                    ResType.File,
                    "overwrite",
                    out strExistRights);
                if (bHasRight == false)
                {
                    strError = "�����ʻ���Ϊ'" + user.Name + "'����·��Ϊ'" + strCfgItemPath + "'����������û��'����(overwrite)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                    return -6;
                }

                // �����������������������ļ���
                // ���ڴ�������Ѵ��ڣ���ô�����ļ���һ�����ڣ��������ļ�һ������
                string strLocalPath = "";
                // return:
                //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
                //		-2	û�ҵ��ڵ�
                //		-3	localname����δ�����Ϊֵ��
                //		-4	localname�ڱ��ز�����
                //		-5	���ڶ���ڵ�
                //		0	�ɹ�
                nRet = this.GetFileCfgItemLocalPath(strCfgItemPath,
                    out strLocalPath,
                    out strError);
                if (nRet != 0)
                {
                    if (nRet != -4)
                        return -1;
                }

                goto DOWRITE;
            }


            //------------------------------------------------
            // ������������������
            //--------------------------------------------------


            string strParentCfgItemPath = ""; //���׵�·��
            string strThisCfgItemName = ""; //���������������
            int nIndex = strCfgItemPath.LastIndexOf('/');
            if (nIndex != -1)
            {
                strParentCfgItemPath = strCfgItemPath.Substring(0, nIndex);
                strThisCfgItemName = strCfgItemPath.Substring(nIndex + 1);
            }
            else
            {
                strThisCfgItemName = strCfgItemPath;
            }

            XmlNode nodeParent = null;
            // ���ϼ�·�����м��
            if (strParentCfgItemPath != "")
            {
                List<XmlNode> parentNodes = DatabaseUtil.GetNodes(this.NodeDbs,
                    strParentCfgItemPath);
                if (parentNodes.Count > 1)
                {
                    nIndex = strCfgItemPath.LastIndexOf("/");
                    string strTempParentPath = strCfgItemPath.Substring(0, nIndex);
                    strError = "��������·��Ϊ '" + strTempParentPath + "' ������������'" + Convert.ToString(parentNodes.Count) + "'���������ļ����Ϸ���";
                    return -1;
                }

                if (parentNodes.Count == 1)
                {
                    nodeParent = parentNodes[0];
                }
                else
                {

                    if (StringUtil.IsInList("autocreatedir", strStyle, true) == false)
                    {
                        nIndex = strCfgItemPath.LastIndexOf("/");
                        string strTempParentPath = strCfgItemPath.Substring(0, nIndex);
                        strError = "δ�ҵ�·��Ϊ '" + strTempParentPath + "' ����������޷������¼��ļ���";
                        return -4;
                    }

                    // return:
                    //		-1	����
                    //		0	�ɹ�
                    nRet = this.AutoCreateDirCfgItem(
                        bNeedLock,
                        strParentCfgItemPath,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    parentNodes = DatabaseUtil.GetNodes(this.NodeDbs,
                        strParentCfgItemPath);
                    if (parentNodes.Count != 1)
                    {
                        strError = "WriteFileCfgItem()���Զ��������ϼ�Ŀ¼�ˣ���ʱ�������Ҳ���·��Ϊ'" + strParentCfgItemPath + "'�����������ˡ�";
                        return -1;
                    }

                    nodeParent = parentNodes[0];
                }
            }
            else
            {
                nodeParent = this.NodeDbs;
            }


            // ����ϼ��Ƿ���ָ��Ȩ��
            bHasRight = user.HasRights(strCfgItemPath,
                ResType.File,
                "create",
                out strExistRights);
            if (bHasRight == false)
            {
                strError = "�����ʻ���Ϊ'" + user.Name + "',��'" + strCfgItemPath + "',û��'����(create)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                return -6;
            }


            // return:
            //		-1	����
            //		0	�ɹ�
            nRet = this.SetFileCfgItem(
                bNeedLock,
                strParentCfgItemPath,
                nodeParent,
                strThisCfgItemName,
                out strError);
            if (nRet == -1)
                return -1;


        DOWRITE:

            string strFilePath = "";//GetCfgItemLacalPath(strCfgItemPath);
            // return:
            //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
            //		-2	û�ҵ��ڵ�
            //		-3	localname����δ�����Ϊֵ��
            //		-4	localname�ڱ��ز�����
            //		-5	���ڶ���ڵ�
            //		0	�ɹ�
            nRet = this.GetFileCfgItemLocalPath(strCfgItemPath,
                out strFilePath,
                out strError);
            if (nRet != 0)
            {
                if (nRet != -4)
                    return -1;
            }

            string strTempPath = strCfgItemPath;
            string strFirstPart = StringUtil.GetFirstPartPath(ref strTempPath);
            Database db = this.GetDatabase(strFirstPart);
            if (db != null)
            {

                // return:
                //		-1  һ���Դ���
                //      -2  ʱ�����ƥ��
                //		0	�ɹ�
                return db.WriteFileForCfgItem(
                    bNeedLock,
                    strCfgItemPath,
                    strFilePath,
                     strRanges,
                     lTotalLength,
                     baSource,
                     // streamSource,
                     strMetadata,
                     strStyle,
                     baInputTimestamp,
                     out baOutputTimestamp,
                     out strError);
            }
            else
            {
                // ��������ĳһ�����ݿ�������ļ�
                // return:
                //		-1	һ���Դ���
                //		-2	ʱ�����ƥ��
                //		0	�ɹ�
                return this.WriteFileForCfgItem(strFilePath,
                    strRanges,
                    lTotalLength,
                    baSource,
                    // streamSource,
                    strMetadata,
                    strStyle,
                    baInputTimestamp,
                    out baOutputTimestamp,
                    out strError);
            }
        }

        // Ϊ�ļ���������д�ļ�
        // parameters:
        //		strFilePath Ŀ���ļ�·��������Ϊnull����ַ���
        //		strRanges	������򣬿���Ϊnull��""��ʾ0-sourceBuffer.Length-1������
        //		nTotalLength	�ܳ��ȣ�����Ϊ0
        //		baSource	�����ֽ����飬����Ϊnull
        //		streamSource	������������Ϊnull
        //		strMetadata	Ԫ������Ϣ������Ϊnull��""
        //		inputTimestamp	�����ʱ���������Ϊnull
        //		outputTimestamp	out����������ʵ�ʵ�ʱ���
        //		strError	out���������س�����Ϣ
        // return:
        //		-1	һ���Դ���
        //		-2	ʱ�����ƥ��
        //		0	�ɹ�
        // ��: ����ȫ
        // ˵��: ���ֺ�����ִ�й��̻����ȼ��һ�±����ǲ���һ�η���
        // ȫ�������ݣ�����ǣ���ֱ��дĿ���ļ�������ʹ����ʱ�ļ�
        // ������ǲ�ʹ����ʱ�ļ��������ж�ranges�Ƿ�������������Ӧ�Ĵ���
        // Ҳ�п������½�һ���ļ�
        internal int WriteFileForCfgItem(string strFilePath,
            string strRanges,
            long lTotalLength,
            byte[] baSource,
            // Stream streamSource,
            string strMetadata,
            string strStyle,
            byte[] baInputTimestamp,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";

            // --------------------------------------------------------
            // ���������������淶���������
            // --------------------------------------------------------
            if (String.IsNullOrEmpty(strFilePath) == true)
            {
                strError = "WriteFileForCfgItem()���ô���strFilePath��������Ϊ��";
                return -1;
            }
            if (lTotalLength <= -1)
            {
                strError = "WriteFileForCfgItem()���ô���lTotalLength������ֵ����Ϊ '" + Convert.ToString(lTotalLength) + "', ������ڵ���0";
                return -1;
            }

            if (strStyle == null)
                strStyle = "";
            if (strMetadata == null)
                strMetadata = "";

            /*
            if (baSource == null && streamSource == null)
            {
                strError = "WriteFileForCfgItem()���ô���baSource������streamSource��������ͬʱΪnull��";
                return -1;
            }
            if (baSource != null && streamSource != null)
            {
                strError = "WriteFileForCfgItem()���ô���baSource������streamSource����ֻ����һ������ֵ��";
                return -1;
            }
             * */
            if (baSource == null)
            {
                strError = "WriteFileForCfgItem()���ô���baSource��������Ϊnull��";
                return -1;
            }


            // --------------------------------------------------------
            // ���������������淶���������
            // --------------------------------------------------------

            string strNewFilePath = DatabaseUtil.GetNewFileName(strFilePath);

            //*************************************************
            // ���ʱ���,���е������ļ�����ʱ
            if (File.Exists(strFilePath) == true
                || File.Exists(strNewFilePath) == true)
            {
                if (StringUtil.IsInList("ignorechecktimestamp", strStyle) == false)
                {
                    if (File.Exists(strNewFilePath) == true)
                        baOutputTimestamp = DatabaseUtil.CreateTimestampForCfg(strNewFilePath);
                    else
                        baOutputTimestamp = DatabaseUtil.CreateTimestampForCfg(strFilePath);
                    if (ByteArray.Compare(baOutputTimestamp, baInputTimestamp) != 0)
                    {
                        strError = "ʱ�����ƥ��";
                        return -2;
                    }
                }
            }
            else
            {
                FileStream s = File.Create(strFilePath);
                s.Close();
                baOutputTimestamp = DatabaseUtil.CreateTimestampForCfg(strFilePath);
            }


            //**************************************************
            long lCurrentLength = 0;

            //if (lTotalLength == 0)
            //	goto END1;

            /*
            if (baSource != null)
             * */
            {
                if (baSource.Length == 0)
                {
                    if (strRanges != "")
                    {
                        strError = "WriteCfgFileByRange()����baSource�����ĳ���Ϊ0ʱ��strRanges��ֵȴΪ'" + strRanges + "'����ƥ�䣬ӦΪ���ַ�����";
                        return -1;
                    }
                    //��д��metadata��ĳߴ����
                    FileInfo fi = new FileInfo(strFilePath);
                    lCurrentLength = fi.Length;
                    fi = null;

                    //goto END1;
                }
            }
            /*
            else
            {
                if (streamSource.Length == 0)
                {
                    if (strRanges != "")
                    {
                        strError = "WriteCfgFileByRange()����streamSource��������Ϊ0ʱ��strRanges��ֵȴΪ'" + strRanges + "'����ƥ�䣬ӦΪ���ַ�����";
                        return -1;
                    }
                    //��д��metadata��ĳߴ����
                    FileInfo fi = new FileInfo(strFilePath);
                    lCurrentLength = fi.Length;
                    fi = null;

                    //goto END1;
                }
            }
             * */

            //******************************************
            // д����
            if (string.IsNullOrEmpty(strRanges) == true)
            {
                if (lTotalLength > 0)
                    strRanges = "0-" + Convert.ToString(lTotalLength - 1);
                else
                    strRanges = "";
            }
            string strRealRanges = strRanges;

            // ��鱾�δ����ķ�Χ�Ƿ����������ļ���
            bool bIsComplete = false;
            if (lTotalLength == 0)
                bIsComplete = true;
            else
            {
                //		-1	���� 
                //		0	����δ���ǵĲ��� 
                //		1	�����Ѿ���ȫ����
                int nState = RangeList.MergeContentRangeString(strRanges,
                    "",
                    lTotalLength,
                    out strRealRanges,
                    out strError);
                if (nState == -1)
                {
                    strError = "MergeContentRangeString() error 1 : " + strError + " (strRanges='" + strRanges + "' lTotalLength=" + lTotalLength.ToString() + ")";
                    return -1;
                }
                if (nState == 1)
                    bIsComplete = true;
            }


            if (bIsComplete == true)
            {
                /*
                if (baSource != null)
                 * */
                {
                    if (baSource.Length != lTotalLength)
                    {
                        strError = "��Χ'" + strRanges + "'�������ֽ����鳤��'" + Convert.ToString(baSource.Length) + "'�����ϡ�";
                        return -1;
                    }
                }
                /*
                else
                {
                    if (streamSource.Length != lTotalLength)
                    {
                        strError = "��Χ'" + strRanges + "'��������'" + Convert.ToString(streamSource.Length) + "'�����ϡ�";
                        return -1;
                    }
                }
                 * */
            }


            RangeList rangeList = new RangeList(strRealRanges);

            // ��ʼд����
            Stream target = null;
            if (bIsComplete == true)
                target = File.Create(strFilePath);  //һ���Է��ֱ꣬��д���ļ�
            else
                target = File.Open(strNewFilePath, FileMode.OpenOrCreate);
            try
            {
                int nStartOfBuffer = 0;
                for (int i = 0; i < rangeList.Count; i++)
                {
                    RangeItem range = (RangeItem)rangeList[i];
                    // int nStartOfTarget = (int)range.lStart;
                    int nLength = (int)range.lLength;
                    if (nLength == 0)
                        continue;

                    // �ƶ�Ŀ������ָ�뵽ָ��λ��
                    target.Seek(range.lStart,   // nStartOfTarget,
                        SeekOrigin.Begin);

                    /*
                    if (baSource != null)
                     * */
                    {
                        target.Write(baSource,
                            nStartOfBuffer,
                            nLength);


                        nStartOfBuffer += nLength; //2005.11.11��
                    }
                    /*
                    else
                    {
                        StreamUtil.DumpStream(streamSource,
                            target,
                            nLength);
                    }
                     * */
                }
            }
            finally
            {
                target.Close();
            }

            string strRangeFileName = DatabaseUtil.GetRangeFileName(strFilePath);

            // ���һ����д�����������Ҫ�����м�������:
            // 1.ʱ�����Ŀ���ļ�����
            // 2.д��metadata�ĳ���ΪĿ���ļ��ܳ���
            // 3.���������ʱ�����ļ�����ɾ����Щ�ļ���
            if (bIsComplete == true)
            {
                baOutputTimestamp = DatabaseUtil.CreateTimestampForCfg(strFilePath);
                lCurrentLength = lTotalLength;

                // ɾ�������ļ�
                if (File.Exists(strNewFilePath) == true)
                    File.Delete(strNewFilePath);
                if (File.Exists(strRangeFileName) == true)
                    File.Delete(strRangeFileName);

                goto END1;
            }


            //****************************************
            //�������ļ�
            bool bFull = false;
            string strResultRange = "";
            if (strRanges == "" || strRanges == null)
            {
                bFull = true;
            }
            else
            {
                string strOldRanges = "";
                if (File.Exists(strRangeFileName) == true)
                    strOldRanges = FileUtil.File2StringE(strRangeFileName);
                int nState1 = RangeList.MergeContentRangeString(strRanges,
                    strOldRanges,
                    lTotalLength,
                    out strResultRange,
                    out strError);
                if (nState1 == -1)
                {
                    strError = "MergeContentRangeString() error 2 : " + strError + " (strRanges='" + strRanges + "' strOldRanges='" + strOldRanges + "' ) lTotalLength=" + lTotalLength.ToString() + "";
                    return -1;
                }
                if (nState1 == 1)
                    bFull = true;
            }

            // ����ļ���������Ҫ�����м�������:
            // 1.����󳤶Ƚ���ʱ�ļ� 
            // 2.����ʱ�ļ�����Ŀ���ļ�
            // 3.ɾ��new,range�����ļ�
            // 4.ʱ�����Ŀ���ļ�����
            // 5.metadata�ĳ���ΪĿ���ļ����ܳ���
            if (bFull == true)
            {
                Stream s = new FileStream(strNewFilePath,
                    FileMode.OpenOrCreate);
                try
                {
                    s.SetLength(lTotalLength);
                }
                finally
                {
                    s.Close();
                }

                // ��.new��ʱ�ļ��滻ֱ���ļ�
                File.Copy(strNewFilePath,
                    strFilePath,
                    true);

                File.Delete(strNewFilePath);

                if (File.Exists(strRangeFileName) == true)
                    File.Delete(strRangeFileName);
                baOutputTimestamp = DatabaseUtil.CreateTimestampForCfg(strFilePath);

                lCurrentLength = lTotalLength;
            }
            else
            {

                //����ļ�δ������Ҫ�����м������飺
                // 1.��Ŀǰ��rangeд��range�����ļ�
                // 2.ʱ�������ʱ�ļ�����
                // 3.metadata�ĳ���Ϊ-1����δ֪�����

                FileUtil.String2File(strResultRange,
                    strRangeFileName);

                lCurrentLength = -1;

                baOutputTimestamp = DatabaseUtil.CreateTimestampForCfg(strNewFilePath);
            }

        END1:

            // дmetadata
            if (strMetadata != "")
            {
                string strMetadataFileName = DatabaseUtil.GetMetadataFileName(strFilePath);

                // ȡ���ɵ����ݽ��кϲ�
                string strOldMetadata = "";
                if (File.Exists(strMetadataFileName) == true)
                    strOldMetadata = FileUtil.File2StringE(strMetadataFileName);
                if (strOldMetadata == "")
                    strOldMetadata = "<file/>";

                string strResultMetadata;
                // return:
                //		-1	����
                //		0	�ɹ�
                int nRet = DatabaseUtil.MergeMetadata(strOldMetadata,
                    strMetadata,
                    lCurrentLength,
                    out strResultMetadata,
                    out strError);
                if (nRet == -1)
                    return -1;

                // �Ѻϲ���������д���ļ���
                FileUtil.String2File(strResultMetadata,
                    strMetadataFileName);
            }
            return 0;
        }


        // GetRes()��range��̫��ʵ��,��Ϊԭ��������ĳ��ȳ�������ĳ���ʱ,���Ȼ��Զ�Ϊ��ȡ
        // �������range����ʾ,��֪�ýض��Ĳ��ֺá�
        // parameter:
        //		strResPath		��Դ·��,����Ϊnull����ַ���
        //						��Դ���Ϳ��������ݿ���������(Ŀ¼���ļ�)����¼�壬������Դ�����ּ�¼��
        //						��������: ����/��������·��
        //						��¼��: ����/��¼��
        //						������Դ: ����/��¼��/object/��ԴID
        //						���ּ�¼��: ����/��¼/xpath/<locate>hitcount</locate><action>AddInteger</action> ���� ����/��¼/xpath/@hitcount
        //		lStart	��ʼ����
        //		lLength	�ܳ���,-1:��start�����
        //		strStyle	ȡ��Դ�ķ���Զ���������ַ���
        /*
        strStyle�÷�

        1.�������ݴ�ŵ�λ��
        content		�ѷ��ص����ݷŵ��ֽ����������
        attachment	�ѷ��ص����ݷŵ�������,�����ظ�����id

        2.���Ʒ��ص�����
        metadata	����metadata��Ϣ
        timestamp	����timestamp
        length		�����ܳ��ȣ�ʼ�ն���ֵ
        data		����������
        respath		���ؼ�¼·��,Ŀǰʼ�ն���ֵ
        all			��������ֵ

        3.���Ƽ�¼��
        prev		ǰһ��
        prev,myself	�Լ���ǰһ��
        next		��һ��
        next,myself	�Լ�����һ��
        �ŵ�strOutputResPath������

        */
        //		baContent	��content�ֽ����鷵����Դ����
        //		strAttachmentID	�ø���������Դ����
        //		strMetadata	���ص�metadata����
        //		strOutputResPath	���ص���Դ·��
        //		baTimestamp	���ص���Դʱ���
        // return:
        //		-1	һ���Դ���
        //		-4	δ�ҵ�·��ָ������Դ
        //		-5	δ�ҵ����ݿ�
        //		-6	û���㹻��Ȩ��
        //		-7	·�����Ϸ�
        //		-10	δ�ҵ���¼xpath��Ӧ�Ľڵ�
        //		>= 0	�ɹ���������󳤶�
        //      nAdditionError -50 ��һ�������¼���Դ��¼������
        // �ߣ���ȫ
        public long API_GetRes(string strResPath,
            long lStart,
            int nLength,
            string strStyle,
            User user,
            int nMaxLength,
            out byte[] baData,
            out string strMetadata,
            out string strOutputResPath,
            out byte[] baOutputTimestamp,
            out int nAdditionError, // ���ӵĴ�����
            out string strError)
        {
            baData = null;
            strMetadata = "";
            strOutputResPath = "";
            baOutputTimestamp = null;
            strError = "";
            nAdditionError = 0;

            //------------------------------------------------
            //�����������Ƿ�Ϸ������淶�������
            //---------------------------------------------------

            Debug.Assert(user != null, "GetRes()���ô���user������Ϊnull��");

            if (user == null)
            {
                strError = "GetRes()���ô���user������Ϊnull��";
                return -1;
            }
            if (String.IsNullOrEmpty(strResPath) == true)
            {
                strError = "��Դ·��'" + strResPath + "'���Ϸ�������Ϊnull����ַ�����";
                return -7;
            }
            if (lStart < 0)
            {
                strError = "GetRes()���ô���lStart����С��0��";
                return -1;
            }
            if (strStyle == null)
                strStyle = "";


            //------------------------------------------------
            // ��ʼ������
            //---------------------------------------------------

            //******************�ӿ⼯�ϼӶ���******
            this.m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);

#if DEBUG_LOCK
			this.WriteDebugInfo("GetRes()���Կ⼯�ϼӶ�����");
#endif
            try
            {
                long lRet = 0;

                bool bRecordPath = this.IsRecordPath(strResPath);
                if (bRecordPath == false)
                {
                    //�����������
                    // return:
                    //		-1  һ���Դ���
                    //		-4	δ�ҵ�·����Ӧ�Ķ���
                    //		-6	û���㹻��Ȩ��
                    //		>= 0    �ɹ� ������󳤶�
                    lRet = this.GetFileCfgItem(
                        false,
                        strResPath,
                        lStart,
                        nLength,
                        nMaxLength,
                        strStyle,
                        user,
                        out baData,
                        out strMetadata,
                        out baOutputTimestamp,
                        out strError);


                    if (StringUtil.IsInList("outputpath", strStyle) == true)
                    {
                        strOutputResPath = strResPath;
                    }
                }
                else
                {

                    // �ж���Դ����
                    string strPath = strResPath;
                    string strDbName = StringUtil.GetFirstPartPath(ref strPath);
                    //***********�Ե���1��*************
                    // ����Ϊֹ��strPath�������ݿ�����,�����·�����������:cfgs;���඼��������¼id
                    if (strPath == "")
                    {
                        strError = "��Դ·��'" + strResPath + "'·�����Ϸ���δָ������¼���";
                        return -7;
                    }

                    // ���������������ݿ⻹�Ƿ������������ļ�

                    // ������Դ���ͣ�д��Դ
                    Database db = this.GetDatabase(strDbName);
                    if (db == null)
                    {
                        strError = "δ�ҵ�'" + strDbName + "'��";
                        return -5;
                    }

                    bool bObject = false;
                    string strRecordID = "";
                    string strObjectID = "";
                    string strXPath = "";

                    string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    //***********�Ե���2��*************
                    // ����Ϊֹ��strPath��¼�Ų��ˣ��¼�������ж�

                    strRecordID = strFirstPart;
                    // ֻ����¼�Ų��·��
                    if (strPath == "")
                    {
                        bObject = false;
                        goto DOGET;
                    }

                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    //***********�Ե���2��*************
                    // ����Ϊֹ��strPath����object��xpath�� strFirstPart������object �� xpath
                    if (strFirstPart != "object"
                        && strFirstPart != "xpath")
                    {
                        strError = "��Դ·�� '" + strResPath + "' ���Ϸ�,��3��������'object'��'xpath'";
                        return -7;
                    }
                    if (strPath == "")  //object��xpath�¼�������ֵ
                    {
                        strError = "��Դ·�� '" + strResPath + "' ���Ϸ�,����3����'object'��'xpath'����4�����������ݡ�";
                        return -7;
                    }

                    if (strFirstPart == "object")
                    {
                        strObjectID = strPath;
                        bObject = true;
                    }
                    else
                    {
                        strXPath = strPath;
                        bObject = false;
                    }

                    ///////////////////////////////////
                ///��ʼ������
                //////////////////////////////////////////

                DOGET:


                    // �������ݿ��м�¼��Ȩ��
                    string strExistRights = "";
                    bool bHasRight = user.HasRights(strDbName + "/" + strRecordID,
                        ResType.Record,
                        "read",
                        out strExistRights);
                    if (bHasRight == false)
                    {
                        strError = "�����ʻ���Ϊ'" + user.Name + "'����'" + strDbName + "'��û��'����¼(read)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                        return -6;
                    }

                    if (bObject == true)  // ����
                    {
                        //		-1  ����
                        //		-4  ��¼������
                        //		>=0 ��Դ�ܳ���
                        lRet = db.GetObject(strRecordID,
                            strObjectID,
                            lStart,
                            nLength,
                            nMaxLength,
                            strStyle,
                            out baData,
                            out strMetadata,
                            out baOutputTimestamp,
                            out strError);

                        if (StringUtil.IsInList("outputpath", strStyle) == true)
                        {
                            strOutputResPath = strDbName + "/" + strRecordID + "/object/" + strObjectID;

                        }
                    }
                    else
                    {
                        string strOutputID;
                        // return:
                        //		-1  ����
                        //		-4  δ�ҵ���¼
                        //      -10 ��¼�ֲ�δ�ҵ�
                        //		>=0 ��Դ�ܳ���
                        //      nAdditionError -50 ��һ�������¼���Դ��¼������
                        lRet = db.GetXml(strRecordID,
                            strXPath,
                            lStart,
                            nLength,
                            nMaxLength,
                            strStyle,
                            out baData,
                            out strMetadata,
                            out strOutputID,
                            out baOutputTimestamp,
                            true,
                            out nAdditionError,
                            out strError);
                        if (StringUtil.IsInList("outputpath", strStyle) == true)
                        {
                            strRecordID = strOutputID;
                        }

                        if (StringUtil.IsInList("outputpath", strStyle) == true)
                        {
                            if (strXPath == "")
                                strOutputResPath = strDbName + "/" + strRecordID;
                            else
                                strOutputResPath = strDbName + "/" + strRecordID + "/xpath/" + strXPath;

                        }
                    }
                }

                return lRet;
            }
            finally
            {
                //******************�Կ⼯�Ͻ����******
                this.m_container_lock.ReleaseReaderLock();
#if DEBUG_LOCK
			this.WriteDebugInfo("GetRes()���Կ⼯�Ͻ������");
#endif
            }
        }

        // ���һ��·���Ƿ������ݿ��¼·��
        private bool IsRecordPath(string strResPath)
        {
            string[] paths = strResPath.Split(new char[] { '/' });
            if (paths.Length >= 2)
            {
                if (StringUtil.IsPureNumber(paths[1]) == true
                    || paths[1] == "?"
                    || paths[1] == "-1")
                {
                    return true;
                }
            }
            return false;
        }


        // ��ָ����Χ�������ļ�
        // strRoleName:  ��ɫ��,��Сд����
        // ��������ͬGetXml(),��strOutputResPath����
        // ��: ��ȫ��
        // return:
        //		-1  һ���Դ���
        //		-4	δ�ҵ�·����Ӧ�Ķ���
        //		-6	û���㹻��Ȩ��
        //		>= 0    �ɹ� ������󳤶�
        // �ߣ���ȫ
        public long GetFileCfgItem(
            bool bNeedLock,
            string strCfgItemPath,
            long lStart,
            int nLength,
            int nMaxLength,
            string strStyle,
            User user,
            out byte[] destBuffer,
            out string strMetadata,
            out byte[] outputTimestamp,
            out string strError)
        {
            strMetadata = "";
            destBuffer = null;
            outputTimestamp = null;
            strError = "";

            // ��鵱ǰ�ʻ������������Ȩ�ޣ���ʱ����Ȩ�޵Ĵ����������Ƿ���ڣ��ٱ���
            string strExistRights = "";
            bool bHasRight = user.HasRights(strCfgItemPath,
                ResType.File,
                "read",
                out strExistRights);


            if (bNeedLock == true)
            {
                //**********�����ݿ⼯�ϼӶ���**************
                this.m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
                this.WriteDebugInfo("GetCfgFile()�������ݿ⼯�ϼӶ�����");
#endif
            }

            try
            {

                string strFilePath = "";//this.GetCfgItemLacalPath(strCfgItemPath);
                // return:
                //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
                //		-2	û�ҵ��ڵ�
                //		-3	localname����δ�����Ϊֵ��
                //		-4	localname�ڱ��ز�����
                //		-5	���ڶ���ڵ�
                //		0	�ɹ�
                int nRet = this.GetFileCfgItemLocalPath(strCfgItemPath,
                    out strFilePath,
                    out strError);
                if (nRet != 0)
                {
                    if (nRet == -2)
                        return -4;
                    return -1;
                }

                // ��ʱ�ٱ�Ȩ�޵Ĵ�
                if (bHasRight == false)
                {
                    strError = "�����ʻ���Ϊ'" + user.Name + "'����·��Ϊ'" + strCfgItemPath + "'����������û��'��(read)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                    return -6;
                }

                // return:
                //		-1      ����
                //		>= 0	�ɹ���������󳤶�
                return DatabaseCollection.GetFileForCfgItem(strFilePath,
                    lStart,
                    nLength,
                    nMaxLength,
                    strStyle,
                    out destBuffer,
                    out strMetadata,
                    out outputTimestamp,
                    out strError);
            }
            finally
            {
                if (bNeedLock == true)
                {
                    //****************�����ݿ⼯�Ͻ����**************
                    this.m_container_lock.ReleaseReaderLock();
#if DEBUG_LOCK
                    this.WriteDebugInfo("GetCfgFile()�������ݿ⼯�Ͻ������");
#endif
                }
            }
        }

        // ΪGetCfgItem���������ڲ�����
        // return:
        //		-1      ����
        //		>= 0	�ɹ���������󳤶�
        public static long GetFileForCfgItem(string strFilePath,
            long lStart,
            int nLength,
            int nMaxLength,
            string strStyle,
            out byte[] destBuffer,
            out string strMetadata,
            out byte[] outputTimestamp,
            out string strError)
        {
            destBuffer = null;
            strMetadata = "";
            outputTimestamp = null;
            strError = "";

            long lTotalLength = 0;
            FileInfo file = new FileInfo(strFilePath);
            if (file.Exists == false)
            {
                strError = "����������������·��Ϊ'" + strFilePath + "'���ļ���";
                return -1;
            }

            // 1.ȡʱ���
            if (StringUtil.IsInList("timestamp", strStyle) == true)
            {
                string strNewFileName = DatabaseUtil.GetNewFileName(strFilePath);
                if (File.Exists(strNewFileName) == true)
                {
                    outputTimestamp = DatabaseUtil.CreateTimestampForCfg(strNewFileName);
                }
                else
                {
                    outputTimestamp = DatabaseUtil.CreateTimestampForCfg(strFilePath);
                }
            }

            // 2.ȡԪ����
            if (StringUtil.IsInList("metadata", strStyle) == true)
            {
                string strMetadataFileName = DatabaseUtil.GetMetadataFileName(strFilePath);
                if (File.Exists(strMetadataFileName) == true)
                {
                    strMetadata = FileUtil.File2StringE(strMetadataFileName);
                }
            }

            // 3.ȡrange
            if (StringUtil.IsInList("range", strStyle) == true)
            {
                string strRangeFileName = DatabaseUtil.GetRangeFileName(strFilePath);
                if (File.Exists(strRangeFileName) == true)
                {
                    string strRange = FileUtil.File2StringE(strRangeFileName);
                }
            }

            // 4.����
            lTotalLength = file.Length;

            // 5.��data���ʱ,�Ż�ȡ����
            if (StringUtil.IsInList("data", strStyle) == true)
            {
                if (nLength == 0)  // ȡ0����
                {
                    destBuffer = new byte[0];
                    return lTotalLength;
                }
                // ��鷶Χ�Ƿ�Ϸ�
                long lOutputLength;
                // return:
                //		-1  ����
                //		0   �ɹ�
                int nRet = ConvertUtil.GetRealLength(lStart,
                    nLength,
                    lTotalLength,
                    nMaxLength,
                    out lOutputLength,
                    out strError);
                if (nRet == -1)
                    return -1;

                FileStream s = new FileStream(strFilePath,
                    FileMode.Open);
                try
                {
                    destBuffer = new byte[lOutputLength];
                    s.Seek(lStart, SeekOrigin.Begin);
                    s.Read(destBuffer,
                        0,
                        (int)lOutputLength);
                }
                finally
                {
                    s.Close();
                }
            }
            return lTotalLength;
        }

        // �õ�һ���ļ���������ı����ļ�����·��
        // parameters:
        //		strFileCfgItemPath	�ļ����������·������ʽΪ'dir1/dir2/file'
        //		strLocalPath	out���������ض�Ӧ�ı����ļ�����·��	
        //		strError	out���������س�����Ϣ
        // return:
        //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
        //		-2	û�ҵ��ڵ�
        //		-3	localname����δ�����Ϊֵ��
        //		-4	localname�ڱ��ز�����
        //		-5	���ڶ���ڵ�
        //		0	�ɹ�
        // �ߣ�����ȫ
        public int GetFileCfgItemLocalPath(string strFileCfgItemPath,
            out string strLocalPath,
            out string strError)
        {
            strLocalPath = "";
            strError = "";

            if (strFileCfgItemPath == ""
                || strFileCfgItemPath == null)
            {
                strError = "GetCfgItemLacalPath()��strPath����ֵ����Ϊnull����ַ���";
                return -1;
            }
            List<XmlNode> nodes = DatabaseUtil.GetNodes(this.NodeDbs,
                strFileCfgItemPath);
            if (nodes.Count == 0)
            {
                strError = "dp2Kernel ��������δ����·��Ϊ '" + strFileCfgItemPath + "' �������ļ�";
                return -2;
            }
            if (nodes.Count > 1)
            {
                strError = "dp2Kernel ��������·��Ϊ '" + strFileCfgItemPath + "' ������������ " + Convert.ToString(nodes.Count) + " ���������ļ����Ϸ�";
                return -5;
            }

            XmlNode nodeFile = nodes[0];

            string strPureFileName = DomUtil.GetAttr(nodeFile, "localname");
            if (strPureFileName == "")
            {
                strError = "dp2Kernel ��������·��Ϊ '" + strFileCfgItemPath + "' ���ļ���������δ�����Ӧ�������ļ�";
                return -3;
            }

            string strLocalDir = DatabaseUtil.GetLocalDir(this.NodeDbs,
                nodeFile.ParentNode);

            string strRealPath = "";
            if (strLocalDir == "")
                strRealPath = this.DataDir + "\\" + strPureFileName;
            else
                strRealPath = this.DataDir + "\\" + strLocalDir + "\\" + strPureFileName;

            strLocalPath = strRealPath;
            if (File.Exists(strRealPath) == false)
            {
                strError = "dp2Kernel ��������·��Ϊ '" + strFileCfgItemPath + "' ���ļ����������Ӧ�������ļ��ڱ��ز�����";
                return -4;
            }
            return 0;
        }


        // ɾ����Դ�������Ǽ�¼ �� ���������֧�ֶ�����Դ�򲿷ּ�¼��
        // parameter:
        //		strResPath		��Դ·��,����Ϊnull����ַ���
        //						��Դ���Ϳ��������ݿ���������(Ŀ¼���ļ�)����¼
        //						��������: ����/��������·��
        //						��¼: ����/��¼��
        //		user	��ǰ�ʻ����󣬲���Ϊnull
        //		baInputTimestamp	�����ʱ���
        //		baOutputTimestamp	out����������ʱ���
        //		strError	out���������س�����Ϣ
        // return:
        //      -1	һ���Դ�����������������Ϸ���
        //      -2	ʱ�����ƥ��
        //      -4	δ�ҵ�·����Ӧ����Դ
        //      -5	δ�ҵ����ݿ�
        //      -6	û���㹻��Ȩ��
        //      -7	·�����Ϸ�
        //      0	�����ɹ�
        // ˵��: 
        // 1)ɾ����Ҫ��ǰ�ʻ��Խ���ɾ���ļ�¼����deleteȨ��		
        // 2)ɾ����¼����ȷ������ɾ����¼�壬����ɾ���ü�¼���������ж�����Դ
        // 3)ɾ������Ŀ¼��Ҫ��ʱ���,ͬʱbaOutputTimestampҲ��null
        // ����Ҫ�Ӷ���
        public int API_DeleteRes(string strResPath,
            User user,
            byte[] baInputTimestamp,
            string strStyle,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";

            //-----------------------------------------
            //��������������м��
            //---------------------------------------
            if (strResPath == null || strResPath == "")
            {
                strError = "DeleteRes()���ô���strResPath��������Ϊnull����ַ�����";
                return -1;
            }
            if (user == null)
            {
                strError = "DeleteRes()���ô���user��������Ϊnull��";
                return -1;
            }


            //---------------------------------------
            //��ʼ������ 
            //---------------------------------------

            //******************�ӿ⼯�ϼӶ���******
            this.m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);

#if DEBUG_LOCK
            this.WriteDebugInfo("API_DeleteRes()���Կ⼯�ϼӶ�����");
#endif
            try
            {
                int nRet = 0;

                bool bRecordPath = this.IsRecordPath(strResPath);
                if (bRecordPath == false)
                {
                    // Ҳ���������ݿ����


                    // ɾ��ʵ�ʵ������ļ�
                    //      -1  һ���Դ���
                    //      -2  ʱ�����ƥ��
                    //      -4  δ�ҵ�·����Ӧ����Դ
                    //      -6  û���㹻��Ȩ��
                    //      0   �ɹ�
                    nRet = this.DeleteCfgItem(user,
                        strResPath,
                        baInputTimestamp,
                        out baOutputTimestamp,
                        out strError);
                    if (nRet <= -1)
                        return nRet;

                    goto CHECK_CHANGED;
                }
                else
                {

                    string strPath = strResPath;
                    string strDbName = StringUtil.GetFirstPartPath(ref strPath);
                    if (strPath == "")
                    {
                        strError = "��Դ·��'" + strResPath + "'���Ϸ���δָ������¼���";
                        return -7;
                    }

                    // ������Դ���ͣ�д��Դ
                    Database db = this.GetDatabase(strDbName);
                    if (db == null)
                    {
                        strError = "û�ҵ���Ϊ'" + strDbName + "'�����ݿ⡣";
                        return -5;
                    }

                    string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    //***********�Ե���2��*************
                    // ����Ϊֹ��strPath����cfgs���¼�Ų��ˣ��¼�������ж�
                    // strFirstPart������Ϊcfg���¼��

                    string strRecordID = strFirstPart;

                    // ��鵱ǰ�ʻ��Ƿ���ɾ����¼
                    string strExistRights = "";
                    bool bHasRight = user.HasRights(strResPath,//db.GetCaption("zh-CN"),
                        ResType.Record,
                        "delete",
                        out strExistRights);
                    if (bHasRight == false)
                    {
                        strError = "�����ʻ���Ϊ'" + user.Name + "'����'" + strDbName + "'���ݿ�û��'ɾ����¼(delete)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                        return -6;
                    }

                    // return:
                    //		-1  һ���Դ���
                    //		-2  ʱ�����ƥ��
                    //      -4  δ�ҵ���¼
                    //		0   �ɹ�
                    nRet = db.DeleteRecord(strRecordID,
                        baInputTimestamp,
                        strStyle,
                        out baOutputTimestamp,
                        out strError);
                    if (nRet <= -1)
                        return nRet;

                    return 0;
                }
            }
            finally
            {
                m_container_lock.ReleaseReaderLock();
                //*************�Կ⼯�Ͻ����***********
#if DEBUG_LOCK
                this.WriteDebugInfo("API_DeleteRes()���Կ⼯�Ͻ������");
#endif
            }

        CHECK_CHANGED:
            //��ʱ����database.xml // ���ü����ĺ�����
            if (this.Changed == true)
                this.SaveXmlSafety(true);

            return 0;
        }

        // �ؽ���¼��keys
        // parameter:
        //		strResPath		��Դ·��,����Ϊnull����ַ���
        //						��¼: ����/��¼��
        //		user	��ǰ�ʻ����󣬲���Ϊnull
        //		strError	out���������س�����Ϣ
        // return:
        //      -1	һ���Դ�����������������Ϸ���
        //      -2	ʱ�����ƥ��
        //      -4	δ�ҵ�·����Ӧ����Դ
        //      -5	δ�ҵ����ݿ�
        //      -6	û���㹻��Ȩ��
        //      -7	·�����Ϸ�
        //      0	�����ɹ�
        // ˵��: 
        // 1)ɾ����Ҫ��ǰ�ʻ��Խ���ɾ���ļ�¼����overwriteȨ��		
        // ����Ҫ�Ӷ���
        public int API_RebuildResKeys(string strResPath,
            User user,
            string strStyle,
            out string strOutputResPath,
            out string strError)
        {
            strError = "";
            strOutputResPath = "";

            //-----------------------------------------
            //��������������м��
            //---------------------------------------
            if (String.IsNullOrEmpty(strResPath) == true)
            {
                strError = "RebuildResKeys()���ô���strResPath��������Ϊnull����ַ�����";
                return -1;
            }

            if (user == null)
            {
                strError = "RebuildResKeys()���ô���user��������Ϊnull��";
                return -1;
            }

            if (strStyle == null)
                strStyle = "";


            //-----------------------------------------
            //��ʼ������ 
            //---------------------------------------

            //******************�ӿ⼯�ϼӶ���******
            this.m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);

#if DEBUG_LOCK
            this.WriteDebugInfo("API_RebuildResKeys()���Կ⼯�ϼӶ�����");
#endif
            try
            {
                int nRet = 0;

                bool bRecordPath = this.IsRecordPath(strResPath);
                if (bRecordPath == false)
                {
                    strError = "��֧�ֶ� '" + strResPath + "' ������ؽ�keys����";
                    return -1;
                    // Ҳ���������ݿ����
                }
                
                {

                    string strPath = strResPath;
                    string strDbName = StringUtil.GetFirstPartPath(ref strPath);
                    if (strPath == "")
                    {
                        strError = "��Դ·��'" + strResPath + "'���Ϸ���δָ������¼���";
                        return -7;
                    }

                    // ������Դ���ͣ�д��Դ
                    Database db = this.GetDatabase(strDbName);
                    if (db == null)
                    {
                        strError = "û�ҵ���Ϊ'" + strDbName + "'�����ݿ⡣";
                        return -5;
                    }

                    string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    //***********�Ե���2��*************
                    // ����Ϊֹ��strPath����cfgs���¼�Ų��ˣ��¼�������ж�
                    // strFirstPart������Ϊcfg���¼��

                    string strRecordID = strFirstPart;

                    // ��鵱ǰ�ʻ��Ƿ���ɾ����¼
                    string strExistRights = "";
                    bool bHasRight = user.HasRights(strResPath,//db.GetCaption("zh-CN"),
                        ResType.Record,
                        "overwrite",
                        out strExistRights);
                    if (bHasRight == false)
                    {
                        strError = "�����ʻ���Ϊ'" + user.Name + "'����'" + strDbName + "'���ݿ�û��'��д��¼(overwrite)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                        return -6;
                    }

                    string strOutputID = "";
                    // return:
                    //		-1  һ���Դ���
                    //		-2  ʱ�����ƥ��
                    //      -4  δ�ҵ���¼
                    //		0   �ɹ�
                    nRet = db.RebuildRecordKeys(strRecordID,
                        strStyle,
                        out strOutputID,
                        out strError);

                    if (StringUtil.IsInList("outputpath", strStyle) == true)
                    {
                        strOutputResPath = strDbName + "/" + strOutputID;
                    }

                    if (nRet <= -1)
                        return nRet;
                }
            }
            finally
            {
                m_container_lock.ReleaseReaderLock();
                //*************�Կ⼯�Ͻ����***********
#if DEBUG_LOCK
                this.WriteDebugInfo("API_RebuildResKeys()���Կ⼯�Ͻ������");
#endif
            }

            /*
            //��ʱ����database.xml // ���ü����ĺ�����
            if (this.Changed == true)
                this.SaveXmlSafety(true);
             * */

            return 0;
        }

        // ɾ��һ���������������Ŀ¼��Ҳ�������ļ�
        // return:
        //      -1  һ���Դ���
        //      -2  ʱ�����ƥ��
        //      -4  δ�ҵ�·����Ӧ����Դ
        //      -6  û���㹻��Ȩ��
        //      0   �ɹ�
        public int DeleteCfgItem(User user,
            string strCfgItemPath,
            byte[] intputTimestamp,
            out byte[] outputTimestamp,
            out string strError)
        {
            outputTimestamp = null;
            strError = "";

            if (strCfgItemPath == null
                || strCfgItemPath == "")
            {
                strError = "DeleteCfgItem()���ô���strCfgItemPath����ֵ����Ϊnull����ַ�����";
                return -1;
            }

            List<XmlNode> nodes = DatabaseUtil.GetNodes(this.NodeDbs,
                strCfgItemPath);
            if (nodes.Count == 0)
            {
                strError = "������������·��Ϊ'" + strCfgItemPath + "'���������";
                return -4;
            }
            if (nodes.Count != 1)
            {
                strError = "dp2Kernel ��������·��Ϊ '" + strCfgItemPath + "' �������������Ϊ '" + Convert.ToString(nodes.Count) + "'��database.xml �����ļ��쳣��";
                return -1;
            }


            string strExistRights = "";
            bool bHasRight = false;

            XmlNode node = nodes[0];

            if (node.Name == "dir")
            {
                // ��鵱ǰ�ʻ��Ƿ���ɾ����¼'
                bHasRight = user.HasRights(strCfgItemPath,
                    ResType.Directory,
                    "delete",
                    out strExistRights);
                if (bHasRight == false)
                {
                    strError = "�����ʻ���Ϊ'" + user.Name + "'����'" + strCfgItemPath + "'��������û��'ɾ��(delete)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                    return -6;
                }
                string strDir = DatabaseUtil.GetLocalDir(this.NodeDbs, node).Trim();
                Directory.Delete(this.DataDir + "\\" + strDir, true);
                node.ParentNode.RemoveChild(node);
                return 0;
            }
            else if (String.Compare(node.Name, "database", true) == 0)
            {

            }


            // ��鵱ǰ�ʻ��Ƿ���ɾ����¼'
            bHasRight = user.HasRights(strCfgItemPath,
                ResType.File,
                "delete",
                out strExistRights);
            if (bHasRight == false)
            {
                strError = "�����ʻ���Ϊ'" + user.Name + "'����'" + strCfgItemPath + "'��������û��'ɾ��(delete)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                return -6;
            }

            string strFilePath = "";//GetCfgItemLacalPath(strCfgItemPath);
            // return:
            //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
            //		-2	û�ҵ��ڵ�
            //		-3	localname����δ�����Ϊֵ��
            //		-4	localname�ڱ��ز�����
            //		-5	���ڶ���ڵ�
            //		0	�ɹ�
            int nRet = this.GetFileCfgItemLocalPath(strCfgItemPath,
                out strFilePath,
                out strError);
            if (nRet != 0)
            {
                if (nRet == -1 || nRet == -5)
                    return -1;

            }
            if (strFilePath != "")
            {
                string strNewFileName = DatabaseUtil.GetNewFileName(strFilePath);

                if (File.Exists(strFilePath) == true)
                {

                    byte[] oldTimestamp = null;
                    if (File.Exists(strNewFileName) == true)
                        oldTimestamp = DatabaseUtil.CreateTimestampForCfg(strNewFileName);
                    else
                        oldTimestamp = DatabaseUtil.CreateTimestampForCfg(strFilePath);

                    outputTimestamp = oldTimestamp;
                    if (ByteArray.Compare(oldTimestamp, intputTimestamp) != 0)
                    {
                        strError = "ʱ�����ƥ��";
                        return -2;
                    }
                }

                File.Delete(strNewFileName);
                File.Delete(strFilePath);

                string strRangeFileName = DatabaseUtil.GetRangeFileName(strFilePath);
                if (File.Exists(strRangeFileName) == false)
                    File.Delete(strRangeFileName);

                string strMetadataFileName = DatabaseUtil.GetMetadataFileName(strFilePath);
                if (File.Exists(strMetadataFileName) == false)
                    File.Delete(strMetadataFileName);
            }
            node.ParentNode.RemoveChild(node);

            this.Changed = true;
            this.SaveXml();

            return 0;
        }



        // ���ݷ������ϵ�ָ��·���г����¼�������
        // parameters:
        //		strPath	·��,�������������֣�
        //				��ʽΪ: "���ݿ���/�¼���/�¼���",
        //				��Ϊnull����Ϊ""ʱ����ʾ�г��÷����������е����ݿ�
        //		lStart	��ʼλ��,��0��ʼ ,����С��0
        //		lLength	���� -1��ʾ��lStart�����
        //		strLang	���԰汾 �ñ�׼��ĸ��ʾ������zh-CN
        //      strStyle    �Ƿ�Ҫ�г��������Ե�����? "alllang"��ʾҪ�г�ȫ������
        //		items	 out�����������¼���������
        // return:
        //		-1  ����
        //      -6  Ȩ�޲���
        //		0   ����
        // ˵��	ֻ�е�ǰ�ʻ���������"list"Ȩ��ʱ�������г�����
        //		����б������������ݿ�ʱ�������е����ݿⶼû��listȨ�ޣ�������������û�����ݿ��������ֿ���
        public int API_Dir(string strResPath,
            long lStart,
            long lLength,
            long lMaxLength,
            string strLang,
            string strStyle,
            User user,
            out ResInfoItem[] items,
            out int nTotalLength,
            out string strError)
        {
            items = new ResInfoItem[0];
            nTotalLength = 0;

            ArrayList aItem = new ArrayList();
            strError = "";
            int nRet = 0;
            //******************�ӿ⼯�ϼӶ���******
            this.m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);

#if DEBUG_LOCK
			this.WriteDebugInfo("Dir()���Կ⼯�ϼӶ�����");
#endif
            try
            {

                if (strResPath == "" || strResPath == null)
                {
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // 1.ȡ�������µ����ݿ�

                    nRet = this.GetDirableChildren(user,
                        strLang,
                        strStyle,
                        out aItem,
                        out strError);
                    if (this.Count > 0 && aItem.Count == 0)
                    {
                        strError = "�����ʻ���Ϊ'" + user.Name + "'�������е����ݿⶼû��'��ʾ(list)'Ȩ�ޡ�";
                        return -6;
                    }
                }
                else
                {
                    string strPath = strResPath;
                    string strDbName = StringUtil.GetFirstPartPath(ref strPath);

                    // ���������ݿ�Ҳ��������������
                    if (strPath == "")
                    {
                        Database db = this.GetDatabase(strDbName);
                        if (db != null)
                        {
                            // return:
                            //		-1	����
                            //		0	�ɹ�
                            nRet = db.GetDirableChildren(user,
                                strLang,
                                strStyle,
                                out aItem,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            goto END1;
                        }
                    }

                    // return:
                    //		-1	����
                    //		0	�ɹ�
                    nRet = this.DirCfgItem(user,
                        strResPath,
                        out aItem,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

            }
            finally
            {
                m_container_lock.ReleaseReaderLock();
                //*************�Կ⼯�Ͻ����***********
#if DEBUG_LOCK
				this.WriteDebugInfo("Dir()���Կ⼯�Ͻ������");
#endif
            }


        END1:
            // �г�ʵ����Ҫ����
            nTotalLength = aItem.Count;
            long lOutputLength;
            // return:
            //		-1  ����
            //		0   �ɹ�
            nRet = ConvertUtil.GetRealLength((int)lStart,
                (int)lLength,
                nTotalLength,
                (int)lMaxLength,
                out lOutputLength,
                out strError);
            if (nRet == -1)
                return -1;

            items = new ResInfoItem[(int)lOutputLength];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = (ResInfoItem)(aItem[i + (int)lStart]);
            }

            return 0;
        }


        // �õ�ĳһָ��·��strPath�Ŀ�����ʾ���¼�
        // parameters:
        //		oUser	��ǰ�ʻ�
        //		db	��ǰ���ݿ�
        //		strPath	���������·��
        //		strLang	���԰汾
        //		aItem	out���������ؿ�����ʾ���¼�
        //		strError	out������������Ϣ
        // return:
        //		-1	����
        //		0	�ɹ�
        private int DirCfgItem(User user,
            string strCfgItemPath,
            out ArrayList aItem,
            out string strError)
        {
            strError = "";
            aItem = new ArrayList();

            if (this.NodeDbs == null)
            {
                strError = "�����������ļ�δ����<dbs>Ԫ��";
                return -1;
            }
            List<XmlNode> list = DatabaseUtil.GetNodes(this.NodeDbs,
                strCfgItemPath);
            if (list.Count == 0)
            {
                strError = "δ�ҵ�·��Ϊ'" + strCfgItemPath + "'��Ӧ�����";
                return -1;
            }

            if (list.Count > 1)
            {
                strError = "���������������ļ����Ϸ�����鵽·��Ϊ'" + strCfgItemPath + "'��Ӧ�Ľڵ���'" + Convert.ToString(list.Count) + "'��������ֻ����һ����";
                return -1;
            }
            XmlNode node = list[0];

            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode child = node.ChildNodes[i];
                string strChildName = DomUtil.GetAttr(child, "name");
                if (strChildName == "")
                    continue;

                string strTempPath = strCfgItemPath + "/" + strChildName;
                string strExistRights;
                bool bHasRight = false;


                ResInfoItem resInfoItem = new ResInfoItem();
                resInfoItem.Name = strChildName;
                if (child.Name == "dir")
                {
                    bHasRight = user.HasRights(strTempPath,
                     ResType.Directory,
                     "list",
                     out strExistRights);
                    if (bHasRight == false)
                        continue;

                    resInfoItem.HasChildren = true;
                    resInfoItem.Type = 4;

                    resInfoItem.TypeString = DomUtil.GetAttr(child, "type");    // xietao 2006/6/5 add
                }
                else
                {
                    bHasRight = user.HasRights(strTempPath,
                        ResType.File,
                        "list",
                        out strExistRights);
                    if (bHasRight == false)
                        continue;
                    resInfoItem.HasChildren = false;
                    resInfoItem.Type = 5;

                    resInfoItem.TypeString = DomUtil.GetAttr(child, "type");    // xietao 2006/6/5 add

                }
                aItem.Add(resInfoItem);
            }
            return 0;
        }

        // �г��������µ�ǰ�ʻ�����ʾȨ�޵����ݿ�
        // �ߣ�����ȫ��
        // parameters:
        //      strStyle    �Ƿ�Ҫ�г��������Ե�����? "alllang"��ʾҪ�г��������Ե�����
        public int GetDirableChildren(User user,
            string strLang,
            string strStyle,
            out ArrayList aItem,
            out string strError)
        {
            aItem = new ArrayList();
            strError = "";

            if (this.NodeDbs == null)
            {
                strError = "��װ�������ļ����Ϸ���δ����<dbs>Ԫ��";
                return -1;
            }

            foreach (XmlNode child in this.NodeDbs.ChildNodes)
            {
                string strChildName = DomUtil.GetAttr(child, "name");
                if (String.Compare(child.Name, "database", true) != 0
                    && strChildName == "")
                    continue;

                if (String.Compare(child.Name, "database", true) != 0
                    && String.Compare(child.Name, "dir", true) != 0
                    && String.Compare(child.Name, "file", true) != 0)
                {
                    continue;
                }

                string strExistRights;
                bool bHasRight = false;

                ResInfoItem resInfoItem = new ResInfoItem();
                if (String.Compare(child.Name, "database", true) == 0)
                {
                    string strID = DomUtil.GetAttr(child, "id");
                    Database db = this.GetDatabaseByID("@" + strID);
                    if (db == null)
                    {
                        strError = "δ�ҵ�idΪ'" + strID + "'�����ݿ�";
                        return -1;
                    }

                    bHasRight = user.HasRights(db.GetCaption("zh"),
                        ResType.Database,
                        "list",
                        out strExistRights);
                    if (bHasRight == false)
                        continue;

                    if (StringUtil.IsInList("account", db.GetDbType(), true) == true)
                        resInfoItem.Style = 1;
                    else
                        resInfoItem.Style = 0;

                    resInfoItem.TypeString = db.GetDbType();

                    resInfoItem.Name = db.GetCaptionSafety(strLang);
                    resInfoItem.Type = 0;   // ���ݿ�
                    resInfoItem.HasChildren = true;

                    // ���Ҫ���ȫ�����Ե�����
                    if (StringUtil.IsInList("alllang", strStyle) == true)
                    {
                        List<string> results = db.GetAllLangCaptionSafety();
                        string [] names = new string[results.Count];
                        results.CopyTo(names);
                        resInfoItem.Names = names;
                    }
                }
                else if (String.Compare(child.Name, "dir", true) == 0)
                {
                    bHasRight = user.HasRights(strChildName,
                        ResType.Directory,
                        "list",
                        out strExistRights);
                    if (bHasRight == false)
                        continue;
                    resInfoItem.HasChildren = true;
                    resInfoItem.Type = 4;   // Ŀ¼
                    resInfoItem.Name = strChildName;

                    resInfoItem.TypeString = DomUtil.GetAttr(child, "type");   // xietao 2006/6/5 add
                }
                else
                {
                    bHasRight = user.HasRights(strChildName,
                        ResType.File,
                        "list",
                        out strExistRights);
                    if (bHasRight == false)
                        continue;
                    resInfoItem.HasChildren = false;
                    resInfoItem.Name = strChildName;
                    resInfoItem.Type = 5;   // �ļ�?

                    resInfoItem.TypeString = DomUtil.GetAttr(child, "type");   // xietao 2006/6/5 add
                }
                aItem.Add(resInfoItem);
            }
            return 0;
        }

        void resultset_GetTempFilename(object sender, GetTempFilenameEventArgs e)
        {
            e.TempFilename = GetTempFileName();
        }



        // �����û����ӿ��в����û���¼���õ��û�����
        // ������δ���뼯��, �������Ϊ�������
        // parameters:
        //		strBelongDb	�û����������ݿ�,��������
        //      user        out�����������ʻ�����
        //      strError    out���������س�����Ϣ
        // return:
        //		-1	����
        //		0	δ�ҵ��ʻ�
        //		1	�ҵ���
        // �ߣ���ȫ
        internal int ShearchUserSafety(string strUserName,
            out User user,
            out string strError)
        {
            user = null;
            strError = "";

            int nRet = 0;

            DpResultSet resultSet = new DpResultSet(GetTempFileName);
            resultSet.GetTempFilename += new GetTempFilenameEventHandler(resultset_GetTempFilename);


            //*********���ʻ��⼯�ϼӶ���***********
            m_container_lock.AcquireReaderLock(m_nContainerLockTimeOut);
#if DEBUG_LOCK
			this.WriteDebugInfo("ShearchUser()�������ݿ⼯�ϼӶ�����");
#endif
            try
            {
                // return:
                //		-1	����
                //		0	�ɹ�
                nRet = this.SearchUserInternal(strUserName,
                    resultSet,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
            finally
            {
                //*********���ʻ��⼯�Ͻ����*************
                m_container_lock.ReleaseReaderLock();
#if DEBUG_LOCK
				this.WriteDebugInfo("ShearchUser()�������ݿ⼯�Ͻ������");
#endif
            }

            // �����û���û�ҵ���Ӧ���ʻ���¼
            long lCount = resultSet.Count;
            if (lCount == 0)
                return 0;

            if (lCount > 1)
            {
                strError = "�û���'" + strUserName + "'��Ӧ������¼";
                return -1;
            }

            // ����һ���ʻ���
            DpRecord record = (DpRecord)resultSet[0];

            // ����һ��DpPsthʵ��
            DbPath path = new DbPath(record.ID);

            // �ҵ�ָ���ʻ����ݿ�
            Database db = this.GetDatabaseSafety(path.Name);
            if (db == null)
            {
                strError = "δ�ҵ�'" + strUserName + "'�ʻ���Ӧ����Ϊ'" + path.Name + "'�����ݿ����";
                return -1;
            }

            // ���ʻ������ҵ���¼
            string strXml = "";
            // return:
            //      -1  ����
            //      -4  ��¼������
            //      0   ��ȷ
            nRet = db.GetXmlDataSafety(path.ID,
                out strXml,
                out strError);
            if (nRet <= -1)  // ��-4��-1����Ϊ-1����
                return -1;

            //���ص�dom
            XmlDocument dom = new XmlDocument();
            //dom.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "�����û� '" + strUserName + "' ���ʻ���¼��domʱ����,ԭ��:" + ex.Message;
                return -1;
            }

            user = new User();
            // return:
            //      -1  ����
            //      0   �ɹ�
            nRet = user.Initial(
                record.ID,
                dom,
                db,
                this,
                out strError);
            if (nRet == -1)
                return -1;

            return 1;
        }

        // ���ݼ�¼·���õ����ݿ����
        public Database GetDatabaseFromRecPathSafety(string strRecPath)
        {
            // ����һ��DpPsthʵ��
            DbPath path = new DbPath(strRecPath);

            // �ҵ�ָ���ʻ����ݿ�
            return this.GetDatabaseSafety(path.Name);
        }

                // �������ʻ�������б��в����ʻ�
        // parameter
        //		strUserName �û���
        //		resultSet   �����,���ڴ�Ų��ҵ����û�
        //      strError    out���������س�����Ϣ
        // return:
        //		-1	����
        //		0	�ɹ�
        // �ߣ�����ȫ
        private int SearchUserInternal(string strUserName,
            DpResultSet resultSet,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strUserName) == true)
            {
                strError = "strUserName����Ϊ��";
                return -1;
            }

            foreach (Database db in this)
            {
                if (StringUtil.IsInList("account", db.GetDbType()) == false)
                    continue;

                if (strUserName.Length > db.KeySize)
                    continue;

                string strWarning = "";
                SearchItem searchItem = new SearchItem();
                searchItem.TargetTables = "";
                searchItem.Word = strUserName;
                searchItem.Match = "exact";
                searchItem.Relation = "=";
                searchItem.DataType = "string";
                searchItem.MaxCount = -1;
                searchItem.OrderBy = "";

                // �ʻ��ⲻ��ȥ������
                // return:
                //		-1	����
                //		0	�ɹ�
                int nRet = db.SearchByUnion(
                    "",
                    searchItem,
                    null,       //�����ж� , deleget
                    resultSet,
                    0,
                    out strError,
                    out strWarning);
                if (nRet == -1)
                    return -1;
            }
            return 0;
        }

    } // end of class DatabaseCollection


#if NO
    //*****************************************************

    // string���͵�ArrayList������IComparer�ӿ�
    public class ComparerClass : IComparer
    {
        int IComparer.Compare(object x, object y)
        {
            if (!(x is String))
                throw new Exception("object x is not a String");
            if (!(y is String))
                throw new Exception("object y is not a String");

            string strText1 = (string)x;
            string strText2 = (string)y;

            return String.Compare(strText1, strText2, true);
        }
    }
#endif

    // ���ͨѶ�Ƿ������ŵ�delegate
    // public delegate bool Delegate_isConnected();

    public delegate void ChannelIdleEventHandler(object sender,
ChannelIdleEventArgs e);

    /// <summary>
    /// �����¼��Ĳ���
    /// </summary>
    public class ChannelIdleEventArgs : EventArgs
    {
        public bool Continue = true;
    }


    public class ChannelHandle
    {
        //public DatabaseCollection Dbs = null;
        public KernelApplication App = null;

        bool m_bStop = false;

        public event ChannelIdleEventHandler Idle = null;
        public event EventHandler Stop = null;

        public void Clear()
        {
            this.m_bStop = false;
        }

        // return:
        //      false   ϣ��ֹͣ
        //      true    ϣ������
        public bool DoIdle()
        {
            if (this.m_bStop == true)
                return false;

            if (this.Idle == null)
                return true;    // ��Զ��ֹͣ

            ChannelIdleEventArgs e = new ChannelIdleEventArgs();
            this.Idle(this, e);

            if (e.Continue == false)
            {
                this.App.MyWriteDebugInfo("abort");

                this.m_bStop = true;    // 2011/1/19 

                return false;
            }
            return true;
        }

        public void DoStop()
        {
            this.m_bStop = true;

            if (this.Stop != null)
            {
                this.Stop(this, null);
            }
        }

        public bool Stopped
        {
            get
            {
                return this.m_bStop;
            }
        }

        /*
        public bool DoIdle()
        {
            if (this.Response1.IsClientConnected == false)
            {
                this.Dbs.MyWriteDebugInfo("abort");
                return false;
            }
            this.Dbs.MyWriteDebugInfo("is...!");
            return true;
        }

        public void DoStop()
        {
            if (this.Response1 != null)
            {
                this.Response1.Close();
            }
        }
         * */
    }

    #region ר�����ڼ�������
    public class DatabaseCommandTask
    {
        public object m_command = null;
        public AutoResetEvent m_event = new AutoResetEvent(false);

        public bool bError = false;
        public string ErrorString = "";
        // ���ⲿʹ��
        public /*SqlDataReader*/object DataReader = null;

        public bool Canceled = false;

        public DatabaseCommandTask(object command)
        {
            m_command = command;
        }

        public void Cancel()
        {
            this.Canceled = true;

            // CloseConnection();

            if (m_command is SqlCommand)
                ((SqlCommand)m_command).Cancel();
            else if (m_command is SQLiteCommand)
                ((System.Data.SQLite.SQLiteCommand)m_command).Cancel();
            else if (m_command is MySqlCommand)
            {
                try
                {
                    ((MySqlCommand)m_command).Cancel();
                }
                catch
                {
                }
            }
            else if (m_command is OracleCommand)
                ((OracleCommand)m_command).Cancel();

        }


        // ������
        public void ThreadMain()
        {
            try
            {
                if (this.Canceled == false)
                {
                    if (m_command is SqlCommand)
                        DataReader = ((SqlCommand)m_command).ExecuteReader(CommandBehavior.CloseConnection);
                    else if (m_command is SQLiteCommand)
                        DataReader = ((SQLiteCommand)m_command).ExecuteReader(CommandBehavior.CloseConnection);
                    else if (m_command is MySqlCommand)
                        DataReader = ((MySqlCommand)m_command).ExecuteReader(CommandBehavior.CloseConnection);
                    else if (m_command is OracleCommand)
                        DataReader = ((OracleCommand)m_command).ExecuteReader(CommandBehavior.CloseConnection);
                }
            }
            catch (SqlException sqlEx)
            {
                this.bError = true;
                string strConnectionName = ((SqlCommand)m_command).Connection.GetHashCode().ToString();
                this.ErrorString = "�����߳�(1):" + SqlDatabase.GetSqlErrors(sqlEx) + "; connection hashcode='" + strConnectionName + "'"; ;
            }
            catch (SQLiteException sqlEx)
            {
                this.bError = true;
                string strConnectionName = ((SQLiteCommand)m_command).Connection.GetHashCode().ToString();
                this.ErrorString = "�����߳�(1):" + sqlEx.ToString() + "; connection hashcode='" + strConnectionName + "'"; ;
            }
            catch (MySqlException sqlEx)
            {
                this.bError = true;
                string strConnectionName = ((MySqlCommand)m_command).Connection.GetHashCode().ToString();
                this.ErrorString = "�����߳�(1):" + sqlEx.ToString() + "; connection hashcode='" + strConnectionName + "'"; ;
            }
            catch (OracleException sqlEx)
            {
                this.bError = true;
                string strConnectionName = ((OracleCommand)m_command).Connection.GetHashCode().ToString();
                this.ErrorString = "�����߳�(1):" + sqlEx.ToString() + "; connection hashcode='" + strConnectionName + "'"; ;
            }
            catch (Exception ex)
            {
                this.bError = true;
                string strConnectionName = "";
                if (m_command is SqlCommand)
                    strConnectionName = ((SqlCommand)m_command).Connection.GetHashCode().ToString();
                else if (m_command is SQLiteCommand)
                    strConnectionName = ((SQLiteCommand)m_command).Connection.GetHashCode().ToString();
                else if (m_command is MySqlCommand)
                    strConnectionName = ((MySqlCommand)m_command).Connection.GetHashCode().ToString();
                else if (m_command is OracleCommand)
                    strConnectionName = ((OracleCommand)m_command).Connection.GetHashCode().ToString();

                this.ErrorString = "�����߳�(2): " + ex.Message + "; connection hashcode='"+strConnectionName+"'";
            }
			finally  // һ��Ҫ�����ź�
            {
                m_event.Set();

                // ���̸߳����ͷ���Դ
                CloseReader();
                CloseConnection();
                DisposeCommand();
            }
        }

        public void DisposeCommand()
        {
            if (this.m_command == null
    || this.Canceled == false)
                return;

                if (m_command is SqlCommand)
                {
                    ((SqlCommand)m_command).Dispose();
                    // ((SqlCommand)m_command).Connection.Close();
                }
                else if (m_command is SQLiteCommand)
                    ((SQLiteCommand)m_command).Dispose();
                else if (m_command is MySqlCommand)
                    ((MySqlCommand)m_command).Dispose();
                else if (m_command is OracleCommand)
                    ((OracleCommand)m_command).Dispose();
        }

        public void CloseConnection()
        {
            if (this.m_command == null
    || this.Canceled == false)
                return;

            if (m_command is SqlCommand)
                ((SqlCommand)m_command).Connection.Close();
            else if (m_command is SQLiteCommand)
                ((SQLiteCommand)m_command).Connection.Close();
            else if (m_command is MySqlCommand)
                ((MySqlCommand)m_command).Connection.Close();
            else if (m_command is OracleCommand)
                ((OracleCommand)m_command).Connection.Close();
        }

        public void CloseReader()
        {
            if (this.DataReader == null
                || this.Canceled == false)
                return;

            if (this.DataReader is SqlDataReader)
                ((SqlDataReader)this.DataReader).Close();
            else if (this.DataReader is SQLiteDataReader)
                ((SQLiteDataReader)this.DataReader).Close();
            else if (this.DataReader is MySqlDataReader)
                ((MySqlDataReader)this.DataReader).Close();
            else if (this.DataReader is OracleDataReader)
                ((OracleDataReader)this.DataReader).Close();
        }
    }


    #endregion

    // ��Դ����Ϣ
    // ��ʱ����DigitalPlatform.rms.Service�����Ҫ��Database.xml��ʹ�ã������ƶ������
    [DataContract(Namespace = "http://dp2003.com/dp2kernel/")]
    public class ResInfoItem
    {
        [DataMember]
        public int Type;	// ����,0 �⣬1 ;��,4 cfgs,5 file
        [DataMember]
        public string Name;	// ������;����
        [DataMember]
        public bool HasChildren = true;  //�Ƿ��ж���
        [DataMember]
        public int Style = 0;   // 0x01:�ʻ���  // ԭ��Style

        [DataMember]
        public string TypeString = "";  // ����
        [DataMember]
        public string[] Names;    // ���� ���������µ����֡�ÿ��Ԫ�صĸ�ʽ ���Դ���:����
    }

    public enum SqlServerType
    {
        None = 0,
        MsSqlServer = 1,
        SQLite = 2,
        MySql = 3,
        Oracle = 4,
    }
}

