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
    /// �ڿ�ͼ�ν���ؼ�
    /// </summary>
    internal partial class BindingControl : Control
    {
#if NOOOOOOOOOOOOOO

        int m_nDirectionAB = 0;
        int m_nDirectionBC = 0;

#endif

        /*
        bool m_bChanged = false;

        public bool Changed
        {
            get
            {
                return this.m_bChanged;
            }
            set
            {
                this.m_bChanged = value;
            }
        }*/

        public string Operator = "";    // ��ǰ�������ʻ���
        public string LibraryCodeList = "";     // ��ǰ�û���Ͻ�Ĺݴ����б�

        public bool HideLockedOrderGroup
        {
            // �Ƿ�������ȫ�ڹ�Ͻ��Χ����Ķ����顣�����ڵ�ʱ���Զ������������ɵĶ����飬�����Ϊ���ܴ˱����Ŀ���
            get
            {
                return this.m_bHideLockedOrderGroup;
            }
            set
            {
                this.m_bHideLockedOrderGroup = value;
                this.m_bHideLockedBindingCell = value;
            }
        }

        internal bool m_bHideLockedOrderGroup = false;    // �Ƿ�������ȫ�ڹ�Ͻ��Χ����Ķ����顣�����ڵ�ʱ���Զ������������ɵĶ����飬�����Ϊ���ܴ˱����Ŀ���
        internal bool m_bHideLockedBindingCell = false;    // �Ƿ������ڹ�Ͻ��Χ����ĺ϶���

        public string WholeLayout = "auto"; // auto/acception/binding

        internal bool m_bChanged = false;    // ��Ҫ�����Ƿ�ɾ��������

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                if (this.m_bChanged == true)
                    return true;

                for (int i = 0; i < this.Issues.Count; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    if (issue.Virtual == true)
                        continue;
                    if (issue.Changed == true)
                        return true;
                    for (int j = 0; j < issue.Cells.Count; j++)
                    {
                        Cell cell = issue.Cells[j];
                        if (cell != null && cell.item != null)
                        {
                            if (cell.item.Deleted == true)
                                continue;
                            if (cell.item.Changed == true
                                || cell.item.NewCreated == true)
                                return true;
                        }
                    }
                }

                return false;
            }
            set
            {
                this.m_bChanged = value;

                if (value == false)
                {
                    for (int i = 0; i < this.Issues.Count; i++)
                    {
                        IssueBindingItem issue = this.Issues[i];
                        if (issue.Virtual == true)
                            continue;
                        issue.Changed = value;
                        for (int j = 0; j < issue.Cells.Count; j++)
                        {
                            Cell cell = issue.Cells[j];
                            if (cell != null && cell.item != null)
                            {
                                cell.item.Changed = value;
                            }
                        }
                    }
                }
            }
        }

        public string[] DefaultTextLineNames = new string[] {
            "location", "�ݲصص�",
            "intact", "�����",
            "state", "��״̬",
            "refID", "�ο�ID",
            "barcode", "�������",
        };

        /*
        public string[] TextLineNames = new string[] {
            "location", "�ݲصص�",
            "intact", "�����",
            "state", "��״̬",
            "refID", "�ο�ID",
            "barcode", "�������",
        };
         * */
        public string[] TextLineNames = null;

        public string[] DefaultGroupTextLineNames = new string[] {
            "seller", "��������",
            "source", "������Դ",
            "price", "����۸�",
            "range", "ʱ�䷶Χ",
            "batchNo", "���κ�",
        };

        public string[] GroupTextLineNames = null;

        public ApplicationInfo AppInfo = null;

        /// <summary>
        /// �Ƿ�Ϊ�´����Ĳ��¼���á��ӹ��С�״̬
        /// </summary>
        public bool SetProcessingState = true;

        bool m_bBindingBatchNoInputed = false; // �Ƿ��������װ�����κ�
        bool m_bAcceptingBatchNoInputed = false;    // �Ƿ���������������κ�

        /// <summary>
        /// �༭������
        /// </summary>
        public event EditAreaEventHandler EditArea = null;

        /// <summary>
        /// ���㷢���ı�
        /// </summary>
        public event FocusChangedEventHandler CellFocusChanged = null;

        Cell m_lastHoverObj = null;

        CellBase m_lastFocusObj = null;


        CellBase DragStartObject
        {
            get
            {
                return this.m_DragStartObject;
            }
            set
            {
                this.m_DragStartObject = value;

                /*
                if (value != null)
                    this.FocusObject = value;   // new changed
                 * */

                if (value != null)
                    SetObjectFocus(m_DragStartObject);
            }
        }

        // �϶���;��������Ķ���
        CellBase DragLastEndObject
        {
            get
            {
                return this.m_DragLastEndObject;
            }
            set
            {
                this.m_DragLastEndObject = value;


                /*
                 * // ���ҪΧѡ�Ĺ�����Ҳ��focus�����ƶ��Ļ�
                if (m_bRectSelecting == true)
                DrawSelectRect(true);


                    this.FocusObject = value;   // new changed
                    this.Update();

                if (m_bRectSelecting == true)
                    DrawSelectRect(true);
                 * */

                // Χѡ������û��focus�����ƶ�
                if (value != null)
                    SetObjectFocus(m_DragLastEndObject);
            }
        }

        public bool CheckProcessingState(ItemBindingItem item)
        {
            // ���ؼ��
            if (this.SetProcessingState == false)
                return true;

            if (item.IsProcessingState() == true)
                return true;

            return false;
        }

        // �϶���ʼʱ�Ķ���
        CellBase m_DragStartObject = null;

        // �϶���;��������Ķ���
        CellBase m_DragLastEndObject = null;
        // �϶���ʼʱ�����λ�ã�view����
        Point DragStartMousePosition = new Point(0, 0);
        //
                // ������϶���ʼʱ��λ�� �����ĵ�����
        PointF m_DragStartPointOnDoc = new PointF(0, 0);

        // ������϶���;ʱ��λ�� �����ĵ�����
        PointF m_DragCurrentPointOnDoc = new PointF(0, 0);

        bool m_bRectSelectMode = true;
        bool m_bRectSelecting = false;  // ���ھ���ѡ����;


        bool m_bDraging = false;    // ������ק��; 2010/2/12

        ToolTip trackTip;

        // �������������ʾ��ѡ������������(����̫��ȷ)
        List<CellBase> m_aSelectedArea = new List<CellBase>();
        bool m_bSelectedAreaOverflowed = true;  // true������������û���������ʱ�����

        // ���㵥Ԫ
        public Cell FocusedCell = null;



        // ��ò���Ϣ
        public event GetItemInfoEventHandler GetItemInfo = null;

        // ��ö�����Ϣ
        public event GetOrderInfoEventHandler GetOrderInfo = null;


        // �� ����
        public List<IssueBindingItem> Issues = new List<IssueBindingItem>();

        public IssueBindingItem FreeIssue = null;   // �����ģ����ɵ���

        // �϶��Ĳ� ����
        public List<ItemBindingItem> ParentItems = new List<ItemBindingItem>();

        /*
        // ��ʼ��ʱ���ڿ������Ͻ�ĵ��� ����
        // ��ʼ�������󣬾����
        internal List<ItemBindingItem> NoneIssueItems = new List<ItemBindingItem>();
         * */

        // ��ʼ��ʱ���в���� ����
        // ��ʼ�������󣬾����
        internal List<ItemBindingItem> InitialItems = new List<ItemBindingItem>();

        BorderStyle borderStyle = BorderStyle.Fixed3D;

        #region ͼ����س�Ա


        public bool DisplayOrderInfoXY = false;

        // ��ͨ����ĵ�Ԫ��ɫ
        //public Color BackColor = Color.White;   // ����ɫ
        //public Color ForeColor = Color.Black;   // ǰ��ɫ��Ҳ����������ɫ
        public Color GrayColor = Color.Gray;   // ǳɫ����

        // ѡ��״̬�ĵ�Ԫ��ɫ
        public Color SelectedBackColor = Color.DarkRed; // Color.FromArgb(200, 255, 100, 100);    // ����ɫ
        public Color SelectedForeColor = Color.Black;   // ǰ��ɫ��Ҳ����������ɫ
        public Color SelectedGrayColor = Color.FromArgb(170, 170, 255);  // ǳɫ����

        // ����ĵ�Ԫ��ɫ
        public Color SingleBackColor = Color.White;    // ����ɫ
        public Color SingleForeColor = Color.Black;   // ǰ��ɫ��Ҳ����������ɫ
        public Color SingleGrayColor = Color.DarkGray;   // ǳɫ����

        // �϶���Ա�ĵ�Ԫ��ɫ
        public Color MemberBackColor = Color.FromArgb(200, 200, 200);    // ����ɫ
        public Color MemberForeColor = Color.White;   // ǰ��ɫ��Ҳ����������ɫ
        public Color MemberGrayColor = Color.FromArgb(180, 180, 180);   // ǳɫ����

        // �϶����ĵ�Ԫ��ɫ
        public Color ParentBackColor = Color.FromArgb(150, 150, 150);    // ����ɫ
        public Color ParentForeColor = Color.White;   // ǰ��ɫ��Ҳ����������ɫ
        public Color ParentGrayColor = Color.FromArgb(130, 130, 130);   // ǳɫ����

        // ��ʾ�´����Ĳ������ɫ
        public Color NewBarColor = Color.FromArgb(255, 255, 0);

        // ��ʾ�������޸ĵĲ������ɫ
        public Color ChangedBarColor = Color.FromArgb(0, 255, 0);

        // �ڸ��ӵ�
        public Color IssueBoxBackColor = Color.FromArgb(255, Color.Black);  // ������ɫ // Color.FromArgb(200, Color.White);
        public Color IssueBoxForeColor = Color.FromArgb(255, Color.White);  // ǰ����ɫ
        public Color IssueBoxGrayColor = Color.DarkGray;   // ǳɫ����

        // Ԥ��ĵ�Ԫ��ɫ
        public Color CalculatedBackColor = Color.FromArgb(0, Color.White);   // ����ɫ
        public Color CalculatedForeColor = Color.Gray;   // ǰ��ɫ��Ҳ����������ɫ
        public Color CalculatedGrayColor = Color.Yellow;   // ǳɫ����������������ɫ

        // �϶������
        public Color FixedBorderColor = Color.DarkBlue; // Color.FromArgb(100, 110, 100) �̻��ĺ϶���Χ���
        public Color NewlyBorderColor = Color.DarkBlue;  // DarkGreen ���޸ĵĺ϶���Χ���

        public enum BoundLineStyle
        {
            Curve = 0,
            Line = 1,
        }

        // �϶��������ߵķ��
        public BoundLineStyle LineStyle = BoundLineStyle.Curve;

        // ������ɫ�����֣�
        // http://msdn.microsoft.com/en-us/library/system.windows.media.color(VS.95).aspx

        // ���ڵ�������
        internal int m_nMaxItemCountOfOneIssue = -1; // -1 ��ʾ��δ��ʼ��

        int nNestedSetScrollBars = 0;

        // ��������� С�ڵ���1.0F
        double m_v_ratio = 1.0F;
        double m_h_ratio = 1.0F;

        int m_nLeftBlank = 20;	// �߿�
        int m_nRightBlank = 20;
        int m_nTopBlank = 20;
        int m_nBottomBlank = 20;

        long m_lWindowOrgX = 0;    // ����ԭ��
        long m_lWindowOrgY = 0;

        long m_lContentWidth = 0;    // ���ݲ��ֵĿ�ȡ�������߱��⣬���ɸ��ӡ����������ҿհ�
        long m_lContentHeight = 0;   // ���ݲ��ֵĸ߶�

        internal Font m_fontLine = null;    // ������ÿ�����ֵ�����
        internal Font m_fontTitleSmall = null;   // ���������ֵ����壬С��
        internal Font m_fontTitleLarge = null;   // ���������ֵ����壬���

        internal int m_nLineHeight = 16;  // ���֣�ÿ�еĸ߶� 18

        internal int m_nCellHeight = 110;   // 70
        internal int m_nCellWidth = 130;
        internal int m_nLeftTextWidth = 130;

        internal Padding CellMargin = new Padding(6);
        internal Padding CellPadding = new Padding(8);

        // internal Padding LeftTextMargin = new Padding(6);
        internal Padding LeftTextMargin {get;set;}

        internal Padding LeftTextPadding = new Padding(0);

        internal Rectangle RectGrab
        {
            get
            {
                return m_rectGrab;
            }
            set
            {
                m_rectGrab = value;
            }
        }

        Rectangle m_rectGrab = new Rectangle(4, 4, 16, 16); // dr g h ndle����(��Cell������)

        #endregion

        public BindingControl()
        {
            this.LeftTextMargin = new Padding(6);

            this.DoubleBuffered = true;

            this.TextLineNames = this.DefaultTextLineNames;
            this.GroupTextLineNames = this.DefaultGroupTextLineNames;

            InitializeComponent();

            trackTip = new ToolTip();

            int nFontHeight = this.m_nLineHeight - 4;
            this.m_fontLine = new Font("΢���ź�",    // "Arial",
                nFontHeight,
                FontStyle.Regular,
                GraphicsUnit.Pixel);

            nFontHeight = this.m_nLineHeight - 4;
            this.m_fontTitleSmall = new Font("΢���ź�",    // "Arial",
                nFontHeight,
                FontStyle.Bold,
                GraphicsUnit.Pixel);

            nFontHeight = this.m_nLineHeight + 4;
            this.m_fontTitleLarge = new Font("΢���ź�",    // "Arial",
                nFontHeight,
                FontStyle.Bold,
                GraphicsUnit.Pixel);

        }

        string m_strBiblioDbName = "";

        // ��ȡֵ�б�ʱ��Ϊ���������ݿ���
        public string BiblioDbName
        {
            get
            {
                return this.m_strBiblioDbName;
            }
            set
            {
                this.m_strBiblioDbName = value;
            }
        }

        public void Clear()
        {
            this.Issues.Clear();
        }

        // �������صĲ�����
        internal List<ItemBindingItem> m_hideitems = new List<ItemBindingItem>();
        public List<ItemBindingItem> AllHideItems
        {
            get
            {
                return this.m_hideitems;
            }
        }

        // �ⲿ�ӿ�
        // ������ʾ�����ĺ����صĲ�����
        // �������Ѿ�ɾ��������
        public List<ItemBindingItem> AllItems
        {
            get
            {
                List<ItemBindingItem> results = new List<ItemBindingItem>();
                results.AddRange(this.AllVisibleItems);
                results.AddRange(this.AllHideItems);

                return results;
            }
        }

        // �ⲿ�ӿ�
        // ������ʾ�����Ĳ�����
        // �������Ѿ�ɾ��������
        public List<ItemBindingItem> AllVisibleItems
        {
            get
            {
                List<ItemBindingItem> results = new List<ItemBindingItem>();
                for (int i = 0; i < this.Issues.Count; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    for (int j = 0; j < issue.Cells.Count; j++)
                    {
                        Cell cell = issue.Cells[j];
                        if (cell != null 
                            && !(cell is GroupCell)
                            && cell.item != null
                            && cell.item.Deleted == false
                            && cell.item.Calculated == false)
                            results.Add(cell.item);
                    }
                }

                return results;
            }
        }

        // 2012/9/25
        public static ItemBindingItem FindItemByRefID(string strRefID,
            List<ItemBindingItem> items)
        {
            foreach (ItemBindingItem item in items)
            {
                if (item == null)
                    continue;
                if (item.RefID == strRefID)
                    return item;
            }

            return null;
        }

        // ���ڡ���������Σ�����һ���ض�refid�Ĳ�����
        // ֻ�����ڳ�ʼ���׶ε�ǰ��
        internal ItemBindingItem InitialFindItemByRefID(string strRefID)
        {
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];

                for (int j = 0; j < issue.Items.Count; j++)
                {
                    ItemBindingItem item = issue.Items[j];
                    if (item.RefID == strRefID)
                        return item;
                }
            }

            return null;
        }

        // ���ڡ���������Σ�����һ���ض�refid�Ĳ�����
        // ֻ�����ڳ�ʼ���׶ε�ǰ��
        internal Cell FindCellByRefID(string strRefID,
            IssueBindingItem exclude_issue)
        {
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];

                if (issue == exclude_issue)
                    continue;

                for (int j = 0; j < issue.Cells.Count; j++)
                {
                    Cell cell = issue.Cells[j];
                    if (cell == null || cell.item == null)
                        continue;
                    if (cell.item.RefID == strRefID)
                        return cell;
                }
            }

            return null;
        }

        // ���ŵ����Ĳ�(���Ǻ϶���Ա��)�������һ����λ
        // return:
        //      ʵ�ʰ��ŵĵ���indexλ��
        static int PlaceSingleToTail(ItemBindingItem item)
        {
            Debug.Assert(item.Container != null, "");

            IssueBindingItem issue = item.Container;

            // ���ұ��ң��ҵ���һ�����õĿ�λ
            int nPos = issue.Cells.Count;
            for (int i = issue.Cells.Count - 1; i >= 0; i--)
            {
                if (issue.IsBlankSingleIndex(i) == false)
                    break;
            }

            Cell cell = new Cell();
            cell.item = item;
            issue.SetCell(nPos, cell);
            return nPos;
        }

        // �°汾
        // �����г�ԱCell�����ƶ����ɸ�����
        public void MoveMemberCellsToRight(ItemBindingItem parent_item,
            int nDistance)
        {
            Debug.Assert(nDistance > 0, "");

            for (int i = 0; i < nDistance; i++)
            {
                MoveMemberCellsToRight(parent_item);
            }
        }

        // �°汾
        // �����г�ԱCell�����ƶ�һ������
        public void MoveMemberCellsToRight(ItemBindingItem parent_item)
        {
#if DEBUG
            int nCol = -1;
#endif
            for (int i = 0; i < parent_item.MemberCells.Count; i++)
            {
                Cell cell = parent_item.MemberCells[i];

                // ItemBindingItem item = cell.item;

                IssueBindingItem issue = cell.Container;
                Debug.Assert(issue != null, "");

                if (issue.IssueLayoutState == IssueLayoutState.Accepting)
                    continue;

                int index = issue.Cells.IndexOf(cell);
                Debug.Assert(index != -1, "");

#if DEBUG
                if (nCol != -1)
                {
                    if (nCol != index)
                    {
                        Debug.Assert(false, "����ͬһ���϶���ĸ�����Ա����index��Ȼ��ͬ");
                    }
                }

                nCol = index;
#endif

                // ע�⣬indexʵ����Ϊ˫����Ҳ����

                issue.GetBlankSingleIndex(index + 1);   // +1Ϊ�ƶ�����Ҳ���ӡ�ȷ�������и����񼴿ɣ�������ȥȷ�����󷽵�˫����Ϊ�����漰���ѱ����϶���Χռ�ݵ�λ��

                // ����һ��˫�񵽱�
                // �����ܱȽ�ԭʼ��������ѹ��λ
                // parameters:
                //      nSourceIndex    Դindexλ�á�ע�⣬������˫������
                //      nTargetIndex    Ŀ��indexλ�á�ע�⣬������˫������
                issue.CopyDoubleIndexTo(
                    index - 1,
                    index,
                    true);
                /*
                {
                    // �ᶯ�Ҳ��������
                    Cell right_cell = issue.GetCell(index);
                    issue.SetCell(index + 2, right_cell);
                    issue.SetCell(index, null);
                }

                {
                    // �������λ��? �Ѻ϶�����Ҳһ���ƶ���
                    Cell temp_cell = issue.GetCell(index - 1);
                    if (cell != null)
                    {
                        issue.SetCell(index  - 1 + 2, temp_cell);
                    }
                    issue.SetCell(index - 1, null);
                }
                 * */
            }
        }

#if OLD_VERSION
        // �����г�ԱCell�����ƶ�һ��˫��
        public void MoveMemberCellsToRight(ItemBindingItem parent_item)
        {
            for(int i=0;i<parent_item.MemberCells.Count;i++)
            {
                Cell cell = parent_item.MemberCells[i];

                // ItemBindingItem item = cell.item;

                IssueBindingItem issue = cell.Container;
                Debug.Assert(issue != null, "");
                int index = issue.Cells.IndexOf(cell);
                Debug.Assert(index != -1, "");

                issue.GetBlankPosition((index/2)+1, parent_item);

                {
                    Cell temp_cell = issue.GetCell(index);
                    issue.SetCell(index + 2, temp_cell);
                    issue.SetCell(index, null);
                }

                {
                    // �����϶�λ��? �Ѻ϶�����Ҳһ���ƶ���
                    Cell temp_cell = issue.GetCell(index - 1);
                    if (cell != null)
                    {
                        issue.SetCell(index + 2 - 1, temp_cell);
                        issue.SetCell(index - 1, null);
                    }
                }
            }
        }
#endif

        //�°汾
        // �۲�һ���϶���������ڣ����Ƿ����������һ������
        // Ҳ���ǿ�������Ƿ��ǿհ�λ��
        public bool CanMoveToLeft(Cell parent_cell)
        {
            Debug.Assert(parent_cell != null, "");
            Debug.Assert(parent_cell.Container != null, "");
            Debug.Assert(parent_cell.item != null, "");

            int nCol = parent_cell.Container.IndexOfCell(parent_cell);
            Debug.Assert(nCol != -1, "");

            if (nCol <= 0)
                return false;

            IssueBindingItem parent_issue = parent_cell.Container;
            Debug.Assert(parent_issue != null, "");

            if (parent_cell.item.MemberCells.Count == 0)
            {
                if (parent_issue.IssueLayoutState == IssueLayoutState.Binding)
                {
                    if (parent_issue.IsBlankDoubleIndex(nCol - 1, parent_cell.item) == false)
                        return false;
                    return true;
                }
                else
                {
                    Debug.Assert(parent_issue.IssueLayoutState == IssueLayoutState.Accepting, "");
                    return false;
                }
            }
            else
            {
                IssueBindingItem first_issue = null;
                IssueBindingItem last_issue = null;

                first_issue = parent_cell.item.MemberCells[0].Container;
                Debug.Assert(first_issue != null, "");
                last_issue = parent_cell.item.MemberCells[parent_cell.item.MemberCells.Count - 1].Container;
                Debug.Assert(last_issue != null, "");

                int nFirstLineNo = this.Issues.IndexOf(first_issue);
                Debug.Assert(nFirstLineNo != -1, "");

                // ���żȻ���ֵ�һ����Ա���ں϶���ͬ�ڵ������������С�к�
                int nParentLineNo = this.Issues.IndexOf(parent_issue);
                if (nParentLineNo < nFirstLineNo)
                    nFirstLineNo = nParentLineNo;

                int nLastLineNo = this.Issues.IndexOf(last_issue);
                Debug.Assert(nLastLineNo != -1, "");

                bool bAllInAcceptingLayout = true;
                for (int i = nFirstLineNo; i <= nLastLineNo; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    Debug.Assert(issue != null, "");

                    if (issue.IssueLayoutState == IssueLayoutState.Binding)
                    {
                        if (issue.IsBlankDoubleIndex(nCol - 1, parent_cell.item) == false)
                            return false;
                        bAllInAcceptingLayout = false;
                    }
                }
                if (bAllInAcceptingLayout == false)
                    return true;

                return false;
            }
        }


#if NOOOOOOOOOOOOOOOOO

        // �۲�һ���϶���������ڣ����Ƿ����������һ��˫��
        // Ҳ���ǿ�������Ƿ��ǿհ�λ��
        public bool CanMoveToLeft(Cell parent_cell)
        {
            Debug.Assert(parent_cell != null, "");
            Debug.Assert(parent_cell.Container != null, "");
            Debug.Assert(parent_cell.item != null, "");

            int nCol = parent_cell.Container.IndexOfCell(parent_cell);
            Debug.Assert(nCol != -1, "");

            if (nCol <= 0)
                return false;

            IssueBindingItem parent_issue = parent_cell.Container;
            Debug.Assert(parent_issue != null, "");

            if (parent_cell.item.MemberCells.Count == 0)
            {
                if (parent_issue.IsBlankPosition((nCol / 2) - 1, null) == false)
                    return false;
            }
            else
            {
                IssueBindingItem first_issue = null;
                IssueBindingItem last_issue = null;

                first_issue = parent_cell.item.MemberCells[0].Container;
                Debug.Assert(first_issue != null, "");
                last_issue = parent_cell.item.MemberCells[parent_cell.item.MemberCells.Count-1].Container;
                Debug.Assert(last_issue != null, "");

                int nFirstLineNo = this.Issues.IndexOf(first_issue);
                Debug.Assert(nFirstLineNo != -1, "");

                // ���żȻ���ֵ�һ����Ա���ں϶���ͬ�ڵ������������С�к�
                int nParentLineNo = this.Issues.IndexOf(parent_issue);
                if (nParentLineNo < nFirstLineNo)
                    nFirstLineNo = nParentLineNo;

                int nLastLineNo = this.Issues.IndexOf(last_issue);
                Debug.Assert(nLastLineNo != -1, "");

                for (int i = nFirstLineNo; i <= nLastLineNo; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    Debug.Assert(issue != null, "");

                    if (issue.IsBlankPosition((nCol / 2)-1, null) == false)
                        return false;
                }
            }

            return true;
        }

#endif

        // �°汾
        // ���϶��������г�ԱCell�����ƶ�һ������
        // ע�⣺���ñ�����ǰ��Ҫ��CanMoveToLeft()����Ƿ��������ơ�����������ͻ
        public bool MoveCellsToLeft(Cell parent_cell)
        {
            Debug.Assert(parent_cell != null, "");
            Debug.Assert(parent_cell.Container != null, "");
            Debug.Assert(parent_cell.item != null, "");

            int nCol = parent_cell.Container.IndexOfCell(parent_cell);
            Debug.Assert(nCol != -1, "");

            if (nCol <= 0)
                return false;

            IssueBindingItem parent_issue = parent_cell.Container;
            Debug.Assert(parent_issue != null, "");

            // int nRet = 0;
            // string strError = "";
            bool bChanged = false;

            if (parent_cell.item.MemberCells.Count == 0)
            {
                if (parent_issue.IssueLayoutState == IssueLayoutState.Binding)
                {

                    parent_issue.CopyDoubleIndexTo(
                        nCol,
                        nCol - 1,
                        true);

                    // ���ڳ�����һ����λ����ɾ��
                    // ���ܻ�����ݹ�
                    parent_issue.RemoveSingleIndex(nCol);

                    bChanged = true;
                }

                return bChanged;
            }
            else
            {
                IssueBindingItem first_issue = null;
                IssueBindingItem last_issue = null;

                first_issue = parent_cell.item.MemberCells[0].Container;
                Debug.Assert(first_issue != null, "");
                last_issue = parent_cell.item.MemberCells[parent_cell.item.MemberCells.Count - 1].Container;
                Debug.Assert(last_issue != null, "");

                int nFirstLineNo = this.Issues.IndexOf(first_issue);
                Debug.Assert(nFirstLineNo != -1, "");

                // ���żȻ���ֵ�һ����Ա���ں϶���ͬ�ڵ������������С�к�
                int nParentLineNo = this.Issues.IndexOf(parent_issue);
                if (nParentLineNo < nFirstLineNo)
                    nFirstLineNo = nParentLineNo;

                int nLastLineNo = this.Issues.IndexOf(last_issue);
                Debug.Assert(nLastLineNo != -1, "");

                for (int i = nFirstLineNo; i <= nLastLineNo; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    Debug.Assert(issue != null, "");

                    if (issue.IssueLayoutState == IssueLayoutState.Accepting)
                        continue;
                    issue.CopyDoubleIndexTo(
                         nCol,
                         nCol - 1,
                         true);
#if DEBUG
                    {
                        Cell cellTemp = issue.GetCell(nCol + 1);
                        Debug.Assert(cellTemp == null, "");
                    }
#endif

                    bChanged = true;
                }

                // ���ڳ�����һ�����еĿ�λ����ɾ��
                // ���ܻ�����ݹ�
                for (int i = nFirstLineNo; i <= nLastLineNo; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    Debug.Assert(issue != null, "");

                    if (issue.IssueLayoutState == IssueLayoutState.Accepting)
                        continue;

                    // �����ұ߿�����һ���еĺ϶��᷶Χ��������Ϊǰ���е�ɾ�����Ѿ�����ѹ���˺����λ
                    Cell cellTemp = issue.GetCell(nCol + 1);
                    if (cellTemp == null)
                    {
                        // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
                        // return:
                        //      -1  �ǡ�������˫������λ��
                        //      0   ����
                        //      1   �ǡ�������˫����Ҳ�λ��
                        int nRet = issue.IsBoundIndex(nCol + 1);
                        if (nRet == -1 || nRet == 1)
                        {
                            Debug.Assert(nRet != 1, "");
                        }
                        else
                            issue.RemoveSingleIndex(nCol+1);
                    }

                    bChanged = true;
                }
            }

            return bChanged;
        }

#if NOOOOOOOOOOOOOOOOOOOO
        // ���϶��������г�ԱCell�����ƶ�һ��˫��
        public bool MoveCellsToLeft(Cell parent_cell)
        {
            Debug.Assert(parent_cell != null, "");
            Debug.Assert(parent_cell.Container != null, "");
            Debug.Assert(parent_cell.item != null, "");

            int nCol = parent_cell.Container.IndexOfCell(parent_cell);
            Debug.Assert(nCol != -1, "");

            if (nCol <= 0)
                return false;

            IssueBindingItem parent_issue = parent_cell.Container;
            Debug.Assert(parent_issue != null, "");

            // int nRet = 0;
            // string strError = "";
            bool bChanged = false;

            if (parent_cell.item.MemberCells.Count == 0)
            {
                parent_issue.CopyPositionTo(
                    nCol / 2,
                    (nCol / 2) - 1,
                    true);

                // ���ڳ�����һ����λ����ɾ��
                // ���ܻ�����ݹ�
                parent_issue.RemovePosition(nCol / 2);

                bChanged = true;
            }
            else
            {
                IssueBindingItem first_issue = null;
                IssueBindingItem last_issue = null;

                first_issue = parent_cell.item.MemberCells[0].Container;
                Debug.Assert(first_issue != null, "");
                last_issue = parent_cell.item.MemberCells[parent_cell.item.MemberCells.Count - 1].Container;
                Debug.Assert(last_issue != null, "");

                int nFirstLineNo = this.Issues.IndexOf(first_issue);
                Debug.Assert(nFirstLineNo != -1, "");

                // ���żȻ���ֵ�һ����Ա���ں϶���ͬ�ڵ������������С�к�
                int nParentLineNo = this.Issues.IndexOf(parent_issue);
                if (nParentLineNo < nFirstLineNo)
                    nFirstLineNo = nParentLineNo;

                int nLastLineNo = this.Issues.IndexOf(last_issue);
                Debug.Assert(nLastLineNo != -1, "");

                for (int i = nFirstLineNo; i <= nLastLineNo; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    Debug.Assert(issue != null, "");

                    issue.CopyPositionTo(
                        nCol / 2,
                        (nCol / 2) - 1,
                        true);

                    bChanged = true;
                }

                // ���ڳ�����һ�����еĿ�λ����ɾ��
                // ���ܻ�����ݹ�
                for (int i = nFirstLineNo; i <= nLastLineNo; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    Debug.Assert(issue != null, "");

                    issue.RemovePosition(nCol / 2);

                    bChanged = true;
                }
            }

            return bChanged;
        }
#endif

        /*
        // ���Ҫ���ŵ�λ���Ѿ��������ݣ��������ƶ�����(����)
        int MoveToRight(IssueBindingItem issue,
            int nCol)
        {
            Debug.Assert((nCol % 2) == 1, "");

            if (issue.Cells.Count <= nCol)
                return 0;

            Cell cell = issue.Cells[nCol];
            if (cell != null && cell.item != null)
            {
                if (cell.item.ParentItem != null)
                {
                    MoveMemberCellsToRight(cell.item.ParentItem);
                }
            }
            else
            {
                ItemBindingItem item = issue.Cells[nCol];

                if (issue.Cells[nCol] != null)
                {
                    issue.Cells.Add(null);
                    issue.Cells.Add(null);
                    for (int j = issue.Cells.Count - 1; j >= nCol + 2; j--)
                    {
                        Cell temp = issue.Cells[j - 2];
                        issue.Cells[j] = temp;
                    }

                    issue.Cells[nCol] = null;
                }
            }

            return 1;
        }*/

        static int IndexOf(List<PublishTimeAndVolume> lists,
            string strPublishTime)
        {
            for (int i = 0; i < lists.Count; i++)
            {
                PublishTimeAndVolume item = lists[i];
                if (strPublishTime == item.PublishTime)
                    return i;
            }

            return -1;
        }

        public class PublishTimeAndVolume
        {
            public string PublishTime = "";
            public string Volume = "";
        }

        // ͳ��(bindingxml��)ȫ���϶���Ա����ʹ�ù���publishtime�ַ���
        int GetAllBindingXmlPublishTimes(
            out List<PublishTimeAndVolume> publishtimes,
            out string strError)
        {
            publishtimes = new List<PublishTimeAndVolume>();
            strError = "";

            // �����϶����������
            for (int i = 0; i < this.ParentItems.Count; i++)
            {
                ItemBindingItem parent_item = this.ParentItems[i];

                string strBindingXml = parent_item.Binding;
                if (String.IsNullOrEmpty(strBindingXml) == true)
                    continue;

                // ����refid, �ҵ�����������ЩItemBindingItem����
                XmlDocument dom = new XmlDocument();
                dom.LoadXml("<root />");
                try
                {
                    dom.DocumentElement.InnerXml = strBindingXml;
                }
                catch (Exception ex)
                {
                    strError = "�ο�IDΪ '" + parent_item.RefID + "' �Ĳ���Ϣ�У�<binding>Ԫ����ǶXMLװ��DOMʱ����: " + ex.Message;
                    return -1;
                }

                /*
                 * bindingxml�У�<item>Ԫ��δ����refID���ԡ�
                 * û��refID���ԣ���������һ����ɾ���˲��¼�ĵ�����Ϣ��Ԫ��������ȱ�������
                 * ȱ�ڿ��ܷ�����װ����Χ�ĵ�һ��������һ�ᣬҪ����ע��
                 * */
                XmlNodeList nodes = dom.DocumentElement.SelectNodes("item");
                if (nodes.Count == 0)
                    continue;
                for (int j = 0; j < nodes.Count; j++)
                {
                    XmlNode node = nodes[j];
                    string strPublishTime = DomUtil.GetAttr(node, "publishTime");
                    if (String.IsNullOrEmpty(strPublishTime) == true)
                        continue;
                    if (strPublishTime.IndexOf("-") != -1)
                        continue;   // �Ƿ񱨴�?

                    if (IndexOf(publishtimes, strPublishTime) != -1)
                        continue;   // �Ż�����������Щ�ظ������TODO: �Ƿ�Ҫ�����õ�һ���ǿյ�valume string?

                    string strVolume = DomUtil.GetAttr(node, "volume");
                    PublishTimeAndVolume item = new PublishTimeAndVolume();
                    item.PublishTime = strPublishTime;
                    item.Volume = strVolume;
                    publishtimes.Add(item);
                }
            }

            return 0;
        }

        // �°汾
        // ���ź϶���Ա��
        // parameters:
        //      items   Item���顣ע�������е�Item������ContainerΪnull������missing����
        //      strPublishTimeString    �������ʱ�䷶Χ�ַ���
        void PlaceMemberItems(
            Cell parent_cell,
            List<ItemBindingItem> items,
            int nCol,
            bool bSetBidingRange = true)
        {
            List<Cell> member_cells = new List<Cell>();

            for (int i = 0; i < items.Count; i++)
            {
                ItemBindingItem item = items[i];
                IssueBindingItem issue = item.Container;

                if (issue == null)
                {
                    // Debug.Assert(item.Missing == true, "");
                    Debug.Assert(String.IsNullOrEmpty(item.PublishTime) == false, "");
                    issue = this.FindIssue(item.PublishTime);
                    if (issue == null)
                    {
                        issue = this.NewIssue(item.PublishTime,
                            item.Volume);
                        // ���������ܻ�����һЩ�϶���Χ�Ķ��ѡ�
                        // ��Ҫ�޲����Ѵ�
                        Debug.Assert(false, "��Ӧ���ߵ������Ϊǰ���Ѿ�Ԥ�ȴ���������Virtual���ڶ���");
                    }
                    item.Container = issue;
                }


                Cell cell = new Cell();
                cell.item = item;
                cell.Container = issue;

                member_cells.Add(cell);
            }

            // ���������ĵ�����
            PlaceMemberCells(parent_cell,
                member_cells,
                nCol);

            if (bSetBidingRange == true)
            {
                // ���ܻ��׳��쳣
                SetBindingRange(parent_cell, false);
            }
        }

#if OLD_VERSION
        // ���ź϶���Ա��
        // TODO: SetCell�Ѿ���ȫ��������Լ�
        // parameters:
        //      items   Item���顣ע�������е�Item������ContainerΪnull������missing����
        //      strPublishTimeString    �������ʱ�䷶Χ�ַ���
        void PlaceMemberItems(
            ItemBindingItem parent_item,
            List<ItemBindingItem> items,
            int nCol,
            out string strPublishTimeString)
        {
            strPublishTimeString = "";

            Debug.Assert(nCol >= 0, "");
            Debug.Assert(items.Count != 0, "");

            List<IssueBindingItem> done_issues = new List<IssueBindingItem>();
            int nFirstLineNo = 99999;
            int nLastLineNo = -1;
            for (int i = 0; i < items.Count; i++)
            {
                ItemBindingItem item = items[i];
                IssueBindingItem issue = item.Container;

                if (issue == null)
                {
                    // Debug.Assert(item.Missing == true, "");

                    issue = this.FindIssue(item.PublishTime);
                    if (issue == null)
                    {
                        issue = this.NewIssue(item.PublishTime,
                            item.Volume);
                    }
                }

                Debug.Assert(issue != null, "");

                done_issues.Add(issue);

                int nIssueLineNo = this.Issues.IndexOf(issue);
                if (nFirstLineNo > nIssueLineNo)
                    nFirstLineNo = nIssueLineNo;

                if (nLastLineNo < nIssueLineNo)
                    nLastLineNo = nIssueLineNo;

                // ���item�����Ƿ��Ѿ�����
                int nExistIndex = issue.IndexOfItem(item);

                // ������������Ǹ�λ����
                if (nExistIndex == nCol)
                {
                    item.ParentItem = parent_item;

                    Cell temp_cell = issue.Cells[nExistIndex];
                    Debug.Assert(temp_cell != null, "");
                    temp_cell.ParentItem = parent_item;
                    Debug.Assert(temp_cell.IsMember == true, "");

                    parent_item.MemberCells.Remove(temp_cell);  // ����

                    parent_item.InsertMemberCell(temp_cell);
                    continue;
                }

                // ɾ���Ѿ����ڵ�Cell
                Cell exist_cell = null;
                if (nExistIndex != -1)
                {
                    Debug.Assert(false, "");
                    if ((nExistIndex % 2) == 0)
                    {
                        // ���������λ�ã��ͺ�����ˡ���Ϊ���������һ���϶��Ĳ�
                        throw new Exception("���ֽ�Ҫ���ŵ�����������Ȼ������Cellλ���Ѿ�����");
                    }
                    exist_cell = issue.GetCell(nExistIndex);
                    issue.Cells.RemoveAt(nExistIndex);

                    issue.Cells.RemoveAt(nExistIndex-1);    // ���һ����Ҳɾ��
                }

                issue.GetBlankDoubleIndex(nCol, parent_item);

                Cell cell = null;
                if (exist_cell == null)
                {
                    cell = new Cell();
                    cell.ParentItem = parent_item;
                    if (item.Missing == true)
                    {
                        // ֻ��ռ��λ��
                        cell.item = null;
                        Debug.Assert(item.Container == null, "");
                    }
                    else
                        cell.item = item;
                }
                else
                    cell = exist_cell;


                item.ParentItem = parent_item;
                issue.SetCell(nCol, cell);

                parent_item.MemberCells.Remove(cell);  // ����
                parent_item.InsertMemberCell(cell);
            }

            Debug.Assert(nFirstLineNo != 99999, "");
            Debug.Assert(nLastLineNo != -1,"");

            // 2009/12/16 
            strPublishTimeString = this.Issues[nFirstLineNo].PublishTime
            + "-"
            + this.Issues[nLastLineNo].PublishTime;


            // �������
            for (int i = nFirstLineNo; i <= nLastLineNo; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                if (done_issues.IndexOf(issue) != -1)
                    continue;

                {
                    Cell cell = issue.GetCell(nCol);
                    // ����ǿհ׸��ӣ�������������ֱ��ʹ��
                    if (cell != null
                        && cell.item == null
                        && cell.IsMember == false)
                    {
                        cell.ParentItem = parent_item;
                        parent_item.InsertMemberCell(cell);
                        continue;
                    }
                }

                issue.GetBlankDoubleIndex(nCol, parent_item);

                {
                    Cell cell = new Cell();
                    cell.item = null;   // ֻ��ռ��λ��
                    cell.ParentItem = parent_item;
                    issue.SetCell(nCol, cell);

                    // ���ں���λ��
                    // parent_item.MemberCells.Add(cell);

                    parent_item.InsertMemberCell(cell);
                }
            }

            // 
        }
#endif

        // �����ض���Issue����
        IssueBindingItem FindIssue(string strPublishTime)
        {
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                if (issue.PublishTime == strPublishTime)
                    return issue;
            }
            return null;
        }

        // ����һ���µ��ڶ���
        // parameters:
        //      strVolume   �ϳɵ��ַ�������ʾ���ڲ�
        IssueBindingItem NewIssue(string strPublishTime,
            string strVolumeString)
        {
            Debug.Assert(String.IsNullOrEmpty(strPublishTime) == false, "");

            string strIssue = "";
            string strZong = "";
            string strOneVolume = "";

            // ���������ںš����ںš���ŵ��ַ���
            VolumeInfo.ParseItemVolumeString(strVolumeString,
                out strIssue,
                out strZong,
                out strOneVolume);

            IssueBindingItem new_issue = new IssueBindingItem();
            new_issue.Container = this;
            {
                string strError = "";
                int nRet = new_issue.Initial("<root />",
                    false,
                    out strError);
                Debug.Assert(nRet != -1, "");
            }
            new_issue.PublishTime = strPublishTime;
            new_issue.Issue = strIssue;
            new_issue.Volume = strOneVolume;
            new_issue.Zong = strZong;

            int nFreeIndex = -1;
            int nInsertIndex = -1;
            string strLastPublishTime = "";
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];

                // ����������
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                {
                    nFreeIndex = i;
                    continue;
                }

                if (String.Compare(strPublishTime, strLastPublishTime) >= 0
                    && String.Compare(strPublishTime, issue.PublishTime) < 0)
                    nInsertIndex = i;

                strLastPublishTime = issue.PublishTime;
            }

            if (nInsertIndex == -1)
            {
                if (nFreeIndex == -1)
                    this.Issues.Add(new_issue);
                else
                    this.Issues.Insert(nFreeIndex, new_issue);    // 2010/3/30
            }
            else
                this.Issues.Insert(nInsertIndex, new_issue);

            return new_issue;
        }

#if OLD_INITIAL
        // *** Ϊ��ʼ������
        // �������Issue��Items�е�refid�ַ�����ע�⣬����Issue��MemberCells�е�
        // ע�⣬Issue��Items��Ϊ�˳�ʼ����;�ģ���AppendIssue()���ú�߱�����ʼ����ɺ󣬼������
        public List<string> AllIssueMembersRefIds
        {
            get
            {
                List<string> results = new List<string>();
                for (int i = 0; i < this.Issues.Count; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    if (issue == null)
                    {
                        Debug.Assert(false, "");
                        continue;
                    }
                    for (int j = 0; j < issue.Items.Count; j++)
                    {
                        ItemBindingItem item = issue.Items[j];
                        if (item == null)
                        {
                            Debug.Assert(item != null, "");
                            continue;
                        }
                        string strRefID = item.RefID;
                        if (String.IsNullOrEmpty(strRefID) == true)
                            continue;

#if DEBUG
                        if (results.IndexOf(strRefID) != -1)
                        {
                            Debug.Assert(false, "�������ظ���refidֵ '"+strRefID+"'");
                        }
#endif
                        results.Add(strRefID);
                    }
                }
                return results;
            }
        }
#endif

        int CreateParentItems(out string strError)
        {
            strError = "";
            for (int i = 0; i < this.InitialItems.Count; i++)
            {
                ItemBindingItem item = this.InitialItems[i];
                string strPublishTime = item.PublishTime;
                if (strPublishTime.IndexOf("-") != -1)
                {
                    item.Container = null;  // ��ʱ������ĳ����
                    this.ParentItems.Add(item);
                    item.IsParent = true;

                    this.InitialItems.RemoveAt(i);
                    i--;
                }

                // 2010/3/30
                // ��������£�û�г���ʱ�䷶Χ������volumstring����Ϊ����
                if (String.IsNullOrEmpty(strPublishTime) == true
                    && string.IsNullOrEmpty(item.Volume) == false)
                {
                    List<VolumeInfo> infos = null;
                    int nRet = VolumeInfo.BuildVolumeInfos(item.Volume,
                        out infos,
                        out strError);
                    if (nRet != -1)
                    {
                        if (infos.Count > 1)
                        {
                            item.Container = null;  // ��ʱ������ĳ����
                            this.ParentItems.Add(item);
                            item.IsParent = true;

                            this.InitialItems.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            return 0;
        }

        int CreateInitialItems(List<string> ItemXmls,
            out string strError)
        {
            strError = "";

            for (int i = 0; i < ItemXmls.Count; i++)
            {
                string strXml = ItemXmls[i];
                ItemBindingItem item = new ItemBindingItem();
                int nRet = item.Initial(strXml, out strError);
                if (nRet == -1)
                {
                    strError = "CreateInitialItems() error, xmlrecord index = " + i.ToString() + " : " + strError;
                    return -1;
                }

                item.Container = null;  // ��ʱ�������κ���
                this.InitialItems.Add(item);
            }

            return 0;
        }

        int CreateIssues(List<string> IssueXmls,
            out string strError)
        {
            strError = "";

            for (int i = 0; i < IssueXmls.Count; i++)
            {
                string strXml = IssueXmls[i];

                IssueBindingItem issue = new IssueBindingItem();

                issue.Container = this;
                int nRet = issue.Initial(strXml,
                    false,
                    out strError);
                if (nRet == -1)
                {
                    strError = "CreateIssues() error, xmlrecord index = "+i.ToString()+" : " + strError;
                    return -1;
                }

                this.Issues.Add(issue);

                nRet = issue.InitialLoadItems(issue.PublishTime,
                    out strError);
                if (nRet == -1)
                {
                    strError = "CreateIssues() InitialLoadItems() error: " + strError;
                    return -1;
                }
            }

            // ����publishtime����
            this.Issues.Sort(new IssuePublishTimeComparer());

            // ����Ƿ����ظ��ĳ�������
            // 2010/3/21 
            string strPrevPublishTime = "";
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;
                if (issue.PublishTime == strPrevPublishTime)
                {
                    strError = "�������ظ��������� '"+issue.PublishTime+"' �Ķ���ڼ�¼";
                    return -1;
                }

                strPrevPublishTime = issue.PublishTime;
            }

            if (this.Issues.Count > 0)
            {
                // �������ڷ������
                if (String.IsNullOrEmpty(this.Issues[0].PublishTime) == true)
                {
                    IssueBindingItem free_issue = this.Issues[0];
                    this.Issues.RemoveAt(0);
                    this.Issues.Add(free_issue);
                }
            }

            this.m_lContentHeight = this.m_nCellHeight * this.Issues.Count;
            return 0;
        }

#if ODERDESIGN_CONTROL
        // ��������¼װ�ص�OrderDesignControl��
        // return:
        //      -1  error
        //      >=0 �������ܷ���
        static int LoadOrderDesignItems(List<string> XmlRecords,
            OrderDesignControl control,
            out string strError)
        {
            strError = "";

            control.DisableUpdate();

            try
            {

                control.Clear();

                int nOrderedCount = 0;  // ˳�������������ܷ���
                for (int i = 0; i < XmlRecords.Count; i++)
                {
                    DigitalPlatform.CommonControl.Item item =
                        control.AppendNewItem(XmlRecords[i],
                        out strError);
                    if (item == null)
                        return -1;

                    nOrderedCount += item.OldCopyValue;
                }

                control.Changed = false;
                return nOrderedCount;

            }
            finally
            {
                control.EnableUpdate();
            }
        }

        // �����ұߵ�OrderDesignControl���ݹ���XML��¼
        static int BuildOrderXmlRecords(
            OrderDesignControl control,
            out List<string> XmlRecords,
            out string strError)
        {
            strError = "";
            XmlRecords = new List<string>();

            for (int i = 0; i < control.Items.Count; i++)
            {
                DigitalPlatform.CommonControl.Item design_item = control.Items[i];

                string strXml = "";
                int nRet = design_item.BuildXml(out strXml, out strError);
                if (nRet == -1)
                    return -1;

                XmlDocument dom = new XmlDocument();
                dom.LoadXml(strXml);
                XmlRecords.Add(dom.DocumentElement.OuterXml);   // ��Ҫ����prolog
            }

            return 0;
        }

        // ��order�ؼ��е���Ϣ�޸Ķ��ֵ�IssueBindingItem������
        // return:
        //      -1  error
        //      0   ����Ҫ����
        //      1   ����
        //      2   �����Ѿ����֣���������Ϣ�����˽�һ���޸�(���紴���˲ᣬ��Ҫ���·�ӳ���ɹ��ؼ���)
        public int GetFromOrderControl(
            OrderDesignControl order_control,
            IssueBindingItem issue,
            out string strError)
        {
            strError = "";

            if (order_control.Changed == false)
            {
                return 0;
            }

            if (issue == null)
            {
                Debug.Assert(false, "");
                return 0;
            }


            // �������뿪������޸Ĺ����ұ������

            // ɾ��orderInfoԪ���µ�ȫ��Ԫ��
            XmlNodeList nodes = issue.dom.DocumentElement.SelectNodes("orderInfo/*");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                node.ParentNode.RemoveChild(node);
            }

            List<string> XmlRecords = null;
            // �����ұߵ�OrderDesignControl���ݹ���XML��¼
            int nRet = BuildOrderXmlRecords(
                order_control,
                out XmlRecords,
                out strError);
            if (nRet == -1)
                return -1;

            XmlNode root = issue.dom.DocumentElement.SelectSingleNode("orderInfo");
            if (root == null)
            {
                root = issue.dom.CreateElement("orderInfo");
                issue.dom.DocumentElement.AppendChild(root);
            }
            for (int i = 0; i < XmlRecords.Count; i++)
            {
                XmlDocumentFragment fragment = issue.dom.CreateDocumentFragment();
                try
                {
                    fragment.InnerXml = XmlRecords[i];
                }
                catch (Exception ex)
                {
                    strError = "fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                    return -1;
                }

                root.AppendChild(fragment);
                // this.Changed = true;
            }

            issue.Changed = true;

            bool bItemCreated = false;
            List<IssueBindingItem> issues = new List<IssueBindingItem>();
            issues.Add(issue);
            // �����������ݣ��Զ������µĲ�
            // return:
            //      -1  error
            //      0   û�д�����
            //      1   �����˲�
            nRet = CreateNewItems(issues,
                GetAcceptingBatchNo(),
                this.SetProcessingState,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 1)
                bItemCreated = true;

            // item.SetNodeCaption(tree_node); // ˢ�½ڵ���ʾ

            order_control.Changed = false;

            if (bItemCreated == true)
                return 2;

            return 1;
        }

        // �����������ݣ��Զ������µĲ�
        // return:
        //      -1  error
        //      0   û�д�����
        //      1   �����˲�
        int CreateNewItems(List<IssueBindingItem> issueitems,
            string strAcceptBatchNo,    // �������κ�
            bool bSetProcessingState,   // �Ƿ�Ϊ״̬���롰�ӹ��С�
            out string strError)
        {
            strError = "";
            int nRet = 0;

            List<CellBase> new_cells = new List<CellBase>();
            for (int i = 0; i < issueitems.Count; i++)
            {
                IssueBindingItem issue_item = issueitems[i];

                if (String.IsNullOrEmpty(issue_item.OrderInfo) == true)
                    continue;

                bool bOrderChanged = false;

                // ���һ������ÿ��������¼��ѭ��
                XmlNodeList order_nodes = issue_item.dom.DocumentElement.SelectNodes("orderInfo/*");
                for (int j = 0; j < order_nodes.Count; j++)
                {
                    XmlNode order_node = order_nodes[j];

                    string strDistribute = DomUtil.GetElementText(order_node, "distribute");

                    LocationColletion locations = new LocationColletion();
                    nRet = locations.Build(strDistribute,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    bool bLocationChanged = false;

                    // Ϊÿ���ݲصص㴴��һ��ʵ���¼
                    for (int k = 0; k < locations.Count; k++)
                    {
                        Location location = locations[k];

                        // TODO: Ҫע�����㣺1) �Ѿ����չ����У��������*��refid���Ƿ�Ҫ�ٴδ����᣿����Ч����ʶ�������õ�ʱ���кô�
                        // 2) û���������ʱ���ǲ���Ҫ������������ѭ���ˣ����һ��

                        // �Ѿ����������������
                        if (location.RefID != "*")
                            continue;

                        GenerateEntityData e = new GenerateEntityData();

                        location.RefID = Guid.NewGuid().ToString();   // �޸ĵ��ݲصص��ַ�����

                        bLocationChanged = true;

                        XmlDocument dom = new XmlDocument();
                        dom.LoadXml("<root />");

                        // 2009/10/19 
                        // ״̬
                        if (bSetProcessingState == true)
                        {
                            // �������ӹ��С�ֵ
                            string strOldState = DomUtil.GetElementText(dom.DocumentElement,
                                "state");
                            DomUtil.SetElementText(dom.DocumentElement,
                                "state", Global.AddStateProcessing(strOldState));
                        }

                        // seller
                        string strSeller = DomUtil.GetElementText(order_node,
                            "seller");

                        // seller���ǵ���ֵ
                        DomUtil.SetElementText(dom.DocumentElement,
                            "seller", strSeller);

                        string strOldValue = "";
                        string strNewValue = "";

                        // source
                        string strSource = DomUtil.GetElementText(order_node,
                            "source");

                        // source�ڲ�����ֵ
                        // ���� "old[new]" �ڵ�����ֵ
                        OrderDesignControl.ParseOldNewValue(strSource,
                            out strOldValue,
                            out strNewValue);
                        DomUtil.SetElementText(dom.DocumentElement,
                            "source", strNewValue);

                        // price
                        string strPrice = DomUtil.GetElementText(order_node,
                            "price");

                        // price�ڲ�����ֵ
                        OrderDesignControl.ParseOldNewValue(strPrice,
                            out strOldValue,
                            out strNewValue);
                        DomUtil.SetElementText(dom.DocumentElement,
                            "price", strNewValue);

                        // location
                        string strLocation = location.Name;
                        DomUtil.SetElementText(dom.DocumentElement,
                            "location", strLocation);

                        // publishTime
                        DomUtil.SetElementText(dom.DocumentElement,
                            "publishTime", issue_item.PublishTime);

                        // volume ��ʵ�ǵ����ںš����ںš������һ���һ���ַ���
                        string strVolume = IssueManageControl.BuildItemVolumeString(issue_item.Issue,
                            issue_item.Zong,
                            issue_item.Volume);
                        DomUtil.SetElementText(dom.DocumentElement,
                            "volume", strVolume);

                        // ���κ�
                        DomUtil.SetElementText(dom.DocumentElement,
                            "batchNo", strAcceptBatchNo);

                        ItemBindingItem item = new ItemBindingItem();
                        nRet = item.Initial(dom.OuterXml, out strError);
                        if (nRet == -1)
                            return -1;
                        item.Container = issue_item;
                        PlaceSingleToTail(item);
                        item.Changed = true;
                        item.RefID = Guid.NewGuid().ToString();
                        item.NewCreated = true;
                        item.ContainerCell.Select(SelectAction.On);
                        new_cells.Add(item.ContainerCell);
                    }

                    // �ݲصص��ַ����б仯����Ҫ��ӳ������
                    if (bLocationChanged == true)
                    {
                        strDistribute = locations.ToString();
                        DomUtil.SetElementText(order_node,
                            "distribute", strDistribute);
                        bOrderChanged = true;
                        // order_item.RefreshListView();
                    }

                } // end of for j

                if (bOrderChanged == true)
                {
                    issue_item.OrderInfo = DomUtil.GetElementInnerXml(issue_item.dom.DocumentElement,
                        "orderInfo");
                    issue_item.Changed = true;

                    // ˢ��Issue?
                }

            } // end of for i


            if (new_cells.Count > 0)
            {
                // this.SelectObjects(new_cells, SelectAction.On);
                this.AfterWidthChanged(true);
                return 1;
            }


            return 0;
        }

                // ��������Ϣ��ʼ���ɹ��ؼ�
        // return:
        //      -1  ����
        //      0   û���ҵ���Ӧ�Ĳɹ���Ϣ
        //      1   �ҵ��ɹ���Ϣ
        public int InitialOrderControl(
            IssueBindingItem issue,
            OrderDesignControl order_control,
            out string strOrderInfoMessage,
            out string strError)
        {
            strError = "";
            strOrderInfoMessage = "";

            List<string> XmlRecords = new List<string>();
            XmlNodeList nodes = issue.dom.DocumentElement.SelectNodes("orderInfo/*");

            if (nodes.Count > 0)
            {
                // XML�����Ѿ��߱�
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlRecords.Add(nodes[i].OuterXml);
                }
            }
            else if (this.GetOrderInfo != null)
            {
                // ��Ҫ���ⲿ��òɹ���Ϣ
                GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
                e1.BiblioRecPath = "";
                e1.PublishTime = issue.PublishTime;
                this.GetOrderInfo(this, e1);
                if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
                {
                    strError = "�ڻ�ȡ�����ڳ�������Ϊ '" + issue.PublishTime + "' �Ķ�����Ϣ�Ĺ����з�������: " + e1.ErrorInfo;
                    return -1;
                }

                XmlRecords = e1.OrderXmls;

                if (XmlRecords.Count == 0)
                {
                    strOrderInfoMessage = "�������� '" + issue.PublishTime + "' û�ж�Ӧ�ĵĶ�����Ϣ";
                    // EanbleOrderDesignControl(false);

                    // issue.OrderedCount = -1;
                    return 0;
                }
            }

            strOrderInfoMessage = "";
            // EanbleOrderDesignControl(true);

            // return:
            //      -1  error
            //      >=0 �������ܷ���
            int nRet = LoadOrderDesignItems(XmlRecords,
                order_control,
                out strError);
            if (nRet == -1)
                return -1;

            // issue.OrderedCount = nRet;

            return 1;
        }

#endif

        // �Ƿ��Ѿ��ҽӵ�GetOrderInfo�¼�
        public bool HasGetOrderInfo()
        {
            if (this.GetOrderInfo == null)
                return false;

            return true;
        }

        // ǿ���û�ȡ������Ϣ�޶��ڵ�ǰ�û���Ͻ��Χ��
        bool m_bForceNarrowRange = false;

        public void DoGetOrderInfo(object sender, GetOrderInfoEventArgs e)
        {
            if (this.GetOrderInfo != null)
            {
                if (m_bForceNarrowRange == true)
                    e.LibraryCodeList = this.LibraryCodeList;

                this.GetOrderInfo(this, e);
            }
            else
            {
                Debug.Assert(false, "");
            }
        }



        // ����β״̬����ȫ����
        public int Check(out string strError)
        {
            strError = "";

            // 1) �����ڶ����refID��Ӧ�ǿգ����Ҳ��ظ�
            // publishtime���ظ�
            List<string> issue_refids = new List<string>();
            List<string> issue_publishtimes = new List<string>();
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                Debug.Assert(issue != null, "");

                // ����������
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;

                if (String.IsNullOrEmpty(issue.RefID) == true
                    && issue.Virtual == false)
                {
                    if (String.IsNullOrEmpty(strError) == false)
                        strError += ";";
                    strError += "�� '" + issue.Caption + "' �Ĳο�IDֵΪ��";
                    continue;
                }

                // ��refid����
                if (string.IsNullOrEmpty(issue.RefID) == false  // ��Ҫ�ǿղŲ���
                    && issue_refids.IndexOf(issue.RefID) != -1)
                {
                    if (String.IsNullOrEmpty(strError) == false)
                        strError += ";";
                    strError += "�� '" + issue.Caption + "' ��refIDֵ '"+issue.RefID+"' �������ڷ������ظ�";
                }

                issue_refids.Add(issue.RefID);

                // ��publishtime����
                if (issue_publishtimes.IndexOf(issue.PublishTime) != -1)
                {
                    if (String.IsNullOrEmpty(strError) == false)
                        strError += ";";
                    strError += "�� '" + issue.Caption + "' �ĳ���ʱ��ֵ '" + issue.PublishTime + "' �������ڷ������ظ�";
                }

                issue_publishtimes.Add(issue.PublishTime);
            }

            if (String.IsNullOrEmpty(strError) == false)
                return -1;

            return 0;
        }



        // ȷ����ӽ���ǰ���ڵ��ڸ�����ʾ��������
        public IssueBindingItem EnsureCurrentIssueVisible()
        {
            // Ѱ�Һ͵�ǰ������ӽ����ڸ���
            DateTime now = DateTime.Now;
            TimeSpan min_delta = new TimeSpan(0);
            IssueBindingItem nearest_issue = null;
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];

                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;

                string strTime = "";
                try
                {
                    strTime = DateTimeUtil.ForcePublishTime8(issue.PublishTime);
                }
                catch
                {
                    continue;
                }

                DateTime time = DateTimeUtil.Long8ToDateTime(strTime);
                TimeSpan delta;
                if (time > now)
                    delta = time - now;
                else
                    delta = now - time;

                if (nearest_issue == null)
                {
                    nearest_issue = issue;
                    min_delta = delta;
                    continue;
                }

                if (min_delta > delta)
                {
                    min_delta = delta;
                    nearest_issue = issue;
                }
            }

            if (nearest_issue != null)
            {
                this.EnsureVisible(nearest_issue);
                if (m_lastFocusObj == null)
                {
                    // �Զ��ѵ�һ��������Ϊ����

                    m_lastFocusObj = nearest_issue.GetFirstCell();
                    SetObjectFocus(m_lastFocusObj);
                }
                return nearest_issue;
            }

            return null;
        }

        // �þ�����Ϣ��Ѱ�ڶ���
        int SearchIssue(
            VolumeInfo info,
            out List<IssueBindingItem> issues,
            out string strError)
        {
            strError = "";
            issues = new List<IssueBindingItem>();

            if (String.IsNullOrEmpty(info.Year) == true)
            {
                strError = "info.Year����Ϊ��";
                return -1;
            }

            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];

                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;

                string strYearPart = IssueUtil.GetYearPart(issue.PublishTime);

                // ������ʱ��
                if (strYearPart != info.Year)
                    continue;

                if (info.IssueNo == issue.Issue)
                    issues.Add(issue);
            }

            // �������һ�����������ں�������
            if (issues.Count > 1 && String.IsNullOrEmpty(info.Zong) == false)
            {
                List<IssueBindingItem> temp = new List<IssueBindingItem>();
                for (int i = 0; i < issues.Count; i++)
                {
                    IssueBindingItem issue = issues[i];
                    if (issue.Zong == info.Zong)
                        temp.Add(issue);
                }

                if (temp.Count == 0)
                {
                }
                else if (temp.Count < issues.Count)
                    issues = temp;
            }

            // �������һ�������þ��������
            if (issues.Count > 1 && String.IsNullOrEmpty(info.Volumn) == false)
            {
                List<IssueBindingItem> temp = new List<IssueBindingItem>();
                for (int i = 0; i < issues.Count; i++)
                {
                    IssueBindingItem issue = issues[i];
                    if (issue.Volume == info.Volumn)
                        temp.Add(issue);
                }

                if (temp.Count == 0)
                {
                }
                else if (temp.Count < issues.Count)
                    issues = temp;
            }

            return 0;
        }

        // TODO: �����������Ѿ����ڴ��е����ֳ�ʼ��
        // ��ʼ����һ���Գ�ʼ����������Ҫ��������
        // parameters:
        //      strLayoutMode   "auto" "accepting" "binding"��autoΪ�Զ�ģʽ��acceptingΪȫ����Ϊ�ǵ���bindingΪȫ����Ϊװ��
        // return:
        //      -1  ����
        //      0   �ɹ�
        //      1   �ɹ������о��档������Ϣ��strError��
        public int NewInitial(
            string strLayoutMode,
            List<string> ItemXmls,
            List<string> IssueXmls,
            out string strError)
        {
            strError = "";
            string strWarning = "";
            int nRet = 0;

            if (strLayoutMode == "auto"
                || strLayoutMode == "accepting"
                || strLayoutMode == "binding")
            {
                this.WholeLayout = strLayoutMode;
            }
            else
            {
                strError = "δ֪�Ĳ���ģʽ '"+strLayoutMode+"'";
                return -1;
            }

            // �״�����״̬��һ�����Ϊ���ر༭�ؼ�
            if (this.CellFocusChanged != null)
            {
                FocusChangedEventArgs e = new FocusChangedEventArgs();
                this.CellFocusChanged(this, e);
            }

            Hashtable placed_table = new Hashtable();   // �Ѿ�����Ϊ�϶����������Ź�λ�õĲ����

            // �������ڶ����Cells�������
            this.FreeIssue = null;
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                issue.Cells.Clear();

                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    this.FreeIssue = issue;
            }

            // ���û�У��򴴽�������
            if (this.FreeIssue == null)
            {
                this.FreeIssue = new IssueBindingItem();
                this.FreeIssue.Container = this;
                this.Issues.Add(this.FreeIssue);
            }

            // ����this.InitalItems
            nRet = CreateInitialItems(ItemXmls,
                out strError);
            if (nRet == -1)
                return -1;


            // ����this.Issues
            nRet = CreateIssues(IssueXmls,
                out strError);
            if (nRet == -1)
                return -1;

            // ����this.ParentItems
            nRet = CreateParentItems(out strError);
            if (nRet == -1)
                return -1;

            // ʣ�µľ����޹����ĵ�����

            // ����û���ڹ����ĵ�����󣬽����ǹ������ʵ���Issue�����Items��Ա��
            // ����virtual issues��ʱ�Ѿ������ˣ�����Ŵ����϶��ᣬ��˳�ʼ���϶�����Χ����(?)���ֶ���
            // ע��������Ȼû�п���bindingxml�п��ܻ���ֵ�virtual�������ڡ���ʱ�п��ܻ���ֶ���
            for (int i = 0; i < this.InitialItems.Count; i++)
            {
                ItemBindingItem item = this.InitialItems[i];
                Debug.Assert(item != null, "");
                Debug.Assert(item.Container == null, "");


                IssueBindingItem issue = this.FindIssue(item.PublishTime);
                if (issue != null)
                {
                    // ע���������publishtimeΪ�յģ����ü��뵽������
                    issue.Items.Add(item);
                    item.Container = issue;
                }
                else
                {
                    Debug.Assert(String.IsNullOrEmpty(item.PublishTime) == false, "");

                    issue = this.NewIssue(item.PublishTime,
                        item.Volume);
                    Debug.Assert(issue != null, "");
                    issue.Virtual = true;
                    issue.Items.Add(item);
                    item.Container = issue;
                }
            }
            this.InitialItems.Clear();

            List<PublishTimeAndVolume> publishtimes = new List<PublishTimeAndVolume>();
            // ͳ��(bindingxml��)ȫ���϶���Ա����ʹ�ù���publishtime�ַ���
            nRet = GetAllBindingXmlPublishTimes(
                out publishtimes,
                out strError);
            if (nRet == -1)
                return -1;
            for (int i = 0; i < publishtimes.Count; i++)
            {
                PublishTimeAndVolume item = publishtimes[i];
                string strPublishTime = item.PublishTime;
                Debug.Assert(String.IsNullOrEmpty(strPublishTime) == false);

                IssueBindingItem issue = this.FindIssue(strPublishTime);
                if (issue == null)
                {
                    issue = this.NewIssue(strPublishTime,
                        item.Volume);
                    Debug.Assert(issue != null, "");
                    issue.Virtual = true;
                }
            }

            /*
            // ����
            while (this.ParentItems.Count > 3)
                this.ParentItems.RemoveAt(3);
             * */

            //// ////
            // parent_item --> member_items
            Hashtable memberitems_table = null;

            // �����϶���������飬������Ա��������
            nRet = CreateMemberItemTable(
                ref this.ParentItems,
                out memberitems_table,
                ref strWarning,
                out strError);
            if (nRet == -1)
                return -1;

            // �ѳ�����Ͻ��Χ�ĺ϶��ᵥԪȥ��
            if (this.m_bHideLockedBindingCell == true
                && Global.IsGlobalUser(this.LibraryCodeList) == false)
            {

#if NO
                for (int i = 0; i < this.ParentItems.Count; i++)
                {
                    ItemBindingItem parent_item = this.ParentItems[i];
                    List<ItemBindingItem> member_items = (List<ItemBindingItem>)memberitems_table[parent_item];

                    Debug.Assert(member_items != null, "");

                    if (member_items.Count == 0)
                        continue;

                    // ���һ���϶�������г�Ա,�����ǲ���(����һ��)�͵�ǰ�ɼ��������д�����ϵ?
                    // return:
                    //      -1  ����
                    //      0   û�н���
                    //      1   �н���
                    nRet = IsMemberCrossOrderGroup(parent_item,
                        member_items,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    string strLibraryCode = Global.GetLibraryCode(parent_item.LocationString);

                    bool bLocked = (StringUtil.IsInList(strLibraryCode, this.LibraryCodeList) == false);
                    parent_item.Locked = bLocked;

                    // �϶�������ݴ������⣬�������ԱҲ���Ϳɼ������齻��ģ�ɾ���϶�������
                    if (bLocked == true
                        && nRet == 0)
                    {
                        // this.RemoveItem(parent_item, false);

                        // ��ʱ��δ����Issue��������
                        this.m_hideitems.Add(parent_item);

                        this.ParentItems.RemoveAt(i);
                        i--;
                    }
                }

#endif
                // �ѵ�ǰ������Ͻ��Χ�ĺ϶��ᵥԪȥ��
                nRet = RemoveOutofBindingItems(
                    ref this.ParentItems,
                    memberitems_table,
                    false,
                    false,
                    out strError);
                if (nRet == -1)
                    return -1;

                // ���������еĳ�����Χ�ĵ�Ԫȥ��
                if (this.FreeIssue != null)
                {
                    foreach (Cell cell in this.FreeIssue.Cells)
                    {
                        if (cell == null || cell.item == null)
                            continue;
                        string strLibraryCode = Global.GetLibraryCode(cell.item.LocationString);
                        if (StringUtil.IsInList(strLibraryCode, this.LibraryCodeList) == false)
                        {
                            this.FreeIssue.Cells.Remove(cell);
                            if (cell.item != null)
                                this.m_hideitems.Add(cell.item);
                        }
                    }
                }

            }

            // ���ź϶���Ա�����
            nRet = PlaceMemberCell(
                ref this.ParentItems,
                memberitems_table,
                ref placed_table,
                out strError);
            if (nRet == -1)
                return -1;

            // �����������󡣼��Ǻ϶���Ա��
            for (int i = 0; i < this.Issues.Count; i++)
            {
                // �ȴ洢������
                List<ItemBindingItem> items = new List<ItemBindingItem>();

                IssueBindingItem issue = this.Issues[i];
                for (int j = 0; j < issue.Items.Count; j++)
                {
                    ItemBindingItem item = issue.Items[j];

                    if (placed_table[item] != null)
                        continue;

                    items.Add(item);
                }
                issue.Items.Clear();    // ��ʼ��ʹ������Ժ��������

                if (items.Count > 0)
                {
                    if (String.IsNullOrEmpty(issue.PublishTime) == false)
                    {
                        // ����Intact����
                        items.Sort(new ItemIntactComparer());
                    }

                    for (int j = 0; j < items.Count; j++)
                    {
                        ItemBindingItem item = items[j];
                        // ����������һ����ұ�λ��
                        PlaceSingleToTail(item);
                    }
                }

                if (String.IsNullOrEmpty(issue.PublishTime) == false)
                {
                    // �״�����ÿ�����ӵ�OutputIssueֵ
                    issue.RefreshAllOutofIssueValue();
                }
            }

            // ��Cell��Missing״̬��Itemȫ������Ϊnull
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                for (int j = 0; j < issue.Cells.Count; j++)
                {
                    Cell cell = issue.Cells[j];
                    if (cell != null && cell.item != null)
                    {
                        if (cell.item.Missing == true)
                            cell.item = null;
                    }
                }
            }

#if NO
            // �ѳ�����Ͻ��Χ�ĺ϶��ᵥԪȥ��
            if (this.HideLockedBindingCell == true
                && Global.IsGlobalUser(this.LibraryCodeList) == false)
            {
                for (int i = 0; i < this.ParentItems.Count; i++)
                {
                    ItemBindingItem parent_item = this.ParentItems[i];

                    // ���һ���϶�������г�Ա,�����ǲ���(����һ��)�͵�ǰ�ɼ��������д�����ϵ?
                    // return:
                    //      -1  ����
                    //      0   û�н���
                    //      1   �н���
                    nRet = IsMemberCrossOrderGroup(parent_item,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    string strLibraryCode = Global.GetLibraryCode(parent_item.LocationString);
                    // �϶�������ݴ������⣬�������ԱҲ���Ϳɼ������齻��ģ�ɾ���϶�������
                    if (StringUtil.IsInList(strLibraryCode, this.LibraryCodeList) == false
                        && nRet == 0)
                    {
                        this.RemoveItem(parent_item, true);
                        this.ParentItems.RemoveAt(i);
                        i--;
                    }
                }

                // ���������еĳ�����Χ�ĵ�Ԫȥ��
                if (this.FreeIssue != null)
                {
                    foreach (Cell cell in this.FreeIssue.Cells)
                    {
                        if (cell == null || cell.item == null)
                            continue;
                        string strLibraryCode = Global.GetLibraryCode(cell.item.LocationString);
                        if (StringUtil.IsInList(strLibraryCode, this.LibraryCodeList) == false)
                        {
                            this.FreeIssue.Cells.Remove(cell);
                        }
                    }
                }
            // TODO: ��Ҫ����placement
            }
#endif

            // ���üǵ�����ģʽ

            if (strLayoutMode == "auto")
            {
                for (int i = 0; i < this.Issues.Count; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    // ����������
                    if (String.IsNullOrEmpty(issue.PublishTime) == true)
                        continue;
                    if (issue.HasMemberOrParentCell() == true)
                    {
                        issue.IssueLayoutState = IssueLayoutState.Binding;
                        nRet = issue.InitialLayoutBinding(out strError);
                        if (nRet == -1)
                            return -1;
                    }
                    else
                    {
                        issue.IssueLayoutState = IssueLayoutState.Accepting;
                        nRet = issue.LayoutAccepting(out strError);
                        if (nRet == -1)
                            return -1;
                    }
                }
            }
            else if (strLayoutMode == "accepting")
            {
                for (int i = 0; i < this.Issues.Count; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    // ����������
                    if (String.IsNullOrEmpty(issue.PublishTime) == true)
                        continue;
                    {
                        issue.IssueLayoutState = IssueLayoutState.Accepting;
                        nRet = issue.LayoutAccepting(out strError);
                        if (nRet == -1)
                            return -1;
                    }
                }
            }
            else
            {
                Debug.Assert(strLayoutMode == "binding", "");
                // ��������ȫ�����Ѿ�Ϊװ��ģʽ
                for (int i = 0; i < this.Issues.Count; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    // ����������
                    if (String.IsNullOrEmpty(issue.PublishTime) == true)
                        continue;
                    {
                        issue.IssueLayoutState = IssueLayoutState.Binding;
                        nRet = issue.InitialLayoutBinding(out strError);
                        if (nRet == -1)
                            return -1;
                    }
                }
            }

#if DEBUG
            this.VerifyAll();
#endif


            AfterWidthChanged(true);
            // Debug.WriteLine("NewInitial() AfterWidthChanged() done");

            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strError = strWarning;
                return 1;
            }

            return 0;
        }

        // 2012/9/29
        // �����϶���������飬������Ա��������
        // parameters:
        //      parent_items    �϶���������顣�����Ķ������������������
        int CreateMemberItemTable(
            ref List<ItemBindingItem> parent_items,
            out Hashtable memberitems_table,
            ref string strWarning,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            // parent_item --> member_items
            memberitems_table = new Hashtable();

            // �����϶���������飬������Ա��������
            for (int i = 0; i < parent_items.Count; i++)
            {
                ItemBindingItem parent_item = parent_items[i];
                List<ItemBindingItem> member_items = new List<ItemBindingItem>();

                string strBindingXml = parent_item.Binding;
                if (String.IsNullOrEmpty(strBindingXml) == true)
                {
                    string strVolume = parent_item.Volume;

                    if (String.IsNullOrEmpty(strVolume) == true)
                    {
                        // ���û��BindingXml������û�о��ڷ�Χ�����ƶ�����������
                        parent_items.Remove(parent_item);
                        Cell temp = new Cell();
                        temp.item = parent_item;
                        AddToFreeIssue(temp);
                        i--;
                        continue;
                    }

                    List<VolumeInfo> infos = null;
                    nRet = VolumeInfo.BuildVolumeInfos(strVolume,
                        out infos,
                        out strError);
                    if (nRet == -1 || infos.Count == 0) // 2015/5/8
                    {
                        parent_item.Comment += "\r\n���������ַ�����ʱ��������: " + strError;
                        parent_items.Remove(parent_item);
                        Cell temp = new Cell();
                        temp.item = parent_item;
                        AddToFreeIssue(temp);
                        i--;
                        continue;
                    }


                    bool bFailed = false;
                    string strFailMessage = "";
                    for (int j = 0; j < infos.Count; j++)
                    {
                        VolumeInfo info = infos[j];

                        ItemBindingItem sub_item = null;

                        // TODO: ��������refidΪ*�Ķ�����Ϣ�ڵĶ���

                        // ͨ��������ϢѰ�Һ��ʵĿ����������ڶ���
                        List<IssueBindingItem> issues = null;
                        // �þ�����Ϣ��Ѱ�ڶ���
                        nRet = SearchIssue(
                            info,
                            out issues,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        if (issues == null || issues.Count == 0)
                        {
                            // ��Ѱʧ�ܣ�
                            // TODO: Ҳ�����ָ������ԭ��?
                            strFailMessage = "�ں�(��:" + info.Year + ") '" + info.IssueNo + "' û���ҵ��ڶ���";
                            bFailed = true;
                            break;
                        }

                        string strPublishTime = issues[0].PublishTime;
                        Debug.Assert(String.IsNullOrEmpty(strPublishTime) == false, "");

                        if (sub_item == null)
                        {
                            // ���û���ֳɵ�item������ͨ��<item>Ԫ�ص��������������
                            sub_item = new ItemBindingItem();
                            nRet = sub_item.Initial("<root />", out strError);
                            Debug.Assert(nRet != -1, "");

                            sub_item.Volume = info.GetString();
                            sub_item.PublishTime = strPublishTime;
                            sub_item.RefID = "*";   // �����Եģ�����������������Ķ���������
                        }

                        sub_item.ParentItem = parent_item;
                        sub_item.Deleted = true;
                        sub_item.State = "��ɾ��";
                        member_items.Add(sub_item);
                    }

                    if (bFailed == true)
                    {
                        parent_item.Comment += "\r\n�����ַ����еĲ����ڲ����ڣ��޷���ԭ�϶�״̬: " + strFailMessage;
                        parent_items.Remove(parent_item);
                        Cell temp = new Cell();
                        temp.item = parent_item;
                        AddToFreeIssue(temp);
                        i--;
                        continue;
                    }

                    goto PLACEMEMT;
                }

                // ����refid, �ҵ�����������ЩItemBindingItem����
                XmlDocument dom = new XmlDocument();
                dom.LoadXml("<root />");
                try
                {
                    dom.DocumentElement.InnerXml = strBindingXml;
                }
                catch (Exception ex)
                {
                    strError = "�ο�IDΪ '" + parent_item.RefID + "' �Ĳ���Ϣ�У�<binding>Ԫ����ǶXMLװ��DOMʱ����: " + ex.Message;
                    return -1;
                }

                /*
                 * bindingxml�У�<item>Ԫ��δ����refID���ԡ�
                 * û��refID���ԣ���������һ����ɾ���˲��¼�ĵ�����Ϣ��Ԫ��������ȱ�������
                 * ȱ�ڿ��ܷ�����װ����Χ�ĵ�һ��������һ�ᣬҪ����ע��
                 * */

                parent_item.MemberCells.Clear();
                XmlNodeList nodes = dom.DocumentElement.SelectNodes("item");
                if (nodes.Count == 0)
                {
                    // ��Ȼ��BindingXml����û���κ��¼�<item>Ԫ�أ����ƶ�����������
                    parent_items.Remove(parent_item);
                    Cell temp = new Cell();
                    temp.item = parent_item;
                    AddToFreeIssue(temp);
                    i--;
                    continue;
                }

                for (int j = 0; j < nodes.Count; j++)
                {
                    XmlNode node = nodes[j];
                    string strRefID = DomUtil.GetAttr(node, "refID");

                    bool bItemRecordDeleted = false;    // ���¼�Ƿ�ɾ��?

                    if (String.IsNullOrEmpty(strRefID) == true)
                    {
                        bItemRecordDeleted = true;
                    }

                    ItemBindingItem sub_item = null;

                    if (bItemRecordDeleted == false)
                    {
                        sub_item = InitialFindItemByRefID(strRefID);
                        if (sub_item == null)
                        {
                            // 2012/9/29
                            sub_item = FindItemByRefID(strRefID, this.m_hideitems);
                            if (sub_item != null)
                                this.m_hideitems.Remove(sub_item);
                        }

                        if (sub_item == null)
                        {
                            if (String.IsNullOrEmpty(strWarning) == false)
                                strWarning += "; ";
                            // strWarning += "�ο�IDΪ '" + parent_item.RefID + "' �Ĳ���Ϣ�У�<binding>Ԫ���ڰ����Ĳο�ID '" + strRefID + "' û���ҵ���Ӧ�Ĳ���Ϣ";
                            bItemRecordDeleted = true;
                        }
                    }

                    if (sub_item == null)
                    {
                        // ���û���ֳɵ�item������ͨ��<item>Ԫ�ص��������������
                        sub_item = new ItemBindingItem();
                        nRet = sub_item.Initial("<root />", out strError);
                        Debug.Assert(nRet != -1, "");

                        // TODO: ���԰�node�����е����Զ�����Ϊitem�е�ͬ��Ԫ�أ�������������ĵط����������ֶ���
                        sub_item.Volume = DomUtil.GetAttr(node, "volume");
                        sub_item.PublishTime = DomUtil.GetAttr(node, "publishTime");
                        sub_item.RefID = DomUtil.GetAttr(node, "refID");
                        sub_item.Barcode = DomUtil.GetAttr(node, "barcode");
                        sub_item.RegisterNo = DomUtil.GetAttr(node, "registerNo");

                        // 2011/9/8
                        sub_item.Price = DomUtil.GetAttr(node, "price");

                        bool bMissing = false;
                        // ��ò����͵����Բ���ֵ
                        // return:
                        //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
                        //      0   ���������ȷ����Ĳ���ֵ
                        //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
                        DomUtil.GetBooleanParam(node,
                            "missing",
                            false,
                            out bMissing,
                            out strError);
                        sub_item.Missing = bMissing;

                        if (String.IsNullOrEmpty(sub_item.PublishTime) == true)
                        {
                            // û��publishtime��Ҳ�޷�����
                        }
                    }

                    // sub_item.Binded = true; // ע��place�׶λ����õ�
                    sub_item.ParentItem = parent_item;
                    if (sub_item.Missing == false
                        && bItemRecordDeleted == true)
                    {
                        sub_item.Deleted = bItemRecordDeleted;
                        sub_item.State = "��ɾ��";
                    }

                    member_items.Add(sub_item);

                    // ʹ�ú���Ȼ�ᱻ����
                }

            PLACEMEMT:
                memberitems_table[parent_item] = member_items;
            }

            return 0;
        }

        // 2012/9/29
        // ֻ���ź϶������
        // parameters:
        //      parent_items    �϶���������顣�޷����ŵĺ϶���������������������
        int PlaceParentItems(
            ref List<ItemBindingItem> parent_items,
            Hashtable memberitems_table,
            out string strError)
        {
            strError = "";

            ////
            // ���Ų����
            for (int i = 0; i < parent_items.Count; i++)
            {
                ItemBindingItem parent_item = parent_items[i];

                // 2012/9/28
                if (Global.IsGlobalUser(this.LibraryCodeList) == false)
                {
                    string strLibraryCode = Global.GetLibraryCode(parent_item.LocationString);
                    parent_item.Locked = (StringUtil.IsInList(strLibraryCode, this.LibraryCodeList) == false);
                }

                List<ItemBindingItem> member_items = (List<ItemBindingItem>)memberitems_table[parent_item];

                Debug.Assert(member_items != null, "");

                if (member_items.Count == 0)
                    continue;

                // ����

                // �Ѻ϶����ItemBindingItem���󰲷����������ĵ�һ����������ڵ�������
                ItemBindingItem first_sub = member_items[0];
                IssueBindingItem first_issue = first_sub.Container;

                if (first_issue == null)
                {
                    // Debug.Assert(first_sub.Missing == true, "");

                    if (String.IsNullOrEmpty(first_sub.PublishTime) == true)
                    {
                        // ��Ա��û��publishtime�޷�����
                        // ��͵���parent���ƶ���������
                        parent_items.Remove(parent_item);
                        Cell temp = new Cell();
                        temp.item = parent_item;
                        temp.item.Comment += "\r\n�϶���ĵ�һ����Ա��û�г���ʱ����Ϣ����˺϶���Ҳ�޷��������ţ�ֻ�÷�����������";
                        AddToFreeIssue(temp);
                        i--;
                        continue;
                    }

                    Debug.Assert(String.IsNullOrEmpty(first_sub.PublishTime) == false, "");

                    first_issue = this.FindIssue(first_sub.PublishTime);
                    if (first_issue == null)
                    {
                        first_issue = this.NewIssue(first_sub.PublishTime,
                            first_sub.Volume);
                    }
                }

                Debug.Assert(first_issue != null, "");

                // ����������һ��Ŀ���λ�á�ż��
                int col = -1;
                col = first_issue.GetFirstAvailableBoundColumn();

                // ���ź϶�������
                Cell parent_cell = new Cell();
                parent_cell.item = parent_item;
                parent_item.Container = first_issue; // ��װ���������
                first_issue.SetCell(col, parent_cell);

                // ���������ĵ�����
                try
                {
                    PlaceMemberItems(parent_cell,
                        member_items,
                        col + 1,
                        true);
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }
            }

            return 0;
        }

        // 2012/9/29
        // ���ź϶���Ա�����
        // parameters:
        //      parent_items    �϶���������顣�޷����ŵĺ϶���������������������
        int PlaceMemberCell(
            ref List<ItemBindingItem> parent_items,
            Hashtable memberitems_table,
            ref Hashtable placed_table,
            out string strError)
        {
            strError = "";

            ////
            // ���Ų����
            for (int i = 0; i < parent_items.Count; i++)
            {
                ItemBindingItem parent_item = parent_items[i];

                // 2012/9/28
                if (Global.IsGlobalUser(this.LibraryCodeList) == false)
                {
                    string strLibraryCode = Global.GetLibraryCode(parent_item.LocationString);
                    parent_item.Locked = (StringUtil.IsInList(strLibraryCode, this.LibraryCodeList) == false);
                }

                List<ItemBindingItem> member_items = (List<ItemBindingItem>)memberitems_table[parent_item];

                Debug.Assert(member_items != null, "");

                if (member_items.Count == 0)
                    continue;

                // ����

                // �Ѻ϶����ItemBindingItem���󰲷����������ĵ�һ����������ڵ�������
                ItemBindingItem first_sub = member_items[0];
                IssueBindingItem first_issue = first_sub.Container;

                if (first_issue == null)
                {
                    // Debug.Assert(first_sub.Missing == true, "");

                    if (String.IsNullOrEmpty(first_sub.PublishTime) == true)
                    {
                        // ��Ա��û��publishtime�޷�����
                        // ��͵���parent���ƶ���������
                        parent_items.Remove(parent_item);
                        Cell temp = new Cell();
                        temp.item = parent_item;
                        temp.item.Comment += "\r\n�϶���ĵ�һ����Ա��û�г���ʱ����Ϣ����˺϶���Ҳ�޷��������ţ�ֻ�÷�����������";
                        AddToFreeIssue(temp);
                        i--;
                        continue;
                    }

                    Debug.Assert(String.IsNullOrEmpty(first_sub.PublishTime) == false, "");

                    first_issue = this.FindIssue(first_sub.PublishTime);
                    if (first_issue == null)
                    {
                        first_issue = this.NewIssue(first_sub.PublishTime,
                            first_sub.Volume);
                    }
                }

                Debug.Assert(first_issue != null, "");

                // ����������һ��Ŀ���λ�á�ż��
                int col = -1;
                col = first_issue.GetFirstAvailableBoundColumn();

                // ���ź϶�������
                Cell parent_cell = new Cell();
                parent_cell.item = parent_item;
                parent_item.Container = first_issue; // ��װ���������
                first_issue.SetCell(col, parent_cell);

                // ���������ĵ�����
                try
                {
                    PlaceMemberItems(parent_cell,
                        member_items,
                        col + 1);
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }

                /* // this.Changed���ᱻ�ı䣬��û�����޸ļǺţ����á������ò˵�����ʵ�֣�������Ա��Ҫȥ����
                if (String.IsNullOrEmpty(parent_item.PublishTime) == true)
                {
                    if (parent_item.RefreshPublishTime() == true)
                        parent_item.Changed = true;
                }
                 * */

                // ����
                foreach (ItemBindingItem temp in member_items)
                {
                    placed_table[temp] = temp;
                }

                /*
                // �Ժ�Ű��ź϶�������
                Cell cell = new Cell();
                cell.item = parent_item;
                parent_item.Container = first_issue; // ��װ���������
                first_issue.SetCell(col, cell);
                 * */

#if DEBUG
                {
                    string strError1 = "";
                    int nRet1 = parent_item.VerifyMemberCells(out strError1);
                    if (nRet1 == -1)
                    {
                        Debug.Assert(false, strError1);
                    }
                }
#endif
            }

            return 0;
        }

        // ���һ���϶�������г�Ա,�����ǲ���(����һ��)�͵�ǰ�ɼ��������д�����ϵ?
        // parameters:
        //      bRefreshOrderItem   �Ƿ�ǿ��ˢ�¶�����Ϣ?
        // return:
        //      -1  ����
        //      0   û�н���
        //      1   �н���
        int IsMemberCrossOrderGroup(ItemBindingItem parent_item,
            List<ItemBindingItem> member_items,
            bool bRefreshOrderItem,
            out string strError)
        {
            strError = "";

            List<string> visible_refids = new List<string>();

            // ������пɼ��������е�refid
            foreach (IssueBindingItem issue in this.Issues)
            {
                if (issue == null)
                    continue;

                // ����������
                if (string.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;

                if (bRefreshOrderItem == true)
                {
                    issue.ClearOrderItems();
                }

                int nRet = issue.InitialOrderItems(out strError);
                if (nRet == -1)
                    return -1;

                List<string> refids = null;
                // ��ÿɼ��Ķ������е�refid
                nRet = issue.GetVisibleRefIDs(
                    this.LibraryCodeList,
                    out refids,
                    out strError);
                if (nRet == -1)
                    return -1;

                visible_refids.AddRange(refids);
            }

            foreach (ItemBindingItem item in member_items)
            {
                if (visible_refids.IndexOf(item.RefID) != -1)
                    return 1;
            }

            return 0;
        }

        // �ҵ���ɾ��һ��item��������item�Ǻ϶���������ҲҪɾ���������Ĳ�item
        void RemoveItem(ItemBindingItem item,
            bool bRemoveMemberCell)
        {
            if (bRemoveMemberCell == true)
            {
                foreach (Cell cell in item.MemberCells)
                {
                    if (cell.Container != null)
                    {
                        cell.Container.Cells.Remove(cell);
                        if (cell.item != null)
                            this.m_hideitems.Add(cell.item);
                    }
                }
            }
            item.MemberCells.Clear();

            foreach (IssueBindingItem issue in this.Issues)
            {
                foreach (Cell cell in issue.Cells)
                {
                    if (cell != null && cell.item == item)
                    {
                        issue.Cells.Remove(cell);
                        break;
                    }
                }
            }

            this.m_hideitems.Add(item);
        }

        // �����ȡ����صĲ�����Ϣ
        // parameters:
        //      exclude_item    Ҫ�ų��Ķ���
        public List<CallNumberItem> GetCallNumberItems(ItemBindingItem exclude_item)
        {
            List<ItemBindingItem> all_items = this.AllItems;

            List<CallNumberItem> results = new List<CallNumberItem>();
            foreach (ItemBindingItem cur_item in all_items)
            {
                if (cur_item == exclude_item)
                    continue;

                CallNumberItem item = new CallNumberItem();

                item.RecPath = cur_item.RecPath;
                item.CallNumber = cur_item.AccessNo;
                item.Location = cur_item.LocationString;
                item.Barcode = cur_item.Barcode;

                results.Add(item);
            }

            return results;
        }

#if OLD_INITIAL
        // *** Ϊ��ʼ������
        // ��ʼ����Ӧ��AppendIssue() AppendNoneIssueSingleItems() ��AppendBindItem()�Ժ����
        // return:
        //      -1  ����
        //      0   �ɹ�
        //      1   �ɹ������о��档������Ϣ��strError��
        public int Initial(out string strError)
        {
            strError = "";
            string strWarning = "";
            int nRet = 0;

            // �״�����״̬��һ�����Ϊ���ر༭�ؼ�
            if (this.CellFocusChanged != null)
            {
                FocusChangedEventArgs e = new FocusChangedEventArgs();
                this.CellFocusChanged(this, e);
            }

            Hashtable placed_table = new Hashtable();   // �Ѿ�����Ϊ�϶����������Ź�λ�õĲ����

            // �������ڶ����Cells�������
            this.FreeIssue = null;
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                issue.Cells.Clear();

                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    this.FreeIssue = issue;
            }

            // ���û�У��򴴽�������
            if (this.FreeIssue == null)
            {
                this.FreeIssue = new IssueBindingItem();
                this.FreeIssue.Container = this;
                this.Issues.Add(this.FreeIssue);
            }

            // ����û���ڹ����ĵ�����󣬽����ǹ������ʵ���Issue�����Items��Ա��
            for (int i = 0; i < this.NoneIssueItems.Count; i++)
            {
                ItemBindingItem item = this.NoneIssueItems[i];
                Debug.Assert(item != null, "");
                Debug.Assert(item.Container == null, "");

                IssueBindingItem issue = this.FindIssue(item.PublishTime);
                if (issue != null)
                {
                    issue.Items.Add(item);
                    item.Container = issue;
                }
                else
                {
                    issue = this.NewIssue(item.PublishTime,
                        item.Volume);
                    Debug.Assert(issue != null, "");
                    issue.Virtual = true;
                    issue.Items.Add(item);
                    item.Container = issue;
                }
            }
            this.NoneIssueItems.Clear();


            // �����϶����������
            for (int i = 0; i < this.ParentItems.Count; i++)
            {
                ItemBindingItem parent_item = this.ParentItems[i];

                string strBindingXml = parent_item.Binding;
                if (String.IsNullOrEmpty(strBindingXml) == true)
                {
                    // ���û��BindingXml�����ƶ�����������
                    this.ParentItems.Remove(parent_item);
                    Cell temp = new Cell();
                    temp.item = parent_item;
                    AddToFreeIssue(temp);
                    i--;
                    continue;
                }

                // ����refid, �ҵ�����������ЩItemBindingItem����
                XmlDocument dom = new XmlDocument();
                dom.LoadXml("<root />");
                try
                {
                    dom.DocumentElement.InnerXml = strBindingXml;
                }
                catch (Exception ex)
                {
                    strError = "�ο�IDΪ '"+parent_item.RefID+"' �Ĳ���Ϣ�У�<binding>Ԫ����ǶXMLװ��DOMʱ����: " + ex.Message;
                    return -1;
                }

                /*
                 * bindingxml�У�<item>Ԫ��δ����refID���ԡ�
                 * û��refID���ԣ���������һ����ɾ���˲��¼�ĵ�����Ϣ��Ԫ��������ȱ�������
                 * ȱ�ڿ��ܷ�����װ����Χ�ĵ�һ��������һ�ᣬҪ����ע��
                 * */

                parent_item.MemberCells.Clear();
                XmlNodeList nodes = dom.DocumentElement.SelectNodes("item");
                if (nodes.Count == 0)
                {
                    // ��ȻBindingXml����û���κ��¼�<item>Ԫ�أ����ƶ�����������
                    this.ParentItems.Remove(parent_item);
                    Cell temp = new Cell();
                    temp.item = parent_item;
                    AddToFreeIssue(temp);
                    i--;
                    continue;
                }

                List<ItemBindingItem> member_items = new List<ItemBindingItem>();

                for (int j = 0; j < nodes.Count; j++)
                {
                    XmlNode node = nodes[j];
                    string strRefID = DomUtil.GetAttr(node, "refID");

                    bool bItemRecordDeleted = false;    // ���¼�Ƿ�ɾ��?

                    if (String.IsNullOrEmpty(strRefID) == true)
                    {
                        bItemRecordDeleted = true;
                    }

                    ItemBindingItem sub_item = null;

                    if (bItemRecordDeleted == false)
                    {
                        sub_item = FindItem(strRefID);
                        if (sub_item == null)
                        {
                            if (String.IsNullOrEmpty(strWarning) == false)
                                strWarning += "; ";
                            // strWarning += "�ο�IDΪ '" + parent_item.RefID + "' �Ĳ���Ϣ�У�<binding>Ԫ���ڰ����Ĳο�ID '" + strRefID + "' û���ҵ���Ӧ�Ĳ���Ϣ";
                            bItemRecordDeleted = true;
                        }
                    }

                    if (sub_item == null)
                    {
                        // ���û���ֳɵ�item������ͨ��<item>Ԫ�ص��������������
                        sub_item = new ItemBindingItem();
                        nRet = sub_item.Initial("<root />", out strError);
                        Debug.Assert(nRet != -1, "");

                        sub_item.State = "��ɾ��";

                        // TODO: ���԰�node�����е����Զ�����Ϊitem�е�ͬ��Ԫ�أ�������������ĵط����������ֶ���
                        sub_item.Volume = DomUtil.GetAttr(node, "volume");
                        sub_item.PublishTime = DomUtil.GetAttr(node, "publishTime");
                        sub_item.RefID = DomUtil.GetAttr(node, "refID");
                        sub_item.Barcode = DomUtil.GetAttr(node, "barcode");
                        sub_item.RegisterNo = DomUtil.GetAttr(node, "registerNo");

                        bool bMissing = false;
                        // ��ò����͵����Բ���ֵ
                        // return:
                        //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
                        //      0   ���������ȷ����Ĳ���ֵ
                        //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
                        DomUtil.GetBooleanParam(node,
                            "missing",
                            false,
                            out bMissing,
                            out strError);
                        sub_item.Missing = bMissing;
                    }

                    // sub_item.Binded = true; // ע��place�׶λ����õ�
                    sub_item.ParentItem = parent_item;
                    sub_item.Deleted = bItemRecordDeleted;

                    member_items.Add(sub_item);

                    // ʹ�ú���Ȼ�ᱻ����
                }

                // ����
                if (member_items.Count > 0)
                {
                    // �Ѻ϶����ItemBindingItem���󰲷����������ĵ�һ����������ڵ�������
                    ItemBindingItem first_sub = member_items[0];
                    IssueBindingItem first_issue = first_sub.Container;

                    if (first_issue == null)
                    {
                        Debug.Assert(first_sub.Missing == true, "");

                        first_issue = this.FindIssue(first_sub.PublishTime);
                        if (first_issue == null)
                        {
                            first_issue = this.NewIssue(first_sub.PublishTime,
                                first_sub.Volume);
                        }
                    }

                    Debug.Assert(first_issue != null, "");

                    // ����������һ��Ŀ���λ�á�ż��
                    int col = -1;
                    if ((first_issue.Cells.Count % 2) == 0)
                    {
                        col = first_issue.Cells.Count;    // ���䰲�ŵ��к�
                        first_issue.Cells.Add(null);
                    }
                    else
                    {
                        col = first_issue.Cells.Count + 1;
                        first_issue.Cells.Add(null);
                        first_issue.Cells.Add(null);
                    }

                    // ���������ĵ�����
                    string strTemp = "";
                    PlaceMemberItems(parent_item,
                        member_items,
                        col + 1,
                        out strTemp);

                    // ����
                    foreach (ItemBindingItem temp in member_items)
                    {
                        placed_table[temp] = temp;
                    }

                    // �Ժ�Ű��ź϶�������
                    Cell cell = new Cell();
                    cell.item = parent_item;
                    parent_item.Container = first_issue; // ��װ���������
                    first_issue.SetCell(col, cell);

#if DEBUG
                    {
                        string strError1 = "";
                        int nRet1 = parent_item.VerifyMemberCells(out strError1);
                        if (nRet1 == -1)
                        {
                            Debug.Assert(false, strError1);
                        }
                    }
#endif
                }
            }

            // �����������󡣼��Ǻ϶���Ա��
            for (int i = 0; i < this.Issues.Count; i++)
            {
                List<ItemBindingItem> items = new List<ItemBindingItem>();
                IssueBindingItem issue = this.Issues[i];
                for (int j = 0; j < issue.Items.Count; j++)
                {
                    ItemBindingItem item = issue.Items[j];

                    if (placed_table[item] != null)
                        continue;

                    items.Add(item);
                }
                issue.Items.Clear();    // ��ʼ��ʹ������Ժ��������

                if (items.Count > 0)
                {
                    // ����Intact����
                    items.Sort(new ItemIntactComparer());

                    for (int j = 0; j < items.Count; j++)
                    {
                        ItemBindingItem item = items[j];
                        // ����������һ����ұ�λ��
                        PlaceSingleToTail(item);
                    }
                }

                // �״�����ÿ�����ӵ�OutputIssueֵ
                issue.RefreshAllOutofIssueValue();


            }


            AfterWidthChanged(true);

            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strError = strWarning;
                return 1;
            }

            return 0;
        }
#endif

        // �������е���ͳ�Ա��item��<binding>Ԫ�ء��϶����<binding>Ԫ���Ѿ���ʱ������
        public int Finish(out string strError)
        {
            strError = "";

            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                for (int j = 0; j < issue.Cells.Count; j++)
                {
                    Cell cell = issue.Cells[j];
                    if (cell == null)
                        continue;
                    if (cell.item == null)
                        continue;
                    if (cell.item.Deleted == true)
                        continue;

                    if (this.ParentItems.IndexOf(cell.item) != -1)
                        continue;   // �����϶���

                    if (cell.item.ParentItem != null)
                    {
                        // ��Ա��
                        string strXmlFragment = "";
                        // ������Ϊ��Ա���<binding>Ԫ����Ƭ��
                        // ��������һ��<bindingParent>Ԫ��
                        int nRet = cell.item.BuildMyselfBindingXmlString(
                            out strXmlFragment,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        if (cell.item.Binding != strXmlFragment)
                        {
                            cell.item.Binding = strXmlFragment;
                            cell.item.Changed = true;
                        }
                    }
                    else
                    {
                        // ����
                        // ���bindingxml
                        if (String.IsNullOrEmpty(cell.item.Binding) == false)
                        {
                            cell.item.Binding = "";
                            cell.item.Changed = true;
                        }
                    }
                }
            }

            return 0;
        }

        // ���ݿ��Ҫ�仯
        void AfterWidthChanged(bool bAlwaysInvalidate)
        {

            int nOldMaxCells = this.m_nMaxItemCountOfOneIssue;
            bool bChanged = false;

            // ������������ĸ߶�
            long lNewHeight = this.m_nCellHeight * this.Issues.Count;
            if (lNewHeight != this.m_lContentHeight)
            {
                this.m_lContentHeight = lNewHeight;
                bChanged = true;
            }

            int nMaxCells = 0;
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                issue.RemoveTailNullCell(); // 2010/2/20 

                if (nMaxCells < issue.Cells.Count)
                    nMaxCells = issue.Cells.Count;
            }

            if (nMaxCells != nOldMaxCells)
                bChanged = true;

            if (bChanged == true)
            {
                this.m_nMaxItemCountOfOneIssue = nMaxCells;

                SetContentWidth();

                try
                {
                    SetScrollBars(ScrollBarMember.Both);
                }
                catch
                {
                }
                // ˢ����ʾ
                this.Invalidate();
            }
            else
            {
                if (bAlwaysInvalidate == true)
                    this.Invalidate();
            }
        }

#if OLD_INITIAL
        // *** Ϊ��ʼ������
        // ��ʼ���ڼ䣬׷��һ���϶������
        public ItemBindingItem AppendBindItem(string strXml,
            out string strError)
        {
            strError = "";

            ItemBindingItem item = new ItemBindingItem();
            item.Container = null;
            item.RecPath = "";

            int nRet = item.Initial(strXml, out strError);
            if (nRet == -1)
                return null;

            this.ParentItems.Add(item);
            return item;
        }

        // *** Ϊ��ʼ������
        // ��ʼ���ڼ䣬׷��һ���ڶ���
        public IssueBindingItem AppendIssue(string strXml,
            out string strError)
        {
            strError = "";

            IssueBindingItem issue = new IssueBindingItem();

            issue.Container = this;
            int nRet = issue.Initial(strXml, 
                true,
                out strError);
            if (nRet == -1)
                return null;

            this.Issues.Add(issue);

            this.m_lContentHeight = this.m_nCellHeight * this.Issues.Count;

            return issue;
        }

         // *** Ϊ��ʼ������
       // ��ʼ���ڼ䣬׷��һϵ���������ڶ���ĵ������
        public int AppendNoneIssueSingleItems(List<string> XmlRecords,
            out string strError)
        {
            strError = "";

            this.NoneIssueItems.Clear();
            for (int i = 0; i < XmlRecords.Count; i++)
            {
                string strXml = XmlRecords[i];
                if (String.IsNullOrEmpty(strXml) == true)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                ItemBindingItem item = new ItemBindingItem();
                int nRet = item.Initial(strXml, out strError);
                if (nRet == -1)
                    return -1;

                item.Container = null;
                this.NoneIssueItems.Add(item);
            }

            return 0;
        }
#endif

        void InitialMaxItemCount()
        {
            if (this.m_nMaxItemCountOfOneIssue != -1)
                return; // �Ѿ���ʼ��

            int nMaxCount = 0;
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];

                int nCount = issue.Items.Count;

                if (nCount > nMaxCount)
                    nMaxCount = nCount;
            }

            this.m_nMaxItemCountOfOneIssue = nMaxCount;

            SetContentWidth();
        }

        void SetContentWidth()
        {
            Debug.Assert(this.m_nMaxItemCountOfOneIssue != -1, "");
            // ������������Ŀ��
            this.m_lContentWidth = m_nLeftTextWidth + (this.m_nMaxItemCountOfOneIssue * m_nCellWidth);
        }

#if OLD_INITIAL
        // �Ƿ��Ѿ��ҽӵ�GetItemInfo�¼�
        public bool HasGetItemInfo()
        {
            if (this.GetItemInfo == null)
                return false;

            return true;
        }
#endif

        #region ͼ����صĺ���

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // InitialMaxItemCount();  // ��ʼ���������������

            // �״���ʾǰ, OnSizeChanged()һ��Ҳû�б�����ǰ, ��ʾ�þ����
            SetScrollBars(ScrollBarMember.Both);
        }

        protected override void OnSizeChanged(System.EventArgs e)
        {

            try
            {
                SetScrollBars(ScrollBarMember.Both);
            }
            catch
            {
            }


            // ���client�����㹻�󣬵���org�����⿴����ĳ����
            DocumentOrgY = DocumentOrgY;
            DocumentOrgX = DocumentOrgX;


            base.OnSizeChanged(e);
        }


        // �������
        // parameters:
        //      p_x ���λ��x��Ϊ��Ļ����
        //      type    Ҫ���Ե����¼���Ҷ������������͡����Ϊnull����ʾһֱ��ĩ��
        void HitTest(long p_x,
            long p_y,
            Type dest_type,
            out HitTestResult result)
        {
            result = new HitTestResult();

            bool bIsRequiredType = false;

            if (dest_type == typeof(BindingControl))
                bIsRequiredType = true;


            // ����Ϊ�����ĵ�(�����������ҵĿհ�����)����
            long x = p_x - m_lWindowOrgX;
            long y = p_y - m_lWindowOrgY;

            if (y < this.m_nTopBlank)
                result.AreaPortion = AreaPortion.TopBlank;  // �Ϸ��հ�
            else if (y > this.m_nTopBlank + this.m_lContentHeight)
                result.AreaPortion = AreaPortion.BottomBlank;  // �·��հ�
            else if ((dest_type == null || bIsRequiredType == true) // ���ĩ������������Ҫ�������ҿհ׶������¼�����ķ�Χ
                && x < this.m_nLeftBlank)
                result.AreaPortion = AreaPortion.LeftBlank;  // �󷽿հ�
            else if ((dest_type == null || bIsRequiredType == true)
                && x > this.m_nLeftBlank + this.m_lContentWidth)
                result.AreaPortion = AreaPortion.RightBlank;  // �ҷ��հ�
            else
            {
                if (dest_type == typeof(BindingControl))
                {
                    result.AreaPortion = AreaPortion.Content;
                    goto END1;
                }

                /*
                long xOffset = m_lWindowOrgX + m_nLeftBlank;
                long yOffset = m_lWindowOrgY + m_nTopBlank;

                long x = p.X - xOffset;
                long y = p.Y - yOffset;
                 * */

                x -= this.m_nLeftBlank;
                y -= this.m_nTopBlank;

                long y0 = 0;
                for (int i = 0; i < this.Issues.Count; i++)
                {
                    IssueBindingItem issue = this.Issues[i];

                    if (y >= y0 && y < y0 + this.m_nCellHeight)
                    {
                        issue.HitTest(x,
                            y - y0,
                            dest_type,
                            out result);
                        return;
                    }

                    y0 += this.m_nCellHeight;
                }

                // ʵ���޷�ƥ��
                result.AreaPortion = AreaPortion.Content;
                goto END1;
            }

        END1:
            result.X = x;
            result.Y = y;
            result.Object = null;
        }

        /*
        static bool PtInRect(int x,
    int y,
    Rectangle rect)
        {
            if (x < rect.X)
                return false;
            if (x >= rect.Right)
                return false;
            if (y < rect.Y)
                return false;
            if (y >= rect.Bottom)
                return false;
            return true;
        }*/

        void BeginDraging()
        {
            this.m_bDraging = true;
            this.Cursor = Cursors.NoMove2D;
        }

        void EndDraging()
        {
            this.m_bDraging = false;
            this.Cursor = Cursors.Arrow;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (m_bRectSelecting == true)
            {
                // Debug.Assert(false, "");
                DoEndRectSelecting();
            }

            if (GuiUtil.PtInRect(e.X, e.Y, this.ClientRectangle) == false)
            {
                // ��ֹ�ھ�����ϵ������϶���ɸ�����
                goto END1;
            }

            this.Capture = true;

            this.Focus();

            if (e.Button == MouseButtons.Left)
            {
                bool bControl = (Control.ModifierKeys == Keys.Control);
                bool bShift = (Control.ModifierKeys == Keys.Shift);

                HitTestResult result = null;
                // ��Ļ����
                this.HitTest(
                    e.X,
                    e.Y,
                    null,
                    out result);
                if (result == null)
                    goto END1;

                this.DragStartMousePosition = e.Location;

                if (result.AreaPortion == AreaPortion.Grab)
                {
                    // ��ק���󣬶�����rectΧѡ
                    this.BeginDraging();
                    this.DragStartObject = (Cell)result.Object;
                    goto END1;
                }


                if (result.Object is CellBase)  // new changed 2010/2/26
                    this.FocusObject = (CellBase)result.Object;

                /*
                if (result.Object is Cell)
                {
                    bool bCheckBox = ShouldDisplayCheckBox((Cell)result.Object);
                    if (bCheckBox == true
                        && result.AreaPortion == AreaPortion.CheckBox)
                    {
                        CellChecked((Cell)result.Object);
                        goto END1;
                    }
                }
                 * */

                // �����ǰ��ѡ��
                if (bControl == false && bShift == false)   // ������SHIFT��Ҳ�������ǰ��
                {
                    if (m_bSelectedAreaOverflowed == false)
                    {
                        SelectObjects(this.m_aSelectedArea, SelectAction.Off);
                        ClearSelectedArea();
                    }
                    else
                    {
                        // ֻ�ò��ñ����ķ�����ȫ�����
                        /*
                         * // ���������ĻҪ����
                        this.DataRoot.ClearAllSubSelected();
                        this.Invalidate();
                         * */
                        List<CellBase> objects = new List<CellBase>();
                        this.ClearAllSubSelected(ref objects, 100);
                        if (objects.Count >= 100)
                            this.Invalidate();
                        else
                        {
                            // ���������Ļ������
                            UpdateObjects(objects);
                        }
                    }
                }

                // ȷ���㵽�Ķ���ȫ��������Ұ
                if (result.Object != null
                    && result.Object is CellBase)
                {
                    if (EnsureVisible((CellBase)result.Object) == true)
                        this.Update();
                }

                // ����ѡ��ʼ
                if (m_bRectSelectMode == true
                    && e.Button == MouseButtons.Left)
                {
                    this.m_DragStartPointOnDoc = new PointF(e.X - m_lWindowOrgX,
                        e.Y - m_lWindowOrgY);
                    this.m_DragCurrentPointOnDoc = m_DragStartPointOnDoc;
                    m_bRectSelecting = true;
                    goto END1;
                }

                if (result.Object != null
                    && result.Object is CellBase)   // 
                {
                    // 
                    // ����ˢ��һ��
                    List<CellBase> temp = new List<CellBase>();
                    temp.Add((CellBase)result.Object);
                    if (bControl == true)
                    {
                        SelectObjects(temp, SelectAction.Toggle);
                    }
                    else
                    {
                        SelectObjects(temp, SelectAction.On);
                    }


                    if (EnsureVisible((CellBase)result.Object) == true)
                        this.Update();


                    /*
                    ShowTip(result.Object, e.Location, false);
                     * */
                }
                // this.Update();
            }

        END1:
            base.OnMouseDown(e);
        }

        void CellChecked(Cell cell)
        {
            string strError = "";
            int nRet = 0;

            Debug.Assert(this.WholeLayout != "binding", "");
            Debug.Assert(cell.item != null, "");

            // 2010/9/21 add
            if (String.IsNullOrEmpty(cell.Container.PublishTime) == true)
            {
                strError = "�����ڵĸ��Ӳ��ܽ��мǵ�";
                goto ERROR1;
            }

            if (cell.item.Locked == true)
            {
                strError = "����״̬Ϊ����ʱ ��������мǵ����߳����ǵ��Ĳ���";
                goto ERROR1;
            }

            if (cell.item.Calculated == true)
            {
                nRet = cell.item.DoAccept(out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            else if (cell.item.OrderInfoPosition.X != -1
                && cell.item.NewCreated == true)
            {
                nRet = cell.item.DoUnaccept(out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            else
            {
                strError = "����״̬ ���ʺϽ��мǵ����߳����ǵ��Ĳ���";
                goto ERROR1;
            }

            // �ӵ�Ԫ���ӱ仯Ϊ���������ڸ���
            IssueBindingItem issue = cell.Container;

            this.UpdateObject(issue);

            // ˢ�±༭����
            if (cell == this.FocusObject)
            {
                // Cell focus_obejct = (Cell)this.FocusObject;
                if (this.CellFocusChanged != null)
                {
                    FocusChangedEventArgs e1 = new FocusChangedEventArgs();
                    e1.OldFocusObject = this.FocusObject;
                    e1.NewFocusObject = this.FocusObject;
                    this.CellFocusChanged(this, e1);
                }
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        protected override void OnMouseHover(EventArgs e)
        {
            if (m_bRectSelecting == true)
                return;

            HitTestResult result = null;

            Point p = this.PointToClient(Control.MousePosition);

            // Debug.WriteLine("hover=" + p.ToString());

            bool bCheckBox = false;

            // ��Ļ����
            this.HitTest(
                p.X,
                p.Y,
                typeof(NullCell),   // null
                out result);
            if (result == null)
                goto END1;

            // ֻ��עCell����
            if (result.Object != null)
            {
                if (result.Object.GetType() != typeof(Cell)
                    && result.Object.GetType() != typeof(GroupCell)
                    && result.Object.GetType() != typeof(NullCell))
                {
                    result.Object = null;
                }

                if (this.m_bDraging == true)
                {
                    this.FocusObject = (CellBase)result.Object;
                }

                if (this.WholeLayout != "binding"
                    && result.AreaPortion == AreaPortion.CheckBox)
                {
                    if (result.Object is Cell)
                    {
                        Cell cell = (Cell)result.Object;
                        if (cell != null)
                        {
                            bCheckBox = ShouldDisplayCheckBox(cell);
                            bool bOld = cell.m_bDisplayCheckBox;
                            cell.m_bDisplayCheckBox = bCheckBox;
                            if (cell.Selected == true && bOld != bCheckBox)
                            {
                                this.UpdateObjectHover(cell);    // ��ʹ�ı�
                            }
                        }
                    }
                }

                if (result.AreaPortion != AreaPortion.Grab
                    && result.Object != null)
                {
                    if (result.Object is Cell
                        || result.Object is GroupCell)
                    {
                        Cell cell = (Cell)result.Object;

                        if (cell != null)
                        {
                            if (this.WholeLayout != "binding")
                            {
                                bCheckBox = ShouldDisplayCheckBox(cell);
                                if (bCheckBox == true)
                                {
                                    if (result.AreaPortion != AreaPortion.CheckBox)
                                        bCheckBox = false;
                                }

                                cell.m_bDisplayCheckBox = bCheckBox;
                            }

                            // unselected cell��grab���ⲿ�ֱ���Ϊ��ͬ��off
                            if (cell.Selected == false && bCheckBox == false)
                                result.Object = null;
                        }
                    }
                    if (result.Object is NullCell)
                    {
                        result.Object = null;
                    }


                }

            }

            if (this.m_bDraging == true)
                return;

            this.HoverObject = (Cell)result.Object;

            /*

            // �����ϴε�hover������
            if (this.m_lastHoverObj == result.Object)
                goto END1;

            // �ڲ�ͬ��hover�����ϣ���ǰ��hover������Ҫoff
            if (this.m_lastHoverObj != null)
            {
                if (this.m_lastHoverObj.m_bHover != false)
                {
                    this.m_lastHoverObj.m_bHover = false;
                    UpdateObjectHover(this.m_lastHoverObj);
                }
            }

            this.m_lastHoverObj = (Cell)result.Object;

            if (this.m_lastHoverObj == null)
                goto END1;

            // ���ε�hover������Ҫon
            if (this.m_lastHoverObj.m_bHover != true)
            {
                this.m_lastHoverObj.m_bHover = true;
                UpdateObjectHover(this.m_lastHoverObj);
            }
            */

        END1:
            base.OnMouseHover(e);
        }

        /*
        // �����Ƿ�������Cell�����checkbox��λ
        // parameters:
        //      x   �����ڲ�����
        bool HitCheckBox(int x,
            int y)
        {
            int nCenterX = this.m_nCellWidth / 2;
            int nCenterY = this.m_nCellHeight / 2;
            int nWidth = this.m_rectGrab.Width;
            Rectangle rectCheckBox = new Rectangle(
                nCenterX - nWidth / 2,
                nCenterY - nWidth / 2,
                nWidth,
                nWidth);
            if (GuiUtil.PtInRect(x, y,
                rectCheckBox) == true)
                return true;

            return false;
        }
         * */

        // һ�������Ƿ�Ҫ(����)��ʾcheckbox?
        static bool ShouldDisplayCheckBox(Cell cell)
        {
            if (cell is GroupCell)
                return false;

            bool bCheckBox = false;
            if (cell.item != null)
            {
                if (cell.item.Locked == true)
                    return false;   // ����״̬�ĸ��Ӳ���ʾcheckbox

                if (cell.item.OrderInfoPosition.X != -1
                    && cell.item.NewCreated == true)
                    bCheckBox = true;
                else if (cell.item.Calculated == true)
                    bCheckBox = true;
            }

            return bCheckBox;
        }

        public Cell HoverObject
        {
            get
            {
                return this.m_lastHoverObj;
            }
            set
            {
                if (this.m_lastHoverObj == value)
                    return;

                if (this.m_lastHoverObj != null
                    && this.m_lastHoverObj.m_bHover == true)
                {
                    this.m_lastHoverObj.m_bHover = false;
                    if (this.m_lastFocusObj is Cell)
                        ((Cell)this.m_lastFocusObj).m_bDisplayCheckBox = false;
                    this.UpdateObjectHover(this.m_lastHoverObj);
                }

                this.m_lastHoverObj = value;

                if (this.m_lastHoverObj != null
                    && this.m_lastHoverObj.m_bHover == false)
                {
                    this.m_lastHoverObj.m_bHover = true;
                    this.UpdateObjectHover(this.m_lastHoverObj);
                }
            }
        }

        public CellBase FocusObject
        {
            get
            {
                return this.m_lastFocusObj;
            }
            set
            {
                if (value == null
                    || value is Cell
                    || value is NullCell
                    || value is IssueBindingItem)
                {
                }
                else
                {
                    throw new Exception("FocusObject����Ϊ����Cell/NullCell/IssueBindingItem֮һ");
                }
                
                if (this.m_lastFocusObj == value)
                    return;

                if (value is NullCell
                    && this.m_lastFocusObj is NullCell)
                {
                    // ��Ȼ���кͼ������õĶ���ͬ������ָ���λ�ú�״̬��ȫ��ͬ
                    NullCell new_cell = (NullCell)value;
                    NullCell exist_cell = (NullCell)this.m_lastFocusObj;
                    if (IsEqual(new_cell, exist_cell) == true)
                        return;
                }


                if (this.m_lastFocusObj != null
                    && this.m_lastFocusObj.m_bFocus == true)
                {
                    this.m_lastFocusObj.m_bFocus = false;
                    this.UpdateObject(this.m_lastFocusObj);
                }

                object oldObject = this.m_lastFocusObj;

                this.m_lastFocusObj = value;

                if (this.m_lastFocusObj != null
                    && this.m_lastFocusObj.m_bFocus == false)
                {
                    this.m_lastFocusObj.m_bFocus = true;
                    this.UpdateObject(this.m_lastFocusObj);
                }

                if (this.CellFocusChanged != null)
                {
                    FocusChangedEventArgs e = new FocusChangedEventArgs();
                    e.OldFocusObject = oldObject;
                    e.NewFocusObject = value;
                    this.CellFocusChanged(this, e);
                }
            }
        }

        static bool IsEqual(NullCell cell1, NullCell cell2)
        {
            if (cell1 == cell2)
                return true;

            if (cell1.X == cell2.X
                && cell1.Y == cell2.Y)
                return true;

            return false;
        }

        static bool IsEqual(CellBase cell1, CellBase cell2)
        {
            if (cell1 == cell2)
                return true;

            if (cell1 is NullCell && cell2 is NullCell)
            {
                return IsEqual((NullCell)cell1, (NullCell)cell2);
            }

            return false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            OnMouseHover(null);

            // �϶�ʱ�����Զ����
            if (this.m_bDraging == true
                && this.Capture == true
                && e.Button == MouseButtons.Left)
            {
                // �϶����
                if (IsNearestPoint(this.DragStartMousePosition, e.Location) == true)
                {
                    // ��ֹ��ԭ�ؾͱ���������϶�
                    goto END1;
                }

                // Ϊ���ܾ��
                HitTestResult result = null;

                // ��Ļ����
                this.HitTest(
                    e.X,
                    e.Y,
                    typeof(NullCell),   // null
                    out result);
                if (result == null)
                    goto END1;

                if (result.Object == null)
                    goto END1;

                {
                    // ȷ���ɼ�
                    if (EnsureVisibleWhenScrolling(result) == true)
                        this.Update();
                }

                if (result.Object.GetType() != typeof(Cell)
                    && result.Object.GetType() != typeof(NullCell))
                    goto END1;

                /*
                if (EnsureVisibleWhenScrolling((CellBase)result.Object) == true)
                    this.Update();
                 * */

                if (GuiUtil.PtInRect(e.X, e.Y, this.ClientRectangle) == false)
                    this.timer_dragScroll.Start();
                else
                    this.timer_dragScroll.Stop();
                goto END1;
            }

            // Χѡ
            if (this.Capture == true
                && e.Button == MouseButtons.Left)
            {

                // �϶����
                if (IsNearestPoint(this.DragStartMousePosition, e.Location) == true)
                {
                    // ��ֹ��ԭ�ؾͱ���������϶�
                    goto END1;
                }

                HitTestResult result = null;

                if (m_bRectSelecting == true
                    && e.Button == MouseButtons.Left
                    )
                {
                    // ����ϴ����ͼ��
                    DrawSelectRect(true);

                    // ������ε����ͼ��
                    this.m_DragCurrentPointOnDoc = new PointF(e.X - m_lWindowOrgX,
                        e.Y - m_lWindowOrgY);
                    DrawSelectRect(true);

                    // Ϊ���ܾ��
                    {
                        /*
                        Type objType = typeof(Cell);

                        if (this.DragStartObject != null)
                            objType = this.DragStartObject.GetType();
                         * */

                        result = null;

                        // ��Ļ����
                        this.HitTest(
                            e.X,
                            e.Y,
                            typeof(NullCell),   // null
                            out result);
                        if (result == null)
                            goto END1;

                        if (result.Object == null)
                            goto END1;

                        {
                            // ���
                            DrawSelectRect(true);
                            if (EnsureVisibleWhenScrolling(result) == true)
                                this.Update();

                            // �ػ�
                            DrawSelectRect(true);
                        }


                        if (result.Object.GetType() != typeof(Cell)
                            && result.Object.GetType() != typeof(NullCell))
                            goto END1;

                        // ����
                        if (this.DragStartObject == null)
                            this.DragStartObject = (CellBase)result.Object;

                        /*
                        if (IsEqual((CellBase)this.DragLastEndObject,
                            (CellBase)result.Object) == false)
                        {
                            // ���
                            DrawSelectRect(true);
                            if (EnsureVisibleWhenScrolling((CellBase)result.Object) == true)
                                this.Update();

                            // �ػ�
                            DrawSelectRect(true);

                            // if (result.Object is Cell)
                                this.DragLastEndObject = (CellBase)result.Object;
                        }
                         * */
                        if (IsEqual((CellBase)this.DragLastEndObject,
    (CellBase)result.Object) == false)
                            this.DragLastEndObject = (CellBase)result.Object;

                        if (GuiUtil.PtInRect(e.X, e.Y, this.ClientRectangle) == false)
                            this.timer_dragScroll.Start();
                        else
                            this.timer_dragScroll.Stop();
                    }

                    goto END1;
                }


                if (m_bRectSelecting == true)
                    goto END1;

#if NOOOOOOOOOOOOOOOOOO
                // �ҵ���ǰ����µĶ��󡣱����Ǻ�DragStartObjectͬһ���Ķ���
                if (this.DragStartObject == null)
                    goto END1;


                bool bControl = (Control.ModifierKeys == Keys.Control);
                bool bShift = (Control.ModifierKeys == Keys.Shift);

                // ��Ļ����
                this.HitTest(
                    e.X,
                    e.Y,
                    this.DragStartObject.GetType(),
                    out result);
                if (result == null)
                    goto END1;

                if (result.Object == null)
                    goto END1;

                /*
                String tipText = String.Format("   {0}", result.Object.FullName);
                trackTip.Show(tipText, this, e.Location);
                 * */
                // ShowTip(result.Object, e.Location, false);

                if (result.Object.GetType() != this.DragStartObject.GetType())
                    goto END1;

                /*
                if (this.DragLastEndObject == null
                    && result.Object == this.DragStartObject)
                {
                    this.DragLastEndObject = result.Object;
                    goto END1;
                }*/

                if (result.Object == this.DragLastEndObject)
                {
                    if (EnsureVisibleWhenScrolling((Cell)result.Object) == true)
                        this.Update();
                    goto END1;
                }

                // ����1
                // ��this.DragStartObject �� DragCurrentObject ֮�����
                // Ȼ�� DragCurrentObject �� result.Object֮�䣬ѡ��
                // ��������ٶ���

                // ����2
                // Current�� result.Object֮�䣬toggle��Ȼ���������Startѡ��

                List<Cell> objects = null;
                if (this.DragLastEndObject == null) // ��һ�ε��������
                {
                    // this.SetObjectFocus(this.DragStartObject, false);

                    objects = GetRangeObjects(
                        true,
                        true,
                        this.DragStartObject, result.Object);

                }
                else
                {
                    // B C֮��ķ���
                    this.m_nDirectionBC = GetDirection(this.DragLastEndObject, result.Object);

                    Debug.Assert(this.m_nDirectionBC != 0, "B C�������󣬲�����ͬ");

                    // ��� A-B B-Cͬ�� �򲻰���ͷ��������β��
                    if (this.m_nDirectionAB == 0 // �״��������
                        || this.m_nDirectionAB == this.m_nDirectionBC)
                    {
                        objects = GetRangeObjects(
                            false,
                            true,
                            this.DragLastEndObject,
                            result.Object);
                    }
                    else
                    {
                        // ��� A-B B-C��ͬ�� �����ͷ����������β��
                        objects = GetRangeObjects(
                            true,
                            false,
                            this.DragLastEndObject,
                            result.Object);
                    }
                }

                SelectObjects(objects, SelectAction.Toggle);

                {
                    // ׷��ѡ��ԭʼͷ��
                    List<AreaBase> temp = new List<AreaBase>();
                    temp.Add(this.DragStartObject);
                    temp.Add(result.Object);    // CҲ����
                    SelectObjects(temp, SelectAction.On);
                }

                // this.SetObjectFocus(this.DragLastEndObject, false);


                this.DragLastEndObject = result.Object;

                // this.SetObjectFocus(this.DragLastEndObject, true);


                // A B ֮��ķ���
                this.m_nDirectionAB = GetDirection(this.DragStartObject, this.DragLastEndObject);

                if (EnsureVisibleWhenScrolling(result.Object) == true)
                    this.Update();


                /*
                String tipText = String.Format("({0}, {1}) rect={2}", e.X, e.Y, this.ClientRectangle.ToString());
                trackTip.Show(tipText, this, e.Location);
                 */

                if (PtInRect(e.X, e.Y, this.ClientRectangle) == false)
                {
                    this.timer_dragScroll.Start();
                    // this.mouseMoveArgs = e;
                }
                else
                {
                    this.timer_dragScroll.Stop();
                }

#endif
                goto END1;
            }

            if (this.Capture == false)
            {
                if (IsNearestPoint(this.DragStartMousePosition, e.Location) == false)
                {
                    // ��ֹ��ԭ�صĶ���һ��MouseMove��Ϣ����tip����
                    trackTip.Hide(this);
                }
            }

        END1:
            // Call MyBase.OnMouseHover to activate the delegate.
            base.OnMouseMove(e);
        }

        // >0 ����ʱ��ֹcheckbox����
        int m_nDisableCheckBox = 0;

        protected override void OnMouseUp(MouseEventArgs e)
        {
            this.Capture = false;

            this.timer_dragScroll.Stop();

            //

            // ��ק�Ľ�������
            if (this.m_bDraging == true
                && e.Button == MouseButtons.Left)
            {

                this.EndDraging();
                this.DragLastEndObject = this.FocusObject;

                // �϶����
                if (IsNearestPoint(this.DragStartMousePosition, e.Location) == true)
                {
                    // ������ԭ��click down up

                }
                else
                {
                    if (this.DragStartObject != this.DragLastEndObject)
                    {
                        // MessageBox.Show(this, "�϶�����");
                        this.DoDragEndFunction();
                        goto END1;
                    }
                }
            }

            // checkbox
            if (this.WholeLayout != "binding"
                    && m_bRectSelecting == true
                    && m_nDisableCheckBox == 0
                && e.Button == MouseButtons.Left
                && IsNearestPoint(this.DragStartMousePosition, e.Location) == true)
            {
                HitTestResult result = null;
                // ��Ļ����
                this.HitTest(
                    e.X,
                    e.Y,
                    null,
                    out result);
                if (result != null && result.Object is Cell)
                {
                    bool bCheckBox = ShouldDisplayCheckBox((Cell)result.Object);
                    if (bCheckBox == true
                        && result.AreaPortion == AreaPortion.CheckBox)
                    {
                        CellChecked((Cell)result.Object);
                    }
                }
            }


            // �϶����ο�Χѡ�Ľ�������
            if (m_bRectSelecting == true
                && e.Button == MouseButtons.Left)
            {
                DoEndRectSelecting();
            }

            if (e.Button == MouseButtons.Right)
            {
                PopupMenu(e.Location);
                goto END1;
            }

        END1:
            base.OnMouseUp(e);
        }


        // ������
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int numberOfTextLinesToMove = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
            int numberOfPixelsToMove = numberOfTextLinesToMove * 18;

            DocumentOrgY += numberOfPixelsToMove;

            // base.OnMouseWheel(e);
        }

        // ���˫��
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (this.FocusObject is IssueBindingItem)
                {
                    menuItem_modifyIssue_Click(this, e);
                    goto END1;
                }

                if (this.FocusObject is Cell)
                {
                    // ��ʾ�༭����
                    {
                        EditAreaEventArgs e1 = new EditAreaEventArgs();
                        e1.Action = "get_state";
                        this.EditArea(this, e1);
                        if (e1.Result == "visible")
                            goto END1;
                    }

                    {
                        EditAreaEventArgs e1 = new EditAreaEventArgs();
                        e1.Action = "open";
                        this.EditArea(this, e1);
                    }

                    goto END1;
                }
            }

        END1:
            base.OnMouseDoubleClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                // �˵���
                case Keys.Apps:
                    {
                        Point p;
                        if (m_lastFocusObj != null)
                        {
                            RectangleF rect = GetViewRect(this.m_lastFocusObj);
                            p = new Point((int)rect.Right, (int)rect.Bottom);
                        }
                        else
                        {
                            p = this.PointToClient(Control.MousePosition);
                        }

                        PopupMenu(p);
                        break;
                    }
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                    {
                        DoArrowLeftRight(e.KeyCode);
                    }
                    break;
                case Keys.PageDown:
                case Keys.PageUp:
                    {
                        if (this.m_lastFocusObj == null)
                        {
                            AutoSetFocusObject();
                            if (this.m_lastFocusObj == null)
                                break;
                        }

                        if (this.m_lastFocusObj != null
                            && this.EnsureVisible(this.m_lastFocusObj) == true)
                            this.Update();

                        // ��õ�ǰ������ӵ��������ڵĴ�������
                        RectangleF rect = this.GetViewRect(this.m_lastFocusObj);

                        if (e.KeyCode == Keys.PageDown)
                            this.DocumentOrgY -= this.ClientSize.Height;
                        else
                            this.DocumentOrgY += this.ClientSize.Height;

                        // ��ҳǰͬ���Ĵ�������λ�ã�ģ���һ�����
                        MouseEventArgs e1 = new MouseEventArgs(MouseButtons.Left,
                            1,
                            (int)(rect.X + rect.Width / 2),
                            (int)(rect.Y + rect.Height / 2),
                            0);

                        // TODO: �㵽����λ�ã����ܻᵼ�¼ǵ���Ҫ��ֹ 2011/8/4

                        // �㵽���ڱ�������ô�죿�ƺ�����Ҫ���õĽ���취��
                        // ���Թ��㵱ǰ�ͻ����߶��൱�ڶ����У�Ȼ��ֱ�Ӱѽ���
                        // �����ƶ���ô���У��������߷�Χ�����ɡ�
                        this.m_nDisableCheckBox++;

                        this.OnMouseDown(e1);
                        this.OnMouseUp(e1);

                        this.m_nDisableCheckBox--;

                        if (this.m_lastFocusObj != null
                            && this.EnsureVisible(this.m_lastFocusObj) == true)
                            this.Update();

                    }
                    break;
            }

            base.OnKeyDown(e);
        }

        void GetCellXY(CellBase cell,
            out int x,
            out int y)
        {
            x = -2;
            y = -1;
            if (cell is NullCell)
            {
                NullCell null_cell = (NullCell)cell;
                x = null_cell.X;
                y = null_cell.Y;
            }
            else if (cell is Cell)
            {
                Cell normal_cell = (Cell)cell;
                x = normal_cell.Container.Cells.IndexOf(normal_cell);
                y = this.Issues.IndexOf(normal_cell.Container);
            }
            else if (cell is IssueBindingItem)
            {
                IssueBindingItem issue = (IssueBindingItem)cell;
                x = -1;
                y = this.Issues.IndexOf(issue);
            }
            else
            {
                Debug.Assert(false, "");
            }
        }

        // parameters:
        //      bCross  �Ƿ������Խ�ڡ���߽�
        CellBase GetLeftCell(CellBase cell,
            bool bCross)
        {
            int x = -2;
            int y = -1;
            GetCellXY(cell,
                out x,
                out y);

            if (x < 0)
                return null;

            if (bCross == false)
            {
                if (x == 0)
                    return null;
            }
            else
            {
                if (x == 0)
                    return this.Issues[y];
            }

            x--;
            Debug.Assert(x >= 0, "");

            IssueBindingItem issue = this.Issues[y];
            if (issue.Cells.Count <= x
                || issue.Cells[x] == null)
                return new NullCell(x, y);

            return issue.Cells[x];
        }


        // parameters:
        //      bCross  �Ƿ������Խ�ڡ���߽�
        CellBase GetRightCell(CellBase cell,
            bool bCross)
        {
            int x = -2;
            int y = -1;
            GetCellXY(cell,
                out x,
                out y);

            if (bCross == false)
            {
                if (x < 0)
                    return null;
            }

            if (x >= this.m_nMaxItemCountOfOneIssue - 1)
                return null;

            x++;
            Debug.Assert(x >= 0, "");
            IssueBindingItem issue = this.Issues[y];
            if (issue.Cells.Count <= x
                || issue.Cells[x] == null)
                return new NullCell(x, y);

            return issue.Cells[x];
        }

        CellBase GetUpCell(CellBase cell)
        {
            int x = -2;
            int y = -1;
            GetCellXY(cell,
                out x,
                out y);

            if (y < 0)
                return null;

            if (y == 0)
                return null;

            y--;

            IssueBindingItem issue = this.Issues[y];

            if (x == -1)
                return issue;

            if (issue.Cells.Count <= x
                || issue.Cells[x] == null)
                return new NullCell(x, y);

            return issue.Cells[x];
        }
        CellBase GetDownCell(CellBase cell)
        {
            int x = -2;
            int y = -1;
            GetCellXY(cell,
                out x,
                out y);

            if (y < 0)
                return null;

            if (y >= this.Issues.Count - 1)
                return null;

            y++;
            IssueBindingItem issue = this.Issues[y];

            if (x == -1)
                return issue;

            if (issue.Cells.Count <= x
                || issue.Cells[x] == null)
                return new NullCell(x, y);

            return issue.Cells[x];
        }

        // 2011/8/4
        // ��û�н������ʱ������һ���������
        bool AutoSetFocusObject()
        {
            if (m_lastFocusObj == null)
            {
                // �Զ��ѵ�һ��������Ϊ����
                if (this.Issues.Count > 0)
                {
                    m_lastFocusObj = this.Issues[0].GetFirstCell();
                    SetObjectFocus(m_lastFocusObj);
                }
                return true;
            }

            return false;
        }

        // �������ҷ����
        void DoArrowLeftRight(Keys key)
        {
            if (m_lastFocusObj == null)
            {
                // �Զ��ѵ�һ��������Ϊ����
                if (this.Issues.Count > 0)
                {
                    m_lastFocusObj = this.Issues[0].GetFirstCell();
                    SetObjectFocus(m_lastFocusObj);
                }
                return;
            }

            CellBase obj = null;

            bool bControl = (Control.ModifierKeys == Keys.Control);
            bool bShift = (Control.ModifierKeys == Keys.Shift);

            if (bControl == true || bShift == true)
            {
                if (key == Keys.Left)
                    obj = GetLeftCell(m_lastFocusObj, false);
                else if (key == Keys.Right)
                    obj = GetRightCell(m_lastFocusObj, false);
                else if (key == Keys.Up)
                    obj = GetUpCell(m_lastFocusObj);
                else if (key == Keys.Down)
                    obj = GetDownCell(m_lastFocusObj);
            }
            else
            {
                if (key == Keys.Left)
                    obj = GetLeftCell(m_lastFocusObj, true);
                else if (key == Keys.Right)
                    obj = GetRightCell(m_lastFocusObj, true);
                else if (key == Keys.Up)
                    obj = GetUpCell(m_lastFocusObj);
                else if (key == Keys.Down)
                    obj = GetDownCell(m_lastFocusObj);
            }

            if (obj != null)
            {
                // �����ǰ��ѡ��
                if (bControl == false && bShift == false)   // ������SHIFT��Ҳ�������ǰ��
                {
                    if (m_bSelectedAreaOverflowed == false)
                    {
                        SelectObjects(this.m_aSelectedArea, SelectAction.Off);
                        ClearSelectedArea();
                    }
                    else
                    {
                        // ֻ�ò��ñ����ķ�����ȫ�����
                        List<CellBase> objects = new List<CellBase>();
                        this.ClearAllSubSelected(ref objects, 100);
                        if (objects.Count >= 100)
                            this.Invalidate();
                        else
                        {
                            // ���������Ļ������
                            UpdateObjects(objects);
                        }
                    }
                }
                else
                {
                    // ������Ctrl����Shift�����
                    /*
                    if (obj.GetType() != this.DragStartObject.GetType())
                        return;
                     * */

                    if (this.DragStartObject == null)
                    {
                        this.DragStartObject = this.FocusObject;
                        this.DragLastEndObject = this.FocusObject;
                    }

                    if (obj == this.DragLastEndObject)
                    {
                        if (EnsureVisible(obj) == true)
                            this.Update();
                        return;
                    }

                    List<CellBase> current = GetRangeObjects(
            this.DragStartObject,
            obj);
                    List<CellBase> last = new List<CellBase>();
                    if (this.DragLastEndObject != null)
                    {
                        last = GetRangeObjects(
                             this.DragStartObject,
                             this.DragLastEndObject);
                    }

                    /*
                    List<CellBase> old = new List<CellBase>();
                    old.AddRange(this.m_aSelectedArea);
                     * */

                    List<CellBase> cross = null;
                    // a��b�н���Ĳ��ַ���union������a��b��ȥ��
                    Compare(ref current,
                        ref last,
                        out cross);

                    SelectObjects(last, SelectAction.Toggle);
                    // cross���ֲ��ò���
                    SelectObjects(current, SelectAction.On);

                    this.DragLastEndObject = obj;

                    // ����2
                    // Current�� result.Object֮�䣬toggle��Ȼ���������Startѡ��

                    if (EnsureVisibleWhenScrolling(obj) == true)
                        this.Update();

                    return;
                }


            // END1:
                this.DragStartObject = obj;
                this.DragLastEndObject = null;  // ���

                // ѡ����һ��
                List<CellBase> temp = new List<CellBase>();
                temp.Add(obj);
                if (bControl == true)
                {
                    SelectObjects(temp, SelectAction.Toggle);
                }
                else
                {
                    SelectObjects(temp, SelectAction.On);
                }

                if (EnsureVisibleWhenScrolling(obj) == true)
                    this.Update();


                // ShowTip(obj, e.Location, false);

            }
            else
            {
                // ���������Ե�����
                // Console.Beep();
            }

        }

        void BuildBindingMeneItems(ContextMenuStrip contextMenu,
            bool bHasCellSelected)
        {
            ToolStripMenuItem menuItem = null;
            ToolStripLabel label = null;

            label = new ToolStripLabel("װ��");
            label.Font = new Font(label.Font, FontStyle.Bold);
            label.ForeColor = Color.DarkGreen;
            contextMenu.Items.Add(label);

            // �϶�ѡ�������
            menuItem = new ToolStripMenuItem(" �϶�(&B)");
            menuItem.Click += new EventHandler(menuItem_bindingSelectedItem_Click);
            if (bHasCellSelected == false)  // TODO: �����������ϸ�һЩ��ֻ�е�����ѡ����δװ���ĵ��ᣬ�˵���ſ���
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);


            bool bHasParentCell = false;
            bool bHasMemberCell = false;
            List<Cell> selected_cells = this.SelectedCells;
            for (int i = 0; i < selected_cells.Count; i++)
            {
                Cell cell = selected_cells[i];

                if (cell.IsMember == true)
                {
                    bHasMemberCell = true;
                }

                if (cell.item != null)
                {
                    if (cell.item.MemberCells.Count > 0)
                    {
                        Debug.Assert(cell.item.MemberCells.Count > 0, "");
                        bHasParentCell = true;
                    }

                }
            }

            // ����϶�
            menuItem = new ToolStripMenuItem(" ����϶�(&R)");
            menuItem.Click += new EventHandler(menuItem_releaseBinding_Click);
            if (bHasParentCell == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // ��ɾ����Ա���¼
            menuItem = new ToolStripMenuItem(" ��ɾ����Ա���¼(&D)");
            menuItem.Click += new EventHandler(menuItem_onlyDeleteMemberRecords_Click);
            if (bHasParentCell == false
                && bHasMemberCell == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);


            // �Ƴ�[������]
            menuItem = new ToolStripMenuItem(" �Ƴ�[������](&M)");
            menuItem.Click += new EventHandler(menuItem_removeFromBinding_Click);
            if (bHasMemberCell == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // �Ƴ�[����]
            menuItem = new ToolStripMenuItem(" �Ƴ�[����](&S)");
            menuItem.Click += new EventHandler(menuItem_removeFromBindingAndShrink_Click);
            if (bHasMemberCell == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

        }

        void BuildAcceptingMeneItems(ContextMenuStrip contextMenu)
        {
            ToolStripMenuItem menuItem = null;
            ToolStripLabel label = null;

            label = new ToolStripLabel("�ǵ�");
            label.Font = new Font(label.Font, FontStyle.Bold);
            label.ForeColor = Color.DarkGreen;
            contextMenu.Items.Add(label);

            // ��
            menuItem = new ToolStripMenuItem(" ��(&A)");
            menuItem.Click += new EventHandler(menuItem_AcceptCells_Click);
            contextMenu.Items.Add(menuItem);

            // �����ǵ�
            menuItem = new ToolStripMenuItem(" �����ǵ�(&U)");
            menuItem.Click += new EventHandler(menuItem_unacceptCells_Click);
            contextMenu.Items.Add(menuItem);


            // ����Ԥ���
            menuItem = new ToolStripMenuItem(" ����Ԥ���[ǰ��](&C)");
            menuItem.Click += new EventHandler(menuItem_newCalulatedCells_Click);
            contextMenu.Items.Add(menuItem);
        }

        // �����Ĳ˵�
        void PopupMenu(Point point)
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            ToolStripMenuItem menuItem = null;
            ToolStripSeparator menuSepItem = null;
            ToolStripLabel label = null;

            // �Ƿ��в���ӱ�ѡ��
            bool bHasCellSelected = this.HasCellSelected();
            // �Ƿ����ڸ��ӱ�ѡ��
            bool bHasIssueSelected = this.HasIssueSelected();

            if (this.WholeLayout != "binding")
            {
                // *** �ǵ�
                BuildAcceptingMeneItems(contextMenu);
            }
            else
            {
                // *** װ��
                BuildBindingMeneItems(contextMenu,
                    bHasCellSelected);
            }

            // ---
            menuSepItem = new ToolStripSeparator();
            contextMenu.Items.Add(menuSepItem);


            label = new ToolStripLabel("��");
            label.Font = new Font(label.Font, FontStyle.Bold);
            label.ForeColor = Color.DarkGreen;
            contextMenu.Items.Add(label);

            // �༭�����
            menuItem = new ToolStripMenuItem(" �༭(&M)");
            menuItem.Click += new EventHandler(menuItem_modifyCell_Click);
            if (bHasCellSelected == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // ˢ�³���ʱ��
            menuItem = new ToolStripMenuItem(" ˢ�³���ʱ��(&P)");
            menuItem.Click += new EventHandler(menuItem_refreshPublishTime_Click);
            if (bHasCellSelected == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // ˢ�¾��ڷ�Χ
            menuItem = new ToolStripMenuItem(" ˢ�¾��ڷ�Χ(&P)");
            menuItem.Click += new EventHandler(menuItem_refreshVolumeString_Click);
            if (bHasCellSelected == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // ��Ϊ�հ�
            menuItem = new ToolStripMenuItem(" ��Ϊ�հ�(&B)");
            menuItem.Tag = point;
            menuItem.Click += new EventHandler(menuItem_setBlank_Click);
            if (bHasCellSelected == true || bHasIssueSelected == true)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            /*
            // �����ƶ�
            menuItem = new ToolStripMenuItem("�����ƶ�(&L)");
            menuItem.Click += new EventHandler(menuItem_moveToLeft_Click);
            if (bHasSelected == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);
             * */


            // ɾ�������
            menuItem = new ToolStripMenuItem(" ɾ��(&D)");
            menuItem.Click += new EventHandler(menuItem_deleteCells_Click);
            if (bHasCellSelected == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);


            // ---
            menuSepItem = new ToolStripSeparator();
            contextMenu.Items.Add(menuSepItem);

            label = new ToolStripLabel("��");
            label.Font = new Font(label.Font, FontStyle.Bold);
            label.ForeColor = Color.DarkGreen;
            contextMenu.Items.Add(label);

            // ������
            menuItem = new ToolStripMenuItem(" ����[���](&N)");
            menuItem.Click += new EventHandler(menuItem_newIssue_Click);
            contextMenu.Items.Add(menuItem);

            // ��ȫ����
            menuItem = new ToolStripMenuItem(" ��ȫ[���](&A)");
            menuItem.Click += new EventHandler(menuItem_newAllIssue_Click);
            contextMenu.Items.Add(menuItem);

            // �޸���
            menuItem = new ToolStripMenuItem(" �޸�(&M)");
            menuItem.Click += new EventHandler(menuItem_modifyIssue_Click);
            if (bHasIssueSelected == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // ˢ�¶�����Ϣ
            menuItem = new ToolStripMenuItem(" ˢ�¶�����Ϣ(&R)");
            menuItem.Click += new EventHandler(menuItem_refreshOrderInfo_Click);
            if (bHasIssueSelected == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);


            // ɾ����
            menuItem = new ToolStripMenuItem(" ɾ��(&D)");
            menuItem.Tag = point;
            menuItem.Click += new EventHandler(menuItem_deleteIssues_Click);
            if (bHasIssueSelected == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);

            // �ָ��ڼ�¼
            menuItem = new ToolStripMenuItem(" �ָ��ڼ�¼(&V)");
            menuItem.Tag = point;
            menuItem.Click += new EventHandler(menuItem_recoverIssues_Click);
            if (bHasIssueSelected == false)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);


            // �л��ڲ���
            menuItem = new ToolStripMenuItem(" �л�����(&S)");
            /*
            if (bHasIssueSelected == false)
                menuItem.Enabled = false;
             * */
            contextMenu.Items.Add(menuItem);

            if (menuItem.Enabled == true)
            {
                // TODO:��ͳ������ռ������һ��ģʽ���򹴵��Ӳ˵���
                IssueLayoutState layout = GetMostSelectedLayoutState();

                // �Ӳ˵�
                {
                    ToolStripMenuItem subMenuItem = new ToolStripMenuItem();
                    subMenuItem.Text = "װ��";
                    subMenuItem.Tag = IssueLayoutState.Binding;
                    subMenuItem.Image = this.imageList_layout.Images[0];
                    subMenuItem.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                    subMenuItem.ImageTransparentColor = this.imageList_layout.TransparentColor;
                    if (bHasIssueSelected == false)
                        subMenuItem.Enabled = false;
                    else
                    {
                        if (layout == IssueLayoutState.Binding)
                            subMenuItem.Checked = true;
                    }
                    subMenuItem.Click += new EventHandler(MenuItem_switchIssueLayout_Click);
                    menuItem.DropDown.Items.Add(subMenuItem);
                }

                {
                    ToolStripMenuItem subMenuItem = new ToolStripMenuItem();
                    subMenuItem.Text = "�ǵ�";
                    subMenuItem.Tag = IssueLayoutState.Accepting;
                    subMenuItem.Image = this.imageList_layout.Images[1];
                    subMenuItem.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                    subMenuItem.ImageTransparentColor = this.imageList_layout.TransparentColor;
                    if (bHasIssueSelected == false)
                        subMenuItem.Enabled = false;
                    else
                    {
                        if (layout == IssueLayoutState.Accepting)
                            subMenuItem.Checked = true;
                    }
                    subMenuItem.Click += new EventHandler(MenuItem_switchIssueLayout_Click);
                    menuItem.DropDown.Items.Add(subMenuItem);
                }

                {
                    // ---
                    menuSepItem = new ToolStripSeparator();
                    menuItem.DropDown.Items.Add(menuSepItem);

                    ToolStripMenuItem subMenuItem = new ToolStripMenuItem();
                    subMenuItem.Text = "���²���";
                    subMenuItem.Click += new EventHandler(MenuItem_refreshIssueLayout_Click);
                    menuItem.DropDown.Items.Add(subMenuItem);
                }
            }

            // ---
            menuSepItem = new ToolStripSeparator();
            contextMenu.Items.Add(menuSepItem);

            if (this.WholeLayout != "binding")
            {
                // *** װ��
                BuildBindingMeneItems(contextMenu,
                    bHasCellSelected);
            }
            else
            {
                // *** �ǵ�
                BuildAcceptingMeneItems(contextMenu);
            }

            // ---
            menuSepItem = new ToolStripSeparator();
            contextMenu.Items.Add(menuSepItem);


            // �༭����
            menuItem = new ToolStripMenuItem("�༭����(&E)");
            menuItem.Click += new EventHandler(menuItem_toggleEditArea_Click);
            if (this.EditArea == null)
                menuItem.Enabled = false;
            contextMenu.Items.Add(menuItem);
            if (this.EditArea != null)
            {
                EditAreaEventArgs e1 = new EditAreaEventArgs();
                e1.Action = "get_state";
                this.EditArea(this, e1);
                if (e1.Result == "visible")
                    menuItem.Checked = true;
                else
                    menuItem.Checked = false;
            }

            this.Update();
            contextMenu.Show(this, point);
        }

        // ˢ�¶�����Ϣ
        void menuItem_refreshOrderInfo_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            List<IssueBindingItem> selected_issues = this.SelectedIssues;
            // ����һ��selected_issues����
            for (int i = 0; i < selected_issues.Count; i++)
            {
                IssueBindingItem issue = selected_issues[i];
                if (issue == null
                    || String.IsNullOrEmpty(issue.PublishTime) == true)
                {
                    selected_issues.RemoveAt(i);
                    i--;
                    continue;
                }
            }

            if (selected_issues.Count == 0)
            {
                strError = "��δѡ��Ҫˢ�µ��ڸ���";
                goto ERROR1;
            }

            for (int i = 0; i < selected_issues.Count; i++)
            {
                IssueBindingItem issue = selected_issues[i];

                nRet = issue.RefreshOrderInfo(out strError);
                if (nRet == -1)
                    goto ERROR1;
            }


            this.AfterWidthChanged(true);   // content��ȿ��ܸı�
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ˢ�³���ʱ��
        void menuItem_refreshPublishTime_Click(object sender, EventArgs e)
        {
            string strError = "";
            // int nRet = 0;

            List<Cell> selected_cells = this.SelectedCells;
            if (selected_cells.Count == 0)
            {
                strError = "��δѡ��Ҫˢ�³���ʱ��ĸ���";
                goto ERROR1;
            }

            List<CellBase> changed_cells = new List<CellBase>();
            for (int i = 0; i < selected_cells.Count; i++)
            {
                Cell cell = selected_cells[i];

                if (cell != null
                    && String.IsNullOrEmpty(cell.Container.PublishTime) == true)
                    continue;   // �����������ڵ�

                if (cell.item == null)
                    continue;

                if (this.IsBindingParent(cell) == true)
                {
                    // �϶���
                    Debug.Assert(cell.item != null, "");
                    Debug.Assert(cell.item.IsMember == false, "");


                    if (cell.item.RefreshPublishTime() == true)
                    {
                        cell.item.Changed = true;
                        changed_cells.Add(cell);
                    }
                }
                else if (cell.IsMember == true)
                {
                    // ע��cell.item����Ϊ��
                    if (cell.item != null)
                    {
                        if (cell.item.RefreshPublishTime() == true)
                        {
                            cell.item.Changed = true;
                            changed_cells.Add(cell);
                        }
                    }
                }
                else
                {
                    // ����
                    if (cell.item != null
                        && cell.item.RefreshPublishTime() == true)
                    {
                        cell.item.Changed = true;
                        changed_cells.Add(cell);
                    }
                }
            }

            if (changed_cells.Count == 0)
            {
                strError = "û�з���ˢ��";
                goto ERROR1;
            }

            if (changed_cells.IndexOf(this.FocusObject) != -1)
            {
                if (this.CellFocusChanged != null)
                {
                    FocusChangedEventArgs e1 = new FocusChangedEventArgs();
                    e1.OldFocusObject = this.FocusObject;
                    e1.NewFocusObject = this.FocusObject;
                    this.CellFocusChanged(this, e1);
                }
            }

            this.UpdateObjects(changed_cells);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ˢ�¾��ڷ�Χ
        void menuItem_refreshVolumeString_Click(object sender, EventArgs e)
        {
            string strError = "";
            // int nRet = 0;

            List<Cell> selected_cells = this.SelectedCells;
            if (selected_cells.Count == 0)
            {
                strError = "��δѡ��Ҫˢ�¾��ڷ�Χ�ĸ���";
                goto ERROR1;
            }

            List<CellBase> changed_cells = new List<CellBase>();
            for (int i = 0; i < selected_cells.Count; i++)
            {
                Cell cell = selected_cells[i];

                if (cell != null
    && String.IsNullOrEmpty(cell.Container.PublishTime) == true)
                    continue;   // �����������ڵ�

                if (cell.item == null)
                    continue;

                if (this.IsBindingParent(cell) == true)
                {
                    // �϶���
                    Debug.Assert(cell.item != null, "");
                    Debug.Assert(cell.item.IsMember == false, "");

                    if (cell.item.RefreshVolumeString() == true)
                    {
                        cell.item.Changed = true;
                        changed_cells.Add(cell);
                    }
                }
                else if (cell.IsMember == true)
                {
                    // ע��cell.item����Ϊ��
                    if (cell.item != null)
                    {
                        if (cell.item.RefreshVolumeString() == true)
                        {
                            cell.item.Changed = true;
                            changed_cells.Add(cell);
                        }
                    }
                }
                else
                {
                    // ����
                    if (cell.item != null
                        && cell.item.RefreshVolumeString() == true)
                    {
                        cell.item.Changed = true;
                        changed_cells.Add(cell);
                    }
                }
            }

            if (changed_cells.Count == 0)
            {
                strError = "û�з���ˢ��";
                goto ERROR1;
            }

            if (changed_cells.IndexOf(this.FocusObject) != -1)
            {
                if (this.CellFocusChanged != null)
                {
                    FocusChangedEventArgs e1 = new FocusChangedEventArgs();
                    e1.OldFocusObject = this.FocusObject;
                    e1.NewFocusObject = this.FocusObject;
                    this.CellFocusChanged(this, e1);
                }
            }

            this.UpdateObjects(changed_cells);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��ɾ����Ա���¼�������ı�϶���Χ��
        void menuItem_onlyDeleteMemberRecords_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strWarning = "";
            // int nRet = 0;

            List<Cell> selected_cells = this.SelectedCells;
            if (selected_cells.Count == 0)
            {
                strError = "��δѡ��Ҫ(��)ɾ����¼�ĸ���";
                goto ERROR1;
            }

            // �Ѿ�����װ���Ĳ�
            List<Cell> member_cells = new List<Cell>();

            // �϶���
            List<Cell> parent_cells = new List<Cell>();


            // ��ѡ
            for (int i = 0; i < selected_cells.Count; i++)
            {
                Cell cell = selected_cells[i];

                if (this.IsBindingParent(cell) == true)
                {
                    Debug.Assert(cell.item != null, "");
                    Debug.Assert(cell.item.IsMember == false, "");
                    parent_cells.Add(cell);
                }
                else if (cell.IsMember == true)
                {
                    // ע��cell.item����Ϊ��
                    member_cells.Add(cell);
                }
                else
                {
                    strError = "�����ܲ���������ͨ�������";
                    goto ERROR1;
                }
            }

            // ����Ա��
            strWarning = "";
            int nErrorCount = 0;
            int nOldCount = member_cells.Count;
            for (int i = 0; i < member_cells.Count; i++)
            {
                Cell cell = member_cells[i];
                IssueBindingItem issue = cell.Container;
                Debug.Assert(issue != null, "");

                if (cell.item == null)
                    continue;

                // ��������ѡ�϶���ĳ�Ա�ᣬҪ�����ظ�ɾ��
                bool bFound = false;
                for (int j = 0; j < parent_cells.Count; j++)
                {
                    Cell parent_cell = parent_cells[j];

                    Debug.Assert(parent_cell.item != null, "");
                    if (cell.ParentItem == parent_cell.item)
                    {
                        bFound = true;
                        break;
                    }
                }
                if (bFound == true)
                {
                    member_cells.RemoveAt(i);
                    i--;
                    continue;
                }

                if (String.IsNullOrEmpty(cell.item.Borrower) == false)
                {
                    // �ѽ��״̬
                    if (String.IsNullOrEmpty(strWarning) == false)
                        strWarning += ";\r\n";
                    strWarning += "�� '" + cell.item.RefID + "' �д��ڡ��ѽ����״̬";
                    nErrorCount++;
                    member_cells.RemoveAt(i);
                    i--;
                }
            }

            // ���治��ɾ���ĳ�Ա��
            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strError =
                    "��ѡ���� " + nOldCount.ToString() + " ����Ա���У������в��¼����ɾ��:\r\n\r\n" + strWarning;
                    goto ERROR1;
            }

            strWarning = "";
            if (member_cells.Count > 0)
                strWarning += " " + member_cells.Count.ToString() + " ����Ա��Ĳ��¼";

            if (parent_cells.Count > 0)
            {
                if (String.IsNullOrEmpty(strWarning) == false)
                    strWarning += " ��";
                strWarning += " " + parent_cells.Count.ToString() + " ���϶������������г�Ա���¼";
            }

            strWarning = "ȷʵҪɾ����ѡ����" + strWarning + "?\r\n\r\n(ע�������ܲ����ı�϶���Χ�ͺ϶�״̬)";
            DialogResult dialog_result = MessageBox.Show(this,
                strWarning,
                "BindingControls",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (dialog_result == DialogResult.No)
                return;

            // ��ʼɾ��

            // �����ѡ�϶���������ȫ����Ա��
            strWarning = "";
            nErrorCount = 0;
            nOldCount = parent_cells.Count;
            for (int i = 0; i < parent_cells.Count; i++)
            {
                Cell parent_cell = parent_cells[i];
                if (parent_cell.item == null)
                    continue;

                // ���϶��������״̬
                if (parent_cell.item.Locked == true)
                {
                    strError = "����״̬�ĺ϶��ᣬ���Ա�᲻����ɾ��";
                    goto ERROR1;
                }

                // ��������ѡ�϶���ĳ�Ա�ᣬҪ�����ظ�ɾ��
                for (int j = 0; j < parent_cell.item.MemberCells.Count; j++)
                {
                    Cell member_cell = parent_cell.item.MemberCells[j];
                    if (member_cell.item == null)
                        continue;

                    Debug.Assert(member_cell.item != null, "");

                    if (String.IsNullOrEmpty(member_cell.item.Borrower) == false)
                    {
                        // �ѽ��״̬
                        if (String.IsNullOrEmpty(strWarning) == false)
                            strWarning += ";\r\n";
                        strWarning += "�� '" + member_cell.item.RefID + "' �д��ڡ��ѽ����״̬";
                        nErrorCount++;
                        j--;
                    }
                }
            }

            // ���治��ɾ����(��ѡ�϶����)��Ա��
            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strError =
                    "��ѡ���� " + nOldCount.ToString() + " ���϶����У������г�Ա���¼����ɾ��:\r\n\r\n" + strWarning;
                    goto ERROR1;
            }

            // ɾ���϶��������ĳ�Ա���¼
            strWarning = "";
            for (int i = 0; i < parent_cells.Count; i++)
            {
                Cell parent_cell = parent_cells[i];

                Debug.Assert(parent_cell.item != null, "");
                this.m_bChanged = true;

                for (int j = 0; j < parent_cell.item.MemberCells.Count; j++)
                {
                    Cell cell = parent_cell.item.MemberCells[j];

                    if (cell.item != null)
                    {
                        Debug.Assert(cell.item.Calculated == false, "");
                        cell.item.Deleted = true;
                        cell.item.Changed = true;
                    }
                }

                parent_cell.item.AfterMembersChanged();
            }

            // ɾ������ѡ���Ա����
            List<ItemBindingItem> temp_parent_items = new List<ItemBindingItem>();    // ȥ������
            for (int i = 0; i < member_cells.Count; i++)
            {
                Cell cell = member_cells[i];
                if (cell == null && cell.item == null)
                    continue;
                Debug.Assert(cell.IsMember == true, "");
                Debug.Assert(cell.item.Calculated == false, "");
                cell.item.Deleted = true;
                cell.item.Changed = true;

                ItemBindingItem parent_item = cell.item.ParentItem;
                Debug.Assert(parent_item != null, "");

                if (temp_parent_items.IndexOf(parent_item) == -1)
                    temp_parent_items.Add(parent_item);
            }
            for (int i = 0; i < temp_parent_items.Count; i++)
            {
                temp_parent_items[i].AfterMembersChanged();
            }

            this.Invalidate();
            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

        // �༭һ�������
        void menuItem_modifyCell_Click(object sender, EventArgs e)
        {
            string strError = "";

            List<Cell> selected_cells = this.SelectedCells;

            Cell cell = null;
            if (this.FocusObject != null && this.FocusObject is Cell)
                cell = (Cell)this.FocusObject;
            else if (selected_cells.Count > 0)
                cell = selected_cells[0];
            else
            {
                strError = "��δѡ��Ҫ�༭�ĸ���";
                goto ERROR1;
            }

            Debug.Assert(cell != null, "");

            // ��ʾ�༭����
            EditAreaEventArgs e1 = new EditAreaEventArgs();
            e1.Action = "get_state";
            this.EditArea(this, e1);
            if (e1.Result != "visible")
            {
                e1 = new EditAreaEventArgs();
                e1.Action = "open";
                this.EditArea(this, e1);
            }

            e1 = new EditAreaEventArgs();
            e1.Action = "focus";
            this.EditArea(this, e1);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �༭��
        void menuItem_modifyIssue_Click(object sender, EventArgs e)
        {
            string strError = "";
            // int nRet = 0;

            List<IssueBindingItem> selected_issues = this.SelectedIssues;
            if (selected_issues.Count == 0)
            {
                if (this.FocusObject is IssueBindingItem)
                {
                    selected_issues.Add((IssueBindingItem)this.FocusObject);
                }
                else
                {
                    strError = "��δѡ��Ҫ�޸ĵ���";
                    goto ERROR1;
                }
            }

            IssueBindingItem issue = selected_issues[0];

            if (String.IsNullOrEmpty(issue.PublishTime) == true)
            {
                strError = "�����ڲ��ܱ��޸�";
                goto ERROR1;
            }

            IssueDialog dlg = new IssueDialog();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.Tag = issue;
            dlg.CheckDup -= new CheckDupEventHandler(dlg_CheckDup);
            dlg.CheckDup += new CheckDupEventHandler(dlg_CheckDup);

            dlg.PublishTime = issue.PublishTime;
            dlg.Issue = issue.Issue;
            dlg.Zong = issue.Zong;
            dlg.Volume = issue.Volume;
            dlg.Comment = issue.Comment;

            dlg.StartPosition = FormStartPosition.CenterScreen;

        // REDO_INPUT:
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            /*
            List<IssueBindingItem> dup_issues = null;
            List<IssueBindingItem> warning_issues = null;
            string strWarning = "";

            // �Գ���ʱ����в���
            // parameters:
            //      exclude �����Ҫ�ų���TreeNode����
            // return:
            //      -1  error
            //      0   û����
            //      1   ��
            nRet = CheckPublishTimeDup(dlg.PublishTime,
                dlg.Issue,
                dlg.Zong,
                dlg.Volume,
                issue,
                out warning_issues,
                out strWarning,
                out dup_issues,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 1)
            {
                // ѡ�����ظ����ڸ��ӣ����ڲ����߹۲��ظ������
                Debug.Assert(dup_issue != null, "");
                if (dup_issue != null)
                {
                    this.ClearAllSelection();
                    dup_issue.Select(SelectAction.On);
                    this.EnsureVisible(dup_issue);  // ȷ��������Ұ
                    this.UpdateObject(dup_issue);
                    this.Update();
                }

                MessageBox.Show(this, "�޸ĺ���� " + strError + "\r\n���޸ġ�");
                goto REDO_INPUT;
            }
             * */


            issue.PublishTime = dlg.PublishTime;
            issue.Issue = dlg.Issue;
            issue.Zong = dlg.Zong;
            issue.Volume = dlg.Volume;
            issue.Comment = dlg.Comment;
            issue.Changed = true;

            // ���û���ˢ��һ����������
            // ���ܻ��׳��쳣
            issue.SetOperation(
                "lastModified",
                this.Operator,
                "");


            // �޸�ȫ���������ӵ�volume string��publish time
            string strNewVolumeString =
    VolumeInfo.BuildItemVolumeString(
    IssueUtil.GetYearPart(issue.PublishTime),
    issue.Issue,
issue.Zong,
issue.Volume);
            for (int i = 0; i < issue.Cells.Count; i++)
            {
                Cell cell = issue.Cells[i];
                if (cell == null)
                    continue;

                // ��Ա��
                if (cell.item == null && cell.ParentItem != null)
                {
                    cell.RefreshOutofIssue();
                    cell.ParentItem.AfterMembersChanged();
                    continue;
                }

                if (cell.item == null)
                    continue;

                if (cell.item.IsParent == true)
                    continue;   // ��ֱ���޸ĺ϶��ᡣ���ǣ��϶����ڵ��κθ��ӱ仯�������Զ����ܵ��϶���

                bool bChanged = false;
                if (cell.item.PublishTime != issue.PublishTime)
                {
                    cell.item.PublishTime = issue.PublishTime;
                    bChanged = true;
                }

                if (cell.item.Volume != strNewVolumeString)
                {
                    cell.item.Volume = strNewVolumeString;
                    bChanged = true;
                }

                if (bChanged == true)
                {
                    cell.RefreshOutofIssue();
                    cell.item.Changed = true;

                    if (cell.item.IsMember == true)
                        cell.item.ParentItem.AfterMembersChanged();
                }
            }

            // ѡ���޸Ĺ����ڸ���
            this.ClearAllSelection();
            issue.Select(SelectAction.On);
            this.EnsureVisible(issue);  // ȷ��������Ұ

            this.AfterWidthChanged(true);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ���ݵ�ǰѡ����ڵ������ͳ�Ƴ��������Ĳ���ģʽ
        IssueLayoutState GetMostSelectedLayoutState()
        {
            int nBindingCount = 0;
            int nAcceptionCount = 0;
            List<IssueBindingItem> selected_issues = this.SelectedIssues;
            for (int i = 0; i < selected_issues.Count; i++)
            {
                IssueBindingItem issue = selected_issues[i];
                if (issue.IssueLayoutState == IssueLayoutState.Accepting)
                    nAcceptionCount++;
                else if (issue.IssueLayoutState == IssueLayoutState.Binding)
                    nBindingCount++;
            }

            if (nAcceptionCount > nBindingCount)
                return IssueLayoutState.Accepting;
            return IssueLayoutState.Binding;
        }

        // ����Ԥ���
        // Ŀǰֻ�ܴ��� �ǵ����ֵ���
        // ���ѡ���˶�������ӣ����ڴ����ĩβ׷��һ���µ�Ԥ���
        // ���ѡ���˶������ڵĸ��ӣ����ڴ�λ�ò���һ���µ�Ԥ���
        // ���ͬһ������ѡ�������ߣ��������ڸ���ѡ��Ϊ��Ч
        // ���ѡ�����������ӣ�Ҳ��������ĸ��ӣ��򱾹�����Ч
        void menuItem_newCalulatedCells_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // ��ѡ��Χ���������еĸ��ӻ�������ӡ�
            List<Cell> selected_cells = this.SelectedCells;
            if (selected_cells.Count == 0)
            {
                strError = "��δѡ��Ҫ����Ԥ����Ĳο���";
                goto ERROR1;
            }

            List<GroupCell> group_cells = new List<GroupCell>();
            List<Cell> ingroup_cells = new List<Cell>();
            // ��ѡ
            for (int i = 0; i < selected_cells.Count; i++)
            {
                Cell cell = selected_cells[i];
                if (cell == null)
                    continue;
                if (cell is GroupCell)
                {
                    group_cells.Add((GroupCell)cell);
                    continue;
                }

                if (cell.item != null && cell.item.InGroup == true)
                {
                    if (cell.Container.IssueLayoutState != IssueLayoutState.Accepting)
                    {
                        strError = "ֻ�ܶ�λ�ڼǵ����ֵ����ڵĲο����ӽ�������Ԥ���Ĳ���";
                        goto ERROR1;
                    }
                    ingroup_cells.Add(cell);
                }
            }

            // ȥ���ظ�����β�������滻Ϊͷ������
            for (int i = 0; i < group_cells.Count; i++)
            {
                GroupCell group = group_cells[i];
                if (group.EndBracket == true)
                {
                    group_cells.RemoveAt(i);
                    GroupCell head = group.HeadGroupCell;
                    Debug.Assert(head != null, "");
                    if (head == null)
                        continue;
                    int idx = group_cells.IndexOf(head);
                    if (idx != i && idx != -1)
                    {
                        group_cells.Insert(i, head);
                        i--;
                        continue;
                    }
                }

                if (group.EndBracket == false)
                {
                    int idx = group_cells.IndexOf(group);
                    if (idx != i && idx != -1)
                    {
                        group_cells.RemoveAt(i);
                        i--;
                    }
                }
            }

            // ���һ��������ڶ����Ѿ���ѡ���ˣ��Ͳ�Ҫ����ͷ������
            for (int i = 0; i < group_cells.Count; i++)
            {
                GroupCell group = group_cells[i];
                Debug.Assert(group.EndBracket == false, "");
                List<Cell> members = group.MemberCells;
                for (int j = 0; j < ingroup_cells.Count; j++)
                {
                    Cell cell = ingroup_cells[j];
                    if (members.IndexOf(cell) != -1)
                    {
                        group_cells.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }

            if (ingroup_cells.Count == 0 && group_cells.Count == 0)
            {
                strError = "��ѡ���ĸ�����û�а�������ӻ������ڸ���";
                goto ERROR1;
            }

            List<Cell> new_cells = new List<Cell>();

            // �ȱ��������
            for (int i = 0; i < group_cells.Count; i++)
            {
                GroupCell group = group_cells[i];
                Debug.Assert(group.EndBracket == false, "");
                // �����ڲ����µĸ���(Ԥ�����)
                // parameters:
                //      nInsertPos  ����λ�á����Ϊ-1����ʾ������β��
                // return:
                //      ���ز����index(����issue.Cells�±�)
                nRet = group.InsertNewMemberCell(
                    -1,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                new_cells.Add(group.Container.GetCell(nRet));
            }

            // Ȼ��������ڸ���
            for (int i = 0; i < ingroup_cells.Count; i++)
            {
                Cell cell = ingroup_cells[i];
                Debug.Assert(cell != null, "");
                Debug.Assert(cell.item != null, "");

                GroupCell group = cell.item.GroupCell;
                Debug.Assert(group != null, "");
                int nInsertPos = group.MemberCells.IndexOf(cell);
                Debug.Assert(nInsertPos != -1, "");

                nRet = group.InsertNewMemberCell(
                    nInsertPos,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                new_cells.Add(group.Container.GetCell(nRet));
            }

            // ѡ���´�������Щ����
            this.ClearAllSelection();
            for (int i = 0; i < new_cells.Count; i++)
            {
                Cell cell = new_cells[i];
                cell.Select(SelectAction.On);
            }

            this.AfterWidthChanged(true);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �л����еĲ���
        void MenuItem_switchIssueLayout_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            ToolStripMenuItem menu = (ToolStripMenuItem)sender;

            IssueLayoutState layout = (IssueLayoutState)menu.Tag;

            List<IssueBindingItem> selected_issues = new List<IssueBindingItem>();
            selected_issues.AddRange(this.SelectedIssues);

            if (selected_issues.Count == 0)
            {
                strError = "��δѡ��Ҫ�л����ֵ��ڶ���";
                goto ERROR1;
            }

            List<IssueBindingItem> changed_issues = null;

            // �����л����еĲ���ģʽ
            nRet = SwitchIssueLayout(selected_issues,
                layout,
                out changed_issues,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // UpdateIssues(changed_issues);
            this.AfterWidthChanged(true);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �ѵ�ǰ������Ͻ��Χ�ĺ϶��ᵥԪȥ��
        // parameters:
        //      bRemoveCell �Ƿ�Ҫ�� this.Issues ����ȥ�����Cell
        //      bRemoveMemberCell �Ƿ�Ҫ�� this.Issues ����ȥ���϶���ԱCell
        int RemoveOutofBindingItems(
            ref List<ItemBindingItem> binding_items,
            Hashtable memberitems_table,
            bool bRemoveCell,
            bool bRemoveMemberCell,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (this.m_bHideLockedBindingCell == true
                && Global.IsGlobalUser(this.LibraryCodeList) == false)
            {
                for (int i = 0; i < binding_items.Count; i++)
                {
                    ItemBindingItem parent_item = binding_items[i];
                    List<ItemBindingItem> member_items = (List<ItemBindingItem>)memberitems_table[parent_item];

                    Debug.Assert(member_items != null, "");

                    if (member_items.Count == 0)
                        continue;

                    // ���һ���϶�������г�Ա,�����ǲ���(����һ��)�͵�ǰ�ɼ��������д�����ϵ?
                    // return:
                    //      -1  ����
                    //      0   û�н���
                    //      1   �н���
                    nRet = IsMemberCrossOrderGroup(parent_item,
                        member_items,
                        true,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    string strLibraryCode = Global.GetLibraryCode(parent_item.LocationString);

                    bool bLocked = (StringUtil.IsInList(strLibraryCode, this.LibraryCodeList) == false);
                    parent_item.Locked = bLocked;

                    // �϶�������ݴ������⣬�������ԱҲ���Ϳɼ������齻��ģ�ɾ���϶�������
                    if (bLocked == true
                        && nRet == 0)
                    {
                        if (bRemoveCell == true)
                            this.RemoveItem(parent_item, bRemoveMemberCell);
                        else
                        {
                            // ��ʱ��δ����Issue��������
                            this.m_hideitems.Add(parent_item);
                        }

                        binding_items.RemoveAt(i);
                        i--;
                    }
                }
            }

            return 0;
        }

        // ����Щ��ǰ���صĺ϶���ͳ�Ա����ͼ���°���һ��
        public int RelayoutHiddenBindingCell(out string strError)
        {
            strError = "";
            int nRet = 0;

            if (this.m_bHideLockedBindingCell == true
    && Global.IsGlobalUser(this.LibraryCodeList) == false)
            {
                Hashtable memberitems_table = new Hashtable();

                foreach (ItemBindingItem item in this.ParentItems)
                {
                    memberitems_table[item] = item.MemberItems;
                }

                // �ѵ�ǰ������Ͻ��Χ�ĺ϶��ᵥԪȥ��
                nRet = RemoveOutofBindingItems(
                    ref this.ParentItems,
                    memberitems_table,
                    true,
                    true,
                    out strError);
                if (nRet == -1)
                    return -1;

                return 0;
            }

            if (this.m_hideitems.Count == 0)
                return 0;

            // �������صĺ϶���
            List<ItemBindingItem> binding_items = new List<ItemBindingItem>();
            foreach (ItemBindingItem item in this.m_hideitems)
            {
                if (item.IsParent == true)
                {
                    if (this.ParentItems.IndexOf(item) != -1)
                        continue;   // ����Ѿ���ʾ�ˣ��Ͳ�Ҫ������
                    binding_items.Add(item);
                }
            }

            if (binding_items.Count == 0)
                return 0;

            {
                Hashtable memberitems_table = new Hashtable();
                string strWarning = "";
                // �����϶���������飬������Ա��������
                // parameters:
                //      parent_items    �϶���������顣�����Ķ������������������
                nRet = CreateMemberItemTable(
                    ref binding_items,
                    out memberitems_table,
                    ref strWarning,
                    out strError);
                if (nRet == -1)
                    return -1;

                // �ѵ�ǰ������Ͻ��Χ�ĺ϶��ᵥԪȥ��
                nRet = RemoveOutofBindingItems(
                    ref binding_items,
                    memberitems_table,
                    true,
                    true,
                    out strError);
                if (nRet == -1)
                    return -1;

                /*
                Hashtable placed_table = new Hashtable();   // �Ѿ�����Ϊ�϶����������Ź�λ�õĲ����

                // ���ź϶���Ա�����
                nRet = PlaceMemberCell(
                    ref binding_items,
                    memberitems_table,
                    ref placed_table,
                    out strError);
                if (nRet == -1)
                    return -1;
                 * */
                // ֻ���ź϶������
                nRet = PlaceParentItems(
                    ref binding_items,
                    memberitems_table,
                    out strError);
                if (nRet == -1)
                    return -1;

            }

            this.ParentItems.AddRange(binding_items);
            foreach (ItemBindingItem item in binding_items)
            {
                this.m_hideitems.Remove(item);
            }
            return 0;
        }

        public void RefreshLayout()
        {
            MenuItem_refreshIssueLayout_Click(null, null);
        }

        // ˢ��ȫ�����еĲ���
        void MenuItem_refreshIssueLayout_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;

                if (issue.IssueLayoutState == IssueLayoutState.Binding)
                    nRet = issue.ReLayoutBinding(out strError);
                else
                    nRet = issue.LayoutAccepting(out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            // UpdateIssues(changed_issues);
            this.AfterWidthChanged(true);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }


        void UpdateIssues(List<IssueBindingItem> issues)
        {
            List<CellBase> list = new List<CellBase>();
            for (int i = 0; i < issues.Count; i++)
            {
                IssueBindingItem issue = issues[i];
                list.Add((CellBase)issue);
            }

            this.UpdateObjects(list);
        }

        // ��ȫ����
        // �ڵ�ǰλ�ú������������ڣ�ֱ�����ö�����Χ��ĩβ
        // TODO: Ӧ���õ����ں������أ������λ��������
        void menuItem_newAllIssue_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            //  int nCreateCount = 0;
            List<IssueBindingItem> new_issues = new List<IssueBindingItem>();

            IssueBindingItem ref_issue = null;

            List<IssueBindingItem> ref_issues = this.SelectedIssues;
            for (int i = 0; i < ref_issues.Count; i++)
            {
                IssueBindingItem issue = ref_issues[i];
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;
                ref_issue = issue;
                break;
            }

            if (ref_issue == null)
                ref_issue = GetTailIssue();
            REDO:
            // �ҵ����һ�ڡ�����Ҳ��������ȳ��ֶԻ���ѯ�ʵ�һ��
            if (ref_issue == null)
            {
                string strStartDate = "";
                string strEndDate = "";
                // ��ÿ��õ���󶩹�ʱ�䷶Χ
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = GetMaxOrderRange(out strStartDate,
                    out strEndDate,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    strError = "��ǰû�ж�����Ϣ���޷�������ȫ����";
                    goto ERROR1;
                }

                // ���ֶԻ����������һ�ڵĲ���������ʱ��������Զ�̽����Ƽ�
                // ����Ҫ���ճ���������Ϣ���Ѿ���ȫ�Ķ�����¼����ա����������ְ�ԭ��������չ��ĵ�һ�ڳ���ʱ���Ƽ����������
                // ��ν���(������Ϣ��)�����������ɹ���װ������������
                // ��գ��ǰ���ض�����¼��<state>�����ӡ������ա��ַ���
                IssueDialog dlg = new IssueDialog();
                MainForm.SetControlFont(dlg, this.Font, false);
                dlg.Tag = null;
                dlg.CheckDup -= new CheckDupEventHandler(dlg_CheckDup);
                dlg.CheckDup += new CheckDupEventHandler(dlg_CheckDup);

                dlg.Text = "��ָ�����ڵ�����";
                dlg.PublishTime = strStartDate + "?";   // ��ö�����Χ���������
                dlg.EditComment = "��ǰ����ʱ�䷶ΧΪ " + strStartDate + "-" + strEndDate;   // ��ʾ���õĶ���ʱ�䷶Χ
                dlg.StartPosition = FormStartPosition.CenterScreen;

            REDO_INPUT:
                dlg.ShowDialog(this);

                if (dlg.DialogResult != DialogResult.OK)
                    return; // ������������

                // ���һ���������ʱ���Ƿ񳬹�����ʱ�䷶Χ?
                if (InOrderRange(dlg.PublishTime) == false)
                {
                    // TODO: �����ʾ��ǰ���õ�ʱ�䷶Χ?
                    MessageBox.Show(this, "��ָ�������ڳ���ʱ�� '" + dlg.PublishTime + "' ���ڵ�ǰ���õĶ���ʱ�䷶Χ�ڣ����������롣");
                    goto REDO_INPUT;
                }

                /*
                // ����?
                // ��publishTimeҪ���أ��Ժ�����ϵҪ���м����������
                IssueBindingItem dup_issue = null;
                // �Գ���ʱ����в���
                // parameters:
                //      exclude �����Ҫ�ų���TreeNode����
                // return:
                //      -1  error
                //      0   û����
                //      1   ��
                nRet = CheckPublishTimeDup(dlg.PublishTime,
                    null,
                    out dup_issue,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 1)
                {
                    // 
                    DialogResult dialog_result = MessageBox.Show(this,
            "�����趨�����ڳ������� '"+dlg.PublishTime+"' �Ѿ����ڣ��Ƿ�Ҫʹ������Ѿ����ڵ�����Ϊ�ο�������������������?\r\n\r\n(Yes: ��������; No: ���������������ڲ���; Cancel: ����������������)",
            "BindingControls",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
                    if (dialog_result == DialogResult.Cancel)
                        return;
                    if (dialog_result == DialogResult.No)
                        goto REDO_INPUT;
                }
                 * */

                IssueBindingItem new_issue = new IssueBindingItem();
                new_issue.Container = this;
                nRet = new_issue.Initial("<root />",
                    false,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                new_issue.PublishTime = dlg.PublishTime;
                new_issue.Issue = dlg.Issue;
                new_issue.Zong = dlg.Zong;
                new_issue.Volume = dlg.Volume;
                new_issue.Comment = dlg.Comment;
                new_issue.RefID = Guid.NewGuid().ToString();

                new_issue.Changed = true;
                new_issue.NewCreated = true;

                /*
                // ���û���ˢ��һ����������
                // ���ܻ��׳��쳣
                new_issue.SetOperation(
                    "create",
                    this.Operator,
                    "");
                 * */


                // ���뵽���ʵ�λ��?
                InsertIssueToIssues(new_issue);

                // Ϊ�����������ú�Layoutģʽ
                // �ֲ���Ϊ��������ĺ϶����Ѷ�
                nRet = SetNewIssueLayout(new_issue,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // nCreateCount++;
                new_issues.Add(new_issue);
                ref_issue = new_issue;
            }
            else
            {
                /*
                // ѡ�����һ��TreeNode
                Debug.Assert(this.TreeView.Nodes.Count != 0, "");
                TreeNode last_tree_node = this.TreeView.Nodes[this.TreeView.Nodes.Count - 1];

                if (this.TreeView.SelectedNode != last_tree_node)
                    this.TreeView.SelectedNode = last_tree_node;
                 * */
            }

            // int nWarningCount = 0;

            int nPreferredDelta = -1;

            // ����ѭ��������ȫ���ڵ�
            for (int i = 0; ; i++)
            {
                string strNextPublishTime = "";
                string strNextIssue = "";
                string strNextZong = "";
                string strNextVolume = "";

                {
                    int nIssueCount = 0;
                    // ���һ���ڵ�������
                    // return:
                    //      -1  ����
                    //      0   �޷����
                    //      1   ���
                    nRet = GetOneYearIssueCount(ref_issue.PublishTime,
                        out nIssueCount,
                        out strError);

                    if (nRet == 0 && i == 0)
                    {
                        ref_issue = null;
                        goto REDO;
                    }

                    // �ο����ں�
                    int nRefIssue = 0;
                    try
                    {
                        string strNumber = GetPureNumber(ref_issue.Issue);
                        nRefIssue = Convert.ToInt32(strNumber);
                    }
                    catch
                    {
                        nRefIssue = 0;
                    }


                    try
                    {
                        int nDelta = nPreferredDelta;
                        // Ԥ����һ�ڵĳ���ʱ��
                        // parameters:
                        //      strPublishTime  ��ǰ��һ�ڳ���ʱ��
                        //      nIssueCount һ���ڳ�������
                        strNextPublishTime = NextPublishTime(ref_issue.PublishTime,
                             nIssueCount,
                             ref nDelta);
                        // ��һ�ε��õ�ʱ���������
                        if (nPreferredDelta == -1)
                            nPreferredDelta = nDelta;
                    }
                    catch (Exception ex)
                    {
                        // 2009/2/8 
                        strError = "�ڻ������ '" + ref_issue.PublishTime + "' �ĺ�һ�ڳ�������ʱ��������: " + ex.Message;
                        goto ERROR1;
                    }

                    if (strNextPublishTime == "????????")
                        break;

                    // ���һ���������ʱ���Ƿ񳬹�����ʱ�䷶Χ?
                    if (InOrderRange(strNextPublishTime) == false)
                        break;  // �����������һ��


                    // �����Զ�������Ҫ֪��һ�����Ƿ���꣬����ͨ����ѯ�ɹ���Ϣ�õ�һ�������ĵ�����
                    if (nRefIssue >= nIssueCount
                        && nIssueCount > 0) // 2010/3/3 
                    {
                        // ������
                        strNextIssue = "1";
                        // 2010/3/16
                        // ���Ԥ�����һ�ڳ���ʱ�䲻�ǲο��ڵĺ�һ���ʱ�䣬����Ҫǿ���޸�
                        string strNextYear = IssueUtil.GetYearPart(strNextPublishTime);
                        string strRefYear = IssueUtil.GetYearPart(ref_issue.PublishTime);

                        // 2012/5/14
                        // ����ο���������ݵĸ���֮���Ѿ����꣬�򲻱�������
                        // ��ͼ�ҵ��ο���֮ǰ�ĵ�һ��
                        string strRefFirstYear = "";
                        IssueBindingItem year_first_issue = GetYearFirstIssue(ref_issue);
                        if (year_first_issue != null)
                        {
                            strRefFirstYear = IssueUtil.GetYearPart(year_first_issue.PublishTime);
                        }

                        if (string.Compare(strNextYear, strRefYear) <= 0
                            && strRefYear == strRefFirstYear/*�ο������ڵ�ȫ����ڲ�����*/)
                        {
                            strNextYear = DateTimeUtil.NextYear(strRefYear);
                            strNextPublishTime = strNextYear + "0101";

                            // 2015/1/30
                            // ���һ���������ʱ���Ƿ񳬹�����ʱ�䷶Χ?
                            if (InOrderRange(strNextPublishTime) == false)
                                break;  // �����������һ��
                        }
                    }
                    else
                    {
                        strNextIssue = (nRefIssue + 1).ToString();
                    }

                    strNextZong = IncreaseNumber(ref_issue.Zong);
                    if (nRefIssue >= nIssueCount && nIssueCount > 0)
                        strNextVolume = IncreaseNumber(ref_issue.Volume);
                    else
                        strNextVolume = ref_issue.Volume;
                }

                // ��publishTimeҪ���أ��Ժ�����ϵҪ���м����������
                List<IssueBindingItem> dup_issues = null;
                List<IssueBindingItem> warning_issues = null;
                string strWarning = "";

                // �Գ���ʱ����в���
                // parameters:
                //      exclude �����Ҫ�ų���TreeNode����
                // return:
                //      -1  error
                //      0   û����
                //      1   ��
                nRet = CheckPublishTimeDup(strNextPublishTime,
                    strNextIssue,
                    strNextZong,
                    "", // strNextVolume, ���ⲻ�����
                    ref_issue,
                    out warning_issues,
                    out strWarning,
                    out dup_issues,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 1)
                {
                    // this.TreeView.SelectedNode = dup_tree_node; // ��û����һ���������ѭ��
                    Debug.Assert(dup_issues.Count > 0, "");
                    ref_issue = dup_issues[0];  // �������������Ľ����ֵ��ظ�������Ϊ�µĲο�λ�ã���������
                    continue;
                }
                if (warning_issues.Count > 0)
                {
                    Debug.Assert(warning_issues.Count > 0, "");
                    ref_issue = warning_issues[0];  // �������������Ľ����ֵ��ظ�������Ϊ�µĲο�λ�ã���������
                    continue;
                }

                IssueBindingItem new_issue = new IssueBindingItem();
                new_issue.Container = this;
                nRet = new_issue.Initial("<root />",
                    false,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                new_issue.PublishTime = strNextPublishTime;
                new_issue.Issue = strNextIssue;
                new_issue.Zong = strNextZong;
                new_issue.Volume = strNextVolume;
                new_issue.RefID = Guid.NewGuid().ToString();

                new_issue.Changed = true;
                new_issue.NewCreated = true;

                /*
                // ���û���ˢ��һ����������
                // ���ܻ��׳��쳣
                new_issue.SetOperation(
                    "create",
                    this.Operator,
                    "");
                 * */

                // ���뵽���ʵ�λ��?
                InsertIssueToIssues(new_issue);

                // Ϊ�����������ú�Layoutģʽ
                // �ֲ���Ϊ��������ĺ϶����Ѷ�
                nRet = SetNewIssueLayout(new_issue,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // nCreateCount++;
                new_issues.Add(new_issue);
                /*
                // ѡ���²���Ľڵ�
                this.TreeView.SelectedNode = tree_node;
                 * */
                ref_issue = new_issue;
            }

            if (new_issues.Count > 0)
            {
                this.ClearAllSelection();
                for (int i = 0; i < new_issues.Count; i++)
                {
                    new_issues[i].Select(SelectAction.On);
                }
                // new_issue.Select(SelectAction.On);
                // ������ҪUpdateObject()��������Ϊ������Invalidate()��������
                this.AfterWidthChanged(true);   // content�߶ȸı�
                this.Update();
                // Application.DoEvents();
            }

            string strMessage = "";
            if (new_issues.Count == 0)
                strMessage = "û�������µ�����";
            else
                strMessage = "�������� " + new_issues.Count.ToString() + " ������";

            MessageBox.Show(this, strMessage);

            if (new_issues.Count > 0)
            {
                // TODO: �ƺ�SetScrollBars()û�б�Ҫ�ˣ�
                try
                {
                    SetScrollBars(ScrollBarMember.Both);
                }
                catch
                {
                }
                this.EnsureVisible(new_issues[new_issues.Count - 1]);  // ���һ��ɼ�

                string strLockedCellLibraryCodes = GetLockedCellLibraryCodes(new_issues);
                if (string.IsNullOrEmpty(strLockedCellLibraryCodes) == false)
                {
                    MessageBox.Show(this, "���棺���йݴ��벻�ڵ�ǰ�û���Ͻ��Χ��: \r\n\r\n" + strLockedCellLibraryCodes + "\r\n\r\nʹ������Щ�ݴ���ĸ����Ѿ���������״̬�����ڼ�¼�ύ�����ʱ����ܻ�������������ʹ��ȫ���û���¼�����²���");
                }
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ���泬����Ͻ��Χ�ĸ���
        string GetLockedCellLibraryCodes(List<IssueBindingItem> issues)
        {
            List<string> locked_librarycodes = new List<string>();
            for (int i = 0; i < issues.Count; i++)
            {
                IssueBindingItem issue = issues[i];

                if (issue.Virtual == true)
                    continue;

                for (int j = 0; j < issue.Cells.Count; j++)
                {
                    Cell cell = issue.Cells[j];
                    if (cell != null && cell.item != null)
                    {
                        if (cell.item.Locked == true)
                        {
                                locked_librarycodes.Add(Global.GetLibraryCode(cell.item.LocationString));
                        }
                    }
                }
            }

            if (locked_librarycodes.Count == 0)
                return "";

            StringUtil.RemoveDupNoSort(ref locked_librarycodes);
            return StringUtil.MakePathList(locked_librarycodes);
        }

        // ���Ҷ˿�ʼ�����һ�δ�������
        static string GetPureNumber(string strText)
        {
            string strValue = "";
            bool bStart = false;
            for (int i = strText.Length - 1; i >= 0; i--)
            {
                char ch = strText[i];
                if (bStart == false)
                {
                    if (ch >= '0' && ch <= '9')
                        bStart = true;
                }
                else
                {
                    if (!(ch >= '0' && ch <= '9'))
                        break;
                }

                if (bStart == true)
                    strValue = new string(ch, 1) + strValue;
            }

            return strValue;
        }

        IssueBindingItem GetTailIssue()
        {
            for (int i = this.Issues.Count - 1; i >= 0; i--)
            {
                IssueBindingItem issue = this.Issues[i];
                if (issue == null)
                    continue;
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;
                return issue;
            }

            return null;
        }

        // ����һ��
        // �ڵ�ǰλ�ú�������һ��
        void menuItem_newIssue_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // ��ѡ��Χ�������ڶ���

            // TODO: ������this.SeletedCells�е�ѡ��?

            List<IssueBindingItem> ref_issues = this.SelectedIssues;
            /*
            if (ref_issues.Count == 0)
            {
                strError = "��δѡ��Ҫ�����ڵ�(�ο�)�ڶ���";
                goto ERROR1;
            }
             * */
            // ����һ��ref_issues����
            for (int i = 0; i < ref_issues.Count; i++)
            {
                IssueBindingItem ref_issue = ref_issues[i];
                if (ref_issue == null
                    || String.IsNullOrEmpty(ref_issue.PublishTime) == true)
                {
                    ref_issues.RemoveAt(i);
                    i--;
                    continue;
                }
            }

            List<IssueBindingItem> new_issues = new List<IssueBindingItem>();   // �´�������
            if (ref_issues.Count > 0)
            {
                // �вο�����
                for (int i = 0; i < ref_issues.Count; i++)
                {
                    IssueBindingItem ref_issue = ref_issues[i];
                    if (ref_issue == null)
                    {
                        continue;
                    }

                    if (String.IsNullOrEmpty(ref_issue.PublishTime) == true)
                        continue;

                    IssueBindingItem new_issue = null;
                    // ����һ����(���)
                    nRet = NewOneIssue(
                        ref_issue,
                        false,
                        out new_issue,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    if (nRet == 0)
                        break;
                    new_issues.Add(new_issue);
                }
            }
            else
            {
                // ���û�вο�����
                Debug.Assert(ref_issues.Count == 0, "");

                IssueBindingItem ref_issue = null;

                // �ҵ����һ���ڶ�����Ϊ�ο�����
                /*
                if (this.Issues.Count > 0)
                {
                    for (int i = this.Issues.Count - 1; i >= 0; i--)
                    {
                        IssueBindingItem issue = this.Issues[i];
                        if (issue == null)
                            continue;
                        if (String.IsNullOrEmpty(issue.PublishTime) == true)
                            continue;
                        ref_issue = issue;
                        break;
                    }
                }
                 * */
                ref_issue = GetTailIssue();

                IssueBindingItem new_issue = null;
                // ����һ����(���)
                nRet = NewOneIssue(
                    ref_issue,
                    false,
                    out new_issue,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet != 0)
                {
                    Debug.Assert(new_issue != null, "");
                    new_issues.Add(new_issue);
                }
            }

            if (new_issues.Count > 0)
            {
                // ѡ���´�������
                this.ClearAllSelection();
                for (int i = 0; i < new_issues.Count; i++)
                {
                    IssueBindingItem issue = new_issues[i];
                    issue.Select(SelectAction.On);
                }
                this.AfterWidthChanged(true);   // content�߶ȸı�

                this.EnsureVisible(new_issues[0]);  // �ɼ�

                string strLockedCellLibraryCodes = GetLockedCellLibraryCodes(new_issues);
                if (string.IsNullOrEmpty(strLockedCellLibraryCodes) == false)
                {
                    MessageBox.Show(this, "���棺���йݴ��벻�ڵ�ǰ�û���Ͻ��Χ��: \r\n\r\n" + strLockedCellLibraryCodes + "\r\n\r\nʹ������Щ�ݴ���ĸ����Ѿ���������״̬�����ڼ�¼�ύ�����ʱ����ܻ�������������ʹ��ȫ���û���¼�����²���");
                }
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ѡ�������ڸ���
        public void SelectIssues(List<IssueBindingItem> issues,
            bool bEnsureVisible)
        {
            Debug.Assert(issues != null, "");
            this.ClearAllSelection();
            List<CellBase> cells = new List<CellBase>();
            for (int i = 0; i < issues.Count; i++)
            {
                cells.Add((CellBase)issues[i]);
                issues[i].Select(SelectAction.On);
            }

            this.UpdateObjects(cells);

            if (bEnsureVisible == true)
                this.EnsureVisible(issues[0]);  // ȷ��������Ұ

            // this.Update();
        }

        static int CompareDateString(string strPublishTime1,
            string strPublishTime2)
        {
            return string.Compare(strPublishTime1, strPublishTime2);
        }

        // ����һ����(���)
        // return:
        //      -1  ����
        //      0   ����
        //      1   �ɹ�
        int NewOneIssue(
            IssueBindingItem ref_issue,
            bool bUpdateDisplay,
            out IssueBindingItem new_issue,
            out string strError)
        {
            strError = "";
            new_issue = null;
            int nRet = 0;

            IssueDialog dlg = new IssueDialog();
            MainForm.SetControlFont(dlg, this.Font, false);
            dlg.Tag = null;
            dlg.CheckDup -= new CheckDupEventHandler(dlg_CheckDup);
            dlg.CheckDup += new CheckDupEventHandler(dlg_CheckDup);

            if (ref_issue == null)
            {
                // û�вο�����?
                // �ҵ���һ����û�мǵ��Ķ���ʱ�䷶Χ�Ŀ���
                // ��õ�һ��δ�ǵ��Ķ�����Χ����ʼʱ��
                string strFirstPublishTime = "";
                // return:
                //      -1  ����
                //      0   �޷����
                //      1   ���
                nRet = GetFirstUseablePublishTime(
                    out strFirstPublishTime,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 1)
                    dlg.PublishTime = strFirstPublishTime;

                if (String.IsNullOrEmpty(strFirstPublishTime) == false)
                {
                    int nIssueCount = 0;
                    // ���һ���ڵ�������
                    // return:
                    //      -1  ����
                    //      0   �޷����
                    //      1   ���
                    nRet = GetOneYearIssueCount(strFirstPublishTime,
                        out nIssueCount,
                        out strError);
                    if (nIssueCount > 0)
                        dlg.EditComment = "һ����� " + nIssueCount.ToString() + " ��";
                    else
                        dlg.EditComment = "�޷���֪��������������û�ж�Ӧ�Ķ�����Ϣ";
                }

                // TODO: Ԥ�����һ�ڵĵ����ں�
                // �㷨�ǣ�����һ����������۲���ʼʱ�䣬�ֲ��ڵ���ı���λ�ã������ں�
            }

            if (ref_issue != null)
            {
                // TODO: ������Զ�����

                int nIssueCount = 0;
                // ���һ���ڵ�������
                // return:
                //      -1  ����
                //      0   �޷����
                //      1   ���
                nRet = GetOneYearIssueCount(ref_issue.PublishTime,
                    out nIssueCount,
                    out strError);


                int nRefIssue = 0;
                try
                {
                    string strNumber = GetPureNumber(ref_issue.Issue);
                    nRefIssue = Convert.ToInt32(strNumber);
                }
                catch
                {
                    nRefIssue = 0;
                }

                bool bGuestNumbers = false;
                string strNextPublishTime = "";

                if (nRet == 0)
                {
                    string strFirstPublishTime = "";
                    // return:
                    //      -1  ����
                    //      0   �޷����
                    //      1   ���
                    nRet = GetFirstUseablePublishTime(
                        out strFirstPublishTime,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 1)
                    {
                        // TODO: Ҫ�Ƚ� ref_issue.PublishTime �� strFirstPublishTime
                        // ������ú��߸��󣬼��ɲ��ɡ������˲���
                        if (CompareDateString(ref_issue.PublishTime, strFirstPublishTime) < 0)
                        {
                            nRet = GetOneYearIssueCount(strFirstPublishTime,
                                out nIssueCount,
                                out strError);
                            strNextPublishTime = strFirstPublishTime;
                            bGuestNumbers = true;
                        }
                    }
                }
                else
                {

                    try
                    {
                        // Ԥ����һ�ڵĳ���ʱ��
                        // parameters:
                        //      strPublishTime  ��ǰ��һ�ڳ���ʱ��
                        //      nIssueCount һ���ڳ�������
                        strNextPublishTime = NextPublishTime(ref_issue.PublishTime,
                             nIssueCount);
                    }
                    catch (Exception ex)
                    {
                        // 2009/2/8 
                        strError = "�ڻ������ '" + ref_issue.PublishTime + "' �ĺ�һ�ڳ�������ʱ��������: " + ex.Message;
                        return -1;
                    }
                }

                dlg.PublishTime = strNextPublishTime;

                // �����Զ�������Ҫ֪��һ�����Ƿ���꣬����ͨ����ѯ�ɹ���Ϣ�õ�һ�������ĵ�����
                if (nRefIssue >= nIssueCount
                    && nIssueCount > 0) // 2010/3/3 
                {
                    // ������
                    dlg.Issue = "1";



                    // 2010/3/16
                    // ���Ԥ�����һ�ڳ���ʱ�䲻�ǲο��ڵĺ�һ���ʱ�䣬����Ҫǿ���޸�
                    string strNextYear = IssueUtil.GetYearPart(strNextPublishTime);
                    string strRefYear = IssueUtil.GetYearPart(ref_issue.PublishTime);

                    // 2012/5/14
                    // ����ο���������ݵĸ���֮���Ѿ����꣬�򲻱�������
                    // ��ͼ�ҵ��ο���֮ǰ�ĵ�һ��
                    string strRefFirstYear = "";
                    IssueBindingItem year_first_issue = GetYearFirstIssue(ref_issue);
                    if (year_first_issue != null)
                    {
                        strRefFirstYear = IssueUtil.GetYearPart(year_first_issue.PublishTime);
                    }

                    if (string.Compare(strNextYear, strRefYear) <= 0
                        && strRefYear == strRefFirstYear/*�ο������ڵ�ȫ����ڲ�����*/)
                    {
                        strNextYear = DateTimeUtil.NextYear(strRefYear);
                        strNextPublishTime = strNextYear + "0101";
                        dlg.PublishTime = strNextPublishTime;
                    }
                }
                else
                {
                    dlg.Issue = (nRefIssue + 1).ToString();
                }

                dlg.Zong = IncreaseNumber(ref_issue.Zong);
                if (nRefIssue >= nIssueCount && nIssueCount > 0)
                    dlg.Volume = IncreaseNumber(ref_issue.Volume);
                else
                    dlg.Volume = ref_issue.Volume;

                if (nIssueCount > 0)
                    dlg.EditComment = "һ����� " + nIssueCount.ToString() + " ��";
                else
                    dlg.EditComment = "�޷���֪һ������������û�ж�Ӧ�Ķ�����Ϣ";

                if (bGuestNumbers == true)
                {
                    if (String.IsNullOrEmpty(dlg.Issue) == false)
                        dlg.Issue += "?";
                    if (String.IsNullOrEmpty(dlg.Volume) == false)
                        dlg.Volume += "?";
                    if (String.IsNullOrEmpty(dlg.Zong) == false)
                        dlg.Zong += "?";
                }
            }

            dlg.StartPosition = FormStartPosition.CenterScreen;

        // REDO_INPUT:
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return 0;
            /*
            // ��publishTimeҪ���أ��Ժ�����ϵҪ���м����������
            IssueBindingItem dup_issue = null;
            // �Գ���ʱ����в���
            // parameters:
            //      exclude �����Ҫ�ų���TreeNode����
            // return:
            //      -1  error
            //      0   û����
            //      1   ��
            nRet = CheckPublishTimeDup(dlg.PublishTime,
                null,
                out dup_issue,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 1)
            {
                // ѡ�����ظ����ڸ��ӣ����ڲ����߹۲��ظ������
                Debug.Assert(dup_issue != null, "");
                if (dup_issue != null)
                {
                    this.ClearAllSelection();
                    dup_issue.Select(SelectAction.On);
                    this.EnsureVisible(dup_issue);  // ȷ��������Ұ
                    this.UpdateObject(dup_issue);
                    this.Update();
                }

                MessageBox.Show(this, "������������ " + strError + "\r\n���޸ġ�");
                goto REDO_INPUT;
            }
             * */

            new_issue = new IssueBindingItem();
            new_issue.Container = this;
            nRet = new_issue.Initial("<root />",
                false, //?
                out strError);
            if (nRet == -1)
                return -1;

            new_issue.PublishTime = dlg.PublishTime;
            new_issue.Issue = dlg.Issue;
            new_issue.Zong = dlg.Zong;
            new_issue.Volume = dlg.Volume;
            new_issue.Comment = dlg.Comment;
            new_issue.RefID = Guid.NewGuid().ToString();
            // TODO: ������������ڵ����κš������������ղ�����κ�?

            new_issue.Changed = true;
            new_issue.NewCreated = true;

            /*
            // ���û���ˢ��һ����������
            // ���ܻ��׳��쳣
            new_issue.SetOperation(
                "create",
                this.Operator,
                "");
             * */

            // ���뵽���ʵ�λ��?
            InsertIssueToIssues(new_issue);

            // Ϊ�����������ú�Layoutģʽ
            // �ֲ���Ϊ��������ĺ϶����Ѷ�
            nRet = SetNewIssueLayout(new_issue,
                out strError);
            if (nRet == -1)
                return -1;

            // ѡ���²���Ľڵ�
            if (bUpdateDisplay == true)
            {
                this.ClearAllSelection();
                new_issue.Select(SelectAction.On);
                // ������ҪUpdateObject()��������Ϊ������Invalidate()��������
                this.AfterWidthChanged(true);   // content�߶ȸı�
            }
            return 1;
        }

        // �ҵ��ο���ǰ�������һ�ڵĽڵ�
        IssueBindingItem GetYearFirstIssue(IssueBindingItem ref_issue)
        {
            IssueBindingItem first = null;
            IssueBindingItem tail = null;
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;
                // TODO: ע���ںŲ����������
                if (issue.Issue == "1")
                    first = issue;
                if (issue == ref_issue)
                {
                    tail = issue;
                    break;
                }
            }

            if (tail == null)
                return null;

            return first; 
        }

        // ����
        void dlg_CheckDup(object sender, CheckDupEventArgs e)
        {
            IssueDialog dialog = (IssueDialog)sender;
            List<IssueBindingItem> warning_issues = null;
            List<IssueBindingItem> dup_issues = null;
            string strDup = "";
            string strWarning = "";
            int nRet = this.CheckPublishTimeDup(
                e.PublishTime,
                e.Issue,
                e.Zong,
                e.Volume,
                (IssueBindingItem)dialog.Tag,
                out warning_issues,
                out strWarning,
                out dup_issues,
                out strDup);
            e.WarningInfo = strWarning;
            e.WarningIssues = warning_issues;
            e.DupInfo = strDup;
            e.DupIssues = dup_issues;

            if (e.EnsureVisible == true)
            {
                this.ClearAllSelection();
                if (dup_issues.Count > 0)
                {
                    this.SelectIssues(dup_issues, true);
                    return;
                }

                if (warning_issues.Count > 0)
                {
                    this.SelectIssues(warning_issues, true);
                }
            }
        }

        // �����ǰ����ѡ��
        // TODO: ���ҵ����ƵĶ��д��룬��Ϊ��������
        public void ClearAllSelection()
        {
            List<CellBase> objects = new List<CellBase>();
            this.ClearAllSubSelected(ref objects, 100);
            if (objects.Count >= 100)
                this.Invalidate();
            else
            {
                // ���������Ļ������
                UpdateObjects(objects);
            }
        }

        #region --- ���������йصĺ��� ---

        // ��ÿ��õ���󶩹�ʱ�䷶Χ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetMaxOrderRange(out string strStartDate,
            out string strEndDate,
            out string strError)
        {
            strStartDate = "";
            strEndDate = "";
            strError = "";

            if (this.GetOrderInfo == null)
                return 0;

            GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
            e1.BiblioRecPath = "";
            e1.PublishTime = "*";
            e1.LibraryCodeList = this.LibraryCodeList;
            this.GetOrderInfo(this, e1);
            if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
            {
                strError = e1.ErrorInfo;
                return -1;
            }

            if (e1.OrderXmls.Count == 0)
                return 0;

            for (int i = 0; i < e1.OrderXmls.Count; i++)
            {
                string strXml = e1.OrderXmls[i];

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "����XMLװ��DOMʱ��������: " + ex.Message;
                    return -1;
                }

                string strRange = DomUtil.GetElementText(dom.DocumentElement,
                    "range");

                string strIssueCount = DomUtil.GetElementText(dom.DocumentElement,
                    "issueCount");

                int nIssueCount = 0;
                try
                {
                    nIssueCount = Convert.ToInt32(strIssueCount);
                }
                catch
                {
                    continue;
                }

                int nRet = strRange.IndexOf("-");
                if (nRet == -1)
                {
                    strError = "ʱ�䷶Χ '" + strRange + "' ��ʽ����ȱ��-";
                    return -1;
                }

                string strStart = strRange.Substring(0, nRet).Trim();
                string strEnd = strRange.Substring(nRet + 1).Trim();

                if (strStart.Length != 8)
                {
                    strError = "ʱ�䷶Χ '" + strRange + "' ��ʽ������߲����ַ�����Ϊ8";
                    return -1;
                }
                if (strEnd.Length != 8)
                {
                    strError = "ʱ�䷶Χ '" + strRange + "' ��ʽ�����ұ߲����ַ�����Ϊ8";
                    return -1;
                }

                if (strStartDate == "")
                    strStartDate = strStart;
                else
                {
                    if (String.Compare(strStartDate, strStart) > 0)
                        strStartDate = strStart;
                }

                if (strEndDate == "")
                    strEndDate = strEnd;
                else
                {
                    if (String.Compare(strEndDate, strEnd) < 0)
                        strEndDate = strEnd;
                }
            }

            if (strStartDate == "")
            {
                Debug.Assert(strEndDate == "", "");
                return 0;
            }

            return 1;
        }

        // ���һ������ʱ���Ƿ����Ѿ������ķ�Χ��
        bool InOrderRange(string strPublishTime)
        {
            if (this.GetOrderInfo == null)
                return false;

            GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
            e1.BiblioRecPath = "";
            e1.PublishTime = strPublishTime;
            e1.LibraryCodeList = this.LibraryCodeList;
            this.GetOrderInfo(this, e1);
            if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
                return false;

            if (e1.OrderXmls.Count == 0)
                return false;

            return true;
        }

        // ��һ���µ�Issue�������this.Issues�ʵ���λ��
        // ע�⣬����ǰ����δ����this.Issues
        void InsertIssueToIssues(IssueBindingItem issueInsert)
        {
            int nFreeIndex = -1;    // ����������λ��
            int nInsertIndex = -1;
            string strPublishTime = issueInsert.PublishTime;
            string strPrevPublishTime = "";
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                if (issue == null)
                {
                    Debug.Assert(false, "");
                    continue;
                }
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                {
                    nFreeIndex = i;
                    continue;
                }

                if (issue == issueInsert)
                {
                    throw new Exception("Ҫ��������ڵ��ú���ǰ�Ѿ�������������");
                }

                if (String.Compare(strPublishTime, strPrevPublishTime) >= 0
    && String.Compare(strPublishTime, issue.PublishTime) < 0)
                    nInsertIndex = i;

                strPrevPublishTime = issue.PublishTime;
            }

            if (nInsertIndex == -1)
            {
                // ������������֮ǰ
                if (nFreeIndex != -1)
                {
                    Debug.Assert(this.Issues.Count > 0, "");
                    if (nFreeIndex == this.Issues.Count - 1)
                    {
                        this.Issues.Insert(nFreeIndex, issueInsert);
                        return;
                    }
                }

                this.Issues.Add(issueInsert);
            }
            else
                this.Issues.Insert(nInsertIndex, issueInsert);
        }

        // Ϊ�����������ú�Layoutģʽ
        int SetNewIssueLayout(IssueBindingItem issue,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            IssueBindingItem prev_issue = null;
            IssueBindingItem next_issue = null;
            int nLineNo = this.Issues.IndexOf(issue);
            if (nLineNo == -1)
            {
                Debug.Assert(false, "");
                strError = "issue not found in this.Issues";
                return -1;
            }

            if (nLineNo > 0)
            {
                prev_issue = this.Issues[nLineNo - 1];
                // �ų�������
                if (String.IsNullOrEmpty(prev_issue.PublishTime) == true)
                    prev_issue = null;
            }

            if (nLineNo < this.Issues.Count - 1)
            {
                next_issue = this.Issues[nLineNo + 1];
                // �ų�������
                if (String.IsNullOrEmpty(next_issue.PublishTime) == true)
                    next_issue = null;
            }

            // �ڴ˷�Χ�ڣ����������ȡ������Ϣ��������ŤתΪ�ӹ�Ͻ��Χ��ȡ
            bool bOld = m_bForceNarrowRange;
            m_bForceNarrowRange = true;
            try
            {

                bool bCross = IsIssueInExistingBoundRange(issue);
                if (bCross == true
                    || (prev_issue != null && prev_issue.IssueLayoutState == IssueLayoutState.Binding)
                    || (prev_issue != null && prev_issue.IssueLayoutState == IssueLayoutState.Binding)
                    )
                {
                    // �����ǰ��Ϊ�����˺϶�������������ǰһ���ڻ��ߺ�һ����������һ����Binding Layout��
                    // ��ô������ΪBindingLayout
                    nRet = issue.ReLayoutBinding(out strError);
                    if (nRet == -1)
                        return -1;
                    issue.IssueLayoutState = IssueLayoutState.Binding;
                    return 0;
                }

                // ��������ΪAccepting Layout
                nRet = issue.LayoutAccepting(out strError);
                if (nRet == -1)
                    return -1;
                issue.IssueLayoutState = IssueLayoutState.Accepting;
                return 0;

            }
            finally
            {
                m_bForceNarrowRange = bOld;
            }
        }

        // �ҵ�һ���϶������Binding�����µ��Ѿ����ɵ��кš�ע�⣬��˫���������
        internal int FindExistBoundCol(ItemBindingItem parent_item,
            IssueBindingItem exclude_issue)
        {
            Debug.Assert(parent_item != null, "");
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                if (issue == null)
                {
                    Debug.Assert(false, "");
                    continue;
                }
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;
                if (issue == exclude_issue)
                    continue;
                if (issue.IssueLayoutState == IssueLayoutState.Accepting)
                    continue;
                int nCol = issue.IndexOfItem(parent_item);
                if (nCol != -1)
                    return nCol;
                for (int j = 0; j < parent_item.MemberCells.Count; j++)
                {
                    Cell cell = parent_item.MemberCells[j];
                    if (cell == null)
                        continue;
                    nCol = issue.IndexOfCell(cell);
                    if (nCol != -1)
                    {
                        Debug.Assert(nCol != 0, "��Ա���Ӳ�Ӧ��0�г���");
                        return nCol - 1;
                    }
                }
            }

            return -1;  // not found
        }


        // ���һ���ڶ������������λ���Ƿ�Խ�����еĺ϶�ʱ�䷶Χ
        // �õ���Щ����Խ�ĺ϶������
        // parameters:
        //      bOnlyDetect     �Ƿ������⣬�����ؾ����parent item?
        //      parent_items    ���ر���Խ�ĺ϶������
        internal int GetCrossBoundRange(IssueBindingItem issueTest,
            bool bOnlyDetect,
            out List<ItemAndCol> cross_infos)
        {
            cross_infos = new List<ItemAndCol>();

            // �����϶����������
            for (int i = 0; i < this.ParentItems.Count; i++)
            {
                ItemBindingItem parent_item = this.ParentItems[i];

                IssueBindingItem issue = parent_item.Container; // ��װ���������
                Debug.Assert(issue != null, "�϶���� Container ��Ӧ��Ϊ�ա���ȷ�Ĵ���ʽ�ǰ������Ĳ���������ڹ�Ͻ��Χ");

                // �ҵ��к�
                int nStartLineNo = this.Issues.IndexOf(issue);
                Debug.Assert(nStartLineNo != -1, "");
                if (nStartLineNo == -1)
                    continue;

                /*
                int nCol = issue.IndexOfItem(parent_item);
                Debug.Assert(nCol != -1, "");
                 * */

                // ������ֱ����������ٸ���
                int nIssueCount = 0;
                if (parent_item.MemberCells.Count == 0)
                    nIssueCount = 1;
                else
                {
                    // TODO: Ҫ��֤item.MemberCells�����ж����������
                    IssueBindingItem tail_issue = parent_item.MemberCells[parent_item.MemberCells.Count - 1].Container;// item.MemberItems[item.MemberItems.Count - 1].Container;
                    Debug.Assert(tail_issue != null, "");
                    // �ҵ��к�
                    int nTailLineNo = this.Issues.IndexOf(tail_issue);
                    Debug.Assert(nTailLineNo != -1, "");
                    if (nTailLineNo == -1)
                        continue;

                    nIssueCount = nTailLineNo - nStartLineNo + 1;
                }

                int nTestLineNo = this.Issues.IndexOf(issueTest);
                if (nTestLineNo == -1)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                if (nTestLineNo >= nStartLineNo && nTestLineNo < nStartLineNo + nIssueCount)
                {
                    if (bOnlyDetect == true)
                        return 1;
                    ItemAndCol info = new ItemAndCol();
                    info.item = parent_item;
                    cross_infos.Add(info);

                    // �ҵ���ǰ�����õ��кš�˫������
                    info.Index = FindExistBoundCol(parent_item,
                        issueTest);
                }
            }

            return cross_infos.Count;
        }

                // ���һ���ڶ������������λ���Ƿ�Խ�����еĺ϶�ʱ�䷶Χ
        internal bool IsIssueInExistingBoundRange(IssueBindingItem issueTest)
        {
            List<ItemAndCol> infos = null;
            if (this.GetCrossBoundRange(issueTest,
                true,
                out infos) > 0)
                return true;
            return false;
        }

#if NOOOOOOOOOOOOOOO
        // ���һ���ڶ������������λ���Ƿ�Խ�����еĺ϶�ʱ�䷶Χ
        internal bool IsIssueInExistingBoundRange(IssueBindingItem issueTest)
        {
            // �����϶����������
            for (int i = 0; i < this.ParentItems.Count; i++)
            {
                ItemBindingItem parent_item = this.ParentItems[i];

                IssueBindingItem issue = parent_item.Container; // ��װ���������
                Debug.Assert(issue != null, "");

                // �ҵ��к�
                int nStartLineNo = this.Issues.IndexOf(issue);
                Debug.Assert(nStartLineNo != -1, "");
                if (nStartLineNo == -1)
                    continue;

                /*
                int nCol = issue.IndexOfItem(parent_item);
                Debug.Assert(nCol != -1, "");
                 * */

                // ������ֱ����������ٸ���
                int nIssueCount = 0;
                if (parent_item.MemberCells.Count == 0)
                    nIssueCount = 1;
                else
                {
                    // TODO: Ҫ��֤item.MemberCells�����ж����������
                    IssueBindingItem tail_issue = parent_item.MemberCells[parent_item.MemberCells.Count - 1].Container;// item.MemberItems[item.MemberItems.Count - 1].Container;
                    Debug.Assert(tail_issue != null, "");
                    // �ҵ��к�
                    int nTailLineNo = this.Issues.IndexOf(tail_issue);
                    Debug.Assert(nTailLineNo != -1, "");
                    if (nTailLineNo == -1)
                        continue;

                    nIssueCount = nTailLineNo - nStartLineNo + 1;
                }

                int nTestLineNo = this.Issues.IndexOf(issueTest);
                if (nTestLineNo == -1)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                if (nTestLineNo >= nStartLineNo && nTestLineNo <= nStartLineNo + nIssueCount)
                    return true;
            }

            return false;
        }
#endif

        // �Գ���ʱ�䡢�����ںš���Ž��в���
        // parameters:
        //      exclude �����Ҫ�ų���TreeNode����
        // return:
        //      -1  error
        //      0   û����
        //      1   ��
        int CheckPublishTimeDup(string strPublishTime,
            string strIssue,
            string strZong,
            string strVolume,
            IssueBindingItem exclude,
            out List<IssueBindingItem> warning_issues,
            out string strWarning,
            out List<IssueBindingItem> dup_issues,
            out string strError)
        {
            strWarning = "";
            strError = "";
            warning_issues = new List<IssueBindingItem>();
            dup_issues = new List<IssueBindingItem>();

            if (String.IsNullOrEmpty(strPublishTime) == true)
            {
                strError = "����ʱ�䲻��Ϊ��";
                return -1;
            }

            string strCurYear = IssueUtil.GetYearPart(strPublishTime);

            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];

                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;

                if (issue == exclude)
                    continue;

                // ������ʱ��
                if (issue.PublishTime == strPublishTime)
                {
                    if (String.IsNullOrEmpty(strError) == false)
                        strError += ";\r\n";
                    strError += "����ʱ�� '" + strPublishTime + "' ��λ�� " + (i + 1).ToString() + " (��1����)�����ظ���";
                    dup_issues.Add(issue);
                }

                // �ڵ��귶Χ�ڼ�鵱���ںš��������귶Χ�ڼ����
                {
                    string strYear = IssueUtil.GetYearPart(issue.PublishTime);
                    if (strYear == strCurYear)
                    {
                        if (strIssue == issue.Issue
                        && String.IsNullOrEmpty(strIssue) == false)
                        {
                            if (String.IsNullOrEmpty(strWarning) == false)
                                strWarning += ";\r\n";
                            strWarning = "�ں� '" + strIssue + "' ��λ�� " + (i + 1).ToString() + " (��1����)�����ظ���";
                            warning_issues.Add(issue);
                        }
                    }
                    else if (strYear != strCurYear)
                    {
                        if (strVolume == issue.Volume
                            && String.IsNullOrEmpty(strVolume) == false)
                        {
                            if (String.IsNullOrEmpty(strWarning) == false)
                                strWarning += ";\r\n";
                            strWarning = "��� '" + strVolume + "' ��λ�� " + (i + 1).ToString() + " (��1����)�����ظ���";
                            warning_issues.Add(issue);
                        }
                    }
                }


                // ������ں�
                if (String.IsNullOrEmpty(strZong) == false)
                {
                    if (strZong == issue.Zong)
                    {
                        if (String.IsNullOrEmpty(strWarning) == false)
                            strWarning += ";\r\n";
                        strWarning = "���ں� '" + strZong + "' ��λ�� " + (i + 1).ToString() + " (��1����)�����ظ���";
                        warning_issues.Add(issue);
                    }
                }
            }

            if (dup_issues.Count > 0)
                return 1;

            return 0;
        }

        static string IncreaseNumber(string strText)
        {
            string strNumber = GetPureNumber(strText);

            int v = 0;
            try
            {
                v = Convert.ToInt32(strNumber);
            }
            catch
            {
                return "";  // ������ز鵽�Լ�   // ����ʧ��    strNumber
            }
            return (v + 1).ToString();
        }

        static string CanonicalizeLong8TimeString(string strPublishTime)
        {
            if (strPublishTime.Length == 4)
                return strPublishTime + "0101";
            if (strPublishTime.Length == 6)
                return strPublishTime + "01";
            return strPublishTime;
        }

        class PartOfMonth
        {
            public string StartDate = "";
            public string EndDate = "";

            public PartOfMonth(string strStartDate, string strEndDate)
            {
                Debug.Assert(strStartDate.Length == 8, "");
                Debug.Assert(strEndDate.Length == 8, "");
                this.StartDate = strStartDate;
                this.EndDate = strEndDate;

                // TODO: ��֤�������ڶ�Ҫ��ͬһ��������
            }

            public PartOfMonth(DateTime start, DateTime end)
            {
                this.StartDate = DateTimeUtil.DateTimeToString8(start);
                this.EndDate = DateTimeUtil.DateTimeToString8(end);
                // TODO: ��֤�������ڶ�Ҫ��ͬһ��������
            }
        }

        // �����ɲ�λ�ж�λһ�����ڣ����õ��ڲ������ڵ�ƫ���� delta
        static void LocationPart(List<PartOfMonth> parts,
            string strPublishTime,
            out int index,
            out int delta)
        {
            index = 0;
            delta = 0;
            foreach (PartOfMonth part in parts)
            {
                if (string.Compare(strPublishTime, part.StartDate) >= 0
                    && string.Compare(strPublishTime, part.EndDate) <= 0)
                {
                    DateTime range_start = DateTimeUtil.Long8ToDateTime(part.StartDate);
                    DateTime publish_time = DateTimeUtil.Long8ToDateTime(strPublishTime);
                    delta = (int)(publish_time - range_start).TotalDays;
                    return;
                }

                index++;
            }

            index = -1; // not found
            return;
        }

        // parameters:
        //      strEndTime  �޶����������������
        static string AddDays(string strPublishTime, int days, string strEndTime)
        {
            string strResult = DateTimeUtil.DateTimeToString8(
                DateTimeUtil.Long8ToDateTime(strPublishTime).AddDays(days)
                );
            if (string.Compare(strResult, strEndTime) > 0)
                return strEndTime;
            return strResult;
        }

        static string AddDays(string strPublishTime, int days)
        {
            return DateTimeUtil.DateTimeToString8(
                DateTimeUtil.Long8ToDateTime(strPublishTime).AddDays(days)
                );
        }

        static List<PartOfMonth> GetMonthParts(string strPublishTime, int nCount)
        {
            return GetMonthParts(DateTimeUtil.Long8ToDateTime(strPublishTime), nCount);
        }

        // ��һ���¾��Ȼ���Ϊ��������
        static List<PartOfMonth> GetMonthParts(DateTime time, int nCount)
        {
            List<PartOfMonth> results = new List<PartOfMonth>();
            // DateTime time = DateTimeUtil.Long8ToDateTime(strPublishTime);
            if (nCount == 1)
            {
                results.Add(
                    new PartOfMonth(
                    new DateTime(time.Year, time.Month, 1),
                    new DateTime(time.Year, time.Month, 1).AddMonths(1).AddDays(-1)
                    )
                    );
                return results;
            }
            if (nCount == 2)
            {
                // 1-15
                results.Add(
                    new PartOfMonth(
                    new DateTime(time.Year, time.Month, 1),
                    new DateTime(time.Year, time.Month, 15)
                    )
                    ); 
                // 16-end
                results.Add(
                    new PartOfMonth(
                    new DateTime(time.Year, time.Month, 16),
                    new DateTime(time.Year, time.Month, 1).AddMonths(1).AddDays(-1)
                    )
                    );
                return results;
            }
            if (nCount == 3)
            {
                // 1-10
                results.Add(
                    new PartOfMonth(
                    new DateTime(time.Year, time.Month, 1),
                    new DateTime(time.Year, time.Month, 10)
                    )
                    );
                // 11-20
                results.Add(
                    new PartOfMonth(
                    new DateTime(time.Year, time.Month, 11),
                    new DateTime(time.Year, time.Month, 20)
                    )
                    );
                // 21-end
                results.Add(
                    new PartOfMonth(
                    new DateTime(time.Year, time.Month, 21),
                    new DateTime(time.Year, time.Month, 1).AddMonths(1).AddDays(-1)
                    )
                    );
                return results;
            }
            if (nCount == 4)
            {
                // 1-7
                results.Add(
                    new PartOfMonth(
                    new DateTime(time.Year, time.Month, 1),
                    new DateTime(time.Year, time.Month, 7)
                    )
                    );
                // 8-15
                results.Add(
                    new PartOfMonth(
                    new DateTime(time.Year, time.Month, 8),
                    new DateTime(time.Year, time.Month, 15)
                    )
                    );
                // 16-22
                results.Add(
                    new PartOfMonth(
                    new DateTime(time.Year, time.Month, 16),
                    new DateTime(time.Year, time.Month, 22)
                    )
                    );
                // 23-end
                results.Add(
                    new PartOfMonth(
                    new DateTime(time.Year, time.Month, 23),
                    new DateTime(time.Year, time.Month, 1).AddMonths(1).AddDays(-1)
                    )
                    );
                return results;
            }

            throw new Exception("�ݲ�֧��һ������ "+nCount.ToString()+" ���ָ�");
        }

        // ��װ��İ汾
        public static string NextPublishTime(string strPublishTime,
            int nIssueCount)
        {
            int nPreferredDelta = -1;
            return NextPublishTime(strPublishTime,
                nIssueCount,
                ref nPreferredDelta);
        }

        // Ԥ����һ�ڵĳ���ʱ��
        // exception:
        //      ������strPublishTimeΪ�����ܵ����ڶ��׳��쳣
        // parameters:
        //      strPublishTime  ��ǰ��һ�ڳ���ʱ��
        //      nIssueCount һ���ڳ�������
        //      nDelta  �Ƽ�������ƫ������������ǰ���Ϊ -1����ʾ��ʹ��������������ú󷵻ر��εõ���ƫ��
        public static string NextPublishTime(string strPublishTime,
            int nIssueCount,
            ref int nPreferredDelta)
        {
            strPublishTime = CanonicalizeLong8TimeString(strPublishTime);

            DateTime start = DateTimeUtil.Long8ToDateTime(strPublishTime);

            int nCount = 0;

            // һ��һ��
            if (nIssueCount == 1)
            {
                return DateTimeUtil.DateTimeToString8(DateTimeUtil.NextYear(start));
            }

            // һ������
            else if (nIssueCount == 2)
            {
                // 6�����Ժ��ͬ��
                for (int i = 0; i < 6; i++)
                {
                    start = DateTimeUtil.NextMonth(start);
                }

                return DateTimeUtil.DateTimeToString8(start);
            }

            // һ������
            else if (nIssueCount == 3)
            {
                // 4�����Ժ��ͬ��
                for (int i = 0; i < 4; i++)
                {
                    start = DateTimeUtil.NextMonth(start);
                }

                return DateTimeUtil.DateTimeToString8(start);
            }

            // һ��4��
            else if (nIssueCount == 4)
            {
                // 3�����Ժ��ͬ��
                for (int i = 0; i < 3; i++)
                {
                    start = DateTimeUtil.NextMonth(start);
                }

                return DateTimeUtil.DateTimeToString8(start);
            }

            // һ��5�� ��һ��6�ڴ���취һ��
            // һ��6��
            else if (nIssueCount == 5 || nIssueCount == 6)
            {
                // 
                // 2�����Ժ��ͬ��
                for (int i = 0; i < 2; i++)
                {
                    start = DateTimeUtil.NextMonth(start);
                }

                return DateTimeUtil.DateTimeToString8(start);
            }

            // һ��7/8/9/10/11�� ��һ��12�ڴ���취һ��
            // һ��12��
            else if (nIssueCount >= 7 && nIssueCount <= 12)
            {
                // 1�����Ժ��ͬ��
                start = DateTimeUtil.NextMonth(start);

                return DateTimeUtil.DateTimeToString8(start);
            }

            // һ��13�� �����Ǵ����ʱ�䷶Χ��ɵ�
            // ��һ��12�ڴ���취һ��
            else if (nIssueCount == 13)
            {

                // 12�·�����
                if (start.Month == 12)
                {
                    // 15���Ժ�
                    start += new TimeSpan(15, 0, 0, 0);
                    return DateTimeUtil.DateTimeToString8(start);
                }

                // 1�����Ժ��ͬ��
                start = DateTimeUtil.NextMonth(start);

                return DateTimeUtil.DateTimeToString8(start);
            }


            // һ��24��
            else if (nIssueCount > 13 && nIssueCount <= 24)
            {
#if NO
                // 15���Ժ�
                start += new TimeSpan(15, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(start);
#endif
                nCount = 2;
            }

            // һ��36��
            else if (nIssueCount > 24 && nIssueCount <= 36)
            {
                // TODO: ����� 36 �ڣ������ֲ���ÿ������ 3 ��
                // ��һ���´��Ի���Ϊ 30/3����������Ĳ�����������һ����ʱ��
                nCount = 3;
#if NO
                // 10���Ժ�
                now += new TimeSpan(10, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(now);
#endif
            }

            // һ��48��
            else if (nIssueCount > 36 && nIssueCount <= 48)
            {
#if NO
                // 7���Ժ�
                start += new TimeSpan(7, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(start);
#endif
                nCount = 4;
            }

            // һ��52��
            else if (nIssueCount > 48 && nIssueCount <= 52)
            {
                // 7���Ժ�
                start += new TimeSpan(7, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(start);
            }

            // 2012/5/9
            // һ��61��
            else if (nIssueCount > 52 && nIssueCount <= 61)
            {
                // 6���Ժ�
                start += new TimeSpan(6, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(start);
            }

            // 2012/5/9
            // һ��73��
            else if (nIssueCount > 61 && nIssueCount <= 73)
            {
                // 5���Ժ�
                start += new TimeSpan(5, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(start);
            }

            // 2012/5/9
            // һ��92��
            else if (nIssueCount > 73 && nIssueCount <= 92)
            {
                // 4���Ժ�
                start += new TimeSpan(4, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(start);
            }

            // 2012/5/9
            // һ��122��
            else if (nIssueCount > 92 && nIssueCount <= 122)
            {
                // 3���Ժ�
                start += new TimeSpan(3, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(start);
            }

            // 2012/5/9
            // һ��183��
            else if (nIssueCount > 122 && nIssueCount <= 183)
            {
                // 2���Ժ�
                start += new TimeSpan(2, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(start);
            }

            // һ��365��
            else if (nIssueCount > 183 && nIssueCount <= 365)
            {
                // 1���Ժ�
                start += new TimeSpan(1, 0, 0, 0);
                return DateTimeUtil.DateTimeToString8(start);
            }

#if NO
            // һ��730��
            else if (nIssueCount > 365 && nIssueCount <= 730)
            {
                // 12Сʱ���Ժ�
                now += new TimeSpan(0, 12, 0, 0);
                return DateTimeUtil.DateTimeToString8(now);
            }
#endif

            if (nCount == 0)
                return "????????";  // �޷����������

            List<PartOfMonth> parts = GetMonthParts(strPublishTime, nCount);
            int current_delta = 0;
            int index = 0;
            LocationPart(parts,
                strPublishTime,
                out index,
                out current_delta);
            Debug.Assert(index != -1, "");

            int nDelta = current_delta;
            if (nPreferredDelta != -1)
                nDelta = nPreferredDelta;

            nPreferredDelta = current_delta;    // ���ر��ε�

            if (index >= nCount - 1)
            {
                // ���ڵ������һ�������ˡ���Ҫ������һ���µĵ�һ������
                List<PartOfMonth> next_parts = GetMonthParts(DateTimeUtil.NextMonth(start), nCount);
                return AddDays(next_parts[0].StartDate, nDelta, next_parts[0].EndDate);
            }

            return AddDays(parts[index + 1].StartDate, nDelta, parts[index + 1].EndDate);


        }

        // ���һ���ڵ�������
        // return:
        //      -1  ����
        //      0   �޷����
        //      1   ���
        int GetOneYearIssueCount(string strPublishYear,
            out int nValue,
            out string strError)
        {
            strError = "";
            nValue = 0;

            if (this.GetOrderInfo == null)
                return 0;   // �޷����

            GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
            e1.BiblioRecPath = "";
            e1.PublishTime = strPublishYear;
            e1.LibraryCodeList = this.LibraryCodeList;
            this.GetOrderInfo(this, e1);
            if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
            {
                strError = "�ڻ�ȡ�����ڳ�������Ϊ '" + strPublishYear + "' �Ķ�����Ϣ�Ĺ����з�������: " + e1.ErrorInfo;
                return -1;
            }

            if (e1.OrderXmls.Count == 0)
                return 0;

            for (int i = 0; i < e1.OrderXmls.Count; i++)
            {
                string strXml = e1.OrderXmls[i];

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "XMLװ��DOMʱ��������: " + ex.Message;
                    return -1;
                }

                string strRange = DomUtil.GetElementText(dom.DocumentElement,
                    "range");

                string strIssueCount = DomUtil.GetElementText(dom.DocumentElement,
                    "issueCount");

                int nIssueCount = 0;
                try
                {
                    nIssueCount = Convert.ToInt32(strIssueCount);
                }
                catch
                {
                    continue;
                }

                float years = Global.Years(strRange);
                if (years != 0)
                {
                    nValue = Convert.ToInt32((float)nIssueCount * (1 / years));
                }
            }

            return 1;
        }

        // ��õ�һ��δ�ǵ��Ķ�����Χ����ʼʱ��
        // return:
        //      -1  ����
        //      0   �޷����
        //      1   ���
        int GetFirstUseablePublishTime(
            out string strPublishTime,
            out string strError)
        {
            strPublishTime = "";
            strError = "";

            if (this.GetOrderInfo == null)
                return 0;   // �޷����

            GetOrderInfoEventArgs e1 = new GetOrderInfoEventArgs();
            e1.BiblioRecPath = "";
            e1.PublishTime = "*";
            e1.LibraryCodeList = this.LibraryCodeList;
            this.GetOrderInfo(this, e1);
            if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
            {
                strError = "�ڻ�ȡ�����ڳ�������Ϊ '" + "*" + "' �Ķ�����Ϣ�Ĺ����з�������: " + e1.ErrorInfo;
                return -1;
            }

            if (e1.OrderXmls.Count == 0)
                return 0;

            List<string> timestrings = new List<string>();
            for (int i = 0; i < e1.OrderXmls.Count; i++)
            {
                string strXml = e1.OrderXmls[i];

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "XMLװ��DOMʱ��������: " + ex.Message;
                    return -1;
                }

                string strState = DomUtil.GetElementText(dom.DocumentElement,
                    "state");

                // ��ʾ��ȫ������
                if (StringUtil.IsInList("������", strState) == true)
                    continue;

                string strRange = DomUtil.GetElementText(dom.DocumentElement,
                    "range");

                string strIssueCount = DomUtil.GetElementText(dom.DocumentElement,
                    "issueCount");

                int nIssueCount = 0;
                try
                {
                    nIssueCount = Convert.ToInt32(strIssueCount);
                }
                catch
                {
                    continue;
                }

                if (nIssueCount == 0)
                    continue;

                int nRet = strRange.IndexOf("-");
                if (nRet == -1)
                {
                    strError = "ʱ�䷶Χ�ַ��� '"+strRange+"' ��ʽ����ȷ";
                    return -1;
                }

                timestrings.Add(strRange.Substring(0, nRet));
            }

            if (timestrings.Count == 0)
                return 0;

            // ����ȡ����С��ʱ��ֵ
            timestrings.Sort();
            strPublishTime = timestrings[0];

            return 1;
        }

        #endregion

        // �����ǵ�
        void menuItem_unacceptCells_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // ��ѡ��Χ�������Ѿ��ǵ��ĸ��ӡ�
            // ���ѡ��Χ�а�����GroupCell left����GroupCellRight�������ѡ������������ȫ���ǵ����ӡ�ע�ⲻҪ�ظ�׼�����Ӷ���

            List<Cell> selected_cells = this.SelectedCells;
            if (selected_cells.Count == 0)
            {
                strError = "��δѡ��Ҫ�����ǵ��Ĳ�";
                goto ERROR1;
            }

            int nSkipCount = 0;
            // Ԥ���
            List<Cell> accepted_cells = new List<Cell>();
            // ��ѡ
            for (int i = 0; i < selected_cells.Count; i++)
            {
                Cell cell = selected_cells[i];
                if (cell == null)
                {
                    nSkipCount++;
                    continue;
                }

                if (cell is GroupCell)
                {
                    // 2010/4/15
                    GroupCell group = (GroupCell)cell;
                    if (group.EndBracket == true)
                        group = group.HeadGroupCell;

                    List<Cell> temp = group.AcceptedMemberCells;
                    for (int j = 0; j < temp.Count; j++)
                    {
                        Cell cell_temp = temp[j];
                        // �����ظ�����
                        if (accepted_cells.IndexOf(cell_temp) == -1)
                            accepted_cells.Add(cell_temp);
                    }
                    continue;
                }

                if (cell.item != null && cell.item.OrderInfoPosition.X != -1)
                {
                    // �����ظ�����
                    if (accepted_cells.IndexOf(cell) == -1)
                        accepted_cells.Add(cell);
                    continue;
                }

                nSkipCount++;
            }

            if (accepted_cells.Count == 0)
            {
                strError = "��ѡ���� " + this.SelectedCells.Count.ToString() + " �������У�û�д����Ѽǵ�״̬�ĸ���";
                goto ERROR1;
            }

            // ͳ�Ƴ�Ҫɾ����(�Ǳ����´�����)���¼
            int nOldRecordCount = 0;
            for (int i = 0; i < accepted_cells.Count; i++)
            {
                Cell cell = accepted_cells[i];
                Debug.Assert(cell != null, "");

                // ͳ�ƽ�����ɾ���ķǱ��δ�����
                if (cell.item.NewCreated == false
                    && cell.item.Deleted == false)
                {
                    // Ҫ�����Щ�������Ƿ��н�����Ϣ
                    if (String.IsNullOrEmpty(cell.item.Borrower) == false)
                    {
                        strError = "�� " +cell.item.RefID+ "(������Ϊ'"+cell.item.Barcode+"') �а����н�����Ϣ������ɾ����������ȡ��";
                        goto ERROR1;
                    }
                    nOldRecordCount++;
                }

                // ����Ƿ�Ϊ��Ա��
                if (cell.IsMember == true)
                {
                    strError = "�� " + cell.item.RefID + " �Ѿ����϶������ܱ������ǵ���������ȡ��";
                    goto ERROR1;
                }

                if (cell.item != null && cell.item.Locked == true)
                {
                    strError = "�Դ�������״̬�ĸ��Ӳ��ܽ��г����ǵ�����";
                    goto ERROR1;
                }
            }

            string strMessage = "";
            if (nOldRecordCount > 0)
                strMessage = "�����ǵ��Ĳ�����������ǰ������ "+nOldRecordCount.ToString()+" �����¼��ɾ����\r\n\r\n";

            // ����
            DialogResult dialog_result = MessageBox.Show(this,
                strMessage
            + "ȷʵҪ����ѡ���� " + accepted_cells.Count.ToString() + " �����ӽ��г����ǵ��Ĳ���?",
"BindingControls",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
            if (dialog_result == DialogResult.No)
                return;


            for (int i = 0; i < accepted_cells.Count; i++)
            {
                Cell cell = accepted_cells[i];
                Debug.Assert(cell != null, "");
                if (String.IsNullOrEmpty(cell.Container.PublishTime) == true)
                {
                    strError = "�Դ����������еĸ��Ӳ��ܽ��г����ǵ�����";
                    goto ERROR1;
                }
            }

            for (int i = 0; i < accepted_cells.Count; i++)
            {
                Cell cell = accepted_cells[i];
                Debug.Assert(cell != null, "");
                nRet = cell.item.DoUnaccept(out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            // �ӵ�Ԫ���ӱ仯Ϊ���������ڸ���
            List<IssueBindingItem> update_issues = GetIssueList(accepted_cells);
            this.UpdateIssues(update_issues);
            /*

            // ˢ������
            List<CellBase> update_cells = new List<CellBase>();
            for (int i = 0; i < update_issues.Count; i++)
            {
                update_cells.Add((CellBase)update_issues[i]);
            }
            this.UpdateObjects(update_cells);
             * */

            // ˢ�±༭����
            if (this.FocusObject != null && this.FocusObject is Cell)
            {
                Cell focus_obejct = (Cell)this.FocusObject;
                if (accepted_cells.IndexOf(focus_obejct) != -1)
                {
                    if (this.CellFocusChanged != null)
                    {
                        FocusChangedEventArgs e1 = new FocusChangedEventArgs();
                        e1.OldFocusObject = this.FocusObject;
                        e1.NewFocusObject = this.FocusObject;
                        this.CellFocusChanged(this, e1);
                    }
                }
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �ǵ� -- ������ΪԤ����Ӵ��������Ĳ��¼
        // Ϊ<orderInfo>�µ��ض�<root>��<location>�趨refid�����޸�<copy>�е��ѵ�ֵ����
        // �û����Ҫ�޸��ֶ����ݣ��ɵ�����Ϣ�༭���н��С�����Ͳ��ٳ��ֶԻ����ˡ�
        // ���κŵȻ��Զ�����
        void menuItem_AcceptCells_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // ��ѡ��Χ������Ԥ����ӡ�
            // ���ѡ��Χ�а�����GroupCell left����GroupCellRight�������ѡ������������ȫ��Ԥ����ӡ�ע�ⲻҪ�ظ�׼�����Ӷ���

            List<Cell> selected_cells = this.SelectedCells;
            if (selected_cells.Count == 0)
            {
                strError = "��δѡ��Ҫ�ǵ��Ĳ�";
                goto ERROR1;
            }

            int nSkipCount = 0;
            // Ԥ���
            List<Cell> calculated_cells = new List<Cell>();
            // ��ѡ
            for (int i = 0; i < selected_cells.Count; i++)
            {
                Cell cell = selected_cells[i];
                if (cell == null)
                {
                    nSkipCount++;
                    continue;
                }

                if (cell is GroupCell)
                {
                    GroupCell group = (GroupCell)cell;

                    if (group.EndBracket == true)
                        group = group.HeadGroupCell;

                    List<Cell> member_cells = group.CalculatedMemberCells;
                    for (int j = 0; j < member_cells.Count; j++)
                    {
                        Cell cell_temp = member_cells[j];
                        // �����ظ�����
                        if (calculated_cells.IndexOf(cell_temp) == -1)
                            calculated_cells.Add(cell_temp);
                    }
                    continue;
                }

                if (cell.item != null && cell.item.Calculated == true)
                {
                    // �����ظ�����
                    if (calculated_cells.IndexOf(cell) == -1)
                        calculated_cells.Add(cell);
                    continue;
                }

                nSkipCount++;
            }

            if (calculated_cells.Count == 0)
            {
                strError = "��ѡ���� " + this.SelectedCells.Count.ToString()+ " �������У�û�д���Ԥ��״̬�ĸ���";
                goto ERROR1;
            }

            for (int i = 0; i < calculated_cells.Count; i++)
            {
                Cell cell = calculated_cells[i];
                Debug.Assert(cell != null, "");

                if (String.IsNullOrEmpty(cell.Container.PublishTime) == true)
                {
                    strError = "�Դ����������еĸ��Ӳ��ܽ��мǵ�����";
                    goto ERROR1;
                }

                if (cell.item != null && cell.item.Locked == true)
                {
                    strError = "�Դ�������״̬�ĸ��Ӳ��ܽ��мǵ�����";
                    goto ERROR1;
                }
            }

            for (int i = 0; i < calculated_cells.Count; i++)
            {
                Cell cell = calculated_cells[i];
                Debug.Assert(cell != null, "");
                nRet = cell.item.DoAccept(out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            // �ӵ�Ԫ���ӱ仯Ϊ���������ڸ���
            List<IssueBindingItem> update_issues = GetIssueList(calculated_cells);
            this.UpdateIssues(update_issues);
            /*

            // ˢ������
            List<CellBase> update_cells = new List<CellBase>();
            for (int i = 0; i < update_issues.Count; i++)
            {
                update_cells.Add((CellBase)update_issues[i]);
            }
            this.UpdateObjects(update_cells);
             * */

            // ˢ�±༭����
            if (this.FocusObject != null && this.FocusObject is Cell)
            {
                Cell focus_obejct = (Cell)this.FocusObject;
                if (calculated_cells.IndexOf(focus_obejct) != -1)
                {
                    if (this.CellFocusChanged != null)
                    {
                        FocusChangedEventArgs e1 = new FocusChangedEventArgs();
                        e1.OldFocusObject = this.FocusObject;
                        e1.NewFocusObject = this.FocusObject;
                        this.CellFocusChanged(this, e1);
                    }
                }
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ����ڿ����Ƶ��б�������ʾ�ڶԻ�����
        // parameters:
        //      strDelimiter    �ָ�����
        //      nMaxCount   ����г����ٸ�
        static string GetIssuesCaption(List<IssueBindingItem> issues,
            string strDelimiter,
            int nMaxCount)
        {
            string strCaptions = "";
            for (int i = 0; i < Math.Min(issues.Count, nMaxCount); i++)
            {
                if (string.IsNullOrEmpty(strCaptions) == false)
                    strCaptions += strDelimiter;
                strCaptions += issues[i].Caption;
            }
            if (issues.Count >= nMaxCount)
                strCaptions += strDelimiter + "...";

            return strCaptions;
        }

        // �ָ����ɸ��ڼ�¼
        void menuItem_recoverIssues_Click(object sender, EventArgs e)
        {
            string strError = "";

            // �Ѿ���ʵ��״̬����
            List<IssueBindingItem> normal_issues = new List<IssueBindingItem>();
            // ���ϻָ���������
            List<IssueBindingItem> selected_issues = new List<IssueBindingItem>();
            for (int i = 0; i < this.SelectedIssues.Count; i++)
            {
                IssueBindingItem issue = this.SelectedIssues[i];

                // ����������
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;

                if (issue.Virtual == false)
                {
                    normal_issues.Add(issue);
                    continue;
                }

                selected_issues.Add(issue);
            }

            if (normal_issues.Count == 0
                && selected_issues.Count == 0)
            {
                strError = "��δѡ��Ҫ�ָ����ڶ���";
                goto ERROR1;
            }

            string strNormalIssueList = "";
            if (normal_issues.Count > 0)
            {
                strNormalIssueList = GetIssuesCaption(normal_issues,
                    "\r\n", 10);
                strError = "���ָܻ����±����ͼ�¼����:\r\n" + strNormalIssueList + "\r\n\r\n";
                if (selected_issues.Count == 0)
                {
                    goto ERROR1;
                }
            }

            string strCaptions = GetIssuesCaption(selected_issues,
                    "\r\n", 10);

            string strMessage = strError;
            if (String.IsNullOrEmpty(strMessage) == false)
                strMessage += "---\r\n�Ƿ�Ҫ�����ָ�����������ڵ����ݿ��¼?\r\n " + strCaptions;
            else
                strMessage += "ȷʵҪ�ָ������ڵ����ݿ��¼?\r\n " + strCaptions;

            // 
            DialogResult dialog_result = MessageBox.Show(this,
    strMessage,
    "BindingControls",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
            if (dialog_result == DialogResult.No)
                return;

            for (int i = 0; i < selected_issues.Count; i++)
            {
                IssueBindingItem issue = selected_issues[i];
                issue.Virtual = false;
                if (string.IsNullOrEmpty(issue.RefID) == true)
                    issue.RefID = Guid.NewGuid().ToString();
                issue.Changed = true;
                issue.NewCreated = true;
                issue.AfterMembersChanged();    // ˢ��Issue�����ڵ�XML
                this.m_bChanged = true;
            }

            // this.m_lContentHeight = this.m_nCellHeight * this.Issues.Count;
            this.AfterWidthChanged(true);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ɾ�����ɸ���
        void menuItem_deleteIssues_Click(object sender, EventArgs e)
        {
            string strError = "";

            /*
            ToolStripMenuItem menu = (ToolStripMenuItem)sender;

            HitTestResult result = null;

            Point p = (Point)menu.Tag;

            // Debug.WriteLine("hover=" + p.ToString());

            // ��Ļ����
            this.HitTest(
                p.X,
                p.Y,
                typeof(IssueBindingItem),
                out result);
            if (result == null || !(result.Object is IssueBindingItem))
            {
                strError = "���δ�����ʵ���λ��";
                goto ERROR1;
            }

            IssueBindingItem issue = (IssueBindingItem)result.Object;

            Debug.Assert(issue != null, "");
             * */
            List<string> messages = new List<string>();
            // �ǿյ���
            List<IssueBindingItem> cantdelete_issues = new List<IssueBindingItem>();
            // ����ɾ����������
            List<IssueBindingItem> selected_issues = new List<IssueBindingItem>();
            for (int i = 0; i < this.SelectedIssues.Count; i++)
            {
                IssueBindingItem issue = this.SelectedIssues[i];

                // ����������
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;

                string strTemp = "";
                if (issue.CanDelete(out strTemp) == false)
                {
                    cantdelete_issues.Add(issue);
                    messages.Add(strTemp);
                    continue;
                }

                selected_issues.Add(issue);
            }

            if (cantdelete_issues.Count == 0
                && selected_issues.Count == 0)
            {
                strError = "��δѡ��Ҫɾ�����ڶ���";
                goto ERROR1;
            }

#if NO
            string strNoneBlankList = "";
            if (noneblank_issues.Count > 0)
            {
                strNoneBlankList = GetIssuesCaption(noneblank_issues,
                    "\r\n", 10);
                strError = "����ɾ�����»����в����:\r\n" + strNoneBlankList + "\r\n\r\n";
                if (selected_issues.Count == 0)
                {
                    goto ERROR1;
                }
            }
#endif
            string strCantDeleteList = "";
            if (cantdelete_issues.Count > 0)
            {
                for (int i = 0; i < cantdelete_issues.Count; i++)
                {
                    strCantDeleteList += cantdelete_issues[i].Caption + " : " + messages[i] + "\r\n";
                    if (i > 10)
                    {
                        strCantDeleteList += "...";
                        break;
                    }
                }

                strError = "����ɾ��������:\r\n" + strCantDeleteList + "\r\n\r\n";
                if (selected_issues.Count == 0)
                {
                    goto ERROR1;
                }
            }

            string strCaptions = GetIssuesCaption(selected_issues,
                    "\r\n", 10);

            string strMessage = strError;
            if (String.IsNullOrEmpty(strMessage) == false)
                strMessage += "---\r\n�Ƿ�Ҫ����ɾ�������������?\r\n " + strCaptions;
            else
                strMessage += "ȷʵҪɾ��������?\r\n " + strCaptions;

            // 
            DialogResult dialog_result = MessageBox.Show(this,
    strMessage,
    "BindingControls",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
            if (dialog_result == DialogResult.No)
                return;

            for (int i = 0; i < selected_issues.Count; i++)
            {
                IssueBindingItem issue = selected_issues[i];

                if (this.FocusObject == issue)
                    this.FocusObject = null;

                this.m_aSelectedArea.Remove(issue); // ��ֹȡ��ѡ��ʱ�׳��쳣

                this.Issues.Remove(issue);
                this.m_bChanged = true;
            }

            // this.m_lContentHeight = this.m_nCellHeight * this.Issues.Count;
            this.AfterWidthChanged(true);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��/�ر� �༭����
        void menuItem_toggleEditArea_Click(object sender, EventArgs e)
        {
            if (this.EditArea == null)
                return;

            bool bClose = false;
            {
                EditAreaEventArgs e1 = new EditAreaEventArgs();
                e1.Action = "get_state";
                this.EditArea(this, e1);
                if (e1.Result == "visible")
                    bClose = true;
                else
                    bClose = false;
            }

            {
                EditAreaEventArgs e1 = new EditAreaEventArgs();
                if (bClose == true)
                    e1.Action = "close";
                else
                    e1.Action = "open";
                this.EditArea(this, e1);
            }
        }

        // ��ѡ��ĺ϶��������ƶ�һ��˫��
        void menuItem_moveToLeft_Click(object sender, EventArgs e)
        {
            string strError = "";
            // string strWarning = "";

            // �϶�������
            List<Cell> parent_cells = new List<Cell>();

            List<Cell> selected_cells = this.SelectedCells;
            for (int i = 0; i < selected_cells.Count; i++)
            {
                Cell cell = selected_cells[i];
                if (IsBindingParent(cell) == true)
                {
                    // ע��cell.item.MemberCells.Count �п��ܵ���0
                    parent_cells.Add(cell);
                }
            }

            if (parent_cells.Count == 0)
            {
                strError = "��ѡ���ĵ�Ԫ�У�û�к϶���";
                goto ERROR1;
            }

            for (int i = 0; i < parent_cells.Count; i++)
            {
                Cell cell = parent_cells[i];
                if (CanMoveToLeft(cell) == false)
                    continue;

                MoveCellsToLeft(cell);
#if DEBUG
                {
                    string strError1 = "";
                    int nRet1 = cell.item.VerifyMemberCells(out strError1);
                    if (nRet1 == -1)
                    {
                        Debug.Assert(false, strError1);
                    }
                }

#endif
            }

            this.AfterWidthChanged(true);

#if DEBUG
            VerifyAll();
#endif
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �Ӻ϶������Ƴ���Ա�ᣬ��(�����Ҫ)��С�϶���Χ
        void menuItem_removeFromBindingAndShrink_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.SelectedCells.Count == 0)
            {
                strError = "��δѡ��Ҫ�Ƴ��ĺ϶���Ա��";
                goto ERROR1;
            }

            List<Cell> source_cells = new List<Cell>();

            source_cells.AddRange(this.SelectedCells);
            // ���϶���Ա��Ӻ϶������Ƴ�����Ϊ����
            nRet = RemoveFromBinding(
                true,   // shrink
                false,
                source_cells,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �Ӻ϶������Ƴ���Ա�ᣬ����С�϶���Χ
        void menuItem_removeFromBinding_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.SelectedCells.Count == 0)
            {
                strError = "��δѡ��Ҫ�Ƴ��ĺ϶���Ա��";
                goto ERROR1;
            }

            List<Cell> source_cells = new List<Cell>();

            source_cells.AddRange(this.SelectedCells);
            // ���϶���Ա��Ӻ϶������Ƴ�����Ϊ����
            nRet = RemoveFromBinding(
                false,  // shrink
                false,
                source_cells,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��Ϊ�հ�
        void menuItem_setBlank_Click(object sender, EventArgs e)
        {
            string strError = "";

            ToolStripMenuItem menu = (ToolStripMenuItem)sender;

            HitTestResult result = null;

            // Point p = this.PointToClient((Point)menu.Tag); // Control.MousePosition
            Point p = (Point)menu.Tag;

            // Debug.WriteLine("hover=" + p.ToString());

            // ��Ļ����
            this.HitTest(
                p.X,
                p.Y,
                typeof(NullCell),
                out result);
            if (result == null || !(result.Object is NullCell))
            {
                strError = "���δ�����ʵ���λ��";
                goto ERROR1;
            }

            NullCell null_cell = (NullCell)result.Object;

            IssueBindingItem issue = this.Issues[null_cell.Y];
            Debug.Assert(issue != null, "");

            // TODO: ������Ѿ�װ���ķ�Χ�ڣ��Ƿ�Ҫ��binded����Ϊtrue

            Cell cell = new Cell();
            cell.item = null;
            cell.ParentItem = null;
            issue.SetCell(null_cell.X, cell);
            this.UpdateObject(cell);

            this.ClearAllSelection();
            cell.Select(SelectAction.On);
            this.FocusObject = cell;

            // �п����´����Ŀհ׸��ӳ������Ҳ�߽�
            if (null_cell.X >= this.m_nMaxItemCountOfOneIssue)
                this.AfterWidthChanged(true);

            this.EnsureVisible(cell);  // ȷ��������Ұ

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ɾ����ѡ��� ����
        void menuItem_deleteCells_Click(object sender, EventArgs e)
        {
            // TODO: �ǵ�����ѡ��Χ������ this.m_aSelectedArea.Remove(deleted_cell); // ��ֹȡ��ѡ��ʱ�׳��쳣

            string strError = "";
            string strWarning = "";
            int nRet = 0;

            List<Cell> selected_cells = this.SelectedCells;
            if (selected_cells.Count == 0)
            {
                strError = "��δѡ��Ҫɾ���ĸ���";
                goto ERROR1;
            }

            // ����
            List<Cell> mono_cells = new List<Cell>();

            // �Ѿ�����װ���Ĳ�
            List<Cell> member_cells = new List<Cell>();

            // �϶���
            List<Cell> parent_cells = new List<Cell>();

            // �հ�Cell
            List<Cell> blank_cells = new List<Cell>();

            // ��ѡ
            for (int i = 0; i < selected_cells.Count; i++)
            {
                Cell cell = selected_cells[i];

                if (this.IsBindingParent(cell) == true)
                {
                    Debug.Assert(cell.item != null, "");
                    Debug.Assert(cell.item.IsMember == false, "");
                    parent_cells.Add(cell);
                }
                else if (cell.IsMember == true)
                {
                    // ע��cell.item����Ϊ��
                    member_cells.Add(cell);
                }
                else
                    mono_cells.Add(cell);

                /*
                blank_cells.Add(cell);
                    // TODO: �հ׸�����δ���?
                 * */
            }

            /*
            // ���棬�϶���Ա��Ҳ�ᱻɾ��
            // TODO: Ҫ���϶�������������ᣬ��û�н軹��Ϣ������ɾ������
            if (binded_items.Count > 0)
            {
                strError = "�� " + binded_items.Count.ToString() + " ���������Ѿ����϶��Ĳᣬ����޷����к϶�";
                goto ERROR1;
            }

            if (mono_items.Count == 0)
            {
                strError = "��ѡ���ĸ�����û�а����κβ�";
                goto ERROR1;
            }
             * */

            // ���̻���
            strWarning = "";
            int nFixedCount = 0;
            int nOldCount = parent_cells.Count;
            for (int i = 0; i < parent_cells.Count; i++)
            {
                Cell parent_cell = parent_cells[i];

                if (CheckProcessingState(parent_cell.item) == false)
                {
                    if (String.IsNullOrEmpty(strWarning) == false)
                        strWarning += "\r\n";
                    strWarning += parent_cell.item.PublishTime;
                    nFixedCount++;
                    parent_cells.RemoveAt(i);
                    i--;
                }

                if (parent_cell.item.Locked == true)
                {
                    strError = "��������״̬�ĺ϶��᲻��ɾ��";
                    goto ERROR1;
                }
            }

            bool bAsked = false;

            // ����̻���
            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strError =
                    "��ѡ���� " + nOldCount.ToString() + " ���϶����У������вᴦ�ڹ̻�״̬������ɾ��:\r\n\r\n" + strWarning;

                if (parent_cells.Count > 0)
                {
                    strError += "\r\n\r\n�Ƿ�Ҫ����ɾ����ѡ��Χ�ڵ����� "
                        + (parent_cells.Count).ToString()
                        + " ���϶���?";
                    DialogResult result = MessageBox.Show(this,
                        strError,
                        "BindingControls",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return;
                    bAsked = true;
                }
                else
                    goto ERROR1;
            }

            // ���϶���ĳ�Ա��
            strWarning = "";
            int nErrorCount = 0;
            for (int i = 0; i < parent_cells.Count; i++)
            {
                Cell parent_cell = parent_cells[i];

                for (int j = 0; j < parent_cell.item.MemberCells.Count; j++)
                {
                    Cell cell = parent_cell.item.MemberCells[j];

                    if (cell == null && cell.item == null)
                        continue;

                    if (CheckProcessingState(cell.item) == false
    && cell.item.Calculated == false   // Ԥ����ӳ���
    && cell.item.Deleted == false)  // �Ѿ�ɾ���ĸ��ӳ���
                    {
                        // ����"�ӹ���"״̬
                        if (String.IsNullOrEmpty(strWarning) == false)
                            strWarning += ";\r\n";
                        strWarning += "�� '" + cell.item.RefID + "' ���ǡ��ӹ��С�״̬";
                        nErrorCount++;
                    }
                    else if (String.IsNullOrEmpty(cell.item.Borrower) == false)
                    {
                        // �ѽ��״̬
                        if (String.IsNullOrEmpty(strWarning) == false)
                            strWarning += ";\r\n";
                        strWarning += "�� '" + cell.item.RefID + "' �д��ڡ��ѽ����״̬";
                        nErrorCount++;
                    }
                }
            }
            // �����в���ɾ���ĳ�Ա�� �� �϶���
            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strError =
                    "��ѡ���� " + parent_cells.Count.ToString() + " ���϶����У��������г�Ա�᲻��ɾ��:\r\n\r\n" + strWarning;
                strError += "\r\n\r\n��˺϶��᱾��Ҳ�����Ų��ܱ�ɾ����\r\n\r\n(���ȷʵ��Ҫɾ���϶��᱾�����Ƚ���϶��Ժ�����ɾ��)";

                goto ERROR1;
            }

            // ��鵥��
            strWarning = "";
            nErrorCount = 0;
            nOldCount = mono_cells.Count;
            for (int i = 0; i < mono_cells.Count; i++)
            {
                Cell cell = mono_cells[i];
                IssueBindingItem issue = cell.Container;
                Debug.Assert(issue != null, "");

                if (cell is GroupCell)
                {
                    if (String.IsNullOrEmpty(strWarning) == false)
                        strWarning += ";\r\n";
                    strWarning += "���������";
                    nErrorCount++;
                    mono_cells.RemoveAt(i);
                    i--;
                }

                if (cell.item == null)
                    continue;

                if (CheckProcessingState(cell.item) == false
                    && cell.item.Calculated == false   // Ԥ����ӳ���
                    && cell.item.Deleted == false)  // �Ѿ�ɾ���ĸ��ӳ���
                {
                    // ����"�ӹ���"״̬
                    if (String.IsNullOrEmpty(strWarning) == false)
                        strWarning += ";\r\n";
                    strWarning += "�� '"+cell.item.RefID+"' ���ǡ��ӹ��С�״̬";
                    nErrorCount++;
                    mono_cells.RemoveAt(i);
                    i--;
                }
                else if (String.IsNullOrEmpty(cell.item.Borrower) == false)
                {
                    // �ѽ��״̬
                    if (String.IsNullOrEmpty(strWarning) == false)
                        strWarning += ";\r\n";
                    strWarning += "�� '" + cell.item.RefID + "' �д��ڡ��ѽ����״̬";
                    nErrorCount++;
                    mono_cells.RemoveAt(i);
                    i--;
                }
                else if (cell.item.Locked == true)
                {
                    // �ѽ��״̬
                    if (String.IsNullOrEmpty(strWarning) == false)
                        strWarning += ";\r\n";
                    strWarning += "�� '" + cell.item.RefID + "' ���ڡ�������״̬";
                    nErrorCount++;
                    mono_cells.RemoveAt(i);
                    i--;
                }
            }
            // ���治��ɾ���ĵ���
            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strError =
                    "��ѡ���� " + nOldCount.ToString() + " �������У������в᲻��ɾ��:\r\n\r\n" + strWarning;

                if (mono_cells.Count > 0)
                {
                    strError += "\r\n\r\n�Ƿ�Ҫ����ɾ����ѡ��Χ�ڵ����� "
                        + (mono_cells.Count).ToString()
                        + " ������?";
                    DialogResult result = MessageBox.Show(this,
                        strError,
                        "BindingControls",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return;
                    bAsked = true;
                }
                else
                    goto ERROR1;
            }

            // ����Ա��
            strWarning = "";
            nErrorCount = 0;
            nOldCount = member_cells.Count;
            for (int i = 0; i < member_cells.Count; i++)
            {
                Cell cell = member_cells[i];
                IssueBindingItem issue = cell.Container;
                Debug.Assert(issue != null, "");

                if (cell.item == null)
                    continue;

                // ��������ѡ�϶���ĳ�Ա�ᣬҪ�����ظ�ɾ��
                bool bFound = false;
                for (int j=0;j<parent_cells.Count;j++)
                {
                    Cell parent_cell = parent_cells[j];

                    Debug.Assert(parent_cell.item != null, "");
                    if (cell.ParentItem == parent_cell.item)
                    {
                        bFound = true;
                        break;
                    }
                }
                if (bFound == true)
                {
                    member_cells.RemoveAt(i);
                    i--;
                    continue;
                }

                if (CheckProcessingState(cell.item) == false)
                {
                    // ����"�ӹ���"״̬
                    if (String.IsNullOrEmpty(strWarning) == false)
                        strWarning += ";\r\n";
                    strWarning += "�� '" + cell.item.RefID + "' ���ǡ��ӹ��С�״̬";
                    nErrorCount++;
                    member_cells.RemoveAt(i);
                    i--;
                }
                else if (String.IsNullOrEmpty(cell.item.Borrower) == false)
                {
                    // �ѽ��״̬
                    if (String.IsNullOrEmpty(strWarning) == false)
                        strWarning += ";\r\n";
                    strWarning += "�� '" + cell.item.RefID + "' �д��ڡ��ѽ����״̬";
                    nErrorCount++;
                    member_cells.RemoveAt(i);
                    i--;
                }
                else
                {
                    // �����ĺ϶��� ���ǡ��ӹ��С�״̬
                    Cell parent_cell = cell.ParentItem.ContainerCell;
                    if (CheckProcessingState(parent_cell.item) == false)
                    {
                        // ����"�ӹ���"״̬
                        if (String.IsNullOrEmpty(strWarning) == false)
                            strWarning += ";\r\n";
                        strWarning += "�� '" + cell.item.RefID + "' �������ĺ϶��ᴦ�ڹ̻�״̬";
                        nErrorCount++;
                        member_cells.RemoveAt(i);
                        i--;
                    }

                    if (parent_cell.item.Locked == true)
                    {
                        // ����"�ӹ���"״̬
                        if (String.IsNullOrEmpty(strWarning) == false)
                            strWarning += ";\r\n";
                        strWarning += "�� '" + cell.item.RefID + "' �������ĺ϶��ᴦ������״̬";
                        nErrorCount++;
                        member_cells.RemoveAt(i);
                        i--;
                    }
                }
            }

            // ���治��ɾ���ĳ�Ա��
            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strError =
                    "��ѡ���� " + nOldCount.ToString() + " ����Ա���У������в᲻��ɾ��:\r\n\r\n" + strWarning;

                if (member_cells.Count > 0)
                {
                    strError += "\r\n\r\n�Ƿ�Ҫ����ɾ����ѡ��Χ�ڵ����� "
                        + (member_cells.Count).ToString()
                        + " ����Ա��?";
                    DialogResult result = MessageBox.Show(this,
                        strError,
                        "BindingControls",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return;
                    bAsked = true;
                }
                else
                    goto ERROR1;
            }

            // ���漴��ɾ���϶���
            if (parent_cells.Count > 0)
            {
                DialogResult dialog_result = MessageBox.Show(this,
    "���棺ɾ���϶����ͬʱ��Ҳ��ɾ���������ĳ�Ա�ᡣ\r\n\r\n(�����Ҫɾ���϶�����������Ա�ᣬ�ɸ�Ϊ���á�����϶������ܣ��ٽ���ɾ��)\r\n\r\nȷʵҪɾ����ѡ���� "
    + parent_cells.Count.ToString()
    + " ���϶��ἰ�������ĳ�Ա��?",
    "BindingControls",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (dialog_result == DialogResult.No)
                    return;
                bAsked = true;
                // ��Ҫ����ɾ������ĵ���/��Ա��?
                if (mono_cells.Count > 0 || member_cells.Count > 0)
                    bAsked = false;
            }


            if (bAsked == false)
            {
                DialogResult dialog_result = MessageBox.Show(this,
    "ȷʵҪɾ����ѡ���� "
    + (mono_cells.Count + member_cells.Count + parent_cells.Count).ToString()
    + " ����?",
    "BindingControls",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (dialog_result == DialogResult.No)
                    return;
                bAsked = true;
            }



            // *** ��ʼ����ɾ��

            // ɾ������
            for (int i = 0; i < mono_cells.Count; i++)
            {
                Cell cell = mono_cells[i];
                IssueBindingItem issue = cell.Container;
                Debug.Assert(issue != null, "");

                int nCol = issue.IndexOfCell(cell);
                Debug.Assert(nCol != -1, "");

                // �ǵ�����ģʽ�µ�ɾ��
                if (issue.IssueLayoutState == IssueLayoutState.Accepting)
                {
                    DeleteOneCellInAcceptingLayout(cell);
                    continue;
                }

                // װ������ģʽ�µ�ɾ��

                // ���вɹ���Ϣ�����ĸ���
                if (cell.item != null
                    && cell.item.OrderInfoPosition.X != -1)
                {
                    Debug.Assert(cell.item.OrderInfoPosition.Y != -1, "");

                    /*
                    // ����ɾ����
                    if (cell.item.Calculated == false)
                    {
                        nRet = cell.item.DoUnaccept(out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                    else
                    {
                        // ɾ��Ԥ���Ҫ���¶�����Ϣ
                        nRet = cell.item.DoDelete(out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        goto DODELETE;
                    }
                     * */

                    // һ��ɾ����
                    // ɾ��ǰ��������Ҫ���¶�����Ϣ
                    nRet = cell.item.DoDelete(out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    goto DODELETE;

                    /*
                    this.m_bChanged = true;
                    continue;
                     * */
                }

                {
                    // �϶�˫������λ�ã�������RemoveSingleIndexɾ��
                    // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
                    // return:
                    //      -1  �ǡ�������˫������λ��
                    //      0   ����
                    //      1   �ǡ�������˫����Ҳ�λ��
                    nRet = issue.IsBoundIndex(nCol);
                    if (nRet == -1)
                    {
                        if (cell.item == null)
                        {
                            issue.SetCell(nCol, null);
                            goto CONTINUE;
                        }

                    }
                }

            DODELETE:
                // ��������(������Ϣ��Ͻ��ĵ���)��ɾ��
                issue.RemoveSingleIndex(nCol);
            CONTINUE:
                this.m_bChanged = true;
            }

            // ɾ���϶���
            strWarning = "";
            for (int i = 0; i < parent_cells.Count; i++)
            {
                Cell parent_cell = parent_cells[i];

                IssueBindingItem parent_issue = parent_cell.Container;
                Debug.Assert(parent_issue != null, "");

                int nParentCol = parent_issue.IndexOfCell(parent_cell);
                Debug.Assert(nParentCol != -1, "");

                Debug.Assert(parent_cell.item != null, "");
                this.ParentItems.Remove(parent_cell.item);

                parent_issue.SetCell(nParentCol, null); // TODO: �����޸�Ϊ����ѹ������
                this.m_bChanged = true;

                for (int j = 0; j < parent_cell.item.MemberCells.Count; j++)
                {
                    Cell cell = parent_cell.item.MemberCells[j];

                    // TODO: �δ�����Ա���Ƿ���Ա�ɾ��? �����Ƿ�߱��ӹ���״̬

                    IssueBindingItem issue = cell.Container;
                    Debug.Assert(issue != null, "");

                    int nCurCol = issue.IndexOfCell(cell);
#if DEBUG
                    if (issue.IssueLayoutState == IssueLayoutState.Binding
                        && parent_issue.IssueLayoutState == IssueLayoutState.Binding)
                    {
                        Debug.Assert(nCurCol == nParentCol + 1, "");
                    }
#endif

                    // ���вɹ���Ϣ�����ĸ���
                    if (cell.item != null
                        && cell.item.OrderInfoPosition.X != -1)
                    {
                        Debug.Assert(cell.item.OrderInfoPosition.Y != -1, "");
                        nRet = cell.item.DoDelete(out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }

                    // ���ΪNullCell
                    issue.SetCell(nCurCol, null);
                    if (issue.IssueLayoutState == IssueLayoutState.Accepting)
                    {
                        if (nCurCol < issue.Cells.Count)
                        {
                            issue.Cells.RemoveAt(nCurCol);
                        }
                    }
                    this.m_bChanged = true;

                    // ��ѡ���ĺ϶�����󣬿�������������Ҳ��ѡ����
                    // ���ﱣ֤�����ظ�ɾ��
                    member_cells.Remove(cell);
                }
            }

            // ɾ����Ա��
            // ���϶���Ա��Ӻ϶������Ƴ�����ʧ
            nRet = RemoveFromBinding(
                true,
                true,
                member_cells,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.AfterWidthChanged(true); 
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �ڼǵ�����ģʽ��ɾ��һ������
        void DeleteOneCellInAcceptingLayout(Cell cell)
        {
            IssueBindingItem issue = cell.Container;
            Debug.Assert(issue != null, "");

            int nCol = issue.IndexOfCell(cell);
            Debug.Assert(nCol != -1, "");

            Debug.Assert(issue.IssueLayoutState == IssueLayoutState.Accepting, "");

            if (cell.item != null)
            {
                GroupCell group = null;
                if (cell.item.Calculated == true
                    || cell.item.OrderInfoPosition.X != -1)
                {
                    group = cell.item.GroupCell;
                }

                if (nCol < issue.Cells.Count)
                {
                    issue.SetCell(nCol, null);  // ��ֹHoverObject Assertion
                    issue.Cells.RemoveAt(nCol);
                }

                if (group != null)
                {
                    int nSourceOrderCountDelta = 0;
                    int nSourceArrivedCountDelta = 0;

                    nSourceOrderCountDelta--;
                    if (cell.item != null
                        && cell.item.Calculated == false)
                        nSourceArrivedCountDelta--;

                    group.RefreshGroupMembersOrderInfo(nSourceOrderCountDelta,
                        nSourceArrivedCountDelta);
                }
                return;
            }

            Debug.Assert(cell.item == null, "");

            if (nCol < issue.Cells.Count)
            {
                issue.SetCell(nCol, null);  // ��ֹHoverObject Assertion
                issue.Cells.RemoveAt(nCol);
            }
        }

        // ����϶�
        // TODO: ��Ա�հ׸�����δ���
        void menuItem_releaseBinding_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strWarning = "";

            // �϶�������
            List<ItemBindingItem> parent_items = new List<ItemBindingItem>();

            List<Cell> selected_cells = this.SelectedCells;
            for (int i = 0; i < selected_cells.Count; i++)
            {
                Cell cell = selected_cells[i];
                if (IsBindingParent(cell) == true)
                {
                    // �������״̬
                    if (cell.item.Locked == true)
                    {
                        strError = "������״̬�ĺ϶��᲻�ܽ���϶�";
                        goto ERROR1;
                    }

                    // ע��cell.item.MemberCells.Count �п��ܵ���0
                    parent_items.Add(cell.item);
                }
            }

            if (parent_items.Count == 0)
            {
                strError = "��ѡ���ĵ�Ԫ�У�û�к϶���";
                goto ERROR1;
            }

            // ���̻���
            int nFixedCount = 0;
            for (int i = 0; i < parent_items.Count; i++)
            {
                ItemBindingItem parent_item = parent_items[i];

                if (CheckProcessingState(parent_item) == false)
                {
                    if (String.IsNullOrEmpty(strWarning) == false)
                        strWarning += "\r\n";
                    strWarning += parent_item.PublishTime;
                    nFixedCount++;
                }
            }

            // ����̻���
            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strError = 
                    "��ѡ���� " + parent_items.Count.ToString() + " �϶����У������вᴦ�ڹ̻�״̬�����ܲ��:\r\n\r\n"+strWarning;

                if (parent_items.Count > nFixedCount)
                {
                    strError += "\r\n\r\n�Ƿ�Ҫ������ɢ��ѡ��Χ�ڵ����� "
                        + (parent_items.Count - nFixedCount).ToString()
                        + " ���϶���?";
                    DialogResult result = MessageBox.Show(this,
                        strError,
                        "BindingControls",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return;
                }
                else
                    goto ERROR1;
            }

            strWarning = "";
            List<Point> nullpos_list = new List<Point>();
            for (int i = 0; i < parent_items.Count; i++)
            {
                ItemBindingItem parent_item = parent_items[i];

                string strPublishTime = parent_item.PublishTime;

                if (CheckProcessingState(parent_item) == false)
                {
                    if (String.IsNullOrEmpty(strWarning) == false)
                        strWarning += "; ";
                    strWarning += parent_item.PublishTime;
                    continue;
                }

                for (int j = 0; j < parent_item.MemberCells.Count; j++)
                {
                    Cell cell = parent_item.MemberCells[j];

                    // �������հ�λ��
                    Point p = GetCellPosition(cell);
                    p.X = p.X - 1;
                    nullpos_list.Add(p);

                    if (cell.item == null)
                    {
                        cell.ParentItem = null;
                    }
                    else
                    {
                        cell.item.ParentItem = null;
                        Debug.Assert(cell.item.IsMember == false, "");

                        // �޸�����Cell binded
                        Cell container_cell = cell.item.ContainerCell;
                        Debug.Assert(container_cell != null, "");
                        Debug.Assert(container_cell == cell, "");
                        container_cell.ParentItem = null;
                    }
                }

#if DEBUG
                Cell parent_cell = parent_item.ContainerCell;
                if (parent_cell != null)
                    Debug.Assert(parent_cell.IsMember == false, "");
#endif

                parent_item.MemberCells.Clear();

                /*
                parent_item.RefreshBindingXml();
                parent_item.RefreshIntact();
                parent_item.Changed = true;
                 * */
                parent_item.AfterMembersChanged();

                // Ϊ��MessageBox()��ǿ��ˢ��
                this.Invalidate();
                this.Update();

                // ѯ���Ƿ�Ҫɾ�� �϶��� ����?
                DialogResult result = MessageBox.Show(this,
    "�϶��� '" + strPublishTime + "' ����ɢ���Ƿ�˳��ɾ��ԭ�ȴ���϶���Ķ���?\r\n\r\n(Yes: ɾ����No: ������������Cancel: ������ԭ��)",
    "BindingControls",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button3);
                if (result == DialogResult.Yes)
                {
                    RemoveParentCell(parent_cell, false);

                }
                else if (result == DialogResult.No)
                {
                    RemoveParentCell(parent_cell, true);
                }
                else
                {
                    Debug.Assert(result == DialogResult.Cancel, "");

                    // ����϶���Ҫ������ԭ�أ���Ҫ��ԭ�к϶���Χ�ĵ�һ����Ա�����һ��������ƿ�����������ԭ�к϶���Χ��
                    IssueBindingItem issue = parent_item.Container;
                    int nCol = issue.IndexOfCell(parent_cell);
                    Debug.Assert(nCol != -1, "");

                    Cell first_member_cell = issue.GetCell(nCol + 1);
                    Debug.Assert(first_member_cell.IsMember == false, "�ոս���϶������ڲ������Ǻ϶���Ա����");
                    if (first_member_cell.item != null)
                    {
                        // ���ұ��ҵ�һ����λ, ��first_member_cellת�ƹ�ȥ
                        issue.GetBlankSingleIndex(nCol + 2/*, parent_item*/);
                        // issue.SetCell(nCol + 1, null);
                        issue.SetCell(nCol + 2, first_member_cell);

                        // ԭ��λ�ø�Ϊ����һ���հ׸���
                        {
                            Cell cell = new Cell();
                            cell.ParentItem = parent_item;
                            issue.SetCell(nCol + 1, cell);
                            Debug.Assert(cell.Container != null, "");
                            parent_item.InsertMemberCell(cell);
                        }
                    }

                    // ȥ����ǰ����ĵ�һ������϶���λ�ò�����Ҫ����ɾ��
                    nullpos_list.RemoveAt(0);
                }
            }

            // ɾ���հ�λ��
            for (int i = 0; i < nullpos_list.Count; i++)
            {
                Point p = nullpos_list[i];
                IssueBindingItem issue = this.Issues[p.Y];
                Debug.Assert(issue != null, "");
                Cell cell = issue.Cells[p.X];
                if (cell == null)
                {
                    // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
                    // return:
                    //      -1  �ǡ�������˫������λ��
                    //      0   ����
                    //      1   �ǡ�������˫����Ҳ�λ��
                    int nRet = issue.IsBoundIndex(p.X);
                    if (nRet == -1 || nRet == 1)
                    {
                        Debug.Assert(nRet != -1, "����˵���ﲻ̫���ܳ���˫�����");
                        Debug.Assert(nRet != 1, "");
                    }
                    else
                        issue.RemoveSingleIndex(p.X);
                }
            }

            this.Invalidate();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ���һ�����ӵ�����x/y��ע�⣬�����кţ�����������λ��
        Point GetCellPosition(Cell cell)
        {
            Debug.Assert(cell != null, "");

            IssueBindingItem issue = cell.Container;
            Debug.Assert(issue != null, "");
            int nLineNo = this.Issues.IndexOf(issue);
            Debug.Assert(nLineNo != -1, "");
            int nCol = issue.IndexOfCell(cell);
            Debug.Assert(nCol != -1, "");
            return new Point(nCol, nLineNo);
        }


        Cell GetCell(Point p)
        {
            Debug.Assert(p.X >= 0, "");
            Debug.Assert(p.Y >= 0, "");

            IssueBindingItem issue = this.Issues[p.Y];
            return issue.Cells[p.X];
        }

        // �ж�һ�������Ƿ�Ϊ�϶���
        internal bool IsBindingParent(Cell cellTest)
        {
            Debug.Assert(cellTest != null, "");

            if (cellTest.item == null)
                return false;
            if (this.ParentItems.IndexOf(cellTest.item) != -1)
                return true;
            return false;
        }

        // �ж�һ��Item�Ƿ�Ϊ�϶���
        bool IsBindingParent(ItemBindingItem itemTest)
        {
            Debug.Assert(itemTest != null, "");

            if (this.ParentItems.IndexOf(itemTest) != -1)
                return true;
            return false;
        }

        /*
        // �ж�һ�������Ƿ��������е�װ����Χ?
        // return:
        //      1   ����װ����Χ����Ϊĳ���϶���ĳ�Ա�ᡣcellParent�з����˺϶�����Ӷ���
        //      0   ������װ����Χ���������Ǻ϶���Ա�ᣬ���ǵ����϶��ᡣ
        int IsBelongToBinding(Cell cellTest,
            out Cell cellParent,
            out string strError)
        {
            cellParent = null;
            strError = "";

            for (int i = 0; i < this.ParentItems.Count; i++)
            {
                ItemBindingItem parent_item = this.ParentItems[i];
                int index = parent_item.MemberCells.IndexOf(cellTest);
                if (index != -1)
                {
                    cellParent = parent_item.ContainerCell;
                    return 1;
                }
            }

            return 0;
        }
         * */

        // ��ó�Ա���������ĺ϶���
        // ����ú϶�����ñ��������򷵻�null
        Cell GetBindingParent(Cell cellTest)
        {
            if (cellTest == null)
            {
                Debug.Assert(false, "");
                return null;
            }

            if (cellTest.ParentItem != null)
                return cellTest.ParentItem.ContainerCell;

            return null;
        }

        // �ҳ������ض����ڵĳ�Ա�����
        Cell GetMemberCellByIssue(Cell parent_cell,
            IssueBindingItem issue)
        {
            for (int i = 0; i < parent_cell.item.MemberCells.Count; i++)
            {
                Cell member_cell = parent_cell.item.MemberCells[i];

                if (member_cell.Container == issue)
                    return member_cell;
            }

            return null;
        }

        // ����ק����ʱ�Ĺ���
        void DoDragEndFunction()
        {
            string strError = "";
            int nRet = 0;

            // ���Դ�ǵ��ᣬĿ��Ϊ�϶�����Ա�ᣬ�����������ɵ������һ���϶��ᡱ
            Cell source = (Cell)this.DragStartObject;
            Cell target = null;
            NullCell target_null = null;

            if (this.DragLastEndObject is Cell)
                target = (Cell)this.DragLastEndObject;
            else if (this.DragLastEndObject is NullCell)
            {
                target_null = (NullCell)this.DragLastEndObject;
            }

            if (source == null)
                return;

            List<Cell> source_cells = new List<Cell>();

            if (this.SelectedCells.IndexOf(source) != -1)
            {
                // Դ��Ԫ�����Ѿ�ѡ��ķ�Χ
                source_cells.AddRange(this.SelectedCells);

                // TODO: ��������е�Ԫ��һ���ԣ�Ҫô������δװ���ĵ��ᣬҪô�������Ѿ�װ���ĳ�Ա��
            }
            else 
            {
                // Դ��Ԫ�������Ѿ�ѡ��ķ�Χ
                source_cells.Add(source);
            }

#if DEBUG
            VerifyListCell(source_cells);
#endif

            if (source != null
                && !(target == null && target_null == null))
            {
                Cell source_parent = null;
                Cell target_parent = null;

                // ׼��Դ�϶������
                if (IsBindingParent(source_cells[0]) == true)
                    source_parent = source_cells[0];
                else
                {
                    /*
                    nRet = IsBelongToBinding(source_cells[0],
                        out source_parent,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                     * */
                    source_parent = GetBindingParent(source_cells[0]);
                }

                // ׼��Ŀ��϶������
                if (target != null)
                {
                    if (IsBindingParent(target) == true)
                        target_parent = target;
                    else
                    {
                        // �ж�target�����Ƿ��������е�װ����Χ?
                        /*
                        nRet = IsBelongToBinding(target,
                            out target_parent,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                         * */
                        target_parent = GetBindingParent(target);
                    }
                }
                else if (target_null != null)
                {
                    target_parent = BelongToBinding(target_null);
                }

                // 1)
                // ����ĸ�������϶���Χ
                if (source_parent == null && target_parent != null)
                {
                    // �����ɵ������һ���϶���
                    nRet = AddToBinding(source_cells,
                        target_parent,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    return;
                }

                // 2)
                // �϶��ĸ������뵥�᷶Χ
                if (source_parent != null
                    && target_parent == null)
                {
                    Debug.Assert(this.ParentItems.IndexOf(source_parent.item) != -1, "Դ�϶���Ӧ����this.BindItems��Ԫ��");

                    // ���м�飬Ҫ��source_cells�����г�Ա���ǳ�Ա��
                    for (int i = 0; i < source_cells.Count;i++)
                    {
                        Cell temp = source_cells[i];
                        Cell temp_parent = null;
                        /*
                        nRet = IsBelongToBinding(temp,
                            out temp_parent,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                         * */
                        temp_parent = GetBindingParent(temp);

                        if (temp_parent == null)
                        {
                            strError = "Ҫ������ק�ĸ��Ӷ��Ǻ϶���Ա";
                            goto ERROR1;
                        }
                    }

                    // ���϶���Ա��Ӻ϶������Ƴ�����Ϊ����
                    // TODO: �п��ܳ���ǰ��memberΪNullCell�ķǳ�����������IsBinded
                    nRet = RemoveFromBinding(
                        false,
                        false,
                        source_cells,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    return;
                }

                // 3)
                // �϶��ĸ���������һ���϶���Χ
                if (source_parent != null
                    && target_parent != null)
                {
                    Debug.Assert(this.ParentItems.IndexOf(source_parent.item) != -1, "Դ�϶���Ӧ����this.BindItems��Ԫ��");
                    Debug.Assert(this.ParentItems.IndexOf(target_parent.item) != -1, "Ŀ��϶���Ӧ����this.BindItems��Ԫ��");

                    // ���м�飬Ҫ��...
                    for (int i = 0; i < source_cells.Count; i++)
                    {
                        Cell temp = source_cells[i];

                        if (temp.item != null)
                        {
                            if (StringUtil.IsInList("ע��", temp.item.State) == true)
                            {
                                strError = "��������Ϊ '" + temp.item.PublishTime + "' �Ĳ��¼״̬Ϊ��ע���������ܱ�������һ�϶���";
                                goto ERROR1;
                            }
                        }

                        // ���м�飬Ҫ��source_cells�����г�Ա���ǳ�Ա��
                        Cell temp_parent = null;
                        /*
                        nRet = IsBelongToBinding(temp,
                            out temp_parent,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                         * */
                        temp_parent = GetBindingParent(temp);

                        if (temp_parent == null)
                        {
                            strError = "Ҫ������ק�ĸ��Ӷ��Ǻ϶���Ա";
                            goto ERROR1;
                        }

                        // ���Դ�϶����Ƿ�Ϊ�̻�״̬
                        if (CheckProcessingState(temp_parent.item) == false)
                        {
                            strError = "Դ�϶��� '" + temp_parent.item.PublishTime + "' Ϊ�̻�״̬�����ܴ����ϳ�����";
                            goto ERROR1;
                        }

                        // ���Դ�϶����Ƿ�Ϊ����״̬
                        if (temp_parent.item.Locked == true)
                        {
                            strError = "Դ�϶��� '" + temp_parent.item.PublishTime + "' Ϊ����״̬�����ܴ����ϳ�����";
                            goto ERROR1;
                        }
                    }

                    // ��ҪԤ�ȼ�飬��������target_parent��Χ�Ķ����Ƿ�����е����ظ���
                    // �������飬�Ϳ������ϳ�ʱ�򲻱�����������ʱ�򱨴�
                    // TODO: ����������Ե�������������ʵ�֣�����MessageBox()ѯ��
                    for (int i = 0; i < source_cells.Count; i++)
                    {
                        Cell cell = source_cells[i];
                        Debug.Assert(cell.Container != null, "");

                        Cell dup_cell = GetMemberCellByIssue(target_parent,
                            cell.Container);
                        if (dup_cell != null && dup_cell.item != null)
                        {
                            strError = "Ŀ��϶��������г�������Ϊ " + dup_cell.Container.PublishTime + " �ĳ�Ա�ᣬ�����������������ͬ�ĳ�Ա��";
                            goto ERROR1;
                        }


                    }

                    // ���Ŀ���Ƿ�Ϊ�̻�״̬
                    if (CheckProcessingState(target_parent.item) == false)
                    {
                        strError = "Ŀ��϶��� '" + target_parent.item.PublishTime + "' Ϊ�̻�״̬�����������뵥��";
                        goto ERROR1;
                    }

                    // ���Ŀ���Ƿ�Ϊ����״̬
                    if (target_parent.item.Locked == true)
                    {
                        strError = "Ŀ��϶��� '" + target_parent.item.PublishTime + "' Ϊ����״̬�����������뵥��";
                        goto ERROR1;
                    }

                    // ���϶���Ա��Ӻ϶������Ƴ�����Ϊ��ʱ����
                    nRet = RemoveFromBinding(
                        true,
                        false,  // TODO: ��������true������AddToBinding()�Ƿ������Ӧ
                        source_cells,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

#if DEBUG
                    VerifyAll();
#endif

                    // �����ɵ������һ���϶���
                    nRet = AddToBinding(source_cells,
                        target_parent,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
#if DEBUG
                    {
                        string strError1 = "";
                        int nRet1 = target_parent.item.VerifyMemberCells(out strError1);
                        if (nRet1 == -1)
                        {
                            Debug.Assert(false, strError1);
                        }
                    }

#endif

#if DEBUG
                    VerifyAll();
#endif

                    return;
                }

                // 4)
                // ����ĸ������뵥�᷶Χ��ʵ�������ƶ�������ӵ�λ��
                if (source_parent == null && target_parent == null)
                {
                    if (source_cells.Count > 1)
                    {
                        strError = "Ŀǰֻ֧�ֽ�һ����������϶�λ��";
                        goto ERROR1;
                    }

                    // Ŀ��Ϊ��ͨ����(��NullCell)
                    if (target != null)
                    {
                        // �����ƶ�
                        if (target.Container != source_cells[0].Container)
                        {
                            Cell cell = source_cells[0];
                            if (cell.item != null && cell.item.IsParent == true
                                && cell.Container == this.FreeIssue)
                            {
                                DialogResult result = MessageBox.Show(this,
"�����������ڵĺ϶������ " + cell.item.PublishTime + "(�ο�ID:" + cell.item.RefID + ") �������������ڣ������ı�Ϊ�������ʡ�\r\n\r\n�Ƿ����?",
"BindingControls",
MessageBoxButtons.OKCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                                if (result == DialogResult.Cancel)
                                {
                                    strError = "�϶�����������";
                                    goto ERROR1;
                                }
                                cell.item.IsParent = false;
                            }

                            /*
                            strError = "�϶�ʱԴ��Ŀ��λ��Ӧ����ͬһ��";
                            goto ERROR1;
                             * */
                            // �ƶ�һ�����ӵ���ͬ����
                            nRet = MoveToAnotherIssue(source_cells[0],
                                target.Container,
                                target.Container.IndexOfCell(target),
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
                            return;
                        }

                        // ͬһ�����ƶ�
                        IssueBindingItem issue = target.Container;
                        Debug.Assert(issue != null, "");
                        int nTargetCol = issue.IndexOfCell(target);
                        Debug.Assert( nTargetCol != -1, "");
                        int nSourceCol = source_cells[0].Container.IndexOfCell(source_cells[0]);
                        Debug.Assert(nSourceCol != -1, "");

                        if (nSourceCol == nTargetCol)
                            return;

                        if (issue.IssueLayoutState == IssueLayoutState.Binding)
                        {

                            // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
                            // return:
                            //      -1  �ǡ�������˫������λ��
                            //      0   ����
                            //      1   �ǡ�������˫����Ҳ�λ��
                            if (issue.IsBoundIndex(nSourceCol) != 0)
                            {
                                strError = "Դ�����ǳ�Ա��";
                                goto ERROR1;
                            }
                            if (issue.IsBoundIndex(nTargetCol) != 0)
                            {
                                strError = "Ŀ�겻���ǳ�Ա��";
                                goto ERROR1;
                            }

                            nRet = issue.MoveSingleIndexTo(
                                nSourceCol,
                                nTargetCol,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            this.FocusObject = source_cells[0];

                            if (nRet == 0)
                                this.Invalidate();
                            else
                                this.AfterWidthChanged(true);
                        }
                        else
                        {
                            nRet = issue.MoveCellTo(
                               nSourceCol,
                               nTargetCol,
                               out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            if (nRet == 0)
                                this.Invalidate();
                            else
                                this.AfterWidthChanged(true);
                        }
                    }

                    // Ŀ��ΪNullCell
                    if (target_null != null)
                    {
                        IssueBindingItem target_issue = this.Issues[target_null.Y];
                        Debug.Assert(target_issue != null, "");

                        // �����ƶ�
                        if (target_issue != source_cells[0].Container)
                        {
                            // ���Դ�����Ƿ�Ϊ�����������ڵĺ϶����ʵĸ��ӣ�����ǣ���Ҫ�޸�item.IsParentΪfalse
                            Cell cell = source_cells[0];
                            if (cell.item != null && cell.item.IsParent == true
                                && cell.Container == this.FreeIssue)
                            {
                                DialogResult result = MessageBox.Show(this,
"�����������ڵĺ϶������ " + cell.item.PublishTime + "(�ο�ID:"+cell.item.RefID+") �������������ڣ������ı�Ϊ�������ʡ�\r\n\r\n�Ƿ����?",
"BindingControls",
MessageBoxButtons.OKCancel,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                                if (result == DialogResult.Cancel)
                                {
                                    strError = "�϶�����������";
                                    goto ERROR1;
                                }
                                cell.item.IsParent = false;
                            }
                            /*
                            strError = "�϶�ʱԴ��Ŀ��λ��Ӧ����ͬһ��";
                            goto ERROR1;
                             * */
                            // �ƶ�һ�����ӵ���ͬ����
                            nRet = MoveToAnotherIssue(source_cells[0],
                                target_issue,
                                target_null.X,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
                            return;
                        }

                        // ͬһ�����ƶ�
                        int nTargetCol = target_null.X;
                        Debug.Assert(nTargetCol != -1, "");
                        int nSourceCol = source_cells[0].Container.IndexOfCell(source_cells[0]);
                        Debug.Assert(nSourceCol != -1, "");

                        if (nSourceCol == nTargetCol)
                            return;

                        if (target_issue.IssueLayoutState == IssueLayoutState.Binding)
                        {
                            if (target_issue.IsBoundIndex(nSourceCol) != 0)
                            {
                                strError = "Դ�����ǳ�Ա��";
                                goto ERROR1;
                            }
                            if (target_issue.IsBoundIndex(nTargetCol) != 0)
                            {
                                strError = "Ŀ�겻���ǳ�Ա��";
                                goto ERROR1;
                            }

                            nRet = target_issue.MoveSingleIndexTo(
                                nSourceCol,
                                nTargetCol,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            this.FocusObject = source_cells[0];

                            if (nRet == 0)
                                this.Invalidate();
                            else
                                this.AfterWidthChanged(true);
                        }
                        else
                        {
                            nRet = target_issue.MoveCellTo(
    nSourceCol,
    nTargetCol,
    out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            if (nRet == 0)
                                this.Invalidate();
                            else
                                this.AfterWidthChanged(true);
                        }

                    } 
                    return;
                }
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �ƶ�һ�����ӵ���ͬ����
        // parameters:
        //      nInsertIndex   Ҫ����ĵ���index
        int MoveToAnotherIssue(Cell source_cell,
            IssueBindingItem target_issue,
            int nInsertIndex,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (source_cell.Container == target_issue)
            {
                strError = "��������ͬһ�ڣ����ǿ����ƶ�";
                return -1;
            }

            if (source_cell is GroupCell)
            {
                strError = "�����ƶ�����β����";
                return -1;
            }

#if DEBUG
            if (source_cell.item != null)
            {
                Debug.Assert(source_cell.item.IsParent == false, "������ֻ�ܴ���Ǻ϶���");
            }
#endif

            Debug.Assert(source_cell.IsMember == false, "������ֻ�ܴ���ǳ�Ա�ĵ���");

            IssueBindingItem source_issue = source_cell.Container;
            Debug.Assert(source_issue != null, "");

            string strOldVolumeString =
                VolumeInfo.BuildItemVolumeString(
                IssueUtil.GetYearPart(source_issue.PublishTime),
                source_issue.Issue,
                        source_issue.Zong,
                        source_issue.Volume);
            string strNewVolumeString =
                VolumeInfo.BuildItemVolumeString(
                IssueUtil.GetYearPart(target_issue.PublishTime),
                target_issue.Issue,
            target_issue.Zong,
            target_issue.Volume);

            string strMessage = "�Ƿ�Ҫ���� " 
                + source_issue.PublishTime 
                + " �еĸ����ƶ��� �� " 
                + target_issue.PublishTime 
                + " �У�\r\n\r\n�������ӳ��˳������ڻ�ı�Ϊ "
                +target_issue.PublishTime
                +" ���⣬���ں�Ҳ���� '"
                + strOldVolumeString 
                + "' ��Ϊ '" + strNewVolumeString + "'��";

            DialogResult result = MessageBox.Show(this,
    strMessage,
    "BindingControls",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
            if (result == DialogResult.No)
                return 0;   // ������

            // GroupCell source_group = null;

            // ��ԭ����������
            int nOldCol = source_issue.IndexOfCell(source_cell);
            Debug.Assert(nOldCol != -1, "");
            if (nOldCol != -1)
            {
                /*
                Cell temp = source_issue.GetCell(nOldCol - 1);
                Debug.Assert(temp == null || temp.item == null, "˫������λ��Ӧ��û������");
                 * */
                if (source_issue.IssueLayoutState == IssueLayoutState.Binding)
                {
                    // �����λ��

                    if (source_cell.item != null
                        && source_cell.item.OrderInfoPosition.X != -1)
                    {
                        // ɾ��ǰ��������Ҫ���¶�����Ϣ
                        nRet = source_cell.item.DoDelete(out strError);
                        if (nRet == -1)
                            return -1;
                    }

                    source_issue.RemoveSingleIndex(nOldCol);

                    // ʧȥ�Ͷ�����Ĺ�ϵ����Ϊ�ƻ������
                    if (source_cell.item != null)
                    {
                        source_cell.item.OrderInfoPosition.X = -1;
                        source_cell.item.OrderInfoPosition.Y = -1;
                    }
                }
                else
                {
                    Debug.Assert(source_issue.IssueLayoutState == IssueLayoutState.Accepting, "");

                    /*
                    if (source_cell.item != null)
                    {
                        if (source_cell.item.Calculated == true
                            || source_cell.item.OrderInfoPosition.X != -1)
                        {
                            source_group = source_cell.item.GroupCell;
                        }
                    }
                    if (source_issue.Cells.Count > nOldCol)
                        source_issue.Cells.RemoveAt(nOldCol);

                    if (source_group != null)
                    {
                        int nSourceOrderCountDelta = 0;
                        int nSourceArrivedCountDelta = 0;
                        nSourceOrderCountDelta--;
                        if (source_cell.item != null
                            && source_cell.item.Calculated == false)
                            nSourceArrivedCountDelta--;

                        source_group.RefreshGroupMembersOrderInfo(nSourceOrderCountDelta,
            nSourceArrivedCountDelta);
                    }
                    */
                    DeleteOneCellInAcceptingLayout(source_cell);
                }
            }

            // ��Ҫ���Ŀ��λ�ã������Ǻ϶�����ռ�ݵ���
            // ���ܻ�ı��֣�nSourceNo������Ч
            if (nInsertIndex != -1)
            {
            }
            else
            {
                // TODO: ��Ϊ���ĩβ�ĵ�һ����λ
                if (target_issue.IssueLayoutState == IssueLayoutState.Binding)
                    nInsertIndex = target_issue.GetFirstAvailableSingleInsertIndex();
                else
                    nInsertIndex = target_issue.GetFirstFreeIndex();
            }

            if (target_issue.IssueLayoutState == IssueLayoutState.Binding)
                target_issue.GetNewSingleIndex(nInsertIndex);
            else
            {
                if (nInsertIndex < target_issue.Cells.Count)
                    target_issue.Cells.Insert(nInsertIndex, null);
            }

            target_issue.SetCell(nInsertIndex, source_cell);
            if (source_cell.item != null)
                source_cell.item.Container = target_issue;

            GroupCell target_group = null;  // ֻ����Acceptionģʽ�²ſ�����ֵ

            if (target_issue.IssueLayoutState == IssueLayoutState.Accepting)
                target_group = target_issue.BelongToGroup(nInsertIndex);

            bool bSourceHasOrderInfo = false;
            
            if (source_cell.item != null)
                bSourceHasOrderInfo = source_cell.item.OrderInfoPosition.X != -1;

            // ���Դ�����������϶����ƻ�������
            if (bSourceHasOrderInfo && target_group == null)
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
            if (bSourceHasOrderInfo == false && target_group != null)
            {
                if (source_cell.item == null)
                {
                    // �հ׸���Ҫ��ΪԤ���ʽ
                    source_cell.item = new ItemBindingItem();
                    source_cell.item.Container = target_issue;
                    source_cell.item.Initial("<root />", out strError);
                    source_cell.item.RefID = "";
                    source_cell.item.LocationString = "";
                    source_cell.item.Calculated = true;
                    IssueBindingItem.SetFieldValueFromOrderInfo(
                        false,
                        source_cell.item,
                        target_group.order);
                }
            }

            // 2010/9/21
            // ���ƶ��������ڵ�Ԥ��״̬�ĸ��ӱ�Ϊ�հ׸���
            if (source_cell.item != null)
            {
                if (String.IsNullOrEmpty(source_cell.Container.PublishTime) == true
        && source_cell.item.Calculated == true)
                {
                    // Ԥ����ӱ�Ϊ��ͨ�հ׸���
                    source_cell.item = null;
                }
            }

            Cell target_cell = source_cell;
            if (target_cell.item != null)
            {
                // �޸Ĳ��¼�ڵ��ֶ�
                target_cell.item.Volume = strNewVolumeString;
                target_cell.item.PublishTime = target_issue.PublishTime;
                target_cell.item.Changed = true;
            }
            target_cell.RefreshOutofIssue();

            if (target_group != null)
            {
                int nTargetOrderCountDelta = 0;
                int nTargetArrivedCountDelta = 0;
                nTargetOrderCountDelta++;
                if (source_cell.item != null
                    && source_cell.item.Calculated == false)
                    nTargetArrivedCountDelta++;
                target_group.RefreshGroupMembersOrderInfo(nTargetOrderCountDelta,
    nTargetArrivedCountDelta);
            }

            //this.Invalidate();   // TODO: �޸�Ϊ����ʧЧ��Դ���Ӻ����ұߵ�����Ŀ����Ӻ��ұߵ�����
            this.AfterWidthChanged(true);

            return 1;   // ����
        }

        // ����ѡ��ĸ�����ѡ���ں���С��һ������������к�
        int DetectFirstMemberCol(List<Cell> members)
        {
            Debug.Assert(members.Count > 0, "");
            members.Sort(new CellPublishTimeComparer());
            Cell cell = members[0];
            return cell.Container.Cells.IndexOf(cell);
        }

        // �����ɵ������һ���϶���
        int AddToBinding(List<Cell> singles,
            Cell parent,
            out string strError)
        {
            strError = "";

            if (CheckProcessingState(parent.item) == false
                && parent.item.Calculated == false   // Ԥ����ӳ���
                    && parent.item.Deleted == false)  // �Ѿ�ɾ���ĸ��ӳ���
            {
                strError = "�϶��� '" + parent.item.PublishTime + "' Ϊ�̻�״̬�������ټ��뵥��";
                return -1;
            }

            if (parent.item.Locked == true
    && parent.item.Calculated == false   // Ԥ����ӳ���
        && parent.item.Deleted == false)  // �Ѿ�ɾ���ĸ��ӳ���
            {
                strError = "�϶��� '" + parent.item.PublishTime + "' Ϊ����״̬�������ټ��뵥��";
                return -1;
            }

            // 2010/4/6
            parent.item.Calculated = false;
            parent.item.Deleted = false;
            if (String.IsNullOrEmpty(parent.item.RefID) == true)
                parent.item.RefID = Guid.NewGuid().ToString();

            // ��飺singles�еĳ�Ա��Ӧ�ú�parent�����ĸ��ӵ��ڲ��ص�
            for (int i = 0; i < singles.Count; i++)
            {
                Cell single = singles[i];
                IssueBindingItem issue = single.Container;

                Debug.Assert(issue.Cells.IndexOf(single) != -1, "");

                for (int j = 0; j < parent.item.MemberCells.Count; j++)
                {
                    Cell exist_cell = parent.item.MemberCells[j];
                    if (exist_cell.Container == single.Container
                        && exist_cell.item != null)
                    {
                        strError = "�϶������Ѿ������˳�������Ϊ '" + issue.PublishTime + "' ��(�ǿհ�)��(����)�������ظ�����";
                        return -1;
                    }
                }
            }

            // ��鵥�ᣬ��ע����״̬�Ĳ��ܼ���϶���Χ
            // Ԥ��״̬�Ĳ��ܼ���϶���Χ
            for (int i = 0; i < singles.Count; i++)
            {
                Cell single = singles[i];
                Debug.Assert(single != null, "");

                if (single is GroupCell)
                {
                    strError = "��������β���Ӳ��ܲ���϶�";
                    return -1;
                }

                if (String.IsNullOrEmpty(single.Container.PublishTime) == true)
                {
                    strError = "�����������ڵĸ��Ӳ��ܼ���϶���Χ";
                    return -1;
                }

                if (single.item == null)
                    continue;

                if (StringUtil.IsInList("ע��", single.item.State) == true)
                {
                    strError = "��������Ϊ '" + single.item.PublishTime + "' �ĵ����¼״̬Ϊ��ע���������ܼ���϶���Χ";
                    return -1;
                }

                if (String.IsNullOrEmpty(single.item.Borrower) == false)
                {
                    strError = "��������Ϊ '" + single.item.PublishTime + "' �ĵ����¼Ŀǰ���ڱ�����״̬�����ܼ���϶���Χ";
                    return -1;
                }

                if (single.item.Calculated == true)
                {
                    strError = "��������Ϊ '" + single.item.PublishTime + "' �ĸ���ΪԤ��״̬�����ܼ���϶���Χ";
                    return -1;
                }
            }

            int nCol = -1;

            if (parent.Container != null)
            {
                nCol = parent.Container.Cells.IndexOf(parent);
                Debug.Assert(nCol != -1, "");
                nCol++;
            }
            else
            {
                nCol = DetectFirstMemberCol(singles);
                Debug.Assert(nCol != -1, "");

                /*
                // ���������û�п�һ��λ�á�����У���ֱ��ռ�ã����û�У���ƫ��ռ��
                IssueBindingItem issue = singles[0].Container;
                Debug.Assert(issue != null, "");
                 * */
                nCol++;
            }

            // ���������ĵ�����
            PlaceMemberCells(parent,
                singles,
                nCol);
            try
            {
                SetBindingRange(parent, true);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            /*
            parent.item.RefreshBindingXml();
            parent.item.RefreshIntact();
            parent.item.Changed = true;
             * */

            /*
            PlaceBinding(
                parent,
                singles,
                out strPublishTimeString);
            parent.item.PublishTime = strPublishTimeString;

            // ����<binding>Ԫ����Ƭ��
            parent.item.RefreshBindingXml();

            parent.item.Changed = true;
             * */

#if DEBUG
            {
                string strError1 = "";
                int nRet1 = parent.item.VerifyMemberCells(out strError1);
                if (nRet1 == -1)
                {
                    Debug.Assert(false, strError1);
                }
            }

            VerifyAll();
#endif

            this.AfterWidthChanged(true);
            return 0;
        }

        // ���϶���Ա��Ӻ϶������Ƴ�����Ϊ����
        // ע����Щ��Ա����ܲ���������ͬһ���϶���
        // TODO: ��Ҫ������һ�����ܣ��Ƴ��Ķ������һ�����飬�����ؽ���ʵ����ʾ�ĸ��ӡ���Щ��������Żᱻ������������һ���϶���
        // parameters:
        //      bShrink �Ƿ���������λλ�õĸ���ʱ��Сװ����Χ
        //      bDelete �Ƿ�ɾ���Ƴ��ĸ��ӡ�==false�����Ƴ������棬�����ڣ�==true���򲻴���
        int RemoveFromBinding(
            bool bShrink,
            bool bDelete,
            List<Cell> members,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (bDelete == true)
            {
                // ���
                for (int i = 0; i < members.Count; i++)
                {
                    Cell member_cell = members[i];
                    if (member_cell.item != null)
                    {
                        nRet = member_cell.item.CanDelete(out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet == 0)
                        {
                            strError = "�� "+member_cell.item.RefID+" ���ܱ�ɾ��: " + strError;
                            return -1;
                        }
                    }
                }
            }

            bool bClearAsBlank = true;  // �Ƿ�Ҫ��ɾ��λ�����հ׸��ӡ�==true��ʾҪ��䣻�������null����

            if (bShrink == true)
                bClearAsBlank = false;

            List<Cell> parent_cells = new List<Cell>(); // ���ͳһ����SetBindingRange()
            for (int i = 0; i < members.Count; i++)
            {
                Cell member_cell = members[i];

                ItemBindingItem parent_item = member_cell.ParentItem;
                if (parent_item == null)
                    continue;   // TODO: �Ƿ�Ҫ����?

                if (CheckProcessingState(parent_item) == false)
                {
                    strError = "�϶��� '" + parent_item.PublishTime + "' Ϊ�̻�״̬�����ܴ����Ƴ�����";
                    return -1;
                }

                // ���϶��������״̬
                if (parent_item.Locked == true)
                {
                    strError = "�϶��� '" + parent_item.PublishTime + "' Ϊ����״̬�����ܴ����Ƴ�����";
                    return -1;
                }

                Cell parent_cell = parent_item.ContainerCell;
                Debug.Assert(parent_cell != null, "");


                IssueBindingItem issue = member_cell.Container;
                Debug.Assert(issue != null, "");


                int nOldCol = issue.Cells.IndexOf(member_cell);
                Debug.Assert(nOldCol != -1, "");

                // 2010/3/3 
                bool bLastPos = false;
                // ��������г�Ա�����һ�������Һ�Parentͬ��һ��
                if (parent_item.MemberCells.Count <= 1
                    && parent_item.Container == member_cell.Container)
                {
                    bLastPos = true;
                }

                // ���ͬһ�ڵ�ĩβ/��һ������λ��
                int nNewCol = -1;
                if (bDelete == false || bLastPos == true)
                {
                    if (issue.IssueLayoutState == IssueLayoutState.Binding)
                    {
                        nNewCol = issue.GetFirstAvailableSingleInsertIndex();
                        Debug.Assert(nNewCol != -1, "");
                        Debug.Assert(nNewCol > nOldCol, "");
                    }
                    else
                    {
                        Debug.Assert(issue.IssueLayoutState == IssueLayoutState.Accepting, "");
                        nNewCol = issue.GetFirstFreeBlankIndex();
                        Debug.Assert(nNewCol != -1, "");
                    }
                }



                // ��ԭ��λ����ӿհ׸���
                // �������壺1)��������С�϶���Χ; 2)��ҪʱҪ��С�϶���Χ���ر������һ���϶������ߵ�ʱ��Ҫ����? �϶�����û�г�Ա����϶���ô��Ҫ��Ȼ����������

                bool bOldSeted = false; // ��λ���Ƿ��Ѿ���������?

                if (bClearAsBlank == false
                    && bDelete == true
                    ) // && bLastPos == false
                {
                    // �����Ƿ��ж�����Ϣ��
                    if (member_cell.item != null
                        && member_cell.item.OrderInfoPosition.X != -1)
                    {
                        nRet = member_cell.item.DoDelete(out strError);
                        if (nRet == -1)
                            return -1;
                    }
                    // ���ΪNullCell
                    issue.SetCell(nOldCol, null);
                    if (issue.IssueLayoutState == IssueLayoutState.Accepting)
                    {
                        if (nOldCol < issue.Cells.Count)
                        {
                            issue.Cells.RemoveAt(nOldCol);
                            if (nNewCol != -1)
                                nNewCol = issue.GetFirstFreeBlankIndex();   // ��������
                        }
                    }
                    bOldSeted = true;
                }

                parent_item.MemberCells.Remove(member_cell);
                this.m_bChanged = true;

                if (bClearAsBlank == true || bLastPos == true)
                {
                    // ���Ϊ�հ׸���
                    Cell blank_cell = new Cell();
                    blank_cell.Container = issue;
                    blank_cell.ParentItem = parent_item;
                    parent_item.InsertMemberCell(blank_cell);
                    if (issue.IssueLayoutState == IssueLayoutState.Binding)
                    {
                        issue.SetCell(nOldCol, blank_cell);
                        bOldSeted = true;
                    }
                    else
                    {
                        Debug.Assert(nNewCol != -1, "");
                        issue.SetCell(nNewCol, blank_cell); // ��λ�ô����հ׵ĸ��ӣ���Ϊ�϶���Ա
                    }
                }

                if (bDelete == false
                    && issue.IssueLayoutState == IssueLayoutState.Binding)
                {
                    // ��װ�������£����ı䶩���󶨵��ƶ�������������У�
                    // ���ص���DoDelete()����
                    if (bOldSeted == false)
                    {
                        // ���ΪNullCell
                        Debug.Assert(nOldCol != -1, "");
                        issue.SetCell(nOldCol, null);
                        bOldSeted = true;
                    }

                    // ��һ����λ�ð���
                    Debug.Assert(nNewCol != -1, "");
                    issue.GetNewSingleIndex(nNewCol);
                    issue.SetCell(nNewCol, member_cell);
                }

                if (member_cell.item != null)
                    member_cell.item.ParentItem = null;  // ��Ϊ�ǳ�Ա
                member_cell.ParentItem = null; // ��Ϊ�ǳ�Ա

                // ���װ����Χ�����仯
                // parent_item.PublishTime = strPublishTimeString;

                /*
                parent_item.RefreshBindingXml();
                parent_item.RefreshIntact();
                parent_item.Changed = true;
                 * */

                if (bShrink == true)
                {
                    /*
#if DEBUG
                    if (parent_cell.item.MemberCells.Count == 0)
                    {
                        VerifyAll();
                    }
#endif
                     * */
                    /*
                    try
                    {
                        SetBindingRange(parent_cell, true);
                    }
                    catch (Exception ex)
                    {
                        strError = ex.Message;
                        return -1;
                    }
                     * */

                    // ����ȥ�ص�����
                    if (parent_cells.IndexOf(parent_cell) == -1)
                        parent_cells.Add(parent_cell);
                }
                else
                {
                    parent_cell.item.AfterMembersChanged(); // ��Ȼ��ѹ����Χ�����ǳ�Ա���ܷ����仯

#if DEBUG
                    VerifyListCell(parent_item.MemberCells);
#endif

#if DEBUG
                    {
                        string strError1 = "";
                        int nRet1 = parent_item.VerifyMemberCells(out strError1);
                        if (nRet1 == -1)
                        {
                            Debug.Assert(false, strError1);
                        }
                    }
#endif
                }

            }

            // ͳһ���д���
            for (int i = 0; i < parent_cells.Count; i++)
            {
                Cell parent_cell = parent_cells[i];
                try
                {
                    SetBindingRange(parent_cell, true);
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }
#if DEBUG
                VerifyListCell(parent_cell.item.MemberCells);
#endif

#if DEBUG
                {
                    string strError1 = "";
                    int nRet1 = parent_cell.item.VerifyMemberCells(out strError1);
                    if (nRet1 == -1)
                    {
                        Debug.Assert(false, strError1);
                    }
                }
#endif
            }


#if DEBUG
            VerifyAll();
#endif

            this.AfterWidthChanged(true);
            return 0;
        }

        // ���ܻ��׳��쳣
        // �����϶�������ķ�Χ������в���NullCell��
        // β������Ҫ��飬ֻ��Ҫ���ͷ��
        // parameters:
        //      bBackSetParent  �Ƿ�Ҫ�ѳ�Ա��Ϣ���޸Ķ��ֵ�parent����?
        void SetBindingRange(Cell parent_cell,
            bool bBackSetParent)
        {
            int nCol = -1;

            if (parent_cell.item.MemberCells.Count == 0)
            {
                ItemBindingItem item = parent_cell.item;
                IssueBindingItem issue = parent_cell.Container;

                if (issue == null)
                {
                    Debug.Assert(false, "");
                    goto END1;
                }

                if (issue.IssueLayoutState == IssueLayoutState.Binding)
                {
                    nCol = issue.Cells.IndexOf(parent_cell);
                    if (nCol == -1)
                    {
                        Debug.Assert(false, "");
                        goto END1;
                    }
                    nCol++;
                }
                else
                {
                    // ����ҵ�һ���հ׸���(����Ҫ����)
                    nCol = issue.GetFirstFreeBlankIndex();
                }

                // �������
                {
                    int nSetCol = -1;
                    if (issue.IssueLayoutState == IssueLayoutState.Binding)
                    {
                        if (issue.IsBlankDoubleIndex(nCol - 1) == true)
                        {
                            Cell cell = issue.GetCell(nCol);
                            if (cell == null)
                            {
                                cell = new Cell();
                                issue.SetCell(nCol, cell);
                            }
                            Debug.Assert(cell != null
                                && cell.item == null
                                && cell.IsMember == false, "");
                            cell.ParentItem = parent_cell.item;
                            parent_cell.item.InsertMemberCell(cell);
                            goto END1;
                        }
                        // issue.GetBlankPosition(nCol / 2, parent_cell.item);
                        issue.GetBlankDoubleIndex(nCol - 1,
                            parent_cell.item,
                            null);
                        nSetCol = nCol;
                    }
                    else
                    {
                        // ����ҵ�һ���հ׸���(����Ҫ����)���ӽ�nColλ�ø���
                        nSetCol = issue.GetFirstFreeBlankIndex();
                        Debug.Assert(nSetCol != -1, "");
                    }

                    {
                        Cell cell = new Cell();
                        cell.item = null;   // ֻ��ռ��λ��
                        cell.ParentItem = parent_cell.item;
                        issue.SetCell(nSetCol, cell);

                        // ���ں���λ��
                        parent_cell.item.InsertMemberCell(cell);
                    }
                }

            END1:
                if (bBackSetParent == true)
                    parent_cell.item.AfterMembersChanged();
                return;
            }

            // string strPublishTimeString = "";

            List<IssueBindingItem> done_issues = new List<IssueBindingItem>();
            int nFirstLineNo = 99999;
            int nLastLineNo = -1;
            for (int i = 0; i < parent_cell.item.MemberCells.Count; i++)
            {
                Cell cell = parent_cell.item.MemberCells[i];

                ItemBindingItem item = cell.item;
                IssueBindingItem issue = cell.Container;

                if (issue == null)
                {
                    Debug.Assert(false, "");
                }

                Debug.Assert(issue != null, "");

                Debug.Assert(issue.Cells.IndexOf(cell) != -1, "");

                done_issues.Add(issue);

                int nIssueLineNo = this.Issues.IndexOf(issue);
                if (nFirstLineNo > nIssueLineNo)
                    nFirstLineNo = nIssueLineNo;

                if (nLastLineNo < nIssueLineNo)
                    nLastLineNo = nIssueLineNo;
            }

            Debug.Assert(nFirstLineNo != 99999, "");
            Debug.Assert(nLastLineNo != -1, "");

            // bool bChanged = false;

            // �϶�����ӷ����䶯
            IssueBindingItem first_issue = this.Issues[nFirstLineNo];
            Debug.Assert(first_issue != null, "");

            int nOldCol = -1;
            IssueBindingItem old_first_issue = parent_cell.Container;
            if (old_first_issue != null)
            {
                nOldCol = old_first_issue.Cells.IndexOf(parent_cell);
                Debug.Assert(nOldCol != -1, "");
            }

            // ����first_issue
            {
                if (first_issue.IssueLayoutState == IssueLayoutState.Binding)
                {
                    Debug.Assert(first_issue != null, "");
                    for (int i = 0; i < parent_cell.item.MemberCells.Count; i++)
                    {
                        nCol = first_issue.Cells.IndexOf(parent_cell.item.MemberCells[i]);
                        if (nCol != -1)
                            break;
                    }
                    Debug.Assert(nCol != -1, "");
                    nCol--;
                }
                else
                {
                    // ����ҵ�һ���հ׸���(����Ҫ����)
                    nCol = first_issue.GetFirstFreeBlankIndex();
                }
            }

            // ����϶����в�������һ����Ա�����
            if (parent_cell.Container != first_issue)
            {
                if (old_first_issue != null)
                    old_first_issue.SetCell(nOldCol, null);

                // TODO: �����ҵ�binding���ֵ�һ����Ա�Ѿ��ù����У������Ͳ��ðᶯ��Ա��

                first_issue.SetCell(nCol, parent_cell);
                // 2010/3/29
                // TODO: ����µ�parent������Ϊbinding���֣���ôҪ������binding���ֵĳ�Ա���еĸ�����λ����������parent����λ�ù�ϵ
                PlaceMemberCells(parent_cell,
                    parent_cell.item.MemberCells,
                    nCol + 1);
                // goto REDO;

                Debug.Assert(parent_cell.Container == first_issue, "");
                parent_cell.item.Container = first_issue;

                // bChanged = true;
            }

            /*
            strPublishTimeString = this.Issues[nFirstLineNo].PublishTime
            + "-"
            + this.Issues[nLastLineNo].PublishTime;

            if (parent_cell.item.PublishTime != strPublishTimeString)
                bChanged = true;
             * */


            nCol++; // nCol��Accepting�в�������
            // �������
            for (int i = nFirstLineNo; i <= nLastLineNo; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                if (done_issues.IndexOf(issue) != -1)
                    continue;

                int nSetCol = -1;
                if (issue.IssueLayoutState == IssueLayoutState.Binding)
                {
                    if (issue.IsBlankDoubleIndex(nCol - 1) == true)
                    {
                        Cell cell = issue.GetCell(nCol);
                        if (cell == null)
                        {
                            cell = new Cell();
                            issue.SetCell(nCol, cell);
                        }
                        Debug.Assert(cell != null
                            && cell.item == null
                            && cell.IsMember == false, "");
                        cell.ParentItem = parent_cell.item;
                        parent_cell.item.InsertMemberCell(cell);
                        continue;
                    }

                    /*
                    {
                        Cell cell = issue.GetCell(nCol);
                        // ����ǿհ׸��ӣ�������������ֱ��ʹ��
                        // TODO: �е����⣿����հ׸��ӵ��󷽵ĸ����أ��Ƿ�Ϊ��ռ�ݵ�?
                        if (cell != null
                            && cell.item == null
                            && cell.IsMember == false)
                        {
                            cell.ParentItem = parent_cell.item;
                            parent_cell.item.InsertMemberCell(cell);
                            // bChanged = true;
                            continue;
                        }
                    }
                     * */

                    // issue.GetBlankPosition(nCol / 2, parent_cell.item);
                    issue.GetBlankDoubleIndex(nCol - 1,
                        parent_cell.item,
                        null);
                    nSetCol = nCol;
                }
                else
                {
                    // ����ҵ�һ���հ׸���(����Ҫ����)���ӽ�nColλ�ø���
                    nSetCol = issue.GetFirstFreeBlankIndex();
                    Debug.Assert(nSetCol != -1, "");
                }

                {
                    Cell cell = new Cell();
                    cell.item = null;   // ֻ��ռ��λ��
                    cell.ParentItem = parent_cell.item;
                    issue.SetCell(nSetCol, cell);

                    // ���ں���λ��
                    parent_cell.item.InsertMemberCell(cell);
                    // bChanged = true;
                }
            }

            if (bBackSetParent == true)
                parent_cell.item.AfterMembersChanged();
        }

        void RemoveParentCell(Cell cell,
            bool bAddToFree)
        {
            Debug.Assert(cell.item != null, "");

            if (this.ParentItems.IndexOf(cell.item) == -1)
            {
                Debug.Assert(false, "");
                return;
            }

            // ����ʾ������ȥ��
            // ��ԭ������������������
            // TODO: ����ѹ��?
            int index = cell.Container.Cells.IndexOf(cell);
            Debug.Assert(index != -1, "");
            cell.Container.SetCell(index, null);

            // ����װ���Ἧ��������
            this.ParentItems.Remove(cell.item);

            /*
            IssueBindingItem issue = item.Container;
            Debug.Assert(issue != null, "");
            if (issue != null)
            {
                int nIndex = issue.IndexOfItem(item);
                Debug.Assert(nIndex != -1, "");
                if (nIndex != -1)
                {
                    Cell cell = issue.GetCell(nIndex);
                    cell.item = null;
                    cell.ParentItem = null;
                }
            }
             * */
            // ����������
            if (bAddToFree == true)
                AddToFreeIssue(cell);
        }

        /*
        void RemoveBindItem(ItemBindingItem item,
    bool bAddToFree)
        {
            if (this.BindItems.IndexOf(item) == -1)
                return;

            // ����װ���Ἧ��������
            this.BindItems.Remove(item);

            // ��ԭ������������������
            IssueBindingItem issue = item.Container;
            Debug.Assert(issue != null, "");
            if (issue != null)
            {
                int nIndex = issue.IndexOfItem(item);
                Debug.Assert(nIndex != -1, "");
                if (nIndex != -1)
                {
                    Cell cell = issue.GetCell(nIndex);
                    cell.item = null;
                    cell.ParentItem = null;
                }
            }

            // ����������
            if (bAddToFree == true)
                AddToFreeIssue(item);
        }
         * */

        /*
        // ����������
        void AddToFreeIssue(ItemBindingItem item)
        {
            Debug.Assert(this.FreeIssue != null, "");

            Cell cell = new Cell();
            cell.item = item;
            this.FreeIssue.AddCell(cell);
            item.Container = this.FreeIssue;
        }
         * */

        // ����������
        void AddToFreeIssue(Cell cell)
        {
            Debug.Assert(cell.item != null, "");
            Debug.Assert(this.FreeIssue != null, "");

            this.FreeIssue.AddCell(cell);
            Debug.Assert(cell.Container == this.FreeIssue, "");
            cell.item.Container = this.FreeIssue;
        }

        public List<Cell> SelectedCells
        {
            get
            {
                List<Cell> results = new List<Cell>();
                for (int i = 0; i < this.Issues.Count; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    List<Cell> temp = issue.SelectedCells;
                    if (temp.Count > 0)
                        results.AddRange(temp);
                }

                return results;
            }
        }

        public List<IssueBindingItem> SelectedIssues
        {
            get
            {
                List<IssueBindingItem> results = new List<IssueBindingItem>();
                for (int i = 0; i < this.Issues.Count; i++)
                {
                    IssueBindingItem issue = this.Issues[i];
                    if (issue.Selected == true)
                        results.Add(issue);
                }

                return results;
            }
        }

        // �Ƿ��е�Ԫ��ѡ��?
        public bool HasCellSelected()
        {
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                if (issue.HasCellSelected() == true)
                    return true;
            }

            return false;
        }

        // �Ƿ��е�Ԫ��ѡ��?
        public bool HasIssueSelected()
        {
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                if (issue.Selected == true)
                    return true;
            }

            return false;
        }

        /*
        // ����Ƿ�һ��ֻ��һ���ᡣ�ڿ��Բ�����
        // ��飺һ��ֻ����һ������롣Ҳ����˵��ÿ�����Container������ͬ
        // return:
        //      -1  error
        //      0   �ϸ�
        //      1   ���ϸ�strError������ʾ
        static int CheckBindingItems(List<ItemBindingItem> items,
            out string strError)
        {
            strError = "";

            for (int i = 0; i < items.Count; i++)
            {
                ItemBindingItem item = items[i];
                if (item == null)
                    continue;

                for (int j = i + 1; j < items.Count; j++)
                {
                    ItemBindingItem item1 = items[j];
                    if (item1 == null)
                        continue;

                    if (item.Container == item1.Container)
                    {
                        strError = "��ͬ����һ�� (" + item.PublishTime + ") �Ķ��";
                        return 1;
                    }
                }
            }

            return 0;
        }
         * */

        // ����Ƿ�һ��ֻ��һ���ᡣ�ڿ��Բ�����
        // ��飺һ��ֻ����һ������롣Ҳ����˵��ÿ�����Container������ͬ
        // return:
        //      -1  error
        //      0   �ϸ�
        //      1   ���ϸ�strError������ʾ
        static int CheckBindingCells(List<Cell> cells,
            out string strError)
        {
            strError = "";

            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (cell == null)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                if (cell.item != null
                    && StringUtil.IsInList("ע��", cell.item.State) == true)
                {
                    strError = "��������Ϊ '" + cell.item.PublishTime + "' �ĵ����¼״̬Ϊ��ע���������ܼ���϶���Χ";
                    return 1;
                }

                if (cell.item != null
    && String.IsNullOrEmpty(cell.item.Borrower) == false)
                {
                    strError = "��������Ϊ '" + cell.item.PublishTime + "' �ĵ����¼Ŀǰ���ڱ�����״̬�����ܼ���϶���Χ";
                    return 1;
                }

                /*
                if (cell.item == null)
                    continue;
                 * */
                Debug.Assert(cell.Container != null, "");

                ItemBindingItem item = cell.item;
                for (int j = i+1; j < cells.Count; j++)
                {
                    Cell cell1 = cells[j];

                    Debug.Assert(cell1.Container != null, "");

                    if (cell.Container == cell1.Container)
                    {
                        strError = "��ͬ����һ�� ("+cell.Container.PublishTime+") �Ķ��";
                        return 1;
                    }
                }
            }

            return 0;
        }

#if NOOOOOOOOOOOOOOOOOOO
        // ���źϲ�������������Cellλ��
        // ��������parent_cell.item.MemberCells�����������(�����ϵ)�����Ҫ�������´�������Ҫ�ڵ��ñ�����ǰ�������
        void PlaceBinding(
            Cell parent_cell,
            List<Cell> member_cells,
            out string strPublishTimeString)
        {
            Debug.Assert(member_cells.Count != 0, "");

            strPublishTimeString = "";

            // parent_item.MemberCells.Clear();

            Debug.Assert(member_cells.Count > 0, "");

#if DEBUG
            VerifyListCell(parent_cell.item.MemberCells);
            VerifyListCell(member_cells);
#endif

            List<Cell> members = new List<Cell>();
            members.AddRange(parent_cell.item.MemberCells);
            members.AddRange(member_cells);

            members.Sort(new CellComparer());

            Cell first_cell = members[0];
            IssueBindingItem first_issue = first_cell.Container;
            Debug.Assert(first_issue != null, "");

            int nCol = -1;
            int nOldCol = parent_cell.Container.Cells.IndexOf(parent_cell);

            // ��ǰ�϶������ڵ���λ�ò��ԣ���Ҫ����
            if (first_issue != parent_cell.Container)
            {
                // ��ԭ����λ������Ϊ�ա������Ͳ�������GetFirstAvailableBindingColumn()����
                if (nOldCol != -1)
                    parent_cell.Container.SetCell(nOldCol, null);

                nCol = first_issue.GetFirstAvailableBindingColumn();

                //���õ��µ�λ��
                first_issue.SetCell(nCol, parent_cell);
                parent_cell.item.Container = first_issue;   // ��װ���������
            }
            else
            {
                // parent_cell position not changed
                nCol = nOldCol;
            }

            Debug.Assert(nCol != -1, "");

            // ���°���Ԫ��˳�򣬱�֤������Ԫ���ں���
            members.Clear();
            members.AddRange(parent_cell.item.MemberCells);
            members.AddRange(member_cells);


            // ���������ĵ�����
            PlaceMemberCells(parent_cell,
                members,
                nCol + 1,
                out strPublishTimeString);

#if DEBUG
            {
                string strError = "";
                int nRet = parent_cell.item.VerifyMemberCells(out strError);
                if (nRet == -1)
                {
                    Debug.Assert(false, strError);
                }
            }
#endif
        }
#endif

        void VerifyListCell(List<Cell> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                IssueBindingItem issue = cell.Container;
                Debug.Assert(issue != null, "");
                Debug.Assert(issue.Cells.IndexOf(cell) != -1, "");
            }
        }

        internal void VerifyAll()
        {
            for (int i = 0; i < this.Issues.Count; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                /*
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;
                 * */

                for (int j = 0; j < issue.Cells.Count; j++)
                {
                    Cell cell = issue.Cells[j];
                    if (cell != null)
                    {

                        if (issue.Cells.IndexOf(cell) != j)
                        {
                            Debug.Assert(false, "issue.Cells�����ظ���Ԫ��");
                        }

                        if (cell.Container != issue)
                        {
                            Debug.Assert(false, "cell.Container����ȷ");
                        }
                    }
                }
            }

            // �����϶����������
            for (int i = 0; i < this.ParentItems.Count; i++)
            {
                ItemBindingItem parent_item = this.ParentItems[i];

                IssueBindingItem issue = parent_item.Container; // ��װ���������

                // Debug.Assert(issue != null, "issue == null");

                if (issue != null)
                {
                    // �ҵ��к�
                    int nLineNo = this.Issues.IndexOf(issue);
                    Debug.Assert(nLineNo != -1, "");

                    int nCol = issue.IndexOfItem(parent_item);
                    if (nCol == -1)
                    {
                        Debug.Assert(nCol != -1, "nCol == -1");
                    }
                }
            }
        }

#if NOOOOOOOOOOOOOOOOO
        // ���ź϶���Ա����
        // parameters:
        //      members   Cell���顣ע�������е�Cell������itemΪnull��Ϊ�հ׸���
        //      strPublishTimeString    �������ʱ�䷶Χ�ַ���
        void PlaceMemberCells(
            Cell parent_cell,
            List<Cell> members,
            int nCol,
            out string strPublishTimeString)
        {
            strPublishTimeString = "";

            Debug.Assert(nCol >= 0, "");
            Debug.Assert(members.Count != 0, "");

            List<IssueBindingItem> done_issues = new List<IssueBindingItem>();
            int nFirstLineNo = 99999;
            int nLastLineNo = -1;
            for (int i = 0; i < members.Count; i++)
            {
                Cell cell = members[i];

                ItemBindingItem item = cell.item;
                IssueBindingItem issue = cell.Container;

                if (issue == null)
                {
                    Debug.Assert(false, "");
                }

                Debug.Assert(issue != null, "");

                Debug.Assert(issue.Cells.IndexOf(cell) != -1, "");

                done_issues.Add(issue);

                int nIssueLineNo = this.Issues.IndexOf(issue);
                if (nFirstLineNo > nIssueLineNo)
                    nFirstLineNo = nIssueLineNo;

                if (nLastLineNo < nIssueLineNo)
                    nLastLineNo = nIssueLineNo;

                // ���cell�����Ƿ��Ѿ�����
                int nExistIndex = issue.Cells.IndexOf(cell);

                // ���Cell��������ͬ�ڵ������λ����
                // ֱ��ʹ��
                if (nExistIndex == nCol)
                {
                    cell.ParentItem = parent_cell.item;
                    if (item != null)
                    {
                        item.ParentItem = parent_cell.item;
                    }

                    // parent_cell.item.MemberCells.Remove(cell);  // ����
                    parent_cell.item.InsertMemberCell(cell);
                    continue;
                }

                // Cell��ͬһ�ڣ������ں��ʵ�λ�á�
                // �Ƴ��Ѿ����ڵ�Cell��exist_cell�У�����
                Cell exist_cell = null;
                if (nExistIndex != -1)
                {
                    if ((nExistIndex % 2) == 0)
                    {
                        // ���������λ�ã��ͺ�����ˡ���Ϊ���������һ���϶��Ĳ�
                        throw new Exception("���ֽ�Ҫ���ŵ�����������Ȼ������Cellλ���Ѿ�����");
                    }
                    exist_cell = issue.GetCell(nExistIndex);
                    issue.Cells.RemoveAt(nExistIndex);

                    issue.Cells.RemoveAt(nExistIndex - 1);    // ���һ����Ҳɾ��
                }
                else
                {
                    // Cell����ͬһ��
                    Debug.Assert(false, "");
                }

                issue.GetBlankPosition(nCol / 2, parent_cell.item);

                /*
                if (exist_cell == null)
                {
                    // Debug.Assert(false, "���񲻿����ߵ�����");

                    exist_cell = new Cell();
                    exist_cell.ParentItem = parent_cell.item;

                    // ֻ��ռ��λ��
                    exist_cell.item = item;
                }*/

                if (exist_cell != null)
                {
                    // ���뵽����λ��
                    // parent_cell.item.MemberCells.Remove(exist_cell);  // ����

                    parent_cell.item.InsertMemberCell(exist_cell);


                    Debug.Assert(exist_cell == cell, "");
                }
                else
                {
                    /*
                    // Cell����ͬһ�ڣ������Ƕ����ĵ���
                    Debug.Assert(cell != null, "");

                    // ��ԭ�ȵ�issueλ������

                    // ��ԭ�ȵ�memberλ������
                    ItemBindingItem temp_parent = cell.ParentItem;
                    if (temp_parent != null)
                        temp_parent.MemberCells.Remove(cell);
                    cell.ParentItem = null;
                     * */

                    Debug.Assert(false, "���񲻿����ߵ�����");

                    // ���뵽����memberλ��
                    // parent_cell.item.MemberCells.Remove(cell); 
                    parent_cell.item.InsertMemberCell(cell);
                }

                cell.ParentItem = parent_cell.item;
                if (item != null)
                {
                    item.ParentItem = parent_cell.item;
                    Debug.Assert(cell.item == item, "");
                }

                // �ڼ��������ǵ�λ�ý��г������
                Cell old_cell = issue.GetCell(nCol);
                if (old_cell != null && old_cell != cell)
                {
                    ItemBindingItem temp_parent = old_cell.ParentItem;
                    if (temp_parent != null)
                        temp_parent.MemberCells.Remove(old_cell);
                    old_cell.ParentItem = null;

                    old_cell.Container = null;  // 

                    int temp = members.IndexOf(old_cell);
                    if (temp >= i)
                    {
                        Debug.Assert(false, "");
                    }
                }

                issue.SetCell(nCol, cell);
            }

            Debug.Assert(nFirstLineNo != 99999, "");
            Debug.Assert(nLastLineNo != -1, "");

            // 2009/12/16 
            strPublishTimeString = this.Issues[nFirstLineNo].PublishTime
            + "-"
            + this.Issues[nLastLineNo].PublishTime;


            // �������
            for (int i = nFirstLineNo; i <= nLastLineNo; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                if (done_issues.IndexOf(issue) != -1)
                    continue;

                {
                    Cell cell = issue.GetCell(nCol);
                    // ����ǿհ׸��ӣ�������������ֱ��ʹ��
                    if (cell != null
                        && cell.item == null
                        && cell.Binded == false)
                    {
                        cell.ParentItem = parent_cell.item;
                        parent_cell.item.InsertMemberCell(cell);
                        continue;
                    }
                }

                issue.GetBlankPosition(nCol / 2, parent_cell.item);
                /*
                // ���Ҫ���ŵ�λ���Ѿ��������ݣ��������ƶ�����(����)
                if (issue.Cells.Count > nCol)
                {
                    if (issue.Cells[nCol] != null)
                    {
                        issue.Cells.Add(null);
                        issue.Cells.Add(null);
                        for (int j = issue.Cells.Count - 1; j >= nCol + 2; j--)
                        {
                            Cell temp = issue.Cells[j - 2];
                            issue.Cells[j] = temp;
                        }
                    }
                }
                else
                {
                    while (issue.Cells.Count <= nCol)
                    {
                        issue.Cells.Add(null);
                    }
                }
                 * */

                {
                    Cell cell = new Cell();
                    cell.item = null;   // ֻ��ռ��λ��
                    cell.ParentItem = parent_cell.item;
                    issue.SetCell(nCol, cell);

                    // ���ں���λ��
                    parent_cell.item.InsertMemberCell(cell);
                }
            }
        }
#endif

        // ׷�Ӱ��ź϶���Ա����
        // ע�Ȿ��������������϶���Cell�ĸ���λ��
        // parameters:
        //      members   Cell���顣ע�������е�Cell������itemΪnull��Ϊ�հ׸���
        //      nCol    �кš�����indexλ�á�TODO: ���==-1������ѡ��һ�����������ڵĵ�һ���϶�����λ�á��ǵ÷�������кţ���������
        void PlaceMemberCells(
            Cell parent_cell,
            List<Cell> members,
            int nCol)
        {
            Debug.Assert(nCol >= 0, "");
            Debug.Assert(members.Count != 0, "");

            for (int i = 0; i < members.Count; i++)
            {
                Cell cell = members[i];

                ItemBindingItem item = cell.item;
                IssueBindingItem issue = cell.Container;

                if (issue == null)
                {
                    Debug.Assert(false, "");
                }

                Debug.Assert(issue != null, "");

                if (issue.IssueLayoutState == IssueLayoutState.Accepting)
                {
                    // ��ԭ���ĳ�Ա����������ڵ�ɾ��
                    parent_cell.item.RemoveMemberCell(issue);
                    // ���þɵ�λ��
                    parent_cell.item.InsertMemberCell(cell);
                    cell.ParentItem = parent_cell.item;
                    if (cell.item != null)
                        cell.item.ParentItem = parent_cell.item;

                    if (issue.IndexOfCell(cell) == -1)
                        issue.AddCell(cell);    // 2012/9/29 !!!TEST!!!

                    continue;
                }

                // Debug.Assert(issue.Cells.IndexOf(cell) != -1, "");
                // ����ȫû�м���issue.Cells��Cell�Ŀ���

                // int nIssueLineNo = this.Issues.IndexOf(issue);

                issue.GetBlankDoubleIndex(nCol - 1, 
                    parent_cell.item,
                    item);

                // ���cell�����Ƿ��Ѿ�����
                int nExistIndex = issue.Cells.IndexOf(cell);

                // ���Cell��������ͬ�ڵ������λ�����
                if (nExistIndex != -1
                    && nExistIndex == nCol - 1)
                {
                    // ���nColλ���Ƿ�Ϊ�հ�
                    if (issue.IsBlankSingleIndex(nCol) == true)
                    {
                        // �����Ż�
                        cell.ParentItem = parent_cell.item;
                        if (item != null)
                            item.ParentItem = parent_cell.item;

                        parent_cell.item.InsertMemberCell(cell);

                        issue.SetCell(nCol - 1, null);
                        issue.SetCell(nCol, cell);
                        continue;
                    }
                }

                // ���Cell��������ͬ�ڵ������λ����
                // ֱ��ʹ��
                if (nExistIndex == nCol)
                {
                    // ��Ҫ����nCol��ߵ�λ���Ƿ����
                    Cell cellLeft = issue.GetCell(nCol - 1);

                    if (IssueBindingItem.IsBlankOrNullCell(cellLeft) == true
                        || (cellLeft != null && cell.item != null && cell.item == parent_cell.item))
                    {
                        // ������
                        if (cellLeft != null && cell.item != null && cell.item == parent_cell.item)
                        {
                        }
                        else
                            issue.SetCell(nCol - 1, null);  // 

                        cell.ParentItem = parent_cell.item;
                        if (item != null)
                        {
                            item.ParentItem = parent_cell.item;
                        }

                        // parent_cell.item.MemberCells.Remove(cell);  // ����
                        parent_cell.item.InsertMemberCell(cell);
                        continue;
                    }
                }

                // Cell��ͬһ�ڣ������ں��ʵ�λ�á�
                // �Ƴ��Ѿ����ڵ�Cell��exist_cell�У�����
                Cell exist_cell = null;
                if (nExistIndex != -1)
                {
                    exist_cell = issue.GetCell(nExistIndex);

                    if (nExistIndex > nCol)
                    {
                        // ̽���Ƿ�Ϊ�϶���Առ�ݵ�λ��
                        // return:
                        //      -1  �ǡ�������˫������λ��
                        //      0   ����
                        //      1   �ǡ�������˫����Ҳ�λ��
                        int nRet = issue.IsBoundIndex(nExistIndex);
                        if (nRet == -1 || nRet == 1)
                            issue.SetCell(nExistIndex, null);   // 2010/3/29
                        else
                            issue.RemoveSingleIndex(nExistIndex);
                    }
                    else
                        issue.SetCell(nExistIndex, null);   // 2010/3/17

                    // ����϶�������Ҳ��ͬһ�ڣ����ܻ���ΪGetBlankDoubleIndex()�������ƶ���
                    // ��Ҫ�����ƶ������ʵ�λ��
                    if (parent_cell.Container == issue)
                    {
                        int nParentIndex = issue.IndexOfCell(parent_cell);
                        if (nParentIndex != nCol - 1)
                        {
                            issue.RemoveSingleIndex(nParentIndex);
                            issue.SetCell(nCol - 1, parent_cell);
                        }
                    }
                }
                else
                {
                    /*
                    // Cell����ͬһ��
                    Debug.Assert(false, "");
                     * */

                    // û�м���issue.Cells�����
                }

                // issue.GetBlankDoubleIndex(nCol - 1, parent_cell.item);

                /*
                if (exist_cell == null)
                {
                    // Debug.Assert(false, "���񲻿����ߵ�����");

                    exist_cell = new Cell();
                    exist_cell.ParentItem = parent_cell.item;

                    // ֻ��ռ��λ��
                    exist_cell.item = item;
                }*/

                if (exist_cell != null)
                {
                    // ���뵽����λ��
                    // parent_cell.item.MemberCells.Remove(exist_cell);  // ����

                    parent_cell.item.InsertMemberCell(exist_cell);


                    Debug.Assert(exist_cell == cell, "");
                }
                else
                {
                    /*
                    // Cell����ͬһ�ڣ������Ƕ����ĵ���
                    Debug.Assert(cell != null, "");

                    // ��ԭ�ȵ�issueλ������

                    // ��ԭ�ȵ�memberλ������
                    ItemBindingItem temp_parent = cell.ParentItem;
                    if (temp_parent != null)
                        temp_parent.MemberCells.Remove(cell);
                    cell.ParentItem = null;
                     * */

                    // Debug.Assert(false, "���񲻿����ߵ�����");
                    // û��Ԥ�ȼ���issue.Cells����������ߵ�����

                    // ���뵽����memberλ��
                    // parent_cell.item.MemberCells.Remove(cell); 
                    parent_cell.item.InsertMemberCell(cell);
                }

                cell.ParentItem = parent_cell.item;
                if (item != null)
                {
                    item.ParentItem = parent_cell.item;
                    Debug.Assert(cell.item == item, "");
                }

                // �ڼ��������ǵ�λ�ý��г������
                Cell old_cell = issue.GetCell(nCol);
                if (old_cell != null && old_cell != cell)
                {
                    ItemBindingItem temp_parent = old_cell.ParentItem;
                    if (temp_parent != null)
                        temp_parent.MemberCells.Remove(old_cell);
                    old_cell.ParentItem = null;

                    old_cell.Container = null;  // 

                    int temp = members.IndexOf(old_cell);
                    if (temp >= i)
                    {
                        Debug.Assert(false, "");
                    }
                }

                issue.SetCell(nCol, cell);
            }
        }

#if NNNNNNNNNNNNNNNNNNNNN
        // ���ź϶�������������Cellλ��
        void PlaceBinding(
            ItemBindingItem parent_item,
            List<ItemBindingItem> members,
            out string strPublishTimeString)
        {
            strPublishTimeString = "";
            Debug.Assert(members.Count != 0, "");

            /*
            parent_item.MemberItems.Clear();
            parent_item.MemberItems.AddRange(members);  // ע�⣬��ʱÿ��member��ParentItem��δ����
            */
            parent_item.MemberCells.Clear();


            ItemBindingItem first_sub = members[0];
            IssueBindingItem first_issue = first_sub.Container;
            Debug.Assert(first_issue != null, "");

            int nCol = first_issue.GetFirstAvailableBoundColumn();

            Cell cell = new Cell();
            cell.item = parent_item;
            first_issue.SetCell(nCol, cell);
            parent_item.Container = first_issue; // ��װ���������

            // ���������ĵ�����
            PlaceMemberItems(parent_item,
                members,
                nCol + 1,
                out strPublishTimeString);

#if DEBUG
            {
                string strError = "";
                int nRet = parent_item.VerifyMemberCells(out strError);
                if (nRet == -1)
                {
                    Debug.Assert(false, strError);
                }
            }
#endif
        }
#endif

        // ��ø��Ӷ������������ڶ���
        static List<IssueBindingItem> GetIssueList(List<Cell> cells)
        {
            List<IssueBindingItem> results = new List<IssueBindingItem>();
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (cell == null)
                    continue;
                if (cell.Container != null)
                {
                    if (results.IndexOf(cell.Container) == -1)
                        results.Add(cell.Container);
                }
            }

            return results;
        }

        // �����л����еĲ���ģʽ
        static int SwitchIssueLayout(List<IssueBindingItem> issues,
            IssueLayoutState state,
            out List<IssueBindingItem> changed_issues,
            out string strError)
        {
            strError = "";
            changed_issues = new List<IssueBindingItem>();

            int nRet = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                IssueBindingItem issue = issues[i];
                if (String.IsNullOrEmpty(issue.PublishTime) == true)
                    continue;

                if (issue.IssueLayoutState != state)
                {
                    if (issue.IssueLayoutState == IssueLayoutState.Accepting)
                        nRet = issue.ReLayoutBinding(out strError);
                    else
                        nRet = issue.LayoutAccepting(out strError);
                    if (nRet == -1)
                        return -1;
                    issue.IssueLayoutState = state;

                    changed_issues.Add(issue);
                }
            }

            return 0;
        }

        // �϶�ѡ�������
        void menuItem_bindingSelectedItem_Click(object sender,
            EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            bool bRemoveFromFreeIssue = false;
            bool bAddToParentItems = false;

            Cell parent_cell = null;

            // ѡ��������װ���ĵ���
            List<Cell> member_cells = new List<Cell>();

            // �Ѿ�����װ���Ĳ�
            List<Cell> binded_cells = new List<Cell>();

            // Ŀ��ᡣ
            // ��ǰ�ʹ��ڵ����������ڵĲᣬ����ֱ����Ϊ�϶������
            List<Cell> target_cells = new List<Cell>();


            List<Cell> selected_cells = this.SelectedCells;
            if (selected_cells.Count == 0)
            {
                strError = "��δѡ��Ҫ�϶��Ĳ�";
                goto ERROR1;
            }

            // �����ѡ���������������
            // 1) �����Ѿ���װ���Ĳ�
            // 2) һ��ֻ����һ������롣Ҳ����˵��ÿ�����Container������ͬ
            for (int i = 0; i < selected_cells.Count; i++)
            {
                Cell cell = selected_cells[i];

                if (cell is GroupCell)
                {
                    strError = "��������β���Ӳ��ܲ���϶�";
                    goto ERROR1;
                }

                if (this.FreeIssue != null
                    && cell.Container == this.FreeIssue)
                {
                    if (cell.item == null)
                    {
                        strError = "ѡ��Ķ����ܰ��������������ڵĿհ׸���";
                        goto ERROR1;
                    }

                    if (cell.item != null
                        && cell.item.IsParent == false)
                    {
                        DialogResult dialog_result = MessageBox.Show(this,
    "������������ѡ���Ķ��󲢲��Ǻ϶������Ƿ�Ҫ�����������κ϶�������Ŀ��?\r\n\r\n(Yes: �����϶�Ŀ��; No: �������϶�Ŀ�꣬��������; Cancel: ���������϶�����)",
    "BindingControls",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                        if (dialog_result == DialogResult.Yes)
                            cell.item.IsParent = true;
                        else if (dialog_result == DialogResult.Cancel)
                        {
                            strError = "�϶�����������";
                            goto ERROR1;
                        }
                        else if (dialog_result == DialogResult.No)
                            continue;
                        else
                        {
                            Debug.Assert(false, "");
                            continue;
                        }
                    }



                    Debug.Assert(cell.item.IsParent == true, "");

                    target_cells.Add(cell);
                    continue;
                }

                if (this.IsBindingParent(cell) == true)
                {
                    Debug.Assert(cell.item != null, "");
                    if (CheckProcessingState(cell.item) == false)
                    {
                        strError = "�϶��� '" + cell.item.PublishTime + "' Ϊ�̻�״̬��������Ϊװ��Ŀ��";
                        goto ERROR1;
                    }
                    if (cell.item.Locked == true)
                    {
                        strError = "�϶��� '" + cell.item.PublishTime + "' Ϊ����״̬��������Ϊװ��Ŀ��";
                        goto ERROR1;
                    }
                    target_cells.Add(cell);
                    continue;
                }

                if (cell.item != null && cell.item.Calculated == true)
                {
                    strError = "Ԥ����Ӳ��ܲ���϶�";
                    goto ERROR1;
                }



                if (cell.IsMember == true)
                    binded_cells.Add(cell);
                else
                {
                    // �ų��϶������󣬽�������ͨ�������
                    if (this.IsBindingParent(cell) == false)
                    {
                        member_cells.Add(cell);
                    }

                    Debug.Assert(this.ParentItems.IndexOf(cell.item) == -1, "");
                }
            }

            if (binded_cells.Count > 0)
            {
                strError = "�� " + binded_cells.Count.ToString() + " ���������Ѿ����϶��ĳ�Ա�ᣬ����޷��ٽ��к϶�";
                goto ERROR1;
            }

            if (member_cells.Count == 0)
            {
                strError = "��ѡ���ĸ�����û�а����κ�δװ���ĵ���";
                goto ERROR1;
            }

            if (target_cells.Count > 1)
            {
                strError = "��ѡ�Ĳ����� " + target_cells.Count.ToString() + " �����ɲ���ߺ϶��ᣬ����޷����к϶�����ȷ��ֻ����һ��(�������϶�Ŀ���)���ɲ��϶���";
                goto ERROR1;
            }

            // return:
            //      -1  error
            //      0   �ϸ�
            //      1   ���ϸ�strError������ʾ
            nRet = CheckBindingCells(member_cells,
                out strError);
            if (nRet != 0)
            {
                strError = "�޷����к϶�: " + strError;
                goto ERROR1;
            }

#if NOOOOOOOOOOOOO

            List<IssueBindingItem> issues = GetIssueList(member_cells);
            if (issues.Count > 0)
            {
                List<IssueBindingItem> changed_issues = null;
                nRet = SwitchIssueLayout(issues,
                    IssueLayoutState.Binding,
                    out changed_issues,
                    out strError);
                if (nRet == -1)
                {
                    strError = "SwitchLayout() to binding mode error: " + strError;
                    goto ERROR1;
                }

#if DEBUG
                // ���
                for (int i = 0; i < member_cells.Count; i++)
                {
                    Cell cell = member_cells[i];
                    IssueBindingItem issue = cell.Container;
                    Debug.Assert(issue != null, "");
                    Debug.Assert(issue.Cells.IndexOf(cell) != -1, "");
                }
#endif

                // �������ǰѡ���Ķ��󱻶�������Ҫ��ʾ����ѡ��
                for (int i = 0; i < member_cells.Count; i++)
                {
                    Cell cell = member_cells[i];
                    if (cell.Container == null
                        || (cell.item != null && cell.item.Container == null))
                    {
                        strError = "��Ϊ�л����֣���ǰѡ���ĳЩ�������˱仯���޷��������к϶�������������ѡ�������";
                        this.Invalidate();
                        goto ERROR1;
                    }
                    IssueBindingItem issue = cell.Container;
                    int nCol = issue.Cells.IndexOf(cell);
                    if (nCol == -1)
                    {
                        strError = "��Ϊ�л����֣���ǰѡ���ĳЩ�������˱仯���޷��������к϶�������������ѡ�������";
                        this.Invalidate();
                        goto ERROR1;
                    }
                }

                // TODO: ʧЧ��Щ���ı��е���ʾ����
            }

#endif

            this.Invalidate();
            this.Update();

            string strBatchNo = GetBindingBatchNo();   // ֻ�ܷ���������ں��������Paint()����

            // ���к϶�
            parent_cell = null;
            if (target_cells.Count == 0)
            {

                ItemBindingItem parent_item = new ItemBindingItem();
                this.ParentItems.Add(parent_item);
                bAddToParentItems = true;
                // ��ʼ������Ϣ
                nRet = parent_item.Initial("<root />",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                parent_item.NewCreated = true;

                /*
                // ���û���ˢ��һ����������
                // ���ܻ��׳��쳣
                parent_item.SetOperation(
                    "create",
                    this.Operator,
                    "");
                 * */

                parent_item.RefID = Guid.NewGuid().ToString();
                if (this.SetProcessingState == true)
                    parent_item.State = Global.AddStateProcessing(parent_item.State);   // �ӹ���
                parent_item.BatchNo = strBatchNo;
                parent_item.IsParent = true;

                parent_cell = new Cell();
                parent_cell.item = parent_item;
            }
            else
            {
                parent_cell = target_cells[0];

                // ��������������
                if (parent_cell.Container == this.FreeIssue)
                {
                    this.FreeIssue.RemoveCell(parent_cell.item);
                    parent_cell.Container = null;
                    // TODO: ���ʧ�ܣ��Ƿ�Ҫ��ԭ?
                    bRemoveFromFreeIssue = true;
                }

                // ����BindItems
                if (this.ParentItems.IndexOf(parent_cell.item) == -1)
                {
                    this.ParentItems.Add(parent_cell.item);
                    parent_cell.Container = null;
                    // TODO: ���ʧ�ܣ��Ƿ�Ҫ��ԭ?
                    bAddToParentItems = true;
                }

                if (this.SetProcessingState == true)
                    parent_cell.item.State = Global.AddStateProcessing(parent_cell.item.State);   // �ӹ���

                parent_cell.item.BatchNo = strBatchNo;
            }

            Debug.Assert(parent_cell.item.IsParent == true, "");

            // �����ɵ������һ���϶���
            nRet = AddToBinding(member_cells,
                parent_cell,
                out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }

            string strPreferredLocationString = "";
            // ���ٲֵص��ַ����Ƿ��ڵ�ǰ�û���Ͻ��Χ��
            // return:
            //      0   �ڵ�ǰ�û���Ͻ��Χ�ڣ�����Ҫ�޸�
            //      1   ���ڵ�ǰ�û���Ͻ��Χ�ڣ���Ҫ�޸ġ�strPreferredLocationString���Ѿ�������һ��ֵ����ֻ���˷ֹݴ���һ�����ⷿ����Ϊ��
            nRet = CheckLocationString(parent_cell.item.LocationString,
            out strPreferredLocationString,
            out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 1)
            {
                parent_cell.item.LocationString = strPreferredLocationString;

                // TODO: ������Yes No�����Yes��������Edit����
                MessageBox.Show(this, "�϶��ɹ�����ע�⼰ʱ���ú϶��� �ݲصص� ֵ");

                // ѡ���´����Ĳ�
                this.ClearAllSelection();
                menuItem_modifyCell_Click(null, null);
                parent_cell.Select(SelectAction.On);
                this.FocusObject = parent_cell;
                EnsureVisible(parent_cell);
            }

            /*
            string strPublishTimeString = "";
            PlaceBinding(
                parent_item,
                selected_items,
                out strPublishTimeString);
            parent_item.PublishTime = strPublishTimeString;

            // ����<binding>Ԫ����Ƭ��
            parent_item.RefreshBindingXml();
            parent_item.State = Global.AddStateProcessing(parent_item.State);   // �ӹ���
            parent_item.Changed = true;
            */


            /*
            // ѡ�����г�Ա����
            parent_item.SelectAllMemberCells();
            this.Invalidate(); 
             * */
#if DEBUG
            {
                string strError1 = "";
                int nRet1 = parent_cell.item.VerifyMemberCells(out strError1);
                if (nRet1 == -1)
                {
                    Debug.Assert(false, strError1);
                }
            }

            VerifyAll();
#endif
            return;
        ERROR1:
            // ��ԭ
            if (bAddToParentItems == true)
            {
                this.ParentItems.Remove(parent_cell.item);
            }

            if (bRemoveFromFreeIssue == true)
            {
                this.AddToFreeIssue(parent_cell);
            }
#if DEBUG
            if (parent_cell != null && parent_cell.item != null)
            {
                string strError1 = "";
                int nRet1 = parent_cell.item.VerifyMemberCells(out strError1);
                if (nRet1 == -1)
                {
                    Debug.Assert(false, strError1);
                }
            }

            VerifyAll();
#endif

            MessageBox.Show(this, strError);
        }

        // ���ٲֵص��ַ����Ƿ��ڵ�ǰ�û���Ͻ��Χ��
        // return:
        //      0   �ڵ�ǰ�û���Ͻ��Χ�ڣ�����Ҫ�޸�
        //      1   ���ڵ�ǰ�û���Ͻ��Χ�ڣ���Ҫ�޸ġ�strPreferredLocationString���Ѿ�������һ��ֵ����ֻ���˷ֹݴ���һ�����ⷿ����Ϊ��
        int CheckLocationString(string strLocationString,
            out string strPreferredLocationString,
            out string strError)
        {
            strError = "";
            strPreferredLocationString = strLocationString;

            if (Global.IsGlobalUser(this.LibraryCodeList) == true)
                return 0;

            string strLibraryCode = Global.GetLibraryCode(strLocationString);

            if (StringUtil.IsInList(strLibraryCode, this.LibraryCodeList) == false)
            {
                strPreferredLocationString = StringUtil.FromListString(this.LibraryCodeList)[0] + "/";
            }

            if (strPreferredLocationString == strLocationString)
                return 0;

            return 1;
        }

        // ���װ�����κ�
        string GetBindingBatchNo()
        {
            if (this.AppInfo == null)
                return "";

            string strDefault = this.AppInfo.GetString(
                "binding_form",
                "binding_batchno",
                "");

            if (this.m_bBindingBatchNoInputed == true)
                return strDefault;

            string strResult = InputDlg.GetInput(this, "��ָ��װ�����κ�",
                "װ�����κ�:",
                strDefault,
                this.Font);
            if (strResult == null)
                return "";

            if (strResult != strDefault)
            {
                this.AppInfo.SetString(
                "binding_form",
                "binding_batchno",
                strResult);
            }

            this.m_bBindingBatchNoInputed = true;
            return strResult;
        }

        /// <summary>
        /// �������κ�
        /// </summary>
        public string AcceptBatchNo
        {
            get
            {
                if (this.AppInfo == null)
                    return "";
                return this.AppInfo.GetString(
                    "binding_form",
                    "accept_batchno",
                    "");
            }
            set
            {
                if (this.AppInfo != null)
                {
                    this.AppInfo.SetString(
                        "binding_form",
                        "accept_batchno",
                        value);
                }
            }
        }

        /// <summary>
        /// �������κ��Ƿ��Ѿ��ڽ��汻������
        /// </summary>
        public bool AcceptBatchNoInputed
        {
            get
            {
                return this.m_bAcceptingBatchNoInputed;
            }
            set
            {
                this.m_bAcceptingBatchNoInputed = value;
            }
        }

        // ����������κ�
        internal string GetAcceptingBatchNo()
        {
            if (this.AppInfo == null)
                return "";

            string strDefault = this.AcceptBatchNo;

            if (this.AcceptBatchNoInputed == true)
                return strDefault;

            string strResult = InputDlg.GetInput(this, "��ָ���������κ�",
                "�������κ�:",
                strDefault,
                this.Font);
            if (strResult == null)
                return "";

            if (strResult != strDefault)
            {
                // ����
                this.AcceptBatchNo = strResult;
            }

            this.AcceptBatchNoInputed = true;
            return strResult;
        }

        public static void PaintButton(Graphics graphics,
            Color color,
            RectangleF rect)
        {
            float upper_height = rect.Height / 2 + 1;
            float lower_height = rect.Height / 2;
            float x = rect.X;
            float y = rect.Y;

            LinearGradientBrush linGrBrush = new LinearGradientBrush(
new PointF(0, y),
new PointF(0, y + upper_height),
Color.FromArgb(70, color),
Color.FromArgb(120, color)
);
            linGrBrush.GammaCorrection = true;

            RectangleF rectBack = new RectangleF(
x,
y,
rect.Width,
upper_height);
            graphics.FillRectangle(linGrBrush, rectBack);

            //

            linGrBrush = new LinearGradientBrush(
new PointF(0, y + upper_height),
new PointF(0, y + upper_height + lower_height),
Color.FromArgb(200, color),
Color.FromArgb(100, color)
);
            rectBack = new RectangleF(
x,
y + upper_height,
rect.Width,
lower_height - 1);
            graphics.FillRectangle(linGrBrush, rectBack);
        }

        // paramters:
        //      pen ���Ʊ߿򡣿���Ϊnull������������һ�����ɫ��û�б߿�
        //      brush   �������ɫ������Ϊnull��������ֻ�б߿�
        public static void RoundRectangle(Graphics graphics,
            Pen pen,
            Brush brush,
            RectangleF rect,
            float radius)
        {
            RoundRectangle(graphics,
                pen,
                brush,
                rect.X,
                rect.Y,
                rect.Width,
                rect.Height,
                radius);
        }

        // paramters:
        //      pen ���Ʊ߿򡣿���Ϊnull������������һ�����ɫ��û�б߿�
        //      brush   �������ɫ������Ϊnull��������ֻ�б߿�
        public static void RoundRectangle(Graphics graphics,
            Pen pen,
            Brush brush,
            float x,
            float y,
            float width,
            float height,
            float radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddLine(x + radius, y, x + width - (radius * 2), y);
            path.AddArc(x + width - (radius * 2), y, radius * 2, radius * 2, 270, 90); 
            path.AddLine(x + width, y + radius, x + width, y + height - (radius * 2));
            path.AddArc(x + width - (radius * 2), y + height - (radius * 2), radius * 2, radius * 2, 0, 90); // Corner
            path.AddLine(x + width - (radius * 2), y + height, x + radius, y + height);
            path.AddArc(x, y + height - (radius * 2), radius * 2, radius * 2, 90, 90);
            path.AddLine(x, y + height - (radius * 2), x, y + radius);
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.CloseFigure();
            if (brush != null)
                graphics.FillPath(brush, path);
            if (pen != null)
                graphics.DrawPath(pen, path);
            path.Dispose();
        }

        // ��װ�汾
        public static void PartRoundRectangle(
    Graphics graphics,
    Pen pen,
    Brush brush,
    RectangleF rect,
    float radius,
    string strMask) // ���� ���� ���� ����
        {
            PartRoundRectangle(graphics,
                pen,
                brush,
                rect.X,
                rect.Y,
                rect.Width,
                rect.Height,
                radius,
                strMask);
        }

        // ����Բ�ǵľ���
        // paramters:
        //      pen ���Ʊ߿򡣿���Ϊnull������������һ�����ɫ��û�б߿�
        //      brush   �������ɫ������Ϊnull��������ֻ�б߿�
        public static void PartRoundRectangle(
            Graphics graphics,
            Pen pen,
            Brush brush,
            float x,
            float y,
            float width,
            float height,
            float radius,
            string strMask) // ���� ���� ���� ����
        {
            float x0 = 0;
            float y0 = 0;
            float width0 = 0;
            float height0 = 0;

            GraphicsPath path = new GraphicsPath();

            // ���� --> ����
            if (strMask[0] == 'r')
            {
                x0 = x + radius;
                y0 = y;
                if (strMask[1] == 'r') 
                    width0 = width-radius*2;
                else
                    width0 = width-radius*1;
            }
            else
            {
                // != 'r'
                x0 = x;
                y0 = y;
                if (strMask[1] == 'r')
                    width0 = width - radius * 1;
                else
                    width0 = width;
            }

            path.AddLine(x0, y0,
                x0 + width0, y0);

            // ����
            if (strMask[1] == 'r')
                path.AddArc(x + width - (radius * 2), y,
                    radius * 2, radius * 2,
                    270, 90);

            // ���� --> ����
            if (strMask[1] == 'r')
            {
                x0 = x + width;
                y0 = y + radius;
                if (strMask[2] == 'r')
                    height0 = height - radius * 2;
                else
                    height0 = height - radius * 1;
            }
            else
            {
                // != 'r'
                x0 = x + width;
                y0 = y;
                if (strMask[2] == 'r')
                    height0 = height - radius * 1;
                else
                    height0 = height;
            }


            path.AddLine(x0, y0, 
                x0, y0 + height0);

            // ����
            if (strMask[2] == 'r')
                path.AddArc(x + width - (radius * 2), y + height - (radius * 2),
                    radius * 2, radius * 2,
                    0, 90); // Corner

            // ���� --> ����
            if (strMask[2] == 'r')
            {
                x0 = x + width - radius;
                y0 = y + height;
                if (strMask[3] == 'r')
                    width0 = width - radius * 2;
                else
                    width0 = width - radius * 1;
            }
            else
            {
                // != 'r'
                x0 = x + width;
                y0 = y + height;
                if (strMask[3] == 'r')
                    width0 = width - radius * 1;
                else
                    width0 = width;
            }

            path.AddLine(x0, y0,
                x0 - width0, y0);

            // ����
            if (strMask[3] == 'r')
                path.AddArc(x, y + height - (radius * 2), 
                    radius * 2, radius * 2,
                    90, 90);

            // ���� --> ����
            if (strMask[3] == 'r')
            {
                x0 = x;
                y0 = y + height - radius;
                if (strMask[0] == 'r')
                    height0 = height - radius * 2;
                else
                    height0 = height - radius * 1;
            }
            else
            {
                // != 'r'
                x0 = x;
                y0 = y + height;
                if (strMask[0] == 'r')
                    height0 = height - radius * 1;
                else
                    height0 = height;
            }

            path.AddLine(x0, y0,
                x0, y0 - height0);

            // ����
            if (strMask[0] == 'r')
                path.AddArc(x, y, 
                    radius * 2, radius * 2,
                    180, 90);

            path.CloseFigure();

            if (brush != null)
                graphics.FillPath(brush, path);
            if (pen != null)
                graphics.DrawPath(pen, path);
            path.Dispose();
        }

        // paramters:
        //      pen ���Ʊ߿򡣿���Ϊnull������������һ�����ɫ��û�б߿�
        //      brush   �������ɫ������Ϊnull��������ֻ�б߿�
        public static void QueRoundRectangle(Graphics graphics,
            Pen pen,
            Brush brush,
            RectangleF rect,
            float radius,
            float que_radius)
        {
            QueRoundRectangle(graphics,
                pen,
                brush,
                rect.X,
                rect.Y,
                rect.Width,
                rect.Height,
                radius,
                que_radius);
        }

        // ȱ�ǵľ���
        // paramters:
        //      pen ���Ʊ߿򡣿���Ϊnull������������һ�����ɫ��û�б߿�
        //      brush   �������ɫ������Ϊnull��������ֻ�б߿�
        public static void QueRoundRectangle(Graphics graphics,
            Pen pen,
            Brush brush,
            float x,
            float y,
            float width,
            float height,
            float radius,
            float que_radius)
        {
            GraphicsPath path = new GraphicsPath();
            // ���� --> ����
            path.AddLine(x + radius, y,
                x + width - (radius + que_radius), y);
            // ����
            path.AddArc(x + width - (que_radius), y - que_radius,
                que_radius * 2, que_radius * 2, 180, -90);
            /*
            // ���� --> ����
            path.AddLine(x + width, y + que_radius, 
                x + width, y + height - (radius + que_radius));
             * */

            // ����
            path.AddArc(x + width - (radius * 2), y + height - (radius * 2),
                radius * 2, radius * 2, 0, 90); // Corner
            // ���� --> ����
            path.AddLine(x + width - (radius * 2), y + height,
                x + radius, y + height);
            // ����
            path.AddArc(x, y + height - (radius * 2), 
                radius * 2, radius * 2, 90, 90);
            // ���� --> ����
            path.AddLine(x, y + height - (radius * 2), x, y + radius);
            // ����
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.CloseFigure();
            if (brush != null)
                graphics.FillPath(brush, path);
            if (pen != null)
                graphics.DrawPath(pen, path);
            path.Dispose();
        }

        // paramters:
        //      pen ���Ʊ߿򡣿���Ϊnull������������һ�����ɫ��û�б߿�
        //      brush   �������ɫ������Ϊnull��������ֻ�б߿�
        public static void Circle(Graphics graphics,
            Pen pen,
            Brush brush,
            RectangleF rect)
        {
            GraphicsPath path = new GraphicsPath();
            if (pen != null)
            {
                rect.Inflate(-pen.Width / 2, -pen.Width / 2);
            }

            float x = rect.X;
            float y = rect.Y;
            float width = rect.Width;
            float height = rect.Height;

            path.AddArc(x, y,
                width, height, 0, 360);
            path.CloseFigure();
            if (brush != null)
                graphics.FillPath(brush, path);
            if (pen != null)
                graphics.DrawPath(pen, path);
            path.Dispose();
        }

        // paramters:
        //      pen ���Ʊ߿򡣿���Ϊnull������������һ�����ɫ��û�б߿�
        //      brush   �������ɫ������Ϊnull��������ֻ�б߿�
        public static void Bracket(Graphics graphics,
            Pen pen,
            bool bLeft,
            RectangleF rect,
            float radius)
        {
            GraphicsPath path = new GraphicsPath();

            rect.Inflate(0, -pen.Width/2);

            float x = rect.X;
            float y = rect.Y;
            float width = rect.Width;
            float height = rect.Height;

            if (bLeft == true)
            {
                path.AddArc(x + width - radius, y,
                    radius * 2, radius * 2, 270, -90);
                /*
                path.AddLine(x + width - (radius), y+(radius),
                    x + width - (radius), y + (height/2)-(radius));
                 * */
                path.AddArc(x + width - (radius * 2) - radius, y + (height / 2) - (radius) - radius,
                    radius * 2, radius * 2, 0, 90);
                path.AddArc(x + width - (radius * 2) - radius, y + (height / 2),
        radius * 2, radius * 2, 270, 90);
                /*
                path.AddLine(x + width - (radius), y + (height/2)+(radius),
        x + width - (radius), y + height - (radius));
                 * */
                path.AddArc(x + width - (radius), y + height - (radius) - radius,
        radius * 2, radius * 2, 180, -90);
            }
            else
            {
                path.AddArc(x - radius, y,
                    radius * 2, radius * 2, 270, 90);
                path.AddArc(x + radius , y + height/2 - radius*2,
                    radius * 2, radius * 2, 180, -90);
                path.AddArc(x + radius, y + (height / 2),
        radius * 2, radius * 2, 270, -90);
                path.AddArc(x - radius, y + height - radius*2,
        radius * 2, radius * 2, 0, 90);
            }

            graphics.DrawPath(pen, path);
            path.Dispose();
        }

        public static void RoundRectangle(Graphics graphics,
            Pen pen,
            RectangleF rect,
            float radius)
        {
            RoundRectangle(graphics,
                pen,
                rect.X,
                rect.Y,
                rect.Width,
                rect.Height,
                radius);
        }

        public static void RoundRectangle(Graphics graphics,
            Pen pen,
            float x,
            float y,
            float width,
            float height,
            float radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddLine(x + radius, y, x + width - (radius * 2), y);
            path.AddArc(x + width - (radius * 2), y, radius * 2, radius * 2, 270, 90);
            path.AddLine(x + width, y + radius, x + width, y + height - (radius * 2));
            path.AddArc(x + width - (radius * 2), y + height - (radius * 2), radius * 2, radius * 2, 0, 90); // Corner
            path.AddLine(x + width - (radius * 2), y + height, x + radius, y + height);
            path.AddArc(x, y + height - (radius * 2), radius * 2, radius * 2, 90, 90);
            path.AddLine(x, y + height - (radius * 2), x, y + radius);
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.CloseFigure();
            graphics.DrawPath(pen, path);
            path.Dispose();
        }
        
        void DoEndRectSelecting()
        {
            bool bControl = (Control.ModifierKeys == Keys.Control);

            // ���ѡ���
            DrawSelectRect(true);

            // �����ĵ�����
            RectangleF rect = MakeRect(m_DragStartPointOnDoc,
                m_DragCurrentPointOnDoc);

            // DataRoot����
            rect.Offset(-this.m_nLeftBlank, -this.m_nTopBlank);

            // ѡ��λ�ھ����ڵĶ���
            List<Type> types = new List<Type>();
            types.Add(typeof(Cell));

            List<CellBase> update_objects = new List<CellBase>();
            this.Select(rect,
                bControl == true ? SelectAction.Toggle : SelectAction.On,
                types,
                ref update_objects,
                100);
            if (update_objects.Count < 100)
                this.UpdateObjects(update_objects);
            else
                this.Invalidate();

            m_bRectSelecting = false;   // ����

            this.DragStartObject = this.FocusObject;
            this.DragLastEndObject = this.FocusObject;
        }

        // ��������ֵ
        static void Exchange<T>(ref T v1, ref T v2)
        {
            T temp = v1;
            v1 = v2;
            v2 = temp;
        }

        // ����һ�����Σ�ͨ�������˵�
        // �����������Զ��Ƚ϶˵��С������������ľ���
        static RectangleF MakeRect(PointF p1,
            PointF p2)
        {
            float x1 = p1.X;
            float y1 = p1.Y;

            float x2 = p2.X;
            float y2 = p2.Y;

            if (x1 > x2)
                Exchange<float>(ref x1, ref x2);

            if (y1 > y2)
                Exchange<float>(ref y1, ref y2);

            return new RectangleF(x1,
                y1,
                x2 - x1,
                y2 - y1);
        }


        // �����������ѡ�����
        // ��Ϊ��������㣬��һ���ǻ����ڶ�����ͬ��λ�þ������
        void DrawSelectRect(bool bUpdateBefore)
        {
            if (bUpdateBefore == true)
                this.Update();

            RectangleF rect = MakeRect(m_DragStartPointOnDoc,
            m_DragCurrentPointOnDoc);

            rect.Offset(m_lWindowOrgX, m_lWindowOrgY);
            ControlPaint.DrawReversibleFrame( // Graphics.FromHwnd(this.Handle),
                this.RectangleToScreen(Rectangle.Round(rect)),
                this.SelectedBackColor, // Color.Yellow,
                FrameStyle.Dashed);
        }

        // ��b�Ƿ��ڵ�a��Χһ������ľ��η�Χ��
        // ���β���ϵͳDoubleClickSize
        static bool IsNearestPoint(Point a, Point b)
        {
            Rectangle rect = new Rectangle(a.X, a.Y, 0, 0);
            rect.Inflate(
                SystemInformation.DoubleClickSize.Width / 2,
                SystemInformation.DoubleClickSize.Height / 2);

            return rect.Contains(b.X, b.Y);
        }



        // ѡ��λ�ھ����ڵĶ���
        public void Select(RectangleF rect,
            SelectAction action,
            List<Type> types,
            ref List<CellBase> update_objects,
            int nMaxCount)
        {
            RectangleF rectThis = new RectangleF(0, 0, this.m_lContentWidth, this.m_lContentHeight);

            if (rectThis.IntersectsWith(rect) == true)
            {
                long y = 0;
                for (int i = 0; i < this.Issues.Count; i++)
                {
                    IssueBindingItem issue = this.Issues[i];

                    // �Ż�
                    if (y > rect.Bottom)
                        break;

                    // �任Ϊissue������
                    RectangleF rectIssue = rect;
                    rectIssue.Offset(0, -y);

                    issue.Select(rectIssue,
                        action,
                        types,
                        ref update_objects,
                        nMaxCount);

                    y += this.m_nCellHeight;
                }
            }

        }

        void SetObjectFocus(CellBase obj)
        {
            if (m_bRectSelecting == true)
                return;

            if (obj == null)    // ��ʾ�ر����focus�����focus״̬
            {
                goto OFF_OLD;
            }

            if (obj.m_bFocus == true)
                return;


            obj.m_bFocus = true;
            this.UpdateObject(obj);

            if (obj == this.m_lastFocusObj)
                return;

            // off��ǰ��focus����
        OFF_OLD:
            if (this.m_lastFocusObj != null
                && this.m_lastFocusObj.m_bFocus == true)
            {
                this.m_lastFocusObj.m_bFocus = false;
                this.UpdateObject(this.m_lastFocusObj);
            }

            this.m_lastFocusObj = obj;  // ����
        }


        // ˢ��һȺ���������
        void UpdateObjects(List<CellBase> objects)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                CellBase obj = objects[i];
                if (obj == null)
                    continue;

                //  rectUpdate = new RectangleF(0, 0, obj.Width, obj.Height);

                RectangleF rectUpdate = GetViewRect(obj);
                this.Invalidate(Rectangle.Round(rectUpdate));
            }
        }

        internal void UpdateObject(CellBase obj)
        {
            Debug.Assert(obj != null, "");

            if (obj is Cell
    || obj is NullCell
                || obj is IssueBindingItem)
            {
            }
            else
            {
                throw new Exception("obj����Ϊ����Cell/NullCell/IssueBindingItem֮һ");
            }

            if (obj is NullCell)
            {
                RectangleF rectUpdate = GetViewRect(obj);
                this.Invalidate(Rectangle.Round(rectUpdate));
                return;
            }

            if (obj is Cell)
            {
                if (((Cell)obj).Container == null)
                    return;

                RectangleF rectUpdate = GetViewRect(obj);
                this.Invalidate(Rectangle.Round(rectUpdate));
                return;
            }

            if (obj is IssueBindingItem)
            {
                if (((IssueBindingItem)obj).Container == null)
                    return;

                RectangleF rectUpdate = GetViewRect(obj);
                this.Invalidate(Rectangle.Round(rectUpdate));
                return;
            }
        }

        // ˢ��һ�������drag handle����
        void UpdateObjectHover(Cell obj)
        {
            UpdateObject(obj);
            /*
            RectangleF rectUpdate = this.m_rectCheckBox; 

            RectangleF rectObj = GetViewRect(obj);
            rectUpdate = new RectangleF(rectObj.X + rectUpdate.X,
                rectObj.Y + rectUpdate.Y,
                rectUpdate.Width,
                rectUpdate.Height);

            this.Invalidate(Rectangle.Round(rectUpdate));
             * */
        }

        // ȷ��һ�������ڴ��ڿͻ����ɼ�
        // parameters:
        //      rectCell    Ҫ��ע������
        //      rectCaret   Ҫ��ע�������У����ڲ�������ȵ㣩�ľ��Ρ�һ�����С��rectCell
        // return:
        //      �Ƿ��������
        public bool EnsureVisible(RectangleF rectCell,
            RectangleF rectCaret)
        {
            /*
            if (rectCaret == null)
                rectCaret = rectCell;
             * */

            long lDelta = (long)rectCell.Y;

            bool bScrolled = false;

            if (lDelta + rectCaret.Height >= this.ClientSize.Height)
            {
                if (rectCaret.Height >= this.ClientSize.Height)
                    DocumentOrgY = DocumentOrgY - (lDelta + (long)rectCaret.Height) + ClientSize.Height + /*����ϵ��*/ ((long)rectCaret.Height / 2) - (this.ClientSize.Height / 2);
                else
                    DocumentOrgY = DocumentOrgY - (lDelta + (long)rectCaret.Height) + ClientSize.Height;
                bScrolled = true;
            }
            else if (lDelta < 0)
            {
                if (rectCaret.Height >= this.ClientSize.Height)
                    DocumentOrgY = DocumentOrgY - (lDelta) - /*����ϵ��*/ (((long)rectCaret.Height / 2) - (this.ClientSize.Height / 2));
                else
                    DocumentOrgY = DocumentOrgY - (lDelta);
                bScrolled = true;
            }
            else
            {
                // y����Ҫ���
            }

            ////
            // ˮƽ����
            lDelta = 0;

            lDelta = (long)rectCell.X;


            if (lDelta + rectCaret.Width >= this.ClientSize.Width)
            {
                if (rectCaret.Width >= this.ClientSize.Width)
                    DocumentOrgX = DocumentOrgX - (lDelta + (long)rectCaret.Width) + ClientSize.Width + /*����ϵ��*/ ((long)rectCaret.Width / 2) - (this.ClientSize.Width / 2);
                else
                    DocumentOrgX = DocumentOrgX - (lDelta + (long)rectCaret.Width) + ClientSize.Width;
                bScrolled = true;
            }
            else if (lDelta < 0)
            {
                if (rectCaret.Width >= this.ClientSize.Width)
                    DocumentOrgX = DocumentOrgX - (lDelta) - /*����ϵ��*/ (((long)rectCaret.Width / 2) - (this.ClientSize.Width / 2));
                else
                    DocumentOrgX = DocumentOrgX - (lDelta);
                bScrolled = true;
            }
            else
            {
                // x����Ҫ���
            }


            return bScrolled;
        }

        // ȷ��һ����Ԫ�ڴ��ڿͻ����ɼ�
        // return:
        //      �Ƿ��������
        public bool EnsureVisible(CellBase obj)
        {
            if (obj == null)
            {
                Debug.Assert(false, "");
                return false;
            }
            RectangleF rectUpdate = GetViewRect(obj);

            RectangleF rectCell = rectUpdate;

            RectangleF rectCaret = rectUpdate;

            if (obj is IssueBindingItem)
            {
                // ����
                IssueBindingItem issue = (IssueBindingItem)obj;
                rectCaret.Width = this.m_nLeftTextWidth;
            }

            return EnsureVisible(rectCell, rectCaret);
        }

        // ȷ��һ������Ԫ�ڴ��ڿͻ����ɼ�
        // ��DayArea�����⴦��
        // return:
        //      �Ƿ��������
        public bool EnsureVisibleWhenScrolling(CellBase obj)
        {
            if (obj == null)
            {
                Debug.Assert(false, "");
                return false;
            }

            RectangleF rectUpdate = GetViewRect(obj);

            /*
            if (obj is Cell)
            {
                DayArea day = (DayArea)obj;
                // �����ÿ�µ�һ�����ڵ�����
                if (day.Container.Week == 1)
                {
                    // �������Σ��԰�������������
                    rectUpdate.Y -= this.DataRoot.m_nDayOfWeekTitleHeight;
                    rectUpdate.Height += this.DataRoot.m_nDayOfWeekTitleHeight;
                }
            }*/

            // TODO:
            // ������¡���Ƚϴ�ߴ�����壬ֻҪ������嵱ǰ���ֿɼ����Ͳ��ؾ����
            // Ҳ����ͨ����caret����Ϊ�Ѿ��ɼ��Ĳ��֣���ʵ������Ч��

            RectangleF rectCell = rectUpdate;

            RectangleF rectCaret = rectUpdate;

            if (obj is IssueBindingItem)
            {
                // ���� 2010/3/26
                IssueBindingItem issue = (IssueBindingItem)obj;
                rectCaret.Width = this.m_nLeftTextWidth;
            }

            return EnsureVisible(rectCell, rectCaret);
        }

        // return:
        //      �Ƿ��������
        public bool EnsureVisibleWhenScrolling(HitTestResult result)
        {
            if (result == null)
                return false;

            if (result.Object is IssueBindingItem
                && result.AreaPortion == AreaPortion.LeftText)
            {
                IssueBindingItem issue = (IssueBindingItem)result.Object;

                RectangleF rectUpdate = GetViewRect(issue);
                rectUpdate.Width = this.m_nLeftTextWidth;   // ��߱��ⲿ��

                RectangleF rectCell = rectUpdate;
                RectangleF rectCaret = rectUpdate;

                return EnsureVisible(rectCell, rectCaret);
            }
            else if (result.Object is Cell)
            {
                RectangleF rectUpdate = GetViewRect(result.Object);

                RectangleF rectCell = rectUpdate;
                RectangleF rectCaret = rectUpdate;

                return EnsureVisible(rectCell, rectCaret);
            }
            else if (result.Object is NullCell)
            {
                RectangleF rectUpdate = GetViewRect(result.Object);

                RectangleF rectCell = rectUpdate;
                RectangleF rectCaret = rectUpdate;

                return EnsureVisible(rectCell, rectCaret);
            }
            else
            {
                throw new Exception("�в�֧��IssueBindingItem/Cell/NullCell�������������");
            }

        }


        // �����ǰ�������Լ�ȫ���¼���ѡ���־, ��������Ҫˢ�µĶ���
        public void ClearAllSubSelected(ref List<CellBase> objects,
            int nMaxCount)
        {
            /*
            // �޸Ĺ��Ĳż�������
            if (this.m_bSelected == true && objects.Count < nMaxCount)
                objects.Add(this);

            this.m_bSelected = false;
             * */

            for (int i = 0; i < this.Issues.Count; i++)
            {
                this.Issues[i].ClearAllSubSelected(ref objects,
                    nMaxCount);
            }
        }

        // ̽������ͬ��������Ⱥ��ϵ
        // return:
        //      -1  start��end֮ǰ
        //      0   start��end��ͬһ������
        //      1   start��end֮��
        int GetDirection(Cell start, Cell end)
        {
            Debug.Assert(start != null, "");
            Debug.Assert(end != null);

            if (start == end)
                return 0;

            IssueBindingItem start_issue = start.Container;
            IssueBindingItem end_issue = end.Container;

            int start_issue_index = this.Issues.IndexOf(start_issue);
            int end_issue_index = this.Issues.IndexOf(end_issue);

            Debug.Assert(start_issue_index != -1, "");
            Debug.Assert(end_issue_index != -1, "");

            if (start_issue_index > end_issue_index)
            {
                // start��end����
                return 1;
            }

            return -1;  // start��endǰ��
        }

        // a��b�н���Ĳ��ַ���union������a��b��ȥ��
        void Compare(ref List<CellBase> a,
            ref List<CellBase> b,
            out List<CellBase> union)
        {
            union = new List<CellBase>();
            for (int i = 0; i < a.Count; i++)
            {
                CellBase x = a[i];

                bool bFound = false;
                for (int j = 0; j < b.Count; j++)
                {
                    CellBase y = b[j];
                    if (IsSamePos(x, y) == true)
                    {
                        union.Add(x);
                        b.RemoveAt(j);
                        bFound = true;
                        break;
                    }
                }

                if (bFound == true)
                {
                    a.RemoveAt(i);
                    i--;
                }
            }
        }

        bool IsSamePos(CellBase start, CellBase end)
        {
            int start_x = -1;
            int start_y = -1;
            GetCellXY(start,
    out start_x,
    out start_y);
            int end_x = -1;
            int end_y = -1;
            GetCellXY(end,
    out end_x,
    out end_y);
            if (start_x == end_x
                && start_y == end_y)
                return true;
            return false;
        }

        // ����㵽�յ㣬������������ֵܶ��������
        List<CellBase> GetRangeObjects(
            CellBase start,
            CellBase end)
        {
            Debug.Assert(start != null, "");
            Debug.Assert(end != null);

            List<CellBase> results = new List<CellBase>();

            if (start == end)
            {
                results.Add(end);
                return results;
            }

            if (start is NullCell
                && end is NullCell)
            {
                NullCell start_n = (NullCell)start;
                NullCell end_n = (NullCell)end;

                if (start_n.X == end_n.X
                    && start_n.Y == end_n.Y)
                {
                    results.Add(end);
                    return results;
                }
            }

            int start_x = -1;
            int start_y = -1;
            GetCellXY(start,
    out start_x,
    out start_y);
            int end_x = -1;
            int end_y = -1;
            GetCellXY(end,
    out end_x,
    out end_y);

            int x1 = 0;
            int y1 = 0;
            int x2 = 0;
            int y2 = 0;

            if (start_x < end_x)
            {
                x1 = start_x;
                x2 = end_x;
            }
            else
            {
                x1 = end_x;
                x2 = start_x;
            }

            if (start_y < end_y)
            {
                y1 = start_y;
                y2 = end_y;
            }
            else
            {
                y1 = end_y;
                y2 = start_y;
            }

            for (int i = y1; i <= y2; i++)
            {
                IssueBindingItem issue = this.Issues[i];
                if (x1 == -1)
                {
                    Debug.Assert(x2 == -1, "");
                    results.Add(issue);
                    continue;
                }

                for (int j = x1; j <= x2; j++)
                {
                    Cell cell = issue.GetCell(j);
                    if (cell == null)
                        results.Add( new NullCell(j, i));
                    else
                        results.Add(cell);
                }
            }

            return results;
        }

#if NOOOOOOOOOOOOOOO
        // ����㵽�յ㣬������������ֵܶ��������
        List<Cell> GetRangeObjects(
            bool bIncludeStart,
            bool bIncludeEnd,
            Cell start,
            Cell end)
        {
            Debug.Assert(start != null, "");
            Debug.Assert(end != null);

            List<Cell> result = new List<Cell>();

            if (start == end)
            {
                if (bIncludeStart == true)
                {
                    result.Add(start);
                    return result;
                }

                if (bIncludeEnd == true)
                    result.Add(end);
                return result;
            }

            // �ȹ۲��ĸ���ǰ��
            int nDirection = GetDirection(start, end);

            if (nDirection > 0)
            {
                // ����start��end
                Cell temp = start;
                start = end;
                end = temp;

                // ����bool
                bool bTemp = bIncludeStart;
                bIncludeStart = bIncludeEnd;
                bIncludeEnd = bTemp;
            }

            if (bIncludeStart == false)
            {
                start = start.GetNextSibling();
                if (start == null)
                {
                    return result;  // ���ؿռ���
                }
                Debug.Assert(start != null, "");
            }

            // ��start��end����������
            for (; ; )
            {
                if (bIncludeEnd == false
                    && start == end)
                    break;  // ������β��

                Debug.Assert(start != null, "");
                result.Add(start);

                if (start == end)
                    break;  // ����β��

                start = start.GetNextSibling();
                if (start == null)
                {
                    Debug.Assert(false, "��Ȼû������end");
                    break;
                }
            }

            return result;
        }
#endif

        void ClearSelectedArea()
        {
            this.m_aSelectedArea.Clear();
            this.m_bSelectedAreaOverflowed = false;
        }

        void AddSelectedArea(CellBase obj)
        {
            if (this.m_bSelectedAreaOverflowed == true)
                return;

            int index = this.m_aSelectedArea.IndexOf(obj);
            if (index == -1)
                this.m_aSelectedArea.Add(obj);

            if (this.m_aSelectedArea.Count > 1000)
                this.m_bSelectedAreaOverflowed = true;
        }

        void RemoveSelectedArea(CellBase obj)
        {
            if (this.m_bSelectedAreaOverflowed == true)
                return;

            this.m_aSelectedArea.Remove(obj);
        }


        // ѡ��һϵ�ж���
        void SelectObjects(List<CellBase> aObject,
            SelectAction action)
        {
            if (aObject == null)
                return;

            for (int i = 0; i < aObject.Count; i++)
            {
                CellBase obj = aObject[i];
                if (obj == null)
                    continue;

                // ���鲻��m_aSelectedArea������
                if (aObject != m_aSelectedArea)
                {
                    if (action == SelectAction.On
                        || action == SelectAction.Toggle)
                    {
                        AddSelectedArea(obj);
                    }
                    else if (action == SelectAction.Off)
                    {
                        RemoveSelectedArea(obj);
                    }
                }

                // RectangleF rectUpdate = new RectangleF(0, 0, obj.Width, obj.Height);

                bool bChanged = obj.Select(action/*, true*/);
                if (bChanged == false)
                    continue;

                RectangleF rectUpdate = GetViewRect(obj);
                /*
                rectUpdate = Object.ToRootCoordinate(rectUpdate);

                // ��DataRoot���꣬�任Ϊ�����ĵ����꣬Ȼ��任Ϊ��Ļ����
                rectUpdate.Offset(this.m_lWindowOrgX + m_nLeftBlank,
                    this.m_lWindowOrgY + m_nTopBlank);
                 */
                this.Invalidate(Rectangle.Round(rectUpdate));
            }
        }

        // �õ�һ������ľ���(view����)
        RectangleF GetViewRect(object objParam)
        {
            Debug.Assert(objParam != null, "");
            // 2011/8/4
            if (objParam == null)
            {
                return new RectangleF(0, 0, 0, 0);
            }

            if (objParam is Cell
    || objParam is NullCell
                || objParam is IssueBindingItem)
            {
            }
            else
            {
                throw new Exception("objParam����Ϊ����Cell��NullCell֮һ");
            }

            if (objParam is Cell)
            {
                Cell obj = (Cell)objParam;

                RectangleF rect = new RectangleF(0, 0, obj.Width, obj.Height);

                rect = obj.ToRootCoordinate(rect);

                // ��DataRoot���꣬�任Ϊ�����ĵ����꣬Ȼ��任Ϊview����
                rect.Offset(this.m_lWindowOrgX + m_nLeftBlank,
                    this.m_lWindowOrgY + m_nTopBlank);

                return rect;
            }
            else if (objParam is NullCell)
            {
                NullCell obj = (NullCell)objParam;

                RectangleF rect = new RectangleF(0, 0, this.m_nCellWidth, this.m_nCellHeight);

                // �任Ϊ�����ĵ�����
                rect.Offset(this.m_nLeftTextWidth + obj.X * this.m_nCellWidth,
                    obj.Y * this.m_nCellHeight);

                // �������ĵ����꣬�任Ϊ�����ĵ����꣬Ȼ��任Ϊview����
                rect.Offset(this.m_lWindowOrgX + this.m_nLeftBlank,
                    this.m_lWindowOrgY + this.m_nTopBlank);

                return rect;
            }
            else
            {
                Debug.Assert(objParam is IssueBindingItem, "");

                IssueBindingItem obj = (IssueBindingItem)objParam;

                int nLineNo = this.Issues.IndexOf(obj);
                if (nLineNo == -1)
                {
                    Debug.Assert(nLineNo != -1, "");
                    return new RectangleF(-1, -1, 0, 0);
                }

                RectangleF rect = new RectangleF(0,
                    0,
                    this.m_nLeftTextWidth  +this.m_nCellWidth * this.m_nMaxItemCountOfOneIssue, // ֻ���������ⲿ��
                    this.m_nCellHeight);

                // �任Ϊ�����ĵ�����
                rect.Offset(0,
                    nLineNo * this.m_nCellHeight);

                // �������ĵ����꣬�任Ϊ�����ĵ����꣬Ȼ��任Ϊview����
                rect.Offset(this.m_lWindowOrgX + this.m_nLeftBlank,
                    this.m_lWindowOrgY + this.m_nTopBlank);

                return rect;
            }
        }


        protected override void OnPaintBackground(PaintEventArgs e)
        {
            /*
            return;
             * */
            base.OnPaintBackground(e);
            /*
            Brush brush0 = null;

            if (this.Enabled == false)
                brush0 = new SolidBrush(Color.LightGray);
            else
                brush0 = new SolidBrush(this.BackColor);

            e.Graphics.FillRectangle(brush0, e.ClipRectangle);

            brush0.Dispose();
            return;
             * */
        }

        // �ж�һ��NullCell�����ĸ��϶���
        Cell BelongToBinding(NullCell cell)
        {
            // �����϶����������
            for (int i = 0; i < this.ParentItems.Count; i++)
            {
                ItemBindingItem parent_item = this.ParentItems[i];

                IssueBindingItem issue = parent_item.Container; // ��װ���������
                Debug.Assert(issue != null, "");

                // 2010/4/1
                if (issue.IssueLayoutState == IssueLayoutState.Accepting)
                    continue;

                int nCol = issue.IndexOfItem(parent_item);
                Debug.Assert(nCol != -1, "");


                if (cell.X != nCol
                    && cell.X != nCol + 1)
                    continue;

                // �ҵ��к�
                int nLineNo = this.Issues.IndexOf(issue);
                Debug.Assert(nLineNo != -1, "");
                if (cell.Y == nLineNo)
                    return parent_item.ContainerCell;

                for (int j = 0; j < parent_item.MemberCells.Count; j++)
                {
                    Cell member_cell = parent_item.MemberCells[j];

                    issue = member_cell.Container;
                    Debug.Assert(issue != null, "");

                    // 2010/4/1
                    if (issue.IssueLayoutState == IssueLayoutState.Accepting)
                        continue;

                    // �ҵ��к�
                    nLineNo = this.Issues.IndexOf(issue);
                    Debug.Assert(nLineNo != -1, "");

                    if (cell.Y == nLineNo)
                        return parent_item.ContainerCell;
                }
            }

            return null;
        }

        // �����������ַ���ת��Ϊ�ʺ���ʾ�ĸ�ʽ
        public static string GetDisplayPublishTime(string strPublishTime)
        {
            int nLength = strPublishTime.Length;
            if (nLength > 8)
                strPublishTime = strPublishTime.Insert(8, ":");

            if (nLength > 6)
                strPublishTime = strPublishTime.Insert(6,"-");
            if (nLength > 4)
                strPublishTime = strPublishTime.Insert(4, "-");

            return strPublishTime;
        }

        // �Ƿ���ĵ�һ��?
        internal bool IsYearFirstIssue(IssueBindingItem issue)
        {
            int index = this.Issues.IndexOf(issue);
            Debug.Assert(index != -1, "");

            if (index == -1)
                return false;

            if (index == 0)
                return true;

            IssueBindingItem prev_issue = this.Issues[index - 1];
            string strThisYear = IssueUtil.GetYearPart(issue.PublishTime);
            string strPrevYear = IssueUtil.GetYearPart(prev_issue.PublishTime);

            if (String.Compare(strThisYear, strPrevYear) > 0)
                return true;

            return false;
        }

        void PaintIssues(
            long x0,
            long y0,
            PaintEventArgs e)
        {
            long x = x0;
            long y = y0;

            // ������������ĸ߶�
            this.m_lContentHeight = this.m_nCellHeight * this.Issues.Count;

            bool bDrawBottomLine = true;    // �Ƿ�Ҫ���·�����

            long lIssueWidth = this.m_lContentWidth;
            long lIssueHeight = this.m_nCellHeight; // issue.Height;

            // �Ż�
            int nStartLine = (int)((e.ClipRectangle.Top - y) / lIssueHeight);
            nStartLine = Math.Max(0, nStartLine);
            y += nStartLine * lIssueHeight;

            for (int i = nStartLine; i < this.Issues.Count; i++)
            {
                // �Ż�
                if (y > e.ClipRectangle.Bottom)
                {
                    bDrawBottomLine = false;
                    break;
                }

                IssueBindingItem issue = this.Issues[i];

                if (TooLarge(x) == true
                    || TooLarge(y) == true)
                    goto CONTINUE;

                // �Ż�
                RectangleF rect = new RectangleF((int)x,
                    (int)y,
                    lIssueWidth,
                    lIssueHeight);

                if (rect.IntersectsWith(e.ClipRectangle) == false)
                    goto CONTINUE;

                issue.Paint(i, (int)x, (int)y, e);

            CONTINUE:
                y += lIssueHeight;
                //  lHeight += lIssueHeight;
            }

            long lHeight = lIssueHeight * this.Issues.Count;

            // �ҡ�������

            Pen penFrame = new Pen(Color.FromArgb(50, Color.Gray), (float)1);

            // �ҷ�����
            if (TooLarge(x0 + this.m_lContentWidth) == false)
            {
                e.Graphics.DrawLine(penFrame,
                    new PointF((int)x0 + this.m_lContentWidth, (int)y0),
                    new PointF((int)x0 + this.m_lContentWidth, (int)(y0 + lHeight))
                    );
            }

            // �·�����
            if (bDrawBottomLine == true
                && TooLarge(y0 + lHeight) == false)
            {

                e.Graphics.DrawLine(penFrame,
                    new PointF((int)x0 + this.m_nLeftTextWidth, (int)(y0 + lHeight)),
                    new PointF((int)x0 + this.m_lContentWidth, (int)(y0 + lHeight))
                    );
            }

#if NOOOOOOOOOOOOOOOO
            // ���ƺ϶�����Χ�ķ���
            // Debug.WriteLine("Draw border clip=" + e.ClipRectangle.ToString());
            // �����϶����������
            for (int i = 0; i < this.ParentItems.Count; i++)
            {
                ItemBindingItem parent_item = this.ParentItems[i];

                IssueBindingItem issue = parent_item.Container; // ��װ���������
                Debug.Assert(issue != null, "");

                // �ҵ��к�
                int nLineNo = this.Issues.IndexOf(issue);
                Debug.Assert(nLineNo != -1, "");
                if (nLineNo == -1)
                    continue;

                long lStartY = (long)this.m_nTopBlank + (long)m_nCellHeight * (long)nLineNo;
                int nWidth = m_nCellWidth * 2;

                int nCol = issue.IndexOfItem(parent_item);
                Debug.Assert(nCol != -1, "");
                if (nCol == -1)
                    continue;

                long lStartX = (long)this.m_nLeftBlank
                    + (long)this.m_nLeftTextWidth
                    + (long)nCol * (long)m_nCellWidth;

                // ������ֱ����������ٸ���
                int nIssueCount = 0;
                if (parent_item.MemberCells.Count == 0)
                    nIssueCount = 1;
                else
                {
                    // TODO: Ҫ��֤item.MemberCells�����ж����������
                    IssueBindingItem tail_issue = parent_item.MemberCells[parent_item.MemberCells.Count - 1].Container;// item.MemberItems[item.MemberItems.Count - 1].Container;
                    Debug.Assert(tail_issue != null, "");
                    // �ҵ��к�
                    int nTailLineNo = this.Issues.IndexOf(tail_issue);
                    Debug.Assert(nTailLineNo != -1, "");

                    nIssueCount = nTailLineNo - nLineNo + 1;
                }

                long lThisHeight = m_nCellHeight * nIssueCount;

                {
                    RectangleF rect = new RectangleF((float)(lStartX + this.m_lWindowOrgX),
                        (float)(lStartY + this.m_lWindowOrgY),
                        (float)nWidth,
                        (float)lThisHeight);

                    if (rect.IntersectsWith(e.ClipRectangle) == false)
                        continue;

                    Pen penBorder = null;
                    Brush brushInner = null;

                    if (CheckProcessingState(parent_item) == false)
                    {
                        Color colorBorder = this.FixedBorderColor;
                        penBorder = new Pen(Color.FromArgb(100, colorBorder),
                            (float)8);  // �̻�����ɫʵ��
                        // brushInner = new SolidBrush(Color.FromArgb(30, Color.Green));
                    }
                    else
                    {
                        Color colorBorder = this.NewlyBorderColor;
                        Brush brush = new HatchBrush(HatchStyle.WideDownwardDiagonal,
                            Color.FromArgb(0, 255, 255, 255),
                            Color.FromArgb(255, colorBorder)
                            );    // back
                        penBorder = new Pen(brush,
                            (float)4);  // ���޸ģ���ɫ����
                        // penBorder.Alignment = PenAlignment.
                    }

                    float delta = (penBorder.Width/2) + 1;
                    rect.Inflate(-delta,-delta);

                    e.Graphics.RenderingOrigin = new Point((int)rect.X, (int)rect.Y);
                    BindingControl.RoundRectangle(e.Graphics,
                        penBorder,
                        brushInner,
                        rect,
                        10);
                }

                /*
                e.Graphics.DrawRectangle(penBorder,
                    (float)(lStartX + this.m_lWindowOrgX + 2),
                    (float)(lStartY + this.m_lWindowOrgY + 2),
                    (float)nWidth - 4,
                    (float)lThisHeight - 4);
                 * */
                /*
                Debug.WriteLine("rect="
                    + lStartX.ToString()
                    + ","
                    + lStartY.ToString()
                    + ","
                    + nWidth.ToString()
                    + ","
                    + lThisHeight.ToString());
                 * */

            }
#endif
            // ���ƺ϶�����Χ��������
            // �����϶����������
            for (int i = 0; i < this.ParentItems.Count; i++)
            {
                ItemBindingItem parent_item = this.ParentItems[i];

                if (parent_item.Container == null)
                    continue;

                PointF[] points = null;
                RectangleF rectBound;

                Debug.Assert(parent_item.Container != null, "");

                bool bAllBindingLayout = GetBoundPoints(
                    parent_item,
                    this.m_lWindowOrgX + this.m_nLeftBlank + this.m_nLeftTextWidth,
                    this.m_lWindowOrgY + this.m_nTopBlank,
                    out points,
                    out rectBound);

                // �Ż�
                if (rectBound.IntersectsWith(e.ClipRectangle) == false)
                    continue;

                if (points.Length == 0)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                Color colorBorder;
                Pen penBorder = null;
                Brush brushInner = null;

                if (CheckProcessingState(parent_item) == false)
                {
                    colorBorder = this.FixedBorderColor;
                    penBorder = new Pen(Color.FromArgb(150, colorBorder),
                        (float)4);  // �̻�
                    // brushInner = new SolidBrush(Color.FromArgb(30, Color.Green));
                }
                else
                {
                    colorBorder = this.NewlyBorderColor;
                    Brush brush = new HatchBrush(HatchStyle.WideDownwardDiagonal,
                        Color.FromArgb(0, 255, 255, 255),
                        Color.FromArgb(255, colorBorder)
                        );    // back
                    penBorder = new Pen(brush,
                        (float)4);  // ���޸�
                    // penBorder.Alignment = PenAlignment.
                }

                e.Graphics.RenderingOrigin = Point.Round(points[0]);

                // ���Ʒ���
                if (bAllBindingLayout == true)
                {
                    float delta = (penBorder.Width / 2) + 1;
                    rectBound.Inflate(-delta, -delta);

                    BindingControl.RoundRectangle(e.Graphics,
    penBorder,
    brushInner,
    rectBound,
    10);
                    continue;
                }

                        // �������ƫ����
                int nOffset = GetVerticalOffset(parent_item.Container,
                    parent_item);
                if (nOffset == 0)
                {
                    if (this.LineStyle == BoundLineStyle.Line)
                        e.Graphics.DrawLines(penBorder, points);
                    else
                        e.Graphics.DrawCurve(penBorder, points);
                }
                else
                {
                    int nStep = (this.m_nCellHeight - this.CellMargin.Vertical - this.CellPadding.Vertical) / 4;
                    nOffset = (nOffset % 5);
                    if ((nOffset % 2) == 1)
                    {
                        nOffset = -1 * ((nOffset + 1) / 2);
                    }
                    else
                    {
                        nOffset = nOffset / 2;
                    }

                    if (points.Length >= 2)
                    {
                        points[0].Y += nOffset * nStep;
                        points[1].Y += nOffset * nStep;
                    }

                    if (this.LineStyle == BoundLineStyle.Line)
                        e.Graphics.DrawLines(penBorder, points);
                    else
                        e.Graphics.DrawCurve(penBorder, points);
                }

                PaintCenterDots(points,
                    colorBorder,
                    e);
            }
        }

        // �������ƫ����
        static int GetVerticalOffset(IssueBindingItem issue,
            ItemBindingItem parent_item)
        {
            int nOffset = 0;
            for (int i = 0; i < issue.Cells.Count; i++)
            {
                Cell cell = issue.Cells[i];
                if (cell == null || cell.item == null)
                    continue;
                if (cell.item == parent_item)
                    break;
                if (cell.item.IsParent == true)
                    nOffset++;
            }

            return nOffset;
        }

        void PaintCenterDots(PointF[] points,
            Color colorBorder,
            PaintEventArgs e)
        {
            // �����ż��

            int nLargeCircleWidth = 16; // ��һ����ȦȦ��ֱ��
            int nCircleWidth = 10;  // ����СȦȦ��ֱ��

            Pen pen = new Pen(colorBorder);
            Brush brush = new SolidBrush(Color.White);
            for (int i = 0; i < points.Length; i++)
            {
                PointF point = points[i];
                RectangleF rect;
                if (i == 0)
                {
                    rect = new RectangleF(point.X - nLargeCircleWidth / 2,
                      point.Y - nLargeCircleWidth / 2,
                      nLargeCircleWidth,
                      nLargeCircleWidth);
                }
                else
                {
                    rect = new RectangleF(point.X - nCircleWidth / 2,
                       point.Y - nCircleWidth / 2,
                       nCircleWidth,
                       nCircleWidth);
                }

                // �Ż�
                if (rect.IntersectsWith(e.ClipRectangle) == false)
                    continue;

                Circle(e.Graphics, pen, brush, rect);
            }
        }

        bool GetBoundPoints(ItemBindingItem parent_item,
            long lStartX,
            long lStartY,
            out PointF[] results,
            out RectangleF rectBound)
        {
            results = new PointF[0];
            rectBound = new RectangleF();
            List<PointF>  points = new List<PointF>();
            IssueBindingItem parent_issue = parent_item.Container; // ��װ���������
            Debug.Assert(parent_issue != null, "");

            // �ҵ���ʼ�к�
            int nStartLineNo = this.Issues.IndexOf(parent_issue);
            Debug.Assert(nStartLineNo != -1, "");
            if (nStartLineNo == -1)
                return true;

            int nParentCol = parent_issue.IndexOfItem(parent_item);
            Debug.Assert(nParentCol != -1, "");


            {
                long y = lStartY
        + (long)m_nCellHeight * (long)nStartLineNo
        + m_nCellHeight / 2;
                long x = lStartX
                    + (long)m_nCellWidth * nParentCol
                    + m_nCellWidth / 2;
                points.Add(new PointF(x, y));

                rectBound = new RectangleF(
                    x - m_nCellWidth / 2,
                    y - m_nCellHeight/2,
                    m_nCellWidth,
                    m_nCellHeight);
            }


            bool bAllBindingLayout = true;

            for (int i = 0; i < parent_item.MemberCells.Count; i++)
            {
                Cell member_cell = parent_item.MemberCells[i];
                if (member_cell == null)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                IssueBindingItem member_issue = member_cell.Container;
                Debug.Assert(member_issue != null, "");

                if (member_issue.IssueLayoutState != IssueLayoutState.Binding)
                    bAllBindingLayout = false;

                int nMemberLineNo = this.Issues.IndexOf(member_issue);
                Debug.Assert(nMemberLineNo != -1, "");

                int nMemberCol = member_issue.Cells.IndexOf(member_cell);
                Debug.Assert(nMemberCol != -1, "");

                long y = lStartY
                    + (long)m_nCellHeight * (long)nMemberLineNo
                    + m_nCellHeight / 2;
                long x = lStartX
                    + (long)m_nCellWidth * nMemberCol
                    + m_nCellWidth / 2;
                points.Add(new PointF(x, y));

                RectangleF rectCell;
                rectCell = new RectangleF(
    x - m_nCellWidth / 2,
    y - m_nCellHeight / 2,
    m_nCellWidth,
    m_nCellHeight);
                rectBound = RectangleF.Union(rectCell, rectBound);
            }

            results = new PointF[points.Count];
            points.CopyTo(results);

            /*
            if (bAllBindingLayout == true)
            {
                rectBound = new RectangleF(points[0].X - this.m_nCellWidth / 2,
                    points[0].Y - this.m_nCellHeight / 2,
                    this.m_nCellWidth * 2,
                    (points.Length - 1) * m_nCellHeight);
            }
             * */

            return bAllBindingLayout;
        }


        public enum ScrollBarMember
        {
            Vert = 0,
            Horz = 1,
            Both = 2,
        }

        // ���һ��long�Ƿ�Խ��int16�ܱ���ֵ��Χ
        public static bool TooLarge(long lValue)
        {
            if (lValue >= Int16.MaxValue || lValue <= Int16.MinValue)
                return true;
            return false;
        }

        static string GetString(API.ScrollInfoStruct si)
        {
            string strResult = "";
            strResult += "si.nMin:" + si.nMin.ToString() + "\r\n";
            strResult += "si.nMax:" + si.nMax.ToString() + "\r\n";
            strResult += "si.nPage:" + si.nPage.ToString() + "\r\n";
            strResult += "si.nPos:" + si.nPos.ToString() + "\r\n";
            return strResult;
        }

        void SetScrollBars(ScrollBarMember member)
        {

            nNestedSetScrollBars++;


            try
            {
                int nClientWidth = this.ClientSize.Width;
                int nClientHeight = this.ClientSize.Height;

                // �ĵ��ߴ�
                long lDocumentWidth = DocumentWidth;
                long lDocumentHeight = DocumentHeight;

                long lWindowOrgX = this.m_lWindowOrgX;
                long lWindowOrgY = this.m_lWindowOrgY;

                if (member == ScrollBarMember.Horz
                    || member == ScrollBarMember.Both)
                {

                    if (TooLarge(lDocumentWidth) == true)
                    {
                        this.m_h_ratio = (double)(Int16.MaxValue - 1) / (double)lDocumentWidth;

                        lDocumentWidth = (long)((double)lDocumentWidth * m_h_ratio);
                        nClientWidth = (int)((double)nClientWidth * m_h_ratio);
                        lWindowOrgX = (long)((double)lWindowOrgX * m_h_ratio);
                    }
                    else
                        this.m_h_ratio = 1.0F;

                    // ˮƽ����
                    API.ScrollInfoStruct si = new API.ScrollInfoStruct();

                    si.cbSize = Marshal.SizeOf(si);
                    si.fMask = API.SIF_RANGE | API.SIF_POS | API.SIF_PAGE;
                    si.nMin = 0;
                    si.nMax = (int)lDocumentWidth;
                    si.nPage = nClientWidth;
                    si.nPos = -(int)lWindowOrgX;
                    API.SetScrollInfo(this.Handle, API.SB_HORZ, ref si, true);

                    // Debug.WriteLine("SetScrollInfo() HORZ\r\n" + GetString(si));
                }


                if (member == ScrollBarMember.Vert
                    || member == ScrollBarMember.Both)
                {
                    if (TooLarge(lDocumentHeight) == true)
                    {
                        this.m_v_ratio = (double)(Int16.MaxValue - 1) / (double)lDocumentHeight;

                        lDocumentHeight = (long)((double)lDocumentHeight * m_v_ratio);
                        nClientHeight = (int)((double)nClientHeight * m_v_ratio);
                        lWindowOrgY = (long)((double)lWindowOrgY * m_v_ratio);

                    }
                    else
                        this.m_v_ratio = 1.0F;

                    // ��ֱ����
                    API.ScrollInfoStruct si = new API.ScrollInfoStruct();

                    si.cbSize = Marshal.SizeOf(si);
                    si.fMask = API.SIF_RANGE | API.SIF_POS | API.SIF_PAGE;
                    si.nMin = 0;
                    si.nMax = (int)lDocumentHeight;
                    si.nPage = nClientHeight;
                    si.nPos = -(int)lWindowOrgY;
                    // Debug.Assert(si.nPos != 0, "");
                    API.SetScrollInfo(this.Handle, API.SB_VERT, ref si, true);

                    // Debug.WriteLine("SetScrollInfo() VERT\r\n" + GetString(si));
                }

            }
            finally
            {
                nNestedSetScrollBars--;
            }
        }

        public long DocumentWidth
        {
            get
            {
                return m_lContentWidth + (long)m_nLeftBlank + (long)m_nRightBlank;
            }

        }
        public long DocumentHeight
        {
            get
            {
                return m_lContentHeight + (long)m_nTopBlank + (long)m_nBottomBlank;
            }
        }

        public long DocumentOrgX
        {
            get
            {
                return m_lWindowOrgX;
            }
            set
            {
                long lWidth = DocumentWidth;
                int nViewportWidth = this.ClientSize.Width;

                long lWindowOrgX_old = m_lWindowOrgX;


                if (nViewportWidth >= lWidth)
                    m_lWindowOrgX = 0;
                else
                {
                    if (value <= -lWidth + nViewportWidth)
                        m_lWindowOrgX = -lWidth + nViewportWidth;
                    else
                        m_lWindowOrgX = value;

                    if (m_lWindowOrgX > 0)
                        m_lWindowOrgX = 0;
                }

                // AfterDocumentChanged(ScrollBarMember.Horz);
                SetScrollBars(ScrollBarMember.Horz);



                if (this.BackgroundImage != null)
                {
                    this.Invalidate();
                    return;
                }

                long lDelta = m_lWindowOrgX - lWindowOrgX_old;

                if (lDelta != 0)
                {
                    // �������ľ��볬��32λ������Χ
                    if (lDelta >= Int32.MaxValue || lDelta <= Int32.MinValue)
                        this.Invalidate();
                    else
                    {
                        RECT rect1 = new RECT();
                        rect1.left = 0;
                        rect1.top = 0;
                        rect1.right = this.ClientSize.Width;
                        rect1.bottom = this.ClientSize.Height;


                        API.ScrollWindowEx(this.Handle,
                            (int)lDelta,
                            0,
                            ref rect1,
                            IntPtr.Zero,	//	ref RECT lprcClip,
                            0,	// int hrgnUpdate,
                            IntPtr.Zero,	// ref RECT lprcUpdate,
                            API.SW_INVALIDATE /*| API.SW_SCROLLCHILDREN*/ /*int fuScroll*/);
                    }
                }

                // this.Invalidate();
            }
        }

        public long DocumentOrgY
        {
            get
            {
                return m_lWindowOrgY;
            }
            set
            {
                // Debug.Assert(value != 0, "");
                long lHeight = DocumentHeight;
                int nViewportHeight = this.ClientSize.Height;

                long lWindowOrgY_old = m_lWindowOrgY;

                if (nViewportHeight >= lHeight)
                    m_lWindowOrgY = 0;
                else
                {
                    if (value <= -lHeight + nViewportHeight)
                        m_lWindowOrgY = -lHeight + nViewportHeight;
                    else
                        m_lWindowOrgY = value;

                    if (m_lWindowOrgY > 0)
                        m_lWindowOrgY = 0;
                }


                // AfterDocumentChanged(ScrollBarMember.Vert);
                SetScrollBars(ScrollBarMember.Vert);

                if (this.BackgroundImage != null)
                {
                    this.Invalidate();
                    return;
                }

                long lDelta = m_lWindowOrgY - lWindowOrgY_old;
                if (lDelta != 0)
                {
                    // �������ľ��볬��32λ������Χ
                    if (lDelta >= Int32.MaxValue || lDelta <= Int32.MinValue)
                        this.Invalidate();
                    else
                    {

                        RECT rect1 = new RECT();
                        rect1.left = 0;
                        rect1.top = 0;
                        rect1.right = this.ClientSize.Width;
                        rect1.bottom = this.ClientSize.Height;


                        API.ScrollWindowEx(this.Handle,
                            0,
                            (int)lDelta,
                            ref rect1,
                            IntPtr.Zero,	//	ref RECT lprcClip,
                            0,	// int hrgnUpdate,
                            IntPtr.Zero,	// ref RECT lprcUpdate,
                            API.SW_INVALIDATE /*| API.SW_SCROLLCHILDREN*/ /*int fuScroll*/);

                    }

                }

                // this.Invalidate();
            }
        }

        #endregion

        protected override void OnPaint(PaintEventArgs pe)
        {
            // TODO: Add custom paint code here

            // Calling the base class OnPaint
            base.OnPaint(pe);

            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            pe.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            // e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            // e.Graphics.SetClip(e.ClipRectangle); // �ϻ�

            long xOffset = m_lWindowOrgX + m_nLeftBlank;
            long yOffset = m_lWindowOrgY + m_nTopBlank;

            this.PaintIssues(xOffset, yOffset, pe);
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case API.WM_GETDLGCODE:
                    m.Result = new IntPtr(API.DLGC_WANTALLKEYS | API.DLGC_WANTARROWS | API.DLGC_WANTCHARS);
                    return;
                case API.WM_VSCROLL:
                    {
                        int CellWidth = this.m_nCellHeight/2;
                        switch (API.LoWord(m.WParam.ToInt32()))
                        {
                            case API.SB_BOTTOM:
                                break;
                            case API.SB_TOP:
                                break;
                            case API.SB_THUMBPOSITION:
                            case API.SB_THUMBTRACK:
                                this.Update();
                                int v = API.HiWord(m.WParam.ToInt32());
                                if (this.m_v_ratio != 1.0F)
                                    DocumentOrgY = -(long)((double)v / this.m_v_ratio);
                                else
                                    DocumentOrgY = -v;
                                break;
                            case API.SB_LINEDOWN:
                                DocumentOrgY -= (int)CellWidth;
                                break;
                            case API.SB_LINEUP:
                                DocumentOrgY += (int)CellWidth;
                                break;
                            case API.SB_PAGEDOWN:
                                DocumentOrgY -= this.ClientSize.Height;
                                break;
                            case API.SB_PAGEUP:
                                DocumentOrgY += this.ClientSize.Height;
                                break;
                        }
                        // MessageBox.Show("this");
                    }
                    break;

                case API.WM_HSCROLL:
                    {

                        int CellWidth = this.m_nCellWidth/2;
                        switch (API.LoWord(m.WParam.ToInt32()))
                        {
                            case API.SB_THUMBPOSITION:
                            case API.SB_THUMBTRACK:
                                int v = API.HiWord(m.WParam.ToInt32());
                                if (this.m_h_ratio != 1.0F)
                                    DocumentOrgX = -(long)((double)v / this.m_h_ratio);
                                else
                                    DocumentOrgX = -v;
                                break;
                            case API.SB_LINEDOWN:
                                DocumentOrgX -= CellWidth;
                                break;
                            case API.SB_LINEUP:
                                DocumentOrgX += CellWidth;
                                break;
                            case API.SB_PAGEDOWN:
                                DocumentOrgX -= this.ClientSize.Width;
                                break;
                            case API.SB_PAGEUP:
                                DocumentOrgX += this.ClientSize.Width;
                                break;
                        }
                    }
                    break;

                default:
                    break;

            }

            base.DefWndProc(ref m);
        }


        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams param = base.CreateParams;

                if (borderStyle == BorderStyle.FixedSingle)
                {
                    param.Style |= API.WS_BORDER;
                }
                else if (borderStyle == BorderStyle.Fixed3D)
                {
                    param.ExStyle |= API.WS_EX_CLIENTEDGE;
                }

                return param;
            }
        }



        private void BorderStyleToWindowStyle(ref int style, ref int exStyle)
        {
            style &= ~API.WS_BORDER;
            exStyle &= ~API.WS_EX_CLIENTEDGE;
            switch (borderStyle)
            {
                case BorderStyle.Fixed3D:
                    exStyle |= API.WS_EX_CLIENTEDGE;
                    break;

                case BorderStyle.FixedSingle:
                    style |= API.WS_BORDER;
                    break;

                case BorderStyle.None:
                    // No border style values
                    break;
            }
        }

        // �����¼��ӿ�this.GetItemInfo���������Ĳ���Ϣ
        // return:
        //      -1  error
        //      >-0 ����ü�¼������(XmlRecords.Count)
        internal int DoGetItemInfo(string strPublishTime,
            out List<string> XmlRecords,
            out string strError)
        {
            strError = "";

            XmlRecords = new List<string>();

            if (this.GetItemInfo == null)
            {
                strError = "��δ�ҽ�GetItemInfo�¼�";
                return -1;
            }


            GetItemInfoEventArgs e1 = new GetItemInfoEventArgs();
            e1.BiblioRecPath = "";
            e1.PublishTime = strPublishTime;
            this.GetItemInfo(this, e1);
            if (String.IsNullOrEmpty(e1.ErrorInfo) == false)
            {
                strError = "�ڻ�ȡ�����ڳ�������Ϊ '" + strPublishTime + "' �Ĳ���Ϣ�Ĺ����з�������: " + e1.ErrorInfo;
                return -1;
            }

            XmlRecords = e1.ItemXmls;

            return XmlRecords.Count;
        }

        // ģ��MouseMove�¼����¾��
        private void timer_dragScroll_Tick(object sender, EventArgs e)
        {
            if (this.Capture == false)
                return;

            Point p = this.PointToClient(Control.MousePosition);
            MouseEventArgs e1 = new MouseEventArgs(MouseButtons.Left,
                0, // clicks,
                p.X,
                p.Y,
                0 //delta
                );
            // this.mouseMoveArgs
            this.OnMouseMove(e1);
        }

#if NO
        public int BuildVolumeStrings(string strText,
            out string strYear,
            out string strVolumn,
            out string strZong,
            out string strNo)
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

#endif
    }

    // ��������
    internal class HitTestResult
    {
        public Object Object = null;    // �������ĩ������
        public AreaPortion AreaPortion = AreaPortion.None;

        // ���������µĵ��λ��
        public long X = -1;
        public long Y = -1;

        public int Param = 0;   // ��������
    }

    // ��������
    internal enum AreaPortion
    {
        None = 0,
        Blank = 1,    // �հײ��֡�ָCell�������쵽�Ĳ��֣����߿յ�Cell��������λ��

        Content = 3,    // ���ݱ���
        LeftText = 4,   // ��ߵ����֣�ָIssueBindingItem

        LeftBlank = 5,  // ��߿հ�
        TopBlank = 6,   // �Ϸ��հ�
        RightBlank = 7, // �ҷ��հ�
        BottomBlank = 8,    // �·��հ�

        Grab = 9,   // moving grab handle
        CheckBox = 10,  // ���������checkbox

        // NullCell = 10,  // Ǳ�ڸ���λ��
    }

    // ѡ��һ������Ķ���
    internal enum SelectAction
    {
        Toggle = 0,
        On = 1,
        Off = 2,
    }


    /// <summary>
    /// ���㷢���ı��¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void FocusChangedEventHandler(object sender,
    FocusChangedEventArgs e);

    /// <summary>
    /// ���㷢���ı��¼��Ĳ���
    /// </summary>
    public class FocusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// ��ǰ�������ڶ���
        /// </summary>
        public object OldFocusObject = null;
        /// <summary>
        /// ���ڽ������ڶ���
        /// </summary>
        public object NewFocusObject = null;
    }

    /// <summary>
    /// �༭�������¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void EditAreaEventHandler(object sender,
        EditAreaEventArgs e);

    /// <summary>
    /// �༭�������¼��Ĳ���
    /// </summary>
    public class EditAreaEventArgs : EventArgs
    {
        /// <summary>
        /// [in] ����
        /// </summary>
        public string Action = "";  // [in] ����

        /// <summary>
        /// [out] ���
        /// </summary>
        public string Result = "";  // [out] ���
    }

    internal class ItemAndCol
    {
        public ItemBindingItem item = null;
        public int Index = -1;
    }
}
