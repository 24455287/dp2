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
using DigitalPlatform.Text;
using DigitalPlatform.LibraryServer;

namespace dp2Circulation
{
    /// <summary>
    /// ���¼�༭�Ի���
    /// </summary>
    public partial class EntityEditForm : EntityEditFormBase
        // ItemEditFormBase<BookItem, BookItemCollection>
    {
        /*
        // ������ȡ��
        public event GenerateDataEventHandler GenerateAccessNo = null;
         * */

        // Ctrl+A�Զ���������
        /// <summary>
        /// �Զ���������
        /// </summary>
        public event GenerateDataEventHandler GenerateData = null;

#if NO
        /// <summary>
        /// �ʼʱ�� BookItem ����
        /// </summary>
        public BookItem StartBookItem = null;   // �ʼʱ�Ķ���

        /// <summary>
        /// ��ǰ BookItem ����
        /// </summary>
        public BookItem BookItem = null;

        /// <summary>
        /// BookItem ����
        /// </summary>
        public BookItemCollection BookItems = null;

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// ������ EntityControl ����
        /// </summary>
        public EntityControl EntityControl = null;
#endif

        /// <summary>
        /// ���캯��
        /// </summary>
        public EntityEditForm()
        {
            InitializeComponent();

            _editing = this.entityEditControl_editing;
            _existing = this.entityEditControl_existing;

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

        /// <summary>
        /// �༭����ʾģʽ
        /// </summary>
        public string DisplayMode
        {
            get
            {
                return this.entityEditControl_editing.DisplayMode;
            }
            set
            {
                this.entityEditControl_editing.DisplayMode = value;
                this.entityEditControl_existing.DisplayMode = value;
            }
        }

        /// <summary>
        /// ��ǰ��¼�ı༭��
        /// </summary>
        public EntityEditControl Editing
        {
            get
            {
                return entityEditControl_editing;
            }
        }

        /// <summary>
        /// �Ѵ��ڼ�¼�ı༭��
        /// </summary>
        public EntityEditControl Existing
        {
            get
            {
                return entityEditControl_existing;
            }
        }

#if NO
        // Ϊ�༭Ŀ�ĵĳ�ʼ��
        // parameters:
        //      bookitems   ����������UndoMaskDelete
        /// <summary>
        /// Ϊ�༭����ʼ��
        /// </summary>
        /// <param name="bookitem">Ҫ�༭�� BookItem ����</param>
        /// <param name="bookitems">�������� BookItem ���ϡ�����ǰ�󷭶��༭</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int InitialForEdit(
            BookItem bookitem,
            BookItemCollection bookitems,
            out string strError)
        {
            strError = "";

            this.BookItem = bookitem;
            this.BookItems = bookitems;

            this.StartBookItem = bookitem;

            return 0;
        }
#endif

        /// <summary>
        /// ��ȡ��ȡ�������
        /// </summary>
        /// <returns>CallNumberItem �����</returns>
        public List<CallNumberItem> GetCallNumberItems()
        {
            List<CallNumberItem> callnumber_items = this.Items.GetCallNumberItems();

            CallNumberItem item = null;

            int index = this.Items.IndexOf(this.Item);
            if (index == -1)
            {
                // ����һ������
                item = new CallNumberItem();
                callnumber_items.Add(item);

                item.CallNumber = "";   // ��Ҫ������ǰ�ģ�����Ӱ�쵽ȡ�Ž��
            }
            else
            {
                // ˢ���Լ���λ��
                item = callnumber_items[index];
                item.CallNumber = entityEditControl_editing.AccessNo;
            }

            item.RecPath = this.entityEditControl_editing.RecPath;
            item.Location = entityEditControl_editing.LocationString;
            item.Barcode = entityEditControl_editing.Barcode;

            return callnumber_items;
        }


        private void EntityEditForm_Load(object sender, EventArgs e)
        {
#if NO
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }

            LoadBookItem(this.BookItem);
            EnablePrevNextRecordButtons();

            // �ο���¼
            if (this.BookItem != null
                && this.BookItem.Error != null)
            {

                this.splitContainer_main.Panel1Collapsed = false;

                string strError = "";
                int nRet = FillExisting(out strError);
                if (nRet == -1)
                    MessageBox.Show(this, strError);


                this.entityEditControl_existing.SetReadOnly(ReadOnlyStyle.All);

                // ͻ����������
                this.entityEditControl_editing.HighlightDifferences(this.entityEditControl_existing);

            }
            else
            {
                this.tableLayoutPanel_main.RowStyles[0].Height = 0F;
                this.textBox_message.Visible = false;

                this.label_editing.Visible = false;
                this.splitContainer_main.Panel1Collapsed = true;
                this.entityEditControl_existing.Enabled = false;
            }
#endif

            this.entityEditControl_editing.GetAccessNoButton.Click -= new EventHandler(button_getAccessNo_Click);
            this.entityEditControl_editing.GetAccessNoButton.Click += new EventHandler(button_getAccessNo_Click);

            this.entityEditControl_editing.LocationStringChanged -= new TextChangeEventHandler(entityEditControl_editing_LocationStringChanged);
            this.entityEditControl_editing.LocationStringChanged += new TextChangeEventHandler(entityEditControl_editing_LocationStringChanged);
        }

