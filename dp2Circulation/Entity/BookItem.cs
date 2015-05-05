using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using System.Xml;
using System.Drawing;

using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.GUI;

using DigitalPlatform.CirculationClient.localhost;  // EntityInfo

namespace dp2Circulation
{
    /// <summary>
    /// ����Ϣ
    /// </summary>
    [Serializable()]
    public class BookItem : BookItemBase
    {
        // ��index��ע��Ҫ���ֺ�EntityControl�е��к�һ��

        /// <summary>
        /// ListView ��Ŀ�±꣺�������
        /// </summary>
        public const int COLUMN_BARCODE = 0;
        /// <summary>
        /// ListView ��Ŀ�±꣺������Ϣ
        /// </summary>
        public const int COLUMN_ERRORINFO = 1;
        /// <summary>
        /// ListView ��Ŀ�±꣺��¼״̬
        /// </summary>
        public const int COLUMN_STATE = 2;
        /// <summary>
        /// ListView ��Ŀ�±꣺����ʱ��
        /// </summary>
        public const int COLUMN_PUBLISHTIME = 3;
        /// <summary>
        /// ListView ��Ŀ�±꣺�ݲصص�
        /// </summary>
        public const int COLUMN_LOCATION = 4;
        /// <summary>
        /// ListView ��Ŀ�±꣺����
        /// </summary>
        public const int COLUMN_SELLER = 5;
        /// <summary>
        /// ListView ��Ŀ�±꣺������Դ
        /// </summary>
        public const int COLUMN_SOURCE = 6;
        /// <summary>
        /// ListView ��Ŀ�±꣺�۸�
        /// </summary>
        public const int COLUMN_PRICE = 7;
        /// <summary>
        /// ListView ��Ŀ�±꣺�����Ϣ
        /// </summary>
        public const int COLUMN_VOLUMN = 8;
        /// <summary>
        /// ListView ��Ŀ�±꣺��ȡ��
        /// </summary>
        public const int COLUMN_ACCESSNO = 9;
        /// <summary>
        /// ListView ��Ŀ�±꣺ͼ������
        /// </summary>
        public const int COLUMN_BOOKTYPE = 10;
        /// <summary>
        /// ListView ��Ŀ�±꣺��¼��
        /// </summary>
        public const int COLUMN_REGISTERNO = 11;
        /// <summary>
        /// ListView ��Ŀ�±꣺ע��
        /// </summary>
        public const int COLUMN_COMMENT = 12;
        /// <summary>
        /// ListView ��Ŀ�±꣺�ϲ�ע��
        /// </summary>
        public const int COLUMN_MERGECOMMENT = 13;
        /// <summary>
        /// ListView ��Ŀ�±꣺���κ�
        /// </summary>
        public const int COLUMN_BATCHNO = 14;
        /// <summary>
        /// ListView ��Ŀ�±꣺������
        /// </summary>
        public const int COLUMN_BORROWER = 15;
        /// <summary>
        /// ListView ��Ŀ�±꣺��������
        /// </summary>
        public const int COLUMN_BORROWDATE = 16;
        /// <summary>
        /// ListView ��Ŀ�±꣺��������
        /// </summary>
        public const int COLUMN_BORROWPERIOD = 17;

        /// <summary>
        /// ListView ��Ŀ�±꣺�����
        /// </summary>
        public const int COLUMN_INTACT = 18;
        /// <summary>
        /// ListView ��Ŀ�±꣺װ������
        /// </summary>
        public const int COLUMN_BINDINGCOST = 19;
        /// <summary>
        /// ListView ��Ŀ�±꣺װ����Ϣ
        /// </summary>
        public const int COLUMN_BINDING = 20;
        /// <summary>
        /// ListView ��Ŀ�±꣺������ʷ��Ϣ
        /// </summary>
        public const int COLUMN_OPERATIONS = 21;

        /// <summary>
        /// ListView ��Ŀ�±꣺���¼·��
        /// </summary>
        public const int COLUMN_RECPATH = 22;
        /// <summary>
        /// ListView ��Ŀ�±꣺�ο� ID
        /// </summary>
        public const int COLUMN_REFID = 23;

