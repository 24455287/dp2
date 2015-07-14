using System;
using System.Collections.Generic;
using System.Collections;   // Hashtable
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.IO;

using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.Range;

namespace DigitalPlatform.CirculationClient
{
    /// <summary>
    /// ��������Դ����ؼ�
    /// </summary>
    public partial class BinaryResControl : UserControl
    {
        // Ctrl+A�Զ���������
        /// <summary>
        /// �Զ���������
        /// </summary>
        public event GenerateDataEventHandler GenerateData = null;

        /// <summary>
        /// �߿���
        /// </summary>
        [Category("Appearance")]
        [DescriptionAttribute("Border style of the control")]
        [DefaultValue(typeof(System.Windows.Forms.BorderStyle), "None")]
        public new BorderStyle BorderStyle
        {
            get
            {
                return base.BorderStyle;
            }
            set
            {
                base.BorderStyle = value;
            }
        }

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
                bool bOldValue = this.m_bChanged;

                this.m_bChanged = value;

                // �����¼�
                if (bOldValue != value && this.ContentChanged != null)
                {
                    ContentChangedEventArgs e = new ContentChangedEventArgs();
                    e.OldChanged = bOldValue;
                    e.CurrentChanged = value;
                    ContentChanged(this, e);
                }
            }
        }


        /// <summary>
        /// Ȩ��ֵ�����ļ�ȫ·��
        /// </summary>
        public string RightsCfgFileName
        {
            get;
            set;
        }

        public const string CaptionNormal = "������";
        public const string CaptionNew = "��δ����(��������)";
        public const string CaptionChanged = "��δ����(�޸Ĺ��Ķ���)";
        public const string CaptionDeleted = "���ɾ��";
        public const string CaptionError = "����";

        public const int COLUMN_ID = 0;
        public const int COLUMN_STATE = 1;
        public const int COLUMN_LOCALPATH = 2;
        public const int COLUMN_SIZE = 3;
        public const int COLUMN_MIME = 4;
        public const int COLUMN_TIMESTAMP = 5;
        public const int COLUMN_USAGE = 6;
        public const int COLUMN_RIGHTS = 7;

        /*
        public const int TYPE_UPLOADED = 0;
        public const int TYPE_NOT_UPLOAD = 1;
        public const int TYPE_ERROR = 2;
         * */

        /// <summary>
        /// ͨѶͨ��
        /// </summary>
        public LibraryChannel Channel = null;

        /// <summary>
        /// ֹͣ����
        /// </summary>
        public DigitalPlatform.Stop Stop = null;

        /// <summary>
        /// ���ݷ����ı�
        /// </summary>
        public event ContentChangedEventHandler ContentChanged = null;

        string m_strBiblioRecPath = "";

        public BinaryResControl()
        {
            InitializeComponent();
        }

        public string BiblioRecPath
        {
            get
            {
                return this.m_strBiblioRecPath;
            }
            set
            {
                this.m_strBiblioRecPath = value;
            }
        }

        public int ObjectCount
        {
            get
            {
                return this.ListView.Items.Count;
            }
        }

        public void Clear()
        {
            this.ListView.Items.Clear();
        }

        // return:
        //      -1  error
        //      0   û��װ��
        //      1   �Ѿ�װ��
        public int LoadObject(string strBiblioRecPath,
            string strXml,
            out string strError)
        {
            strError = "";

            this.ErrorInfo = "";

            // 2007/12/2 
            if (String.IsNullOrEmpty(strXml) == true)
            {
                this.Changed = false;
                return 0;
            }

            this.BiblioRecPath = strBiblioRecPath;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XML��¼װ�ص�DOMʱ����: " + ex.Message;
                return -1;
            }

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("dprms", DpNs.dprms);

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("//dprms:file", nsmgr);

            return LoadObject(nodes,
                out strError);
        }

        static Hashtable ParseMedaDataXml(string strXml,
            out string strError)
        {
            strError = "";
            Hashtable result = new Hashtable();

            if (strXml == "")
                return result;

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return null;
            }

            XmlAttributeCollection attrs = dom.DocumentElement.Attributes;
            for (int i = 0; i < attrs.Count; i++)
            {
                string strName = attrs[i].Name;
                string strValue = attrs[i].Value;

                result.Add(strName, strValue);
            }

            return result;
        }

        // ����б�����
        // return:
        //      -1  error
        //      0   û������κ����ݣ��б�Ϊ��
        //      1   �Ѿ����������
        public int LoadObject(XmlNodeList nodes,
            out string strError)
        {
            strError = "";

            bool bOldEnabled = this.Enabled;

            this.Enabled = bOldEnabled;
            try
            {
                this.ListView.Items.Clear();

                List<ListViewItem> items = new List<ListViewItem>();
                // ��һ�׶Σ������� XML ��¼�е� <file> Ԫ����Ϣ���롣
                // �����ͱ�֤�����ٿ����ڱ�����Ŀ��¼�׶��ܻ�ԭ XML ��¼�е���ز���
                foreach(XmlElement node in nodes)
                {
                    string strID = DomUtil.GetAttr(node, "id");
                    string strUsage = DomUtil.GetAttr(node, "usage");
                    string strRights = DomUtil.GetAttr(node, "rights");

                    ListViewItem item = new ListViewItem();

                    // state
                    SetLineInfo(item,
                        LineState.Normal);

                    // id
                    ListViewUtil.ChangeItemText(item, COLUMN_ID, strID);
                    // usage
                    ListViewUtil.ChangeItemText(item, COLUMN_USAGE, strUsage);
                    // rights
                    ListViewUtil.ChangeItemText(item, COLUMN_RIGHTS, strRights);
                    this.ListView.Items.Add(item);

                    items.Add(item);
                }

                // �ڶ��׶Σ��� dp2library ��������ȡ metadata ��Ϣ����������ֶ�����
                foreach(ListViewItem item in items)
                {
                    string strID = ListViewUtil.GetItemText(item, COLUMN_ID);

                    string strMetadataXml = "";
                    byte[] baMetadataTimestamp = null;
                    // ���һ��������Դ��Ԫ����
                    int nRet = GetOneObjectMetadata(
                        this.BiblioRecPath,
                        strID,
                        out strMetadataXml,
                        out baMetadataTimestamp,
                        out strError);
                    if (nRet == -1)
                    {
                        if (Channel.ErrorCode == localhost.ErrorCode.AccessDenied)
                        {
                            return -1;
                        }
                        // item.SubItems.Add(strError);
                        ListViewUtil.ChangeItemText(item, COLUMN_STATE, strError);
                        item.ImageIndex = 1;    // error!
                        continue;
                    }

                    // ȡmetadataֵ
                    Hashtable values = ParseMedaDataXml(strMetadataXml,
                        out strError);
                    if (values == null)
                    {
                        ListViewUtil.ChangeItemText(item, COLUMN_STATE, strError);
                        item.ImageIndex = 1;    // error!
                        continue;
                    }

                    // localpath
                    ListViewUtil.ChangeItemText(item, COLUMN_LOCALPATH, (string)values["localpath"]);

                    // size
                    ListViewUtil.ChangeItemText(item, COLUMN_SIZE, (string)values["size"]);

                    // mime
                    ListViewUtil.ChangeItemText(item, COLUMN_MIME, (string)values["mimetype"]);

                    // tiemstamp
                    string strTimestamp = ByteArray.GetHexTimeStampString(baMetadataTimestamp);
                    ListViewUtil.ChangeItemText(item, COLUMN_TIMESTAMP, strTimestamp);
                }

                this.Changed = false;

                if (this.ListView.Items.Count > 0)
                    return 1;

                return 0;
            }
            finally
            {
                this.Enabled = bOldEnabled;
            }
        }

