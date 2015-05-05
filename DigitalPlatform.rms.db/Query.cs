using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using DigitalPlatform.rms;
using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.ResultSet;
using DigitalPlatform.Text;

namespace DigitalPlatform.rms
{

    public class SearchItem
    {
        public string TargetTables;  // ����Ŀ���,�Զ��ŷָ�
        public string Word = "";     // ������
        public string Match = "";    // ƥ�䷽ʽ
        public string Relation = "";  // ��ϵ��
        public string DataType = "";  // ��������

        public string IdOrder = "";
        public string KeyOrder = "";

        public string OrderBy = "";       // ������
        public int MaxCount = -1;      // ���Ƶ�������� -1:����
    }

    // ר�ŵ��߼�������,�����ÿ��Ǽ��������⣬
    // ��Ϊ��DatabaseCollection���DoSearch()����������ջ����
    public class Query
    {
        DatabaseCollection m_dbColl;  // 1.���ݿ⼯��ָ��
        User m_oUser;                 // 2.�ʻ�ָ��
        public XmlDocument m_dom;     // 3.����ʽdom

        // ������ĵȼ�
        //   0:�����������ݾ��棬ֱ�ӷ���-1
        //   1:���ݣ�������ì��ʱ��ϵͳ�Զ������޸ĺ󣬽��м���
        int m_nWarningLevel = 1;


        //��̬��Աm_precedenceTable,string���飬��Ų��������Ӧ�����ȼ�
        public static string[] m_precedenceTable = {"NOT","2",
													   "OR","1",
													   "AND","1",
													   "SUB","1",
													   "!","2",
													   "+","1",
													   "-","1"};
        // ���캯��
        // paramter:
        //		dbColl  ���ݿ⼯��ָ��
        //		user    �ʻ�����ָ��
        //		dom     ����ʽDOM
        public Query(DatabaseCollection dbColl,
            User user,
            XmlDocument dom)
        {
            m_dbColl = dbColl;
            m_oUser = user;
            m_dom = dom;

            // ��dom���ҵ�warnging�Ĵ�������Ϣ
            string strWarningLevel = "";
            XmlNode nodeWarningLevel = dom.SelectSingleNode("//option");
            if (nodeWarningLevel != null)
                strWarningLevel = DomUtil.GetAttr(nodeWarningLevel, "warning");

            if (StringUtil.RegexCompare(@"\d", strWarningLevel) == true)
                m_nWarningLevel = Convert.ToInt32(strWarningLevel);
        }


        // �õ�һ����������Ӧ�����ȼ�
        // parameter:
        //		strOperator ������
        // return: 
        //		!= -1   �ò����������ȼ���
        //		== -1   û�ҵ�,ע����ʹ�õĵط���-1����û�ҵ������ܲ��ڱȽ�
        public static int GetPrecedence(string strOperator)
        {
            //ÿ�ι�������
            for (int i = 0; i < m_precedenceTable.Length; i += 2)
            {
                if (String.Compare(m_precedenceTable[i], strOperator, true) == 0)
                {
                    //��Ϊ�κ�ʱ��������һ�飬�������ѭ���ǰ�ȫ�ģ�
                    //���������ǣ���ʹm_precedenceTableû������ȷ���˺���Ҳ��������Ҫ�������ж�
                    if (i + 1 < m_precedenceTable.Length)
                        return Convert.ToInt32(m_precedenceTable[i + 1]);
                    else
                        return -1;
                }
            }
            return -1;
        }


        /*
         The algorithm in detail
        Read character. 
        1.If the character is a number then add it to the output. 
        ����ַ������֣�ֱ�Ӽӵ�output����

        2.If the character is an operator then do something based on the various situations: 
        ����ַ�����������������е��������:

            If the operator has a higher precedence than the operator at the top of the stack or the stack is empty, 
            push the operator onto the stack. 
            ���������ĵ����ȼ�����ջ��Ԫ�أ�����ջΪ�գ��򽫸������push��ջ��
	
            If the operator's precedence is less than or equal to the precedence of the operator at the top of the stack stack 
            then pop operators off the stack, 
            onto the output until the operator at the top of the stack has less precedence than the current operator or there is no more stack to pop.
            At this point, push the operator onto the stack. 
            �����������ȼ����ڻ��ߵ���ջ���������
            ���ջ��pop��������ӵ�output�����
            ֱ��ջ��Ԫ�ص����ȼ�С�ڵ�ǰ�����������ջ��Ϊ��ʱ��
            push��ǰ������ջ��
	
        3.If the character is a left-parenthesis then push it onto the stack. 
        ����������һ�������ţ�ֱ��push��ջ��

        4.If the character is a right-parenthesis 
        then pop an operators off the stack,
        onto the output until the operator read is a left-parenthesis 
        at which point it is popped off the stack but not added to the output. 
        ����������һ�������ţ����ջ��pop��������ӵ�output�����
        ֱ������һ�������ţ���������pop��ջ�������ӵ�������

        If the stack runs out without finding a left-parenthesis 
        then there are mismatched parenthesis. 
        �����ջ��û���ҵ������ţ������Ų�ƥ�䣬����������

        5.If there are no more characters after the current,
        pop all the operators off the stack and exit. 
        If a left-parenthesis is popped then there are mismatched parenthesis. 
        ��󣬴�ջ��pop���еĲ��������ӵ�output�����
        �������һ�������ţ���������Ų�ƥ����󣬼��������š�

        After doing one or more of the above steps, go back to the start. 
         */

