using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using System.Xml;
using System.Drawing;

using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.GUI;

using DigitalPlatform.CirculationClient.localhost;
using System.Collections;
using DigitalPlatform.IO;  // EntityInfo


/*
 * TODO:
 * 1) ��Ҫ����Source��Ա -- ��ʾ������Դ���ֶΡ���������ҲҪ������Ӧ���塣
 * 2) ����IssueCount��Ա
 * 
 * */


namespace dp2Circulation
{
    /// <summary>
    /// ������Ϣ
    /// </summary>
    [Serializable()]
    public class OrderItem : BookItemBase
    {
#if NO
        public ItemDisplayState ItemDisplayState = ItemDisplayState.Normal;
#endif

        // ��index��ע��Ҫ���ֺ�OrderControl�е��к�һ��
        /// <summary>
        /// ListView ��Ŀ�±꣺���
        /// </summary>
        public const int COLUMN_INDEX = 0;
        /// <summary>
        /// ListView ��Ŀ�±꣺������Ϣ
        /// </summary>
        public const int COLUMN_ERRORINFO = 1;
        /// <summary>
        /// ListView ��Ŀ�±꣺��¼״̬
        /// </summary>
        public const int COLUMN_STATE = 2;
        /// <summary>
        /// ListView ��Ŀ�±꣺��Ŀ��
        /// </summary>
        public const int COLUMN_CATALOGNO = 3;
        /// <summary>
        /// ListView ��Ŀ�±꣺����
        /// </summary>
        public const int COLUMN_SELLER = 4;
        /// <summary>
        /// ListView ��Ŀ�±꣺������Դ
        /// </summary>
        public const int COLUMN_SOURCE = 5;
        /// <summary>
        /// ListView ��Ŀ�±꣺����ʱ�䷶Χ
        /// </summary>
        public const int COLUMN_RANGE = 6;
        /// <summary>
        /// ListView ��Ŀ�±꣺����
        /// </summary>
        public const int COLUMN_ISSUECOUNT = 7;
        /// <summary>
        /// ListView ��Ŀ�±꣺������
        /// </summary>
        public const int COLUMN_COPY = 8;
        /// <summary>
        /// ListView ��Ŀ�±꣺�۸�
        /// </summary>
        public const int COLUMN_PRICE = 9;
        /// <summary>
        /// ListView ��Ŀ�±꣺�ܼ۸�
        /// </summary>
        public const int COLUMN_TOTALPRICE = 10;
        /// <summary>
        /// ListView ��Ŀ�±꣺����ʱ��
        /// </summary>
        public const int COLUMN_ORDERTIME = 11;
        /// <summary>
        /// ListView ��Ŀ�±꣺���� ID
        /// </summary>
        public const int COLUMN_ORDERID = 12;
        /// <summary>
        /// ListView ��Ŀ�±꣺�ݲط���ȥ��
        /// </summary>
        public const int COLUMN_DISTRIBUTE = 13;
        /// <summary>
        /// ListView ��Ŀ�±꣺��Ŀ
        /// </summary>
        public const int COLUMN_CLASS = 14;
        /// <summary>
        /// ListView ��Ŀ�±꣺ע��
        /// </summary>
        public const int COLUMN_COMMENT = 15;
        /// <summary>
        /// ListView ��Ŀ�±꣺���κ�
        /// </summary>
        public const int COLUMN_BATCHNO = 16;
        /// <summary>
        /// ListView ��Ŀ�±꣺������ַ
        /// </summary>
        public const int COLUMN_SELLERADDRESS = 17;
        /// <summary>
        /// ListView ��Ŀ�±꣺�ο� ID
        /// </summary>
        public const int COLUMN_REFID = 18;
        /// <summary>
        /// ListView ��Ŀ�±꣺������ʷ��Ϣ
        /// </summary>
        public const int COLUMN_OPERATIONS = 19;
        /// <summary>
        /// ListView ��Ŀ�±꣺������¼·��
        /// </summary>
        public const int COLUMN_RECPATH = 20;

