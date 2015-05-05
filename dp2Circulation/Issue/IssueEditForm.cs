using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using System.Xml;

using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.LibraryServer;

namespace dp2Circulation
{
    /// <summary>
    /// �ڼ�¼�༭�Ի���
    /// </summary>
    public partial class IssueEditForm : IssueEditFormBase
        // ItemEditFormBase<IssueItem, IssueItemCollection>
    {
#if NO
        /// <summary>
        /// ��ʼ����
        /// </summary>
        public IssueItem StartIssueItem = null;   // �ʼʱ�Ķ���

        /// <summary>
        /// ��ǰ����
        /// </summary>
        public IssueItem IssueItem = null;

        /// <summary>
        /// �����
        /// </summary>
        public IssueItemCollection IssueItems = null;

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// �ڿؼ�
        /// </summary>
        public IssueControl IssueControl = null;
#endif

        /// <summary>
        /// ���캯��
        /// </summary>
        public IssueEditForm()
        {
            InitializeComponent();

            _editing = this.issueEditControl_editing;
            _existing = this.issueEditControl_existing;

            _label_editing = this.label_editing;
            _button_editing_undoMaskDelete = this.button_editing_undoMaskDelete;
            _button_editing_nextRecord = this.button_editing_nextRecord;
            _button_editing_prevRecord = this.button_editing_prevRecord;

            _checkBox_autoSearchDup = this.checkBox_autoSearchDup;

            _button_OK = this.button_OK;
            _button_Cancel = this.button_Cancel;

            _textBox_message = this.textBox_message;
            _splitContainer_main = this.splitContainer_main;
            _tableLayoutPanel_main = this.tableLayoutPanel_main;
        }

#if NO
        // Ϊ�༭Ŀ�ĵĳ�ʼ��
        // parameters:
        //      issueitems   ����������UndoMaskDelete
        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="issueitem">Ҫ�༭��������</param>
        /// <param name="issueitems">�������ڵļ���</param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public int InitialForEdit(
            IssueItem issueitem,
            IssueItemCollection issueitems,
            out string strError)
        {
            strError = "";

            this.IssueItem = issueitem;
            this.IssueItems = issueitems;

            this.StartIssueItem = issueitem;

            return 0;
        }
#endif

        private void IssueEditForm_Load(object sender, EventArgs e)
        {
#if NO
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
            LoadIssueItem(this.IssueItem);
            EnablePrevNextRecordButtons();

            // �ο���¼
            if (this.IssueItem != null
                && this.IssueItem.Error != null
                && string.IsNullOrEmpty(this.IssueItem.Error.OldRecord) == false)
            {

                this.splitContainer_main.Panel1Collapsed = false;

                string strError = "";
                int nRet = FillExisting(out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);


                this.issueEditControl_existing.SetReadOnly(ReadOnlyStyle.All);

                // ͻ����������
                this.issueEditControl_editing.HighlightDifferences(this.issueEditControl_existing);

            }
            else
            {
                this.tableLayoutPanel_main.RowStyles[0].Height = 0F;
                this.textBox_message.Visible = false;

                this.label_editing.Visible = false;
                this.splitContainer_main.Panel1Collapsed = true;
                this.issueEditControl_existing.Enabled = false;
            }
#endif
        }

        private void IssueEditForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            this.issueEditControl_editing.GetValueTable -= new GetValueTableEventHandler(issueEditControl_editing_GetValueTable);
#endif
        }

