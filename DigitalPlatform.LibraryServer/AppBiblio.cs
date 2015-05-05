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
    /// �������Ǳ�Ŀҵ����صĴ���
    /// </summary>
    public partial class LibraryApplication
    {
        public const int QUOTA_SIZE = (int)((double)(1024 * 1024) * (double)0.8);   // �������� 0.5 �������� ��Ϊ�ַ�������Ϊ byte �������ĵ�Ե��

        // �Ƿ�����ɾ�������¼���¼����Ŀ��¼
        public bool DeleteBiblioSubRecords = true;

        // ���Ŀ���¼·����998$t
        // return:
        //      -1  error
        //      0   OK
        public static int GetTargetRecPath(string strBiblioXml,
            out string strTargetRecPath,
            out string strError)
        {
            strError = "";
            strTargetRecPath = "";

            XmlDocument bibliodom = new XmlDocument();
            try
            {
                bibliodom.LoadXml(strBiblioXml);
            }
            catch (Exception ex)
            {
                strError = "��Ŀ��¼XMLװ�ص�DOM����:" + ex.Message;
                return -1;
            }

            XmlNamespaceManager mngr = new XmlNamespaceManager(new NameTable());
            mngr.AddNamespace("dprms", DpNs.dprms);

            XmlNode node = null;
            string strXPath = "";
            if (bibliodom.DocumentElement.NamespaceURI == Ns.usmarcxml)
            {
                mngr.AddNamespace("usmarc", Ns.usmarcxml);	// "http://www.loc.gov/MARC21/slim"


                strXPath = "//usmarc:record/usmarc:datafield[@tag='998']/usmarc:subfield[@code='t']";

                // string d = "";

                node = bibliodom.SelectSingleNode(strXPath, mngr);
                if (node != null)
                    strTargetRecPath = node.InnerText;

                return 1;
            }
            else if (bibliodom.DocumentElement.NamespaceURI == DpNs.unimarcxml)
            {
                mngr.AddNamespace("unimarc", DpNs.unimarcxml);	// "http://dp2003.com/UNIMARC"
                strXPath = "//unimarc:record/unimarc:datafield[@tag='998']/unimarc:subfield[@code='t']";

                node = bibliodom.SelectSingleNode(strXPath, mngr);
                if (node != null)
                    strTargetRecPath = node.InnerText;

                return 1;
            }
            else
            {
                strError = "�޷�ʶ���MARC��ʽ";
                return -1;
            }

            // return 1;
        }

        // ������ǰ�� library.xml �ڽű�
        public LibraryServerResult GetBiblioInfos(
    SessionInfo sessioninfo,
    string strBiblioRecPath,
    string[] formats,
    out string[] results,
    out byte[] timestamp)
        {
            return GetBiblioInfos(sessioninfo,
                strBiblioRecPath,
                "",
                formats,
                out results,
                out timestamp);
        }

        // �����Ŀ��Ϣ
        // �����ö��ָ�ʽ��xml html text @??? summary outputpath
        // TODO: ��������������strBiblioRecPath��������ּ�����ڵ��������ȷ�˵����ʹ��itembarcode��itemconfirmpath(������excludebibliopath)���������λ�֡���������ȫ����ȡ��ԭ��GetBiblioSummary API�Ĺ���
        // parameters:
        //      strBiblioRecPath    �ּ�¼·���������������"$prev" "$next"����ʾǰһ�����һ����
        //      formats     ϣ�������Ϣ�����ɸ�ʽ����� == null����ʾϣ��ֻ����timestamp (results���ؿ�)
        // Result.Value -1���� 0û���ҵ� 1�ҵ�
        public LibraryServerResult GetBiblioInfos(
            SessionInfo sessioninfo,
            string strBiblioRecPath,
            string strBiblioXmlParam,    // 2013/3/6
            string[] formats,
            out string[] results,
            out byte[] timestamp)
        {
            results = null;
            timestamp = null;

            LibraryServerResult result = new LibraryServerResult();

            int nRet = 0;
            long lRet = 0;
            string strError = "";

            if (String.IsNullOrEmpty(strBiblioRecPath) == true)
            {
                strError = "strBiblioRecPath��������Ϊ��";
                goto ERROR1;
            }

            // ����ض���ʽ��Ȩ��
            if (formats != null)
            {
                foreach (string format in formats)
                {
                    if (String.Compare(format, "summary", true) == 0)
                    {
                        // Ȩ���ַ���
                        if (StringUtil.IsInList("getbibliosummary", sessioninfo.RightsOrigin) == false
                            && StringUtil.IsInList("order", sessioninfo.RightsOrigin) == false)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "��ȡ��ժҪ��Ϣ���ܾ������߱�order��getbibliosummaryȨ�ޡ�";
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }
                    }
                }
            }

            List<string> commands = new List<string>();
            List<string> biblio_records = new List<string>();

            if (StringUtil.HasHead(strBiblioRecPath, "@path-list:") == true)
            {
                string strText = strBiblioRecPath.Substring("@path-list:".Length);
                commands = StringUtil.SplitList(strText);

                // ���ǰ�˷�����¼����Ҫ�и�Ϊ�������ַ���
                if (string.IsNullOrEmpty(strBiblioXmlParam) == false)
                {
                    biblio_records = StringUtil.SplitList(strBiblioXmlParam.Replace("<!-->", new string((char)0x01, 1)), (char)0x01);
                    if (commands.Count != biblio_records.Count)
                    {
                        strError = "strBiblioXml �����а������Ӵ����� "+biblio_records.Count.ToString()+" �� strBiblioRecPath �а�����¼·���Ӵ����� "+commands.Count.ToString()+" Ӧ����ȲŶ�";
                        goto ERROR1;
                    }
                }
            }
            else
            {
                commands.Add(strBiblioRecPath);
            }

            int nPackageSize = 0;   // ����ͨѶ���ĳߴ�

            List<String> result_strings = new List<string>();
            string strErrorText = "";
            foreach (string command in commands)
            {
                if (string.IsNullOrEmpty(command) == true)
                    continue;

                string strOutputPath = "";
                string strCurrentBiblioRecPath = "";

                // ������ݿ�·���������ǲ����Ѿ����涨��ı�Ŀ�⣿


                // ����������
                string strCommand = "";
                nRet = command.IndexOf("$");
                if (nRet != -1)
                {
                    strCommand = command.Substring(nRet + 1);
                    strCurrentBiblioRecPath = command.Substring(0, nRet);
                }
                else
                    strCurrentBiblioRecPath = command;

                string strBiblioDbName = ResPath.GetDbName(strCurrentBiblioRecPath);

                if (IsBiblioDbName(strBiblioDbName) == false)
                {
                    strError = "��Ŀ��¼·�� '" + strCurrentBiblioRecPath + "' �а��������ݿ��� '" + strBiblioDbName + "' ���ǺϷ�����Ŀ����";
                    goto ERROR1;
                }

                string strAccessParameters = "";
                bool bRightVerified = false;
                // ����ȡȨ��
                if (String.IsNullOrEmpty(sessioninfo.Access) == false)
                {
                    string strAction = "*";

                    // return:
                    //      null    ָ���Ĳ������͵�Ȩ��û�ж���
                    //      ""      ������ָ�����͵Ĳ���Ȩ�ޣ����Ƿ񶨵Ķ���
                    //      ����      Ȩ���б�* ��ʾͨ���Ȩ���б�
                    string strActionList = LibraryApplication.GetDbOperRights(sessioninfo.Access,
                        strBiblioDbName,
                        "getbiblioinfo");
                    if (strActionList == null)
                    {
                        if (LibraryApplication.GetDbOperRights(sessioninfo.Access,
                            "",
                            "getbiblioinfo") != null)
                        {
                            strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strBiblioDbName + "' ִ�� getbiblioinfo �����Ĵ�ȡȨ��";
                            result.Value = -1;
                            result.ErrorInfo = strError;
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }
                        else
                        {
                            // û�ж����κ� getbiblioinfo �Ĵ�ȡ����(��Ȼ���������������Ĵ�ȡ����)
                            // ��ʱӦ��ת��ȥ����ͨ��Ȩ��
                            // TODO: �����㷨���ٶȽ���
                            goto VERIFY_NORMAL_RIGHTS;
                        }
                    }
                    if (strActionList == "*")
                    {
                        // ͨ��
                    }
                    else
                    {
                        if (IsInAccessList(strAction, strActionList, out strAccessParameters) == false)
                        {
                            strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strBiblioDbName + "' ִ�� getbiblioinfo �����Ĵ�ȡȨ��";
                            result.Value = -1;
                            result.ErrorInfo = strError;
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }
                    }
                    bRightVerified = true;
                }

            VERIFY_NORMAL_RIGHTS:
                if (bRightVerified == false)
                {
                    // Ȩ���ַ���
                    if (StringUtil.IsInList("getbiblioinfo", sessioninfo.RightsOrigin) == false
                        && StringUtil.IsInList("order", sessioninfo.RightsOrigin) == false)
                    {
                        result.Value = -1;
                        result.ErrorInfo = "��ȡ��Ŀ��Ϣ���ܾ������߱�order��getbiblioinfoȨ�ޡ�";
                        result.ErrorCode = ErrorCode.AccessDenied;
                        return result;
                    }
                }

                // ����ض���ʽ��Ȩ��
                if (formats != null)
                {
                    foreach (string format in formats)
                    {
                        if (String.Compare(format, "summary", true) == 0)
                        {
                            // ����ȡȨ��
                            if (String.IsNullOrEmpty(sessioninfo.Access) == false)
                            {
                                string strAction = "*";

                                // return:
                                //      null    ָ���Ĳ������͵�Ȩ��û�ж���
                                //      ""      ������ָ�����͵Ĳ���Ȩ�ޣ����Ƿ񶨵Ķ���
                                //      ����      Ȩ���б�* ��ʾͨ���Ȩ���б�
                                string strActionList = LibraryApplication.GetDbOperRights(sessioninfo.Access,
                                    strBiblioDbName,
                                    "getbibliosummary");
                                if (strActionList == null)
                                {
                                    if (LibraryApplication.GetDbOperRights(sessioninfo.Access,
                                        "",
                                        "getbibliosummary") != null)
                                    {
                                        strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strBiblioDbName + "' ִ�� getbibliosummary �����Ĵ�ȡȨ��";
                                        result.Value = -1;
                                        result.ErrorInfo = strError;
                                        result.ErrorCode = ErrorCode.AccessDenied;
                                        return result;
                                    }
                                    else
                                    {
                                        // û�ж����κ� getbibliosummary �Ĵ�ȡ����(��Ȼ���������������Ĵ�ȡ����)
                                        // ��ʱӦ��ת��ȥ����ͨ��Ȩ��
                                        // TODO: �����㷨���ٶȽ���
                                        strActionList = "*";    // Ϊ���ܹ�ͨ����֤
                                    }
                                }
                                if (strActionList == "*")
                                {
                                    // ͨ��
                                }
                                else
                                {
                                    if (IsInAccessList(strAction, strActionList, out strAccessParameters) == false)
                                    {
                                        strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strBiblioDbName + "' ִ�� getbibliosummary �����Ĵ�ȡȨ��";
                                        result.Value = -1;
                                        result.ErrorInfo = strError;
                                        result.ErrorCode = ErrorCode.AccessDenied;
                                        return result;
                                    }
                                }
                            }
                        }
                    }
                }

                string strBiblioXml = "";
                string strMetaData = "";
                if (String.IsNullOrEmpty(strBiblioXmlParam) == false)
                {
                    // ǰ���Ѿ����͹���һ����¼

                    if (commands.Count > 1)
                    {
                        // ǰ�˷�������Ŀ��¼�Ƕ����¼����̬
                        int index = commands.IndexOf(command);
                        strBiblioXml = biblio_records[index];
                    }
                    else
                        strBiblioXml = strBiblioXmlParam;
                }
                else
                {

                    RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                    if (channel == null)
                    {
                        strError = "channel == null";
                        goto ERROR1;
                    }

                    string strStyle = "timestamp,outputpath";  // "metadata,timestamp,outputpath";

                    if (formats != null && formats.Length > 0)
                        strStyle += ",content,data";

                    string strSearchRecPath = strCurrentBiblioRecPath;  // ����ʵ�ʼ�����·����������ʾ
                    if (String.IsNullOrEmpty(strCommand) == false
                        && (strCommand == "prev" || strCommand == "next"))
                    {
                        strStyle += "," + strCommand;
                        strSearchRecPath += "," + strCommand;
                    }

                    lRet = channel.GetRes(strCurrentBiblioRecPath,
                        strStyle,
                        out strBiblioXml,
                        out strMetaData,
                        out timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.NotFound)
                        {
                            if (commands.Count == 1)
                            {
                                result.Value = 0;
                                result.ErrorCode = ErrorCode.NotFound;  // 2009/8/8 new add
                                result.ErrorInfo = "��Ŀ��¼ '" + strSearchRecPath + "' ������";  // 2009/8/8 new add
                                return result;
                            }
                            // ������ַ���
                            if (formats != null)
                            {
                                for (int i = 0; i < formats.Length; i++)
                                {
                                    result_strings.Add("");
                                }
                                // strErrorText += "��Ŀ��¼ '" + strCurrentBiblioRecPath + "' ������;\r\n";
                            }
                            continue;
                        }
                        strError = "�����Ŀ��¼ '" + strSearchRecPath + "' ʱ����: " + strError;
                        goto ERROR1;
                    }

                    // 2014/12/16
                    strCurrentBiblioRecPath = strOutputPath;

                    // 2013/3/6
                    // �����ֶ�����
                    if (string.IsNullOrEmpty(strAccessParameters) == false)
                    {
                        // �����ֶ�Ȩ�޶�����˳����������
                        // return:
                        //      -1  ����
                        //      0   �ɹ�
                        //      1   �в����ֶα��޸Ļ��˳�
                        nRet = FilterBiblioByFieldNameList(
                strAccessParameters,
                ref strBiblioXml,
                out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                }

                if (formats != null)
                {
                    List<string> temp_results = null;
                    nRet = BuildFormats(
                        sessioninfo,
                        strCurrentBiblioRecPath,
                        strBiblioXml,
                        strOutputPath,
                        strMetaData,
                        timestamp,
                        formats,
                        out temp_results,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    strBiblioXml = "";  // ��������ѭ����Ϊ��ǰ�˷������ļ�¼

                    int nSize = GetSize(temp_results);
                    if (nPackageSize > 0 && nPackageSize + nSize >= QUOTA_SIZE)
                        break;  // û�з���ȫ��������ж���

                    nPackageSize += nSize;

                    if (string.IsNullOrEmpty(strError) == false)
                        strErrorText += strError;

                    if (temp_results != null)
                        result_strings.AddRange(temp_results);
                }
            } // end of each command

            // ���Ƶ������
            if (result_strings.Count > 0)
            {
                results = new string[result_strings.Count];
                result_strings.CopyTo(results);

                if (String.IsNullOrEmpty(strErrorText) == false)
                {
                    // ͳһ����
                    // strError = strErrorText;
                    // goto ERROR1;
                    result.ErrorInfo = strError;    // 2014/1/8
                }
            }

            result.Value = 1;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        static int GetSize(List<string> list)
        {
            int nSize = 0;
            foreach (string s in list)
            {
                nSize += 100;   // ��װ���Ϲ���
                if (s != null)
                    nSize += s.Length;
            }

            return nSize;
        }

        int BuildFormats(
            SessionInfo sessioninfo,
            string strCurrentBiblioRecPath,
            string strBiblioXml,
            string strOutputPath,   // ��¼��·��
            string strMetadata,     // ��¼��metadata
            byte [] timestamp,
            string[] formats,
            out List<String> result_strings,
            out string strErrorText)
        {
            strErrorText = "";
            result_strings = new List<string>();
            string strError = "";
            int nRet = 0;

            string strBiblioDbName = ResPath.GetDbName(strCurrentBiblioRecPath);

            for (int i = 0; i < formats.Length; i++)
            {
                string strBiblioType = formats[i];
                string strBiblio = "";

                // ����ֻ���ȡ�ֲ�����
                if (strBiblioType[0] == '@')
                {
                    if (String.IsNullOrEmpty(strBiblioXml) == true)
                    {
                        strBiblio = ""; //  "XML��¼Ϊ��";
                        goto CONTINUE;
                    }

                    string strPartName = strBiblioType.Substring(1);

                    XmlDocument bibliodom = new XmlDocument();

                    try
                    {
                        bibliodom.LoadXml(strBiblioXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "��XMLװ��DOMʱʧ��: " + ex.Message;
                        // goto ERROR1;
                        if (String.IsNullOrEmpty(strErrorText) == false)
                            strErrorText += ";\r\n";
                        strErrorText += strError;
                        goto CONTINUE;
                    }
                    int nResultValue = 0;

                    // ִ�нű�����GetBiblioPart
                    // parameters:
                    // return:
                    //      -2  not found script
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = this.DoGetBiblioPartScriptFunction(
                        bibliodom,
                        strPartName,
                        out nResultValue,
                        out strBiblio,
                        out strError);
                    if (nRet == -1 || nRet == -2)
                    {
                        strError = "�����Ŀ��¼ '" + strCurrentBiblioRecPath + "' �ľֲ� " + strBiblioType + " ʱ����: " + strError;
                        // goto ERROR1;
                        if (String.IsNullOrEmpty(strErrorText) == false)
                            strErrorText += ";\r\n";
                        strErrorText += strError;
                        goto CONTINUE;
                    }
                }
                // ��ĿժҪ
                else if (String.Compare(strBiblioType, "summary", true) == 0)
                {
                    if (String.IsNullOrEmpty(strBiblioXml) == true)
                    {
                        strBiblio = ""; // "XML��¼Ϊ��";
                        goto CONTINUE;
                    }

                    // ��ñ��������ļ�
                    string strLocalPath = "";

                    string strRemotePath = BrowseFormat.CanonicalizeScriptFileName(
                        ResPath.GetDbName(strCurrentBiblioRecPath),
                        "./cfgs/summary.fltx");

                    nRet = this.CfgsMap.MapFileToLocal(
                        sessioninfo.Channels,
                        strRemotePath,
                        out strLocalPath,
                        out strError);
                    if (nRet == -1)
                    {
                        // goto ERROR1;
                        if (String.IsNullOrEmpty(strErrorText) == false)
                            strErrorText += ";\r\n";
                        strErrorText += strError;
                        goto CONTINUE;
                    }
                    if (nRet == 0)
                    {
                        // ����.fltx�ļ�������, ����̽.cs�ļ�
                        strRemotePath = BrowseFormat.CanonicalizeScriptFileName(
                        ResPath.GetDbName(strCurrentBiblioRecPath),
                        "./cfgs/summary.cs");

                        nRet = this.CfgsMap.MapFileToLocal(
                            sessioninfo.Channels,
                            strRemotePath,
                            out strLocalPath,
                            out strError);
                        if (nRet == -1)
                        {
                            // goto ERROR1;
                            if (String.IsNullOrEmpty(strErrorText) == false)
                                strErrorText += ";\r\n";
                            strErrorText += strError;
                            goto CONTINUE;
                        }
                        if (nRet == 0)
                        {
                            strError = strRemotePath + "������...";
                            // goto ERROR1;
                            if (String.IsNullOrEmpty(strErrorText) == false)
                                strErrorText += ";\r\n";
                            strErrorText += strError;
                            goto CONTINUE;
                        }
                    }

                    bool bFltx = false;
                    // �����һ��.cs�ļ�, ����Ҫ���.cs.ref�����ļ�
                    if (IsCsFileName(strRemotePath) == true)
                    {
                        string strTempPath = "";
                        nRet = this.CfgsMap.MapFileToLocal(
                        sessioninfo.Channels,
                            strRemotePath + ".ref",
                            out strTempPath,
                            out strError);
                        if (nRet == -1)
                        {
                            // goto ERROR1;
                            if (String.IsNullOrEmpty(strErrorText) == false)
                                strErrorText += ";\r\n";
                            strErrorText += strError;
                            goto CONTINUE;
                        }
                        bFltx = false;
                    }
                    else
                    {
                        bFltx = true;
                    }
                    string strSummary = "";

                    // ���ּ�¼���ݴ�XML��ʽת��ΪHTML��ʽ
                    if (string.IsNullOrEmpty(strBiblioXml) == false)
                    {
                        if (bFltx == true)
                        {
                            string strFilterFileName = strLocalPath;
                            nRet = this.ConvertBiblioXmlToHtml(
                                    strFilterFileName,
                                    strBiblioXml,
                                    null,
                                    strCurrentBiblioRecPath,
                                    out strSummary,
                                    out strError);
                        }
                        else
                        {
                            nRet = this.ConvertRecordXmlToHtml(
                                strLocalPath,
                                strLocalPath + ".ref",
                                strBiblioXml,
                                strCurrentBiblioRecPath,   // 2009/10/18 new add
                                out strSummary,
                                out strError);
                        }
                        if (nRet == -1)
                        {
                            // goto ERROR1;
                            if (String.IsNullOrEmpty(strErrorText) == false)
                                strErrorText += ";\r\n";
                            strErrorText += strError;
                            goto CONTINUE;
                        }
                    }
                    else
                        strSummary = "";

                    strBiblio = strSummary;
                }
                // Ŀ���¼·��
                else if (String.Compare(strBiblioType, "targetrecpath", true) == 0)
                {
                    // ���Ŀ���¼·����998$t
                    // return:
                    //      -1  error
                    //      0   OK
                    nRet = GetTargetRecPath(strBiblioXml,
                        out strBiblio,
                        out strError);
                    if (nRet == -1)
                    {
                        // goto ERROR1;
                        if (String.IsNullOrEmpty(strErrorText) == false)
                            strErrorText += ";\r\n";
                        strErrorText += strError;
                        goto CONTINUE;
                    }
                }
                // ���ֻ��Ҫ�ּ�¼��XML��ʽ
                else if (String.Compare(strBiblioType, "xml", true) == 0)
                {
                    strBiblio = strBiblioXml;
                }
                // ģ�ⴴ��������
                else if (String.Compare(strBiblioType, "keys", true) == 0)
                {
                    string strResultXml = "";
                    nRet = GetKeys(sessioninfo,
                        strCurrentBiblioRecPath,
                        strBiblioXml,
                        out strResultXml,
                        out strError);
                    if (nRet == -1)
                    {
                        // goto ERROR1;
                        if (String.IsNullOrEmpty(strErrorText) == false)
                            strErrorText += ";\r\n";
                        strErrorText += strError;
                        goto CONTINUE;
                    }
                    strBiblio = strResultXml;
                }
                    // 2014/3/17
                else if (IsResultType(strBiblioType, "subcount") == true)
                {
                    string strType = "";
                    string strSubType = "";
                    StringUtil.ParseTwoPart(strBiblioType,
                        ":",
                        out strType,
                        out strSubType);

                    RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                    if (channel == null)
                    {
                        strError = "channel == null";
                        if (String.IsNullOrEmpty(strErrorText) == false)
                            strErrorText += ";\r\n";
                        strErrorText += strError;
                        goto CONTINUE;
                    }
                    long lTotalCount = 0;
                    if (strSubType == "item" || string.IsNullOrEmpty(strSubType) == true
                        || strSubType == "all")
                    {
                        // ̽����Ŀ��¼��û��������ʵ���¼(Ҳ˳�㿴��ʵ���¼�����Ƿ�����ͨ��Ϣ)?
                        List<DeleteEntityInfo> entityinfos = null;
                        long lTemp = 0;
                        // return:
                        // return:
                        //      -2  not exist entity dbname
                        //      -1  error
                        //      >=0 ������ͨ��Ϣ��ʵ���¼����, ��strStyle����count_borrow_infoʱ��
                        nRet = SearchChildEntities(channel,
                            strCurrentBiblioRecPath,
                            "only_getcount",
                            (Delegate_checkRecord)null,
                            null,
                            out lTemp,
                            out entityinfos,
                            out strError);
                        if (nRet == -1)
                        {
                            strBiblio = strError;
                            goto CONTINUE;
                        }

                        if (nRet == -2)
                        {
                            Debug.Assert(entityinfos.Count == 0, "");
                        }

                        lTotalCount += lTemp;
                    }
                    if (strSubType == "order" 
                        || string.IsNullOrEmpty(strSubType) == true
                        || strSubType == "all")
                    {
                        // ̽����Ŀ��¼��û�������Ķ�����¼
                        List<DeleteEntityInfo> orderinfos = null;
                        long lTemp = 0;
                        // return:
                        //      -1  error
                        //      0   not exist entity dbname
                        //      1   exist entity dbname
                        nRet = this.OrderItemDatabase.SearchChildItems(channel,
                            strCurrentBiblioRecPath,
                            "only_getcount",
                            out lTemp,
                            out orderinfos,
                            out strError);
                        if (nRet == -1)
                        {
                            strBiblio = strError;
                            goto CONTINUE;
                        }

                        if (nRet == 0)
                        {
                            Debug.Assert(orderinfos.Count == 0, "");
                        }
                        lTotalCount += lTemp;
                    }
                    if (strSubType == "issue"
    || string.IsNullOrEmpty(strSubType) == true
    || strSubType == "all")
                    {
                        // ̽����Ŀ��¼��û���������ڼ�¼
                        List<DeleteEntityInfo> issueinfos = null;
                        long lTemp = 0;

                        // return:
                        //      -1  error
                        //      0   not exist entity dbname
                        //      1   exist entity dbname
                        nRet = this.IssueItemDatabase.SearchChildItems(channel,
                            strCurrentBiblioRecPath,
                            "only_getcount",
                            out lTemp,
                            out issueinfos,
                            out strError);
                        if (nRet == -1)
                        {
                            strBiblio = strError;
                            goto CONTINUE;
                        }

                        if (nRet == 0)
                        {
                            Debug.Assert(issueinfos.Count == 0, "");
                        }
                        lTotalCount += lTemp;
                    }
                    if (strSubType == "comment"
    || string.IsNullOrEmpty(strSubType) == true
    || strSubType == "all")
                    {
                        // ̽����Ŀ��¼��û����������ע��¼
                        List<DeleteEntityInfo> commentinfos = null;
                        long lTemp = 0;
                        // return:
                        //      -1  error
                        //      0   not exist entity dbname
                        //      1   exist entity dbname
                        nRet = this.CommentItemDatabase.SearchChildItems(channel,
                            strCurrentBiblioRecPath,
                            "only_getcount",
                            out lTemp,
                            out commentinfos,
                            out strError);
                        if (nRet == -1)
                        {
                            strBiblio = strError;
                            goto CONTINUE;
                        }

                        if (nRet == 0)
                        {
                            Debug.Assert(commentinfos.Count == 0, "");
                        }

                        lTotalCount += lTemp;
                    }

                    strBiblio = lTotalCount.ToString();
                }
                else if (String.Compare(strBiblioType, "outputpath", true) == 0)
                {
                    strBiblio = strOutputPath;  // 2008/3/18 new add
                }
                else if (String.Compare(strBiblioType, "timestamp", true) == 0)
                {
                    strBiblio = ByteArray.GetHexTimeStampString(timestamp);  // 2013/3/8
                }
                else if (String.Compare(strBiblioType, "metadata", true) == 0)
                {
                    strBiblio = strMetadata;  // 2010/10/27 new add
                }
                else if (String.Compare(strBiblioType, "html", true) == 0)
                {
                    if (String.IsNullOrEmpty(strBiblioXml) == true)
                    {
                        strBiblio = "XML��¼Ϊ��";
                        goto CONTINUE;
                    }

                    // string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
                    // �Ƿ���Ҫ���������ݿ���ȷʵΪ��Ŀ������

                    // ��Ҫ���ں�ӳ������ļ�
                    string strLocalPath = "";
                    nRet = this.MapKernelScriptFile(
                        sessioninfo,
                        strBiblioDbName,
                        "./cfgs/loan_biblio.fltx",
                        out strLocalPath,
                        out strError);
                    if (nRet == -1)
                    {
                        // goto ERROR1;
                        if (String.IsNullOrEmpty(strErrorText) == false)
                            strErrorText += ";\r\n";
                        strErrorText += strError;
                        goto CONTINUE;
                    }


                    // ���ּ�¼���ݴ�XML��ʽת��ΪHTML��ʽ
                    string strFilterFileName = strLocalPath;    // app.CfgDir + "\\biblio.fltx";
                    if (string.IsNullOrEmpty(strBiblioXml) == false)
                    {
                        nRet = this.ConvertBiblioXmlToHtml(
                                strFilterFileName,
                                strBiblioXml,
                                     null,
                               strCurrentBiblioRecPath,
                                out strBiblio,
                                out strError);
                        if (nRet == -1)
                        {
                            // goto ERROR1;

                            if (String.IsNullOrEmpty(strErrorText) == false)
                                strErrorText += ";\r\n";
                            strErrorText += strError;
                            goto CONTINUE;
                        }
                    }
                    else
                        strBiblio = "";
                }
                else if (String.Compare(strBiblioType, "text", true) == 0)
                {
                    if (String.IsNullOrEmpty(strBiblioXml) == true)
                    {
                        strBiblio = "XML��¼Ϊ��";
                        goto CONTINUE;
                    }

                    // string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
                    // �Ƿ���Ҫ���������ݿ���ȷʵΪ��Ŀ������

                    // ��Ҫ���ں�ӳ������ļ�
                    string strLocalPath = "";
                    nRet = this.MapKernelScriptFile(
                        sessioninfo,
                        strBiblioDbName,
                        "./cfgs/loan_biblio_text.fltx",
                        out strLocalPath,
                        out strError);
                    if (nRet == -1)
                    {
                        //goto ERROR1;
                        if (String.IsNullOrEmpty(strErrorText) == false)
                            strErrorText += ";\r\n";
                        strErrorText += strError;
                        goto CONTINUE;
                    }


                    // ���ּ�¼���ݴ�XML��ʽת��Ϊtext��ʽ
                    string strFilterFileName = strLocalPath;    // app.CfgDir + "\\biblio.fltx";

                    if (string.IsNullOrEmpty(strBiblioXml) == false)
                    {
                        nRet = this.ConvertBiblioXmlToHtml(
                            strFilterFileName,
                            strBiblioXml,
                                    null,
                            strCurrentBiblioRecPath,
                            out strBiblio,
                            out strError);
                        if (nRet == -1)
                        {
                            //goto ERROR1;
                            if (String.IsNullOrEmpty(strErrorText) == false)
                                strErrorText += ";\r\n";
                            strErrorText += strError;
                            goto CONTINUE;
                        }
                    }
                    else
                        strBiblio = "";
                }
                else
                {
                    strErrorText = "δ֪����Ŀ��ʽ '" + strBiblioType + "'";
                    return -1;
                }

            CONTINUE:
                result_strings.Add(strBiblio);
            } // end of for

            return 0;
        }

        public int GetKeys(SessionInfo sessioninfo,
            string strRecPath,
            string strXml,
            out string strResultXml,
            out string strError)
        {
            strError = "";
            strResultXml = "";

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }
            List<AccessKeyInfo> keys = null;
            long lRet = channel.DoGetKeys(strRecPath,
                strXml,
                string.IsNullOrEmpty(sessioninfo.Lang) == true ? "zh" : sessioninfo.Lang,
                null,
                out keys,
                out strError);
            if (lRet == -1)
                return -1;

            // ���� XML ����
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            foreach (AccessKeyInfo key in keys)
            {
                XmlNode node = dom.CreateElement("k");
                dom.DocumentElement.AppendChild(node);
                DomUtil.SetAttr(node, "k", key.Key);
                DomUtil.SetAttr(node, "f", key.FromName);
            }

            strResultXml = dom.DocumentElement.OuterXml;
            return 0;
        }

        // ���ͼ��ժҪ��Ϣ
        // ����ʱ����ҪSessionInfo
        public int GetBiblioSummary(string strItemBarcode,
            string strConfirmItemRecPath,
            string strBiblioRecPathExclude,
            int nMaxLength,
            out string strBiblioRecPath,
            out string strSummary,
            out string strError)
        {
            strError = "";
            strSummary = "";
            strBiblioRecPath = "";

            // ��ʱ��SessionInfo����
            SessionInfo sessioninfo = new SessionInfo(this);
            // ģ��һ���˻�
            Account account = new Account();
            account.LoginName = "�ڲ�����";
            account.Password = "";
            account.Rights = "getbibliosummary";

            account.Type = "";
            account.Barcode = "";
            account.Name = "�ڲ�����";
            account.UserID = "�ڲ�����";
            account.RmsUserName = this.ManagerUserName;
            account.RmsPassword = this.ManagerPassword;

            sessioninfo.Account = account;
            try
            {
                RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    return -1;
                }

                LibraryServerResult result = this.GetBiblioSummary(
                    sessioninfo,
                    channel,
                    strItemBarcode,
                    strConfirmItemRecPath,
                    strBiblioRecPathExclude,
                    out strBiblioRecPath,
                    out strSummary);
                if (result.Value == -1)
                {
                    strError = result.ErrorInfo;
                    return -1;
                }
                else
                {
                    if (nMaxLength != -1)
                    {
                        // �ض�
                        if (strSummary.Length > nMaxLength)
                            strSummary = strSummary.Substring(0, nMaxLength) + "...";
                    }
                }
            }
            finally
            {
                sessioninfo.CloseSession();
                sessioninfo = null;
            }

            return 0;
        }

        // �Ӳ������(+���¼·��)����ּ�¼ժҪ�����ߴӶ�����¼·�����ڼ�¼·������ע��¼·������ּ�¼ժҪ
        // Ȩ��:   ��Ҫ����getbibliosummaryȨ��
        // parameters:
        //      strBiblioRecPathExclude   �����б��е���Щ��·��, �ŷ���ժҪ����, �������������·������
        public LibraryServerResult GetBiblioSummary(
            SessionInfo sessioninfo,
            RmsChannel channel,
            string strItemBarcode,
            string strConfirmItemRecPath,
            string strBiblioRecPathExclude,
            out string strBiblioRecPath,
            out string strSummary)
        {
            strBiblioRecPath = "";
            strSummary = "";
            string strError = "";

            LibraryServerResult result = new LibraryServerResult();

            // Ȩ���ж�
            // Ȩ���ַ���
            if (StringUtil.IsInList("getbibliosummary", sessioninfo.RightsOrigin) == false
                && StringUtil.IsInList("order", sessioninfo.RightsOrigin) == false)
            {
                result.Value = -1;
                result.ErrorInfo = "��ȡ��ժҪ��Ϣ���ܾ������߱�order��getbibliosummaryȨ�ޡ�";
                result.ErrorCode = ErrorCode.AccessDenied;
                return result;
            }

            int nRet = 0;
            long lRet = 0;

            if (string.IsNullOrEmpty(strItemBarcode) == true
                && string.IsNullOrEmpty(strConfirmItemRecPath) == true)
            {
                strError = "strItemBarcode��strConfirmItemRecPath����ֵ����ͬʱΪ��";
                goto ERROR1;
            }

            string strItemXml = "";
            string strOutputItemPath = "";
            string strMetaData = "";

            /*
            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "channel == null";
                goto ERROR1;
            }
             * */


            // ���������ͨ����·��
            string strHead = "@bibliorecpath:";
            if (strItemBarcode.Length > strHead.Length
                && strItemBarcode.Substring(0, strHead.Length) == strHead)
            {
                strBiblioRecPath = strItemBarcode.Substring(strHead.Length);

                // �����Ŀ�����Ƿ�Ϸ�
                string strTempBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
                if (this.IsBiblioDbName(strTempBiblioDbName) == false)
                {
                    strError = "strItemBarcode��������������Ŀ��·�� '" + strBiblioRecPath + "' �У���Ŀ���� '" + strTempBiblioDbName + "' ����ϵͳ�������Ŀ����";
                    goto ERROR1;
                }
                goto LOADBIBLIO;
            }


            bool bByRecPath = false;    // �Ƿ񾭹���¼·������ȡ�ģ�

            if (string.IsNullOrEmpty(strItemBarcode) == false)
            {
                // ��ò��¼
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                nRet = this.GetItemRecXml(
                    channel,
                    strItemBarcode,
                    out strItemXml,
                    out strOutputItemPath,
                    out strError);
                if (nRet == 0)
                {
                    result.Value = 0;
                    result.ErrorInfo = "���¼û���ҵ�";
                    result.ErrorCode = ErrorCode.NotFound;
                    return result;
                }

                if (nRet == -1)
                    goto ERROR1;
            }

            // ������ж���һ��(����û�������)�������Ѿ���ȷ���Ĳ��¼·�������ж�
            if (string.IsNullOrEmpty(strItemBarcode) == true
                ||
                (nRet > 1 && String.IsNullOrEmpty(strConfirmItemRecPath) == false))
            {
                // ���·���еĿ������ǲ���ʵ��⡢�����⡢�ڿ⡢��ע����
                nRet = CheckRecPath(strConfirmItemRecPath,
                    "item,order,issue,comment",
                    out strError);
                if (nRet != 1)
                    goto ERROR1;

                byte[] item_timestamp = null;

                lRet = channel.GetRes(strConfirmItemRecPath,
                    out strItemXml,
                    out strMetaData,
                    out item_timestamp,
                    out strOutputItemPath,
                    out strError);
                if (lRet == -1)
                {
                    strError = "����strConfirmItemRecPath '" + strConfirmItemRecPath + "' ��ü�¼ʧ��: " + strError;
                    goto ERROR1;
                }

                bByRecPath = true;
            }

            // �Ӳ��¼�л�ô�������id
            string strBiblioRecID = "";

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strItemXml);
            }
            catch (Exception ex)
            {
                strError = "���¼XMLװ�ص�DOM����:" + ex.Message;
                goto ERROR1;
            }

            if (bByRecPath == true
                && string.IsNullOrEmpty(strItemBarcode) == false)   // 2011/9/6
            {
                // ���������Ҫ��ʵ�������
                string strTempItemBarcode = DomUtil.GetElementText(dom.DocumentElement,
                    "//barcode");
                if (strTempItemBarcode != strItemBarcode)
                {
                    strError = "ͨ��������� '" + strItemBarcode + "' ��ȡʵ���¼�������ж�����Ȼ���Զ��ü�¼·�� '" + strConfirmItemRecPath + "' ����ȡʵ���¼����Ȼ��ȡ�ɹ������Ƿ�������ȡ�ļ�¼��<barcode>Ԫ���еĲ������ '" + strTempItemBarcode + "' ������Ҫ��Ĳ������ '" + strItemBarcode + "��(����)�����������������ʵ���¼�������ƶ���ɵġ�";
                    goto ERROR1;
                }
            }

            strBiblioRecID = DomUtil.GetElementText(dom.DocumentElement, "parent"); //
            if (String.IsNullOrEmpty(strBiblioRecID) == true)
            {
                strError = "��������¼XML��<parent>Ԫ��ȱ������ֵΪ��, ����޷���λ�ּ�¼";
                goto ERROR1;
            }

            // �������ļ��л�ú�ʵ����Ӧ����Ŀ����

            /*
            // ׼������: ӳ�����ݿ���
            nRet = this.GetGlobalCfg(sessioninfo.Channels,
                out strError);
            if (nRet == -1)
                goto ERROR1;
             * */

            string strItemDbName = ResPath.GetDbName(strOutputItemPath);
            string strBiblioDbName = "";

            // ������Ŀ��������, �ҵ���Ӧ����Ŀ����
            // return:
            //      -1  ����
            //      0   û���ҵ�
            //      1   �ҵ�
            nRet = this.GetBiblioDbNameByChildDbName(strItemDbName,
                out strBiblioDbName,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                strError = "�������� '" + strItemDbName + "' û���ҵ�����������Ŀ����";
                goto ERROR1;
            }

            string strBiblioXml = "";
            strBiblioRecPath = strBiblioDbName + "/" + strBiblioRecID;

        LOADBIBLIO:

            // �����Ƿ����ų��б���
            if (String.IsNullOrEmpty(strBiblioRecPathExclude) == false
                && IsInBarcodeList(strBiblioRecPath,
                strBiblioRecPathExclude) == true)
            {
                result.Value = 1;
                return result;
            }

            /*
strSummary = "";
result.Value = 1;
return result;
 * */

            // ��ñ��������ļ�
            string strLocalPath = "";

            string strRemotePath = BrowseFormat.CanonicalizeScriptFileName(
                ResPath.GetDbName(strBiblioRecPath),
                "./cfgs/summary.fltx");

            nRet = this.CfgsMap.MapFileToLocal(
                sessioninfo.Channels,
                strRemotePath,
                out strLocalPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                // ����.fltx�ļ�������, ����̽.cs�ļ�
                strRemotePath = BrowseFormat.CanonicalizeScriptFileName(
                ResPath.GetDbName(strBiblioRecPath),
                "./cfgs/summary.cs");

                nRet = this.CfgsMap.MapFileToLocal(
                    sessioninfo.Channels,
                    strRemotePath,
                    out strLocalPath,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    strError = strRemotePath + "������...";
                    goto ERROR1;
                }
            }

            bool bFltx = false;
            // �����һ��.cs�ļ�, ����Ҫ���.cs.ref�����ļ�
            if (IsCsFileName(strRemotePath) == true)
            {
                string strTempPath = "";
                nRet = this.CfgsMap.MapFileToLocal(
                sessioninfo.Channels,
                    strRemotePath + ".ref",
                    out strTempPath,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                bFltx = false;
            }
            else
            {
                bFltx = true;
            }



            // ȡ���ּ�¼
            byte[] timestamp = null;
            lRet = channel.GetRes(strBiblioRecPath,
                out strBiblioXml,
                out strMetaData,
                out timestamp,
                out strOutputItemPath,
                out strError);
            if (lRet == -1)
            {
                strError = "����ּ�¼ '" + strBiblioRecPath + "' ʱ����: " + strError;
                goto ERROR1;
            }

                string strMarc = "";
                string strMarcSyntax = "";
            {
                // ת��ΪMARC��ʽ

                // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
                // parameters:
                //		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
                //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
                //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
                nRet = MarcUtil.Xml2Marc(strBiblioXml,
                    true,
                    "", // this.CurMarcSyntax,
                    out strMarcSyntax,
                    out strMarc,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            string strFragment = "";
            if (StringUtil.IsInList("coverimage", strBiblioRecPathExclude) == true)
            {
                // ��÷���ͼ�� URL
                string strImageUrl = ScriptUtil.GetCoverImageUrl(strMarc, "SmallImage");
                if (string.IsNullOrEmpty(strImageUrl) == false)
                {
                    if (StringUtil.HasHead(strImageUrl, "uri:") == true)
                    {
                        strImageUrl = "object-path:" + strBiblioRecPath + "/object/" + strImageUrl.Substring(4);
                        strFragment = "<img class='biblio pending' name='" + strImageUrl + "'/>";
                    }
                    else
                    {
                        strFragment = "<img class='biblio' src='" + strImageUrl + "'/>";
                    }
                }
            }

            // ���ּ�¼���ݴ�XML��ʽת��ΪHTML��ʽ
            if (string.IsNullOrEmpty(strBiblioXml) == false)
            {
                if (bFltx == true)
                {
                    string strFilterFileName = strLocalPath;
                    nRet = this.ConvertBiblioXmlToHtml(
                        strFilterFileName,
                            strMarc,    // strBiblioXml,
                            strMarcSyntax,
                            strBiblioRecPath,
                            out strSummary,
                            out strError);
                }
                else
                {
                    nRet = this.ConvertRecordXmlToHtml(
                        strLocalPath,
                        strLocalPath + ".ref",
                        strBiblioXml,
                        strBiblioRecPath,   // 2009/10/18 new add
                        out strSummary,
                        out strError);
                }
                if (nRet == -1)
                    goto ERROR1;

            }
            else
                strSummary = "";

            strSummary = strFragment + strSummary;

            result.Value = 1;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }


        // ̽��MARC��ʽ
        // return:
        //      -1  error
        //      0   �޷�̽��
        //      1   ̽�⵽��
        static int DetectMarcSyntax(XmlDocument dom,
            out string strMarcSyntax,
            out string strError)
        {
            strMarcSyntax = "";
            strError = "";

            // ȡMARC�� �� ȡ��marc syntax
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("unimarc", Ns.unimarcxml);
            nsmgr.AddNamespace("usmarc", Ns.usmarcxml);

            XmlNode root_new = null;
            // '//'��֤������MARC�ĸ��ںδ�������������ȡ����
            root_new = dom.DocumentElement.SelectSingleNode("//unimarc:record",
                nsmgr);
            if (root_new == null)
            {
                root_new = dom.DocumentElement.SelectSingleNode("//usmarc:record",
                    nsmgr);

                if (root_new == null)
                {
                    return 0;   // �޷�̽�⵽
                }

                strMarcSyntax = "usmarc";
            }
            else
            {
                strMarcSyntax = "unimarc";
            }

            Debug.Assert(strMarcSyntax != "", "");
            return 1;
        }

        // �����Ŀ��¼�Ĵ�����
        // return:
        //      -1  ����
        //      0   û���ҵ� 998$z���ֶ�
        //      1   �ҵ�
        static int GetBiblioOwner(string strXml,
            out string strOwner,
            out string strError)
        {
            strError = "";
            strOwner = "";

            XmlDocument domNew = new XmlDocument();
            try
            {
                domNew.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XML�ַ���װ��DOMʱ����: " + ex.Message;
                return -1;
            }

            string strMarcSyntax = "";
            int nRet = DetectMarcSyntax(domNew,
    out strMarcSyntax,
    out strError);
            if (nRet == -1)
                return -1;

            // ȡMARC�� �� ȡ��marc syntax
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("unimarc", Ns.unimarcxml);
            nsmgr.AddNamespace("usmarc", Ns.usmarcxml);

            string strXPath = "";

            if (strMarcSyntax == "unimarc")
                strXPath = "//unimarc:record/unimarc:datafield[@tag='998']/unimarc:subfield[@code='z']";
            else
                strXPath = "//usmarc:record/usmarc:datafield[@tag='998']/usmarc:subfield[@code='z']";

            XmlNode node = domNew.DocumentElement.SelectSingleNode(strXPath, nsmgr);

            if (node == null)
                return 0;   // û���ҵ�

            strOwner = node.InnerText.Trim();
            return 1;
        }

        // �ϲ����ϱ�Ŀ���¾���Ŀ��XML��¼
        // ���ܣ��ų��¼�¼�ж�strLibraryCode���������905�ֶε��޸�
        // parameters:
        //      bChangePartDenied   ������α��趨Ϊ true���� strError �з����˹��ڲ����޸ĵ�ע����Ϣ
        // return:
        //      -1  error
        //      0   new record not changed
        //      1   new record changed
        int MergeOldNewBiblioRec(
            string strRights,
            string strUnionCatalogStyle,
            string strLibraryCode,
            string strDefaultOperation,
            string strFieldNameList,
            string strOldBiblioXml,
            ref string strNewBiblioXml,
            ref bool bChangePartDeniedParam,
            out string strError)
        {
            strError = "";
            string strNewSave = strNewBiblioXml;

            string strComment = "";
            bool bChangePartDenied = false;

            try
            {

                if (string.IsNullOrEmpty(strFieldNameList) == false)
                {
                    // return:
                    //      -1  ����
                    //      0   �ɹ�
                    //      1   �в����޸�Ҫ�󱻾ܾ���strError �з�����ע����Ϣ
                    int nRet = MergeOldNewBiblioByFieldNameList(
                        strDefaultOperation,
                        strFieldNameList,
                        strOldBiblioXml,
                        ref strNewBiblioXml,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 1)
                    {
                        bChangePartDenied = true;
                        strComment = strError;
                    }

                    strError = "";
                }

                XmlDocument domNew = new XmlDocument();
                if (String.IsNullOrEmpty(strNewBiblioXml) == true)
                    strNewBiblioXml = "<root />";
                try
                {
                    domNew.LoadXml(strNewBiblioXml);
                }
                catch (Exception ex)
                {
                    strError = "strNewBiblioXmlװ��XMLDOMʱ����: " + ex.Message;
                    return -1;
                }

                // string strNewSave = domNew.OuterXml;

                XmlDocument domOld = new XmlDocument();
                if (String.IsNullOrEmpty(strOldBiblioXml) == true
                    || (string.IsNullOrEmpty(strOldBiblioXml) == false && strOldBiblioXml.Length == 1))
                    strOldBiblioXml = "<root />";
                try
                {
                    domOld.LoadXml(strOldBiblioXml);
                }
                catch (Exception ex)
                {
                    strError = "strOldBiblioXmlװ��XMLDOMʱ����: " + ex.Message;
                    return -1;
                }

                // ȷ��<operations>Ԫ�ر����������׿���
                {
                    // ɾ��new�е�ȫ��<operations>Ԫ�أ�Ȼ��old��¼�е�ȫ��<operations>Ԫ�ز��뵽new��¼��

                    // ɾ��new�е�ȫ��<operations>Ԫ��
                    XmlNodeList nodes = domNew.DocumentElement.SelectNodes("//operations");
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        XmlNode node = nodes[i];
                        if (node.ParentNode != null)
                            node.ParentNode.RemoveChild(node);
                    }

                    // Ȼ��old��¼�е�ȫ��<operations>Ԫ�ز��뵽new��¼��
                    nodes = domOld.DocumentElement.SelectNodes("//operations");
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        XmlNode node = nodes[i];

                        XmlDocumentFragment fragment = domNew.CreateDocumentFragment();
                        fragment.InnerXml = node.OuterXml;

                        domNew.DocumentElement.AppendChild(fragment);
                    }
                }


                // ������߱�writeobjectsȨ��
                if (StringUtil.IsInList("writeobject", strRights) == false)
                {
                    // TODO: ��MergeDprmsFile()�������������� 

                    // ɾ��new�е�ȫ��<dprms:file>Ԫ�أ�Ȼ��old��¼�е�ȫ��<dprms:file>Ԫ�ز��뵽new��¼��

                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
                    nsmgr.AddNamespace("dprms", DpNs.dprms);

                    // ɾ��new�е�ȫ��<dprms:file>Ԫ��
                    XmlNodeList nodes = domNew.DocumentElement.SelectNodes("//dprms:file", nsmgr);
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        XmlNode node = nodes[i];
                        if (node.ParentNode != null)
                            node.ParentNode.RemoveChild(node);
                    }

                    // Ȼ��old��¼�е�ȫ��<dprms:file>Ԫ�ز��뵽new��¼��
                    nodes = domOld.DocumentElement.SelectNodes("//dprms:file", nsmgr);
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        XmlNode node = nodes[i];

                        XmlDocumentFragment fragment = domNew.CreateDocumentFragment();
                        fragment.InnerXml = node.OuterXml;

                        domNew.DocumentElement.AppendChild(fragment);
                    }
                }


                if (StringUtil.IsInList("905", strUnionCatalogStyle) == true)
                {
                    // *�ű�ʾ��Ȩ������ȫ���ݴ����905
                    if (strLibraryCode == "*")
                    {
                        strNewBiblioXml = domNew.OuterXml;

                        if (strNewSave == strNewBiblioXml)
                            return 0;

                        return 1;
                    }

                    string strMarcSyntax = "";

                    // ȡMARC�� �� ȡ��marc syntax
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
                    nsmgr.AddNamespace("unimarc", Ns.unimarcxml);
                    nsmgr.AddNamespace("usmarc", Ns.usmarcxml);

                    XmlNode root_new = null;
                    if (strMarcSyntax == "")
                    {
                        // '//'��֤������MARC�ĸ��ںδ�������������ȡ����
                        root_new = domNew.DocumentElement.SelectSingleNode("//unimarc:record",
                            nsmgr);
                        if (root_new == null)
                        {
                            root_new = domNew.DocumentElement.SelectSingleNode("//usmarc:record",
                                nsmgr);

                            if (root_new == null)
                            {
                                root_new = domNew.DocumentElement;

                                int nRet = DetectMarcSyntax(domOld,
                                    out strMarcSyntax,
                                    out strError);
                                if (nRet == -1)
                                    return -1;
                                if (nRet == 0)
                                {
                                    strError = "�¾�MARC��¼��syntax���޷�̽�⵽������޷����д���";
                                    return -1;
                                }
                            }
                            else
                                strMarcSyntax = "usmarc";
                        }
                        else
                        {
                            strMarcSyntax = "unimarc";
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "��ʱ�߲�������");
                        root_new = domNew.DocumentElement.SelectSingleNode("//" + strMarcSyntax + ":record",
                            nsmgr);
                        if (root_new == null)
                        {
                            return 0;
                        }
                    }

                    // ���¼�¼��ɾ��ָ���ݴ��������ȫ��905�ֶΣ�������ָ���ݴ����905�ֶα���
                    XmlNodeList nodes_new = domNew.DocumentElement.SelectNodes("//" + strMarcSyntax + ":datafield[@tag='905']",
                        nsmgr);

                    List<XmlNode> deleting = new List<XmlNode>();

                    for (int i = 0; i < nodes_new.Count; i++)
                    {
                        XmlNode field = nodes_new[i];

                        XmlNode subfield_a = field.SelectSingleNode(strMarcSyntax + ":subfield[@code='a']",
                            nsmgr);
                        string strValue = "";
                        if (subfield_a != null)
                            strValue = subfield_a.InnerText;

                        // �ҳ���Щ905$a�����Ϲݴ����
                        if (strValue != strLibraryCode)
                            deleting.Add(field);
                    }
                    for (int i = 0; i < deleting.Count; i++)
                    {
                        XmlNode temp = deleting[i];
                        if (temp.ParentNode != null)
                        {
                            temp.ParentNode.RemoveChild(temp);
                        }
                    }

                    // Ȼ�����¼�¼�в���ɼ�¼�У�ָ���ݴ��������ȫ��905�ֶ�
                    XmlNodeList nodes_old = null;
                    nodes_old = domOld.DocumentElement.SelectNodes("//" + strMarcSyntax + ":datafield[@tag='905']",
                        nsmgr);

                    if (nodes_old.Count > 0)
                    {
                        // �ҵ������ -- ��һ��905�ֶ�
                        XmlNode insert_pos = domNew.SelectSingleNode("//" + strMarcSyntax + ":datafield[@tag='905']",
                            nsmgr);

                        for (int i = 0; i < nodes_old.Count; i++)
                        {
                            XmlNode field = nodes_old[i];

                            XmlNode subfield_a = field.SelectSingleNode(strMarcSyntax + ":subfield[@code='a']",
                                nsmgr);
                            string strValue = "";
                            if (subfield_a != null)
                                strValue = subfield_a.InnerText;

                            // ����ָ���ݴ���ģ�������������
                            if (strValue == strLibraryCode)
                                continue;

                            // ���뵽�ɼ�¼ĩβ
                            XmlDocumentFragment fragment = domNew.CreateDocumentFragment();
                            fragment.InnerXml = field.OuterXml;

                            if (insert_pos != null)
                                root_new.InsertBefore(fragment, insert_pos);
                            else
                                root_new.AppendChild(fragment);
                        }
                    }


                }

                strNewBiblioXml = domNew.OuterXml;

                if (strNewSave == strNewBiblioXml)
                    return 0;

                return 1;
            }
            finally
            {
                if (bChangePartDenied == true && string.IsNullOrEmpty(strComment) == false)
                    strError += strComment;

                if (bChangePartDenied == true)
                    bChangePartDeniedParam = true;
            }
        }

        // ��ý���Ĺݴ���ĵ�һ��
        static string Cross(string strLibraryCodeList1, string strLibraryCodeList2)
        {
            if (string.IsNullOrEmpty(strLibraryCodeList1) == true
                && string.IsNullOrEmpty(strLibraryCodeList2) == true)
                return "";

            if (string.IsNullOrEmpty(strLibraryCodeList1) == true
                && string.IsNullOrEmpty(strLibraryCodeList2) == false)
                return null;

            if (string.IsNullOrEmpty(strLibraryCodeList1) == false
                && string.IsNullOrEmpty(strLibraryCodeList2) == true)
                return null;

            string[] parts1 = strLibraryCodeList1.Split(new char[] { ',' });
            string[] parts2 = strLibraryCodeList2.Split(new char[] { ',' });

            foreach (string s1 in parts1)
            {
                string code1 = s1.Trim();
                if (string.IsNullOrEmpty(code1) == true)
                    continue;
                foreach (string s2 in parts2)
                {
                    string code2 = s2.Trim();
                    if (string.IsNullOrEmpty(code2) == true)
                        continue;
                    if (code1 == code2)
                        return code1;
                }
            }

            return null;
        }

        // ���strSubList����Ĺݴ����Ƿ���ȫ������strList��
        static bool FullyContainIn(string strSubList, string strList)
        {
            if (string.IsNullOrEmpty(strSubList) == true
                && string.IsNullOrEmpty(strList) == true)
                return true;

            if (string.IsNullOrEmpty(strSubList) == true
    && string.IsNullOrEmpty(strList) == false)
                return false;

            string[] subs = strSubList.Split(new char [] {','}, StringSplitOptions.RemoveEmptyEntries);
            string[] parts = strList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string sub in subs)
            {
                foreach (string part in parts)
                {
                    if (sub == part)
                        goto FOUND;
                }
                return false;
            FOUND:
                continue;
            }

            return true;    // return false BUG
        }

        // �����ֶ�Ȩ�޶�����˳����������
        // return:
        //      -1  ����
        //      0   �ɹ�
        //      1   �в����ֶα��޸Ļ��˳�
        static int FilterBiblioByFieldNameList(
            string strFieldNameList,
            ref string strBiblioXml,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (string.IsNullOrEmpty(strBiblioXml) == true)
                return 0;

            string strMarcSyntax = "";
            string strMarc = "";

            if (string.IsNullOrEmpty(strBiblioXml) == false)
            {
                // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
                // parameters:
                //		bWarning	== true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
                //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
                //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
                nRet = MarcUtil.Xml2Marc(strBiblioXml,
                    true,
                    "", // this.CurMarcSyntax,
                    out strMarcSyntax,
                    out strMarc,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            if (string.IsNullOrEmpty(strMarcSyntax) == true)
                return 0;   // ���� MARC ��ʽ

            // �����ֶ�Ȩ�޶�����˳����������
            // return:
            //      -1  ����
            //      0   �ɹ�
            //      1   �в����ֶα��޸Ļ��˳�
            nRet = MarcDiff.FilterFields(strFieldNameList,
                ref strMarc,
                out strError);
            if (nRet == -1)
                return -1;
            bool bChanged = false;
            if (nRet == 1)
                bChanged = true;

            nRet = MarcUtil.Marc2XmlEx(strMarc,
                strMarcSyntax,
                ref strBiblioXml,
                out strError);
            if (nRet == -1)
                return -1;

            if (bChanged == true)
                return 1;
            return 0;
        }

        // ����������ֶ����б��ϲ��¾�������Ŀ��¼
        // �б��в�������ֶΣ����þɼ�¼�е�ԭʼ�ֶ�����
        // �㷨������ new �и�ԭ����Щ�����޸ĵ� MARC �ֶ�
        // TODO: �ض������ݿ�Ӧ�ù涨�� MARC ��ʽ��������Ϊ����������������������Ա��⸴�ӵ��ж�
        // return:
        //      -1  ����
        //      0   �ɹ�
        //      1   �в����޸�Ҫ�󱻾ܾ���strError �з�����ע����Ϣ
        static int MergeOldNewBiblioByFieldNameList(
            string strDefaultOperation,
            string strFieldNameList,
            string strOldBiblioXml,
            ref string strNewBiblioXml,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (string.IsNullOrEmpty(strOldBiblioXml) == true
                && string.IsNullOrEmpty(strNewBiblioXml) == true)
                return 0;

            string strOldMarcSyntax = "";
            string strOldMarc = "";

            if (string.IsNullOrEmpty(strOldBiblioXml) == false)
            {
                // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
                // parameters:
                //		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
                //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
                //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
                nRet = MarcUtil.Xml2Marc(strOldBiblioXml,
                    true,
                    "", // this.CurMarcSyntax,
                    out strOldMarcSyntax,
                    out strOldMarc,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            string strNewMarcSyntax = "";
            string strNewMarc = "";

            if (string.IsNullOrEmpty(strNewBiblioXml) == false)
            {
                // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
                // parameters:
                //		bWarning	==true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
                //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
                //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
                nRet = MarcUtil.Xml2Marc(strNewBiblioXml,
                    true,
                    "", // this.CurMarcSyntax,
                    out strNewMarcSyntax,
                    out strNewMarc,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            if (string.IsNullOrEmpty(strOldMarcSyntax) == true
                && string.IsNullOrEmpty(strNewMarcSyntax) == true)
                return 0;   // ���� MARC ��ʽ


            string strMarcSyntax = "";
            if (string.IsNullOrEmpty(strOldMarcSyntax) == false)
                strMarcSyntax = strOldMarcSyntax;
            else if (string.IsNullOrEmpty(strNewMarcSyntax) == false)
                strMarcSyntax = strNewMarcSyntax;
            else
            {
                strError = "MergeOldNewBiblioByFieldNameList() ���� �¾�����XML�о��� MARC ��ʽ��Ϣ";
                return -1;
            }

            // ������� MARC ��ʽ�Ƿ�һ��
            if (string.IsNullOrEmpty(strOldMarcSyntax) == false && string.IsNullOrEmpty(strNewMarcSyntax) == false)
            {
                if (strOldMarcSyntax != strNewMarcSyntax)
                {
                    strError = "�ɼ�¼�� MARC ��ʽ '" + strOldMarcSyntax + "' �������¼�¼�� MARC ��ʽ '" + strNewMarcSyntax + "'";
                    return -1;
                }
            }

            string strComment = "";
            // �����ֶ��޸�Ȩ�޶��壬�ϲ��¾����� MARC ��¼
            // return:
            //      -1  ����
            //      0   �ɹ�
            //      1   �в����޸�Ҫ�󱻾ܾ�
            nRet = MarcDiff.MergeOldNew(
                strDefaultOperation,
                strFieldNameList,
                strOldMarc,
                ref strNewMarc,
                out strComment,
                out strError);
            if (nRet == -1)
                return -1;
            bool bNotAccepted = false;
            if (nRet == 1)
                bNotAccepted = true;

            nRet = MarcUtil.Marc2XmlEx(strNewMarc,
                strMarcSyntax,
                ref strNewBiblioXml,
                out strError);
            if (nRet == -1)
                return -1;

            if (bNotAccepted == true)
            {
                strError = strComment;
                return 1;
            }
            return 0;
        }


#if NO
        // ����������ֶ����б��ϲ��¾�������Ŀ��¼
        // �б��в�������ֶΣ����þɼ�¼�е�ԭʼ�ֶ�����
        // �㷨������ new �и�ԭ����Щ�����޸ĵ� MARC �ֶ�
        static int MergeOldNewBiblioByFieldNameList(
            string strFieldNameList,
            string strOldBiblioXml,
            ref string strNewBiblioXml,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            XmlDocument domNew = new XmlDocument();
            if (String.IsNullOrEmpty(strNewBiblioXml) == true)
                strNewBiblioXml = "<root />";
            try
            {
                domNew.LoadXml(strNewBiblioXml);
            }
            catch (Exception ex)
            {
                strError = "strNewBiblioXmlװ��XMLDOMʱ����: " + ex.Message;
                return -1;
            }

            string strNewSave = domNew.OuterXml;

            XmlDocument domOld = new XmlDocument();
            if (String.IsNullOrEmpty(strOldBiblioXml) == true
                || (string.IsNullOrEmpty(strOldBiblioXml) == false && strOldBiblioXml.Length == 1))
                strOldBiblioXml = "<root />";
            try
            {
                domOld.LoadXml(strOldBiblioXml);
            }
            catch (Exception ex)
            {
                strError = "strOldBiblioXmlװ��XMLDOMʱ����: " + ex.Message;
                return -1;
            }

            string strMarcSyntax = "";

            // ̽��MARC��ʽ
            // return:
            //      -1  error
            //      0   �޷�̽��
            //      1   ̽�⵽��
            nRet = DetectMarcSyntax(domNew,
            out strMarcSyntax,
            out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                nRet = DetectMarcSyntax(domOld,
out strMarcSyntax,
out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    strError = "�޷�̽�⵽ MARC ��ʽ";
                    return -1;
                }
            }

            FieldNameList list = new FieldNameList();
            nRet = list.Build(strFieldNameList, out strError);
            if (nRet == -1)
                return -1;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("unimarc", Ns.unimarcxml);
            nsmgr.AddNamespace("usmarc", Ns.usmarcxml);

            XmlNode old_root = domOld.DocumentElement.SelectSingleNode("//" + strMarcSyntax + ":record", nsmgr);
            if (old_root == null)
            {
                // �� new ���˳�ȫ�������޸ĵ��ֶμ���
                XmlNodeList new_nodes = domNew.DocumentElement.SelectNodes("//" + strMarcSyntax + ":leader | //" + strMarcSyntax + ":controlfield | //" + strMarcSyntax + ":datafield",
nsmgr);
                foreach (XmlNode node in new_nodes)
                {
                    XmlElement element = (XmlElement)node;
                    string strTag = GetTag(element);

                    // TODO�������ͷ����ҲҪ�˳���Ҫ����һ��ȱʡֵ��ͷ����
                    if (list.Contains(strTag) == false && strTag != "###")
                        node.ParentNode.RemoveChild(node);

                }
                strNewBiblioXml = domNew.DocumentElement.OuterXml;
                return 0;
            }

            XmlNode new_root = domNew.DocumentElement.SelectSingleNode("//" + strMarcSyntax + ":record", nsmgr);
            if (new_root == null)
            {
                // new ����û���κ� MARC �ֶΣ���˲�������
                return 0;
            }

            // 1) �� new �е� �������޸ĵ��ֶα�ǳ���
            List<XmlNode> reserve_nodes = new List<XmlNode>();
            {
                XmlNodeList new_nodes = domNew.DocumentElement.SelectNodes("//" + strMarcSyntax + ":leader | //" + strMarcSyntax + ":controlfield | //" + strMarcSyntax + ":datafield",
                    nsmgr);
                foreach (XmlNode node in new_nodes)
                {
                    XmlElement element = (XmlElement)node;
                    string strTag = GetTag(element);

                    if (list.Contains(strTag) == false)
                        reserve_nodes.Add(element);
                }
            }

            // 2) �� old �а�ȫ���������޸ĵ��ֶζ�Ӧλ��һ��һ������ new �еĶ�Ӧ�ֶΡ������ new ��û���ҵ���Ӧ�ֶΣ��ڲ��������һ��ͬ���ֶκ��棬���û��ͬ���ֶΣ�����뵽�ʵ���˳��λ��
            XmlNodeList old_nodes = domOld.DocumentElement.SelectNodes("//" + strMarcSyntax + ":leader | //" + strMarcSyntax + ":controlfield | //" + strMarcSyntax + ":datafield",
    nsmgr);
            foreach (XmlNode node in old_nodes)
            {
                XmlElement element = (XmlElement)node;
                string strTag = GetTag(element);

                if (list.Contains(strTag) == true)
                    continue;

                XmlElement last_same_name_node = null;
                // ����һ��(nodes��)Ѱ�Ҷ�Ӧ���ֶ�Ԫ��
                // parameters:
                //      last_same_name_node ���һ��ͬtag����Ԫ��
                XmlElement target = FindElement(element,
                    new_root,
                    out last_same_name_node);
                if (target != null)
                {
                    target.InnerXml = element.InnerXml;
                    if (target.LocalName == "datafield")
                    {
                        // ��Ҫ�޸� ind1 ind2 ����
                        target.SetAttribute("ind1", element.GetAttribute("ind1"));
                        target.SetAttribute("ind2", element.GetAttribute("ind2"));
                    }
                    reserve_nodes.Remove(target);
                    continue;
                }

                if (last_same_name_node != null)
                {
                    last_same_name_node.InsertAfter(domNew.ImportNode(element, true), last_same_name_node);
                    continue;
                }

                // �ҵ�һ�����ʵ�λ�ò���
                insertSequence(element, new_root);
            }

            // 3) �� new �а�û�б����ǵı���˵��ֶ�ȫ��ɾ��
            foreach (XmlNode node in reserve_nodes)
            {
                node.ParentNode.RemoveChild(node);
            }

            strNewBiblioXml = domNew.DocumentElement.OuterXml;
            return 0;
        }

        static string GetTag(XmlElement element)
        {
            string strTag = element.GetAttribute("tag");
            if (element.LocalName == "leader")
                strTag = "###";
            return strTag;
        }

        public static void insertSequence(XmlElement element,
            XmlNode root)
        {
            string strTag = GetTag(element);
            if (strTag == "###")
            {
                // TODO: ע�����ǰ�Ƿ��Ѿ�����һ��ͷ��������������������
                root.InsertBefore(root.OwnerDocument.ImportNode(element, true), null);
                return;
            }

            // Ѱ�Ҳ���λ��
            List<int> values = new List<int>(); // �ۻ�ÿ���ȽϽ������
            int nInsertPos = -1;
            int i = 0;
            foreach (XmlNode current in root.ChildNodes)
            {
                if (current.NodeType != XmlNodeType.Element)
                {
                    i++;
                    continue;
                }
                XmlElement current_element = (XmlElement)current;
                string strCurrentTag = GetTag(current_element);

                int nBigThanCurrent = 0;   // �൱��node�͵�ǰ�������

                nBigThanCurrent = string.Compare(strTag, strCurrentTag);
                if (nBigThanCurrent < 0)
                {
                    nInsertPos = i;
                    break;
                }
                if (nBigThanCurrent == 0)
                {
                    /*
                    if ((style & InsertSequenceStyle.PreferHead) != 0)
                    {
                        nInsertPos = i;
                        break;
                    }
                     * */
                }

                // �ո���������ȵ�һ�Σ����ڵ�ǰλ�ý�������� (���߿�ʼ��󣬻��߿�ʼ��С)
                if (nBigThanCurrent != 0 && values.Count > 0 && values[values.Count - 1] == 0)
                {
                        nInsertPos = i - 1;
                        break;
                }

                values.Add(nBigThanCurrent);
                i++;
            }

            if (nInsertPos == -1)
            {
                root.AppendChild(root.OwnerDocument.ImportNode(element, true));
                return;
            }

            root.InsertBefore(root.OwnerDocument.ImportNode(element, true), root.ChildNodes[nInsertPos]);
        }

        // ���һ��Ԫ�����ֵ�Ԫ����ͬ����λ��
        static int GetDupCount(XmlElement start)
        {
            string strTag = GetTag(start);
            int nCount = 0;
            XmlNode current = start.PreviousSibling;
            while (current != null)
            {
                if (current.NodeType == XmlNodeType.Element)
                {
                    XmlElement element = (XmlElement)current;
                    string strCurrentTag = GetTag(element);

                    if (strCurrentTag == strTag)
                        nCount ++;
                }
                current = current.PreviousSibling;
            }

            return nCount;
        }

        // ����һ��(root֮����)Ѱ�Ҷ�Ӧ���ֶ�Ԫ��
        // parameters:
        //      root    ҪѰ�ҵ�Ԫ�ص�����Ԫ��
        //      last_same_name_node ���һ��ͬtag����Ԫ��
        static XmlElement FindElement(XmlElement start,
            XmlNode root,
            out XmlElement last_same_name_node)
        {
            last_same_name_node = null;

            string strTag = GetTag(start);

            int dup = GetDupCount(start);

            int nCount = 0;
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                XmlElement element = (XmlElement)node;
                string strCurrenTag = GetTag(element);

                if (strTag == strCurrenTag)
                {
                    if (nCount == dup)
                        return element;
                    nCount++;
                    last_same_name_node = element;
                }
            }

            return null;    // û���ҵ�
        }

#endif


        // ֪ͨ�����Ƽ������鵽��
        // parameters:
        //      strLibraryCodeList  Ҫ֪ͨ�Ķ����������Ĺݴ����б��ձ�ʾֻ֪ͨȫ�ֶ���
        public LibraryServerResult NotifyNewBook(
            SessionInfo sessioninfo,
            string strBiblioRecPath,
            string strLibraryCodeList)
        {
            string strError = "";
            LibraryServerResult result = new LibraryServerResult();
            int nRet = 0;

            // ��� strLibraryCodeList �еĹݴ����Ƿ�ȫ�ڵ�ǰ�û���Ͻ֮��
            if (sessioninfo.GlobalUser == false)
            {
                if (FullyContainIn(strLibraryCodeList, sessioninfo.LibraryCodeList) == false)
                {
                    strError = "������Ĺݴ��� '" + strLibraryCodeList + "' ������ȫ�����ڵ�ǰ�û��Ĺ�Ͻ��Χ�ݴ��� '"+sessioninfo.LibraryCodeList+"' ��";
                    goto ERROR1;
                }
            }

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            // ̽����Ŀ��¼��û����������ע��¼
            List<DeleteEntityInfo> commentinfos = null;
            long lHitCount = 0;
            // return:
            //      -1  error
            //      0   not exist entity dbname
            //      1   exist entity dbname
            nRet = this.CommentItemDatabase.SearchChildItems(channel,
                strBiblioRecPath,
                "return_record_xml", // ��DeleteEntityInfo�ṹ�з���OldRecord���ݣ� ���Ҳ�Ҫ�����ͨ��Ϣ
                out lHitCount,
                out commentinfos,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 0)
            {
                Debug.Assert(commentinfos.Count == 0, "");
            }

            // ���û����ע��¼���򲻱�֪ͨ
            if (commentinfos == null || commentinfos.Count == 0)
            {
                result.Value = 0;   // ��ʾû�п�֪ͨ��
                return result;
            }

            // List<string> suggestors = new List<string>();
            Hashtable suggestor_table = new Hashtable();    // ������ --> �ݴ���
            foreach (DeleteEntityInfo info in commentinfos)
            {
                if (string.IsNullOrEmpty(info.OldRecord) == true)
                    continue;

                XmlDocument domExist = new XmlDocument();
                try
                {
                    domExist.LoadXml(info.OldRecord);
                }
                catch (Exception ex)
                {
                    strError = "��ע��¼ '" + info.RecPath + "' װ�ؽ���DOMʱ��������: " + ex.Message;
                    goto ERROR1;
                }

                // �Ƿ�Ϊ�Ƽ���
                string strType = DomUtil.GetElementText(domExist.DocumentElement, "type");
                if (strType != "������ѯ")
                    continue;
                string strOrderSuggestion = DomUtil.GetElementText(domExist.DocumentElement, "orderSuggestion");
                if (strOrderSuggestion != "yes")
                    continue;

                string strLibraryCode = DomUtil.GetElementText(domExist.DocumentElement, "libraryCode");
                // �������������Ĺݴ����Ƿ����б���
                if (string.IsNullOrEmpty(strLibraryCodeList) == true
                    && string.IsNullOrEmpty(strLibraryCode) == true)
                {
                    // ȫ�ֵĶ��ߣ�ȫ�ֵĹݴ���Ҫ��
                }
                else
                {
                    if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                        continue;   // �����б��еķֹݵĶ��߲�Ҫ֪ͨ����Ϊһ��֪ͨ�ˣ��ᷢ���ⲿ�ֶ��ߵ��Լ��ķֹݽ費����ľ���
                }

                // ��ô������û���
                XmlNode node = domExist.DocumentElement.SelectSingleNode("operations/operation[@name='create']");
                if (node == null)
                    continue;
                string strOperator = DomUtil.GetAttr(node, "operator");
                if (string.IsNullOrEmpty(strOperator) == true)
                    continue;

                // suggestors.Add(strOperator);

                // ����ע��¼�л��<libraryCode>Ԫ�أ����Ǵ�����ע��¼ʱ�̵Ĳ����ߵĹݴ���
                // �����Ϳ��Բ��ظ��ݶ��߼�¼·�����Ƶ����ߵĹݴ���
                suggestor_table[strOperator] = strLibraryCode;  // ��Ȼ��ȥ����
            }

            if (suggestor_table.Count == 0)
            {
                result.Value = 0;   // ��ʾû�п�֪ͨ��
                return result;
            }

            // �����Ŀ��¼
#if NO
            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "channel == null";
                goto ERROR1;
            }
#endif
            string strMetaData = "";
            string strOutputPath = "";
            byte[] exist_timestamp = null;
            string strBiblioXml = "";

            // �ȶ������ݿ��д�λ�õ����м�¼
            long lRet = channel.GetRes(strBiblioRecPath,
                out strBiblioXml,
                out strMetaData,
                out exist_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            if (strBiblioRecPath != strOutputPath)
            {
                strError = "����·�� '" + strBiblioRecPath + "' ����ԭ�м�¼ʱ�����ַ��ص�·�� '" + strOutputPath + "' ��ǰ�߲�һ��";
                goto ERROR1;
            }

            // ������ĿժҪ
            string[] formats = new string[1];
            formats[0] = "summary";
            List<string> temp_results = null;
            nRet = BuildFormats(
                sessioninfo,
                strBiblioRecPath,
                strBiblioXml,
                "", // strOutputPath,   // ���¼��·��
                "", // strMetaData,     // ���¼��metadata
                null,
                formats,
                out temp_results,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (temp_results == null || temp_results.Count == 0)
            {
                strError = "temp_results error";
                goto ERROR1;
            }
            string strSummary = temp_results[0];

            // ȥ��
            // StringUtil.RemoveDupNoSort(ref suggestors);

            foreach (string id in suggestor_table.Keys)
            {
                if (string.IsNullOrEmpty(id) == true)
                    continue;

                string strLibraryCode = (string)suggestor_table[id];

#if NO
                string strReaderXml = "";
                byte[] reader_timestamp = null;
                string strOutputReaderRecPath = "";

                int nIsReader = -1; // -1 �����  0 ���Ƕ��� 1 �Ƕ���
                // return:
                //      -1  error
                //      0   not found
                //      1   ����1��
                //      >1  ���ж���1��
                nRet = this.GetReaderRecXml(
                    sessioninfo.Channels,
                    id,
                    out strReaderXml,
                    out strOutputReaderRecPath,
                    out reader_timestamp,
                    out strError);
                if (nRet == -1)
                {
                    // text-level: �ڲ�����
                    strError = "������߼�¼ '" + id + "' ʱ��������: " + strError;
                    goto ERROR1;
                }

                if (nRet == 0 || string.IsNullOrEmpty(strReaderXml) == true)
                    nIsReader = 0; // ���Ƕ���
                else
                    nIsReader = 1;

                // ��ö��ߴ����Ĺݴ���
                string strLibraryCode = "";

                if (nIsReader == 1)
                {
                    // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                    if (this.IsCurrentChangeableReaderPath(strOutputReaderRecPath,
                        sessioninfo.LibraryCodeList,
                        out strLibraryCode) == false)
                    {
                        continue;   // ���ߵĹݴ��벻�ڵ�ǰ�û���Ͻ��Χ��
                    }

                    // �������������Ĺݴ����Ƿ����б���
                    if (string.IsNullOrEmpty(strLibraryCodeList) == true
                        && string.IsNullOrEmpty(strLibraryCode) == true)
                    {
                        // ȫ�ֵĶ��ߣ�ȫ�ֵĹݴ���Ҫ��
                    }
                    else
                    {
                        if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                            continue;   // �����б��еķֹݵĶ��߲�Ҫ֪ͨ����Ϊһ��֪ͨ�ˣ��ᷢ���ⲿ�ֶ��ߵ��Լ��ķֹݽ費����ľ���
                    }
                }
                else
                {
                    // ������Ա�ʻ�����ù�����Ա�Ĺݴ���

                    UserInfo userinfo = null;
                    // return:
                    //      -1  ����
                    //      0   û���ҵ�
                    //      1   �ҵ�
                    nRet = GetUserInfo(id,
                        out userinfo,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "��ȡ�ʻ� '" + id + "' ����Ϣʱ����: " + strError;
                        goto ERROR1;
                    }

                    if (nRet == 0)
                        continue;   // û������û�

                    // ��鹤����Ա��Ͻ��ͼ��ݺ� strLibraryCodeList ֮��Ľ������
                    Debug.Assert(userinfo != null, "");
                    strLibraryCode = Cross(userinfo.LibraryCode, strLibraryCodeList);
                    if (strLibraryCode == null)
                        continue;   // �� strLibraryCodeList û�н���
                }
#endif

                string strBody = "�𾴵Ķ��ߣ�\r\n\r\n���Ƽ�������ͼ��\r\n\r\n------\r\n" + strSummary + "\r\n------\r\n\r\n�Ѿ�����ͼ��ݣ���ӭ����ͼ��ݽ��Ļ���������л����ͼ��ݹ����Ĵ���֧�֡�";
                nRet = MessageNotify(
                    sessioninfo,
                    strLibraryCode,
                    id,
                    "", // strReaderXml,
                    "���鵽��֪ͨ", // strTitle,
                    strBody,
                    "text", // strMime,    // "text",
                    "ͼ���",
                    "���鵽��֪ͨ",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            result.Value = 0;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        // ����߷���֪ͨ��Ϣ
        // parameters:
        //      strLibraryCode  �����������Ĺݴ��롣���Ϊnull(��""�Ǳ�ʾȫ���û�)����ʾϣ�������ڲ����л�ȡ�ݴ��룬�����ж��Ƿ��ڵ�ǰ�û��Ĺ�Ͻ��Χ�ڣ�������ڹ�Ͻ��Χ���򲻷�����Ϣ���������������Ϊ�յ��õĻ�����ٶ�����ǰ�Ѿ������ˣ������ڲ��ټ��
        //      strReaderXml    ���߼�¼XML�����Ϊ�գ���ʾ��������Ҫ�Զ���ö��߼�¼XML
        // return
        //      -1  ����
        //      0   �ɹ�
        //      1   ��Ϊ���߹ݴ��벻�ڵ�ǰ�û���Ͻ��Χ�ڣ�����������
        public int MessageNotify(
            SessionInfo sessioninfo,
            string strLibraryCode,
            string strReaderBarcode,
            string strReaderXml,
            string strTitle,
            string strBody,
            string strMime,    // "text",
            string strSender,
            string strErrorType,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            List<string> bodytypes = new List<string>();
            bodytypes.Add("dpmail");
            bodytypes.Add("email");
            if (this.m_externalMessageInterfaces != null)
            {
                foreach (MessageInterface message_interface in this.m_externalMessageInterfaces)
                {
                    bodytypes.Add(message_interface.Type);
                }
            }

            string strReaderEmailAddress = "";

            // ������߼�¼
            byte[] reader_timestamp = null;
            string strOutputReaderRecPath = "";
            XmlDocument readerdom = null;
            int nIsReader = -1; // -1 �����  0 ���Ƕ��� 1 �Ƕ���

            for (int i = 0; i < bodytypes.Count; i++)
            {
                string strBodyType = bodytypes[i];

                if (strBodyType == "email")
                {
                    if (readerdom == null && (nIsReader == -1 || nIsReader == 1))
                    {
                        if (string.IsNullOrEmpty(strReaderXml) == true)
                        {
                            // return:
                            //      -1  error
                            //      0   not found
                            //      1   ����1��
                            //      >1  ���ж���1��
                            nRet = this.GetReaderRecXml(
                                sessioninfo.Channels,
                                strReaderBarcode,
                                out strReaderXml,
                                out strOutputReaderRecPath,
                                out reader_timestamp,
                                out strError);
                            if (nRet == -1)
                            {
                                // text-level: �ڲ�����
                                strError = "������߼�¼ '" + strReaderBarcode + "' ʱ��������: " + strError;
                                return -1;
                            }

                            if (nRet == 0 || string.IsNullOrEmpty(strReaderXml) == true)
                            {
                                nIsReader = 0; // ���Ƕ���
                                continue;
                            }

                            if (strLibraryCode == null)
                            {
                                // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                                if (this.IsCurrentChangeableReaderPath(strOutputReaderRecPath,
                                    sessioninfo.LibraryCodeList,
                                    out strLibraryCode) == false)
                                {
                                    strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
                                    return 1;
                                }
                            }
                        }

                        nIsReader = 1;
                        nRet = LibraryApplication.LoadToDom(strReaderXml,
                            out readerdom,
                            out strError);
                        if (nRet == -1)
                        {
                            // text-level: �ڲ�����
                            strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                            return -1;
                        }
                    }
                    strReaderEmailAddress = DomUtil.GetElementText(readerdom.DocumentElement,
                        "email");

                    if (String.IsNullOrEmpty(strReaderEmailAddress) == true)
                        continue;
                }

                if (strBodyType == "dpmail")
                {
                    if (this.MessageCenter == null)
                    {
                        continue;
                    }
                }

#if NO
                List<string> notifiedBarcodes = new List<string>();


                // ����ض����͵���֪ͨ���Ĳ�������б�
                // return:
                //      -1  error
                //      ����    notifiedBarcodes������Ÿ���
                nRet = GetNotifiedBarcodes(readerdom,
                    strBodyType,
                    out notifiedBarcodes,
                    out strError);
                if (nRet == -1)
                    return -1;
#endif


                bool bSendMessageError = false;


                // dpmail
                if (strBodyType == "dpmail")
                {
                    // ������Ϣ
                    // return:
                    //      -1  ����
                    //      0   �ɹ�
                    nRet = this.MessageCenter.SendMessage(
                        sessioninfo.Channels,
                        strReaderBarcode,
                        strSender, // "ͼ���",
                        strTitle,
                        strMime,    // "text",
                        strBody,
                        false,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "����dpmail����: " + strError;
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(strLibraryCode,
                            strErrorType,
                            "dpmail message " + strErrorType + "��Ϣ���ʹ�����",
                            1);
                        bSendMessageError = true;
                        // return -1;
                    }
                    else
                    {
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(
                            strLibraryCode,
                            strErrorType,
                            "dpmail" + strErrorType + "����",
                            1);
                    }
                }

                // ��չ��Ϣ
                MessageInterface external_interface = this.GetMessageInterface(strBodyType);

                if (external_interface != null && nIsReader != 0)
                {
                    if (readerdom == null)
                    {
                        if (string.IsNullOrEmpty(strReaderXml) == true)
                        {
                            // return:
                            //      -1  error
                            //      0   not found
                            //      1   ����1��
                            //      >1  ���ж���1��
                            nRet = this.GetReaderRecXml(
                                sessioninfo.Channels,
                                strReaderBarcode,
                                out strReaderXml,
                                out strOutputReaderRecPath,
                                out reader_timestamp,
                                out strError);
                            if (nRet == -1)
                            {
                                // text-level: �ڲ�����
                                strError = "������߼�¼ '" + strReaderBarcode + "' ʱ��������: " + strError;
                                return -1;
                            }

                            if (nRet == 0 || string.IsNullOrEmpty(strReaderXml) == true)
                            {
                                nIsReader = 0; // ���Ƕ���
                                continue;
                            }

                            if (strLibraryCode == null)
                            {
                                // �۲�һ�����߼�¼·���������ǲ����ڵ�ǰ�û���Ͻ�Ķ��߿ⷶΧ��?
                                if (this.IsCurrentChangeableReaderPath(strOutputReaderRecPath,
                                    sessioninfo.LibraryCodeList,
                                    out strLibraryCode) == false)
                                {
                                    strError = "���߼�¼·�� '" + strOutputReaderRecPath + "' �Ķ��߿ⲻ�ڵ�ǰ�û���Ͻ��Χ��";
                                    return 1;
                                }
                            }
                        }

                        nIsReader = 1;
                        nRet = LibraryApplication.LoadToDom(strReaderXml,
                            out readerdom,
                            out strError);
                        if (nRet == -1)
                        {
                            // text-level: �ڲ�����
                            strError = "װ�ض��߼�¼����XML DOMʱ��������: " + strError;
                            return -1;
                        }
                    }


                    // ������Ϣ
                    try
                    {
                        // ����һ����Ϣ
                        // parameters:
                        //      strPatronBarcode    ����֤�����
                        //      strPatronXml    ���߼�¼XML�ַ����������Ҫ��֤����������ĳЩ�ֶ���ȷ����Ϣ���͵�ַ�����Դ�XML��¼��ȡ
                        //      strMessageText  ��Ϣ����
                        //      strError    [out]���ش����ַ���
                        // return:
                        //      -1  ����ʧ��
                        //      0   û�б�Ҫ����
                        //      1   ���ͳɹ�
                        nRet = external_interface.HostObj.SendMessage(
                            strReaderBarcode,
                            readerdom.DocumentElement.OuterXml,
                            strBody,
                            strLibraryCode,
                            out strError);
                    }
                    catch (Exception ex)
                    {
                        strError = external_interface.Type + " ���͵��ⲿ��Ϣ�ӿ�Assembly��SendMessage()�����׳��쳣: " + ex.Message;
                        nRet = -1;
                    }
                    if (nRet == -1)
                    {
                        strError = "����� '" + strReaderBarcode + "' ����" + external_interface.Type + " messageʱ����: " + strError;
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(
                            strLibraryCode,
                            strErrorType,
                            external_interface.Type + " message " + strErrorType + "��Ϣ���ʹ�����",
                            1);
                        bSendMessageError = true;
                        // return -1;
                    }
                    else if (nRet == 1)
                    {
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(strLibraryCode,
                            strErrorType,
                            external_interface.Type + " message " + strErrorType + "����",
                            1);
                    }
                }

                // email
                if (strBodyType == "email")
                {
                    // ����email
                    // return:
                    //      -1  error
                    //      0   not found smtp server cfg
                    //      1   succeed
                    nRet = this.SendEmail(strReaderEmailAddress,
                        strTitle,
                        strBody,
                        strMime,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "����email����: " + strError;
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(
                            strLibraryCode,
                            strErrorType,
                            "email message " + strErrorType + "��Ϣ���ʹ�����",
                            1);
                        bSendMessageError = true;
                        // return -1;
                    }
                    else if (nRet == 1)
                    {
                        if (this.Statis != null)
                            this.Statis.IncreaseEntryValue(
                            strLibraryCode,
                            strErrorType,
                            "email" + strErrorType + "����",
                            1);
                    }
                }

            } // end of for


            return 0;
        }

        // parameters:
        //      strParameters   ()�еĲ���
        public static bool IsInAccessList(string strSub,
            string strList,
            out string strParameters)
        {
            strParameters = "";

            List<string> segments = StringUtil.SplitString(strList,
    ",",
    new string[] { "()" },
    StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in segments)
            {
                string strLeft = "";
                string strRight = "";
                int nRet = s.IndexOf("(");
                if (nRet != -1)
                {
                    strLeft = s.Substring(0, nRet).Trim();
                    strRight = s.Substring(nRet + 1).Trim();
                    if (string.IsNullOrEmpty(strRight) == false && strRight[strRight.Length - 1] == ')')
                        strRight = strRight.Substring(0, strRight.Length - 1);
                }
                else
                    strLeft = s;

                if (strLeft == strSub)
                {
                    strParameters = strRight;
                    return true;
                }
            }

            return false;
        }

        // �޸ı�Ŀ��¼
        // parameters:
        //      strAction   ������Ϊ"new" "change" "delete" "onlydeletebiblio" "onlydeletesubrecord"֮һ��"delete"��ɾ����Ŀ��¼��ͬʱ�����Զ�ɾ��������ʵ���¼������Ҫ��ʵ���δ���������ɾ����
        //      strBiblioType   Ŀǰֻ����xmlһ��
        //      baTimestamp ʱ��������Ϊ�´�����¼������Ϊnull 
        //      strOutputBiblioRecPath �������Ŀ��¼·������strBiblioRecPath��ĩ��Ϊ�ʺţ���ʾ׷�ӱ�����Ŀ��¼��ʱ�򣬱���������ʵ�ʱ������Ŀ��¼·��
        //      baOutputTimestamp   ������ɺ��µ�ʱ���
        public LibraryServerResult SetBiblioInfo(
            SessionInfo sessioninfo,
            string strAction,
            string strBiblioRecPath,
            string strBiblioType,
            string strBiblio,
            byte[] baTimestamp,
            string strComment,
            out string strOutputBiblioRecPath,
            out byte[] baOutputTimestamp)
        {
            string strError = "";
            long lRet = 0;
            int nRet = 0;

            strOutputBiblioRecPath = "";
            baOutputTimestamp = null;

            LibraryServerResult result = new LibraryServerResult();
            bool bChangePartDenied = false; // �޸Ĳ������ֱ��ܾ�
            string strDeniedComment = "";   // ���ڲ����ֶα��ܾ���ע��

            string strLibraryCode = ""; // ͼ��ݴ���
            if (sessioninfo.Account != null)
                strLibraryCode = sessioninfo.Account.AccountLibraryCode;

            // ������
            strAction = strAction.ToLower();

            if (strAction != "new"
                && strAction != "change"
                && strAction != "delete"
                && strAction != "onlydeletebiblio"
                && strAction != "onlydeletesubrecord")
            {
                strError = "strAction����ֵӦ��Ϊnew change delete onlydeletebiblio onlydeletesubrecord֮һ";
                goto ERROR1;
            }

            strBiblioType = strBiblioType.ToLower();
            if (strBiblioType != "xml")
            {
                strError = "strBiblioType����Ϊ\"xml\"";
                goto ERROR1;
            }

            {
                if (this.TestMode == true || sessioninfo.TestMode == true)
                {
                    // �������ģʽ
                    // return:
                    //      -1  �����̳���
                    //      0   ����ͨ��
                    //      1   ������ͨ��
                    nRet = CheckTestModePath(strBiblioRecPath,
                        out strError);
                    if (nRet != 0)
                    {
                        strError = "�޸���Ŀ��¼�Ĳ������ܾ�: " + strError;
                        goto ERROR1;
                    }
                }
            }

            string strUnionCatalogStyle = "";
            string strBiblioDbName = "";
            bool bRightVerified = false;
            bool bOwnerOnly = false;

            string strAccessParameters = "";

            // ������ݿ�·���������ǲ����Ѿ����涨��ı�Ŀ�⣿
            if (String.IsNullOrEmpty(strBiblioRecPath) == false)
            {
                strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);

                if (this.IsBiblioDbName(strBiblioDbName) == false)
                {
                    strError = "��Ŀ��¼·�� '" + strBiblioRecPath + "' �а��������ݿ��� '" + strBiblioDbName + "' ���ǺϷ�����Ŀ����";
                    goto ERROR1;
                }

#if NO
                if (this.TestMode == true || sessioninfo.TestMode == true)
                {
                    string strID = ResPath.GetRecordId(strBiblioRecPath);
                    if (StringUtil.IsPureNumber(strID) == true)
                    {
                        long v = 0;
                        long.TryParse(strID, out v);
                        if (v > 1000)
                        {
                            strError = "����ģʽ��ֻ���޸� ID С�ڵ��� 1000 ����Ŀ��¼";
                            goto ERROR1;
                        }
                    }
                }
#endif

                ItemDbCfg cfg = null;
                cfg = GetBiblioDbCfg(strBiblioDbName);
                Debug.Assert(cfg != null, "");
                strUnionCatalogStyle = cfg.UnionCatalogStyle;

                // ����ȡȨ��
                if (String.IsNullOrEmpty(sessioninfo.Access) == false)
                {
                    string strAccessActionList = "";
                    // return:
                    //      null    ָ���Ĳ������͵�Ȩ��û�ж���
                    //      ""      ������ָ�����͵Ĳ���Ȩ�ޣ����Ƿ񶨵Ķ���
                    //      ����      Ȩ���б�* ��ʾͨ���Ȩ���б�
                    strAccessActionList = GetDbOperRights(sessioninfo.Access,
                        strBiblioDbName,
                        "setbiblioinfo");
                    if (strAccessActionList == null)
                    {
                        // �����ǲ��ǹ��� setbiblioinfo ���κ�Ȩ�޶�û�ж���?
                        strAccessActionList = GetDbOperRights(sessioninfo.Access,
                            "",
                            "setbiblioinfo");
                        if (strAccessActionList == null)
                        {
                            // 2013/4/18
                            // TODO: ������ʾ"��û��... Ҳû�� ..."
                            goto CHECK_RIGHTS_2;
                        }
                        else
                        {
                            strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strBiblioDbName + "' ִ�� setbiblioinfo " + strAction + " �����Ĵ�ȡȨ��";
                            result.Value = -1;
                            result.ErrorInfo = strError;
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }
                    }
                    if (strAccessActionList == "*")
                    {
                        // ͨ��
                    }
                    else
                    {
                        if (strAction == "delete"
                            && IsInAccessList("ownerdelete", strAccessActionList, out strAccessParameters) == true)
                        {
                            bOwnerOnly = true;
                        }
                        else if (strAction == "change"
                            && IsInAccessList("ownerchange", strAccessActionList, out strAccessParameters) == true)
                        {
                            bOwnerOnly = true;
                        }
                        else if (strAction == "onlydeletebiblio"
                            && IsInAccessList("owneronlydeletebiblio", strAccessActionList, out strAccessParameters) == true)
                        {
                            bOwnerOnly = true;
                        }
                        else if (strAction == "onlydeletesubrecord"
                            && IsInAccessList("owneronlydeletesubrecord", strAccessActionList, out strAccessParameters) == true)
                        {
                            bOwnerOnly = true;
                        }
                        else if (IsInAccessList(strAction, strAccessActionList, out strAccessParameters) == false)
                        {
                            strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strBiblioDbName + "' ִ�� setbiblioinfo " + strAction + " �����Ĵ�ȡȨ��";
                            result.Value = -1;
                            result.ErrorInfo = strError;
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }
                    }

                    bRightVerified = true;
                }
            }

            CHECK_RIGHTS_2:
            if (bRightVerified == false)
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("setbiblioinfo", sessioninfo.RightsOrigin) == false
                    && StringUtil.IsInList("order", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "������Ŀ��Ϣ���ܾ������߱�order��setbiblioinfoȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }


            // TODO: ��Ҫ����ļ�飬���������������MARC��ʽ�ǲ���������ݿ�Ҫ��ĸ�ʽ��


            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "channel == null";
                goto ERROR1;
            }


            // ׼����־DOM
            XmlDocument domOperLog = new XmlDocument();
            domOperLog.LoadXml("<root />");
            // �������漰�����߿⣬����û��<libraryCode>Ԫ��
            DomUtil.SetElementText(domOperLog.DocumentElement, "operation",
                "setBiblioInfo");
            DomUtil.SetElementText(domOperLog.DocumentElement, "action",
                strAction);
            if (string.IsNullOrEmpty(strComment) == false)
            {
                DomUtil.SetElementText(domOperLog.DocumentElement, "comment",
        strComment);
            }

            string strOperTime = this.Clock.GetClock();

            string strExistingXml = "";
            byte[] exist_timestamp = null;

            if (strAction == "change"
                || strAction == "delete"
                || strAction == "onlydeletebiblio"
                || strAction == "onlydeletesubrecord")
            {
                string strMetaData = "";
                string strOutputPath = "";

                // �ȶ������ݿ��д�λ�õ����м�¼
                lRet = channel.GetRes(strBiblioRecPath,
                    out strExistingXml,
                    out strMetaData,
                    out exist_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.NotFound)
                    {
                        // 2013/3/12
                        if (strAction == "change")
                        {
                            strError = "ԭ�м�¼ '" + strBiblioRecPath + "' ������, ��� setbiblioinfo " + strAction + " �������ܾ� (��ʱ���Ҫ�����¼�¼����ʹ�� new �ӹ���)";
                            result.Value = -1;
                            result.ErrorInfo = strError;
                            result.ErrorCode = ErrorCode.NotFound;
                            return result;
                        }
                        goto SKIP_MEMO_OLDRECORD;
                    }
                    else
                    {
                        strError = "������Ŀ��Ϣ��������, �ڶ���ԭ�м�¼�׶�:" + strError;
                        goto ERROR1;
                    }
                }

                if (strBiblioRecPath != strOutputPath)
                {
                    strError = "����·�� '" + strBiblioRecPath + "' ����ԭ�м�¼ʱ�����ַ��ص�·�� '" + strOutputPath + "' ��ǰ�߲�һ��";
                    goto ERROR1;
                }


                XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                        "oldRecord", strExistingXml);
                DomUtil.SetAttr(node, "recPath", strBiblioRecPath);

                // �����Ŀ��¼ԭ���Ĵ����� 998$z
                if (bOwnerOnly)
                {
                    string strOwner = "";

                    // �����Ŀ��¼�Ĵ�����
                    // return:
                    //      -1  ����
                    //      0   û���ҵ� 998$z���ֶ�
                    //      1   �ҵ�
                    nRet = GetBiblioOwner(strExistingXml,
                        out strOwner,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (strOwner != sessioninfo.UserID)
                    {
                        strError = "��ǰ�û� '" + sessioninfo.UserID + "' ������Ŀ��¼ '" + strBiblioDbName + "' �Ĵ�����(998$z)����� setbiblioinfo " + strAction + " �������ܾ�";
                        result.Value = -1;
                        result.ErrorInfo = strError;
                        result.ErrorCode = ErrorCode.AccessDenied;
                        return result;
                    }
                }

                // TODO: ����Ѵ��ڵ�XML��¼�У�MARC�������ĵ�������ô������Ŀ��¼
                // ���洢��������Ϣ����ʱ����Ҫ��ǰ��������XML��¼���Ѵ��ڵļ�¼���кϲ�����
                // ��ֹóȻ�������ĵ����µ�������Ϣ��
            }

        SKIP_MEMO_OLDRECORD:

            bool bBiblioNotFound = false;

            string strRights = "";
            
            if (sessioninfo.Account != null)
                strRights = sessioninfo.Account.Rights;

            if (strAction == "new")
            {
                // ��orderȨ�޵��жϡ�orderȨ��������κο����new����

                // TODO: ��ֻ�����ϱ�Ŀģ��Ҫ���м�¼Ԥ����
                // ҲҪ��ϵ�ǰ�û��ǲ��Ǿ���writeobjectȨ�ޣ������жϺʹ���
                // �����ǰ�û����߱�writeobjectȨ�ޣ���Ҳ��Ӧ��XML�а����κ�<dprms:file>Ԫ��(��������ˣ�����Ϊ������߾���(�������ǰ�˵ĸ���)�����Ǻ��Ժ�д�룿)

                {
                    /*
                    // ��strBiblio�����ݽ��мӹ���ȷ��905�ֶη������ϱ�ĿҪ��

                    // ׼�����ϱ�Ŀ������Ŀ��XML��¼
                    // ���ܣ��ų�strLibraryCode���������905�ֶ�
                    // return:
                    //      -1  error
                    //      0   not delete any fields
                    //      1   deleted some fields
                    nRet = PrepareNewBiblioRec(
                        strLibraryCode,
                        ref strBiblio,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                     * */

                    strExistingXml = "";
                    // �ϲ����ϱ�Ŀ���¾���Ŀ��XML��¼
                    // ���ܣ��ų��¼�¼�ж�strLibraryCode���������905�ֶε��޸�
                    // parameters:
                    //      bChangePartDenied   ������α��趨Ϊ true���� strError �з����˹��ڲ����޸ĵ�ע����Ϣ
                    // return:
                    //      -1  error
                    //      0   not delete any fields
                    //      1   deleted some fields
                    nRet = MergeOldNewBiblioRec(
                        strRights,
                        strUnionCatalogStyle,
                        strLibraryCode,
                        "insert",
                        strAccessParameters,
                        strExistingXml,
                        ref strBiblio,
                        ref bChangePartDenied,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (bChangePartDenied == true && string.IsNullOrEmpty(strError) == false)
                        strDeniedComment += " " + strError;
                }

                // 2009/11/2 new add
                // ��Ҫ�ж�·�����һ���Ƿ�Ϊ�ʺţ�
                string strTargetRecId = ResPath.GetRecordId(strBiblioRecPath);
                if (strTargetRecId == "?" || String.IsNullOrEmpty(strTargetRecId) == true)
                {
                    if (String.IsNullOrEmpty(strTargetRecId) == true)
                        strBiblioRecPath += "/?";
                }
                else
                {
                    /*
                    strError = "��������Ŀ��¼��ʱ��ֻ��ʹ�á���Ŀ����/?����ʽ��·��(������ʹ�� '"+strBiblioRecPath+"' ��ʽ)�����Ҫ��ָ��λ�ñ��棬��ʹ���޸�(change)�ӹ���";
                    goto ERROR1;
                     * */
                }

                // 2011/11/30
                nRet = this.ClearOperation(
                    ref strBiblio,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                nRet = this.SetOperation(
ref strBiblio,
"create",
sessioninfo.UserID,
"",
true,
10,
out strError);
                if (nRet == -1)
                    goto ERROR1;

                lRet = channel.DoSaveTextRes(strBiblioRecPath,
                    strBiblio,
                    false,
                    "content", // ,ignorechecktimestamp
                    baTimestamp,
                    out baOutputTimestamp,
                    out strOutputBiblioRecPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (this.TestMode == true || sessioninfo.TestMode)
                {
                    string strID = ResPath.GetRecordId(strOutputBiblioRecPath);
                    if (StringUtil.IsPureNumber(strID) == true)
                    {
                        long v = 0;
                        long.TryParse(strID, out v);
                        if (v > 1000)
                        {
                            strError = "����ģʽ��ֻ���޸� ID С�ڵ��� 1000 ����Ŀ��¼������¼ " + strOutputBiblioRecPath + " ��Ȼ�����ɹ������Ժ��޷���������޸� ";
                            goto ERROR1;
                        }
                    }
                }

            }
            else if (strAction == "change")
            {
                // ֻ��orderȨ�޵����
                if (StringUtil.IsInList("setbiblioinfo", sessioninfo.RightsOrigin) == false
                    && StringUtil.IsInList("order", sessioninfo.RightsOrigin) == true)
                {
                    // ����������ȫ���������ǹ�����ֻ��׷�Ӽ�¼
                    if (IsOrderWorkBiblioDb(strBiblioDbName) == false)
                    {
                        // �ǹ����⡣Ҫ��ԭ����¼������
                        if (String.IsNullOrEmpty(strExistingXml) == false)
                        {
                            strError = "��ǰ�ʻ�ֻ�� order Ȩ�޶�û�� setbiblioinfo Ȩ�ޣ������� change �����޸��Ѿ����ڵ���Ŀ��¼ '"+strBiblioRecPath+"'";
                            goto ERROR1;
                        }
                    }
                }

                {
                    // �ϲ����ϱ�Ŀ���¾���Ŀ��XML��¼
                    // ���ܣ��ų��¼�¼�ж�strLibraryCode���������905�ֶε��޸�
                    // parameters:
                    //      bChangePartDenied   ������α��趨Ϊ true���� strError �з����˹��ڲ����޸ĵ�ע����Ϣ
                    // return:
                    //      -1  error
                    //      0   not delete any fields
                    //      1   deleted some fields
                    nRet = MergeOldNewBiblioRec(
                        strRights,
                        strUnionCatalogStyle,
                        strLibraryCode,
                        "insert,replace,delete",
                        strAccessParameters,
                        strExistingXml,
                        ref strBiblio,
                        ref bChangePartDenied,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (bChangePartDenied == true && string.IsNullOrEmpty(strError) == false)
                        strDeniedComment += " " + strError;
                }

                // 2011/11/30
                nRet = this.SetOperation(
ref strBiblio,
"change",
sessioninfo.UserID,
"",
true,
10,
out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ��Ҫ�ж�·���Ƿ�Ϊ�߱���ĩһ�������ŵ���ʽ��

                this.BiblioLocks.LockForWrite(strBiblioRecPath);

                try
                {
                    lRet = channel.DoSaveTextRes(strBiblioRecPath,
                        strBiblio,
                        false,
                        "content", // ,ignorechecktimestamp
                        baTimestamp,
                        out baOutputTimestamp,
                        out strOutputBiblioRecPath,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {
                            result.Value = -1;
                            result.ErrorInfo = strError;
                            result.ErrorCode = ErrorCode.TimestampMismatch;
                            return result;
                        }
                        goto ERROR1;
                    }
                }
                finally
                {
                    this.BiblioLocks.UnlockForWrite(strBiblioRecPath);
                }
            }
            else if (strAction == "delete"
                || strAction == "onlydeletesubrecord")
            {
                // ֻ��orderȨ�޵����
                if (StringUtil.IsInList("setbiblioinfo", sessioninfo.RightsOrigin) == false
                    && StringUtil.IsInList("order", sessioninfo.RightsOrigin) == true)
                {
                    // ����������ȫ���������ǹ����ⲻ��ɾ����¼
                    if (IsOrderWorkBiblioDb(strBiblioDbName) == false)
                    {
                        // �ǹ����⡣Ҫ��ԭ����¼������
                        strError = "��ǰ�ʻ�ֻ��orderȨ�޶�û��setbiblioinfoȨ�ޣ�������delete����ɾ����Ŀ��¼ '" + strBiblioRecPath + "'";
                        goto ERROR1;
                    }
                }

                if (strAction == "delete")
                {
                    strBiblio = "";

                    // �ϲ����ϱ�Ŀ���¾���Ŀ��XML��¼
                    // ���ܣ��ų��¼�¼�ж�strLibraryCode���������905�ֶε��޸�
                    // parameters:
                    //      bChangePartDenied   ������α��趨Ϊ true���� strError �з����˹��ڲ����޸ĵ�ע����Ϣ
                    // return:
                    //      -1  error
                    //      0   not delete any fields
                    //      1   deleted some fields
                    nRet = MergeOldNewBiblioRec(
                        strRights,
                        strUnionCatalogStyle,
                        strLibraryCode,
                        "delete",
                        strAccessParameters,
                        strExistingXml,
                        ref strBiblio,
                        ref bChangePartDenied,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (bChangePartDenied == true && string.IsNullOrEmpty(strError) == false)
                        strDeniedComment += " " + strError;

                    // ���������ǲ���û���κ�Ԫ���ˡ�������У�˵����ǰȨ�޲�����ɾ�����ǡ�
                    // ����Ѿ�Ϊ�գ��ͱ�ʾ���ؼ����
                    if (String.IsNullOrEmpty(strBiblio) == false)
                    {
                        XmlDocument tempdom = new XmlDocument();
                        try
                        {
                            tempdom.LoadXml(strBiblio);
                        }
                        catch (Exception ex)
                        {
                            strError = "���� MergeOldNewBiblioRec() ������ strBiblio װ�� XmlDocument ʧ��: " + ex.Message;
                            goto ERROR1;
                        }

                        // 2011/11/30
                        // ɾ��ȫ��<operations>Ԫ��
                        XmlNodeList nodes = tempdom.DocumentElement.SelectNodes("//operations");
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            XmlNode node = nodes[i];
                            if (node.ParentNode != null)
                                node.ParentNode.RemoveChild(node);
                        }

                        if (tempdom.DocumentElement.ChildNodes.Count != 0)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "��ǰ�û���Ȩ�޲�����ɾ������MARC�ֶΣ����ɾ���������ܾ����ɸ����޸Ĳ�����";
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }

                    }
                }


                // ���ɾ��������ô�򵥣���Ҫͬʱɾ��������ʵ���¼
                // Ҫ���ֺ�ʵ�嶼��������
                this.BiblioLocks.LockForWrite(strBiblioRecPath);
                try
                {
                    // ̽����Ŀ��¼��û��������ʵ���¼(Ҳ˳�㿴��ʵ���¼�����Ƿ�����ͨ��Ϣ)?
                    List<DeleteEntityInfo> entityinfos = null;
                    string strStyle = "check_borrow_info";
                    long lHitCount = 0;

                    // return:
                    //      -2  not exist entity dbname
                    //      -1  error
                    //      >=0 ������ͨ��Ϣ��ʵ���¼����
                    nRet = SearchChildEntities(channel,
                        strBiblioRecPath,
                        strStyle,
                        sessioninfo.GlobalUser == false ? CheckItemRecord : (Delegate_checkRecord)null,
                        sessioninfo.GlobalUser == false ? sessioninfo.LibraryCodeList : null,
                out lHitCount,
                        out entityinfos,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == -2)
                    {
                        Debug.Assert(entityinfos.Count == 0, "");
                    }

                    // �����ʵ���¼����Ҫ��setentitiesȨ�ޣ�����һͬɾ��ʵ����
                    if (entityinfos != null && entityinfos.Count > 0)
                    {
                        // Ȩ���ַ���
                        if (StringUtil.IsInList("setentities", sessioninfo.RightsOrigin) == false
                            && StringUtil.IsInList("setiteminfo", sessioninfo.RightsOrigin) == false)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "������Ŀ��Ϣ��ɾ��(delete)�������ܾ�������ɾ������Ŀ��¼����������ʵ���¼������ǰ�û����߱�setiteminfo��setentitiesȨ�ޣ�����ɾ�����ǡ�";
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }

                        if (this.DeleteBiblioSubRecords == false)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "������Ŀ��Ϣ��ɾ��(delete)�������ܾ�������ɾ������Ŀ��¼����������ʵ���¼��������ɾ����Ŀ��¼";
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }

                        // bFoundEntities = true;
                    }

                    //
                    // ̽����Ŀ��¼��û�������Ķ�����¼
                    List<DeleteEntityInfo> orderinfos = null;
                    // bool bFoundOrders = false;

                    // return:
                    //      -1  error
                    //      0   not exist entity dbname
                    //      1   exist entity dbname
                    nRet = this.OrderItemDatabase.SearchChildItems(channel,
                        strBiblioRecPath,
                        "check_circulation_info", // ��DeleteEntityInfo�ṹ��*��*����OldRecord����
                        out lHitCount,
                        out orderinfos,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == 0)
                    {
                        Debug.Assert(orderinfos.Count == 0, "");
                    }

                    // ����ж�����¼����Ҫ��setordersȨ�ޣ�����һͬɾ������
                    if (orderinfos != null && orderinfos.Count > 0)
                    {
                        // Ȩ���ַ���
                        if (StringUtil.IsInList("setorders", sessioninfo.RightsOrigin) == false
                            && StringUtil.IsInList("setorderinfo", sessioninfo.RightsOrigin) == false
                            && StringUtil.IsInList("order", sessioninfo.RightsOrigin) == false)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "������Ŀ��Ϣ��ɾ��(delete)�������ܾ�������ɾ������Ŀ��¼���������Ķ�����¼������ǰ�û����߱�order��setorderinfo��setordersȨ�ޣ�����ɾ�����ǡ�";
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }

                        if (this.DeleteBiblioSubRecords == false)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "������Ŀ��Ϣ��ɾ��(delete)�������ܾ�������ɾ������Ŀ��¼���������Ķ�����¼��������ɾ����Ŀ��¼";
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }

                        // bFoundOrders = true;
                    }


                    //
                    // ̽����Ŀ��¼��û���������ڼ�¼
                    List<DeleteEntityInfo> issueinfos = null;
                    // bool bFoundIssues = false;

                    // return:
                    //      -1  error
                    //      0   not exist entity dbname
                    //      1   exist entity dbname
                    nRet = this.IssueItemDatabase.SearchChildItems(channel,
                        strBiblioRecPath,
                        "check_circulation_info", // ��DeleteEntityInfo�ṹ��*��*����OldRecord����
                        out lHitCount,
                        out issueinfos,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == 0)
                    {
                        Debug.Assert(issueinfos.Count == 0, "");
                    }

                    // ������ڼ�¼����Ҫ��setissuesȨ�ޣ�����һͬɾ������
                    if (issueinfos != null && issueinfos.Count > 0)
                    {
                        // Ȩ���ַ���
                        if (StringUtil.IsInList("setissues", sessioninfo.RightsOrigin) == false
                            && StringUtil.IsInList("setissueinfo", sessioninfo.RightsOrigin) == false)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "������Ŀ��Ϣ��ɾ��(delete)�������ܾ�������ɾ������Ŀ��¼�����������ڼ�¼������ǰ�û����߱�setissueinfo��setissuesȨ�ޣ�����ɾ�����ǡ�";
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }

                        if (this.DeleteBiblioSubRecords == false)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "������Ŀ��Ϣ��ɾ��(delete)�������ܾ�������ɾ������Ŀ��¼�����������ڼ�¼��������ɾ����Ŀ��¼";
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }
                        // bFoundIssues = true;
                    }

                    // ̽����Ŀ��¼��û����������ע��¼
                    List<DeleteEntityInfo> commentinfos = null;
                    // return:
                    //      -1  error
                    //      0   not exist entity dbname
                    //      1   exist entity dbname
                    nRet = this.CommentItemDatabase.SearchChildItems(channel,
                        strBiblioRecPath,
                        "check_circulation_info", // ��DeleteEntityInfo�ṹ��*��*����OldRecord����
                        out lHitCount,
                        out commentinfos,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (nRet == 0)
                    {
                        Debug.Assert(commentinfos.Count == 0, "");
                    }

                    // �������ע��¼����Ҫ��setcommentinfoȨ�ޣ�����һͬɾ������
                    if (commentinfos != null && commentinfos.Count > 0)
                    {
                        // Ȩ���ַ���
                        if (StringUtil.IsInList("setcommentinfo", sessioninfo.RightsOrigin) == false)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "������Ŀ��Ϣ��ɾ��(delete)�������ܾ�������ɾ������Ŀ��¼������������ע��¼������ǰ�û����߱�setcommentinfoȨ�ޣ�����ɾ�����ǡ�";
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }

                        if (this.DeleteBiblioSubRecords == false)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "������Ŀ��Ϣ��ɾ��(delete)�������ܾ�������ɾ������Ŀ��¼������������ע��¼��������ɾ����Ŀ��¼";
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }
                    }


                    baOutputTimestamp = null;

                    if (strAction == "delete")
                    {
                        // ɾ����Ŀ��¼
                        lRet = channel.DoDeleteRes(strBiblioRecPath,
                            baTimestamp,
                            out baOutputTimestamp,
                            out strError);
                        if (lRet == -1)
                        {
                            if (channel.ErrorCode == ChannelErrorCode.NotFound
                                && (entityinfos.Count > 0 || orderinfos.Count > 0 || issueinfos.Count > 0)
                                )
                            {
                                bBiblioNotFound = true;
                                // strWarning = "��Ŀ��¼ '" + strBiblioRecPath + "' ������";
                            }
                            else
                                goto ERROR1;
                        }
                    }

                    strBiblio = ""; // �������Ѳ�����Ϣд�������־�� <record>Ԫ�� 2013/3/11
                    baOutputTimestamp = null;

                    // ɾ������ͬһ��Ŀ��¼��ȫ��ʵ���¼
                    // ������Ҫ�ṩEntityInfo����İ汾
                    // return:
                    //      -1  error
                    //      0   û���ҵ�������Ŀ��¼���κ�ʵ���¼�����Ҳ���޴�ɾ��
                    //      >0  ʵ��ɾ����ʵ���¼��
                    nRet = DeleteBiblioChildEntities(channel,
                        entityinfos,
                        domOperLog,
                        out strError);
                    if (nRet == -1 && bBiblioNotFound == false)
                    {
                        // TODO: ����Ŀ��¼���ж�����Դʱ��DoSaveTextRes���޷��ָ���

                        // ���±����ȥ��Ŀ��¼, �Ա㻹���´�����ɾ���Ļ���
                        // �����Ҫע�⣬ǰ����ɾ��ʧ�ܺ󣬲�Ҫ�����˸���timestamp
                        if (strAction == "delete")
                        {
                            string strError_1 = "";
                            lRet = channel.DoSaveTextRes(strBiblioRecPath,
                                strExistingXml,
                                false,
                                "content", // ,ignorechecktimestamp
                                null,   // timestamp
                                out baOutputTimestamp,
                                out strOutputBiblioRecPath,
                                out strError_1);
                            if (lRet == -1)
                            {
                                strError = "ɾ���¼�ʵ���¼ʧ��: " + strError + "��\r\n������ͼ����д�ظո���ɾ������Ŀ��¼ '"+strBiblioRecPath+"' �Ĳ���Ҳ�����˴���: " + strError_1;
                                goto ERROR1;
                            }
                        }

                        goto ERROR1;
                    }

                    // return:
                    //      -1  error
                    //      0   û���ҵ�������Ŀ��¼���κ�ʵ���¼�����Ҳ���޴�ɾ��
                    //      >0  ʵ��ɾ����ʵ���¼��
                    nRet = this.OrderItemDatabase.DeleteBiblioChildItems(sessioninfo.Channels,
                        orderinfos,
                        domOperLog,
                        out strError);
                    if (nRet == -1 && bBiblioNotFound == false)
                    {
                        // ���±����ȥ��Ŀ��¼, �Ա㻹���´�����ɾ���Ļ���
                        // �����Ҫע�⣬ǰ����ɾ��ʧ�ܺ󣬲�Ҫ�����˸���timestamp
                        try
                        {
                            string strError_1 = "";
                            lRet = channel.DoSaveTextRes(strBiblioRecPath,
                                strExistingXml,
                                false,
                                "content", // ,ignorechecktimestamp
                                null,   // timestamp
                                out baOutputTimestamp,
                                out strOutputBiblioRecPath,
                                out strError_1);
                            if (lRet == -1)
                            {
                                strError = "ɾ���¼�������¼ʧ��: " + strError + "��\r\n������ͼ����д�ظո���ɾ������Ŀ��¼ '" + strBiblioRecPath + "' �Ĳ���Ҳ�����˴���: " + strError_1;
                                goto ERROR1;
                            }
                            goto ERROR1;
                        }
                        finally
                        {
                            if (entityinfos.Count > 0)
                                strError += "��\r\n��ɾ���� " + entityinfos.Count.ToString() + " �����¼�Ѿ��޷��ָ�";
                        }
                    }

                    // return:
                    //      -1  error
                    //      0   û���ҵ�������Ŀ��¼���κ�ʵ���¼�����Ҳ���޴�ɾ��
                    //      >0  ʵ��ɾ����ʵ���¼��
                    nRet = this.IssueItemDatabase.DeleteBiblioChildItems(sessioninfo.Channels,
                        issueinfos,
                        domOperLog,
                        out strError);
                    if (nRet == -1 && bBiblioNotFound == false)
                    {
                        // ���±����ȥ��Ŀ��¼, �Ա㻹���´�����ɾ���Ļ���
                        // �����Ҫע�⣬ǰ����ɾ��ʧ�ܺ󣬲�Ҫ�����˸���timestamp
                        try
                        {
                            string strError_1 = "";
                            lRet = channel.DoSaveTextRes(strBiblioRecPath,
                                strExistingXml,
                                false,
                                "content", // ,ignorechecktimestamp
                                null,   // timestamp
                                out baOutputTimestamp,
                                out strOutputBiblioRecPath,
                                out strError_1);
                            if (lRet == -1)
                            {
                                strError = "ɾ���¼��ڼ�¼ʧ��: " + strError + "��\r\n������ͼ����д�ظո���ɾ������Ŀ��¼ '" + strBiblioRecPath + "' �Ĳ���Ҳ�����˴���: " + strError_1;
                                goto ERROR1;
                            }
                            goto ERROR1;
                        }
                        finally
                        {
                            if (entityinfos.Count > 0)
                                strError += "��\r\n��ɾ���� " + entityinfos.Count.ToString() + " �����¼�Ѿ��޷��ָ�";
                            if (orderinfos.Count > 0)
                                strError += "��\r\n��ɾ���� " + orderinfos.Count.ToString() + " ��������¼�Ѿ��޷��ָ�";
                        }
                    }

                    // return:
                    //      -1  error
                    //      0   û���ҵ�������Ŀ��¼���κ�ʵ���¼�����Ҳ���޴�ɾ��
                    //      >0  ʵ��ɾ����ʵ���¼��
                    nRet = this.CommentItemDatabase.DeleteBiblioChildItems(sessioninfo.Channels,
                        commentinfos,
                        domOperLog,
                        out strError);
                    if (nRet == -1 && bBiblioNotFound == false)
                    {
                        // ���±����ȥ��Ŀ��¼, �Ա㻹���´�����ɾ���Ļ���
                        // �����Ҫע�⣬ǰ����ɾ��ʧ�ܺ󣬲�Ҫ�����˸���timestamp
                        try
                        {
                            string strError_1 = "";
                            lRet = channel.DoSaveTextRes(strBiblioRecPath,
                                strExistingXml,
                                false,
                                "content", // ,ignorechecktimestamp
                                null,   // timestamp
                                out baOutputTimestamp,
                                out strOutputBiblioRecPath,
                                out strError_1);
                            if (lRet == -1)
                            {
                                strError = "ɾ���¼���ע��¼ʧ��: " + strError + "��\r\n������ͼ����д�ظո���ɾ������Ŀ��¼ '" + strBiblioRecPath + "' �Ĳ���Ҳ�����˴���: " + strError_1;
                                goto ERROR1;
                            }
                            goto ERROR1;
                        }
                        finally
                        {
                            if (entityinfos.Count > 0)
                                strError += "��\r\n��ɾ���� " + entityinfos.Count.ToString() + " �����¼�Ѿ��޷��ָ�";
                            if (orderinfos.Count > 0)
                                strError += "��\r\n��ɾ���� " + orderinfos.Count.ToString() + " ��������¼�Ѿ��޷��ָ�";
                            if (issueinfos.Count > 0)
                                strError += "��\r\n��ɾ���� " + issueinfos.Count.ToString() + " ���ڼ�¼�Ѿ��޷��ָ�";
                        }
                    }
                }
                finally
                {
                    this.BiblioLocks.UnlockForWrite(strBiblioRecPath);
                }
            }
            else if (strAction == "onlydeletebiblio")
            {
                // ֻ��orderȨ�޵����
                if (StringUtil.IsInList("setbiblioinfo", sessioninfo.RightsOrigin) == false
                    && StringUtil.IsInList("order", sessioninfo.RightsOrigin) == true)
                {
                    // ����������ȫ���������ǹ����ⲻ��ɾ����¼
                    if (IsOrderWorkBiblioDb(strBiblioDbName) == false)
                    {
                        // �ǹ����⡣Ҫ��ԭ����¼������
                        strError = "��ǰ�ʻ�ֻ��orderȨ�޶�û��setbiblioinfoȨ�ޣ�������onlydeletebiblio����ɾ����Ŀ��¼ '" + strBiblioRecPath + "'";
                        goto ERROR1;
                    }
                }

                {
                    strBiblio = "";

                    // �ϲ����ϱ�Ŀ���¾���Ŀ��XML��¼
                    // ���ܣ��ų��¼�¼�ж�strLibraryCode���������905�ֶε��޸�
                    // parameters:
                    //      bChangePartDenied   ������α��趨Ϊ true���� strError �з����˹��ڲ����޸ĵ�ע����Ϣ
                    // return:
                    //      -1  error
                    //      0   not delete any fields
                    //      1   deleted some fields
                    nRet = MergeOldNewBiblioRec(
                        strRights,
                        strUnionCatalogStyle,
                        strLibraryCode,
                        "delete",
                        strAccessParameters,
                        strExistingXml,
                        ref strBiblio,
                        ref bChangePartDenied,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (bChangePartDenied == true && string.IsNullOrEmpty(strError) == false)
                        strDeniedComment += " " + strError;

                    // ���������ǲ���û���κ�Ԫ���ˡ�������У�˵����ǰȨ�޲�����ɾ�����ǡ�
                    // ����Ѿ�Ϊ�գ��ͱ�ʾ���ؼ����
                    if (String.IsNullOrEmpty(strBiblio) == false)
                    {
                        XmlDocument tempdom = new XmlDocument();
                        try
                        {
                            tempdom.LoadXml(strBiblio);
                        }
                        catch (Exception ex)
                        {
                            strError = "���� MergeOldNewBiblioRec() ������ strBiblio װ�� XmlDocument ʧ��: " + ex.Message;
                            goto ERROR1;
                        }

                        // 2011/12/9
                        // ɾ��ȫ��<operations>Ԫ��
                        XmlNodeList nodes = tempdom.DocumentElement.SelectNodes("//operations");
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            XmlNode node = nodes[i];
                            if (node.ParentNode != null)
                                node.ParentNode.RemoveChild(node);
                        }

                        if (tempdom.DocumentElement.ChildNodes.Count != 0)
                        {
                            result.Value = -1;
                            result.ErrorInfo = "��ǰ�û���Ȩ�޲�����ɾ������MARC�ֶΣ����ɾ���������ܾ����ɸ����޸Ĳ�����";
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }
                    }
                }

                // ����Ҫͬʱɾ��������ʵ���¼
                this.BiblioLocks.LockForWrite(strBiblioRecPath);
                try
                {
                    baOutputTimestamp = null;

                    // ɾ����Ŀ��¼
                    lRet = channel.DoDeleteRes(strBiblioRecPath,
                        baTimestamp,
                        out baOutputTimestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        // ֻɾ����Ŀ��¼�����������Ŀ��¼ȴ�����ڣ�Ҫ����
                        goto ERROR1;
                    }
                }
                finally
                {
                    this.BiblioLocks.UnlockForWrite(strBiblioRecPath);
                }
            }
            else
            {
                strError = "δ֪��strAction����ֵ '" + strAction + "'";
                goto ERROR1;
            }

            if (string.IsNullOrEmpty(strOutputBiblioRecPath) == false)
            {
                XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                    "record", strBiblio);
                DomUtil.SetAttr(node, "recPath", strOutputBiblioRecPath);
            }

            DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                sessioninfo.UserID);
            DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                strOperTime);

            // д����־
            nRet = this.OperLog.WriteOperLog(domOperLog,
                sessioninfo.ClientAddress,
                out strError);
            if (nRet == -1)
            {
                strError = "SetBiblioInfo() API д����־ʱ��������: " + strError;
                goto ERROR1;
            }

            result.Value = 0;
            if (bBiblioNotFound == true)
                result.ErrorInfo = "��Ȼ��Ŀ��¼ '" + strBiblioRecPath + "' �����ڣ�����ɾ��������ʵ���¼�ɹ���";  // ��Ȼ...����...
            // 2013/3/5
            if (bChangePartDenied == true)
            {
                result.ErrorCode = ErrorCode.PartialDenied;
                if (string.IsNullOrEmpty(strDeniedComment) == false)
                {
                    if (string.IsNullOrEmpty(result.ErrorInfo) == false)
                        result.ErrorInfo += " ; ";
                    result.ErrorInfo += strDeniedComment;
                }
            }
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        // �������ֵ����0�����ж�ѭ��������
        int CheckItemRecord(string strRecPath,
            XmlDocument dom,
            byte[] baTimestamp,
            object param,
            out string strError)
        {
            strError = "";

            string strLibraryCodeList = (string)param;
            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == true)
                return 0;

            string strLocation = DomUtil.GetElementText(dom.DocumentElement, "location");
            strLocation = StringUtil.GetPureLocationString(strLocation);

            string strLibraryCode = "";
            string strPureName = "";

            // ����
            ParseCalendarName(strLocation,
        out strLibraryCode,
        out strPureName);

            if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
            {
                strError = "���¼�� '"+strRecPath+"' �Ĺݲصص� '"+strLocation+"' ���ڵ�ǰ�û���Ͻ��Χ '"+strLibraryCodeList+"' �ڣ��������ܾ�";
                return -1;
            }

            return 0;
        }

        // ���ƻ����ƶ���Ŀ��¼
        // parameters:
        //      strAction   ������Ϊ"onlycopybiblio" "onlymovebiblio" "copy" "move" ֮һ 
        //      strBiblioType   Ŀǰֻ����xmlһ��
        //      strBiblio   Դ��Ŀ��¼��Ŀǰ��Ҫ��null����
        //      baTimestamp Դ��¼��ʱ���
        //      strNewBiblio    ��Ҫ��Ŀ���¼�и��µ����ݡ���� == null����ʾ���������
        //      strMergeStyle   ��κϲ�������Ŀ��¼��Ԫ���ݲ���? reserve_source / reserve_target / missing_source_subrecord / overwrite_target_subrecord�� �ձ�ʾ reserve_source + combine_subrecord
        //                      reserve_source ��ʾ����Դ��Ŀ��¼; reserve_target ��ʾ����Ŀ����Ŀ��¼
        //                      missing_source_subrecord ��ʾ��ʧ����Դ���¼���¼(����Ŀ��ԭ�����¼���¼); overwrite_target_subrecord ��ʾ��������Դ���¼���¼��ɾ��Ŀ���¼ԭ�����¼���¼(ע���˹�����ʱû��ʵ��); combine_subrecord ��ʾ�����Դ��Ŀ����¼���¼
        //      strOutputBiblioRecPath �������Ŀ��¼·������strBiblioRecPath��ĩ��Ϊ�ʺţ���ʾ׷�ӱ�����Ŀ��¼��ʱ�򣬱���������ʵ�ʱ������Ŀ��¼·��
        //      baOutputTimestamp   ������ɺ��µ�ʱ���
        // result.Value:
        //      -1  ����
        //      0   �ɹ���û�о�����Ϣ��
        //      1   �ɹ����о�����Ϣ��������Ϣ�� result.ErrorInfo ��
        public LibraryServerResult CopyBiblioInfo(
            SessionInfo sessioninfo,
            string strAction,
            string strBiblioRecPath,
            string strBiblioType,
            string strBiblio,
            byte[] baTimestamp,
            string strNewBiblioRecPath,
            string strNewBiblio,
            string strMergeStyle,
            out string strOutputBiblio,
            out string strOutputBiblioRecPath,
            out byte[] baOutputTimestamp)
        {
            string strError = "";
            long lRet = 0;
            int nRet = 0;

            strOutputBiblioRecPath = "";
            baOutputTimestamp = null;

            strOutputBiblio = "";

            LibraryServerResult result = new LibraryServerResult();

            if (StringUtil.IsInList("overwrite_target_subrecord", strMergeStyle) == true)
            {
                strError = "strMergeStyle �е� overwrite_target_subrecord ��δʵ��";
                goto ERROR1;
            }

            bool bChangePartDenied = false; // �޸Ĳ������ֱ��ܾ�
            string strDeniedComment = "";   // ���ڲ����ֶα��ܾ���ע��

            string strLibraryCodeList = sessioninfo.LibraryCodeList;

            // ������
            if (strAction != null)
                strAction = strAction.ToLower();

            if (strAction != "onlymovebiblio"
                && strAction != "onlycopybiblio"
                && strAction != "copy"
                && strAction != "move")
            {
                strError = "strAction����ֵӦ��Ϊonlymovebiblio/onlycopybiblio/move/copy֮һ";
                goto ERROR1;
            }

            strBiblioType = strBiblioType.ToLower();
            if (strBiblioType != "xml")
            {
                strError = "strBiblioType����Ϊ\"xml\"";
                goto ERROR1;
            }

            {
                if (this.TestMode == true || sessioninfo.TestMode == true)
                {
                    // �������ģʽ
                    // return:
                    //      -1  �����̳���
                    //      0   ����ͨ��
                    //      1   ������ͨ��
                    nRet = CheckTestModePath(strBiblioRecPath,
                        out strError);
                    if (nRet != 0)
                    {
                        strError = "����/�ƶ���Ŀ��¼�Ĳ������ܾ�: " + strError;
                        goto ERROR1;
                    }
                }
            }

            string strUnionCatalogStyle = "";
            string strAccessParameters = "";
            bool bRightVerified = false;

            // TODO: Ҳ��Ҫ��� strNewBiblioRecPath

            // ������ݿ�·���������ǲ����Ѿ����涨��ı�Ŀ�⣿
            if (String.IsNullOrEmpty(strBiblioRecPath) == false)
            {
                string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);

                if (this.IsBiblioDbName(strBiblioDbName) == false)
                {
                    strError = "��Ŀ��¼·�� '" + strBiblioRecPath + "' �а��������ݿ��� '" + strBiblioDbName + "' ���ǺϷ�����Ŀ����";
                    goto ERROR1;
                }

#if NO
                if (this.TestMode == true)
                {
                    string strID = ResPath.GetRecordId(strBiblioRecPath);
                    if (StringUtil.IsPureNumber(strID) == true)
                    {
                        long v = 0;
                        long.TryParse(strID, out v);
                        if (v > 1000)
                        {
                            strError = "dp2Library XE ����ģʽ��ֻ���޸� ID С�ڵ��� 1000 ����Ŀ��¼";
                            goto ERROR1;
                        }
                    }
                }
#endif

                ItemDbCfg cfg = null;
                cfg = GetBiblioDbCfg(strBiblioDbName);
                Debug.Assert(cfg != null, "");
                strUnionCatalogStyle = cfg.UnionCatalogStyle;

                // ����ȡȨ��
                if (String.IsNullOrEmpty(sessioninfo.Access) == false)
                {
                    // return:
                    //      null    ָ���Ĳ������͵�Ȩ��û�ж���
                    //      ""      ������ָ�����͵Ĳ���Ȩ�ޣ����Ƿ񶨵Ķ���
                    //      ����      Ȩ���б�* ��ʾͨ���Ȩ���б�
                    string strActionList = GetDbOperRights(sessioninfo.Access,
                        strBiblioDbName,
                        "setbiblioinfo");
                    if (strActionList == null)
                    {
                        // �����ǲ��ǹ��� setbiblioinfo ���κ�Ȩ�޶�û�ж���?
                        strActionList = GetDbOperRights(sessioninfo.Access,
                            "",
                            "setbiblioinfo");
                        if (strActionList == null)
                        {
                            // 2014/3/12
                            // TODO: ������ʾ"��û��... Ҳû�� ..."
                            goto CHECK_RIGHTS_2;
                        }
                        else
                        {
                            strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strBiblioDbName + "' ִ�� setbiblioinfo " + strAction + " �����Ĵ�ȡȨ��";
                            result.Value = -1;
                            result.ErrorInfo = strError;
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }
#if NO
                        strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strBiblioDbName + "' ִ�� setbiblioinfo " + strAction + " �����Ĵ�ȡȨ��";
                        result.Value = -1;
                        result.ErrorInfo = strError;
                        result.ErrorCode = ErrorCode.AccessDenied;
                        return result;
#endif
                    }
                    if (strActionList == "*")
                    {
                        // ͨ��
                    }
                    else
                    {
                        if (IsInAccessList(strAction, strActionList, out strAccessParameters) == false)
                        {
                            strError = "��ǰ�û� '" + sessioninfo.UserID + "' ���߱� ������ݿ� '" + strBiblioDbName + "' ִ�� setbiblioinfo " + strAction + " �����Ĵ�ȡȨ��";
                            result.Value = -1;
                            result.ErrorInfo = strError;
                            result.ErrorCode = ErrorCode.AccessDenied;
                            return result;
                        }
                    }

                    bRightVerified = true;
                }
            }

        CHECK_RIGHTS_2:
            if (bRightVerified == false)
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("setbiblioinfo", sessioninfo.RightsOrigin) == false
                    && StringUtil.IsInList("order", sessioninfo.RightsOrigin) == false)
                {
                    result.Value = -1;
                    result.ErrorInfo = "������Ŀ��Ϣ���ܾ������߱�order��setbiblioinfoȨ�ޡ�";
                    result.ErrorCode = ErrorCode.AccessDenied;
                    return result;
                }
            }
            // TODO: ��Ҫ����ļ�飬���������������MARC��ʽ�ǲ���������ݿ�Ҫ��ĸ�ʽ��


            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "channel == null";
                goto ERROR1;
            }

            // ׼����־DOM
            XmlDocument domOperLog = new XmlDocument();
            domOperLog.LoadXml("<root />");
            // �������漰�����߿⣬����û��<libraryCode>Ԫ��
            DomUtil.SetElementText(domOperLog.DocumentElement, "operation",
                "setBiblioInfo");
            DomUtil.SetElementText(domOperLog.DocumentElement, "action",
                strAction);

            string strOperTime = this.Clock.GetClock();

            string strExistingSourceXml = "";
            byte[] exist_source_timestamp = null;

            if (strAction == "onlymovebiblio"
                || strAction == "onlycopybiblio"
                || strAction == "copy"
                || strAction == "move")
            {
                string strMetaData = "";
                string strOutputPath = "";

                // �ȶ������ݿ��д�λ�õ����м�¼
                lRet = channel.GetRes(strBiblioRecPath,
                    out strExistingSourceXml,
                    out strMetaData,
                    out exist_source_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.NotFound)
                    {
                        goto SKIP_MEMO_OLDRECORD;
                    }
                    else
                    {
                        strError = "������Ŀ��Ϣ��������, �ڶ���ԭ�м�¼�׶�:" + strError;
                        goto ERROR1;
                    }
                }

                if (strBiblioRecPath != strOutputPath)
                {
                    strError = "����·�� '" + strBiblioRecPath + "' ����ԭ�м�¼ʱ�����ַ��ص�·�� '" + strOutputPath + "' ��ǰ�߲�һ��";
                    goto ERROR1;
                }


                XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                        "oldRecord", strExistingSourceXml);
                DomUtil.SetAttr(node, "recPath", strBiblioRecPath);


                // TODO: ����Ѵ��ڵ�XML��¼�У�MARC�������ĵ�������ô������Ŀ��¼
                // ���洢��������Ϣ����ʱ����Ҫ��ǰ��������XML��¼���Ѵ��ڵļ�¼���кϲ�����
                // ��ֹóȻ�������ĵ����µ�������Ϣ��
            }

        SKIP_MEMO_OLDRECORD:

            // bool bBiblioNotFound = false;

            string strRights = "";
            
            if (sessioninfo.Account != null)
                strRights = sessioninfo.Account.Rights;

            if (strAction == "onlycopybiblio"
                || strAction == "onlymovebiblio"
                || strAction == "copy"
                || strAction == "move")
            {
                if (string.IsNullOrEmpty(strNewBiblio) == false)
                {
                    // �۲�ʱ����Ƿ����仯
                    nRet = ByteArray.Compare(baTimestamp, exist_source_timestamp);
                    if (nRet != 0)
                    {
                        strError = "�ƶ����Ʋ�����������Դ��¼�Ѿ��������޸�(ʱ�����ƥ�䡣��ǰ�ύ��ʱ���: '" + ByteArray.GetHexTimeStampString(baTimestamp) + "', �������ԭ��¼��ʱ���: '" + ByteArray.GetHexTimeStampString(exist_source_timestamp) + "')";
                        goto ERROR1;
                    }
                }

                // TODO: ���Ŀ����Ŀ��¼·����֪������Ҫ������·����������ע���С�ŵ����˳�μ�������������

                this.BiblioLocks.LockForWrite(strBiblioRecPath);
                try
                {
                    if (String.IsNullOrEmpty(strNewBiblio) == false)
                    {
                        // �ϲ����ϱ�Ŀ���¾���Ŀ��XML��¼
                        // ���ܣ��ų��¼�¼�ж�strLibraryCode���������905�ֶε��޸�
                        // parameters:
                        //      bChangePartDenied   ������α��趨Ϊ true���� strError �з����˹��ڲ����޸ĵ�ע����Ϣ
                        // return:
                        //      -1  error
                        //      0   not delete any fields
                        //      1   deleted some fields
                        nRet = MergeOldNewBiblioRec(
                            strRights,
                            strUnionCatalogStyle,
                            strLibraryCodeList,
                            "insert,replace,delete",
                            strAccessParameters,
                            strExistingSourceXml,
                            ref strNewBiblio,
                            ref bChangePartDenied,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (bChangePartDenied == true && string.IsNullOrEmpty(strError) == false)
                            strDeniedComment += " " + strError;
                        /*
                        // 2011/11/30
                        nRet = this.SetOperation(
        ref strNewBiblio,
        strAction,
        sessioninfo.UserID,
        "source: " + strBiblioRecPath,
        out strError);
                        if (nRet == -1)
                            goto ERROR1;
                         * */
                    }

                    nRet = DoBiblioOperMove(
                        strAction,
                        sessioninfo,
                        channel,
                        strBiblioRecPath,
                        strExistingSourceXml,
                        strNewBiblioRecPath,
                        strNewBiblio,    // �Ѿ�����MergeԤ������¼�¼XML
                        strMergeStyle,
                        out strOutputBiblio,
                        out baOutputTimestamp,
                        out strOutputBiblioRecPath,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if ((strAction == "copy" || strAction == "move")
                        && StringUtil.IsInList("missing_source_subrecord", strMergeStyle) == false)
                    {
                        string strWarning = "";
                        // 
                        // ����ǰ���ٶ���Ŀ��¼�Ѿ�������
                        // parameters:
                        //      strAction   copy / move
                        // return:
                        //      -2  Ȩ�޲���
                        //      -1  ����
                        //      0   �ɹ�
                        nRet = DoCopySubRecord(
                            sessioninfo,
                            strAction,
                            strBiblioRecPath,
                            strOutputBiblioRecPath,
                            domOperLog,
                            out strWarning,
                            out strError);
                        if (nRet == -1)
                        {
                            // Undo Copy biblio record

                            // �ƶ���ȥ
                            if (strAction == "onlymovebiblio" || strAction == "move")
                            {
                                byte[] output_timestamp = null;
                                string strTempOutputRecPath = "";
                                string strError_1 = "";

                                lRet = channel.DoCopyRecord(strOutputBiblioRecPath,
                                     strBiblioRecPath,
                                     true,   // bDeleteSourceRecord
                                     out output_timestamp,
                                     out strTempOutputRecPath,
                                     out strError_1);
                                if (lRet == -1)
                                {
                                    this.WriteErrorLog("���� '" + strBiblioRecPath + "' �����Ĳ��¼ʱ����: " + strError + "������Undo��ʱ��(�� '" + strOutputBiblioRecPath + "' ���ƻ� '" + strBiblioRecPath + "')ʧ��: " + strError_1);
                                }
                            }
                            else if (strAction == "onlycopybiblio" || strAction == "copy")
                            {
                                // ɾ���ոո��Ƶ�Ŀ���¼
                                string strError_1 = "";
                                int nRedoCount = 0;
                            REDO_DELETE:
                                lRet = channel.DoDeleteRes(strOutputBiblioRecPath,
                                    baTimestamp,
                                    out baOutputTimestamp,
                                    out strError_1);
                                if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                                {
                                    if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
                                        && nRedoCount < 10)
                                    {
                                        baTimestamp = baOutputTimestamp;
                                        nRedoCount++;
                                        goto REDO_DELETE;
                                    }
                                    this.WriteErrorLog("���� '" + strBiblioRecPath + "' �����Ĳ��¼ʱ����: " + strError + "������Undo��ʱ��(ɾ����¼ '" + strOutputBiblioRecPath + "')ʧ��: " + strError_1);
                                }
                            }
                            goto ERROR1;
                        }
                        result.ErrorInfo = strWarning;
                    }

                }
                finally
                {
                    this.BiblioLocks.UnlockForWrite(strBiblioRecPath);
                }
            }
            else
            {
                strError = "δ֪��strAction����ֵ '" + strAction + "'";
                goto ERROR1;
            }

            {
                // ע�����strNewBiblioΪ�գ���������������˸��ƣ���û����Ŀ���¼дʲô������
                // �������־��¼��Ҫ�鵽���׸�����ʲô���ݣ����Կ�<oldRecord>Ԫ�ص��ı�����
                // ע: ��� strMergeStyle Ϊ reserve_target�� ��Ҫ����һ�����λ���Ѿ����ڵļ�¼
                XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                        "record", string.IsNullOrEmpty(strOutputBiblio) == false ? strOutputBiblio : strNewBiblio);
                DomUtil.SetAttr(node, "recPath", strOutputBiblioRecPath);
            }

            // 2015/1/21
            DomUtil.SetElementText(domOperLog.DocumentElement, "mergeStyle",
    strMergeStyle);

            DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                sessioninfo.UserID);
            DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                strOperTime);

            // д����־
            nRet = this.OperLog.WriteOperLog(domOperLog,
                sessioninfo.ClientAddress,
                out strError);
            if (nRet == -1)
            {
                strError = "CopyBiblioInfo() API д����־ʱ��������: " + strError;
                goto ERROR1;
            }

            if (string.IsNullOrEmpty(result.ErrorInfo) == true)
                result.Value = 0;   // û�о���
            else
                result.Value = 1;   // �о���

            // 2013/3/5
            if (bChangePartDenied == true)
            {
                result.ErrorCode = ErrorCode.PartialDenied;
                if (string.IsNullOrEmpty(strDeniedComment) == false)
                {
                    if (string.IsNullOrEmpty(result.ErrorInfo) == false)
                        result.ErrorInfo += " ; ";
                    result.ErrorInfo += strDeniedComment;
                }
            }

            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        // 2011/4/24
        // ����ǰ���ٶ���Ŀ��¼�Ѿ�������
        // parameters:
        //      strAction   copy / move
        // return:
        //      -2  Ȩ�޲���
        //      -1  ����
        //      0   �ɹ�
        int DoCopySubRecord(
            SessionInfo sessioninfo,
            string strAction,
            string strBiblioRecPath,
            string strNewBiblioRecPath,
            XmlDocument domOperLog,
            out string strWarning,
            out string strError)
        {
            strError = "";
            strWarning = "";

            int nRet = 0;

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            // 1)
            // ̽����Ŀ��¼��û��������ʵ���¼(Ҳ˳�㿴��ʵ���¼�����Ƿ�����ͨ��Ϣ)?
            List<DeleteEntityInfo> entityinfos = null;
            long lHitCount = 0;

            // TODO: ֻҪ��ü�¼·�����ɣ���Ϊ����������CopyRecord����
            // return:
            //      -2  not exist entity dbname
            //      -1  error
            //      >=0 ������ͨ��Ϣ��ʵ���¼����
            nRet = SearchChildEntities(channel,
                strBiblioRecPath,
                "count_borrow_info,return_record_xml",
                sessioninfo.GlobalUser == false ? CheckItemRecord : (Delegate_checkRecord)null,
                sessioninfo.GlobalUser == false ? sessioninfo.LibraryCodeList : null,
                out lHitCount,
                out entityinfos,
                out strError);
            if (nRet == -1)
                return -1;

            if (nRet == -2)
            {
                Debug.Assert(entityinfos.Count == 0, "");
            }

            int nBorrowInfoCount = nRet;

            // �����ʵ���¼����Ҫ��setentitiesȨ�ޣ����ܴ��������ƶ�ʵ����
            if (entityinfos != null && entityinfos.Count > 0)
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("setentities", sessioninfo.RightsOrigin) == false
                    && StringUtil.IsInList("setiteminfo", sessioninfo.RightsOrigin) == false)
                {
                    strError = "����(�ƶ�)��Ŀ��Ϣ�Ĳ������ܾ��������������Ŀ��¼����������ʵ���¼������ǰ�û����߱�setiteminfo��setentitiesȨ�ޣ����ܸ��ƻ����ƶ����ǡ�";
                    return -2;
                }
            }

            // 2)
            // ̽����Ŀ��¼��û�������Ķ�����¼
            List<DeleteEntityInfo> orderinfos = null;
            // return:
            //      -1  error
            //      0   not exist entity dbname
            //      1   exist entity dbname
            nRet = this.OrderItemDatabase.SearchChildItems(channel,
                strBiblioRecPath,
                "return_record_xml,check_circulation_info",
                out lHitCount,
                out orderinfos,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 0)
            {
                Debug.Assert(orderinfos.Count == 0, "");
            }

            // ����ж�����¼����Ҫ��setordersȨ�ޣ����ܴ��������ƶ�����
            if (orderinfos != null && orderinfos.Count > 0)
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("setorders", sessioninfo.RightsOrigin) == false
                    && StringUtil.IsInList("setorderinfo", sessioninfo.RightsOrigin) == false
                    && StringUtil.IsInList("order", sessioninfo.RightsOrigin) == false)
                {
                    strError = "����(�ƶ�)��Ŀ��Ϣ�Ĳ������ܾ��������������Ŀ��¼���������Ķ�����¼������ǰ�û����߱�order��setorderinfo��setordersȨ�ޣ����ܸ��ƻ��ƶ����ǡ�";
                    return -2;
                }
            }


            // 3)
            // ̽����Ŀ��¼��û���������ڼ�¼
            List<DeleteEntityInfo> issueinfos = null;

            // return:
            //      -1  error
            //      0   not exist entity dbname
            //      1   exist entity dbname
            nRet = this.IssueItemDatabase.SearchChildItems(channel,
                strBiblioRecPath,
                "return_record_xml,check_circulation_info",
                out lHitCount,
                out issueinfos,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 0)
            {
                Debug.Assert(issueinfos.Count == 0, "");
            }

            // ������ڼ�¼����Ҫ��setissuesȨ�ޣ����ܴ��������ƶ�����
            if (issueinfos != null && issueinfos.Count > 0)
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("setissues", sessioninfo.RightsOrigin) == false
                    && StringUtil.IsInList("setissueinfo", sessioninfo.RightsOrigin) == false)
                {
                    strError = "����(�ƶ�)��Ŀ��Ϣ�Ĳ������ܾ��������������Ŀ��¼�����������ڼ�¼������ǰ�û����߱�setissueinfo��setissuesȨ�ޣ����ܸ��ƻ��ƶ����ǡ�";

                    return -2;
                }
            }

            // 4)
            // ̽����Ŀ��¼��û����������ע��¼
            List<DeleteEntityInfo> commentinfos = null;
            // return:
            //      -1  error
            //      0   not exist entity dbname
            //      1   exist entity dbname
            nRet = this.CommentItemDatabase.SearchChildItems(channel,
                strBiblioRecPath,
                "return_record_xml,check_circulation_info",
                out lHitCount,
                out commentinfos,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 0)
            {
                Debug.Assert(commentinfos.Count == 0, "");
            }

            // �������ע��¼����Ҫ��setcommentinfoȨ�ޣ����ܴ��������ƶ�����
            if (commentinfos != null && commentinfos.Count > 0)
            {
                // Ȩ���ַ���
                if (StringUtil.IsInList("setcommentinfo", sessioninfo.RightsOrigin) == false)
                {
                    strError = "����(�ƶ�)��Ŀ��Ϣ�Ĳ������ܾ��������������Ŀ��¼������������ע��¼������ǰ�û����߱�setcommentinfoȨ�ޣ����ܸ��ƻ��ƶ����ǡ�";
                    return -2;
                }
            }


            // ** �ڶ��׶�
            string strTargetBiblioDbName = ResPath.GetDbName(strNewBiblioRecPath);

            if (entityinfos != null && entityinfos.Count > 0)
            {
                // TODO: ����Ǹ���, ��ҪΪĿ��ʵ���¼�Ĳ����������һ��ǰ׺�������ܵ�strStyle���ƣ��ܾ�����source����target�м���ǰ׺

                // ��������ͬһ��Ŀ��¼��ȫ��ʵ���¼
                // parameters:
                //      strAction   copy / move
                // return:
                //      -2  Ŀ��ʵ��ⲻ���ڣ��޷����и��ƻ���ɾ��
                //      -1  error
                //      >=0  ʵ�ʸ��ƻ����ƶ���ʵ���¼��
                nRet = CopyBiblioChildEntities(channel,
                    strAction,
                    entityinfos,
                    strNewBiblioRecPath,
                    domOperLog,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == -2)
                {
                    // TODO: ��Ҫ���Դʵ���¼���Ƿ�������һ��������ͨ��Ϣ������У���������ʧ������ζ����ͨ��Ϣ�Ķ�ʧ�����ǲ��������
                    if (nBorrowInfoCount > 0
                        && strAction == "move")
                    {
                        strError = "Ŀ����Ŀ�� '" + strTargetBiblioDbName + "' û��������ʵ��⣬(�ƶ�����)����ʧ����Դ��Ŀ�������� " + entityinfos.Count + " ��ʵ���¼������Щʵ���¼���Ѿ������� "+nBorrowInfoCount.ToString()+" ����ͨ��Ϣ������ζ����Щʵ���¼������ʧ������ƶ��������ȷ���";
                        goto ERROR1;
                    }

                    strWarning += "Ŀ����Ŀ�� '"+strTargetBiblioDbName+"' û��������ʵ��⣬�Ѷ�ʧ����Դ��Ŀ�������� "+entityinfos.Count+" ��ʵ���¼; ";
                }
            }

            if (orderinfos != null && orderinfos.Count > 0)
            {
                // ���ƶ�����¼
                // return:
                //      -2  Ŀ��ʵ��ⲻ���ڣ��޷����и��ƻ���ɾ��
                //      -1  error
                //      >=0  ʵ�ʸ��ƻ����ƶ���ʵ���¼��
                nRet = this.OrderItemDatabase.CopyBiblioChildItems(channel,
                strAction,
                orderinfos,
                strNewBiblioRecPath,
                domOperLog,
                out strError);
                if (nRet == -1)
                {
                    if (entityinfos.Count > 0)
                        strError += "��\r\n��" + strAction + "�� " + entityinfos.Count.ToString() + " �����¼�Ѿ��޷��ָ�";
                    goto ERROR1;
                }
                if (nRet == -2)
                {
                    strWarning += "Ŀ����Ŀ�� '" + strTargetBiblioDbName + "' û�������Ķ����⣬�Ѷ�ʧ����Դ��Ŀ�������� " + orderinfos.Count + " ��������¼; ";
                }
            }

            if (issueinfos != null && issueinfos.Count > 0)
            {
                // �����ڼ�¼
                // return:
                //      -2  Ŀ��ʵ��ⲻ���ڣ��޷����и��ƻ���ɾ��
                //      -1  error
                //      >=0  ʵ�ʸ��ƻ����ƶ���ʵ���¼��
                nRet = this.IssueItemDatabase.CopyBiblioChildItems(channel,
            strAction,
            issueinfos,
            strNewBiblioRecPath,
            domOperLog,
            out strError);
                if (nRet == -1)
                {
                    if (entityinfos.Count > 0)
                        strError += "��\r\n��" + strAction + "�� " + entityinfos.Count.ToString() + " �����¼�Ѿ��޷��ָ�";
                    if (orderinfos.Count > 0)
                        strError += "��\r\n��" + strAction + "�� " + orderinfos.Count.ToString() + " ��������¼�Ѿ��޷��ָ�";
                    goto ERROR1;
                }
                if (nRet == -2)
                {
                    strWarning += "Ŀ����Ŀ�� '" + strTargetBiblioDbName + "' û���������ڿ⣬�Ѷ�ʧ����Դ��Ŀ�������� " + issueinfos.Count + " ���ڼ�¼; ";
                }
            }

            if (commentinfos != null && commentinfos.Count > 0)
            {
                // ������ע��¼
                // return:
                //      -2  Ŀ��ʵ��ⲻ���ڣ��޷����и��ƻ���ɾ��
                //      -1  error
                //      >=0  ʵ�ʸ��ƻ����ƶ���ʵ���¼��
                nRet = this.CommentItemDatabase.CopyBiblioChildItems(channel,
            strAction,
            commentinfos,
            strNewBiblioRecPath,
            domOperLog,
            out strError);
                if (nRet == -1)
                {
                    if (entityinfos.Count > 0)
                        strError += "��\r\n��" + strAction + "�� " + entityinfos.Count.ToString() + " �����¼�Ѿ��޷��ָ�";
                    if (orderinfos.Count > 0)
                        strError += "��\r\n��" + strAction + "�� " + orderinfos.Count.ToString() + " ��������¼�Ѿ��޷��ָ�";
                    if (issueinfos.Count > 0)
                        strError += "��\r\n��" + strAction + "�� " + issueinfos.Count.ToString() + " ���ڼ�¼�Ѿ��޷��ָ�";
                    goto ERROR1;
                }
                if (nRet == -2)
                {
                    strWarning += "Ŀ����Ŀ�� '" + strTargetBiblioDbName + "' û����������ע�⣬�Ѷ�ʧ����Դ��Ŀ�������� " + commentinfos.Count + " ����ע��¼; ";
                }

            }

            return 0;
        ERROR1:
            return -1;
        }

        // �ƶ����߸�����Ŀ��¼
        // strExistingXml�������д�����old xml��ʱ����Ƚϣ��ڱ������⡢����ǰ����
        // parameters:
        //      strAction   ������Ϊ"onlycopybiblio" "onlymovebiblio"֮һ������ copy / move
        //      strNewBiblio    ��Ҫ��Ŀ���¼�и��µ����ݡ���� == null����ʾ���������
        //      strMergeStyle   ��κϲ�������¼��Ԫ���ݲ���? reserve_source / reserve_target�� �ձ�ʾ reserve_source
        int DoBiblioOperMove(
            string strAction,
            SessionInfo sessioninfo,
            RmsChannel channel,
            string strOldRecPath,
            string strExistingSourceXml,
            // byte[] baExistingSourceTimestamp, // �������ύ������ʱ���
            string strNewRecPath,
            string strNewBiblio,    // �Ѿ�����MergeԤ������¼�¼XML
            string strMergeStyle,
            out string strOutputTargetXml,
            out byte[] baOutputTimestamp,
            out string strOutputRecPath,
            out string strError)
        {
            strError = "";
            long lRet = 0;
            baOutputTimestamp = null;
            strOutputRecPath = "";

            strOutputTargetXml = ""; // ��󱣴�ɹ��ļ�¼

            // ���·��
            if (strOldRecPath == strNewRecPath)
            {
                strError = "��actionΪ\"" + strAction + "\"ʱ��strNewRecordPath·�� '" + strNewRecPath + "' ��strOldRecPath '" + strOldRecPath + "' ���벻��ͬ";
                goto ERROR1;
            }

            if (String.IsNullOrEmpty(strNewRecPath) == true)
            {
                strError = "DoBiblioOperMove() strNewRecPath����ֵ����Ϊ��";
                goto ERROR1;
            }

            // ��鼴�����ǵ�Ŀ��λ���ǲ����м�¼������У����������move������
            bool bAppendStyle = false;  // Ŀ��·���Ƿ�Ϊ׷����̬��
            string strTargetRecId = ResPath.GetRecordId(strNewRecPath);
            string strExistTargetXml = "";

            if (strTargetRecId == "?" || String.IsNullOrEmpty(strTargetRecId) == true)
            {
                // 2009/11/1 new add
                if (String.IsNullOrEmpty(strTargetRecId) == true)
                    strNewRecPath += "/?";

                bAppendStyle = true;
            }


            string strOutputPath = "";
            string strMetaData = "";

            if (bAppendStyle == false)
            {
                byte[] exist_target_timestamp = null;

                // ��ȡ����Ŀ��λ�õ����м�¼
                lRet = channel.GetRes(strNewRecPath,
                    out strExistTargetXml,
                    out strMetaData,
                    out exist_target_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.NotFound)
                    {
                        // �����¼������, ˵��������ɸ���̬��
                        /*
                        strExistSourceXml = "<root />";
                        exist_source_timestamp = null;
                        strOutputPath = info.NewRecPath;
                         * */
                    }
                    else
                    {
                        strError = "�ƶ�������������, �ڶ��뼴�����ǵ�Ŀ��λ�� '" + strNewRecPath + "' ԭ�м�¼�׶�:" + strError;
                        goto ERROR1;
                    }
                }
                else
                {
#if NO
                    // �����¼���ڣ���Ŀǰ�����������Ĳ���
                    strError = "�ƶ�(move)�������ܾ�����Ϊ�ڼ������ǵ�Ŀ��λ�� '" + strNewRecPath + "' �Ѿ�������Ŀ��¼������ɾ��(delete)������¼���ٽ����ƶ�(move)����";
                    goto ERROR1;
#endif
                }
            }

            /*
            // ��������¼װ��DOM

            XmlDocument domSourceExist = new XmlDocument();
            XmlDocument domNew = new XmlDocument();

            try
            {
                domSourceExist.LoadXml(strExistingSourceXml);
            }
            catch (Exception ex)
            {
                strError = "strExistXmlװ�ؽ���DOMʱ��������: " + ex.Message;
                goto ERROR1;
            }

            try
            {
                domNew.LoadXml(strNewBiblio);
            }
            catch (Exception ex)
            {
                strError = "strNewBiblioװ�ؽ���DOMʱ��������: " + ex.Message;
                goto ERROR1;
            }
             * */

            // ֻ��orderȨ�޵����
            if (StringUtil.IsInList("setbiblioinfo", sessioninfo.RightsOrigin) == false
                && StringUtil.IsInList("order", sessioninfo.RightsOrigin) == true)
            {
                if (strAction == "onlymovebiblio"
                    || strAction == "move")
                {
                    string strSourceDbName = ResPath.GetDbName(strOldRecPath);
                    // Դͷ��Ŀ��Ϊ �ǹ����� ���
                    if (IsOrderWorkBiblioDb(strSourceDbName) == false)
                    {
                        // �ǹ����ⲻ��ɾ����¼
                        if (IsOrderWorkBiblioDb(strSourceDbName) == false)
                        {
                            // �ǹ����⡣Ҫ��ԭ����¼������
                            strError = "��ǰ�ʻ�ֻ��orderȨ�޶�û��setbiblioinfoȨ�ޣ�������" + strAction + "����ɾ��Դ��Ŀ��¼ '" + strOldRecPath + "'";
                            goto ERROR1;
                        }
                    }
                }
            }

            // �ƶ���¼
            byte[] output_timestamp = null;
            string strIdChangeList = "";

            // TODO: Copy��Ҫдһ�Σ���ΪCopy����д���¼�¼��
            // ��ʵCopy���������ڴ�����Դ�����򻹲�����Save+Delete
            lRet = channel.DoCopyRecord(strOldRecPath,
                 strNewRecPath,
                 strAction == "onlymovebiblio" || strAction == "move" ? true : false,   // bDeleteSourceRecord
                 strMergeStyle,
                 out strIdChangeList,
                 out output_timestamp,
                 out strOutputRecPath,
                 out strError);
            if (lRet == -1)
            {
                strError = "DoCopyRecord() error :" + strError;
                goto ERROR1;
            }

            // TODO: ���ֶ� 856 �ֶεĺϲ���������Դ�� 856 �ֶε� $u �޸�

            if (String.IsNullOrEmpty(strNewBiblio) == false)
            {
                this.BiblioLocks.LockForWrite(strOutputRecPath);

                try
                {
                    // TODO: ����µġ��Ѵ��ڵ�xmlû�в�ͬ�������µ�xmlΪ�գ����ⲽ�������ʡ��
                    string strOutputBiblioRecPath = "";
                    lRet = channel.DoSaveTextRes(strOutputRecPath,
                        strNewBiblio,
                        false,
                        "content", // ,ignorechecktimestamp
                        output_timestamp,
                        out baOutputTimestamp,
                        out strOutputBiblioRecPath,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;
                }
                finally
                {
                    this.BiblioLocks.UnlockForWrite(strOutputRecPath);
                }
            }

            {
                // TODO: �Ƿ��ǰ��һ������?
                byte[] exist_target_timestamp = null;

                // ��ȡ���ļ�¼
                lRet = channel.GetRes(strOutputRecPath,
                    out strOutputTargetXml,
                    out strMetaData,
                    out exist_target_timestamp,
                    out strOutputPath,
                    out strError);
            }

            return 0;
        ERROR1:
            return -1;
        }

        /*
        // �ϲ��¾�������¼������MARC�������������Ϣ��
        // �����м���ģʽ��
        // 1) �¼�¼��ֻ��MARC������Ч��������������
        // 2) �¼�¼��ȫ�����ݾ���Ч
        // 3) �¼�¼�н�MARC���������������Ч
        // 4) ɾ��MARC��
        // 5) ɾ��MARC�������������
        int MergeOldNewRecord(string strMarcSyntax)
        {

            return 0;
        }
         * */
    }

    /*
    public enum MergeType
    {
        MARC = 0x01,
        OTHER = 0x02,
    }*/

}
