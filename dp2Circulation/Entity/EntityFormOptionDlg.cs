using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DigitalPlatform;
using DigitalPlatform.Text;

namespace dp2Circulation
{
    internal partial class EntityFormOptionDlg : Form
    {
        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        public string DisplayStyle = "all"; // Ҫֻ��ʾ��Щ page? Ŀǰ���� all / quick_entity

        public EntityFormOptionDlg()
        {
            InitializeComponent();

            this.tabPage_quickEntityRegisterDefault.Tag = "quick_entity";
            this.tabPage_quickIssueRegisterDefault.Tag = "quick_issue";
            this.tabPage_normalEntityRegisterDefault.Tag = "normal_entity";
            this.tabPage_normalIssueRegisterDefault.Tag = "normal_issue";
        }

        private void EntityFormOptionDlg_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }

            string strError = "";

            // ��һ��Ǽ�
            {
                string strNormalDefault = this.MainForm.AppInfo.GetString(
                    "entityform_optiondlg",
                    "normalRegister_default",
                    "<root />");
                int nRet = this.entityEditControl_normalRegisterDefault.SetData(strNormalDefault,
                     "",
                     null,
                     out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);

                this.entityEditControl_normalRegisterDefault.SetReadOnly("librarian");
                this.entityEditControl_normalRegisterDefault.GetValueTable += new GetValueTableEventHandler(entityEditControl_GetValueTable);

                this.checkBox_normalRegister_simple.Checked = this.MainForm.AppInfo.GetBoolean(
    "entityform_optiondlg",
    "normalRegister_simple",
    false);
            }

            // ����ٵǼ�
            {
                string strQuickDefault = this.MainForm.AppInfo.GetString(
                    "entityform_optiondlg",
                    "quickRegister_default",
                    "<root />");
                int nRet = this.entityEditControl_quickRegisterDefault.SetData(strQuickDefault,
                     "",
                     null,
                     out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);

                this.entityEditControl_quickRegisterDefault.SetReadOnly("librarian");
                this.entityEditControl_quickRegisterDefault.GetValueTable += new GetValueTableEventHandler(entityEditControl_GetValueTable);


            }

            // ��һ��Ǽ�
            {
                string strIssueNormalDefault = this.MainForm.AppInfo.GetString(
                    "entityform_optiondlg",
                    "issue_normalRegister_default",
                    "<root />");
                int nRet = this.issueEditControl_normalRegisterDefault.SetData(
                    strIssueNormalDefault,
                     "",
                     null,
                     out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);

                this.issueEditControl_normalRegisterDefault.SetReadOnly("librarian");
                this.issueEditControl_normalRegisterDefault.GetValueTable += new GetValueTableEventHandler(entityEditControl_GetValueTable);
            }

            // �ڿ��ٵǼ�
            {
                string strIssueQuickDefault = this.MainForm.AppInfo.GetString(
                    "entityform_optiondlg",
                    "issue_quickRegister_default",
                    "<root />");
                int nRet = this.issueEditControl_quickRegisterDefault.SetData(
                    strIssueQuickDefault,
                     "",
                     null,
                     out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);

                this.issueEditControl_quickRegisterDefault.SetReadOnly("librarian");
                this.issueEditControl_quickRegisterDefault.GetValueTable += new GetValueTableEventHandler(entityEditControl_GetValueTable);
            }


            // ����һ��Ǽ�
            {
                string strOrderNormalDefault = this.MainForm.AppInfo.GetString(
                    "entityform_optiondlg",
                    "order_normalRegister_default",
                    "<root />");
                int nRet = this.orderEditControl_normalRegisterDefault.SetData(
                    strOrderNormalDefault,
                     "",
                     null,
                     out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);

                this.orderEditControl_normalRegisterDefault.SetReadOnly("librarian");
                this.orderEditControl_normalRegisterDefault.GetValueTable += new GetValueTableEventHandler(entityEditControl_GetValueTable);
            }

            // 2012/12/26
            // ��עһ��Ǽ�
            {
                string strCommentNormalDefault = this.MainForm.AppInfo.GetString(
                    "entityform_optiondlg",
                    "comment_normalRegister_default",
                    "<root />");
                int nRet = this.commentEditControl1.SetData(
                    strCommentNormalDefault,
                     "",
                     null,
                     out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);

                this.commentEditControl1.SetReadOnly("librarian");
                this.commentEditControl1.GetValueTable += new GetValueTableEventHandler(entityEditControl_GetValueTable);
            }


            // У������
            this.checkBox_verifyItemBarcode.Checked = this.MainForm.AppInfo.GetBoolean(
                "entity_form",
                "verify_item_barcode",
                false);

