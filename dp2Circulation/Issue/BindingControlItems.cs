using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Xml;
using System.Drawing.Imaging;

using System.Runtime.InteropServices;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// �ڿ�ͼ�������󣺵�һ��Σ��ڶ���
    /// </summary>
    internal class IssueBindingItem : CellBase
    {
        /// <summary>
        /// �������ڿ�ͼ�ν���ؼ�
        /// </summary>
        public BindingControl Container = null;

        // ��ʾ��Ԫ������
        /// <summary>
        /// ��Ԫ�񼯺�
        /// </summary>
        public List<Cell> Cells = new List<Cell>();

        /// <summary>
        /// ����ģʽ
        /// </summary>
        public IssueLayoutState IssueLayoutState = IssueLayoutState.Binding;  // Binding

        /// <summary>
        /// ���ڴ����Ҫ���ӵ��������Ͷ���
        /// </summary>
        public object Tag = null;   // 

        // public string Xml = ""; // 
        // string m_strXml = "";

        /// <summary>
        /// ����ڼ�¼�� XML �ַ���
        /// </summary>
        public string Xml
        {
            get
            {
                if (dom != null)
                    return dom.OuterXml;

                return "";

                // return m_strXml;
            }
            /*
            set
            {
                m_strXml = value;
            }
             * */
        }
        internal XmlDocument dom = null;

        internal bool Virtual = false;  // �Ƿ�Ϊ������ڣ�== true ������ġ���ν����ģ����Ǹ���ʵ�еĲ��publishtime��ʱ�������ڶ��󣬶�������ʵ���������ݿ��е��ڶ���

        // ����Ϣ������
        // ע��: ��Cells���ź��Ժ������Ҫ���?
        internal List<ItemBindingItem> Items = new List<ItemBindingItem>();   // �����Ĳ�

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed = false;

        /// <summary>
        /// �Ƿ�Ϊ�´����Ķ���
        /// </summary>
        public bool NewCreated = false;

        /// <summary>
        /// �����ķ������Ӷ���XML�л�õġ�-1��ʾδ֪
        /// </summary>
        public int OrderedCount = -1;    // �����ķ������Ӷ���XML�л�õġ�-1��ʾδ֪

        bool OrderInfoLoaded = false;   // �ɹ���Ϣ�Ƿ���ⲿװ�ع��������ظ�װ��

        // �����Ĳɹ���Ϣ���������
        internal List<OrderBindingItem> OrderItems = new List<OrderBindingItem>();

        /// <summary>
        /// ��ñ�������ʾ�����ؿ��
        /// </summary>
        public override int Width
        {
            get
            {
                return this.Container.m_nLeftTextWidth + (Container.m_nCellWidth * this.Cells.Count);
            }
        }

        /// <summary>
        /// ��ñ�������ʾ���ظ߶�
        /// </summary>
        public override int Height
        {
            get
            {
                return this.Container.m_nCellHeight;
            }
        }

        /// <summary>
        /// ������ֲ��ֵ����ؿ��
        /// </summary>
        public int LeftTextWidth
        {
            get
            {
                return this.Container.m_nLeftTextWidth;
            }
        }

        // ������ʾ�����������磺��2008���1�� (��.100 v.10)��
        /// <summary>
        /// ������ʾ�����������磺��2008���1�� (��.100 v.10)��
        /// </summary>
        public string Caption
        {
            get
            {
                if (String.IsNullOrEmpty(this.PublishTime) == true)
                    return "����";

                string strZongAndVolume = "";

                if (String.IsNullOrEmpty(this.Volume) == false)
                    strZongAndVolume += "v." + this.Volume;
                if (String.IsNullOrEmpty(this.Zong) == false)
                {
                    if (String.IsNullOrEmpty(strZongAndVolume) == false)
                        strZongAndVolume += " ";
                    strZongAndVolume += "��." + this.Zong;
                }

                string strYear = IssueUtil.GetYearPart(this.PublishTime);
                return strYear + "���" + this.Issue + "��"
                    + (String.IsNullOrEmpty(strZongAndVolume) == false ?
                        "(" + strZongAndVolume + ")" : "");
            }

        }

        /// <summary>
        /// ��������� OrderItems ��������
        /// </summary>
        public void ClearOrderItems()
        {
            this.OrderItems.Clear();
        }

        // ��ǰ���Ƿ����ɾ��?
        internal bool CanDelete(out string strMessage)
        {
            strMessage = "";
            this.RemoveTailNullCell();
            if (this.Cells.Count == 0)
                return true;

            int nLockedCellCount = 0;
            int nCellCount = 0;
            for (int i = 0; i < this.Cells.Count; i++)
            {
                Cell cell = this.Cells[i];
                if (cell == null)
                    continue;
                if (cell.item == null)
                    continue;

                if (cell.item.Deleted == true)
                    continue;

                if (cell.item.Calculated == true && cell.item.Locked == true)
                {
                    nLockedCellCount++;
                }

                if (cell.item.Calculated == true)
                    continue;

                // ����к϶��ᣬ�Ƿ�����ɾ�������ɾ���Ժ�����װ��ʱ�����ؽ�����������ɾ��
                nCellCount ++;
            }

            if (nLockedCellCount > 0)
                strMessage += "���� " + nLockedCellCount.ToString() + " ������״̬�Ĳ����";
            if (nCellCount > 0)
            {
                if (string.IsNullOrEmpty(strMessage) == false)
                    strMessage += ",";
                strMessage += "���� " + nCellCount.ToString() + " ���ѵ������";
            }

            if (string.IsNullOrEmpty(strMessage) == false)
                return false;

            return true;
        }

        /// <summary>
        /// ͨ�� index ���һ����Ԫ����
        /// </summary>
        /// <param name="index">index</param>
        /// <returns>��Ԫ����</returns>
        public Cell GetCell(int index)
        {
            if (this.Cells.Count <= index)
                return null;

            return this.Cells[index];
        }

        /// <summary>
        /// ����һ����Ԫ����
        /// </summary>
        /// <param name="index">index</param>
        /// <param name="cell">��Ԫ����</param>
        public void SetCell(int index, Cell cell)
        {
            // ȷ����������㹻
            while (this.Cells.Count <= index)
                this.Cells.Add(null);

            Cell old_cell = this.Cells[index];
            if (old_cell != null && old_cell != cell)
            {
                if (this.Container.FocusObject == old_cell)
                    this.Container.FocusObject = null;
                if (this.Container.HoverObject == old_cell)
                    this.Container.HoverObject = null;
            }

            this.Cells[index] = cell;
            if (cell != null)
            {
                cell.Container = this;
                /*
                // 2010/3/3
                if (cell.item != null)
                    cell.item.Container = this;
                 * */
            }

            // TODO: ��������Cell��Container�Ƿ�Ҫ����Ϊnull? ����������Ӱ�쵽ˢ��
        }

        /// <summary>
        /// ׷��һ����Ԫ����
        /// </summary>
        /// <param name="cell">��Ԫ����</param>
        public void AddCell(Cell cell)
        {
            this.Cells.Add(cell);
            cell.Container = this;
        }

        /// <summary>
        /// ����һ����Ԫ����
        /// </summary>
        /// <param name="nPos">Ҫ�����λ���±�</param>
        /// <param name="cell">Ҫ����ĵ�Ԫ����</param>
        public void InsertCell(int nPos, Cell cell)
        {
            this.Cells.Insert(nPos, cell);
            cell.Container = this;
        }

        // ɾ��һ��Cell��
        // ע�⣬���Ҫ����Ϊ�հ�Cell������ʹ�ñ�����
        /// <summary>
        /// ͨ��ָ�� ItemBindingItem ������ɾ��һ����Ԫ��
        /// </summary>
        /// <param name="item">ItemBindingItem ����</param>
        public void RemoveCell(ItemBindingItem item)
        {
            int index = IndexOfItem(item);
            if (index == -1)
                return;
            this.Cells.RemoveAt(index);
        }

        /*
        // ����Ϊ�հ�Cell
        // parameters:
        //      parent_item �����õ�Cell�Ĵ����϶��������Ϊnull��ʾ�����Ǻ϶�����
        public void SetCellBlank(ItemBindingItem item,
            ItemBindingItem parent_item)
        {
            int index = IndexOfItem(item);
            if (index == -1)
                return;
            Cell cell = this.Cells[index];

            if (cell.item != null)
            {
                cell.ParentItem = parent_item;
                cell.item = null;
            }
        }
         * */

        /// <summary>
        /// ��õ�Ԫ������±�
        /// </summary>
        /// <param name="cell">��Ԫ����</param>
        /// <returns>�±�</returns>
        public int IndexOfCell(Cell cell)
        {
            return this.Cells.IndexOf(cell);
        }

        // ��λ��Cells������±�
        /// <summary>
        /// ͨ�� ItemBindingItem ����λ��Ԫ���±�
        /// </summary>
        /// <param name="item">ItemBindingItem����</param>
        /// <returns>�±�</returns>
        public int IndexOfItem(ItemBindingItem item)
        {
            for (int i = 0; i < this.Cells.Count; i++)
            {
                Cell cell = this.Cells[i];
                if (cell == null)
                    continue;
                if (cell.item == item)
                    return i;
            }

            return -1;
        }

        // ɾ��ĩβ��null��Ԫ
        /// <summary>
        /// ɾ��ĩβ��null��Ԫ
        /// </summary>
        /// <returns>�Ƿ�����ɾ��</returns>
        public bool RemoveTailNullCell()
        {
            bool bChanged = false;
            for (int i = this.Cells.Count - 1; i >= 0; i--)
            {
                Cell cell = this.Cells[i];
                if (cell == null)
                {
                    this.Cells.RemoveAt(i);
                    bChanged = true;
                }
                else
                    break;
            }

            return bChanged;
        }

        // �Ƿ������Ա���϶�������?
        /// <summary>
        /// �Ƿ������Ա���϶�������?
        /// </summary>
        /// <returns>�ǻ��߷�</returns>
        public bool HasMemberOrParentCell()
        {
            if (this.IssueLayoutState == dp2Circulation.IssueLayoutState.Binding)
            {
                for (int i = 0; i < this.Cells.Count; i++)
                {
                    int nRet = this.IsBoundIndex(i);
                    if (nRet != 0)
                        return true;
                }
            }
            else
            {
                // 2012/9/29
                for (int i = 0; i < this.Cells.Count; i++)
                {
                    Cell cell = this.Cells[i];
                    if (cell == null)
                        continue;
                    if (cell.IsMember == true)
                        return true;
                    if (cell.item != null &&  cell.item.IsParent == true)
                        return true;
                }
            }

            return false;
        }

        public void AfterMembersChanged()
        {
            bool bChanged = false;
            if (RefreshOrderInfoXml() == true)
                bChanged = true;

            if (bChanged == true)
            {
                this.Changed = true;

                // ����ڸ���������ʾ���������ַ����仯�Ļ�
                /*
                try
                {
                    this.Container.UpdateObject(this);
                }
                catch
                {
                }
                 * */
            }
        }

        // MemberCells�޸ĺ�Ҫˢ��binding XMLƬ��
        // ���ܻ��׳��쳣
        // return:
        //      false   binding XMLƬ��û�з����޸�
        //      true    binding XMLƬ�Ϸ������޸�
        public bool RefreshOrderInfoXml()
        {
            if (this.OrderItems.Count == 0)
            {
                if (this.OrderInfo == "")
                    return false;
                this.OrderInfo = "";
                return true;
            }

            // ����<orderInfo>Ԫ����Ƭ��
            string strInnerXml = "";
            string strError = "";
            int nRet = BuildOrderInfoXmlString(this.OrderItems,
                out strInnerXml,
                out strError);
            if (nRet == -1)
                throw new Exception(strError);

            if (this.OrderInfo == strInnerXml)
                return false;

            this.OrderInfo = strInnerXml;
            return true;
        }

        // ����<orderInfo>Ԫ����Ƭ��
        // Ҫ��������<root>Ԫ��
        public static int BuildOrderInfoXmlString(List<OrderBindingItem> orders,
            out string strInnerXml,
            out string strError)
        {
            strInnerXml = "";
            strError = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<orderInfo />");

            for (int i = 0; i < orders.Count; i++)
            {
                OrderBindingItem order = orders[i];
                if (order == null)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                XmlNode node = dom.CreateElement("root");   // item?
                dom.DocumentElement.AppendChild(node);

                node.InnerXml = order.dom.DocumentElement.InnerXml;
            }

            strInnerXml = dom.DocumentElement.InnerXml;
            return 0;
        }

        // 2012/5/16
        // �ڶ�����Ϣ�����ж�λ������Ԫ����Ϣ��Cell����
        static List<OrderBindingItem> LocateInOrders(Cell cell,
            List<OrderBindingItem> orders)
        {
            List<OrderBindingItem> results = new List<OrderBindingItem>();
            if (cell.item == null)
                return results;

            for (int i = 0; i < orders.Count; i++)
            {
                OrderBindingItem order = orders[i];

                // �����Ԫ��
                if (cell.item.Seller != order.Seller)
                    continue;
                if (cell.item.Source != order.Source)
                    continue;
                if (cell.item.Price != order.Price)
                    continue;
                // ���һ���������ʱ���Ƿ񳬹�����ʱ�䷶Χ?
                if (Global.InRange(cell.item.PublishTime,
                    order.Range) == false)
                    continue;

                results.Add(order);
            }

            return results;
        }

        // 2012/5/16
        // ���������ҵ����϶�����Ԫ����Ϣ��Cell����
        // ����ж��ƥ�䣬�򷵻�null
        static Cell FindGroupMemberCell(List<Cell> cells,
            OrderBindingItem order,
            string strLocationName)
        {
            List<Cell> results = new List<Cell>();
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (cell == null)
                    continue;
                if (cell.item == null)
                    continue;

                if (cell.item.LocationString != strLocationName)
                    continue;

                // �����Ԫ��
                if (cell.item.Seller != order.Seller)
                    continue;
                if (cell.item.Source != order.Source)
                    continue;
                if (cell.item.Price != order.Price)
                    continue;
                // ���һ���������ʱ���Ƿ񳬹�����ʱ�䷶Χ?
                if (Global.InRange(cell.item.PublishTime,
                    order.Range) == false)
                    continue;

                results.Add(cell);
            }

            if (results.Count == 1)
                return results[0];

            return null;
        }

        // ���������ҵ�����ָ�� x, y�������ӵ�Cell����
        static Cell FindGroupMemberCell(List<Cell> cells,
            int x, int y)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (cell == null)
                    continue;
                if (cell.item == null)
                    continue;
                if (cell.item.OrderInfoPosition.X == x
                    && cell.item.OrderInfoPosition.Y == y)
                    return cell;
            }

            return null;
        }

        public static int GetNumberValue(string strNumber)
        {
            try
            {
                return Convert.ToInt32(strNumber);
            }
            catch
            {
                return 0;
            }
        }

        // �޸��¾�ֵ�ַ����е����ַ�������
        public static string ChangeNewValue(string strExistString,
            string strNewValue)
        {
            string strTempNewValue = "";
            string strOldValue = "";
            OrderDesignControl.ParseOldNewValue(strExistString,
                out strOldValue,
                out strTempNewValue);
            return OrderDesignControl.LinkOldNewValue(strOldValue,
                strNewValue);
        }

        // ���¾�ֵ�ַ����л����ֵ����
        public static string GetNewValue(string strValue)
        {
            string strNewValue = "";
            string strOldValue = "";
            OrderDesignControl.ParseOldNewValue(strValue,
                out strOldValue,
                out strNewValue);
            return strNewValue;
        }


        // ���¾�ֵ�ַ�����˳�λ����ֵ���֣����Ȼ����ֵ
        public static string GetNewOrOldValue(string strValue)
        {
            string strNewValue = "";
            string strOldValue = "";
            OrderDesignControl.ParseOldNewValue(strValue,
                out strOldValue,
                out strNewValue);
            if (String.IsNullOrEmpty(strNewValue) == false)
                return strNewValue;
            return strOldValue;
        }

        // ���¾�ֵ�ַ�����˳�λ����ֵ���֣����Ȼ�þ�ֵ
        public static string GetOldOrNewValue(string strValue)
        {
            string strNewValue = "";
            string strOldValue = "";
            OrderDesignControl.ParseOldNewValue(strValue,
                out strOldValue,
                out strNewValue);
            if (String.IsNullOrEmpty(strOldValue) == false)
                return strOldValue;
            return strNewValue;
        }

        // ���¾�ֵ�ַ����л�þ�ֵ����
        public static string GetOldValue(string strValue)
        {
            string strNewValue = "";
            string strOldValue = "";
            OrderDesignControl.ParseOldNewValue(strValue,
                out strOldValue,
                out strNewValue);
            return strOldValue;
        }

        internal static void SetFieldValueFromOrderInfo(
            bool bForce,    // �Ƿ�ǿ������
            ItemBindingItem item,
            OrderBindingItem order)
        {
            if (bForce == true || String.IsNullOrEmpty(item.Source) == true)
                item.Source = GetNewOrOldValue(order.Source);

            if (bForce == true || String.IsNullOrEmpty(item.Seller) == true)
                item.Seller = GetNewOrOldValue(order.Seller);

            if (bForce == true || String.IsNullOrEmpty(item.Price) == true)
            {
                // TODO: ���������¼�е� price Ϊ�գ�����Ҫ�� totalPrice �м������
                string strPrice = GetNewOrOldValue(order.Price);
                if (string.IsNullOrEmpty(strPrice) == false)
                    item.Price = strPrice;
                else
                {
                    // 2015/4/1
                    item.Price = CalcuPrice(order.TotalPrice, order.IssueCount, GetOldOrNewValue(order.Copy));
                }

                // item.Price = GetNewOrOldValue(order.Price);
            }
        }

        // �����ܼۺ����������������������
        internal static string CalcuPrice(string strTotalPrice,
            string strIssueCount,
            string strCopy)
        {
            long count = 0;
            if (long.TryParse(strIssueCount, out count) == false)
            {
                return "������Ϣ������ '" + strIssueCount + "' ��ʽ����";
            }

            long copy = 0;
            if (long.TryParse(strCopy, out copy) == false)
            {
                return "������Ϣ�и����� '" + strCopy + "' ��ʽ����";
            }

            return strTotalPrice + "/" + (count * copy).ToString();
        }

        // ���ݸ���index�����(ͷ��)����
        // ����������ӻ�����������λ�ã�������ʹ�ñ�����
        // ��������indexλ�õĸ��ӱ����״̬����Ҫ��ֻ�ǰ���λ�ù�ϵ���ж�
        internal GroupCell BelongToGroup(int index)
        {
            GroupCell group = null;
            // int n = -1;
            for (int i = 0; i < this.Cells.Count; i++)
            {
                Cell cell = this.Cells[i];
                if (cell == null)
                    goto CONTINUE;
                if (cell is GroupCell)
                {
                    GroupCell current_group = (GroupCell)cell;
                    if (current_group.EndBracket == false)
                    {
                        // n++;
                        group = current_group;
                    }
                    else if (current_group.EndBracket == true)
                    {
                        if (index == i)
                            return group;
                        group = null;
                    }
                }
            CONTINUE:
                if (index == i)
                    return group;
            }

            return null;
        }

        // ��������Ż����(ͷ��)����
        internal GroupCell GetGroupCellHead(int group_index)
        {
            int n = 0;
            for (int i = 0; i < this.Cells.Count; i++)
            {
                Cell cell = this.Cells[i];
                if (cell == null)
                    continue;
                if (cell is GroupCell)
                {
                    GroupCell group = (GroupCell)cell;
                    if (group.EndBracket == false)
                    {
                        if (n == group_index)
                            return group;
                        n++;
                    }
                }
            }

            return null;
        }

        // ��������Ż����(β��)����
        internal GroupCell GetGroupCellTail(int group_index)
        {
            int n = 0;
            for (int i = 0; i < this.Cells.Count; i++)
            {
                Cell cell = this.Cells[i];
                if (cell == null)
                    continue;
                if (cell is GroupCell)
                {
                    GroupCell group = (GroupCell)cell;
                    if (group.EndBracket == true)
                    {
                        if (n == group_index)
                            return group;
                        n++;
                    }
                }
            }

            return null;
        }

        // ��ÿɼ��Ķ������е�refid
        public int GetVisibleRefIDs(
            string strLibraryCodeList,
            out List<string> refids,
            out string strError)
        {
            strError = "";
            refids = new List<string>();

            // TODO: ����Ϊorderitem�����Ƿ�ɼ��ı�ǣ��Ϳ��Լ����ж�
            // �����Ƿ������ǰɾ�����ɼ���orderitem����?
            foreach (OrderBindingItem order in this.OrderItems)
            {
                // �۲�һ���ݲط����ַ����������Ƿ񲿷��ڵ�ǰ�û���Ͻ��Χ��
                // return:
                //      -1  ����
                //      0   û���κβ����ڹ�Ͻ��Χ
                //      1   ���ٲ����ڹ�Ͻ��Χ��
                int nRet = Global.DistributeCross(order.Distribute,
                    strLibraryCodeList,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                    continue;

                List<string> temp = null;
                nRet = Global.GetRefIDs(order.Distribute,
                    out temp,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (temp != null && temp.Count > 0)
                    refids.AddRange(temp);
            }

            return 0;
        }

        // 2012/9/21
        public int InitialOrderItems(out string strError)
        {
            strError = "";
            int nRet = 0;

            XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*");

            if (nodes.Count == 0
                && this.OrderInfoLoaded == false)
            {
                // ��Ҫ���ⲿ��òɹ���Ϣ
                GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
                e1.BiblioRecPath = "";
                e1.PublishTime = this.PublishTime;
                if (this.Container.m_bHideLockedOrderGroup == true)
                    e1.LibraryCodeList = this.Container.LibraryCodeList;
                this.Container.DoGetOrderInfo(this, e1);
                if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
                {
                    strError = "�ڻ�ȡ�����ڳ�������Ϊ '" + this.PublishTime + "' �Ķ�����Ϣ�Ĺ����з�������: " + e1.ErrorInfo;
                    return -1;
                }

                if (e1.OrderXmls.Count == 0)
                {
                    this.OrderInfoLoaded = true;
                }
                else
                {
                    XmlNode root = this.dom.DocumentElement.SelectSingleNode("orderInfo");
                    if (root == null)
                    {
                        root = this.dom.CreateElement("orderInfo");
                        this.dom.DocumentElement.AppendChild(root);
                    }
                    for (int i = 0; i < e1.OrderXmls.Count; i++)
                    {
                        XmlDocument whole_dom = new XmlDocument();
                        try
                        {
                            whole_dom.LoadXml(e1.OrderXmls[i]);
                        }
                        catch (Exception ex)
                        {
                            strError = "������¼�� " + i.ToString() + " ��XMLװ��DOMʱ����: " + ex.Message;
                            return -1;
                        }
                        XmlNode node = this.dom.CreateElement("root");
                        root.AppendChild(node);
                        node.InnerXml = whole_dom.DocumentElement.InnerXml;
                    }
                    nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*");
                }
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                OrderBindingItem order = new OrderBindingItem();
                nRet = order.Initial(node.OuterXml, out strError);
                if (nRet == -1)
                    return -1;

#if NO
                if (this.Container.HideLockedOrderGroup == true)
                {
                    // �۲�һ���ݲط����ַ����������Ƿ񲿷��ڵ�ǰ�û���Ͻ��Χ��
                    // return:
                    //      -1  ����
                    //      0   û���κβ����ڹ�Ͻ��Χ
                    //      1   ���ٲ����ڹ�Ͻ��Χ��
                    nRet = Global.DistributeCross(order.Distribute,
                        this.Container.LibraryCodeList,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                    {
                        List<string> refids = null;
                        // ���һ���ݲط����ַ������������refid
                        nRet = Global.GetRefIDs(order.Distribute,
            out refids,
            out strError);
                        if (nRet == -1)
                            return -1;
                        foreach (string refid in refids)
                        {
                            Cell cell = FindCellByRefID(refid, already_placed_cells);
                            if (cell != null && cell.IsMember == false)
                            {
                                already_placed_cells.Remove(cell);
                                if (bAddGroupCells == false)
                                    index = already_placed_cells.Count; // ʹ�����н���
                            }
                        }
                        foreach (string refid in refids)
                        {
                            Cell cell = FindCellByRefID(refid, exist_cells);
                            if (cell != null && cell.IsMember == false)
                                exist_cells.Remove(cell);
                        }
                        continue;
                    }
                }
#endif

                this.OrderItems.Add(order);
            }

            this.OrderInfoLoaded = true;
            return 0;
        }


        // ���ݶ�����Ϣ���Ÿ���
        // TODO: ������Ѿ����ŵ������з��֣��������Ӳɹ���Ϣ���Ӷ��ѣ��������°���
        // parameters:
        int PlaceCellsByOrderInfo(
            bool bAddGroupCells,
            List<Cell> already_placed_cells,
            ref List<Cell> exist_cells,
            ref int index,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // Ȼ�󰲷ŵ���

            // ����Xml��<orderInfo>Ԫ�����ݣ��������û�е����Ԥ��ᣬ׷�����ұ�
            // TODO: �����Ƿ��һ���㶨��λ�ÿ�ʼ׷��? 
            XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*");

            if (nodes.Count == 0
                && this.OrderInfoLoaded == false)
            {
                // ��Ҫ���ⲿ��òɹ���Ϣ
                GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
                e1.BiblioRecPath = "";
                e1.PublishTime = this.PublishTime;
                if (this.Container.m_bHideLockedOrderGroup == true)
                    e1.LibraryCodeList = this.Container.LibraryCodeList;
                this.Container.DoGetOrderInfo(this, e1);
                if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
                {
                    strError = "�ڻ�ȡ�����ڳ�������Ϊ '" + this.PublishTime + "' �Ķ�����Ϣ�Ĺ����з�������: " + e1.ErrorInfo;
                    return -1;
                }

                if (e1.OrderXmls.Count == 0)
                {
                    this.OrderInfoLoaded = true;
                }
                else
                {
                    XmlNode root = this.dom.DocumentElement.SelectSingleNode("orderInfo");
                    if (root == null)
                    {
                        root = this.dom.CreateElement("orderInfo");
                        this.dom.DocumentElement.AppendChild(root);
                    }
                    for (int i = 0; i < e1.OrderXmls.Count; i++)
                    {
                        XmlDocument whole_dom = new XmlDocument();
                        try
                        {
                            whole_dom.LoadXml(e1.OrderXmls[i]);
                        }
                        catch (Exception ex)
                        {
                            strError = "������¼�� " + i.ToString() + " ��XMLװ��DOMʱ����: " + ex.Message;
                            return -1;
                        }
                        XmlNode node = this.dom.CreateElement("root");
                        root.AppendChild(node);
                        node.InnerXml = whole_dom.DocumentElement.InnerXml;
                    }
                    nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*");
                }
            }

            string strVolumeString =
    VolumeInfo.BuildItemVolumeString(
    IssueUtil.GetYearPart(this.PublishTime),
    this.Issue,
            this.Zong,
            this.Volume);

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                OrderBindingItem order = new OrderBindingItem();
                nRet = order.Initial(node.OuterXml, out strError);
                if (nRet == -1)
                    return -1;

                if (this.Container.m_bHideLockedOrderGroup == true)
                {
                    // �۲�һ���ݲط����ַ����������Ƿ񲿷��ڵ�ǰ�û���Ͻ��Χ��
                    // return:
                    //      -1  ����
                    //      0   û���κβ����ڹ�Ͻ��Χ
                    //      1   ���ٲ����ڹ�Ͻ��Χ��
                    nRet = Global.DistributeCross(order.Distribute,
                        this.Container.LibraryCodeList,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                    {
                        List<string> refids = null;
                        // ���һ���ݲط����ַ������������refid
                        nRet = Global.GetRefIDs(order.Distribute,
            out refids,
            out strError);
                        if (nRet == -1)
                            return -1;
                        foreach (string refid in refids)
                        {
                            Cell cell = FindCellByRefID(refid, already_placed_cells);
                            if (cell != null && cell.IsMember == false)
                            {
                                already_placed_cells.Remove(cell);
                                if (cell.item != null)
                                    this.Container.m_hideitems.Add(cell.item);

                                if (bAddGroupCells == false)
                                    index = already_placed_cells.Count; // ʹ�����н���
                            }
                        }
                        foreach (string refid in refids)
                        {
                            Cell cell = FindCellByRefID(refid, exist_cells);
                            if (cell != null && cell.IsMember == false)
                            {
                                exist_cells.Remove(cell);
                                if (cell.item != null)
                                    this.Container.m_hideitems.Add(cell.item);
                            }
                        }
                        continue;
                    }
                }

                this.OrderItems.Add(order);
            }

            for (int i = 0; i < this.OrderItems.Count; i++)
            {
                /*
                XmlNode node = nodes[i];

                OrderBindingItem order = new OrderBindingItem();
                nRet = order.Initial(node.OuterXml, out strError);
                if (nRet == -1)
                    return -1;

                this.OrderItems.Add(order);
                 * */
                OrderBindingItem order = this.OrderItems[i];

                GroupCell group_head = null;
                if (bAddGroupCells == true)
                {
                    // ���ȴ���һ��GroupCell����
                    group_head = new GroupCell();
                    group_head.order = order;

                    this.SetCell(index++, group_head);
                }

                // 
                string strOldCopy = GetOldValue(order.Copy);
                int nOldCopy = GetNumberValue(strOldCopy);

                string strNewCopy = GetNewValue(order.Copy);
                int nNewCopy = GetNumberValue(strNewCopy);



                LocationCollection locations = new LocationCollection();
                nRet = locations.Build(order.Distribute,
                    out strError);
                if (nRet == -1)
                    return -1;

                int nArriveCount = 0;

                for (int j = 0; j < Math.Max(nOldCopy, locations.Count); j++)
                {
                    if (nArriveCount >= nNewCopy
                        && nNewCopy > nOldCopy)
                        break;  // ע�����location�е���������Ҳ�޷��ﵽ����nAriiveCount��ô����ѵ������ôδ���ü���������Щ�����ֻ�ñ������ƻ����������

                    // 2010/4/28
                    if (nArriveCount >= nNewCopy
    && j >= nOldCopy)
                        break;


                    string strLocationName = "";
                    string strLocationRefID = "";

                    if (j < locations.Count)
                    {
                        Location location = locations[j];
                        strLocationName = location.Name;
                        strLocationRefID = location.RefID;
                    }

                    bool bOutOfControl = false;
                    if (Global.IsGlobalUser(this.Container.LibraryCodeList) == false)
                    {
                        string strLibraryCode = "";
                        string strPureName = "";

                        // ����
                        Global.ParseCalendarName(strLocationName,
                    out strLibraryCode,
                    out strPureName);
                        if (StringUtil.IsInList(strLibraryCode, this.Container.LibraryCodeList) == false)
                            bOutOfControl = true;
                    }

                    // û��refid
                    if (String.IsNullOrEmpty(strLocationRefID) == true
                        && this.Virtual == false)
                    {
                        // Ԥ�����

                        // ���Ǿ�����exist_cells�в��ҡ���Ϊ�������Ա�����󱻷���������ⲿ����ʧЧ������
                        Cell cell = FindGroupMemberCell(exist_cells, i, j);

                        /*
                        // �����޸������У����ն�����Ϣ��Ԫ�����Ѱ��
                        if (cell == null && this.Virtual == true)
                        {
                            cell = FindGroupMemberCell(exist_cells, order);
                            if (cell != null)
                            {
                                cell.item.OrderInfoPosition = new Point(i, j);
                            }
                        }
                         * */

                        if (cell == null)
                        {
                            cell = new Cell();
                            cell.item = new ItemBindingItem();
                            cell.item.Container = this;
                            cell.item.Initial("<root />", out strError);
                            cell.item.RefID = strLocationRefID;
                            cell.item.LocationString = strLocationName;
                            cell.item.Locked = bOutOfControl;
                            cell.item.Calculated = true;
                            SetFieldValueFromOrderInfo(
                                false,
                                cell.item,
                                order);
                            cell.item.OrderInfoPosition = new Point(i, j);
                            // �����Ҫ��������ʱ��
                            if (String.IsNullOrEmpty(cell.item.PublishTime) == true)
                                cell.item.PublishTime = this.PublishTime;
                            if (String.IsNullOrEmpty(cell.item.Volume) == true)
                                cell.item.Volume = strVolumeString;
                        }
                        else
                        {
                            Debug.Assert(cell.item.OrderInfoPosition.X == i
                                && cell.item.OrderInfoPosition.Y == j, "");

                            // 2010/4/28
                            cell.item.LocationString = strLocationName;
                            cell.item.Locked = bOutOfControl;

                            if (cell.item != null)
                                cell.item.Container = this;
                            exist_cells.Remove(cell);   // ���ź�ʹ���ʱ����������
                        }

                        this.SetCell(index++, cell);

                        if (group_head != null)
                            group_head.MemberCells.Add(cell);
                    }
                    else if (String.IsNullOrEmpty(strLocationRefID) == true
    && this.Virtual == true)
                    {
                        // 2012/5/16
                        // û��refid
                        // �������Ѿ�����ĸ���

                        // �ӵ���ǰ�Ѿ����ŵ��б��в���
                        Cell cell = FindGroupMemberCell(already_placed_cells, order, strLocationName);
                        if (cell != null)
                        {
                            // �Ѿ����ţ�������Ӳɹ���Ϣ
                            cell.item.OrderInfoPosition = new Point(i, j);
                            exist_cells.Remove(cell);   // ���ź�ʹ���ʱ����������

                            if (group_head != null)
                                group_head.MemberCells.Add(cell);

                            {
                                string strTempRefID = cell.item.RefID;
                                string strTempLocationName = "";
                                nRet = order.DoAccept(cell.item.OrderInfoPosition.Y,
    ref strTempRefID,
    out strTempLocationName,
    out strError);
                                /*
                                if (nRet == -1)
                                    return -1;
                                 * */
                            }
                            nArriveCount++;
                            continue;
                        }

                        // ����refid��exist_cells�в���
                        cell = FindGroupMemberCell(exist_cells, order, strLocationName);
                        if (cell == null)
                        {
                            /*
                            // �ڶ�����Ϣ�б����ﵽ��������Ŀǰ�����Ѿ�Deleted
                            cell = new Cell();
                            cell.item = new ItemBindingItem();
                            cell.item.Container = this;
                            cell.item.Initial("<root />", out strError);
                            cell.item.RefID = strLocationRefID;
                            cell.item.LocationString = strLocationName;
                            cell.item.Deleted = true;
                            SetFieldValueFromOrderInfo(
                                false,
                                cell.item,
                                order);
                            cell.item.OrderInfoPosition = new Point(i, j);
                            // �����Ҫ��������ʱ��
                            if (String.IsNullOrEmpty(cell.item.PublishTime) == true)
                                cell.item.PublishTime = this.PublishTime;
                            if (String.IsNullOrEmpty(cell.item.Volume) == true)
                                cell.item.Volume = strVolumeString;
                            this.SetCell(index++, cell);
                             * */
                            cell = new Cell();
                            cell.item = new ItemBindingItem();
                            cell.item.Container = this;
                            cell.item.Initial("<root />", out strError);
                            cell.item.RefID = strLocationRefID;
                            cell.item.LocationString = strLocationName;
                            cell.item.Locked = bOutOfControl;
                            cell.item.Calculated = true;
                            SetFieldValueFromOrderInfo(
                                false,
                                cell.item,
                                order);
                            cell.item.OrderInfoPosition = new Point(i, j);
                            // �����Ҫ��������ʱ��
                            if (String.IsNullOrEmpty(cell.item.PublishTime) == true)
                                cell.item.PublishTime = this.PublishTime;
                            if (String.IsNullOrEmpty(cell.item.Volume) == true)
                                cell.item.Volume = strVolumeString;
                            this.SetCell(index++, cell);

                        }
                        else
                        {
                            // �Ѿ����ڣ�ֱ�Ӱ���
                            this.SetCell(index++, cell);
                            if (cell.item != null)
                                cell.item.Container = this;

                            cell.item.OrderInfoPosition = new Point(i, j);
                            exist_cells.Remove(cell);   // ���ź�ʹ���ʱ����������

                            {
                                string strTempRefID = cell.item.RefID;
                                string strTempLocationName = "";
                                nRet = order.DoAccept(cell.item.OrderInfoPosition.Y,
    ref strTempRefID,
    out strTempLocationName,
    out strError);
                                /*
                                if (nRet == -1)
                                    return -1;
                                 * */
                            } 
                            nArriveCount++;
                        }

                        if (group_head != null)
                            group_head.MemberCells.Add(cell);
                    }
                    else
                    {
                        // ��refid
                        // �Ѿ�����ĸ���
                        Debug.Assert(String.IsNullOrEmpty(strLocationRefID) == false, "");

                        nArriveCount++;

                        // �ӵ���ǰ�Ѿ����ŵ��б��в���
                        Cell cell = FindCellByRefID(strLocationRefID, already_placed_cells);
                        if (cell != null)
                        {
                            // �Ѿ����ţ�������Ӳɹ���Ϣ
                            cell.item.OrderInfoPosition = new Point(i, j);

                            cell.item.Locked = bOutOfControl;

                            exist_cells.Remove(cell);   // ���ź�ʹ���ʱ����������

                            if (group_head != null)
                                group_head.MemberCells.Add(cell);
                            continue;
                        }

                        // ����refid��exist_cells�в���
                        cell = FindCellByRefID(strLocationRefID, exist_cells);

                        // 2012/9/25
                        // �����صļ���������
                        if (cell == null && this.Container.m_bHideLockedOrderGroup == false)
                        {
                            ItemBindingItem item = BindingControl.FindItemByRefID(strLocationRefID, this.Container.m_hideitems);
                            if (item != null)
                            {
                                this.Container.m_hideitems.Remove(item);
                                cell = new Cell();
                                cell.item = item;
                                cell.item.RefID = strLocationRefID;
                                cell.item.LocationString = strLocationName;
                                cell.item.Deleted = false;
                                SetFieldValueFromOrderInfo(
                                    false,
                                    cell.item,
                                    order);
                                cell.item.OrderInfoPosition = new Point(i, j);
                                // �����Ҫ��������ʱ��
                                if (String.IsNullOrEmpty(cell.item.PublishTime) == true)
                                    cell.item.PublishTime = this.PublishTime;
                                if (String.IsNullOrEmpty(cell.item.Volume) == true)
                                    cell.item.Volume = strVolumeString;

                                cell.ParentItem = cell.item.ParentItem;

                                // TODO: �����Ҫ����parentitem������
                            }
                        }

                        if (cell == null)
                        {
                            // �ڶ�����Ϣ�б����ﵽ��������Ŀǰ�����Ѿ�Deleted
                            cell = new Cell();
                            cell.item = new ItemBindingItem();
                            cell.item.Container = this;
                            cell.item.Initial("<root />", out strError);
                            cell.item.RefID = strLocationRefID;
                            cell.item.LocationString = strLocationName;
                            cell.item.Locked = bOutOfControl;
                            cell.item.Deleted = true;
                            SetFieldValueFromOrderInfo(
                                false,
                                cell.item,
                                order);
                            cell.item.OrderInfoPosition = new Point(i, j);
                            // �����Ҫ��������ʱ��
                            if (String.IsNullOrEmpty(cell.item.PublishTime) == true)
                                cell.item.PublishTime = this.PublishTime;
                            if (String.IsNullOrEmpty(cell.item.Volume) == true)
                                cell.item.Volume = strVolumeString;
                            this.SetCell(index++, cell);

                            // ����������ȥ��
                            // "*"��һ�������refID��������Щ�����ڵļ�¼�����û�б�Ҫȥ��refidΪ��*���ļ�¼����Ϊ����������ʲô�������Ҫȥ�ң�Ҳ�ᷢ�ֺܶ��ظ���
                            if (strLocationRefID != "*")
                            {
                                Cell cellTemp = this.Container.FindCellByRefID(strLocationRefID, this);
                                if (cellTemp != null)
                                {
                                    Debug.Assert(cellTemp.item != null, "");
                                    // ��������㹻����Ϣ�ˡ������Ƿ�����ƶ��Ǹ��������?
                                    nRet = cell.item.Initial(cellTemp.item.Xml, out strError);
                                    if (nRet == -1)
                                    {
                                        string strTemp = "";
                                        cell.item.Initial("<root />", out strTemp);
                                        cell.item.State = strError;
                                    }
                                    else
                                    {
                                        cell.item.Comment += "\r\n����: �ڱ��ֵ��������ڷ����˱���";
                                    }
                                }
                            }
                        }
                        else
                        {
                            // �Ѿ����ڣ�ֱ�Ӱ���
                            this.SetCell(index++, cell);
                            if (cell.item != null)
                                cell.item.Container = this;

                            cell.item.Locked = bOutOfControl;

                            cell.item.OrderInfoPosition = new Point(i, j);
                            exist_cells.Remove(cell);   // ���ź�ʹ���ʱ����������
                        }

                        if (group_head != null)
                            group_head.MemberCells.Add(cell);
                    }
                }

                if (bAddGroupCells == true)
                {
                    // ��󴴽�һ�����������ŵ�GroupCell����
                    GroupCell group = new GroupCell();
                    group.order = null;
                    group.EndBracket = true;

                    this.SetCell(index++, group);
                }
            }

            return 0;
        }

        static int IndexOf(List<ItemAndCol> infos,
            ItemBindingItem item)
        {
            for (int i = 0; i < infos.Count; i++)
            {
                if (infos[i].item == item)
                    return i;
            }

            return -1;
        }

        static void Remove(ref List<ItemAndCol> infos,
    ItemBindingItem item)
        {
            for (int i = 0; i < infos.Count; i++)
            {
                if (infos[i].item == item)
                {
                    infos.RemoveAt(i);
                    return;
                }
            }
        }

        static Cell FindCell(List<Cell> cells,
            ItemBindingItem item)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (cell == null)
                    continue;
                if (cell.item == item)
                    return cell;
            }

            return null;
        }

        // ��ñ��ù�λ�������ұߵĵ�һ���յ�λ��
        static int GetRightUseableCol(List<ItemAndCol> infos)
        {
            int nCol = 0;
            for (int j = 0; j < infos.Count; j++)
            {
                ItemAndCol info = infos[j];
                if (info.Index == -1)
                    continue;

                if (nCol <= info.Index + 1)
                {
                    nCol = info.Index + 1 + 1;
                }
            }

            return nCol;
        }

        // ���һ��û�б��ù��Ŀյ�λ��
        static int GetUseableCol(List<ItemAndCol> infos,
            int nCellCount,
            int nStartIndex)
        {
            Debug.Assert(nCellCount == 1 || nCellCount == 2, "");
            for (int index = nStartIndex; ; index++)
            {
                bool bExist = false;
                for (int j = 0; j < infos.Count; j++)
                {
                    ItemAndCol info = infos[j];
                    if (info.Index == -1)
                        continue;
                    if (nCellCount == 2)
                    {
                        if (info.Index == index
                            || info.Index == index + 1)
                        {
                            bExist = true;
                            break;
                        }
                        if (info.Index+1 == index
                            || info.Index+1 == index + 1)
                        {
                            bExist = true;
                            break;
                        }
                        continue;
                    }
                    Debug.Assert(nCellCount == 1, "");
                    if (info.Index == index
                        || info.Index + 1== index)
                    {
                        bExist = true;
                        break;
                    }
                }

                if (bExist == false)
                    return index;
            }

        }

        // parameters:
        //      index   ��infos���Ҳ����Ķ�����index��ȷ������λ��
        void PlaceCells(
            List<ItemAndCol> infos,
            ref List<Cell> exist_cells,
            ref int index)
        {
            int nStartIdex = index;
            List<int> used_indexs = new List<int>();

            for (int i = 0; i < exist_cells.Count; i++)
            {
                Cell cell = exist_cells[i];
                if (cell == null)
                    continue;
                // ���һ�������Ƿ�Ϊ�϶���Ա?
                if (cell.ParentItem != null)
                {
                    int nCol = -1;
                    int nInfoIndex = IndexOf(infos, cell.ParentItem);
                    if (nInfoIndex != -1)
                    {
                        ItemAndCol info = infos[nInfoIndex];
                        if (info.Index == -1)
                        {
                            // �ҵ�һ�����õ�λ��
                            nCol = GetUseableCol(infos,
                                2,
                                0);
                            info.Index = nCol;  // ���뵽���棬�Ա������Զ��ܿ�
                        }
                        else
                            nCol = info.Index;
                    }
                    else
                    {
                        nCol = index++;
                        index++;
                    }

                    this.SetCell(nCol + 1, cell);
                    if (cell.item != null)
                        cell.item.Container = this;
                    exist_cells.Remove(cell);   // ���ź�ʹ���ʱ����������
                    i--;

                    // �����϶����Ƿ������ڱ���λ��?
                    if (cell.ParentItem.MemberCells.Count > 0
                        && cell.ParentItem.MemberCells[0] == cell)
                    {
                        Cell parent_cell = FindCell(exist_cells, cell.ParentItem);
                        Debug.Assert(parent_cell != null, "��һ����Ա��Ȼ�ͺ϶��������ͬһ��");
                        if (parent_cell != null)
                        {
                            this.SetCell(nCol, parent_cell);
                            if (parent_cell.item != null)
                                parent_cell.item.Container = this;
                            // �Ѿ��Ѻ϶�������Ҳ�����ˣ����Դ�����������
                            exist_cells.Remove(parent_cell);
                        }
                    }
                }
                else if (cell.item != null && cell.item.IsParent == true)
                {
                    int nCol = -1;
                    int nInfoIndex = IndexOf(infos, cell.item);
                    if (nInfoIndex != -1)
                    {
                        ItemAndCol info = infos[nInfoIndex];
                        if (info.Index == -1)
                        {
                            // �ҵ�һ�����õ�λ��
                            nCol = GetUseableCol(infos,
                                2,
                                0);
                            info.Index = nCol;  // ���뵽���棬�Ա������Զ��ܿ�
                        }
                        else
                            nCol = info.Index;
                    }
                    else
                    {
                        nCol = index++;
                        index++;
                    }

                    this.SetCell(nCol, cell);
                    if (cell.item != null)
                        cell.item.Container = this;
                    exist_cells.Remove(cell);   // ���ź�ʹ���ʱ����������
                    i--;

                    // �����Ű��ŵ�һ����Ա��
                    if (cell.item.MemberCells.Count > 0)
                    {
                        this.SetCell(nCol + 1, cell.item.MemberCells[0]);
                        // �Ѿ��Ѻ϶�������Ҳ�����ˣ����Դ�����������
                        exist_cells.Remove(cell.item.MemberCells[0]);
                    }

                }
            }
            
            int temp = GetRightUseableCol(infos);
            if (temp > index)
                index = temp;
        }

        // parameters:
        //      strDistribute1  ��Ҫ��
        //      strDistribute2  ��Ҫ�ġ���strDistribute1��û��refid��λ�ã�������Ķ�Ӧλ�����
        static int MergeDistribute(string strDistribute1,
            string strDistribute2,
            out string strResult,
            out string strError)
        {
            strResult = "";
            strError = "";

            LocationCollection locations1 = new LocationCollection();
            int nRet = locations1.Build(strDistribute1,
                out strError);
            if (nRet == -1)
                return -1;

            LocationCollection locations2 = new LocationCollection();
            nRet = locations2.Build(strDistribute2,
                out strError);
            if (nRet == -1)
                return -1;

            LocationCollection locations3 = new LocationCollection();

            for(int i=0;i<locations2.Count;i++)
            {
                Location location2 = locations2[i];
                Location location1 = null;
                
                if (locations1.Count > i)
                    location1 = locations1[i];

                if (location1 == null || String.IsNullOrEmpty(location1.RefID) == true)
                    locations3.Add(location2);
                else
                    locations3.Add(location1);
            }

            for (int i = locations2.Count; i < locations1.Count; i++)
            {
                Location location1 = locations1[i];
                locations3.Add(location1);
            }

            strResult = locations3.ToString(true);
            return 0;
        }

        // ˢ�¶�����Ϣ
        public int RefreshOrderInfo(out string strError)
        {
            strError = "";
            int nRet = 0;

            XmlNodeList exist_nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*");


            List<string> exist_xmls = new List<string>();   // ˢ��ǰ�Ѿ����ڵ�XMLƬ��
            List<string> exist_refids = new List<string>(); // ��ЩXMLƬ�ϵ�refid�ַ���
            foreach (XmlNode node in exist_nodes)
            {
                exist_xmls.Add(node.InnerXml);
                string strRefID = DomUtil.GetElementText(node, "refID");
                exist_refids.Add(strRefID);
            }

            {
                // ��Ҫ���ⲿ��òɹ���Ϣ
                GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
                e1.BiblioRecPath = "";
                e1.PublishTime = this.PublishTime;
                if (this.Container.m_bHideLockedOrderGroup == true)
                    e1.LibraryCodeList = this.Container.LibraryCodeList;
                this.Container.DoGetOrderInfo(this, e1);
                if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
                {
                    strError = "�ڻ�ȡ�����ڳ�������Ϊ '" + this.PublishTime + "' �Ķ�����Ϣ�Ĺ����з�������: " + e1.ErrorInfo;
                    return -1;
                }

                {
                    XmlNode root = this.dom.DocumentElement.SelectSingleNode("orderInfo");
                    if (root == null)
                    {
                        root = this.dom.CreateElement("orderInfo");
                        this.dom.DocumentElement.AppendChild(root);
                    }

                    root.InnerXml = ""; // ɾ��ԭ�е��¼�Ԫ��

                    this.Changed = true;

                    for (int i = 0; i < e1.OrderXmls.Count; i++)
                    {
                        XmlDocument whole_dom = new XmlDocument();
                        try
                        {
                            whole_dom.LoadXml(e1.OrderXmls[i]);
                        }
                        catch (Exception ex)
                        {
                            strError = "������¼�� " + i.ToString() + " ��XMLװ��DOMʱ����: " + ex.Message;
                            return -1;
                        }

                        XmlNode node = null;
                        node = this.dom.CreateElement("root");
                        root.AppendChild(node);

                        string strRefID = DomUtil.GetElementText(whole_dom.DocumentElement, "refID");
                        int index = exist_refids.IndexOf(strRefID);

                        // ��ǰ����
                        if (index != -1)
                        {
                            node.InnerXml = exist_xmls[index];
                            // �����޸�<copy>�����oldvalue���֣�����<distribute>
                            // 
                            string strCopy = DomUtil.GetElementText(node, "copy");
                            string strNewValue = "";
                            string strOldValue = "";
                            OrderDesignControl.ParseOldNewValue(strCopy,
                                out strOldValue,
                                out strNewValue);

                            string strDistribute = DomUtil.GetElementText(node, "distribute");

                            // ��Ϊˢ�µ�����
                            node.InnerXml = whole_dom.DocumentElement.InnerXml;

                            string strNewDistribute = DomUtil.GetElementText(node, "distribute");

                            string strMerged = "";
                            nRet = MergeDistribute(strDistribute,
                                strNewDistribute,
                                out strMerged,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            DomUtil.SetElementText(node, "distribute", strMerged);
                            strCopy = DomUtil.GetElementText(node, "copy");
                            string strNewValue1 = "";
                            string strOldValue1 = "";
                            OrderDesignControl.ParseOldNewValue(strCopy,
                                out strOldValue1,
                                out strNewValue1);
                            DomUtil.SetElementText(node, "copy",
                                OrderDesignControl.LinkOldNewValue(strOldValue1,
                                strNewValue));

                            /*
                            // �õ�һ�����������λ��
                            exist_refids[index] = "";
                            exist_xmls[index] = "";
                             * */
                        }
                        else
                        {
                            // ˢ�º�������
                            node.InnerXml = whole_dom.DocumentElement.InnerXml;
                        }
                    }

                    // TODL: ˢ�º󣬱�ԭ���ٵ�?


                    /*
                    XmlNodeList nodes = null;

                    nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*");
                     * */
                }
                this.OrderInfoLoaded = true;
            }

            if (this.IssueLayoutState == IssueLayoutState.Accepting)
            {
                return LayoutAccepting(out strError);
            }
            else
            {
                return ReLayoutBinding(out strError);
            }
        }

        // ����װ��ģʽ(����)������ʾ
        public int ReLayoutBinding(out string strError)
        {
            strError = "";
            int nRet = 0;

            // this.OrderItems.Clear();
            this.ClearOrderItems();

            // ���������е�Cell����㼯��һ�𣬴���
            List<Cell> exist_cells = new List<Cell>();
            exist_cells.AddRange(this.Cells);

            // ���Cells����
            this.Cells.Clear();

            int index = 0;  // �������������±�

            // TODO: ��׼���ñ���Խ��λ�úͺ϶�������Ϣ��Ȼ��ӱ��������ʵ��ĵ�Ԫ
            // ȥ������Щλ�ã��Ҳ����ĵ�Ԫ���򰲷ſյĳ�Ա����

            // �ҵ������и��ѵĺ϶���
            List<ItemAndCol> crossed_infos = null;
            nRet = this.Container.GetCrossBoundRange(this,
                false,  // ������⣬��Ҫ���ؾ����parent items
                out crossed_infos);

            List<Cell> crossed_cells = new List<Cell>();

            // ������ɾ����ʣ�µľ��Ǳ���Խ�Ŀհ�λ��
            List<ItemAndCol> crossed_blank_infos = new List<ItemAndCol>();
            crossed_blank_infos.AddRange(crossed_infos);

            // ��exist_cell�з�ѡ����Խλ�õĶ���
            if (crossed_blank_infos.Count > 0)
            {
                // �Ȱ����ڱ��������ҵ��ĳ�Ա
                for (int i = 0; i < exist_cells.Count; i++)
                {
                    Cell cell = exist_cells[i];
                    if (cell == null)
                        continue;
                    // ���һ�������Ƿ�Ϊ�϶���Ա?
                    if (cell.ParentItem != null
                        && IndexOf(crossed_blank_infos, cell.ParentItem) != -1)
                    {
                        Remove(ref crossed_blank_infos, cell.ParentItem);

                        crossed_cells.Add(cell);

                        exist_cells.Remove(cell);
                        i--;

                        // �����϶����Ƿ������ڱ���λ��?
                        if (cell.ParentItem.MemberCells.Count > 0
                            && cell.ParentItem.MemberCells[0] == cell)
                        {
                            Cell parent_cell = FindCell(exist_cells, cell.ParentItem);
                            Debug.Assert(parent_cell != null, "��һ����Ա��Ȼ�ͺ϶��������ͬһ��");
                            if (parent_cell != null)
                            {
                                crossed_cells.Add(parent_cell);
                                // �Ѿ��Ѻ϶�������Ҳ�����ˣ����Դ�����������
                                exist_cells.Remove(parent_cell);
                            }
                        }
                    }
                    else if (cell.item != null && cell.item.IsParent == true
                        && IndexOf(crossed_blank_infos, cell.item) != -1)
                    {
                        Remove(ref crossed_blank_infos, cell.item);

                        crossed_cells.Add(cell);

                        exist_cells.Remove(cell);
                        i--;

                        // �����Ű��ŵ�һ����Ա��
                        if (cell.item.MemberCells.Count > 0)
                        {
                            Debug.Assert(cell.item.MemberCells[0] != null, "");

                            crossed_cells.Add(cell.item.MemberCells[0]);
                            // �Ѿ��Ѻ϶�������Ҳ�����ˣ����Դ�����������
                            exist_cells.Remove(cell.item.MemberCells[0]);
                        }
                    }
                }
            }

            // ���ſ�Խλ�õĶ���
            if (crossed_cells.Count > 0)
            {
                PlaceCells(
                    crossed_infos,
                    ref crossed_cells,
                    ref index);
            }

            // Ȼ��ʣ�µı���Խ�Ĳ�λ���ſհ׸���
            if (crossed_blank_infos.Count > 0)
            {
                for (int i = 0; i < crossed_blank_infos.Count; i++)
                {
                    ItemAndCol info = crossed_blank_infos[i];

                    // �ҵ����еĺ��ʵ�nColλ�ã���������
                    // �����ʱû�к��ʵ�λ�ã���˳�ΰ���
                    int nCol = info.Index;
                    if (nCol == -1)
                    {
                        // �ҵ�һ�����õ�λ��
                        nCol = GetUseableCol(crossed_infos,
                            2,
                            0);
                        info.Index = nCol;  // ���뵽���棬�Ա������Զ��ܿ�
                    }

                    // �����հ׸���
                    Cell cell = new Cell();
                    cell.item = null;
                    cell.ParentItem = info.item;
                    this.SetCell(nCol + 1, cell);
                }

                int temp = GetRightUseableCol(crossed_infos);
                if (temp > index)
                    index = temp;

            }

            // Ȼ�󰲷�ʣ�µĳ�Ա��϶������
            if (exist_cells.Count > 0)
            {
                PlaceCells(
                    crossed_infos,
                    ref exist_cells,
                    ref index);
            }

            Debug.Assert(index >= this.Cells.Count, "");
            if (index < this.Cells.Count)
                index = this.Cells.Count;

            // ���ݶ�����Ϣ���ŵ������
            nRet = PlaceCellsByOrderInfo(
                false,
                this.Cells,
                ref exist_cells,
                ref index,
                out strError);
            if (nRet == -1)
                return -1;

            // �����ʣ�µĵ�Ԫ����ĩβ
            if (exist_cells.Count > 0)
            {
                if (this.Cells.Count > 0)
                {
                    // �Ȳ���һ���ָ��null����ʾ���ж�����Ϣ�ĸ���
                    this.SetCell(index++, null);
                }

                for (int i = 0; i < exist_cells.Count; i++)
                {
                    Cell cell = exist_cells[i];

                    if (cell == null)
                        continue;
                    if (cell is GroupCell)
                        continue;
                    if (cell.item == null)
                        continue;
                    if (cell.item != null && cell.item.Calculated == true)
                        continue;

                    this.SetCell(index++, cell);
                    if (cell.item != null)
                        cell.item.Container = this;

                    exist_cells.RemoveAt(i);
                    i--;
                }

                // ����ʣ�µĶ���
                for (int i = 0; i < exist_cells.Count; i++)
                {
                    Cell cell = exist_cells[i];

                    if (cell == null)
                        continue;
                    cell.Container = null;
                    if (cell.item != null)
                        cell.item.Container = null;
                }
            }

            this.RefreshAllOutofIssueValue();

#if DEBUG
            this.Container.VerifyAll();
#endif
            return 0;
        }

        // ����װ��ģʽ(�״�)������ʾ
        public int InitialLayoutBinding(out string strError)
        {
            strError = "";
            int nRet = 0;

            // this.OrderItems.Clear();
            this.ClearOrderItems();

            List<Cell> exist_cells = new List<Cell>();
            int index = this.Cells.Count;  // �������������±�

            // ���ݶ�����Ϣ���Ÿ���
            nRet = PlaceCellsByOrderInfo(
                false,
                this.Cells,
                ref exist_cells,
                ref index,
                out strError);
            if (nRet == -1)
                return -1;

            /*
            // ����Xml��<orderInfo>Ԫ�����ݣ��������û�е����Ԥ��ᣬ׷�����ұ�
            // TODO: �����Ƿ��һ���㶨��λ�ÿ�ʼ׷��? 
            XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*");

            if (nodes.Count == 0
                && this.OrderInfoLoaded == false)
            {
                // ��Ҫ���ⲿ��òɹ���Ϣ
                GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
                e1.BiblioRecPath = "";
                e1.PublishTime = this.PublishTime;
                if (this.Container.m_bHideLockedOrderGroup == true)
                    e1.LibraryCodeList = this.Container.LibraryCodeList;
                this.Container.DoGetOrderInfo(this, e1);
                if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
                {
                    strError = "�ڻ�ȡ�����ڳ�������Ϊ '" + this.PublishTime + "' �Ķ�����Ϣ�Ĺ����з�������: " + e1.ErrorInfo;
                    return -1;
                }

                if (e1.OrderXmls.Count == 0)
                {
                    this.OrderInfoLoaded = true;
                }
                else
                {
                    XmlNode root = this.dom.DocumentElement.SelectSingleNode("orderInfo");
                    if (root == null)
                    {
                        root = this.dom.CreateElement("orderInfo");
                        this.dom.DocumentElement.AppendChild(root);
                    }
                    for (int i = 0; i < e1.OrderXmls.Count; i++)
                    {
                        XmlDocument whole_dom = new XmlDocument();
                        try
                        {
                            whole_dom.LoadXml(e1.OrderXmls[i]);
                        }
                        catch (Exception ex)
                        {
                            strError = "������¼�� " + i.ToString() + " ��XMLװ��DOMʱ����: " + ex.Message;
                            return -1;
                        }
                        XmlNode node = this.dom.CreateElement("root");
                        root.AppendChild(node);
                        node.InnerXml = whole_dom.DocumentElement.InnerXml;
                    }
                    nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*");
                }
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                OrderBindingItem order = new OrderBindingItem();
                nRet = order.Initial(node.OuterXml, out strError);
                if (nRet == -1)
                    return -1;

                this.OrderItems.Add(order);

                LocationColletion locations = new LocationColletion();
                nRet = locations.Build(order.Distribute,
                    out strError);
                if (nRet == -1)
                    return -1;

                for (int j = 0; j < locations.Count; j++)
                {
                    Location location = locations[j];

                    if (String.IsNullOrEmpty(location.RefID) == true)
                    {
                        // Ԥ�����
                        Cell cell = new Cell();
                        cell.item = new ItemBindingItem();
                        cell.item.Container = this;
                        cell.item.Initial("<root />", out strError);
                        cell.item.RefID = location.RefID;
                        cell.item.LocationString = location.Name;
                        cell.item.Calculated = true;
                        cell.item.OrderInfoPosition = new Point(i, j);
                        this.SetCell(index++, cell);
                    }
                    else
                    {
                        // �Ѿ�����ĸ���

                        // ����refid��this.Cells�в���
                        Cell cell = FindCellByRefID(location.RefID, this.Cells);
                        if (cell == null)
                        {
                            // �ڶ�����Ϣ�б����ﵽ��������Ŀǰ�����Ѿ�Deleted
                            cell = new Cell();
                            cell.item = new ItemBindingItem();
                            cell.item.Container = this;
                            cell.item.Initial("<root />", out strError);
                            cell.item.RefID = location.RefID;
                            cell.item.LocationString = location.Name;
                            cell.item.Deleted = true;
                            cell.item.OrderInfoPosition = new Point(i, j);
                            this.SetCell(index++, cell);
                        }
                        else
                        {
                            // �Ѿ����ڣ�����
                            cell.item.OrderInfoPosition = new Point(i, j);
                        }
                    }
                }
            }
             * */

            this.RefreshAllOutofIssueValue();
            return 0;
        }

        // ���ռǵ�ģʽ(����)������ʾ
        public int LayoutAccepting(out string strError)
        {
            strError = "";
            int nRet = 0;

            // this.OrderItems.Clear();
            this.ClearOrderItems();

            // �����е�Cell��������
            List<Cell> exist_cells = new List<Cell>();
            exist_cells.AddRange(this.Cells);

            // ���Cells����
            this.Cells.Clear();

            int index = 0;  // �������������±�

            // ���ݶ�����Ϣ���Ÿ���
            nRet = PlaceCellsByOrderInfo(
                true,
                this.Cells, // ���� new List<Cells>();
                ref exist_cells,
                ref index,
                out strError);
            if (nRet == -1)
                return -1;

            /*
            // ����Xml��<orderInfo>Ԫ�����ݣ���ʼ����ȫ��Ӧ�е�λ��
            XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*");

            if (nodes.Count == 0
                && this.OrderInfoLoaded == false)
            {
                // ��Ҫ���ⲿ��òɹ���Ϣ
                GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
                e1.BiblioRecPath = "";
                e1.PublishTime = this.PublishTime;
                if (this.Container.m_bHideLockedOrderGroup == true)
                    e1.LibraryCodeList = this.Container.LibraryCodeList;
                this.Container.DoGetOrderInfo(this, e1);
                if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
                {
                    strError = "�ڻ�ȡ�����ڳ�������Ϊ '" + this.PublishTime + "' �Ķ�����Ϣ�Ĺ����з�������: " + e1.ErrorInfo;
                    return -1;
                }

                if (e1.OrderXmls.Count == 0)
                {
                    this.OrderInfoLoaded = true;
                }
                else
                {
                    XmlNode root = this.dom.DocumentElement.SelectSingleNode("orderInfo");
                    if (root == null)
                    {
                        root = this.dom.CreateElement("orderInfo");
                        this.dom.DocumentElement.AppendChild(root);
                    }
                    for (int i = 0; i < e1.OrderXmls.Count; i++)
                    {
                        XmlDocument whole_dom = new XmlDocument();
                        try
                        {
                            whole_dom.LoadXml(e1.OrderXmls[i]);
                        }
                        catch (Exception ex)
                        {
                            strError = "������¼�� " + i.ToString() + " ��XMLװ��DOMʱ����: " + ex.Message;
                            return -1;
                        }
                        XmlNode node = this.dom.CreateElement("root");
                        root.AppendChild(node);
                        node.InnerXml = whole_dom.DocumentElement.InnerXml;
                    }
                    nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*");
                }
            }


            for(int i=0;i<nodes.Count;i++)
            {
                XmlNode node = nodes[i];


                OrderBindingItem order = new OrderBindingItem();
                nRet = order.Initial(node.OuterXml, out strError);
                if (nRet == -1)
                    return -1;

                this.OrderItems.Add(order);

                // ���ȴ���һ��GroupCell����
                GroupCell group = new GroupCell();
                group.order = order;

                this.SetCell(index++, group);

                // ������������Ĳ�(Ԥ��)����
                LocationColletion locations = new LocationColletion();
                nRet = locations.Build(order.Distribute,
                    out strError);
                if (nRet == -1)
                    return -1;

                for (int j = 0; j < locations.Count; j++)
                {
                    Location location = locations[j];

                    if (String.IsNullOrEmpty(location.RefID) == true)
                    {
                        // Ԥ�����

                        // ���Ǿ�����exist_cells�в��ҡ���Ϊ�������Ա�����󱻷���������ⲿ����ʧЧ������
                        Cell cell = FindOrderedCell(exist_cells, i, j);
                        if (cell == null)
                        {

                            cell = new Cell();
                            cell.item = new ItemBindingItem();
                            cell.item.Container = this;
                            cell.item.Initial("<root />", out strError);
                            cell.item.RefID = location.RefID;
                            cell.item.LocationString = location.Name;
                            cell.item.Calculated = true;
                            cell.item.OrderInfoPosition = new Point(i, j);
                        }
                        else
                        {
                            Debug.Assert(cell.item.OrderInfoPosition.X == i
    && cell.item.OrderInfoPosition.Y == j, "");
                            exist_cells.Remove(cell);   // ���ź�ʹ���ʱ����������
                        }

                        this.SetCell(index++, cell);
                    }
                    else
                    {
                        // �Ѿ�����ĸ���

                        // ����refid��exist_cells�в���
                        Cell cell = FindCellByRefID(location.RefID, exist_cells);
                        if (cell == null)
                        {
                            // Deleted
                            cell = new Cell();
                            cell.item = new ItemBindingItem();
                            cell.item.Container = this;
                            cell.item.Initial("<root />", out strError);
                            cell.item.RefID = location.RefID;
                            cell.item.LocationString = location.Name;
                            cell.item.Deleted = true;
                            cell.item.OrderInfoPosition = new Point(i, j);
                            this.SetCell(index++, cell);
                        }
                        else
                        {
                            // ֱ�Ӳ���
                            cell.item.OrderInfoPosition = new Point(i, j);
                            this.SetCell(index++, cell);
                            exist_cells.Remove(cell);   // �ù��ľ�����
                        }
                    }
                }

                // ��󴴽�һ�����������ŵ�GroupCell����
                group = new GroupCell();
                group.order = null;
                group.EndBracket = true;

                this.SetCell(index++, group);
            }
            */

            // ��ʣ�µĵ�Ԫ����ĩβ
            if (exist_cells.Count > 0)
            {
                /*
                if (nodes.Count > 0)
                {
                    // �Ȳ���һ���ָ��null
                    this.SetCell(index++, null);
                }
                 * */

                for (int i = 0; i < exist_cells.Count; i++)
                {
                    Cell cell = exist_cells[i];
                    if (cell == null)
                        continue;
                    if (cell is GroupCell)
                        continue;
                    if (cell.item == null
                        && cell.ParentItem == null) // �϶���Ա��cell.item == null��ȻҪ���뵽���� 2010/6/5
                    {
                        continue;
                    }

                    if (cell.item != null && cell.item.Calculated == true)
                        continue;

                    this.SetCell(index++, cell);
                    if (cell.item != null)
                        cell.item.Container = this;

                    exist_cells.RemoveAt(i);
                    i--;
                }

                // ����ʣ�µĶ���
                for (int i = 0; i < exist_cells.Count; i++)
                {
                    Cell cell = exist_cells[i];

                    if (cell == null)
                        continue;
                    cell.Container = null;
                    if (cell.item != null)
                        cell.item.Container = null;
                }

                // exist_cells.Clear();    // 
            }
            this.RefreshAllOutofIssueValue();

#if DEBUG
            this.Container.VerifyAll();
#endif
            return 0;
        }



        static Cell FindCellByRefID(string strRefID,
            List<Cell> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (cell == null)
                    continue;
                if (cell.item == null)
                    continue;
                if (cell.item.RefID == strRefID)
                    return cell;
            }
            return null;
        }

        // һ������ʱ���Ƿ��ڳ���ʱ�䷶Χ��
        // ���ܻ��׳��쳣
        static bool IsInPublishTimeRange(string strPublishTime,
            string strRange)
        {
            string strError = "";
            int nRet = 0;

            if (strPublishTime.IndexOf("-") != -1)
            {
                strError = "strPublishTimeʱ���ַ��� '"+strPublishTime+"' Ӧ��Ϊ������ʽ���������÷�Χ��ʽ";
                throw new Exception(strError);
            }

            if (strRange.IndexOf("-") == -1)
            {
                strError = "strRangeʱ���ַ��� '" + strRange + "' Ӧ��Ϊ��Χ��ʽ(�������ۺ�)";
                throw new Exception(strError);
            }

            DateTime startTime = new DateTime(0);
            DateTime endTime = new DateTime(0);
            nRet = Global.ParseTimeRangeString(strRange,
                false,
                out startTime,
                out endTime,
                out strError);
            if (nRet == -1)
                throw new Exception(strError);
            DateTime testTime = DateTimeUtil.Long8ToDateTime(strPublishTime);

            if (testTime >= startTime && testTime <= endTime)
                return true;
            return false;
        }

        // �������������г�Ա��͵�����ӵ�OutofIssueֵ
        internal void RefreshAllOutofIssueValue()
        {
            for (int i = 0; i < this.Cells.Count; i++)
            {
                Cell cell = this.GetCell(i);
                if (cell == null)
                    continue;
                if (cell.item == null)
                {
                    cell.OutofIssue = false;
                    continue;
                }

                // �����϶������
                // TODO: �Ƿ���ԱȽ�һ�º϶������ڵ��ڣ�����ں϶����publishtimerange����
                if (cell.item.IsParent == true)
                {
                    try
                    {
                        if (IsInPublishTimeRange(this.PublishTime, cell.item.PublishTime) == false)
                        {
                            cell.OutofIssue = true;
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        cell.item.Comment += "\r\n����: " + ex.Message;
                        cell.OutofIssue = true;
                    }
                    continue;
                }

                if (cell.item.PublishTime != this.PublishTime)
                {
                    cell.OutofIssue = true;
                    continue;
                }

                string strIssue = "";
                string strZong = "";
                string strVolume = "";

                // ���������ںš����ںš���ŵ��ַ���
                VolumeInfo.ParseItemVolumeString(cell.item.Volume,
                    out strIssue,
                    out strZong,
                    out strVolume);

                if (strIssue != this.Issue
                    || strZong != this.Zong
                    || strVolume != this.Volume)
                    cell.OutofIssue = true;
                else
                    cell.OutofIssue = false;
            }
        }

        // ����������һ����Ա��͵�����ӵ�OutofIssueֵ
        // parameters:
        //      index ����index
        internal void RefreshOutofIssueValue(int index)
        {
            if (index >= this.Cells.Count)
                return;
            Cell cell = this.GetCell(index);
            if (cell == null)
                return;

            if (cell.item == null)
            {
                cell.OutofIssue = false;
                return;
            }

            // TODO: �Ƿ���ԱȽ�һ�º϶������ڵ��ڣ�����ں϶����publishtimerange����
            if (cell.item.IsParent == true)
            {
                try
                {
                    if (IsInPublishTimeRange(this.PublishTime, cell.item.PublishTime) == false)
                        cell.OutofIssue = true;
                    else
                        cell.OutofIssue = false;
                    return;
                }
                catch (Exception ex)
                {
                    cell.item.Comment += "\r\n����: " + ex.Message;
                }
                return;
            }

            if (cell.item.PublishTime != this.PublishTime)
            {
                cell.OutofIssue = true;
                return;
            }

            string strIssue = "";
            string strZong = "";
            string strVolume = "";

            // ���������ںš����ںš���ŵ��ַ���
            VolumeInfo.ParseItemVolumeString(cell.item.Volume,
                out strIssue,
                out strZong,
                out strVolume);

            if (strIssue != this.Issue
                || strZong != this.Zong
                || strVolume != this.Volume)
                cell.OutofIssue = true;
            else
                cell.OutofIssue = false;
        }

#if OLD_VERSION

        // ����һ��˫��λ���Ƿ�Ϊ�հ�
        public bool IsBlankPosition(int nNo,
            ItemBindingItem exclude_parent_item)
        {
            if (this.Cells.Count <= nNo*2)
                return true;
            Cell cell = this.Cells[nNo*2];
            if (cell != null)
            {
                Debug.Assert(cell.ParentItem == null, "�϶������ڸ���ParentItem����Ϊnull");

                // ����Ҫ�ų��ĺ϶���ġ�����������λ��
                if (exclude_parent_item != null
                    && exclude_parent_item == cell.ParentItem)
                    return true;

                if (cell.IsMember == true)
                    return false;

                if (cell.item != null)
                {
                    Debug.Assert(cell.item.ParentItem == null, "�϶���Item��ParentItem����Ϊnull");

                    // ����Ҫ�ų��ĺ϶���ġ�����������λ��
                    if (exclude_parent_item != null
                        && cell.item == exclude_parent_item)
                        return true;

                    return false;
                }
            }
            if (this.Cells.Count <= nNo*2 + 1)
                return true;

            cell = this.Cells[nNo*2+1];
            if (cell == null)
                return true;

            //
            if (cell.ParentItem == exclude_parent_item
                && exclude_parent_item != null)
                return true;

            if (cell.IsMember == true)
                return false;

            if (cell.item != null)
            {
                //
                if (cell.item.ParentItem == exclude_parent_item
                    && exclude_parent_item != null)
                    return true;

                return false;
            } 
            
            return true;
        }
#endif

#if OLD_VERSION

        // ����һ��˫��λ���Ƿ�Ϊ�϶�λ��
        public bool IsBindedPosition(int nNo)
        {
            if (this.Cells.Count <= nNo * 2)
                return false;
            /* �״ΰ��ŵ�ʱ����������
            Cell cell = this.Cells[nNo * 2];
            if (cell.Binded == true)
                return true;
            if (cell.item != null)
                return true;    // ����λ�ó���item
             * */
            if (this.Cells.Count <= nNo * 2 + 1)
                return false;
            Cell cell = this.Cells[nNo * 2 + 1];
            if (cell == null)
                return false;
            if (cell.IsMember == true)
                return true;
            if (cell.item != null && cell.item.IsMember == true)
                return true;
            return false;
        }

#endif

        // ̽���Ƿ�Ϊ�ʺϸ��ǵ���Ŀհ�λ��
        public bool IsBlankSingleIndex(int index)
        {
            Cell cell = this.GetCell(index);
            bool bBlank = IsBlankOrNullCell(cell);
            if (bBlank == true)
            {
                // 2010/3/6
                // ��Ҫ�жϵ�ǰλ���Ƿ�Ϊ�϶���Ա����
                if (cell != null && cell.ParentItem != null)
                    return false;

                // ��Ҫ�ж��Ҳ��Ƿ�Ϊ�϶���Ա����
                Cell cellRight = this.GetCell(index + 1);
                if (cellRight == null || cellRight.IsMember == false)
                    return true;
            }

            return false;
        }

        // ̽���Ƿ�Ϊ�ʺϸ���˫��Ŀհ�λ��
        // �򵥰汾��TODO: Ҳ����ͨ����װIsBlankDoubleIndex(int, ItemBindingItem)�汾��ʵ�֣�����������ά��һЩ
        public bool IsBlankDoubleIndex(int index)
        {
            bool bLeftBlank = IsBlankSingleIndex(index);
            if (bLeftBlank == false)
                return false;   // �Ż�
            bool bRightBlank = IsBlankSingleIndex(index + 1);
            if (bLeftBlank == true && bRightBlank == true)
                return true;
            return false;
        }

        // ��װ�汾
        public bool IsBlankDoubleIndex(int index,
            ItemBindingItem exclude_parent_item)
        {
            return this.IsBlankDoubleIndex(index, exclude_parent_item, null);
        }


        // ̽���Ƿ�Ϊ�ʺϸ���˫��Ŀհ�λ��
        // ����һ��İ汾�������ų��ض��ĺ϶�������
        public bool IsBlankDoubleIndex(int index,
            ItemBindingItem exclude_parent_item,
            ItemBindingItem exclude_member_item)
        {
            bool bLeftBlank = IsBlankSingleIndex(index);
            bool bRightBlank = IsBlankSingleIndex(index + 1);
            if (bLeftBlank == true && bRightBlank == true)
                return true;

            // ��Ҫ�����жϳ�Ա�������ĺ϶��ᣬ�Ƿ�ΪҪ�ų��Ķ���
            if (exclude_parent_item != null
                || exclude_member_item != null)
            {
                // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
                // return:
                //      -1  �ǡ�������˫������λ��
                //      0   ����
                //      1   �ǡ�������˫����Ҳ�λ��
                int nRet = IsBoundIndex(index);
                if (nRet == 0)
                {
                    // ��Ҫ�����ж�index�ұ�һ�����Ƿ�Ϊ��Ա����࣬������exclude_parent_item
                    if (exclude_parent_item != null)
                    {
                        if (this.GetIndexMemberParent(index + 1) == exclude_parent_item
                             && bLeftBlank == true)
                            return true;
                    }

                    if (exclude_member_item != null)
                    {
                        Debug.Assert(exclude_member_item != null, "");
                        if (bLeftBlank == true)
                        {
                            Cell cellTemp = this.GetCell(index + 1);
                            if (cellTemp != null && cellTemp.item == exclude_member_item)
                                return true;
                        }
                        else if (bRightBlank == true)
                        {
                            Debug.Assert(bRightBlank == true, "");
                            Cell cellTemp = this.GetCell(index);
                            if (cellTemp != null && cellTemp.item == exclude_member_item)
                                return true;
                        }
                    }

                    return false;
                }

                int nLeftIndex = -1;
                int nRightIndex = -1;
                if (nRet == -1)
                {
                    //      -1  �ǡ�������˫������λ��

                    nLeftIndex = index;
                    nRightIndex = index + 1;
                }
                else if (nRet == 1)
                {
                    //      1   �ǡ�������˫����Ҳ�λ��


                    // ��Ҫ�����ж�һ�£�index�ұ�һ�������Ƿ�Ϊ�հ�λ��
                    if (this.IsBlankSingleIndex(index + 1) == false)
                        return false;

                    nLeftIndex = index - 1;
                    nRightIndex = index;
                }
                else
                {
                    Debug.Assert(false, "");
                }

                Debug.Assert(nLeftIndex >= 0, "");
                Debug.Assert(nRightIndex >= 0, "");
                Debug.Assert(nLeftIndex + 1 == nRightIndex, "");

                Cell cellLeft = this.GetCell(nLeftIndex);
                if (cellLeft != null)
                {
                    Debug.Assert(cellLeft != null, "");
                    if (cellLeft.item != null)
                    {
                        if (exclude_parent_item != null)
                        {
                            if (cellLeft.item == exclude_parent_item)
                                return true;
                        }

                        if (exclude_member_item != null)
                        {
                            if (cellLeft.item == exclude_member_item)
                                return true;
                        }
                    }
                }

                Cell cellRight = this.GetCell(nRightIndex);
                if (cellRight != null)
                {
                    Debug.Assert(cellRight != null, "");

                    if (exclude_parent_item != null)
                    {
                        if (cellRight.ParentItem == exclude_parent_item)
                            return true;
                    }
                    if (exclude_member_item != null)
                    {
                        if (cellRight.ParentItem == exclude_member_item)
                            return true;
                    }
                }
                else
                {
                    Debug.Assert(false, "�Ҳ�ĸ��Ӳ�Ӧ����null");
                }
            }

            return false;
        }

        // ���ĳindex�����϶������
        public ItemBindingItem GetIndexMemberParent(int index)
        {
            // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
            // return:
            //      -1  �ǡ�������˫������λ��
            //      0   ����
            //      1   �ǡ�������˫����Ҳ�λ��
            int nRet = IsBoundIndex(index);
            if (nRet == 0)
                return null;
            int nLeftIndex = -1;
            int nRightIndex = -1;
            if (nRet == -1)
            {
                nLeftIndex = index;
                nRightIndex = index + 1;
            }
            else if (nRet == 1)
            {
                nLeftIndex = index - 1;
                nRightIndex = index;
            }
            else
            {
                Debug.Assert(false, "");
            }

            Debug.Assert(nLeftIndex >= 0, "");
            Debug.Assert(nRightIndex >= 0, "");
            Debug.Assert(nLeftIndex + 1 == nRightIndex, "");

            Cell cellLeft = this.GetCell(nLeftIndex);
            if (cellLeft != null)
            {
                Debug.Assert(cellLeft != null, "");
                if (cellLeft.item != null)
                {
                    return cellLeft.item;
                }
            }

            Cell cellRight = this.GetCell(nRightIndex);
            if (cellRight != null)
            {
                Debug.Assert(cellRight != null, "");
                return cellRight.ParentItem;
            }
            else
            {
                Debug.Assert(false, "�Ҳ�ĸ��Ӳ�Ӧ����null");
            }

            return null;
        }

        // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
        // return:
        //      -1  �ǡ�������˫������λ��
        //      0   ����
        //      1   �ǡ�������˫����Ҳ�λ��
        public int IsBoundIndex(int index)
        {
            // �����������
            if (String.IsNullOrEmpty(this.PublishTime) == true)
                return 0;

            Debug.Assert(this.IssueLayoutState == IssueLayoutState.Binding, "ֻ����Binding����ʹ��IsBoundIndex()����");
            Debug.Assert(index >= 0, "");

            Cell cellCurrent = this.GetCell(index);

            // ����Ƿ�Null����, Ҫ����cell.IsMember�Ƿ�Ϊtrue
            if (cellCurrent != null)
            {
                if (cellCurrent.IsMember == true)
                    return 1;
            }

            Cell cellRight = null;
            bool bLeftBlank = IsBlankOrNullCell(cellCurrent);
            if (bLeftBlank == true)
            {
                // ��Ҫ�ж��Ҳ��Ƿ�Ϊ�϶���Ա����
                cellRight = this.GetCell(index + 1);
                if (cellRight != null && cellRight.IsMember == true)
                    return -1;
                return 0;
            }

            // ��ǰλ��Ϊ�ǿհ�
            Debug.Assert(cellCurrent != null && cellCurrent.item != null, "");

            /*
            if (cellCurrent.IsMember == true)
                return 1;
             * */

            // �������Ǻ϶���
            if (cellCurrent.item != null && cellCurrent.item.IsParent == true)
                return -1;


            // ��Ҫ�ж��Ҳ��Ƿ�Ϊ�϶���Ա����
            cellRight = this.GetCell(index + 1);
            if (cellRight != null && cellRight.IsMember == true)
                return -1;
            return 0;
        }

        // 2010/2/28
        // �ڳ�һ������Ŀ�λ������Ѿ��ǿհ׸��ӣ���ֱ��ʹ��
        // parameters:
        //      nNo ����index
        public void GetBlankSingleIndex(int nIndex)
        {
            if (this.IsBlankSingleIndex(nIndex) == true)
                return; // �Ѿ��ǿ�λ

            // �������ұ���չ����λ
            // Ҫ��Ϊ���������
            // 1) ��ǰλ����һ����ͨ�ĵ������ݡ�����Ǻ϶���
            // 2) ��ǰλ����һ���϶���Առ�ݡ�
            // �ֱ���Ҫ��չ��һ��������������

            // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
            // return:
            //      -1  �ǡ�������˫������λ��
            //      0   ����
            //      1   �ǡ�������˫����Ҳ�λ��
            int nIsBoundIndex = IsBoundIndex(nIndex);

            // ��˫����֮��
            if (nIsBoundIndex == -1)
            {
                // ��nIndex����������ƶ����ұߡ�
                // ��Ϊ�����Ѿ��Ǻ϶����ݣ�����Ҫͬʱ�ᶯ����һ���϶�������г�Ա����
                Cell cell_2 = this.GetCell(nIndex + 1);
                Debug.Assert(cell_2.ParentItem != null, "");

                // �ƶ�����һ��
                this.Container.MoveMemberCellsToRight(cell_2.ParentItem, 1);
            }
            else if (nIsBoundIndex == 1)
            {
                // ��˫��֮��

                // ��nIndex����������ƶ����ұߡ�
                // ��Ϊ�����Ѿ��Ǻ϶����ݣ�����Ҫͬʱ�ᶯ����һ���϶�������г�Ա����
                Cell cell_2 = this.GetCell(nIndex);
                Debug.Assert(cell_2.ParentItem != null, "");

                // �ƶ��������
                this.Container.MoveMemberCellsToRight(cell_2.ParentItem, 2);
            }
            else
            {
                // ����

                // ���ұ���չ��һ���ո���
                bool bRet = this.IsBlankSingleIndex(nIndex + 1);
                if (bRet == false)
                    this.GetBlankSingleIndex(nIndex + 1);

                Cell cell = this.GetCell(nIndex);
                this.SetCell(nIndex + 1, cell);
                this.SetCell(nIndex, null);
            }
        }

        // 2010/2/28
        // �ڳ�һ��˫���λ������Ѿ��ǿհ׸��ӣ���ֱ��ʹ��
        // parameters:
        //      nIndex ����index
        //      exclude_parent_item Ҫ�ų��ĺ϶�������
        //      exclude_member_item Ҫ�ų��ĳ�Ա����
        public void GetBlankDoubleIndex(int nIndex,
            ItemBindingItem exclude_parent_item,
            ItemBindingItem exclude_member_item)
        {
            if (this.IsBlankDoubleIndex(nIndex,
                exclude_parent_item,
                exclude_member_item) == true)
                return; // �Ѿ��ǿ�λ

            // �������ұ���չ����λ
            // Ҫ��Ϊ���������
            // 1) ��ǰλ����һ����ͨ�ĵ������ݡ�����Ǻ϶���
            // 2) ��ǰλ����һ���϶���Առ�ݡ�
            // �ֱ���Ҫ��չ��һ��������������

            // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
            // return:
            //      -1  �ǡ�������˫������λ��
            //      0   ����
            //      1   �ǡ�������˫����Ҳ�λ��
            int nIsBoundIndex = IsBoundIndex(nIndex);

            // ��˫����֮��
            if (nIsBoundIndex == -1)
            {
                // ��nIndex����������ƶ����ұߡ�
                // ��Ϊ�����Ѿ��Ǻ϶����ݣ�����Ҫͬʱ�ᶯ����һ���϶�������г�Ա����
                Cell cell_2 = this.GetCell(nIndex + 1);
                Debug.Assert(cell_2.ParentItem != null, "");

                // �ƶ��������
                this.Container.MoveMemberCellsToRight(cell_2.ParentItem, 2);
            }
            else if (nIsBoundIndex == 1)
            {
                // ��˫��֮��

                // ��nIndex����������ƶ����ұߡ�
                // ��Ϊ�����Ѿ��Ǻ϶����ݣ�����Ҫͬʱ�ᶯ����һ���϶�������г�Ա����
                Cell cell_2 = this.GetCell(nIndex);
                Debug.Assert(cell_2.ParentItem != null, "");

                // �ƶ���������
                this.Container.MoveMemberCellsToRight(cell_2.ParentItem, 3);
            }
            else
            {
                // ����
                Cell cell = this.GetCell(nIndex);

                /*
                // indexλ���Ƿ�ΪҪ�ų��ĳ�Ա����
                if (cell.item == exclude_member_item
                    && exclude_member_item != null)
                {
                    // ���ұ���չ��һ���ո��ӣ��͹���
                    bool bRet = this.IsBlankSingleIndex(nIndex + 1);
                    if (bRet == false)
                        this.GetBlankSingleIndex(nIndex + 1);

                    return;
                }*/

                {
                    // ���ұ���չ�������ո���
                    bool bRet = this.IsBlankDoubleIndex(nIndex + 1,
                        exclude_parent_item,
                        exclude_member_item);
                    if (bRet == false)
                        this.GetBlankDoubleIndex(nIndex + 1,
                            exclude_parent_item,
                            exclude_member_item);

                    // ���ڵ�ǰλ�õĸ���Ų����ȥ
                    this.SetCell(nIndex + 2, cell);
                    this.SetCell(nIndex, null);
                }
            }
        }

        // �°汾�����GetNewPosition()
        // �ڳ�һ��������λ��������ζ�Ҫ������λ
        // parameters:
        //      nNo ˫�����
        public void GetNewSingleIndex(int index)
        {
            {
                Cell cell_1 = this.GetCell(index);
                if (cell_1 == null)
                    return;
            }

            // �������ұ���չ����λ
            // Ҫ��Ϊ���������
            // 1) ��ǰλ����һ����ͨ�ĵ������ݡ�����Ǻ϶���
            // 2) ��ǰλ����һ���϶���Առ�ݡ�
            // �ֱ���Ҫ��չ��һ��������������

            // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
            // return:
            //      -1  �ǡ�������˫������λ��
            //      0   ����
            //      1   �ǡ�������˫����Ҳ�λ��
            int nIsBoundIndex = IsBoundIndex(index);

            // ��˫����֮��
            if (nIsBoundIndex == -1)
            {
                // ��nIndex����������ƶ����ұߡ�
                // ��Ϊ�����Ѿ��Ǻ϶����ݣ�����Ҫͬʱ�ᶯ����һ���϶�������г�Ա����
                Cell cell_2 = this.GetCell(index + 1);
                Debug.Assert(cell_2.ParentItem != null, "");

                // �ƶ�����1��
                this.Container.MoveMemberCellsToRight(cell_2.ParentItem, 1);
            }
            else if (nIsBoundIndex == 1)
            {
                // ��˫��֮��

                // ��nIndex����������ƶ����ұߡ�
                // ��Ϊ�����Ѿ��Ǻ϶����ݣ�����Ҫͬʱ�ᶯ����һ���϶�������г�Ա����
                Cell cell_2 = this.GetCell(index);
                Debug.Assert(cell_2.ParentItem != null, "");

                // �ƶ�����2��
                this.Container.MoveMemberCellsToRight(cell_2.ParentItem, 2);
            }
            else
            {
                // ����

                /*
                Cell cell = this.GetCell(index);
                IssueBindingItem issue = cell.Container;
                 * */

                // �ڵ�ǰλ����չ��һ����˫��
                bool bRet = this.IsBlankSingleIndex(index + 1);
                if (bRet == false)
                    this.GetBlankSingleIndex(index + 1);

                Cell cell = this.GetCell(index);
                this.SetCell(index + 1, cell);
                this.SetCell(index, null);
            }

            // �����ǰλ��
            this.SetCell(index, null);
        }

        // �°汾�����GetNewPosition()
        // �ڳ�һ��˫����λ��������ζ�Ҫ������λ
        // parameters:
        //      nNo ˫�����
        public void GetNewDoubleIndex(int index)
        {
            {
                Cell cell_1 = this.GetCell(index);
                Cell cell_2 = this.GetCell(index + 1);

                if (cell_1 == null && cell_2 == null)
                    return;
            }

            // �������ұ���չ����λ
            // Ҫ��Ϊ���������
            // 1) ��ǰλ����һ����ͨ�ĵ������ݡ�����Ǻ϶���
            // 2) ��ǰλ����һ���϶���Առ�ݡ�
            // �ֱ���Ҫ��չ��һ��������������

            // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
            // return:
            //      -1  �ǡ�������˫������λ��
            //      0   ����
            //      1   �ǡ�������˫����Ҳ�λ��
            int nIsBoundIndex = IsBoundIndex(index);

            // ��˫����֮��
            if (nIsBoundIndex == -1)
            {
                // ��nIndex����������ƶ����ұߡ�
                // ��Ϊ�����Ѿ��Ǻ϶����ݣ�����Ҫͬʱ�ᶯ����һ���϶�������г�Ա����
                Cell cell_2 = this.GetCell(index + 1);
                Debug.Assert(cell_2.ParentItem != null, "");

                // �ƶ��������
                this.Container.MoveMemberCellsToRight(cell_2.ParentItem, 2);
            }
            else if (nIsBoundIndex == 1)
            {
                // ��˫��֮��

                // ��nIndex����������ƶ����ұߡ�
                // ��Ϊ�����Ѿ��Ǻ϶����ݣ�����Ҫͬʱ�ᶯ����һ���϶�������г�Ա����
                Cell cell_2 = this.GetCell(index);
                Debug.Assert(cell_2.ParentItem != null, "");

                // �ƶ���������
                this.Container.MoveMemberCellsToRight(cell_2.ParentItem, 3);
            }
            else
            {
                // ����

                /*
                Cell cell = this.GetCell(index);
                IssueBindingItem issue = cell.Container;
                 * */

                // �ڵ�ǰλ���ұ���չ��һ����˫��
                bool bRet = this.IsBlankDoubleIndex(index + 1, null, null);
                if (bRet == false)
                    this.GetBlankDoubleIndex(index + 1, null, null);

                // ���ڵ�ǰλ�õĸ���Ų����ȥ
                Cell cell = this.GetCell(index);
                this.SetCell(index + 2, cell);
                this.SetCell(index, null);
            }

            // �����ǰλ��
            this.SetCell(index, null);
            this.SetCell(index + 1, null);
        }

#if OLD_VERSION
        // �ڳ�һ����λ������Ѿ��ǿհ׸��ӣ���ֱ��ʹ��
        // parameters:
        //      nNo ˫�����
        public void GetBlankPosition(int nNo,
            ItemBindingItem exclude_parent_item)
        {
            if (this.IsBlankPosition(nNo, exclude_parent_item) == true)
                return; // �Ѿ��ǿ�λ

            // �����ұ��Ƿ��п�λ
            bool bRet = this.IsBlankPosition(nNo + 1, exclude_parent_item);
            if (bRet == false)
                this.GetBlankPosition(nNo + 1, exclude_parent_item);

            bRet = IsBindedPosition(nNo);
            if (bRet == false)
            {
                Cell cell_1 = this.GetCell(nNo * 2);
                Cell cell_2 = this.GetCell(nNo * 2 + 1);
                if (cell_1 != null)
                {
                    this.SetCell(nNo * 2 + 2, cell_1);
                    this.SetCell(nNo * 2,  null);

                }
                if (cell_2 != null)
                {
                    this.SetCell(nNo * 2 + 2 + 1, cell_2);
                    this.SetCell(nNo * 2 + 1, null);
                }
            }
            else
            {
                Cell cell_2 = this.GetCell(nNo * 2 + 1);
                /*
                Debug.Assert(cell_2.item != null, "");
                Debug.Assert(cell_2.item.ParentItem != null, "");
                // Ϊ�϶�����
                this.Container.MoveMemberCellsToRight(cell_2.item.ParentItem);
                 * */
                Debug.Assert(cell_2.ParentItem != null, "");

                if (exclude_parent_item != null
                    && cell_2.ParentItem == exclude_parent_item)
                    return; //

                // Ϊ�϶�����
                this.Container.MoveMemberCellsToRight(cell_2.ParentItem);
            }
        }

        // �ڳ�һ����λ��������ζ�Ҫ������λ
        // parameters:
        //      nNo ˫�����
        public void GetNewPosition(int nNo)
        {
            {
                Cell cell_1 = this.GetCell(nNo * 2);
                Cell cell_2 = this.GetCell(nNo * 2 + 1);

                if (cell_1 == null && cell_2 == null)
                    return;
            }

            // �����ұ��Ƿ��п�λ
            bool bRet = this.IsBlankPosition(nNo + 1, null);
            if (bRet == false)
                this.GetBlankPosition(nNo + 1, null);

            bRet = IsBindedPosition(nNo);
            if (bRet == false)
            {
                Cell cell_1 = this.GetCell(nNo * 2);
                Cell cell_2 = this.GetCell(nNo * 2 + 1);
                if (cell_1 != null)
                {
                    this.SetCell(nNo * 2 + 2, cell_1);
                    this.SetCell(nNo * 2, null);

                }
                if (cell_2 != null)
                {
                    this.SetCell(nNo * 2 + 2 + 1, cell_2);
                    this.SetCell(nNo * 2 + 1, null);
                }
            }
            else
            {
                Cell cell_2 = this.GetCell(nNo * 2 + 1);
                Debug.Assert(cell_2.ParentItem != null, "");
                // Ϊ�϶�����
                this.Container.MoveMemberCellsToRight(cell_2.ParentItem);
            }
        }
#endif

        // ɾ��һ������λ�á�����ұ��ǵ��ᣬ�������ƶ������λ������ұ��Ǻ϶���Աλ�ã�����������������ƶ�����Ҳ�ƶ�
        // parameters:
        //      nNo ˫�����
        public void RemoveSingleIndex(int index)
        {
            Debug.Assert(this.IssueLayoutState == IssueLayoutState.Binding, "");

            if (this.Cells.Count <= index)
                return;

            // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
            // return:
            //      -1  �ǡ�������˫������λ��
            //      0   ����
            //      1   �ǡ�������˫����Ҳ�λ��
            int nRet = IsBoundIndex(index);
            if (nRet == -1 || nRet == 1)
            {
                Debug.Assert(false, "�����ñ�����RemoveSingleIndex()ɾ���϶�λ��");
            }

            this.SetCell(index, null);

            // �����ƶ�ͬ�����е���ֱ����һ���϶���Ա˫��
            for (int i = index + 1; ; i++)
            {
                if (i >= this.Cells.Count)
                    break;

                // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
                // return:
                //      -1  �ǡ�������˫������λ��
                //      0   ����
                //      1   �ǡ�������˫����Ҳ�λ��
                nRet = IsBoundIndex(i);
                if (nRet == -1)
                {
                    //      -1  �ǡ�������˫������λ��
                    Cell member = this.GetCell(i + 1);
                    Debug.Assert(member != null, "");
                    Debug.Assert(member.ParentItem != null, "");

                    Cell parent_cell = member.ParentItem.ContainerCell;
                    if (this.Container.CanMoveToLeft(parent_cell) == true)
                    {
                        this.Container.MoveCellsToLeft(parent_cell);
                    }
                    break;
                }
                else if (nRet == 1)
                {
                    //      1   �ǡ�������˫����Ҳ�λ��
                    Debug.Assert(false, "");
                    break;
                }


                Cell cell_1 = this.GetCell(i);
                this.SetCell(i - 1, cell_1);
                this.SetCell(i, null);
            }
        }

#if OLD_VERSION

        // ɾ��һ��˫��λ�á�����ұ��ǵ��ᣬ�������ƶ������λ������ұ��Ǻ϶���Աλ�ã�����������������ƶ�����Ҳ�ƶ�
        // parameters:
        //      nNo ˫�����
        public void RemovePosition(int nNo)
        {
            if (this.Cells.Count <= nNo * 2)
                return;

            this.SetCell(nNo * 2, null);
            this.SetCell((nNo * 2)+1, null);

            // �����ƶ����е���˫��ֱ����һ���϶���Ա˫��
            for (int i = nNo + 1; ; i++)
            {
                if (i * 2 >= this.Cells.Count)
                    break;

                if (IsBindedPosition(i) == true)
                {
                    Cell member = this.GetCell((i * 2) + 1);
                    Debug.Assert(member != null, "");
                    Debug.Assert(member.ParentItem != null, "");

                    Cell parent_cell = member.ParentItem.ContainerCell;
                    if (this.Container.CanMoveToLeft(parent_cell) == true)
                    {
                        this.Container.MoveCellsToLeft(parent_cell);
                    }
                    break;
                }

                Cell cell_1 = this.GetCell(i * 2);
                Cell cell_2 = this.GetCell((i * 2)+ 1);

                this.SetCell((i * 2) - 2, cell_1);
                this.SetCell((i * 2)+1 - 2, cell_2);

                this.SetCell((i * 2), null);
                this.SetCell((i * 2) + 1, null);
            }
        }

#endif

        // ����һ�����񵽱�
        // �����ܱȽ�ԭʼ��������ѹ��λ
        // parameters:
        //      nSourceIndex    Դindexλ�á�ע�⣬������˫������
        //      nTargetIndex    Ŀ��indexλ�á�ע�⣬������˫������
        public void CopySingleIndexTo(
            int nSourceIndex,
            int nTargetIndex,
            bool bClearSource)
        {
            string strError = "";
            if (nSourceIndex == nTargetIndex)
            {
                strError = "Դ��Ŀ�겻����ͬһ��";
                throw new Exception(strError);
            }

            Cell source_cell_1 = this.GetCell(nSourceIndex);

            this.SetCell(nTargetIndex,
                source_cell_1);

            if (bClearSource == true)
            {
                if (nSourceIndex != nTargetIndex)
                {
                    this.SetCell(nSourceIndex,
                        null);
                }
            }
        }

        // ����һ��˫�񵽱�
        // �����ܱȽ�ԭʼ��������ѹ��λ
        // parameters:
        //      nSourceIndex    Դindexλ�á�ע�⣬������˫������
        //      nTargetIndex    Ŀ��indexλ�á�ע�⣬������˫������
        public void CopyDoubleIndexTo(
            int nSourceIndex,
            int nTargetIndex,
            bool bClearSource)
        {
            string strError = "";

            Debug.Assert(this.IssueLayoutState == IssueLayoutState.Binding, "");

            if (nSourceIndex == nTargetIndex)
            {
                strError = "Դ��Ŀ�겻����ͬһ��";
                throw new Exception(strError);
            }

            Cell source_cell_1 = this.GetCell(nSourceIndex);
            Cell source_cell_2 = this.GetCell(nSourceIndex + 1);

            this.SetCell(nTargetIndex,
                source_cell_1);
            this.SetCell(nTargetIndex + 1,
                source_cell_2);

            if (bClearSource == true)
            {
                if (nSourceIndex != nTargetIndex
                    && nSourceIndex != nTargetIndex + 1)
                {
                    this.SetCell(nSourceIndex,
                        null);
                }
                if (nSourceIndex + 1 != nTargetIndex
                    && nSourceIndex + 1 != nTargetIndex + 1)
                {
                    this.SetCell(nSourceIndex + 1,
                        null);
                }
            }
        }

#if OLD_VERSION
        // ����һ��˫�񵽱�
        // �����ܱȽ�ԭʼ��������ѹ��λ
        public void CopyPositionTo(
            int nSourceNo,
            int nTargetNo,
            bool bClearSource)
        {
            string strError = "";
            if (nSourceNo == nTargetNo)
            {
                strError = "Դ��Ŀ�겻����ͬһ��";
                throw new Exception(strError);
            }

            Cell source_cell_1 = this.GetCell(nSourceNo * 2);
            Cell source_cell_2 = this.GetCell((nSourceNo * 2) + 1);

            this.SetCell(nTargetNo * 2,
                source_cell_1);
            this.SetCell((nTargetNo * 2) + 1,
                source_cell_2);

            if (bClearSource == true)
            {
                this.SetCell(nSourceNo * 2,
                    null);
                this.SetCell((nSourceNo * 2) + 1,
                    null);
            }
        }
#endif

        // ��һ�������ƶ�(����)����һ��λ�á�Ȼ�������Ҫ���ڳ�����Դλ�ñ���ѹ
        // �������̲�ɾ���κ������ݵĸ���
        // return:
        //      -1  ����
        //      0   ���Ԫ��û�иı�
        //      1   ���Ԫ�������ı�
        public int MoveSingleIndexTo(
    int nSourceIndex,
    int nTargetIndex,
    out string strError)
        {
            strError = "";

            Debug.Assert(this.IssueLayoutState == IssueLayoutState.Binding, "ֻ����Binding������ʹ��MoveSingleIndexTo()����");

            if (nSourceIndex == nTargetIndex)
            {
                strError = "Դ��Ŀ�겻����ͬһ��";
                return -1;
            }

#if DEBUG
            // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
            // return:
            //      -1  �ǡ�������˫������λ��
            //      0   ����
            //      1   �ǡ�������˫����Ҳ�λ��
            int nRet = IsBoundIndex(nSourceIndex);
            if (nRet == -1 || nRet == 1)
            {
                Debug.Assert(false, "���������ƶ����Ժ϶���Χ�ĸ���");
            }
#endif



            // sourceС��target����������ɾ��Դ����ΪԴ��λ�ò���仯
            // source����target��ɾ����Դ�ٲ��븴�Ƶ�targetλ�á���Ϊ����ȥɾ��Դ��Դ��λ���Ѿ������˱仯

            int nOldMaxCells = this.Cells.Count;

            Cell source_cell = this.GetCell(nSourceIndex);

            // �Ż�
            if (nSourceIndex + 1 == nTargetIndex
                || nSourceIndex == nTargetIndex + 1)
            {
                // ���������������ͼҪ�����߽���

                // source already in temp

                // target --> source
                this.SetCell(nSourceIndex,
                    this.GetCell(nTargetIndex));

                // temp --> target
                this.SetCell(nTargetIndex,
                    source_cell);

                // this.Container.Invalidate();
                if (nOldMaxCells != this.Cells.Count)
                    return 1;
                return 0;
            }

            // source����target��ɾ����Դ�ٲ��븴�Ƶ�targetλ�á���Ϊ����ȥɾ��Դ��Դ��λ���Ѿ������˱仯
            if (nSourceIndex > nTargetIndex)
            {
                // �����λ��
                this.RemoveSingleIndex(nSourceIndex);
            }


            // ���ܻ�ı��֣�nSourceNo������Ч
            // this.GetNewDoubleIndex(nTargetIndex);
            this.GetNewSingleIndex(nTargetIndex);   // 2010/3/12

            /*
            {
                // ����ȷ��nSourceNo
                int nTemp = -1;

                Debug.Assert(source_cell_1 != null || source_cell_2 != null, "Դ���������ӣ������ܶ�Ϊnull");

                if (source_cell_1 != null)
                {
                    nTemp = this.IndexOfCell(source_cell_1);
                }
                if (nTemp == -1 && source_cell_2 != null)
                {
                    nTemp = this.IndexOfCell(source_cell_2);
                    if (nTemp != -1)
                        nTemp--;
                }

                if (nTemp != -1)
                    nSourceIndex = nTemp;
                else
                    nSourceIndex = -1; // �Ҳ�����?
            }
             * */

            // ���õ���λ��
            this.SetCell(nTargetIndex,
                source_cell);

            // sourceС��target����������ɾ��Դ����ΪԴ��λ�ò���仯
            if (nSourceIndex < nTargetIndex)
            {
                if (nSourceIndex != -1)
                {
                    // �����λ��
                    this.RemoveSingleIndex(nSourceIndex);
                }
            }

            // TODO: ����������������б仯ʱ�ŵ��á�һ���������ˢ�µ�ǰissue��Χ
            // this.Container.AfterWidthChanged(true);
            if (nOldMaxCells != this.Cells.Count)
                return 1;
            return 0;
        }

        // ��һ�������ƶ�(����)����һ��λ�á�
        // ���Ŀ��λ���Ƕ������У�������һ�����ӣ�����ڼƻ��������򾡿���ռ�ÿհ׸���
        // �ڳ�����Դλ�ã�����ڶ������У���ΪԤ��״̬������ڼƻ������䣬�򱻼�ѹ
        // �������̲�ɾ���κ������ݵĸ���
        // return:
        //      -1  ����
        //      0   ���Ԫ��û�иı�
        //      1   ���Ԫ�������ı�
        public int MoveCellTo(
    int nSourceIndex,
    int nTargetIndex,
    out string strError)
        {
            strError = "";

            Debug.Assert(this.IssueLayoutState == IssueLayoutState.Accepting, "ֻ����Accepting������ʹ��MoveCellTo()����");

            if (nSourceIndex == nTargetIndex)
            {
                strError = "Դ��Ŀ�겻����ͬһ��";
                return -1;
            }
            int nOldMaxCells = this.Cells.Count;

            // ����Դλ���Ƿ��ڶ�������
            Cell source_cell = this.GetCell(nSourceIndex);
            if (source_cell is GroupCell)
            {
                strError = "��������Ӳ��ܱ��ƶ�";
                return -1;
            }

            GroupCell source_group = null;
            if (source_cell.item != null)
            {
                if (source_cell.item.Calculated == true
                    || source_cell.item.OrderInfoPosition.X != -1)
                {
                    source_group = source_cell.item.GroupCell;
                }
            }

            // sourceС��target����������ɾ��Դ����ΪԴ��λ�ò���仯
            // source����target��ɾ����Դ�ٲ��븴�Ƶ�targetλ�á���Ϊ����ȥɾ��Դ��Դ��λ���Ѿ������˱仯

            // �Ż�
            if (nSourceIndex + 1 == nTargetIndex
                || nSourceIndex == nTargetIndex + 1)
            {
                // ���������������ͼҪ�����߽���

                // source already in temp

                // target --> source
                this.SetCell(nSourceIndex,
                    this.GetCell(nTargetIndex));

                // temp --> target
                this.SetCell(nTargetIndex,
                    source_cell);

                goto END1;
            }

            // source����target��ɾ����Դ�ٲ��븴�Ƶ�targetλ�á���Ϊ����ȥɾ��Դ��Դ��λ���Ѿ������˱仯
            if (nSourceIndex > nTargetIndex)
            {
                // �����λ��
                if (this.Cells.Count > nSourceIndex)
                    this.Cells.RemoveAt(nSourceIndex);
            }


            // ���ܻ�ı��֣�nSourceNo������Ч
            // ��targetλ������һ������
            if (this.Cells.Count > nTargetIndex)
                this.Cells.Insert(nTargetIndex, null);


            // ���õ���λ��
            this.SetCell(nTargetIndex,
                source_cell);

            // sourceС��target����������ɾ��Դ����ΪԴ��λ�ò���仯
            if (nSourceIndex < nTargetIndex)
            {
                if (nSourceIndex != -1)
                {
                    // �����λ��
                    if (this.Cells.Count > nSourceIndex)
                        this.Cells.RemoveAt(nSourceIndex);
                }
            }

        END1:
            // ˢ�¶�����Ϣ
            /*
            if (source_group != null)
            {
                // source group������ˢ��һ�Σ������޷�ͨ���ƶ�λ���Ժ��source_cell�õ�target_group
                source_group.RefreshGroupMembersOrderInfo(0, 0);
            }
             * */

            int nNewTargetIndex = this.IndexOfCell(source_cell);
            Debug.Assert(nNewTargetIndex != -1, "");
            GroupCell target_group = this.BelongToGroup(nNewTargetIndex);


            // ���Դ�����������϶����ƻ�������
            if (source_group != null && target_group == null)
            {
                if (source_cell.item != null)
                {
                    source_cell.item.OrderInfoPosition.X = -1;
                    source_cell.item.OrderInfoPosition.Y = -1;
                    if (source_cell.item.Calculated == true)
                    {
                        // Ԥ����ӱ�Ϊ��ͨ�հ׸���
                        source_cell.item = null;
                    }
                }
            }

            // ���Դ���Ӽƻ��������϶���������
            if (source_group == null && target_group != null)
            {
                if (source_cell.item == null)
                {
                    // �հ׸���Ҫ��ΪԤ���ʽ
                    source_cell.item = new ItemBindingItem();
                    source_cell.item.Container = this;
                    source_cell.item.Initial("<root />", out strError);
                    source_cell.item.RefID = "";
                    source_cell.item.LocationString = "";
                    source_cell.item.Calculated = true;
                    SetFieldValueFromOrderInfo(
                        false,
                        source_cell.item,
                        target_group.order);
                    // �����Ҫ��������ʱ��
                    if (String.IsNullOrEmpty(source_cell.item.PublishTime) == true)
                        source_cell.item.PublishTime = this.PublishTime;
                    if (String.IsNullOrEmpty(source_cell.item.Volume) == true)
                    {
                        string strVolumeString =
VolumeInfo.BuildItemVolumeString(
IssueUtil.GetYearPart(this.PublishTime),
this.Issue,
this.Zong,
this.Volume);
                        source_cell.item.Volume = strVolumeString;
                    }
                }

                // order xy ������Ȼ������
            }

            int nSourceOrderCountDelta = 0;
            int nSourceArrivedCountDelta = 0;
            if (source_group != null && source_group != target_group)
            {
                nSourceOrderCountDelta--;
                if (source_cell.item != null
                    && source_cell.item.Calculated == false)
                    nSourceArrivedCountDelta--;

                source_group.RefreshGroupMembersOrderInfo(nSourceOrderCountDelta,
    nSourceArrivedCountDelta);
            }
            int nTargetOrderCountDelta = 0;
            int nTargetArrivedCountDelta = 0;
            if (target_group != null && source_group != target_group)
            {
                nTargetOrderCountDelta++;
                if (source_cell.item != null
                    && source_cell.item.Calculated == false)
                    nTargetArrivedCountDelta++;
                target_group.RefreshGroupMembersOrderInfo(nTargetOrderCountDelta,
    nTargetArrivedCountDelta);

            }

            if (source_group == target_group
                && source_group != null)
            {
                // ��һ�������϶�
                source_group.RefreshGroupMembersOrderInfo(0,
                    0);
            }


            // TODO: ����������������б仯ʱ�ŵ��á�һ���������ˢ�µ�ǰissue��Χ
            // this.Container.AfterWidthChanged(true);
            if (nOldMaxCells != this.Cells.Count)
                return 1;
            return 0;
        }

#if OLD_VERSION
        // ��һ��˫���ƶ�(����)����һ��˫��λ�á�Ȼ�������Ҫ���ڳ�����Դλ�ñ���ѹ
        // �������̲�ɾ���κ������ݵ�˫��
        // return:
        //      -1  ����
        //      0   ���Ԫ��û�иı�
        //      1   ���Ԫ�������ı�
        public int MovePositionTo(
    int nSourceNo,
    int nTargetNo,
    out string strError)
        {
            strError = "";

            if (nSourceNo == nTargetNo)
            {
                strError = "Դ��Ŀ�겻����ͬһ��";
                return -1;
            }
            /*
            if (this.IsBindedPosition(nSourceNo) == true)
            {
                strError = "Դ�����ǳ�Ա��";
                return -1;
            }
            if (this.IsBindedPosition(nTargetNo) == true)
            {
                strError = "Ŀ�겻���ǳ�Ա��";
                return -1;
            }
             * */
            int nOldMaxCells = this.Cells.Count;

            Cell source_cell_1 = this.GetCell(nSourceNo * 2);
            Cell source_cell_2 = this.GetCell((nSourceNo * 2) + 1);

            // �Ż�
            if (nSourceNo + 1 == nTargetNo
                || nSourceNo == nTargetNo + 1)
            {
                // ���������������ͼҪ�����߽���

                // source already in temp

                // target --> source
                this.SetCell(nSourceNo * 2,
                    this.GetCell(nTargetNo * 2));
                this.SetCell((nSourceNo * 2) + 1,
                    this.GetCell((nTargetNo * 2) + 1));

                // temp --> target
                this.SetCell(nTargetNo * 2,
                    source_cell_1);
                this.SetCell((nTargetNo * 2) + 1,
                    source_cell_2);

                // this.Container.Invalidate();
                if (nOldMaxCells != this.Cells.Count)
                    return 1;
            }


            // ���ܻ�ı��֣�nSourceNo������Ч
            this.GetNewPosition(nTargetNo);

            {
                // ����ȷ��nSourceNo
                int nTemp = -1;

                Debug.Assert(source_cell_1 != null || source_cell_2 != null, "Դ���������ӣ������ܶ�Ϊnull");

                if (source_cell_1 != null)
                    nTemp = this.IndexOfCell(source_cell_1);
                if (nTemp == -1 && source_cell_2 != null)
                    nTemp = this.IndexOfCell(source_cell_2);

                if (nTemp != -1)
                    nSourceNo = nTemp / 2;
                else
                    nSourceNo = -1; // �Ҳ�����?
            }


            // ���õ���λ��
            this.SetCell(nTargetNo * 2,
                source_cell_1);
            this.SetCell((nTargetNo * 2) + 1,
                source_cell_2);


            if (nSourceNo != -1)
            {
                // �����λ��
                this.RemovePosition(nSourceNo);
            }

            // TODO: ����������������б仯ʱ�ŵ��á�һ���������ˢ�µ�ǰissue��Χ
            // this.Container.AfterWidthChanged(true);
            if (nOldMaxCells != this.Cells.Count)
                return 1;
            return 0;
        }
#endif

        public CellBase GetFirstCell()
        {
            if (this.Cells.Count == 0
                || this.Cells[0] == null)
                return new NullCell(0, this.Container.Issues.IndexOf(this));

            return this.Cells[0];
        }

        // ˢ��<copy>Ԫ������Ķ���/�ѵ�ֵ
        public int RefreshOrderCopy(int x,
            out string strError)
        {
            strError = "";

            if (x >= this.OrderItems.Count)
            {
                Debug.Assert(false, "x >= this.OrderItems.Count["+this.OrderItems.Count.ToString()+"]");
                return -1;
            }
            OrderBindingItem order = this.OrderItems[x];
            // return:
            //      -1  ����
            //      0   û�з����޸�
            //      1   �������޸�
            int nRet = order.RefreshOrderCopy(
                    false,
                    out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        // Binding����?��ˢ��OrderInfoPosition
        // ������x����Щ�����У�yΪstart_Y���ϵ�����nDelta
        public void RefreshOrderInfoPositionXY(int x,
            int start_y,
            int nDeltaY)
        {
            // Debug.Assert(this.IssueLayoutState == IssueLayoutState.Binding, "");

            for (int i = 0; i < this.Cells.Count; i++)
            {
                Cell cell = this.Cells[i];
                if (cell == null || cell.item == null)
                    continue;
                if (cell.item.OrderInfoPosition.X == -1)
                    continue;
                if (cell.item.OrderInfoPosition.X == x)
                {
                    if (cell.item.OrderInfoPosition.Y >= start_y)
                        cell.item.OrderInfoPosition.Y += nDeltaY;
                }
            }
        }

        // �����ǰ�������Լ�ȫ���¼���ѡ���־, ��������Ҫˢ�µĶ���
        public void ClearAllSubSelected(ref List<CellBase> objects,
            int nMaxCount)
        {
            if (this.Selected == true)
            {
                this.Selected = false;
                // ״̬�޸Ĺ��Ĳż�������
                if (objects.Count < nMaxCount)
                    objects.Add(this);
            }

            for (int i = 0; i < this.Cells.Count; i++)
            {
                CellBase cell = this.Cells[i];
                if (cell != null)
                {
                    if (cell.Selected == true)
                    {
                        cell.Selected = false;
                        // ״̬�޸Ĺ��Ĳż�������
                        if (objects.Count < nMaxCount)
                            objects.Add(cell);
                    }
                }
            }
        }

        // ȷ����ǰ��װ����ռ�ݵ���(����)֮�ң���һ�������ڲ��뵥���ӵ��С����ҿ���
        // ע�ⷵ�ص��ǵ���index��
        internal int GetFirstAvailableSingleInsertIndex()
        {
            Debug.Assert(this.IssueLayoutState == IssueLayoutState.Binding, "");
            int nMax = this.Cells.Count - 1;

            int index = -1;
            for (int i = nMax; i >= 0; i--)
            {
                // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
                // return:
                //      -1  �ǡ�������˫������λ��
                //      0   ����
                //      1   �ǡ�������˫����Ҳ�λ��
                int nRet = IsBoundIndex(i);
                if (nRet != 0)
                    break;

                index = i;
            }

            if (index == -1)
            {
                Debug.Assert(nMax + 1 >= 0, "");
                return (nMax + 1);
            }

            return index;
        }

        // �õ����вɹ�������(��)�ĵ�һ�����õ�index
        internal int GetFirstFreeIndex()
        {
            Debug.Assert(this.IssueLayoutState == IssueLayoutState.Accepting, "");
            int nMax = this.Cells.Count - 1;

            int index = -1;
            for (int i = nMax; i >= 0; i--)
            {
                Cell cell = this.Cells[i];
                if (cell == null)
                {
                    index = i;
                    continue;
                }

                if (cell is GroupCell)
                    break;

                index = i;
            }

            if (index == -1)
            {
                Debug.Assert(nMax + 1 >= 0, "");
                return (nMax + 1);
            }

            return index;
        }

        // �õ����вɹ�������(��)�ĵ�һ���հ׻���null��index
        internal int GetFirstFreeBlankIndex()
        {
            Debug.Assert(this.IssueLayoutState == IssueLayoutState.Accepting, "");

            int nMax = this.Cells.Count - 1;
            int nFreeIndex = GetFirstFreeIndex();

            for (int i = nFreeIndex; i <=nMax; i++)
            {
                Cell cell = this.Cells[i];
                if (IsBlankOrNullCell(cell) == true)
                {
                    // 2010/3/29
                    if (cell != null && cell.IsMember == true)
                        continue;   // ����ռ��ȱ�ڳ�Ա��
                    return i;
                }
            }

            return this.Cells.Count;
        }

        // �Ƿ�Ϊ�հ׻���(��)����
        internal static bool IsBlankOrNullCell(Cell cell)
        {
            if (cell == null)
                return true;
            if (cell.item == null)
                return true;
            return false;
        }

        // 2010/2/28
        // ????? ������
        // ̽�⵱ǰ��װ����ռ�ݵ���֮�⣬��һ�����õ���(������)������
        // ע�⣬�����ص�index���ܳ�������Cells����Ĺ��
        internal int GetFirstAvailableBoundColumn()
        {
            for (int i = 0; i < this.Cells.Count; i++)
            {
                if (IsBlankDoubleIndex(i) == true)
                    return i;
                /*
                Cell cell = this.Cells[i];
                bool bBlank = IsBlankOrNullCell(cell);

                if (bBlank == true)
                {
                    // ��Ҫ�ж��Ҳ��Ƿ�Ϊ�϶���Ա����
                    Cell cellRight = this.GetCell(i + 1);
                    if (cell == null || cell.IsMember == false)
                        return i;
                }
                 * */
            }

            return this.Cells.Count;
        }

#if OLD_VERSION

        // ȷ����ǰ��װ����ռ�ݵ���֮�⣬��һ�����õ��С�����
        // ע�⣬�����ص�index���ܳ�������Cells����Ĺ��
        // ע��Ҫ�����򼴱��ǿհ׵�λ�ã�ҲӦ����Cell���������϶���Χռ��? �����ж��������鷳��
        internal int GetFirstAvailableBindingColumn()
        {
            // ����λ��������ˣ��ͱ����Ǻ϶���
            for (int i = 0; i < this.Cells.Count; i++)
            {
                Cell cell = this.Cells[i];
                if ((i % 2) == 0)
                {
                    // ����λ��
                    bool bBlank = false;
                    if (cell == null)
                        bBlank = true;

                    // 2010/2/16 changed
                    if (cell != null && cell.item == null)
                    {
                        if (cell.ParentItem == null)
                            bBlank = true;
                    }


                    if (bBlank == false)
                        continue;

                    // ׷���ж�ż��λ��
                    Cell right_cell = null;
                    if (i+1<this.Cells.Count)
                        right_cell = this.Cells[i+1];

                    if (right_cell != null)
                    {
                        if (right_cell.Binded == false)
                            return i;
                    }
                }
                else
                {
                    // ż��λ��
                }
            }
            if ((this.Cells.Count % 2) == 0)
                return this.Cells.Count;
            return this.Cells.Count + 1;
        }
#endif

        // ѡ��λ�ھ����ڵĶ���
        public void Select(RectangleF rect,
            SelectAction action,
            List<Type> types,
            ref List<CellBase> update_objects,
            int nMaxCount)
        {
            float x0 = this.Container.m_nLeftTextWidth;

            // �ȿ�����������û�н���
            RectangleF rectAll = new RectangleF(0,
                0,
                x0 + (this.Container.m_nCellWidth * this.Cells.Count),
                this.Container.m_nCellHeight);
            if (rectAll.IntersectsWith(rect) == false)
                return;

            // ��߱��ⲿ�֡�����Issue����
            RectangleF rectLeftText = new RectangleF(0,
                0,
                x0,
                this.Container.m_nCellHeight);
            if (rectLeftText.IntersectsWith(rect) == true)
            {
                bool bRet = this.Select(action);
                if (bRet == true && update_objects.Count < nMaxCount)
                {
                    update_objects.Add(this);
                }
                return;
            }

            // �ȿ�����������û�н���
            rectAll = new RectangleF(x0,
                0,
                this.Container.m_nCellWidth * this.Cells.Count,
                this.Container.m_nCellHeight);
            if (rectAll.IntersectsWith(rect) == false)
                return;


            for (int i = 0; i < this.Cells.Count; i++)
            {
                Cell cell = this.Cells[i];
                if (cell == null)
                    goto CONTINUE;

                RectangleF rectCell = new RectangleF(x0,
                    0,
                    this.Container.m_nCellWidth,
                    this.Container.m_nCellHeight);


                if (rectCell.IntersectsWith(rect) == true)
                {
                    bool bRet = cell.Select(action);
                    if (bRet == true && update_objects.Count < nMaxCount)
                    {
                        update_objects.Add(cell);
                    }
                }
            CONTINUE:
                x0 += this.Container.m_nCellWidth;
            }
        }

        // �Ƿ��е�Ԫ��ѡ��?
        public bool HasCellSelected()
        {
            for (int i = 0; i < this.Cells.Count; i++)
            {
                Cell cell = this.Cells[i];
                if (cell != null)
                {
                    if (cell.Selected == true)
                        return true;
                }
            }

            return false;
        }

        public List<Cell> SelectedCells
        {
            get
            {
                List<Cell> results = new List<Cell>();
                for (int i = 0; i < this.Cells.Count; i++)
                {
                    Cell cell = this.Cells[i];
                    if (cell != null)
                    {
                        if (cell.Selected == true)
                            results.Add(cell);
                    }
                }

                return results;
            }
        }

        // ��ʼ������Ϣ��Ȼ���ʼ���·��Ĳ���Ϣ
        // parameters:
        //      strXml  �ڼ�¼XML
        //      bLoadItems  �Ƿ������ⲿ�ӿ�װ�������Ĳ����Items������
        public int Initial(string strXml,
            bool bLoadItems,
            out string strError)
        {
            strError = "";

            // this.Xml = strXml;
            this.dom = new XmlDocument();
            try
            {
                this.dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "�ڼ�¼ XML װ�� DOM ʱ����: " + ex.Message;
                return -1;
            }

            Debug.Assert(this.Container != null, "");

            if (bLoadItems == true)
            {
                // TODO: this.PublishTimeΪ����ô�죿
                // TODO: ��ͬ�����н���ӵ�еĲ���ô�죿�Ƿ�Ҫ�޶��������ڵ�λ���͸�ʽ�����м�鲻ͬ�������Ƿ�����ͬ�ĳ�������

                // װ���������Ĳ����
                int nRet = LoadItems(this.PublishTime,
                    out strError);
                if (nRet == -1)
                    return -1;
            }



            return 0;
        }



        #region ���ݳ�Ա

        public string PublishTime
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "publishTime");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "publishTime", value);
            }
        }

        public string Issue
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "issue");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "issue", value);
            }
        }

        public string Volume
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "volume");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "volume", value);
            }
        }

        public string Zong
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "zong");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "zong", value);
            }
        }

        // 2010/3/28
        public string Comment
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "comment");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "comment", value);
            }
        }

        public string OrderInfo
        {
            get
            {
                if (this.dom == null)
                    return "";

                // ����ж��<orderInfo>Ԫ�أ�Ҫɾ�������
                {
                    XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("orderInfo");
                    if (nodes.Count > 1)
                    {
                        for (int i = 1; i < nodes.Count; i++)
                        {
                            XmlNode node = nodes[i];
                            node.ParentNode.RemoveChild(node);
                        }
                    }
                }

                return DomUtil.GetElementInnerXml(this.dom.DocumentElement,
                    "orderInfo");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("dom��δ��ʼ��");

                // ����ж��<orderInfo>Ԫ�أ�Ҫɾ�������
                {
                    XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("orderInfo");
                    if (nodes.Count > 1)
                    {
                        for (int i = 1; i < nodes.Count; i++)
                        {
                            XmlNode node = nodes[i];
                            node.ParentNode.RemoveChild(node);
                        }
                    }
                }

                DomUtil.SetElementInnerXml(this.dom.DocumentElement,
                    "orderInfo",
                    value);
            }
        }

        public string RefID
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "refID");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "refID", value);
            }
        }

        // 2010/4/7
        public string Operations
        {
            get
            {
                if (this.dom == null)
                    return "";

                // ����ж��<operations>Ԫ�أ�Ҫɾ�������
                {
                    XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("operations");
                    if (nodes.Count > 1)
                    {
                        for (int i = 1; i < nodes.Count; i++)
                        {
                            XmlNode node = nodes[i];
                            node.ParentNode.RemoveChild(node);
                        }
                    }
                }

                return DomUtil.GetElementInnerXml(this.dom.DocumentElement,
                    "operations");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("dom��δ��ʼ��");

                // ����ж��<operations>Ԫ�أ�Ҫɾ�������
                {
                    XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("operations");
                    if (nodes.Count > 1)
                    {
                        for (int i = 1; i < nodes.Count; i++)
                        {
                            XmlNode node = nodes[i];
                            node.ParentNode.RemoveChild(node);
                        }
                    }
                }

                DomUtil.SetElementInnerXml(this.dom.DocumentElement,
                    "operations",
                    value);
            }
        }

        #endregion

        // ���û���ˢ��һ����������
        // ���ܻ��׳��쳣
        public void SetOperation(
            string strAction,
            string strOperator,
            string strComment)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<operations />");

            string strInnerXml = this.Operations;
            if (String.IsNullOrEmpty(strInnerXml) == false)
            {
                dom.DocumentElement.InnerXml = this.Operations;
            }

            XmlNode node = dom.DocumentElement.SelectSingleNode("operation[@name='" + strAction + "']");
            if (node == null)
            {
                node = dom.CreateElement("operation");
                dom.DocumentElement.AppendChild(node);
                DomUtil.SetAttr(node, "name", strAction);
            }

            DomUtil.SetAttr(node, "time", DateTimeUtil.Rfc1123DateTimeString(DateTime.Now.ToUniversalTime()));
            DomUtil.SetAttr(node, "operator", strOperator);
            if (String.IsNullOrEmpty(strComment) == false)
                DomUtil.SetAttr(node, "comment", strComment);

            this.Operations = dom.DocumentElement.InnerXml;
        }

        // ��ò�ο�ID�б�
        public int GetItemRefIDs(out List<string> ids,
            out string strError)
        {
            strError = "";
            ids = new List<string>();

            XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*/distribute");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strDistribute = node.InnerText.Trim();
                if (String.IsNullOrEmpty(strDistribute) == true)
                    continue;

                LocationCollection locations = new LocationCollection();
                int nRet = locations.Build(strDistribute,
                    out strError);
                if (nRet == -1)
                    return -1;

                for (int j = 0; j < locations.Count; j++)
                {
                    Location location = locations[j];

                    // ��δ���������������
                    if (location.RefID == "*"
                        || String.IsNullOrEmpty(location.RefID) == true)
                        continue;

                    ids.Add(location.RefID);
                }
            }

            return 0;
        }



        // IssueBindingItem �������
        // parameters:
        //      p_x   �Ѿ����ĵ����ꡣ���ĵ����Ͻ�Ϊ(0,0)
        //      type    Ҫ���Ե����¼���Ҷ������������͡����Ϊnull����ʾһֱ��ĩ��
        public void HitTest(long p_x,
            long p_y,
            Type dest_type,
            out HitTestResult result)
        {
            result = new HitTestResult();

            if (dest_type == typeof(IssueBindingItem))
            {
                result.AreaPortion = AreaPortion.Content;
                result.X = p_x;
                result.Y = p_y;
                result.Object = this;
                return;
            }

            if (p_x < this.Container.m_nLeftTextWidth)
            {
                result.AreaPortion = AreaPortion.LeftText;
                result.X = p_x;
                result.Y = p_y;
                result.Object = this;
                return;
            }

            p_x -= this.Container.m_nLeftTextWidth;
            long x0 = 0;
            for (int i = 0; i < this.Cells.Count; i++)
            {
                Cell cell = this.Cells[i];
                if (p_x >= x0 && p_x < x0 + this.Container.m_nCellWidth)
                {
                    if (cell == null)
                    {
                        if (dest_type == typeof(NullCell))
                        {
                            result.AreaPortion = AreaPortion.Content;
                            //result.X = i;   // cell�������е�indexλ��
                            //result.Y = this.Container.Issues.IndexOf(this); // issue��indexλ��
                            result.Object = new NullCell(i, this.Container.Issues.IndexOf(this));
                            return;
                        }

                        result.AreaPortion = AreaPortion.Blank; // �հײ���
                        result.X = p_x;
                        result.Y = p_y;
                        result.Object = null;
                        return;
                    }

                    /*
                    result.AreaPortion = AreaPortion.Content;
                    result.X = p_x;
                    result.Y = p_y;
                    result.Object = cell;
                     * */
                    cell.HitTest(p_x - x0,
                        p_y,
                        dest_type,
                        out result);
                    return;
                }

                x0 += this.Container.m_nCellWidth;
            }

            if (dest_type == typeof(NullCell))
            {
                result.AreaPortion = AreaPortion.Content;
                result.Object = new NullCell((int)(p_x / this.Container.m_nCellWidth),
                    this.Container.Issues.IndexOf(this));
                return;
            }

            result.AreaPortion = AreaPortion.Blank; // �հײ���
            result.X = p_x;
            result.Y = p_y;
            result.Object = null;
        }

        // ��ö���/���������ַ���
        // return:
        //      -2  ȫȱ
        //      -1  ȱ
        //      0   ����
        //      1   �������ȵ��뻹Ҫ��
        int GetOrderAndArrivedCountString(out string strResult)
        {
            bool bMissing = false;
            bool bOverflow = false;
            int nTotalOrderCopy = 0;
            int nTotalArrivedCopy = 0;
            for (int i = 0; i < this.OrderItems.Count; i++)
            {
                OrderBindingItem order = this.OrderItems[i];
                string strOrderCopy = IssueBindingItem.GetOldValue(order.Copy);
                string strArrivedCopy = IssueBindingItem.GetNewValue(order.Copy);

                int nOrderCopy = IssueBindingItem.GetNumberValue(strOrderCopy);
                int nArrivedCopy = IssueBindingItem.GetNumberValue(strArrivedCopy);

                if (nArrivedCopy < nOrderCopy)
                    bMissing = true;
                if (nArrivedCopy > nOrderCopy)
                    bOverflow = true;

                nTotalOrderCopy += nOrderCopy;
                nTotalArrivedCopy += nArrivedCopy;
            }

            int nState = 0;
            if (nTotalArrivedCopy == 0)
                nState = -2;
            else if (bMissing == false && bOverflow == true)
            {
                Debug.Assert(nTotalArrivedCopy > nTotalOrderCopy, "");
                nState = 1;
            }
            else if (bMissing == true)
            {
                // ע�⣬�������ܳ����ܶ�����������ֻҪĳһ�����ֲ��㣬ȫ�����㲻��
                nState = -1;
            }
            else
            {
                Debug.Assert(nTotalArrivedCopy >= nTotalOrderCopy, "");
                nState = 0;
            }

            strResult = nTotalArrivedCopy.ToString() + "/" + nTotalOrderCopy.ToString();

            return nState;
        }

        // ��üƻ������򵽴�����֡�
        // �������հ׸��ӡ��϶������
        int GetFreeArrivedCount()
        {
            int nCount = 0;
            for (int i = 0; i < this.Cells.Count; i++)
            {
                Cell cell = this.Cells[i];
                if (cell == null)
                    continue;
                if (cell is GroupCell)
                    continue;
                if (cell.item == null)
                    continue;
                if (cell.item.OrderInfoPosition.X != -1)
                    continue;
                if (cell.item.Calculated == true)
                    continue;
                if (cell.item.IsParent == true)
                    continue;
                nCount++;
            }

            return nCount;
        }

        // ����һ������
        internal void Paint(
            int nLineNo,
            long start_x,
            long start_y,
            PaintEventArgs e)
        {
            if (BindingControl.TooLarge(start_x) == true
    || BindingControl.TooLarge(start_y) == true)
                return;

            int x0 = (int)start_x;
            int y0 = (int)start_y;

            RectangleF rect;

            // ���Ƶ�һ�������ھ�����
            rect = new RectangleF(
                x0,
                y0,
                this.LeftTextWidth,
                this.Height);

            // �Ż�
            if (rect.IntersectsWith(e.ClipRectangle) == true)
            {
                /*
                if (this.Selected == true)
                {
                    // ѡ���ı���
                    RectangleF rect1 = new RectangleF(
                       x0,
                       y0,
                       this.LeftTextWidth,
                        this.Height);
                    Brush brush = new SolidBrush(this.Container.SelectedBackColor);
                    e.Graphics.FillRectangle(brush,
                        rect1);
                }
                else
                {
                    // �ұߵ�����
                    e.Graphics.DrawLine(new Pen(Color.LightGray, (float)1),
                        new PointF(x0 + this.LeftTextWidth, y0),
                        new PointF(x0 + this.LeftTextWidth, y0 + this.Height));
                }
                 * */

                PaintLeftTextArea(
                    nLineNo,
                    x0,
                    y0,
                    this.LeftTextWidth,
                    (int)this.Height,
                    e);

            }

            int x = x0;
            int y = y0;

            // �����·�
            x = x0 + this.LeftTextWidth;
            y = y0;

            NullCell null_cell = null;
            if (this.Container.FocusObject is NullCell)
            {
                null_cell = (NullCell)this.Container.FocusObject;
                if (null_cell != null)
                {
                    if (null_cell.Y != nLineNo)
                        null_cell = null;
                    else if (null_cell.X >= this.Container.m_nMaxItemCountOfOneIssue)
                        null_cell = null;   // �����ұ߼��޷�Χ�Ĳ�Ҫ��ʾ
                }
            }

            // �Ż�
            int nStartIndex = (e.ClipRectangle.Left - x) / this.Container.m_nCellWidth;
            nStartIndex = Math.Max(0, nStartIndex);
            x += this.Container.m_nCellWidth * nStartIndex;

            // �Ը��������ѭ������������
            for (int i = nStartIndex; i < this.Cells.Count; i++)
            {
                // �Ż�
                if (x > e.ClipRectangle.Right)
                    break;

                Cell cell = this.Cells[i];
                if (cell != null)
                {
                    cell.Paint(x, y, e);

                    if (null_cell != null)
                    {
                        if (null_cell.X == i)
                            null_cell = null;   // �Ѿ����������ƹ���
                    }
                }
                x += this.Container.m_nCellWidth;
            }

            if (null_cell != null)
            {
                null_cell.Paint(
                    this.Container,
                    x0 + this.LeftTextWidth + this.Container.m_nCellWidth * null_cell.X,
                    y0,
                    e);
            }

            // ��������
            if (this.m_bFocus == true)
            {
                rect = new RectangleF(
    start_x,
    start_y,
    this.Width,
    this.Height);
                rect.Inflate(-1, -1);
                if (rect.IntersectsWith(e.ClipRectangle) == true)
                {
                    ControlPaint.DrawFocusRectangle(e.Graphics,
                    Rectangle.Round(rect));
                }
            }
        }

        // ����һ���ڵ�������ֲ��֡��������ھ����Ϣ
        void PaintLeftTextArea(
            int nLineNo,
            int start_x,
            int start_y,
            int nWidth11,
            int nHeight11,
            PaintEventArgs e)
        {
            Pen penBorder = new Pen(Color.FromArgb(255, Color.LightGray), (float)1);

            Rectangle rectFrame = new Rectangle(start_x, start_y, nWidth11, nHeight11);

            Brush brushBack = null;
            if (this.Selected == true)
            {
                // ѡ���ı���
                /*
                RectangleF rect1 = new RectangleF(
                   x0,
                   y0,
                   this.LeftTextWidth,
                    this.Height);
                 * */
                brushBack = new SolidBrush(this.Container.SelectedBackColor);
            }
            else
            {
                brushBack = new SolidBrush(this.Container.IssueBoxBackColor);
            }

            /*
            e.Graphics.FillRectangle(brushBack,
                    rectFrame);
             * */

            /*
            // ������
            e.Graphics.DrawLine(penBorder,
                new PointF(rectFrame.X, rectFrame.Y),
                new PointF(rectFrame.X, rectFrame.Y + rectFrame.Height)
                );

            // �Ϸ�����
            e.Graphics.DrawLine(penBorder,
                new PointF(rectFrame.X, rectFrame.Y),
                new PointF(rectFrame.X + rectFrame.Width, rectFrame.Y)
                );

            // �ұߵ�����
            e.Graphics.DrawLine(penBorder,
                new PointF(rectFrame.X+rectFrame.Width, rectFrame.Y),
                new PointF(rectFrame.X + rectFrame.Width, rectFrame.Y + rectFrame.Height)
                );

             * */
            bool bFirstLine = false;
            bool bTailLine = false;
            if (nLineNo == 0)
                bFirstLine = true;
            if (nLineNo == this.Container.Issues.Count - 1)
                bTailLine = true;
            string strMask = "++++";
            if (bFirstLine && bTailLine)
                strMask = "+rr+";
            else if (bFirstLine == true)
                strMask = "+r++";
            else if (bTailLine == true)
                strMask = "++r+";

            BindingControl.PartRoundRectangle(
                e.Graphics,
penBorder,
brushBack,
rectFrame,
10,
strMask); // ���� ���� ���� ����

            rectFrame = GuiUtil.PaddingRect(this.Container.LeftTextMargin,
rectFrame);

            Rectangle rectContent = GuiUtil.PaddingRect(this.Container.LeftTextPadding,
    rectFrame);

            /*
            // �������屳��
            BindingControl.PaintButton(e.Graphics,
            Color.Red,
            rectContent);
             * */


            int x0 = rectContent.X;
            int y0 = rectContent.Y;
            int nWidth = rectContent.Width;
            int nHeight = rectContent.Height;

            Color colorDark = this.Container.IssueBoxForeColor;
            Color colorGray = this.Container.IssueBoxGrayColor;
            Brush brushText = null;

            int nMaxWidth = nWidth;
            int nMaxHeight = nHeight;
            int nUsedHeight = 0;
            SizeF size;

            string strPublishTime = this.PublishTime;

            bool bFree = false; // �Ƿ�Ϊ������
            if (String.IsNullOrEmpty(strPublishTime) == true)
                bFree = true;

            bool bFirstIssue = false;
            if (this.Container.IsYearFirstIssue(this) == true)
                bFirstIssue = true;

            // Ԥ�Ȼ���ںţ��Ա�������α���
            string strNo = "";
            string strYear = "";
            if (bFree == true)
            {
                strNo = "(����)";
            }
            else
            {
                strYear = IssueUtil.GetYearPart(strPublishTime);
                strNo = this.Issue;
            }

            size = e.Graphics.MeasureString(strNo,
    this.Container.m_fontTitleLarge);
            if (size.Width > nMaxWidth)
                size.Width = nMaxWidth;
            if (size.Height > nMaxHeight)
                size.Height = nMaxHeight;


            // ���ⱳ��
            if (bFirstIssue == true || bFree == true)
            {
                RectangleF rect1 = new RectangleF(
                    x0,
                    y0,
                    nMaxWidth,
                    size.Height);

                // ���� -- ����
                LinearGradientBrush brushGradient = new LinearGradientBrush(
new PointF(rect1.X, rect1.Y + rect1.Height),
new PointF(rect1.X + rect1.Width, rect1.Y),
this.Container.IssueBoxBackColor,
ControlPaint.Light(this.Container.IssueBoxForeColor, 0.99F)
);

                e.Graphics.FillRectangle(brushGradient,
                    rect1);
            }

            Color colorSideBar = Color.FromArgb(0, 255, 255, 255);
            //Padding margin = this.Container.LeftTextMargin;
            //Padding padding = this.Container.LeftTextPadding;

            // �½��ĺͷ������޸ĵģ��������ɫ��Ҫ�趨
            if (this.NewCreated == true)
            {
                // �´����ĵ���
                colorSideBar = this.Container.NewBarColor;
            }
            else if (this.Changed == true)
            {
                // �޸Ĺ��ĵĵ���
                colorSideBar = this.Container.ChangedBarColor;
            }

            {
                // ���������
                Brush brushSideBar = new SolidBrush(colorSideBar);
                RectangleF rectSideBar = new RectangleF(
    start_x,
    y0,
    Math.Max(4, this.Container.LeftTextMargin.Left),
    nMaxHeight);
                e.Graphics.FillRectangle(brushSideBar, rectSideBar);
            }

            if (this.Virtual == true)
            {

                float nLittleWidth = Math.Min(nMaxWidth,
                    nMaxHeight);

                RectangleF rectMask = new RectangleF(
                    x0 + nMaxWidth/2 - nLittleWidth/2,
                    y0 + nMaxHeight/2 - nLittleWidth/2,
                    nLittleWidth,
                    nLittleWidth);
                Cell.PaintDeletedMask(rectMask,
                    Color.LightGray,
                    e,
                    false);
            }

            // ��һ������ ���+�ں�
            // �������ĳ��ĵ�һ�ڣ��������ʾΪ��ɫ
            {


                // 1) �ں�
                brushText = new SolidBrush(colorDark);
                // size�����Ѿ���strNo�ĳߴ�
                RectangleF rect = new RectangleF(
                    x0 + nMaxWidth - size.Width,   // ����
                    y0,
                    size.Width,
                    size.Height);
                float fNoLeft = x0 + nMaxWidth - size.Width;  // �����������

                e.Graphics.FillRectangle(brushText, rect);


                Brush brushBackground = new SolidBrush(this.Container.IssueBoxBackColor);

                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Far;
                stringFormat.LineAlignment = StringAlignment.Near;
                stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                e.Graphics.DrawString(strNo,
                    this.Container.m_fontTitleLarge,
                    brushBackground,
                    rect,
                    stringFormat);

                // 2) ���
                if (String.IsNullOrEmpty(strYear) == false)
                {
                    if (bFirstIssue == true)
                        brushText = new SolidBrush(colorDark);
                    else
                        brushText = new SolidBrush(colorGray);

                    size = e.Graphics.MeasureString(strYear,
                        this.Container.m_fontTitleLarge);
                    if (size.Width > nMaxWidth)
                        size.Width = nMaxWidth;
                    if (size.Height > nMaxHeight)
                        size.Height = nMaxHeight;
                    rect = new RectangleF(
                        x0,  // + 4 // ����
                        y0,
                        size.Width,
                        size.Height);
                    /*
                    // ���Ϊ�����һ���ڣ�����Ҫ���Ƶ�ɫ���ֱ���
                    if (bFirstIssue == true)
                    {
                        RectangleF rect1 = new RectangleF(
                            x0,   // ����
                            y0,
                            fNoLeft - x0,   // �ѿ������Ϊ��ͨ�����ұߵ�no����
                            size.Height);
                        Brush brushRect = new SolidBrush(ControlPaint.Light(this.Container.ForeColor, 0.99F));
                        // ���� -- ����
                        LinearGradientBrush brushGradient = new LinearGradientBrush(
    new PointF(rect1.X, rect1.Y),
    new PointF(rect1.X + rect1.Width, rect1.Y + rect1.Height),
    Color.White,
    ControlPaint.Light(this.Container.ForeColor, 0.99F)
    );

                        e.Graphics.FillRectangle(brushGradient,
                            rect1);
                    }
                     * */

                    stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Near;
                    stringFormat.LineAlignment = StringAlignment.Near;
                    stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                    e.Graphics.DrawString(strYear,
                        this.Container.m_fontTitleLarge,
                        brushText,
                        rect,
                        stringFormat);
                }

                nUsedHeight += (int)size.Height;
            }

            if (nUsedHeight >= nMaxHeight)
                return;

            // �ڶ������� ��������
            // ��ɫ��������С
            if (bFree == false)
            {
                y0 += (int)size.Height;
                string strText = BindingControl.GetDisplayPublishTime(this.PublishTime);

                brushText = new SolidBrush(colorGray);
                size = e.Graphics.MeasureString(strText,
                    this.Container.m_fontTitleSmall);
                if (size.Width > nMaxWidth)
                    size.Width = nMaxWidth;
                if (size.Height > nMaxHeight - nUsedHeight)
                    size.Height = nMaxHeight - nUsedHeight;
                RectangleF rect = new RectangleF(
                    x0 + nMaxWidth - size.Width,   // ����
                    y0,
                    size.Width,
                    size.Height);

                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Far;
                stringFormat.LineAlignment = StringAlignment.Near;
                stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                e.Graphics.DrawString(strText,
        this.Container.m_fontTitleSmall,
        brushText,
        rect,
        stringFormat);
                nUsedHeight += (int)size.Height;
            }

            if (nUsedHeight >= nMaxHeight)
                return;

            // ���������� ���+���ں�
            // ��ɫ��������С
            if (bFree == false)
            {
                string strText = this.Comment;

                int nTrimLength = 6;
                if (strText.Length > nTrimLength)
                    strText = strText.Substring(0, nTrimLength) + "...";

                if (String.IsNullOrEmpty(this.Volume) == false)
                {
                    if (String.IsNullOrEmpty(strText) == false)
                        strText += " ";
                    strText += "v." + this.Volume;
                }

                if (String.IsNullOrEmpty(this.Zong) == false)
                {
                    if (String.IsNullOrEmpty(strText) == false)
                        strText += " ";
                    strText += "��." + this.Zong;
                }

                if (String.IsNullOrEmpty(strText) == false)
                {
                    y0 += (int)size.Height;
                    brushText = new SolidBrush(colorGray);
                    size = e.Graphics.MeasureString(strText,
                        this.Container.m_fontTitleSmall);
                    if (size.Width > nMaxWidth)
                        size.Width = nMaxWidth;
                    if (size.Height > nMaxHeight - nUsedHeight)
                        size.Height = nMaxHeight - nUsedHeight;
                    RectangleF rect = new RectangleF(
                        x0 + nMaxWidth - size.Width,   // ����
                        y0,
                        size.Width,
                        size.Height);

                    StringFormat stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Far;
                    stringFormat.LineAlignment = StringAlignment.Near;
                    stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                    e.Graphics.DrawString(strText,
            this.Container.m_fontTitleSmall,
            brushText,
            rect,
            stringFormat);
                    nUsedHeight += (int)size.Height;
                }
            }

            if (nUsedHeight >= nMaxHeight)
                return;
            //
            // ���������� ����/��������
            // ��ɫ��������С
            if (bFree == false)
            {
                string strText = "";
                // ��ö���/���������ַ���
                // return:
                //      -2  ȫȱ
                //      -1  ȱ
                //      0   ����
                //      1   �������ȵ��뻹Ҫ��
                int nState = GetOrderAndArrivedCountString(out strText);

                int nFreeCount = GetFreeArrivedCount();
                if (nFreeCount > 0)
                    strText += " + " + nFreeCount.ToString();

                if (String.IsNullOrEmpty(strText) == false)
                {
                    y0 += (int)size.Height;
                    // brushText = new SolidBrush(colorGray);
                    if (nState == -2)
                        brushText = new SolidBrush(colorGray);
                    else if (nState == -1)
                        brushText = new SolidBrush(Color.DarkRed);
                    else if (nState == 0)
                        brushText = new SolidBrush(Color.DarkGreen);
                    else
                    {
                        Debug.Assert(nState == 1, "");
                        brushText = new SolidBrush(Color.DarkOrange);
                    }

                    size = e.Graphics.MeasureString(strText,
                        this.Container.m_fontTitleSmall);
                    if (size.Width > nMaxWidth)
                        size.Width = nMaxWidth;
                    if (size.Height > nMaxHeight - nUsedHeight)
                        size.Height = nMaxHeight - nUsedHeight;
                    RectangleF rect = new RectangleF(
                        x0 + nMaxWidth - size.Width,   // ����
                        y0,
                        size.Width,
                        size.Height);

                    StringFormat stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Far;
                    stringFormat.LineAlignment = StringAlignment.Near;
                    stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                    e.Graphics.DrawString(strText,
            this.Container.m_fontTitleSmall,
            brushText,
            rect,
            stringFormat);
                    int nImageIndex = 0;
                    if (nState == -1)
                        nImageIndex = 1;
                    else if (nState == 0 || nState == 1)
                        nImageIndex = 2;

                    ImageAttributes attr = new ImageAttributes();
                    attr.SetColorKey(this.Container.imageList_treeIcon.TransparentColor,
                        this.Container.imageList_treeIcon.TransparentColor);

                    Image image = this.Container.imageList_treeIcon.Images[nImageIndex];
                    /*
                    e.Graphics.DrawImage(image,
                        rect.X + rect.Width - size.Width - image.Width - 4,
                        rect.Y);
                     * */
                    e.Graphics.DrawImage(
                        image,
                        new Rectangle(
                        (int)(rect.X + rect.Width - size.Width - image.Width - 4),
                        (int)rect.Y,
                        image.Width,
                        image.Height),
                        0, 0, image.Width, image.Height,
                        GraphicsUnit.Pixel,
                        attr);
                }



            }


            // ����ģʽ
            if (bFree == false)
            {
                ImageAttributes attr = new ImageAttributes();
                attr.SetColorKey(this.Container.imageList_layout.TransparentColor,
                    this.Container.imageList_layout.TransparentColor);
                int nImageIndex = 0;
                if (this.IssueLayoutState == IssueLayoutState.Accepting)
                    nImageIndex = 1;

                Image image = this.Container.imageList_layout.Images[nImageIndex];

                // ��������
                x0 = rectContent.X;
                y0 = rectContent.Y;

                Rectangle rect = new Rectangle(
                    x0, // + 8   // ����
                    y0 + nHeight - image.Height,    // ����
                    image.Width,
                    image.Height);

                if (rect.IntersectsWith(e.ClipRectangle) == true)
                {
                    e.Graphics.DrawImage(
                        image,
                        rect,
                        0, 0, image.Width, image.Height,
                        GraphicsUnit.Pixel,
                        attr);
                }
            }

        }