        #region ���ݳ�Ա

#if NO
        public string RefID
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "refID");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "refID", value);
                this.Changed = true;
            }
        }

                /// <summary>
        /// ��������Ŀ��¼id
        /// </summary>
        public string Parent
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "parent");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "parent",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }
#endif

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

        /// <summary>
        /// ���
        /// </summary>
        public string Index
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "index");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "index",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ״̬
        /// </summary>
        public string State
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "state");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "state",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ��Ŀ��
        /// </summary>
        public string CatalogNo
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "catalogNo");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "catalogNo",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ����(����)
        /// </summary>
        public string Seller
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "seller");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "seller",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ������Դ
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
                    "source",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ʱ�䷶Χ
        /// </summary>
        public string Range
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "range");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "range",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ��������
        /// </summary>
        public string IssueCount
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "issueCount");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "issueCount",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ������
        /// </summary>
        public string Copy
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "copy");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "copy",
                    value);
                this.Changed = true; // 2009/3/5
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
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// �ܼ۸�
        /// </summary>
        public string TotalPrice
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "totalPrice");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "totalPrice",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ����ʱ�� RFC1123��ʽ
        /// </summary>
        public string OrderTime
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "orderTime");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "orderTime",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ������
        /// </summary>
        public string OrderID
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "orderID");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "orderID",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }


        /// <summary>
        /// �ݲط���
        /// </summary>
        public string Distribute
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "distribute");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "distribute",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ���
        /// </summary>
        public string Class
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "class");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "class",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ע��
        /// </summary>
        public string Comment
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "comment");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "comment",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ���κ�
        /// </summary>
        public string BatchNo
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "batchNo");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "batchNo",
                    value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ������ַ
        /// </summary>
        public string SellerAddress
        {
            get
            {
                return DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement,
                    "sellerAddress");
            }
            set
            {
                // ע�⣬�����׳��쳣
                DomUtil.SetElementInnerXml(this.RecordDom.DocumentElement,
                    "sellerAddress",
                    value);
            }
        }

        #endregion

