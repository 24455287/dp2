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
    /// �����������ݿ�
    /// locateParam����
    /// 1) �������� 2)����¼id 3)index
    /// </summary>
    public class OrderItemDatabase : ItemDatabase
    {
        // Ҫ��Ԫ�����б�
        static string[] core_order_element_names = new string[] {
                "parent",   // ����¼ID
                "index",    // ���
                "state",    // ״̬
                "catalogNo",    // ��Ŀ�� 2008/8/31 new add
                "seller",   // ����
                "source",   // 2008/2/15 new add ������Դ
                "range",    // ������ʱ�䷶Χ
                "issueCount",   // ����(ʱ�䷶Χ��)��Խ������? �Ա�����ܼ�
                "copy", // ������
                "price",    // �ᡢ�ڵ���
                "totalPrice",   // �ܼ�
                "orderTime",    // ����ʱ��
                "orderID",  // ������
                "distribute",   // �ݲط���
                "class",    // ��� 2008/8/31 new add
                "comment",  // ע��
                "batchNo",  // ���κ�
                "sellerAddress",    // ���̵�ַ�����ڷǴ��ڶ������� 2009/2/13 new add
                "refID",    // �ο�ID 2010/3/15 add
                "operations", // 2010/4/8 new add
        };

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
                strError = "�������¼��������߽����޸�";
                return -1;
            }


            // �㷨��Ҫ����, ��"�¼�¼"�е�Ҫ���ֶ�, ���ǵ�"�Ѵ��ڼ�¼"��

            /*
            // Ҫ��Ԫ�����б�
            string[] element_names = new string[] {
                "parent",   // ����¼ID
                "index",    // ���
                "state",    // ״̬
                "catalogNo",    // ��Ŀ�� 2008/8/31 new add
                "seller",   // ����
                "source",   // 2008/2/15 new add ������Դ
                "range",    // ������ʱ�䷶Χ
                "issueCount",   // ����(ʱ�䷶Χ��)��Խ������? �Ա�����ܼ�
                "copy", // ������
                "price",    // �ᡢ�ڵ���
                "totalPrice",   // �ܼ�
                "orderTime",    // ����ʱ��
                "orderID",  // ������
                "distribute",   // �ݲط���
                "class",    // ��� 2008/8/31 new add
                "comment",  // ע��
                "batchNo",  // ���κ�
            };
             * */

            bool bControlled = true;
            if (sessioninfo.GlobalUser == false)
            {
                string strDistribute = DomUtil.GetElementText(domExist.DocumentElement, "distribute");
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
                    bControlled = false;

                if (bControlled == true)
                {
                    // �ٿ��������ǲ���Ҳȫ���ڹ�Ͻ֮��
                    strDistribute = DomUtil.GetElementText(domNew.DocumentElement, "distribute");
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
                        bControlled = false;
                }
            }


            if (bControlled == true // ������ȫ���õ��Ĺݲصص������
    || sessioninfo.GlobalUser == true) // ȫ���û�
            {
                for (int i = 0; i < core_order_element_names.Length; i++)
                {
                    /*
                    string strTextNew = DomUtil.GetElementText(domNew.DocumentElement,
                        core_order_element_names[i]);

                    DomUtil.SetElementText(domExist.DocumentElement,
                        core_order_element_names[i], strTextNew);
                     * */

                    // 2009/10/23 changed inner-->outer
                    string strTextNew = DomUtil.GetElementOuterXml(domNew.DocumentElement,
                        core_order_element_names[i]);

                    DomUtil.SetElementOuterXml(domExist.DocumentElement,
                        core_order_element_names[i], strTextNew);
                }
            }

            string strWarning = "";

            // �ֹ��û�Ҫ���ⵥ������<distribute>Ԫ��
            if (sessioninfo.GlobalUser == false
                && bControlled == false)
            {
                string strRefID = DomUtil.GetElementText(domNew.DocumentElement, "refID");

                string strTempMergedXml = "";
                // ����������XMLƬ�Ϻϲ�
                // parameters:
                //      strLibraryCodeList  ��ǰ�û���Ͻ�ķֹݴ����б�
                // return:
                //      -1  ����
                //      0   ����
                //      1   �����˳�Խ��Χ���޸�
                //      2   �в����޸�����û�ж���
                nRet = MergeOrderNode(domExist.DocumentElement,
        domNew.DocumentElement,
        sessioninfo.LibraryCodeList,
        out strTempMergedXml,
        out strError);
                if (nRet == -1)
                {
                    strError = "�ϲ��¾ɼ�¼ʱ����: " + strError;
                    return -1;
                }
                if (nRet == 1)
                {
                    strError = "��ǰ�û��Բ���ȫ��Ͻ�Ķ��������޸ĳ���Ȩ�޷�Χ: " + strError;
                    return -1;
                }
                if (nRet == 2)
                    strWarning = strError;

                domExist.DocumentElement.InnerXml = strTempMergedXml;
            }

            strMergedXml = domExist.OuterXml;

            if (string.IsNullOrEmpty(strWarning) == false)
            {
                strError = strWarning;
                return 1;
            }

            return 0;
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
        //      2   �в����޸�����û�ж���
        public int MergeOrderNode(XmlNode exist_node,
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
                IssueItemDatabase.ParseOldNewValue(strExistCopy,
            out strExistOldValue,
            out strExistNewValue);

                string strChangedOldValue = "";
                string strChangedNewValue = "";
                IssueItemDatabase.ParseOldNewValue(strChangedCopy,
            out strChangedOldValue,
            out strChangedNewValue);

                if (strExistOldValue != strChangedOldValue)
                {
                    strError = "��������(��������ߵĲ���)�������޸ġ�(ԭ��='" + strExistCopy + "',�µ�='" + strChangedCopy + "')";
                    return 1;
                }

                // ������������ĸı䣬�Ƿ����ú�distribute�ַ����ڵĸı��Ǻ�
            }

            // �Ƚ������۸��ַ���
            {
                string strExistOldValue = "";
                string strExistNewValue = "";
                IssueItemDatabase.ParseOldNewValue(strExistPrice,
            out strExistOldValue,
            out strExistNewValue);

                string strChangedOldValue = "";
                string strChangedNewValue = "";
                IssueItemDatabase.ParseOldNewValue(strChangedPrice,
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

            bool bDistributeChanged = false;
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
                    bDistributeChanged = true;
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

                    bDistributeChanged = true;
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

            List<string> skips = new List<string>();
            List<string> differents = null;
            skips.Add("distribute");
            skips.Add("operations");
            // parameters:
            //      skips   Ҫ�����ġ�������Ƚϵ�Ԫ����
            // return:
            //      0   û�в���
            //      1   �в��졣differents�������淵�����в����Ԫ����
            nRet = IsItemInfoChanged(new_node,
                dom.DocumentElement,
                skips,
                out differents);
            if (nRet == 1)
            {
                strError = "������Ԫ�ص��޸�û�ж���: " + StringUtil.MakePathList(differents);
                return 2;
            }
            if (nRet == 0 && bDistributeChanged == false)
            {
                // û���κ��޸ķ���
            }

            return 0;
        }

        // parameters:
        //      skips   Ҫ�����ġ�������Ƚϵ�Ԫ����
        // return:
        //      0   û�в���
        //      1   �в��졣differents�������淵�����в����Ԫ����
        public int IsItemInfoChanged(XmlNode new_root,
            XmlNode oldrec_root,
            List<string> skips,
            out List<string> differents)
        {
            differents = new List<string>();

            for (int i = 0; i < core_order_element_names.Length; i++)
            {
                string strElementName = core_order_element_names[i];
                if (skips.IndexOf(strElementName) != -1)
                    continue;

                if (DomUtil.IsEmptyElement(new_root, strElementName) == true
                    && DomUtil.IsEmptyElement(oldrec_root, strElementName) == true)
                    continue;

                string strText1 = DomUtil.GetElementOuterXml(new_root,
                    strElementName);
                string strText2 = DomUtil.GetElementOuterXml(oldrec_root,
                    strElementName);

                if (strText1 != strText2)
                {
                    differents.Add(strText2 + "-->" + strText1);
                }
            }

            if (differents.Count > 0)
                return 1;
            return 0;
        }

        // (�������������)
        // �Ƚ�������¼, ����������Ҫ����Ϣ�йص��ֶ��Ƿ����˱仯
        // return:
        //      0   û�б仯
        //      1   �б仯
        public override int IsItemInfoChanged(XmlDocument domExist,
            XmlDocument domOldRec)
        {
            for (int i = 0; i < core_order_element_names.Length; i++)
            {
                /*
                string strText1 = DomUtil.GetElementText(domExist.DocumentElement,
                    core_order_element_names[i]);
                string strText2 = DomUtil.GetElementText(domOldRec.DocumentElement,
                    core_order_element_names[i]);
                 * */
                // 2009/10/24 changed
                string strText1 = DomUtil.GetElementOuterXml(domExist.DocumentElement,
                    core_order_element_names[i]);
                string strText2 = DomUtil.GetElementOuterXml(domOldRec.DocumentElement,
                    core_order_element_names[i]);

                if (strText1 != strText2)
                    return 1;
            }

            return 0;
        }

        public OrderItemDatabase(LibraryApplication app)
            : base(app)
        {

        }

#if NO
        public int BuildLocateParam(string strBiblioRecPath,
            string strIndex,
            out List<string> locateParam,
            out string strError)
        {
            strError = "";
            locateParam = null;

            int nRet = 0;

            string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
            string strOrderDbName = "";

            // ������Ŀ����, �ҵ���Ӧ���������
            // return:
            //      -1  ����
            //      0   û���ҵ�(��Ŀ��)
            //      1   �ҵ�
            nRet = this.GetItemDbName(strBiblioDbName,
                out strOrderDbName,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                strError = "��Ŀ�� '" + strBiblioDbName + "' û���ҵ�";
                goto ERROR1;
            }
            if (String.IsNullOrEmpty(strOrderDbName) == true)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' ��Ӧ��" + this.ItemName + "����û�ж���";
                goto ERROR1;
            }

            string strParentID = ResPath.GetRecordId(strBiblioRecPath);


            locateParam = new List<string>();
            locateParam.Add(strOrderDbName);
            locateParam.Add(strParentID);
            locateParam.Add(strIndex);

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
            string strOrderDbName = "";

            // ������Ŀ����, �ҵ���Ӧ���������
            // return:
            //      -1  ����
            //      0   û���ҵ�(��Ŀ��)
            //      1   �ҵ�
            nRet = this.GetItemDbName(strBiblioDbName,
                out strOrderDbName,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                strError = "��Ŀ�� '" + strBiblioDbName + "' û���ҵ�";
                goto ERROR1;
            }
            if (String.IsNullOrEmpty(strOrderDbName) == true)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' ��Ӧ��" + this.ItemName + "����û�ж���";
                goto ERROR1;
            }
            */

            locateParam = new List<string>();
            locateParam.Add(strRefID);

            return 0;
            /*
        ERROR1:
            return -1;
             * */
        }

