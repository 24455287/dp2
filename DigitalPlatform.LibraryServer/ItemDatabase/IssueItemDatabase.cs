using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

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
    /// �������
    /// </summary>
    public class IssueItemDatabase : ItemDatabase
    {
        // Ҫ��Ԫ�����б�
        static string[] core_issue_element_names = new string[] {
                "parent",
                "state",    // ״̬
                "publishTime",  // ����ʱ��
                "issue",    // �����ں�
                "zong",   // ���ں�
                "volume",   // ���
                "orderInfo",    // ������Ϣ
                "comment",  // ע��
                "batchNo",   // ���κ�
                "refID",    // �ο�ID 2010/2/27 add
                "operations", // 2010/4/7 new add
            };

        // (�������������)
        // �Ƚ�������¼, ����������Ҫ����Ϣ�йص��ֶ��Ƿ����˱仯
        // return:
        //      0   û�б仯
        //      1   �б仯
        public override int IsItemInfoChanged(XmlDocument domExist,
            XmlDocument domOldRec)
        {
            for (int i = 0; i < core_issue_element_names.Length; i++)
            {
                string strText1 = DomUtil.GetElementOuterXml(domExist.DocumentElement,
                    core_issue_element_names[i]);
                string strText2 = DomUtil.GetElementOuterXml(domOldRec.DocumentElement,
                    core_issue_element_names[i]);

                if (strText1 != strText2)
                    return 1;
            }

            return 0;
        }

        // DoOperChange()��DoOperMove()���¼�����
        // �ϲ��¾ɼ�¼
        // return:
        //      -1  ����
        //      0   ��ȷ
        //      1   �в����޸�û�ж��֡�˵����strError��
        public override int MergeTwoItemXml(
            SessionInfo sessioninfo,
            XmlDocument domExist,
            XmlDocument domNew,
            out string strMergedXml,
            out string strError)
        {
            strMergedXml = "";
            strError = "";
            int nRet = 0;

            if (sessioninfo != null
&& sessioninfo.Account != null
&& sessioninfo.UserType == "reader")
            {
                strError = "�ڿ��¼��������߽����޸�";
                return -1;
            }

            // �㷨��Ҫ����, ��"�¼�¼"�е�Ҫ���ֶ�, ���ǵ�"�Ѵ��ڼ�¼"��

            /*
            // Ҫ��Ԫ�����б�
            string[] element_names = new string[] {
                "parent",
                "state",    // ״̬
                "publishTime",  // ����ʱ��
                "issue",    // �����ں�
                "zong",   // ���ں�
                "volume",   // ���
                "orderInfo",    // ������Ϣ
                "comment",  // ע��
                "batchNo"   // ���κ�
            };
             * */

            bool bControlled = true;
            {
                XmlNode nodeExistRoot = domExist.DocumentElement.SelectSingleNode("orderInfo");
                if (nodeExistRoot != null)
                {
                    // �Ƿ�ȫ��������ϢƬ���еĹݲصص㶼�ڵ�ǰ�û���Ͻ֮��?
                    // return:
                    //      -1  ����
                    //      0   ����ȫ�����ڹ�Ͻ��Χ��
                    //      1   ���ڹ�Ͻ��Χ��
                    nRet = IsAllOrderControlled(nodeExistRoot,
                        sessioninfo.LibraryCodeList,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                        bControlled = false;
                }

                if (bControlled == true)
                {
                    // �ٿ��������ǲ���Ҳȫ���ڹ�Ͻ֮��
                    XmlNode nodeNewRoot = domNew.DocumentElement.SelectSingleNode("orderInfo");
                    if (nodeNewRoot != null)
                    {
                        // �Ƿ�ȫ��������ϢƬ���еĹݲصص㶼�ڵ�ǰ�û���Ͻ֮��?
                        // return:
                        //      -1  ����
                        //      0   ����ȫ�����ڹ�Ͻ��Χ��
                        //      1   ���ڹ�Ͻ��Χ��
                        nRet = IsAllOrderControlled(nodeNewRoot,
                            sessioninfo.LibraryCodeList,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet == 0)
                            bControlled = false;
                    }
                }
            }

            if (bControlled == true // ������ȫ���õ��Ĺݲصص�����Σ�Ҳ�����޸Ļ����ֶΡ����Ҿ���ɾ�� <orderInfo> ��ĳЩƬ�ϵ�������ֻҪ�¼�¼�в�������ЩƬ�ϣ��͵���ɾ����
                || sessioninfo.GlobalUser == true) // ֻ��ȫ���û������޸Ļ����ֶ�
            {
                for (int i = 0; i < core_issue_element_names.Length; i++)
                {
                    /*
                    string strTextNew = DomUtil.GetElementText(domNew.DocumentElement,
                        element_names[i]);

                    DomUtil.SetElementText(domExist.DocumentElement,
                        element_names[i], strTextNew);
                     * */
                    // 2009/10/24 changed inner-->outer
                    string strTextNew = DomUtil.GetElementOuterXml(domNew.DocumentElement,
                        core_issue_element_names[i]);

                    DomUtil.SetElementOuterXml(domExist.DocumentElement,
                        core_issue_element_names[i], strTextNew);
                }
            }

            // �ֹ��û�Ҫ���ⵥ������<orderInfo>Ԫ��
            if (sessioninfo.GlobalUser == false
                && bControlled == false)
            {
                // �ֹ��û��ύ��<orderInfo>Ԫ���ڿ��ܰ�����<root>Ԫ�ظ���Ҫ���٣���������ζ��Ҫɾ�������<root>Ԫ��
                XmlNode nodeNewRoot = domNew.DocumentElement.SelectSingleNode("orderInfo");
                XmlNode nodeExistRoot = domExist.DocumentElement.SelectSingleNode("orderInfo");
                if (nodeNewRoot != null && nodeExistRoot == null)
                {
                    //strError = "������ֹ��û�Ϊ�ڼ�¼����<orderInfo>Ԫ��";    // ������ǰ�ļ�¼�ʹ���<orderInfo>Ԫ��
                    //return -1;
                    // ����
                    nodeExistRoot = domExist.CreateElement("orderInfo");
                    domExist.DocumentElement.AppendChild(nodeExistRoot);
                }

                if (nodeNewRoot == null || nodeExistRoot == null)
                    goto END1;

                // ���Ѿ����ڵļ�¼���ҳ���ǰ�û��ܹ�Ͻ�Ķ���Ƭ��
                List<XmlNode> exists_overwriteable_nodes = new List<XmlNode>();
                XmlNodeList exist_nodes = nodeExistRoot.SelectNodes("*");
                foreach (XmlNode exist_node in exist_nodes)
                {
                    string strRefID = DomUtil.GetElementText(exist_node, "refID");
                    if (string.IsNullOrEmpty(strRefID) == true)
                        continue;   // �޷���λ����������?
                    string strDistribute = DomUtil.GetElementText(exist_node, "distribute");
                    if (string.IsNullOrEmpty(strDistribute) == true)
                        continue;

                    // �۲�һ���ݲط����ַ����������Ƿ��ڵ�ǰ�û���Ͻ��Χ��
                    // return:
                    //      -1  ����
                    //      0   ������Ͻ��Χ��strError���н���
                    //      1   �ڹ�Ͻ��Χ��
                    nRet = DistributeInControlled(strDistribute,
                sessioninfo.LibraryCodeList,
                out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 1)
                    {
                        exists_overwriteable_nodes.Add(exist_node);
                    }
                }

                // �����ύ�ļ�¼�е�ÿ������Ƭ�Ͻ���ѭ��
                XmlNodeList new_nodes = nodeNewRoot.SelectNodes("*");
                foreach (XmlNode new_node in new_nodes)
                {
                    string strRefID = DomUtil.GetElementText(new_node, "refID");
                    if (string.IsNullOrEmpty(strRefID) == true)
                    {
                        // ǰ���ύ��һ������Ƭ��refidΪ��
                        strError = "�ڼ�¼�еĶ���XMLƬ����<refID>Ԫ�����ݲ���Ϊ��";
                        return -1;
                    }
                    XmlNode exist_node = nodeExistRoot.SelectSingleNode("*[./refID[text()='" + strRefID + "']]");
                    if (exist_node == null)
                    {
                        // ǰ���ύ��һ������Ƭ��ƥ�䲻��refid
                        // ���������XMLƬ�ϣ�����distribute�ַ�������ȫ���ڹ�Ͻ��Χ��������������
                        string strDistribute = DomUtil.GetElementText(new_node, "distribute");
                        if (string.IsNullOrEmpty(strDistribute) == false)
                        {

                            // �۲�һ���ݲط����ַ����������Ƿ��ڵ�ǰ�û���Ͻ��Χ��
                            // return:
                            //      -1  ����
                            //      0   ������Ͻ��Χ��strError���н���
                            //      1   �ڹ�Ͻ��Χ��
                            nRet = DistributeInControlled(strDistribute,
                        sessioninfo.LibraryCodeList,
                        out strError);
                            if (nRet == -1)
                                return -1;
                            if (nRet == 0)
                            {
                                strError = "�ܵ�ǰ�û��ķֹ��û�������ƣ��ڼ�¼�в���������(�����˳�����Ͻ��Χ�ݴ����)����XMLƬ�ϡ�(refID='" + strRefID + "')";
                                return -1;
                            }
                        }

                        // ��domExit��׷��
                        XmlNode new_frag = domExist.CreateElement("root");
                        new_frag.InnerXml = new_node.InnerXml;
                        nodeExistRoot.AppendChild(new_frag);
                        continue;
                    }

                    Debug.Assert(exist_node != null, "");

                    string strTempMergedXml = "";
                    // ����������XMLƬ�Ϻϲ�
                    // parameters:
                    //      strLibraryCodeList  ��ǰ�û���Ͻ�ķֹݴ����б�
                    // return:
                    //      -1  ����
                    //      0   ����
                    //      1   �����˳�Խ��Χ���޸�
                    nRet = MergeOrderNode(exist_node,
            new_node,
            sessioninfo.LibraryCodeList,
            out strTempMergedXml,
            out strError);
                    if (nRet != 0)
                    {
                        strError = "���ڼ�¼�� refid Ϊ '" + strRefID + "' �Ķ���Ƭ�������޸ĳ���Ȩ�޷�Χ: " + strError;
                        return -1;
                    }
                    exist_node.InnerXml = strTempMergedXml;

                    exists_overwriteable_nodes.Remove(exist_node);  // �Ѿ��޸Ĺ����Ѵ��ڽڵ㣬��������ȥ��
                }

                // ɾ����Щ���¼�¼��û�г��ֵģ�����ǰ�û�ʵ�����ܹ�Ͻ�Ľڵ�
                foreach (XmlNode node in exists_overwriteable_nodes)
                {
                    node.ParentNode.RemoveChild(node);
                }
            }

        END1:
            strMergedXml = domExist.OuterXml;
            return 0;
        }

        // �Ƿ�ȫ��������ϢƬ���еĹݲصص㶼�ڵ�ǰ�û���Ͻ֮��?
        // ��Щû�����ֵ�(�ݲصص�)����������ڷֹ��û��Ĺ�Ͻ��Χ���⡣û�����ֵĹݲصص㣬�ڶ���ʱ��Ϊ�ص�δ��
        // return:
        //      -1  ����
        //      0   ����ȫ�����ڹ�Ͻ��Χ��
        //      1   ���ڹ�Ͻ��Χ��
        int IsAllOrderControlled(XmlNode nodeOrderInfo,
            string strLibraryCodeList,
            out string strError)
        {
            strError = "";

            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == true)
                return 1;

            // �����ύ�ļ�¼�е�ÿ������Ƭ�Ͻ���ѭ��
            XmlNodeList nodes = nodeOrderInfo.SelectNodes("*");
            foreach (XmlNode node in nodes)
            {
                string strDistribute = DomUtil.GetElementText(node, "distribute");
                if (string.IsNullOrEmpty(strDistribute) == true)
                    continue;

                // �۲�һ���ݲط����ַ����������Ƿ��ڵ�ǰ�û���Ͻ��Χ��
                // return:
                //      -1  ����
                //      0   ������Ͻ��Χ��strError���н���
                //      1   �ڹ�Ͻ��Χ��
                int nRet = DistributeInControlled(strDistribute,
            strLibraryCodeList,
            out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                    return 0;
#if NO
                LocationCollcetion locations = new LocationCollcetion();
                int nRet = locations.Build(strDistribute, out strError);
                if (nRet == -1)
                {
                    strError = "�ݲط����ַ��� '" + strDistribute + "' ��ʽ����ȷ";
                    return -1;
                }

                foreach (Location location in locations)
                {
                    if (string.IsNullOrEmpty(location.Name) == true)
                        continue;

                    string strLibraryCode = "";
                    string strPureName = "";

                    // ����
                    LibraryApplication.ParseCalendarName(location.Name,
                out strLibraryCode,
                out strPureName);

                    if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                    {
                        strError = "�ݴ��� '" + strLibraryCode + "' ���ڷ�Χ '" + strLibraryCodeList + "' ��";
                        return 0;
                    }
                }
#endif


            }

            return 1;
        }

        // ����������XMLƬ�Ϻϲ�
        // ���ɵĺ��µĶ���ȫ��Ͻ��Χ�ڣ��������µ�ȫ���滻�ɵģ�����ֻ�����滻<distribute>Ԫ������
        // parameters:
        //      strLibraryCodeList  ��ǰ�û���Ͻ�ķֹݴ����б�
        //      strMergedXml    [out]��Χ����<root>Ԫ�ص�InnerXml
        // return:
        //      -1  ����
        //      0   ����
        //      1   �����˳�Խ��Χ���޸�
        public static int MergeOrderNode(XmlNode exist_node,
            XmlNode new_node,
            string strLibraryCodeList,
            out string strMergedXml,
            out string strError)
        {
            strError = "";
            strMergedXml = "";
            int nRet = 0;

            Debug.Assert(SessionInfo.IsGlobalUser(strLibraryCodeList) == false, "ȫ���û���Ӧ���ú��� MergeOrderNode()");

            string strExistDistribute = DomUtil.GetElementText(exist_node, "distribute");
            string strNewDistribute = DomUtil.GetElementText(new_node, "distribute");

            bool bExistControlled = true;
            bool bNewControlled = true;

            if (string.IsNullOrEmpty(strExistDistribute) == false)
            {
                // �۲�һ���ݲط����ַ����������Ƿ��ڵ�ǰ�û���Ͻ��Χ��
                // return:
                //      -1  ����
                //      0   ������Ͻ��Χ��strError���н���
                //      1   �ڹ�Ͻ��Χ��
                nRet = DistributeInControlled(strExistDistribute,
            strLibraryCodeList,
            out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                    bExistControlled = false;
            }

            if (string.IsNullOrEmpty(strNewDistribute) == false)
            {
                // �۲�һ���ݲط����ַ����������Ƿ��ڵ�ǰ�û���Ͻ��Χ��
                // return:
                //      -1  ����
                //      0   ������Ͻ��Χ��strError���н���
                //      1   �ڹ�Ͻ��Χ��
                nRet = DistributeInControlled(strNewDistribute,
            strLibraryCodeList,
            out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                    bNewControlled = false;
            }

            if (bExistControlled == true && bNewControlled == true)
            {
                // ���ɵĺ��µĶ���ȫ��Ͻ��Χ�ڣ��������µ�ȫ���滻�ɵ�
                strMergedXml = new_node.InnerXml;
                return 0;
            }

            string strExistCopy = DomUtil.GetElementText(exist_node, "copy");
            string strExistPrice = DomUtil.GetElementText(exist_node, "price");

            string strChangedCopy = DomUtil.GetElementText(new_node, "copy");
            string strChangedPrice = DomUtil.GetElementText(new_node, "price");

            // �Ƚ����������ַ���
            {
                string strExistOldValue = "";
                string strExistNewValue = "";
                ParseOldNewValue(strExistCopy,
            out strExistOldValue,
            out strExistNewValue);

                string strChangedOldValue = "";
                string strChangedNewValue = "";
                ParseOldNewValue(strChangedCopy,
            out strChangedOldValue,
            out strChangedNewValue);

                if (strExistOldValue != strChangedOldValue)
                {
                    strError = "��������(��������ߵĲ���)�������޸ġ�(ԭ��='"+strExistCopy+"',�µ�='"+strChangedCopy+"')";
                    return 1;
                }

                // ������������ĸı䣬�Ƿ����ú�distribute�ַ����ڵĸı��Ǻ�
            }

            // �Ƚ������۸��ַ���
            {
                string strExistOldValue = "";
                string strExistNewValue = "";
                ParseOldNewValue(strExistPrice,
            out strExistOldValue,
            out strExistNewValue);

                string strChangedOldValue = "";
                string strChangedNewValue = "";
                ParseOldNewValue(strChangedPrice,
            out strChangedOldValue,
            out strChangedNewValue);

                if (strExistOldValue != strChangedOldValue)
                {
                    strError = "������(��������ߵĲ���)�������޸ġ�(ԭ��='" + strExistPrice + "',�µ�='" + strChangedPrice + "')";
                    return 1;
                }
                if (strExistNewValue != strChangedNewValue)
                {
                    strError = "���ռ�(�����еĲ���)�������޸ġ�(ԭ��='" + strExistPrice + "',�µ�='" + strChangedPrice + "')";
                    return 1;
                }
            }

            LocationCollection new_locations = new LocationCollection();
            nRet = new_locations.Build(strNewDistribute, out strError);
            if (nRet == -1)
            {
                strError = "�ݲط����ַ��� '" + strNewDistribute + "' ��ʽ����ȷ";
                return -1;
            }

            LocationCollection exist_locations = new LocationCollection();
            nRet = exist_locations.Build(strExistDistribute, out strError);
            if (nRet == -1)
            {
                strError = "�ݲط����ַ��� '" + strExistDistribute + "' ��ʽ����ȷ";
                return -1;
            }

            if (exist_locations.Count != new_locations.Count)
            {
                strError = "�ݲط���������������˸ı�(ԭ��=" + exist_locations.Count.ToString() + "���µ�=" + new_locations.Count.ToString() + ")";
                return 1;
            }

            for (int i = 0; i < exist_locations.Count; i++)
            {
                Location exist_location = exist_locations[i];
                Location new_location = new_locations[i];

                if (exist_location.Name != new_location.Name)
                {
                    // ��һ������Ƿ�ݴ��벿�ָı���
                    string strCode1 = "";
                    string strPureName = "";
                    string strCode2 = "";

                    // ����
                    LibraryApplication.ParseCalendarName(exist_location.Name,
                        out strCode1,
                        out strPureName);
                    LibraryApplication.ParseCalendarName(new_location.Name,
                        out strCode2,
                        out strPureName);
                    // ֻҪ�ݴ��벿�ֲ��ı伴��
                    if (strCode1 != strCode2)
                    {
                        strError = "�� " + (i + 1).ToString() + " ���ݲط������������(�Ĺݴ��벿��)�����ı� (ԭ��='" + exist_location.Name + "',�µ�='" + new_location.Name + "')";
                        return 1;
                    }
                }

                if (exist_location.RefID != new_location.RefID)
                {
                    string strLibraryCode = "";
                    string strPureName = "";

                    // ����
                    LibraryApplication.ParseCalendarName(exist_location.Name,
                out strLibraryCode,
                out strPureName);
                    if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                    {
                        strError = "�ݴ��� '" + strLibraryCode + "' ���ڷ�Χ '" + strLibraryCodeList + "' �ڣ�����������յǲ�����";
                        return 1;
                    }
                }
            }

            // ���ɵ�XMLƬ��װ�룬ֻ�޸����������Ԫ��ֵ���������Ա�֤����Ԫ�������ԭ��¼���ݲ����޸�
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(exist_node.OuterXml);
            }
            catch (Exception ex)
            {
                strError = "exist_node.OuterXmlװ��XMLDOMʧ��: " + ex.Message;
                return -1;
            }

            DomUtil.SetElementText(dom.DocumentElement, "copy", strChangedCopy);
            DomUtil.SetElementText(dom.DocumentElement, "price", strChangedPrice);
            DomUtil.SetElementText(dom.DocumentElement, "distribute", strNewDistribute);

            strMergedXml = dom.DocumentElement.InnerXml;
            return 0;
        }

        public static string LinkOldNewValue(string strOldValue,
            string strNewValue)
        {
            if (String.IsNullOrEmpty(strNewValue) == true)
                return strOldValue;

            if (strOldValue == strNewValue)
            {
                if (String.IsNullOrEmpty(strOldValue) == true)  // �¾ɾ�Ϊ��
                    return "";

                return strOldValue + "[=]";
            }

            return strOldValue + "[" + strNewValue + "]";
        }


        // ���� "old[new]" �ڵ�����ֵ
        public static void ParseOldNewValue(string strValue,
            out string strOldValue,
            out string strNewValue)
        {
            strOldValue = "";
            strNewValue = "";
            int nRet = strValue.IndexOf("[");
            if (nRet == -1)
            {
                strOldValue = strValue;
                strNewValue = "";
                return;
            }

            strOldValue = strValue.Substring(0, nRet).Trim();
            strNewValue = strValue.Substring(nRet + 1).Trim();

            // ȥ��ĩβ��']'
            if (strNewValue.Length > 0 && strNewValue[strNewValue.Length - 1] == ']')
                strNewValue = strNewValue.Substring(0, strNewValue.Length - 1);

            if (strNewValue == "=")
                strNewValue = strOldValue;
        }

        public IssueItemDatabase(LibraryApplication app) : base(app)
        {
        }


#if NO
        public int BuildLocateParam(string strBiblioRecPath,
            string strPublishTime,
            out List<string> locateParam,
            out string strError)
        {
            strError = "";
            locateParam = null;

            int nRet = 0;

            string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
            string strItemDbName = "";

            // ������Ŀ����, �ҵ���Ӧ���������
            // return:
            //      -1  ����
            //      0   û���ҵ�(��Ŀ��)
            //      1   �ҵ�
            nRet = this.GetItemDbName(strBiblioDbName,
                out strItemDbName,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                strError = "��Ŀ�� '" + strBiblioDbName + "' û���ҵ�";
                goto ERROR1;
            }
            if (String.IsNullOrEmpty(strItemDbName) == true)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' ��Ӧ��" + this.ItemName + "����û�ж���";
                goto ERROR1;
            }

            string strParentID = ResPath.GetRecordId(strBiblioRecPath);

            locateParam = new List<string>();
            locateParam.Add(strItemDbName);
            locateParam.Add(strParentID);
            locateParam.Add(strPublishTime);

            return 0;
        ERROR1:
            return -1;
        }
#endif
        public override int BuildLocateParam(// string strBiblioRecPath,
string strRefID,
out List<string> locateParam,
out string strError)
        {
            strError = "";
            locateParam = null;

            /*
            int nRet = 0;

            string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
            string strIssueDbName = "";

            // ������Ŀ����, �ҵ���Ӧ���������
            // return:
            //      -1  ����
            //      0   û���ҵ�(��Ŀ��)
            //      1   �ҵ�
            nRet = this.GetItemDbName(strBiblioDbName,
                out strIssueDbName,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                strError = "��Ŀ�� '" + strBiblioDbName + "' û���ҵ�";
                goto ERROR1;
            }
            if (String.IsNullOrEmpty(strIssueDbName) == true)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' ��Ӧ��" + this.ItemName + "����û�ж���";
                goto ERROR1;
            }
             * */

            locateParam = new List<string>();
            locateParam.Add(strRefID);

            return 0;
            /*
        ERROR1:
            return -1;
             * */
        }

#if NO
        // �������������
        // �������ڻ�ȡ�����¼��XML����ʽ
        public override int MakeGetItemRecXmlSearchQuery(
            List<string> locateParams,
            int nMax,
            out string strQueryXml,
            out string strError)
        {
            strQueryXml = "";
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 3)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ3��";
                return -1;
            }

            string strIssueDbName = locateParams[0];
            string strParentID = locateParams[1];
            string strPublishTime = locateParams[2];


            strQueryXml = "<target list='"
        + StringUtil.GetXmlStringSimple(strIssueDbName + ":" + "����ʱ��")
        + "'><item><word>"
        + StringUtil.GetXmlStringSimple(strPublishTime)
        + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            strQueryXml += "<operator value='AND'/>";


            strQueryXml += "<target list='"
                    + StringUtil.GetXmlStringSimple(strIssueDbName + ":" + "����¼")
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strParentID)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            strQueryXml = "<group>" + strQueryXml + "</group>";

            return 0;
        }
