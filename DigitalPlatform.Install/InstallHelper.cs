using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

using System.DirectoryServices;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Xml;
using System.Data;

using Microsoft.Win32;

using DigitalPlatform;
using DigitalPlatform.IO;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using System.Runtime.InteropServices;
using DigitalPlatform.GUI;
using System.Threading;
using DigitalPlatform.CirculationClient;

namespace DigitalPlatform.Install
{
    public class InstallHelper
    {


        // parameters:
        //      lines   �����в�����ÿ��ִ��һ��
        //      bOutputCmdLine  ��������Ƿ����������? ���Ϊ false����ʾ�����������У�ֻ������������
        // return:
        //      -1  ����
        //      0   �ɹ���strError �����������������Ϣ
        public static int RunCmd(
            string fileName,
            List<string> lines,
            bool bOutputCmdLine,
            out string strError)
        {
            strError = "";

            string strErrorInfo = "";
            StringBuilder result = new StringBuilder();

            try
            {
                int i = 0;
                foreach (string arguments in lines)
                {
                    ProcessStartInfo info = new ProcessStartInfo()
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };

                    if (bOutputCmdLine == true)
                        result.Append("\r\n" + (i + 1).ToString() + ")\r\n" + fileName + " " + arguments + "\r\n");

                    using (Process process = Process.Start(info))
                    {
                        process.OutputDataReceived += new DataReceivedEventHandler(
            (s, e1) =>
            {
                result.Append(e1.Data + "\r\n");
            }
        );
                        process.ErrorDataReceived += new DataReceivedEventHandler((s, e1) =>
                        {
                            strErrorInfo = e1.Data;
                        }
                        );
                        process.BeginOutputReadLine();
                        while (true)
                        {
                            Application.DoEvents();
                            if (process.WaitForExit(500) == true)
                                break;
                        }
                    }

                    for (int j = 0; j < 10; j++)
                    {
                        Application.DoEvents();
                        Thread.Sleep(1);
                    }

                    i++;
                }
            }
            catch(Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            if (string.IsNullOrEmpty(strErrorInfo) == false)
            {
                strError = strErrorInfo;
                return -1;
            }

            // ������Ϣ
            strError = result.ToString();
            return 0;
        }


        // return:
        //      -1  �����������������Ȼ�����
        //      0   �ɹ�
        public static int DeleteDataDir(string strDataDir,
            out string strError)
        {
            strError = "";
        REDO_DELETE_DATADIR:
            try
            {
                Directory.Delete(strDataDir, true);
                return 0;
            }
            catch (Exception ex)
            {
                strError = "ɾ������Ŀ¼ '" + strDataDir + "' ʱ����: " + ex.Message;
            }

            DialogResult temp_result = MessageBox.Show(ForegroundWindow.Instance,
strError + "\r\n\r\n�Ƿ�����?",
"ɾ������Ŀ¼ '" + strDataDir + "'",
MessageBoxButtons.RetryCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button1);
            if (temp_result == DialogResult.Retry)
                goto REDO_DELETE_DATADIR;

            return -1;
        }

        // http://stackoverflow.com/questions/13984920/how-can-i-use-system-data-sql-sqldatasourceenumerator-class-to-know-available-sq
        public List<string> GetMsSqlInstances()
        {
            List<string> sqlInstances = new List<string>();

            while (true)
            {
                System.Data.Sql.SqlDataSourceEnumerator instance = System.Data.Sql.SqlDataSourceEnumerator.Instance;
                System.Data.DataTable dataTable = instance.GetDataSources();
                foreach (DataRow row in dataTable.Rows)
                {
                    string instanceName = String.Format(@"{0}\{1}", row["ServerName"].ToString(), row["InstanceName"].ToString());

                    //Do not add the local instance, we will add it in the next section. Otherwise, duplicated!
                    if (!sqlInstances.Contains(instanceName) && !instanceName.Contains(Environment.MachineName))
                    {
                        sqlInstances.Add(instanceName);
                    }
                }

                /*
                 * For some reason, GetDataSources() does not get local instances. So using code from here to get them
                 * http://stackoverflow.com/questions/6824188/sqldatasourceenumerator-instance-getdatasources-does-not-locate-local-sql-serv
                 */
                List<string> lclInstances = GetLocalSqlServerInstanceNames();
                foreach (var lclInstance in lclInstances)
                {
                    string instanceName = String.Format(@"{0}\{1}", Environment.MachineName, lclInstance);
                    if (!sqlInstances.Contains(instanceName)) sqlInstances.Add(instanceName);
                }
                sqlInstances.Sort();
            }
        }

        //Got code from: http://stackoverflow.com/questions/6824188/sqldatasourceenumerator-instance-getdatasources-does-not-locate-local-sql-serv
        /// <summary>
        ///  get local sql server instance names from registry, search both WOW64 and WOW3264 hives
        /// </summary>
        /// <returns>a list of local sql server instance names</returns>
        public static List<string> GetLocalSqlServerInstanceNames()
        {
            RegistryValueDataReader registryValueDataReader = new RegistryValueDataReader();

            string[] instances64Bit = registryValueDataReader.ReadRegistryValueData(RegistryHive.Wow64,
                                                                                    Registry.LocalMachine,
                                                                                    @"SOFTWARE\Microsoft\Microsoft SQL Server",
                                                                                    "InstalledInstances");

            string[] instances32Bit = registryValueDataReader.ReadRegistryValueData(RegistryHive.Wow6432,
                                                                                    Registry.LocalMachine,
                                                                                    @"SOFTWARE\Microsoft\Microsoft SQL Server",
                                                                                    "InstalledInstances");

            //FormatLocalSqlInstanceNames(ref instances64Bit);
            //FormatLocalSqlInstanceNames(ref instances32Bit);

            List<string> localInstanceNames = new List<string>(instances64Bit);
            foreach (var item in instances32Bit)
            {
                if (!localInstanceNames.Contains(item)) localInstanceNames.Add(item);
            }

            //localInstanceNames = localInstanceNames.Union(instances32Bit).ToList();

            return localInstanceNames;
        }


