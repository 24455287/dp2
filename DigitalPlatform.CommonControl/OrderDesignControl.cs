using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using System.Globalization;
using System.Threading;

using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;

namespace DigitalPlatform.CommonControl
{
    /// <summary>
    /// ������Ϣ�����ϵ �ؼ�
    /// </summary>
    public partial class OrderDesignControl : UserControl
    {
        public bool CheckDupItem = true;    // �Ƿ��ڽ�����ʱ������Ԫ�顢��Ԫ��

        internal bool m_bFocused = false;

        bool m_bHideSelection = true;

        internal int DisableNewlyOrderTextChanged = 0;

        internal int DisableNewlyArriveTextChanged = 0;

        public Item LastClickItem = null;   // ���һ��clickѡ�����Item����

        // ��ȡֵ�б�ʱ��Ϊ���������ݿ���
        string m_strBiblioDbName = "";
        public string BiblioDbName
        {
            get
            {
                return this.m_strBiblioDbName;
            }
            set
            {
                this.m_strBiblioDbName = value;
                foreach (Item item in this.Items)
                {
                    item.location.DbName = value;
                }
            }
        }

        // ���ȱʡ��¼
        /// <summary>
        /// ���ȱʡ��¼
        /// </summary>
        public event GetDefaultRecordEventHandler GetDefaultRecord = null;

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        // 2012/10/4
        /// <summary>
        /// ���ݴ����Ƿ��ڹ�Ͻ��Χ��
        /// </summary>
        public event VerifyLibraryCodeEventHandler VerifyLibraryCode = null;

        // const int WM_NUMBER_CHANGED = API.WM_USER + 201;

        int m_nInSuspend = 0;

        // public int m_nTotalCopy = 0;

        public List<Item> Items = new List<Item>();

        bool m_bChanged = false;

        public OrderDesignControl()
        {
            InitializeComponent();
        }

        bool m_bSeriesMode = false;

        // �Ƿ�Ϊ�ڿ�ģʽ? true��ʾΪ�ڿ�ģʽ��false��ʾΪͼ��ģʽ
        [Category("Appearance")]
        [DescriptionAttribute("SeriesMode")]
        [DefaultValue(false)]
        public bool SeriesMode
        {
            get
            {
                return this.m_bSeriesMode;
            }
            set
            {
                if (this.m_bSeriesMode != value)
                {
                    this.m_bSeriesMode = value;

                    SetSeriesMode(value);
                }
            }
        }

        void SetSeriesMode(bool bSeriesMode)
        {
            this.DisableUpdate();

            for (int i = 0; i < this.Items.Count; i++)
            {
                Item item = this.Items[i];

                item.SeriesMode = bSeriesMode;
            }

            if (bSeriesMode == true)
            {
                this.label_range.Visible = true;
                this.label_range.Text = "ʱ�䷶Χ";
                this.label_issueCount.Visible = true;
            }
            else
            {
                // this.label_range.Visible = false;
                this.label_range.Visible = true;    // ????
                this.label_range.Text = "Ԥ�Ƴ���ʱ��";
                this.label_issueCount.Visible = false;
            }

            this.EnableUpdate();
        }

        bool m_bArriveMode = false;

        // �Ƿ�Ϊ����ģʽ? true��ʾΪ����ģʽ��false��ʾΪ����ģʽ
        [Category("Appearance")]
        [DescriptionAttribute("ArriveMode")]
        [DefaultValue(false)]
        public bool ArriveMode
        {
            get
            {
                return this.m_bArriveMode;
            }
            set
            {
                this.m_bArriveMode = value;

                SetArriveMode(value);
            }
        }

        void SetArriveMode(bool bArriveMode)
        {
            if (bArriveMode == true)
            {
                // ����̬

                /*
                this.label_orderedTotalCopy.Text = "�������ܸ�����(&O):";
                this.label_newlyOrderTotalCopy.Text = "�������ܸ�����(&N):";
                 * */
                this.label_copy.ForeColor = Color.Red;
                this.label_price.ForeColor = Color.Red;
                this.label_location.ForeColor = Color.Red;

                this.label_newlyOrderTotalCopy.Visible = false;
                this.textBox_newlyOrderTotalCopy.Visible = false;

                this.button_newItem.Visible = false;

                this.label_arrivedTotalCopy.Visible = true;
                this.textBox_arrivedTotalCopy.Visible = true;

                // 2008/11/3
                this.panel_targetRecPath.Visible = true;

                this.label_newlyArriveTotalCopy.Visible = true;
                this.textBox_newlyArriveTotalCopy.Visible = true;

                // 2008/11/3
                this.button_fullyAccept.Visible = true;
            }
            else
            {
                // false ��ʾ����̬

                /*
                this.label_orderedTotalCopy.Text = "�Ѷ����ܸ�����(&O):";
                this.label_newlyOrderTotalCopy.Text = "�¶����ܸ�����(&N):";
                 * */

                this.label_copy.ForeColor = this.ForeColor;
                this.label_price.ForeColor = this.ForeColor;
                this.label_location.ForeColor = this.ForeColor;


                this.label_newlyOrderTotalCopy.Visible = true;
                this.textBox_newlyOrderTotalCopy.Visible = true;

                this.button_newItem.Visible = true;

                this.label_arrivedTotalCopy.Visible = false;
                this.textBox_arrivedTotalCopy.Visible = false;

                // 2008/11/3
                this.panel_targetRecPath.Visible = false;

                this.label_newlyArriveTotalCopy.Visible = false;
                this.textBox_newlyArriveTotalCopy.Visible = false;

                // 2008/11/3
                this.button_fullyAccept.Visible = false;
            }
        }

        public void EnsureVisible(Item item)
        {
            int [] row_heights = this.tableLayoutPanel_content.GetRowHeights();
            int nYOffs = row_heights[0];
            int i = 1;
            foreach (Item cur_item in this.Items)
            {
                if (cur_item == item)
                    break;
                nYOffs += row_heights[i++];
            }

            // this.AutoScrollPosition = new Point(this.AutoScrollOffset.X, 1000);

            this.AutoScrollPosition = new Point(this.AutoScrollOffset.X, nYOffs);
        }

        bool m_bOrderedTotalCopyVisible = false;

        // �Ѷ������� �Ƿ�ɼ�?
        internal bool OrderedTotalCopyVisible
        {
            get
            {
                return this.m_bOrderedTotalCopyVisible;
            }
            set
            {
                if (this.m_bOrderedTotalCopyVisible != value)
                {
                    this.m_bOrderedTotalCopyVisible = value;

                    this.textBox_orderedTotalCopy.Visible = value;
                    this.label_orderedTotalCopy.Visible = value;
                }
            }
        }

        bool m_bArrivedTotalCopyVisible = false;

        // �����շ��� �Ƿ�ɼ�?
        internal bool ArrivedTotalCopyVisible
        {
            get
            {
                return this.m_bArrivedTotalCopyVisible;
            }
            set
            {
                if (this.m_bArrivedTotalCopyVisible != value)
                {
                    this.m_bArrivedTotalCopyVisible = value;

                    this.textBox_arrivedTotalCopy.Visible = value;
                    this.label_arrivedTotalCopy.Visible = value;
                }
            }
        }

        // ����Ŀ���¼·��
        public string TargetRecPath
        {
            get
            {
                return this.textBox_targetRecPath.Text;
            }
            set
            {
                this.textBox_targetRecPath.Text = value;
            }
        }

        [Category("Appearance")]
        [DescriptionAttribute("HideSelection")]
        [DefaultValue(true)]
        public bool HideSelection
        {
            get
            {
                return this.m_bHideSelection;
            }
            set
            {
                if (this.m_bHideSelection != value)
                {
                    this.m_bHideSelection = value;
                    this.RefreshLineColor(); // ��ʹ��ɫ�ı�
                }
            }
        }

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        [Category("Content")]
        [DescriptionAttribute("Changed")]
        [DefaultValue(false)]
        public bool Changed
        {
            get
            {
                return this.m_bChanged;
            }
            set
            {

                if (this.m_bChanged != value)
                {
                    this.m_bChanged = value;

                    if (value == false)
                        ResetLineState();
                }
            }
        }


        // �޸ĸ����ַ����е���������
        // parameters:
        //      strText     ���޸ĵ����������ַ���
        //      strCopy     Ҫ�ĳɵ���������
        // return:
        //      �����޸ĺ�����������ַ���
        public static string ModifyCopy(string strText, string strCopy)
        {
            int nRet = strText.IndexOf("*");
            if (nRet == -1)
                return strCopy;

            return strCopy + "*" + strText.Substring(nRet + 1).Trim();
        }

        // �޸ĸ����ַ����е����ڲ�������
        // parameters:
        //      strText     ���޸ĵ����������ַ���
        //      strRightCopy     Ҫ�ĳɵ����ڲ�������
        // return:
        //      �����޸ĺ�����������ַ���
        public static string ModifyRightCopy(string strText, string strRightCopy)
        {
            int nRet = strText.IndexOf("*");
            if (nRet == -1)
                return strText + "*" + strRightCopy;

            return strText.Substring(0, nRet).Trim() + "*" + strRightCopy;
        }

        // �Ӹ������ַ����еõ���������
        // Ҳ���� "3*5"����"3"���֡����ֻ��һ�����֣���ȡ��
        public static string GetCopyFromCopyString(string strText)
        {
            int nRet = strText.IndexOf("*");
            if (nRet == -1)
                return strText;

            return strText.Substring(0, nRet).Trim();
        }

        // �Ӹ������ַ����еõ����ڲ�������
        // Ҳ���� "3*5"����"5"���֡����ֻ��һ�����֣��ͷ���""
        public static string GetRightFromCopyString(string strText)
        {
            int nRet = strText.IndexOf("*");
            if (nRet == -1)
                return "";

            return strText.Substring(nRet + 1).Trim();
        }


        // return:
        //      -1  error
        //      0   succeed
        static int VerifyDateRange(string strValue,
            out string strError)
        {
            strError = "";

            string strStart = "";
            string strEnd = "";

            int nRet = strValue.IndexOf("-");
            if (nRet == -1)
            {
                strStart = strValue;
                if (strStart.Length != 8)
                {
                    strError = "ʱ�䷶Χ '" + strValue + "' ��ʽ����ȷ";
                    return -1;
                }

                strEnd = "";
            }
            else
            {
                strStart = strValue.Substring(0, nRet).Trim();

                if (strStart.Length != 8)
                {
                    strError = "ʱ�䷶Χ '" + strValue + "' �� '" + strStart + "' ��ʽ����ȷ";
                    return -1;
                }

                strEnd = strValue.Substring(nRet + 1).Trim();

                if (strStart.Length != 8)
                {
                    strError = "ʱ�䷶Χ '" + strValue + "' �� '" + strEnd + "' ��ʽ����ȷ";
                    return -1;
                }
            }

            if (String.Compare(strStart, strEnd) > 0)
            {
                strError = "ʱ�䷶Χ�ڵ���ʼʱ�䲻Ӧ���ڽ���ʱ��";
                return -1;
            }

            return 0;
        }

        // ���м��
        // return:
        //      -1  �������г���
        //      0   ���û�з��ִ���
        //      1   ��鷢���˴���
        public int Check(out string strError)
        {
            strError = "";
            int nRet = 0;

            bool bStrict = true;    // �Ƿ��ϸ���

            // ����Ƿ�ÿ�ж������˼۸񡢷���
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item item = this.Items[i];

                // ֻ����¹滮������
                if ((item.State & ItemState.ReadOnly) != 0)
                    continue;
                // ����δ���޸Ĺ�������
                if ((item.State & ItemState.New) == 0
                    && (item.State & ItemState.Changed) == 0)
                    continue;

                // ���м��
                // return:
                //      -1  �������г���
                //      0   ���û�з��ִ���
                //      1   ��鷢���˴���
                nRet = item.location.Check(out strError);
                if (nRet != 0)
                {
                    strError = "�� " + (i + 1).ToString() + " ��: ȥ�� ��ʽ������: " + strError;
                    return 1;
                }

                // 2009/11/9
                string strTotalPrice = "";

                try
                {
                    strTotalPrice = item.TotalPrice;
                }
                catch (Exception ex)
                {
                    strError = "��ȡitem.TotalPriceʱ����: " + ex.Message;
                    return -1;
                }

                if (String.IsNullOrEmpty(strTotalPrice) == true)
                {
                    if (String.IsNullOrEmpty(item.Price) == true)
                    {
                        strError = "�� " + (i + 1).ToString() + " ��: ��δ����۸�";
                        return 1;
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(item.StateString) == true
                        && String.IsNullOrEmpty(item.Price) == false)
                    {
                        strError = "�� " + (i + 1).ToString() + " ��: �������˼۸� ('"+item.Price+"') ʱ��������ܼ۸�����Ϊ�� (������Ϊ '"+strTotalPrice+"')";
                        return 1;
                    }
                }

                if (this.ArriveMode == false)   // 2009/2/4
                {
                    // ����ģʽ
                    if (String.IsNullOrEmpty(item.CopyString) == true)
                    {
                        strError = "�� " + (i + 1).ToString() + " ��: ��δ���븴����";
                        return 1;
                    }
                }
                else
                {
                    // ����ģʽ

                    // ��һ��ÿһ�ж�Ҫ����

                    // TODO: �Ƿ���һ��������һ�������ˣ���̫�ü�顣

                }

                if (this.SeriesMode == true)
                {
                    if (String.IsNullOrEmpty(item.RangeString) == true)
                    {
                        strError = "�� " + (i + 1).ToString() + " ��: ��δ����ʱ�䷶Χ";
                        return 1;
                    }

                    if (item.RangeString.Length != (2*8 + 1))
                    {
                        strError = "�� " + (i + 1).ToString() + " ��: ��δ����������ʱ�䷶Χ";
                        return 1;
                    }

                    // return:
                    //      -1  error
                    //      0   succeed
                    nRet = VerifyDateRange(item.RangeString,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "�� " + (i + 1).ToString() + " ��: " + strError;
                        return 1;
                    }

                    if (String.IsNullOrEmpty(item.IssueCountString) == true)
                    {
                        strError = "�� " + (i + 1).ToString() + " ��: ��δ��������";
                        return 1;
                    }


                }


                if (bStrict == true)
                {
                    if (String.IsNullOrEmpty(item.Source) == true
                        && item.Seller != "����" && item.Seller != "��")
                    {
                        strError = "�� " + (i + 1).ToString() + " ��: ��δ���뾭����Դ";
                        return 1;
                    }

                    // 2009/2/15
                    if (item.Seller == "����" || item.Seller == "��")
                    {
                        if (String.IsNullOrEmpty(item.Source) == false)
                        {
                            strError = "�� " + (i + 1).ToString() + " ��: �������Ϊ ���� �� �����򾭷���Դ����Ϊ��";
                            return 1;
                        }
                    }

                    if (String.IsNullOrEmpty(item.Seller) == true)
                    {
                        strError = "�� " + (i + 1).ToString() + " ��: ��δ��������";
                        return 1;
                    }
                    /*
                    if (String.IsNullOrEmpty(item.CatalogNo) == true)
                    {
                        strError = "�� " + (i + 1).ToString() + " ��: ��δ������Ŀ��";
                        return 1;
                    }
                     * */
                    if (String.IsNullOrEmpty(item.Class) == true)
                    {
                        strError = "�� " + (i + 1).ToString() + " ��: ��δ�������";
                        return 1;
                    }
                }
            }

