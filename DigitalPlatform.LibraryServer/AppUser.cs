using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Serialization;

using DigitalPlatform;	// Stop��
using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Script;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Marc;
using DigitalPlatform.Range;

using DigitalPlatform.Message;
using DigitalPlatform.rms.Client.rmsws_localhost;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// �������Ǻ��û�������صĴ���
    /// </summary>
    public partial class LibraryApplication
    {
        // �������һ���ض����ݿ⡢�ض�������Ȩ�޶����ַ���
        /*
            ԭʼ�����ַ�����ʽ�� "�����:setbiblioinfo=new,change|getbiblioinfo=xxx;������:setbiblioinfo=new"
         * 
         * */
        // parameters:
        //      strDbName   ���ݿ��������Ϊ�գ���ʾƥ��Ȩ���ַ����е��������ݿ���
        // return:
        //      null    ָ���Ĳ������͵�Ȩ��û�ж���
        //      ""      ������ָ�����͵Ĳ���Ȩ�ޣ����Ƿ񶨵Ķ���
        //      ����      Ȩ���б�* ��ʾͨ���Ȩ���б�
        public static string GetDbOperRights(string strAccessString,
            string strDbName,
            string strOperation)
        {
            // string[] segments = strAccessString.Split(new char[] {';'});
            List<string> segments = StringUtil.SplitString(strAccessString,
                ";",
                new string [] {"()"},
                StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Count; i++)
            {
                string strSegment = segments[i].Trim();
                if (String.IsNullOrEmpty(strSegment) == true)
                    continue;
                string strDbNameList = "";

                int nRet = strSegment.IndexOf(":");
                if (nRet == -1)
                {
                    // �������ݿ����б��֣��������б��Ȩ���б�Ϊ*
                    strDbNameList = strSegment;

                    // ʣ�ಿ��
                    strSegment = "*";
                    goto DOMATCH;
                }
                else
                {
                    strDbNameList = strSegment.Substring(0, nRet).Trim();

                    // ʣ�ಿ��
                    strSegment = strSegment.Substring(nRet + 1).Trim();
                }

            DOMATCH:
                // string[] sections = strSegment.Split(new char[] {'|'});
                List<string> sections = StringUtil.SplitString(strSegment,
                    "|",
                    new string[] { "()" },
                    StringSplitOptions.RemoveEmptyEntries);

                for (int j = 0; j<sections.Count; j++)
                {
                    string strOperList = "";
                    string strRightsList = "";

                    string strSection = sections[j];
                    if (String.IsNullOrEmpty(strSection) == true)
                        continue;

                    nRet = strSection.IndexOf("=");
                    if (nRet == -1)
                    {
                        // ���в������б��֣�Ȩ���б�Ϊ*
                        strOperList = strSection;
                        strRightsList = "*";
                    }
                    else
                    {
                        strOperList = strSection.Substring(0, nRet).Trim();
                        strRightsList = strSection.Substring(nRet + 1).Trim();
                    }

                    if (strDbNameList == "*")
                    {
                        // ���ݿ���ͨ��
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(strDbName) == false    // ������� strDbName Ϊ�գ����κο�������ƥ��
                            && StringUtil.IsInList(strDbName, strDbNameList) == false)
                            continue;   // ���ݿ��������б���
                    }

                    if (strOperList == "*")
                    {
                        // ������ͨ��
                    }
                    else
                    {
                        if (StringUtil.IsInList(strOperation, strOperList) == false)
                            continue;   // �����������б���
                    }

                    return strRightsList;
                }
            }

            return null;    // not found
        }

        // ���һ���˻�����Ϣ�����ܵ�ǰ�û��Ĺ�Ͻ��Χ�����ơ������������ֻ���ṩ�ڲ�ʹ�ã�Ҫ����
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        public int GetUserInfo(string strUserName,
            out UserInfo userinfo,
            out string strError)
        {
            strError = "";
            userinfo = null;

            if (string.IsNullOrEmpty(strUserName) == true)
            {
                strError = "�û�������Ϊ��";
                return -1;
            }

            UserInfo[] userinfos = null;
            // return:
            //      -1  ����
            //      ����    �û����������Ǳ����ĸ�����
            int nRet = ListUsers(
                "",
                strUserName,
                0,
                1,
                out userinfos,
                out strError);
            if (nRet == -1)
                return -1;

            if (nRet == 0)
                return 0;   // not found

            if (userinfos == null || userinfos.Length < 1)
            {
                strError = "userinfos error";
                return -1;
            }
            userinfo = userinfos[0];

            return 1;
        }

        // �г�ָ�����û�
        // parameters:
        //      strUserName �û��������Ϊ�գ���ʾ�г�ȫ���û���
        // return:
        //      -1  ����
        //      ����    �û����������Ǳ����ĸ�����
        public int ListUsers(
            string strLibraryCodeList,
            string strUserName,
            int nStart,
            int nCount,
            out UserInfo[] userinfos,
            out string strError)
        {
            this.m_lock.AcquireReaderLock(m_nLockTimeout);
            try
            {

                strError = "";
                userinfos = null;

                string strXPath = "";

                if (String.IsNullOrEmpty(strUserName) == true)
                {
                    strXPath = "//accounts/account";
                }
                else
                {
                    strXPath = "//accounts/account[@name='" + strUserName + "']";
                }


                List<UserInfo> userList = new List<UserInfo>();

                XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes(strXPath);

                // ����Ϊ��ǰ�ܹ�Ͻ��С��Χnode����
                List<XmlNode> smallerlist = new List<XmlNode>();
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    // 2012/9/9
                    // �ֹ��û�ֻ�����г���Ͻ�ֹݵ������û�
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        string strCurrentLibraryCodeList = DomUtil.GetAttr(node, "libraryCode");
                        // TODO: �ʻ������еĹݴ����б��в����� ,, ���������
                        if (IsListInList(strCurrentLibraryCodeList, strLibraryCodeList) == false)
                            continue;
                    }

                    smallerlist.Add(node);
                }

                if (nCount == -1)
                    nCount = Math.Max(0, smallerlist.Count - nStart);
                nCount = Math.Min(100, nCount); // ����ÿ�����100��

                for (int i = nStart; i < Math.Min(nStart + nCount, smallerlist.Count); i++)   // 
                {
                    XmlNode node = smallerlist[i];

                    string strCurrentLibraryCodeList = DomUtil.GetAttr(node, "libraryCode");

                    UserInfo userinfo = new UserInfo();
                    userinfo.UserName = DomUtil.GetAttr(node, "name");
                    userinfo.Type = DomUtil.GetAttr(node, "type");
                    userinfo.Rights = DomUtil.GetAttr(node, "rights");
                    userinfo.LibraryCode = strCurrentLibraryCodeList;
                    userinfo.Access = DomUtil.GetAttr(node, "access");
                    userinfo.Comment = DomUtil.GetAttr(node, "comment");

                    userList.Add(userinfo);
                }

                userinfos = new UserInfo[userList.Count];
                userList.CopyTo(userinfos);

                return smallerlist.Count;
            }
            finally
            {
                this.m_lock.ReleaseReaderLock();
            }
        }

        // �������û�
        // TODO: ��DOM����
        public int CreateUser(
            string strLibraryCodeList,
            string strUserName,
            string strOperator,
            UserInfo userinfo,
            string strClientAddress,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strUserName) == true)
            {
                strError = "strUserName����ֵ����Ϊ��";
                return -1;
            }

            if (strUserName != userinfo.UserName)
            {
                strError = "strUserName����ֵ��userinfo.UserName��һ��";
                return -1;
            }

            // 2012/9/9
            // �ֹ��û�ֻ�������ݴ������ڹ�Ͻ�ֹݵ��ʻ�
            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
            {
                if (string.IsNullOrEmpty(userinfo.LibraryCode) == true
                    || IsListInList(userinfo.LibraryCode, strLibraryCodeList) == false)
                {
                    strError = "��ǰ�û�ֻ�ܴ���ͼ��ݴ�����ȫ���� '" + strLibraryCodeList + "' ��Χ�����û�";
                    return -1;
                }
            }

            int nResultValue = -1;
            // ������ֿռ䡣
            // return:
            //      -2  not found script
            //      -1  ����
            //      0   �ɹ�
            int nRet = this.DoVerifyBarcodeScriptFunction(
                null,
                "",
                strUserName,
                out nResultValue,
                out strError);
            if (nRet == -2)
            {
                // û��У������Ź��ܣ������޷�У���û�������������ֿռ�ĳ�ͻ
                goto SKIP_VERIFY;
            }
            if (nRet == -1)
            {
                strError = "У���û��� '" + strUserName + "' �������Ǳ�ڳ�ͻ������(���ú���DoVerifyBarcodeScriptFunction()ʱ)��������: " + strError;
                return -1;
            }

            Debug.Assert(nRet == 0, "");

            if (nResultValue == -1)
            {
                strError = "У���û��� '" + strUserName + "' �������Ǳ�ڳ�ͻ�����з�������: " + strError;
                return -1;
            }

            if (nResultValue == 1)
            {
                strError = "���� '" + strUserName + "' ����������ֿռ䷢����ͻ��������Ϊ�û�����";
                return -1;
            }

        SKIP_VERIFY:
            XmlNode nodeAccount = null;

            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {

                // ����
                nodeAccount = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//accounts/account[@name='" + strUserName + "']");
                if (nodeAccount != null)
                {
                    strError = "�û� '" + strUserName + "' �Ѿ�����";
                    return -1;
                }

                XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("accounts");
                if (root == null)
                {
                    root = this.LibraryCfgDom.CreateElement("accounts");
                    this.LibraryCfgDom.DocumentElement.AppendChild(root);
                }

                nodeAccount = this.LibraryCfgDom.CreateElement("account");
                root.AppendChild(nodeAccount);

                DomUtil.SetAttr(nodeAccount, "name", userinfo.UserName);

                if (String.IsNullOrEmpty(userinfo.Type) == false)
                    DomUtil.SetAttr(nodeAccount, "type", userinfo.Type);

                DomUtil.SetAttr(nodeAccount, "rights", userinfo.Rights);

                DomUtil.SetAttr(nodeAccount, "libraryCode", userinfo.LibraryCode);

                DomUtil.SetAttr(nodeAccount, "access", userinfo.Access);

                DomUtil.SetAttr(nodeAccount, "comment", userinfo.Comment);

                // ��������
                if (userinfo.SetPassword == true)
                {
                    string strPassword = Cryptography.Encrypt(userinfo.Password,
                        EncryptKey);
                    DomUtil.SetAttr(nodeAccount, "password", strPassword);
                }

                this.Changed = true;

                // 2014/9/16
                if (userinfo.UserName == "reader")
                    this.ClearLoginCache("");
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

            {
                XmlDocument domOperLog = PrepareOperlogDom("new", strOperator);
                XmlNode node = domOperLog.CreateElement("account");
                domOperLog.DocumentElement.AppendChild(node);

                DomUtil.SetElementOuterXml(node, nodeAccount.OuterXml);

                // д����־
                nRet = this.OperLog.WriteOperLog(domOperLog,
                    strClientAddress,
                    out strError);
                if (nRet == -1)
                {
                    strError = "SetUser() API д����־ʱ��������: " + strError;
                    return -1;
                }
            }

            return 0;
        }

        XmlDocument PrepareOperlogDom(string strAction,
            string strOperator)
        {
            // ׼����־DOM
            XmlDocument domOperLog = new XmlDocument();
            domOperLog.LoadXml("<root />");
            // �������漰�����߿⣬����û��<libraryCode>Ԫ��
            DomUtil.SetElementText(domOperLog.DocumentElement, "operation",
                "setUser");
            DomUtil.SetElementText(domOperLog.DocumentElement, "action",
                strAction);

            string strOperTimeString = this.Clock.GetClock();   // RFC1123��ʽ

            DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                strOperator);
            DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                strOperTimeString);

            return domOperLog;
        }

        // ���û�������
        public bool SearchUserNameDup(string strUserName)
        {
            this.m_lock.AcquireReaderLock(m_nLockTimeout);
            try
            {
                // ����
                XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//accounts/account[@name='" + strUserName + "']");
                if (node != null)
                    return true;

                return false;
            }
            finally
            {
                this.m_lock.ReleaseReaderLock();
            }
        }

        // �޸��û����롣����ָ�û��޸��Լ��ʻ������룬���ṩ������
        // return:
        //      -1  error
        //      0   succeed
        public int ChangeUserPassword(
            string strLibraryCodeList,
            string strUserName,
            string strOldPassword,
            string strNewPassword,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strUserName) == true)
            {
                strError = "strUserName����ֵ����Ϊ��";
                return -1;
            }

            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {

                // ����
                XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//accounts/account[@name='" + strUserName + "']");
                if (node == null)
                {
                    strError = "�û� '" + strUserName + "' ������";
                    return -1;
                }

                string strExistLibraryCodeList = DomUtil.GetAttr(node, "libraryCode");

                // 2012/9/9
                // �ֹ��û�ֻ�����޸Ĺݴ������ڹ�Ͻ�ֹݵ��ʻ�
                if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                {
                    if (string.IsNullOrEmpty(strExistLibraryCodeList) == true
                        || IsListInList(strExistLibraryCodeList, strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�ֻ���޸�ͼ��ݴ�����ȫ��ȫ���� '" + strLibraryCodeList + "' ��Χ���û�������";
                        return -1;
                    }
                }

                // ��֤������
                string strExistPassword = DomUtil.GetAttr(node, "password");
                if (String.IsNullOrEmpty(strExistPassword) == false)
                {
                    try
                    {
                        strExistPassword = Cryptography.Decrypt(strExistPassword,
                            EncryptKey);
                    }
                    catch
                    {
                        strError = "�Ѿ����ڵ�(���ܺ�)�����ʽ����ȷ";
                        return -1;
                    }
                }

                if (strExistPassword != strOldPassword)
                {
                    strError = "���ṩ�ľ����뾭��֤��ƥ��";
                    return -1;
                }

                // ����������
                strNewPassword = Cryptography.Encrypt(strNewPassword,
                        EncryptKey);
                DomUtil.SetAttr(node, "password", strNewPassword);

                this.Changed = true;

                return 0;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

            // return 0;
        }

        public int ChangeKernelPassword(
            SessionInfo sessioninfo,
            string strOldPassword,
            string strNewPassword,
            out string strError)
        {
            strError = "";

            // ��֤�������Ƿ���� library.xml �еĶ���
            if (strOldPassword != this.ManagerPassword)
            {
                strError = "�����벻�Ǻϡ��޸� kernel ����ʧ��";
                return -1;
            }

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            //		value == -1 ����
            //				 0  �û��������벻��ȷ
            //				 1  �ɹ�
            int nRet = channel.Login(this.ManagerUserName,
                strOldPassword,
                out strError);
            if (nRet == -1)
            {
                strError = "��¼ dp2kernel ʧ�ܣ�" + strError;
                return -1;
            }
            if (nRet == 0)
            {
                strError = "������� dp2kernel �ʻ����Ǻϡ��޸� kernel ����ʧ��";
                return -1;
            }

            // return:
            //		-1	����������Ϣ��strError��
            //		0	�ɹ���
            nRet = channel.ChangePassword(this.ManagerUserName,
                strOldPassword,
                strNewPassword,
                false,
                out strError);
            if (nRet == -1)
            {
                strError = "�� dp2kernel ���޸�����ʧ�ܣ�" + strError;
                return -1;
            }

            this.ManagerPassword = strNewPassword;
            this.Changed = true;
            return 0;
        }

        // �޸��û�
        public int ChangeUser(
            string strLibraryCodeList,
            string strUserName,
            string strOperator,
            UserInfo userinfo,
            string strClientAddress,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strUserName) == true)
            {
                strError = "strUserName����ֵ����Ϊ��";
                return -1;
            }

            if (strUserName != userinfo.UserName)
            {
                strError = "strUserName����ֵ��userinfo.UserName��һ��";
                return -1;
            }

            XmlNode nodeAccount = null;
            string strOldOuterXml = "";

            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                // ����
                nodeAccount = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//accounts/account[@name='" + strUserName + "']");
                if (nodeAccount == null)
                {
                    strError = "�û� '" + strUserName + "' ������";
                    return -1;
                }

                strOldOuterXml = nodeAccount.OuterXml;

                string strExistLibraryCodeList = DomUtil.GetAttr(nodeAccount, "libraryCode");

                // 2012/9/9
                // �ֹ��û�ֻ�����޸Ĺݴ������ڹ�Ͻ�ֹݵ��ʻ�
                if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                {
                    if (string.IsNullOrEmpty(strExistLibraryCodeList) == true
                        || IsListInList(strExistLibraryCodeList, strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�ֻ���޸�ͼ��ݴ�����ȫ���� '" + strLibraryCodeList + "' ��Χ���û���Ϣ";
                        return -1;
                    }
                }

                // 2012/9/9
                // �ֹ��û�ֻ�����ʻ��Ĺݴ����޸ĵ�ָ����Χ��
                if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                {
                    if (string.IsNullOrEmpty(userinfo.LibraryCode) == true
                        || IsListInList(userinfo.LibraryCode, strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�ֻ�ܽ��û���Ϣ�Ĺݴ����޸ĵ���ȫ���� '" + strLibraryCodeList + "' ��Χ�ڵ�ֵ";
                        return -1;
                    }
                }

                DomUtil.SetAttr(nodeAccount, "name", userinfo.UserName);
                DomUtil.SetAttr(nodeAccount, "type", userinfo.Type);
                DomUtil.SetAttr(nodeAccount, "rights", userinfo.Rights);
                DomUtil.SetAttr(nodeAccount, "libraryCode", userinfo.LibraryCode);
                DomUtil.SetAttr(nodeAccount, "access", userinfo.Access);
                DomUtil.SetAttr(nodeAccount, "comment", userinfo.Comment);

                // ǿ���޸����롣������֤������
                if (userinfo.SetPassword == true)
                {
                    string strPassword = Cryptography.Encrypt(userinfo.Password,
                        EncryptKey);
                    DomUtil.SetAttr(nodeAccount, "password", strPassword);
                }

                this.Changed = true;

                // 2014/9/16
                if (userinfo.UserName == "reader")
                    this.ClearLoginCache("");
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

            {
                XmlDocument domOperLog = PrepareOperlogDom("change", strOperator);

                if (string.IsNullOrEmpty(strOldOuterXml) == false)
                {
                    XmlNode node_old = domOperLog.CreateElement("oldAccount");
                    domOperLog.DocumentElement.AppendChild(node_old);
                    node_old = DomUtil.SetElementOuterXml(node_old, strOldOuterXml);
                    DomUtil.RenameNode(node_old,
                        null,
                        "oldAccount");
                }

                XmlNode node = domOperLog.CreateElement("account");
                domOperLog.DocumentElement.AppendChild(node);

                DomUtil.SetElementOuterXml(node, nodeAccount.OuterXml);

                // д����־
                int nRet = this.OperLog.WriteOperLog(domOperLog,
                    strClientAddress,
                    out strError);
                if (nRet == -1)
                {
                    strError = "SetUser() API д����־ʱ��������: " + strError;
                    return -1;
                }
            }
            return 0;
        }

        // list1�е�ֵ�Ƿ�ȫ������list2�У�
        static bool IsListInList(string strList1, string strList2)
        {
            string[] parts1 = strList1.Split(new char[] { ',' });
            string[] parts2 = strList2.Split(new char[] { ',' });

            int nCount = 0;
            foreach (string s1 in parts1)
            {
                string strText1 = s1.Trim();
                if (string.IsNullOrEmpty(strText1) == true)
                    continue;
                bool bFound = false;
                foreach (string s2 in parts2)
                {
                    string strText2 = s2.Trim();
                    if (string.IsNullOrEmpty(strText2) == true)
                        continue;
                    if (strText1 == strText2)
                    {
                        bFound = true;
                        break;
                    }
                }

                if (bFound == false)
                    return false;

                nCount++;
            }

            if (nCount == 0)
                return false;

            return true;
        }

        // ǿ���޸��û����롣���޸�������Ϣ��
        public int ResetUserPassword(
            string strLibraryCodeList,
            string strUserName,
            string strOperator,
            string strNewPassword,
            string strClientAddress,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strUserName) == true)
            {
                strError = "strUserName����ֵ����Ϊ��";
                return -1;
            }

            XmlNode nodeAccount = null;
            string strHashedPassword = "";

            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {

                // ����
                nodeAccount = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//accounts/account[@name='" + strUserName + "']");
                if (nodeAccount == null)
                {
                    strError = "�û� '" + strUserName + "' ������";
                    return -1;
                }

                string strExistLibraryCodeList = DomUtil.GetAttr(nodeAccount, "libraryCode");

                // 2012/9/9
                // �ֹ��û�ֻ�����޸Ĺݴ������ڹ�Ͻ�ֹݵ��ʻ�
                if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                {
                    if (string.IsNullOrEmpty(strExistLibraryCodeList) == true
                        || IsListInList(strExistLibraryCodeList, strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�ֻ������ ͼ��ݴ�����ȫ���� '" + strLibraryCodeList + "' ��Χ���û�������";
                        return -1;
                    }
                }

                // ǿ���޸����롣������֤������
                strHashedPassword = Cryptography.Encrypt(strNewPassword,
                    EncryptKey);
                DomUtil.SetAttr(nodeAccount, "password", strHashedPassword);

                this.Changed = true;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

            {
                XmlDocument domOperLog = PrepareOperlogDom("resetpassword", strOperator);

                XmlNode node = domOperLog.CreateElement("newPassword");
                domOperLog.DocumentElement.AppendChild(node);

                node.InnerText = strHashedPassword;

                // д����־
                int nRet = this.OperLog.WriteOperLog(domOperLog,
                    strClientAddress,
                    out strError);
                if (nRet == -1)
                {
                    strError = "SetUser() API д����־ʱ��������: " + strError;
                    return -1;
                }
            }

            return 0;
        }


        // ɾ���û�
        public int DeleteUser(
            string strLibraryCodeList,
            string strUserName,
            string strOperator,
            string strClientAddress,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strUserName) == true)
            {
                strError = "strUserName����ֵ����Ϊ��";
                return -1;
            }

            XmlNode nodeAccount = null;
            string strOldOuterXml = "";

            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {

                // ����
                nodeAccount = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//accounts/account[@name='" + strUserName + "']");
                if (nodeAccount == null)
                {
                    strError = "�û� '" + strUserName + "' ������";
                    return -1;
                }
                strOldOuterXml = nodeAccount.OuterXml;

                string strExistLibraryCodeList = DomUtil.GetAttr(nodeAccount, "libraryCode");

                // 2012/9/9
                // �ֹ��û�ֻ����ɾ���ݴ������ڹ�Ͻ�ֹݵ��ʻ�
                if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                {
                    if (string.IsNullOrEmpty(strExistLibraryCodeList) == true
                        || IsListInList(strExistLibraryCodeList, strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�ֻ��ɾ�� ͼ��ݴ�����ȫ���� '" + strLibraryCodeList + "' ��Χ���û�";
                        return -1;
                    }
                }


                nodeAccount.ParentNode.RemoveChild(nodeAccount);

                this.Changed = true;

                // 2014/9/16
                if (strUserName == "reader")
                    this.ClearLoginCache("");
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

            {
                XmlDocument domOperLog = PrepareOperlogDom("delete", strOperator);

                if (string.IsNullOrEmpty(strOldOuterXml) == false)
                {
                    XmlNode node_old = domOperLog.CreateElement("oldAccount");
                    domOperLog.DocumentElement.AppendChild(node_old);
                    node_old = DomUtil.SetElementOuterXml(node_old, strOldOuterXml);
                    DomUtil.RenameNode(node_old,
                        null,
                        "oldAccount");
                }

                // д����־
                int nRet = this.OperLog.WriteOperLog(domOperLog,
                    strClientAddress,
                    out strError);
                if (nRet == -1)
                {
                    strError = "SetUser() API д����־ʱ��������: " + strError;
                    return -1;
                }
            }

            return 0;
        }

        // ��װ
        public int SetUser(
            string strLibraryCodeList,
            string strAction,
            string strOperator,
            UserInfo info,
            string strClientAddress,
            out string strError)
        {
            if (strAction == "new")
            {
                return this.CreateUser(strLibraryCodeList, 
                    info.UserName,
                    strOperator,
                    info,
                    strClientAddress,
                    out strError);
            }

            if (strAction == "change")
            {
                return this.ChangeUser(strLibraryCodeList, 
                    info.UserName,
                    strOperator,
                    info,
                    strClientAddress,
                    out strError);
            }

            if (strAction == "resetpassword")
            {
                return this.ResetUserPassword(strLibraryCodeList, 
                    info.UserName,
                    strOperator,
                    info.Password,
                    strClientAddress,
                    out strError);
            }

            if (strAction == "delete")
            {
                return this.DeleteUser(strLibraryCodeList, 
                    info.UserName,
                    strOperator,
                    strClientAddress,
                    out strError);
            }

            strError = "δ֪�Ķ��� '" + strAction + "'";
            return -1;
        }
    }

    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class UserInfo
    {
        [DataMember]
        public string UserName = "";    // �û���

        [DataMember]
        public bool SetPassword = false;    // �Ƿ���������
        [DataMember]
        public string Password = "";    // ����

        [DataMember]
        public string Rights = "";  // Ȩ��ֵ
        [DataMember]
        public string Type = "";    // �˻�����

        [DataMember]
        public string LibraryCode = ""; // ͼ��ݴ��� 2007/12/15 new add

        [DataMember]
        public string Access = "";  // ���ڴ�ȡȨ�޵Ķ��� 2008/2/28 new add

        [DataMember]
        public string Comment = "";  // ע�� 2012/10/8
    }
}
