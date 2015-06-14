using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Net;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Data.SqlClient;
using System.Web;

using DigitalPlatform.Xml;
using DigitalPlatform.IO;


namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// �Ͽ�Զ��һ��ͨ������Ϣͬ�� ����������
    /// </summary>
    public class DkywReplication : BatchTask
    {
        // �Ѿ�����˺�����ͬ�������ڡ����������������ڣ��ͱ���ͬһ������Ҳ��������
        string BlackListDoneDate = "";

        // internal AutoResetEvent eventDownloadFinished = new AutoResetEvent(false);	// true : initial state is signaled 
        // bool DownloadCancelled = false;
        // Exception DownloadException = null;

        // ���캯��
        public DkywReplication(LibraryApplication app,
            string strName)
            : base(app, strName)
        {
            this.Loop = true;

            this.PerTime = 5 * 60 * 1000;	// 5����
        }

        public override string DefaultName
        {
            get
            {
                return "�Ͽ�Զ��һ��ͨ������Ϣͬ��";
            }
        }



        // ���� ��ʼ ����
        // parameters:
        //      strStart    �����ַ�������ʽΪXML
        //                  ����Զ��ַ���Ϊ"!breakpoint"����ʾ�ӷ���������Ķϵ���Ϣ��ʼ
        int ParseDkywReplicationStart(string strStart,
            out string strRecordID,
            out string strError)
        {
            strError = "";
            strRecordID = "";

            // int nRet = 0;

            if (String.IsNullOrEmpty(strStart) == true)
            {
                // strError = "������������Ϊ��";
                // return -1;
                strRecordID = "1";
                return 0;
            }

            if (strStart == "!breakpoint")
            {
                /*
                // �Ӷϵ�����ļ��ж�����Ϣ
                // return:
                //      -1  error
                //      0   file not found
                //      1   found
                nRet = this.App.ReadBatchTaskBreakPointFile(
                    this.DefaultName,
                    out strStart,
                    out strError);
                if (nRet == -1)
                {
                    strError = "ReadBatchTaskBreakPointFileʱ����" + strError;
                    this.App.WriteErrorLog(strError);
                    return -1;
                }

                // ���nRet == 0����ʾû�жϵ��ļ����ڣ�Ҳ��û�б�Ҫ�Ĳ����������������
                if (nRet == 0)
                {
                    strError = "��ǰ������û�з��� " + this.DefaultName + " �ϵ���Ϣ���޷���������";
                    return -1;
                }

                Debug.Assert(nRet == 1, "");
                this.AppendResultText("����������� " + this.DefaultName + " �ϴζϵ��ַ���Ϊ: "
                    + HttpUtility.HtmlEncode(strStart)
                    + "\r\n");
                */
                strRecordID = strStart;
                return 0;
            }

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strStart);
            }
            catch (Exception ex)
            {
                strError = "װ��XML�ַ��� '"+strStart+"'����DOMʱ��������: " + ex.Message;
                return -1;
            }

            XmlNode nodeLoop = dom.DocumentElement.SelectSingleNode("loop");
            if (nodeLoop != null)
            {
                strRecordID = DomUtil.GetAttr(nodeLoop, "recordid");
            }

            return 0;
        }

        /*
        public static string MakeDkywReplicationParam(
    bool bLoop)
        {
            XmlDocument dom = new XmlDocument();

            dom.LoadXml("<root />");

            DomUtil.SetAttr(dom.DocumentElement, "loop",
                bLoop == true ? "yes" : "no");

            return dom.OuterXml;
        }*/


        // ����ͨ����������
        // ��ʽ
        /*
         * <root loop='...'/>
         * loopȱʡΪtrue
         * 
         * */
        public static int ParseDkywReplicationParam(string strParam,
            out bool bLoop,
            out string strError)
        {
            strError = "";
            bLoop = true;

            if (String.IsNullOrEmpty(strParam) == true)
                return 0;

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strParam);
            }
            catch (Exception ex)
            {
                strError = "strParam���� '"+strParam+"' װ��XML DOMʱ����: " + ex.Message;
                return -1;
            }

            // ȱʡΪtrue
            string strLoop = DomUtil.GetAttr(dom.DocumentElement,
    "loop");
            if (strLoop.ToLower() == "no"
                || strLoop.ToLower() == "false")
                bLoop = false;
            else
                bLoop = true;

            return 0;
        }


        public override void Worker()
        {
            // ϵͳ�����ʱ�򣬲����б��߳�
            // 2007/12/18
            if (this.App.HangupReason == HangupReason.LogRecover)
                return;
            // 2012/2/4
            if (this.App.PauseBatchTask == true)
                return;

            string strError = "";

            BatchTaskStartInfo startinfo = this.StartInfo;
            if (startinfo == null)
                startinfo = new BatchTaskStartInfo();   // ����ȱʡֵ��

            // ͨ����������
            bool bLoop = true;
            int nRet = ParseDkywReplicationParam(startinfo.Param,
                out bLoop,
                out strError);
            if (nRet == -1)
            {
                this.AppendResultText("����ʧ��: " + strError + "\r\n");
                return;
            }

            this.Loop = bLoop;

            string strID = "";
            nRet = ParseDkywReplicationStart(startinfo.Start,
                out strID,
                out strError);
            if (nRet == -1)
            {
                this.AppendResultText("����ʧ��: " + strError + "\r\n");
                this.Loop = false;
                return;
            }


            if (strID == "!breakpoint")
            {
                string strLastNumber = "";
                bool bTempLoop = false;

                nRet = ReadLastNumber(
                    out bTempLoop,
                    out strLastNumber,
                    out strError);
                if (nRet == -1)
                {
                    string strErrorText = "�Ӷϵ��ļ��л�ȡ������ʱ��������: " + strError;
                    this.AppendResultText(strErrorText + "\r\n");
                    this.App.WriteErrorLog(strErrorText);
                    return;
                }
                strID = strLastNumber;
            }

            try
            {
                // �������ļ�д����ӳ���ϵ�Ķ��߿�
                this.AppendResultText("ͬ���������ݿ�ʼ\r\n");

                string strMaxNumber = "";   // ���ز���ĩβ������
                try
                {
                    // return:
                    //      -1  error
                    //      0   succeed
                    //      1   �ж�
                    nRet = WriteToReaderDb(strID,
                        out strMaxNumber,
                        out strError);
                }
                finally
                {
                    // д���ļ��������Ѿ�������������
                    // Ҫ��bLoop������������������ֵ��������this.Loop ��Ϊ�ж�ʱ��ֵ�Ѿ����ı�
                    if (String.IsNullOrEmpty(strMaxNumber) == true)
                    {
                        // ������г�����߸���û����Դ��¼����һ��Ҳû�гɹ��������ͱ���ԭ���Ķϵ��¼��
                        // ���д��Ķϵ��¼���ǿգ��´����е�ʱ�򣬽���'1'��ʼ����һ���ǲ��ܽ��ܵ�
                        WriteLastNumber(bLoop, strID);
                    }
                    else
                        WriteLastNumber(bLoop, strMaxNumber);
                }

                if (nRet == -1)
                {
                    string strErrorText = "д����߿�: " + strError;
                    this.AppendResultText(strErrorText + "\r\n");
                    this.App.WriteErrorLog(strErrorText);
                    return;
                }
                else if (nRet == 1)
                {
                    this.AppendResultText("ͬ���������ݱ��ж�\r\n");
                    return;
                }
                else
                {
                    this.AppendResultText("ͬ�������������\r\n");
                    Debug.Assert(this.App != null, "");
                }

                this.AppendResultText("���ֺ�������ʼ\r\n");
                // ���������еĿ���ʧ����߿�
                // parameters:
                // return:
                //      -1  error
                //      0   succeed
                //      1   �ж�
                nRet = DoBlackList(out strError);
                if (nRet == -1)
                {
                    string strErrorText = "���ֺ�����: " + strError;
                    this.AppendResultText(strErrorText + "\r\n");
                    this.App.WriteErrorLog(strErrorText);
                    return;
                }
                else if (nRet == 1)
                {
                    this.AppendResultText("���ֺ��������ж�\r\n");
                    return;
                }
                else
                {
                    this.AppendResultText("���ֺ��������\r\n");
                    Debug.Assert(this.App != null, "");
                }
            }
            finally
            {
                this.StartInfo.Start = "!breakpoint"; // �Զ�ѭ����ʱ��û�к��룬Ҫ�Ӷϵ��ļ���ȡ��
            }
        }

        // new
        // ��ȡ�ϴ������ĺ���
        // parameters:
        //
        // return:
        //      -1  ����
        //      0   û���ҵ��ϵ���Ϣ
        //      1   �ҵ��˶ϵ���Ϣ
        public int ReadLastNumber(
            out bool bLoop,
            out string strLastNumber,
            out string strError)
        {
            bLoop = false;
            strLastNumber = "";
            strError = "";

            string strBreakPointString = "";
            // �Ӷϵ�����ļ��ж�����Ϣ
            // return:
            //      -1  error
            //      0   file not found
            //      1   found
            int nRet = this.App.ReadBatchTaskBreakPointFile(this.DefaultName,
                            out strBreakPointString,
                            out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;

            // return:
            //      -1  xml error
            //      0   not found
            //      1   found
            nRet = ParseBreakPointString(
                strBreakPointString,
                out bLoop,
                out strLastNumber);
            return 1;

            /*
            strError = "";
            strLastNumber = "";

            string strFileName = PathUtil.MergePath(this.App.DkywDir, "lastnumber.txt");

            StreamReader sr = null;

            try
            {
                sr = new StreamReader(strFileName, Encoding.UTF8);
            }
            catch (FileNotFoundException )
            {
                return 0;   // file not found
            }
            catch (Exception ex)
            {
                strError = "open file '" + strFileName + "' error : " + ex.Message;
                return -1;
            }
            try
            {
                strLastNumber = sr.ReadLine();  // ����ʱ����
            }
            finally
            {
                sr.Close();
            }

            return 1;
             * */
        }

        // ����ϵ��ַ���
        static string MakeBreakPointString(
            bool bLoop,
            string strRecordID)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            DomUtil.SetElementText(dom.DocumentElement,
                "recordID",
                strRecordID);
            DomUtil.SetElementText(dom.DocumentElement,
                "loop",
                bLoop == true ? "true" : "false");

            return dom.OuterXml;
        }

        // return:
        //      -1  xml error
        //      0   not found
        //      1   found
        static int ParseBreakPointString(
            string strBreakPointString,
            out bool bLoop,
            out string strRecordID)
        {
            bLoop = false;
            strRecordID = "";

            if (String.IsNullOrEmpty(strBreakPointString) == true)
                return 0;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strBreakPointString);
            }
            catch
            {
                return -1;
            }

            string strLoop = DomUtil.GetElementText(dom.DocumentElement,
                "loop");
            if (strLoop == "true")
                bLoop = true;

            strRecordID = DomUtil.GetElementText(dom.DocumentElement,
                "recordID");

            return 1;
        }

        // new
        // д��ϵ�����ļ�
        public void WriteLastNumber(
            bool bLoop,
            string strLastNumber)
        {
            string strBreakPointString = MakeBreakPointString(bLoop, strLastNumber);

            // д��ϵ��ļ�
            this.App.WriteBatchTaskBreakPointFile(this.DefaultName,
                strBreakPointString);

            /*
            string strFileName = PathUtil.MergePath(this.App.DkywDir, "lastnumber.txt");

            // ɾ��ԭ�����ļ�
            File.Delete(strFileName);

            // д��������
            StreamUtil.WriteText(strFileName,
                strLastNumber);
             * */
        }

        // ��������ֵ�
        // parameters:
        //      strValueFieldName   ֵ�ֶ��������Ϊ���ŷָ����̬����ʾҪ�������ֶ�ֵȡ����ƴ����һ��
        int GetDictionary(string strTableName,
            string strCodeFieldName,
            string strValueFieldNames,
            out Hashtable result,
            out string strError)
        {
            strError = "";

            string [] value_field_names = strValueFieldNames.Split(new char[] {','});

            result = new Hashtable();

            XmlNode node = this.App.LibraryCfgDom.DocumentElement.SelectSingleNode("//dkyw/dataCenter");
            if (node == null)
            {
                strError = "��δ����<dkyw><dataCenter>����";
                return -1;
            }
            string strConnectionString = DomUtil.GetAttr(node, "connection");
            if (String.IsNullOrEmpty(strConnectionString) == true)
            {
                strError = "��δ����<dkyw/dataCenter>Ԫ�ص�connection����";
                return -1;
            }

            string strDbName = DomUtil.GetAttr(node, "db");
            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "��δ����<dkyw/dataCenter>Ԫ�ص�db����";
                return -1;
            }


            SqlConnection connection = new SqlConnection(strConnectionString);
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                strError = "���ӵ�SQL������ʧ��: " + ex.Message;
                return -1;
            }

            try
            {
                SqlCommand command = null;
                SqlDataReader dr = null;

                string strCommand = "";

                strCommand = "use " + strDbName + "\r\nselect * from " + strTableName;
                command = new SqlCommand(strCommand,
                    connection);
                try
                {
                    dr = command.ExecuteReader();
                }
                catch (Exception ex)
                {
                    strError = "��ѯSQLʱ����: "
                        + ex.Message + "; "
                        + "SQL����: "
                        + strCommand;
                    return -1;
                }

                for (; ;)
                {
                    Thread.Sleep(1);    // ���⴦��̫��æ

                    if (this.Stopped == true)
                    {
                        return 1;
                    }

                    try
                    {
                        if (dr == null || dr.HasRows == false)
                        {
                            return 0;
                        }
                        if (dr.Read() == false)
                            break;
                    }
                    catch (Exception ex)
                    {
                        strError = "��SQL���з�������: " + ex.Message;
                        return -1;
                    }

                    // ����ֶ�ֵ
                    string strCode = GetSqlStringValue(dr, strCodeFieldName);
                    strCode = strCode.Trim();
                    string strValue = "";

                    List<string> temp_values = new List<string>();
                    for (int i = 0; i < value_field_names.Length; i++)
                    {
                        string strText = GetSqlStringValue(dr, value_field_names[i]);
                        strText = strText.Trim();

                        temp_values.Add(strText.Trim());
                    }

                    // ȥ��ĩβ�����Ŀ��ַ���
                    for (int i = temp_values.Count-1; i > 0; i--)
                    {
                        if (String.IsNullOrEmpty(temp_values[i]) == true)
                            temp_values.RemoveAt(i);
                        else
                            break;
                    }

                    for (int i = 0; i < temp_values.Count; i++)
                    {
                        if (i > 0)
                        {
                            strValue += ", ";
                        }
                        strValue += temp_values[i];
                    }

                    result[strCode] = strValue;
                }
                return 0;
            }
            catch (Exception ex)
            {
                strError = "GetDictionary() Exception: " + ExceptionUtil.GetDebugText(ex);
                return -1;
            }
            finally
            {
            }
        }

        // �������߼�¼XML
        int BuildReaderXml(
            string strBarcode,
            string strName,
            string strGender,
            // string strReaderType,
            string strDepartment,
            string strPost,
            string strBornDate,
            string strIdCardNumber,
            string strAddress,
            string strComment,
            string strCreateDate,
            out string strXml,
            out string strError)
        {
            strError = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            DomUtil.SetElementText(dom.DocumentElement,
                "barcode", strBarcode);
            /*
            DomUtil.SetElementText(dom.DocumentElement,
                "state", strState);
             * */
            DomUtil.SetElementText(dom.DocumentElement,
                "name", strName);
            DomUtil.SetElementText(dom.DocumentElement,
                "gender", strGender);
            /*
            DomUtil.SetElementText(dom.DocumentElement,
                "readerType", strReaderType);
             * */
            DomUtil.SetElementText(dom.DocumentElement,
                "department", strDepartment);
            DomUtil.SetElementText(dom.DocumentElement,
                "post", strPost);
            DomUtil.SetElementText(dom.DocumentElement,
                "bornDate", strBornDate);
            DomUtil.SetElementText(dom.DocumentElement,
                "idCardNumber", strIdCardNumber);
            if (String.IsNullOrEmpty(strAddress) == false)
            {
                DomUtil.SetElementText(dom.DocumentElement,
                    "address", strAddress);
            }
            if (String.IsNullOrEmpty(strComment) == false)
            {
                DomUtil.SetElementText(dom.DocumentElement,
                    "comment", strComment);
            }
            DomUtil.SetElementText(dom.DocumentElement,
                "createDate", strCreateDate);

            strXml = dom.DocumentElement.OuterXml;

            return 0;
        }

        static string GetSqlStringValue(SqlDataReader dr,
            string strFieldName)
        {
            if (dr[strFieldName] is System.DBNull)
                return "";

            return (string)dr[strFieldName];
        }

        static int GetSqlIntValue(SqlDataReader dr,
    string strFieldName)
        {
            if (dr[strFieldName] is System.DBNull)
                return 0;

            return (int)dr[strFieldName];
        }


        // ���û���Ϣ���±�(User_Infor_Message)д����߿�
        // parameters:
        //      strLastNumber   ���Ϊ�գ���ʾȫ������
        // return:
        //      -1  error
        //      0   succeed
        //      1   �ж�
        int WriteToReaderDb(string strLastNumber,
            out string strMaxNumber,
            out string strError)
        {
            strError = "";
            strMaxNumber = "";
            int nRet = 0;

            /*
    <dkyw>
        <dataCenter connection="Persist Security Info=False;User ID=dp2rms;Password=dp2rms;Data Source=test111;Connect Timeout=30" db="zzdy" startTime="20:00" />
        <replication mapDbName="����" />
    </dkyw>
             * */

            XmlNode node = this.App.LibraryCfgDom.DocumentElement.SelectSingleNode("//dkyw/replication");
            if (node == null)
            {
                strError = "��δ����<dkyw><replication>����";
                return -1;
            }

            string strReaderDbName = DomUtil.GetAttr(node, "mapDbName");
            if (String.IsNullOrEmpty(strReaderDbName) == true)
            {
                strError = "��δ����<dkyw/replication>Ԫ�ص�mapDbName����";
                return -1;
            }

            node = this.App.LibraryCfgDom.DocumentElement.SelectSingleNode("//dkyw/dataCenter");
            if (node == null)
            {
                strError = "��δ����<dkyw><dataCenter>����";
                return -1;
            }
            string strConnectionString = DomUtil.GetAttr(node, "connection");
            if (String.IsNullOrEmpty(strConnectionString) == true)
            {
                strError = "��δ����<dkyw/dataCenter>Ԫ�ص�connection����";
                return -1;
            }

            string strDbName = DomUtil.GetAttr(node, "db");
            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "��δ����<dkyw/dataCenter>Ԫ�ص�db����";
                return -1;
            }

            // ��ݴ����ֵ�
            Hashtable pid_table = null;
            nRet = GetDictionary("Pid_Ctrl",
                "PID",
                "PNAME",
                out pid_table,
                out strError);
            if (nRet == -1)
                return -1;

            // ���Ŵ����ֵ�
            Hashtable dept_table = null;
            nRet = GetDictionary("Dept_Ctrl",
                "DeptStr",
                "DeptName1,DeptName2,DeptName3,DeptName4,DeptName5",
                out dept_table,
                out strError);
            if (nRet == -1)
                return -1;

            // ְλ(��λ)�����ֵ�
            Hashtable job_table = null;
            nRet = GetDictionary("Job",
                "Code",
                "Name",
                out job_table,
                out strError);
            if (nRet == -1)
                return -1;

            SqlConnection connection = new SqlConnection(strConnectionString);
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                strError = "���ӵ�SQL������ʧ��: " + ex.Message;
                return -1;
            }

            try
            {
                SqlCommand command = null;
                SqlDataReader dr = null;

                string strCommand = "";

                strCommand = "use " + strDbName + "\r\nselect * from User_Infor_Message";
                if (String.IsNullOrEmpty(strLastNumber) == false)
                {
                    strCommand += " where IDNumber > " + strLastNumber;
                }

                strCommand += " order by IDNumber";

                command = new SqlCommand(strCommand,
                    connection);
                try
                {
                    dr = command.ExecuteReader();
                }
                catch (Exception ex)
                {
                    strError = "��ѯSQLʱ����: "
                        + ex.Message + "; "
                        + "SQL����: "
                        + strCommand;
                    return -1;
                }

                // bool bRet = false;

                // ��ʱ��SessionInfo����
                SessionInfo sessioninfo = new SessionInfo(this.App);

                // ģ��һ���˻�
                Account account = new Account();
                account.LoginName = "replication";
                account.Password = "";
                account.Rights = "setreaderinfo,devolvereaderinfo";

                account.Type = "";
                account.Barcode = "";
                account.Name = "replication";
                account.UserID = "replication";
                account.RmsUserName = this.App.ManagerUserName;
                account.RmsPassword = this.App.ManagerPassword;

                sessioninfo.Account = account;

                int nRecordCount = 0;
                for (int i = 0; ; i++)
                {
                    Thread.Sleep(1);    // ���⴦��̫��æ

                    if (this.Stopped == true)
                    {
                        return 1;
                    }

                    try
                    {
                        if (dr == null || dr.HasRows == false)
                        {
                            break;
                        }
                        if (dr.Read() == false)
                            break;
                    }
                    catch (Exception ex)
                    {
                        strError = "��SQL���з�������: " + ex.Message;
                        return -1;
                    }

                    // ����ֶ�ֵ
                    int nIDNumber = GetSqlIntValue(dr, "IDNumber");

                    this.SetProgressText("ͬ�� " + (i + 1).ToString() + " IDNumber=" + nIDNumber.ToString());
                    this.AppendResultText("ͬ�� " + (i + 1).ToString() + " IDNumber=" + nIDNumber.ToString() + "\r\n");

                    string strMessageType = GetSqlStringValue(dr,"MessageType");
                    string strCardNo = GetSqlStringValue(dr,"CARDNO");
                    string strCardID = GetSqlStringValue(dr,"CARDID");
                    string strOldCardNo = GetSqlStringValue(dr,"OLDCARDNO");
                    string strOldCardID = GetSqlStringValue(dr,"OLDCARDID");
                    string strCardType = GetSqlStringValue(dr,"CDTYPE");
                    string strUserName = GetSqlStringValue(dr,"USERNAME");
                    string strIdType = GetSqlStringValue(dr,"IDTYPE");
                    string strIdSerial = GetSqlStringValue(dr,"IDSERIAL");
                    string strPersonID = GetSqlStringValue(dr,"PID");
                    string strDepartmentCode = GetSqlStringValue(dr,"DEPTSTR");
                    string strCountryCode = GetSqlStringValue(dr,"CTRCODE");
                    string strNationCode = GetSqlStringValue(dr,"NATCODE");
                    string strSex = GetSqlStringValue(dr,"SEX");
                    string strBirthday = GetSqlStringValue(dr,"BIRTHDAY");
                    string strInSchoolDate = GetSqlStringValue(dr,"INSCHOOL");
                    string strJobCode = GetSqlStringValue(dr,"JOBCODE");
                    string strRecType = GetSqlStringValue(dr,"RECTYPE");
                    string strGrade = GetSqlStringValue(dr,"GRADE");
                    string strIdSerial1 = GetSqlStringValue(dr,"IDSERIAL1");
                    string strOtherString = GetSqlStringValue(dr,"OTHERSTR");


                    // ���������ַ���
                    if (String.IsNullOrEmpty(strOldCardNo) == false)
                        strOldCardNo = strOldCardNo.Trim().PadLeft(8, '0');

                    string strXml = "";
                    string strBarcode = strCardNo.Trim().PadLeft(8, '0');;

                    string strRfc1123Birthday = "";

                    if (String.IsNullOrEmpty(strBirthday) == false)
                    {
                        nRet = DateTimeUtil.Date8toRfc1123(strBirthday,
                            out strRfc1123Birthday,
                            out strError);
                        if (nRet == -1)
                            return -1;
                    }

                    string strCreateDate = "";

                    if (String.IsNullOrEmpty(strInSchoolDate) == false)
                    {
                        nRet = DateTimeUtil.Date8toRfc1123(strInSchoolDate,
                            out strCreateDate,
                            out strError);
                        if (nRet == -1)
                            return -1;
                    }

                    // �����¼��
                    nRet = BuildReaderXml(
                        strBarcode,
                        strUserName,
                        strSex == "0" ? "Ů" : "��",
                        // (string)pid_table[strPersonID.Trim()],
                        (string)dept_table[strDepartmentCode.Trim()],
                        (string)pid_table[strPersonID.Trim()],  // (string)job_table[strJobCode.Trim()],
                        strRfc1123Birthday,
                        strIdSerial1,   // ���֤��
                        strOtherString, // address
                        "", // comment
                        strCreateDate,
                        out strXml,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    /*
                    if (nIDNumber > 200)
                    {
                        strError = "ģ�����";
                        return -1;
                    }*/

                    if (strMessageType == "3")
                    {
                        // ���л�������
                        // parameters:
                        //      strOriginReaderXml  ԭʼ��¼�������<barcode>Ԫ��ֵΪ�µĿ���
                        //      strOldCardNo    �ɵĿ��š�
                        // return:
                        //      -1  error
                        //      0   �Ѿ�д��
                        //      1   û�б�Ҫд��
                        nRet = DoChangeCard(
                            sessioninfo,
                            strOldCardNo,
                            strReaderDbName,
                            strXml,
                            out strError);
                    }
                    else
                    {
                        // return:
                        //      -1  error
                        //      0   �Ѿ�д��
                        //      1   û�б�Ҫд��
                        nRet = WriteOneReaderInfo(
                            sessioninfo,
                            strMessageType,
                            strReaderDbName,
                            strXml,
                            out strError);
                    }

                    if (nRet == -1)
                        return -1;

                    // ��¼������ɵļ�¼ID
                    strMaxNumber = nIDNumber.ToString();

                    nRecordCount++;
                }

                this.SetProgressText("ͬ�����߼�¼��ɣ�ʵ�ʴ����¼ " + nRecordCount.ToString() + " ��");

                if (nRecordCount == 0)
                {
                    if (String.IsNullOrEmpty(strLastNumber) == false)
                        this.AppendResultText("û�д��ڼ�¼�� " + strLastNumber + "���κ��¼�¼\r\n");
                    else
                        this.AppendResultText("û���κμ�¼\r\n");
                }

                return 0;
            }
            catch (Exception ex)
            {
                strError = "WriteToReaderDb() Exception: " + ExceptionUtil.GetDebugText(ex);
                return -1;
            }
            finally
            {
            }
        }


        // ���л�������
        // parameters:
        //      strOriginReaderXml  ԭʼ��¼�������<barcode>Ԫ��ֵΪ�µĿ���
        //      strOldCardNo    �ɵĿ��š�
        // return:
        //      -1  error
        //      0   �Ѿ�д��
        //      1   û�б�Ҫд��
        int DoChangeCard(
            SessionInfo sessioninfo,
            string strOldCardNo,
            string strReaderDbName,
            string strOriginReaderXml,
            out string strError)
        {
            strError = "";

            string strOperType = "replace"; // replace -- ������ change -- �޸��¼�¼�� new -- �����¼�¼

            bool bNewRecordWrited = false;  // �¼�¼�����Ƿ��Ѿ�д��

            // ������
            if (String.IsNullOrEmpty(strOldCardNo) == true)
            {
                strError = "��������Ϊ ���� ʱ��strOldCardNo����ֵ����Ϊ��";
                return -1;
            }

            XmlDocument origin_dom = new XmlDocument();

            try
            {
                origin_dom.LoadXml(strOriginReaderXml);
            }
            catch (Exception ex)
            {
                strError = "ԭʼXMLƬ��װ��DOMʧ��: " + ex.Message;
                return -1;
            }

            string strNewState = "";

            string strNewBarcode = DomUtil.GetElementText(origin_dom.DocumentElement,
                "barcode");
            if (String.IsNullOrEmpty(strNewBarcode) == true)
            {
                strError = "ȱ��<barcode>Ԫ��";
                return -1;
            }

            string strExistingXml = "";
            string strSavedXml = "";
            string strSavedRecPath = "";
            byte[] baNewTimestamp = null;
            DigitalPlatform.rms.Client.rmsws_localhost.ErrorCodeValue kernel_errorcode = DigitalPlatform.rms.Client.rmsws_localhost.ErrorCodeValue.NoError;

            /*
             * ��һ����ת����ͨ��Ϣ
             * */
            if (strNewBarcode != strOldCardNo)
            {
            REDO_DEVOLVE:
                // ת�ƽ�����Ϣ
                // ��Դ���߼�¼�е�<borrows>��<overdues>ת�Ƶ�Ŀ����߼�¼��
                // result.Value:
                //      -1  error
                //      0   û�б�Ҫת�ơ���Դ���߼�¼��û����Ҫת�ƵĽ�����Ϣ
                //      1   �Ѿ��ɹ�ת��
                LibraryServerResult result1 = this.App.DevolveReaderInfo(
                    sessioninfo,
                    strOldCardNo,
                    strNewBarcode);
                if (result1.Value == -1)
                {
                    if (result1.ErrorCode == ErrorCode.SourceReaderBarcodeNotFound)
                    {
                        // Դ��¼û���ҵ�������´���Ŀ����߼�¼�Ĳ���
                        strOperType = "create";
                    }
                    else if (result1.ErrorCode == ErrorCode.TargetReaderBarcodeNotFound)
                    {
                        // Ŀ���¼û���ҵ�����Ҫ�ȴ���Ŀ�꣬Ȼ�����½����ƶ�
                        LibraryServerResult result = this.App.SetReaderInfo(
                                sessioninfo,
                                "new",
                                strReaderDbName + "/?",
                                origin_dom.OuterXml,
                                "", // strReaderXml,
                                null,   // baTimestamp,
                                out strExistingXml,
                                out strSavedXml,
                                out strSavedRecPath,
                                out baNewTimestamp,
                                out kernel_errorcode);
                        if (result.Value == -1)
                        {
                            strError = "������ʱ�����¼�¼�����ڣ��ȴ����¼�¼������̷�������" + result.ErrorInfo;
                            return -1;
                        }

                        bNewRecordWrited = true;

                        goto REDO_DEVOLVE;
                    }
                    else
                    {
                        strError = "���������У�ת����ͨ��Ϣ('"+strOldCardNo+"' --> '"+strNewBarcode+"')��ʱ�����" + result1.ErrorInfo;
                        return -1;
                    }
                }
            }
            else
            {
                // Ҫ�ĵľɺź��º���ͬ
                // ��Ϊ��ֻд���¼�¼��
                strOperType = "change";
            }

            int nRet = 0;
            string strReaderXml = "";
            string strOutputPath = "";
            byte[] baTimestamp = null;

            /*
             * �ڶ�����ɾ��Դ��¼
             * */
            if (strOperType == "replace")
            {
                // �Ӷ���
                // ���Ա����õ����߼�¼������;����ʱ״̬
                this.App.ReaderLocks.LockForRead(strOldCardNo);
                try
                {
                    // ��ÿ��е�Ŀ����߼�¼
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   ����1��
                    //      >1  ���ж���1��
                    nRet = this.App.GetReaderRecXml(
                        this.RmsChannels, // sessioninfo.Channels,
                        strOldCardNo,
                        out strReaderXml,
                        out strOutputPath,
                        out baTimestamp,
                        out strError);

                }
                finally
                {
                    this.App.ReaderLocks.UnlockForRead(strOldCardNo);
                }

                if (nRet == -1)
                {
                    strError = "���������У���þɼ�¼ '" + strOldCardNo + "' ʱ����: " + strError;
                    return -1;
                }

                if (nRet > 1)
                {
                    strError = "����� " + strOldCardNo + "�ڶ��߿�Ⱥ�м������� " + nRet.ToString() + " �����뾡������˴���";
                    return -1;
                }

                // ���ĺ�
                // ����Ҫɾ���ɿ�

                XmlDocument temp_dom = new XmlDocument();
                try
                {
                    temp_dom.LoadXml(strReaderXml);
                }
                catch (Exception ex)
                {
                    strError = "����XML��¼װ��DOM��������: " + ex.Message;
                    return -1;
                }

                /*
                DomUtil.SetElementInnerXml(temp_dom.DocumentElement,
                    "barcode", strOldCardNo);
                 * */

                LibraryServerResult result = this.App.SetReaderInfo(
                    sessioninfo,
                    "delete",
                    "", //        strRecPath,
                    "", //        strNewXml,
                    temp_dom.OuterXml,  // strOldXml
                    null,   // baTimestamp,
                    out strExistingXml,
                    out strSavedXml,
                    out strSavedRecPath,
                    out baNewTimestamp,
                    out kernel_errorcode);
                if (result.Value == -1)
                {
                    if (result.ErrorCode == ErrorCode.ReaderBarcodeNotFound)
                    {
                        // ��¼�Ѿ�������
                    }
                    else if (result.ErrorCode == ErrorCode.HasCirculationInfo)
                    {
                        // ���������̫���ܷ�������Ϊת�Ʋ����Ѿ��Ѿɿ�����ͨ��Ϣ�����
                        // TODO: �Ƿ�Ҫ����?

                        // ���߼�¼��������ͨ��Ϣ
                        // ��Ϊ�޸ļ�¼
                        // ��״̬�޸�Ϊ��ɾ���������ǲ�ɾ����¼
                        strNewState = "ɾ��";
                        DomUtil.SetElementText(origin_dom.DocumentElement,
                            "state",
                            strNewState);

                        // ��Ȼ���޸�Ŀ���¼

                        strOperType = "change";
                    }
                    else
                    {
                        strError = "���������У�ɾ��Դ��¼ '"+strOldCardNo+"' ʱ����: " + result.ErrorInfo;
                        return -1;
                    }
                }
            }

            // �¼�¼��ǰ���Ѿ�д��
            if (bNewRecordWrited == true)
                return 0;

            /*
             * �ڶ������޸�Ŀ���¼
             * */

            // �Ӷ���
            // ���Ա����õ����߼�¼������;����ʱ״̬
            this.App.ReaderLocks.LockForRead(strNewBarcode);

            try
            {
                // ��ÿ��е�Ŀ����߼�¼
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                nRet = this.App.GetReaderRecXml(
                    this.RmsChannels, // sessioninfo.Channels,
                    strNewBarcode,
                    out strReaderXml,
                    out strOutputPath,
                    out baTimestamp,
                    out strError);

            }
            finally
            {
                this.App.ReaderLocks.UnlockForRead(strNewBarcode);
            }

            if (nRet == -1)
                return -1;
            if (nRet > 1)
            {
                strError = "����� " + strNewBarcode + "�ڶ��߿�Ⱥ�м������� " + nRet.ToString() + " �����뾡������˴���";
                return -1;
            }

            string strAction = "";
            string strRecPath = "";

            string strNewXml = "";  // �޸ĺ�ļ�¼

            if (nRet == 0)
            {
                // ��¼������

                // û�����У������¼�¼
                strAction = "new";
                strRecPath = strReaderDbName + "/?";
                strReaderXml = "";  // "<root />";
            }
            else
            {
                // ��¼����

                Debug.Assert(nRet == 1, "");

                strAction = "change";
                strRecPath = strOutputPath;
            }

            XmlDocument readerdom = new XmlDocument();
            try
            {
                readerdom.LoadXml(String.IsNullOrEmpty(strReaderXml) == false ? strReaderXml : "<root />");
            }
            catch (Exception ex)
            {
                strError = "����XML��¼װ��DOM��������: " + ex.Message;
                return -1;
            }

            // ��������SQL��������޸Ļ��ߴ�����¼
            // return:
            //      -1  error
            //      0   û�з����޸�
            //      1   �������޸�
            nRet = ModifyReaderRecord(ref readerdom,
                origin_dom,
                out strError);
            if (nRet == -1)
                return -1;

            if (nRet == 0) // û�з����޸ģ�û�б�Ҫд��
            {
                return 1;
            }

            // �޸�Ŀ����߼�¼
            {
                strNewXml = readerdom.OuterXml;

                LibraryServerResult result = this.App.SetReaderInfo(
                        sessioninfo,
                        strAction,
                        strRecPath,
                        strNewXml,
                        strReaderXml,
                        baTimestamp,
                        out strExistingXml,
                        out strSavedXml,
                        out strSavedRecPath,
                        out baNewTimestamp,
                        out kernel_errorcode);
                if (result.Value == -1)
                {
                    strError = "���������У��޸�Ŀ���¼ʱ����: " + result.ErrorInfo;
                    return -1;
                }
            }

            return 0;   // ����д����
        }

        // �����������޸ġ�ɾ���Ĳ���
        // parameters:
        //      strOperType     0 ���� 1 ɾ�� 2 �޸� 3 ����
        // return:
        //      -1  error
        //      0   �Ѿ�д��
        //      1   û�б�Ҫд��
        int WriteOneReaderInfo(
            SessionInfo sessioninfo,
            string strOperType,
            string strReaderDbName,
            string strOriginReaderXml,
            out string strError)
        {
            strError = "";

            // ������
            if (strOperType == "3")
            {
                strError = "��������Ϊ '" + strOperType + "' (����) ʱ�����ܵ���WriteOneReaderInfo()����";
                return -1;
            }

            XmlDocument origin_dom = new XmlDocument();

            try
            {
                origin_dom.LoadXml(strOriginReaderXml);
            }
            catch (Exception ex)
            {
                strError = "ԭʼXMLƬ��װ��DOMʧ��: " + ex.Message;
                return -1;
            }

            string strNewState = "";

            string strBarcode = DomUtil.GetElementText(origin_dom.DocumentElement,
                "barcode");
            if (String.IsNullOrEmpty(strBarcode) == true)
            {
                strError = "ȱ��<barcode>Ԫ��";
                return -1;
            }

            int nRet = 0;
            string strReaderXml = "";
            string strOutputPath = "";
            byte[] baTimestamp = null;

            // �Ӷ���
            // ���Ա����õ����߼�¼������;����ʱ״̬
            this.App.ReaderLocks.LockForRead(strBarcode);

            try
            {
                // ��ö��߼�¼
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                nRet = this.App.GetReaderRecXml(
                    this.RmsChannels, // sessioninfo.Channels,
                    strBarcode,
                    out strReaderXml,
                    out strOutputPath,
                    out baTimestamp,
                    out strError);

            }
            finally
            {
                this.App.ReaderLocks.UnlockForRead(strBarcode);
            }

            if (nRet == -1)
                return -1;
            if (nRet > 1)
            {
                strError = "����� " + strBarcode + "�ڶ��߿�Ⱥ�м������� " + nRet.ToString() + " �����뾡������˴���";
                return -1;
            }

            string strAction = "";
            string strRecPath = "";

            string strNewXml = "";  // �޸ĺ�ļ�¼


            string strExistingXml = "";
            string strSavedXml = "";
            string strSavedRecPath = "";
            byte[] baNewTimestamp = null;
            DigitalPlatform.rms.Client.rmsws_localhost.ErrorCodeValue kernel_errorcode = DigitalPlatform.rms.Client.rmsws_localhost.ErrorCodeValue.NoError;


            if (nRet == 0)
            {
                // ��¼������

                if (strOperType == "1")
                    return 0;   // �����ɾ�������������ݿ�������û�У�������

                // û�����У������¼�¼
                strAction = "new";
                strRecPath = strReaderDbName + "/?";
                strReaderXml = "";  // "<root />";
            }
            else
            {
                // ��¼����

                Debug.Assert(nRet == 1, "");
                // ���У��޸ĺ󸲸�ԭ��¼

                // ɾ����
                if (strOperType == "1")
                {
                    strAction = "delete";
                    strRecPath = strOutputPath;

                    LibraryServerResult result = this.App.SetReaderInfo(
                        sessioninfo,
                        strAction,
                        strRecPath,
                        "", //        strNewXml,
                        strReaderXml,
                        baTimestamp,
                        out strExistingXml,
                        out strSavedXml,
                        out strSavedRecPath,
                        out baNewTimestamp,
                        out kernel_errorcode);
                    if (result.Value == -1)
                    {
                        if (result.ErrorCode == ErrorCode.ReaderBarcodeNotFound)
                        {
                            // ��¼�Ѿ�������
                            return 0;
                        }
                        else if (result.ErrorCode == ErrorCode.HasCirculationInfo)
                        {
                            // ���߼�¼��������ͨ��Ϣ
                            // ��Ϊ�޸ļ�¼
                            // ��״̬�޸�Ϊ��ɾ���������ǲ�ɾ����¼
                            strNewState = "ɾ��";
                            DomUtil.SetElementText(origin_dom.DocumentElement,
                                "state",
                                strNewState);

                            strOperType = "2";
                            strAction = "change";
                            strRecPath = strOutputPath;

                            // TODO: �ڲ�����־��д��һ������ͼ���Ա�߸ö��߻��飿
                        }
                        else
                            return -1;
                    }
                    else
                        return 0;
                }
                else
                {
                    strAction = "change";
                    strRecPath = strOutputPath;
                }
            }

            XmlDocument readerdom = new XmlDocument();
            try
            {
                readerdom.LoadXml(String.IsNullOrEmpty(strReaderXml) == false ? strReaderXml : "<root />");
            }
            catch (Exception ex)
            {
                strError = "����XML��¼װ��DOM��������: " + ex.Message;
                return -1;
            }

            // ��������SQL��������޸Ļ��ߴ�����¼
            // return:
            //      -1  error
            //      0   û�з����޸�
            //      1   �������޸�
            nRet = ModifyReaderRecord(ref readerdom,
                origin_dom,
                out strError);
            if (nRet == -1)
                return -1;

            if (nRet == 0) // û�з����޸ģ�û�б�Ҫд��
            {
                return 1;
            }

            // �޸Ķ��߼�¼
            {
                strNewXml = readerdom.OuterXml;

                LibraryServerResult result = this.App.SetReaderInfo(
                        sessioninfo,
                        strAction,
                        strRecPath,
                        strNewXml,
                        strReaderXml,
                        baTimestamp,
                        out strExistingXml,
                        out strSavedXml,
                        out strSavedRecPath,
                        out baNewTimestamp,
                        out kernel_errorcode);
                if (result.Value == -1)
                {
                    strError = result.ErrorInfo;
                    return -1;
                }
            }

            return 0;   // ����д����
        }



        // ��������SQL��������޸Ļ��ߴ�����¼
        // return:
        //      -1  error
        //      0   û�з����޸�
        //      1   �������޸�
        int ModifyReaderRecord(ref XmlDocument readerdom,
            XmlDocument origin_dom,
            out string strError)
        {
            strError = "";
            // int nRet = 0;
            bool bChanged = false;

            for (int i = 0; i < origin_dom.DocumentElement.ChildNodes.Count; i++)
            {
                XmlNode node = origin_dom.DocumentElement.ChildNodes[i];
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                string strName = node.Name;

                XmlNode node_find = readerdom.DocumentElement.SelectSingleNode(strName);
                if (node_find != null)
                {
                    if (node_find.InnerXml != node.InnerXml)
                    {
                        node_find.InnerXml = node.InnerXml;
                        bChanged = true;
                    }
                }
                else
                {
                    node_find = readerdom.CreateElement(strName);
                    readerdom.DocumentElement.AppendChild(node_find);
                    node_find.InnerXml = node.InnerXml;
                    bChanged = true;
                }
            }

            if (bChanged == true)
                return 1;

            return 0;
        }


        // ���������еĿ���ʧ����߿�
        // parameters:
        // return:
        //      -1  error
        //      0   succeed
        //      1   �ж�
        int DoBlackList(out string strError)
        {
            strError = "";
            int nRet = 0;

            DateTime timeStart = DateTime.Now;

            if (String.IsNullOrEmpty(this.BlackListDoneDate) == false)
            {
                // ����û�б�Ҫ�ظ���
                if (this.BlackListDoneDate == DateTimeUtil.DateTimeToString8(timeStart))
                {
                    this.AppendResultText("����("+this.BlackListDoneDate+")�ڲ�������\r\n");
                    return 0;
                }
            }

            /*
    <dkyw>
        <dataCenter connection="Persist Security Info=False;User ID=dp2rms;Password=dp2rms;Data Source=test111;Connect Timeout=30" db="zzdy" startTime="20:00" />
        <replication mapDbName="����" />
    </dkyw>
             * */
            XmlNode node = this.App.LibraryCfgDom.DocumentElement.SelectSingleNode("//dkyw/replication");
            if (node == null)
            {
                strError = "��δ����<dkyw><replication>����";
                return -1;
            }

            string strReaderDbName = DomUtil.GetAttr(node, "mapDbName");
            if (String.IsNullOrEmpty(strReaderDbName) == true)
            {
                strError = "��δ����<dkyw/replication>Ԫ�ص�mapDbName����";
                return -1;
            }

            node = this.App.LibraryCfgDom.DocumentElement.SelectSingleNode("//dkyw/dataCenter");
            if (node == null)
            {
                strError = "��δ����<dkyw><dataCenter>����";
                return -1;
            }
            string strConnectionString = DomUtil.GetAttr(node, "connection");
            if (String.IsNullOrEmpty(strConnectionString) == true)
            {
                strError = "��δ����<dkyw/dataCenter>Ԫ�ص�connection����";
                return -1;
            }

            string strDbName = DomUtil.GetAttr(node, "db");
            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "��δ����<dkyw/dataCenter>Ԫ�ص�db����";
                return -1;
            }

            // ��ʱ��SessionInfo����
            SessionInfo sessioninfo = new SessionInfo(this.App);

            // ģ��һ���˻�
            Account account = new Account();
            account.LoginName = "replication";
            account.Password = "";
            account.Rights = "setreaderinfo";

            account.Type = "";
            account.Barcode = "";
            account.Name = "replication";
            account.UserID = "replication";
            account.RmsUserName = this.App.ManagerUserName;
            account.RmsPassword = this.App.ManagerPassword;

            sessioninfo.Account = account;

            /*
             * ������ȫ����ʧ״̬�Ķ��߼�¼
             * */

            List<string> loss_barcodes = null;
                    // ���ݶ���֤״̬�Զ��߿���м���
        // parameters:
        //      strMatchStyle   ƥ�䷽ʽ left exact right middle
        //      strState  ����֤״̬
        //      bOnlyIncirculation  �Ƿ��������������ͨ�����ݿ�? true ������������ false : ����ȫ��
        //      bGetPath    == true ���path; == false ���barcode
        // return:
        //      -1  error
        //      ����    ���м�¼����(������nMax�涨�ļ���)
            nRet = this.App.SearchReaderState(
                sessioninfo.Channels,
                "��ʧ",
                "left",
                false,
                false,  // bGetPath,
                -1,
                out loss_barcodes,
                out strError);
            if (nRet == -1)
            {
                strError = "����ȫ����ʧ���߼�¼��Ϣʱ����: " + strError;
                return -1;
            }

            if (nRet == 0)
            {
                if (loss_barcodes == null)
                    loss_barcodes = new List<string>();
            }

            SqlConnection connection = new SqlConnection(strConnectionString);
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                strError = "���ӵ�SQL������ʧ��: " + ex.Message;
                return -1;
            }

            try
            {
                SqlCommand command = null;
                SqlDataReader dr = null;

                string strCommand = "";

                strCommand = "use " + strDbName + "\r\nselect * from balck_list";
                command = new SqlCommand(strCommand,
                    connection);
                try
                {
                    dr = command.ExecuteReader();
                }
                catch (Exception ex)
                {
                    strError = "��ѯSQLʱ����: "
                        + ex.Message + "; "
                        + "SQL����: "
                        + strCommand;
                    return -1;
                }

                // bool bRet = false;



                for (int i = 0; ; i++)
                {
                    Thread.Sleep(1);    // ���⴦��̫��æ

                    if (this.Stopped == true)
                    {
                        return 1;
                    }

                    try
                    {
                        if (dr == null || dr.HasRows == false)
                        {
                            break;
                        }
                        if (dr.Read() == false)
                            break;
                    }
                    catch (Exception ex)
                    {
                        strError = "��SQL���з�������: " + ex.Message;
                        return -1;
                    }

                    // ����ֶ�ֵ
                    string strCardNo = GetSqlStringValue(dr, "CardNo");
                    // ���������ַ���
                    if (String.IsNullOrEmpty(strCardNo) == false)
                        strCardNo = strCardNo.PadLeft(8, '0');

                    this.SetProgressText("��ʧ " + (i + 1).ToString() + " CardNumber=" + strCardNo);

                    // �۲켯�����Ƿ��Ѿ�����
                    int nIndex = loss_barcodes.IndexOf(strCardNo);
                    if (nIndex != -1)
                    {
                        loss_barcodes.RemoveAt(nIndex);
                        this.AppendResultText("��ʧ " + (i + 1).ToString() + " CardNumber=" + strCardNo + "  ԭ�����ǹ�ʧ״̬\r\n");
                        continue;
                    }

                    this.AppendResultText("��ʧ " + (i + 1).ToString() + " CardNumber=" + strCardNo + " ");

                    string strLossDate = GetSqlStringValue(dr, "LossDate");

                    /*
                    string strRfc1123LossDate = "";

                    if (String.IsNullOrEmpty(strLossDate) == false)
                    {
                        nRet = DateTimeUtil.Date8toRfc1123(strLossDate,
                            out strRfc1123LossDate,
                            out strError);
                        if (nRet == -1)
                            return -1;
                    }
                     * */

                    // return:
                    //      -1  error
                    //      0   �Ѿ�д��
                    //      1   û�б�Ҫд��
                    nRet = LossOneReaderInfo(
                            sessioninfo,
                            strCardNo,
                            strLossDate,
                            out strError);
                    if (nRet == -1)
                        return -1;

                    this.AppendResultTextNoTime(strError + "\r\n");
                }

                // Thread.Sleep(2 * 60 * 1000);    // test
                // ���ڼ�����ʣ�µģ����Ǻ���������ģ�״̬��ӦΪ����ʧ���������
                for (int i = 0; i < loss_barcodes.Count; i++)
                {
                    string strBarcode = loss_barcodes[i];

                    if (String.IsNullOrEmpty(strBarcode) == true)
                        continue;

                    this.SetProgressText("��� " + (i + 1).ToString() + " CardNumber=" + strBarcode);

                    this.AppendResultText("��� " + (i + 1).ToString() + " CardNumber=" + strBarcode + " ");
                    // return:
                    //      -1  error
                    //      0   �Ѿ�д��
                    //      1   û�б�Ҫд��
                    nRet = UnLossOneReaderInfo(
                            sessioninfo,
                            strBarcode,
                            out strError);
                    if (nRet == -1)
                        return -1;

                    this.AppendResultTextNoTime(strError + "\r\n");
                }

                // �۲����ĵ�ʱ��
                TimeSpan delta = DateTime.Now - timeStart;
                int nMaxMinutes = 5;
                if (delta.Minutes > nMaxMinutes)
                {
                    // �������5���ӣ�������µ��յ����ڣ�����ͬһ�պ�������
                    this.BlackListDoneDate = DateTimeUtil.DateTimeToString8(timeStart);
                    this.AppendResultText("������ͬ�����̴���ʱ��Ϊ "+delta.ToString()+"�������� "+nMaxMinutes.ToString()+" ���ӣ�����("+this.BlackListDoneDate+")�ڽ������ظ�����\r\n");
                }

                this.SetProgressText("ͬ����������ɣ��ķ�ʱ�� " + delta.ToString() + " ");

                return 0;
            }
            catch (Exception ex)
            {
                strError = "DoBlackList() Exception: " + ExceptionUtil.GetDebugText(ex);
                return -1;
            }
            finally
            {
            }
        }

        // ���н����ʧ�Ĳ���
        // parameters:
        // return:
        //      -1  error
        //      0   �Ѿ�д��
        //      1   û�б�Ҫд��
        int UnLossOneReaderInfo(
            SessionInfo sessioninfo,
            string strBarcode,
            out string strError)
        {
            strError = "";

            int nRet = 0;
            string strReaderXml = "";
            string strOutputPath = "";
            byte[] baTimestamp = null;

            // �Ӷ���
            // ���Ա����õ����߼�¼������;����ʱ״̬
            this.App.ReaderLocks.LockForRead(strBarcode);

            try
            {
                // ��ö��߼�¼
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                nRet = this.App.GetReaderRecXml(
                    this.RmsChannels, // sessioninfo.Channels,
                    strBarcode,
                    out strReaderXml,
                    out strOutputPath,
                    out baTimestamp,
                    out strError);

            }
            finally
            {
                this.App.ReaderLocks.UnlockForRead(strBarcode);
            }

            if (nRet == -1)
                return -1;
            if (nRet > 1)
            {
                strError = "����� " + strBarcode + "�ڶ��߿�Ⱥ�м������� " + nRet.ToString() + " �����뾡������˴���";
                return -1;
            }

            if (nRet == 0)
            {
                // ��¼��Ȼ�����ڣ���û�б�Ҫ�����ʧ
                strError = "���߼�¼������";
                return 1;
            }

            string strAction = "";
            string strRecPath = "";

            string strNewXml = "";  // �޸ĺ�ļ�¼



            // ��¼����

            Debug.Assert(nRet == 1, "");
            // ���У��޸ĺ󸲸�ԭ��¼

            strAction = "change";
            strRecPath = strOutputPath;

            XmlDocument readerdom = new XmlDocument();
            try
            {
                readerdom.LoadXml(String.IsNullOrEmpty(strReaderXml) == false ? strReaderXml : "<root />");
            }
            catch (Exception ex)
            {
                strError = "����XML��¼װ��DOM��������: " + ex.Message;
                return -1;
            }

            string strOldOuterValue = DomUtil.GetElementOuterXml(readerdom.DocumentElement,
                "state");

            string strValue = DomUtil.GetElementText(readerdom.DocumentElement,
                "state");
            string strHead = "";
            if (strValue.Length >= "��ʧ".Length)
                strHead = strValue.Substring(0, "��ʧ".Length);
            else
            {
                // ԭ��ֵ���ǡ���ʧ��������д��
                strError = "ԭ��״̬Ϊ '" + strValue + "'�����ǹ�ʧ״̬�������޸�";
                return 1;
            }

            if (strHead != "��ʧ")
            {
                // ԭ��ֵ���ǡ���ʧ��������д��
                strError = "ԭ��״̬Ϊ '" + strValue + "'�����ǹ�ʧ״̬�������޸�";
                return 1;
            }

            DomUtil.SetElementText(readerdom.DocumentElement,
                "state", "");
            string strNewOuterValue = DomUtil.GetElementOuterXml(readerdom.DocumentElement,
                "state");

            if (strOldOuterValue == strNewOuterValue) // û�з����޸ģ�û�б�Ҫд��
            {
                strError = "��¼û�з����޸�";
                return 1;
            }

            // �޸Ķ��߼�¼
            {
                string strExistingXml = "";
                string strSavedXml = "";
                string strSavedRecPath = "";
                byte[] baNewTimestamp = null;
                DigitalPlatform.rms.Client.rmsws_localhost.ErrorCodeValue kernel_errorcode = DigitalPlatform.rms.Client.rmsws_localhost.ErrorCodeValue.NoError;

                strNewXml = readerdom.OuterXml;

                LibraryServerResult result = this.App.SetReaderInfo(
                        sessioninfo,
                        strAction,
                        strRecPath,
                        strNewXml,
                        strReaderXml,
                        baTimestamp,
                        out strExistingXml,
                        out strSavedXml,
                        out strSavedRecPath,
                        out baNewTimestamp,
                        out kernel_errorcode);
                if (result.Value == -1)
                {
                    strError = result.ErrorInfo;
                    return -1;
                }
            }

            strError = "����д��";
            return 0;   // ����д����
        }

        // ���й�ʧ����
        // parameters:
        // return:
        //      -1  error
        //      0   �Ѿ�д��
        //      1   û�б�Ҫд��
        int LossOneReaderInfo(
            SessionInfo sessioninfo,
            string strBarcode,
            string strLossDate,
            out string strError)
        {
            strError = "";

            int nRet = 0;
            string strReaderXml = "";
            string strOutputPath = "";
            byte[] baTimestamp = null;

            // �Ӷ���
            // ���Ա����õ����߼�¼������;����ʱ״̬
            this.App.ReaderLocks.LockForRead(strBarcode);

            try
            {
                // ��ö��߼�¼
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                nRet = this.App.GetReaderRecXml(
                    this.RmsChannels, // sessioninfo.Channels,
                    strBarcode,
                    out strReaderXml,
                    out strOutputPath,
                    out baTimestamp,
                    out strError);

            }
            finally
            {
                this.App.ReaderLocks.UnlockForRead(strBarcode);
            }

            if (nRet == -1)
                return -1;
            if (nRet > 1)
            {
                strError = "����� " + strBarcode + "�ڶ��߿�Ⱥ�м������� " + nRet.ToString() + " �����뾡������˴���";
                return -1;
            }

            if (nRet == 0)
            {
                // ��¼��Ȼ�����ڣ���û�б�Ҫ��ʧ
                strError = "���߼�¼������";
                return 1;
            }

            string strAction = "";
            string strRecPath = "";

            string strNewXml = "";  // �޸ĺ�ļ�¼



            // ��¼����

            Debug.Assert(nRet == 1, "");
            // ���У��޸ĺ󸲸�ԭ��¼

            strAction = "change";
            strRecPath = strOutputPath;

            XmlDocument readerdom = new XmlDocument();
            try
            {
                readerdom.LoadXml(String.IsNullOrEmpty(strReaderXml) == false ? strReaderXml : "<root />");
            }
            catch (Exception ex)
            {
                strError = "����XML��¼װ��DOM��������: " + ex.Message;
                return -1;
            }

            string strOldOuterValue = DomUtil.GetElementOuterXml(readerdom.DocumentElement,
                "state");
            DomUtil.SetElementText(readerdom.DocumentElement,
                "state", "��ʧ (" + strLossDate + ")");
            string strNewOuterValue = DomUtil.GetElementOuterXml(readerdom.DocumentElement,
                "state");

            if (strOldOuterValue == strNewOuterValue) // û�з����޸ģ�û�б�Ҫд��
            {
                strError = "��¼û�з����޸�";
                return 1;
            }

            // �޸Ķ��߼�¼
            {
                string strExistingXml = "";
                string strSavedXml = "";
                string strSavedRecPath = "";
                byte[] baNewTimestamp = null;
                DigitalPlatform.rms.Client.rmsws_localhost.ErrorCodeValue kernel_errorcode = DigitalPlatform.rms.Client.rmsws_localhost.ErrorCodeValue.NoError;

                strNewXml = readerdom.OuterXml;

                LibraryServerResult result = this.App.SetReaderInfo(
                        sessioninfo,
                        strAction,
                        strRecPath,
                        strNewXml,
                        strReaderXml,
                        baTimestamp,
                        out strExistingXml,
                        out strSavedXml,
                        out strSavedRecPath,
                        out baNewTimestamp,
                        out kernel_errorcode);
                if (result.Value == -1)
                {
                    strError = result.ErrorInfo;
                    return -1;
                }
            }

            strError = "����д��";
            return 0;   // ����д����
        }

