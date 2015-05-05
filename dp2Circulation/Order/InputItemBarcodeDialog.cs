using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;

namespace dp2Circulation
{
    /// <summary>
    /// �ɹ�ģ�������ʱ�����û��������������ŵĶԻ���
    /// </summary>
    internal partial class InputItemBarcodeDialog : Form
    {
        public ApplicationInfo AppInfo = null;

        public bool SeriesMode = false;

        public event VerifyBarcodeHandler VerifyBarcode = null;
        public event DetectBarcodeDupHandler DetectBarcodeDup = null;

        public EntityControl EntityControl = null;  // ��ص�EntityControl

        // �ı���������������ڴ��Ƿ�ı�
        bool m_bTextChanged = false;

        const int WM_ACTIVATE_BARCODE_INPUT = API.WM_USER + 201;

        public List<InputBookItem> BookItems = null;

        // ��������������
        List<string> m_oldBarcodes = null;

        int m_nIndex = -1;  // ��ǰ�������Ӧ���к�

        bool m_bChanged = false;

        int m_nOriginDisplayColumnWidth = 0;

        const int COLUMN_PRICE = 6;
        const int COLUMN_REF_PRICE = 7;

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

        public InputItemBarcodeDialog()
        {
            InitializeComponent();
        }

        private void InputItemBarcodeDialog_Load(object sender, EventArgs e)
        {
            if (this.AppInfo != null)
            {
                string strWidths = this.AppInfo.GetString(
                    "input_item_barcode_dialog",
                    "list_column_width",
                    "");
                if (String.IsNullOrEmpty(strWidths) == false)
                {
                    ListViewUtil.SetColumnHeaderWidth(this.listView_barcodes,
                        strWidths,
                        true);
                }
            }

            this.m_nOriginDisplayColumnWidth = this.columnHeader_volumeDisplay.Width;

            if (this.SeriesMode == false)
            {
                this.columnHeader_volumeDisplay.Width = 0;
            }

            FillBookItemList();
        }