#if NO
        // return:
        //      -1  error
        //      0   û��װ��
        //      1   �Ѿ�װ��
        public int LoadObject(XmlNodeList nodes,
            out string strError)
        {
            strError = "";

            bool bOldEnabled = this.Enabled;

            this.Enabled = bOldEnabled;
            try
            {
                this.ListView.Items.Clear();

                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    string strID = DomUtil.GetAttr(node, "id");
                    string strUsage = DomUtil.GetAttr(node, "usage");
                    string strRights = DomUtil.GetAttr(node, "rights");

                    ListViewItem item = new ListViewItem();
                    // item.Text = strID;
                    ListViewUtil.ChangeItemText(item, COLUMN_ID, strID);

                    this.ListView.Items.Add(item);

                    string strMetadataXml = "";
                    byte[] baMetadataTimestamp = null;
                    // ���һ��������Դ��Ԫ����
                    int nRet = GetOneObjectMetadata(
                        this.BiblioRecPath,
                        strID,
                        out strMetadataXml,
                        out baMetadataTimestamp,
                        out strError);
                    if (nRet == -1)
                    {
                        if (Channel.ErrorCode == localhost.ErrorCode.AccessDenied)
                        {
                            return -1;
                        }
                        // item.SubItems.Add(strError);
                        ListViewUtil.ChangeItemText(item, COLUMN_STATE, strError);
                        item.ImageIndex = 1;    // error!
                        continue;
                    }

                    // ȡmetadataֵ
                    Hashtable values = ParseMedaDataXml(strMetadataXml,
                        out strError);
                    if (values == null)
                    {
                        // item.SubItems.Add(strError);
                        ListViewUtil.ChangeItemText(item, COLUMN_STATE, strError);
                        item.ImageIndex = 1;    // error!
                        continue;
                    }

                    // state
                    SetLineInfo(item,
                        // strUsage,
                        LineState.Normal);

                    // localpath
                    // item.SubItems.Add((string)values["localpath"]);
                    ListViewUtil.ChangeItemText(item, COLUMN_LOCALPATH, (string)values["localpath"]);

                    // size
                    // item.SubItems.Add((string)values["size"]);
                    ListViewUtil.ChangeItemText(item, COLUMN_SIZE, (string)values["size"]);

                    // mime
                    // item.SubItems.Add((string)values["mimetype"]);
                    ListViewUtil.ChangeItemText(item, COLUMN_MIME, (string)values["mimetype"]);

                    // tiemstamp
                    string strTimestamp = ByteArray.GetHexTimeStampString(baMetadataTimestamp);
                    // item.SubItems.Add(strTimestamp);
                    ListViewUtil.ChangeItemText(item, COLUMN_TIMESTAMP, strTimestamp);

                    // usage
                    ListViewUtil.ChangeItemText(item, COLUMN_USAGE, strUsage);

                    // rights
                    ListViewUtil.ChangeItemText(item, COLUMN_RIGHTS, strRights);
                }

                this.Changed = false;

                if (this.ListView.Items.Count > 0)
                    return 1;

                return 0;
            }
            finally
            {
                this.Enabled = bOldEnabled;
            }
        }