        // ���������㷨�������븸�׽ڵ���ӵ�����˳��ת�����沨����˳����
        // �õ�һ��ArrayList
        // ע����˵���չ�Ľڵ�
        // parameter:
        //		node    ���׽ڵ�
        //		output  out�����������沨����˳����ArrayList
        //				ע���п����ǿ�ArrayList��
        //		strErrorInfo    out���������ش�����Ϣ
        // return:
        //		-1  ���� ����:���Ų�ƥ��;�Ҳ���ĳ�����������ȼ�
        //		0   �ɹ�
        public int Infix2RPN(XmlNode node,
            out ArrayList output,
            out string strError)
        {
            strError = "";
            output = new ArrayList();
            if (node == null)
            {
                strError = "node����Ϊnull\r\n";
                return -1;
            }
            if (node.ChildNodes.Count == 0)
            {
                strError = "node��ChildNodes����Ϊ0\r\n";
                return 0;
            }

            //������һ��ջ����ֱ����.net��Stack��
            Stack stackOperator = new Stack();

            foreach (XmlNode nodeChild in node.ChildNodes)
            {
                //����������;����չ�ڵ㣬��Ҫ����
                if (nodeChild.Name == "lang" || nodeChild.Name == "option")
                    continue;

                // 2010/9/26 add
                if (nodeChild.NodeType == XmlNodeType.Whitespace)
                    continue;

                //������ʱ���ӵ�output.
                if (nodeChild.Name != "operator")
                {
                    output.Add(nodeChild);
                }
                else //������ʱ
                {
                    string strOperator = DomUtil.GetAttr(nodeChild, "value");

                    //����������ڵ㣬��valueΪ��ʱ����OR��
                    if (strOperator == "")
                        strOperator = "OR";

                    //������ֱ��pushջ��
                    if (strOperator == "(")
                    {
                        stackOperator.Push(nodeChild);
                        continue;
                    }

                    //������ʱ
                    if (strOperator == ")")
                    {
                        bool bFound = false;
                        while (stackOperator.Count != 0)
                        {
                            //���ȴ�ջ��pop��һ��������
                            XmlNode nodeTemp = (XmlNode)stackOperator.Pop();

                            string strTemp = DomUtil.GetAttr(node, "value");

                            //��������������ţ��ӵ�output��
                            if (strTemp != "(")
                            {
                                output.Add(nodeTemp);
                            }
                            else  //����������ѭ��
                            {
                                bFound = true;
                                break;
                            }
                        }

                        //û�ҵ�������ʱ������-1
                        if (bFound == false)
                        {
                            strError = "������ȱ����Ե�������";
                            return -1;
                        }
                    }


                    //���ջΪ�գ�ֱ��push
                    if (stackOperator.Count == 0)
                    {
                        stackOperator.Push(nodeChild);
                        continue;
                    }

                    //�õ���ǰ�����������ȼ�
                    int nPrecedence;
                    nPrecedence = Query.GetPrecedence(strOperator);

                    //-1������û�ҵ�
                    if (nPrecedence == -1)
                    {
                        strError = "û���ҵ�������" + strOperator + "�����ȼ�<br/>";
                        return -1;
                    }

                    //��ջ��peekһ��Ԫ�أ�����pop��������ܻ���ӻ�ȥ��
                    XmlNode nodeFromStack = (XmlNode)stackOperator.Peek();
                    string strOperatorFormStack = "";
                    if (nodeFromStack != null)
                        strOperatorFormStack = DomUtil.GetAttr(nodeFromStack, "value");

                    //�õ�ջ��Ԫ�ص����ȼ�
                    int nPrecedenceFormStack;
                    nPrecedenceFormStack = Query.GetPrecedence(strOperatorFormStack);

                    if (nPrecedenceFormStack == -1)
                    {
                        strError = "û���ҵ��˲�����" + strOperatorFormStack + "�����ȼ�<br/>";
                        return -1;
                    }

                    //��ǰ�����������ȼ�����ջ������������ȼ���
                    //�򽫵�ǰ������push��ջ��
                    if (nPrecedence > nPrecedenceFormStack)
                    {
                        stackOperator.Push(nodeChild);
                    }

                    //��ǰ����ջ��ʱ
                    if (nPrecedence <= nPrecedenceFormStack)
                    {
                        //���д�������
                        while (true)
                        {
                            //ջ��ʱ������ǰ������push��ջ�����ѭ��
                            if (stackOperator.Count == 0)
                            {
                                stackOperator.Push(nodeChild);
                                break;
                            }

                            //�õ�ջ��������(ʹ��peek)�������ȼ�
                            XmlNode nodeIn = (XmlNode)stackOperator.Peek();
                            string strOperatorIn = "";
                            if (nodeIn != null)
                                strOperatorIn = DomUtil.GetAttr(nodeIn, "value");

                            int nPrecedenceIn;
                            nPrecedenceIn = Query.GetPrecedence(strOperatorIn);

                            if (nPrecedenceIn == -1)
                            {
                                strError = "û���ҵ��˲�����" + strOperatorIn + "�����ȼ�<br/>";
                                return -1;
                            }

                            //��ǰ����ջ���ģ��򽫵�ǰpush��ջ�����ѭ��
                            if (nPrecedence > nPrecedenceIn)
                            {
                                stackOperator.Push(nodeChild);
                                break;
                            }

                            //������ջ��pop���ӵ�output��
                            nodeIn = (XmlNode)stackOperator.Pop();
                            output.Add(nodeIn);
                        }
                    }
                }
            }


            //��󣬽�ջ��ʣ�µĲ���������ջ��pop���ӵ�output��
            while (stackOperator.Count != 0)
            {
                XmlNode nodeTemp = (XmlNode)stackOperator.Pop();

                //������Ϊ�����ţ����Ų�ƥ�䣬��������-1
                string strOperator = "";
                if (nodeTemp != null)
                    strOperator = DomUtil.GetAttr(nodeTemp, "value");
                if (strOperator == ")")
                {
                    strError = "�����Ŷ�";
                    return -1;
                }

                output.Add(nodeTemp);
            }

            return 0;
        }

