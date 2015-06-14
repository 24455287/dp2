#define NEW_DUP_API

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using DigitalPlatform;
using DigitalPlatform.IO;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.CommonControl;

using DigitalPlatform.CirculationClient;
using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.Text;

namespace dp2Circulation
{
    // public partial class OrderControl : UserControl
    /// <summary>
    /// ������¼�б�ؼ�
    /// </summary>
    public partial class OrderControl : OrderControlBase
    {
#if NO
        // ����������к�����
        SortColumns SortColumns = new SortColumns();
#endif

        /// <summary>
        /// �Ƿ�Ϊ�ڿ�����ģʽ��
        /// </summary>
        public bool SeriesMode = false; // �Ƿ�Ϊ�ڿ�����ģʽ��

        /*
        // (һ�����ղ�����)��ǰ�Ѿ�������BookItem
        public List<BookItem> AcceptedBookItems = new List<BookItem>();
         * */

        /// <summary>
        /// Ŀ���¼·��
        /// </summary>
        public string TargetRecPath = "";   // 4��״̬��1)�����·���͵�ǰ��¼·��һ�£�����ʵ���¼�ʹ����ڵ�ǰ��¼�£�2)�����·���͵�ǰ��¼·����һ�£��ּ�¼�Ѿ����ڣ���Ҫ�������洴��ʵ���¼��3) �����·�����п������֣���ʾ�ּ�¼�����ڣ���Ҫ���ݵ�ǰ��¼��MARC��������4) �����·��Ϊ�գ���ʾ��Ҫͨ���˵�ѡ��Ŀ��⣬Ȼ������ͬ3)
        /// <summary>
        /// �������κ�
        /// </summary>
        public string AcceptBatchNo = "";   // �������κ�
        /// <summary>
        /// �Ƿ�Ҫ�����ղ���ĩ���Զ������������������ŵĽ���?
        /// </summary>
        public bool InputItemsBarcode = true;   // �Ƿ�Ҫ�����ղ���ĩ���Զ������������������ŵĽ���?
        /// <summary>
        /// �Ƿ�Ϊ�´����Ĳ��¼���á��ӹ��С�״̬
        /// </summary>
        public bool SetProcessingState = true;   // �Ƿ�Ϊ�´����Ĳ��¼���á��ӹ��С�״̬ 2009/10/19
        /// <summary>
        /// �Ƿ�Ϊ�´����Ĳ��¼������ȡ��
        /// </summary>
        public bool CreateCallNumber = true;   // �Ƿ�Ϊ�´����Ĳ��¼������ȡ�� 2012/5/7

        // 2010/12/5
        /// <summary>
        /// Ϊ���¼�еļ۸��ֶ����ú��ּ۸�ֵ��ֵΪ ��Ŀ��/������/���ռ�/�հ� ֮һ
        /// </summary>
        public string PriceDefault = "���ռ�";  // Ϊ���¼�еļ۸��ֶ����ú��ּ۸�ֵ����Ŀ��/������/���ռ�/�հ�

        // 
        /// <summary>
        /// ������Ŀ���¼(�Ա����������)
        /// </summary>
        public event OpenTargetRecordEventHandler OpenTargetRecord = null;

        /// <summary>
        /// ����ָ���������κŵ�ʵ����
        /// </summary>
        public event HilightTargetItemsEventHandler HilightTargetItem = null;

        /// <summary>
        /// ׼������
        /// </summary>
        public event PrepareAcceptEventHandler PrepareAccept = null;

        /// <summary>
        /// ����Ŀ���¼·��
        /// </summary>
        public event SetTargetRecPathEventHandler SetTargetRecPath = null;

        // 2012/10/4
        /// <summary>
        /// ���ݴ����Ƿ��ڹ�Ͻ��Χ��
        /// </summary>
        public event VerifyLibraryCodeEventHandler VerifyLibraryCode = null;

#if NO
        /// <summary>
        /// ������� / ��ֹ״̬�����ı�
        /// </summary>
        public event EnableControlsHandler EnableControlsEvent = null;


        public event LoadRecordHandler LoadRecord = null;



        public bool m_bRemoveDeletedItem = false;   // ��ɾ������ʱ, �Ƿ���Ӿ���Ĩ����Щ����(ʵ�����ڴ����滹�����м����ύ������)?

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

        /// <summary>
        /// ��ú��ֵ
        /// </summary>
        public event GetMacroValueHandler GetMacroValue = null;

        /// <summary>
        /// ���ݷ����ı�
        /// </summary>
        public event ContentChangedEventHandler ContentChanged = null;
        string m_strBiblioRecPath = "";

        public OrderItemCollection Items = null;

#endif


        /// <summary>
        /// ����ʵ������
        /// </summary>
        public event GenerateEntityEventHandler GenerateEntity = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        public OrderControl()
        {
            InitializeComponent();

            this.m_listView = this.listView;
            this.ItemType = "order";
            this.ItemTypeName = "����";
        }

#if NO
        public int OrderCount
        {
            get
            {
                if (this.Items != null)
                    return this.Items.Count;

                return 0;
            }
        }

        // ��listview�еĶ��������޸�Ϊnew״̬
        public void ChangeAllItemToNewState()
        {
            foreach (OrderItem orderitem in this.Items)
            {
                // OrderItem orderitem = this.OrderItems[i];

                if (orderitem.ItemDisplayState == ItemDisplayState.Normal
                    || orderitem.ItemDisplayState == ItemDisplayState.Changed
                    || orderitem.ItemDisplayState == ItemDisplayState.Deleted)   // ע��δ�ύ��deletedҲ��Ϊnew��
                {
                    orderitem.ItemDisplayState = ItemDisplayState.New;
                    orderitem.RefreshListView();
                    orderitem.Changed = true;    // ��һ�������ʹ�ܺ���������رմ��ڣ��Ƿ�ᾯ��(ʵ���޸�)���ݶ�ʧ
                }
            }
        }

        public string BiblioRecPath
        {
            get
            {
                return this.m_strBiblioRecPath;
            }
            set
            {
                this.m_strBiblioRecPath = value;

                if (this.Items != null)
                {
                    string strID = Global.GetRecordID(value);
                    this.Items.SetParentID(strID);
                }

            }
        }

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                if (this.Items == null)
                    return false;

                return this.Items.Changed;
            }
            set
            {
                if (this.Items != null)
                    this.Items.Changed = value;
            }
        }

        // ���listview�е�ȫ������
        public void Clear()
        {
            this.ListView.Items.Clear();

            // 2009/2/10
            this.SortColumns.Clear();
            SortColumns.ClearColumnSortDisplay(this.ListView.Columns);

            // 2012/7/24
            this.TargetRecPath = "";
        }

        // ��������й���Ϣ
        public void ClearOrders()
        {
            this.Clear();
            this.Items = new OrderItemCollection();
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }

        public int CountOfVisibleOrderItems()
        {
            return this.ListView.Items.Count;
        }

        public int IndexOfVisibleOrderItems(OrderItem orderitem)
        {
            for (int i = 0; i < this.ListView.Items.Count; i++)
            {
                OrderItem cur = (OrderItem)this.ListView.Items[i].Tag;

                if (cur == orderitem)
                    return i;
            }

            return -1;
        }

        public OrderItem GetAtVisibleOrderItems(int nIndex)
        {
            return (OrderItem)this.ListView.Items[nIndex].Tag;
        }