        void entityEditControl_editing_LocationStringChanged(object sender, TextChangeEventArgs e)
        {
            string strError = "";

            if (this.entityEditControl_editing.Initializing == false
                && string.IsNullOrEmpty(this.entityEditControl_editing.AccessNo) == false)
            {
                // MessageBox.Show(this, "�޸� old '"+e.OldText+"' new '"+e.NewText+"'" );

                ArrangementInfo old_info = null;
                // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
                int nRet = this.MainForm.GetArrangementInfo(e.OldText,
                    out old_info,
                    out strError);
                if (nRet == 0)
                    return;
                if (nRet == -1)
                    goto ERROR1;

                ArrangementInfo new_info = null;
                // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
                nRet = this.MainForm.GetArrangementInfo(e.NewText,
                   out new_info,
                   out strError);
                if (nRet == 0)
                    return;
                if (nRet == -1)
                    goto ERROR1;

                if (old_info.ArrangeGroupName != new_info.ArrangeGroupName)
                {
                    DialogResult result = MessageBox.Show(this,
    "���޸��˹ݲصص㣬����䶯�˼�¼���������ż���ϵ�����е���ȡ���Ѳ����ʺϱ䶯����ż���ϵ��\r\n\r\n�Ƿ�Ҫ�Ѵ�������ȡ���ֶ�������գ��Ա����Ժ����´�����ȡ��?",
    "EntityEditForm",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button1);
                    if (result == DialogResult.No)
                        return;
                    this.entityEditControl_editing.AccessNo = "";
                }
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �����ȡ��
        void button_getAccessNo_Click(object sender, EventArgs e)
        {

            if (this.GenerateData != null)
            {
                GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                if (Control.ModifierKeys == Keys.Control)
                    e1.ScriptEntry = "ManageCallNumber";
                else
                    e1.ScriptEntry = "CreateCallNumber";
                e1.FocusedControl = sender; // senderΪ��ԭʼ���ӿؼ�
                this.GenerateData(this, e1);
            }


            /*
            if (this.GenerateAccessNo != null)
            {
                GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                e1.FocusedControl = this.entityEditControl_editing.textBox_accessNo;
                this.GenerateAccessNo(this, e1);
            }*/

        }

#if NO
        void LoadBookItem(BookItem bookitem)
        {
            if (bookitem != null)
            {
                string strError = "";
                int nRet = FillEditing(bookitem, out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, "LoadBookItem() ��������: " + strError);
                    return;
                }
            }
            if (bookitem != null
                && bookitem.ItemDisplayState == ItemDisplayState.Deleted)
            {
                // �Ѿ����ɾ��������, ���ܽ����޸ġ����ǿ��Թ۲�
                this.entityEditControl_editing.SetReadOnly(ReadOnlyStyle.All);
                this.checkBox_autoSearchDup.Enabled = false;

                this.button_editing_undoMaskDelete.Enabled = true;
                this.button_editing_undoMaskDelete.Visible = true;
            }
            else
            {
                this.entityEditControl_editing.SetReadOnly(ReadOnlyStyle.Librarian);

                this.button_editing_undoMaskDelete.Enabled = false;
                this.button_editing_undoMaskDelete.Visible = false;
            }

            this.entityEditControl_editing.GetValueTable -= new GetValueTableEventHandler(entityEditControl1_GetValueTable);
            this.entityEditControl_editing.GetValueTable += new GetValueTableEventHandler(entityEditControl1_GetValueTable);

            this.BookItem = bookitem;

            SetOkButtonState();
        }

        void SetOkButtonState()
        {
            if (this.BookItem != this.StartBookItem)
            {
                this.button_OK.Enabled = entityEditControl_editing.Changed;
            }
            else
            {
                this.button_OK.Enabled = true;
                // this.button_Cancel.Text = "ȡ��";
            }
        }

        void entityEditControl1_GetValueTable(object sender, GetValueTableEventArgs e)
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
#endif

        private void EntityEditForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            this.entityEditControl_editing.GetValueTable -= new GetValueTableEventHandler(entityEditControl1_GetValueTable);
#endif
        }

#if NO
        // ����һ������ı༭
        // return:
        //      -1  ����
        //      0   û�б�Ҫ��restore
        //      1   ����restore
        int FinishOneBookItem(out string strError)
        {
            strError = "";
            int nRet = 0;

            if (entityEditControl_editing.Changed == false)
                return 0;

            string strBarcode = this.entityEditControl_editing.Barcode;

            // �����������ʽ�Ƿ�Ϸ�
            if (String.IsNullOrEmpty(strBarcode) == false   // 2009/2/23 
                && this.EntityControl != null
                && this.EntityControl.NeedVerifyItemBarcode == true)
            {
                // ��ʽУ�������
                // return:
                //      -2  ������û������У�鷽�����޷�У��
                //      -1  error
                //      0   ���ǺϷ��������
                //      1   �ǺϷ��Ķ���֤�����
                //      2   �ǺϷ��Ĳ������
                nRet = this.EntityControl.DoVerifyBarcode(
                    strBarcode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ����������ʽ���Ϸ�
                if (nRet == 0)
                {
                    strError = "����������� " + strBarcode + " ��ʽ����ȷ("+strError+")�����������롣";
                    goto ERROR1;
                }

                // ʵ��������Ƕ���֤�����
                if (nRet == 1)
                {
                    strError = "������������ " + strBarcode + " �Ƕ���֤����š������������š�";
                    goto ERROR1;
                }

                // ���ڷ�����û������У�鹦�ܣ�����ǰ�˷�����У��Ҫ������������һ��
                if (nRet == -2)
                    MessageBox.Show(this, "���棺ǰ�˲����������У�����빦�ܣ����Ƿ�������ȱ����Ӧ�Ľű��������޷�У�����롣\r\n\r\n��Ҫ������ִ˾���Ի�����ر�ǰ�˲����У�鹦��");

            }

            nRet = Restore(out strError);
            if (nRet == -1)
                goto ERROR1;

            return nRet;
        ERROR1:
            return -1;
        }
#endif
        EntityControl EntityControl
        {
            get
            {
                return (EntityControl)this.ItemControl;
            }
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

            string strBarcode = this.entityEditControl_editing.Barcode;

            // �����������ʽ�Ƿ�Ϸ�
            if (String.IsNullOrEmpty(strBarcode) == false   // 2009/2/23 
                && this.EntityControl != null
                && this.EntityControl.NeedVerifyItemBarcode == true)
            {
                // ��ʽУ�������
                // return:
                //      -2  ������û������У�鷽�����޷�У��
                //      -1  error
                //      0   ���ǺϷ��������
                //      1   �ǺϷ��Ķ���֤�����
                //      2   �ǺϷ��Ĳ������
                nRet = this.EntityControl.DoVerifyBarcode(
                    strBarcode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ����������ʽ���Ϸ�
                if (nRet == 0)
                {
                    strError = "����������� " + strBarcode + " ��ʽ����ȷ(" + strError + ")�����������롣";
                    goto ERROR1;
                }

                // ʵ��������Ƕ���֤�����
                if (nRet == 1)
                {
                    strError = "������������ " + strBarcode + " �Ƕ���֤����š������������š�";
                    goto ERROR1;
                }

                // ���ڷ�����û������У�鹦�ܣ�����ǰ�˷�����У��Ҫ������������һ��
                if (nRet == -2)
                    MessageBox.Show(this, "���棺ǰ�˲����������У�����빦�ܣ����Ƿ�������ȱ����Ӧ�Ľű��������޷�У�����롣\r\n\r\n��Ҫ������ִ˾���Ի�����ر�ǰ�˲����У�鹦��");
            }

            // �ݲصص��ַ������治�����Ǻ�
            string strLocation = this.entityEditControl_editing.LocationString;
            if (strLocation.IndexOf("*") != -1)
            {
                strError = "�ݲصص��ַ����в���������ַ� '*'";
                goto ERROR1;
            }


            // �۸��ַ����в�������� @
            string strPrice = this.entityEditControl_editing.Price;
            if (strPrice.IndexOf("@") != -1)
            {
                strError = "�۸��ַ����в���������ַ� '@'";
                goto ERROR1;
            }

            if (string.IsNullOrEmpty(strPrice) == false)
            {
                CurrencyItem item = null;
                // ������������ַ��������� CNY10.00 �� -CNY100.00/7
                nRet = PriceUtil.ParseSinglePrice(strPrice,
                    out item,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�۸��ַ�����ʽ���Ϸ�: " +strError;
                    goto ERROR1;
                }
            }

            string strIssueDbName = "";

            if (string.IsNullOrEmpty(this.BiblioDbName) == false)
                strIssueDbName = this.MainForm.GetIssueDbName(this.BiblioDbName);

            if (string.IsNullOrEmpty(strIssueDbName) == false)
            {
                // 2014/10/23
                if (string.IsNullOrEmpty(this.entityEditControl_editing.PublishTime) == false)
                {
                    // ������ʱ�䷶Χ�ַ����Ƿ�Ϸ�
                    // ���ʹ�õ�������ʱ�������ñ�������Ҳ�ǿ��Ե�
                    // return:
                    //      -1  ����
                    //      0   ��ȷ
                    nRet = LibraryServerUtil.CheckPublishTimeRange(this.entityEditControl_editing.PublishTime,
                        out strError);
                    if (nRet == -1)
                    {
                        goto ERROR1;
                    }
                }

                // 2014/10/23
                if (string.IsNullOrEmpty(this.entityEditControl_editing.Volume) == false)
                {
                    List<VolumeInfo> infos = null;
                    nRet = VolumeInfo.BuildVolumeInfos(this.entityEditControl_editing.Volume,
                        out infos,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "�����ַ��� '" + this.entityEditControl_editing.Volume + "' ��ʽ����: " + strError;
                        goto ERROR1;
                    }
                }
            }

            return 0;
        ERROR1:
            return -1;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
#if NO
            string strError = "";
            int nRet = 0;


            /*
            string strBarcode = this.entityEditControl_editing.Barcode;

            // �����������ʽ�Ƿ�Ϸ�
            if (this.EntityForm != null
                && this.EntityForm.NeedVerifyItemBarcode == true)
            {
                // ��ʽУ�������
                // return:
                //      -2  ������û������У�鷽�����޷�У��
                //      -1  error
                //      0   ���ǺϷ��������
                //      1   �ǺϷ��Ķ���֤�����
                //      2   �ǺϷ��Ĳ������
                nRet = this.EntityForm.VerifyBarcode(
                    strBarcode,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // ����������ʽ���Ϸ�
                if (nRet == 0)
                {
                    strError = "����������� " + strBarcode + " ��ʽ����ȷ�����������롣";
                    goto ERROR1;
                }

                // ʵ��������Ƕ���֤����
                if (nRet == 1)
                {
                    strError = "������������ " + strBarcode + " �Ƕ���֤����š������������š�";
                    goto ERROR1;
                }

                // ���ڷ�����û������У�鹦�ܣ�����ǰ�˷�����У��Ҫ������������һ��
                if (nRet == -2)
                    MessageBox.Show(this, "���棺ǰ�˲����������У�����빦�ܣ����Ƿ�������ȱ����Ӧ�Ľű��������޷�У�����롣\r\n\r\n��Ҫ������ִ˾���Ի�����ر�ǰ�˲����У�鹦��");

            }

            nRet = Restore(out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }
             * */
            nRet = this.FinishOneBookItem(out strError);
            if (nRet == -1)
                goto ERROR1;

            // TODO: �ύ�����timestamp��ƥ��ʱ���ֵĶԻ���Ӧ����ֹprev/next��ť

            // ����б�����Ϣ�����
            if (this.BookItem != null
                && this.BookItem.Error != null
                && this.BookItem.Error.ErrorCode == DigitalPlatform.CirculationClient.localhost.ErrorCodeValue.TimestampMismatch)
            {
                this.BookItem.OldRecord = this.BookItem.Error.OldRecord;
                this.BookItem.Timestamp = this.BookItem.Error.OldTimestamp;
            }

            this.BookItem.Error = null; // ��������״̬
            // this.BookItem.RefreshListView();    //  ˢ����ʾ

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
        int FillEditing(BookItem bookitem,
            out string strError)
        {
            strError = "";

            if (bookitem == null)
            {
                strError = "bookitem����ֵΪ��";
                return -1;
            }

            string strXml = "";
            int nRet = bookitem.BuildRecord(out strXml,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = this.entityEditControl_editing.SetData(strXml,
                bookitem.RecPath,
                bookitem.Timestamp,
                out strError);
            if (nRet == -1)
                return -1;


            return 0;
        }

        // ���ο��༭��������
        int FillExisting(out string strError)
        {
            strError = "";

            if (this.BookItem == null)
            {
                strError = "BookItemΪ��";
                return -1;
            }

            if (this.BookItem.Error == null)
            {
                strError = "BookItem.ErrorΪ��";
                return -1;
            }

            this.textBox_message.Text = this.BookItem.ErrorInfo;

            int nRet = this.entityEditControl_existing.SetData(this.BookItem.Error.OldRecord,
                this.BookItem.Error.OldRecPath, // NewRecPath
                this.BookItem.Error.OldTimestamp,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        // �ӽ����и���bookitem�е�����
        // return:
        //      -1  error
        //      0   û�б�Ҫ����
        //      1   �Ѿ�����
        int Restore(out string strError)
        {
            strError = "";
            int nRet = 0;

            if (entityEditControl_editing.Changed == false)
                return 0;

            if (this.BookItem == null)
            {
                strError = "BookItemΪ��";
                return -1;
            }


            // TODO: �Ƿ����checkboxΪfalse��ʱ������ҲҪ��鱾��֮����ظ����Σ�
            // ������ﲻ��飬�ɷ����ύ�����ʱ���Ȳ��걾��֮����ظ�����������������ύ?
            if (this.checkBox_autoSearchDup.Checked == true
                && this.EntityControl != null
                && String.IsNullOrEmpty(this.entityEditControl_editing.Barcode) == false)   // 2008/11/3 �����յ�������Ƿ��ظ�
            {
                // Debug.Assert(false, "");
                // �������
                // return:
                //      -1  ����
                //      0   ���ظ�
                //      1   �ظ�
                nRet = this.EntityControl.CheckBarcodeDup(
                    this.entityEditControl_editing.Barcode,
                    this.BookItem,
                    true,   // bCheckCurrentList,
                    true,   // bCheckDb,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 1)
                    return -1;   // �ظ�
            }

            // ��ñ༭�������
            try
            {
                this.BookItem.RecordDom = this.entityEditControl_editing.DataDom;
            }
            catch (Exception ex)
            {
                strError = "�������ʱ����: " + ex.Message;
                return -1;
            }

            this.BookItem.Changed = true;
            if (this.BookItem.ItemDisplayState != ItemDisplayState.New)
            {
                this.BookItem.ItemDisplayState = ItemDisplayState.Changed;
                // ����ζ��Deleted״̬Ҳ�ᱻ�޸�ΪChanged
            }

            this.BookItem.RefreshListView();

            return 1;
        }
#endif

        internal override int RestoreVerify(out string strError)
        {
            strError = "";
            int nRet = 0;

            // TODO: �Ƿ����checkboxΪfalse��ʱ������ҲҪ��鱾��֮����ظ����Σ�
            // ������ﲻ��飬�ɷ����ύ�����ʱ���Ȳ��걾��֮����ظ�����������������ύ?
            if (this.checkBox_autoSearchDup.Checked == true
                && this.EntityControl != null
                && String.IsNullOrEmpty(this.entityEditControl_editing.Barcode) == false)   // 2008/11/3 �����յ�������Ƿ��ظ�
            {
                // Debug.Assert(false, "");
                // �������
                // return:
                //      -1  ����
                //      0   ���ظ�
                //      1   �ظ�
                nRet = this.EntityControl.CheckBarcodeDup(
                    this.entityEditControl_editing.Barcode,
                    this.Item,
                    true,   // bCheckCurrentList,
                    true,   // bCheckDb,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 1)
                    return -1;   // �ظ�
            }

            return 0;
        }

#if NO
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
#endif

        private void EntityEditForm_Activated(object sender, EventArgs e)
        {
            // this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;
        }

        // �������ɾ��״̬
        private void button_editing_undoMaskDelete_Click(object sender, EventArgs e)
        {
            if (this.Items != null)
            {
                this.Items.UndoMaskDeleteItem(this.Item);
                this.entityEditControl_editing.SetReadOnly("librarian");
                this.checkBox_autoSearchDup.Enabled = true;
                // this.button_OK.Enabled = entityEditControl_editing.Changed;
            }
        }

        /*
        void LoadPrevOrNextBookItem(bool bPrev)
        {
            string strError = "";

            if (this.EntityForm == null)
            {
                strError = "û������";
                goto ERROR1;
            }

            int nIndex = this.EntityForm.IndexOfVisibleBookItems(this.BookItem);
            if (nIndex == -1)
            {
                // ��Ȼ��������û���ҵ�
                strError = "BookItem�����Ȼ��������û���ҵ���";
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

            if (nIndex >= this.EntityForm.CountOfVisibleBookItems())
            {
                strError = "��β";
                goto ERROR1;
            }

            // ���浱ǰ����
            int nRet = FinishOneBookItem(out strError);
            if (nRet == -1)
                goto ERROR1;

            BookItem new_bookitem = this.EntityForm.GetAtVisibleBookItems(nIndex);
            LoadBookItem(new_bookitem);

            // ��listview�й������ɼ���Χ
            new_bookitem.HilightListViewItem();
            this.Text = "����Ϣ";
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
         * */

#if NO
        void LoadPrevOrNextBookItem(bool bPrev)
        {
            string strError = "";

            BookItem new_bookitem = GetPrevOrNextBookItem(bPrev,
                out strError);
            if (new_bookitem == null)
                goto ERROR1;

            // ���浱ǰ����
            int nRet = FinishOneBookItem(out strError);
            if (nRet == -1)
                goto ERROR1;

            LoadBookItem(new_bookitem);

            // ��listview�й������ɼ���Χ
            new_bookitem.HilightListViewItem(true);
            this.Text = "����Ϣ";
            return;
        ERROR1:
            AutoCloseMessageBox.Show(this, strError, 2000);
            // MessageBox.Show(this, strError);
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
            if (this.BookItem != null
                && this.BookItem.Error != null)
            {
                goto DISABLE_TWO_BUTTON;
            }


            if (this.EntityControl == null)
            {
                // ��Ϊû�������������޷�prev/next�����Ǿ�diable
                goto DISABLE_TWO_BUTTON;
            }

            int nIndex = 0;

            nIndex = this.EntityControl.IndexOfVisibleItems(this.BookItem);

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

            if (nIndex >= this.EntityControl.CountOfVisibleItems() - 1)
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

        private void entityEditControl_editing_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            // this.button_OK.Enabled = e.CurrentChanged;
            SetOkButtonState();
        }

        static string DoAction(
            string strAction,
            string strValue)
        {
            string strError = "";
            string strResult = "";
            int nNumber = 0;
            int nRet = 0;

            if (strAction == "minus")
            {
                nNumber = -1;

                // ��һ�����ַ���������������һ��������
                // ���� B019 + 1 ��� B020
                nRet = StringUtil.IncreaseLeadNumber(strValue,
                    nNumber,
                    out strResult,
                    out strError);
                if (nRet == -1)
                    strResult = strError;
                return strResult;
            }
            else if (strAction == "plus")
            {
                nNumber = 1;

                // ��һ�����ַ���������������һ��������
                // ���� B019 + 1 ��� B020
                nRet = StringUtil.IncreaseLeadNumber(strValue,
                    nNumber,
                    out strResult,
                    out strError);
                if (nRet == -1)
                    strResult = strError;
                return strResult;
            }
            else if (strAction == "copy")
                return strValue;
            else
                return "δ֪��strActionֵ '" + strAction + "'";
        }

        // entityeditcontrol��ĳ�������򴥷��˰���
        private void entityEditControl_editing_ControlKeyDown(object sender,
            ControlKeyEventArgs e)
        {
            string strAction = "copy";

            bool bUp = false;

            Debug.WriteLine("keycode=" + e.e.KeyCode.ToString());

            if (e.e.KeyCode == Keys.A && e.e.Control == true)
            {
                if (this.GenerateData != null)
                {
                    GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                    e1.FocusedControl = sender; // senderΪ EntityEditControl
                    this.GenerateData(this, e1);
                }
                e.e.SuppressKeyPress = true;    // 2015/5/28
                return;
            }
            else if (e.e.KeyCode == Keys.PageDown && e.e.Control == true)
            {
                this.button_editing_nextRecord_Click(null, null);
                return;
            }
            else if (e.e.KeyCode == Keys.PageUp && e.e.Control == true)
            {
                this.button_editing_prevRecord_Click(null, null);
                return;
            }
            else if (e.e.KeyCode == Keys.OemOpenBrackets && e.e.Control == true)
            {
                bUp = true; // �����濽��
            }
            else if (e.e.KeyCode == Keys.OemCloseBrackets && e.e.Control == true)
            {
                bUp = false;    // �����濽��
            }
            else if (e.e.KeyCode == Keys.OemMinus && e.e.Control == true)
            {
                bUp = true; // ���������
                strAction = "minus";
            }
            else if (e.e.KeyCode == Keys.Oemplus && e.e.Control == true)
            {
                bUp = true;    // ����������
                strAction = "plus";
            }
            else if (e.e.KeyCode == Keys.D0 && e.e.Control == true)
            {
                bUp = false; // ���������
                strAction = "minus";
            }
            else if (e.e.KeyCode == Keys.D9 && e.e.Control == true)
            {
                bUp = false;    // ����������
                strAction = "plus";
            }
            else
                return;

            string strError = "";
            BookItem bookitem = GetPrevOrNextItem(bUp, out strError);
            if (bookitem == null)
                return;
            switch (e.Name)
            {
                case "PublishTime":
                    this.entityEditControl_editing.PublishTime =
                        DoAction(strAction, bookitem.PublishTime);
                    break;
                case "Seller":
                    this.entityEditControl_editing.Seller =
                        DoAction(strAction, bookitem.Seller);
                    break;
                case "Source":
                    this.entityEditControl_editing.Source =
                        DoAction(strAction, bookitem.Source);
                    break;
                case "Intact":
                    this.entityEditControl_editing.Intact =
                        DoAction(strAction, bookitem.Intact);
                    break;
                case "Binding":
                    this.entityEditControl_editing.Binding =
                        DoAction(strAction, bookitem.Binding);
                    break;
                case "Operations":
                    this.entityEditControl_editing.Operations =
                        DoAction(strAction, bookitem.Operations);
                    break;


                case "Price":
                    this.entityEditControl_editing.Price = 
                        DoAction(strAction, bookitem.Price); 
                    break;
                case "Barcode":
                    this.entityEditControl_editing.Barcode =  
                        DoAction(strAction, bookitem.Barcode);
                    break;
                case "State":
                    this.entityEditControl_editing.State =  
                        DoAction(strAction, bookitem.State);
                    break;
                case "Location":
                    this.entityEditControl_editing.LocationString =  
                        DoAction(strAction, bookitem.Location);
                    break;
                case "Comment":
                    this.entityEditControl_editing.Comment =  
                        DoAction(strAction, bookitem.Comment);
                    break;
                case "Borrower":
                    Console.Beep();
                    //this.entityEditControl_editing.Borrower = bookitem.Borrower;
                    break;
                case "BorrowDate":
                    Console.Beep();
                    //this.entityEditControl_editing.BorrowDate = bookitem.BorrowDate;
                    break;
                case "BorrowPeriod":
                    Console.Beep();
                    //this.entityEditControl_editing.BorrowPeriod = bookitem.BorrowPeriod;
                    break;
                case "RecPath":
                    Console.Beep();
                    //this.entityEditControl_editing.RecPath = bookitem.RecPath;
                    break;
                case "BookType":
                    this.entityEditControl_editing.BookType =  
                        DoAction(strAction, bookitem.BookType);
                    break;
                case "RegisterNo":
                    this.entityEditControl_editing.RegisterNo =  
                        DoAction(strAction, bookitem.RegisterNo);
                    break;
                case "MergeComment":
                    this.entityEditControl_editing.MergeComment =  
                        DoAction(strAction, bookitem.MergeComment);
                    break;
                case "BatchNo":
                    this.entityEditControl_editing.BatchNo =  
                        DoAction(strAction, bookitem.BatchNo);
                    break;
                case "Volume":
                    this.entityEditControl_editing.Volume =  
                        DoAction(strAction, bookitem.Volume);
                    break;
                case "AccessNo":
                    this.entityEditControl_editing.AccessNo =
                        DoAction(strAction, bookitem.AccessNo);
                    break;
                case "RefID":
                    Console.Beep();
                    // this.entityEditControl_editing.RefID = bookitem.RefID;  // 2009/6/2
                    break;
                default:
                    Debug.Assert(false, "δ֪����Ŀ���� '" +e.Name+ "'");
                    return;
            }

        }

#if NO
        BookItem GetPrevOrNextBookItem(bool bPrev,
            out string strError)
        {
            strError = "";

            if (this.EntityControl == null)
            {
                strError = "û������";
                goto ERROR1;
            }

            int nIndex = this.EntityControl.IndexOfVisibleItems(this.BookItem);
            if (nIndex == -1)
            {
                // ��Ȼ��������û���ҵ�
                strError = "BookItem�����Ȼ��������û���ҵ���";
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

            if (nIndex >= this.EntityControl.CountOfVisibleItems())
            {
                strError = "��β";
                goto ERROR1;
            }

            return this.EntityControl.GetVisibleItemAt(nIndex);
        ERROR1:
            return null;
        }
#endif

        private void entityEditControl_editing_ControlKeyPress(object sender, ControlKeyPressEventArgs e)
        {

        }

#if NO
        // ��ȡֵ�б�ʱ��Ϊ���������ݿ���
        /// <summary>
        /// ��Ŀ��������ȡֵ�б�ʱ��Ϊ���������ݿ���
        /// </summary>
        public string BiblioDbName
        {
            get
            {
                return this.entityEditControl_editing.BiblioDbName;
            }
            set
            {
                this.entityEditControl_editing.BiblioDbName = value;
                this.entityEditControl_existing.BiblioDbName = value;
            }
        }
#endif
    }

    /// <summary>
    /// ���¼�༭�Ի���Ļ�����
    /// </summary>
    public class EntityEditFormBase : ItemEditFormBase<BookItem, BookItemCollection>
    {
    }
}