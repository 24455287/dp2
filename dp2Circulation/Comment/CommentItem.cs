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
using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient.localhost;  // EntityInfo

namespace dp2Circulation
{
    /// <summary>
    /// ��ע��Ϣ
    /// ��Ҫ���� CommentControl �У���ʾһ����ע��¼
    /// </summary>
    [Serializable()]
    public class CommentItem : BookItemBase
    {
#if NO
        /// <summary>
        /// �������ʾ״̬
        /// </summary>
        public ItemDisplayState ItemDisplayState = ItemDisplayState.Normal;
#endif

        // ��index��ע��Ҫ���ֺ�CommentControl�е��к�һ��
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
        /// ListView ��Ŀ�±꣺��ע����
        /// </summary>
        public const int COLUMN_TYPE = 3;

        /// <summary>
        /// ListView ��Ŀ�±꣺��������
        /// </summary>
        public const int COLUMN_ORDERSUGGESTION = 4;

        /// <summary>
        /// ListView ��Ŀ�±꣺����
        /// </summary>
        public const int COLUMN_TITLE = 5;
        /// <summary>
        /// ListView ��Ŀ�±꣺����
        /// </summary>
        public const int COLUMN_CREATOR = 6;
        /// <summary>
        /// ListView ��Ŀ�±꣺ͼ��ݴ���
        /// </summary>
        public const int COLUMN_LIBRARYCODE = 7;
        /// <summary>
        /// ListView ��Ŀ�±꣺�����
        /// </summary>
        public const int COLUMN_SUBJECT = 8;
        /// <summary>
        /// ListView ��Ŀ�±꣺����ժҪ
        /// </summary>
        public const int COLUMN_SUMMARY = 9;
        /// <summary>
        /// ListView ��Ŀ�±꣺����
        /// </summary>
        public const int COLUMN_CONTENT = 10;
        /// <summary>
        /// ListView ��Ŀ�±꣺����ʱ��
        /// </summary>
        public const int COLUMN_CREATETIME = 11;
        /// <summary>
        /// ListView ��Ŀ�±꣺����޸�ʱ��
        /// </summary>
        public const int COLUMN_LASTMODIFIED = 12;
        /// <summary>
        /// ListView ��Ŀ�±꣺�ο� ID
        /// </summary>
        public const int COLUMN_REFID = 13;
        /// <summary>
        /// ListView ��Ŀ�±꣺������ʷ��Ϣ
        /// </summary>
        public const int COLUMN_OPERATIONS = 14;
        /// <summary>
        /// ListView ��Ŀ�±꣺��ע��¼·��
        /// </summary>
        public const int COLUMN_RECPATH = 15;

        #region ���ݳ�Ա

#if NO
        /// <summary>
        /// ��ȡ������ �ο� ID
        /// ��Ӧ����ע��¼ XML �ṹ�е� refID Ԫ������
        /// </summary>
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
        /// ��ȡ�������� ��ǰ��¼��������Ŀ��¼ ID
        /// ��Ӧ����ע��¼ XML �ṹ�е� parent Ԫ������
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
                this.Changed = true;
            }
        }
