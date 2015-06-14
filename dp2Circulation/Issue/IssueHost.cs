using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.IO;
using DigitalPlatform.CommonControl;

/*
 * 1) ��Ҫ���һ�����ı��ļ���������ϸ������ÿ���ڿ����ڷֲ��������̽���ȱ�ڵ�ԭ�����ڵ��ԡ�Ҫ�����Ѿ��������ڽڵ��(���ݶ�����Ϣ)Ԥ����ڽڵ�
 * 
 * 
 * */

namespace dp2Circulation
{
    /// <summary>
    /// ��ӡ�ڿ���ȱ����������Ҫ�� ��������Ϣ��������
    /// �����ڴ��д洢��ģ�����ڵĽṹ
    /// </summary>
    public class IssueHost
    {
        /// <summary>
        /// ͨѶͨ��
        /// </summary>
        public LibraryChannel Channel = null;

        /// <summary>
        /// ֹͣ����
        /// </summary>
        public DigitalPlatform.Stop Stop = null;

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;


        // 
        /// <summary>
        /// ��Ŀ��¼·��
        /// </summary>
        public string BiblioRecPath = "";

        // �ڶ�������
        List<OneIssue> Issues = new List<OneIssue>();

        // ������������
        List<OneOrder> Orders = new List<OneOrder>();

        /// <summary>
        /// ����ڶ��󼯺�
        /// </summary>
        public void ClearIssues()
        {
            this.Issues.Clear();
        }

