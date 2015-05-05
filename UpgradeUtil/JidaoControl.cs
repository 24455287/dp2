using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using System.Drawing.Drawing2D;

using DigitalPlatform;
using DigitalPlatform.Marc;
using DigitalPlatform.IO;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.CommonControl;

namespace UpgradeUtil
{
    /// <summary>
    /// dt1000�ǵ��ؼ�
    /// ��������dt1000�ڿ��ǵ�����
    /// </summary>
    public partial class JidaoControl : ScrollableControl
    {
        public string TimeStyle = "";    // day/month

        public List<JidaoYear> Years = new List<JidaoYear>();

        int m_nCellWidth = 70;
        int m_nCellHeight = 70;
        int m_nLeftTextWidth = 100;

        int m_nDaysTitleHeight = 20;

        Padding m_padding = new Padding(0, 0, 0, 0);

        public JidaoControl()
        {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            pe.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            Point pt = AutoScrollPosition;
            pe.Graphics.TranslateTransform(pt.X, pt.Y);

            Font fontSmall = new Font("΢���ź�", 12, GraphicsUnit.Pixel);
            Font fontLarge = new Font("΢���ź�", 24, GraphicsUnit.Pixel);
            Brush brushText = new SolidBrush(Color.Black);
            Brush brushGray = new SolidBrush(Color.LightGray);
            Pen penBorder = new Pen(Color.Gray);


            int y = m_padding.Top;
            {
                // �ո���ˮƽ����
                int x = this.m_padding.Left + this.m_nLeftTextWidth;
                for (int i = 0; i < 31; i++)
                {
                    Rectangle rectCell = new Rectangle(x, y,
                        this.m_nCellWidth, this.m_nDaysTitleHeight);

                    pe.Graphics.DrawRectangle(penBorder, rectCell);
                    pe.Graphics.DrawString((i + 1).ToString(), fontSmall, brushText, rectCell);

                    x += this.m_nCellWidth;
                }
            }

            y += m_nDaysTitleHeight;
            for (int i = 0; i < this.Years.Count; i++)
            {
                JidaoYear year = this.Years[i];

                for (int j = 0; j < year.Months.Count; j++)
                {
                    JidaoMonth month = year.Months[j];
                    // ��ݣ��·�
                    string strTitle = year.Year.ToString().PadLeft(4, '0') + "." + month.Month.ToString();
                    Rectangle rect = new Rectangle(this.m_padding.Left,
                        y,
                        this.m_nLeftTextWidth,
                        this.m_nCellHeight);
                    pe.Graphics.DrawString(strTitle, fontSmall, brushText, rect);

                    // �ո���
                    int x = this.m_padding.Left + this.m_nLeftTextWidth;
                    for (int k = 0; k < month.Cells.Count; k++)
                    {
                        JidaoCell cell = month.Cells[k];
                        Rectangle rectCell = new Rectangle(x, y,
                            this.m_nCellWidth, this.m_nCellHeight);

                        if (this.TimeStyle == "month"
                            && k > 0)
                        {
                            pe.Graphics.FillRectangle(brushGray, rectCell);
                        }
                        else
                        {
                            if (cell != null && cell.Disable == false)
                            {
                                pe.Graphics.DrawRectangle(penBorder, rectCell);
                                string strDate = year.Year.ToString() + "-" + month.Month.ToString() + "-" + (k + 1).ToString();

                                PaintCell(
                                    strDate,
                                    rectCell,
                                    cell,
                                    pe,
                                    brushText,
                                    fontSmall,
                                    fontLarge);
                            }
                            else
                                pe.Graphics.FillRectangle(brushGray, rectCell);
                        }

                        x += this.m_nCellWidth;
                    }

                    y += this.m_nCellHeight;
                }
            }



            // Calling the base class OnPaint
            base.OnPaint(pe);
        }

        static void PaintCell(
            string strDate,
            Rectangle rect,
            JidaoCell cell,
            PaintEventArgs pe,
            Brush brushText,
            Font fontSmall,
            Font fontLarge)
        {
            // ���� ��һ�ţ��Ҷ���
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Far;
            stringFormat.LineAlignment = StringAlignment.Near;
            stringFormat.FormatFlags = StringFormatFlags.NoWrap;

            SizeF size = pe.Graphics.MeasureString(strDate, fontSmall);

            pe.Graphics.DrawString(strDate,
                fontSmall,
                brushText,
                rect,
                stringFormat);

            // ���ں� ��� �ڶ��ţ������
            string strSecondLine = "";
            
            if (String.IsNullOrEmpty(cell.Zong) == false)
                strSecondLine = "��." + cell.Zong;

            if (String.IsNullOrEmpty(cell.Vol) == false)
            {
                if (String.IsNullOrEmpty(strSecondLine) == false)
                    strSecondLine += ", ";
                strSecondLine += "v." + cell.Vol;
            }

            if (String.IsNullOrEmpty(strSecondLine) == false)
            {
                stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.LineAlignment = StringAlignment.Near;
                stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                Rectangle rectSecondLine = rect;
                rectSecondLine.Y += (int)size.Height;
                rectSecondLine.Height -= (int)size.Height;
                pe.Graphics.DrawString(strSecondLine,
                    fontSmall,
                    brushText,
                    rectSecondLine,
                    stringFormat);
            }


            // �����ں� ���½�
            stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Far;
            stringFormat.LineAlignment = StringAlignment.Far;
            stringFormat.FormatFlags = StringFormatFlags.NoWrap;

            pe.Graphics.DrawString(cell.No,
                fontLarge, 
                brushText,
                rect,
                stringFormat);

            // �������� ���½�
            stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Far;
            stringFormat.FormatFlags = StringFormatFlags.NoWrap;

            pe.Graphics.DrawString(cell.OperDate,
                fontSmall,
                brushText,
                rect,
                stringFormat);
        }