#if NO
        // �������ڻ�ȡ�����¼��XML����ʽ
        public override int MakeGetItemRecXmlSearchQuery(
            List<string> locateParams,
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

            string strOrderDbName = locateParams[0];
            string strParentID = locateParams[1];
            string strIndex = locateParams[2];

            strQueryXml = "<target list='"
        + StringUtil.GetXmlStringSimple(strOrderDbName + ":" + "���")
        + "'><item><word>"
        + StringUtil.GetXmlStringSimple(strIndex)
        + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            strQueryXml += "<operator value='AND'/>";


            strQueryXml += "<target list='"
                    + StringUtil.GetXmlStringSimple(strOrderDbName + ":" + "����¼")
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
                string strDbName = this.App.ItemDbs[i].OrderDbName;

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
            string strOrderDbName = locateParams[0];
            string strParentID = locateParams[1];
            string strIndex = locateParams[2];

            strText = "������Ϊ '" + strOrderDbName + "'������¼IDΪ '" + strParentID + "' ���Ϊ '" + strIndex + "'";
            return 0;
        }
#endif

#if NO1
        // ���춨λ��ʾ��Ϣ�����ڱ���
        public override int GetLocateText(
            List<string> locateParams,
            out string strText,
            out string strError)
        {
            strText = "";
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 1)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ1��";
                return -1;
            }
            string strRefID = locateParams[0];

            strText = "�ο�IDΪ '" + strRefID + "'";
            return 0;
        }