        // ����: ��������Ԫmatch,relation,dataType����Ĺ�ϵ
        // �������ì��:
        // ���������ļ���Ϊ0,���������Ԫnode��warning��Ϣ�����Զ�����������-1,
        // �������Ϊ��0��Ӧ�ù̶�Ϊ1������ϵͳ�Զ����ӣ����ڸ����ĵط���comment��Ϣ������0
        // ������ì��:����0
        // parameter:
        //		nodeItem    ������Ԫ�ڵ�
        // return:
        //		0   �����������ì�ܣ������ڴ����漶��Ϊ��0���Զ�������Ҳ����0��
        //		-1  ����ì�ܣ��Ҿ��漶��Ϊ0
        public int ProcessRelation(XmlNode nodeItem)
        {
            //ƥ�䷽ʽ
            XmlNode nodeMatch = nodeItem.SelectSingleNode("match");
            string strMatch = "";
            if (nodeMatch != null)
                strMatch = nodeMatch.InnerText.Trim(); // 2012/2/16
            else
            {
                // 2008/11/23 new add
                nodeMatch = nodeItem.OwnerDocument.CreateElement("match");
                nodeItem.AppendChild(nodeMatch);
            }


            if (String.IsNullOrEmpty(strMatch) == true)
            {
                strMatch = "left";  // 2008/11/23 new add
                // DomUtil.SetNodeText(nodeMatch, strMatch);   // ԭ����"left"
                nodeMatch.InnerText = strMatch;   // ԭ����"left"   // 2012/2/16
                DomUtil.SetAttr(nodeMatch, "comment",
                    "ԭΪ" + strMatch + ",�޸�Ϊȱʡ��left");
            }

            //��ϵ������
            XmlNode nodeRelation = nodeItem.SelectSingleNode("relation");
            string strRelation = "";
            if (nodeRelation != null)
                strRelation = nodeRelation.InnerText.Trim(); // 2012/2/16

            strRelation = QueryUtil.ConvertLetterToOperator(strRelation);

            //��������
            XmlNode nodeDataType = nodeItem.SelectSingleNode("dataType");
            string strDataType = "";
            if (nodeDataType != null)
                strDataType = nodeDataType.InnerText.Trim(); // 2012/2/16

            if (strDataType == "number")
            {
                if (strMatch == "left" || strMatch == "right")
                {
                    //��dataTypeֵΪnumberʱ��matchֵΪleft��right��
                    //�޸Ŀ����Զ�������:
                    //1.��left����exact;
                    //2.��dataType��Ϊstring,
                    //�����Ȱ�dataType���ȣ���match��Ϊexact

                    if (m_nWarningLevel == 0)
                    {
                        DomUtil.SetAttr(nodeItem,
                            "warningInfo",
                            "ƥ�䷽ʽΪֵ��" + strMatch + "'���������͵�ֵ'" + strDataType + "'ì��, �������漶��Ϊ0���������Զ�����");

                        return -1;
                    }
                    else
                    {
                        nodeMatch.InnerText = "exact";   // 2012/2/16
                        DomUtil.SetAttr(nodeMatch,
                            "comment",
                            "ԭΪ" + strMatch + ",��������������'" + strDataType + "'ì�ܣ��޸�Ϊexact");
                    }
                }
            }

            if (strDataType == "string")
            {
                //���dataTypeֵΪstring,
                //matchֵΪleft��right,��relationֵ������"="

                //����ì�ܣ�(������Ϊmatch��left��rgithֵ��ֻ��relation��"="ֵƥ��)
                //�����ֲþ��취��
                //1.��relationֵ��Ϊ"="��;
                //2.��matchֵ��left��right��Ϊexact
                //Ŀǰ��1�����޸�

                if ((strMatch == "left" || strMatch == "right") && strRelation != "=")
                {
                    //���ݴ����漶������ͬ�Ĵ���
                    if (m_nWarningLevel == 0)
                    {
                        DomUtil.SetAttr(nodeItem,
                            "warningInfo",
                            "��ϵ������'" + strRelation + "'����������" + strDataType + "��ƥ�䷽ʽ'" + strMatch + "'��ƥ��");
                        return -1;
                    }
                    else
                    {
                        nodeRelation.InnerText = "=";   // 2012/2/16
                        DomUtil.SetAttr(nodeRelation,
                            "comment",
                            "ԭΪ" + strRelation + ",��������������'" + strDataType + "��ƥ�䷽ʽ'" + strMatch + "'��ƥ�䣬�޸�Ϊ'='");
                    }
                }
            }
            return 0;
        }