        public static string GetProductString(string strProductName,
            string strEntryName)
        {
            // throw new Exception("test rollback");
            // Debug.Assert(false, "");
            using (RegistryKey digitalplatform = Registry.LocalMachine.CreateSubKey("SOFTWARE\\DigitalPlatform"))
            {
                using (RegistryKey product = digitalplatform.CreateSubKey(strProductName))
                {
                    if (product.GetValue(strEntryName) == null)
                        return null;

                    if (product.GetValue(strEntryName) is string)
                    {
                        return (string)product.GetValue(strEntryName);
                    }

                    return null;
                }
            }
        }

        public static void SetProductString(
            string strProductName,
            string strEntryName,
            string strValue)
        {
            // Debug.Assert(false , "");
            using (RegistryKey digitalplatform = Registry.LocalMachine.CreateSubKey("SOFTWARE\\DigitalPlatform"))
            {
                using (RegistryKey product = digitalplatform.CreateSubKey(strProductName))
                {
                    if (product.GetValue(strEntryName) != null)
                        product.DeleteValue(strEntryName);
                    if (String.IsNullOrEmpty(strValue) == false)
                    {
                        product.SetValue(strEntryName, strValue, RegistryValueKind.String);
                    }
                }
            }
        }

        // ����������Ʒ��bindings�Ƿ��ͻ
        // return:
        //      -1  ����
        //      0   ����
        //      1    �ظ�
        public static int IsGlobalBindingDup(string strBindings,
            string strProductName,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (string.IsNullOrEmpty(strBindings) == true)
                return 0;

            string[] bindings = strBindings.Replace("\r\n", ";").Split(new char[] { ';' });
            if (bindings.Length == 0)
                return 0;

            string[] products = new string[] {
                "dp2Kernel",
                "dp2Library",
                "GcatServer",
                "dp2ZServer",
            };
            foreach (string product in products)
            {
                if (string.Compare(strProductName, product, true) == 0)
                    continue;

                for (int i = 0; ; i++)
                {
                    string strInstanceName = "";
                    string strDataDir = "";
                    string strCertificatSN = "";

                    string[] existing_urls = null;
                    bool bRet = InstallHelper.GetInstanceInfo(product,
                        i,
                        out strInstanceName,
                        out strDataDir,
                        out existing_urls,
                        out strCertificatSN);
                    if (bRet == false)
                        break;

                    for (int j = 0; j < bindings.Length; j++)
                    {
                        string strStart = bindings[j];

                        // ��������е��ĸ�url��strOneBinding�˿ڡ���ַ��ͻ
                        // return:
                        //      -2  ����ͻ
                        //      -1  ����
                        //      >=0 ������ͻ��url�������е��±�
                        nRet = IsBindingDup(strStart,
                            existing_urls,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet >= 0)
                        {
                            strError = "��ǰ�󶨼��Ϻ��Ѱ�װ�� '" + product + "' ��ʵ�� '" + strInstanceName + "' �İ󶨼���֮�䷢���˳�ͻ: " + strError;
                            return 1;
                        }
                    }
                }
            }

            return 0;
        }

        // ��������е��ĸ�url��strOneBinding�˿ڡ���ַ��ͻ
        // return:
        //      -2  ����ͻ
        //      -1  ����
        //      >=0 ������ͻ��url�������е��±�
        public static int IsBindingDup(string strOneBinding,
            string[] bindings,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strOneBinding) == true)
            {
                strError = "strOneBinding����ֵ����Ϊ��";
                return -1;
            }

            Uri one_uri = new Uri(strOneBinding);
            if (one_uri.Scheme.ToLower() == "rest.http")
                one_uri = new Uri(strOneBinding.Substring(5));
            else if (one_uri.Scheme.ToLower() == "basic.http")
                one_uri = new Uri(strOneBinding.Substring(6));

            for (int i = 0; i < bindings.Length; i++)
            {
                string strCurrentBinding = bindings[i];
                if (String.IsNullOrEmpty(strCurrentBinding) == true)
                    continue;

                Uri current_uri = new Uri(strCurrentBinding);

                if (current_uri.Scheme.ToLower() == "rest.http")
                    current_uri = new Uri(strCurrentBinding.Substring(5));
                else if (current_uri.Scheme.ToLower() == "basic.http")
                    current_uri = new Uri(strCurrentBinding.Substring(6));

                if (one_uri.Scheme.ToLower() == "net.tcp")
                {
                    if (current_uri.Scheme.ToLower() == "net.tcp")
                    {
                        // �˿ڲ��ܳ�ͻ
                        if (one_uri.Port == current_uri.Port)
                        {
                            strError = "'" + strOneBinding + "' �� '" + strCurrentBinding + "' ֮��˿ںų�ͻ��";
                            return i;
                        }
                    }
                    else if (current_uri.Scheme.ToLower() == "net.pipe")
                    {
                        // �����ڳ�ͻ�Ŀ���
                    }
                    else if (current_uri.Scheme.ToLower() == "http")
                    {
                        // �˿ںŲ��ܳ�ͻ
                        if (one_uri.Port == current_uri.Port)
                        {
                            strError = "'" + strOneBinding + "' �� '" + strCurrentBinding + "' ֮��˿ںų�ͻ��";
                            return i;
                        }
                    }
                }
                else if (one_uri.Scheme.ToLower() == "net.pipe")
                {
                    if (current_uri.Scheme.ToLower() == "net.pipe")
                    {
                        // ����ȫ����ͬ
                        if (one_uri.Equals(current_uri) == true)
                        {
                            strError = "net.pipe���͵�URL '" + strOneBinding + "' ��������ȫ��ͬ";
                            return i;
                        }

                        if (IsUrlEqual(one_uri.ToString(), current_uri.ToString()) == true)
                        {
                            strError = "net.pipe���͵�URL '" + strOneBinding + "' ������ʵ������ͬ(ĩβ��������һ��'/'�ַ�)";
                            return i;
                        }
                    }
                }
                else if (one_uri.Scheme.ToLower() == "http")
                {
                    if (current_uri.Scheme.ToLower() == "net.tcp")
                    {
                        // �˿ڲ��ܳ�ͻ
                        if (one_uri.Port == current_uri.Port)
                        {
                            strError = "'" + strOneBinding + "' �� '" + strCurrentBinding + "' ֮��˿ںų�ͻ��";
                            return i;
                        }
                    }
                    else if (current_uri.Scheme.ToLower() == "net.pipe")
                    {
                        // �����ܳ�ͻ
                    }
                    else if (current_uri.Scheme.ToLower() == "http")
                    {
                        // �˿ںſ�����ͬ�����ǲ���ȫ����ͬ
                        if (one_uri.Equals(current_uri) == true)
                        {
                            strError = "http���͵�URL '" + strOneBinding + "' ��������ȫ��ͬ";
                            return i;
                        }

                        if (IsUrlEqual(one_uri.ToString(), current_uri.ToString()) == true)
                        {
                            strError = "http���͵�URL '" + strOneBinding + "' ������ʵ������ͬ(ĩβ��������һ��'/'�ַ�)";
                            return i;
                        }
                    }
                }
            }