#endif

        static LineState GetLineState(ListViewItem item)
        {
            LineInfo info = (LineInfo)item.Tag;
            if (info == null)
            {
                return LineState.Error;
                // throw new Exception("�յ�Tag");
            }

            return info.LineState;
        }

        LineState GetOldLineState(ListViewItem item)
        {
            LineInfo info = (LineInfo)item.Tag;
            if (info == null)
            {
                return LineState.Error;
                // throw new Exception("�յ�Tag");
            }

            return info.OldLineState;
        }

        void SetOldLineState(ListViewItem item,
            LineState old_state)
        {
            LineInfo info = (LineInfo)item.Tag;
            if (info == null)
            {
                info = new LineInfo();
                item.Tag = info;
            }

            info.OldLineState = old_state;
        }

        void SetResChanged(ListViewItem item,
    bool bChanged)
        {
            LineInfo info = (LineInfo)item.Tag;
            if (info == null)
            {
                info = new LineInfo();
                item.Tag = info;
            }

            info.ResChanged = bChanged;
        }

        void SetXmlChanged(ListViewItem item,
bool bChanged)
        {
            LineInfo info = (LineInfo)item.Tag;
            if (info == null)
            {
                info = new LineInfo();
                item.Tag = info;
            }

            info.XmlChanged = bChanged;
        }

        /*
        void SetLineState(ListViewItem item,
            LineState state)
        {
            if (state == LineState.Normal)
            {
                ListViewUtil.ChangeItemText(item, 1, CaptionNormal);
                item.ForeColor = Color.Black;
                item.BackColor = Color.White;
            }
            else if (state == LineState.New)
            {
                ListViewUtil.ChangeItemText(item, 1, CaptionNew);
                item.ForeColor = Color.Black;
                item.BackColor = Color.LightGreen;
            }
            else if (state == LineState.Changed)
            {
                ListViewUtil.ChangeItemText(item, 1, CaptionChanged);
                item.ForeColor = Color.Black;
                item.BackColor = Color.Yellow;
            }
            else if (state == LineState.Deleted)
            {
                ListViewUtil.ChangeItemText(item, 1, CaptionDeleted);
                item.ForeColor = Color.DarkGray;
                item.BackColor = Color.White;
            }
            else if (state == LineState.Error)
            {
                ListViewUtil.ChangeItemText(item, 1, CaptionError);
                item.ForeColor = Color.Red;
                item.BackColor = Color.White;
            }

            LineInfo info = (LineInfo)item.Tag;
            if (info == null)
            {
                info = new LineInfo();
                item.Tag = info;
            }

            info.LineState = state;
        }
         * */

        // ���� item �� Tag�������� item ��ǰ��������ɫ
        // parameters:
        //      strInitialUsage ���Ϊnull�������ô���
        void SetLineInfo(ListViewItem item,
            // string strInitialUsage,
            LineState state)
        {
            if (state == LineState.Normal)
            {
                ListViewUtil.ChangeItemText(item, 1, CaptionNormal);
                item.ForeColor = Color.Black;
                item.BackColor = Color.White;
            }
            else if (state == LineState.New)
            {
                ListViewUtil.ChangeItemText(item, 1, CaptionNew);
                item.ForeColor = Color.Black;
                item.BackColor = Color.LightGreen;
            }
            else if (state == LineState.Changed)
            {
                ListViewUtil.ChangeItemText(item, 1, CaptionChanged);
                item.ForeColor = Color.Black;
                item.BackColor = Color.Yellow;
            }
            else if (state == LineState.Deleted)
            {
                ListViewUtil.ChangeItemText(item, 1, CaptionDeleted);
                item.ForeColor = Color.DarkGray;
                item.BackColor = Color.White;
            }
            else if (state == LineState.Error)
            {
                ListViewUtil.ChangeItemText(item, 1, CaptionError);
                item.ForeColor = Color.Red;
                item.BackColor = Color.White;
            }

            LineInfo info = (LineInfo)item.Tag;
            if (info == null)
            {
                info = new LineInfo();
                item.Tag = info;
            }

#if NO
            if (strInitialUsage != null)
                info.InitialUsage = strInitialUsage;
#endif
            info.LineState = state;
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }

        // ���һ��������Դ��Ԫ����
        int GetOneObjectMetadata(
            string strBiblioRecPath,
            string strID,
            out string strMetadataXml,
            out byte[] timestamp,
            out string strError)
        {
            timestamp = null;
            strError = "";

            string strResPath = strBiblioRecPath + "/object/" + strID;

            strResPath = strResPath.Replace(":", "/");

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("�������ض����Ԫ���� " + strResPath);
            Stop.BeginLoop();

            try
            {
                string strOutputPath = "";

                // EnableControlsInLoading(true);
                string strResult = "";
                // ֻ�õ�metadata
                long lRet = this.Channel.GetRes(
                    Stop,
                    strResPath,
                    "metadata,timestamp,outputpath",
                    out strResult,
                    out strMetadataXml,
                    out timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    strError = "���ض��� " + strResPath + " Ԫ����ʧ�ܣ�ԭ��: " + strError;
                    return -1;
                }

                return 0;
            }
            finally
            {
                // EnableControlsInLoading(false);
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");
            }
        }

        private void ListView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            bool bHasBillioLoaded = false;

            if (String.IsNullOrEmpty(this.BiblioRecPath) == false)
                bHasBillioLoaded = true;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("�޸�(&M)");
            menuItem.Click += new System.EventHandler(this.menu_modify_Click);
            if (this.ListView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("����(&N)");
            menuItem.Click += new System.EventHandler(this.menu_new_Click);
            if (bHasBillioLoaded == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            //
            menuItem = new MenuItem("���ɾ��(&D)");
            menuItem.Click += new System.EventHandler(this.menu_delete_Click);
            if (this.ListView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("�������ɾ��(&U)");
            menuItem.Click += new System.EventHandler(this.menu_undoDelete_Click);
            if (this.ListView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("����(&E)");
            menuItem.Click += new System.EventHandler(this.menu_export_Click);
            if (this.ListView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // ��������
            menuItem = new MenuItem("��������[Ctrl+A](&G)");
            menuItem.Click += new System.EventHandler(this.menu_generateData_Click);
            /*
            if (this.ListView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
             * */
            contextMenu.MenuItems.Add(menuItem);

            // ����ά��856�ֶ�
            menuItem = new MenuItem("����ά��856�ֶ�(&C)");
            menuItem.Click += new System.EventHandler(this.menu_manage856_Click);
            /*
            if (this.ListView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
             * */
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.ListView, new Point(e.X, e.Y));	
        }

        // ��������
        void menu_generateData_Click(object sender, EventArgs e)
        {
            if (this.GenerateData != null)
            {
                GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                e1.FocusedControl = this.ListView;
                e1.ScriptEntry = "";    // ����Ctrl+A�˵�
                this.GenerateData(this, e1);
            }
            else
            {
                MessageBox.Show(this, "EntityControlû�йҽ�GenerateData�¼�");
            }
        }

        // ������ȡ��
        void menu_manage856_Click(object sender, EventArgs e)
        {
            if (this.GenerateData != null)
            {
                GenerateDataEventArgs e1 = new GenerateDataEventArgs();
                e1.FocusedControl = this.ListView;
                e1.ScriptEntry = "Manage856";    // ֱ������Manage856()����
                this.GenerateData(this, e1);
            }
            else
            {
                MessageBox.Show(this, "EntityControlû�йҽ�GenerateData�¼�");
            }
        }

        void menu_modify_Click(object sender, EventArgs e)
        {
            if (this.ListView.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫ�޸ĵ���...");
                return;
            }

            ListViewItem item = this.ListView.SelectedItems[0];
            LineState old_state = GetLineState(item);

            if (old_state == LineState.Deleted)
            {
                MessageBox.Show(this, "���Ѿ����ɾ�����в��ܽ����޸�...");
                return;
            }

            ResObjectDlg dlg = new ResObjectDlg();
            GuiUtil.AutoSetDefaultFont(dlg);
            dlg.ID = ListViewUtil.GetItemText(item, COLUMN_ID);

            dlg.State = ListViewUtil.GetItemText(item, COLUMN_STATE);
            dlg.Mime = ListViewUtil.GetItemText(item, COLUMN_MIME);
            dlg.LocalPath = ListViewUtil.GetItemText(item, COLUMN_LOCALPATH);
            dlg.SizeString = ListViewUtil.GetItemText(item, COLUMN_SIZE);
            dlg.Timestamp = ListViewUtil.GetItemText(item, COLUMN_TIMESTAMP);
            dlg.Usage = ListViewUtil.GetItemText(item, COLUMN_USAGE);
            dlg.Rights = ListViewUtil.GetItemText(item, COLUMN_RIGHTS);
            dlg.RightsCfgFileName = this.RightsCfgFileName;

            string strOldUsage = dlg.Usage;
            string strOldRights = dlg.Rights;

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            if (old_state != LineState.New)
            {
                SetLineInfo(item, 
                    // null, 
                    LineState.Changed);
                SetResChanged(item, dlg.ResChanged);
            }
            else
            {
                SetResChanged(item, true);
            }

            if (strOldRights != dlg.Rights
                || strOldUsage != dlg.Usage)
                SetXmlChanged(item, true);

            ListViewUtil.ChangeItemText(item, COLUMN_MIME, dlg.Mime);
            ListViewUtil.ChangeItemText(item, COLUMN_LOCALPATH, dlg.LocalPath);
            ListViewUtil.ChangeItemText(item, COLUMN_SIZE, dlg.SizeString);
            ListViewUtil.ChangeItemText(item, COLUMN_TIMESTAMP, dlg.Timestamp);
            ListViewUtil.ChangeItemText(item, COLUMN_USAGE, dlg.Usage);
            ListViewUtil.ChangeItemText(item, COLUMN_RIGHTS, dlg.Rights);
            this.Changed = true;
        }

        string GetNewID()
        {
            List<string> ids = new List<string>();
            for (int i = 0; i < this.ListView.Items.Count; i++)
            {
                string strCurrentID = ListViewUtil.GetItemText(this.ListView.Items[i], COLUMN_ID);
                ids.Add(strCurrentID);
            }

            int nSeed = 0;
            string strID = "";
            for (; ; )
            {
                strID = Convert.ToString(nSeed++);
                if (ids.IndexOf(strID) == -1)
                    return strID;
            }

        }

        void menu_new_Click(object sender, EventArgs e)
        {
            ResObjectDlg dlg = new ResObjectDlg();
            GuiUtil.AutoSetDefaultFont(dlg);

            dlg.ID = GetNewID();
            dlg.State = "";
            dlg.RightsCfgFileName = this.RightsCfgFileName;

            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
                return;

            ListViewItem item = new ListViewItem();
            this.ListView.Items.Add(item);

            SetLineInfo(item,
                // null,
                LineState.New);
            SetResChanged(item, true);
            SetXmlChanged(item, true);

            ListViewUtil.ChangeItemText(item, COLUMN_ID, dlg.ID);
            ListViewUtil.ChangeItemText(item, COLUMN_MIME, dlg.Mime);
            ListViewUtil.ChangeItemText(item, COLUMN_LOCALPATH, dlg.LocalPath);
            ListViewUtil.ChangeItemText(item, COLUMN_SIZE, dlg.SizeString);
            ListViewUtil.ChangeItemText(item, COLUMN_TIMESTAMP, dlg.Timestamp);
            ListViewUtil.ChangeItemText(item, COLUMN_USAGE, dlg.Usage);
            ListViewUtil.ChangeItemText(item, COLUMN_RIGHTS, dlg.Rights);
            this.Changed = true;
        }

        // ����usage�ַ�����Ѱ����
        public List<ListViewItem> FindItemByUsage(string strUsage)
        {
            List<ListViewItem> results = new List<ListViewItem>();
            for (int i = 0; i < this.ListView.Items.Count; i++)
            {
                ListViewItem item = this.ListView.Items[i];
                string strCurrentUsage = ListViewUtil.GetItemText(item, COLUMN_USAGE);
                if (strCurrentUsage == strUsage)
                    results.Add(item);
            }

            return results;
        }

        // TODO: findItemByRights ������һ�����߶�� right ֵ������Ѱ

        // ����ȫ���Ѿ����ɾ��������
        public List<ListViewItem> FindAllMaskDeleteItem()
        {
            List<ListViewItem> results = new List<ListViewItem>();
            for (int i = 0; i < this.ListView.Items.Count; i++)
            {
                ListViewItem item = this.ListView.Items[i];
                LineState old_state = GetLineState(item);

                if (old_state == LineState.Deleted)
                    results.Add(item);
            }

            return results;
        }


        // ����id�ַ�����Ѱ����
        public List<ListViewItem> FindItemByID(string strID)
        {
            List<ListViewItem> results = new List<ListViewItem>();
            for (int i = 0; i < this.ListView.Items.Count; i++)
            {
                ListViewItem item = this.ListView.Items[i];
                string strCurrentID = ListViewUtil.GetItemText(item, COLUMN_ID);
                if (strCurrentID == strID)
                    results.Add(item);
            }

            return results;
        }

        // ���һ���������δ���صı����ļ���
        // parameters:
        // return:
        //      -1  ����
        //      0   �������޸Ļ��ߴ�������δ���ص����
        //      1   �ɹ�
        public int GetUnuploadFilePath(ListViewItem item,
            out string strLocalPath,
            out string strError)
        {
            strError = "";
            strLocalPath = "";

            if (this.ListView.Items.IndexOf(item) == -1)
            {
                strError = "item���ǵ�ǰListView������֮һ";
                return -1;
            }

            LineState state = GetLineState(item);
            LineInfo info = (LineInfo)item.Tag;

            if (state == LineState.Changed ||
                state == LineState.New)
            {
                if (state == LineState.Changed)
                {
                    if (info != null
                        && info.ResChanged == false)
                    {
                        return 0;   // ��Դû���޸ĵ�
                    }
                }
                strLocalPath = ListViewUtil.GetItemText(item, COLUMN_LOCALPATH);
                return 1;
            }

            return 0;
        }

        public int ChangeObjectFile(ListViewItem item,
    string strObjectFilePath,
    string strUsage,
    out string strError)
        {
            return ChangeObjectFile(item, strObjectFilePath, strUsage, "", out strError);
        }

        public int ChangeObjectFile(ListViewItem item,
            string strObjectFilePath,
            string strUsage,
            string strRights,
            out string strError)
        {
            strError = "";

            if (this.ListView.Items.IndexOf(item) == -1)
            {
                strError = "item���ǵ�ǰListView������֮һ";
                return -1;
            }

            LineState old_state = GetLineState(item);
            if (old_state == LineState.Deleted)
            {
                strError = "���Ѿ����ɾ�����в��ܽ����޸�...";
                return -1;
            }
            ResObjectDlg dlg = new ResObjectDlg();
            dlg.ID = ListViewUtil.GetItemText(item, COLUMN_ID);

            dlg.State = ListViewUtil.GetItemText(item, COLUMN_STATE);
            dlg.Mime = ListViewUtil.GetItemText(item, COLUMN_MIME);
            dlg.LocalPath = ListViewUtil.GetItemText(item, COLUMN_LOCALPATH);
            dlg.SizeString = ListViewUtil.GetItemText(item, COLUMN_SIZE);
            dlg.Timestamp = ListViewUtil.GetItemText(item, COLUMN_TIMESTAMP);
            dlg.Usage = strUsage;
            dlg.Rights = strRights;
            dlg.RightsCfgFileName = this.RightsCfgFileName;

            string strOldUsage = dlg.Usage;
            string strOldRights = dlg.Rights;

            int nRet = dlg.SetObjectFilePath(strObjectFilePath,
            out strError);
            if (nRet == -1)
                return -1;

            if (old_state != LineState.New)
            {
                SetLineInfo(item, 
                    // null, 
                    LineState.Changed);
                SetResChanged(item, true);
            }
            else
            {
                SetResChanged(item, true);
            }

            if (strOldRights != dlg.Rights
                || strOldUsage != dlg.Usage)
                SetXmlChanged(item, true);

            ListViewUtil.ChangeItemText(item, COLUMN_MIME, dlg.Mime);
            ListViewUtil.ChangeItemText(item, COLUMN_LOCALPATH, dlg.LocalPath);
            ListViewUtil.ChangeItemText(item, COLUMN_SIZE, dlg.SizeString);
            ListViewUtil.ChangeItemText(item, COLUMN_TIMESTAMP, dlg.Timestamp);
            ListViewUtil.ChangeItemText(item, COLUMN_USAGE, dlg.Usage);
            ListViewUtil.ChangeItemText(item, COLUMN_RIGHTS, dlg.Rights);
            this.Changed = true;
            return 0;
        }

        /// <summary>
        /// ׷��һ������
        /// </summary>
        /// <param name="strObjectFilePath">�����ļ���ȫ·��</param>
        /// <param name="strUsage">��;�ַ���</param>
        /// <param name="strRights">Ȩ��</param>
        /// <param name="item">���� ListView ���Ĵ����� ListViewItem ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int AppendNewItem(
            string strObjectFilePath,
            string strUsage,
            string strRights,
            out ListViewItem item,
            out string strError)
        {
            strError = "";
            item = null;

            ResObjectDlg dlg = new ResObjectDlg();
            dlg.ID = GetNewID();

            dlg.State = "";
            dlg.Usage = strUsage;
            dlg.Rights = strRights;
            dlg.RightsCfgFileName = this.RightsCfgFileName;

            int nRet = dlg.SetObjectFilePath(strObjectFilePath,
                out strError);
            if (nRet == -1)
                return -1;


            item = new ListViewItem();
            this.ListView.Items.Add(item);

            SetLineInfo(item,
                // null,
                LineState.New);
            SetResChanged(item, true);
            SetXmlChanged(item, true);

            ListViewUtil.ChangeItemText(item, COLUMN_ID, dlg.ID);
            ListViewUtil.ChangeItemText(item, COLUMN_MIME, dlg.Mime);
            ListViewUtil.ChangeItemText(item, COLUMN_LOCALPATH, dlg.LocalPath);
            ListViewUtil.ChangeItemText(item, COLUMN_SIZE, dlg.SizeString);
            ListViewUtil.ChangeItemText(item, COLUMN_TIMESTAMP, dlg.Timestamp);
            ListViewUtil.ChangeItemText(item, COLUMN_USAGE, dlg.Usage);
            ListViewUtil.ChangeItemText(item, COLUMN_RIGHTS, dlg.Rights);
            this.Changed = true;
            return 0;
        }

        // ���ɾ����������
        public int MaskDelete(List<ListViewItem> items)
        {
            bool bRemoved = false;   // �Ƿ���������ɾ��listview item�����
            int nMaskDeleteCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                LineState state = GetLineState(item);

                // ������������Ѿ����ɾ��������
                if (state == LineState.Deleted)
                    continue;


                // ��������������������ô���״�listview���Ƴ�
                if (state == LineState.New)
                {
                    bRemoved = true;
                    this.ListView.Items.Remove(item);
                    continue;
                }

                // �����״̬
                SetOldLineState(item, state);

                SetLineInfo(item, 
                    // null, 
                    LineState.Deleted);

                this.Changed = true;

                nMaskDeleteCount++;
            }

            return nMaskDeleteCount;
        }

        void menu_delete_Click(object sender, EventArgs e)
        {
            if (this.ListView.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫɾ������...");
                return;
            }

            DialogResult result = MessageBox.Show(this,
                "ȷʵҪ���ɾ��ѡ���� "+this.ListView.SelectedItems.Count.ToString()+" ������? ",
                "BinaryResControl",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.ListView.SelectedItems)
            {
                items.Add(item);
            }

            bool bRemoved = false;   // �Ƿ���������ɾ��listview item�����
            int nMaskDeleteCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                ListViewItem item = items[i];

                LineState state = GetLineState(item);

                // ������������Ѿ����ɾ��������
                if (state == LineState.Deleted)
                    continue;


                // ��������������������ô���״�listview���Ƴ�
                if (state == LineState.New)
                {
                    bRemoved = true;
                    this.ListView.Items.Remove(item);
                    continue;
                }

                // �����״̬
                SetOldLineState(item, state);

                SetLineInfo(item, 
                    // null, 
                    LineState.Deleted);

                this.Changed = true;

                nMaskDeleteCount++;
            }

            if (bRemoved == true)
            {
                // ��Ҫ����listview���ǲ���������һ����Ҫ������������Changed��Ϊfalse
                if (IsChanged() == false)
                {
                    this.Changed = false;
                    return;
                }
            }

            if (nMaskDeleteCount > 0)
                MessageBox.Show(this, "ע�⣺���ɾ��������ֱ���ύ/����ʱ�Ż������ӷ�����ɾ����");
        }


        void menu_undoDelete_Click(object sender, EventArgs e)
        {
            if (this.ListView.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫ�������ɾ������...");
                return;
            }

            int nNotDeleted = 0;    // �û�ѡ��Ҫ����ɾ���������У��ж�������Ͳ����Ѿ����ɾ�������
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in this.ListView.SelectedItems)
            {
                // ListViewItem item = this.ListView.SelectedItems[i];

                if (GetLineState(item) != LineState.Deleted)
                {
                    nNotDeleted ++;
                    continue;
                }

                items.Add(item);
            }

            foreach (ListViewItem item in items)
            {
                // ListViewItem item = items[i];

                LineState old_state = GetOldLineState(item);

                Debug.Assert(old_state != LineState.Deleted, "");

                // �ָ����ɾ��ǰ�ľ�״̬
                SetLineInfo(item,
                    // null, 
                    old_state);

                this.Changed = true;
            }

            // ��Ҫ����listview���ǲ���������һ����Ҫ������������Changed��Ϊfalse
            if (IsChanged() == false)
                this.Changed = false;
        }

        // ���������ļ�
        void menu_export_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.ListView.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫ��������...");
                return;
            }

            if (this.ListView.SelectedItems.Count != 1)
            {
                MessageBox.Show(this, "һ��ֻ��ѡ��һ�е���...");
                return;
            }

            ListViewItem item = this.ListView.SelectedItems[0];

            LineState state = GetLineState(item);

            if (state == LineState.New)
            {
                strError = "��δ���صĶ��󣬱������ڱ��أ����赼��";
                goto ERROR1;
            }

            if (state == LineState.Changed)
            {
                strError = "�Ѿ��޸Ķ�δ�ύ�Ķ��󣬱������ڱ��أ����赼��";
                goto ERROR1;
            }

            string strID = ListViewUtil.GetItemText(item, COLUMN_ID);
            string strLocalPath = ListViewUtil.GetItemText(item, COLUMN_LOCALPATH);

            string strResPath = this.BiblioRecPath + "/object/" + strID;

            strResPath = strResPath.Replace(":", "/");


            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "��ָ��Ҫ����ı����ļ���";
            dlg.CreatePrompt = false;
            dlg.FileName = strLocalPath == "" ? strID + ".res" : strLocalPath;
            dlg.InitialDirectory = Environment.CurrentDirectory;
            // dlg.Filter = "projects files (outer*.xml)|outer*.xml|All files (*.*)|*.*" ;

            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("�������ض��� " + strResPath);
            Stop.BeginLoop();

            try
            {
                byte[] baOutputTimeStamp = null;

                // EnableControlsInLoading(true);

                string strMetaData;
                string strOutputPath = "";

                long lRet = this.Channel.GetRes(
                    Stop,
                    strResPath,
                    dlg.FileName,
                    out strMetaData,
                    out baOutputTimeStamp,
                    out strOutputPath,
                    out strError);
                // EnableControlsInLoading(false);
                if (lRet == -1)
                {
                    strError = "������Դ�ļ�ʧ�ܣ�ԭ��: " + strError;
                    goto ERROR1;
                }
            }
            finally
            {
                Stop.EndLoop();
                Stop.OnStop -= new StopEventHandler(this.DoStop);
                Stop.Initial("");
            }
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // ȷ���Ƿ�����ɾ�ĵ�����
        // ��this.Changed������ô��ȷ��
        bool IsChanged()
        {
            for (int i = 0; i < this.ListView.Items.Count; i++)
            {
                LineState state = GetLineState(this.ListView.Items[i]);
                if (state == LineState.Changed
                    || state == LineState.Deleted
                    || state == LineState.New)
                    return true;
            }

            return false;
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            menu_modify_Click(sender, e);
        }

        // ID��usage�Ƿ����ı�?
        // ����ɾ����Ĳ�����ID�ᷢ���ı䡣�����޸�һ�У�ID�������ı�
        // ID�иı䣬����Ҫ���¹���biblioxml���浽������
        public bool IsIdUsageChanged()
        {
            for (int i = 0; i < this.ListView.Items.Count; i++)
            {
                ListViewItem item = this.ListView.Items[i];

                LineState state = GetLineState(item);

                if (state == LineState.New
                    || state == LineState.Deleted)
                    return true;

                // �۲�usage�Ƿ�ı�
                LineInfo info = (LineInfo)item.Tag;
                if (info != null)
                {
#if NO
                    string strUsage = ListViewUtil.GetItemText(item, COLUMN_USAGE);
                    if (strUsage != info.InitialUsage)
                        return true;
#endif
                    if (info.XmlChanged == true)
                        return true;
                }
            }

            return false;
        }

        // �� XmlDocument ��������� <file> Ԫ�ء���Ԫ�ؼ����ڸ�֮��
        public int AddFileFragments(ref XmlDocument domRecord,
            out string strError)
        {
            strError = "";
            foreach (ListViewItem item in this.ListView.Items)
            {
                string strID = ListViewUtil.GetItemText(item, COLUMN_ID);

                if (String.IsNullOrEmpty(strID) == true)
                    continue;

                string strUsage = ListViewUtil.GetItemText(item, COLUMN_USAGE);
                string strRights = ListViewUtil.GetItemText(item, COLUMN_RIGHTS);

                XmlElement node = domRecord.CreateElement("dprms",
                    "file",
                    DpNs.dprms);
                domRecord.DocumentElement.AppendChild(node);

                node.SetAttribute("id", strID);
                if (string.IsNullOrEmpty(strUsage) == false)
                    node.SetAttribute("usage", strUsage);
                if (string.IsNullOrEmpty(strRights) == false)
                    node.SetAttribute("rights", strRights);
            }

            return 0;
        }

