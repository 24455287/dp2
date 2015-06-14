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

using DigitalPlatform.CirculationClient.localhost;  // IssueInfo
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// ����Ϣ
    /// ��Ҫ���� IssueControl �У���ʾһ���ڼ�¼
    /// </summary>
    [Serializable()]
    public class IssueItem : BookItemBase
    {
#if NO
        public ItemDisplayState ItemDisplayState = ItemDisplayState.Normal;

#endif

        // ��index��ע��Ҫ���ֺ�IssueControl�е��к�һ��

        /// <summary>
        /// ListView ��Ŀ�±꣺��������
        /// </summary>
        public const int COLUMN_PUBLISHTIME = 0;
        /// <summary>
        /// ListView ��Ŀ�±꣺������Ϣ
        /// </summary>
        public const int COLUMN_ERRORINFO = 1;
        /// <summary>
        /// ListView ��Ŀ�±꣺��¼״̬
        /// </summary>
        public const int COLUMN_STATE = 2;
        /// <summary>
        /// ListView ��Ŀ�±꣺�ں�
        /// </summary>
        public const int COLUMN_ISSUE = 3;
        /// <summary>
        /// ListView ��Ŀ�±꣺���ں�
        /// </summary>
        public const int COLUMN_ZONG = 4;
        /// <summary>
        /// ListView ��Ŀ�±꣺���
        /// </summary>
        public const int COLUMN_VOLUME = 5;
        /// <summary>
        /// ListView ��Ŀ�±꣺������Ϣ
        /// </summary>
        public const int COLUMN_ORDERINFO = 6;
        /// <summary>
        /// ListView ��Ŀ�±꣺ע��
        /// </summary>
        public const int COLUMN_COMMENT = 7;
        /// <summary>
        /// ListView ��Ŀ�±꣺���κ�
        /// </summary>
        public const int COLUMN_BATCHNO = 8;
        /// <summary>
        /// ListView ��Ŀ�±꣺�ο� ID
        /// </summary>
        public const int COLUMN_REFID = 9;
        /// <summary>
        /// ListView ��Ŀ�±꣺������ʷ��Ϣ
        /// </summary>
        public const int COLUMN_OPERATIONS = 10;
        /// <summary>
        /// ListView ��Ŀ�±꣺�ڼ�¼·��
        /// </summary>
        public const int COLUMN_RECPATH = 11;

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
                    "parent", value);
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
        /// ����ʱ��
        /// </summary>
        public string PublishTime
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "publishTime");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, 
                    "publishTime", value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ��״̬
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
                    "state", value);
                this.Changed = true; // 2009/3/5
            }
        }



        /// <summary>
        /// ������Ϣ
        /// </summary>
        public string OrderInfo
        {
            get
            {
                /*
                XmlNode node = this.RecordDom.DocumentElement.SelectSingleNode("orderInfo");
                if (node == null)
                    return "";

                return node.InnerXml;
                 * */
                return DomUtil.GetElementInnerXml(this.RecordDom.DocumentElement,
                    "orderInfo");
            }
            set
            {
                /*
                XmlNode node = this.RecordDom.DocumentElement.SelectSingleNode("orderInfo");
                if (node == null)
                {
                    node = this.RecordDom.CreateElement("orderInfo");
                    this.RecordDom.DocumentElement.AppendChild(node);
                }

                // ע�⣬�����׳��쳣
                node.InnerXml = value;
                 * */

                // ע�⣬�����׳��쳣
                DomUtil.SetElementInnerXml(this.RecordDom.DocumentElement, 
                    "orderInfo",
                    value);
                this.Changed = true; // 2009/11/24
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
                    "comment", value);
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
                    "batchNo", value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ���
        /// </summary>
        public string Volume
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "volume");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, 
                    "volume", value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// ���ں�
        /// </summary>
        public string Zong
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "zong");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, 
                    "zong", value);
                this.Changed = true; // 2009/3/5
            }
        }

        /// <summary>
        /// �ں�
        /// </summary>
        public string Issue
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, 
                    "issue");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "issue", value);
                this.Changed = true; // 2009/3/5
            }
        }

        #endregion