#endif

        // 
        // return:
        //      -1  ����
        //      0   û��װ��
        //      1   �Ѿ�װ��
        /// <summary>
        /// ���һ����Ŀ��¼������ȫ��������¼·��
        /// </summary>
        /// <param name="stop">Stop����</param>
        /// <param name="channel">ͨѶͨ��</param>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="recpaths">���ؼ�¼·���ַ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1 ����</para>
        /// <para>0 û��װ��</para>
        /// <para>1 �Ѿ�װ��</para>
        /// </returns>
        public static int GetOrderRecPaths(
            Stop stop,
            LibraryChannel channel,
            string strBiblioRecPath,
            out List<string> recpaths,
            out string strError)
        {
            strError = "";
            recpaths = new List<string>();

            long lPerCount = 100; // ÿ����ö��ٸ�
            long lStart = 0;
            long lResultCount = 0;
            long lCount = -1;
            for (; ; )
            {
                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }
                }
                EntityInfo[] entities = null;

                /*
                if (lCount > 0)
                    stop.SetMessage("����װ�����Ϣ " + lStart.ToString() + "-" + (lStart + lCount - 1).ToString() + " ...");
                 * */

                long lRet = channel.GetOrders(
                    stop,
                    strBiblioRecPath,
                    lStart,
                    lCount,
                    "onlygetpath",
                    "zh",
                    out entities,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                lResultCount = lRet;

                if (lRet == 0)
                    return 0;

                Debug.Assert(entities != null, "");


                for (int i = 0; i < entities.Length; i++)
                {
                    if (entities[i].ErrorCode != ErrorCodeValue.NoError)
                    {
                        strError = "·��Ϊ '" + entities[i].OldRecPath + "' �Ķ�����¼װ���з�������: " + entities[i].ErrorInfo;  // NewRecPath
                        return -1;
                    }

                    recpaths.Add(entities[i].OldRecPath);
                }

                lStart += entities.Length;
                if (lStart >= lResultCount)
                    break;

                if (lCount == -1)
                    lCount = lPerCount;

                if (lStart + lCount > lResultCount)
                    lCount = lResultCount - lStart;
            }

            return 1;
        ERROR1:
            return -1;
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

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("����װ�붩����Ϣ ...");
            Stop.BeginLoop();

            this.Update();
            // this.MainForm.Update();

            try
            {
                // string strHtml = "";
                long lStart = 0;
                long lResultCount = 0;
                long lCount = -1;
                this.ClearOrders();

                // 2012/5/9 ��дΪѭ����ʽ
                for (; ; )
                {
                    EntityInfo[] orders = null;

                    long lRet = Channel.GetOrders(
                        Stop,
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

                    this.ListView.BeginUpdate();
                    try
                    {
                        for (int i = 0; i < orders.Length; i++)
                        {
                            if (orders[i].ErrorCode != ErrorCodeValue.NoError)
                            {
                                strError = "·��Ϊ '" + orders[i].OldRecPath + "' �Ķ�����¼װ���з�������: " + orders[i].ErrorInfo;  // NewRecPath
                                return -1;
                            }

                            // ����һ������xml��¼��ȡ���й���Ϣ����listview��
                            OrderItem orderitem = new OrderItem();

                            int nRet = orderitem.SetData(orders[i].OldRecPath, // NewRecPath
                                     orders[i].OldRecord,
                                     orders[i].OldTimestamp,
                                     out strError);
                            if (nRet == -1)
                                return -1;

                            if (orders[i].ErrorCode == ErrorCodeValue.NoError)
                                orderitem.Error = null;
                            else
                                orderitem.Error = orders[i];

                            this.Items.Add(orderitem);

                            orderitem.AddToListView(this.ListView);
                        }
                    }
                    finally
                    {
                        this.ListView.EndUpdate();
                    }

                    lStart += orders.Length;
                    if (lStart >= lResultCount)
                        break;
                }
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }

#endif

        void designOrder_GetValueTable(object sender, GetValueTableEventArgs e)
        {
            string strError = "";
            string[] values = null;
            int nRet = MainForm.GetValueTable(e.TableName,
                e.DbName,
                out values,
                out strError);
            if (nRet == -1)
                MessageBox.Show(ForegroundWindow.Instance, strError);
            e.values = values;
        }

        // �滮�����������
        void DoDesignOrder()
        {
            string strError = "";
            int nRet = 0;

            // 
            if (this.Items == null)
                this.Items = new OrderItemCollection();

            Debug.Assert(this.Items != null, "");

            OrderDesignForm dlg = new OrderDesignForm();
            dlg.MainForm = this.MainForm;
            dlg.SeriesMode = this.SeriesMode;   // 2008/12/24
            dlg.BiblioDbName = Global.GetDbName(this.BiblioRecPath);    // 2009/2/15
            dlg.CheckDupItem = true;

            // TODO: ��ȱʡ�������л�����κ�? ֻ��ֱ����ȱʡ���������޸�?
            // dlg.Text = "���� -- ���κ�:" + this.OrderBatchNo;
            dlg.ClearAllItems();

            // bool bCleared = false;  // �Ƿ�������Ի�������Ĳ�������?

            // �����еĶ�����Ϣ��ӳ���Ի����С�
            // �Ѿ������Ķ�����������޸ġ���������������޸�
            foreach (OrderItem item in this.Items)
            {
                // OrderItem item = this.OrderItems[i];

                if (item.ItemDisplayState == ItemDisplayState.Deleted)
                {
                    strError = "��ǰ���ڱ��ɾ���Ķ�������������ύ����󣬲���ʹ�ö����滮����";
                    goto ERROR1;
                }


                string strOrderXml = "";
                nRet = item.BuildRecord(
                    true,   // Ҫ��� Parent ��Ա
                    out strOrderXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                /*
                if (bCleared == false)
                {
                    dlg.ClearAllItems();
                    bCleared = true;
                }
                 * */

                DigitalPlatform.CommonControl.Item design_item = 
                    dlg.AppendNewItem(strOrderXml, out strError);
                if (design_item == null)
                    goto ERROR1;

                design_item.Tag = (object)item; // �������ӹ�ϵ
            }

            dlg.Changed = false;

            dlg.GetValueTable -= new GetValueTableEventHandler(designOrder_GetValueTable);
            dlg.GetValueTable += new GetValueTableEventHandler(designOrder_GetValueTable);
            dlg.GetDefaultRecord -= new DigitalPlatform.CommonControl.GetDefaultRecordEventHandler(dlg_GetDefaultRecord);
            dlg.GetDefaultRecord += new DigitalPlatform.CommonControl.GetDefaultRecordEventHandler(dlg_GetDefaultRecord);
            dlg.VerifyLibraryCode += new VerifyLibraryCodeEventHandler(dlg_VerifyLibraryCode);
            dlg.VerifyLibraryCode += new VerifyLibraryCodeEventHandler(dlg_VerifyLibraryCode);

            MainForm.AppInfo.LinkFormState(dlg,
                "order_design_form_state");

            dlg.FocusedTime = DateTime.Now;

            dlg.ShowDialog(this);

            MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult != DialogResult.OK)
                return;


            bool bOldChanged = this.Items.Changed;

            // TODO: û�б���ԭ�Ķ���Ҫ���Ϊ���ɾ��
            List<OrderItem> save_orderitems = new List<OrderItem>();
            foreach (OrderItem item in this.Items)
            {
                save_orderitems.Add(item);
            }

            // ����������ڵ�����Ԫ��
            this.Items.Clear();

            for (int i = 0; i < dlg.Items.Count; i++)
            {
                DigitalPlatform.CommonControl.Item design_item = dlg.Items[i];

                if ((design_item.State & ItemState.ReadOnly) != 0)
                {
                    // ��ԭ
                    OrderItem order_item = (OrderItem)design_item.Tag;
                    Debug.Assert(order_item != null, "");
                    this.Items.Add(order_item);
                    order_item.AddToListView(this.listView);

                    save_orderitems.Remove(order_item);
                    continue;
                }

                OrderItem orderitem = new OrderItem();

                // ��ԭĳЩ�ֶ�
                nRet = RestoreOtherFields(design_item.OtherXml,
                    orderitem,
                    out strError);
                if (nRet == -1)
                {
                    strError = "RestoreOtherFields()��������: " + strError;
                    goto ERROR1;
                }

                bool bNew = false;
                // ����ȫ�´�������
                if (design_item.Tag == null)
                {
                    // ��ʹ������׷�ӱ���
                    orderitem.RecPath = "";
                    orderitem.ItemDisplayState = ItemDisplayState.New;
                    bNew = true;
                }
                else
                {
                    // ��ԭrecpath
                    OrderItem order_item = (OrderItem)design_item.Tag;

                    /*
                    // ��ԭһЩ��Ҫ��ֵ
                    orderitem.RecPath = order_item.RecPath;
                    orderitem.RefID = order_item.RefID;
                    orderitem.Timestamp = order_item.Timestamp;
                    orderitem.OldRecord = order_item.OldRecord;

                    // 2009/1/6 changed
                    orderitem.ItemDisplayState = order_item.ItemDisplayState;
                    */
                    orderitem = order_item;

                    save_orderitems.Remove(order_item);
                }

                bool bChanged = false;

                if (orderitem.Parent != Global.GetRecordID(this.BiblioRecPath))
                {
                    orderitem.Parent = Global.GetRecordID(this.BiblioRecPath);
                    bChanged = true;
                }

                if (orderitem.CatalogNo != design_item.CatalogNo)
                {
                    orderitem.CatalogNo = design_item.CatalogNo;
                    bChanged = true;
                }

                if (orderitem.Seller != design_item.Seller)
                {
                    orderitem.Seller = design_item.Seller;
                    bChanged = true;
                }

                if (orderitem.Source != design_item.Source)
                {
                    orderitem.Source = design_item.Source;  // ֻȡ����ֵ
                    bChanged = true;
                }
                if (orderitem.Range != design_item.RangeString)
                {
                    orderitem.Range = design_item.RangeString;
                    bChanged = true;
                }

                if (orderitem.IssueCount != design_item.IssueCountString)
                {
                    orderitem.IssueCount = design_item.IssueCountString;
                    bChanged = true;
                }
                if (orderitem.Copy != design_item.CopyString)
                {
                    orderitem.Copy = design_item.CopyString;    // ֻȡ����ֵ
                    bChanged = true;
                }
                if (orderitem.Price != design_item.Price)
                {
                    orderitem.Price = design_item.Price;   // ֻȡ����ֵ
                    bChanged = true;
                }

                if (orderitem.Distribute != design_item.Distribute)
                {
                    orderitem.Distribute = design_item.Distribute;
                    bChanged = true;
                }
                if (orderitem.Class != design_item.Class)
                {
                    orderitem.Class = design_item.Class;
                    bChanged = true;
                }
                // 2009/2/13
                string strAddressXml = design_item.SellerAddressXml;
                if (String.IsNullOrEmpty(strAddressXml) == false)
                {
                    try
                    {
                        XmlDocument address_dom = new XmlDocument();
                        address_dom.LoadXml(strAddressXml);

                        if (orderitem.SellerAddress != address_dom.DocumentElement.InnerXml)
                        {
                            orderitem.SellerAddress = address_dom.DocumentElement.InnerXml;
                            bChanged = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        strError = "����SellerAddressʱ��������: " + ex.Message;
                        goto ERROR1;
                    }
                }

                // 2009/11/9
                try
                {
                    if (orderitem.TotalPrice != design_item.TotalPrice)
                    {
                        orderitem.TotalPrice = design_item.TotalPrice;
                        bChanged = true;
                    }
                }
                catch (Exception ex)
                {
                    strError = "����TotalPriceʱ��������: " + ex.Message;
                    goto ERROR1;
                }

                if (bNew == false && bChanged == true)
                {
                    if (orderitem.ItemDisplayState != ItemDisplayState.New)
                    {
                        // ע: ״̬ΪNew�Ĳ����޸�ΪChanged������һ������
                        orderitem.ItemDisplayState = ItemDisplayState.Changed;
                    }
                }

                // �ȼ����б�
                this.Items.Add(orderitem);

                orderitem.AddToListView(this.listView);
                orderitem.HilightListViewItem(true);

                if (bChanged == true)
                    orderitem.Changed = true;    // ��Ϊ�����������������ζ����޸Ĺ����������Ա��⼯����ֻ��һ�����������ʱ�򣬼��ϵ�changedֵ����
            }

            // ���ɾ��ĳЩԪ��
            // 2008/12/24
            for (int i = 0; i < save_orderitems.Count; i++)
            {
                OrderItem order_item = save_orderitems[i];

                // �ȼ����б�
                this.Items.Add(order_item);
                order_item.AddToListView(this.listView);

                nRet = MaskDeleteItem(order_item,
                        m_bRemoveDeletedItem);
            }

#if NO
            // �ı䱣�水ť״̬
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = true;
                this.ContentChanged(this, e1);
            }
#endif 
            TriggerContentChanged(bOldChanged, true);

            return;

        ERROR1:
            MessageBox.Show(ForegroundWindow.Instance, strError);
            return;
        }

        void dlg_VerifyLibraryCode(object sender, VerifyLibraryCodeEventArgs e)
        {
            if (this.VerifyLibraryCode != null)
                this.VerifyLibraryCode(sender, e);
        }

        // ���ȱʡ��¼
        void dlg_GetDefaultRecord(object sender, DigitalPlatform.CommonControl.GetDefaultRecordEventArgs e)
        {
            string strError = "";

            string strNewDefault = this.MainForm.AppInfo.GetString(
                "entityform_optiondlg",
                "order_normalRegister_default",
                "<root />");

            // �ַ���strNewDefault������һ��XML��¼�������൱��һ����¼��ԭò��
            // ���ǲ����ֶε�ֵ����Ϊ"@"��������ʾ����һ�������
            // ��Ҫ����Щ����ֺ�����ʽ���ؼ�
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strNewDefault);
            }
            catch (Exception ex)
            {
                strError = "XML��¼װ��DOMʱ����: " + ex.Message;
                goto ERROR1;
            }

            // ��������һ��Ԫ�ص�����
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("*");
            for (int i = 0; i < nodes.Count; i++)
            {
                string strText = nodes[i].InnerText;
                if (strText.Length > 0 && strText[0] == '@')
                {
                    // ���ֺ�
                    nodes[i].InnerText = DoGetMacroValue(strText);
                }
            }

            DomUtil.SetElementText(dom.DocumentElement,
                "parent", "");

            // ���һЩ�ֶΣ���������ȱʡֵ
            DomUtil.SetElementText(dom.DocumentElement,
                "orderID", "");
            DomUtil.SetElementText(dom.DocumentElement,
                "state", "");


            strNewDefault = dom.OuterXml;

            e.Xml = strNewDefault;

            return;
        ERROR1:
            throw new Exception(strError);
        }

        // ����XML��¼�ָ�һЩ����Ҫ�������ֶ�ֵ
        int RestoreOtherFields(string strXml,
            OrderItem item,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(strXml) == true)
                return 0;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XML��¼װ��DOMʱ����: " + ex.Message;
                return -1;
            }

            // ��ȡ��������ݣ������ı���ʾ
            item.Index = DomUtil.GetElementText(dom.DocumentElement,
                "index");
            item.State = DomUtil.GetElementText(dom.DocumentElement,
                "state");
            item.Range = DomUtil.GetElementText(dom.DocumentElement,
                "range");
            item.IssueCount = DomUtil.GetElementText(dom.DocumentElement,
                "issueCount");
            item.OrderTime = DomUtil.GetElementText(dom.DocumentElement,
                "orderTime");
            item.OrderID = DomUtil.GetElementText(dom.DocumentElement,
                "orderID");
            item.Comment = DomUtil.GetElementText(dom.DocumentElement,
                "comment");
            item.BatchNo = DomUtil.GetElementText(dom.DocumentElement,
                "batchNo");
            // 2014/2/24
            item.RefID = DomUtil.GetElementText(dom.DocumentElement,
                "refID");
            return 0;
        }

        // ���ݳ���ʱ�䣬ƥ�䡰ʱ�䷶Χ�����ϵĶ�����¼
        // 2008/12/24
        // parameters:
        //      strPublishTime  ����ʱ�䣬8�ַ������Ϊ"*"����ʾͳ���������ʱ�����
        internal int GetOrderInfoByPublishTime(string strPublishTime,
            string strLibraryCodeList,
            out List<string> XmlRecords,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            XmlRecords = new List<string>();

            if (this.Items == null)
                return 0;

            foreach (OrderItem item in this.Items)
            {
                // OrderItem item = this.OrderItems[i];

                if (item.ItemDisplayState == ItemDisplayState.Deleted)
                    continue;

                // �Ǻű�ʾͨ��
                if (strPublishTime != "*")
                {
                    try
                    {
                        if (Global.InRange(strPublishTime, item.Range) == false)
                            continue;
                    }
                    catch (Exception ex)
                    {
                        strError = ex.Message;
                        return -1;
                    }
                }

                // 2012/9/19
                // �۲�һ���ݲط����ַ����������Ƿ����ٲ����ڵ�ǰ�û���Ͻ��Χ��
                // return:
                //      -1  ����
                //      0   û���κβ����ڹ�Ͻ��Χ
                //      1   ���ٲ����ڹ�Ͻ��Χ��
                nRet = Global.DistributeCross(item.Distribute,
                    strLibraryCodeList,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                    continue;

                string strOrderXml = "";
                nRet = item.BuildRecord(
                    true,   // Ҫ��� Parent ��Ա
                    out strOrderXml,
                    out strError);
                if (nRet == -1)
                    return -1;

                XmlRecords.Add(strOrderXml);
            }

            return 1;
        }

        // ��������
        void DoAccept()
        {
            string strError = "";
            int nRet = 0;

            // this.AcceptedBookItems.Clear();
            string strBiblioSourceRecord = "";
            string strBiblioSourceSyntax = "";
            if (this.PrepareAccept != null)
            {
                PrepareAcceptEventArgs e = new PrepareAcceptEventArgs();
                e.SourceRecPath = this.BiblioRecPath;
                this.PrepareAccept(this, e);
                if (String.IsNullOrEmpty(e.ErrorInfo) == false)
                {
                    strError = e.ErrorInfo;
                    goto ERROR1;
                }

                if (e.Cancel == true)
                    return;

                this.TargetRecPath = e.TargetRecPath;
                this.AcceptBatchNo = e.AcceptBatchNo;
                this.InputItemsBarcode = e.InputItemsBarcode;
                this.SetProcessingState = e.SetProcessingState;
                this.CreateCallNumber = e.CreateCallNumber;

                this.PriceDefault = e.PriceDefault;

                strBiblioSourceRecord = e.BiblioSourceRecord;
                strBiblioSourceSyntax = e.BiblioSourceSyntax;

                if (String.IsNullOrEmpty(e.WarningInfo) == false)
                {
                    DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
                        "����: \r\n" + e.WarningInfo + "\r\n\r\n������������?",
                            "OrderControl",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return;
                }
            }

            // 
            if (this.Items == null)
                this.Items = new OrderItemCollection();

            Debug.Assert(this.Items != null, "");

            OrderArriveForm dlg = new OrderArriveForm();
            dlg.MainForm = this.MainForm;
            dlg.BiblioDbName = Global.GetDbName(this.BiblioRecPath);    // 2009/2/15
            dlg.Text = "���� -- ���κ�:"+this.AcceptBatchNo+" -- Դ:" + this.BiblioRecPath + ", Ŀ��:" + this.TargetRecPath;
            dlg.TargetRecPath = this.TargetRecPath;
            dlg.ClearAllItems();

            // bool bCleared = false;  // �Ƿ�������Ի�������Ĳ�������?

            // �����еĶ�����Ϣ��ӳ���Ի����С�
            foreach (OrderItem item in this.Items)
            {
                // OrderItem item = this.OrderItems[i];

                if (item.ItemDisplayState == ItemDisplayState.Deleted)
                {
                    strError = "��ǰ���ڱ��ɾ���Ķ�������������ύ����󣬲���ʹ�ö����滮����";
                    goto ERROR1;
                }

                string strOrderXml = "";
                nRet = item.BuildRecord(
                    true,   // Ҫ��� Parent ��Ա
                    out strOrderXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                /*
                if (bCleared == false)
                {
                    dlg.ClearAllItems();
                    bCleared = true;
                }*/

                DigitalPlatform.CommonControl.Item design_item =
                    dlg.AppendNewItem(strOrderXml, out strError);
                if (design_item == null)
                    goto ERROR1;

                design_item.Tag = (object)item; // �������ӹ�ϵ
            }

            dlg.Changed = false;

            dlg.GetValueTable -= new GetValueTableEventHandler(designOrder_GetValueTable);
            dlg.GetValueTable += new GetValueTableEventHandler(designOrder_GetValueTable);

            MainForm.AppInfo.LinkFormState(dlg,
                "order_accept_design_form_state");

            dlg.ShowDialog(this);

            MainForm.AppInfo.UnlinkFormState(dlg);


            if (dlg.DialogResult != DialogResult.OK)
                return;


            bool bOldChanged = this.Items.Changed;

            // ���漯���ڵ�����Ԫ��
            OrderItemCollection save_items = new OrderItemCollection();
            save_items.AddRange(this.Items);

            // ��������ڵ�����Ԫ��
            this.Items.Clear();

            List<OrderItem> changed_orderitems = new List<OrderItem>();

            for (int i = 0; i < dlg.Items.Count; i++)
            {
                DigitalPlatform.CommonControl.Item design_item = dlg.Items[i];

                if ((design_item.State & ItemState.ReadOnly) != 0)
                {
                    // ��ԭ
                    OrderItem order_item = (OrderItem)design_item.Tag;
                    Debug.Assert(order_item != null, "");
                    this.Items.Add(order_item);
                    order_item.AddToListView(this.listView);
                    continue;
                }

                OrderItem orderitem = new OrderItem();

                // ��ԭĳЩ�ֶ�
                nRet = RestoreOtherFields(design_item.OtherXml,
                    orderitem,
                    out strError);
                if (nRet == -1)
                {
                    strError = "RestoreOtherFields()��������: " + strError;
                    goto ERROR1;
                }

                // ����ȫ�´�������
                if (design_item.Tag == null)
                {
                    // ��ʹ������׷�ӱ���
                    orderitem.RecPath = "";

                    orderitem.ItemDisplayState = ItemDisplayState.New;
                }
                else
                {
                    // ��ԭrecpath
                    OrderItem order_item = (OrderItem)design_item.Tag;

                    // ��ԭһЩ��Ҫ��ֵ
                    orderitem.RecPath = order_item.RecPath;
                    orderitem.Timestamp = order_item.Timestamp;
                    orderitem.OldRecord = order_item.OldRecord;

                    // 2009/1/6 changed
                    orderitem.ItemDisplayState = order_item.ItemDisplayState;

                    if (orderitem.ItemDisplayState != ItemDisplayState.New)
                    {
                        // ע: ״̬ΪNew�Ĳ����޸�ΪChanged������һ������
                        orderitem.ItemDisplayState = ItemDisplayState.Changed;
                    }
                }

                orderitem.Parent = Global.GetRecordID(this.BiblioRecPath);

                orderitem.CatalogNo = design_item.CatalogNo;    // 2008/8/31
                orderitem.Seller = design_item.Seller;

                orderitem.Source = OrderDesignControl.LinkOldNewValue(design_item.OldSource, design_item.Source);

                orderitem.Range = design_item.RangeString;  // 2008/12/17
                orderitem.IssueCount = design_item.IssueCountString;    // 2008/12/17

                orderitem.Copy = OrderDesignControl.LinkOldNewValue(design_item.OldCopyString, design_item.CopyString);

                orderitem.Price = OrderDesignControl.LinkOldNewValue(design_item.OldPrice, design_item.Price);

                orderitem.Distribute = design_item.Distribute;
                orderitem.Class = design_item.Class;    // 2008/8/31

                // 2009/2/13
                string strAddressXml = design_item.SellerAddressXml;
                if (String.IsNullOrEmpty(strAddressXml) == false)
                {
                    try
                    {
                        XmlDocument address_dom = new XmlDocument();
                        address_dom.LoadXml(strAddressXml);
                        orderitem.SellerAddress = address_dom.DocumentElement.InnerXml;
                    }
                    catch (Exception ex)
                    {
                        strError = "����SellerAddressʱ��������: " + ex.Message;
                        goto ERROR1;
                    }
                }

                // orderitem.State ��Ҫ���޸�Ϊ �������ա�
                // 2008/10/22
                if (design_item.NewlyAcceptedCount > 0)
                    orderitem.State = "������";

                changed_orderitems.Add(orderitem);

                /*
                if (this.GenerateEntity != null)
                {
                    // �����������ݣ��Զ�����ʵ������
                    // TODO: �ܷ�ŵ�ѭ������ȥ��һ���������ɸ�orderitem?
                    nRet = GenerateEntities(ref orderitem,
                        out strError);
                    if (nRet == -1)
                    {
                        // TODO: ��������ʵ���¼������ʵ���¼����ʧ�ܺ�Ӧ��ԭ��������¼���޸�ǰ״̬?
                        this.orderitems.Clear();
                        this.orderitems.AddRange(save_items);
                        // ˢ����ʾ
                        this.orderitems.AddToListView(this.ListView);
                        goto ERROR1;
                    }
                }
                 * */

                // �ȼ����б�
                this.Items.Add(orderitem);

                orderitem.AddToListView(this.listView);
                orderitem.HilightListViewItem(true);

                orderitem.Changed = true;    // ��Ϊ�����������������ζ����޸Ĺ����������Ա��⼯����ֻ��һ�����������ʱ�򣬼��ϵ�changedֵ����
            }

            // TODO: ��Ҫע��ɾ��listview��ĳЩԪ�أ�ѧDoDesignOrder


            if (this.GenerateEntity != null)
            {
                string strTargetRecPath = "";
                // �����������ݣ��Զ�����ʵ������
                nRet = GenerateEntities(
                    strBiblioSourceRecord,
                    strBiblioSourceSyntax,
                    changed_orderitems,
                    out strTargetRecPath,
                    out strError);
                if (nRet == -1)
                {
                    // TODO: ��������ʵ���¼������ʵ���¼����ʧ�ܺ�Ӧ��ԭ��������¼���޸�ǰ״̬?
                    this.Items.Clear();
                    this.Items.AddRange(save_items);
                    // ˢ����ʾ
                    this.Items.AddToListView(this.listView);
                    goto ERROR1;
                }

                // 2012/7/24
                this.TargetRecPath = strTargetRecPath;

                // Դ��¼�����ڲɹ�����ʱ��Դ��¼��Ҫд��998$t
                if (String.IsNullOrEmpty(strTargetRecPath) == false)
                {
                    string strBiblioDbName = Global.GetDbName(this.BiblioRecPath);
                    if (this.MainForm.IsOrderWorkDb(strBiblioDbName) == false)
                    {
                        if (this.SetTargetRecPath != null)
                        {
                            SetTargetRecPathEventArgs e = new SetTargetRecPathEventArgs();
                            e.TargetRecPath = strTargetRecPath;
                            this.SetTargetRecPath(this, e);
                            if (String.IsNullOrEmpty(e.ErrorInfo) == false)
                                goto ERROR1;
                        }
                    }


                }

                if (nRet == 0)
                    MessageBox.Show(this, "���棺��������û�д����κ��µĲ�(ʵ��)");
            }

#if NO
            // �ı䱣�水ť״̬
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = true;
                this.ContentChanged(this, e1);
            }
#endif
            TriggerContentChanged(bOldChanged, true);

            return;

        ERROR1:
            MessageBox.Show(ForegroundWindow.Instance, strError);
            return;
        }

        static string BuildOtherPrices(string strOrderPrice,
            string strAcceptPrice,
            string strBiblioPrice,
            int nRightCopy)
        {
            string strResult = "";

            if (String.IsNullOrEmpty(strOrderPrice) == false)
            {
                strResult += "������:" + strOrderPrice;
                if (nRightCopy > 1)
                    strResult += "/" + nRightCopy.ToString();
            }

            if (String.IsNullOrEmpty(strAcceptPrice) == false)
            {
                if (String.IsNullOrEmpty(strResult) == false)
                    strResult += ";";
                strResult += "���ռ�:" + strAcceptPrice;
                if (nRightCopy > 1)
                    strResult += "/" + nRightCopy.ToString();
            }

            if (String.IsNullOrEmpty(strBiblioPrice) == false)
            {
                if (String.IsNullOrEmpty(strResult) == false)
                    strResult += ";";
                strResult += "��Ŀ��:" + strBiblioPrice;
                if (nRightCopy > 1)
                    strResult += "/" + nRightCopy.ToString();
            }

            return strResult;
        }

        // �����������ݣ��Զ�����ʵ������
        // parameters:
        //      strTargetRecPath    ������ʵ���¼��Ŀ����Ŀ��¼���������´�����Ŀ���¼
        // return:
        //      -1  error
        //      0   û�д����κ��µ�ʵ��
        //      1   �ɹ�������ʵ��
        int GenerateEntities(
            string strNewBiblioRecord,
            string strNewBiblioRecordSyntax,
            List<OrderItem> orderitems,
            out string strTargetRecPath,
            out string strError)
        {
            strError = "";
            strTargetRecPath = "";

            if (this.GenerateEntity == null)
            {
                strError = "GenerateEntity�¼���δ�ҽ�";
                return -1;
            }

            GenerateEntityEventArgs data_container = new GenerateEntityEventArgs();
            data_container.InputItemBarcode = this.InputItemsBarcode;
            data_container.SetProcessingState = this.SetProcessingState;
            data_container.CreateCallNumber = this.CreateCallNumber;

            data_container.BiblioRecord = strNewBiblioRecord;
            data_container.BiblioSyntax = strNewBiblioRecordSyntax;

            string strBiblioPrice = DoGetMacroValue("@price");

            for (int j = 0; j < orderitems.Count; j++)
            {
                OrderItem order_item = orderitems[j];

                LocationCollection locations = new LocationCollection();
                int nRet = locations.Build(order_item.Distribute,
                    out strError);
                if (nRet == -1)
                    return -1;

                bool bChanged = false;

                // 2010/12/1 add
                string strOldCopyValue = "";
                string strNewCopyValue = "";
                OrderDesignControl.ParseOldNewValue(order_item.Copy,
                    out strOldCopyValue,
                    out strNewCopyValue);
                string strCopyString = strNewCopyValue;
                if (String.IsNullOrEmpty(strCopyString) == true)
                    strCopyString = strOldCopyValue;

                // 2010/12/1 add
                int nRightCopy = 1;  // ���ڲ���
                string strRightCopy = OrderDesignControl.GetRightFromCopyString(strCopyString);
                if (String.IsNullOrEmpty(strRightCopy) == false)
                {
                    try
                    {
                        nRightCopy = Convert.ToInt32(strRightCopy);
                    }
                    catch
                    {
                        strError = "���ڲ����ַ��� '" + strRightCopy + "' ��ʽ����";
                        return -1;
                    }
                }

                // Ϊÿ���ݲصص㴴��һ��ʵ���¼
                for (int i = 0; i < locations.Count; i++)
                {
                    Location location = locations[i];

                    // TODO: Ҫע�����㣺1) �Ѿ����չ����У��������*��refid���Ƿ�Ҫ�ٴδ����᣿����Ч����ʶ�������õ�ʱ���кô�
                    // 2) û���������ʱ���ǲ���Ҫ������������ѭ���ˣ����һ��

                    // �Ѿ����������������
                    if (location.RefID != "*")
                        continue;

                    location.RefID = "";


                    // 2010/12/1 add
                    for (int k = 0; k<nRightCopy ; k++)
                    {
                        GenerateEntityData e = new GenerateEntityData();

                        if (nRightCopy > 1)
                            e.Sequence = (k + 1).ToString() + "/" + nRightCopy.ToString();

                        e.Action = "new";
                        e.RefID = Guid.NewGuid().ToString();

                        if (String.IsNullOrEmpty(location.RefID) == false)
                            location.RefID += "|";  // ��ʾ��������

                        location.RefID += e.RefID;   // �޸ĵ��ݲصص��ַ�����

                        bChanged = true;

                        XmlDocument dom = new XmlDocument();
                        dom.LoadXml("<root />");

                        // 2009/10/19
                        // ״̬
                        if (this.SetProcessingState == true)
                        {
                            // �������ӹ��С�ֵ
                            string strOldState = DomUtil.GetElementText(dom.DocumentElement,
                                "state");
                            DomUtil.SetElementText(dom.DocumentElement,
                                "state", Global.AddStateProcessing(strOldState));

                        }

                        // seller���ǵ���ֵ
                        DomUtil.SetElementText(dom.DocumentElement,
                            "seller", order_item.Seller);

                        {
                            string strOldValue = "";
                            string strNewValue = "";


                            // source�ڲ�����ֵ
                            // ���� "old[new]" �ڵ�����ֵ
                            OrderDesignControl.ParseOldNewValue(order_item.Source,
                                out strOldValue,
                                out strNewValue);
                            DomUtil.SetElementText(dom.DocumentElement,
                                "source", strNewValue);
                        }

                        string strOrderPrice = "";
                        string strArrivePrice = "";

                        // ���������۸�
                        OrderDesignControl.ParseOldNewValue(order_item.Price,
                            out strOrderPrice,
                            out strArrivePrice);
                        string strPriceValue = "";
                        if (this.PriceDefault == "������")
                            strPriceValue = strOrderPrice;
                        else if (this.PriceDefault == "���ռ�")
                            strPriceValue = strArrivePrice;
                        else if (this.PriceDefault == "��Ŀ��")
                            strPriceValue = strBiblioPrice;


                        if (nRightCopy == 1)
                        {
                            DomUtil.SetElementText(dom.DocumentElement,
                                "price", strPriceValue);
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(strArrivePrice) == false)
                            {
                                DomUtil.SetElementText(dom.DocumentElement,
                                    "price", strPriceValue + "/" + nRightCopy.ToString());
                            }
                        }

                        e.OtherPrices = BuildOtherPrices(
    strOrderPrice,
    strArrivePrice,
    strBiblioPrice,
    nRightCopy);

                        // location
                        DomUtil.SetElementText(dom.DocumentElement,
                            "location", location.Name);

                        // ���κ�
                        DomUtil.SetElementText(dom.DocumentElement,
                            "batchNo", this.AcceptBatchNo);

                        e.Xml = dom.OuterXml;

                        data_container.DataList.Add(e);
                    } // end of j loop
                }

                // �ݲصص��ַ����б仯����Ҫ��ӳ������
                if (bChanged == true)
                {
                    order_item.Distribute = locations.ToString();
                    order_item.RefreshListView();
                }
            }

            if (data_container.DataList != null
                && (data_container.DataList.Count > 0 || String.IsNullOrEmpty(data_container.BiblioRecord) == false)
                )
            {
                // �����ⲿ�ҽӵ��¼�
                this.GenerateEntity(this, data_container);
                string strErrorText = "";

                if (String.IsNullOrEmpty(data_container.ErrorInfo) == false)
                {
                    strError = data_container.ErrorInfo;
                    return -1;
                }

                // 2009/11/8
                strTargetRecPath = data_container.TargetRecPath;

                for (int i = 0; i < data_container.DataList.Count; i++)
                {
                    GenerateEntityData data = data_container.DataList[i];
                    if (String.IsNullOrEmpty(data.ErrorInfo) == false)
                    {
                        strErrorText += data.ErrorInfo;
                    }
                }

                if (String.IsNullOrEmpty(strErrorText) == false)
                {
                    strError = strErrorText;
                    return -1;
                }

                return 1;
            }

            return 0;
        }