            return -2;
        }

        // ������URL�ַ������к������һ��'/'�ַ��ıȽ�
        static bool IsUrlEqual(string url1, string url2)
        {
            if (url1.Length > 0 && url1[url1.Length - 1] != '/')
                url1 += "/";
            if (url2.Length > 0 && url2[url2.Length - 1] != '/')
                url2 += "/";

            if (url1 == url2)
                return true;

            // ���ϸ�رȽ�

            try
            {
                Uri uri1 = new Uri(url1);
                Uri uri2 = new Uri(url2);

                if (uri1.Equals(uri2) == true)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        // ����ض�ʵ������ע��������±�
        // return:
        //      -1  û���ҵ�
        //      ����  �±�
        public static int GetInstanceIndex(string strProductName, string strInstanceNameParam)
        {
            for (int i = 0; ; i++)
            {
                string strInstanceName = "";
                string strDataDir = "";
                string strCertificatSN = "";
                string[] existing_urls = null;
                string strSerialNumber = "";
                bool bRet = InstallHelper.GetInstanceInfo(strProductName,
                    i,
                    out strInstanceName,
                    out strDataDir,
                    out existing_urls,
                    out strCertificatSN,
                    out strSerialNumber);
                if (bRet == false)
                    break;

                if (strInstanceNameParam == strInstanceName)
                    return i;
            }

            return -1;  // û���ҵ�
        }

        // ɾ��Instance��Ϣ
        // return:
        //      false   instanceû���ҵ�
        //      true    �ҵ������Ѿ�ɾ��
        public static bool DeleteInstanceInfo(
            string strProductName,
            string strInstanceName)
        {
            int index = GetInstanceIndex(strProductName, strInstanceName);
            if (index == -1)
                return false;
            return DeleteInstanceInfo(strProductName, index);
        }

        // ɾ��Instance��Ϣ
        // return:
        //      false   instanceû���ҵ�
        //      true    �ҵ������Ѿ�ɾ��
        public static bool DeleteInstanceInfo(
            string strProductName,
            int nIndex)
        {
            using (RegistryKey digitalplatform = Registry.LocalMachine.CreateSubKey("SOFTWARE\\DigitalPlatform"))
            {
                using (RegistryKey product = digitalplatform.CreateSubKey(strProductName))
                {
                    RegistryKey instance = product.OpenSubKey("instance_" + nIndex.ToString());
                    if (instance == null)
                        return false;   // not found
                    instance.Close();

                    product.DeleteSubKeyTree("instance_" + nIndex.ToString(), false);
                }
            }

            return true;
        }

        // ��װ��İ汾
        public static void SetInstanceInfo(
    string strProductName,
    int nIndex,
    string strInstanceName,
    string strDataDir,
    string[] urls,
    string strCertificateSN)
        {
            SetInstanceInfo(
                strProductName,
                nIndex,
                strInstanceName,
                strDataDir,
                urls,
                strCertificateSN,
                null);
        }

        // ����instance��Ϣ
        public static void SetInstanceInfo(
            string strProductName,
            int nIndex,
            string strInstanceName,
            string strDataDir,
            string[] urls,
            string strCertificateSN,
            string strSerialNumber)
        {
            using (RegistryKey digitalplatform = Registry.LocalMachine.CreateSubKey("SOFTWARE\\DigitalPlatform"))
            {
                using (RegistryKey product = digitalplatform.CreateSubKey(strProductName))
                {
                    using (RegistryKey instance = product.CreateSubKey("instance_" + nIndex.ToString()))
                    {
                        instance.SetValue("name", strInstanceName);

                        instance.SetValue("datadir", strDataDir);

                        if (instance.GetValue("cert_sn") != null)
                            instance.DeleteValue("cert_sn");

                        if (string.IsNullOrEmpty(strCertificateSN) == false)
                            instance.SetValue("cert_sn", strCertificateSN);

                        if (instance.GetValue("bindings") != null)
                            instance.DeleteValue("bindings");
                        if (urls != null)
                            instance.SetValue("bindings", urls, RegistryValueKind.MultiString);

                        if (string.IsNullOrEmpty(strSerialNumber) == false)
                            instance.SetValue("sn", strSerialNumber);
                    }
                }
            }
        }

        // ��װ��İ汾
        public static bool GetInstanceInfo(string strProductName,
    int nIndex,
    out string strInstanceName,
    out string strDataDir,
    out string[] urls,
    out string strCertificateSN)
        {
            string strSN = "";
            return GetInstanceInfo(strProductName,
                nIndex,
                out strInstanceName,
                out strDataDir,
                out urls,
                out strCertificateSN,
                out strSN);
        }

        // ���instance��Ϣ
        // parameters:
        //      urls ��ð󶨵�Urls
        // return:
        //      false   instanceû���ҵ�
        //      true    �ҵ�
        public static bool GetInstanceInfo(string strProductName,
            int nIndex,
            out string strInstanceName,
            out string strDataDir,
            out string[] urls,
            out string strCertificateSN,
            out string strSerialNumber)
        {
            strInstanceName = "";
            strDataDir = "";
            urls = null;
            strCertificateSN = "";
            strSerialNumber = "";

            using (RegistryKey digitalplatform = Registry.LocalMachine.CreateSubKey("SOFTWARE\\DigitalPlatform"))
            {
                using (RegistryKey product = digitalplatform.CreateSubKey(strProductName))
                {
                    RegistryKey instance = product.OpenSubKey("instance_" + nIndex.ToString());
                    if (instance == null)
                        return false;   // not found

                    using (instance)
                    {
                        strInstanceName = (string)instance.GetValue("name");

                        strDataDir = (string)instance.GetValue("datadir");

                        strCertificateSN = (string)instance.GetValue("cert_sn");

                        urls = (string[])instance.GetValue("bindings");
                        if (urls == null)
                            urls = new string[0];

                        strSerialNumber = (string)instance.GetValue("sn");

                        return true;    // found
                    }
                }
            }
        }

        // ������ֹ
        // �Ƿ�Ϊ��ǰ��SingleString��hosturl��̬
        public static bool IsOldHostUrl(string strProductName)
        {
            // throw new Exception("test rollback");
            // Debug.Assert(false, "");
            using (RegistryKey digitalplatform = Registry.LocalMachine.CreateSubKey("SOFTWARE\\DigitalPlatform"))
            {
                using (RegistryKey dp2kernel = digitalplatform.CreateSubKey(strProductName))
                {
                    if (dp2kernel.GetValue("hosturl") is string)
                        return true;

                    return false;
                }
            }
        }

        // ������ֹ
        // ��ð󶨵�Urls
        public static string[] GetHostUrl(string strProductName)
        {
            // throw new Exception("test rollback");
            // Debug.Assert(false, "");
            using (RegistryKey digitalplatform = Registry.LocalMachine.CreateSubKey("SOFTWARE\\DigitalPlatform"))
            {
                using (RegistryKey dp2kernel = digitalplatform.CreateSubKey(strProductName))
                {
                    if (dp2kernel.GetValue("hosturl") == null)
                        return new string[0];

                    if (dp2kernel.GetValue("hosturl") is string)
                    {
                        string[] results = new string[1]; ;
                        results[0] = (string)dp2kernel.GetValue("hosturl");
                        return results;
                    }

                    return (string[])dp2kernel.GetValue("hosturl");
                }
            }
        }

        // ������ֹ
        // ���ð󶨵�Urls
        public static void SetHostUrl(
            string strProductName,
            string[] urls)
        {
            // Debug.Assert(false , "");
            using (RegistryKey digitalplatform = Registry.LocalMachine.CreateSubKey("SOFTWARE\\DigitalPlatform"))
            {
                using (RegistryKey dp2kernel = digitalplatform.CreateSubKey(strProductName))
                {
                    if (dp2kernel.GetValue("hosturl") != null)
                        dp2kernel.DeleteValue("hosturl");
                    dp2kernel.SetValue("hosturl", urls, RegistryValueKind.MultiString);
                }
            }
        }



        // ɾ����¼��װ�������ļ�
        public static void DeleteSetupCfgFile(string strRootDir)
        {
            try
            {
                string strSetupCfgFileName = PathUtil.MergePath(strRootDir, "SetupCfg.xml");
                if (File.Exists(strSetupCfgFileName) == true)
                    File.Delete(strSetupCfgFileName);
            }
            catch
            {
            }
        }

        // ���氲װ�������Ա��޸�ʱʹ��
        public static int SetSetupParams(string strRootDir,
            Hashtable table,
            out string strError)
        {
            strError = "";

            string strSetupCfgFileName = PathUtil.MergePath(strRootDir, "SetupCfg.xml");

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.Load(strSetupCfgFileName);
            }
            catch (FileNotFoundException /*ex*/)
            {
                // ����ļ�������
                dom.LoadXml("<?xml version='1.0' encoding='utf-8'?><root />");
            }
            catch (Exception ex)
            {
                strError = "�����ļ� " + strSetupCfgFileName + " �� XMLDOM ʱ����" + ex.Message;
                return -1;
            }

            bool bChanged = false;
            foreach (string key in table.Keys)
            {
                DomUtil.SetElementText(dom.DocumentElement,
                    key,
                    (string)table[key]);
                bChanged = true;
            }

            if (bChanged == true)
                dom.Save(strSetupCfgFileName);

            return 0;
        }

        // �ӱ����ļ��еõ���װ����
        // VDIR TSITE
        // parameters:
        // return:
        //      -1  error
        //      0   xml file not found
        //      1   xml file found
        public static int GetSetupParams(string strRootDir,
            out Hashtable table,
            out string strError)
        {
            strError = "";
            table = new Hashtable();

            string strSetupCfgFileName = PathUtil.MergePath(strRootDir, "SetupCfg.xml");
            if (File.Exists(strSetupCfgFileName) == false)
                return 0;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.Load(strSetupCfgFileName);
            }
            catch (Exception ex)
            {
                strError = "�����ļ� " + strSetupCfgFileName + "�� XMLDOM ʱ����" + ex.Message;
                return -1;
            }

            for (int i = 0; i < dom.DocumentElement.ChildNodes.Count; i++)
            {
                XmlNode node = dom.DocumentElement.ChildNodes[i];

                if (node.NodeType != XmlNodeType.Element)
                    continue;

                string strParamName = node.Name;
                table[strParamName] = node.InnerText.Trim();
            }

            return 1;
        }

        // ���ServerBindings��ǰ����
        // ����һ�����飬ÿ��Ԫ����̬Ϊ ":80:" �� "ip:80:hostname"
        public static string [] GetServerBindings(string strTargetSite)
        {
            string strFolderPath = "IIS://localhost" + strTargetSite;

            //MessageBox.Show(strFolderPath);

            DirectoryEntry folderEntry = new DirectoryEntry(strFolderPath);
            try
            {
                string[] values = new string[folderEntry.Properties["ServerBindings"].Count];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = (string)folderEntry.Properties["ServerBindings"][i];
                }

                return values;
            }
            catch (Exception /*ex*/)
            {
                return null;
            }
        }

        // �޸�һ������Ŀ¼��ĳЩ���� ContentIndexed DontLog AppIsolated
        // parameters:
        //      strTargetSite   ��̬Ϊ"/W3SVC/1"��ע��û��ǰ���"LM"����
        //      strVDir ����Ŀ¼��
        public static bool SetVdirProperties(string strTargetSite,
            string strVDir)
        {
            string strFolderPath = "IIS://localhost" + strTargetSite + "/ROOT/" + strVDir;

            //MessageBox.Show(strFolderPath);

            DirectoryEntry folderEntry = new DirectoryEntry(strFolderPath);
            try
            {
                folderEntry.Properties["ContentIndexed"][0] = false;
                folderEntry.Properties["DontLog"][0] = true;
                folderEntry.Properties["AppIsolated"][0] = 2;
            }
            catch (Exception /*ex*/)
            {
                return false;
            }

            folderEntry.CommitChanges();

            return true;
        }

        // �޸�һ������Ŀ¼��app pool id��ֻ��IIS 6����
        // parameters:
        //      strTargetSite   ��̬Ϊ"/W3SVC/1"��ע��û��ǰ���"LM"����
        //      strVDir ����Ŀ¼��
        public static bool SetAppPoolId(string strTargetSite,
            string strVDir,
            string strAppPoolName)
        {
            string strFolderPath = "IIS://localhost" + strTargetSite + "/ROOT/" + strVDir;

            //MessageBox.Show(strFolderPath);

            DirectoryEntry folderEntry = new DirectoryEntry(strFolderPath);

            try
            {
                folderEntry.Properties["AppPoolId"][0] = strAppPoolName;
            }
            catch (Exception /*ex*/)
            {
                return false;
            }

            folderEntry.CommitChanges();

            return true;
        }

        // ����һ��app pool��ֻ��IIS 6����
        public static bool CreateAppPool(string strAppPoolName)
        {

            DirectoryEntry folder = null;

            try
            {
                folder = new DirectoryEntry("IIS://localhost/W3SVC/AppPools");
                int temp = folder.Properties.Count; // ���紥���쳣���Ա��֪AppPools����������(IIS 5.0)
            }
            catch (Exception /*ex*/)
            {
                return false;
            }

            // ���������Ƿ��Ѿ�����?
            DirectoryEntry pool = null;

            try
            {
                pool = folder.Children.Find(strAppPoolName, "IIsApplicationPool");
            }
            catch (System.IO.DirectoryNotFoundException /*ex*/)
            {
                pool = null;
            }

            if (pool != null)
            {
            }
            else
            {
                pool = folder.Children.Add(strAppPoolName, "IIsApplicationPool");
            }



            // ɾ��PeriodicRestartTime 
            pool.Properties["PeriodicRestartTime"][0] = 0;

            // ɾ��IdleTimeout 
            pool.Properties["IdleTimeout"][0] = 0;

            // ��DisallowOverlappingRotation����Ϊtrue
            pool.Properties["DisallowOverlappingRotation"][0] = true;

            pool.CommitChanges();
            return true;
        }

        // ����һ������Ŀ¼���Զ������ļ�
        // parameters:
        //      strTargetSite   ��̬Ϊ"/W3SVC/1"��ע��û��ǰ���"LM"����
        //      strVDir ����Ŀ¼��
        //      strFileName �������ļ���
        public static void SetDefaultDoc(string strTargetSite,
            string strVDir,
            string strFileName)
        {
            // �ο� http://geekswithblogs.net/mnf/articles/78888.aspx
            string strFolderPath = "IIS://localhost" + strTargetSite + "/ROOT/" + strVDir;

            DirectoryEntry folderEntry = new DirectoryEntry(strFolderPath);

            // Debug.Assert(false, "����");

            // string strText = GetAllChildrenNames(entry);

            folderEntry.Properties["DefaultDoc"][0] = strFileName;
            // entry.Properties["AccessRead"][0] = true;

            folderEntry.CommitChanges();
        }




        public static int SetControlRightsToDirectory(string strDataDir,
            out string strError)
        {
            strError = "";

            string strAccount = "";
            try
            {
                // ������Ŀ¼��APSNET��ȫ����Ȩ��
                strAccount = Environment.MachineName + "\\ASPNET";
                InstallHelper.AddDirectorySecurity(strDataDir,
                    strAccount,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow);
            }
            catch (System.Security.Principal.IdentityNotMappedException)
            {

            }

            try
            {
                // ������Ŀ¼��IIS_WPG��ȫ����Ȩ��
                strAccount = Environment.MachineName + "\\IIS_WPG";
                InstallHelper.AddDirectorySecurity(strDataDir,
                    strAccount,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow);
            }
            catch (System.Security.Principal.IdentityNotMappedException)
            {

            }

            return 0;
        }


        // ��Ӧ��down����
        public static void DownApplication(string strRootDir)
        {
            string strBinDir = PathUtil.MergePath(strRootDir, "bin");

            string strRestartFileName = Path.Combine(strBinDir, "temp.temp");
            using (Stream s = File.Create(strRestartFileName))
            {
                s.WriteByte(0);
            }

            try
            {
                File.Delete(strRestartFileName);
            }
            catch
            {
            }
        }

        // ִ��aspnet_regiis.exe, ������Ŀ¼����ΪASP.NET 4.0����
        // ע��Windows 8 �����������Ч������ dism ���� IIS ������
        // parameters:
        //      strTargetSite   վ��·��. ���� "/W3SVC/1"
        //      strVDir ����Ŀ¼�� ���� "dp2OPAC"
        public static int SetVDirAspNet40(string strTargetSite,
            string strVDir,
            out string strError)
        {
            strError = "";

            if (strTargetSite.Length > 0 && strTargetSite[0]== '/')
                strTargetSite = strTargetSite.Substring(1);

            string strVPath = strTargetSite + "/ROOT/" + strVDir;

            string strDotNetDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            string strExePath = PathUtil.MergePath(strDotNetDir, "aspnet_regiis.exe");
            string strParameters = " -sn " + strVPath;

            ProcessStartInfo startInfo = new ProcessStartInfo(strExePath);
            startInfo.Arguments = strParameters;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            Process p = Process.Start(startInfo);

            // Process p = Process.Start(strExePath, strParameters);
            if (p == null)
            {
                strError = "���� " + strExePath + " ��������";
                return -1;
            }
            p.WaitForExit();
            int nExitCode = p.ExitCode;

            if (nExitCode != 0)
            {
                // exit code 3 ��δע��ASP.NET

                string strText = "";
                string line;
                StreamReader reader = p.StandardOutput;
                p.Close();
                for (line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    strText += line + "\r\n";
                }
                reader.Close();

                strError = "���趨����Ŀ¼ΪASP.NET 4.0ʱ, ִ�г��� " + strExePath + strParameters + " ʧ��, ������Ϊ " + nExitCode.ToString() + "\r\n\r\n������ʾ��Ϣ����:\r\n" + strText;
                return -1;
            }

            return 0;
        }

        // ִ��aspnet_regiis.exe, ������Ŀ¼����ΪASP.NET 2.0����
        // parameters:
        //      strTargetSite   վ��·��. ���� "/W3SVC/1"
        //      strVDir ����Ŀ¼�� ���� "dp2opac"
        public static int SetVDirAspNet20(string strTargetSite,
            string strVDir,
            out string strError)
        {
            strError = "";

            string strVPath = strTargetSite + "/ROOT/" + strVDir;

            string strNetDir = API.GetClrInstallationDirectory();
            string strExePath = strNetDir + "aspnet_regiis.exe";
            string strParameters = " -sn " + strVPath;

            ProcessStartInfo startInfo = new ProcessStartInfo(strExePath);
            startInfo.Arguments = strParameters;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            Process p = Process.Start(startInfo);

            // Process p = Process.Start(strExePath, strParameters);
            if (p == null)
            {
                strError = "���� " + strExePath + " ��������";
                return -1;
            }
            p.WaitForExit();
            int nExitCode = p.ExitCode;

            if (nExitCode != 0)
            {
                // exit code 3 ��δע��ASP.NET

                string strText = "";
                string line;
                StreamReader reader = p.StandardOutput;
                p.Close();
                for (line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    strText += line + "\r\n";
                }
                reader.Close();

                strError = "���趨����Ŀ¼ΪASP.NET 2.0ʱ, ִ�г��� " + strExePath + strParameters + " ʧ��, ������Ϊ " + nExitCode.ToString() + "\r\n\r\n������ʾ��Ϣ����:\r\n" + strText;
                return -1;
            }

            return 0;
        }

        // �����ļ�Ϊ����(����)��
        // parameters:
        //      strTargetSite   ��̬Ϊ"/W3SVC/1"��ע��û��ǰ���"LM"����
        //      strVDir ����Ŀ¼��
        //      strFileName �������ļ���
        public static void RemoveFileReadProperty(string strTargetSite,
            string strVDir,
            string strFileName)
        {
            // �ο� http://geekswithblogs.net/mnf/articles/78888.aspx
            string strFolderPath = "IIS://localhost" + strTargetSite + "/ROOT/" + strVDir;
            string strFilePath = strFolderPath + "/" + strFileName;

            DirectoryEntry folderEntry = new DirectoryEntry(strFolderPath);
            DirectoryEntry fileEntry = null;

            // Debug.Assert(false, "����");

            // string strText = GetAllChildrenNames(entry);

            if (DirectoryEntry.Exists(strFilePath) == false)
            {
                // ����ļ���metabase�в�����
                string SchemaClassName = "IIsObject";

                // can't assign "IIsWebFile" directly, causes HRESULT: 0x8000500F exception. E_ADS_SCHEMA_VIOLATION - The attempted action violates the directory service schema rules". 
                // see http://groups.google.com.au/group/microsoft.public.adsi.general/browse_frm/thread/3b339d218e673aca/050974e5903530e3  
                fileEntry = folderEntry.Children.Add(strFileName, SchemaClassName);


                fileEntry.CommitChanges();  // �������ύһ�Σ�������������޸Ĳ���һ���ύ�����׳��쳣
                folderEntry.CommitChanges();

                fileEntry = new DirectoryEntry(strFilePath);
                // Fortunately ADSUTIL shows the WARNING: The Object Type of this object was not specified or was specified as IIsObject. 
                // This means that you will not be able to set or get properties on the object until the KeyType property is set.
                fileEntry.Properties["keyType"].Value = "IIsWebFile";
                fileEntry.CommitChanges();

            }

            fileEntry = new DirectoryEntry(strFilePath);

            fileEntry.Properties["AuthAnonymous"][0] = false;
            // entry.Properties["AccessRead"][0] = true;

            fileEntry.CommitChanges();
        }


        // Adds an ACL entry on the specified directory for the specified account.
        public static void AddDirectorySecurity(string FileName,
            string Account,
            FileSystemRights Rights,
            AccessControlType ControlType)
        {
            // Create a new DirectoryInfo object.
            DirectoryInfo dInfo = new DirectoryInfo(FileName);

            // Get a DirectorySecurity object that represents the 
            // current security settings.
            DirectorySecurity dSecurity = dInfo.GetAccessControl();

            // Add the FileSystemAccessRule to the security settings. 
            dSecurity.AddAccessRule(new FileSystemAccessRule(Account,
                                                            Rights,
                                                            InheritanceFlags.None,
                                                            PropagationFlags.NoPropagateInherit,
                                                            ControlType));

            // *** Always allow objects to inherit on a directory 
            FileSystemAccessRule AccessRule = null;
            AccessRule = new FileSystemAccessRule(Account,
                Rights,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.InheritOnly,
                ControlType);

            bool Result = false;
            dSecurity.ModifyAccessRule(AccessControlModification.Add,
                AccessRule,
                out Result);
            if (!Result)
            {
                throw (new Exception("add failed"));
            }

            // Set the new access settings.
            dInfo.SetAccessControl(dSecurity);
        }

        // Adds an ACL entry on the specified file for the specified account.
        public static void AddFileSecurity(string FileName,
            string Account,
            FileSystemRights Rights,
            AccessControlType ControlType)
        {
            // Create a new FileInfo object.
            FileInfo fInfo = new FileInfo(FileName);

            // Get a FileSecurity object that represents the 
            // current security settings.
            FileSecurity fileSecurity = fInfo.GetAccessControl();


            // Add the FileSystemAccessRule to the security settings. 
            fileSecurity.AddAccessRule(new FileSystemAccessRule(Account,
                                                            Rights,
                                                            InheritanceFlags.None,
                                                            PropagationFlags.NoPropagateInherit,
                                                            ControlType));

            /*
            // *** Always allow objects to inherit on a directory 
            FileSystemAccessRule AccessRule = null;
            AccessRule = new FileSystemAccessRule(Account,
                Rights,
                InheritanceFlags.None,
                PropagationFlags.InheritOnly,
                ControlType);

            bool Result = false;
            fileSecurity.ModifyAccessRule(AccessControlModification.Add,
                AccessRule,
                out Result);
            if (!Result)
            {
                throw (new Exception("add failed"));
            }
             */

            // Set the new access settings.
            fInfo.SetAccessControl(fileSecurity);
        }

        // ���氲װ�������Ա��޸�ʱʹ��
        public static void SetSetupParams(string strRootDir,
            string strVDir)
        {
            string strSetupCfgFileName = PathUtil.MergePath(strRootDir, "SetupCfg.xml");

            string strXml = "<root><vdir>" + strVDir + "</vdir></root>";

            FileUtil.String2File(strXml, strSetupCfgFileName);
        }

        // �ӱ����ļ��еõ���װ����
        public static int GetSetupParams(string strRootDir,
            out string strVDir,
            out string strError)
        {
            strError = "";
            strVDir = "";

            string strSetupCfgFileName = PathUtil.MergePath(strRootDir, "SetupCfg.xml");
            if (File.Exists(strSetupCfgFileName) == false)
                return 0;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.Load(strSetupCfgFileName);
            }
            catch (Exception ex)
            {
                strError = "����'SetupCfg.xml'�ļ���dom����" + ex.Message;
                return -1;
            }

            XmlNode node = dom.DocumentElement.SelectSingleNode("vdir");
            if (node == null)
            {
                strError = "��'SetupCfg.xml'�ļ��ĸ���δ�ҵ�<vdir>�ڵ㡣";
                return -1;

            }
            // strVDir = DomUtil.GetNodeText(node);
            strVDir = node.InnerText.Trim();
            if (strVDir == "")
            {
                strError = "��'SetupCfg.xml'�ļ��ĸ��µ�<vdir>�ڵ������Ϊ�ա�";
                return -1;
            }

            return 0;
        }