#endif

        /// <summary>
        /// ��ȡ������ ������ʷ XML Ƭ����Ϣ
        /// ��Ӧ����ע��¼ XML �ṹ�е� operations Ԫ�ص� InnerXml ����
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

        // ��δʹ��
        /// <summary>
        /// ��ȡ������ ���
        /// ��Ӧ����ע��¼ XML �ṹ�е� index Ԫ������
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
                this.Changed = true; 
            }
        }

        /// <summary>
        /// ��ȡ������ ��¼״̬
        /// ��Ӧ����ע��¼ XML �ṹ�е� state Ԫ������
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
                this.Changed = true;
            }
        }

        /// <summary>
        /// ��ȡ������ ����
        /// ��Ӧ����ע��¼ XML �ṹ�е� type Ԫ������
        /// </summary>
        public string TypeString
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "type");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "type",
                    value);
                this.Changed = true;
            }
        }

        /// <summary>
        /// ��ȡ������ ��������
        /// ��Ӧ����ע��¼ XML �ṹ�е� orderSuggestion Ԫ������
        /// </summary>
        public string OrderSuggestion
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "orderSuggestion");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "orderSuggestion",
                    value);
                this.Changed = true;
            }
        }

        /// <summary>
        /// ��ȡ������ ����
        /// ��Ӧ����ע��¼ XML �ṹ�е� title Ԫ������
        /// </summary>
        public string Title
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "title");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "title",
                    value);
                this.Changed = true;
            }
        }

        /// <summary>
        /// ��ȡ������ ����
        /// ��Ӧ����ע��¼ XML �ṹ�е� creator Ԫ������
        /// </summary>
        public string Creator
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "creator");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "creator",
                    value);
                this.Changed = true;
            }
        }

        /// <summary>
        /// ��ȡ������ �ݴ���
        /// ��Ӧ����ע��¼ XML �ṹ�е� libraryCode Ԫ������
        /// </summary>
        public string LibraryCode
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "libraryCode");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "libraryCode",
                    value);
                this.Changed = true;
            }
        }

        /// <summary>
        /// ��ȡ������ ����
        /// ��Ӧ����ע��¼ XML �ṹ�е� subject Ԫ������
        /// </summary>
        public string Subject
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "subject");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "subject",
                    value);
                this.Changed = true; 
            }
        }

        /// <summary>
        /// ��ȡ������ ժҪ
        /// ��Ӧ����ע��¼ XML �ṹ�е� summary Ԫ������
        /// </summary>
        public string Summary
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "summary");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "summary",
                    value);
                this.Changed = true;
            }
        }

        /// <summary>
        /// ��ȡ������ ����
        /// ��Ӧ����ע��¼ XML �ṹ�е� content Ԫ������
        /// </summary>
        public string Content
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "content");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "content",
                    value);
                this.Changed = true; 
            }
        }

        // ��δʹ��
        /// <summary>
        /// ��ȡ�������� ����ʱ�䡣RFC1123 ��ʽ
        /// ��Ӧ����ע��¼ XML �ṹ�е� createTime Ԫ������
        /// </summary>
        public string CreateTime
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "createTime");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "createTime",
                    value);
                this.Changed = true; 
            }
        }

        // ��δʹ��
        /// <summary>
        /// ��ȡ������ ����޸�ʱ�䡣RFC1123��ʽ
        /// ��Ӧ����ע��¼ XML �ṹ�е� lastModified Ԫ������
        /// </summary>
        public string LastModified
        {
            get
            {
                return DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "lastModified");
            }
            set
            {
                DomUtil.SetElementText(this.RecordDom.DocumentElement, 
                    "lastModified", 
                    value);
                this.Changed = true; 
            }
        }

        #endregion