#endif

#if NO
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
            string strOrderDbName = locateParams[0];
            string strParentID = locateParams[1];
            string strIndex = locateParams[2];

            if (String.IsNullOrEmpty(strIndex) == false)
            {
                string strExistingIndex = DomUtil.GetElementText(domExist.DocumentElement,
                    "index");
                if (strExistingIndex != strIndex)
                {
                    strError = "������¼��<index>Ԫ���еı�� '" + strExistingIndex + "' ��ͨ��ɾ����������ָ���ı�� '" + strIndex + "' ��һ�¡�";
                    return 1;
                }
            }

            return 0;
        }
#endif

#if NO1
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
            if (locateParams.Count != 1)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ1��";
                return -1;
            }
            string strRefID = locateParams[0];

            if (String.IsNullOrEmpty(strRefID) == false)
            {
                string strExistingRefID = DomUtil.GetElementText(domExist.DocumentElement,
                    "refID");
                if (strExistingRefID != strRefID)
                {
                    strError = "������¼��<refID>Ԫ���еĲο�ID '" + strExistingRefID + "' ��ͨ��ɾ����������ָ���Ĳο�ID '" + strRefID + "' ��һ�¡�";
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

        // 2012/9/29
        // �Ƿ�����Ծɼ�¼�����޸�(�����ƶ�)? 
        // parameters:
        // return:
        //      -1  �����������޸ġ�
        //      0   �������޸ģ���ΪȨ�޲�����ԭ��ԭ����strError��
        //      1   �����޸�
        public override int CanChange(
            SessionInfo sessioninfo,
            string strAction,
            XmlDocument domExist,
            XmlDocument domNew,
            out string strError)
        {
            strError = "";

            if (sessioninfo == null)
            {
                strError = "sessioninfo == null";
                return -1;
            }

            if (strAction == "move")
            {
                if (sessioninfo.UserType == "reader")
                {
                    strError = "������ݵ��û������ƶ�������¼";
                    return 0;
                }

                if (sessioninfo.GlobalUser == false)
                {
                    // �ٿ��Ѿ����ڵ������ǲ���ȫ���ڹ�Ͻ֮��
                    string strDistribute = DomUtil.GetElementText(domExist.DocumentElement, "distribute");
                    // �۲�һ���ݲط����ַ����������Ƿ��ڵ�ǰ�û���Ͻ��Χ��
                    // return:
                    //      -1  ����
                    //      0   ������Ͻ��Χ��strError���н���
                    //      1   �ڹ�Ͻ��Χ��
                    int nRet = DistributeInControlled(strDistribute,
                sessioninfo.LibraryCodeList,
                out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                    {
                        strError = "��ԭ��¼�г����˳�Խ��ǰ�û���Ͻ��Χ�ķֹݹݲ���Ϣ���ƶ�������¼�Ĳ������ܾ���" + strError;
                        return 0;
                    }
                }

            }

            return 1;
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
                // �ٿ��Ѿ����ڵ������ǲ���ȫ���ڹ�Ͻ֮��
                string strDistribute = DomUtil.GetElementText(domExist.DocumentElement, "distribute");
                // �۲�һ���ݲط����ַ����������Ƿ��ڵ�ǰ�û���Ͻ��Χ��
                // return:
                //      -1  ����
                //      0   ������Ͻ��Χ��strError���н���
                //      1   �ڹ�Ͻ��Χ��
                int nRet = DistributeInControlled(strDistribute,
            sessioninfo.LibraryCodeList,
            out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    strError = "������˳�Խ��ǰ�û���Ͻ��Χ�ķֹݹݲ���Ϣ��ɾ��������¼�Ĳ������ܾ���" + strError;
                    return 0;
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
            string strOrderDbName = locateParams[0];
            string strParentID = locateParams[1];
            string strIndex = locateParams[2];

            if (String.IsNullOrEmpty(strIndex) == true)
            {
                strError = "<index>Ԫ���еı��Ϊ��";
                return 1;
            }

            return 0;
        }
#endif

#if NO1
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
            if (locateParams.Count != 1)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ1��";
                return -1;
            }
            string strRefID = locateParams[0];


            if (String.IsNullOrEmpty(strRefID) == true)
            {
                strError = "�ο�ID Ϊ��";
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
                return "����";
            }
        }

        // �������ơ�
        public override string ItemNameInternal
        {
            get
            {
                return "Order";
            }
        }

        public override string DefaultResultsetName
        {
            get
            {
                return "orders";
            }
        }

        // ׼��д����־��SetXXX�����ַ��������硰SetEntity�� ��SetIssue��
        public override string OperLogSetName
        {
            get
            {
                return "setOrder";
            }
        }

        public override string SetApiName
        {
            get
            {
                return "SetOrders";
            }
        }

        public override string GetApiName
        {
            get
            {
                return "GetOrders";
            }
        }

        // �Ƿ��������¼�¼?
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
                // �ٿ��������ǲ���ȫ���ڹ�Ͻ֮��
                string strDistribute = DomUtil.GetElementText(domNew.DocumentElement, "distribute");
                // �۲�һ���ݲط����ַ����������Ƿ��ڵ�ǰ�û���Ͻ��Χ��
                // return:
                //      -1  ����
                //      0   ������Ͻ��Χ��strError���н���
                //      1   �ڹ�Ͻ��Χ��
                int nRet = DistributeInControlled(strDistribute,
            sessioninfo.LibraryCodeList,
            out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    strError = "������˳�Խ��ǰ�û���Ͻ��Χ�ķֹݹݲ���Ϣ������������¼�Ĳ������ܾ���" + strError;
                    return 0;
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
            return this.App.GetOrderDbName(strBiblioDbName,
                out strItemDbName,
                out strError);
        }

        // 2012/4/27
        public override bool IsItemDbName(string strItemDbName)
        {
            return this.App.IsOrderDbName(strItemDbName);
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
                return host.VerifyOrder(strAction,
                    itemdom,
                    out strError);
            }
            catch (Exception ex)
            {
                strError = "ִ�нű����� '" + "VerifyOrder" + "' ʱ����" + ex.Message;
                return -1;
            }

            return 0;
        }

#if NO1
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

#if NO
            string strOldIndex = DomUtil.GetElementText(domOldRec.DocumentElement,
                "index");

            string strNewIndex = DomUtil.GetElementText(domNewRec.DocumentElement,
                "index");


            string strOldParentID = DomUtil.GetElementText(domOldRec.DocumentElement,
                "parent");

            string strNewParentID = DomUtil.GetElementText(domNewRec.DocumentElement,
                "parent");

            oldLocateParam = new List<string>();
            oldLocateParam.Add(strItemDbName);
            oldLocateParam.Add(strOldParentID);
            oldLocateParam.Add(strOldIndex);

            newLocateParam = new List<string>();
            newLocateParam.Add(strItemDbName);
            newLocateParam.Add(strNewParentID);
            newLocateParam.Add(strNewIndex);

            if (strOldIndex != strNewIndex)
                return 1;   // �����

            return 0;   // ��ȡ�
#endif
            // 2012/4/1 ����
            string strOldRefID = DomUtil.GetElementText(domOldRec.DocumentElement,
                "refID");

            string strNewRefID = DomUtil.GetElementText(domNewRec.DocumentElement,
                "refID");

            oldLocateParam = new List<string>();
            oldLocateParam.Add(strOldRefID);

            newLocateParam = new List<string>();
            newLocateParam.Add(strNewRefID);

            if (strOldRefID != strNewRefID)
                return 1;   // �����

            return 0;   // ��ȡ�
        }
#endif

#if NO
        public override void LockItem(List<string> locateParam)
        {
            string strItemDbName = locateParam[0];
            string strParentID = locateParam[1];
            string strIndex = locateParam[2];

            this.App.EntityLocks.LockForWrite(
                "order:" + strItemDbName + "|" + strParentID + "|" + strIndex);
        }

        public override void UnlockItem(List<string> locateParam)
        {
            string strItemDbName = locateParam[0];
            string strParentID = locateParam[1];
            string strIndex = locateParam[2];

            this.App.EntityLocks.UnlockForWrite(
                "order:" + strItemDbName + "|" + strParentID + "|" + strIndex);
        }
#endif

#if NO1
        public override void LockItem(List<string> locateParam)
        {
            string strRefID = locateParam[0];

            this.App.EntityLocks.LockForWrite(
                "order:" + strRefID);
        }

        public override void UnlockItem(List<string> locateParam)
        {
            string strRefID = locateParam[0];

            this.App.EntityLocks.UnlockForWrite(
                "order:" + strRefID);
        }
#endif
    }
}
