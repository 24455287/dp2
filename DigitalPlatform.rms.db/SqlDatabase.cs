//#define DEBUG_LOCK_SQLDATABASE
//#define XML_WRITE_TO_FILE   // ���ߴ����Ҫ���XML��¼Ҳд������ļ�
//#define UPDATETEXT_WITHLOG    // ����Ҫ���ո��Ƶ�ʱ�������
// #define PARAMETERS  // ModifyKeys() MySql �汾ʹ�ò���SQL����

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Collections;
using System.Data;

using System.Data.SqlClient;
using System.Data.SQLite;

using MySql.Data;
using MySql.Data.MySqlClient;

using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

using System.Threading;
using System.Diagnostics;

using DigitalPlatform.ResultSet;
using DigitalPlatform.Text;
using DigitalPlatform.Range;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;

namespace DigitalPlatform.rms
{
    // SQL��������
    public class SqlDatabase : Database
    {
        const string KEY_COL_LIST = "(keystring, idstring)";
        const string KEYNUM_COL_LIST = "(keystringnum, idstring)";

        public SQLiteInfo SQLiteInfo = null;

        public bool FastMode
        {
            get
            {
                if (this.SQLiteInfo != null && this.SQLiteInfo.FastMode == true)
                    return true;
                return false;
            }
            set
            {
                if (this.container.SqlServerType != SqlServerType.SQLite)
                    return;

                if (this.SQLiteInfo == null)
                {
                    this.SQLiteInfo = new SQLiteInfo();
                }

                if (this.FastMode == true
                    && value == false)
                {
                    this.Commit();
                    /*
                    if (this.SQLiteInfo.m_connection != null)
                        this.Close();
                     * */
                }
                this.SQLiteInfo.FastMode = value;

            }
        }

        static int m_nLongTimeout = 30 * 60; //  
        // �����ַ���
        private string m_strConnStringPooling = "";        // ��ͨ�����ַ�����pooling = true
        private string m_strConnString = "";        // ��ͨ�����ַ�����pooling = false
        private string m_strLongConnString = "";    // timeout�ϳ��������ַ���, pooling = false

        // Sql���ݿ�����
        private string m_strSqlDbName = "";

        private string m_strObjectDir = "";     // �����ļ��洢Ŀ¼
        private long m_lObjectStartSize = 0x7ffffffe;   // 10 * 1024;    // ���ڵ�������ߴ�Ķ��󽫴洢�ڶ����ļ��С�-1��ʾ��Զ��ʹ�ö���Ŀ¼

        public SqlDatabase(DatabaseCollection container)
            : base(container)
        { }

        public static string GetSqlErrors(SqlException exception)
        {
            if (exception.Errors is SqlErrorCollection)
            {
                string strResult = "";
                for (int i = 0; i < exception.Errors.Count; i++)
                {
                    strResult += "error " + (i + 1).ToString() + ": " + exception.Errors[i].ToString() + "\r\n";
                }
                return strResult;
            }
            else
            {
                return exception.Message;
            }
        }

        // ��ʼ�����ݿ����
        // parameters:
        //      node    ���ݿ����ýڵ�<database>
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        internal override int Initial(XmlNode node,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            Debug.Assert(node != null, "Initial()���ô���node����ֵ����Ϊnull��");

            //****************�����ݿ��д��**** �ڹ���ʱ,�����ܶ�Ҳ����д
            this.m_db_lock.AcquireWriterLock(m_nTimeOut);
            try
            {
                this.m_selfNode = node;

                // ֻ�������д�ˣ�Ҫ������δ��ʼ���ء�
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("Initial()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif

                // �����㳤��
                // return:
                //      -1  ����
                //      0   �ɹ�
                // ��: ����ȫ
                nRet = this.container.InternalGetKeySize(
                    out this.KeySize,
                    out strError);
                if (nRet == -1)
                    return -1;

                // ��ID
                this.PureID = DomUtil.GetAttr(this.m_selfNode, "id").Trim();
                if (this.PureID == "")
                {
                    strError = "�����ļ����Ϸ�����nameΪ'" + this.GetCaption("zh-CN") + "'��<database>�¼�δ����'id'���ԣ���'id'����Ϊ��";
                    return -1;
                }

                // ���Խڵ�
                this.PropertyNode = this.m_selfNode.SelectSingleNode("property");
                if (this.PropertyNode == null)
                {
                    strError = "�����ļ����Ϸ�����nameΪ'" + this.GetCaption("zh-CN") + "'��<database>�¼�δ����<property>Ԫ��";
                    return -1;
                }

                // <sqlserverdb>�ڵ�
                XmlNode nodeSqlServerDb = this.PropertyNode.SelectSingleNode("sqlserverdb");
                if (nodeSqlServerDb == null)
                {
                    strError = "�����ļ����Ϸ�����nameΪ'" + this.GetCaption("zh-CN") + "'��database/property�¼�δ����<sqlserverdb>Ԫ��";
                    return -1;
                }

                // ���SqlServer������ֻ��Sql���Ϳ����Ҫ
                this.m_strSqlDbName = DomUtil.GetAttr(nodeSqlServerDb, "name").Trim();
                if (this.m_strSqlDbName == "")
                {
                    strError = "�����ļ����Ϸ�����nameΪ'" + this.GetCaption("zh-CN") + "'��database/property/sqlserverdb�Ľڵ�δ����'name'���ԣ���'name'����ֵΪ��";
                    return -1;
                }

                // <object>�ڵ�
                XmlNode nodeObject = this.PropertyNode.SelectSingleNode("object");
                if (nodeObject != null)
                {
                    this.m_strObjectDir = DomUtil.GetAttr(nodeObject, "dir").Trim();

                    if (string.IsNullOrEmpty(this.m_strObjectDir) == false)
                    {
                        // �������ļ�Ŀ¼�Ƿ���Ϲ���
                        // ����ʹ�ø�Ŀ¼
                        string strRoot = Directory.GetDirectoryRoot(this.m_strObjectDir);
                        if (PathUtil.IsEqual(strRoot, this.m_strObjectDir) == true)
                        {
                            strError = "����Ŀ¼���� '" + this.m_strObjectDir + "' ���Ϸ�������Ŀ¼�����Ǹ�Ŀ¼";
                            // ��������־дһ����Ϣ
                            this.container.KernelApplication.WriteErrorLog(strError);
                            return -1;
                        }
                    }

                    long lValue = 0;
                    if (DomUtil.GetIntegerParam(nodeObject,
                        "startSize",
                        this.m_lObjectStartSize,
                        out lValue,
                        out strError) == -1)
                    {
                        strError = "��ȡ���ݿ�� startSize ����ʱ��������" + strError;
                        return -1;
                    }

                    this.m_lObjectStartSize = lValue;
                }

                if (this.container.SqlServerType != SqlServerType.MsSqlServer)
                {
                    this.m_lObjectStartSize = 0;    // �ڲ���MS SQL Server����£����ж���д������ļ�
                }

                if (this.m_lObjectStartSize != -1)
                {
                    if (string.IsNullOrEmpty(this.m_strObjectDir) == true)
                    {
                        // ����ȱʡ����
                        this.m_strObjectDir = PathUtil.MergePath(this.container.ObjectDir, this.m_strSqlDbName);
                        try
                        {
                            PathUtil.CreateDirIfNeed(this.m_strObjectDir);
                        }
                        catch (Exception ex)
                        {
                            strError = "�����������ݿ�����ݶ���Ŀ¼ '" + this.m_strObjectDir + "' ʱ����: " + ex.Message;
                            return -1;
                        }
                    }
                }

#if NO
                // *****************************************
                this.m_lObjectStartSize = 0;    // testing !!!
#endif

                if (this.container.SqlServerType == SqlServerType.SQLite)
                {
                    this.SQLiteInfo = new SQLiteInfo();
                }

                // return:
                //      -1  ����
                //      0   �ɹ�
                // ��: ����ȫ��
                nRet = this.InternalGetConnectionString(
                    30,
                    "",
                    out this.m_strConnString,
                    out strError);
                if (nRet == -1)
                    return -1;

                // 2012/2/17
                nRet = this.InternalGetConnectionString(
    30,
    "pooling",
    out this.m_strConnStringPooling,
    out strError);
                if (nRet == -1)
                    return -1;

                //      -1  ����
                //      0   �ɹ�
                // ��: ����ȫ��
                nRet = this.InternalGetConnectionString(
                    m_nLongTimeout,
                    "",
                    out this.m_strLongConnString,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
            finally
            {
                m_db_lock.ReleaseWriterLock();
                //***********�����ݿ��д��*************
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("Initial()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
            }

            return 0;
        }

        internal override void Close()
        {
            this.CloseInternal();
        }

        void CloseInternal(bool bLock = true)
        {
#if NO
            if (bLock == true)
                this.m_db_lock.AcquireWriterLock(m_nTimeOut);
            try
            {
#endif
                if (this.SQLiteInfo != null)
                {
                    lock (this.SQLiteInfo)
                    {
                        if (this.SQLiteInfo != null
                            && this.SQLiteInfo.m_connection != null)
                        {
                            this.SQLiteInfo.m_connection.Close(false);
                            this.SQLiteInfo.m_connection = null;
                        }
                    }
                }
#if NO
            }
            finally
            {
                if (bLock == true)
                    m_db_lock.ReleaseWriterLock();
            }
#endif
        }

        // �쳣�����ܻ��׳��쳣
        internal override void Commit()
        {
            try
            {
                // ����ʱ��
                DateTime start_time = DateTime.Now;

                this.CommitInternal();

                TimeSpan delta = DateTime.Now - start_time;
                int nTicks = (int)(delta.TotalSeconds * 1000);
                if (this.m_nTimeOut < nTicks * 2)
                    this.m_nTimeOut = nTicks * 2;

                if (nTicks > 5000 && this.SQLiteInfo != null
                    && this.SQLiteInfo.m_connection != null)
                {
                    this.SQLiteInfo.m_connection.m_nThreshold = 100;
                }
            }
            catch (Exception ex)
            {
                string strError = ex.Message;
            }
        }

        void CommitInternal(bool bLock = true)
        {
            if (this.SQLiteInfo == null
                || this.SQLiteInfo.m_connection == null)
                return;

            if (bLock == true)
                this.m_db_lock.AcquireWriterLock(m_nTimeOut);
            try
            {
                if (this.SQLiteInfo != null)
                {
                    lock (this.SQLiteInfo)
                    {
                        if (this.SQLiteInfo != null
                            && this.SQLiteInfo.m_connection != null)
                        {
                            this.SQLiteInfo.m_connection.Commit(bLock);

                            /*
                            this.SQLiteInfo.m_connection.Close(false);
                            this.SQLiteInfo.m_connection = null;
                             * */
                        }
                    }
                }
            }
            finally
            {
                if (bLock == true)
                    m_db_lock.ReleaseWriterLock();
            }
        }

        // �õ������ַ���,ֻ�п�����ΪSqlDatabaseʱ��������
        // parameters:
        //      strStyle    ���pooling ������߱��������pooling = false
        //      strConnection   out���������������ַ�����
        //      strError        out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        // ��: ����ȫ��
        internal int InternalGetConnectionString(
            int nTimeout,
            string strStyle,
            out string strConnection,
            out string strError)
        {
            strConnection = "";
            strError = "";

            XmlNode nodeDataSource = this.container.CfgDom.DocumentElement.SelectSingleNode("datasource");
            if (nodeDataSource == null)
            {
                strError = "�����������ļ����Ϸ���δ�ڸ�Ԫ���¶���<datasource>Ԫ��";
                return -1;
            }

            string strMode = DomUtil.GetAttr(nodeDataSource, "mode");

            SqlServerType servertype = this.container.SqlServerType;

            if (servertype == SqlServerType.SQLite)
            {
                if (string.IsNullOrEmpty(this.m_strObjectDir) == true)
                {
                    strError = "���ݿ� '" + this.GetCaption("zh-CN") + "' û�ж��� m_strObjectDir ֵ";
                    return -1;
                }
                strConnection = "Data Source=" + PathUtil.MergePath(this.m_strObjectDir, "sqlite_database.bin")
                    + ";Page Size=8192";   // Synchronues=OFF;;Cache Size=70000
                return 0;
            }

            if (servertype == SqlServerType.MySql)
            {
                if (String.IsNullOrEmpty(strMode) == true)
                {
                    string strUserID = "";
                    string strPassword = "";

                    strUserID = DomUtil.GetAttr(nodeDataSource, "userid").Trim();
                    if (strUserID == "")
                    {
                        strError = "�����������ļ����Ϸ���δ����Ԫ���¼���<datasource>����'userid'���ԣ���'userid'����ֵΪ�ա�";
                        return -1;
                    }

                    strPassword = DomUtil.GetAttr(nodeDataSource, "password").Trim();
                    if (strPassword == "")
                    {
                        strError = "�����������ļ����Ϸ���δ����Ԫ���¼���<datasource>����'password'���ԣ���'password'����ֵΪ�ա�";
                        return -1;
                    }
                    // password����Ϊ��
                    try
                    {
                        strPassword = Cryptography.Decrypt(strPassword,
                                "dp2003");
                    }
                    catch
                    {
                        strError = "�����������ļ����Ϸ�����Ԫ���¼���<datasource>����'password'����ֵ���Ϸ���";
                        return -1;
                    }

                    strConnection = @"Persist Security Info=False;"
                        + "User ID=" + strUserID + ";"    //�ʻ�������
                        + "Password=" + strPassword + ";"
                        //+ "Integrated Security=SSPI; "      //��������
                        + "Data Source=" + this.container.SqlServerName + ";"
                        // http://msdn2.microsoft.com/en-us/library/8xx3tyca(vs.71).aspx
                        + "Connect Timeout=" + nTimeout.ToString() + ";"
                        + "charset=utf8;";

                }
                else if (strMode == "SSPI") // 2006/3/22
                {
                    strConnection = @"Persist Security Info=False;"
                        + "Integrated Security=SSPI; "      //��������
                        + "Data Source=" + this.container.SqlServerName + ";"
                        + "Connect Timeout=" + nTimeout.ToString() + ";" // 30��
                        + "charset=utf8;";
                }
                else
                {
                    strError = "�����������ļ����Ϸ�����Ԫ���¼���<datasource>����mode����ֵ'" + strMode + "'���Ϸ���";
                    return -1;
                }

                if (StringUtil.IsInList("pooling", strStyle) == false)
                    strConnection += "Pooling=false;";

                return 0;
            }

            if (servertype == SqlServerType.Oracle)
            {
                if (String.IsNullOrEmpty(strMode) == true)
                {
                    string strUserID = "";
                    string strPassword = "";

                    strUserID = DomUtil.GetAttr(nodeDataSource, "userid").Trim();
                    if (strUserID == "")
                    {
                        strError = "�����������ļ����Ϸ���δ����Ԫ���¼���<datasource>����'userid'���ԣ���'userid'����ֵΪ�ա�";
                        return -1;
                    }

                    strPassword = DomUtil.GetAttr(nodeDataSource, "password").Trim();
                    if (strPassword == "")
                    {
                        strError = "�����������ļ����Ϸ���δ����Ԫ���¼���<datasource>����'password'���ԣ���'password'����ֵΪ�ա�";
                        return -1;
                    }
                    // password����Ϊ��
                    try
                    {
                        strPassword = Cryptography.Decrypt(strPassword,
                                "dp2003");
                    }
                    catch
                    {
                        strError = "�����������ļ����Ϸ�����Ԫ���¼���<datasource>����'password'����ֵ���Ϸ���";
                        return -1;
                    }

                    strConnection = @"Persist Security Info=False;"
                        + "User ID=" + strUserID + ";"    //�ʻ�������
                        + "Password=" + strPassword + ";"
                        //+ "Integrated Security=SSPI; "      //��������
                        + "Data Source=" + this.container.SqlServerName + ";"
                        // http://msdn2.microsoft.com/en-us/library/8xx3tyca(vs.71).aspx
                        + "Connect Timeout=" + nTimeout.ToString() + ";";

                }
                else if (strMode == "SSPI") // 2006/3/22
                {
                    strConnection = @"Persist Security Info=False;"
                        + "Integrated Security=SSPI; "      //��������
                        + "Data Source=" + this.container.SqlServerName + ";"
                        + "Connect Timeout=" + nTimeout.ToString() + ";"; // 30��
                }
                else
                {
                    strError = "�����������ļ����Ϸ�����Ԫ���¼���<datasource>����mode����ֵ'" + strMode + "'���Ϸ���";
                    return -1;
                }

                // ȫ����pooling
                /*
                if (StringUtil.IsInList("pooling", strStyle) == false)
                    strConnection += "Pooling=false;";
                 * */
                return 0;
            }


            if (String.IsNullOrEmpty(strMode) == true)
            {
                string strUserID = "";
                string strPassword = "";

                strUserID = DomUtil.GetAttr(nodeDataSource, "userid").Trim();
                if (strUserID == "")
                {
                    strError = "�����������ļ����Ϸ���δ����Ԫ���¼���<datasource>����'userid'���ԣ���'userid'����ֵΪ�ա�";
                    return -1;
                }

                strPassword = DomUtil.GetAttr(nodeDataSource, "password").Trim();
                if (strPassword == "")
                {
                    strError = "�����������ļ����Ϸ���δ����Ԫ���¼���<datasource>����'password'���ԣ���'password'����ֵΪ�ա�";
                    return -1;
                }
                // password����Ϊ��
                try
                {
                    strPassword = Cryptography.Decrypt(strPassword,
                            "dp2003");
                }
                catch
                {
                    strError = "�����������ļ����Ϸ�����Ԫ���¼���<datasource>����'password'����ֵ���Ϸ���";
                    return -1;
                }

                strConnection = @"Persist Security Info=False;"
                    + "User ID=" + strUserID + ";"    //�ʻ�������
                    + "Password=" + strPassword + ";"
                    //+ "Integrated Security=SSPI; "      //��������
                    + "Data Source=" + this.container.SqlServerName + ";"
                    // http://msdn2.microsoft.com/en-us/library/8xx3tyca(vs.71).aspx
                    + "Connect Timeout=" + nTimeout.ToString() + ";";

            }
            else if (strMode == "SSPI") // 2006/3/22
            {
                strConnection = @"Persist Security Info=False;"
                    + "Integrated Security=SSPI; "      //��������
                    + "Data Source=" + this.container.SqlServerName + ";"
                    + "Connect Timeout=" + nTimeout.ToString() + ";"; // 30��
            }
            else
            {
                strError = "�����������ļ����Ϸ�����Ԫ���¼���<datasource>����mode����ֵ'" + strMode + "'���Ϸ���";
                return -1;
            }

            /*
            if (StringUtil.IsInList("pooling", strStyle) == false)
                strConnection += "Pooling=false;";
             * */

            /*
        else
            strConnection += "Max Pool Size=1000;";
             * */
            strConnection += "Asynchronous Processing=true;";

            return 0;
        }


        // �õ�����Դ���ƣ�����Sql���ݿ⣬����Sql���ݿ�����
        public override string GetSourceName()
        {
            return this.m_strSqlDbName;
        }

        // ��ʼ�����ݿ⣬ע���麯������Ϊprivate
        // parameter:
        //		strError    out���������س�����Ϣ
        // return:
        //		-1  ����
        //		0   �ɹ�
        // ��: ��ȫ��
        // ��д����ԭ���޸ļ�¼β�ţ������SQL�Ĳ������ص�����
        public override int InitialPhysicalDatabase(out string strError)
        {
            strError = "";

            //************�����ݿ��д��********************
            m_db_lock.AcquireWriterLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
			this.container.WriteDebugInfo("Initialize()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif

            try
            {

                if (this.RebuildIDs != null && this.RebuildIDs.Count > 0)
                {
                    this.RebuildIDs.Delete();
                    this.RebuildIDs = null;
                }

                if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                {
                    SqlConnection connection = new SqlConnection(this.m_strConnString);
                    connection.Open();
                    try //����
                    {
                        string strCommand = "";
                        // 1.����
                        strCommand = this.GetCreateDbCmdString(this.container.SqlServerType);
                        using (SqlCommand command = new SqlCommand(strCommand,
                            connection))
                        {
                            try
                            {
                                command.CommandTimeout = 20 * 60;  // �ѳ�ʱʱ��Ŵ� 2013/2/10
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "�������.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }

                            // 2.����
                            int nRet = this.GetCreateTablesString(
                                this.container.SqlServerType,
                                out strCommand,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            command.CommandText = strCommand;
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "�������.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }

                            // 3.������
                            nRet = this.GetCreateIndexString(
                                "keys,records",
                                this.container.SqlServerType,
                                "create",
                                out strCommand,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            command.CommandText = strCommand;
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "����������.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }
                        } // end of using command

                        // 4.����¼����Ϊ0
                        this.SetTailNo(0);
                        this.m_bTailNoVerified = true;  // 2011/2/26
                        this.container.Changed = true;   //���ݸı�
                    }
                    finally
                    {
                        connection.Close();
                    }

                    // ɾ������Ŀ¼��Ȼ���ؽ�
                    try
                    {
                        if (string.IsNullOrEmpty(this.m_strObjectDir) == false)
                        {
                            PathUtil.DeleteDirectory(this.m_strObjectDir);
                            PathUtil.CreateDirIfNeed(this.m_strObjectDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        strError = "��� ���ݿ� '" + this.GetCaption("zh") + "' �� ԭ�ж���Ŀ¼ '" + this.m_strObjectDir + "' ʱ�������� " + ex.Message;
                        return -1;
                    }
                }
                else if (this.container.SqlServerType == SqlServerType.SQLite)
                {
                    // Commit Transaction
                    this.CloseInternal(false);

                    // ɾ������Ŀ¼��Ȼ���ؽ�
                    try
                    {
                        if (string.IsNullOrEmpty(this.m_strObjectDir) == false)
                        {
                            PathUtil.DeleteDirectory(this.m_strObjectDir);

                            PathUtil.CreateDirIfNeed(this.m_strObjectDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        strError = "��� ���ݿ� '" + this.GetCaption("zh") + "' �� ԭ�ж���Ŀ¼ '" + this.m_strObjectDir + "' ʱ�������� " + ex.Message;
                        return -1;
                    }

                    SQLiteConnection connection = new SQLiteConnection(this.m_strConnString);
                    // connection.Open();
                    Open(connection);
                    try //����
                    {
                        string strCommand = "";
                        // 2.����
                        int nRet = this.GetCreateTablesString(
                            this.container.SqlServerType,
                            out strCommand,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        using (SQLiteCommand command = new SQLiteCommand(strCommand,
                            connection))
                        {
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "�������.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }

                            // 3.������
                            nRet = this.GetCreateIndexString(
                                "keys,records",
                                this.container.SqlServerType,
                                "create",
                                out strCommand,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            command.CommandText = strCommand;
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "����������.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }
                        } // end of using command

                        // 4.����¼����Ϊ0
                        this.SetTailNo(0);
                        this.m_bTailNoVerified = true;  // 2011/2/26
                        this.container.Changed = true;   //���ݸı�
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
                else if (this.container.SqlServerType == SqlServerType.MySql)
                {
                    MySqlConnection connection = new MySqlConnection(this.m_strConnString);
                    connection.Open();
                    try //����
                    {
                        string strCommand = "";
                        // 1.����
                        strCommand = this.GetCreateDbCmdString(this.container.SqlServerType);
                        using (MySqlCommand command = new MySqlCommand(strCommand,
                            connection))
                        {
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "�������.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }

                            // 2.����
                            int nRet = this.GetCreateTablesString(
                                this.container.SqlServerType,
                                out strCommand,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            command.CommandText = strCommand;
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "�������.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }

                            // 3.������
                            nRet = this.GetCreateIndexString(
                                "keys,records",
                                this.container.SqlServerType,
                                "create",
                                out strCommand,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            command.CommandText = strCommand;
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "����������.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }
                        } // end of using command

                        // 4.����¼����Ϊ0
                        this.SetTailNo(0);
                        this.m_bTailNoVerified = true;  // 2011/2/26
                        this.container.Changed = true;   //���ݸı�
                    }
                    finally
                    {
                        connection.Close();
                    }

                    // ɾ������Ŀ¼��Ȼ���ؽ�
                    try
                    {
                        if (string.IsNullOrEmpty(this.m_strObjectDir) == false)
                        {
                            PathUtil.DeleteDirectory(this.m_strObjectDir);
                            PathUtil.CreateDirIfNeed(this.m_strObjectDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        strError = "��� ���ݿ� '" + this.GetCaption("zh") + "' �� ԭ�ж���Ŀ¼ '" + this.m_strObjectDir + "' ʱ�������� " + ex.Message;
                        return -1;
                    }
                }
                else if (this.container.SqlServerType == SqlServerType.Oracle)
                {
                    OracleConnection connection = new OracleConnection(this.m_strConnString);
                    connection.Open();
                    try //����
                    {
                        string strCommand = "";

                        using (OracleCommand command = new OracleCommand("",
    connection))
                        {

#if NO
                        // 1.����
                        strCommand = this.GetCreateDbCmdString(this.container.SqlServerType);
                        command = new OracleCommand(strCommand,
                            connection);
                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            strError = "�������.\r\n"
                                + ex.Message + "\r\n"
                                + "SQL����:\r\n"
                                + strCommand;
                            return -1;
                        }
#endif
                            int nRet = DropAllTables(
                                 connection,
                                 this.m_strSqlDbName,
                                 "keys,records",
                                 out strError);
                            if (nRet == -1)
                                return -1;

                            // 2.����
                            nRet = this.GetCreateTablesString(
                                this.container.SqlServerType,
                                out strCommand,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            string[] lines = strCommand.Split(new char[] { ';' });
                            foreach (string line in lines)
                            {
                                string strLine = line.Trim();
                                if (string.IsNullOrEmpty(strLine) == true)
                                    continue;
                                command.CommandText = strLine;
                                try
                                {
                                    command.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    strError = "�������.\r\n"
                                        + ex.Message + "\r\n"
                                        + "SQL����:\r\n"
                                        + strLine;
                                    return -1;
                                }
                            }

                            // 3.������
                            nRet = this.GetCreateIndexString(
                                "keys,records",
                                this.container.SqlServerType,
                                "create",
                                out strCommand,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            lines = strCommand.Split(new char[] { ';' });
                            foreach (string line in lines)
                            {
                                string strLine = line.Trim();
                                if (string.IsNullOrEmpty(strLine) == true)
                                    continue;
                                command.CommandText = strLine;
                                try
                                {
                                    command.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    strError = "����������.\r\n"
                                        + ex.Message + "\r\n"
                                        + "SQL����:\r\n"
                                        + strLine;
                                    return -1;
                                }
                            }

                        } // end of using command

                        // 4.����¼����Ϊ0
                        this.SetTailNo(0);
                        this.m_bTailNoVerified = true;  // 2011/2/26
                        this.container.Changed = true;   //���ݸı�
                    }
                    finally
                    {
                        connection.Close();
                    }

                    // ɾ������Ŀ¼��Ȼ���ؽ�
                    try
                    {
                        if (string.IsNullOrEmpty(this.m_strObjectDir) == false)
                        {
                            PathUtil.DeleteDirectory(this.m_strObjectDir);
                            PathUtil.CreateDirIfNeed(this.m_strObjectDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        strError = "��� ���ݿ� '" + this.GetCaption("zh") + "' �� ԭ�ж���Ŀ¼ '" + this.m_strObjectDir + "' ʱ�������� " + ex.Message;
                        return -1;
                    }
                }
            }
            finally
            {
                //*********************�����ݿ��д��******
                m_db_lock.ReleaseWriterLock();
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("Initialize()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
            }
            return 0;
        }

        // ��ȡһ��SQL���ݿ����Ѿ����ڵ�records��keys����
        // ע�⣬�����Ѿ�ת��ΪСд���ߴ�д��̬
        // parameters:
        //      strStyle    Ҫ̽����Щ��keys��records����ϡ����൱��"keys,records"
        int GetExistTableNames(
            Connection connection,
            out List<string> table_names,
            out string strError)
        {
            strError = "";

            table_names = new List<string>();

            if (connection.SqlServerType == SqlServerType.MySql)
            {
                // string strCommand = "use `" + this.m_strSqlDbName + "` ;\n";

                string strCommand = "select table_name from information_schema.tables where table_schema = '" + this.m_strSqlDbName + "'; ";

                try
                {
                    using (MySqlCommand command = new MySqlCommand(strCommand,
        connection.MySqlConnection))
                    {
                        using (MySqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult))
                        {
                            if (dr != null
                && dr.HasRows == true)
                            {
                                while (dr.Read())
                                {
                                    if (dr.IsDBNull(0) == false)
                                        table_names.Add(dr.GetString(0).ToLower());
                                }
                            }
                        }

                    } // end of using command
                }
                catch (Exception ex)
                {
                    strError = "����ִ�ı���ʱ����: " + ex.Message;
                    return -1;
                }
            }
            else if (connection.SqlServerType == SqlServerType.Oracle)
            {
                string strCommand = " SELECT table_name FROM user_tables WHERE table_name like '" + this.m_strSqlDbName.ToUpper() + "_%'";

                try
                {
                    using (OracleCommand command = new OracleCommand(strCommand,
        connection.OracleConnection))
                    {
                        using (OracleDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult))
                        {
                            if (dr != null
                && dr.HasRows == true)
                            {
                                while (dr.Read())
                                {
                                    if (dr.IsDBNull(0) == false)
                                        table_names.Add(dr.GetString(0).ToUpper());
                                }
                            }
                        }

                    } // end of using command
                }
                catch (Exception ex)
                {
                    strError = "����ִ�ı���ʱ����: " + ex.Message;
                    return -1;
                }
            }

            return 0;
        }

        // ɾ��һ�����ݿ��е�ȫ����Ŀǰר����Oracle�汾
        // parameters:
        //      strStyle    Ҫɾ����Щ��keys��records����ϡ����൱��"keys,records"
        int DropAllTables(
            OracleConnection connection,
            string strSqlDbName,
            string strStyle,
            out string strError)
        {
            strError = "";

            List<string> table_names = new List<string>();

            // ��һ����������б���
            string strCommand = " SELECT table_name FROM user_tables WHERE table_name like '" + strSqlDbName.ToUpper() + "_%'";

            if (string.IsNullOrEmpty(strStyle) == true
                || (StringUtil.IsInList("keys", strStyle) == true && StringUtil.IsInList("records", strStyle) == true)
                )
            {
                // ɾ��ȫ��
                strCommand = " SELECT table_name FROM user_tables WHERE table_name like '" + strSqlDbName.ToUpper() + "_%'";
            }
            else if (StringUtil.IsInList("keys", strStyle) == true)
            {
                // ֻɾ��keys
                strCommand = " SELECT table_name FROM user_tables WHERE table_name like '" + strSqlDbName.ToUpper() + "_%' AND table_name <> '" + strSqlDbName.ToUpper() + "_RECORDS' ";
            }
            else if (StringUtil.IsInList("records", strStyle) == true)
            {
                // ֻɾ��records
                strCommand = " SELECT table_name FROM user_tables WHERE table_name == '" + strSqlDbName.ToUpper() + "_RECORDS' ";
            }

            using (OracleCommand command = new OracleCommand(strCommand,
                connection))
            {
                using (OracleDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult))
                {
                    if (dr != null
        && dr.HasRows == true)
                    {
                        while (dr.Read())
                        {
                            if (dr.IsDBNull(0) == false)
                                table_names.Add(dr.GetString(0));
                        }
                    }
                }

                // �ڶ�����ɾ����Щ��
                List<string> cmd_lines = new List<string>();
                foreach (string strTableName in table_names)
                {
                    cmd_lines.Add("DROP TABLE " + strTableName + " \n");
                }

                if (string.IsNullOrEmpty(strCommand) == false)
                {
                    foreach (string strLine in cmd_lines)
                    {
                        command.CommandText = strLine;
                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            strError = "ɾ�����ݿ� '" + strSqlDbName + "' �����б�ʱ����\r\n"
                                + ex.Message + "\r\n"
                                + "SQL����:\r\n"
                                + strLine;
                            return -1;
                        }
                    }
                }
            } // end of using command

            return 0;
        }

        // ��������Ϣ���Ƿ��ʾ�� ȫ������ 3701 errorcode ?
        static bool IsErrorCode3701(SqlException ex)
        {
            if (ex.Errors == null || ex.Errors.Count == 0)
                return false;
            foreach(SqlError error in ex.Errors)
            {
                if (error.Number == 5701)
                    continue;

                if (error.Number != 3701)
                    return false;
            }

            return true;    // ��ʾȫ������ 3701 error
        }

        // ����keys���index
        // parameters:
        //      strAction   delete/create/rebuild/disable/rebuildall/disableall
        public override int ManageKeysIndex(
            string strAction,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // 2013/3/2
            // ������û�м�����ʱ�����
            if (this.container.SqlServerType == SqlServerType.SQLite)
                this.Commit();

            //************�����ݿ��д��********************
            m_db_lock.AcquireWriterLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
			this.container.WriteDebugInfo("Refresh()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
            try
            {
                Connection connection = new Connection(this,
                    this.m_strConnString);
                connection.Open();
                try //����
                {
                    string strCommand = "";

                    if (strAction == "create"
                        || strAction == "rebuild"
                        || strAction == "rebuildall")
                    {
                        nRet = this.GetCreateIndexString(
                            "keys",
                            connection.SqlServerType,
                            strAction,
                            out strCommand,
                            out strError);
                    }
                    else
                    {
                        Debug.Assert(strAction == "delete"
                            || strAction == "disable"
                            || strAction == "disableall",
                            "");
                        nRet = this.GetDeleteIndexString(
                            connection.SqlServerType,
                            strAction,
                            out strCommand,
                            out strError);
                    }
                    if (nRet == -1)
                        return -1;

                    #region MS SQL Server
                    if (connection.SqlServerType == SqlServerType.MsSqlServer)
                    {
                        using (SqlCommand command = new SqlCommand(strCommand,
                            connection.SqlConnection))
                        {
                            try
                            {
                                command.CommandTimeout = 20 * 60;  // �ѳ�ʱʱ��Ŵ�

                                command.ExecuteNonQuery();
                            }
                            catch (SqlException ex)
                            {
                                // 2013/2/20
                                if (strAction == "delete"
                                    && IsErrorCode3701(ex) == true)
                                {
                                    return 0;
                                }
                                strError = "ˢ�±��� " + strAction + " ����.\r\n"
    + ex.Message + "\r\n"
    + "SQL����:\r\n"
    + strCommand;
                                return -1;
                            }
                            catch (Exception ex)
                            {
                                strError = "ˢ�±��� " + strAction + " ����.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }
                        } // end of using command
                    }
                    #endregion // MS SQL Server

                    #region SQLite
                    else if (connection.SqlServerType == SqlServerType.SQLite)
                    {
                        using (SQLiteCommand command = new SQLiteCommand(strCommand,
                            connection.SQLiteConnection))
                        {
                            try
                            {
                                command.CommandTimeout = 20 * 60;  // �ѳ�ʱʱ��Ŵ� 2008/11/20 new add

                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "ˢ�±��� " + strAction + " ����.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }
                        } // end of using command
                    }
                    #endregion // SQLite

                    #region MySql
                    else if (connection.SqlServerType == SqlServerType.MySql)
                    {

                        using (MySqlCommand command = new MySqlCommand(strCommand,
                            connection.MySqlConnection))
                        {
                            try
                            {
                                command.CommandTimeout = 20 * 60;  // �ѳ�ʱʱ��Ŵ� 2008/11/20 new add

                                command.ExecuteNonQuery();
                            }
                            catch (MySqlException ex)
                            {
                                if (strAction == "delete"
                                && ex.Number == 1091)
                                    return 0;
                                strError = "ˢ�±��� " + strAction + " ����.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }
                            catch (Exception ex)
                            {
                                strError = "ˢ�±��� " + strAction + " ����.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }
                        } // end of using command
                    }
                    #endregion // MySql

                    #region Oracle
                    else if (connection.SqlServerType == SqlServerType.Oracle)
                    {
                        using (OracleCommand command = new OracleCommand("",
                            connection.OracleConnection))
                        {
                            string[] lines = strCommand.Split(new char[] { ';' });
                            foreach (string line in lines)
                            {
                                string strLine = line.Trim();
                                if (string.IsNullOrEmpty(strLine) == true)
                                    continue;
                                try
                                {
                                    command.CommandText = strLine;
                                    command.CommandTimeout = 20 * 60;  // �ѳ�ʱʱ��Ŵ� 2008/11/20 new add

                                    command.ExecuteNonQuery();
                                }
                                catch (OracleException ex)
                                {
                                    if (strAction == "delete"
                                    && ex.Number == 1418)
                                        continue;
                                    strError = "ˢ�±��� " + strAction + " ����.\r\n"
                                        + ex.Message + "\r\n"
                                        + "SQL����:\r\n"
                                        + strLine;
                                    return -1;
                                }
                                catch (Exception ex)
                                {
                                    strError = "ˢ�±��� " + strAction + " ����.\r\n"
                                        + ex.Message + "\r\n"
                                        + "SQL����:\r\n"
                                        + strLine;
                                    return -1;
                                }
                            }
                        } // end of using command
                    }
                    #endregion // Oracle
                }
                finally
                {
                    connection.Close();
                }
            }
            finally
            {
                //*********************�����ݿ��д��******
                m_db_lock.ReleaseWriterLock();
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("Refresh()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
            }


            return 0;
        }

        // 2008/11/14
        // ˢ�����SQL���ݿ�ı��壬ע���麯������Ϊprivate
        // parameters:
        //      bClearAllKeyTables �Ƿ�˳��Ҫɾ������keys���е�����?
        //		strError    out���������س�����Ϣ
        // return:
        //		-1  ����
        //		0   �ɹ�
        // ��: ��ȫ��
        // ��д����ԭ��? �޸ļ�¼β���Ѿ�ȥ�����ƺ����Բ�������
        public override int RefreshPhysicalDatabase(
            bool bClearAllKeyTables,
            out string strError)
        {
            strError = "";

            //************�����ݿ��д��********************
            m_db_lock.AcquireWriterLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
			this.container.WriteDebugInfo("Refresh()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
            try
            {
                Connection connection = new Connection(this,
                    this.m_strConnString);
                connection.Open();
                try //����
                {
                    string strCommand = "";

                    // ˢ�±���
                    int nRet = this.GetRefreshTablesString(
                        connection.SqlServerType,
                        bClearAllKeyTables,
                        null,
                        out strCommand,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    if (connection.SqlServerType == SqlServerType.MsSqlServer)
                    {
                        using (SqlCommand command = new SqlCommand(strCommand,
                            connection.SqlConnection))
                        {
                            try
                            {
                                command.CommandTimeout = 20 * 60;  // �ѳ�ʱʱ��Ŵ� 2008/11/20 new add

                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "ˢ�±������.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }
                        } // end of using command
                    }
                    else if (connection.SqlServerType == SqlServerType.SQLite)
                    {
                        using (SQLiteCommand command = new SQLiteCommand(strCommand,
                            connection.SQLiteConnection))
                        {
                            try
                            {
                                command.CommandTimeout = 20 * 60;  // �ѳ�ʱʱ��Ŵ� 2008/11/20 new add

                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "ˢ�±������.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }
                        } // end of using command
                    }
                    else if (connection.SqlServerType == SqlServerType.MySql)
                    {
                        if (bClearAllKeyTables == false)
                        {
                            List<string> table_names = null;

                            // ��ȡһ��SQL���ݿ����Ѿ����ڵ�records��keys����
                            nRet = GetExistTableNames(
                                connection,
                                out table_names,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            // ˢ�±���
                            nRet = this.GetRefreshTablesString(
                                connection.SqlServerType,
                                bClearAllKeyTables,
                                table_names,
                                out strCommand,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }

                        using (MySqlCommand command = new MySqlCommand(strCommand,
                            connection.MySqlConnection))
                        {
                            try
                            {
                                command.CommandTimeout = 20 * 60;  // �ѳ�ʱʱ��Ŵ� 2008/11/20 new add

                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "ˢ�±������.\r\n"
                                    + ex.Message + "\r\n"
                                    + "SQL����:\r\n"
                                    + strCommand;
                                return -1;
                            }
                        } // end of using command
                    }
                    else if (connection.SqlServerType == SqlServerType.Oracle)
                    {
                        if (bClearAllKeyTables == true)
                        {
                            nRet = DropAllTables(
                                connection.OracleConnection,
                                this.m_strSqlDbName,
                                "keys",
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }
                        else
                        {
                            List<string> table_names = null;

                            // ��ȡһ��SQL���ݿ����Ѿ����ڵ�records��keys����
                            nRet = GetExistTableNames(
                                connection,
                                out table_names,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            // ˢ�±���
                            nRet = this.GetRefreshTablesString(
                                connection.SqlServerType,
                                bClearAllKeyTables,
                                table_names,
                                out strCommand,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }

                        using (OracleCommand command = new OracleCommand("",
                            connection.OracleConnection))
                        {
                            string[] lines = strCommand.Split(new char[] { ';' });
                            foreach (string line in lines)
                            {
                                string strLine = line.Trim();
                                if (string.IsNullOrEmpty(strLine) == true)
                                    continue;
                                try
                                {
                                    command.CommandText = strLine;
                                    command.CommandTimeout = 20 * 60;  // �ѳ�ʱʱ��Ŵ� 2008/11/20 new add

                                    command.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    strError = "ˢ�±������.\r\n"
                                        + ex.Message + "\r\n"
                                        + "SQL����:\r\n"
                                        + strLine;
                                    return -1;
                                }
                            }
                        } // end of using command
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
            finally
            {
                //*********************�����ݿ��д��******
                m_db_lock.ReleaseWriterLock();
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("Refresh()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
            }

            return 0;
        }


        // �õ����������ַ���
        public string GetCreateDbCmdString(SqlServerType server_type)
        {
            if (server_type == SqlServerType.MsSqlServer)
            {
                string strCommand = "use master " + "\n"
                    + " if exists (select * from dbo.sysdatabases where name = N'" + this.m_strSqlDbName + "')" + "\n"
                    + " drop database " + this.m_strSqlDbName + "\n"
                    + " CREATE database " + this.m_strSqlDbName + "\n";

                strCommand += " use master " + "\n";

                return strCommand;
            }
            else if (server_type == SqlServerType.SQLite)
            {
                // ע: SQLiteû�д������ݿ�Ĳ��裬ֱ�Ӵ�����Ϳ�����
                return "";
            }
            else if (server_type == SqlServerType.MySql)
            {
                string strCommand = 
                    " DROP DATABASE IF EXISTS `" + this.m_strSqlDbName + "`; \n"
                    + " CREATE DATABASE IF NOT EXISTS `" + this.m_strSqlDbName + "`;\n";
                return strCommand;
            }
            else if (server_type == SqlServerType.Oracle)
            {
                /*
                string strCommand =
                    " DROP DATABASE IF EXISTS " + this.m_strSqlDbName + "; \n"
                    + " CREATE DATABASE " + this.m_strSqlDbName + " \n"
                    + " CONTROLFILE REUSE " 
                    + " LOGFILE "
                    + " group 1 ('" + PathUtil.MergePath(this.m_strObjectDir, "redo1.log") + "') size 10M, "
                    + " group 2 ('" + PathUtil.MergePath(this.m_strObjectDir, "redo2.log") + "') size 10M,"
                    + " group 3 ('" + PathUtil.MergePath(this.m_strObjectDir, "redo3.log") + "') size 10M "
                    + " CHARACTER SET AL32UTF8"
                    + " NATIONAL CHARACTER SET AL16UTF16"
                    + " DATAFILE  "
                    + " '" + PathUtil.MergePath(this.m_strObjectDir, "database.dbf") + "' "
                    + "       size 50M"
                    + "       autoextend on "
                    + "       next 10M maxsize unlimited"
                    + "       extent management local"
                    + " DEFAULT TEMPORARY TABLESPACE temp_ts"
                    + " UNDO TABLESPACE undo_ts ; ";
                return strCommand;
                 * */
                return "";
            }

            return "";
        }

        // �õ����������ַ���
        // return
        //		-1	����
        //		0	�ɹ�
        private int GetCreateTablesString(
            SqlServerType strSqlServerType,
            out string strCommand,
            out string strError)
        {
            strCommand = "";
            strError = "";

            #region MS SQL Server
            if (strSqlServerType == SqlServerType.MsSqlServer)
            {
                // ����records��
                strCommand = "use " + this.m_strSqlDbName + "\n"
                    + "if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[records]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)" + "\n"
                    + "drop table [dbo].[records]" + "\n"
                    + "CREATE TABLE [dbo].[records]" + "\n"
                    + "(" + "\n"
                    + "[id] [nvarchar] (255) NULL UNIQUE," + "\n"
                    + "[data] [image] NULL ," + "\n"
                    + "[newdata] [image] NULL ," + "\n"
                    + "[range] [nvarchar] (4000) NULL," + "\n"
                    + "[dptimestamp] [nvarchar] (100) NULL ," + "\n"
                    + "[newdptimestamp] [nvarchar] (100) NULL ," + "\n"   // 2012/1/19
                    + "[metadata] [nvarchar] (4000) NULL ," + "\n"
                    + "[filename] [nvarchar] (255) NULL, \n"
                    + "[newfilename] [nvarchar] (255) NULL\n"
                    + ") ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]" + "\n" + "\n";
                // UNIQUEΪ2008/3/13�¼���

                KeysCfg keysCfg = null;
                int nRet = this.GetKeysCfg(out keysCfg,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (keysCfg != null)
                {

                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;


                    // ���������
                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = aTableInfo[i];

                        strCommand += "\n" +
                            "if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[" + tableInfo.SqlTableName + "]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)" + "\n" +
                            "drop table [dbo].[" + tableInfo.SqlTableName + "]" + "\n" +
                            "CREATE TABLE [dbo].[" + tableInfo.SqlTableName + "]" + "\n" +
                            "(" + "\n" +
                            "[keystring] [nvarchar] (" + Convert.ToString(this.KeySize) + ") Null," + "\n" +         //keystring�ĳ����������ļ���
                            "[fromstring] [nvarchar] (255) NULL ," + "\n" +
                            "[idstring] [nvarchar] (255)  NULL ," + "\n" +
                            "[keystringnum] [bigint] NULL " + "\n" +
                            ")" + "\n" + "\n";
                    }
                }

                strCommand += " use master " + "\n";
                return 0;
            }
            #endregion // MS SQL Server

            #region SQLite
            else if (strSqlServerType == SqlServerType.SQLite)
            {
                // ����records��
                strCommand = "CREATE TABLE records "
                    + "(" + " "
                    + "id nvarchar (255) NULL UNIQUE," + " "
                    + "range nvarchar (4000) NULL," + " "
                    + "dptimestamp nvarchar (100) NULL ," + " "
                    + "newdptimestamp nvarchar (100) NULL ," + " "   // 2012/1/19
                    + "metadata nvarchar (4000) NULL ," + " "
                    + "filename nvarchar (255) NULL,  "
                    + "newfilename nvarchar (255) NULL "
                    + ") ; ";

                KeysCfg keysCfg = null;
                int nRet = this.GetKeysCfg(out keysCfg,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (keysCfg != null)
                {

                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;


                    // ���������
                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = aTableInfo[i];

                        strCommand += " " +
                            "CREATE TABLE " + tableInfo.SqlTableName + " " +
                            "(" + " " +
                            "keystring nvarchar (" + Convert.ToString(this.KeySize) + ") NULL," + " " +         //keystring�ĳ����������ļ���
                            "fromstring nvarchar (255) NULL ," + " " +
                            "idstring nvarchar (255)  NULL ," + " " +
                            "keystringnum bigint NULL " + " " +
                            ")" + " ; ";
                    }
                }

                return 0;
            }
            #endregion // SQLite 

            #region MySql
            else if (strSqlServerType == SqlServerType.MySql)
            {
                string strCharset = " CHARACTER SET utf8 "; // COLLATE utf8_bin ";

                // ����records��
                strCommand = // "use `" + this.m_strSqlDbName + "` ;\n" +
                    "DROP TABLE IF EXISTS `" + this.m_strSqlDbName + "`.records" + " ;\n"
                    + "CREATE TABLE `" + this.m_strSqlDbName + "`.records" + " \n"
                    + "(" + "\n"
                    + "id varchar (255) "+strCharset+" NULL UNIQUE," + "\n"
                    + "`range` varchar (4000) " + strCharset + " NULL," + "\n"
                    + "dptimestamp varchar (100) " + strCharset + " NULL ," + "\n"
                    + "newdptimestamp varchar (100) " + strCharset + " NULL ," + "\n"
                    + "metadata varchar (4000) " + strCharset + " NULL ," + "\n"
                    + "filename varchar (255) " + strCharset + " NULL, \n"
                    + "newfilename varchar (255) " + strCharset + " NULL\n"
                    + ") ;\n";

                KeysCfg keysCfg = null;
                int nRet = this.GetKeysCfg(out keysCfg,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (keysCfg != null)
                {

                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // ���������
                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = aTableInfo[i];

                        strCommand += "\n" +
                            "DROP TABLE IF EXISTS `" + this.m_strSqlDbName + "`." + tableInfo.SqlTableName + "" + " ;\n" +
                            "CREATE TABLE `" + this.m_strSqlDbName + "`." + tableInfo.SqlTableName + "\n" +
                            "(" + "\n" +
                            "keystring varchar (" + Convert.ToString(this.KeySize) + ") " + strCharset + " NULL," + "\n" +         //keystring�ĳ����������ļ���
                            "fromstring varchar (255) " + strCharset + " NULL ," + "\n" +
                            "idstring varchar (255) " + strCharset + " NULL ," + "\n" +
                            "keystringnum bigint NULL " + "\n" +
                            ")" + " ;\n";
                    }
                }
                return 0;
            }
            #endregion // MySql

            #region Oracle
            else if (strSqlServerType == SqlServerType.Oracle)
            {
                // ����records��
                strCommand = "CREATE TABLE "+this.m_strSqlDbName+"_records " + "\n"
                    + "(" + "\n"
                    + "id nvarchar2 (255) NULL UNIQUE," + "\n"
                    + "range nvarchar2 (2000) NULL," + "\n"
                    + "dptimestamp nvarchar2 (100) NULL ," + "\n"
                    + "newdptimestamp nvarchar2 (100) NULL ," + "\n"
                    + "metadata nvarchar2 (2000) NULL ," + "\n"
                    + "filename nvarchar2 (255) NULL, \n"
                    + "newfilename nvarchar2 (255) NULL\n"
                    + ") \n";

                string strTemp = this.m_strSqlDbName + "_" + "_records";
                if (strTemp.Length > 30)
                {
                    strError = "������ '" + strTemp + "' ���ַ������� 30����ʹ�ø��̵� SQL ���ݿ�����";
                    return -1;
                }

                KeysCfg keysCfg = null;
                int nRet = this.GetKeysCfg(out keysCfg,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (keysCfg != null)
                {
                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // ���������
                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = aTableInfo[i];

                        if (string.IsNullOrEmpty(strCommand) == false)
                            strCommand += " ; ";

                        // TODO Ҫ��ֹkeys������recordsײ��

                        strTemp = this.m_strSqlDbName + "_" + tableInfo.SqlTableName;
                        if (strTemp.Length > 30)
                        {
                            strError = "������ '" + strTemp + "' ���ַ������� 30����ʹ�ø��̵� SQL ���ݿ�����";
                            return -1;
                        }

                        // int16 number(5)
                        // int32 number(10)
                        // int64 number(19)

                        strCommand += " CREATE TABLE " + this.m_strSqlDbName + "_" + tableInfo.SqlTableName + " " + "\n" +
                            "(" + "\n" +
                            "keystring nvarchar2 (" + Convert.ToString(this.KeySize) + ") NULL," + "\n" +
                            "fromstring nvarchar2 (255) NULL ," + "\n" +
                            "idstring nvarchar2 (255)  NULL ," + "\n" +
                            "keystringnum NUMBER(19) NULL " + "\n" +
                            ")" + " \n";
                    }
                }
                return 0;
            }
            #endregion // Oracle

            return 0;
        }

        // �õ�ˢ�±��������ַ���
        // �������µ�keys���壬����������Щû�б�������SQL��
        // ע���Ѿ������˴���SQL���������
        // parameters:
        //      bClearAllKeyTables �Ƿ�˳��Ҫɾ������keys���е�����?
        // return
        //		-1	����
        //		0	�ɹ�
        private int GetRefreshTablesString(
            SqlServerType server_type,
            bool bClearAllKeyTables,
            List<string> existing_tablenames,
            out string strCommand,
            out string strError)
        {
            strCommand = "";
            strError = "";

            KeysCfg keysCfg = null;
            int nRet = this.GetKeysCfg(out keysCfg,
                out strError);
            if (nRet == -1)
                return -1;

            if (server_type == SqlServerType.MsSqlServer)
            {

                strCommand = "use " + this.m_strSqlDbName + "\n";

                if (keysCfg != null)
                {

                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // ���������������
                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = aTableInfo[i];

                        if (bClearAllKeyTables == true)
                        {
                            // ������Ѿ����ڣ�����drop�ٴ���
                            strCommand += "\n" +
                                "if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[" + tableInfo.SqlTableName + "]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)" + "\n" +
                                "DROP TABLE [dbo].[" + tableInfo.SqlTableName + "]" + "\n" +
                                "\n" +
                                "CREATE TABLE [dbo].[" + tableInfo.SqlTableName + "]" + "\n" +
                                "(" + "\n" +
                                "[keystring] [nvarchar] (" + Convert.ToString(this.KeySize) + ") Null," + "\n" +         //keystring�ĳ����������ļ���
                                "[fromstring] [nvarchar] (255) NULL ," + "\n" +
                                "[idstring] [nvarchar] (255)  NULL ," + "\n" +
                                "[keystringnum] [bigint] NULL " + "\n" +
                                ")" + "\n" + "\n";

                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystring_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEY_COL_LIST + " \n";
                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystringnum_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEYNUM_COL_LIST + " \n";
                            // 2008/11/20 new add
                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_idstring_index \n"
                                + " ON " + tableInfo.SqlTableName + " (idstring) \n";
                        }
                        else
                        {
                            // �����ڲŴ���
                            strCommand += "\n" +
                                "if not exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[" + tableInfo.SqlTableName + "]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)" + "\n" +
                                "BEGIN\n" +
                                "CREATE TABLE [dbo].[" + tableInfo.SqlTableName + "]" + "\n" +
                                "(" + "\n" +
                                "[keystring] [nvarchar] (" + Convert.ToString(this.KeySize) + ") Null," + "\n" +         //keystring�ĳ����������ļ���
                                "[fromstring] [nvarchar] (255) NULL ," + "\n" +
                                "[idstring] [nvarchar] (255)  NULL ," + "\n" +
                                "[keystringnum] [bigint] NULL " + "\n" +
                                ")" + "\n" + "\n";

                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystring_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEY_COL_LIST + " \n";
                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystringnum_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEYNUM_COL_LIST + " \n";
                            // 2008/11/20 new add
                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_idstring_index \n"
                                + " ON " + tableInfo.SqlTableName + " (idstring) \n";
                            strCommand += "END\n";
                        }
                    }
                }

                strCommand += " use master " + "\n";

                return 0;
            }
            else if (server_type == SqlServerType.SQLite)
            {
                strCommand = "";

                if (keysCfg != null)
                {

                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // ���������������
                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = aTableInfo[i];

                        if (bClearAllKeyTables == true)
                        {
                            // ������Ѿ����ڣ�����drop�ٴ���
                            strCommand += "DROP TABLE if exists " + tableInfo.SqlTableName + " ;\n"
                                + "CREATE TABLE " + tableInfo.SqlTableName + " \n" +
                                "(" + "\n" +
                                "[keystring] [nvarchar] (" + Convert.ToString(this.KeySize) + ") NULL," + "\n" +         //keystring�ĳ����������ļ���
                                "[fromstring] [nvarchar] (255) NULL ," + "\n" +
                                "[idstring] [nvarchar] (255)  NULL ," + "\n" +
                                "[keystringnum] [bigint] NULL " + "\n" +
                                ")" + " ;\n";

                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystring_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEY_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystringnum_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEYNUM_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_idstring_index \n"
                                + " ON " + tableInfo.SqlTableName + " (idstring) ;\n";
                        }
                        else
                        {
                            // �����ڲŴ���
                            strCommand += 
                                "CREATE TABLE if not exists " + tableInfo.SqlTableName + " \n" +
                                "(" + "\n" +
                                "[keystring] [nvarchar] (" + Convert.ToString(this.KeySize) + ") NULL," + "\n" +         //keystring�ĳ����������ļ���
                                "[fromstring] [nvarchar] (255) NULL ," + "\n" +
                                "[idstring] [nvarchar] (255)  NULL ," + "\n" +
                                "[keystringnum] [bigint] NULL " + "\n" +
                                ")" + " ;\n";

                            strCommand += " CREATE INDEX if not exists " + tableInfo.SqlTableName + "_keystring_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEY_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX if not exists " + tableInfo.SqlTableName + "_keystringnum_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEYNUM_COL_LIST + " ;\n";
                            // 2008/11/20 new add
                            strCommand += " CREATE INDEX if not exists " + tableInfo.SqlTableName + "_idstring_index \n"
                                + " ON " + tableInfo.SqlTableName + " (idstring) ;\n";
                        }
                    }
                }

                return 0;
            }
            else if (server_type == SqlServerType.MySql)
            {
                strCommand = "use `" + this.m_strSqlDbName + "` ;\n";
                string strCharset = " CHARACTER SET utf8 "; // COLLATE utf8_bin ";

                if (keysCfg != null)
                {
                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // ���������������
                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = aTableInfo[i];

                        if (bClearAllKeyTables == true)
                        {
                            // ������Ѿ����ڣ�����drop�ٴ���
                            strCommand +=
                                "DROP TABLE if exists `" + tableInfo.SqlTableName + "` ;\n"
                                + "CREATE TABLE `" + tableInfo.SqlTableName + "` \n" +
                                "(" + "\n" +
                                "keystring varchar (" + Convert.ToString(this.KeySize) + ") " + strCharset + " NULL," + "\n" +         //keystring�ĳ����������ļ���
                                "fromstring varchar (255) " + strCharset + " NULL ," + "\n" +
                                "idstring varchar (255) " + strCharset + " NULL ," + "\n" +
                                "keystringnum bigint NULL " + "\n" +
                                ")" + " ;\n";

                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystring_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEY_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystringnum_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEYNUM_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_idstring_index \n"
                                + " ON " + tableInfo.SqlTableName + " (idstring) ;\n";
                        }
                        else
                        {
                            if (existing_tablenames != null
                                && existing_tablenames.IndexOf(tableInfo.SqlTableName.ToLower()) != -1)
                                continue;

                            // �����ڲŴ���
                            strCommand +=
                                "CREATE TABLE if not exists `" + tableInfo.SqlTableName + "` \n" +
                                "(" + "\n" +
                                "keystring varchar (" + Convert.ToString(this.KeySize) + ") " + strCharset + " NULL," + "\n" +         //keystring�ĳ����������ļ���
                                "fromstring varchar (255) " + strCharset + " NULL ," + "\n" +
                                "idstring varchar (255) " + strCharset + " NULL ," + "\n" +
                                "keystringnum bigint NULL " + "\n" +
                                ")" + " ;\n";

                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystring_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEY_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystringnum_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEYNUM_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_idstring_index \n"
                                + " ON " + tableInfo.SqlTableName + " (idstring) ;\n";
                        }
                    }
                }

                return 0;
            }
            else if (server_type == SqlServerType.Oracle)
            {
                strCommand = "";    //  "use `" + this.m_strSqlDbName + "` ;\n";

                // bClearAllKeyTables==true����Ҫͨ���ڵ��ñ�����ǰɾ��ȫ������ʵ��

                if (keysCfg != null)
                {
                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // ���������������
                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = aTableInfo[i];
                        string strTableName = (this.m_strSqlDbName + "_" + tableInfo.SqlTableName).ToUpper();

                        if (existing_tablenames != null
    && existing_tablenames.IndexOf(strTableName) != -1)
                            continue;

                        strCommand += // "IF NOT EXISTS ( SELECT table_name FROM user_tables WHERE table_name = '" + strTableName + "' ) " + 
                            "CREATE TABLE " + strTableName + " \n" +
                            "(" + "\n" +
                            "keystring nvarchar2 (" + Convert.ToString(this.KeySize) + ") NULL," + "\n" +         //keystring�ĳ����������ļ���
                            "fromstring nvarchar2 (255) NULL ," + "\n" +
                            "idstring nvarchar2 (255)  NULL ," + "\n" +
                            "keystringnum NUMBER(19) NULL " + "\n" +
                            ")" + " ;\n";

                        string strTemp = strTableName + "ki";
                        if (strTemp.Length > 30)
                        {
                            strError = "�������� '" + strTemp + "' ���ַ������� 30����ʹ�ø��̵� SQL ���ݿ�����";
                            return -1;
                        }

                        strCommand += " CREATE INDEX " + strTableName + "ki \n"
                            + " ON " + strTableName + " " + KEY_COL_LIST + " ;\n";
                        strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "ni \n"
                            + " ON " + strTableName + " " + KEYNUM_COL_LIST + " ;\n";
                        strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "ii \n"
                            + " ON " + strTableName + " (idstring) ;\n";
                    }
                }

                return 0;
            }

            return 0;
        }

        // �����������ַ���
        // parameters:
        //      strIndexTYpeList    keys,records
        //                          key��ʾ���� keys �������, records ��ʾ���� records �������
        //      strAction   create / rebuild / rebuildall
        // return
        //		-1	����
        //		0	�ɹ�
        public int GetCreateIndexString(
            string strIndexTypeList,
            SqlServerType strSqlServerType,
            string strAction,
            out string strCommand,
            out string strError)
        {
            strCommand = "";
            strError = "";

            if (string.IsNullOrEmpty(strIndexTypeList) == true)
                strIndexTypeList = "keys,records";

            if (string.IsNullOrEmpty(strAction) == true)
                strAction = "create";

            #region MS SQL Server
            if (strSqlServerType == SqlServerType.MsSqlServer)
            {
                strCommand = "use " + this.m_strSqlDbName + "\n";
                if (StringUtil.IsInList("records", strIndexTypeList) == true)
                {
                    if (strAction == "create")
                    {
                        strCommand += " CREATE INDEX records_id_index " + "\n"
                            + " ON records (id) \n";
                    }
                    else if (strAction == "rebuild")
                    {
                        strCommand += " ALTER INDEX records_id_index " + "\n"
                            + " ON records REBUILD \n";
                    }
                    else if (strAction == "rebuildall")
                    {
                        strCommand += " ALTER INDEX ALL " + "\n"
                            + " ON records REBUILD \n";
                    }
                }

                if (StringUtil.IsInList("keys", strIndexTypeList) == true)
                {
                    KeysCfg keysCfg = null;
                    int nRet = this.GetKeysCfg(out keysCfg,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (keysCfg != null)
                    {
                        List<TableInfo> aTableInfo = null;
                        nRet = keysCfg.GetTableInfosRemoveDup(
                            out aTableInfo,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        if (strAction == "create")
                        {
                            for (int i = 0; i < aTableInfo.Count; i++)
                            {
                                TableInfo tableInfo = (TableInfo)aTableInfo[i];

                                strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystring_index \n"
                                    + " ON " + tableInfo.SqlTableName + " " + KEY_COL_LIST + " \n";
                                strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystringnum_index \n"
                                    + " ON " + tableInfo.SqlTableName + " " + KEYNUM_COL_LIST + " \n";
                                // 2008/11/20 new add
                                strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_idstring_index \n"
                                    + " ON " + tableInfo.SqlTableName + " (idstring) \n";
                            }
                        }
                        else if (strAction == "rebuild")
                        {
                            for (int i = 0; i < aTableInfo.Count; i++)
                            {
                                TableInfo tableInfo = (TableInfo)aTableInfo[i];

                                strCommand += " ALTER INDEX " + tableInfo.SqlTableName + "_keystring_index \n"
                                    + " ON " + tableInfo.SqlTableName + " REBUILD \n";
                                strCommand += " ALTER INDEX " + tableInfo.SqlTableName + "_keystringnum_index \n"
                                    + " ON " + tableInfo.SqlTableName + " REBUILD \n";
                                strCommand += " ALTER INDEX " + tableInfo.SqlTableName + "_idstring_index \n"
                                    + " ON " + tableInfo.SqlTableName + " REBUILD \n";
                            }
                        }
                        else if (strAction == "rebuildall")
                        {
                            for (int i = 0; i < aTableInfo.Count; i++)
                            {
                                TableInfo tableInfo = (TableInfo)aTableInfo[i];

                                strCommand += " ALTER INDEX ALL \n"
                                    + " ON " + tableInfo.SqlTableName + " REBUILD \n";
                            }
                        }
                    }
                }

                strCommand += " use master " + "\n";
            }
            #endregion MS SQL Server

            #region SQLite
            else if (strSqlServerType == SqlServerType.SQLite)
            {
                if (StringUtil.IsInList("records", strIndexTypeList) == true)
                {
                    strCommand = "CREATE INDEX records_id_index " + "\n"
                        + " ON records (id) ;\n";
                }

                if (StringUtil.IsInList("keys", strIndexTypeList) == true)
                {
                    KeysCfg keysCfg = null;
                    int nRet = this.GetKeysCfg(out keysCfg,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (keysCfg != null)
                    {
                        List<TableInfo> aTableInfo = null;
                        nRet = keysCfg.GetTableInfosRemoveDup(
                            out aTableInfo,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        for (int i = 0; i < aTableInfo.Count; i++)
                        {
                            TableInfo tableInfo = (TableInfo)aTableInfo[i];

                            strCommand += " CREATE INDEX IF NOT EXISTS " + tableInfo.SqlTableName + "_keystring_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEY_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX IF NOT EXISTS " + tableInfo.SqlTableName + "_keystringnum_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEYNUM_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX IF NOT EXISTS " + tableInfo.SqlTableName + "_idstring_index \n"
                                + " ON " + tableInfo.SqlTableName + " (idstring) ;\n";
                        }
                    }
                }
            }
            #endregion // SQLite 

            #region MySql
            else if (strSqlServerType == SqlServerType.MySql)
            {
                strCommand = "use " + this.m_strSqlDbName + " ;\n";
                if (StringUtil.IsInList("records", strIndexTypeList) == true)
                {
                    strCommand += " CREATE INDEX records_id_index " + "\n"
    + " ON records (id) ;\n";
                }

                if (StringUtil.IsInList("keys", strIndexTypeList) == true)
                {
                    KeysCfg keysCfg = null;
                    int nRet = this.GetKeysCfg(out keysCfg,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (keysCfg != null)
                    {
                        List<TableInfo> aTableInfo = null;
                        nRet = keysCfg.GetTableInfosRemoveDup(
                            out aTableInfo,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        for (int i = 0; i < aTableInfo.Count; i++)
                        {
                            TableInfo tableInfo = (TableInfo)aTableInfo[i];

                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystring_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEY_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_keystringnum_index \n"
                                + " ON " + tableInfo.SqlTableName + " " + KEYNUM_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX " + tableInfo.SqlTableName + "_idstring_index \n"
                                + " ON " + tableInfo.SqlTableName + " (idstring) ;\n";
                        }
                    }
                }
            }
            #endregion // MySql

            #region Oracle
            else if (strSqlServerType == SqlServerType.Oracle)
            {
                /*
                strCommand = " CREATE INDEX " + this.m_strSqlDbName + "_records_ii " + "\n"
                    + " ON "+this.m_strSqlDbName+"_records (id) \n";
                 * */
                // records���id���Ѿ��������ˣ���Ϊ����UNIQUE
                strCommand = "";

                if (StringUtil.IsInList("keys", strIndexTypeList) == true)
                {
                    KeysCfg keysCfg = null;
                    int nRet = this.GetKeysCfg(out keysCfg,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (keysCfg != null)
                    {
                        List<TableInfo> aTableInfo = null;
                        nRet = keysCfg.GetTableInfosRemoveDup(
                            out aTableInfo,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        for (int i = 0; i < aTableInfo.Count; i++)
                        {
                            TableInfo tableInfo = (TableInfo)aTableInfo[i];
                            string strTableName = (this.m_strSqlDbName + "_" + tableInfo.SqlTableName).ToUpper();

                            //if (string.IsNullOrEmpty(strCommand) == false)
                            //    strCommand += " ; ";

                            string strTemp = strTableName + "ki";
                            if (strTemp.Length > 30)
                            {
                                strError = "�������� '" + strTemp + "' ���ַ������� 30����ʹ�ø��̵� SQL ���ݿ�����";
                                return -1;
                            }

                            strCommand += " CREATE INDEX " + strTableName + "ki \n"
                                + " ON " + strTableName + " " + KEY_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX " + strTableName + "ni \n"
                                + " ON " + strTableName + " " + KEYNUM_COL_LIST + " ;\n";
                            strCommand += " CREATE INDEX " + strTableName + "ii \n"
                                + " ON " + strTableName + " (idstring) ;\n";
                        }
                    }
                }
            }
            #endregion // Oracle

            return 0;
        }

        // ɾ��keys�����������ַ���
        // parameters:
        //      strAction   delete / disable / disableall
        // return
        //		-1	����
        //		0	�ɹ�
        public int GetDeleteIndexString(
            SqlServerType strSqlServerType,
            string strAction,
            out string strCommand,
            out string strError)
        {
            strCommand = "";
            strError = "";

            if (string.IsNullOrEmpty(strAction) == true)
                strAction = "delete";

            #region MS SQL Server
            if (strSqlServerType == SqlServerType.MsSqlServer)
            {
                strCommand = "use " + this.m_strSqlDbName + "\n";

                KeysCfg keysCfg = null;
                int nRet = this.GetKeysCfg(out keysCfg,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (keysCfg != null)
                {
                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    if (strAction == "delete")
                    {
                        for (int i = 0; i < aTableInfo.Count; i++)
                        {
                            TableInfo tableInfo = (TableInfo)aTableInfo[i];

                            strCommand += " DROP INDEX " + tableInfo.SqlTableName + "_keystring_index \n"
                                + " ON " + tableInfo.SqlTableName + " \n";
                            strCommand += " DROP INDEX " + tableInfo.SqlTableName + "_keystringnum_index \n"
                                + " ON " + tableInfo.SqlTableName + " \n";
                            strCommand += " DROP INDEX " + tableInfo.SqlTableName + "_idstring_index \n"
                                + " ON " + tableInfo.SqlTableName + " \n";
                        }
                    }
                    else if (strAction == "disable")
                    {
                        for (int i = 0; i < aTableInfo.Count; i++)
                        {
                            TableInfo tableInfo = (TableInfo)aTableInfo[i];

                            strCommand += " ALTER INDEX " + tableInfo.SqlTableName + "_keystring_index \n"
                                + " ON " + tableInfo.SqlTableName + " DISABLE \n";
                            strCommand += " ALTER INDEX " + tableInfo.SqlTableName + "_keystringnum_index \n"
                                + " ON " + tableInfo.SqlTableName + " DISABLE \n";
                            strCommand += " ALTER INDEX " + tableInfo.SqlTableName + "_idstring_index \n"
                                + " ON " + tableInfo.SqlTableName + " DISABLE \n";
                        }
                    }
                    else if (strAction == "disableall")
                    {
                        for (int i = 0; i < aTableInfo.Count; i++)
                        {
                            TableInfo tableInfo = (TableInfo)aTableInfo[i];

                            strCommand += " ALTER INDEX ALL \n"
                                + " ON " + tableInfo.SqlTableName + " DISABLE \n";
                        }
                    }
                }

                strCommand += " use master " + "\n";
            }
            #endregion // MS SQL Server

            #region SQLite
            else if (strSqlServerType == SqlServerType.SQLite)
            {
                strCommand = "";

                KeysCfg keysCfg = null;
                int nRet = this.GetKeysCfg(out keysCfg,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (keysCfg != null)
                {
                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = (TableInfo)aTableInfo[i];

                        strCommand += " DROP INDEX IF EXISTS " + tableInfo.SqlTableName + "_keystring_index ;\n";
                        strCommand += " DROP INDEX IF EXISTS " + tableInfo.SqlTableName + "_keystringnum_index ;\n";
                        strCommand += " DROP INDEX IF EXISTS " + tableInfo.SqlTableName + "_idstring_index ;\n";
                    }
                }
            }
            #endregion // SQLite

            #region MySql
            else if (strSqlServerType == SqlServerType.MySql)
            {
                strCommand = "use " + this.m_strSqlDbName + " ;\n";

                KeysCfg keysCfg = null;
                int nRet = this.GetKeysCfg(out keysCfg,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (keysCfg != null)
                {
                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = (TableInfo)aTableInfo[i];

                        strCommand += " DROP INDEX " + tableInfo.SqlTableName + "_keystring_index \n"
                            + " ON " + tableInfo.SqlTableName + " ;\n";
                        strCommand += " DROP INDEX " + tableInfo.SqlTableName + "_keystringnum_index \n"
                            + " ON " + tableInfo.SqlTableName + " ;\n";
                        strCommand += " DROP INDEX " + tableInfo.SqlTableName + "_idstring_index \n"
                            + " ON " + tableInfo.SqlTableName + " ;\n";
                    }
                }
            }
            #endregion // MySql

            #region Oracle
            else if (strSqlServerType == SqlServerType.Oracle)
            {
                strCommand = "";

                KeysCfg keysCfg = null;
                int nRet = this.GetKeysCfg(out keysCfg,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (keysCfg != null)
                {
                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = (TableInfo)aTableInfo[i];
                        string strTableName = (this.m_strSqlDbName + "_" + tableInfo.SqlTableName).ToUpper();

                        //if (string.IsNullOrEmpty(strCommand) == false)
                        //    strCommand += " ; ";

                        string strTemp = strTableName + "ki";
                        if (strTemp.Length > 30)
                        {
                            strError = "�������� '" + strTemp + "' ���ַ������� 30����ʹ�ø��̵� SQL ���ݿ�����";
                            return -1;
                        }

                        strCommand += " DROP INDEX " + strTableName + "ki ;\n";
                        strCommand += " DROP INDEX " + strTableName + "ni ;\n";
                        strCommand += " DROP INDEX " + strTableName + "ii ;\n";
                    }
                }
            }
            #endregion

            return 0;
        }

        // ɾ�����ݿ�
        // return:
        //      -1  ����
        //      0   �ɹ�
        public override int Delete(out string strError)
        {
            strError = "";

            //************�����ݿ��д��********************
            this.m_db_lock.AcquireWriterLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
			this.container.WriteDebugInfo("Delete()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
            try //��
            {
                string strCommand = "";

                Connection connection = new Connection(this,
                    this.m_strConnString);
                connection.Open();
                try //����
                {
                    if (connection.SqlServerType == SqlServerType.MsSqlServer)
                    {
                        // 1.ɾ���sql���ݿ�
                        strCommand = "use master " + "\n"
                            + " if exists (select * from dbo.sysdatabases where name = N'" + this.m_strSqlDbName + "')" + "\n"
                            + " drop database " + this.m_strSqlDbName + "\n";
                        strCommand += " use master " + "\n";
                        SqlCommand command = new SqlCommand(strCommand,
                            connection.SqlConnection);

                        command.ExecuteNonQuery();
                    }
                    else if (connection.SqlServerType == SqlServerType.SQLite)
                    {
                        // SQLiteû��DROP TABLE��䣬ֱ��ɾ�����ݿ��ļ�����
                    }
                    else if (connection.SqlServerType == SqlServerType.MySql)
                    {
                        // 1.ɾ���sql���ݿ�
                        strCommand = " DROP DATABASE IF EXISTS `" + this.m_strSqlDbName + "` \n";
                        MySqlCommand command = new MySqlCommand(strCommand,
                            connection.MySqlConnection);

                        command.ExecuteNonQuery();
                    }
                    else if (connection.SqlServerType == SqlServerType.Oracle)
                    {
                        // ɾ��ȫ����
                        int nRet = DropAllTables(
                             connection.OracleConnection,
                             this.m_strSqlDbName,
                             "keys,records",
                             out strError);
                        if (nRet == -1)
                            return -1;
                    }
                }
                catch (SqlException sqlEx)
                {
                    // ����������������ݿ⣬�򲻱���

                    if (!(sqlEx.Errors is SqlErrorCollection))
                    {
                        strError = "ɾ��sql�����.\r\n"
                           + sqlEx.Message + "\r\n"
                           + "SQL����:\r\n"
                           + strCommand;
                        return -1;
                    }
                }
                catch (Exception ex)
                {
                    strError = "ɾ��sql�����.\r\n"
                        + ex.Message + "\r\n"
                        + "SQL����:\r\n"
                        + strCommand;
                    return -1;
                }
                finally  //����
                {
                    connection.Close();
                }


                // ɾ������Ŀ¼
                string strCfgsDir = DatabaseUtil.GetLocalDir(this.container.NodeDbs,
                    this.m_selfNode);
                if (strCfgsDir != "")
                {
                    // Ӧ��Ŀ¼���أ������������ʹ�����Ŀ¼������ɾ����������Ϣ
                    if (this.container.IsExistCfgsDir(strCfgsDir, this) == true)
                    {
                        // ��������־дһ����Ϣ
                        this.container.KernelApplication.WriteErrorLog("���ֳ��� '" + this.GetCaption("zh-CN") + "' ��ʹ�� '" + strCfgsDir + "' Ŀ¼�⣬�����������ʹ�����Ŀ¼�����Բ�����ɾ����ʱɾ��Ŀ¼");
                    }
                    else
                    {
                        string strRealDir = this.container.DataDir + "\\" + strCfgsDir;
                        if (Directory.Exists(strRealDir) == true)
                        {
                            PathUtil.DeleteDirectory(strRealDir);
                        }
                    }
                }

                if (this.container.SqlServerType == SqlServerType.SQLite)
                {
                    // Commit Transaction
                    this.CloseInternal(false);
                }

                // ɾ������Ŀ¼
                try
                {
                    if (string.IsNullOrEmpty(this.m_strObjectDir) == false)
                    {
                        PathUtil.DeleteDirectory(this.m_strObjectDir);
                    }
                }
                catch (Exception ex)
                {
                    strError = "ɾ�����ݿ� '" + this.GetCaption("zh") + "' �Ķ���Ŀ¼ '" + this.m_strObjectDir + "' ʱ�������� " + ex.Message;
                    return -1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                strError = "ɾ��'" + this.GetCaption("zh") + "'���ݿ����ԭ��:" + ex.Message;
                return -1;
            }
            finally
            {

                //*********************�����ݿ��д��**********
                m_db_lock.ReleaseWriterLock();
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("Delete()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
            }
        }

        // ��ID������¼
        // parameter:
        //		searchItem  SearchItem���󣬰���������Ϣ searchItem.IdOrder���������˳��
        //		isConnected ���Ӷ����delegate
        //		resultSet   ���������,������м�¼
        // return:
        //		-1  ����
        //		0   �ɹ�
        // �ߣ�����ȫ
        private int SearchByID(SearchItem searchItem,
            ChannelHandle handle,
            // Delegate_isConnected isConnected,
            DpResultSet resultSet,
            string strOutputStyle,
            out string strError)
        {
            strError = "";

            Debug.Assert(searchItem != null, "SearchByID()���ô���searchItem����ֵ����Ϊnull��");
            // Debug.Assert(isConnected != null, "SearchByID()���ô���isConnected����ֵ����Ϊnull��");
            Debug.Assert(handle != null, "SearchByID()���ô���handle����ֵ����Ϊnull��");
            Debug.Assert(resultSet != null, "SearchByID()���ô���resultSet����ֵ����Ϊnull��");

            Debug.Assert(this.container != null, "");

            bool bOutputKeyCount = StringUtil.IsInList("keycount", strOutputStyle);
            bool bOutputKeyID = StringUtil.IsInList("keyid", strOutputStyle);

            // SQLite���ñ�������
            Connection connection = new Connection(this,
                this.m_strConnString);
            connection.Open();
            try
            {
                string strPattern = "N'[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'";
                if (connection.SqlServerType == SqlServerType.MsSqlServer)
                    strPattern = "N'[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'";
                else if (connection.SqlServerType == SqlServerType.SQLite)
                    strPattern = "'__________'";
                else if (connection.SqlServerType == SqlServerType.MySql)
                    strPattern = "'__________'";
                else if (connection.SqlServerType == SqlServerType.Oracle)
                    strPattern = "'__________'";
                else
                    throw new Exception("δ֪�� SqlServerType");

                List<object> aSqlParameter = new List<object>();
                string strWhere = "";
                if (searchItem.Match == "left"
                    || searchItem.Match == "")
                {
                    strWhere = " WHERE id LIKE @id and id like " + strPattern + " ";
                    if (connection.SqlServerType == SqlServerType.MsSqlServer)
                    {
                        SqlParameter temp = new SqlParameter("@id", SqlDbType.NVarChar);
                        temp.Value = searchItem.Word + "%";
                        aSqlParameter.Add(temp);
                    }
                    else if (connection.SqlServerType == SqlServerType.SQLite)
                    {
                        SQLiteParameter temp = new SQLiteParameter("@id", DbType.String);
                        temp.Value = searchItem.Word + "%";
                        aSqlParameter.Add(temp);
                    }
                    else if (connection.SqlServerType == SqlServerType.MySql)
                    {
                        MySqlParameter temp = new MySqlParameter("@id", MySqlDbType.String);
                        temp.Value = searchItem.Word + "%";
                        aSqlParameter.Add(temp);
                    }
                    else if (connection.SqlServerType == SqlServerType.Oracle)
                    {
                        strWhere = strWhere.Replace("@", ":");
                        OracleParameter temp = new OracleParameter(":id", OracleDbType.NVarchar2);
                        temp.Value = searchItem.Word + "%";
                        aSqlParameter.Add(temp);
                    }
                }
                else if (searchItem.Match == "middle")
                {
                    strWhere = " WHERE id LIKE @id and id like " + strPattern + " ";
                    if (connection.SqlServerType == SqlServerType.MsSqlServer)
                    {
                        SqlParameter temp = new SqlParameter("@id", SqlDbType.NVarChar);
                        temp.Value = "%" + searchItem.Word + "%";
                        aSqlParameter.Add(temp);
                    }
                    else if (connection.SqlServerType == SqlServerType.SQLite)
                    {
                        SQLiteParameter temp = new SQLiteParameter("@id", DbType.String);
                        temp.Value = "%" + searchItem.Word + "%";
                        aSqlParameter.Add(temp);
                    }
                    else if (connection.SqlServerType == SqlServerType.MySql)
                    {
                        MySqlParameter temp = new MySqlParameter("@id", MySqlDbType.String);
                        temp.Value = "%" + searchItem.Word + "%";
                        aSqlParameter.Add(temp);
                    }
                    else if (connection.SqlServerType == SqlServerType.Oracle)
                    {
                        strWhere = strWhere.Replace("@", ":");
                        OracleParameter temp = new OracleParameter(":id", OracleDbType.NVarchar2);
                        temp.Value = "%" + searchItem.Word + "%";
                        aSqlParameter.Add(temp);
                    }

                }
                else if (searchItem.Match == "right")
                {
                    strWhere = " WHERE id LIKE @id and id like " + strPattern + " ";
                    if (connection.SqlServerType == SqlServerType.MsSqlServer)
                    {
                        SqlParameter temp = new SqlParameter("@id", SqlDbType.NVarChar);
                        temp.Value = "%" + searchItem.Word;
                        aSqlParameter.Add(temp);
                    }
                    else if (connection.SqlServerType == SqlServerType.SQLite)
                    {
                        SQLiteParameter temp = new SQLiteParameter("@id", DbType.String);
                        temp.Value = "%" + searchItem.Word;
                        aSqlParameter.Add(temp);
                    }
                    else if (connection.SqlServerType == SqlServerType.MySql)
                    {
                        MySqlParameter temp = new MySqlParameter("@id", MySqlDbType.String);
                        temp.Value = "%" + searchItem.Word;
                        aSqlParameter.Add(temp);
                    }
                    else if (connection.SqlServerType == SqlServerType.Oracle)
                    {
                        strWhere = strWhere.Replace("@", ":");
                        OracleParameter temp = new OracleParameter(":id", OracleDbType.NVarchar2);
                        temp.Value = "%" + searchItem.Word;
                        aSqlParameter.Add(temp);
                    }
                }
                else if (searchItem.Match == "exact")
                {
                    if (searchItem.DataType == "string")
                        searchItem.Word = DbPath.GetID10(searchItem.Word);

                    if (searchItem.Relation == "draw"
                    || searchItem.Relation == "range")
                    {
                        string strStartID;
                        string strEndID;
                        bool bRet = StringUtil.SplitRangeEx(searchItem.Word,
                            out strStartID,
                            out strEndID);

                        if (bRet == true)
                        {
                            strStartID = DbPath.GetID10(strStartID);
                            strEndID = DbPath.GetID10(strEndID);

                            strWhere = " WHERE @idMin <=id and id<= @idMax and id like " + strPattern + " ";

                            if (connection.SqlServerType == SqlServerType.MsSqlServer)
                            {
                                SqlParameter temp = new SqlParameter("@idMin", SqlDbType.NVarChar);
                                temp.Value = strStartID;
                                aSqlParameter.Add(temp);

                                temp = new SqlParameter("@idMax", SqlDbType.NVarChar);
                                temp.Value = strEndID;
                                aSqlParameter.Add(temp);
                            }
                            else if (connection.SqlServerType == SqlServerType.SQLite)
                            {
                                SQLiteParameter temp = new SQLiteParameter("@idMin", DbType.String);
                                temp.Value = strStartID;
                                aSqlParameter.Add(temp);

                                temp = new SQLiteParameter("@idMax", DbType.String);
                                temp.Value = strEndID;
                                aSqlParameter.Add(temp);
                            }
                            else if (connection.SqlServerType == SqlServerType.MySql)
                            {
                                MySqlParameter temp = new MySqlParameter("@idMin", MySqlDbType.String);
                                temp.Value = strStartID;
                                aSqlParameter.Add(temp);

                                temp = new MySqlParameter("@idMax", MySqlDbType.String);
                                temp.Value = strEndID;
                                aSqlParameter.Add(temp);
                            }
                            else if (connection.SqlServerType == SqlServerType.Oracle)
                            {
                                strWhere = strWhere.Replace("@", ":");

                                OracleParameter temp = new OracleParameter(":idMin", OracleDbType.NVarchar2);
                                temp.Value = strStartID;
                                aSqlParameter.Add(temp);

                                temp = new OracleParameter(":idMax", OracleDbType.NVarchar2);
                                temp.Value = strEndID;
                                aSqlParameter.Add(temp);
                            }
                        }
                        else
                        {
                            string strOperator;
                            string strRealText;
                            StringUtil.GetPartCondition(searchItem.Word,
                                out strOperator,
                                out strRealText);

                            strRealText = DbPath.GetID10(strRealText);
                            strWhere = " WHERE id " + strOperator + " @id and id like " + strPattern + " ";

                            if (connection.SqlServerType == SqlServerType.MsSqlServer)
                            {
                                SqlParameter temp = new SqlParameter("@id", SqlDbType.NVarChar);
                                temp.Value = strRealText;
                                aSqlParameter.Add(temp);
                            }
                            else if (connection.SqlServerType == SqlServerType.SQLite)
                            {
                                SQLiteParameter temp = new SQLiteParameter("@id", DbType.String);
                                temp.Value = strRealText;
                                aSqlParameter.Add(temp);
                            }
                            else if (connection.SqlServerType == SqlServerType.MySql)
                            {
                                MySqlParameter temp = new MySqlParameter("@id", MySqlDbType.String);
                                temp.Value = strRealText;
                                aSqlParameter.Add(temp);
                            }
                            else if (connection.SqlServerType == SqlServerType.Oracle)
                            {
                                strWhere = strWhere.Replace("@", ":");

                                OracleParameter temp = new OracleParameter(":id", OracleDbType.NVarchar2);
                                temp.Value = strRealText;
                                aSqlParameter.Add(temp);
                            }
                        }
                    }
                    else
                    {
                        searchItem.Word = DbPath.GetID10(searchItem.Word);
                        strWhere = " WHERE id " + searchItem.Relation + " @id and id like " + strPattern + " ";

                        if (connection.SqlServerType == SqlServerType.MsSqlServer)
                        {
                            SqlParameter temp = new SqlParameter("@id", SqlDbType.NVarChar);
                            temp.Value = searchItem.Word;
                            aSqlParameter.Add(temp);
                        }
                        else if (connection.SqlServerType == SqlServerType.SQLite)
                        {
                            SQLiteParameter temp = new SQLiteParameter("@id", DbType.String);
                            temp.Value = searchItem.Word;
                            aSqlParameter.Add(temp);
                        }
                        else if (connection.SqlServerType == SqlServerType.MySql)
                        {
                            MySqlParameter temp = new MySqlParameter("@id", MySqlDbType.String);
                            temp.Value = searchItem.Word;
                            aSqlParameter.Add(temp);
                        }
                        else if (connection.SqlServerType == SqlServerType.Oracle)
                        {
                            strWhere = strWhere.Replace("@", ":");

                            OracleParameter temp = new OracleParameter(":id", OracleDbType.NVarchar2);
                            temp.Value = searchItem.Word;
                            aSqlParameter.Add(temp);
                        }
                    }
                }

                string strTop = "";
                string strLimit = "";
                if (searchItem.MaxCount != -1)  // ֻ����ָ��������
                {
                    if (connection.SqlServerType == SqlServerType.MsSqlServer)
                        strTop = " TOP " + Convert.ToString(searchItem.MaxCount) + " ";
                    else if (connection.SqlServerType == SqlServerType.SQLite)
                        strLimit = " LIMIT " + Convert.ToString(searchItem.MaxCount) + " ";
                    else if (connection.SqlServerType == SqlServerType.MySql)
                        strLimit = " LIMIT " + Convert.ToString(searchItem.MaxCount) + " ";
                    else if (connection.SqlServerType == SqlServerType.Oracle)
                        strLimit = " WHERE rownum <= " + Convert.ToString(searchItem.MaxCount) + " ";
                    else
                        throw new Exception("δ֪�� SqlServerType");
                }

                string strOrderBy = "";

                // Oracle����ʹʹ��˳��
                if (connection.SqlServerType == SqlServerType.Oracle)
                {
                    if (string.IsNullOrEmpty(searchItem.IdOrder) == true)
                    {
                        searchItem.IdOrder = "ASC";
                    }
                }

                if (searchItem.IdOrder != "")
                {
                    strOrderBy = "ORDER BY id " + searchItem.IdOrder + " ";

                    // 2010/5/10
                    string strTemp = searchItem.IdOrder.ToLower();
                    if (strTemp.IndexOf("desc") != -1)
                        resultSet.Asc = -1;
                }

                string strCommand = "";
                if (connection.SqlServerType == SqlServerType.MsSqlServer)
                    strCommand = "use " + this.m_strSqlDbName;
                else if (connection.SqlServerType == SqlServerType.MySql)
                    strCommand = "use `" + this.m_strSqlDbName + "` ;\n";

                strCommand += " SELECT "
            + " DISTINCT "
            + strTop
            + (bOutputKeyID == false ? " id " : " id AS keystring, id, 'recid' AS fromstring ")
            + " FROM records "
            + strWhere
            + " " + strOrderBy
            + " " + strLimit + "\n";

                if (connection.SqlServerType == SqlServerType.MsSqlServer)
                    strCommand += " use master " + "\n";

                // Oracle�����ǳ�����
                if (connection.SqlServerType == SqlServerType.Oracle)
                {
                    // TODO ���û�� order by �Ӿ䣬 rownum�����Լ�
                    if (string.IsNullOrEmpty(strLimit) == false)
                        strCommand = "SELECT * from ( SELECT "
    + " DISTINCT "
    + (bOutputKeyID == false ? " id " : " id keystring, id, 'recid' fromstring ")
    + " FROM " + this.m_strSqlDbName + "_records "
    + strWhere
    + " " + strOrderBy
    + ") " + strLimit + "\n";
                    else
                        strCommand = "SELECT "
+ " DISTINCT "
+ (bOutputKeyID == false ? " id " : " id keystring, id, 'recid' fromstring ")
+ " FROM " + this.m_strSqlDbName + "_records "
+ strWhere
+ " " + strOrderBy
+ "\n";

                }


                if (connection.SqlServerType == SqlServerType.MsSqlServer)
                {
                    SqlCommand command = new SqlCommand(strCommand,
                        connection.SqlConnection);
                    try
                    {
                        command.CommandTimeout = 20 * 60;  // �Ѽ���ʱ����
                        foreach (SqlParameter sqlParameter in aSqlParameter)
                        {
                            command.Parameters.Add(sqlParameter);
                        }

                        IAsyncResult r = command.BeginExecuteReader(CommandBehavior.CloseConnection);
                        while (true)
                        {
                            if (handle != null)
                            {
                                if (handle.DoIdle() == false)
                                {
                                    command.Cancel();
                                    try
                                    {
                                        command.EndExecuteReader(r);
                                    }
                                    catch
                                    {
                                    }
                                    strError = "�û��ж�";
                                    return -1;
                                }
                            }
                            else
                                break;

                            bool bRet = r.AsyncWaitHandle.WaitOne(100, false);  //millisecondsTimeout
                            if (bRet == true)
                                break;
                        }

                        SqlDataReader reader = command.EndExecuteReader(r);
                        try
                        {
                            if (reader == null
                                || reader.HasRows == false)
                            {
                                return 0;
                            }

                            int nLoopCount = 0;
                            while (reader.Read())
                            {
                                if (nLoopCount % 10000 == 0)
                                {
                                    if (handle != null)
                                    {
                                        if (handle.DoIdle() == false)
                                        {
                                            strError = "�û��ж�";
                                            return -1;
                                        }
                                    }
                                }

                                string strID = ((string)reader[0]);
                                if (strID.Length != 10)
                                {
                                    strError = "������г����˳��Ȳ���10λ�ļ�¼�ţ�������";
                                    return -1;
                                }

#if NO
                        string strId = this.FullID + "/" + strID;   //��¼·����ʽ����ID/��¼��
                        resultSet.Add(new DpRecord(strId));
#endif
                                if (bOutputKeyCount == true)
                                {
                                    DpRecord dprecord = new DpRecord((string)reader[0]);
                                    dprecord.Index = 1;
                                    resultSet.Add(dprecord);
                                }
                                else if (bOutputKeyID == true)
                                {
                                    // datareader key, id
                                    // �������ʽ key, path
                                    string strKey = (string)reader[0];
                                    string strId = this.FullID + "/" + (string)reader[1]; // ��ʽΪ����id/��¼��
                                    string strFrom = (string)reader[2];
                                    DpRecord record = new DpRecord(strId);
                                    // new DpRecord(strKey + "," + strId)
                                    record.BrowseText = strKey + new string(DpResultSetManager.FROM_LEAD, 1) + strFrom;
                                    resultSet.Add(record);
                                }
                                else
                                {
                                    string strId = "";
                                    strId = this.FullID + "/" + (string)reader[0]; // ��¼��ʽΪ����id/��¼��
                                    resultSet.Add(new DpRecord(strId));
                                }


                                nLoopCount++;

                                if (nLoopCount % 100 == 0)
                                    Thread.Sleep(1);
                            }
                        }
                        finally
                        {
                            if (reader != null)
                                reader.Close();
                        }
                    } // end of using command
                    finally
                    {
                        if (command != null)
                            command.Dispose();
                    }
                }
                else if (connection.SqlServerType == SqlServerType.SQLite)
                {
                    // strCommand = "SELECT id FROM records WHERE id LIKE '__________' ";
                    SQLiteCommand command = new SQLiteCommand(strCommand,
                        connection.SQLiteConnection);
                    try
                    {
                        command.CommandTimeout = 20 * 60;  // �Ѽ���ʱ����
                        foreach (SQLiteParameter sqlParameter in aSqlParameter)
                        {
                            command.Parameters.Add(sqlParameter);
                        }

                        SQLiteDataReader reader = null;

                        DatabaseCommandTask task =
                            new DatabaseCommandTask(command);
                        try
                        {
                            Thread t1 = new Thread(new ThreadStart(task.ThreadMain));
                            t1.Start();
                            bool bRet;
                            while (true)
                            {
                                if (handle != null)  //ֻ�ǲ��ټ�����
                                {
                                    if (handle.DoIdle() == false)
                                    {
                                        command = null; // ���ﲻҪDispose() �����߳� task.ThreadMain ȥDispose()
                                        connection = null;
                                        reader = null;
                                        task.Cancel();
                                        strError = "�û��ж�";
                                        return -1;
                                    }
                                }
                                bRet = task.m_event.WaitOne(100, false);  //millisecondsTimeout
                                if (bRet == true)
                                    break;
                            }
                            if (task.bError == true)
                            {
                                strError = task.ErrorString;
                                return -1;
                            }

                            if (task.DataReader == null)
                                return 0;

                            reader = (SQLiteDataReader)task.DataReader;
                            if (reader.HasRows == false)
                            {
                                return 0;
                            }


                            int nLoopCount = 0;
                            while (reader.Read())
                            {
                                if (nLoopCount % 10000 == 0)
                                {
                                    if (handle != null)
                                    {
                                        if (handle.DoIdle() == false)
                                        {
                                            strError = "�û��ж�";
                                            return -1;
                                        }
                                    }
                                }

                                string strID = ((string)reader[0]);
                                if (strID.Length != 10)
                                {
                                    strError = "������г����˳��Ȳ���10λ�ļ�¼�ţ�������";
                                    return -1;
                                }

#if NO
                        string strId = this.FullID + "/" + strID;   //��¼·����ʽ����ID/��¼��
                        resultSet.Add(new DpRecord(strId));
#endif
                                if (bOutputKeyCount == true)
                                {
                                    DpRecord dprecord = new DpRecord((string)reader[0]);
                                    dprecord.Index = 1;
                                    resultSet.Add(dprecord);
                                } 
                                else if (bOutputKeyID == true)
                                {
                                    // datareader key, id
                                    // �������ʽ key, path
                                    string strKey = (string)reader[0];
                                    string strId = this.FullID + "/" + (string)reader[1]; // ��ʽΪ����id/��¼��
                                    string strFrom = (string)reader[2];
                                    DpRecord record = new DpRecord(strId);
                                    // new DpRecord(strKey + "," + strId)
                                    record.BrowseText = strKey + new string(DpResultSetManager.FROM_LEAD, 1) + strFrom;
                                    resultSet.Add(record);
                                }
                                else
                                {
                                    string strId = "";
                                    strId = this.FullID + "/" + (string)reader[0]; // ��¼��ʽΪ����id/��¼��
                                    resultSet.Add(new DpRecord(strId));
                                }

                                nLoopCount++;

                                if (nLoopCount % 100 == 0)
                                    Thread.Sleep(1);
                            }
                        }
                        finally
                        {
                            if (task != null && reader != null)
                                reader.Close();
                        }
                    } // end of using command
                    finally
                    {
                        if (command != null)
                            command.Dispose();
                    }
                }
                else if (connection.SqlServerType == SqlServerType.MySql)
                {
                    // strCommand = "SELECT id FROM records WHERE id LIKE '__________' ";
                    MySqlCommand command = new MySqlCommand(strCommand,
                        connection.MySqlConnection);
                    try
                    {
                        command.CommandTimeout = 20 * 60;  // �Ѽ���ʱ����
                        foreach (MySqlParameter sqlParameter in aSqlParameter)
                        {
                            command.Parameters.Add(sqlParameter);
                        }

                        IAsyncResult r = command.BeginExecuteReader(CommandBehavior.CloseConnection);
                        while (true)
                        {
                            if (handle != null)
                            {
                                if (handle.DoIdle() == false)
                                {
                                    command.Cancel();
                                    try
                                    {
                                        command.EndExecuteReader(r);
                                    }
                                    catch
                                    {
                                    }
                                    strError = "�û��ж�";
                                    return -1;
                                }
                            }
                            else
                                break;

                            bool bRet = r.AsyncWaitHandle.WaitOne(100, false);  //millisecondsTimeout
                            if (bRet == true)
                                break;
                        }

                        MySqlDataReader reader = command.EndExecuteReader(r);
                        try
                        {
                            if (reader == null
                                || reader.HasRows == false)
                            {
                                return 0;
                            }

                            int nLoopCount = 0;
                            while (reader.Read())
                            {
                                if (nLoopCount % 10000 == 0)
                                {
                                    if (handle != null)
                                    {
                                        if (handle.DoIdle() == false)
                                        {
                                            strError = "�û��ж�";
                                            return -1;
                                        }
                                    }
                                }

                                string strID = ((string)reader[0]);
                                if (strID.Length != 10)
                                {
                                    strError = "������г����˳��Ȳ���10λ�ļ�¼�ţ�������";
                                    return -1;
                                }

                                if (bOutputKeyCount == true)
                                {
                                    DpRecord dprecord = new DpRecord((string)reader[0]);
                                    dprecord.Index = 1;
                                    resultSet.Add(dprecord);
                                }
                                else if (bOutputKeyID == true)
                                {
                                    // datareader key, id
                                    // �������ʽ key, path
                                    string strKey = (string)reader[0];
                                    string strId = this.FullID + "/" + (string)reader[1]; // ��ʽΪ����id/��¼��
                                    string strFrom = (string)reader[2];
                                    DpRecord record = new DpRecord(strId);
                                    // new DpRecord(strKey + "," + strId)
                                    record.BrowseText = strKey + new string(DpResultSetManager.FROM_LEAD, 1) + strFrom;
                                    resultSet.Add(record);
                                }
                                else
                                {
                                    string strId = "";
                                    strId = this.FullID + "/" + (string)reader[0]; // ��¼��ʽΪ����id/��¼��
                                    resultSet.Add(new DpRecord(strId));
                                }

                                nLoopCount++;

                                if (nLoopCount % 100 == 0)
                                    Thread.Sleep(1);
                            }
                        }
                        finally
                        {
                            if (reader != null)
                                reader.Close();
                        }
                    } // end of using command
                    finally
                    {
                        if (command != null)
                            command.Dispose();
                    }
                }
                else if (connection.SqlServerType == SqlServerType.Oracle)
                {
                    // strCommand = "SELECT id FROM records WHERE id LIKE '__________' ";
                    OracleCommand command = new OracleCommand(strCommand,
                        connection.OracleConnection);
                    try
                    {
                        command.BindByName = true;
                        command.CommandTimeout = 20 * 60;  // �Ѽ���ʱ����
                        foreach (OracleParameter sqlParameter in aSqlParameter)
                        {
                            command.Parameters.Add(sqlParameter);
                        }

                        OracleDataReader reader = null;

                        DatabaseCommandTask task =
                            new DatabaseCommandTask(command);
                        try
                        {
                            Thread t1 = new Thread(new ThreadStart(task.ThreadMain));
                            t1.Start();
                            bool bRet;
                            while (true)
                            {
                                if (handle != null)  //ֻ�ǲ��ټ�����
                                {
                                    if (handle.DoIdle() == false)
                                    {
                                        command = null; // ���ﲻҪDispose() �����߳� task.ThreadMain ȥDispose()
                                        connection = null;
                                        reader = null;
                                        task.Cancel();
                                        strError = "�û��ж�";
                                        return -1;
                                    }
                                }
                                bRet = task.m_event.WaitOne(100, false);  //millisecondsTimeout
                                if (bRet == true)
                                    break;
                            }
                            if (task.bError == true)
                            {
                                strError = task.ErrorString;
                                return -1;
                            }

                            if (task.DataReader == null)
                                return 0;

                            reader = (OracleDataReader)task.DataReader;
                            if (reader.HasRows == false)
                            {
                                return 0;
                            }

                            int nLoopCount = 0;
                            while (reader.Read())
                            {
                                if (nLoopCount % 10000 == 0)
                                {
                                    if (handle != null)
                                    {
                                        if (handle.DoIdle() == false)
                                        {
                                            strError = "�û��ж�";
                                            return -1;
                                        }
                                    }
                                }

                                string strID = ((string)reader[0]);
                                if (strID.Length != 10)
                                {
                                    strError = "������г����˳��Ȳ���10λ�ļ�¼�ţ�������";
                                    return -1;
                                }

                                if (bOutputKeyCount == true)
                                {
                                    DpRecord dprecord = new DpRecord((string)reader[0]);
                                    dprecord.Index = 1;
                                    resultSet.Add(dprecord);
                                }
                                else if (bOutputKeyID == true)
                                {
                                    // datareader key, id
                                    // �������ʽ key, path
                                    string strKey = (string)reader[0];
                                    string strId = this.FullID + "/" + (string)reader[1]; // ��ʽΪ����id/��¼��
                                    string strFrom = (string)reader[2];
                                    DpRecord record = new DpRecord(strId);
                                    // new DpRecord(strKey + "," + strId)
                                    record.BrowseText = strKey + new string(DpResultSetManager.FROM_LEAD, 1) + strFrom;
                                    resultSet.Add(record);
                                }
                                else
                                {
                                    string strId = "";
                                    strId = this.FullID + "/" + (string)reader[0]; // ��¼��ʽΪ����id/��¼��
                                    resultSet.Add(new DpRecord(strId));
                                }

                                nLoopCount++;

                                if (nLoopCount % 100 == 0)
                                    Thread.Sleep(1);
                            }
                        }
                        finally
                        {
                            if (task != null && reader != null)
                                reader.Close();
                        }
                    } // end of using command
                    finally
                    {
                        if (command != null)
                            command.Dispose();
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                strError = SqlDatabase.GetSqlErrors(sqlEx);

                /*
                if (sqlEx.Errors is SqlErrorCollection)
                    strError = "���ݿ�'" + this.GetCaption("zh") + "'��δ��ʼ����";
                else
                    strError = sqlEx.Message;
                 * */
                return -1;
            }
            catch (Exception ex)
            {
                strError = "SearchByID() exception: " + ExceptionUtil.GetDebugText(ex);
                return -1;
            }
            finally // ����
            {
                if (connection != null)
                    connection.Close();
            }
            return 0;
        }

        /*
<target list="����ͼ��ʵ��:�������">
    <item>
        <word>00000335,00000903</word>
        <match>exact</match>
        <relation>list</relation>
        <dataType>string</dataType>
    </item>
    <lang>zh</lang>
</target>         * */
        int ProcessList(
            string strWordList,
            ref List<object> aSqlParameter,
            out string strKeyCondition,
            out string strError)
        {
            strError = "";
            strKeyCondition = "";

            StringBuilder text = new StringBuilder(4096);

            List<string> words = StringUtil.SplitList(strWordList);
            int i = 0;
            foreach (string word in words)
            {
                string strWord = word.Trim();

                if (i > 0)
                    text.Append(" OR ");
                string strParameterName = "@key" + i.ToString();
                text.Append(" keystring = " + strParameterName);

                if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                {
                    SqlParameter temp = new SqlParameter(strParameterName, SqlDbType.NVarChar);
                    temp.Value = strWord;
                    aSqlParameter.Add(temp);
                }
                else if (this.container.SqlServerType == SqlServerType.SQLite)
                {
                    SQLiteParameter temp = new SQLiteParameter(strParameterName, DbType.String);
                    temp.Value = strWord;
                    aSqlParameter.Add(temp);
                }
                else if (this.container.SqlServerType == SqlServerType.MySql)
                {
                    MySqlParameter temp = new MySqlParameter(strParameterName, MySqlDbType.String);
                    temp.Value = strWord;
                    aSqlParameter.Add(temp);
                }
                else if (this.container.SqlServerType == SqlServerType.Oracle)
                {
                    OracleParameter temp = new OracleParameter(strParameterName.Replace("@", ":"),
                        OracleDbType.NVarchar2);
                    temp.Value = strWord;
                    aSqlParameter.Add(temp);
                }

                i++;
            }

            strKeyCondition = text.ToString();
            return 0;
        }

        // �õ�����������˽�к�������SearchByUnion()������
        // ���ܻ��׳����쳣:NoMatchException(������ʽ����������)
        // ע�� �������غ�strKeyCondition�е� '@' �ַ�������Ҫ�滻Ϊ ':' (Oracle����)
        // parameters:
        //      searchItem              SearchItem����
        //      nodeConvertQueryString  �ַ����ͼ����ʵĴ�����Ϣ�ڵ�
        //      nodeConvertQueryNumber  ��ֵ�ͼ����ʵĴ�����Ϣ�ڵ�
        //      strPostfix              Sql����������ƺ�׺���Ա�����������һ��ʱ����
        //      aParameter              ��������
        //      strKeyCondition         out����������Sql����ʽ��������
        //      strError                out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        // �ߣ�����ȫ
        // ???�ú����׳��쳣�Ĵ���̫˳
        private int GetKeyCondition(SearchItem searchItem,
            XmlNode nodeConvertQueryString,
            XmlNode nodeConvertQueryNumber,
            string strPostfix,
            ref List<object> aSqlParameter,
            out string strKeyCondition,
            out string strError)
        {
            strKeyCondition = "";
            strError = "";

            bool bSearchNull = false;
            if (searchItem.Match == "exact"
                && searchItem.Relation == "="
                && String.IsNullOrEmpty(searchItem.Word) == true)
            {
                bSearchNull = true;
            }

            //���������Ƿ���ì�ܣ��ú������ܻ��׳�NoMatchException�쳣
            QueryUtil.VerifyRelation(ref searchItem.Match,
                ref searchItem.Relation,
                ref searchItem.DataType);


            int nRet = 0;
            KeysCfg keysCfg = null;
            nRet = this.GetKeysCfg(out keysCfg,
                out strError);
            if (nRet == -1)
                return -1;


            //3.�����������ͣ��Լ����ʽ��мӹ�
            string strKeyValue = searchItem.Word.Trim();
            if (searchItem.DataType == "string")    //�ַ����͵��ַ������ã��Լ����ʽ��мӹ�
            {
                if (nodeConvertQueryString != null && keysCfg != null)
                {
                    List<string> keys = null;
                    nRet = keysCfg.ConvertKeyWithStringNode(
                        null,//dataDom
                        strKeyValue,
                        nodeConvertQueryString,
                        out keys,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (keys.Count != 1)
                    {
                        string[] list = new string[keys.Count];
                        keys.CopyTo(list);
                        strError = "��֧�ְѼ����� '" + strKeyValue + "' ͨ��'split'��ʽ�ӹ��ɶ��("+string.Join(",", list)+")";
                        return -1;
                    }
                    strKeyValue = keys[0];
                }
            }
            else if (searchItem.DataType == "number"   //�����͵����ָ�ʽ�����ã��Լ����ʽ��мӹ�
                     && (searchItem.Relation != "draw" && searchItem.Relation != "range"))  // 2009/9/26 add
            {
                if (nodeConvertQueryNumber != null
                    && keysCfg != null)
                {
                    string strMyKey;
                    nRet = keysCfg.ConvertKeyWithNumberNode(
                        null,
                        strKeyValue,
                        nodeConvertQueryNumber,
                        out strMyKey,
                        out strError);
                    if (nRet == -1 || nRet == 1)
                        return -1;
                    strKeyValue = strMyKey;
                }
            }

            string strParameterName;
            //4.����match��ֵ���ֱ�õ���ͬ�ļ������ʽ
            if (searchItem.Match == "left"
                || searchItem.Match == "")  //���strMatchΪ�գ���"��һ��"
            {
                //��ʵһ��ʼ���Ѿ�����������Ƿ�ì�ܣ������ì���׳����죬�����ظ�����޺������ϸ�
                if (searchItem.DataType != "string")
                {
                    NoMatchException ex =
                        new NoMatchException("��ƥ�䷽ʽֵΪleft���ʱ������������ֵ" + searchItem.DataType + "ì�ܣ���������Ӧ��Ϊstring");
                    throw (ex);
                }
                strParameterName = "@keyValue" + strPostfix;
                strKeyCondition = "keystring LIKE "
                    + strParameterName + " ";

                if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                {
                    SqlParameter temp = new SqlParameter(strParameterName, SqlDbType.NVarChar);
                    temp.Value = strKeyValue + "%";
                    aSqlParameter.Add(temp);
                }
                else if (this.container.SqlServerType == SqlServerType.SQLite)
                {
                    SQLiteParameter temp = new SQLiteParameter(strParameterName, DbType.String);
                    temp.Value = strKeyValue + "%";
                    aSqlParameter.Add(temp);
                }
                else if (this.container.SqlServerType == SqlServerType.MySql)
                {
                    MySqlParameter temp = new MySqlParameter(strParameterName, MySqlDbType.String);
                    temp.Value = strKeyValue + "%";
                    aSqlParameter.Add(temp);
                }
                else if (this.container.SqlServerType == SqlServerType.Oracle)
                {
                    OracleParameter temp = new OracleParameter(strParameterName.Replace("@", ":"),
                        OracleDbType.NVarchar2);
                    temp.Value = strKeyValue + "%";
                    aSqlParameter.Add(temp);
                }
            }
            else if (searchItem.Match == "middle")
            {
                //��ʵһ��ʼ���Ѿ�����������Ƿ�ì�ܣ������ì���׳����죬�����ظ�����޺������ϸ�
                if (searchItem.DataType != "string")
                {
                    NoMatchException ex = new NoMatchException("��ƥ�䷽ʽֵΪmiddle���ʱ������������ֵ" + searchItem.DataType + "ì�ܣ���������Ӧ��Ϊstring");
                    throw (ex);
                }
                strParameterName = "@keyValue" + strPostfix;
                strKeyCondition = "keystring LIKE "
                    + strParameterName + " "; //N'%" + strKeyValue + "'";

                if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                {
                    SqlParameter temp = new SqlParameter(strParameterName, SqlDbType.NVarChar);
                    temp.Value = "%" + strKeyValue + "%";
                    aSqlParameter.Add(temp);
                }
                else if (this.container.SqlServerType == SqlServerType.SQLite)
                {
                    SQLiteParameter temp = new SQLiteParameter(strParameterName, DbType.String);
                    temp.Value = "%" + strKeyValue + "%";
                    aSqlParameter.Add(temp);
                }
                else if (this.container.SqlServerType == SqlServerType.MySql)
                {
                    MySqlParameter temp = new MySqlParameter(strParameterName, MySqlDbType.String);
                    temp.Value = "%" + strKeyValue + "%";
                    aSqlParameter.Add(temp);
                }
                else if (this.container.SqlServerType == SqlServerType.Oracle)
                {
                    OracleParameter temp = new OracleParameter(strParameterName.Replace("@", ":"), OracleDbType.NVarchar2);
                    temp.Value = "%" + strKeyValue + "%";
                    aSqlParameter.Add(temp);
                }
            }
            else if (searchItem.Match == "right")
            {
                //��ʵһ��ʼ���Ѿ�����������Ƿ�ì�ܣ������ì���׳����죬�����ظ�����޺������ϸ�
                if (searchItem.DataType != "string")
                {
                    NoMatchException ex = new NoMatchException("��ƥ�䷽ʽֵΪleft���ʱ������������ֵ" + searchItem.DataType + "ì�ܣ���������Ӧ��Ϊstring");
                    throw (ex);
                }
                strParameterName = "@keyValue" + strPostfix;
                strKeyCondition = "keystring LIKE "
                    + strParameterName + " "; //N'%" + strKeyValue + "'";

                if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                {
                    SqlParameter temp = new SqlParameter(strParameterName, SqlDbType.NVarChar);
                    temp.Value = "%" + strKeyValue;
                    aSqlParameter.Add(temp);
                }
                else if (this.container.SqlServerType == SqlServerType.SQLite)
                {
                    SQLiteParameter temp = new SQLiteParameter(strParameterName, DbType.String);
                    temp.Value = "%" + strKeyValue;
                    aSqlParameter.Add(temp);
                }
                else if (this.container.SqlServerType == SqlServerType.MySql)
                {
                    MySqlParameter temp = new MySqlParameter(strParameterName, MySqlDbType.String);
                    temp.Value = "%" + strKeyValue;
                    aSqlParameter.Add(temp);
                }
                else if (this.container.SqlServerType == SqlServerType.Oracle)
                {
                    OracleParameter temp = new OracleParameter(strParameterName.Replace("@", ":"),
                        OracleDbType.NVarchar2);
                    temp.Value = "%" + strKeyValue;
                    aSqlParameter.Add(temp);
                }
            }
            else if (searchItem.Match == "exact") //�ȿ�match���ٿ�relation,���dataType
            {
                // 2012/11/27
                if (searchItem.Relation == "list")
                {
                    nRet = ProcessList(searchItem.Word,
            ref aSqlParameter,
            out strKeyCondition,
            out strError);
                    if (nRet == -1)
                        return -1;
                }
                //�Ӵ��м�ȡ,�ϸ��ӣ�ע��
                else if (searchItem.Relation == "draw"
                    || searchItem.Relation == "range")
                {
                    // 2012/3/29
                    if (string.IsNullOrEmpty(searchItem.Word) == true)
                    {
                        if (bSearchNull == true && searchItem.DataType == "number")
                            searchItem.Word = "~";
                        else if (searchItem.DataType == "number")
                            searchItem.Word = "~";
                    }

                    string strStartText;
                    string strEndText;
                    bool bRet = StringUtil.SplitRangeEx(searchItem.Word,
                        out strStartText,
                        out strEndText);

                    if (bRet == true)
                    {
                        if (searchItem.DataType == "string")
                        {
                            if (nodeConvertQueryString != null
                                && keysCfg != null)
                            {
                                // �ӹ���
                                List<string> keys = null;
                                nRet = keysCfg.ConvertKeyWithStringNode(
                                    null,//dataDom
                                    strStartText,
                                    nodeConvertQueryString,
                                    out keys,
                                    out strError);
                                if (nRet == -1)
                                    return -1;
                                if (keys.Count != 1)
                                {
                                    strError = "��֧�ְѼ�����ͨ��'split'��ʽ�ӹ��ɶ��.";
                                    return -1;
                                }
                                strStartText = keys[0];


                                // �ӹ�β
                                nRet = keysCfg.ConvertKeyWithStringNode(
                                    null,//dataDom
                                    strEndText,
                                    nodeConvertQueryString,
                                    out keys,
                                    out strError);
                                if (nRet == -1)
                                    return -1;
                                if (keys.Count != 1)
                                {
                                    strError = "��֧�ְѼ�����ͨ��'split'��ʽ�ӹ��ɶ��.";
                                    return -1;
                                }
                                strEndText = keys[0];
                            }
                            string strParameterMinName = "@keyValueMin" + strPostfix;
                            string strParameterMaxName = "@keyValueMax" + strPostfix;

                            strKeyCondition = " " + strParameterMinName
                                + " <=keystring and keystring<= "
                                + strParameterMaxName + " ";

                            if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                            {
                                SqlParameter temp = new SqlParameter(strParameterMinName, SqlDbType.NVarChar);
                                temp.Value = strStartText;
                                aSqlParameter.Add(temp);

                                temp = new SqlParameter(strParameterMaxName, SqlDbType.NVarChar);
                                temp.Value = strEndText;
                                aSqlParameter.Add(temp);
                            }
                            else if (this.container.SqlServerType == SqlServerType.SQLite)
                            {
                                SQLiteParameter temp = new SQLiteParameter(strParameterMinName, DbType.String);
                                temp.Value = strStartText;
                                aSqlParameter.Add(temp);

                                temp = new SQLiteParameter(strParameterMaxName, DbType.String);
                                temp.Value = strEndText;
                                aSqlParameter.Add(temp);
                            }
                            else if (this.container.SqlServerType == SqlServerType.MySql)
                            {
                                MySqlParameter temp = new MySqlParameter(strParameterMinName, MySqlDbType.String);
                                temp.Value = strStartText;
                                aSqlParameter.Add(temp);

                                temp = new MySqlParameter(strParameterMaxName, MySqlDbType.String);
                                temp.Value = strEndText;
                                aSqlParameter.Add(temp);
                            }
                            else if (this.container.SqlServerType == SqlServerType.Oracle)
                            {
                                OracleParameter temp = new OracleParameter(strParameterMinName.Replace("@", ":"),
                                    OracleDbType.NVarchar2);
                                temp.Value = strStartText;
                                aSqlParameter.Add(temp);

                                temp = new OracleParameter(strParameterMaxName.Replace("@", ":"),
                                    OracleDbType.NVarchar2);
                                temp.Value = strEndText;
                                aSqlParameter.Add(temp);
                            }
                        }
                        else if (searchItem.DataType == "number")
                        {
                            if (nodeConvertQueryNumber != null
                                && keysCfg != null)
                            {
                                // ��
                                string strMyKey;
                                nRet = keysCfg.ConvertKeyWithNumberNode(
                                    null,
                                    strStartText,
                                    nodeConvertQueryNumber,
                                    out strMyKey,
                                    out strError);
                                if (nRet == -1 || nRet == 1)
                                    return -1;
                                strStartText = strMyKey;

                                // β
                                nRet = keysCfg.ConvertKeyWithNumberNode(
                                    null,
                                    strEndText,
                                    nodeConvertQueryNumber,
                                    out strMyKey,
                                    out strError);
                                if (nRet == -1 || nRet == 1)
                                    return -1;
                                strEndText = strMyKey;
                            }
                            strKeyCondition = strStartText
                                + " <= keystringnum and keystringnum <= "
                                + strEndText +
                                " and keystringnum <> -1";
                        }
                    }
                    else
                    {
                        string strOperator;
                        string strRealText;

                        //�������û�а�����ϵ������=����
                        StringUtil.GetPartCondition(searchItem.Word,
                            out strOperator,
                            out strRealText);

                        if (strOperator == "!=")
                            strOperator = "<>";

                        if (searchItem.DataType == "string")
                        {
                            if (nodeConvertQueryString != null
                                && keysCfg != null)
                            {
                                List<string> keys = null;
                                nRet = keysCfg.ConvertKeyWithStringNode(
                                    null,//dataDom
                                    strRealText,
                                    nodeConvertQueryString,
                                    out keys,
                                    out strError);
                                if (nRet == -1)
                                    return -1;
                                if (keys.Count != 1)
                                {
                                    strError = "��֧�ְѼ�����ͨ��'split'��ʽ�ӹ��ɶ��.";
                                    return -1;
                                }
                                strRealText = keys[0];

                            }

                            strParameterName = "@keyValue" + strPostfix;
                            strKeyCondition = " keystring"
                                + strOperator
                                + " " + strParameterName + " ";

                            if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                            {
                                SqlParameter temp = new SqlParameter(strParameterName, SqlDbType.NVarChar);
                                temp.Value = strRealText;
                                aSqlParameter.Add(temp);
                            }
                            else if (this.container.SqlServerType == SqlServerType.SQLite)
                            {
                                SQLiteParameter temp = new SQLiteParameter(strParameterName, DbType.String);
                                temp.Value = strRealText;
                                aSqlParameter.Add(temp);
                            }
                            else if (this.container.SqlServerType == SqlServerType.MySql)
                            {
                                MySqlParameter temp = new MySqlParameter(strParameterName, MySqlDbType.String);
                                temp.Value = strRealText;
                                aSqlParameter.Add(temp);
                            }
                            else if (this.container.SqlServerType == SqlServerType.Oracle)
                            {
                                OracleParameter temp = new OracleParameter(strParameterName.Replace("@", ":"),
                                    OracleDbType.NVarchar2);
                                temp.Value = strRealText;
                                aSqlParameter.Add(temp);
                            }
                        }
                        else if (searchItem.DataType == "number")
                        {
                            if (nodeConvertQueryNumber != null
                                && keysCfg != null)
                            {
                                string strMyKey;
                                nRet = keysCfg.ConvertKeyWithNumberNode(
                                    null,
                                    strRealText,
                                    nodeConvertQueryNumber,
                                    out strMyKey,
                                    out strError);
                                if (nRet == -1 || nRet == 1)
                                    return -1;
                                strRealText = strMyKey;
                            }

                            strKeyCondition = " keystringnum"
                                + strOperator
                                + strRealText
                                + " and keystringnum <> -1";
                        }
                    }
                }
                else   //��ͨ�Ĺ�ϵ������
                {
                    //����ϵ������Ϊ��Ϊ����������
                    if (searchItem.Relation == "")
                        searchItem.Relation = "=";
                    if (searchItem.Relation == "!=")
                        searchItem.Relation = "<>";

                    if (searchItem.DataType == "string")
                    {
                        strParameterName = "@keyValue" + strPostfix;

                        strKeyCondition = " keystring "
                            + searchItem.Relation
                            + " " + strParameterName + " ";

                        if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                        {
                            SqlParameter temp = new SqlParameter(strParameterName, SqlDbType.NVarChar);
                            temp.Value = strKeyValue;
                            aSqlParameter.Add(temp);
                        }
                        else if (this.container.SqlServerType == SqlServerType.SQLite)
                        {
                            SQLiteParameter temp = new SQLiteParameter(strParameterName, DbType.String);
                            temp.Value = strKeyValue;
                            aSqlParameter.Add(temp);
                        }
                        else if (this.container.SqlServerType == SqlServerType.MySql)
                        {
                            MySqlParameter temp = new MySqlParameter(strParameterName, MySqlDbType.String);
                            temp.Value = strKeyValue;
                            aSqlParameter.Add(temp);
                        }
                        else if (this.container.SqlServerType == SqlServerType.Oracle)
                        {
                            OracleParameter temp = new OracleParameter(strParameterName.Replace("@", ":"),
                                OracleDbType.NVarchar2);
                            temp.Value = strKeyValue;
                            aSqlParameter.Add(temp);
                        }
                    }
                    else if (searchItem.DataType == "number")
                    {
                        if (string.IsNullOrEmpty(strKeyValue) == false)
                            strKeyCondition = " keystringnum "
                                + searchItem.Relation
                                + strKeyValue
                                + " and keystringnum <> -1";
                        else
                            strKeyCondition = " keystringnum <> -1";    // 2012/3/29
                    }
                }
            }

            return 0;
        }

        // ����
        // parameters:
        //      searchItem  SearchItem���󣬴�ż����ʵ���Ϣ
        //      isConnected ���Ӷ���
        //      resultSet   ��������󣬴�����м�¼�������������ڼ���ǰ��ս��������ˣ���ͬһ�����������ִ�б�����������԰����н��׷����һ��
        //      strLang     ���԰汾��
        // return:
        //		-1	����
        //		0	�ɹ�
        //      1   �ɹ�����resultset��Ҫ��������һ��
        internal override int SearchByUnion(
            string strOutputStyle,
            SearchItem searchItem,
            ChannelHandle handle,
            // Delegate_isConnected isConnected,
            DpResultSet resultSet,
            int nWarningLevel,
            out string strError,
            out string strWarning)
        {
            strError = "";
            strWarning = "";

            bool bOutputKeyCount = StringUtil.IsInList("keycount", strOutputStyle);
            bool bOutputKeyID = StringUtil.IsInList("keyid", strOutputStyle);

            bool bNeedSort = false;

            DateTime start_time = DateTime.Now;

            //**********�����ݿ�Ӷ���**************
            m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
			this.container.WriteDebugInfo("SearchByUnion()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            // 2006/12/18 changed

            try
            {
                bool bHasID = false;
                List<TableInfo> aTableInfo = null;
                int nRet = this.TableNames2aTableInfo(searchItem.TargetTables,
                    out bHasID,
                    out aTableInfo,
                    out strError);
                if (nRet == -1)
                    return -1;

                // TODO: ***ע�⣺������ɼ���;��������__id,��ô��ֻ����һ����Ч���������ľ���Ч�ˡ����ƺ���Ҫ�Ľ���2007/9/13

                if (bHasID == true)
                {
                    nRet = SearchByID(searchItem,
                        handle,
                        // isConnected,
                        resultSet,
                        strOutputStyle,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

                // ��sql����˵,ͨ��ID�����󣬼�¼������ȥ��
                if (aTableInfo == null || aTableInfo.Count == 0)
                    return 0;

                // 2009/8/5 new add
                bool bSearchNull = false;
                if (searchItem.Match == "exact"
                    && searchItem.Relation == "="
                    && String.IsNullOrEmpty(searchItem.Word) == true)
                {
                    bSearchNull = true;
                }


                string strCommand = "";

                // Sql�����������
                List<object> aSqlParameter = new List<object>();

                string strColumnList = "";

                if (bOutputKeyCount == true
                    && bSearchNull == false)    // 2009/8/6 new add
                {
                    strColumnList = " keystring, count(*) ";
                }
                else if (bOutputKeyID == true
                    && bSearchNull == false)    // 2010/5/12 new add
                {
                    strColumnList = " keystring, idstring, fromstring ";
                }
                else
                {
                    // ��bSearchNull==true��ʱ��column listӦ����bOutputKeysCount == falseʱ��һ��

                    string strSelectKeystring = "";
                    if (searchItem.KeyOrder != "")
                    {
                        if (aTableInfo.Count > 1)
                            strSelectKeystring = ",keystring";
                    }

                    strColumnList = " idstring" + strSelectKeystring + " ";
                }

                // ѭ��ÿһ������;��
                for (int i = 0; i < aTableInfo.Count; i++)
                {
                    TableInfo tableInfo = aTableInfo[i];

                    // �������ĺ�׺
                    string strPostfix = Convert.ToString(i);

                    string strConditionAboutKey = "";
                    try
                    {
                        nRet = GetKeyCondition(
                            searchItem,
                            tableInfo.nodeConvertQueryString,
                            tableInfo.nodeConvertQueryNumber,
                            strPostfix,
                            ref aSqlParameter,
                            out strConditionAboutKey,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        if (this.container.SqlServerType == SqlServerType.Oracle)
                        {
                            strConditionAboutKey = strConditionAboutKey.Replace("@", ":");
                        }
                    }
                    catch (NoMatchException ex)
                    {
                        strWarning = ex.Message;
                        strError = strWarning;
                        return -1;
                    }

                    // ���������һ�����������ÿ��;����������������
                    string strTop = "";
                    string strLimit = "";

                    if (bSearchNull == false)
                    {
                        if (searchItem.MaxCount != -1)  //���Ƶ������
                        {
                            if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                                strTop = " TOP " + Convert.ToString(searchItem.MaxCount) + " ";
                            else if (this.container.SqlServerType == SqlServerType.SQLite)
                                strLimit = " LIMIT " + Convert.ToString(searchItem.MaxCount) + " ";
                            else if (this.container.SqlServerType == SqlServerType.MySql)
                                strLimit = " LIMIT " + Convert.ToString(searchItem.MaxCount) + " ";
                            else if (this.container.SqlServerType == SqlServerType.Oracle)
                                strLimit = " rownum <= " + Convert.ToString(searchItem.MaxCount) + " ";
                            else
                                throw new Exception("δ֪�� SqlServerType");
                        }
                    }

                    string strWhere = "";

                    if (bSearchNull == false)
                    {
                        if (strConditionAboutKey != "")
                            strWhere = " WHERE " + strConditionAboutKey;
                    }

                    string strDistinct = " DISTINCT ";
                    string strGroupBy = "";
                    if (bOutputKeyCount == true
                        && bSearchNull == false)
                    {
                        strDistinct = "";
                        strGroupBy = " GROUP BY keystring";
                    }

                    string strTableName = tableInfo.SqlTableName;
                    if (this.container.SqlServerType == SqlServerType.Oracle)
                    {
                        strTableName = this.m_strSqlDbName + "_" + tableInfo.SqlTableName;
                    }

                    string strOneCommand = "";
                    if (i == 0)// ��һ����
                    {
                        strOneCommand = 
                            " SELECT "
                            + strDistinct
                            + strTop
                            // + " idstring" + strSelectKeystring + " "
                            + strColumnList
                            + " FROM " + strTableName + " "
                            + strWhere
                            + strGroupBy
                            + (i == aTableInfo.Count - 1 ? strLimit : "");

                        if (this.container.SqlServerType == SqlServerType.Oracle)
                        {
                            strOneCommand =
    " SELECT "
    + strDistinct
    + strTop
                                // + " idstring" + strSelectKeystring + " "
    + strColumnList
    + " FROM " + strTableName + " "
    + strWhere
    + strGroupBy;
                            if (string.IsNullOrEmpty(strLimit) == false)
                            {
                                // ע�����Ҫ�����������������ȷ�����п�ǰ����Ŀ����Ҫ���� select * from ( �취
                                if (string.IsNullOrEmpty(strGroupBy) == false)
                                    strOneCommand = " SELECT * FROM ("
                                        + strOneCommand
                                        + ") WHERE " + strLimit;
                                else
                                {
                                    strOneCommand = strOneCommand
                                        + (string.IsNullOrEmpty(strWhere) == false ? " AND " : " ") 
                                        + strLimit;
                                }
                            }
                        }
                    }
                    else
                    {
                        strOneCommand = " union SELECT "
                            + strDistinct
                            + strTop
                            // + " idstring" + strSelectKeystring + " "  //DISTINCT ȥ��
                            + strColumnList
                            + " FROM " + strTableName + " "
                            + strWhere
                            + strGroupBy
                            + (i == aTableInfo.Count - 1 ? strLimit : "");
                        if (this.container.SqlServerType == SqlServerType.Oracle)
                        {
                            strOneCommand = " SELECT "
    + strDistinct
    + strTop
                                // + " idstring" + strSelectKeystring + " "  //DISTINCT ȥ��
    + strColumnList
    + " FROM " + strTableName + " "
    + strWhere
    + strGroupBy;
                            if (string.IsNullOrEmpty(strLimit) == false)
                            {
                                // ע�����Ҫ�����������������ȷ�����п�ǰ����Ŀ����Ҫ���� select * from ( �취
                                if (string.IsNullOrEmpty(strGroupBy) == false)
                                    strOneCommand = " SELECT * FROM ("
                                    + strOneCommand
                                    + ") WHERE " + strLimit;
                                else
                                {
                                    strOneCommand = strOneCommand
                                        + (string.IsNullOrEmpty(strWhere) == false ? " AND " : " ")
                                        + strLimit;
                                }

                            }

                            strOneCommand = " union " + strOneCommand;
                        }
                    }
                    strCommand += strOneCommand;
                }

                string strOrderBy = "";
                if (string.IsNullOrEmpty(searchItem.OrderBy) == false)
                {
                    strOrderBy = " ORDER BY " + searchItem.OrderBy + " ";

                    // 2010/5/10
                    string strTemp = searchItem.OrderBy.ToLower();
                    if (strTemp.IndexOf("desc") != -1)
                        resultSet.Asc = -1;

                    // TODO: ���select union, �ܵ���������ҵ�
                }

                // 2009/8/5
                if (bSearchNull == true)
                {
                    string strTop = "";
                    string strLimit = "";

                    if (searchItem.MaxCount != -1)  //���Ƶ������
                    {
                        if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                            strTop = " TOP " + Convert.ToString(searchItem.MaxCount) + " ";
                        else if (this.container.SqlServerType == SqlServerType.SQLite)
                            strLimit = " LIMIT " + Convert.ToString(searchItem.MaxCount) + " ";
                        else if (this.container.SqlServerType == SqlServerType.MySql)
                            strLimit = " LIMIT " + Convert.ToString(searchItem.MaxCount) + " ";
                        else if (this.container.SqlServerType == SqlServerType.Oracle)
                            strLimit = " WHERE rownum <= " + Convert.ToString(searchItem.MaxCount) + " ";
                        else
                            throw new Exception("δ֪�� SqlServerType");
                    }

                    string strColumns = " id ";
                    if (bOutputKeyCount == true)
                        strColumns = " keystring='', count(*) ";
                    else if (bOutputKeyID == true)
                        strColumns = " keystring=id, id, fromstring='recid' ";   // fromstring='' 2011/7/24


                    // Oracle�Ƚ�����
                    if (this.container.SqlServerType == SqlServerType.Oracle)
                    {
                        if (string.IsNullOrEmpty(strLimit) == false)
                            strCommand = "SELECT * FROM (select "
    + strColumns // " id "
    + "from " + this.m_strSqlDbName + "_records where id like '__________' and id not in (" + strCommand + ") "
    + strOrderBy    // 2012/3/30
    + ") " + strLimit;
                        else
                            strCommand = "select "
+ strColumns // " id "
+ "from " + this.m_strSqlDbName + "_records where id like '__________' and id not in (" + strCommand + ") "
+ strOrderBy    // 2012/3/30
;
                    }
                    else
                    {
                        strCommand = "select "
    + strTop
    + strColumns // " id "
    + "from records where id like '__________' and id not in (" + strCommand + ") "
    + strOrderBy    // 2012/3/30
    + strLimit;
                    }

                }
                else
                {
                    if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                        strCommand += " " + strOrderBy;
                    else
                        bNeedSort = true;
                    // TODO: �������ݿ����ͣ��Ƿ���һ��select * from () �����order by(���ֻ��һ��select�����Ҫ�����)��������ÿ�������select��������order by?
                }

                if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                    strCommand = "use " + this.m_strSqlDbName + " "
                    + strCommand;
                else if (this.container.SqlServerType == SqlServerType.MySql)
                    strCommand = "use `" + this.m_strSqlDbName + "` ;\n"
                    + strCommand;

                if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                    strCommand += " use master " + "\n";

                if (aSqlParameter == null)
                {
                    strError = "һ������Ҳû �ǲ����ܵ����";
                    return -1;
                }

                if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                {
                    SqlConnection connection =
                        new SqlConnection(this.m_strConnString/*Pooling*/);
                    connection.Open();
                    try
                    {
                        SqlCommand command = new SqlCommand(strCommand,
                            connection);
                        try
                        {
                            foreach (SqlParameter sqlParameter in aSqlParameter)
                            {
                                command.Parameters.Add(sqlParameter);
                            }
                            command.CommandTimeout = 20 * 60;  // �Ѽ���ʱ����

                            IAsyncResult r = command.BeginExecuteReader(CommandBehavior.CloseConnection);
                            while (true)
                            {
                                if (handle != null)
                                {
                                    if (handle.DoIdle() == false)
                                    {
                                        command.Cancel();
                                        try
                                        {
                                            command.EndExecuteReader(r);
                                        }
                                        catch
                                        {
                                        }
                                        strError = "�û��ж�";
                                        return -1;
                                    }
                                }
                                else
                                    break;

                                bool bRet = r.AsyncWaitHandle.WaitOne(100, false);  //millisecondsTimeout
                                if (bRet == true)
                                    break;
                            }

                            SqlDataReader reader = command.EndExecuteReader(r);
                            try
                            {

                                if (reader == null
                                    || reader.HasRows == false)
                                {
                                    return 0;
                                }

                                int nGetedCount = 0;
                                while (reader.Read())
                                {
                                    if (handle != null
                                        && (nGetedCount % 10000) == 0)
                                    {
                                        if (handle.DoIdle() == false)
                                        {
                                            strError = "�û��ж�";
                                            return -1;
                                        }
                                    }

                                    if (bOutputKeyCount == true)
                                    {
                                        int count = (int)reader[1];
                                        DpRecord dprecord = new DpRecord((string)reader[0]);
                                        dprecord.Index = count;
                                        resultSet.Add(dprecord);
                                    }
                                    else if (bOutputKeyID == true)
                                    {
                                        // datareader key, id
                                        // �������ʽ key, path
                                        string strKey = (string)reader[0];
                                        string strId = this.FullID + "/" + (string)reader[1]; // ��ʽΪ����id/��¼��
                                        string strFrom = (string)reader[2];
                                        DpRecord record = new DpRecord(strId);
                                        // new DpRecord(strKey + "," + strId)
                                        record.BrowseText = strKey + new string(DpResultSetManager.FROM_LEAD, 1) + strFrom;
                                        resultSet.Add(record);
                                    }
                                    else
                                    {
                                        string strId = "";
                                        strId = this.FullID + "/" + (string)reader[0]; // ��¼��ʽΪ����id/��¼��
                                        resultSet.Add(new DpRecord(strId));
                                    }

                                    nGetedCount++;

                                    // �����������
                                    if (searchItem.MaxCount != -1
                                        && nGetedCount >= searchItem.MaxCount)
                                        break;

                                    Thread.Sleep(0);
                                }
                            }
                            finally
                            {
                                if (reader != null)
                                    reader.Close();
                            }
                        } // end of using command
                        finally
                        {
                            if (command != null)
                                command.Dispose();
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        strError = GetSqlErrors(sqlEx);

                        /*
                        if (sqlEx.Errors is SqlErrorCollection)
                            strError = "���ݿ�'" + this.GetCaption("zh") + "'��δ��ʼ����";
                        else
                            strError = sqlEx.Message;
                         * */
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        strError = "SearchByUnion() exception: " + ExceptionUtil.GetDebugText(ex);
                        return -1;
                    }
                    finally // ����
                    {
                        if (connection != null)
                        {
                            connection.Close();
                            connection.Dispose();
                        }
                    }
                }
                else if (this.container.SqlServerType == SqlServerType.SQLite)
                {
                    // SQLite ���ñ�������
                    SQLiteConnection connection = 
                        new SQLiteConnection(this.m_strConnString/*Pooling*/);
                    // connection.Open();
                    Open(connection);
                    try
                    {
                        SQLiteCommand command = new SQLiteCommand(strCommand,
                            connection);
                        try
                        {
                            foreach (SQLiteParameter sqlParameter in aSqlParameter)
                            {
                                command.Parameters.Add(sqlParameter);
                            }
                            command.CommandTimeout = 20 * 60;  // �Ѽ���ʱ����
                            SQLiteDataReader reader = null;

                            // �����̴߳���
                            DatabaseCommandTask task = new DatabaseCommandTask(command);
                            try
                            {
                                if (task == null)
                                {
                                    strError = "testΪnull";
                                    return -1;
                                }
                                Thread t1 = new Thread(new ThreadStart(task.ThreadMain));
                                t1.Start();
                                bool bRet;
                                while (true)
                                {
                                    if (handle != null)
                                    {
                                        if (handle.DoIdle() == false)
                                        {
                                            command = null; // ���ﲻҪDispose() �����߳� task.ThreadMain ȥDispose()
                                            connection = null;
                                            reader = null;
                                            task.Cancel();
                                            strError = "�û��ж�";
                                            return -1;
                                        }
                                    }
                                    bRet = task.m_event.WaitOne(100, false);  //1/10�뿴һ��
                                    if (bRet == true)
                                        break;
                                }

                                // ���DataReader==null��������SQL����ʽ������
                                // 2007/9/14 new add
                                if (task.bError == true)
                                {
                                    strError = task.ErrorString;
                                    return -1;
                                }

                                reader = (SQLiteDataReader)task.DataReader;

                                if (reader == null
                                    || reader.HasRows == false)
                                {
                                    return 0;
                                }

                                int nGetedCount = 0;
                                while (reader.Read())
                                {
                                    if (handle != null
                                        && (nGetedCount % 10000) == 0)
                                    {
                                        if (handle.DoIdle() == false)
                                        {
                                            strError = "�û��ж�";
                                            return -1;
                                        }
                                    }

                                    if (bOutputKeyCount == true)
                                    {
                                        long count = (long)reader[1];
                                        DpRecord dprecord = new DpRecord((string)reader[0]);
                                        dprecord.Index = (int)count;
                                        resultSet.Add(dprecord);
                                    }
                                    else if (bOutputKeyID == true)
                                    {
                                        // datareader key, id
                                        // �������ʽ key, path
                                        string strKey = (string)reader[0];
                                        string strId = this.FullID + "/" + (string)reader[1]; // ��ʽΪ����id/��¼��
                                        string strFrom = (string)reader[2];
                                        DpRecord record = new DpRecord(strId);
                                        // new DpRecord(strKey + "," + strId)
                                        record.BrowseText = strKey + new string(DpResultSetManager.FROM_LEAD, 1) + strFrom;
                                        resultSet.Add(record);
                                    }
                                    else
                                    {
                                        string strId = "";
                                        strId = this.FullID + "/" + (string)reader[0]; // ��¼��ʽΪ����id/��¼��
                                        resultSet.Add(new DpRecord(strId));
                                    }

                                    nGetedCount++;

                                    // �����������
                                    if (searchItem.MaxCount != -1
                                        && nGetedCount >= searchItem.MaxCount)
                                        break;

                                    Thread.Sleep(0);
                                }
                            }
                            finally
                            {
                                if (reader != null)
                                    reader.Close();
                            }
                        } // end of using command
                        finally
                        {
                            if (command != null)
                                command.Dispose();
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        strError = GetSqlErrors(sqlEx);

                        /*
                        if (sqlEx.Errors is SqlErrorCollection)
                            strError = "���ݿ�'" + this.GetCaption("zh") + "'��δ��ʼ����";
                        else
                            strError = sqlEx.Message;
                         * */
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        strError = "SearchByUnion() exception: " + ExceptionUtil.GetDebugText(ex);
                        return -1;
                    }
                    finally // ����
                    {
                        if (connection != null)
                        {
                            connection.Close();
                            connection.Dispose();
                        }
                    }
                }
                else if (this.container.SqlServerType == SqlServerType.MySql)
                {
                    MySqlConnection connection =
                        new MySqlConnection(this.m_strConnString/*Pooling*/);
                    connection.Open();
                    try
                    {
                        MySqlCommand command = new MySqlCommand(strCommand,
                            connection);
                        try
                        {
                            foreach (MySqlParameter sqlParameter in aSqlParameter)
                            {
                                command.Parameters.Add(sqlParameter);
                            }
                            command.CommandTimeout = 20 * 60;  // �Ѽ���ʱ����
                                
                            IAsyncResult r = command.BeginExecuteReader(CommandBehavior.CloseConnection);
                            while (true)
                            {
                                if (handle != null)
                                {
                                    if (handle.DoIdle() == false)
                                    {
                                        command.Cancel();
                                        try
                                        {
                                            command.EndExecuteReader(r);
                                        }
                                        catch
                                        {
                                        }
                                        strError = "�û��ж�";
                                        return -1;
                                    }
                                }
                                else
                                    break;

                                bool bRet = r.AsyncWaitHandle.WaitOne(100, false);  //millisecondsTimeout
                                if (bRet == true)
                                    break;
                                /*
                                if (r.IsCompleted == true)
                                    break;
                                Thread.Sleep(1);
                                 * */
                            }

                            MySqlDataReader reader = command.EndExecuteReader(r);
                            try
                            {
                                if (reader == null
                                    || reader.HasRows == false)
                                {
                                    return 0;
                                }

                                int nGetedCount = 0;
                                while (reader.Read())
                                {
                                    if (handle != null
                                        && (nGetedCount % 10000) == 0)
                                    {
                                        if (handle.DoIdle() == false)
                                        {
                                            strError = "�û��ж�";
                                            return -1;
                                        }
                                    }

                                    if (bOutputKeyCount == true)
                                    {
                                        int count = (int)reader.GetInt32(1);
                                        DpRecord dprecord = new DpRecord((string)reader[0]);
                                        dprecord.Index = count;
                                        resultSet.Add(dprecord);
                                    }
                                    else if (bOutputKeyID == true)
                                    {
                                        // datareader key, id
                                        // �������ʽ key, path
                                        string strKey = (string)reader[0];
                                        string strId = this.FullID + "/" + (string)reader[1]; // ��ʽΪ����id/��¼��
                                        string strFrom = (string)reader[2];
                                        DpRecord record = new DpRecord(strId);
                                        // new DpRecord(strKey + "," + strId)
                                        record.BrowseText = strKey + new string(DpResultSetManager.FROM_LEAD, 1) + strFrom;
                                        resultSet.Add(record);
                                    }
                                    else
                                    {
                                        string strId = "";
                                        strId = this.FullID + "/" + (string)reader[0]; // ��¼��ʽΪ����id/��¼��
                                        resultSet.Add(new DpRecord(strId));
                                    }

                                    nGetedCount++;

                                    // �����������
                                    if (searchItem.MaxCount != -1
                                        && nGetedCount >= searchItem.MaxCount)
                                        break;

                                    Thread.Sleep(0);
                                }
                            }
                            finally
                            {
                                if (reader != null)
                                    reader.Close();
                            }
                        }
                        finally
                        {
                            if (command != null)
                                command.Dispose();
                        }

                    }
                    catch (SqlException sqlEx)
                    {
                        strError = GetSqlErrors(sqlEx);
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        strError = "SearchByUnion() exception: " + ExceptionUtil.GetDebugText(ex);
                        return -1;
                    }
                    finally // ����
                    {
                        if (connection != null)
                        {
                            try
                            {
                                connection.Close();
                                connection.Dispose();
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                else if (this.container.SqlServerType == SqlServerType.Oracle)
                {
                    OracleConnection connection =
                        new OracleConnection(this.m_strConnString/*Pooling*/);
                    connection.Open();
                    try
                    {
                        OracleCommand command = new OracleCommand(strCommand,
                             connection);
                        try
                        {
                            command.BindByName = true;
                            foreach (OracleParameter sqlParameter in aSqlParameter)
                            {
                                command.Parameters.Add(sqlParameter);
                            }
                            command.CommandTimeout = 20 * 60;  // �Ѽ���ʱ����
                            OracleDataReader reader = null;

                            // �����̴߳���
                            DatabaseCommandTask task = new DatabaseCommandTask(command);
                            try
                            {
                                if (task == null)
                                {
                                    strError = "testΪnull";
                                    return -1;
                                }
                                Thread t1 = new Thread(new ThreadStart(task.ThreadMain));
                                t1.Start();
                                bool bRet;
                                while (true)
                                {
                                    if (handle != null)
                                    {
                                        if (handle.DoIdle() == false)
                                        {
                                            command = null; // ���ﲻҪDispose() �����߳� task.ThreadMain ȥDispose()
                                            connection = null;
                                            reader = null;
                                            task.Cancel();
                                            strError = "�û��ж�";
                                            return -1;
                                        }
                                    }
                                    bRet = task.m_event.WaitOne(100, false);  //1/10�뿴һ��
                                    if (bRet == true)
                                        break;
                                }

                                // ���DataReader==null��������SQL����ʽ������
                                // 2007/9/14 new add
                                if (task.bError == true)
                                {
                                    strError = task.ErrorString;
                                    return -1;
                                }

                                reader = (OracleDataReader)task.DataReader;

                                if (reader == null
                                    || reader.HasRows == false)
                                {
                                    return 0;
                                }

                                int nGetedCount = 0;
                                while (reader.Read())
                                {
                                    if (handle != null
                                        && (nGetedCount % 10000) == 0)
                                    {
                                        if (handle.DoIdle() == false)
                                        {
                                            strError = "�û��ж�";
                                            return -1;
                                        }
                                    }

                                    if (bOutputKeyCount == true)
                                    {
                                        int count = reader.GetOracleDecimal(1).ToInt32();
                                        DpRecord dprecord = new DpRecord((string)reader[0]);
                                        dprecord.Index = count;
                                        resultSet.Add(dprecord);
                                    }
                                    else if (bOutputKeyID == true)
                                    {
                                        // datareader key, id
                                        // �������ʽ key, path
                                        string strKey = (string)reader[0];
                                        string strId = this.FullID + "/" + (string)reader[1]; // ��ʽΪ����id/��¼��
                                        string strFrom = (string)reader[2];
                                        DpRecord record = new DpRecord(strId);
                                        // new DpRecord(strKey + "," + strId)
                                        record.BrowseText = strKey + new string(DpResultSetManager.FROM_LEAD, 1) + strFrom;
                                        resultSet.Add(record);
                                    }
                                    else
                                    {
                                        string strId = "";
                                        strId = this.FullID + "/" + (string)reader[0]; // ��¼��ʽΪ����id/��¼��
                                        resultSet.Add(new DpRecord(strId));
                                    }

                                    nGetedCount++;

                                    // �����������
                                    if (searchItem.MaxCount != -1
                                        && nGetedCount >= searchItem.MaxCount)
                                        break;

                                    Thread.Sleep(0);
                                }
                            }
                            finally
                            {
                                if (reader != null)
                                    reader.Close();
                            }

                        }
                        finally
                        {
                            if (command != null)
                                command.Dispose();
                        }

                    }
                    catch (SqlException sqlEx)
                    {
                        strError = GetSqlErrors(sqlEx);
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        strError = "SearchByUnion() exception: " + ExceptionUtil.GetDebugText(ex);
                        return -1;
                    }
                    finally // ����
                    {
                        if (connection != null)
                        {
                            connection.Close();
                            connection.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                strError = "1: " + ExceptionUtil.GetDebugText(ex);
                return -1;
            }
            finally
            {

                //*****************�����ݿ�����***************
                m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("SearchByUnion()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
                
                // 2006/12/18 changed

                TimeSpan delta = DateTime.Now - start_time;
                Debug.WriteLine("SearchByUnion��ʱ " + delta.ToString());
            }

            if (bNeedSort == true)
                return 1;

            return 0;
        }

        static void Open(SQLiteConnection connection)
        {
#if REDO_OPEN
            int nRedoCount = 0;
        REDO:
            try
            {
                connection.Open();
            }
            catch (SQLiteException ex)
            {
                if (ex.ErrorCode == SQLiteErrorCode.Busy
                    && nRedoCount < 2)
                {
                    nRedoCount++;
                    goto REDO;
                }
                throw ex;
            }
#else
            connection.Open();
#endif
        }

        // ����Ƿ�Ҫ�Զ����� SQL ���ݿ�ṹ
        // Ϊrecords������newdptimestamp��
        internal override int UpdateStructure(out string strError)
        {
            strError = "";

            if (this.container.SqlServerType == SqlServerType.MsSqlServer)
            {
                SqlConnection connection = new SqlConnection(this.m_strConnString);
                connection.Open();
                try
                {
                    /*
                    string strCommand = "use " + this.m_strSqlDbName + "\n"
                        + "IF NOT EXISTS (select * from INFORMATION_SCHEMA.COLUMNS where table_name = 'records' and column_name = 'newdptimestamp')"
                        + "begin\n"
                        + "ALTER TABLE records ADD [newdptimestamp] [nvarchar] (100) NULL\n"
                        + "end\n"
                        + "IF NOT EXISTS (select * from INFORMATION_SCHEMA.COLUMNS where table_name = 'records' and column_name = 'filename')"
                        + "begin\n"
                        + "ALTER TABLE records ADD [filename] [nvarchar] (255) NULL\n"
                        + ", [newfilename] [nvarchar] (255) NULL\n"
                        + "end\n"
                        + "use master\n";
                     * */
                    string strCommand = "use " + this.m_strSqlDbName + "\n"
        + "IF NOT EXISTS (select * from INFORMATION_SCHEMA.COLUMNS where table_name = 'records' and column_name = 'newdptimestamp')"
        + "begin\n"
        + "ALTER TABLE records ADD [newdptimestamp] [nvarchar] (100) NULL\n"
        + ", [filename] [nvarchar] (255) NULL\n"
        + ", [newfilename] [nvarchar] (255) NULL\n"
        + "end\n"
        + "use master\n";

                    SqlCommand command = new SqlCommand(strCommand,
                        connection);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        strError = "���� newdptimestamp ��ʱ����.\r\n"
                            + ex.Message + "\r\n"
                            + "SQL����:\r\n"
                            + strCommand;
                        return -1;
                    }
                }
                catch (SqlException ex)
                {
                    /*
                    if (ex.Errors is SqlErrorCollection)
                        return 0;
                     * */

                    strError = "2: " + ex.Message;
                    return -1;
                }
                catch (Exception ex)
                {
                    strError = "3: " + ex.Message;
                    return -1;
                }
                finally // ����
                {
                    connection.Close();
                }
            }

            return 0;
        }


        // ����strStyle���,�õ���͵ļ�¼��
        // prev:ǰһ��,next:��һ��,���strID == ? ��prevΪ��һ��,nextΪ���һ��
        // ���������prev��next���ܵ��˺���
        // parameter:
        //		connection	        ���Ӷ���
        //		strCurrentRecordID	��ǰ��¼ID
        //		strStyle	        ���
        //      strOutputRecordID   out�����������ҵ��ļ�¼��
        //      strError            out���������س�����Ϣ
        // return:
        //		-1  ����
        //      0   δ�ҵ�
        //      1   �ҵ�
        // �ߣ�����ȫ
        private int GetRecordID(Connection connection,
            string strCurrentRecordID,
            string strStyle,
            out string strOutputRecordID,
            out string strError)
        {
            strOutputRecordID = "";
            strError = "";

            Debug.Assert(connection != null, "GetRecordID()���ô���connection����ֵ����Ϊnull��");

            if ((StringUtil.IsInList("prev", strStyle) == false)
                && (StringUtil.IsInList("next", strStyle) == false))
            {
                Debug.Assert(false, "GetRecordID()���ô������strStyle����������prev��nextֵ��Ӧ�ߵ����");
                throw new Exception("GetRecordID()���ô������strStyle����������prev��nextֵ��Ӧ�ߵ����");
            }

            strCurrentRecordID = DbPath.GetID10(strCurrentRecordID);

            if (connection.SqlServerType == SqlServerType.MsSqlServer)
            {
                string strPattern = "N'[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'";

                string strWhere = "";
                string strOrder = "";
                if ((StringUtil.IsInList("prev", strStyle) == true))
                {
                    if (DbPath.GetCompressedID(strCurrentRecordID) == "-1")
                    {
                        strWhere = " where id like "+strPattern+" ";
                        strOrder = " ORDER BY id DESC ";
                    }
                    else if (StringUtil.IsInList("myself", strStyle) == true)
                    {
                        strWhere = " where id<='" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id DESC ";
                    }
                    else
                    {
                        strWhere = " where id<'" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id DESC ";
                    }
                }
                else if (StringUtil.IsInList("next", strStyle) == true)
                {
                    if (DbPath.GetCompressedID(strCurrentRecordID) == "-1")
                    {
                        strWhere = " where id like " + strPattern + " ";
                        strOrder = " ORDER BY id ASC ";
                    }
                    else if (StringUtil.IsInList("myself", strStyle) == true)
                    {
                        strWhere = " where id>='" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id ASC ";
                    }
                    else
                    {
                        strWhere = " where id>'" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id ASC ";
                    }
                }
                string strCommand = "use " + this.m_strSqlDbName + " "
                    + " SELECT Top 1 id "
                    + " FROM records "
                    + strWhere
                    + strOrder;
                strCommand += " use master " + "\n";

                DateTime start_time = DateTime.Now;

                using (SqlCommand command = new SqlCommand(strCommand,
                    connection.SqlConnection))
                {

                    SqlDataReader dr =
                        command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        if (dr == null || dr.HasRows == false)
                        {
                            return 0;
                        }
                        else
                        {
                            dr.Read();
                            strOutputRecordID = (string)dr[0];

                            TimeSpan delta = DateTime.Now - start_time;
                            Debug.WriteLine("MS SQL Server ������ݿ� '" + this.GetCaption("zh-CN") + "' ��ǰβ�źķ�ʱ�� " + delta.TotalSeconds.ToString() + " ��");

                            return 1;
                        }
                    }
                    finally
                    {
                        dr.Close();
                    }
                } // end of using command
            }
            else if (connection.SqlServerType == SqlServerType.SQLite)
            {
                string strPattern = "'__________'";
                string strWhere = "";
                string strOrder = "";
                if ((StringUtil.IsInList("prev", strStyle) == true))
                {
                    if (DbPath.GetCompressedID(strCurrentRecordID) == "-1")
                    {
                        strWhere = " where id like " + strPattern + " ";
                        strOrder = " ORDER BY id DESC ";
                    }
                    else if (StringUtil.IsInList("myself", strStyle) == true)
                    {
                        strWhere = " where id<='" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id DESC ";
                    }
                    else
                    {
                        strWhere = " where id<'" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id DESC ";
                    }
                }
                else if (StringUtil.IsInList("next", strStyle) == true)
                {
                    if (DbPath.GetCompressedID(strCurrentRecordID) == "-1")
                    {
                        strWhere = " where id like " + strPattern + " ";
                        strOrder = " ORDER BY id ASC ";
                    }
                    else if (StringUtil.IsInList("myself", strStyle) == true)
                    {
                        strWhere = " where id>='" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id ASC ";
                    }
                    else
                    {
                        strWhere = " where id>'" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id ASC ";
                    }
                }
                string strCommand = " SELECT id "
                    + " FROM records "
                    + strWhere
                    + strOrder
                    + " LIMIT 1";

                DateTime start_time = DateTime.Now;

                using (SQLiteCommand command = new SQLiteCommand(strCommand,
                    connection.SQLiteConnection))
                {

                    try
                    {
                        SQLiteDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                        try
                        {
                            if (dr == null || dr.HasRows == false)
                            {
                                return 0;
                            }
                            else
                            {
                                dr.Read();
                                strOutputRecordID = (string)dr[0];

                                TimeSpan delta = DateTime.Now - start_time;
                                Debug.WriteLine("SQLite ������ݿ� '" + this.GetCaption("zh-CN") + "' ��ǰβ�źķ�ʱ�� " + delta.TotalSeconds.ToString() + " ��");

                                return 1;
                            }
                        }
                        finally
                        {
                            dr.Close();
                        }
                    }
                    catch (SQLiteException ex)
                    {
                        strError = "ִ��SQL��䷢������: " + ex.Message + "\r\nSQL ���: " + strCommand;
                        return -1;
                    }
                } // end of using command
            }
            else if (connection.SqlServerType == SqlServerType.MySql)
            {
                string strPattern = "'__________'";

                string strWhere = "";
                string strOrder = "";
                if ((StringUtil.IsInList("prev", strStyle) == true))
                {
                    if (DbPath.GetCompressedID(strCurrentRecordID) == "-1")
                    {
                        strWhere = " where id like " + strPattern + " ";
                        strOrder = " ORDER BY id DESC ";
                    }
                    else if (StringUtil.IsInList("myself", strStyle) == true)
                    {
                        strWhere = " where id<='" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id DESC ";
                    }
                    else
                    {
                        strWhere = " where id<'" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id DESC ";
                    }
                }
                else if (StringUtil.IsInList("next", strStyle) == true)
                {
                    if (DbPath.GetCompressedID(strCurrentRecordID) == "-1")
                    {
                        strWhere = " where id like " + strPattern + " ";
                        strOrder = " ORDER BY id ASC ";
                    }
                    else if (StringUtil.IsInList("myself", strStyle) == true)
                    {
                        strWhere = " where id>='" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id ASC ";
                    }
                    else
                    {
                        strWhere = " where id>'" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id ASC ";
                    }
                }
                string strCommand = " SELECT id "
                    + " FROM `" + this.m_strSqlDbName + "`.records "
                    + strWhere
                    + strOrder
                    + " LIMIT 1";

                DateTime start_time = DateTime.Now;

                using (MySqlCommand command = new MySqlCommand(strCommand,
                    connection.MySqlConnection))
                {

                    MySqlDataReader dr =
                        command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        if (dr == null || dr.HasRows == false)
                        {
                            return 0;
                        }
                        else
                        {
                            dr.Read();
                            strOutputRecordID = (string)dr[0];

                            TimeSpan delta = DateTime.Now - start_time;
                            Debug.WriteLine("MySQL ������ݿ� '" + this.GetCaption("zh-CN") + "' ��ǰβ�źķ�ʱ�� " + delta.TotalSeconds.ToString() + " ��");

                            return 1;
                        }
                    }
                    finally
                    {
                        dr.Close();
                    }
                } // end of using command
            }
            else if (connection.SqlServerType == SqlServerType.Oracle)
            {
                string strPattern = "'__________'";

                string strWhere = "";
                string strOrder = "";
                if ((StringUtil.IsInList("prev", strStyle) == true))
                {
                    if (DbPath.GetCompressedID(strCurrentRecordID) == "-1")
                    {
                        strWhere = " where id like " + strPattern + " ";
                        strOrder = " ORDER BY id DESC ";
                    }
                    else if (StringUtil.IsInList("myself", strStyle) == true)
                    {
                        strWhere = " where id<='" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id DESC ";
                    }
                    else
                    {
                        strWhere = " where id<'" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id DESC ";
                    }
                }
                else if (StringUtil.IsInList("next", strStyle) == true)
                {
                    if (DbPath.GetCompressedID(strCurrentRecordID) == "-1")
                    {
                        strWhere = " where id like " + strPattern + " ";
                        strOrder = " ORDER BY id ASC ";
                    }
                    else if (StringUtil.IsInList("myself", strStyle) == true)
                    {
                        strWhere = " where id>='" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id ASC ";
                    }
                    else
                    {
                        strWhere = " where id>'" + strCurrentRecordID + "' and id like " + strPattern + " ";
                        strOrder = " ORDER BY id ASC ";
                    }
                }
                string strCommand = "SELECT * FROM (SELECT id "
                    + " FROM " + this.m_strSqlDbName + "_records "
                    + strWhere
                    + strOrder
                    + " ) WHERE rownum <= 1";

                DateTime start_time = DateTime.Now;

                try
                {
                    using (OracleCommand command = new OracleCommand(strCommand,
                        connection.OracleConnection))
                    {

                        OracleDataReader dr =
                            command.ExecuteReader(CommandBehavior.SingleResult);
                        try
                        {
                            if (dr == null || dr.HasRows == false)
                            {
                                return 0;
                            }
                            else
                            {
                                dr.Read();
                                strOutputRecordID = (string)dr[0];

                                TimeSpan delta = DateTime.Now - start_time;
                                Debug.WriteLine("Oracle ������ݿ� '" + this.GetCaption("zh-CN") + "' ��ǰβ�źķ�ʱ�� " + delta.TotalSeconds.ToString() + " ��");

                                return 1;
                            }
                        }
                        finally
                        {
                            if (dr != null)
                                dr.Close();
                        }
                    } // end of using command
                }
                catch (OracleException ex)
                {
                    if (ex.Number == 942)
                    {
                        strError = "SQL�� '" + this.m_strSqlDbName + "_records' ������";
                        return -1;
                    }
                    throw ex;
                }
            }
            else
            {
                strError = "δ֪�� connection ���� '" + connection.SqlServerType.ToString() + "'";
                return -1;
            }
        }

        // ����strStyle���,�õ���͵ļ�¼��
        // prev:ǰһ��,next:��һ��,���strID == ? ��prevΪ��һ��,nextΪ���һ��
        // ���������prev��next���ܵ��˺���
        // parameter:
        //		strCurrentRecordID	��ǰ��¼ID
        //		strStyle	        ���
        //      strOutputRecordID   out�����������ҵ��ļ�¼��
        //      strError            out���������س�����Ϣ
        // return:
        //		-1  ����
        //      0   δ�ҵ�
        //      1   �ҵ�
        // �ߣ�����ȫ
        internal override int GetRecordID(string strCurrentRecordID,
            string strStyle,
            out string strOutputRecordID,
            out string strError)
        {
            strOutputRecordID = "";
            strError = "";

            Connection connection = new Connection(
                this,
                this.m_strConnStringPooling);
            connection.Open();
            try
            {
                // return:
                //		-1  ����
                //      0   δ�ҵ�
                //      1   �ҵ�
                return this.GetRecordID(connection,
                    strCurrentRecordID,
                    strStyle,
                    out strOutputRecordID,
                    out strError);
            }
            catch (SqlException ex)
            {
                if (ex.Errors is SqlErrorCollection)
                    return 0;

                strError = "4: " + ex.Message;
                return -1;
            }
            catch (Exception ex)
            {
                strError = "5: " + ex.Message;
                return -1;
            }
            finally // ����
            {
                connection.Close();
            }
        }

        // ��ָ����Χ��Xml
        // parameter:
        //		strRecordID			��¼ID
        //		strXPath			������λ�ڵ��xpath
        //		nStart				��Ŀ����Ŀ�ʼλ��
        //		nLength				���� -1:��ʼ������
        //		nMaxLength			���Ƶ���󳤶�
        //		strStyle			���,data:ȡ���� prev:ǰһ����¼ next:��һ����¼
        //							withresmetadata���Ա�ʾ����Դ��Ԫ�����body���
        //							ͬʱע��ʱ��������ߺϲ����ʱ���(ע:�����Ѿ���������, ʱ��������Ƕ�����)
        //		destBuffer			out�����������ֽ�����
        //		strMetadata			out����������Ԫ����
        //		strOutputResPath	out������������ؼ�¼��·��
        //		outputTimestamp		out����������ʱ���
        //		strError			out���������س�����Ϣ
        // return:
        //		-1  ����
        //		-4  δ�ҵ���¼
        //      -10 ��¼�ֲ�δ�ҵ�
        //		>=0 ��Դ�ܳ���
        //      nAdditionError -50 ��һ�������¼���Դ��¼������
        // ��: ��ȫ��
        public override long GetXml(string strRecordID,
            string strXPath,
            long lStart,
            int nLength,
            int nMaxLength,
            string strStyle,
            out byte[] destBuffer,
            out string strMetadata,
            out string strOutputRecordID,
            out byte[] outputTimestamp,
            bool bCheckAccount,
            out int nAdditionError,
            out string strError)
        {
            destBuffer = null;
            strMetadata = "";
            strOutputRecordID = "";
            outputTimestamp = null;
            strError = "";
            nAdditionError = 0;

            int nRet = 0;
            long lRet = 0;

            int nNotFoundSubRes = 0;    // �¼�û���ҵ�����Դ����
            string strNotFoundSubResIds = "";

            // ���ID
            // return:
            //      -1  ����
            //      0   �ɹ�
            nRet = DatabaseUtil.CheckAndGet10RecordID(ref strRecordID,
                out strError);
            if (nRet == -1)
                return -1;

            // ����ʽȥ�հ�
            strStyle = strStyle.Trim();

#if SUPER
            if (this.FastMode == true)
            {
                // �ڶ�д�����У������ⶼ�����ų�
                m_db_lock.AcquireWriterLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
			this.container.WriteDebugInfo("GetXml()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
            }
            else
            {
#endif

                //********����Ӷ���**************
                m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
			this.container.WriteDebugInfo("GetXml()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif

#if SUPER
            }
#endif

            try
            {

                // ȡ��ʵ�ʵļ�¼��
                if (StringUtil.IsInList("prev", strStyle) == true
                    || StringUtil.IsInList("next", strStyle) == true)
                {
                    string strTempOutputID = "";

                    // TODO: �����Connection�ɷ�ͺ���ĺ���
                    Connection connection = new Connection(this,
                        this.m_strConnString
#if SUPER
                        ,
                        this.container.SqlServerType == SqlServerType.SQLite && this.FastMode == true ? ConnectionStyle.Global : ConnectionStyle.None
#endif
);

                    connection.Open();
                    try
                    {
                        // return:
                        //		-1  ����
                        //      0   δ�ҵ�
                        //      1   �ҵ�
                        nRet = this.GetRecordID(connection,
                            strRecordID,
                            strStyle,
                            out strTempOutputID,
                            out strError);
                        if (nRet == -1)
                            return -1;
                    }
                    finally
                    {
                        connection.Close();
                    }
                    if (nRet == 0 || strTempOutputID == "")
                    {
                        strError = "δ�ҵ���¼ID '" + strRecordID + "' �ķ��Ϊ'" + strStyle + "'�ļ�¼";
                        return -4;
                    }
                    strRecordID = strTempOutputID;

                    // �ٴμ��һ�·��ص�ID
                    // return:
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = DatabaseUtil.CheckAndGet10RecordID(ref strRecordID,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

                // ���ݷ��Ҫ�󣬷�����Դ·��
                if (StringUtil.IsInList("outputpath", strStyle) == true)
                {
                    strOutputRecordID = DbPath.GetCompressedID(strRecordID);
                }


                // ���ʻ��⿪�ĺ��ţ����ڸ����ʻ�,RefreshUser�ǻ��WriteXml()�Ǽ����ĺ���
                // �����ڿ�ͷ��һ��connection����
                if (bCheckAccount == true &&
                    StringUtil.IsInList("account", this.GetDbType()) == true)   // ע�⣺�����û�������Ļ�Ӧ����this.TypeSafety
                {
                    // ���Ҫ��ü�¼�������˻����¼��������
                    // UserCollection�У��ǾͰ���ص�User��¼
                    // ��������ݿ⣬�Ա��Ժ�����ݿ�����ȡ��
                    // �����ش��ڴ�����ȡ��
                    string strAccountPath = this.FullID + "/" + strRecordID;

                    // return:
                    //		-1  ����
                    //      -4  ��¼������
                    //		0   �ɹ�
                    nRet = this.container.UserColl.SaveUserIfNeed(
                        strAccountPath,
                        out strError);
                    if (nRet <= -1)
                        return nRet;
                }

                // ԭ���Ŀ����������

                //*******************�Լ�¼�Ӷ���************************
                m_recordLockColl.LockForRead(strRecordID, m_nTimeOut);

#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("GetXml()����'" + this.GetCaption("zh-CN") + "/" + strRecordID + "'��¼�Ӷ�����");
#endif
                try //��
                {

                    Connection connection = new Connection(this,
                        this.m_strConnString
#if SUPER
                        ,
                        this.container.SqlServerType == SqlServerType.SQLite && this.FastMode == true ? ConnectionStyle.Global : ConnectionStyle.None
#endif
);
                    connection.Open();

                    /*
                    // ������
                    string strConnectionName = connection.GetHashCode().ToString();
                    this.container.WriteErrorLog("getimage use connection '"+strConnectionName+"'");
                     * */

                    try  //����
                    {
                        /*
                         * 
                         * ע:ֱ��ʹ��GetImage()������Ҳ�ܸ�֪����¼�����ڣ�����û�б�ҪԤ��̽��һ�¼�¼�Ƿ���ڡ�2012/1/8
                        // return:
                        //		-1  ����
                        //      0   ������
                        //      1   ����
                        nRet = this.RecordIsExist(connection,
                            strRecordID,
                            out strError);
                        if (nRet == -1)
                            return -1;


                        if (nRet == 0)
                        {
                            strError = "��¼'" + strRecordID + "'�ڿ��в�����";
                            return -4;
                        }
                         * */

                        byte[] baWholeXml = null;
                        byte[] baPreamble = null;


                        string strXml = null;
                        XmlDocument dom = null;

                        if (string.IsNullOrEmpty(strXPath) == false
                            || StringUtil.IsInList("withresmetadata", strStyle) == true)
                        {
                            // return:
                            //		-1  ����
                            //		-4  ��¼������
                            //      -100    �����ļ�������
                            //		>=0 ��Դ�ܳ���
                            lRet = this.GetImage(connection,
                                null,
                                strRecordID,
                                false,  // "data",
                                0,
                                -1,
                                -1,
                                strStyle,
                                out baWholeXml,
                                out strMetadata,
                                out outputTimestamp,
                                out strError);
                            if (lRet <= -1)
                                return lRet;

                            if (baWholeXml == null && string.IsNullOrEmpty(strXPath) == false)
                            {
                                strError = "����Ȼʹ����xpath����δȡ�����ݣ�����������style�����ȷ����ǰstyle��ֵΪ '" + strStyle + "'��";
                                return -1;
                            }

                            strXml = DatabaseUtil.ByteArrayToString(baWholeXml,
                                out baPreamble);

                            if (strXml != "")
                            {
                                dom = new XmlDocument();
                                dom.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue

                                try
                                {
                                    dom.LoadXml(strXml);
                                }
                                catch (Exception ex)
                                {
                                    strError = "GetXml() �������ݵ�dom����ԭ��" + ex.Message;
                                    return -1;
                                }
                            }
                        }



                        // ����ԴԪ���ݵ������Ҫ�������xml���ݵ�
                        if (StringUtil.IsInList("withresmetadata", strStyle) == true)
                        {
                            /*
                            // ������һ���򵥵ĺ�����һ��
                            // return:
                            //		-1  ����
                            //		-4  ��¼������
        //      -100    �����ļ�������
                            //		>=0 ��Դ�ܳ���
                            lRet = this.GetImage(connection,
                                strRecordID,
                                "data",
                                0,
                                -1,
                                -1,
                                strStyle,
                                out baWholeXml,
                                out strMetadata,
                                out outputTimestamp,
                                out strError);
                            if (lRet <= -1)
                                return lRet;

                            strXml = DatabaseUtil.ByteArrayToString(baWholeXml,
                                out baPreamble);
                             * */

                            if (dom != null/*strXml != ""*/)
                            {
                                /*
                                dom = new XmlDocument();
                                dom.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue

                                try
                                {
                                    dom.LoadXml(strXml);
                                }
                                catch (Exception ex)
                                {
                                    strError = "GetXml() �������ݵ�dom����ԭ��" + ex.Message;
                                    return -1;
                                }
                                */

                                // �ҵ����е�dprms:fileԪ��
                                XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
                                nsmgr.AddNamespace("dprms", DpNs.dprms);
                                XmlNodeList fileList = dom.DocumentElement.SelectNodes("//dprms:file", nsmgr);
                                foreach (XmlNode fileNode in fileList)
                                {
                                    string strObjectID = DomUtil.GetAttr(fileNode, "id");
                                    if (strObjectID == "")
                                        continue;

                                    byte[] baObjectDestBuffer;
                                    string strObjectMetadata;
                                    byte[] baObjectOutputTimestamp;

                                    string strObjectFullID = strRecordID + "_" + strObjectID;
                                    // return:
                                    //		-1  ����
                                    //		-4  ��¼������
                                    //      -100    �����ļ�������
                                    //		>=0 ��Դ�ܳ���
                                    lRet = this.GetImage(connection,
                                        null,
                                        strObjectFullID,
                                        false,  // "data",
                                        lStart,
                                        nLength,
                                        nMaxLength,
                                        "metadata,timestamp",//strStyle,
                                        out baObjectDestBuffer,
                                        out strObjectMetadata,
                                        out baObjectOutputTimestamp,
                                        out strError);
                                    if (lRet <= -1)
                                    {
                                        // ��Դ��¼������
                                        if (lRet == -4)
                                        {
                                            nNotFoundSubRes++;

                                            if (strNotFoundSubResIds != "")
                                                strNotFoundSubResIds += ",";
                                            strNotFoundSubResIds += strObjectID;
                                        }
                                    }

                                    // ����metadata
                                    if (strObjectMetadata != "")
                                    {
                                        Hashtable values = rmsUtil.ParseMedaDataXml(strObjectMetadata,
                                            out strError);
                                        if (values == null)
                                            return -1;

                                        string strObjectTimestamp = ByteArray.GetHexTimeStampString(baObjectOutputTimestamp);

                                        DomUtil.SetAttr(fileNode, "__mime", (string)values["mimetype"]);
                                        DomUtil.SetAttr(fileNode, "__localpath", (string)values["localpath"]);
                                        DomUtil.SetAttr(fileNode, "__size", (string)values["size"]);


                                        // 2007/12/13 new add
                                        string strLastModifyTime = (string)values["lastmodifytime"];
                                        if (String.IsNullOrEmpty(strLastModifyTime) == false)
                                            DomUtil.SetAttr(fileNode, "__lastmodifytime", strLastModifyTime);

                                        DomUtil.SetAttr(fileNode, "__timestamp", strObjectTimestamp);
                                    }
                                }
                            } // end if (strXml != "")

                        } // if (StringUtil.IsInList("withresmetadata", strStyle) == true)

                        // ͨ��xpath��Ƭ�ϵ����
                        if (strXPath != null && strXPath != "")
                        {
                            if (dom != null)
                            {
                                string strLocateXPath = "";
                                string strCreatePath = "";
                                string strNewRecordTemplate = "";
                                string strAction = "";
                                nRet = DatabaseUtil.ParseXPathParameter(strXPath,
                                    out strLocateXPath,
                                    out strCreatePath,
                                    out strNewRecordTemplate,
                                    out strAction,
                                    out strError);
                                if (nRet == -1)
                                    return -1;

                                if (strLocateXPath == "")
                                {
                                    strError = "xpath���ʽ�е�locate��������Ϊ��ֵ";
                                    return -1;
                                }

                                XmlNode node = dom.DocumentElement.SelectSingleNode(strLocateXPath);
                                if (node == null)
                                {
                                    strError = "��dom��δ�ҵ�XPathΪ'" + strLocateXPath + "'�Ľڵ�";
                                    return -10;
                                }

                                string strOutputText = "";
                                if (node.NodeType == XmlNodeType.Element)
                                {
                                    strOutputText = node.OuterXml;
                                }
                                else if (node.NodeType == XmlNodeType.Attribute)
                                {
                                    strOutputText = node.Value;
                                }
                                else
                                {
                                    strError = "ͨ��xpath '" + strXPath + "' �ҵ��Ľڵ�����Ͳ�֧�֡�";
                                    return -1;
                                }

                                byte[] baOutputText = DatabaseUtil.StringToByteArray(strOutputText,
                                    baPreamble);

                                long lRealLength;
                                // return:
                                //		-1  ����
                                //		0   �ɹ�
                                nRet = ConvertUtil.GetRealLength(lStart,
                                    nLength,
                                    baOutputText.Length,
                                    nMaxLength,
                                    out lRealLength,
                                    out strError);
                                if (nRet == -1)
                                    return -1;

                                destBuffer = new byte[lRealLength];

                                Array.Copy(baOutputText,
                                    lStart,
                                    destBuffer,
                                    0,
                                    lRealLength);
                            }
                            else
                            {
                                destBuffer = new byte[0];
                            }

                            return 0;
                        } // end if (strXPath != null && strXPath != "")

                        if (dom != null)
                        {
                            // ����ԴԪ���ݵ������Ҫ�������xml���ݵ�
                            if (StringUtil.IsInList("withresmetadata", strStyle) == true)
                            {
                                // ʹ��XmlTextWriter�����utf8�ı��뷽ʽ
                                MemoryStream ms = new MemoryStream();
                                XmlTextWriter textWriter = new XmlTextWriter(ms, Encoding.UTF8);
                                dom.Save(textWriter);
                                //dom.Save(ms);

                                long lRealLength;
                                // return:
                                //		-1  ����
                                //		0   �ɹ�
                                nRet = ConvertUtil.GetRealLength(lStart,
                                    nLength,
                                    (int)ms.Length,
                                    nMaxLength,
                                    out lRealLength,
                                    out strError);
                                if (nRet == -1)
                                    return -1;

                                destBuffer = new byte[lRealLength];

                                // ��Ԫ�ص���Ϣ����ܳ���
                                long nWithMetedataTotalLength = ms.Length;

                                ms.Seek(lStart, SeekOrigin.Begin);
                                ms.Read(destBuffer,
                                    0,
                                    destBuffer.Length);
                                ms.Close();

                                if (nNotFoundSubRes > 0)
                                {
                                    strError = "��¼" + strRecordID + "��idΪ " + strNotFoundSubResIds + " ���¼���Դ��¼������";
                                    nAdditionError = -50; // ��һ�������¼���Դ��¼������
                                }

                                return nWithMetedataTotalLength;
                            }
                        } // end if (dom != null)

                        if (baWholeXml != null)
                        {
                            strError = "dp2Kernel GetXml()������ �������ظ� GetImage() �����";
                            return -1;
                        }

                        // ��ʹ��xpath�����
                        // return:
                        //		-1  ����
                        //		-4  ��¼������
                        //      -100    �����ļ�������
                        //		>=0 ��Դ�ܳ���
                        lRet = this.GetImage(connection,
                            null,
                            strRecordID,
                            false,  // "data",
                            lStart,
                            nLength,
                            nMaxLength,
                            strStyle,
                            out destBuffer,
                            out strMetadata,
                            out outputTimestamp,
                            out strError);

                        if (lRet >= 0 && nNotFoundSubRes > 1)
                        {
                            strError = "��¼ " + strRecordID + " �� id Ϊ " + strNotFoundSubResIds + " ���¼���Դ��¼������";
                            nAdditionError = -50; // ��һ�������¼���Դ��¼������
                        }

                        return lRet;
                    }
                    catch (SqlException sqlEx)
                    {
                        strError = "ȡ��¼ '" + strRecordID + "' ʱ���� ԭ��: " + GetSqlErrors(sqlEx);

                        // TODO: ���������ʱ�����Ƿ���Ҫǰ������? ��Ҫ�����ִ�������ר�ŷֱ����
                        /*
                        if (sqlEx.Errors is SqlErrorCollection)
                            strError = "���ݿ�'" + this.GetCaption("zh") + "'��δ��ʼ����";
                        else
                            strError = "ȡ��¼'" + strRecordID + "'�����ˣ�ԭ��:" + sqlEx.Message;
                         * */
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        strError = "ȡ��¼ '" + strRecordID + "' �����ˣ�ԭ��: " + ex.Message;
                        return -1;
                    }
                    finally //����
                    {
                        connection.Close();
                    }
                }
                finally //��
                {

                    //*********�Լ�¼�����******
                    m_recordLockColl.UnlockForRead(strRecordID);
#if DEBUG_LOCK_SQLDATABASE
					this.container.WriteDebugInfo("GetXml()����'" + this.GetCaption("zh-CN") + "/" + strRecordID + "'��¼�������");
#endif
                }
            }
            catch (Exception ex)
            {
                strError = "ȡ��¼'" + strRecordID + "'�����ˣ�ԭ��:" + ex.Message;
                return -1;
            }
            finally
            {
#if SUPER
                if (this.FastMode == true)
                {
                    m_db_lock.ReleaseWriterLock();
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("GetXml()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
                }
                else
                {
#endif
                    //***********�����ݿ�����*****************
                    m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("GetXml()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif

#if SUPER
                }
#endif
            }
        }


        // �õ�xml����
        // ��:��ȫ��,���ⲿ��
        // return:
        //      -1  ����
        //      -4  ��¼������
        //      -100    �����ļ�������
        //      0   ��ȷ
        public override int GetXmlData(string strID,
            out string strXml,
            out string strError)
        {
            strXml = "";
            strError = "";

            strID = DbPath.GetID10(strID);

            Connection connection = new Connection(this,
                this.m_strConnStringPooling
#if SUPER
                ,  // ��Ϊ��;�����жϣ����Կ���ʹ��pooling
                this.container.SqlServerType == SqlServerType.SQLite && this.FastMode == true ? ConnectionStyle.Global : ConnectionStyle.None
#endif
);
            connection.Open();
            try
            {
                // return:
                //      -1  ����
                //      -4  ��¼������
                //      -100    �����ļ�������
                //      0   ��ȷ
                return this.GetXmlString(connection,
                    strID,
                    out strXml,
                    out strError);
            }
            finally
            {
                connection.Close();
            }
        }


        // ȡxml���ݵ��ַ���,��װGetXmlData()
        // ��:����ȫ
        // return:
        //      -1  ����
        //      -4  ��¼������
        //      -100    �����ļ�������
        //      0   ��ȷ
        private int GetXmlString(Connection connection,
            string strID,
            out string strXml,
            out string strError)
        {
            byte[] baPreamble;
            // return:
            //      -1  ����
            //      -4  ��¼������
            //      -100    �����ļ�������
            //      0   ��ȷ
            return this.GetXmlData(connection,
                null,
                strID,
                false,  // "data",
                out strXml,
                out baPreamble,
                out strError);
        }

        // �õ�xml�ַ���,��װGetImage()
        // ��: ����ȫ
        // parameters:
        //      row_info    ���row_info != null�������strID������
        //		strID       ��¼ID��������rowinfo == null ������»�ȡ����Ϣ
        // return:
        //      -1  ����
        //      -4  ��¼������
        //      -100    �����ļ�������
        //      0   ��ȷ
        private int GetXmlData(Connection connection,
            RecordRowInfo row_info,
            string strID,
            // string strFieldName,
            bool bTempField,
            out string strXml,
            out byte[] baPreamble,
            out string strError)
        {
            baPreamble = new byte[0];
            strXml = "";
            strError = "";

            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            byte[] newXmlBuffer;
            byte[] outputTimestamp;
            string strMetadata;
            // return:
            //		-1  ����
            //		-4  ��¼������
            //      -100    �����ļ�������
            //		>=0 ��Դ�ܳ���
            long lRet = this.GetImage(connection,
                row_info,
                strID,
                // strFieldName,
                bTempField,
                0,
                -1,
                -1,
                "data", // style
                out newXmlBuffer,
                out strMetadata,
                out outputTimestamp,
                out strError);
            if (lRet <= -1)
                return (int)lRet;

            strXml = DatabaseUtil.ByteArrayToString(newXmlBuffer,
                out baPreamble);
            return 0;
        }

        // ��ָ����Χ����Դ
        // parameter:
        //		strID       ��¼ID
        //		nStart      ��ʼλ��
        //		nLength     ���� -1:��ʼ������
        //		destBuffer  out�����������ֽ�����
        //		timestamp   out����������ʱ���
        //		strError    out���������س�����Ϣ
        // return:
        // return:
        //		-1  ����
        //		-4  ��¼������
        //		>=0 ��Դ�ܳ���
        public override long GetObject(string strRecordID,
            string strObjectID,
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
            outputTimestamp = null;
            strMetadata = "";
            strError = "";

            strRecordID = DbPath.GetID10(strRecordID);
            //********�����ݿ�Ӷ���**************
            m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
			this.container.WriteDebugInfo("GetObject()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            try
            {
                //*******************�Լ�¼�Ӷ���************************
                m_recordLockColl.LockForRead(strRecordID, m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("GetObject()����'" + this.GetCaption("zh-CN") + "/" + strRecordID + "'��¼�Ӷ�����");
#endif
                try  // ��¼��
                {

                    Connection connection = new Connection(this,
                        this.m_strConnString);
                    connection.Open();
                    try // ����
                    {

                        string strObjectFullID = strRecordID + "_" + strObjectID;
                        // return:
                        //		-1  ����
                        //		-4  ��¼������
                        //      -100    �����ļ�������
                        //		>=0 ��Դ�ܳ���
                        return this.GetImage(connection,
                            null,
                            strObjectFullID,
                            false,  // "data",
                            lStart,
                            nLength,
                            nMaxLength,
                            strStyle,
                            out destBuffer,
                            out strMetadata,
                            out outputTimestamp,
                            out strError);
                    }
                    catch (SqlException sqlEx)
                    {
                        strError = GetSqlErrors(sqlEx);

                        /*
                        if (sqlEx.Errors is SqlErrorCollection)
                            strError = "���ݿ�'" + this.GetCaption("zh") + "'��δ��ʼ����";
                        else
                            strError = sqlEx.Message;
                         * */
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        strError = "6: " + ex.Message;
                        return -1;
                    }
                    finally // ����
                    {
                        connection.Close();
                    }
                }
                finally // ��¼��
                {
                    //*************�Լ�¼�����***********
                    m_recordLockColl.UnlockForRead(strRecordID);
#if DEBUG_LOCK_SQLDATABASE
					this.container.WriteDebugInfo("GetObject()����'" + this.GetCaption("zh-CN") + "/" + strRecordID + "'��¼�������");
#endif
                }
            }
            finally //����
            {
                //******�����ݿ�����*********
                m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("GetObject()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }
        }

        // 2012/1/21
        // ���һ��Ψһ�ġ������ķ�Χ�ĳ���
        static long GetTotalLength(string strRange,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(strRange) == true)
                return 0;

            // ׼��rangelist
            RangeList rangeList = null;
            try
            {
                rangeList = new RangeList(strRange);
            }
            catch (Exception ex)
            {
                strError = "���ַ��� '" + strRange + "' ���� RangeList ʱ����: " + ex.Message;
                return -1;
            }
            if (rangeList.Count != 1)
            {
                strError = "��Χ�ַ���������һ����������ֹ��Ԫ��(�������� '"+strRange+"')";
                return -1;
            }
            if (rangeList[0].lStart != 0)
            {
                strError = "��Χ�Ŀ�ʼ������0��(�������� '" + strRange + "')";
                return -1;
            }
            return rangeList[0].lLength;
        }


        // ��ָ����Χ����Դ
        // parameter:
        //      row_info    ���row_info != null�������strID������
        //		strID       ��¼ID��������row_info == null������»������Ϣ
        //      bTempField  �Ƿ���Ҫ����ʱ data �ֶ�����ȡ����? (��û��reverse������£���ʱdata�ֶ�ָ newdata �ֶ�)
        //		nStart      ��ʼλ��
        //		nLength     ���� -1:��ʼ������
        //		nMaxLength  ��󳤶�,��Ϊ-1ʱ,��ʾ����
        //		destBuffer  out�����������ֽ�����
        //		timestamp   out����������ʱ���
        //		strError    out���������س�����Ϣ
        // return:
        //		-1  ����
        //		-4  ��¼������
        //      -100    �����ļ�������
        //		>=0 ��Դ�ܳ���
        private long GetImage(Connection connection,
            RecordRowInfo row_info,
            string strID,
            // string strImageFieldName,
            bool bTempField,    // �Ƿ���Ҫ����ʱ data �ֶ�����ȡ����?
            long lStart,
            int nReadLength,
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

            // ������Ӷ���
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            if (connection.SqlServerType == SqlServerType.MsSqlServer)
            {
                long lTotalLength = 0;
                byte[] textPtr = null;
                string strDataFieldName = "data";

                bool bObjectFile = false;

                if (row_info != null)
                {
                    bool bReverse = false;  // �����־�����Ϊfalse����ʾ data Ϊ��ʽ���ݣ�newdataΪ��ʱ����

                    string strRange = row_info.Range;

                    if (String.IsNullOrEmpty(strRange) == false
        && strRange[0] == '#')
                    {
                        bObjectFile = true;
                        strRange = strRange.Substring(1);

                        lTotalLength = -1;  // ��ʾ��ȡ��
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(strRange) == false
                            && strRange[0] == '!')
                        {
                            bReverse = true;
                            strRange = strRange.Substring(1);
                        }

                        if (bTempField == true)
                            bReverse = !bReverse;

                        strDataFieldName = "data";
                        if (bReverse == true)
                            strDataFieldName = "newdata";

                        if (bReverse == false)
                        {
                            lTotalLength = row_info.data_length;
                            textPtr = row_info.data_textptr;
                        }
                        else
                        {
                            lTotalLength = row_info.newdata_length;
                            textPtr = row_info.newdata_textptr;
                        }
                    }

                    if (StringUtil.IsInList("timestamp", strStyle) == true)
                    {
                        if (bReverse == false || bObjectFile == true)
                            outputTimestamp = ByteArray.GetTimeStampByteArray(row_info.TimestampString);
                        else
                            outputTimestamp = ByteArray.GetTimeStampByteArray(row_info.NewTimestampString);
                    }

                    if (StringUtil.IsInList("metadata", strStyle) == true)
                        strMetadata = row_info.Metadata;
                }
                else
                {
                    // ��Ҫ��ʱ�������Ϣ
                    strID = DbPath.GetID10(strID);

                    // ���������ַ���
                    string strPartComm = "";

                    // 1.textPtr
                    if (StringUtil.IsInList("data", strStyle) == true)
                    {
                        if (string.IsNullOrEmpty(strPartComm) == false)
                            strPartComm += ",";

                        strPartComm += " @textPtr=TEXTPTR(data), ";
                        strPartComm += " @textPtrNew=TEXTPTR(newdata)";
                    }

                    // filename һ��Ҫ��
                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " @filename=filename, ";
                    strPartComm += " @newfilename=newfilename";

                    // 2.length,һ��Ҫ��
                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " @Length=DataLength(data), ";
                    strPartComm += " @LengthNew=DataLength(newdata)";

                    // 3.timestamp
                    if (StringUtil.IsInList("timestamp", strStyle) == true)
                    {
                        if (string.IsNullOrEmpty(strPartComm) == false)
                            strPartComm += ",";
                        strPartComm += " @dptimestamp=dptimestamp,";
                        strPartComm += " @newdptimestamp=newdptimestamp";
                    }
                    // 4.metadata
                    if (StringUtil.IsInList("metadata", strStyle) == true)
                    {
                        if (string.IsNullOrEmpty(strPartComm) == false)
                            strPartComm += ",";
                        strPartComm += " @metadata=metadata";
                    }
                    // 5.range��һ��Ҫ�У������жϷ���
                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " @range=range";

                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " @testid=id";

                    string strCommand = "";
                    // DataLength()����int����
                    strCommand = "use " + this.m_strSqlDbName + " "
                        + " SELECT "
                        + strPartComm + " "
                        + " FROM records WHERE id=@id";

                    strCommand += " use master " + "\n";

                    using (SqlCommand command = new SqlCommand(strCommand,
                        connection.SqlConnection))
                    {
                        SqlParameter idParam =
                            command.Parameters.Add("@id",
                            SqlDbType.NVarChar);
                        idParam.Value = strID;


                        SqlParameter testidParam =
                                command.Parameters.Add("@testid",
                                SqlDbType.NVarChar,
                                255);
                        testidParam.Direction = ParameterDirection.Output;

                        // 1.textPtr
                        SqlParameter textPtrParam = null;
                        SqlParameter textPtrParamNew = null;
                        if (StringUtil.IsInList("data", strStyle) == true)
                        {
                            textPtrParam =
                                command.Parameters.Add("@textPtr",
                                SqlDbType.VarBinary,
                                16);
                            textPtrParam.Direction = ParameterDirection.Output;

                            textPtrParamNew =
                command.Parameters.Add("@textPtrNew",
                SqlDbType.VarBinary,
                16);
                            textPtrParamNew.Direction = ParameterDirection.Output;
                        }

                        SqlParameter filename = null;
                        SqlParameter newfilename = null;
                        // 
                        filename =
                            command.Parameters.Add("@filename",
                            SqlDbType.NVarChar,
                            255);
                        filename.Direction = ParameterDirection.Output;

                        newfilename =
                            command.Parameters.Add("@newfilename",
                            SqlDbType.NVarChar,
                            255);
                        newfilename.Direction = ParameterDirection.Output;


                        // 2.length,һ��Ҫ����
                        SqlParameter lengthParam =
                            command.Parameters.Add("@length",
                            SqlDbType.Int);
                        lengthParam.Direction = ParameterDirection.Output;

                        SqlParameter lengthParamNew =
                            command.Parameters.Add("@lengthNew",
                            SqlDbType.Int);
                        lengthParamNew.Direction = ParameterDirection.Output;

                        // 3.timestamp
                        SqlParameter timestampParam = null;
                        SqlParameter newtimestampParam = null;
                        if (StringUtil.IsInList("timestamp", strStyle) == true)
                        {
                            timestampParam =
                                command.Parameters.Add("@dptimestamp",
                                SqlDbType.NVarChar,
                                100);
                            timestampParam.Direction = ParameterDirection.Output;

                            newtimestampParam =
            command.Parameters.Add("@newdptimestamp",
            SqlDbType.NVarChar,
            100);
                            newtimestampParam.Direction = ParameterDirection.Output;
                        }
                        // 4.metadata
                        SqlParameter metadataParam = null;
                        if (StringUtil.IsInList("metadata", strStyle) == true)
                        {
                            metadataParam =
                                command.Parameters.Add("@metadata",
                                SqlDbType.NVarChar,
                                4000);
                            metadataParam.Direction = ParameterDirection.Output;
                        }
                        // 5.range��һ��Ҫ��
                        SqlParameter rangeParam =
                                command.Parameters.Add("@range",
                                SqlDbType.NVarChar,
                                4000);
                        rangeParam.Direction = ParameterDirection.Output;


                        try
                        {
                            // ִ������
                            nRet = command.ExecuteNonQuery();
                            /*
                For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command. For all other types of statements, the return value is -1. If a rollback occurs, the return value is also -1.

                             * */
                        }
                        catch (Exception ex)
                        {
                            string strConnectionName = command.Connection.GetHashCode().ToString();
                            this.container.KernelApplication.WriteErrorLog("GetImage() ExecuteNonQuery exception: " + ex.Message + "; connection hashcode='" + strConnectionName + "'");
                            throw ex;
                        }

                        if (testidParam == null
                            || (testidParam.Value is System.DBNull))
                        {
                            strError = "��¼'" + strID + "'�ڿ��в�����";
                            return -4;
                        }

                        // 5.range��һ���᷵��
                        string strRange = "";
                        if (rangeParam != null
                            && (!(rangeParam.Value is System.DBNull)))
                            strRange = (string)rangeParam.Value;

                        bool bReverse = false;  // �����־�����Ϊfalse����ʾ data Ϊ��ʽ���ݣ�newdataΪ��ʱ����


                        if (String.IsNullOrEmpty(strRange) == false
        && strRange[0] == '#')
                        {
                            bObjectFile = true;
                            strRange = strRange.Substring(1);

                            lTotalLength = -1;  // ��ʾ��ȡ��

                            if (row_info == null)
                                row_info = new RecordRowInfo();

                            // 
                            if (filename != null
                                && (!(filename.Value is System.DBNull)))
                            {
                                row_info.FileName = (string)filename.Value;
                            }

                            if (newfilename != null
        && (!(newfilename.Value is System.DBNull)))
                            {
                                row_info.NewFileName = (string)newfilename.Value;
                            }
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(strRange) == false
                                && strRange[0] == '!')
                            {
                                bReverse = true;
                                strRange = strRange.Substring(1);
                            }

                            if (bTempField == true)
                                bReverse = !bReverse;

                            strDataFieldName = "data";
                            if (bReverse == true)
                                strDataFieldName = "newdata";


                            // 1.textPtr
                            if (StringUtil.IsInList("data", strStyle) == true)
                            {
                                if (bReverse == false)
                                {
                                    if (textPtrParam != null
                                        && (!(textPtrParam.Value is System.DBNull)))
                                    {
                                        textPtr = (byte[])textPtrParam.Value;
                                    }
                                    else
                                    {
                                        textPtr = null; // 2013/2/15
                                        destBuffer = new byte[0];
                                        // return 0;  // ������ǰ���أ������ timestamp ����Ϊ��
                                    }
                                }
                                else
                                {
                                    if (textPtrParamNew != null
                    && (!(textPtrParamNew.Value is System.DBNull)))
                                    {
                                        textPtr = (byte[])textPtrParamNew.Value;
                                    }
                                    else
                                    {
                                        textPtr = null; // 2013/2/11
                                        destBuffer = new byte[0];
                                        // return 0;   // ������ǰ���أ������ timestamp ����Ϊ��
                                    }
                                }
                            }

                            // 2.length,һ���᷵��
                            if (bReverse == false)
                            {
                                if (lengthParam != null
                                    && (!(lengthParam.Value is System.DBNull)))
                                {
                                    lTotalLength = (int)lengthParam.Value;
                                    // TODO: ��仰�����׳��쳣����Ҫ���Բ��� 2011/1/7
                                }
                            }
                            else
                            {
                                if (lengthParamNew != null
                    && (!(lengthParamNew.Value is System.DBNull)))
                                    lTotalLength = (int)lengthParamNew.Value;
                            }

                        }


                        // 3.timestamp
                        if (StringUtil.IsInList("timestamp", strStyle) == true)
                        {
                            if (bReverse == false || bObjectFile == true)
                            {
                                if (timestampParam != null)
                                {
                                    if (!(timestampParam.Value is System.DBNull))
                                    {
                                        string strOutputTimestamp = (string)timestampParam.Value;
                                        outputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);//Encoding.UTF8.GetBytes(strOutputTimestamp);
                                    }
                                    else
                                    {
                                        // 2008/3/13 new add
                                        outputTimestamp = null;
                                    }
                                }
                            }
                            else
                            {
                                if (newtimestampParam != null)
                                {
                                    if (!(newtimestampParam.Value is System.DBNull))
                                    {
                                        string strOutputTimestamp = (string)newtimestampParam.Value;
                                        outputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);//Encoding.UTF8.GetBytes(strOutputTimestamp);
                                    }
                                    else
                                    {
                                        // 2008/3/13 new add
                                        outputTimestamp = null;
                                    }
                                }

                            }
                        }
                        // 4.metadata
                        if (StringUtil.IsInList("metadata", strStyle) == true)
                        {
                            if (metadataParam != null
                                && (!(metadataParam.Value is System.DBNull)))
                            {
                                strMetadata = (string)metadataParam.Value;
                            }
                        }
                    } // end of using command
                }

                string strObjectFilename = "";
                if (bObjectFile == true)
                {
                    if (string.IsNullOrEmpty(this.m_strObjectDir) == true)
                    {
                        strError = "���ݿ���δ���ö����ļ�Ŀ¼�������ݼ�¼�г��������ö����ļ������";
                        return -1;
                    }

                    if (bTempField == false)
                    {
                        if (string.IsNullOrEmpty(row_info.FileName) == true)
                        {
                            /*
                            strError = "����Ϣ��û�ж����ļ� ��ʽ�ļ���";
                            return -1;
                             * */
                            // ��û���Ѿ���ɵĶ����ļ�
                            destBuffer = new byte[0];
                            return 0;
                        }

                        Debug.Assert(string.IsNullOrEmpty(row_info.FileName) == false, "");

                        strObjectFilename = GetObjectFileName(row_info.FileName);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(row_info.NewFileName) == true)
                        {
                            // ��û����ʱ�Ķ����ļ�
                            destBuffer = new byte[0];
                            return 0;
                        }

                        Debug.Assert(string.IsNullOrEmpty(row_info.NewFileName) == false, "");

                        strObjectFilename = GetObjectFileName(row_info.NewFileName);
                    }



                    FileInfo fi = new FileInfo(strObjectFilename);
                    if (fi.Exists == false)
                    {
                        // TODO: ��Ҫֱ�ӻ㱨�����ļ���
                        strError = "�����ļ� '" + strObjectFilename + "' ������";
                        return -100;
                    }
                    lTotalLength = fi.Length;
                }

                // ��Ҫ��ȡ����ʱ,�Ż�ȡ����
                if (StringUtil.IsInList("data", strStyle) == true)
                {
                    if (nReadLength == 0)  // ȡ0����
                    {
                        destBuffer = new byte[0];
                        return lTotalLength;    // >= 0
                    }

                    long lOutputLength = 0;
                    // �õ�ʵ�ʶ��ĳ���
                    // return:
                    //		-1  ����
                    //		0   �ɹ�
                    nRet = ConvertUtil.GetRealLength(lStart,
                        nReadLength,
                        lTotalLength,
                        nMaxLength,
                        out lOutputLength,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    Debug.Assert(lOutputLength < Int32.MaxValue && lOutputLength > Int32.MinValue, "");

                    // 2012/1/21
                    if (lTotalLength == 0)  // �ܳ���Ϊ0
                    {
                        destBuffer = new byte[0];
                        return lTotalLength;
                    }

                    // �Ӷ����ļ���ȡ
                    if (bObjectFile == true)
                    {
                        Debug.Assert(string.IsNullOrEmpty(strObjectFilename) == false, "");

                        destBuffer = new Byte[lOutputLength];

                        try
                        {
                            using (FileStream s = File.Open(
            strObjectFilename,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite))
                            {
                                s.Seek(lStart, SeekOrigin.Begin);
                                s.Read(destBuffer,
                                    0,
                                    (int)lOutputLength);

                                // lTotalLength = s.Length;
                            }
                        }
                        catch (FileNotFoundException /* ex */)
                        {
                            // TODO: ��Ҫֱ�ӻ㱨�����ļ���
                            strError = "�����ļ� '" + strObjectFilename + "' ������";
                            return -100;
                        }
                        return lTotalLength;
                    }

                    if (textPtr == null)
                    {
                        strError = "textPtrΪnull";
                        return -1;
                    }

                    // READTEXT����:
                    // text_ptr: ��Ч�ı�ָ�롣text_ptr ������ binary(16)��
                    // offset:   ��ʼ��ȡimage����֮ǰ�������ֽ�����ʹ�� text �� image ��������ʱ�����ַ�����ʹ�� ntext ��������ʱ����
                    //			 ʹ�� ntext ��������ʱ��offset ���ڿ�ʼ��ȡ����ǰ�������ַ�����
                    //			 ʹ�� text �� image ��������ʱ��offset ���ڿ�ʼ��ȡ����ǰ�������ֽ�����
                    // size:     ��Ҫ��ȡ���ݵ��ֽ�����ʹ�� text �� image ��������ʱ�����ַ�����ʹ�� ntext ��������ʱ������� size �� 0�����ʾ��ȡ�� 4 KB �ֽڵ����ݡ�
                    // HOLDLOCK: ʹ�ı�ֵһֱ��������������������û����Զ�ȡ��ֵ�����ǲ��ܶ�������޸ġ�

                    string strCommand = "use " + this.m_strSqlDbName + " "
                       + " READTEXT records." + strDataFieldName
                       + " @text_ptr"
                       + " @offset"
                       + " @size"
                       + " HOLDLOCK";

                    strCommand += " use master " + "\n";

                    using (SqlCommand command = new SqlCommand(strCommand,
                        connection.SqlConnection))
                    {

                        SqlParameter text_ptrParam =
                            command.Parameters.Add("@text_ptr",
                            SqlDbType.VarBinary,
                            16);
                        text_ptrParam.Value = textPtr;

                        SqlParameter offsetParam =
                            command.Parameters.Add("@offset",
                            SqlDbType.Int);  // old Int
                        offsetParam.Value = lStart;

                        SqlParameter sizeParam =
                            command.Parameters.Add("@size",
                            SqlDbType.Int);  // old Int
                        sizeParam.Value = lOutputLength;

                        destBuffer = new Byte[lOutputLength];

                        SqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                        try
                        {
                            dr.Read();
                            dr.GetBytes(0,
                                0,
                                destBuffer,
                                0,
                                System.Convert.ToInt32(sizeParam.Value));
                        }
                        catch (Exception ex)
                        {
                            string strConnectionName = command.Connection.GetHashCode().ToString();
                            this.container.KernelApplication.WriteErrorLog("GetImage() ExecuteReader exception: " + ex.Message + "; connection hashcode='" + strConnectionName + "'");
                            throw ex;
                        }
                        finally
                        {
                            dr.Close();
                        }
                    } // end of using command
                }

                return lTotalLength;
            }
            else if (connection.SqlServerType == SqlServerType.SQLite)
            {
                long lTotalLength = 0;
                // byte[] textPtr = null;
                // string strDataFieldName = "data";

                bool bObjectFile = false;

                if (row_info != null)
                {
                    string strRange = row_info.Range;

                    if (String.IsNullOrEmpty(strRange) == false
        && strRange[0] == '#')
                    {
                        bObjectFile = true;
                        strRange = strRange.Substring(1);

                        lTotalLength = -1;  // ��ʾ��ȡ��
                    }
                    else
                    {
                        bObjectFile = true;
                    }

                    if (StringUtil.IsInList("timestamp", strStyle) == true)
                    {
                        if (bObjectFile == true)
                            outputTimestamp = ByteArray.GetTimeStampByteArray(row_info.TimestampString);
                        else
                            outputTimestamp = ByteArray.GetTimeStampByteArray(row_info.NewTimestampString);
                    }

                    if (StringUtil.IsInList("metadata", strStyle) == true)
                        strMetadata = row_info.Metadata;
                }
                else
                {
                    // ��Ҫ��ʱ�������Ϣ
                    strID = DbPath.GetID10(strID);

                    // ���������ַ���
                    string strPartComm = "";
                    int nColIndex = 0;

                    // filename һ��Ҫ��
                    int nFileNameColIndex = -1;
                    int nNewFileNameColIndex = -1;
                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " filename, ";
                    nFileNameColIndex = nColIndex++;
                    strPartComm += " newfilename";
                    nNewFileNameColIndex = nColIndex++;

                    // 3.timestamp
                    int nTimestampColIndex = -1;
                    int nNewTimestampColIndex = -1;
                    if (StringUtil.IsInList("timestamp", strStyle) == true)
                    {
                        if (string.IsNullOrEmpty(strPartComm) == false)
                            strPartComm += ",";
                        strPartComm += " dptimestamp,";
                        nTimestampColIndex = nColIndex++;
                        strPartComm += " newdptimestamp";
                        nNewTimestampColIndex = nColIndex++;
                    }
                    // 4.metadata
                    int nMetadataColIndex = -1;
                    if (StringUtil.IsInList("metadata", strStyle) == true)
                    {
                        if (string.IsNullOrEmpty(strPartComm) == false)
                            strPartComm += ",";
                        strPartComm += " metadata";
                        nMetadataColIndex = nColIndex++;
                    }
                    // 5.range��һ��Ҫ�У������жϷ���
                    int nRangeColIndex = -1;
                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " range";
                    nRangeColIndex = nColIndex++;

                    int nIdColIndex = -1;
                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " id";
                    nIdColIndex = nColIndex++;

                    string strCommand = "";
                    // DataLength()����int����
                    strCommand = " SELECT "
                        + strPartComm + " "
                        + " FROM records WHERE id=@id";

                    using (SQLiteCommand command = new SQLiteCommand(strCommand,
                        connection.SQLiteConnection))
                    {

                        SQLiteParameter idParam =
                            command.Parameters.Add("@id",
                            DbType.String);
                        idParam.Value = strID;

                        try
                        {
                            // ִ������
                            SQLiteDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                            try
                            {
                                if (dr == null || dr.HasRows == false)
                                {
                                    strError = "��¼ '" + strID + "' �ڿ��в�����";
                                    return -4;
                                }

                                dr.Read();

                                // 5.range��һ���᷵��
                                string strRange = "";

                                if (!dr.IsDBNull(nRangeColIndex))
                                    strRange = (string)dr[nRangeColIndex];

                                bool bReverse = false;  // �����־�����Ϊfalse����ʾ data Ϊ��ʽ���ݣ�newdataΪ��ʱ����

                                if (String.IsNullOrEmpty(strRange) == false
                && strRange[0] == '#')
                                {
                                    bObjectFile = true;
                                    strRange = strRange.Substring(1);

                                    lTotalLength = -1;  // ��ʾ��ȡ��

                                    if (row_info == null)
                                        row_info = new RecordRowInfo();
                                    // 
                                    if (nFileNameColIndex != -1 && !dr.IsDBNull(nFileNameColIndex))
                                    {
                                        row_info.FileName = (string)dr[nFileNameColIndex];
                                    }

                                    if (nNewFileNameColIndex != -1 && !dr.IsDBNull(nNewFileNameColIndex))
                                    {
                                        row_info.NewFileName = (string)dr[nNewFileNameColIndex];
                                    }
                                }

                                // 3.timestamp
                                if (StringUtil.IsInList("timestamp", strStyle) == true)
                                {
                                    if (bReverse == false || bObjectFile == true)
                                    {
                                        if (nTimestampColIndex != -1 && !dr.IsDBNull(nTimestampColIndex))
                                        {
                                            string strOutputTimestamp = (string)dr[nTimestampColIndex];
                                            outputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);//Encoding.UTF8.GetBytes(strOutputTimestamp);
                                        }
                                        else
                                            outputTimestamp = null;
                                    }
                                    else
                                    {
                                        if (nNewTimestampColIndex != -1 && !dr.IsDBNull(nNewTimestampColIndex))
                                        {
                                            string strOutputTimestamp = (string)dr[nNewTimestampColIndex];
                                            outputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);//Encoding.UTF8.GetBytes(strOutputTimestamp);
                                        }
                                        else
                                            outputTimestamp = null;
                                    }
                                }

                                // 4.metadata
                                if (StringUtil.IsInList("metadata", strStyle) == true)
                                {
                                    if (nMetadataColIndex != -1 && !dr.IsDBNull(nMetadataColIndex))
                                    {
                                        strMetadata = (string)dr[nMetadataColIndex];
                                    }
                                }
                            }
                            finally
                            {
                                dr.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            string strConnectionName = command.Connection.GetHashCode().ToString();
                            this.container.KernelApplication.WriteErrorLog("GetImage() ExecuteNonQuery exception: " + ex.Message + "; connection hashcode='" + strConnectionName + "'");
                            throw ex;
                        }
                    } // end of using command
                }

                string strObjectFilename = "";

                {
                    if (string.IsNullOrEmpty(this.m_strObjectDir) == true)
                    {
                        strError = "���ݿ���δ���ö����ļ�Ŀ¼�������ݼ�¼�г��������ö����ļ������";
                        return -1;
                    }

                    if (bTempField == false)
                    {
                        if (string.IsNullOrEmpty(row_info.FileName) == true)
                        {
                            /*
                            strError = "����Ϣ��û�ж����ļ� ��ʽ�ļ���";
                            return -1;
                             * */
                            // ��û���Ѿ���ɵĶ����ļ�
                            destBuffer = new byte[0];
                            return 0;
                        }

                        Debug.Assert(string.IsNullOrEmpty(row_info.FileName) == false, "");

                        strObjectFilename = GetObjectFileName(row_info.FileName);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(row_info.NewFileName) == true)
                        {
                            // ��û����ʱ�Ķ����ļ�
                            destBuffer = new byte[0];
                            return 0;
                        }

                        Debug.Assert(string.IsNullOrEmpty(row_info.NewFileName) == false, "");

                        strObjectFilename = GetObjectFileName(row_info.NewFileName);
                    }

                    FileInfo fi = new FileInfo(strObjectFilename);
                    if (fi.Exists == false)
                    {
                        // TODO: ��Ҫֱ�ӻ㱨�����ļ���
                        strError = "�����ļ� '" + strObjectFilename + "' ������";
                        return -100;
                    }
                    lTotalLength = fi.Length;
                }

                // ��Ҫ��ȡ����ʱ,�Ż�ȡ����
                if (StringUtil.IsInList("data", strStyle) == true)
                {
                    if (nReadLength == 0)  // ȡ0����
                    {
                        destBuffer = new byte[0];
                        return lTotalLength;    // >= 0
                    }

                    long lOutputLength = 0;
                    // �õ�ʵ�ʶ��ĳ���
                    // return:
                    //		-1  ����
                    //		0   �ɹ�
                    nRet = ConvertUtil.GetRealLength(lStart,
                        nReadLength,
                        lTotalLength,
                        nMaxLength,
                        out lOutputLength,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // 2012/1/21
                    if (lTotalLength == 0)  // �ܳ���Ϊ0
                    {
                        destBuffer = new byte[0];
                        return lTotalLength;
                    }

                    // �Ӷ����ļ���ȡ
                    if (bObjectFile == true)
                    {
                        Debug.Assert(string.IsNullOrEmpty(strObjectFilename) == false, "");

                        destBuffer = new Byte[lOutputLength];

                        try
                        {
                            using (FileStream s = File.Open(
            strObjectFilename,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite))
                            {
                                s.Seek(lStart, SeekOrigin.Begin);
                                s.Read(destBuffer,
                                    0,
                                    (int)lOutputLength);

                                // lTotalLength = s.Length;
                            }
                        }
                        catch (FileNotFoundException /* ex */)
                        {
                            // TODO: ��Ҫֱ�ӻ㱨�����ļ���
                            strError = "�����ļ� '" + strObjectFilename + "' ������";
                            return -100;
                        }
                        return lTotalLength;
                    }

                }

                return lTotalLength;
            }
            else if (connection.SqlServerType == SqlServerType.MySql)
            {
                // ע�� MySql ����� SQLite ����һ��
                long lTotalLength = 0;

                bool bObjectFile = false;

                if (row_info != null)
                {
                    string strRange = row_info.Range;

                    if (String.IsNullOrEmpty(strRange) == false
        && strRange[0] == '#')
                    {
                        bObjectFile = true;
                        strRange = strRange.Substring(1);

                        lTotalLength = -1;  // ��ʾ��ȡ��
                    }
                    else
                    {
                        bObjectFile = true;
                    }

                    if (StringUtil.IsInList("timestamp", strStyle) == true)
                    {
                        if (bObjectFile == true)
                            outputTimestamp = ByteArray.GetTimeStampByteArray(row_info.TimestampString);
                        else
                            outputTimestamp = ByteArray.GetTimeStampByteArray(row_info.NewTimestampString);
                    }

                    if (StringUtil.IsInList("metadata", strStyle) == true)
                        strMetadata = row_info.Metadata;
                }
                else
                {
                    // ��Ҫ��ʱ�������Ϣ
                    strID = DbPath.GetID10(strID);

                    // ���������ַ���
                    string strPartComm = "";
                    int nColIndex = 0;

                    // filename һ��Ҫ��
                    int nFileNameColIndex = -1;
                    int nNewFileNameColIndex = -1;
                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " filename, ";
                    nFileNameColIndex = nColIndex++;
                    strPartComm += " newfilename";
                    nNewFileNameColIndex = nColIndex++;

                    // 3.timestamp
                    int nTimestampColIndex = -1;
                    int nNewTimestampColIndex = -1;
                    if (StringUtil.IsInList("timestamp", strStyle) == true)
                    {
                        if (string.IsNullOrEmpty(strPartComm) == false)
                            strPartComm += ",";
                        strPartComm += " dptimestamp,";
                        nTimestampColIndex = nColIndex++;
                        strPartComm += " newdptimestamp";
                        nNewTimestampColIndex = nColIndex++;
                    }
                    // 4.metadata
                    int nMetadataColIndex = -1;
                    if (StringUtil.IsInList("metadata", strStyle) == true)
                    {
                        if (string.IsNullOrEmpty(strPartComm) == false)
                            strPartComm += ",";
                        strPartComm += " metadata";
                        nMetadataColIndex = nColIndex++;
                    }
                    // 5.range��һ��Ҫ�У������жϷ���
                    int nRangeColIndex = -1;
                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " `range`";
                    nRangeColIndex = nColIndex++;

                    int nIdColIndex = -1;
                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " id";
                    nIdColIndex = nColIndex++;

                    string strCommand = "";
                    // DataLength()����int����
                    strCommand = " SELECT "
                        + strPartComm + " "
                        + " FROM `" + this.m_strSqlDbName + "`.records WHERE id=@id";

                    using (MySqlCommand command = new MySqlCommand(strCommand,
                        connection.MySqlConnection))
                    {

                        MySqlParameter idParam =
                            command.Parameters.Add("@id",
                            MySqlDbType.String);
                        idParam.Value = strID;

                        try
                        {
                            // ִ������
                            MySqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                            try
                            {
                                if (dr == null || dr.HasRows == false)
                                {
                                    strError = "��¼ '" + strID + "' �ڿ��в�����";
                                    return -4;
                                }

                                dr.Read();

                                // 5.range��һ���᷵��
                                string strRange = "";

                                if (!dr.IsDBNull(nRangeColIndex))
                                    strRange = (string)dr[nRangeColIndex];

                                bool bReverse = false;  // �����־�����Ϊfalse����ʾ data Ϊ��ʽ���ݣ�newdataΪ��ʱ����

                                if (String.IsNullOrEmpty(strRange) == false
                && strRange[0] == '#')
                                {
                                    bObjectFile = true;
                                    strRange = strRange.Substring(1);

                                    lTotalLength = -1;  // ��ʾ��ȡ��

                                    if (row_info == null)
                                        row_info = new RecordRowInfo();
                                    // 
                                    if (nFileNameColIndex != -1 && !dr.IsDBNull(nFileNameColIndex))
                                    {
                                        row_info.FileName = (string)dr[nFileNameColIndex];
                                    }

                                    if (nNewFileNameColIndex != -1 && !dr.IsDBNull(nNewFileNameColIndex))
                                    {
                                        row_info.NewFileName = (string)dr[nNewFileNameColIndex];
                                    }
                                }

                                // 3.timestamp
                                if (StringUtil.IsInList("timestamp", strStyle) == true)
                                {
                                    if (bReverse == false || bObjectFile == true)
                                    {
                                        if (nTimestampColIndex != -1 && !dr.IsDBNull(nTimestampColIndex))
                                        {
                                            string strOutputTimestamp = (string)dr[nTimestampColIndex];
                                            outputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);//Encoding.UTF8.GetBytes(strOutputTimestamp);
                                        }
                                        else
                                            outputTimestamp = null;
                                    }
                                    else
                                    {
                                        if (nNewTimestampColIndex != -1 && !dr.IsDBNull(nNewTimestampColIndex))
                                        {
                                            string strOutputTimestamp = (string)dr[nNewTimestampColIndex];
                                            outputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);//Encoding.UTF8.GetBytes(strOutputTimestamp);
                                        }
                                        else
                                            outputTimestamp = null;
                                    }
                                }

                                // 4.metadata
                                if (StringUtil.IsInList("metadata", strStyle) == true)
                                {
                                    if (nMetadataColIndex != -1 && !dr.IsDBNull(nMetadataColIndex))
                                    {
                                        strMetadata = (string)dr[nMetadataColIndex];
                                    }
                                }
                            }
                            finally
                            {
                                dr.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            string strConnectionName = command.Connection.GetHashCode().ToString();
                            this.container.KernelApplication.WriteErrorLog("GetImage() ExecuteNonQuery exception: " + ex.Message + "; connection hashcode='" + strConnectionName + "'");
                            throw ex;
                        }
                    } // end of using command
                }

                string strObjectFilename = "";

                {
                    if (string.IsNullOrEmpty(this.m_strObjectDir) == true)
                    {
                        strError = "���ݿ���δ���ö����ļ�Ŀ¼�������ݼ�¼�г��������ö����ļ������";
                        return -1;
                    }

                    if (bTempField == false)
                    {
                        if (string.IsNullOrEmpty(row_info.FileName) == true)
                        {
                            /*
                            strError = "����Ϣ��û�ж����ļ� ��ʽ�ļ���";
                            return -1;
                             * */
                            // ��û���Ѿ���ɵĶ����ļ�
                            destBuffer = new byte[0];
                            return 0;
                        }

                        Debug.Assert(string.IsNullOrEmpty(row_info.FileName) == false, "");

                        strObjectFilename = GetObjectFileName(row_info.FileName);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(row_info.NewFileName) == true)
                        {
                            // ��û����ʱ�Ķ����ļ�
                            destBuffer = new byte[0];
                            return 0;
                        }

                        Debug.Assert(string.IsNullOrEmpty(row_info.NewFileName) == false, "");

                        strObjectFilename = GetObjectFileName(row_info.NewFileName);
                    }

                    FileInfo fi = new FileInfo(strObjectFilename);
                    if (fi.Exists == false)
                    {
                        // TODO: ��Ҫֱ�ӻ㱨�����ļ���
                        strError = "�����ļ� '" + strObjectFilename + "' ������";
                        return -100;
                    }
                    lTotalLength = fi.Length;
                }

                // ��Ҫ��ȡ����ʱ,�Ż�ȡ����
                if (StringUtil.IsInList("data", strStyle) == true)
                {
                    if (nReadLength == 0)  // ȡ0����
                    {
                        destBuffer = new byte[0];
                        return lTotalLength;    // >= 0
                    }

                    long lOutputLength = 0;
                    // �õ�ʵ�ʶ��ĳ���
                    // return:
                    //		-1  ����
                    //		0   �ɹ�
                    nRet = ConvertUtil.GetRealLength(lStart,
                        nReadLength,
                        lTotalLength,
                        nMaxLength,
                        out lOutputLength,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // 2012/1/21
                    if (lTotalLength == 0)  // �ܳ���Ϊ0
                    {
                        destBuffer = new byte[0];
                        return lTotalLength;
                    }

                    // �Ӷ����ļ���ȡ
                    if (bObjectFile == true)
                    {
                        Debug.Assert(string.IsNullOrEmpty(strObjectFilename) == false, "");

                        destBuffer = new Byte[lOutputLength];

                        try
                        {
                            using (FileStream s = File.Open(
            strObjectFilename,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite))
                            {
                                s.Seek(lStart, SeekOrigin.Begin);
                                s.Read(destBuffer,
                                    0,
                                    (int)lOutputLength);

                                // lTotalLength = s.Length;
                            }
                        }
                        catch (FileNotFoundException /* ex */)
                        {
                            // TODO: ��Ҫֱ�ӻ㱨�����ļ���
                            strError = "�����ļ� '" + strObjectFilename + "' ������";
                            return -100;
                        }
                        return lTotalLength;
                    }

                }

                return lTotalLength;
            }
            else if (connection.SqlServerType == SqlServerType.Oracle)
            {
                // ע�� Oracle ����� MySql ����һ��
                long lTotalLength = 0;

                bool bObjectFile = false;

                if (row_info != null)
                {
                    string strRange = row_info.Range;

                    if (String.IsNullOrEmpty(strRange) == false
        && strRange[0] == '#')
                    {
                        bObjectFile = true;
                        strRange = strRange.Substring(1);

                        lTotalLength = -1;  // ��ʾ��ȡ��
                    }
                    else
                    {
                        bObjectFile = true;
                    }

                    if (StringUtil.IsInList("timestamp", strStyle) == true)
                    {
                        if (bObjectFile == true)
                            outputTimestamp = ByteArray.GetTimeStampByteArray(row_info.TimestampString);
                        else
                            outputTimestamp = ByteArray.GetTimeStampByteArray(row_info.NewTimestampString);
                    }

                    if (StringUtil.IsInList("metadata", strStyle) == true)
                        strMetadata = row_info.Metadata;
                }
                else
                {
                    // ��Ҫ��ʱ�������Ϣ
                    strID = DbPath.GetID10(strID);

                    // ���������ַ���
                    string strPartComm = "";
                    int nColIndex = 0;

                    // filename һ��Ҫ��
                    int nFileNameColIndex = -1;
                    int nNewFileNameColIndex = -1;
                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " filename, ";
                    nFileNameColIndex = nColIndex++;
                    strPartComm += " newfilename";
                    nNewFileNameColIndex = nColIndex++;

                    // 3.timestamp
                    int nTimestampColIndex = -1;
                    int nNewTimestampColIndex = -1;
                    if (StringUtil.IsInList("timestamp", strStyle) == true)
                    {
                        if (string.IsNullOrEmpty(strPartComm) == false)
                            strPartComm += ",";
                        strPartComm += " dptimestamp,";
                        nTimestampColIndex = nColIndex++;
                        strPartComm += " newdptimestamp";
                        nNewTimestampColIndex = nColIndex++;
                    }
                    // 4.metadata
                    int nMetadataColIndex = -1;
                    if (StringUtil.IsInList("metadata", strStyle) == true)
                    {
                        if (string.IsNullOrEmpty(strPartComm) == false)
                            strPartComm += ",";
                        strPartComm += " metadata";
                        nMetadataColIndex = nColIndex++;
                    }
                    // 5.range��һ��Ҫ�У������жϷ���
                    int nRangeColIndex = -1;
                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " range";
                    nRangeColIndex = nColIndex++;

                    int nIdColIndex = -1;
                    if (string.IsNullOrEmpty(strPartComm) == false)
                        strPartComm += ",";
                    strPartComm += " id";
                    nIdColIndex = nColIndex++;

                    string strCommand = "";
                    // DataLength()����int����
                    strCommand = " SELECT "
                        + strPartComm + " "
                        + " FROM " + this.m_strSqlDbName + "_records WHERE id=:id";

                    using (OracleCommand command = new OracleCommand(strCommand,
                        connection.OracleConnection))
                    {

                        OracleParameter idParam =
                            command.Parameters.Add(":id",
                            OracleDbType.NVarchar2);
                        idParam.Value = strID;

                        try
                        {
                            // ִ������
                            OracleDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                            try
                            {
                                if (dr == null || dr.HasRows == false)
                                {
                                    strError = "��¼ '" + strID + "' �ڿ��в�����";
                                    return -4;
                                }

                                dr.Read();

                                // 5.range��һ���᷵��
                                string strRange = "";

                                if (!dr.IsDBNull(nRangeColIndex))
                                    strRange = (string)dr[nRangeColIndex];

                                bool bReverse = false;  // �����־�����Ϊfalse����ʾ data Ϊ��ʽ���ݣ�newdataΪ��ʱ����

                                if (String.IsNullOrEmpty(strRange) == false
                && strRange[0] == '#')
                                {
                                    bObjectFile = true;
                                    strRange = strRange.Substring(1);

                                    lTotalLength = -1;  // ��ʾ��ȡ��

                                    if (row_info == null)
                                        row_info = new RecordRowInfo();
                                    // 
                                    if (nFileNameColIndex != -1 && !dr.IsDBNull(nFileNameColIndex))
                                    {
                                        row_info.FileName = (string)dr[nFileNameColIndex];
                                    }

                                    if (nNewFileNameColIndex != -1 && !dr.IsDBNull(nNewFileNameColIndex))
                                    {
                                        row_info.NewFileName = (string)dr[nNewFileNameColIndex];
                                    }
                                }

                                // 3.timestamp
                                if (StringUtil.IsInList("timestamp", strStyle) == true)
                                {
                                    if (bReverse == false || bObjectFile == true)
                                    {
                                        if (nTimestampColIndex != -1 && !dr.IsDBNull(nTimestampColIndex))
                                        {
                                            string strOutputTimestamp = (string)dr[nTimestampColIndex];
                                            outputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);//Encoding.UTF8.GetBytes(strOutputTimestamp);
                                        }
                                        else
                                            outputTimestamp = null;
                                    }
                                    else
                                    {
                                        if (nNewTimestampColIndex != -1 && !dr.IsDBNull(nNewTimestampColIndex))
                                        {
                                            string strOutputTimestamp = (string)dr[nNewTimestampColIndex];
                                            outputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);//Encoding.UTF8.GetBytes(strOutputTimestamp);
                                        }
                                        else
                                            outputTimestamp = null;
                                    }
                                }

                                // 4.metadata
                                if (StringUtil.IsInList("metadata", strStyle) == true)
                                {
                                    if (nMetadataColIndex != -1 && !dr.IsDBNull(nMetadataColIndex))
                                    {
                                        strMetadata = (string)dr[nMetadataColIndex];
                                    }
                                }
                            }
                            finally
                            {
                                dr.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            string strConnectionName = command.Connection.GetHashCode().ToString();
                            this.container.KernelApplication.WriteErrorLog("GetImage() ExecuteNonQuery exception: " + ex.Message + "; connection hashcode='" + strConnectionName + "'");
                            throw ex;
                        }

                    } // end of using command
                }

                string strObjectFilename = "";

                {
                    if (string.IsNullOrEmpty(this.m_strObjectDir) == true)
                    {
                        strError = "���ݿ���δ���ö����ļ�Ŀ¼�������ݼ�¼�г��������ö����ļ������";
                        return -1;
                    }

                    if (bTempField == false)
                    {
                        if (string.IsNullOrEmpty(row_info.FileName) == true)
                        {
                            /*
                            strError = "����Ϣ��û�ж����ļ� ��ʽ�ļ���";
                            return -1;
                             * */
                            // ��û���Ѿ���ɵĶ����ļ�
                            destBuffer = new byte[0];
                            return 0;
                        }

                        Debug.Assert(string.IsNullOrEmpty(row_info.FileName) == false, "");

                        strObjectFilename = GetObjectFileName(row_info.FileName);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(row_info.NewFileName) == true)
                        {
                            // ��û����ʱ�Ķ����ļ�
                            destBuffer = new byte[0];
                            return 0;
                        }

                        Debug.Assert(string.IsNullOrEmpty(row_info.NewFileName) == false, "");

                        strObjectFilename = GetObjectFileName(row_info.NewFileName);
                    }

                    FileInfo fi = new FileInfo(strObjectFilename);
                    if (fi.Exists == false)
                    {
                        // TODO: ��Ҫֱ�ӻ㱨�����ļ���
                        strError = "�����ļ� '" + strObjectFilename + "' ������";
                        return -100;
                    }
                    lTotalLength = fi.Length;
                }

                // ��Ҫ��ȡ����ʱ,�Ż�ȡ����
                if (StringUtil.IsInList("data", strStyle) == true)
                {
                    if (nReadLength == 0)  // ȡ0����
                    {
                        destBuffer = new byte[0];
                        return lTotalLength;    // >= 0
                    }

                    long lOutputLength = 0;
                    // �õ�ʵ�ʶ��ĳ���
                    // return:
                    //		-1  ����
                    //		0   �ɹ�
                    nRet = ConvertUtil.GetRealLength(lStart,
                        nReadLength,
                        lTotalLength,
                        nMaxLength,
                        out lOutputLength,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // 2012/1/21
                    if (lTotalLength == 0)  // �ܳ���Ϊ0
                    {
                        destBuffer = new byte[0];
                        return lTotalLength;
                    }

                    // �Ӷ����ļ���ȡ
                    if (bObjectFile == true)
                    {
                        Debug.Assert(string.IsNullOrEmpty(strObjectFilename) == false, "");

                        destBuffer = new Byte[lOutputLength];

                        try
                        {
                            using (FileStream s = File.Open(
            strObjectFilename,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite))
                            {
                                s.Seek(lStart, SeekOrigin.Begin);
                                s.Read(destBuffer,
                                    0,
                                    (int)lOutputLength);

                                // lTotalLength = s.Length;
                            }
                        }
                        catch (FileNotFoundException /* ex */)
                        {
                            // TODO: ��Ҫֱ�ӻ㱨�����ļ���
                            strError = "�����ļ� '" + strObjectFilename + "' ������";
                            return -100;
                        }
                        return lTotalLength;
                    }

                }

                return lTotalLength;
            }
            strError = "δ֪�� connection ���� '"+connection.SqlServerType.ToString()+"'";
            return -1;
        }

#if NO
        // ��ָ����Χ����Դ
        // GetBytes()�汾�����ش�ߴ�����ʱ���ٶȷǳ���
        // parameter:
        //		strID       ��¼ID
        //		nStart      ��ʼλ��
        //		nLength     ���� -1:��ʼ������
        //		nMaxLength  ��󳤶�,��Ϊ-1ʱ,��ʾ����
        //		destBuffer  out�����������ֽ�����
        //		timestamp   out����������ʱ���
        //		strError    out���������س�����Ϣ
        // return:
        //		-1  ����
        //		-4  ��¼������
        //		>=0 ��Դ�ܳ���
        private long GetImage(SqlConnection connection,
            string strID,
            string strImageFieldName,
            long lStart,
            int nLength1,
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

            // ������Ӷ���
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            strID = DbPath.GetID10(strID);

            long lTotalLength = 0;

            List<string> cols = new List<string>();

            // data or newdata, һ��Ҫ��
            cols.Add(strImageFieldName);

            // 2.length

            // 3.timestamp
            if (StringUtil.IsInList("timestamp", strStyle) == true)
            {
                cols.Add("dptimestamp");
            }
            // 4.metadata
            if (StringUtil.IsInList("metadata", strStyle) == true)
            {
                cols.Add("metadata");
            }
            // 5.range
            if (StringUtil.IsInList("range", strStyle) == true)
            {
                cols.Add("range");
            }

            cols.Add("id");

            // ���������ַ���
            string strPartComm = StringUtil.MakePathList(cols);

            string strCommand = "";
            // DataLength()����int����
            strCommand = "use " + this.m_strSqlDbName + " "
                + " SELECT "
                + strPartComm + " "
                + " FROM records WHERE id='" + strID + "'";

            strCommand += " use master " + "\n";

            SqlCommand command = new SqlCommand(strCommand,
                connection);

            SqlDataReader reader = null;
            try
            {
                // ִ������
                reader = command.ExecuteReader(/*CommandBehavior.SingleResult | */ CommandBehavior.SequentialAccess);
                /*
    For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command. For all other types of statements, the return value is -1. If a rollback occurs, the return value is also -1.

                 * */
            }
            catch (Exception ex)
            {
                string strConnectionName = command.Connection.GetHashCode().ToString();
                this.container.KernelApplication.WriteErrorLog("GetImage() ExecuteReader exception: " + ex.Message + "; connection hashcode='" + strConnectionName + "'");
                throw ex;
            }

            try
            {
                if (reader == null || reader.HasRows == false)
                {
                    strError = "��¼'" + strID + "'�ڿ��в�����";
                    return -4;
                }

                reader.Read();


                lTotalLength = reader.GetBytes(0, 0, null, 0, 0);

                int nOutputLength = 0;
                // �õ�ʵ�ʶ��ĳ���
                // return:
                //		-1  ����
                //		0   �ɹ�
                nRet = ConvertUtil.GetRealLength(lStart,
                    nLength1,
                    lTotalLength,
                    nMaxLength,
                    out nOutputLength,
                    out strError);
                if (nRet == -1)
                    return -1;

                destBuffer = new byte[nOutputLength];

                reader.GetBytes(0,
    lStart,
    destBuffer,
    0,
    nOutputLength);

                // 3.timestamp
                if (StringUtil.IsInList("timestamp", strStyle) == true)
                {
                    string strOutputTimestamp = (string)reader["dptimestamp"];
                    outputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);//Encoding.UTF8.GetBytes(strOutputTimestamp);

                }
                // 4.metadata
                if (StringUtil.IsInList("metadata", strStyle) == true)
                {
                    strMetadata = (string)reader["metadata"];

                }
                // 5.range
                if (StringUtil.IsInList("range", strStyle) == true)
                {
                    string strRange = (string)reader["range"];
                }
            }
            catch (Exception ex)
            {
                strError = "GetImage() ReadData exception: " + ex.Message;
                return -1;
            }
            finally
            {
                reader.Close();
            }

            /*
            if (testidParam == null
                || (testidParam.Value is System.DBNull))
            {
                strError = "��¼'" + strID + "'�ڿ��в�����";
                return -4;
            }
             * */
            return lTotalLength;
        }

#endif

        // parameters:
        //      strNewXml   [in]�ֲ�XML
        //                  [out]�����õ�ȫ��XML
        int BuildRecordXml(
            string strID,
            string strXPath,
            string strOldXml,
            ref string strNewXml,
            byte [] baNewPreamble,
            out byte[] baWholeXml,
            out string strRange,
            out string strOutputValue,
            out string strError)
        {
            strError = "";
            baWholeXml = null;
            strRange = "";
            strOutputValue = "";
            int nRet = 0;

            Debug.Assert(string.IsNullOrEmpty(strXPath) == false, "");

            // �޸Ĳ���

                string strLocateXPath = "";
                string strCreatePath = "";
                string strNewRecordTemplate = "";
                string strAction = "";
                nRet = DatabaseUtil.ParseXPathParameter(strXPath,
                    out strLocateXPath,
                    out strCreatePath,
                    out strNewRecordTemplate,
                    out strAction,
                    out strError);
                if (nRet == -1)
                    return -1;

                XmlDocument tempDom = new XmlDocument();
                tempDom.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue

                try
                {
                    if (strOldXml == "")
                    {
                        if (strNewRecordTemplate == "")
                            tempDom.LoadXml("<root/>");
                        else
                            tempDom.LoadXml(strNewRecordTemplate);
                    }
                    else
                        tempDom.LoadXml(strOldXml);
                }
                catch (Exception ex)
                {
                    strError = "1 WriteXml() �ڸ�'" + this.GetCaption("zh-CN") + "'��д���¼'" + strID + "'ʱ��װ�ؾɼ�¼��dom����,ԭ��:" + ex.Message;
                    return -1;
                }


                if (strLocateXPath == "")
                {
                    strError = "xpath���ʽ�е�locate��������Ϊ��ֵ";
                    return -1;
                }

                // ͨ��strLocateXPath��λ��ָ���Ľڵ�
                XmlNode node = null;
                try
                {
                    node = tempDom.DocumentElement.SelectSingleNode(strLocateXPath);
                }
                catch (Exception ex)
                {
                    strError = "2 WriteXml() �ڸ�'" + this.GetCaption("zh-CN") + "'��д���¼'" + strID + "'ʱ��XPathʽ��'" + strXPath + "'ѡ��Ԫ��ʱ����,ԭ��:" + ex.Message;
                    return -1;
                }

                if (node == null)
                {
                    if (strCreatePath == "")
                    {
                        strError = "��'" + this.GetCaption("zh-CN") + "'��д���¼'" + strID + "'ʱ��XPathʽ��'" + strXPath + "'ָ���Ľڵ�δ�ҵ�����ʱxpath���ʽ�е�create��������Ϊ��ֵ";
                        return -1;
                    }

                    node = DomUtil.CreateNodeByPath(tempDom.DocumentElement,
                        strCreatePath);
                    if (node == null)
                    {
                        strError = "�ڲ�����!";
                        return -1;
                    }

                }

                if (node.NodeType == XmlNodeType.Attribute)
                {

                    if (strAction == "AddInteger"
                        || strAction == "+AddInteger"
                        || strAction == "AddInteger+")
                    {
                        int nNumber = 0;
                        try
                        {
                            nNumber = Convert.ToInt32(strNewXml);
                        }
                        catch (Exception ex)
                        {
                            strError = "���������'" + strNewXml + "'�������ָ�ʽ��" + ex.Message;
                            return -1;
                        }

                        string strOldValue = node.Value;
                        string strLastValue;
                        nRet = StringUtil.IncreaseNumber(strOldValue,
                            nNumber,
                            out strLastValue,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        if (strAction == "AddInteger+")
                        {
                            strOutputValue = node.Value;
                        }
                        else
                        {
                            strOutputValue = strLastValue;
                        }

                        node.Value = strLastValue;
                        //strOutputValue = node.Value;
                    }
                    else if (strAction == "AppendString")
                    {

                        node.Value = node.Value + strNewXml;
                        strOutputValue = node.Value;
                    }
                    else if (strAction == "Push")
                    {
                        string strLastValue;
                        nRet = StringUtil.GetBiggerLedNumber(node.Value,
                            strNewXml,
                            out strLastValue,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        node.Value = strLastValue;
                        strOutputValue = node.Value;
                    }
                    else
                    {
                        node.Value = strNewXml;
                        strOutputValue = node.Value;
                    }
                }
                else if (node.NodeType == XmlNodeType.Element)
                {

                    //Create a document fragment.
                    XmlDocumentFragment docFrag = tempDom.CreateDocumentFragment();

                    //Set the contents of the document fragment.
                    docFrag.InnerXml = strNewXml;

                    //Add the children of the document fragment to the
                    //original document.
                    node.ParentNode.InsertBefore(docFrag, node);

                    if (strAction == "AddInteger"
                        || strAction == "AppendString")
                    {
                        XmlNode newNode = node.PreviousSibling;
                        if (newNode == null)
                        {
                            strError = "newNode������Ϊnull";
                            return -1;
                        }

                        string strNewValue = newNode.InnerText;
                        string strOldValue = node.InnerText.Trim();  // 2012/2/16
                        if (strAction == "AddInteger")
                        {
                            int nNumber = 0;
                            try
                            {
                                nNumber = Convert.ToInt32(strNewValue);
                            }
                            catch (Exception ex)
                            {
                                strError = "���������'" + strNewValue + "'�������ָ�ʽ��" + ex.Message;
                                return -1;
                            }


                            string strLastValue;
                            nRet = StringUtil.IncreaseNumber(strOldValue,
                                nNumber,
                                out strLastValue,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            /*
                                                                    string strLastValue;
                                                                    nRet = Database.AddInteger(strNewValue,
                                                                        strOldValue,
                                                                        out strLastValue,
                                                                        out strError);
                                                                    if (nRet == -1)
                                                                        return -1;
                            */
                            newNode.InnerText = strLastValue;
                            strOutputValue = newNode.OuterXml;
                        }
                        else if (strAction == "AppendString")
                        {
                            newNode.InnerText = strOldValue + strNewValue;
                            strOutputValue = newNode.OuterXml;
                        }
                        else if (strAction == "Push")
                        {
                            string strLastValue;
                            nRet = StringUtil.GetBiggerLedNumber(strOldValue,
                                strNewValue,
                                out strLastValue,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            newNode.InnerText = strLastValue;
                            strOutputValue = newNode.OuterXml;
                        }
                    }

                    node.ParentNode.RemoveChild(node);

                }

                strNewXml = tempDom.OuterXml;

                baWholeXml =
                    DatabaseUtil.StringToByteArray(
                    strNewXml,
                    baNewPreamble);

                strRange = "0-" + Convert.ToString(baWholeXml.Length - 1);

                /*
                    lTotalLength = baRealXml.Length;

                    // return:
                    //		-1	һ���Դ���
                    //		-2	ʱ�����ƥ��
                    //		0	�ɹ�
                    nRet = this.WriteSqlRecord(connection,
                        ref row_info,
                        strID,
                        strMyRange,
                        lTotalLength,
                        baRealXml,
                        // null,
                        strMetadata,
                        strStyle,
                        outputTimestamp,   //ע�����
                        out outputTimestamp,
                        out bFull,
                        out bSingleFull,
                        out strError);
                    if (nRet <= -1)
                        return nRet;
                */
                return 0;
        }

        // ���� Keys
        // parameters:
        //      bDeleteKeys         �Ƿ�Ҫɾ��Ӧ��ɾ���� keys
        //      bDelayCreateKeys    �Ƿ��ӳٴ��� keys
        int UpdateKeysRows(
            // SessionInfo sessioninfo,
            Connection connection,
            bool bDeleteKeys,
            bool bDelayCreateKeys,
            List<WriteInfo> records,
            //out List<WriteInfo> results,
            out string strError)
        {
            strError = "";
            //results = new List<WriteInfo>();
            int nRet = 0;

            if (records == null || records.Count == 0)
                return 0;

            if (bDeleteKeys == false && bDelayCreateKeys == false)
            {
                // ���Ҫ�������� Keys�����ֲ�����ɾ�� ��ǰ�� Keys���������ظ���
                strError = "UpdateKeysRows() bDeleteKeys �� bDelayCreateKeys ����ͬʱΪ false";
                return -1;
            }

            KeyCollection total_newkeys = new KeyCollection();
            KeyCollection total_oldkeys = new KeyCollection();

            foreach (WriteInfo info in records)
            {
                KeyCollection newKeys = null;
                KeyCollection oldKeys = null;
                XmlDocument newDom = null;
                XmlDocument oldDom = null;


                string strNewXml = info.record.Xml;
                string strOldXml = "";

                if (bDelayCreateKeys == false)
                {
                    if (info.row_info != null)
                    {
                        byte[] baOldData = GetCompleteData(info.row_info);
                        if (baOldData != null && baOldData.Length > 0)
                        {
                            byte[] baPreamble = null;
                            strOldXml = DatabaseUtil.ByteArrayToString(baOldData,
                                out baPreamble);
                        }
                    }
                }
                else // bFastMode == true
                {
                    if (info.row_info != null)
                    {
                        // ����������¼��ID�������ͳһ����һ��ˢ�¼�����Ĳ���������Ͳ��ش�����������
                        if (this.RebuildIDs == null)
                        {
                            strError = "�� UpdateKeysRows() ��Ҫд�� '" + this.GetCaption("zh-CN") + "' �� ID �洢��ʱ�򣬷����� RebuildIDs Ϊ��";
                            return -1;
                        }
                        Debug.Assert(this.RebuildIDs != null, "");
                        this.RebuildIDs.Append(info.ID);
                        continue;
                    }
                }

                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = this.MergeKeys(info.ID,
                    strNewXml,
                    strOldXml,
                    true,
                    out newKeys,
                    out oldKeys,
                    out newDom,
                    out oldDom,
                    out strError);
                if (nRet == -1)
                    return -1;

                // �������ļ�
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = this.ModifyFiles(connection,
                    info.ID,
                    newDom,
                    oldDom,
                    out strError);
                if (nRet == -1)
                    return -1;

                total_newkeys.AddRange(newKeys);
                total_oldkeys.AddRange(oldKeys);
            }

            total_newkeys.Sort();
            if (total_oldkeys.Count > 1)
                total_oldkeys.Sort();

            if (bDelayCreateKeys == false)
            {
                // ��������ɾ���ʹ���

                // ���������
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = this.ModifyKeys(connection,
                    total_newkeys,
                    total_oldkeys,
                    bDelayCreateKeys,   // bFastMode ������ SQLite ������
                    out strError);
                if (nRet == -1)
                    return -1;

            }
            else
            {
                // �ӳٴ��������㡣����Ҫ�����ļ�����洢����
                // ע�⣬����ģʽ�£�������Ҫɾ���ļ����㣬������δ���д�����Ҫ�������ƴ���
                if (this.container.DelayTables == null)
                    this.container.DelayTables = new DelayTableCollection();
                nRet = this.container.DelayTables.Write(
                    this.m_strSqlDbName,
                    total_newkeys,
                    (dbname, tablename) => { return this.container.GetTempFileName(); },
                    out strError);
                if (nRet == -1)
                    return -1;

                if (bDeleteKeys == true)
                {
                    // ���������
                    // return:
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = this.ModifyKeys(connection,
                        null,
                        total_oldkeys,
                        bDelayCreateKeys,   // bFastMode ������ SQLite ������
                        out strError);
                    if (nRet == -1)
                        return -1;
                }
            }

            return 0;
        }

        // �ؽ� Keys
        // parameters:
        //      bDeleteKeys         �Ƿ��ڴ���ǰɾ����¼��ȫ�� keys
        //      bDelayCreateKeys    �Ƿ��ӳٴ��� keys
        // return:
        //      -1  ����
        //      >=0 �����ܹ������ keys ����
        int RebuildKeysRows(
            Connection connection,
            bool bDeleteKeys,
            bool bDelayCreateKeys,
            List<WriteInfo> records,
            //out List<WriteInfo> results,
            out string strError)
        {
            strError = "";
            //results = new List<WriteInfo>();
            int nRet = 0;
            // int nTotalCount = 0;

            if (bDeleteKeys == false && bDelayCreateKeys == false)
            {
                // ���Ҫ�������� Keys�����ֲ�����ɾ�� ��ǰ�� Keys���������ظ���
                strError = "RebuildKeysRows() bDeleteKeys �� bDelayCreateKeys ����ͬʱΪ false";
                return -1;
            }

            KeyCollection total_oldkeys = new KeyCollection();

            foreach (WriteInfo info in records)
            {
                KeyCollection newKeys = null;
                KeyCollection oldKeys = null;
                XmlDocument newDom = null;
                XmlDocument oldDom = null;

                string strOldXml = "";

                if (info.row_info != null)
                {
                    byte[] baOldData = GetCompleteData(info.row_info);
                    if (baOldData != null && baOldData.Length > 0)
                    {
                        byte[] baPreamble = null;
                        strOldXml = DatabaseUtil.ByteArrayToString(baOldData,
                            out baPreamble);
                    }
                }

                // TODO: �Ƿ񾯸���Щ��Ϊ��¼�ߴ�̫����޷�����������ļ�¼?


                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = this.MergeKeys(info.ID,
                    "",
                    strOldXml,
                    true,
                    out newKeys,
                    out oldKeys,
                    out newDom,
                    out oldDom,
                    out strError);
                if (nRet == -1)
                    return -1;
                total_oldkeys.AddRange(oldKeys);
            }

            if (total_oldkeys.Count > 1)
                total_oldkeys.Sort();

            if (bDeleteKeys == true)
            {
                // �ȸ��� ID �б�ɾ������ keys
                // ע�⣬��û��ȫ��Ԥ��ɾ��keys + Bulkcopy ������£�Ϊ��������ʱɾ��ÿ��XML��¼�� keys �� SQL keys ��Ӧ�þ��� B+ ����������Ȼ��ʱ��δ����д���µ� keys����ɾ����ǰ�� keys ��Ҫ B+ ���������ȵ����� BulkCopy ǰ������ר��ɾ�� B+ ��������Ȼ��� BulkCopy ���������½��� B+ ������
                nRet = this.ForceDeleteKeys(connection,
                    WriteInfo.get_ids(records),
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            if (bDelayCreateKeys == false)
            {
                // ���������
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = this.ModifyKeys(connection,
                    total_oldkeys,
                    null,
                    bDelayCreateKeys,
                    out strError);
                if (nRet == -1)
                    return -1;
                return total_oldkeys.Count;
            }
            else
            {
                // �ӳٴ��������㡣����Ҫ�����ļ�����洢����
                if (this.container.DelayTables == null)
                    this.container.DelayTables = new DelayTableCollection();
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = this.container.DelayTables.Write(
                    this.m_strSqlDbName,
                    total_oldkeys,
                    (dbname, tablename) => { return this.container.GetTempFileName(); },
                    out strError);
                if (nRet == -1)
                    return -1;
                return total_oldkeys.Count;
            }

            // return 0;
        }

        // �������߸��� SQL ��¼��
        // ���ú�.record.Metadata �� .record.Timestamp �����仯
        // �����������޸� .row_info �ĳ�Աֵ����Ϊ���洴��������Ľ׶Σ���Ҫͨ�� row_info ���ҵ��ɼ�¼����Ϣ���������и����⣬���� row_info.NewFileName �ᱻ�޸�(��Ӧ���ļ���ɾ��)�����洴���������ʱ��Ҳ�ò��������Ϣ����Ϊ NewFileName ֻ�Ǵ�����ǰû�������������ļ�
        // parameters:
        //      results [out] �����Ѿ��ɹ����»��ߴ����ļ�¼
        int UpdateRecordRows(Connection connection,
            List<WriteInfo> records,
            string strStyle,
            out List<WriteInfo> results,
            out string strError)
        {
            strError = "";
            results = new List<WriteInfo>();

            // ������Ӷ���
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            // 2013/11/23
            // �Ƿ�Ҫֱ�����������ʱ���
            bool bForceTimestamp = StringUtil.IsInList("forcesettimestamp", strStyle);

            #region MS SQL Server
            if (connection.SqlServerType == SqlServerType.MsSqlServer)
            {
                int nParameters = 0;

                using (SqlCommand command = new SqlCommand("",
                    connection.SqlConnection))
                {
                    string strCommand = "";

                    List<WriteInfo> parts = new List<WriteInfo>();
                    int i = 0;
                    foreach (WriteInfo info in records)
                    {
                        // �ύһ��
                        bool bCommit = false;
                        if (info.row_info == null && nParameters + 5 > 2100 - 1)
                            bCommit = true;
                        if (info.row_info != null && nParameters + 5 > 2100 - 1)
                            bCommit = true;

                        if (bCommit == true)
                        {
                            Debug.Assert(string.IsNullOrEmpty(strCommand) == false, "");
                            command.CommandText = "use " + this.m_strSqlDbName + "\n"
    + strCommand + "use master\n";

                            int nCount = command.ExecuteNonQuery();
                            if (nCount == 0)
                            {
                                strError = "��������� records ��ʧ��";
                                return -1;
                            }
                            strCommand = "";
                            command.Parameters.Clear();
                            nParameters = 0;

                            results.AddRange(parts);
                            parts.Clear();
                        }

                        if (info.record == null)
                        {
                            Debug.Assert(false, "");
                            strError = "info.record����Ϊ��";
                            return -1;
                        }

                        bool bObjectFile = false;
                        Debug.Assert(info.baContent != null, "");
                        if (this.m_lObjectStartSize != -1 && info.baContent.Length >= this.m_lObjectStartSize)
                            bObjectFile = true;

                        string strShortFileName = "";
                        if (bObjectFile == true)
                        {
                            // ������������һ����д������ļ�
                            nRet = WriteToObjectFile(
                                info.ID,
                                info.baContent,
                                out strShortFileName,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            Debug.Assert(string.IsNullOrEmpty(strShortFileName) == false, "");
                        }

                        // ɾ������ľ��ж����ļ�
                        if (info.row_info != null && string.IsNullOrEmpty(info.row_info.NewFileName) == false)
                        {
                            File.Delete(GetObjectFileName(info.row_info.NewFileName));
                            info.row_info.NewFileName = "";
                        }

                        // ���� metadata
                        string strResultMetadata = "";
                        // return:
                        //		-1	����
                        //		0	�ɹ�
                        nRet = DatabaseUtil.MergeMetadata(info.row_info != null ? info.row_info.Metadata : "",
                            info.record.Metadata,
                            info.baContent.Length,
                            out strResultMetadata,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        info.record.Metadata = strResultMetadata;


                        // ���� timestamp
                        string strOutputTimestamp = "";
                        if (bForceTimestamp == true)
                            strOutputTimestamp = ByteArray.GetHexTimeStampString(info.record.Timestamp);
                        else
                            strOutputTimestamp = this.CreateTimestampForDb();

                        info.record.Timestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);

                        if (info.row_info == null)
                        {
                            // ��������
                            if (bObjectFile == false)
                            {
                                strCommand +=
            " INSERT INTO records(id, data, range, metadata, dptimestamp) "
            + " VALUES(@id" + i + ", @data" + i + ", @range" + i + ", @metadata" + i + ", @dptimestamp" + i + ")"
            + "\n";
                            }
                            else
                            {
                                strCommand +=
    " INSERT INTO records(id, data, range, metadata, dptimestamp,  filename) "
    + " VALUES(@id" + i + ", NULL, @range" + i + ", @metadata" + i + ", @dptimestamp" + i + ", @filename"+i+")"
    + "\n";
                            }

                            SqlParameter idParam =
command.Parameters.Add("@id" + i,
SqlDbType.NVarChar);
                            idParam.Value = info.ID;

                            if (bObjectFile == false)
                            {
                                SqlParameter dataParam =
                                    command.Parameters.Add("@data" + i,
                                    SqlDbType.Binary,
                                    info.baContent.Length);
                                dataParam.Value = info.baContent;   // ?? �Ƿ������?
                            }

                            SqlParameter rangeParam =
    command.Parameters.Add("@range" + i,
    SqlDbType.NVarChar,
    4000);
                            if (bObjectFile == true)
                                rangeParam.Value = "#";
                            else
                                rangeParam.Value = "";

                            SqlParameter metadataParam =
                                command.Parameters.Add("@metadata" + i,
                                SqlDbType.NVarChar);
                            metadataParam.Value = info.record.Metadata;

                            SqlParameter dptimestampParam =
                                command.Parameters.Add("@dptimestamp" + i,
                                SqlDbType.NVarChar,
                                100);
                            dptimestampParam.Value = strOutputTimestamp;

                            if (bObjectFile == true)
                            {
                                SqlParameter filenameParam =
                        command.Parameters.Add("@filename" + i,
                        SqlDbType.NVarChar,
                        255);
                                filenameParam.Value = strShortFileName;
                            }

                            nParameters += 5;
                            parts.Add(info);
                        }
                        else
                        {
                            // �������е���

                            // TODO: ���ڱ��ο���һ�����ú� data�����Բ��ؿ��Ƿ������⣬�����һ����������ȥ���ú���

#if NO
                            bool bReverse = false;  // �����־�����Ϊfalse����ʾ data Ϊ��ʽ���ݣ�newdataΪ��ʱ����
                            if (String.IsNullOrEmpty(info.row_info.Range) == false
                                && info.row_info.Range[0] == '!')
                                bReverse = true;
#endif

                            if (bObjectFile == false)
                            {
                                strCommand += " UPDATE records "
                                + " SET dptimestamp=@dptimestamp" + i + ","
                                + " newdptimestamp=NULL,"
                                + " data=@data"+i+", newdata=NULL,"
                                + " range=@range" + i + ","
                                + " filename=NULL, newfilename=NULL,"
                                + " metadata=@metadata" + i + " "
                                + " WHERE id=@id" + i + " \n";
                            }
                            else
                            {
                                strCommand += " UPDATE records "
                                + " SET dptimestamp=@dptimestamp" + i + ","
                                + " newdptimestamp=NULL,"
                                + " data=NULL, newdata=NULL,"
                                + " range=@range" + i + ","
                                + " filename=@filename"+i+", newfilename=NULL,"
                                + " metadata=@metadata" + i + " "
                                + " WHERE id=@id" + i + " \n";
                            }

                            string strCurrentRange = "";

                            SqlParameter idParam = command.Parameters.Add("@id" + i,
        SqlDbType.NVarChar);
                            idParam.Value = info.ID;

                            if (bObjectFile == false)
                            {
                                SqlParameter dataParam =
                                    command.Parameters.Add("@data" + i,
                                    SqlDbType.Binary,
                                    info.baContent.Length);
                                dataParam.Value = info.baContent;   // ?? �Ƿ������?
                            }

                            SqlParameter dptimestampParam =
                                command.Parameters.Add("@dptimestamp" + i,
                                SqlDbType.NVarChar,
                                100);
                            dptimestampParam.Value = strOutputTimestamp;

                            SqlParameter rangeParam =
                                command.Parameters.Add("@range" + i,
                                SqlDbType.NVarChar,
                                4000);
                            if (bObjectFile == true)
                                rangeParam.Value = "#" + strCurrentRange;
                            else
                            {
                                rangeParam.Value = strCurrentRange;
                            }

                            // info.row_info.Range = (string)rangeParam.Value;  // ����ת�����ʱ����


                            SqlParameter metadataParam =
                                command.Parameters.Add("@metadata" + i,
                                SqlDbType.NVarChar,
                                4000);
                            metadataParam.Value = info.record.Metadata;

                            if (bObjectFile == true)
                            {
                                SqlParameter filenameParam =
                        command.Parameters.Add("@filename" + i,
                        SqlDbType.NVarChar,
                        255);
                                filenameParam.Value = strShortFileName;
                                // info.row_info.FileName = strShortFileName;
                            }

                            if (bObjectFile == true)
                                nParameters += 5;
                            else
                                nParameters += 5;
                            parts.Add(info);
                        }

                        i++;
                    }

                    if (string.IsNullOrEmpty(strCommand) == false)
                    {
                        command.CommandText = "use " + this.m_strSqlDbName + "\n" 
                            + strCommand + "use master\n";

                        int nCount = command.ExecuteNonQuery();
                        if (nCount == 0)
                        {
                            strError = "��������� records ��ʧ��";
                            return -1;
                        }
                        strCommand = "";
                        results.AddRange(parts);
                        parts.Clear();
                    }
                } // end of using command
            }
            #endregion // MS SQL Server

            #region SQLite
            if (connection.SqlServerType == SqlServerType.SQLite)
            {
                bool bFastMode = false;
                using (SQLiteCommand command = new SQLiteCommand("",
                    connection.SQLiteConnection))
                {
                    IDbTransaction trans = null;

                    if (bFastMode == false)
                        trans = connection.SQLiteConnection.BeginTransaction();
                    try
                    {
                        string strCommand = "";

                        List<WriteInfo> parts = new List<WriteInfo>();
                        int i = 0;
                        foreach (WriteInfo info in records)
                        {
                            if (info.record == null)
                            {
                                Debug.Assert(false, "");
                                strError = "info.record����Ϊ��";
                                return -1;
                            }

                            string strShortFileName = "";
                            // ������������һ����д������ļ�
                            nRet = WriteToObjectFile(
                                info.ID,
                                info.baContent,
                                out strShortFileName,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            Debug.Assert(string.IsNullOrEmpty(strShortFileName) == false, "");

                            // ɾ������ľ��ж����ļ�
                            if (info.row_info != null && string.IsNullOrEmpty(info.row_info.NewFileName) == false)
                            {
                                File.Delete(GetObjectFileName(info.row_info.NewFileName));
                                info.row_info.NewFileName = "";
                            }

                            // ���� metadata
                            string strResultMetadata = "";
                            // return:
                            //		-1	����
                            //		0	�ɹ�
                            nRet = DatabaseUtil.MergeMetadata(info.row_info != null ? info.row_info.Metadata : "",
                                info.record.Metadata,
                                info.baContent.Length,
                                out strResultMetadata,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            info.record.Metadata = strResultMetadata;

                            // ���� timestamp
                            string strOutputTimestamp = "";
                            if (bForceTimestamp == true)
                                strOutputTimestamp = ByteArray.GetHexTimeStampString(info.record.Timestamp);
                            else
                                strOutputTimestamp = this.CreateTimestampForDb();

                            info.record.Timestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);

                            if (info.row_info == null)
                            {
                                // ��������
                                strCommand +=
    " INSERT INTO records(id, range, metadata, dptimestamp, filename) "
    + " VALUES(@id" + i + ", @range" + i + ", @metadata" + i + ", @dptimestamp" + i + ", @filename" + i + ")"
    + " ; ";

                                SQLiteParameter idParam =
command.Parameters.Add("@id" + i,
DbType.String);
                                idParam.Value = info.ID;


                                SQLiteParameter rangeParam =
        command.Parameters.Add("@range" + i,
        DbType.String);
                                rangeParam.Value = "#";

                                SQLiteParameter metadataParam =
                                command.Parameters.Add("@metadata" + i,
                                DbType.String);
                                metadataParam.Value = info.record.Metadata;

                                SQLiteParameter dptimestampParam =
                                    command.Parameters.Add("@dptimestamp" + i,
                                    DbType.String);
                                dptimestampParam.Value = strOutputTimestamp;


                                SQLiteParameter filenameParam =
                            command.Parameters.Add("@filename" + i,
                            DbType.String);
                                filenameParam.Value = strShortFileName;


                                parts.Add(info);
                            }
                            else
                            {
                                // �������е���
                                strCommand += " UPDATE records "
                                + " SET dptimestamp=@dptimestamp" + i + ","
                                + " newdptimestamp=NULL,"
                                + " range=@range" + i + ","
                                + " filename=@filename" + i + ", newfilename=NULL,"
                                + " metadata=@metadata" + i + " "
                                + " WHERE id=@id" + i + " ; ";

                                string strCurrentRange = "";

                                SQLiteParameter idParam = command.Parameters.Add("@id" + i,
            DbType.String);
                                idParam.Value = info.ID;

                                SQLiteParameter dptimestampParam =
                                    command.Parameters.Add("@dptimestamp" + i,
                                    DbType.String);
                                dptimestampParam.Value = strOutputTimestamp;

                                SQLiteParameter rangeParam =
                                    command.Parameters.Add("@range" + i,
                                    DbType.String);
                                rangeParam.Value = "#" + strCurrentRange;


                                SQLiteParameter metadataParam =
                                command.Parameters.Add("@metadata" + i,
                                DbType.String);
                                metadataParam.Value = info.record.Metadata;

                                SQLiteParameter filenameParam =
                            command.Parameters.Add("@filename" + i,
                            DbType.String);
                                filenameParam.Value = strShortFileName;


                                parts.Add(info);
                            }

                            {
                                // �ύһ��
                                Debug.Assert(string.IsNullOrEmpty(strCommand) == false, "");
                                command.CommandText = strCommand;

                                int nCount = command.ExecuteNonQuery();
                                if (nCount == 0)
                                {
                                    strError = "��������� records ��ʧ��";
                                    return -1;
                                }
                                strCommand = "";
                                command.Parameters.Clear();
                                results.AddRange(parts);
                                parts.Clear();
                            }

                            i++;
                        }
                        if (trans != null)
                        {
                            trans.Commit();
                            trans = null;
                        }
                    }
                    finally
                    {
                        if (trans != null)
                            trans.Rollback();
                    }
                } // end of using command
            }
            #endregion // SQLite

            #region MySql
            if (connection.SqlServerType == SqlServerType.MySql)
            {
                int nParameters = 0;

                using (MySqlCommand command = new MySqlCommand("",
                    connection.MySqlConnection))
                {
                    MySqlTransaction trans = null;

                    trans = connection.MySqlConnection.BeginTransaction();
                    try
                    {
                        string strCommand = "";

                        List<WriteInfo> parts = new List<WriteInfo>();
                        int i = 0;
                        foreach (WriteInfo info in records)
                        {
                            // �ύһ��
                            bool bCommit = false;
                            if (info.row_info == null && nParameters + 5 > 2100 - 1)
                                bCommit = true;
                            if (info.row_info != null && nParameters + 5 > 2100 - 1)
                                bCommit = true;

                            if (bCommit == true)
                            {
                                Debug.Assert(string.IsNullOrEmpty(strCommand) == false, "");
                                command.CommandText = "use " + this.m_strSqlDbName + " ;\n"
        + strCommand;

                                int nCount = command.ExecuteNonQuery();
                                if (nCount == 0)
                                {
                                    strError = "��������� records ��ʧ��";
                                    return -1;
                                }
                                strCommand = "";
                                command.Parameters.Clear();
                                nParameters = 0;

                                results.AddRange(parts);
                                parts.Clear();
                            }

                            if (info.record == null)
                            {
                                Debug.Assert(false, "");
                                strError = "info.record����Ϊ��";
                                return -1;
                            }

                            string strShortFileName = "";
                            // ������������һ����д������ļ�
                            nRet = WriteToObjectFile(
                                info.ID,
                                info.baContent,
                                out strShortFileName,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            Debug.Assert(string.IsNullOrEmpty(strShortFileName) == false, "");

                            // ɾ������ľ��ж����ļ�
                            if (info.row_info != null && string.IsNullOrEmpty(info.row_info.NewFileName) == false)
                            {
                                File.Delete(GetObjectFileName(info.row_info.NewFileName));
                                info.row_info.NewFileName = "";
                            }

                            // ���� metadata
                            string strResultMetadata = "";
                            // return:
                            //		-1	����
                            //		0	�ɹ�
                            nRet = DatabaseUtil.MergeMetadata(info.row_info != null ? info.row_info.Metadata : "",
                                info.record.Metadata,
                                info.baContent.Length,
                                out strResultMetadata,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            info.record.Metadata = strResultMetadata;

                            // ���� timestamp
                            string strOutputTimestamp = "";
                            if (bForceTimestamp == true)
                                strOutputTimestamp = ByteArray.GetHexTimeStampString(info.record.Timestamp);
                            else
                                strOutputTimestamp = this.CreateTimestampForDb();

                            info.record.Timestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);

                            if (info.row_info == null)
                            {
                                // ��������
                                strCommand +=
    " INSERT INTO `" + this.m_strSqlDbName + "`.records (id, `range`, metadata, dptimestamp, filename) "
    + " VALUES (@id" + i + ", @range" + i + ", @metadata" + i + ", @dptimestamp" + i + ", @filename" + i + ")"
    + " ;\n";


                                MySqlParameter idParam =
command.Parameters.Add("@id" + i,
MySqlDbType.String);
                                idParam.Value = info.ID;


                                MySqlParameter rangeParam =
        command.Parameters.Add("@range" + i,
        MySqlDbType.String);
                                rangeParam.Value = "#";

                                MySqlParameter metadataParam =
                                command.Parameters.Add("@metadata" + i,
                                MySqlDbType.String);
                                metadataParam.Value = info.record.Metadata;

                                MySqlParameter dptimestampParam =
                                    command.Parameters.Add("@dptimestamp" + i,
                                    MySqlDbType.String);
                                dptimestampParam.Value = strOutputTimestamp;


                                MySqlParameter filenameParam =
                            command.Parameters.Add("@filename" + i,
                            MySqlDbType.String);
                                filenameParam.Value = strShortFileName;

                                nParameters += 5; 
                                parts.Add(info);
                            }
                            else
                            {
                                // �������е���
                                strCommand += " UPDATE `" + this.m_strSqlDbName + "`.records "
                                + " SET dptimestamp=@dptimestamp" + i + ","
                                + " newdptimestamp=NULL,"
                                + " `range`=@range" + i + ","
                                + " filename=@filename" + i + ", newfilename=NULL,"
                                + " metadata=@metadata" + i + " "
                                + " WHERE id=@id" + i + " ; ";

                                string strCurrentRange = "";

                                MySqlParameter idParam = 
                                    command.Parameters.Add("@id" + i,
                                    MySqlDbType.String);
                                idParam.Value = info.ID;

                                MySqlParameter dptimestampParam =
                                    command.Parameters.Add("@dptimestamp" + i,
                                    MySqlDbType.String);
                                dptimestampParam.Value = strOutputTimestamp;

                                MySqlParameter rangeParam =
                                    command.Parameters.Add("@range" + i,
                                    MySqlDbType.String);
                                rangeParam.Value = "#" + strCurrentRange;

                                MySqlParameter metadataParam =
                                    command.Parameters.Add("@metadata" + i,
                                    MySqlDbType.String);
                                metadataParam.Value = info.record.Metadata;

                                MySqlParameter filenameParam =
                                    command.Parameters.Add("@filename" + i,
                                    MySqlDbType.String);
                                filenameParam.Value = strShortFileName;

                                nParameters += 5; 
                                parts.Add(info);
                            }

                            i++;
                        }

                        // ����ύһ��
                        if (string.IsNullOrEmpty(strCommand) == false)
                        {
                            Debug.Assert(string.IsNullOrEmpty(strCommand) == false, "");
                            command.CommandText = "use " + this.m_strSqlDbName + " ;\n"
    + strCommand;

                            int nCount = command.ExecuteNonQuery();
                            if (nCount == 0)
                            {
                                strError = "��������� records ��ʧ��";
                                return -1;
                            }
                            strCommand = "";
                            command.Parameters.Clear();
                            nParameters = 0;

                            results.AddRange(parts);
                            parts.Clear();
                        }

                        if (trans != null)
                        {
                            trans.Commit();
                            trans = null;
                        }
                    }
                    finally
                    {
                        if (trans != null)
                            trans.Rollback();
                    }
                } // end of using command
            }
            #endregion // MySql

            #region Oracle
            if (connection.SqlServerType == SqlServerType.Oracle)
            {
                using (OracleCommand command = new OracleCommand("", connection.OracleConnection))
                {
                    command.BindByName = true;

                    IDbTransaction trans = null;

                    trans = connection.OracleConnection.BeginTransaction();
                    try
                    {
                        string strCommand = "";

                        List<WriteInfo> parts = new List<WriteInfo>();
                        int i = 0;
                        foreach (WriteInfo info in records)
                        {
                            if (info.record == null)
                            {
                                Debug.Assert(false, "");
                                strError = "info.record����Ϊ��";
                                return -1;
                            }


                            string strShortFileName = "";
                            // ������������һ����д������ļ�
                            nRet = WriteToObjectFile(
                                info.ID,
                                info.baContent,
                                out strShortFileName,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            Debug.Assert(string.IsNullOrEmpty(strShortFileName) == false, "");

                            // ɾ������ľ��ж����ļ�
                            if (info.row_info != null && string.IsNullOrEmpty(info.row_info.NewFileName) == false)
                            {
                                File.Delete(GetObjectFileName(info.row_info.NewFileName));
                                info.row_info.NewFileName = "";
                            }

                            // ���� metadata
                            string strResultMetadata = "";
                            // return:
                            //		-1	����
                            //		0	�ɹ�
                            nRet = DatabaseUtil.MergeMetadata(info.row_info != null ? info.row_info.Metadata : "",
                                info.record.Metadata,
                                info.baContent.Length,
                                out strResultMetadata,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            info.record.Metadata = strResultMetadata;

                            // ���� timestamp
                            string strOutputTimestamp = "";
                            if (bForceTimestamp == true)
                                strOutputTimestamp = ByteArray.GetHexTimeStampString(info.record.Timestamp);
                            else
                                strOutputTimestamp = this.CreateTimestampForDb();

                            info.record.Timestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);

                            if (info.row_info == null)
                            {
                                // ��������
                                strCommand +=
    " INSERT INTO " + this.m_strSqlDbName + "_records (id, range, metadata, dptimestamp, filename) "
    + " VALUES(:id" + i + ", :range" + i + ", :metadata" + i + ", :dptimestamp" + i + ", :filename" + i + ")"
    + " ";

                                OracleParameter idParam =
command.Parameters.Add(":id" + i,
OracleDbType.NVarchar2);
                                idParam.Value = info.ID;


                                OracleParameter rangeParam =
        command.Parameters.Add(":range" + i,
        OracleDbType.NVarchar2);
                                rangeParam.Value = "#";

                                OracleParameter metadataParam =
                                command.Parameters.Add(":metadata" + i,
                                OracleDbType.NVarchar2);
                                metadataParam.Value = info.record.Metadata;

                                OracleParameter dptimestampParam =
                                    command.Parameters.Add(":dptimestamp" + i,
                                    OracleDbType.NVarchar2);
                                dptimestampParam.Value = strOutputTimestamp;


                                OracleParameter filenameParam =
                            command.Parameters.Add(":filename" + i,
                            OracleDbType.NVarchar2);
                                filenameParam.Value = strShortFileName;


                                parts.Add(info);
                            }
                            else
                            {
                                // �������е���
                                strCommand += " UPDATE " + this.m_strSqlDbName + "_records "
                                + " SET dptimestamp=:dptimestamp" + i + ","
                                + " newdptimestamp=NULL,"
                                + " range=:range" + i + ","
                                + " filename=:filename" + i + ", newfilename=NULL,"
                                + " metadata=:metadata" + i + " "
                                + " WHERE id=:id" + i + " ";

                                string strCurrentRange = "";

                                OracleParameter idParam = 
                                    command.Parameters.Add(":id" + i,
                                    OracleDbType.NVarchar2);
                                idParam.Value = info.ID;

                                OracleParameter dptimestampParam =
                                    command.Parameters.Add(":dptimestamp" + i,
                                    OracleDbType.NVarchar2);
                                dptimestampParam.Value = strOutputTimestamp;

                                OracleParameter rangeParam =
                                    command.Parameters.Add(":range" + i,
                                    OracleDbType.NVarchar2);
                                rangeParam.Value = "#" + strCurrentRange;


                                OracleParameter metadataParam =
                                command.Parameters.Add(":metadata" + i,
                                OracleDbType.NVarchar2);
                                metadataParam.Value = info.record.Metadata;

                                OracleParameter filenameParam =
                            command.Parameters.Add(":filename" + i,
                            OracleDbType.NVarchar2);
                                filenameParam.Value = strShortFileName;


                                parts.Add(info);
                            }

                            {
                                // �ύһ��
                                Debug.Assert(string.IsNullOrEmpty(strCommand) == false, "");
                                command.CommandText = strCommand;

                                int nCount = command.ExecuteNonQuery();
                                if (nCount == 0)
                                {
                                    strError = "��������� records ��ʧ��";
                                    return -1;
                                }
                                strCommand = "";
                                command.Parameters.Clear();

                                results.AddRange(parts);
                                parts.Clear();
                            }

                            i++;
                        }
                        if (trans != null)
                        {
                            trans.Commit();
                            trans = null;
                        }
                    }
                    finally
                    {
                        if (trans != null)
                            trans.Rollback();
                    }
                } // end of using command
            }
            #endregion // Oracle

            return 0;
        }

        // ������������һ����д������ļ�
        int WriteToObjectFile(
            string strID,
            byte[] baContent,
            out string strShortFileName,
            // ref RecordRowInfo row_info,
            out string strError)
        {
            strError = "";
            strShortFileName = "";

            if (string.IsNullOrEmpty(this.m_strObjectDir) == true)
            {
                strError = "���ݿ���δ���ö����ļ�Ŀ¼����д�����ʱ��������Ҫ���ö����ļ������";
                return -1;
            }

            string strFileName = "";

            strFileName = BuildObjectFileName(strID, false);
            strShortFileName = GetShortFileName(strFileName); // ����
            if (strShortFileName == null)
            {
                strError = "������ļ���ʱ������¼ID '" + strID + "', �����ļ�Ŀ¼ '" + this.m_strObjectDir + "', �����ļ��� '" + strFileName + "'";
                return -1;
            }

            int nRedoCount = 0;
        REDO:
            try
            {
                using (FileStream s = File.Open(
strFileName,
FileMode.OpenOrCreate,
FileAccess.Write,
FileShare.ReadWrite))
                {
                    // ��һ��д�ļ�,�����ļ����ȴ��ڶ����ܳ��ȣ���ض��ļ�
                    if (s.Length > baContent.Length)
                        s.SetLength(0);

                    s.Seek(0, SeekOrigin.Begin);
                    s.Write(baContent,
                        0,
                        baContent.Length);
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                if (nRedoCount == 0)
                {
                    // �����м���Ŀ¼
                    PathUtil.CreateDirIfNeed(PathUtil.PathPart(strFileName));
                    nRedoCount++;
                    goto REDO;
                }
                throw ex;
            }
            catch (Exception ex)
            {
                strError = "д���ļ� '" + strFileName + "' ʱ��������: " + ex.Message;
                return -1;
            }
            return 0;
        }

        // ��� records���� ����Ѵ��ڵ�����Ϣ
        // parameters:
        //      bGetData    �Ƿ���Ҫ��ü�¼��?
        private int GetRowInfos(Connection connection,
            bool bGetData,
            List<string> ids,
            out List<RecordRowInfo> row_infos,
            out string strError)
        {
            strError = "";
            row_infos = new List<RecordRowInfo>();

            if (ids.Count == 0)
                return 0;

            // ������Ӷ���
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

#if NO
            StringBuilder idstring = new StringBuilder(4096);
            int i = 0;
            foreach (string s in ids)
            {
                if (StringUtil.IsPureNumber(s) == false)
                {
                    strError = "ID '" + s + "' �����Ǵ�����";
                    return -1;
                }
                if (i != 0)
                    idstring.Append(",");
                idstring.Append("'" + s + "'");
                i++;
            }
#endif
            string strIdString = "";
            nRet = BuildIdString(ids, out strIdString, out strError);
            if (nRet == -1)
                return -1;

            #region MS SQL Server
            if (connection.SqlServerType == SqlServerType.MsSqlServer)
            {
                // TODO: �ɷ��޶�����һ���ߴ�����ݿ�Ͳ�Ҫ����? 
                // ��ʵ�ƺ�û�������Ҫ����Ϊ����Ҫ���أ�Ҳ�� SqlReader GetBytes() ��ʱ�����ʱȥ SQL Server ȡ�İ�
                string strSelect = " SELECT TEXTPTR(data)," // 0
                    + " DataLength(data),"  // 1
                    + " TEXTPTR(newdata),"  // 2
                    + " DataLength(newdata),"   // 3
                    + " range,"             // 4
                    + " dptimestamp,"       // 5
                    + " metadata, "         // 6
                    + " newdptimestamp,"    // 7
                    + " filename,"          // 8
                    + " newfilename,"        // 9
                    + " id"                 // 10
                    + (bGetData == true ?
                    ", data,"                 // 11
                    + " newdata"            // 12
                    : "")
                    + " FROM records "
                    + " WHERE id in (" + strIdString + ")\n";

                string strCommand = "use " + this.m_strSqlDbName + " \n"
                    + strSelect
                    + " use master " + "\n";

                using (SqlCommand command = new SqlCommand(strCommand,
                    connection.SqlConnection))
                {

                    SqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        // һ����¼Ҳ������
                        if (dr == null
                            || dr.HasRows == false)
                            return 0;

                        while (dr.Read())
                        {
                            RecordRowInfo row_info = new RecordRowInfo();

                            if (dr.IsDBNull(0) == false)
                                row_info.data_textptr = (byte[])dr[0];

                            if (dr.IsDBNull(1) == false)
                                row_info.data_length = dr.GetInt32(1);

                            if (dr.IsDBNull(2) == false)
                                row_info.newdata_textptr = (byte[])dr[2];

                            if (dr.IsDBNull(3) == false)
                                row_info.newdata_length = dr.GetInt32(3);

                            if (dr.IsDBNull(4) == false)
                                row_info.Range = dr.GetString(4);

                            if (dr.IsDBNull(5) == false)
                                row_info.TimestampString = dr.GetString(5);
                            // TODO: ������ȱȽ�ʱ��������緢����ʱ�����һ�µ�������Ϳ��Ա�������ȡ data bytes �Ķ��ද���ˡ������� delegate ʵ�ֲ�ѯĳ�� ID ��Ӧ���ύ������ʱ���

                            if (dr.IsDBNull(6) == false)
                                row_info.Metadata = dr.GetString(6);

                            if (dr.IsDBNull(7) == false)
                                row_info.NewTimestampString = dr.GetString(7);

                            if (dr.IsDBNull(8) == false)
                                row_info.FileName = dr.GetString(8);

                            if (dr.IsDBNull(9) == false)
                                row_info.NewFileName = dr.GetString(9);

                            if (dr.IsDBNull(10) == false)
                                row_info.ID = dr.GetString(10);

                            if (bGetData == true)
                            {
                                // �����ļ�
                                if (String.IsNullOrEmpty(row_info.Range) == false
&& row_info.Range[0] == '#')
                                {
                                    nRet = ReadObjectFileContent(row_info,
            out strError);
                                    if (nRet == -1)
                                        return -1;  // TODO: �Ƿ�����������ݣ����ͳһ������߱���?
                                    goto CONTINUE;
                                }

                                // �Ƿ���Ը��ݷ����־����ȡ����˵� data? �������Ա����˷���Դ
                                if (dr.IsDBNull(11) == false && row_info.data_length <= 1024 * 1024)
                                {
                                    row_info.Data = new byte[row_info.data_length];
                                    dr.GetBytes(11, 0, row_info.Data, 0, (int)row_info.data_length);
                                }

                                if (dr.IsDBNull(12) == false && row_info.newdata_length <= 1024 * 1024)
                                {
                                    row_info.NewData = new byte[row_info.newdata_length];
                                    dr.GetBytes(12, 0, row_info.NewData, 0, (int)row_info.newdata_length);
                                }
                            }
                        CONTINUE:
                            row_infos.Add(row_info);
                        }
                    }
                    finally
                    {
                        dr.Close();
                    }
                } // end of using command

                return 0;
            }
            #endregion // MS SQL Server

            #region SQLite
            else if (connection.SqlServerType == SqlServerType.SQLite)
            {
                string strCommand = " SELECT "
                    + " range," // 0 
                    + " dptimestamp,"   // 1
                    + " metadata, "  // 2
                    + " newdptimestamp,"   // 3
                    + " filename,"   // 4
                    + " newfilename,"   // 5
                    + " id"             // 6
                    + " FROM records "
                    + " WHERE id in (" + strIdString + ")\n";

                using (SQLiteCommand command = new SQLiteCommand(strCommand,
                    connection.SQLiteConnection))
                {

                    SQLiteDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        // �����¼������
                        if (dr == null
                            || dr.HasRows == false)
                            return 0;

                        // �����¼�Ѿ�����
                        while (dr.Read())
                        {
                            RecordRowInfo row_info = new RecordRowInfo();

                            if (dr.IsDBNull(0) == false)
                                row_info.Range = dr.GetString(0);

                            if (dr.IsDBNull(1) == false)
                                row_info.TimestampString = dr.GetString(1);

                            if (dr.IsDBNull(2) == false)
                                row_info.Metadata = dr.GetString(2);

                            if (dr.IsDBNull(3) == false)
                                row_info.NewTimestampString = dr.GetString(3);

                            if (dr.IsDBNull(4) == false)
                                row_info.FileName = dr.GetString(4);

                            if (dr.IsDBNull(5) == false)
                                row_info.NewFileName = dr.GetString(5);

                            if (dr.IsDBNull(6) == false)
                                row_info.ID = dr.GetString(6);

                            if (bGetData == true)
                            {
                                nRet = ReadObjectFileContent(row_info,
out strError);
                                if (nRet == -1)
                                    return -1;  // TODO: �Ƿ�����������ݣ����ͳһ������߱���?
                            }

                            row_infos.Add(row_info);
                        }
                    }
                    finally
                    {
                        dr.Close();
                    }
                } // end of using command

                return 0;
            }
            #endregion // SQLite

            #region MySql
            else if (connection.SqlServerType == SqlServerType.MySql)
            {
                // ע�� MySql ����� SQLite ����һ��
                string strCommand = " SELECT "
                    + " `range`," // 0 
                    + " dptimestamp,"   // 1
                    + " metadata, "  // 2
                    + " newdptimestamp,"   // 3
                    + " filename,"   // 4
                    + " newfilename,"   // 5
                    + " id"             // 6
                    + " FROM `" + this.m_strSqlDbName + "`.records "
                    + " WHERE id in (" + strIdString + ") \n";

                using (MySqlCommand command = new MySqlCommand(strCommand,
                    connection.MySqlConnection))
                {

                    MySqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        // �����¼�����ڣ���Ҫ����
                        if (dr == null
                            || dr.HasRows == false)
                            return 0;

                        // �����¼�Ѿ�����
                        while (dr.Read())
                        {
                            RecordRowInfo row_info = new RecordRowInfo();

                            if (dr.IsDBNull(0) == false)
                                row_info.Range = dr.GetString(0);

                            if (dr.IsDBNull(1) == false)
                                row_info.TimestampString = dr.GetString(1);

                            if (dr.IsDBNull(2) == false)
                                row_info.Metadata = dr.GetString(2);

                            if (dr.IsDBNull(3) == false)
                                row_info.NewTimestampString = dr.GetString(3);

                            if (dr.IsDBNull(4) == false)
                                row_info.FileName = dr.GetString(4);

                            if (dr.IsDBNull(5) == false)
                                row_info.NewFileName = dr.GetString(5);

                            if (dr.IsDBNull(6) == false)
                                row_info.ID = dr.GetString(6);

                            if (bGetData == true)
                            {
                                nRet = ReadObjectFileContent(row_info,
    out strError);
                                if (nRet == -1)
                                    return -1;  // TODO: �Ƿ�����������ݣ����ͳһ������߱���?
                            }
                            row_infos.Add(row_info);
                        }
                    }
                    finally
                    {
                        dr.Close();
                    }
                } // end of using command

                return 0;
            }
            #endregion // MySql

            #region Oracle
            else if (connection.SqlServerType == SqlServerType.Oracle)
            {
                string strCommand = " SELECT "
                    + " range," // 0
                    + " dptimestamp,"   // 1
                    + " metadata, "  // 2
                    + " newdptimestamp,"   // 3
                    + " filename,"   // 4
                    + " newfilename,"   // 5
                    + " id"             // 6
                    + " FROM " + this.m_strSqlDbName + "_records "
                    + " WHERE id in (" + strIdString + ") \n";

                using (OracleCommand command = new OracleCommand(strCommand,
                    connection.OracleConnection))
                {

                    OracleDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        // �����¼������
                        if (dr == null
                            || dr.HasRows == false)
                            return 0;

                        // �����¼�Ѿ�����
                        while (dr.Read())
                        {
                            RecordRowInfo row_info = new RecordRowInfo();

                            if (dr.IsDBNull(0) == false)
                                row_info.Range = dr.GetString(0);

                            if (dr.IsDBNull(1) == false)
                                row_info.TimestampString = dr.GetString(1);

                            if (dr.IsDBNull(2) == false)
                                row_info.Metadata = dr.GetString(2);

                            if (dr.IsDBNull(3) == false)
                                row_info.NewTimestampString = dr.GetString(3);

                            if (dr.IsDBNull(4) == false)
                                row_info.FileName = dr.GetString(4);

                            if (dr.IsDBNull(5) == false)
                                row_info.NewFileName = dr.GetString(5);

                            if (dr.IsDBNull(6) == false)
                                row_info.ID = dr.GetString(6);

                            if (bGetData == true)
                            {
                                nRet = ReadObjectFileContent(row_info,
    out strError);
                                if (nRet == -1)
                                    return -1;  // TODO: �Ƿ�����������ݣ����ͳһ������߱���?
                            }

                            row_infos.Add(row_info);
                        }
                    }
                    finally
                    {
                        if (dr != null)
                            dr.Close();
                    }
                } // end of using command

                return 0;
            }
            #endregion // Oracle

            return 0;
        }

        public int ReadObjectFileContent(RecordRowInfo row_info,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(row_info.FileName) == true)
                row_info.Data = new byte[0];
            else
            {
                string strObjectFilename = GetObjectFileName(row_info.FileName);
                try
                {
                    row_info.Data = null;
                    using (FileStream s = File.Open(
    strObjectFilename,
    FileMode.Open,
    FileAccess.Read,
    FileShare.ReadWrite))
                    {
                        if (s.Length > 1024 * 1024)
                        {
                            return 0;   // �ļ��ߴ�̫�󣬲����ʷŵ� byte [] ��
                        }
                        row_info.Data = new byte[s.Length];
                        s.Read(row_info.Data,
                            0,
                            (int)s.Length);
                    }
                }
                catch (FileNotFoundException /* ex */)
                {
                    // TODO: ��Ҫֱ�ӻ㱨�����ļ���
                    strError = "�����ļ� '" + strObjectFilename + "' ������";
                    return -1;
                }
                catch (Exception ex)
                {
                    strError = "��ȡ�����ļ� '" + strObjectFilename + "' ʱ��������: " + ex.Message;
                    return -1;
                }
            }
            return 0;
        }

        // �� Session �кͱ����ݿ��йصĻ������ݳ���д�� SQL Server
        // parameters:
        //      strAction   "getdelaysize"  ������Ҫд�����Ϣ����
        //                  ""  ���� BulkCopy
        public override long BulkCopy(
            // SessionInfo sessioninfo,
            string strAction,
            out string strError)
        {
            strError = "";

            if (this.container.SqlServerType != SqlServerType.MsSqlServer
                && this.container.SqlServerType != SqlServerType.Oracle
                && this.container.SqlServerType != SqlServerType.MySql
                && this.container.SqlServerType != SqlServerType.SQLite)
            {
                strError = "BulkCopy() ��֧�� "+this.container.SqlServerType.ToString()+" ���͵����ݿ�";
                return -1;
            }

            if (this.container.DelayTables == null || this.container.DelayTables.Count == 0)
                return 0;

            List<DelayTable> tables = this.container.DelayTables.GetTables(this.m_strSqlDbName);
            if (tables.Count == 0)
                return 0;

            if (strAction == "getdelaysize")
            {
                long lSize = 0;
                foreach (DelayTable table in tables)
                {
                    lSize += table.Size;
                }

                return lSize;
            }

            bool bFastMode = false;

            // ������û�м�����ʱ�����
            if (this.container.SqlServerType == SqlServerType.SQLite)
                this.Commit();

            //*********�����ݿ�Ӷ���*************
            m_db_lock.AcquireReaderLock(m_nTimeOut);
            try
            {
                Connection connection = GetConnection(
    this.m_strConnString,
    this.container.SqlServerType == SqlServerType.SQLite && bFastMode == true ? ConnectionStyle.Global : ConnectionStyle.None);
                connection.Open();
                try
                {
                    #region MS SQL Server
                    if (this.container.SqlServerType == SqlServerType.MsSqlServer)
                    {
                        Stopwatch watch = new Stopwatch();
                        watch.Start();
                        foreach (DelayTable table in tables)
                        {
                            var bulkCopy = new SqlBulkCopy(connection.SqlConnection);
                            bulkCopy.DestinationTableName = this.m_strSqlDbName + ".." + table.TableName;
                            int nRet = table.OpenForRead(table.FileName, out strError);
                            if (nRet == -1)
                                return -1;
                            table.LockForRead();    // ������������������� Read() ��������Ͳ���Ҫ������
                            try
                            {
                                bulkCopy.WriteToServer(table);
                            }
                            finally
                            {
                                table.UnlockForRead();
                            }
                            table.Free();
                            this.container.DelayTables.Remove(table);
                        }
                        watch.Stop();
                        this.container.KernelApplication.WriteErrorLog("MS SQL Server BulkCopy ��ʱ " + watch.Elapsed.ToString());
                    }
                    #endregion // MS SQL Server

                    #region Oracle
                    //strError = "�ݲ�֧��";
                    //return -1;
                    if (this.container.SqlServerType == SqlServerType.Oracle)
                    {
                        Stopwatch watch = new Stopwatch();
                        watch.Start();
                        foreach (DelayTable table in tables)
                        {
                            // http://stackoverflow.com/questions/26941161/oraclebulkcopy-class-in-oracle-manageddataaccess-dll
                            // OracleBulkCopy Class in Oracle.ManagedDataAccess.dll?
                            var bulkCopy = new OracleBulkCopy(connection.OracleConnection);
                            bulkCopy.BatchSize = 5000;  // default is zero , whole in one batch
                            bulkCopy.BulkCopyTimeout = 20 * 60; // default is 30 deconds
                            bulkCopy.DestinationTableName = this.m_strSqlDbName + "_" + table.TableName;   // this.m_strSqlDbName + ".." + table.TableName;
                            int nRet = table.OpenForRead(table.FileName, out strError);
                            if (nRet == -1)
                                return -1;
                            table.LockForRead();    // ������������������� Read() ��������Ͳ���Ҫ������
                            try
                            {
                                bulkCopy.WriteToServer(table);
                            }
                            finally
                            {
                                table.UnlockForRead();
                            }
                            table.Free();
                            this.container.DelayTables.Remove(table);
                        }
                        watch.Stop();
                        this.container.KernelApplication.WriteErrorLog("Oracle BulkCopy ��ʱ " + watch.Elapsed.ToString());
                    }
                    #endregion // Oracle

                    #region MySql
                    if (this.container.SqlServerType == SqlServerType.MySql)
                    {
                        Stopwatch watch = new Stopwatch();
                        watch.Start();
                        foreach (DelayTable table in tables)
                        {
                            var bulkCopy = new MySqlBulkCopy(connection.MySqlConnection);
                            bulkCopy.BatchSize = 5000;
                            bulkCopy.DestinationTableName = "`" + this.m_strSqlDbName + "`." + table.TableName;
                            int nRet = table.OpenForRead(table.FileName, out strError);
                            if (nRet == -1)
                                return -1;
                            table.LockForRead();    // ������������������� Read() ��������Ͳ���Ҫ������
                            try
                            {
                                bulkCopy.WriteToServer(table);
                            }
                            finally
                            {
                                table.UnlockForRead();
                            }
                            table.Free();
                            this.container.DelayTables.Remove(table);
                        }
                        watch.Stop();
                        this.container.KernelApplication.WriteErrorLog("MySql BulkCopy ��ʱ " + watch.Elapsed.ToString());
                    }
                    #endregion // MySql


                    #region SQLite
                    if (this.container.SqlServerType == SqlServerType.SQLite)
                    {
                        Stopwatch watch = new Stopwatch();
                        watch.Start();
                        // this.CommitInternal(false);
                        foreach (DelayTable table in tables)
                        {
                            var bulkCopy = new SqliteBulkCopy(connection.SQLiteConnection);
                            bulkCopy.BatchSize = 5000;
                            bulkCopy.DestinationTableName = table.TableName;
                            int nRet = table.OpenForRead(table.FileName, out strError);
                            if (nRet == -1)
                                return -1;
                            table.LockForRead();    // ������������������� Read() ��������Ͳ���Ҫ������
                            try
                            {
                                bulkCopy.WriteToServer(table);
                            }
                            finally
                            {
                                table.UnlockForRead();
                            }
                            table.Free();
                            this.container.DelayTables.Remove(table);
                        }
                        // this.CommitInternal(false);
                        // connection.Commit(false);
                        watch.Stop();
                        this.container.KernelApplication.WriteErrorLog("SQLite BulkCopy ��ʱ " + watch.Elapsed.ToString());
                    }
                    #endregion // SQLite

                }
                catch (SqlException sqlEx)
                {
                    strError = "3 BulkCopy() �ڸ�'" + this.GetCaption("zh-CN") + "'��д���¼ʱ����,ԭ��:" + GetSqlErrors(sqlEx);
                    return -1;
                }
                catch (Exception ex)
                {
                    strError = "4 BulkCopy() �ڸ�'" + this.GetCaption("zh-CN") + "'��д���¼ʱ����,ԭ��:" + ex.Message;
                    return -1;
                }
                finally
                {
                    connection.Close();
                }
            }
            finally
            {
                //********�����ݿ�����****************
                m_db_lock.ReleaseReaderLock();
            }

            return 0;
        }

        // д��һ�� XML ��¼������ˢ��һ����¼�ļ�����
        // return:
        //      -1  ����ע�⣬����û�з��� -1���� outputs ������Ҳ�п�����Ԫ�ؾ��з��صĴ�����Ϣ
        //      >=0 ����� rebuildkeys���򷵻��ܹ������ keys ����
        public override int WriteRecords(
            // SessionInfo sessioninfo,
            User oUser,
            List<RecordBody> inputs,
            string strStyle,
            out List<RecordBody> outputs,
            out string strError)
        {
            strError = "";
            outputs = new List<RecordBody>();

            if (StringUtil.IsInList("fastmode", strStyle) == true)
                this.FastMode = true;
            bool bFastMode = StringUtil.IsInList("fastmode", strStyle) || this.FastMode;

            bool bRebuildKeys = StringUtil.IsInList("rebuildkeys", strStyle);
            bool bDeleteKeys = StringUtil.IsInList("deletekeys", strStyle);

            // ע�� rebuildkeys��ʾ����������Ҫ�������ؽ������㡣
            // ����� deletekeys ����ʹ�ã����ʾ��ÿ����¼�ؽ��ĵ�ʱ����ɾ�����Ѿ����ڵľ��м�����
            // ��������� deletekeys����ôӦ����������������ǰ��ȫ��ɾ�� keys �����ݣ�ʹ�ñ����ؽ�����������в����ٿ�������ɾ�����м������ˣ�ֻ��Ҫ��������
            // ������� fastmode ʹ�ã���ʾ��Ҫ������ keys �ӳٴ�������� Bulkcopy ���� keys ��
            // ��������� fastmode������û��Ԥ��ɾ��ȫ�� keys����Ӧ������ keys ��� B+ ����������������������ɾ������ keys ��ÿ��С��������� Bulkcopy ǰ����Ҫע��ɾ�� B+ ����������ɺ����´���

            List<RecordBody> error_records = new List<RecordBody>();

            List<WriteInfo> records = new List<WriteInfo>();

            foreach (RecordBody record in inputs)
            {
                string strPath = record.Path;   // �������ݿ�����·��

                string strDbName = StringUtil.GetFirstPartPath(ref strPath);
                if (strDbName == ".")
                    strDbName = this.GetCaption("zh-CN");

                bool bObject = false;
                string strRecordID = "";
                string strObjectID = "";
                string strXPath = "";

                string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                //***********�Ե���2��*************
                // ����Ϊֹ��strPath������¼�Ų��ˣ��¼�������ж�
                strRecordID = strFirstPart;
                // ֻ����¼�Ų��·��
                if (strPath == "")
                {
                    bObject = false;
                }
                else
                {
                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    //***********�Ե���2��*************
                    // ����Ϊֹ��strPath����object��xpath�� strFirstPart������object �� xpath

                    if (strFirstPart != "object"
        && strFirstPart != "xpath")
                    {
                        record.Result.SetValue("��Դ·�� '" + record.Path + "' ���Ϸ�, ��3�������� 'object' �� 'xpath' ",
                            ErrorCodeValue.PathError); // -7;
                        continue;
                    }
                    if (string.IsNullOrEmpty(strPath) == true)  //object��xpath�¼�������ֵ
                    {
                        record.Result.SetValue("��Դ·�� '" + record.Path + "' ���Ϸ�,����3���� 'object' �� 'xpath' ʱ����4������������",
                            ErrorCodeValue.PathError); // -7;
                        continue;
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
                }

                if (bObject == true)
                {
                    record.Result.SetValue("Ŀǰ�������� WriteRecords д�������Դ",
                        ErrorCodeValue.CommonError);
                    continue;
                }

                if (strRecordID == "?")
                    strRecordID = "-1";

                if (bRebuildKeys == true && strRecordID == "-1")
                {
                    record.Result.SetValue("�������ò�ȷ���ļ�¼ID���ؽ������� (��¼·��Ϊ '" + record.Path + "')",
                        ErrorCodeValue.CommonError);
                    continue;
                }


                bool bPushTailNo = false;
                // �� �� ����β��¼��
                strRecordID = this.EnsureID(strRecordID,
                    out bPushTailNo);  //�Ӻ�д��

                // bPushed == true ˵��û�б�Ҫ select ��ȡԭ�� records ��

                if (oUser != null)
                {
                    string strTempRecordPath = this.GetCaption("zh-CN") + "/" + strRecordID;
                    if (bPushTailNo == true)
                    {
                        string strExistRights = "";
                        bool bHasRight = oUser.HasRights(strTempRecordPath,
                            ResType.Record,
                            "create",//"append",
                            out strExistRights);
                        if (bHasRight == false)
                        {
                            strError = "�����ʻ���Ϊ'" + oUser.Name + "'����'" + strTempRecordPath + "'��¼û��'����(create)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                            record.Result.SetValue(strError,
                                ErrorCodeValue.NotHasEnoughRights);    // return -6;
                            error_records.Add(record);
                            continue;
                        }
                    }
                    else
                    {
                        string strExistRights = "";
                        bool bHasRight = oUser.HasRights(strTempRecordPath,
                            ResType.Record,
                            "overwrite",
                            out strExistRights);
                        if (bHasRight == false)
                        {
                            strError = "�����ʻ���Ϊ'" + oUser.Name + "'����'" + strTempRecordPath + "'��¼û��'����(overwrite)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                            record.Result.SetValue(
                                strError,
                                ErrorCodeValue.NotHasEnoughRights,   // return -6;
                                -1);
                            error_records.Add(record);
                            continue;
                        }
                    }
                }

                // TODO: rebuild keys ��ҪʲôȨ�� ?

                WriteInfo write_info = new WriteInfo();
                write_info.record = record;
                write_info.ID = strRecordID;
                write_info.Pushed = bPushTailNo;
                write_info.XPath = strXPath;
                if (string.IsNullOrEmpty(record.Xml) == false)
                {
                    byte[] baContent = Encoding.UTF8.GetBytes(record.Xml);
                    write_info.baContent = baContent;
                    string strRange = "0-" + (baContent.Length - 1).ToString();
                    write_info.strRange = strRange;
                }
                records.Add(write_info);
            }

            bool bIgnoreCheckTimestamp = StringUtil.IsInList("ignorechecktimestamp", strStyle);

            //*********�����ݿ�Ӷ���*************
            m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
			this.container.WriteDebugInfo("WriteRecords()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            try
            {
                List<string> locked_ids = WriteInfo.get_ids(records, true);
                //**********�Լ�¼��д��***************
                this.m_recordLockColl.LockForWrite(ref locked_ids, m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("WriteRecords()����'" + this.GetCaption("zh-CN") + "/" + strID + "'��¼��д����");
#endif
                try // ��¼��
                {

                    Connection connection = GetConnection(
        this.m_strConnString,
        this.container.SqlServerType == SqlServerType.SQLite && bFastMode == true ? ConnectionStyle.Global : ConnectionStyle.None);
                    connection.Open();
                    try
                    {
                        // select �Ѿ����ڵ�����Ϣ
                        List<RecordRowInfo> row_infos = null;
                        // ��ö���Ѵ��ڵ�����Ϣ
                        int nRet = GetRowInfos(connection,
                            bRebuildKeys ? true : !bFastMode,
                            WriteInfo.get_ids(records),    // ���� get_existing_ids ��׷�� 40 ������Ŀ���ݲżӿ��ٶ�1���Ӷ���
                            out row_infos,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        // �� row_infos �е�ֵ ƥ�� id �ŵ� infos������row_info��
                        WriteInfo.set_rowinfos(ref records, row_infos);

                        // ���ؽ�������
                        if (bRebuildKeys == true)
                        {
                            if (bFastMode == false && bDeleteKeys == false)
                            {
                                strError = "WriteRecords() ִ�� rebuildkeys ����ʱ�� ��� style �в����� fastmode���������� deletekeys";
                                return -1;
                            }
                            //List<WriteInfo> temp = null;
                            // ���� Keys
                            nRet = RebuildKeysRows(
                                connection,
                                bDeleteKeys,
                                bFastMode,
                                records,
                                // out temp,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            return nRet;
                        }

                        // �Դ��Ժ� .row_info Ϊ�յľ���Ҫ�´�������

                        // ���ʱ���
                        if (bIgnoreCheckTimestamp == false)
                        {
                            for (int i = 0; i < records.Count; i++)
                            {
                                WriteInfo info = records[i];
                                Debug.Assert(string.IsNullOrEmpty(info.ID) == false, "");

                                if (info.row_info == null)
                                    continue;

                                byte[] baExistTimestamp = ByteArray.GetTimeStampByteArray(GetCompleteTimestamp(info.row_info));
                                if (ByteArray.Compare(info.record.Timestamp,
                                    baExistTimestamp) != 0)
                                {
                                    info.record.Timestamp = baExistTimestamp;   // ���ظ�ǰ�ˣ���ǰ���ܹ���֪��ǰ��ʱ���
                                    info.record.Result.Value = -1;
                                    info.record.Result.ErrorString = "ʱ�����ƥ��";
                                    info.record.Result.ErrorCode = ErrorCodeValue.TimestampMismatch; //   return -2;

                                    error_records.Add(info.record);
                                    records.RemoveAt(i);
                                    i--;
                                    continue;
                                }
                            }

                        }

                        List<WriteInfo> results = null;
                        if (records.Count > 0)
                        {
                            // �������߸��� SQL ��¼��
                            nRet = UpdateRecordRows(connection,
                                records,
                                strStyle,
                                out results,
                                out strError);
                            foreach (WriteInfo info in results)
                            {
                                string strPath = info.record.Path;   // �������ݿ�����·��
                                string strDbName = StringUtil.GetFirstPartPath(ref strPath);
                                if (strDbName == ".")
                                    strDbName = this.GetCaption("zh-CN");
                                string strRecordID = DbPath.GetCompressedID(info.ID);
                                if (string.IsNullOrEmpty(info.XPath) == true)
                                    info.record.Path = strDbName + "/" + strRecordID;
                                else
                                    info.record.Path = strDbName + "/" + strRecordID + "/xpath/" + info.XPath;

                                outputs.Add(info.record);
                            }
                        }

                        // records��(����ʱ)û�����ü�������Ĳ��־Ͳ�������
                        outputs.AddRange(error_records);
                        if (nRet == -1)
                            return -1;

                        if (results != null && results.Count > 0)
                        {
                            //List<WriteInfo> temp = null;
                            // ���� Keys
                            nRet = UpdateKeysRows(
                                // sessioninfo,
                                connection,
                                true,   // ʼ��Ҫ����ɾ���ɵ� keys
                                bFastMode,
                                results,
                                //out temp,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        strError = "3 WriteRecords() �ڸ�'" + this.GetCaption("zh-CN") + "'��д���¼ '" + StringUtil.MakePathList(WriteInfo.get_ids(records)) + "' ʱ����,ԭ��:" + GetSqlErrors(sqlEx);
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        strError = "4 WriteRecords() �ڸ�'" + this.GetCaption("zh-CN") + "'��д���¼ '" + StringUtil.MakePathList(WriteInfo.get_ids(records)) + "' ʱ����,ԭ��:" + ex.Message;
                        return -1;
                    }
                    finally
                    {
                        connection.Close();
                    }

                }
                finally  // ��¼��
                {
                    //******�Լ�¼��д��****************************
                    m_recordLockColl.UnlockForWrite(locked_ids);
#if DEBUG_LOCK_SQLDATABASE
					this.container.WriteDebugInfo("WriteRecords()����'" + this.GetCaption("zh-CN") + "/" + strID + "'��¼��д����");
#endif
                }

            }
            finally
            {
                //********�����ݿ�����****************
                m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("WriteRecords()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }
            return 0;
        }

        class WriteInfo
        {
            public string ID = "";
            public bool Pushed = false; // ����д��� ID �����Ƿ��ƶ�����ǰβ�� ? ����ƶ���(Pushed == true)��˵��û�б�Ҫ select ��� records ���е������У�Ҳ����˵ records ���в�������
            public string XPath = "";
            public RecordRowInfo row_info = null;
            public RecordBody record = null;
            public byte[] baContent = null;
            public string strRange = "";

            public static List<string> get_ids(List<WriteInfo> infos,
                bool bEnsure10 = false)
            {
                List<string> results = new List<string>();
                foreach (WriteInfo info in infos)
                {
                    if (bEnsure10 == true)
                        results.Add(DbPath.GetID10(info.ID));
                    else
                        results.Add(info.ID);
                }

                return results;
            }

            // ������Щ���ܴ��� records �е� id �ţ����� select ǰ׼�� id �Ĺ���
            public static List<string> get_existing_ids(List<WriteInfo> infos)
            {
                List<string> results = new List<string>();
                foreach (WriteInfo info in infos)
                {
                    if (info.Pushed == false)
                        results.Add(info.ID);
                }

                return results;
            }

            // �� row_infos �е�ֵ ƥ�� id �ŵ� infos ������ row_info ��
            public static void set_rowinfos(ref List<WriteInfo> infos,
                List<RecordRowInfo> row_infos)
            {
                if (row_infos == null || row_infos.Count == 0)
                    return;

                Hashtable id_table = new Hashtable();   // id --> RecordRowInfo

                foreach (RecordRowInfo row_info in row_infos)
                {
                    id_table[row_info.ID] = row_info;
                }

                foreach (WriteInfo info in infos)
                {
                    info.row_info = (RecordRowInfo)id_table[info.ID];
                }
            }


        }



        // �ϴ�����д��ʱ��ʱ���
        static string GetCompleteTimestamp(RecordRowInfo row_info)
        {
            string strCurrentRange = row_info.Range;

            if (String.IsNullOrEmpty(strCurrentRange) == false
    && strCurrentRange[0] == '!')
            {
                return row_info.NewTimestampString; // ���ε�ʱ���
            }
            else
            {
                return row_info.TimestampString; // �ϴ�����д��ʱ��ʱ���
            }
        }

        // �ϴ�����д��ʱ�ļ�¼��
        static byte [] GetCompleteData(RecordRowInfo row_info)
        {
            string strCurrentRange = row_info.Range;

            if (String.IsNullOrEmpty(strCurrentRange) == false
    && strCurrentRange[0] == '!')
                return row_info.NewData;
            else
                return row_info.Data;
        }

        // дxml����
        // parameter:
        //		strID           ��¼ID -1:��ʾ׷��һ����¼
        //		strRanges       Ŀ���λ��,���range�ö��ŷָ�
        //		nTotalLength    �ܳ���
        //		inputTimestamp  �����ʱ���
        //		outputTimestamp ���ص�ʱ���
        //		strOutputID     ���صļ�¼ID,��strID == -1ʱ,�õ�ʵ�ʵ�ID
        //		strError        
        // return:
        //		-1  ����
        //		-2  ʱ�����ƥ��
        //      -4  ��¼������
        //      -6  Ȩ�޲���
        //		0   �ɹ�
        public override int WriteXml(User oUser,  //null���򲻼���Ȩ��
            string strID,
            string strXPath,
            string strRanges,
            long lTotalLength,
            byte[] baSource,
            // Stream streamSource,
            string strMetadata,
            string strStyle,
            byte[] inputTimestamp,
            out byte[] outputTimestamp,
            out string strOutputID,
            out string strOutputValue,   //��AddInteger �� AppendStringʱ ����ֵ����ֵ
            bool bCheckAccount,
            out string strError)
        {
            strOutputValue = "";
            outputTimestamp = null;
            strOutputID = "";
            strError = "";

            if (StringUtil.IsInList("fastmode", strStyle) == true)
                this.FastMode = true;

            bool bFastMode = StringUtil.IsInList("fastmode", strStyle) || this.FastMode;

            if (strID == "?")
                strID = "-1";

            bool bPushTailNo = false;
            strID = this.EnsureID(strID,
                out bPushTailNo);  //�Ӻ�д��
            if (oUser != null)
            {
                string strTempRecordPath = this.GetCaption("zh-CN") + "/" + strID;
                if (bPushTailNo == true)
                {
                    string strExistRights = "";
                    bool bHasRight = oUser.HasRights(strTempRecordPath,
                        ResType.Record,
                        "create",//"append",
                        out strExistRights);
                    if (bHasRight == false)
                    {
                        strError = "�����ʻ���Ϊ'" + oUser.Name + "'����'" + strTempRecordPath + "'��¼û��'����(create)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                        return -6;
                    }
                }
                else
                {
                    string strExistRights = "";
                    bool bHasRight = oUser.HasRights(strTempRecordPath,
                        ResType.Record,
                        "overwrite",
                        out strExistRights);
                    if (bHasRight == false)
                    {
                        strError = "�����ʻ���Ϊ'" + oUser.Name + "'����'" + strTempRecordPath + "'��¼û��'����(overwrite)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                        return -6;
                    }
                }
            }

            strOutputID = DbPath.GetCompressedID(strID);
            int nRet = 0;

            bool bFull = false;
            bool bSingleFull = false;

            string strDbType = "";

            // ���ﲻ����ΪFastMode��д��

            //*********�����ݿ�Ӷ���*************
            m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
			this.container.WriteDebugInfo("WriteXml()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif

            try
            {
                strDbType = this.GetDbType();

                strID = DbPath.GetID10(strID);
                //**********�Լ�¼��д��***************
                this.m_recordLockColl.LockForWrite(strID, m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("WriteXml()����'" + this.GetCaption("zh-CN") + "/" + strID + "'��¼��д����");
#endif
                try // ��¼��
                {
                    Connection connection = GetConnection(
                        this.m_strConnString,
                        this.container.SqlServerType == SqlServerType.SQLite && bFastMode == true ? ConnectionStyle.Global : ConnectionStyle.None);
                    connection.Open();
                    try
                    {
#if NO
                            // 1.�����¼������,����һ���ֽڵļ�¼,��ȷ���õ�textPtr
                            // return:
                            //		-1  ����
                            //      0   ������
                            //      1   ����
                            nRet = this.RecordIsExist(connection,
                                strID,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            bool bExist = false;
                            if (nRet == 1)
                                bExist = true;

                            // �¼�¼ʱ������һ���ֽڣ���������ʱ���
                            if (bExist == false)
                            {
                                byte[] tempInputTimestamp = inputTimestamp;
                                // ע���¼�¼��ʱ���,��inputTimestamp����
                                nRet = this.InsertRecord(connection,
                                    strID,
                                    out inputTimestamp,//tempTimestamp,//
                                    out strError);

                                if (nRet == -1)
                                    return -1;
                            }
#endif
                        // ����������¼�¼����������Ϣ
                        RecordRowInfo row_info = null;
                        // return:
                        //		-1  ����
                        //      0   û�д����¼�¼
                        //      1   �������µļ�¼(Ҳ����ζ��ԭ�ȼ�¼��������)
                        //      2   ��Ҫ�����µļ�¼������Ϊ�Ż���Ե��(�Ժ���Ҫ����)��û�д���
                        nRet = this.CreateNewRecordIfNeed(connection,
                            strID,
                            null,
                            out row_info,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        bool bExist = false;
                        if (nRet == 0)
                            bExist = true;

                        bool bNeedInsertRow = false;
                        if (nRet == 2)
                            bNeedInsertRow = true;

#if NO
                            byte[] baOldPreamble = new byte[0];
                            string strOldXml = "";
                            // ��֧
                            if (string.IsNullOrEmpty(strXPath) == false)
                            {
                                if (bExist == true)
                                {
                                    // return:
                                    //      -1  ����
                                    //      -4  ��¼������
        //      -100    �����ļ�������
                                    //      0   ��ȷ
                                    nRet = this.GetXmlData(
                                        connection,
                                        row_info,
                                        strID,
                                        false,   // "data",
                                        out strOldXml,
                                        out baOldPreamble,
                                        out strError);
                                    if (nRet <= -1 && nRet != -3)
                                        return nRet;
                                }

                                byte[] baPreamble = null;
                                string strNewPartXml = DatabaseUtil.ByteArrayToString(baSource,
out baPreamble);

                                // ���ݲ������ṩ�ľֲ����ݴ����������ļ�¼
                                nRet = BuildRecordXml(
                                    strID,
                                    strXPath,
                                    strOldXml,
                                    strNewPartXml,
                                    baPreamble,
                                    out baSource,
                                    out strRanges,
                                    out strOutputValue,
                                    out strError);
                                if (nRet == -1)
                                    return -1;

                                lTotalLength = baSource.Length;
                            }
#endif

                        bool bForceDeleteKeys = false;  // �Ƿ�Ҫǿ��ɾ���Ѿ����ڵ�keys

                        string strOldXml = "";
                        byte[] baOldPreamble = new byte[0];

                        string strExistingRanges = GetPureRangeString(row_info.Range);

                        // �жϱ���������ɺ��Ƿ񸲸�ȫ��Χ
                        bFull = IsFull(
        strExistingRanges,
        lTotalLength,
        strRanges,
        baSource.Length);
                        // ���Ԥ�Ƶ�����д�������temp������ɣ�����ǰȡ���Ѿ����ڵ�xml�ַ���
                        if (bFull == true)
                        {
                            // ����Ѿ����ڵļ�¼��XML�ַ���
                            if (string.IsNullOrEmpty(strOldXml) == true
                                && bExist == true)
                            {
                                // return:
                                //      -1  ����
                                //      -4  ��¼������
                                //      -100    �����ļ�������
                                //      0   ��ȷ
                                nRet = this.GetXmlData(
                                    connection,
                                    row_info,
                                    strID,
                                    false,
                                    out strOldXml,
                                    out baOldPreamble,
                                    out strError);
                                if (nRet == -100)
                                {
                                    // Ҫд��ʱ�����ּ��������ǵ�λ�ö����ļ�������
                                    strOldXml = "";
                                    baOldPreamble = new byte[0];
                                    bForceDeleteKeys = true;
                                }
                                else
                                {
                                    if (nRet <= -1 && nRet != -3)   // ?? -3��ʲô���
                                        return nRet;
                                }
                            }
                        }

                        int nWriteCount = 0;
                        if (string.IsNullOrEmpty(strXPath) == false
                            && IsSingleFull(strRanges, baSource, lTotalLength) == true)
                        {
                            // ���һ���Ծͷ�����ȫ������, ����Ϊxpath��ʽд�룬��ô
                            // ��Ҫʡȥ���д���¼�Ĳ������ں���ֱ��д�뼴��
                            Debug.Assert(bFull == true, "");
                            bFull = true;
                            bSingleFull = true;
                        }
                        else
                        {
                            // д����
                            // return:
                            //		-1	һ���Դ���
                            //		-2	ʱ�����ƥ��
                            //		0	�ɹ�
                            nRet = this.WriteSqlRecord(connection,
                                ref row_info,
                                bNeedInsertRow,
                                strID,
                                strRanges,
                                lTotalLength,
                                baSource,
                                // streamSource,
                                strMetadata,
                                strStyle,
                                inputTimestamp,
                                out outputTimestamp,
                                out bFull,
                                out bSingleFull,
                                out strError);
                            if (nRet <= -1)
                                return nRet;

                            nWriteCount++;
                        }

                        // ��鷶Χ
                        //string strCurrentRange = this.GetRange(connection,
                        //	strID);
                        if (bFull == true)  //��������
                        {
                            // 1.�õ��¾ɼ�����
                            byte[] baNewPreamble = new byte[0];
                            string strNewXml = "";

                            if (bSingleFull == true)
                            {
                                // �Ż������ش����ݿ��ж�ȡ��
                                byte[] baPreamble = null;
                                strNewXml = DatabaseUtil.ByteArrayToString(baSource,
out baPreamble);
                            }
                            else
                            {
                                // return:
                                //      -1  ����
                                //      -4  ��¼������
                                //      -100    �����ļ�������
                                //      0   ��ȷ
                                nRet = this.GetXmlData(
                                    connection,
                                    row_info,
                                    strID,
                                    nWriteCount == 0 ? true : !true,  // "newdata",   // WriteSqlRecord()���Ѿ��ߵ�������
                                    out strNewXml,
                                    out baNewPreamble,
                                    out strError);
                                if (nRet == -1)
                                    return -1;
                            }

                            ////
                            ////
                            if (string.IsNullOrEmpty(strXPath) == false)
                            {
                                // ���ݲ������ṩ�ľֲ����ݴ����������ļ�¼
                                nRet = BuildRecordXml(
                                    strID,
                                    strXPath,
                                    strOldXml,
                                    ref strNewXml,
                                    baNewPreamble,
                                    out baSource,
                                    out strRanges,
                                    out strOutputValue,
                                    out strError);
                                if (nRet == -1)
                                    return -1;

                                lTotalLength = baSource.Length;

                                // д����
                                // return:
                                //		-1	һ���Դ���
                                //		-2	ʱ�����ƥ��
                                //		0	�ɹ�
                                nRet = this.WriteSqlRecord(connection,
                                    ref row_info,
                                    bNeedInsertRow,
                                    strID,
                                    strRanges,
                                    lTotalLength,
                                    baSource,
                                    // streamSource,
                                    strMetadata,
                                    strStyle,
                                    inputTimestamp,
                                    out outputTimestamp,
                                    out bFull,
                                    out bSingleFull,
                                    out strError);
                                if (nRet <= -1)
                                    return nRet;

                                nWriteCount++;
                                // ע�����д����������ǵڶ���д������range�ڵı�ǻ��ٴη�ת
                                // �����������ģ��Ӧ���ܹ��Զ���Ӧ
                            }

                            KeyCollection newKeys = null;
                            KeyCollection oldKeys = null;
                            XmlDocument newDom = null;
                            XmlDocument oldDom = null;

                            // return:
                            //      -1  ����
                            //      0   �ɹ�
                            nRet = this.MergeKeys(strID,
                                strNewXml,
                                strOldXml,
                                true,
                                out newKeys,
                                out oldKeys,
                                out newDom,
                                out oldDom,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            if (bForceDeleteKeys == true)
                            {
                                // return:
                                //      -1  ����
                                //      0   �ɹ�
                                nRet = this.ForceDeleteKeys(connection,
                                    strID,
                                    out strError);
                                if (nRet == -1)
                                    return -1;
                            }

                            // ���������
                            // return:
                            //      -1  ����
                            //      0   �ɹ�
                            nRet = this.ModifyKeys(connection,
                                newKeys,
                                oldKeys,
                                bFastMode,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            // ע�������Ϊ�ɵ�XML�����ļ���ʧ�����ModifyFiles()ȥ�����Ѿ����ڵĶ���records�У���ô������Ȼ�ᱻ���ӣ�û��ʲô������

                            // �������ļ�
                            // return:
                            //      -1  ����
                            //      0   �ɹ�
                            nRet = this.ModifyFiles(connection,
                                strID,
                                newDom,
                                oldDom,
                                out strError);
                            if (nRet == -1)
                                return -1;


                            {
#if NO
                                    // 4.��new����data
                                    // return:
                                    //      -1  ����
                                    //      >=0   �ɹ� ����Ӱ��ļ�¼��
                                    nRet = this.UpdateDataField(connection,
                                        strID,
                                        out strError);
                                    if (nRet == -1)
                                        return -1;

#endif

#if NO
                                    // 5.ɾ��newdata�ֶ�
                                    string strRemoveFieldName = "";
                                    byte [] remove_textptr = null;
                                    long lRemoveLength = 0;
                                    // return:
                                    //      -1  �����ļ�
                                    //      0   ����image�ֶ�
                                    //      1   ����image�ֶ�
                                    int nReverse = GetReverse(row_info.Range);

                                    // ע���Դ�WriteSqlRecord()�Ժ��־�Ѿ���ת�����ˣ��պñ�����ʵ�����
                                    if (nReverse == 0)
                                    {
                                        strRemoveFieldName = "newdata";
                                        remove_textptr = row_info.newdata_textptr;
                                        lRemoveLength = row_info.newdata_length;
                                    }
                                    else if (nReverse == 1)
                                    {
                                        strRemoveFieldName = "data";
                                        remove_textptr = row_info.data_textptr;
                                        lRemoveLength = row_info.data_length;
                                    }

                                    if (nReverse != -1
                                        && lRemoveLength > 0 && remove_textptr != null)
                                    {
                                        // return:
                                        //		-1  ����
                                        //		0   �ɹ�
                                        nRet = this.RemoveImage(connection,
                                            strRemoveFieldName,
                                            remove_textptr,
                                            out strError);
                                        if (nRet == -1)
                                            return -1;
                                    }
#endif
                            }
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        strError = "3 WriteXml() �ڸ�'" + this.GetCaption("zh-CN") + "'��д���¼'" + strID + "'ʱ����,ԭ��:" + GetSqlErrors(sqlEx);

                        /*
                        if (sqlEx.Errors is SqlErrorCollection)
                            strError = "���ݿ�'" + this.GetCaption("zh") + "'��δ��ʼ����";
                        else
                            strError = "WriteXml() �ڸ�'" + this.GetCaption("zh-CN") + "'��д���¼'" + strID + "'ʱ����,ԭ��:" + sqlEx.Message;
                         * */

                        return -1;
                    }
                    catch (Exception ex)
                    {
                        strError = "4 WriteXml() �ڸ�'" + this.GetCaption("zh-CN") + "'��д���¼'" + strID + "'ʱ����,ԭ��:" + ex.Message;
                        return -1;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
                finally  // ��¼��
                {
                    //******�Լ�¼��д��****************************
                    m_recordLockColl.UnlockForWrite(strID);
#if DEBUG_LOCK_SQLDATABASE
					this.container.WriteDebugInfo("WriteXml()����'" + this.GetCaption("zh-CN") + "/" + strID + "'��¼��д����");
#endif
                }
            }
            finally
            {
                //********�����ݿ�����****************
                m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("WriteXml()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }


            // ������������֪Ϊ�˻����д��������ʱ, һ��Ҫ��bCheckAccount==false
            // �����ã������������𲻱�Ҫ�ĵݹ�
            if (bFull == true
                && bCheckAccount == true
                && StringUtil.IsInList("account", strDbType/*this.TypeSafety*/) == true)
            {
                string strResPath = this.FullID + "/" + strID;

                this.container.UserColl.RefreshUserSafety(strResPath);
            }
            else
            {
                if (StringUtil.IsInList("fastmode", strStyle) == false
                    && this.FastMode == true)
                {
                    // this.FastMode = false;
                    this.Commit();
                }
            }

            return 0;
        }

        // parameters:
        //      strRecorID   ��¼ID
        //      strObjectID  ����ID
        //      ��������ͬWriteXml,��strOutputID����
        // return:
        //		-1  ����
        //		-2  ʱ�����ƥ��
        //      -4  ��¼�������Դ������
        //      -6  Ȩ�޲���
        //		0   �ɹ�
        public override int WriteObject(User user,
            string strRecordID,
            string strObjectID,
            string strRanges,
            long lTotalLength,
            byte[] baSource,
            // Stream streamSource,
            string strMetadata,
            string strStyle,
            byte[] inputTimestamp,
            out byte[] outputTimestamp,
            out string strError)
        {
            outputTimestamp = null;
            strError = "";
            int nRet = 0;

            if (StringUtil.IsInList("fastmode", strStyle) == true)
                this.FastMode = true;
            bool bFastMode = StringUtil.IsInList("fastmode", strStyle) || this.FastMode;

            if (user != null)
            {
                string strTempRecordPath = this.GetCaption("zh-CN") + "/" + strRecordID;
                string strExistRights = "";
                bool bHasRight = user.HasRights(strTempRecordPath,
                    ResType.Record,
                    "overwrite",
                    out strExistRights);
                if (bHasRight == false)
                {
                    strError = "�����ʻ���Ϊ'" + user.Name + "'����'" + strTempRecordPath + "'��¼û��'����(overwrite)'Ȩ�ޣ�Ŀǰ��Ȩ��ֵΪ'" + strExistRights + "'��";
                    return -6;
                }
            }

            // ���ﲻ����ΪFastMode��д��

            //**********�����ݿ�Ӷ���************
            m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
			this.container.WriteDebugInfo("WriteObject()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif

            try
            {
                string strOutputRecordID = "";
                // return:
                //      -1  ����
                //      0   �ɹ�
                nRet = this.CanonicalizeRecordID(strRecordID,
                    out strOutputRecordID,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (strOutputRecordID == "-1")
                {
                    strError = "���������Դ��֧�ּ�¼�Ų���ֵΪ'" + strRecordID + "'��";
                    return -1;
                }
                strRecordID = strOutputRecordID;


                //**********�Լ�¼��д��***************
                m_recordLockColl.LockForWrite(strRecordID, m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("WriteObject()����'" + this.GetCaption("zh-CN") + "/" + strRecordID + "'��¼��д����");
#endif
                try // ��¼��
                {
                    // �����Ӷ���
                    Connection connection = GetConnection(
                        this.m_strConnString,
                        this.container.SqlServerType == SqlServerType.SQLite && bFastMode == true ? ConnectionStyle.Global : ConnectionStyle.None);
                    connection.Open();
                    try // ����
                    {
                        // TODO: �Ƿ���ԸĽ�Ϊ���������SQL��¼�д��ڣ���ֱ�ӽ���д�룬ֻ�е�SQL��¼�в����ڵ�ʱ��ŶԴ�����XML��¼���м�飬�����Ҫ���䴴��SQL��¼�С������������ִ���ٶ�
                        // TODO: ������lStart == 0 �ĵ�һ�ε�ʱ����
#if NO
                        // 1.�ڶ�Ӧ��xml���ݣ��ö���·���ҵ�����ID
                        string strXml;
                        // return:
                        //      -1  ����
                        //      -4  ��¼������
        //      -100    �����ļ�������
                        //      0   ��ȷ
                        nRet = this.GetXmlString(connection,
                            strRecordID,
                            out strXml,
                            out strError);
                        if (nRet <= -1)
                        {
                            strError = "����'" + strRecordID + "/" + strObjectID + "'��Դʧ�ܣ�ԭ��:" + strError;
                            return nRet;
                        }
                        XmlDocument xmlDom = new XmlDocument();
                        xmlDom.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue

                        xmlDom.LoadXml(strXml);

                        XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDom.NameTable);
                        nsmgr.AddNamespace("dprms", DpNs.dprms);
                        XmlNode fileNode = xmlDom.DocumentElement.SelectSingleNode("//dprms:file[@id='" + strObjectID + "']", nsmgr);
                        if (fileNode == null)
                        {
                            strError = "�ڼ�¼ '" + strRecordID + "' ��xml��û���ҵ�����ID '" + strObjectID + "' ��Ӧ��dprms:file�ڵ�";
                            return -1;
                        }
#endif

                        strObjectID = strRecordID + "_" + strObjectID;

                        /*
                        // 2. ����¼Ϊ�ռ�¼ʱ,��update�����ı�ָ��
                        if (this.IsEmptyObject(connection, strObjectID) == true)
                        {
                            // return
                            //		-1  ����
                            //		0   �ɹ�
                            nRet = this.UpdateObject(connection,
                                strObjectID,
                                out inputTimestamp,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }
                         * */
                        RecordRowInfo row_info = null;
                        // return:
                        //      -1  ����
                        //      0   ��¼������
                        //      1   �ɹ�
                        nRet = GetRowInfo(connection,
    strObjectID,
    out row_info,
    out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet == 0)  // 2013/11/21
                            return -4;

                        // 3.������д��rangeָ���ķ�Χ
                        bool bFull = false; // �Ƿ�Ϊ�����ɵ�һ��д�����
                        bool bSingleFull = false;
                        // return:
                        //		-1	һ���Դ���
                        //		-2	ʱ�����ƥ��
                        //		0	�ɹ�
                        nRet = this.WriteSqlRecord(connection,
                            ref row_info,
                            false,
                            strObjectID,
                            strRanges,
                            lTotalLength,
                            baSource,
                            // streamSource,
                            strMetadata,
                            strStyle,
                            inputTimestamp,
                            out outputTimestamp,
                            out bFull,
                            out bSingleFull,
                            out strError);
                        if (nRet <= -1)
                            return nRet;

                        //string strCurrentRange = this.GetRange(connection,strObjectID);
                        if (bFull == true)  //��������
                        {
#if NO111
                            // 1. ��newdata�滻data�ֶ�
                            // return:
                            //      -1  ����
                            //      >=0   �ɹ� ����Ӱ��ļ�¼��
                            nRet = this.UpdateDataField(connection,
                                strObjectID,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            // 2. ɾ��newdata�ֶ�
                            // return:
                            //		-1  ����
                            //		0   �ɹ�
                            nRet = this.DeleteDuoYuImage(connection,
                                strObjectID,
                                "newdata",
                                0,
                                out strError);
                            if (nRet == -1)
                                return -1;
#endif

#if NO
                            string strRemoveFieldName = "";
                            byte[] remove_textptr = null;
                            long lRemoveLength = 0;
                            int nReverse = GetReverse(row_info.Range);
                            // ע���Դ�WriteSqlRecord()�Ժ��־�Ѿ���ת�����ˣ��պñ�����ʵ�����
                            if (nReverse == 0)
                            {
                                strRemoveFieldName = "newdata";
                                remove_textptr = row_info.newdata_textptr;
                                lRemoveLength = row_info.newdata_length;
                            }
                            else if (nReverse == 1)
                            {
                                strRemoveFieldName = "data";
                                remove_textptr = row_info.data_textptr;
                                lRemoveLength = row_info.data_length;
                            }

                            if (nReverse != -1
                                && lRemoveLength > 0 && remove_textptr != null)
                            {
                                // return:
                                //		-1  ����
                                //		0   �ɹ�
                                nRet = this.RemoveImage(connection,
                                    strRemoveFieldName,
                                    remove_textptr,
                                    out strError);
                                if (nRet == -1)
                                    return -1;
                            }
#endif
                        }

/* // ��Ҫ�ڱ��������޸ļ�¼��ʱ�����
                        // �����޸�һ�¼�¼��ʱ���
                        string strNewTimestamp = this.CreateTimestampForDb();
                        // return:
                        //      -1  ����
                        //      >=0   �ɹ� ���ر�Ӱ��ļ�¼��
                        nRet = this.SetTimestampForDb(connection,
                            strRecordID,
                            strNewTimestamp,
                            out strError);
                        if (nRet == -1)
                            return -1;
 * */
                    }
                    catch (SqlException sqlEx)
                    {
                        strError = GetSqlErrors(sqlEx);

                        /*
                        if (sqlEx.Errors is SqlErrorCollection)
                            strError = "���ݿ�'" + this.GetCaption("zh") + "'��δ��ʼ����";
                        else
                            strError = sqlEx.Message;
                         * */
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        strError = "WriteXml() �ڸ�'" + this.GetCaption("zh-CN") + "'��д����Դ'" + strObjectID + "'ʱ����,ԭ��:" + ex.Message;
                        return -1;
                    }
                    finally // ����
                    {
                        connection.Close();
                    }
                }
                finally // ��¼��
                {
                    //*********�Լ�¼��д��****************************
                    m_recordLockColl.UnlockForWrite(strRecordID);
#if DEBUG_LOCK_SQLDATABASE
					this.container.WriteDebugInfo("WriteObject()����'" + this.GetCaption("zh-CN") + "/" + strRecordID + "'��¼��д����");
#endif

                }
            }
            finally
            {

                //************�����ݿ�����************
                m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("WriteObject()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }

            if (StringUtil.IsInList("fastmode", strStyle) == false
&& this.FastMode == true)
            {
                this.Commit();
            }

            return 0;
        }

        // ��ô���� range �ַ���
        static string GetPureRangeString(string strText)
        {
            if (string.IsNullOrEmpty(strText) == false)
            {
                if (strText[0] == '!' || strText[0] == '#')
                    return strText.Substring(1);
            }
            return strText;
        }

        // �жϱ���������ɺ��Ƿ񸲸�ȫ��Χ
        static bool IsFull(
            string strExistingRanges,
            long lTotalLength,
            string strThisRanges,
            int nThisLength)
        {
            // ׼��rangelist
            RangeList rangeList = null;
            if (string.IsNullOrEmpty(strExistingRanges) == true)
            {
                rangeList = new RangeList();
            }
            else
            {
                try
                {
                    rangeList = new RangeList(strExistingRanges);
                }
                catch (Exception ex)
                {
                    string strError = "���ַ��� '" + strExistingRanges + "' ���� RangeList ʱ����: " + ex.Message;
                    throw new Exception(strError);
                }

            }

            RangeList thisRangeList = null;

            try
            {
                thisRangeList = new RangeList(strThisRanges);
            }
            catch (Exception ex)
            {
                string strError = "���ַ��� '" + strThisRanges + "' ���� RangeList ʱ����: " + ex.Message;
                throw new Exception(strError);
            }
            // �������RangeList
            rangeList.AddRange(thisRangeList);

#if NO
            // 2015/1/21
            if (rangeList.Count == 0)
                return true;
#endif

            rangeList.Sort();
            rangeList.Merge();

            if (rangeList.Count == 1)
            {
                RangeItem item = (RangeItem)rangeList[0];

                if (item.lLength > lTotalLength)
                    return false;	// Ψһһ������ĳ��Ⱦ�Ȼ�������ĳ��ȣ�ͨ�������������������

                if (item.lStart == 0
                    && item.lLength == lTotalLength)
                    return true;	// ��ʾ��ȫ����
            }

            return false;
        }

        static bool IsSingleFull(string strRanges,
            byte [] baSource,
            long lTotalLength)
        {
            // ׼��rangelist
            RangeList rangeList = null;
            if (string.IsNullOrEmpty(strRanges) == true)
            {
                RangeItem rangeItem = new RangeItem();
                rangeItem.lStart = 0;
                rangeItem.lLength = baSource.Length;
                rangeList = new RangeList();
                rangeList.Add(rangeItem);
            }
            else
            {
                try
                {
                    rangeList = new RangeList(strRanges);
                }
                catch (Exception ex)
                {
                    string strError = "���ַ��� '" + strRanges + "' ���� RangeList ʱ����: " + ex.Message;
                    throw new Exception(strError);
                }
            }

            // һ����ȫд�������
            if (rangeList.Count == 1
                && rangeList[0].lStart == 0
                && rangeList[0].lLength == lTotalLength)
                return true;

            return false;
        }

        // ��sql��дһ����¼
        // ��baContent��streamContentд��image�ֶ���rangeָ��Ŀ��λ��,
        // ˵����sql�еļ�¼������Xml���¼Ҳ���Զ�����Դ��¼
        // �������temp��д����ɣ������old��
        // parameters:
        //		connection	    ���Ӷ���	����Ϊnull
        //		strID	        ��¼ID	����Ϊnull����ַ���
        //		strRanges	    Ŀ�귶Χ�������Χ�ö��ŷָ�
        //		nTotalLength	��¼�����ܳ���
        //						����Sql ServerĿǰֻ֧��int������nTotalLength��Ϊint���ͣ�������ӿ���long
        //		baContent	    �����ֽ�����	����Ϊnull
        //		streamContent	������	����Ϊnull
        //		strStyle	    ���
        //					    ignorechecktimestamp	����ʱ���
        //		baInputTimestamp    �����ʱ���	����Ϊnull
        //		baOutputTimestamp	out���������ص�ʱ���
        //		bFull	        out��������¼�Ƿ񱻱���д��
        //		strError	    out���������س�����Ϣ
        // return:
        //		-1	һ���Դ���
        //		-2	ʱ�����ƥ��
        //		0	�ɹ�
        // ˵��	baContent��streamContent��˭��ֵ����˭
        private int WriteSqlRecord(Connection connection,
            ref RecordRowInfo row_info,
            bool bNeedInsertRow,
            string strID,
            string strRanges,
            long lTotalLength,
            byte[] baSource,
            // Stream streamSource,
            string strMetadata,
            string strStyle,
            byte[] baInputTimestamp,
            out byte[] baOutputTimestamp,
            out bool bFull,
            out bool bSingleFull,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";
            bFull = false;
            bSingleFull = false;

            int nRet = 0;

            //-------------------------------------------
            //��������������м��
            //-------------------------------------------

            // return:
            //      -1  ����
            //      0   ����
            nRet = this.CheckConnection(connection, out strError);
            if (nRet == -1)
            {
                strError = "WriteSqlRecord()���ô���" + strError;
                return -1;
            }
            Debug.Assert(nRet == 0, "");

            if (strID == null || strID == "")
            {
                strError = "WriteSqlRecord()���ô���strID��������Ϊnull����ַ�����";
                return -1;
            }
            if (lTotalLength < 0)
            {
                strError = "WriteSqlRecord()���ô���lTotalLength����ֵ����Ϊ'" + Convert.ToString(lTotalLength) + "'��������ڵ���0��";
                return -1;
            }
            /*
            if (baSource == null && streamSource == null)
            {
                strError = "WriteSqlRecord()���ô���baSource������streamSource��������ͬʱΪnull��";
                return -1;
            }
            if (baSource != null && streamSource != null)
            {
                strError = "WriteSqlRecord()���ô���baSource������streamSource����ֻ����һ������ֵ��";
                return -1;
            }
             * */
            if (baSource == null)
            {
                strError = "WriteSqlRecord()���ô���baSource��������Ϊnull��";
                return -1;
            }
            if (strStyle == null)
                strStyle = "";
            if (strRanges == null)
                strRanges = "";
            if (strMetadata == null)
                strMetadata = "";

            long nSourceTotalLength = baSource.Length;
            /*
            if (baSource != null)
                nSourceTotalLength = baSource.Length;
            else
                nSourceTotalLength = streamSource.Length;
             * */

            // ׼��rangelist
            RangeList rangeList = null;
            if (string.IsNullOrEmpty(strRanges) == true)
            {
                RangeItem rangeItem = new RangeItem();
                rangeItem.lStart = 0;
                rangeItem.lLength = nSourceTotalLength;
                rangeList = new RangeList();
                rangeList.Add(rangeItem);
            }
            else
            {
                try
                {
                    rangeList = new RangeList(strRanges);
                }
                catch (Exception ex)
                {
                    strError = "���ַ��� '"+strRanges+"' ���� RangeList ʱ����: " + ex.Message;
                    return -1;
                }
            }

            // һ����ȫд�������
            if (rangeList.Count == 1
                && rangeList[0].lStart == 0
                && rangeList[0].lLength == lTotalLength)
            {
                bSingleFull = true;
            }

            bool bFirst = false;    // �Ƿ�Ϊ��һ��д��
            if (rangeList.Count >= 1
    && rangeList[0].lStart == 0)
            {
                bFirst = true;
            }
#if NO
            //-------------------------------------------
            //��ʼ������
            //-------------------------------------------

            ////////////////////////////////////////////////////
            // ����¼�Ƿ����,ʱ���Ƿ�ƥ��,���õ�����,range��textPtr
            /////////////////////////////////////////////////////
            string strCommand = "use " + this.m_strSqlDbName + " "
                + " SELECT TEXTPTR("+strDataFieldName+"),"
                + " DataLength("+strDataFieldName+"),"
                + " range,"
                + " dptimestamp,"
                + " metadata "
                + " FROM records "
                + " WHERE id=@id";

            strCommand += " use master " + "\n";

            SqlCommand command = new SqlCommand(strCommand,
                connection);
            SqlParameter idParam =
                command.Parameters.Add("@id",
                SqlDbType.NVarChar);
            idParam.Value = strID;

            byte[] textPtr = null;
            string strOldMetadata = "";
            string strCurrentRange = "";
            long lCurrentLength = 0;
            string strOutputTimestamp = "";

            SqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
            try
            {
                // 1.��¼�����ڱ���
                if (dr == null
                    || dr.HasRows == false)
                {
                    strError = "��¼ '" + strID + "' �ڿ��в����ڣ���������²�Ӧ������";
                    return -1;
                }

                dr.Read();

                // 2.textPtrΪnull����
                if (dr[0] is System.DBNull)
                {
                    strError = "TextPtr������Ϊnull";
                    return -1;
                }
                textPtr = (byte[])dr[0];

                // 3.ʱ���������Ϊnull,ʱ�����ƥ�䱨��
                if ((dr[4] is System.DBNull))
                {
                    strError = "ʱ���������Ϊnull";
                    return -1;
                }

                // ��strStyle���� ignorechecktimestampʱ�����ж�ʱ���
                strOutputTimestamp = dr.GetString(3);
                baOutputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);

                if (StringUtil.IsInList("ignorechecktimestamp", strStyle) == false)
                {
                    if (ByteArray.Compare(baInputTimestamp,
                        baOutputTimestamp) != 0)
                    {
                        strError = "ʱ�����ƥ��";
                        return -2;
                    }
                }
                // 4.metadataΪnull����
                if ((dr[4] is System.DBNull))
                {
                    strError = "Metadata������Ϊnull";
                    return -1;
                }
                strOldMetadata = dr.GetString(4);


                // 5.rangeΪnull�ı���
                if ((dr[2] is System.DBNull))
                {
                    strError = "range��ʱҲ������Ϊnull";
                    return -1;
                }
                strCurrentRange = dr.GetString(2);

                // 6.ȡ������
                lCurrentLength = dr.GetInt32(1);


                bool bRet = dr.Read();

                // 2008/3/13 new add
                if (bRet == true)
                {
                    // ����һ��
                    strError = "��¼ '" + strID + "' ��SQL��" + this.m_strSqlDbName + "��records���д��ڶ���������һ�ֲ�������״̬, ��ϵͳ����Ա����SQL����ɾ������ļ�¼��";
                    return -1;
                }
            }
            finally
            {
                dr.Close();
            }
#endif
            bool bObjectFile = false;

            string strCurrentRange = row_info.Range;
            bool bReverse = false;  // �����־�����Ϊfalse����ʾ data Ϊ��ʽ���ݣ�newdataΪ��ʱ����

            string strDataFieldName = "newdata";    // ��ʱ�洢�ֶ���
            byte[] textptr = row_info.newdata_textptr;  // ����ָ��
            long lCurrentLength = row_info.newdata_length;  // �����������
            string strCompleteTimestamp = row_info.TimestampString; // �ϴ�����д��ʱ��ʱ���
            string strCurrentTimestamp = row_info.NewTimestampString; // ���ε�ʱ���

            // �������ݵ�ʱ���
            if (String.IsNullOrEmpty(strCurrentRange) == false
    && strCurrentRange[0] == '!')
            {
                strCompleteTimestamp = row_info.NewTimestampString;
                strCurrentTimestamp = row_info.TimestampString;
            }

            if (this.m_lObjectStartSize != -1 && lTotalLength >= this.m_lObjectStartSize
#if !XML_WRITE_TO_FILE
                && (strID.Length > 10 || connection.SqlServerType != SqlServerType.MsSqlServer)   // д������ļ���ֻ��Զ����ƶ��󣬶��������ͨXML��¼
#endif
                && String.IsNullOrEmpty(strCurrentRange) == false
                && strCurrentRange[0] == '#')
            {
                bObjectFile = true;
                strCurrentRange = strCurrentRange.Substring(1);

                lCurrentLength = GetObjectFileLength(strID, true);
            }
            else if (this.m_lObjectStartSize != -1 && lTotalLength >= this.m_lObjectStartSize
#if !XML_WRITE_TO_FILE
                && (strID.Length > 10 || connection.SqlServerType != SqlServerType.MsSqlServer)
#endif
 && (string.IsNullOrEmpty(strCurrentRange) == true || strCurrentRange == "!") )
            {
                bObjectFile = true;

                /*
                if (strCurrentRange == "!")
                    lCurrentLength = row_info.data_length;
                 * */
                // ԭ���Ǵ洢��image�ֶ��У����Ǳ���Ҫ��Ϊ�洢��object file�У�����lCurrentLength���Ϊ0
                lCurrentLength = 0;
                strCurrentRange = "";
            }
            else
            {
                if (String.IsNullOrEmpty(strCurrentRange) == false
                    && strCurrentRange[0] == '!')
                {
                    bReverse = true;
                    strCurrentRange = strCurrentRange.Substring(1);
                    strDataFieldName = "data";
                    textptr = row_info.data_textptr;
                    lCurrentLength = row_info.data_length;
                    strCompleteTimestamp = row_info.NewTimestampString;
                    strCurrentTimestamp = row_info.TimestampString;
                }

                if (String.IsNullOrEmpty(strCurrentRange) == false
    && strCurrentRange[0] == '#')
                {
                    strCurrentRange = strCurrentRange.Substring(1);
                    if (string.IsNullOrEmpty(strCurrentRange) == false)
                    {
                        // TODO: ת����ʽ��ʱ����Ҫ����
                    }

                }

            }

            // ��strStyle���� ignorechecktimestampʱ�����ж�ʱ���
            if (StringUtil.IsInList("ignorechecktimestamp", strStyle) == false)
            {
                // �����ʱ�������ݣ���Ҫ����ʱʱ��������Ƚϡ�����ͱȽ��������ʱ���
                if (string.IsNullOrEmpty(strCurrentRange) == false)
                {
                    // strCurrentTimestamp = strCurrentTimestamp;
                }
                else
                {
                    strCurrentTimestamp = strCompleteTimestamp;
                }

                if (string.IsNullOrEmpty(strCurrentTimestamp) == false)
                {
                    byte[] baExistTimestamp = ByteArray.GetTimeStampByteArray(strCurrentTimestamp);
                    if (ByteArray.Compare(baInputTimestamp,
                        baExistTimestamp) != 0)
                    {
                        strError = "ʱ�����ƥ��";
                        baOutputTimestamp = baExistTimestamp;   // ���ظ�ǰ�ˣ���ǰ���ܹ���֪��ǰ��ʱ���
                        return -2;
                    }
                }
            }

            bool bDeleted = false;

            // ����rangeд����
            int nStartOfBuffer = 0;    // ��������λ��
            int nState = 0;
            for (int i = 0; i < rangeList.Count; i++)
            {
                bool bCanDeleteDuoYu = false;  // ȱʡ������ɾ������ĳ���

                RangeItem range = (RangeItem)rangeList[i];
                long lStartOfTarget = range.lStart;     // �ָ���image�ֶε�λ��  
                int nNeedReadLength = (int)range.lLength;   // ��Ҫ���������ĳ���
                if (rangeList.Count == 1 && nNeedReadLength == 0)
                {
                    bFull = true;
                    break;
                }

                string strThisEnd = Convert.ToString(lStartOfTarget + (Int64)nNeedReadLength - (Int64)1);

                Debug.Assert(strThisEnd.IndexOf("-") == -1, "");

                string strThisRange = Convert.ToString(lStartOfTarget)
                    + "-" + strThisEnd;

                string strNewRange;
                nState = RangeList.MergeContentRangeString(strThisRange,
                    strCurrentRange,
                    lTotalLength,
                    out strNewRange,
                    out strError);
                if (nState == -1)
                {
                    strError = "MergeContentRangeString() error 4 : " + strError + " (strThisRange='" + strThisRange + "' strCurrentRange='" + strCurrentRange + "' ) lTotalLength=" + lTotalLength.ToString() + "";
                    return -1;
                }
                if (nState == 1)  //��Χ����
                {
                    bFull = true;
                    string strFullEnd = "";
                    int nPosition = strNewRange.IndexOf('-');
                    if (nPosition >= 0)
                        strFullEnd = strNewRange.Substring(nPosition + 1);

                    // ��Ϊ��Χ�����һ��,�ұ��η�Χ��ĩβ�����ܷ�Χ��ĩβ,�һ�û��ɾ��ʱ
                    if (i == rangeList.Count - 1
                        && (strFullEnd == strThisEnd)
                        && bDeleted == false)
                    {
                        bCanDeleteDuoYu = true;
                        bDeleted = true;
                    }
                }
                strCurrentRange = strNewRange;

                if (bObjectFile == true)
                {
                    // д������ļ�

                    if (string.IsNullOrEmpty(this.m_strObjectDir) == true)
                    {
                        strError = "���ݿ���δ���ö����ļ�Ŀ¼����д�����ʱ��������Ҫ���ö����ļ������";
                        return -1;
                    }

                    string strFileName = "";
                    if (bFirst == true)
                    {
                        strFileName = BuildObjectFileName(strID, true);
                        row_info.NewFileName = GetShortFileName(strFileName); // ����
                        if (row_info.NewFileName == null)
                        {
                            strError = "������ļ���ʱ������¼ID '"+strID+"', �����ļ�Ŀ¼ '"+this.m_strObjectDir+"', �����ļ��� '"+strFileName+"'";
                            return -1;
                        }
                    }
                    else
                    {
                        // �ڻ�û���ļ��������һ������д�벻�Ǵ�0��ʼ�Ĳ���
                        if (string.IsNullOrEmpty(row_info.NewFileName) == true)
                        {
                            strFileName = BuildObjectFileName(strID, true);
                            row_info.NewFileName = GetShortFileName(strFileName); // ����
                        }

                        Debug.Assert(string.IsNullOrEmpty(row_info.NewFileName) == false, "");
                        strFileName = GetObjectFileName(row_info.NewFileName);
                    }

                    int nRedoCount = 0;
                REDO:
                    try
                    {
                        using (FileStream s = File.Open(
        strFileName,
        FileMode.OpenOrCreate,
        FileAccess.Write,
        FileShare.ReadWrite))
                        {
                            // ��һ��д�ļ�,�����ļ����ȴ��ڶ����ܳ��ȣ���ض��ļ�
                            if (bFirst == true && s.Length > lTotalLength)
                                s.SetLength(0);

                            s.Seek(lStartOfTarget, SeekOrigin.Begin);
                            s.Write(baSource,
                                nStartOfBuffer,
                                nNeedReadLength);
                        }
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        if (nRedoCount == 0)
                        {
                            // �����м���Ŀ¼
                            PathUtil.CreateDirIfNeed(PathUtil.PathPart(strFileName));
                            nRedoCount++;
                            goto REDO;
                        }
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        strError = "д���ļ� '"+strFileName+"' ʱ��������: " + ex.Message;
                        return -1;
                    }

                    lCurrentLength = Math.Max(lStartOfTarget + nNeedReadLength, lCurrentLength);
                }
                else
                {
                    // Ӧ���Ѿ�׼��������
                    Debug.Assert(bNeedInsertRow == false, "");

                    // return:	
                    //		-1  ����
                    //		0   �ɹ�
                    nRet = this.WriteImage(connection,
                        ref textptr,
                        ref lCurrentLength,   // ��ǰimage�ĳ����ڲ��ϵı仯��
                        bCanDeleteDuoYu,
                        strID,
                        strDataFieldName,   // "newdata",
                        lStartOfTarget,
                        baSource,
                        // streamSource,
                        nStartOfBuffer,
                        nNeedReadLength,
                        lTotalLength,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // ת���洢��ʽʱҪ��ʱɾ��ԭ�еĶ����ļ�
                    if (string.IsNullOrEmpty(row_info.FileName) == false)
                    {
                        File.Delete(GetObjectFileName(row_info.FileName));
                        row_info.FileName = "";
                    }
                    if (string.IsNullOrEmpty(row_info.NewFileName) == false)
                    {
                        File.Delete(GetObjectFileName(row_info.NewFileName));
                        row_info.NewFileName = "";
                    }
                }

                nStartOfBuffer += nNeedReadLength;

                // textptr�п��ܱ�WriteImage()�����޸�
                if (bReverse == false)
                {
                    row_info.newdata_textptr = textptr;
                    row_info.newdata_length = lCurrentLength;
                }
                else
                {
                    row_info.data_textptr = textptr;
                    row_info.data_length = lCurrentLength;
                }
            }

            // TODO: ע�����ﲻҪ�ж���Ĳ�����ע���ٶ�����
            if (bFull == true)
            {
#if NO
                if (bDeleted == false)
                {
                    // ����¼������ʱ��ɾ�������ֵ
                    // return:
                    //		-1  ����
                    //		0   �ɹ�
                    nRet = this.DeleteDuoYuImage(connection,
                        strID,
                        strDataFieldName,   // "newdata",
                        lTotalLength,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }
#endif
                strCurrentRange = "";
                lCurrentLength = lTotalLength;

                if (bObjectFile == true)
                {
                    string strDeletedFilename = "";
                    // �����ļ�����
                    if (string.IsNullOrEmpty(row_info.FileName) == false)
                    {
                        strDeletedFilename = GetObjectFileName(row_info.FileName);
                        File.Delete(strDeletedFilename);   // ɾ��ԭ�е���ʽ�ļ�
                    }

                    // ��ʽ�ļ�����������
                    string strFileName = BuildObjectFileName(strID, false); // ���ļ���
                    row_info.FileName = GetShortFileName(strFileName); // ���ļ���

                    if (lTotalLength == 0)
                    {
                        // ����һ��0bytes���ļ�
                        int nRedoCount = 0;
                    REDO:
                        try
                        {
                            using (FileStream s = File.Open(
    strFileName,
    FileMode.OpenOrCreate,
    FileAccess.Write,
    FileShare.ReadWrite))
                            {
                                s.SetLength(0);
                            }
                        }
                        catch (DirectoryNotFoundException ex)
                        {
                            if (nRedoCount == 0)
                            {
                                // �����м���Ŀ¼
                                PathUtil.CreateDirIfNeed(PathUtil.PathPart(strFileName));
                                nRedoCount++;
                                goto REDO;
                            }
                            throw ex;
                        }
                        catch (Exception ex)
                        {
                            strError = "����0�ֽڵ��ļ� '"+strFileName+"' ʱ����" + ex.Message;
                            return -1;
                        }
                    }
                    else
                    {
                        Debug.Assert(string.IsNullOrEmpty(row_info.NewFileName) == false, "");
                        string strSourceFilename = GetObjectFileName(row_info.NewFileName);

                        if (strDeletedFilename != strFileName)
                            File.Delete(strFileName);   // ������ɾ���Ѿ����ڵ�Ŀ���ļ���TODO: ���߳����Ժ�������?

                        try
                        {
                            File.Move(strSourceFilename, strFileName);    // ����
                        }
                        catch (FileNotFoundException /* ex */)
                        {
                            // ���Դ�ļ�������
                            strError = "�����ļ�(��ʱ�ļ�) '" + strSourceFilename + "' ������...";
                            return -1;
                        }
                    }

                    row_info.NewFileName = "";
                }
            }
            else
            {
                lCurrentLength = -1;
            }

            // ���,����range,metadata,dptimestamp;

            // �õ���Ϻ��Metadata;
            string strResultMetadata = "";
            if (bFull == true)
            {
                // return:
                //		-1	����
                //		0	�ɹ�
                nRet = DatabaseUtil.MergeMetadata(row_info.Metadata,
                    strMetadata,
                    lCurrentLength,
                    out strResultMetadata,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            // 2013/11/23
            // �Ƿ�Ҫֱ�����������ʱ���
            bool bForceTimestamp = StringUtil.IsInList("forcesettimestamp", strStyle);

            // �����µ�ʱ���,���浽���ݿ���
            string strOutputTimestamp = "";
            if (bForceTimestamp == true)
                strOutputTimestamp = ByteArray.GetHexTimeStampString(baInputTimestamp);
            else
                strOutputTimestamp = this.CreateTimestampForDb();

            string strCommand = "";
            if (bObjectFile == false)
            {
                string strSetNull = ""; // ���ü���ɾ���� timestamp �ֶ�����Ϊ�յ����
                if (bFull == true)
                {
                    strSetNull = (bReverse == true ? " newdptimestamp=NULL, newdata=NULL," : " dptimestamp=NULL, data=NULL,");
                    // ʱ�����data���ݶ����
                }

                strCommand = "use " + this.m_strSqlDbName + "\n"
                    + " UPDATE records "
                    + (bReverse == true ? " SET dptimestamp=@dptimestamp," : " SET newdptimestamp=@dptimestamp,")
                    + strSetNull
                    + " range=@range,"
                    + " filename=NULL, newfilename=NULL,"
                    + " metadata=@metadata "
                    + " WHERE id=@id";
            }
            else
            {
                string strSetNull = ""; // ���ü���ɾ���� timestamp �ֶ�����Ϊ�յ����
                if (bFull == true)
                    strSetNull = " newdptimestamp=NULL,";

                if (connection.SqlServerType == SqlServerType.MsSqlServer)
                {
                    strCommand = "use " + this.m_strSqlDbName + "\n"
                         + " UPDATE records "
                         + (bFull == true ? " SET dptimestamp=@dptimestamp," : " SET newdptimestamp=@dptimestamp,")
                         + strSetNull
                         + " range=@range,"
                         + " metadata=@metadata,"
                         + (bFull == true ? " filename=@filename, newfilename=NULL," : " newfilename=@filename,")
                         + " data=NULL, newdata=NULL "
                         + " WHERE id=@id";
                    strCommand += " use master " + "\n";
                }
                else if (connection.SqlServerType == SqlServerType.SQLite)
                {
                    if (bNeedInsertRow == false)
                    {
                        strCommand = " UPDATE records "
                             + (bFull == true ? " SET dptimestamp=@dptimestamp," : " SET newdptimestamp=@dptimestamp,")
                             + strSetNull
                             + " range=@range,"
                             + " metadata=@metadata,"
                             + (bFull == true ? " filename=@filename, newfilename=NULL " : " newfilename=@filename ")
                             + " WHERE id=@id";
                    }
                    else
                    {
                        strCommand = " INSERT INTO records(id, range, metadata, dptimestamp, newdptimestamp, filename, newfilename) "
                            + (bFull == true ? " VALUES(@id, @range, @metadata, @dptimestamp, NULL, @filename, NULL)"
                                             : " VALUES(@id, @range, @metadata, NULL, @dptimestamp, NULL, @filename)");

                    }
                }
                else if (connection.SqlServerType == SqlServerType.MySql)
                {
                    if (bNeedInsertRow == false)
                    {
                        strCommand = " UPDATE `" + this.m_strSqlDbName + "`.records "
                             + (bFull == true ? " SET dptimestamp=@dptimestamp," : " SET newdptimestamp=@dptimestamp,")
                             + strSetNull
                             + " `range`=@range,"
                             + " metadata=@metadata,"
                             + (bFull == true ? " filename=@filename, newfilename=NULL " : " newfilename=@filename ")
                             + " WHERE id=@id";
                    }
                    else
                    {
                        strCommand = " INSERT INTO `" + this.m_strSqlDbName + "`.records (id, `range`, metadata, dptimestamp, newdptimestamp, filename, newfilename) "
                            + (bFull == true ? " VALUES (@id, @range, @metadata, @dptimestamp, NULL, @filename, NULL)"
                                             : " VALUES (@id, @range, @metadata, NULL, @dptimestamp, NULL, @filename)");

                    }
                }
                else if (connection.SqlServerType == SqlServerType.Oracle)
                {
                    if (bNeedInsertRow == false)
                    {
                        strCommand = " UPDATE " + this.m_strSqlDbName + "_records "
                             + (bFull == true ? " SET dptimestamp=:dptimestamp," : " SET newdptimestamp=:dptimestamp,")
                             + strSetNull
                             + " range=:range,"
                             + " metadata=:metadata,"
                             + (bFull == true ? " filename=:filename, newfilename=NULL " : " newfilename=:filename ")
                             + " WHERE id=:id";
                    }
                    else
                    {
                        strCommand = " INSERT INTO " + this.m_strSqlDbName + "_records (id, range, metadata, dptimestamp, newdptimestamp, filename, newfilename) "
                            + (bFull == true ? " VALUES (:id, :range, :metadata, :dptimestamp, NULL, :filename, NULL)"
                                             : " VALUES (:id, :range, :metadata, NULL, :dptimestamp, NULL, :filename)");

                    }
                }

            }


            if (connection.SqlServerType == SqlServerType.MsSqlServer)
            {
                using (SqlCommand command = new SqlCommand(strCommand,
                    connection.SqlConnection))
                {

                    SqlParameter idParam = command.Parameters.Add("@id",
        SqlDbType.NVarChar);
                    idParam.Value = strID;

                    SqlParameter dptimestampParam =
                        command.Parameters.Add("@dptimestamp",
                        SqlDbType.NVarChar,
                        100);
                    dptimestampParam.Value = strOutputTimestamp;

                    SqlParameter rangeParam =
                        command.Parameters.Add("@range",
                        SqlDbType.NVarChar,
                        4000);
                    if (bObjectFile == true)
                        rangeParam.Value = "#" + strCurrentRange;
                    else
                    {
                        if (bFull == true)
                            rangeParam.Value = (bReverse == false ? "!" : "") + strCurrentRange;   // ��ת
                        else
                            rangeParam.Value = (bReverse == true ? "!" : "") + strCurrentRange;   // ����ת
                    }

                    row_info.Range = (string)rangeParam.Value;  // ����ת�����ʱ����


                    SqlParameter metadataParam =
                        command.Parameters.Add("@metadata",
                        SqlDbType.NVarChar,
                        4000);
                    if (bFull == true)
                        metadataParam.Value = strResultMetadata;    // ֻ�е����һ��д���ʱ��Ÿ��� metadata
                    else
                        metadataParam.Value = row_info.Metadata;

                    if (bObjectFile == true)
                    {
                        SqlParameter filenameParam =
                command.Parameters.Add("@filename",
                SqlDbType.NVarChar,
                255);
                        if (bFull == true)
                            filenameParam.Value = row_info.FileName;
                        else
                            filenameParam.Value = row_info.NewFileName;
                    }

                    int nCount = command.ExecuteNonQuery();
                    if (nCount == 0)
                    {
                        strError = "���¼�¼��Ϊ '" + strID + "' ���е� ʱ���,range,metadata,(new)filename ʧ��";
                        return -1;
                    }
                } // end of using command
            }
            else if (connection.SqlServerType == SqlServerType.SQLite)
            {
                using (SQLiteCommand command = new SQLiteCommand(strCommand,
                    connection.SQLiteConnection))
                {

                    SQLiteParameter idParam = command.Parameters.Add("@id",
                        DbType.String);
                    idParam.Value = strID;

                    SQLiteParameter dptimestampParam =
                        command.Parameters.Add("@dptimestamp",
                        DbType.String,
                        100);
                    dptimestampParam.Value = strOutputTimestamp;

                    SQLiteParameter rangeParam =
                        command.Parameters.Add("@range",
                        DbType.String,
                        4000);
                    if (bObjectFile == true)
                        rangeParam.Value = "#" + strCurrentRange;
                    else
                    {
                        Debug.Assert(false, "�������ߵ�����");
                        /*
                        if (bFull == true)
                            rangeParam.Value = (bReverse == false ? "!" : "") + strCurrentRange;   // ��ת
                        else
                            rangeParam.Value = (bReverse == true ? "!" : "") + strCurrentRange;   // ����ת
                         * */
                    }

                    row_info.Range = (string)rangeParam.Value;  // ����ת�����ʱ����


                    SQLiteParameter metadataParam =
                        command.Parameters.Add("@metadata",
                        DbType.String,
                        4000);
                    if (bFull == true)
                        metadataParam.Value = strResultMetadata;    // ֻ�е����һ��д���ʱ��Ÿ��� metadata
                    else
                        metadataParam.Value = row_info.Metadata;

                    if (bObjectFile == true)
                    {
                        SQLiteParameter filenameParam =
                command.Parameters.Add("@filename",
                DbType.String,
                255);
                        if (bFull == true)
                            filenameParam.Value = row_info.FileName;
                        else
                            filenameParam.Value = row_info.NewFileName;
                    }

                    try
                    {
                        int nCount = command.ExecuteNonQuery();
                        // ????
                        if (nCount == 0)
                        {
                            strError = "���¼�¼��Ϊ '" + strID + "' ���е� ʱ���,range,metadata,(new)filename ʧ��";
                            return -1;
                        }
                    }
                    catch (SQLiteException ex)
                    {
                        strError = "ִ��SQL��䷢������: " + ex.Message + "\r\nSQL ���: " + strCommand;
                        return -1;
                    }
                } // end of using command
            }
            else if (connection.SqlServerType == SqlServerType.MySql)
            {
                // ע�� MySql ����� SQLite ����һ��
                using (MySqlCommand command = new MySqlCommand(strCommand,
                    connection.MySqlConnection))
                {

                    MySqlParameter idParam = command.Parameters.Add("@id",
                        MySqlDbType.String);
                    idParam.Value = strID;

                    MySqlParameter dptimestampParam =
                        command.Parameters.Add("@dptimestamp",
                        MySqlDbType.String,
                        100);
                    dptimestampParam.Value = strOutputTimestamp;

                    MySqlParameter rangeParam =
                        command.Parameters.Add("@range",
                        MySqlDbType.String,
                        4000);
                    if (bObjectFile == true)
                        rangeParam.Value = "#" + strCurrentRange;
                    else
                    {
                        Debug.Assert(false, "�������ߵ�����");
                    }

                    row_info.Range = (string)rangeParam.Value;  // ����ת�����ʱ����

                    MySqlParameter metadataParam =
                        command.Parameters.Add("@metadata",
                        MySqlDbType.String,
                        4000);
                    if (bFull == true)
                        metadataParam.Value = strResultMetadata;    // ֻ�е����һ��д���ʱ��Ÿ��� metadata
                    else
                        metadataParam.Value = row_info.Metadata;

                    if (bObjectFile == true)
                    {
                        MySqlParameter filenameParam =
                command.Parameters.Add("@filename",
                MySqlDbType.String,
                255);
                        if (bFull == true)
                            filenameParam.Value = row_info.FileName;
                        else
                            filenameParam.Value = row_info.NewFileName;
                    }

                    try
                    {
                        int nCount = command.ExecuteNonQuery();
                        // ????
                        if (nCount == 0)
                        {
                            strError = "���¼�¼��Ϊ '" + strID + "' ���е� ʱ���,range,metadata,(new)filename ʧ��";
                            return -1;
                        }
                    }
                    catch (MySqlException ex)
                    {
                        strError = "ִ��SQL��䷢������: " + ex.Message + "\r\nSQL ���: " + strCommand;
                        return -1;
                    }
                } // end of using command

            }
            else if (connection.SqlServerType == SqlServerType.Oracle)
            {
                // ע�� Oracle ����� MySql ����һ��
                using (OracleCommand command = new OracleCommand(strCommand,
                    connection.OracleConnection))
                {

                    command.BindByName = true;

                    OracleParameter idParam = command.Parameters.Add(":id",
                        OracleDbType.NVarchar2);
                    idParam.Value = strID;

                    OracleParameter dptimestampParam =
                        command.Parameters.Add(":dptimestamp",
                        OracleDbType.NVarchar2,
                        100);
                    dptimestampParam.Value = strOutputTimestamp;

                    OracleParameter rangeParam =
                        command.Parameters.Add(":range",
                        OracleDbType.NVarchar2,
                        4000);
                    if (bObjectFile == true)
                        rangeParam.Value = "#" + strCurrentRange;
                    else
                    {
                        Debug.Assert(false, "�������ߵ�����");
                    }

                    row_info.Range = (string)rangeParam.Value;  // ����ת�����ʱ����

                    OracleParameter metadataParam =
                        command.Parameters.Add(":metadata",
                        OracleDbType.NVarchar2,
                        4000);
                    if (bFull == true)
                        metadataParam.Value = strResultMetadata;    // ֻ�е����һ��д���ʱ��Ÿ��� metadata
                    else
                        metadataParam.Value = row_info.Metadata;

                    if (bObjectFile == true)
                    {
                        OracleParameter filenameParam =
                command.Parameters.Add(":filename",
                OracleDbType.NVarchar2,
                255);
                        if (bFull == true)
                            filenameParam.Value = row_info.FileName;
                        else
                            filenameParam.Value = row_info.NewFileName;
                    }

                    try
                    {
                        int nCount = command.ExecuteNonQuery();
                        // ????
                        if (nCount == 0)
                        {
                            strError = "���¼�¼��Ϊ '" + strID + "' ���е� ʱ���,range,metadata,(new)filename ʧ��";
                            return -1;
                        }
                    }
                    catch (MySqlException ex)
                    {
                        strError = "ִ��SQL��䷢������: " + ex.Message + "\r\nSQL ���: " + strCommand;
                        return -1;
                    }

                } // end of using command

            }
            else
            {
                strError = "δ��ʶ��� SqlServerType '"+connection.SqlServerType.ToString()+"'";
                return -1;
            }


            baOutputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);    // Encoding.UTF8.GetBytes(strOutputTimestamp);

            // ���α仯���ʱ���
            if (bObjectFile == true)
            {
                if (bFull == true)
                {
                    row_info.TimestampString = strOutputTimestamp;
                    row_info.NewTimestampString = "";
                }
                else
                {
                    row_info.NewTimestampString = strOutputTimestamp;
                }
            }
            else
            {
                if (bReverse == false)
                    row_info.NewTimestampString = strOutputTimestamp;
                else
                    row_info.TimestampString = strOutputTimestamp;

                if (bFull == true)
                {
                    // ��ӳ�Ѿ������
                    if (bReverse == false)
                    {
                        row_info.TimestampString = "";

                        row_info.data_length = 0;
                        row_info.data_textptr = null;
                    }
                    else
                    {
                        row_info.NewTimestampString = "";

                        row_info.newdata_length = 0;
                        row_info.newdata_textptr = null;
                    }
                }
            }

            // ע����������һ��д�룬��������ʱ��newdata�ֶ����ݱ����
            return 0;
        }

        // return:
        //      -1  �����ļ�
        //      0   ����image�ֶ�
        //      1   ����image�ֶ�
        static int GetReverse(string strCurrentRange)
        {
            if (String.IsNullOrEmpty(strCurrentRange) == false
    && strCurrentRange[0] == '#')
                return -1;
            if (String.IsNullOrEmpty(strCurrentRange) == false
                && strCurrentRange[0] == '!')
                return 1;
            return 0;
        }

        // дimage�ֶε�����
        // ����ָ��һ��textprtָ��
        // parameter:
        //		connection  ���Ӷ���
        //		textPtr     imageָ��
        //		nOldLength  ԭ����
        //		nDeleteDuoYu    �Ƿ�ɾ������
        //		strID           ��¼id
        //		strImageFieldName   image�ֶ�
        //		nStartOfTarget      Ŀ�����ʼλ��
        //		sourceBuffer    Դ���ֽ�����
        //		streamSource    Դ����
        //		nStartOfBuffer  Դ������ʼλ��
        //		nNeedReadLength ��Ҫд�ĳ���
        //		strError        out���������س�����Ϣ
        // return:	
        //		-1  ����
        //		0   �ɹ�
        private int WriteImage(Connection connection,
            ref byte[] textPtr,
            ref long lCurrentLength,           // ԭ���ĳ���     
            bool bDeleteDuoYu,
            string strID,
            string strImageFieldName,
            long lStartOfTarget,       // Ŀ�����ʼλ��
            byte[] baSource,
            // Stream streamSource,
            int nStartOfSource,     // ��������ʵ��λ�� ���� >=0 
            int nNeedReadLength,    // ��Ҫ���������ĳ��ȿ�����-1,��ʾ��Դ��nSourceStartλ�õ�ĩβ
            long lTotalLength,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            //---------------------------------------
            //���м���������
            //-----------------------------------------
            /*
            if (baSource == null && streamSource == null)
            {
                strError = "WriteImage()���ô���baSource������streamSource��������ͬʱΪnull��";
                return -1;
            }
            if (baSource != null && streamSource != null)
            {
                strError = "WriteImage()���ô���baSource������streamSource����ֻ����һ������ֵ��";
                return -1;
            }
             * */
            if (baSource == null)
            {
                strError = "WriteImage()���ô���baSource��������Ϊnull��";
                return -1;
            }

            if (connection.SqlServerType != SqlServerType.MsSqlServer)
            {
                strError = "SqlServerType '"+connection.SqlServerType.ToString()+"' ��connection�������ڵ���WriteImage()����";
                return -1;
            }


            int nSourceTotalLength = baSource.Length;
            /*
            if (baSource != null)
                nSourceTotalLength = baSource.Length;
            else
                nSourceTotalLength = (int)streamSource.Length;
             * */

            long lOutputLength = 0;
            // return:
            //		-1  ����
            //		0   �ɹ�
            nRet = ConvertUtil.GetRealLength(nStartOfSource,
                nNeedReadLength,
                nSourceTotalLength,
                -1,//nMaxLength
                out lOutputLength,
                out strError);
            if (nRet == -1)
                return -1;


            //---------------------------------------
            //��ʼ������
            //-----------------------------------------
            if (textPtr == null 
                || lStartOfTarget == 0 && lCurrentLength > lTotalLength)
            {
                string strCommand = "use " + this.m_strSqlDbName + " "
    + " UPDATE records "
    + " set " + strImageFieldName + "=0x0 "
    + " where id='" + strID + "'\n"
    + " SELECT TEXTPTR(" + strImageFieldName + ") from records"
    + " where id='" + strID + "'\n";

                strCommand += " use master " + "\n";

                SqlCommand command = new SqlCommand(strCommand,
                    connection.SqlConnection);

                SqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                try
                {
                    // 1.��¼�����ڱ���
                    if (dr == null
                        || dr.HasRows == false)
                    {
                        strError = "��¼ '" + strID + "' �ڿ��в�����";
                        return -1;
                    }

                    dr.Read();

                    textPtr = (byte[])dr[0];

                    bool bRet = dr.Read();

                    if (bRet == true)
                    {
                        // ����һ��
                        strError = "��¼ '" + strID + "' ��SQL��" + this.m_strSqlDbName + "��records���д��ڶ���������һ�ֲ�������״̬, ��ϵͳ����Ա����SQL����ɾ������ļ�¼��";
                        return -1;
                    }

                    lCurrentLength = 1; // ��ʾд�ɹ���һ�� 0 �ַ�
                }
                finally
                {
                    dr.Close();
                }
            }

            Debug.Assert(textPtr != null, "");

            {
                int chucksize = 32 * 1024;  //д��ʱÿ��Ϊ32K


                // ִ�и��²���,ʹ��UPDATETEXT���

                // UPDATETEXT����˵��:
                // dest_text_ptr: ָ��Ҫ���µ�image ���ݵ��ı�ָ���ֵ���� TEXTPTR �������أ�����Ϊ binary(16)
                // insert_offset: ����Ϊ���ĸ�����ʼλ��,
                //				  ����image �У�insert_offset ���ڲ���������ǰ�������е���㿪ʼҪ�������ֽ���
                //				  ��ʼ���������Ϊ������ʼ������� image ���������ƣ�Ϊ�������ڳ��ռ䡣
                //				  ֵΪ 0 ��ʾ�������ݲ��뵽����λ�õĿ�ʼ����ֵΪ NULL ��������׷�ӵ���������ֵ�С�
                // delete_length: �Ǵ� insert_offset λ�ÿ�ʼ�ġ�Ҫ������ image ����ɾ�������ݳ��ȡ�
                //				  delete_length ֵ���� text �� image �����ֽ�ָ�������� ntext �����ַ�ָ����ÿ�� ntext �ַ�ռ�� 2 ���ֽڡ�
                //				  ֵΪ 0 ��ʾ��ɾ�����ݡ�ֵΪ NULL ��ɾ������ text �� image ���д� insert_offset λ�ÿ�ʼ��ĩβ���������ݡ�
                // WITH LOG:      �� Microsoft? SQL Server? 2000 �б����ԡ��ڸð汾�У���־��¼�����ݿ����Ч�ָ�ģ�;�����
                // inserted_data: ��Ҫ���뵽���� text��ntext �� image �� insert_offset λ�õ����ݡ�
                //				  ���ǵ��� char��nchar��varchar��nvarchar��binary��varbinary��text��ntext �� image ֵ��
                //				  inserted_data ���������ֻ������
                // ���ʹ��UPDATETEXT����?
                // �滻��������:  ָ��һ���ǿ� insert_offset ֵ������ delete_length ֵ��Ҫ����������ݡ�
                // ɾ����������:  ָ��һ���ǿ� insert_offset ֵ������ delete_length ֵ����ָ��Ҫ����������ݡ�
                // ����������:    ָ�� insert_offset ֵ��Ϊ��� delete_length ֵ��Ҫ����������ݡ�
                string strCommand = "use " + this.m_strSqlDbName + " "
                    + " UPDATETEXT records." + strImageFieldName
                    + " @dest_text_ptr"
                    + " @insert_offset"
                    + " @delete_length"
#if UPDATETEXT_WITHLOG
                    + " WITH LOG"
#endif
                    + " @inserted_data";   //���ܼ�where���

                strCommand += " use master " + "\n";

                SqlCommand command = new SqlCommand(strCommand,
                    connection.SqlConnection);

                // ��������ֵ
                SqlParameter dest_text_ptrParam =
                    command.Parameters.Add("@dest_text_ptr",
                    SqlDbType.Binary,
                    16);

                SqlParameter insert_offsetParam =
                    command.Parameters.Add("@insert_offset",
                    SqlDbType.Int);  // old Int

                SqlParameter delete_lengthParam =
                    command.Parameters.Add("@delete_length",
                    SqlDbType.Int);  // old Int

                SqlParameter inserted_dataParam =
                    command.Parameters.Add("@inserted_data",
                    SqlDbType.Binary,
                    0);

                long insert_offset = lStartOfTarget; // ����image�ֶε�λ��
                int nReadStartOfBuffer = nStartOfSource;         // ��Դ�������еĶ�����ʼλ��
                Byte[] chuckBuffer = null; // �黺����
                int nCount = 0;             // Ӱ��ļ�¼����

                dest_text_ptrParam.Value = textPtr;

                while (true)
                {
                    // �Ѵӻ����������ĳ���
                    int nReadedLength = nReadStartOfBuffer - nStartOfSource;
                    if (nReadedLength >= nNeedReadLength)
                        break;

                    // ����Ҫ���ĳ���
                    int nContinueLength = nNeedReadLength - nReadedLength;
                    if (nContinueLength > chucksize)  // ��Դ���ж��ĳ���
                        nContinueLength = chucksize;

                    inserted_dataParam.Size = nContinueLength;
                    chuckBuffer = new byte[nContinueLength];

                    /*
                    if (baSource != null)
                     * */
                    {
                        // ����Դ�����һ�ε�ÿ������д��chuckbuffer
                        Array.Copy(baSource,
                            nReadStartOfBuffer,
                            chuckBuffer,
                            0,
                            nContinueLength);
                    }
                    /*
                    else
                    {
                        streamSource.Read(chuckBuffer,
                            0,
                            nContinueLength);
                    }
                     * */

                    if (chuckBuffer.Length <= 0)
                        break;

                    insert_offsetParam.Value = insert_offset;

#if NO
                    // ɾ���ֶεĳ���
                    long lDeleteLength = 0;
                    if (bDeleteDuoYu == true)  //���һ��
                    {
                        lDeleteLength = lCurrentLength - insert_offset;  // ��ǰ���ȱ�ʾimage�ĳ���
                        if (lDeleteLength < 0)
                            lDeleteLength = 0;
                    }
                    else
                    {
                        // д��ĳ��ȳ�����ǰ��󳤶�ʱ,Ҫɾ���ĳ���Ϊ��ǰ����-start
                        if (insert_offset + chuckBuffer.Length > lCurrentLength)
                        {
                            lDeleteLength = lCurrentLength - insert_offset;
                            if (lDeleteLength < 0)
                                lDeleteLength = lCurrentLength;
                        }
                        else
                        {
                            lDeleteLength = chuckBuffer.Length;
                        }
                    }
#endif

                    // null��ʾ�Ӳ���㵽ĩβ��ԭ��������ȫ��ɾ�� 2013/2/15
                    delete_lengthParam.Value = DBNull.Value;   // lDeleteLength;
                    inserted_dataParam.Value = chuckBuffer;

                    nCount = command.ExecuteNonQuery();
                    if (nCount == 0)
                    {
                        strError = "û�и��µ���¼��";
                        return -1;
                    }

                    // д���,��ǰ���ȷ����ı仯
                    // lCurrentLength = lCurrentLength + chuckBuffer.Length - lDeleteLength;
                    lCurrentLength = insert_offset + chuckBuffer.Length;    // 2012/2/15

                    // ��������λ�ñ仯
                    nReadStartOfBuffer += chuckBuffer.Length;

                    // Ŀ���λ�ñ仯
                    insert_offset += chuckBuffer.Length;   //�ָ�ʱҪ�ָ���ԭ����λ��

                    if (chuckBuffer.Length < chucksize)
                        break;
                }
            }

            return 0;
        }


        // �ӳ�ʼ����Ĵ����п��Եõ���������ٳ�ȫ��keys����

        // �޸ļ�����keys
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int ModifyKeys(Connection connection,
            KeyCollection keysAdd,
            KeyCollection keysDelete,
            bool bFastMode,
            out string strError)
        {
            strError = "";
            StringBuilder strCommand = new StringBuilder(4096);

            int nCount1 = 0;
            int nCount2 = 0;

            if (keysAdd != null)
                nCount1 = keysAdd.Count;
            if (keysDelete != null)
                nCount2 = keysDelete.Count;

            if (nCount1 == 0 && nCount2 == 0)
                return 0;

            string strRecordID = "";
            if (keysAdd != null && keysAdd.Count > 0)
                strRecordID = ((KeyItem)keysAdd[0]).RecordID;
            else if (keysDelete != null && keysDelete.Count > 0)
                strRecordID = ((KeyItem)keysDelete[0]).RecordID;

            #region MS SQL Server
            if (connection.SqlServerType == SqlServerType.MsSqlServer)
            {
                using (SqlCommand command = new SqlCommand("",
                    connection.SqlConnection))
                {
                    SqlTransaction trans = null;
                    // trans = connection.SqlConnection.BeginTransaction();
                    // command.Transaction = trans;

                    try
                    {
                        int i = 0;
                        int nNameIndex = 0;

                        int nCount = 0; // �ۻ�����δ�������������� 2008/10/21 new add
                        int nExecuted = 0;   // �Ѿ�����ִ�е��������� 2008/10/21 new add

                        int nMaxLinesPerExecute = (2100 / 5) - 1;   // 4������������һ��sql�����ַ��� 2008/10/23 new add

                        // 2006/12/8 ��ɾ����ǰ��������ǰ
                        if (keysDelete != null)
                        {
                            // ɾ��keys
                            for (i = 0; i < keysDelete.Count; i++)
                            {
                                KeyItem oneKey = (KeyItem)keysDelete[i];

                                string strKeysTableName = oneKey.SqlTableName;

                                string strIndex = Convert.ToString(nNameIndex++);

                                string strKeyParamName = "@key" + strIndex;
                                string strFromParamName = "@from" + strIndex;
                                string strIdParamName = "@id" + strIndex;
                                string strKeynumParamName = "@keynum" + strIndex;

                                strCommand.Append( " DELETE FROM " + strKeysTableName
                                    + " WHERE keystring = " + strKeyParamName 
                                    + " AND fromstring = " + strFromParamName
                                    + " AND idstring = " + strIdParamName
                                    + " AND keystringnum = " + strKeynumParamName );

                                SqlParameter keyParam =
                                    command.Parameters.Add(strKeyParamName,
                                    SqlDbType.NVarChar);
                                keyParam.Value = oneKey.Key;

                                SqlParameter fromParam =
                                    command.Parameters.Add(strFromParamName,
                                    SqlDbType.NVarChar);
                                fromParam.Value = oneKey.FromValue;

                                SqlParameter idParam =
                                    command.Parameters.Add(strIdParamName,
                                    SqlDbType.NVarChar);
                                idParam.Value = oneKey.RecordID;

                                SqlParameter keynumParam =
                                    command.Parameters.Add(strKeynumParamName,
                                    SqlDbType.NVarChar);
                                keynumParam.Value = oneKey.Num;

                                if (nCount >= nMaxLinesPerExecute)
                                {
                                    command.CommandText = "use " + this.m_strSqlDbName + " \n"
                                        + strCommand
                                        + " use master " + "\n";
                                    command.CommandTimeout = 20 * 60;  // �ѳ�ʱʱ��Ŵ� 2013/2/19
                                    try
                                    {
                                        command.ExecuteNonQuery();
                                    }
                                    catch (Exception ex)
                                    {
                                        // TODO: ������ֳ�ʱ��������ʵ�� SQL Server һ���Ѿ���ȷִ�У����Բ�������������ִ����ȥ
                                        // �����Ҫ���Դ�������������������Ҫ�����𿪳�Ϊһ��һ�������Ķ�����䣬Ȼ�����²����ɾ���������ʱ�������ظ����͵��������������ɾ����ʱ�������в����ڣ�Ҳ������������
                                        strError = "�������������, ƫ�� " + (nExecuted).ToString() + "����¼·��'" + this.GetCaption("zh-CN") + "/" + strRecordID + "��ԭ��" + ex.Message;
                                        return -1;
                                    }
                                    strCommand.Clear();
                                    nExecuted += nCount;
                                    nCount = 0;
                                    command.Parameters.Clear();
                                }
                                else
                                {
                                    nCount++;
                                }
                            }
                        }

                        if (keysAdd != null)
                        {
                            // nCount = keysAdd.Count;

                            // ����keys
                            for (i = 0; i < keysAdd.Count; i++)
                            {
                                KeyItem oneKey = (KeyItem)keysAdd[i];

                                string strKeysTableName = oneKey.SqlTableName;

                                // string strIndex = Convert.ToString(i);
                                string strIndex = Convert.ToString(nNameIndex++);

                                string strKeyParamName = "@key" + strIndex;
                                string strFromParamName = "@from" + strIndex;
                                string strIdParamName = "@id" + strIndex;
                                string strKeynumParamName = "@keynum" + strIndex;


                                //��keynum
                                strCommand.Append(" INSERT INTO " + strKeysTableName
                                    + " (keystring,fromstring,idstring,keystringnum) "
                                    + " VALUES (" + strKeyParamName + ","
                                    + strFromParamName + ","
                                    + strIdParamName + ","
                                    + strKeynumParamName + ")");

                                SqlParameter keyParam =
                                    command.Parameters.Add(strKeyParamName,
                                    SqlDbType.NVarChar);
                                keyParam.Value = oneKey.Key;

                                SqlParameter fromParam =
                                    command.Parameters.Add(strFromParamName,
                                    SqlDbType.NVarChar);
                                fromParam.Value = oneKey.FromValue;

                                SqlParameter idParam =
                                    command.Parameters.Add(strIdParamName,
                                    SqlDbType.NVarChar);
                                idParam.Value = oneKey.RecordID;

                                SqlParameter keynumParam =
                                    command.Parameters.Add(strKeynumParamName,
                                    SqlDbType.NVarChar);
                                keynumParam.Value = oneKey.Num;

                                if (nCount >= nMaxLinesPerExecute)
                                {
                                    command.CommandText = "use " + this.m_strSqlDbName + " \n"
                                        + strCommand
                                        + " use master " + "\n";
                                    command.CommandTimeout = 20 * 60;  // �ѳ�ʱʱ��Ŵ� 2013/2/19
                                    try
                                    {
                                        command.ExecuteNonQuery();
                                    }
                                    catch (Exception ex)
                                    {
                                        strError = "�������������,ƫ�� " + (nExecuted).ToString() + "����¼·��'" + this.GetCaption("zh-CN") + "/" + strRecordID + "��ԭ��" + ex.Message;
                                        return -1;
                                    }
                                    strCommand.Clear();
                                    nExecuted += nCount;
                                    nCount = 0;
                                    command.Parameters.Clear();
                                }
                                else
                                {
                                    nCount++;
                                }
                            }
                        }



                        // ������ʣ�µ�����
                        if (strCommand.Length > 0)
                        {
                            command.CommandText = "use " + this.m_strSqlDbName + " \n"
                                + strCommand
                                + " use master " + "\n";
                            command.CommandTimeout = 20 * 60;  // �ѳ�ʱʱ��Ŵ� 2013/2/19
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "�������������,ƫ�� " + (nExecuted).ToString() + "����¼·��'" + this.GetCaption("zh-CN") + "/" + strRecordID + "��ԭ��" + ex.Message;
                                return -1;
                            }

                            strCommand.Clear();
                            nExecuted += nCount;
                            nCount = 0;
                            command.Parameters.Clear();
                        }
                        if (trans != null)
                        {
                            trans.Commit();
                            trans = null;
                        }
                    }
                    finally
                    {
                        if (trans != null)
                            trans.Rollback();
                    }
                } // end of using command

                return 0;
            }
#endregion // MS SQL Server

            #region SQLite
            else if (connection.SqlServerType == SqlServerType.SQLite)
            {
                using (SQLiteCommand command = new SQLiteCommand("",
                    connection.SQLiteConnection))
                {

                    IDbTransaction trans = null;

                    if (bFastMode == false)
                        trans = connection.SQLiteConnection.BeginTransaction();
                    try
                    {

                        int i = 0;
                        int nNameIndex = 0;
                        int nCount = 0; // �ۻ�����δ��������������

                        // ��ɾ����ǰ��������ǰ
                        if (keysDelete != null)
                        {
                            // ɾ��keys
                            for (i = 0; i < keysDelete.Count; i++)
                            {
                                KeyItem oneKey = (KeyItem)keysDelete[i];

                                string strKeysTableName = oneKey.SqlTableName;

                                string strIndex = Convert.ToString(nNameIndex++);

                                string strKeyParamName = "@key" + strIndex;
                                string strFromParamName = "@from" + strIndex;
                                string strIdParamName = "@id" + strIndex;
                                string strKeynumParamName = "@keynum" + strIndex;

                                strCommand.Append( " DELETE FROM " + strKeysTableName
                                    + " WHERE keystring = " + strKeyParamName
                                    + " AND fromstring = " + strFromParamName 
                                    + " AND idstring = " + strIdParamName 
                                    + " AND keystringnum = " + strKeynumParamName
                                    + " ; ");

                                SQLiteParameter keyParam =
                                    command.Parameters.Add(strKeyParamName,
                                    DbType.String);
                                keyParam.Value = oneKey.Key;

                                SQLiteParameter fromParam =
                                    command.Parameters.Add(strFromParamName,
                                    DbType.String);
                                fromParam.Value = oneKey.FromValue;

                                SQLiteParameter idParam =
                                    command.Parameters.Add(strIdParamName,
                                    DbType.String);
                                idParam.Value = oneKey.RecordID;

                                SQLiteParameter keynumParam =
                                    command.Parameters.Add(strKeynumParamName,
                                    DbType.String);
                                keynumParam.Value = oneKey.Num;

                                command.CommandText = strCommand.ToString();
                                try
                                {
                                    command.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    strError = "�������������, ƫ�� " + (nCount).ToString() + "����¼·��'" + this.GetCaption("zh-CN") + "/" + strRecordID + "��ԭ��" + ex.Message;
                                    return -1;
                                }
                                strCommand.Clear();

                                command.Parameters.Clear();

                                nCount++;
                            }
                        }

                        if (keysAdd != null)
                        {
                            // nCount = keysAdd.Count;

                            // ����keys
                            for (i = 0; i < keysAdd.Count; i++)
                            {
                                KeyItem oneKey = (KeyItem)keysAdd[i];

                                string strKeysTableName = oneKey.SqlTableName;

                                // string strIndex = Convert.ToString(i);
                                string strIndex = Convert.ToString(nNameIndex++);

                                string strKeyParamName = "@key" + strIndex;
                                string strFromParamName = "@from" + strIndex;
                                string strIdParamName = "@id" + strIndex;
                                string strKeynumParamName = "@keynum" + strIndex;

                                //��keynum
                                strCommand.Append( " INSERT INTO " + strKeysTableName
                                    + " (keystring,fromstring,idstring,keystringnum) "
                                    + " VALUES (" + strKeyParamName + ","
                                    + strFromParamName + ","
                                    + strIdParamName + ","
                                    + strKeynumParamName + ") ; ");

                                SQLiteParameter keyParam =
                                    command.Parameters.Add(strKeyParamName,
                                    DbType.String);
                                keyParam.Value = oneKey.Key;

                                SQLiteParameter fromParam =
                                    command.Parameters.Add(strFromParamName,
                                    DbType.String);
                                fromParam.Value = oneKey.FromValue;

                                SQLiteParameter idParam =
                                    command.Parameters.Add(strIdParamName,
                                    DbType.String);
                                idParam.Value = oneKey.RecordID;

                                SQLiteParameter keynumParam =
                                    command.Parameters.Add(strKeynumParamName,
                                    DbType.String);
                                keynumParam.Value = oneKey.Num;

                                command.CommandText = strCommand.ToString();
                                try
                                {
                                    command.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    strError = "�������������,ƫ�� " + (nCount).ToString() + "����¼·��'" + this.GetCaption("zh-CN") + "/" + strRecordID + "��ԭ��" + ex.Message;
                                    return -1;
                                }
                                strCommand.Clear();

                                command.Parameters.Clear();

                                nCount++;
                            }
                        }
                        if (trans != null)
                        {
                            trans.Commit();
                            trans = null;
                        }
                    }
                    finally
                    {
                        if (trans != null)
                            trans.Rollback();
                    }
                } // end of using command
            }
#endregion // SQLite

            #region MySql
            else if (connection.SqlServerType == SqlServerType.MySql)
            {
                using (MySqlCommand command = new MySqlCommand("",
                    connection.MySqlConnection))
                {
                    MySqlTransaction trans = null;

                    trans = connection.MySqlConnection.BeginTransaction();
                    try
                    {
                        int i = 0;
#if PARAMETERS
                        int nNameIndex = 0;
#endif

                        int nCount = 0; // �ۻ�����δ�������������� 
                        int nExecuted = 0;   // �Ѿ�����ִ�е��������� 

#if PARAMETERS
                        int nMaxLinesPerExecute = (2100 / 5) - 1;   // 4������������һ��sql�����ַ���
#else
                        int nMaxLinesPerExecute = 5000;
#endif

                        if (keysDelete != null)
                        {
                            // ɾ��keys
                            for (i = 0; i < keysDelete.Count; i++)
                            {
                                KeyItem oneKey = (KeyItem)keysDelete[i];

                                string strKeysTableName = oneKey.SqlTableName;

#if PARAMETERS
                                string strIndex = Convert.ToString(nNameIndex++);

                                string strKeyParamName = "@key" + strIndex;
                                string strFromParamName = "@from" + strIndex;
                                string strIdParamName = "@id" + strIndex;
                                string strKeynumParamName = "@keynum" + strIndex;

                                strCommand.Append(" DELETE FROM " + strKeysTableName
                                    + " WHERE keystring = " + strKeyParamName
                                    + " AND fromstring = " + strFromParamName 
                                    + " AND idstring = " + strIdParamName 
                                    + " AND keystringnum = " + strKeynumParamName + " ;\n");

                                MySqlParameter keyParam =
                                    command.Parameters.Add(strKeyParamName,
                                    MySqlDbType.String);
                                keyParam.Value = oneKey.Key;

                                MySqlParameter fromParam =
                                    command.Parameters.Add(strFromParamName,
                                    MySqlDbType.String);
                                fromParam.Value = oneKey.FromValue;

                                MySqlParameter idParam =
                                    command.Parameters.Add(strIdParamName,
                                    MySqlDbType.String);
                                idParam.Value = oneKey.RecordID;

                                MySqlParameter keynumParam =
                                    command.Parameters.Add(strKeynumParamName,
                                    MySqlDbType.String);
                                keynumParam.Value = oneKey.Num;
#else

                                strCommand.Append(" DELETE FROM " + strKeysTableName
    + " WHERE keystring = '" + MySqlHelper.EscapeString(oneKey.Key)
    + "' AND fromstring = '" + MySqlHelper.EscapeString(oneKey.FromValue)
    + "' AND idstring = '" + MySqlHelper.EscapeString(oneKey.RecordID)
    + "' AND keystringnum = '" + MySqlHelper.EscapeString(oneKey.Num) + "' ;\n");

#endif


                                if (nCount >= nMaxLinesPerExecute)
                                {
                                    // ÿ100�������һ��
                                    command.CommandText = "use " + this.m_strSqlDbName + " ;\n"
                                        + strCommand
#if !PARAMETERS
                                        + " ;\n"
#endif
                                        ;
                                    try
                                    {
                                        command.ExecuteNonQuery();
                                    }
                                    catch (Exception ex)
                                    {
                                        strError = "�������������, ƫ�� " + (nExecuted).ToString() + "����¼·��'" + this.GetCaption("zh-CN") + "/" + strRecordID + "��ԭ��" + ex.Message;
                                        return -1;
                                    }
                                    strCommand.Clear();
                                    nExecuted += nCount;
                                    nCount = 0;
                                    command.Parameters.Clear();
                                }
                                else
                                {
                                    nCount++;
                                }
                            }
                        }

                        if (keysAdd != null)
                        {
                            // nCount = keysAdd.Count;
#if !PARAMETERS
                            string strPrevSqlTableName = "";
#endif

                            // ����keys
                            for (i = 0; i < keysAdd.Count; i++)
                            {
                                KeyItem oneKey = (KeyItem)keysAdd[i];

                                string strKeysTableName = oneKey.SqlTableName;

#if PARAMETERS
                                // string strIndex = Convert.ToString(i);
                                string strIndex = Convert.ToString(nNameIndex++);

                                string strKeyParamName = "@key" + strIndex;
                                string strFromParamName = "@from" + strIndex;
                                string strIdParamName = "@id" + strIndex;
                                string strKeynumParamName = "@keynum" + strIndex;

                                //��keynum
                                strCommand.Append(" INSERT INTO " + strKeysTableName
                                    + " (keystring,fromstring,idstring,keystringnum) "
                                    + " VALUES (" + strKeyParamName + ","
                                    + strFromParamName + ","
                                    + strIdParamName + ","
                                    + strKeynumParamName + ") ;\n");

                                MySqlParameter keyParam =
                                    command.Parameters.Add(strKeyParamName,
                                    MySqlDbType.String);
                                keyParam.Value = oneKey.Key;

                                MySqlParameter fromParam =
                                    command.Parameters.Add(strFromParamName,
                                    MySqlDbType.String);
                                fromParam.Value = oneKey.FromValue;

                                MySqlParameter idParam =
                                    command.Parameters.Add(strIdParamName,
                                    MySqlDbType.String);
                                idParam.Value = oneKey.RecordID;

                                MySqlParameter keynumParam =
                                    command.Parameters.Add(strKeynumParamName,
                                    MySqlDbType.String);
                                keynumParam.Value = oneKey.Num;
#else
                                if (strCommand.Length == 0
                                    || strKeysTableName != strPrevSqlTableName)
                                {
                                    if (strCommand.Length > 0)
                                        strCommand.Append(" ; ");

                                    strCommand.Append(" INSERT INTO " + strKeysTableName
        + " (keystring,fromstring,idstring,keystringnum) "
        + " VALUES ('" + MySqlHelper.EscapeString(oneKey.Key) + "','"
        + MySqlHelper.EscapeString(oneKey.FromValue) + "','"
        + MySqlHelper.EscapeString(oneKey.RecordID) + "','"
        + MySqlHelper.EscapeString(oneKey.Num) + "') ");
                                }
                                else
                                {
                                    strCommand.Append(", ('" + MySqlHelper.EscapeString(oneKey.Key) + "','"
        + MySqlHelper.EscapeString(oneKey.FromValue) + "','"
        + MySqlHelper.EscapeString(oneKey.RecordID) + "','"
        + MySqlHelper.EscapeString(oneKey.Num) + "') ");
                                }

                                strPrevSqlTableName = strKeysTableName;
#endif

                                if (nCount >= nMaxLinesPerExecute)
                                {
                                    // ÿ100�������һ��
                                    command.CommandText = "use " + this.m_strSqlDbName + " ;\n"
                                        + strCommand
#if !PARAMETERS
                                        + " ;\n"
#endif
                                        ;
                                    try
                                    {
                                        command.ExecuteNonQuery();
                                    }
                                    catch (Exception ex)
                                    {
                                        strError = "�������������,ƫ�� " + (nExecuted).ToString() + "����¼·��'" + this.GetCaption("zh-CN") + "/" + strRecordID + "��ԭ��" + ex.Message;
                                        return -1;
                                    }
                                    strCommand.Clear();
                                    nExecuted += nCount;
                                    nCount = 0;
                                    command.Parameters.Clear();
                                }
                                else
                                {
                                    nCount++;
                                }
                            }
                        }

                        // ������ʣ�µ�����
                        if (strCommand.Length > 0)
                        {
                            command.CommandText = "use " + this.m_strSqlDbName + " ;\n"
                                + strCommand
#if !PARAMETERS
                                + " ;\n"
#endif
                                ;
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "�������������,ƫ�� " + (nExecuted).ToString() + "����¼·��'" + this.GetCaption("zh-CN") + "/" + strRecordID + "��ԭ��" + ex.Message;
                                return -1;
                            }

                            strCommand.Clear();
                            nExecuted += nCount;
                            nCount = 0;
                            command.Parameters.Clear();
                        }
                        if (trans != null)
                        {
                            trans.Commit();
                            trans = null;
                        }
                    }
                    finally
                    {
                        if (trans != null)
                            trans.Rollback();
                    }
                } // end of using command

                return 0;
            }
#endregion // MySql

            #region Oracle
            else if (connection.SqlServerType == SqlServerType.Oracle)
            {
                using (OracleCommand command = new OracleCommand("", connection.OracleConnection))
                {
                    command.BindByName = true;

                    IDbTransaction trans = null;

                    trans = connection.OracleConnection.BeginTransaction();
                    try
                    {

                        int i = 0;
                        int nNameIndex = 0;
                        int nCount = 0; // �ۻ�����δ��������������

                        // ��ɾ����ǰ��������ǰ
                        if (keysDelete != null)
                        {
                            // ɾ��keys
                            for (i = 0; i < keysDelete.Count; i++)
                            {
                                KeyItem oneKey = (KeyItem)keysDelete[i];

                                string strKeysTableName = oneKey.SqlTableName;

                                string strIndex = Convert.ToString(nNameIndex++);

                                string strKeyParamName = ":key" + strIndex;
                                string strFromParamName = ":from" + strIndex;
                                string strIdParamName = ":id" + strIndex;
                                string strKeynumParamName = ":keynum" + strIndex;

                                strCommand.Append(" DELETE FROM " + this.m_strSqlDbName + "_" + strKeysTableName
                                    + " WHERE keystring = " + strKeyParamName
                                    + " AND fromstring = " + strFromParamName 
                                    + " AND idstring = " + strIdParamName 
                                    + " AND keystringnum = " + strKeynumParamName
                                    + " ");

                                OracleParameter keyParam =
                                    command.Parameters.Add(strKeyParamName,
                                    OracleDbType.NVarchar2);
                                keyParam.Value = oneKey.Key;

                                OracleParameter fromParam =
                                    command.Parameters.Add(strFromParamName,
                                    OracleDbType.NVarchar2);
                                fromParam.Value = oneKey.FromValue;

                                OracleParameter idParam =
                                    command.Parameters.Add(strIdParamName,
                                    OracleDbType.NVarchar2);
                                idParam.Value = oneKey.RecordID;

                                OracleParameter keynumParam =
                                    command.Parameters.Add(strKeynumParamName,
                                    OracleDbType.NVarchar2);
                                keynumParam.Value = oneKey.Num;

                                command.CommandText = strCommand.ToString();
                                try
                                {
                                    command.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    strError = "ɾ�����������, ƫ�� " + (nCount).ToString() + "����¼·��'" + this.GetCaption("zh-CN") + "/" + strRecordID + "��ԭ��" + ex.Message;
                                    return -1;
                                }
                                strCommand.Clear();

                                // ÿ�ж�����������ۻ�����ֵ
                                command.Parameters.Clear();
                                nCount++;
                            }
                        }

                        if (keysAdd != null)
                        {
                            // nCount = keysAdd.Count;

                            // ����keys
                            for (i = 0; i < keysAdd.Count; i++)
                            {
                                KeyItem oneKey = (KeyItem)keysAdd[i];

                                string strKeysTableName = oneKey.SqlTableName;

                                // string strIndex = Convert.ToString(i);
                                string strIndex = Convert.ToString(nNameIndex++);

                                string strKeyParamName = ":key" + strIndex;
                                string strFromParamName = ":from" + strIndex;
                                string strIdParamName = ":id" + strIndex;
                                string strKeynumParamName = ":keynum" + strIndex;

                                //��keynum
                                strCommand.Append(" INSERT INTO " + this.m_strSqlDbName + "_" + strKeysTableName
                                    + " (keystring,fromstring,idstring,keystringnum) "
                                    + " VALUES(" + strKeyParamName + ","
                                    + strFromParamName + ","
                                    + strIdParamName + ","
                                    + strKeynumParamName + ")  ");

                                OracleParameter keyParam =
                                    command.Parameters.Add(strKeyParamName,
                                    OracleDbType.NVarchar2);
                                keyParam.Value = oneKey.Key;

                                OracleParameter fromParam =
                                    command.Parameters.Add(strFromParamName,
                                    OracleDbType.NVarchar2);
                                fromParam.Value = oneKey.FromValue;

                                OracleParameter idParam =
                                    command.Parameters.Add(strIdParamName,
                                    OracleDbType.NVarchar2);
                                idParam.Value = oneKey.RecordID;

                                OracleParameter keynumParam =
                                    command.Parameters.Add(strKeynumParamName,
                                    OracleDbType.NVarchar2);
                                keynumParam.Value = oneKey.Num;

                                command.CommandText = strCommand.ToString();
                                try
                                {
                                    command.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    strError = "�������������,ƫ�� " + (nCount).ToString() + "����¼·��'" + this.GetCaption("zh-CN") + "/" + strRecordID + "��ԭ��" + ex.Message;
                                    return -1;
                                }
                                strCommand.Clear();

                                // ÿ�ж�����������ۻ�����ֵ
                                command.Parameters.Clear();

                                nCount++;
                            }
                        }
                        if (trans != null)
                        {
                            trans.Commit();
                            trans = null;
                        }
                    }
                    finally
                    {
                        if (trans != null)
                            trans.Rollback();
                    }
                } // end of using command
            }
#endregion // Oracle

            return 0;
        }

        // �������ļ�
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int ModifyFiles(Connection connection,
            string strID,
            XmlDocument newDom,
            XmlDocument oldDom,
            out string strError)
        {
            strError = "";
            strID = DbPath.GetID10(strID);

            // ���ļ�
            List<string> new_fileids = new List<string>();
            if (newDom != null)
            {
                XmlNamespaceManager newNsmgr = new XmlNamespaceManager(newDom.NameTable);
                newNsmgr.AddNamespace("dprms", DpNs.dprms);
                XmlNodeList newFileList = newDom.SelectNodes("//dprms:file", newNsmgr);
                foreach (XmlNode newFileNode in newFileList)
                {
                    string strNewFileID = DomUtil.GetAttr(newFileNode,
                        "id");
                    if (string.IsNullOrEmpty(strNewFileID) == false)
                        new_fileids.Add(strNewFileID);
                }
            }

            // ���ļ�
            List<string> old_fileids = new List<string>();
            if (oldDom != null)
            {
                XmlNamespaceManager oldNsmgr = new XmlNamespaceManager(oldDom.NameTable);
                oldNsmgr.AddNamespace("dprms", DpNs.dprms);
                XmlNodeList oldFileList = oldDom.SelectNodes("//dprms:file", oldNsmgr);
                foreach (XmlNode oldFileNode in oldFileList)
                {
                    string strOldFileID = DomUtil.GetAttr(oldFileNode,
                        "id");
                    if (string.IsNullOrEmpty(strOldFileID) == false)
                        old_fileids.Add(strOldFileID);
                }
            }

            if (new_fileids.Count == 0 && old_fileids.Count == 0)
                return 0;

            //���ݱ���������
            //aNewFileID.Sort(new ComparerClass());
            //aOldFileID.Sort(new ComparerClass());
            new_fileids.Sort();  // TODO: ��Сд�Ƿ����� ?
            old_fileids.Sort();

            List<string> targetLeft = new List<string>();
            List<string> targetMiddle = null;   //  new List<string>();
            List<string> targetRight = new List<string>();

            //�¾�����File������
            StringUtil.MergeStringList(new_fileids,
                old_fileids,
                ref targetLeft,
                ref targetMiddle,
                ref targetRight);

            if (targetLeft.Count == 0 && targetRight.Count == 0)
                return 0;

            List<string> filenames = new List<string>();    // �����ļ������� (���ļ���)

            #region MS SQL Server
            if (connection.SqlServerType == SqlServerType.MsSqlServer)
            {
                string strCommand = "";
                using (SqlCommand command = new SqlCommand("",
                    connection.SqlConnection))
                {
                    int nCount = 0;

                    // TODO: ע���עMS SQL Server �������� 2100 ����

                    // ɾ�����ļ�
                    if (targetRight.Count > 0)
                    {
                        if (this.m_lObjectStartSize != -1)
                        {
                            // ��úʹ�����ļ���
                            string strWhere = "";
                            for (int i = 0; i < targetRight.Count; i++)
                            {
                                string strPureObjectID = targetRight[i];
                                string strObjectID = strID + "_" + strPureObjectID;
                                string strParamIDName = "@id" + Convert.ToString(i);

                                // ׼���ò���
                                SqlParameter idParam =
                                    command.Parameters.Add(strParamIDName,
                                    SqlDbType.NVarChar);
                                idParam.Value = strObjectID;

                                if (string.IsNullOrEmpty(strWhere) == false)
                                    strWhere += " OR ";
                                strWhere += " id = " + strParamIDName + " ";
                            }

                            if (string.IsNullOrEmpty(strWhere) == false)
                            {
                                strCommand = " SELECT filename, newfilename FROM records WHERE " + strWhere + " \n";
                                strCommand = "use " + this.m_strSqlDbName + " \n"
        + strCommand
        + " use master " + "\n";
                                command.CommandText = strCommand;

                                using (SqlDataReader dr = command.ExecuteReader())
                                {
                                    if (dr.HasRows == true)
                                    {
                                        while (dr.Read())
                                        {
                                            if (dr.IsDBNull(0) == false)
                                                filenames.Add(dr.GetString(0));
                                            if (dr.IsDBNull(1) == false)
                                                filenames.Add(dr.GetString(1));
                                        }
                                    }
                                }

                                command.Parameters.Clear();
                            }
                        }

                        // ����ɾ����records�е����
                        strCommand = "";
                        command.Parameters.Clear();
                        nCount = targetRight.Count;

                        for (int i = 0; i < targetRight.Count; i++)
                        {
                            string strPureObjectID = targetRight[i];
                            string strObjectID = strID + "_" + strPureObjectID;

                            string strParamIDName = "@id" + Convert.ToString(i);
                            strCommand += " DELETE FROM records WHERE id = " + strParamIDName + " \n";
                            SqlParameter idParam =
                                command.Parameters.Add(strParamIDName,
                                SqlDbType.NVarChar);
                            idParam.Value = strObjectID;
                        }
                    }

                    // �������ļ�
                    if (targetLeft.Count > 0)
                    {
                        // ���촴����records�е����
                        for (int i = 0; i < targetLeft.Count; i++)
                        {
                            string strPureObjectID = targetLeft[i];
                            string strObjectID = strID + "_" + strPureObjectID;

                            string strParamIDName = "@id" + Convert.ToString(i) + nCount;
                            strCommand += " INSERT INTO records(id) "
                                + " VALUES(" + strParamIDName + ")\n";
                            SqlParameter idParam =
                                command.Parameters.Add(strParamIDName,
                                SqlDbType.NVarChar);
                            idParam.Value = strObjectID;
                        }
                    }

                    if (string.IsNullOrEmpty(strCommand) == false)
                    {
                        strCommand = "use " + this.m_strSqlDbName + " \n"
                            + strCommand
                            + " use master " + "\n";

                        command.CommandText = strCommand;
                        command.CommandTimeout = 30 * 60; // 30����

                        int nResultCount = 0;
                        try
                        {
                            nResultCount = command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            strError = "�����¼·��Ϊ'" + this.GetCaption("zh") + "/" + strID + "'�����ļ���������:" + ex.Message + ",sql����:\r\n" + strCommand;
                            return -1;
                        }

                        if (nResultCount != targetRight.Count + targetLeft.Count)
                        {
                            this.container.KernelApplication.WriteErrorLog("ϣ��������ļ���'" + Convert.ToString(targetRight.Count + targetLeft.Count) + "'����ʵ��ɾ�����ļ���'" + Convert.ToString(nResultCount) + "'��");
                        }
                    }
                } // enf of using command
            }
            #endregion // MS SQL Server

            #region SQLite
            else if (connection.SqlServerType == SqlServerType.SQLite)
            {
                string strCommand = "";
                using (SQLiteCommand command = new SQLiteCommand("",
                    connection.SQLiteConnection))
                {

                    int nCount = 0;
                    // ɾ�����ļ�
                    if (targetRight.Count > 0)
                    {
                        // ��úʹ�����ļ���
                        string strWhere = "";
                        for (int i = 0; i < targetRight.Count; i++)
                        {
                            string strPureObjectID = targetRight[i];
                            string strObjectID = strID + "_" + strPureObjectID;
                            string strParamIDName = "@id" + Convert.ToString(i);

                            // ׼���ò���
                            SQLiteParameter idParam =
                                command.Parameters.Add(strParamIDName,
                                DbType.String);
                            idParam.Value = strObjectID;

                            if (string.IsNullOrEmpty(strWhere) == false)
                                strWhere += " OR ";
                            strWhere += " id = " + strParamIDName + " ";
                        }

                        if (string.IsNullOrEmpty(strWhere) == false)
                        {
                            strCommand = " SELECT filename, newfilename FROM records WHERE " + strWhere + " \n";
                            command.CommandText = strCommand;

                            using (SQLiteDataReader dr = command.ExecuteReader())
                            {
                                if (dr.HasRows == true)
                                {
                                    while (dr.Read())
                                    {
                                        if (dr.IsDBNull(0) == false)
                                            filenames.Add(dr.GetString(0));
                                        if (dr.IsDBNull(1) == false)
                                            filenames.Add(dr.GetString(1));
                                    }
                                }
                            }

                            command.Parameters.Clear();
                        }

                        // ����ɾ����records�е����
                        strCommand = "";
                        command.Parameters.Clear();
                        nCount = targetRight.Count;

                        for (int i = 0; i < targetRight.Count; i++)
                        {
                            string strPureObjectID = targetRight[i];
                            string strObjectID = strID + "_" + strPureObjectID;

                            string strParamIDName = "@id" + Convert.ToString(i);
                            strCommand += " DELETE FROM records WHERE id = " + strParamIDName + " ;\n";
                            SQLiteParameter idParam =
                                command.Parameters.Add(strParamIDName,
                                DbType.String);
                            idParam.Value = strObjectID;
                        }
                    }

                    // �������ļ�
                    if (targetLeft.Count > 0)
                    {
                        for (int i = 0; i < targetLeft.Count; i++)
                        {
                            string strPureObjectID = targetLeft[i];
                            string strObjectID = strID + "_" + strPureObjectID;

                            string strParamIDName = "@id" + Convert.ToString(i) + nCount;
                            strCommand += " INSERT INTO records(id) "
                                + " VALUES(" + strParamIDName + ") ;\n";
                            SQLiteParameter idParam =
                                command.Parameters.Add(strParamIDName,
                                DbType.String);
                            idParam.Value = strObjectID;
                        }
                    }

                    if (string.IsNullOrEmpty(strCommand) == false)
                    {
                        command.CommandText = strCommand;
                        command.CommandTimeout = 30 * 60; // 30����

                        int nResultCount = 0;
                        try
                        {
                            nResultCount = command.ExecuteNonQuery();
                        }
                        catch (SQLiteException ex)
                        {
                            if (ex.ResultCode == SQLiteErrorCode.Constraint)    // ex.ErrorCode 2015/4/19
                            {
                                // ������Ѿ����ڣ����ã���Ҫ����
                                goto DELETE_OBJECTFILE;
                            }
                            else
                            {
                                strError = "�����¼·��Ϊ'" + this.GetCaption("zh") + "/" + strID + "'�����ļ���������:" + ex.Message + ",sql����:\r\n" + strCommand;
                                return -1;
                            }
                        }
                        catch (Exception ex)
                        {
                            strError = "�����¼·��Ϊ'" + this.GetCaption("zh") + "/" + strID + "'�����ļ���������:" + ex.Message + ",sql����:\r\n" + strCommand;
                            return -1;
                        }

                        if (nResultCount != targetRight.Count + targetLeft.Count)
                        {
                            this.container.KernelApplication.WriteErrorLog("ϣ��������ļ���'" + Convert.ToString(targetRight.Count + targetLeft.Count) + "'����ʵ��ɾ�����ļ���'" + Convert.ToString(nResultCount) + "'��");
                        }
                    }
                } // end of using command
            }
            #endregion // SQLite

            #region MySql
            else if (connection.SqlServerType == SqlServerType.MySql)
            {
                string strCommand = "";
                using (MySqlCommand command = new MySqlCommand("",
                    connection.MySqlConnection))
                {
                    int nCount = 0;
                    // ɾ�����ļ�
                    if (targetRight.Count > 0)
                    {
                        // ��úʹ�����ļ���
                        string strWhere = "";
                        for (int i = 0; i < targetRight.Count; i++)
                        {
                            string strPureObjectID = targetRight[i];
                            string strObjectID = strID + "_" + strPureObjectID;
                            string strParamIDName = "@id" + Convert.ToString(i);

                            // ׼���ò���
                            MySqlParameter idParam =
                                command.Parameters.Add(strParamIDName,
                                MySqlDbType.String);
                            idParam.Value = strObjectID;

                            if (string.IsNullOrEmpty(strWhere) == false)
                                strWhere += " OR ";
                            strWhere += " id = " + strParamIDName + " ";
                        }

                        if (string.IsNullOrEmpty(strWhere) == false)
                        {
                            strCommand = " SELECT filename, newfilename FROM records WHERE " + strWhere + " \n";
                            strCommand = "use `" + this.m_strSqlDbName + "` ;\n"
                                + strCommand;
                            command.CommandText = strCommand;

                            using (MySqlDataReader dr = command.ExecuteReader())
                            {
                                if (dr.HasRows == true)
                                {
                                    while (dr.Read())
                                    {
                                        if (dr.IsDBNull(0) == false)
                                            filenames.Add(dr.GetString(0));
                                        if (dr.IsDBNull(1) == false)
                                            filenames.Add(dr.GetString(1));
                                    }
                                }
                            }

                            command.Parameters.Clear();
                        }

                        // ����ɾ����records�е����
                        strCommand = "";
                        command.Parameters.Clear();
                        nCount = targetRight.Count;

                        for (int i = 0; i < targetRight.Count; i++)
                        {
                            string strPureObjectID = targetRight[i];
                            string strObjectID = strID + "_" + strPureObjectID;

                            string strParamIDName = "@id" + Convert.ToString(i);
                            strCommand += " DELETE FROM records WHERE id = " + strParamIDName + " ;\n";
                            MySqlParameter idParam =
                                command.Parameters.Add(strParamIDName,
                                MySqlDbType.String);
                            idParam.Value = strObjectID;
                        }
                    }

                    // �������ļ�
                    if (targetLeft.Count > 0)
                    {
                        for (int i = 0; i < targetLeft.Count; i++)
                        {
                            string strPureObjectID = targetLeft[i];
                            string strObjectID = strID + "_" + strPureObjectID;

                            string strParamIDName = "@id" + Convert.ToString(i) + nCount;
                            strCommand += " INSERT INTO records (id) "
                                + " VALUES (" + strParamIDName + ") ;\n";
                            MySqlParameter idParam =
                                command.Parameters.Add(strParamIDName,
                                MySqlDbType.String);
                            idParam.Value = strObjectID;
                        }
                    }

                    if (strCommand != "")
                    {
                        strCommand = "use `" + this.m_strSqlDbName + "` ;\n"
                            + strCommand;

                        command.CommandText = strCommand;
                        command.CommandTimeout = 30 * 60; // 30����

                        int nResultCount = 0;
                        try
                        {
                            nResultCount = command.ExecuteNonQuery();
                        }
                        catch (MySqlException ex)
                        {
                            if (ex.Number == 1062)
                            {
                                // ������Ѿ����ڣ����ã���Ҫ����
                                goto DELETE_OBJECTFILE;
                            }
                            else
                            {
                                strError = "�����¼·��Ϊ'" + this.GetCaption("zh") + "/" + strID + "'�����ļ���������:" + ex.Message + ",sql����:\r\n" + strCommand;
                                return -1;
                            }
                        }
                        catch (Exception ex)
                        {
                            strError = "�����¼·��Ϊ'" + this.GetCaption("zh") + "/" + strID + "'�����ļ���������:" + ex.Message + ",sql����:\r\n" + strCommand;
                            return -1;
                        }

                        if (nResultCount != targetRight.Count + targetLeft.Count)
                        {
                            this.container.KernelApplication.WriteErrorLog("ϣ��������ļ���'" + Convert.ToString(targetRight.Count + targetLeft.Count) + "'����ʵ��ɾ�����ļ���'" + Convert.ToString(nResultCount) + "'��");
                        }
                    }
                } // end of using command
            }
            #endregion // MySql

            #region Oracle
            else if (connection.SqlServerType == SqlServerType.Oracle)
            {
                string strCommand = "";
                using (OracleCommand command = new OracleCommand(strCommand, connection.OracleConnection))
                {
                    int nCount = 0;
                    // ɾ�����ļ�
                    if (targetRight.Count > 0)
                    {
                        nCount = targetRight.Count;

                        for (int i = 0; i < targetRight.Count; i++)
                        {
                            string strPureObjectID = targetRight[i];
                            string strObjectID = strID + "_" + strPureObjectID;
                            string strParamIDName = ":id" + Convert.ToString(i);

                            // ׼���ò���
                            OracleParameter idParam =
        command.Parameters.Add(strParamIDName,
        OracleDbType.NVarchar2);
                            idParam.Value = strObjectID;

                            // �г������ļ���
                            strCommand = " SELECT filename, newfilename FROM " + this.m_strSqlDbName + "_records WHERE id = " + strParamIDName + " \n";
                            command.CommandText = strCommand;

                            OracleDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                            try
                            {
                                if (dr != null
                                    && dr.HasRows == true)
                                {
                                    while (dr.Read())
                                    {
                                        if (dr.IsDBNull(0) == false)
                                            filenames.Add(dr.GetString(0));
                                        if (dr.IsDBNull(1) == false)
                                            filenames.Add(dr.GetString(1));
                                    }
                                }
                                else
                                    goto CONTINUE_1;    // ���id��records�в�����
                            }
                            finally
                            {
                                if (dr != null)
                                    dr.Close();
                            }

                            strCommand = " DELETE FROM " + this.m_strSqlDbName + "_records WHERE id = " + strParamIDName + " \n";
                            command.CommandText = strCommand;

                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                strError = "�����¼·��Ϊ '" + this.GetCaption("zh") + "/" + strID + "' �����ļ���������:" + ex.Message + ",sql����:\r\n" + strCommand;
                                return -1;
                            }
                        CONTINUE_1:
                            command.Parameters.Clear();
                        }
                    }

                    // �������ļ�
                    if (targetLeft.Count > 0)
                    {
                        for (int i = 0; i < targetLeft.Count; i++)
                        {
                            string strPureObjectID = targetLeft[i];
                            string strObjectID = strID + "_" + strPureObjectID;

                            string strParamIDName = ":id" + Convert.ToString(i) + nCount;
                            strCommand = " INSERT INTO " + this.m_strSqlDbName + "_records (id) "
                                + " VALUES (" + strParamIDName + ") \n";

                            command.CommandText = strCommand;
                            command.Parameters.Clear();

                            OracleParameter idParam =
                                command.Parameters.Add(strParamIDName,
                                OracleDbType.NVarchar2);
                            idParam.Value = strObjectID;

                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (OracleException ex)
                            {
                                if (ex.Errors.Count > 0 && ex.Errors[0].Number == 00001)
                                {
                                    // ������Ѿ����ڣ����ã���Ҫ����
                                }
                                else
                                {
                                    strError = "�����¼·��Ϊ '" + this.GetCaption("zh") + "/" + strID + "' ���Ӽ�¼ʱ��������:" + ex.Message + ", SQL����:\r\n" + strCommand;
                                    return -1;
                                }
                            }
                            catch (Exception ex)
                            {

                                strError = "�����¼·��Ϊ '" + this.GetCaption("zh") + "/" + strID + "' ���Ӽ�¼ʱ��������:" + ex.Message + ", SQL����:\r\n" + strCommand;
                                return -1;
                            }
                            command.Parameters.Clear();
                        }
                    }
                } // end of using command
            }
            #endregion // Oracle

        DELETE_OBJECTFILE:
            // ɾ�������ļ�
            foreach (string strShortFilename in filenames)
            {
                if (string.IsNullOrEmpty(strShortFilename) == true)
                    continue;

                string strFilename = this.GetObjectFileName(strShortFilename);
                try
                {
                    if (string.IsNullOrEmpty(strFilename) == false)
                        File.Delete(strFilename);
                }
                catch (Exception ex)
                {
                    strError = "ɾ�����ݿ� '" + this.GetCaption("zh-CN") + "' �� IDΪ '" + strID + "' �Ķ����ļ�ʱ��������: " + ex.Message;
                    this.container.KernelApplication.WriteErrorLog(strError);
                    return -1;
                }
            }

            return 0;
        }


        // �������Ӷ����Ƿ���ȷ
        // return:
        //      -1  ����
        //      0   ����
        private int CheckConnection(Connection connection,
            out string strError)
        {
            strError = "";
            if (connection == null)
            {
                strError = "connectionΪnull";
                return -1;
            }
            #region MS SQL Server
            if (connection.SqlServerType == SqlServerType.MsSqlServer)
            {
                if (connection.SqlConnection == null)
                {
                    strError = "connection.SqlConnectionΪnull";
                    return -1;
                }
                if (connection.SqlConnection.State != ConnectionState.Open)
                {
                    strError = "connectionû�д�";
                    return -1;
                }
                return 0;
            }
            #endregion // MS SQL Server

            #region SQLite
            if (connection.SqlServerType == SqlServerType.SQLite)
            {
                if (connection.SQLiteConnection == null)
                {
                    strError = "connection.SQLiteConnectionΪnull";
                    return -1;
                }
                if (connection.SQLiteConnection.State != ConnectionState.Open)
                {
                    strError = "connectionû�д�";
                    return -1;
                }
                return 0;
            }
            #endregion // SQLite

            #region MySql
            if (connection.SqlServerType == SqlServerType.MySql)
            {
                if (connection.MySqlConnection == null)
                {
                    strError = "connection.MySqlConnectionΪnull";
                    return -1;
                }
                if (connection.MySqlConnection.State != ConnectionState.Open)
                {
                    strError = "connectionû�д�";
                    return -1;
                }
                return 0;
            }
            #endregion // MySql

            #region Oracle
            if (connection.SqlServerType == SqlServerType.Oracle)
            {
                if (connection.OracleConnection == null)
                {
                    strError = "connection.OracleConnectionΪnull";
                    return -1;
                }

                if (connection.OracleConnection.State != ConnectionState.Open)
                {
                    strError = "connectionû�д�";
                    return -1;
                }
                return 0;
            }
            #endregion // Oracle

            return 0;
        }

        // �õ���Χ
        private string GetRange(SqlConnection connection,
            string strID)
        {
            string strRange = "";

            string strCommand = "use " + this.m_strSqlDbName + " "
                + "select range from records where id='" + strID + "'";

            strCommand += " use master " + "\n";

            SqlCommand command = new SqlCommand(strCommand,
                connection);
            SqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
            try
            {
                if (dr != null && dr.HasRows == true)
                {
                    dr.Read();
                    strRange = dr.GetString(0);
                    if (strRange == null)
                        strRange = "";
                }
            }
            finally
            {
                dr.Close();
            }

            return strRange;
        }


#if NO
        // ���¶���, ʹimage�ֶλ����Ч��TextPrtָ��
        // return
        //		-1  ����
        //		0   �ɹ�
        private int UpdateObject(SqlConnection connection,
            string strObjectID,
            out byte[] outputTimestamp,
            out string strError)
        {
            outputTimestamp = null;
            strError = "";

            // ������Ӷ���
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            string strCommand = "";
            SqlCommand command = null;

            string strOutputTimestamp = this.CreateTimestampForDb();

            strCommand = "use " + this.m_strSqlDbName + " "
                + " UPDATE records "
                + " set newdata=0x0,range='0-0',dptimestamp=@dptimestamp,metadata=@metadata "
                + " where id='" + strObjectID + "'";

            strCommand += " use master " + "\n";

            command = new SqlCommand(strCommand,
                connection);

            string strMetadata = "<file size='0'/>";
            SqlParameter metadataParam =
                command.Parameters.Add("@metadata",
                SqlDbType.NVarChar);
            metadataParam.Value = strMetadata;


            SqlParameter dptimestampParam =
                command.Parameters.Add("@dptimestamp",
                SqlDbType.NVarChar,
                100);
            dptimestampParam.Value = strOutputTimestamp;

            int nCount = command.ExecuteNonQuery();
            if (nCount <= 0)
            {
                strError = "û�и���'" + strObjectID + "'��¼";
                return -1;
            }
            // ���ص�ʱ���
            outputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);//Encoding.UTF8.GetBytes(strOutputTimestamp);
            return 0;
        }


        // �ж�һ���Զ�����Դ���ǿն���
        private bool IsEmptyObject(SqlConnection connection,
            string strID)
        {
            return this.IsEmptyObject(connection,
                "newdata",
                strID);
        }

        // �ж�һ���Զ�����Դ���ǿն���
        private bool IsEmptyObject(SqlConnection connection,
            string strImageFieldName,
            string strID)
        {
            string strError = "";
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                throw (new Exception(strError));

            string strCommand = "";
            SqlCommand command = null;
            strCommand = "use " + this.m_strSqlDbName + " "
                + " SELECT @Pointer=TEXTPTR(" + strImageFieldName + ") "
                + " FROM records "
                + " WHERE id=@id";

            strCommand += " use master " + "\n";

            command = new SqlCommand(strCommand,
                connection);
            SqlParameter idParam =
                command.Parameters.Add("@id",
                SqlDbType.NVarChar);
            idParam.Value = strID;

            SqlParameter PointerOutParam =
                command.Parameters.Add("@Pointer",
                SqlDbType.VarBinary,
                100);
            PointerOutParam.Direction = ParameterDirection.Output;
            command.ExecuteNonQuery();
            if (PointerOutParam == null
                || PointerOutParam.Value is System.DBNull)
            {
                return true;
            }
            return false;
        }


        // ����һ���¼�¼,ʹ������Ч��textptr,��װInsertRecord
        private int InsertRecord(SqlConnection connection,
            string strID,
            out byte[] outputTimestamp,
            out string strError)
        {
            return this.InsertRecord(connection,
                strID,
                "newdata",
                new byte[] { 0x0 },
                out outputTimestamp,
                out strError);
        }

        // �����в���һ����¼
        private int InsertRecord(SqlConnection connection,
            string strID,
            string strImageFieldName,
            byte[] sourceBuffer,
            out byte[] outputTimestamp,
            out string strError)
        {
            outputTimestamp = null;
            strError = "";

            // ������Ӷ���
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            string strCommand = "";
            SqlCommand command = null;

            string strRange = "0-" + Convert.ToString(sourceBuffer.Length - 1);
            string strOutputTimestamp = this.CreateTimestampForDb();

            strCommand = "use " + this.m_strSqlDbName + " "
                + " INSERT INTO records(id," + strImageFieldName + ",range,metadata,dptimestamp) "
                + " VALUES(@id,@data,@range,@metadata,@dptimestamp);";

            strCommand += " use master " + "\n";

            command = new SqlCommand(strCommand,
                connection);

            SqlParameter idParam =
                command.Parameters.Add("@id",
                SqlDbType.NVarChar);
            idParam.Value = strID;

            SqlParameter dataParam =
                command.Parameters.Add("@data",
                SqlDbType.Binary,
                sourceBuffer.Length);
            dataParam.Value = sourceBuffer;

            SqlParameter rangeParam =
                command.Parameters.Add("@range",
                SqlDbType.NVarChar);
            rangeParam.Value = strRange;

            string strMetadata = "<file size='0'/>";
            SqlParameter metadataParam =
                command.Parameters.Add("@metadata",
                SqlDbType.NVarChar);
            metadataParam.Value = strMetadata;

            SqlParameter dptimestampParam =
                command.Parameters.Add("@dptimestamp",
                SqlDbType.NVarChar,
                100);
            dptimestampParam.Value = strOutputTimestamp;

            int nCount = command.ExecuteNonQuery();
            if (nCount <= 0)
            {
                strError = "InsertImage() SQL����ִ��Ӱ�������Ϊ" + Convert.ToString(nCount);
                return -1;
            }

            // ���ص�ʱ���
            outputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);//Encoding.UTF8.GetBytes(strOutputTimestamp);
            return 0;
        }

#endif

#if NO
        // ��newdata�ֶ��滻data�ֶ�
        // parameters:
        //      connection  SqlConnection����
        //      strID       ��¼id
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      >=0   �ɹ� ����Ӱ��ļ�¼��
        // ��: ����ȫ
        private int UpdateDataField(SqlConnection connection,
            string strID,
            out string strError)
        {
            strError = "";
            // ������Ӷ���
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            SqlConnection new_connection = null;
            if (connection.ConnectionTimeout < m_nLongTimeout)
            {
                new_connection = new SqlConnection(this.m_strLongConnString);
                new_connection.Open();
                connection = new_connection;
            }

            try
            {
                string strCommand = "use " + this.m_strSqlDbName + " "
                    + " UPDATE records \n"
                    + " SET data=newdata \n"
                    + " WHERE id='" + strID + "'";
                strCommand += " use master " + "\n";

                SqlCommand command = new SqlCommand(strCommand,
                    connection);
                command.CommandTimeout = m_nLongTimeout;  // 30����

                int nCount = command.ExecuteNonQuery();
                if (nCount == -1)
                {
                    strError = "û���滻���ü�¼'" + strID + "'��data�ֶ�";
                    return -1;
                }

                return nCount;
            }
            finally
            {
                if (new_connection != null)
                    new_connection.Close();
            }

        }
#endif

        long GetObjectFileLength(string strID,
            bool bTempObject)
        {
            string strFileName = BuildObjectFileName(strID, bTempObject);

            FileInfo fi = new FileInfo(strFileName);
            if (fi.Exists == false)
                return 0;

            return fi.Length;
        }

        // ��ó��δ����Ķ����ļ���
        string BuildObjectFileName(string strID,
bool bTempObject)
        {
            if (string.IsNullOrEmpty(this.m_strObjectDir) == true)
                return null;

            Debug.Assert(strID.Length >= 10, "");

            if (bTempObject == true)
                return PathUtil.MergePath(this.m_strObjectDir, strID.Insert(7, "/") + ".temp");
            else
                return PathUtil.MergePath(this.m_strObjectDir, strID.Insert(7, "/"));
        }

        // �����ֶ����ݹ��������Ķ����ļ���
        // parameters:
        //      strShotFileName filename��newfilename�ֶ��д洢�Ķ��ļ�����������������Ŀ¼�µ���Ŀ¼���ļ�������
        string GetObjectFileName(string strShortFileName)
        {
            if (string.IsNullOrEmpty(this.m_strObjectDir) == true)
                return null;

            if (string.IsNullOrEmpty(strShortFileName) == true)
                return null;

            return PathUtil.MergePath(this.m_strObjectDir, strShortFileName);
        }

        // ������ʺϱ�����filename��newfilename�ֶ��еĶ��ļ���
        string GetShortFileName(string strLongFileName)
        {
            if (string.IsNullOrEmpty(this.m_strObjectDir) == true)
                return null;

            // ���滯Ŀ¼·�������������ַ�'/'�滻Ϊ'\'������Ϊĩβȷ�����ַ�'\'
            string strObjectDir = PathUtil.CanonicalizeDirectoryPath(this.m_strObjectDir);

            if (strLongFileName.Length <= strObjectDir.Length)
                return null;

            return strLongFileName.Substring(strObjectDir.Length);
        }

        /*
        int DeleteObjectFile(string strID,
            bool bTempObject)
        {
            string strFileName = "";
            
            if (bTempObject == true)
                strFileName = PathUtil.MergePath(this.m_strObjectDir, strID + ".temp");
            else
                strFileName = PathUtil.MergePath(this.m_strObjectDir, strID);

            File.Delete(strFileName);

            return 1;
        }
         * */


#if NO
        // TODO: ҲҪ�����Ӧ��timestamp�ֶ�
        // ɾ��imageȫ������
        // parameter:
        //		connection  ���Ӷ���
        //		strID       ��¼ID
        //		strImageFieldName   image�ֶ���
        //		strError    out���������س�����Ϣ
        // return:
        //		-1  ����
        //		0   �ɹ�
        // ��: ����ȫ
        private int RemoveImage(SqlConnection connection,
            // string strID,
            string strImageFieldName,
            byte [] textptr,
            out string strError)
        {
            strError = "";

            Debug.Assert(textptr != null, "");

            // ������Ӷ���
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            SqlConnection new_connection = null;
            if (connection.ConnectionTimeout < m_nLongTimeout)
            {
                new_connection = new SqlConnection(this.m_strLongConnString);
                new_connection.Open();
                connection = new_connection;
            }


            try
            {
                string strCommand = "";
                SqlCommand command = null;

                strCommand = "use " + this.m_strSqlDbName + " "
                    + " UPDATETEXT records." + strImageFieldName
                    + " @dest_text_ptr"
                    + " @insert_offset"
                    + " NULL"  //@delete_length"
#if UPDATETEXT_WITHLOG
                    + " WITH LOG";
#endif
        //+ " @inserted_data";   //���ܼ�where���

                strCommand += " use master " + "\n";

                command = new SqlCommand(strCommand,
                    connection);
                command.CommandTimeout = m_nLongTimeout;  // 30���� 2011/1/16

                // ��������ֵ
                SqlParameter dest_text_ptrParam =
                    command.Parameters.Add("@dest_text_ptr",
                    SqlDbType.Binary,
                    16);

                SqlParameter insert_offsetParam =
                    command.Parameters.Add("@insert_offset",
                    SqlDbType.Int); // old Int

                dest_text_ptrParam.Value = textptr;
                insert_offsetParam.Value = 0;

                command.ExecuteNonQuery();

                return 0;
            }
            finally
            {
                if (new_connection != null)
                    new_connection.Close();
            }
        }
#endif

#if NO
        // ɾ��image����Ĳ���
        // parameter:
        //		connection  ���Ӷ���
        //		strID       ��¼ID
        //		strImageFieldName   image�ֶ���
        //		nStart      ��ʼλ��
        //		strError    out���������س�����Ϣ
        // return:
        //		-1  ����
        //		0   �ɹ�
        // ��: ����ȫ
        private int DeleteDuoYuImage(SqlConnection connection,
            string strID,
            string strImageFieldName,
            long lStart,
            out string strError)
        {
            strError = "";

            // ������Ӷ���
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            SqlConnection new_connection = null;
            if (connection.ConnectionTimeout < m_nLongTimeout)
            {
                new_connection = new SqlConnection(this.m_strLongConnString);
                new_connection.Open();
                connection = new_connection;
            }


            try
            {
                string strCommand = "";
                SqlCommand command = null;

                // 1.�õ�imageָ�� �� ����
                strCommand = "use " + this.m_strSqlDbName + " "
                    + " SELECT @Pointer=TEXTPTR(" + strImageFieldName + "),"
                    + " @Length=DataLength(" + strImageFieldName + ") "
                    + " FROM records "
                    + " WHERE id=@id";

                strCommand += " use master " + "\n";

                command = new SqlCommand(strCommand,
                    connection);
                command.CommandTimeout = m_nLongTimeout;  // 30����

                SqlParameter idParam =
                    command.Parameters.Add("@id",
                    SqlDbType.NVarChar);
                idParam.Value = strID;

                SqlParameter PointerOutParam =
                    command.Parameters.Add("@Pointer",
                    SqlDbType.VarBinary,
                    100);
                PointerOutParam.Direction = ParameterDirection.Output;

                SqlParameter LengthOutParam =
                    command.Parameters.Add("@Length",
                    SqlDbType.Int);  // old Int
                LengthOutParam.Direction = ParameterDirection.Output;

                command.ExecuteNonQuery();
                if (PointerOutParam == null)
                {
                    strError = "û�ҵ�imageָ��";
                    return -1;
                }

                long lTotalLength = (int)LengthOutParam.Value;
                if (lStart >= lTotalLength)
                    return 0;


                // 2.����ɾ��
                strCommand = "use " + this.m_strSqlDbName + " "
                    + " UPDATETEXT records." + strImageFieldName
                    + " @dest_text_ptr"
                    + " @insert_offset"
                    + " NULL"  //@delete_length"
#if UPDATETEXT_WITHLOG
                    + " WITH LOG";
#endif
        //+ " @inserted_data";   //���ܼ�where���

                strCommand += " use master " + "\n";

                command = new SqlCommand(strCommand,
                    connection);
                command.CommandTimeout = m_nLongTimeout;  // 30���� 2011/1/16

                // ��������ֵ
                SqlParameter dest_text_ptrParam =
                    command.Parameters.Add("@dest_text_ptr",
                    SqlDbType.Binary,
                    16);

                SqlParameter insert_offsetParam =
                    command.Parameters.Add("@insert_offset",
                    SqlDbType.Int); // old Int

                dest_text_ptrParam.Value = PointerOutParam.Value;
                insert_offsetParam.Value = lStart;

                command.ExecuteNonQuery();

                return 0;
            }
            finally
            {
                if (new_connection != null)
                    new_connection.Close();
            }
        }
#endif

#if NO
        // ����¼�ڿ����Ƿ����
        // return:
        //		-1  ����
        //      0   ������
        //      1   ����
        private int RecordIsExist(SqlConnection connection,
            string strID,
            out string strError)
        {
            strError = "";

            // ������Ӷ���
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            string strCommand = "use " + this.m_strSqlDbName + " "
                + " SET NOCOUNT OFF;"
                + "select id from records where id='" + strID + "'";
            strCommand += " use master " + "\n";

            SqlCommand command = new SqlCommand(strCommand,
                connection);
            SqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
            try
            {
                if (dr != null && dr.HasRows == true)
                    return 1;
            }
            finally
            {
                dr.Close();
            }
            return 0;
        }
#endif
        public class RecordRowInfo
        {
            public byte[] data_textptr = null;
            public long data_length = 0;

            public byte[] newdata_textptr = null;
            public long newdata_length = 0;

            public string TimestampString = "";
            public string NewTimestampString = "";  // 2012/1/19

            public string Metadata = "";
            public string Range = "";

            public string FileName = "";
            public string NewFileName = "";

            public string ID = "";                  // 2013/2/17
            public byte[] Data = null;              // 2013/2/17
            public byte[] NewData = null;           // 2013/2/17
        }

        // ����¼�ڿ����Ƿ���ڣ�����������򷵻�һЩ�ֶ����ݣ���������������һ���¼�¼
        // return:
        //		-1  ����
        //      0   û�д����¼�¼
        //      1   �������µļ�¼
        //      2   ��Ҫ�����µļ�¼������Ϊ�Ż���Ե��(�Ժ���Ҫ����)��û�д���
        private int CreateNewRecordIfNeed(Connection connection,
            string strID,
            byte [] sourceBuffer,
            out RecordRowInfo row_info,
            out string strError)
        {
            strError = "";
            row_info = null;

            // 2013/2/17
            if (StringUtil.IsPureNumber(strID) == false)
            {
                strError = "ID '"+strID+"' �����Ǵ�����";
                return -1;
            }

            // ������Ӷ���
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            if (connection.SqlServerType == SqlServerType.MsSqlServer)
            {
                string strSelect = " SELECT TEXTPTR(data)," // 0
                    + " DataLength(data),"  // 1
                    + " TEXTPTR(newdata),"  // 2
                    + " DataLength(newdata),"   // 3
                    + " range," // 4
                    + " dptimestamp,"   // 5
                    + " metadata, "  // 6
                    + " newdptimestamp,"   // 7
                    + " filename,"   // 8
                    + " newfilename"   // 9
                    + " FROM records "
                    + " WHERE id='" + strID + "'\n";


                string strCommand = "use " + this.m_strSqlDbName + " \n"
                    + "SET NOCOUNT OFF\n"
                    + strSelect
                    + "if @@ROWCOUNT = 0\n"
                    + "begin\n"
                    + " INSERT INTO records(id, data, range, metadata, dptimestamp, newdptimestamp) "
                    + " VALUES(@id, @data, @range, @metadata, @dptimestamp, @newdptimestamp)"
                    + "end\n";
                strCommand += " use master " + "\n";

                using (SqlCommand command = new SqlCommand(strCommand,
                    connection.SqlConnection))
                {

                    if (sourceBuffer == null)
                        sourceBuffer = new byte[] { 0x0 };

                    row_info = new RecordRowInfo();
                    row_info.data_textptr = null;
                    row_info.data_length = sourceBuffer.Length;
                    row_info.newdata_textptr = null;
                    row_info.newdata_length = 0;
                    // row_info.Range = "0-" + Convert.ToString(sourceBuffer.Length - 1);
                    row_info.Range = "";
                    row_info.TimestampString = "";    // this.CreateTimestampForDb();
                    row_info.NewTimestampString = "";
                    row_info.Metadata = "<file size='0'/>";
                    row_info.FileName = "";
                    row_info.NewFileName = "";

                    SqlParameter idParam =
            command.Parameters.Add("@id",
            SqlDbType.NVarChar);
                    idParam.Value = strID;

                    SqlParameter dataParam =
                        command.Parameters.Add("@data",
                        SqlDbType.Binary,
                        sourceBuffer.Length);
                    dataParam.Value = sourceBuffer;

                    SqlParameter rangeParam =
                        command.Parameters.Add("@range",
                        SqlDbType.NVarChar);
                    rangeParam.Value = row_info.Range;

                    SqlParameter metadataParam =
                        command.Parameters.Add("@metadata",
                        SqlDbType.NVarChar);
                    metadataParam.Value = row_info.Metadata;

                    SqlParameter dptimestampParam =
                        command.Parameters.Add("@dptimestamp",
                        SqlDbType.NVarChar,
                        100);
                    dptimestampParam.Value = row_info.TimestampString;

                    SqlParameter newdptimestampParam =
            command.Parameters.Add("@newdptimestamp",
            SqlDbType.NVarChar,
            100);
                    newdptimestampParam.Value = row_info.NewTimestampString;


                    SqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        // 1.��¼�����ڱ���
                        if (dr == null
                            || dr.HasRows == false)
                        {
                            // strError = "��¼ '" + strID + "' �ڿ��в����ڣ���������²�Ӧ������";
                            return 1;   // �Ѿ������¼�¼
                        }

                        dr.Read();

                        row_info = new RecordRowInfo();

                        /*
                        // 2.textPtrΪnull����
                        if (dr[0] is System.DBNull)
                        {
                            strError = "TextPtr������Ϊnull";
                            return -1;
                        }
                         * */

                        if (dr.IsDBNull(0) == false)
                            row_info.data_textptr = (byte[])dr[0];

                        if (dr.IsDBNull(1) == false)
                            row_info.data_length = dr.GetInt32(1);

                        if (dr.IsDBNull(2) == false)
                            row_info.newdata_textptr = (byte[])dr[2];

                        if (dr.IsDBNull(3) == false)
                            row_info.newdata_length = dr.GetInt32(3);

                        if (dr.IsDBNull(4) == false)
                            row_info.Range = dr.GetString(4);

                        if (dr.IsDBNull(5) == false)
                            row_info.TimestampString = dr.GetString(5);

                        if (dr.IsDBNull(6) == false)
                            row_info.Metadata = dr.GetString(6);

                        if (dr.IsDBNull(7) == false)
                            row_info.NewTimestampString = dr.GetString(7);

                        if (dr.IsDBNull(8) == false)
                            row_info.FileName = dr.GetString(8);

                        if (dr.IsDBNull(9) == false)
                            row_info.NewFileName = dr.GetString(9);

                        bool bRet = dr.Read();

                        if (bRet == true)
                        {
                            // ����һ��
                            strError = "��¼ '" + strID + "' ��SQL��" + this.m_strSqlDbName + "��records���д��ڶ���������һ�ֲ�������״̬, ��ϵͳ����Ա����SQL����ɾ������ļ�¼��";
                            return -1;
                        }
                    }
                    finally
                    {
                        dr.Close();
                    }
                } // end of using command

                return 0;
            }
            else if (connection.SqlServerType == SqlServerType.SQLite)
            {
                string strCommand = " SELECT "
                    + " range," // 0 4
                    + " dptimestamp,"   // 1 5
                    + " metadata, "  // 2 6
                    + " newdptimestamp,"   // 3 7
                    + " filename,"   // 4 8
                    + " newfilename"   // 5 9
                    + " FROM records "
                    + " WHERE id='" + strID + "'\n";

                using (SQLiteCommand command = new SQLiteCommand(strCommand,
                    connection.SQLiteConnection))
                {

                    SQLiteDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        // �����¼�����ڣ���Ҫ����
                        if (dr == null
                            || dr.HasRows == false)
                        {
                            row_info = new RecordRowInfo();
                            row_info.Range = "";
                            row_info.TimestampString = "";
                            row_info.NewTimestampString = "";
                            row_info.Metadata = "<file size='0'/>";
                            row_info.FileName = "";
                            row_info.NewFileName = "";
                            return 2;
                            // goto DO_CREATE;
                        }

                        // �����¼�Ѿ�����
                        dr.Read();

                        row_info = new RecordRowInfo();

                        /*
                        if (dr.IsDBNull(0) == false)
                            row_info.data_textptr = (byte[])dr[0];

                        if (dr.IsDBNull(1) == false)
                            row_info.data_length = dr.GetInt32(1);

                        if (dr.IsDBNull(2) == false)
                            row_info.newdata_textptr = (byte[])dr[2];

                        if (dr.IsDBNull(3) == false)
                            row_info.newdata_length = dr.GetInt32(3);
                         * */

                        if (dr.IsDBNull(0) == false)
                            row_info.Range = dr.GetString(0);

                        if (dr.IsDBNull(1) == false)
                            row_info.TimestampString = dr.GetString(1);

                        if (dr.IsDBNull(2) == false)
                            row_info.Metadata = dr.GetString(2);

                        if (dr.IsDBNull(3) == false)
                            row_info.NewTimestampString = dr.GetString(3);

                        if (dr.IsDBNull(4) == false)
                            row_info.FileName = dr.GetString(4);

                        if (dr.IsDBNull(5) == false)
                            row_info.NewFileName = dr.GetString(5);

                        bool bRet = dr.Read();

                        if (bRet == true)
                        {
                            // ����һ��
                            strError = "��¼ '" + strID + "' ��SQL��" + this.m_strSqlDbName + "��records���д��ڶ���������һ�ֲ�������״̬, ��ϵͳ����Ա����SQL����ɾ������ļ�¼��";
                            return -1;
                        }
                    }
                    finally
                    {
                        dr.Close();
                    }
                } // end of using command

                return 0;   // û�д����¼�¼

#if NO
            DO_CREATE:
                if (sourceBuffer == null)
                    sourceBuffer = new byte[] { 0x0 };

                row_info = new RecordRowInfo();
                /*
                row_info.data_textptr = null;
                row_info.data_length = sourceBuffer.Length;
                row_info.newdata_textptr = null;
                row_info.newdata_length = 0;
                 * */

                row_info.Range = "";
                row_info.TimestampString = "";    // this.CreateTimestampForDb();
                row_info.NewTimestampString = "";
                row_info.Metadata = "<file size='0'/>";
                row_info.FileName = "";
                row_info.NewFileName = "";

                strCommand = " INSERT INTO records(id, range, metadata, dptimestamp, newdptimestamp) "
    + " VALUES(@id, @range, @metadata, @dptimestamp, @newdptimestamp)";
                command = new SQLiteCommand(strCommand,
    connection.SQLiteConnection);

                SQLiteParameter idParam =
        command.Parameters.Add("@id",
        DbType.String);
                idParam.Value = strID;

                /*
                SqlParameter dataParam =
                    command.Parameters.Add("@data",
                    SqlDbType.Binary,
                    sourceBuffer.Length);
                dataParam.Value = sourceBuffer;
                 * */

                SQLiteParameter rangeParam =
                    command.Parameters.Add("@range",
                    DbType.String);
                rangeParam.Value = row_info.Range;

                SQLiteParameter metadataParam =
                    command.Parameters.Add("@metadata",
                    DbType.String);
                metadataParam.Value = row_info.Metadata;

                SQLiteParameter dptimestampParam =
                    command.Parameters.Add("@dptimestamp",
                    DbType.String,
                    100);
                dptimestampParam.Value = row_info.TimestampString;

                SQLiteParameter newdptimestampParam =
                    command.Parameters.Add("@newdptimestamp",
                    DbType.String,
                    100);
                newdptimestampParam.Value = row_info.NewTimestampString;

                command.ExecuteNonQuery();
                return 1;
#endif
            }
            else if (connection.SqlServerType == SqlServerType.MySql)
            {
                // ע�� MySql ����� SQLite ����һ��
                string strCommand = " SELECT "
                    + " `range`," // 0 4
                    + " dptimestamp,"   // 1 5
                    + " metadata, "  // 2 6
                    + " newdptimestamp,"   // 3 7
                    + " filename,"   // 4 8
                    + " newfilename"   // 5 9
                    + " FROM `" + this.m_strSqlDbName + "`.records "
                    + " WHERE id='" + strID + "'\n";

                using (MySqlCommand command = new MySqlCommand(strCommand,
                    connection.MySqlConnection))
                {

                    MySqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        // �����¼�����ڣ���Ҫ����
                        if (dr == null
                            || dr.HasRows == false)
                        {
                            row_info = new RecordRowInfo();
                            row_info.Range = "";
                            row_info.TimestampString = "";
                            row_info.NewTimestampString = "";
                            row_info.Metadata = "<file size='0'/>";
                            row_info.FileName = "";
                            row_info.NewFileName = "";
                            return 2;
                            // goto DO_CREATE;
                        }

                        // �����¼�Ѿ�����
                        dr.Read();

                        row_info = new RecordRowInfo();

                        if (dr.IsDBNull(0) == false)
                            row_info.Range = dr.GetString(0);

                        if (dr.IsDBNull(1) == false)
                            row_info.TimestampString = dr.GetString(1);

                        if (dr.IsDBNull(2) == false)
                            row_info.Metadata = dr.GetString(2);

                        if (dr.IsDBNull(3) == false)
                            row_info.NewTimestampString = dr.GetString(3);

                        if (dr.IsDBNull(4) == false)
                            row_info.FileName = dr.GetString(4);

                        if (dr.IsDBNull(5) == false)
                            row_info.NewFileName = dr.GetString(5);

                        bool bRet = dr.Read();

                        if (bRet == true)
                        {
                            // ����һ��
                            strError = "��¼ '" + strID + "' ��SQL��" + this.m_strSqlDbName + "��records���д��ڶ���������һ�ֲ�������״̬, ��ϵͳ����Ա����SQL����ɾ������ļ�¼��";
                            return -1;
                        }
                    }
                    finally
                    {
                        dr.Close();
                    }
                } // end of using command

                return 0;   // û�д����¼�¼
            }
            else if (connection.SqlServerType == SqlServerType.Oracle)
            {
                // ע�� MySql ����� SQLite ����һ��
                string strCommand = " SELECT "
                    + " range," // 0 4
                    + " dptimestamp,"   // 1 5
                    + " metadata, "  // 2 6
                    + " newdptimestamp,"   // 3 7
                    + " filename,"   // 4 8
                    + " newfilename"   // 5 9
                    + " FROM " + this.m_strSqlDbName + "_records "
                    + " WHERE id='" + strID + "'\n";

                using (OracleCommand command = new OracleCommand(strCommand,
                    connection.OracleConnection))
                {

                    OracleDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        // �����¼�����ڣ���Ҫ����
                        if (dr == null
                            || dr.HasRows == false)
                        {
                            row_info = new RecordRowInfo();
                            row_info.Range = "";
                            row_info.TimestampString = "";
                            row_info.NewTimestampString = "";
                            row_info.Metadata = "<file size='0'/>";
                            row_info.FileName = "";
                            row_info.NewFileName = "";
                            return 2;
                            // goto DO_CREATE;
                        }

                        // �����¼�Ѿ�����
                        dr.Read();

                        row_info = new RecordRowInfo();

                        if (dr.IsDBNull(0) == false)
                            row_info.Range = dr.GetString(0);

                        if (dr.IsDBNull(1) == false)
                            row_info.TimestampString = dr.GetString(1);

                        if (dr.IsDBNull(2) == false)
                            row_info.Metadata = dr.GetString(2);

                        if (dr.IsDBNull(3) == false)
                            row_info.NewTimestampString = dr.GetString(3);

                        if (dr.IsDBNull(4) == false)
                            row_info.FileName = dr.GetString(4);

                        if (dr.IsDBNull(5) == false)
                            row_info.NewFileName = dr.GetString(5);

                        bool bRet = dr.Read();

                        if (bRet == true)
                        {
                            // ����һ��
                            strError = "��¼ '" + strID + "' ��SQL��" + this.m_strSqlDbName + "��records���д��ڶ���������һ�ֲ�������״̬, ��ϵͳ����Ա����SQL����ɾ������ļ�¼��";
                            return -1;
                        }
                    }
                    finally
                    {
                        if (dr != null)
                            dr.Close();
                    }
                } // end of using command

                return 0;   // û�д����¼�¼
            }
            return 0;   // û�д����¼�¼
        }

        // return:
        //      -1  ����
        //      0   ��¼������
        //      1   �ɹ�
        private int GetRowInfo(Connection connection,
    string strID,
    out RecordRowInfo row_info,
    out string strError)
        {
            strError = "";
            row_info = null;

            // ������Ӷ���
            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            if (connection.SqlServerType == SqlServerType.MsSqlServer)
            {
                string strSelect = " SELECT TEXTPTR(data)," // 0
                    + " DataLength(data),"  // 1
                    + " TEXTPTR(newdata),"  // 2
                    + " DataLength(newdata),"   // 3
                    + " range," // 4
                    + " dptimestamp,"   // 5
                    + " metadata,"  // 6
                    + " newdptimestamp, "   // 7
                    + " filename, "   // 8
                    + " newfilename "   // 9
                    + " FROM records "
                    + " WHERE id='" + strID + "'\n";

                string strCommand = "use " + this.m_strSqlDbName + " \n"
                    + "SET NOCOUNT OFF\n"
                    + strSelect;
                strCommand += " use master " + "\n";

                using (SqlCommand command = new SqlCommand(strCommand,
                    connection.SqlConnection))
                {

                    SqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        // 1.��¼�����ڱ���
                        if (dr == null
                            || dr.HasRows == false)
                        {
                            strError = "��¼ '" + strID + "' �ڿ��в�����";
                            return 0;
                        }

                        dr.Read();

                        row_info = new RecordRowInfo();

                        /*
                        // 2.textPtrΪnull����
                        if (dr[0] is System.DBNull)
                        {
                            strError = "TextPtr������Ϊnull";
                            return -1;
                        }
                         * */

                        row_info.data_textptr = (byte[])GetValue(dr[0]);

                        if (dr.IsDBNull(1) == false)
                            row_info.data_length = dr.GetInt32(1);

                        row_info.newdata_textptr = (byte[])GetValue(dr[2]);

                        if (dr.IsDBNull(3) == false)
                            row_info.newdata_length = dr.GetInt32(3);

                        if (dr.IsDBNull(4) == false)
                            row_info.Range = dr.GetString(4);

                        if (dr.IsDBNull(5) == false)
                            row_info.TimestampString = dr.GetString(5);

                        if (dr.IsDBNull(6) == false)
                            row_info.Metadata = dr.GetString(6);

                        if (dr.IsDBNull(7) == false)
                            row_info.NewTimestampString = dr.GetString(7);

                        if (dr.IsDBNull(8) == false)
                            row_info.FileName = dr.GetString(8);

                        if (dr.IsDBNull(9) == false)
                            row_info.NewFileName = dr.GetString(9);

                        bool bRet = dr.Read();

                        if (bRet == true)
                        {
                            // ����һ��
                            strError = "��¼ '" + strID + "' ��SQL��" + this.m_strSqlDbName + "��records���д��ڶ���������һ�ֲ�������״̬, ��ϵͳ����Ա����SQL����ɾ������ļ�¼��";
                            return -1;
                        }
                    }
                    finally
                    {
                        dr.Close();
                    }
                } // end of using command

                return 1;
            }
            else if (connection.SqlServerType == SqlServerType.SQLite)
            {
                string strCommand = " SELECT "
                    + " range," // 0 4
                    + " dptimestamp,"   // 1 5
                    + " metadata,"  // 2 6
                    + " newdptimestamp, "   // 3 7
                    + " filename, "   // 4 8
                    + " newfilename "   // 5 9
                    + " FROM records "
                    + " WHERE id='" + strID + "'\n";

                using (SQLiteCommand command = new SQLiteCommand(strCommand,
                    connection.SQLiteConnection))
                {

                    SQLiteDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        // 1.��¼�����ڱ���
                        if (dr == null
                            || dr.HasRows == false)
                        {
                            strError = "��¼ '" + strID + "' �ڿ��в�����";
                            return 0;
                        }

                        dr.Read();

                        row_info = new RecordRowInfo();

                        /*
                        row_info.data_textptr = (byte[])GetValue(dr[0]);

                        if (dr.IsDBNull(1) == false)
                            row_info.data_length = dr.GetInt32(1);

                        row_info.newdata_textptr = (byte[])GetValue(dr[2]);

                        if (dr.IsDBNull(3) == false)
                            row_info.newdata_length = dr.GetInt32(3);
                         * */

                        if (dr.IsDBNull(0) == false)
                            row_info.Range = dr.GetString(0);

                        if (dr.IsDBNull(1) == false)
                            row_info.TimestampString = dr.GetString(1);

                        if (dr.IsDBNull(2) == false)
                            row_info.Metadata = dr.GetString(2);

                        if (dr.IsDBNull(3) == false)
                            row_info.NewTimestampString = dr.GetString(3);

                        if (dr.IsDBNull(4) == false)
                            row_info.FileName = dr.GetString(4);

                        if (dr.IsDBNull(5) == false)
                            row_info.NewFileName = dr.GetString(5);

                        bool bRet = dr.Read();

                        if (bRet == true)
                        {
                            // ����һ��
                            strError = "��¼ '" + strID + "' ��SQL��" + this.m_strSqlDbName + "��records���д��ڶ���������һ�ֲ�������״̬, ��ϵͳ����Ա����SQL����ɾ������ļ�¼��";
                            return -1;
                        }
                    }
                    finally
                    {
                        dr.Close();
                    }
                } // end of using command

                return 1;
            }
            else if (connection.SqlServerType == SqlServerType.MySql)
            {
                string strCommand = " SELECT "
                    + " `range`," // 0 4
                    + " dptimestamp,"   // 1 5
                    + " metadata,"  // 2 6
                    + " newdptimestamp, "   // 3 7
                    + " filename, "   // 4 8
                    + " newfilename "   // 5 9
                    + " FROM `" + this.m_strSqlDbName + "`.records "
                    + " WHERE id='" + strID + "'\n";

                using (MySqlCommand command = new MySqlCommand(strCommand,
                    connection.MySqlConnection))
                {

                    MySqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        // 1.��¼�����ڱ���
                        if (dr == null
                            || dr.HasRows == false)
                        {
                            strError = "��¼ '" + strID + "' �ڿ��в�����";
                            return 0;
                        }

                        dr.Read();

                        row_info = new RecordRowInfo();

                        if (dr.IsDBNull(0) == false)
                            row_info.Range = dr.GetString(0);

                        if (dr.IsDBNull(1) == false)
                            row_info.TimestampString = dr.GetString(1);

                        if (dr.IsDBNull(2) == false)
                            row_info.Metadata = dr.GetString(2);

                        if (dr.IsDBNull(3) == false)
                            row_info.NewTimestampString = dr.GetString(3);

                        if (dr.IsDBNull(4) == false)
                            row_info.FileName = dr.GetString(4);

                        if (dr.IsDBNull(5) == false)
                            row_info.NewFileName = dr.GetString(5);

                        bool bRet = dr.Read();

                        if (bRet == true)
                        {
                            // ����һ��
                            strError = "��¼ '" + strID + "' ��SQL��" + this.m_strSqlDbName + "��records���д��ڶ���������һ�ֲ�������״̬, ��ϵͳ����Ա����SQL����ɾ������ļ�¼��";
                            return -1;
                        }
                    }
                    finally
                    {
                        dr.Close();
                    }
                } // end of using command

                return 1;
            }
            else if (connection.SqlServerType == SqlServerType.Oracle)
            {
                string strCommand = " SELECT "
                    + " range," // 0 4
                    + " dptimestamp,"   // 1 5
                    + " metadata,"  // 2 6
                    + " newdptimestamp, "   // 3 7
                    + " filename, "   // 4 8
                    + " newfilename "   // 5 9
                    + " FROM " + this.m_strSqlDbName + "_records "
                    + " WHERE id='" + strID + "'\n";

                using (OracleCommand command = new OracleCommand(strCommand,
                    connection.OracleConnection))
                {
                    OracleDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    try
                    {
                        // 1.��¼�����ڱ���
                        if (dr == null
                            || dr.HasRows == false)
                        {
                            strError = "��¼ '" + strID + "' �ڿ��в�����";
                            return 0;
                        }

                        dr.Read();

                        row_info = new RecordRowInfo();

                        if (dr.IsDBNull(0) == false)
                            row_info.Range = dr.GetString(0);

                        if (dr.IsDBNull(1) == false)
                            row_info.TimestampString = dr.GetString(1);

                        if (dr.IsDBNull(2) == false)
                            row_info.Metadata = dr.GetString(2);

                        if (dr.IsDBNull(3) == false)
                            row_info.NewTimestampString = dr.GetString(3);

                        if (dr.IsDBNull(4) == false)
                            row_info.FileName = dr.GetString(4);

                        if (dr.IsDBNull(5) == false)
                            row_info.NewFileName = dr.GetString(5);

                        bool bRet = dr.Read();

                        if (bRet == true)
                        {
                            // ����һ��
                            strError = "��¼ '" + strID + "' ��SQL��" + this.m_strSqlDbName + "��records���д��ڶ���������һ�ֲ�������״̬, ��ϵͳ����Ա����SQL����ɾ������ļ�¼��";
                            return -1;
                        }
                    }
                    finally
                    {
                        if (dr != null)
                            dr.Close();
                    }

                }
                return 1;
            }
            else
            {
                strError = "δ֪�����ݿ�����: " + connection.SqlServerType.ToString();
                return -1;
            }

            return 0;
        }

        static object GetValue(object obj)
        {
            if (obj is System.DBNull)
                return null;
            return obj;
        }

#if NO

        // �ӿ��еõ�һ����¼��ʱ���
        // return:
        //		-1  ����
        //		-4  δ�ҵ���¼
        //      0   �ɹ�
        private int GetTimestampFromDb(SqlConnection connection,
            string strID,
            out byte[] outputTimestamp,
            out string strError)
        {
            strError = "";
            outputTimestamp = null;
            int nRet = 0;

            string strOutputRecordID = "";
            // return:
            //      -1  ����
            //      0   �ɹ�
            nRet = this.CanonicalizeRecordID(strID,
                out strOutputRecordID,
                out strError);
            if (nRet == -1)
            {
                strError = "GetTimestampFormDb()���ô���strID����ֵ '" + strID + "' ���Ϸ���";
                return -1;
            }
            if (strOutputRecordID == "-1")
            {
                strError = "GetTimestampFormDb()���ô���strID����ֵ '" + strID + "' ���Ϸ���";
                return -1;
            }
            strID = strOutputRecordID;


            // return:
            //      -1  ����
            //      0   ����
            nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            string strCommand = "use " + this.m_strSqlDbName + " "
                + "select dptimestamp, newdptimestamp, range"
                + " from records "
                + " where id='" + strID + "'";

            strCommand += " use master " + "\n";

            SqlCommand command = new SqlCommand(strCommand,
                connection);
            SqlDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
            try
            {
                if (dr == null
                    || dr.HasRows == false)
                {
                    strError = "GetTimestampFromDb() ���ּ�¼'" + strID + "'�ڿ��в�����";
                    return -4;
                }
                dr.Read();

                bool bReverse = false;  // strRange��һ�ַ�Ϊ'#'��Ҳ�� bReverse==false һ��������ʹ�� dptimestamp �ֶ�
                string strRange = "";
                if (dr.IsDBNull(2) == false)
                    strRange = dr.GetString(2);

                if (string.IsNullOrEmpty(strRange) == false
                    && strRange[0] == '!')
                    bReverse = true;

                string strOutputTimestamp = "";
                
                if (bReverse == false)
                    strOutputTimestamp = dr.GetString(0);
                else
                    strOutputTimestamp = dr.GetString(1);

                outputTimestamp = ByteArray.GetTimeStampByteArray(strOutputTimestamp);//Encoding.UTF8.GetBytes(strOutputTimestamp);

                bool bRet = dr.Read();

                // 2008/3/13 new add
                if (bRet == true)
                {
                    // ����һ��
                    strError = "��¼ '" + strID + "' ��SQL��" + this.m_strSqlDbName + "��records���д��ڶ�������ϵͳ����Ա����SQL����ɾ������ļ�¼��";
                    return -1;
                }

            }
            finally
            {
                dr.Close();
            }
            return 0;
        }

        // ��ȡ��¼��ʱ���
        // parameters0:
        //      strID   ��¼id
        //      baOutputTimestamp
        // return:
        //		-1  ����
        //		-4  δ�ҵ���¼
        //      0   �ɹ�
        public override int GetTimestampFromDb(
            string strID,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";

            // �����Ӷ���
            SqlConnection connection = new SqlConnection(this.m_strConnString);
            connection.Open();
            try
            {
                // return:
                //		-1  ����
                //		-4  δ�ҵ���¼
                //      0   �ɹ�
                return this.GetTimestampFromDb(connection,
                    strID,
                    out baOutputTimestamp,
                    out strError);
            }
            finally
            {
                connection.Close();
            }
        }

#endif

#if NO
        // ����ָ����¼��ʱ���
        // parameters:
        //      connection  SqlConnection����
        //      strID       ��¼id�������Ǽ�¼Ҳ��������Դ
        //      strInputTimestamp   �����ʱ���
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      >=0   �ɹ� ���ر�Ӱ��ļ�¼��
        private int SetTimestampForDb(SqlConnection connection,
            string strID,
            string strInputTimestamp,
            out string strError)
        {
            strError = "";

            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            string strCommand = "use " + this.m_strSqlDbName + "\n"
                + " UPDATE records "
                + " SET dptimestamp=@dptimestamp"
                + " WHERE id=@id";
            strCommand += " use master " + "\n";

            SqlCommand command = new SqlCommand(strCommand,
                connection);

            SqlParameter idParam = command.Parameters.Add("@id",
                SqlDbType.NVarChar);
            idParam.Value = strID;

            SqlParameter dptimestampParam =
                command.Parameters.Add("@dptimestamp",
                SqlDbType.NVarChar,
                100);
            dptimestampParam.Value = strInputTimestamp;

            int nCount = command.ExecuteNonQuery();
            if (nCount == 0)
            {
                strError = "û�и��µ���¼��Ϊ'" + strID + "'��ʱ���";
                return -1;
            }
            return nCount;
        }
#endif

        // ɾ����¼,�������ļ�,������,�ͱ���¼
        // parameter:
        //		strRecordID           ��¼ID
        //      strStyle        �ɰ��� fastmode
        //		inputTimestamp  �����ʱ���
        //		outputTimestamp out����,���ص�ʵ�ʵ�ʱ���
        //		strError        out����,���س�����Ϣ
        // return:
        //		-1  һ���Դ���
        //		-2  ʱ�����ƥ��
        //      -4  δ�ҵ���¼
        //		0   �ɹ�
        // ��: ��ȫ
        public override int DeleteRecord(
            string strRecordID,
            byte[] baInputTimestamp,
            string strStyle,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            strError = "";
            baOutputTimestamp = null;

            if (StringUtil.IsInList("fastmode", strStyle) == true)
                this.FastMode = true;
            bool bFastMode = StringUtil.IsInList("fastmode", strStyle) || this.FastMode;

            bool bDeleteKeysByID = false;   //  StringUtil.IsInList("fastmode", strStyle) || this.FastMode;

            strRecordID = DbPath.GetID10(strRecordID);

            // ���ﲻ����ΪFastMode��д��

            //********�����ݿ�Ӷ���*********************
            m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE		
			this.container.WriteDebugInfo("DeleteRecordForce()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif

            int nRet = 0;
            try
            {
                //*********�Լ�¼��д��**********
                m_recordLockColl.LockForWrite(strRecordID, m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("DeleteRecordForce()����'" + this.GetCaption("zh-CN") + "/" + strID + "'��¼��д����");
#endif
                try
                {
                    Connection connection = GetConnection(
                        this.m_strConnString,
                        this.container.SqlServerType == SqlServerType.SQLite && bFastMode == true ? ConnectionStyle.Global : ConnectionStyle.None);
                    connection.Open();
                    try
                    {
                        connection.m_nOpenCount += 10;
                        /*
                        // �Ƚ�ʱ���
                        // return:
                        //		-1  ����
                        //		-4  δ�ҵ���¼
                        //      0   �ɹ�
                        nRet = this.GetTimestampFromDb(connection,
                            strRecordID,
                            out baOutputTimestamp,
                            out strError);
                        if (nRet <= -1)
                        {
                            if (nRet == -4)
                            {
                                strError = "ɾ����¼ʧ�ܣ�ԭ��: " + strError;
                                return nRet;
                            }
                            return nRet;
                        }

                        if (baOutputTimestamp == null)
                        {
                            strError = "������ȡ����ʱ���Ϊnull";
                            return -1;
                        }

                        if (ByteArray.Compare(baInputTimestamp,
                            baOutputTimestamp) != 0)
                        {
                            strError = "ʱ�����ƥ��";
                            return -2;
                        }
                         * */
                        RecordRowInfo row_info = null;
                        // return:
                        //      -1  ����
                        //      0   ��¼������
                        //      1   �ɹ�
                        nRet = GetRowInfo(connection,
    strRecordID,
    out row_info,
    out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet == 0)  // 2013/11/21
                            return -4;

                        // �Ƚ�ʱ���
                        string strCompleteTimestamp = row_info.TimestampString; // �ϴ�����д��ʱ��ʱ���

                        // �������ݵ�ʱ���
                        if (String.IsNullOrEmpty(row_info.Range) == false
                && row_info.Range[0] == '!')
                            strCompleteTimestamp = row_info.NewTimestampString;

                        baOutputTimestamp = ByteArray.GetTimeStampByteArray(strCompleteTimestamp);

                        if (ByteArray.Compare(baInputTimestamp,
    baOutputTimestamp) != 0)
                        {
                            strError = "ʱ�����ƥ��";
                            return -2;
                        }

                        XmlDocument newDom = null;
                        XmlDocument oldDom = null;

                        KeyCollection newKeys = null;
                        KeyCollection oldKeys = null;

                        if (bDeleteKeysByID == false)
                        {

                            string strXml = "";
                            // return:
                            //      -1  ����
                            //      -4  ��¼������
                            //      -100    �����ļ�������
                            //      0   ��ȷ
                            nRet = this.GetXmlString(connection,
                                strRecordID,
                                out strXml,
                                out strError);
                            if (nRet == -100)
                                strXml = "";
                            else if (nRet <= -1)
                                return nRet;


                            // 1.ɾ��������

                            // return:
                            //      -1  ����
                            //      0   �ɹ�
                            nRet = this.MergeKeys(strRecordID,
                                "",
                                strXml,
                                true,
                                out newKeys,
                                out oldKeys,
                                out newDom,
                                out oldDom,
                                out strError);
                            if (nRet == -1)
                            {
                                strError = "ɾ���й��������׶γ��� " + strError;
                                return -1;
                            }
                        }

                        if (oldDom != null)
                        {
                            // return:
                            //      -1  ����
                            //      0   �ɹ�
                            nRet = this.ModifyKeys(connection,
                                null,
                                oldKeys,
                                bFastMode,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }
                        else
                        {
                            // return:
                            //      -1  ����
                            //      0   �ɹ�
                            nRet = this.ForceDeleteKeys(connection,
                                strRecordID,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }

#if NO
                        // 2.ɾ�����ļ�
                        if (oldDom != null)
                        {
                            // return:
                            //      -1  ����
                            //      0   �ɹ�
                            nRet = this.ModifyFiles(connection,
                                strRecordID,
                                null,
                                oldDom,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }
                        else
                        {

                            // ͨ����¼��֮��Ĺ�ϵǿ��ɾ��
                            // return:
                            //      -1  ����
                            //      0   �ɹ�
                            nRet = this.ForceDeleteFiles(connection,
                                strRecordID,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }
#endif

                        if (this.container.SqlServerType == SqlServerType.Oracle)
                        {
                            // 2.ɾ���Ӽ�¼
                            if (oldDom != null)
                            {
                                // return:
                                //      -1  ����
                                //      0   �ɹ�
                                nRet = this.ModifyFiles(connection,
                                    strRecordID,
                                    null,
                                    oldDom,
                                    out strError);
                                if (nRet == -1)
                                    return -1;
                            }

                            // ɾ���Լ�,����ɾ���ļ�¼��
                            // return:
                            //      -1  ����
                            //      >=0   �ɹ� ����ɾ���ļ�¼��
                            nRet = DeleteRecordByID(connection,
                                row_info,
                                strRecordID,
                                oldDom != null ? false : true,
                                this.m_lObjectStartSize != -1,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            if (nRet == 0)
                            {
                                strError = "ɾ����¼ʱ,�ӿ���û�ҵ���¼��Ϊ'" + strRecordID + "'�ļ�¼";
                                return -1;
                            }
                        }
                        else
                        {

                            // 3.ɾ���Լ�,����ɾ���ļ�¼��
                            // return:
                            //      -1  ����
                            //      >=0   �ɹ� ����ɾ���ļ�¼��
                            nRet = DeleteRecordByID(connection,
                                row_info,
                                strRecordID,
                                true,
                                this.m_lObjectStartSize != -1,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            if (nRet == 0)
                            {
                                strError = "ɾ����¼ʱ,�ӿ���û�ҵ���¼��Ϊ'" + strRecordID + "'�ļ�¼";
                                return -1;
                            }
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        strError = GetSqlErrors(sqlEx);

                        /*
                        if (sqlEx.Errors is SqlErrorCollection)
                            strError = "���ݿ�'" + this.GetCaption("zh") + "'��δ��ʼ����";
                        else
                            strError = sqlEx.Message;
                         * */
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        strError = "ɾ��'" + this.GetCaption("zh-CN") + "'����idΪ'" + strRecordID + "'�ļ�¼ʱ����,ԭ��:" + ex.Message;
                        return -1;
                    }
                    finally // ����
                    {
                        connection.Close();
                    }
                }
                finally // ��¼��
                {
                    //**************�Լ�¼��д��**********
                    m_recordLockColl.UnlockForWrite(strRecordID);
#if DEBUG_LOCK_SQLDATABASE			
					this.container.WriteDebugInfo("DeleteRecordForce()����'" + this.GetCaption("zh-CN") + "/" + strID + "'��¼��д����");
#endif

                }
            }
            finally
            {
                //***************�����ݿ�����*****************
                m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK_SQLDATABASE		
				this.container.WriteDebugInfo("DeleteRecordForce()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }

            if (StringUtil.IsInList("fastmode", strStyle) == false
    && this.FastMode == true)
            {
                // this.FastMode = false;
                this.Commit();
            }

            return 0;
        }


        // �ؽ���¼��keys
        // parameter:
        //		strRecordID           ��¼ID
        //      strStyle    next prev outputpath forcedeleteoldkeys
        //                  forcedeleteoldkeys Ҫ�ڴ�����keysǰǿ��ɾ��һ�¾��е�keys? ���Ϊ��������ǿ��ɾ��ԭ�е�keys�����Ϊ������������̽�Ŵ����µ�keys������оɵ�keys���´��㴴����keys�غϣ��ǾͲ��ظ�����������ɵ�keys�в���û�б�ɾ����Ҳ����������
        //                          ���� һ�����ڵ�����¼�Ĵ��������� һ������Ԥ��ɾ��������keys����������Ժ���ѭ���ؽ�����ÿ����¼��������ʽ
        //		strError        out����,���س�����Ϣ
        // return:
        //		-1  һ���Դ���
        //		-2  ʱ�����ƥ��
        //      -4  δ�ҵ���¼
        //		0   �ɹ�
        // ��: ��ȫ
        public override int RebuildRecordKeys(string strRecordID,
            string strStyle,
            out string strOutputRecordID,
            out string strError)
        {
            strError = "";
            strOutputRecordID = "";
            int nRet = 0;

            if (StringUtil.IsInList("fastmode", strStyle) == true)
                this.FastMode = true;
            bool bFastMode = StringUtil.IsInList("fastmode", strStyle) || this.FastMode;

            strRecordID = DbPath.GetID10(strRecordID);

            //********�����ݿ�Ӷ���*********************
            m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE		
			this.container.WriteDebugInfo("RebuildRecordKeys()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            ////
            try // lock database
            {
                Connection connection = new Connection(this,
                    this.m_strConnString);
                connection.Open();
                try // connection
                {
                    // ���ID
                    // return:
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = DatabaseUtil.CheckAndGet10RecordID(ref strRecordID,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // ����ʽȥ�հ�
                    strStyle = strStyle.Trim();

                    // ȡ��ʵ�ʵļ�¼��
                    if (StringUtil.IsInList("prev", strStyle) == true
                        || StringUtil.IsInList("next", strStyle) == true)
                    {
                        string strTempOutputID = "";
                        // return:
                        //		-1  ����
                        //      0   δ�ҵ�
                        //      1   �ҵ�
                        nRet = this.GetRecordID(connection,
                            strRecordID,
                            strStyle,
                            out strTempOutputID,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        if (nRet == 0 || strTempOutputID == "")
                        {
                            strError = "δ�ҵ���¼ID '" + strRecordID + "' �ķ��Ϊ '" + strStyle + "' �ļ�¼";
                            return -4;
                        }

                        strRecordID = strTempOutputID;

                        // �ٴμ��һ�·��ص�ID
                        // return:
                        //      -1  ����
                        //      0   �ɹ�
                        nRet = DatabaseUtil.CheckAndGet10RecordID(ref strRecordID,
                            out strError);
                        if (nRet == -1)
                            return -1;
                    }

                    // ���ݷ��Ҫ�󣬷�����Դ·��
                    if (StringUtil.IsInList("outputpath", strStyle) == true)
                    {
                        strOutputRecordID = DbPath.GetCompressedID(strRecordID);
                    }


                    //*********�Լ�¼��д��**********
                    m_recordLockColl.LockForWrite(strRecordID, m_nTimeOut);
#if DEBUG_LOCK_SQLDATABASE
				this.container.WriteDebugInfo("RebuildRecordKeys()����'" + this.GetCaption("zh-CN") + "/" + strID + "'��¼��д����");
#endif
                    try // lock record
                    {
                        string strXml;
                        // return:
                        //      -1  ����
                        //      -4  ��¼������
                        //      -100    �����ļ�������
                        //      0   ��ȷ
                        nRet = this.GetXmlString(connection,
                            strRecordID,
                            out strXml,
                            out strError);
                        if (nRet <= -1)
                            return nRet;

                        XmlDocument newDom = null;
                        XmlDocument oldDom = null;

                        KeyCollection newKeys = null;
                        KeyCollection oldKeys = null;

                        // TODO: �Ƿ�������β�����һ��command�ַ���ʵ�֣�

                        // return:
                        //      -1  ����
                        //      0   �ɹ�
                        nRet = this.MergeKeys(strRecordID,
                            strXml, // newxml
                            "", // oldxml
                            true,
                            out newKeys,
                            out oldKeys,
                            out newDom,
                            out oldDom,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        if (StringUtil.IsInList("forcedeleteoldkeys", strStyle) == true)
                        {
                            // return:
                            //      -1  ����
                            //      0   �ɹ�
                            nRet = this.ForceDeleteKeys(connection,
                                strRecordID,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }

                        if (newDom != null)
                        {
                            // TODO: ��bForceDeleteOldKeysΪfalse��ʱ�򣬿����ظ�����keyu�Ƿ�ᱨ���������⣿

                            // return:
                            //      -1  ����
                            //      0   �ɹ�
                            nRet = this.ModifyKeys(connection,
                                newKeys,
                                null,
                                bFastMode,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }


                    } // end of lock record
                finally // ��¼��
                    {
                        //**************�Լ�¼��д��**********
                        m_recordLockColl.UnlockForWrite(strRecordID);
#if DEBUG_LOCK_SQLDATABASE			
					this.container.WriteDebugInfo("RebuildRecordKeys()����'" + this.GetCaption("zh-CN") + "/" + strID + "'��¼��д����");
#endif
                    }

                    ////
                } // enf of try connection
                catch (SqlException sqlEx)
                {
                    strError = GetSqlErrors(sqlEx);
                    return -1;
                }
                catch (Exception ex)
                {
                    strError = "�ؽ� '" + this.GetCaption("zh-CN") + "' ����idΪ '" + strRecordID + "' �ļ�¼�����keysʱ����,ԭ��:" + ex.Message;
                    return -1;
                }
                finally // ����
                {
                    connection.Close();
                }

            } // lock database
            finally
            {
                //***************�����ݿ�����*****************
                m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK_SQLDATABASE		
				this.container.WriteDebugInfo("RebuildRecordKeys()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }

            if (StringUtil.IsInList("fastmode", strStyle) == false
&& this.FastMode == true)
            {
                // this.FastMode = false;
                this.Commit();
            }

            return 0;
        }

#if NO
        // 2011/1/16
        // ɾ�����ļ�
        // ��ModifyFiles()��������������like�㷨��ɾ�������Ӽ�¼������<dprms:file>��û�м��ص��Ӽ�¼
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int DeleteSubRecords(SqlConnection connection,
            string strID,
            out string strError)
        {
            strError = "";
            strID = DbPath.GetID10(strID);

            SqlConnection new_connection = null;
            if (connection.ConnectionTimeout < m_nLongTimeout)
            {
                new_connection = new SqlConnection(this.m_strLongConnString);
                new_connection.Open();
                connection = new_connection;
            }


            try
            {
                SqlCommand command = new SqlCommand("", connection);

                string strCommand = "use " + this.m_strSqlDbName + " \n"
                        + " DELETE FROM records WHERE id like '" + strID + "_%' \n"
                        + " use master " + "\n";

                command.CommandText = strCommand;
                command.CommandTimeout = m_nLongTimeout; // 30����

                int nResultCount = 0;
                try
                {
                    nResultCount = command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    strError = "ɾ����¼·��Ϊ'" + this.GetCaption("zh") + "/" + strID + "'�����ļ���������:" + ex.Message + ",sql����:\r\n" + strCommand;
                    return -1;
                }
                return 0;
            }
            finally
            {
                if (new_connection != null)
                    new_connection.Close();
            }
        }

        // ���ݼ�¼��֮��Ĺ�ϵ(��¼��~~��¼��_0),ǿ��ɾ����Դ�ļ�
        // parameters:
        //      connection  SqlConnection����
        //      strRecordID ��¼id  ������10λ
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        private int ForceDeleteFiles(SqlConnection connection,
            string strRecordID,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // ��������
            // return:
            //      -1  ����
            //      0   ����
            nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            Debug.Assert(strRecordID != null && strRecordID.Length == 10, "ForceDeleteFiles()���ô���strRecordID����ֵ����Ϊnull�ҳ��ȱ������10λ��");

            string strCommand = "use " + this.m_strSqlDbName + " "
                + " DELETE FROM records WHERE id like @id";
            strCommand += " use master " + "\n";

            SqlCommand command = new SqlCommand(strCommand,
                connection);
            command.CommandTimeout = m_nLongTimeout; // 30����

            SqlParameter param = command.Parameters.Add("@id",
                SqlDbType.NVarChar);
            param.Value = strRecordID + "_%";

            //???�������ɾ������
            int nDeletedCount = command.ExecuteNonQuery();

            return 0;
        }
#endif


#if NOOOOOOOOOOOOOO
        // 2007/4/16
        // ǿ��ɾ������һ����¼��ȫ�������㡣����Ҫ�����㶨�塣
        // parameters:
        //      strRecordID ����Ϊ10λ������
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int ForceDeleteKeys(SqlConnection connection,
            string strRecordID,
            out string strError)
        {
            strError = "";
            string strCommand = "";

            KeysCfg keysCfg = null;

            nRet = this.GetKeysCfg(out keysCfg,
                out strError);
            if (nRet == -1)
                return -1;

            List<TableInfo> aTableInfo = null;
            nRet = keysCfg.GetTableInfosRemoveDup(
                out aTableInfo,
                out strError);
            if (nRet == -1)
                return -1;

            SqlCommand command = new SqlCommand("", connection);

            // ѭ��ȫ����
            for (int i = 0; aTableInfo.Count; i++)
            {
                TableInfo tableInfo = aTableInfo[i];

                string strKeysTableName = tableInfo.SqlTableName;

                string strIdParamName = "@id" + i.ToString();

                strCommand += " DELETE FROM " + strKeysTableName
                    + " WHERE idstring= " + strIdParamName;

                SqlParameter idParam =
                    command.Parameters.Add(strIdParamName,
                    SqlDbType.NVarChar);
                idParam.Value = strRecordID;

                SqlParameter keynumParam =
                    command.Parameters.Add(strKeynumParamName,
                    SqlDbType.NVarChar);
                keynumParam.Value = oneKey.Num;
            }

            strCommand = "use " + this.m_strSqlDbName + " \n"
                + strCommand
                + " use master " + "\n";
            command.CommandText = strCommand;
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                strError = "ǿ�Ƽ��������,��¼·��'" + this.GetCaption("zh-CN") + "/" + strRecordID + "��ԭ��" + ex.Message;
                return -1;
            }

            return 0;
        }
#endif

        // �������� where idstring in (...) �� ID �б��ַ���
        static int BuildIdString(List<string> ids,
            out string strResult,
            out string strError)
        {
            strError = "";
            strResult = "";

            StringBuilder idstring = new StringBuilder(4096);
            int i = 0;
            foreach (string s in ids)
            {
                if (string.IsNullOrEmpty(s) == true || s.Length != 10)
                {
                    strError = "ID�ַ��� '" + s + "' ���Ϸ�";
                    return -1;
                }
                if (StringUtil.IsPureNumber(s) == false)
                {
                    strError = "ID '" + s + "' �����Ǵ�����";
                    return -1;
                }
                if (i != 0)
                    idstring.Append(",");
                idstring.Append("'" + s + "'");
                i++;
            }

            strResult = idstring.ToString();
            return 0;
        }

        // ǿ��ɾ����¼��Ӧ�ļ�����,������еı�
        // parameters:
        //      connection  SqlConnection���Ӷ���
        //      ids         ��¼id���顣ÿ�� id ӦΪ 10 �ַ���̬
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      >=0 �ɹ������ֱ�ʾʵ��ɾ���ļ��������
        // ��: ����ȫ
        public int ForceDeleteKeys(Connection connection,
            List<string> ids,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // return:
            //      -1  ����
            //      0   ����
            nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            foreach(string strRecordID in ids)
            {
                Debug.Assert(strRecordID != null && strRecordID.Length == 10, "ForceDeleteKeys()���ô���strRecordID����ֵ����Ϊnull�ҳ��ȱ������10λ��");
                if (string.IsNullOrEmpty(strRecordID) == true || strRecordID.Length != 10)
                {
                    strError = "ForceDeleteKeys() ID�ַ��� '" + strRecordID + "' ���Ϸ�";
                    return -1;
                }
            }

            KeysCfg keysCfg = null;
            nRet = this.GetKeysCfg(out keysCfg,
                out strError);
            if (nRet == -1)
                return -1;
            if (keysCfg == null)
                return 0;

            List<TableInfo> aTableInfo = null;
            nRet = keysCfg.GetTableInfosRemoveDup(
                out aTableInfo,
                out strError);
            if (nRet == -1)
                return -1;

            if (aTableInfo.Count == 0)
                return 0;

            string strIdString = "";
            nRet = BuildIdString(ids, out strIdString, out strError);
            if (nRet == -1)
                return -1;

            int nDeletedCount = 0;

            if (container.SqlServerType == SqlServerType.MsSqlServer)
            {
                string strCommand = "";
                for (int i = 0; i < aTableInfo.Count; i++)
                {
                    TableInfo tableInfo = aTableInfo[i];

                    //strCommand += "DELETE FROM " + tableInfo.SqlTableName
                    //    + " WHERE idstring=@id \r\n";
                    strCommand += "DELETE FROM " + tableInfo.SqlTableName
                        + " WHERE idstring in (" + strIdString + ")\r\n";
                }

                if (string.IsNullOrEmpty(strCommand) == false)
                {
                    strCommand = "use " + this.m_strSqlDbName + " \r\n"
                        + strCommand
                        + "use master " + "\r\n";

                    using (SqlCommand command = new SqlCommand(strCommand,
                        connection.SqlConnection))
                    {
#if NO
                        SqlParameter idParam = command.Parameters.Add("@id",
                            SqlDbType.NVarChar);
                        idParam.Value = strRecordID;
#endif

                        // ????�������ɾ������
                        nDeletedCount = command.ExecuteNonQuery();
                    } // end of using command
                }

                return nDeletedCount;
            }
            else if (container.SqlServerType == SqlServerType.SQLite)
            {
                string strCommand = "";
                for (int i = 0; i < aTableInfo.Count; i++)
                {
                    TableInfo tableInfo = aTableInfo[i];

#if NO
                    strCommand += "DELETE FROM " + tableInfo.SqlTableName
                        + " WHERE idstring=@id ;\r\n";
#endif
                    strCommand += "DELETE FROM " + tableInfo.SqlTableName
    + " WHERE idstring IN (" + strIdString + ") ;\r\n";

                }

                if (string.IsNullOrEmpty(strCommand) == false)
                {
                    using (SQLiteCommand command = new SQLiteCommand(strCommand,
                        connection.SQLiteConnection))
                    {
#if NO
                        SQLiteParameter idParam = command.Parameters.Add("@id",
                            DbType.String);
                        idParam.Value = strRecordID;
#endif

                        // ????�������ɾ������
                        nDeletedCount = command.ExecuteNonQuery();
                    } // end of using command
                }

                return nDeletedCount;
            }
            else if (container.SqlServerType == SqlServerType.MySql)
            {
                string strCommand = "";
                for (int i = 0; i < aTableInfo.Count; i++)
                {
                    TableInfo tableInfo = aTableInfo[i];

#if NO
                    strCommand += "DELETE FROM " + tableInfo.SqlTableName
                        + " WHERE idstring=@id ;\r\n";
#endif
                    strCommand += "DELETE FROM " + tableInfo.SqlTableName
    + " WHERE idstring IN (" + strIdString + ") ;\r\n";

                }

                if (string.IsNullOrEmpty(strCommand) == false)
                {
                    strCommand = "use `" + this.m_strSqlDbName + "` ;\n"
    + strCommand;

                    using (MySqlCommand command = new MySqlCommand(strCommand,
                        connection.MySqlConnection))
                    {
#if NO
                        MySqlParameter idParam = command.Parameters.Add("@id",
                            MySqlDbType.String);
                        idParam.Value = strRecordID;
#endif

                        // ????�������ɾ������
                        nDeletedCount = command.ExecuteNonQuery();
                    } // end of using command
                }

                return nDeletedCount;
            }
            else if (container.SqlServerType == SqlServerType.Oracle)
            {
                using (OracleCommand command = new OracleCommand("",
    connection.OracleConnection))
                {
                    command.BindByName = true;

                    string strCommand = "";
                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = aTableInfo[i];

#if NO
                        strCommand = "DELETE FROM " + this.m_strSqlDbName + "_" + tableInfo.SqlTableName
                            + " WHERE idstring=:id \r\n";

                        OracleParameter idParam = command.Parameters.Add(":id",
                            OracleDbType.NVarchar2);
                        idParam.Value = strRecordID;
#endif

                        strCommand = "DELETE FROM " + this.m_strSqlDbName + "_" + tableInfo.SqlTableName
    + " WHERE idstring IN (" + strIdString + ") \r\n";

                        // ????�������ɾ������
                        command.CommandText = strCommand;
                        nDeletedCount += command.ExecuteNonQuery();

                        command.Parameters.Clear();
                    }
                } // end of using command

                return nDeletedCount;
            }

            return 0;
        }

        // ǿ��ɾ����¼��Ӧ�ļ�����,������еı�
        // parameters:
        //      connection  SqlConnection���Ӷ���
        //      strRecordID ��¼id, ��֮ǰ������Ϊ10�ַ�
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      >=0 �ɹ������ֱ�ʾʵ��ɾ���ļ��������
        // ��: ����ȫ
        public int ForceDeleteKeys(Connection connection,
            string strRecordID,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // return:
            //      -1  ����
            //      0   ����
            nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            Debug.Assert(strRecordID != null && strRecordID.Length == 10, "ForceDeleteKeys()���ô���strRecordID����ֵ����Ϊnull�ҳ��ȱ������10λ��");

            KeysCfg keysCfg = null;
            nRet = this.GetKeysCfg(out keysCfg,
                out strError);
            if (nRet == -1)
                return -1;
            if (keysCfg == null)
                return 0;

            List<TableInfo> aTableInfo = null;
            nRet = keysCfg.GetTableInfosRemoveDup(
                out aTableInfo,
                out strError);
            if (nRet == -1)
                return -1;

            if (aTableInfo.Count == 0)
                return 0;

            int nDeletedCount = 0;

            if (container.SqlServerType == SqlServerType.MsSqlServer)
            {
                string strCommand = "";
                for (int i = 0; i < aTableInfo.Count; i++)
                {
                    TableInfo tableInfo = aTableInfo[i];

                    strCommand += "DELETE FROM " + tableInfo.SqlTableName
                        + " WHERE idstring=@id \r\n";
                }

                if (string.IsNullOrEmpty(strCommand) == false)
                {
                    strCommand = "use " + this.m_strSqlDbName + " \r\n"
                        + strCommand
                        + "use master " + "\r\n";

                    using (SqlCommand command = new SqlCommand(strCommand,
                        connection.SqlConnection))
                    {
                        SqlParameter idParam = command.Parameters.Add("@id",
                            SqlDbType.NVarChar);
                        idParam.Value = strRecordID;

                        // ????�������ɾ������
                        nDeletedCount = command.ExecuteNonQuery();
                    } // end of using command
                }

                return nDeletedCount;
            }
            else if (container.SqlServerType == SqlServerType.SQLite)
            {
                string strCommand = "";
                for (int i = 0; i < aTableInfo.Count; i++)
                {
                    TableInfo tableInfo = aTableInfo[i];

                    strCommand += "DELETE FROM " + tableInfo.SqlTableName
                        + " WHERE idstring=@id ;\r\n";
                }

                if (string.IsNullOrEmpty(strCommand) == false)
                {
                    using (SQLiteCommand command = new SQLiteCommand(strCommand,
                        connection.SQLiteConnection))
                    {
                        SQLiteParameter idParam = command.Parameters.Add("@id",
                            DbType.String);
                        idParam.Value = strRecordID;

                        // ????�������ɾ������
                        nDeletedCount = command.ExecuteNonQuery();
                    } // end of using command
                }

                return nDeletedCount;
            }
            else if (container.SqlServerType == SqlServerType.MySql)
            {
                string strCommand = "";
                for (int i = 0; i < aTableInfo.Count; i++)
                {
                    TableInfo tableInfo = aTableInfo[i];

                    strCommand += "DELETE FROM " + tableInfo.SqlTableName
                        + " WHERE idstring=@id ;\r\n";
                }

                if (string.IsNullOrEmpty(strCommand) == false)
                {
                    strCommand = "use `" + this.m_strSqlDbName + "` ;\n"
    + strCommand;

                    using (MySqlCommand command = new MySqlCommand(strCommand,
                        connection.MySqlConnection))
                    {
                        MySqlParameter idParam = command.Parameters.Add("@id",
                            MySqlDbType.String);
                        idParam.Value = strRecordID;

                        // ????�������ɾ������
                        nDeletedCount = command.ExecuteNonQuery();
                    } // end of using command
                }

                return nDeletedCount;
            }
            else if (container.SqlServerType == SqlServerType.Oracle)
            {
                using (OracleCommand command = new OracleCommand("",
    connection.OracleConnection))
                {
                    command.BindByName = true;

                    string strCommand = "";
                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = aTableInfo[i];

                        strCommand = "DELETE FROM " + this.m_strSqlDbName + "_" + tableInfo.SqlTableName
                            + " WHERE idstring=:id \r\n";

                        OracleParameter idParam = command.Parameters.Add(":id",
                            OracleDbType.NVarchar2);
                        idParam.Value = strRecordID;

                        // ????�������ɾ������
                        command.CommandText = strCommand;
                        nDeletedCount += command.ExecuteNonQuery();

                        command.Parameters.Clear();
                    }
                } // end of using command

                return nDeletedCount;
            }

            return 0;
        }

        // �ӿ���ɾ��ָ���ļ�¼,�����Ǽ�¼Ҳ��������Դ
        // parameters:
        //      connection  ���Ӷ���
        //      strID       ��¼id
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      >=0   �ɹ� ����ɾ���ļ�¼��
        private int DeleteRecordByID(
            Connection connection,
            RecordRowInfo row_info,
            string strID,
            bool bDeleteSubrecord,
            bool bDeleteObjectFiles,
            out string strError)
        {
            strError = "";

            Debug.Assert(connection != null, "DeleteRecordById()���ô���connection����ֵ����Ϊnull��");
            Debug.Assert(strID != null, "DeleteRecordById()���ô���strID����ֵ����Ϊnull��");
            Debug.Assert(strID.Length >= 10, "DeleteRecordByID()���ô��� strID����ֵ�ĳ��ȱ�����ڵ���10��");

            int nDeletedCount = 0;

            // return:
            //      -1  ����
            //      0   ����
            int nRet = this.CheckConnection(connection,
                out strError);
            if (nRet == -1)
                return -1;

            List<string> filenames = new List<string>();

            if (connection.SqlServerType == SqlServerType.MsSqlServer)
            {
                SqlConnection current_connection = null;
                SqlConnection new_connection = null;
                if (connection.SqlConnection.ConnectionTimeout < m_nLongTimeout)
                {
                    new_connection = new SqlConnection(this.m_strLongConnString);
                    new_connection.Open();
                    current_connection = new_connection;
                }
                else
                    current_connection = connection.SqlConnection;

                try
                {
                    using (SqlCommand command = new SqlCommand("",
            current_connection))
                    {
                        string strCommand = "";

                        // ��һ������ö��ļ���
                        if (bDeleteObjectFiles == true)
                        {
                            if (row_info != null && bDeleteSubrecord == false)
                            {
                                // �������ͨ�� row_info ��ɾ����¼�Ķ����ļ�
                            }
                            else if (bDeleteSubrecord == true)
                            {
                                // TODO: ��Ҫ���ȫ��filename��newfilename�ֶ�����ֵ
                                Debug.Assert(strID.Length == 10, "");

                                strCommand = "use " + this.m_strSqlDbName + " "
                                    + " SELECT filename, newfilename FROM records WHERE id like @id1 OR id = @id2";
                                strCommand += " use master " + "\n";

                                command.CommandText = strCommand;
                                command.CommandTimeout = m_nLongTimeout;// 30����

                                SqlParameter param1 = command.Parameters.Add("@id1",
                SqlDbType.NVarChar);
                                param1.Value = strID + "_%";

                                SqlParameter param2 = command.Parameters.Add("@id2",
                                    SqlDbType.NVarChar);
                                param2.Value = strID;
                            }
                            else if (row_info == null)
                            {
                                strCommand = "use " + this.m_strSqlDbName + " "
            + " SELECT filename, newfilename FROM records WHERE id = @id";
                                strCommand += " use master " + "\n";

                                command.CommandText = strCommand;
                                command.CommandTimeout = m_nLongTimeout;// 30����

                                SqlParameter param = command.Parameters.Add("@id",
                SqlDbType.NVarChar);
                                param.Value = strID;
                            }
                        }

                        if (string.IsNullOrEmpty(strCommand) == false)
                        {
                            SqlDataReader dr =
            command.ExecuteReader();
                            if (dr != null
        && dr.HasRows == true)
                            {
                                while (dr.Read())
                                {
                                    if (dr.IsDBNull(0) == false)
                                        filenames.Add(dr.GetString(0));
                                    if (dr.IsDBNull(1) == false)
                                        filenames.Add(dr.GetString(1));
                                }
                            }
                            dr.Close();
                        }

                        // �ڶ�����ɾ��SQL��
                        if (bDeleteSubrecord == true)
                        {
                            Debug.Assert(strID.Length == 10, "");

                            strCommand = "use " + this.m_strSqlDbName + " "
                                + " DELETE FROM records WHERE id like @id1 OR id = @id2";
                            strCommand += " use master " + "\n";

                            command.CommandText = strCommand;
                            command.CommandTimeout = m_nLongTimeout;// 30����
                            command.Parameters.Clear();

                            SqlParameter param1 = command.Parameters.Add("@id1",
                                SqlDbType.NVarChar);
                            param1.Value = strID + "_%";

                            SqlParameter param2 = command.Parameters.Add("@id2",
                                SqlDbType.NVarChar);
                            param2.Value = strID;
                        }
                        else
                        {
                            strCommand = "use " + this.m_strSqlDbName + " "
            + " DELETE FROM records WHERE id = @id";
                            strCommand += " use master " + "\n";

                            command.CommandText = strCommand;
                            command.CommandTimeout = m_nLongTimeout;// 30����
                            command.Parameters.Clear();

                            SqlParameter param = command.Parameters.Add("@id",
                                SqlDbType.NVarChar);
                            param.Value = strID;
                        }

                        nDeletedCount = command.ExecuteNonQuery();
                        if (nDeletedCount != 1)
                        {
                            this.container.KernelApplication.WriteErrorLog("ϣ��ɾ��" + strID + " '1'����ʵ��ɾ��'" + Convert.ToString(nDeletedCount) + "'��");
                        }
                    } // end of using command
                }
                finally
                {
                    if (new_connection != null)
                        new_connection.Close();
                }
            }
            else if (connection.SqlServerType == SqlServerType.SQLite)
            {
                using (SQLiteCommand command = new SQLiteCommand("",
                            connection.SQLiteConnection))
                {
                    string strCommand = "";

                    // ��һ������ö��ļ���
                    if (bDeleteObjectFiles == true)
                    {
                        if (row_info != null && bDeleteSubrecord == false)
                        {
                            // �������ͨ�� row_info ��ɾ����¼�Ķ����ļ�
                        }
                        else if (bDeleteSubrecord == true)
                        {
                            // TODO: ��Ҫ���ȫ��filename��newfilename�ֶ�����ֵ
                            Debug.Assert(strID.Length == 10, "");

                            strCommand = " SELECT filename, newfilename FROM records WHERE id like @id1 OR id = @id2";
                            command.CommandText = strCommand;
                            command.CommandTimeout = m_nLongTimeout;// 30����

                            SQLiteParameter param1 = command.Parameters.Add("@id1",
                                DbType.String);
                            param1.Value = strID + "_%";

                            SQLiteParameter param2 = command.Parameters.Add("@id2",
                                DbType.String);
                            param2.Value = strID;
                        }
                        else if (row_info == null)
                        {
                            strCommand = " SELECT filename, newfilename FROM records WHERE id = @id";
                            command.CommandText = strCommand;
                            command.CommandTimeout = m_nLongTimeout;// 30����

                            SQLiteParameter param = command.Parameters.Add("@id",
                                DbType.String);
                            param.Value = strID;
                        }
                    }

                    if (string.IsNullOrEmpty(strCommand) == false)
                    {
                        SQLiteDataReader dr = command.ExecuteReader();
                        if (dr != null
                            && dr.HasRows == true)
                        {
                            while (dr.Read())
                            {
                                if (dr.IsDBNull(0) == false)
                                    filenames.Add(dr.GetString(0));
                                if (dr.IsDBNull(1) == false)
                                    filenames.Add(dr.GetString(1));
                            }
                        }
                        dr.Close();
                    }

                    // �ڶ�����ɾ��SQL��
                    if (bDeleteSubrecord == true)
                    {
                        Debug.Assert(strID.Length == 10, "");

                        strCommand = " DELETE FROM records WHERE id like @id1 OR id = @id2";
                        command.CommandText = strCommand;
                        command.CommandTimeout = m_nLongTimeout;// 30����
                        command.Parameters.Clear();

                        SQLiteParameter param1 = command.Parameters.Add("@id1",
                            DbType.String);
                        param1.Value = strID + "_%";

                        SQLiteParameter param2 = command.Parameters.Add("@id2",
                            DbType.String);
                        param2.Value = strID;
                    }
                    else
                    {
                        strCommand = " DELETE FROM records WHERE id = @id";
                        command.CommandText = strCommand;
                        command.CommandTimeout = m_nLongTimeout;// 30����
                        command.Parameters.Clear();

                        SQLiteParameter param = command.Parameters.Add("@id",
                            DbType.String);
                        param.Value = strID;
                    }

                    nDeletedCount = command.ExecuteNonQuery();
                    if (nDeletedCount != 1)
                    {
                        this.container.KernelApplication.WriteErrorLog("ϣ��ɾ��" + strID + " '1'����ʵ��ɾ��'" + Convert.ToString(nDeletedCount) + "'��");
                    }
                } // end of using command
            }
            else if (connection.SqlServerType == SqlServerType.MySql)
            {
                using (MySqlCommand command = new MySqlCommand("",
                            connection.MySqlConnection))
                {
                    string strCommand = "";

                    // ��һ������ö��ļ���
                    if (bDeleteObjectFiles == true)
                    {
                        if (row_info != null && bDeleteSubrecord == false)
                        {
                            // �������ͨ�� row_info ��ɾ����¼�Ķ����ļ�
                        }
                        else if (bDeleteSubrecord == true)
                        {
                            // TODO: ��Ҫ���ȫ��filename��newfilename�ֶ�����ֵ
                            Debug.Assert(strID.Length == 10, "");

                            strCommand = " SELECT filename, newfilename FROM `" + this.m_strSqlDbName + "`.records WHERE id like @id1 OR id = @id2";
                            command.CommandText = strCommand;
                            command.CommandTimeout = m_nLongTimeout;// 30����

                            MySqlParameter param1 = command.Parameters.Add("@id1",
                                MySqlDbType.String);
                            param1.Value = strID + "_%";

                            MySqlParameter param2 = command.Parameters.Add("@id2",
                                MySqlDbType.String);
                            param2.Value = strID;
                        }
                        else if (row_info == null)
                        {
                            strCommand = " SELECT filename, newfilename FROM `" + this.m_strSqlDbName + "`.records WHERE id = @id";
                            command.CommandText = strCommand;
                            command.CommandTimeout = m_nLongTimeout;// 30����

                            MySqlParameter param = command.Parameters.Add("@id",
                                MySqlDbType.String);
                            param.Value = strID;
                        }
                    }

                    if (string.IsNullOrEmpty(strCommand) == false)
                    {
                        MySqlDataReader dr = command.ExecuteReader();
                        if (dr != null
                            && dr.HasRows == true)
                        {
                            while (dr.Read())
                            {
                                if (dr.IsDBNull(0) == false)
                                    filenames.Add(dr.GetString(0));
                                if (dr.IsDBNull(1) == false)
                                    filenames.Add(dr.GetString(1));
                            }
                        }
                        dr.Close();
                    }

                    // �ڶ�����ɾ��SQL��
                    if (bDeleteSubrecord == true)
                    {
                        Debug.Assert(strID.Length == 10, "");

                        strCommand = " DELETE FROM `" + this.m_strSqlDbName + "`.records WHERE id like @id1 OR id = @id2";
                        command.CommandText = strCommand;
                        command.CommandTimeout = m_nLongTimeout;// 30����
                        command.Parameters.Clear();

                        MySqlParameter param1 = command.Parameters.Add("@id1",
                            MySqlDbType.String);
                        param1.Value = strID + "_%";

                        MySqlParameter param2 = command.Parameters.Add("@id2",
                            MySqlDbType.String);
                        param2.Value = strID;
                    }
                    else
                    {
                        strCommand = " DELETE FROM `" + this.m_strSqlDbName + "`.records WHERE id = @id";
                        command.CommandText = strCommand;
                        command.CommandTimeout = m_nLongTimeout;// 30����
                        command.Parameters.Clear();

                        MySqlParameter param = command.Parameters.Add("@id",
                            MySqlDbType.String);
                        param.Value = strID;
                    }

                    nDeletedCount = command.ExecuteNonQuery();
                    if (nDeletedCount != 1)
                    {
                        this.container.KernelApplication.WriteErrorLog("ϣ��ɾ��" + strID + " '1'����ʵ��ɾ��'" + Convert.ToString(nDeletedCount) + "'��");
                    }
                } // end of using command
            }
            else if (connection.SqlServerType == SqlServerType.Oracle)
            {
                int nExecuteCount = 0;
                using (OracleCommand command = new OracleCommand("",
                    connection.OracleConnection))
                {
                    string strCommand = "";

                    // ��һ������ö��ļ���
                    if (bDeleteObjectFiles == true)
                    {
                        if (row_info != null && bDeleteSubrecord == false)
                        {
                            // �������ͨ�� row_info ��ɾ����¼�Ķ����ļ�
                        }
                        else if (bDeleteSubrecord == true)
                        {
                            // TODO: ��Ҫ���ȫ��filename��newfilename�ֶ�����ֵ
                            Debug.Assert(strID.Length == 10, "");

                            strCommand = " SELECT filename, newfilename FROM " + this.m_strSqlDbName + "_records WHERE id like :id1 OR id = :id2";
                            command.CommandText = strCommand;
                            command.BindByName = true;
                            command.CommandTimeout = m_nLongTimeout;// 30����

                            OracleParameter param1 = command.Parameters.Add(":id1",
                                OracleDbType.NVarchar2);
                            param1.Value = strID + "_%";

                            OracleParameter param2 = command.Parameters.Add(":id2",
                                OracleDbType.NVarchar2);
                            param2.Value = strID;
                        }
                        else if (row_info == null)
                        {
                            strCommand = " SELECT filename, newfilename FROM " + this.m_strSqlDbName + "_records WHERE id = :id";
                            command.CommandText = strCommand;
                            command.BindByName = true;
                            command.CommandTimeout = m_nLongTimeout;// 30����

                            OracleParameter param = command.Parameters.Add(":id",
                                OracleDbType.NVarchar2);
                            param.Value = strID;
                        }
                    }

                    if (string.IsNullOrEmpty(strCommand) == false)
                    {
                        nExecuteCount++;
                        OracleDataReader dr = command.ExecuteReader();
                        if (dr != null
                            && dr.HasRows == true)
                        {
                            while (dr.Read())
                            {
                                if (dr.IsDBNull(0) == false)
                                    filenames.Add(dr.GetString(0));
                                if (dr.IsDBNull(1) == false)
                                    filenames.Add(dr.GetString(1));
                            }
                        }
                        if (dr != null)
                            dr.Close();
                        command.Parameters.Clear();
                    }

                    // �ڶ�����ɾ��SQL��
                    if (bDeleteSubrecord == true)
                    {
                        Debug.Assert(strID.Length == 10, "");

                        strCommand = " DELETE FROM " + this.m_strSqlDbName + "_records WHERE id like :id1 OR id = :id2";
                        command.CommandText = strCommand;
                        command.BindByName = true;
                        command.CommandTimeout = m_nLongTimeout;// 30����

                        OracleParameter param1 = command.Parameters.Add(":id1",
                            OracleDbType.NVarchar2);
                        param1.Value = strID + "_%";

                        OracleParameter param2 = command.Parameters.Add(":id2",
                            OracleDbType.NVarchar2);
                        param2.Value = strID;
                    }
                    else
                    {
                        strCommand = " DELETE FROM " + this.m_strSqlDbName + "_records WHERE id = :id";
                        command.CommandText = strCommand;
                        command.BindByName = true;
                        command.CommandTimeout = m_nLongTimeout;// 30����

                        OracleParameter param = command.Parameters.Add(":id",
                            OracleDbType.NVarchar2);
                        param.Value = strID;
                    }

                    nExecuteCount++;
                    nDeletedCount = command.ExecuteNonQuery();
                    if (nDeletedCount != 1)
                    {
                        this.container.KernelApplication.WriteErrorLog("ϣ��ɾ��" + strID + " '1'����ʵ��ɾ��'" + Convert.ToString(nDeletedCount) + "'��");
                    }
                    command.Parameters.Clear();
                } // end of using command

                /*
                // ����
                if (nExecuteCount == 0)
                {
                    Debug.Assert(false, "");
                }
                 * */
            }

            // ��������ɾ�������ļ�
            if (this.m_lObjectStartSize != -1)
            {
                if (row_info != null && bDeleteSubrecord == false)
                {
                    string strFilename1 = this.GetObjectFileName(row_info.FileName);
                    string strFileName2 = this.GetObjectFileName(row_info.NewFileName);
                    try
                    {
                        if (string.IsNullOrEmpty(strFilename1) == false)
                            File.Delete(strFilename1);
                        if (string.IsNullOrEmpty(strFileName2) == false)
                            File.Delete(strFileName2);
                    }
                    catch (Exception ex)
                    {
                        strError = "ɾ�����ݿ� '" + this.GetCaption("zh-CN") + "' �� IDΪ '" + strID + "' �Ķ����ļ�ʱ��������: " + ex.Message;
                        this.container.KernelApplication.WriteErrorLog(strError);
                        return -1;
                    }
                }
                else if (bDeleteSubrecord == true || row_info == null)
                {
                    foreach (string strShortFilename in filenames)
                    {
                        if (string.IsNullOrEmpty(strShortFilename) == true)
                            continue;

                        string strFilename = this.GetObjectFileName(strShortFilename);
                        try
                        {
                            if (string.IsNullOrEmpty(strFilename) == false)
                                File.Delete(strFilename);
                        }
                        catch (Exception ex)
                        {
                            strError = "ɾ�����ݿ� '" + this.GetCaption("zh-CN") + "' �� IDΪ '" + strID + "' �Ķ����ļ�ʱ��������: " + ex.Message;
                            this.container.KernelApplication.WriteErrorLog(strError);
                            return -1;
                        }
                    }
                }
            }

            return nDeletedCount;
        }


        Connection GetConnection(
            string strConnectionString,
            ConnectionStyle style = ConnectionStyle.None)
        {
            // SQLite ר��, ���ٵģ� ȫ�ֹ��õ�
            if ( ((style & ConnectionStyle.Global) == ConnectionStyle.Global)
                && this.SQLiteInfo != null) // && this.SQLiteInfo.FastMode == true
            {
                Debug.Assert(this.SQLiteInfo != null, "");

                lock (this.SQLiteInfo)
                {
                    if (this.SQLiteInfo.m_connection == null)
                    {
                        this.SQLiteInfo.m_connection = new Connection(this,
                            strConnectionString,
                            style);
                        return this.SQLiteInfo.m_connection;
                    }

                    return this.SQLiteInfo.m_connection;
                }
            }

            return new Connection(this,
                            strConnectionString,
                            style);
        }
    }

    // ��װ�������͵�Connection
    public class Connection
    {
        public SqlDatabase SqlDatabase = null;
        public SqlServerType SqlServerType = SqlServerType.None;
        object m_connection = null;
        bool m_bGlobal = false;
        internal IDbTransaction m_trans = null;

        ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim();
        int m_nLockTimeout = 5 * 1000;

        internal int m_nOpenCount = 0;
        internal int m_nThreshold = 1000;

        public void Clone(Connection connection)
        {
            this.SqlDatabase = connection.SqlDatabase;
            this.SqlServerType = connection.SqlServerType;
            this.m_connection = connection.m_connection;
            this.m_bGlobal = connection.m_bGlobal;
            this.m_lock = connection.m_lock;
            this.m_nLockTimeout = connection.m_nLockTimeout;
            this.m_nOpenCount = connection.m_nOpenCount;
        }

        /*
        public Connection(SqlServerType server_type,
            string strConnectionString)
        {
            this.SqlServerType = server_type;
            if (server_type == rms.SqlServerType.MsSqlServer)
                this.m_connection = new SqlConnection(strConnectionString);
            else if (server_type == rms.SqlServerType.SQLite)
                this.m_connection = new SQLiteConnection(strConnectionString);
            else
            {
                throw new Exception("��֧�ֵ����� " + server_type.ToString());
            }
        }
         * */

        public Connection(SqlDatabase database,
            string strConnectionString,
            ConnectionStyle style = ConnectionStyle.None)
        {
            this.SqlDatabase = database;
            this.SqlServerType = database.container.SqlServerType;

            if (this.m_nLockTimeout < this.SqlDatabase.m_nTimeOut)
                this.m_nLockTimeout = this.SqlDatabase.m_nTimeOut;

            if (this.SqlServerType == rms.SqlServerType.MsSqlServer)
                this.m_connection = new SqlConnection(strConnectionString);
            else if (this.SqlServerType == rms.SqlServerType.SQLite)
            {
#if NO
                // SQLite ר��, ���ٵģ� ȫ�ֹ��õ�
                if ((style & ConnectionStyle.Global) == ConnectionStyle.Global)
                {
                    Debug.Assert(this.SqlDatabase.SQLiteInfo != null, "");

                    lock (this.SqlDatabase.SQLiteInfo)
                    {
                        if (this.SqlDatabase.SQLiteInfo.FastMode == false)
                        {
                            this.m_connection = new SQLiteConnection(strConnectionString);
                            return;
                        }

                        if (this.SqlDatabase.SQLiteInfo.m_connection == null)
                        {
                            this.m_connection = new SQLiteConnection(strConnectionString);
                            this.m_bGlobal = true;
                            this.SqlDatabase.SQLiteInfo.m_connection = this;
                        }
                        else
                        {
                            // ���Ƴ�Ա
                            this.Clone(this.SqlDatabase.SQLiteInfo.m_connection);
                            if (this.m_nLockTimeout < this.SqlDatabase.m_nTimeOut)
                                this.m_nLockTimeout = this.SqlDatabase.m_nTimeOut;
                        }
                    }
                    return;
                }
#endif
                if ((style & ConnectionStyle.Global) == ConnectionStyle.Global)
                {
                    this.m_bGlobal = true;
                }
                this.m_connection = new SQLiteConnection(strConnectionString);
            }
            else if (this.SqlServerType == rms.SqlServerType.MySql)
                this.m_connection = new MySqlConnection(strConnectionString);
            else if (this.SqlServerType == rms.SqlServerType.Oracle)
                this.m_connection = new OracleConnection(strConnectionString);
            else
            {
                throw new Exception("��֧�ֵ����� " + this.SqlServerType.ToString());
            }
        }

        void SQLiteConnectionOpen()
        {
            #if REDO_OPEN
            int nRedoCount = 0;
            REDO:
            try
            {
                this.SQLiteConnection.Open();
            }
            catch (SQLiteException ex)
            {
                if (ex.ErrorCode == SQLiteErrorCode.Busy
                    && nRedoCount < 2)
                {
                    nRedoCount++;
                    goto REDO;
                }
                throw ex;
            }
#else
            this.SQLiteConnection.Open();
#endif

        }

        public void Open()
        {
            if (this.SqlServerType == rms.SqlServerType.MsSqlServer)
                this.SqlConnection.Open();
            else if (this.SqlServerType == rms.SqlServerType.SQLite)
            {
                if (this.m_bGlobal == false)
                {
                    this.SQLiteConnectionOpen();
                    return;
                }

                if (this.m_bGlobal == true)
                {
                    if (this.m_nLockTimeout < this.SqlDatabase.m_nTimeOut)
                        this.m_nLockTimeout = this.SqlDatabase.m_nTimeOut;

                    if (this.m_lock != null && this.m_lock.TryEnterWriteLock(this.m_nLockTimeout) == false)
                        throw new ApplicationException("ΪDatabaseȫ��Connection (Open) ��д��ʱʧ�ܡ�Timeout=" + this.m_nLockTimeout.ToString());

                    this.m_nOpenCount++;
                    if (this.m_nOpenCount > this.m_nThreshold)
                    {
                        this.m_nOpenCount = 0;
                        this.SqlDatabase.container.ActivateCommit();
                    }

                    if (this.SQLiteConnection.State == ConnectionState.Closed)
                    {
                        this.SQLiteConnectionOpen();

                        Debug.Assert(this.m_trans == null, ""); // ��Ҫ�������ύ��ǰ��Transaction ?

                        this.m_trans = this.SQLiteConnection.BeginTransaction();
                    }
                    else
                    {
                        if (this.m_trans == null)
                            this.m_trans = this.SQLiteConnection.BeginTransaction();
                    }
                }
            }
            else if (this.SqlServerType == rms.SqlServerType.MySql)
                this.MySqlConnection.Open();
            else if (this.SqlServerType == rms.SqlServerType.Oracle)
            {
                this.OracleConnection.Open();

#if NO
                int nRedoCount = 0;
            REDO_OPEN:
                try
                {
                    this.OracleConnection.Open();
                    if (this.OracleConnection.State != ConnectionState.Open)
                    {
                        if (nRedoCount <= 5)
                        {
                            nRedoCount++;
                            goto REDO_OPEN;
                        }
                        else
                        {
                            Debug.Assert(false, "");
                        }
                    }

                }
                catch (OracleException ex)
                {
                    if (ex.Errors.Count > 0 && ex.Errors[0].Number == 12520
                        && nRedoCount <= 0)
                    {
                        nRedoCount++;
                        this.OracleConnection.Close();
                        goto REDO_OPEN;
                    }

                    throw ex;
                }
#endif
            }
            else
            {
                throw new Exception("��֧�ֵ����� " + this.SqlServerType.ToString());
            }
        }


        // parameters:
        //      bAuto   �Ƿ��Զ��رա� false��ʾǿ�ƹر�
        public void Close(bool bAuto = true)
        {
            if (this.SqlServerType == rms.SqlServerType.MsSqlServer)
            {
                this.SqlConnection.Close();
                this.SqlConnection.Dispose();
            }
            else if (this.SqlServerType == rms.SqlServerType.SQLite)
            {
                // ��Ҫ����
                // ֻ��ǿ�ƹرգ�ȫ�ֵ�Connection���������ر�
                if (bAuto == false && this.m_bGlobal == true)
                {


                    // ǿ���ύ
                    if (this.m_lock != null && this.m_lock.TryEnterWriteLock(this.m_nLockTimeout) == false)
                        throw new ApplicationException("ΪDatabaseȫ��Connection (Commit) ��д��ʱʧ�ܡ�Timeout=" + this.m_nLockTimeout.ToString());
                    try
                    {
                        if (this.m_trans != null)
                        {
                            this.m_trans.Commit();
                            this.m_trans = null;

                            // this.m_nOpenCount = 0;
                        }

                        this.SQLiteConnection.Close();
                        this.SQLiteConnection.Dispose();
                    }
                    finally
                    {
                        if (this.m_lock != null)
                            this.m_lock.ExitWriteLock();
                    }
                    return;
                }

                if (m_bGlobal == true)
                {
                    if (this.m_lock != null)
                        this.m_lock.ExitWriteLock();
                }

                // �������İ汾
                // ����ȫ�ֵ�ÿ�ζ�Ҫ�ر�
                if (this.m_bGlobal == false)
                {
                    if (this.m_trans != null)
                    {
                        this.m_trans.Commit();
                        this.m_trans = null;
                    }
                    this.SQLiteConnection.Close();
                    this.SQLiteConnection.Dispose();
                }
            }
            else if (this.SqlServerType == rms.SqlServerType.MySql)
            {
                this.MySqlConnection.Close();
                this.MySqlConnection.Dispose();
            }
            else if (this.SqlServerType == rms.SqlServerType.Oracle)
            {
                /*
                using (OracleCommand command = new OracleCommand("select count(*) from v$session", this.OracleConnection))
                {
                    object result = command.ExecuteScalar();
                    Debug.WriteLine("session=" + result.ToString());
                }
                 * */

                this.OracleConnection.Close();
                this.OracleConnection.Dispose();
            }
            else
            {
                throw new Exception("��֧�ֵ����� " + this.SqlServerType.ToString());
            }
        }

        // parameters:
        //      bLock   �Ƿ���Ҫ������2013/3/2
        public void Commit(bool bLock = true)
        {
            if (this.SqlServerType == rms.SqlServerType.SQLite)
            {
                // ��Ҫ����
                // ֻ��ǿ�ƹرգ�ȫ�ֵ�Connection���������ر�
                if (this.m_bGlobal == true)
                {

                    // ǿ���ύ
                    if (bLock == true)
                    {
                        if (this.m_lock != null && this.m_lock.TryEnterWriteLock(this.m_nLockTimeout) == false)
                            throw new ApplicationException("ΪDatabaseȫ��Connection (Commit) ��д��ʱʧ�ܡ�Timeout=" + this.m_nLockTimeout.ToString());
                    }

                    try
                    {
                        if (this.m_trans != null)
                        {

                            this.m_trans.Commit();
                            this.m_trans = null;

                            /*
                            Debug.Assert(this.m_trans == null, "");
                            this.m_trans = this.SQLiteConnection.BeginTransaction();

                            this.m_nOpenCount = 0;
                             * */
                        }
                    }
                    finally
                    {
                        if (bLock == true)
                        {
                            if (this.m_lock != null)
                                this.m_lock.ExitWriteLock();
                        }
                    }
                    return;
                }

                // �������İ汾
                // ����ȫ�ֵ�
                if (this.m_bGlobal == false)
                {
                    if (this.m_trans != null)
                    {
                        this.m_trans.Commit();
                        this.m_trans = null;

                        Debug.Assert(this.m_trans == null, "");
                        this.m_trans = this.SQLiteConnection.BeginTransaction();
                    }
                }
            }
        }


        public SqlConnection SqlConnection
        {
            get
            {
                return (SqlConnection)m_connection;
            }
        }

        public SQLiteConnection SQLiteConnection
        {
            get
            {
                return (SQLiteConnection)m_connection;
            }
        }

        public MySqlConnection MySqlConnection
        {
            get
            {
                return (MySqlConnection)m_connection;
            }
        }

        public OracleConnection OracleConnection
        {
            get
            {
                return (OracleConnection)m_connection;
            }
        }
    }

    public class SQLiteInfo
    {
        public bool FastMode = false;    // �Ƿ�Ϊ����ģʽ
        internal Connection m_connection = null;
    }

    // flag
    public enum ConnectionStyle
    {
        None = 0,
        Global = 0x01,
    }
}