        /// <summary>
        /// ���ݵ�ǰ�����¡��һ���¶���
        /// </summary>
        /// <returns>�¶���</returns>
        public BookItem Clone()
        {
            BookItem item = new BookItem();
            this.CopyTo(item);
            return item;
        }

        /// <summary>
        /// ����ָ������Ŀ�������ֶ�����
        /// </summary>
        /// <param name="nCol">��Ŀ��</param>
        /// <param name="strText">Ҫ���õ�����</param>
        public void SetColumnText(int nCol, string strText)
        {
            if (nCol == COLUMN_BARCODE)
                this.Barcode = strText;
            else if (nCol == COLUMN_ERRORINFO)
                this.ErrorInfo = strText;

            else if (nCol == COLUMN_STATE)
                this.State = strText;
            else if (nCol == COLUMN_PUBLISHTIME)
                this.PublishTime = strText;
            else if (nCol == COLUMN_LOCATION)
                this.Location = strText;
            else if (nCol == COLUMN_SELLER)
                this.Seller = strText;
            else if (nCol == COLUMN_SOURCE)
                this.Source = strText;
            else if (nCol == COLUMN_PRICE)
                this.Price = strText;
            else if (nCol == COLUMN_VOLUMN)
                this.Volume = strText;
            else if (nCol == COLUMN_ACCESSNO)
                this.AccessNo = strText;
            else if (nCol == COLUMN_BOOKTYPE)
                this.BookType = strText;
            else if (nCol == COLUMN_REGISTERNO)
                this.RegisterNo = strText;
            else if (nCol == COLUMN_COMMENT)
                this.Comment = strText;
            else if (nCol == COLUMN_MERGECOMMENT)
                this.MergeComment = strText;
            else if (nCol == COLUMN_BATCHNO)
                this.BatchNo = strText;
            else if (nCol == COLUMN_BORROWER)
                this.Borrower = strText;
            else if (nCol == COLUMN_BORROWDATE)
                this.BorrowDate = strText;
            else if (nCol == COLUMN_BORROWPERIOD)
                this.BorrowPeriod = strText;

            else if (nCol == COLUMN_INTACT)
                this.Intact = strText;
            else if (nCol == COLUMN_BINDINGCOST)
                this.BindingCost = strText;
            else if (nCol == COLUMN_BINDING)
                this.Binding = strText;
            else if (nCol == COLUMN_OPERATIONS)
                this.Operations = strText;

            else if (nCol == COLUMN_RECPATH)
                this.RecPath = strText;
            else if (nCol == COLUMN_REFID)
                this.RefID = strText;
            else
                throw new Exception("δ֪���к� " + nCol.ToString());

        }

        #region ���ݳ�Ա

        /*
        string m_strTempRefID = "";

        public string TempRefID
        {

            get
            {
                // TODO: ���ˢ����ʾ?
                if (String.IsNullOrEmpty(this.m_strTempRefID) == true)
                    this.m_strTempRefID = Guid.NewGuid().ToString();

                return this.m_strTempRefID;
            }
        }*/