#if NO
        // �ⲿ���ýӿ�
        // ׷��һ���µĶ�����¼
        public int AppendOrder(OrderItem orderitem,
            out string strError)
        {
            strError = "";

            orderitem.Parent = Global.GetID(this.BiblioRecPath);

            this.Items.Add(orderitem);

            orderitem.ItemDisplayState = ItemDisplayState.New;
            orderitem.AddToListView(this.ListView);
            orderitem.HilightListViewItem(true);

            orderitem.Changed = true;
            return 0;
        }
#endif

                // �ⲿ���ýӿ�
        // ׷��һ���µĶ�����¼
        /// <summary>
        /// ׷��һ���µĶ�����¼
        /// </summary>
        /// <param name="orderitem">Ҫ׷�ӵ����OrderItem ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int AppendOrder(OrderItem orderitem,
            out string strError)
        {
            return this.AppendItem(orderitem, out strError);
        }

        // ����һ���������Ҫ�򿪶Ի�����������ϸ��Ϣ
        void DoNewOrder(/*string strIndex*/)
        {
            string strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(this.BiblioRecPath) == true)
            {
                strError = "��δ������Ŀ��¼";
                goto ERROR1;
            }

            // 
            if (this.Items == null)
                this.Items = new OrderItemCollection();

            Debug.Assert(this.Items != null, "");

            bool bOldChanged = this.Items.Changed;