#if NO
        /// <summary>
        /// ������¼·��
        /// </summary>
        public string RecPath = "";

        /// <summary>
        /// �Ƿ��޸�
        /// </summary>
        bool m_bChanged = false;

        public string OldRecord = "";

        public string CurrentRecord = "";   // ��Serialize��������������RecordDom����

        /// <summary>
        /// ��¼��dom
        /// </summary>
        [NonSerialized()]
        public XmlDocument RecordDom = new XmlDocument();

        // �ָ���Щ�������л��ĳ�Աֵ
        public void RestoreNonSerialized()
        {
            this.RecordDom = new XmlDocument();

            if (String.IsNullOrEmpty(this.CurrentRecord) == false)
            {
                this.RecordDom.LoadXml(this.CurrentRecord);
                this.CurrentRecord = "";    // ���������
            }
            else
                this.RecordDom.LoadXml("<root />");

        }

        /// <summary>
        /// ʱ���
        /// </summary>
        public byte[] Timestamp = null;

        [NonSerialized()]
        internal ListViewItem ListViewItem = null;

        public string ErrorInfo
        {
            get
            {
                if (this.Error == null)
                    return "";
                return this.Error.ErrorInfo;
            }
        }

        public EntityInfo Error = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        public OrderItem()
        {
            this.RecordDom.LoadXml("<root />");
        }

        public OrderItem Clone()
        {
            OrderItem newObject = new OrderItem();

            newObject.ItemDisplayState = this.ItemDisplayState;

            newObject.RecPath = this.RecPath;
            newObject.m_bChanged = this.m_bChanged;
            newObject.OldRecord = this.OldRecord;


            // ���������ʵ�����
            newObject.CurrentRecord = this.RecordDom.OuterXml;


            newObject.RecordDom = new XmlDocument();
            newObject.RecordDom.LoadXml(this.RecordDom.OuterXml);

            newObject.Timestamp = ByteArray.GetCopy(this.Timestamp);
            newObject.ListViewItem = null;  // this.ListViewItem;
            newObject.Error = null; // this.Error;

            return newObject;
        }

        // ��������
        public int SetData(string strRecPath,
            string strXml,
            byte[] baTimeStamp,
            out string strError)
        {
            strError = "";

            Debug.Assert(this.RecordDom != null);
            // �����׳��쳣
            try
            {
                this.RecordDom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XML����װ�ص�DOMʱ����: " + ex.Message;
                return -1;
            }

            this.OldRecord = strXml;

            this.RecPath = strRecPath;
            this.Timestamp = baTimeStamp;

            return 0;
        }

        // ������������
        public int ResetData(
            string strRecPath,
            string strNewXml,
            byte[] baTimeStamp,
            out string strError)
        {
            strError = "";

            this.RecPath = strRecPath;
            this.Timestamp = baTimeStamp;

            Debug.Assert(this.RecordDom != null);
            try
            {
                this.RecordDom.LoadXml(strNewXml);
            }
            catch (Exception ex)
            {
                strError = "xmlװ�ص�DOMʱ����: " + ex.Message;
                return -1;
            }

            // this.Initial();

            this.Changed = false;   // 2009/3/5
            this.ItemDisplayState = ItemDisplayState.Normal;

            // this.RefreshListView();
            return 0;
        }


        /// <summary>
        /// �������ʺ��ڱ���ļ�¼��Ϣ
        /// </summary>
        /// <param name="strXml"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public int BuildRecord(
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";


            if (this.Parent == "")
            {
                strError = "Parent��Ա��δ����";
                return -1;
            }

            strXml = this.RecordDom.OuterXml;

            return 0;
        }

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                return m_bChanged;
            }
            set
            {
                m_bChanged = value;

                // 2009/3/5
                if ((this.ItemDisplayState == ItemDisplayState.Normal)
                    && this.m_bChanged == true)
                    this.ItemDisplayState = ItemDisplayState.Changed;
                else if ((this.ItemDisplayState == ItemDisplayState.Changed)
                    && this.m_bChanged == false)
                    this.ItemDisplayState = ItemDisplayState.Normal;
            }

        }


        /// <summary>
        /// ����������뵽listview��
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public ListViewItem AddToListView(ListView list)
        {
            ListViewItem item = new ListViewItem(this.Index, 0);

            /*
            item.SubItems.Add(this.ErrorInfo);
            item.SubItems.Add(this.State);
            item.SubItems.Add(this.CatalogNo);  // 2008/8/31
            item.SubItems.Add(this.Seller);

            item.SubItems.Add(this.Source);

            item.SubItems.Add(this.Range);
            item.SubItems.Add(this.IssueCount);
            item.SubItems.Add(this.Copy);
            item.SubItems.Add(this.Price);

            item.SubItems.Add(this.TotalPrice);
            item.SubItems.Add(this.OrderTime);
            item.SubItems.Add(this.OrderID);
            item.SubItems.Add(this.Distribute);
            item.SubItems.Add(this.Class);


            item.SubItems.Add(this.Comment);
            item.SubItems.Add(this.BatchNo);

            item.SubItems.Add(this.SellerAddress);  // 2009/2/13

            item.SubItems.Add(this.RefID);  // 2010/3/15
            item.SubItems.Add(this.RecPath);
             * */
            ListViewUtil.ChangeItemText(item,
                COLUMN_ERRORINFO,
                this.ErrorInfo);
            ListViewUtil.ChangeItemText(item,
                COLUMN_STATE,
                this.State);
            ListViewUtil.ChangeItemText(item,
    COLUMN_CATALOGNO,
    this.CatalogNo);
            ListViewUtil.ChangeItemText(item,
    COLUMN_SELLER,
    this.Seller);
            ListViewUtil.ChangeItemText(item,
    COLUMN_SOURCE,
    this.Source);
            ListViewUtil.ChangeItemText(item,
    COLUMN_RANGE,
    this.Range);
            ListViewUtil.ChangeItemText(item,
    COLUMN_ISSUECOUNT,
    this.IssueCount);
            ListViewUtil.ChangeItemText(item,
    COLUMN_COPY,
    this.Copy);
            ListViewUtil.ChangeItemText(item,
    COLUMN_PRICE,
    this.Price);
            ListViewUtil.ChangeItemText(item,
                COLUMN_TOTALPRICE,
                this.TotalPrice);
            ListViewUtil.ChangeItemText(item,
    COLUMN_ORDERTIME,
    this.OrderTime);
            ListViewUtil.ChangeItemText(item,
    COLUMN_ORDERID,
    this.OrderID);
            ListViewUtil.ChangeItemText(item,
    COLUMN_DISTRIBUTE,
    this.Distribute);
            ListViewUtil.ChangeItemText(item,
    COLUMN_CLASS,
    this.Class);
            ListViewUtil.ChangeItemText(item,
    COLUMN_COMMENT,
    this.Comment);
            ListViewUtil.ChangeItemText(item,
    COLUMN_BATCHNO,
    this.BatchNo);
            ListViewUtil.ChangeItemText(item,
    COLUMN_SELLERADDRESS,
    this.SellerAddress);
            ListViewUtil.ChangeItemText(item,
    COLUMN_REFID,
    this.RefID);
            ListViewUtil.ChangeItemText(item,
    COLUMN_OPERATIONS,
    this.Operations);
            ListViewUtil.ChangeItemText(item,
                COLUMN_RECPATH,
                this.RecPath);

            this.SetItemBackColor(item);

            list.Items.Add(item);

            Debug.Assert(item.ListView != null, "");

            this.ListViewItem = item;

            this.ListViewItem.Tag = this;   // ��OrderItem�������ñ�����ListViewItem������

            return item;
        }