            if (bStrict == true)
            {
                // ��� ���� + ������Դ + �۸� 3Ԫ���Ƿ����ظ�
                for (int i = 0; i < this.Items.Count; i++)
                {
                    Item item = this.Items[i];

                    // ֻ����¹滮������
                    if ((item.State & ItemState.ReadOnly) != 0)
                        continue;


                    // 2009/2/4 ֻ���������Ķ�������
                    if (String.IsNullOrEmpty(item.StateString) == false)
                        continue;

                    string strLocationString = item.location.Value;
                    LocationCollection locations = new LocationCollection();
                    nRet = locations.Build(strLocationString, out strError);
                    if (nRet == -1)
                    {
                        strError = "�� " + (i + 1).ToString() + " ��: ȥ���ַ��� '"+strLocationString+"' ��ʽ����: " + strError;
                        return -1;
                    }
                    string strUsedLibraryCodes = StringUtil.MakePathList(locations.GetUsedLibraryCodes());

                    // ���ݴ����Ƿ��ڹ�Ͻ��Χ��
                    // ֻ����޸Ĺ�������
                    if (IsChangedItem(item) == true
                        && this.VerifyLibraryCode != null)
                    {
                        VerifyLibraryCodeEventArgs e = new VerifyLibraryCodeEventArgs();
                        e.LibraryCode = strUsedLibraryCodes;
                        this.VerifyLibraryCode(this, e);
                        if (string.IsNullOrEmpty(e.ErrorInfo) == false)
                        {
                            strError = "�� " + (i + 1).ToString() + " ��: ȥ�����: " + e.ErrorInfo;
                            return -1;
                        }
                    }

                    for (int j = i + 1; j < this.Items.Count; j++)
                    {
                        Item temp_item = this.Items[j];

                        // ֻ����¹滮������
                        if ((temp_item.State & ItemState.ReadOnly) != 0)
                            continue;
                        // ����δ���޸Ĺ�������
                        if (IsChangedItem(temp_item) == false)
                            continue;

                        // 2009/2/4 ֻ���������Ķ�������
                        if (String.IsNullOrEmpty(temp_item.StateString) == false)
                            continue;

                        string strTempLocationString = temp_item.location.Value;
                        LocationCollection temp_locations = new LocationCollection();
                        nRet = temp_locations.Build(strTempLocationString, out strError);
                        if (nRet == -1)
                        {
                            strError = "�� " + (j + 1).ToString() + " ��: ȥ���ַ��� '" + strTempLocationString + "' ��ʽ����: " + strError;
                            return -1;
                        }
                        string strTempUsedLibraryCodes = StringUtil.MakePathList(temp_locations.GetUsedLibraryCodes());

                        if (this.CheckDupItem == true)
                        {
                            if (this.SeriesMode == false)
                            {
                                // ��ͼ������Ԫ��
                                if (item.Seller == temp_item.Seller
                                    && item.Source == temp_item.Source
                                    && item.Price == temp_item.Price
                                    && strUsedLibraryCodes == strTempUsedLibraryCodes)
                                {
                                    strError = "�� " + (i + 1).ToString() + " �� �� �� " + (j + 1) + " ��֮�� ����/������Դ/�۸�/ȥ��(�������Ĺݴ���) ��Ԫ���ظ�����Ҫ�����Ǻϲ�Ϊһ��";
                                    return 1;
                                }
                            }
                            else
                            {
                                // ���ڿ������Ԫ��
                                if (item.Seller == temp_item.Seller
                                    && item.Source == temp_item.Source
                                    && item.Price == temp_item.Price
                                    && item.RangeString == temp_item.RangeString
                                    && strUsedLibraryCodes == strTempUsedLibraryCodes)
                                {
                                    strError = "�� " + (i + 1).ToString() + " �� �� �� " + (j + 1) + " ��֮�� ����/������Դ/ʱ�䷶Χ/�۸�/ȥ��(�������Ĺݴ���) ��Ԫ���ظ�����Ҫ�����Ǻϲ�Ϊһ��";
                                    return 1;
                                }
                            }
                        }

                    }
                }
            }

            return 0;
        }

        static bool IsChangedItem(Item item)
        {
            if ((item.State & ItemState.Changed) != 0
                || (item.State & ItemState.New) != 0)
                return true;
            return false;
        }

        // ����ܷ����������������¹滮�ĺ��Ѷ���(������)������
        public int GetTotalCopy()
        {
            int value = 0;
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item cur_element = this.Items[i];
                value += cur_element.CopyValue;
            }

            return value;
        }

        // ����¹滮���ܷ������������Ѷ���(������)������
        public int GetNewlyOrderTotalCopy()
        {
            Debug.Assert(this.ArriveMode == false, "������ֻ���ڶ���״̬��ʹ��");
            int value = 0;
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item cur_element = this.Items[i];

                if ((cur_element.State & ItemState.ReadOnly) != 0)
                    continue;

                value += cur_element.CopyValue;
            }

            return value;
        }

        // ��������յ��ܷ�����������δ���������������(��ʾΪֻ����)����������
        public int GetNewlyArriveTotalCopy()
        {
            Debug.Assert(this.ArriveMode == true, "������ֻ��������״̬��ʹ��");
            int value = 0;
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item cur_element = this.Items[i];

                value += cur_element.location.ArrivedCount - cur_element.location.ReadOnlyArrivedCount;
            }

            return value;
        }

        // ����Ƿ���������δ����(״̬Ϊ�գ���ʾ�ո������˲ɹ�����)
        // return:
        //      -1  error
        //      0   û�д���δ����״̬������
        //      1   �в��ִ���δ����״̬������
        //      2   ȫ�������δ����״̬
        public int NotOrdering(out string strMessage)
        {
            strMessage = "";
            int nNotOrderItemCount = 0;
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item cur_element = this.Items[i];

                if (cur_element.StateString == "")
                {
                    Debug.Assert(cur_element.location.ReadOnly == true, "");
                    nNotOrderItemCount++;
                }
            }

            if (nNotOrderItemCount == this.Items.Count)
            {
                strMessage = "ȫ�� " + this.Items.Count.ToString() + " ���������δ����״̬";
                return 2;
            }

            if (nNotOrderItemCount > 0)
            {
                strMessage = "ȫ�� " + this.Items.Count + " ���������� "+nNotOrderItemCount.ToString()+" �������δ����״̬";
                return 1;
            }

            strMessage = "û�������δ����״̬";
            return 0;
        }

        // ��ÿ������յ�����ܷ����������˱���������ǰ�Ѿ������յķ�����
        public int GetNewlyArrivingTotalCopy()
        {
            Debug.Assert(this.ArriveMode == true, "������ֻ��������״̬��ʹ��");
            int value = 0;
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item cur_element = this.Items[i];

                // Ҫ����item�����״̬�ǲ�����ȫ���������� 2008/11/12
                if (cur_element.location.ReadOnly == true)
                    continue;

                value += cur_element.location.Count - cur_element.location.ReadOnlyArrivedCount;
            }

            return value;
        }

        // ����Ѷ���(������)���ܷ������������¹滮������
        public int GetOrderedTotalCopy()
        {
            Debug.Assert(this.ArriveMode == false, "������ֻ���ڶ���״̬��ʹ��");

            int value = 0;
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item cur_element = this.Items[i];

                if ((cur_element.State & ItemState.ReadOnly) == 0)
                    continue;

                value += cur_element.CopyValue;
            }

            return value;
        }

        // ��������յ��ܷ������������¹滮������
        public int GetArrivedTotalCopy()
        {
            Debug.Assert(this.ArriveMode == true, "������ֻ��������״̬��ʹ��");
            int value = 0;
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item cur_element = this.Items[i];

                value += cur_element.location.ReadOnlyArrivedCount;
                /*
                if ((cur_element.State & ItemState.ReadOnly) == 0)
                    continue;

                value += cur_element.CopyValue;
                 * */
            }

            return value;
        }

        public void SelectAll()
        {
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item cur_element = this.Items[i];
                if ((cur_element.State & ItemState.Selected) == 0)
                    cur_element.State |= ItemState.Selected;
            }

            this.Invalidate();
        }

        public void SelectItem(Item element,
            bool bClearOld)
        {

            if (bClearOld == true)
            {
                for (int i = 0; i < this.Items.Count; i++)
                {
                    Item cur_element = this.Items[i];

                    if (cur_element == element)
                        continue;   // ��ʱ������ǰ��

                    if ((cur_element.State & ItemState.Selected) != 0)
                    {
                        cur_element.State -= ItemState.Selected;

                        this.InvalidateLine(cur_element);
                    }
                }
            }

            // ѡ�е�ǰ��
            if ((element.State & ItemState.Selected) == 0)
            {
                element.State |= ItemState.Selected;

                this.InvalidateLine(element);
            }

            this.LastClickItem = element;
        }

        public void ToggleSelectItem(Item element)
        {
            // ѡ�е�ǰ��
            if ((element.State & ItemState.Selected) == 0)
                element.State |= ItemState.Selected;
            else
                element.State -= ItemState.Selected;

            this.InvalidateLine(element);

            this.LastClickItem = element;
        }

        public void RangeSelectItem(Item element)
        {
            Item start = this.LastClickItem;

            int nStart = this.Items.IndexOf(start);
            if (nStart == -1)
                return;

            int nEnd = this.Items.IndexOf(element);

            if (nStart > nEnd)
            {
                // ����
                int nTemp = nStart;
                nStart = nEnd;
                nEnd = nTemp;
            }

            for (int i = nStart; i <= nEnd; i++)
            {
                Item cur_element = this.Items[i];

                if ((cur_element.State & ItemState.Selected) == 0)
                {
                    cur_element.State |= ItemState.Selected;

                    this.InvalidateLine(cur_element);
                }
            }

            // �������λ��
            for (int i = 0; i < nStart; i++)
            {
                Item cur_element = this.Items[i];

                if ((cur_element.State & ItemState.Selected) != 0)
                {
                    cur_element.State -= ItemState.Selected;

                    this.InvalidateLine(cur_element);
                }
            }

            for (int i = nEnd + 1; i < this.Items.Count; i++)
            {
                Item cur_element = this.Items[i];

                if ((cur_element.State & ItemState.Selected) != 0)
                {
                    cur_element.State -= ItemState.Selected;

                    this.InvalidateLine(cur_element);
                }
            }
        }


        public bool HasGetValueTable()
        {
            if (this.GetValueTable != null)
                return true;

            return false;
        }

        public void OnGetValueTable(object sender, GetValueTableEventArgs e)
        {
            if (this.GetValueTable != null)
                this.GetValueTable(sender, e);
        }

        // ���һ��δ��ʹ�õ�index�ַ���
        // paremeters:
        //      exclude ���Ĺ����У��ų�����������Ҫ������(�����ų��κ�����)����ʹ��ֵnull
        string GetNewIndex(Item exclude)
        {
            for (int j = 1; ; j++)
            {
                string strIndex = j.ToString();

                bool bFound = false;
                for (int i = 0; i < this.Items.Count; i++)
                {
                    Item item = this.Items[i];

                    if (item == exclude)
                        continue;

                    if (item.Index == strIndex)
                    {
                        bFound = true;
                        break;
                    }
                }

                if (bFound == false)
                    return strIndex;
            }
        }

        // ��ȫ���е�״̬�ָ�Ϊ��ͨ״̬
        // �������Ա�����Ordered״̬
        void ResetLineState()
        {
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item item = this.Items[i];

                item.location.Changed = false;  // 2014/8/29

                if ((item.State & ItemState.ReadOnly) != 0)
                    item.State = ItemState.Normal | ItemState.ReadOnly;
                else
                    item.State = ItemState.Normal;
            }

            this.Invalidate();
        }

        void RefreshLineColor()
        {
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item item = this.Items[i];
                item.SetLineColor();
            }
        }

        // �¹滮�������ܷ���
        // ���� �����յ��ܷ���
        public int NewlyOrderTotalCopy
        {
            get
            {
                if (String.IsNullOrEmpty(this.textBox_newlyOrderTotalCopy.Text) == true)
                    return 0;

                return Convert.ToInt32(this.textBox_newlyOrderTotalCopy.Text);
            }
            set
            {
                this.textBox_newlyOrderTotalCopy.Text = value.ToString();
            }

        }

        public void Clear()
        {
            this.DisableUpdate();

            try
            {

                for (int i = 0; i < this.Items.Count; i++)
                {
                    Item element = this.Items[i];
                    ClearOneItemControls(this.tableLayoutPanel_content,
                        element);
                }

                this.Items.Clear();
                this.tableLayoutPanel_content.RowCount = 2;    // Ϊʲô��2��
                for (; ; )
                {
                    if (this.tableLayoutPanel_content.RowStyles.Count <= 2)
                        break;
                    this.tableLayoutPanel_content.RowStyles.RemoveAt(2);
                }

                // 2008/12/30
                this.textBox_arrivedTotalCopy.Text = "";
                this.textBox_newlyArriveTotalCopy.Text = "";
                this.textBox_newlyOrderTotalCopy.Text = "";
                this.textBox_orderedTotalCopy.Text = "";
            }
            finally
            {
                this.EnableUpdate();
            }
        }


        // ���һ��Item�����Ӧ��Control
        public void ClearOneItemControls(
            TableLayoutPanel table,
            Item line)
        {
            // color
            Label label = line.label_color;
            table.Controls.Remove(label);

            // catalog no
            table.Controls.Remove(line.textBox_catalogNo);

            // seller
            table.Controls.Remove(line.comboBox_seller);

            // source
            table.Controls.Remove(line.comboBox_source);

            // range
            table.Controls.Remove(line.dateRange_range);

            // issue count
            table.Controls.Remove(line.comboBox_issueCount);

            // copy
            table.Controls.Remove(line.comboBox_copy);

            // price
            table.Controls.Remove(line.textBox_price);

            // location
            table.Controls.Remove(line.location);

            // class
            table.Controls.Remove(line.comboBox_class);

            // seller address
            table.Controls.Remove(line.label_sellerAddress);

            // other
            table.Controls.Remove(line.label_other);
        }

        public List<Item> SelectedItems
        {
            get
            {
                List<Item> results = new List<Item>();

                for (int i = 0; i < this.Items.Count; i++)
                {
                    Item cur_element = this.Items[i];
                    if ((cur_element.State & ItemState.Selected) != 0)
                        results.Add(cur_element);
                }

                return results;
            }
        }

        public List<int> SelectedIndices
        {
            get
            {
                List<int> results = new List<int>();

                for (int i = 0; i < this.Items.Count; i++)
                {
                    Item cur_element = this.Items[i];
                    if ((cur_element.State & ItemState.Selected) != 0)
                        results.Add(i);
                }

                return results;
            }
        }

        public void DisableUpdate()
        {
            /*
            bool bOldVisible = this.Visible;

            this.Visible = false;

            return bOldVisible;
             * */

            if (this.m_nInSuspend == 0)
            {
                this.tableLayoutPanel_content.SuspendLayout();
                /*
                this.tableLayoutPanel_main.SuspendLayout();

                this.SuspendLayout();
                 * */
            }

            this.m_nInSuspend++;
        }

        // parameters:
        //      bOldVisible ���Ϊtrue, ��ʾ���Ҫ����
        public void EnableUpdate()
        {
            this.m_nInSuspend--;


            if (this.m_nInSuspend == 0)
            {
                this.tableLayoutPanel_content.ResumeLayout(false);
                this.tableLayoutPanel_content.PerformLayout();

                /*
                this.tableLayoutPanel_main.ResumeLayout(false);
                this.tableLayoutPanel_main.PerformLayout();

                this.ResumeLayout(false);
                 * */
            }
        }

#if NOOOOOOOOOOOOOOOOOOOOO
        // ����XML������¼����һ���µ�����
        public Item AppendNewItem(string strDefaultRecord,
            out string strError)
        {
            strError = "";

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strDefaultRecord);
            }
            catch (Exception ex)
            {
                strError = "XML��¼װ��DOMʱ����: " + ex.Message;
                return null;
            }

            Item item = AppendNewItem();

            item.Seller = DomUtil.GetElementText(dom.DocumentElement,
                "seller");
            item.Source = DomUtil.GetElementText(dom.DocumentElement,
                "source");
            try
            {
                item.Copy = Convert.ToInt32(DomUtil.GetElementText(dom.DocumentElement,
                    "copy"));
            }
            catch
            {
            }
            item.Price = DomUtil.GetElementText(dom.DocumentElement,
                "price");
            item.Distribute = DomUtil.GetElementText(dom.DocumentElement,
                "distribute");

            // ���ú� �Ѷ��� ״̬
            string strState = DomUtil.GetElementText(dom.DocumentElement,
                "state");
            if (strState == "�Ѷ���" || strState == "������")
                item.State |= ItemState.Ordered;

            try
            {
                item.OtherXml = strDefaultRecord;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return null;
            }


            return item;
        }