#if NO
            if (String.IsNullOrEmpty(strIndex) == false)
            {

                // �Ե�ǰ�����ڽ��б�Ų���
                OrderItem dupitem = this.OrderItems.GetItemByIndex(
                    strIndex,
                    null);
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "�������ı�� '" + strIndex + "' �ͱ�����δ�ύ֮һɾ��������ء��������ύ����֮�޸ģ��ٽ����¶���������";
                    else
                        strText = "�������ı�� '" + strIndex + "' �ڱ������Ѿ����ڡ�";

                    // ������δ����
                    DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
        strText + "\r\n\r\nҪ�������Ѵ��ڱ�Ž����޸���",
        "OrderControl",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button2);

                    // תΪ�޸�
                    if (result == DialogResult.Yes)
                    {
                        ModifyOrder(dupitem);
                        return;
                    }

                    // ͻ����ʾ���Ա������Ա�۲������Ѿ����ڵļ�¼
                    dupitem.HilightListViewItem(true);
                    return;
                }

                // ��(����)���ж�����¼���б�Ų���
                if (true)
                {
                    string strOrderText = "";
                    string strBiblioText = "";
                    nRet = SearchOrderIndex(strIndex,
                        this.BiblioRecPath,
                        out strOrderText,
                        out strBiblioText,
                        out strError);
                    if (nRet == -1)
                        MessageBox.Show(ForegroundWindow.Instance, "�Ա�� '" + strIndex + "' ���в��صĹ����з�������: " + strError);
                    else if (nRet == 1) // �����ظ�
                    {
                        OrderIndexFoundDupDlg dlg = new OrderIndexFoundDupDlg();
                        MainForm.SetControlFont(dlg, this.Font, false);
                        dlg.MainForm = this.MainForm;
                        dlg.BiblioText = strBiblioText;
                        dlg.OrderText = strOrderText;
                        dlg.MessageText = "�������ı�� '" + strIndex + "' �����ݿ��з����Ѿ����ڡ�����޷�������";
                        dlg.ShowDialog(this);
                        return;
                    }
                }

            } // end of ' if (String.IsNullOrEmpty(strIndex) == false)
#endif

            OrderItem orderitem = new OrderItem();

            // ����ȱʡֵ
            nRet = SetItemDefaultValues(
                "order_normalRegister_default",
                true,
                orderitem,
                out strError);
            if (nRet == -1)
            {
                strError = "����ȱʡֵ��ʱ��������: " + strError;
                goto ERROR1;
            }

#if NO
            orderitem.Index = strIndex;
#endif
            orderitem.Parent = Global.GetRecordID(this.BiblioRecPath);

            // �ȼ����б�
            this.Items.Add(orderitem);
            orderitem.ItemDisplayState = ItemDisplayState.New;
            orderitem.AddToListView(this.listView);
            orderitem.HilightListViewItem(true);

            orderitem.Changed = true;    // ��Ϊ�����������������ζ����޸Ĺ����������Ա��⼯����ֻ��һ�����������ʱ�򣬼��ϵ�changedֵ����


            OrderEditForm edit = new OrderEditForm();

            edit.BiblioDbName = Global.GetDbName(this.BiblioRecPath);   // 2009/2/15
            edit.Text = "������������";
            edit.MainForm = this.MainForm;
            nRet = edit.InitialForEdit(orderitem,
                this.Items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            //REDO:
            this.MainForm.AppInfo.LinkFormState(edit, "OrderEditForm_state");
            edit.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(edit);

            if (edit.DialogResult != DialogResult.OK
                && edit.Item == orderitem    // ������δǰ���ƶ��������ƶ��ص���㣬Ȼ��Cancel
                )
            {
                this.Items.PhysicalDeleteItem(orderitem);

#if NO
                // �ı䱣�水ť״̬
                // SetSaveAllButtonState(true);
                if (this.ContentChanged != null)
                {
                    ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                    e1.OldChanged = bOldChanged;
                    e1.CurrentChanged = this.Items.Changed;
                    this.ContentChanged(this, e1);
                }
#endif
                TriggerContentChanged(bOldChanged, this.Items.Changed);


                return;
            }

#if NO
            // �ı䱣�水ť״̬
            // SetSaveAllButtonState(true);
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = true;
                this.ContentChanged(this, e1);
            }
#endif
            TriggerContentChanged(bOldChanged, true);


            // Ҫ�Ա��ֺ�������ض�������б�Ų��ء�
            // ������ˣ�Ҫ���ִ��ڣ��Ա��޸ġ�����������Ƕȣ���������ڶԻ���ر�ǰ����
            // �������´򿪶Ի���
            string strRefID = orderitem.RefID;
            if (String.IsNullOrEmpty(strRefID) == false)
            {

                // ��Ҫ�ų����ռ�����Լ�: orderitem��
                List<BookItemBase> excludeItems = new List<BookItemBase>();
                excludeItems.Add(orderitem);

                // �Ե�ǰ�����ڽ��вο�ID����
                OrderItem dupitem = this.Items.GetItemByRefID(
                    strRefID,
                    excludeItems) as OrderItem;
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "�������Ĳο�ID '" + strRefID + "' �ͱ�����δ�ύ֮һɾ���ο�ID���ء��������ύ����֮�޸ģ��ٽ�����������������";
                    else
                        strText = "�������Ĳο�ID '" + strRefID + "' �ڱ������Ѿ����ڡ�";

                    // ������δ����
                    DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
        strText + "\r\n\r\nҪ�������¼�¼�Ĳο�ID�����޸���\r\n(Yes �����޸�; No ���޸ģ��÷����ظ����¼�¼�����б�; Cancel �����ոմ������¼�¼)",
        "OrderControl",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);

                    // תΪ�޸�
                    if (result == DialogResult.Yes)
                    {
                        ModifyOrder(orderitem);
                        return;
                    }

                    // �����ոմ����ļ�¼
                    if (result == DialogResult.Cancel)
                    {
                        this.Items.PhysicalDeleteItem(orderitem);

#if NO
                        // �ı䱣�水ť״̬
                        // SetSaveAllButtonState(true);
                        if (this.ContentChanged != null)
                        {
                            ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                            e1.OldChanged = bOldChanged;
                            e1.CurrentChanged = this.Items.Changed;
                            this.ContentChanged(this, e1);
                        }
#endif
                        TriggerContentChanged(bOldChanged, this.Items.Changed);

                        return;
                    }

                    // ͻ����ʾ���Ա������Ա�۲������Ѿ����ڵļ�¼
                    dupitem.HilightListViewItem(true);
                    return;
                }
            } // end of ' if (String.IsNullOrEmpty(strPublishTime) == false)


            return;

        ERROR1:
            MessageBox.Show(ForegroundWindow.Instance, strError);
            return;
        }