#endif
        // �������ڻ�ȡ�����¼��XML����ʽ
        public override int MakeGetItemRecXmlSearchQuery(
            List<string> locateParams,
            int nMax,
            out string strQueryXml,
            out string strError)
        {
            strQueryXml = "";
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 1)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ1��";
                return -1;
            }

            string strRefID = locateParams[0];

            // �������ʽ
            int nDbCount = 0;
            for (int i = 0; i < this.App.ItemDbs.Count; i++)
            {
                string strDbName = this.App.ItemDbs[i].IssueDbName;

                if (String.IsNullOrEmpty(strDbName) == true)
                    continue;

                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + "�ο�ID")
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strRefID)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>" + nMax.ToString() + "</maxCount></item><lang>zh</lang></target>";

                if (nDbCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
                nDbCount++;
            }

            if (nDbCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            return 0;
        }

#if NO
        // �������������
        // ���춨λ��ʾ��Ϣ�����ڱ���
        public override int GetLocateText(
            List<string> locateParams,
            out string strText,
            out string strError)
        {
            strText = "";
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 3)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ3��";
                return -1;
            }
            string strIssueDbName = locateParams[0];
            string strParentID = locateParams[1];
            string strPublishTime = locateParams[2];

            strText = "��������Ϊ '" + strPublishTime + "'���ڿ�Ϊ '" + strIssueDbName + "'������¼IDΪ '" + strParentID + "'";
            return 0;
        }