        /// <summary>
        /// ����������󼯺�
        /// </summary>
        public void ClearOrders()
        {
            this.Orders.Clear();
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }

#if NO
        // װ���ڼ�¼
        // return:
        //      -1  ����
        //      0   û��װ��
        //      1   �Ѿ�װ��
        public int LoadIssueRecords(string strBiblioRecPath,
            out string strError)
        {
            this.BiblioRecPath = strBiblioRecPath;
            this.ClearIssues();
            /*
            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����װ������Ϣ ...");
            stop.BeginLoop();
             * */

            try
            {
                // string strHtml = "";
                long lStart = 0;
                long lResultCount = 0;
                long lCount = -1;

                // 2012/5/9 ��дΪѭ����ʽ
                for (; ; )
                {
                    EntityInfo[] issues = null;

                    long lRet = Channel.GetIssues(
                        stop,
                        strBiblioRecPath,
                        lStart,
                        lCount,
                        "",
                        "zh",
                        out issues,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (lRet == 0)
                        return 0;

                    lResultCount = lRet;

                    Debug.Assert(issues != null, "");

                    for (int i = 0; i < issues.Length; i++)
                    {
                        if (issues[i].ErrorCode != ErrorCodeValue.NoError)
                        {
                            strError = "·��Ϊ '" + issues[i].OldRecPath + "' ���ڼ�¼װ���з�������: " + issues[i].ErrorInfo;  // NewRecPath
                            return -1;
                        }

                        OneIssue issue = new OneIssue();
                        int nRet = issue.LoadRecord(issues[i].OldRecord,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "·��Ϊ '" + issues[i].OldRecPath + "' ���ڼ�¼���ڳ�ʼ��OneIssue����ʱ��������: " + strError;
                            return -1;
                        }

                        this.Issues.Add(issue);
                    }

                    lStart += issues.Length;
                    if (lStart >= lResultCount)
                        break;
                }
            }
            finally
            {
                /*
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                 * */
            }

            return 1;
        ERROR1:
            return -1;
        }
#endif

        // 2012/8/30
        // 
        // return:
        //      -1  ����
        //      0   û��װ��
        //      1   �Ѿ�װ��
        /// <summary>
        /// װ���ڼ�¼
        /// </summary>
        /// <param name="strOrderRefID">������¼�Ĳο� ID</param>
        /// <param name="strOrderTime">����ʱ��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        ///      -1  ����
        ///      0   û��װ��
        ///      1   �Ѿ�װ��
        /// </returns>
        public int LoadIssueRecords(string strOrderRefID,
            string strOrderTime,
            out string strError)
        {
            this.ClearIssues();

            // ������ڿ��Ķ����⣬����Ҫͨ��������¼��refid����ڼ�¼�����ڼ�¼�в��ܵõ��ݲط�����Ϣ
            string strOutputStyle = "";
            long lRet = Channel.SearchIssue(Stop,
"<ȫ��>",
strOrderRefID,
-1,
"�����ο�ID",
"exact",
"zh",
"tempissue",
"",
strOutputStyle,
out strError);
            if (lRet == -1)
            {
                strError = "���� �����ο�ID Ϊ " + strOrderRefID + " ���ڼ�¼ʱ����: " + strError;
                return -1;
            }
            if (lRet == 0)
                return 0;


            long lHitCount = lRet;
            long lStart = 0;
            long lCount = lHitCount;
            DigitalPlatform.CirculationClient.localhost.Record[] searchresults = null;

            // ��ȡ���н��
            for (; ; )
            {

                lRet = Channel.GetSearchResult(
                    Stop,
                    "tempissue",
                    lStart,
                    lCount,
                    "id",
                    "zh",
                    out searchresults,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��ȡ�����ʱ����: " + strError;
                    return -1;
                }
                if (lRet == 0)
                {
                    strError = "��ȡ�����ʱ����: lRet = 0";
                    return -1;
                }

                for (int i = 0; i < searchresults.Length; i++)
                {
                    DigitalPlatform.CirculationClient.localhost.Record searchresult = searchresults[i];

                    string strIssueRecPath = searchresult.Path;

                    string strIssueXml = "";
                    string strOutputIssueRecPath = "";

                    string strBiblioText = "";
                    string strOutputBiblioRecPath = "";
                    byte[] item_timestamp = null;

                    lRet = Channel.GetIssueInfo(
Stop,
"@path:" + strIssueRecPath,
                        // "",
"xml",
out strIssueXml,
out strOutputIssueRecPath,
out item_timestamp,
"recpath",
out strBiblioText,
out strOutputBiblioRecPath,
out strError);
                    if (lRet == -1 || lRet == 0)
                    {
                        strError = "��ȡ�ڼ�¼ " + strIssueRecPath + " ʱ����: " + strError;
                        return -1;
                    }

                    OneIssue issue = new OneIssue();
                    int nRet = issue.LoadRecord(strIssueXml,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "·��Ϊ '" + strOutputIssueRecPath + "' ���ڼ�¼���ڳ�ʼ��OneIssue����ʱ��������: " + strError;
                        return -1;
                    }

                    // �޶�
                    issue.OrderRefIDs.Add(strOrderRefID);
                    issue.OrderTime = strOrderTime;

                    this.Issues.Add(issue);

#if NO
                        // Ѱ�� /orderInfo/* Ԫ��
                        XmlNode nodeRoot = issue_dom.DocumentElement.SelectSingleNode("orderInfo/*[refID/text()='" + strRefID + "']");
                        if (nodeRoot == null)
                        {
                            strError = "�ڼ�¼ '" + strOutputIssueRecPath + "' ��û���ҵ�<refID>Ԫ��ֵΪ '" + strRefID + "' �Ķ������ݽڵ�...";
                            return -1;
                        }

                        string strDistribute = DomUtil.GetElementText(nodeRoot, "distribute");

                        distributes.Add(strDistribute);
#endif
                }

                lStart += searchresults.Length;
                lCount -= searchresults.Length;

                if (lStart >= lHitCount || lCount <= 0)
                    break;
            }

            return 1;
        }

        // 2012/8/30
        // 
        // parameters:
        //      strOrderTime    ���ض�����¼�Ķ���ʱ�䡣RFC1123��ʽ
        // return:
        //      -1  ����
        //      0   û��װ��
        //      1   �Ѿ�װ��
        /// <summary>
        /// װ�붩����¼
        /// </summary>
        /// <param name="strOrderRecPath">������¼·��</param>
        /// <param name="strRefID">���ض�����¼�Ĳο� ID</param>
        /// <param name="strOrderTime">���ض�����¼�Ķ���ʱ�䡣RFC1123��ʽ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        ///      -1  ����
        ///      0   û��װ��
        ///      1   �Ѿ�װ��
        /// </returns>
        public int LoadOrderRecord(string strOrderRecPath,
            out string strRefID,
            out string strOrderTime,
            out string strError)
        {
            strError = "";
            strRefID = "";
            strOrderTime = "";

            this.ClearOrders();

            string strOutputBiblioRecPath = "";
            byte[] order_timestamp = null;
            string strOutputOrderRecPath = "";
            string strResult = "";
            string strBiblio = "";

            long lRet = Channel.GetOrderInfo(
                Stop,
                "@path:" + strOrderRecPath,
                "xml",
                out strResult,
                out strOutputOrderRecPath,
                out order_timestamp,
                "recpath",
                out strBiblio,
                out strOutputBiblioRecPath,
                out strError);
            if (lRet == -1)
                return -1;
            if (lRet == 0)
                return 0;

            this.BiblioRecPath = strOutputBiblioRecPath;

            OneOrder order = new OneOrder();
            int nRet = order.LoadRecord(
                strOutputOrderRecPath,
                strResult,
                out strError);
            if (nRet == -1)
            {
                strError = "·��Ϊ '" + strOutputOrderRecPath + "' �Ķ�����¼���ڳ�ʼ��OneOrder����ʱ��������: " + strError;
                return -1;
            }

            this.Orders.Add(order);

            strRefID = DomUtil.GetElementText(order.Dom.DocumentElement, "refID");
            strOrderTime = DomUtil.GetElementText(order.Dom.DocumentElement, "orderTime");
            return 1;
        }

#if NO
        // װ�붩����¼
        // return:
        //      -1  ����
        //      0   û��װ��
        //      1   �Ѿ�װ��
        public int LoadOrderRecords(string strBiblioRecPath,
            out string strError)
        {
            this.BiblioRecPath = strBiblioRecPath;
            this.ClearOrders();

            try
            {
                // string strHtml = "";
                long lStart = 0;
                long lResultCount = 0;
                long lCount = -1;

                // 2012/5/9 ��дΪѭ����ʽ
                for (; ; )
                {
                    EntityInfo[] orders = null;

                    long lRet = Channel.GetOrders(
                        stop,
                        strBiblioRecPath,
                            lStart,
                            lCount,
                            "",
                            "zh",
                        out orders,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    if (lRet == 0)
                        return 0;

                    lResultCount = lRet;

                    Debug.Assert(orders != null, "");

                    for (int i = 0; i < orders.Length; i++)
                    {
                        if (orders[i].ErrorCode != ErrorCodeValue.NoError)
                        {
                            strError = "·��Ϊ '" + orders[i].OldRecPath + "' �Ķ�����¼װ���з�������: " + orders[i].ErrorInfo;  // NewRecPath
                            return -1;
                        }

                        OneOrder order = new OneOrder();
                        int nRet = order.LoadRecord(
                            orders[i].OldRecPath,
                            orders[i].OldRecord,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "·��Ϊ '" + orders[i].OldRecPath + "' �Ķ�����¼���ڳ�ʼ��OneOrder����ʱ��������: " + strError;
                            return -1;
                        }

                        this.Orders.Add(order);
                    }

                    lStart += orders.Length;
                    if (lStart >= lResultCount)
                        break;
                }
            }
            finally
            {
                /*
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
                 * */
            }

            return 1;
        ERROR1:
            return -1;
        }
#endif

        // ��ÿ��õ���󶩹�ʱ�䷶Χ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetMaxOrderRange(out string strStartDate,
            out string strEndDate,
            out string strError)
        {
            strStartDate = "";
            strEndDate = "";
            strError = "";

            if (this.Orders == null || this.Orders.Count == 0)
                return 0;

            for (int i = 0; i < this.Orders.Count; i++)
            {
                XmlDocument dom = this.Orders[i].Dom;

                string strRange = DomUtil.GetElementText(dom.DocumentElement,
                    "range");

                string strIssueCount = DomUtil.GetElementText(dom.DocumentElement,
                    "issueCount");

                int nIssueCount = 0;
                try
                {
                    nIssueCount = Convert.ToInt32(strIssueCount);
                }
                catch
                {
                    continue;
                }

                int nRet = strRange.IndexOf("-");
                if (nRet == -1)
                {
                    strError = "ʱ�䷶Χ '" + strRange + "' ��ʽ����ȱ��-";
                    return -1;
                }

                string strStart = strRange.Substring(0, nRet).Trim();
                string strEnd = strRange.Substring(nRet + 1).Trim();

                if (strStart.Length != 8)
                {
                    strError = "ʱ�䷶Χ '" + strRange + "' ��ʽ������߲����ַ�����Ϊ8";
                    return -1;
                }
                if (strEnd.Length != 8)
                {
                    strError = "ʱ�䷶Χ '" + strRange + "' ��ʽ�����ұ߲����ַ�����Ϊ8";
                    return -1;
                }

                if (strStartDate == "")
                    strStartDate = strStart;
                else
                {
                    if (String.Compare(strStartDate, strStart) > 0)
                        strStartDate = strStart;
                }

                if (strEndDate == "")
                    strEndDate = strEnd;
                else
                {
                    if (String.Compare(strEndDate, strEnd) < 0)
                        strEndDate = strEnd;
                }
            }

            if (strStartDate == "")
            {
                Debug.Assert(strEndDate == "", "");
                return 0;
            }

            return 1;
        }

        // ���һ���ڵ�������
        // return:
        //      -1  ����
        //      0   �޷����
        //      1   ���
        int GetOneYearIssueCount(string strPublishYear,
            out int nValue,
            out string strError)
        {
            strError = "";
            nValue = 0;

            if (this.Orders == null || this.Orders.Count == 0)
                return 0;

            for (int i = 0; i < this.Orders.Count; i++)
            {
                XmlDocument dom = this.Orders[i].Dom;

                string strRange = DomUtil.GetElementText(dom.DocumentElement,
                    "range");

                string strIssueCount = DomUtil.GetElementText(dom.DocumentElement,
                    "issueCount");

                int nIssueCount = 0;
                try
                {
                    nIssueCount = Convert.ToInt32(strIssueCount);
                }
                catch
                {
                    continue;
                }

                float years = Global.Years(strRange);
                if (years != 0)
                {
                    nValue = Convert.ToInt32((float)nIssueCount * (1 / years));
                }
            }

            return 1;
        }

        // ���һ������ʱ���Ƿ����Ѿ������ķ�Χ��
        // ���ܻ��׳��쳣
        bool InOrderRange(string strPublishTime)
        {
            if (this.Orders == null || this.Orders.Count == 0)
                return false;

            for (int i = 0; i < this.Orders.Count; i++)
            {
                XmlDocument dom = this.Orders[i].Dom;

                string strRange = DomUtil.GetElementText(dom.DocumentElement,
                    "range");

                // �Ǻű�ʾͨ��
                if (strPublishTime != "*")
                {
                    if (Global.InRange(strPublishTime, strRange) == false)
                        continue;
                }

                return true;
            }

            return false;
        }

#if NO
        // Ԥ����һ�ڵĳ���ʱ��
        // exception:
        //      ������strPublishTimeΪ�����ܵ����ڶ��׳��쳣
        // parameters:
        //      strPublishTime  ��ǰ��һ�ڳ���ʱ��
        //      nIssueCount һ���ڳ�������
        static string NextPublishTime(string strPublishTime,
            int nIssueCount)
        {
            DateTime now = DateTimeUtil.Long8ToDateTime(strPublishTime);

            // һ��һ��
            if (nIssueCount == 1)
            {
                return DateTimeUtil.DateTimeToString8(DateTimeUtil.NextYear(now));
            }

            // һ������
            if (nIssueCount == 2)
            {
                // 6�����Ժ��ͬ��
                for (int i = 0; i < 6; i++)
                {
                    now = DateTimeUtil.NextMonth(now);
                }

                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ������
            if (nIssueCount == 3)
            {
                // 4�����Ժ��ͬ��
                for (int i = 0; i < 4; i++)
                {
                    now = DateTimeUtil.NextMonth(now);
                }

                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��4��
            if (nIssueCount == 4)
            {
                // 3�����Ժ��ͬ��
                for (int i = 0; i < 3; i++)
                {
                    now = DateTimeUtil.NextMonth(now);
                }

                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��5�� ��һ��6�ڴ���취һ��
            // һ��6��
            if (nIssueCount == 5 || nIssueCount == 6)
            {
                // 
                // 2�����Ժ��ͬ��
                for (int i = 0; i < 2; i++)
                {
                    now = DateTimeUtil.NextMonth(now);
                }

                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��7/8/9/10/11�� ��һ��12�ڴ���취һ��
            // һ��12��
            if (nIssueCount >= 7 && nIssueCount <= 12)
            {
                // 1�����Ժ��ͬ��
                now = DateTimeUtil.NextMonth(now);

                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��24��
            if (nIssueCount == 24)
            {
                // 15���Ժ�
                now += new TimeSpan(15, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��36��
            if (nIssueCount == 36)
            {
                // 10���Ժ�
                now += new TimeSpan(10, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(now);
            }

            // һ��48��
            if (nIssueCount == 48)
            {
                // 7���Ժ�
                now += new TimeSpan(7, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(now);
            }

            return "????????";  // �޷����������
        }
#endif

        // 
        // return:
        //      -1  error
        //      0   �޷���ö���ʱ�䷶Χ
        //      1   �ɹ�
        /// <summary>
        /// ����ÿ���ڶ���
        /// </summary>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        ///      -1  error
        ///      0   �޷���ö���ʱ�䷶Χ
        ///      1   �ɹ�
        /// </returns>
        public int CreateIssues(out string strError)
        {
            strError = "";

            List<OneIssue> issues = new List<OneIssue>();

            string strStartDate = "";
            string strEndDate = "";
            // ��ÿ��õ���󶩹�ʱ�䷶Χ
            // return:
            //      -1  error
            //      0   not found
            //      1   found
            int nRet = GetMaxOrderRange(out strStartDate,
                out strEndDate,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "�޷���ö���ʱ�䷶Χ";
                return 0;
            }

            // ��ʱ�䷶Χ��Ѱ�ҵ����ں�Ϊ'1'���Ѿ����ڵ��ڽڵ㣬
            // ��������ڣ���ٶ���һ�ڵĵ����ں�Ϊ'1'
            string strCurrentPublishTime = strStartDate;
            int nCurrentIssue = 1;

            // ����ѭ��������ȫ���ڵ�
            for (; ; )
            {
                try
                {
                    // ���һ���������ʱ���Ƿ񳬹�����ʱ�䷶Χ?
                    if (InOrderRange(strCurrentPublishTime) == false)
                        break;  // �����������һ��
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }

                OneIssue issue = new OneIssue();

                issue.LoadRecord("<root />", out strError);
                issue.PublishTime = strCurrentPublishTime;
                issue.Issue = nCurrentIssue.ToString();

                issues.Add(issue);


                string strNextPublishTime = "";
                int nNextIssue = 0;
                /*
                string strNextIssue = "";
                string strNextZong = "";
                string strNextVolume = "";
                 * */

                {
                    int nIssueCount = 0;
                    // ���һ���ڵ�������
                    // return:
                    //      -1  ����
                    //      0   �޷����
                    //      1   ���
                    nRet = GetOneYearIssueCount(strCurrentPublishTime,
                        out nIssueCount,
                        out strError);

                    try
                    {
                        // Ԥ����һ�ڵĳ���ʱ��
                        // parameters:
                        //      strPublishTime  ��ǰ��һ�ڳ���ʱ��
                        //      nIssueCount һ���ڳ�������
                        strNextPublishTime = BindingControl.NextPublishTime(strCurrentPublishTime,
                             nIssueCount);
                    }
                    catch (Exception ex)
                    {
                        // 2009/2/8
                        strError = "�ڻ������ '" + strCurrentPublishTime + "' �ĺ�һ�ڳ�������ʱ��������: " + ex.Message;
                        return -1;
                    }

                    if (strNextPublishTime == "????????")
                        break;

                    try
                    {

                        // ���һ���������ʱ���Ƿ񳬹�����ʱ�䷶Χ?
                        if (InOrderRange(strNextPublishTime) == false)
                            break;  // �����������һ��
                    }
                    catch (Exception ex)
                    {
                        strError = ex.Message;
                        return -1;
                    }


                    // �����Զ�������Ҫ֪��һ�����Ƿ���꣬����ͨ����ѯ�ɹ���Ϣ�õ�һ�������ĵ�����
                    if (nCurrentIssue >= nIssueCount)
                    {
                        // ������
                        // strNextIssue = "1";
                        nNextIssue = 1;

                        // 2012/9/1
                        strNextPublishTime = DateTimeUtil.NextYear(strCurrentPublishTime.Substring(0, 4)) + "0101";
                    }
                    else
                    {
                        // strNextIssue = (nCurrentIssue + 1).ToString();
                        nNextIssue = nCurrentIssue + 1;
                    }

                    /*
                    strNextZong = IncreaseNumber(ref_item.Zong);
                    if (nRefIssue >= nIssueCount && nIssueCount > 0)
                        strNextVolume = IncreaseNumber(ref_item.Volume);
                    else
                        strNextVolume = ref_item.Volume;
                    */
                }

                // nCreateCount++;

                strCurrentPublishTime = strNextPublishTime;
                nCurrentIssue = nNextIssue;
            }


            // ���²���ڽڵ�ϲ���this.Issues������
            nRet = MergeGuessIssues(issues,
                out strError);
            if (nRet == -1)
                return -1;

            return 1;
        }

        // ƥ���ںš�strRange�п���Ϊ"3/4/5"��������̬��Ҫ����ȷƥ����
        static bool MatchIssueNo(string strRange,
            string strOne)
        {
            string[] parts = strRange.Split(new char[] {'/'});
            foreach (string s in parts)
            {
                string strCurrent = s.Trim();
                if (string.IsNullOrEmpty(strCurrent) == true)
                    continue;
                if (strCurrent == strOne)
                    return true;
            }

            return false;
        }

        // ��this.Issues�и����ꡢ�ںŲ���һ���ڵ�
        // return:
        //      -1  not found
        //      >=0 found
        int FindIssue(string strYear,
            string strIssue,
            int nStartIndex)
        {

            for (int i = nStartIndex; i < this.Issues.Count; i++)
            {
                OneIssue issue = this.Issues[i];
                string strCurrentYear = issue.PublishTime.Substring(0, 4);
                if (strYear == strCurrentYear
                    && MatchIssueNo(issue.Issue, strIssue) == true)
                {
                    return i;
                }
            }

            return -1;
        }


        // �ƶ�����ʱ��
        // ���ܻ��׳��쳣
        // TODO: ����ƶ��󳬹��������ʱ�䷶Χ��ô��?
        // parameters:
        //      issues  ȫ����������ڶ�������
        //      indices �±����顣ָ����issues������±�
        static void MovePublishTime(List<OneIssue> issues,
            List<int> indices,
            TimeSpan delta)
        {
            // ͳ��Խ���귶Χ�ĳ̶�
            List<TimeSpan> exceeds = new List<TimeSpan>();
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                OneIssue issue = issues[index];

                string strRest = "";
                string strPublishTime = issue.PublishTime;

                strPublishTime = DateTimeUtil.CanonicalizePublishTimeString(strPublishTime);

                if (strPublishTime.Length > 8)
                {
                    strRest = strPublishTime.Substring(8);
                    strPublishTime = strPublishTime.Substring(0, 8);
                }

                DateTime time = DateTimeUtil.Long8ToDateTime(strPublishTime);
                int nYear = time.Year;
                time = time + delta;

                // ����һ��ʱ��Խ��ָ����ݶ��پ���
                TimeSpan exceed = GetExceedValue(nYear, time);
                if (exceed != new TimeSpan())
                    exceeds.Add(exceed);
            }

            // ���� delta
            if (exceeds.Count > 0)
            {
                exceeds.Sort();
                if (exceeds[0] < new TimeSpan(0))
                    delta -= exceeds[0];
                else
                    delta -= exceeds[exceeds.Count - 1];
            }

            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                OneIssue issue = issues[index];

                string strRest = "";
                string strPublishTime = issue.PublishTime;

                strPublishTime = DateTimeUtil.CanonicalizePublishTimeString(strPublishTime);

                if (strPublishTime.Length > 8)
                {
                    strRest = strPublishTime.Substring(8);
                    strPublishTime = strPublishTime.Substring(0, 8);
                }

                DateTime time = DateTimeUtil.Long8ToDateTime(strPublishTime);
                int nYear = time.Year;
                time = time + delta;

                Debug.Assert(nYear == time.Year, "");

                strPublishTime = DateTimeUtil.DateTimeToString8(time);
                strPublishTime += strRest;

                issue.PublishTime = strPublishTime;
            }
        }

        // 2012/9/1
        // ����һ��ʱ��Խ��ָ����ݶ��پ���
        static TimeSpan GetExceedValue(int nYear,
            DateTime time)
        {
            if (time.Year > nYear)
                return time - new DateTime(nYear, 12, 31);
            if (time.Year == nYear)
                return new TimeSpan();
            return time - new DateTime(nYear, 1, 1);
        }

#if NO
        // ���ճ���ʱ�䣬��һ���ڽڵ����this.Issues������ʵ�λ��
        // ���ܻ��׳��쳣
        void AddIssueByIssueNo(OneIssue issue)
        {
            // �淶Ϊ10λ
            string strYear = issue.PublishTime.Substring(0, 4);
            string strIssue = issue.Issue.PadLeft(3, '0');

            string strLastYear = "0000";
            string strLastIssue = "000";

            for (int i = 0; i < this.Issues.Count; i++)
            {
                OneIssue current_issue = this.Issues[i];

                // �淶Ϊ10λ
                string strCurrentYear = current_issue.PublishTime.Substring(0,4);
                string strCurrentIssue = current_issue.Issue.PadLeft(3, '0');

                if (String.Compare(strYear,strLastYear)>=0 && string.Compare(strIssue, strLastIssue) > 0
                    && String.Compare(strYear, strCurrentYear) <= 0 && String.Compare(strIssue, strCurrentIssue) <= 0)
                {
                    this.Issues.Insert(i, issue);
                    return;
                }

                strLastYear = strCurrentYear;
                strLastIssue = strCurrentIssue;
            }

            this.Issues.Add(issue);
        }
#endif

#if NOOOOOOOOOOOOOOOOOOOOOO
        // ���ճ���ʱ�䣬��һ���ڽڵ����this.Issues������ʵ�λ��
        // ���ܻ��׳��쳣
        void AddIssueByPublishTime(OneIssue issue)
        {
            // �淶Ϊ10λ
            string strPublishTime = issue.PublishTime;
            strPublishTime = CannonicalizepublishTimeString(strPublishTime);
            strPublishTime = strPublishTime.PadRight(10, '0');

            for (int i = 0; i < this.Issues.Count; i++)
            {
                OneIssue current_issue = this.Issues[i];

                // �淶Ϊ10λ
                string strCurrentPublishTime = current_issue.PublishTime;
                strCurrentPublishTime = CannonicalizepublishTimeString(strCurrentPublishTime);
                strCurrentPublishTime = strCurrentPublishTime.PadRight(10, '0');

                if (strPublishTime < strCurrentPublishTime)
                {
                    this.Issues.Insert(i, issue);
                    return;
                }
            }

            this.Issues.Add(issue);
        }
#endif

        /// <summary>
        /// ���ȫ���ڶ���ĵ�����Ϣ
        /// </summary>
        /// <returns>������Ϣ</returns>
        public string DumpIssue()
        {
            string strResult = "";
            for (int i = 0; i < this.Issues.Count; i++)
            {
                OneIssue issue = this.Issues[i];

                strResult += "publish_time ["+ issue.PublishTime + "] issue[" + issue.Issue + "] is_guess[" + issue.IsGuess.ToString() + "]\r\n";
            }

            return strResult;
        }

        // ���²���ڽڵ�ϲ���this.Issues������
        int MergeGuessIssues(List<OneIssue> guess_issues,
            out string strError)
        {
            strError = "";

            try
            {
                List<int> not_matchs = new List<int>();
                int nLastIndex = 0;
                TimeSpan last_delta = new TimeSpan(0);
                for (int i = 0; i < guess_issues.Count; i++)
                {
                    OneIssue guess_issue = guess_issues[i];

                    string strYear = guess_issue.PublishTime.Substring(0, 4);
                    string strIssue = guess_issue.Issue;

                    // ��this.Issues�и����ꡢ�ںŲ���һ���ڵ�
                    // return:
                    //      -1  not found
                    //      >=0 found
                    int index = FindIssue(strYear,
                        strIssue,
                        nLastIndex);
                    if (index == -1)
                    {
                        not_matchs.Add(i);  // û��ƥ���ϵ��±�

                        // ��һ���ڽڵ����this.Issues������ʵ�λ��
                        // ���ܻ��׳��쳣
                        // AddIssueByIssueNo(guess_issue);
                        this.Issues.Add(guess_issue);   // ������Ҫ����

                        guess_issue.IsGuess = true;
                    }
                    else
                    {
                        OneIssue found = this.Issues[index];
                        string strRealPublishTime = found.PublishTime;
                        string strGuessPublishTime = guess_issue.PublishTime;

                        strRealPublishTime = DateTimeUtil.CanonicalizePublishTimeString(strRealPublishTime);
                        strGuessPublishTime = DateTimeUtil.CanonicalizePublishTimeString(strGuessPublishTime);


                        // ����������죬Ȼ���ǰ��û��ƥ��Ľڵ�ĳ���ʱ�������Ӧ��ƽ��
                        DateTime real = DateTimeUtil.Long8ToDateTime(strRealPublishTime);
                        DateTime guess = DateTimeUtil.Long8ToDateTime(strGuessPublishTime);
                        TimeSpan delta = real - guess;

                        last_delta = delta;

                        // �ƶ�����ʱ��
                        // ���ܻ��׳��쳣
                        MovePublishTime(guess_issues,
                            not_matchs,
                            delta);
                        not_matchs.Clear();
                    }
                }

                // ���һ��û��ƥ���ϵ�
                if (not_matchs.Count > 0
                    && last_delta != new TimeSpan(0))
                {
                    // �ƶ�����ʱ��
                    // ���ܻ��׳��쳣
                    MovePublishTime(guess_issues,
                        not_matchs,
                        last_delta);
                    not_matchs.Clear();
                }

                // ����publishtime��issue����
                this.Issues.Sort(new OneIssueComparer());

                return 0;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }
        }

        // 
        // return:
        //      -1  error
        //      0   û�ж�����Ϣ
        //      1   ��ʼ���ɹ�
        /// <summary>
        /// ��ʼ���ؼ�
        /// </summary>
        /// <param name="strOrderRecPath">������¼·��</param>
        /// <param name="bGuess">�Ƿ�Ҫ�²�δ�����ں�</param>
        /// <param name="debugInfo">׷�ӵ�����Ϣ���������ǰΪ null����ʾ����ִ�й����в�����������Ϣ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        ///      -1  ����
        ///      0   û�ж�����Ϣ
        ///      1   ��ʼ���ɹ�
        /// </returns>
        public int Initial(string strOrderRecPath,
            bool bGuess,
            ref StringBuilder debugInfo,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // װ��һ��������¼
            string strRefID = "";
            string strOrderTime = "";
            // return:
            //      -1  ����
            //      0   û��װ��
            //      1   �Ѿ�װ��
            nRet = this.LoadOrderRecord(strOrderRecPath,
                out strRefID,
                out strOrderTime,
                out strError);
            if (nRet == -1 || nRet == 0)
            {
                strError = "��IssueHost��װ�붩����¼ " + strOrderRecPath + " ʱ��������: " + strError;
                return -1;
            }

            Debug.Assert(nRet == 1, "");

            if (debugInfo != null)
                debugInfo.Append("����������¼ '" + strOrderRecPath + "' �������С�refid ["+strRefID+"] ordertime ["+strOrderTime+"]\r\n");

            // ���ݶ�����¼�� refid װ���ڼ�¼
            nRet = this.LoadIssueRecords(strRefID,
                strOrderTime,
                out strError);
            if (nRet == -1)
            {
                strError = "��IssueHost��װ��Ͷ�����¼ " + strRefID + " �������ڼ�¼ʱ��������:" + strError;
                return -1;
            }

            if (debugInfo != null)
            {
                if (nRet == 0)
                    debugInfo.Append("refid '"+strRefID+"' û�������κ��ڼ�¼\r\n");
                else
                    debugInfo.Append("refid '" + strRefID + "' ���� "+this.Issues.Count.ToString()+" ���ڼ�¼\r\n");
            }

            // ��ʹû�������κ��ڼ�¼��ҲҪ��������

            if (bGuess == true)
            {
                // ����ÿ���ڶ���
                // return:
                //      -1  error
                //      0   �޷���ö���ʱ�䷶Χ
                //      1   �ɹ�
                nRet = this.CreateIssues(out strError);
                if (nRet == -1)
                {
                    strError = "��IssueHost��CreateIssues() " + strOrderRecPath + " error: " + strError;
                    return -1;
                }
            }

            if (nRet == 0)
                return 0;

            for (int i = 0; i < this.Issues.Count; i++)
            {
                OneIssue issue = this.Issues[i];

                // �ͱ���ʱ��ƥ������ɸ�������������ϵ
                // return:
                //      -1  error
                //      0   not found
                //      >0  ƥ��ĸ���
                nRet = issue.LinkOrders(
                    this.Orders,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return 1;
        }

        // 
        // ÿ��һ�У����������������˻���
        // TODO: ��ֱ��������Ҫ���⴦��
        // return:
        //      -1  error
        //      0   û���κ���Ϣ
        //      >0  ��Ϣ����
        /// <summary>
        /// ����ڸ�����Ϣ
        /// </summary>
        /// <param name="issue_infos">��������Ϣ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        ///      -1  ����
        ///      0   û���κ���Ϣ
        ///      >0  ��Ϣ����
        /// </returns>
        public int GetIssueInfo(
            out List<IssueInfo> issue_infos,
            out string strError)
        {
            strError = "";

            issue_infos = new List<IssueInfo>();

            for (int i = 0; i < this.Issues.Count; i++)
            {
                OneIssue issue = this.Issues[i];

                string strLastSeller = "";
                int nOrderCount = 0;
                int nArriveCount = 0;

                issue.Orders.Sort(new OrderSorter());
                for (int j = 0; j < issue.Orders.Count; j++)
                {
                    OneOrder order = issue.Orders[j];

                    string strCurrentSeller = order.Seller;
                    if (strCurrentSeller != strLastSeller
                        && strLastSeller != "")
                    {
                        // ����һ�λ���ֵ�Ƴ�
                        IssueInfo info = new IssueInfo();

                        info.PublishTime = issue.PublishTime;
                        info.OrderTime = issue.OrderTime;   // 2012/8/31
                        info.Issue = issue.Issue;
                        info.Seller = strLastSeller;
                        info.OrderCount = nOrderCount.ToString();
                        info.ArrivedCount = nArriveCount.ToString();
                        info.MissingCount = Math.Max(0, nOrderCount - nArriveCount).ToString();
                        issue_infos.Add(info);

                        nOrderCount = 0;
                        nArriveCount = 0;
                    }

                    nOrderCount += order.OldCopyValue;
                    nArriveCount += order.NewCopyValue;
                    strLastSeller = strCurrentSeller;
                }

                // �����һ�λ���ֵ�Ƴ�
                if (strLastSeller != "")
                {
                    IssueInfo info = new IssueInfo();

                    info.PublishTime = issue.PublishTime;
                    info.OrderTime = issue.OrderTime;   // 2012/8/31
                    info.Issue = issue.Issue;
                    info.Seller = strLastSeller;
                    info.OrderCount = nOrderCount.ToString();
                    info.ArrivedCount = nArriveCount.ToString();
                    info.MissingCount = Math.Max(0, nOrderCount - nArriveCount).ToString();
                    issue_infos.Add(info);
                }

            }

            return issue_infos.Count;
        }

        // 
        /// <summary>
        /// ��IssueInfo��������������������Ϊ����������
        /// </summary>
        /// <param name="issue_infos">IssueInfo����</param>
        /// <returns>NamedIssueInfoCollection �ļ���</returns>
        public static List<NamedIssueInfoCollection> SortIssueInfo(List<IssueInfo> issue_infos)
        {
            List<NamedIssueInfoCollection> results = new List<NamedIssueInfoCollection>();
            NamedIssueInfoCollection one = new NamedIssueInfoCollection();

            issue_infos.Sort(new IssueInfoSorter());

            for (int i = 0; i < issue_infos.Count; i++)
            {
                IssueInfo info = issue_infos[i];

                string strSeller = info.Seller;

                if (info.Seller != one.Seller
                    && one.Count > 0)
                {
                    results.Add(one);
                    one = new NamedIssueInfoCollection();
                }

                one.Seller = info.Seller;
                one.Add(info);
            }

            if (one.Count > 0)
            {
                results.Add(one);
            }

            return results;
        }

        // 
        /// <summary>
        /// ��IssueInfo�������Ѿ���������Ƴ�
        /// </summary>
        /// <param name="issue_infos">Ҫ����ļ���</param>
        public static void RemoveArrivedIssueInfos(ref List<IssueInfo> issue_infos)
        {
            for (int i = 0; i < issue_infos.Count; i++)
            {
                IssueInfo info = issue_infos[i];

                if (info.MissingCount == "0")
                {
                    issue_infos.RemoveAt(i);
                    i--;
                }
            }
        }

#if NOOOOOOOOOOO
        // ��IssueInfo�����д���ָ��ʱ�䷶Χ��������Ƴ�
        public static void RemoveOutofTimeRangeIssueInfos(ref List<IssueInfo> issue_infos,
            TimeFilter filter)
        {
#if NO
            string strLastArrivedPublishTime = "";
            // Ѱ��ʵ��1�����ϵ����һ��
            for (int i = 0; i < issue_infos.Count; i++)
            {
                IssueInfo info = issue_infos[i];

                int nArrivedCount = 0;

                try
                {
                    nArrivedCount = Convert.ToInt32(info.ArrivedCount);
                }
                catch
                {
                }

                if (nArrivedCount > 0)
                {
                    string strTemp = DateTimeUtil.ForcePublishTime8(info.PublishTime);

                    if (string.Compare(strTemp, strLastArrivedPublishTime) > 0)
                        strLastArrivedPublishTime = strTemp;
                }
            }

            // У��end��ʹ������ʾ���һ��ʵ�ʵ�����ڵĳ�������
            if (String.IsNullOrEmpty(strLastArrivedPublishTime) == false)
            {
                DateTime last = DateTimeUtil.Long8ToDateTime(strLastArrivedPublishTime);
                if (last > end)
                    end = last;
            }
#endif
            if (filter.Style == "none")
                return;

            DateTime start = filter.StartTime;
            DateTime end = filter.EndTime;

            for (int i = 0; i < issue_infos.Count; i++)
            {
                IssueInfo info = issue_infos[i];

                string strPublishTime = DateTimeUtil.ForcePublishTime8(info.PublishTime);

                DateTime publish_time = DateTimeUtil.Long8ToDateTime(strPublishTime);

                if (publish_time < start || (publish_time > end && end != new DateTime(0)))
                {
                    issue_infos.RemoveAt(i);
                    i--;
                }
            }
        }
#endif
        /// <summary>
        /// ���������ʾ������Ϣ�ַ���
        /// </summary>
        /// <param name="info">����Ϣ����</param>
        /// <returns>������ʾ���ַ���</returns>
        public static string GetIssueString(IssueInfo info)
        {
            return info.PublishTime + " ����� ��" + info.Issue + "��";
        }

        /// <summary>
        /// ���ʱ�䷶Χ�ַ���
        /// </summary>
        /// <param name="start">��ʼʱ��</param>
        /// <param name="end">����ʱ��</param>
        /// <returns>ʱ�䷶Χ�ַ���</returns>
        public static string GetRangeString(DateTime start, DateTime end)
        {
            end -= new TimeSpan(24, 0, 0);
            return start.ToShortDateString() + "-" + end.ToShortDateString();
        }

        // 
        /// <summary>
        /// ��IssueInfo�����д���ָ��ʱ�䷶Χ��������Ƴ�
        /// </summary>
        /// <param name="issue_infos">Ҫ����ļ���</param>
        /// <param name="filter">ʱ�������</param>
        /// <param name="strDebugInfo">���ص�����Ϣ</param>
        public static void RemoveOutofTimeRangeIssueInfos(ref List<IssueInfo> issue_infos,
            TimeFilter filter,
            out string strDebugInfo)
        {
            strDebugInfo = "";

            if (filter.Style == "none")
            {
                strDebugInfo = "������ʱ�����";
                return;
            }

            DateTime start = filter.StartTime;
            DateTime end = filter.EndTime;

            // Ѱ��ʵ��1�����ϵ����һ�ڡ�����һ�����ɣ���Ϊ���ĳ����Ȼ������ȱ�ķ�Χ������ʵ���ϵ��ˣ�����������ʱ�仹Ҫ�����Ӧ��Ҳ���ˡ�������Ҫ����ʵ�ʵ�����������Ǿ���������趨��ʱ��
            if (filter.VerifyArrivedIssue == true)
            {
                string strLastArrivedPublishTime = "";
                // Ѱ��ʵ��1�����ϵ����һ��
                for (int i = 0; i < issue_infos.Count; i++)
                {
                    IssueInfo info = issue_infos[i];

                    int nArrivedCount = 0;

                    try
                    {
                        nArrivedCount = Convert.ToInt32(info.ArrivedCount);
                    }
                    catch
                    {
                    }

                    if (nArrivedCount > 0)
                    {
                        string strTemp = DateTimeUtil.ForcePublishTime8(info.PublishTime);

                        if (string.Compare(strTemp, strLastArrivedPublishTime) > 0)
                            strLastArrivedPublishTime = strTemp;
                    }
                }

                // У��end��ʹ������ʾ���һ��ʵ�ʵ�����ڵĳ�������
                if (String.IsNullOrEmpty(strLastArrivedPublishTime) == false)
                {
                    DateTime last = DateTimeUtil.Long8ToDateTime(strLastArrivedPublishTime);
                    if (last > end)
                    {
                        strDebugInfo += "filter��ĩβʱ��� "+end.ToShortDateString()+" ������ʵ���ѵ������һ�� "+last.ToShortDateString()+"\r\n";
                        end = last;
                    }
                }
            }

            for (int i = 0; i < issue_infos.Count; i++)
            {
                IssueInfo info = issue_infos[i];

                DateTime publish_time = new DateTime(0);
                string strPublishTime = info.PublishTime;
                if (string.IsNullOrEmpty(strPublishTime) == false)
                {
                    strPublishTime = DateTimeUtil.ForcePublishTime8(strPublishTime);
                    publish_time = DateTimeUtil.Long8ToDateTime(strPublishTime);
                }

                DateTime order_time = new DateTime(0);
                string strOrderTime = info.OrderTime;
                if (string.IsNullOrEmpty(strOrderTime) == false)
                {
                    try
                    {
                        order_time = DateTimeUtil.FromRfc1123DateTimeString(info.OrderTime).ToLocalTime();
                        order_time += filter.OrderTimeDelta;
                    }
                    catch (Exception ex)
                    {
                        // 2015/1/27
                        strDebugInfo += "����: " + IssueHost.GetIssueString(info) + " ����ʱ�� '" + info.OrderTime + "' ��ʽ����: " + ex.Message + "\r\n";
                        order_time = new DateTime(0);
                    }
                }

                if (filter.Style == "publishtime")
                {
                    if (string.IsNullOrEmpty(strPublishTime) == true)
                    {
                        // ������Ҫ�ó���ʱ�䣬�򲻴���
                        strDebugInfo += IssueHost.GetIssueString(info) + " ���������Ϊ�գ����ų�\r\n";
                        goto DO_REMOVE;
                    }
                    // �ó���ʱ�����ж�
                    if (IssueHost.IsInRange(start, end, publish_time) == false)
                    {
                        strDebugInfo += IssueHost.GetIssueString(info) + " ��������� "+publish_time.ToShortDateString()+" ���� "+IssueHost.GetRangeString(start,end)+" ��Χ�ڣ����ų�\r\n";
                        goto DO_REMOVE;
                    }
                    goto CONTINUE;
                }

                if (filter.Style == "ordertime")
                {
                    if (string.IsNullOrEmpty(strOrderTime) == true)
                    {
                        // ������Ҫ�ö���ʱ�䣬�򲻴���
                        strDebugInfo += IssueHost.GetIssueString(info) + " �򶩹�����Ϊ�գ����ų�\r\n";
                        goto DO_REMOVE;
                    }
                    // �ö���ʱ�����ж�
                    if (order_time != new DateTime(0)
                        && IssueHost.IsInRange(start, end, order_time) == false)
                    {
                        strDebugInfo += IssueHost.GetIssueString(info) + " �򶩹������Ʋ�ĳ������� " + order_time.ToShortDateString() + " ���� " + IssueHost.GetRangeString(start, end) + " ��Χ�ڣ����ų�\r\n";
                        goto DO_REMOVE;
                    }
                    goto CONTINUE;
                }

                if (filter.Style == "both")
                {
                    if (string.IsNullOrEmpty(strPublishTime) == false)
                    {
                        // �ó���ʱ�����ж�
                        if (IssueHost.IsInRange(start, end, publish_time) == false)
                        {
                            strDebugInfo += IssueHost.GetIssueString(info) + " ��������� " + publish_time.ToShortDateString() + " ���� " + IssueHost.GetRangeString(start, end) + " ��Χ�ڣ����ų�\r\n";
                            goto DO_REMOVE;
                        }
                        goto CONTINUE;
                    }

                    if (string.IsNullOrEmpty(strOrderTime) == false)
                    {
                        // �ö���ʱ�����ж�
                        if (order_time != new DateTime(0)
                            && IssueHost.IsInRange(start, end, order_time) == false)
                        {
                            strDebugInfo += IssueHost.GetIssueString(info) + " �򶩹������Ʋ�ĳ������� " + order_time.ToShortDateString() + " ���� " + IssueHost.GetRangeString(start, end) + " ��Χ�ڣ����ų�\r\n";
                            goto DO_REMOVE;
                        }
                        goto CONTINUE;
                    }

                    // ����ʱ�䶼Ϊ��
                    goto DO_REMOVE;
                }

            CONTINUE:
                strDebugInfo += IssueHost.GetIssueString(info) + " ������\r\n";
                continue;
            DO_REMOVE:
                issue_infos.RemoveAt(i);
                i--;
            }
        }

        // ��һ��ʱ���Ƿ��ڷ�Χ��
        // start�ǰ����ģ�end�ǲ�������
        internal static bool IsInRange(DateTime start,
            DateTime end,
            DateTime current)
        {
            if (current < start || (current >= end && end != new DateTime(0)))
                return false;
            return true;
        }

        // 
        /// <summary>
        /// ���IssueInfo����ĵ����ı�
        /// </summary>
        /// <param name="issue_infos">IssueInfo����</param>
        /// <returns>�����ı�</returns>
        public static string DumpIssueInfos(List<IssueInfo> issue_infos)
        {
            string strResult = "";

            for (int i = 0; i < issue_infos.Count; i++)
            {
                IssueInfo info = issue_infos[i];

                strResult += "PublishTime[" + info.PublishTime + "]\tIssue[" + info.Issue + "]\tSeller[" + info.Seller + "]\tOrderCount[" + info.OrderCount + "]\tArrivedCount[" + info.ArrivedCount + "]\r\n";
            }

            return strResult;
        }

        /// <summary>
        /// ���������ַ XML
        /// </summary>
        /// <param name="strSeller">����</param>
        /// <returns>XML �ַ���</returns>
        public string GetAddressXml(string strSeller)
        {
            if (this.Orders == null)
                return null;

            for (int i = 0; i < this.Orders.Count; i++)
            {
                OneOrder order = this.Orders[i];

                if (order.Seller == strSeller)
                    return order.SellerAddress;
            }

            return null;
        }
    }

    // 
    /// <summary>
    /// �������ֵ�IssueInfo�������顣���־���������
    /// </summary>
    public class NamedIssueInfoCollection : List<IssueInfo>
    {
        /// <summary>
        /// ������
        /// </summary>
        public string Seller = "";
    }

    /// <summary>
    /// ����Ϣ
    /// </summary>
    public class IssueInfo
    {
        // 
        /// <summary>
        /// ��������
        /// </summary>
        public string PublishTime = "";

        // 
        /// <summary>
        /// ��������
        /// </summary>
        public string OrderTime = "";

        // 
        /// <summary>
        /// �����ں�
        /// </summary>
        public string Issue = "";

        // 
        /// <summary>
        /// ����(����)
        /// </summary>
        public string Seller = "";

        // 
        /// <summary>
        /// ����
        /// </summary>
        public string OrderCount = "";

        // 
        /// <summary>
        /// �ѵ���
        /// </summary>
        public string ArrivedCount = "";

        // 
        /// <summary>
        /// ȱ��
        /// </summary>
        public string MissingCount = "";
    }

    // �ڶ���
    /// <summary>
    /// һ���ڶ���
    /// </summary>
    public class OneIssue
    {
        /// <summary>
        /// �ڼ�¼�� XmlDocument
        /// </summary>
        public XmlDocument Dom = null;

        // 
        /// <summary>
        /// �Ƿ�Ϊ�²�Ľڵ�
        /// </summary>
        public bool IsGuess = false;

        /// <summary>
        /// ����ʱ�䡣RFC1123 ��ʽ
        /// </summary>
        public string OrderTime = "";   // RFC1123

        // 
        /// <summary>
        /// �ͱ���ʱ��ƥ������ɸ���������
        /// </summary>
        public List<OneOrder> Orders = null;

        // 
        /// <summary>
        /// �޶������Ķ�����¼�Ĳο� ID ����
        /// </summary>
        public List<string> OrderRefIDs = new List<string>();

        // 
        // return:
        //      -1  error
        //      0   not found
        //      >0  ƥ��ĸ���
        /// <summary>
        /// �ͱ���ʱ��ƥ������ɸ�������������ϵ
        /// </summary>
        /// <param name="orders">�������󼯺�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        ///      -1  ����
        ///      0   û���ҵ�
        ///      >0  ƥ��ĸ���
        /// </returns>
        public int LinkOrders(
            List<OneOrder> orders,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            this.Orders = new List<OneOrder>();

            // �ȴ��ڼ�¼��<orderInfo>Ԫ����ȡ
            List<string> XmlRecords = new List<string>();
            XmlNodeList nodes = this.Dom.DocumentElement.SelectNodes("orderInfo/*");

            if (nodes.Count > 0)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    OneOrder order = new OneOrder();
                    nRet = order.LoadRecord(
                        "",
                        nodes[i].OuterXml,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    if (this.OrderRefIDs.IndexOf(order.RefID) == -1)
                        continue;

                    this.Orders.Add(order);
                }

                return this.Orders.Count;
            }

            // ����ڼ�¼��û�ж�����Ϣ���ٴӶ�����¼��ȡ
            if (orders == null || orders.Count == 0)
                return 0;

            string strPublishTime = this.PublishTime;

            for (int i = 0; i < orders.Count; i++)
            {
                XmlDocument dom = orders[i].Dom;

                string strRange = DomUtil.GetElementText(dom.DocumentElement,
                    "range");

                // �Ǻű�ʾͨ��
                if (strPublishTime != "*")
                {
                    if (Global.InRange(strPublishTime, strRange) == false)
                        continue;
                }

                if (this.OrderRefIDs.Count > 0)
                {
                    if (this.OrderRefIDs.IndexOf(orders[i].RefID) == -1)
                        continue;
                }

                this.Orders.Add(orders[i]);
            }

            return this.Orders.Count;
        }

        /// <summary>
        /// װ�ؼ�¼ XML
        /// </summary>
        /// <param name="strXml">XML �ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int LoadRecord(string strXml,
            out string strError)
        {
            strError = "";

            this.Dom = new XmlDocument();
            try
            {
                this.Dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ��������: " + ex.Message;
                return -1;
            }

            OrderRefIDs.Clear();
            return 0;
        }

        /// <summary>
        /// �ο� ID
        /// </summary>
        public string RefID
        {
            get
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                return DomUtil.GetElementText(this.Dom.DocumentElement,
                    "refID");
            }
            set
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                DomUtil.SetElementText(this.Dom.DocumentElement,
                    "refID",
                    value);
            }
        }

        /// <summary>
        /// ����ʱ��
        /// </summary>
        public string PublishTime
        {
            get
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                return DomUtil.GetElementText(this.Dom.DocumentElement,
                    "publishTime");
            }
            set
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                DomUtil.SetElementText(this.Dom.DocumentElement,
                    "publishTime",
                    value);
            }
        }

        /// <summary>
        /// �����ں�
        /// </summary>
        public string Issue
        {
            get
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                return DomUtil.GetElementText(this.Dom.DocumentElement,
                    "issue");
            }
            set
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                DomUtil.SetElementText(this.Dom.DocumentElement,
                    "issue",
                    value);
            }
        }
    }

    // ��������
    /// <summary>
    /// һ����������
    /// </summary>
    public class OneOrder
    {
        /// <summary>
        /// ������¼·��
        /// </summary>
        public string RecPath = ""; // ������¼·��

        /// <summary>
        /// ������¼ XmlComent
        /// </summary>
        public XmlDocument Dom = null;

        // parameters:
        //      strRecPath  ������¼·��
        /// <summary>
        /// װ�ؼ�¼ XML
        /// </summary>
        /// <param name="strRecPath">��¼·��</param>
        /// <param name="strXml">XML �ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int LoadRecord(
            string strRecPath,
            string strXml,
            out string strError)
        {
            strError = "";

            this.RecPath = strRecPath;

            this.Dom = new XmlDocument();
            try
            {
                this.Dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ��������: " + ex.Message;
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// ����
        /// </summary>
        public string Seller
        {
            get
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                return DomUtil.GetElementText(this.Dom.DocumentElement,
                    "seller");
            }
            set
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                DomUtil.SetElementText(this.Dom.DocumentElement,
                    "seller",
                    value);
            }
        }

        /// <summary>
        /// �ο� ID
        /// </summary>
        public string RefID
        {
            get
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                return DomUtil.GetElementText(this.Dom.DocumentElement,
                    "refID");
            }
            set
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                DomUtil.SetElementText(this.Dom.DocumentElement,
                    "refID",
                    value);
            }
        }

