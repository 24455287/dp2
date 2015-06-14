using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System.Globalization;

using DigitalPlatform.Text;
using System.Xml;
using System.Web.UI;
using System.IO;


namespace DigitalPlatform.dp2.Statis
{
    // �����ʽ������йص���

    /*
	// ����������
	public enum DataType
	{
		Auto = 0,
		String = 1,
		Number = 2,
		Price = 3,	// 100���������
	}
	*/

    // һ���еĸ�ʽ�������
    public class PrintColumn
    {
        public string Title = "";	// �б���

        string m_strCssClass = "";

        // CSS��ʽ�ࡣ2007/5/18
        public string CssClass
        {
            get
            {
#if NO
                if (String.IsNullOrEmpty(m_strCssClass) == true)
                    return Title;   // ���û������css�࣬�����б���������
#endif
                return m_strCssClass;
            }
            set
            {
                m_strCssClass = value;
            }
        }

        public bool Hidden = false;	// ���Ƿ������ʱ����
        public int ColumnNumber = -2;	// ��table�е���ʵ�к� -1 �����б���

        public string DefaultValue = "";	// ����ȱʡֵ

        public bool Sum = false;	// �����Ƿ���Ҫ���ϼơ�

        public DataType DataType = DataType.Auto;

        public int Width = -1;	// �п��

        public int Colspan = 1; // 2013/6/14

        public string Eval = "";    // 2014/6/1
    }

    // ����ʵ������һ����ʽ�����������
    public class Report : ArrayList
    {
        public event OutputLineEventHandler OutputLine = null;
        public event SumCellEventHandler SumCell = null;

        public bool SumLine = false;	// �Ƿ���Ҫ����� ��� �ϼ���


        public new PrintColumn this[int nIndex]
        {
            get
            {
                return (PrintColumn)base[nIndex];
            }
            set
            {
                base[nIndex] = value;
            }
        }



        // ����һ�������ȱʡ���Դ���һ��Report����
        // parameters:
        //		strDefaultValue	ȫ���е�ȱʡֵ
        //				null��ʾ���ı�ȱʡֵ""������ΪstrDefaultValueָ����ֵ
        //		bSum	�Ƿ�ȫ���ж�Ҫ�μӺϼ�
        //      bContentColumn  �Ƿ����������б�ָ������Ŀ���������Ŀ
        public static Report BuildReport(Table table,
            string strColumnTitles,
            string strDefaultValue,
            bool bSum,
            bool bContentColumn = true)
        {
            // Debug.Assert(false, "");
            if (table.Count == 0)
                return null;	// �޷����������ݱ�������һ������

            Report report = new Report();

            Line line = table.FirstHashLine();	// ���õ�һ�С�������Ҫ��table�Ź���

            // �б���
            {
                PrintColumn column = new PrintColumn();
                column.ColumnNumber = -1;
                report.Add(column);
            }

            int nTitleCount = 0;

            if (strColumnTitles != null)
            {
                string[] aName = strColumnTitles.Split(new Char[] { ',' });
                nTitleCount = aName.Length;
            }

            int nColumnCount = nTitleCount;
            if (bContentColumn == true)
                nColumnCount = Math.Max(line.Count + 1, nTitleCount);


            // ������һ��
            // ��Ϊ�б���column�Ѿ����룬��������������nTitleCount-1��
            for (int i = 0; i < nColumnCount - 1; i++)
            {
                PrintColumn column = new PrintColumn();
                column.ColumnNumber = i;

                if (strDefaultValue != null)
                    column.DefaultValue = strDefaultValue;

                column.Sum = bSum;

                report.Add(column);
            }


            // ����б���
            if (strColumnTitles != null)
            {
                string[] aName = strColumnTitles.Split(new Char[] { ',' });

                /*
                if (aName.Length < report.Count)
                {
                    string strError = "�ж��� '" + strColumnTitles + "' �е����� " + aName.Length.ToString() + "С�ڱ���ʵ��������� " + report.Count.ToString();
                    throw new Exception(strError);
                }*/


                int j = 0;
                for (j = 0; j < report.Count; j++)
                {
                    // 2007/10/26
                    if (j >= aName.Length)
                        break;

                    string strText = "";

                    strText = aName[j];

                    string strNameText = "";
                    string strNameClass = "";

                    int nRet = strText.IndexOf("||");
                    if (nRet == -1)
                        strNameText = strText;
                    else
                    {
                        strNameText = strText.Substring(0, nRet);
                        strNameClass = strText.Substring(nRet + 2);
                    }


                    PrintColumn column = (PrintColumn)report[j];
                    if (j < aName.Length)
                    {
                        column.Title = strNameText;
                        column.CssClass = strNameClass;
                    }
                }
            }

            report.SumLine = bSum;

            // ���� colspan
            PrintColumn current = null;
            foreach (PrintColumn column in report)
            {
                if (string.IsNullOrEmpty(column.Title) == false
                    && column.Title[0] == '+'
                    && current != null)
                {
                    column.Colspan = 0; // ��ʾ����һ����������
                    current.Colspan++;
                }
                else
                    current = column;
            }

            return report;
        }

        static int ParseLines(string strInnerXml,
            out List<string> lines,
            out string strError)
        {
            lines = new List<string>();
            strError = "";

            XmlDocument dom = new XmlDocument();
            // dom.LoadXml("<root />");
            dom.AppendChild(dom.CreateElement("root"));

            try
            {
                dom.DocumentElement.InnerXml = strInnerXml;
            }
            catch (Exception ex)
            {
                strError = "InnerXml װ��ʱ����: " + ex.Message;
                return -1;
            }

            // TODO: ֻ�� <br /> �ŷָ���������Ҫ����һƬ
            foreach (XmlNode node in dom.DocumentElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Text)
                {
                    lines.Add(node.InnerText);
                }
            }

            return 0;
        }

