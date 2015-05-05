using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Web;
using System.Diagnostics;

using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.dp2.Statis;
using DigitalPlatform.Marc;
using DigitalPlatform.Script;

using DigitalPlatform.CirculationClient;
using DigitalPlatform.CirculationClient.localhost;

// 2013/3/16 ��� XML ע��

namespace dp2Circulation
{
    /// <summary>
    /// BiblioStatisForm (��Ŀͳ�ƴ�) ͳ�Ʒ�����������
    /// </summary>
    public class BiblioStatis : StatisHostBase
    {
        /// <summary>
        /// �������������� BiblioStatisForm (��Ŀͳ�ƴ�)
        /// </summary>
        public BiblioStatisForm BiblioStatisForm = null;	// ����

        /// <summary>
        /// ��ǰ��Ŀ��� ���ݸ�ʽ
        /// </summary>
        public string CurrentDbSyntax = "";

        /// <summary>
        /// ��ǰ��Ŀ��¼·��
        /// </summary>
        public string CurrentRecPath = "";    // 

        /// <summary>
        /// ��ǰ��Ŀ��¼�������е��±ꡣ�� 0 ��ʼ���������Ϊ -1����ʾ��δ��ʼ����
        /// </summary>
        public long CurrentRecordIndex = -1; // 

        /// <summary>
        /// ��ǰ���ڴ������Ŀ XML ��¼��XmlDocument ����
        /// </summary>
        public XmlDocument BiblioDom = null;    // Xmlװ��XmlDocument

        string m_strXml = "";
        /// <summary>
        /// ��ǰ���ڴ������Ŀ XML ��¼���ַ�������
        /// </summary>
        public string Xml
        {
            get
            {
                return this.m_strXml;
            }
            set
            {
                this.m_strXml = value;
            }
        }

        /// <summary>
        /// ��ǰ��Ŀ��¼��ʱ���
        /// </summary>
        public byte[] Timestamp = null;

        /// <summary>
        /// ��ǰ��Ŀ��¼�� MARC ���ڸ�ʽ�ַ���
        /// </summary>
        public string MarcRecord = "";

        /// <summary>
        /// ���캯��
        /// </summary>
        public BiblioStatis()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        /// <summary>
        /// ��һ�� MARC ��¼���浽��ǰ���ڴ������Ŀ��¼�����ݿ�ԭʼλ��
        /// ��ν��ǰλ���� this.CurrentRecPath ����
        /// �ύ���������õ�ʱ����� this.Timestamp
        /// </summary>
        /// <param name="strMARC">MARC���ڸ�ʽ�ַ���</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError ��; 0: �ɹ�</returns>
        public int SaveMarcRecord(string strMARC,
            out string strError)
        {
            strError = "";

            string strXml = "";
            int nRet = MarcUtil.Marc2Xml(strMARC,
                this.CurrentDbSyntax,
                out strXml,
                out strError);
            if (nRet == -1)
                return -1;

            string strOutputPath = "";
            byte[] baNewTimestamp = null;
            nRet = this.BiblioStatisForm.SaveXmlBiblioRecordToDatabase(this.CurrentRecPath,
                strXml,
                this.Timestamp,
                out strOutputPath,
                out baNewTimestamp,
                out strError);
            if (nRet == -1)
                return -1;

            this.Timestamp = baNewTimestamp;
            return 0;
        }


        // ÿһ��¼���ڴ���MARCFilter֮ǰ
        /// <summary>
        /// ����һ����¼֮ǰ����ͳ�Ʒ���ִ���У������׶Σ����ÿ����¼������һ�Σ��� OnRecord() ֮ǰ����
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void PreFilter(object sender, StatisEventArgs e)
        {

        }

        internal override string GetOutputFileNamePrefix()
        {
            return Path.Combine(this.BiblioStatisForm.MainForm.DataDir, "~biblio_statis");
        }

