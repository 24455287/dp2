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

using DigitalPlatform.rms.Client.rmsws_localhost;


namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// �������Ǻ��ִκŹ�����صĴ���
    /// </summary>
    public partial class LibraryApplication
    {
        // ͨ���ִκ���������ִκſ���
        // parameters:
        //      strZhongcihaoGroupName  @�����ִκſ��� !����������Ŀ���� ������� �ִκ�����
        string GetZhongcihaoDbName(string strZhongcihaoGroupName)
        {
            if (String.IsNullOrEmpty(strZhongcihaoGroupName) == true)
                return null;

            // 2012/11/8
            // @�����ִκſ���
            if (strZhongcihaoGroupName[0] == '@')
            {
                return strZhongcihaoGroupName.Substring(1);
            }

            // !����������Ŀ����
            if (strZhongcihaoGroupName[0] == '!')
            {
                string strTemp = GetZhongcihaoGroupName(strZhongcihaoGroupName.Substring(1));

                if (strTemp == null)
                {
                    /*
                    strError = "��Ŀ���� " + strZhongcihaoGroupName.Substring(1) + " û���ҵ���Ӧ���ִκ�����";
                    goto ERROR1;
                     * */
                    return null;
                }
                strZhongcihaoGroupName = strTemp;
            }

            // ������� �ִκ�����
            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//zhongcihao/group[@name='"+strZhongcihaoGroupName+"']");
            if (node == null)
                return null;

            return DomUtil.GetAttr(node, "zhongcihaodb");
        }

        // ����β�ż�¼��·���ͼ�¼��
        // return:
        //      -1  error(ע���������ж���������������󷵻�)
        //      0   not found
        //      1   found
        public int SearchTailNumberPathAndRecord(
            RmsChannelCollection Channels,
            string strZhongcihaoGroupName,
            string strClass,
            out string strPath,
            out string strXml,
            out byte[] timestamp,
            out string strError)
        {
            strError = "";
            strPath = "";
            strXml = "";
            timestamp = null;

            if (strClass == "")
            {
                strError = "��δָ�������";
                return -1;
            }

            if (strZhongcihaoGroupName == "")
            {
                strError = "��δָ���ִκ�����";
                return -1;
            }


            string strZhongcihaoDbName = GetZhongcihaoDbName(strZhongcihaoGroupName);
            if (String.IsNullOrEmpty(strZhongcihaoDbName) == true)
            {
                strError = "�޷�ͨ���ִκ����� '" + strZhongcihaoGroupName + "' ����ִκſ���";
                return -1;
            }

            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(strZhongcihaoDbName + ":" + "�����")       // 2007/9/14 new add 
                + "'><item><word>"
                + StringUtil.GetXmlStringSimple(strClass)
                + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            List<string> aPath = null;
            // ���ͨ�ü�¼
            // �������ɻ�ó���1�����ϵ�·��
            // return:
            //      -1  error
            //      0   not found
            //      1   ����1��
            //      >1  ���ж���1��
            int nRet = GetRecXml(
                Channels,
                strQueryXml,
                out strXml,
                2,
                out aPath,
                out timestamp,
                out strError);
            if (nRet == -1)
            {
                strError = "������ " + strZhongcihaoDbName + " ʱ����: " + strError;
                return -1;
            }
            if (nRet == 0)
            {
                return 0;	// û���ҵ�
            }

            if (nRet > 1)
            {
                strError = "�Է����'" + strClass + "'������ " + strZhongcihaoDbName + " ʱ���� " + Convert.ToString(nRet) + " �����޷�ȡ��β�š����޸Ŀ� '" + strZhongcihaoDbName + "' ����Ӧ��¼��ȷ��ͬһ��Ŀֻ��һ����Ӧ�ļ�¼��";
                return -1;
            }

            Debug.Assert(aPath.Count >= 1, "");
            strPath = aPath[0];

            return 1;
        }

        // ����ִκ�β��
        public LibraryServerResult GetZhongcihaoTailNumber(
            SessionInfo sessioninfo,
            string strZhongcihaoGroupName,
            string strClass,
            out string strTailNumber)
        {
            strTailNumber = "";

            string strError = "";

            LibraryServerResult result = new LibraryServerResult();

            if (String.IsNullOrEmpty(strZhongcihaoGroupName) == true)
            {
                strError = "�ִκ���������ֵ����Ϊ��";
                goto ERROR1;
            }

            string strPath = "";
            string strXml = "";
            byte[] timestamp = null;
            // ����β�ż�¼��·���ͼ�¼��
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            int nRet = SearchTailNumberPathAndRecord(
                sessioninfo.Channels,
                strZhongcihaoGroupName,
                strClass,
                out strPath,
                out strXml,
                out timestamp,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 0)
            {
                result.ErrorCode = ErrorCode.NotFound;
                result.ErrorInfo = strError;
                result.Value = 0;
                return result;
            }

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "β�ż�¼ '" + strPath + "' XMLװ��DOMʱ��������: " + ex.Message;
                goto ERROR1;
            }

            strTailNumber = DomUtil.GetAttr(dom.DocumentElement, "v");

            result.Value = 1;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }

        // �����ִκ�β��
        public LibraryServerResult SetZhongcihaoTailNumber(
            SessionInfo sessioninfo,
            string strAction,
            string strZhongcihaoGroupName,
            string strClass,
            string strTestNumber,
            out string strOutputNumber)
        {
            strOutputNumber = "";

            string strError = "";

            LibraryServerResult result = new LibraryServerResult();

            string strPath = "";
            string strXml = "";
            byte[] timestamp = null;
            // ����β�ż�¼��·���ͼ�¼��
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            int nRet = SearchTailNumberPathAndRecord(
                sessioninfo.Channels,
                strZhongcihaoGroupName,
                strClass,
                out strPath,
                out strXml,
                out timestamp,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            /*
            if (nRet == 0)
            {
            }

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "β�ż�¼ '" + strPath + "' XMLװ��DOMʱ��������: " + ex.Message;
                goto ERROR1;
            }*/

            string strZhongcihaoDbName = GetZhongcihaoDbName(strZhongcihaoGroupName);
            if (String.IsNullOrEmpty(strZhongcihaoDbName) == true)
            {
                strError = "�޷�ͨ���ִκ����� '" + strZhongcihaoGroupName + "' ����ִκſ���";
                goto ERROR1;
            }

            // byte[] baOutputTimestamp = null;
            bool bNewRecord = false;
            long lRet = 0;

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            byte[] output_timestamp = null;
            string strOutputPath = "";

            if (strAction == "conditionalpush")
            {

                if (nRet == 0)
                {
                    // �´�����¼
                    strPath = strZhongcihaoDbName + "/?";
                    strXml = "<r c='" + strClass + "' v='" + strTestNumber + "'/>";

                    bNewRecord = true;
                }
                else
                {
                    string strPartXml = "/xpath/<locate>@v</locate><action>Push</action>";
                    strPath += strPartXml;
                    strXml = strTestNumber;

                    bNewRecord = false;
                }

                lRet = channel.DoSaveTextRes(strPath,
                    strXml,
                    false,
                    "content",
                    timestamp,   // timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    strError = "����β�ż�¼ʱ����: " + strError;
                    goto ERROR1;
                }

                if (bNewRecord == true)
                {
                    strOutputNumber = strTestNumber;
                }
                else
                {
                    strOutputNumber = strError;
                }

                goto END1;
            }
            else if (strAction == "increase" || strAction == "+increase" || strAction == "increase+")
            {
                string strDefaultNumber = strTestNumber;

                if (nRet == 0)
                {
                    // �´�����¼
                    strPath = strZhongcihaoDbName + "/?";
                    strXml = "<r c='" + strClass + "' v='" + strDefaultNumber + "'/>";

                    bNewRecord = true;
                }
                else
                {
                    string strPartXml = "/xpath/<locate>@v</locate><action>+AddInteger</action>";

                    // 2012/11/8
                    if (strAction == "increase+")
                          strPartXml = "/xpath/<locate>@v</locate><action>AddInteger+</action>";

                    strPath += strPartXml;
                    strXml = "1";

                    bNewRecord = false;
                }


                // 
                lRet = channel.DoSaveTextRes(strPath,
                    strXml,
                    false,
                    "content",
                    timestamp,   // timestamp,
                    out output_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    strError = "����β�ż�¼ʱ����: " + strError;
                    goto ERROR1;
                }

                if (bNewRecord == true)
                {
                    strOutputNumber = strDefaultNumber;
                }
                else
                {
                    strOutputNumber = strError;
                }

                goto END1;
            }
            else if (strAction == "save")
            {
                string strTailNumber = strTestNumber;

                if (nRet == 0)
                {
                    strPath = strZhongcihaoDbName + "/?";
                }
                else
                {
                    // ���Ǽ�¼
                    if (String.IsNullOrEmpty(strPath) == true)
                    {
                        strError = "��¼����ʱstrPath��ȻΪ��";
                        goto ERROR1;
                    }

                }

                strXml = "<r c='" + strClass + "' v='" + strTailNumber + "'/>";

                lRet = channel.DoSaveTextRes(strPath,
    strXml,
    false,
    "content",
    timestamp,   // timestamp,
    out output_timestamp,
    out strOutputPath,
    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                    {
                        strError = "β�ż�¼ʱ�����ƥ�䣬˵�����ܱ������޸Ĺ�����ϸԭ��: " + strError;
                        goto ERROR1;
                    }

                    strError = "����β�ż�¼ʱ����: " + strError;
                    goto ERROR1;
                }

            }
            else
            {
                strError = "�޷�ʶ���strAction����ֵ '" + strAction + "'";
                goto ERROR1;
            }

            END1:
            result.Value = 1;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }



        // ͨ����Ŀ�����õ��ִκ�group��
        string GetZhongcihaoGroupName(string strBiblioDbName)
        {
            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//zhongcihao/group[./database[@name='"+strBiblioDbName+"']]");
            if (node == null)
                return null;

            return DomUtil.GetAttr(node, "name");
        }

        static string GetZhongcihaoPart(string strText)
        {
            int nRet = strText.IndexOf("/");
            if (nRet == -1)
                return strText;

            return strText.Substring(nRet + 1).Trim();
        }

        public static string BuildZhongcihaoString(DigitalPlatform.rms.Client.rmsws_localhost.KeyFrom[] keys)
        {
            if (keys == null || keys.Length == 0)
                return "";
            /*
            foreach (KeyFrom entry in keys)
            {
                return GetZhongcihaoPart(entry.Key);
            }

            return "";
             * */
            return GetZhongcihaoPart(keys[0].Key);
        }

        public LibraryServerResult GetZhongcihaoSearchResult(
    SessionInfo sessioninfo,
    string strZhongcihaoGroupName,
    string strResultSetName,
    long lStart,
    long lCount,
    string strBrowseInfoStyle,
    string strLang,
    out ZhongcihaoSearchResult[] searchresults)
        {
            string strError = "";
            searchresults = null;

            LibraryServerResult result = new LibraryServerResult();
            // int nRet = 0;
            long lRet = 0;

            if (String.IsNullOrEmpty(strZhongcihaoGroupName) == true)
            {
                strError = "strZhongcihaoGroupName����ֵ����Ϊ��";
                goto ERROR1;
            }

            if (strZhongcihaoGroupName[0] == '!')
            {
                string strTemp = GetZhongcihaoGroupName(strZhongcihaoGroupName.Substring(1));

                if (strTemp == null)
                {
                    strError = "��Ŀ���� " + strZhongcihaoGroupName.Substring(1) + " û���ҵ���Ӧ���ִκ�����";
                    goto ERROR1;
                }
                strZhongcihaoGroupName = strTemp;
            }

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                result.Value = -1;
                result.ErrorInfo = "get channel error";
                result.ErrorCode = ErrorCode.SystemError;
                return result;
            }

            /*

// 
XmlNode nodeNsTable = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//zhongcihao/nstable");

XmlNamespaceManager mngr = null;

if (nodeNsTable != null)
{
    // ׼�����ֿռ价��
    nRet = PrepareNs(
        nodeNsTable,
        out mngr,
        out strError);
    if (nRet == -1)
        goto ERROR1;
}

// �������ݿⶨ��Ϳ����Ķ��ձ�
XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("//zhongcihao/group[@name='" + strZhongcihaoGroupName + "']/database");
if (nodes.Count == 0)
{
    strError = "library.xml����δ�����й� '" + strZhongcihaoGroupName + "'��<zhongcihao>/<group>/<database>��ز���";
    goto ERROR1;
}

Hashtable db_prop_table = new Hashtable();

for (int i = 0; i < nodes.Count; i++)
{
    XmlNode node = nodes[i];

    DbZhongcihaoProperty prop = new DbZhongcihaoProperty();
    prop.DbName = DomUtil.GetAttr(node, "name");
    prop.NumberXPath = DomUtil.GetAttr(node, "rightxpath");
    prop.TitleXPath = DomUtil.GetAttr(node, "titlexpath");
    prop.AuthorXPath = DomUtil.GetAttr(node, "authorxpath");

    db_prop_table[prop.DbName] = prop;
}
 * */
            bool bCols = (StringUtil.IsInList("cols", strBrowseInfoStyle) == true);
            string strBrowseStyle = "keyid,key,id";
            if (bCols == true)
                strBrowseStyle += ",cols";

            if (String.IsNullOrEmpty(strResultSetName) == true)
                strResultSetName = "default";

            Record[] origin_searchresults = null; // 

            lRet = channel.DoGetSearchResult(
                strResultSetName,
                lStart,
                lCount,
                strBrowseStyle, // "id",
                strLang,
                null,
                out origin_searchresults,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            long lResultCount = lRet;

            searchresults = new ZhongcihaoSearchResult[origin_searchresults.Length];

            for (int i = 0; i < origin_searchresults.Length; i++)
            {
                ZhongcihaoSearchResult item = new ZhongcihaoSearchResult();

                Record origin_item =  origin_searchresults[i];

                item.Path = origin_item.Path;
                searchresults[i] = item;
                item.Zhongcihao = BuildZhongcihaoString(origin_item.Keys);
                item.Cols = origin_item.Cols;
            }


#if NO
            List<string> pathlist = new List<string>();

            searchresults = new ZhongcihaoSearchResult[origin_searchresults.Length];

            for (int i = 0; i < origin_searchresults.Length; i++)
            {
                ZhongcihaoSearchResult item = new ZhongcihaoSearchResult();

                item.Path = origin_searchresults[i].Path;
                searchresults[i] = item;
                item.Zhongcihao = BuildZhongcihaoString(origin_searchresults[i].Keys);

                if (bCols == true)
                    pathlist.Add(item.Path);
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
                for (int i = 0; i < searchresults.Length; i++)
                {
                    ZhongcihaoSearchResult result_item = searchresults[i];
                    if (result_item.Path != pathlist[j])
                        continue;

                    string[] cols = (string[])aRecord[j];

                    result_item.Cols = cols;   // style�в�����id
                    j++;
                    if (j >= pathlist.Count)
                        break;
                }
            }

#endif

            result.Value = lResultCount;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }

#if OLD
        public LibraryServerResult GetZhongcihaoSearchResult(
            SessionInfo sessioninfo,
            string strZhongcihaoGroupName,
            string strResultSetName,
            long lStart,
            long lCount,
            string strBrowseInfoStyle,
            string strLang,
            out ZhongcihaoSearchResult[] searchresults)
        {
            string strError = "";
            searchresults = null;

            LibraryServerResult result = new LibraryServerResult();
            int nRet = 0;
            long lRet = 0;

            if (String.IsNullOrEmpty(strZhongcihaoGroupName) == true)
            {
                strError = "strZhongcihaoGroupName����ֵ����Ϊ��";
                goto ERROR1;
            }

            if (strZhongcihaoGroupName[0] == '!')
            {
                string strTemp = GetZhongcihaoGroupName(strZhongcihaoGroupName.Substring(1));

                if (strTemp == null)
                {
                    strError = "��Ŀ���� " + strZhongcihaoGroupName.Substring(1) + " û���ҵ���Ӧ���ִκ�����";
                    goto ERROR1;
                }
                strZhongcihaoGroupName = strTemp;
            }

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                result.Value = -1;
                result.ErrorInfo = "get channel error";
                result.ErrorCode = ErrorCode.SystemError;
                return result;
            }

            // 
            XmlNode nodeNsTable = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//zhongcihao/nstable");

            XmlNamespaceManager mngr = null;

            if (nodeNsTable != null)
            {
                // ׼�����ֿռ价��
                nRet = PrepareNs(
                    nodeNsTable,
                    out mngr,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            // �������ݿⶨ��Ϳ����Ķ��ձ�
            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("//zhongcihao/group[@name='" + strZhongcihaoGroupName + "']/database");
            if (nodes.Count == 0)
            {
                strError = "library.xml����δ�����й� '" + strZhongcihaoGroupName + "'��<zhongcihao>/<group>/<database>��ز���";
                goto ERROR1;
            }

            Hashtable db_prop_table = new Hashtable();

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                DbZhongcihaoProperty prop = new DbZhongcihaoProperty();
                prop.DbName = DomUtil.GetAttr(node, "name");
                prop.NumberXPath = DomUtil.GetAttr(node, "rightxpath");
                prop.TitleXPath = DomUtil.GetAttr(node, "titlexpath");
                prop.AuthorXPath = DomUtil.GetAttr(node, "authorxpath");

                db_prop_table[prop.DbName] = prop;
            }

            if (String.IsNullOrEmpty(strResultSetName) == true)
                strResultSetName = "default";

            Record[] origin_searchresults = null; // 

            lRet = channel.DoGetSearchResult(
                strResultSetName,
                lStart,
                lCount,
                "id",
                strLang,
                null,
                out origin_searchresults,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            long lResultCount = lRet;

            searchresults = new ZhongcihaoSearchResult[origin_searchresults.Length];

            for (int i = 0; i < origin_searchresults.Length; i++)
            {
                ZhongcihaoSearchResult item = new ZhongcihaoSearchResult();

                item.Path = origin_searchresults[i].Path;
                searchresults[i] = item;

                // ������������Ա
                string strXml = "";
                string strMetaData = "";
                byte[] timestamp = null;
                string strOutputPath = "";

                lRet = channel.GetRes(item.Path,
                    out strXml,
                    out strMetaData,
                    out timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    item.Zhongcihao = "��ȡ��¼ '" + item.Path + "' ����: " + strError;
                    continue;
                }

                string strDbName = ResPath.GetDbName(item.Path);

                DbZhongcihaoProperty prop = (DbZhongcihaoProperty)db_prop_table[strDbName];
                if (prop == null)
                {
                    item.Zhongcihao = "���ݿ��� '" + strDbName + "' ���ڶ�����ִκ�����(<zhongcihao>/<group>/<database>)��";
                    continue;
                }

                string strNumber = "";
                string strTitle = "";
                string strAuthor = "";

                nRet = GetRecordProperties(
                    strXml,
                    prop,
                    mngr,
                    out strNumber,
                    out strTitle,
                    out strAuthor,
                    out strError);
                if (nRet == -1)
                {
                    item.Zhongcihao = strError;
                    continue;
                }

                item.Zhongcihao = strNumber;
                item.Cols = new string[2];
                item.Cols[0] = strTitle;
                item.Cols[1] = strAuthor;
            }


            result.Value = lResultCount;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }

        /// <summary>
        /// ׼�����ֿռ价��
        /// </summary>
        /// <param name="nodeNsTable">nstable�ڵ�</param>
        /// <param name="mngr">�������ֿռ����������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>0</returns>
        static int PrepareNs(
            XmlNode nodeNsTable,
            out XmlNamespaceManager mngr,
            out string strError)
        {
            strError = "";
            mngr = new XmlNamespaceManager(new NameTable());
            XmlNodeList nodes = nodeNsTable.SelectNodes("item");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strPrefix = DomUtil.GetAttr(node, "prefix");
                string strUri = DomUtil.GetAttr(node, "uri");

                mngr.AddNamespace(strPrefix, strUri);
            }

            return 0;
        }

        int GetRecordProperties(
            string strXml,
            DbZhongcihaoProperty prop,
            XmlNamespaceManager mngr,
            out string strNumber,
            out string strTitle,
            out string strAuthor,
            out string strError)
        {
            strNumber = "";
            strTitle = "";
            strAuthor = "";
            strError = "";

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            try
            {

                if (prop.NumberXPath != "")
                {
                    XmlNode node = dom.DocumentElement.SelectSingleNode(prop.NumberXPath, mngr);
                    if (node != null)
                        strNumber = node.Value;
                }

                if (prop.TitleXPath != "")
                {
                    XmlNode node = dom.DocumentElement.SelectSingleNode(prop.TitleXPath, mngr);
                    if (node != null)
                        strTitle = node.Value;
                }

                if (prop.AuthorXPath != "")
                {
                    XmlNode node = dom.DocumentElement.SelectSingleNode(prop.AuthorXPath, mngr);
                    if (node != null)
                        strAuthor = node.Value;
                }
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            return 0;
        }
#endif

        // ����ͬ���¼
        public LibraryServerResult SearchUsedZhongcihao(
            SessionInfo sessioninfo,
            string strZhongcihaoGroupName,
            string strClass,
            string strResultSetName,
            out string strQueryXml)
        {
            strQueryXml = "";

            string strError = "";

            LibraryServerResult result = new LibraryServerResult();

            if (String.IsNullOrEmpty(strZhongcihaoGroupName) == true)
            {
                strError = "strZhongcihaoGroupName����ֵ����Ϊ��";
                goto ERROR1;
            }

            if (strZhongcihaoGroupName[0] == '!')
            {
                string strTemp = GetZhongcihaoGroupName(strZhongcihaoGroupName.Substring(1));

                if (strTemp == null)
                {
                    strError = "��Ŀ���� " + strZhongcihaoGroupName.Substring(1) + " û���ҵ���Ӧ���ִκ�����";
                    goto ERROR1;
                }
                strZhongcihaoGroupName = strTemp;
            }


            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("//zhongcihao/group[@name='" + strZhongcihaoGroupName + "']/database");
            if (nodes.Count == 0)
            {
                strError = "library.xml����δ�����й� '" + strZhongcihaoGroupName + "'��<zhongcihao>/<group>/<database>��ز���";
                goto ERROR1;
            }

            // �������ʽ
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strDbName = DomUtil.GetAttr(node, "name");

                if (string.IsNullOrEmpty(strDbName) == true)
                {
                    strError = "<database>Ԫ�ر����зǿյ�name����ֵ";
                    goto ERROR1;
                }

                Debug.Assert(String.IsNullOrEmpty(strDbName) == false, "");

                if (i > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + "��ȡ��")       // 2007/9/14 new add
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strClass) + "/"
                    + "</word><match>left</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            }

            if (nodes.Count > 0)
                strQueryXml = "<group>" + strQueryXml + "</group>";

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            long lRet = channel.DoSearch(strQueryXml,
                strResultSetName,   // "default",
                "keyid", // strOuputStyle
                out strError);
            if (lRet == -1)
                goto ERROR1;

            if (lRet == 0)
            {
                result.Value = 0;
                result.ErrorInfo = "not found";
                result.ErrorCode = ErrorCode.NotFound;
                return result;
            }


            result.Value = lRet;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }


#if OLD
        // ����ͬ���¼
        public LibraryServerResult SearchUsedZhongcihao(
            SessionInfo sessioninfo,
            string strZhongcihaoGroupName,
            string strClass,
            string strResultSetName,
            out string strQueryXml)
        {
            strQueryXml = "";

            string strError = "";

            LibraryServerResult result = new LibraryServerResult();

            if (String.IsNullOrEmpty(strZhongcihaoGroupName) == true)
            {
                strError = "strZhongcihaoGroupName����ֵ����Ϊ��";
                goto ERROR1;
            }

            if (strZhongcihaoGroupName[0] == '!')
            {
                string strTemp = GetZhongcihaoGroupName(strZhongcihaoGroupName.Substring(1));

                if (strTemp == null)
                {
                    strError = "��Ŀ���� " + strZhongcihaoGroupName.Substring(1) + " û���ҵ���Ӧ���ִκ�����";
                    goto ERROR1;
                }
                strZhongcihaoGroupName = strTemp;
            }


            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("//zhongcihao/group[@name='" + strZhongcihaoGroupName + "']/database");
            if (nodes.Count == 0)
            {
                strError = "library.xml����δ�����й� '"+strZhongcihaoGroupName+"'��<zhongcihao>/<group>/<database>��ز���";
                goto ERROR1;
            }

            // �������ʽ
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strDbName = DomUtil.GetAttr(node, "name");
                string strLeftFrom = DomUtil.GetAttr(node, "leftfrom");

                Debug.Assert(String.IsNullOrEmpty(strDbName) == false, "");

                if (i > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + strLeftFrom)       // 2007/9/14 new add
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strClass)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            }

            if (nodes.Count > 0)
                strQueryXml = "<group>" + strQueryXml + "</group>";

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            long lRet = channel.DoSearch(strQueryXml,
                strResultSetName,   // "default",
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
                goto ERROR1;

            if (lRet == 0)
            {
                result.Value = 0;
                result.ErrorInfo = "not found";
                result.ErrorCode = ErrorCode.NotFound;
                return result;
            }


            result.Value = lRet;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }
#endif


    }

    // �ִκż������н����һ��
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class ZhongcihaoSearchResult
    {
        [DataMember]
        public string Path = "";    // ��¼·��
        [DataMember]
        public string Zhongcihao = "";  // ͬ�������ֺ�
        [DataMember]
        public string[] Cols = null;    // ������С�һ��Ϊ���������ߣ�������ĿժҪ
    }

    // ���ݿ���й��ִκŵ�����
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class DbZhongcihaoProperty
    {
        [DataMember]
        public string DbName = "";
        [DataMember]
        public string NumberXPath = ""; // "rightxpath"
        [DataMember]
        public string TitleXPath = "";  // "titlexpath"
        [DataMember]
        public string AuthorXPath = ""; // "authorxpath"

    }
}
