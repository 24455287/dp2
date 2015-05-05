using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Diagnostics;

using System.Runtime.InteropServices;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Range;

namespace DigitalPlatform.CommonDialog
{
    // ����������Ļ���
    public class AreaBase
    {
        internal bool m_bSelected = false;
        internal bool m_bFocus = false;

        internal long m_lWidthCache = -1;
        internal long m_lHeightCache = -1;

        public AreaBase _Container = null;

        public List<AreaBase> ChildrenCollection = new List<AreaBase>();

        public int NameValue = 0;   // 0 ��ʾ��δ��ʼ��

        public AreaBase FirstChild
        {
            get
            {
                if (this.ChildrenCollection.Count == 0)
                    return null;
                return this.ChildrenCollection[0];
            }
        }

        public AreaBase LastChild
        {
            get
            {
                if (this.ChildrenCollection.Count == 0)
                    return null;
                return this.ChildrenCollection[this.ChildrenCollection.Count-1];
            }
        }

        public AreaBase EdgeChild(bool bHead)
        {
            if (this.ChildrenCollection.Count == 0)
                return null;
            if (bHead == true)
                return this.ChildrenCollection[0];
            else
                return this.ChildrenCollection[this.ChildrenCollection.Count - 1];
        }

        // �������ı���
        // �������ȵ�·��, ȫ�����
        public virtual void ClearCache()
        {
            AreaBase obj = this;

            while (obj != null)
            {
                obj.m_lWidthCache = -1;
                obj.m_lHeightCache = -1;

                obj = obj._Container;
            }
        }

        // Ϊ�¼��Լ����¼���selected������״̬ (�������Լ�)
        // parameters:
        public void SetChildrenDayState(int nState,
            ref List<AreaBase> update_objects,
            int nMaxCount)
        {
            for (int i = 0; i < this.ChildrenCollection.Count; i++)
            {
                AreaBase obj = this.ChildrenCollection[i];

                // �����DayArea����
                if (obj is DayArea)
                {
                    DayArea day = (DayArea)obj;

                    if (obj.m_bSelected == true
                        && day.State != nState
                        && day.Blank == false)
                    {

                        day.State = nState;
                        if (update_objects.Count < nMaxCount)
                            update_objects.Add(obj);
                    }
                }
                else
                {
                    // �ݹ�
                    obj.SetChildrenDayState(nState,
                        ref update_objects,
                        nMaxCount);
                }
            }

        }


        /*
        // Ϊ�¼��Լ����¼���selected������״̬ (�������Լ�)
        // parameters:
        //      bForce  ���Ϊtrue�����ʾ�����Ƿ���ѡ���ǣ����޸�״̬
        //              ���Ϊfalse������ѡ���ǵĲ��޸�״̬
        public void SetChildrenDayState(int nState,
            bool bForce,
            ref List<AreaBase> update_objects,
            int nMaxCount)
        {
            for (int i = 0; i < this.ChildrenCollection.Count; i++)
            {
                AreaBase obj = this.ChildrenCollection[i];

                // �����DayArea����
                if (obj is DayArea)
                {
                    DayArea day = (DayArea)obj;

                    if (( obj.m_bSelected == true || bForce == true)
                        && day.State != nState
                        && day.Blank == false)
                    {

                        day.State = nState;
                        if (update_objects.Count < nMaxCount)
                            update_objects.Add(obj);
                    }
                }
                else
                {
                    bool bNewForce = false;

                    // ���һ��������Ȼ����DayArea���󣬵�������Ѿ���ѡ���Ǿ���ζ�����¼�ȫ��DayArea����Ҫǿ�Ʊ�����״̬
                    if (bForce == true
                        || obj.m_bSelected == true)
                        bNewForce = true;

                    // �ݹ�
                    obj.SetChildrenDayState(nState,
                        bNewForce,
                        ref update_objects,
                        nMaxCount);
                }
            }

            
        }
         * */

        // �¼��Լ����¼��Ƿ���selected? (�������Լ�)
        public bool HasChildrenSelected()
        {
            for (int i = 0; i < this.ChildrenCollection.Count; i++)
            {
                AreaBase obj = this.ChildrenCollection[i];
                if (obj.m_bSelected == true)
                    return true;

                // �ݹ�
                if (obj.HasChildrenSelected() == true)
                    return true;
            }

            return false;
        }

        // ��������¼�����
        public virtual void Clear()
        {
            this.ChildrenCollection.Clear();

            m_bSelected = false;

            m_lWidthCache = -1;
            m_lHeightCache = -1;

            NameValue = 0;   // 0 ��ʾ��δ��ʼ��
        }

        // �����ǰ�������Լ�ȫ���¼���ѡ���־
        public void ClearAllSubSelected()
        {
            this.m_bSelected = false;

            for (int i = 0; i < this.ChildrenCollection.Count; i++)
            {
                this.ChildrenCollection[i].ClearAllSubSelected();
            }
        }

        // �����ǰ�������Լ�ȫ���¼���ѡ���־, ��������Ҫˢ�µĶ���
        public void ClearAllSubSelected(ref List<AreaBase> objects,
            int nMaxCount)
        {

            // �޸Ĺ��Ĳż�������
            if (this.m_bSelected == true && objects.Count < nMaxCount)
                objects.Add(this);

            this.m_bSelected = false;

            for (int i = 0; i < this.ChildrenCollection.Count; i++)
            {
                this.ChildrenCollection[i].ClearAllSubSelected(ref objects,
                    nMaxCount);
            }
        }

        // ���ݸ�����NameValueֵ, �ӵ�ǰ����ʼ(������ǰ����) ��λ�������
        public AreaBase FindByNameValue(List<int> values)
        {
            if (values == null)
            {
                Debug.Assert(false, "values����Ϊ��");
                return null;
            }

            if (values.Count == 0)
            {
                Debug.Assert(false, "values.Count����Ϊ0");
                return null;
            }

            if (values[0] == -1 // -1��ʾͨ���
                 || this.NameValue == values[0])
            {
                if (values.Count == 1)
                    return this;

                Debug.Assert(values.Count > 1, "");
                // ����������

                // ���̲���һ��
                List<int> temp = new List<int>();
                temp.AddRange(values);
                temp.RemoveAt(0);

                // ����������
                // �ݹ鼴��
                for (int i = 0; i < this.ChildrenCollection.Count; i++)
                {
                    AreaBase obj = this.ChildrenCollection[i];

                    AreaBase result_obj = obj.FindByNameValue(temp);
                    if (result_obj != null)
                        return result_obj;

                    // �Ż�
                    // �����obj�Լ���һ��ƥ��ɹ�����������û��ƥ��ɹ�
                    if (temp[0] != -1 // -1��ʾͨ���
                        && obj.NameValue == temp[0])
                    {
                        return null;
                    }
            
                    /*
                    if (temp[0] != -1)
                        return null;    // �������ͨ���ģʽ, ���˾ͽ�����
                     * */
                }

                return null;

            }
            else
                return null;


        }

        // �ҵ���һ��ͬ�����󣨿����ǿ�Խ���ס��汲�ģ�
        public AreaBase GetNextSibling()
        {
            if (this._Container == null)
                return null;

            List<AreaBase> children = this._Container.ChildrenCollection;

            for (int i = 0; i < children.Count - 1; i++)
            {
                if (children[i] == this)
                {
                    return children[i + 1];
                }
            }

            // List<AreaBase> stack = new List<AreaBase>();

            AreaBase parent = this._Container;
            //    stack.Add(this._Container);

            // û���ҵ�
            for (; ; )
            {
                // �ҵ����׵��ֵ�
                AreaBase parent_sibling = parent.GetNextSibling();
                if (parent_sibling == null)
                    return null;

                List<AreaBase> temp_children = parent_sibling.ChildrenCollection;

                // �����ֵܵĵ�һ������
                if (temp_children.Count != 0)
                    return temp_children[0];

                // ��������Ҹ��׵��ֵ�

                parent = parent_sibling;
            }


            // return null;
        }

        // �ҵ�ǰһ��ͬ�����󣨿����ǿ�Խ���ס��汲�ģ�
        public AreaBase GetPrevSibling()
        {
            if (this._Container == null)
                return null;

            List<AreaBase> children = this._Container.ChildrenCollection;

            for (int i = children.Count - 1; i > 0; i--)
            {
                if (children[i] == this)
                {
                    return children[i - 1];
                }
            }

            AreaBase parent = this._Container;

            // û���ҵ�
            for (; ; )
            {
                // �ҵ����׵��ֵ�
                AreaBase parent_sibling = parent.GetPrevSibling();
                if (parent_sibling == null)
                    return null;

                List<AreaBase> temp_children = parent_sibling.ChildrenCollection;

                // �����ֵܵ���ĩһ������
                if (temp_children.Count != 0)
                    return temp_children[temp_children.Count-1];

                // ��������Ҹ��׵��ֵ�
                parent = parent_sibling;
            }


            // return null;
        }

        public virtual string FullName
        {
            get
            {
                return "��δʵ��";
            }
        }


        // return:
        //      true    ״̬�����仯
        //      false   ״̬û�б仯
        public bool Select(SelectAction action,
            bool bRecursive)
        {
            bool bOldSelected = this.m_bSelected;

            if (action == SelectAction.Off)
                this.m_bSelected = false;
            else if (action == SelectAction.On)
                this.m_bSelected = true;
            else
            {
                Debug.Assert(action == SelectAction.Toggle, "");
                if (this.m_bSelected == true)
                    this.m_bSelected = false;
                else
                    this.m_bSelected = true;
            }

            // �ݹ�
            if (bRecursive == true)
            {
                for (int i = 0; i < this.ChildrenCollection.Count; i++)
                {
                    AreaBase obj = this.ChildrenCollection[i];
                    obj.Select(action, true);
                }
            }

            return (bOldSelected == this.m_bSelected ? false : true);
        }

        public virtual long Width
        {
            get
            {
                throw new Exception("Width not implement");
            }
        }

        public virtual long Height
        {
            get
            {
                throw new Exception("Height not implement");
            }
        }

        // ����Ӷ����� ������������ϵ�е� ���Ͻ�λ��
        public virtual PointF GetChildLeftTopPoint(AreaBase child)
        {
            throw new Exception("��δʵ��");
        }

        // ��������������ϵ �ľ��� ת��Ϊ ��������������ϵ
        public virtual RectangleF ToRootCoordinate(RectangleF rect)
        {
            AreaBase obj = this;

            for (; ; )
            {
                AreaBase parent = obj._Container;
                if (parent == null)
                    break;

                PointF childStart = parent.GetChildLeftTopPoint(obj);

                // �任Ϊ��������
                rect.Offset(childStart.X, childStart.Y);

                obj = parent;
            }

            return rect;
        }

        // ������
        // parameters:
        //      colorBack   �����ı�����ɫ
        public virtual void PaintBack(
            long x0,
            long y0,
            long width,
            long height,
            PaintEventArgs e,
            Color colorBack)
        {
            RectangleF rect = new RectangleF(
                x0,
                y0,
                width,
                height);


            Rectangle rectClip = e.ClipRectangle;
            rectClip.Inflate(1, 1); // �����ľ��Σ�������float��ʽ�����׶�ʧ1����
            RectangleF result = RectangleF.Intersect(rect, rectClip);

            if (result.IsEmpty)
                return;

            /*
            // �Ż�
            if (rect.IntersectsWith(e.ClipRectangle) == false)
                return;
             * */

            Brush brush = null;
            /*
            if (this.m_bSelected == true)
            {
                // ��ѡ�к�ı�����ɫ
                brush = new SolidBrush(Color.LightGray);
            }
            else
            {*/
                // ����������ɫ
                brush = new SolidBrush(colorBack);
            /*
            }
             * */

            e.Graphics.FillRectangle(brush, result);
        }

