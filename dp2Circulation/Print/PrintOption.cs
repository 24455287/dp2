using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using DigitalPlatform.Xml;
using DigitalPlatform.IO;

namespace dp2Circulation
{

    // һ����
    internal class Column
    {
        public string Caption = "";
        public string Name = "";
        public int WidthChars = -1; // 2014/11/30
        public int MaxChars = -1;
        public string Evalue = "";  // �ű�����
    }

    // 2008/11/23 new add
    /// <summary>
    /// ģ��ҳ����
    /// </summary>
    internal class TemplatePageParam
    {
        /// <summary>
        /// ����
        /// </summary>
        public string Caption = "";
        /// <summary>
        /// �ļ�·��
        /// </summary>
        public string FilePath = "";
    }


    // ��ӡ����
    // TODO: ��Ҫ����Ԥ����ȱʡ��Ŀ�Ĺ���
    internal class PrintOption
    {
        public string DataDir = ""; // ����Ŀ¼������ģ���ļ������ʱ�򣬱������ô˳�Ա

        public string PageHeader = "";  // "%pageno%/%pagecount%";  // ҳü����
        public string PageHeaderDefault = "";

        public string PageFooter = "";  // ҳ������
        public string PageFooterDefault = "";

        public string TableTitle = "";  // "���ƽ��嵥";  // ������
        public string TableTitleDefault = "";

        public int LinesPerPage = 10;   // ÿҳ����������
        public int LinesPerPageDefault = 10;

        public List<Column> Columns = new List<Column>();   // ��Ŀ�б���������Ҫ��ӡ����Щ��Ŀ��

        public List<TemplatePageParam> TemplatePages = new List<TemplatePageParam>();   // ���Ƶ�ҳ��

        // ��Application������װ������
        public virtual void LoadData(ApplicationInfo ai,
            string strPath)
        {
            this.PageHeader = ai.GetString(strPath,
                "PageHeader", this.PageHeaderDefault);

                // "%date% ���ƽ��嵥 - %barcodefilename% - (�� %pagecount% ҳ)");
            this.PageFooter = ai.GetString(strPath,
                "PageFooter", this.PageFooterDefault);
            // "%pageno%/%pagecount%");

            this.TableTitle = ai.GetString(strPath,
                "TableTitle", this.TableTitleDefault);
            
            // "%date% ���ƽ��嵥");

            this.LinesPerPage = ai.GetInt(strPath,
                "LinesPerPage", this.LinesPerPageDefault);
            
            // 20);

            int nCount = ai.GetInt(strPath, "ColumnsCount", 0);
            if (nCount != 0) // ֻ�е��ⲿ�洢����������Ϣʱ����������캯��������ȱʡ��Ϣ
            {
                Columns.Clear();
                for (int i = 0; i < nCount; i++)
                {
                    string strColumnName = ai.GetString(strPath,
                        "ColumnName_" + i.ToString(),
                        "");
                    if (String.IsNullOrEmpty(strColumnName) == true)
                        break;

                    string strColumnCaption = ai.GetString(strPath,
                        "ColumnCaption_" + i.ToString(),
                        "");

                    int nMaxChars = ai.GetInt(strPath,
                        "ColumnMaxChars_" + i.ToString(),
                        -1);
                    int nWidthChars = ai.GetInt(strPath,
    "ColumnWidthChars_" + i.ToString(),
    -1);

                    string strEvalue = ai.GetString(strPath,
    "ColumnEvalue_" + i.ToString(),
    "");


                    Column column = new Column();
                    column.Name = strColumnName;
                    column.Caption = strColumnCaption;
                    column.WidthChars = nWidthChars;
                    column.MaxChars = nMaxChars;
                    column.Evalue = strEvalue;

                    this.Columns.Add(column);
                }
            }

            nCount = ai.GetInt(strPath, "TemplatePagesCount", 0);
            if (nCount != 0) // ֻ�е��ⲿ�洢����������Ϣʱ����������캯��������ȱʡ��Ϣ
            {
                this.TemplatePages.Clear();
                for (int i = 0; i < nCount; i++)
                {
                    TemplatePageParam param = new TemplatePageParam();
                    param.Caption = ai.GetString(strPath,
                        "TemplateCaption_" + i.ToString(),
                        "");
                    param.FilePath = ai.GetString(strPath,
                        "TemplateFilePath_" + i.ToString(),
                        "");

                    Debug.Assert(String.IsNullOrEmpty(this.DataDir) == false, "");

                    param.FilePath = UnMacroPath(param.FilePath);

                    Debug.Assert(param.FilePath.IndexOf("%") == -1, "ȥ�����Ժ��·���ַ������治����%����");

                    this.TemplatePages.Add(param);
                }
            }
        }

