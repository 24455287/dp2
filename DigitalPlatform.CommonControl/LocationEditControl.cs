using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using DigitalPlatform;

/*
 * TODO:
 * 1) Ӧ�ṩ�˵�������ɾ�У���������CountChanged�¼���ContentChanged�¼�
 * 
 * 
 * */

namespace DigitalPlatform.CommonControl
{
    /// <summary>
    /// �ݲ�ȥ�� �༭�ؼ�
    /// </summary>
    public partial class LocationEditControl : UserControl
    {
        // ��ǰ���ù��ĵص�����Ϊ��ʵ��ȥ����������Ҫ����ʱ���ص�ԭ��������
        List<string> UsedText = new List<string>();

        internal bool m_bFocused = false;

        bool m_bHideSelection = true;

        /// <summary>
        /// ���ݷ����ı�
        /// </summary>
        public event ContentChangedEventHandler ContentChanged = null;

        // �����ѵ�checkbox״̬�ı�
        [Category("New Event")]
        public event EventHandler ArrivedChanged = null;

        // ReadOnly״̬�����ı�
        [Category("New Event")]
        public event EventHandler ReadOnlyChanged = null;

        public LocationItem LastClickItem = null;   // ���һ��clickѡ�����LocationItem����

        public string DbName = "";  // ���ݿ��������ڻ��ֵ�б�

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        int m_nLineHeight = 26;

        internal int m_nLabelWidth = 26;    // 6
        // internal int m_nLibraryWidth = 100;
        internal int m_nLocationWidth = 160;
        internal int m_nArrivedWidth = 40;

        internal int m_nLineLeftBlank = 6;    // ����������ߵĿհ׿��
        internal int m_nLineWidth = 6;    // �������߲��ֵĿ��
        internal int m_nNumberTextWidth = 20;    // ���������ұߵ��������ֵĿ��
        
        internal int m_nRightBlank = 4;    // 30

        public List<LocationItem> LocationItems = new List<LocationItem>();

        bool m_bChanged = false;


        public LocationEditControl()
        {
            InitializeComponent();
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

                // SetArriveMode(value);
            }
        }



        // �ӹ�id�б�ֻȡ��ָ����Ŀ���ڵ�id�������б�
        static string LimitIDs(string strIDs,
            int nCount)
        {
            if (String.IsNullOrEmpty(strIDs) == true)
                return "";

            string[] ids = strIDs.Split(new char[] {','});

            if (ids.Length <= nCount)
                return strIDs;

            string strResult = "";
            for (int i = 0; i < nCount; i++)
            {
                if (i != 0)
                    strResult += ",";

                strResult += ids[i];
            }

            return strResult;
        }

        // �淶�ݲصص��ַ������޶���������������
        // �ӹ�����������ж��������Ͳü��������ȱ�������������
        public static string CanonicalizeDistributeString(
            string strDistributeString,
            int nAmount)
        {
            string strResult = "";
            int nCurrent = 0;
            string[] sections = strDistributeString.Split(new char[] { ';' });
            for (int i = 0; i < sections.Length; i++)
            {
                string strSection = sections[i].Trim();
                if (String.IsNullOrEmpty(strSection) == true)
                    continue;

                string strIDs = ""; // ������id�б�

                string strLocationString = "";
                int nCount = 0;
                int nRet = strSection.IndexOf(":");
                if (nRet == -1)
                {
                    strLocationString = strSection;
                    nCount = 1;
                }
                else
                {
                    strLocationString = strSection.Substring(0, nRet).Trim();
                    string strCount = strSection.Substring(nRet + 1);


                    nRet = strCount.IndexOf("{");
                    if (nRet != -1)
                    {
                        strIDs = strCount.Substring(nRet + 1).Trim();

                        if (strIDs.Length > 0 && strIDs[strIDs.Length - 1] == '}')
                            strIDs = strIDs.Substring(0, strIDs.Length - 1);

                        strCount = strCount.Substring(0, nRet).Trim();
                    }

                    try
                    {
                        nCount = Convert.ToInt32(strCount);
                    }
                    catch
                    {
                        throw new Exception("'" + strCount + "' ӦΪ������");
                    }

                    if (nCount > 1000)
                        throw new Exception("����̫�󣬳���1000");
                }

                if (nCurrent + nCount > nAmount)
                {
                    if (nAmount - nCurrent > 0)
                    {
                        if (strResult != "")
                            strResult += ";";
                        strResult += strLocationString + ":" + (nAmount - nCurrent).ToString();

                        string strPart = LimitIDs(strIDs, nAmount - nCurrent);
                        if (LocationCollection.IsEmptyIDs(strPart) == false)
                            strResult += "{" + strPart + "}";
                    }
                    nCurrent += nAmount - nCurrent;
                    break;
                }

                if (strResult != "")
                    strResult += ";";
                strResult += strLocationString + ":" + nCount.ToString();

                {
                    string strPart = LimitIDs(strIDs, nCount);
                    if (LocationCollection.IsEmptyIDs(strPart) == false)
                        strResult += "{" + strPart + "}";
                }


                nCurrent += nCount;
            }

            // �������������
            if (nCurrent < nAmount)
            {
                if (strResult != "")
                    strResult += ";";
                strResult += "" + ":" + (nAmount-nCurrent).ToString();

                // ids������
            }

            return strResult;
        }

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

        bool m_bReadOnly = false;

        // ȫ��Item��ReadOnly״̬
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
                    for (int i = 0; i < this.LocationItems.Count; i++)
                    {
                        LocationItem item = this.LocationItems[i];
                        /*
                        if (value == true)
                            item.comboBox_location.Enabled = false;
                        else
                            item.comboBox_location.Enabled = true;
                         * */
                        item.ReadOnly = value;
                    }

