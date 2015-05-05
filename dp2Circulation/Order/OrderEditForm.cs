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
using DigitalPlatform.IO;

namespace dp2Circulation
{
    /// <summary>
    /// ������¼�༭�Ի���
    /// </summary>
    public partial class OrderEditForm : OrderEditFormBase
        // ItemEditFormBase<OrderItem, OrderItemCollection>
    {
#if NO
        /// <summary>
        /// ��ʼ����
        /// </summary>
        public OrderItem StartOrderItem = null;   // �ʼʱ�Ķ���

        /// <summary>
        /// ��ǰ����
        /// </summary>
        public OrderItem OrderItem = null;

        /// <summary>
        /// �����
        /// </summary>
        public OrderItemCollection OrderItems = null;

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// �����ؼ�
        /// </summary>
        public OrderControl OrderControl = null;
#endif

        /// <summary>
        /// ���캯��
        /// </summary>
        public OrderEditForm()
        {
            InitializeComponent();

            _editing = this.orderEditControl_editing;
            _existing = this.orderEditControl_existing;

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
        //      bookitems   ����������UndoMaskDelete
        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="orderitem">Ҫ�༭�Ķ�������</param>
        /// <param name="orderitems">�������ڵļ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int InitialForEdit(
            OrderItem orderitem,
            OrderItemCollection orderitems,
            out string strError)
        {
            strError = "";

            this.OrderItem = orderitem;
            this.OrderItems = orderitems;

            this.StartOrderItem = orderitem;

            return 0;
        }

        void LoadOrderItem(OrderItem orderitem)
        {
            if (orderitem != null)
            {
                string strError = "";
                int nRet = FillEditing(orderitem, out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, "LoadOrderItem() ��������: " + strError);
                    return;
                }
            }
            if (orderitem != null
                && orderitem.ItemDisplayState == ItemDisplayState.Deleted)
            {
                // �Ѿ����ɾ��������, ���ܽ����޸ġ����ǿ��Թ۲�
                this.orderEditControl_editing.SetReadOnly(ReadOnlyStyle.All);
                this.checkBox_autoSearchDup.Enabled = false;

                this.button_editing_undoMaskDelete.Enabled = true;
                this.button_editing_undoMaskDelete.Visible = true;
            }
            else
            {
                this.orderEditControl_editing.SetReadOnly(ReadOnlyStyle.Librarian);

                this.button_editing_undoMaskDelete.Enabled = false;
                this.button_editing_undoMaskDelete.Visible = false;
            }

            this.orderEditControl_editing.GetValueTable -= new GetValueTableEventHandler(orderEditControl_editing_GetValueTable);
            this.orderEditControl_editing.GetValueTable += new GetValueTableEventHandler(orderEditControl_editing_GetValueTable);

            this.OrderItem = orderitem;

            SetOkButtonState();
        }