        // RML ��ʽת��Ϊ Excel �ļ�
        public static int RmlToExcel(string strRmlFileName,
    string strExcelFileName,
    out string strError)
        {
            strError = "";
            int nRet = 0;

            Stream stream = File.OpenRead(strRmlFileName);

            using (XmlTextReader reader = new XmlTextReader(stream))
            {
                while (true)
                {
                    bool bRet = reader.Read();
                    if (bRet == false)
                    {
                        strError = "�ļ� " + strRmlFileName + " û�и�Ԫ��";
                        return -1;
                    }
                    if (reader.NodeType == XmlNodeType.Element)
                        break;
                }

                ExcelDocument doc = ExcelDocument.Create(strExcelFileName);
                try
                {
                    doc.NewSheet("Sheet1");

                    int nColIndex = 0;
                    int _lineIndex = 0;

                    string strTitle = "";
                    string strComment = "";
                    string strCreateTime = "";
                    // string strCss = "";
                    List<ColumnStyle> col_defs = null;

                    while (true)
                    {
                        bool bRet = reader.Read();
                        if (bRet == false)
                            break;
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name == "title")
                            {
                                strTitle = reader.ReadInnerXml();
                            }
                            else if (reader.Name == "comment")
                            {
                                strComment = reader.ReadInnerXml();
                            }
                            else if (reader.Name == "createTime")
                            {
                                strCreateTime = reader.ReadElementContentAsString();
                            }
                            else if (reader.Name == "style")
                            {
                                // strCss = reader.ReadElementContentAsString();
                            }
                            else if (reader.Name == "columns")
                            {
                                // �� RML �ļ��ж��� <columns> Ԫ��
                                nRet = ReadColumnStyle(reader,
            out col_defs,
            out strError);
                                if (nRet == -1)
                                {
                                    strError = "ReadColumnStyle() error : " + strError;
                                    return -1;
                                }

                            }
                            else if (reader.Name == "table")
                            {
                                List<string> lines = null;

                                nRet = ParseLines(strTitle,
           out lines,
           out strError);
                                if (nRet == -1)
                                {
                                    strError = "���� title ���� '" + strTitle + "' ʱ��������: " + strError;
                                    return -1;
                                }

                                // �����������
                                nColIndex = 0;
                                foreach (string t in lines)
                                {
                                    List<CellData> cells = new List<CellData>();
                                    cells.Add(new CellData(nColIndex, t));
                                    doc.WriteExcelLine(_lineIndex, cells);
                                    _lineIndex++;
                                }

                                nRet = ParseLines(strComment,
out lines,
out strError);
                                if (nRet == -1)
                                {
                                    strError = "���� comment ���� '" + strTitle + "' ʱ��������: " + strError;
                                    return -1;
                                }
                                nColIndex = 0;
                                foreach (string t in lines)
                                {
                                    List<CellData> cells = new List<CellData>();
                                    cells.Add(new CellData(nColIndex, t));
                                    doc.WriteExcelLine(_lineIndex, cells);

                                    _lineIndex++;
                                }

                                // ����
                                _lineIndex++;

                            }
                            else if (reader.Name == "tr")
                            {
                                // ���һ��
                                List<CellData> cells = null;
                                nRet = ReadLine(reader,
                                    col_defs,
            out cells,
            out strError);
                                if (nRet == -1)
                                {
                                    strError = "ReadLine error : " + strError;
                                    return -1;
                                }
                                doc.WriteExcelLine(_lineIndex, cells, WriteExcelLineStyle.None);
                                _lineIndex++;
                            }
                        }
                    }

                    // create time
                    {
                        _lineIndex++;
                        List<CellData> cells = new List<CellData>();
                        cells.Add(new CellData(0, "����ʱ��"));
                        cells.Add(new CellData(1, strCreateTime));
                        doc.WriteExcelLine(_lineIndex, cells);

                        _lineIndex++;
                    }

                }
                finally
                {
                    doc.SaveWorksheet();
                    doc.Close();
                }
            }

            return 0;
        }

        // �� RML �ļ��ж��� <tr> Ԫ��
        static int ReadLine(XmlTextReader reader,
            List<ColumnStyle> col_defs,
            out List<CellData> cells,
            out string strError)
        {
            strError = "";
            cells = new List<CellData>();
            int col_index = 0;

            int nColIndex = 0;
            while (true)
            {
                if (reader.Read() == false)
                    break;
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "th" || reader.Name == "td")
                    {
                        string strText = reader.ReadElementContentAsString();

                        CellData new_cell = null;

                        string strType = "";

                        // 2014/8/16
                        if (col_defs != null
                            && col_index < col_defs.Count)
                            strType = col_defs[col_index].Type;

                        if (strType == "String")
                            new_cell = new CellData(nColIndex++, strText, true, 0);
                        else if (strType == "Number")
                        {
                            new_cell = new CellData(nColIndex++, strText, false, 0);
                        }
                        else // "Auto")
                        {
                            bool isString = !IsExcelNumber(strText);

                            new_cell = new CellData(nColIndex++, strText, isString, 0);
                        }

                        cells.Add(new_cell);

                        col_index++;
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement
    && reader.Name == "tr")
                    break;
            }

            return 0;
        }

        // ����ַ����Ƿ�Ϊ������(ǰ����԰���һ��'-'��)
        public static bool IsExcelNumber(string s)
        {
            if (string.IsNullOrEmpty(s) == true)
                return false;

            bool bFoundNumber = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '-' && bFoundNumber == false)
                {
                    continue;
                }
                if (s[i] == '%' && i == s.Length - 1)
                {
                    // ��ĩһ���ַ�Ϊ %
                    continue;
                }
                if (s[i] > '9' || s[i] < '0')
                    return false;
                bFoundNumber = true;
            }
            return true;
        }

        class ColumnStyle
        {
            public string Class = "";
            public string Align = "";   // left/center/right
            public string Type = "";    // String/Currency/Auto/Number
        }

        // �� RML �ļ��ж��� <columns> Ԫ��
        static int ReadColumnStyle(XmlTextReader reader,
            out List<ColumnStyle> styles,
            out string strError)
        {
            strError = "";
            styles = new List<ColumnStyle>();

            while (true)
            {
                if (reader.Read() == false)
                    break;
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "column")
                    {
                        ColumnStyle style = new ColumnStyle();
                        style.Align = reader.GetAttribute("align");
                        style.Class = reader.GetAttribute("class");
                        style.Type = reader.GetAttribute("type");
                        styles.Add(style);
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement
    && reader.Name == "columns")
                    break;
            }

            return 0;
        }

#if NO
        // �ٶ���ǰ node ����� element ÿ��������̫��
        static void DumpNode(XmlTextReader reader,
            XmlWriter writer)
        {
            string strName = reader.Name;
            while (true)
            {
                if (reader.Read() == false)
                    break;
                if (reader.NodeType == XmlNodeType.Element)
                    writer.WriteRaw(reader.ReadOuterXml());

                if (reader.NodeType == XmlNodeType.EndElement
    && reader.Name == strName)
                    break;
            }
        }