                    this.OnReadOnlyChanged();
                }
            }
        }

        public LocationItem InsertNewItem(int index)
        {
            LocationItem item = new LocationItem(this);
            this.LocationItems.Insert(index, item);
            item.State = ItemState.New;

            this.SetSize();
            this.LayoutItems();

            return item;
        }

        // ɾ��һ������
        // 2008/9/16
        public void RemoveItem(int index,
            bool bUpdateDisplay)
        {
            LocationItem line = this.LocationItems[index];

            line.RemoveFromContainer();

            this.LocationItems.Remove(line);

            /*
            if (this.LastClickItem == line)
                this.LastClickItem = null;
             * */

            this.Changed = true;

            if (bUpdateDisplay == true)
            {
                this.SetSize();
                this.LayoutItems();
            }

        }

        // ɾ��һ������
        // 2008/9/16
        public void RemoveItem(LocationItem line,
            bool bUpdateDisplay)
        {
            int index = this.LocationItems.IndexOf(line);

            if (index == -1)
                return;

            line.RemoveFromContainer();

            this.LocationItems.Remove(line);

            /*
            if (this.LastClickItem == line)
                this.LastClickItem = null;
             * */

            this.Changed = true;

            if (bUpdateDisplay == true)
            {
                this.SetSize();
                this.LayoutItems();
            }
        }

        // ���Ѿ���ѡ�ġ�����ref id����������ΪReadOnly״̬
        // 2008/9/13 new add
        // parameters:
        //      bClearAllReadOnlyBeforeSet  �Ƿ�������ǰ������е�readonly״̬
        public void SetAlreadyCheckedToReadOnly(bool bClearAllReadOnlyBeforeSet)
        {
            bool bChanged = false;
            for (int i = 0; i < this.LocationItems.Count; i++)
            {
                LocationItem item = this.LocationItems[i];

                if (item.checkBox_arrived.Checked == true
                    && String.IsNullOrEmpty(item.ArrivedID) == false)
                {
                    if (item.ReadOnly != true && item.ArrivedID != "*") // 2008/12/25 changed
                    {
                        item.ReadOnly = true;
                        bChanged = true;
                    }
                }
                else
                {
                    if (bClearAllReadOnlyBeforeSet == true)
                    {
                        if (item.ReadOnly != false)
                        {
                            item.ReadOnly = false;
                            bChanged = true;
                        }
                    }
                }
            }

            if (this.m_bReadOnly != false)
            {
                this.m_bReadOnly = false;   // ����ȫ�����ΪReadOnly
                bChanged = true;
            }

            if (bChanged == true)
                this.OnReadOnlyChanged();

            // ���������һ�������ȫ��item����readonly�ˣ�����container����readonly״̬��
            // ����״̬��ζ�Ż�����������item��������containerΪreadonly��ʱ���ǲ���������������κ���item����
        }

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                return this.m_bChanged;
            }
            set
            {

                bool bOldValue = this.m_bChanged;



                if (this.m_bChanged != value)
                {
                    this.m_bChanged = value;

                    if (value == false)
                        ResetLineState();

                    // �����¼�
                    if (bOldValue != value && this.ContentChanged != null)
                    {
                        ContentChangedEventArgs e = new ContentChangedEventArgs();
                        e.OldChanged = bOldValue;
                        e.CurrentChanged = value;
                        ContentChanged(this, e);
                    }

                }
            }
        }

        internal void OnArrivedChanged()
        {
            if (this.ArrivedChanged != null)
            {
                this.ArrivedChanged(this, new EventArgs());
            }
        }

        internal void OnReadOnlyChanged()
        {
            if (this.ReadOnlyChanged != null)
            {
                this.ReadOnlyChanged(this, new EventArgs());
            }
        }

        // ���м��
        // return:
        //      -1  �������г���
        //      0   ���û�з��ִ���
        //      1   ��鷢���˴���
        public int Check(out string strError)
        {
            strError = "";

            bool bStrict = true;    // �Ƿ��ϸ���

            if (bStrict == true)
            {
                for (int i = 0; i < this.LocationItems.Count; i++)
                {
                    LocationItem element = this.LocationItems[i];

                    if (String.IsNullOrEmpty(element.LocationString) == true)
                    {
                        if (this.LocationItems.Count == 1)
                            strError = "��δָ��ȷ�еĹݲصص�";   // ֻ��һ�е������������ʾ�кš����������ñ��ؼ�Ƕ�� OrderDesignControl ʱ����ʾ��ˬһЩ 2014/8/29
                        else
                            strError = "�ݲ������ " + (i + 1).ToString() + " ��: ��δָ��ȷ�еĹݲصص�";
                        return 1;
                    }
                }
            }

            return 0;
        }

        // ��ȫ�������״̬����ΪNormal
        void ResetLineState()
        {
            for (int i = 0; i < this.LocationItems.Count; i++)
            {
                LocationItem element = this.LocationItems[i];
                element.State = ItemState.Normal;
            }
        }

        void RefreshLineColor()
        {
            for (int i = 0; i < this.LocationItems.Count; i++)
            {
                LocationItem element = this.LocationItems[i];
                element.SetLineColor();
            }
        }

        public void Clear()
        {
            this.LocationItems.Clear();

            while(this.panel_main.Controls.Count != 0)
                this.panel_main.Controls.RemoveAt(0);
        }

        public void SelectAll()
        {
            for (int i = 0; i < this.LocationItems.Count; i++)
            {
                LocationItem cur_element = this.LocationItems[i];
                if ((cur_element.State & ItemState.Selected) == 0)
                    cur_element.State |= ItemState.Selected;
            }
        }

        public void SelectItem(LocationItem element,
            bool bClearOld)
        {

            if (bClearOld == true)
            {
                for (int i = 0; i < this.LocationItems.Count; i++)
                {
                    LocationItem cur_element = this.LocationItems[i];

                    if (cur_element == element)
                        continue;   // ��ʱ������ǰ��

                    if ((cur_element.State & ItemState.Selected) != 0)
                        cur_element.State -= ItemState.Selected;
                }
            }

            // ѡ�е�ǰ��
            if ((element.State & ItemState.Selected) == 0)
                element.State |= ItemState.Selected;

            this.LastClickItem = element;
        }

        public void ToggleSelectItem(LocationItem element)
        {
            // ѡ�е�ǰ��
            if ((element.State & ItemState.Selected) == 0)
                element.State |= ItemState.Selected;
            else
                element.State -= ItemState.Selected;

            this.LastClickItem = element;
        }

        public void RangeSelectItem(LocationItem element)
        {
            LocationItem start = this.LastClickItem;

            int nStart = this.LocationItems.IndexOf(start);
            if (nStart == -1)
                return;

            int nEnd = this.LocationItems.IndexOf(element);

            if (nStart > nEnd)
            {
                // ����
                int nTemp = nStart;
                nStart = nEnd;
                nEnd = nTemp;
            }

            for (int i = nStart; i <= nEnd; i++)
            {
                LocationItem cur_element = this.LocationItems[i];

                if ((cur_element.State & ItemState.Selected) == 0)
                    cur_element.State |= ItemState.Selected;
            }

            // �������λ��
            for (int i = 0; i < nStart; i++)
            {
                LocationItem cur_element = this.LocationItems[i];

                if ((cur_element.State & ItemState.Selected) != 0)
                    cur_element.State -= ItemState.Selected;
            }

            for (int i = nEnd + 1; i < this.LocationItems.Count; i++)
            {
                LocationItem cur_element = this.LocationItems[i];

                if ((cur_element.State & ItemState.Selected) != 0)
                    cur_element.State -= ItemState.Selected;
            }
        }