#endif


#if NO
        // �������������
        // �۲��Ѵ��ڵļ�¼�У�Ψһ���ֶ��Ƿ��Ҫ���һ��
        // return:
        //      -1  ����
        //      0   һ��
        //      1   ��һ�¡�������Ϣ��strError��
        public override int IsLocateInfoCorrect(
            List<string> locateParams,
            XmlDocument domExist,
            out string strError)
        {
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 3)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ3��";
                return -1;
            }
            string strIssueDbName = locateParams[0];
            string strParentID = locateParams[1];
            string strPublishTime = locateParams[2];


            if (String.IsNullOrEmpty(strPublishTime) == false)
            {
                string strExistingPublishTime = DomUtil.GetElementText(domExist.DocumentElement,
                    "publishTime");
                if (strExistingPublishTime != strPublishTime)
                {
                    strError = "�ڼ�¼��<publishTime>Ԫ���еĳ���ʱ�� '" + strExistingPublishTime + "' ��ͨ��ɾ����������ָ���ĳ���ʱ�� '" + strPublishTime + "' ��һ�¡�";
                    return 1;
                }
            }

            return 0;
        }
#endif


        // �۲��Ѿ����ڵļ�¼�Ƿ�����ͨ��Ϣ
        // return:
        //      -1  ����
        //      0   û��
        //      1   �С�������Ϣ��strError��
        public override int HasCirculationInfo(XmlDocument domExist,
            out string strError)
        {
            strError = "";
            return 0;
        }

        // ��¼�Ƿ�����ɾ��?
        // return:
        //      -1  ����������ɾ����
        //      0   ������ɾ������ΪȨ�޲�����ԭ��ԭ����strError��
        //      1   ����ɾ��
        public override int CanDelete(
            SessionInfo sessioninfo,
            XmlDocument domExist,
            out string strError)
        {
            strError = "";
            if (sessioninfo == null)
            {
                strError = "sessioninfo == null";
                return -1;
            }

            if (sessioninfo.GlobalUser == false)
            {
                XmlNode nodeExistRoot = domExist.DocumentElement.SelectSingleNode("orderInfo");
                if (nodeExistRoot != null)
                {
                    // �Ƿ�ȫ��������ϢƬ���еĹݲصص㶼�ڵ�ǰ�û���Ͻ֮��?
                    // return:
                    //      -1  ����
                    //      0   ����ȫ�����ڹ�Ͻ��Χ��
                    //      1   ���ڹ�Ͻ��Χ��
                    int nRet = IsAllOrderControlled(nodeExistRoot,
                        sessioninfo.LibraryCodeList,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                    {
                        strError = "������˳�Խ��ǰ�û���Ͻ��Χ�ķֹݹݲ���Ϣ��ɾ���ڼ�¼�Ĳ������ܾ���" + strError;
                        return 0;
                    }
                }
            }
            return 1;
        }

#if NO
        // ��λ����ֵ�Ƿ�Ϊ��?
        // return:
        //      -1  ����
        //      0   ��Ϊ��
        //      1   Ϊ��(��ʱ��Ҫ��strError�и�������˵������)
        public override int IsLocateParamNullOrEmpty(
            List<string> locateParams,
            out string strError)
        {
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 3)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ3��";
                return -1;
            }
            string strIssueDbName = locateParams[0];
            string strParentID = locateParams[1];
            string strPublishTime = locateParams[2];

            if (String.IsNullOrEmpty(strPublishTime) == true)
            {
                strError = "<publishTime>Ԫ���еĳ���ʱ��Ϊ��";
                return 1;
            }
            return 0;
        }