#if NO
        // ����������š������±�Ų��ء�
        // ע������strIndex�޷���ö�����¼�����������Ŀ��¼·������
        int SearchOrderRefID(string strRefID,
            string strBiblioRecPath,
            out string strOrderText,
            out string strBiblioText,
            out string strError)
        {
            strError = "";
            strOrderText = "";
            strBiblioText = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڶԲο�ID '" + strRefID + "' ���в��� ...");
            stop.BeginLoop();

            try
            {
                byte[] order_timestamp = null;
                string strOrderRecPath = "";
                string strOutputBiblioRecPath = "";

                long lRet = Channel.GetOrderInfo(
                    stop,
                    strRefID,
                    strBiblioRecPath,
                    "html",
                    out strOrderText,
                    out strOrderRecPath,
                    out order_timestamp,
                    "html",
                    out strBiblioText,
                    out strOutputBiblioRecPath,
                    out strError);
                if (lRet == -1)
                    return -1;  // error

                if (lRet == 0)
                    return 0;   // not found
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 1;   // found
        }
#endif

#if !NEW_DUP_API
        // ��Ų��ء�����(������)�ɱ�Ų��ء�
        // �����������Զ��ų��͵�ǰ·��strOriginRecPath�ظ�֮����
        // parameters:
        //      strOriginRecPath    ������¼��·����
        //      paths   �������е�·��
        // return:
        //      -1  error
        //      0   not dup
        //      1   dup
        int SearchOrderIndexDup(string strIndex,
            string strBiblioRecPath,
            string strOriginRecPath,
            out string[] paths,
            out string strError)
        {
            strError = "";
            paths = null;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڶԱ�� '" + strIndex + "' ���в��� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.SearchOrderDup(
                    stop,
                    strIndex,
                    strBiblioRecPath,
                    100,
                    out paths,
                    out strError);
                if (lRet == -1)
                    return -1;  // error

                if (lRet == 0)
                    return 0;   // not found

                if (lRet == 1)
                {
                    // ��������һ��������·���Ƿ�ͳ�����¼һ��
                    if (paths.Length != 1)
                    {
                        strError = "ϵͳ����: SearchOrderDup() API����ֵΪ1������paths����ĳߴ�ȴ����1, ���� " + paths.Length.ToString();
                        return -1;
                    }

                    if (paths[0] != strOriginRecPath)
                        return 1;   // �����ظ�����

                    return 0;   // ���ظ�
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 1;   // found
        }
#endif

#if NEW_DUP_API

#if NO
        // �ο�ID���ء�����(������)�ɲο�ID���ء�
        // �����������Զ��ų��͵�ǰ·��strOriginRecPath�ظ�֮����
        // parameters:
        //      strOriginRecPath    ������¼��·����
        //      paths   �������е�·��
        // return:
        //      -1  error
        //      0   not dup
        //      1   dup
        int SearchOrderRefIdDup(string strRefID,
            string strBiblioRecPath,
            string strOriginRecPath,
            out string[] paths,
            out string strError)
        {
            strError = "";
            paths = null;

#if NO
            if (string.IsNullOrEmpty(strRefID) == true)
                return 0;   // ���ڿյĲο�ID���ز���
#endif
            if (string.IsNullOrEmpty(strRefID) == true)
            {
                strError = "��Ӧ�òο�IDΪ��������";
                return -1;
            }

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڶԲο�ID '" + strRefID + "' ���в��� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.SearchOrderDup(
                    stop,
                    strRefID,
                    strBiblioRecPath,
                    100,
                    out paths,
                    out strError);
                if (lRet == -1)
                    return -1;  // error

                if (lRet == 0)
                    return 0;   // not found

                if (lRet == 1)
                {
                    // ��������һ��������·���Ƿ�ͳ�����¼һ��
                    if (paths.Length != 1)
                    {
                        strError = "ϵͳ����: SearchOrderDup() API����ֵΪ1������paths����ĳߴ�ȴ����1, ���� " + paths.Length.ToString();
                        return -1;
                    }

                    if (paths[0] != strOriginRecPath)
                        return 1;   // �����ظ�����

                    return 0;   // ���ظ�
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 1;   // found
        }
#endif
        int SearchOrderRefIdDup(string strRefID,
    string strOriginRecPath,
    out string[] paths,
    out string strError)
        {
            strError = "";
            paths = null;

            if (string.IsNullOrEmpty(strRefID) == true)
            {
                strError = "��Ӧ�òο�IDΪ��������";
                return -1;
            }

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڶԲο�ID '" + strRefID + "' ���в��� ...");
            Stop.BeginLoop();

            try
            {
                long lRet = Channel.SearchOrder(
    Stop,
    "<ȫ��>",
    strRefID,
    100,
    "�ο�ID",
    "exact",
    "zh",
    "dup",
    "", // strSearchStyle
    "", // strOutputStyle
    out strError);
                if (lRet == -1)
                    return -1;  // error

                if (lRet == 0)
                    return 0;   // not found

                long lHitCount = lRet;

                List<string> aPath = null;
                lRet = Channel.GetSearchResult(Stop,
                    "dup",
                    0,
                    Math.Min(lHitCount, 100),
                    "zh",
                    out aPath,
                    out strError);
                if (lRet == -1)
                    return -1;

                paths = new string[aPath.Count];
                aPath.CopyTo(paths);

                if (lHitCount == 1)
                {
                    // ��������һ��������·���Ƿ�ͳ�����¼һ��
                    if (paths.Length != 1)
                    {
                        strError = "ϵͳ����: SearchOrder() API����ֵΪ1������paths����ĳߴ�ȴ����1, ���� " + paths.Length.ToString();
                        return -1;
                    }

                    if (paths[0] != strOriginRecPath)
                        return 1;   // �����ظ�����

                    return 0;   // ���ظ�
                }
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");
            }

            return 1;   // found
        }


#endif

#if NO
        string DoGetMacroValue(string strMacroName)
        {
            if (this.GetMacroValue != null)
            {
                GetMacroValueEventArgs e = new GetMacroValueEventArgs();
                e.MacroName = strMacroName;
                this.GetMacroValue(this, e);

                return e.MacroValue;
            }

            return null;
        }
#endif

#if NO
        // ΪOrderItem��������ȱʡֵ
        // parameters:
        //      strCfgEntry Ϊ"order_normalRegister_default"��"order_quickRegister_default"
        public int SetOrderItemDefaultValues(
            string strCfgEntry,
            OrderItem orderitem,
            out string strError)
        {
            strError = "";

            string strNewDefault = this.MainForm.AppInfo.GetString(
    "entityform_optiondlg",
    strCfgEntry,
    "<root />");

            // �ַ���strNewDefault������һ��XML��¼�������൱��һ����¼��ԭò��
            // ���ǲ����ֶε�ֵ����Ϊ"@"��������ʾ����һ�������
            // ��Ҫ����Щ����ֺ�����ʽ���ؼ�
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strNewDefault);
            }
            catch (Exception ex)
            {
                strError = "XML��¼װ��DOMʱ����: " + ex.Message;
                return -1;
            }

            // ��������һ��Ԫ�ص�����
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("*");
            for (int i = 0; i < nodes.Count; i++)
            {
                string strText = nodes[i].InnerText;
                if (strText.Length > 0 && strText[0] == '@')
                {
                    // ���ֺ�
                    nodes[i].InnerText = DoGetMacroValue(strText);
                }
            }

            strNewDefault = dom.OuterXml;

            int nRet = orderitem.SetData("",
                strNewDefault,
                null,
                out strError);
            if (nRet == -1)
                return -1;

            orderitem.Parent = "";
            orderitem.RecPath = "";

            return 0;
        }
#endif

        void ModifyOrder(OrderItem orderitem)
        {
            Debug.Assert(orderitem != null, "");

            bool bOldChanged = this.Items.Changed;

            string strOldIndex = orderitem.Index;

            OrderEditForm edit = new OrderEditForm();

            edit.BiblioDbName = Global.GetDbName(this.BiblioRecPath);   // 2009/2/15
            edit.MainForm = this.MainForm;
            edit.ItemControl = this;
            string strError = "";
            int nRet = edit.InitialForEdit(orderitem,
                this.Items,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(ForegroundWindow.Instance, strError);
                return;
            }
            edit.StartItem = null;  // ���ԭʼ������

        REDO:
            this.MainForm.AppInfo.LinkFormState(edit, "OrderEditForm_state");
            edit.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(edit);

            if (edit.DialogResult != DialogResult.OK)
                return;
#if NO
            // OrderItem�����Ѿ����޸�
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = true;
                this.ContentChanged(this, e1);
            }
#endif
            TriggerContentChanged(bOldChanged, true);


            this.EnableControls(false);
            try
            {


                if (strOldIndex != orderitem.Index) // ��Ÿı��˵�����²Ų���
                {
                    // ��Ҫ�ų����Լ�: orderitem��
                    List<OrderItem> excludeItems = new List<OrderItem>();
                    excludeItems.Add(orderitem);


                    // �Ե�ǰ�����ڽ��б�Ų���
                    OrderItem dupitem = this.Items.GetItemByIndex(
                        orderitem.Index,
                        excludeItems);
                    if (dupitem != null)
                    {
                        string strText = "";
                        if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                            strText = "��� '" + orderitem.Index + "' �ͱ�����δ�ύ֮һɾ��������ء�����ȷ������ť�������룬���˳��Ի���������ύ����֮�޸ġ�";
                        else
                            strText = "��� '" + orderitem.Index + "' �ڱ������Ѿ����ڡ�����ȷ������ť�������롣";

                        MessageBox.Show(ForegroundWindow.Instance, strText);
                        goto REDO;
                    }

                    // ��(����)���ж�����¼���б�Ų���
                    if (edit.AutoSearchDup == true
#if NEW_DUP_API
 && string.IsNullOrEmpty(orderitem.RefID) == false
#endif
)
                    {
                        // Debug.Assert(false, "");

                        string[] paths = null;

#if !NEW_DUP_API
                        // ��Ų��ء�
                        // parameters:
                        //      strOriginRecPath    ������¼��·����
                        //      paths   �������е�·��
                        // return:
                        //      -1  error
                        //      0   not dup
                        //      1   dup
                        nRet = SearchOrderIndexDup(orderitem.Index,
                            this.BiblioRecPath,
                            orderitem.RecPath,
                            out paths,
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(ForegroundWindow.Instance, "�Ա�� '" + orderitem.Index + "' ���в��صĹ����з�������: " + strError);
                        else if (nRet == 1) // �����ظ�
                        {
                            string pathlist = String.Join(",", paths);

                            string strText = "��� '" + orderitem.Index + "' �����ݿ��з����Ѿ���(���������ֵ�)���ж�����¼��ʹ�á�\r\n" + pathlist + "\r\n\r\n����ȷ������ť���±༭������Ϣ�����߸�����ʾ�Ķ�����¼·����ȥ�޸�����������¼��Ϣ��";
                            MessageBox.Show(ForegroundWindow.Instance, strText);

                            goto REDO;
                        }
#else
                        // �ο�ID���ء�
                        // parameters:
                        //      strOriginRecPath    ������¼��·����
                        //      paths   �������е�·��
                        // return:
                        //      -1  error
                        //      0   not dup
                        //      1   dup
                        nRet = SearchOrderRefIdDup(orderitem.RefID,
                            // this.BiblioRecPath,
                            orderitem.RecPath,
                            out paths,
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(ForegroundWindow.Instance, "�Բο�ID '" + orderitem.RefID + "' ���в��صĹ����з�������: " + strError);
                        else if (nRet == 1) // �����ظ�
                        {
                            string pathlist = String.Join(",", paths);

                            string strText = "�ο�ID '" + orderitem.RefID + "' �����ݿ��з����Ѿ���(���������ֵ�)���ж�����¼��ʹ�á�\r\n" + pathlist + "\r\n\r\n����ȷ������ť���±༭������Ϣ�����߸�����ʾ�Ķ�����¼·����ȥ�޸�����������¼��Ϣ��";
                            MessageBox.Show(ForegroundWindow.Instance, strText);

                            goto REDO;
                        }
#endif
                    }
                }

            }
            finally
            {
                this.EnableControls(true);
            }
        }

#if NO
        // �������б���
        // return:
        //      -2  �Ѿ�����(���ֳɹ�������ʧ��)
        //      -1  ����
        //      0   ����ɹ���û�д���;���
        int SaveOrders(EntityInfo[] orders,
            out string strError)
        {
            strError = "";

            bool bWarning = false;
            EntityInfo[] errorinfos = null;

            int nBatch = 100;
            for (int i = 0; i < (orders.Length / nBatch) + ((orders.Length % nBatch) != 0 ? 1 : 0); i++)
            {
                int nCurrentCount = Math.Min(nBatch, orders.Length - i * nBatch);
                EntityInfo[] current = EntityControl.GetPart(orders, i * nBatch, nCurrentCount);

                int nRet = SaveOrderRecords(this.BiblioRecPath,
                    current,
                    out errorinfos,
                    out strError);

                // �ѳ�����������Ҫ����״̬��������ֵ���ʾ���ڴ�
                if (RefreshOperResult(errorinfos) == true)
                    bWarning = true;

                if (nRet == -1)
                    return -1;
            }

            if (bWarning == true)
                return -2;
            return 0;
        }

        // �ύ������������
        // return:
        //      -1  ����
        //      0   û�б�Ҫ����
        //      1   ����ɹ�
        public int DoSaveOrders()
        {
            // 2008/9/17
            if (this.Items == null)
                return 0;

            EnableControls(false);

            try
            {
                string strError = "";
                int nRet = 0;

                if (this.Items == null)
                {
                    /*
                    strError = "û�ж�����Ϣ��Ҫ����";
                    goto ERROR1;
                     * */
                    return 0;
                }

                // ���ȫ�������Parentֵ�Ƿ��ʺϱ���
                // return:
                //      -1  �д��󣬲��ʺϱ���
                //      0   û�д���
                nRet = this.Items.CheckParentIDForSave(out strError);
                if (nRet == -1)
                {
                    strError = "���涩����Ϣʧ�ܣ�ԭ��" + strError;
                    goto ERROR1;
                }

                EntityInfo[] orders = null;

                // ������Ҫ�ύ�Ķ�����Ϣ����
                nRet = BuildSaveOrders(
                    out orders,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (orders == null || orders.Length == 0)
                    return 0; // û�б�Ҫ����

#if NO
                EntityInfo[] errorinfos = null;
                nRet = SaveOrderRecords(this.BiblioRecPath,
                    orders,
                    out errorinfos,
                    out strError);

                // �ѳ�����������Ҫ����״̬��������ֵ���ʾ���ڴ�
                RefreshOperResult(errorinfos);

                if (nRet == -1)
                {
                    goto ERROR1;
                }
#endif
                // return:
                //      -2  �Ѿ�����(���ֳɹ�������ʧ��)
                //      -1  ����
                //      0   ����ɹ���û�д���;���
                nRet = SaveOrders(orders, out strError);
                if (nRet == -2)
                    return -1;  // SaveOrders()�Ѿ�MessageBox()��ʾ����
                if (nRet == -1)
                    goto ERROR1;

                this.Changed = false;
                this.MainForm.StatusBarMessage = "������Ϣ �ύ / ���� �ɹ�";
                return 1;
            ERROR1:
                MessageBox.Show(ForegroundWindow.Instance, strError);
                return -1;
            }
            finally
            {
                EnableControls(true);
            }
        }

        // ���ݶ�����¼·����������
        public OrderItem HilightLineByItemRecPath(string strItemRecPath,
                bool bClearOtherSelection)
        {
            OrderItem orderitem = null;

            if (bClearOtherSelection == true)
            {
                this.ListView.SelectedItems.Clear();
            }

            if (this.Items != null)
            {
                orderitem = this.Items.GetItemByRecPath(strItemRecPath) as OrderItem;
                if (orderitem != null)
                    orderitem.HilightListViewItem(true);
            }

            return orderitem;
        }

        // �������ڱ���Ķ�����Ϣ����
        int BuildSaveOrders(
            out EntityInfo[] orders,
            out string strError)
        {
            strError = "";
            orders = null;
            int nRet = 0;

            // TODO: �Լ����ڵ�ȫ�������refid���в���

            Debug.Assert(this.Items != null, "");

            List<EntityInfo> orderArray = new List<EntityInfo>();

            foreach (OrderItem orderitem in this.Items)
            {
                // OrderItem orderitem = this.OrderItems[i];

                if (orderitem.ItemDisplayState == ItemDisplayState.Normal)
                    continue;

                EntityInfo info = new EntityInfo();

                // 2010/3/15 add
                if (String.IsNullOrEmpty(orderitem.RefID) == true)
                {
                    orderitem.RefID = Guid.NewGuid().ToString();
                    orderitem.RefreshListView();
                }

                info.RefID = orderitem.RefID;  // 2008/2/17

                string strXml = "";
                nRet = orderitem.BuildRecord(out strXml,
                        out strError);
                if (nRet == -1)
                    return -1;

                if (orderitem.ItemDisplayState == ItemDisplayState.New)
                {
                    info.Action = "new";
                    info.NewRecPath = "";
                    info.NewRecord = strXml;
                    info.NewTimestamp = null;
                }

                if (orderitem.ItemDisplayState == ItemDisplayState.Changed)
                {
                    info.Action = "change";
                    info.OldRecPath = orderitem.RecPath;
                    info.NewRecPath = orderitem.RecPath;

                    info.NewRecord = strXml;
                    info.NewTimestamp = null;

                    info.OldRecord = orderitem.OldRecord;
                    info.OldTimestamp = orderitem.Timestamp;
                }

                if (orderitem.ItemDisplayState == ItemDisplayState.Deleted)
                {
                    info.Action = "delete";
                    info.OldRecPath = orderitem.RecPath; // NewRecPath

                    info.NewRecord = "";
                    info.NewTimestamp = null;

                    info.OldRecord = orderitem.OldRecord;
                    info.OldTimestamp = orderitem.Timestamp;
                }

                orderArray.Add(info);
            }

            // ���Ƶ�Ŀ��
            orders = new EntityInfo[orderArray.Count];
            for (int i = 0; i < orderArray.Count; i++)
            {
                orders[i] = orderArray[i];
            }

            return 0;
        }

        // ���������޸Ĺ�������Ϣ����
        // ���strNewBiblioPath�е���Ŀ���������仯���Ƕ�����¼��Ҫ�ڶ�����֮���ƶ�����Ϊ���������Ŀ����һ���������ϵ��
        int BuildChangeParentRequestOrders(
            List<OrderItem> orderitems,
            string strNewBiblioRecPath,
            out EntityInfo[] entities,
            out string strError)
        {
            strError = "";
            entities = null;
            int nRet = 0;

            string strSourceBiblioDbName = Global.GetDbName(this.BiblioRecPath);
            string strTargetBiblioDbName = Global.GetDbName(strNewBiblioRecPath);

            // ���һ��Ŀ����Ŀ�����ǲ��ǺϷ�����Ŀ����
            if (MainForm.IsValidBiblioDbName(strTargetBiblioDbName) == false)
            {
                strError = "Ŀ����� '" + strTargetBiblioDbName + "' ����ϵͳ�������Ŀ����֮��";
                return -1;
            }

            // ���Ŀ����Ŀ��¼id
            string strTargetBiblioRecID = Global.GetRecordID(strNewBiblioRecPath);   // !!!
            if (String.IsNullOrEmpty(strTargetBiblioRecID) == true)
            {
                strError = "��Ŀ����Ŀ��¼·�� '" + strNewBiblioRecPath + "' ��û�а���ID���֣��޷����в���";
                return -1;
            }
            if (strTargetBiblioRecID == "?")
            {
                strError = "Ŀ����Ŀ��¼·�� '" + strNewBiblioRecPath + "' �м�¼ID��ӦΪ�ʺ�";
                return -1;
            }
            if (Global.IsPureNumber(strTargetBiblioRecID) == false)
            {
                strError = "Ŀ����Ŀ��¼·�� '" + strNewBiblioRecPath + "' �м�¼IDӦΪ������";
                return -1;
            }

            bool bMove = false; // �Ƿ���Ҫ�ƶ�������¼
            string strTargetOrderDbName = "";  // Ŀ�궩������

            if (strSourceBiblioDbName != strTargetBiblioDbName)
            {
                // ��Ŀ�ⷢ���˸ı䣬���б�Ҫ�ƶ�����������޸Ķ�����¼��<parent>����
                bMove = true;
                strTargetOrderDbName = MainForm.GetOrderDbName(strTargetBiblioDbName);

                if (String.IsNullOrEmpty(strTargetOrderDbName) == true)
                {
                    strError = "��Ŀ�� '" + strTargetBiblioDbName + "' ��û�д����Ķ����ⶨ�塣����ʧ��";
                    return -1;
                }
            }

            Debug.Assert(orderitems != null, "");

            List<EntityInfo> entityArray = new List<EntityInfo>();

            for (int i = 0; i < orderitems.Count; i++)
            {
                OrderItem orderitem = orderitems[i];

                EntityInfo info = new EntityInfo();

                if (String.IsNullOrEmpty(orderitem.RefID) == true)
                {
                    Debug.Assert(false,"orderitem.RefIDӦ��Ϊֻ�������Ҳ�����Ϊ��");
                    /*
                    orderitem.RefID = Guid.NewGuid().ToString();
                    orderitem.RefreshListView();
                     * */
                }

                info.RefID = orderitem.RefID;
                orderitem.Parent = strTargetBiblioRecID;

                string strXml = "";
                nRet = orderitem.BuildRecord(out strXml,
                        out strError);
                if (nRet == -1)
                    return -1;

                info.OldRecPath = orderitem.RecPath;
                if (bMove == false)
                {
                    info.Action = "change";
                    info.NewRecPath = orderitem.RecPath;
                }
                else
                {
                    info.Action = "move";
                    Debug.Assert(String.IsNullOrEmpty(strTargetOrderDbName) == false, "");
                    info.NewRecPath = strTargetOrderDbName + "/?";  // �Ѷ�����¼�ƶ�����һ���������У�׷�ӳ�һ���¼�¼�����ɼ�¼�Զ���ɾ��
                }

                info.NewRecord = strXml;
                info.NewTimestamp = null;

                info.OldRecord = orderitem.OldRecord;
                info.OldTimestamp = orderitem.Timestamp;

                entityArray.Add(info);
            }

            // ���Ƶ�Ŀ��
            entities = new EntityInfo[entityArray.Count];
            for (int i = 0; i < entityArray.Count; i++)
            {
                entities[i] = entityArray[i];
            }

            return 0;
        }

        // ���涩����¼
        // ������ˢ�½���ͱ���
        int SaveOrderRecords(string strBiblioRecPath,
            EntityInfo[] orders,
            out EntityInfo[] errorinfos,
            out string strError)
        {
            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڱ��涩����Ϣ ...");
            Stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.SetOrders(
                    Stop,
                    strBiblioRecPath,
                    orders,
                    out errorinfos,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }

        // �ѱ�����Ϣ�еĳɹ������״̬�޸Ķ���
        // ���ҳ���ȥ��û�б���ġ�ɾ����OrderItem����ڴ���Ӿ��ϣ�
        // return:
        //      false   û�о���
        //      true    ���־���
        bool RefreshOperResult(EntityInfo[] errorinfos)
        {
            int nRet = 0;

            string strWarning = ""; // ������Ϣ

            if (errorinfos == null)
                return false;

            bool bOldChanged = this.Items.Changed;

            for (int i = 0; i < errorinfos.Length; i++)
            {
                /*
                XmlDocument dom = new XmlDocument();

                string strNewXml = errorinfos[i].NewRecord;
                string strOldXml = errorinfos[i].OldRecord;

                if (String.IsNullOrEmpty(strNewXml) == false)
                {
                    dom.LoadXml(strNewXml);
                }
                else if (String.IsNullOrEmpty(strOldXml) == false)
                {
                    dom.LoadXml(strOldXml);
                }
                else
                {
                    // �Ҳ����������λ
                    Debug.Assert(false, "�Ҳ�����λ�ı��");
                    // �Ƿ񵥶���ʾ����?
                    continue;
                }
                 * */

                OrderItem orderitem = null;

                string strError = "";

                if (String.IsNullOrEmpty(errorinfos[i].RefID) == true)
                {
                    MessageBox.Show(ForegroundWindow.Instance, "���������ص�EntityInfo�ṹ��RefIDΪ��");
                    return true;
                }

                /*
                string strIndex = "";
                // ��listview�ж�λ��dom����������
                // ˳�θ��� ��¼·�� -- ��� ����λ
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = LocateOrderItem(
                    errorinfos[i].OldRecPath,   // ԭ����NewRecPath
                    dom,
                    out orderitem,
                    out strIndex,
                    out strError);
                 * */
                nRet = LocateOrderItem(
                    errorinfos[i].RefID,
                    GetOneRecPath(errorinfos[i].NewRecPath, errorinfos[i].OldRecPath),
                    out orderitem,
                    out strError);
                if (nRet == -1 || nRet == 0)
                {
                    MessageBox.Show(ForegroundWindow.Instance, "��λ������Ϣ '" + errorinfos[i].ErrorInfo + "' �����еĹ����з�������:" + strError);
                    continue;
                }

                if (nRet == 0)
                {
                    MessageBox.Show(ForegroundWindow.Instance, "�޷���λ����ֵΪ " + i.ToString() + " �Ĵ�����Ϣ '" + errorinfos[i].ErrorInfo + "'");
                    continue;
                }

                string strLocationSummary = GetLocationSummary(
                    orderitem.Index,    // strIndex,
                    errorinfos[i].NewRecPath,
                    errorinfos[i].RefID);

                // ������Ϣ����
                if (errorinfos[i].ErrorCode == ErrorCodeValue.NoError)
                {
                    if (errorinfos[i].Action == "new")
                    {
                        orderitem.OldRecord = errorinfos[i].NewRecord;
                        nRet = orderitem.ResetData(
                            errorinfos[i].NewRecPath,
                            errorinfos[i].NewRecord,
                            errorinfos[i].NewTimestamp,
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(ForegroundWindow.Instance, strError);
                    }
                    else if (errorinfos[i].Action == "change"
                        || errorinfos[i].Action == "move")
                    {
                        orderitem.OldRecord = errorinfos[i].NewRecord;

                        nRet = orderitem.ResetData(
                            errorinfos[i].NewRecPath,
                            errorinfos[i].NewRecord,
                            errorinfos[i].NewTimestamp,
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(ForegroundWindow.Instance, strError);

                        orderitem.ItemDisplayState = ItemDisplayState.Normal;
                    }

                    // ���ڱ�����ò������ڱ��ֵģ�Ҫ��listview������
                    if (String.IsNullOrEmpty(orderitem.RecPath) == false)
                    {
                        string strTempOrderDbName = Global.GetDbName(orderitem.RecPath);
                        string strTempBiblioDbName = this.MainForm.GetBiblioDbNameFromOrderDbName(strTempOrderDbName);

                        Debug.Assert(String.IsNullOrEmpty(strTempBiblioDbName) == false, "");
                        // TODO: ����Ҫ���汨��

                        string strTempBiblioRecPath = strTempBiblioDbName + "/" + orderitem.Parent;

                        if (strTempBiblioRecPath != this.BiblioRecPath)
                        {
                            this.Items.PhysicalDeleteItem(orderitem);
                            continue;
                        }
                    }

                    orderitem.Error = null;   // ������ʾ ��?

                    orderitem.Changed = false;
                    orderitem.RefreshListView();
                    continue;
                }

                // ������
                orderitem.Error = errorinfos[i];
                orderitem.RefreshListView();

                strWarning += strLocationSummary + "���ύ������������з������� -- " + errorinfos[i].ErrorInfo + "\r\n";
            }


            // ����û�б���ģ���Щ�ɹ�ɾ����������ڴ���Ӿ���Ĩ��
            for (int i = 0; i < this.Items.Count; i++)
            {
                OrderItem orderitem = this.Items[i] as OrderItem;
                if (orderitem.ItemDisplayState == ItemDisplayState.Deleted)
                {
                    if (string.IsNullOrEmpty(orderitem.ErrorInfo) == true)
                    {
                        this.Items.PhysicalDeleteItem(orderitem);
                        i--;
                    }
                }
            }

            // �޸�Changed״̬
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = this.Items.Changed;
                this.ContentChanged(this, e1);
            }

            // 
            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strWarning += "\r\n��ע���޸Ķ�����Ϣ�������ύ����";
                MessageBox.Show(ForegroundWindow.Instance, strWarning);
                return true;
            }

            return false;
        }
        // ��������¼·����ѡ��һ������׷�ӷ�ʽ��ʵ��·��
        public static string GetOneRecPath(string strRecPath1, string strRecPath2)
        {
            if (string.IsNullOrEmpty(strRecPath1) == true)
                return strRecPath2;

            if (Global.IsAppendRecPath(strRecPath1) == false)
                return strRecPath1;

            return strRecPath2;
        }
#endif



#if NO
        // ��������ƺ�
        static string GetLocationSummary(
            string strIndex,
            string strRecPath,
            string strRefID)
        {
            if (String.IsNullOrEmpty(strIndex) == false)
                return "���Ϊ '" + strIndex + "' ������";
            if (String.IsNullOrEmpty(strRecPath) == false)
                return "��¼·��Ϊ '" + strRecPath + "' ������";
            // 2009/10/27
            if (String.IsNullOrEmpty(strRefID) == false)
                return "�ο�IDΪ '" + strRefID + "' ������";


            return "���κζ�λ��Ϣ������";
        }
#endif

        // ��������ƺ�
        internal override string GetLocationSummary(OrderItem bookitem)
        {
            string strIndex = bookitem.Index;

            if (String.IsNullOrEmpty(strIndex) == false)
                return "���Ϊ '" + strIndex + "' ������";

            string strRecPath = bookitem.RecPath;

            if (String.IsNullOrEmpty(strRecPath) == false)
                return "��¼·��Ϊ '" + strRecPath + "' ������";

            string strRefID = bookitem.RefID;
            // 2008/6/24
            if (String.IsNullOrEmpty(strRefID) == false)
                return "�ο�IDΪ '" + strRefID + "' ������";

            return "���κζ�λ��Ϣ������";
        }



#if NOOOOOOOOOOOOOOOO
        // ��this.orderitems�ж�λ��dom����������
        // ˳�θ��� ��¼·�� -- ��� ����λ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int LocateOrderItem(
            string strRecPath,
            XmlDocument dom,
            out OrderItem orderitem,
            out string strIndex,
            out string strError)
        {
            strError = "";
            orderitem = null;
            strIndex = "";

            // ��ǰ��ȡ, �Ա��κη���·��ʱ, �����Եõ���Щֵ
            strIndex = DomUtil.GetElementText(dom.DocumentElement,
                "index");

            if (String.IsNullOrEmpty(strRecPath) == false)
            {
                orderitem = this.orderitems.GetItemByRecPath(strRecPath);

                if (orderitem != null)
                    return 1;   // found

            }

            if (String.IsNullOrEmpty(strIndex) == false)
            {
                orderitem = this.orderitems.GetItemByIndex(
                    strIndex,
                    null);
                if (orderitem != null)
                    return 1;   // found

            }

            return 0;
        }
#endif

        // ��this.orderitems�ж�λ��strRecPath/strRefID����������
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int LocateOrderItem(
            string strRefID,
            string strRecPath,
            out OrderItem orderitem,
            out string strError)
        {
            strError = "";

            // �����ü�¼·������λ
            if (string.IsNullOrEmpty(strRecPath) == false
                && Global.IsAppendRecPath(strRecPath) == false)
            {
                orderitem = this.Items.GetItemByRecPath(strRecPath) as OrderItem;
                if (orderitem != null)
                    return 1;   // found
            }

            // Ȼ���òο�ID����λ
            orderitem = this.Items.GetItemByRefID(strRefID, null) as OrderItem;

            if (orderitem != null)
                return 1;   // found

            strError = "û���ҵ� ��¼·��Ϊ '"+strRecPath+"'������ �ο�ID Ϊ '" + strRefID + "' ��OrderItem����";
            return 0;
        }

        private void ListView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            bool bHasBillioLoaded = false;

            if (String.IsNullOrEmpty(this.BiblioRecPath) == false)
                bHasBillioLoaded = true;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;



            menuItem = new MenuItem("����(&O)");
            menuItem.Click += new System.EventHandler(this.menu_design_Click);
            if (bHasBillioLoaded == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("����(&A)");
            menuItem.Click += new System.EventHandler(this.menu_arrive_Click);
            if (bHasBillioLoaded == false || this.SeriesMode == true)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            bool bEnableOpenTarget = true;
            string strTargetRecID = "";
            if (String.IsNullOrEmpty(this.TargetRecPath) == false)
            {
                strTargetRecID = Global.GetRecordID(this.TargetRecPath);
            }

            if (this.OpenTargetRecord == null)
                bEnableOpenTarget = false;
            else if (this.TargetRecPath == this.BiblioRecPath)
                bEnableOpenTarget = false;
            else if (String.IsNullOrEmpty(strTargetRecID) == true || strTargetRecID == "?")
                bEnableOpenTarget = false;

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            bool bAllowModify = StringUtil.IsInList("client_uimodifyorderrecord", this.Rights) == true;

            {
                menuItem = new MenuItem("�޸�(&M)");
                menuItem.Click += new System.EventHandler(this.menu_modifyOrder_Click);
                if (this.listView.SelectedItems.Count == 0 || bAllowModify == false)
                    menuItem.Enabled = false;
                contextMenu.MenuItems.Add(menuItem);

                // -----
                menuItem = new MenuItem("-");
                contextMenu.MenuItems.Add(menuItem);


                menuItem = new MenuItem("����(&N)");
                menuItem.Click += new System.EventHandler(this.menu_newOrder_Click);
                if (bHasBillioLoaded == false || bAllowModify == false)
                    menuItem.Enabled = false;
                contextMenu.MenuItems.Add(menuItem);

                // -----
                menuItem = new MenuItem("-");
                contextMenu.MenuItems.Add(menuItem);
            }

            // ȫѡ
            menuItem = new MenuItem("ȫѡ(&A)");
            menuItem.Click += new System.EventHandler(this.menu_selectAll_Click);
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�������յ�Ŀ���¼ '"+this.TargetRecPath+"' (&T)");
            menuItem.Click += new System.EventHandler(this.menu_openTargetRecord_Click);
            if (bEnableOpenTarget == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("������ʾ���ڱ��ֵġ��������κ�Ϊ '" + this.AcceptBatchNo + "' �Ĳ��¼(&H)");
            menuItem.Click += new System.EventHandler(this.menu_hilightTargetItemLines_Click);
            if (String.IsNullOrEmpty(this.AcceptBatchNo) == true
                || this.HilightTargetItem == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            /*

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // cut ����
            menuItem = new MenuItem("����(&T)");
            menuItem.Click += new System.EventHandler(this.menu_cutEntity_Click);
            if (this.listView_items.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // copy ����
            menuItem = new MenuItem("����(&C)");
            menuItem.Click += new System.EventHandler(this.menu_copyEntity_Click);
            if (this.listView_items.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            bool bHasClipboardObject = false;
            IDataObject iData = Clipboard.GetDataObject();
            if (iData == null
                || iData.GetDataPresent(typeof(ClipboardBookItemCollection)) == false)
                bHasClipboardObject = false;
            else
                bHasClipboardObject = true;

            // paste ճ��
            menuItem = new MenuItem("ճ��(&P)");
            menuItem.Click += new System.EventHandler(this.menu_pasteEntity_Click);
            if (bHasClipboardObject == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


             * */

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            // �ı����
            menuItem = new MenuItem("�ı����(&B)");
            menuItem.Click += new System.EventHandler(this.menu_changeParent_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("װ���¿��Ķ�����(&E)");
            menuItem.Click += new System.EventHandler(this.menu_loadToNewItemForm_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("װ���Ѿ��򿪵Ķ�����(&E)");
            menuItem.Click += new System.EventHandler(this.menu_loadToExistItemForm_Click);
            if (this.listView.SelectedItems.Count == 0
                || this.MainForm.GetTopChildWindow<ItemInfoForm>() == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�쿴������¼�ļ����� (&K)");
            menuItem.Click += new System.EventHandler(this.menu_getKeys_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("���ɾ��(&D)");
            menuItem.Click += new System.EventHandler(this.menu_deleteOrder_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("����ɾ��(&U)");
            menuItem.Click += new System.EventHandler(this.menu_undoDeleteOrder_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView, new Point(e.X, e.Y));		
        }

        // ȫѡ
        void menu_selectAll_Click(object sender, EventArgs e)
        {
            ListViewUtil.SelectAllLines(this.listView);
        }

        void menu_loadToNewItemForm_Click(object sender, EventArgs e)
        {
#if NO
            string strError = "";

            if (this.ListView.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ����������";
                goto ERROR1;
            }

            OrderItem cur = (OrderItem)this.ListView.SelectedItems[0].Tag;

            if (cur == null)
            {
                strError = "OrderItem == null";
                goto ERROR1;
            }

            string strRecPath = cur.RecPath;
            if (string.IsNullOrEmpty(strRecPath) == true)
            {
                strError = "��ѡ���������¼·��Ϊ�գ���δ�����ݿ��н���";
                goto ERROR1;
            }

            ItemInfoForm form = null;

            form = new ItemInfoForm();
            form.MdiParent = this.MainForm;
            form.MainForm = this.MainForm;
            form.Show();

            form.DbType = "order";

            form.LoadRecordByRecPath(strRecPath, "");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
#endif
            LoadToItemInfoForm(true);

        }

        void menu_loadToExistItemForm_Click(object sender, EventArgs e)
        {
#if NO
            string strError = "";

            if (this.ListView.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ����������";
                goto ERROR1;
            }

            OrderItem cur = (OrderItem)this.ListView.SelectedItems[0].Tag;

            if (cur == null)
            {
                strError = "OrderItem == null";
                goto ERROR1;
            }

            string strRecPath = cur.RecPath;
            if (string.IsNullOrEmpty(strRecPath) == true)
            {
                strError = "��ѡ���������¼·��Ϊ�գ���δ�����ݿ��н���";
                goto ERROR1;
            }

            ItemInfoForm form = this.MainForm.GetTopChildWindow<ItemInfoForm>();
            if (form == null)
            {
                strError = "��ǰ��û���Ѿ��򿪵Ķ�����";
                goto ERROR1;
            }
            form.DbType = "order";
            form.Activate();
            if (form.WindowState == FormWindowState.Minimized)
                form.WindowState = FormWindowState.Normal;

            form.LoadRecordByRecPath(strRecPath, "");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
#endif
            LoadToItemInfoForm(false);

        }

#if NO
        // �ı����
        // ���޸Ķ�����Ϣ��<parent>Ԫ�����ݣ�ʹָ������һ����Ŀ��¼
        void menu_changeParent_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.ListView.SelectedItems.Count == 0)
            {
                strError = "��δָ��Ҫ�޸Ĺ���������";
                goto ERROR1;
            }

            // TODO: �������δ�����,�Ƿ�Ҫ�����ȱ���?

            string strNewBiblioRecPath = InputDlg.GetInput(
                this,
                "��ָ���µ���Ŀ��¼·��",
                "��Ŀ��¼·��(��ʽ'����/ID'): ",
                "",
            this.MainForm.DefaultFont);

            if (strNewBiblioRecPath == null)
                return;

            // TODO: ��ü��һ�����·���ĸ�ʽ���Ϸ�����Ŀ����������MainForm���ҵ�

            if (String.IsNullOrEmpty(strNewBiblioRecPath) == true)
            {
                strError = "��δָ���µ���Ŀ��¼·������������";
                goto ERROR1;
            }

            if (strNewBiblioRecPath == this.BiblioRecPath)
            {
                strError = "ָ��������Ŀ��¼·���͵�ǰ��Ŀ��¼·����ͬ����������";
                goto ERROR1;
            }

            List<OrderItem> selectedorderitems = new List<OrderItem>();
            foreach (ListViewItem item in this.ListView.SelectedItems)
            {
                // ListViewItem item = this.ListView.SelectedItems[i];

                OrderItem orderitem = (OrderItem)item.Tag;

                selectedorderitems.Add(orderitem);
            }

            EntityInfo[] orders = null;

            nRet = BuildChangeParentRequestEntities(
                selectedorderitems,
                strNewBiblioRecPath,
                out orders,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (orders == null || orders.Length == 0)
                return; // û�б�Ҫ����

#if NO
            EntityInfo[] errorinfos = null;
            nRet = SaveOrderRecords(strNewBiblioRecPath,
                entities,
                out errorinfos,
                out strError);

            // �ѳ�����������Ҫ����״̬��������ֵ���ʾ���ڴ�
            // �Ƿ��������ѹ����Ѿ��ı�������ų���listview?
            RefreshOperResult(errorinfos);


            if (nRet == -1)
            {
                goto ERROR1;
            }
#endif
            nRet = SaveEntities(orders, out strError);
            if (nRet == -1)
                goto ERROR1;

            this.MainForm.StatusBarMessage = "������Ϣ �޸Ĺ��� �ɹ�";
            return;
        ERROR1:
            MessageBox.Show(ForegroundWindow.Instance, strError);
        }
#endif

        void menu_hilightTargetItemLines_Click(object sender, EventArgs e)
        {
            if (this.HilightTargetItem == null)
            {
                MessageBox.Show(this, "��δ�ҽ�HilightTargetItem�¼�");
                return;
            }

            if (String.IsNullOrEmpty(this.AcceptBatchNo) == false)
            {
                HilightTargetItemsEventArgs e1 = new HilightTargetItemsEventArgs();
                e1.BatchNo = this.AcceptBatchNo;
                this.HilightTargetItem(this, e1);
            }
        }

        void menu_openTargetRecord_Click(object sender, EventArgs e)
        {
            if (this.OpenTargetRecord == null)
                return;

            OpenTargetRecordEventArgs e1 = new OpenTargetRecordEventArgs();
            e1.SourceRecPath = this.BiblioRecPath;
            e1.TargetRecPath = this.TargetRecPath;
            e1.BatchNo = this.AcceptBatchNo;
            this.OpenTargetRecord(this, e1);
            if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
                MessageBox.Show(this, e1.ErrorInfo);
        }

        void menu_modifyOrder_Click(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "��δѡ��Ҫ�༭������");
                return;
            }
            OrderItem orderitem = (OrderItem)this.listView.SelectedItems[0].Tag;

            ModifyOrder(orderitem);
        }

        void menu_newOrder_Click(object sender, EventArgs e)
        {
            DoNewOrder();
        }

        // ����(�滮)
        void menu_design_Click(object sender, EventArgs e)
        {
            DoDesignOrder();
        }

        // ����
        void menu_arrive_Click(object sender, EventArgs e)
        {
            DoAccept();
        }

        // ����ɾ��һ��������������
        void menu_undoDeleteOrder_Click(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "��δѡ��Ҫ����ɾ��������");
                return;
            }

            this.EnableControls(false);

            try
            {
                bool bOldChanged = this.Items.Changed;

                // ʵ��Undo
                List<ListViewItem> selectedItems = new List<ListViewItem>();
                foreach (ListViewItem item in this.listView.SelectedItems)
                {
                    selectedItems.Add(item);
                }

                string strNotUndoList = "";
                int nUndoCount = 0;
                foreach (ListViewItem item in selectedItems)
                {
                    OrderItem orderitem = (OrderItem)item.Tag;

                    bool bRet = this.Items.UndoMaskDeleteItem(orderitem);

                    if (bRet == false)
                    {
                        if (strNotUndoList != "")
                            strNotUndoList += ",";
                        strNotUndoList += orderitem.Index;
                        continue;
                    }

                    nUndoCount++;
                }

                string strText = "";

                if (strNotUndoList != "")
                    strText += "���Ϊ '" + strNotUndoList + "' ��������ǰ��δ�����ɾ����, ��������̸���ϳ���ɾ����\r\n\r\n";

                strText += "������ɾ�� " + nUndoCount.ToString() + " �";
                MessageBox.Show(ForegroundWindow.Instance, strText);

#if NO
                if (this.ContentChanged != null
    && bOldChanged != this.Items.Changed)
                {
                    ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                    e1.OldChanged = bOldChanged;
                    e1.CurrentChanged = this.Items.Changed;
                    this.ContentChanged(this, e1);
                }
#endif
                TriggerContentChanged(bOldChanged, this.Items.Changed);

            }
            finally
            {
                this.EnableControls(true);
            }
        }

        // ɾ��һ��������������
        void menu_deleteOrder_Click(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "��δѡ��Ҫ���ɾ��������");
                return;
            }

            string strIndexList = "";
            for (int i = 0; i < this.listView.SelectedItems.Count; i++)
            {
                if (i > 20)
                {
                    strIndexList += "...(�� " + this.listView.SelectedItems.Count.ToString() + " ��)";
                    break;
                }
                string strIndex = this.listView.SelectedItems[i].Text;
                strIndexList += strIndex + "\r\n";
            }

            string strWarningText = "����(���)������������ɾ��: \r\n" + strIndexList + "\r\n\r\nȷʵҪ���ɾ������?";

            // ����
            DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
                strWarningText,
                "OrderControl",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result == DialogResult.Cancel)
                return;

            List<string> deleted_recpaths = new List<string>();

            this.EnableControls(false);

            try
            {
                bool bOldChanged = this.Items.Changed;

                // ʵ��ɾ��
                List<ListViewItem> selectedItems = new List<ListViewItem>();
                foreach (ListViewItem item in this.listView.SelectedItems)
                {
                    selectedItems.Add(item);
                }

                string strNotDeleteList = "";
                int nDeleteCount = 0;
                foreach (ListViewItem item in selectedItems)
                {
                    OrderItem orderitem = (OrderItem)item.Tag;

                    int nRet = MaskDeleteItem(orderitem,
                        m_bRemoveDeletedItem);

                    if (nRet == 0)
                    {
                        if (strNotDeleteList != "")
                            strNotDeleteList += ",";
                        strNotDeleteList += orderitem.Index;
                        continue;
                    }

                    if (string.IsNullOrEmpty(orderitem.RecPath) == false)
                        deleted_recpaths.Add(orderitem.RecPath);

                    nDeleteCount++;
                }

                string strText = "";

                if (strNotDeleteList != "")
                    strText += "���Ϊ '" + strNotDeleteList + "' �Ķ�������δ�ܼ��Ա��ɾ����\r\n\r\n";

                if (deleted_recpaths.Count == 0)
                    strText += "��ֱ��ɾ�� " + nDeleteCount.ToString() + " �";
                else if (nDeleteCount - deleted_recpaths.Count == 0)
                    strText += "�����ɾ�� "
                        + deleted_recpaths.Count.ToString()
                        + " �\r\n\r\n(ע�������ɾ�������Ҫ�����ύ����Ż������ӷ�����ɾ��)";
                else
                    strText += "�����ɾ�� "
    + deleted_recpaths.Count.ToString()
    + " �ֱ��ɾ�� "
    + (nDeleteCount - deleted_recpaths.Count).ToString()
    + " �\r\n\r\n(ע�������ɾ�������Ҫ�����ύ����Ż������ӷ�����ɾ��)";

                MessageBox.Show(ForegroundWindow.Instance, strText);

#if NO
                if (this.ContentChanged != null
                    && bOldChanged != this.Items.Changed)
                {
                    ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                    e1.OldChanged = bOldChanged;
                    e1.CurrentChanged = this.Items.Changed;
                    this.ContentChanged(this, e1);
                }
#endif
                TriggerContentChanged(bOldChanged, this.Items.Changed);


            }
            finally
            {
                this.EnableControls(true);
            }
        }

#if NO
        // ���ɾ������
        // return:
        //      0   ��Ϊ�в���Ϣ��δ�ܱ��ɾ��
        //      1   �ɹ�ɾ��
        int MaskDeleteItem(OrderItem orderitem,
            bool bRemoveDeletedItem)
        {
            this.Items.MaskDeleteItem(bRemoveDeletedItem,
                orderitem);
            return 1;
        }
#endif


        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            menu_modifyOrder_Click(this, null);
        }

#if NO
        void EnableControls(bool bEnable)
        {
            if (this.EnableControlsEvent == null)
                return;

            EnableControlsEventArgs e = new EnableControlsEventArgs();
            e.bEnable = bEnable;
            this.EnableControlsEvent(this, e);
        }
#endif

        private void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int nClickColumn = e.Column;
            this.SortColumns.SetFirstColumn(nClickColumn,
                this.listView.Columns);

            // ����
            this.listView.ListViewItemSorter = new SortColumnsComparer(this.SortColumns);

            this.listView.ListViewItemSorter = null;
        }