#if NOOOOOOOOOOOOOO

        // ���ݲصص��ַ���ת��Ϊ�����б�
        static int LocationStringToList(
            string value,
            LocationEditControl container,
            out List<LocationItem> items,
            out string strError)
        {
            strError = "";
            items = new List<LocationItem>();

            string[] sections = value.Split(new char[] { ';' });
            for (int i = 0; i < sections.Length; i++)
            {
                string strSection = sections[i].Trim();
                if (String.IsNullOrEmpty(strSection) == true)
                    continue;

                string strIDs = ""; // ������id�б�

                string strLocationString = "";
                int nCount = 0;
                int nRet = strSection.IndexOf(":");
                if (nRet == -1)
                {
                    strLocationString = strSection;
                    nCount = 1;
                }
                else
                {
                    strLocationString = strSection.Substring(0, nRet).Trim();
                    string strCount = strSection.Substring(nRet + 1);


                    nRet = strCount.IndexOf("{");
                    if (nRet != -1)
                    {
                        strIDs = strCount.Substring(nRet + 1).Trim();

                        if (strIDs.Length > 0 && strIDs[strIDs.Length - 1] == '}')
                            strIDs = strIDs.Substring(0, strIDs.Length - 1);

                        strCount = strCount.Substring(0, nRet).Trim();
                    }

                    try
                    {
                        nCount = Convert.ToInt32(strCount);
                    }
                    catch
                    {
                        strError = "'" + strCount + "' ӦΪ������";
                        return -1;
                    }

                    if (nCount > 1000)
                    {
                        strError = "����̫�󣬳���1000";
                        return -1;
                    }

                }

                for (int j = 0; j < nCount; j++)
                {
                    LocationItem item = new LocationItem(container);
                    if (container != null)
                        item.LocationString = strLocationString;
                    items.Add(item);
                }

                if (string.IsNullOrEmpty(strIDs) == false)
                {
                    string[] ids = strIDs.Split(new char[] { ',' });

                    int nStartBase = items.Count - nCount;
                    for (int k = 0; k < nCount; k++)
                    {
                        LocationItem item = items[nStartBase + k];

                        if (k >= ids.Length)
                            break;

                        string strID = ids[k];

                        if (String.IsNullOrEmpty(strID) == true)
                        {
                            // item.Arrived = false;
                            continue;
                        }


                        item.Arrived = true;
                        item.ArrivedID = strID;
                    }
                }
            }

            return 0;
        }

        // �������б�ת��Ϊ�ݲصص��ַ���
        static string ListToLocationString(List<LocationItem> items,
            bool bOutputID)
        {
            string strResult = "";
            string strPrevLocationString = null;
            int nPartCount = 0;
            string strIDs = "";
            for (int i = 0; i < items.Count; i++)
            {
                LocationItem item = items[i];

                if (item.LocationString == strPrevLocationString)
                {
                    nPartCount++;
                    strIDs += item.ArrivedID + ",";
                }
                else
                {
                    if (strPrevLocationString != null)
                    {
                        if (strResult != "")
                            strResult += ";";
                        strResult += strPrevLocationString + ":" + nPartCount.ToString();

                        if (bOutputID == true)
                        {
                            if (LocationEditControl.IsEmptyIDs(strIDs) == false)
                                strResult += "{" + RemoveTailComma(strIDs) + "}";
                        }

                        nPartCount = 0;
                        strIDs = "";
                    }

                    nPartCount++;
                    strIDs += item.ArrivedID + ",";
                }

                strPrevLocationString = item.LocationString;
            }

            if (nPartCount != 0)
            {
                if (strResult != "")
                    strResult += ";";
                strResult += strPrevLocationString + ":" + nPartCount.ToString();

                if (bOutputID == true)
                {
                    if (LocationEditControl.IsEmptyIDs(strIDs) == false)
                        strResult += "{" + RemoveTailComma(strIDs) + "}";
                }
            }

            return strResult;
        }