        // ��ѡ��Ч��
        // parameters:
        public virtual void PaintSelectEffect(
            long x0,
            long y0,
            long width,
            long height,
            PaintEventArgs e)
        {
            if (this.m_bSelected == false)
                return;

            RectangleF rect = new RectangleF(
                x0,
                y0,
                width,
                height);


            Rectangle rectClip = e.ClipRectangle;
            rectClip.Inflate(1, 1); // �����ľ��Σ�������float��ʽ�����׶�ʧ1����
            RectangleF result = RectangleF.Intersect(rect, rectClip);

            if (result.IsEmpty)
                return;

            /*
            // �Ż�
            if (rect.IntersectsWith(e.ClipRectangle) == false)
                return;
             * */

            Brush brush = null;

            brush = new SolidBrush(Color.FromArgb(20, SystemColors.Highlight));

            e.Graphics.FillRectangle(brush, result);
        }
    }


    // ר��Ϊ���������Ԫ�������������Ƶ����������
    // ǿ���� T ��������� (TΪAreaBase��������)
    public class TypedList<T> where T : AreaBase
    {
        List<AreaBase> m_base_array = null;


        public TypedList(List<AreaBase> base_array)
        {
            this.m_base_array = base_array;
        }

        // ��һ��List<AreaBase>�͵�������������
        public void Link(List<AreaBase> base_array)
        {
            this.m_base_array = base_array;
        }

        public T this[int index]
        {
            get
            {
                return (T)this.m_base_array[index];
            }
            set
            {
                this.m_base_array[index] = value;
            }
        }

        public int Count
        {
            get
            {
                return this.m_base_array.Count;
            }
        }

        /*
        // �������ֶ��ṩ
        public int Length
        {
            get
            {
                return this.m_base_array.Count;
            }
        }*/

        public void Add(T obj)
        {
            this.m_base_array.Add(obj);
        }

        // 2010/3/21
        public void Remove(T obj)
        {
            this.m_base_array.Remove(obj);
        }

        public void Insert(int index, T obj)
        {
            this.m_base_array.Insert(index, obj);
        }

    }

    // ��AreaBase��ʵ��������֮���һ�������࣬��������һЩ�����ĳ�ʼ��ϸ�ڡ�
    public class NamedArea<ChildType> : AreaBase
        where ChildType : AreaBase
    {
        public TypedList<ChildType> ChildTypedCollection = null;

        public NamedArea()
        {
            this.ChildTypedCollection = new TypedList<ChildType>(this.ChildrenCollection);
        }
    }

    // ����������Ķ�������
    public class DataRoot : NamedArea<YearArea>
    {
        // public List<YearArea> YearCollection = new List<YearArea>();

        internal int m_nYearNameWidth = 100; // 50 // �����ʾ�����������Ŀ��
        internal int m_nMonthNameWidth = 80;     // �����ʾ�����������Ŀ��

        internal int m_nDayCellWidth = 100; // �ո��ӵĿ��
        internal int m_nDayCellHeight = 100;    // �ո��ӵĸ߶�

        internal Rectangle m_rectCheckBox = new Rectangle(4, 4, 16, 16); // checkbox����(��DayArea������)

        internal int m_nDayOfWeekTitleHeight = 30;   // ���ڱ���ĸ߶�

        internal string m_strDayOfWeekTitleLang = "zh";

        public Font DayTextFont = new Font("Arial Black", 12, FontStyle.Regular);

        public Font DaysOfWeekTitleFont = new Font("����_GB2312", 11, FontStyle.Regular);

        public Color YearBackColor = Color.White;

        public Color MonthBackColor = Color.White;

        public bool HoverCheckBox = false;

        // "Tahoma" "Verdana" "Jokerman" "Rockwell Extra Bold"
        // "Century Gothic" "Croobie"

        bool m_bBackColorTransparent = false;


        public DayStateDefCollection DayStateDefs = new DayStateDefCollection();

        // ���캯��
        public DataRoot()
        {
            this.DayTextFont = new Font("Arial Black", 12, FontStyle.Regular);
            if (m_strDayOfWeekTitleLang == "zh")
                this.DaysOfWeekTitleFont = new Font("����_GB2312", 11, FontStyle.Regular);
            else
                this.DaysOfWeekTitleFont = new Font("Arial", 11, FontStyle.Regular);


        }

        public bool BackColorTransparent
        {
            get
            {
                return m_bBackColorTransparent;
            }
            set
            {
                if (this.m_bBackColorTransparent == value)
                    return;

                this.m_bBackColorTransparent = value;

                if (this.m_bBackColorTransparent == true)
                {
                    // ��Ϊ͸��
                    if (this.YearBackColor.A >= 255)
                        this.YearBackColor = Color.FromArgb(100, this.YearBackColor);
                    if (this.MonthBackColor.A >= 255)
                        this.MonthBackColor = Color.FromArgb(100, this.MonthBackColor);
                }
                else
                {
                    // ��Ϊ��͸��
                    this.YearBackColor = Color.FromArgb(255, this.YearBackColor);
                    this.MonthBackColor = Color.FromArgb(255, this.MonthBackColor);
                }
            }
        }

        // ��ʱ�䷶Χɾ��һ��(ͷ������β��)
        public bool ShrinkYear(bool bHead)
        {
            YearArea first_year = null;

            if (this.YearCollection.Count > 0)
            {
                // �õ��ִ��һ��

                if (bHead == true)
                {
                    first_year = (YearArea)this.FirstChild;
                }
                else
                {
                    first_year = (YearArea)this.LastChild;
                }
            }
            else
            {
                return false;
            }

            this.YearCollection.Remove(first_year);
            this.ClearCache();

            return true;
        }

        // ��ʱ�䷶Χɾ��һ��(ͷ������β��)
        public bool ShrinkMonth(bool bHead)
        {
            YearArea first_year = null;

            if (this.YearCollection.Count == 0)
            {
                return false;
            }
            else
                first_year = (YearArea)this.EdgeChild(bHead);

            Debug.Assert(first_year != null, "");

            bool bRet = first_year.ShrinkMonth(bHead);

            // ע���ƺ�
            if (first_year.MonthCollection.Count == 0)
                this.YearCollection.Remove(first_year);

            return bRet;
        }

        // ��ʱ�䷶Χɾ��һ����
        public bool ShrinkWeek(bool bHead)
        {
            YearArea first_year = null;

            if (this.YearCollection.Count == 0)
            {
                return false;
            }
            else
                first_year = (YearArea)this.EdgeChild(bHead);

            Debug.Assert(first_year != null, "");

            MonthArea first_month = null;

            if (first_year.MonthCollection.Count == 0)
            {
                return false;
            }
            else
            {
                first_month = (MonthArea)first_year.EdgeChild(bHead);
            }

            Debug.Assert(first_month != null, "");

            bool bRet = first_month.ShrinkWeek(bHead);

            // ע���ƺ�
            if (bRet == true 
                && first_month.WeekCollection.Count == 0)
            {
                first_year.MonthCollection.Remove(first_month);
                if (first_year.MonthCollection.Count == 0)
                    this.YearCollection.Remove(first_year);
            }

            return bRet;
        }

        // ��ʱ�䷶Χ��չһ��
        // ע����Ҫ���ԭ�еĵ�һ��������һ���Ƿ�����
        public YearArea ExpandYear(bool bHead, 
            bool bEmpty)
        {
            int nNewYear = 0;

            if (this.YearCollection.Count > 0)
            {
            // �õ��ִ��һ��
                YearArea first_year = null;

                if (bHead == true)
                {
                    first_year = (YearArea)this.FirstChild;
                    // ȷ��������
                    first_year.CompleteMonth(bHead);

                    nNewYear = first_year.Year - 1;
                    if (nNewYear < 0)
                        throw new Exception("�굽����С����ֵ");
                }
                else
                {
                    first_year = (YearArea)this.LastChild;

                    // ȷ��������
                    first_year.CompleteMonth(bHead);

                    nNewYear = first_year.Year + 1;
                    if (nNewYear > 9999)
                        throw new Exception("�굽�������ֵ");

                }
            }
            else
            {
                // ���ݵ�ǰʱ������¶�������ֵ
                DateTime now = DateTime.Now;
                nNewYear = now.Year;
            }

            YearArea new_year = new YearArea(this, nNewYear, bEmpty);
            if (bHead == true)
                this.YearCollection.Insert(0, new_year);
            else
                this.YearCollection.Add(new_year);

            this.ClearCache();

            return new_year;
        }

        // ��ʱ�䷶Χ��չһ��
        public MonthArea ExpandMonth(bool bHead,
            bool bEmpty)
        {
            YearArea first_year = null;

            if (this.YearCollection.Count == 0)
            {
                // ������һ���յ���
                first_year = this.ExpandYear(bHead, true);
            }
            else
                first_year = (YearArea)this.EdgeChild(bHead);

            Debug.Assert(first_year != null, "");

            return first_year.ExpandMonth(bHead,
                bEmpty);
        }

        // ��ʱ�䷶Χ��չһ����
        public WeekArea ExpandWeek(bool bHead,
            bool bEmpty)
        {
            YearArea first_year = null;

            if (this.YearCollection.Count == 0)
            {
                // ������һ���յ���
                first_year = this.ExpandYear(bHead, true);
            }
            else
                first_year = (YearArea)this.EdgeChild(bHead);

            Debug.Assert(first_year != null, "");

            MonthArea first_month = null;

            if (first_year.MonthCollection.Count == 0)
            {
                // ������һ���յ���
                first_month = first_year.ExpandMonth(bHead, true);
            }
            else
            {
                first_month = (MonthArea)first_year.EdgeChild(bHead);
            }

            Debug.Assert(first_month != null, "");

            return first_month.ExpandWeek(bHead,
                bEmpty);
        }


        // �ٻ�һ������Ҳ�޷�
        // �������һ�㡰ä���������generic֧�ָ����������ַ������춯̬�ĺ������ͺ���
        public TypedList<YearArea> YearCollection
        {
            get
            {
                return this.ChildTypedCollection;    // ��ʵNamedCollectionҲ�ܺ��ã���������û����ɫ
            }
        }

        // ����
        // parameters:
        //      nStartYear  ��ʼ��
        //      nEndYear    ������(��������)
        public int Build(int nStartYear,
            int nEndYear,
            out string strError)
        {
            strError = "";

            if (nStartYear > nEndYear)
            {
                strError = "��ʼ�겻Ӧ���ڽ�����";
                return -1;
            }

            for (int i = nStartYear; i <= nEndYear; i++)
            {
                YearArea year = new YearArea(this, i);

                this.ChildrenCollection.Add(year);
            }

            return 0;
        }

        #region DataRoot����AreaBase��virtual����

        public override string FullName
        {
            get
            {
                return "DataRoot";
            }
        }


        public override long Height
        {
            get
            {
                if (m_lHeightCache == -1)
                {
                    long lHeight = 0;
                    for (int i = 0; i < this.ChildrenCollection.Count; i++)
                    {
                        lHeight += ((YearArea)this.ChildrenCollection[i]).Height;
                    }

                    m_lHeightCache = lHeight;
                }

                return m_lHeightCache;
            }
        }

        public override long Width
        {
            get
            {
                if (m_lWidthCache == -1)
                    m_lWidthCache = (7 * this.m_nDayCellWidth) + this.m_nMonthNameWidth + this.m_nYearNameWidth;

                return m_lWidthCache;
            }
        }

        // ����Ӷ����� ������������ϵ�е� ���Ͻ�λ��
        public override PointF GetChildLeftTopPoint(AreaBase child)
        {
            if (!(child is YearArea))
                throw new Exception("childֻ��ΪYearArea����");

            YearArea year = (YearArea)child;

            bool bFound = false;
            long lHeight = 0;
            for (int i = 0; i < this.ChildrenCollection.Count; i++)
            {
                YearArea cur_year = (YearArea)this.ChildrenCollection[i];
                if (cur_year == year)
                {
                    bFound = true;
                    break;
                }
                lHeight += cur_year.Height;
            }

            if (bFound == false)
                throw new Exception("child���Ӷ�����û���ҵ�");

            return new PointF(0,
                lHeight);
        }


        #endregion