#if NO
        void LoadIssueItem(IssueItem issueitem)
        {
            if (issueitem != null)
            {
                string strError = "";
                int nRet = FillEditing(issueitem, out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, "LoadIssueItem() ��������: " + strError);
                    return;
                }
            }
            if (issueitem != null
                && issueitem.ItemDisplayState == ItemDisplayState.Deleted)
            {
                // �Ѿ����ɾ��������, ���ܽ����޸ġ����ǿ��Թ۲�
                this.issueEditControl_editing.SetReadOnly(ReadOnlyStyle.All);
                this.checkBox_autoSearchDup.Enabled = false;

                this.button_editing_undoMaskDelete.Enabled = true;
                this.button_editing_undoMaskDelete.Visible = true;
            }
            else
            {
                this.issueEditControl_editing.SetReadOnly(ReadOnlyStyle.Librarian);

                this.button_editing_undoMaskDelete.Enabled = false;
                this.button_editing_undoMaskDelete.Visible = false;
            }

            this.issueEditControl_editing.GetValueTable -= new GetValueTableEventHandler(issueEditControl_editing_GetValueTable);
            this.issueEditControl_editing.GetValueTable += new GetValueTableEventHandler(issueEditControl_editing_GetValueTable);

            this.IssueItem = issueitem;

            SetOkButtonState();
        }

        void issueEditControl_editing_GetValueTable(object sender, GetValueTableEventArgs e)
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

        void SetOkButtonState()
        {
            if (this.IssueItem != this.StartIssueItem)
            {
                this.button_OK.Enabled = issueEditControl_editing.Changed;
            }
            else
            {
                this.button_OK.Enabled = true;
            }
        }

        // ����һ������ı༭
        // return:
        //      -1  ����
        //      0   û�б�Ҫ��restore
        //      1   ����restore
        int FinishOneIssueItem(out string strError)
        {
            strError = "";
            int nRet = 0;

            if (issueEditControl_editing.Changed == false)
                return 0;

            string strPublishTime = this.issueEditControl_editing.PublishTime;

            // TODOL ������ʱ����ʽ�Ƿ�Ϸ�
            if (String.IsNullOrEmpty(strPublishTime) == true)
            {
                strError = "����ʱ�䲻��Ϊ��";
                goto ERROR1;
            }

            nRet = Restore(out strError);
            if (nRet == -1)
                goto ERROR1;

            return nRet;
        ERROR1:
            return -1;
        }