#if NO
        /// <summary>
        /// ��ע��¼·��
        /// </summary>
        public string RecPath = "";

        /// <summary>
        /// �Ƿ��޸�
        /// </summary>
        bool m_bChanged = false;

        /// <summary>
        /// �ɼ�¼����
        /// </summary>
        public string OldRecord = "";

        /// <summary>
        /// ��ǰ��¼����
        /// </summary>
        public string CurrentRecord = "";   // ��Serialize��������������RecordDom����

        /// <summary>
        /// ��¼���ݵ� XmlDocument ��̬
        /// </summary>
        [NonSerialized()]
        public XmlDocument RecordDom = new XmlDocument();

        // 
        /// <summary>
        /// �ָ���Щ�������л��ĳ�Աֵ
        /// </summary>
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

        /// <summary>
        /// ������Ϣ
        /// </summary>
        public string ErrorInfo
        {
            get
            {
                if (this.Error == null)
                    return "";
                return this.Error.ErrorInfo;
            }
        }

        /// <summary>
        /// �洢���صĴ�����Ϣ
        /// </summary>
        public EntityInfo Error = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        public CommentItem()
        {
            this.RecordDom.LoadXml("<root />");
        }

        /// <summary>
        /// ���Ƴ�һ���µ� CommentItem ����
        /// </summary>
        /// <returns>�µ� CommentItem ����</returns>
        public CommentItem Clone()
        {
            CommentItem newObject = new CommentItem();

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

        // 
        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="strRecPath">��ע��¼·��</param>
        /// <param name="strXml">��ע��¼ XML ����</param>
        /// <param name="baTimeStamp">��ע��¼ʱ���</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError ��; 0: �ɹ�</returns>
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

        // 
        /// <summary>
        /// ������������
        /// </summary>
        /// <param name="strRecPath">��ע��¼·��</param>
        /// <param name="strNewXml">��ע��¼ XML ����</param>
        /// <param name="baTimeStamp">��ע��¼ʱ���</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError ��; 0: �ɹ�</returns>
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

            this.Changed = false;
            this.ItemDisplayState = ItemDisplayState.Normal;
            return 0;
        }


        /// <summary>
        /// ����ʺ��ڱ���ļ�¼��Ϣ
        /// </summary>
        /// <param name="strXml">��¼ XML ����</param>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError ��; 0: �ɹ�</returns>
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
        /// ��ǰ�����Ƿ��޸Ĺ�
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

                if ((this.ItemDisplayState == ItemDisplayState.Normal)
                    && this.m_bChanged == true)
                    this.ItemDisplayState = ItemDisplayState.Changed;
                else if ((this.ItemDisplayState == ItemDisplayState.Changed)
                    && this.m_bChanged == false)
                    this.ItemDisplayState = ItemDisplayState.Normal;
            }
        }

        /// <summary>
        /// ����������뵽 ListView ��
        /// </summary>
        /// <param name="list">ListView����</param>
        /// <returns>���μ���� ListViewItem ����</returns>
        public ListViewItem AddToListView(ListView list)
        {
            ListViewItem item = new ListViewItem(this.Index, 0);

            ListViewUtil.ChangeItemText(item,
                COLUMN_ERRORINFO,
                this.ErrorInfo);
            ListViewUtil.ChangeItemText(item,
                COLUMN_STATE,
                this.State);
            ListViewUtil.ChangeItemText(item,
                COLUMN_TYPE,
                this.TypeString);
            ListViewUtil.ChangeItemText(item,
    COLUMN_ORDERSUGGESTION,
    this.OrderSuggestion);
            ListViewUtil.ChangeItemText(item,
    COLUMN_TITLE,
    this.Title);
            ListViewUtil.ChangeItemText(item,
    COLUMN_CREATOR,
    this.Creator);
            ListViewUtil.ChangeItemText(item,
COLUMN_LIBRARYCODE,
this.LibraryCode);
            ListViewUtil.ChangeItemText(item,
    COLUMN_SUBJECT,
    this.Subject);
            ListViewUtil.ChangeItemText(item,
    COLUMN_SUMMARY,
    this.Summary);
            ListViewUtil.ChangeItemText(item,
    COLUMN_CONTENT,
    this.Content);
            ListViewUtil.ChangeItemText(item,
    COLUMN_CREATETIME,
    this.CreateTime);
            ListViewUtil.ChangeItemText(item,
    COLUMN_LASTMODIFIED,
    this.LastModified);
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

            this.ListViewItem.Tag = this;   // ��CommentItem�������ñ�����ListViewItem������

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
                COLUMN_TYPE,
                this.TypeString);
            ListViewUtil.ChangeItemText(item,
    COLUMN_ORDERSUGGESTION,
    this.OrderSuggestion);
            ListViewUtil.ChangeItemText(item,
    COLUMN_TITLE,
    this.Title);
            ListViewUtil.ChangeItemText(item,
    COLUMN_CREATOR,
    this.Creator);
            ListViewUtil.ChangeItemText(item,
COLUMN_LIBRARYCODE,
this.LibraryCode);
            ListViewUtil.ChangeItemText(item,
    COLUMN_SUBJECT,
    this.Subject);
            ListViewUtil.ChangeItemText(item,
    COLUMN_SUMMARY,
    this.Summary);
            ListViewUtil.ChangeItemText(item,
    COLUMN_CONTENT,
    this.Content);
            ListViewUtil.ChangeItemText(item,
    COLUMN_CREATETIME,
    this.CreateTime);
            ListViewUtil.ChangeItemText(item,
    COLUMN_LASTMODIFIED,
    this.LastModified);
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
        /// <summary>
        /// ��������� ListView ��ɾ��
        /// </summary>
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

        // 
        /// <summary>
        /// ˢ�±������� ListView �еĸ������ݺ�ͼ�ꡢ������ɫ
        /// </summary>
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
                COLUMN_TYPE,
                this.TypeString);
            ListViewUtil.ChangeItemText(item,
                COLUMN_ORDERSUGGESTION,
                this.OrderSuggestion);
            ListViewUtil.ChangeItemText(item,
                COLUMN_TITLE,
                this.Title);
            ListViewUtil.ChangeItemText(item,
                COLUMN_CREATOR,
                this.Creator);
            ListViewUtil.ChangeItemText(item,
    COLUMN_LIBRARYCODE,
    this.LibraryCode);

            ListViewUtil.ChangeItemText(item,
                COLUMN_SUBJECT,
                this.Subject);

            ListViewUtil.ChangeItemText(item,
                COLUMN_SUMMARY,
                this.Summary);
            ListViewUtil.ChangeItemText(item,
                COLUMN_CONTENT,
                this.Content);
            ListViewUtil.ChangeItemText(item,
                COLUMN_CREATETIME,
                this.CreateTime);
            ListViewUtil.ChangeItemText(item,
                COLUMN_LASTMODIFIED,
                this.LastModified);

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
        /// <summary>
        /// ���������� ListView �е���ʾˢ��Ϊ����״̬
        /// </summary>
        /// <param name="bClearOtherHilight">�Ƿ���� ListView ����������ĸ�����ʾ״̬��</param>
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
    /// ��ע��Ϣ����ļ�������
    /// </summary>
    [Serializable()]
    public class CommentItemCollection : BookItemCollectionBase
    {
#if NO
        // ���ȫ������� Parent ��Աֵ�Ƿ��ʺϱ���
        // return:
        //      -1  �д��󣬲��ʺϱ���
        //      0   û�д���
        /// <summary>
        /// ���ȫ������� Parent ��Աֵ�Ƿ��ʺϱ���
        /// ��������пյ� Parent ��Աֵ������ '?' ֵ�� Parent ��Աֵ����᷵�ش���
        /// </summary>
        /// <param name="strError">������Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError ��; 0: û�д���</returns>
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
                    strError = "��ע�����г����˿յ� ParentID ֵ";
                    return -1;
                }

                if (strID == "?")
                {
                    strError = "��ע�����г����� '?' ʽ�� ParentID ֵ";
                    return -1;
                }
            }

            return 0;
        }

        /// <summary>
        /// ��� Panrent ID �б�
        /// </summary>
        /// <returns>Parent ID �б�</returns>
        public List<string> GetParentIDs()
        {
            List<string> results = new List<string>();

            for (int i = 0; i < this.Count; i++)
            {
                CommentItem item = this[i];
                string strParentID = item.Parent;
                if (results.IndexOf(strParentID) == -1)
                    results.Add(strParentID);
            }

            return results;
        }

        // ����ȫ��commentitem�����Parent��
        /// <summary>
        /// Ϊȫ����������һ�µ� Parent ID ֵ
        /// </summary>
        /// <param name="strParentID">Ҫ���õ� Parent ID ֵ</param>
        public void SetParentID(string strParentID)
        {
            for (int i = 0; i < this.Count; i++)
            {
                CommentItem item = this[i];
                if (item.Parent != strParentID) // ����������ν���޸�item.Changed
                    item.Parent = strParentID;
            }
        }