#if NOOOOOOOOOOOOOOOOOOO
        // ��������Ŀ¼��������վ����
        // return
        //      �����ҵ���վ������
        public static int FindVDirWebsite(string strVDir,
            string strPhysicalPath,
            out ArrayList aWebSite,
            out string strError,
            out string strDebugInfo)
        {
            strError = "";
            strDebugInfo = "";
            aWebSite = new ArrayList();

            string strServerName = "localhost";
            string strPath = "IIS://" + strServerName + "/W3SVC";

            DirectoryEntry entryRoot = new DirectoryEntry(strPath);

            strDebugInfo += "����DirectoryEntry : " + strPath + "\r\n";

            strDebugInfo += "�������¼�����:\r\n";


            foreach (DirectoryEntry entry in entryRoot.Children)
            {
                //MessageBox.Show(entry.Name);
                strDebugInfo += "������ : " + entry.Name + "\r\n";

                if (StringUtil.IsPureNumber(entry.Name) == true)
                {
                    strDebugInfo += "������Ϊ����������.\r\n";

                    strPath = "IIS://" + strServerName + "/W3SVC/" + entry.Name + "/ROOT/" + strVDir;

                    //MessageBox.Show("Path:'" + strPath + "'");

                    DirectoryEntry tempentry = null;
                    try
                    {
                        strDebugInfo += "����DirectoryEntry : " + strPath + " ";

                        tempentry = new DirectoryEntry(strPath);
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show("catch");
                        strDebugInfo += "�׳��쳣 : " + ex.Message + "\r\n";
                        continue;
                    }

                    strDebugInfo += "�ɹ�.\r\n";


                    //MessageBox.Show("ȡ��'" + strPath + "'");

                    if (String.IsNullOrEmpty(strPhysicalPath) == false)
                    {
                        bool bExist = false;

                        string strTempPath = "";
                        try
                        {
                            strDebugInfo += "��ȡ��Path���� ";

                            object o = tempentry.Parent;    // �������������쳣
                            bExist = true;

                            strTempPath = (string)tempentry.Properties["Path"][0]; // �����׳��쳣

                        }
                        catch (Exception ex)
                        {
                            //MessageBox.Show(ex.Message + "\r\n type:" + ex.GetType().ToString());
                            strDebugInfo += "�׳��쳣 : " + ex.Message + " �쳣����:" + ex.GetType().ToString() + "\r\n";
                            if (bExist == true)
                            {
                                // ����Ŀ¼����, �����޷���ȡ����Ŀ¼
                                strDebugInfo += "����Ŀ¼����, �����޷���ȡ����Ŀ¼, ���������� '" + entry.Name + "' �����ʵؼ���վ���б�.\r\n";

                                aWebSite.Add(entry.Name);
                                continue;

                            }
                            continue;
                        }
                        strDebugInfo += "�ɹ�. ����ֵΪ'"+strTempPath+"'\r\n";

                        //MessageBox.Show("strPhysicalPath='" + strPhysicalPath + "' strTempPath='" + strTempPath + "'");

                        if (PathUtil.IsEqualEx(strPhysicalPath, strTempPath) == true)
                        {
                            strDebugInfo += "����·�� '" + strPhysicalPath + "' ��������ֵ '" + strTempPath + "', ���������� '"+entry.Name+"' ������վ���б�.\r\n";

                            aWebSite.Add(entry.Name);
                        }
                    }
                    else
                    {
                        strDebugInfo += "����·��Ϊ��, ������ '"+entry.Name+"' ������վ���б�.\r\n";

                        aWebSite.Add(entry.Name);
                    }
                }
            }

            return aWebSite.Count;
        }