#endif
        internal override int FinishVerify(out string strError)
        {
            strError = "";
            int nRet = 0;

            string strPublishTime = this.issueEditControl_editing.PublishTime;

            // TODOL ������ʱ����ʽ�Ƿ�Ϸ�
            if (String.IsNullOrEmpty(strPublishTime) == true)
            {
                strError = "����ʱ�䲻��Ϊ��";
                return -1;
            }

            // 2014/10/23
            if (string.IsNullOrEmpty(this.issueEditControl_editing.PublishTime) == false)
            {
                // ��鵥�����������ַ����Ƿ�Ϸ�
                // return:
                //      -1  ����
                //      0   ��ȷ
                nRet = LibraryServerUtil.CheckSinglePublishTime(this.issueEditControl_editing.PublishTime,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return 0;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
#if NO
            string strError = "";
            int nRet = 0;


            nRet = this.FinishOneIssueItem(out strError);
            if (nRet == -1)
                goto ERROR1;

            // TODO: �ύ�����timestamp��ƥ��ʱ���ֵĶԻ���Ӧ����ֹprev/next��ť

            // ����б�����Ϣ�����
            if (this.IssueItem != null
                && this.IssueItem.Error != null
                && this.IssueItem.Error.ErrorCode == DigitalPlatform.CirculationClient.localhost.ErrorCodeValue.TimestampMismatch)
            {
                this.IssueItem.OldRecord = this.IssueItem.Error.OldRecord;
                this.IssueItem.Timestamp = this.IssueItem.Error.OldTimestamp;
            }

            this.IssueItem.Error = null; // ��������״̬

            this.DialogResult = DialogResult.OK;
            this.Close();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
#endif
            OnButton_OK_Click(sender, e);
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();

        }

#if NO
        // ���༭��������
        int FillEditing(IssueItem issueitem,
            out string strError)
        {
            strError = "";

            if (issueitem == null)
            {
                strError = "issueitem����ֵΪ��";
                return -1;
            }

            string strXml = "";
            int nRet = issueitem.BuildRecord(out strXml,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = this.issueEditControl_editing.SetData(strXml,
                issueitem.RecPath,
                issueitem.Timestamp,
                out strError);
            if (nRet == -1)
                return -1;


            return 0;
        }

        // ���ο��༭��������
        int FillExisting(out string strError)
        {
            strError = "";

            if (this.IssueItem == null)
            {
                strError = "IssueItemΪ��";
                return -1;
            }

            if (this.IssueItem.Error == null)
            {
                strError = "IssueItem.ErrorΪ��";
                return -1;
            }

            this.textBox_message.Text = this.IssueItem.ErrorInfo;

            int nRet = this.issueEditControl_existing.SetData(this.IssueItem.Error.OldRecord,
                this.IssueItem.Error.OldRecPath, // NewRecPath
                this.IssueItem.Error.OldTimestamp,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        // �ӽ����и���issueitem�е�����
        // return:
        //      -1  error
        //      0   û�б�Ҫ����
        //      1   �Ѿ�����
        int Restore(out string strError)
        {
            strError = "";
            // int nRet = 0;

            if (issueEditControl_editing.Changed == false)
                return 0;

            if (this.IssueItem == null)
            {
                strError = "IssueItemΪ��";
                return -1;
            }


            // TODO: �Ƿ����checkboxΪfalse��ʱ������ҲҪ��鱾��֮����ظ����Σ�
            // ������ﲻ��飬�ɷ����ύ�����ʱ���Ȳ��걾��֮����ظ�����������������ύ?
            if (this.checkBox_autoSearchDup.Checked == true
                && this.IssueControl != null)
            {
#if NOOOOOOOOOOOOO
                // Debug.Assert(false, "");
                // �������
                // return:
                //      -1  ����
                //      0   ���ظ�
                //      1   �ظ�
                nRet = this.EntityForm.CheckPublishTimeDup(
                    this.issueEditControl_editing.PublishTime,
                    this.IssueItem,
                    true,   // bCheckCurrentList,
                    true,   // bCheckDb,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 1)
                    return -1;   // �ظ�
#endif 
            }

            // ��ñ༭�������
            try
            {
                this.IssueItem.RecordDom = this.issueEditControl_editing.DataDom;
            }
            catch (Exception ex)
            {
                strError = "�������ʱ����: " + ex.Message;
                return -1;
            }

            this.IssueItem.Changed = true;
            if (this.IssueItem.ItemDisplayState != ItemDisplayState.New)
            {
                this.IssueItem.ItemDisplayState = ItemDisplayState.Changed;
                // ����ζ��Deleted״̬Ҳ�ᱻ�޸�ΪChanged
            }

            this.IssueItem.RefreshListView();

            return 1;
        }


        public bool AutoSearchDup
        {
            get
            {
                return this.checkBox_autoSearchDup.Checked;
            }
            set
            {
                this.checkBox_autoSearchDup.Checked = value;
            }
        }
#endif

        private void button_editing_undoMaskDelete_Click(object sender, EventArgs e)
        {
            if (this.Items != null)
            {
                this.Items.UndoMaskDeleteItem(this.Item);
                this.issueEditControl_editing.SetReadOnly("librarian");
                this.checkBox_autoSearchDup.Enabled = true;
                // this.button_OK.Enabled = entityEditControl_editing.Changed;
            }
        }

#if NO
        void LoadPrevOrNextIssueItem(bool bPrev)
        {
            string strError = "";

            IssueItem new_issueitem = GetPrevOrNextIssueItem(bPrev,
                out strError);
            if (new_issueitem == null)
                goto ERROR1;

            // ���浱ǰ����
            int nRet = FinishOneIssueItem(out strError);
            if (nRet == -1)
                goto ERROR1;

            LoadIssueItem(new_issueitem);

            // ��listview�й������ɼ���Χ
            new_issueitem.HilightListViewItem(true);
            this.Text = "����Ϣ";
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public void EnableControls(bool bEnable)
        {
            this.button_Cancel.Enabled = bEnable;

            if (bEnable == true)
                SetOkButtonState();
            else
                this.button_OK.Enabled = false;


            if (bEnable == false)
            {
                this.button_editing_nextRecord.Enabled = bEnable;
                this.button_editing_prevRecord.Enabled = bEnable;
            }
            else
                this.EnablePrevNextRecordButtons();
        }
#endif

        private void button_editing_prevRecord_Click(object sender, EventArgs e)
        {
            this.EnableControls(false);

            LoadPrevOrNextItem(true);
            EnablePrevNextRecordButtons();

            this.EnableControls(true);

        }

        private void button_editing_nextRecord_Click(object sender, EventArgs e)
        {
            this.EnableControls(false);

            LoadPrevOrNextItem(false);
            EnablePrevNextRecordButtons();

            this.EnableControls(true);

        }

#if NO
        // ���ݵ�ǰbookitem�����������е�λ�ã�����PrevRecord��NextRecord��ť��Enabled״̬
        void EnablePrevNextRecordButtons()
        {
            // �вο���¼�����
            if (this.IssueItem != null
                && this.IssueItem.Error != null)
            {
                goto DISABLE_TWO_BUTTON;
            }


            if (this.IssueControl == null)
            {
                // ��Ϊû�������������޷�prev/next�����Ǿ�diable
                goto DISABLE_TWO_BUTTON;
            }

            int nIndex = 0;

            nIndex = this.IssueControl.IndexOfVisibleItems(this.IssueItem);

            if (nIndex == -1)
            {
                // ��Ȼ��������û���ҵ�
                // Debug.Assert(false, "BookItem�����Ȼ��������û���ҵ���");
                goto DISABLE_TWO_BUTTON;
            }

            this.button_editing_prevRecord.Enabled = true;
            this.button_editing_nextRecord.Enabled = true;

            if (nIndex == 0)
            {
                this.button_editing_prevRecord.Enabled = false;
            }

            if (nIndex >= this.IssueControl.CountOfVisibleItems() - 1)
            {
                this.button_editing_nextRecord.Enabled = false;
            }

            return;
        DISABLE_TWO_BUTTON:
            this.button_editing_prevRecord.Enabled = false;
            this.button_editing_nextRecord.Enabled = false;
            return;
        }
#endif

        private void issueEditControl_editing_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            SetOkButtonState();
        }

        private void issueEditControl_editing_ControlKeyDown(object sender, ControlKeyEventArgs e)
        {
            bool bUp = false;
            if (e.e.KeyCode == Keys.OemOpenBrackets && e.e.Control == true)
            {
                bUp = true; // �����濽��
            }
            else if (e.e.KeyCode == Keys.OemCloseBrackets && e.e.Control == true)
            {
                bUp = false;    // �����濽��
            }
            else
                return;

            string strError = "";
            IssueItem issueitem = GetPrevOrNextItem(bUp, out strError);
            if (issueitem == null)
                return;
            switch (e.Name)
            {
                case "PublishTime":
                    this.issueEditControl_editing.PublishTime = issueitem.PublishTime;
                    break;
                case "State":
                    this.issueEditControl_editing.State = issueitem.State;
                    break;
                case "Issue":
                    this.issueEditControl_editing.Issue = issueitem.Issue;
                    break;
                case "Zong":
                    this.issueEditControl_editing.Zong = issueitem.Zong;
                    break;
                case "Volume":
                    this.issueEditControl_editing.Volume = issueitem.Volume;
                    break;
                case "OrderInfo":
                    this.issueEditControl_editing.OrderInfo = issueitem.OrderInfo;
                    break;
                case "Comment":
                    this.issueEditControl_editing.Comment = issueitem.Comment;
                    break;
                case "BatchNo":
                    this.issueEditControl_editing.BatchNo = issueitem.BatchNo;
                    break;
                case "RecPath":
                    //this.entityEditControl_editing.RecPath = bookitem.RecPath;
                    break;
                default:
                    Debug.Assert(false, "δ֪����Ŀ���� '" + e.Name + "'");
                    return;
            }

        }

#if NO
        // ��ȡֵ�б�ʱ��Ϊ���������ݿ���
        public string BiblioDbName
        {
            get
            {
                return this.issueEditControl_editing.BiblioDbName;
            }
            set
            {
                this.issueEditControl_editing.BiblioDbName = value;
                this.issueEditControl_existing.BiblioDbName = value;
            }
        }

        IssueItem GetPrevOrNextIssueItem(bool bPrev,
            out string strError)
        {
            strError = "";

            if (this.IssueControl == null)
            {
                strError = "û������";
                goto ERROR1;
            }

            int nIndex = this.IssueControl.IndexOfVisibleItems(this.IssueItem);
            if (nIndex == -1)
            {
                // ��Ȼ��������û���ҵ�
                strError = "IssueItem�����Ȼ��������û���ҵ���";
                Debug.Assert(false, strError);
                goto ERROR1;
            }

            if (bPrev == true)
                nIndex--;
            else
                nIndex++;

            if (nIndex <= -1)
            {
                strError = "��ͷ";
                goto ERROR1;
            }

            if (nIndex >= this.IssueControl.CountOfVisibleItems())
            {
                strError = "��β";
                goto ERROR1;
            }

            return this.IssueControl.GetVisibleItemAt(nIndex);
        ERROR1:
            return null;
        }
#endif
    }

    /// <summary>
    /// �ڼ�¼�༭�Ի���Ļ�����
    /// </summary>
    public class IssueEditFormBase : ItemEditFormBase<IssueItem, IssueItemCollection>
    {
    }
}