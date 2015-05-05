using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using System.Xml;
using System.Drawing;

using DigitalPlatform.Xml;

namespace DigitalPlatform.Library
{
    /// <summary>
    /// ����Ϣ
    /// </summary>
    public class BookItem
    {
        /// <summary>
        ///  �������
        /// </summary>
        public string Barcode = ""; 

        /// <summary>
        /// ��״̬
        /// </summary>
        public string State = "";   

        /// <summary>
        /// ��������Ŀ��¼id
        /// </summary>
        public string Parent = ""; 

        /// <summary>
        /// �ݲصص�
        /// </summary>
        public string Location = "";  

        /// <summary>
        /// ��۸�
        /// </summary>
        public string Price = ""; 
        /// <summary>
        /// ͼ������
        /// </summary>
        public string BookType = "";  
        /// <summary>
        /// ע��
        /// </summary>
        public string Comment = ""; 
        /// <summary>
        /// ������֤�����
        /// </summary>
        public string Borrower = "";  
        /// <summary>
        /// ���������
        /// </summary>
        public string BorrowDate = ""; 
        /// <summary>
        /// ��������
        /// </summary>
        public string BorrowPeriod = ""; 

        /// <summary>
        ///  ���¼·��
        /// </summary>
        public string RecPath = "";

        /// <summary>
        /// �Ƿ��޸�
        /// </summary>
        bool m_bChanged = false;

        /// <summary>
        /// ��¼��dom
        /// </summary>
        public XmlDocument RecordDom = null;  

        /// <summary>
        /// ʱ���
        /// </summary>
        public byte[] Timestamp = null;

        ListViewItem ListViewItem = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        public BookItem()
        {
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="strRecPath"></param>
        /// <param name="dom"></param>
        public BookItem(string strRecPath, XmlDocument dom)
        {
            this.RecPath = strRecPath;
            this.RecordDom = dom;

            this.Initial();
        }


        /// <summary>
        /// ����dom��ʼ��������Ա
        /// </summary>
        /// <returns></returns>
        public int Initial()
        {
            if (this.RecordDom == null)
                return 0;

            this.Barcode = DomUtil.GetElementText(this.RecordDom.DocumentElement, "barcode");
            this.State = DomUtil.GetElementText(this.RecordDom.DocumentElement, "state");
            this.Location = DomUtil.GetElementText(this.RecordDom.DocumentElement, "location");
            this.Price = DomUtil.GetElementText(this.RecordDom.DocumentElement, "price");
            this.BookType = DomUtil.GetElementText(this.RecordDom.DocumentElement, "bookType");

            this.Comment = DomUtil.GetElementText(this.RecordDom.DocumentElement, "comment");
            this.Borrower = DomUtil.GetElementText(this.RecordDom.DocumentElement, "borrower");
            this.BorrowDate = DomUtil.GetElementText(this.RecordDom.DocumentElement, "borrowDate");
            this.BorrowPeriod = DomUtil.GetElementText(this.RecordDom.DocumentElement, "borrowPeriod");

            this.Parent = DomUtil.GetElementText(this.RecordDom.DocumentElement, "parent");
            m_bChanged = false;
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

            if (this.RecordDom == null)
            {
                this.RecordDom = new XmlDocument();
                this.RecordDom.LoadXml("<root />");
            }

            if (this.Parent == "")
            {
                strError = "Parent��Ա��δ����";
                return -1;
            }

            if (this.Barcode == "")
            {
                strError = "Barcode��Ա��δ����";
                return -1;
            }

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "parent", this.Parent);

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "barcode", this.Barcode);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "state", this.State);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "location", this.Location);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "price", this.Price);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "bookType", this.BookType);

            DomUtil.SetElementText(this.RecordDom.DocumentElement, "comment", this.Comment);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "borrower", this.Borrower);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "borrowDate", this.BorrowDate);
            DomUtil.SetElementText(this.RecordDom.DocumentElement, "borrowPeriod", this.BorrowPeriod);

            strXml = this.RecordDom.OuterXml;

            return 0;
        }

        /// <summary>
        /// �Ƿ��޸Ĺ�
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
            }
        
        }


        /// <summary>
        /// ����������뵽listview��
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public ListViewItem AddToListView(ListView list)
        {
            ListViewItem item = new ListViewItem(this.Barcode);

            item.SubItems.Add(this.State);
            item.SubItems.Add(this.Location);
            item.SubItems.Add(this.Price);
            item.SubItems.Add(this.BookType);
            item.SubItems.Add(this.Comment);
            item.SubItems.Add(this.Borrower);
            item.SubItems.Add(this.BorrowDate);
            item.SubItems.Add(this.BorrowPeriod);
            item.SubItems.Add(this.RecPath);

            this.SetItemBackColor(item);

            list.Items.Add(item);

            this.ListViewItem = item;

            return item;
        }

        void SetItemBackColor(ListViewItem item)
        {
            if (this.State == "" && this.m_bChanged == true)
            {
                // ������
               item.BackColor = Color.FromArgb(255, 255, 100); // ǳ��ɫ
            }
            else if (this.m_bChanged == true)
            {
                // �޸Ĺ��ľ�����
                item.BackColor = Color.FromArgb(100, 255, 100); // ǳ��ɫ
            }
            else
            {
                item.BackColor = SystemColors.Window;
            }
        }

        /// <summary>
        /// ˢ��������ɫ
        /// </summary>
        public void RefreshItemColor()
        {
            if (this.ListViewItem != null)
                this.SetItemBackColor(this.ListViewItem);
        }

    }

    /// <summary>
    /// ����Ϣ�ļ�������
    /// </summary>
    public class BookItemCollection : List<BookItem>
    {

        /// <summary>
        /// �Բ�����Ŷ�λһ������
        /// </summary>
        /// <param name="strBarcode"></param>
        /// <returns></returns>
        public BookItem GetItem(string strBarcode)
        {
            for (int i = 0; i < this.Count; i++)
            {
                BookItem item = this[i];
                if (item.Barcode == strBarcode)
                    return item;
            }

            return null;
        }

        /// <summary>
        /// �Ƿ��޸Ĺ�
        /// </summary>
        public bool Changed
        {
            get
            {
                for (int i = 0; i < this.Count; i++)
                {
                    BookItem item = this[i];
                    if (item.Changed == true)
                        return true;
                }

                return false;
            }

            set
            {
                for (int i = 0; i < this.Count; i++)
                {
                    BookItem item = this[i];
                    if (item.Changed != value)
                        item.Changed = value;
                }
            }
        }
    }
}