        // ���һ��long�Ƿ�Խ��int16�ܱ���ֵ��Χ
        public static bool TooLarge(long lValue)
        {
            if (lValue >= Int16.MaxValue || lValue <= Int16.MinValue)
                return true;
            return false;
        }

        // ���ݷ�Χ֮��С��
        public int MinYear
        {
            get
            {
                if (this.ChildrenCollection.Count == 0)
                    return -1;  // ��ʾ��δ��ʼ��
                return ((YearArea)this.ChildrenCollection[0]).Year;
            }
        }

        // ���ݷ�Χ֮�����
        public int MaxYear
        {
            get
            {
                if (this.ChildrenCollection.Count == 0)
                    return -1;  // ��ʾ��δ��ʼ��
                return ((YearArea)this.ChildrenCollection[this.ChildrenCollection.Count - 1]).Year;
            }
        }

        // ������
        // parameters:
        //      p_x   �Ѿ����ĵ����ꡣ���ĵ����Ͻ�Ϊ(0,0)
        public void HitTest(
            long p_x,
            long p_y,
            Type dest_type,
            out HitTestResult result)
        {
            result = null;

            if (dest_type == typeof(DataRoot))
            {
                result = new HitTestResult();
                result.Object = this;
                result.AreaPortion = AreaPortion.Content;
                result.X = p_x;
                result.Y = p_y;
                return;
            }

            // ���������Ҫ���ϲ�Ҳ������һ�����
            if (p_y < 0 && dest_type != null)
            {
                if (this.YearCollection.Count > 1)
                {
                    // ȷ����һ��YearArea������
                    this.YearCollection[0].HitTest(p_x,
                        p_y,
                        dest_type,
                        out result);
                    return;
                }
            }

            long y = 0;
            for (int i = 0; i < this.YearCollection.Count; i++)
            {
                // �Ż�
                if (dest_type == null
                    && y > p_y)
                    break;

                YearArea year = this.YearCollection[i];

                long lYearHeight = year.Height;

                if (p_y >= y && p_y < y + lYearHeight)
                {
                    // ȷ����һ��YearArea������
                    year.HitTest(p_x,
                        p_y - y,
                        dest_type,
                        out result);
                    return;
                }
                y += lYearHeight;
            }

            // ���������Ҫ���²�Ҳ�������һ�����
            if (dest_type != null)
            {
                if (this.YearCollection.Count > 1)
                {
                    // ȷ����һ��YearArea������
                    this.YearCollection[this.YearCollection.Count - 1].HitTest(p_x,
                        p_y - y,
                        dest_type,
                        out result);
                    return;
                }
            }

            // ���û��ƥ�����κ�YearArea����
            result = new HitTestResult();
            result.Object = this;
            result.AreaPortion = AreaPortion.BottomBlank;
            result.X = p_x;
            result.Y = p_y;
        }

        // ��ͼ ������
        public void Paint(
            long x0,
            long y0,
            PaintEventArgs e)
        {
            /*
            if (TooLarge(start_x) == true
                || TooLarge(start_y) == true )
                return;
             */

            /*
            PaintBack(
                x0,
                y0,
                0,
                this.Height,
                e,
                Color.White);
             * */


            long x = x0;
            long y = y0;

            bool bDrawBottomLine = true;    // �Ƿ�Ҫ���·�����

            long lYearWidth = this.Width;

            long lHeight = 0;
            for (int i = 0; i < this.YearCollection.Count; i++)
            {
                YearArea year = this.YearCollection[i];
                long lYearHeight = year.Height;

                if (TooLarge(x) == true
                    || TooLarge(y) == true)
                    goto CONTINUE;

                // �Ż�
                RectangleF rect = new RectangleF((int)x,
                    (int)y,
                    lYearWidth,
                    lYearHeight);

                if (y > e.ClipRectangle.Y + e.ClipRectangle.Height)
                {
                    bDrawBottomLine = false;
                    break;
                }


                if (rect.IntersectsWith(e.ClipRectangle) == false)
                    goto CONTINUE;

                year.Paint((int)x, (int)y, e);

            CONTINUE:
                y += lYearHeight;
                lHeight += lYearHeight;
            }

            // �ҡ�������

            Pen penBold = new Pen(Color.FromArgb(50, Color.Gray), (float)2);

            // �ҷ�����
            if (TooLarge(x0 + this.Width) == false)
            {
                e.Graphics.DrawLine(penBold,
                    new PointF((int)x0 + this.Width, (int)y0),
                    new PointF((int)x0 + this.Width, (int)(y0 + lHeight))
                    );
            }

            // �·�����
            if (bDrawBottomLine == true
                && TooLarge(y0 + lHeight) == false)
            {

                e.Graphics.DrawLine(penBold,
                    new PointF((int)x0, (int)(y0 + lHeight)),
                    new PointF((int)x0 + this.Width, (int)(y0 + lHeight))
                    );
            }

        }

        /*
        // �ҵ�ָ�����ڵ�DayArea����
        public DayArea FindDayArea(int year, int month, int day)
        {
            for (int i = 0; i < this.ChildrenCollection.Count; i++)
            {
                YearArea cur_year = (YearArea)this.ChildrenCollection[i];
                if (cur_year.Year == year)
                    return cur_year.FindDayArea(month, day);

                // �Ż�
                if (year < cur_year.Year)
                    return null;
            }

            return null;
        }
         * */

        // �ҵ�ָ�����ڵ�DayArea����
        public DayArea FindDayArea(int year, int month, int day)
        {
            List<int> values = new List<int>();
            values.Add(-1); // -1 ��ʾ��������һ��, ͨ��
            values.Add(year);
            values.Add(month);
            values.Add(-1); // -1 ��ʾ������һ��, ͨ��
            values.Add(day);

            return (DayArea)this.FindByNameValue(values);
        }

        // ѡ��λ�ھ����ڵĶ���
        public void Select(RectangleF rect,
            SelectAction action,
            List<Type> types,
            ref List<AreaBase> update_objects,
            int nMaxCount)
        {
            RectangleF rectThis = new RectangleF(0, 0, this.Width, this.Height);

            if (rectThis.IntersectsWith(rect) == true)
            {
                /*
                if (types.IndexOf(this.GetType()) != -1)
                {
                    bool bRet = this.Select(action, false);
                    if (bRet == true && update_objects.Count < nMaxCount)
                    {
                        update_objects.Add(this);
                    }
                }*/

                long y = 0;
                for (int i = 0; i < this.YearCollection.Count; i++)
                {
                    YearArea year = this.YearCollection[i];

                    // �Ż�
                    if (y > rect.Bottom)
                        break;

                    // �任Ϊyear������
                    RectangleF rectYear = rect;
                    rectYear.Offset(0, -y);

                    year.Select(rectYear,
                        action,
                        types,
                        ref update_objects,
                        nMaxCount);

                    y += year.Height;
                }
            }

        }
    }

    // ��
    public class YearArea : NamedArea<MonthArea>
    {
        // public List<MonthArea> MonthCollection = new List<MonthArea>();

        // int m_nYear = 0;

        // ȷ��һ���е�ǰ�����ߺ��·�������
        // return:
        //      �Ƿ���������
        public bool CompleteMonth(bool bHead)
        {
            if (this.MonthCollection.Count == 12)
                return false;

            bool bChanged = false;

            while(true)
            {
                MonthArea first_month = null;
                if (this.MonthCollection.Count > 0)
                {
                    if (bHead == true)
                    {
                        first_month = (MonthArea)this.FirstChild;
                        if (first_month.Month <= 1)
                            break;
                    }
                    else
                    {
                        first_month = (MonthArea)this.LastChild;
                        if (first_month.Month >= 12)
                            break;
                    }
                }

                ExpandMonth(bHead, false);
                bChanged = true;
            }

            return bChanged;
        }

        // ��ʱ�䷶Χɾ��һ��
        // return:
        public bool ShrinkMonth(bool bHead)
        {
            int nNewMonth = 0;

                MonthArea first_month = null;
            if (this.MonthCollection.Count > 0)
            {
                // �õ��ִ��һ��

                if (bHead == true)
                {
                    first_month = (MonthArea)this.FirstChild;
                }
                else
                {
                    first_month = (MonthArea)this.LastChild;
                }
            }
            else
            {
                return false;
            }

            this.MonthCollection.Remove(first_month);
            this.ClearCache();
            return true;
        }

        // ��ʱ�䷶Χ��չһ��
        // ע����Ҫ���ԭ�еĵ�һ�»������һ���Ƿ�����
        // return:
        //      ����    �����������·ݶ���
        public MonthArea ExpandMonth(bool bHead, 
            bool bEmpty)
        {
            int nNewMonth = 0;

            if (this.MonthCollection.Count > 0)
            {
                // �õ��ִ��һ��
                MonthArea first_month = null;

                if (bHead == true)
                {
                    first_month = (MonthArea)this.FirstChild;
                    // ȷ��������
                    first_month.CompleteWeek(bHead);
                    nNewMonth = first_month.Month - 1;
                    if (nNewMonth == 0)
                    {
                        // ��Ҫ������
                        return this.Container.ExpandYear(bHead, true).ExpandMonth(bHead, bEmpty);
                    }
                }
                else
                {
                    first_month = (MonthArea)this.LastChild;
                    // ȷ��������
                    first_month.CompleteWeek(bHead);
                    nNewMonth = first_month.Month + 1;
                    if (nNewMonth >= 13)
                    {
                        // ��Ҫ������
                        return this.Container.ExpandYear(bHead, true).ExpandMonth(bHead, bEmpty);
                    }
                }
            }
            else
            {
                if (bHead == true)
                    nNewMonth = 12;
                else
                    nNewMonth = 1;
            }

            MonthArea new_month = new MonthArea(this, nNewMonth, bEmpty);
            if (bHead == true)
                this.MonthCollection.Insert(0, new_month);
            else
                this.MonthCollection.Add(new_month);

            this.ClearCache();

            return new_month;
        }

        // ��չһ������
        public WeekArea ExpandWeek(bool bHead,
            bool bEmpty)
        {
            MonthArea first_month = null;

            if (this.MonthCollection.Count == 0)
            {
                // ������һ���յ���
                first_month = this.ExpandMonth(bHead, true);
            }
            else
            {
                first_month = (MonthArea)this.EdgeChild(bHead);
            }

            Debug.Assert(first_month != null, "");

            return first_month.ExpandWeek(bHead,
                bEmpty);
        }

        public DataRoot Container
        {
            get
            {
                return (DataRoot)this._Container;
            }
        }

        // ���캯��
        // (������װ�汾)
        public YearArea(DataRoot container,
            int nYear)
        {
            InitialYearArea(container, nYear, false);
        }

        // ���캯��
        public YearArea(DataRoot container,
            int nYear,
            bool bEmpty)
        {
            InitialYearArea(container, nYear, bEmpty);
        }

        // ���캯��
        void InitialYearArea(DataRoot container,
            int nYear,
            bool bEmpty)
        {
            this._Container = container;

            this.Year = nYear;

            if (bEmpty == false)
            {
                // ����������
                for (int i = 0; i < 12; i++)
                {
                    MonthArea month = new MonthArea(this, i + 1);
                    this.ChildrenCollection.Add(month);
                }
            }
        }

        // ����
        public TypedList<MonthArea> MonthCollection
        {
            get
            {
                return this.ChildTypedCollection;    // ��ʵNamedCollectionҲ�ܺ��ã���������û����ɫ
            }
        }