#endif

        /// <summary>
        /// ���±��Ŷ�λһ������
        /// </summary>
        /// <param name="strIndex">�±��š���0��ʼ����</param>
        /// <param name="excludeItems">�ж�����Ҫ�ų�������</param>
        /// <returns>�ҵ������null ��ʾû���ҵ�</returns>
        public CommentItem GetItemByIndex(string strIndex,
            List<CommentItem> excludeItems)
        {
            foreach (CommentItem item in this)
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
        /// <param name="strRecPath">��ע��¼·��</param>
        /// <returns>�ҵ������null ��ʾû���ҵ�</returns>
        public CommentItem GetItemByRecPath(string strRecPath)
        {
            for (int i = 0; i < this.Count; i++)
            {
                CommentItem item = this[i];
                if (item.RecPath == strRecPath)
                    return item;
            }

            return null;
        }
#endif

        // parameters:
        //      strLibraryCodeList  �ݴ����б����ڹ��ˡ���ͳ������б��еġ����Ϊnull��ʾȫ��ͳ��
        //      nYes    ���鶩��������
        //      nNo     ���鲻����������
        //      nNull   û�б�̬����Ҳ�ǡ�������ѯ��������
        //      nOther  "������ѯ"���������
        /// <summary>
        /// ��ý��鶩����ͳ����Ϣ��Ҳ������Щ����Ϊ��������ѯ��������ĸ���
        /// </summary>
        /// <param name="strLibraryCodeList">�ݴ����б����ڹ��˲���ͳ�Ƶ������ͳ�Ʒ��Ϲݴ��뷶Χ��������������Ϊ null�� ��ʾȫ���������ͳ��</param>
        /// <param name="nYes">ѡ���� Yes �ĸ���</param>
        /// <param name="nNo">ѡ���� No �ĸ���</param>
        /// <param name="nNull">��û��ѡ�� Yes Ҳû��ѡ�� No �ĸ���</param>
        /// <param name="nOther">���Ͳ��ǡ�������ѯ�����������</param>
        public void GetOrderSuggestion(
            string strLibraryCodeList,
            out int nYes,
            out int nNo,
            out int nNull,
            out int nOther)
        {
            nYes = 0;
            nNo = 0;
            nNull = 0;
            nOther = 0;

            foreach (CommentItem item in this)
            {
                if (Global.IsGlobalUser(strLibraryCodeList) == false)
                {
                    // ע�⣺item.LibraryCode������һ�������б�
                    if (StringUtil.IsInList(item.LibraryCode, strLibraryCodeList) == false)
                        continue;
                }

                if (item.TypeString != "������ѯ")
                {
                    nOther++;
                    continue;
                }

                if (item.OrderSuggestion == "yes")
                    nYes++;
                else if (string.IsNullOrEmpty(item.OrderSuggestion) == true)
                    nNull++;
                else
                    nNo++;
            }
        }

#if NO
        /// <summary>
        /// �Բο� ID ��λһ������
        /// </summary>
        /// <param name="strRefID">�ο� ID</param>
        /// <param name="excludeItems">Ҫ�����ų��������б�</param>
        /// <returns>�ҵ������null ��ʾû���ҵ�</returns>
        public CommentItem GetItemByRefID(string strRefID,
            List<CommentItem> excludeItems)
        {
            for (int i = 0; i < this.Count; i++)
            {
                CommentItem item = this[i];

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
        /// ��ȡ�������ã������Ƿ��޸Ĺ�
        /// ֻҪ��һ��Ԫ�ر��޸Ĺ����͵������ϱ��޸Ĺ�
        /// </summary>
        public bool Changed
        {
            get
            {
                if (this.m_bChanged == true)
                    return true;

                for (int i = 0; i < this.Count; i++)
                {
                    CommentItem item = this[i];
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
                        CommentItem item = this[i];
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

        // 
        /// <summary>
        /// ���ɾ��ָ��������
        /// </summary>
        /// <param name="bRemoveFromList">�Ƿ�Ҫ�� ListView ���Ƴ��������?</param>
        /// <param name="comentitem">Ҫ���ɾ��������</param>
        public void MaskDeleteItem(
            bool bRemoveFromList,
            CommentItem comentitem)
        {
            if (comentitem.ItemDisplayState == ItemDisplayState.New)
            {
                PhysicalDeleteItem(comentitem);
                return;
            }


            comentitem.ItemDisplayState = ItemDisplayState.Deleted;
            comentitem.Changed = true;

            // ��listview����ʧ?
            if (bRemoveFromList == true)
                comentitem.DeleteFromListView();
            else
            {
                comentitem.RefreshListView();
            }
        }

        // Undo���ɾ��
        // return:
        //      false   û�б�ҪUndo
        //      true    �ɹ�Undo
        /// <summary>
        /// ������һ������ı��ɾ��
        /// </summary>
        /// <param name="commentitem">Ҫ�������ɾ��������</param>
        /// <returns>false: û�б�Ҫ����(��Ϊָ��������ڱ��ɾ��״̬); true: �ɹ��������ɾ��</returns>
        public bool UndoMaskDeleteItem(CommentItem commentitem)
        {
            if (commentitem.ItemDisplayState != ItemDisplayState.Deleted)
                return false;   // ҪUndo����������Ͳ���Deleted״̬������̸����Undo

            // ��Ϊ��֪���ϴα��ɾ��ǰ�����Ƿ�Ĺ������ȫ���Ĺ�
            commentitem.ItemDisplayState = ItemDisplayState.Changed;
            commentitem.Changed = true;

            // ˢ��
            commentitem.RefreshListView();
            return true;
        }

        // �Ӽ����к��Ӿ���ͬʱɾ��
        /// <summary>
        /// �Ӽ����к� ListView ��ͬʱ���ָ�������
        /// ע�⣬����ָ�����ݿ�ɾ����¼
        /// </summary>
        /// <param name="commentitem"></param>
        public void PhysicalDeleteItem(
            CommentItem commentitem)
        {
            // ��listview����ʧ
            commentitem.DeleteFromListView();

            this.Remove(commentitem);
        }

        /// <summary>
        /// ��� ListView ��ȫ������״̬����
        /// </summary>
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

        /// <summary>
        /// ���������Ԫ��
        /// �Ӽ����к� ListView ��ͬʱ���ȫ������
        /// ע�⣬����ָ�����ݿ�ɾ����¼
        /// </summary>
        public new void Clear()
        {
            for (int i = 0; i < this.Count; i++)
            {
                CommentItem item = this[i];
                item.DeleteFromListView();
            }

            base.Clear();
        }

        // ����������ȫ������listview
        /// <summary>
        /// �ѵ�ǰ�����е�����ȫ������ ListView
        /// </summary>
        /// <param name="list"></param>
        public void AddToListView(ListView list)
        {
            for (int i = 0; i < this.Count; i++)
            {
                CommentItem item = this[i];
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
            dom.LoadXml("<comments />");

            foreach (CommentItem item in this)
            {
                XmlNode node = dom.CreateElement("dprms", "comment", DpNs.dprms);
                dom.DocumentElement.AppendChild(node);

                node.InnerXml = item.RecordDom.DocumentElement.InnerXml;
            }

            strXml = dom.DocumentElement.OuterXml;
            return 0;
        }

        /// <summary>
        /// ����һ�� XML �ַ������ݣ������������ڵ���������
        /// </summary>
        /// <param name="nodeCommentCollection">XmlNode���󣬱�������ʹ���������� dprms:comment Ԫ������������</param>
        /// <param name="list">ListView ���󡣹���õ��������ʾ������</param>
        /// <param name="bRefreshRefID">��������Ĺ����У��Ƿ�Ҫˢ��ÿ������� RefID ��Աֵ</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����������Ϣ�� strError ��; 0: �ɹ�</returns>
        public int ImportFromXml(XmlNode nodeCommentCollection,
            ListView list,
            bool bRefreshRefID,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (nodeCommentCollection == null)
                return 0;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);

            XmlNodeList nodes = nodeCommentCollection.SelectNodes("dprms:comment", nsmgr);
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                CommentItem comment_item = new CommentItem();
                nRet = comment_item.SetData("",
                    node.OuterXml,
                    null,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (bRefreshRefID == true)
                    comment_item.RefID = Guid.NewGuid().ToString();

                this.Add(comment_item);
                comment_item.ItemDisplayState = ItemDisplayState.New;
                comment_item.AddToListView(list);

                comment_item.Changed = true;
            }

            return 0;
        }
    }
}