#endif


        // 2013/6/20
        /// <summary>
        /// ���ڴ�ֵ���µ���ʾ����Ŀ
        /// </summary>
        /// <param name="item">ListViewItem���ListView�е�һ��</param>
        public override void SetItemColumns(ListViewItem item)
        {
            ListViewUtil.ChangeItemText(item,
    COLUMN_ERRORINFO,
    this.ErrorInfo);
            ListViewUtil.ChangeItemText(item,
                COLUMN_STATE,
                this.State);
            ListViewUtil.ChangeItemText(item,
    COLUMN_CATALOGNO,
    this.CatalogNo);
            ListViewUtil.ChangeItemText(item,
    COLUMN_SELLER,
    this.Seller);
            ListViewUtil.ChangeItemText(item,
    COLUMN_SOURCE,
    this.Source);
            ListViewUtil.ChangeItemText(item,
    COLUMN_RANGE,
    this.Range);
            ListViewUtil.ChangeItemText(item,
    COLUMN_ISSUECOUNT,
    this.IssueCount);
            ListViewUtil.ChangeItemText(item,
    COLUMN_COPY,
    this.Copy);
            ListViewUtil.ChangeItemText(item,
    COLUMN_PRICE,
    this.Price);
            ListViewUtil.ChangeItemText(item,
                COLUMN_TOTALPRICE,
                this.TotalPrice);
#if NO
            ListViewUtil.ChangeItemText(item,
    COLUMN_ORDERTIME,
    this.OrderTime);
#endif
            // 2015/1/28
            string strOrderTime = "";
            try
            {
                strOrderTime = DateTimeUtil.Rfc1123DateTimeStringToLocal(this.OrderTime, "s");
            }
            catch (Exception ex)
            {
                strOrderTime = "����ʱ���ַ��� '"+this.OrderTime+"' ��ʽ���Ϸ�";
            }
            ListViewUtil.ChangeItemText(item,
COLUMN_ORDERTIME,
strOrderTime);

            ListViewUtil.ChangeItemText(item,
    COLUMN_ORDERID,
    this.OrderID);
            ListViewUtil.ChangeItemText(item,
    COLUMN_DISTRIBUTE,
    this.Distribute);
            ListViewUtil.ChangeItemText(item,
    COLUMN_CLASS,
    this.Class);
            ListViewUtil.ChangeItemText(item,
    COLUMN_COMMENT,
    this.Comment);
            ListViewUtil.ChangeItemText(item,
    COLUMN_BATCHNO,
    this.BatchNo);
            ListViewUtil.ChangeItemText(item,
    COLUMN_SELLERADDRESS,
    this.SellerAddress);
            ListViewUtil.ChangeItemText(item,
    COLUMN_REFID,
    this.RefID);
            ListViewUtil.ChangeItemText(item,
    COLUMN_OPERATIONS,
    this.Operations);
            ListViewUtil.ChangeItemText(item,
                COLUMN_RECPATH,
                this.RecPath);
        }