        private void InputItemBarcodeDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.AppInfo != null)
            {
                if (this.columnHeader_volumeDisplay.Width == 0)
                    this.columnHeader_volumeDisplay.Width = this.m_nOriginDisplayColumnWidth;

                string strWidths = ListViewUtil.GetColumnWidthListString(this.listView_barcodes);
                this.AppInfo.SetString(
                    "input_item_barcode_dialog",
                    "list_column_width",
                    strWidths);
            }
        }

        // ��textbox���޸Ķ��ֵ��ڴ���
        int UpdateData(out string strError)
        {
            strError = "";

            if (this.m_nIndex == -1)
                return 0;

            if (this.m_bTextChanged == false)
                return 0;

            int index = this.m_nIndex;

            InputBookItem book_item = this.BookItems[index];

            string strCurrentBarcode = book_item.BookItem.Barcode;

            if (strCurrentBarcode != this.textBox_itemBarcode.Text)
            {
                // У��barcode�Ϸ���
                if (this.VerifyBarcode != null
                    && this.textBox_itemBarcode.Text != "") // 2009/1/15 new add
                {
                    VerifyBarcodeEventArgs e = new VerifyBarcodeEventArgs();
                    e.Barcode = this.textBox_itemBarcode.Text;
                    this.VerifyBarcode(this, e);
                    // return:
                    //      -2  ������û������У�鷽�����޷�У��
                    //      -1  error
                    //      0   ���ǺϷ��������
                    //      1   �ǺϷ��Ķ���֤�����
                    //      2   �ǺϷ��Ĳ������

                    if (e.Result != -2)
                    {
                        if (e.Result != 2)
                        {
                            if (String.IsNullOrEmpty(strError) == false)
                                strError = e.ErrorInfo;
                            else
                            {
                                // ����ӷ�������û�еõ�������Ϣ���򲹳�
                                //      -1  error
                                if (e.Result == -1)
                                    strError = "��У������� '" + e.Barcode + "' ʱ����";
                                //      0   ���ǺϷ��������
                                else if (e.Result == 0)
                                    strError = "'" + e.Barcode + "' ���ǺϷ��������";
                                //      1   �ǺϷ��Ķ���֤�����
                                else if (e.Result == 1)
                                    strError = "'" + e.Barcode + "' �Ƕ���֤�����(�����ǲ������)";
                            }
                            return -1;
                        }
                    }
                }

                book_item.BookItem.Barcode = this.textBox_itemBarcode.Text;
                this.Changed = true;
                ListViewItem item = this.listView_barcodes.Items[index];
                item.Font = new Font(item.Font, FontStyle.Bold);    // �Ӵ������ʾ���ݱ��ı���

                book_item.BookItem.Changed = true;
                book_item.BookItem.RefreshListView();

                // �޸�ListViewItem��ʾ
                this.listView_barcodes.Items[index].Text = this.textBox_itemBarcode.Text;

                this.m_bTextChanged = false;
                return 1;
            }

            return 0;
        }
        
        private void button_register_Click(object sender, EventArgs e)
        {
            /*
            if (this.textBox_itemBarcode.Text == "")
            {
                MessageBox.Show(this, "��δ����������");
                this.textBox_itemBarcode.Focus();
                return;
            }*/

            if (this.listView_barcodes.SelectedIndices.Count == 0)
            {
                MessageBox.Show(this, "��δѡ����ǰ��");
                return;
            }

            string strError = "";
            int nRet = UpdateData(out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                this.textBox_itemBarcode.SelectAll();
                this.textBox_itemBarcode.Focus();
                return;
            }

            // ѡ����һ��
            int index = this.listView_barcodes.SelectedIndices[0];

            this.listView_barcodes.SelectedItems.Clear();

            // �������û������
            if (index >= this.listView_barcodes.Items.Count - 1)
            {
                // this.listView_barcodes.SelectedItems[0].Selected = false;
                return;
            }

            index++;
            this.listView_barcodes.Items[index].Selected = true;
            this.listView_barcodes.EnsureVisible(index);
        }

        void FillBookItemList()
        {
            this.listView_barcodes.Items.Clear();
            this.m_oldBarcodes = new List<string>();

            if (this.BookItems == null)
                return;

            for (int i = 0; i < this.BookItems.Count; i++)
            {
                InputBookItem book_item = this.BookItems[i];

                Debug.Assert(book_item != null, "");

                ListViewItem item = new ListViewItem();
                // ����
                item.Text = book_item.BookItem.Barcode;
                // ������Ϣ
                string strVolumeDisplayString = IssueManageControl.BuildVolumeDisplayString(
                    book_item.BookItem.PublishTime,
                    book_item.BookItem.Volume);
                item.SubItems.Add(strVolumeDisplayString);

                // ����
                // 2010/12/1
                item.SubItems.Add(book_item.Sequence);

                // �ݲصص�
                item.SubItems.Add(book_item.BookItem.Location);
                // ��������
                item.SubItems.Add(book_item.BookItem.Seller);
                // ������Դ
                item.SubItems.Add(book_item.BookItem.Source);
                // �۸�
                item.SubItems.Add(book_item.BookItem.Price);
                // �����۸�
                item.SubItems.Add(book_item.OtherPrices);

                item.Tag = book_item;

                this.listView_barcodes.Items.Add(item);

                this.m_oldBarcodes.Add(book_item.BookItem.Barcode);
            }

            // ѡ����һ������
            if (this.listView_barcodes.Items.Count > 0)
            {
                this.listView_barcodes.Items[0].Selected = true;
            }

            /*
            // �����һ������ɼ�
            if (this.listView_barcodes.Items.Count > 0)
                this.listView_barcodes.EnsureVisible(this.listView_barcodes.Items.Count - 1);
             * */
        }

        void UpdateBookItemsPrice()
        {
            for (int i = 0; i < this.listView_barcodes.Items.Count; i++)
            {
                ListViewItem item = this.listView_barcodes.Items[i];

                InputBookItem book_item = (InputBookItem)item.Tag;

                string strNewPrice = ListViewUtil.GetItemText(item, COLUMN_PRICE);
                if (strNewPrice != book_item.BookItem.Price)
                {
                    book_item.BookItem.Price = strNewPrice;
                    book_item.BookItem.RefreshListView();
                }
            }
        }

        private void textBox_itemBarcode_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.button_register;
        }

        private void textBox_itemBarcode_Leave(object sender, EventArgs e)
        {
            this.AcceptButton = null;
        }

        static List<BookItem> GetBookItemList(List<InputBookItem> items)
        {
            List<BookItem> results = new List<BookItem>();
            for (int i = 0; i < items.Count; i++)
            {
                results.Add(items[i].BookItem);
            }

            return results;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            this.button_OK.Enabled = false;

            try
            {
                // ������δ�����������
                int nBlankCount = 0;
                for (int i = 0; i < this.listView_barcodes.Items.Count; i++)
                {
                    string strBarcode = this.listView_barcodes.Items[i].Text;
                    if (String.IsNullOrEmpty(strBarcode) == true)
                        nBlankCount++;
                }

                if (nBlankCount > 0)
                {
                    DialogResult result = MessageBox.Show(this,
                        "��ǰ�� "+nBlankCount.ToString()+" ������δ�������롣\r\n\r\nȷʵҪ������������? ",
                        "InputItemBarcodeDialog",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return;

                    // �������
                }

                // �������?
                string strError = "";
                int nRet = this.UpdateData(out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    return;
                }

                if (this.DetectBarcodeDup != null)
                {
                    DetectBarcodeDupEventArgs e1 = new DetectBarcodeDupEventArgs();
                    e1.EntityControl = this.EntityControl;
                    e1.BookItems = GetBookItemList(this.BookItems);
                    this.DetectBarcodeDup(this, e1);

                    if (e1.Result == -1 || e1.Result == 1)
                    {
                        // TODO: �ɷ����MessageBox����?
                        MessageBox.Show(this, e1.ErrorInfo.Replace("; ", "\r\n"));
                        return;
                    }
                }

                UpdateBookItemsPrice();
            }
            finally
            {
                this.button_OK.Enabled = true;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            RestoreOldBarcodes();

            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        void RestoreOldBarcodes()
        {
            if (this.m_oldBarcodes == null)
                return;

            for (int i = 0; i < this.m_oldBarcodes.Count; i++)
            {
                InputBookItem book_item = this.BookItems[i];

                if (book_item.BookItem.Barcode != this.m_oldBarcodes[i])
                {
                    book_item.BookItem.Barcode = this.m_oldBarcodes[i];
                    book_item.BookItem.RefreshListView();
                }
            }
        }

        private void listView_barcodes_SelectedIndexChanged(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = UpdateData(out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }

            if (this.listView_barcodes.SelectedItems.Count == 0)
            {
                this.toolStripButton_modifyByBiblioPrice.Enabled = false;
                this.toolStripButton_modifyByOrderPrice.Enabled = false;
                this.toolStripButton_modifyByArrivePrice.Enabled = false;
                this.toolStripButton_modifyPrice.Enabled = false;
                this.toolStripButton_discount.Enabled = false;
            }
            else
            {
                this.toolStripButton_modifyByBiblioPrice.Enabled = true;
                this.toolStripButton_modifyByOrderPrice.Enabled = true;
                this.toolStripButton_modifyByArrivePrice.Enabled = true;
                this.toolStripButton_modifyPrice.Enabled = true;
                this.toolStripButton_discount.Enabled = true;
            }

            if (this.listView_barcodes.SelectedItems.Count != 1)
            {
                this.textBox_itemBarcode.Text = "";
                this.textBox_itemBarcode.Enabled = false;
                this.button_register.Enabled = false;
                m_nIndex = -1;
                return;
            }

            int nIndex = this.listView_barcodes.SelectedIndices[0];

            // ���仯���������װ��textbox
            if (nIndex != m_nIndex)
            {
                ListViewItem item = this.listView_barcodes.Items[nIndex];

                this.textBox_itemBarcode.Enabled = true;
                this.button_register.Enabled = true;

                this.textBox_itemBarcode.Text = item.Text;

                m_nIndex = nIndex;
                this.m_bTextChanged = false;
            }

            if (this.checkBox_alwaysFocusInputBox.Checked == true)
                API.PostMessage(this.Handle, WM_ACTIVATE_BARCODE_INPUT, 0, 0);
       }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_ACTIVATE_BARCODE_INPUT:
                    if (this.textBox_itemBarcode.Enabled == true)
                    {
                        this.textBox_itemBarcode.SelectAll();
                        this.textBox_itemBarcode.Focus();
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }

        private void textBox_itemBarcode_TextChanged(object sender, EventArgs e)
        {
            this.m_bTextChanged = true;
        }

        /*
        public bool BarcodeMode
        {
            get
            {
                if (this.toolStripButton_barcodeMode.Checked == true)
                    return true;
                return false;
            }
            set
            {
                if (value == true)
                {
                    this.toolStripButton_barcodeMode.Checked = true;
                    this.toolStripButton_priceMode.Checked = false;
                }
                else
                {
                    this.toolStripButton_barcodeMode.Checked = false;
                    this.toolStripButton_priceMode.Checked = true;
                }
            }
        }
         * */

        private void listView_barcodes_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.listView_barcodes.SelectedItems.Count == 1)
            {
                if (this.checkBox_alwaysFocusInputBox.Checked == true)
                    API.PostMessage(this.Handle, WM_ACTIVATE_BARCODE_INPUT, 0, 0);
            }

            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("����Ŀ�� ����۸�(&B)");
            menuItem.Click += new System.EventHandler(this.menu_modifyPriceByBiblioPrice_Click);
            if (this.listView_barcodes.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�������� ����۸�(&O)");
            menuItem.Click += new System.EventHandler(this.menu_modifyPriceByOrderPrice_Click);
            if (this.listView_barcodes.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�����ռ� ����۸�(&A)");
            menuItem.Click += new System.EventHandler(this.menu_modifyPriceByArrivePrice_Click);
            if (this.listView_barcodes.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("����۸�(&M)");
            menuItem.Click += new System.EventHandler(this.menu_modifyPrice_Click);
            if (this.listView_barcodes.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�����ۿ�(&M)");
            menuItem.Click += new System.EventHandler(this.menu_appendDiscount_Click);
            if (this.listView_barcodes.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView_barcodes, new Point(e.X, e.Y));	
        }

        // ���ն���������۸�
        void menu_modifyPriceByOrderPrice_Click(object sender, EventArgs e)
        {
            ModifyPriceBy("������");
        }

        void ModifyPriceBy(string strRefName)
        {
            foreach (ListViewItem item in this.listView_barcodes.SelectedItems)
            {
                // ListViewItem item = this.listView_barcodes.SelectedItems[i];
                string strRefPrice = ListViewUtil.GetItemText(item, COLUMN_REF_PRICE);

                // �����ż���Ĳ����������Hashtable��
                // parameters:
                //      strText �ַ�������̬�� "��1=ֵ1,��2=ֵ2"
                Hashtable table = StringUtil.ParseParameters(strRefPrice,
                    ';',
                    ':');

                ListViewUtil.ChangeItemText(item, COLUMN_PRICE, (string)table[strRefName]);
            }
        }

        // �������ռ�����۸�
        void menu_modifyPriceByArrivePrice_Click(object sender, EventArgs e)
        {
            ModifyPriceBy("���ռ�");
        }

        // ������Ŀ������۸�
        void menu_modifyPriceByBiblioPrice_Click(object sender, EventArgs e)
        {
            ModifyPriceBy("��Ŀ��");
        }

        public string UsedDiscountString
        {
            get
            {
                if (this.AppInfo == null)
                    return "";
                return this.AppInfo.GetString(
                    "input_item_barcode_dialog",
                    "used_discount",
                    "0.75");
            }
            set
            {
                if (this.AppInfo == null)
                    return;
                this.AppInfo.SetString(
                    "input_item_barcode_dialog",
                    "used_discount",
                    value);

            }

        }

        // �����ۿ�
        void menu_appendDiscount_Click(object sender, EventArgs e)
        {
            string strError = "";

            string strDiscountPart = InputDlg.GetInput(
    this,
    "Ϊ���еļ۸��ַ��������ۿ۲���",
    "�ۿ�: ",
    this.UsedDiscountString,
    this.Font);
            if (strDiscountPart == null)
                return;

            strDiscountPart = strDiscountPart.Trim();

            if (string.IsNullOrEmpty(strDiscountPart) == true)
            {
                strError = "��������ۿ۲���Ϊ�գ���������";
                goto ERROR1;
            }

            if (strDiscountPart[0] == '*')
                strDiscountPart = strDiscountPart.Substring(1).Trim();

            if (string.IsNullOrEmpty(strDiscountPart) == true)
            {
                strError = "��������ۿ۲��ֵ���Ч����Ϊ�գ���������";
                goto ERROR1;
            }

            this.UsedDiscountString = strDiscountPart;  // ����

            foreach (ListViewItem item in this.listView_barcodes.SelectedItems)
            {
                // ListViewItem item = this.listView_barcodes.SelectedItems[i];
                string strOldPrice = ListViewUtil.GetItemText(item, COLUMN_PRICE);
                if (string.IsNullOrEmpty(strOldPrice) == true)
                {
                    strError = "�� "+(this.listView_barcodes.Items.IndexOf(item) + 1).ToString()+" ������۸񲿷�Ϊ�գ��޷������ۿ۲��֡������ж�";
                    goto ERROR1;
                }

                int nRet = strOldPrice.IndexOf("*");
                if (nRet != -1)
                    strOldPrice = strOldPrice.Substring(0, nRet).Trim();

                strOldPrice += "*" + strDiscountPart;

                ListViewUtil.ChangeItemText(item, COLUMN_PRICE, strOldPrice);
            }
            return;

        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ����۸�
        void menu_modifyPrice_Click(object sender, EventArgs e)
        {
            string strNewPrice = InputDlg.GetInput(
    this,
    "����ѡ��������ļ۸�",
    "�۸�: ",
    "",
    this.Font);
            if (strNewPrice == null)
                return;

            foreach (ListViewItem item in this.listView_barcodes.SelectedItems)
            {
                // ListViewItem item = this.listView_barcodes.SelectedItems[i];
                ListViewUtil.ChangeItemText(item, COLUMN_PRICE, strNewPrice);
            }
        }

        private void checkBox_alwaysFocusInputBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_alwaysFocusInputBox.Checked == true)
                API.PostMessage(this.Handle, WM_ACTIVATE_BARCODE_INPUT, 0, 0);
        }

        private void toolStripButton_modifyByBiblioPrice_Click(object sender, EventArgs e)
        {
            menu_modifyPriceByBiblioPrice_Click(sender, e);
        }

        private void toolStripButton_modifyByOrderPrice_Click(object sender, EventArgs e)
        {
            menu_modifyPriceByOrderPrice_Click(sender, e);

        }

        private void toolStripButton_modifyByArrivePrice_Click(object sender, EventArgs e)
        {
            menu_modifyPriceByArrivePrice_Click(sender, e);

        }

        private void toolStripButton_modifyPrice_Click(object sender, EventArgs e)
        {
            menu_modifyPrice_Click(sender, e);
        }

        private void toolStripButton_discount_Click(object sender, EventArgs e)
        {
            menu_appendDiscount_Click(sender, e);
        }
    }

    
    /// <summary>
    /// ��������в���
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void DetectBarcodeDupHandler(object sender,
    DetectBarcodeDupEventArgs e);

    /// <summary>
    /// ��������в��صĲ���
    /// </summary>
    public class DetectBarcodeDupEventArgs : EventArgs
    {
        /// <summary>
        /// EntityControl �ؼ�
        /// </summary>
        public EntityControl EntityControl = null;

        /// <summary>
        /// BookItem �ļ���
        /// </summary>
        public List<BookItem> BookItems = null;

        /// <summary>
        /// ���س�����Ϣ
        /// </summary>
        public string ErrorInfo = "";

        // return:
        //      -1  ����������Ϣ��ErrorInfo��
        //      0   û����
        //      1   ���ء���Ϣ��ErrorInfo��
        /// <summary>
        /// ���ؽ����
        /// <para>-1:  ����������Ϣ��ErrorInfo��</para>
        /// <para>0:   û����</para>
        /// <para>1:   ���ء���Ϣ��ErrorInfo��</para>
        /// </summary>
        public int Result = 0;
    }

    internal class InputBookItem
    {
        public string Sequence = "";    // �������硰1/7��
        public string OtherPrices = ""; // ��ѡ�������۸񡣸�ʽΪ: "������:CNY12.00;���ռ�:CNY15.00"
        public BookItem BookItem = null;
    }
}