        /// <summary>
        ///  �������
        /// </summary>
        public string Barcode 
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "barcode");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "barcode", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// ��¼�� (2006/9/25 ����)
        /// </summary>
        public string RegisterNo
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "registerNo");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "registerNo", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// ��״̬
        /// </summary>
        public string State 
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "state");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "state", value);
                this.Changed = true; // 2009/3/5 new add
            }            
        }

        /// <summary>
        /// ����ʱ�� 2007/10/24 new add
        /// </summary>
        public string PublishTime
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "publishTime");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "publishTime", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// ����(����) 2007/10/24 new add
        /// </summary>
        public string Seller
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "seller");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "seller", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// �ɹ�������Դ 2008/2/15 new add
        /// </summary>
        public string Source
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "source");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "source", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }



        /// <summary>
        /// �ݲصص�
        /// </summary>
        public string Location
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "location");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "location", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// ��۸�
        /// </summary>
        public string Price 
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "price");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "price", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// ������
        /// </summary>
        public string BookType 
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "bookType");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "bookType", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// ע��
        /// </summary>
        public string Comment 
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "comment");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "comment", value);
                this.Changed = true; // 2009/3/5 new add
            }            
        }

        /// <summary>
        /// �ϲ�ע�� (2006/9/25 ����)
        /// </summary>
        public string MergeComment
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "mergeComment");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "mergeComment", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// ���κ� (2006/9/29 ����)
        /// </summary>
        public string BatchNo
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "batchNo");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "batchNo", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// ��� (2007/10/19 ����)
        /// </summary>
        public string Volume
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "volume");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "volume", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// ��� (2008/12/12 ����)
        /// </summary>
        public string AccessNo
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "accessNo");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "accessNo", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// ����������
        /// </summary>
        public string Borrower 
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "borrower");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "borrower", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// ���������
        /// </summary>
        public string BorrowDate
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "borrowDate");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "borrowDate", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// ��������
        /// </summary>
        public string BorrowPeriod
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "borrowPeriod");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "borrowPeriod", value);
                this.Changed = true; // 2009/3/5 new add
            }
        }

        /// <summary>
        /// �����
        /// </summary>
        public string Intact
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "intact");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "intact", value);
                this.Changed = true;
            }
        }

        /// <summary>
        /// װ����
        /// </summary>
        public string BindingCost
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "bindingCost");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "bindingCost", value);
                this.Changed = true;
            }
        }

        /// <summary>
        /// װ����Ϣ
        /// </summary>
        public string Binding
        {
            get
            {
                return DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement,
                    "binding");
            }
            set
            {
                // ע�⣬�����׳��쳣
                DomUtil.SetElementInnerXml(this.RecordDom.DocumentElement,
                    "binding",
                    value);
                this.Changed = true;
            }
        }

        /// <summary>
        /// ����
        /// </summary>
        public string Operations
        {
            get
            {
                return DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement,
                    "operations");
            }
            set
            {
                // ע�⣬�����׳��쳣
                DomUtil.SetElementInnerXml(this.RecordDom.DocumentElement,
                    "operations",
                    value);
                this.Changed = true;
            }
        }

        #endregion


        // parameters:
        // return:
        //      -1  ����
        //      0   û�з����޸�
        //      1   �������޸�
        /// <summary>
        /// ���� binding Ԫ����� item Ԫ���е� refID ����ֵ�ַ���
        /// </summary>
        /// <param name="item_refid_change_table">�ο� ID ���ձ�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û�з����޸�; 1: �������޸�</returns>
        public int ReplaceBindingItemRefID(Hashtable item_refid_change_table,
            out string strError)
        {
            strError = "";
            // int nRet = 0;

            string strBinding = this.Binding;
            if (String.IsNullOrEmpty(strBinding) == true)
                return 0;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<binding/>");
            try
            {
                dom.DocumentElement.InnerXml = strBinding;
            }
            catch (Exception ex)
            {
                strError = "load inner xml error: " + ex.Message;
                return -1;
            }

            bool bChanged = false;
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("item");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strRefID = DomUtil.GetAttr(node, "refID");
                if (String.IsNullOrEmpty(strRefID) == false)
                {
                    if (item_refid_change_table.Contains(strRefID) == true)
                    {
                        DomUtil.SetAttr(node, "refID", (string)item_refid_change_table[strRefID]);
                        bChanged = true;
                    }
                }
            }

            if (bChanged == true)
            {
                this.Binding = dom.DocumentElement.InnerXml;
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// ���ڴ�ֵ���µ���ʾ����Ŀ
        /// </summary>
        /// <param name="item">ListViewItem���ListView�е�һ��</param>
        public override void SetItemColumns(ListViewItem item)
        {
            ListViewUtil.ChangeItemText(item,
    COLUMN_BARCODE,
    this.Barcode);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ERRORINFO,
                this.ErrorInfo);
            ListViewUtil.ChangeItemText(item,
                COLUMN_STATE,
                this.State);
            ListViewUtil.ChangeItemText(item,
                COLUMN_PUBLISHTIME,
                this.PublishTime);
            ListViewUtil.ChangeItemText(item,
                COLUMN_LOCATION,
                this.Location);

            ListViewUtil.ChangeItemText(item,
                COLUMN_SELLER,
                this.Seller);
            ListViewUtil.ChangeItemText(item,
                COLUMN_SOURCE,
                this.Source);

            ListViewUtil.ChangeItemText(item,
                COLUMN_PRICE,
                this.Price);

            ListViewUtil.ChangeItemText(item,
                COLUMN_VOLUMN,
                this.Volume);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ACCESSNO,
                this.AccessNo);


            ListViewUtil.ChangeItemText(item,
                COLUMN_BOOKTYPE,
                this.BookType);
            ListViewUtil.ChangeItemText(item,
                COLUMN_REGISTERNO,
                this.RegisterNo);

            ListViewUtil.ChangeItemText(item,
                COLUMN_COMMENT,
                this.Comment);
            ListViewUtil.ChangeItemText(item,
                COLUMN_MERGECOMMENT,
                this.MergeComment);
            ListViewUtil.ChangeItemText(item,
                COLUMN_BATCHNO,
                this.BatchNo);

            ListViewUtil.ChangeItemText(item,
                COLUMN_BORROWER,
                this.Borrower);
            ListViewUtil.ChangeItemText(item,
                COLUMN_BORROWDATE,
                this.BorrowDate);
            ListViewUtil.ChangeItemText(item,
                COLUMN_BORROWPERIOD,
                this.BorrowPeriod);

            ListViewUtil.ChangeItemText(item,
                COLUMN_INTACT,
                this.Intact);
            ListViewUtil.ChangeItemText(item,
                COLUMN_BINDINGCOST,
                this.BindingCost);
            ListViewUtil.ChangeItemText(item,
                COLUMN_BINDING,
                this.Binding);
            ListViewUtil.ChangeItemText(item,
                COLUMN_OPERATIONS,
                this.Operations);

            ListViewUtil.ChangeItemText(item,
                COLUMN_RECPATH,
                this.RecPath);
            ListViewUtil.ChangeItemText(item,
                COLUMN_REFID,
                this.RefID);
        }


    }

    /// <summary>
    /// ����Ϣ�ļ�������
    /// </summary>
    [Serializable()]
    public class BookItemCollection : BookItemCollectionBase
    {
        /// <summary>
        /// ������ȡ����Ϣ����
        /// </summary>
        /// <returns>CallNumberItem�����</returns>
        public List<CallNumberItem> GetCallNumberItems()
        {
            List<CallNumberItem> results = new List<CallNumberItem>();
            foreach (BookItem book_item in this)
            {
                CallNumberItem item = new CallNumberItem();
                item.RecPath = book_item.RecPath;
                item.CallNumber = DomUtil.GetElementText(book_item.RecordDom.DocumentElement, "accessNo");
                item.Location = DomUtil.GetElementText(book_item.RecordDom.DocumentElement, "location");
                item.Barcode = DomUtil.GetElementText(book_item.RecordDom.DocumentElement, "barcode");

                results.Add(item);
            }

            return results;
        }

        /// <summary>
        /// �Բ�����Ŷ�λһ������
        /// </summary>
        /// <param name="strBarcode">�������</param>
        /// <returns>����</returns>
        public BookItem GetItemByBarcode(string strBarcode)
        {
            foreach (BookItem item in this)
            {
                if (item.Barcode == strBarcode)
                    return item;
            }

            return null;
        }

        /// <summary>
        /// �Ե�¼�Ŷ�λһ������
        /// </summary>
        /// <param name="strRegisterNo">��¼��</param>
        /// <returns>����</returns>
        public BookItem GetItemByRegisterNo(string strRegisterNo)
        {
            foreach (BookItem item in this)
            {
                if (item.RegisterNo == strRegisterNo)
                    return item;
            }

            return null;
        }

        // 2008/11/4 new add
        /// <summary>
        /// ѡ��(����)ƥ��ָ�����κŵ���Щ��
        /// </summary>
        /// <param name="strBatchNo">���κ�</param>
        /// <param name="bClearOthersHilight">ͬʱ�����������ļ���״̬</param>
        public void SelectItemsByBatchNo(string strBatchNo,
            bool bClearOthersHilight)
        {
            if (this.Count == 0)
                return;

            ListView list = this[0].ListViewItem.ListView;
            int first_hilight_item_index = -1;
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem listview_item = list.Items[i];

                BookItem book_item = (BookItem)listview_item.Tag;

                Debug.Assert(book_item != null, "");

                if (book_item.BatchNo == strBatchNo)
                {
                    listview_item.Selected = true;
                    if (first_hilight_item_index == -1)
                        first_hilight_item_index = i;
                }
                else
                {
                    if (bClearOthersHilight == true)
                        listview_item.Selected = false;
                }
            }

            // ������Ұ��Χ
            if (first_hilight_item_index != -1)
                list.EnsureVisible(first_hilight_item_index);
        }

        /// <summary>
        /// �������е�ȫ��������Ϣ���Ϊһ�������� XML ��ʽ�ַ���
        /// </summary>
        /// <param name="strXml">XML �ַ���</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError ��; 0: �ɹ�</returns>
        public int BuildXml(
    out string strXml,
    out string strError)
        {
            strError = "";
            strXml = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<items />");

            foreach (BookItem item in this)
            {
                XmlNode node = dom.CreateElement("dprms", "item", DpNs.dprms);
                dom.DocumentElement.AppendChild(node);

                node.InnerXml = item.RecordDom.DocumentElement.InnerXml;
            }

            strXml = dom.DocumentElement.OuterXml;
            return 0;
        }

        // parameters:
        //      refid_change_table  �޸Ĺ���refid��keyΪ��ֵ��valueΪ��ֵ
        /// <summary>
        /// ����һ�� XML �ַ������ݣ������������ڵ���������
        /// </summary>
        /// <param name="nodeItemCollection">XmlNode���󣬱�������ʹ���������� dprms:item Ԫ������������</param>
        /// <param name="list">ListView ���󡣹���õ��������ʾ������</param>
        /// <param name="bRefreshRefID">��������Ĺ����У��Ƿ�Ҫˢ��ÿ������� RefID ��Աֵ</param>
        /// <param name="refid_change_table">�����޸Ĺ���refid��keyΪ��ֵ��valueΪ��ֵ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError ��; 0: �ɹ�</returns>
        public int ImportFromXml(XmlNode nodeItemCollection,
            ListView list,
            bool bRefreshRefID,
            out Hashtable refid_change_table,
            out string strError)
        {
            strError = "";
            refid_change_table = new Hashtable();
            int nRet = 0;

            if (nodeItemCollection == null)
                return 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);

            XmlNodeList nodes = nodeItemCollection.SelectNodes("dprms:item", nsmgr);
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                BookItem book_item = new BookItem();
                nRet = book_item.SetData("",
                    node.OuterXml,
                    null,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (bRefreshRefID == true)
                {
                    string strOldRefID = book_item.RefID;
                    book_item.RefID = Guid.NewGuid().ToString();
                    if (String.IsNullOrEmpty(strOldRefID) == false)
                    {
                        refid_change_table[strOldRefID] = book_item.RefID;
                    }
                }

                this.Add(book_item);
                book_item.ItemDisplayState = ItemDisplayState.New;
                book_item.AddToListView(list);

                book_item.Changed = true;
            }

            // ����<binding>Ԫ����<item>Ԫ�ص�refID����ֵ
            if (bRefreshRefID == true
                && refid_change_table.Count > 0)
            {
                foreach (BookItem item in this)
                {
                    nRet = item.ReplaceBindingItemRefID(refid_change_table,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }
            }

            return 0;
        }
    }

    /// <summary>
    /// �������ʾ״̬
    /// </summary>
    public enum ItemDisplayState
    {
        /// <summary>
        /// ��ͨ
        /// </summary>
        Normal = 0,
        /// <summary>
        /// ����
        /// </summary>
        New = 1,
        /// <summary>
        /// �������޸�
        /// </summary>
        Changed = 2,
        /// <summary>
        /// ��ɾ��
        /// </summary>
        Deleted = 3,
    }
}
