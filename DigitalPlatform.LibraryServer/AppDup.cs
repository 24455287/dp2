using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Runtime.Serialization;

using DigitalPlatform.Text;
using DigitalPlatform.Xml;
using DigitalPlatform.rms.Client;

// using DigitalPlatform.rms.Client.rmsws_localhost;   // Record

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// �������Ǻͱ�Ŀ���ع�����صĴ���
    /// </summary>
    public partial class LibraryApplication
    {

        // ��ò��ؼ������н��
        // parameters:
        //      lStart  �������н������ʼλ��
        //      lCount  �������н�����ļ�¼����
        //      strBrowseInfoStyle  �����ص�DupSearchResult�а�����Щ��Ϣ
        //              "cols"  ���������
        //              "excludecolsoflowthreshold" ������Ȩֵ������ֵ���е�����С�Ҫ��ͬʱ����colsʱ��������
        //      searchresults   ������¼��Ϣ��DupSearchResult����
        public LibraryServerResult GetDupSearchResult(
            SessionInfo sessioninfo,
            long lStart,
            long lCount,
            string strBrowseInfoStyle,
            out DupSearchResult[] searchresults)
        {
            string strError = "";
            searchresults = null;
            int nRet = 0;

            LibraryServerResult result = new LibraryServerResult();

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            DupResultSet dupset = sessioninfo.DupResultSet;

            if (dupset == null)
            {
                strError = "���ؽ����������";
                goto ERROR1;
            }

            dupset.EnsureCreateIndex();

            int nCount = (int)lCount;
            int nStart = (int)lStart;

            if (nCount == -1)
            {
                nCount = (int)dupset.Count - nStart;
                if (nCount < 0)
                    nCount = 0;
            }
            else
            {
                if (nCount > (int)dupset.Count - nStart)
                {
                    nCount = (int)dupset.Count - nStart;

                    if (nCount < 0)
                        nCount = 0;
                }
            }

            bool bExcludeCols = (StringUtil.IsInList("excludecolsoflowthreshold", strBrowseInfoStyle) == true);

            bool bCols = (StringUtil.IsInList("cols", strBrowseInfoStyle) == true);


            List<string> pathlist = new List<string>();

            List<DupSearchResult> results = new List<DupSearchResult>();
            for (int i = 0; i < nCount; i++)    // BUG nStart + 
            {
                DupLineItem item = (DupLineItem)dupset[nStart + i]; // changed

                DupSearchResult result_item = new DupSearchResult();
                results.Add(result_item);

                result_item.Path = item.Path;
                result_item.Weight = item.Weight;
                result_item.Threshold = item.Threshold;

                // paths[i] = item.Path;
                if (bCols == true)
                {
                    if (bExcludeCols == true && item.Weight < item.Threshold)
                    {
                    }
                    else
                        pathlist.Add(item.Path);
                }
            }

            if (pathlist.Count > 0)
            {
                // string[] paths = new string[pathlist.Count];
                string[] paths = StringUtil.FromListString(pathlist);

                ArrayList aRecord = null;

                nRet = channel.GetBrowseRecords(paths,
                    "cols",
                    out aRecord,
                    out strError);
                if (nRet == -1)
                {
                    strError = "GetBrowseRecords() error: " + strError;
                    goto ERROR1;
                }

                int j = 0;
                for (int i = 0; i < results.Count; i++)
                {
                    DupSearchResult result_item = results[i];
                    if (result_item.Path != pathlist[j])
                        continue;

                    string[] cols = (string[])aRecord[j];

                    results[i].Cols = cols;   // style�в�����id
                    j++;
                    if (j >= pathlist.Count)
                        break;
                }
            }

            searchresults = new DupSearchResult[results.Count];
            results.CopyTo(searchresults);

            result.Value = searchresults.Length;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;

        }

        // �г����ط�����Ϣ
        // parameters:
        //      strOriginBiblioDbName  �������Ŀ����
        public LibraryServerResult ListDupProjectInfos(
            string strOriginBiblioDbName,
            out DupProjectInfo[] results)
        {
            // string strError = "";
            results = null;

            LibraryServerResult result = new LibraryServerResult();

            XmlNodeList nodes = null;

            if (String.IsNullOrEmpty(strOriginBiblioDbName) == true)
            {
                // ����<project>Ԫ��
                nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("//dup/project");
            }
            else
            {
                // ���а���ָ�����ݿ�����<project>Ԫ��
                nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("//dup/project[./database[@name='" + strOriginBiblioDbName + "']]");
            }

            results = new DupProjectInfo[nodes.Count];
            for (int i = 0; i < results.Length; i++)
            {
                DupProjectInfo dpi = new DupProjectInfo();
                dpi.Name = DomUtil.GetAttr(nodes[i], "name");
                dpi.Comment = DomUtil.GetAttr(nodes[i], "comment");

                results[i] = dpi;
            }

            result.Value = results.Length;
            return result;
            /*
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
             * */
        }

        // ���в���
        // parameters:
        //      sessioninfo �����������DupResultSet����Ӧ������sessioninfo.GetChannel()����Ҫ��channel�����м�������
        //      channel
        //      strOriginBiblioRecPath  �������Ŀ��¼·��
        //      strOriginBiblioRecXml   �������Ŀ��¼XML
        //      strProjectName  ���ط�����
        //      strStyle    includeoriginrecord�������а��������¼(ȱʡΪ������)
        // return:
        //      -1  error
        //      0   not found
        //      ����    ���м�¼����
        public LibraryServerResult SearchDup(
            SessionInfo sessioninfo1,
            RmsChannel channel,
            string strOriginBiblioRecPath,
            string strOriginBiblioRecXml,
            string strProjectName,
            string strStyle,
            out string strUsedProjectName)
        {
            string strError = "";
            int nRet = 0;
            strUsedProjectName = "";

            string strDebugInfo = "";

            strStyle = strStyle.ToLower();
            bool bIncludeOriginRecord = StringUtil.IsInList("includeoriginrecord", strStyle);

            LibraryServerResult result = new LibraryServerResult();

            // ���û�и���������������Ҫ��<default>Ԫ�����ҵ�һ����Ŀ���ȱʡ���ط���
            if (String.IsNullOrEmpty(strProjectName) == true)
            {
                if (String.IsNullOrEmpty(strOriginBiblioRecPath) == true)
                {
                    strError = "��û�и������ط�������Ҳû�и�����¼·�����޷����в���";
                    goto ERROR1;
                }
                string strOriginBiblioDbName = ResPath.GetDbName(strOriginBiblioRecPath);

                XmlNode nodeDefault = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//dup/default[@origin='" + strOriginBiblioDbName + "']");
                if (nodeDefault == null)
                {
                    strError = "��û����ȷָ�����ط�����������£���ϣ��ͨ�������Ŀ���ȱʡ���ط��������в��ء���Ŀǰϵͳû��Ϊ��Ŀ�� '" + strOriginBiblioDbName + "' ����ȱʡ���ط��������޷����в���";
                    goto ERROR1;
                }

                string strDefaultProjectName = DomUtil.GetAttr(nodeDefault, "project");
                if (String.IsNullOrEmpty(strDefaultProjectName) == true)
                {
                    strError = "��Ŀ�� '" + strOriginBiblioDbName + "' ��<default>Ԫ����δ����project����ֵ";
                    goto ERROR1;
                }

                strProjectName = strDefaultProjectName;
            }

            strUsedProjectName = strProjectName;

            XmlNode nodeProject = null;
            // ��ò��ط�������ڵ�
            // return:
            //      -1  ����
            //      0   not found
            //      1   found
            nRet = GetDupProjectNode(strProjectName,
                out nodeProject,
                out strError);
            if (nRet == 0 || nRet == -1)
                goto ERROR1;

            Debug.Assert(nodeProject != null, "");

            DupResultSet alldatabase_set = null;    // ���п�Ľ����

            XmlNodeList nodeDatabases = nodeProject.SelectNodes("database");

            // ѭ�������ÿ�����ݿ���м���
            for (int i = 0; i < nodeDatabases.Count; i++)
            {
                XmlNode nodeDatabase = nodeDatabases[i];
                string strDatabaseName = DomUtil.GetAttr(nodeDatabase, "name");
                string strThreshold = DomUtil.GetAttr(nodeDatabase, "threshold");
                int nThreshold = 0;
                try
                {
                    nThreshold = Convert.ToInt32(strThreshold);
                }
                catch
                {
                }

                List<AccessKeyInfo> aKeyLine = null;
                // ģ�ⴴ�������㣬�Ի�ü������б�
        // return:
        //      -1  error
        //      0   succeed
                nRet = GetKeys(
                    // sessioninfo.Channels,
                    channel,
                    strOriginBiblioRecPath,
                    strOriginBiblioRecXml,
                    out aKeyLine,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                DupResultSet onedatabase_set = null;    // һ����Ľ����


                XmlNodeList accesspoints = nodeDatabase.SelectNodes("accessPoint");
                // <accessPoint>ѭ��
                for (int j = 0; j < accesspoints.Count; j++)
                {
                    XmlNode accesspoint = accesspoints[j];

                    string strFrom = DomUtil.GetAttr(accesspoint, "name");

                    // ���from����Ӧ��key
                    List<string> keys = GetKeyByFrom(aKeyLine,
                        strFrom);
                    if (keys.Count == 0)
                        continue;

                    string strWeight = DomUtil.GetAttr(accesspoint, "weight");
                    string strSearchStyle = DomUtil.GetAttr(accesspoint, "searchStyle");

                    int nWeight = 0;
                    try
                    {
                        nWeight = Convert.ToInt32(strWeight);
                    }
                    catch
                    {
                        // ���涨������?
                    }

                    for (int k = 0; k < keys.Count; k++)
                    {
                        string strKey = (string)keys[k];
                        if (strKey == "")
                            continue;

                        DupResultSet dupset = null;

                        // ���һ��from���м���
                        // return:
                        //      -1  error
                        //      0   not found
                        //      1   found
                        nRet = SearchOneFrom(
                            // sessioninfo.Channels,
                            channel,
                            strDatabaseName,
                            strFrom,
                            strKey,
                            strSearchStyle,
                            nWeight,
                            nThreshold,
                            5000,   // ???
                            (bIncludeOriginRecord == false) ? strOriginBiblioRecPath : null,
                            out dupset,
                            out strError);

                        if (nRet == -1)
                        {
                            // ??? �����������?
                            continue;
                        }

                        if (onedatabase_set == null)
                        {
                            onedatabase_set = dupset;
                            continue;
                        }

                        if (nRet == 0)
                            continue;

                        Debug.Assert(dupset != null, "");

                        onedatabase_set.EnsureCreateIndex();
                        dupset.EnsureCreateIndex();

                        // ��dupset��ǰһ��set�鲢
                        // �鲢���Բο�ResultSet�е�Merge�㷨
                        DupResultSet tempset = new DupResultSet();
                        tempset.Open(false);
                        // ����: �ϲ���������
                        // parameters:
                        //		strStyle	������ OR , AND , SUB
                        //		sourceLeft	Դ��߽����
                        //		sourceRight	Դ�ұ߽����
                        //		targetLeft	Ŀ����߽����
                        //		targetMiddle	Ŀ���м�����
                        //		targetRight	Ŀ���ұ߽����
                        //		bOutputDebugInfo	�Ƿ����������Ϣ
                        //		strDebugInfo	������Ϣ
                        // return
                        //		-1	����
                        //		0	�ɹ�
                        nRet = DupResultSet.Merge("OR",
                            onedatabase_set,
                            dupset,
                            null,   // targetLeft,
                            tempset,
                            null,   // targetRight,
                            false,
                            out strDebugInfo,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        onedatabase_set = tempset;

                    } // end of k loop

                } // end of j loop


                if (alldatabase_set == null)
                {
                    alldatabase_set = onedatabase_set;
                    continue;
                }

                // �ϲ�
                if (onedatabase_set != null)
                {
                    DupResultSet tempset0 = new DupResultSet();
                    tempset0.Open(false);


                    alldatabase_set.EnsureCreateIndex();
                    onedatabase_set.EnsureCreateIndex();


                    nRet = DupResultSet.Merge("OR",
                        alldatabase_set,
                        onedatabase_set,
                        null,   // targetLeft,
                        tempset0,
                        null,   // targetRight,
                        false,
                        out strDebugInfo,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    alldatabase_set = tempset0;
                }
            }

            // ���Ҫ���� Weight��Threshold�Ĳ�� �Խ�����������򣬱������
            if (alldatabase_set != null)
            {
                alldatabase_set.SortStyle = DupResultSetSortStyle.OverThreshold;
                alldatabase_set.Sort();
            }


            sessioninfo1.DupResultSet = alldatabase_set;

            if (alldatabase_set != null)
                result.Value = alldatabase_set.Count;
            else
                result.Value = 0;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }

        // ���һ��from���м���
        // parameters:
        //      strExcludeBiblioRecPath Ҫ�ų����ļ�¼·��
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int SearchOneFrom(
            // RmsChannelCollection Channels,
            RmsChannel channel,
            string strDbName,
            string strFrom,
            string strKey,
            string strSearchStyle,
            int nWeight,
            int nThreshold,
            long nMax,
            string strExcludeBiblioRecPath,
            out DupResultSet dupset,
            out string strError)
        {
            strError = "";
            dupset = null;
            long lRet = 0;

            if (strSearchStyle == "")
                strSearchStyle = "exact";

            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14
                + "'><item><word>"
                + StringUtil.GetXmlStringSimple(strKey)
                + "</word><match>" + strSearchStyle + "</match><relation>=</relation><dataType>string</dataType><maxCount>" + nMax.ToString() + "</maxCount></item><lang>zh</lang></target>";

            string strSearchReason = "key='" + strKey + "', from='" + strFrom + "', weight=" + Convert.ToString(nWeight);

            /*
            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }
             * */
            Debug.Assert(channel != null, "");

            lRet = channel.DoSearch(strQueryXml,
                "dup",
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
                goto ERROR1;

            if (lRet == 0)
                return 0;   // not found

            long lHitCount = lRet;

            long lStart = 0;
            long lPerCount = Math.Min(50, lHitCount);
            List<string> aPath = null;

            dupset = new DupResultSet();
            dupset.Open(false);

            // ��ý�������������¼���д���
            for (; ; )
            {
                // TODO: �м�Ҫ�����ж�


                lRet = channel.DoGetSearchResult(
                    "dup",   // strResultSetName
                    lStart,
                    lPerCount,
                    "zh",
                    null,   // stop
                    out aPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (lRet == 0)
                {
                    strError = "δ����";
                    break;  // ??
                }

                // ����������
                for (int i = 0; i < aPath.Count; i++)
                {
                    string strPath = aPath[i];

                    // ���Է����¼��·��
                    if (strPath == strExcludeBiblioRecPath)
                        continue;

                    DupLineItem item = new DupLineItem();
                    item.Path = strPath;
                    item.Weight = nWeight;
                    item.Threshold = nThreshold;
                    dupset.Add(item);

                }

                lStart += aPath.Count;
                if (lStart >= lHitCount || lPerCount <= 0)
                    break;
            }

            return 1;
        ERROR1:
            return -1;
        }


        // ��ģ��keys�и���from��ö�Ӧ��key
        static List<string> GetKeyByFrom(List<AccessKeyInfo> aKeyLine,
            string strFromName)
        {
            List<string> aResult = new List<string>();
            for (int i = 0; i < aKeyLine.Count; i++)
            {
                AccessKeyInfo info = (AccessKeyInfo)aKeyLine[i];
                if (info.FromName == strFromName)
                    aResult.Add(info.Key);
            }

            return aResult;
        }

        // ģ�ⴴ��������
        // return:
        //      -1  error
        //      0   succeed
        public int GetKeys(
            // RmsChannelCollection Channels,
            RmsChannel channel,
            string strPath,
            string strXml,
            out List<AccessKeyInfo> aLine,
            out string strError)
        {
            strError = "";
            aLine = null;

            /*
            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }
             * */
            Debug.Assert(channel != null, "");

            long lRet = channel.DoGetKeys(
                strPath,
                strXml,
                "zh",	// strLang
                // "",	// strStyle
                null,	// this.stop,
                out aLine,
                out strError);
            if (lRet == -1)
            {
                return -1;
            }

            return 0;
        }

        // ��ò��ط�������ڵ�
        // return:
        //      -1  ����
        //      0   not found
        //      1   found
        int GetDupProjectNode(string strProjectName,
            out XmlNode node,
            out string strError)
        {
            strError = "";
            node = null;

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//dup");
            if (root == null)
            {
                strError = "library.xml����δ����<dup>Ԫ���Լ�����Ԫ��";
                return -1;
            }

            node = root.SelectSingleNode("project[@name='"+strProjectName+"']");
            if (node == null)
            {
                strError = "���ط��� '" +strProjectName+ "' �Ķ��岻����";
                return 0;
            }

            return 1;
        }

    }

    // ���ط�����Ϣ
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class DupProjectInfo
    {
        [DataMember]
        public string Name = "";
        [DataMember]
        public string Comment = "";
    }

    // ���ؼ������н����һ��
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class DupSearchResult
    {
        [DataMember]
        public string Path = "";    // ��¼·��
        [DataMember]
        public int Weight = 0;  // Ȩֵ
        [DataMember]
        public int Threshold = 0;   // ��ֵ
        [DataMember]
        public string[] Cols = null;    // ������С�һ��Ϊ���������ߣ�������ĿժҪ
    }
}