#if NO
        /// <summary>
        ///  �ڼ�¼·��
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
        public IssueItem()
        {
            this.RecordDom.LoadXml("<root />");
        }


        public IssueItem Clone()
        {
            IssueItem newObject = new IssueItem();

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

#endif

        // 2013/6/18
        // 
        // return:
        //      -1  ����
        //      0   û�з����滻�޸�
        //      >0  ���޸��˶��ٸ�<refID>Ԫ������
        /// <summary>
        /// ���� orderInfo Ԫ����� refID Ԫ���е� �ο� ID �ַ���
        /// </summary>
        /// <param name="order_refid_change_table">�ο� ID ���ձ�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1  ����; 0   û�з����滻�޸�; >0  ���޸��˶��ٸ� refID Ԫ������</returns>
        public int ReplaceOrderInfoRefID(Hashtable order_refid_change_table,
            out string strError)
        {
            strError = "";
            // int nRet = 0;

            string strOrderInfo = this.OrderInfo;
            if (String.IsNullOrEmpty(strOrderInfo) == true)
                return 0;


            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<orderInfo/>");
            try
            {
                dom.DocumentElement.InnerXml = strOrderInfo;
            }
            catch (Exception ex)
            {
                strError = "load inner xml error: " + ex.Message;
                return -1;
            }

            int nChangedCount = 0;

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("*/refID");
            foreach (XmlNode node in nodes)
            {
                string strOldValue = node.InnerText;
                if (String.IsNullOrEmpty(strOldValue) == true)
                    continue;

                string strNewValue = (string)order_refid_change_table[strOldValue];
                if (string.IsNullOrEmpty(strNewValue) == false)
                {
                    node.InnerText = strNewValue;
                    nChangedCount++;
                }
            }

            if (nChangedCount > 0)
                this.OrderInfo = dom.DocumentElement.InnerXml;

            return nChangedCount;
        }

        // 
        // return:
        //      -1  ����
        //      0   û�з����滻�޸�
        //      >0  ���޸��˶��ٸ�<distribute>Ԫ������
        /// <summary>
        /// ���� orderInfo Ԫ����� distribute Ԫ���е� refid �ַ���
        /// </summary>
        /// <param name="item_refid_change_table">�ο� ID ���ձ�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1  ����; 0   û�з����滻�޸�; >0  ���޸��˶��ٸ� distribute Ԫ������</returns>
        public int ReplaceOrderInfoItemRefID(Hashtable item_refid_change_table,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            string strOrderInfo = this.OrderInfo;
            if (String.IsNullOrEmpty(strOrderInfo) == true)
                return 0;


            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<orderInfo/>");
            try
            {
                dom.DocumentElement.InnerXml = strOrderInfo;
            }
            catch (Exception ex)
            {
                strError = "load inner xml error: " + ex.Message;
                return -1;
            }

            int nChangedCount = 0;
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("*/distribute");
            for (int i = 0; i < nodes.Count; i++)
            {
                string strDistribute = nodes[i].InnerText;
                if (String.IsNullOrEmpty(strDistribute) == true)
                    continue;

                LocationCollection locations = new LocationCollection();
                nRet = locations.Build(strDistribute,
                    out strError);
                if (nRet == -1)
                    return -1;

                bool bChanged = false;

                for (int j = 0; j < locations.Count; j++)
                {
                    Location location = locations[j];
                    if (item_refid_change_table.Contains(location.RefID) == true)
                    {
                        location.RefID = (string)item_refid_change_table[location.RefID];
                        bChanged = true;
                    }
                }

                if (bChanged == true)
                {
                    nodes[i].InnerText = locations.ToString(true);
                    nChangedCount++;
                }

            }

            if (nChangedCount > 0)
                this.OrderInfo = dom.DocumentElement.InnerXml;

            return nChangedCount;
        }

#if NO
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
            ListViewItem item = new ListViewItem(this.PublishTime, 0);

            /*
            item.SubItems.Add(this.ErrorInfo);
            item.SubItems.Add(this.State);
            item.SubItems.Add(this.Issue);
            item.SubItems.Add(this.Zong);
            item.SubItems.Add(this.Volume);
            item.SubItems.Add(this.OrderInfo);

            item.SubItems.Add(this.Comment);
            item.SubItems.Add(this.BatchNo);

            item.SubItems.Add(this.RefID);  // 2010/2/27

            item.SubItems.Add(this.RecPath);
             * */
            ListViewUtil.ChangeItemText(item,
                COLUMN_ERRORINFO,
                this.ErrorInfo);
            ListViewUtil.ChangeItemText(item,
                COLUMN_STATE,
                this.State);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ISSUE,
                this.Issue);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ZONG,
                this.Zong);
            ListViewUtil.ChangeItemText(item,
                COLUMN_VOLUME,
                this.Volume);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ORDERINFO,
                this.OrderInfo);
            ListViewUtil.ChangeItemText(item,
                COLUMN_COMMENT,
                this.Comment);
            ListViewUtil.ChangeItemText(item,
                COLUMN_BATCHNO,
                this.BatchNo);
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

            this.ListViewItem = item;

            this.ListViewItem.Tag = this;   // ��IssueItem�������ñ�����ListViewItem������

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
                COLUMN_PUBLISHTIME,
                this.PublishTime);  // 2014/6/5
            ListViewUtil.ChangeItemText(item,
                COLUMN_ERRORINFO,
                this.ErrorInfo);
            ListViewUtil.ChangeItemText(item,
                COLUMN_STATE,
                this.State);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ISSUE,
                this.Issue);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ZONG,
                this.Zong);
            ListViewUtil.ChangeItemText(item,
                COLUMN_VOLUME,
                this.Volume);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ORDERINFO,
                this.OrderInfo);
            ListViewUtil.ChangeItemText(item,
                COLUMN_COMMENT,
                this.Comment);
            ListViewUtil.ChangeItemText(item,
                COLUMN_BATCHNO,
                this.BatchNo);
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
                COLUMN_PUBLISHTIME,
                this.PublishTime);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ERRORINFO,
                this.ErrorInfo);
            ListViewUtil.ChangeItemText(item,
                COLUMN_STATE,
                this.State);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ISSUE,
                this.Issue);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ZONG,
                this.Zong);
            ListViewUtil.ChangeItemText(item,
                COLUMN_VOLUME,
                this.Volume);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ORDERINFO,
                this.OrderInfo);


            ListViewUtil.ChangeItemText(item,
                COLUMN_COMMENT,
                this.Comment);
            ListViewUtil.ChangeItemText(item, 
                COLUMN_BATCHNO,
                this.BatchNo);

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
    /// ����Ϣ�ļ�������
    /// </summary>
    [Serializable()]
    public class IssueItemCollection : BookItemCollectionBase
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
                    strError = "�������г����˿յ�ParentIDֵ";
                    return -1;
                }

                if (strID == "?")
                {
                    strError = "�������г�����'?'ʽ��ParentIDֵ";
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
                IssueItem item = this[i];
                string strParentID = item.Parent;
                if (results.IndexOf(strParentID) == -1)
                    results.Add(strParentID);
            }

            return results;
        }


        // ����ȫ��isueitem�����Parent��
        public void SetParentID(string strParentID)
        {
            for (int i = 0; i < this.Count; i++)
            {
                IssueItem item = this[i];
                if (item.Parent != strParentID) // ����������ν���޸�item.Changed 2009/3/6
                    item.Parent = strParentID;
            }
        }