#endif

        // ����XML������¼����һ���µ�����
        public Item AppendNewItem(string strDefaultRecord,
            out string strError)
        {
            strError = "";

            Item item = AppendNewItem(false);

            int nRet = SetDefaultRecord(item,
                strDefaultRecord,
                false,  // ������indexֵ������ԭ����ֵ
                out strError);
            if (nRet == -1)
            {
                this.RemoveItem(item);
                return null;
            }

            return item;
        }

        // TODO: �����ƶ���OrderDesignControl��
        public static string LinkOldNewValue(string strOldValue,
            string strNewValue)
        {
            if (String.IsNullOrEmpty(strNewValue) == true)
                return strOldValue;

            if (strOldValue == strNewValue)
            {
                if (String.IsNullOrEmpty(strOldValue) == true)  // �¾ɾ�Ϊ��
                    return "";

                return strOldValue + "[=]";
            }

            return strOldValue + "[" + strNewValue + "]";
        }


        // ���� "old[new]" �ڵ�����ֵ
        public static void ParseOldNewValue(string strValue,
            out string strOldValue,
            out string strNewValue)
        {
            strOldValue = "";
            strNewValue = "";
            int nRet = strValue.IndexOf("[");
            if (nRet == -1)
            {
                strOldValue = strValue;
                strNewValue = "";
                return;
            }

            strOldValue = strValue.Substring(0, nRet).Trim();
            strNewValue = strValue.Substring(nRet + 1).Trim();

            // ȥ��ĩβ��']'
            if (strNewValue.Length > 0 && strNewValue[strNewValue.Length - 1] == ']')
                strNewValue = strNewValue.Substring(0, strNewValue.Length - 1);

            if (strNewValue == "=")
                strNewValue = strOldValue;
        }

        // ����ȱʡXML������¼����Ҫ���ֶ�
        int SetDefaultRecord(Item item,
            string strDefaultRecord,
            bool bResetIndexValue,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strDefaultRecord) == true)
                return 0;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strDefaultRecord);
            }
            catch (Exception ex)
            {
                strError = "XML��¼װ��DOMʱ����: " + ex.Message;
                return -1;
            }

            // catalog no
            item.CatalogNo = DomUtil.GetElementText(dom.DocumentElement,
                "catalogNo");

            // seller
            item.Seller = DomUtil.GetElementText(dom.DocumentElement,
                "seller");

            // range
            try
            {
                item.RangeString = DomUtil.GetElementText(dom.DocumentElement,
                    "range");
            }
            catch (Exception ex)
            {
                // 2008/12/18
                strError = ex.Message;
                return -1;
            }

            item.IssueCountString = DomUtil.GetElementText(dom.DocumentElement,
                "issueCount");

            // source
            string strSource = DomUtil.GetElementText(dom.DocumentElement,
                "source");

            string strNewSource = "";
            string strOldSource = "";
            ParseOldNewValue(strSource,
                out strOldSource,
                out strNewSource);

            if (String.IsNullOrEmpty(strNewSource) == true) // û����ֵ��ʱ���þ�ֵ��Ϊ��ʼֵ
                item.Source = strOldSource;
            else
                item.Source = strNewSource;

            item.OldSource = strOldSource;


            // distribute string
            // ע�⣺������copyǰ���ã���Ϊcopy string�п��ܰ�����ѡlocation item����Ϣ�����copy string�����ã���ѡ�õ�״̬�ᱻ��������distribute string�����
            string strDistribute = DomUtil.GetElementText(dom.DocumentElement,
                "distribute");

            item.Distribute = strDistribute;

            // copy
            // ע��copyֵ�ǰ���XML��¼�����õġ�һ������Ϊ���ܵ����ֵ
            string strCopy = DomUtil.GetElementText(dom.DocumentElement,
                    "copy");

            string strNewCopy = "";
            string strOldCopy = "";
            ParseOldNewValue(strCopy,
                out strOldCopy,
                out strNewCopy);

            /*
            if (String.IsNullOrEmpty(strNewCopy) == true) // û����ֵ��ʱ���þ�ֵ��Ϊ��ʼֵ
                item.CopyString = strOldCopy;
            else
                item.CopyString = strNewCopy;

            item.OldCopyString = strOldCopy;
             * */

            // 2008/11/3 changed
            if (this.ArriveMode == false)
            {
                // ����ʱ���þɼ۸�
                item.CopyString = strOldCopy;
                item.OldCopyString = strOldCopy;
            }
            else
            {
                // 2008/10/19 changed
                // ����ʱ���¾ɼ۸񶼷���
                if (String.IsNullOrEmpty(strNewCopy) == false)
                    item.CopyString = strNewCopy;

                if (String.IsNullOrEmpty(strOldCopy) == false)
                    item.OldCopyString = strOldCopy;
            }


            // ���ƹݲصص�����ĸ���
            int nMaxCopyValue = Math.Max(item.CopyValue, item.OldCopyValue);

            if (nMaxCopyValue < item.DistributeCount)
                item.DistributeCount = nMaxCopyValue;

            /*
            // ���ƹݲصص�����ĸ���
            try
            {
                strDistribute = LocationEditControl.CanonicalizeDistributeString(
                    strDistribute,
                    item.CopyValue);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }*/


            // price
            string strPrice = DomUtil.GetElementText(dom.DocumentElement,
                "price");

            string strNewPrice = "";
            string strOldPrice = "";
            ParseOldNewValue(strPrice,
                out strOldPrice,
                out strNewPrice);

            if (String.IsNullOrEmpty(strNewPrice) == true) // û����ֵ��ʱ���þ�ֵ��Ϊ��ʼֵ
                item.Price = strOldPrice;
            else
                item.Price = strNewPrice;

            item.OldPrice = strOldPrice;




            // class
            item.Class = DomUtil.GetElementText(dom.DocumentElement,
                "class");


            // ���ú� �Ѷ��� ״̬
            string strState = DomUtil.GetElementText(dom.DocumentElement,
                "state");

            if (this.ArriveMode == false)
            {
                // ����̬

                // ע��ֻ��δ�����ġ��ݸ�״̬������������޸Ķ���
                // �Ѷ���(��Ȼ��ȫ�������������ֲܾ�����)������������(�п����Ǿֲ����գ�����(��Ȼ�������굫��)Ǳ�ڿ���׷��)����������������ٽ����κζ�������������Ϊreadonly

                if (strState == "�Ѷ���" || strState == "������")
                    item.State |= ItemState.ReadOnly;
            }
            else
            {
                // ����̬
                if (strState == "�Ѷ���" || strState == "������")
                {
                    // ע��״̬Ϊ�������ա�ʱ����һ��ȫ�������������գ�������ʱӦ�������ٴ����ա�
                    // �������и����������գ�������׷�ӡ������ո��������������������readonly

                    // item.State -= ItemState.ReadOnly;

                    // ��location item���Ѿ���ѡ����������Ϊreadonly̬����ʾ���Ѿ����յ�(�ݲصص㡢��)����
                    item.location.SetAlreadyCheckedToReadOnly(false);

                }
                else
                {
                    // һ����Կ��ܳ����˿հ׵�״ֵ̬���������δ�����������ڲݸ��¼����ȻҲ���޴�������

                    item.State |= ItemState.ReadOnly;
                }

            }
            
            // 2009/2/13
            try
            {
                item.SellerAddressXml = DomUtil.GetElementOuterXml(dom.DocumentElement, "sellerAddress");
            }
            catch (Exception ex)
            {
                strError = "����SellerAddressXmlʱ��������: " + ex.Message;
                return -1;
            }

            try
            {
                item.OtherXml = strDefaultRecord;
            }
            catch (Exception ex)
            {
                strError = "����OtherXmlʱ��������: " + ex.Message;
                return -1;
            }

            if (bResetIndexValue == true)
            {
                // ����index
                item.Index = GetNewIndex(item);
            }

            return 0;
        }

        // ����һ�£����Ѿ���0�����������ǰ���£��������ķ���Ϊ0������
        public void RemoveMultipleZeroCopyItem()
        {
            int nTotalCopies = 0;
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item item = this.Items[i];
                nTotalCopies += item.CopyValue;
            }

            // 2008/8/27
            // ruguo you duoyu yige de 0 shixiang
            if (nTotalCopies == 0 && this.Items.Count > 1)
            {
                    for (int i = 1; i < this.Items.Count; i++)
                    {
                        Item item = this.Items[i];
                        this.RemoveItem(i);
                        i--;
                    }

            }

            if (nTotalCopies > 0)
            {
                for (int i = 0; i < this.Items.Count; i++)
                {
                    Item item = this.Items[i];
                    if (item.CopyValue == 0)
                    {
                        this.RemoveItem(i);
                        i--;
                    }
                }
            }
        }

        public Item AppendNewItem(bool bSetDefaultRecord)
        {
            this.DisableUpdate();   // ��ֹ���������׽�����⡣2009/10/13 

            try
            {
                this.tableLayoutPanel_content.RowCount += 1;
                this.tableLayoutPanel_content.RowStyles.Add(new System.Windows.Forms.RowStyle());

                Item item = new Item(this);

                item.AddToTable(this.tableLayoutPanel_content, this.Items.Count + 1);

                this.Items.Add(item);

                if (this.GetDefaultRecord != null
                    && bSetDefaultRecord == true)
                {
                    GetDefaultRecordEventArgs e = new GetDefaultRecordEventArgs();
                    this.GetDefaultRecord(this, e);

                    string strDefaultRecord = e.Xml;

                    if (String.IsNullOrEmpty(strDefaultRecord) == true)
                        goto END1;

                    string strError = "";
                    // ����ȱʡXML������¼����Ҫ���ֶ�
                    int nRet = SetDefaultRecord(item,
                        strDefaultRecord,
                        true,
                        out strError);
                    if (nRet == -1)
                        throw new Exception(strError);

                }

            END1:
                item.State = ItemState.New;

                return item;
            }
            finally
            {
                this.EnableUpdate();
            }
        }

        public Item InsertNewItem(int index)
        {
            this.DisableUpdate();   // ��ֹ���������׽�����⡣2009/10/13 

            try
            {

                this.tableLayoutPanel_content.RowCount += 1;
                this.tableLayoutPanel_content.RowStyles.Insert(index + 1, new System.Windows.Forms.RowStyle());

                Item item = new Item(this);

                item.InsertToTable(this.tableLayoutPanel_content, index);

                this.Items.Insert(index, item);

                if (this.GetDefaultRecord != null)
                {
                    GetDefaultRecordEventArgs e = new GetDefaultRecordEventArgs();
                    this.GetDefaultRecord(this, e);

                    string strDefaultRecord = e.Xml;

                    if (String.IsNullOrEmpty(strDefaultRecord) == true)
                        goto END1;

                    string strError = "";
                    // ����ȱʡXML������¼����Ҫ���ֶ�
                    int nRet = SetDefaultRecord(item,
                        strDefaultRecord,
                        true,
                        out strError);
                    if (nRet == -1)
                        throw new Exception(strError);
                }
            END1:

                item.State = ItemState.New;

                return item;
            }
            finally
            {
                this.EnableUpdate();
            }
        }

        public void RemoveItem(int index)
        {
            Item line = this.Items[index];

            line.RemoveFromTable(this.tableLayoutPanel_content, index);

            this.Items.Remove(line);

            /*
            if (this.LastClickItem == line)
                this.LastClickItem = null;
             * */

            this.Changed = true;
        }

        public void RemoveItem(Item line)
        {
            int index = this.Items.IndexOf(line);

            if (index == -1)
                return;

            line.RemoveFromTable(this.tableLayoutPanel_content, index);

            this.Items.Remove(line);

            /*
            if (this.LastClickItem == line)
                this.LastClickItem = null;
             * */

            this.Changed = true;
        }

        /*
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown1_KeyPress(object sender, KeyPressEventArgs e)
        {
            API.PostMessage(this.Handle, WM_NUMBER_CHANGED, 0, 0);
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NUMBER_CHANGED:
                    {
                        numericUpDown1_ValueChanged(null, null);
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }
         * */

