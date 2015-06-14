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
using System.Text.RegularExpressions;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// �������Ǻ���ȡ�Ź�����صĴ���
    /// </summary>
    public partial class LibraryApplication
    {

        /*
    <callNumber>
        <group name="����" zhongcihaodb="�ִκ�">
            <location name="���ؿ�" />
            <location name="��ͨ��" />
        </group>
        <group name="Ӣ��" zhongcihaodb="���ִκſ�">
            <location name="Ӣ�Ļ��ؿ�" />
            <location name="Ӣ����ͨ��" />
        </group>
    </callNumber>         * */

        // ͨ���ݲصص����õ��ż�group��
        string GetArrangeGroupName(string strLocation)
        {
            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//callNumber/group[./location[@name='" + strLocation + "']]");
            if (node == null)
            {
                XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("//callNumber/group/location");
                if (nodes.Count == 0)
                    return null;
                foreach (XmlNode current in nodes)
                {
                    string strPattern = DomUtil.GetAttr(current, "name");
                    if (LibraryServerUtil.MatchLocationName(strLocation, strPattern) == true)
                        return DomUtil.GetAttr(current.ParentNode, "name");
                }

                return null;
            }

            return DomUtil.GetAttr(node, "name");
        }

        // ͨ�����ż���ϵ������ִκſ���
        string GetTailDbName(string strArrangeGroupName)
        {
            if (String.IsNullOrEmpty(strArrangeGroupName) == true)
                return null;

            if (strArrangeGroupName[0] == '!')
            {
                string strTemp = GetArrangeGroupName(strArrangeGroupName.Substring(1));

                if (strTemp == null)
                {
                    return null;
                }
                strArrangeGroupName = strTemp;
            }

            XmlNode node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("//callNumber/group[@name='" + strArrangeGroupName + "']");
            if (node == null)
                return null;

            return DomUtil.GetAttr(node, "zhongcihaodb");
        }

        // (����һ���ż���ϵ)������ĳһ���ͬ�������ȡ��
        // parameters:
        //      strArrangeGroupName �ż���ϵ�������Ϊ"!xxx"��ʽ����ʾͨ���ݲصص�������ʾ�ż���ϵ��
        public LibraryServerResult SearchOneClassCallNumber(
            SessionInfo sessioninfo,
            string strArrangeGroupName,
            string strClass,
            string strResultSetName,
            out string strQueryXml)
        {
            strQueryXml = "";

            string strError = "";

            LibraryServerResult result = new LibraryServerResult();

            if (String.IsNullOrEmpty(strArrangeGroupName) == true)
            {
                strError = "strArrangeGroupName����ֵ����Ϊ��";
                goto ERROR1;
            }

            if (strArrangeGroupName[0] == '!')
            {
                string strTemp = GetArrangeGroupName(strArrangeGroupName.Substring(1));

                if (strTemp == null)
                {
                    strError = "�ݲصص��� " + strArrangeGroupName.Substring(1) + " û���ҵ���Ӧ���ż���ϵ��";
                    goto ERROR1;
                }
                strArrangeGroupName = strTemp;
            }

            // <location>Ԫ������
            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("//callNumber/group[@name='" + strArrangeGroupName + "']/location");
            if (nodes.Count == 0)
            {
                strError = "library.xml����δ�����й� '" + strArrangeGroupName + "' ��<callNumber>/<group>/<location>��ز���";
                goto ERROR1;
            }

            string strTargetList = "";

            // ��������ʵ���
            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                string strItemDbName = this.ItemDbs[i].DbName;

                if (String.IsNullOrEmpty(strItemDbName) == true)
                    continue;

                if (String.IsNullOrEmpty(strTargetList) == false)
                    strTargetList += ";";
                strTargetList += strItemDbName + ":��ȡ���";
            }

            int nCount = 0;
            // �������ʽ
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strLocationName = DomUtil.GetAttr(node, "name");

                if (String.IsNullOrEmpty(strLocationName) == true)
                    continue;

                if (nCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strLocationName = strLocationName.Replace("*", "%");

                /*
                strQueryXml += "<item><word>"
                    + StringUtil.GetXmlStringSimple(strLocationName + "|" + strClass)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang>";
                 * */
                strQueryXml += "<item><word>"
    + StringUtil.GetXmlStringSimple(strLocationName + "|" + strClass + "/")
    + "</word><match>left</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang>";

                nCount++;
            }

            strQueryXml = "<target list='"
                    + StringUtil.GetXmlStringSimple(strTargetList)       // 2007/9/14
                    + "'>" + strQueryXml + "</target>";

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            long lRet = channel.DoSearch(strQueryXml,
                strResultSetName,   // "default",
                "keyid", // "", // strOuputStyle
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

        // parameters:
        //      strBrowseInfoStyle  ���ص����ԡ�cols ����ʵ���¼�������
        public LibraryServerResult GetCallNumberSearchResult(
            SessionInfo sessioninfo,
            string strArrangeGroupName,
            string strResultSetName,
            long lStart,
            long lCount,
            string strBrowseInfoStyle,
            string strLang,
            out CallNumberSearchResult[] searchresults)
        {
            string strError = "";
            searchresults = null;

            LibraryServerResult result = new LibraryServerResult();
            // int nRet = 0;
            long lRet = 0;

            if (String.IsNullOrEmpty(strArrangeGroupName) == true)
            {
                strError = "strArrangeGroupName����ֵ����Ϊ��";
                goto ERROR1;
            }

            if (strArrangeGroupName[0] == '!')
            {
                string strTemp = GetArrangeGroupName(strArrangeGroupName.Substring(1));

                if (strTemp == null)
                {
                    strError = "�ݲصص��� " + strArrangeGroupName.Substring(1) + " û���ҵ���Ӧ���ż���ϵ��";
                    goto ERROR1;
                }
                strArrangeGroupName = strTemp;
            }

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                result.Value = -1;
                result.ErrorInfo = "get channel error";
                result.ErrorCode = ErrorCode.SystemError;
                return result;
            }


            if (String.IsNullOrEmpty(strResultSetName) == true)
                strResultSetName = "default";

            bool bCols = StringUtil.IsInList("cols", strBrowseInfoStyle);

            string strBrowseStyle = "keyid,id,key";
            if (bCols == true)
                strBrowseStyle += ",cols,format:cfgs/browse_callnumber";

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


            searchresults = new CallNumberSearchResult[origin_searchresults.Length];

            for (int i = 0; i < origin_searchresults.Length; i++)
            {
                CallNumberSearchResult item = new CallNumberSearchResult();

                Record record = origin_searchresults[i];
                item.ItemRecPath = record.Path;
                searchresults[i] = item;

                string strLocation = "";
                item.CallNumber = BuildAccessNoKeyString(record.Keys, out strLocation);

                if (bCols == true && record.Cols != null)
                {
                    if (record.Cols.Length > 0)
                        item.ParentID = record.Cols[0];
                    if (record.Cols.Length > 1)
                        item.Location = record.Cols[1];
                    if (record.Cols.Length > 2)
                        item.Barcode = record.Cols[2];
                }

                if (string.IsNullOrEmpty(item.Location) == true)
                    item.Location = strLocation;    // �ô�keys�е����Ĵ��档�����д�Сд�Ĳ��� --- keys�ж��Ǵ�д
#if NO
                if (bCols == true)
                {
                    // ������������Ա
                    string strXml = "";
                    string strMetaData = "";
                    byte[] timestamp = null;
                    string strOutputPath = "";

                    lRet = channel.GetRes(item.ItemRecPath,
                        out strXml,
                        out strMetaData,
                        out timestamp,
                        out strOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        item.ErrorInfo = "��ȡ��¼ '" + item.ItemRecPath + "' ����: " + strError;
                        continue;
                    }

                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strXml);
                    }
                    catch (Exception ex)
                    {
                        item.ErrorInfo = "��¼ '" + item.ItemRecPath + "' XMLװ�ص�DOMʱ����: " + ex.Message;
                        continue;
                    }

                    /*
                    item.CallNumber = DomUtil.GetElementText(dom.DocumentElement,
                        "accessNo");
                     * */
                    item.ParentID = DomUtil.GetElementText(dom.DocumentElement,
                        "parent");
                    item.Location = DomUtil.GetElementText(dom.DocumentElement,
                        "location");
                    item.Barcode = DomUtil.GetElementText(dom.DocumentElement,
                        "barcode");
                }
#endif
            }

            result.Value = lResultCount;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }

        public static string BuildAccessNoKeyString(KeyFrom[] keys,
            out string strLocation)
        {
            strLocation = "";

            if (keys == null || keys.Length == 0)
                return "";
            string strValue = keys[0].Key;
            int nRet = strValue.IndexOf("|");
            if (nRet == -1)
                return strValue;
            strLocation = strValue.Substring(0, nRet);
            return strValue.Substring(nRet + 1);
        }

        // ����ִκ�β��
        public LibraryServerResult GetOneClassTailNumber(
            SessionInfo sessioninfo,
            string strArrangeGroupName,
            string strClass,
            out string strTailNumber)
        {
            strTailNumber = "";

            string strError = "";

            LibraryServerResult result = new LibraryServerResult();

            if (String.IsNullOrEmpty(strArrangeGroupName) == true)
            {
                strError = "strArrangeGroupName����ֵ����Ϊ��";
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
            int nRet = SearchOneClassTailNumberPathAndRecord(
                sessioninfo.Channels,
                strArrangeGroupName,
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
        public LibraryServerResult SetOneClassTailNumber(
            SessionInfo sessioninfo,
            string strAction,
            string strArrangeGroupName,
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
            int nRet = SearchOneClassTailNumberPathAndRecord(
                sessioninfo.Channels,
                strArrangeGroupName,
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

            string strZhongcihaoDbName = GetTailDbName(strArrangeGroupName);
            if (String.IsNullOrEmpty(strZhongcihaoDbName) == true)
            {
                // TODO: ���ﱨ����Ҫ��ȷһЩ�����ڴ���'!'�Ĺݲصص���
                strError = "�޷�ͨ���ż���ϵ�� '" + strArrangeGroupName + "' ����ִκſ���";
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
            else if (strAction == "increase")
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

        // ����β�ż�¼��·���ͼ�¼��
        // return:
        //      -1  error(ע���������ж���������������󷵻�)
        //      0   not found
        //      1   found
        public int SearchOneClassTailNumberPathAndRecord(
            RmsChannelCollection Channels,
            string strArrangeGroupName,
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

            if (strArrangeGroupName == "")
            {
                strError = "��δָ���ż���ϵ��";
                return -1;
            }


            string strZhongcihaoDbName = GetTailDbName(strArrangeGroupName);
            if (String.IsNullOrEmpty(strZhongcihaoDbName) == true)
            {
                strError = "�޷�ͨ���ż���ϵ�� '" + strArrangeGroupName + "' ����ִκſ���";
                return -1;
            }

            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(strZhongcihaoDbName + ":" + "�����")       // 2007/9/14 
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
    }

    // ��ȡ�ż������н����һ��
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class CallNumberSearchResult
    {
        [DataMember]
        public string ItemRecPath = "";    // ʵ���¼·��
        [DataMember]
        public string CallNumber = "";  // ��ȡ��ȫ��
        [DataMember]
        public string Location = "";    // �ݲصص�
        [DataMember]
        public string Barcode = ""; // �������
        // public string[] Cols = null;    // ������С�һ��Ϊ���������ߣ�������ĿժҪ
        [DataMember]
        public string ParentID = "";    // ��(��Ŀ)��¼ID
        [DataMember]
        public string ErrorInfo = "";   // ������Ϣ
    }

}