#endif

        /// <summary>
        /// �Գ���ʱ�䶨λһ������
        /// </summary>
        /// <param name="strPublishTime">����ʱ��</param>
        /// <param name="excludeItems">�ж�����Ҫ�ų�������</param>
        /// <returns>�ҵ������null ��ʾû���ҵ�</returns>
        public IssueItem GetItemByPublishTime(string strPublishTime,
            List<IssueItem> excludeItems)
        {
            foreach (IssueItem item in this)
            {
                // ��Ҫ�ų�������
                if (excludeItems != null)
                {
                    if (excludeItems.IndexOf(item) != -1)
                        continue;
                }

                if (item.PublishTime == strPublishTime)
                    return item;
            }

            return null;
        }

#if NO
        /// <summary>
        /// �Լ�¼·����λһ������
        /// </summary>
        /// <param name="strRegisterNo"></param>
        /// <returns></returns>
        public IssueItem GetItemByRecPath(string strRecPath)
        {
            for (int i = 0; i < this.Count; i++)
            {
                IssueItem item = this[i];
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
        public IssueItem GetItemByRefID(string strRefID,
            List<IssueItem> excludeItems)
        {
            for (int i = 0; i < this.Count; i++)
            {
                IssueItem item = this[i];

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
                    IssueItem item = this[i];
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
                        IssueItem item = this[i];
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
            IssueItem issueitem)
        {
            if (issueitem.ItemDisplayState == ItemDisplayState.New)
            {
                PhysicalDeleteItem(issueitem);
                return;
            }


            issueitem.ItemDisplayState = ItemDisplayState.Deleted;
            issueitem.Changed = true;

            // ��listview����ʧ?
            if (bRemoveFromList == true)
                issueitem.DeleteFromListView();
            else
            {
                issueitem.RefreshListView();
            }
        }

        // Undo���ɾ��
        // return:
        //      false   û�б�ҪUndo
        //      true    �Ѿ�Undo
        public bool UndoMaskDeleteItem(IssueItem issueitem)
        {
            if (issueitem.ItemDisplayState != ItemDisplayState.Deleted)
                return false;   // ҪUndo����������Ͳ���Deleted״̬������̸����Undo

            // ��Ϊ��֪���ϴα��ɾ��ǰ�����Ƿ�Ĺ������ȫ���Ĺ�
            issueitem.ItemDisplayState = ItemDisplayState.Changed;
            issueitem.Changed = true;

            // ˢ��
            issueitem.RefreshListView();
            return true;
        }

        // �Ӽ����к��Ӿ���ͬʱɾ��
        public void PhysicalDeleteItem(
            IssueItem issueitem)
        {
            // ��listview����ʧ
            issueitem.DeleteFromListView();

            this.Remove(issueitem);
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
                this[i].DeleteFromListView();
            }

            base.Clear();
        }

        // ����������ȫ������listview
        public void AddToListView(ListView list)
        {
            for (int i = 0; i < this.Count; i++)
            {
                this[i].AddToListView(list);
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
            dom.LoadXml("<issues />");

            foreach (IssueItem item in this)
            {
                XmlNode node = dom.CreateElement("dprms", "issue", DpNs.dprms);
                dom.DocumentElement.AppendChild(node);

                node.InnerXml = item.RecordDom.DocumentElement.InnerXml;
            }

            strXml = dom.DocumentElement.OuterXml;
            return 0;
        }

        // parameters:
        //      order_refid_change_table   ������¼�� refid ��Ǩ���
        //      item_refid_change_table �������޸ĵĲ�refid���ձ�keyΪ��ֵ��valueΪ��ֵ
        /// <summary>
        /// ����һ�� XML �ַ������ݣ������������ڵ���������
        /// </summary>
        /// <param name="nodeIssueCollection">XmlNode���󣬱�������ʹ���������� dprms:issue Ԫ������������</param>
        /// <param name="list">ListView ���󡣹���õ��������ʾ������</param>
        /// <param name="order_refid_change_table">������¼�� refid ��Ǩ���</param>
        /// <param name="bRefreshRefID">��������Ĺ����У��Ƿ�Ҫˢ��ÿ������� RefID ��Աֵ</param>
        /// <param name="item_refid_change_table">�������޸ĵĲ�refid���ձ�keyΪ��ֵ��valueΪ��ֵ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError ��; 0: �ɹ�</returns>
        public int ImportFromXml(XmlNode nodeIssueCollection,
            ListView list,
            Hashtable order_refid_change_table,
            bool bRefreshRefID,
            Hashtable item_refid_change_table,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (nodeIssueCollection == null)
                return 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);

            XmlNodeList nodes = nodeIssueCollection.SelectNodes("dprms:issue", nsmgr);
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                IssueItem issue_item = new IssueItem();
                nRet = issue_item.SetData("",
                    node.OuterXml,
                    null,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (bRefreshRefID == true)
                    issue_item.RefID = Guid.NewGuid().ToString();

                if (item_refid_change_table != null
                    && item_refid_change_table.Count > 0)
                {
                    // ����<orderInfo>���<distribute>�е�refid�ַ���
                    nRet = issue_item.ReplaceOrderInfoItemRefID(item_refid_change_table,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

                if (order_refid_change_table != null
                    && order_refid_change_table.Count > 0)
                {
                    // ����<orderInfo>���<refID>�е�refid�ַ���
                    nRet = issue_item.ReplaceOrderInfoRefID(order_refid_change_table,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

                this.Add(issue_item);
                issue_item.ItemDisplayState = ItemDisplayState.New;
                issue_item.AddToListView(list);

                issue_item.Changed = true;
            }

            return 0;
        }
    }

}