        public virtual void SaveData(ApplicationInfo ai,
            string strPath)
        {
            ai.SetString(strPath, "PageHeader",
                this.PageHeader);
            ai.SetString(strPath, "PageFooter",
                this.PageFooter);
            ai.SetString(strPath, "TableTitle",
                this.TableTitle);

            ai.SetInt(strPath, "LinesPerPage",
                this.LinesPerPage);
            /*
            ai.SetInt(strPath, "MaxSummaryChars",
                this.MaxSummaryChars);
             * */

            ai.SetInt(strPath, "ColumnsCount",
                this.Columns.Count);

            for (int i = 0; i < this.Columns.Count; i++)
            {
                ai.SetString(strPath,
                    "ColumnName_" + i.ToString(),
                    this.Columns[i].Name);

                ai.SetString(strPath,
                    "ColumnCaption_" + i.ToString(),
                    this.Columns[i].Caption);

                ai.SetInt(strPath,
                    "ColumnMaxChars_" + i.ToString(),
                    this.Columns[i].MaxChars);

                ai.SetInt(strPath,
    "ColumnWidthChars_" + i.ToString(),
    this.Columns[i].WidthChars);

                ai.SetString(strPath,
    "ColumnEvalue_" + i.ToString(),
    this.Columns[i].Evalue);

            }

            ai.SetInt(strPath, "TemplatePagesCount",
    this.TemplatePages.Count);

            for (int i = 0; i < this.TemplatePages.Count; i++)
            {
                ai.SetString(strPath,
                    "TemplateCaption_" + i.ToString(),
                    this.TemplatePages[i].Caption);

                Debug.Assert(String.IsNullOrEmpty(this.DataDir) == false, "");

                // �任Ϊ���к��ͨ����̬
                string strFilePath = this.TemplatePages[i].FilePath;
                strFilePath = MacroPath(strFilePath);

                ai.SetString(strPath,
                    "TemplateFilePath_" + i.ToString(),
                    strFilePath);
            }

        }

        string MacroPath(string strPath)
        {
            if (String.IsNullOrEmpty(this.DataDir) == true)
                return strPath;

            // ����strPath1�Ƿ�ΪstrPath2���¼�Ŀ¼���ļ�
            if (PathUtil.IsChildOrEqual(strPath, this.DataDir) == true)
            {
                string strPart = strPath.Substring(this.DataDir.Length);
                return "%datadir%" + strPart;
            }

            return strPath;
        }

        string UnMacroPath(string strPath)
        {
            if (String.IsNullOrEmpty(this.DataDir) == true)
                return strPath;

            return strPath.Replace("%datadir%", this.DataDir);
        }

        // ���ģ��ҳ�ļ�
        // parameters:
        //      strCaption  ģ�����ơ���Сд������
        public string GetTemplatePageFilePath(string strCaption)
        {
            if (this.TemplatePages == null)
                return null;

            for (int i = 0; i < this.TemplatePages.Count; i++)
            {
                TemplatePageParam param = this.TemplatePages[i];
                if (param.Caption.ToLower() == strCaption.ToLower())
                {
                    Debug.Assert(param.FilePath.IndexOf("%") == -1, "ȥ�����Ժ��·���ַ������治����%����");

                    return param.FilePath;
                }
            }

            return null;    // not found
        }

        // �Ƿ����ٰ���һ���ű����壿
        public bool HasEvalue()
        {
            foreach (Column column in this.Columns)
            {
                if (string.IsNullOrEmpty(column.Evalue) == false)
                    return true;
            }

            return false;
        }
    }
}