#if NO
        public void DeleteFromListView()
        {
            Debug.Assert(this.ListViewItem.ListView != null, "");
            ListView list = this.ListViewItem.ListView;

            list.Items.Remove(this.ListViewItem);
        }

        // ˢ�±�����ɫ��ͼ��
        void SetItemBackColor(ListViewItem item)
        {
            if ((this.ItemDisplayState == ItemDisplayState.Normal)
                && this.Changed == true)
            {
                Debug.Assert(false, "ItemDisplayState.Normal״̬��Changed == trueì����");
            }
            else if ((this.ItemDisplayState == ItemDisplayState.Changed)
                && this.Changed == false) // 2009/3/5
            {
                Debug.Assert(false, "ItemDisplayState.Changed״̬��Changed == falseì����");
            }

            if (String.IsNullOrEmpty(this.ErrorInfo) == false)
            {
                // ���������
                item.BackColor = Color.FromArgb(255, 0, 0); // ����ɫ
                item.ForeColor = Color.White;
            }
            else if (this.ItemDisplayState == ItemDisplayState.Normal)
            {
                item.BackColor = SystemColors.Window;
                item.ForeColor = SystemColors.WindowText;
            }
            else if (this.ItemDisplayState == ItemDisplayState.Changed)
            {
                // �޸Ĺ��ľ�����
                item.BackColor = Color.FromArgb(100, 255, 100); // ǳ��ɫ
                item.ForeColor = SystemColors.WindowText;
            }
            else if (this.ItemDisplayState == ItemDisplayState.New)
            {
                // ������
                item.BackColor = Color.FromArgb(255, 255, 100); // ǳ��ɫ
                item.ForeColor = SystemColors.WindowText;
            }
            else if (this.ItemDisplayState == ItemDisplayState.Deleted)
            {
                // ɾ��������
                item.BackColor = Color.FromArgb(255, 150, 150); // ǳ��ɫ
                item.ForeColor = SystemColors.WindowText;
            }
            else // ��������
            {
                item.BackColor = SystemColors.Window;
                item.ForeColor = SystemColors.WindowText;
            }

            item.ImageIndex = Convert.ToInt32(this.ItemDisplayState);

        }

        /// <summary>
        /// ˢ��������ɫ
        /// </summary>
        public void RefreshItemColor()
        {
            if (this.ListViewItem != null)
            {
                this.SetItemBackColor(this.ListViewItem);
            }
        }

        // ˢ�¸������ݺ�ͼ�ꡢ������ɫ
        public void RefreshListView()
        {
            if (this.ListViewItem == null)
                return;

            ListViewItem item = this.ListViewItem;

            ListViewUtil.ChangeItemText(item, 
                COLUMN_INDEX,
                this.Index);
            ListViewUtil.ChangeItemText(item, 
                COLUMN_ERRORINFO,
                this.ErrorInfo);
            ListViewUtil.ChangeItemText(item, 
                COLUMN_STATE,
                this.State);
            ListViewUtil.ChangeItemText(item, 
                COLUMN_CATALOGNO,
                this.CatalogNo);   // 2008/8/31
            ListViewUtil.ChangeItemText(item, 
                COLUMN_SELLER,
                this.Seller);

            ListViewUtil.ChangeItemText(item, 
                COLUMN_SOURCE,
                this.Source);

            ListViewUtil.ChangeItemText(item,
                COLUMN_RANGE,
                this.Range);
            ListViewUtil.ChangeItemText(item, 
                COLUMN_ISSUECOUNT,
                this.IssueCount);
            ListViewUtil.ChangeItemText(item, 
                COLUMN_COPY,
                this.Copy);
            ListViewUtil.ChangeItemText(item,
                COLUMN_PRICE,
                this.Price);

            ListViewUtil.ChangeItemText(item, 
                COLUMN_TOTALPRICE,
                this.TotalPrice);
            ListViewUtil.ChangeItemText(item, 
                COLUMN_ORDERTIME,
                this.OrderTime);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ORDERID,
                this.OrderID);
            ListViewUtil.ChangeItemText(item, 
                COLUMN_DISTRIBUTE,
                this.Distribute);
            ListViewUtil.ChangeItemText(item,
                COLUMN_CLASS,
                this.Class);  // 2008/8/31

            ListViewUtil.ChangeItemText(item, 
                COLUMN_COMMENT,
                this.Comment);
            ListViewUtil.ChangeItemText(item, 
                COLUMN_BATCHNO,
                this.BatchNo);
            ListViewUtil.ChangeItemText(item,
                COLUMN_SELLERADDRESS,
                this.SellerAddress);

            ListViewUtil.ChangeItemText(item, 
                COLUMN_REFID,
                this.RefID);
            ListViewUtil.ChangeItemText(item,
                COLUMN_OPERATIONS,
                this.Operations);
            ListViewUtil.ChangeItemText(item, 
                COLUMN_RECPATH,
                this.RecPath);

            this.SetItemBackColor(item);
        }

        // parameters:
        //      bClearOtherHilight  �Ƿ����������ڵĸ�����ǣ�
        public void HilightListViewItem(bool bClearOtherHilight)
        {
            if (this.ListViewItem == null)
                return;

            int nIndex = -1;
            ListView list = this.ListViewItem.ListView;
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];

                if (item == this.ListViewItem)
                {
                    item.Selected = true;
                    nIndex = i;
                }
                else
                {
                    if (bClearOtherHilight == true)
                    {
                        if (item.Selected == true)
                            item.Selected = false;
                    }
                }
            }

            if (nIndex != -1)
                list.EnsureVisible(nIndex);
        }