#endif

        // RML ��ʽת��Ϊ HTML �ļ�
        // parameters:
        //      strCssTemplate  CSS ģ�塣���� %columns% ������е���ʽ
        public static int RmlToHtml(string strRmlFileName,
            string strHtmlFileName,
            string strCssTemplate,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            Stream stream = File.OpenRead(strRmlFileName);

            using (XmlTextReader reader = new XmlTextReader(stream))
            {
                while (true)
                {
                    bool bRet = reader.Read();
                    if (bRet == false)
                    {
                        strError = "�ļ� "+strRmlFileName+" û�и�Ԫ��";
                        return -1;
                    }
                    if (reader.NodeType == XmlNodeType.Element)
                        break;
                }

                using (XmlWriter writer = XmlWriter.Create(strHtmlFileName, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                {
                    writer.WriteDocType("html", "-//W3C//DTD XHTML 1.0 Transitional//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", null);
                    writer.WriteStartElement("html", "http://www.w3.org/1999/xhtml");
                    // writer.WriteAttributeString("xml", "lang", "", "en");

                    string strTitle = "";
                    string strComment = "";
                    string strCreateTime = "";
                    string strCss = "";
                    List<ColumnStyle> styles = null;

                    while (true)
                    {
                        bool bRet = reader.Read();
                        if (bRet == false)
                            break;
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name == "title")
                            {
                                strTitle = reader.ReadInnerXml();
                            }
                            else if (reader.Name == "comment")
                            {
                                strComment = reader.ReadInnerXml();
                            }
                            else if (reader.Name == "createTime")
                            {
                                strCreateTime = reader.ReadElementContentAsString();
                            }
                            else if (reader.Name == "style")
                            {
                                strCss = reader.ReadElementContentAsString();
                            }
                            else if (reader.Name == "columns")
                            {
                                    // �� RML �ļ��ж��� <columns> Ԫ��
                                nRet = ReadColumnStyle(reader,
            out styles,
            out strError);
                                if (nRet == -1)
                                {
                                    strError = "ReadColumnStyle() error : " + strError;
                                    return -1;
                                }

                            }
                            else if (reader.Name == "table")
                            {

                                writer.WriteStartElement("head");

                                writer.WriteStartElement("meta");
                                writer.WriteAttributeString("http-equiv", "Content-Type");
                                writer.WriteAttributeString("content", "text/html; charset=utf-8");
                                writer.WriteEndElement();

                                // title
                                {
                                    writer.WriteStartElement("title");
                                    // TODO �����ʱ��ֱ���γ� lines
                                    writer.WriteString(strTitle.Replace("<br />", " ").Replace("<br/>", " "));
                                    writer.WriteEndElement();
                                }

                                // css
                                if (string.IsNullOrEmpty(strCss) == false)
                                {
                                    writer.WriteStartElement("style");
                                    writer.WriteAttributeString("media", "screen");
                                    writer.WriteAttributeString("type", "text/css");
                                    writer.WriteString(strCss);
                                    writer.WriteEndElement();
                                }

                                // CSS ģ��
                                else if (string.IsNullOrEmpty(strCssTemplate) == false)
                                {
                                    StringBuilder text = new StringBuilder();
                                    foreach (ColumnStyle style in styles)
                                    {
                                        string strAlign = style.Align;
                                        if (string.IsNullOrEmpty(strAlign) == true)
                                            strAlign = "left";
                                        text.Append("TABLE.table ."+style.Class+" {"
                                            + "text-align: "+strAlign+"; }\r\n");
                                    }

                                    writer.WriteStartElement("style");
                                    writer.WriteAttributeString("media", "screen");
                                    writer.WriteAttributeString("type", "text/css");
                                    writer.WriteString("\r\n" + strCssTemplate.Replace("%columns%", text.ToString()) + "\r\n");
                                    writer.WriteEndElement();
                                }

                                writer.WriteEndElement();   // </head>

                                writer.WriteStartElement("body");

                                if (string.IsNullOrEmpty(strTitle) == false)
                                {
                                    writer.WriteStartElement("div");
                                    writer.WriteAttributeString("class", "tabletitle");
                                    writer.WriteRaw(strTitle);
                                    writer.WriteEndElement();
                                }

                                if (string.IsNullOrEmpty(strComment) == false)
                                {
                                    writer.WriteStartElement("div");
                                    writer.WriteAttributeString("class", "titlecomment");
                                    writer.WriteRaw(strComment);
                                    writer.WriteEndElement();
                                }


                                // writer.WriteRaw(reader.ReadOuterXml());
                                // DumpNode(reader, writer);
                                writer.WriteNode(reader, true);

                                {
                                    writer.WriteStartElement("div");
                                    writer.WriteAttributeString("class", "createtime");
                                    writer.WriteString("����ʱ��: " + strCreateTime);
                                    writer.WriteEndElement();
                                }

                                writer.WriteEndElement();   // </body>
                            }
                        }
                    }

                    writer.WriteEndElement();   // </html>
                }
            }

            return 0;
        }

        static Jurassic.ScriptEngine engine = null;


        // ��� RML ��ʽ�ı��
        // ����������д�� <table> Ԫ��
        // parameters:
        //      nTopLines   ����Ԥ��������
        public void OutputRmlTable(Table table,
            XmlTextWriter writer,
            int nMaxLines = -1)
        {
            // StringBuilder strResult = new StringBuilder(4096);
            int i, j;

            if (nMaxLines == -1)
                nMaxLines = table.Count;

            writer.WriteStartElement("table");
            writer.WriteAttributeString("class", "table");

            writer.WriteStartElement("thead");
            writer.WriteStartElement("tr");

            int nEvalCount = 0; // ���� eval ����Ŀ����
            for (j = 0; j < this.Count; j++)
            {
                PrintColumn column = (PrintColumn)this[j];
                if (column.Colspan == 0)
                    continue;

                if (string.IsNullOrEmpty(column.Eval) == false)
                    nEvalCount++;

                writer.WriteStartElement("th");
                if (string.IsNullOrEmpty(column.CssClass) == false)
                    writer.WriteAttributeString("class", column.CssClass);
                if (column.Colspan > 1)
                    writer.WriteAttributeString("colspan", column.Colspan.ToString());

                writer.WriteString(column.Title);
                writer.WriteEndElement();   // </th>
            }

            writer.WriteEndElement();   // </tr>
            writer.WriteEndElement();   // </thead>


            // �ϼ�����
            object[] sums = null;   // 2008/12/1 new changed

            if (this.SumLine)
            {
                sums = new object[this.Count];
                for (i = 0; i < sums.Length; i++)
                {
                    sums[i] = null;
                }
            }

            NumberFormatInfo nfi = new CultureInfo("zh-CN", false).NumberFormat;
            nfi.NumberDecimalDigits = 2;

            writer.WriteStartElement("tbody");

            // Jurassic.ScriptEngine engine = null;
            if (nEvalCount > 0 && engine == null)
            {
                engine = new Jurassic.ScriptEngine();
                engine.EnableExposedClrTypes = true;
            }

            // ������ѭ��
            for (i = 0; i < Math.Min(nMaxLines, table.Count); i++)
            {
                Line line = table[i];

                if (engine != null)
                    engine.SetGlobalValue("line", line);

                string strLineCssClass = "content";
                if (this.OutputLine != null)
                {
                    OutputLineEventArgs e = new OutputLineEventArgs();
                    e.Line = line;
                    e.Index = i;
                    e.LineCssClass = strLineCssClass;
                    this.OutputLine(this, e);
                    if (e.Output == false)
                        continue;

                    strLineCssClass = e.LineCssClass;
                }

                // strResult.Append("<tr class='" + strLineCssClass + "'>\r\n");
                writer.WriteStartElement("tr");
                writer.WriteAttributeString("class", strLineCssClass);

                // ��ѭ��
                for (j = 0; j < this.Count; j++)
                {
                    PrintColumn column = (PrintColumn)this[j];

                    if (column.ColumnNumber < -1)
                    {
                        throw (new Exception("PrintColumn����ColumnNumber����δ��ʼ����λ��" + Convert.ToString(j)));
                    }

                    string strText = "";
                    if (column.ColumnNumber != -1)
                    {
                        if (string.IsNullOrEmpty(column.Eval) == false)
                        {
                            // engine.SetGlobalValue("cell", line.GetObject(column.ColumnNumber));
                            strText = engine.Evaluate(column.Eval).ToString();
                        }
                        else if (column.DataType == DataType.PriceDouble)
                        {
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;
                            else
                            {
                                double v = line.GetDouble(column.ColumnNumber);
                                /*
                                NumberFormatInfo provider = new NumberFormatInfo();
                                provider.NumberDecimalDigits = 2;
                                provider.NumberGroupSeparator = ".";
                                provider.NumberGroupSizes = new int[] { 3 };
                                strText = Convert.ToString(v, provider);
                                 * */
                                strText = v.ToString("N", nfi);
                            }
                        }
                        else if (column.DataType == DataType.PriceDecimal)
                        {
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;
                            else
                            {
                                decimal v = line.GetDecimal(column.ColumnNumber);
                                strText = v.ToString("N", nfi);
                            }
                        }
                        else if (column.DataType == DataType.PriceDecimal)
                        {
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;
                            else
                            {
                                decimal v = line.GetDecimal(column.ColumnNumber);
                                strText = v.ToString("N", nfi);
                            }
                        }
                        else if (column.DataType == DataType.Price)
                        {
                            // Debug.Assert(false, "");
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;	// 2005/5/26
                            else
                                strText = line.GetPriceString(column.ColumnNumber);
                        }
                        else
                            strText = line.GetString(column.ColumnNumber, column.DefaultValue);
                    }
                    else
                    {
                        strText = line.Entry;
                    }

                    writer.WriteStartElement(j == 0 ? "th" : "td");
                    if (string.IsNullOrEmpty(column.CssClass) == false)
                        writer.WriteAttributeString("class", column.CssClass);
                    writer.WriteString(strText);
                    writer.WriteEndElement();   // </td>

                    if (this.SumLine == true
                        && column.Sum == true
                        && column.ColumnNumber != -1)
                    {
                        try
                        {
                            // if (column.DataType != DataType.Currency)
                            {
                                object v = line.GetObject(column.ColumnNumber);
                                if (this.SumCell != null)
                                {
                                    SumCellEventArgs e = new SumCellEventArgs();
                                    e.DataType = column.DataType;
                                    e.ColumnNumber = column.ColumnNumber;
                                    e.LineIndex = i;
                                    e.Line = line;
                                    e.Value = v;
                                    this.SumCell(this, e);
                                    if (e.Value == null)
                                        continue;

                                    v = e.Value;
                                }

                                if (sums[j] == null)
                                    sums[j] = v;
                                else
                                {
                                    sums[j] = AddValue(column.DataType,
            sums[j],
            v);
                                    // sums[j] = ((decimal)sums[j]) + v;
                                }
                            }
                        }
                        catch (Exception ex)	// ����������ַ���ת��Ϊ�����׳����쳣
                        {
                            throw new Exception("���ۼ� �� " + i.ToString() + " �� " + column.ColumnNumber.ToString() + " ֵ��ʱ���׳��쳣: " + ex.Message);
                        }
                    }
                }

                // strResult.Append("</tr>\r\n");
                writer.WriteEndElement();   // </tr>
            }

            writer.WriteEndElement();   // </tbody>

            if (this.SumLine == true)
            {
                Line sum_line = null;
                if (engine != null)
                {
                    // ׼�� Line ����
                    sum_line = new Line(0);
                    for (j = 1; j < this.Count; j++)
                    {
                        PrintColumn column = (PrintColumn)this[j];
                        if (column.Sum == true
                            && sums[j] != null)
                        {
                            sum_line.SetValue(j-1, sums[j]);
                        }
                    }
                    engine.SetGlobalValue("line", sum_line);
                }

                // strResult.Append("<tr class='sum'>\r\n");
                writer.WriteStartElement("tfoot");
                writer.WriteStartElement("tr");
                writer.WriteAttributeString("class", "sum");

                for (j = 0; j < this.Count; j++)
                {
                    PrintColumn column = (PrintColumn)this[j];
                    string strText = "";

                    if (j == 0)
                        strText = "�ϼ�";
                    else if (string.IsNullOrEmpty(column.Eval) == false)
                    {
                        strText = engine.Evaluate(column.Eval).ToString();
                    }
                    else if (column.Sum == true
                        && sums[j] != null)
                    {
                        if (column.DataType == DataType.PriceDouble)
                            strText = ((double)sums[j]).ToString("N", nfi);
                        else if (column.DataType == DataType.PriceDecimal)
                            strText = ((decimal)sums[j]).ToString("N", nfi);
                        else if (column.DataType == DataType.Price)
                        {
                            strText = StatisUtil.Int64ToPrice((Int64)sums[j]);
                        }
                        else
                            strText = Convert.ToString(sums[j]);

                        if (column.DataType == DataType.Currency)
                        {
                            string strSomPrice = "";
                            string strError = "";
                            // ���ܼ۸�
                            int nRet = PriceUtil.SumPrices(strText,
            out strSomPrice,
            out strError);
                            if (nRet == -1)
                                strText = strError;
                            else
                                strText = strSomPrice;
                        }
                    }
                    else
                        strText = column.DefaultValue;  //  "&nbsp;";

#if NO
                    doc.WriteExcelCell(
    _lineIndex,
    j,
    strText,
    true);
#endif
                    writer.WriteStartElement(j == 0? "th" : "td");
                    if (string.IsNullOrEmpty(column.CssClass) == false)
                        writer.WriteAttributeString("class", column.CssClass);
                    writer.WriteString(strText);
                    writer.WriteEndElement();   // </td>
                }

                // strResult.Append("</tr>\r\n");
                writer.WriteEndElement();   // </tr>
                writer.WriteEndElement();   // </tfoot>
            }

            writer.WriteEndElement();   // </table>
        }

        // ��� Excel ��ʽ�ı��
        // parameters:
        //      nTopLines   ����Ԥ��������
        public void OutputExcelTable(Table table,
            ExcelDocument doc,
            int nTopLines,
            int nMaxLines = -1)
        {
            // StringBuilder strResult = new StringBuilder(4096);
            int i, j;

            if (nMaxLines == -1)
                nMaxLines = table.Count;

            int _lineIndex = nTopLines;

            // ������
            // strResult.Append("<tr class='column'>\r\n");

            int nColIndex = 0;
            for (j = 0; j < this.Count; j++)
            {
                PrintColumn column = (PrintColumn)this[j];
                if (column.Colspan == 0)
                    continue;

                if (column.Colspan > 1)
                {
                    doc.WriteExcelTitle(_lineIndex,
            nColIndex,
            column.Colspan,
            column.Title);
#if NO
                    cells.Add(new CellData(nColIndex, column.Title));
#endif
                    nColIndex += column.Colspan;
                }
                else
                {
                    doc.WriteExcelCell(
_lineIndex,
nColIndex++,
column.Title,
true);
#if NO
                    cells.Add(new CellData(nColIndex, column.Title));
#endif
                }
            }

#if NO
            if (cells.Count > 0)
                doc.WriteExcelLine(_lineIndex, cells);
#endif

            // �ϼ�����
            object[] sums = null;   // 2008/12/1 new changed

            if (this.SumLine)
            {
                sums = new object[this.Count];
                for (i = 0; i < sums.Length; i++)
                {
                    sums[i] = null;
                }
            }

            NumberFormatInfo nfi = new CultureInfo("zh-CN", false).NumberFormat;
            nfi.NumberDecimalDigits = 2;

            // ������ѭ��
            for (i = 0; i < Math.Min(nMaxLines, table.Count); i++)
            {
                Line line = table[i];

                string strLineCssClass = "content";
                if (this.OutputLine != null)
                {
                    OutputLineEventArgs e = new OutputLineEventArgs();
                    e.Line = line;
                    e.Index = i;
                    e.LineCssClass = strLineCssClass;
                    this.OutputLine(this, e);
                    if (e.Output == false)
                        continue;

                    strLineCssClass = e.LineCssClass;
                }

                // strResult.Append("<tr class='" + strLineCssClass + "'>\r\n");
                _lineIndex++;

                List<CellData> cells = new List<CellData>();

                // ��ѭ��
                for (j = 0; j < this.Count; j++)
                {

                    PrintColumn column = (PrintColumn)this[j];

                    if (column.ColumnNumber < -1)
                    {
                        throw (new Exception("PrintColumn����ColumnNumber����δ��ʼ����λ��" + Convert.ToString(j)));
                    }


                    string strText = "";
                    if (column.ColumnNumber != -1)
                    {
                        if (column.DataType == DataType.PriceDouble)
                        {
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;
                            else
                            {
                                double v = line.GetDouble(column.ColumnNumber);
                                /*
                                NumberFormatInfo provider = new NumberFormatInfo();
                                provider.NumberDecimalDigits = 2;
                                provider.NumberGroupSeparator = ".";
                                provider.NumberGroupSizes = new int[] { 3 };
                                strText = Convert.ToString(v, provider);
                                 * */
                                strText = v.ToString("N", nfi);
                            }
                        }
                        else if (column.DataType == DataType.PriceDecimal)
                        {
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;
                            else
                            {
                                decimal v = line.GetDecimal(column.ColumnNumber);
                                strText = v.ToString("N", nfi);
                            }
                        }
                        else if (column.DataType == DataType.PriceDecimal)
                        {
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;
                            else
                            {
                                decimal v = line.GetDecimal(column.ColumnNumber);
                                strText = v.ToString("N", nfi);
                            }
                        }
                        else if (column.DataType == DataType.Price)
                        {
                            // Debug.Assert(false, "");
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;	// 2005/5/26
                            else
                                strText = line.GetPriceString(column.ColumnNumber);
                        }
                        else
                            strText = line.GetString(column.ColumnNumber, column.DefaultValue);
                    }
                    else
                    {
                        strText = line.Entry;
                    }

#if NO
                    doc.WriteExcelCell(
    _lineIndex,
    j,
    strText,
    true);
#endif
                    cells.Add(new CellData(j, strText));

                    if (this.SumLine == true
                        && column.Sum == true
                        && column.ColumnNumber != -1)
                    {
                        try
                        {
                            // if (column.DataType != DataType.Currency)
                            {
                                object v = line.GetObject(column.ColumnNumber);
                                if (this.SumCell != null)
                                {
                                    SumCellEventArgs e = new SumCellEventArgs();
                                    e.DataType = column.DataType;
                                    e.ColumnNumber = column.ColumnNumber;
                                    e.LineIndex = i;
                                    e.Line = line;
                                    e.Value = v;
                                    this.SumCell(this, e);
                                    if (e.Value == null)
                                        continue;

                                    v = e.Value;
                                }

                                if (sums[j] == null)
                                    sums[j] = v;
                                else
                                {
                                    sums[j] = AddValue(column.DataType,
            sums[j],
            v);
                                    // sums[j] = ((decimal)sums[j]) + v;
                                }
                            }
                            /*
                        else
                        {
                            string v = line.GetString(column.ColumnNumber);
                            if (this.SumCell != null)
                            {
                                SumCellEventArgs e = new SumCellEventArgs();
                                e.DataType = column.DataType;
                                e.ColumnNumber = column.ColumnNumber;
                                e.LineIndex = i;
                                e.Line = line;
                                e.Value = v;
                                this.SumCell(this, e);
                                if (e.Value == null)
                                    continue;
                                v = (string)e.Value;
                            }
                            sums[j] = PriceUtil.JoinPriceString((string)sums[j],
                                v);
                        }
                             * */
                        }
                        catch (Exception ex)	// ����������ַ���ת��Ϊ�����׳����쳣
                        {
                            throw new Exception("���ۼ� �� " + i.ToString() + " �� " + column.ColumnNumber.ToString() + " ֵ��ʱ���׳��쳣: " + ex.Message);
                        }
                    }


                }

                // strResult.Append("</tr>\r\n");
                doc.WriteExcelLine(_lineIndex, cells);
            }

            if (this.SumLine == true)
            {
                // strResult.Append("<tr class='sum'>\r\n");
                _lineIndex++;
                List<CellData> cells = new List<CellData>();
                for (j = 0; j < this.Count; j++)
                {
                    PrintColumn column = (PrintColumn)this[j];
                    string strText = "";

                    if (j == 0)
                        strText = "�ϼ�";
                    else if (column.Sum == true
                        && sums[j] != null)
                    {
                        if (column.DataType == DataType.PriceDouble)
                            strText = ((double)sums[j]).ToString("N", nfi);
                        else if (column.DataType == DataType.PriceDecimal)
                            strText = ((decimal)sums[j]).ToString("N", nfi);
                        else if (column.DataType == DataType.Price)
                        {
                            strText = StatisUtil.Int64ToPrice((Int64)sums[j]);
                        }
                        else
                            strText = Convert.ToString(sums[j]);

                        if (column.DataType == DataType.Currency)
                        {
                            string strSomPrice = "";
                            string strError = "";
                            // ���ܼ۸�
                            int nRet = PriceUtil.SumPrices(strText,
            out strSomPrice,
            out strError);
                            if (nRet == -1)
                                strText = strError;
                            else
                                strText = strSomPrice;
                        }
                    }
                    else
                        strText = column.DefaultValue;  //  "&nbsp;";

#if NO
                    doc.WriteExcelCell(
    _lineIndex,
    j,
    strText,
    true);
#endif
                    cells.Add(new CellData(j, strText));
                    
                }

                // strResult.Append("</tr>\r\n");
                doc.WriteExcelLine(_lineIndex, cells);
            }
        }

        // ���html��ʽ�ı��
        public string HtmlTable(Table table,
            int nMaxLines = -1)
        {
            StringBuilder strResult = new StringBuilder(4096);
            int i, j;

            if (nMaxLines == -1)
                nMaxLines = table.Count;

            strResult.Append("<table class='table'>\r\n");    //  border='0' bgcolor=Gainsboro cellspacing='2' cellpadding='2'

            // ������
            strResult.Append("<tr class='column'>\r\n");

            for (j = 0; j < this.Count; j++)
            {
                PrintColumn column = (PrintColumn)this[j];
                if (column.Colspan == 0)
                    continue;

                string strText = column.Title;
                string strColspan = "";
                if (column.Colspan > 1)
                    strColspan = " colspan='" + column.Colspan.ToString() + "' ";
                strResult.Append( "<td class='"
                    + column.CssClass
                    + "'"
                    + strColspan + ">" + strText + "</td>\r\n");
            }

            strResult.Append( "</tr>\r\n");

            // �ϼ�����
            object[] sums = null;   // 2008/12/1 new changed

            if (this.SumLine)
            {
                sums = new object[this.Count];
                for (i = 0; i < sums.Length; i++)
                {
                    sums[i] = null;
                }
            }

            NumberFormatInfo nfi = new CultureInfo("zh-CN", false).NumberFormat;
            nfi.NumberDecimalDigits = 2;

            // ������ѭ��
            for (i = 0; i < Math.Min(nMaxLines, table.Count); i++)
            {
                Line line = table[i];

                string strLineCssClass = "content";
                if (this.OutputLine != null)
                {
                    OutputLineEventArgs e = new OutputLineEventArgs();
                    e.Line = line;
                    e.Index = i;
                    e.LineCssClass = strLineCssClass;
                    this.OutputLine(this, e);
                    if (e.Output == false)
                        continue;

                    strLineCssClass = e.LineCssClass;
                }

                strResult.Append("<tr class='" + strLineCssClass + "'>\r\n");

                // ��ѭ��
                for (j = 0; j < this.Count; j++)
                {

                    PrintColumn column = (PrintColumn)this[j];

                    if (column.ColumnNumber < -1)
                    {
                        throw (new Exception("PrintColumn����ColumnNumber����δ��ʼ����λ��" + Convert.ToString(j)));
                    }


                    string strText = "";
                    if (column.ColumnNumber != -1)
                    {
                        if (column.DataType == DataType.PriceDouble)
                        {
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;
                            else
                            {
                                double v = line.GetDouble(column.ColumnNumber);
                                /*
                                NumberFormatInfo provider = new NumberFormatInfo();
                                provider.NumberDecimalDigits = 2;
                                provider.NumberGroupSeparator = ".";
                                provider.NumberGroupSizes = new int[] { 3 };
                                strText = Convert.ToString(v, provider);
                                 * */
                                strText = v.ToString("N", nfi);
                            }
                        }
                        else if (column.DataType == DataType.PriceDecimal)
                        {
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;
                            else
                            {
                                decimal v = line.GetDecimal(column.ColumnNumber);
                                strText = v.ToString("N", nfi);
                            }
                        }
                        else if (column.DataType == DataType.PriceDecimal)
                        {
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;
                            else
                            {
                                decimal v = line.GetDecimal(column.ColumnNumber);
                                strText = v.ToString("N", nfi);
                            }
                        }
                        else if (column.DataType == DataType.Price)
                        {
                            // Debug.Assert(false, "");
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;	// 2005/5/26
                            else
                                strText = line.GetPriceString(column.ColumnNumber);
                        }
                        else
                            strText = line.GetString(column.ColumnNumber, column.DefaultValue);
                    }
                    else
                    {
                        strText = line.Entry;
                    }


                    strResult.Append( "<td class='"
                        + column.CssClass
                        + "'>" + strText + "</td>\r\n");

                    if (this.SumLine == true
                        && column.Sum == true
                        && column.ColumnNumber != -1)
                    {
                        try
                        {
                            // if (column.DataType != DataType.Currency)
                            {
                                object v = line.GetObject(column.ColumnNumber);
                                if (this.SumCell != null)
                                {
                                    SumCellEventArgs e = new SumCellEventArgs();
                                    e.DataType = column.DataType;
                                    e.ColumnNumber = column.ColumnNumber;
                                    e.LineIndex = i;
                                    e.Line = line;
                                    e.Value = v;
                                    this.SumCell(this, e);
                                    if (e.Value == null)
                                        continue;

                                    v = e.Value;
                                }

                                if (sums[j] == null)
                                    sums[j] = v;
                                else
                                {
                                    sums[j] = AddValue(column.DataType,
            sums[j],
            v);
                                    // sums[j] = ((decimal)sums[j]) + v;
                                }
                            }
                                /*
                            else
                            {
                                string v = line.GetString(column.ColumnNumber);
                                if (this.SumCell != null)
                                {
                                    SumCellEventArgs e = new SumCellEventArgs();
                                    e.DataType = column.DataType;
                                    e.ColumnNumber = column.ColumnNumber;
                                    e.LineIndex = i;
                                    e.Line = line;
                                    e.Value = v;
                                    this.SumCell(this, e);
                                    if (e.Value == null)
                                        continue;
                                    v = (string)e.Value;
                                }
                                sums[j] = PriceUtil.JoinPriceString((string)sums[j],
                                    v);
                            }
                                 * */
                        }
                        catch (Exception ex)	// ����������ַ���ת��Ϊ�����׳����쳣
                        {
                            throw new Exception("���ۼ� �� " + i.ToString() + " �� " + column.ColumnNumber.ToString() + " ֵ��ʱ���׳��쳣: " + ex.Message );
                        }
                    }
                }

                strResult.Append("</tr>\r\n");
            }

            if (this.SumLine == true)
            {
                strResult.Append("<tr class='sum'>\r\n");

                for (j = 0; j < this.Count; j++)
                {
                    PrintColumn column = (PrintColumn)this[j];
                    string strText = "";

                    if (j == 0)
                        strText = "�ϼ�";
                    else if (column.Sum == true
                        && sums[j] != null)
                    {
                        if (column.DataType == DataType.PriceDouble)
                            strText = ((double)sums[j]).ToString("N", nfi);
                        else if (column.DataType == DataType.PriceDecimal)
                            strText = ((decimal)sums[j]).ToString("N", nfi);
                        else if (column.DataType == DataType.Price)
                        {
                            strText = StatisUtil.Int64ToPrice((Int64)sums[j]);
                        }
                        else
                            strText = Convert.ToString(sums[j]);

                        if (column.DataType == DataType.Currency)
                        {
                            string strSomPrice = "";
                            string strError = "";
                            // ���ܼ۸�
                            int nRet = PriceUtil.SumPrices(strText,
                                out strSomPrice,
                                out strError);
                            if (nRet == -1)
                                strText = strError;
                            else
                                strText = strSomPrice;
                        }
                    }
                    else
                        strText = column.DefaultValue;  //  "&nbsp;";

                    strResult.Append("<td class='"
                        + column.CssClass
                        + "'>" + strText + "</td>\r\n");
                }

                strResult.Append("</tr>\r\n");
            }

            strResult.Append("</table>\r\n");
            return strResult.ToString();
        }

        object AddValue(DataType datatype,
            object o1,
            object o2)
        {
            if (o1 == null && o2 == null)
                return null;
            if (o1 == null)
                return o2;
            if (o2 == null)
                return o1;
            if (datatype == DataType.Auto)
            {
                if (o1 is Int64)
                    return (Int64)o1 + (Int64)o2;
                if (o1 is Int32)
                    return (Int32)o1 + (Int32)o2;
                if (o1 is double)
                    return (double)o1 + (double)o2;
                if (o1 is decimal)
                    return (decimal)o1 + (decimal)o2;
                if (o1 is string)
                    return (string)o1 + (string)o2;

                throw new Exception("�޷�֧�ֵ� Auto �����ۼ�");
            }
            if (datatype == DataType.Number)
            {
                if (o1 is Int64)
                    return (Int64)o1 + (Int64)o2;
                if (o1 is Int32)
                    return (Int32)o1 + (Int32)o2;
                if (o1 is double)
                    return (double)o1 + (double)o2;
                if (o1 is decimal)
                    return (decimal)o1 + (decimal)o2;

                throw new Exception("�޷�֧�ֵ� Number �����ۼ�");
            }
            if (datatype == DataType.String)
            {
                if (o1 is string)
                    return (string)o1 + (string)o2;

                throw new Exception("�޷�֧�ֵ� String �����ۼ�");
            }
            if (datatype == DataType.Price) // 100���������
            {
                return (Int64)o1 + (Int64)o2;
            }
            if (datatype == DataType.PriceDouble)  // double��������ʾ��Ҳ�������ֻ����λС������ -- ע�⣬���ۼ�������⣬�Ժ����ֹ
            {
                return (double)o1 + (double)o2;
            }
            if (datatype == DataType.PriceDecimal) // decimal��������ʾ��
            {
                return (decimal)o1 + (decimal)o2;
            }
            if (datatype == DataType.Currency)
            {
                // ��һ�����׷����� �������� �Ĵ���
                return PriceUtil.JoinPriceString((string)o1,
                    (string)o2);
#if NO
                // ��һ�����׳һЩ
                return PriceUtil.JoinPriceString(Convert.ToString(o1),
                    Convert.ToString(o2));
#endif
            }
            throw new Exception("�޷�֧�ֵ� "+datatype.ToString()+" �����ۼ�");
        }