        // ������Ԫitem����Ϣ���Կ���м���
        // parameter:
        //		nodeItem	item�ڵ�
        //		resultSet	�����������ʱ����ȷ��������Ѿ�������Ҫ��resultset.Sorted��Ա
        //		            ���������,????????ÿ����գ���Ȼÿ����գ��ǻ����緵��һ���������
        //		isConnected	�Ƿ�����
        //		strError	out���������س�����Ϣ
        // return:
        //		-1	����
        //		-6	���㹻��Ȩ��
        //		0	�ɹ�
        public int doItem(
            string strOutputStyle,
            XmlNode nodeItem,
            DpResultSet resultSet,
            ChannelHandle handle,
            // Delegate_isConnected isConnected,
            out string strError)
        {
            strError = "";
            if (nodeItem == null)
            {
                strError = "doItem() nodeItem����Ϊnull.";
                return -1;
            }

            if (resultSet == null)
            {
                strError = "doItem() oResult����Ϊnull.";
                return -1;
            }

            //�����һ��
            resultSet.Clear();

            int nRet;

            //��processRelation�Լ�����Ԫ�ĳ�Ա����Ƿ����ì��
            //�������0������ܶ�item�ĳ�Ա�������޸ģ����Ժ���������ȡ����
            nRet = ProcessRelation(nodeItem);
            if (nRet == -1)
            {
                // strError = "doItem()���processRelation����";
                strError = "����ʽ�ֲ��д�: " + nodeItem.OuterXml;
                return -1;
            }


            // ����nodeItem�õ�������Ϣ
            string strTarget;
            string strWord;
            string strMatch;
            string strRelation;
            string strDataType;
            string strIdOrder;
            string strKeyOrder;
            string strOrderBy;
            int nMaxCount;
            nRet = QueryUtil.GetSearchInfo(nodeItem,
                strOutputStyle,
                out strTarget,
                out strWord,
                out strMatch,
                out strRelation,
                out strDataType,
                out strIdOrder,
                out strKeyOrder,
                out strOrderBy,
                out nMaxCount,
                out strError);
            if (nRet == -1)
                return -1;

            bool bSearched = false;
            bool bNeedSort = false;

            //��target��;�ŷֳɶ����
            string[] aDatabase = strTarget.Split(new Char[] { ';' });
            foreach (string strOneDatabase in aDatabase)
            {
                if (strOneDatabase == "")
                    continue;

                string strDbName;
                string strTableList;

                // ��ֿ�����;��
                nRet = DatabaseUtil.SplitToDbNameAndForm(strOneDatabase,
                    out strDbName,
                    out strTableList,
                    out strError);
                if (nRet == -1)
                    return -1;

                // �õ���
                Database db = m_dbColl.GetDatabase(strDbName);
                if (db == null)
                {
                    strError = "δ�ҵ�'" + strDbName + "'��";
                    return -1;
                }

                // 2009/7/19 new add
                if (db.InRebuildingKey == true)
                {
                    strError = "���ݿ� '" + db.GetCaption(null) + "' �������ؽ�������״̬�����ܽ��м���...";
                    return -1;
                }

                string strTempRecordPath = db.GetCaption("zh-CN") + "/" + "record";
                string strExistRights = "";
                bool bHasRight = this.m_oUser.HasRights(strTempRecordPath,
                    ResType.Record,
                    "read",
                    out strExistRights);
                if (bHasRight == false)
                {
                    strError = "�����ʻ���Ϊ'" + m_oUser.Name + "'" +
                        ",��'" + strDbName + "'" +
                        "���ݿ��еļ�¼û��'��(read)'Ȩ�ޣ�Ŀǰ��Ȩ��Ϊ'" + strExistRights + "'��";
                    return -6;
                }

                SearchItem searchItem = new SearchItem();
                searchItem.TargetTables = strTableList;
                searchItem.Word = strWord;
                searchItem.Match = strMatch;
                searchItem.Relation = strRelation;
                searchItem.DataType = strDataType;
                searchItem.IdOrder = strIdOrder;
                searchItem.KeyOrder = strKeyOrder;
                searchItem.OrderBy = strOrderBy;
                searchItem.MaxCount = nMaxCount;

                // ע: SearchByUnion�����resultSet���Ӷ�ʹ�����Ľ��������һ��
                string strWarningInfo = "";
                //		-1	����
                //		0	�ɹ�
                //      1   �ɹ�����resultset��Ҫ��������һ��
                nRet = db.SearchByUnion(
                    strOutputStyle,
                    searchItem,
                    handle,
                    // isConnected,
                    resultSet,
                    this.m_nWarningLevel,
                    out strError,
                    out strWarningInfo);
                if (nRet == -1)
                    return -1;

                bSearched = true;

                if (nRet == 1)
                    bNeedSort = true;
            }

            // 2010/5/17
            if (bSearched == true)
            {
                // 2010/5/11
                resultSet.EnsureCreateIndex();   // ȷ������������

                // ����
                // TODO: ��ʵ����ʹ��EnsureSorted()����
                if (bNeedSort == true)
                {
                    if (DoSort(resultSet, handle/*isConnected*/) == true)
                    {
                        strError = "ǰ���ж�";
                        return -1;
                    }
                }
                // TODO: Ҳ�����ӳ����򣬱���������һ��ֵ��ϣ�����򡣵ȵ���󲻵ò�����ʱ�������������

                //resultSet.RemoveDup();
            }

            return 0;
        }

        // return:
        //      fasle   �������
        //      true    �ж�
        static bool DoSort(DpResultSet resultset,
            ChannelHandle handle
            // Delegate_isConnected isConnected
            )
        {
            resultset.Idle += new IdleEventHandler(sort_Idle);
            resultset.Param = handle;   //  isConnected;
            try
            {
                resultset.Sort();
            }
            catch (InterruptException /*ex*/)
            {
                return true;    // �ж�
            }
            finally
            {
                resultset.Idle -= new IdleEventHandler(sort_Idle);
                resultset.Param = null;
            }

            return false;
        }

        static void sort_Idle(object sender, IdleEventArgs e)
        {
            DpResultSet resultset = (DpResultSet)sender;
#if NO
            Delegate_isConnected isConnected = (Delegate_isConnected)resultset.Param;

            if (isConnected() == false)
            {
                throw new InterruptException("�ж�");
            }
#endif

            ChannelHandle handle = (ChannelHandle)resultset.Param;
            if (handle.DoIdle() == false)
                throw new InterruptException("�ж�");

            // e.bDoEvents = false;
        }