#endif

        // �������ơ�
        public override string ItemName
        {
            get
            {
                return "��";
            }
        }

        // �����ڲ����ơ�
        public override string ItemNameInternal
        {
            get
            {
                return "Issue";
            }
        }

        public override string DefaultResultsetName
        {
            get
            {
                return "issues";
            }
        }

        // ׼��д����־��SetXXX�����ַ��������硰SetEntity�� ��SetIssue��
        public override string OperLogSetName
        {
            get
            {
                return "setIssue";
            }
        }

        public override string SetApiName
        {
            get
            {
                return "SetIssues";
            }
        }

        public override string GetApiName
        {
            get
            {
                return "GetIssues";
            }
        }

        // �Ƿ��������¼�¼?
        // TODO: �Ƿ��������г�����Ͻ��Χ�Ķ�����Ϣ���������Ȼ�������������˳����Ĳ���<root>����Ƭ�ϣ������������dp2circulationǰ��Ҫ���и��죬���ֹ��û������ڼ�¼��ʱ�򣬲�Ҫ�ύ�����Լ���Ͻ��Χ���ڼ�¼����
        // parameters:
        // return:
        //      -1  �����������޸ġ�
        //      0   ������������ΪȨ�޲�����ԭ��ԭ����strError��
        //      1   ���Դ���
        public override int CanCreate(
            SessionInfo sessioninfo,
            XmlDocument domNew,
            out string strError)
        {
            strError = "";

            if (sessioninfo == null)
            {
                strError = "sessioninfo == null";
                return -1;
            }

            if (sessioninfo.GlobalUser == false)
            {
                XmlNode nodeExistRoot = domNew.DocumentElement.SelectSingleNode("orderInfo");
                if (nodeExistRoot != null)
                {
                    // �Ƿ�ȫ��������ϢƬ���еĹݲصص㶼�ڵ�ǰ�û���Ͻ֮��?
                    // return:
                    //      -1  ����
                    //      0   ����ȫ�����ڹ�Ͻ��Χ��
                    //      1   ���ڹ�Ͻ��Χ��
                    int nRet = IsAllOrderControlled(nodeExistRoot,
                        sessioninfo.LibraryCodeList,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                    {
                        strError = "������˳�Խ��ǰ�û���Ͻ��Χ�ķֹݹݲ���Ϣ�������ڼ�¼�Ĳ������ܾ���" + strError;
                        return 0;
                    }
                }
            }

            return 1;
        }

        // ������ʺϱ�����������¼
        public override int BuildNewItemRecord(
            SessionInfo sessioninfo,
            bool bForce,
            string strBiblioRecId,
            string strOriginXml,
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strOriginXml);
            }
            catch (Exception ex)
            {
                strError = "װ��strOriginXml��DOMʱ����: " + ex.Message;
                return -1;
            }

            // 2010/4/2
            DomUtil.SetElementText(dom.DocumentElement,
                "parent",
                strBiblioRecId);

            strXml = dom.OuterXml;

            return 0;
        }


        // ����������ݿ���
        // return:
        //      -1  error
        //      0   û���ҵ�(��Ŀ��)
        //      1   found
        public override int GetItemDbName(string strBiblioDbName,
            out string strItemDbName,
            out string strError)
        {
            return this.App.GetIssueDbName(strBiblioDbName,
                out strItemDbName,
                out strError);
        }

        // 2012/4/27
        public override bool IsItemDbName(string strItemDbName)
        {
            return this.App.IsIssueDbName(strItemDbName);
        }

        public override int VerifyItem(
    LibraryHost host,
    string strAction,
    XmlDocument itemdom,
    out string strError)
        {
            strError = "";

            // ִ�к���
            try
            {
                return host.VerifyIssue(strAction,
                    itemdom,
                    out strError);
            }
            catch (Exception ex)
            {
                strError = "ִ�нű����� '" + "VerifyIssue" + "' ʱ����" + ex.Message;
                return -1;
            }

            return 0;
        }

