using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Net.Mail;
using System.Web;

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
    /// �������Ǻ����ݿ�����йصĴ���
    /// </summary>
    public partial class LibraryApplication
    {
        // �������ݿ�
        // parameters:
        //      strLibraryCodeList  ��ǰ�û��Ĺ�Ͻ�ֹݴ����б�
        //      strAction   ������getinfo create delete change initialize backup refresh
        public int ManageDatabase(
            RmsChannelCollection Channels,
            string strLibraryCodeList,
            string strAction,
            string strDatabaseNames,
            string strDatabaseInfo,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            // �г����ݿ���
            if (strAction == "getinfo")
            {
                return GetDatabaseInfo(
                    strLibraryCodeList,
                    strDatabaseNames,
                    out strOutputInfo,
                    out strError);
            }

            // �������ݿ�
            if (strAction == "create")
            {
                return CreateDatabase(
                    Channels,
                    strLibraryCodeList,
                    // strDatabaseNames,
                    strDatabaseInfo,
                    false,
                    out strOutputInfo,
                    out strError);
            }

            // ���´������ݿ�
            if (strAction == "recreate")
            {
                return CreateDatabase(
                    Channels,
                    strLibraryCodeList,
                    // strDatabaseNames,
                    strDatabaseInfo,
                    true,
                    out strOutputInfo,
                    out strError);
            }

            // ɾ�����ݿ�
            if (strAction == "delete")
            {
                return DeleteDatabase(
                    Channels,
                    strLibraryCodeList,
                    strDatabaseNames,
                    out strOutputInfo,
                    out strError);
            }

            if (strAction == "initialize")
            {
                return InitializeDatabase(
                    Channels,
                    strLibraryCodeList,
                    strDatabaseNames,
                    out strOutputInfo,
                    out strError);
            }

            // 2008/11/16
            if (strAction == "refresh")
            {
                return RefreshDatabaseDefs(
                    Channels,
                    strLibraryCodeList,
                    strDatabaseNames,
                    strDatabaseInfo,
                    out strOutputInfo,
                    out strError);
            }

            // �޸����ݿ�
            if (strAction == "change")
            {
                return ChangeDatabase(
                    Channels,
                    strLibraryCodeList,
                    strDatabaseNames,
                    strDatabaseInfo,
                    out strOutputInfo,
                    out strError);
            }


            strError = "δ֪��strActionֵ '" + strAction + "'";
            return -1;
        }

        // ɾ��һ�����ݿ⣬��ɾ��library.xml�����OPAC�����ⶨ��
        // ������ݿⲻ���ڻᵱ������-1������
        int DeleteDatabase(RmsChannel channel,
            string strDbName,
            out string strError)
        {
            strError = "";
            long lRet = channel.DoDeleteDB(strDbName, out strError);
            if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                return -1;

            // ɾ��һ�����ݿ���OPAC�ɼ������еĶ���
            // return:
            //      -1  error
            //      0   not change
            //      1   changed
            int nRet = RemoveOpacDatabaseDef(
                channel.Container,
                strDbName,
                out strError);
            if (nRet == -1)
            {
                this.Changed = true;
                return -1;
            }

            return 0;
        }

#if NO
        // ��װ��İ汾
        int ChangeDbName(
    RmsChannel channel,
    string strOldDbName,
    string strNewDbName,
    out string strError)
        {
            return ChangeDbName(
            channel,
            strOldDbName,
            strNewDbName,
            () => { },
            out strError);
        }
#endif

        // parameters:
        //      change_complte  �������޸ĳɹ������β����
        int ChangeDbName(
            RmsChannel channel,
            string strOldDbName,
            string strNewDbName,
            Action change_complete,
            out string strError)
        {
            strError = "";

            // TODO: Ҫ�� strNewDbName ���в��أ������ǲ����Ѿ���ͬ�������ݿ������
            // ���� DoSetDBInfo() API �Ƿ�����أ�

            List<string[]> log_names = new List<string[]>();
            string[] one = new string[2];
            one[0] = strNewDbName;
            one[1] = "zh";
            log_names.Add(one);

            // �޸����ݿ���Ϣ
            // parameters:
            //		logicNames	�߼�������ArrayList��ÿ��Ԫ��Ϊһ��string[2]���͡����е�һ���ַ���Ϊ���֣��ڶ���Ϊ���Դ���
            // return:
            //		-1	����
            //		0	�ɹ�(����WebService�ӿ�CreateDb�ķ���ֵ)
            long lRet = channel.DoSetDBInfo(
                    strOldDbName,
                    log_names,
                    null,   // string strType,
                    null,   // string strSqlDbName,
                    null,   // string strKeysDef,
                    null,   // string strBrowseDef,
                    out strError);
            if (lRet == -1)
                return -1;

            // �������޸ĳɹ������β����
            change_complete();

            // �޸�һ�����ݿ���OPAC�ɼ������еĶ��������
            // return:
            //      -1  error
            //      0   not change
            //      1   changed
            int nRet = RenameOpacDatabaseDef(
                channel.Container,
                strOldDbName,
                strNewDbName,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        // �޸����ݿ�
        int ChangeDatabase(
            RmsChannelCollection Channels,
            string strLibraryCodeList,
            string strDatabaseNames,
            string strDatabaseInfo,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            int nRet = 0;
            // long lRet = 0;

            bool bDbNameChanged = false;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strDatabaseInfo);
            }
            catch (Exception ex)
            {
                strError = "strDatabaseInfo����װ��XMLDOMʱ����: " + ex.Message;
                return -1;
            }

            // �˶�strDatabaseNames�а��������ݿ�����Ŀ�Ƿ��dom�е�<database>Ԫ�������
            string[] names = strDatabaseNames.Split(new char[] {','});
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");
            if (names.Length != nodes.Count)
            {
                strError = "strDatabaseNames�����а��������ݿ������� "+names.Length.ToString()+" ��strDatabaseInfo�����а�����<database>Ԫ���� "+nodes.Count.ToString()+" ����";
                return -1;
            }

            RmsChannel channel = Channels.GetChannel(this.WsUrl);

            for (int i = 0; i < names.Length; i++)
            {
                string strName = names[i].Trim();
                if (String.IsNullOrEmpty(strName) == true)
                {
                    strError = "strDatabaseNames�����в��ܰ����յ�����";
                    return -1;
                }

                // ����strDatabaseInfo
                XmlElement nodeNewDatabase = nodes[i] as XmlElement;

                // �޸���Ŀ������������Ŀ��������������ݿ���
                if (this.IsBiblioDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ����Ŀ��(biblioDbName����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������޸���Ŀ�ⶨ��";
                        return -1;
                    }

                    /*
                     * <database>Ԫ���е�name/entityDbName/orderDbName/issueDbName
                     * �����˸��������������ֵΪ�գ�������Ҫɾ�����ݿ⣬���Ǹ�������Ч��
                     * ʵ�������ﲻ��Ӧɾ�����ݿ�Ķ�����ֻ�ܸ���
                     * �������Ը���Ϊ��Ӧɾ�����ݿ��������ô��ֻҪ���Ծ߱������������������������Ҫ�����Ǹ�����
                     * */
                    {
                        // ��Ŀ����
                        string strOldBiblioDbName = strName;

                        // ����strDatabaseInfo
                        string strNewBiblioDbName = DomUtil.GetAttr(nodeNewDatabase,
                            "name");

                        // ���strNewBiblioDbNameΪ�գ���ʾ����ı�����
                        if (String.IsNullOrEmpty(strNewBiblioDbName) == false
                            && strOldBiblioDbName != strNewBiblioDbName)
                        {
                            if (String.IsNullOrEmpty(strOldBiblioDbName) == true
                                && String.IsNullOrEmpty(strNewBiblioDbName) == false)
                            {
                                strError = "Ҫ������Ŀ�� '" + strNewBiblioDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                                goto ERROR1;
                            }

                            nRet = ChangeDbName(
                                channel,
                                strOldBiblioDbName,
                                strNewBiblioDbName,
                                () => {
                                    DomUtil.SetAttr(nodeDatabase, "biblioDbName", strNewBiblioDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
#if NO
                            bDbNameChanged = true;
                            DomUtil.SetAttr(nodeDatabase, "biblioDbName", strNewBiblioDbName);
                            this.Changed = true;
#endif
                        }
                    }

                    {
                        // ʵ�����
                        string strOldEntityDbName = DomUtil.GetAttr(nodeDatabase,
                            "name");

                        // ����strDatabaseInfo
                        string strNewEntityDbName = DomUtil.GetAttr(nodeNewDatabase,
                            "entityDbName");

                        if (String.IsNullOrEmpty(strNewEntityDbName) == false
                            && strOldEntityDbName != strNewEntityDbName)
                        {
                            if (String.IsNullOrEmpty(strOldEntityDbName) == true
                                && String.IsNullOrEmpty(strNewEntityDbName) == false)
                            {
                                strError = "Ҫ����ʵ��� '" + strNewEntityDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                                goto ERROR1;
                            }

                            nRet = ChangeDbName(
                                channel,
                                strOldEntityDbName,
                                strNewEntityDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "name", strNewEntityDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

#if NO
                            bDbNameChanged = true;

                            DomUtil.SetAttr(nodeDatabase, "name", strNewEntityDbName);
                            this.Changed = true;
#endif
                        }
                    }


                    {
                        // ��������
                        string strOldOrderDbName = DomUtil.GetAttr(nodeDatabase,
                            "orderDbName");

                        // ����strDatabaseInfo
                        string strNewOrderDbName = DomUtil.GetAttr(nodeNewDatabase,
                            "orderDbName");
                        if (String.IsNullOrEmpty(strNewOrderDbName) == false
                            && strOldOrderDbName != strNewOrderDbName)
                        {
                            if (String.IsNullOrEmpty(strOldOrderDbName) == true
                                && String.IsNullOrEmpty(strNewOrderDbName) == false)
                            {
                                strError = "Ҫ���������� '" + strNewOrderDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                                goto ERROR1;
                            }

                            nRet = ChangeDbName(
                                channel,
                                strOldOrderDbName,
                                strNewOrderDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "orderDbName", strNewOrderDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

#if NO
                            bDbNameChanged = true;

                            DomUtil.SetAttr(nodeDatabase, "orderDbName", strNewOrderDbName);
                            this.Changed = true;
#endif
                        }
                    }

                    {
                        // �ڿ���
                        string strOldIssueDbName = DomUtil.GetAttr(nodeDatabase,
                            "issueDbName");

                        // ����strDatabaseInfo
                        string strNewIssueDbName = DomUtil.GetAttr(nodeNewDatabase,
                            "issueDbName");
                        if (String.IsNullOrEmpty(strNewIssueDbName) == false
                            && strOldIssueDbName != strNewIssueDbName)
                        {
                            if (String.IsNullOrEmpty(strOldIssueDbName) == true
                                && String.IsNullOrEmpty(strNewIssueDbName) == false)
                            {
                                strError = "Ҫ�����ڿ� '" + strNewIssueDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                                goto ERROR1;
                            }

                            nRet = ChangeDbName(
                                channel,
                                strOldIssueDbName,
                                strNewIssueDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "issueDbName", strNewIssueDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

#if NO
                            bDbNameChanged = true;

                            DomUtil.SetAttr(nodeDatabase, "issueDbName", strNewIssueDbName);
                            this.Changed = true;
#endif
                        }
                    }


                    {
                        // ��ע����
                        string strOldCommentDbName = DomUtil.GetAttr(nodeDatabase,
                            "commentDbName");

                        // ����strDatabaseInfo
                        string strNewCommentDbName = DomUtil.GetAttr(nodeNewDatabase,
                            "commentDbName");
                        if (String.IsNullOrEmpty(strNewCommentDbName) == false
                            && strOldCommentDbName != strNewCommentDbName)
                        {
                            if (String.IsNullOrEmpty(strOldCommentDbName) == true
                                && String.IsNullOrEmpty(strNewCommentDbName) == false)
                            {
                                strError = "Ҫ������ע�� '" + strNewCommentDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                                goto ERROR1;
                            }

                            nRet = ChangeDbName(
                                channel,
                                strOldCommentDbName,
                                strNewCommentDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "commentDbName", strNewCommentDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

#if NO
                            bDbNameChanged = true;

                            DomUtil.SetAttr(nodeDatabase, "commentDbName", strNewCommentDbName);
                            this.Changed = true;
#endif
                        }
                    }

                    // �Ƿ������ͨ
                    if (DomUtil.HasAttr(nodeNewDatabase, "inCirculation") == true)
                    {
                        string strOldInCirculation = DomUtil.GetAttr(nodeDatabase,
                            "inCirculation");
                        if (String.IsNullOrEmpty(strOldInCirculation) == true)
                            strOldInCirculation = "true";

                        string strNewInCirculation = DomUtil.GetAttr(nodeNewDatabase,
                            "inCirculation");
                        if (String.IsNullOrEmpty(strNewInCirculation) == true)
                            strNewInCirculation = "true";

                        if (strOldInCirculation != strNewInCirculation)
                        {
                            DomUtil.SetAttr(nodeDatabase, "inCirculation",
                                strNewInCirculation);
                            this.Changed = true;
                        }
                    }

                    // ��ɫ
                    // TODO: �Ƿ�Ҫ���м��?
                    if (DomUtil.HasAttr(nodeNewDatabase, "role") == true)
                    {
                        string strOldRole = DomUtil.GetAttr(nodeDatabase,
                            "role");

                        string strNewRole = DomUtil.GetAttr(nodeNewDatabase,
                            "role");

                        if (strOldRole != strNewRole)
                        {
                            DomUtil.SetAttr(nodeDatabase, "role",
                                strNewRole);
                            this.Changed = true;
                        }
                    }

                    // 2012/4/30
                    // ���ϱ�Ŀ����
                    if (DomUtil.HasAttr(nodeNewDatabase, "unionCatalogStyle") == true)
                    {
                        string strOldUnionCatalogStyle = DomUtil.GetAttr(nodeDatabase,
                            "unionCatalogStyle");

                        string strNewUnionCatalogStyle = DomUtil.GetAttr(nodeNewDatabase,
                            "unionCatalogStyle");

                        if (strOldUnionCatalogStyle != strNewUnionCatalogStyle)
                        {
                            DomUtil.SetAttr(nodeDatabase, "unionCatalogStyle",
                                strNewUnionCatalogStyle);
                            this.Changed = true;
                        }
                    }

                    // ����
                    if (DomUtil.HasAttr(nodeNewDatabase, "replication") == true)
                    {
                        string strOldReplication = DomUtil.GetAttr(nodeDatabase,
                            "replication");

                        string strNewReplication = DomUtil.GetAttr(nodeNewDatabase,
                            "replication");

                        if (strOldReplication != strNewReplication)
                        {
                            DomUtil.SetAttr(nodeDatabase, "replication",
                                strNewReplication);
                            this.Changed = true;
                        }
                    }

                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    continue;
                } // end of if ��Ŀ����


                // �����޸�ʵ�����
                // ���޸��Ƿ������ͨ
                if (this.IsItemDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ��ʵ���(name����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������޸�ʵ��ⶨ��";
                        return -1;
                    }

                    // ʵ�����
                    string strOldEntityDbName = DomUtil.GetAttr(nodeDatabase,
                        "name");

                    // ����strDatabaseInfo
                    string strNewEntityDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldEntityDbName != strNewEntityDbName)
                    {
                        if (String.IsNullOrEmpty(strOldEntityDbName) == true
                            && String.IsNullOrEmpty(strNewEntityDbName) == false)
                        {
                            strError = "Ҫ����ʵ��� '" + strNewEntityDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldEntityDbName) == false
                            && String.IsNullOrEmpty(strNewEntityDbName) == true)
                        {
                            strError = "Ҫɾ��ʵ��� '" + strNewEntityDbName + "'����ʹ��delete���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldEntityDbName,
                            strNewEntityDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "name", strNewEntityDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        bDbNameChanged = true;

                        DomUtil.SetAttr(nodeDatabase, "name", strNewEntityDbName);
                        this.Changed = true;
#endif
                    }

                    // �Ƿ������ͨ
                    {
                        string strOldInCirculation = DomUtil.GetAttr(nodeDatabase,
                            "inCirculation");
                        if (String.IsNullOrEmpty(strOldInCirculation) == true)
                            strOldInCirculation = "true";

                        string strNewInCirculation = DomUtil.GetAttr(nodeNewDatabase,
                            "inCirculation");
                        if (String.IsNullOrEmpty(strNewInCirculation) == true)
                            strNewInCirculation = "true";

                        if (strOldInCirculation != strNewInCirculation)
                        {
                            DomUtil.SetAttr(nodeDatabase, "inCirculation",
                                strNewInCirculation);
                            this.Changed = true;
                        }

                    }

                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    continue;
                }

                // �����޸Ķ�������
                if (this.IsOrderDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@orderDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' �Ķ�����(orderDbName����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������޸Ķ����ⶨ��";
                        return -1;
                    }

                    // ����LibraryCfgDom
                    string strOldOrderDbName = DomUtil.GetAttr(nodeDatabase,
                        "orderDbName");

                    // ����strDatabaseInfo
                    string strNewOrderDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldOrderDbName != strNewOrderDbName)
                    {
                        if (String.IsNullOrEmpty(strOldOrderDbName) == true
                            && String.IsNullOrEmpty(strNewOrderDbName) == false)
                        {
                            strError = "Ҫ���������� '" + strNewOrderDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldOrderDbName) == false
                            && String.IsNullOrEmpty(strNewOrderDbName) == true)
                        {
                            strError = "Ҫɾ�������� '" + strNewOrderDbName + "'����ʹ��delete���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldOrderDbName,
                            strNewOrderDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "orderDbName", strNewOrderDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        bDbNameChanged = true;

                        DomUtil.SetAttr(nodeDatabase, "orderDbName", strNewOrderDbName);
                        this.Changed = true;
#endif
                    }

                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    continue;
                }

                // �����޸��ڿ���
                if (this.IsIssueDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@issueDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ���ڿ�(issueDbName����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������޸��ڿⶨ��";
                        return -1;
                    }

                    // ����LibraryCfgDom
                    string strOldIssueDbName = DomUtil.GetAttr(nodeDatabase,
                        "issueDbName");
                    // ����strDatabaseInfo
                    string strNewIssueDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");    // 2012/4/30 changed

                    if (strOldIssueDbName != strNewIssueDbName)
                    {
                        if (String.IsNullOrEmpty(strOldIssueDbName) == true
                            && String.IsNullOrEmpty(strNewIssueDbName) == false)
                        {
                            strError = "Ҫ�����ڿ� '" + strNewIssueDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldIssueDbName) == false
                            && String.IsNullOrEmpty(strNewIssueDbName) == true)
                        {
                            strError = "Ҫɾ���ڿ� '" + strNewIssueDbName + "'����ʹ��delete���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldIssueDbName,
                            strNewIssueDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "issueDbName", strNewIssueDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        bDbNameChanged = true;

                        DomUtil.SetAttr(nodeDatabase, "issueDbName", strNewIssueDbName);
                        this.Changed = true;
#endif
                    }

                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    continue;
                }

                // �����޸���ע����
                if (this.IsCommentDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@commentDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ����ע��(commentDbName����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������޸���ע�ⶨ��";
                        return -1;
                    }

                    // ����LibraryCfgDom
                    string strOldCommentDbName = DomUtil.GetAttr(nodeDatabase,
                        "commentDbName");
                    // ����strDatabaseInfo
                    string strNewCommentDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");    // 2012/4/30 changed

                    if (strOldCommentDbName != strNewCommentDbName)
                    {
                        if (String.IsNullOrEmpty(strOldCommentDbName) == true
                            && String.IsNullOrEmpty(strNewCommentDbName) == false)
                        {
                            strError = "Ҫ������ע�� '" + strNewCommentDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldCommentDbName) == false
                            && String.IsNullOrEmpty(strNewCommentDbName) == true)
                        {
                            strError = "Ҫɾ����ע�� '" + strNewCommentDbName + "'����ʹ��delete���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldCommentDbName,
                            strNewCommentDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "commentDbName", strNewCommentDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        bDbNameChanged = true;

                        DomUtil.SetAttr(nodeDatabase, "commentDbName", strNewCommentDbName);
                        this.Changed = true;
#endif
                    }

                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    continue;
                }

                // �޸Ķ��߿���
                if (this.IsReaderDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readerdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' �Ķ��߿�(name����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    // 2012/9/9
                    // �ֹ��û�ֻ�����޸����ڹ�Ͻ�ֹݵĶ��߿�
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        string strExistLibraryCode = DomUtil.GetAttr(nodeDatabase, "libraryCode");

                        if (string.IsNullOrEmpty(strExistLibraryCode) == true
                            || StringUtil.IsInList(strExistLibraryCode, strLibraryCodeList) == false)
                        {
                            strError = "�޸Ķ��߿� '" + strName + "' ���屻�ܾ�����ǰ�û�ֻ���޸�ͼ��ݴ�����ȫ���� '" + strLibraryCodeList + "' ��Χ�Ķ��߿�";
                            return -1;
                        }
                    }

                    // ����LibraryCfgDom
                    string strOldReaderDbName = DomUtil.GetAttr(nodeDatabase,
                        "name");
                    // ����strDatabaseInfo
                    string strNewReaderDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldReaderDbName != strNewReaderDbName)
                    {
                        if (String.IsNullOrEmpty(strOldReaderDbName) == true
                            && String.IsNullOrEmpty(strNewReaderDbName) == false)
                        {
                            strError = "Ҫ�������߿� '" + strNewReaderDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldReaderDbName) == false
                            && String.IsNullOrEmpty(strNewReaderDbName) == true)
                        {
                            strError = "Ҫɾ�����߿� '" + strNewReaderDbName + "'����ʹ��delete���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldReaderDbName,
                            strNewReaderDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "name", strNewReaderDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        bDbNameChanged = true;

                        DomUtil.SetAttr(nodeDatabase, "name", strNewReaderDbName);
                        this.Changed = true;
#endif
                    }

                    // �Ƿ������ͨ
                    // ֻ�е��ύ�� XML Ƭ���о��� inCirculation ���Ե�ʱ�򣬲Żᷢ���޸�
                    if (nodeNewDatabase.GetAttributeNode("inCirculation") != null)
                    {
                        // ����LibraryCfgDom
                        string strOldInCirculation = DomUtil.GetAttr(nodeDatabase,
                            "inCirculation");
                        if (String.IsNullOrEmpty(strOldInCirculation) == true)
                            strOldInCirculation = "true";

                        // ����strDatabaseInfo
                        string strNewInCirculation = DomUtil.GetAttr(nodeNewDatabase,
                            "inCirculation");
                        if (String.IsNullOrEmpty(strNewInCirculation) == true)
                            strNewInCirculation = "true";

                        if (strOldInCirculation != strNewInCirculation)
                        {
                            DomUtil.SetAttr(nodeDatabase,
                                "inCirculation",
                                strNewInCirculation);
                            this.Changed = true;
                        }

                    }

                    // 2012/9/7
                    // ͼ��ݴ���
                    // ֻ�е��ύ�� XML Ƭ���о��� libraryCode ���Ե�ʱ�򣬲Żᷢ���޸�
                    if (nodeNewDatabase.GetAttributeNode("libraryCode") != null)
                    {
                        // ����LibraryCfgDom
                        string strOldLibraryCode = DomUtil.GetAttr(nodeDatabase,
                            "libraryCode");
                        // ����strDatabaseInfo
                        string strNewLibraryCode = DomUtil.GetAttr(nodeNewDatabase,
                            "libraryCode");

                        if (strOldLibraryCode != strNewLibraryCode)
                        {
                            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                            {
                                if (string.IsNullOrEmpty(strNewLibraryCode) == true
                                    || StringUtil.IsInList(strNewLibraryCode, strLibraryCodeList) == false)
                                {
                                    strError = "�޸Ķ��߿� '" + strName + "' ���屻�ܾ����޸ĺ����ͼ��ݴ��������ȫ���� '" + strLibraryCodeList + "' ��Χ";
                                    return -1;
                                }
                            }

                            // ���һ��������ͼ��ݴ����Ƿ��ʽ��ȷ
                            // Ҫ����Ϊ '*'�����ܰ�������
                            // return:
                            //      -1  У�麯�����������
                            //      0   У����ȷ
                            //      1   У�鷢�����⡣strError��������
                            nRet = VerifySingleLibraryCode(strNewLibraryCode,
                out strError);
                            if (nRet != 0)
                            {
                                strError = "ͼ��ݴ��� '" + strNewLibraryCode + "' ��ʽ����: " + strError;
                                goto ERROR1;
                            }

                            DomUtil.SetAttr(nodeDatabase,
                                "libraryCode",
                                strNewLibraryCode);
                            this.Changed = true;
                        }
                    }

                    // <readerdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    this.LoadReaderDbGroupParam(this.LibraryCfgDom);

                    this.Changed = true;
                    continue;
                }

                // �޸�ԤԼ�������
                if (this.ArrivedDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������޸�ԤԼ����ⶨ��";
                        return -1;
                    }

                    string strOldArrivedDbName = this.ArrivedDbName;

                    // ����strDatabaseInfo
                    string strNewArrivedDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldArrivedDbName != strNewArrivedDbName)
                    {
                        if (String.IsNullOrEmpty(strOldArrivedDbName) == true
                            && String.IsNullOrEmpty(strNewArrivedDbName) == false)
                        {
                            strError = "Ҫ����ԤԼ����� '" + strNewArrivedDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldArrivedDbName) == false
                            && String.IsNullOrEmpty(strNewArrivedDbName) == true)
                        {
                            strError = "Ҫɾ��ԤԼ����� '" + strNewArrivedDbName + "'����ʹ��delete���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldArrivedDbName,
                            strNewArrivedDbName,
                                () =>
                                {
                                    this.ArrivedDbName = strNewArrivedDbName;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        this.Changed = true;
                        this.ArrivedDbName = strNewArrivedDbName;
#endif
                    }

                    continue;
                }

                // �޸�ΥԼ�����
                if (this.AmerceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������޸�ΥԼ��ⶨ��";
                        return -1;
                    }

                    string strOldAmerceDbName = this.AmerceDbName;

                    // ����strDatabaseInfo
                    string strNewAmerceDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldAmerceDbName != strNewAmerceDbName)
                    {
                        if (String.IsNullOrEmpty(strOldAmerceDbName) == true
                            && String.IsNullOrEmpty(strNewAmerceDbName) == false)
                        {
                            strError = "Ҫ����ΥԼ��� '" + strNewAmerceDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldAmerceDbName) == false
                            && String.IsNullOrEmpty(strNewAmerceDbName) == true)
                        {
                            strError = "Ҫɾ��ΥԼ��� '" + strNewAmerceDbName + "'����ʹ��delete���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldAmerceDbName,
                            strNewAmerceDbName,
                                () =>
                                {
                                    this.AmerceDbName = strNewAmerceDbName;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        this.Changed = true;
                        this.AmerceDbName = strNewAmerceDbName;
#endif
                    }

                    continue;
                }

                // �޸ķ�Ʊ����
                if (this.InvoiceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������޸ķ�Ʊ�ⶨ��";
                        return -1;
                    }

                    string strOldInvoiceDbName = this.InvoiceDbName;

                    // ����strDatabaseInfo
                    string strNewInvoiceDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldInvoiceDbName != strNewInvoiceDbName)
                    {
                        if (String.IsNullOrEmpty(strOldInvoiceDbName) == true
                            && String.IsNullOrEmpty(strNewInvoiceDbName) == false)
                        {
                            strError = "Ҫ������Ʊ�� '" + strNewInvoiceDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldInvoiceDbName) == false
                            && String.IsNullOrEmpty(strNewInvoiceDbName) == true)
                        {
                            strError = "Ҫɾ����Ʊ�� '" + strNewInvoiceDbName + "'����ʹ��delete���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldInvoiceDbName,
                            strNewInvoiceDbName,
                                () =>
                                {
                                    this.InvoiceDbName = strNewInvoiceDbName;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        this.Changed = true;
                        this.InvoiceDbName = strNewInvoiceDbName;
#endif
                    }

                    continue;
                }


                // �޸���Ϣ����
                if (this.MessageDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������޸�ԤԼ��Ϣ�ⶨ��";
                        return -1;
                    }

                    string strOldMessageDbName = this.MessageDbName;

                    // ����strDatabaseInfo
                    string strNewMessageDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldMessageDbName != strNewMessageDbName)
                    {
                        if (String.IsNullOrEmpty(strOldMessageDbName) == true
                            && String.IsNullOrEmpty(strNewMessageDbName) == false)
                        {
                            strError = "Ҫ������Ϣ�� '" + strNewMessageDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldMessageDbName) == false
                            && String.IsNullOrEmpty(strNewMessageDbName) == true)
                        {
                            strError = "Ҫɾ����Ϣ�� '" + strNewMessageDbName + "'����ʹ��delete���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldMessageDbName,
                            strNewMessageDbName,
                                () =>
                                {
                                    this.MessageDbName = strNewMessageDbName;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        this.Changed = true;
                        this.MessageDbName = strNewMessageDbName;
#endif
                    }

                    continue;
                }

                // �޸�ʵ�ÿ���
                if (IsUtilDbName(strName) == true)
                {
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "������name����ֵΪ '"+strName+"' ��<utilDb/database>��Ԫ��";
                        goto ERROR1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������޸�ʵ�ÿⶨ��";
                        return -1;
                    }

                    string strOldUtilDbName = strName;

                    // ����strDatabaseInfo
                    string strNewUtilDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldUtilDbName != strNewUtilDbName)
                    {
                        if (String.IsNullOrEmpty(strOldUtilDbName) == true
                            && String.IsNullOrEmpty(strNewUtilDbName) == false)
                        {
                            strError = "Ҫ����ʵ�ÿ� '" + strNewUtilDbName + "'����ʹ��create���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldUtilDbName) == false
                            && String.IsNullOrEmpty(strNewUtilDbName) == true)
                        {
                            strError = "Ҫɾ��ʵ�ÿ� '" + strNewUtilDbName + "'����ʹ��delete���ܣ�������ʹ��change����";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldUtilDbName,
                            strNewUtilDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "name", strNewUtilDbName);
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        this.Changed = true;
                        DomUtil.SetAttr(nodeDatabase, "name", strNewUtilDbName);
#endif
                    }

                    string strOldType = DomUtil.GetAttr(nodeDatabase, "type");
                    string strNewType = DomUtil.GetAttr(nodeNewDatabase, "type");

                    if (strOldType != strNewType)
                    {
                        DomUtil.SetAttr(nodeDatabase, "type", strNewType);
                        this.Changed = true;
                        // TODO: �����޸ĺ��Ƿ�ҪӦ���µ�ģ�����޸����ݿⶨ�壿���Ǹ�������
                    }

                    continue;
                }

                strError = "���ݿ��� '" + strName + "' ������ dp2library Ŀǰ��Ͻ�ķ�Χ...";
                return -1;
            }

            if (this.Changed == true)
                this.ActivateManagerThread();

            if (bDbNameChanged == true)
            {
                nRet = InitialKdbs(
                    Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
                // ���³�ʼ������ⶨ��
                this.vdbs = null;
                nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return 0;
        ERROR1:
            // 2015/1/29
            if (this.Changed == true)
                this.ActivateManagerThread();

            // 2015/1/29
            if (bDbNameChanged == true)
            {
                {
                    string strError1 = "";
                    nRet = InitialKdbs(
                        Channels,
                        out strError1);
                    if (nRet == -1)
                        strError += "; ����β��ʱ����� InitialKdbs() �����ֳ���" + strError1;
                }

                {
                    string strError1 = "";
                    // ���³�ʼ������ⶨ��
                    this.vdbs = null;
                    nRet = this.InitialVdbs(Channels,
                        out strError1);
                    if (nRet == -1)
                        strError += "; ����β��ʱ����� InitialVdbs() �����ֳ���" + strError1;
                }
            }
            return -1;
        }

        // ���һ��������ͼ��ݴ����Ƿ��ʽ��ȷ
        // Ҫ����Ϊ '*'�����ܰ�������
        // return:
        //      -1  У�麯�����������
        //      0   У����ȷ
        //      1   У�鷢�����⡣strError��������
        public static int VerifySingleLibraryCode(string strText,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(strText) == true)
                return 0;

            strText = strText.Trim();
            if (strText == "*")
            {
                strError = "����ͼ��ݴ��벻����ʹ��ͨ���";
                return 1;
            }

            if (strText.IndexOf(",") != -1)
            {
                strError = "����ͼ��ݴ����в�������ֶ���";
                return 1;
            }

            return 0;
        }

        // ɾ��һ�����ݿ���OPAC�ɼ������еĶ���
        // return:
        //      -1  error
        //      0   not change
        //      1   changed
        int RemoveOpacDatabaseDef(
            RmsChannelCollection Channels,
            string strDatabaseName,
            out string strError)
        {
            strError = "";

            bool bChanged = false;

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("virtualDatabases");
            if (root != null)
            {


                // �����Ա�ⶨ��
                XmlNodeList nodes = root.SelectNodes("virtualDatabase/database[@name='" + strDatabaseName + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    node.ParentNode.RemoveChild(node);
                    bChanged = true;
                }

                // ��ͨ��Ա�ⶨ��
                nodes = root.SelectNodes("database[@name='" + strDatabaseName + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    node.ParentNode.RemoveChild(node);
                    bChanged = true;
                }

                // ���³�ʼ������ⶨ��
                this.vdbs = null;
                int nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;

            }

            root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("browseformats");
            if (root != null)
            {
                // ��ʾ�����еĿ���
                XmlNodeList nodes = root.SelectNodes("database[@name='" + strDatabaseName + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    node.ParentNode.RemoveChild(node);
                    bChanged = true;
                }
            }

            if (bChanged == true)
                return 1;

            return 0;
        }

        // �޸�һ�����ݿ���OPAC�ɼ������еĶ��������
        // return:
        //      -1  error
        //      0   not change
        //      1   changed
        int RenameOpacDatabaseDef(
            RmsChannelCollection Channels,
            string strOldDatabaseName,
            string strNewDatabaseName,
            out string strError)
        {
            strError = "";

            bool bChanged = false;

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("virtualDatabases");
            if (root != null)
            {
                // TODO: �Ƿ���Ҫ���һ���������Ƿ��Ѿ�������?

                // �����Ա�ⶨ��
                XmlNodeList nodes = root.SelectNodes("virtualDatabase/database[@name='" + strOldDatabaseName + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    DomUtil.SetAttr(node, "name", strNewDatabaseName);
                    bChanged = true;
                }

                // ��ͨ��Ա�ⶨ��
                nodes = root.SelectNodes("database[@name='" + strOldDatabaseName + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    DomUtil.SetAttr(node, "name", strNewDatabaseName);
                    bChanged = true;
                }

                // ���³�ʼ������ⶨ��
                this.vdbs = null;   // ǿ�Ƴ�ʼ��
                int nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;

            }

            root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("browseformats");
            if (root != null)
            {
                // ��ʾ�����еĿ���
                XmlNodeList nodes = root.SelectNodes("database[@name='" + strOldDatabaseName + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    DomUtil.SetAttr(node, "name", strNewDatabaseName);
                    bChanged = true;
                }
            }

            if (bChanged == true)
                return 1;

            return 0;
        }

        // ɾ�����ݿ�
        int DeleteDatabase(
            RmsChannelCollection Channels,
            string strLibraryCodeList,
            string strDatabaseNames,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            int nRet = 0;
            // long lRet = 0;

            bool bDbNameChanged = false;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);

            string[] names = strDatabaseNames.Split(new char[] { ',' });
            for (int i = 0; i < names.Length; i++)
            {
                string strName = names[i].Trim();
                if (String.IsNullOrEmpty(strName) == true)
                    continue;

                // ��Ŀ������ɾ����Ҳ�ǿ��Ե�
                // TODO: �������Կ��ǵ���ɾ����Ŀ�����ɾ��������ؿ�
                if (this.IsBiblioDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='"+strName+"']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ����Ŀ��(biblioDbName����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ɾ����Ŀ��";
                        return -1;
                    }

                    // ɾ����Ŀ��
                    /*
                    lRet = channel.DoDeleteDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "ɾ����Ŀ�� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }
                     * */
                    nRet = DeleteDatabase(channel, strName, out strError);
                    if (nRet == -1)
                    {
                        strError = "ɾ����Ŀ�� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    // ɾ��ʵ���
                    string strEntityDbName = DomUtil.GetAttr(nodeDatabase, "name");
                    if (String.IsNullOrEmpty(strEntityDbName) == false)
                    {
                        /*
                        lRet = channel.DoDeleteDB(strEntityDbName, out strError);
                        if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                        {
                            strError = "ɾ����Ŀ�� '" + strName + "' ��������ʵ��� '" + strEntityDbName + "' ʱ��������: " + strError;
                            return -1;
                        }
                         * */
                        nRet = DeleteDatabase(channel, strEntityDbName, out strError);
                        if (nRet == -1)
                        {
                            strError = "ɾ����Ŀ�� '" + strName + "' ��������ʵ��� '" + strEntityDbName + "' ʱ��������: " + strError;
                            return -1;
                        }
                    }

                    // ɾ��������
                    string strOrderDbName = DomUtil.GetAttr(nodeDatabase, "orderDbName");
                    if (String.IsNullOrEmpty(strOrderDbName) == false)
                    {
                        /*
                        lRet = channel.DoDeleteDB(strOrderDbName, out strError);
                        if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                        {
                            strError = "ɾ����Ŀ�� '" + strName + "' �������Ķ����� '" + strOrderDbName + "' ʱ��������: " + strError;
                            return -1;
                        }
                         * */
                        nRet = DeleteDatabase(channel, strOrderDbName, out strError);
                        if (nRet == -1)
                        {
                            strError = "ɾ����Ŀ�� '" + strName + "' �������Ķ����� '" + strOrderDbName + "' ʱ��������: " + strError;
                            return -1;
                        }

                    }

                    // ɾ���ڿ�
                    string strIssueDbName = DomUtil.GetAttr(nodeDatabase, "issueDbName");
                    if (String.IsNullOrEmpty(strIssueDbName) == false)
                    {
                        /*
                        lRet = channel.DoDeleteDB(strIssueDbName, out strError);
                        if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                        {
                            strError = "ɾ����Ŀ�� '" + strName + "' ���������ڿ� '" + strIssueDbName + "' ʱ��������: " + strError;
                            return -1;
                        }
                         * */
                        nRet = DeleteDatabase(channel, strIssueDbName, out strError);
                        if (nRet == -1)
                        {
                            strError = "ɾ����Ŀ�� '" + strName + "' ���������ڿ� '" + strIssueDbName + "' ʱ��������: " + strError;
                            return -1;
                        }
                    }

                    // ɾ����ע��
                    string strCommentDbName = DomUtil.GetAttr(nodeDatabase, "commentDbName");
                    if (String.IsNullOrEmpty(strCommentDbName) == false)
                    {
                        nRet = DeleteDatabase(channel, strCommentDbName, out strError);
                        if (nRet == -1)
                        {
                            strError = "ɾ����Ŀ�� '" + strName + "' ����������ע�� '" + strCommentDbName + "' ʱ��������: " + strError;
                            return -1;
                        }
                    }

                    nodeDatabase.ParentNode.RemoveChild(nodeDatabase);

                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    /*
                    // ɾ��һ�����ݿ���OPAC�ɼ������еĶ���
                    // return:
                    //      -1  error
                    //      0   not change
                    //      1   changed
                    nRet = RemoveOpacDatabaseDef(
                        Channels,
                        strName,
                        out strError);
                    if (nRet == -1)
                    {
                        this.Changed = true;
                        this.ActivateMangerThread();
                        return -1;
                    }*/

                    this.Changed = true;
                    this.ActivateManagerThread();

                    continue;
                }

                // ����ɾ��ʵ���
                if (this.IsItemDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ��ʵ���(name����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ɾ��ʵ���";
                        return -1;
                    }

                    // ɾ��ʵ���
                    /*
                    lRet = channel.DoDeleteDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "ɾ��ʵ��� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }
                     * */
                    nRet = DeleteDatabase(channel, strName, out strError);
                    if (nRet == -1)
                    {
                        strError = "ɾ��ʵ��� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "name", null);

                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    this.ActivateManagerThread();
                    continue;
                }

                // ����ɾ��������
                if (this.IsOrderDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@orderDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' �Ķ�����(orderDbName����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ɾ��������";
                        return -1;
                    }

                    // ɾ��������
                    /*
                    lRet = channel.DoDeleteDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "ɾ�������� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }
                     * */
                    nRet = DeleteDatabase(channel, strName, out strError);
                    if (nRet == -1)
                    {
                        strError = "ɾ�������� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }


                    bDbNameChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "orderDbName", null);

                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    this.ActivateManagerThread();
                    continue;
                }

                // ����ɾ���ڿ�
                if (this.IsIssueDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@issueDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ���ڿ�(issueDbName����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ɾ���ڿ�";
                        return -1;
                    }

                    // ɾ���ڿ�
                    /*
                    lRet = channel.DoDeleteDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "ɾ���ڿ� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }
                     * */
                    nRet = DeleteDatabase(channel, strName, out strError);
                    if (nRet == -1)
                    {
                        strError = "ɾ���ڿ� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "issueDbName", null);

                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    this.ActivateManagerThread();
                    continue;
                }

                // ����ɾ����ע��
                if (this.IsCommentDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@commentDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ����ע��(commentDbName����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ɾ����ע��";
                        return -1;
                    }

                    nRet = DeleteDatabase(channel, strName, out strError);
                    if (nRet == -1)
                    {
                        strError = "ɾ����ע�� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "commentDbName", null);

                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    this.ActivateManagerThread();
                    continue;
                }

                // ɾ�����߿�
                if (this.IsReaderDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readerdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' �Ķ��߿�(name����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }


                    // 2012/9/9
                    // �ֹ��û�ֻ����ɾ�����ڹ�Ͻ�ֹݵĶ��߿�
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        string strExistLibraryCode = DomUtil.GetAttr(nodeDatabase, "libraryCode");

                        if (string.IsNullOrEmpty(strExistLibraryCode) == true
                            || StringUtil.IsInList(strExistLibraryCode, strLibraryCodeList) == false)
                        {
                            strError = "ɾ�����߿� '" + strName + "' ���ܾ�����ǰ�û�ֻ��ɾ��ͼ��ݴ�����ȫ��ȫ���� '" + strLibraryCodeList + "' ��Χ�Ķ��߿�";
                            return -1;
                        }
                    }

                    // ɾ�����߿�
                    /*
                    lRet = channel.DoDeleteDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "ɾ�����߿� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }
                     * */
                    nRet = DeleteDatabase(channel, strName, out strError);
                    if (nRet == -1)
                    {
                        strError = "ɾ�����߿� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    nodeDatabase.ParentNode.RemoveChild(nodeDatabase);

                    // <readerdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    this.LoadReaderDbGroupParam(this.LibraryCfgDom);

                    /*
                    // ɾ��һ�����ݿ���OPAC�ɼ������еĶ���
                    // return:
                    //      -1  error
                    //      0   not change
                    //      1   changed
                    nRet = RemoveOpacDatabaseDef(
                        Channels,
                        strName,
                        out strError);
                    if (nRet == -1)
                    {
                        this.Changed = true;
                        this.ActivateMangerThread();
                        return -1;
                    }
                     * */

                    this.Changed = true;
                    this.ActivateManagerThread();
                    continue;
                }

                // ɾ��ԤԼ�����
                if (this.ArrivedDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ɾ��ԤԼ�����";
                        return -1;
                    }

                    // ɾ��ԤԼ�����
                    /*
                    lRet = channel.DoDeleteDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "ɾ��ԤԼ����� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }
                     * */
                    nRet = DeleteDatabase(channel, strName, out strError);
                    if (nRet == -1)
                    {
                        strError = "ɾ��ԤԼ����� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    this.ArrivedDbName = "";

                    /*
                    // ɾ��һ�����ݿ���OPAC�ɼ������еĶ���
                    // return:
                    //      -1  error
                    //      0   not change
                    //      1   changed
                    nRet = RemoveOpacDatabaseDef(
                        Channels,
                        strName,
                        out strError);
                    if (nRet == -1)
                    {
                        this.Changed = true;
                        this.ActivateMangerThread();
                        return -1;
                    }
                     * */

                    this.Changed = true;
                    this.ActivateManagerThread();
                    continue;
                }

                // ɾ��ΥԼ���
                if (this.AmerceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ɾ��ΥԼ���";
                        return -1;
                    }

                    // ɾ��ΥԼ���
                    /*
                    lRet = channel.DoDeleteDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "ɾ��ΥԼ��� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }
                     * */
                    nRet = DeleteDatabase(channel, strName, out strError);
                    if (nRet == -1)
                    {
                        strError = "ɾ��ΥԼ��� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }


                    this.AmerceDbName = "";

                    /*
                    // ɾ��һ�����ݿ���OPAC�ɼ������еĶ���
                    // return:
                    //      -1  error
                    //      0   not change
                    //      1   changed
                    nRet = RemoveOpacDatabaseDef(
                        Channels,
                        strName,
                        out strError);
                    if (nRet == -1)
                    {
                        this.Changed = true;
                        this.ActivateMangerThread();
                        return -1;
                    }*/

                    this.Changed = true;
                    this.ActivateManagerThread();
                    continue;
                }

                // ɾ����Ʊ��
                if (this.InvoiceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ɾ����Ʊ��";
                        return -1;
                    }

                    nRet = DeleteDatabase(channel, strName, out strError);
                    if (nRet == -1)
                    {
                        strError = "ɾ����Ʊ�� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    this.InvoiceDbName = "";

                    this.Changed = true;
                    this.ActivateManagerThread();
                    continue;
                }


                // ɾ����Ϣ��
                if (this.MessageDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ɾ����Ϣ��";
                        return -1;
                    }

                    // ɾ����Ϣ��
                    /*
                    lRet = channel.DoDeleteDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "ɾ����Ϣ�� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }
                     * */
                    nRet = DeleteDatabase(channel, strName, out strError);
                    if (nRet == -1)
                    {
                        strError = "ɾ����Ϣ�� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    this.MessageDbName = "";

                    /*
                    // ɾ��һ�����ݿ���OPAC�ɼ������еĶ���
                    // return:
                    //      -1  error
                    //      0   not change
                    //      1   changed
                    nRet = RemoveOpacDatabaseDef(
                        Channels,
                        strName,
                        out strError);
                    if (nRet == -1)
                    {
                        this.Changed = true;
                        this.ActivateMangerThread();
                        return -1;
                    }
                     * */

                    this.Changed = true;
                    this.ActivateManagerThread();
                    continue;
                }

                // ����ɾ��ʵ�ÿ�
                if (IsUtilDbName(strName) == true)
                {
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "������name����ֵΪ '" + strName + "' ��<utilDb/database>��Ԫ��";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ɾ��ʵ�ÿ�";
                        return -1;
                    }

                    // ɾ��ʵ�ÿ�
                    /*
                    lRet = channel.DoDeleteDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "ɾ��ʵ�ÿ� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }
                     * */
                    nRet = DeleteDatabase(channel, strName, out strError);
                    if (nRet == -1)
                    {
                        strError = "ɾ��ʵ�ÿ� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    nodeDatabase.ParentNode.RemoveChild(nodeDatabase);

                    /*
                    // ɾ��һ�����ݿ���OPAC�ɼ������еĶ���
                    // return:
                    //      -1  error
                    //      0   not change
                    //      1   changed
                    nRet = RemoveOpacDatabaseDef(
                        Channels,
                        strName,
                        out strError);
                    if (nRet == -1)
                    {
                        this.Changed = true;
                        this.ActivateMangerThread();
                        return -1;
                    }
                     * */

                    this.Changed = true;
                    this.ActivateManagerThread();
                    continue;
                }

                strError = "���ݿ��� '" +strName+ "' ������ dp2library Ŀǰ��Ͻ�ķ�Χ...";
                return -1;
            }

            if (bDbNameChanged == true)
            {
                nRet = InitialKdbs(
                    Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
                // ���³�ʼ������ⶨ��
                this.vdbs = null;
                nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return 0;
        }


        // ˢ�����ݿⶨ��
        // parameters:
        //      strDatabaseInfo Ҫˢ�µ������ļ����ԡ�<refreshStyle include="keys,browse" exclude="">(��ʾֻˢ��keys��browse������Ҫ�����ļ�)����<refreshStyle include="*" exclude="template">(��ʾˢ��ȫ���ļ������ǲ�Ҫˢ��template) �������ֵΪ�գ���ʾȫ��ˢ��
        //      strOutputInfo   ����keys���巢���ı�����ݿ�����"<keysChanged dbpaths='http://localhost:8001/dp2kernel?dbname1;http://localhost:8001/dp2kernel?dbname2'/>"
        int RefreshDatabaseDefs(
            RmsChannelCollection Channels,
            string strLibraryCodeList,
            string strDatabaseNames,
            string strDatabaseInfo,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            int nRet = 0;
            // long lRet = 0;

            string strInclude = "";
            string strExclude = "";

            bool bAutoRebuildKeys = false;  // 2014/11/26

            if (String.IsNullOrEmpty(strDatabaseInfo) == false)
            {
                XmlDocument style_dom = new XmlDocument();
                try
                {
                    style_dom.LoadXml(strDatabaseInfo);
                }
                catch (Exception ex)
                {
                    strError = "����strDatabaseInfo��ֵװ��XMLDOMʱ����: " + ex.Message;
                    return -1;
                }
                XmlNode style_node = style_dom.DocumentElement.SelectSingleNode("//refreshStyle");
                if (style_node != null)
                {
                    strInclude = DomUtil.GetAttr(style_node, "include");
                    strExclude = DomUtil.GetAttr(style_node, "exclude");
                    bAutoRebuildKeys = DomUtil.GetBooleanParam(style_node, "autoRebuildKeys", false);
                }
            }

            if (String.IsNullOrEmpty(strInclude) == true)
                strInclude = "*";   // ��ʾȫ��

            // bool bKeysDefChanged = false;    // ˢ�º�keys���ÿ��ܱ��ı�
            List<string> keyschanged_dbnames = new List<string>();  // keys���巢���˸ı�����ݿ���

            RmsChannel channel = Channels.GetChannel(this.WsUrl);

            string[] names = strDatabaseNames.Split(new char[] { ',' });
            for (int i = 0; i < names.Length; i++)
            {
                string strName = names[i].Trim();
                if (String.IsNullOrEmpty(strName) == true)
                    continue;

                // ��Ŀ������ˢ�£�Ҳ�ǿ��Ե�
                if (this.IsBiblioDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ����Ŀ��(biblioDbName����)���<database>Ԫ��û���ҵ�";
                        goto ERROR1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ˢ����Ŀ��Ķ���";
                        goto ERROR1;
                    }

                    string strSyntax = DomUtil.GetAttr(nodeDatabase, "syntax");
                    if (String.IsNullOrEmpty(strSyntax) == true)
                        strSyntax = "unimarc";

                    string strUsage = "";
                    string strIssueDbName = DomUtil.GetAttr(nodeDatabase, "issueDbName");
                    if (String.IsNullOrEmpty(strIssueDbName) == true)
                        strUsage = "book";
                    else
                        strUsage = "series";

                    // ˢ����Ŀ��
                    string strTemplateDir = this.DataDir + "\\templates\\" + "biblio_" + strSyntax + "_" + strUsage;

                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ˢ��С��Ŀ�� '" + strName + "' ����ʱ��������: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    // ˢ��ʵ���
                    string strEntityDbName = DomUtil.GetAttr(nodeDatabase, "name");
                    if (String.IsNullOrEmpty(strEntityDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "item";

                        nRet = RefreshDatabase(channel,
                            strTemplateDir,
                            strEntityDbName,
                            strInclude,
                            strExclude,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "ˢ����Ŀ�� '" + strName + "' ��������ʵ��� '" + strEntityDbName + "' ����ʱ��������: " + strError;
                            goto ERROR1;
                        }
                        if (nRet == 1)
                            keyschanged_dbnames.Add(strEntityDbName);
                    }

                    // ˢ�¶�����
                    string strOrderDbName = DomUtil.GetAttr(nodeDatabase, "orderDbName");
                    if (String.IsNullOrEmpty(strOrderDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "order";

                        nRet = RefreshDatabase(channel,
                            strTemplateDir,
                            strOrderDbName,
                            strInclude,
                            strExclude,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "ˢ����Ŀ�� '" + strName + "' �������Ķ����� '" + strOrderDbName + "' ����ʱ��������: " + strError;
                            goto ERROR1;
                        }
                        if (nRet == 1)
                            keyschanged_dbnames.Add(strOrderDbName);
                    }

                    // ˢ���ڿ�
                    if (String.IsNullOrEmpty(strIssueDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "issue";

                        nRet = RefreshDatabase(channel,
                            strTemplateDir,
                            strIssueDbName,
                            strInclude,
                            strExclude,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "ˢ����Ŀ�� '" + strName + "' ���������ڿ� '" + strIssueDbName + "' ����ʱ��������: " + strError;
                            goto ERROR1;
                        }
                        if (nRet == 1)
                            keyschanged_dbnames.Add(strIssueDbName);
                    }

                    // ˢ����ע��
                    string strCommentDbName = DomUtil.GetAttr(nodeDatabase, "commentDbName");
                    if (String.IsNullOrEmpty(strCommentDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "comment";

                        nRet = RefreshDatabase(channel,
                            strTemplateDir,
                            strCommentDbName,
                            strInclude,
                            strExclude,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "ˢ����Ŀ�� '" + strName + "' ����������ע�� '" + strCommentDbName + "' ����ʱ��������: " + strError;
                            goto ERROR1;
                        }
                        if (nRet == 1)
                            keyschanged_dbnames.Add(strCommentDbName);
                    }

                    continue;
                }

                // ����ˢ��ʵ���
                if (this.IsItemDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ��ʵ���(name����)���<database>Ԫ��û���ҵ�";
                        goto ERROR1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ˢ��ʵ���Ķ���";
                        goto ERROR1;
                    }

                    // ˢ��ʵ���
                    string strTemplateDir = this.DataDir + "\\templates\\" + "item";

                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ˢ��ʵ��� '" + strName + "' ����ʱ��������: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    continue;
                }

                // ����ˢ�¶�����
                if (this.IsOrderDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@orderDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' �Ķ�����(orderDbName����)���<database>Ԫ��û���ҵ�";
                        goto ERROR1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ˢ�¶�����Ķ���";
                        goto ERROR1;
                    }

                    // ˢ�¶�����
                    string strTemplateDir = this.DataDir + "\\templates\\" + "order";
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ˢ�¶����� '" + strName + "' ����ʱ��������: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    continue;
                }

                // ����ˢ���ڿ�
                if (this.IsIssueDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@issueDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ���ڿ�(issueDbName����)���<database>Ԫ��û���ҵ�";
                        goto ERROR1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ˢ���ڿ�Ķ���";
                        goto ERROR1;
                    }

                    // ˢ���ڿ�
                    string strTemplateDir = this.DataDir + "\\templates\\" + "issue";

                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ˢ���ڿ� '" + strName + "' ����ʱ��������: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    continue;
                }

                // ����ˢ����ע��
                if (this.IsCommentDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@commentDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ����ע��(commentDbName����)���<database>Ԫ��û���ҵ�";
                        goto ERROR1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ˢ����ע��Ķ���";
                        goto ERROR1;
                    }

                    // ˢ����ע��
                    string strTemplateDir = this.DataDir + "\\templates\\" + "comment";

                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ˢ����ע�� '" + strName + "' ����ʱ��������: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    continue;
                }

                // ˢ�¶��߿�
                if (this.IsReaderDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readerdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' �Ķ��߿�(name����)���<database>Ԫ��û���ҵ�";
                        goto ERROR1;
                    }

                    // 2012/9/9
                    // �ֹ��û�ֻ����ˢ�����ڹ�Ͻ�ֹݵĶ��߿�
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        string strExistLibraryCode = DomUtil.GetAttr(nodeDatabase, "libraryCode");

                        if (string.IsNullOrEmpty(strExistLibraryCode) == true
                            || StringUtil.IsInList(strExistLibraryCode, strLibraryCodeList) == false)
                        {
                            strError = "ˢ�¶��߿� '" + strName + "' ���屻�ܾ�����ǰ�û�ֻ��ˢ��ͼ��ݴ�����ȫ��ȫ���� '" + strLibraryCodeList + "' ��Χ�Ķ��߿ⶨ��";
                            goto ERROR1;
                        }
                    }

                    // ˢ�¶��߿�
                    string strTemplateDir = this.DataDir + "\\templates\\" + "reader";

                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ˢ�¶��߿� '" + strName + "' ����ʱ��������: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    continue;
                }

                // ˢ��ԤԼ�����
                if (this.ArrivedDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ˢ��ԤԼ�����Ķ���";
                        goto ERROR1;
                    }

                    // ˢ��ԤԼ�����
                    string strTemplateDir = this.DataDir + "\\templates\\" + "arrived";
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ˢ��ԤԼ����� '" + strName + "' ����ʱ��������: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);
                    continue;
                }

                // ˢ��ΥԼ���
                if (this.AmerceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ˢ��ΥԼ���Ķ���";
                        goto ERROR1;
                    }

                    // ˢ��ΥԼ���
                    string strTemplateDir = this.DataDir + "\\templates\\" + "amerce";
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ˢ��ΥԼ��� '" + strName + "' ����ʱ��������: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    continue;
                }

                // ˢ�·�Ʊ��
                if (this.InvoiceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ˢ�·�Ʊ��Ķ���";
                        goto ERROR1;
                    }

                    // ˢ�·�Ʊ��
                    string strTemplateDir = this.DataDir + "\\templates\\" + "invoice";
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ˢ�·�Ʊ�� '" + strName + "' ����ʱ��������: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    continue;
                }

                // ˢ����Ϣ��
                if (this.MessageDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ˢ����Ϣ��Ķ���";
                        goto ERROR1;
                    }

                    // ˢ����Ϣ��
                    string strTemplateDir = this.DataDir + "\\templates\\" + "message";
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ˢ����Ϣ�� '" + strName + "' ����ʱ��������: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    continue;
                }

                // ˢ��ʵ�ÿ�
                if (IsUtilDbName(strName) == true)
                {
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "������name����ֵΪ '" + strName + "' ��<utilDb/database>��Ԫ��";
                        goto ERROR1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û���������ˢ��ʵ�ÿ�Ķ���";
                        goto ERROR1;
                    }

                    string strType = DomUtil.GetAttr(nodeDatabase, "type").ToLower();

                    // ˢ��ʵ�ÿ�
                    string strTemplateDir = this.DataDir + "\\templates\\" + strType;
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ˢ��ʵ�ÿ� '" + strName + "' ����ʱ��������: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);
                    continue;
                }

                strError = "���ݿ��� '" + strName + "' ������ dp2library Ŀǰ��Ͻ�ķ�Χ...";
                goto ERROR1;
            }

            // 2015/6/13
            if (keyschanged_dbnames.Count > 0)
            {
                nRet = InitialKdbs(
                    Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
                // ���³�ʼ������ⶨ��
                this.vdbs = null;
                nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            if (bAutoRebuildKeys == true
                && keyschanged_dbnames.Count > 0)
            {
                nRet = StartRebuildKeysTask(StringUtil.MakePathList(keyschanged_dbnames, ","),
            out strError);
                if (nRet == -1)
                    return -1;
            }

            {
                // ����WebServiceUrl����
                for (int i = 0; i < keyschanged_dbnames.Count; i++)
                {
                    keyschanged_dbnames[i] = this.WsUrl.ToLower() + "?" + keyschanged_dbnames[i];
                }

                XmlDocument dom = new XmlDocument();
                dom.LoadXml("<keysChanged />");
                DomUtil.SetAttr(dom.DocumentElement, "dbpaths", StringUtil.MakePathList(keyschanged_dbnames, ";"));
                strOutputInfo = dom.OuterXml;
            }

            return 0;
        ERROR1:
            if (keyschanged_dbnames.Count > 0)
            {
                // ����WebServiceUrl����
                for (int i = 0; i < keyschanged_dbnames.Count; i++)
                {
                    keyschanged_dbnames[i] = this.WsUrl.ToLower() + "?" + keyschanged_dbnames[i];
                }

                XmlDocument dom = new XmlDocument();
                dom.LoadXml("<keysChanged />");
                DomUtil.SetAttr(dom.DocumentElement, "dbpaths", StringUtil.MakePathList(keyschanged_dbnames, ";"));
                strOutputInfo = dom.OuterXml;
            }
            return -1;
        }




        // TODO: �����ǰ������������, ��Ҫ���µ�����׷�ӵ�ĩβ��������
        int StartRebuildKeysTask(string strDbNameList,
            out string strError)
        {
            strError = "";

            BatchTaskInfo info = null;

            // ����ԭʼ�洢��ʱ��Ϊ�˱����ڲ����ַ����з������������ݿ���֮���� | ���
            if (string.IsNullOrEmpty(strDbNameList) == false)
                strDbNameList = strDbNameList.Replace(",", "|");

            BatchTaskStartInfo start_info = new BatchTaskStartInfo();
            start_info.Start = "dbnamelist=" + strDbNameList;

            BatchTaskInfo param = new BatchTaskInfo();
            param.StartInfo = start_info;

            int nRet = StartBatchTask("�ؽ�������",
                param,
                out info,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        // ��ʼ�����ݿ�
        int InitializeDatabase(
            RmsChannelCollection Channels,
            string strLibraryCodeList,
            string strDatabaseNames,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            int nRet = 0;
            long lRet = 0;

            bool bDbNameChanged = false;    // ��ʼ���󣬼���;�����ȶ����ܱ��ı�

            RmsChannel channel = Channels.GetChannel(this.WsUrl);

            string[] names = strDatabaseNames.Split(new char[] { ',' });
            for (int i = 0; i < names.Length; i++)
            {
                string strName = names[i].Trim();
                if (String.IsNullOrEmpty(strName) == true)
                    continue;

                // ��Ŀ�������ʼ����Ҳ�ǿ��Ե�
                // TODO: �������Կ��ǵ�����ʼ����Ŀ�����ɾ��������ؿ�
                if (this.IsBiblioDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ����Ŀ��(biblioDbName����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������ʼ����Ŀ��";
                        return -1;
                    }

                    // ��ʼ����Ŀ��
                    lRet = channel.DoInitialDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "��ʼ��С��Ŀ�� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    // ��ʼ��ʵ���
                    string strEntityDbName = DomUtil.GetAttr(nodeDatabase, "name");
                    if (String.IsNullOrEmpty(strEntityDbName) == false)
                    {
                        lRet = channel.DoInitialDB(strEntityDbName, out strError);
                        if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                        {
                            strError = "��ʼ����Ŀ�� '" + strName + "' ��������ʵ��� '" + strEntityDbName + "' ʱ��������: " + strError;
                            return -1;
                        }
                    }

                    // ��ʼ��������
                    string strOrderDbName = DomUtil.GetAttr(nodeDatabase, "orderDbName");
                    if (String.IsNullOrEmpty(strOrderDbName) == false)
                    {
                        lRet = channel.DoInitialDB(strOrderDbName, out strError);
                        if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                        {
                            strError = "��ʼ����Ŀ�� '" + strName + "' �������Ķ����� '" + strOrderDbName + "' ʱ��������: " + strError;
                            return -1;
                        }
                    }

                    // ��ʼ���ڿ�
                    string strIssueDbName = DomUtil.GetAttr(nodeDatabase, "issueDbName");
                    if (String.IsNullOrEmpty(strIssueDbName) == false)
                    {
                        lRet = channel.DoInitialDB(strIssueDbName, out strError);
                        if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                        {
                            strError = "��ʼ����Ŀ�� '" + strName + "' ���������ڿ� '" + strIssueDbName + "' ʱ��������: " + strError;
                            return -1;
                        }
                    }

                    // ��ʼ����ע��
                    string strCommentDbName = DomUtil.GetAttr(nodeDatabase, "commentDbName");
                    if (String.IsNullOrEmpty(strCommentDbName) == false)
                    {
                        lRet = channel.DoInitialDB(strCommentDbName, out strError);
                        if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                        {
                            strError = "��ʼ����Ŀ�� '" + strName + "' ����������ע�� '" + strCommentDbName + "' ʱ��������: " + strError;
                            return -1;
                        }
                    }

                    continue;
                }

                // ������ʼ��ʵ���
                if (this.IsItemDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ��ʵ���(name����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������ʼ��ʵ���";
                        return -1;
                    }

                    // ��ʼ��ʵ���
                    lRet = channel.DoInitialDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "��ʼ��ʵ��� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    continue;
                }

                // ������ʼ��������
                if (this.IsOrderDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@orderDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' �Ķ�����(orderDbName����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������ʼ��������";
                        return -1;
                    }

                    // ��ʼ��������
                    lRet = channel.DoInitialDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "��ʼ�������� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    continue;
                }

                // ������ʼ���ڿ�
                if (this.IsIssueDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@issueDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ���ڿ�(issueDbName����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������ʼ���ڿ�";
                        return -1;
                    }

                    // ��ʼ���ڿ�
                    lRet = channel.DoInitialDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "��ʼ���ڿ� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    continue;
                }

                // ������ʼ����ע��
                if (this.IsCommentDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@commentDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' ����ע��(commentDbName����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������ʼ����ע��";
                        return -1;
                    }

                    // ��ʼ����ע��
                    lRet = channel.DoInitialDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "��ʼ����ע�� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;
                    continue;
                }

                // ��ʼ�����߿�
                if (this.IsReaderDbName(strName) == true)
                {
                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readerdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strName + "' �Ķ��߿�(name����)���<database>Ԫ��û���ҵ�";
                        return -1;
                    }

                    // 2012/9/9
                    // �ֹ��û�ֻ�����ʼ�����ڹ�Ͻ�ֹݵĶ��߿�
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        string strExistLibraryCode = DomUtil.GetAttr(nodeDatabase, "libraryCode");

                        if (string.IsNullOrEmpty(strExistLibraryCode) == true
                            || StringUtil.IsInList(strExistLibraryCode, strLibraryCodeList) == false)
                        {
                            strError = "��ʼ�����߿� '" + strName + "' ���ܾ�����ǰ�û�ֻ�ܳ�ʼ��ͼ��ݴ�����ȫ��ȫ���� '" + strLibraryCodeList + "' ��Χ�Ķ��߿�";
                            return -1;
                        }
                    }

                    // ��ʼ�����߿�
                    lRet = channel.DoInitialDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "��ʼ�����߿� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    continue;
                }

                // ��ʼ��ԤԼ�����
                if (this.ArrivedDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������ʼ��ԤԼ�����";
                        return -1;
                    }

                    // ��ʼ��ԤԼ�����
                    lRet = channel.DoInitialDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "��ʼ��ԤԼ����� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }
                    continue;
                }

                // ��ʼ��ΥԼ���
                if (this.AmerceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������ʼ��ΥԼ���";
                        return -1;
                    }

                    // ��ʼ��ΥԼ���
                    lRet = channel.DoInitialDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "��ʼ��ΥԼ��� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    continue;
                }

                // ��ʼ����Ʊ��
                if (this.InvoiceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������ʼ����Ʊ��";
                        return -1;
                    }

                    // ��ʼ����Ʊ��
                    lRet = channel.DoInitialDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "��ʼ����Ʊ�� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    continue;
                }

                // ��ʼ����Ϣ��
                if (this.MessageDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������ʼ����Ϣ��";
                        return -1;
                    }

                    // ��ʼ����Ϣ��
                    lRet = channel.DoInitialDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "��ʼ����Ϣ�� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }

                    continue;
                }

                // ��ʼ��ʵ�ÿ�
                if (IsUtilDbName(strName) == true)
                {
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "������name����ֵΪ '" + strName + "' ��<utilDb/database>��Ԫ��";
                        return -1;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������ʼ��ʵ�ÿ�";
                        return -1;
                    }

                    // ��ʼ��ʵ�ÿ�
                    lRet = channel.DoInitialDB(strName, out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "��ʼ��ʵ�ÿ� '" + strName + "' ʱ��������: " + strError;
                        return -1;
                    }
                    continue;
                }

                strError = "���ݿ��� '" + strName + "' ������ dp2library Ŀǰ��Ͻ�ķ�Χ...";
                return -1;
            }

            if (bDbNameChanged == true)
            {
                nRet = InitialKdbs(
                    Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
                /*
                // ���³�ʼ������ⶨ��
                this.vdbs = null;
                nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
                 * */
            }

            return 0;
        }

        // �������ݿ�
        // parameters:
        //      strLibraryCodeList  ��ǰ�û��Ĺ�Ͻ�ֹݴ����б�
        //      bRecreate   �Ƿ�Ϊ���´��������Ϊ���´������������Ѿ����ڶ��壻����������´��������״δ������������Ѿ����ڶ���
        //                  ע: ���´�������˼, �� library.xml ���ж��壬�� dp2kernel ��û�ж�Ӧ�����ݿ⣬Ҫ���ݶ������´�����Щ dp2kernel ���ݿ�
        int CreateDatabase(
            RmsChannelCollection Channels,
            string strLibraryCodeList,
            string strDatabaseInfo,
            bool bRecreate,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            int nRet = 0;

            List<string> created_dbnames = new List<string>();  // �����У��Ѿ����������ݿ���

            bool bDbChanged = false;    // ���ݿ����Ƿ������ı䣿�����´��������ݿ�? �������������Ҫ���³�ʼ��kdbs

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strDatabaseInfo);
            }
            catch (Exception ex)
            {
                strError = "strDatabaseInfo����װ��XMLDOMʱ����: " + ex.Message;
                return -1;
            }

            RmsChannel channel = Channels.GetChannel(this.WsUrl);

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strType = DomUtil.GetAttr(node, "type").ToLower();

                string strName = DomUtil.GetAttr(node, "name");

                // ������Ŀ���ݿ�
                if (strType == "biblio")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������������´�����Ŀ��";
                        return -1;
                    }

                    if (this.TestMode == true)
                    {
                        XmlNodeList existing_nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("itemdbgroup/database");
                        if (existing_nodes.Count >= 4)
                        {
                            strError = "dp2Library XE ����ģʽ��ֻ�ܴ������ 4 ����Ŀ��";
                            goto ERROR1;
                        }
                    }

                    // 2009/11/13
                    XmlNode exist_database_node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='"+strName+"']");
                    if (bRecreate == true && exist_database_node == null)
                    {
                        strError = "library.xml�в���������Ŀ�� '"+strName+"' �Ķ��壬�޷��������´���";
                        goto ERROR1;
                    }

                    string strSyntax = DomUtil.GetAttr(node, "syntax");
                    if (String.IsNullOrEmpty(strSyntax) == true)
                        strSyntax = "unimarc";

                    // usage: book series
                    string strUsage = DomUtil.GetAttr(node, "usage");
                    if (String.IsNullOrEmpty(strUsage) == true)
                        strUsage = "book";

                    // 2009/10/23
                    string strRole = DomUtil.GetAttr(node, "role");

                    if (bRecreate == false)
                    {
                        // ���cfgdom���Ƿ��Ѿ�����ͬ������Ŀ��
                        if (this.IsBiblioDbName(strName) == true)
                        {
                            strError = "��Ŀ�� '" + strName + "' �Ķ����Ѿ����ڣ������ظ�����";
                            goto ERROR1;
                        }
                    }

                    // ���dp2kernel���Ƿ��к���Ŀ��ͬ�������ݿ����
                    {
                        // ���ݿ��Ƿ��Ѿ����ڣ�
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   �������͵�ͬ�������Ѿ�����
                        nRet = IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    string strEntityDbName = DomUtil.GetAttr(node, "entityDbName");

                    string strOrderDbName = DomUtil.GetAttr(node, "orderDbName");

                    string strIssueDbName = DomUtil.GetAttr(node, "issueDbName");

                    string strCommentDbName = DomUtil.GetAttr(node, "commentDbName");

                    if (strEntityDbName == "<default>")
                        strEntityDbName = strName + "ʵ��";

                    if (strOrderDbName == "<default>")
                        strOrderDbName = strName + "����";

                    if (strIssueDbName == "<default>")
                        strIssueDbName = strName + "��";

                    if (strCommentDbName == "<default>")
                        strCommentDbName = strName + "��ע";

                    string strInCirculation = DomUtil.GetAttr(node, "inCirculation");
                    if (String.IsNullOrEmpty(strInCirculation) == true)
                        strInCirculation = "true";  // ȱʡΪtrue

                    string strUnionCatalogStyle = DomUtil.GetAttr(node, "unionCatalogStyle");

                    string strReplication = DomUtil.GetAttr(node, "replication");

                    if (String.IsNullOrEmpty(strEntityDbName) == false)
                    {
                        if (bRecreate == false)
                        {
                            // ���cfgdom���Ƿ��Ѿ�����ͬ����ʵ���
                            if (this.IsItemDbName(strEntityDbName) == true)
                            {
                                strError = "ʵ��� '" + strEntityDbName + "' �Ķ����Ѿ����ڣ������ظ�����";
                                goto ERROR1;
                            }
                        }

                        // ���ݿ��Ƿ��Ѿ����ڣ�
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   �������͵�ͬ�������Ѿ�����
                        nRet = IsDatabaseExist(
                            channel,
                            strEntityDbName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(strOrderDbName) == false)
                    {
                        if (bRecreate == false)
                        {
                            // ���cfgdom���Ƿ��Ѿ�����ͬ���Ķ�����
                            if (this.IsOrderDbName(strOrderDbName) == true)
                            {
                                strError = "������ '" + strOrderDbName + "' �Ķ����Ѿ����ڣ������ظ�����";
                                goto ERROR1;
                            }
                        }

                        // ���ݿ��Ƿ��Ѿ����ڣ�
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   �������͵�ͬ�������Ѿ�����
                        nRet = IsDatabaseExist(
                            channel,
                            strOrderDbName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }


                    if (String.IsNullOrEmpty(strIssueDbName) == false)
                    {
                        if (bRecreate == false)
                        {
                            // ���cfgdom���Ƿ��Ѿ�����ͬ�����ڿ�
                            if (this.IsOrderDbName(strIssueDbName) == true)
                            {
                                strError = "�ڿ� '" + strIssueDbName + "' �Ķ����Ѿ����ڣ������ظ�����";
                                goto ERROR1;
                            }
                        }

                        // ���ݿ��Ƿ��Ѿ����ڣ�
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   �������͵�ͬ�������Ѿ�����
                        nRet = IsDatabaseExist(
                            channel,
                            strIssueDbName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(strCommentDbName) == false)
                    {
                        if (bRecreate == false)
                        {
                            // ���cfgdom���Ƿ��Ѿ�����ͬ������ע��
                            if (this.IsCommentDbName(strCommentDbName) == true)
                            {
                                strError = "��ע�� '" + strCommentDbName + "' �Ķ����Ѿ����ڣ������ظ�����";
                                goto ERROR1;
                            }
                        }

                        // ���ݿ��Ƿ��Ѿ����ڣ�
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   �������͵�ͬ�������Ѿ�����
                        nRet = IsDatabaseExist(
                            channel,
                            strCommentDbName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    // ��ʼ����

                    // ������Ŀ��
                    string strTemplateDir = this.DataDir + "\\templates\\" + "biblio_" + strSyntax + "_" + strUsage;

                    // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    created_dbnames.Add(strName);

                    bDbChanged = true;

                    // ����ʵ���
                    if (String.IsNullOrEmpty(strEntityDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "item";

                        // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                        nRet = CreateDatabase(channel,
                            strTemplateDir,
                            strEntityDbName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        created_dbnames.Add(strEntityDbName);

                        bDbChanged = true;
                    }

                    // ����������
                    if (String.IsNullOrEmpty(strOrderDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "order";

                        // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                        nRet = CreateDatabase(channel,
                            strTemplateDir,
                            strOrderDbName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        created_dbnames.Add(strOrderDbName);

                        bDbChanged = true;
                    }

                    // �����ڿ�
                    if (String.IsNullOrEmpty(strIssueDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "issue";

                        // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                        nRet = CreateDatabase(channel,
                            strTemplateDir,
                            strIssueDbName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        created_dbnames.Add(strIssueDbName);

                        bDbChanged = true;
                    }

                    // ������ע��
                    if (String.IsNullOrEmpty(strCommentDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "comment";

                        // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                        nRet = CreateDatabase(channel,
                            strTemplateDir,
                            strCommentDbName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        created_dbnames.Add(strCommentDbName);

                        bDbChanged = true;
                    }

                    // ��CfgDom��������ص�������Ϣ
                    XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup");
                    if (root == null)
                    {
                        root = this.LibraryCfgDom.CreateElement("itemdbgroup");
                        this.LibraryCfgDom.DocumentElement.AppendChild(root);
                    }

                    XmlNode nodeNewDatabase = null;

                    if (bRecreate == false)
                    {
                        nodeNewDatabase = this.LibraryCfgDom.CreateElement("database");
                        root.AppendChild(nodeNewDatabase);
                    }
                    else
                    {
                        nodeNewDatabase = exist_database_node;
                    }

                    DomUtil.SetAttr(nodeNewDatabase, "name", strEntityDbName);
                    DomUtil.SetAttr(nodeNewDatabase, "biblioDbName", strName);
                    if (String.IsNullOrEmpty(strOrderDbName) == false)
                    {
                        DomUtil.SetAttr(nodeNewDatabase, "orderDbName", strOrderDbName);
                    }
                    if (String.IsNullOrEmpty(strIssueDbName) == false)
                    {
                        DomUtil.SetAttr(nodeNewDatabase, "issueDbName", strIssueDbName);
                    }
                    if (String.IsNullOrEmpty(strCommentDbName) == false)
                    {
                        DomUtil.SetAttr(nodeNewDatabase, "commentDbName", strCommentDbName);
                    }
                    DomUtil.SetAttr(nodeNewDatabase, "syntax", strSyntax);

                    // 2009/10/23
                    DomUtil.SetAttr(nodeNewDatabase, "role", strRole);

                    DomUtil.SetAttr(nodeNewDatabase, "inCirculation", strInCirculation);

                    // 2012/4/30
                    if (string.IsNullOrEmpty(strUnionCatalogStyle) == false)
                        DomUtil.SetAttr(nodeNewDatabase, "unionCatalogStyle", strUnionCatalogStyle);

                    if (string.IsNullOrEmpty(strReplication) == false)
                        DomUtil.SetAttr(nodeNewDatabase, "replication", strReplication);

                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    this.Changed = true;
                    this.ActivateManagerThread();

                    created_dbnames.Clear();

                    continue;
                } // end of type biblio
                else if (strType == "entity")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������������´���ʵ���";
                        return -1;
                    }
                    // TODO: ����recreate����

                    // ��������ʵ���
                    string strBiblioDbName = DomUtil.GetAttr(node, "biblioDbName");
                    if (String.IsNullOrEmpty(strBiblioDbName) == true)
                    {
                        strError = "���󴴽�ʵ����<database>Ԫ���У�Ӧ����biblioDbName����";
                        goto ERROR1;
                    }

                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strBiblioDbName + "' ����Ŀ��(biblioDbName����)���<database>Ԫ��û���ҵ����޷������´���ʵ��� " + strName;
                        goto ERROR1;
                    }

                    string strOldEntityDbName = DomUtil.GetAttr(nodeDatabase,
                        "name");
                    if (strOldEntityDbName == strName)
                    {
                        strError = "��������Ŀ�� '" + strBiblioDbName + "' ��ʵ��� '" + strName + "' �����Ѿ����ڣ������ظ�����";
                        goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(strOldEntityDbName) == false)
                    {
                        strError = "Ҫ������������Ŀ�� '" + strBiblioDbName + "' ����ʵ��� '" + strName + "'��������ɾ���Ѿ����ڵ�ʵ��� '"
                            + strOldEntityDbName + "'";
                        goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "item";

                    // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    created_dbnames.Add(strName);

                    bDbChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "name", strName);

                    // 2008/12/4
                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    this.Changed = true;
                }
                else if (strType == "order")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������������´���������";
                        return -1;
                    }
                    // TODO: ����recreate����

                    // ��������������
                    string strBiblioDbName = DomUtil.GetAttr(node, "biblioDbName");
                    if (String.IsNullOrEmpty(strBiblioDbName) == true)
                    {
                        strError = "�����������<database>Ԫ���У�Ӧ����biblioDbName����";
                        goto ERROR1;
                    }

                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strBiblioDbName + "' ����Ŀ��(biblioDbName����)���<database>Ԫ��û���ҵ����޷������´��������� " + strName;
                        goto ERROR1;
                    }

                    string strOldOrderDbName = DomUtil.GetAttr(nodeDatabase,
                        "orderDbName");
                    if (strOldOrderDbName == strName)
                    {
                        strError = "��������Ŀ�� '" + strBiblioDbName + "' �Ķ����� '" + strName + "' �����Ѿ����ڣ������ظ�����";
                        goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(strOldOrderDbName) == false)
                    {
                        strError = "Ҫ������������Ŀ�� '" + strBiblioDbName + "' ���¶����� '" + strName + "'��������ɾ���Ѿ����ڵĶ����� '"
                            + strOldOrderDbName + "'";
                        goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "order";

                    // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);

                    bDbChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "orderDbName", strName);

                    // 2008/12/4
                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    this.Changed = true;
                }
                else if (strType == "issue")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������������´����ڿ�";
                        return -1;
                    }
                    // TODO: ����recreate����

                    // ���������ڿ�
                    string strBiblioDbName = DomUtil.GetAttr(node, "biblioDbName");
                    if (String.IsNullOrEmpty(strBiblioDbName) == true)
                    {
                        strError = "�����ڿ��<database>Ԫ���У�Ӧ����biblioDbName����";
                        goto ERROR1;
                    }

                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strBiblioDbName + "' ����Ŀ��(biblioDbName����)���<database>Ԫ��û���ҵ����޷������´����ڿ� " + strName;
                        goto ERROR1;
                    }

                    string strOldIssueDbName = DomUtil.GetAttr(nodeDatabase,
                        "issueDbName");
                    if (strOldIssueDbName == strName)
                    {
                        strError = "��������Ŀ�� '"+strBiblioDbName+"' ���ڿ� '" + strName + "' �����Ѿ����ڣ������ظ�����";
                        goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(strOldIssueDbName) == false)
                    {
                        strError = "Ҫ������������Ŀ�� '" + strBiblioDbName + "' �����ڿ� '" + strName + "'��������ɾ���Ѿ����ڵ��ڿ� '"
                            +strOldIssueDbName+"'";
                        goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "issue";

                    // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);

                    bDbChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "issueDbName", strName);

                    // 2008/12/4
                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    this.Changed = true;
                }
                else if (strType == "comment")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������������´�����ע��";
                        return -1;
                    }
                    // TODO: ����recreate����

                    // ����������ע��
                    string strBiblioDbName = DomUtil.GetAttr(node, "biblioDbName");
                    if (String.IsNullOrEmpty(strBiblioDbName) == true)
                    {
                        strError = "������ע���<database>Ԫ���У�Ӧ����biblioDbName����";
                        goto ERROR1;
                    }

                    // ����������С��
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "����DOM������Ϊ '" + strBiblioDbName + "' ����Ŀ��(biblioDbName����)���<database>Ԫ��û���ҵ����޷������´�����ע�� " + strName;
                        goto ERROR1;
                    }

                    string strOldCommentDbName = DomUtil.GetAttr(nodeDatabase,
                        "commentDbName");
                    if (strOldCommentDbName == strName)
                    {
                        strError = "��������Ŀ�� '" + strBiblioDbName + "' ����ע�� '" + strName + "' �����Ѿ����ڣ������ظ�����";
                        goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(strOldCommentDbName) == false)
                    {
                        strError = "Ҫ������������Ŀ�� '" + strBiblioDbName + "' ������ע�� '" + strName + "'��������ɾ���Ѿ����ڵ���ע�� '"
                            + strOldCommentDbName + "'";
                        goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "comment";

                    // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);

                    bDbChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "commentDbName", strName);

                    // 2008/12/4
                    // <itemdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    this.Changed = true;
                }
                else if (strType == "reader")
                {
                    // �������߿�

                    // 2009/11/13
                    XmlNode exist_database_node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readerdbgroup/database[@name='" + strName + "']");
                    if (bRecreate == true && exist_database_node == null)
                    {
                        strError = "library.xml�в������ڶ��߿� '" + strName + "' �Ķ��壬�޷��������´���";
                        goto ERROR1;
                    }


                    if (bRecreate == false)
                    {
                        // ���cfgdom���Ƿ��Ѿ�����ͬ���Ķ��߿�
                        if (this.IsReaderDbName(strName) == true)
                        {
                            strError = "���߿� '" + strName + "' �Ķ����Ѿ����ڣ������ظ�����";
                            goto ERROR1;
                        }
                    }
                    else
                    {
                        if (exist_database_node != null)
                        {
                            string strExistLibraryCode = DomUtil.GetAttr(exist_database_node, "libraryCode");

                            // 2012/9/9
                            // �ֹ��û�ֻ�����޸Ĺݴ������ڹ�Ͻ�ֹݵĶ��߿�
                            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                            {
                                if (string.IsNullOrEmpty(strExistLibraryCode) == true
                                    || StringUtil.IsInList(strExistLibraryCode, strLibraryCodeList) == false)
                                {
                                    strError = "���´������߿� '"+strName+"' ���ܾ�����ǰ�û�ֻ�����´���ͼ��ݴ�����ȫ��ȫ���� '" + strLibraryCodeList + "' ��Χ�Ķ��߿�";
                                    goto ERROR1;
                                }
                            }
                        }
                    }

                    string strLibraryCode = DomUtil.GetAttr(node,
    "libraryCode");

                    // 2012/9/9
                    // �ֹ��û�ֻ������ݴ���Ϊ�ض���Χ�Ķ��߿�
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        if (string.IsNullOrEmpty(strLibraryCode) == true
                            || IsListInList(strLibraryCode, strLibraryCodeList) == false)
                        {
                            strError = "��ǰ�û�ֻ�ܴ����ݴ�����ȫ���� '" + strLibraryCodeList + "' ��Χ�ڵĶ��߿�";
                            return -1;
                        }
                    }

                    // ���dp2kernel���Ƿ��кͶ��߿�ͬ�������ݿ����
                    {
                        // ���ݿ��Ƿ��Ѿ����ڣ�
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   �������͵�ͬ�������Ѿ�����
                        nRet = IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "reader";

                    // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);

                    bDbChanged = true;

                    string strInCirculation = DomUtil.GetAttr(node,
                        "inCirculation");
                    if (String.IsNullOrEmpty(strInCirculation) == true)
                        strInCirculation = "true";  // ȱʡΪtrue


                    // ���һ��������ͼ��ݴ����Ƿ��ʽ��ȷ
                    // Ҫ����Ϊ '*'�����ܰ�������
                    // return:
                    //      -1  У�麯�����������
                    //      0   У����ȷ
                    //      1   У�鷢�����⡣strError��������
                    nRet = VerifySingleLibraryCode(strLibraryCode,
        out strError);
                    if (nRet != 0)
                    {
                        strError = "ͼ��ݴ��� '" + strLibraryCode + "' ��ʽ����: " + strError;
                        goto ERROR1;
                    }

                    // ��CfgDom��������ص�������Ϣ
                    XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readerdbgroup");
                    if (root == null)
                    {
                        root = this.LibraryCfgDom.CreateElement("readerdbgroup");
                        this.LibraryCfgDom.DocumentElement.AppendChild(root);
                    }

                    XmlNode nodeNewDatabase = null;
                    if (bRecreate == false)
                    {
                        nodeNewDatabase = this.LibraryCfgDom.CreateElement("database");
                        root.AppendChild(nodeNewDatabase);
                    }
                    else
                    {
                        nodeNewDatabase = exist_database_node;
                    }

                    DomUtil.SetAttr(nodeNewDatabase, "name", strName);
                    DomUtil.SetAttr(nodeNewDatabase, "inCirculation", strInCirculation);
                    DomUtil.SetAttr(nodeNewDatabase, "libraryCode", strLibraryCode);    // 2012/9/7

                    // <readerdbgroup>���ݸ��£�ˢ�����׵��ڴ�ṹ
                    this.LoadReaderDbGroupParam(this.LibraryCfgDom);
                    this.Changed = true;
                }
                else if (strType == "publisher"
                    || strType == "zhongcihao"
                    || strType == "dictionary"
                    || strType == "inventory")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������������´��������߿⡢�ִκſ⡢�ֵ����̵��";
                        return -1;
                    }

                    // ����ͬ���� publisher/zhongcihao/dictionary/inventory ���ݿ��Ƿ��Ѿ�����?
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strName + "']");
                    if (bRecreate == false)
                    {
                        if (nodeDatabase != null)
                        {
                            strError = strType + "�� '" + strName + "' �Ķ����Ѿ����ڣ������ظ�����";
                            goto ERROR1;
                        }
                    }
                    else
                    {
                        if (nodeDatabase == null)
                        {
                            strError = strType + "�� '" + strName + "' �Ķ��岢�����ڣ��޷������ظ�����";
                            goto ERROR1;
                        }
                    }

                    // TODO: �Ƿ��޶�publisher��ֻ�ܴ���һ����
                    // ��zhongcihao����Ȼ�ǿ��Դ��������

                    // ���dp2kernel���Ƿ��к� publisher/zhongcihao/dictionary/inventory ��ͬ�������ݿ����
                    {
                        // ���ݿ��Ƿ��Ѿ����ڣ�
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   �������͵�ͬ�������Ѿ�����
                        nRet = IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }


                    string strTemplateDir = this.DataDir + "\\templates\\" + strType;

                    // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);
                    bDbChanged = true;  // 2012/12/12

                    // ��CfgDom��������ص�������Ϣ
                    XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb");
                    if (root == null)
                    {
                        root = this.LibraryCfgDom.CreateElement("utilDb");
                        this.LibraryCfgDom.DocumentElement.AppendChild(root);
                    }

                    XmlNode nodeNewDatabase = null;
                    if (bRecreate == false)
                    {
                        nodeNewDatabase = this.LibraryCfgDom.CreateElement("database");
                        root.AppendChild(nodeNewDatabase);
                    }
                    else
                    {
                        nodeNewDatabase = nodeDatabase;
                    }

                    DomUtil.SetAttr(nodeNewDatabase, "name", strName);
                    DomUtil.SetAttr(nodeNewDatabase, "type", strType);
                    this.Changed = true;
                }
                else if (strType == "arrived")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������������´���ԤԼ�����";
                        return -1;
                    }

                    // ����ͬ���� arrived ���ݿ��Ƿ��Ѿ�����?
                    if (bRecreate == false)
                    {
                        if (this.ArrivedDbName == strName)
                        {
                            strError = "ԤԼ����� '" + strName + "' �Ķ����Ѿ����ڣ������ظ�����";
                            goto ERROR1;
                        }
                    }

                    if (String.IsNullOrEmpty(this.ArrivedDbName) == false)
                    {
                        if (bRecreate == true)
                        {
                            if (this.ArrivedDbName != strName)
                            {
                                strError = "�Ѿ�����һ��ԤԼ����� '" + this.ArrivedDbName + "' ���壬�����������´�����ԤԼ����� '" + strName + "' ���ֲ�ͬ���޷�ֱ�ӽ������´���������ɾ���Ѵ��ڵ����ݿ��ٽ��д���";
                                goto ERROR1;
                            }
                        }
                        else
                        {
                            strError = "Ҫ�����µ�ԤԼ����� '" + strName + "'��������ɾ���Ѿ����ڵ�ԤԼ����� '"
                                + this.ArrivedDbName + "'";
                            goto ERROR1;
                        }
                    }

                    // ���dp2kernel���Ƿ��к�arrived��ͬ�������ݿ����
                    {
                        // ���ݿ��Ƿ��Ѿ����ڣ�
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   �������͵�ͬ�������Ѿ�����
                        nRet = IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "arrived";

                    // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);
                    bDbChanged = true;  // 2012/12/12

                    // ��CfgDom��������ص�������Ϣ
                    this.ArrivedDbName = strName;
                    this.Changed = true;
                }
                else if (strType == "amerce")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������������´���ΥԼ���";
                        return -1;
                    }

                    // ����ͬ����amerce���ݿ��Ƿ��Ѿ�����?
                    if (bRecreate == false)
                    {
                        if (this.AmerceDbName == strName)
                        {
                            strError = "ΥԼ��� '" + strName + "' �Ķ����Ѿ����ڣ������ظ�����";
                            goto ERROR1;
                        }
                    }

                    if (String.IsNullOrEmpty(this.AmerceDbName) == false)
                    {
                        if (bRecreate == true)
                        {
                            if (this.AmerceDbName != strName)
                            {
                                strError = "�Ѿ�����һ��ΥԼ��� '" + this.AmerceDbName + "' ���壬�����������´�����ΥԼ��� '" + strName + "' ���ֲ�ͬ���޷�ֱ�ӽ������´���������ɾ���Ѵ��ڵ����ݿ��ٽ��д���";
                                goto ERROR1;
                            }
                        }
                        else
                        {
                            strError = "Ҫ�����µ�ΥԼ��� '" + strName + "'��������ɾ���Ѿ����ڵ�ΥԼ��� '"
                                + this.AmerceDbName + "'";
                            goto ERROR1;
                        }
                    }

                    // ���dp2kernel���Ƿ��к�amerce��ͬ�������ݿ����
                    {
                        // ���ݿ��Ƿ��Ѿ����ڣ�
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   �������͵�ͬ�������Ѿ�����
                        nRet = IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }


                    string strTemplateDir = this.DataDir + "\\templates\\" + "amerce";

                    // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);
                    bDbChanged = true;  // 2012/12/12

                    // ��CfgDom��������ص�������Ϣ
                    this.AmerceDbName = strName;
                    this.Changed = true;
                }
                else if (strType == "message")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������������´�����Ϣ��";
                        return -1;
                    }

                    // ����ͬ����message���ݿ��Ƿ��Ѿ�����?
                    if (bRecreate == false)
                    {
                        if (this.MessageDbName == strName)
                        {
                            strError = "��Ϣ�� '" + strName + "' �Ķ����Ѿ����ڣ������ظ�����";
                            goto ERROR1;
                        }
                    }

                    if (String.IsNullOrEmpty(this.MessageDbName) == false)
                    {
                        if (bRecreate == true)
                        {
                            if (this.MessageDbName != strName)
                            {
                                strError = "�Ѿ�����һ����Ϣ�� '" + this.MessageDbName + "' ���壬�����������´�������Ϣ�� '" + strName + "' ���ֲ�ͬ���޷�ֱ�ӽ������´���������ɾ���Ѵ��ڵ����ݿ��ٽ��д���";
                                goto ERROR1;
                            }
                        }
                        else
                        {
                            strError = "Ҫ�����µ���Ϣ�� '" + strName + "'��������ɾ���Ѿ����ڵ���Ϣ�� '"
                                + this.MessageDbName + "'";
                            goto ERROR1;
                        }
                    }

                    // ���dp2kernel���Ƿ��к�message��ͬ�������ݿ����
                    {
                        // ���ݿ��Ƿ��Ѿ����ڣ�
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   �������͵�ͬ�������Ѿ�����
                        nRet = IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }


                    string strTemplateDir = this.DataDir + "\\templates\\" + "message";

                    // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);
                    bDbChanged = true;  // 2012/12/12

                    // ��CfgDom��������ص�������Ϣ
                    this.MessageDbName = strName;
                    this.Changed = true;
                }
                else if (strType == "invoice")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "��ǰ�û�����ȫ���û����������������´�����Ʊ��";
                        return -1;
                    }

                    // ����ͬ����invoice���ݿ��Ƿ��Ѿ�����?
                    if (bRecreate == false)
                    {
                        if (this.InvoiceDbName == strName)
                        {
                            strError = "��Ʊ�� '" + strName + "' �Ķ����Ѿ����ڣ������ظ�����";
                            goto ERROR1;
                        }
                    }

                    if (String.IsNullOrEmpty(this.InvoiceDbName) == false)
                    {
                        if (bRecreate == true)
                        {
                            if (this.InvoiceDbName != strName)
                            {
                                strError = "�Ѿ�����һ����Ʊ�� '" + this.InvoiceDbName + "' ���壬�����������´����ķ�Ʊ�� '" + strName + "' ���ֲ�ͬ���޷�ֱ�ӽ������´���������ɾ���Ѵ��ڵ����ݿ��ٽ��д���";
                                goto ERROR1;
                            }
                        }
                        else
                        {
                            strError = "Ҫ�����µķ�Ʊ�� '" + strName + "'��������ɾ���Ѿ����ڵķ�Ʊ�� '"
                                + this.InvoiceDbName + "'";
                            goto ERROR1;
                        }
                    }

                    // ���dp2kernel���Ƿ��к�invoice��ͬ�������ݿ����
                    {
                        // ���ݿ��Ƿ��Ѿ����ڣ�
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   �������͵�ͬ�������Ѿ�����
                        nRet = IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }


                    string strTemplateDir = this.DataDir + "\\templates\\" + "invoice";

                    // ����Ԥ�ȵĶ��壬����һ�����ݿ�
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);
                    bDbChanged = true;  // 2012/12/12

                    // ��CfgDom��������ص�������Ϣ
                    this.InvoiceDbName = strName;
                    this.Changed = true;
                }
                else
                {
                    strError = "δ֪�����ݿ����� '" + strType + "'";
                    goto ERROR1;
                }

                if (this.Changed == true)
                    this.ActivateManagerThread();

                created_dbnames.Clear();
            }


            Debug.Assert(created_dbnames.Count == 0, "");

            if (bDbChanged == true)
            {
                nRet = InitialKdbs(
                    Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
                // ���³�ʼ������ⶨ��
                this.vdbs = null;
                nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return 0;
        ERROR1:
            List<string> error_deleting_dbnames = new List<string>();
            // �������Ѿ����������ݿ��ڷ���ǰɾ����
            for (int i = 0; i < created_dbnames.Count; i++)
            {
                string strDbName = created_dbnames[i];

                string strError_1 = "";

                long lRet = channel.DoDeleteDB(strDbName, out strError_1);
                if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    continue;
                if (lRet == -1)
                    error_deleting_dbnames.Add(strDbName + "[����:"+strError_1+"]");
            }

            if (error_deleting_dbnames.Count > 0)
            {
                strError = strError + ";\r\n����ɾ���մ��������ݿ�ʱ���������������ݿ�δ��ɾ��:" + StringUtil.MakePathList(error_deleting_dbnames);
                return -1;
            }

            return -1;
        }

        static int ConvertGb2312TextfileToUtf8(string strFilename,
            out string strError)
        {
            strError = "";

            StreamReader sr = null;

            // 2013/10/31 ����޷�ͨ���ļ�ͷ��̽�����������ת��
            Encoding encoding = FileUtil.DetectTextFileEncoding(strFilename, null);

            if (encoding == null || encoding.Equals(Encoding.UTF8) == true)
                return 0;

            try
            {
                sr = new StreamReader(strFilename, encoding);
            }
            catch (Exception ex)
            {
                strError = "���ļ� " + strFilename + " ʧ��: " + ex.Message;
                return -1;
            }

            string strContent = sr.ReadToEnd();

            sr.Close();

            try
            {

                StreamWriter sw = new StreamWriter(strFilename, false, Encoding.UTF8);
                sw.Write(strContent);
                sw.Close();
            }
            catch (Exception ex)
            {
                strError = "д���ļ� " + strFilename + " ʧ��: " + ex.Message;
                return -1;
            }

            return 0;
        }

        static string ConvertCrLf(string strText)
        {
            strText = strText.Replace("\r\n", "\r");
            strText = strText.Replace("\n", "\r");
            return strText.Replace("\r", "\r\n");
        }

        // �������ݿ�ģ��Ķ��壬ˢ��һ���Ѿ����ڵ����ݿ�Ķ���
        // return:
        //      -1
        //      0   keys����û�и���
        //      1   keys���������
        int RefreshDatabase(RmsChannel channel,
            string strTemplateDir,
            string strDatabaseName,
            string strIncludeFilenames,
            string strExcludeFilenames,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            strIncludeFilenames = strIncludeFilenames.ToLower();
            strExcludeFilenames = strExcludeFilenames.ToLower();

            bool bKeysChanged = false;

            DirectoryInfo di = new DirectoryInfo(strTemplateDir);
            FileInfo[] fis = di.GetFiles();

            // ���������ļ�����
            for (int i = 0; i < fis.Length; i++)
            {
                string strName = fis[i].Name;
                if (strName == "." || strName == "..")
                    continue;

                if (FileUtil.IsBackupFile(strName) == true)
                    continue;

                /*
                if (strName.ToLower() == "keys"
                    || strName.ToLower() == "browse")
                    continue;
                 * */

                // ���Include��exclude���涼��һ���ļ�����������exclude(�ų�)
                if (StringUtil.IsInList(strName, strExcludeFilenames) == true)
                    continue;

                if (strIncludeFilenames != "*")
                {
                    if (StringUtil.IsInList(strName, strIncludeFilenames) == false)
                        continue;
                }


                string strFullPath = fis[i].FullName;

                nRet = ConvertGb2312TextfileToUtf8(strFullPath,
                    out strError);
                if (nRet == -1)
                    return -1;

                string strExistContent = "";
                string strNewContent = "";

                Stream new_stream = new FileStream(strFullPath, FileMode.Open);

                {
                    StreamReader sr = new StreamReader(new_stream, Encoding.UTF8);
                    strNewContent = ConvertCrLf(sr.ReadToEnd());
                }

                new_stream.Seek(0, SeekOrigin.Begin);


                try
                {
                    string strPath = strDatabaseName + "/cfgs/" + strName;


                    // ��ȡ���е������ļ�����
                    byte[] timestamp = null;
                    string strOutputPath = "";
                    string strMetaData = "";

                    string strStyle = "content,data,metadata,timestamp,outputpath";
                    MemoryStream exist_stream = new MemoryStream();

                    try
                    {

                        long lRet = channel.GetRes(
                            strPath,
                            exist_stream,
                            null,	// stop,
                            strStyle,
                            null,	// byte [] input_timestamp,
                            out strMetaData,
                            out timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            // �����ļ������ڣ���ô���ش������?
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            {
                                timestamp = null;
                                goto DO_CREATE;
                            }
                            return -1;
                        }

                        exist_stream.Seek(0, SeekOrigin.Begin);
                        {
                            StreamReader sr = new StreamReader(exist_stream, Encoding.UTF8);
                            strExistContent = ConvertCrLf(sr.ReadToEnd());
                        }
                    }
                    finally
                    {
                        if (exist_stream != null)
                            exist_stream.Close();
                    }

                    // �Ƚϱ��صĺͷ���������������������Ͳ�Ҫ������
                    if (strExistContent == strNewContent)
                    {
                        continue;
                    }

                    DO_CREATE:

                    // �ڷ������˴�������
                    // parameters:
                    //      strStyle    ��񡣵�����Ŀ¼��ʱ��Ϊ"createdir"������Ϊ��
                    // return:
                    //		-1	����
                    //		1	�Ѿ�����ͬ������
                    //		0	��������
                    nRet = NewServerSideObject(
                        channel,
                        strPath,
                        "",
                        new_stream,
                        timestamp,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 1)
                    {
                        strError = "NewServerSideObject()�����Ѿ�����ͬ������: " + strError;
                        return -1;
                    }

                    if (strName.ToLower() == "keys")
                        bKeysChanged = true;

                }
                finally
                {
                    new_stream.Close();
                }
            }

            if (bKeysChanged == true)
            {
                // �����ݿ⼰ʱ����ˢ��keys���API
                long lRet = channel.DoRefreshDB(
                    "begin",
                    strDatabaseName,
                    false,
                    out strError);
                if (lRet == -1)
                {
                    strError = "���ݿ� '" + strDatabaseName + "' �Ķ����Ѿ����ɹ�ˢ�£�����ˢ���ں�Keys�����ʱʧ��: " + strError;
                    return -1;
                }
                return 1;
            }

            return 0;
        }

        // �������ݿ�ģ��Ķ��壬����һ�����ݿ�
        int CreateDatabase(RmsChannel channel,
            string strTemplateDir,
            string strDatabaseName,
            out string strError)
        {
            strError = "";

            int nRet = 0;

            List<string[]> logicNames = new List<string[]>();

            string[] cols = new string[2];
            cols[1] = "zh";
            cols[0] = strDatabaseName;
            logicNames.Add(cols);


            string strKeysDefFileName = PathUtil.MergePath(strTemplateDir, "keys");
            string strBrowseDefFileName = PathUtil.MergePath(strTemplateDir, "browse");

            nRet = ConvertGb2312TextfileToUtf8(strKeysDefFileName,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = ConvertGb2312TextfileToUtf8(strBrowseDefFileName,
                out strError);
            if (nRet == -1)
                return -1;

            string strKeysDef = "";
            string strBrowseDef = "";

            StreamReader sr = null;

            try
            {
                sr = new StreamReader(strKeysDefFileName, Encoding.UTF8);
                strKeysDef = sr.ReadToEnd();
                sr.Close();
            }
            catch (Exception ex)
            {
                strError = "װ���ļ� " + strKeysDefFileName + " ʱ��������: " + ex.Message;
                return -1;
            }


            try
            {
                sr = new StreamReader(strBrowseDefFileName, Encoding.UTF8);
                strBrowseDef = sr.ReadToEnd();
                sr.Close();
            }
            catch (Exception ex)
            {
                strError = "װ���ļ� " + strBrowseDefFileName + " ʱ��������: " + ex.Message;
                return -1;
            }


            long lRet = channel.DoCreateDB(logicNames,
                "", // strType,
                "", // strSqlDbName,
                strKeysDef,
                strBrowseDef,
                out strError);
            if (lRet == -1)
            {
                strError = "�������ݿ� " + strDatabaseName + " ʱ��������: " + strError;
                return -1;
            }

            lRet = channel.DoInitialDB(strDatabaseName,
                out strError);
            if (lRet == -1)
            {
                strError = "��ʼ�����ݿ� " + strDatabaseName + " ʱ��������: " + strError;
                return -1;
            }

            // �����������ݴ�������

            /*
            List<string> subdirs = new List<string>();
            // ��������Ŀ¼����
            GetSubdirs(strTemplateDir, ref subdirs);
            for (int i = 0; i < subdirs.Count; i++)
            {
                string strDiskPath = subdirs[i];

                // ����������Ϊ�߼�·��
                // ����Ԥ���ڻ�õ������оʹ��Ϊ����(�߼�)·����
                string strPath = "";

                // �ڷ������˴�������
                // parameters:
                //      strStyle    ��񡣵�����Ŀ¼��ʱ��Ϊ"createdir"������Ϊ��
                // return:
                //		-1	����
                //		1	�Լ�����ͬ������
                //		0	��������
                nRet = NewServerSideObject(
                    channel,
                    strPath,
                    "createdir",
                    null,
                    null,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
                // �г�ÿ��Ŀ¼�е��ļ������ڷ������˴���֮
                // ע��ģ��Ŀ¼�µ��ļ���������cfgs�е��ļ�������
             * */

            DirectoryInfo di = new DirectoryInfo(strTemplateDir);
            FileInfo[] fis = di.GetFiles();

            // ���������ļ�����
            for (int i = 0; i < fis.Length; i++)
            {
                string strName = fis[i].Name;
                if (strName == "." || strName == "..")
                    continue;

                if (strName.ToLower() == "keys"
                    || strName.ToLower() == "browse")
                    continue;

                string strFullPath = fis[i].FullName;

                nRet = ConvertGb2312TextfileToUtf8(strFullPath,
                    out strError);
                if (nRet == -1)
                    return -1;

                Stream s = new FileStream(strFullPath, FileMode.Open);

                try
                {
                    string strPath = strDatabaseName + "/cfgs/" + strName;




                    // �ڷ������˴�������
                    // parameters:
                    //      strStyle    ��񡣵�����Ŀ¼��ʱ��Ϊ"createdir"������Ϊ��
                    // return:
                    //		-1	����
                    //		1	�Լ�����ͬ������
                    //		0	��������
                    nRet = NewServerSideObject(
                        channel,
                        strPath,
                        "",
                        s,
                        null,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }
                finally
                {
                    s.Close();
                }
            }



            return 0;
        }

        void GetSubdirs(string strCurrentDir,
            ref List<string> results)
        {
            DirectoryInfo di = new DirectoryInfo(strCurrentDir + "\\");
            DirectoryInfo[] dia = di.GetDirectories("*.*");

            // Array.Sort(dia, new DirectoryInfoCompare());
            for (int i = 0; i < dia.Length; i++)
            {
                string strThis = dia[i].FullName;
                results.Add(strThis);
                GetSubdirs(strThis, ref results);
            }
        }

        // �ڷ������˴�������
        // parameters:
        //      strStyle    ��񡣵�����Ŀ¼��ʱ��Ϊ"createdir"������Ϊ��
        // return:
        //		-1	����
        //		1	�Լ�����ͬ������
        //		0	��������
        int NewServerSideObject(
            RmsChannel channel,
            string strPath,
            string strStyle,
            Stream stream,
            byte[] baTimeStamp,
            out string strError)
        {
            strError = "";

            byte[] baOutputTimestamp = null;
            string strOutputPath = "";

            string strRange = "";
            if (stream != null && stream.Length != 0)
            {
                Debug.Assert(stream.Length != 0, "test");
                strRange = "0-" + Convert.ToString(stream.Length - 1);
            }
            long lRet = channel.DoSaveResObject(strPath,
                stream,
                (stream != null && stream.Length != 0) ? stream.Length : 0,
                strStyle,
                "",	// strMetadata,
                strRange,
                true,
                baTimeStamp,	// timestamp,
                out baOutputTimestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                if (channel.ErrorCode == ChannelErrorCode.AlreadyExist)
                {
                    return 1;	// �Ѿ�����ͬ��ͬ���Ͷ���
                }
                strError = "д�� '" + strPath + "' ��������: " + strError;
                return -1;
            }

            return 0;
        }


        // ���ݿ��Ƿ��Ѿ����ڣ�
        // return:
        //      -1  error
        //      0   not exist
        //      1   exist
        //      2   �������͵�ͬ�������Ѿ�����
        int IsDatabaseExist(
            RmsChannel channel,
            string strDatabaseName,
            out string strError)
        {
            strError = "";

            // �������ݿ��Ƿ��Ѿ�����
            ResInfoItem[] items = null;
            long lRet = channel.DoDir("",
                "zh",
                "", // style
                out items,
                out strError);
            if (lRet == -1)
            {
                strError = "�з����� " + channel.Url + " ��ȫ�����ݿ�Ŀ¼��ʱ�����: " + strError;
                return -1;
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Name == strDatabaseName)
                {
                    if (items[i].Type == ResTree.RESTYPE_DB)
                    {
                        strError = "���ݿ� " + strDatabaseName + " �Ѿ����ڡ�";
                        return 1;
                    }
                    else
                    {
                        strError = "�����ݿ� " + strDatabaseName + " ͬ���ķ����ݿ����Ͷ����Ѿ����ڡ�";
                        return 2;
                    }
                }
            }

            return 0;
        }

        // ������ݿ���Ϣ
        // parameters:
        //      strLibraryCodeList  ��ǰ�û��Ĺ�Ͻ�ֹݴ����б�
        int GetDatabaseInfo(
            string strLibraryCodeList,
            string strDatabaseNames,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            if (String.IsNullOrEmpty(strDatabaseNames) == true)
                strDatabaseNames = "#biblio,#reader,#arrived,#amerce,#invoice,#util,#message";  // ע: #util �൱�� #zhongcihao,#publisher,#dictionary,#inventory

            // ���ڹ��췵�ؽ���ַ�����DOM
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            string[] names = strDatabaseNames.Split(new char[] {','});
            for (int i = 0; i < names.Length; i++)
            {
            CONTINUE:
                string strName = names[i].Trim();
                if (String.IsNullOrEmpty(strName) == true)
                    continue;

                // ��������
                if (strName[0] == '#')
                {
                    if (strName == "#reader")
                    {
                        // ���߿�
                        for (int j = 0; j < this.ReaderDbs.Count; j++)
                        {
                            string strDbName = this.ReaderDbs[j].DbName;
                            string strLibraryCode = this.ReaderDbs[j].LibraryCode;

                            // 2012/9/9
                            // ֻ����ǰ�û������Լ���Ͻ�Ķ��߿�
                            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                            {
                                if (string.IsNullOrEmpty(strLibraryCode) == true
                                    || StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                                    continue;
                            }

                            XmlNode nodeDatabase = dom.CreateElement("database");
                            dom.DocumentElement.AppendChild(nodeDatabase);

                            DomUtil.SetAttr(nodeDatabase, "type", "reader");
                            DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                            string strInCirculation = this.ReaderDbs[j].InCirculation == true ? "true" : "false";
                            DomUtil.SetAttr(nodeDatabase, "inCirculation", strInCirculation);

                            DomUtil.SetAttr(nodeDatabase, "libraryCode", strLibraryCode);
                        }
                    }
                    else if (strName == "#biblio")
                    {
                        // ʵ���(��Ŀ��)
                        for (int j = 0; j < this.ItemDbs.Count; j++)
                        {
                            XmlNode nodeDatabase = dom.CreateElement("database");
                            dom.DocumentElement.AppendChild(nodeDatabase);

                            ItemDbCfg cfg = this.ItemDbs[j];

                            DomUtil.SetAttr(nodeDatabase, "type", "biblio");
                            DomUtil.SetAttr(nodeDatabase, "name", cfg.BiblioDbName);
                            DomUtil.SetAttr(nodeDatabase, "syntax", cfg.BiblioDbSyntax);
                            DomUtil.SetAttr(nodeDatabase, "entityDbName", cfg.DbName);
                            DomUtil.SetAttr(nodeDatabase, "orderDbName", cfg.OrderDbName);
                            DomUtil.SetAttr(nodeDatabase, "issueDbName", cfg.IssueDbName);
                            DomUtil.SetAttr(nodeDatabase, "commentDbName", cfg.CommentDbName);
                            DomUtil.SetAttr(nodeDatabase, "unionCatalogStyle", cfg.UnionCatalogStyle);
                            string strInCirculation = cfg.InCirculation == true ? "true" : "false";
                            DomUtil.SetAttr(nodeDatabase, "inCirculation", strInCirculation);
                            DomUtil.SetAttr(nodeDatabase, "role", cfg.Role);    // 2009/10/23
                            DomUtil.SetAttr(nodeDatabase, "replication", cfg.Replication);    // 2009/10/23
                            /*
                            DomUtil.SetAttr(nodeDatabase, "biblioDbName", cfg.BiblioDbName);
                            DomUtil.SetAttr(nodeDatabase, "itemDbName", cfg.DbName);
                             * */
                        }
                    }
                    else if (strName == "#arrived")
                    {
                        if (String.IsNullOrEmpty(this.ArrivedDbName) == true)
                            continue;

                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", "arrived");
                        DomUtil.SetAttr(nodeDatabase, "name", this.ArrivedDbName);
                    }
                    else if (strName == "#amerce")
                    {
                        if (String.IsNullOrEmpty(this.AmerceDbName) == true)
                            continue;

                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", "amerce");
                        DomUtil.SetAttr(nodeDatabase, "name", this.AmerceDbName);
                    }
                    else if (strName == "#invoice")
                    {
                        if (String.IsNullOrEmpty(this.InvoiceDbName) == true)
                            continue;

                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", "invoice");
                        DomUtil.SetAttr(nodeDatabase, "name", this.InvoiceDbName);
                    }
                    else if (strName == "#message")
                    {
                        if (String.IsNullOrEmpty(this.MessageDbName) == true)
                            continue;

                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", "message");
                        DomUtil.SetAttr(nodeDatabase, "name", this.MessageDbName);
                    }
                    else if (strName == "#util"
                        || strName == "#publisher"
                        || strName == "#zhongcihao"
                        || strName == "#dictionary"
                        || strName == "#inventory")
                    {
                        string strType = "";
                        if (strName != "#util")
                            strType = strName.Substring(1);
                        XmlNodeList nodes = null;
                        if (string.IsNullOrEmpty(strType ) == true)
                            nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("utilDb/database");
                        else
                            nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("utilDb/database[@type='" + strType + "']");
                        foreach (XmlNode node in nodes)
                        {
                            XmlNode nodeDatabase = dom.CreateElement("database");
                            dom.DocumentElement.AppendChild(nodeDatabase);

                            DomUtil.SetAttr(nodeDatabase, "type", DomUtil.GetAttr(node, "type"));
                            DomUtil.SetAttr(nodeDatabase, "name", DomUtil.GetAttr(node, "name"));
                        }
                    }
#if NO
                    else if (strName == "#zhongcihao")
                    {
                        XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("utilDb/database[@type='zhongcihao']");
                        for (int j = 0; j < nodes.Count; j++)
                        {
                            XmlNode nodeDatabase = dom.CreateElement("database");
                            dom.DocumentElement.AppendChild(nodeDatabase);

                            DomUtil.SetAttr(nodeDatabase, "type", "zhongcihao");
                            string strTemp = DomUtil.GetAttr(nodes[j], "name");
                            DomUtil.SetAttr(nodeDatabase, "name", strTemp);
                        }
                    }
#endif
                    else
                    {
                        strError = "����ʶ������ݿ��� '" + strName + "'";
                        return -1;
                    }
                    continue;
                }

                // ��ͨ����

                // �Ƿ�ΪԤԼ������п⣿
                if (strName == this.ArrivedDbName)
                {
                    XmlNode nodeDatabase = dom.CreateElement("database");
                    dom.DocumentElement.AppendChild(nodeDatabase);

                    DomUtil.SetAttr(nodeDatabase, "type", "arrived");
                    DomUtil.SetAttr(nodeDatabase, "name", this.ArrivedDbName);
                    goto CONTINUE;
                }

                // �Ƿ�ΪΥԼ���?
                if (strName == this.AmerceDbName)
                {
                    XmlNode nodeDatabase = dom.CreateElement("database");
                    dom.DocumentElement.AppendChild(nodeDatabase);

                    DomUtil.SetAttr(nodeDatabase, "type", "amerce");
                    DomUtil.SetAttr(nodeDatabase, "name", this.AmerceDbName);
                    goto CONTINUE;
                }

                // �Ƿ�Ϊ��Ʊ��?
                if (strName == this.InvoiceDbName)
                {
                    XmlNode nodeDatabase = dom.CreateElement("database");
                    dom.DocumentElement.AppendChild(nodeDatabase);

                    DomUtil.SetAttr(nodeDatabase, "type", "invoice");
                    DomUtil.SetAttr(nodeDatabase, "name", this.InvoiceDbName);
                    goto CONTINUE;
                }

                // �Ƿ�Ϊ��Ϣ��?
                if (strName == this.MessageDbName)
                {
                    XmlNode nodeDatabase = dom.CreateElement("database");
                    dom.DocumentElement.AppendChild(nodeDatabase);

                    DomUtil.SetAttr(nodeDatabase, "type", "message");
                    DomUtil.SetAttr(nodeDatabase, "name", this.MessageDbName);
                    goto CONTINUE;
                }

                // �Ƿ�Ϊ���߿⣿
                for (int j = 0; j < this.ReaderDbs.Count; j++)
                {
                    string strDbName = this.ReaderDbs[j].DbName;
                    if (strName == strDbName)
                    {
                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", "reader");
                        DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                        string strInCirculation = this.ReaderDbs[j].InCirculation == true ? "true" : "false";
                        DomUtil.SetAttr(nodeDatabase, "inCirculation", strInCirculation);

                        goto CONTINUE;
                    }
                }

                // �Ƿ�Ϊ��Ŀ��?
                for (int j = 0; j < this.ItemDbs.Count; j++)
                {
                    string strDbName = this.ItemDbs[j].BiblioDbName;
                    if (strName == strDbName)
                    {
                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        ItemDbCfg cfg = this.ItemDbs[j];

                        DomUtil.SetAttr(nodeDatabase, "type", "biblio");
                        DomUtil.SetAttr(nodeDatabase, "name", cfg.BiblioDbName);
                        DomUtil.SetAttr(nodeDatabase, "syntax", cfg.BiblioDbSyntax);
                        DomUtil.SetAttr(nodeDatabase, "entityDbName", cfg.DbName);
                        DomUtil.SetAttr(nodeDatabase, "orderDbName", cfg.OrderDbName);
                        DomUtil.SetAttr(nodeDatabase, "issueDbName", cfg.IssueDbName);
                        DomUtil.SetAttr(nodeDatabase, "commentDbName", cfg.CommentDbName);
                        DomUtil.SetAttr(nodeDatabase, "unionCatalogStyle", cfg.UnionCatalogStyle);
                        DomUtil.SetAttr(nodeDatabase, "replication", cfg.Replication);

                        string strInCirculation = cfg.InCirculation == true ? "true" : "false";
                        DomUtil.SetAttr(nodeDatabase, "inCirculation", strInCirculation);


                        goto CONTINUE;
                    }
                }

                // �Ƿ�Ϊʵ���?
                for (int j = 0; j < this.ItemDbs.Count; j++)
                {
                    string strDbName = this.ItemDbs[j].DbName;
                    if (strName == strDbName)
                    {
                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        ItemDbCfg cfg = this.ItemDbs[j];

                        DomUtil.SetAttr(nodeDatabase, "type", "entity");
                        DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                        DomUtil.SetAttr(nodeDatabase, "biblioDbName", cfg.BiblioDbSyntax);

                        string strInCirculation = cfg.InCirculation == true ? "true" : "false";
                        DomUtil.SetAttr(nodeDatabase, "inCirculation", strInCirculation);


                        goto CONTINUE;
                    }
                }

                // �Ƿ�Ϊ������?
                for (int j = 0; j < this.ItemDbs.Count; j++)
                {
                    string strDbName = this.ItemDbs[j].OrderDbName;
                    if (strName == strDbName)
                    {
                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        ItemDbCfg cfg = this.ItemDbs[j];

                        DomUtil.SetAttr(nodeDatabase, "type", "order");
                        DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                        DomUtil.SetAttr(nodeDatabase, "biblioDbName", cfg.BiblioDbSyntax);
                        goto CONTINUE;
                    }
                }

                // �Ƿ�Ϊ�ڿ���?
                for (int j = 0; j < this.ItemDbs.Count; j++)
                {
                    string strDbName = this.ItemDbs[j].IssueDbName;
                    if (strName == strDbName)
                    {
                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        ItemDbCfg cfg = this.ItemDbs[j];

                        DomUtil.SetAttr(nodeDatabase, "type", "issue");
                        DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                        DomUtil.SetAttr(nodeDatabase, "biblioDbName", cfg.BiblioDbSyntax);
                        goto CONTINUE;
                    }
                }

                // �Ƿ�Ϊ��ע��?
                for (int j = 0; j < this.ItemDbs.Count; j++)
                {
                    string strDbName = this.ItemDbs[j].CommentDbName;
                    if (strName == strDbName)
                    {
                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        ItemDbCfg cfg = this.ItemDbs[j];

                        DomUtil.SetAttr(nodeDatabase, "type", "comment");
                        DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                        DomUtil.SetAttr(nodeDatabase, "biblioDbName", cfg.BiblioDbSyntax);
                        goto CONTINUE;
                    }
                }


                // �Ƿ�Ϊ publisher/zhongcihao/dictionary/inventory ��?
                {
                    XmlNode nodeUtilDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strName + "']");
                    if (nodeUtilDatabase != null)
                    {
                        string strType = DomUtil.GetAttr(nodeUtilDatabase, "type");

                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", strType);
                        DomUtil.SetAttr(nodeDatabase, "name", strName);
                        goto CONTINUE;
                    }
                }



                strError = "���������ݿ��� '" + strName + "'";
                return -1;

                /*
            CONTINUE:
                int test = 0;
                 * */

            }

            strOutputInfo = dom.OuterXml;

            return 0;
        }


        // ��ʼ���������ݿ�
        public int ClearAllDbs(
            RmsChannelCollection Channels,
            out string strError)
        {
            strError = "";

            string strTempError = "";

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "GetChannel error";
                return -1;
            }

            long lRet = 0;

            // ����Ŀ��
            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                ItemDbCfg cfg = this.ItemDbs[i];
                // ʵ���
                string strEntityDbName = cfg.DbName;

                if (String.IsNullOrEmpty(strEntityDbName) == false)
                {
                    lRet = channel.DoInitialDB(strEntityDbName,
                        out strTempError);
                    if (lRet == -1)
                    {
                        strError += "���ʵ��� '" + strEntityDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }

                // ������
                string strOrderDbName = cfg.OrderDbName;

                if (String.IsNullOrEmpty(strOrderDbName) == false)
                {
                    lRet = channel.DoInitialDB(strOrderDbName,
                        out strTempError);
                    if (lRet == -1)
                    {
                        strError += "��������� '" + strOrderDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }

                // �ڿ�
                string strIssueDbName = cfg.IssueDbName;

                if (String.IsNullOrEmpty(strIssueDbName) == false)
                {
                    lRet = channel.DoInitialDB(strIssueDbName,
                        out strTempError);
                    if (lRet == -1)
                    {
                        strError += "����ڿ� '" + strIssueDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }

                // С��Ŀ��
                string strBiblioDbName = cfg.BiblioDbName;

                if (String.IsNullOrEmpty(strBiblioDbName) == false)
                {
                    lRet = channel.DoInitialDB(strBiblioDbName,
                        out strTempError);
                    if (lRet == -1)
                    {
                        strError += "���С��Ŀ�� '" + strBiblioDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }
            }

            // ���߿�
            for (int i = 0; i < this.ReaderDbs.Count; i++)
            {
                string strDbName = this.ReaderDbs[i].DbName;

                if (String.IsNullOrEmpty(strDbName) == false)
                {
                    lRet = channel.DoInitialDB(strDbName,
                        out strTempError);
                    if (lRet == -1)
                    {
                        strError += "������߿� '" + strDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }
            }

            // ԤԼ������п�
            if (String.IsNullOrEmpty(this.ArrivedDbName) == false)
            {
                string strDbName = this.ArrivedDbName;
                lRet = channel.DoInitialDB(strDbName,
                    out strTempError);
                if (lRet == -1)
                {
                    strError += "���ԤԼ����� '" + strDbName + "' ������ʱ��������" + strTempError + "; ";
                }

            }

            // ΥԼ���
            if (String.IsNullOrEmpty(this.AmerceDbName) == false)
            {
                string strDbName = this.AmerceDbName;
                lRet = channel.DoInitialDB(strDbName,
                    out strTempError);
                if (lRet == -1)
                {
                    strError += "���ΥԼ��� '" + strDbName + "' ������ʱ��������" + strTempError + "; ";
                }
            }

            // ��Ʊ��
            if (String.IsNullOrEmpty(this.InvoiceDbName) == false)
            {
                string strDbName = this.InvoiceDbName;
                lRet = channel.DoInitialDB(strDbName,
                    out strTempError);
                if (lRet == -1)
                {
                    strError += "�����Ʊ�� '" + strDbName + "' ������ʱ��������" + strTempError + "; ";
                }
            }

            // ��Ϣ��
            if (String.IsNullOrEmpty(this.MessageDbName) == false)
            {
                string strDbName = this.MessageDbName;
                lRet = channel.DoInitialDB(strDbName,
                    out strTempError);
                if (lRet == -1)
                {
                    strError += "�����Ϣ�� '" + strDbName + "' ������ʱ��������" + strTempError + "; ";
                }
            }

            // ʵ�ÿ�
            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("utilDb/database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strDbName = DomUtil.GetAttr(node, "name");
                string strType = DomUtil.GetAttr(node, "type");
                if (String.IsNullOrEmpty(strDbName) == false)
                {
                    lRet = channel.DoInitialDB(strDbName,
                        out strTempError);
                    if (lRet == -1)
                    {
                        strError += "�������Ϊ "+strType+" ��ʵ�ÿ� '" + strDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }
            }


            if (String.IsNullOrEmpty(strError) == false)
                return -1;

            return 0;
        }

        // ɾ�������õ����ں����ݿ�
        // ר�ſ�������װ����ж��ʱ��ʹ��
        public static int DeleteAllDatabase(
            RmsChannel channel,
            XmlDocument cfg_dom,
            out string strError)
        {
            strError = "";

            string strTempError = "";

            long lRet = 0;

            // ����Ŀ��
            XmlNodeList nodes = cfg_dom.DocumentElement.SelectNodes("itemdbgroup/database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                // ʵ���
                string strEntityDbName = DomUtil.GetAttr(node, "name");

                if (String.IsNullOrEmpty(strEntityDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strEntityDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "ɾ��ʵ��� '" + strEntityDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }

                // ������
                string strOrderDbName = DomUtil.GetAttr(node, "orderDbName");

                if (String.IsNullOrEmpty(strOrderDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strOrderDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "ɾ�������� '" + strOrderDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }

                // �ڿ�
                string strIssueDbName = DomUtil.GetAttr(node, "issueDbName");

                if (String.IsNullOrEmpty(strIssueDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strIssueDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "ɾ���ڿ� '" + strIssueDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }

                // С��Ŀ��
                string strBiblioDbName = DomUtil.GetAttr(node, "biblioDbName");

                if (String.IsNullOrEmpty(strBiblioDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strBiblioDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "ɾ��С��Ŀ�� '" + strBiblioDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }

            }


            // ���߿�
            nodes = cfg_dom.DocumentElement.SelectNodes("readerdbgroup/database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strDbName = DomUtil.GetAttr(node, "name");

                if (String.IsNullOrEmpty(strDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "ɾ�����߿� '" + strDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }
            }


            // ԤԼ������п�
            XmlNode arrived_node = cfg_dom.DocumentElement.SelectSingleNode("arrived");
            if (arrived_node != null)
            {
                string strArrivedDbName = DomUtil.GetAttr(arrived_node, "dbname");
                if (String.IsNullOrEmpty(strArrivedDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strArrivedDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "ɾ��ԤԼ����� '" + strArrivedDbName + "' ������ʱ��������" + strTempError + "; ";
                    }

                }
            }

            // ΥԼ���
            XmlNode amerce_node = cfg_dom.DocumentElement.SelectSingleNode("amerce");
            if (amerce_node != null)
            {
                string strAmerceDbName = DomUtil.GetAttr(amerce_node, "dbname");
                if (String.IsNullOrEmpty(strAmerceDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strAmerceDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "ɾ��ΥԼ��� '" + strAmerceDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }
            }

            // ��Ʊ��
            XmlNode invoice_node = cfg_dom.DocumentElement.SelectSingleNode("invoice");
            if (invoice_node != null)
            {
                string strInvoiceDbName = DomUtil.GetAttr(amerce_node, "dbname");
                if (String.IsNullOrEmpty(strInvoiceDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strInvoiceDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "ɾ����Ʊ�� '" + strInvoiceDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }
            }

            // ��Ϣ��
            XmlNode message_node = cfg_dom.DocumentElement.SelectSingleNode("message");
            if (message_node != null)
            {
                string strMessageDbName = DomUtil.GetAttr(message_node, "dbname");
                if (String.IsNullOrEmpty(strMessageDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strMessageDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "ɾ����Ϣ�� '" + strMessageDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }
            }

            // ʵ�ÿ�
            nodes = cfg_dom.DocumentElement.SelectNodes("utilDb/database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strDbName = DomUtil.GetAttr(node, "name");
                string strType = DomUtil.GetAttr(node, "type");
                if (String.IsNullOrEmpty(strDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "ɾ������Ϊ " + strType + " ��ʵ�ÿ� '" + strDbName + "' ������ʱ��������" + strTempError + "; ";
                    }
                }
            }


            if (String.IsNullOrEmpty(strError) == false)
                return -1;

            return 0;
        }
    }
}