        // ѡ��λ�ھ����ڵĶ���
        public void Select(RectangleF rect,
            SelectAction action,
            List<Type> types,
            ref List<AreaBase> update_objects,
            int nMaxCount)
        {
            RectangleF rectThis = new RectangleF(0, 0, this.Width, this.Height);

            if (rectThis.IntersectsWith(rect) == true)
            {

                RectangleF rectName = new RectangleF(0, 0,
                    this.DataRoot.m_nYearNameWidth,
                    this.Height);

                if (rectName.IntersectsWith(rect) == true)
                {
                    if (types.IndexOf(this.GetType()) != -1)
                    {
                        bool bRet = this.Select(action, true);
                        if (bRet == true && update_objects.Count < nMaxCount)
                        {
                            update_objects.Add(this);
                        }
                    }
                }



                long y = 0;
                for (int i = 0; i < this.MonthCollection.Count; i++)
                {
                    MonthArea month = this.MonthCollection[i];

                    // �Ż�
                    if (y > rect.Bottom)
                        break;

                    

                    // �任Ϊmonth������
                    RectangleF rectMonth = rect;
                    rectMonth.Offset(-this.DataRoot.m_nYearNameWidth, -y);

                    month.Select(rectMonth,
                        action,
                        types,
                        ref update_objects,
                        nMaxCount);

                    y += month.Height;
                }
            }

        }

        #region YearArea����AreaBase��virtual����

        public override string FullName
        {
            get
            {
                return this.YearName;
            }
        }


        public override long Height
        {
            get
            {
                if (m_lHeightCache == -1)
                {
                    long lHeight = 0;
                    for (int i = 0; i < this.MonthCollection.Count; i++)
                    {
                        lHeight += this.MonthCollection[i].Height;
                    }
                    m_lHeightCache = lHeight;
                }

                return m_lHeightCache;
            }
        }

        public override long Width
        {
            get
            {
                if (m_lWidthCache == -1)
                    m_lWidthCache = (7 * this.DataRoot.m_nDayCellWidth) + this.DataRoot.m_nMonthNameWidth + this.DataRoot.m_nYearNameWidth;

                return m_lWidthCache;
            }
        }

        /*
        public override bool Select(SelectAction action,
bool bRecursive)
        {
            bool bRet = base.Select(action, bRecursive);

            // �ݹ�
            if (bRecursive == true)
            {
                for (int i = 0; i < this.MonthCollection.Count; i++)
                {
                    MonthArea month = this.MonthCollection[i];
                    month.Select(action, true);
                }
            }

            return bRet;
        }*/

        // ����Ӷ����� ������������ϵ�е� ���Ͻ�λ��
        public override PointF GetChildLeftTopPoint(AreaBase child)
        {
            if (!(child is MonthArea))
                throw new Exception("childֻ��ΪMonthArea����");

            MonthArea month = (MonthArea)child;

            bool bFound = false;
            long lHeight = 0;
            for (int i = 0; i < this.MonthCollection.Count; i++)
            {
                MonthArea cur_month = this.MonthCollection[i];
                if (cur_month == month)
                {
                    bFound = true;
                    break;
                }
                lHeight += cur_month.Height;
            }

            if (bFound == false)
                throw new Exception("child���Ӷ�����û���ҵ�");

            return new PointF(this.DataRoot.m_nYearNameWidth,
                lHeight);
        }


        #endregion

        public DataRoot DataRoot
        {
            get
            {
                return (DataRoot)this._Container;
            }
        }


        // ������
        public void HitTest(
            long p_x,
            long p_y,
            Type dest_type,
            out HitTestResult result)
        {
            result = null;

            if (dest_type == typeof(YearArea))
            {
                result = new HitTestResult();
                result.Object = this;
                result.AreaPortion = AreaPortion.Content;
                result.X = p_x;
                result.Y = p_y;
                return;
            }

            // �����ǲ�������ߵ�������
            // dest_type�����Ҫ������������Ҳ�ø��¼��ж�
            if (dest_type == null
                && p_x < this.DataRoot.m_nYearNameWidth)
            {
                result = new HitTestResult();
                result.AreaPortion = AreaPortion.LeftBar;
                result.Object = this;
                result.X = p_x;
                result.Y = p_y;
                return;
            }

            // ���������Ҫ���ϲ�Ҳ������һ���·ݵ�
            if (p_y < 0 && dest_type != null)
            {
                if (this.MonthCollection.Count > 1)
                {
                    this.MonthCollection[0].HitTest(p_x - this.DataRoot.m_nYearNameWidth,
                        p_y - 0,
                        dest_type,
                        out result);
                    return;
                }
            }

            long y = 0;
            for (int i = 0; i < this.MonthCollection.Count; i++)
            {
                // �Ż�
                if (dest_type == null
                    && y > p_y)
                    break;

                MonthArea month = this.MonthCollection[i];

                long lMonthHeight = month.Height;

                if (p_y >= y && p_y < y + lMonthHeight)
                {
                    // ȷ����һ��MonthArea������
                    month.HitTest(p_x - this.DataRoot.m_nYearNameWidth,
                        p_y - y,
                        dest_type,
                        out result);
                    return;
                }
                y += lMonthHeight;
            }

            // ���������Ҫ���²�Ҳ�������һ���·ݵ�
            if (dest_type != null)
            {
                if (this.MonthCollection.Count > 1)
                {
                    this.MonthCollection[this.MonthCollection.Count - 1].HitTest(p_x - this.DataRoot.m_nYearNameWidth,
                        p_y - y,
                        dest_type,
                        out result);
                    return;
                }
            }

            // ���û��ƥ�����κ�MonthArea����
            result = new HitTestResult();
            result.Object = this;
            result.AreaPortion = AreaPortion.BottomBlank;
            result.X = p_x;
            result.Y = p_y;
        }

        // ��ͼ����
        public void Paint(
            long start_x,
            long start_y,
            PaintEventArgs e)
        {
            if (DataRoot.TooLarge(start_x) == true
                || DataRoot.TooLarge(start_y) == true)
                return;

            /*
            PaintBack(
    start_x,
    start_y,
    // this.Width, // ���ﲻ�Ż���ѡ�е�ʱ��ˢ�·��������� 
    this.DataRoot.m_nYearNameWidth,
    this.Height,
    e,
                Color.White);
             * */

            PaintBack(
                start_x,
                start_y,
                this.Width, // ���ﲻ�Ż���������ǰ��������Ӧ��ѡ��Ч������ôѡ�е�ʱ��ˢ�·��������� 
                this.Height,
                e,
                this.DataRoot.YearBackColor);

            // Ϊ��ֹ���������ӵĴ���
            if (this.m_bSelected == true)
            {
                // ��ǰ��͸�����Ӻ�ı�����ɫģ�����
                Color colorMask = SystemColors.Highlight;
                Color colorBase = Color.White;
                int r = (byte)((float)colorBase.R * ((255F - 20F) / 255F)
                     + (float)colorMask.R * (20F / 255F));
                int g = (byte)((float)colorBase.G * ((255F - 20F) / 255F)
                     + (float)colorMask.G * (20F / 255F));
                int b = (byte)((float)colorBase.B * ((255F - 20F) / 255F)
     + (float)colorMask.B * (20F / 255F));

                this.PaintBack(
                    start_x + this.DataRoot.m_nYearNameWidth,
                    start_y,
                    this.Width - this.DataRoot.m_nYearNameWidth,
                    this.Height,
                    e,
                    Color.FromArgb(r,g,b));
            }


            int x0 = (int)start_x;
            int y0 = (int)start_y;

            RectangleF rect;

            // ����������
            rect = new RectangleF(
x0,
y0,
this.DataRoot.m_nYearNameWidth,
this.Height);

            // �Ż�
            if (rect.IntersectsWith(e.ClipRectangle) == true)
            {
                PaintYearName(
                    x0,
                    y0,
                    this.DataRoot.m_nYearNameWidth,
                    (int)this.Height,
                    e);

                // ѡ����Ч��
                if (this.m_bSelected == true)
                {
                    this.PaintSelectEffect(
                    x0,
                    y0,
                    this.DataRoot.m_nYearNameWidth,
                    (int)this.Height,
        e);
                }
            }

            int x = x0;
            int y = y0;

            // �����·�
            x = x0 + this.DataRoot.m_nYearNameWidth;
            y = y0;
            for (int i = 0; i < this.MonthCollection.Count; i++)
            {
                MonthArea month = this.MonthCollection[i];

                rect = new RectangleF(
    x,
    y,
    month.Width,
    month.Height);

                // ��ǰ����ѭ��
                if (rect.Y > e.ClipRectangle.Bottom)
                    break;

                // �Ż�
                if (rect.IntersectsWith(e.ClipRectangle) == true)
                    month.Paint(x, y, e);

                long lMonthHeight = month.Height;
                y += (int)lMonthHeight;
                // nHeight += (int)lMonthHeight;
            }
        }

        // ��������
        void PaintYearName(
            int x0,
            int y0,
            int nWidth,
            int nHeight,
            PaintEventArgs e)
        {
            Pen penBold = new Pen(Color.FromArgb(50, Color.Gray), (float)2);

            // ������
            e.Graphics.DrawLine(penBold,
                new PointF(x0, y0),
                new PointF(x0, y0 + nHeight)
                );

            // �Ϸ�����
            e.Graphics.DrawLine(penBold,
                new PointF(x0, y0),
                new PointF(x0 + nWidth, y0)
                );

            int nFontHeight = Math.Min(nWidth, nHeight / 5);

            Font font = new Font("Arial", nFontHeight, FontStyle.Bold, GraphicsUnit.Pixel);
            Brush brushText = null;

            brushText = new SolidBrush(Color.Blue);

            RectangleF rect = new RectangleF(
    x0 + nWidth / 4,
    y0,
    nWidth / 2 ,
    nHeight);

            StringFormat stringFormat = new StringFormat();

            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            e.Graphics.DrawString(this.YearName,
                font,
                brushText,
                rect,
                stringFormat);
        }


        // ���ֵ
        public int Year
        {
            get
            {
                return this.NameValue;
                // return this.m_nYear;
            }
            set
            {
                this.NameValue = value;
                // this.m_nYear = value;
            }
        }

        public string YearName
        {
            get
            {
                return this.Year.ToString().PadLeft(4, '0');
            }

        }


        // ����֮��һ�������
        public YearArea NextYearArea
        {
            // ��д
            get
            {
                return (YearArea)this.GetNextSibling();
            }

            /*
            get
            {
                int nIndex = this.Container.YearCollection.IndexOf(this);

                if (nIndex == -1)
                    throw new Exception("��ǰYearArea������������");

                if (nIndex + 1 < this.Container.YearCollection.Count)
                    return this.Container.YearCollection[nIndex + 1];

                return null;
            }
             * */
        }

        public MonthArea FirstMonthArea
        {
            get
            {
                if (this.ChildrenCollection.Count == 0)
                    return null;

                return (MonthArea)this.ChildrenCollection[0];
            }
        }

        /*
        // �����һ�����족����
        public DayArea FirstDayArea
        {
            get
            {
                if (this.MonthCollection.Length == 0)
                    return null;

                return this.MonthCollection[0].FirstDayArea;
            }
        }*/

        /*
        // �ҵ�ָ�����ڵ�DayArea����
        public DayArea FindDayArea(int month, int day)
        {
            if (this.ChildrenCollection.Count == 0)
                return null;

            MonthArea cur_month = null;

            // �Ż�
            if (((MonthArea)this.ChildrenCollection[0]).Month == 1)
            {
                // ˵����һ���·ݶ�����1�£�month�Ϳ��������±�
                if (month - 1 < this.ChildrenCollection.Count)
                    cur_month = (MonthArea)this.ChildrenCollection[month - 1];
                else
                    return null;
            }
            else
            {
                // ����
                for (int i = 0; i < this.ChildrenCollection.Count; i++)
                {
                    cur_month = (MonthArea)this.ChildrenCollection[i];
                    if (cur_month.Month == month)
                        goto FOUND;


                    if (month > cur_month.Month)    // �Ż�
                        return null;
                }

                return null;
            }

            FOUND:
            if (cur_month != null)
                return cur_month.FindDayArea(day);

            return null;
        }
        */

    }

    // ��
    public class MonthArea : NamedArea<WeekArea>
    {
        // public List<WeekArea> WeekCollection = new List<WeekArea>();

        // int m_nMonth = 0;   // ��δ��ʼ��

        // �������������
        public int MaxWeekCount
        {
            get
            {
                int nWeekCount = 0;

                // ��ʼ�� 1��
                DateTime date = new DateTime(this.Year,
                    this.Month,
                    1);

                int nStartIndex = Convert.ToInt32(date.DayOfWeek);   // ��ʼ�յ���š�0Ϊ������
                int nMaxDays = this.Days;
                int nDay = 1;
                bool bBlank = true;
                for (int nCurWeek = 1; nCurWeek <= 6 && nDay <= nMaxDays; nCurWeek++)
                {
                    for (int nDayOfWeek = 0; nDayOfWeek < 7; nDayOfWeek++)
                    {
                        // ��Ϊ���ǿհ�
                        if (nCurWeek == 1 && nDayOfWeek >= nStartIndex)
                            bBlank = false;

                        // ��ؿհ�
                        if (nDay > nMaxDays)
                            bBlank = true;

                        if (bBlank == false)
                            nDay++;
                    }
                    nWeekCount++;
                }

                return nWeekCount;
            }
        }

        // ȷ��һ���е�ǰ�����ߺ�����������
        // return:
        //      �Ƿ���������
        public bool CompleteWeek(bool bHead)
        {
            if (this.WeekCollection.Count == 6)
                return false;

            bool bChanged = false;

            while (true)
            {
                WeekArea first_week = null;
                if (this.WeekCollection.Count > 0)
                {
                    if (bHead == true)
                    {
                        first_week = (WeekArea)this.FirstChild;
                        if (first_week.MinDay <= 1)
                            break;
                    }
                    else
                    {
                        first_week = (WeekArea)this.LastChild;
                        if (first_week.MaxDay >= first_week.Container.Days)
                            break;
                    }
                }

                ExpandWeek(bHead, false);
                bChanged = true;
            }

            return bChanged;
        }

        // ��ʱ�䷶Χɾ��һ����
        // return:
        public bool ShrinkWeek(bool bHead)
        {
                WeekArea first_week = null;
            if (this.WeekCollection.Count > 0)
            {
                // �õ��ִ��һ����
                if (bHead == true)
                {
                    first_week = (WeekArea)this.FirstChild;
                }
                else
                {
                    first_week = (WeekArea)this.LastChild;
                }
            }
            else
            {
                return false;
            }

            this.WeekCollection.Remove(first_week);
            this.ClearCache();

            return true;
        }

        // ��ʱ�䷶Χ��չһ����
        // ע����Ҫ���ԭ�еĵ�һ���ڻ������һ�����Ƿ�����
        // return:
        //      ����    ���������µ����ڶ���
        public WeekArea ExpandWeek(bool bHead,
            bool bEmpty)
        {
            int nNewWeek = 0;
            if (this.WeekCollection.Count > 0)
            {
                // �õ��ִ��һ����
                WeekArea first_week = null;

                if (bHead == true)
                {
                    first_week = (WeekArea)this.FirstChild;
                    nNewWeek = first_week.Week - 1;
                    if (nNewWeek == 0)
                    {
                        // ��Ҫ������
                        return this.Container.ExpandMonth(bHead, true).ExpandWeek(bHead, bEmpty);
                    }
                }
                else
                {
                    first_week = (WeekArea)this.LastChild;
                    nNewWeek = first_week.Week + 1;
                    if (first_week.MaxDay >= first_week.Container.Days)
                    {
                        // ��Ҫ������
                        return this.Container.ExpandMonth(bHead, true).ExpandWeek(bHead, bEmpty);
                    }
                }
            }
            else
            {
                if (bHead == true)
                    nNewWeek = this.MaxWeekCount;  // ĩ����
                else
                    nNewWeek = 1;
            }

            WeekArea new_week = new WeekArea(this, nNewWeek);
            if (bHead == true)
                this.WeekCollection.Insert(0, new_week);
            else
                this.WeekCollection.Add(new_week);

            this.ClearCache();

            return new_week;
        }

        public YearArea Container
        {
            get
            {
                return (YearArea)this._Container;
            }
        }

                // ���캯��
        // parameters:
        //      nMonth  �·�������1��ʼ����
        public MonthArea(YearArea container,
            int nMonth,
            bool bEmpty)
        {
            InitialMonthArea(container, nMonth, bEmpty);
        }

        // ���캯��
        // parameters:
        //      nMonth  �·�������1��ʼ����
        public MonthArea(YearArea container,
            int nMonth)
        {
            InitialMonthArea(container, nMonth, false);
        }

        // ���캯��ʵ�ʹ���
        // parameters:
        //      nMonth  �·�������1��ʼ����
        void InitialMonthArea(YearArea container,
            int nMonth,
            bool bEmpty)
        {
            this._Container = container;

            this.Month = nMonth;

            if (bEmpty == true)
                return;

            // �����������������������
            int nDays = this.Days;

            WeekArea week = null;
            int nWeek = 0;
            for (int i = 0; i < nDays; i++)
            {
                // this.Days���Ե�֪�����ж�����
                // DataTime.DayOfWeek��������̽�Ȿ��һ��Ϊ���ڼ�������һֱ������ȥ
                // �Ϳ��Ե�֪�ж��ٸ����ڡ�
                // ע�⣬DayOfWeek��0��ʾ�����졣A DayOfWeek enumerated constant that indicates the day of the week. This property value ranges from zero, indicating Sunday, to six, indicating Saturday. 
                // ���⽨������Ĺ��̣�Ҳ��ͬʱ����WeekArea��DayArea����Ĺ��̣������ٷ�����ȥ������
                DateTime date = new DateTime(this.Container.Year,
                    this.Month,
                    i + 1);

                // ���ϱ��µ�һ�죬����ÿ�ܵ�һ�죨�����죩���Ŵ�Ϊ�������ڶ���
                if (i == 0
                    || date.DayOfWeek == 0)
                {
                    week = new WeekArea(this, i + 1, ++nWeek);
                    this.ChildrenCollection.Add(week);
                }
            }

        }

        // ����
        public TypedList<WeekArea> WeekCollection
        {
            get
            {
                return this.ChildTypedCollection;    // ��ʵNamedCollectionҲ�ܺ��ã���������û����ɫ
            }
        }

        // ѡ��λ�ھ����ڵĶ���
        public void Select(RectangleF rect,
            SelectAction action,
            List<Type> types,
            ref List<AreaBase> update_objects,
            int nMaxCount)
        {
            RectangleF rectThis = new RectangleF(0, 0, this.Width, this.Height);

            if (rectThis.IntersectsWith(rect) == true)
            {
                RectangleF rectName = new RectangleF(0, 0,
    this.DataRoot.m_nMonthNameWidth,
    this.Height);

                if (rectName.IntersectsWith(rect) == true)
                {
                    if (types.IndexOf(this.GetType()) != -1)
                    {
                        bool bRet = this.Select(action, true);
                        if (bRet == true && update_objects.Count < nMaxCount)
                        {
                            update_objects.Add(this);
                        }
                    }
                }

                long y = this.DataRoot.m_nDayOfWeekTitleHeight;
                for (int i = 0; i < this.WeekCollection.Count; i++)
                {
                    WeekArea week = this.WeekCollection[i];

                    // �Ż�
                    if (y > rect.Bottom)
                        break;

                    // �任Ϊweek������
                    RectangleF rectWeek = rect;
                    rectWeek.Offset(-this.DataRoot.m_nMonthNameWidth, -y);

                    week.Select(rectWeek,
                        action,
                        types,
                        ref update_objects,
                        nMaxCount);

                    y += week.Height;
                }
            }

        }

        #region MonthArea����AreaBase��virtual����

        public override string FullName
        {
            get
            {
                return this.Year.ToString().PadLeft(4, '0') + "/" + this.MonthName;
            }
        }

        /*
        public override AreaBase[] Children
        {
            get
            {
                AreaBase[] children = new AreaBase[this.WeekCollection.Count];
                for (int i = 0; i < children.Length; i++)
                {
                    children[i] = (AreaBase)this.WeekCollection[i];
                }
                return children;
            }
        }*/

        public override long Height
        {
            get
            {
                if (m_lHeightCache == -1)
                {
                    long lHeight = this.DataRoot.m_nDayOfWeekTitleHeight; // ����߶�

                    for (int i = 0; i < this.WeekCollection.Count; i++)
                    {
                        lHeight += this.WeekCollection[i].Height;
                    }
                    m_lHeightCache = lHeight;
                }

                return m_lHeightCache;
            }
        }

        public override long Width
        {
            get
            {
                if (m_lWidthCache == -1)
                {
                    m_lWidthCache = this.DataRoot.m_nMonthNameWidth + 7 * this.DataRoot.m_nDayCellWidth;
                }

                return m_lWidthCache;
            }
        }

        /*
        public override bool Select(SelectAction action,
bool bRecursive)
        {
            bool bRet = base.Select(action, bRecursive);

            // �ݹ�
            if (bRecursive == true)
            {
                for (int i = 0; i < this.WeekCollection.Count; i++)
                {
                    WeekArea week = this.WeekCollection[i];
                    week.Select(action, true);
                }
            }

            return bRet;
        }
         * */

        // ����Ӷ����� ������������ϵ�е� ���Ͻ�λ��
        public override PointF GetChildLeftTopPoint(AreaBase child)
        {
            if (!(child is WeekArea))
                throw new Exception("childֻ��ΪWeekArea����");

            WeekArea week = (WeekArea)child;
            int index = this.ChildrenCollection.IndexOf(week);

            if (index == -1)
                throw new Exception("child���Ӷ�����û���ҵ�");

            return new PointF(this.DataRoot.m_nMonthNameWidth,
                this.DataRoot.m_nDayOfWeekTitleHeight + index * week.Height);
        }

        #endregion

        public DataRoot DataRoot
        {
            get
            {
                return this.Container.Container;
            }
        }

        // ������
        public void HitTest(
            long p_x,
            long p_y,
            Type dest_type,
            out HitTestResult result)
        {
            result = null;

            if (dest_type == typeof(MonthArea))
            {
                result = new HitTestResult();
                result.Object = this;
                result.AreaPortion = AreaPortion.Content;
                result.X = p_x;
                result.Y = p_y;
                return;
            }

            // �����ǲ�������ߵ�����(����)��
            // dest_type�����Ҫ������������Ҳ�ø��¼��ж�
            if (dest_type == null
                && p_x < this.DataRoot.m_nMonthNameWidth)
            {
                result = new HitTestResult();
                result.AreaPortion = AreaPortion.LeftBar;
                result.Object = this;
                result.X = p_x;
                result.Y = p_y;
                return;
            }

            // �����ǲ��������ڱ�����
            // dest_type�����Ҫ����ѱ���Ҳ�ø��¼��ж�
            if (dest_type == null
                && p_y < this.DataRoot.m_nDayOfWeekTitleHeight)
            {
                result = new HitTestResult();
                result.AreaPortion = AreaPortion.ColumnTitle;
                result.Object = this;
                result.X = p_x; // ע��x���껹������m_nMonthNameWidth����
                result.Y = p_y;
                return;
            }

            // ���������Ҫ���ϲ����ܱ���Ҳ������һ�����ڵ�
            if (p_y < this.DataRoot.m_nDayOfWeekTitleHeight
                && dest_type != null)
            {
                if (this.WeekCollection.Count > 1)
                {
                    this.WeekCollection[0].HitTest(p_x - this.DataRoot.m_nMonthNameWidth,
                        p_y - this.DataRoot.m_nDayOfWeekTitleHeight,
                        dest_type,
                        out result);
                    return;
                }
            }

            // �����ǲ�������������
            long y = this.DataRoot.m_nDayOfWeekTitleHeight;
            for (int i = 0; i < this.WeekCollection.Count; i++)
            {
                // �Ż�
                if (dest_type == null
                    && y > p_y)
                    break;

                WeekArea week = this.WeekCollection[i];

                long lWeekHeight = week.Height;

                if (p_y >= y && p_y < y + lWeekHeight)
                {
                    // ȷ����һ��WeekArea������
                    week.HitTest(p_x - this.DataRoot.m_nMonthNameWidth,
                        p_y - y,
                        dest_type,
                        out result);
                    return;
                }
                y += lWeekHeight;
            }

            // ���������Ҫ���²�Ҳ�������һ�����ڵ�
            if (dest_type != null)
            {
                if (this.WeekCollection.Count > 1)
                {
                    this.WeekCollection[this.WeekCollection.Count - 1].HitTest(p_x - this.DataRoot.m_nMonthNameWidth,
                        p_y - y,
                        dest_type,
                        out result);
                    return;
                }
            }

            // ���û��ƥ�����κ�WeekArea����
            result = new HitTestResult();
            result.Object = this;
            result.AreaPortion = AreaPortion.BottomBlank;
            result.X = p_x;
            result.Y = p_y;
        }