#if NO
        // ���¾������¼�а����Ķ�λ��Ϣ���бȽ�, �����Ƿ����˱仯(��������Ҫ����)
        // parameters:
        //      oldLocateParam   ˳�㷵�ؾɼ�¼�еĶ�λ����
        //      newLocateParam   ˳�㷵���¼�¼�еĶ�λ����
        // return:
        //      -1  ����
        //      0   ���
        //      1   �����
        public override int CompareTwoItemLocateInfo(
            string strItemDbName,
            XmlDocument domOldRec,
            XmlDocument domNewRec,
            out List<string> oldLocateParam,
            out List<string> newLocateParam,
            out string strError)
        {
            strError = "";

            string strOldPublishTime = DomUtil.GetElementText(domOldRec.DocumentElement,
                "publishTime");

            string strNewPublishTime = DomUtil.GetElementText(domNewRec.DocumentElement,
                "publishTime");

            string strOldParentID = DomUtil.GetElementText(domOldRec.DocumentElement,
                "parent");

            string strNewParentID = DomUtil.GetElementText(domNewRec.DocumentElement,
                "parent");

            oldLocateParam = new List<string>();
            oldLocateParam.Add(strItemDbName);
            oldLocateParam.Add(strOldParentID);
            oldLocateParam.Add(strOldPublishTime);

            newLocateParam = new List<string>();
            newLocateParam.Add(strItemDbName);
            newLocateParam.Add(strNewParentID);
            newLocateParam.Add(strNewPublishTime);

            if (strOldPublishTime != strNewPublishTime)
                return 1;   // �����

            return 0;   // ���
        }
#endif


#if NO
        public override void LockItem(List<string> locateParam)
        {
            string strIssueDbName = locateParam[0];
            string strParentID = locateParam[1];
            string strPublishTime = locateParam[2];

            this.App.EntityLocks.LockForWrite(
                "issue:" + strIssueDbName + "|" + strParentID + "|" + strPublishTime);
        }

        public override void UnlockItem(List<string> locateParam)
        {
            string strIssueDbName = locateParam[0];
            string strParentID = locateParam[1];
            string strPublishTime = locateParam[2];

            this.App.EntityLocks.UnlockForWrite(
                "issue:" + strIssueDbName + "|" + strParentID + "|" + strPublishTime);
        }
#endif

    }
}