        // �ݹ麯������һ���ڵ�ļ��ϣ�����doSearch���ú���
        // parameter:
        //		nodeRoot	��ǰ���ڵ�
        //		resultSet	�����������ʱ����ȷ��������Ѿ�������Ҫ��resultset.Sorted��Ա
        // return:
        //		-1	����
        //		-6	��Ȩ��
        //		0	�ɹ�
        public int DoQuery(
            SessionInfo sessioninfo,
            string strOutputStyle,
            XmlNode nodeRoot,
            ref DpResultSet resultSet,
            ChannelHandle handle,
            // Delegate_isConnected isConnected,
            out string strError)
        {

            DateTime start_time = DateTime.Now;
            Debug.WriteLine("Begin DoQuery()");

            try
            {

                strError = "";
                if (nodeRoot == null)
                {
                    strError = "DoQuery() nodeRoot��������Ϊnull��";
                    return -1;
                }

#if NO
                if (resultSet == null)
                {
                    strError = "DoQuery() resultSet��������Ϊnull��";
                    return -1;
                }
#endif

                if (resultSet != null)
                {
                    // �����һ��
                    resultSet.Clear();
                }

                //��itemʱ���ټ����ݹ�
                if (nodeRoot.Name == "item")
                {
                    if (resultSet == null)
                        resultSet = sessioninfo.NewResultSet(); // �ӳٴ���
                    // return:
                    //		-1	����
                    //		-6	���㹻��Ȩ��
                    //		0	�ɹ�
                    return doItem(
                        strOutputStyle,
                        nodeRoot,
                        resultSet,
                        handle,
                        // isConnected,
                        out strError);
                }

                //���Ϊ��չ�ڵ㣬�򲻵ݹ�
                if (nodeRoot.Name == "operator"
                    || nodeRoot.Name == "lang")
                {
                    return 0;
                }

                //������˳�����沨������
                ArrayList rpn;
                int nRet = Infix2RPN(nodeRoot,
                    out rpn,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�沨�������:" + strError;
                    return -1;
                }

                //�����������
                if (rpn.Count == 0)
                    return 0;

                // return:
                //		-1  ����
                //		-6	���㹻��Ȩ��
                //		0   �ɹ�
                nRet = ProceedRPN(
                    sessioninfo,
                    strOutputStyle,
                    rpn,
                    ref resultSet,
                    handle,
                    // isConnected,
                    out strError);
                if (nRet <= -1)
                    return nRet;

                return 0;
            }
            finally
            {
                TimeSpan delta = DateTime.Now - start_time;
                Debug.WriteLine("End DoQuery() ��ʱ " + delta.ToString());
            }
        }

        static int EnsureSorted(DpResultSet resultset,
            ChannelHandle handle,
            out string strError)
        {
            strError = "";

            resultset.EnsureCreateIndex();   // ȷ������������

            if (resultset.Sorted == false)
            {
                if (DoSort(resultset, handle/*isConnected*/) == true)
                {
                    strError = "ǰ���ж�";
                    return -1;
                }

                Debug.Assert(resultset.Sorted == true, "");
            }
            return 0;
        }