#endif
    }

    /// <summary>
    /// ������Ϣ�ļ�������
    /// </summary>
    [Serializable()]
    public class OrderItemCollection : BookItemCollectionBase
    {

#if NO
        // ���ȫ�������Parentֵ�Ƿ��ʺϱ���
        // return:
        //      -1  �д��󣬲��ʺϱ���
        //      0   û�д���
        public int CheckParentIDForSave(out string strError)
        {
            strError = "";
            // ���ÿ�������ParentID
            List<string> ids = this.GetParentIDs();
            for (int i = 0; i < ids.Count; i++)
            {
                string strID = ids[i];
                if (String.IsNullOrEmpty(strID) == true)
                {
                    strError = "���������г����˿յ�ParentIDֵ";
                    return -1;
                }

                if (strID == "?")
                {
                    strError = "���������г�����'?'ʽ��ParentIDֵ";
                    return -1;
                }
            }

            return 0;
        }

        // 2008/11/28
        public List<string> GetParentIDs()
        {
            List<string> results = new List<string>();

            for (int i = 0; i < this.Count; i++)
            {
                OrderItem item = this[i];
                string strParentID = item.Parent;
                if (results.IndexOf(strParentID) == -1)
                    results.Add(strParentID);
            }

            return results;
        }

        // ����ȫ��orderitem�����Parent��
        public void SetParentID(string strParentID)
        {
            for (int i = 0; i < this.Count; i++)
            {
                OrderItem item = this[i];
                if (item.Parent != strParentID) // ����������ν���޸�item.Changed 2009/3/6
                    item.Parent = strParentID;
            }
        }

#endif

        /// <summary>
        /// �Ա�Ŷ�λһ������
        /// </summary>
        /// <param name="strIndex">���</param>
        /// <param name="excludeItems">�ж�����Ҫ�ų�������</param>
        /// <returns>�ҵ������null ��ʾû���ҵ�</returns>
        public OrderItem GetItemByIndex(string strIndex,
            List<OrderItem> excludeItems)
        {
            foreach (OrderItem item in this)
            {
                // ��Ҫ�ų�������
                if (excludeItems != null)
                {
                    if (excludeItems.IndexOf(item) != -1)
                        continue;
                }

                if (item.Index == strIndex)
                    return item;
            }

            return null;
        }

#if NO
        /// <summary>
        /// �Լ�¼·����λһ������
        /// </summary>
        /// <param name="strRecPath"></param>
        /// <returns></returns>
        public OrderItem GetItemByRecPath(string strRecPath)
        {
            for (int i = 0; i < this.Count; i++)
            {
                OrderItem item = this[i];
                if (item.RecPath == strRecPath)
                    return item;
            }

            return null;
        }

        /// <summary>
        /// ��RefID��λһ������
        /// </summary>
        /// <param name="strRefID"></param>
        /// <returns></returns>
        public OrderItem GetItemByRefID(string strRefID,
            List<OrderItem> excludeItems)
        {
            for (int i = 0; i < this.Count; i++)
            {
                OrderItem item = this[i];

                // ��Ҫ�ų�������
                if (excludeItems != null)
                {
                    if (excludeItems.IndexOf(item) != -1)
                        continue;
                }

                if (item.RefID == strRefID)
                    return item;
            }

            return null;
        }

        bool m_bChanged = false;
        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                if (this.m_bChanged == true)
                    return true;

                for (int i = 0; i < this.Count; i++)
                {
                    OrderItem item = this[i];
                    if (item.Changed == true)
                        return true;
                }

                return false;
            }

            set
            {
                // 2012/3/20
                // true��false���Գ�
                if (value == false)
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        OrderItem item = this[i];
                        if (item.Changed != value)
                            item.Changed = value;
                    }
                    this.m_bChanged = value;
                }
                else
                {
                    this.m_bChanged = value;
                }
            }
        }

        // ���ɾ��
        public void MaskDeleteItem(
            bool bRemoveFromList,
            OrderItem orderitem)
        {
            if (orderitem.ItemDisplayState == ItemDisplayState.New)
            {
                PhysicalDeleteItem(orderitem);
                return;
            }


            orderitem.ItemDisplayState = ItemDisplayState.Deleted;
            orderitem.Changed = true;

            // ��listview����ʧ?
            if (bRemoveFromList == true)
                orderitem.DeleteFromListView();
            else
            {
                orderitem.RefreshListView();
            }
        }

        // Undo���ɾ��
        // return:
        //      false   û�б�ҪUndo
        //      true    �Ѿ�Undo
        public bool UndoMaskDeleteItem(OrderItem orderitem)
        {
            if (orderitem.ItemDisplayState != ItemDisplayState.Deleted)
                return false;   // ҪUndo����������Ͳ���Deleted״̬������̸����Undo

            // ��Ϊ��֪���ϴα��ɾ��ǰ�����Ƿ�Ĺ������ȫ���Ĺ�
            orderitem.ItemDisplayState = ItemDisplayState.Changed;
            orderitem.Changed = true;

            // ˢ��
            orderitem.RefreshListView();
            return true;
        }

        // �Ӽ����к��Ӿ���ͬʱɾ��
        public void PhysicalDeleteItem(
            OrderItem orderitem)
        {
            // ��listview����ʧ
            orderitem.DeleteFromListView();

            this.Remove(orderitem);
        }

        public void ClearListViewHilight()
        {
            if (this.Count == 0)
                return;

            ListView list = this[0].ListViewItem.ListView;
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem item = list.Items[i];

                if (item.Selected == true)
                    item.Selected = false;
            }
        }

        public new void Clear()
        {
            for (int i = 0; i < this.Count; i++)
            {
                OrderItem item = this[i];
                item.DeleteFromListView();
            }

            base.Clear();
        }

        // ����������ȫ������listview
        public void AddToListView(ListView list)
        {
            for (int i = 0; i < this.Count; i++)
            {
                OrderItem item = this[i];
                item.AddToListView(list);
            }
        }