#if NOOOOOOOOOOOOOOOOOOOO
        // �������ڵ�����ֺ�ͼ��Icon
        public void SetNodeCaption(TreeNode tree_node)
        {
            Debug.Assert(this.dom != null, "");

            string strPublishTime = DomUtil.GetElementText(this.dom.DocumentElement,
                "publishTime");
            string strIssue = DomUtil.GetElementText(this.dom.DocumentElement,
                "issue");
            string strVolume = DomUtil.GetElementText(this.dom.DocumentElement,
                "volume");
            string strZong = DomUtil.GetElementText(this.dom.DocumentElement,
                "zong");

            int nOrderdCount = 0;
            int nRecievedCount = 0;
            // �����յĲ���
            // string strOrderInfoXml = "";

            if (this.dom == null)
                goto SKIP_COUNT;

            {

                XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("orderInfo/*/copy");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    string strCopy = node.InnerText.Trim();
                    if (String.IsNullOrEmpty(strCopy) == true)
                        continue;

                    string strNewCopy = "";
                    string strOldCopy = "";
                    OrderDesignControl.ParseOldNewValue(strCopy,
                        out strOldCopy,
                        out strNewCopy);

                    int nNewCopy = 0;
                    int nOldCopy = 0;

                    try
                    {
                        if (String.IsNullOrEmpty(strNewCopy) == false)
                        {
                            nNewCopy = Convert.ToInt32(strNewCopy);
                        }
                        if (String.IsNullOrEmpty(strOldCopy) == false)
                        {
                            nOldCopy = Convert.ToInt32(strOldCopy);
                        }
                    }
                    catch
                    {
                    }

                    nOrderdCount += nOldCopy;
                    nRecievedCount += nNewCopy;
                }
            }

        SKIP_COUNT:

            if (this.OrderedCount == -1 && nOrderdCount > 0)
                this.OrderedCount = nOrderdCount;

            tree_node.Text = strPublishTime + " no." + strIssue + " ��." + strZong + " v." + strVolume + " (" + nRecievedCount.ToString() + ")";

            if (this.OrderedCount == -1)
            {
                if (nRecievedCount == 0)
                    tree_node.ImageIndex = IssueManageControl.TYPE_RECIEVE_ZERO;
                else
                    tree_node.ImageIndex = IssueManageControl.TYPE_RECIEVE_NOT_COMPLETE;
            }
            else
            {
                if (nRecievedCount >= this.OrderedCount)
                    tree_node.ImageIndex = IssueManageControl.TYPE_RECIEVE_COMPLETED;
                else if (nRecievedCount > 0)
                    tree_node.ImageIndex = IssueManageControl.TYPE_RECIEVE_NOT_COMPLETE;
                else
                    tree_node.ImageIndex = IssueManageControl.TYPE_RECIEVE_ZERO;
            }

            tree_node.SelectedImageIndex = tree_node.ImageIndex;
        }

