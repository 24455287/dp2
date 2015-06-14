using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using System.Threading;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// ���¼�б�ؼ�
    /// </summary>
    public partial class EntityControl : EntityControlBase
    {
        /*
        // ������ȡ��
        public event GenerateDataEventHandler GenerateAccessNo = null;
         * */

        /// <summary>
        /// У�������
        /// </summary>
        public event VerifyBarcodeHandler VerifyBarcode = null;

        /// <summary>
        /// ��ò���ֵ
        /// </summary>
        public event GetParameterValueHandler GetParameterValue = null;

        /// <summary>
        /// �Ƿ�ҪУ��������
        /// </summary>
        public bool NeedVerifyItemBarcode
        {
            get
            {
                if (this.GetParameterValue == null)
                    return false;

                GetParameterValueEventArgs e = new GetParameterValueEventArgs();
                e.Name = "NeedVerifyItemBarcode";
                this.GetParameterValue(this, e);

                return DomUtil.IsBooleanTrue(e.Value);
            }
        }


        // 
        // return:
        //      -2  ������û������У�鷽�����޷�У��
        //      -1  error
        //      0   ���ǺϷ��������
        //      1   �ǺϷ��Ķ���֤�����
        //      2   �ǺϷ��Ĳ������
        /// <summary>
        /// ��ʽУ�������
        /// </summary>
        /// <param name="strBarcode">�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>      -2  ������û������У�鷽�����޷�У��</para>
        /// <para>      -1  ����</para>
        /// <para>      0   ���ǺϷ��������</para>
        /// <para>      1   �ǺϷ��Ķ���֤�����</para>
        /// <para>      2   �ǺϷ��Ĳ������</para>
        /// </returns>
        public int DoVerifyBarcode(string strBarcode,
            out string strError)
        {
            if (this.VerifyBarcode == null)
            {
                strError = "��δ�ҽ�VerifyBarcode�¼�";
                return -1;
            }

            VerifyBarcodeEventArgs e = new VerifyBarcodeEventArgs();
            e.Barcode = strBarcode;
            this.VerifyBarcode(this, e);
            strError = e.ErrorInfo;
            return e.Result;
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public EntityControl()
        {
            InitializeComponent();

            this.m_listView = this.listView;
            this.ItemType = "item";
            this.ItemTypeName = "��";
        }

        // 
        // return:
        //      -1  ����
        //      0   û��װ��
        //      1   �Ѿ�װ��
        /// <summary>
        /// ���һ����Ŀ��¼������ȫ��ʵ���¼·��
        /// </summary>
        /// <param name="stop">Stop����</param>
        /// <param name="channel">ͨѶͨ��</param>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="recpaths">���ؼ�¼·���ַ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1 ����</para>
        /// <para>0 û��װ��</para>
        /// <para>1 �Ѿ�װ��</para>
        /// </returns>
        public static int GetEntityRecPaths(
            Stop stop,
            LibraryChannel channel,
            string strBiblioRecPath,
            out List<string> recpaths,
            out string strError)
        {
            strError = "";
            recpaths = new List<string>();

            long lPerCount = 100; // ÿ����ö��ٸ�
            long lStart = 0;
            long lResultCount = 0;
            long lCount = -1;
            for (; ; )
            {
                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }
                }
                EntityInfo[] entities = null;

                /*
                if (lCount > 0)
                    stop.SetMessage("����װ�����Ϣ " + lStart.ToString() + "-" + (lStart + lCount - 1).ToString() + " ...");
                 * */

                long lRet = channel.GetEntities(
                    stop,
                    strBiblioRecPath,
                    lStart,
                    lCount,
                    "onlygetpath",
                    "zh",
                    out entities,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                lResultCount = lRet;

                if (lRet == 0)
                    return 0;

                Debug.Assert(entities != null, "");


                for (int i = 0; i < entities.Length; i++)
                {
                    if (entities[i].ErrorCode != ErrorCodeValue.NoError)
                    {
                        strError = "·��Ϊ '" + entities[i].OldRecPath + "' �Ĳ��¼װ���з�������: " + entities[i].ErrorInfo;  // NewRecPath
                        return -1;
                    }

                    recpaths.Add(entities[i].OldRecPath);
                }

                lStart += entities.Length;
                if (lStart >= lResultCount)
                    break;

                if (lCount == -1)
                    lCount = lPerCount;

                if (lStart + lCount > lResultCount)
                    lCount = lResultCount - lStart;
            }

            return 1;
        ERROR1:
            return -1;
        }

#if NO
        // װ��ʵ���¼
        // return:
        //      -1  ����
        //      0   û��װ��
        //      1   �Ѿ�װ��
        public int LoadEntityRecords(string strBiblioRecPath,
            bool bDisplayOtherLibraryItem,
            out string strError)
        {
            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("����װ�����Ϣ ...");
            Stop.BeginLoop();

            // this.Update();   // �Ż�
            // this.MainForm.Update();


            try
            {
                // string strHtml = "";

                this.ClearEntities();

                long lPerCount = 100; // ÿ����ö��ٸ�
                long lStart = 0;
                long lResultCount = 0;
                long lCount = -1;
                for (; ; )
                {

                    EntityInfo[] entities = null;

                    Thread.Sleep(500);

                    if (lCount > 0)
                        Stop.SetMessage("����װ�����Ϣ "+lStart.ToString()+"-"+(lStart+lCount-1).ToString()+" ...");

                    long lRet = Channel.GetEntities(
                        Stop,
                        strBiblioRecPath,
                        lStart,
                        lCount,
                        bDisplayOtherLibraryItem == true ? "getotherlibraryitem" : "",
                        "zh",
                        out entities,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    lResultCount = lRet;

                    if (lRet == 0)
                        return 0;

                    Debug.Assert(entities != null, "");

                    this.ListView.BeginUpdate();
                    try
                    {
                        for (int i = 0; i < entities.Length; i++)
                        {
                            if (entities[i].ErrorCode != ErrorCodeValue.NoError)
                            {
                                strError = "·��Ϊ '" + entities[i].OldRecPath + "' �Ĳ��¼װ���з�������: " + entities[i].ErrorInfo;  // NewRecPath
                                return -1;
                            }

                            // �����صļ�¼�п����Ǳ����˵���
                            if (string.IsNullOrEmpty(entities[i].OldRecord) == true)
                                continue;

                            // ����һ�����xml��¼��ȡ���й���Ϣ����listview��
                            BookItem bookitem = new BookItem();

                            int nRet = bookitem.SetData(entities[i].OldRecPath, // NewRecPath
                                     entities[i].OldRecord,
                                     entities[i].OldTimestamp,
                                     out strError);
                            if (nRet == -1)
                                return -1;

                            if (entities[i].ErrorCode == ErrorCodeValue.NoError)
                                bookitem.Error = null;
                            else
                                bookitem.Error = entities[i];

                            this.BookItems.Add(bookitem);


                            bookitem.AddToListView(this.ListView);
                        }
                    }
                    finally
                    {
                        this.ListView.EndUpdate();
                    }

                    lStart += entities.Length;
                    if (lStart >= lResultCount)
                        break;

                    if (lCount == -1)
                        lCount = lPerCount;

                    if (lStart + lCount > lResultCount)
                        lCount = lResultCount - lStart;
                }
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }

#endif

        private void listView_items_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            bool bHasBillioLoaded = false;

            if (String.IsNullOrEmpty(this.BiblioRecPath) == false)
                bHasBillioLoaded = true;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("�޸�(&M)");
            menuItem.Click += new System.EventHandler(this.menu_modifyEntity_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("����(&N)");
            menuItem.Click += new System.EventHandler(this.menu_newEntity_Click);
            if (bHasBillioLoaded == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�������(&U)");
            menuItem.Click += new System.EventHandler(this.menu_newMultiEntity_Click);
            if (bHasBillioLoaded == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            ListViewHitTestInfo hittest_info = this.listView.HitTest(this.PointToClient(Control.MousePosition));
            string strColumnName = "";
            if (hittest_info.Item != null)
            {
                int x = hittest_info.Item.SubItems.IndexOf(hittest_info.SubItem);
                if (x >= 0)
                    strColumnName = this.listView.Columns[x].Text;
            }
            menuItem = new MenuItem("�Զ������� '" + strColumnName + "' (&V)");
            menuItem.Click += new System.EventHandler(this.menu_autoCopyColumn_Click);
            if (bHasBillioLoaded == false
                || hittest_info.Item == null
                || hittest_info.SubItem == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);
            menuItem.Tag = hittest_info;

            menuItem = new MenuItem("����� '" + strColumnName + "' (&C)");
            menuItem.Click += new System.EventHandler(this.menu_autoClearColumn_Click);
            if (bHasBillioLoaded == false
                || hittest_info.Item == null
                || hittest_info.SubItem == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);
            menuItem.Tag = hittest_info;


            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // cut ����
            menuItem = new MenuItem("����(&T)");
            menuItem.Click += new System.EventHandler(this.menu_cutEntity_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // copy ����
            menuItem = new MenuItem("����(&C) [" + this.listView.SelectedItems.Count.ToString() + "��]");
            menuItem.Click += new System.EventHandler(this.menu_copyEntity_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            bool bHasClipboardObject = false;
            IDataObject iData = Clipboard.GetDataObject();
            if (iData == null
                || iData.GetDataPresent(typeof(ClipboardBookItemCollection)) == false)
                bHasClipboardObject = false;
            else
                bHasClipboardObject = true;

            // paste ճ��
            menuItem = new MenuItem("ճ��(&P)");
            menuItem.Click += new System.EventHandler(this.menu_pasteEntity_Click);
            if (bHasClipboardObject == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // ȫѡ
            menuItem = new MenuItem("ȫѡ(&A)");
            menuItem.Click += new System.EventHandler(this.menu_selectAll_Click);
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ѡ��ȫ��ͬһ�ż���ϵ������(&R)");
            menuItem.Click += new System.EventHandler(this.menu_autoSelectCallNumber_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            // �ı����
            menuItem = new MenuItem("�ı����(&B)");
            menuItem.Click += new System.EventHandler(this.menu_changeParent_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // ��������
            menuItem = new MenuItem("��������[Ctrl+A](&G)");
            menuItem.Click += new System.EventHandler(this.menu_generateData_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ������ȡ��
            menuItem = new MenuItem("������ȡ��(&M)");
            menuItem.Click += new System.EventHandler(this.menu_manageCallNumber_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // ������ȡ��
            menuItem = new MenuItem("������ȡ��(&C)");
            menuItem.Click += new System.EventHandler(this.menu_createCallNumber_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("װ���¿��Ĳᴰ(&E)");
            menuItem.Click += new System.EventHandler(this.menu_loadToNewItemForm_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("װ���Ѿ��򿪵Ĳᴰ(&E)");
            menuItem.Click += new System.EventHandler(this.menu_loadToExistItemForm_Click);
            if (this.listView.SelectedItems.Count == 0
                || this.MainForm.GetTopChildWindow<ItemInfoForm>() == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�쿴���¼�ļ����� (&K)");
            menuItem.Click += new System.EventHandler(this.menu_getKeys_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("���ɾ��(&D)");
            menuItem.Click += new System.EventHandler(this.menu_deleteEntity_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("����ɾ��(&U)");
            menuItem.Click += new System.EventHandler(this.menu_undoDeleteEntity_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView, new Point(e.X, e.Y));

        }

        void menu_loadToNewItemForm_Click(object sender, EventArgs e)
        {
#if NO
            string strError = "";

            if (this.ListView.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ����������";
                goto ERROR1;
            }

            BookItem cur = (BookItem)this.ListView.SelectedItems[0].Tag;

            if (cur == null)
            {
                strError = "bookitem == null";
                goto ERROR1;
            }

            string strRecPath = cur.RecPath;
            if (string.IsNullOrEmpty(strRecPath) == true)
            {
                strError = "��ѡ���������¼·��Ϊ�գ���δ�����ݿ��н���";
                goto ERROR1;
            }

            ItemInfoForm form = null;

            form = new ItemInfoForm();
            form.MdiParent = this.MainForm;
            form.MainForm = this.MainForm;
            form.Show();

            form.DbType = "item";

            form.LoadRecordByRecPath(strRecPath, "");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
#endif
            LoadToItemInfoForm(true);
        }

        void menu_loadToExistItemForm_Click(object sender, EventArgs e)
        {
#if NO
            string strError = "";

            if (this.ListView.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ����������";
                goto ERROR1;
            }

            BookItem cur = (BookItem)this.ListView.SelectedItems[0].Tag;

            if (cur == null)
            {
                strError = "bookitem == null";
                goto ERROR1;
            }

            string strRecPath = cur.RecPath;
            if (string.IsNullOrEmpty(strRecPath) == true)
            {
                strError = "��ѡ���������¼·��Ϊ�գ���δ�����ݿ��н���";
                goto ERROR1;
            }

            ItemInfoForm form = this.MainForm.GetTopChildWindow<ItemInfoForm>();
            if (form == null)
            {
                strError = "��ǰ��û���Ѿ��򿪵Ĳᴰ";
                goto ERROR1;
            }
            form.DbType = "item";
            Global.Activate(form);
            if (form.WindowState == FormWindowState.Minimized)
                form.WindowState = FormWindowState.Normal;

            form.LoadRecordByRecPath(strRecPath, "");
            return;
        ERROR1:
            MessageBox.Show(this, strError);
#endif
            LoadToItemInfoForm(false);
        }

        // �Զ������
        void menu_autoClearColumn_Click(object sender, EventArgs e)
        {
            // bool bOldChanged = this.Changed;

            MenuItem menu_item = (MenuItem)sender;

            ListViewHitTestInfo hittest_info = (ListViewHitTestInfo)menu_item.Tag;
            Debug.Assert(hittest_info.Item != null, "");
            int x = hittest_info.Item.SubItems.IndexOf(hittest_info.SubItem);
            Debug.Assert(x != -1, "");

            bool bChanged = false;
            foreach (ListViewItem item in this.listView.SelectedItems)
            {
                BookItem bookitem = (BookItem)item.Tag;

                string strTemp = ListViewUtil.GetItemText(item, x);
                if (String.IsNullOrEmpty(strTemp) == false)
                {
                    bookitem.SetColumnText(x, "");
                    bookitem.RefreshListView();
                    bChanged = true;
                }
            }
            if (bChanged == true)
            {
                this.Changed = bChanged;
            }
        }

        // ѡ��ȫ��ͬһ�ż���ϵ������
        void menu_autoSelectCallNumber_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.listView.SelectedItems.Count == 0)
            {
                strError = "����Ҫѡ��һ������";
                goto ERROR1;
            }

            List<string> arrangement_names = new List<string>();
            foreach (ListViewItem item in this.listView.SelectedItems)
            {
                BookItem bookitem = (BookItem)item.Tag;

                // TODO: #reservation, �����ô����
                string strLocation = bookitem.Location;

                ArrangementInfo info = null;
                // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = this.MainForm.GetArrangementInfo(strLocation,
            out info,
            out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0 || info == null)
                {
                    strError = "�ݲصص� '"+strLocation+"' û�ж����Ӧ���ż���ϵ";
                    goto ERROR1;
                }

                if (arrangement_names.IndexOf(info.ArrangeGroupName) == -1)
                    arrangement_names.Add(info.ArrangeGroupName);
            }

            Debug.Assert(arrangement_names.Count >= 1, "");

            if (arrangement_names.Count > 1)
            {
                strError = "����ѡ���� "+this.listView.SelectedItems.Count.ToString()+" �������У������˶���һ�����ż���ϵ�� " + StringUtil.MakePathList(arrangement_names) + "���뽫ѡ���ķ�ΧԼ���ڽ�����һ���ż���ϵ��Ȼ����ʹ�ñ�����";
                goto ERROR1;
            }

            string strName = arrangement_names[0];
            foreach (ListViewItem item in this.listView.Items)
            {
                BookItem bookitem = (BookItem)item.Tag;

                // TODO: #reservation, �����ô����
                string strLocation = bookitem.Location;

                ArrangementInfo info = null;
                // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = this.MainForm.GetArrangementInfo(strLocation,
            out info,
            out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0 || info == null)
                {
                    strError = "�ݲصص� '" + strLocation + "' û�ж����Ӧ���ż���ϵ";
                    goto ERROR1;
                }

                if (info.ArrangeGroupName == strName)
                {
                    item.Selected = true;
                }
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // �Զ�������
        void menu_autoCopyColumn_Click(object sender, EventArgs e)
        {
            // bool bOldChanged = this.Changed;

            MenuItem menu_item = (MenuItem)sender;

            ListViewHitTestInfo hittest_info = (ListViewHitTestInfo)menu_item.Tag;
            Debug.Assert(hittest_info.Item != null, "");
            int x = hittest_info.Item.SubItems.IndexOf(hittest_info.SubItem);
            Debug.Assert(x != -1, "");

            string strFirstText = "";
            foreach (ListViewItem item in this.listView.SelectedItems)
            {
                string strTemp = ListViewUtil.GetItemText(item, x);
                if (String.IsNullOrEmpty(strTemp) == false)
                {
                    strFirstText = strTemp;
                    break;
                }
            }

            if (string.IsNullOrEmpty(strFirstText) == true)
            {
                MessageBox.Show(this, "�� " + this.listView.Columns[x].Text + " ��û���ҵ��ɸ��Ƶ�ֵ...");
                return;
            }

            bool bChanged = false;
            foreach (ListViewItem item in this.listView.SelectedItems)
            {
                BookItem bookitem = (BookItem)item.Tag;

                string strTemp = ListViewUtil.GetItemText(item, x);
                if (String.IsNullOrEmpty(strTemp) == true)
                {
                    bookitem.SetColumnText(x, strFirstText);
                    // ListViewUtil.ChangeItemText(item, x, strFirstText);
                    bookitem.RefreshListView();
                    bChanged = true;
                }
            }
            if (bChanged == true)
            {
                /*
                if (this.ContentChanged != null)
                {
                    ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                    e1.OldChanged = bOldChanged;
                    e1.CurrentChanged = this.Changed;
                    this.ContentChanged(this, e1);
                }
                 * */
                this.Changed = bChanged;
            }
        }

        // ȫѡ
        void menu_selectAll_Click(object sender, EventArgs e)
        {
            ListViewUtil.SelectAllLines(this.listView);
            /*
            for (int i = 0; i < this.ListView.Items.Count; i++)
            {
                this.ListView.Items[i].Selected = true;
            }
             * */
        }

        // ��������
        void menu_generateData_Click(object sender, EventArgs e)
        {
#if NO
            if (this.GenerateData != null)
            {
                GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                e1.FocusedControl = this.ListView;
                e1.ScriptEntry = "";    // ����Ctrl+A�˵�
                this.GenerateData(this, e1);
            }
            else
            {
                MessageBox.Show(this, this.GetType().ToString() + "�ؼ�û�йҽ� GenerateData �¼�");
            }
#endif
            this.DoGenerateData("");    // ����Ctrl+A�˵�
        }

        // ������ȡ��
        void menu_manageCallNumber_Click(object sender, EventArgs e)
        {
#if NO
            if (this.GenerateData != null)
            {
                GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                e1.FocusedControl = this.ListView;
                e1.ScriptEntry = "ManageCallNumber";    // ֱ������ManageCallNumber()����
                this.GenerateData(this, e1);
            }
            else
            {
                MessageBox.Show(this, "EntityControlû�йҽ�GenerateData�¼�");
            }
#endif
            this.DoGenerateData("ManageCallNumber");    // ֱ������ManageCallNumber()����

        }

        // ������ȡ��
        void menu_createCallNumber_Click(object sender, EventArgs e)
        {
#if NO
            if (this.GenerateData != null)
            {
                GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                e1.FocusedControl = this.ListView;
                e1.ScriptEntry = "CreateCallNumber";    // ֱ������CreateCallNumber()����
                this.GenerateData(this, e1);
            }
            else
            {
                MessageBox.Show(this, "EntityControlû�йҽ�GenerateData�¼�");
            }
#endif
            this.DoGenerateData("CreateCallNumber");    // ֱ������CreateCallNumber()����

        }

        // 
        /// <summary>
        /// ��listview��ѡ��ָ��������
        /// </summary>
        /// <param name="bClearSelectionFirst">�Ƿ���ѡ��ǰ���ȫ�����е�ѡ��״̬</param>
        /// <param name="bookitems">Ҫѡ���������</param>
        /// <returns>����ѡ�����������</returns>
        public int SelectItems(
            bool bClearSelectionFirst,
            List<BookItem> bookitems)
        {
            if (bClearSelectionFirst == true)
                ListViewUtil.ClearSelection(this.listView);

            int nSelectedCount = 0;
            foreach (BookItem item in bookitems)
            {
                int nRet = this.Items.IndexOf(item);
                if (nRet == -1)
                    continue;
                item.ListViewItem.Selected = true;
                nSelectedCount++;
            }

            return nSelectedCount;
        }

        // 
        // return:
        //      -1  ����
        //      0   ��������
        //      1   �Ѿ�����
        /// <summary>
        /// Ϊ��ǰѡ�����������ȡ��
        /// </summary>
        /// <param name="bOverwriteExist">true: ��ȫ��ѡ����������´�����ȡ��; false: ֻ�е�ǰ��ȡ���ַ���Ϊ�յĲŸ����д��������ŵĲ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ��������; 1: �Ѿ�����</returns>
        public int CreateCallNumber(
            bool bOverwriteExist,
            out string strError)
        {
            strError = "";

#if NO
            if (this.GenerateData == null)
            {
                strError = "EntityControlû�йҽ�GenerateData�¼�";
                return -1;
            }
#endif
            if (this.HasGenerateData() == false)
            {
                strError = "EntityControl û�йҽ� GenerateData �¼�";
                return -1;
            }

            if (bOverwriteExist == false)
            {
                // ֻ�е�ǰ��ȡ���ַ���Ϊ�յĲŸ����д��������ŵĲ���
                List<ListViewItem> items = new List<ListViewItem>();
                foreach (ListViewItem item in this.listView.SelectedItems)
                {
                    BookItem bookitem = (BookItem)item.Tag;

                    if (string.IsNullOrEmpty(bookitem.AccessNo) == false)
                        items.Add(item);
                }

                foreach (ListViewItem item in items)
                {
                    item.Selected = false;
                }

                if (this.listView.SelectedItems.Count == 0)
                    return 0;
            }

#if NO
            GenerateDataEventArgs e1 = new GenerateDataEventArgs();
            e1.FocusedControl = this.ListView;
            e1.ScriptEntry = "CreateCallNumber";    // ֱ������CreateCallNumber()����
            e1.ShowErrorBox = false;
            this.GenerateData(this, e1);
#endif
            GenerateDataEventArgs e1 = this.DoGenerateData("CreateCallNumber", false);// ֱ������CreateCallNumber()����
            if (e1 == null)
            {
                strError = "e1 null";
                return -1;
            }

            if (string.IsNullOrEmpty(e1.ErrorInfo) == false)
            {
                strError = e1.ErrorInfo;
                return -1;
            }

            return 1;
        }

        private void listView_items_DoubleClick(object sender, EventArgs e)
        {
            menu_modifyEntity_Click(this, null);
        }

        // ����
        void menu_cutEntity_Click(object sender, EventArgs e)
        {
            ClipboardBookItemCollection newbookitems = new ClipboardBookItemCollection();

            string strNotDeleteList = "";
            int nDeleteCount = 0;

            // bool bOldChanged = this.Changed;

            List<BookItem> deleteitems = new List<BookItem>();

            // �ȼ��һ���н�����Ϣ����ɾ�������
            for (int i = 0; i < this.listView.Items.Count; i++)
            {
                ListViewItem item = this.listView.Items[i];

                if (item.Selected == false)
                    continue;

                BookItem bookitem = (BookItem)item.Tag;

                if (String.IsNullOrEmpty(bookitem.Borrower) == false)
                {
                    if (strNotDeleteList != "")
                        strNotDeleteList += ",";
                    strNotDeleteList += bookitem.Barcode;
                    continue;
                }

                nDeleteCount++;
                deleteitems.Add(bookitem);
            }

            if (strNotDeleteList != "")
            {
                string strText = "����Ϊ '"
                    + strNotDeleteList +
                    "' �Ĳ��������ͨ��Ϣ, ���ܼ��Ա��ɾ����\r\n\r\n";

                if (nDeleteCount == 0)
                {
                    // ��������ɾ�����������Ҳû��Ҫɾ��������
                    MessageBox.Show(ForegroundWindow.Instance, strText);
                    return;
                }


                strText += "�Ƿ�Ҫ������������ " + nDeleteCount.ToString() + " ��?";

                DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
                    strText,
                    "EntityForm",
MessageBoxButtons.YesNo,
MessageBoxIcon.Question,
MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                    return;
            }

            for (int i = 0; i < deleteitems.Count; i++)
            {
                BookItem bookitem = deleteitems[i];

                // ��¡�������
                BookItem dupitem = bookitem.Clone();

                newbookitems.Add(dupitem);
                // �����漰��Դ�����޸�Ϊdeleted״̬

                int nRet = MaskDeleteItem(bookitem,
                    m_bRemoveDeletedItem);
                if (nRet == 0)
                {
                    Debug.Assert(false, "����MaskDeleteItem()�����ܳ��ַ���0�����ѽ����Ϊǰ���Ѿ�Ԥ�й���");
                    continue;
                }
            }

            Clipboard.SetDataObject(newbookitems, true);

            // this.SetSaveAllButtonState(true);
            /*
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = this.Changed;
                this.ContentChanged(this, e1);
            }
             * */
            this.Changed = this.Changed;
        }

        // ����
        void menu_copyEntity_Click(object sender, EventArgs e)
        {
            ClipboardBookItemCollection newbookitems = new ClipboardBookItemCollection();


            for (int i = 0; i < this.listView.Items.Count; i++)
            {
                ListViewItem item = this.listView.Items[i];

                if (item.Selected == false)
                    continue;

                BookItem bookitem = (BookItem)item.Tag;

                // ��¡�������
                BookItem dupitem = bookitem.Clone();

                newbookitems.Add(dupitem);
            }

            // DataObject obj = new DataObject(newbookitems);

            Clipboard.SetDataObject(newbookitems, true);
        }

        // ʵ��ճ��
        int DoPaste(out string strError)
        {
            strError = "";

            // bool bOldChanged = this.Changed;

            /*
if (String.IsNullOrEmpty(this.BiblioRecPath) == true)
{
    strError = "��δ������Ŀ��¼���޷��������Ϣ";
    goto ERROR1;
}
 * */
            IDataObject iData = Clipboard.GetDataObject();
            if (iData == null
                || iData.GetDataPresent(typeof(ClipboardBookItemCollection)) == false)
            {
                strError = "���������в�����ClipboardBookItemCollection��������";
                return -1;
            }

            ClipboardBookItemCollection clipbookitems = (ClipboardBookItemCollection)iData.GetData(typeof(ClipboardBookItemCollection));
            if (clipbookitems == null)
            {
                strError = "iData.GetData() return null";
                return -1;
            }

            clipbookitems.RestoreNonSerialized();

            if (this.Items == null)
                this.Items = new BookItemCollection();

            Debug.Assert(this.Items != null, "");

            this.Items.ClearListViewHilight();

            // ׼�������õ�refid���
            Hashtable table = new Hashtable();
            foreach (BookItem bookitem in this.Items)
            {
                if (string.IsNullOrEmpty(bookitem.RefID) == false)
                {
                    if (table.Contains(bookitem.RefID) == true)
                    {
                        strError = "ԭ�в������г������ظ��Ĳο�IDֵ '" + bookitem.RefID + "'";
                        return -1;
                    }

                    if (table.Contains(bookitem.RefID) == false)
                        table.Add(bookitem.RefID, null);
                }
            }

            // ��������paste�����������������û�кͱ��������������ظ��ģ�
            string strDupBarcodeList = "";
            for (int i = 0; i < clipbookitems.Count; i++)
            {
                BookItem bookitem = clipbookitems[i];

                string strBarcode = bookitem.Barcode;

                // refid����
                if (string.IsNullOrEmpty(bookitem.RefID) == false)
                {
                    if (table.Contains(bookitem.RefID) == true)
                    {
                        /*
                        strError = "������������г������ظ��Ĳο�IDֵ '" + bookitem.RefID + "'";
                        return -1;
                         * */
                        bookitem.RefID = "";    // ��ʹ�Ժ����·���
                    }

                    if (table.Contains(bookitem.RefID) == false)
                        table.Add(bookitem.RefID, null);
                }

                if (String.IsNullOrEmpty(strBarcode) == true)
                    continue;   // 2008/11/3


                // �Ե�ǰ�����ڽ����������
                BookItem dupitem = this.Items.GetItemByBarcode(strBarcode);
                if (dupitem != null)
                {
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                    {
                    }
                    else
                    {
                        // ͻ����ʾ���Ա������Ա�۲������Ѿ����ڵļ�¼
                        dupitem.HilightListViewItem(false);

                        // �����ظ������б�
                        if (strDupBarcodeList != "")
                            strDupBarcodeList += ",";
                        strDupBarcodeList += strBarcode;
                    }
                }
            }

            bool bOverwrite = false;

            if (String.IsNullOrEmpty(strDupBarcodeList) == false)
            {
                DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
    "����ճ�����������������ڵ�ǰ�������Ѿ�����:\r\n" + strDupBarcodeList + "\r\n\r\n�Ƿ�Ҫ������Щ����? (Yes ���� / No ������Щ���������ճ���������� / Cancel ��������ճ������)",
    "EntityForm",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Cancel)
                    return 0;
                if (result == DialogResult.Yes)
                    bOverwrite = true;
                else
                    bOverwrite = false;
            }

            for (int i = 0; i < clipbookitems.Count; i++)
            {
                BookItem bookitem = clipbookitems[i];

                string strBarcode = bookitem.Barcode;

                BookItem dupitem = null;

                if (String.IsNullOrEmpty(strBarcode) == false)  // 2008/11/3
                {
                    // �Ե�ǰ�����ڽ����������
                    dupitem = this.Items.GetItemByBarcode(strBarcode);
                    if (dupitem != null)
                    {
                        if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                            this.Items.PhysicalDeleteItem(dupitem);
                        else
                        {
                            if (bOverwrite == false)
                                continue;
                            else
                                this.Items.PhysicalDeleteItem(dupitem);
                        }
                    }
                }

                // ����
                bookitem.Parent = Global.GetRecordID(this.BiblioRecPath);

                this.Items.Add(bookitem);

                if (dupitem != null)
                {
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                    {
                        bookitem.ItemDisplayState = ItemDisplayState.Changed;
                        bookitem.Timestamp = dupitem.Timestamp; // �̳е�ǰ���������timestamp
                    }
                    else if (dupitem.ItemDisplayState == ItemDisplayState.Changed
                        || dupitem.ItemDisplayState == ItemDisplayState.Normal)
                    {
                        bookitem.ItemDisplayState = ItemDisplayState.Changed;
                        bookitem.Timestamp = dupitem.Timestamp; // �̳е�ǰ���������timestamp
                    }
                    else
                        bookitem.ItemDisplayState = ItemDisplayState.New;
                }
                else
                    bookitem.ItemDisplayState = ItemDisplayState.New;

                bookitem.Changed = true;    // ��Ϊ�����������������ζ����޸Ĺ����������Ա��⼯����ֻ��һ�����������ʱ�򣬼��ϵ�changedֵ����
                bookitem.AddToListView(this.listView);
                bookitem.HilightListViewItem(false);
            }

            // this.SetSaveAllButtonState(true);
            /*
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = this.Changed;
                this.ContentChanged(this, e1);
            }
             * */
            this.Changed = this.Changed;

            return 0;
        }

        // ճ��
        void menu_pasteEntity_Click(object sender, EventArgs e)
        {
            int nRet = 0;
            string strError = "";

            nRet = DoPaste(out strError);
            if (nRet == -1)
                MessageBox.Show(ForegroundWindow.Instance, strError);
        }

        // �޸�һ��ʵ��
        void menu_modifyEntity_Click(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "��δѡ��Ҫ�༭������");
                return;
            }
            BookItem bookitem = (BookItem)this.listView.SelectedItems[0].Tag;

            ModifyEntity(bookitem);
        }

        void ModifyEntity(BookItem bookitem)
        {
            // BookItem bookitem = (BookItem)this.listView_items.SelectedItems[0].Tag;

            Debug.Assert(bookitem != null, "");

            string strOldBarcode = bookitem.Barcode;

            EntityEditForm edit = new EntityEditForm();

            // 2009/2/24 
            edit.GenerateData -= new GenerateDataEventHandler(edit_GenerateData);
            edit.GenerateData += new GenerateDataEventHandler(edit_GenerateData);

            /*
            edit.GenerateAccessNo -= new GenerateDataEventHandler(edit_GenerateAccessNo);
            edit.GenerateAccessNo += new GenerateDataEventHandler(edit_GenerateAccessNo);
             * */

            edit.BiblioDbName = Global.GetDbName(this.BiblioRecPath);   // 2009/2/15 add 
            edit.MainForm = this.MainForm;
            edit.ItemControl = this;
            string strError = "";
            int nRet = edit.InitialForEdit(bookitem,
                this.Items,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(ForegroundWindow.Instance, strError);
                return;
            }
            edit.StartItem = null;  // ���ԭʼ������

        REDO:
            this.MainForm.AppInfo.LinkFormState(edit, "EntityEditForm_state");
            edit.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(edit);

            if (edit.DialogResult != DialogResult.OK)
                return;

            // BookItem�����Ѿ����޸�

            this.EnableControls(false);
            try
            {


                if (strOldBarcode != bookitem.Barcode // ����ı��˵�����²Ų���
                    && String.IsNullOrEmpty(bookitem.Barcode) == false)   // 2008/11/3 �����յ�������Ƿ��ظ�
                {

                    // �Ե�ǰ�����ڽ����������
                    BookItem dupitem = this.Items.GetItemByBarcode(bookitem.Barcode);
                    if (dupitem != bookitem)
                    {
                        string strText = "";
                        if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                            strText = "������� '" + bookitem.Barcode + "' �ͱ�����δ�ύ֮һɾ������������ء�����ȷ������ť�������룬���˳��Ի���������ύ����֮�޸ġ�";
                        else
                            strText = "������� '" + bookitem.Barcode + "' �ڱ������Ѿ����ڡ�����ȷ������ť�������롣";

                        MessageBox.Show(ForegroundWindow.Instance, strText);
                        goto REDO;
                    }

                    // ������ʵ���¼�����������
                    if (edit.AutoSearchDup == true
                        && string.IsNullOrEmpty(bookitem.Barcode) == false)
                    {
                        // Debug.Assert(false, "");

                        string[] paths = null;
                        // ������Ų��ء�����(������)������Ų��ء�
                        // parameters:
                        //      strBarcode  ������š�
                        //      strOriginRecPath    ������¼��·����
                        //      paths   �������е�·��
                        // return:
                        //      -1  error
                        //      0   not dup
                        //      1   dup
                        nRet = SearchEntityBarcodeDup(bookitem.Barcode,
                            bookitem.RecPath,
                            out paths,
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(ForegroundWindow.Instance, "�Բ������ '" + bookitem.Barcode + "' ���в��صĹ����з�������: " + strError);
                        else if (nRet == 1) // �����ظ�
                        {
                            string pathlist = String.Join(",", paths);

                            string strText = "���� '" + bookitem.Barcode + "' �����ݿ��з����Ѿ���(���������ֵ�)���в��¼��ʹ�á�\r\n" + pathlist + "\r\n\r\n����ȷ������ť���±༭����Ϣ�����߸�����ʾ�Ĳ��¼·����ȥ�޸��������¼��Ϣ��";
                            MessageBox.Show(ForegroundWindow.Instance, strText);

                            goto REDO;
                        }
                    }
                }

            }
            finally
            {
                this.EnableControls(true);
            }
        }

        /*
        void edit_GenerateAccessNo(object sender, GenerateDataEventArgs e)
        {
            if (this.GenerateAccessNo != null)
            {
                this.GenerateAccessNo(sender, e);
            }
            else
            {
                MessageBox.Show(this, "EntityControlû�йҽ�GenerateAccessNo�¼�");
            }
        }*/

        void edit_GenerateData(object sender, GenerateDataEventArgs e)
        {
#if NO
            if (this.GenerateData != null)
            {
                this.GenerateData(sender, e);
            }
            else
            {
                MessageBox.Show(this, "EntityControlû�йҽ�GenerateData�¼�");
            }
#endif
            this.DoGenerateData(sender, e);
        }

        // �������
        // return:
        //      -1  ����
        //      0   ���ظ�
        //      1   �ظ�
        /// <summary>
        /// ��һ��������в�����Ų���
        /// </summary>
        /// <param name="strBarcode">�������</param>
        /// <param name="myself">������صĶ���</param>
        /// <param name="bCheckCurrentList">�Ƿ�Ҫ��鵱ǰ�б��е�(��δ�����)����</param>
        /// <param name="bCheckDb">�Ƿ�����ݿ���в���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ���ظ�; 1: ���ظ�</returns>
        public int CheckBarcodeDup(
            string strBarcode,
            BookItem myself,
            bool bCheckCurrentList,
            bool bCheckDb,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (bCheckCurrentList == true)
            {
                // �Ե�ǰlist�ڽ����������
                BookItem dupitem = this.Items.GetItemByBarcode(strBarcode);
                if (dupitem != null && dupitem != myself)
                {
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strError = "������� '" + strBarcode + "' �ͱ�����δ�ύ֮һɾ������������ء�(��Ҫ���������������Ҫ�˳��Ի���������ύ����֮�޸�)";
                    else
                        strError = "������� '" + strBarcode + "' �ڱ������Ѿ����ڡ�";
                    return 1;
                }
            }

            // ������ʵ���¼�����������
            if (bCheckDb == true)
            {
                string strOriginRecPath = "";

                if (myself != null)
                    strOriginRecPath = myself.RecPath;

                string[] paths = null;
                // ������Ų��ء�����(������)������Ų��ء�
                // parameters:
                //      strBarcode  ������š�
                //      strOriginRecPath    ������¼��·����
                //      paths   �������е�·��
                // return:
                //      -1  error
                //      0   not dup
                //      1   dup
                nRet = SearchEntityBarcodeDup(strBarcode,
                    strOriginRecPath,
                    out paths,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�Բ����� '" + strBarcode + "' ���в��صĹ����з�������: " + strError;
                    return -1;
                }
                else if (nRet == 1) // �����ظ�
                {
                    string pathlist = String.Join(",", paths);

                    strError = "����� '" + strBarcode + "' �����ݿ��з����Ѿ���(���������ֵ�)���в��¼��ʹ�á�\r\n" + pathlist + "\r\n\r\n�������������������Ƿ���ȷ���������ʾ�Ĳ��¼·����ȥ�޸��������¼��Ϣ������������ظ���";
                    return 1;
                }
            }

            return 0;
        }


        // ��һ��������������
        // return:
        //      -1  ����
        //      0   ���ظ�
        //      1   �ظ�
        /// <summary>
        /// ��һ��������в�����Ų���
        /// </summary>
        /// <param name="book_items">Ҫ���в��ص������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ���ظ�; 1: ���ظ�</returns>
        public int CheckBarcodeDup(
            List<BookItem> book_items,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            for (int i = 0; i < book_items.Count; i++)
            {
                BookItem myself = book_items[i];
                string strBarcode = myself.Barcode;

                if (String.IsNullOrEmpty(strBarcode) == true)
                    continue;

                {
                    // �Ե�ǰlist�ڽ����������
                    BookItem dupitem = this.Items.GetItemByBarcode(strBarcode);
                    if (dupitem != null && dupitem != myself)
                    {
                        if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                            strError += "������� '" + strBarcode + "' �ͱ�����δ�ύ֮һɾ������������ء�(��Ҫ���������������Ҫ�����ύ����֮�޸�); ";
                        else
                            strError += "������� '" + strBarcode + "' �ڱ������Ѿ�����; ";
                        continue;   // �Ͳ��ټ�����ȫ���ݿ���
                    }
                }

                // ������ʵ���¼�����������
                {
                    string strOriginRecPath = "";

                    if (myself != null)
                        strOriginRecPath = myself.RecPath;

                    string[] paths = null;
                    string strTempError = "";
                    // ������Ų��ء�����(������)������Ų��ء�
                    // parameters:
                    //      strBarcode  ������š�
                    //      strOriginRecPath    ������¼��·����
                    //      paths   �������е�·��
                    // return:
                    //      -1  error
                    //      0   not dup
                    //      1   dup
                    nRet = SearchEntityBarcodeDup(strBarcode,
                        strOriginRecPath,
                        out paths,
                        out strTempError);
                    if (nRet == -1)
                    {
                        strError = "�Բ������ '" + strBarcode + "' ���в��صĹ����з�������: " + strTempError;
                        return -1;
                    }
                    else if (nRet == 1) // �����ظ�
                    {
                        string pathlist = String.Join(",", paths);

                        strError += "���� '" + strBarcode + "' �����ݿ��з����Ѿ���(���������ֵ�)���в��¼��ʹ��: " + pathlist + "; ";
                    }
                }
            }

            if (String.IsNullOrEmpty(strError) == false)
                return 1;

            return 0;
        }

        // ����һ��ʵ��
        void menu_newEntity_Click(object sender, EventArgs e)
        {
            DoNewEntity("");
        }

        // �������ʵ��
        void menu_newMultiEntity_Click(object sender, EventArgs e)
        {
            int nRet = 0;
            string strError = "";
            // bool bOldChanged = this.Changed;

        REDO_INPUT:
            string strNumber = InputDlg.GetInput(
                this,
                "�������ʵ��",
                "Ҫ�����ĸ���: ",
                "2",
            this.MainForm.DefaultFont);
            if (strNumber == null)
                return;

            int nNumber = 0;
            try
            {
                nNumber = Convert.ToInt32(strNumber);
            }
            catch
            {
                MessageBox.Show(ForegroundWindow.Instance, "�������봿����");
                goto REDO_INPUT;
            }

            for (int i = 0; i < nNumber; i++)
            {
                BookItem bookitem = new BookItem();

                // ����ȱʡֵ
                nRet = SetItemDefaultValues(
                    "normalRegister_default",
                    true,
                    bookitem,
                    out strError);
                if (nRet == -1)
                {
                    strError = "����ȱʡֵ��ʱ��������: " + strError;
                    goto ERROR1;
                }


                bookitem.Barcode = "";
                bookitem.Parent = Global.GetRecordID(this.BiblioRecPath);


                // �����б�
                this.Items.Add(bookitem);
                bookitem.ItemDisplayState = ItemDisplayState.New;
                bookitem.AddToListView(this.listView);
                bookitem.HilightListViewItem(true);

                bookitem.Changed = true;    // ��Ϊ�����������������ζ����޸Ĺ����������Ա��⼯����ֻ��һ�����������ʱ�򣬼��ϵ�changedֵ����
            }

            // �ı䱣�水ť״̬
            // SetSaveAllButtonState(true);
            /*
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = this.Changed;
                this.ContentChanged(this, e1);
            }
            */
            this.Changed = this.Changed;

            return;

        ERROR1:
            MessageBox.Show(ForegroundWindow.Instance, strError);
            return;
        }

        // 
        /// <summary>
        /// ����һ��ʵ�壬Ҫ�򿪶Ի�����������ϸ��Ϣ
        /// </summary>
        /// <param name="strBarcode">�������</param>
        public void DoNewEntity(string strBarcode)
        {
            string strError = "";
            int nRet = 0;

            // bool bOldChanged = this.Changed;

            if (String.IsNullOrEmpty(this.BiblioRecPath) == true)
            {
                strError = "��δ������Ŀ��¼";
                goto ERROR1;
            }

            // 
            if (this.Items == null)
                this.Items = new BookItemCollection();

            Debug.Assert(this.Items != null, "");

            if (String.IsNullOrEmpty(strBarcode) == false)
            {

                // �Ե�ǰ�����ڽ����������
                BookItem dupitem = this.Items.GetItemByBarcode(strBarcode);
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "�������Ĳ������ '" + strBarcode + "' �ͱ�����δ�ύ֮һɾ������������ء��������ύ����֮�޸ģ��ٽ��в�Ǽǡ�";
                    else
                        strText = "�������Ĳ������ '" + strBarcode + "' �ڱ������Ѿ����ڡ�";

                    // ������δ����
                    DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
        strText + "\r\n\r\nҪ�������Ѵ�����������޸���",
        "EntityForm",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button2);

                    // תΪ�޸�
                    if (result == DialogResult.Yes)
                    {
                        /*
                        // ��UndoǱ�ڵ�Delete״̬
                        this.bookitems.UndoMaskDeleteItem(dupitem);
                         * */

                        ModifyEntity(dupitem);
                        return;
                    }

                    // ͻ����ʾ���Ա������Ա�۲������Ѿ����ڵļ�¼
                    dupitem.HilightListViewItem(true);
                    return;
                }

                // ������ʵ���¼�����������
                if (true)
                {
                    string strItemText = "";
                    string strBiblioText = "";
                    nRet = SearchEntityBarcode(strBarcode,
                        out strItemText,
                        out strBiblioText,
                        out strError);
                    if (nRet == -1)
                        MessageBox.Show(ForegroundWindow.Instance, "�Բ������ '" + strBarcode + "' ���в��صĹ����з�������: " + strError);
                    else if (nRet == 1) // �����ظ�
                    {
                        EntityBarcodeFoundDupDlg dlg = new EntityBarcodeFoundDupDlg();
                        MainForm.SetControlFont(dlg, this.Font, false);
                        dlg.MainForm = this.MainForm;
                        dlg.BiblioText = strBiblioText;
                        dlg.ItemText = strItemText;
                        dlg.MessageText = "�������Ĳ������ '" + strBarcode + "' �����ݿ��з����Ѿ����ڡ�����޷�������";
                        dlg.ShowDialog(this);
                        return;
                    }
                }

            } // end of ' if (String.IsNullOrEmpty(strBarcode) == false)

            BookItem bookitem = new BookItem();

            // ����ȱʡֵ
            nRet = SetItemDefaultValues(
                "normalRegister_default",
                true,
                bookitem,
                out strError);
            if (nRet == -1)
            {
                strError = "����ȱʡֵ��ʱ��������: " + strError;
                goto ERROR1;
            }


            bookitem.Barcode = strBarcode;
            bookitem.Parent = Global.GetRecordID(this.BiblioRecPath);


            // �ȼ����б�
            this.Items.Add(bookitem);
            bookitem.ItemDisplayState = ItemDisplayState.New;
            bookitem.AddToListView(this.listView);
            bookitem.HilightListViewItem(true);

            bookitem.Changed = true;    // ��Ϊ�����������������ζ����޸Ĺ����������Ա��⼯����ֻ��һ�����������ʱ�򣬼��ϵ�changedֵ����


            EntityEditForm edit = new EntityEditForm();

            // 2009/2/24 
            edit.GenerateData -= new GenerateDataEventHandler(edit_GenerateData);
            edit.GenerateData += new GenerateDataEventHandler(edit_GenerateData);

            /*
            edit.GenerateAccessNo -= new GenerateDataEventHandler(edit_GenerateAccessNo);
            edit.GenerateAccessNo += new GenerateDataEventHandler(edit_GenerateAccessNo);
             * */

            edit.BiblioDbName = Global.GetDbName(this.BiblioRecPath);   // 2009/2/15 add
            edit.Text = "������";
            edit.MainForm = this.MainForm;
            edit.ItemControl = this;
            edit.DisplayMode = this.MainForm.AppInfo.GetBoolean(
    "entityform_optiondlg",
    "normalRegister_simple",
    false) == true ? "simple" : "full";
            nRet = edit.InitialForEdit(bookitem,
                this.Items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            //REDO:
            this.MainForm.AppInfo.LinkFormState(edit, "EntityEditForm_state");
            edit.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(edit);

            if (edit.DialogResult != DialogResult.OK
                && edit.Item == bookitem    // ������δǰ���ƶ��������ƶ��ص���㣬Ȼ��Cancel
                )
            {
                this.Items.PhysicalDeleteItem(bookitem);

                // �ı䱣�水ť״̬
                // SetSaveAllButtonState(true);
                /*
                if (this.ContentChanged != null)
                {
                    ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                    e1.OldChanged = bOldChanged;
                    e1.CurrentChanged = this.Changed;
                    this.ContentChanged(this, e1);
                }
                 * */
                this.Changed = this.Changed;

                return;
            }

            // �ı䱣�水ť״̬
            // SetSaveAllButtonState(true);
            /*
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = this.Changed;
                this.ContentChanged(this, e1);
            }
             * */
            this.Changed = this.Changed;


#if NOOOOOOOOOOOOOO
            this.EnableControls(false);
            try
            {
                // ������ʵ���¼�����������
                if (edit.AutoSearchDup == true)
                {
                    string strItemText = "";
                    string strBiblioText = "";
                    nRet = SearchEntityBarcode(bookitem.Barcode,
                        out strItemText,
                        out strBiblioText,
                        out strError);
                    if (nRet == -1)
                        MessageBox.Show(ForegroundWindow.Instance, "�Բ������ '" + bookitem.Barcode + "' ���в��صĹ����з�������: " + strError);
                    else if (nRet == 1) // �����ظ�
                    {
                        EntityBarcodeFoundDupDlg dlg = new EntityBarcodeFoundDupDlg();
                        dlg.MainForm = this.MainForm;
                        dlg.BiblioText = strBiblioText;
                        dlg.ItemText = strItemText;
                        dlg.MessageText = "�������Ĳ���Ϣ�У����� '" + bookitem.Barcode + "' �����ݿ��з����Ѿ����ڡ�����ȷ������ť�������롣";
                        dlg.ShowDialog(this);
                        goto REDO;
                    }
                }

                /*
                this.bookitems.Add(bookitem);
                bookitem.ItemDisplayState = ItemDisplayState.New;
                bookitem.AddToListView(this.listView_items);
                bookitem.HilightListViewItem();
                 * */
            }
            finally
            {
                this.EnableControls(true);
            }
#endif
            // TODO: 2007/10/23
            // Ҫ�Ա��ֺ��������ʵ������������ء�
            // ������ˣ�Ҫ���ִ��ڣ��Ա��޸ġ�����������Ƕȣ���������ڶԻ���ر�ǰ����
            // �������´򿪶Ի���
            return;

        ERROR1:
            MessageBox.Show(ForegroundWindow.Instance, strError);
            return;
        }

        // �ⲿ���ã�����һ��ʵ���¼��
        // ���嶯���У�new change delete neworchange
        // parameters:
        //      bWarningBarcodeDup  �Ƿ�������������ص������==true�������棬������Ȼ������¼��==false���������������Ӻ����з���
        //      bookitem    [out]������ص�BookItem����
        // return:
        //      0   ��������޸ġ�ɾ���ɹ���û�з��ֲ������ظ�
        //      1   ����ɹ������Ƿ����˲������ظ�
        /// <summary>
        /// ����һ��ʵ���¼
        /// </summary>
        /// <param name="bWarningBarcodeDup">�Ƿ�������������ص������==true�������棬������Ȼ������¼��==false���������������Ӻ����з���</param>
        /// <param name="strAction">������Ϊ new change delete neworchange ֮һ</param>
        /// <param name="strRefID">�ο� ID</param>
        /// <param name="strXml">��¼ XML</param>
        /// <param name="bookitem">������ص� BookItem ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ��������޸ġ�ɾ���ɹ���û�з��ֲ������ظ�; 1: ����ɹ������Ƿ����˲������ظ�</returns>
        public int DoSetEntity(
            bool bWarningBarcodeDup,
            string strAction,
            string strRefID,
            string strXml,
            out BookItem bookitem,
            out string strError)
        {
            int nRet = 0;
            strError = "";
            bookitem = null;
            string strWarning = "";

            if (String.IsNullOrEmpty(this.BiblioRecPath) == true)
            {
                strError = "��δ������Ŀ��¼";
                return -1;
            }

            if (String.IsNullOrEmpty(strRefID) == true)
            {
                strError = "strRefID����ֵ����Ϊ��";
                return -1;
            }

            // 2008/9/17 
            if (this.Items == null)
                this.Items = new BookItemCollection();

            // �����Ƿ����Ѿ����ڵļ�¼
            BookItem exist_item = this.Items.GetItemByRefID(strRefID) as BookItem;

            // 2009/12/16 
            if (exist_item != null)
            {
                if (strAction == "neworchange")
                    strAction = "change";
            }
            else
            {
                if (strAction == "neworchange")
                    strAction = "new";
            }

            if (exist_item != null)
            {
                if (strAction == "new")
                {
                    strError = "refidΪ'" + strRefID + "' �������Ѿ����ڣ��������ظ�����";
                    return -1;
                }
            }
            else
            {
                if (strAction == "change")
                {
                    strError = "refidΪ'" + strRefID + "' ����������ڣ��޷������޸�";
                    return -1;
                }

                if (strAction == "delete")
                {
                    strError = "refidΪ'" + strRefID + "' ����������ڣ��޷�����ɾ��";
                    return -1;
                }
            }

            string strOperName = "";
            if (strAction == "new")
                strOperName = "����";
            else if (strAction == "change")
                strOperName = "�޸�";
            else if (strAction == "delete")
                strOperName = "ɾ��";

            if (strAction == "delete")
            {
                // ���ɾ������
                // return:
                //      0   ��Ϊ����ͨ��Ϣ��δ�ܱ��ɾ��
                //      1   �ɹ�ɾ��
                nRet = MaskDeleteItem(exist_item,
                         this.m_bRemoveDeletedItem);
                if (nRet == 0)
                {
                    strError = "refidΪ'" + strRefID + "' �Ĳ�������Ϊ��������ͨ��Ϣ���޷�����ɾ��";
                    return -1;
                }

                return 0;   // 1
            }


            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XML�ַ���װ��XMLDOMʱ����: " + ex.Message;
                return -1;
            }

            string strBarcode = DomUtil.GetElementText(dom.DocumentElement,
                "barcode");

            if (String.IsNullOrEmpty(strBarcode) == false)
            {
                // �Ե�ǰ�����ڽ����������
                BookItem dupitem = this.Items.GetItemByBarcode(strBarcode);
                if (dupitem != null)
                {
                    if (strAction == "change" || strAction == "delete")
                    {
                        if (exist_item == dupitem)
                            goto SKIP1;
                    }

                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strError = "��" + strOperName + "�Ĳ���Ϣ�У�������� '" + strBarcode + "' �ͱ�����δ�ύ֮һɾ�������������";
                    else
                        strError = "��" + strOperName + "�Ĳ���Ϣ�У�������� '" + strBarcode + "' �ڱ������Ѿ�����";

                    if (bWarningBarcodeDup == true)
                    {
                        if (string.IsNullOrEmpty(strWarning) == false)
                            strWarning += ";\r\n";
                        strWarning += strError;
                    }
                    else
                        return -1;
                }
            }

            SKIP1:

            // ������ʵ���¼�����������
            if (String.IsNullOrEmpty(strBarcode) == false 
                && strAction == "new")
            {
                string strItemText = "";
                string strBiblioText = "";
                nRet = SearchEntityBarcode(strBarcode,
                    out strItemText,
                    out strBiblioText,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�Բ������ '" + strBarcode + "' ���в��صĹ����з�������: " + strError;
                    return -1;
                }
                else if (nRet == 1) // �����ظ�
                {
                    strError = "�������Ĳ���Ϣ�У�������� '" + strBarcode + "' �����ݿ��з����Ѿ����ڡ�";
                    if (bWarningBarcodeDup == true)
                    {
                        if (string.IsNullOrEmpty(strWarning) == false)
                            strWarning += ";\r\n";
                        strWarning += strError;
                    }
                    else
                        return -1;
                }
            }

            // BookItem bookitem = null;

            if (strAction == "new")
            {
                bookitem = new BookItem();

                // ����ȱʡֵ
                nRet = SetItemDefaultValues(
                    "quickRegister_default",
                true,
                    bookitem,
                    out strError);
                if (nRet == -1)
                {
                    strError = "����ȱʡֵ��ʱ��������: " + strError;
                    return -1;
                }
            }
            else
                bookitem = exist_item;

            bookitem.Barcode = strBarcode;

            Debug.Assert(String.IsNullOrEmpty(strRefID) == false, "");

            bookitem.RefID = strRefID;

            // Ϊ�˱���BuildRecord()����
            bookitem.Parent = Global.GetRecordID(this.BiblioRecPath);

            if (exist_item == null)
            {

                string strExistXml = "";
                nRet = bookitem.BuildRecord(
                    true,   // Ҫ��� Parent ��Ա
                    out strExistXml,
                    out strError);
                if (nRet == -1)
                    return -1;

                XmlDocument domExist = new XmlDocument();
                try
                {
                    domExist.LoadXml(strExistXml);
                }
                catch (Exception ex)
                {
                    strError = "XML�ַ���strExistXmlװ��XMLDOMʱ����: " + ex.Message;
                    return -1;
                }

                // ��������һ��Ԫ�ص�����
                XmlNodeList nodes = dom.DocumentElement.SelectNodes("*");
                for (int i = 0; i < nodes.Count; i++)
                {
                    /*
                    string strText = nodes[i].InnerText;
                    if (String.IsNullOrEmpty(strText) == false)
                    {
                        DomUtil.SetElementText(domExist.DocumentElement,
                            nodes[i].Name, strText);
                    }*/

                    // 2009/12/17 changed
                    string strText = nodes[i].OuterXml;
                    if (String.IsNullOrEmpty(strText) == false)
                    {
                        DomUtil.SetElementOuterXml(domExist.DocumentElement,
                            nodes[i].Name, strText);
                    }
                }

                nRet = bookitem.SetData(bookitem.RecPath,
                    domExist.OuterXml,
                    null,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
            else
            {
                // ע: OldRecord/Timestamp��ϣ�����ı� 2010/3/22
                string strOldXml = bookitem.OldRecord;
                nRet = bookitem.SetData(bookitem.RecPath,
                    strXml,
                    bookitem.Timestamp, // 2010/2/16 changed
                    out strError);
                if (nRet == -1)
                    return -1;
                bookitem.OldRecord = strOldXml;
            }

            /*
            // seller
            string strSeller = DomUtil.GetElementText(dom.DocumentElement,
                "selller");
            if (String.IsNullOrEmpty(strSeller) == false)
                bookitem.Seller = strSeller;

            // source
            string strSource = DomUtil.GetElementText(dom.DocumentElement,
                "source");
            if (String.IsNullOrEmpty(strSeller) == false)
                bookitem.Seller = strSeller;
             * */

            if (this.Items == null)
                this.Items = new BookItemCollection();

            Debug.Assert(this.Items != null, "");

            if (exist_item == null)
            {
                this.Items.Add(bookitem);
                bookitem.Parent = Global.GetRecordID(this.BiblioRecPath);
                bookitem.ItemDisplayState = ItemDisplayState.New;
                bookitem.AddToListView(this.listView);
            }
            else
            {
                // 2010/5/5
                if (bookitem.ItemDisplayState != ItemDisplayState.New)
                    bookitem.ItemDisplayState = ItemDisplayState.Changed;
            }

            bookitem.Changed = true;    // ���򡰱��桱��ť����Enabled

            // ���ոռ�����������ɼ���Χ
            bookitem.HilightListViewItem(true);
            bookitem.RefreshListView(); // 2009/12/18 add

            this.EnableControls(true);

            if (String.IsNullOrEmpty(strWarning) == false)
            {
                bookitem.Error = new EntityInfo();
                bookitem.Error.ErrorInfo = strWarning;
                bookitem.RefreshListView();

                strError = strWarning;
                return 1;   // �������ظ�
            }

            return 0;
        }

        // 
        // return:
        //      -1  ����
        //      0   �����ظ���û�м���
        //      1   �Ѽ���
        /// <summary>
        /// ��������һ��ʵ�壬���򿪶Ի���
        /// </summary>
        /// <param name="strBarcode">�������</param>
        /// <returns>-1: ����; 0 �����ظ���û�м���; 1: �Ѽ���</returns>
        public int DoQuickNewEntity(string strBarcode)
        {
            int nRet = 0;
            string strError = "";

            if (String.IsNullOrEmpty(this.BiblioRecPath) == true)
            {
                strError = "��δ������Ŀ��¼";
                goto ERROR1;
            }

            if (String.IsNullOrEmpty(strBarcode) == false)  // 2008/11/3
            {

                // �Ե�ǰ�����ڽ����������
                BookItem dupitem = this.Items.GetItemByBarcode(strBarcode);
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "�������Ĳ���Ϣ�У�������� '" + strBarcode + "' �ͱ�����δ�ύ֮һɾ������������ء���ȷʵҪ�������������ύ����֮ɾ������";
                    else
                        strText = "�������Ĳ���Ϣ�У�������� '" + strBarcode + "' �ڱ������Ѿ����ڡ�";

                    dupitem.HilightListViewItem(true);

                    MessageBox.Show(ForegroundWindow.Instance, strText);
                    return 0;
                }

                // ������ʵ���¼�����������
                if (true)
                {
                    string strItemText = "";
                    string strBiblioText = "";
                    // string strError = "";
                    nRet = SearchEntityBarcode(strBarcode,
                        out strItemText,
                        out strBiblioText,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "�Բ������ '" + strBarcode + "' ���в��صĹ����з�������: " + strError;
                        goto ERROR1;
                    }
                    else if (nRet == 1) // �����ظ�
                    {
                        EntityBarcodeFoundDupDlg dlg = new EntityBarcodeFoundDupDlg();
                        MainForm.SetControlFont(dlg, this.Font, false);
                        dlg.MainForm = this.MainForm;
                        dlg.BiblioText = strBiblioText;
                        dlg.ItemText = strItemText;
                        dlg.MessageText = "�������Ĳ���Ϣ�У����� '" + strBarcode + "' �����ݿ��з����Ѿ����ڡ�";
                        dlg.ShowDialog(this);
                        return 0;
                    }
                }
            }

            BookItem bookitem = new BookItem();

            // ����ȱʡֵ
            nRet = SetItemDefaultValues(
                "quickRegister_default",
                true,
                bookitem,
                out strError);
            if (nRet == -1)
            {
                strError = "����ȱʡֵ��ʱ��������: " + strError;
                goto ERROR1;
            }

            bookitem.Barcode = strBarcode;

            if (this.Items == null)
                this.Items = new BookItemCollection();

            Debug.Assert(this.Items != null, "");

            this.Items.Add(bookitem);
            bookitem.Parent = Global.GetRecordID(this.BiblioRecPath);
            bookitem.ItemDisplayState = ItemDisplayState.New;
            /* ListViewItem newitem = */
            bookitem.AddToListView(this.listView);
            bookitem.Changed = true;    // ���򡰱��桱��ť����Enabled

            // ���ոռ�����������ɼ���Χ
            //this.listView_items.EnsureVisible(this.listView_items.Items.IndexOf(newitem));
            bookitem.HilightListViewItem(true);

            this.EnableControls(true);

            return 1;
        ERROR1:
            MessageBox.Show(ForegroundWindow.Instance, strError);
            return -1;
        }



        // ����ɾ��һ������ʵ��
        void menu_undoDeleteEntity_Click(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "��δѡ��Ҫ����ɾ��������");
                return;
            }

            this.EnableControls(false);

            try
            {

                /*
                string strBarcodeList = "";
                for (int i = 0; i < this.listView_items.SelectedItems.Count; i++)
                {
                    if (i > 20)
                    {
                        strBarcodeList += "...(�� " + this.listView_items.SelectedItems.Count.ToString() + " ��)";
                        break;
                    }
                    string strBarcode = this.listView_items.SelectedItems[i].Text;
                    strBarcodeList += strBarcode + "\r\n";
                }

                string strWarningText = "����(����)�Ὣ������ɾ��: \r\n" + strBarcodeList + "\r\n\r\nȷʵҪ����ɾ������?";

                // ����
                DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
                    strWarningText,
                    "EntityForm",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Cancel)
                    return;
                 * */

                // ʵ��ɾ��
                List<ListViewItem> selectedItems = new List<ListViewItem>();
                foreach (ListViewItem item in this.listView.SelectedItems)
                {
                    selectedItems.Add(item);
                }

                string strNotUndoList = "";
                int nUndoCount = 0;
                foreach (ListViewItem item in selectedItems)
                {
                    BookItem bookitem = (BookItem)item.Tag;

                    bool bRet = this.Items.UndoMaskDeleteItem(bookitem);

                    if (bRet == false)
                    {
                        if (strNotUndoList != "")
                            strNotUndoList += ",";
                        strNotUndoList += bookitem.Barcode;
                        continue;
                    }

                    nUndoCount++;
                }

                string strText = "";

                if (strNotUndoList != "")
                    strText += "����Ϊ '" + strNotUndoList + "' ��������ǰ��δ�����ɾ����, ��������̸���ϳ���ɾ����\r\n\r\n";

                strText += "������ɾ�� " + nUndoCount.ToString() + " �";
                MessageBox.Show(ForegroundWindow.Instance, strText);

            }
            finally
            {
                this.EnableControls(true);
            }
        }

        // ɾ��һ������ʵ��
        void menu_deleteEntity_Click(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "��δѡ��Ҫ���ɾ��������");
                return;
            }

            string strBarcodeList = "";
            for (int i = 0; i < this.listView.SelectedItems.Count; i++)
            {
                ListViewItem item = this.listView.SelectedItems[i];
                if (i > 20)
                {
                    strBarcodeList += "...(�� " + this.listView.SelectedItems.Count.ToString() + " ��)";
                    break;
                }
                BookItem bookitem = (BookItem)item.Tag;

                string strBarcode = bookitem.Barcode;
                if (String.IsNullOrEmpty(strBarcode) == true)
                    strBarcode = bookitem.RecPath;
                if (String.IsNullOrEmpty(strBarcode) == true)
                    strBarcode = bookitem.RefID;

                strBarcodeList += strBarcode + "\r\n";
            }

            string strWarningText = "���²Ὣ�����ɾ��: \r\n" + strBarcodeList + "\r\n\r\nȷʵҪ���ɾ������?";

            // ����
            DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
                strWarningText,
                "EntityForm",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result == DialogResult.Cancel)
                return;

            List<string> deleted_recpaths = new List<string>();

            this.EnableControls(false);

            try
            {
                // ʵ��ɾ��
                List<ListViewItem> selectedItems = new List<ListViewItem>();
                foreach (ListViewItem item in this.listView.SelectedItems)
                {
                    selectedItems.Add(item);
                }

                string strNotDeleteList = "";
                int nDeleteCount = 0;
                foreach (ListViewItem item in selectedItems)
                {
                    BookItem bookitem = (BookItem)item.Tag;

                    int nRet = MaskDeleteItem(bookitem,
                        m_bRemoveDeletedItem);

                    if (nRet == 0)
                    {
                        if (strNotDeleteList != "")
                            strNotDeleteList += ",";
                        strNotDeleteList += bookitem.Barcode;
                        continue;
                    }

                    if (string.IsNullOrEmpty(bookitem.RecPath) == false)
                        deleted_recpaths.Add(bookitem.RecPath);

                    /*
                    if (String.IsNullOrEmpty(bookitem.Borrower) == false)
                    {
                        if (strNotDeleteList != "")
                            strNotDeleteList += ",";
                        strNotDeleteList += bookitem.Barcode;
                        continue;
                    }

                    this.bookitems.MaskDeleteItem(m_bRemoveDeletedItem,
                        bookitem);
                     * */
                    nDeleteCount++;
                }

                string strText = "";

                if (strNotDeleteList != "")
                    strText += "����Ϊ '" + strNotDeleteList + "' �Ĳ��������ͨ��Ϣ, δ�ܼ��Ա��ɾ����\r\n\r\n";

                if (deleted_recpaths.Count == 0)
                    strText += "��ֱ��ɾ�� " + nDeleteCount.ToString() + " �";
                else if (nDeleteCount - deleted_recpaths.Count == 0)
                    strText += "�����ɾ�� "
                        +deleted_recpaths.Count.ToString()
                        + " �\r\n\r\n(ע�������ɾ�������Ҫ�����ύ����Ż������ӷ�����ɾ��)";
                else
                    strText += "�����ɾ�� "
    + deleted_recpaths.Count.ToString()
    + " �ֱ��ɾ�� "
    + (nDeleteCount - deleted_recpaths.Count).ToString()
    + " �\r\n\r\n(ע�������ɾ�������Ҫ�����ύ����Ż������ӷ�����ɾ��)";

                MessageBox.Show(ForegroundWindow.Instance, strText);
            }
            finally
            {
                this.EnableControls(true);
            }
        }



        // ����������š�����������Ų��ء�
        int SearchEntityBarcode(string strBarcode,
            out string strItemText,
            out string strBiblioText,
            out string strError)
        {
            strError = "";
            strItemText = "";
            strBiblioText = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڶԲ������ '" + strBarcode + "' ���в��� ...");
            Stop.BeginLoop();

            try
            {
                long lRet = Channel.GetItemInfo(
                    Stop,
                    strBarcode,
                    "html",
                    out strItemText,
                    "html",
                    out strBiblioText,
                    out strError);
                if (lRet == -1)
                    return -1;  // error

                if (lRet == 0)
                    return 0;   // not found
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");
            }

            return 1;   // found
        }

#if NO
        // ������Ų��ء�����(������)������Ų��ء�
        // �����������Զ��ų��͵�ǰ·��strOriginRecPath�ظ�֮����
        // parameters:
        //      strBarcode  ������š�
        //      strOriginRecPath    ������¼��·����
        //      paths   �������е�·��
        // return:
        //      -1  error
        //      0   not dup
        //      1   dup
        int SearchEntityBarcodeDup(string strBarcode,
            string strOriginRecPath,
            out string[] paths,
            out string strError)
        {
            strError = "";
            paths = null;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڶԲ������ '" + strBarcode + "' ���в��� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.SearchItemDup(
                    stop,
                    strBarcode,
                    100,
                    out paths,
                    out strError);
                if (lRet == -1)
                    return -1;  // error

                if (lRet == 0)
                    return 0;   // not found

                if (lRet == 1)
                {
                    // ��������һ��������·���Ƿ�ͳ�����¼һ��
                    if (paths.Length != 1)
                    {
                        strError = "ϵͳ����: SearchItemDup() API����ֵΪ1������paths����ĳߴ�ȴ����1, ���� " + paths.Length.ToString();
                        return -1;
                    }

                    if (paths[0] != strOriginRecPath)
                        return 1;   // �����ظ�����

                    return 0;   // ���ظ�
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 1;   // found
        }
#endif
        int SearchEntityBarcodeDup(string strBarcode,
    string strOriginRecPath,
    out string[] paths,
    out string strError)
        {
            strError = "";
            paths = null;

            if (string.IsNullOrEmpty(strBarcode) == true)
            {
                strError = "��Ӧ�ò������Ϊ��������";
                return -1;
            }

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڶԲ������ '" + strBarcode + "' ���в��� ...");
            Stop.BeginLoop();

            try
            {
                long lRet = Channel.SearchItem(
    Stop,
    "<ȫ��>",
    strBarcode,
    100,
    "�������",
    "exact",
    "zh",
    "dup",
    "", // strSearchStyle
    "", // strOutputStyle
    out strError);
                if (lRet == -1)
                    return -1;  // error

                if (lRet == 0)
                    return 0;   // not found

                long lHitCount = lRet;

                List<string> aPath = null;
                lRet = Channel.GetSearchResult(Stop,
                    "dup",
                    0,
                    Math.Min(lHitCount, 100),
                    "zh",
                    out aPath,
                    out strError);
                if (lRet == -1)
                    return -1;

                paths = new string[aPath.Count];
                aPath.CopyTo(paths);

                if (lHitCount == 1)
                {
                    // ��������һ��������·���Ƿ�ͳ�����¼һ��
                    if (paths.Length != 1)
                    {
                        strError = "ϵͳ����: SearchItem() API����ֵΪ1������paths����ĳߴ�ȴ����1, ���� " + paths.Length.ToString();
                        return -1;
                    }

                    if (paths[0] != strOriginRecPath)
                        return 1;   // �����ظ�����

                    return 0;   // ���ظ�
                }
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");
            }

            return 1;   // found
        }


        // 
        /// <summary>
        /// ���ݲ�����ż�������
        /// </summary>
        /// <param name="strBarcode">�������</param>
        /// <param name="bClearOtherSelection">�Ƿ�������������ѡ��״̬</param>
        public void HilightLine(string strBarcode,
                bool bClearOtherSelection)
        {
            if (bClearOtherSelection == true)
            {
                this.listView.SelectedItems.Clear();
            }

            if (this.Items != null)
            {
                BookItem bookitem = this.Items.GetItemByBarcode(strBarcode);
                if (bookitem != null)
                    bookitem.HilightListViewItem(true);
            }
        }

        // ���ɾ������
        // return:
        //      0   ��Ϊ����ͨ��Ϣ��δ�ܱ��ɾ��
        //      1   �ɹ�ɾ��
        /// <summary>
        /// ���ɾ������
        /// </summary>
        /// <param name="bookitem">����</param>
        /// <param name="bRemoveDeletedItem">�Ƿ�� ListView ������������ʾ</param>
        /// <returns>0: ��Ϊ����ͨ��Ϣ��δ�ܱ��ɾ��; 1: �ɹ�ɾ��</returns>
        public override int MaskDeleteItem(BookItem bookitem,
            bool bRemoveDeletedItem = false)
        {
            if (String.IsNullOrEmpty(bookitem.Borrower) == false)
                return 0;

            this.Items.MaskDeleteItem(bRemoveDeletedItem,
                bookitem);
            return 1;
        }



#if NOOOOOOOOOOOOOOOO
        // ��this.bookitems�ж�λ��dom����������
        // ˳�θ��� ��¼·�� -- ���� -- ��¼�� ����λ
        int LocateBookItem(
            string strRecPath,
            XmlDocument dom,
            out BookItem bookitem,
            out string strBarcode,
            out string strRegisterNo,
            out string strError)
        {
            strError = "";
            bookitem = null;
            strBarcode = "";
            strRegisterNo = "";

            // ��ǰ��ȡ, �Ա��κη���·��ʱ, �����Եõ���Щֵ
            strBarcode = DomUtil.GetElementText(dom.DocumentElement, "barcode");
            strRegisterNo = DomUtil.GetElementText(dom.DocumentElement, "registerNo");

            if (String.IsNullOrEmpty(strRecPath) == false)
            {
                bookitem = this.bookitems.GetItemByRecPath(strRecPath);

                if (bookitem != null)
                    return 1;   // found

            }

            if (String.IsNullOrEmpty(strBarcode) == false)
            {
                bookitem = this.bookitems.GetItemByBarcode(strBarcode);

                if (bookitem != null)
                    return 1;   // found

            }

            if (String.IsNullOrEmpty(strRegisterNo) == false)
            {
                bookitem = this.bookitems.GetItemByRegisterNo(strRegisterNo);

                if (bookitem != null)
                    return 1;   // found
            }

            return 0;
        }
#endif

#if NO
        // ��������ƺ�
        static string GetLocationSummary(string strBarcode,
            string strRegisterNo,
            string strRecPath,
            string strRefID)
        {
            if (String.IsNullOrEmpty(strBarcode) == false)
                return "����Ϊ '" + strBarcode + "' ������";
            if (String.IsNullOrEmpty(strRegisterNo) == false)
                return "��¼��Ϊ '" + strRegisterNo + "' ������";
            if (String.IsNullOrEmpty(strRecPath) == false)
                return "��¼·��Ϊ '" + strRecPath + "' ������";

            // 2008/6/24 
            if (String.IsNullOrEmpty(strRefID) == false)
                return "�ο�IDΪ '" + strRefID + "' ������";

            return "���κζ�λ��Ϣ������";
        }
#endif


        // ��������ƺ�
        internal override string GetLocationSummary(BookItem bookitem)
        {
            string strBarcode = bookitem.Barcode;

            if (String.IsNullOrEmpty(strBarcode) == false)
                return "����Ϊ '" + strBarcode + "' ������";

            string strRegisterNo = bookitem.RegisterNo;

            if (String.IsNullOrEmpty(strRegisterNo) == false)
                return "��¼��Ϊ '" + strRegisterNo + "' ������";

            string strRecPath = bookitem.RecPath;

            if (String.IsNullOrEmpty(strRecPath) == false)
                return "��¼·��Ϊ '" + strRecPath + "' ������";

            string strRefID = bookitem.RefID;
            // 2008/6/24 
            if (String.IsNullOrEmpty(strRefID) == false)
                return "�ο�IDΪ '" + strRefID + "' ������";

            return "���κζ�λ��Ϣ������";
        }

        /// <summary>
        /// ���ݲ������ ������ ��Ŀ��¼ ��ȫ�������ᣬװ�봰��
        /// </summary>
        /// <param name="strBarcode">�������</param>
        /// <returns>-1: ����; 0: û���ҵ�; 1: �ɹ�</returns>
        public int DoSearchEntity(string strBarcode)
        {
            BookItem result_item = null;
            return this.DoSearchItem("",
                strBarcode,
                out result_item,
                true);
        }

#if NO
        // 
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// ���ݲ������ ������ ��Ŀ��¼ ��ȫ�������ᣬװ�봰��
        /// </summary>
        /// <param name="strBarcode">�������</param>
        /// <returns>-1: ����; 0: û���ҵ�; 1: �ɹ�</returns>
        public int DoSearchEntity(string strBarcode)
        {
            int nRet = 0;
            string strError = "";
            // �ȼ���Ƿ����ڱ�������?

            // �Ե�ǰ�����ڽ����������
            if (this.Items != null)
            {
                BookItem dupitem = this.Items.GetItemByBarcode(strBarcode);
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "������� '" + strBarcode + "' ����Ϊ������δ�ύ֮һɾ��������";
                    else
                        strText = "������� '" + strBarcode + "' �ڱ������ҵ���";

                    dupitem.HilightListViewItem(true);

                    MessageBox.Show(ForegroundWindow.Instance, strText);
                    return 1;
                }
            }

            string strConfirmItemRecPath = "";
        // ��������ύ��������

            REDO:

            string strBiblioRecPath = "";
            string strItemRecPath = "";

            string strSearchText = "";

            if (String.IsNullOrEmpty(strConfirmItemRecPath) == true)
                strSearchText = strBarcode;
            else
                strSearchText = "@path:" + strConfirmItemRecPath;


            // ����������ţ����������������Ŀ��¼·����
            nRet = SearchTwoRecPathByBarcode(strSearchText,
                out strItemRecPath,
                out strBiblioRecPath,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(ForegroundWindow.Instance, "�Բ������ '" + strBarcode + "' ���м����Ĺ����з�������: " + strError);
                return -1;
            }
            else if (nRet == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "û���ҵ������������ '" + strBarcode + "' �ļ�¼��");
                return 0;
            }
            else if (nRet == 1)
            {
                Debug.Assert(strBiblioRecPath != "", "");
                this.TriggerLoadRecord(strBiblioRecPath);

                // ѡ����������
                HilightLine(strBarcode, true);
                return 1;
            }
            else if (nRet > 1) // ���з����ظ�
            {
                /*
                string strText = "���� '" + strBarcode + "' �����ݿ��з����Ѿ������ж������¼��ʹ�á�\r\n" + strItemRecPath + "\r\n\r\n����ϵϵͳ����Ա������������ݴ���";
                MessageBox.Show(ForegroundWindow.Instance, strText);
                return -1;
                 * */
                this.MainForm.PrepareSearch();

                try
                {
                    ItemBarcodeDupDlg dupdlg = new ItemBarcodeDupDlg();
                    // ��ʱEntityForm�����廹û�г�ʼ��
                    MainForm.SetControlFont(dupdlg, this.MainForm.DefaultFont, false);
                    string strErrorNew = "";
                    string[] aDupPath = strItemRecPath.Split(new char[] { ',' });
                    nRet = dupdlg.Initial(
                        this.MainForm,
                        aDupPath,
                        "���� '" + strBarcode + "' �����ݿ��з����Ѿ������ж������¼��ʹ�á����������Ҫ���������\r\n\r\n�ɸ��������г�����ϸ��Ϣ��ѡ���ʵ��Ĳ��¼�����Բ�����",
                        this.MainForm.Channel,
                        this.MainForm.Stop,
                        out strErrorNew);
                    if (nRet == -1)
                    {
                        // ��ʼ���Ի���ʧ��
                        MessageBox.Show(ForegroundWindow.Instance, strErrorNew);
                        goto ERROR1;
                    }

                    this.MainForm.AppInfo.LinkFormState(dupdlg, "ChargingForm_dupdlg_state");
                    dupdlg.ShowDialog(this);
                    this.MainForm.AppInfo.UnlinkFormState(dupdlg);

                    if (dupdlg.DialogResult == DialogResult.Cancel)
                    {
                        strError = "���� '" + strBarcode + "' �����ݿ��з����Ѿ������ж������¼��ʹ�á�\r\n" + strItemRecPath + "\r\n\r\n����ϵϵͳ����Ա������������ݴ���";
                        goto ERROR1;
                    }

                    strConfirmItemRecPath = dupdlg.SelectedRecPath;

                    goto REDO;
                }
                finally
                {
                    this.MainForm.EndSearch();
                }


            }

            return 0;
        ERROR1:
            return -1;
        }

#endif
#if NO
        // 2008/11/2
        // 
        // parameters:
        //      strItemBarcode  [out]���ز��¼�Ĳ������
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// ���ݲ��¼·�� ������ ��Ŀ��¼ ��ȫ�������ᣬװ�봰��
        /// </summary>
        /// <param name="strItemRecPath">���¼·��</param>
        /// <param name="strItemBarcode">���ز��¼�Ĳ������</param>
        /// <param name="bDisplayWarning">�Ƿ���ʾ������Ϣ</param>
        /// <returns>-1: ����; 0: û���ҵ�; 1: �ɹ�</returns>
        public int DoSearchEntityByRecPath(string strItemRecPath,
            out string strItemBarcode,
            bool bDisplayWarning = true)
        {
            strItemBarcode = "";

            int nRet = 0;
            string strError = "";
            // �ȼ���Ƿ����ڱ�������?
            // �Ե�ǰ�����ڽ��в��¼·������
            if (this.Items != null)
            {
                BookItem dupitem = this.Items.GetItemByRecPath(strItemRecPath) as BookItem;
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "���¼ '" + strItemRecPath + "' ����Ϊ������δ�ύ֮һɾ��������";
                    else
                        strText = "���¼ '" + strItemRecPath + "' �ڱ������ҵ���";

                    dupitem.HilightListViewItem(true);

                    if (bDisplayWarning == true)
                        MessageBox.Show(ForegroundWindow.Instance, strText);
                    return 1;
                }
            }

        // ��������ύ��������
            string strBiblioRecPath = "";
            string strOutputItemRecPath = "";

            string strSearchText = "";

            strSearchText = "@path:" + strItemRecPath;

            // ���ݲ��¼·�����������������������Ŀ��¼·����
            nRet = SearchTwoRecPathByBarcode(strSearchText,
                out strOutputItemRecPath,
                out strBiblioRecPath,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(ForegroundWindow.Instance, "�Բ��¼·�� '" + strItemRecPath + "' ���м����Ĺ����з�������: " + strError);
                return -1;
            }
            else if (nRet == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "û���ҵ�·��Ϊ '" + strItemRecPath + "' �Ĳ��¼��");
                return 0;
            }
            else if (nRet == 1)
            {
                Debug.Assert(strBiblioRecPath != "", "");
                this.TriggerLoadRecord(strBiblioRecPath);

                // ѡ����������
                BookItem result_item = HilightLineByItemRecPath(strItemRecPath, true);
                if (result_item != null)
                    strItemBarcode = result_item.Barcode;
                return 1;
            }
            else if (nRet > 1) // ���з����ظ�
            {
                Debug.Assert(false, "�ò��¼·���������Բ��ᷢ���ظ�����");
            }

            return 0;
            /*
        ERROR1:
            return -1;
             * */
        }
#endif

#if NO
        // 2010/2/26 
        // 
        // parameters:
        //      strItemBarcode  [out]���ز��¼�Ĳ������
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        /// <summary>
        /// ���ݲ��¼�ο�ID ������ ��Ŀ��¼ ��ȫ�������ᣬװ�봰��
        /// </summary>
        /// <param name="strItemRefID">���¼�Ĳο� ID</param>
        /// <param name="strItemBarcode">���ز��¼�Ĳ������</param>
        /// <returns>-1: ����; 0: û���ҵ�; 1: �ɹ�</returns>
        public int DoSearchEntityByRefID(string strItemRefID,
            out string strItemBarcode)
        {
            strItemBarcode = "";

            int nRet = 0;
            string strError = "";

            // �ȼ���Ƿ����ڱ�������?
            // �Ե�ǰ�����ڽ��в��¼�ο� ID ����
            if (this.Items != null)
            {
                BookItem dupitem = this.Items.GetItemByRefID(strItemRefID) as BookItem;
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "���¼ '" + strItemRefID + "' ����Ϊ������δ�ύ֮һɾ��������";
                    else
                        strText = "���¼ '" + strItemRefID + "' �ڱ������ҵ���";

                    dupitem.HilightListViewItem(true);

                    MessageBox.Show(ForegroundWindow.Instance, strText);
                    return 1;
                }
            }

            // ��������ύ��������
            string strBiblioRecPath = "";
            string strOutputItemRecPath = "";

            string strSearchText = "";

            strSearchText = "@refID:" + strItemRefID;

            // ���ݲ��¼·�����������������������Ŀ��¼·����
            nRet = SearchTwoRecPathByBarcode(strSearchText,
                out strOutputItemRecPath,
                out strBiblioRecPath,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(ForegroundWindow.Instance, "�Բ��¼�Ĳο�ID '" + strItemRefID + "' ���м����Ĺ����з�������: " + strError);
                return -1;
            }
            else if (nRet == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "û���ҵ�·��Ϊ '" + strItemRefID + "' �Ĳ��¼��");
                return 0;
            }
            else if (nRet == 1)
            {
                Debug.Assert(strBiblioRecPath != "", "");
                this.TriggerLoadRecord(strBiblioRecPath);

                // ѡ����������
                BookItem result_item = HilightLineByItemRefID(strItemRefID, true);
                if (result_item != null)
                    strItemBarcode = result_item.Barcode;
                return 1;
            }
            else if (nRet > 1) // ���з����ظ�
            {
                Debug.Assert(false, "�ò��¼�ο�ID����Ӧ�����ᷢ���ظ�����");
                MessageBox.Show(ForegroundWindow.Instance, "�òο�ID '"+strItemRefID+"' �������ж���һ����Ϊ "+nRet.ToString()+" ��");
                return -1;
            }

            return 0;
            /*
        ERROR1:
            return -1;
             * */
        }

#endif

        private void listView_items_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int nClickColumn = e.Column;
            this.SortColumns.SetFirstColumn(nClickColumn,
                this.listView.Columns);

            // ����
            this.listView.ListViewItemSorter = new SortColumnsComparer(this.SortColumns);

            this.listView.ListViewItemSorter = null;
        }

        // 
        // 2009/10/12 
        // parameters:
        //      strPublishTime  ����ʱ�䣬8�ַ���
        //                      ���Ϊ"*"����ʾͳ���������ʱ�����
        //                      ���Ϊ"<range>"����ʾƥ�䷶Χ�͵ĳ���ʱ���ַ���������"20090101-20091231"
        //                      ���Ϊ"<single>"����ʾƥ�䵥���͵ĳ���ʱ���ַ���������"20090115"
        //                      ���Ϊ"refids:"�������ַ�������ʾҪ����refid�б��ȡ���ɼ�¼
        /// <summary>
        /// ���ݳ���ʱ�䣬ƥ�䡰ʱ�䷶Χ�����ϵĲ��¼
        /// </summary>
        /// <param name="strPublishTime">����ʱ�䣬8�ַ���
        /// <para>���Ϊ"*"����ʾͳ���������ʱ�����</para>
        /// <para>���Ϊ"&lt;range&gt;"����ʾƥ�䷶Χ�͵ĳ���ʱ���ַ���������"20090101-20091231"</para>
        /// <para>���Ϊ"&lt;single&gt;"����ʾƥ�䵥���͵ĳ���ʱ���ַ���������"20090115"</para>
        /// <para>���Ϊ"refids:"�������ַ�������ʾҪ����refid�б��ȡ���ɼ�¼</para>
        /// </param>
        /// <param name="XmlRecords">���� XML �ַ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int GetItemInfoByPublishTime(string strPublishTime,
            out List<string> XmlRecords,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            XmlRecords = new List<string>();

            if (this.Items == null)
                return 0;

            if (StringUtil.HasHead(strPublishTime, "refids:") == true)
            {
                string strList = strPublishTime.Substring("refids:".Length);
                string[] parts = strList.Split(new char[] {','});
                for (int i = 0; i < parts.Length; i++)
                {
                    string strRefID = parts[i];
                    if (String.IsNullOrEmpty(strRefID) == true)
                        continue;

                    BookItem item = this.Items.GetItemByRefID(strRefID) as BookItem;
                    if (item == null)
                    {
                        XmlRecords.Add(null);   // ��ʾû���ҵ�������Ҳռ��һ��λ��
                        continue;
                    }

                    string strItemXml = "";
                    nRet = item.BuildRecord(
                        true,   // Ҫ��� Parent ��Ա
                        out strItemXml,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    XmlRecords.Add(strItemXml);
                }
                return 0;
            }

            foreach (BookItem item in this.Items)
            {
                // BookItem item = this.BookItems[i];

                if (item.ItemDisplayState == ItemDisplayState.Deleted)
                {
                    continue;
                }

                // �Ǻű�ʾͨ��
                if (strPublishTime == "*")
                {
                }
                else if (strPublishTime == "<single>")
                {
                    nRet = item.PublishTime.IndexOf("-");
                    if (nRet != -1)
                        continue;
                }
                else if (strPublishTime == "<range>")
                {
                    nRet = item.PublishTime.IndexOf("-");
                    if (nRet == -1)
                        continue;
                }
                else 
                {
                    /*
                    try
                    {
                        if (Global.InRange(strPublishTime, item.Range) == false)
                            continue;
                    }
                    catch (Exception ex)
                    {
                        strError = ex.Message;
                        return -1;
                    }
                     * */
                    // TODO: jianglai keneng chuxian fanwei zai item.PublishTime
                    // TODO: shi fou yao paichu hedingben?
                    if (strPublishTime != item.PublishTime)
                        continue;
                }

                string strItemXml = "";
                nRet = item.BuildRecord(
                    true,   // Ҫ��� Parent ��Ա
                    out strItemXml,
                    out strError);
                if (nRet == -1)
                    return -1;

                XmlRecords.Add(strItemXml);
            }

            return 1;
        }

#if NO
        // ΪBookItem��������ȱʡֵ
        // parameters:
        //      strCfgEntry Ϊ"normalRegister_default"��"quickRegister_default"
        int SetBookItemDefaultValues(
            string strCfgEntry,
            BookItem bookitem,
            out string strError)
        {
            strError = "";

            string strNewDefault = this.MainForm.AppInfo.GetString(
    "entityform_optiondlg",
    strCfgEntry,
    "<root />");

            // �ַ���strNewDefault������һ��XML��¼�������൱��һ����¼��ԭò��
            // ���ǲ����ֶε�ֵ����Ϊ"@"��������ʾ����һ�������
            // ��Ҫ����Щ����ֺ�����ʽ���ؼ�
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strNewDefault);
            }
            catch (Exception ex)
            {
                strError = "XML��¼װ��DOMʱ����: " + ex.Message;
                return -1;
            }

            // ��������һ��Ԫ�ص�����
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("*");
            for (int i = 0; i < nodes.Count; i++)
            {
                string strText = nodes[i].InnerText;
                if (strText.Length > 0 && strText[0] == '@')
                {
                    // ���ֺ�
                    nodes[i].InnerText = DoGetMacroValue(strText);
                }
            }

            strNewDefault = dom.OuterXml;

            int nRet = bookitem.SetData("",
                strNewDefault,
                null,
                out strError);
            if (nRet == -1)
                return -1;

            bookitem.Parent = "";
            bookitem.RecPath = "";

            return 0;
        }
