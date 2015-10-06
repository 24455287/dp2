using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

using DigitalPlatform.GUI;
using DigitalPlatform.Xml;

namespace DigitalPlatform.CirculationClient
{
    /// <summary>
    /// �ݲص�����������Ա༭ �Ի���
    /// �� UpgradeDt1000ToDp2 ����
    /// </summary>
    public partial class LocationCanBorrowDialog : Form
    {
        /*
    <locationTypes>
        <item canborrow="yes">��ͨ��</item>
        <item canborrow="no">������</item>
        <item canborrow="yes">test</item>
    </locationTypes>
         * */
        public string Xml = "";

        public LocationCanBorrowDialog()
        {
            InitializeComponent();
        }

        private void LocationCanBorrowDialog_Load(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = FillList(this.Xml,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.listView_location_list.Items.Count == 0)
            {
                strError = "��δ����ݲصص�����";
                goto ERROR1;
            }

            string strLocationDef = "";
            // ����<locationTypes>�����XMLƬ��
            // ע�������<locationTypes>Ԫ����Ϊ����
            int nRet = BuildLocationTypesDef(out strLocationDef,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.Xml = strLocationDef;


            this.DialogResult = DialogResult.OK;
            this.Close();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public string Comment
        {
            get
            {
                return this.textBox_comment.Text;
            }
            set
            {
                this.textBox_comment.Text = value;
            }
        }

        int FillList(string strXml,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            this.listView_location_list.Items.Clear();

            if (String.IsNullOrEmpty(strXml) == true)
                return 0;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ�ص�DOMʱ����: " + ex.Message;
                return -1;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("item");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strText = node.InnerText.Trim();

                bool bCanBorrow = false;
                // ��ò����͵����Բ���ֵ
                // return:
                //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
                //      0   ���������ȷ����Ĳ���ֵ
                //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
                nRet = DomUtil.GetBooleanParam(node,
                    "canborrow",
                    false,
                    out bCanBorrow,
                    out strError);
                if (nRet == -1)
                    return -1;

                ListViewItem item = new ListViewItem();
                item.Text = strText;
                item.SubItems.Add(bCanBorrow == true ? "��" : "��");

                this.listView_location_list.Items.Add(item);
            }

            return 0;
        }

        private void toolStripButton_location_new_Click(object sender, EventArgs e)
        {
            LocationItemDialog dlg = new LocationItemDialog();

            dlg.CreateMode = true;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            ListViewItem item = new ListViewItem(dlg.LocationString, 0);
            item.SubItems.Add(dlg.CanBorrow == true ? "��" : "��");

            this.listView_location_list.Items.Add(item);
            ListViewUtil.SelectLine(item, true);
        }

        private void toolStripButton_location_modify_Click(object sender, EventArgs e)
        {
            string strError = "";
            if (this.listView_location_list.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ�޸ĵĹݲصص�����";
                goto ERROR1;
            }
            ListViewItem item = this.listView_location_list.SelectedItems[0];

            LocationItemDialog dlg = new LocationItemDialog();

            dlg.LocationString = ListViewUtil.GetItemText(item, 0);
            dlg.CanBorrow = (ListViewUtil.GetItemText(item, 1) == "��") ? true : false;
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            ListViewUtil.ChangeItemText(item, 0, dlg.LocationString);
            ListViewUtil.ChangeItemText(item, 1, dlg.CanBorrow == true ? "��" : "��");

            ListViewUtil.SelectLine(item, true);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void toolStripButton_location_delete_Click(object sender, EventArgs e)
        {
            string strError = "";
            if (this.listView_location_list.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫɾ���Ĺݲصص�����";
                goto ERROR1;
            }

            string strItemNameList = ListViewUtil.GetItemNameList(this.listView_location_list.SelectedItems);
            /*
            for (int i = 0; i < this.listView_location_list.SelectedItems.Count; i++)
            {
                if (i > 0)
                    strItemNameList += ",";
                strItemNameList += this.listView_location_list.SelectedItems[i].Text;
            }
             * */

            // �Ի��򾯸�
            DialogResult result = MessageBox.Show(this,
                "ȷʵҪɾ���ݲصص����� " + strItemNameList + "?",
                "ManagerForm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

#if NO
            for (int i = this.listView_location_list.SelectedIndices.Count - 1;
                i >= 0;
                i--)
            {
                int index = this.listView_location_list.SelectedIndices[i];
                string strDatabaseName = this.listView_location_list.Items[index].Text;
                this.listView_location_list.Items.RemoveAt(index);
            }
#endif
            // 2012/3/11
            ListViewUtil.DeleteSelectedItems(this.listView_location_list);


            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void toolStripButton_location_up_Click(object sender, EventArgs e)
        {
            MoveLocationItemUpDown(true);
        }

        private void toolStripButton_location_down_Click(object sender, EventArgs e)
        {
            MoveLocationItemUpDown(false);
        }

        void MoveLocationItemUpDown(bool bUp)
        {
            string strError = "";
            // int nRet = 0;

            if (this.listView_location_list.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫ���������ƶ��Ĺݲصص�����");
                return;
            }

            ListViewItem item = this.listView_location_list.SelectedItems[0];
            int index = this.listView_location_list.Items.IndexOf(item);

            Debug.Assert(index >= 0 && index <= this.listView_location_list.Items.Count - 1, "");

            //bool bChanged = false;

            if (bUp == true)
            {
                if (index == 0)
                {
                    strError = "��ͷ";
                    goto ERROR1;
                }

                this.listView_location_list.Items.RemoveAt(index);
                index--;
                this.listView_location_list.Items.Insert(index, item);
                this.listView_location_list.FocusedItem = item;

                //bChanged = true;
            }

            if (bUp == false)
            {
                if (index >= this.listView_location_list.Items.Count - 1)
                {
                    strError = "��β";
                    goto ERROR1;
                }
                this.listView_location_list.Items.RemoveAt(index);
                index++;
                this.listView_location_list.Items.Insert(index, item);
                this.listView_location_list.FocusedItem = item;

                //bChanged = true;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void listView_location_list_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            string strName = "";
            string strCanBorrow = "";
            if (this.listView_location_list.SelectedItems.Count > 0)
            {
                strName = this.listView_location_list.SelectedItems[0].Text;
                strCanBorrow = ListViewUtil.GetItemText(this.listView_location_list.SelectedItems[0], 1);
            }


            // �޸Ĺݲ�����
            {
                menuItem = new MenuItem("�޸� " + strName + "(&M)");
                menuItem.Click += new System.EventHandler(this.toolStripButton_location_modify_Click);
                if (this.listView_location_list.SelectedItems.Count == 0)
                    menuItem.Enabled = false;
                // ȱʡ����
                menuItem.DefaultItem = true;
                contextMenu.MenuItems.Add(menuItem);
            }


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("����(&N)");
            menuItem.Click += new System.EventHandler(this.toolStripButton_location_new_Click);
            contextMenu.MenuItems.Add(menuItem);


            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);



            string strText = "";
            if (this.listView_location_list.SelectedItems.Count == 1)
                strText = "ɾ�� " + strName + "(&D)";
            else
                strText = "ɾ����ѡ " + this.listView_location_list.SelectedItems.Count.ToString() + " ���ݲصص�����(&D)";

            menuItem = new MenuItem(strText);
            menuItem.Click += new System.EventHandler(this.toolStripButton_location_delete_Click);
            if (this.listView_location_list.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            /*
            menuItem = new MenuItem("�۲���ѡ " + this.listView_location_list.SelectedItems.Count.ToString() + " ���ݲ�����Ķ���(&D)");
            menuItem.Click += new System.EventHandler(this.menu_viewOpacDatabaseDefine_Click);
            if (this.listView_location_list.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);
             * */

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            // 
            menuItem = new MenuItem("����(&U)");
            menuItem.Click += new System.EventHandler(this.menu_location_up_Click);
            if (this.listView_location_list.SelectedItems.Count == 0
                || this.listView_location_list.Items.IndexOf(this.listView_location_list.SelectedItems[0]) == 0)
                menuItem.Enabled = false;
            else
                menuItem.Enabled = true;
            contextMenu.MenuItems.Add(menuItem);



            // 
            menuItem = new MenuItem("����(&D)");
            menuItem.Click += new System.EventHandler(this.menu_location_down_Click);
            if (this.listView_location_list.SelectedItems.Count == 0
                || this.listView_location_list.Items.IndexOf(this.listView_location_list.SelectedItems[0]) >= this.listView_location_list.Items.Count - 1)
                menuItem.Enabled = false;
            else
                menuItem.Enabled = true;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView_location_list, new Point(e.X, e.Y));		

        }

        private void listView_location_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listView_location_list.SelectedItems.Count > 0)
            {
                this.toolStripButton_location_modify.Enabled = true;
                this.toolStripButton_location_delete.Enabled = true;
            }
            else
            {
                this.toolStripButton_location_modify.Enabled = false;
                this.toolStripButton_location_delete.Enabled = false;
            }

            if (this.listView_location_list.SelectedItems.Count == 0
                || this.listView_location_list.Items.IndexOf(this.listView_location_list.SelectedItems[0]) == 0)
                this.toolStripButton_location_up.Enabled = false;
            else
                this.toolStripButton_location_up.Enabled = true;

            if (this.listView_location_list.SelectedItems.Count == 0
                || this.listView_location_list.Items.IndexOf(this.listView_location_list.SelectedItems[0]) >= this.listView_location_list.Items.Count - 1)
                this.toolStripButton_location_down.Enabled = false;
            else
                this.toolStripButton_location_down.Enabled = true;
        }

        void menu_location_up_Click(object sender, EventArgs e)
        {
            MoveLocationItemUpDown(true);
        }

        void menu_location_down_Click(object sender, EventArgs e)
        {
            MoveLocationItemUpDown(false);
        }

        // ����<locationTypes>�����XMLƬ��
        // ע�������<locationTypes>Ԫ����Ϊ����
        int BuildLocationTypesDef(out string strLocationDef,
            out string strError)
        {
            strError = "";
            strLocationDef = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<locationTypes />");

            for (int i = 0; i < this.listView_location_list.Items.Count; i++)
            {
                ListViewItem item = this.listView_location_list.Items[i];
                string strText = item.Text;
                string strCanBorrow = ListViewUtil.GetItemText(item, 1);

                bool bCanBorrow = false;

                if (strCanBorrow == "��" || strCanBorrow == "yes")
                    bCanBorrow = true;

                XmlNode nodeItem = dom.CreateElement("item");
                dom.DocumentElement.AppendChild(nodeItem);

                nodeItem.InnerText = strText;
                DomUtil.SetAttr(nodeItem, "canborrow", bCanBorrow == true ? "yes" : "no");
            }

            strLocationDef = dom.DocumentElement.OuterXml;

            return 0;
        }

        // ˫��
        private void listView_location_list_DoubleClick(object sender, EventArgs e)
        {
            toolStripButton_location_modify_Click(sender, e);
        }
    }
}