            this.HidePages();
        }

        void entityEditControl_GetValueTable(object sender, GetValueTableEventArgs e)
        {
            string strError = "";
            string[] values = null;
            int nRet = MainForm.GetValueTable(e.TableName,
                e.DbName,
                out values,
                out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
            e.values = values;
        }

        private void EntityFormOptionDlg_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.entityEditControl_normalRegisterDefault.GetValueTable -= new GetValueTableEventHandler(entityEditControl_GetValueTable);
            this.entityEditControl_quickRegisterDefault.GetValueTable -= new GetValueTableEventHandler(entityEditControl_GetValueTable);

            this.issueEditControl_normalRegisterDefault.GetValueTable -= new GetValueTableEventHandler(entityEditControl_GetValueTable);
            this.issueEditControl_quickRegisterDefault.GetValueTable -= new GetValueTableEventHandler(entityEditControl_GetValueTable);

            this.orderEditControl_normalRegisterDefault.GetValueTable -= new GetValueTableEventHandler(entityEditControl_GetValueTable);

            this.commentEditControl1.GetValueTable -= new GetValueTableEventHandler(entityEditControl_GetValueTable);
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";

            // ��һ��Ǽ�
            {
                string strNormalDefault = "";
                this.entityEditControl_normalRegisterDefault.ParentId = "?";
                int nRet = this.entityEditControl_normalRegisterDefault.GetData(
                    true,
                    out strNormalDefault,
                    out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);
                else
                {
                    this.MainForm.AppInfo.SetString(
                        "entityform_optiondlg",
                        "normalRegister_default",
                        strNormalDefault);
                }

                this.MainForm.AppInfo.SetBoolean(
"entityform_optiondlg",
"normalRegister_simple",
this.checkBox_normalRegister_simple.Checked);
            }

            // ����ٵǼ�
            {
                string strQuickDefault = "";

                this.entityEditControl_quickRegisterDefault.ParentId = "?";
                int nRet = this.entityEditControl_quickRegisterDefault.GetData(
                    true,
                    out strQuickDefault,
                    out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);
                else
                {
                    this.MainForm.AppInfo.SetString(
                        "entityform_optiondlg",
                        "quickRegister_default",
                        strQuickDefault);
                }
            }

            // ��һ��Ǽ�
            {
                string strIssueNormalDefault = "";
                this.issueEditControl_normalRegisterDefault.ParentId = "?";
                int nRet = this.issueEditControl_normalRegisterDefault.GetData(
                    true,
                    out strIssueNormalDefault,
                    out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);
                else
                {
                    this.MainForm.AppInfo.SetString(
                        "entityform_optiondlg",
                        "issue_normalRegister_default",
                        strIssueNormalDefault);
                }
            }

            // �ڿ��ٵǼ�
            {
                string strIssueQuickDefault = "";

                this.issueEditControl_quickRegisterDefault.ParentId = "?";
                int nRet = this.issueEditControl_quickRegisterDefault.GetData(
                    true,
                    out strIssueQuickDefault,
                    out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);
                else
                {
                    this.MainForm.AppInfo.SetString(
                        "entityform_optiondlg",
                        "issue_quickRegister_default",
                        strIssueQuickDefault);
                }
            }

            // ����һ��Ǽ�
            {
                string strOrderNormalDefault = "";
                this.orderEditControl_normalRegisterDefault.ParentId = "?";
                int nRet = this.orderEditControl_normalRegisterDefault.GetData(
                    true,
                    out strOrderNormalDefault,
                    out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);
                else
                {
                    this.MainForm.AppInfo.SetString(
                        "entityform_optiondlg",
                        "order_normalRegister_default",
                        strOrderNormalDefault);
                }
            }

            // ��עһ��Ǽ�
            {
                string strCommentNormalDefault = "";
                this.commentEditControl1.ParentId = "?";
                int nRet = this.commentEditControl1.GetData(
                    true,
                    out strCommentNormalDefault,
                    out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);
                else
                {
                    this.MainForm.AppInfo.SetString(
                        "entityform_optiondlg",
                        "comment_normalRegister_default",
                        strCommentNormalDefault);
                }
            }
            this.MainForm.AppInfo.SetBoolean(
                "entity_form",
                "verify_item_barcode",
                this.checkBox_verifyItemBarcode.Checked);


            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void checkBox_normalRegister_simple_CheckedChanged(object sender, EventArgs e)
        {
            this.entityEditControl_normalRegisterDefault.DisplayMode = this.checkBox_normalRegister_simple.Checked == true ? "simple" : "full";
        }

        void HidePages()
        {
            if (StringUtil.IsInList("all", this.DisplayStyle) == true)
                return;

            for(int i=0;i<this.tabControl_main.TabPages.Count; i++)
            {
                TabPage page = this.tabControl_main.TabPages[i];
                string strPageName = page.Tag as string;
                if (StringUtil.IsInList(strPageName, this.DisplayStyle) == false)
                {
                    this.tabControl_main.TabPages.Remove(page);
                    i--;
                }
            }
        }

    }
}