        // �����沨�����õ������
        // parameter:
        //		rpn         �沨����
        //		resultSet  �����
        // return:
        //		0   �ɹ�
        //		-1  ���� ԭ���������:
        //			1)rpn����Ϊnull
        //			2)oResultSet����Ϊnull
        //			3)ջ���ĳ��Ա����node��result��Ϊnull��
        //			4)��ջ��pop()��peek()Ԫ��ʱ������ջ��
        //			5)pop�����ͣ�����ʵ�ʴ��ڵ�����
        //			6)ͨ��һ���ڵ㣬�õ������������DoQuery()��������
        //			7)������ʱ����DpResultSetManager.Merge()��������
        //			8)���ջ���Ԫ�ض���1�����沨�������
        //			9)�������Ϊ��
        //		-6	���㹻��Ȩ��
        public int ProceedRPN(
            SessionInfo sessioninfo,
            string strOutputStyle,
            ArrayList rpn,
            ref DpResultSet resultSet,
            ChannelHandle handle,
            // Delegate_isConnected isConnected,
            out string strError)
        {
            DateTime start_time = DateTime.Now;
            Debug.WriteLine("Begin ProceedRPN()");

            try
            {
                strError = "";
                //???Ҫ������ò������
                //Ӧ����գ����������ʹ�õĽ�����Ƕ�ջ�����������������������ý����
                //DoQuery����ҲӦ�������
                //doItem����һ��ȥ����գ����ٶ����ݿ�ѭ������ʱ��ǧ�����

                if (resultSet != null)
                    resultSet.Clear();

                if (rpn == null)
                {
                    strError = "rpn����Ϊnull";
                    return -1;
                }
#if NO
                if (resultSet == null)
                {
                    strError = "resultSet����Ϊnull";
                    return -1;
                }
#endif

                if (rpn.Count == 0)
                    return 0;

                int ret;

                // ����ջ,ReversePolishStackջ���Զ������
                // ������һ��ջ�����㣬���������������������ֱ��push��ջ��
                // �����������������˫Ŀ����ջ��pop�����������
                // ע��SUB�����ǣ��ú�һ��pop�Ķ����ǰһ��pop�Ķ���
                //
                // oReversePolandStack�ĳ�ԱΪReversePolishItem,
                // ReversePolishItem��һ�����Ӷ���
                // ����m_int(����),m_node(�ڵ�),m_resultSet.
                // ʵ�������У�m_node��m_resultSetֻ��һ��ֵ��Ч����һ����null
                // m_int�����ж��ĸ�ֵ��Ч��
                // 0��ʾnode��Ч��1��ʾresultSet��Ч
                ReversePolishStack oReversePolandStack =
                    new ReversePolishStack();

                //��ѭ��
                for (int i = 0; i < rpn.Count; i++)
                {
                    XmlNode node = (XmlNode)rpn[i];

                    if (node.Name != "operator")  //������ֱ��push��ջ��
                    {
                        oReversePolandStack.PushNode(node);
                    }
                    else
                    {
                        string strOpreator = DomUtil.GetAttr(node, "value");

#if NO
                        //���������������Ĳ�������Ϊ��ָ�룬���Բ���out
                        DpResultSet oTargetLeft = sessioninfo.NewResultSet();   // new DpResultSet();
                        DpResultSet oTargetMiddle = sessioninfo.NewResultSet();   // new DpResultSet();
                        DpResultSet oTargetRight = sessioninfo.NewResultSet();   // new DpResultSet();
#endif

                        //��һ��������Ա��ArrayList��
                        //��Ա����ΪDpResultSet��
                        //��Ŵ�ջ��pop���ģ������node����Ҫ���м��㣩�Ľ����
                        List<DpResultSet> oSource = new List<DpResultSet>();
                        oSource.Add(sessioninfo.NewResultSet());   // new DpResultSet()
                        oSource.Add(sessioninfo.NewResultSet());   // new DpResultSet()
                        try
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                //����Ϊ-1����ʾnode��resultSet��Ϊnull�����ִ���
                                if (oReversePolandStack.PeekType() == -1)
                                {
                                    strError = strOpreator + "ʱ,PeekType()����-1�����ʾ�����null����������-1<br/>";
                                    return -1;
                                }

                                //��ʾ�ŵ���node
                                if (oReversePolandStack.PeekType() == 0)
                                {
                                    XmlNode nodePop;
                                    nodePop = oReversePolandStack.PopNode();
                                    if (nodePop == null)
                                    {
                                        strError = "nodePop��Ϊ����Ϊnull";
                                        return -1;
                                    }

                                    DpResultSet temp = oSource[j];
                                    // return:
                                    //		-1	����
                                    //		-6	��Ȩ��
                                    //		0	�ɹ�
                                    ret = this.DoQuery(
                                        sessioninfo,
                                        strOutputStyle,
                                        nodePop,
                                        ref temp,
                                        handle,
                                        // isConnected,
                                        out strError);
                                    if (ret <= -1)
                                        return ret;
                                    if (temp != oSource[j])
                                    {
                                        // 2014/3/11
                                        if (oSource[j] != null)
                                            oSource[j].Close();
                                        oSource[j] = temp;
                                    }
                                }
                                else
                                {
                                    DpResultSet temp = oReversePolandStack.PopResultSet();
                                    Debug.Assert(temp != oSource[j], "");

                                    // 2014/3/11
                                    if (oSource[j] != null)
                                        oSource[j].Close();
                                    oSource[j] = temp;

                                    if (oSource[j] == null)
                                    {
                                        strError = "PopResultSet() return null";
                                        return -1;
                                    }
                                }
                            }
                        }
                        catch (StackUnderflowException /*ex*/)
                        {
                            // 2008/12/4 new add
                            string strOutXml = "";
                            if (node.ParentNode != null)
                            {
                                int nRet = DomUtil.GetIndentXml(node.ParentNode.OuterXml,
                                    out strOutXml,
                                    out strError);
                            }
                            strError = strOpreator + " �Ƕ�Ԫ����������ȱ����������";

                            if (String.IsNullOrEmpty(strOutXml) == false)
                                strError += "\r\n" + strOutXml;

                            // strError = "StackUnderflowException :" + ex.Message;
                            return -1;
                        }

                        string strDebugInfo;

                        //OR,AND,SUB���㶼�ǵ���DpResultSetManager.Merge()������
                        //ע�������ʹ��
                        if (strOpreator == "OR")
                        {
                            bool bOutputKeyCount = StringUtil.IsInList("keycount", strOutputStyle);
                            bool bOutputKeyID = StringUtil.IsInList("keyid", strOutputStyle);

                            DpResultSet left = oSource[1];
                            DpResultSet right = oSource[0];

#if DEBUG
                            Debug.Assert(left.IsClosed == false, "");
                            Debug.Assert(right.IsClosed == false, "");
#endif

                            {
                                // ֱ�����
                                if (left.Count == 0)
                                {
                                    oReversePolandStack.PushResultSet(right);
                                    // 2014/3/11
                                    Debug.Assert(left != right, "");
                                    left.Close();
                                }
                                else if (right.Count == 0)
                                {
                                    oReversePolandStack.PushResultSet(left);
                                    // 2014/3/11
                                    Debug.Assert(left != right, "");
                                    right.Close();
                                }
                                else
                                {
                                    if (EnsureSorted(left, handle, out strError) == -1)
                                        return -1;
                                    if (EnsureSorted(right, handle, out strError) == -1)
                                        return -1;
                                    // return:
                                    //      -1  ����
                                    //      0   û�н��沿��
                                    //      1   �н��沿��
                                    ret = DpResultSetManager.IsCross(left,
                                        right,
                                        out strError);
                                    if (ret == -1)
                                        return -1;
                                    if (ret == 0)
                                    {
                                        DpResultSet left_save = left;
                                        // ע�⣺����ִ�й��̣����ܽ��� left �� right��Ҳ����˵���غ� left == right
                                        ret = DpResultSetManager.AddResults(ref left,
                                            right,
                                            out strError);
                                        if (ret == -1)
                                            return -1;

                                        oReversePolandStack.PushResultSet(left);
                                        // 2014/3/11
                                        if (left != right)
                                        {
                                            Debug.Assert(left_save == left, "");
                                            right.Close();
                                        }
                                        else
                                        {
                                            Debug.Assert(left_save != left, "");
                                            left_save.Close();
                                        }
                                    }
                                    else
                                    {
                                        if (left.Asc != right.Asc)
                                        {
                                            right.Asc = left.Asc;
                                            right.Sorted = false;
                                        }

                                        if (EnsureSorted(left, handle, out strError) == -1)
                                            return -1;
                                        if (EnsureSorted(right, handle, out strError) == -1)
                                            return -1;

                                        {
                                            DpResultSet oTargetMiddle = sessioninfo.NewResultSet();   // new DpResultSet();

                                            ret = DpResultSetManager.Merge(LogicOper.OR,
        left,
        right,
        strOutputStyle,
        null,
        oTargetMiddle,
        null,
        false,
        querystop,
        handle,
        out strDebugInfo,
        out strError);
                                            if (ret == -1)
                                                return -1;

                                            oReversePolandStack.PushResultSet(oTargetMiddle);
                                            // 2014/3/11
                                            Debug.Assert(left != oTargetMiddle, "");
                                            Debug.Assert(right != oTargetMiddle, "");
                                            left.Close();
                                            right.Close();
                                        }
                                    }
                                }
                            }

                            continue;
                        }

                        if (strOpreator == "AND")
                        {
                            DpResultSet left = oSource[1];
                            DpResultSet right = oSource[0];
#if DEBUG
                            Debug.Assert(left.IsClosed == false, "");
                            Debug.Assert(right.IsClosed == false, "");
#endif

                            if (left.Asc != right.Asc)
                            {
                                right.Asc = left.Asc;
                                right.Sorted = false;
                            }

                            if (EnsureSorted(left, handle, out strError) == -1)
                                return -1;
                            if (EnsureSorted(right, handle, out strError) == -1)
                                return -1;

                            // �Ż�
                            if (left.Count == 0)
                            {
                                oReversePolandStack.PushResultSet(left);
                                // 2014/3/11
                                Debug.Assert(left != right, "");
                                right.Close();
                            }
                            else if (right.Count == 0)
                            {
                                oReversePolandStack.PushResultSet(right);
                                // 2014/3/11
                                Debug.Assert(left != right, "");
                                left.Close();
                            }
                            else
                            {
                                DpResultSet oTargetMiddle = sessioninfo.NewResultSet();   // new DpResultSet();

                                ret = DpResultSetManager.Merge(LogicOper.AND,
                                    left,
                                    right,
                                    strOutputStyle,
                                    null,    //oTargetLeft
                                    oTargetMiddle,
                                    null,   //oTargetRight
                                    false,
                                    querystop,
                                    handle,
                                    out strDebugInfo,
                                    out strError);
                                if (ret == -1)
                                    return -1;

                                oReversePolandStack.PushResultSet(oTargetMiddle);
                                // 2014/3/11
                                Debug.Assert(left != oTargetMiddle, "");
                                Debug.Assert(right != oTargetMiddle, "");
                                left.Close();
                                right.Close();
                            }

                            continue;
                        }

                        if (strOpreator == "SUB")
                        {
                            //��Ϊʹ�ô�ջ��pop�����Ե�0���Ǻ���ģ���1����ǰ���
                            DpResultSet left = oSource[1];
                            DpResultSet right = oSource[0];
#if DEBUG
                            Debug.Assert(left.IsClosed == false, "");
                            Debug.Assert(right.IsClosed == false, "");
#endif

                            if (left.Asc != right.Asc)
                            {
                                right.Asc = left.Asc;
                                right.Sorted = false;
                            }

                            if (EnsureSorted(left, handle, out strError) == -1)
                                return -1;
                            if (EnsureSorted(right, handle, out strError) == -1)
                                return -1;

                            // �Ż�
                            if (left.Count == 0)
                            {
                                oReversePolandStack.PushResultSet(left);
                                // 2014/3/11
                                Debug.Assert(left != right, "");
                                right.Close();
                            }
                            else if (right.Count == 0)
                            {
                                oReversePolandStack.PushResultSet(left);
                                // 2014/3/11
                                Debug.Assert(left != right, "");
                                right.Close();
                            }
                            else
                            {
                                DpResultSet oTargetLeft = sessioninfo.NewResultSet();   // new DpResultSet();

                                ret = DpResultSetManager.Merge(LogicOper.SUB,
                                    left,
                                    right,
                                    strOutputStyle,
                                    oTargetLeft,
                                    null, //oTargetMiddle
                                    null, //oTargetRight
                                    false,
                                    querystop,
                                    handle,
                                    out strDebugInfo,
                                    out strError);
                                if (ret == -1)
                                {
                                    return -1;
                                }

                                oReversePolandStack.PushResultSet(oTargetLeft);
                                // 2014/3/11
                                Debug.Assert(left != oTargetLeft, "");
                                Debug.Assert(right != oTargetLeft, "");
                                left.Close();
                                right.Close();
                            }

                            continue;
                        }
                    }
                }
                if (oReversePolandStack.Count > 1)
                {
                    strError = "�沨������";
                    return -1;
                }
                try
                {
                    int nTemp = oReversePolandStack.PeekType();
                    //�������Ϊ0,��ʾ��ŵ��ǽڵ�
                    if (nTemp == 0)
                    {
                        XmlNode node = oReversePolandStack.PopNode();

                        // return:
                        //		-1	����
                        //		-6	��Ȩ��
                        //		0	�ɹ�
                        ret = this.DoQuery(
                            sessioninfo,
                            strOutputStyle,
                            node,
                            ref resultSet,
                            handle,
                            // isConnected,
                            out strError);
                        if (ret <= -1)
                            return ret;
                    }
                    else if (nTemp == 1)
                    {
                        // ��DpResultSet��copy����

                        // TODO: �������Copy�����ѵ�ʱ�䡣
                        // resultSet.Copy((DpResultSet)(oReversePolandStack.PopResultSet()));
                        resultSet = (DpResultSet)(oReversePolandStack.PopResultSet());
                    }
                    else
                    {
                        strError = "oReversePolandStack�����Ͳ�����Ϊ" + Convert.ToString(nTemp);
                        return -1;
                    }
                }
                catch (StackUnderflowException)
                {
                    strError = "peek��popʱ���׳�StackUnderflowException�쳣";
                    return -1;
                }