#if NO
        // 2009/11/23
        // ���ݶ�����¼·�� ������ ��Ŀ��¼ ��ȫ������������¼��װ�봰��
        // parameters:
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        public int DoSearchOrderByRecPath(string strOrderRecPath)
        {
            int nRet = 0;
            string strError = "";
            // �ȼ���Ƿ����ڱ�������?

            // �Ե�ǰ�����ڽ��в��¼·������
            if (this.Items != null)
            {
                OrderItem dupitem = this.Items.GetItemByRecPath(strOrderRecPath) as OrderItem;
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "������¼ '" + strOrderRecPath + "' ����Ϊ������δ�ύ֮һɾ����������";
                    else
                        strText = "������¼ '" + strOrderRecPath + "' �ڱ������ҵ���";

                    dupitem.HilightListViewItem(true);

                    MessageBox.Show(ForegroundWindow.Instance, strText);
                    return 1;
                }
            }

            // ��������ύ��������
            string strBiblioRecPath = "";


            // ���ݶ�����¼·�����������������������Ŀ��¼·����
            nRet = SearchBiblioRecPath(strOrderRecPath,
                out strBiblioRecPath,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(ForegroundWindow.Instance, "�Զ�����¼·�� '" + strOrderRecPath + "' ���м����Ĺ����з�������: " + strError);
                return -1;
            }
            else if (nRet == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "û���ҵ�·��Ϊ '" + strOrderRecPath + "' �Ķ�����¼��");
                return 0;
            }
            else if (nRet == 1)
            {
                Debug.Assert(strBiblioRecPath != "", "");
                this.TriggerLoadRecord(strBiblioRecPath);

                // ѡ�϶�������
                OrderItem result_item = HilightLineByItemRecPath(strOrderRecPath, true);
                return 1;
            }
            else if (nRet > 1) // ���з����ظ�
            {
                Debug.Assert(false, "�ö�����¼·���������Բ��ᷢ���ظ�����");
            }

            return 0;
        }