        // �����ܱ���
        void PaintDayOfWeekTitle(long x0,
            long y0,
            PaintEventArgs e)
        {
            int nTitleHeight = this.DataRoot.m_nDayOfWeekTitleHeight;
            int nTitleCellWidth = this.DataRoot.m_nDayCellWidth;

            /*
            PaintBack(
x0,
y0,
nTitleCellWidth * 7,
nTitleHeight,
e,
Color.White);
             * */

            // �������ڱ���
            Font font = this.DataRoot.DaysOfWeekTitleFont;
            Brush brushText = null;

            Pen pen = new Pen(Color.FromArgb(50, Color.Gray));
            Pen penBold = new Pen(Color.FromArgb(50, Color.Gray), (float)2);

            brushText = new SolidBrush(Color.Gray);
            long x = x0;
            long y = y0;

            // ����
            {
                float upper_height = ((float)nTitleHeight / 2) + 1;
                float lower_height = ((float)nTitleHeight / 2);

                LinearGradientBrush linGrBrush = new LinearGradientBrush(
new PointF(0, y),
new PointF(0, y + upper_height),
Color.FromArgb(200, 251, 252, 253), 
Color.FromArgb(255, 235, 238, 242)
); 

                linGrBrush.GammaCorrection = true;

                RectangleF rectBack = new RectangleF(
    x,
    y,
    nTitleCellWidth * 7,
    upper_height);
                e.Graphics.FillRectangle(linGrBrush, rectBack);

                //

                linGrBrush = new LinearGradientBrush(
new PointF(0, y + upper_height),
new PointF(0, y + upper_height + lower_height),
Color.FromArgb(255, 220, 226, 231),
Color.FromArgb(255, 215, 222, 228)
);
                rectBack = new RectangleF(
    x,
    y + upper_height,
    nTitleCellWidth * 7,
    lower_height-1);
                e.Graphics.FillRectangle(linGrBrush, rectBack);


            }

            Pen penVert = new Pen(Color.FromArgb(200, Color.White), (float)1.5);


            for (int i = 0; i < 7; i++)
            {
                RectangleF rectUpdate = new RectangleF(
                    x,
                    y,
                    nTitleCellWidth,
                    nTitleHeight);

                // �Ż�
                if (rectUpdate.IntersectsWith(e.ClipRectangle) == false)
                    goto CONTINUE;

                // ������
                e.Graphics.DrawLine(penVert,
                    new PointF(x, y+1),
                    new PointF(x, y+1 + nTitleHeight)
                    );

                // �Ϸ�����
                e.Graphics.DrawLine(penBold,
                    new PointF(x, y),
                    new PointF(x + nTitleCellWidth, y)
                    );


                // ����
                RectangleF rect = new RectangleF(
                    x,
                    y,
                    nTitleCellWidth,
                    nTitleHeight);

                StringFormat stringFormat = new StringFormat();

                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                string strText = "";
                if (this.DataRoot.m_strDayOfWeekTitleLang == "zh")
                    strText = WeekArea.WeekDayNames_ZH[i];
                if (this.DataRoot.m_strDayOfWeekTitleLang == "en")
                    strText = WeekArea.WeekDayNames_EN[i];

                e.Graphics.DrawString(strText,
                    font,
                    brushText,
                    rect,
                    stringFormat);

            CONTINUE:
                x += nTitleCellWidth;
            }

        }

        // ��ͼ ��
        public void Paint(
            long start_x,
            long start_y,
            PaintEventArgs e)
        {
            if (DataRoot.TooLarge(start_x) == true
    || DataRoot.TooLarge(start_y) == true)
                return;


            // ���Ʊ���
            PaintBack(
    start_x,
    start_y,
    this.Width, // this.DataRoot.m_nMonthNameWidth,
    this.Height,
    e,
                this.DataRoot.MonthBackColor);

            int x0 = (int)start_x;
            int y0 = (int)start_y;

            RectangleF rectUpdate;


            // ����������
            rectUpdate = new RectangleF(
x0,
y0,
this.DataRoot.m_nMonthNameWidth,
this.Height);

            // �Ż�
            if (rectUpdate.IntersectsWith(e.ClipRectangle) == true)
            {
                PaintLeftMonthName(
       x0,
       y0,
       this.DataRoot.m_nMonthNameWidth,
       (int)this.Height,
       e);

                // ѡ����Ч��
                if (this.m_bSelected == true)
                {
                    // ����
                    this.PaintSelectEffect(
                        x0,
                        y0,
                        this.DataRoot.m_nMonthNameWidth,
                        (int)this.Height,
                        e);
                }

            }



            int x = x0 + this.DataRoot.m_nMonthNameWidth;
            int y = y0;


            // �������ڱ��� 
            rectUpdate = new RectangleF(
                x,
                y,
                this.DataRoot.m_nDayCellWidth * 7,  // this.Width - this.DataRoot.m_nMonthNameWidth,
                this.DataRoot.m_nDayOfWeekTitleHeight);

            // �Ż�
            if (rectUpdate.IntersectsWith(e.ClipRectangle) == true)
            {
                PaintDayOfWeekTitle(x,
                y,
                e);

                // ѡ����Ч��
                if (this.m_bSelected == true)
                {
                    // ���ڱ���
                    this.PaintSelectEffect(
                    x0 + this.DataRoot.m_nMonthNameWidth,
                    y0,
                    this.DataRoot.m_nDayCellWidth * 7, // (int)(this.Width - this.DataRoot.m_nMonthNameWidth),
                    this.DataRoot.m_nDayOfWeekTitleHeight,
                    e);

                }
            }

            // ���Ʊ�����������
            this.PaintBackMonthName(
               x0 + this.DataRoot.m_nMonthNameWidth,
               y0 + this.DataRoot.m_nDayOfWeekTitleHeight,
               this.Width - this.DataRoot.m_nMonthNameWidth,
               this.Height - this.DataRoot.m_nDayOfWeekTitleHeight,
               e);

            // �����¼�����

            x = x0 + this.DataRoot.m_nMonthNameWidth;
            y += this.DataRoot.m_nDayOfWeekTitleHeight;


            // ����ÿ������
            for (int i = 0; i < this.WeekCollection.Count; i++)
            {
                WeekArea week = this.WeekCollection[i];

                rectUpdate = new RectangleF(
    x,
    y,
    week.Width,
    week.Height);

                // ��ǰ����ѭ��
                if (rectUpdate.Y > e.ClipRectangle.Bottom)
                    break;

                // �Ż�
                if (rectUpdate.IntersectsWith(e.ClipRectangle) == true) 
                    week.Paint(x, y, e);

                long lWeekHeight = week.Height;
                y += (int)lWeekHeight;

            }


        }

        // ����������ϵ�������
        void PaintLeftMonthName(
            int x0,
            int y0,
            int nWidth,
            int nHeight,
            PaintEventArgs e)
        {
            Pen pen = new Pen(Color.FromArgb(50, Color.Gray));
            Pen penBold = new Pen(Color.FromArgb(50, Color.Gray), (float)2);

            // ������
            e.Graphics.DrawLine(pen,
                new PointF(x0, y0),
                new PointF(x0, y0 + nHeight)
                );

            // �Ϸ�����
            e.Graphics.DrawLine(penBold,
                new PointF(x0, y0),
                new PointF(x0 + nWidth, y0)
                );


            Font font = null;
            Brush brushText = null;

            // ����С��������

            int x = x0;
            int y = y0;

            RectangleF rect;

            {
                font = new Font("Arial Black",
                    this.DataRoot.m_nMonthNameWidth/4,
                    FontStyle.Regular, 
                    GraphicsUnit.Pixel);
                brushText = new SolidBrush(Color.Blue);

                rect = new RectangleF(
    x,
    y,
    nWidth,
    this.DataRoot.m_nDayOfWeekTitleHeight);

                StringFormat stringFormat = new StringFormat();

                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Far;

                e.Graphics.DrawString(this.Container.YearName,
                    font,
                    brushText,
                    rect,
                    stringFormat);

            }


            // ����������
            {
                font = new Font("Arial", 20, FontStyle.Bold);
                brushText = new SolidBrush(Color.Green);

                rect = new RectangleF(
                    x0,
                    y0 + this.DataRoot.m_nDayOfWeekTitleHeight,
                    nWidth,
                    nHeight - this.DataRoot.m_nDayOfWeekTitleHeight);

                StringFormat stringFormat = new StringFormat();

                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawString(this.MonthName,
                    font,
                    brushText,
                    rect,
                    stringFormat);
            }

        }

        // �汳���ϵĵ�ɫ������(��������)
        void PaintBackMonthName(
            long x0,
            long y0,
            long lWidth,
            long lHeight,
            PaintEventArgs e)
        {
            RectangleF rectUpdate = new RectangleF(
                x0,
                y0,
                lWidth,
                lHeight);

            // �Ż�
            if (rectUpdate.IntersectsWith(e.ClipRectangle) == false)
                return;

            // ����������
            RectangleF rect;
            long lHalfHeight = lHeight / 2;
            long lRegionHeight = Math.Min(lHalfHeight, this.DataRoot.m_nDayCellHeight * 2);
            Font font = null;
            Brush brushText = null;


                long lYearNameHeight = lRegionHeight/2;
            long lYDelta = lHalfHeight - lYearNameHeight;// �������Ϸ�Ԥ���Ŀհ�
            {
                font = new Font("Arial Black", lYearNameHeight, FontStyle.Regular, GraphicsUnit.Pixel);
                brushText = new SolidBrush(Color.FromArgb(80, Color.LightGray));

                rect = new RectangleF(
    x0,
    y0 + lYDelta - (lYDelta / 2),
    lWidth,
    lYearNameHeight);

                StringFormat stringFormat = new StringFormat();

                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawString(this.Container.YearName,
                    font,
                    brushText,
                    rect,
                    stringFormat);

            }


            // ����������
            {
                font = new Font("Arial Black", lRegionHeight, FontStyle.Regular, GraphicsUnit.Pixel);
                brushText = new SolidBrush(Color.FromArgb(100, Color.LightGray));

                rect = new RectangleF(
    x0,
    y0 + lHalfHeight - (lYDelta / 2),
    lWidth,
    Math.Min(lRegionHeight,lHalfHeight));

                StringFormat stringFormat = new StringFormat();

                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                /*
                Brush brushTest = new SolidBrush(Color.Yellow);
                e.Graphics.FillRectangle(brushTest, rect);
                 * */

                e.Graphics.DrawString(this.MonthName,
                    font,
                    brushText,
                    rect,
                    stringFormat);
            }

        }

        // ��1��ʼ����
        public int Month
        {
            get
            {
                return this.NameValue;
            }
            set
            {
                this.NameValue = value;
            }
        }

        public string MonthName
        {
            get
            {
                return this.Month.ToString();
            }
        }

        public int Year
        {
            get
            {
                return this.Container.Year;
            }
        }



        // ���¹��ж�����
        public int Days
        {
            get
            {
                if (this.Month == 0)
                    throw new Exception("Ҫʹ��Days����, Month���Ա����ȱ���ʼ��");

                return DaysInOneMonth(Container.Year, this.Month);
            }
        }