#endif

#if NO
        // ����ʹ�� SearchBiblioRecPath()
        // ���ݲ�����ţ�����������¼·���ʹ�������Ŀ��¼·����
        int SearchTwoRecPathByBarcode(string strBarcode,
            out string strItemRecPath,
            out string strBiblioRecPath,
            out string strError)
        {
            strError = "";
            strBiblioRecPath = "";
            strItemRecPath = "";


            string strItemText = "";
            string strBiblioText = "";

            byte[] item_timestamp = null;

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڼ���������� '" + strBarcode + "' ����������Ŀ��¼·�� ...");
            Stop.BeginLoop();

            try
            {
                long lRet = Channel.GetItemInfo(
                    Stop,
                    strBarcode,
                    null,
                    out strItemText,
                    out strItemRecPath,
                    out item_timestamp,
                    "recpath",
                    out strBiblioText,
                    out strBiblioRecPath,
                    out strError);
                if (lRet == -1)
                    return -1;  // error

                return (int)lRet;   // not found
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");
            }
        }
#endif

        private void ListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control == true)
            {
                // Ctrl+A
                menu_generateData_Click(sender, null);
            }
        }

        // ѡ��(����)items�����з���ָ�����κŵ���Щ��
        /// <summary>
        /// ѡ��(����) Items ��ƥ��ָ�����κŵ���Щ����
        /// </summary>
        /// <param name="strBatchNo">���κ�</param>
        /// <param name="bClearOthersHilight">ͬʱ�����������ļ���״̬</param>
        public void SelectItemsByBatchNo(string strBatchNo,
            bool bClearOthersHilight)
        {
            this.Items.SelectItemsByBatchNo(strBatchNo,
                bClearOthersHilight);
        }

        /// <summary>
        /// ׷��һ���µ� �� ��¼
        /// Ҳ����ֱ��ʹ�� EntityControlBase.AppendItem()
        /// </summary>
        /// <param name="item">Ҫ׷�ӵ�����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int AppendEntity(BookItem item,
            out string strError)
        {
            return this.AppendItem(item, out strError);
        }
    }

    /// <summary>
    /// ��ø��ֲ���ֵ
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void GetParameterValueHandler(object sender,
        GetParameterValueEventArgs e);

    /// <summary>
    /// ��ø��ֲ���ֵ�¼� GetParameterValueHandler �Ĳ���
    /// </summary>
    public class GetParameterValueEventArgs : EventArgs
    {
        /// <summary>
        /// ������
        /// </summary>
        public string Name = "";
        /// <summary>
        /// ����ֵ
        /// </summary>
        public string Value = "";
    }

    /// <summary>
    /// У������
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void VerifyBarcodeHandler(object sender,
        VerifyBarcodeEventArgs e);

    /// <summary>
    /// У��������¼� VerifyBarcodeHandler �Ĳ���
    /// </summary>
    public class VerifyBarcodeEventArgs : EventArgs
    {
        /// <summary>
        /// �����
        /// </summary>
        public string Barcode = "";
        /// <summary>
        /// [out]������Ϣ��
        /// </summary>
        public string ErrorInfo = "";

                // return:
                //      -2  ������û������У�鷽�����޷�У��
                //      -1  error
        //      0   ���ǺϷ��������
        //      1   �ǺϷ��Ķ���֤�����
        //      2   �ǺϷ��Ĳ������
        /// <summary>
        /// ����ֵ
        /// <para>      -2  ������û������У�鷽�����޷�У��</para>
        /// <para>      -1  ����</para>
        /// <para>      0   ���ǺϷ��������</para>
        /// <para>      1   �ǺϷ��Ķ���֤�����</para>
        /// <para>      2   �ǺϷ��Ĳ������</para>
        /// </summary>
        public int Result = -2;
    }

    /// <summary>
    /// ���ڼ������ BookItem ����
    /// </summary>
    [Serializable()]
    public class ClipboardBookItemCollection : List<BookItem>
    {
#if NO
        ArrayList m_list = new ArrayList();

        /// <summary>
        /// ׷��һ������
        /// </summary>
        /// <param name="bookitem">BookItem ����</param>
        public void Add(BookItem bookitem)
        {
            this.m_list.Add(bookitem);
        }


        public BookItem this[int nIndex]
        {
            get
            {
                return (BookItem)m_list[nIndex];
            }
            set
            {
                m_list[nIndex] = value;
            }
        }

        public int Count
        {
            get
            {
                return this.m_list.Count;
            }
        }
#endif

        // 
        /// <summary>
        /// �ָ���Щ�������л��ĳ�Աֵ
        /// </summary>
        public void RestoreNonSerialized()
        {
            for (int i = 0; i < this.Count; i++)
            {
                this[i].RestoreNonSerialized();
            }
        }
    }


    // 
    /// <summary>
    /// �޸�ʵ��(��)������¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void ChangeItemEventHandler(object sender,
        ChangeItemEventArgs e);

    /// <summary>
    /// �޸Ĳ������¼� �Ĳ���
    /// </summary>
    public class ChangeItemEventArgs : EventArgs
    {
        // [in]
        /// <summary>
        /// [in] �Ƿ�Ϊ�ڿ�ģʽ
        /// </summary>
        public bool SeriesMode = false; // �Ƿ�Ϊ�ڿ�ģʽ
        // [in] 
        /// <summary>
        /// [in] ����Ĳ������
        /// </summary>
        public bool InputItemBarcode = true;
        // [in]
        /// <summary>
        /// [in] �Ƿ�ҪΪ�µĲᴴ����ȡ��
        /// </summary>
        public bool CreateCallNumber = false;   // Ϊ�µĲᴴ����ȡ��

        // [in]
        /// <summary>
        /// [in] �����б�
        /// </summary>
        public List<ChangeItemData> DataList = new List<ChangeItemData>();

        // [out]
        /// <summary>
        /// [out] ������Ϣ�����Ϊ�գ���ʾû�г���
        /// </summary>
        public string ErrorInfo = "";   // [out]���Ϊ�ǿգ���ʾִ�й��̳��������ǳ�����Ϣ
        // 2010/4/15
        // [out]
        /// <summary>
        /// [out] ������Ϣ�����Ϊ�գ���ʾû�о���
        /// </summary>
        public string WarningInfo = "";   // [out]���Ϊ�ǿգ���ʾִ�й��̳��־��棬�����Ǿ�����Ϣ
    }

    /// <summary>
    /// һ�����ݴ洢��Ԫ
    /// ���� ChangeItemEventArgs ��
    /// </summary>
    public class ChangeItemData
    {
        /// <summary>
        /// ������Ϊ new/delete/change/neworchange ֮һ
        /// </summary>
        public string Action = "";  // new/delete/change/neworchange
        /// <summary>
        /// �ο� ID
        /// </summary>
        public string RefID = "";   // �ο�ID��������Ϣ��ϵ��һ��Ψһ��IDֵ
        /// <summary>
        /// ���¼ XML 
        /// </summary>
        public string Xml = ""; // ʵ���¼XML
        /// <summary>
        /// [out] ������Ϣ�����Ϊ�գ���ʾû�г���
        /// </summary>
        public string ErrorInfo = "";   // [out]���Ϊ�ǿգ���ʾִ�й��̳��������ǳ�����Ϣ
        // 2010/4/15
        /// <summary>
        /// [out] ������Ϣ�����Ϊ�գ���ʾû�о���
        /// </summary>
        public string WarningInfo = "";   // [out]���Ϊ�ǿգ���ʾִ�й��̳��־��棬�����Ǿ�����Ϣ

        // 2010/12/1
        /// <summary>
        /// �������硰1/7��
        /// </summary>
        public string Sequence = "";    // �������硰1/7��
        /// <summary>
        /// ��ѡ�������۸񡣸�ʽΪ: "������:CNY12.00;���ռ�:CNY15.00"
        /// </summary>
        public string OtherPrices = ""; // ��ѡ�������۸񡣸�ʽΪ: "������:CNY12.00;���ռ�:CNY15.00"

    }

    // �����������д����ͼ���������ֹ���
    /// <summary>
    /// EntityControl ��Ļ�����
    /// </summary>
    public class EntityControlBase : ItemControlBase<BookItem, BookItemCollection>
    {
    }

}