        public static List<String> GetMenuTexts(string strMARC)
        {
            List<string> results = new List<string>();

            results.Add("920 �ʷ���");
            results.Add("921 ���ʷ���");
            results.Add("922 ֱ����");
            results.Add("923 ������");
            results.Add("924 �ʽɿ�");

            string strField = "";
            string strNextFieldName = "";

            for (int i = 0; i < results.Count; i++)
            {
                string strText = results[i];

                string strFieldName = strText;
                int nRet = strFieldName.IndexOf(" ");
                if (nRet != -1)
                    strFieldName = strFieldName.Substring(0, nRet).Trim();

                // �ʷ���
                nRet = MarcUtil.GetField(strMARC,
                    strFieldName,
                    0,
                    out strField,
                    out strNextFieldName);
                if (String.IsNullOrEmpty(strField) == false)
                {
                    strText += "*";
                    results[i] = strText;
                }
            }

            return results;
        }

        // ��ʼ������
        public int SetData(
            string strFieldName,
            string strMARC,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (strFieldName != "920"
                && strFieldName != "921"
                && strFieldName != "922"
                && strFieldName != "923"
                && strFieldName != "924")
            {
                strError = "��֧�ֵļǵ��ֶ��� '" + strFieldName + "'";
                return -1;
            }

            this.Years.Clear();

            this.TimeStyle = "";

            string strField = "";
            string strNextFieldName = "";

            // �ʷ���
            nRet = MarcUtil.GetField(strMARC,
                strFieldName,
                0,
                out strField,
                out strNextFieldName);
            if (String.IsNullOrEmpty(strField) == false)
            {
                nRet = SetOneData(strFieldName,
                    strField,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            // ������������ߴ�
            this.AutoScrollMinSize = new Size(this.m_padding.Horizontal + this.m_nLeftTextWidth + this.m_nCellWidth * 31
                + SystemInformation.VerticalScrollBarWidth,
                            this.m_padding.Vertical + this.m_nDaysTitleHeight + this.m_nCellHeight * 12 * this.Years.Count
                            + SystemInformation.HorizontalScrollBarHeight);
            this.Invalidate();
            return 0;
        }

        JidaoDay FindDay(List<JidaoDay> days,
            string strDate)
        {
            for (int i = 0; i < days.Count; i++)
            {
                JidaoDay day = days[i];
                if (strDate == day.Date)
                    return day;
            }

            return null;
        }

        public class JidaoDayCompare : IComparer<JidaoDay>
        {
            int IComparer<JidaoDay>.Compare(JidaoDay x, JidaoDay y)
            {
                return string.Compare(x.Date, y.Date);
            }
        }

        // ����ǰ�ļ��
        // return:
        //      -1  ������ʧ��
        //      0   ����û�д�
        //      1   �����д�
        public int Check(string strMARC,
            out string strError)
        {
            int nRet = 0;
            strError = "";

            List<string> fieldnames = new List<string>();

            fieldnames.Add("920");
            fieldnames.Add("921");
            fieldnames.Add("922");
            fieldnames.Add("923");
            fieldnames.Add("924");

            List<JidaoDay> days = new List<JidaoDay>();

            for (int i = 0; i < fieldnames.Count; i++)
            {
                string strFieldName = fieldnames[i];

                this.Years.Clear();
                this.TimeStyle = "";

                string strField = "";
                string strNextFieldName = "";

                nRet = MarcUtil.GetField(strMARC,
                    strFieldName,
                    0,
                    out strField,
                    out strNextFieldName);
                if (String.IsNullOrEmpty(strField) == true)
                    continue;

                nRet = SetOneData(strFieldName,
                    strField,
                    out strError);
                if (nRet == -1)
                    return -1;

                // �ϲ������Ӵ洢
                nRet = MergeToDays(ref days,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return this.CheckDays(days,
            out strError);
        }

        // ����MARC����Ϣ��dp2 xml �ڼ�¼��ʽ
        public int Upgrade(string strMARC,
            string strOperator,
            out List<string> Xmls,
            out string strError)
        {
            int nRet = 0;
            strError = "";
            Xmls = null;

            List<string> fieldnames = new List<string>();

            fieldnames.Add("920");
            fieldnames.Add("921");
            fieldnames.Add("922");
            fieldnames.Add("923");
            fieldnames.Add("924");

            List<JidaoDay> days = new List<JidaoDay>();

            for (int i = 0; i < fieldnames.Count; i++)
            {
                string strFieldName = fieldnames[i];

                this.Years.Clear();
                this.TimeStyle = "";

                string strField = "";
                string strNextFieldName = "";

                // 
                nRet = MarcUtil.GetField(strMARC,
                    strFieldName,
                    0,
                    out strField,
                    out strNextFieldName);
                if (String.IsNullOrEmpty(strField) == true)
                    continue;

                nRet = SetOneData(strFieldName,
                    strField,
                    out strError);
                if (nRet == -1)
                    return -1;

                // �ϲ������Ӵ洢
                nRet = MergeToDays(ref days,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            // ת��Ϊ�ڼ�¼��ʽ
            nRet = ConvertToIssueXmls(days,
                strOperator,
                out Xmls,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

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

        // ת��Ϊ�ڼ�¼��ʽ
        int ConvertToIssueXmls(
            List<JidaoDay> days,
            string strOperator,
            out List<string> Xmls,
            out string strError)
        {
            int nRet = 0;
            strError = "";
            Xmls = new List<string>();

            string strOperTime = DateTimeUtil.Rfc1123DateTimeString(DateTime.Now.ToUniversalTime());

            // ����
            days.Sort(new JidaoDayCompare());

            for (int i = 0; i < days.Count; i++)
            {
                JidaoDay day = days[i];

                Debug.Assert(day.Cells.Count > 0, "");
                if (day.Cells.Count == 0)
                    continue;

                JidaoCell first_cell = day.Cells[0];

                XmlDocument dom = new XmlDocument();
                dom.LoadXml("<root />");


                DomUtil.SetElementText(dom.DocumentElement,
                    "publishTime",
                    day.Date);
                DomUtil.SetElementText(dom.DocumentElement,
                    "issue",
                    first_cell.No);
                if (String.IsNullOrEmpty(first_cell.Zong) == false)
                {
                    DomUtil.SetElementText(dom.DocumentElement,
                        "zong",
                        first_cell.Zong);
                }
                if (String.IsNullOrEmpty(first_cell.Vol) == false)
                {
                    DomUtil.SetElementText(dom.DocumentElement,
                        "volume",
                        first_cell.Vol);
                }
                if (String.IsNullOrEmpty(first_cell.Comment) == false)
                {
                    DomUtil.SetElementText(dom.DocumentElement,
                        "comment",
                        first_cell.Comment);
                }

                DomUtil.SetElementText(dom.DocumentElement,
                    "refID",
                    Guid.NewGuid().ToString());

                // ����<orderInfo>
                XmlNode nodeOrderInfo = dom.CreateElement("orderInfo");
                dom.DocumentElement.AppendChild(nodeOrderInfo);

                for (int j = 0;j< day.Cells.Count; j++)
                {
                    JidaoCell cell = day.Cells[j];

                    XmlNode nodeOrderRoot = dom.CreateElement("record");
                    nodeOrderInfo.AppendChild(nodeOrderRoot);

                    DomUtil.SetElementText(nodeOrderRoot,
                        "seller",
                        cell.Source);

                    string strCopy = LinkOldNewValue(cell.OrderCopy.ToString(),
                        cell.ArrivedCopy.ToString());

                    DomUtil.SetElementText(nodeOrderRoot,
                        "copy",
                        strCopy);

                    if (cell.OrderCopy > 0 || cell.ArrivedCopy > 0)
                    {
                        int nMax = Math.Max(cell.OrderCopy, cell.ArrivedCopy);
                        int nCount = 0;
                        // distribute string
                        LocationCollection locations = new LocationCollection();
                        for (int k = 0; k < nMax; k++)
                        {
                            DigitalPlatform.Location location = new DigitalPlatform.Location();
                            location.Name = "δ֪";
                            if (nCount < cell.ArrivedCopy)
                            {
                                location.RefID = "*";
                                nCount++;
                            }
                            locations.Add(location);
                        }
                        DomUtil.SetElementText(nodeOrderRoot,
                            "distribute",
                            locations.ToString(true));
                    }

                    try
                    {
                        DateTime time = DateTimeUtil.Long8ToDateTime(cell.OperDate);
                        string strTime = DateTimeUtil.Rfc1123DateTimeString(time.ToUniversalTime());

                        // ���û���ˢ��һ����������
                        nRet = SetOperation(
                            nodeOrderRoot,
                            "lastModified",
                            cell.Operator,
                            "upgrade from dt1000",
                            strTime,
                            out strError);
                        if (nRet == -1)
                            return -1;
                    }
                    catch
                    {
                    }
                }

                if (String.IsNullOrEmpty(strOperator) == false)
                {
                    // ���û���ˢ��һ����������
                    nRet = SetOperation(
                        dom.DocumentElement,
                        "create",
                        strOperator,
                        "upgrade from dt1000",
                        strOperTime,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

                Xmls.Add(dom.DocumentElement.OuterXml);
            }

            return 0;
        }

        // ���û���ˢ��һ����������
        public static int SetOperation(
            XmlNode root,
            string strOperName,
            string strOperator,
            string strComment,
            string strTime,
            out string strError)
        {
            strError = "";

            if (root == null)
            {
                strError = "root == null";
                return -1;
            }

            XmlNode nodeOperations = root.SelectSingleNode("operations");
            if (nodeOperations == null)
            {
                nodeOperations = root.OwnerDocument.CreateElement("operations");
                root.AppendChild(nodeOperations);
            }

            XmlNode node = nodeOperations.SelectSingleNode("operation[@name='" + strOperName + "']");
            if (node == null)
            {
                node = root.OwnerDocument.CreateElement("operation");
                nodeOperations.AppendChild(node);
                DomUtil.SetAttr(node, "name", strOperName);
            }

            DomUtil.SetAttr(node, "time", strTime);
            DomUtil.SetAttr(node, "operator", strOperator);
            if (String.IsNullOrEmpty(strComment) == false)
                DomUtil.SetAttr(node, "comment", strComment);

            return 0;
        }

        // �ϲ������Ӵ洢
        int MergeToDays(ref List<JidaoDay> days,
            out string strError)
        {
            strError = "";

            for (int i = 0; i < this.Years.Count; i++)
            {
                JidaoYear year = this.Years[i];

                for (int j = 0; j < year.Months.Count; j++)
                {
                    JidaoMonth month = year.Months[j];
                    for (int k = 0; k < month.Cells.Count; k++)
                    {
                        JidaoCell cell = month.Cells[k];
                        if (cell == null)
                            continue;
                        if (cell.Disable == true)
                            continue;
                        if (cell.ArrivedCopy == -1)
                            continue;
                        string strDate = DateTimeUtil.DateTimeToString8(new DateTime(year.Year, month.Month, k + 1));
                        JidaoDay day = FindDay(days, strDate);
                            
                        if (day == null)    
                        {
                            day = new JidaoDay();
                            day.Date = strDate;
                            days.Add(day);
                        }

                        day.Cells.Add(cell);
                    }
                }
            }
            return 0;
        }

        static int CompareTwoCell(JidaoCell a, 
            JidaoCell b,
            out string strError)
        {
            strError = "";

            if (a.No != b.No)
            {
                strError += "�ںŲ�ͬ('"+a.No+"'��'"+b.No+"')";
            }
            if (a.Zong != b.Zong)
            {
                if (String.IsNullOrEmpty(strError) == false)
                    strError += ",";
                strError += "���ںŲ�ͬ('" + a.Zong + "'��'" + b.Zong + "')";
            }
            if (a.Vol != b.Vol)
            {
                if (String.IsNullOrEmpty(strError) == false)
                    strError += ",";
                strError += "��Ų�ͬ('" + a.Vol + "'��'" + b.Vol + "')";
            }

            if (String.IsNullOrEmpty(strError) == false)
                return 1;

            return 0;
        }

        // ���м��
        // return:
        //      -1  ������ʧ��
        //      0   ����û�д�
        //      1   �����д�
        int CheckDays(List<JidaoDay> days,
            out string strError)
        {
            strError = "";

            // ����
            days.Sort(new JidaoDayCompare());

            for (int i = 0; i < days.Count; i++)
            {
                JidaoDay day = days[i];

                Debug.Assert(day.Cells.Count > 0, "");
                if (day.Cells.Count == 0)
                    continue;

                JidaoCell prev_cell = null;

                for (int j = 0; j < day.Cells.Count; j++)
                {
                    JidaoCell cell = day.Cells[j];

                    if (cell != null
                        && String.IsNullOrEmpty(cell.OperDate) == false
                        && cell.OperDate != "00000000")
                    {
                        try
                        {
                            DateTime date = DateTimeUtil.Long8ToDateTime(cell.OperDate);
                            if (date.Year < 1900 || date.Year > DateTime.Now.Year)
                            {
                                if (String.IsNullOrEmpty(strError) == false)
                                    strError += ";\r\n";
                                strError += "����Ϊ '" + day.Date + "' �������� '" + cell.Source + "' �ļǵ�����ʱ�� '" + cell.OperDate + "' ���Ʋ���";
                            }
                        }
                        catch
                        {
                            if (String.IsNullOrEmpty(strError) == false)
                                strError += ";\r\n";
                            strError += "����Ϊ '" + day.Date + "' �������� '" + cell.Source + "' �ļǵ�����ʱ�� '"+cell.OperDate+"' ��ʽ���Ի��߳����˲����ڵ�����" ;
                        }
                    }

                    if (prev_cell != null)
                    {
                        if (prev_cell.No != cell.No)
                        {
                            string strMessage = "";
                            int nRet = CompareTwoCell(prev_cell,
                                cell,
                                out strMessage);
                            if (nRet == 1)
                            {
                                if (String.IsNullOrEmpty(strError) == false)
                                    strError += ";\r\n";
                                strError += "����Ϊ '" + day.Date + "' �������� '"+prev_cell.Source+"' �� '"+cell.Source+"' ���������²�ͬ: " + strMessage;
                            }
                        }
                    }

                    prev_cell = cell;
                }
            }

            if (String.IsNullOrEmpty(strError) == false)
                return 1;

            return 0;
        }

        int SetOneData(
            string strFieldName,
            string strField,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            for (int i = 0; ; i++)
            {
                string strGroup = "";
                // return:
                //		-1	����
                //		0	��ָ�������ֶ���û���ҵ�
                //		1	�ҵ����ҵ������ֶ��鷵����strGroup������
                nRet = MarcUtil.GetGroup(strField,
                    i,
                    out strGroup);
                if (nRet != 1)
                    break;

                string strSubfield = "";
                string strNextSubfieldName = "";
                // parameters:
                //		textType	��ʾstrText�а��������ֶ����ݻ��������ݡ���ΪItemType.Field����ʾstrText������Ϊ�ֶΣ���ΪItemType.Group����ʾstrText������Ϊ���ֶ��顣
                //		strSubfield		[out]������ֶΡ����ֶ���(1�ַ�)�����ֶ����ݡ�
                // return:
                //		-1	����
                //		0	��ָ�������ֶ�û���ҵ�
                //		1	�ҵ����ҵ������ֶη�����strSubfield������
                nRet = MarcUtil.GetSubfield(strGroup,
                    ItemType.Group,
                    "a",
                    0,
                    out strSubfield,
                    out strNextSubfieldName);
                if (nRet != 1)
                    continue;

                Debug.Assert(strSubfield.Length >= 1, "");
                string strTimeRange = strSubfield.Substring(1);
                if (String.IsNullOrEmpty(strTimeRange) == true)
                    continue;

                string strStart = "";
                string strEnd = "";

                // չ��ʱ�䷶Χ
                nRet = EnsureTimeRange(
                    strFieldName,
                    strTimeRange,
                    out strStart,
                    out strEnd,
                    out strError);
                if (nRet == -1)
                    return -1;

                // $b ��������
                string strOrderCopy = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "b",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (nRet == 1)
                {
                    Debug.Assert(strSubfield.Length >= 1, "");
                    strOrderCopy = strSubfield.Substring(1);
                }

                // $c ʵ������
                string strArrivedCopy = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "c",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (nRet == 1)
                {
                    Debug.Assert(strSubfield.Length >= 1, "");
                    strArrivedCopy = strSubfield.Substring(1);
                }

                // $Z �������
                string strBitMask = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "Z",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (nRet == 1)
                {
                    Debug.Assert(strSubfield.Length >= 1, "");
                    strBitMask = strSubfield.Substring(1);
                }

                // $Y Ԥ������
                string strYuceMask = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "Y",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (nRet == 1)
                {
                    Debug.Assert(strSubfield.Length >= 1, "");
                    strYuceMask = strSubfield.Substring(1);
                }

                if (String.IsNullOrEmpty(strBitMask) == false)
                {
                    nRet = SetArrivedCells(
                        strStart,
                        strEnd,
                        strBitMask,
                        strYuceMask,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

                // $w �ں�
                string strIssueNos = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "w",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (nRet == 1)
                {
                    Debug.Assert(strSubfield.Length >= 1, "");
                    strIssueNos = strSubfield.Substring(1);
                }

                // $S ���ں�
                string strZongs = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "S",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (nRet == 1)
                {
                    Debug.Assert(strSubfield.Length >= 1, "");
                    strZongs = strSubfield.Substring(1);
                }

                // $V ���
                string strVolumes = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "V",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (nRet == 1)
                {
                    Debug.Assert(strSubfield.Length >= 1, "");
                    strVolumes = strSubfield.Substring(1);
                }

                // $t ����ʱ��
                string strOperDates = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "t",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (nRet == 1)
                {
                    Debug.Assert(strSubfield.Length >= 1, "");
                    strOperDates = strSubfield.Substring(1);
                }

                // $p ������
                string strOperator = "";
                nRet = MarcUtil.GetSubfield(strGroup,
    ItemType.Group,
    "p",
    0,
    out strSubfield,
    out strNextSubfieldName);
                if (nRet == 1)
                {
                    Debug.Assert(strSubfield.Length >= 1, "");
                    strOperator = strSubfield.Substring(1);
                }

                if (String.IsNullOrEmpty(strIssueNos) == false)
                {
                    nRet = SetIssueNos(
                        strStart,
                        strEnd,
                        strIssueNos,
                        strZongs,
                        strVolumes,
                        strOperDates,
                        strOperator,
                        strOrderCopy,
                        strArrivedCopy,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

            }


            return 0;
        }

        int SetIssueNos(
            string strStart,
            string strEnd,
            string strNos,
            string strZongs,
            string strVolumes,
            string strOperDates,
            string strOperator,
            string strOrderCopy,
            string strArrivedCopy,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (strStart.Length == 6)
            {
                strStart = strStart + "01";
                Debug.Assert(this.TimeStyle == "month", "");
            }
            if (strEnd.Length == 6)
            {
                strEnd = strEnd + "01";
                Debug.Assert(this.TimeStyle == "month", "");
            }
            DateTime start = DateTimeUtil.Long8ToDateTime(strStart);
            DateTime end = DateTimeUtil.Long8ToDateTime(strEnd);

            if (start > end)
            {
                strError = "SetIssueNos() error, start[" + strStart + "] > end[" + strEnd + "]";
                return -1;
            }

            Debug.Assert(start <= end, "");

            int nOrderCopy = 0;
            if (String.IsNullOrEmpty(strOrderCopy) == false)
            {
                try
                {
                    nOrderCopy = Convert.ToInt32(strOrderCopy);
                }
                catch
                {
                    strError = "���������� '" + strOrderCopy + "' ��ʽ����";
                    return -1;
                }
            }

            try
            {

                List<string> numbers = null;
                List<string> copies = null;
                List<string> comments = null;
                ExpandNoSequence(strNos,
                out numbers,
                out comments,
                out copies);
                Debug.Assert(numbers.Count == copies.Count, "");
                Debug.Assert(numbers.Count == comments.Count, "");
                // List<string> numbers = ExpandSequence(strNos);

                List<string> zongs = ExpandSequence(strZongs);
                List<string> volumes = ExpandSequence(strVolumes);
                List<string> dates = ExpandDateSequence(strOperDates);

                int nOffsetNumber = 0;
                int nOffsetAnother = 0;
                for (DateTime cur = start; cur <= end; )
                {
                    // ���һ�����ӵ�JidaoCell����
                    JidaoCell cell = null;
                    nRet = GetOneCell(cur,
                       out cell,
                       out strError);
                    if (nRet == -1)
                        return -1;

                    Debug.Assert(cell != null, "");

                    if (cell.On == true)
                    {
                        if (nOffsetNumber < numbers.Count)
                        {
                            cell.No = numbers[nOffsetNumber];
                            string strCopy = copies[nOffsetNumber];
                            if (String.IsNullOrEmpty(strCopy) == true)
                                strCopy = strArrivedCopy;
                            if (String.IsNullOrEmpty(strCopy) == true)
                                strCopy = "0";
                            try
                            {
                                cell.ArrivedCopy = Convert.ToInt32(strCopy);
                            }
                            catch
                            {
                                strError = "������ '" + strCopy + "' ��ʽ����";
                                return -1;
                                // cell.ArrivedCopy = 0;
                            }

                            cell.OrderCopy = nOrderCopy;
                            cell.Comment = comments[nOffsetNumber];
                        }
                        nOffsetNumber++;
                    }

                    if (cell.Disable == false)
                    {
                        if (nOffsetAnother < zongs.Count)
                        {
                            cell.Zong = zongs[nOffsetAnother];
                        }

                        if (nOffsetAnother < volumes.Count)
                        {
                            cell.Vol = volumes[nOffsetAnother];
                        }

                        if (nOffsetAnother < dates.Count)
                        {
                            cell.OperDate = dates[nOffsetAnother];
                            cell.Operator = strOperator;
                        }

                        nOffsetAnother++;
                    }

                    if (cur >= end)
                        break;

                    if (this.TimeStyle == "day")
                        cur = cur.AddDays(1);
                    else
                        cur = cur.AddMonths(1);

                }
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            return 0;
        }

        // ���ܻ��׳��쳣
        static string AddDelta(string strDate,
            string strDelta)
        {
            if (StringUtil.IsPureNumber(strDate) == false)
                throw new Exception("��� '"+strDate+"' Ӧ��Ϊȫ������");

            long date = Convert.ToInt64(strDate);

            if (StringUtil.IsNumber(strDelta) == false)
                throw new Exception("����ֵ '" + strDelta + "' Ӧ����������");

            long delta = Convert.ToInt64(strDelta);

            return (date + delta).ToString().PadLeft(8, '0');
        }

        // չ�������ַ���
        // �����׳��쳣
        static List<string> ExpandDateSequence(string strText)
        {
            try
            {

                string strPrevValue = "00000000";
                List<string> results = new List<string>();
                string[] parts = strText.Split(new char[] { ',' });
                for (int i = 0; i < parts.Length; i++)
                {
                    string strPart = parts[i];
                    if (String.IsNullOrEmpty(strPart) == true)
                    {
                        results.Add(strPart);
                        continue;
                    }

                    // *
                    int nRet = strPart.IndexOf("*");
                    if (nRet != -1)
                    {
                        string strValue = strPart.Substring(0, nRet);
                        string strCount = strPart.Substring(nRet + 1);

                        if (strValue == "z")
                            strValue = "00000000";
                        else
                        {
                            // if (strValue.Length != 8)
                            {
                                strValue = AddDelta(strPrevValue, strValue);
                                Debug.Assert(strValue.IndexOf("-") == -1, "");
                                Debug.Assert(strValue != "00000000", "");
                            }

                            if (strValue != "00000000")  // ��������
                                strPrevValue = strValue;
                            Debug.Assert(strPrevValue != "00000000", "");
                        }

                        if (StringUtil.IsNumber(strCount) == false)
                            throw new Exception("���� '"+strCount+"' ��ʽ����ȷ");

                        int count = Convert.ToInt32(strCount);
                        for (int j = 0; j < count; j++)
                        {
                            // TODO: У��ʱ���ַ����Ƿ���ȷ
                            // DateTimeUtil.Long8ToDateTime()
                            results.Add(strValue);
                        }

                        // strPrevValue = strValue;
                        continue;
                    }

                    if (strPart == "z")
                    {
                        strPart = "00000000";
                        // ������
                    }
                    else
                    {
                        Debug.Assert(strPart != "00000000", "");
                        // if (strPart.Length != 8)
                        {
                            string strTemp = AddDelta(strPrevValue, strPart);
                            Debug.Assert(strTemp.IndexOf("-") == -1, "");
                            // Debug.Assert(strTemp != "00000000", "");
                            strPart = strTemp;
                        }

                        if (strPart != "00000000")  // ��������
                            strPrevValue = strPart;
                        Debug.Assert(strPrevValue != "00000000", "");
                    }

                    // TODO: У��ʱ���ַ����Ƿ���ȷ
                    results.Add(strPart);
                }

                return results;
            }
            catch (Exception ex)
            {
                throw new Exception("�������� '" + strText + "' ��ʽ����ȷ: " + ex.Message);
            }
        }

        static void ParseNoCommentCopy(string strText,
            out string strNo,
            out string strComment,
            out string strCopy)
        {
            string strTemp = "";
            int nRet = strText.IndexOf(":");
            if (nRet != -1)
            {
                strTemp = strText.Substring(0, nRet).Trim();
                strCopy = strText.Substring(nRet + 1).Trim();
            }
            else
            {
                strTemp = strText.Trim();
                strCopy = "";
            }

            nRet = strTemp.IndexOf("(");
            if (nRet != -1)
            {
                strNo = strTemp.Substring(0, nRet).Trim();
                strComment = strTemp.Substring(nRet + 1).Trim();

                if (strComment.Length > 0 && strComment[strComment.Length - 1] == ')')
                    strComment = strComment.Substring(0, strComment.Length - 1);
            }
            else
            {
                strNo = strTemp.Trim();
                strComment = "";
            }
        }

        // չ�������ַ���
        // 1:20,2-11
        // �����׳��쳣
        static void ExpandNoSequence(string strText,
            out List<string> numbers,
            out List<string> comments,
            out List<string> copies)
        {
            try
            {
                numbers = new List<string>();
                comments = new List<string>();
                copies = new List<string>();

                string[] parts = strText.Split(new char[] { ',' });
                for (int i = 0; i < parts.Length; i++)
                {
                    string strPart = parts[i];
                    if (String.IsNullOrEmpty(strPart) == true)
                    {
                        numbers.Add(strPart);
                        comments.Add("");
                        copies.Add("");
                        continue;
                    }

                    // -
                    int nRet = strPart.IndexOf("-");
                    if (nRet != -1)
                    {
                        string strStart = strPart.Substring(0, nRet);
                        string strEnd = strPart.Substring(nRet + 1);

                        string strStartNo = "";
                        string strStartComment = "";
                        string strStartCopy = "";
                        ParseNoCommentCopy(strStart,
                            out strStartNo,
                            out strStartComment,
                            out strStartCopy);

                        string strEndNo = "";
                        string strEndComment = "";
                        string strEndCopy = "";
                        ParseNoCommentCopy(strEnd,
                            out strEndNo,
                            out strEndComment,
                            out strEndCopy);

                        string strCopy = "";
                        if (String.IsNullOrEmpty(strStartCopy) == false)
                            strCopy = strStartCopy;
                        else if (String.IsNullOrEmpty(strEndCopy) == false)
                            strCopy = strEndCopy;

                        string strComment = "";
                        if (String.IsNullOrEmpty(strStartComment) == false)
                            strComment = strStartComment;
                        else if (String.IsNullOrEmpty(strEndComment) == false)
                            strComment = strEndComment;

                        int start = Convert.ToInt32(strStartNo);
                        int end = Convert.ToInt32(strEndNo);

                        for (int j = start; j <= end; j++)
                        {
                            numbers.Add(j.ToString());
                            comments.Add(strComment);
                            copies.Add(strCopy);
                        }

                        continue;
                    }

                    // *
                    nRet = strPart.IndexOf("*");
                    if (nRet != -1)
                    {
                        string strValue = strPart.Substring(0, nRet);
                        string strCount = strPart.Substring(nRet + 1);

                        string strNo = "";
                        string strComment = "";
                        string strCopy = "";
                        ParseNoCommentCopy(strValue,
                            out strNo,
                            out strComment,
                            out strCopy);
                        int count = 1;

                        if (String.IsNullOrEmpty(strCount) == true)
                            count = 1;
                        else
                        {
                            if (StringUtil.IsPureNumber(strCount) == false)
                                throw new Exception("���� '" + strText + "' ��ʽ����ȷ: '" + strCount + "'Ӧ��Ϊ������");

                            count = Convert.ToInt32(strCount);
                        }

                        for (int j = 0; j < count; j++)
                        {
                            numbers.Add(strNo);
                            comments.Add(strComment);
                            copies.Add(strCopy);
                        }

                        continue;
                    }

                    {
                        string strNo = "";
                        string strComment = "";
                        string strCopy = "";
                        ParseNoCommentCopy(strPart,
                            out strNo,
                            out strComment,
                            out strCopy);

                        numbers.Add(strNo);
                        comments.Add(strComment);
                        copies.Add(strCopy);
                    }
                }

                Debug.Assert(numbers.Count == copies.Count, "");
            }
            catch (Exception ex)
            {
                throw new Exception("���� '" + strText + "' ��ʽ����ȷ: " + ex.Message);
            }
        }

        // չ�������ַ���
        // �����׳��쳣
        static List<string> ExpandSequence(string strText)
        {
            try
            {
                List<string> results = new List<string>();
                string[] parts = strText.Split(new char[] { ',' });
                for (int i = 0; i < parts.Length; i++)
                {
                    string strPart = parts[i];
                    if (String.IsNullOrEmpty(strPart) == true)
                    {
                        results.Add(strPart);
                        continue;
                    }

                    // -
                    int nRet = strPart.IndexOf("-");
                    if (nRet != -1)
                    {
                        string strStart = strPart.Substring(0, nRet);
                        string strEnd = strPart.Substring(nRet + 1);

                        int start = Convert.ToInt32(strStart);
                        int end = Convert.ToInt32(strEnd);

                        for (int j = start; j <= end; j++)
                        {
                            results.Add(j.ToString());
                        }

                        continue;
                    }

                    // *
                    nRet = strPart.IndexOf("*");
                    if (nRet != -1)
                    {
                        string strValue = strPart.Substring(0, nRet);
                        string strCount = strPart.Substring(nRet + 1);

                        int count = Convert.ToInt32(strCount);
                        for (int j = 0; j < count; j++)
                        {
                            results.Add(strValue);
                        }

                        continue;
                    }

                    results.Add(strPart);
                }

                return results;
            }
            catch (Exception ex)
            {
                throw new Exception("���� '"+strText+"' ��ʽ����ȷ: " + ex.Message);
            }
        }

        // ���һ�����ӵ�JidaoCell����
        int GetOneCell(DateTime day,
            out JidaoCell cell,
            out string strError)
        {
            strError = "";
            cell = null;

            JidaoYear year = this.FindYear(day.Year);
            if (year == null)
            {
                strError = "�� '" + day.Year.ToString() + "' û���ҵ�";
                return -1;
            }

            JidaoMonth month = year.FindMonth(day.Month);
            if (month == null)
            {
                strError = "�� '" + day.Year.ToString() + "' ���� '" + day.Month + "' û���ҵ�";
                return -1;
            }

            if (month.Cells.Count < (day.Day - 1))
            {
                strError = "�� '" + day.Year.ToString() + "' ���� '" + day.Month + "' ���� '" + day.Day + "' ����Cells��Χ";
                return -1;
            }

            cell = month.Cells[day.Day - 1];
            return 0;
        }

        int SetArrivedCells(
            string strStart,
            string strEnd,
            string strBitMask,
            string strYuceMask,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (strStart.Length == 6)
            {
                strStart = strStart + "01";
                Debug.Assert(this.TimeStyle == "month", "");
            }
            if (strEnd.Length == 6)
            {
                strEnd = strEnd + "01";
                Debug.Assert(this.TimeStyle == "month", "");
            }
            DateTime start = DateTimeUtil.Long8ToDateTime(strStart);
            DateTime end = DateTimeUtil.Long8ToDateTime(strEnd);

            if (start > end)
            {
                strError = "SetArrivedCells() error, start["+strStart+"] > end["+strEnd+"]";
                return -1;
            }

            Debug.Assert(start <= end, "");

            int nOffset = 0;
            for (DateTime cur = start; cur <= end; )
            {
                bool bValue = GetOneBitValue(nOffset, strBitMask);
                bool bYuceValue = GetOneBitValue(nOffset, strYuceMask);

                nRet = OnOneCell(cur,
                    bValue,
                    out strError);
                if (nRet == -1)
                    return -1;

                nRet = DisableOneCell(cur,
    bYuceValue,
    out strError);
                if (nRet == -1)
                    return -1;

                if (cur >= end)
                    break;
                nOffset++;

                if (this.TimeStyle == "day")
                    cur = cur.AddDays(1);
                else
                    cur = cur.AddMonths(1);
            }

            return 0;
        }

        // ��һ����������Ϊ���õ�״̬
        int OnOneCell(DateTime day, 
            bool bValue,
            out string strError)
        {
            strError = "";
            JidaoYear year = this.FindYear(day.Year);
            if (year == null)
            {
                strError = "�� '"+day.Year.ToString()+"' û���ҵ�";
                return -1;
            }

            JidaoMonth month = year.FindMonth(day.Month);
            if (month == null)
            {
                strError = "�� '" + day.Year.ToString() + "' ���� '"+day.Month+"' û���ҵ�";
                return -1;
            }

            if (month.Cells.Count < (day.Day-1))
            {
                strError = "�� '" + day.Year.ToString() + "' ���� '" + day.Month + "' ���� '"+day.Day+"' ����Cells��Χ";
                return -1;
            }

            JidaoCell cell = month.Cells[day.Day - 1];
            cell.On = bValue;
            return 0;
        }

        // ��һ����������Ϊ���ε�״̬
        int DisableOneCell(DateTime day,
            bool bValue,
            out string strError)
        {
            strError = "";
            JidaoYear year = this.FindYear(day.Year);
            if (year == null)
            {
                strError = "�� '" + day.Year.ToString() + "' û���ҵ�";
                return -1;
            }

            JidaoMonth month = year.FindMonth(day.Month);
            if (month == null)
            {
                strError = "�� '" + day.Year.ToString() + "' ���� '" + day.Month + "' û���ҵ�";
                return -1;
            }

            if (month.Cells.Count < (day.Day - 1))
            {
                strError = "�� '" + day.Year.ToString() + "' ���� '" + day.Month + "' ���� '" + day.Day + "' ����Cells��Χ";
                return -1;
            }

            JidaoCell cell = month.Cells[day.Day - 1];
            cell.Disable = bValue;
            return 0;
        }

        static bool GetOneBitValue(int index,
            string strBitMask)
        {
            int nChar = index / 4;
            int nOffset = index % 4;

            if (nChar >= strBitMask.Length)
                return false;

            int value = Convert.ToInt32(new string(strBitMask[nChar],1), 16);
            int mask = 0x0000008 >> nOffset;
            if ((value & mask) != 0)
                return true;
            return false;
        }

        // ȷ��ʱ�䷶Χ�㹻
        int EnsureTimeRange(
            string strSource,
            string strTimeRange,
            out string strStart,
            out string strEnd,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // ������·ݷ����ֻʹ��ÿ���µĵ�һ������

            strStart = "";
            strEnd = "";
            nRet = Global.ParseTimeRangeString(strTimeRange,
            out strStart,
            out strEnd,
            out strError);
            if (nRet == -1)
                return -1;

            if (strStart.Length == 6)
            {
                if (String.IsNullOrEmpty(this.TimeStyle) == false)
                {
                    if (this.TimeStyle != "month")
                    {
                        strError = "��ǰ��ʼ��ʱʹ�õ�ʱ���� '"+this.TimeStyle+"' ��Ŀǰ�� 'month'��ͬ";
                        return -1;
                    }
                }

                this.TimeStyle = "month";
                if (strEnd.Length != 6)
                {
                    strError = "ʱ�䷶Χ�ַ��� '" + strTimeRange + "' ��ʽ����ȷ����ʼ�ͽ���ʱ����ַ�����һ��";
                    return -1;
                }
            }
            else if (strStart.Length == 8)
            {
                if (String.IsNullOrEmpty(this.TimeStyle) == false)
                {
                    if (this.TimeStyle != "day")
                    {
                        strError = "��ǰ��ʼ��ʱʹ�õ�ʱ���� '" + this.TimeStyle + "' ��Ŀǰ�� 'day'��ͬ";
                        return -1;
                    }
                }

                this.TimeStyle = "day";
                if (strEnd.Length != 8)
                {
                    strError = "ʱ�䷶Χ�ַ��� '" + strTimeRange + "' ��ʽ����ȷ����ʼ�ͽ���ʱ����ַ�����һ��";
                    return -1;
                }
            }
            else
            {
                strError = "ʱ�䷶Χ�ַ��� '"+strTimeRange+"' ��ʽ����ȷ����ʾ��ʼ���߽���ʱ����ַ����ַ���Ӧ��Ϊ6��8�ַ�";
                return -1;
            }

            // Ϊ����������չʱ�䷶Χ��ʱ��λΪ����
            string strStartYear = strStart.Substring(0, 4);
            string strEndYear = strEnd.Substring(0, 4);

            int nStartYear = -1;
            int nEndYear = -1;

            try
            {
                nStartYear = Convert.ToInt32(strStartYear);
            }
            catch
            {
                strError = "��ʼ��� '" + strStartYear + "' ��ʽ����ȷ";
                return -1;
            }
            try
            {
                nEndYear = Convert.ToInt32(strEndYear);
            }
            catch
            {
                strError = "������� '" + strEndYear + "' ��ʽ����ȷ";
                return -1;
            }

            for (int i = nStartYear; i <= nEndYear; i++)
            {
                JidaoYear year = FindYear(i);
                if (year != null)
                    continue;

                year = new JidaoYear(i, strSource);
                this.AddYear(year);
            }

            return 0;
        }

        JidaoYear FindYear(int nYear)
        {
            for (int i = 0; i < this.Years.Count; i++)
            {
                JidaoYear year = this.Years[i];
                if (year.Year == nYear)
                    return year;
            }

            return null;
        }

        // ���������뵽�ʵ���λ��
        void AddYear(JidaoYear year)
        {
            JidaoYear prev_year = null;
            for (int i = 0; i < this.Years.Count; i++)
            {
                JidaoYear cur_year = this.Years[i];
                if (prev_year == null
                    && year.Year < cur_year.Year)
                {
                    this.Years.Insert(i, year);
                    return;
                }

                if (prev_year != null
                    && prev_year.Year <= year.Year
                    && cur_year.Year >= year.Year)
                {
                    this.Years.Insert(i, year);
                    return;
                }

                prev_year = cur_year;
            }

            // ֻ�ü�����ĩβ
            this.Years.Add(year);
        }
    }

    // ����һ����ȫ����Ϣ������
    public class JidaoDay
    {
        public string Date = "";    // 8�ַ�������

        public List<JidaoCell> Cells = new List<JidaoCell>();
    }

    public class JidaoCell
    {
        public JidaoMonth Container = null;

        public string Source = "";  // ������Դ�ֶ���

        public string No = "";
        public string Vol = "";
        public string Zong = "";

        public string OperDate = "";
        public string Operator = "";

        public bool On = false;
        public bool Disable = false;

        public int OrderCopy = 0;
        public int ArrivedCopy = -1;
        public string Comment = "";

        public JidaoCell(string strSource)
        {
            Debug.Assert(strSource.Length == 3, "");
            this.Source = strSource;
        }
    }

    public class JidaoMonth
    {
        public JidaoYear Container = null;

        public int Month = -1;

        public List<JidaoCell> Cells = new List<JidaoCell>();
    }

    public class JidaoYear
    {
        public int Year = -1;

        public List<JidaoMonth> Months = new List<JidaoMonth>();

        public JidaoYear(int nYear, string strSource)
        {
            this.Year = nYear;

            // ����ÿ����
            for (int nMonth = 1; nMonth < 13; nMonth++)
            {
                JidaoMonth month = new JidaoMonth();
                month.Container = this;
                month.Month = nMonth;
                this.Months.Add(month);

                // Ԥ����չ��Cells����
                int nDays = DateTime.DaysInMonth(nYear, nMonth);
                for (int i = 0; i < nDays; i++)
                {
                    month.Cells.Add(new JidaoCell(strSource));
                }
            }
        }

        public JidaoMonth FindMonth(int nMonth)
        {
            Debug.Assert(nMonth >= 1 && nMonth <= 12, "");
            // �·ݶ�����������£�����ȡ��
            if (this.Months.Count == 12 && this.Months[0].Month == 1)
                return this.Months[nMonth - 1];

            // �·ݲ������������
            for (int i = 0; i < this.Months.Count; i++)
            {
                JidaoMonth month = this.Months[i];
                if (month.Month == nMonth)
                    return month;
            }

            return null;
        }
    }
}