        void orderEditControl_editing_GetValueTable(object sender, GetValueTableEventArgs e)
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
            if (this.OrderItem != this.StartOrderItem)
            {
                this.button_OK.Enabled = orderEditControl_editing.Changed;
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
        int FinishOneOrderItem(out string strError)
        {
            strError = "";
            int nRet = 0;

            if (orderEditControl_editing.Changed == false)
                return 0;

#if NO
            string strIndex = this.orderEditControl_editing.Index;

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
        int FillEditing(OrderItem orderitem,
            out string strError)
        {
            strError = "";

            if (orderitem == null)
            {
                strError = "orderitem����ֵΪ��";
                return -1;
            }

            string strXml = "";
            int nRet = orderitem.BuildRecord(out strXml,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = this.orderEditControl_editing.SetData(strXml,
                orderitem.RecPath,
                orderitem.Timestamp,
                out strError);
            if (nRet == -1)
                return -1;


            return 0;
        }

#endif

#if NO

        // ���ο��༭��������
        int FillExisting(out string strError)
        {
            strError = "";

            if (this.OrderItem == null)
            {
                strError = "OrderItemΪ��";
                return -1;
            }

            if (this.OrderItem.Error == null)
            {
                strError = "OrderItem.ErrorΪ��";
                return -1;
            }

            this.textBox_message.Text = this.OrderItem.ErrorInfo;

            int nRet = this.orderEditControl_existing.SetData(this.OrderItem.Error.OldRecord,
                this.OrderItem.Error.OldRecPath, // NewRecPath
                this.OrderItem.Error.OldTimestamp,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        // �ӽ����и���orderitem�е�����
        // return:
        //      -1  error
        //      0   û�б�Ҫ����
        //      1   �Ѿ�����
        int Restore(out string strError)
        {
            strError = "";
            // int nRet = 0;

            if (orderEditControl_editing.Changed == false)
                return 0;

            if (this.OrderItem == null)
            {
                strError = "OrderItemΪ��";
                return -1;
            }


            // TODO: �Ƿ����checkboxΪfalse��ʱ������ҲҪ��鱾��֮����ظ����Σ�
            // ������ﲻ��飬�ɷ����ύ�����ʱ���Ȳ��걾��֮����ظ�����������������ύ?
            if (this.checkBox_autoSearchDup.Checked == true
                && this.OrderControl != null)
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

                this.OrderItem.RecordDom = this.orderEditControl_editing.DataDom;
            }
            catch (Exception ex)
            {
                strError = "�������ʱ����: " + ex.Message;
                return -1;
            }

            this.OrderItem.Changed = true;
            if (this.OrderItem.ItemDisplayState != ItemDisplayState.New)
            {
                this.OrderItem.ItemDisplayState = ItemDisplayState.Changed;
                // ����ζ��Deleted״̬Ҳ�ᱻ�޸�ΪChanged
            }

            this.OrderItem.RefreshListView();
            return 1;
        }


        /// <summary>
        /// �Ƿ��Զ�����
        /// </summary>
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

        void LoadPrevOrNextOrderItem(bool bPrev)
        {
            string strError = "";

            OrderItem new_orderitem = GetPrevOrNextOrderItem(bPrev,
                out strError);
            if (new_orderitem == null)
                goto ERROR1;

            // ���浱ǰ����
            int nRet = FinishOneOrderItem(out strError);
            if (nRet == -1)
                goto ERROR1;

            LoadOrderItem(new_orderitem);

            // ��listview�й������ɼ���Χ
            new_orderitem.HilightListViewItem(true);
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
            if (this.OrderItem != null
                && this.OrderItem.Error != null)
            {
                goto DISABLE_TWO_BUTTON;
            }


            if (this.OrderControl == null)
            {
                // ��Ϊû�������������޷�prev/next�����Ǿ�diable
                goto DISABLE_TWO_BUTTON;
            }

            int nIndex = 0;

            nIndex = this.OrderControl.IndexOfVisibleItems(this.OrderItem);

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

            if (nIndex >= this.OrderControl.CountOfVisibleItems() - 1)
            {
                this.button_editing_nextRecord.Enabled = false;
            }

            return;
        DISABLE_TWO_BUTTON:
            this.button_editing_prevRecord.Enabled = false;
            this.button_editing_nextRecord.Enabled = false;
            return;
        }

        OrderItem GetPrevOrNextOrderItem(bool bPrev,
    out string strError)
        {
            strError = "";

            if (this.OrderControl == null)
            {
                strError = "û������";
                goto ERROR1;
            }

            int nIndex = this.OrderControl.IndexOfVisibleItems(this.OrderItem);
            if (nIndex == -1)
            {
                // ��Ȼ��������û���ҵ�
                strError = "OrderItem�����Ȼ��������û���ҵ���";
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

            if (nIndex >= this.OrderControl.CountOfVisibleItems())
            {
                strError = "��β";
                goto ERROR1;
            }

            return this.OrderControl.GetVisibleItemAt(nIndex);
        ERROR1:
            return null;
        }

#endif

        private void OrderEditForm_Load(object sender, EventArgs e)
        {
#if NO
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
            LoadOrderItem(this.OrderItem);
            EnablePrevNextRecordButtons();

            // �ο���¼
            if (this.OrderItem != null
                && this.OrderItem.Error != null)
            {

                this.splitContainer_main.Panel1Collapsed = false;

                string strError = "";
                int nRet = FillExisting(out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);


                this.orderEditControl_existing.SetReadOnly(ReadOnlyStyle.All);

                // ͻ����������
                this.orderEditControl_editing.HighlightDifferences(this.orderEditControl_existing);

            }
            else
            {
                this.tableLayoutPanel_main.RowStyles[0].Height = 0F;
                this.textBox_message.Visible = false;

                this.label_editing.Visible = false;
                this.splitContainer_main.Panel1Collapsed = true;
                this.orderEditControl_existing.Enabled = false;
            }
#endif
        }

        private void OrderEditForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            /*
            this.orderEditControl_editing.GetValueTable -= new GetValueTableEventHandler(orderEditControl_editing_GetValueTable);
             * */
        }

        /// <summary>
        /// ����ǰ��У��
        /// </summary>
        /// <param name="strError">���ش�����Ϣ</param>
        /// <returns>-1: ����; 0: û�д���</returns>
        internal override int FinishVerify(out string strError)
        {
            strError = "";
            int nRet = 0;

            string strRange = this.orderEditControl_editing.Range;
            string strOrderTime = this.orderEditControl_editing.OrderTime;

            if (string.IsNullOrEmpty(strRange) == false)
            {
                // ������ʱ�䷶Χ�ַ����Ƿ�Ϸ�
                // ���ʹ�õ�������ʱ�������ñ�������Ҳ�ǿ��Ե�
                // return:
                //      -1  ����
                //      0   ��ȷ
                nRet = LibraryServerUtil.CheckPublishTimeRange(strRange,
                    out strError);
                if (nRet == -1)
                {
                    goto ERROR1;
                }
            }

            if (string.IsNullOrEmpty(strOrderTime) == false)
            {
                try
                {
                    DateTime time = DateTimeUtil.FromRfc1123DateTimeString(strOrderTime);
                    if (time.Year == 1753)
                    {
                        strError = "����ʱ���ַ��� '" + strOrderTime + "' ����һ����̫���ܵ�ʱ��";
                        goto ERROR1;
                    }
                }
                catch (Exception ex)
                {
                    strError = "����ʱ���ַ��� '" + strOrderTime + "' ��ʽ����: " + ex.Message;
                    goto ERROR1;
                }
            }

            // TODO: ��֤�ݲط����ַ���

            return 0;
        ERROR1:
            return -1;
        }


        private void button_OK_Click(object sender, EventArgs e)
        {
#if NO
            string strError = "";
            int nRet = 0;


            nRet = this.FinishOneOrderItem(out strError);
            if (nRet == -1)
                goto ERROR1;

            // TODO: �ύ�����timestamp��ƥ��ʱ���ֵĶԻ���Ӧ����ֹprev/next��ť

            // ����б�����Ϣ�����
            if (this.OrderItem != null
                && this.OrderItem.Error != null
                && this.OrderItem.Error.ErrorCode == DigitalPlatform.CirculationClient.localhost.ErrorCodeValue.TimestampMismatch)
            {
                this.OrderItem.OldRecord = this.OrderItem.Error.OldRecord;
                this.OrderItem.Timestamp = this.OrderItem.Error.OldTimestamp;
            }

            this.OrderItem.Error = null; // ��������״̬

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

        private void button_existing_undoMaskDelete_Click(object sender, EventArgs e)
        {
        }

        private void button_editing_undoMaskDelete_Click(object sender, EventArgs e)
        {
            if (this.Items != null)
            {
                this.Items.UndoMaskDeleteItem(this.Item);
                this.orderEditControl_editing.SetReadOnly("librarian");
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

        private void orderEditControl_editing_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            SetOkButtonState();
        }

        private void orderEditControl_editing_ControlKeyDown(object sender, ControlKeyEventArgs e)
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
            OrderItem orderitem = GetPrevOrNextItem(bUp, out strError);
            if (orderitem == null)
                return;
            switch (e.Name)
            {
                case "Index":
                    this.orderEditControl_editing.Index = orderitem.Index;
                    break;
                case "State":
                    this.orderEditControl_editing.State = orderitem.State;
                    break;
                case "Seller":
                    this.orderEditControl_editing.Seller = orderitem.Seller;
                    break;
                case "Range":
                    this.orderEditControl_editing.Range = orderitem.Range;
                    break;
                case "Copy":
                    this.orderEditControl_editing.Copy = orderitem.Copy;
                    break;
                case "Price":
                    this.orderEditControl_editing.Price = orderitem.Price;
                    break;
                case "TotalPrice":
                    this.orderEditControl_editing.TotalPrice = orderitem.TotalPrice;
                    break;
                case "OrderTime":
                    this.orderEditControl_editing.OrderTime = orderitem.OrderTime;
                    break;
                case "OrderID":
                    this.orderEditControl_editing.OrderID = orderitem.OrderID;
                    break;
                case "Distribute":
                    this.orderEditControl_editing.Distribute = orderitem.Distribute;
                    break;


                case "Comment":
                    this.orderEditControl_editing.Comment = orderitem.Comment;
                    break;
                case "BatchNo":
                    this.orderEditControl_editing.BatchNo = orderitem.BatchNo;
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
        /// <summary>
        /// ��Ŀ������
        /// ��ȡֵ�б�ʱ��Ϊ���������ݿ���
        /// </summary>
        public string BiblioDbName
        {
            get
            {
                return this.orderEditControl_editing.BiblioDbName;
            }
            set
            {
                this.orderEditControl_editing.BiblioDbName = value;
                this.orderEditControl_existing.BiblioDbName = value;
            }
        }
#endif
    }

    /// <summary>
    /// ������¼�༭�Ի���Ļ�����
    /// </summary>
    public class OrderEditFormBase : ItemEditFormBase<OrderItem, OrderItemCollection>
    {
    }
}