#if NOOOOOOOOOOOOOOOOOOOOOOOO
        private void textBox_totalCopy_TextChanged(object sender, EventArgs e)
        {
            // ��Ҫ��Ӧ�¼�
            if (this.DisableTextChanged > 0)
                return;

            if (this.textBox_totalCopy.Text == "")
                return;

            Item item = null;


            int nValue = 0;

            try
            {
                nValue = Convert.ToInt32(this.textBox_totalCopy.Text);
            }
            catch
            {
                MessageBox.Show(this, "�ܲ��� '" + this.textBox_totalCopy.Text + "' Ӧ��Ϊ������";
                return;
            }

            // ����б���һ������Ҳû�У�������һ������
            if (this.Items.Count == 0)
            {
                item = AppendNewItem();
                item.Copy = nValue;
                return;
            }

            if (this.Items.Count == 1)
            {
                item = this.Items[0];
                item.Copy = nValue;
                return;
            }


            int nCurrent = 0;   // ��ǰ̨��
            for (int i = 0; i < this.Items.Count; i++)
            {
                item = this.Items[i];

                if (nValue >= nCurrent
                    && nValue < nCurrent + item.Copy)
                {
                    // ����һ��item�ķ�Χ
                    item.Copy = nValue - nCurrent;

                    // this.Items.RemoveRange(i + 1, this.Items.Count - i - 1);
                    for (int j = i + 1; j < this.Items.Count; j++)
                    {
                        this.RemoveItem(i + 1);
                    }
                    return;
                }

                nCurrent += item.Copy;
            }

            // �޸����һ��
            item.Copy = nValue - nCurrent;
        }
#endif

        // ������׸��������ֵ��������(���Ѷ�����״̬�����)�Ƿ�ֵ��ʱ��ʹ�á�
        int GetFirstValidCopyValue()
        {
            int nValue = 0;
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item item = this.Items[i];
                if ((item.State & ItemState.ReadOnly) == 0)
                    continue;
                nValue += item.CopyValue;
            }

            return nValue;
        }

        // �¶����ܲ��� textbox ֵ�ı�
        private void textBox_newlyOrderTotalCopy_TextChanged(object sender, EventArgs e)
        {
            // �����ǰΪ����ģʽ������Ӧ
            if (this.ArriveMode == true)
                return;

            // ��Ҫ��Ӧ�¼�
            if (this.DisableNewlyOrderTextChanged > 0)
                return;

            if (this.textBox_newlyOrderTotalCopy.Text == "")
                return;

            this.DisableUpdate();

            try
            {

                Item item = null;


                int nValue = 0;

                try
                {
                    nValue = Convert.ToInt32(this.textBox_newlyOrderTotalCopy.Text);
                }
                catch
                {
                    MessageBox.Show(this, "�¹滮�ܲ��� '" + this.textBox_newlyOrderTotalCopy.Text + "' Ӧ��Ϊ������");

                    // 2008/9/16
                    this.textBox_newlyOrderTotalCopy.Text = this.GetNewlyOrderTotalCopy().ToString();  // �ı�ؿ��е�ֵ
                    return;
                }

                // ����б���һ������Ҳû�У�������һ������
                if (this.Items.Count == 0)
                {
                    item = AppendNewItem(true);
                    item.CopyValue = nValue;
                    return;
                }


                Item lastChangeableItem = null; // �����з��ֵ����һ���ǡ��Ѷ���״̬�������

                int nCurrent = 0;   // ��ǰ̨��
                for (int i = 0; i < this.Items.Count; i++)
                {
                    item = this.Items[i];

                    // �����Ѷ�������
                    if ((item.State & ItemState.ReadOnly) != 0)
                        continue;

                    lastChangeableItem = item;

                    if (nValue >= nCurrent
                        && nValue < nCurrent + item.CopyValue)
                    {
                        // ����һ��item�ķ�Χ
                        item.CopyValue = nValue - nCurrent;

                        // ɾ�����item��������з��Ѷ���״̬������
                        for (int j = i + 1; j < this.Items.Count; j++)
                        {
                            Item temp = this.Items[j];
                            // �����Ѷ�������
                            if ((temp.State & ItemState.ReadOnly) != 0)
                                continue;

                            this.RemoveItem(i + 1);
                        }
                        return;
                    }

                    nCurrent += item.CopyValue;
                }
                // ѭ��������item�б����˱��������������һ�����
                // lastChangeableItem����Ϊ���һ���ǡ��Ѷ��������

                if (nValue - nCurrent == 0)
                    return; // û�б�Ҫ�޸�ʲô

                // ����������һ���ɸı�����
                if (lastChangeableItem != null)
                {
                    lastChangeableItem.CopyValue += nValue - nCurrent;
                    return;
                }

                /*
                // �޸����һ�������һ����Ѷ�������
                if ((item.State & ItemState.Ordered) == 0)
                {
                    item.Copy += nValue - nCurrent;
                    return;
                }*/

                Debug.Assert(nValue > nCurrent, "");

                // ����Ҫ���������һ��������
                item = AppendNewItem(true);
                item.CopyValue = nValue - nCurrent;
                return;

            }
            finally
            {
                this.EnableUpdate();
            }

            /*
            ERROR1:
            MessageBox.Show(this, strError);
             * */
        }


        private void textBox_newlyOrderTotalCopy_Validating(object sender, CancelEventArgs e)
        {
            if (this.textBox_newlyOrderTotalCopy.Text == "")
                return;

            try
            {
                int nValue = Convert.ToInt32(this.textBox_newlyOrderTotalCopy.Text);
            }
            catch
            {
                MessageBox.Show(this, "����������� '" + this.textBox_newlyOrderTotalCopy.Text + "' ��ʽ����ȷ");
                e.Cancel = true;
                return;
            }
        }

        private void label_topleft_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void OrderCrossControl_Enter(object sender, EventArgs e)
        {
            this.m_bFocused = true;
            this.RefreshLineColor();

        }

        private void OrderCrossControl_Leave(object sender, EventArgs e)
        {
            this.m_bFocused = false;
            this.RefreshLineColor();

        }

        // ��ĩβ׷��һ���µ�����
        private void button_newItem_Click(object sender, EventArgs e)
        {
            int nPos = this.Items.Count;

            this.InsertNewItem(nPos).EnsureVisible();
        }

        // �������һ��δ����ȫ�����ġ��ɸı�copyֵ������
        Item GetLastChangeableItem()
        {
            for (int i = this.Items.Count - 1; i >=0 ; i--)
            {
                Item item = this.Items[i];

                // ����(����)�ѱ��Ϊֻ���ķ���������
                if ((item.State & ItemState.ReadOnly) != 0)
                    continue;

                return item;
            }

            return null;
        }

        // �������ܲ��� textbox ֵ�ı�
        private void textBox_newlyArriveTotalCopy_TextChanged(object sender, EventArgs e)
        {
            // �����ǰΪ����ģʽ������Ӧ
            if (this.ArriveMode == false)
                return;

            // ��Ҫ��Ӧ�¼�
            if (this.DisableNewlyArriveTextChanged > 0)
                return;

            if (this.textBox_newlyArriveTotalCopy.Text == "")
                return;

            /*
             * �㷨Ϊ��������������������������ǵĹݲ������д򹴵��ж��١�
             * ����򹴵Ĳ��㣬�����ʵ�λ�����Ӵ򹴡�����򹴵�̫�࣬��off�󷽵Ķ�������
             * �������ݲ�����㣬Ҳ���Ǽ���ȫ�������Ҳ����Ҫ�����Ŀ���������Ӷ���������������õĹݲ�����
             * */

            Item item = null;

            int nValue = 0;

            try
            {
                nValue = Convert.ToInt32(this.textBox_newlyArriveTotalCopy.Text);
            }
            catch
            {
                MessageBox.Show(this, "�������ܲ��� '" + this.textBox_newlyArriveTotalCopy.Text + "' Ӧ��Ϊ������");

                // 2008/9/16
                this.textBox_newlyArriveTotalCopy.Text = GetNewlyArriveTotalCopy().ToString();  // �ı�ؿ��е�ֵ
                return;
            }

            // ����б���һ������Ҳû�У�������һ������
            if (this.Items.Count == 0)
            {
                if (nValue == 0)
                    return;

                // ����̫���ֵ
                if (nValue > 10)
                {
                    DialogResult result = MessageBox.Show(this,
                        "ȷʵҪ���� " + nValue.ToString() + " ��ô���ֵ?",
                        "OrderDesignControl",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result != DialogResult.Yes)
                    {
                        this.textBox_newlyArriveTotalCopy.Text = GetNewlyArriveTotalCopy().ToString();  // �ı�ؿ��е�ֵ
                        return;
                    }
                }

                item = AppendNewItem(true);
                item.CopyValue = nValue;
                return;
            }

            // TODO: Ӧ�ȼ����deltaֵ��Ȼ�����ÿ��itemʱ��һ��Item����һ��(��CopyValue��OldCopyValue֮��)

            // �������е�arrived count(������readonly checked)
            int nNewlyArrivedCount = GetNewlyArriveTotalCopy();

            /*
            for (int i = 0; i < this.Items.Count; i++)
            {
                item = this.Items[i];

                nNewlyArrivedCount += item.location.ArrivedCount - item.location.ReadOnlyArrivedCount;
            }*/

            // 
            int nDelta = nValue - nNewlyArrivedCount;

            if (nDelta == 0)
                return; // ��û�б�Ҫ����Ҳû�б�Ҫ��

            // ����Ҫ��ǰ����ʼ���С�
            if (nDelta > 0)
            {
                // ����̫���ֵ
                if (nDelta > 10)
                {
                    DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
                        "ȷʵҪ���� " + nValue.ToString() + " ��ô���ֵ?",
                        "OrderDesignControl",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result != DialogResult.Yes)
                    {
                        this.textBox_newlyArriveTotalCopy.Text = GetNewlyArriveTotalCopy().ToString();  // �ı�ؿ��е�ֵ
                        return;
                    }
                }

                for (int i = 0; i < this.Items.Count; i++)
                {
                    item = this.Items[i];

                    int nCheckable = item.location.Count - item.location.ArrivedCount;
                    if (nCheckable > 0)
                    {
                        int nThisCount = Math.Min(nDelta, nCheckable);
                        item.location.ArrivedCount += nThisCount;
                        item.UpdateCopyCount(); // 2008/12/18
                    }

                    nDelta -= nCheckable;
                    if (nDelta <= 0)
                        break;
                }

                if (nDelta > 0)
                {
                    // ���������������Χ�ڣ���û�����㹻����Ҫ���������һ��
                    item = GetLastChangeableItem();
                    if (item == null)
                    {
                        MessageBox.Show(this, "û�пɸı������");
                        textBox_newlyArriveTotalCopy.Text = (nValue - nDelta).ToString();   // �޸ĵ�һ������ֵ
                        return;
                    }
                    item.location.ArrivedCount += nDelta;
                    item.UpdateCopyCount(); // 2008/12/18
                }

                return;
            }

            // ����Ҫ�Ӻ󷽿�ʼ���С�
            if (nDelta < 0)
            {
                nDelta *= -1;   // ��Ϊ����

                for (int i = this.Items.Count - 1; i>= 0; i--)
                {
                    item = this.Items[i];

                    int nUnCheckable = item.location.ArrivedCount - item.location.ReadOnlyArrivedCount;
                    if (nUnCheckable > 0)
                    {
                        int nThisCount = Math.Min(nDelta, nUnCheckable);
                        item.location.ArrivedCount -= nThisCount;   // ���Զ�ɾ��һЩ�հ׹ݲصص������
                        item.UpdateCopyCount(); // 2008/12/18
                    }

                    nDelta -= nUnCheckable;
                    if (nDelta <= 0)
                        break;
                }

                if (nDelta > 0)
                {
                    MessageBox.Show(this, "�޷���С�� " + nValue.ToString());
                    textBox_newlyArriveTotalCopy.Text = (nValue + nDelta).ToString();   // �޸ĵ�һ������ֵ
                    return;
                }

                return;
            }
        }

        // ��ʣ�µ����ȫ������
        private void button_fullyAccept_Click(object sender, EventArgs e)
        {
            int nValue = GetNewlyArrivingTotalCopy();
            if (nValue == 0)
            {
                string strMessage = "";
                int nRet = NotOrdering(out strMessage);
                if (nRet == 2)
                    MessageBox.Show(ForegroundWindow.Instance, "������δ������ӡ�������ڣ��޷���������");
                else
                    MessageBox.Show(ForegroundWindow.Instance, "�Ѿ�����");
                return;
            }

            // string strRightCopy = OrderDesignControl.GetRightFromCopyString(this.comboBox_copy.Text);

            string strOldValue = this.textBox_newlyArriveTotalCopy.Text;

            try
            {
                this.textBox_newlyArriveTotalCopy.Text = nValue.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                this.textBox_newlyArriveTotalCopy.Text = strOldValue;
            }
        }

        internal void InvalidateLine(Item item)
        {

            Point p = this.tableLayoutPanel_content.PointToScreen(new Point(0, 0));

            Rectangle rect = item.label_color.RectangleToScreen(item.label_color.ClientRectangle);
            rect.Width = this.tableLayoutPanel_content.DisplayRectangle.Width;
            rect.Offset(-p.X, -p.Y);
            rect.Height = (int)this.Font.GetHeight() + 8;   // ��Сˢ�¸߶�

            this.tableLayoutPanel_content.Invalidate(rect, false);

            // this.tableLayoutPanel_content.Invalidate();
        }


        private void tableLayoutPanel_content_Paint(object sender, PaintEventArgs e)
        {
            Brush brushText = new SolidBrush(Color.Black);


            // Brush brushDark = new SolidBrush(Color.Gray); // Color.DarkGreen
            // e.Graphics.FillRectangle(brush, e.ClipRectangle);

            Pen pen = new Pen(Color.Red);

            Point p = this.tableLayoutPanel_content.PointToScreen(new Point(0, 0));
            // Debug.WriteLine("p x=" + p.X.ToString() + " y=" + p.Y.ToString());

            // int[] row_heights = this.tableLayoutPanel_content.GetRowHeights();
            int[] column_widths = this.tableLayoutPanel_content.GetColumnWidths();
            // Debug.WriteLine("height count=" + row_heights.Length.ToString() + " width count=" + column_widths.Length.ToString());

            Font font = null;
            List<string> column_titles = new List<string>();
            for (int j = 0; j < this.tableLayoutPanel_content.ColumnCount; j++)
            {
                Control control = this.tableLayoutPanel_content.GetControlFromPosition(j, 0);
                if (control != null)
                {
                    column_titles.Add(control.Text);
                    if (font == null)
                        font = control.Font;
                }
                else
                    column_titles.Add("");
            }


            // float y = row_heights[0];   // +this.AutoScrollPosition.Y + this.tableLayoutPanel_content.Location.Y;
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item item = this.Items[i];

                if ((item.State & ItemState.Selected) == 0
                    || i == 0)
                    continue;

                // int height = row_heights[i + 1];

                Rectangle rect = item.label_color.RectangleToScreen(item.label_color.ClientRectangle);
                rect.Width = this.tableLayoutPanel_content.DisplayRectangle.Width;
                rect.Offset(-p.X, -p.Y);
                rect.Height = (int)this.Font.GetHeight() + 8;

                LinearGradientBrush brushGradient = new LinearGradientBrush(
new PointF(rect.X, rect.Y),
new PointF(rect.X, rect.Y + rect.Height),
Color.FromArgb(10, Color.Gray),
Color.FromArgb(50, Color.Gray)
);


                e.Graphics.FillRectangle(brushGradient, rect);


                // һ����ÿ������
                float x = rect.X;    //  this.AutoScrollPosition.X + this.tableLayoutPanel_content.Location.X;
                for (int j = 0; j < column_widths.Length; j++)
                {
                    float fWidth = column_widths[j];

                    string strTitle = column_titles[j];
                    // Debug.WriteLine("x=" + x.ToString() + " y=" + y.ToString());

                    /*
                    Rectangle rect = new Rectangle((int)x, (int)y, (int)fWidth, height);
                    rect.Offset(-p.X, -p.Y);
                    e.Graphics.DrawRectangle(pen, rect);
                     * */
                    if (fWidth > 0 && string.IsNullOrEmpty(strTitle) == false)
                    {

                        e.Graphics.DrawString(
                        strTitle,
                        font,
                        brushText,
                        x + 6,
                        rect.Y + 4);
                    }
                    x += fWidth;


                }

                // y += height;
            }






#if NOOOOOOOOOOOOOOOOOOO
            Brush brush = new SolidBrush(Color.Red);
            // e.Graphics.FillRectangle(brush, e.ClipRectangle);

            Pen pen = new Pen(Color.Red);

            Point p = this.tableLayoutPanel_content.PointToScreen(new Point(0, 0));

            // ������
            for (int i = 0; i < this.Items.Count; i++)
            {
                Item item = this.Items[i];

                Rectangle rect = item.label_color.RectangleToScreen(item.label_color.ClientRectangle);
                rect.Width = this.tableLayoutPanel_content.DisplayRectangle.Width;
                rect.Offset(-p.X, -p.Y);
                e.Graphics.DrawRectangle(pen, rect);
            }

            /*
            // ������
            float y = 0;
            for (int i = 0; i < this.tableLayoutPanel_content.RowStyles.Count; i++)
            {
                float fHeight = this.tableLayoutPanel_content.RowStyles[i].Height;

                e.Graphics.DrawLine(pen,
                    p.X, y+p.Y,
                    p.X + 3000, y+p.Y);
                y += fHeight;
            }
             * */

            // ������
            float x = 0;
            for(int i=0;i<this.tableLayoutPanel_content.ColumnStyles.Count;i++)
            {
                float fWidth = this.tableLayoutPanel_content.ColumnStyles[i].Width;

                e.Graphics.DrawLine(pen, 
                    x+p.X, 0,
                    x+p.X, 3000);
                x += fWidth;
            }