#endif

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
            dom.LoadXml("<orders />");

            foreach (OrderItem item in this)
            {
                XmlNode node = dom.CreateElement("dprms", "order", DpNs.dprms);
                dom.DocumentElement.AppendChild(node);

                node.InnerXml = item.RecordDom.DocumentElement.InnerXml;
            }

            strXml = dom.DocumentElement.OuterXml;
            return 0;
        }

        // parameters:
        //       changed_refids  �ۼ��޸Ĺ��� refid ���ձ� ԭ���� --> �µ�
        /// <summary>
        /// ����һ�� XML �ַ������ݣ������������ڵ���������
        /// </summary>
        /// <param name="nodeOrderCollection">XmlNode���󣬱�������ʹ���������� dprms:order Ԫ������������</param>
        /// <param name="list">ListView ���󡣹���õ��������ʾ������</param>
        /// <param name="bRefreshRefID">��������Ĺ����У��Ƿ�Ҫˢ��ÿ������� RefID ��Աֵ</param>
        /// <param name="changed_refids">�ۼ��޸Ĺ��� refid ���ձ� ԭ���� --> �µ�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError ��; 0: �ɹ�</returns>
        public int ImportFromXml(XmlNode nodeOrderCollection,
            ListView list,
            bool bRefreshRefID,
            ref Hashtable changed_refids,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (nodeOrderCollection == null)
                return 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);

            XmlNodeList nodes = nodeOrderCollection.SelectNodes("dprms:order", nsmgr);
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                OrderItem order_item = new OrderItem();
                nRet = order_item.SetData("",
                    node.OuterXml,
                    null,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (bRefreshRefID == true)
                {
                    string strOldRefID = order_item.RefID;
                    order_item.RefID = Guid.NewGuid().ToString();

                    changed_refids[strOldRefID] = order_item.RefID;
                }

                this.Add(order_item);
                order_item.ItemDisplayState = ItemDisplayState.New;
                order_item.AddToListView(list);

                order_item.Changed = true;
            }

            return 0;
        }
    }
}