#if NOOOOOOOOOOOOOOOOOOOOOOOOOO
        static string GetAccStatusString(string strAccStatus)
        {
            if (strAccStatus == "0")
                return "�ѳ���";
            if (strAccStatus == "1")
                return "��Ч��";
            if (strAccStatus == "2")
                return "��ʧ��";
            if (strAccStatus == "3")
                return "���Ῠ";
            if (strAccStatus == "4")
                return "Ԥ����";
            return strAccStatus;    // ����Ԥ�����ֵ
        }
        // ��������������ò���
        int GetDataCenterParam(
            out string strServerUrl,
            out string strUserName,
            out string strPassword,
            out string strError)
        {
            strError = "";
            strServerUrl =
            strUserName = "";
            strPassword = "";

            XmlNode node = this.App.LibraryCfgDom.DocumentElement.SelectSingleNode("//zhengyuan/dataCenter");

            if (node == null)
            {
                strError = "��δ����<zhangyuan/dataCenter>Ԫ��";
                return -1;
            }

            strServerUrl = DomUtil.GetAttr(node, "url");
            strUserName = DomUtil.GetAttr(node, "username");
            strPassword = DomUtil.GetAttr(node, "password");

            return 0;
        }

        // ���������ļ�
        // parameters:
        //      strDataFileName �����ļ�����������ļ�����
        //      strLocalFilePath    �����ļ���
        // return:
        //      -1  ����
        //      0   ��������
        //      1   ���û��ж�
        int DownloadDataFile(string strDataFileName,
            string strLocalFilePath,
            out string strError)
        {
            strError = "";

            string strServerUrl = "";
            string strUserName = "";
            string strPassword = "";

            // ��������������ò���
            int nRet = GetDataCenterParam(
                out strServerUrl,
                out strUserName,
                out strPassword,
                out strError);
            if (nRet == -1)
                return -1;

            string strPath = strServerUrl + "/" + strDataFileName;

            Uri serverUri = new Uri(strPath);

            /*
            // The serverUri parameter should start with the ftp:// scheme.
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
            }
             * */


            // Get the object used to communicate with the server.
            WebClient request = new WebClient();

            this.DownloadException = null;
            this.DownloadCancelled = false;
            this.eventDownloadFinished.Reset();

            request.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(request_DownloadFileCompleted);
            request.DownloadProgressChanged += new DownloadProgressChangedEventHandler(request_DownloadProgressChanged);

            request.Credentials = new NetworkCredential(strUserName,
                strPassword);

            try
            {

                File.Delete(strLocalFilePath);

                request.DownloadFileAsync(serverUri,
                    strLocalFilePath);
            }
            catch (WebException ex)
            {
                strError = "���������ļ� " + strPath + " ʧ��: " + ex.ToString();
                return -1;
            }

            // �ȴ����ؽ���

            WaitHandle[] events = new WaitHandle[2];

            events[0] = this.eventClose;
            events[1] = this.eventDownloadFinished;

            while (true)
            {
                if (this.Stopped == true)
                {
                    request.CancelAsync();
                }

                int index = WaitHandle.WaitAny(events, 1000, false);    // ÿ�볬ʱһ��

                if (index == WaitHandle.WaitTimeout)
                {
                    // ��ʱ
                }
                else if (index == 0)
                {
                    strError = "���ر��ر��ź���ǰ�ж�";
                    return -1;
                }
                else
                {
                    // �õ������ź�
                    break;
                }
            }

            if (this.DownloadCancelled == true)
                return 1;   // ���û��ж�

            if (this.DownloadException != null)
            {
                strError = this.DownloadException.Message;
                if (this.DownloadException is WebException)
                {
                    WebException webex = (WebException)this.DownloadException;
                    if (webex.Response is FtpWebResponse)
                    {
                        FtpWebResponse ftpr = (FtpWebResponse)webex.Response;
                        if (ftpr.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                        {
                            return -1;
                        }
                    }

                }
                return -1;
            }

            return 0;
        }

        void request_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if ((e.BytesReceived % 1024 * 100) == 0)
                this.AppendResultText("������: " + e.BytesReceived + "\r\n");
        }

        void request_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            this.DownloadException = e.Error;
            this.DownloadCancelled = e.Cancelled;
            this.eventDownloadFinished.Set();
        }

#endif

        static string GetCurrentDate()
        {
            DateTime now = DateTime.Now;

            return now.Year.ToString().PadLeft(4, '0')
            + now.Month.ToString().PadLeft(2, '0')
            + now.Day.ToString().PadLeft(2, '0');
        }


    }
}