                //�������Ϊnull�����س���
                if (resultSet == null)
                {
                    strError = "���������PopResultSetΪnull" + Convert.ToString(oReversePolandStack.PeekType());
                    return -1;
                }

                return 0;
            }
            finally
            {
                TimeSpan delta = DateTime.Now - start_time;
                Debug.WriteLine("End ProceedRPN() ��ʱ " + delta.ToString());
            }
        }

        bool querystop(object param)
        {
            ChannelHandle handle = (ChannelHandle)param;
            if (handle.DoIdle() == true)
                return false;
            return true;
        }

    }  //  end of class Query


    // �沨�������࣬��ReversePolishStack�ĳ�Ա
    public class ReversePolishItem
    {
        public int m_int;               // ���� 0:node 1:�����
        public XmlNode m_node;          // node�ڵ�
        public DpResultSet m_resultSet; // �����

        // ���캯��
        // parameter:
        //		node        �ڵ�
        //		oResultSet  �����
        public ReversePolishItem(XmlNode node,
            DpResultSet resultSet)
        {
            m_node = node;
            m_resultSet = resultSet;

            if (m_node != null)
            {
                m_int = 0; //0��ʾXmlNode
            }
            else if (m_resultSet != null)
            {
                m_int = 1; //1��ʾresultSet
            }
            else
            {
                m_int = -1;
            }
        }

    } //  end of class ReversePolishItem


    // ��ProceedRPN������õ����沨��ջ��ע�����ջֻ���ֵ����Ա��ReversePolishItem
    public class ReversePolishStack : ArrayList
    {
        // ֻ�������ڵ��ReversePolishItem���󣬲�push��ջ��
        // parameter:
        //		node    node�ڵ�
        // return:
        //      void
        public void PushNode(XmlNode node)
        {
            ReversePolishItem oItem = new ReversePolishItem(node,
                null);
            Add(oItem);
        }

        // ֻ�������������ReversePolishItem���󣬲�push��ջ��
        // parameter:
        //		oResult �����
        // return:
        //      void
        public void PushResultSet(DpResultSet oResult)
        {
            ReversePolishItem oItem = new ReversePolishItem(null,
                oResult);
            Add(oItem);
        }

        // ����ͬʱ���ڵ�ͽ������ReversePolishItem����,��push��ջ��
        // parameter:
        //		node    �ڵ�
        //		oResult �����
        // return:
        //      void
        public void Push(XmlNode node,
            DpResultSet oResult)
        {
            ReversePolishItem oItem = new ReversePolishItem(node,
                oResult);
            Add(oItem);
        }

        // popһ������ֻ���ؽڵ�
        // return:
        //		node�ڵ�
        public XmlNode PopNode()
        {
            //ջΪ�գ��׳�StackUnderflowException�쳣
            if (this.Count == 0)
            {
                StackUnderflowException ex = new StackUnderflowException("Popǰ,��ջ�Ѿ���");
                throw (ex);
            }

            ReversePolishItem oTemp = (ReversePolishItem)this[this.Count - 1];
            this.RemoveAt(this.Count - 1);

            //���ܷ��ؿգ������ߴ��������ͣ�Ӧ�ڵ��ô����жϡ�
            return oTemp.m_node;
        }

        // popһ������ֻ���ؽ����
        // return:
        //		�����
        public DpResultSet PopResultSet()
        {
            if (this.Count == 0)
            {
                StackUnderflowException ex = new StackUnderflowException("Popǰ,��ջ�Ѿ���");
                throw (ex);
            }

            ReversePolishItem oTemp = (ReversePolishItem)this[this.Count - 1];
            this.RemoveAt(this.Count - 1);

            //���ܷ��ؿգ������ߴ��������ͣ�Ӧ�ڵ��ô����жϡ�
            return oTemp.m_resultSet;
        }


        // popһ������
        // return:
        //		ReversePolishItem����
        public ReversePolishItem Pop()
        {
            if (this.Count == 0)
            {
                StackUnderflowException ex = new StackUnderflowException("Popǰ,��ջ�Ѿ���");
                throw (ex);
            }

            ReversePolishItem oTemp = (ReversePolishItem)this[this.Count - 1];
            this.RemoveAt(this.Count - 1);

            //���ܷ��ؿգ������ߴ��������ͣ�Ӧ�ڵ��ô����жϡ�
            return oTemp;
        }

        // ����ջ��Ԫ�ص����ͣ��Ա�����pop�ڵ����pop�����
        // return:
        //		������ջ������ĵ�����
        public int PeekType()
        {
            if (this.Count == 0)
            {
                StackUnderflowException ex = new StackUnderflowException("Popǰ,��ջ�Ѿ���");
                throw (ex);
            }

            ReversePolishItem oTemp = (ReversePolishItem)this[this.Count - 1];
            return oTemp.m_int;
        }

        // ����ջ��Ԫ��
        // return:
        //      ����ReversePolishItem����
        public ReversePolishItem Peek()
        {
            if (this.Count == 0)
            {
                StackUnderflowException ex = new StackUnderflowException("Popǰ,��ջ�Ѿ���");
                throw (ex);
            }

            ReversePolishItem oTemp = (ReversePolishItem)this[this.Count - 1];
            return oTemp;
        }

    } //  end of class ReversePolishStack

    public class InterruptException : Exception
    {
        public InterruptException(string s)
            : base(s)
		{
		}
    }
}
