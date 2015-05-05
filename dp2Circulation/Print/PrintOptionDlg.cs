using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;

// 2013/3/16 ��� XML ע��

namespace dp2Circulation
{
    /// <summary>
    /// ��ӡѡ��Ի���
    /// </summary>
    internal partial class PrintOptionDlg : Form
    {
        /// <summary>
        /// �����ڴ����Ŀ�ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// ��ӡ����
        /// </summary>
        internal PrintOption PrintOption = new PrintOption();

        /// <summary>
        /// Ҫ����Ŀ�������б�����ʾ������
        /// </summary>
        public string[] ColumnItems = null;

        /// <summary>
        /// ����Ŀ¼�����ڴ洢���õ�ģ���ļ���
        /// </summary>
        public string DataDir = ""; // �������Ϊ�գ����޷������µ�ģ���ļ�

        int m_nCurrentTemplateIndex = -1;   // ��ǰ�ļ���������Ӧ��ģ��listview����index
        bool m_bTemplateFileContentChanged = false;

        bool m_bTempaltesChanged = false;   // ģ���б����˱仯�������˳���ʱ����Ҫ����

        List<string> m_newCreateTemplateFiles = new List<string>();

        const int COLUMN_NAME = 0;
        const int COLUMN_CAPTION = 1;
        const int COLUMN_WIDTHCHARS = 2;
        const int COLUMN_MAXCHARS = 3;
        const int COLUMN_EVALUE = 4;



        /// <summary>
        /// ���캯��
        /// </summary>
        public PrintOptionDlg()
        {
            InitializeComponent();
        }

        private void PrintOptionDlg_Load(object sender, EventArgs e)
        {
            this.textBox_pageHeader.Text = PrintOption.PageHeader;
            this.textBox_pageFooter.Text = PrintOption.PageFooter;

            this.textBox_tableTitle.Text = PrintOption.TableTitle;
            this.textBox_linesPerPage.Text = PrintOption.LinesPerPage.ToString();
            // this.textBox_maxSummaryChars.Text = PrintOption.MaxSummaryChars.ToString();

            this.listView_columns.Items.Clear();
            for (int i = 0; i < PrintOption.Columns.Count; i++)
            {
                ListViewItem item = new ListViewItem();

#if NO
                item.Text = PrintOption.Columns[i].Name;
                item.SubItems.Add(PrintOption.Columns[i].Caption);
                item.SubItems.Add(PrintOption.Columns[i].MaxChars.ToString());
#endif
                ListViewUtil.ChangeItemText(item, COLUMN_NAME, PrintOption.Columns[i].Name);
                ListViewUtil.ChangeItemText(item, COLUMN_CAPTION, PrintOption.Columns[i].Caption);
                ListViewUtil.ChangeItemText(item, COLUMN_WIDTHCHARS, PrintOption.Columns[i].WidthChars.ToString());
                ListViewUtil.ChangeItemText(item, COLUMN_MAXCHARS, PrintOption.Columns[i].MaxChars.ToString());
                ListViewUtil.ChangeItemText(item, COLUMN_EVALUE, PrintOption.Columns[i].Evalue);


                this.listView_columns.Items.Add(item);
            }

            LoadTemplates();
        }