#if NO
        // ���ȫ��ID
        public List<string> GetIds()
        {
            List<string> results = new List<string>();

            for (int i = 0; i < this.ListView.Items.Count; i++)
            {
                ListViewItem item = this.ListView.Items[i];

                LineState state = GetLineState(item);

                if (state == LineState.Deleted)
                    continue;   // ���Ա��ɾ��������

                string strID = ListViewUtil.GetItemText(item ,COLUMN_ID);

                results.Add(strID);
            }

            return results;
        }

        // ���ȫ��usage
        public List<string> GetUsages()
        {
            List<string> results = new List<string>();

            for (int i = 0; i < this.ListView.Items.Count; i++)
            {
                ListViewItem item = this.ListView.Items[i];

                LineState state = GetLineState(item);

                if (state == LineState.Deleted)
                    continue;   // ���Ա��ɾ��������

                string strUsage = ListViewUtil.GetItemText(item, COLUMN_USAGE);
                results.Add(strUsage);
            }

            return results;
        }

#endif

        // ��·����ȡ����¼�Ų���
        // parammeters:
        //      strPath ·��������"����ͼ��/3"
        public static string GetRecordID(string strPath)
        {
            int nRet = strPath.LastIndexOf("/");
            if (nRet == -1)
                return "";

            return strPath.Substring(nRet + 1).Trim();
        }

        // �Ƿ�Ϊ������¼��·��
        public static bool IsNewPath(string strPath)
        {
            if (String.IsNullOrEmpty(strPath) == true)
                return true;    //???? ��·��������·��?

            string strID = GetRecordID(strPath);

            if (strID == "?"
                || String.IsNullOrEmpty(strID) == true) // 2008/11/28 
                return true;

            return false;
        }

        // ������Դ��������
        // return:
        //		-1	error
        //		>=0 ʵ�����ص���Դ������
        public int Save(
            out string strError)
        {
            strError = "";

            if (this.ListView.Items.Count == 0)
                return 0;

            if (String.IsNullOrEmpty(this.BiblioRecPath) == true)
            {
                strError = "��δָ��BiblioRecPath";
                return -1;
            }

            if (IsNewPath(this.BiblioRecPath) == true)
            {
                strError = "��Ŀ��¼·�� '" + this.BiblioRecPath + "' �����ѱ���ļ�¼·�����޷����ڶ�����Դ����";
                return -1;
            }

            if (this.Channel == null)
            {
                strError = "BinaryResControl��δָ��Channel";
                return -1;
            }

            StopStyle old_stop_style = StopStyle.None;

            if (Stop != null)
            {
                old_stop_style = Stop.Style;
                Stop.Style = StopStyle.EnableHalfStop;

                Stop.OnStop += new StopEventHandler(this.DoStop);
                Stop.Initial("����������Դ ...");
                Stop.BeginLoop();
            }

            int nUploadCount = 0;   // ʵ�����ص���Դ����

            try
            {
                // bNotAskTimestampMismatchWhenOverwrite = false;

                for (int i = 0; i < this.ListView.Items.Count; i++)
                {
                    ListViewItem item = this.ListView.Items[i];
                    LineInfo info = (LineInfo)item.Tag;
                    // string strUsage = ListViewUtil.GetItemText(item, COLUMN_USAGE);

                    LineState state = GetLineState(item);

                    if (state == LineState.Changed ||
                        state == LineState.New)
                    {
                        if (state == LineState.Changed)
                        {
                            if (info != null
                                && info.ResChanged == false)
                            {
                                SetLineInfo(item,
                                    // strUsage, 
                                    LineState.Normal);
                                SetXmlChanged(item, false);
                                SetResChanged(item, false);
                                continue;   // ��Դû���޸ĵģ�����������
                            }
                        }
                    }
                    else
                    {
                        // ���ɾ�������ֻҪ��ĿXML���¹����ʱ��
                        // ��������ID����ĿXML����󣬾͵���ɾ���˸����
                        // ���Ա�����ֻ�Ǽ�Remove������listview�����
                        if (state == LineState.Deleted)
                        {
                            this.ListView.Items.Remove(item);
                            i--;
                        }

                        continue;
                    }

                    string strState = ListViewUtil.GetItemText(item, COLUMN_STATE);

                    string strID = ListViewUtil.GetItemText(item, COLUMN_ID);
                    string strResPath = this.BiblioRecPath + "/object/" + ListViewUtil.GetItemText(item, COLUMN_ID);
                    string strLocalFilename = ListViewUtil.GetItemText(item, COLUMN_LOCALPATH);
                    string strMime = ListViewUtil.GetItemText(item, COLUMN_MIME);
                    string strTimestamp = ListViewUtil.GetItemText(item, COLUMN_TIMESTAMP);

                    // ����ļ��ߴ�
                    FileInfo fi = new FileInfo(strLocalFilename);

                    if (fi.Exists == false)
                    {
                        strError = "�ļ� '" + strLocalFilename + "' ������...";
                        return -1;
                    }

                    string[] ranges = null;

                    if (fi.Length == 0)
                    {
                        // ���ļ�
                        ranges = new string[1];
                        ranges[0] = "";
                    }
                    else
                    {
                        string strRange = "";
                        strRange = "0-" + Convert.ToString(fi.Length - 1);

                        // ����100K��Ϊһ��chunk
                        // TODO: ʵ�ֻ������ڣ���������������chunk�ߴ�
                        ranges = RangeList.ChunkRange(strRange,
                            500 * 1024);
                    }

                    byte[] timestamp = ByteArray.GetTimeStampByteArray(strTimestamp);
                    byte[] output_timestamp = null;

                    nUploadCount++;

                    // REDOWHOLESAVE:
                    string strWarning = "";

                    for (int j = 0; j < ranges.Length; j++)
                    {
                        // REDOSINGLESAVE:

                        Application.DoEvents();	// ���ý������Ȩ

                        if (Stop.State != 0)
                        {
                            strError = "�û��ж�";
                            goto ERROR1;
                        }

                        string strWaiting = "";
                        if (j == ranges.Length - 1)
                            strWaiting = " �����ĵȴ�...";

                        string strPercent = "";
                        RangeList rl = new RangeList(ranges[j]);
                        if (rl.Count >= 1)
                        {
                            double ratio = (double)((RangeItem)rl[0]).lStart / (double)fi.Length;
                            strPercent = String.Format("{0,3:N}", ratio * (double)100) + "%";
                        }

                        if (Stop != null)
                            Stop.SetMessage("�������� " + ranges[j] + "/"
                                + Convert.ToString(fi.Length)
                                + " " + strPercent + " " + strLocalFilename + strWarning + strWaiting);

                        long lRet = this.Channel.SaveResObject(
                            Stop,
                            strResPath,
                            strLocalFilename,
                            strLocalFilename,
                            strMime,
                            ranges[j],
                            j == ranges.Length - 1 ? true : false,	// ��βһ�β��������ѵײ�ע�����������WebService API��ʱʱ��
                            timestamp,
                            out output_timestamp,
                            out strError);
                        timestamp = output_timestamp;

                        ListViewUtil.ChangeItemText(item,
                            COLUMN_TIMESTAMP,
                            ByteArray.GetHexTimeStampString(timestamp));

                        strWarning = "";

                        if (lRet == -1)
                        {
                            /*
                            if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                            {

                                if (this.bNotAskTimestampMismatchWhenOverwrite == true)
                                {
                                    timestamp = new byte[output_timestamp.Length];
                                    Array.Copy(output_timestamp, 0, timestamp, 0, output_timestamp.Length);
                                    strWarning = " (ʱ�����ƥ��, �Զ�����)";
                                    if (ranges.Length == 1 || j == 0)
                                        goto REDOSINGLESAVE;
                                    goto REDOWHOLESAVE;
                                }


                                DialogResult result = MessageDlg.Show(this,
                                    "���� '" + strLocalFilename + "' (Ƭ��:" + ranges[j] + "/�ܳߴ�:" + Convert.ToString(fi.Length)
                                    + ") ʱ����ʱ�����ƥ�䡣��ϸ������£�\r\n---\r\n"
                                    + strError + "\r\n---\r\n\r\n�Ƿ�����ʱ���ǿ������?\r\nע��(��)ǿ������ (��)���Ե�ǰ��¼����Դ���أ�����������Ĵ��� (ȡ��)�ж�����������",
                                    "dp2batch",
                                    MessageBoxButtons.YesNoCancel,
                                    MessageBoxDefaultButton.Button1,
                                    ref this.bNotAskTimestampMismatchWhenOverwrite);
                                if (result == DialogResult.Yes)
                                {
                                    timestamp = new byte[output_timestamp.Length];
                                    Array.Copy(output_timestamp, 0, timestamp, 0, output_timestamp.Length);
                                    strWarning = " (ʱ�����ƥ��, Ӧ�û�Ҫ������)";
                                    if (ranges.Length == 1 || j == 0)
                                        goto REDOSINGLESAVE;
                                    goto REDOWHOLESAVE;
                                }

                                if (result == DialogResult.No)
                                {
                                    goto END1;	// �������������Դ
                                }

                                if (result == DialogResult.Cancel)
                                {
                                    strError = "�û��ж�";
                                    goto ERROR1;	// �ж���������
                                }
                            }
                             * */

                            goto ERROR1;
                        }
                    }

                    SetLineInfo(item, 
                        // strUsage, 
                        LineState.Normal);
                    SetXmlChanged(item, false);
                    SetResChanged(item, false);
                }

                this.Changed = false;
                return nUploadCount;
            ERROR1:
                return -1;
            }
            finally
            {
                if (Stop != null)
                {
                    Stop.EndLoop();
                    Stop.OnStop -= new StopEventHandler(this.DoStop);
                    if (nUploadCount > 0)
                        Stop.Initial("������Դ���");
                    else
                        Stop.Initial("");
                    Stop.Style = old_stop_style;
                }
            }
        }

        private void ListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control == true)
            {
                // Ctrl+A
                menu_generateData_Click(sender, null);
            }
        }

        public string ErrorInfo
        {
            get
            {
                return this.Text;
            }
            set
            {
                this.Text = value;
                if (this.ListView != null)
                {
                    if (string.IsNullOrEmpty(value) == true)
                        this.ListView.Visible = true;
                    else
                        this.ListView.Visible = false;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // ���ƴ�����Ϣ�ַ���
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
#if NO
            Brush brush = new SolidBrush(Color.FromArgb(100, 0,0,255));
            e.Graphics.FillEllipse(brush, 30, 30, 100, 100);
#endif
            if (string.IsNullOrEmpty(this.Text) == true)
                return;

            StringFormat format = new StringFormat();   //  (StringFormat)StringFormat.GenericTypographic.Clone();
            format.FormatFlags |= StringFormatFlags.FitBlackBox;
            format.Alignment = StringAlignment.Center;
            format.FormatFlags |= StringFormatFlags.FitBlackBox;
            SizeF size = e.Graphics.MeasureString(this.Text,
                this.Font,
                this.Size.Width,
                format);

            RectangleF textRect = new RectangleF(
(this.Size.Width - size.Width) / 2,
(this.Size.Height - size.Height) / 2,
size.Width,
size.Height);
            using (Brush brush = new SolidBrush(this.ForeColor))
            {
                e.Graphics.DrawString(
                    this.Text,
                    this.Font,
                    brush,
                    textRect,
                    format);
            }
        }

    }

    class LineInfo
    {
        public LineState LineState = LineState.Normal;

        // ���ɾ��ǰ��״̬
        public LineState OldLineState = LineState.Normal;

        // public string InitialUsage = "";    // �����usageֵ

        public bool ResChanged = false; // ��Դ�Ƿ��޸Ĺ������һ�������޸Ĺ���������Դû���޸Ĺ�����Ϊusage�޸Ĺ�

        public bool XmlChanged = false; // ������Դ�� usage rights �Ƿ��޸Ĺ���2015/7/11
    }

    enum LineState
    {
        Normal = 0, // ��ͨ���Ѿ����ص�����
        New = 1,    // �����ģ���δ���ص�ʱ��
        Changed = 2,    // �޸Ĺ��ģ���������δ���ص�����
        Deleted = 3,    // ���ɾ���ģ���δ�ύɾ��������
        Error = 4,  // ���metadataʱ���������
    }
}
