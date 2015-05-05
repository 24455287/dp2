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

namespace dp2Circulation
{
    /// <summary>
    /// ��ע��¼�༭�Ի���
    /// </summary>
    public partial class CommentEditForm : CommentEditFormBase
        // ItemEditFormBase<CommentItem, CommentItemCollection>
    {
#if NO
        public CommentItem StartCommentItem = null;   // �ʼʱ�Ķ���

        public CommentItem CommentItem = null;

        public CommentItemCollection CommentItems = null;

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        public CommentControl CommentControl = null;
#endif

        /// <summary>
        /// ���캯��
        /// </summary>
        public CommentEditForm()
        {
            InitializeComponent();

            _editing = this.commentEditControl_editing;
            _existing = this.commentEditControl_existing;

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
        //      commentitems   ����������UndoMaskDelete
        public int InitialForEdit(
            CommentItem commentitem,
            CommentItemCollection commentitems,
            out string strError)
        {
            strError = "";

            this.CommentItem = commentitem;
            this.CommentItems = commentitems;

            this.StartCommentItem = commentitem;

            return 0;
        }

        void LoadCommentItem(CommentItem commentitem)
        {
            if (commentitem != null)
            {
                string strError = "";
                int nRet = FillEditing(commentitem, out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, "LoadCommentItem() ��������: " + strError);
                    return;
                }
            }
            if (commentitem != null
                && commentitem.ItemDisplayState == ItemDisplayState.Deleted)
            {
                // �Ѿ����ɾ��������, ���ܽ����޸ġ����ǿ��Թ۲�
                this.commentEditControl_editing.SetReadOnly(ReadOnlyStyle.All);
                this.checkBox_autoSearchDup.Enabled = false;

                this.button_editing_undoMaskDelete.Enabled = true;
                this.button_editing_undoMaskDelete.Visible = true;
            }
            else
            {
                this.commentEditControl_editing.SetReadOnly(ReadOnlyStyle.Librarian);

                this.button_editing_undoMaskDelete.Enabled = false;
                this.button_editing_undoMaskDelete.Visible = false;
            }

            this.commentEditControl_editing.GetValueTable -= new GetValueTableEventHandler(commentEditControl_editing_GetValueTable);
            this.commentEditControl_editing.GetValueTable += new GetValueTableEventHandler(commentEditControl_editing_GetValueTable);

            this.CommentItem = commentitem;

            SetOkButtonState();
        }

        private void commentEditControl_editing_GetValueTable(object sender, GetValueTableEventArgs e)
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
            if (this.CommentItem != this.StartCommentItem)
            {
                this.button_OK.Enabled = commentEditControl_editing.Changed;
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
        int FinishOneCommentItem(out string strError)
        {
            strError = "";
            int nRet = 0;

            if (commentEditControl_editing.Changed == false)
                return 0;

#if NO
            string strIndex = this.commentEditControl_editing.Index;

            // TODOL �������ʽ�Ƿ�Ϸ�
            if (String.IsNullOrEmpty(strIndex) == true)
            {
                strError = "��Ų���Ϊ��";
                goto ERROR1;
            }
#endif

            nRet = Restore(out strError);
            if (nRet == -1)
                goto ERROR1;

            return nRet;
        ERROR1:
            return -1;
        }

        // ���༭��������
        int FillEditing(CommentItem commentitem,
            out string strError)
        {
            strError = "";

            if (commentitem == null)
            {
                strError = "commentitem����ֵΪ��";
                return -1;
            }

            string strXml = "";
            int nRet = commentitem.BuildRecord(out strXml,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = this.commentEditControl_editing.SetData(strXml,
                commentitem.RecPath,
                commentitem.Timestamp,
                out strError);
            if (nRet == -1)
                return -1;


            return 0;
        }

        // ���ο��༭��������
        int FillExisting(out string strError)
        {
            strError = "";

            if (this.CommentItem == null)
            {
                strError = "CommentItemΪ��";
                return -1;
            }

            if (this.CommentItem.Error == null)
            {
                strError = "CommentItem.ErrorΪ��";
                return -1;
            }

            this.textBox_message.Text = this.CommentItem.ErrorInfo;

            int nRet = this.commentEditControl_existing.SetData(this.CommentItem.Error.OldRecord,
                this.CommentItem.Error.OldRecPath, // NewRecPath
                this.CommentItem.Error.OldTimestamp,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        // �ӽ����и���Commtentitem�е�����
        // return:
        //      -1  error
        //      0   û�б�Ҫ����
        //      1   �Ѿ�����
        int Restore(out string strError)
        {
            strError = "";
            // int nRet = 0;

            if (commentEditControl_editing.Changed == false)
                return 0;

            if (this.CommentItem == null)
            {
                strError = "CommentItemΪ��";
                return -1;
            }


            // TODO: �Ƿ����checkboxΪfalse��ʱ������ҲҪ��鱾��֮����ظ����Σ�
            // ������ﲻ��飬�ɷ����ύ�����ʱ���Ȳ��걾��֮����ظ�����������������ύ?
            if (this.checkBox_autoSearchDup.Checked == true
                && this.CommentControl != null)
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

                this.CommentItem.RecordDom = this.commentEditControl_editing.DataDom;
            }
            catch (Exception ex)
            {
                strError = "�������ʱ����: " + ex.Message;
                return -1;
            }

            this.CommentItem.Changed = true;
            if (this.CommentItem.ItemDisplayState != ItemDisplayState.New)
            {
                this.CommentItem.ItemDisplayState = ItemDisplayState.Changed;
                // ����ζ��Deleted״̬Ҳ�ᱻ�޸�ΪChanged
            }

            this.CommentItem.RefreshListView();

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

        void LoadPrevOrNextCommentItem(bool bPrev)
        {
            string strError = "";

            CommentItem new_commentitem = GetPrevOrNextCommentItem(bPrev,
                out strError);
            if (new_commentitem == null)
                goto ERROR1;

            // ���浱ǰ����
            int nRet = FinishOneCommentItem(out strError);
            if (nRet == -1)
                goto ERROR1;

            LoadCommentItem(new_commentitem);

            // ��listview�й������ɼ���Χ
            new_commentitem.HilightListViewItem(true);
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

        // ���ݵ�ǰbookitem�����������е�λ�ã�����PrevRecord��NextRecord��ť��Enabled״̬
        void EnablePrevNextRecordButtons()
        {
            // �вο���¼�����
            if (this.CommentItem != null
                && this.CommentItem.Error != null)
            {
                goto DISABLE_TWO_BUTTON;
            }


            if (this.CommentControl == null)
            {
                // ��Ϊû�������������޷�prev/next�����Ǿ�diable
                goto DISABLE_TWO_BUTTON;
            }

            int nIndex = 0;

            nIndex = this.CommentControl.IndexOfVisibleItems(this.CommentItem);

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

            if (nIndex >= this.CommentControl.CountOfVisibleItems() - 1)
            {
                this.button_editing_nextRecord.Enabled = false;
            }

            return;
        DISABLE_TWO_BUTTON:
            this.button_editing_prevRecord.Enabled = false;
            this.button_editing_nextRecord.Enabled = false;
            return;
        }

        CommentItem GetPrevOrNextCommentItem(bool bPrev,
    out string strError)
        {
            strError = "";

            if (this.CommentControl == null)
            {
                strError = "û������";
                goto ERROR1;
            }

            int nIndex = this.CommentControl.IndexOfVisibleItems(this.CommentItem);
            if (nIndex == -1)
            {
                // ��Ȼ��������û���ҵ�
                strError = "CommentItem�����Ȼ��������û���ҵ���";
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

            if (nIndex >= this.CommentControl.CountOfVisibleItems())
            {
                strError = "��β";
                goto ERROR1;
            }

            return this.CommentControl.GetVisibleItemAt(nIndex);
        ERROR1:
            return null;
        }
#endif


        private void CommentEditForm_Load(object sender, EventArgs e)
        {
#if NO
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }

            LoadCommentItem(this.CommentItem);
            EnablePrevNextRecordButtons();

            // �ο���¼
            if (this.CommentItem != null
                && this.CommentItem.Error != null)
            {

                this.splitContainer_main.Panel1Collapsed = false;

                string strError = "";
                int nRet = FillExisting(out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);


                this.commentEditControl_existing.SetReadOnly(ReadOnlyStyle.All);

                // ͻ����������
                this.commentEditControl_editing.HighlightDifferences(this.commentEditControl_existing);

            }
            else
            {
                this.tableLayoutPanel_main.RowStyles[0].Height = 0F;
                this.textBox_message.Visible = false;

                this.label_editing.Visible = false;
                this.splitContainer_main.Panel1Collapsed = true;
                this.commentEditControl_existing.Enabled = false;
            }
#endif
        }

        private void CommentEditForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            this.commentEditControl_editing.GetValueTable -= new GetValueTableEventHandler(commentEditControl_editing_GetValueTable);
        
#endif
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
#if NO
            string strError = "";
            int nRet = 0;


            nRet = this.FinishOneCommentItem(out strError);
            if (nRet == -1)
                goto ERROR1;

            // TODO: �ύ�����timestamp��ƥ��ʱ���ֵĶԻ���Ӧ����ֹprev/next��ť

            // ����б�����Ϣ�����
            if (this.CommentItem != null
                && this.CommentItem.Error != null
                && this.CommentItem.Error.ErrorCode == DigitalPlatform.CirculationClient.localhost.ErrorCodeValue.TimestampMismatch)
            {
                this.CommentItem.OldRecord = this.CommentItem.Error.OldRecord;
                this.CommentItem.Timestamp = this.CommentItem.Error.OldTimestamp;
            }

            this.CommentItem.Error = null; // ��������״̬

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

        private void button_editing_undoMaskDelete_Click(object sender, EventArgs e)
        {
            if (this.Items != null)
            {
                this.Items.UndoMaskDeleteItem(this.Item);
                this.commentEditControl_editing.SetReadOnly("librarian");
                this.checkBox_autoSearchDup.Enabled = true;
                // this.button_OK.Enabled = entityEditControl_editing.Changed;
            }
        }

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

        private void commentEditControl_editing_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            SetOkButtonState();
        }

        private void commentEditControl_editing_ControlKeyDown(object sender, ControlKeyEventArgs e)
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
            CommentItem commentitem = GetPrevOrNextItem(bUp, out strError);
            if (commentitem == null)
                return;
            switch (e.Name)
            {
                case "Index":
                    this.commentEditControl_editing.Index = commentitem.Index;
                    break;
                case "State":
                    this.commentEditControl_editing.State = commentitem.State;
                    break;
                case "Type":
                    this.commentEditControl_editing.TypeString = commentitem.TypeString;
                    break;
                case "Title":
                    this.commentEditControl_editing.Title = commentitem.Title;
                    break;
                case "Author":
                    this.commentEditControl_editing.Creator = commentitem.Creator;
                    break;
                case "Subject":
                    this.commentEditControl_editing.Subject = commentitem.Subject;
                    break;
                case "Summary":
                    this.commentEditControl_editing.Summary = commentitem.Summary;
                    break;
                case "Content":
                    this.commentEditControl_editing.Content = commentitem.Content;
                    break;
                case "CreateTime":
                    this.commentEditControl_editing.CreateTime = commentitem.CreateTime;
                    break;
                case "LastModified":
                    this.commentEditControl_editing.LastModified = commentitem.LastModified;
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
                return this.commentEditControl_editing.BiblioDbName;
            }
            set
            {
                this.commentEditControl_editing.BiblioDbName = value;
                this.commentEditControl_existing.BiblioDbName = value;
            }
        }
#endif
    }

    /// <summary>
    /// ��ע��¼�༭�Ի���Ļ�����
    /// </summary>
    public class CommentEditFormBase : ItemEditFormBase<CommentItem, CommentItemCollection>
    {
    }
}