#if NO
        // �ϲ�����
        // 100% + 100% = 100%
        // 50% + 50% = 25%
        static string JoinPercentString(string s1, string s2)
        {
            string t1 = s1.Replace("%", "");
            string t2 = s2.Replace("%", "");

            if (string.IsNullOrEmpty(t1) == true
                && string.IsNullOrEmpty(t2) == true)
                return "";
            if (string.IsNullOrEmpty(t1) == true)
                return t2;
            if (string.IsNullOrEmpty(t2) == true)
                return t1;

            Decimal v1 = 0;
            Decimal v2 = 0;

            if (decimal.TryParse(t1, out v1) == false)
                return t2;
            if (decimal.TryParse(t2, out v2) == false)
                return t1;

            return (v1 + v2 / (decimal)200).ToString();
        }
#endif

        // ���text��ʽ�ı��
        public string TextTable(Table table,
            int nMaxLines = -1)
        {
            StringBuilder strResult = new StringBuilder(4096);
            int i, j;

            if (nMaxLines == -1)
                nMaxLines = table.Count;
            // ������
            for (j = 0; j < this.Count; j++)
            {
                PrintColumn column = (PrintColumn)this[j];
                string strText = column.Title;

                if (column.Colspan == 0)
                    strText = "";   // tab �ַ�������

                if (j != 0)
                    strResult.Append("\t");
                strResult.Append(strText);
            }

            strResult.Append("\r\n");

            // �ϼ�����
            object[] sums = null;

            if (this.SumLine)
            {
                sums = new object[this.Count];
                for (i = 0; i < sums.Length; i++)
                {
                    sums[i] = null;
                }
            }

            // ������ѭ��
            for (i = 0; i < Math.Min(nMaxLines, table.Count); i++)
            {
                Line line = table[i];

                if (this.OutputLine != null)
                {
                    OutputLineEventArgs e = new OutputLineEventArgs();
                    e.Line = line;
                    e.Index = i;
                    this.OutputLine(this, e);
                    if (e.Output == false)
                        continue;
                }

                // ��ѭ��
                for (j = 0; j < this.Count; j++)
                {

                    PrintColumn column = (PrintColumn)this[j];

                    if (column.ColumnNumber < -1)
                    {
                        throw (new Exception("PrintColumn����ColumnNumber����δ��ʼ����λ��" + Convert.ToString(j)));
                    }

                    string strText = "";
                    if (column.ColumnNumber != -1)
                    {
                        if (column.DataType == DataType.Price)
                        {
                            if (line.IsNull(column.ColumnNumber) == true)
                                strText = column.DefaultValue;	// 2005/5/26
                            else
                                strText = line.GetPriceString(column.ColumnNumber);
                        }
                        else
                            strText = line.GetString(column.ColumnNumber, column.DefaultValue);
                    }
                    else
                    {
                        strText = line.Entry;
                    }

                    if (j != 0)
                        strResult.Append("\t");
                    strResult.Append(strText);

                    if (this.SumLine == true
                        && column.Sum == true
                        && column.ColumnNumber != -1)
                    {

                        /*
                        try
                        {
                            sums[j] += line.GetDouble(column.ColumnNumber);
                        }
                        catch	// ����������ַ���ת��Ϊ��ֵ�׳����쳣
                        {
                        }*/

                        try
                        {
                            // if (column.DataType != DataType.Currency)
                            {
                                object v = line.GetObject(column.ColumnNumber);
                                if (this.SumCell != null)
                                {
                                    SumCellEventArgs e = new SumCellEventArgs();
                                    e.DataType = column.DataType;
                                    e.ColumnNumber = column.ColumnNumber;
                                    e.LineIndex = i;
                                    e.Line = line;
                                    e.Value = v;
                                    this.SumCell(this, e);
                                    if (e.Value == null)
                                        continue;
                                    v = (decimal)e.Value;
                                }
                                if (sums[j] == null)
                                    sums[j] = v;
                                else
                                    sums[j] = AddValue(column.DataType,
            sums[j],
            v);
                            }
                            /*
                            else
                            {
                                string v = line.GetString(column.ColumnNumber);
                                if (this.SumCell != null)
                                {
                                    SumCellEventArgs e = new SumCellEventArgs();
                                    e.DataType = column.DataType;
                                    e.ColumnNumber = column.ColumnNumber;
                                    e.LineIndex = i;
                                    e.Line = line;
                                    e.Value = v;
                                    this.SumCell(this, e);
                                    if (e.Value == null)
                                        continue;
                                    v = (string)e.Value;
                                }
                                sums[j] = PriceUtil.JoinPriceString((string)sums[j],
                                    v);
                            }
                             * */

                        }
                        catch (Exception ex)	// ����������ַ���ת��Ϊ�����׳����쳣
                        {
                            throw new Exception("���ۼ� �� " + i.ToString() + " �� " + column.ColumnNumber.ToString() + " ֵ��ʱ���׳��쳣: " + ex.Message);
                        }

                    }


                }

                strResult.Append("\r\n");

            }

            if (this.SumLine == true)
            {
                for (j = 0; j < this.Count; j++)
                {
                    PrintColumn column = (PrintColumn)this[j];
                    string strText = "";

                    if (j == 0)
                        strText = "�ϼ�";
                    else if (column.Sum == true
                        && sums[j] != null)
                    {
                        if (column.DataType == DataType.Price)
                            strText = StatisUtil.Int64ToPrice((Int64)sums[j]);
                        else
                            strText = Convert.ToString(sums[j]);

                        if (column.DataType == DataType.Currency)
                        {
                            string strSomPrice = "";
                            string strError = "";
                                    // ���ܼ۸�
                            int nRet = PriceUtil.SumPrices(strText,
            out strSomPrice,
            out strError);
                            if (nRet == -1)
                                strText = strError;
                            else
                                strText = strSomPrice;
                        }
                    }
                    else
                        strText = " ";

                    if (j != 0)
                        strResult.Append("\t");
                    strResult.Append(strText);
                }

                strResult.Append("\r\n");
            }

            return strResult.ToString();
        }

    }

    // �ϼ��ۼӽ׶ε� ÿһ���ۼ�
    public delegate void SumCellEventHandler(object sender,
        SumCellEventArgs e);

    public class SumCellEventArgs : EventArgs
    {
        public int ColumnNumber = 0; // [in] �кš���0��ʼ���
        public object Value = 0;    // [in,out] ��Ԫֵ���������޸�Ϊ0������ʵ�ֺ��Ըõ�Ԫ������
        public long LineIndex = 0;  // [in]�к�
        public Line Line = null;    // [in]�ж�����Line.Entry���Ի��������
        public DataType DataType = DataType.Auto;
    }

    // �������� ÿһ��
    public delegate void OutputLineEventHandler(object sender,
        OutputLineEventArgs e);

    public class OutputLineEventArgs : EventArgs
    {
        public int Index = -1;      // [in] �е�index
        public Line Line = null;    // [in] ��ǰ׼������������ж���
        public string LineCssClass = "";    // [in][out] �е�css class����
        public bool Output = true;  // [out] �Ƿ���Ҫ���
    }
}