        // ͨ�ð汾
        List<ItemInfo> GetItemInfos(string strDbType,
            string strHowToGetItemRecord,
            ref List<ItemInfo> item_infos)
        {
            // �Ż��ٶ�
            if (item_infos != null)
                return item_infos;

            // �����ǰ��Ŀ����û�а���ʵ��⣬���û��׳��쳣�����⴦��
            // TODO: �Ƿ���Ҫ��hashtable�Ż��ٶ�?
            string strBiblioDBName = Global.GetDbName(this.CurrentRecPath);
            string strItemDbName = "";
            
            if (strDbType == "item")
                strItemDbName = this.BiblioStatisForm.MainForm.GetItemDbName(strBiblioDBName);
            else if (strDbType == "order")
                strItemDbName = this.BiblioStatisForm.MainForm.GetOrderDbName(strBiblioDBName);
            else if (strDbType == "issue")
                strItemDbName = this.BiblioStatisForm.MainForm.GetIssueDbName(strBiblioDBName);
            else if (strDbType == "comment")
                strItemDbName = this.BiblioStatisForm.MainForm.GetCommentDbName(strBiblioDBName);
            else
            {
                throw new Exception("δ֪�� strDbType '"+strDbType+"'");
            }

            if (String.IsNullOrEmpty(strItemDbName) == true)
                return new List<ItemInfo>();    // ����һ���յ�����

            item_infos = new List<ItemInfo>();

            long lPerCount = 100; // ÿ����ö��ٸ�
            long lStart = 0;
            long lResultCount = 0;
            long lCount = -1;
            for (; ; )
            {

                string strStyle = "";
                if (strHowToGetItemRecord == "delay")
                    strStyle = "onlygetpath";
                else if (strHowToGetItemRecord == "first")
                    strStyle = "onlygetpath,getfirstxml";

                EntityInfo[] infos = null;
                string strError = "";
                long lRet = 0;
                
                if (strDbType == "item")
                lRet = this.BiblioStatisForm.Channel.GetEntities(
                     null,
                     this.CurrentRecPath,
                     lStart,
                     lCount,
                     strStyle,
                     "zh",
                     out infos,
                     out strError);
                else if (strDbType == "order")
                    lRet = this.BiblioStatisForm.Channel.GetOrders(
                         null,
                         this.CurrentRecPath,
                         lStart,
                         lCount,
                         strStyle,
                         "zh",
                         out infos,
                         out strError);
                else if (strDbType == "issue")
                    lRet = this.BiblioStatisForm.Channel.GetIssues(
                         null,
                         this.CurrentRecPath,
                         lStart,
                         lCount,
                         strStyle,
                         "zh",
                         out infos,
                         out strError);
                else if (strDbType == "comment")
                    lRet = this.BiblioStatisForm.Channel.GetComments(
                         null,
                         this.CurrentRecPath,
                         lStart,
                         lCount,
                         strStyle,
                         "zh",
                         out infos,
                         out strError);

                if (lRet == -1)
                    throw new Exception(strError);

                lResultCount = lRet;    // 2009/11/23 new add

                if (infos == null)
                    return item_infos;

                for (int i = 0; i < infos.Length; i++)
                {
                    EntityInfo info = infos[i];
                    string strXml = info.OldRecord;

                    /*
                    if (String.IsNullOrEmpty(strXml) == true)
                        continue;
                     * */

                    ItemInfo item_info = new ItemInfo(strDbType);
                    item_info.Container = this;
                    item_info.RecPath = info.OldRecPath;
                    item_info.Timestamp = info.OldTimestamp;
                    item_info.OldRecord = strXml;

                    item_infos.Add(item_info);
                }

                lStart += infos.Length;
                if (lStart >= lResultCount)
                    break;

                if (lCount == -1)
                    lCount = lPerCount;

                if (lStart + lCount > lResultCount)
                    lCount = lResultCount - lStart;

            } // end of for

            return item_infos;
        }

        #region ʵ���
        List<ItemInfo> m_itemInfos = null;

        /// <summary>
        /// ��λ�õ�ǰ��Ŀ��¼������ �� ��¼ ?
        /// all/delay/first  һ����ȫ�����/�ӳٻ��/�״λ�õ�һ��
        /// </summary>
        public string HowToGetItemRecord = "all";   // all/delay/first  һ����ȫ�����/�ӳٻ��/�״λ�õ�һ��

        internal void ClearItemDoms()
        {
            this.m_itemInfos = null;
        }

        /// <summary>
        /// ��õ�ǰ��Ŀ��¼������ ���¼��Ϣ����
        /// </summary>
        public List<ItemInfo> ItemInfos
        {
            get
            {
                return this.GetItemInfos("item",
                    this.HowToGetItemRecord,
                    ref this.m_itemInfos);
            }
        }