        // 
        /// <summary>
        /// ���Ԥ�Ƶĳ���ʱ��
        /// </summary>
        /// <param name="filter">ʱ�������</param>
        /// <param name="strPublishTime">���س���ʱ��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int GetPublishTime(
            TimeFilter filter,
            out string strPublishTime,
            out string strError)
        {
            strError = "";
            strPublishTime = "";

            if (this.Dom == null)
                throw new Exception("dom��δ��ʼ��");

            string strValue = DomUtil.GetElementText(this.Dom.DocumentElement,
"range");
            if (string.IsNullOrEmpty(strValue) == false)
            {
                int nRet = strValue.IndexOf("-");
                if (nRet == -1)
                {
                    if (strValue.Length == 8)
                    {
                        strPublishTime = strValue;
                        return 0;
                    }
                }
                else
                {
                    string strLeft = strValue.Substring(0, nRet).Trim();
                    // ȡ��һ��ʱ���
                    strValue = strValue.Substring(nRet + 1);
                    if (strValue.Length == 8)
                    {
                        strPublishTime = strValue;
                        return 0;
                    }
                    if (string.IsNullOrEmpty(strValue) == true)
                    {
                        // 2012/9/1
                        // �Ҷ�ʱ��Ϊ��
                        strValue = strLeft; // �������ʱ��
                        if (strValue.Length == 8)
                        {
                            strPublishTime = strValue;
                            return 0;
                        }
                    }
                }

                // ��ʽ����
                strError = "<range>ֵ '" + strValue + "' ��ʽ����";
                return -1;
            }

#if NO
            strValue = this.OrderTime;
            //strValue = DomUtil.GetElementText(this.Dom.DocumentElement,
            //    "orderTime");
            if (string.IsNullOrEmpty(strValue) == true)
            {
                strError = "������¼����δд�����ʱ��<orderTime>���ݡ���ͨ������Ϊû�о�����ӡ����������ɵġ�";
                return -1;
            }
            DateTime time = DateTimeUtil.FromRfc1123DateTimeString(strValue).ToLocalTime();
            // TODO: ������������ýű������������ʱ��
            time += filter.OrderTimeDelta;
            strPublishTime = DateTimeUtil.DateTimeToString8(time);
#endif
            return 0;
        }
    
        /// <summary>
        /// ʱ�䷶Χ
        /// </summary>
        public string Range
        {
            get
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                return DomUtil.GetElementText(this.Dom.DocumentElement,
    "range");
            }
            set
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                DomUtil.SetElementText(this.Dom.DocumentElement,
                    "range",
                    value);
            }
        }

        /// <summary>
        /// ����ʱ��
        /// </summary>
        public string OrderTime
        {
            get
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                return DomUtil.GetElementText(this.Dom.DocumentElement,
                    "orderTime");
            }
            set
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                DomUtil.SetElementText(this.Dom.DocumentElement,
                    "orderTime",
                    value);
            }
        }

        /// <summary>
        /// ������
        /// </summary>
        public string Copy
        {
            get
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                return DomUtil.GetElementText(this.Dom.DocumentElement,
                    "copy");
            }
            set
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                DomUtil.SetElementText(this.Dom.DocumentElement,
                    "copy",
                    value);
            }
        }

        /// <summary>
        /// ����������
        /// </summary>
        public int OldCopyValue
        {
            get
            {
                string strOldValue = "";
                string strNewValue = "";
                // ���� "old[new]" �ڵ�����ֵ
                OrderDesignControl.ParseOldNewValue(this.Copy,
                    out strOldValue,
                    out strNewValue);

                // �����г˺�
                string strLeftCopy = OrderDesignControl.GetCopyFromCopyString(strOldValue);
                string strRightCopy = OrderDesignControl.GetRightFromCopyString(strOldValue);

                try
                {
                    return Convert.ToInt32(strLeftCopy);
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// ���ո�����
        /// </summary>
        public int NewCopyValue
        {
            get
            {
                string strOldValue = "";
                string strNewValue = "";
                // ���� "old[new]" �ڵ�����ֵ
                OrderDesignControl.ParseOldNewValue(this.Copy,
                    out strOldValue,
                    out strNewValue);

                // �����г˺�
                string strLeftCopy = OrderDesignControl.GetCopyFromCopyString(strNewValue);
                string strRightCopy = OrderDesignControl.GetRightFromCopyString(strNewValue);

                try
                {
                    return Convert.ToInt32(strLeftCopy);
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// ������ַ
        /// </summary>
        public string SellerAddress
        {
            get
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                // 2009/9/17 changed
                return DomUtil.GetElementInnerXml(this.Dom.DocumentElement,
                    "sellerAddress");
            }
            set
            {
                if (this.Dom == null)
                    throw new Exception("dom��δ��ʼ��");

                DomUtil.SetElementInnerXml(this.Dom.DocumentElement,
                    "sellerAddress",
                    value);
            }
        }
    }

    // ��������Ϣ������������������
    internal class OrderSorter : IComparer<OneOrder>
    {
        int IComparer<OneOrder>.Compare(OneOrder x, OneOrder y)
        {
            return string.Compare(x.Seller, y.Seller);
        }
    }

    // ������Ϣ�������������ơ��������+�ں�����
    internal class IssueInfoSorter : IComparer<IssueInfo>
    {
        int IComparer<IssueInfo>.Compare(IssueInfo x, IssueInfo y)
        {
            int nRet = string.Compare(x.Seller, y.Seller);
            if (nRet != 0)
                return nRet;

            // 2012/9/1
            {
                string strXYearPart = IssueUtil.GetYearPart(x.PublishTime);
                string strYYearPart = IssueUtil.GetYearPart(y.PublishTime);

                /*
                int nMaxWidth = Math.Max(x.Issue.Length, y.Issue.Length);
                string strXIssue = x.Issue.PadLeft(nMaxWidth, '0');
                string strYIssue = y.Issue.PadLeft(nMaxWidth, '0');
                 * */
                string strXIssue = x.Issue.Trim();
                string strYIssue = y.Issue.Trim();
                OneIssueComparer.FixingWidth(ref strXIssue, ref strYIssue);

                nRet = string.Compare(strXYearPart + "!" + strXIssue, strYYearPart + "!" + strYIssue);
                if (nRet != 0)
                    return nRet;
            }

            return string.Compare(x.PublishTime, y.PublishTime);
        }
    }

    // �Ƚϳ������+�ںš�С����ǰ
    internal class OneIssueComparer : IComparer<OneIssue>
    {
        // 2012/10/12
        // ���ںŹ���Ϊ�̶���ȡ���֮ǰ��Ҫ�ӡ�3/4/5��������̬�а�"3"ȡ��
        public static void FixingWidth(ref string strIssue1,
            ref string strIssue2)
        {
            int nRet = strIssue1.IndexOf("/");
            if (nRet != -1)
                strIssue1 = strIssue1.Substring(0, nRet).Trim();
            nRet = strIssue2.IndexOf("/");
            if (nRet != -1)
                strIssue2 = strIssue2.Substring(0, nRet).Trim();

            int nMaxWidth = Math.Max(strIssue1.Length, strIssue2.Length);
            strIssue1 = strIssue1.PadLeft(nMaxWidth, '0');
            strIssue2 = strIssue2.PadLeft(nMaxWidth, '0');
        }

        int IComparer<OneIssue>.Compare(OneIssue x, OneIssue y)
        {
            {
                string strXYearPart = IssueUtil.GetYearPart(x.PublishTime);
                string strYYearPart = IssueUtil.GetYearPart(y.PublishTime);

                /*
                int nMaxWidth = Math.Max(x.Issue.Length, y.Issue.Length);
                string strXIssue = x.Issue.PadLeft(nMaxWidth, '0');
                string strYIssue = y.Issue.PadLeft(nMaxWidth, '0');
                 * */
                string strXIssue = x.Issue.Trim();
                string strYIssue = y.Issue.Trim();
                FixingWidth(ref strXIssue, ref strYIssue);

                int nRet = string.Compare(strXYearPart + "!" + strXIssue, strYYearPart + "!" + strYIssue);
                if (nRet != 0)
                    return nRet;
            }

            return string.Compare(x.PublishTime, y.PublishTime);
        }
#if NO
        int IComparer<OneIssue>.Compare(OneIssue x, OneIssue y)
        {
            string s1 = x.PublishTime;
            string s2 = y.PublishTime;

            int nRet = String.Compare(s1, s2);
            if (nRet == 0)
            {
                int nMaxWidth = Math.Max(x.Issue.Length, y.Issue.Length);
                string strXIssue = x.Issue.PadLeft(nMaxWidth, '0');
                string strYIssue = y.Issue.PadLeft(nMaxWidth, '0');

                return String.Compare(strXIssue, strYIssue);
            }

            return nRet;
        }
#endif

    }
}