        private void PrintOptionDlg_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.m_bTempaltesChanged == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "ģ��ҳ���иĶ���δ���棬ȷʵҪ������Щ�Ķ�?\r\n(����뱣����Щ�޸Ĳ��˳���ӡѡ��Ի���Ҫ����ӡѡ��Ի����²��ġ�ȷ������ť)",
                    "PrintOptionDlg",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                RemoveNewCreatedTemplateFiles();
            }
        }

        private void PrintOptionDlg_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            PrintOption.PageHeader = this.textBox_pageHeader.Text;
            PrintOption.PageFooter = this.textBox_pageFooter.Text;

            PrintOption.TableTitle = this.textBox_tableTitle.Text;

            try
            {
                PrintOption.LinesPerPage = Convert.ToInt32(this.textBox_linesPerPage.Text);
            }
            catch
            {
                MessageBox.Show(this, "ÿҳ����ֵ����Ϊ������");
                return;
            }



            PrintOption.Columns.Clear();
            for (int i = 0; i < this.listView_columns.Items.Count; i++)
            {
                ListViewItem item = this.listView_columns.Items[i];

                Column column = new Column();
                column.Name = ListViewUtil.GetItemText(item, COLUMN_NAME); // item.Text;
                column.Caption = ListViewUtil.GetItemText(item, COLUMN_CAPTION);  // item.SubItems[1].Text;

                try
                {
                    column.WidthChars = Convert.ToInt32(
                        ListViewUtil.GetItemText(item, COLUMN_WIDTHCHARS)
                        // item.SubItems[2].Text
                        );
                }
                catch
                {
                    column.WidthChars = -1;
                }

                try
                {
                    column.MaxChars = Convert.ToInt32(
                        ListViewUtil.GetItemText(item, COLUMN_MAXCHARS)
                        // item.SubItems[2].Text
                        );
                }
                catch
                {
                    column.MaxChars = -1;
                }

                column.Evalue = ListViewUtil.GetItemText(item, COLUMN_EVALUE);

                PrintOption.Columns.Add(column);
            }

            // �������һ�ζ�textbox���޸�
            this.RefreshContentToTemplateFile();

            /*
            // ����ģ���б�
            if (this.m_bTempaltesChanged == true)
            {
                PrintOption.TemplatePages.Clear();
                for (int i = 0; i < this.listView_templates.Items.Count; i++)
                {
                    ListViewItem item = this.listView_templates.Items[i];

                    TemplatePageParam param = new TemplatePageParam();
                    param.Caption = item.Text;
                    param.FilePath = ListViewUtil.GetItemText(item, 1);

                    PrintOption.TemplatePages.Add(param);
                }

                this.m_bTempaltesChanged = false;
            }

            this.m_newCreateTemplateFiles.Clear();  // �������Closing()�����в�С��ɾ���ոմ������ļ�
             * */
            SaveTemplatesChanges();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        void SaveTemplatesChanges()
        {
            // ����ģ���б�
            if (this.m_bTempaltesChanged == true)
            {
                PrintOption.TemplatePages.Clear();
                for (int i = 0; i < this.listView_templates.Items.Count; i++)
                {
                    ListViewItem item = this.listView_templates.Items[i];

                    TemplatePageParam param = new TemplatePageParam();
                    param.Caption = item.Text;
                    param.FilePath = ListViewUtil.GetItemText(item, 1);

                    PrintOption.TemplatePages.Add(param);
                }

                this.m_bTempaltesChanged = false;
            }

            this.m_newCreateTemplateFiles.Clear();  // �������Closing()�����в�С��ɾ���ոմ������ļ�
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            /*
            if (this.m_bTempaltesChanged == true)
            {
                this.RefreshContentToTemplateFile();
            }
            */

            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // ɾ�����θոմ�����ģ���ļ���
        void RemoveNewCreatedTemplateFiles()
        {
            for (int i = 0; i < this.m_newCreateTemplateFiles.Count; i++)
            {
                try
                {
                    File.Delete(this.m_newCreateTemplateFiles[i]);
                }
                catch
                {
                }
            }

            this.m_newCreateTemplateFiles.Clear();
        }

        // ������Ŀ
        private void button_columns_new_Click(object sender, EventArgs e)
        {
            PrintColumnDlg dlg = new PrintColumnDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            if (this.ColumnItems != null)
            {
                dlg.ColumnItems = this.ColumnItems;
            }

            if (this.MainForm != null)
                this.MainForm.AppInfo.LinkFormState(dlg, "printorderdlg_formstate");
            dlg.ShowDialog(this);
            if (this.MainForm != null)
                this.MainForm.AppInfo.UnlinkFormState(dlg);


            if (dlg.DialogResult != DialogResult.OK)
                return;

            // ���Ʋ���
            ListViewItem dup = ListViewUtil.FindItem(this.listView_columns, dlg.ColumnName, 0);
            if (dup != null)
            {
                // �ò������ܿ����Ѿ����ڵ���
                ListViewUtil.SelectLine(dup, true);
                dup.EnsureVisible();

                DialogResult result = MessageBox.Show(this,
                    "��ǰ�Ѿ�������Ϊ '"+dlg.ColumnName+"' ����Ŀ����������?",
                    "PrintOptionDlg",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                    return;
            }

            ListViewItem item = new ListViewItem();
#if NO
            item.Text = dlg.ColumnName;
            item.SubItems.Add(dlg.ColumnCaption);
            item.SubItems.Add(dlg.MaxChars.ToString());
#endif
            ListViewUtil.ChangeItemText(item, COLUMN_NAME, dlg.ColumnName);
            ListViewUtil.ChangeItemText(item, COLUMN_CAPTION, dlg.ColumnCaption);
            ListViewUtil.ChangeItemText(item, COLUMN_WIDTHCHARS, dlg.WidthChars.ToString());
            ListViewUtil.ChangeItemText(item, COLUMN_MAXCHARS, dlg.MaxChars.ToString());
            ListViewUtil.ChangeItemText(item, COLUMN_EVALUE, dlg.ColumnEvalue);


            this.listView_columns.Items.Add(item);

            // �ò������ܿ����²������
            ListViewUtil.SelectLine(item, true);
            item.EnsureVisible();

            // ��������󣬵�ǰ��ѡ������������ƶ��Ŀ����Ի������ı�
            listView_columns_SelectedIndexChanged(sender, null);
        }

        // �޸���Ŀ
        private void button_columns_modify_Click(object sender, EventArgs e)
        {
            if (this.listView_columns.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫ�޸ĵ�����");
                return;
            }

            PrintColumnDlg dlg = new PrintColumnDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            if (this.ColumnItems != null)
            {
                dlg.ColumnItems = this.ColumnItems;
            }

            ListViewItem item = this.listView_columns.SelectedItems[0];

            dlg.ColumnName = ListViewUtil.GetItemText(item, COLUMN_NAME);   // this.listView_columns.SelectedItems[0].Text;
            dlg.ColumnCaption = ListViewUtil.GetItemText(item, COLUMN_CAPTION);  // this.listView_columns.SelectedItems[0].SubItems[1].Text;

            try
            {
                dlg.WidthChars = Convert.ToInt32(
                    ListViewUtil.GetItemText(item, COLUMN_WIDTHCHARS)
                    // this.listView_columns.SelectedItems[0].SubItems[2].Text
                    );
            }
            catch
            {
                dlg.WidthChars = -1;
            }

            try
            {
                dlg.MaxChars = Convert.ToInt32(
                    ListViewUtil.GetItemText(item, COLUMN_MAXCHARS)
                    // this.listView_columns.SelectedItems[0].SubItems[2].Text
                    );
            }
            catch
            {
                dlg.MaxChars = -1;
            }

            dlg.ColumnEvalue = ListViewUtil.GetItemText(item, COLUMN_EVALUE);
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            // ListViewItem item = this.listView_columns.SelectedItems[0];
#if NO
            item.Text = dlg.ColumnName;
            item.SubItems[1].Text = dlg.ColumnCaption;
            item.SubItems[2].Text = dlg.MaxChars.ToString();
#endif
            ListViewUtil.ChangeItemText(item, COLUMN_NAME, dlg.ColumnName);
            ListViewUtil.ChangeItemText(item, COLUMN_CAPTION, dlg.ColumnCaption);
            ListViewUtil.ChangeItemText(item, COLUMN_WIDTHCHARS, dlg.WidthChars.ToString());
            ListViewUtil.ChangeItemText(item, COLUMN_MAXCHARS, dlg.MaxChars.ToString());
            ListViewUtil.ChangeItemText(item, COLUMN_EVALUE, dlg.ColumnEvalue);

        }

        // ɾ����Ŀ
        private void button_columns_delete_Click(object sender, EventArgs e)
        {
            if (this.listView_columns.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫɾ��������");
                return;
            }

            DialogResult result = MessageBox.Show(this,
                "ȷʵҪɾ��ѡ���� "+this.listView_columns.SelectedItems.Count.ToString()+" ������? ",
                "PrintOptionDlg",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;


            while (this.listView_columns.SelectedItems.Count>0)
            {
                this.listView_columns.Items.Remove(this.listView_columns.SelectedItems[0]);
            }

            // ɾ������󣬵�ǰ��ѡ������������ƶ��Ŀ����Ի������ı�
            listView_columns_SelectedIndexChanged(sender, null);
        }

        // �����ƶ�(��Ŀ)
        private void button_columns_moveUp_Click(object sender, EventArgs e)
        {
            if (this.listView_columns.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫ�ƶ�������");
                return;
            }

            int nIndex = this.listView_columns.SelectedIndices[0];

            if (nIndex == 0)
            {
                MessageBox.Show(this, "���ڶ���");
                return;
            }

            ListViewItem item = this.listView_columns.SelectedItems[0];

            this.listView_columns.Items.Remove(item);
            this.listView_columns.Items.Insert(nIndex - 1, item);
        }

        // �����ƶ�(��Ŀ)
        private void button_columns_moveDown_Click(object sender, EventArgs e)
        {
            if (this.listView_columns.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫ�ƶ�������");
                return;
            }

            int nIndex = this.listView_columns.SelectedIndices[0];

            if (nIndex >= this.listView_columns.Items.Count - 1)
            {
                MessageBox.Show(this, "���ڵײ�");
                return;
            }

            ListViewItem item = this.listView_columns.SelectedItems[0];

            this.listView_columns.Items.Remove(item);
            this.listView_columns.Items.Insert(nIndex + 1, item);
        }

        private void listView_columns_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView_columns.SelectedIndices.Count == 0)
            {
                // û��ѡ������
                this.button_columns_delete.Enabled = false;
                this.button_columns_modify.Enabled = false;
                this.button_columns_moveDown.Enabled = false;
                this.button_columns_moveUp.Enabled = false;
                this.button_columns_new.Enabled = true;
            }
            else
            {
                // ��ѡ������
                this.button_columns_delete.Enabled = true;
                this.button_columns_modify.Enabled = true;
                if (this.listView_columns.SelectedIndices[0] >= this.listView_columns.Items.Count - 1)
                    this.button_columns_moveDown.Enabled = false;
                else
                    this.button_columns_moveDown.Enabled = true;

                if (this.listView_columns.SelectedIndices[0] == 0)
                    this.button_columns_moveUp.Enabled = false;
                else
                    this.button_columns_moveUp.Enabled = true;

                this.button_columns_new.Enabled = true;

            }
        }

        private void listView_columns_DoubleClick(object sender, EventArgs e)
        {
            this.button_columns_modify_Click(sender, null);
        }

        void LoadTemplates()
        {
            if (this.m_bTempaltesChanged == true)
            {
                DialogResult result = MessageBox.Show(this,
                    "��ǰģ���б����иĶ���δ���档���ʱǿ��ˢ���б������͸Ķ������ݻᶪʧ��\r\n\r\nȷʵҪˢ���б�? ",
                    "PrintOptionDlg",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                    return;

                RemoveNewCreatedTemplateFiles();
            }


            this.listView_templates.Items.Clear();
            this.textBox_templates_content.Text = "";
            this.textBox_templates_content.Enabled = false;

            this.m_nCurrentTemplateIndex = -1;
            this.m_bTemplateFileContentChanged = false;

            if (this.PrintOption == null)
                return;
            if (this.PrintOption.TemplatePages == null)
                return;

            for (int i = 0; i < this.PrintOption.TemplatePages.Count; i++)
            {
                TemplatePageParam param = this.PrintOption.TemplatePages[i];

                ListViewItem item = new ListViewItem();
                item.Text = param.Caption;
                item.SubItems.Add(param.FilePath);

                this.listView_templates.Items.Add(item);
            }

            this.m_bTempaltesChanged = false;
        }

        private void listView_templates_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshContentToTemplateFile();

            if (this.listView_templates.SelectedItems.Count == 0)
            {
                this.textBox_templates_content.Text = "";
                this.textBox_templates_content.Enabled = false;
                this.m_nCurrentTemplateIndex = -1;
                this.m_bTemplateFileContentChanged = false;
            }
            else
            {
                string strError = "";

                // ����λ��
                this.m_nCurrentTemplateIndex = this.listView_templates.SelectedIndices[0];

                this.textBox_templates_content.Text = "";
                this.textBox_templates_content.Enabled = true;

                string strFilePath = ListViewUtil.GetItemText(this.listView_templates.SelectedItems[0], 1);

                /*
                if (File.Exists(strFilePath) == false)
                {
                    this.m_bTemplateFileContentChanged = false;
                    return;
                }

                Encoding encoding = FileUtil.DetectTextFileEncoding(strFilePath);

                StreamReader sr = null;

                try
                {
                    // TODO: ������Զ�̽���ļ����뷽ʽ���ܲ���ȷ��
                    // ��Ҫר�ű�дһ��������̽���ı��ļ��ı��뷽ʽ
                    // Ŀǰֻ����UTF-8���뷽ʽ
                    sr = new StreamReader(strFilePath, encoding);
                    this.textBox_templates_content.Text = sr.ReadToEnd();
                    sr.Close();
                    sr = null;
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    goto ERROR1;
                }
                 * */

                string strContent = "";
                // ���Զ�ʶ���ļ����ݵı��뷽ʽ�Ķ����ı��ļ�����ģ��
                // return:
                //      -1  ����
                //      0   �ļ�������
                //      1   �ļ�����
                int nRet = Global.ReadTextFileContent(strFilePath,
                    out strContent,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                this.textBox_templates_content.Text = strContent;

                this.m_bTemplateFileContentChanged = false;
                return;
            ERROR1:
                this.m_bTemplateFileContentChanged = false;
                MessageBox.Show(this, strError);
            }
        }

        private void listView_templates_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("����ģ��(&N)");
            menuItem.Click += new System.EventHandler(this.menu_newTemplatePage_Click);
            if (String.IsNullOrEmpty(this.DataDir) == true)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("��Windows���±���ģ���ļ�(&O)");
            menuItem.Click += new System.EventHandler(this.menu_openTemplateFileByNotepad_Click);
            if (this.listView_templates.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("ɾ��(&D)");
            menuItem.Click += new System.EventHandler(this.menu_deleteTemplatePages_Click);
            if (this.listView_templates.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.listView_templates,
                new Point(e.X, e.Y));		
        }

        void menu_openTemplateFileByNotepad_Click(object sender, EventArgs e)
        {
            string strError = "";
            if (this.listView_templates.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ�ü��±��򿪵�ģ���ļ�����";
                goto ERROR1;
            }
            foreach (ListViewItem item in this.listView_templates.SelectedItems)
            {
                // ListViewItem item = this.listView_templates.SelectedItems[i];
                string strFilePath = ListViewUtil.GetItemText(item, 1);
                if (String.IsNullOrEmpty(strFilePath) == true)
                    continue;

                System.Diagnostics.Process.Start("notepad.exe", strFilePath);
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

        // ����ģ��
        void menu_newTemplatePage_Click(object sender, EventArgs e)
        {
            string strError = "";

            REDO_INPUT:
            string strName = DigitalPlatform.InputDlg.GetInput(
                this,
                "��ָ��ģ����",
                "ģ����(&T):",
                "",
            this.Font);
            if (strName == null)
                return;

            if (String.IsNullOrEmpty(strName) == true)
            {
                MessageBox.Show(this, "ģ��������Ϊ��");
                goto REDO_INPUT;
            }

            // ����
            ListViewItem dup = ListViewUtil.FindItem(this.listView_templates, strName, 0);
            if (dup != null)
            {
                strError = "ģ���� '" + strName + "' ���б����Ѿ����ڣ������ظ�����";
                goto ERROR1;
            }

            string strFilePath = "";
            int nRedoCount = 0;
            string strDir = PathUtil.MergePath(this.DataDir, "print_templates");
            PathUtil.CreateDirIfNeed(strDir);
            for (int i = 0; ; i++)
            {
                strFilePath = PathUtil.MergePath(strDir, "template_" + (i+1).ToString());
                if (File.Exists(strFilePath) == false)
                {
                    // ����һ��0�ֽڵ��ļ�
                    try
                    {
                        File.Create(strFilePath).Close();
                    }
                    catch (Exception/* ex*/)
                    {
                        if (nRedoCount > 10)
                        {
                            strError = "�����ļ� '" + strFilePath + "' ʧ��...";
                            goto ERROR1;
                        }
                        nRedoCount++;
                        continue;
                    }
                    break;
                }
            }

            // ���ԭ�����е�ѡ��
            this.listView_templates.SelectedItems.Clear();

            ListViewItem item = new ListViewItem();
            item.Text = strName;
            item.SubItems.Add(strFilePath);
            this.listView_templates.Items.Add(item);
            item.Selected = true;   // ѡ������������
            this.m_bTempaltesChanged = true;

            item.EnsureVisible();   // ������Ұ

            this.m_newCreateTemplateFiles.Add(strFilePath);

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void menu_deleteTemplatePages_Click(object sender, EventArgs e)
        {
            string strError = "";
            if (this.listView_templates.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫɾ����ģ������";
                goto ERROR1;
            }

            // ����
            DialogResult result = MessageBox.Show(this,
                "ȷʵҪɾ����ѡ���� " + this.listView_templates.SelectedItems.Count.ToString() + " ��ģ���ļ�?\r\n\r\n(����: ɾ������һ�����У����޷��ô�ӡѡ��Ի����ϵġ�ȡ������ť��ȡ��)",
                "PrintOptionDlg",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result == DialogResult.No)
                return;

            // �������һ�ζ�textbox���޸�
            // ����ɾ����index�����仯���Ź����
            this.RefreshContentToTemplateFile();

            for (int i = this.listView_templates.SelectedIndices.Count - 1; i >= 0; i--)
            {
                int index = this.listView_templates.SelectedIndices[i];
                ListViewItem item = this.listView_templates.Items[index];

                string strFilePath = ListViewUtil.GetItemText(item, 1);

                try
                {
                    File.Delete(strFilePath);
                }
                catch (Exception ex)
                {
                    strError = "ɾ���ļ� '" + strFilePath + "' ʱ��������: " + ex.Message;
                    //goto ERROR1;
                    MessageBox.Show(this, strError);
                }

                this.m_newCreateTemplateFiles.Remove(strFilePath);

                this.listView_templates.Items.RemoveAt(index);
                this.m_bTempaltesChanged = true;
            }

            SaveTemplatesChanges(); // �޸��޷�����
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void RefreshContentToTemplateFile()
        {
            if (this.m_bTemplateFileContentChanged == false)
                return;

            if (this.m_nCurrentTemplateIndex == -1)
            {
                Debug.Assert(false, "");
                this.m_bTemplateFileContentChanged = false;
                return;
            }

            string strError = "";

            ListViewItem item = this.listView_templates.Items[this.m_nCurrentTemplateIndex];
            string strFilePath = ListViewUtil.GetItemText(item, 1);

            if (String.IsNullOrEmpty(strFilePath) == true)
                return;

            try
            {
                StreamWriter sw = new StreamWriter(strFilePath, false, Encoding.UTF8);
                sw.Write(this.textBox_templates_content.Text);
                sw.Close();
            }
            catch (Exception ex)
            {
                strError = "д���ļ� '" + strFilePath + "' ʱ��������" + ex.Message;
                goto ERROR1;
            }

            this.m_bTemplateFileContentChanged = false;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void textBox_templates_content_TextChanged(object sender, EventArgs e)
        {
            this.m_bTemplateFileContentChanged = true;
        }
    }

}