        // �����޸Ĺ��Ĳ���Ϣ��
        // ���ñ�����ǰ��Ҫ�޸�Dom��Ա
        // return:
        //      -1  error
        //      0   succeed
        /// <summary>
        /// ���浱ǰ��Ŀ��¼������ ���¼��Ϣ
        /// </summary>
        /// <param name="iteminfos">Ҫ����Ĳ��¼��Ϣ����</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError �У� 0: �ɹ�</returns>
        public int SaveItemInfo(List<ItemInfo> iteminfos,
            out string strError)
        {
            return SaveItemInfo("item",
                iteminfos,
                out strError);
        }

        #endregion

        #region ������
        List<ItemInfo> m_orderInfos = null;

        /// <summary>
        /// ��λ�õ�ǰ��¼������ ���� ��¼ ?
        /// all/delay/first  һ����ȫ�����/�ӳٻ��/�״λ�õ�һ��
        /// </summary>
        public string HowToGetOrderRecord = "all";   // all/delay/first  һ����ȫ�����/�ӳٻ��/�״λ�õ�һ��

        internal void ClearOrderDoms()
        {
            this.m_orderInfos = null;
        }

        /// <summary>
        /// ��û�õ�ǰ��Ŀ��¼������ ������¼��Ϣ����
        /// </summary>
        public List<ItemInfo> OrderInfos
        {
            get
            {
                return this.GetItemInfos("order",
                    this.HowToGetOrderRecord,
                    ref this.m_orderInfos);
            }
        }

        // �����޸Ĺ��Ķ�����Ϣ��
        // ���ñ�����ǰ��Ҫ�޸�Dom��Ա
        // return:
        //      -1  error
        //      0   succeed
        /// <summary>
        /// ���浱ǰ��Ŀ��¼������ ������¼��Ϣ
        /// </summary>
        /// <param name="orderinfos">Ҫ����Ķ�����¼��Ϣ����</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError �У� 0: �ɹ�</returns>
        public int SaveOrderInfo(List<ItemInfo> orderinfos,
            out string strError)
        {
            return SaveItemInfo("order",
                orderinfos,
                out strError);
        }

        #endregion

        #region �ڿ�
        List<ItemInfo> m_issueInfos = null;

        /// <summary>
        /// ��λ�õ�ǰ��Ŀ��¼������ �� ��¼ ?
        /// all/delay/first  һ����ȫ�����/�ӳٻ��/�״λ�õ�һ��
        /// </summary>
        public string HowToGetIssueRecord = "all";   // all/delay/first  һ����ȫ�����/�ӳٻ��/�״λ�õ�һ��

        internal void ClearIssueDoms()
        {
            this.m_issueInfos = null;
        }

        /// <summary>
        /// ��õ�ǰ��Ŀ��¼������ �ڼ�¼��Ϣ����
        /// </summary>
        public List<ItemInfo> IssueInfos
        {
            get
            {
                return this.GetItemInfos("issue",
                    this.HowToGetIssueRecord,
                    ref this.m_issueInfos);
            }
        }

        // �����޸Ĺ�������Ϣ��
        // ���ñ�����ǰ��Ҫ�޸�Dom��Ա
        // return:
        //      -1  error
        //      0   succeed
        /// <summary>
        /// ���浱ǰ��Ŀ��¼������ �ڼ�¼��Ϣ
        /// </summary>
        /// <param name="issueinfos">Ҫ������ڼ�¼��Ϣ����</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError �У� 0: �ɹ�</returns>
        public int SaveIssueInfo(List<ItemInfo> issueinfos,
            out string strError)
        {
            return SaveItemInfo("issue",
                issueinfos,
                out strError);
        }
        #endregion

        #region ��ע��
        List<ItemInfo> m_commentInfos = null;

        /// <summary>
        /// ��λ�õ�ǰ��¼������ ��ע ��¼ ?
        /// all/delay/first  һ����ȫ�����/�ӳٻ��/�״λ�õ�һ��
        /// </summary>
        public string HowToGetCommentRecord = "all";   // all/delay/first  һ����ȫ�����/�ӳٻ��/�״λ�õ�һ��

        internal void ClearCommentDoms()
        {
            this.m_commentInfos = null;
        }

        /// <summary>
        /// ��û�õ�ǰ��Ŀ��¼������ ��ע��¼��Ϣ����
        /// </summary>
        public List<ItemInfo> CommentInfos
        {
            get
            {
                return this.GetItemInfos("comment",
                    this.HowToGetCommentRecord,
                    ref this.m_commentInfos);
            }
        }