#endif



    }

    // http://stackoverflow.com/questions/6824188/sqldatasourceenumerator-instance-getdatasources-does-not-locate-local-sql-serv
    public enum RegistryHive
    {
        Wow64,
        Wow6432
    }

    public class RegistryValueDataReader
    {
        private static readonly int KEY_WOW64_32KEY = 0x200;
        private static readonly int KEY_WOW64_64KEY = 0x100;

        private static readonly UIntPtr HKEY_LOCAL_MACHINE = (UIntPtr)0x80000002;

        private static readonly int KEY_QUERY_VALUE = 0x1;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegOpenKeyEx")]
        static extern int RegOpenKeyEx(
                    UIntPtr hKey,
                    string subKey,
                    uint options,
                    int sam,
                    out IntPtr phkResult);


        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegQueryValueEx(
                    IntPtr hKey,
                    string lpValueName,
                    int lpReserved,
                    out uint lpType,
                    IntPtr lpData,
                    ref uint lpcbData);

        private static int GetRegistryHiveKey(RegistryHive registryHive)
        {
            return registryHive == RegistryHive.Wow64 ? KEY_WOW64_64KEY : KEY_WOW64_32KEY;
        }

        private static UIntPtr GetRegistryKeyUIntPtr(RegistryKey registry)
        {
            if (registry == Registry.LocalMachine)
            {
                return HKEY_LOCAL_MACHINE;
            }

            return UIntPtr.Zero;
        }

        public string[] ReadRegistryValueData(RegistryHive registryHive, RegistryKey registryKey, string subKey, string valueName)
        {
            string[] instanceNames = new string[0];

            int key = GetRegistryHiveKey(registryHive);
            UIntPtr registryKeyUIntPtr = GetRegistryKeyUIntPtr(registryKey);

            IntPtr hResult;

            int res = RegOpenKeyEx(registryKeyUIntPtr, subKey, 0, KEY_QUERY_VALUE | key, out hResult);

            if (res == 0)
            {
                uint type;
                uint dataLen = 0;

                RegQueryValueEx(hResult, valueName, 0, out type, IntPtr.Zero, ref dataLen);

                byte[] databuff = new byte[dataLen];
                byte[] temp = new byte[dataLen];

                List<String> values = new List<string>();

                GCHandle handle = GCHandle.Alloc(databuff, GCHandleType.Pinned);
                try
                {
                    RegQueryValueEx(hResult, valueName, 0, out type, handle.AddrOfPinnedObject(), ref dataLen);
                }
                finally
                {
                    handle.Free();
                }

                int i = 0;
                int j = 0;

                while (i < databuff.Length)
                {
                    if (databuff[i] == '\0')
                    {
                        j = 0;
                        string str = Encoding.Default.GetString(temp).Trim('\0');

                        if (!string.IsNullOrEmpty(str))
                        {
                            values.Add(str);
                        }

                        temp = new byte[dataLen];
                    }
                    else
                    {
                        temp[j++] = databuff[i];
                    }

                    ++i;
                }

                instanceNames = new string[values.Count];
                values.CopyTo(instanceNames);
            }

            return instanceNames;
        }
    }

    /// <summary>
    /// �����ļ��¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void CopyFilesEventHandler(object sender,
    CopyFilesEventArgs e);

    /// <summary>
    /// �����ļ��¼��Ĳ���
    /// </summary>
    public class CopyFilesEventArgs : EventArgs
    {
        /// <summary>
        /// ��������
        /// </summary>
        public string Action = "";

        /// <summary>
        /// Ŀ��Ŀ¼
        /// </summary>
        public string DataDir = "";

        /// <summary>
        /// [out] ������Ϣ
        /// </summary>
        public string ErrorInfo = "";
    }

    public delegate void VerifyEventHandler(object sender,
    VerifyEventArgs e);

    public class VerifyEventArgs : EventArgs
    {
        public string Value = "";   // [in] ҪУ���ֵ
        public string ErrorInfo = "";   // [out]������Ϣ
    }

    //
    public delegate void LoadXmlFileInfoEventHandler(object sender,
    LoadXmlFileInfoEventArgs e);

    public class LoadXmlFileInfoEventArgs : EventArgs
    {
        public string DataDir = "";   // [in] ����Ŀ¼

        public object LineInfo = null;    // out
        public string ErrorInfo = "";   // [out]������Ϣ
    }
}