#endif 

        // 2012/5/18
        internal int m_nDontMerge = 0;

        // �ַ�����̬��������
        // �ֺż��ÿ��segment��segment���ڲ��ṹ��: �ݲصص�:����{�ѵ���¼id����}
        // '�ѵ���¼����'�ĸ�ʽΪ�����ŷָ����ַ��������ĳ��ֵΪ�գ���󲿵Ķ��Ų���ʡ��
        public string Value
        {
            get
            {
                // this.Merge();

                string strResult = "";
                string strPrevLocationString = null;
                int nPartCount = 0;
                string strIDs = "";
                // int nIDCount = 0;
                for (int i = 0; i < this.LocationItems.Count; i++)
                {
                    LocationItem item = this.LocationItems[i];

                    if (item.LocationString == strPrevLocationString)
                    {
                        nPartCount++;
                        strIDs += (item.Arrived == true ? item.ArrivedID : "")
                            + ",";
                        /*
                        if (item.Arrived == true)
                            nIDCount++;
                         * */
                    }
                    else
                    {
                        if (strPrevLocationString != null)
                        {
                            Debug.Assert(nPartCount >= 0, "");

                            if (strResult != "")
                                strResult += ";";
                            strResult += strPrevLocationString + ":" + nPartCount.ToString();

                            if (LocationCollection.IsEmptyIDs(strIDs) == false)
                                strResult += "{" + LocationCollection.RemoveTailComma(strIDs) + "}";

                            nPartCount = 0;
                            strIDs = "";
                            // nIDCount = 0;
                        }

                        nPartCount++;
                        strIDs += (item.Arrived == true ? item.ArrivedID : "")
                            + ",";
                        /*
                        if (item.Arrived == true)
                            nIDCount++;
                         * */
                    }

                    strPrevLocationString = item.LocationString;
                }

                if (nPartCount != 0)
                {
                    Debug.Assert(nPartCount > 0, "");

                    if (strResult != "")
                        strResult += ";";
                    strResult += strPrevLocationString + ":" + nPartCount.ToString();
                    if (LocationCollection.IsEmptyIDs(strIDs) == false)
                        strResult += "{" + LocationCollection.RemoveTailComma(strIDs) + "}";

                }

                return strResult;
            }
            set
            {
                this.Clear();

                string[] sections = value.Split(new char[] {';'});
                for (int i = 0; i < sections.Length; i++)
                {
                    string strSection = sections[i].Trim();
                    if (String.IsNullOrEmpty(strSection) == true)
                        continue;

                    string strIDs = ""; // ������id�б�

                    string strLocationString = "";
                    int nCount = 0;
                    int nRet = strSection.IndexOf(":");
                    if (nRet == -1)
                    {
                        strLocationString = strSection;
                        nCount = 1;
                    }
                    else
                    {
                        strLocationString = strSection.Substring(0, nRet).Trim();
                        string strCount = strSection.Substring(nRet + 1);


                        nRet = strCount.IndexOf("{");
                        if (nRet != -1)
                        {
                            strIDs = strCount.Substring(nRet + 1).Trim();

                            if (strIDs.Length > 0 && strIDs[strIDs.Length - 1] == '}')
                                strIDs = strIDs.Substring(0, strIDs.Length - 1);

                            strCount = strCount.Substring(0, nRet).Trim();
                        }

                        try
                        {
                            nCount = Convert.ToInt32(strCount);
                        }
                        catch
                        {
                            throw new Exception(
                                "�ݲصص��ַ����ֲ� '" + strSection + "' �� " 
                                + "'"+strCount+"' ӦΪ������");
                        }

                        if (nCount > 1000)
                            throw new Exception(
                                "�ݲصص��ַ����ֲ� '" + strSection + "' �� " 
                                + "���� "+strCount+" ֵ̫�󣬳���1000");

                        // 2008/12/5 new add
                        if (nCount < 0)
                            throw new Exception(
                                "�ݲصص��ַ����ֲ� '" + strSection + "' �� " 
                                + "���� "+strCount+" Ϊ��������ʽ����");

                        Debug.Assert(nCount >=0, "");
                    }

                    this.m_nDontMerge++;
                    try
                    {
                        for (int j = 0; j < nCount; j++)
                        {
                            LocationItem item = new LocationItem(this);
                            item.LocationString = strLocationString;
                            this.LocationItems.Add(item);
                        }
                    }
                    finally
                    {
                        this.m_nDontMerge--;
                    }


                    if (string.IsNullOrEmpty(strIDs) == false)
                    {
                        Debug.Assert(nCount >=0, "");

                        string[] ids = strIDs.Split(new char[] { ',' });

                        int nStartBase = this.LocationItems.Count - nCount;
                        for (int k = 0; k < nCount; k++)
                        {
                            Debug.Assert((nStartBase + k) >=0
                                && (nStartBase + k) < this.LocationItems.Count,
                                "");
                            LocationItem item = this.LocationItems[nStartBase + k];

                            if (k >= ids.Length)
                                break;

                            string strID = ids[k];

                            if (String.IsNullOrEmpty(strID) == true)
                            {
                                // item.Arrived = false;
                                continue;
                            }

                            int nCountSave = this.LocationItems.Count;

                            item.Arrived = true;
                            item.ArrivedID = strID;

                            Debug.Assert(nCountSave == this.LocationItems.Count, "����ǰ���count���ܱ仯");
                        }
                    }
                }

                this.ResetLineState();
                this.SetSize();
                this.LayoutItems();
            }
        }

        internal void SetSize()
        {
            // ���������߶�
            this.Size = new Size(this.TotalWidth,
                this.m_nLineHeight * Math.Max( 1, this.LocationItems.Count) + 4/*΢��*/);
        }

        public override Size MaximumSize
        {
            get
            {
                Size size = base.MaximumSize;
                int nLimitHeight = this.m_nLineHeight * Math.Max(1, this.LocationItems.Count) + 4;
                if (size.Height > nLimitHeight
                    || size.Height == 0)
                    size.Height = nLimitHeight;

                int nLimitWidth = this.TotalWidth;
                if (size.Width > nLimitWidth)
                    size.Width = nLimitWidth;

                return size;
            }
            set
            {
                base.MaximumSize = value;
            }
        }

        public override Size MinimumSize
        {
            get
            {
                Size size = base.MinimumSize;
                int nLimitHeight = this.m_nLineHeight * Math.Max(1, this.LocationItems.Count) + 4;
                int nLimitWidth = this.TotalWidth;
                size.Height = nLimitHeight;
                size.Width = nLimitWidth;

                return size;
            }
            set
            {
                base.MinimumSize = value;
            }
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            base.SetBoundsCore(x, y, this.TotalWidth, height, specified);
        }

        public List<LocationItem> SelectedItems
        {
            get
            {
                List<LocationItem> results = new List<LocationItem>();

                for (int i = 0; i < this.LocationItems.Count; i++)
                {
                    LocationItem cur_element = this.LocationItems[i];
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

                for (int i = 0; i < this.LocationItems.Count; i++)
                {
                    LocationItem cur_element = this.LocationItems[i];
                    if ((cur_element.State & ItemState.Selected) != 0)
                        results.Add(i);
                }

                return results;
            }
        }

        // readonly״̬���ѵ�����ĸ���
        public int ReadOnlyArrivedCount
        {
            get
            {
                int nValue = 0;
                for (int i = 0; i < this.LocationItems.Count; i++)
                {
                    LocationItem item = this.LocationItems[i];
                    if (item.Arrived == true && 
                        (item.ReadOnly == true || this.ReadOnly == true))// 2008/11/12 new add
                        nValue++;
                }
                return nValue;
            }
        }

        // �����ѵ�״̬������ĸ���
        // ע���޸�ֵ��ʱ����Ȼ�ı���checked״̬�����ǲ��ᴥ��checkboxҪ������¼�
        // Exception:   set operation may throw exception
        public int ArrivedCount
        {
            get
            {
                int nValue = 0;
                for (int i = 0; i < this.LocationItems.Count; i++)
                {
                    LocationItem item = this.LocationItems[i];
                    if (item.Arrived == true)
                        nValue++;
                }
                return nValue;
            }
            set
            {
                // ��������õ���������ȵ�ǰȫ������������࣬
                // ����Ҫ������������
                if (value > this.LocationItems.Count)
                {
                    this.Count = value;
                }

                Debug.Assert(value <= this.Count, "");

                // ͳ�Ƴ���ࣺ��ǰ�Ѿ�ΪArrived״̬�������Ҫ����������֮��ĵĲ��
                int nDelta = value - this.ArrivedCount;
                if (nDelta == 0)
                    return;

                if (nDelta > 0)
                {
                    // ��
                    int nPassReadOnlyCount = 0; // �����м侭���ı�������Ҫ��ĵ���Ϊreadonly״̬���������

                    // ��ǰ����ʼ���𲽹�ѡ����Arrived״̬�����ֱ����nDelta��
                    int nCount = 0;
                    for (int i = 0; i < this.LocationItems.Count; i++)
                    {
                        LocationItem item = this.LocationItems[i];

                        if (item.Arrived == true)
                            continue;

                        // 2008/9/13 new add
                        if (item.ReadOnly == true)
                        {
                            nPassReadOnlyCount++;
                            continue;
                        }

                        item.Arrived = true;
                        nCount++;
                        if (nCount >= nDelta)
                            break;
                    }

                    if (nCount < nDelta)
                    {
                        NotEnoughException ex = new NotEnoughException("�޷�������ѡ���� " + nDelta.ToString() + " ������������ " + nCount + " ��");
                        ex.WantValue = nDelta;
                        ex.DoneValue = nCount;
                        throw ex;
                    }
                }
                else if (nDelta < 0)
                {
                    // ��

                    Debug.Assert(nDelta < 0, "");
                    // ���������Ѿ���ѡ������

                    bool bDeleted = false;  // �Ƿ���������ɾ��

                    int nPassReadOnlyCount = 0; // �����м侭���ı�������Ҫ��ĵ���Ϊreadonly״̬���������

                    // �Ӻ󷽿�ʼ����off�Ѿ���Arrived״̬�����ֱ����nDelta��
                    int nCount = 0;
                    for (int i = LocationItems.Count - 1; i >= 0; i--)
                    {
                        LocationItem item = this.LocationItems[i];


                        if (item.Arrived == false)
                            continue;

                        // 2008/9/13 new add
                        if (item.ReadOnly == true)
                        {
                            nPassReadOnlyCount++;
                            continue;
                        }

                        item.Arrived = false;

                        // 2008/9/16 new add
                        // ɾ�����θո����ӵģ�����û�����ü����õص��ַ���������
                        if (String.IsNullOrEmpty(item.ArrivedID) == true
                            || item.ArrivedID == "*")
                        {
                            if (String.IsNullOrEmpty(item.LocationString) == true)
                            {
                                this.RemoveItem(item, false);
                                bDeleted = true;
                            }
                        }

                        nCount++;
                        Debug.Assert(nDelta < 0, "");
                        if (nCount >= -1*nDelta)
                            break;
                    }

                    if (bDeleted == true)
                    {
                        this.SetSize();
                        this.LayoutItems();
                    }

                    if (nCount < -1 * nDelta)
                    {
                        NotEnoughException ex = new NotEnoughException("�޷��¼���ѡ���� " + (-1 * nDelta).ToString() + " �������¼��� " + nCount + " ��");
                        ex.WantValue = nDelta;
                        ex.DoneValue = -1 * nCount;
                        throw ex;
                    }
                }
            }
        }

        void SetUsedText(int index,
            string strText)
        {
            while (this.UsedText.Count < index + 1)
                this.UsedText.Add("");
            this.UsedText[index] = strText;
        }

        string GetUsedText(int index)
        {
            if (index >= this.UsedText.Count)
                return "";
            return this.UsedText[index];
        }

        // �������
        public int Count
        {
            get
            {
                return this.LocationItems.Count;
            }
            set
            {
                // ɾ��һЩ
                if (value < this.LocationItems.Count)
                {
                    for (int i = value; i < this.LocationItems.Count; i++)
                    {
                        SetUsedText(i, this.LocationItems[i].LocationString);
                        this.LocationItems[i].RemoveFromContainer();
                    }

                    this.LocationItems.RemoveRange(value, this.LocationItems.Count - value);

                    this.SetSize();
                    this.LayoutItems();

                    return;
                }

                // ����һЩ
                if (value > this.LocationItems.Count)
                {

                    int nStart = this.LocationItems.Count;
                    for (int i = nStart; i < value; i++)
                    {
                        LocationItem item = new LocationItem(this);
                        item.LocationString = GetUsedText(i);   // 2009/10/13 new add
                        item.Location = new Point(0, this.m_nLineHeight * i);
                        item.No = (i + 1).ToString();
                        item.State = ItemState.New;
                        this.LocationItems.Add(item);
                    }

                    // this.ResetLineColor();
                    this.SetSize();
                    this.LayoutItems();

                    return;
                }
            }
        }

        public void RefreshLineAndText()
        {
            this.panel_main.Invalidate();   // ��ʹ�����ϵ����ֺ�������ˢ��
            // this.panel_main.Update();
        }

        // ���������������ʾλ�á���������������š�
        // һ����LocationItems�����ʹ�á�
        public void LayoutItems()
        {
            // ����һ���ʱ�����ʾ���
            bool bSetNo = false;

            if (this.LocationItems.Count > 1)
                bSetNo = true;

            for (int i = 0; i < this.LocationItems.Count; i++)
            {
                LocationItem item = this.LocationItems[i];
                item.Location = new Point(0, this.m_nLineHeight * i);

                // ���LocationEditControl AutoScaleMode ���� AutoScaleMode.None�� ����Ҫ���¶�λ?
                // item.comboBox_location.Size = new Size(this.m_nLocationWidth, 28);

                if (bSetNo == true)
                    item.No = (i + 1).ToString();
                else
                    item.No = "";
            }

            this.RefreshLineAndText();   // ��ʹ�����ϵ����ֺ�������ˢ��
        }

        public void Sort()
        {
            this.LocationItems.Sort(new LocationItemComparer());
            this.LayoutItems();
        }

        // 2008/8/29
        // �鲢������ͬ���������������ͬ�������ı����еĻ�������
        // return:
        //      0   unchanged
        //      1   changed
        public static int Merge(ref List<LocationItem> items)
        {
            bool bChanged = false;
            for (int i = 0; i < items.Count; )
            {
                LocationItem item = items[i];

                string strLocationString = item.LocationString;
                int nTop = i + 1;
                for (int j = i + 1; j < items.Count; j++)
                {
                    LocationItem comp_item = items[j];
                    if (comp_item.LocationString == strLocationString)
                    {
                        // �������λ��(���౻�ƺ�)
                        if (j != nTop)
                        {
                            LocationItem temp = items[j];
                            items.RemoveAt(j);
                            items.Insert(nTop, temp);
                            bChanged = true;
                        }

                        nTop++;
                    }

                }

                i = nTop;
            }

            if (bChanged == true)
                return 1;
            return 0;
        }

        public void Merge()
        {
            int nRet = Merge(ref this.LocationItems);
            if (nRet == 1)
                this.LayoutItems();
        }

#if NOOOOOOOOOOOOOOOO
        // �鲢������ͬ���������������ͬ�������ı����еĻ�������
        public void Merge()
        {
            bool bChanged = false;
            for (int i = 0; i < this.LocationItems.Count;)
            {
                LocationItem item = this.LocationItems[i];

                string strLocationString = item.LocationString;
                int nTop = i + 1;
                for (int j = i+1; j < this.LocationItems.Count; j++)
                {
                    LocationItem comp_item = this.LocationItems[j];
                    if (comp_item.LocationString == strLocationString)
                    {
                        // �������λ��(���౻�ƺ�)
                        if (j != nTop)
                        {
                            LocationItem temp = this.LocationItems[j];
                            this.LocationItems.RemoveAt(j);
                            this.LocationItems.Insert(nTop, temp);
                            bChanged = true;
                        }

                        nTop++;
                    }

                }

                i = nTop;
            }

            if (bChanged == true)
                this.LayoutItems();
        }
#endif

        /*
        // ������������
        void ExchangeTwoItems(int i, int j)
        {
            if (i == j)
                return;

            LocationItem item = this.LocationItems[i];

            this.LocationItems[i] = this.LocationItems[j];

            this.LocationItems[j] = item;
        }
         * */

        public int TotalWidth
        {
            get
            {
                return m_nLabelWidth    // ���ɫ��
                    + m_nLocationWidth  // ��Ͽ�
                    + m_nArrivedWidth   // checkbox����
                    + m_nLineLeftBlank
                    + m_nLineWidth
                    + m_nNumberTextWidth
                    + m_nRightBlank;    // �ұ߿հ�
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
            this.GetValueTable(sender, e);
        }

        // ���ƻ�������
        void PaintLine(Graphics g,
            int nStart,
            int nCount)
        {
            int x = m_nLabelWidth + m_nLocationWidth 
                + m_nArrivedWidth
                + m_nLineLeftBlank; // 6
            int w = m_nLineWidth;   // 6

            Pen pen = new Pen(SystemColors.GrayText);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            pen.DashCap = System.Drawing.Drawing2D.DashCap.Round;
            pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel;

            int start_y = this.m_nLineHeight * nStart + (this.m_nLineHeight / 2);

            // ��ʼλ�ú���
            g.DrawLine(pen, 
                new Point(x, start_y), 
                new Point(x + w, start_y));

            int end_y = this.m_nLineHeight * (nStart + nCount -1) + (this.m_nLineHeight / 2);

            if (nCount > 1)
            {

                // ����
                g.DrawLine(pen, new Point(x + w, start_y), new Point(x + w, end_y));


                // ����λ�ú���
                g.DrawLine(pen, 
                    new Point(x + w, end_y),
                    new Point(x-1, end_y)
                    );
            }

            // ����
            int middle_y = ((start_y + end_y) / 2) - (this.m_nLineHeight / 4);

            Brush brush = new SolidBrush(SystemColors.GrayText);

            g.DrawString(nCount.ToString(),
                this.panel_main.Font,
                brush,
                new Point(x+w+2, middle_y));
        }

        // ���� ��������������
        private void panel_main_Paint(object sender, PaintEventArgs e)
        {
            // ֻ��һ�������ʱ�򣬲�����ʾ����������
            if (this.LocationItems.Count <= 1)
                return;

            string strPrevText = null;
            int nSegmentCount = 0;
            for (int i = 0; i < this.LocationItems.Count; i++)
            {
                LocationItem item = this.LocationItems[i];

                if (strPrevText != item.LocationString)
                {
                    if (strPrevText != null)
                    {
                        // ����ǰ���ۻ���count
                        PaintLine(e.Graphics, i - nSegmentCount, nSegmentCount);
                        nSegmentCount = 0;
                    }

                    nSegmentCount++;
                }
                else
                {
                    nSegmentCount++;
                }


                strPrevText = item.LocationString;
            }

            if (nSegmentCount != 0)
            {
                // ����ǰ���ۻ���count
                PaintLine(e.Graphics, this.LocationItems.Count - nSegmentCount, nSegmentCount);
            }
        }

        private void LocationEditControl_Enter(object sender, EventArgs e)
        {
            this.m_bFocused = true;
            this.RefreshLineColor();
        }

        private void LocationEditControl_Leave(object sender, EventArgs e)
        {
            this.m_bFocused = false;
            this.RefreshLineColor();
        }
    }


    // ����
    class LocationItemComparer : IComparer<LocationItem>
    {
        /*
        public LocationItemComparer()
        {
        }*/

        int IComparer<LocationItem>.Compare(LocationItem x, LocationItem y)
        {
            string s1 = x.LocationString;
            string s2 = y.LocationString;

            return String.Compare(s1, s2);
        }
    }

    // һ���ݲصص�����
    public class LocationItem
    {
        int DisableArrivedCheckedChanged = 0;   // �Ƿ���Ҫ��ֹ��checkBox_arrived��Checked�޸��������𴥷��¼��������������޸���Ҫ��ֹ�����û�һ����������Ҫ����

        public LocationEditControl Container = null;

        // ��ɫ��popupmenu
        public Label label_color = null;

        /*
        // ����
        public ComboBox comboBox_library = null;
         * */

        // �ݲصص�
        public ComboBox comboBox_location = null;

        public CheckBox checkBox_arrived = null;

        ItemState m_state = ItemState.Normal;

        int m_nTopX = 0;
        int m_nTopY = 0;

        internal ItemState State
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

#if DEBUG
                    // 2014/11/13
                    // ������״̬�� ReadOnly ֮��Ĺ�ϵ
                    if ((this.m_state & ItemState.ReadOnly) != 0)
                    {
                        Debug.Assert(this.ReadOnly == true, "");
                    }
                    else
                    {
                        Debug.Assert(this.ReadOnly == false, "");
                    }
#endif
                }
            }
        }

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
                    this.label_color.ForeColor = SystemColors.HighlightText;
                    return;
                }
            }

            if ((this.m_state & ItemState.New) != 0)
            {
                this.label_color.BackColor = Color.Yellow;
                this.label_color.ForeColor = SystemColors.GrayText;
                return;
            }
            if ((this.m_state & ItemState.Changed) != 0)
            {
                this.label_color.BackColor = Color.LightGreen;
                this.label_color.ForeColor = SystemColors.GrayText;
                return;
            }

            this.label_color.BackColor = SystemColors.Window;
            this.label_color.ForeColor = SystemColors.GrayText;
        }

        // ����Item��ReadOnly״̬
        // ע: ��set����item��readonly״̬��ʱ��û�д��� container.OnReadOnlyChanged(); ��������Ϊ��Ч�ʿ���
        public bool ReadOnly
        {
            get
            {
#if NO
                bool bRet = (this.comboBox_location.Enabled == true ? false : true);

#if DEBUG
                if ((this.State & ItemState.ReadOnly) != 0)
                {
                    Debug.Assert(bRet == true, "");
                }
                else
                {
                    Debug.Assert(bRet == false, "");
                }
#endif

                return bRet;
#endif
                return ((this.State & ItemState.ReadOnly) != 0);
            }
            set
            {
                if (value == true)
                {
                    this.comboBox_location.Enabled = false;
                    if (this.checkBox_arrived != null)
                        this.checkBox_arrived.Enabled = false;

                    this.State |= ItemState.ReadOnly;
                }
                else
                {
                    this.comboBox_location.Enabled = true;
                    if (this.checkBox_arrived != null)
                        this.checkBox_arrived.Enabled = true;

                    if ((this.State & ItemState.ReadOnly) != 0)
                        this.State -= ItemState.ReadOnly;
                }
            }
        }

        public LocationItem(LocationEditControl container)
        {
            this.Container = container;

            if (container == null)
                return; // 2008/8/29 new add

            // ��ɫ
            label_color = new Label();
            label_color.Size = new Size(this.Container.m_nLabelWidth, 26);
            label_color.TextAlign = ContentAlignment.MiddleLeft;
            label_color.ForeColor = SystemColors.GrayText;

            container.panel_main.Controls.Add(label_color);

            /*
            // ����
            comboBox_library = new ComboBox();
            comboBox_library.DropDownStyle = ComboBoxStyle.DropDown;
            comboBox_library.FlatStyle = FlatStyle.Flat;
            comboBox_library.Size = new Size(this.Container.m_nLibraryWidth, 28);
            comboBox_library.DropDownHeight = 300;
            comboBox_library.DropDownWidth = 300;
            comboBox_library.ForeColor = this.Container.panel_main.ForeColor;
            comboBox_library.Text = "";

            container.panel_main.Controls.Add(comboBox_library);
             * */



            // �ݲصص�
            comboBox_location = new ComboBox();
            comboBox_location.DropDownStyle = ComboBoxStyle.DropDown;
            comboBox_location.FlatStyle = FlatStyle.Flat;
            comboBox_location.DropDownHeight = 300;
            comboBox_location.DropDownWidth = 300;
            comboBox_location.Size = new Size(this.Container.m_nLocationWidth, 28);
            comboBox_location.ForeColor = this.Container.panel_main.ForeColor;

            container.panel_main.Controls.Add(comboBox_location);

            // �����ձ�־
            this.checkBox_arrived = new CheckBox();
            this.checkBox_arrived.Size = new Size(this.Container.m_nArrivedWidth, 28);
            this.checkBox_arrived.ForeColor = this.Container.panel_main.ForeColor;
            container.panel_main.Controls.Add(checkBox_arrived);

            if (this.Container.ArriveMode == false)
                this.checkBox_arrived.Enabled = false;  // �ڶ���״̬�£������ձ��Ҳ��Ҫ��ʾ������������Disable״̬�������޸�

            AddEvents();
        }

        public void RemoveFromContainer()
        {
            Container.panel_main.Controls.Remove(this.label_color);
            Container.panel_main.Controls.Remove(this.comboBox_location);
            Debug.Assert(this.checkBox_arrived != null, "");

            Container.panel_main.Controls.Remove(this.checkBox_arrived);
        }

        public Point Location
        {
            get
            {
                return new Point(this.m_nTopX, this.m_nTopY);
            }
            set
            {
                this.m_nTopX = value.X;
                this.m_nTopY = value.Y;

                this.label_color.Location = new Point(this.m_nTopX, this.m_nTopY);

                /*
                this.comboBox_library.Location = new Point(this.m_nTopX + this.label_color.Width,
                    this.m_nTopY);
                 * */

                this.comboBox_location.Location = new Point(this.m_nTopX + this.label_color.Width/* + this.comboBox_library.Width*/,
                    this.m_nTopY);

                if (this.checkBox_arrived != null)
                {
                    this.checkBox_arrived.Location = new Point(this.m_nTopX + this.label_color.Width + this.comboBox_location.Width,
                       this.m_nTopY);
                }

                // TODO: �޸�������С?
            }
        }

        public string LocationString
        {
            get
            {
                return this.comboBox_location.Text;
            }
            set
            {
                this.comboBox_location.Text = value;
            }
        }

        // �����
        public string No
        {
            get
            {
                return this.label_color.Text;
            }
            set
            {
                this.label_color.Text = value;
            }
        }

        public bool Arrived
        {
            get
            {
                Debug.Assert(this.checkBox_arrived != null, "");
                return this.checkBox_arrived.Checked;
            }
            set
            {
                Debug.Assert(this.checkBox_arrived != null, "");

                this.DisableArrivedCheckedChanged++;    // 2008/12/17 new add
                try
                {
                    this.checkBox_arrived.Checked = value;
                }
                finally
                {
                    this.DisableArrivedCheckedChanged--;
                }

                if (value == false)
                {
                    // this.checkBox_arrived.Text = "";    // TODO: ������û�б�Ҫ��id�������ʵ���Բ���(�����ĺô������¹�ѡ��id����)���ڱ����¼�����׶��پ�����(�����¼��ʱ������壬��Ϊid������������check״̬)
                }
                else
                {
                    if (this.checkBox_arrived.Text == "")
                    {
                        this.checkBox_arrived.Text = "*";   // ��ʾ�µ�����

                        // Ϊ�β�������?
                        // this.Container.toolTip1.SetToolTip(this.checkBox_arrived, this.checkBox_arrived.Text);
                    }
                }
            }
        }

        public string ArrivedID
        {
            get
            {
                Debug.Assert(this.checkBox_arrived != null, "");

                // ���checkedΪtrue������textΪ�գ��򷵻��Ǻš��Ա��������֪������true��״̬
                if (this.checkBox_arrived.Checked == true
                    && string.IsNullOrEmpty(this.checkBox_arrived.Text) == true)
                {
                    return "*";
                }

                return this.checkBox_arrived.Text;
            }
            set
            {
                Debug.Assert(this.checkBox_arrived != null, "");
                this.checkBox_arrived.Text = value;

                // Ϊ�β�������?
                // this.Container.toolTip1.SetToolTip(this.checkBox_arrived, this.checkBox_arrived.Text);

                if (string.IsNullOrEmpty(value) == false)
                {
                    if (this.checkBox_arrived.Checked != true)
                    {
                        this.DisableArrivedCheckedChanged++;    // 2009/12/17 new add
                        try
                        {
                            this.checkBox_arrived.Checked = true;   // ��������״̬
                        }
                        finally
                        {
                            this.DisableArrivedCheckedChanged--;
                        }
                    }
                }

                // ������checked == true��ʱ��text�Կ���Ϊ�ա���������ʾ�����ġ���δ����id�ַ���������
                // �����ַ�����Ϊ�յ�ʱ��checked���Բ���Ϊfalse
            }
        }

        void AddEvents()
        {
            // label_color
            this.label_color.MouseUp -= new MouseEventHandler(label_color_MouseUp);
            this.label_color.MouseUp += new MouseEventHandler(label_color_MouseUp);

            label_color.MouseClick -= new MouseEventHandler(label_color_MouseClick);
            label_color.MouseClick += new MouseEventHandler(label_color_MouseClick);

            /*
            // library
            this.comboBox_library.DropDown -= new EventHandler(comboBox_location_DropDown);
            this.comboBox_library.DropDown += new EventHandler(comboBox_location_DropDown);
             * */


            // location
            this.comboBox_location.DropDown -= new EventHandler(comboBox_location_DropDown);
            this.comboBox_location.DropDown += new EventHandler(comboBox_location_DropDown);

            this.comboBox_location.TextChanged -= new EventHandler(comboBox_location_TextChanged);
            this.comboBox_location.TextChanged += new EventHandler(comboBox_location_TextChanged);

            this.comboBox_location.Enter -= new EventHandler(comboBox_location_Enter);
            this.comboBox_location.Enter += new EventHandler(comboBox_location_Enter);

            // arrived
            this.checkBox_arrived.CheckedChanged -= new EventHandler(checkBox_arrived_CheckedChanged);
            this.checkBox_arrived.CheckedChanged += new EventHandler(checkBox_arrived_CheckedChanged);
        }

        // �ѵ���(����)checkbox��checked
        // 2008/4/16 new add
        void checkBox_arrived_CheckedChanged(object sender, EventArgs e)
        {
            this.Container.Changed = true;

            if (this.DisableArrivedCheckedChanged == 0)
                this.Container.OnArrivedChanged();
        }

        void label_color_MouseClick(object sender, MouseEventArgs e)
        {
            this.Container.Focus(); // 2008/9/16 new add

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

        void comboBox_location_Enter(object sender, EventArgs e)
        {
            this.Container.SelectItem(this, true);
        }

        // location���ָı�
        void comboBox_location_TextChanged(object sender, EventArgs e)
        {
#if NO
            // �����ǰѡ�����������һ������ѡ������������ҲҪ�޸�
            List<LocationItem> selected = this.Container.SelectedItems;
            for (int i = 0; i < selected.Count; i++)
            {
                LocationItem item = selected[i];
                if (item == this)
                    continue;

                if (item.LocationString != this.LocationString)
                {
                    item.LocationString = this.LocationString;

                    if ((item.State & ItemState.New) == 0)
                        item.State |= ItemState.Changed;
                }
            }
#endif


            // �����鲢������˳��
            if (Control.ModifierKeys == Keys.Control)
            {
            }
            else
            {
                if (this.Container.m_nDontMerge == 0)
                    this.Container.Merge();
            }

            this.Container.RefreshLineAndText();

            if ((this.State & ItemState.New) == 0)
            {
                this.State |= ItemState.Changed;
                // TODO: ��һ���¼�?
            }

            this.Container.Changed = true;
        }

        // ��ֹ���� 2009/7/19 new add
        int m_nInDropDown = 0;

        void comboBox_location_DropDown(object sender, EventArgs e)
        {
            // ��ֹ���� 2009/7/19 new add
            if (this.m_nInDropDown > 0)
                return;

            Cursor oldCursor = this.Container.Cursor;
            this.Container.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {
                ComboBox combobox = (ComboBox)sender;

                if (combobox.Items.Count == 0
                    && this.Container.HasGetValueTable() != false)
                {
                    GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                    e1.DbName = this.Container.DbName;

                    if (combobox == this.comboBox_location)
                        e1.TableName = "location";
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

            // 2012/5/30
            menuItem = new MenuItem("��ֵ");
            if (nSelectedCount == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            List<string> values = GetLocationListItem();
            if (values.Count > 0)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    string strText = values[i];
                    MenuItem subMenuItem = new MenuItem(strText);
                    subMenuItem.Tag = strText;
                    subMenuItem.Click += new System.EventHandler(this.menu_setLocationString_Click);
                    menuItem.MenuItems.Add(subMenuItem);
                }
            }
            else
                menuItem.Enabled = false;

            menuItem = new MenuItem("ȫѡ(&A)");
            menuItem.Click += new System.EventHandler(this.menu_selectAll_Click);
            contextMenu.MenuItems.Add(menuItem);


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


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

            menuItem = new MenuItem("ǿ������ο�ID(&R)");
            menuItem.Click += new System.EventHandler(this.menu_clearRefIDs_Click);
            if (this.Container.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("�ϲ�ͬ������(&M)");
            menuItem.Click += new System.EventHandler(this.menu_merge_Click);
            contextMenu.MenuItems.Add(menuItem);


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("ɾ��(&D)");
            menuItem.Click += new System.EventHandler(this.menu_deleteElements_Click);
            if (this.Container.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.label_color, new Point(e.X, e.Y));
        }

        List<string> m_locationListItems = new List<string>();

        List<string> GetLocationListItem()
        {
            if (m_locationListItems.Count > 0)
                return m_locationListItems;

            GetValueTableEventArgs e1 = new GetValueTableEventArgs();
            e1.DbName = this.Container.DbName;
            e1.TableName = "location";

            this.Container.OnGetValueTable(this, e1);

            if (e1.values != null)
            {
                for (int i = 0; i < e1.values.Length; i++)
                {
                    m_locationListItems.Add(e1.values[i]);
                }
            }

            return m_locationListItems;
        }

        void menu_selectAll_Click(object sender, EventArgs e)
        {
            this.Container.SelectAll();
        }

        // һ�����޸Ķ��combobox��ֵ
        void menu_setLocationString_Click(object sender, EventArgs e)
        {
            string strValue = (string)((MenuItem)sender).Tag;

            foreach (LocationItem item in this.Container.SelectedItems)
            {
                if (item.LocationString != strValue)
                    item.LocationString = strValue;
            }
        }

        void menu_insertElement_Click(object sender, EventArgs e)
        {
            int nPos = this.Container.LocationItems.IndexOf(this);

            if (nPos == -1)
                throw new Exception("not found myself");

            this.Container.InsertNewItem(nPos);
        }

        void menu_appendElement_Click(object sender, EventArgs e)
        {
            int nPos = this.Container.LocationItems.IndexOf(this);
            if (nPos == -1)
            {
                throw new Exception("not found myself");
            }

            this.Container.InsertNewItem(nPos + 1);
        }

        // �ϲ�ͬ������
        void menu_merge_Click(object sender, EventArgs e)
        {
            this.Container.Merge();
        }

        // ǿ������ο�ID
        void menu_clearRefIDs_Click(object sender, EventArgs e)
        {
            List<LocationItem> selected_lines = this.Container.SelectedItems;

            if (selected_lines.Count == 0)
            {
                MessageBox.Show(this.Container, "��δѡ��Ҫ����ο�ID������");
                return;
            }

            string strText = "";

            if (selected_lines.Count == 1)
                strText = "ȷʵҪ������� '" + selected_lines[0].No + "' �Ĳο�ID? ";
            else
                strText = "ȷʵҪ�����ѡ���� " + selected_lines.Count.ToString() + " ������Ĳο�ID?";

            DialogResult result = MessageBox.Show(this.Container,
                strText,
                "LocationEditControl",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                return;
            }

            int nNotChangeCount = 0;
            int nChangedCount = 0;

            for (int i = 0; i < selected_lines.Count; i++)
            {
                LocationItem item = selected_lines[i];
                if ((item.State & ItemState.ReadOnly) != 0)
                {
                    nNotChangeCount++;
                    continue;
                }

                if (item.Arrived == true)
                    item.ArrivedID = "*";
                else
                    item.ArrivedID = "";

                nChangedCount++;
            }

            if (nNotChangeCount > 0)
            {
                MessageBox.Show(this.Container, "�� " + nNotChangeCount.ToString() + " ��ֻ������δ�������ο�ID");
            }
        }

        // ɾ����ǰԪ��
        void menu_deleteElements_Click(object sender, EventArgs e)
        {
            List<LocationItem> selected_lines = this.Container.SelectedItems;

            if (selected_lines.Count == 0)
            {
                MessageBox.Show(this.Container, "��δѡ��Ҫɾ��������");
                return;
            }
            string strText = "";

            if (selected_lines.Count == 1)
                strText = "ȷʵҪɾ������ '" + selected_lines[0].No + "'? ";
            else
                strText = "ȷʵҪɾ����ѡ���� " + selected_lines.Count.ToString() + " ������?";

            DialogResult result = MessageBox.Show(this.Container,
                strText,
                "LocationEditControl",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                return;
            }

            int nNotDeleteCount = 0;
            int nDeletedCount = 0;

            for (int i = 0; i < selected_lines.Count; i++)
            {
                LocationItem item = selected_lines[i];
                if ((item.State & ItemState.ReadOnly) != 0)
                {
                    nNotDeleteCount++;
                    continue;
                }
                this.Container.RemoveItem(item, false);
                nDeletedCount++;
            }

            if (nDeletedCount > 0)
            {
                this.Container.SetSize();
                this.Container.LayoutItems();
            }

            if (nNotDeleteCount > 0)
            {
                MessageBox.Show(this.Container, "�� " + nNotDeleteCount.ToString() + " ��ֻ������δ��ɾ��");
            }
        }
    }



    /*
    /// <summary>
    /// ReadOnly״̬�����ı�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void ReadOnlyChangedEventHandler(object sender,
    ReadOnlyChangedEventArgs e);

    /// <summary>
    /// ReadOnly״̬�����ı�Ĳ���
    /// </summary>
    public class ReadOnlyChangedEventArgs : EventArgs
    {
        
    }
     * */

    // ���ӡ�����Ŀ�Ĳ��ܴﵽ���쳣
    public class NotEnoughException : Exception
    {
        public int WantValue = 0;   // ��Ҫ�ı��ֵ
        public int DoneValue = 0;   // ʵ�ʸı��ֵ

        public NotEnoughException(string s)
            : base(s)
		{
		}
    }
}