        // �����޸Ĺ�����ע��Ϣ��
        // ���ñ�����ǰ��Ҫ�޸�Dom��Ա
        // return:
        //      -1  error
        //      0   succeed
        /// <summary>
        /// ���浱ǰ��Ŀ��¼������ ��ע��¼��Ϣ
        /// </summary>
        /// <param name="commentinfos">Ҫ�������ע��¼��Ϣ����</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError �У� 0: �ɹ�</returns>
        public int SaveCommentInfo(List<ItemInfo> commentinfos,
            out string strError)
        {
            return SaveItemInfo("comment",
                commentinfos,
                out strError);
        }

        #endregion

        // (�������ݿ�����ͨ�ð汾)
        // �����޸Ĺ��Ĳ���Ϣ��
        // ���ñ�����ǰ��Ҫ�޸�Dom��Ա
        // return:
        //      -1  error
        //      0   succeed
        /// <summary>
        /// ���浱ǰ��Ŀ��¼������ ��/����/��/��ע��¼��Ϣ
        /// </summary>
        /// <param name="strDbType">�������ݿ����͡�Ϊ item/order/issue/comment ֮һ</param>
        /// <param name="iteminfos">Ҫ�������ע��¼��Ϣ����</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError �У� 0: �ɹ�</returns>
        public int SaveItemInfo(
            string strDbType,
            List<ItemInfo> iteminfos,
            out string strError)
        {
            strError = "";
            List<EntityInfo> entityArray = new List<EntityInfo>();

            for (int i = 0; i < iteminfos.Count; i++)
            {
                ItemInfo item = iteminfos[i];

                EntityInfo info = new EntityInfo();

                if (String.IsNullOrEmpty(item.RefID) == true)
                {
                    item.RefID = Guid.NewGuid().ToString();
                }

                info.RefID = item.RefID;

                DomUtil.SetElementText(item.Dom.DocumentElement,
                    "parent", Global.GetRecordID(CurrentRecPath));

                string strXml = item.Dom.DocumentElement.OuterXml;

                info.OldRecPath = item.RecPath;
                info.Action = "change";
                info.NewRecPath = item.RecPath;

                info.NewRecord = strXml;
                info.NewTimestamp = null;

                info.OldRecord = item.OldRecord;
                info.OldTimestamp = item.Timestamp;

                entityArray.Add(info);
            }

            // ���Ƶ�Ŀ��
            EntityInfo[] entities = null;
            entities = new EntityInfo[entityArray.Count];
            for (int i = 0; i < entityArray.Count; i++)
            {
                entities[i] = entityArray[i];
            }

            EntityInfo[] errorinfos = null;

            long lRet = 0;

            if (strDbType == "item")
                lRet = this.BiblioStatisForm.Channel.SetEntities(
                     null,   // this.BiblioStatisForm.stop,
                     this.CurrentRecPath,
                     entities,
                     out errorinfos,
                     out strError);
            else if (strDbType == "order")
                lRet = this.BiblioStatisForm.Channel.SetOrders(
                     null,   // this.BiblioStatisForm.stop,
                     this.CurrentRecPath,
                     entities,
                     out errorinfos,
                     out strError);
            else if (strDbType == "issue")
                lRet = this.BiblioStatisForm.Channel.SetIssues(
                     null,   // this.BiblioStatisForm.stop,
                     this.CurrentRecPath,
                     entities,
                     out errorinfos,
                     out strError);
            else if (strDbType == "comment")
                lRet = this.BiblioStatisForm.Channel.SetComments(
                     null,   // this.BiblioStatisForm.stop,
                     this.CurrentRecPath,
                     entities,
                     out errorinfos,
                     out strError);
            else
            {
                strError = "δ֪�� strDbType '" + strDbType + "'";
                return -1;
            }
            if (lRet == -1)
                return -1;

            // string strWarning = ""; // ������Ϣ

            if (errorinfos == null)
                return 0;

            strError = "";
            for (int i = 0; i < errorinfos.Length; i++)
            {
                if (String.IsNullOrEmpty(errorinfos[i].RefID) == true)
                {
                    strError = "���������ص�EntityInfo�ṹ��RefIDΪ��";
                    return -1;
                }

                // ������Ϣ����
                if (errorinfos[i].ErrorCode == ErrorCodeValue.NoError)
                    continue;

                strError += errorinfos[i].RefID + "���ύ��������з������� -- " + errorinfos[i].ErrorInfo + "\r\n";
            }

            if (String.IsNullOrEmpty(strError) == false)
                return -1;

            return 0;
        }


