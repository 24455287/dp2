using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.CommonControl;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// �û���
    /// </summary>
    public partial class UserForm : MyForm
    {
        const int COLUMN_LIBRARYCODE =  0;
        const int COLUMN_USERNAME =     1;
        const int COLUMN_TYPE =         2;
        const int COLUMN_RIGHTS =       3;
        const int COLUMN_CHANGED =      4;
        const int COLUMN_ACCESSCODE =   5;
        const int COLUMN_COMMENT =      6;

        const int WM_PREPARE = API.WM_USER + 200;

        int m_nCurrentItemIndex = -1;   // ��ǰѡ��������ڱ༭���е�listview�����±�

        bool m_bEditChanged = false;

        bool EditChanged
        {
            get
            {
                return this.m_bEditChanged;
            }
            set
            {
                this.m_bEditChanged = value;

                if (this.m_bEditChanged == true)
                    this.button_save.Enabled = true;
                else
                    this.button_save.Enabled = false;
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public UserForm()
        {
            InitializeComponent();
        }

        private void UserForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }

            EnableControls(false);
            API.PostMessage(this.Handle, WM_PREPARE, 0, 0);
        }

        private void UserForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            int nChangedCount = GetChangedCount();

            if (nChangedCount > 0)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ�� "+nChangedCount.ToString()+" ���û���Ϣ�޸ĺ���δ���档����ʱ�رմ��ڣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪ�رմ���? ",
    "UserForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void UserForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveSize();
        }

        void SaveSize()
        {
            // ����splitContainer_main��״̬
            this.MainForm.SaveSplitterPos(
                this.splitContainer_main,
                "userform_state",
                "splitContainer_main_ratio");

            string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_users);
            this.MainForm.AppInfo.SetString(
                "user_form",
                "amerced_list_column_width",
                strWidths);
        }

        void LoadSize()
        {
            try
            {
                // ���splitContainer_main��״̬
                if (this.MainForm != null)
                {
                    this.MainForm.LoadSplitterPos(
                    this.splitContainer_main,
                    "userform_state",
                    "splitContainer_main_ratio");
                }
            }
            catch
            {
            }

            string strWidths = this.MainForm.AppInfo.GetString(
                "user_form",
                "amerced_list_column_width",
                "");
            if (String.IsNullOrEmpty(strWidths) == false)
            {
                ListViewUtil.SetColumnHeaderWidth(this.listView_users,
                    strWidths,
                    true);
            }
        }

        // �ж����������޸Ĺ�(��δ����)?
        int GetChangedCount()
        {
            int nResult = 0;
            for (int i = 0; i < this.listView_users.Items.Count; i++)
            {
                ItemInfo item_info = (ItemInfo)this.listView_users.Items[i].Tag;
                if (item_info.Changed == true)
                    nResult++;
            }

            return nResult;
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_PREPARE:
                    {
                        LoadSize();
                        // װ��ȫ���û���Ϣ

                        // Ȼ����ɽ���
                        EnableControls(true);
                        return;
                    }
                // break;
            }
            base.DefWndProc(ref m);
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public override void EnableControls(bool bEnable)
        {
            this.textBox_userName.Enabled = bEnable;
            this.textBox_userRights.Enabled = bEnable;
            this.textBox_userType.Enabled = bEnable;
            // this.textBox_libraryCode.Enabled = bEnable;
            this.checkedComboBox_libraryCode.Enabled = bEnable;
            this.textBox_access.Enabled = bEnable;
            this.textBox_comment.Enabled = bEnable;
            this.listView_users.Enabled = bEnable;

            this.button_listAllUsers.Enabled = bEnable;
            this.button_create.Enabled = bEnable;
            this.button_editUserRights.Enabled = bEnable;

            this.checkBox_changePassword.Enabled = bEnable;

            if (bEnable == true)
            {
                if (this.m_bEditChanged == true)
                    this.button_save.Enabled = true;
                else
                    this.button_save.Enabled = false;
            }
            else
            {
                this.button_save.Enabled = false;
            }

            if (this.textBox_userName.Text == "")
                this.button_delete.Enabled = false;
            else
                this.button_delete.Enabled = bEnable;

            if (this.checkBox_changePassword.Checked == true)
            {
                this.textBox_confirmPassword.Enabled = bEnable;
                this.textBox_password.Enabled = bEnable;
                this.button_resetPassword.Enabled = bEnable;
            }
            else
            {
                this.textBox_confirmPassword.Enabled = false;
                this.textBox_password.Enabled = false;
                this.button_resetPassword.Enabled = false;
            }
        }

        void ClearEdit()
        {
            this.textBox_userName.Text = "";
            this.textBox_userType.Text = "";
            this.textBox_userRights.Text = "";
            //this.textBox_libraryCode.Text = "";
            this.checkedComboBox_libraryCode.Text = "";
            this.textBox_access.Text = "";
            this.textBox_comment.Text = "";
        }

        // �г������û�
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        int ListAllUsers(out string strError)
        {
            strError = "";

            this.listView_users.Items.Clear();
            this.m_nCurrentItemIndex = -1;
            this.ClearEdit();

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ��ȫ���û���Ϣ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                int nStart = 0;
                for (; ; )
                {
                    UserInfo[] users = null;
                    long lRet = Channel.GetUser(
                        stop,
                        "list",
                        "",
                        nStart,
                        -1,
                        out users,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;
                    if (lRet == 0)
                    {
                        strError = "�������û���Ϣ��";
                        return 0;   // not found
                    }

                    Debug.Assert(users != null, "");

                    for (int i = 0; i < users.Length; i++)
                    {
                        UserInfo info = users[i];

                        ListViewItem item = new ListViewItem();

                        /*
                        item.Text = info.UserName;
                        item.SubItems.Add(info.Type);
                        item.SubItems.Add(info.Rights);
                        item.SubItems.Add("");
                         * */

                        ItemInfo item_info = new ItemInfo();
                        item_info.UserInfo = info;
                        item.Tag = item_info;

                        SetListViewItemValue(item_info,
                            item);

                        this.listView_users.Items.Add(item);
                    }

                    nStart += users.Length;
                    if (nStart >= lRet)
                        break;
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            return 1;
        ERROR1:
            return -1;
        }

        static void SetListViewItemValue(ItemInfo item_info,
            ListViewItem item)
        {
            ListViewUtil.ChangeItemText(item, COLUMN_LIBRARYCODE,
                item_info.UserInfo.LibraryCode);
            ListViewUtil.ChangeItemText(item, COLUMN_USERNAME,
                item_info.UserInfo.UserName);
            ListViewUtil.ChangeItemText(item, COLUMN_TYPE,
                item_info.UserInfo.Type);
            ListViewUtil.ChangeItemText(item, COLUMN_RIGHTS,
                item_info.UserInfo.Rights);
            ListViewUtil.ChangeItemText(item, COLUMN_CHANGED,
                item_info.Changed == true ? "*" : "");
            if (item_info.Changed == true)
                item.BackColor = Color.Yellow;
            else
                item.BackColor = SystemColors.Window;

            ListViewUtil.ChangeItemText(item, COLUMN_ACCESSCODE,
                item_info.UserInfo.Access);
            ListViewUtil.ChangeItemText(item, COLUMN_COMMENT,
                item_info.UserInfo.Comment);
#if NO
            while (item.SubItems.Count < 6)
            {
                item.SubItems.Add("");
            }

            item.SubItems[0].Text = item_info.UserInfo.LibraryCode;
            item.SubItems[1].Text = item_info.UserInfo.UserName;
            item.SubItems[2].Text = item_info.UserInfo.Type;
            item.SubItems[3].Text = item_info.UserInfo.Rights;
            item.SubItems[4].Text = item_info.Changed == true ? "*" : "";
            if (item_info.Changed == true)
                item.BackColor = Color.Yellow;
            else
                item.BackColor = SystemColors.Window;

            item.SubItems[5].Text = item_info.UserInfo.Access;
#endif
        }

        private void button_listAllUsers_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            int nChangedCount = GetChangedCount();

            if (nChangedCount > 0)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
    "��ǰ�� " + nChangedCount.ToString() + " ���û���Ϣ�޸ĺ���δ���档����ʱ������ȫ������Ϣ������δ������Ϣ����ʧ��\r\n\r\nȷʵҪ������ȫ���û���Ϣ? ",
    "UserForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            nRet = ListAllUsers(out strError);
            if (nRet == -1)
                goto ERROR1;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ��edit�е����ݻָ���listviewitem��
        void StoreEditToListViewItem()
        {
            if (this.m_nCurrentItemIndex == -1)
                return;

            ItemInfo item_info = (ItemInfo)this.listView_users.Items[this.m_nCurrentItemIndex].Tag;
            item_info.UserInfo.UserName = this.textBox_userName.Text;
            item_info.UserInfo.Type = this.textBox_userType.Text;
            item_info.UserInfo.Rights = this.textBox_userRights.Text;
            item_info.UserInfo.LibraryCode = this.checkedComboBox_libraryCode.Text; //  this.textBox_libraryCode.Text;
            item_info.UserInfo.Access = this.textBox_access.Text;
            item_info.UserInfo.Comment = this.textBox_comment.Text;
            item_info.Changed = this.EditChanged;

            // �޸���ʾ���ı�����ɫ
            SetListViewItemValue(item_info,
                this.listView_users.Items[this.m_nCurrentItemIndex]);
        }

        // ��listviewitem�е��������õ�edit��
        void SetListViewItemToEdit(int index)
        {
            if (index == -1)
            {
                ClearUserEdit();
                this.m_nCurrentItemIndex = -1;
                this.textBox_userName.ReadOnly = false;
                return;
            }

            ItemInfo item_info = (ItemInfo)this.listView_users.Items[index].Tag;

            UserInfo info = item_info.UserInfo;

            this.textBox_userName.Text = info.UserName;

            this.textBox_userName.ReadOnly = true;

            this.textBox_userType.Text = info.Type;
            this.textBox_userRights.Text = info.Rights;
            // this.textBox_libraryCode.Text = info.LibraryCode;
            this.checkedComboBox_libraryCode.Text = info.LibraryCode;
            this.textBox_access.Text = info.Access;
            this.textBox_comment.Text = info.Comment;


            // ��������������벻һ������ֹ����������������
            this.textBox_password.Text = "1";
            this.textBox_confirmPassword.Text = "2";

            this.m_nCurrentItemIndex = index;

            if (this.textBox_userName.Text == "")
                this.button_delete.Enabled = false;
            else
                this.button_delete.Enabled = true;

            // ÿ�ζ�Ҫ��ΪOn���checkbox�������޸�����
            this.checkBox_changePassword.Checked = false;
            this.checkBox_changePassword_CheckedChanged(this, null);

            this.EditChanged = item_info.Changed;

            // ResetTextBoxHeight();
        }

        void ClearUserEdit()
        {
            this.textBox_userName.Text = "";
            this.textBox_userRights.Text = "";
            this.textBox_userType.Text = "";
            // this.textBox_libraryCode.Text = "";
            this.checkedComboBox_libraryCode.Text = "";
            this.textBox_access.Text = "";
            this.textBox_comment.Text = "";

            this.textBox_password.Text = "";
            this.textBox_confirmPassword.Text = "";

            this.EditChanged = false;

            // ÿ�ζ�Ҫ��ΪOn���checkbox�������޸�����
            this.checkBox_changePassword.Checked = false;
            this.checkBox_changePassword_CheckedChanged(this, null);

            // ResetTextBoxHeight();
        }

        private void listView_users_SelectedIndexChanged(object sender, EventArgs e)
        {
            StoreEditToListViewItem();

            if (this.listView_users.SelectedItems.Count == 0)
            {
                SetListViewItemToEdit(-1);
                return;
            }

            SetListViewItemToEdit(this.listView_users.SelectedIndices[0]);
        }


        // ��������
        private void button_resetPassword_Click(object sender, EventArgs e)
        {
            string strError = "";
            if (this.textBox_password.Text != this.textBox_confirmPassword.Text)
            {
                strError = "���� �� �ٴ��������� ��һ�¡�";
                goto ERROR1;
            }

            int nRet = ResetPassword(
                this.textBox_userName.Text,
                this.textBox_password.Text,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // ���ﲻ�Ǻ����⡣Ӧ����������������ͨ��Ϣ����changed��־��
            this.EditChanged = false;

            MessageBox.Show(this, "�����������");
            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

        // ��������
        int ResetPassword(
            string strUserName,
            string strNewPassword,
            out string strError)
        {
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���������û����� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                UserInfo info = new UserInfo();

                info.UserName = strUserName;
                info.Password = strNewPassword;
                info.SetPassword = true;    // û�б�Ҫ

                long lRet = Channel.SetUser(
                    stop,
                    "resetpassword",
                    info,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            return 1;
        ERROR1:
            return -1;
        }

        // �����û���Ϣ
        int SaveUserInfo(
            UserInfo info,
            out string strError)
        {
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ����û���Ϣ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.SetUser(
                    stop,
                    "change",
                    info,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            return 1;
        ERROR1:
            return -1;
        }

        // �����û���Ϣ
        int CreateUserInfo(
            UserInfo info,
            out string strError)
        {
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڴ����û���Ϣ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.SetUser(
                    stop,
                    "new",
                    info,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            return 1;
        ERROR1:
            return -1;
        }

        // ɾ���û���Ϣ
        int DeleteUserInfo(
            string strUserName,
            out string strError)
        {
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����ɾ���û���Ϣ ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                UserInfo info = new UserInfo();
                info.UserName = strUserName;

                long lRet = Channel.SetUser(
                    stop,
                    "delete",
                    info,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            return 1;
        ERROR1:
            return -1;
        }

        // �༭Ȩ��
        private void button_editUserRights_Click(object sender, EventArgs e)
        {
            DigitalPlatform.CommonDialog.PropertyDlg dlg = new DigitalPlatform.CommonDialog.PropertyDlg();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.Text = "�û� '" + this.textBox_userName.Text + "' ��Ȩ��";
            dlg.PropertyString = this.textBox_userRights.Text;
            dlg.CfgFileName = this.MainForm.DataDir + "\\userrightsdef.xml";
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            this.textBox_userRights.Text = dlg.PropertyString;
        }

        // �����û���Ϣ
        private void button_save_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.textBox_userName.Text == "")
            {
                strError = "�û�������Ϊ��";
                goto ERROR1;
            }

            UserInfo info = new UserInfo();

            info.UserName = this.textBox_userName.Text;
            info.Type = this.textBox_userType.Text;
            info.Rights = this.textBox_userRights.Text;
            info.LibraryCode = this.checkedComboBox_libraryCode.Text;   //  this.textBox_libraryCode.Text;
            info.Access = this.textBox_access.Text;
            info.Comment = this.textBox_comment.Text;

            if (this.checkBox_changePassword.Checked == true)
            {
                if (this.textBox_confirmPassword.Text != this.textBox_password.Text)
                {
                    strError = "���� �� �ٴ��������� ��һ�¡�";
                    goto ERROR1;
                }
                info.SetPassword = true;
                info.Password = this.textBox_password.Text;
            }
            else
                info.SetPassword = false;

            // �����û���Ϣ
            nRet = SaveUserInfo(
                info,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.EditChanged = false;
            MessageBox.Show(this, "�û���Ϣ����ɹ�");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        internal class ItemInfo
        {
            public UserInfo UserInfo = null;
            bool m_bChanged = false;

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
                    this.m_bChanged = value;
                }
            }
        }

        private void textBox_userName_TextChanged(object sender, EventArgs e)
        {
            this.EditChanged = true;
        }

        private void textBox_userType_TextChanged(object sender, EventArgs e)
        {
            this.EditChanged = true;
        }

        private void textBox_userRights_TextChanged(object sender, EventArgs e)
        {
            this.EditChanged = true;
        }

        private void textBox_libraryCode_TextChanged(object sender, EventArgs e)
        {
            this.EditChanged = true;
        }

        private void textBox_access_TextChanged(object sender, EventArgs e)
        {
            this.EditChanged = true;
        }


        private void textBox_password_TextChanged(object sender, EventArgs e)
        {
            this.EditChanged = true;
        }

        private void textBox_confirmPassword_TextChanged(object sender, EventArgs e)
        {
            this.EditChanged = true;
        }

        private void button_delete_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.textBox_userName.Text == "")
            {
                strError = "�û�������Ϊ��";
                goto ERROR1;
            }

            // ����
            DialogResult result = MessageBox.Show(this,
                "ȷʵҪɾ���û� " + this.textBox_userName.Text,
                "UserForm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result == DialogResult.No)
                return;

            // ɾ���û���Ϣ
            nRet = DeleteUserInfo(
                this.textBox_userName.Text,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            MessageBox.Show(this, "�û���Ϣɾ���ɹ�");

            // ��listview��ɾ��
            if (this.listView_users.SelectedItems.Count > 0
                && ListViewUtil.GetItemText(this.listView_users.SelectedItems[0], COLUMN_USERNAME) == this.textBox_userName.Text)
            {
                this.listView_users.Items.Remove(this.listView_users.SelectedItems[0]);
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

        private void checkBox_changePassword_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_changePassword.Checked == true)
            {
                this.textBox_confirmPassword.Enabled = true;
                this.textBox_password.Enabled = true;
                this.button_resetPassword.Enabled = true;
            }
            else
            {
                this.textBox_confirmPassword.Enabled = false;
                this.textBox_password.Enabled = false;
                this.button_resetPassword.Enabled = false;
            }
        }

        // �������û�
        private void button_create_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            // ˢ��ģ�壬׼��������Ϣ
            if (Control.ModifierKeys == Keys.Control)
            {
                for (int i = 0; i < this.listView_users.Items.Count; i++)
                {
                    this.listView_users.Items[i].Selected = false;
                }
                return;
            }

            if (this.textBox_userName.Text == "")
            {
                strError = "�û�������Ϊ��";
                goto ERROR1;
            }

            UserInfo info = new UserInfo();

            info.UserName = this.textBox_userName.Text;
            info.Type = this.textBox_userType.Text;
            info.Rights = this.textBox_userRights.Text;
            info.LibraryCode = this.checkedComboBox_libraryCode.Text;   //  this.textBox_libraryCode.Text;
            info.Access = this.textBox_access.Text;
            info.Comment = this.textBox_comment.Text;

            if (this.checkBox_changePassword.Checked == true)
            {
                if (this.textBox_confirmPassword.Text != this.textBox_password.Text)
                {
                    strError = "���� �� �ٴ��������� ��һ�¡�";
                    goto ERROR1;
                }
                info.SetPassword = true;
                info.Password = this.textBox_password.Text;
            }
            else
                info.SetPassword = false;

            // �����û���Ϣ
            nRet = CreateUserInfo(
                info,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.EditChanged = false;

            // ����listview
            ListViewItem item = new ListViewItem();
            ItemInfo item_info = new ItemInfo();
            info.SetPassword = false;
            item_info.UserInfo = info;

            item.Tag = item_info;

            SetListViewItemValue(item_info,
                item);

            this.listView_users.Items.Add(item);

            MessageBox.Show(this, "�û� '" + info.UserName + "' �����ɹ�");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void UserForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

        SortColumns SortColumns = new SortColumns();

        private void listView_users_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int nClickColumn = e.Column;
            this.SortColumns.SetFirstColumn(nClickColumn,
                this.listView_users.Columns);

            // ����
            this.listView_users.ListViewItemSorter = new SortColumnsComparer(this.SortColumns);

            this.listView_users.ListViewItemSorter = null;

        }

        private void checkedComboBox_libraryCode_TextChanged(object sender, EventArgs e)
        {
            this.EditChanged = true;
        }

        private void checkedComboBox_libraryCode_DropDown(object sender, EventArgs e)
        {
            if (this.checkedComboBox_libraryCode.Items.Count > 0)
                return;
            lock(this.checkedComboBox_libraryCode)
            {
                List<string> librarycodes = null;
                string strError = "";
                // �г����йݴ���
                // return:
                //      -1  ����
                //      0   û���ҵ�
                //      1   �ҵ�
                int nRet = GetLibraryCodes(
                out librarycodes,
                out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    return;
                }

                this.checkedComboBox_libraryCode.Items.AddRange(librarycodes);
            }
        }

        // �г����йݴ���
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        int GetLibraryCodes(
            out List<string> librarycodes,
            out string strError)
        {
            strError = "";
            librarycodes = new List<string>();

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ��ȫ���ݴ��� ...");
            stop.BeginLoop();

            try
            {
                string strValue = "";
                long lRet = Channel.GetSystemParameter(stop,
                    "system",
                    "libraryCodes",
                    out strValue,
                    out strError);
                if (lRet == -1)
                {
                    strError = "��ùݴ���ʱ��������" + strError;
                    return -1;
                }

                librarycodes = StringUtil.FromListString(strValue);
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            return 1;
        }

        private void textBox_comment_TextChanged(object sender, EventArgs e)
        {
            this.EditChanged = true;
        }

        private void tableLayoutPanel_userEdit_SizeChanged(object sender, EventArgs e)
        {
        }

#if NO
        int _inReset = 0;

        void ResetTextBoxHeight()
        {
            // ��ֹ����
            if (_inReset > 0)
                return;
            _inReset++;
            try
            {
                this.tableLayoutPanel_userEdit.ResetAllTextBoxHeight();
            }
            finally
            {
                _inReset--;
            }
        }
#endif
    }
}