        static int[] month_array = new int[] {
31, // 1
28, // 2
31, // 3
30, // 4
31, // 5
30, // 6
31, // 7
31, // 8
30, // 9
31, // 10
30, // 11
31, // 12

};

        // parameters:
        //      month   ��1��ʼ����
        public static int DaysInOneMonth(int year, int month)
        {
            if (month < 1 || month > 12)
                return -1;  // ����

            if (month != 2)
                return month_array[month - 1];

            // �������
            if ((year % 100) == 0)
            {
                if ((year % 400) == 0)
                    return 29;
            }
            else
            {
                if ((year % 4) == 0)
                    return 29;
            }
            return 28;
        }

        /*
        public WeekArea FirstWeekArea
        {
            get
            {
                return(WeekArea) this.FirstChild;
            }
        }*/

    }

    // ����
    public class WeekArea : NamedArea<DayArea>
    {
        // public List<DayArea> DayCollection = new List<DayArea>();

        // int m_nWeek = 0;    // ��ǰ�����Ǳ��µڼ������ڣ���1��ʼ���� 0��ʾ��δ��ʼ��

        int m_nMinDay = -1; // �������ڵ���С��ֵ
        int m_nMaxDay = -1; // �������ڵ������ֵ

        public static string[] WeekDayNames_ZH = new string[]
        {
            "������",
            "����һ",
            "���ڶ�",
            "������",
            "������",
            "������",
            "������",
        };

        public static string[] WeekDayNames_EN = new string[]
        {
            "SUN",
            "MON",
            "TUE",
            "WED",
            "THU",
            "FRI",
            "SAT",
        };

        public MonthArea Container
        {
            get
            {
                return (MonthArea)this._Container;
            }
        }



        // ���캯��
        // ֻ�ṩ�ܱ�ţ�������������������
        // parameters:
        public WeekArea(MonthArea container,
            int nWeek)
        {
            this._Container = container;

            this.NameValue = nWeek;

            // ��ʼ�� 1��
            DateTime date = new DateTime(this.Container.Container.Year,
                this.Container.Month,
                1);

            int nStartIndex = Convert.ToInt32(date.DayOfWeek);   // ��ʼ�յ���š�0Ϊ������
            int nMaxDays = this.Container.Days;
            int nDay = 1;
            bool bBlank = true;
            for (int nCurWeek = 1; nCurWeek <= nWeek && nDay <= nMaxDays; nCurWeek++)
            {
                for (int nDayOfWeek = 0; nDayOfWeek < 7; nDayOfWeek++)
                {
                    // ��Ϊ���ǿհ�
                    if (nCurWeek == 1 && nDayOfWeek >= nStartIndex)
                        bBlank = false;

                    // ��ؿհ�
                    if (nDay > nMaxDays)
                        bBlank = true;

                    // ֻ�е�ָ����ŵ����ڣ��ſ�ʼ����
                    if (nCurWeek == nWeek)
                    {
                        DayArea day = null;
                        // �����ո���
                        if (bBlank == true)
                            day = new DayArea(this, 0, nCurWeek);
                        else
                        {
                            day = new DayArea(this, nDay, nCurWeek);
                            if (this.m_nMinDay == -1)
                                this.m_nMinDay = nDay;

                            this.m_nMaxDay = nDay; // ���ϱ�ˢ��
                        }

                        this.ChildrenCollection.Add(day);
                    }

                    if (bBlank == false)
                        nDay++;
                }
            }

        }

        // ���캯��
        // ��Ҫ�ṩ��ʼ���ں����ڱ��
        // parameters:
        //      nStartDay   ��ʼ����
        public WeekArea(MonthArea container,
            int nStartDay,
            int nWeek)
        {
            this._Container = container;

            this.NameValue = nWeek;
            // this.m_nWeek = nWeek;

            // �۲���ʼ��
            DateTime date = new DateTime(this.Container.Container.Year,
                this.Container.Month,
                nStartDay);

            int nStartIndex = Convert.ToInt32(date.DayOfWeek);   // ��ʼ�յ���š�0Ϊ������
            int nMaxDays = this.Container.Days;
            for (int i = 0; i < 7; i++)
            {
                DayArea day = null;
                if (i < nStartIndex || nStartDay > nMaxDays)
                {
                    day = new DayArea(this, 0, i);
                }
                else
                {
                    if (i == nStartIndex)
                        this.m_nMinDay = nStartDay;

                    this.m_nMaxDay = nStartDay; // ���ϱ�ˢ��

                    day = new DayArea(this, nStartDay++, i);
                }

                this.ChildrenCollection.Add(day);
            }
        }

        // ����
        public TypedList<DayArea> DayCollection
        {
            get
            {
                return this.ChildTypedCollection;    // ��ʵNamedCollectionҲ�ܺ��ã���������û����ɫ
            }
        }

        // ѡ��λ�ھ����ڵĶ���
        public void Select(RectangleF rect,
            SelectAction action,
            List<Type> types,
            ref List<AreaBase> update_objects,
            int nMaxCount)
        {
            RectangleF rectThis = new RectangleF(0, 0, this.Width, this.Height);

            if (rectThis.IntersectsWith(rect) == true)
            {
                /*
                if (types.IndexOf(this.GetType()) != -1)
                {
                    bool bRet = this.Select(action, false);
                    if (bRet == true && update_objects.Count < nMaxCount)
                    {
                        update_objects.Add(this);
                    }
                }*/

                long x = 0;
                for (int i = 0; i < this.DayCollection.Count; i++)
                {
                    DayArea day = this.DayCollection[i];

                    // �Ż�
                    if (x > rect.Right)
                        break;

                    // �任Ϊday������
                    RectangleF rectDay = rect;
                    rectDay.Offset(-x, 0);

                    day.Select(rectDay,
                        action,
                        types,
                        ref update_objects,
                        nMaxCount);

                    x += day.Width;
                }
            }

        }

        #region WeekArea����AreaBase��virtual����

        public override string FullName
        {
            get
            {
                return this.Year.ToString().PadLeft(4, '0') + "/" + this.Month.ToString() + " �� " + this.Week.ToString() + " ��";
            }
        }

        /*
        public override AreaBase[] Children
        {
            get
            {
                AreaBase[] children = new AreaBase[this.DayCollection.Count];
                for (int i = 0; i < children.Length; i++)
                {
                    children[i] = (AreaBase)this.DayCollection[i];
                }
                return children;
            }
        }*/

        public override long Height
        {
            get
            {
                // ����Ҫ����
                return this.DataRoot.m_nDayCellHeight;
                /*
                if (m_lHeightCache == -1)
                    m_lHeightCache = this.DataRoot.m_nDayCellHeight;

                return m_lHeightCache;
                 */
            }
        }

        public override long Width
        {
            get
            {
                // ����Ҫ����
                return this.DataRoot.m_nDayCellWidth * 7;
                /*
                if (m_lWidthCache == -1)
                    m_lWidthCache = this.DataRoot.m_nDayCellWidth * 7;

                return m_lWidthCache;
                 * */
            }
        }

        /*
        public override bool Select(SelectAction action,
bool bRecursive)
        {
            bool bRet = base.Select(action, bRecursive);

            // �ݹ�
            if (bRecursive == true)
            {
                for (int i = 0; i < this.DayCollection.Count; i++)
                {
                    DayArea day = this.DayCollection[i];
                    day.Select(action, true);
                }
            }

            return bRet;
        }*/

        // ����Ӷ����� ������������ϵ�е� ���Ͻ�λ��
        public override PointF GetChildLeftTopPoint(AreaBase child)
        {
            if (!(child is DayArea))
                throw new Exception("childֻ��ΪDayArea����");

            DayArea day = (DayArea)child;
            int index = this.ChildrenCollection.IndexOf(day);

            if (index == -1)
                throw new Exception("child���Ӷ�����û���ҵ�");

            return new PointF(index * day.Width, 0);
        }

        #endregion

        DataRoot m_cacheDataRoot = null;

        public DataRoot DataRoot
        {
            get
            {
                // ����
                if (m_cacheDataRoot == null)
                    m_cacheDataRoot = this.Container.Container.Container;

                return m_cacheDataRoot;
            }
        }

        // ������
        public void HitTest(
            long p_x,
            long p_y,
            Type dest_type,
            out HitTestResult result)
        {
            result = null;

            if (dest_type == typeof(WeekArea))
            {
                result = new HitTestResult();
                result.Object = this;
                result.AreaPortion = AreaPortion.Content;
                result.X = p_x;
                result.Y = p_y;
                return;
            }

            // ���������Ҫ����Ҳ������һ�յ�
            if (p_x < 0 && dest_type != null)
            {
                if (this.DayCollection.Count > 1)
                {
                    this.DayCollection[0].HitTest(p_x,
                        p_y,
                        dest_type,
                        out result);
                    return;
                }
            }

            long x = 0;
            int nDayWidth = -1; // -1 ��ʾ��δ��ʼ��
            for (int i = 0; i < this.DayCollection.Count; i++)
            {
                // �Ż�
                if (dest_type == null
                    && x > p_x)
                    break;

                DayArea day = this.DayCollection[i];

                // ����ٶ�
                if (nDayWidth == -1)
                    nDayWidth = (int)day.Width;

                if (p_x >= x && p_x < x + nDayWidth)
                {
                    // ȷ����һ��DayArea������
                    day.HitTest(p_x - x,
                        p_y,
                        dest_type,
                        out result);
                    return;
                }
                x += nDayWidth;
            }

            // ���������Ҫ���Ҳ�Ҳ�������һ�յ�
            if (dest_type != null)
            {
                if (this.DayCollection.Count > 1)
                {
                    this.DayCollection[this.DayCollection.Count - 1].HitTest(p_x - x,
                        p_y,
                        dest_type,
                        out result);
                    return;
                }
            }

            // û��ƥ�����κ�DayArea����
            result = new HitTestResult();
            result.Object = this;
            result.AreaPortion = AreaPortion.RightBlank;
            result.X = p_x;
            result.Y = p_y;
        }


        // ��ͼ ����
        public void Paint(
            long start_x,
            long start_y,
            PaintEventArgs e)
        {
            if (DataRoot.TooLarge(start_x) == true
                || DataRoot.TooLarge(start_y) == true)
                return;

            /*
            PaintBack(
start_x,
start_y,
0,
0,
e,
                Color.White);
             * */

            int x0 = (int)start_x;
            int y0 = (int)start_y;


            int x = x0;
            int y = y0;
            for (int i = 0; i < this.DayCollection.Count; i++)
            {
                DayArea day = this.DayCollection[i];

                RectangleF rectUpdate = new RectangleF(
    x,
    y,
    day.Width,
    day.Height);

                // ��ǰ�˳�ѭ��
                if (x > rectUpdate.Right)
                    break;

                // �Ż�
                if (rectUpdate.IntersectsWith(e.ClipRectangle) == true)
                    day.Paint(x, y, e);


                x += (int)day.Width;
            }
        }


        // �������ڵ���С��ֵ����1��ʼ����
        public int MinDay
        {
            get
            {
                return m_nMinDay;
            }
        }

        // �������ڵ������ֵ����1��ʼ����
        public int MaxDay
        {
            get
            {
                return m_nMaxDay;
            }
        }

        public int Week
        {
            get
            {
                return this.NameValue;
                // return m_nWeek;
            }
        }

        public int Month
        {
            get
            {
                return this.Container.Month;
            }
        }

        public int Year
        {
            get
            {
                return this.Container.Container.Year;
            }
        }


        /*

        // ��һ�����ڡ�ע�⣬���Կ�Խ����
        public WeekArea NextWeekArea
        {
            get
            {
                if (this.Week == 0)
                    throw new Exception("WeekArea�����Week������δ��ʼ��");

                if (this.Week < this.Container.WeekCollection.Length)
                    return this.Container.WeekCollection[this.Week];

                MonthArea next_month = this.Container.NextMonthArea;
                if (next_month == null)
                    return null;

                return next_month.FirstWeekArea;
            }
        }
         * */