#endif

        // TODO: ������ֹ
        // װ���������Ĳ����
        int LoadItems(string strPublishTime,
            out string strError)
        {
            strError = "";

            this.Items.Clear();

            List<string> XmlRecords = null;

            Debug.Assert(this.Container != null, "");

            // �����¼��ӿ�this.GetItemInfo���������Ĳ���Ϣ
            // return:
            //      -1  error
            //      >-0 ����ü�¼������(XmlRecords.Count)
            int nRet = this.Container.DoGetItemInfo(strPublishTime,
                out XmlRecords,
                out strError);
            if (nRet == -1)
                return -1;

            for (int i = 0; i < XmlRecords.Count; i++)
            {
                string strXml = XmlRecords[i];
                ItemBindingItem item = new ItemBindingItem();
                nRet = item.Initial(strXml, out strError);
                if (nRet == -1)
                    return -1;

                item.Container = this;
                this.Items.Add(item);
            }

            return 0;
        }

        // װ���������Ĳ����
        internal int InitialLoadItems(
            string strPublishTime,
            out string strError)
        {
            strError = "";

            this.Items.Clear();

            Debug.Assert(this.Container != null, "");

            Debug.Assert(String.IsNullOrEmpty(strPublishTime) == false, "");

            // 2010/9/21 add
            if (strPublishTime.IndexOf("-") != -1)
            {
                strError = "�������� '"+strPublishTime+"' Ӧ��Ϊ������̬";
                return -1;
            }

            Debug.Assert(strPublishTime.IndexOf("-") == -1, "��������Ӧ��Ϊ������̬");

            for (int i = 0; i < this.Container.InitialItems.Count; i++)
            {
                ItemBindingItem item = this.Container.InitialItems[i];
                if (strPublishTime == item.PublishTime)
                {
                    item.Container = this;
                    this.Items.Add(item);

                    // ʹ�ú����������
                    this.Container.InitialItems.RemoveAt(i);
                    i--;
                }
            }

            return 0;
        }
    }

    // �ڶ���Σ������
    internal class ItemBindingItem
    {
        public IssueBindingItem Container = null;   // ���ContainerΪ�գ������ں϶��������ֱ�������ؼ�����

        public object Tag = null;   // ���ڴ����Ҫ���ӵ��������Ͷ���

        internal bool Missing = false;  // ��������Ϊ���ö����ʱ�򣬱���Ա������ʾ�հ׵ĸ���

        internal bool IsParent = false; // �Ƿ�Ϊ�϶���?

        public bool Deleted = false;    // ��¼�Ƿ��Ѿ���ɾ��?

        public string RecPath = "";

        public bool NewCreated = false; // �Ƿ�Ϊ�´����Ķ���

        public bool Calculated = false; // �Ƿ�ΪԤ��ġ���δ����Ĳ�

        public bool Locked = false;   // �Ƿ񳬳���ǰ�û���Ͻ��Χ?

        // �ɹ���Ϣ����
            // һ����<orderInfo>�µ�<root>ƫ�ƣ�һ����<root>��<distribute>����Ĺݲصص�ƫ��
        public Point OrderInfoPosition = new Point(-1,-1);  // -1 ��ʾ��δ��ʼ��

        // �Ƿ�Ϊλ�ڶ������еĵ�Ԫ
        public bool InGroup
        {
            get
            {
                if (this.OrderInfoPosition.X == -1)
                {
                    Debug.Assert(this.OrderInfoPosition.Y == -1, "");
                    return false;
                }
                return true;
            }
        }

        // ��������������GroupCell����
        internal GroupCell GroupCell
        {
            get
            {
                IssueBindingItem issue = this.Container;
                Debug.Assert(issue != null, "");

                if (issue.IssueLayoutState != IssueLayoutState.Accepting)
                    return null;
                if (this.InGroup == false)
                    return null;
                int nOrderInfoIndex = this.OrderInfoPosition.X;

                return issue.GetGroupCellHead(nOrderInfoIndex);
            }
        }

        // string m_strXml = "";

        public string Xml
        {
            get
            {
                if (dom != null)
                    return dom.OuterXml;

                return "";
                // return m_strXml;
            }
            /*
            set
            {
                m_strXml = value;
            }
             * */
        }

        internal XmlDocument dom = null;

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed = false;

        // �Լ��Ƿ�Ϊ�϶�����Ա�᣿== true����ʾ�Ѿ���װ��
        // == false, ��ʾΪ���ᣬ���ߺ϶���
        public bool IsMember
        {
            get
            {
                if (this.ParentItem != null)
                    return true;
                return false;
            }
        }

        // ����Ǻ϶��������������Ĳ���Ϣ����
        // public List<ItemBindingItem> MemberItems = new List<ItemBindingItem>();
        public List<ItemBindingItem> MemberItems
        {
            get
            {
                List<ItemBindingItem> results = new List<ItemBindingItem>();
                for (int i = 0; i < this.MemberCells.Count; i++)
                {
                    Cell cell = this.MemberCells[i];
                    if (cell == null)
                        continue;
                    if (cell.item == null)
                        continue;
                    results.Add(cell.item);
                }

                if (results.Count > 0)
                {
                    Debug.Assert(this.IsMember == false, "");   // == true BUG?
                }

                return results;
            }
        }

        // ����Ǻ϶�����������������ĸ��Ӷ���
        // ���������ù�ϵ������ӵ�й�ϵ
        // �������п����������Ӷ������Ϊ0
        internal List<Cell> MemberCells = new List<Cell>();

        // ���MemberCells�������ȷ��
        internal int VerifyMemberCells(out string strError)
        {
            strError = "";
            if (this.MemberCells.Count > 0)
            {
                Debug.Assert(this.IsParent == true, "");

                // ������ʱ����
                List<Cell> members = new List<Cell>();
                members.AddRange(this.MemberCells);

                members.Sort(new CellPublishTimeComparer());

                // ������޳���ʱ���ظ�
                string strPrevPublishTime = "";
                for (int i = 0; i < members.Count; i++)
                {
                    Cell cell = members[i];
                    string strPublishTime = cell.Container.PublishTime;

                    if (strPublishTime == strPrevPublishTime)
                    {
                        strError = "�����˶�����Ӿ�����ͬ�ĳ���ʱ�� '" + strPublishTime + "'";
                        return -1;
                    }

                    strPrevPublishTime = strPublishTime;
                }

                // ���ԭʼ�����Ƿ�����
                for (int i = 0; i < members.Count; i++)
                {
                    Cell cell = members[i];
                    string strPublishTime = cell.Container.PublishTime;

                    if (strPublishTime != this.MemberCells[i].Container.PublishTime)
                    {
                        strError = "MemberCells�ڵĸ���δ���ճ���ʱ������";
                        return -1;
                    }
                }

                // ���ParentItem�Ƿ���ȷ
                for (int i = 0; i < members.Count; i++)
                {
                    Cell cell = members[i];

                    if (cell.ParentItem != this)
                    {
                        strError = "cell.ParentItemֵ����ȷ";
                        return -1;
                    }

                    if (cell.item != null)
                    {
                        if (cell.item.ParentItem != this)
                        {
                            strError = "cell.item.ParentItemֵ����ȷ";
                            return -1;
                        }
                    }
                }

                IssueBindingItem issue = this.Container;
                int index = issue.IndexOfItem(this);    // �϶������ڵ��к�
                if (index == -1)
                {
                    strError = "��Ȼ����Container������������";
                    return -1;
                }

                if (issue.IssueLayoutState != IssueLayoutState.Binding)
                    index = -1; // ���������

                /*
                // �������ż��λ��
                if ((index % 2) != 0)
                {
                    strError = "�϶�������Ӧ����˫������λ��";
                    return -1;
                }
                 * */

                if (index != -1)
                {
                    // ����Ա���ӵ��к�
                    for (int i = 0; i < members.Count; i++)
                    {
                        Cell cell = members[i];

                        int nCol = cell.Container.IndexOfCell(cell);
                        Debug.Assert(nCol != -1, "");

                        if (cell.Container.IssueLayoutState != IssueLayoutState.Binding)
                            continue;   // ����binding layout��Ҳ���������

                        if (nCol != index + 1)
                        {
                            strError = "��Ա���� '" + cell.Container.PublishTime
                                + "' ���к�Ϊ"
                                + nCol.ToString() + "�����ɺ϶����������ĳ�Ա�к� "
                                + (index + 1).ToString() + " ������";
                            return -1;
                        }
                    }
                }

                // �˶�MemberCells.Count��ʵ����ʾ������

                IssueBindingItem first_issue = members[0].Container;
                Debug.Assert(first_issue != null, "");
                // �ҵ��к�
                int nLineNo = this.Container.Container.Issues.IndexOf(first_issue);
                Debug.Assert(nLineNo != -1, "");
                // ������ֱ����������ٸ���
                int nIssueCount = 0;

                IssueBindingItem tail_issue = members[members.Count - 1].Container;// item.MemberItems[item.MemberItems.Count - 1].Container;
                Debug.Assert(tail_issue != null, "");
                // �ҵ��к�
                int nTailLineNo = this.Container.Container.Issues.IndexOf(tail_issue);
                Debug.Assert(nTailLineNo != -1, "");

                nIssueCount = nTailLineNo - nLineNo + 1;

                if (nIssueCount != members.Count)
                {
                    strError = "��ʾ������Ϊ " + nIssueCount.ToString() + "������Ա��Ϊ " + members.Count.ToString() + "����һ��";
                    return -1;
                }
            }
            else
            {
                // ���Ǻ϶���

                IssueBindingItem issue = this.Container;
                int index = issue.IndexOfItem(this);
                if (index == -1)
                {
                    strError = "��Ȼ����Container������������";
                    return -1;
                }

                /*
                // �������ż��λ��
                if ((index % 2) != 0)
                {
                    strError = "�������Ӧ����˫����Ҳ�λ��";
                    return -1;
                }
                 * */
            }

            return 0;
        }

        // ��MemberCells�����е����������ض��ڵĸ���
        internal void RemoveMemberCell(IssueBindingItem issue)
        {
            for (int i = 0; i < this.MemberCells.Count; i++)
            {
                Cell current = this.MemberCells[i];

                IssueBindingItem current_issue = current.Container;
                Debug.Assert(current_issue != null, "");

                if (current_issue == issue)
                {
                    this.MemberCells.RemoveAt(i);
                    i--;
                }
            }
        }

        // ��Cell������뵽MemberCells�����е��ʵ�λ��
        internal void InsertMemberCell(Cell cell)
        {
            Debug.Assert(this.IsParent == true, "");

            this.MemberCells.Remove(cell);

            Debug.Assert(this.MemberCells.IndexOf(cell) == -1, "����ǰ�Ѿ���������");

            string strPublishTime = cell.Container.PublishTime;

            int nInsertIndex = -1;
            string strLastPublishTime = "";
            for (int i = 0; i < this.MemberCells.Count; i++)
            {
                Cell current = this.MemberCells[i];

                IssueBindingItem issue = current.Container;
                Debug.Assert(issue != null, "");

                if (String.Compare(strPublishTime, strLastPublishTime) >= 0
                    && String.Compare(strPublishTime, issue.PublishTime) < 0)
                    nInsertIndex = i;

                strLastPublishTime = issue.PublishTime;
            }

            if (nInsertIndex == -1)
                this.MemberCells.Add(cell);
            else
                this.MemberCells.Insert(nInsertIndex, cell);
        }

        public void SelectAllMemberCells()
        {
            for (int i = 0; i < this.MemberCells.Count; i++)
            {
                Cell cell = this.MemberCells[i];
                if (cell.Selected == false)
                    cell.Select(SelectAction.On);
            }
        }

        // �Ƿ�Ϊ���ӹ��С�״̬
        public bool IsProcessingState()
        {
            if (Global.IncludeStateProcessing(this.State) == true)
                return true;

            return false;
        }


        // ����Ǻ϶�����������ر������������ĺ϶���
        // ���������ù�ϵ������ӵ�й�ϵ
        public ItemBindingItem ParentItem = null;

        // ��ʼ������Ϣ
        public int Initial(string strXml,
            out string strError)
        {
            strError = "";

            // this.Xml = strXml;
            this.dom = new XmlDocument();
            try
            {
                this.dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "���¼ XML װ�� DOM ʱ����: " + ex.Message;
                return -1;
            }

            return 0;
        }

        // �޸Ĳ���Ϣ
        public int ChangeItemXml(string strXml,
            out string strError)
        {
            strError = "";

            this.dom = new XmlDocument();
            try
            {
                this.dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "���¼ XML װ�� DOM ʱ����: " + ex.Message;
                return -1;
            }

            // �޸Ĵ����ĺ϶�����Ϣ
            if (this.ParentItem != null)
            {
                /*
                this.ParentItem.RefreshBindingXml();
                this.ParentItem.RefreshIntact();
                try
                {
                    this.Container.Container.UpdateObject(this.ParentItem.ContainerCell);
                }
                catch
                {
                }
                 * */
                this.ParentItem.AfterMembersChanged();
            }

            // ˢ���Լ��ĸ���
            try
            {
                this.Container.Container.UpdateObject(this.ContainerCell);
            }
            catch
            {
            }
            return 0;
        }

        // ���һ�����ӣ������Ƿ��ʺ�ɾ��
        // return:
        //      -1  ����
        //      0   ���ʺ�ɾ��
        //      1   �ʺ�ɾ��
        public int CanDelete(out string strError)
        {
            strError = "";

            if (this.Container.Container.CheckProcessingState(this) == false
                && this.Calculated == false
                && this.Deleted == false)   // 2010/4/13
            {
                strError = "���߱����ӹ��С�״̬";
                return 0;
            }

            if (this.Locked == true
    && this.Calculated == false
    && this.Deleted == false)
            {
                strError = "���ڡ�������״̬";
                return 0;
            }


            if (String.IsNullOrEmpty(this.Borrower) == false)
            {
                strError = "�н�����Ϣ";
                return 0;
            }

            return 1;
        }

        // ɾ������ǰ��׼��������
        // �Ծ��ж�����Ϣ�󶨵ĸ��Ӳ��б�Ҫʹ�ñ�����
        public int DoDelete(out string strError)
        {
            strError = "";
            int nRet = 0;

            if (this.OrderInfoPosition.X == -1)
            {
                strError = "ֻ�жԱ�������Ϣ�󶨵Ĳ����ʹ�ñ�����DoDelete()";
                return -1;
            }

            nRet = this.CanDelete(out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "�� " + this.RefID + " ���ܱ�ɾ��: " + strError;
                return -1;
            }

            // �ҵ���ص�OrderBindingItem����ˢ��<distribute>Ԫ������
            IssueBindingItem issue = this.Container;
            Debug.Assert(issue != null, "");

            Debug.Assert(this.OrderInfoPosition.X >= 0, "");
            OrderBindingItem order_item = issue.OrderItems[this.OrderInfoPosition.X];
            // TODO: Ԥ������ѵ��Ĳ�����У�Ӧ�����غ�OrderBindingItem�Ĺ�����Ϣ
            // �ѵ��Ĳ���󣬿���ͨ��refid��������Ԥ��Ĳ����ֻ��ͨ������λ��ƫ�������أ�
            // һ����<orderInfo>�µ�<root>ƫ�ƣ�һ����<root>��<distribute>����Ĺݲصص�ƫ��

            Debug.Assert(this.OrderInfoPosition.Y >= 0, "");
            // ���ض�ƫ�Ƶ�Ԥ�����мǵ�
            // �������Ľ���Ƕ�dom�����XML�ַ��������˸Ķ�
            nRet = order_item.DoDelete(this.OrderInfoPosition.Y,
            out strError);
            if (nRet == -1)
                return -1;

            bool bRefreshError = false;
            if (nRet == -2)
            {
                // ȫ��ˢ��
                nRet = issue.RefreshOrderCopy(this.OrderInfoPosition.X,
                    out strError);
                if (nRet == -1)
                {
                    bRefreshError = true;   // �ӳٱ���
                    strError = "issue.RefreshOrderCopy() error: " + strError;
                }
            }

            issue.RefreshOrderInfoPositionXY(this.OrderInfoPosition.X,
                this.OrderInfoPosition.Y,
                -1);

            issue.Changed = true;
            issue.AfterMembersChanged();    // ˢ��Issue�����ڵ�XML

            if (bRefreshError == true)
                return -1;

            return 0;
        }

        // ���Ѿ��ǵ��ĸ��ӳ�����δ�ǵ�״̬
        public int DoUnaccept(out string strError)
        {
            strError = "";
            int nRet = 0;

            if (this.OrderInfoPosition.X == -1)
            {
                strError = "ֻ�жԱ�������Ϣ�󶨵Ĳ���ܽ��г����ǵ�����";
                return -1;
            }
            if (this.Calculated == true)
            {
                strError = "ֻ�ж��Ѿ��ǵ��Ĳ���ܽ��г����ǵ�����";
                return -1;
            }
            nRet = this.CanDelete(out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "�� " + this.RefID + " ���ܱ�ɾ��: " + strError;
                return -1;
            }

            // �ҵ���ص�OrderBindingItem����ˢ��<distribute>Ԫ������
            IssueBindingItem issue = this.Container;
            Debug.Assert(issue != null, "");

            Debug.Assert(this.OrderInfoPosition.X >= 0, "");
            OrderBindingItem order_item = issue.OrderItems[this.OrderInfoPosition.X];
            // TODO: Ԥ������ѵ��Ĳ�����У�Ӧ�����غ�OrderBindingItem�Ĺ�����Ϣ
            // �ѵ��Ĳ���󣬿���ͨ��refid��������Ԥ��Ĳ����ֻ��ͨ������λ��ƫ�������أ�
            // һ����<orderInfo>�µ�<root>ƫ�ƣ�һ����<root>��<distribute>����Ĺݲصص�ƫ��

            Debug.Assert(this.OrderInfoPosition.Y >= 0, "");
            // ���ض�ƫ�Ƶ�Ԥ�����мǵ�
            // �������Ľ���Ƕ�dom�����XML�ַ��������˸Ķ�
            nRet = order_item.DoUnaccept(this.OrderInfoPosition.Y,
            out strError);
            if (nRet == -1)
                return -1;

            // ˢ�µ�ǰ����(ItemBindingItem)����ʾ
            this.RefID = "";
            // ���κ�
            this.BatchNo = "";
            this.State = "";

            IssueBindingItem.SetFieldValueFromOrderInfo(
                true,    // �Ƿ�ǿ������
                this,
                order_item);

            this.Changed = true;
            this.NewCreated = false;
            this.Calculated = true;
            this.Deleted = false;

            issue.Changed = true;
            issue.AfterMembersChanged();    // ˢ��Issue�����ڵ�XML

            // TODO: �����Acception Layout, ��Ҫˢ�´�����GroupCell����ʾ

            return 0;
        }

        // ��Ԥ���������յ�(�ǵ�)
        public int DoAccept(out string strError)
        {
            strError = "";
            int nRet = 0;

            bool bSetProcessingState = this.Container.Container.SetProcessingState;

            if (this.Calculated == false)
            {
                strError = "ֻ�ж�Ԥ��״̬�Ĳ���ܽ��мǵ�����";
                return -1;
            }

#if NO
            if (this.Locked == true)
            {
                strError = "����״̬Ϊ����ʱ ��������мǵ�����";
                return -1;
            }
#endif

            // �ҵ���ص�OrderBindingItem����ˢ��<distribute>Ԫ������
            IssueBindingItem issue = this.Container;
            Debug.Assert(issue != null, "");

            Debug.Assert(this.OrderInfoPosition.X >= 0, "");
            OrderBindingItem order_item = issue.OrderItems[this.OrderInfoPosition.X];
            // TODO: Ԥ������ѵ��Ĳ�����У�Ӧ�����غ�OrderBindingItem�Ĺ�����Ϣ
            // �ѵ��Ĳ���󣬿���ͨ��refid��������Ԥ��Ĳ����ֻ��ͨ������λ��ƫ�������أ�
            // һ����<orderInfo>�µ�<root>ƫ�ƣ�һ����<root>��<distribute>����Ĺݲصص�ƫ��

            Debug.Assert(this.OrderInfoPosition.Y >= 0, "");
            string strRefID = "";
            string strLocation = "";
            // ���ض�ƫ�Ƶ�Ԥ�����мǵ�
            // �������Ľ���Ƕ�dom�����XML�ַ��������˸Ķ�
            nRet = order_item.DoAccept(this.OrderInfoPosition.Y,
            ref strRefID,
            out strLocation,
            out strError);
            if (nRet == -1)
                return -1;

            string strBatchNo = this.Container.Container.GetAcceptingBatchNo();

            /*
            XmlNode order_node = order_item.dom.DocumentElement;
            Debug.Assert(order_item.dom != null, "");
            Debug.Assert(order_node != null, "");
             * */

            // ˢ�µ�ǰ����(ItemBindingItem)����ʾ
            this.RefID = strRefID;
            // location
            this.LocationString = strLocation;
            // ���κ�
            this.BatchNo = strBatchNo;

            // 2009/10/19
            // ״̬
            if (bSetProcessingState == true)
            {
                // �������ӹ��С�ֵ
                this.State = Global.AddStateProcessing(this.State);
            }

            /*
            // seller
            // seller���ǵ���ֵ
            if (String.IsNullOrEmpty(this.Seller) == true)
                this.Seller = order_item.Seller;

            // source
            // source��˳�β�����ֵ/��ֵ
            if (String.IsNullOrEmpty(this.Source) == true)
                this.Source = IssueBindingItem.GetNewOrOldValue(order_item.Source);

            // price
            // price��˳�β�����ֵ/��ֵ
            if (String.IsNullOrEmpty(this.Price) == true)
                this.Price = IssueBindingItem.GetNewOrOldValue(order_item.Price);
             * */
            IssueBindingItem.SetFieldValueFromOrderInfo(
                false,    // �Ƿ�ǿ������
                this,
                order_item);

            // publishTime
            this.PublishTime = issue.PublishTime;

            // volume ��ʵ�ǵ����ںš����ںš������һ���һ���ַ���
            string strVolume = VolumeInfo.BuildItemVolumeString(
                IssueUtil.GetYearPart(issue.PublishTime),
                issue.Issue,
                issue.Zong,
                issue.Volume);
            this.Volume = strVolume;

            // this.ContainerCell.Select(SelectAction.On);
            this.Changed = true;
            this.NewCreated = true;
            this.Calculated = false;
            this.Deleted = false;

            /*
            // ���û���ˢ��һ����������
            // ���ܻ��׳��쳣
            this.SetOperation(
                "create",
                this.Container.Container.Operator,
                "");
             * */

            issue.Changed = true;
            issue.AfterMembersChanged();    // ˢ��Issue�����ڵ�XML

            // TODO: �����Acception Layout, ��Ҫˢ�´�����GroupCell����ʾ

            return 0;
        }

        // ���׳��쳣��
        public void AfterMembersChanged()
        {
            bool bChanged = false;
            if (RefreshPublishTime() == true)
                bChanged = true;
            try
            {
                if (RefreshIntact() == true)
                    bChanged = true;
            }
            catch (Exception ex)
            {
                this.Intact = "����: " + ex.Message;
            }

            if (RefreshBindingXml() == true)
                bChanged = true;
            if (RefreshVolumeString() == true)
                bChanged = true;
            if (RefreshPriceString() == true)
                bChanged = true;

            if (bChanged == true)
            {
                this.Changed = true;
                try
                {
                    this.Container.Container.UpdateObject(this.ContainerCell);
                }
                catch
                {
                }
            }
        }

        // MemberCells�޸ĺ�Ҫˢ�º϶����Intactֵ
        // return:
        //      false   Intactû�з����޸�
        //      true    Intact�������޸�
        public bool RefreshIntact()
        {
            if (this.MemberCells.Count == 0)
            {
                if (this.Intact == "0")
                    return false;
                this.Intact = "0";
                return true;
            }
            // ���Intact
            string strIntact = "";
            string strError = "";
            int nRet = BuildIntactString(this.MemberCells,
                out strIntact,
                out strError);
            if (nRet == -1)
                throw new Exception(strError);
            if (this.Intact == "" && strIntact == "100")
                return false;

            if (this.Intact == strIntact)
                return false;
            this.Intact = strIntact;
            return true;
        }

        public static int BuildIntactString(List<Cell> cells,
    out string strIntact,
    out string strError)
        {
            strIntact = "";
            strError = "";

            if (cells.Count == 0)
            {
                strIntact = "0";
                return 0;
            }

            float fTotal = 0;

            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (cell == null)
                    continue;

                if (cell.item == null)
                    continue;

                string strValue = cell.item.Intact;
                if (String.IsNullOrEmpty(strValue) == true)
                {
                    fTotal += 100;
                    continue;
                }

                strValue = strValue.Replace("%", "");

                float v = 0;
                try
                {
                    v = (float)Convert.ToDecimal(strValue);
                }
                catch
                {
                    strError = "�����ֵ '" + strValue + "' ��ʽ����(��'" + cell.item.RefID + "')";
                    strIntact = "0";
                    return -1;
                }

                if (v > 100)
                {
                    strError = "�����ֵ '" + strValue + "' ��ʽ����(��'" + cell.item.RefID + "')�����ܴ���100";
                    strIntact = "0";
                    return -1;
                }

                fTotal += v;
            }

            Debug.Assert(cells.Count != 0, "");
            strIntact = (fTotal / (float)cells.Count).ToString("0.#");
            return 0;
        }

        // ���ں϶��ᣬMemberCells�޸ĺ�Ҫˢ�³���ʱ�䷶Χ�����ڳ�Ա����ߵ��ᣬˢ�³���ʱ��ֵ
        // return:
        //      false   ����ʱ�䷶Χû�з����޸�
        //      true    ����ʱ�䷶Χ�������޸�
        public bool RefreshPublishTime()
        {
            if (this.IsParent == false)
            {
                IssueBindingItem issue = this.Container;
                if (issue != null 
                    && String.IsNullOrEmpty(issue.PublishTime) == false)
                {
                    if (this.PublishTime != issue.PublishTime)
                    {
                        this.PublishTime = issue.PublishTime;
                        return true;
                    }
                }

                return false;
            }

            try
            {
                string strNewPublishTime = "";

                IssueBindingItem first_issue = this.Container;
                Debug.Assert(first_issue != null, "");

                int nFirstLineNo = this.Container.Container.Issues.IndexOf(first_issue);
                Debug.Assert(nFirstLineNo != -1, "");

                string strFirstPublishTime = first_issue.PublishTime;
                Debug.Assert(String.IsNullOrEmpty(strFirstPublishTime) == false, "");

                if (this.MemberCells.Count == 0)
                {
                    strNewPublishTime = strFirstPublishTime + "-" + strFirstPublishTime;
                    if (this.PublishTime == strNewPublishTime)
                        return false;
                    this.PublishTime = strNewPublishTime;
                    return true;
                }

                IssueBindingItem last_issue = this.MemberCells[this.MemberCells.Count - 1].Container;
                Debug.Assert(last_issue != null, "");

                int nLastLineNo = this.Container.Container.Issues.IndexOf(last_issue);
                Debug.Assert(nLastLineNo != -1, "");

                string strLastPublishTime = last_issue.PublishTime;
                Debug.Assert(String.IsNullOrEmpty(strLastPublishTime) == false, "");

                strNewPublishTime = strFirstPublishTime + "-" + strLastPublishTime;
                if (this.PublishTime == strNewPublishTime)
                    return false;
                this.PublishTime = strNewPublishTime;
                return true;
            }
            finally
            {
                // ˢ��outofissue״̬
                int nCol = this.Container.IndexOfItem(this);
                Debug.Assert(nCol != -1, "");
                if (nCol != -1)
                    this.Container.RefreshOutofIssueValue(nCol);
            }
        }

        // ���ں϶��ᣬMemberCells�޸ĺ�Ҫˢ�º϶�������volume�ַ��������ڳ�Ա����ߵ��ᣬˢ��valume string
        public bool RefreshVolumeString()
        {
            if (this.IsParent == false)
            {
                IssueBindingItem issue = this.Container;
                if (issue != null
                    && String.IsNullOrEmpty(issue.PublishTime) == false)
                {
                    string strVolumeString = VolumeInfo.BuildItemVolumeString(
                        IssueUtil.GetYearPart(issue.PublishTime),
                        issue.Issue,
                        issue.Zong,
                        issue.Volume);
                    if (this.Volume != strVolumeString)
                    {
                        this.Volume = strVolumeString;
                        return true;
                    }
                }

                return false;
            }

            if (this.MemberCells.Count == 0)
            {
                if (this.Volume == "")
                    return false;
                this.Volume = "";
                return true;
            }

            Hashtable no_list_table = new Hashtable();
            // List<string> no_list = new List<string>();
            List<string> volumn_list = new List<string>();
            List<string> zong_list = new List<string>();

            for (int i = 0; i < this.MemberCells.Count; i++)
            {
                Cell cell = this.MemberCells[i];
                if (cell == null)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                if (cell.item == null)
                    continue;   // ����ȱ��

                IssueBindingItem issue = cell.Container;
                Debug.Assert(issue != null, "");

                string strNo = "";
                string strVolume = "";
                string strZong = "";

                if (cell.item != null
                    && String.IsNullOrEmpty(cell.item.Volume) == false)
                {
                    // ���������ںš����ںš���ŵ��ַ���
                    VolumeInfo.ParseItemVolumeString(cell.item.Volume,
                        out strNo,
                        out strZong,
                        out strVolume);
                }

                // ʵ�ڲ��У����������е�?
                if (String.IsNullOrEmpty(strNo) == true)
                {
                    strNo = issue.Issue;
                    Debug.Assert(String.IsNullOrEmpty(strNo) == false, "");

                    strVolume = issue.Volume;
                    strZong = issue.Zong;
                }

                Debug.Assert(String.IsNullOrEmpty(issue.PublishTime) == false, "");
                string strYear = IssueUtil.GetYearPart(issue.PublishTime);

                List<string> no_list = (List<string>)no_list_table[strYear];
                if (no_list == null)
                {
                    no_list = new List<string>();
                    no_list_table[strYear] = no_list;
                }

                no_list.Add(strNo);
                volumn_list.Add(strVolume);
                zong_list.Add(strZong);
            }

            List<string> keys = new List<string>();
            foreach (string key in no_list_table.Keys)
            {
                keys.Add(key);
            }
            keys.Sort();

            string strNoString = "";
            for (int i = 0; i < keys.Count; i++)
            {
                string strYear = keys[i];
                List<string> no_list = (List<string>)no_list_table[strYear];
                Debug.Assert(no_list != null);

                if (String.IsNullOrEmpty(strNoString) == false)
                    strNoString += ","; // ;
                strNoString += strYear + ",no." + Global.BuildNumberRangeString(no_list);   // :no
            }

            string strVolumnString = Global.BuildNumberRangeString(volumn_list);
            string strZongString = Global.BuildNumberRangeString(zong_list);

            string strValue = strNoString;


            if (String.IsNullOrEmpty(strZongString) == false)
            {
                if (String.IsNullOrEmpty(strValue) == false)
                    strValue += "=";
                strValue += "��." + strZongString;
            }

            if (String.IsNullOrEmpty(strVolumnString) == false)
            {
                if (String.IsNullOrEmpty(strValue) == false)
                    strValue += "=";
                strValue += "v." + strVolumnString;
            }

            if (this.Volume == strValue)
                return false;

            this.Volume = strValue;
            return true;
        }

        // MemberCells�޸ĺ�Ҫˢ�º϶���ļ۸��ַ���
        public bool RefreshPriceString()
        {
            if (this.MemberCells.Count == 0)
            {
                if (this.Price == "")
                    return false;
                this.Price = "";
                return true;
            }

            List<string> prices = new List<string>();
            for (int i = 0; i < this.MemberCells.Count; i++)
            {
                Cell cell = this.MemberCells[i];
                if (cell == null)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                if (cell.item == null)
                    continue;   // ����ȱ��

                prices.Add(cell.item.Price);
            }

            string strTotalPrice = PriceUtil.TotalPrice(prices);

            if (this.Price == strTotalPrice)
                return false;
            this.Price = strTotalPrice;
            return true;
        }

        // MemberCells�޸ĺ�Ҫˢ��binding XMLƬ��
        // ���ܻ��׳��쳣
        // return:
        //      false   binding XMLƬ��û�з����޸�
        //      true    binding XMLƬ�Ϸ������޸�
        public bool RefreshBindingXml()
        {
            if (this.MemberCells.Count == 0)
            {
                if (this.Binding == "")
                    return false;
                this.Binding = "";
                return true;
            }

            // ����<binding>Ԫ����Ƭ��
            string strInnerXml = "";
            string strError = "";
            int nRet = BuildBindingXmlString(this.MemberCells,
                out strInnerXml,
                out strError);
            if (nRet == -1)
                throw new Exception(strError);

            if (this.Binding == strInnerXml)
                return false;

            this.Binding = strInnerXml;
            return true;
        }

        /*
        // ����<binding>Ԫ����Ƭ��
        public static int BuildBindingXmlString(List<ItemBindingItem> items,
            out string strInnerXml,
            out string strError)
        {
            strInnerXml = "";
            strError = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<binding />");

            for (int i = 0; i < items.Count; i++)
            {
                ItemBindingItem item = items[i];

                XmlNode node = dom.CreateElement("item");
                dom.DocumentElement.AppendChild(node);

                DomUtil.SetAttr(node, "publishTime", item.PublishTime);
                DomUtil.SetAttr(node, "volume", item.Volume);
                DomUtil.SetAttr(node, "refID", item.RefID);
            }

            strInnerXml = dom.DocumentElement.InnerXml;

            return 0;
        }
         * */

        // ������Ϊ�϶�����<binding>Ԫ����Ƭ��
        // Ҫ��������<item>Ԫ��
        public static int BuildBindingXmlString(List<Cell> cells,
            out string strInnerXml,
            out string strError)
        {
            strInnerXml = "";
            strError = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<binding />");

            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (cell == null)
                    continue;

                XmlNode node = dom.CreateElement("item");
                dom.DocumentElement.AppendChild(node);

                ItemBindingItem item = cell.item;

                if (item != null)
                {
                    // �洢��ԭ���ǣ���Ҫ�洢��ʶ��λ��Ϣ
                    DomUtil.SetAttr(node, "publishTime", item.PublishTime);
                    DomUtil.SetAttr(node, "volume", item.Volume);
                    DomUtil.SetAttr(node, "refID", item.RefID);
                    if (String.IsNullOrEmpty(item.Barcode) == false)
                        DomUtil.SetAttr(node, "barcode", item.Barcode);
                    if (String.IsNullOrEmpty(item.RegisterNo) == false)
                        DomUtil.SetAttr(node, "registerNo", item.RegisterNo);

                    // 2011/9/8
                    if (String.IsNullOrEmpty(item.Price) == false)
                        DomUtil.SetAttr(node, "price", item.Price);
                }
                else
                {
                    DomUtil.SetAttr(node, "publishTime", cell.Container.PublishTime);

                    string strVolume = VolumeInfo.BuildItemVolumeString(
                        IssueUtil.GetYearPart(cell.Container.PublishTime),
                        cell.Container.Issue,
                        cell.Container.Zong,
                        cell.Container.Volume);
                    DomUtil.SetAttr(node, "volume", strVolume);
                    DomUtil.SetAttr(node, "refID", "");
                    DomUtil.SetAttr(node, "missing", "true");

                    // TODO: ����ͷβ������missing�����⣬�м䲿���Ƿ����ʡ��?
                }
            }

            strInnerXml = dom.DocumentElement.InnerXml;

            return 0;
        }

        // ������Ϊ��Ա���<binding>Ԫ����Ƭ��
        // ��������һ��<bindingParent>Ԫ��
        public int BuildMyselfBindingXmlString(
            out string strInnerXml,
            out string strError)
        {
            strInnerXml = "";
            strError = "";

            Debug.Assert(this.ParentItem != null, "��Ա����.ParentItem����Ϊ�ǿ�");

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<binding />");

            {
                XmlNode node = dom.CreateElement("bindingParent");
                dom.DocumentElement.AppendChild(node);

                DomUtil.SetAttr(node, "refID", this.ParentItem.RefID);
            }

            strInnerXml = dom.DocumentElement.InnerXml;
            return 0;
        }

        // ��λ��MemberCells������±�
        public int GetCellIndexOfMemberItem(ItemBindingItem item)
        {
            for (int i = 0; i < this.MemberCells.Count; i++)
            {
                Cell cell = this.MemberCells[i];
                if (cell == null)
                    continue;
                if (cell.item == item)
                    return i;
            }

            return -1;
        }

        // ���ݱ������Cell����
        public Cell ContainerCell
        {
            get
            {
                IssueBindingItem issue = this.Container;
                if (issue == null)
                    return null;

                Debug.Assert(issue != null, "");
                int index = issue.IndexOfItem(this);

                if (index == -1)
                    return null;

                Debug.Assert(index != -1, "");
                return issue.Cells[index];
            }
        }


        #region ��¼�ڵ������ֶ�

        public string GetText(string strName)
        {
            switch (strName)
            {
                case "location":
                    return this.LocationString;
                case "intact":
                    return this.Intact;
                case "state":
                    return this.State;
                case "refID":
                    return this.RefID;

                case "publishTime":
                    return this.RefID;
                case "barcode":
                    return this.Barcode;
                case "regitserNo":
                    return this.RegisterNo;
                case "source":
                    return this.Source;
                case "seller":
                    return this.Seller;
                case "accessNo":
                    return this.AccessNo;
                case "bookType":
                    return this.BookType;
                case "price":
                    return this.Price;
                case "volumn":
                    return this.Volume;
                case "comment":
                    return this.Comment;
                case "batchNo":
                    return this.BatchNo;
                case "binding":
                    return this.Binding;
                case "recpath":
                    return this.RecPath;
                case "mergeComment":
                    return this.MergeComment;
                case "borrower":
                    return this.Borrower;
                case "borrowDate":
                    return this.BorrowDate;
                case "borrowPeriod":
                    return this.BorrowPeriod;

            }

            return "��֧�ֵ�strName '" + strName + "'";
        }

        public string PublishTime
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "publishTime");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "publishTime", value);
            }
        }

        public string LocationString
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "location");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "location", value);
            }
        }

        public string Intact
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "intact");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "intact", value);
            }
        }

        public string Barcode
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "barcode");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "barcode", value);
            }
        }

        public string RegisterNo
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "registerNo");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "registerNo", value);
            }
        }

        public string RefID
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "refID");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "refID", value);
            }
        }

        public string Source
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "source");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "source", value);
            }
        }

        public string Seller
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "seller");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "seller", value);
            }
        }

        public string AccessNo
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "accessNo");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "accessNo", value);
            }
        }

        public string State
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "state");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "state", value);
            }
        }

        public string BookType
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "booktype");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "booktype", value);
            }
        }

        public string Price
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "price");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "price", value);
            }
        }

        public string Volume
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "volume");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "volume", value);
            }
        }

        public string Comment
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "comment");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "comment", value);
            }
        }

        public string BatchNo
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "batchNo");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "batchNo", value);
            }
        }

        public string Binding
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementInnerXml(this.dom.DocumentElement,
                    "binding");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementInnerXml(this.dom.DocumentElement,
                    "binding", value);
            }
        }

        public string Borrower
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "borrower");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "borrower", value);
            }
        }

        public string BorrowDate
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "borrowDate");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "borrowDate", value);
            }
        }

        public string BorrowPeriod
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "borrowPeriod");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "borrowPeriod", value);
            }
        }

        public string MergeComment
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "mergeComment");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "mergeComment", value);
            }
        }


        public string Operations
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementInnerXml(this.dom.DocumentElement,
                    "operations");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementInnerXml(this.dom.DocumentElement,
                    "operations", value);
            }
        }

        #endregion

        // ���û���ˢ��һ����������
        // ���ܻ��׳��쳣
        public void SetOperation(
            string strAction,
            string strOperator,
            string strComment)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<operations />");

            string strInnerXml = this.Operations;
            if (String.IsNullOrEmpty(strInnerXml) == false)
            {
                dom.DocumentElement.InnerXml = this.Operations;
            }

            XmlNode node = dom.DocumentElement.SelectSingleNode("operation[@name='" + strAction + "']");
            if (node == null)
            {
                node = dom.CreateElement("operation");
                dom.DocumentElement.AppendChild(node);
                DomUtil.SetAttr(node, "name", strAction);
            }

            DomUtil.SetAttr(node, "time", DateTimeUtil.Rfc1123DateTimeString(DateTime.Now.ToUniversalTime()));
            DomUtil.SetAttr(node, "operator", strOperator);
            if (String.IsNullOrEmpty(strComment) == false)
                DomUtil.SetAttr(node, "comment", strComment);

            this.Operations = dom.DocumentElement.InnerXml;
        }

    }

    // �ڶ���Σ���������
    internal class OrderBindingItem
    {
        public IssueBindingItem Container = null;

        public object Tag = null;   // ���ڴ����Ҫ���ӵ��������Ͷ���

        internal XmlDocument dom = null;

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed = false;

        string m_strXml = "";

        public string Xml
        {
            get
            {
                if (dom != null)
                    return dom.OuterXml;

                return m_strXml;
            }
        }

        // ��ʼ��
        public int Initial(string strXml,
            out string strError)
        {
            strError = "";

            this.dom = new XmlDocument();
            try
            {
                this.dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "���� XML װ�� DOM ʱ����: " + ex.Message;
                return -1;
            }

            return 0;
        }

        // ˢ��<distribute>�ַ���
        // return:
        //      false   û�з����µĸı�
        //      true    �����˸ı�
        internal bool UpdateDistributeString(GroupCell group_head)
        {
            List<Cell> members = group_head.MemberCells;
            LocationCollection locations = new LocationCollection();
            for (int i = 0; i < members.Count; i++)
            {
                Cell cell = members[i];
                Debug.Assert(cell.item != null, "");
                Location location = new Location();
                location.Name = cell.item.LocationString;
                location.RefID = cell.item.RefID;
                locations.Add(location);
            }

            string strNewValue = locations.ToString(true);
            if (this.Distribute != strNewValue)
            {
                this.Distribute = strNewValue;
                this.Changed = true;
                return true;
            }

            return false;
        }

        // ����<distribute>�е�ʵ�����ˢ��<copy>ֵ
        // parameters:
        //      bRefreshOrderCount  �Ƿ�ҲҪˢ�¶���������������Σ��ѵ���������Ҫˢ�µ�
        // return:
        //      -1  ����
        //      0   û�з����޸�
        //      1   �������޸�
        public int RefreshOrderCopy(
            bool bRefreshOrderCount,
            out string strError)
        {
            strError = "";

                string strNewValue = "";
                string strOldValue = "";
                OrderDesignControl.ParseOldNewValue(this.Copy,
                    out strOldValue,
                    out strNewValue);
                int nOldCopy = IssueBindingItem.GetNumberValue(strOldValue);
                int nNewCopy = IssueBindingItem.GetNumberValue(strNewValue);

            string strDistribute = this.Distribute;

            LocationCollection locations = new LocationCollection();
            int nRet = locations.Build(strDistribute,
                out strError);
            if (nRet == -1)
                return -1;

            bool bChanged = false;

            // ��������һ���գ���Ϊû���κ�֤�ݿ��Ը�ԭ���������жϣ�����Ҫ���ڵ����ѵ�ֵ
            int nArrivedCount = locations.GetArrivedCopy();
            if (bRefreshOrderCount == true)
            {
                if (nOldCopy < nArrivedCount)
                {
                    bChanged = true;
                    nOldCopy = nArrivedCount;
                }
            }

            if (nNewCopy != nArrivedCount)
            {
                bChanged = true;
                nNewCopy = nArrivedCount;
            }

            if (bChanged == true)
            {
                this.Copy = OrderDesignControl.LinkOldNewValue(nOldCopy.ToString(),
         nNewCopy.ToString());
                return 1;
            }
            return 0;
        }

        // ���ض�ƫ�Ƶ�locationλ�ý���ɾ�������޸Ķ�������
        // return:
        //      -2  �������ѵ�ֵ��Ȼ����ȷ����Ҫˢ��
        //      -1  ����
        //      0   ��ȷ
        public int DoDelete(int nLocationIndex,
            out string strError)
        {
            strError = "";

            string strDistribute = this.Distribute;

            LocationCollection locations = new LocationCollection();
            int nRet = locations.Build(strDistribute,
                out strError);
            if (nRet == -1)
                return -1;

            if (nLocationIndex >= locations.Count)
                return 0;   // ���ò����޸���

            Location location = locations[nLocationIndex];
            bool bArrived = String.IsNullOrEmpty(location.RefID) == false;

            locations.RemoveAt(nLocationIndex);
            this.Distribute = locations.ToString(true);

            int nArrivedCountDelta = 0;
            int nOrderCountDelta = -1;
            if (bArrived == true)
                nArrivedCountDelta = -1;

            bool bCopyValueError = false;

            // ˢ��<copy>Ԫ���еĶ������ѵ�����ֵ
            if (nOrderCountDelta != 0 || nArrivedCountDelta != 0)
            {
                string strNewValue = "";
                string strOldValue = "";
                OrderDesignControl.ParseOldNewValue(this.Copy,
                    out strOldValue,
                    out strNewValue);
                int nOldCopy = IssueBindingItem.GetNumberValue(strOldValue);
                int nNewCopy = IssueBindingItem.GetNumberValue(strNewValue);
                nOldCopy += nOrderCountDelta;
                if (nOldCopy < 0)
                {
                    bCopyValueError = true;
                    nOldCopy = 0;
                }

                Debug.Assert(nOldCopy >= 0, "");

                nNewCopy += nArrivedCountDelta;
                if (nNewCopy < 0)
                {
                    bCopyValueError = true;
                    nNewCopy = 0;
                }

                Debug.Assert(nNewCopy >= 0, "");
                this.Copy = OrderDesignControl.LinkOldNewValue(nOldCopy.ToString(),
                     nNewCopy.ToString());
            }

            this.Changed = true;
            if (bCopyValueError == true)
            {
                strError = "copyֵ�д����뼰ʱˢ��";
                return -2;
            }
            return 0;
        }

        // ���ض�ƫ�Ƶ��Ѿ��ǵ��Ĳ���г����ǵ�
        // �������Ľ���Ƕ�dom�����XML�ַ��������˸Ķ�
        // ע�⣬�������������޸Ļ��ܵ�IssueBindingItem�����У���Ҫ������ע�����
        // parameters:
        public int DoUnaccept(int nLocationIndex,
            out string strError)
        {
            strError = "";

            string strDistribute = this.Distribute;

            LocationCollection locations = new LocationCollection();
            int nRet = locations.Build(strDistribute,
                out strError);
            if (nRet == -1)
                return -1;

            if (nLocationIndex >= locations.Count)
                return 0;   // ���ò����޸���

            Location location = locations[nLocationIndex];
            location.RefID = "";
            this.Distribute = locations.ToString(true);

            // ˢ��<copy>Ԫ���е��ѵ�����ֵ
            int nArrivedCount = locations.GetArrivedCopy();
            this.Copy = IssueBindingItem.ChangeNewValue(this.Copy, nArrivedCount.ToString());

            this.Changed = true;
            return 0;
        }

        // ���ض�ƫ�Ƶ�Ԥ�����мǵ�
        // �������Ľ���Ƕ�dom�����XML�ַ��������˸Ķ�
        // ע�⣬�������������޸Ļ��ܵ�IssueBindingItem�����У���Ҫ������ע�����
        // parameters:
        //      strRefID    [in]�����ʹ����ǰ��refid [out]�������λ�õ�refid
        //      strLocation [out]�������λ�õĹݲصص���
        public int DoAccept(int nLocationIndex,
            ref string strRefID,
            out string strLocation,
            out string strError)
        {
            strError = "";
            // strRefID = "";
            strLocation = "";

            string strDistribute = this.Distribute;

            LocationCollection locations = new LocationCollection();
            int nRet = locations.Build(strDistribute,
                out strError);
            if (nRet == -1)
                return -1;

            if (nLocationIndex >= locations.Count)
            {
                /*
                strError = "nLocationIndexֵ "+nLocationIndex.ToString()+" (��0��ʼ����)����ʵ�ʾ��еĹݲصص���Ŀ���� " + locations.Count.ToString();
                return -1;
                 * */
                // �ں�������㹻�Ŀյص�����
                while (locations.Count <= nLocationIndex)
                {
                    locations.Add(new Location());
                }
                Debug.Assert(nLocationIndex < locations.Count, "");
            }

            Location location = locations[nLocationIndex];

            if (String.IsNullOrEmpty(location.RefID) == false)
            {
                strError = "�ǵ�����ǰ������λ�� "+nLocationIndex.ToString()+" �Ѿ����� refid ["+location.RefID+"]";
                return -1;
            }

            strLocation = location.Name;
            if (string.IsNullOrEmpty(strRefID) == true)
                strRefID = Guid.NewGuid().ToString();
            location.RefID = strRefID;

            strDistribute = locations.ToString(true);
            this.Distribute = strDistribute;

            // ˢ��<copy>Ԫ���е��ѵ�����ֵ
            int nArrivedCount = locations.GetArrivedCopy();
            this.Copy = IssueBindingItem.ChangeNewValue(this.Copy, nArrivedCount.ToString());

            this.Changed = true;
            return 0;
        }

        #region ���ݳ�Ա

        public string GetText(string strName)
        {
            switch (strName)
            {
                case "state":
                    return this.State;
                case "range":
                    return this.Range;
                case "issueCount":
                    return this.IssueCount;
                case "orderTime":
                    return this.OrderTime;

                case "orderID":
                    return this.OrderID;
                case "comment":
                    return this.Comment;
                case "batchNo":
                    return this.BatchNo;
                case "source":
                    return IssueBindingItem.GetNewOrOldValue(this.Source);
                case "seller":
                    return this.Seller;
                case "catalogNo":
                    return this.CatalogNo;
                case "copy":
                    return IssueBindingItem.GetNewOrOldValue(this.Copy);
                case "price":
                    {
                        string strPrice = IssueBindingItem.GetNewOrOldValue(this.Price);
                        if (string.IsNullOrEmpty(strPrice) == false)
                            return strPrice;
                        else
                        {
                            // 2015/4/1
                            return IssueBindingItem.CalcuPrice(this.TotalPrice, this.IssueCount, IssueBindingItem.GetOldOrNewValue(this.Copy));
                        }
                        // return IssueBindingItem.GetNewOrOldValue(this.Price);
                    }
                case "distribute":
                    return this.Distribute;
                case "class":
                    return this.Class;
                case "totalPrice":
                    return this.TotalPrice;
                case "sellerAddress":
                    return this.SellerAddress;
            }

            return "��֧�ֵ�strName '" + strName + "'";
        }

        public string State
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "state");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "state", value);
            }
        }

        public string Range
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "range");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "range", value);
            }
        }

        public string IssueCount
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "issueCount");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "issueCount", value);
            }
        }

        public string OrderTime
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "orderTime");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "orderTime", value);
            }
        }

        public string OrderID
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "orderID");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "orderID", value);
            }
        }

        public string Comment
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "comment");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "comment", value);
            }
        }

        public string BatchNo
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "batchNo");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "batchNo", value);
            }
        }

        public string CatalogNo
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "catalogNo");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "catalogNo", value);
            }
        }

        public string Seller
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "seller");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "seller", value);
            }
        }

        public string Source
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "source");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "source", value);
            }
        }

        public string Copy
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "copy");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "copy", value);
            }
        }




        public string Price
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "price");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "price", value);
            }
        }

        public string Distribute
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "distribute");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "distribute", value);
            }
        }

        public string Class
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "class");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "class", value);
            }
        }

        public string TotalPrice
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementText(this.dom.DocumentElement,
                    "totalPrice");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementText(this.dom.DocumentElement,
                    "totalPrice", value);
            }
        }

        public string SellerAddress
        {
            get
            {
                if (this.dom == null)
                    return "";

                return DomUtil.GetElementInnerXml(this.dom.DocumentElement,
                    "sellerAddress");
            }
            set
            {
                if (this.dom == null)
                    throw new Exception("this.dom��δ��ʼ��");

                DomUtil.SetElementInnerXml(this.dom.DocumentElement,
                    "sellerAddress", value);
            }
        }



        #endregion


    }

    // �Ƚϳ������ڡ�С����ǰ
    internal class IssuePublishTimeComparer : IComparer<IssueBindingItem>
    {

        int IComparer<IssueBindingItem>.Compare(IssueBindingItem x, IssueBindingItem y)
        {
            string s1 = x.PublishTime;
            string s2 = y.PublishTime;

            int nRet = String.Compare(s1, s2);
            if (nRet == 0)
            {
                // �������������ͬ������������ǰ��
                if (x.Virtual == false && y.Virtual == false)
                    return 0;
                if (x.Virtual == true)
                    return -1;
                return 1;
            }

            return nRet;
        }
    }

    // ���еĲ���ģʽ
    internal enum IssueLayoutState
    {
        Binding = 1,    // װ��
        Accepting = 2,  // �ǵ�
    }

    // �Ƚ�����ʡ������ǰ
    internal class ItemIntactComparer : IComparer<ItemBindingItem>
    {

        int IComparer<ItemBindingItem>.Compare(ItemBindingItem x, ItemBindingItem y)
        {
            string s1 = x.Intact;
            string s2 = y.Intact;

            float v1 = 0;
            if (String.IsNullOrEmpty(s1) == true)
                v1 = 100;
            else
            {
                try
                {
                    v1 = (float)Convert.ToDecimal(s1);
                }
                catch
                {
                    v1 = 0;
                }
            }

            float v2 = 0;
            if (String.IsNullOrEmpty(s2) == true)
                v2 = 100;
            else
            {
                try
                {
                    v2 = (float)Convert.ToDecimal(s2);
                }
                catch
                {
                    v2 = 0;
                }
            }

            if (v1 - v2 > 0)
                return -1;
            if (v1 - v2 < 0)
                return 1;
            return 0;
        }
    }

}