#endif

#if NO
        // ���ݶ�����¼·�������������������Ŀ��¼·����
        int SearchBiblioRecPath(string strOrderRecPath,
            out string strBiblioRecPath,
            out string strError)
        {
            strError = "";
            strBiblioRecPath = "";

            string strItemText = "";
            string strBiblioText = "";

            byte[] item_timestamp = null;

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڼ���������¼ '" + strOrderRecPath + "' ����������Ŀ��¼·�� ...");
            Stop.BeginLoop();

            try
            {
                string strIndex = "@path:" + strOrderRecPath;
                string strOutputItemRecPath = "";

                long lRet = Channel.GetOrderInfo(
                    Stop,
                    strIndex,
                    // "", // strBiblioRecPath,
                    null,
                    out strItemText,
                    out strOutputItemRecPath,
                    out item_timestamp,
                    "recpath",
                    out strBiblioText,
                    out strBiblioRecPath,
                    out strError);
                if (lRet == -1)
                    return -1;  // error

                return (int)lRet;   // not found
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");
            }
        }

#endif

#if NO
        // return:
        //      -1  �����Ѿ���MessageBox����
        //      0   û��װ��
        //      1   �ɹ�װ��
        public int DoLoadRecord(string strBiblioRecPath)
        {
            if (this.LoadRecord == null)
                return 0;

            LoadRecordEventArgs e = new LoadRecordEventArgs();
            e.BiblioRecPath = strBiblioRecPath;
            this.LoadRecord(this, e);
            return e.Result;
        }