        /*
        void LineBreak(ref string strText,
            int nLineWidth,
            string strHead,
            string strDelimiters)
        {
            int nStart = 0;
            for (int i=0; ;i++)
            {


            }

        }
         * */

    }

    /// <summary>
    /// ��/����/��/��ע��Ϣ
    /// </summary>
    public class ItemInfo
    {
        /// <summary>
        /// ���ݿ����͡�Ϊ item/order/issue/comment ֮һ 
        /// </summary>
        public string DbType = "item";

        /// <summary>
        /// ��¼·��
        /// </summary>
        public string RecPath = "";

        /// <summary>
        /// ʱ���
        /// </summary>
        public byte[] Timestamp = null;

        /// <summary>
        /// ��������
        /// </summary>
        public BiblioStatis Container = null;

        XmlDocument m_dom = null;

        /// <summary>
        /// ��ȡ����¼���ݵ� XmlDocument ��̬
        /// </summary>
        public XmlDocument Dom
        {
            get
            {
                if (m_dom != null)
                    return m_dom;

                string strXml = this.OldRecord;
                this.m_dom = new XmlDocument();
                this.m_dom.LoadXml(strXml);
                return m_dom;
            }
        }

        string m_strOldRecord = "";

        /// <summary>
        /// ��ȡ���ɼ�¼
        /// </summary>
        public string OldRecord
        {
            get
            {
                if (string.IsNullOrEmpty(this.m_strOldRecord) == false)
                    return m_strOldRecord;

                if (string.IsNullOrEmpty(this.RecPath) == true)
                    throw new Exception("ItemInfo��RecPathΪ�գ��޷����OldRecord");

                string strBarcodeOrRecPath = "@path:" + this.RecPath;
                string strItemXml = "";
                string strOutputItemRecPath = "";
                byte[] item_timestamp = null;
                string strBiblioText = "";
                string strBiblioRecPath = "";
                string strError = "";
                long lRet = 0;
                
                if (this.DbType == "item")
                lRet = this.Container.BiblioStatisForm.Channel.GetItemInfo(
     null,
     strBarcodeOrRecPath,
     "xml",
     out strItemXml,
     out strOutputItemRecPath,
     out item_timestamp,
     "",
     out strBiblioText,
     out strBiblioRecPath,
     out strError);
                else if (this.DbType == "order")
                    lRet = this.Container.BiblioStatisForm.Channel.GetOrderInfo(
         null,
         strBarcodeOrRecPath,
         "xml",
         out strItemXml,
         out strOutputItemRecPath,
         out item_timestamp,
         "",
         out strBiblioText,
         out strBiblioRecPath,
         out strError);
                else if (this.DbType == "issue")
                    lRet = this.Container.BiblioStatisForm.Channel.GetIssueInfo(
         null,
         strBarcodeOrRecPath,
         "xml",
         out strItemXml,
         out strOutputItemRecPath,
         out item_timestamp,
         "",
         out strBiblioText,
         out strBiblioRecPath,
         out strError);
                else if (this.DbType == "comment")
                    lRet = this.Container.BiblioStatisForm.Channel.GetCommentInfo(
         null,
         strBarcodeOrRecPath,
         "xml",
         out strItemXml,
         out strOutputItemRecPath,
         out item_timestamp,
         "",
         out strBiblioText,
         out strBiblioRecPath,
         out strError);
                else
                {
                    throw new Exception("�޷�ʶ��� DbType '" + this.DbType + "'");
                }

                if (lRet == -1 || lRet == 0)
                    throw new Exception(strError);
                this.m_strOldRecord = strItemXml;
                this.Timestamp = item_timestamp;
                return strItemXml;
            }
            set
            {
                this.m_strOldRecord = value;
            }
        }

        /*
        public string RefID
        {
            get
            {
                return DomUtil.GetElementText(this.Dom.DocumentElement, "refID");
            }
        }
         * */
        /// <summary>
        /// �ο� ID
        /// </summary>
        public string RefID = "";

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="strDbType">���ݿ����͡�ֵΪ item order issue comment ֮һ</param>
        public ItemInfo(string strDbType)
        {
            Debug.Assert(strDbType == "item"
                || strDbType == "order"
                || strDbType == "issue"
                || strDbType == "comment",
                "");
            this.DbType = strDbType;
        }
    }

}