#endif
        }

        private void tableLayoutPanel_content_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            /*
            Brush brush = new SolidBrush(Color.Red);
            Rectangle rect = Rectangle.Inflate(e.CellBounds, -1, -1);
            e.Graphics.FillRectangle(brush, rect);
             * */
            if (this.m_nInSuspend > 0)
                return; // ��ֹ����

            // Rectangle rect = Rectangle.Inflate(e.CellBounds, -1, -1);
            Rectangle rect = e.CellBounds;
            Pen pen = new Pen(Color.FromArgb(200,200,200));
            e.Graphics.DrawRectangle(pen, rect);
        }


    }

    [Flags]
    public enum ItemState
    {
        Normal = 0x00,  // ��ͨ״̬
        Changed = 0x01, // ���ݱ��޸Ĺ�
        New = 0x02, // ��������
        Selected = 0x04,    // ��ѡ��

        ReadOnly = 0x10, // ״̬Ϊֻ�����С�����̬�£���Ϊ���Ѷ������������Ѿ����������ݲ����ٸ����ˣ�����̬�£���Ϊ��δ���������Բ��ܽ������գ����ݲ��ܸ���
    }

    public class Item
    {
        int m_nInDropDown = 0;  // 2009/1/15

        public OrderDesignControl Container = null;

        public object Tag = null;   // ���ڴ����Ҫ���ӵ��������Ͷ���

        // ��ɫ��popupmenu
        public Label label_color = null;

        // ��Ŀ�� 2008/8/31
        public TextBox textBox_catalogNo = null;

        // ����
        public ComboBox comboBox_seller = null;

        // ������Դ
        public DoubleComboBox comboBox_source = null;

        // ʱ�䷶Χ
        public DateRangeControl dateRange_range = null;

        // ����
        public ComboBox comboBox_issueCount = null;

        // ������
        public DoubleComboBox comboBox_copy = null;

        // ����
        public DoubleTextBox textBox_price = null;


        // ȥ��
        // public TextBox textBox_location = null;
        public LocationEditControl location = null;

        // ��� 2008/8/31
        public ComboBox comboBox_class = null;

        // ������ַ
        public Label label_sellerAddress = null;

        internal string m_sellerAddressXml = "";    // ��ʾ������ַ��XML��¼����Ԫ��Ϊ<sellerAddress>

        // ������Ϣ
        public Label label_other = null;

        internal string m_otherXml = "";    // ��ʾ������Ϣ��XML��¼

        ItemState m_state = ItemState.Normal;

        // �����޸�location�ؼ���ArrivedCount����Ҫ����ݹ鴦���ɴ�������¼�
        int DisableLocationArrivedChanged = 0;


        public Item(OrderDesignControl container)
        {

            this.Container = container;
            int nTopBlank = (int)this.Container.Font.GetHeight() + 2;

            label_color = new Label();
            label_color.Dock = DockStyle.Fill;
            label_color.Size = new Size(6, 28);
            label_color.Margin = new Padding(1, 0, 1, 0);

            // ��Ŀ��
            this.textBox_catalogNo = new TextBox();
            textBox_catalogNo.BorderStyle = BorderStyle.None;
            textBox_catalogNo.Dock = DockStyle.Fill;
            textBox_catalogNo.MinimumSize = new Size(80, 28);
            // textBox_price.Multiline = true;
            textBox_catalogNo.Margin = new Padding(6, nTopBlank + 6, 6, 0);
            textBox_catalogNo.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;
            // this.textBox_catalogNo.Visible = false;


            // ����
            comboBox_seller = new ComboBox();
            comboBox_seller.DropDownStyle = ComboBoxStyle.DropDown;
            comboBox_seller.FlatStyle = FlatStyle.Flat;
            comboBox_seller.Dock = DockStyle.Fill;
            comboBox_seller.MaximumSize = new Size(150, 28);
            comboBox_seller.Size = new Size(100, 28);
            comboBox_seller.MinimumSize = new Size(50, 28);
            comboBox_seller.DropDownHeight = 300;
            comboBox_seller.DropDownWidth = 300;
            comboBox_seller.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;
            comboBox_seller.Text = "";
            comboBox_seller.Margin = new Padding(6, nTopBlank + 6, 6, 0);
            // this.comboBox_seller.Visible = false;

            // ������Դ
            comboBox_source = new DoubleComboBox();

            comboBox_source.ComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            comboBox_source.ComboBox.FlatStyle = FlatStyle.Flat;
            comboBox_source.ComboBox.DropDownHeight = 300;
            comboBox_source.ComboBox.DropDownWidth = 300;
            comboBox_source.ComboBox.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;
            comboBox_source.Margin = new Padding(6, nTopBlank,  // + 3,
                6, 0);

            comboBox_source.TextBox.ReadOnly = true;
            comboBox_source.TextBox.BorderStyle = BorderStyle.None;
            comboBox_source.TextBox.ForeColor = SystemColors.GrayText;

            comboBox_source.Dock = DockStyle.Fill;
            comboBox_source.MaximumSize = new Size(110, 28*2);
            comboBox_source.Size = new Size(80, 28*2);
            comboBox_source.MinimumSize = new Size(50, 28);


            // ��Χ
            dateRange_range = new DateRangeControl();

            if (container != null && container.SeriesMode == false)
            {
                // dateRange_range.Visible = false; // ????
            }

            // dateRange_range.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;
            dateRange_range.BorderStyle = BorderStyle.None;

            dateRange_range.Dock = DockStyle.Fill;
            /*
            dateRange_range.MaximumSize = new Size(150, 28 * 2);
            dateRange_range.Size = new Size(150, 28 * 2);
            dateRange_range.MinimumSize = new Size(130, 28 * 2);
             * */
            dateRange_range.Margin = new Padding(1, nTopBlank, // + 3,
                1, 0);
            // this.dateRange_range.Visible = false;



            // ����
            /*
            textBox_issueCount = new TextBox();
            textBox_issueCount.BorderStyle = BorderStyle.None;
            textBox_issueCount.Dock = DockStyle.Fill;
            textBox_issueCount.MinimumSize = new Size(100, 28);
            textBox_issueCount.Margin = new Padding(6, 3, 6, 0);
            textBox_issueCount.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;
            */
            comboBox_issueCount = new ComboBox();

            if (container != null && container.SeriesMode == false)
                this.comboBox_issueCount.Visible = false;

            comboBox_issueCount.DropDownStyle = ComboBoxStyle.DropDown;
            comboBox_issueCount.FlatStyle = FlatStyle.Flat;
            comboBox_issueCount.DropDownHeight = 300;
            comboBox_issueCount.DropDownWidth = 100;
            comboBox_issueCount.Dock = DockStyle.Fill;
            comboBox_issueCount.MaximumSize = new Size(100, 28);
            comboBox_issueCount.Size = new Size(70, 28);
            comboBox_issueCount.MinimumSize = new Size(50, 28);
            comboBox_issueCount.Items.AddRange(new object[] {
            "6",
            "12",
            "24",
            "36"});

            comboBox_issueCount.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;
            comboBox_issueCount.Margin = new Padding(6, nTopBlank + 6, // + 3,
                6, 0);
            // this.comboBox_issueCount.Visible = false;



            // ������
            /*
            comboBox_copy = new ComboBox();
            comboBox_copy.DropDownStyle = ComboBoxStyle.DropDown;
            comboBox_copy.FlatStyle = FlatStyle.Flat;
            comboBox_copy.DropDownHeight = 300;
            comboBox_copy.DropDownWidth = 250;
            comboBox_copy.Dock = DockStyle.Fill;
            comboBox_copy.MaximumSize = new Size(100, 28);
            comboBox_copy.Size = new Size(70, 28);
            comboBox_copy.MinimumSize = new Size(50, 28);

            comboBox_copy.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;
             * */
            comboBox_copy = new DoubleComboBox();
            comboBox_copy.ComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            comboBox_copy.ComboBox.FlatStyle = FlatStyle.Flat;
            comboBox_copy.ComboBox.DropDownHeight = 300;
            comboBox_copy.ComboBox.DropDownWidth = 250;
            comboBox_copy.ComboBox.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;
            comboBox_copy.Margin = new Padding(6, nTopBlank, // + 3,
                6, 0);

            comboBox_copy.TextBox.ReadOnly = true;
            comboBox_copy.TextBox.BorderStyle = BorderStyle.None;
            comboBox_copy.TextBox.ForeColor = SystemColors.GrayText;

            comboBox_copy.Dock = DockStyle.Fill;
            comboBox_copy.MaximumSize = new Size(60, 28*2);
            comboBox_copy.Size = new Size(40, 28*2);
            comboBox_copy.MinimumSize = new Size(30, 28*2);
            // this.comboBox_copy.Visible = false;



            // ����
            textBox_price = new DoubleTextBox();
            textBox_price.TextBox.BorderStyle = BorderStyle.None;
            textBox_price.TextBox.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;

            textBox_price.SecondTextBox.ReadOnly = true;
            textBox_price.SecondTextBox.BorderStyle = BorderStyle.None;
            textBox_price.SecondTextBox.ForeColor = SystemColors.GrayText;

            textBox_price.Dock = DockStyle.Fill;
            textBox_price.MaximumSize = new Size(90, 28 * 2);
            textBox_price.Size = new Size(70, 28 * 2);
            textBox_price.MinimumSize = new Size(50, 28 * 2);
            textBox_price.Margin = new Padding(6, nTopBlank + 1,
                6, 0);
            // textBox_price.BorderStyle = BorderStyle.FixedSingle;
            // this.textBox_price.Visible = false;


            // ȥ��
            location = new LocationEditControl();
            location.ArriveMode = this.Container.ArriveMode;
            location.BorderStyle = BorderStyle.None;
            location.Dock = DockStyle.Fill;
            // location.MinimumSize = new Size(100, 28);
            location.Margin = new Padding(6, nTopBlank + 6,
                6, 0);

            location.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;
            location.AutoScaleMode = AutoScaleMode.None;    // ��ֹ���ڲ��Ŀؼ�����ȥ������Ų��λ��
            // location.BorderStyle = BorderStyle.FixedSingle;
            location.DbName = container.BiblioDbName;

            // ���
            comboBox_class = new ComboBox();
            comboBox_class.DropDownStyle = ComboBoxStyle.DropDown;
            comboBox_class.FlatStyle = FlatStyle.Flat;
            comboBox_class.Dock = DockStyle.Fill;
            comboBox_class.MaximumSize = new Size(150, 28);
            comboBox_class.Size = new Size(100, 28);
            comboBox_class.MinimumSize = new Size(50, 28);
            comboBox_class.DropDownHeight = 300;
            comboBox_class.DropDownWidth = 300;
            comboBox_class.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;
            comboBox_class.Text = "";
            comboBox_class.Margin = new Padding(6, nTopBlank + 6,
                6, 0);
            // this.comboBox_class.Visible = false;

            // ������ַ
            this.label_sellerAddress = new Label();
            this.label_sellerAddress.BorderStyle = BorderStyle.None;
            this.label_sellerAddress.Dock = DockStyle.Fill;
            this.label_sellerAddress.MinimumSize = new Size(40, 28 * 2);
            // this.label_sellerAddress.Multiline = true;
            this.label_sellerAddress.Margin = new Padding(6, nTopBlank + 6,
                6, 0);
            this.label_sellerAddress.AutoSize = true;

            this.label_sellerAddress.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;
            // this.label_sellerAddress.Visible = false;


            // ����
            this.label_other = new Label();
            this.label_other.BorderStyle = BorderStyle.None;
            this.label_other.Dock = DockStyle.Fill;
            this.label_other.MinimumSize = new Size(50, 28 * 2);
            // this.label_other.Multiline = true;
            this.label_other.Margin = new Padding(6, nTopBlank + 6,
                6, 0);
            this.label_other.AutoSize = true;
            // this.label_other.Visible = false;

            this.label_other.ForeColor = this.Container.tableLayoutPanel_content.ForeColor;
        }

        public void EnsureVisible()
        {
            this.Container.EnsureVisible(this);
        }

        bool m_bSeriesMode = false;
        public bool SeriesMode
        {
            get
            {
                return this.m_bSeriesMode;
            }
            set
            {
                this.m_bSeriesMode = value;
                if (value == true)
                {
                    this.dateRange_range.Visible = true;
                    this.comboBox_issueCount.Visible = true;
                }
                else
                {
                    this.dateRange_range.Visible = false;
                    this.comboBox_issueCount.Visible = false;
                }
            }
        }

        bool m_bReadOnly = false;

        public bool ReadOnly
        {
            get
            {
                return this.m_bReadOnly;
            }
            set
            {
                bool bOldValue = this.m_bReadOnly;
                if (bOldValue != value)
                {
                    this.m_bReadOnly = value;

                    // ��Ŀ��
                    this.textBox_catalogNo.ReadOnly = value;

                    // ����
                    this.comboBox_seller.Enabled = !value;

                    // ������Դ
                    this.comboBox_source.Enabled = !value;

                    // ʱ�䷶Χ
                    this.dateRange_range.Enabled = !value;

                    // ����
                    this.comboBox_issueCount.Enabled = !value;

                    // ������
                    this.comboBox_copy.Enabled = !value;

                    // ����
                    this.textBox_price.ReadOnly = value;

                    // ȥ��
                    this.location.ReadOnly = value;

                    // ���
                    this.comboBox_class.Enabled = !value;

                    // ������ַ

                    // ����
                    // this.label_other
                }
            }
        }

        // ����״̬
        public ItemState State
        {
            get
            {
                return this.m_state;
            }
            set
            {
                if (this.m_state != value)
                {
                    this.m_state = value;

                    SetLineColor();

                    bool bOldReadOnly = this.ReadOnly;
                    if ((this.m_state & ItemState.ReadOnly) != 0)
                    {
                        this.ReadOnly = true;
                    }
                    else
                    {
                        this.ReadOnly = false;
                    }

                    // ״̬�䶯�󣬻����������ͳ��ֵ�ı䶯
                    if (bOldReadOnly != this.ReadOnly)
                    {
                        // �����ǰ�Ƕ���̬
                        if (this.Container.ArriveMode == false)
                        {
                            this.Container.DisableNewlyOrderTextChanged++;    // �Ż����������Ķ���
                            this.Container.textBox_newlyOrderTotalCopy.Text = this.Container.GetNewlyOrderTotalCopy().ToString();
                            this.Container.DisableNewlyOrderTextChanged--;

                            int nOrderedTotalCopy = this.Container.GetOrderedTotalCopy();
                            this.Container.textBox_orderedTotalCopy.Text = nOrderedTotalCopy.ToString();

                            if (nOrderedTotalCopy > 0)
                                this.Container.OrderedTotalCopyVisible = true;
                            else
                                this.Container.OrderedTotalCopyVisible = false;
                        }
                        else
                        {
                            // �����ǰ������̬

                            this.Container.DisableNewlyArriveTextChanged++;    // �Ż����������Ķ���
                            this.Container.textBox_newlyArriveTotalCopy.Text = this.Container.GetNewlyArriveTotalCopy().ToString();
                            this.Container.DisableNewlyArriveTextChanged--;

                            int nArrivedTotalCopy = this.Container.GetArrivedTotalCopy();
                            this.Container.textBox_arrivedTotalCopy.Text = nArrivedTotalCopy.ToString();

                            if (nArrivedTotalCopy > 0)
                                this.Container.ArrivedTotalCopyVisible = true;
                            else
                                this.Container.ArrivedTotalCopyVisible = false;
                        }

                    }
                }
            }
        }

        // �����������label����ɫ
        internal void SetLineColor()
        {
            if ((this.m_state & ItemState.Selected) != 0)
            {
                // û�н��㣬����Ҫ����selection����
                if (this.Container.HideSelection == true
                    && this.Container.m_bFocused == false)
                {
                    // ��������ߣ���ʾ������ɫ
                }
                else
                {
                    this.label_color.BackColor = SystemColors.Highlight;
                    return;
                }
            }
            if ((this.m_state & ItemState.New) != 0)
            {
                this.label_color.BackColor = Color.Yellow;
                return;
            }
            if ((this.m_state & ItemState.Changed) != 0)
            {
                this.label_color.BackColor = Color.LightGreen;
                return;
            }
            if ((this.m_state & ItemState.ReadOnly) != 0)
            {
                this.label_color.BackColor = Color.LightGray;
                return;
            }

            this.label_color.BackColor = SystemColors.Window;
        }

        // ���ؼ����뵽tablelayoutpanel��
        internal void AddToTable(TableLayoutPanel table,
            int nRow)
        {
            table.Controls.Add(this.label_color, 0, nRow);
            table.Controls.Add(this.textBox_catalogNo, 1, nRow);
            table.Controls.Add(this.comboBox_seller, 2, nRow);
            table.Controls.Add(this.comboBox_source, 3, nRow);

            table.Controls.Add(this.dateRange_range, 4, nRow);
            table.Controls.Add(this.comboBox_issueCount, 5, nRow);

            table.Controls.Add(this.comboBox_copy, 6, nRow);
            table.Controls.Add(this.textBox_price, 7, nRow);
            table.Controls.Add(this.location, 8, nRow);
            table.Controls.Add(this.comboBox_class, 9, nRow);
            table.Controls.Add(this.label_sellerAddress, 10, nRow);
            table.Controls.Add(this.label_other, 11, nRow);

            AddEvents();
        }

        // ��tablelayoutpanel���Ƴ���Item�漰�Ŀؼ�
        // parameters:
        //      nRow    ��0��ʼ����
        internal void RemoveFromTable(TableLayoutPanel table,
            int nRow)
        {
            this.Container.DisableUpdate();

            try
            {

                // �Ƴ�������صĿؼ�
                table.Controls.Remove(this.label_color);
                table.Controls.Remove(this.textBox_catalogNo);
                table.Controls.Remove(this.comboBox_seller);
                table.Controls.Remove(this.comboBox_source);

                table.Controls.Remove(this.dateRange_range);
                table.Controls.Remove(this.comboBox_issueCount);

                table.Controls.Remove(this.comboBox_copy);
                table.Controls.Remove(this.textBox_price);
                table.Controls.Remove(this.location);
                table.Controls.Remove(this.comboBox_class);
                table.Controls.Remove(this.label_sellerAddress);
                table.Controls.Remove(this.label_other);

                Debug.Assert(this.Container.Items.Count == table.RowCount - 2, "");

                // Ȼ��ѹ���󷽵�
                for (int i = (table.RowCount - 2) - 1; i >= nRow + 1; i--)
                {
                    Item line = this.Container.Items[i];

                    // color
                    Label label = line.label_color;
                    table.Controls.Remove(label);
                    table.Controls.Add(label, 0, i - 1 + 1);

                    // catalog no
                    TextBox catalogNo = line.textBox_catalogNo;
                    table.Controls.Remove(catalogNo);
                    table.Controls.Add(catalogNo, 1, i - 1 + 1);

                    // seller
                    ComboBox seller = line.comboBox_seller;
                    table.Controls.Remove(seller);
                    table.Controls.Add(seller, 2, i - 1 + 1);

                    // source
                    DoubleComboBox source = line.comboBox_source;
                    table.Controls.Remove(source);
                    table.Controls.Add(source, 3, i - 1 + 1);

                    // time range
                    DateRangeControl range = line.dateRange_range;
                    table.Controls.Remove(range);
                    table.Controls.Add(range, 4, i - 1 + 1);

                    // issue count
                    ComboBox issueCount = line.comboBox_issueCount;
                    table.Controls.Remove(issueCount);
                    table.Controls.Add(issueCount, 5, i - 1 + 1);


                    // copy
                    DoubleComboBox copy = line.comboBox_copy;
                    table.Controls.Remove(copy);
                    table.Controls.Add(copy, 6, i - 1 + 1);

                    // price
                    DoubleTextBox price = line.textBox_price;
                    table.Controls.Remove(price);
                    table.Controls.Add(price, 7, i - 1 + 1);

                    // location
                    LocationEditControl location = line.location;
                    table.Controls.Remove(location);
                    table.Controls.Add(location, 8, i - 1 + 1);

                    // class
                    ComboBox orderClass = line.comboBox_class;
                    table.Controls.Remove(orderClass);
                    table.Controls.Add(orderClass, 9, i - 1 + 1);

                    // seller address
                    Label sellerAddress = line.label_sellerAddress;
                    table.Controls.Remove(sellerAddress);
                    table.Controls.Add(sellerAddress, 10, i - 1 + 1);

                    // other
                    Label other = line.label_other;
                    table.Controls.Remove(other);
                    table.Controls.Add(other, 11, i - 1 + 1);

                }

                table.RowCount--;
                table.RowStyles.RemoveAt(nRow);

            }
            finally
            {
                this.Container.EnableUpdate();
            }

        }

        // ���뱾Line��ĳ�С�����ǰ��table.RowCount�Ѿ�����
        // parameters:
        //      nRow    ��0��ʼ����
        internal void InsertToTable(TableLayoutPanel table,
            int nRow)
        {
            this.Container.DisableUpdate();

            try
            {

                Debug.Assert(table.RowCount == this.Container.Items.Count + 3, "");

                // ���ƶ��󷽵�
                for (int i = (table.RowCount - 1) - 3; i >= nRow; i--)
                {
                    Item line = this.Container.Items[i];

                    // color
                    Label label = line.label_color;
                    table.Controls.Remove(label);
                    table.Controls.Add(label, 0, i + 1 + 1);

                    // catalog no
                    TextBox catalogNo = line.textBox_catalogNo;
                    table.Controls.Remove(catalogNo);
                    table.Controls.Add(catalogNo, 1, i + 1 + 1);


                    // seller
                    ComboBox seller = line.comboBox_seller;
                    table.Controls.Remove(seller);
                    table.Controls.Add(seller, 2, i + 1 + 1);


                    // source
                    DoubleComboBox source = line.comboBox_source;
                    table.Controls.Remove(source);
                    table.Controls.Add(source, 3, i + 1 + 1);

                    // time range
                    DateRangeControl range = line.dateRange_range;
                    table.Controls.Remove(range);
                    table.Controls.Add(range, 4, i + 1 + 1);

                    // issue count
                    ComboBox issueCount = line.comboBox_issueCount;
                    table.Controls.Remove(issueCount);
                    table.Controls.Add(issueCount, 5, i + 1 + 1);

                    // copy
                    DoubleComboBox copy = line.comboBox_copy;
                    table.Controls.Remove(copy);
                    table.Controls.Add(copy, 6, i + 1 + 1);

                    // price
                    DoubleTextBox price = line.textBox_price;
                    table.Controls.Remove(price);
                    table.Controls.Add(price, 7, i + 1 + 1);

                    // location
                    table.Controls.Remove(line.location);
                    table.Controls.Add(line.location, 8, i + 1 + 1);

                    // class
                    ComboBox orderClass = line.comboBox_class;
                    table.Controls.Remove(orderClass);
                    table.Controls.Add(orderClass, 9, i + 1 + 1);

                    // seller address
                    table.Controls.Remove(line.label_sellerAddress);
                    table.Controls.Add(line.label_sellerAddress, 10, i + 1 + 1);

                    // other
                    table.Controls.Remove(line.label_other);
                    table.Controls.Add(line.label_other, 11, i + 1 + 1);
                }

                table.Controls.Add(this.label_color, 0, nRow + 1);
                table.Controls.Add(this.textBox_catalogNo, 1, nRow + 1);
                table.Controls.Add(this.comboBox_seller, 2, nRow + 1);
                table.Controls.Add(this.comboBox_source, 3, nRow + 1);

                table.Controls.Add(this.dateRange_range, 4, nRow + 1);
                table.Controls.Add(this.comboBox_issueCount, 5, nRow + 1);

                table.Controls.Add(this.comboBox_copy, 6, nRow + 1);
                table.Controls.Add(this.textBox_price, 7, nRow + 1);
                table.Controls.Add(this.location, 8, nRow + 1);
                table.Controls.Add(this.comboBox_class, 9, nRow + 1);
                table.Controls.Add(this.label_sellerAddress, 10, nRow + 1);
                table.Controls.Add(this.label_other, 11, nRow + 1);
            }
            finally
            {
                this.Container.EnableUpdate();
            }

            // events
            AddEvents();
        }


        void AddEvents()
        {
            // label_color
            this.label_color.MouseUp -= new MouseEventHandler(label_color_MouseUp);
            this.label_color.MouseUp += new MouseEventHandler(label_color_MouseUp);

            this.label_color.MouseClick -= new MouseEventHandler(label_color_MouseClick);
            this.label_color.MouseClick += new MouseEventHandler(label_color_MouseClick);

            // catalog no 
            this.textBox_catalogNo.Enter -= new EventHandler(control_Enter);
            this.textBox_catalogNo.Enter += new EventHandler(control_Enter);

            this.textBox_catalogNo.TextChanged -= new EventHandler(textBox_catalogNo_TextChanged);
            this.textBox_catalogNo.TextChanged += new EventHandler(textBox_catalogNo_TextChanged);

            // seller
            this.comboBox_seller.DropDown -= new EventHandler(comboBox_seller_DropDown);
            this.comboBox_seller.DropDown += new EventHandler(comboBox_seller_DropDown);

            this.comboBox_seller.Enter -= new EventHandler(control_Enter);
            this.comboBox_seller.Enter += new EventHandler(control_Enter);

            this.comboBox_seller.TextChanged -= new EventHandler(comboBox_seller_TextChanged);
            this.comboBox_seller.TextChanged += new EventHandler(comboBox_seller_TextChanged);

            this.comboBox_seller.SelectedIndexChanged -= new EventHandler(comboBox_seller_SelectedIndexChanged);
            this.comboBox_seller.SelectedIndexChanged += new EventHandler(comboBox_seller_SelectedIndexChanged);

            // source
            this.comboBox_source.ComboBox.DropDown -= new EventHandler(comboBox_seller_DropDown);
            this.comboBox_source.ComboBox.DropDown += new EventHandler(comboBox_seller_DropDown);

            this.comboBox_source.Enter -= new EventHandler(control_Enter);
            this.comboBox_source.Enter += new EventHandler(control_Enter);

            this.comboBox_source.ComboBox.TextChanged -= new EventHandler(comboBox_source_TextChanged);
            this.comboBox_source.ComboBox.TextChanged += new EventHandler(comboBox_source_TextChanged);

            this.comboBox_source.SelectedIndexChanged -= new EventHandler(comboBox_seller_SelectedIndexChanged);
            this.comboBox_source.SelectedIndexChanged += new EventHandler(comboBox_seller_SelectedIndexChanged);

            // 2012/5/26
            // range
            this.dateRange_range.DateTextChanged -= new EventHandler(dateRange_range_DateTextChanged);
            this.dateRange_range.DateTextChanged += new EventHandler(dateRange_range_DateTextChanged);

            this.dateRange_range.Enter -= new EventHandler(control_Enter);
            this.dateRange_range.Enter += new EventHandler(control_Enter);

            // issuecount
            this.comboBox_issueCount.TextChanged -= new EventHandler(comboBox_issueCount_TextChanged);
            this.comboBox_issueCount.TextChanged +=new EventHandler(comboBox_issueCount_TextChanged);

            this.comboBox_issueCount.Enter -= new EventHandler(control_Enter);
            this.comboBox_issueCount.Enter += new EventHandler(control_Enter);

            // copy
            this.comboBox_copy.ComboBox.DropDown -= new EventHandler(comboBox_copy_DropDown);
            this.comboBox_copy.ComboBox.DropDown += new EventHandler(comboBox_copy_DropDown);

            this.comboBox_copy.Enter -= new EventHandler(control_Enter);
            this.comboBox_copy.Enter += new EventHandler(control_Enter);

            this.comboBox_copy.ComboBox.TextChanged -= new EventHandler(comboBox_copy_TextChanged);
            this.comboBox_copy.ComboBox.TextChanged += new EventHandler(comboBox_copy_TextChanged);

            // price
            this.textBox_price.TextBox.TextChanged -= new EventHandler(Price_TextChanged);
            this.textBox_price.TextBox.TextChanged += new EventHandler(Price_TextChanged);

            this.textBox_price.TextBox.Enter -= new EventHandler(control_Enter);
            this.textBox_price.TextBox.Enter += new EventHandler(control_Enter);

            // location
            this.location.GetValueTable -= new GetValueTableEventHandler(textBox_location_GetValueTable);
            this.location.GetValueTable += new GetValueTableEventHandler(textBox_location_GetValueTable);

            this.location.Enter -= new EventHandler(control_Enter);
            this.location.Enter += new EventHandler(control_Enter);

            this.location.ContentChanged -= new ContentChangedEventHandler(location_ContentChanged);
            this.location.ContentChanged += new ContentChangedEventHandler(location_ContentChanged);

            this.location.ArrivedChanged -= new EventHandler(location_ArrivedChanged);
            this.location.ArrivedChanged += new EventHandler(location_ArrivedChanged);

            this.location.ReadOnlyChanged -= new EventHandler(location_ReadOnlyChanged);
            this.location.ReadOnlyChanged += new EventHandler(location_ReadOnlyChanged);

            // class
            this.comboBox_class.DropDown -= new EventHandler(comboBox_seller_DropDown);
            this.comboBox_class.DropDown += new EventHandler(comboBox_seller_DropDown);

            this.comboBox_class.Enter -= new EventHandler(control_Enter);
            this.comboBox_class.Enter += new EventHandler(control_Enter);

            this.comboBox_class.TextChanged -=new EventHandler(comboBox_class_TextChanged);
            this.comboBox_class.TextChanged += new EventHandler(comboBox_class_TextChanged);

            this.comboBox_class.SelectedIndexChanged -= new EventHandler(comboBox_seller_SelectedIndexChanged);
            this.comboBox_class.SelectedIndexChanged += new EventHandler(comboBox_seller_SelectedIndexChanged);

            // address
            this.label_sellerAddress.Click -= new EventHandler(control_Enter);
            this.label_sellerAddress.Click += new EventHandler(control_Enter);

            // other
            this.label_other.Click -= new EventHandler(control_Enter);
            this.label_other.Click += new EventHandler(control_Enter);
        }

        // ���˵� {} ��Χ�Ĳ���
        static string GetPureSeletedValue(string strText)
        {
            for (; ; )
            {
                int nRet = strText.IndexOf("{");
                if (nRet == -1)
                    return strText;
                int nStart = nRet;
                nRet = strText.IndexOf("}", nStart + 1);
                if (nRet == -1)
                    return strText;
                int nEnd = nRet;
                strText = strText.Remove(nStart, nEnd - nStart + 1).Trim();
            }
        }

        delegate void Delegate_filterValue(Control control);

        // ����ȫ�汾
        // ���˵� {} ��Χ�Ĳ���
        void __FilterValue(Control control)
        {
            if (control is DoubleComboBox)
            {
                DoubleComboBox combox = (DoubleComboBox)control;
                string strText = GetPureSeletedValue(combox.Text);
                if (combox.Text != strText)
                    combox.Text = strText;
            }
            else
            {
                string strText = GetPureSeletedValue(control.Text);
                if (control.Text != strText)
                    control.Text = strText;
            }
        }

#if NO
        // ��ȫ�汾
        void FilterValue(Control control)
        {
            if (this.Container.InvokeRequired == true)
            {
                Delegate_filterValue d = new Delegate_filterValue(__FilterValue);
                this.Container.BeginInvoke(d, new object[] { control });
            }
            else
            {
                __FilterValue((Control)control);
            }
        }
#endif
        // ��ȫ�汾
        void FilterValue(Control control)
        {
            Delegate_filterValue d = new Delegate_filterValue(__FilterValue);

            if (this.Container.Created == false)
                __FilterValue((Control)control);
            else
                this.Container.BeginInvoke(d, new object[] { control });
        }

        void comboBox_seller_SelectedIndexChanged(object sender, EventArgs e)
        {
            FilterValue((Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(__FilterValue);
            this.Container.BeginInvoke(d, new object[] { sender });
#endif
        }

        void comboBox_issueCount_TextChanged(object sender, EventArgs e)
        {
            if ((this.State & ItemState.New) == 0)
                this.State |= ItemState.Changed;

            this.Container.Changed = true;
        }

        void dateRange_range_DateTextChanged(object sender, EventArgs e)
        {

            if ((this.State & ItemState.New) == 0)
                this.State |= ItemState.Changed;

            this.Container.Changed = true;
        }



        #region events

        // 2008/9/13
        void location_ReadOnlyChanged(object sender, EventArgs e)
        {
            if (this.Container.ArriveMode == false)
            {
                this.Container.DisableNewlyOrderTextChanged++;    // �Ż����������Ķ���
                this.Container.textBox_newlyOrderTotalCopy.Text = this.Container.GetNewlyOrderTotalCopy().ToString();
                this.Container.DisableNewlyOrderTextChanged--;

                this.Container.textBox_orderedTotalCopy.Text = this.Container.GetOrderedTotalCopy().ToString();
            }
            else
            {
                this.Container.DisableNewlyArriveTextChanged++;    // �Ż����������Ķ���
                this.Container.textBox_newlyArriveTotalCopy.Text = this.Container.GetNewlyArriveTotalCopy().ToString();
                this.Container.DisableNewlyArriveTextChanged--;

                this.Container.textBox_arrivedTotalCopy.Text = this.Container.GetArrivedTotalCopy().ToString();
            }

        }

        void location_ArrivedChanged(object sender, EventArgs e)
        {
            // ���������޸ģ����ⲻ��Ҫ�ش�����������¼�
            if (this.DisableLocationArrivedChanged > 0)
                return;

            UpdateCopyCount();
        }

        // ������location checked״̬�л����ѵ��Ĳ���
        public void UpdateCopyCount()
        {
            string strCount = this.location.ArrivedCount.ToString();

            // 2010/12/1
            string strCopy = OrderDesignControl.GetCopyFromCopyString(this.comboBox_copy.Text);

            if (strCopy != strCount)
            {
                // �������copy�ַ���Ϊ�գ�����Ҫ�Ӷ���copy�ַ�����Ѱ�ҿ��ܵ����ڲ���
                if (String.IsNullOrEmpty(this.comboBox_copy.Text) == true)
                {
                    string strRightCopy = OrderDesignControl.GetRightFromCopyString(this.comboBox_copy.OldText);
                    if (String.IsNullOrEmpty(strRightCopy) == false)
                    {
                        this.comboBox_copy.Text = OrderDesignControl.ModifyCopy(this.comboBox_copy.Text, strCount);
                        this.comboBox_copy.Text = OrderDesignControl.ModifyRightCopy(this.comboBox_copy.Text, strRightCopy);
                        return;
                    }
                }


                this.comboBox_copy.Text = OrderDesignControl.ModifyCopy(this.comboBox_copy.Text, strCount);
            }


            /*
            if (strCount != this.comboBox_copy.Text)
                this.comboBox_copy.Text = strCount;
             * */
        }

        void location_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            if ((this.State & ItemState.New) == 0)
                this.State |= ItemState.Changed;

            this.Container.Changed = true;
        }

        void comboBox_source_TextChanged(object sender, EventArgs e)
        {
            if (this.Container.ArriveMode == false)
            {
                // �ڶ���״̬�£��¾�ֵ����ͳһ���Ա���ʾ����
                this.comboBox_source.OldText = this.comboBox_source.Text;
            }

            if ((this.State & ItemState.New) == 0)
                this.State |= ItemState.Changed;

            this.Container.Changed = true;

            // 2009/2/15
            // ���seller��sourceì�ܣ���seller��Ϊ��
            if (this.comboBox_seller.Text == "����"
                || this.comboBox_seller.Text == "��")
            {
                if (String.IsNullOrEmpty(this.comboBox_source.Text) == false)
                    this.comboBox_seller.Text = "";
            }

        }

        void Price_TextChanged(object sender, EventArgs e)
        {
            if (this.Container.ArriveMode == false)
            {
                // �ڶ���״̬�£��¾�ֵ����ͳһ���Ա���ʾ����
                this.textBox_price.OldText = this.textBox_price.Text;
            }

            if ((this.State & ItemState.New) == 0)
                this.State |= ItemState.Changed;

            this.Container.Changed = true;
        }

        void comboBox_seller_TextChanged(object sender, EventArgs e)
        {
            if ((this.State & ItemState.New) == 0)
                this.State |= ItemState.Changed;

            this.Container.Changed = true;

            // 2009/2/15
            if (this.comboBox_seller.Text == "����"
                || this.comboBox_seller.Text == "��")
                this.comboBox_source.Text = "";
        }

        void comboBox_class_TextChanged(object sender, EventArgs e)
        {
            if ((this.State & ItemState.New) == 0)
                this.State |= ItemState.Changed;

            this.Container.Changed = true;
        }



        void textBox_catalogNo_TextChanged(object sender, EventArgs e)
        {
            if ((this.State & ItemState.New) == 0)
                this.State |= ItemState.Changed;

            this.Container.Changed = true;
        }

        void control_Enter(object sender, EventArgs e)
        {
            this.Container.SelectItem(this, true);
        }

        void label_color_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (Control.ModifierKeys == Keys.Control)
                {
                    this.Container.ToggleSelectItem(this);
                }
                else if (Control.ModifierKeys == Keys.Shift)
                    this.Container.RangeSelectItem(this);
                else
                {
                    this.Container.SelectItem(this, true);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // �����ǰ�ж���ѡ���򲻱���ʲôl
                // �����ǰΪ����һ��ѡ�����0��ѡ����ѡ��ǰ����
                // ��������Ŀ���Ƿ������
                if (this.Container.SelectedIndices.Count < 2)
                {
                    this.Container.SelectItem(this, true);
                }
            }
        }

        void textBox_location_GetValueTable(object sender, GetValueTableEventArgs e)
        {
            this.Container.OnGetValueTable(sender, e);
        }

        // ������ ���ָı�
        void comboBox_copy_TextChanged(object sender, EventArgs e)
        {
            if (this.Container.ArriveMode == false)
            {
                // �ڶ���״̬�£��¾�ֵ����ͳһ���Ա���ʾ����
                this.comboBox_copy.OldText = this.comboBox_copy.Text;
            }

            try
            {
                // location�ؼ�����
                // 2010/12/1 changed
                int nCopy = Convert.ToInt32(OrderDesignControl.GetCopyFromCopyString(this.comboBox_copy.Text));

                // �����ǰΪ����ģʽ
                if (this.Container.ArriveMode == false)
                {
                    this.location.Count = nCopy;

                    // ����ֵ�����仯
                    if ((this.State & ItemState.ReadOnly) == 0)
                    {
                        this.Container.DisableNewlyOrderTextChanged++;    // �Ż����������Ķ���
                        this.Container.textBox_newlyOrderTotalCopy.Text = this.Container.GetNewlyOrderTotalCopy().ToString();
                        this.Container.DisableNewlyOrderTextChanged--;
                    }
                    else
                    {
                        this.Container.textBox_orderedTotalCopy.Text = this.Container.GetOrderedTotalCopy().ToString();
                    }
                }
                else
                {
                    // �����ǰΪ����ģʽ


                    // ����̫���ֵ
                    // 2008/9/17
                    int nDelta = nCopy - this.location.ArrivedCount;
                    if (nDelta > 10)
                    {
                        DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
                            "ȷʵҪ���� " + nCopy.ToString() + " ��ô���ֵ?",
                            "OrderDesignControl",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                        if (result != DialogResult.Yes)
                        {
                            // 2010/12/1 changed
                            // this.comboBox_copy.Text = this.location.ArrivedCount.ToString();    // �ָ�ԭ����ֵ����������õ�ֵ
                            this.comboBox_copy.Text = OrderDesignControl.ModifyCopy(
                                this.comboBox_copy.Text, this.location.ArrivedCount.ToString());    // �ָ�ԭ����ֵ����������õ�ֵ
                            return;
                        }
                    }


                    // �����޸�location�ؼ���ArrivedCount����Ҫ����ݹ鴦���ɴ�������¼�
                    this.DisableLocationArrivedChanged++;
                    try
                    {
                        this.location.ArrivedCount = nCopy;
                    }
                    catch (NotEnoughException ex)
                    {
                        MessageBox.Show(this.Container, ex.Message);

                        // 2008/9/16
                        // this.comboBox_copy.Text = this.location.ArrivedCount.ToString(); 
                        // �ָ�ԭ����ֵ����������õ�ֵ
                        // 2010/12/1 changed
                        this.comboBox_copy.Text = OrderDesignControl.ModifyCopy(
                            this.comboBox_copy.Text, this.location.ArrivedCount.ToString());    // �ָ�ԭ����ֵ����������õ�ֵ
                        return;
                    }
                    finally
                    {
                        this.DisableLocationArrivedChanged--;
                    }

                    // ����ֵ�����仯
                    if ((this.State & ItemState.ReadOnly) == 0)
                    {
                        this.Container.DisableNewlyArriveTextChanged++;    // �Ż����������Ķ���
                        this.Container.textBox_newlyArriveTotalCopy.Text = this.Container.GetNewlyArriveTotalCopy().ToString();
                        this.Container.DisableNewlyArriveTextChanged--;
                    }
                    else
                    {
                        this.Container.textBox_arrivedTotalCopy.Text = this.Container.GetArrivedTotalCopy().ToString();
                    }
                }
            }
            catch
            {
                return;
            }

            if ((this.State & ItemState.New) == 0)
                this.State |= ItemState.Changed;

            this.Container.Changed = true;
        }

        void comboBox_seller_DropDown(object sender, EventArgs e)
        {
            // ��ֹ���� 2009/1/15
            if (this.m_nInDropDown > 0)
                return;

            Cursor oldCursor = this.Container.Cursor;
            this.Container.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {

                ComboBox combobox = null;

                if (sender is DoubleComboBox)
                    combobox = ((DoubleComboBox)sender).ComboBox;
                else
                    combobox = (ComboBox)sender;

                if (combobox.Items.Count == 0
                    && this.Container.HasGetValueTable() != false)
                {
                    GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                    e1.DbName = this.Container.BiblioDbName;

                    if (combobox == this.comboBox_seller)
                        e1.TableName = "orderSeller";
                    else if (combobox == this.comboBox_class)
                        e1.TableName = "orderClass";
                    else if (combobox == this.comboBox_source.ComboBox)
                        e1.TableName = "orderSource";
                    else if (combobox == this.comboBox_copy.ComboBox)
                        e1.TableName = "orderCopy";
                    else
                    {
                        Debug.Assert(false, "��֧�ֵ�sender");
                        return;
                    }

                    this.Container.OnGetValueTable(this, e1);

                    if (e1.values != null)
                    {
                        for (int i = 0; i < e1.values.Length; i++)
                        {
                            combobox.Items.Add(e1.values[i]);
                        }
                    }
                    else
                    {
                        combobox.Items.Add("<not found>");
                    }
                }
            }
            finally
            {
                this.Container.Cursor = oldCursor;
                this.m_nInDropDown--;
            }
        }

        void comboBox_copy_DropDown(object sender, EventArgs e)
        {
            ComboBox combobox = (ComboBox)sender;

            if (combobox.Items.Count == 0)
            {
                    for (int i = 0; i < 10; i++)
                    {
                        combobox.Items.Add((i+1).ToString());
                    }
            }
        }



        void label_color_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            int nSelectedCount = this.Container.SelectedIndices.Count;
            /*
            bool bHasClipboardObject = false;
            IDataObject ido = Clipboard.GetDataObject();
            if (ido.GetDataPresent(DataFormats.Text) == true)
                bHasClipboardObject = true;
             * */

            //
            menuItem = new MenuItem("ǰ��(&I)");
            menuItem.Click += new System.EventHandler(this.menu_insertElement_Click);
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("���(&A)");
            menuItem.Click += new System.EventHandler(this.menu_appendElement_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            if (this.Container.ArriveMode == false)
            {
                menuItem = new MenuItem("������������(&S)");
                menuItem.Click += new System.EventHandler(this.menu_specialOrder_Click);
                contextMenu.MenuItems.Add(menuItem);

                // ---
                menuItem = new MenuItem("-");
                contextMenu.MenuItems.Add(menuItem);
            }


            menuItem = new MenuItem("�ܼ�(&T)");
            menuItem.Click += new System.EventHandler(this.menu_totalPrice_Click);
            contextMenu.MenuItems.Add(menuItem);


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            //
            menuItem = new MenuItem("ɾ��(&D)");
            menuItem.Click += new System.EventHandler(this.menu_deleteElements_Click);
            contextMenu.MenuItems.Add(menuItem);

            /*
            menuItem = new MenuItem("test");
            menuItem.Click += new System.EventHandler(this.menu_test_Click);
            contextMenu.MenuItems.Add(menuItem);
             * */

            contextMenu.Show(this.label_color, new Point(e.X, e.Y));
        }


        #endregion

        void menu_test_Click(object sender, EventArgs e)
        {
            this.EnsureVisible();
        }

        void menu_totalPrice_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.StateString) == false)
            {
                MessageBox.Show(this.Container, "����״̬Ϊ�ǿյĶ�������������޸����ܼ۸�");
                return;
            }

            try
            {
                string strTotalPrice = this.TotalPrice;
                string strNewTotalPrice = InputDlg.GetInput(
                    this.Container,
                    "�������ܼ۸�",
                    "�ܼ۸�: ",
                    strTotalPrice,
                    this.Container.Font);
                if (strNewTotalPrice == null)
                    return;

                this.TotalPrice = strNewTotalPrice;

                // ����������ܼ۸����۸�Ϊ��
                if (String.IsNullOrEmpty(strNewTotalPrice) == false)
                    this.Price = "";

                // ˢ����ʾ
                this.OtherXml = this.OtherXml;

                if ((this.State & ItemState.New) == 0)
                    this.State |= ItemState.Changed;

                this.Container.Changed = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.Container, ex.Message);
            }
        }

        // ������������
        void menu_specialOrder_Click(object sender, EventArgs e)
        {
            List<Item> selected_lines = this.Container.SelectedItems;

            if (selected_lines.Count == 0)
            {
                MessageBox.Show(this.Container, "��δѡ��Ҫ����������������������");
                return;
            }

            if (selected_lines.Count > 1)
            {
                // ���Ҫ�޸ĵĶ���һ���������
                DialogResult result = MessageBox.Show(this.Container,
                     "ȷʵҪͬʱ�༭ " + selected_lines.Count.ToString() + " ���е�����������������?",
                     "OrderDesignControl",
                     MessageBoxButtons.OKCancel,
                     MessageBoxIcon.Question,
                     MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Cancel)
                    return;
            } 

            SpecialSourceSeriesDialog dlg = new SpecialSourceSeriesDialog();
            GuiUtil.SetControlFont(dlg, this.Container.Font, false);

            dlg.DbName = this.Container.BiblioDbName;
            dlg.Seller = selected_lines[0].Seller;
            dlg.Source = selected_lines[0].Source;
            dlg.AddressXml = selected_lines[0].SellerAddressXml;

            dlg.GetValueTable -= new GetValueTableEventHandler(dlg_GetValueTable);
            dlg.GetValueTable += new GetValueTableEventHandler(dlg_GetValueTable);

            // TODO: ��α���Ի����޸ĺ�Ĵ�С��λ��?
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this.Container);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            int nNotChangeCount = 0;
            for (int i = 0; i < selected_lines.Count; i++)
            {
                Item item = selected_lines[i];

                if ((item.State & ItemState.ReadOnly) != 0)
                {
                    nNotChangeCount++;
                    continue;
                }

                item.Source = dlg.Source;
                item.Seller = dlg.Seller;
                item.SellerAddressXml = dlg.AddressXml;
            }

            if (nNotChangeCount > 0)
                MessageBox.Show(this.Container, "�� " + nNotChangeCount.ToString() + " ��ֻ��״̬����û�б��޸�");
        }

        void dlg_GetValueTable(object sender, GetValueTableEventArgs e)
        {
            this.Container.OnGetValueTable(sender, e);
        }

        void menu_insertElement_Click(object sender, EventArgs e)
        {
            int nPos = this.Container.Items.IndexOf(this);

            if (nPos == -1)
                throw new Exception("not found myself");

            this.Container.InsertNewItem(nPos).EnsureVisible();
        }

        void menu_appendElement_Click(object sender, EventArgs e)
        {
            int nPos = this.Container.Items.IndexOf(this);
            if (nPos == -1)
            {
                throw new Exception("not found myself");
            }

            this.Container.InsertNewItem(nPos + 1).EnsureVisible();
        }

        // ɾ����ǰԪ��
        void menu_deleteElements_Click(object sender, EventArgs e)
        {
            List<Item> selected_lines = this.Container.SelectedItems;

            if (selected_lines.Count == 0)
            {
                MessageBox.Show(this.Container, "��δѡ��Ҫɾ��������");
                return;
            }
            string strText = "";

            if (selected_lines.Count == 1)
                strText = "ȷʵҪɾ������ '" + selected_lines[0].ItemCaption + "'? ";
            else
                strText = "ȷʵҪɾ����ѡ���� " + selected_lines.Count.ToString() + " ������?";

            DialogResult result = MessageBox.Show(this.Container,
                strText,
                "OrderCrossControl",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                return;
            }

            int nNotDeleteCount = 0;
            this.Container.DisableUpdate();
            try
            {
                for (int i = 0; i < selected_lines.Count; i++)
                {
                    Item item = selected_lines[i];
                    if ((item.State & ItemState.ReadOnly) != 0)
                    {
                        nNotDeleteCount++;
                        continue;
                    }
                    this.Container.RemoveItem(item);
                }
            }
            finally
            {
                this.Container.EnableUpdate();
            }

            if (nNotDeleteCount > 0)
                MessageBox.Show(this.Container, "�� " + nNotDeleteCount.ToString() + " ���Ѷ���״̬������δ��ɾ��");
        }

        #region �ⲿ��Ҫʹ�õ�����

        // ��Ŀ��
        public string CatalogNo
        {
            get
            {
                return this.textBox_catalogNo.Text;
            }
            set
            {
                this.textBox_catalogNo.Text = value;
            }
        }

        // ����
        public string Seller
        {
            get
            {
                return this.comboBox_seller.Text;
            }
            set
            {
                this.comboBox_seller.Text = value;
            }
        }

        // ������Դ
        public string Source
        {
            get
            {
                return this.comboBox_source.Text;
            }
            set
            {
                this.comboBox_source.Text = value;
            }
        }

        // ԭ�еľ�����Դ
        public string OldSource
        {
            get
            {
                return this.comboBox_source.OldText;
            }
            set
            {
                this.comboBox_source.OldText = value;
            }
        }

        // ���ڷ�Χ
        // Exception: set��ʱ����ܻ��׳��쳣
        public string RangeString
        {
            get
            {
                return this.dateRange_range.Text;
            }
            set
            {
                // ���ܻ��׳��쳣
                this.dateRange_range.Text = value;
            }

        }

        // ���һ������ʱ���Ƿ���RangeString��ʱ�䷶Χ��?
        // Exception: �п����׳��쳣
        // parameters:
        //      strPublishTime  4/6/8�ַ�
        //      strRange    ��ʽΪ"20080101-20081231"
        public bool InRange(string strPublishTime)
        {
            try
            {
                if (strPublishTime.Length == 4)
                    strPublishTime += "0101";
                else if (strPublishTime.Length == 6)
                    strPublishTime += "01";

                string strRange = this.RangeString;

                if (string.IsNullOrEmpty(strRange) == true)
                    return false;

                int nRet = strRange.IndexOf("-");

                string strStart = strRange.Substring(0, nRet).Trim();
                string strEnd = strRange.Substring(nRet + 1).Trim();

                if (strStart.Length != 8)
                    throw new Exception("ʱ�䷶Χ�ַ��� '" + strRange + "' ����߲��� '" + strStart + "' ��ʽ����ӦΪ8�ַ�");

                if (strEnd.Length != 8)
                    throw new Exception("ʱ�䷶Χ�ַ��� '" + strRange + "' ���ұ߲��� '" + strEnd + "' ��ʽ����ӦΪ8�ַ�");

                if (String.Compare(strPublishTime, strStart) < 0)
                    return false;

                if (String.Compare(strPublishTime, strEnd) > 0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ��������
        public int IssueCountValue
        {
            get
            {
                try
                {
                    return Convert.ToInt32(this.comboBox_issueCount.Text);
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                this.comboBox_issueCount.Text = value.ToString();
            }
        }

        // �����ַ���
        public string IssueCountString
        {
            get
            {
                return this.comboBox_issueCount.Text;
            }
            set
            {
                this.comboBox_issueCount.Text = value;
            }
        }

        // ����������
        public int CopyValue
        {
            get
            {
                try
                {
                    // 2010/12/1 changed
                    return Convert.ToInt32(OrderDesignControl.GetCopyFromCopyString(this.comboBox_copy.Text));
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                // 2010/12/1 changed
                // this.comboBox_copy.Text = value.ToString();
                this.comboBox_copy.Text = OrderDesignControl.ModifyCopy(this.comboBox_copy.Text, value.ToString());
            }
        }

        // �������ַ���
        public string CopyString
        {
            get
            {
                return this.comboBox_copy.Text;
            }
            set
            {
                this.comboBox_copy.Text = value;
            }
        }

        // ԭ�е� ����������
        // 2008/9/12
        public int OldCopyValue
        {
            get
            {
                try
                {
                    // 2010/12/1 changed
                    return Convert.ToInt32(OrderDesignControl.GetCopyFromCopyString(this.comboBox_copy.OldText));
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                // 2010/12/1 changed
                // this.comboBox_copy.OldText = value.ToString();
                this.comboBox_copy.OldText = OrderDesignControl.ModifyCopy(this.comboBox_copy.OldText, value.ToString());
            }
        }

        // ԭ�е� �������ַ���
        public string OldCopyString
        {
            get
            {
                return this.comboBox_copy.OldText;
            }
            set
            {
                this.comboBox_copy.OldText = value;
            }
        }

        // ����
        public string Price
        {
            get
            {
                return this.textBox_price.Text;
            }
            set
            {
                this.textBox_price.Text = value;
            }
        }

        // ԭ�е� ����
        public string OldPrice
        {
            get
            {
                return this.textBox_price.OldText;
            }
            set
            {
                this.textBox_price.OldText = value;
            }
        }

        // �ݲصص�����ĸ��� 2008/9/12
        public int DistributeCount
        {
            get
            {
                return this.location.Count;
            }
            set
            {
                this.location.Count = value;
            }
        }

        // ȥ�򣬹ݲط������
        public string Distribute
        {
            get
            {
                return this.location.Value;
            }
            set
            {
                // �ݲط����ַ������޸Ļᵼ����count�ı䣬������Ӱ�쵽ͬһ�����Copyֵ
                this.location.Value = value;
            }
        }

        public string OtherXml
        {
            get
            {
                return this.m_otherXml;
            }
            set
            {
                this.m_otherXml = value;
                string strError = "";
                int nRet = DisplayOtherXml(value,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);
            }
        }

        public string SellerAddressXml
        {
            get
            {
                return this.m_sellerAddressXml;
            }
            set
            {
                this.m_sellerAddressXml = value;
                string strError = "";
                int nRet = DisplaySellerAddressXml(value,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);
            }
        }

        // ���
        public string Class
        {
            get
            {
                return this.comboBox_class.Text;
            }
            set
            {
                this.comboBox_class.Text = value;
            }
        }

        public string Index
        {
            get
            {
                if (String.IsNullOrEmpty(this.OtherXml) == true)
                    return "";

                string strError = "";

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(this.OtherXml);
                }
                catch (Exception ex)
                {
                    strError = "XML��¼װ��DOMʱ����: " + ex.Message;
                    throw new Exception(strError);
                }

                // ��ȡ��������ݣ������ı���ʾ
                return DomUtil.GetElementText(dom.DocumentElement,
                    "index");
            }
            set
            {
                if (String.IsNullOrEmpty(this.OtherXml) == true)
                    this.OtherXml = "<root />";

                string strError = "";

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(this.OtherXml);
                }
                catch (Exception ex)
                {
                    strError = "XML��¼װ��DOMʱ����: " + ex.Message;
                    throw new Exception(strError);
                }

                // ��ȡ��������ݣ������ı���ʾ
                DomUtil.SetElementText(dom.DocumentElement,
                    "index", value);

                this.OtherXml = dom.OuterXml;
                // ���Զ�ˢ����ʾ

            }
        }

        // 2008/11/12
        string m_strStateString = "";

        public string StateString
        {
            get
            {
                return m_strStateString;
            }
        }

        int DisplaySellerAddressXml(string strXml,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strXml) == true)
            {
                this.label_sellerAddress.Text = "";
                return 0;
            }

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

            if (dom.DocumentElement == null
                || dom.DocumentElement.ChildNodes.Count == 0)
            {
                this.label_sellerAddress.Text = "";
                return 0;
            }

            // ��ȡ��������ݣ������ı���ʾ
            string strZipcode = DomUtil.GetElementText(dom.DocumentElement,
                "zipcode");
            string strAddress = DomUtil.GetElementText(dom.DocumentElement,
                "address");
            string strDepartment = DomUtil.GetElementText(dom.DocumentElement,
                "department");
            string strName = DomUtil.GetElementText(dom.DocumentElement,
                "name");
            string strTel = DomUtil.GetElementText(dom.DocumentElement,
                "tel");
            string strEmail = DomUtil.GetElementText(dom.DocumentElement,
                "email");
            string strBank = DomUtil.GetElementText(dom.DocumentElement,
                "bank");
            string strAccounts = DomUtil.GetElementText(dom.DocumentElement,
                "accounts");
            string strPayStyle = DomUtil.GetElementText(dom.DocumentElement,
                "payStyle");
            string strComment = DomUtil.GetElementText(dom.DocumentElement,
                "comment");

            this.label_sellerAddress.Text = "��������: \t" + strZipcode + "\r\n"
            + "��ַ: \t" + strAddress + "\r\n"
            + "��λ��: \t" + strDepartment + "\r\n"
            + "��ϵ��: \t" + strName + "\r\n"
            + "�绰: \t" + strTel + "\r\n"
            + "Email: \t" + strEmail + "\r\n"
            + "������: \t" + strBank + "\r\n"
            + "�����˺�: \t" + strAccounts + "\r\n"
            + "��ʽ: \t" + strPayStyle + "\r\n"
            + "ע��: \t" + strComment + "\r\n";

            return 0;
        }

        int DisplayOtherXml(string strXml,
            out string strError)
        {
            strError = "";

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
            string strIndex = DomUtil.GetElementText(dom.DocumentElement,
                "index");
            string strState = DomUtil.GetElementText(dom.DocumentElement,
                "state");

            m_strStateString = strState;

            string strRange = DomUtil.GetElementText(dom.DocumentElement,
                "range");
            string strOrderTime = DateTimeUtil.LocalTime(DomUtil.GetElementText(dom.DocumentElement,
                "orderTime"));   // 2008/12/17 changed
            string strOrderID = DomUtil.GetElementText(dom.DocumentElement,
                "orderID"); 
            string strComment = DomUtil.GetElementText(dom.DocumentElement,
                "comment");
            string strBatchNo = DomUtil.GetElementText(dom.DocumentElement,
                "batchNo");
            /*
            string strIssueCount = DomUtil.GetElementText(dom.DocumentElement,
                "issueCount");
             * */
            string strTotalPrice = DomUtil.GetElementText(dom.DocumentElement,
                "totalPrice");

            this.label_other.Text = "���: \t" + strIndex + "\r\n"
            + "״̬: \t" + strState + "\r\n"
                // + "ʱ�䷶Χ:\t" + strRange + "\r\n"
            + "����ʱ��: \t" + strOrderTime + "\r\n"
            + "������: \t" + strOrderID + "\r\n"
            + "�ܼ۸�: \t" + strTotalPrice + "\r\n"
            + "ע��: \t" + strComment + "\r\n"
            + "���κ�: \t" + strBatchNo + "\r\n";

            return 0;
        }

        // ���ܻ��׳��쳣
        public string TotalPrice
        {
            get
            {
                XmlDocument dom = new XmlDocument();
                if (String.IsNullOrEmpty(this.m_otherXml) == false)
                {
                    try
                    {
                        dom.LoadXml(this.m_otherXml);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("load other xml error: " + ex.Message);
                    }
                    return DomUtil.GetElementText(dom.DocumentElement,
                        "totalPrice");
                }
                else
                    return "";
            }
            set
            {
                XmlDocument dom = new XmlDocument();
                if (String.IsNullOrEmpty(this.m_otherXml) == false)
                {
                    try
                    {
                        dom.LoadXml(this.m_otherXml);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("load other xml error: " + ex.Message);
                    }
                }
                else
                    dom.LoadXml("<root />");

                DomUtil.SetElementText(dom.DocumentElement,
                    "totalPrice", value);

                this.m_otherXml = dom.DocumentElement.OuterXml;
            }
        }

        // ��ñ�ʾ����ȫ�����ݵ�XML��¼
        public int BuildXml(out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            XmlDocument dom = new XmlDocument();
            if (String.IsNullOrEmpty(this.m_otherXml) == false)
            {
                try
                {
                    dom.LoadXml(this.m_otherXml);
                }
                catch (Exception ex)
                {
                    strError = "load other xml error: " + ex.Message;
                    return -1;
                }
            }
            else
                dom.LoadXml("<root />");

            /*
             * other xml���Ѿ�������������:
                "index"
                "state"
                "range"
                "orderTime"
                "orderID"
                "comment"
                "batchNo"
                "totalPrice" 
             * */

            DomUtil.SetElementText(dom.DocumentElement,
                "catalogNo", this.CatalogNo);
            DomUtil.SetElementText(dom.DocumentElement,
                "seller", this.Seller);
            DomUtil.SetElementText(dom.DocumentElement,
                "source", OrderDesignControl.LinkOldNewValue(this.OldSource, this.Source));
            DomUtil.SetElementText(dom.DocumentElement,
                "range", this.RangeString);
            DomUtil.SetElementText(dom.DocumentElement,
                "issueCount", this.IssueCountString);
            DomUtil.SetElementText(dom.DocumentElement,
                "copy", OrderDesignControl.LinkOldNewValue(this.OldCopyString, this.CopyString));
            DomUtil.SetElementText(dom.DocumentElement,
                "price", OrderDesignControl.LinkOldNewValue(this.OldPrice, this.Price));
            DomUtil.SetElementText(dom.DocumentElement,
                "distribute", this.Distribute);
            DomUtil.SetElementText(dom.DocumentElement,
                "class", this.Class);

            strXml = dom.OuterXml;

            return 0;
        }

#if NO
        // ��RFC1123ʱ���ַ���ת��Ϊ����һ��ʱ���ַ���
        // exception: �����׳��쳣
        public static string LocalTime(string strRfc1123Time)
        {
            try
            {
                if (String.IsNullOrEmpty(strRfc1123Time) == true)
                    return "";
                return Rfc1123DateTimeStringToLocal(strRfc1123Time, "G");
            }
            catch // (Exception ex)    // 2008/10/28
            {
                return "ʱ���ַ��� '" + strRfc1123Time + "' ��ʽ���󣬲��ǺϷ���RFC1123��ʽ";
            }
        }
#endif

        // �������
        public string ItemCaption
        {
            get
            {
                // ���ɻ���һ���е�����
                return this.comboBox_seller.Text + ":" + this.comboBox_source.Text + ":" + this.comboBox_copy.Text;
            }
        }

        // ���������յ�����
        public int NewlyAcceptedCount
        {
            get
            {
                if (this.location == null)
                    return 0;

                return this.location.ArrivedCount - this.location.ReadOnlyArrivedCount;
            }

        }

        #endregion
    }


    /// <summary>
    /// ���ȱʡ��¼
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void GetDefaultRecordEventHandler(object sender,
    GetDefaultRecordEventArgs e);

    /// <summary>
    /// ���ȱʡ��¼�Ĳ���
    /// </summary>
    public class GetDefaultRecordEventArgs : EventArgs
    {
        public string Xml = ""; // ȱʡ��¼
    }

    public class MyTableLayoutPanel : TableLayoutPanel
    {
        public bool DisableUpdate = false;

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            if (this.DisableUpdate == true)
            {
                if ((int)API.WM_ERASEBKGND == m.Msg)
                    m.Msg = (int)API.WM_NULL;
                else if ((int)API.WM_PAINT == m.Msg)
                    m.Msg = (int)API.WM_NULL;
                return;
            }
            base.DefWndProc(ref m);
        }
    }

    // ���ݴ����Ƿ��ڹ�Ͻ��Χ��
    /// <summary>
    /// ���ݴ����Ƿ��ڹ�Ͻ��Χ�� �¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void VerifyLibraryCodeEventHandler(object sender,
VerifyLibraryCodeEventArgs e);

    /// <summary>
    /// ���ݴ����Ƿ��ڹ�Ͻ��Χ���¼��Ĳ���
    /// </summary>
    public class VerifyLibraryCodeEventArgs : EventArgs
    {
        /// <summary>
        /// [in] �����Ĺݴ��롣������һ���ַ����б���̬
        /// </summary>
        public string LibraryCode = ""; // [in]�����Ĺݴ��롣������һ���ַ����б���̬
        /// <summary>
        /// [out] ��������ǿձ�ʾ��鷢��������
        /// </summary>
        public string ErrorInfo = "";   // [out]��������ǿձ�ʾ��鷢��������
    }
}