#endif
    }

    // ����998$tĿ���¼·��
    /// <summary>
    /// ����Ŀ���¼·���¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void SetTargetRecPathEventHandler(object sender,
        SetTargetRecPathEventArgs e);

    /// <summary>
    /// ����Ŀ���¼·���¼��Ĳ���
    /// </summary>
    public class SetTargetRecPathEventArgs : EventArgs
    {
        /// <summary>
        /// [in] Ŀ���¼·��
        /// </summary>
        public string TargetRecPath = "";    // [in] Ŀ���¼·��

        /// <summary>
        /// [out] ���س�����Ϣ�����Ϊ�ǿգ���ʾִ�й��̳���
        /// </summary>
        public string ErrorInfo = "";   // [out] ���Ϊ�ǿգ���ʾִ�й��̳��������ǳ�����Ϣ
    }

    // ����ʵ��(��)����
    /// <summary>
    /// ����ʵ�������¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void GenerateEntityEventHandler(object sender,
        GenerateEntityEventArgs e);

    /// <summary>
    /// ����ʵ�������¼��Ĳ���
    /// </summary>
    public class GenerateEntityEventArgs : EventArgs
    {
        // 2009/11/5
        /// <summary>
        /// [in] ��Ŀ��¼��һ������������Դ��Ŀ���ݡ����Ϊ�գ���ʾֱ������Դ����Ŀ�����Ŀ��¼
        /// </summary>
        public string BiblioRecord = "";    // [in] ��Ŀ��¼��һ������������Դ��Ŀ���ݡ����Ϊ�գ���ʾֱ������Դ����Ŀ�����Ŀ��¼

        /// <summary>
        /// [in] ��Ŀ��¼�ĸ�ʽ��Ϊ unimarc usmarc xml ֮һ
        /// </summary>
        public string BiblioSyntax = "";    // [in] ��Ŀ��¼�ĸ�ʽ unimarc usmarc xml

        /// <summary>
        /// [in] �Ƿ�Ϊ�ڿ�ģʽ
        /// </summary>
        public bool SeriesMode = false; // [in] �Ƿ�Ϊ�ڿ�ģʽ

        /// <summary>
        /// [in] �Ƿ���Ҫ��������������
        /// </summary>
        public bool InputItemBarcode = true;

        /// <summary>
        /// [in] �Ƿ�Ϊ�´����Ĳ��¼���á��ӹ��С�״̬
        /// </summary>
        public bool SetProcessingState = true;

        /// <summary>
        /// [in] �Ƿ�Ϊ�´����Ĳ��¼������ȡ��
        /// </summary>
        public bool CreateCallNumber = false;   // [in] �Ƿ񴴽���ȡ�� 2012/5/7

        /// <summary>
        /// [in] �����ݼ���
        /// </summary>
        public List<GenerateEntityData> DataList = new List<GenerateEntityData>();

        /// <summary>
        /// [out] ���س�����Ϣ
        /// </summary>
        public string ErrorInfo = "";   // [out] ���Ϊ�ǿգ���ʾִ�й��̳��������ǳ�����Ϣ

        // 2009/11/8
        /// <summary>
        /// [out] �����´����ġ�����ֱ�����õ�Ŀ���¼·��
        /// </summary>
        public string TargetRecPath = "";   // [out] �´����ġ�����ֱ�����õ�Ŀ���¼·��
    }

    // һ�����ݴ洢��Ԫ
    /// <summary>
    /// ������ʱ���õ��Ĳ���Ϣ�洢�ṹ
    /// </summary>
    public class GenerateEntityData
    {
        /// <summary>
        /// ������Ϊ new/delete/change ֮һ
        /// </summary>
        public string Action = "";  // new/delete/change
        /// <summary>
        /// �ο�ID��������Ϣ��ϵ��һ��Ψһ��IDֵ
        /// </summary>
        public string RefID = "";   // �ο�ID��������Ϣ��ϵ��һ��Ψһ��IDֵ
        /// <summary>
        /// ���¼ XML
        /// </summary>
        public string Xml = ""; // ʵ���¼XML
        /// <summary>
        /// [out] ���س�����Ϣ
        /// </summary>
        public string ErrorInfo = "";   // [out]���Ϊ�ǿգ���ʾִ�й��̳��������ǳ�����Ϣ

        // 2010/12/1
        /// <summary>
        /// �������硰1/7��
        /// </summary>
        public string Sequence = "";    // �������硰1/7��
        /// <summary>
        /// ��ѡ�������۸񡣸�ʽΪ: "������:CNY12.00;���ռ�:CNY15.00"
        /// </summary>
        public string OtherPrices = ""; // ��ѡ�������۸񡣸�ʽΪ: "������:CNY12.00;���ռ�:CNY15.00"
    }


    // ѯ���Ƿ�������գ�
    /// <summary>
    /// ׼�������¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void PrepareAcceptEventHandler(object sender,
        PrepareAcceptEventArgs e);

    /// <summary>
    /// ׼�������¼��Ĳ���
    /// </summary>
    public class PrepareAcceptEventArgs : EventArgs
    {
        // 2009/11/8
        /// <summary>
        /// [in] ��Դ��¼��·��
        /// </summary>
        public string BiblioSourceRecPath = ""; // ��Դ��¼��·��
        /// <summary>
        /// [in] ��Դ��¼����Ŀ��¼
        /// </summary>
        public string BiblioSourceRecord = "";  // ��Դ��¼����Ŀ��¼
        /// <summary>
        /// [in] ��Դ��¼����Ŀ��ʽ��Ϊ unimarc usmarc xml ֮һ
        /// </summary>
        public string BiblioSourceSyntax = "";  // marc unimarc usmarc xml

        // 
        /// <summary>
        /// [in] Դ��¼·��
        /// </summary>
        public string SourceRecPath = "";   // Դ��¼·��

        // 
        /// <summary>
        /// [out] Ŀ���¼·��
        /// </summary>
        public string TargetRecPath = "";   // Ŀ���¼·��

        // 
        /// <summary>
        /// [out] �������յ����κ�
        /// </summary>
        public string AcceptBatchNo = "";   // �������յ����κ�

        // 
        /// <summary>
        /// [out] �Ƿ�������ĩ�Σ��Զ��������������������ŵĽ���?
        /// </summary>
        public bool InputItemsBarcode = true;   // �Ƿ�������ĩ�Σ��Զ��������������������ŵĽ���?

        // 
        /// <summary>
        /// [out] �Ƿ�Ϊ�´����Ĳ��¼���á��ӹ��С�״̬
        /// </summary>
        public bool SetProcessingState = true;    // �Ƿ�Ϊ�´����Ĳ��¼���á��ӹ��С�״̬ 2009/10/19

        // 
        /// <summary>
        /// [out] �Ƿ�Ϊ�´����Ĳ��¼������ȡ��
        /// </summary>
        public bool CreateCallNumber = true;    // �Ƿ�Ϊ�´����Ĳ��¼������ȡ�� 2012/5/7

        // 
        /// <summary>
        /// [out] Ϊ���¼�еļ۸��ֶ����ú��ּ۸�ֵ��ֵΪ ��Ŀ��/������/���ռ�/�հ� ֮һ
        /// </summary>
        public string PriceDefault = "���ռ�";  // Ϊ���¼�еļ۸��ֶ����ú��ּ۸�ֵ����Ŀ��/������/���ռ�/�հ�

        // 
        /// <summary>
        /// [out] ������Ϣ�����ԶԲ�����������棬���������ִ��Ҫ����ִ�У�Ҳ���ԡ�������Ҫ����Դ��Ŀ��title�����ϵ����
        /// </summary>
        public string WarningInfo = ""; // ������Ϣ�����ԶԲ�����������棬���������ִ��Ҫ����ִ�У�Ҳ���ԡ�������Ҫ����Դ��Ŀ��title�����ϵ����

        // 
        /// <summary>
        /// [out] ���ش�����Ϣ�����Ϊ�ǿգ���ʾִ�й��̳����������յ�����������
        /// </summary>
        public string ErrorInfo = "";   // [out]���Ϊ�ǿգ���ʾִ�й��̳����������յ����������㣬�����ǳ�����Ϣ

        // 
        /// <summary>
        /// [out] �Ƿ�Ҫ�������������Ϊtrue����ʾִ�й����������ԭ���� ErrorInfo �У�����ǰ������ʾ����
        /// </summary>
        public bool Cancel = false;     // [out]���Ϊtrue����ʾִ�й����������ԭ����ErrorInfo�У�����ǰ������ʾ����
    }

    /// <summary>
    /// ������Ŀ���¼�¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void OpenTargetRecordEventHandler(object sender,
        OpenTargetRecordEventArgs e);

    /// <summary>
    /// ������Ŀ���¼�¼��Ĳ���
    /// </summary>
    public class OpenTargetRecordEventArgs : EventArgs
    {
        // �ݲ�ʹ��
        // 
        /// <summary>
        /// [in] Դ��¼·��
        /// </summary>
        public string SourceRecPath = "";   // Դ��¼·��

        // 
        /// <summary>
        /// [in] Ŀ���¼·��
        /// </summary>
        public string TargetRecPath = "";   // Ŀ���¼·��

        // 
        /// <summary>
        /// [in] �������κ�
        /// </summary>
        public string BatchNo = "";   // �������κ�


        // 
        /// <summary>
        /// [out] ���ش�����Ϣ�����Ϊ�ǿգ���ʾִ�й��̳�����������������
        /// </summary>
        public string ErrorInfo = "";   // [out]���Ϊ�ǿգ���ʾִ�й��̳����������������㣬�����ǳ�����Ϣ

        // 
        /// <summary>
        /// [out] �Ƿ���Ҫ�������������Ϊ true����ʾִ�й����������ԭ���� ErrorInfo �У�����ǰ������ʾ����
        /// </summary>
        public bool Cancel = false;     // [out]���Ϊtrue����ʾִ�й����������ԭ����ErrorInfo�У�����ǰ������ʾ����
    }

    // 
    /// <summary>
    /// ����ָ���������κŵ�ʵ�����¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void HilightTargetItemsEventHandler(object sender,
        HilightTargetItemsEventArgs e);

    /// <summary>
    /// ����ָ���������κŵ�ʵ�����¼��Ĳ���
    /// </summary>
    public class HilightTargetItemsEventArgs : EventArgs
    {
        // 
        /// <summary>
        /// [in] �������κ�
        /// </summary>
        public string BatchNo = "";   // �������κ�


        // 
        /// <summary>
        /// [out] ���ش�����Ϣ�����Ϊ�ǿգ���ʾִ�й��̳�����������������
        /// </summary>
        public string ErrorInfo = "";   // [out]���Ϊ�ǿգ���ʾִ�й��̳����������������㣬�����ǳ�����Ϣ

        // 
        /// <summary>
        /// [out] �Ƿ���Ҫ�������������Ϊ true����ʾִ�й����������ԭ���� ErrorInfo ��
        /// </summary>
        public bool Cancel = false;     // [out]���Ϊtrue����ʾִ�й����������ԭ����ErrorInfo�У�����ǰ������ʾ����
    }

    // �����������д����ͼ���������ֹ���
    /// <summary>
    /// OrderControl ��Ļ�����
    /// </summary>
    public class OrderControlBase : ItemControlBase<OrderItem, OrderItemCollection>
    {
    }
}