        /*
        // ������һ�����ڵĵ�һ���ǿհ���
        public DayArea NextWeekFirstDay()
        {
            WeekArea next_week = null;

            // �������������һ����
            if (this.Week < this.Container.WeekCollection.Count)
            {
                next_week = this.Container.WeekCollection[this.Week];
                return next_week.FirstNonBlankDay;
            }

            return this.Container.NextMonthFirstDay();
        }*/

        public DayArea FistNonBlankDayArea
        {
            get
            {
                for (int i = 0; i < this.ChildrenCollection.Count; i++)
                {
                    DayArea day = (DayArea)this.ChildrenCollection[i];
                    if (day.Blank == false)
                        return day;
                }

                return null;    // û���ҵ�
            }
        }
    }


    // ��
    public class DayArea : AreaBase
    {
        // bool m_bChecked = false;
        public bool m_bHover = false;

        // int m_nDay = 0; // �գ���1��ʼ���������Ϊ0����ʾ�ø���δʹ��

        int m_nDayOfWeek = -1;  // -1��ʾ��δ��ʼ��

        // DayState m_daystate = DayState.WorkDay;
        int m_nDayState = -1;   // -1��ʾ��δ��ʼ��

        /*
        int m_nCacheHeight = -1;
        int m_nCacheWidth = -1;
         */
        DataRoot m_cacheDataRoot = null;    // ��߷���DataRoot���ٶ�

        public WeekArea Container
        {
            get
            {
                return (WeekArea)this._Container;
            }
        }

        // ѡ��λ�ھ����ڵĶ���
        public void Select(RectangleF rect,
            SelectAction action,
            List<Type> types,
            ref List<AreaBase> update_objects,
            int nMaxCount)
        {
            RectangleF rectThis = new RectangleF(0, 0, this.Width, this.Height);

            if (rectThis.IntersectsWith(rect) == true)
            {
                if (types.IndexOf(this.GetType()) != -1)
                {
                    bool bRet = this.Select(action, false);
                    if (bRet == true && update_objects.Count < nMaxCount)
                    {
                        update_objects.Add(this);
                    }
                }
            }
        }

        #region DayArea����AreaBase��virtual����

        public override string FullName
        {
            get
            {
                return this.Year.ToString().PadLeft(4, '0') + "/" + this.Month.ToString() + "/" + this.DayName;
            }
        }

        public override long Height
        {
            get
            {
                // û�л���
                return this.DataRoot.m_nDayCellHeight;
            }
        }

        public override long Width
        {
            get
            {
                // û�л���
                return this.DataRoot.m_nDayCellWidth;
            }
        }

        // Select()����Ҫ����

        #endregion

        public DataRoot DataRoot
        {
            get
            {
                if (m_cacheDataRoot == null)
                    m_cacheDataRoot = this.Container.Container.Container.Container;

                return m_cacheDataRoot;
            }
        }



        // ������
        public void HitTest(
            long p_x,
            long p_y,
            Type dest_type,
            out HitTestResult result)
        {
            /*
            if (dest_type == typeof(DayArea))
            {
                result = new HitTestResult();
                result.Object = this;
                result.AreaPortion = AreaPortion.Content;
                result.X = p_x;
                result.Y = p_y;
                return;
            }*/

            result = new HitTestResult();
            result.Object = this;

            // �۲��������ĸ���λ
            Rectangle rectCheckBox = this.DataRoot.m_rectCheckBox;
            if (p_x >= rectCheckBox.X
                && p_x <= rectCheckBox.X + rectCheckBox.Width
                && p_y >= rectCheckBox.Y
                && p_y <= rectCheckBox.Y + rectCheckBox.Height)
                result.AreaPortion = AreaPortion.CheckBox;
            else
                result.AreaPortion = AreaPortion.Content;
            result.X = p_x;
            result.Y = p_y;
        }

        // ��ͼ �ո���
        public void Paint(
            long start_x,
            long start_y,
            PaintEventArgs e)
        {
            if (DataRoot.TooLarge(start_x) == true
                || DataRoot.TooLarge(start_y) == true)
                return;

            DayStateDef def = this.DataRoot.DayStateDefs.GetDef(this.State);
            Color colorText = Color.Black;
            Color colorBack = Color.White;
            if (def != null)
            {
                colorText = def.TextColor;
                colorBack = def.BackColor;
            }

            if (colorBack != Color.White)
            {
                colorBack = Color.FromArgb(100, colorBack);

                // ���Ʊ���
                PaintBack(
                    start_x,
                    start_y,
                    this.Width,
                    this.Height,
                    e,
                    colorBack);
            }

            int x0 = (int)start_x;
            int y0 = (int)start_y;


            Pen pen = new Pen(Color.FromArgb(50, Color.Gray));

            // ������
            e.Graphics.DrawLine(pen,
                new PointF(x0, y0),
                new PointF(x0, y0 + this.Height)
                );

            // �Ϸ�����
            e.Graphics.DrawLine(pen,
                new PointF(x0, y0),
                new PointF(x0 + this.Width, y0)
                );

            RectangleF rect = new RectangleF(
x0,
y0,
this.Width,
this.Height);

            if (this.Blank == false )
            {

                // ����״̬ͼ��
                if (def != null 
                    && (this.DataRoot.HoverCheckBox == false || this.m_bHover == true))
                {
                    Image image = def.Icon;
                    if (image != null)
                    {
                        e.Graphics.DrawImage(image,
                            (float)x0 + this.DataRoot.m_rectCheckBox.X,
                            (float)y0 + this.DataRoot.m_rectCheckBox.Y);
                    }
                }


                // ��������
                Font font = this.DataRoot.DayTextFont;
                if (this.m_bFocus)
                    font = new Font(font.FontFamily.GetName(0), 
                        font.Size + 3, 
                        font.Style, 
                        font.Unit);
                Brush brushText = null;


                brushText = new SolidBrush(colorText);


                StringFormat stringFormat = new StringFormat();

                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawString(this.DayName,
                    font,
                    brushText,
                    rect,
                    stringFormat);
            }

            // ѡ����Ч��
            if (this.m_bSelected == true)
            {
                this.PaintSelectEffect(
                    start_x,
                    start_y,
                    this.Width,
                    this.Height,
                    e);
            }

            // ��������
            if (this.m_bFocus == true)
            {
                rect.Inflate(-4, -4);
                ControlPaint.DrawFocusRectangle(e.Graphics,
                    Rectangle.Round(rect));
            }
        }

        public int State
        {
            get
            {
                return this.m_nDayState;
            }
            set
            {
                // �հ׸��Ӳ����޸�״̬
                if (this.Blank == false)
                    this.m_nDayState = value;
            }
        }

        /*
        public bool Checked
        {
            get
            {
                return m_bChecked;
            }
            set
            {
                m_bChecked = value;
            }
        }*/

        public int Day
        {
            get
            {
                return this.NameValue;
                // return this.m_nDay;
            }
        }

        public string DayName
        {
            get
            {
                if (this.Day == 0)
                    return null;    // ��ʾ��ǰ����û��ʹ��

                return this.Day.ToString();
            }
        }

        public int Month
        {
            get
            {
                return this.Container.Container.Month;
            }
        }

        // �ڼ�������
        public int Week
        {
            get
            {
                return this.Container.Week;
            }
        }

        public int Year
        {
            get
            {
                return this.Container.Container.Container.Year;
            }
        }

        // ��һ�������е���һ�� 0��ʾ������
        public int DayOfWeek
        {
            get
            {
                return this.m_nDayOfWeek;
            }
        }

 
        public string DayOfWeekName(string strLang)
        {
            if (strLang == "zh")
                return WeekArea.WeekDayNames_ZH[this.m_nDayOfWeek];
            if (strLang == "en")
                return WeekArea.WeekDayNames_EN[this.m_nDayOfWeek];
            throw new Exception("��֧�ֵ����Դ��� '" + strLang + "'");
        }

        // �Ƿ�Ϊ�հ׸���
        public bool Blank
        {
            get
            {
                if (this.NameValue == 0)
                    return true;

                return false;
            }
        }

        // ���캯��
        // parameters:
        //      nDay    �գ���1��ʼ���������Ϊ0����ʾ�ø���δʹ��
        public DayArea(WeekArea container, int nDay, int nDayOfWeek)
        {
            this._Container = container;

            this.NameValue = nDay;
            // this.m_nDay = nDay;

            if (nDay != 0)
                this.m_nDayState = 0;   // ��ʼ��Ϊȱʡ״̬


            this.m_nDayOfWeek = nDayOfWeek;
        }

        // ���طǿյ���һ��
        public DayArea NextNoneBlankDayArea
        {
            // ��д
            get
            {
                DayArea day = this;
                for (; ; )
                {
                    day = (DayArea)day.GetNextSibling();
                    if (day == null)
                        return null;
                    if (day.Blank == false)
                        return day;
                }
            }
        }

        // ���طǿյ�ǰһ��
        public DayArea PrevNoneBlankDayArea
        {
            get
            {
                DayArea day = this;
                for (; ; )
                {
                    day = (DayArea)day.GetPrevSibling();
                    if (day == null)
                        return null;
                    if (day.Blank == false)
                        return day;
                }
            }
        }


        // �л���״̬
        // return:
        //      ״̬�Ƿ����˸ı�
        public bool ToggleState()
        {
            if (this.Blank == true)
                return false;

            DayStateDefCollection defs = this.DataRoot.DayStateDefs;
            if (defs != null)
            {
                if (defs.Count == 1)
                    return false;   // ֻ��һ��״̬���޷��л�

                if (this.m_nDayState >= defs.Count - 1)
                {
                    this.m_nDayState = 0;
                }
                else
                    this.m_nDayState++;
                return true;
            }

            return false;
        }

        public string Name8
        {
            get
            {
                return this.Year.ToString().PadLeft(4, '0')
                + this.Month.ToString().PadLeft(2, '0')
                + this.Day.ToString().PadLeft(2, '0');
            }
        }

    }

    // ��������
    public enum AreaPortion
    {
        None = 0,
        LeftBar = 1,    // ��ߵ�����
        ColumnTitle = 2,    // ��Ŀ����
        Content = 3,    // ���ݱ���
        CheckBox = 4,   // checkbox

        LeftBlank = 5,  // ��߿հ�
        TopBlank = 6,   // �Ϸ��հ�
        RightBlank = 7, // �ҷ��հ�
        BottomBlank = 8,    // �·��հ�
    }

    // ��������
    public class HitTestResult
    {
        public AreaBase Object = null;    // �������ĩ������
        public AreaPortion AreaPortion = AreaPortion.None;

        // ���������µĵ��λ��
        public long X = -1;
        public long Y = -1;

        public int Param = 0;   // ��������
    }

    // ѡ��һ������Ķ���
    public enum SelectAction
    {
        Toggle = 0,
        On = 1,
        Off = 2,
    }

    /*
        // ��״̬
    public enum DayState
    {
        NoneWorkDay = 0,
        WorkDay = 1,
    }*/

    // һ��״̬����
    public class DayStateDef
    {
        // ����
        public string Caption = "";

        /*
        // ״ֵ̬
        public int State = -1;  // -1��ʾ��δ��ʼ��
         * */

        // ͼ��
        public Image Icon = null;

        // ������ɫ
        public Color TextColor = Color.Black;
        // ������ɫ
        public Color BackColor = Color.White;
    }

    // һϵ��״̬����
    public class DayStateDefCollection : List<DayStateDef>
    {
        public int IconWidth 
        {
            get
            {
                for (int i = 0; i < this.Count; i++)
                {
                    DayStateDef item = this[i];
                    if (item.Icon != null)
                        return item.Icon.Width;
                }

                return 16;
            }
        }
        public int IconHeight
        {
            get
            {
                for (int i = 0; i < this.Count; i++)
                {
                    DayStateDef item = this[i];
                    if (item.Icon != null)
                        return item.Icon.Height;
                }

                return 16;
            }
        }

        public DayStateDef GetDef(int nState)
        {
            if (nState < 0 || nState >= this.Count)
                return null;
            return this[nState];
        }

    }
}
