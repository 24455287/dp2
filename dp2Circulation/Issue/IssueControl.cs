using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.CommonControl;    // LocationCollection
using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient;
using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    //public partial class IssueControl : UserControl
    /// <summary>
    /// �ڼ�¼�б�ؼ�
    /// </summary>
    public partial class IssueControl : IssueControlBase
    {

        // 
        /// <summary>
        /// ׼������
        /// </summary>
        public event PrepareAcceptEventHandler PrepareAccept = null;

        // 
        /// <summary>
        /// ����ʵ������
        /// </summary>
        public event GenerateEntityEventHandler GenerateEntity = null;

        /// <summary>
        /// �޸Ĳ����
        /// </summary>
        public event ChangeItemEventHandler ChangeItem = null;

        /// <summary>
        /// Ŀ���¼·��
        /// </summary>
        public string TargetRecPath = "";   // 4��״̬��1)�����·���͵�ǰ��¼·��һ�£�����ʵ���¼�ʹ����ڵ�ǰ��¼�£�2)�����·���͵�ǰ��¼·����һ�£��ּ�¼�Ѿ����ڣ���Ҫ�������洴��ʵ���¼��3) �����·�����п������֣���ʾ�ּ�¼�����ڣ���Ҫ���ݵ�ǰ��¼��MARC��������4) �����·��Ϊ�գ���ʾ��Ҫͨ���˵�ѡ��Ŀ��⣬Ȼ������ͬ3)
        
        /// <summary>
        /// �������κ�
        /// </summary>
        public string AcceptBatchNo = "";   // �������κ�
        
        /// <summary>
        /// �Ƿ�Ҫ�����ղ���ĩ���Զ������������������ŵĽ���?
        /// </summary>
        public bool InputItemsBarcode = true;   // �Ƿ�Ҫ�����ղ���ĩ���Զ������������������ŵĽ���?

        /// <summary>
        /// �Ƿ�Ϊ�´����Ĳ��¼���á��ӹ��С�״̬
        /// </summary>
        public bool SetProcessingState = true;   // �Ƿ�Ϊ�´����Ĳ��¼���á��ӹ��С�״̬ 2009/10/19

        /// <summary>
        /// �Ƿ�Ϊ�´����Ĳ��¼������ȡ��
        /// </summary>
        public bool CreateCallNumber = false;   // �Ƿ�Ϊ�´����Ĳ��¼������ȡ�� 2012/5/7

        // 
        /// <summary>
        /// ��ö�����Ϣ
        /// </summary>
        public event GetOrderInfoEventHandler GetOrderInfo = null;

        // 
        /// <summary>
        /// ��ò���Ϣ
        /// </summary>
        public event GetItemInfoEventHandler GetItemInfo = null;

#if NO
                public event LoadRecordHandler LoadRecord = null;

                // Ctrl+A�Զ���������
        /// <summary>
        /// �Զ���������
        /// </summary>
        public event GenerateDataEventHandler GenerateData = null;

        // ����������к�����
        SortColumns SortColumns = new SortColumns();


        /// <summary>
        /// ������� / ��ֹ״̬�����ı�
        /// </summary>
        public event EnableControlsHandler EnableControlsEvent = null;

        public bool m_bRemoveDeletedItem = false;   // ��ɾ������ʱ, �Ƿ���Ӿ���Ĩ����Щ����(ʵ�����ڴ����滹�����м����ύ������)?

        /// <summary>
        /// ͨѶͨ��
        /// </summary>
        public LibraryChannel Channel = null;

        /// <summary>
        /// ֹͣ����
        /// </summary>
        public DigitalPlatform.Stop Stop = null;

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// ��ú��ֵ
        /// </summary>
        public event GetMacroValueHandler GetMacroValue = null;

        /// <summary>
        /// ���ݷ����ı�
        /// </summary>
        public event ContentChangedEventHandler ContentChanged = null;

        string m_strBiblioRecPath = "";

        public IssueItemCollection Items = null;

#endif

        /// <summary>
        /// ���캯��
        /// </summary>
        public IssueControl()
        {
            InitializeComponent();

            this.m_listView = this.listView;
            this.ItemType = "issue";
            this.ItemTypeName = "��";
        }

#if NO
        public int IssueCount
        {
            get
            {
                if (this.Items != null)
                    return this.Items.Count;

                return 0;
            }
        }

        // ��listview�е��������޸�Ϊnew״̬
        public void ChangeAllItemToNewState()
        {
            foreach (IssueItem issueitem in this.Items)
            {
                // IssueItem issueitem = this.IssueItems[i];

                if (issueitem.ItemDisplayState == ItemDisplayState.Normal
                    || issueitem.ItemDisplayState == ItemDisplayState.Changed
                    || issueitem.ItemDisplayState == ItemDisplayState.Deleted)   // ע��δ�ύ��deletedҲ��Ϊnew��
                {
                    issueitem.ItemDisplayState = ItemDisplayState.New;
                    issueitem.RefreshListView();
                    issueitem.Changed = true;    // ��һ�������ʹ�ܺ���������رմ��ڣ��Ƿ�ᾯ��(ʵ���޸�)���ݶ�ʧ
                }
            }
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

                if (this.Items != null)
                {
                    string strID = Global.GetRecordID(value);
                    this.Items.SetParentID(strID);
                }
            }
        }

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                if (this.Items == null)
                    return false;

                return this.Items.Changed;
            }
            set
            {
                if (this.Items != null)
                    this.Items.Changed = value;
            }
        }

        // ���listview�е�ȫ������
        public void Clear()
        {
            this.ListView.Items.Clear();
            // this.BiblioRecPath = "";

            // 2009/2/10
            this.SortColumns.Clear();
            SortColumns.ClearColumnSortDisplay(this.ListView.Columns);
        }

        // ������й���Ϣ
        public void ClearIssues()
        {
            this.Clear();
            this.Items = new IssueItemCollection();
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }

        public int CountOfVisibleIssueItems()
        {
            return this.ListView.Items.Count;
        }

        public int IndexOfVisibleIssueItems(IssueItem issueitem)
        {
            for (int i = 0; i < this.ListView.Items.Count; i++)
            {
                IssueItem cur = (IssueItem)this.ListView.Items[i].Tag;

                if (cur == issueitem)
                    return i;
            }

            return -1;
        }

        public IssueItem GetAtVisibleIssueItems(int nIndex)
        {
            return (IssueItem)this.ListView.Items[nIndex].Tag;
        }

#endif

        // 
        // return:
        //      -1  ����
        //      0   û��װ��
        //      1   �Ѿ�װ��
        /// <summary>
        /// ���һ����Ŀ��¼������ȫ���ڼ�¼·��
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
        public static int GetIssueRecPaths(
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

                long lRet = channel.GetIssues(
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
                        strError = "·��Ϊ '" + entities[i].OldRecPath + "' ���ڼ�¼װ���з�������: " + entities[i].ErrorInfo;  // NewRecPath
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
        // װ���ڼ�¼
        // return:
        //      -1  ����
        //      0   û��װ��
        //      1   �Ѿ�װ��
        public int LoadIssueRecords(string strBiblioRecPath,
            out string strError)
        {
            this.BiblioRecPath = strBiblioRecPath;

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("����װ������Ϣ ...");
            Stop.BeginLoop();

            this.Update();
            // this.MainForm.Update();

            try
            {
                // string strHtml = "";
                long lStart = 0;
                long lResultCount = 0;
                long lCount = -1;
                this.ClearIssues();

                // 2012/5/9 ��дΪѭ����ʽ
                for (; ; )
                {
                    EntityInfo[] issues = null;

                    long lRet = Channel.GetIssues(
                        Stop,
                        strBiblioRecPath,
                            lStart,
                            lCount,
                            "",
                            "zh",
                        out issues,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;


                    if (lRet == 0)
                        return 0;

                    lResultCount = lRet;

                    Debug.Assert(issues != null, "");

                    this.ListView.BeginUpdate();
                    try
                    {
                        for (int i = 0; i < issues.Length; i++)
                        {
                            if (issues[i].ErrorCode != ErrorCodeValue.NoError)
                            {
                                strError = "·��Ϊ '" + issues[i].OldRecPath + "' ���ڼ�¼װ���з�������: " + issues[i].ErrorInfo;  // NewRecPath
                                return -1;
                            }

                            // ����һ���ڵ�xml��¼��ȡ���й���Ϣ����listview��
                            IssueItem issueitem = new IssueItem();

                            int nRet = issueitem.SetData(issues[i].OldRecPath, // NewRecPath
                                     issues[i].OldRecord,
                                     issues[i].OldTimestamp,
                                     out strError);
                            if (nRet == -1)
                                return -1;

                            if (issues[i].ErrorCode == ErrorCodeValue.NoError)
                                issueitem.Error = null;
                            else
                                issueitem.Error = issues[i];

                            this.Items.Add(issueitem);


                            issueitem.AddToListView(this.ListView);
                        }
                    }
                    finally
                    {
                        this.ListView.EndUpdate();
                    }

                    lStart += issues.Length;
                    if (lStart >= lResultCount)
                        break;
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

        // ����һ���ڣ�Ҫ�򿪶Ի�����������ϸ��Ϣ
        void DoNewIssue(/*string strPublishTime*/)
        {
            string strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(this.BiblioRecPath) == true)
            {
                strError = "��δ������Ŀ��¼";
                goto ERROR1;
            }

            // 
            if (this.Items == null)
                this.Items = new IssueItemCollection();

            Debug.Assert(this.Items != null, "");

            bool bOldChanged = this.Items.Changed;

#if NO
            if (String.IsNullOrEmpty(strPublishTime) == false)
            {

                // �Ե�ǰ�����ڽ��г���ʱ�����
                IssueItem dupitem = this.IssueItems.GetItemByPublishTime(
                    strPublishTime,
                    null);
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "�������ĳ���ʱ�� '" + strPublishTime + "' �ͱ�����δ�ύ֮һɾ������ʱ�����ء��������ύ����֮�޸ģ��ٽ����ڼǵ���";
                    else
                        strText = "�������ĳ���ʱ�� '" + strPublishTime + "' �ڱ������Ѿ����ڡ�";

                    // ������δ����
                    DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
        strText + "\r\n\r\nҪ�������Ѵ��ڳ���ʱ������޸���",
        "EntityForm",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button2);

                    // תΪ�޸�
                    if (result == DialogResult.Yes)
                    {
                        ModifyIssue(dupitem);
                        return;
                    }

                    // ͻ����ʾ���Ա������Ա�۲������Ѿ����ڵļ�¼
                    dupitem.HilightListViewItem(true);
                    return;
                }

                // ��(����)�����ڼ�¼���г���ʱ�����
                if (true)
                {
                    string strIssueText = "";
                    string strBiblioText = "";
                    nRet = SearchIssuePublishTime(strPublishTime,
                        this.BiblioRecPath,
                        out strIssueText,
                        out strBiblioText,
                        out strError);
                    if (nRet == -1)
                        MessageBox.Show(ForegroundWindow.Instance, "�Գ���ʱ�� '" + strPublishTime + "' ���в��صĹ����з�������: " + strError);
                    else if (nRet == 1) // �����ظ�
                    {
                        IssuePublishTimeFoundDupDlg dlg = new IssuePublishTimeFoundDupDlg();
                        MainForm.SetControlFont(dlg, this.Font, false);
                        dlg.MainForm = this.MainForm;
                        dlg.BiblioText = strBiblioText;
                        dlg.IssueText = strIssueText;
                        dlg.MessageText = "�������ĳ���ʱ�� '" + strPublishTime + "' �����ݿ��з����Ѿ����ڡ�����޷�������";
                        dlg.ShowDialog(this);
                        return;
                    }
                }

            } // end of ' if (String.IsNullOrEmpty(strPublishTime) == false)
#endif

            IssueItem issueitem = new IssueItem();

            // ����ȱʡֵ
            nRet = SetItemDefaultValues(
                "issue_normalRegister_default",
                true,
                issueitem,
                out strError);
            if (nRet == -1)
            {
                strError = "����ȱʡֵ��ʱ��������: " + strError;
                goto ERROR1;
            }

#if NO
            issueitem.PublishTime = strPublishTime;
#endif
            issueitem.Parent = Global.GetRecordID(this.BiblioRecPath);


            // �ȼ����б�
            this.Items.Add(issueitem);
            issueitem.ItemDisplayState = ItemDisplayState.New;
            issueitem.AddToListView(this.listView);
            issueitem.HilightListViewItem(true);

            issueitem.Changed = true;    // ��Ϊ�����������������ζ����޸Ĺ����������Ա��⼯����ֻ��һ�����������ʱ�򣬼��ϵ�changedֵ����

            IssueEditForm edit = new IssueEditForm();

            edit.BiblioDbName = Global.GetDbName(this.BiblioRecPath);   // 2009/2/15
            edit.Text = "������";
            edit.MainForm = this.MainForm;
            // edit.EntityForm = this;
            nRet = edit.InitialForEdit(issueitem,
                this.Items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            //REDO:
            this.MainForm.AppInfo.LinkFormState(edit, "IssueEditForm_state");
            edit.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(edit);

            if (edit.DialogResult != DialogResult.OK
                && edit.Item == issueitem    // ������δǰ���ƶ��������ƶ��ص���㣬Ȼ��Cancel
                )
            {
                this.Items.PhysicalDeleteItem(issueitem);

#if NO
                // �ı䱣�水ť״̬
                // SetSaveAllButtonState(true);
                if (this.ContentChanged != null)
                {
                    ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                    e1.OldChanged = bOldChanged;
                    e1.CurrentChanged = this.Items.Changed;
                    this.ContentChanged(this, e1);
                }
#endif
                TriggerContentChanged(bOldChanged, this.Items.Changed);

                return;
            }

#if NO
            // �ı䱣�水ť״̬
            // SetSaveAllButtonState(true);
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = true;
                this.ContentChanged(this, e1);
            }
#endif
            TriggerContentChanged(bOldChanged, true);

            // Ҫ�Ա��ֽ��г������ںͲο�ID���ء�
            // ������ˣ�Ҫ���ִ��ڣ��Ա��޸ġ�����������Ƕȣ���������ڶԻ���ر�ǰ����
            // �������´򿪶Ի���
            string strPublishTime = issueitem.PublishTime;
            if (String.IsNullOrEmpty(strPublishTime) == false)
            {

                // ��Ҫ�ų����ռ�����Լ�: issueitem��
                List<IssueItem> excludeItems = new List<IssueItem>();
                excludeItems.Add(issueitem);

                // �Ե�ǰ�����ڽ��г���ʱ�����
                IssueItem dupitem = this.Items.GetItemByPublishTime(
                    strPublishTime,
                    excludeItems);
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "�������ĳ���ʱ�� '" + strPublishTime + "' �ͱ�����δ�ύ֮һɾ������ʱ�����ء��������ύ����֮�޸ģ��ٽ����ڼǵ���";
                    else
                        strText = "�������ĳ���ʱ�� '" + strPublishTime + "' �ڱ������Ѿ����ڡ�";

                    // ������δ����
                    DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
        strText + "\r\n\r\nҪ�������¼�¼�ĳ���ʱ������޸���\r\n(Yes �����޸�; No ���޸ģ��÷����ظ����¼�¼�����б�; Cancel �����ոմ������¼�¼)",
        "EntityForm",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);

                    // תΪ�޸�
                    if (result == DialogResult.Yes)
                    {
                        ModifyIssue(issueitem);
                        return;
                    }

                    // �����ոմ����ļ�¼
                    if (result == DialogResult.Cancel)
                    {
                        this.Items.PhysicalDeleteItem(issueitem);

#if NO
                        // �ı䱣�水ť״̬
                        // SetSaveAllButtonState(true);
                        if (this.ContentChanged != null)
                        {
                            ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                            e1.OldChanged = bOldChanged;
                            e1.CurrentChanged = this.Items.Changed;
                            this.ContentChanged(this, e1);
                        } 
#endif
                        TriggerContentChanged(bOldChanged, this.Items.Changed);
                        return;
                    }

                    // ͻ����ʾ���Ա������Ա�۲������Ѿ����ڵļ�¼
                    dupitem.HilightListViewItem(true);
                    return;
                }
            } // end of ' if (String.IsNullOrEmpty(strPublishTime) == false)

            return;
        ERROR1:
            MessageBox.Show(ForegroundWindow.Instance, strError);
            return;
        }

        // �Ժ���Ҫ�õ���ʱ����޸Ĳ���
#if NO
        // �����ڲο�ID�������²ο�ID�Ĳ��ء�
        int SearchIssueRefID(string strRefID,
            string strBiblioRecPath,
            out string strIssueText,
            out string strBiblioText,
            out string strError)
        {
            strError = "";
            strIssueText = "";
            strBiblioText = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڶԲο�ID '" + strRefID + "' ���в��� ...");
            stop.BeginLoop();

            try
            {
                byte[] issue_timestamp = null;
                string strIssueRecPath = "";
                string strOutputBiblioRecPath = "";

                long lRet = Channel.GetIssueInfo(
                    stop,
                    strRefID,
                    strBiblioRecPath,
                    "html",
                    out strIssueText,
                    out strIssueRecPath,
                    out issue_timestamp,
                    "html",
                    out strBiblioText,
                    out strOutputBiblioRecPath,
                    out strError);
                if (lRet == -1)
                    return -1;  // error

                if (lRet == 0)
                    return 0;   // not found
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

        // �ο�ID���ء�����(������)�ɲο�ID���ء�
        // �����������Զ��ų��͵�ǰ·��strOriginRecPath�ظ�֮����
        // parameters:
        //      strRefID  �ο�ID��
        //      strOriginRecPath    ������¼��·����
        //      paths   �������е�·��
        // return:
        //      -1  error
        //      0   not dup
        //      1   dup
        int SearchIssueRefIdDup(string strRefID,
            // string strBiblioRecPath,
            string strOriginRecPath,
            out string[] paths,
            out string strError)
        {
            strError = "";
            paths = null;

            if (string.IsNullOrEmpty(strRefID) == true)
            {
                strError = "��Ӧ�òο�IDΪ��������";
                return -1;
            }

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڶԲο�ID '" + strRefID + "' ���в��� ...");
            Stop.BeginLoop();

            try
            {
                /*
                long lRet = Channel.SearchIssueDup(
                    stop,
                    strRefID,
                    strBiblioRecPath,
                    100,
                    out paths,
                    out strError);
                 * */
                long lRet = Channel.SearchIssue(
    Stop,
    "<ȫ��>",
    strRefID,
    100,
    "�ο�ID",
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
                        strError = "ϵͳ����: SearchIssue() API����ֵΪ1������paths����ĳߴ�ȴ����1, ���� " + paths.Length.ToString();
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

#if NO
        string DoGetMacroValue(string strMacroName)
        {
            if (this.GetMacroValue != null)
            {
                GetMacroValueEventArgs e = new GetMacroValueEventArgs();
                e.MacroName = strMacroName;
                this.GetMacroValue(this, e);

                return e.MacroValue;
            }

            return null;
        }
#endif

#if NO
        // ΪIssueItem��������ȱʡֵ
        // parameters:
        //      strCfgEntry Ϊ"issue_normalRegister_default"��"issue_quickRegister_default"
        int SetIssueItemDefaultValues(
            string strCfgEntry,
            IssueItem issueitem,
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

            int nRet = issueitem.SetData("",
                strNewDefault,
                null,
                out strError);
            if (nRet == -1)
                return -1;

            issueitem.Parent = "";
            issueitem.RecPath = "";

            return 0;
        }
#endif

        void ModifyIssue(IssueItem issueitem)
        {
            Debug.Assert(issueitem != null, "");

            bool bOldChanged = this.Items.Changed;

            string strOldRefID = issueitem.RefID;
            string strOldPublishTime = issueitem.PublishTime;

            IssueEditForm edit = new IssueEditForm();

            edit.BiblioDbName = Global.GetDbName(this.BiblioRecPath);   // 2009/2/15
            edit.MainForm = this.MainForm;
            edit.ItemControl = this;
            string strError = "";
            int nRet = edit.InitialForEdit(issueitem,
                this.Items,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(ForegroundWindow.Instance, strError);
                return;
            }
            edit.StartItem = null;  // ���ԭʼ������

        REDO:
            this.MainForm.AppInfo.LinkFormState(edit, "IssueEditForm_state");
            edit.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(edit);

            if (edit.DialogResult != DialogResult.OK)
                return;
#if NO
            // IssueItem�����Ѿ����޸�
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = true;
                this.ContentChanged(this, e1);
            }
#endif
            TriggerContentChanged(bOldChanged, true);

            this.EnableControls(false);
            try
            {
                // �Գ������ڲ���
                if (strOldPublishTime != issueitem.PublishTime) // �������ڸı��˵�����²Ų���
                {
                    if (string.IsNullOrEmpty(issueitem.PublishTime) == true)
                    {
                        MessageBox.Show(ForegroundWindow.Instance, "�������ڲ���Ϊ�ա�����ȷ������ť�������롣");
                        goto REDO;
                    }

                    // ��Ҫ�ų����Լ�: issueitem��
                    List<IssueItem> excludeItems = new List<IssueItem>();
                    excludeItems.Add(issueitem);

                    // �Ե�ǰ�����ڽ��вο�ID����
                    IssueItem dupitem = this.Items.GetItemByPublishTime(
                        issueitem.PublishTime,
                        excludeItems);
                    if (dupitem != null)
                    {
                        string strText = "";
                        if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                            strText = "�������� '" + issueitem.RefID + "' �ͱ�����δ�ύ֮һɾ�������������ء�����ȷ������ť�������룬���˳��Ի���������ύ����֮�޸ġ�";
                        else
                            strText = "�������� '" + issueitem.RefID + "' �ڱ������Ѿ����ڡ�����ȷ������ť�������롣";

                        MessageBox.Show(ForegroundWindow.Instance, strText);
                        goto REDO;
                    }

                    // ע���������ڵĲ���ֻ�Ա������ڼ�¼�����塣��ͬ�����������ڼ�¼�����������������ظ���
                }

                // �Բο�ID���в���
                if (string.IsNullOrEmpty(issueitem.RefID) == false)
                {
                    // ��Ҫ�ų����Լ�: issueitem��
                    List<BookItemBase> excludeItems = new List<BookItemBase>();
                    excludeItems.Add(issueitem);

                    // �Ե�ǰ�����ڽ��вο�ID����
                    IssueItem dupitem = this.Items.GetItemByRefID(
                        issueitem.RefID,
                        excludeItems) as IssueItem;
                    if (dupitem != null)
                    {
                        string strText = "";
                        if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                            strText = "�ο�ID '" + issueitem.RefID + "' �ͱ�����δ�ύ֮һɾ���ο�ID���ء�����ȷ������ť�������룬���˳��Ի���������ύ����֮�޸ġ�";
                        else
                            strText = "�ο�ID '" + issueitem.RefID + "' �ڱ������Ѿ����ڡ�����ȷ������ť�������롣";

                        MessageBox.Show(ForegroundWindow.Instance, strText);
                        goto REDO;
                    }

                    // �������ڼ�¼���вο�ID����
                    if (edit.AutoSearchDup == true)
                    {
                        // Debug.Assert(false, "");

                        string[] paths = null;
                        // �ο�ID���ء�
                        // parameters:
                        //      strOriginRecPath    ������¼��·����
                        //      paths   �������е�·��
                        // return:
                        //      -1  error
                        //      0   not dup
                        //      1   dup
                        nRet = SearchIssueRefIdDup(issueitem.RefID,
                            // this.BiblioRecPath,
                            issueitem.RecPath,
                            out paths,
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(ForegroundWindow.Instance, "�Բο�ID '" + issueitem.RefID + "' ���в��صĹ����з�������: " + strError);

                        else if (nRet == 1) // �����ظ�
                        {
                            string pathlist = String.Join(",", paths);

                            string strText = "�ο�ID '" + issueitem.RefID + "' �����ݿ��з����Ѿ���(���������ֵ�)�����ڼ�¼��ʹ�á�\r\n" + pathlist + "\r\n\r\n����ȷ������ť���±༭����Ϣ�����߸�����ʾ���ڼ�¼·����ȥ�޸������ڼ�¼��Ϣ��";
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

#if NO
        // �������б���
        // return:
        //      -2  �Ѿ�����(���ֳɹ�������ʧ��)
        //      -1  ����
        //      0   ����ɹ���û�д���;���
        int SaveIssues(EntityInfo[] issues,
            out string strError)
        {
            strError = "";

            bool bWarning = false;
            EntityInfo[] errorinfos = null;

            int nBatch = 100;
            for (int i = 0; i < (issues.Length / nBatch) + ((issues.Length % nBatch) != 0 ? 1 : 0); i++)
            {
                int nCurrentCount = Math.Min(nBatch, issues.Length - i * nBatch);
                EntityInfo[] current = EntityControl.GetPart(issues, i * nBatch, nCurrentCount);

                int nRet = SaveIssueRecords(this.BiblioRecPath,
                    current,
                    out errorinfos,
                    out strError);

                // �ѳ�����������Ҫ����״̬��������ֵ���ʾ���ڴ�
                if (RefreshOperResult(errorinfos) == true)
                    bWarning = true;

                if (nRet == -1)
                    return -1;
            }

            if (bWarning == true)
                return -2;
            return 0;
        }

        // �ύ�ڱ�������
        // return:
        //      -1  ����
        //      0   û�б�Ҫ����
        //      1   ����ɹ�
        public int DoSaveIssues()
        {
            // 2008/9/17
            if (this.Items == null)
                return 0;

            EnableControls(false);
            try
            {
                string strError = "";
                int nRet = 0;

                if (this.Items == null)
                {
                    /*
                    strError = "û������Ϣ��Ҫ����";
                    goto ERROR1;
                     * */
                    return 0;
                }

                // ���ȫ�������Parentֵ�Ƿ��ʺϱ���
                // return:
                //      -1  �д��󣬲��ʺϱ���
                //      0   û�д���
                nRet = this.Items.CheckParentIDForSave(out strError);
                if (nRet == -1)
                {
                    strError = "��������Ϣʧ�ܣ�ԭ��" + strError;
                    goto ERROR1;
                }

                EntityInfo[] issues = null;
                // EntityInfo[] errorinfos = null;

                // ������Ҫ�ύ������Ϣ����
                nRet = BuildSaveIssues(
                    out issues,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (issues == null || issues.Length == 0)
                    return 0; // û�б�Ҫ����

#if NO
                nRet = SaveIssueRecords(this.BiblioRecPath,
                    issues,
                    out errorinfos,
                    out strError);

                // �ѳ�����������Ҫ����״̬��������ֵ���ʾ���ڴ�
                RefreshOperResult(errorinfos);

                if (nRet == -1)
                {
                    goto ERROR1;
                }
#endif
                // return:
                //      -2  �Ѿ�����(���ֳɹ�������ʧ��)
                //      -1  ����
                //      0   ����ɹ���û�д���;���
                nRet = SaveIssues(issues, out strError);
                if (nRet == -2)
                    return -1;  // SaveIssues()�Ѿ�MessageBox()��ʾ����
                if (nRet == -1)
                    goto ERROR1;

                this.Changed = false;
                this.MainForm.StatusBarMessage = "����Ϣ �ύ / ���� �ɹ�";
                return 1;
            ERROR1:
                MessageBox.Show(ForegroundWindow.Instance, strError);
                return -1;
            }
            finally
            {
                EnableControls(true);
            }
        }

        // �������ڱ��������Ϣ����
        int BuildSaveIssues(
            out EntityInfo[] issues,
            out string strError)
        {
            strError = "";
            issues = null;
            int nRet = 0;

            Debug.Assert(this.Items != null, "");

            List<EntityInfo> issueArray = new List<EntityInfo>();

            foreach (IssueItem issueitem in this.Items)
            {
                // IssueItem issueitem = this.IssueItems[i];

                if (issueitem.ItemDisplayState == ItemDisplayState.Normal)
                    continue;

                EntityInfo info = new EntityInfo();

                // 2010/2/27 add
                if (String.IsNullOrEmpty(issueitem.RefID) == true)
                {
                    issueitem.RefID = Guid.NewGuid().ToString();
                    issueitem.RefreshListView();
                }

                info.RefID = issueitem.RefID;  // 2008/2/17

                string strXml = "";
                nRet = issueitem.BuildRecord(out strXml,
                        out strError);
                if (nRet == -1)
                    return -1;

                if (issueitem.ItemDisplayState == ItemDisplayState.New)
                {
                    info.Action = "new";
                    info.NewRecPath = "";
                    info.NewRecord = strXml;
                    info.NewTimestamp = null;
                }

                if (issueitem.ItemDisplayState == ItemDisplayState.Changed)
                {
                    info.Action = "change";

                    Debug.Assert(String.IsNullOrEmpty(issueitem.RecPath) == false, "issueitem.RecPath ����Ϊ��");

                    info.OldRecPath = issueitem.RecPath; // 2007/6/2
                    info.NewRecPath = issueitem.RecPath;

                    info.NewRecord = strXml;
                    info.NewTimestamp = null;

                    info.OldRecord = issueitem.OldRecord;
                    info.OldTimestamp = issueitem.Timestamp;
                }

                if (issueitem.ItemDisplayState == ItemDisplayState.Deleted)
                {
                    info.Action = "delete";
                    info.OldRecPath = issueitem.RecPath; // NewRecPath

                    info.NewRecord = "";
                    info.NewTimestamp = null;

                    info.OldRecord = issueitem.OldRecord;
                    info.OldTimestamp = issueitem.Timestamp;
                }

                issueArray.Add(info);
            }

            // ���Ƶ�Ŀ��
            issues = new EntityInfo[issueArray.Count];
            for (int i = 0; i < issueArray.Count; i++)
            {
                issues[i] = issueArray[i];
            }

            return 0;
        }

#endif

#if NO
        // �����ڼ�¼
        // ������ˢ�½���ͱ���
        int SaveIssueRecords(string strBiblioRecPath,
            EntityInfo[] issues,
            out EntityInfo[] errorinfos,
            out string strError)
        {
            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڱ�������Ϣ ...");
            Stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.SetIssues(
                    Stop,
                    strBiblioRecPath,
                    issues,
                    out errorinfos,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

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

#if NO
        // �ѱ�����Ϣ�еĳɹ������״̬�޸Ķ���
        // ���ҳ���ȥ��û�б���ġ�ɾ����IssueItem����ڴ���Ӿ��ϣ�
        // return:
        //      false   û�о���
        //      true    ���־���
        bool RefreshOperResult(EntityInfo[] errorinfos)
        {
            int nRet = 0;

            string strWarning = ""; // ������Ϣ

            if (errorinfos == null)
                return false;

            bool bOldChanged = this.Items.Changed;

            for (int i = 0; i < errorinfos.Length; i++)
            {
                /*
                XmlDocument dom = new XmlDocument();

                string strNewXml = errorinfos[i].NewRecord;
                string strOldXml = errorinfos[i].OldRecord;

                if (String.IsNullOrEmpty(strNewXml) == false)
                {
                    dom.LoadXml(strNewXml);
                }
                else if (String.IsNullOrEmpty(strOldXml) == false)
                {
                    dom.LoadXml(strOldXml);
                }
                else
                {
                    // �Ҳ���������������λ
                    Debug.Assert(false, "�Ҳ�����λ�ĳ�������");
                    // �Ƿ񵥶���ʾ����?
                    continue;
                }
                 * */

                IssueItem issueitem = null;

                string strError = "";

                if (String.IsNullOrEmpty(errorinfos[i].RefID) == true)
                {
                    MessageBox.Show(ForegroundWindow.Instance, "���������ص�EntityInfo�ṹ��RefIDΪ��");
                    return true;
                }

                /*
                string strPublishTime = "";
                // ��listview�ж�λ��dom����������
                // ˳�θ��� ��¼·�� -- ����ʱ�� ����λ
                // return:
                //      -1  error
                //      0   not found
                //      1   found
                nRet = LocateIssueItem(
                    errorinfos[i].OldRecPath,   // ԭ����NewRecPath
                    dom,
                    out issueitem,
                    out strPublishTime,
                    out strError);
                 * */
                nRet = LocateIssueItem(
                    errorinfos[i].RefID,
                    OrderControl.GetOneRecPath(errorinfos[i].NewRecPath, errorinfos[i].OldRecPath),
                    out issueitem,
                    out strError);
                if (nRet == -1 || nRet == 0)
                {
                    MessageBox.Show(ForegroundWindow.Instance, "��λ������Ϣ '" + errorinfos[i].ErrorInfo + "' �����еĹ����з�������:" + strError);
                    continue;
                }

                if (nRet == 0)
                {
                    MessageBox.Show(ForegroundWindow.Instance, "�޷���λ����ֵΪ " + i.ToString() + " �Ĵ�����Ϣ '" + errorinfos[i].ErrorInfo + "'");
                    continue;
                }

                string strLocationSummary = GetLocationSummary(
                    issueitem.PublishTime,  // strPublishTime, 
                    errorinfos[i].NewRecPath);

                // ������Ϣ����
                if (errorinfos[i].ErrorCode == ErrorCodeValue.NoError)
                {
                    if (errorinfos[i].Action == "new")
                    {
                        issueitem.OldRecord = errorinfos[i].NewRecord;
                        nRet = issueitem.ResetData(
                            errorinfos[i].NewRecPath,
                            errorinfos[i].NewRecord,
                            errorinfos[i].NewTimestamp,
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(ForegroundWindow.Instance, strError);
                    }
                    else if (errorinfos[i].Action == "change"
                        || errorinfos[i].Action == "move")
                    {
                        issueitem.OldRecord = errorinfos[i].NewRecord;

                        nRet = issueitem.ResetData(
                            errorinfos[i].NewRecPath,
                            errorinfos[i].NewRecord,
                            errorinfos[i].NewTimestamp,
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(ForegroundWindow.Instance, strError);

                        issueitem.ItemDisplayState = ItemDisplayState.Normal;
                    }

                    // ���ڱ�����ò������ڱ��ֵģ�Ҫ��listview������
                    if (String.IsNullOrEmpty(issueitem.RecPath) == false)
                    {
                        string strTempItemDbName = Global.GetDbName(issueitem.RecPath);
                        string strTempBiblioDbName = this.MainForm.GetBiblioDbNameFromIssueDbName(strTempItemDbName);

                        Debug.Assert(String.IsNullOrEmpty(strTempBiblioDbName) == false, "");
                        // TODO: ����Ҫ���汨��

                        string strTempBiblioRecPath = strTempBiblioDbName + "/" + issueitem.Parent;

                        if (strTempBiblioRecPath != this.BiblioRecPath)
                        {
                            this.Items.PhysicalDeleteItem(issueitem);
                            continue;
                        }
                    }

                    issueitem.Error = null;   // ������ʾ ��?

                    issueitem.Changed = false;
                    issueitem.RefreshListView();
                    continue;
                }

                // ������
                issueitem.Error = errorinfos[i];
                issueitem.RefreshListView();

                strWarning += strLocationSummary + "���ύ�ڱ�������з������� -- " + errorinfos[i].ErrorInfo + "\r\n";
            }


            // ����û�б���ģ���Щ�ɹ�ɾ����������ڴ���Ӿ���Ĩ��
            for (int i = 0; i < this.Items.Count; i++)
            {
                IssueItem issueitem = this.Items[i] as IssueItem;
                if (issueitem.ItemDisplayState == ItemDisplayState.Deleted)
                {
                    if (issueitem.ErrorInfo == "")
                    {
                        this.Items.PhysicalDeleteItem(issueitem);
                        i--; 
                    }
                }
            }

#if NO
            // �޸�Changed״̬
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = this.Items.Changed;
                this.ContentChanged(this, e1);
            }
#endif
            TriggerContentChanged(bOldChanged, this.Items.Changed);

            // 
            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strWarning += "\r\n��ע���޸�����Ϣ�������ύ����";
                MessageBox.Show(ForegroundWindow.Instance, strWarning);
                return true;
            }

            return false;
        }

#endif

#if NO
        // ��������ƺ�
        static string GetLocationSummary(
            string strPublishTime,
            string strRecPath)
        {
            if (String.IsNullOrEmpty(strPublishTime) == false)
                return "����ʱ��Ϊ '" + strPublishTime + "' ������";
            if (String.IsNullOrEmpty(strRecPath) == false)
                return "��¼·��Ϊ '" + strRecPath + "' ������";

            return "���κζ�λ��Ϣ������";
        }
#endif

        // ��������ƺ�
        internal override string GetLocationSummary(IssueItem bookitem)
        {
            string strPublishTime = bookitem.PublishTime;
            if (String.IsNullOrEmpty(strPublishTime) == false)
                return "����ʱ��Ϊ '" + strPublishTime + "' ������";

            string strRecPath = bookitem.RecPath;

            if (String.IsNullOrEmpty(strRecPath) == false)
                return "��¼·��Ϊ '" + strRecPath + "' ������";

            string strRefID = bookitem.RefID;
            if (String.IsNullOrEmpty(strRefID) == false)
                return "�ο�IDΪ '" + strRefID + "' ������";

            return "���κζ�λ��Ϣ������";
        }

#if NO
        // ��this.issueitems�ж�λ��strRefID����������
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int LocateIssueItem(
            string strRefID,
            string strRecPath,
            out IssueItem issueitem,
            out string strError)
        {
            strError = "";

            // �����ü�¼·������λ
            if (string.IsNullOrEmpty(strRecPath) == false
                && Global.IsAppendRecPath(strRecPath) == false)
            {
                issueitem = this.Items.GetItemByRecPath(strRecPath) as IssueItem;
                if (issueitem != null)
                    return 1;   // found
            }

            // Ȼ���òο�ID����λ
            issueitem = this.Items.GetItemByRefID(strRefID, null) as IssueItem;

            if (issueitem != null)
                return 1;   // found

            strError = "û���ҵ� ��¼·��Ϊ '" + strRecPath + "'������ �ο�ID Ϊ '" + strRefID + "' ��IssueItem����";
            return 0;
        }
#endif

#if NOOOOOOOOOOOOOOOOO
        // ��this.issueitems�ж�λ��dom����������
        // ˳�θ��� ��¼·�� -- ����ʱ�� ����λ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int LocateIssueItem(
            string strRecPath,
            XmlDocument dom,
            out IssueItem issueitem,
            out string strPublishTime,
            out string strError)
        {
            strError = "";
            issueitem = null;
            strPublishTime = "";

            // ��ǰ��ȡ, �Ա��κη���·��ʱ, �����Եõ���Щֵ
            strPublishTime = DomUtil.GetElementText(dom.DocumentElement, 
                "publishTime");

            if (String.IsNullOrEmpty(strRecPath) == false)
            {
                issueitem = this.issueitems.GetItemByRecPath(strRecPath);

                if (issueitem != null)
                    return 1;   // found

            }

            if (String.IsNullOrEmpty(strPublishTime) == false)
            {
                issueitem = this.issueitems.GetItemByPublishTime(
                    strPublishTime,
                    null);
                if (issueitem != null)
                    return 1;   // found

            }

            return 0;
        }
#endif

        private void ListView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            bool bHasBiblioLoaded = false;

            if (String.IsNullOrEmpty(this.BiblioRecPath) == false)
                bHasBiblioLoaded = true;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("�ǵ�(&A)");
            menuItem.Click += new System.EventHandler(this.menu_manageIssue_Click);
            if (bHasBiblioLoaded == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�޸�(&M)");
            menuItem.Click += new System.EventHandler(this.menu_modifyIssue_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("����(&N)");
            menuItem.Click += new System.EventHandler(this.menu_newIssue_Click);
            if (bHasBiblioLoaded == false)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("װ��(&B)");
            menuItem.Click += new System.EventHandler(this.menu_binding_Click);
            if (bHasBiblioLoaded == false)  // Ϊʲô?
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("���ս���ʱ��������������(&I)");
            menuItem.Click += new System.EventHandler(this.menu_toggleInputItemsBarcode_Click);
            if (this.InputItemsBarcode == true)
                menuItem.Checked = true;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("Ϊ�����յĲ����á��ӹ��С�״̬(&P)");
            menuItem.Click += new System.EventHandler(this.menu_toggleSetProcessingState_Click);
            if (this.SetProcessingState == true)
                menuItem.Checked = true;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("Ϊ�����յĲᴴ����ȡ��(&C)");
            menuItem.Click += new System.EventHandler(this.menu_toggleAutoCreateCallNumber_Click);
            if (this.CreateCallNumber == true)
                menuItem.Checked = true;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("װ���¿����ڴ�(&E)");
            menuItem.Click += new System.EventHandler(this.menu_loadToNewItemForm_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("װ���Ѿ��򿪵��ڴ�(&E)");
            menuItem.Click += new System.EventHandler(this.menu_loadToExistItemForm_Click);
            if (this.listView.SelectedItems.Count == 0
                || this.MainForm.GetTopChildWindow<ItemInfoForm>() == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�쿴�ڼ�¼�ļ����� (&K)");
            menuItem.Click += new System.EventHandler(this.menu_getKeys_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            /*

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            // cut ����
            menuItem = new MenuItem("����(&T)");
            menuItem.Click += new System.EventHandler(this.menu_cutEntity_Click);
            if (this.listView_items.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // copy ����
            menuItem = new MenuItem("����(&C)");
            menuItem.Click += new System.EventHandler(this.menu_copyEntity_Click);
            if (this.listView_items.SelectedItems.Count == 0)
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

            // �ı����
            menuItem = new MenuItem("�ı����(&B)");
            menuItem.Click += new System.EventHandler(this.menu_changeParent_Click);
            if (this.listView_items.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

             * */

            // ȫѡ
            menuItem = new MenuItem("ȫѡ(&A)");
            menuItem.Click += new System.EventHandler(this.menu_selectAll_Click);
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("���ɾ��(&D)");
            menuItem.Click += new System.EventHandler(this.menu_deleteIssue_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("����ɾ��(&U)");
            menuItem.Click += new System.EventHandler(this.menu_undoDeleteIssue_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView, new Point(e.X, e.Y));		
        }


        // ȫѡ
        void menu_selectAll_Click(object sender, EventArgs e)
        {
            ListViewUtil.SelectAllLines(this.listView);
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

            IssueItem cur = (IssueItem)this.ListView.SelectedItems[0].Tag;

            if (cur == null)
            {
                strError = "IssueItem == null";
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

            form.DbType = "issue";

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

            IssueItem cur = (IssueItem)this.ListView.SelectedItems[0].Tag;

            if (cur == null)
            {
                strError = "IssueItem == null";
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
                strError = "��ǰ��û���Ѿ��򿪵��ڴ�";
                goto ERROR1;
            }
            form.DbType = "issue";
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

        // �Զ�Ϊ�´����Ĳᴴ����ȡ��
        void menu_toggleAutoCreateCallNumber_Click(object sender, EventArgs e)
        {
            if (this.CreateCallNumber == true)
                this.CreateCallNumber = false;
            else
                this.CreateCallNumber = true;
        }


        // ���ս���ʱ��������������
        void menu_toggleInputItemsBarcode_Click(object sender, EventArgs e)
        {
            if (this.InputItemsBarcode == true)
                this.InputItemsBarcode = false;
            else
                this.InputItemsBarcode = true;
        }

        void menu_toggleSetProcessingState_Click(object sender, EventArgs e)
        {
            if (this.SetProcessingState == true)
                this.SetProcessingState = false;
            else
                this.SetProcessingState = true;
        }


        // װ��
        void menu_binding_Click(object sender, EventArgs e)
        {
            DoBinding("װ��", "binding");
        }

        // ����װ��
        // parameters:
        //      strLayoutMode   "auto" "accepting" "binding"��autoΪ�Զ�ģʽ��acceptingΪȫ����Ϊ�ǵ���bindingΪȫ����Ϊװ��
        void DoBinding(string strTitle,
            string strLayoutMode)
        {
            string strError = "";
            int nRet = 0;

            // ����ǰ��׼������
            if (this.PrepareAccept != null)
            {
                PrepareAcceptEventArgs e = new PrepareAcceptEventArgs();
                e.SourceRecPath = this.BiblioRecPath;
                this.PrepareAccept(this, e);
                if (String.IsNullOrEmpty(e.ErrorInfo) == false)
                {
                    strError = e.ErrorInfo;
                    goto ERROR1;
                }

                if (e.Cancel == true)
                    return;

                this.TargetRecPath = e.TargetRecPath;
                this.AcceptBatchNo = e.AcceptBatchNo;
                this.InputItemsBarcode = e.InputItemsBarcode;
                this.SetProcessingState = e.SetProcessingState;
                this.CreateCallNumber = e.CreateCallNumber;

                if (String.IsNullOrEmpty(e.WarningInfo) == false)
                {
                    DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
                        "����: \r\n" + e.WarningInfo + "\r\n\r\n������������?",
                            "IssueControl",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return;
                }
            }

            // 
            if (this.Items == null)
                this.Items = new IssueItemCollection();

            Debug.Assert(this.Items != null, "");
            bool bOldChanged = this.Items.Changed;
            bool bChanged = false;

            try
            {

                BindingForm dlg = new BindingForm();

                dlg.Text = strTitle;
                dlg.MainForm = this.MainForm;
                dlg.AppInfo = this.MainForm.AppInfo;
                dlg.BiblioDbName = Global.GetDbName(this.BiblioRecPath);
                if (this.PrepareAccept != null)
                {
                    dlg.AcceptBatchNoInputed = true;
                    // dlg.AcceptBatchNo = this.AcceptBatchNo;
                    this.MainForm.AppInfo.SetString(
                        "binding_form",
                        "accept_batchno",
                        this.AcceptBatchNo);
                }

                dlg.Operator = this.MainForm.DefaultUserName;
                if (this.Channel != null)
                    dlg.LibraryCodeList = this.Channel.LibraryCodeList;

                dlg.SetProcessingState = this.SetProcessingState;
                /*
                dlg.GetItemInfo -= new GetItemInfoEventHandler(dlg_GetItemInfo);
                dlg.GetItemInfo += new GetItemInfoEventHandler(dlg_GetItemInfo);
                 * */

                dlg.GetOrderInfo -= new GetOrderInfoEventHandler(dlg_GetOrderInfo);
                dlg.GetOrderInfo += new GetOrderInfoEventHandler(dlg_GetOrderInfo);

                dlg.GetValueTable -= new GetValueTableEventHandler(dlg_GetValueTable);
                dlg.GetValueTable += new GetValueTableEventHandler(dlg_GetValueTable);

                dlg.GenerateData -= new GenerateDataEventHandler(dlg_GenerateData);
                dlg.GenerateData += new GenerateDataEventHandler(dlg_GenerateData);

                // TODO: �����listview���б��ɾ���Ķ���Ҫ�����ύ�����ܽ���װ��

                // �㼯ȫ������Ϣ
                List<String> ItemXmls = new List<string>();
                List<string> all_item_refids = new List<string>();  // ���öԻ�����ǰ��ȫ�����refif����
                {
                    GetItemInfoEventArgs e = new GetItemInfoEventArgs();
                    e.BiblioRecPath = this.BiblioRecPath;
                    e.PublishTime = "*";
                    dlg_GetItemInfo(this, e);
                    for (int i = 0; i < e.ItemXmls.Count; i++)
                    {
                        XmlDocument dom = new XmlDocument();
                        try
                        {
                            dom.LoadXml(e.ItemXmls[i]);
                        }
                        catch (Exception ex)
                        {
                            strError = "XMLװ��DOMʱ����: " + ex.Message;
                            goto ERROR1;
                        }
                        string strRefID = DomUtil.GetElementText(dom.DocumentElement,
                            "refID");
                        if (String.IsNullOrEmpty(strRefID) == false)
                        {
                            all_item_refids.Add(strRefID);
                            ItemXmls.Add(e.ItemXmls[i]);
                        }
                        else
                        {
                            Debug.Assert(false, "");
                        }
                    }
                    ItemXmls = e.ItemXmls;  // ֱ����
                }

                // �㼯����Ϣ
                List<String> IssueXmls = new List<string>();
                List<string> all_issue_refids = new List<string>();  // ���öԻ�����ǰ��ȫ���ڵ�refif����
                foreach (IssueItem issue_item in this.Items)
                {
                    // IssueItem issue_item = this.IssueItems[i];

                    if (issue_item.ItemDisplayState == ItemDisplayState.Deleted)
                    {
                        strError = "��ǰ���ڱ��ɾ����������������ύ����󣬲���ʹ���ڹ�����";
                        goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(issue_item.RefID) == true)
                    {
                        issue_item.RefID = Guid.NewGuid().ToString();
                        issue_item.Changed = true;
                        issue_item.RefreshListView();
                        Debug.Assert(String.IsNullOrEmpty(issue_item.RefID) == false, "");
                    }

                    string strIssueXml = "";
                    nRet = issue_item.BuildRecord(
                        true,   // Ҫ��� Parent ��Ա
                        out strIssueXml,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    IssueXmls.Add(strIssueXml);
                    Debug.Assert(String.IsNullOrEmpty(issue_item.RefID) == false, "");
                    all_issue_refids.Add(issue_item.RefID);

                    /*
                // �����е�����Ϣ��ӳ���Ի�����
                    IssueBindingItem design_item =
                        dlg.AppendIssue(strIssueXml, out strError);
                    if (design_item == null)
                        goto ERROR1;

                    design_item.Tag = (object)item; // �������ӹ�ϵ
                     * */
                }

#if OLD_INITIAL

            // �����еĵĺ϶�������Ϣ��ӳ���Ի�����
            {
                GetItemInfoEventArgs e = new GetItemInfoEventArgs();
                e.BiblioRecPath = this.BiblioRecPath;
                e.PublishTime = "<range>";
                dlg_GetItemInfo(this, e);
                for (int i = 0; i < e.ItemXmls.Count; i++)
                {
                    ItemBindingItem design_item =
                        dlg.AppendBindItem(e.ItemXmls[i],
                        out strError);
                    if (design_item == null)
                        goto ERROR1;
                }
            }

            List<string> issued_item_refids = dlg.AllIssueMembersRefIds;
            List<string> none_issued_refids = new List<string>();
            for (int i = 0; i < all_refids.Count; i++)
            {
                string strRefID = all_refids[i];
                if (String.IsNullOrEmpty(strRefID) == true)
                    continue;
                if (issued_item_refids.IndexOf(strRefID) == -1)
                {
                    none_issued_refids.Add(strRefID);
                }
            }

            if (none_issued_refids.Count > 0)
            {
                GetItemInfoEventArgs e = new GetItemInfoEventArgs();
                e.BiblioRecPath = this.BiblioRecPath;
                // refid�ַ����ڲ������ж���
                e.PublishTime = "refids:" + StringUtil.MakePathList(none_issued_refids);
                dlg_GetItemInfo(this, e);

                nRet = dlg.AppendNoneIssueSingleItems(e.ItemXmls, out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            nRet = dlg.Initial(out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 1)  // ����
                MessageBox.Show(this, strError);
#endif
                dlg.LoadState();

                nRet = dlg.NewInitial(
                    strLayoutMode,
                    ItemXmls,
                    IssueXmls,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 1)  // ����
                    MessageBox.Show(this, strError);

                dlg.Changed = false;

                MainForm.AppInfo.LinkFormState(dlg,
                    "binding_form_state");
                dlg.ShowDialog(this);
                MainForm.AppInfo.UnlinkFormState(dlg);

                if (dlg.DialogResult != DialogResult.OK)
                    return;

                // *** ���ֶԲ���޸�
                {
                    // ����ȫ��refidװ����Ȼ��ȥ����Щ�����ڵģ�ʣ�µľ��Ǹ�ɾ������
                    List<string> deleting_bind_refids = new List<string>();
                    deleting_bind_refids.AddRange(all_item_refids);

                    List<string> Xmls = new List<string>();
                    List<ItemBindingItem> allitems = dlg.AllItems;  // ����
                    // �����������飬���� �޸�/����/ɾ�� ����
                    for (int i = 0; i < allitems.Count; i++)
                    {
                        ItemBindingItem bind_item = allitems[i];

                        deleting_bind_refids.Remove(bind_item.RefID);

                        if (bind_item.Changed == true)
                        {
                            // ����refid�ҵ��������󣬲������޸�
                            // ���û���ҵ����򴴽�֮
                            Xmls.Add(bind_item.Xml);
                        }
                    }

                    if (this.ChangeItem != null)
                    {
                        string strWarning = "";
                        // ���ݲ�XML���ݣ��Զ����������޸Ĳ����
                        // return:
                        //      -1  error
                        //      0   û���޸�
                        //      1   �޸���
                        nRet = ChangeItems(Xmls,
                            out strWarning,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 1)
                            bChanged = true;

                        if (String.IsNullOrEmpty(strWarning) == false)
                            MessageBox.Show(this, strWarning);

                        // ɾ��ʵ������
                        if (deleting_bind_refids.Count != 0)
                        {
                            List<string> deleted_ids = null;
                            nRet = DeleteItemRecords(deleting_bind_refids,
                                out deleted_ids,
                                out strError);
                            if (nRet == -1)
                            {
                                /*
                                this.issueitems.Clear();
                                this.issueitems.AddRange(save_items);
                                // ˢ����ʾ
                                this.issueitems.AddToListView(this.ListView);
                                 * */
                                goto ERROR1;
                            }
                            if (deleted_ids.Count > 0)
                                bChanged = true;

                        }
                    }
                }

                // *** ���ֶ��ڵ��޸�
                {
                    // ����ȫ��refidװ����Ȼ��ȥ����Щ�����ڵģ�ʣ�µľ��Ǹ�ɾ������
                    List<string> deleting_issue_refids = new List<string>();
                    deleting_issue_refids.AddRange(all_issue_refids);

                    List<string> Xmls = new List<string>(); // Ҫ���������޸ĵ��ڼ�¼

                    // �����������飬����deleting_issue_refids��Xmls
                    for (int i = 0; i < dlg.Issues.Count; i++)
                    {
                        IssueBindingItem issue_item = dlg.Issues[i];

                        if (issue_item.Virtual == true)
                            continue;

                        deleting_issue_refids.Remove(issue_item.RefID);

                        if (issue_item.Changed == true)
                        {
                            // ����refid�ҵ��������󣬲������޸�
                            // ���û���ҵ����򴴽�֮
                            Xmls.Add(issue_item.Xml);
                        }
                    }

                    // ���ݲ�XML���ݣ��Զ����������޸��ڶ���
                    // return:
                    //      -1  error
                    //      0   succeed
                    nRet = ChangeIssues(Xmls,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // ɾ���ڶ���
                    if (deleting_issue_refids.Count != 0)
                    {
                        List<string> deleted_ids = null;
                        nRet = DeleteIssueRecords(deleting_issue_refids,
                            out deleted_ids,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (deleted_ids.Count > 0)
                            bChanged = true;
                    }
                }
            }
            finally
            {
                if (this.Items.Changed == true)
                    bChanged = true;

#if NO
                if (this.ContentChanged != null
                    && bChanged == true)
                {
                    ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                    e1.OldChanged = bOldChanged;
                    e1.CurrentChanged = true;
                    this.ContentChanged(this, e1);
                }
#endif
                TriggerContentChanged(bOldChanged, true);
            }


            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void dlg_GenerateData(object sender, GenerateDataEventArgs e)
        {
#if NO
            if (this.GenerateData != null)
            {
                this.GenerateData(sender, e);
            }
            else
            {
                MessageBox.Show(this, "IssueControlû�йҽ�GenerateData�¼�");
            }
#endif
            DoGenerateData(sender, e);
        }

        // ���ݲ�XML���ݣ����������޸Ĳ����
        // return:
        //      -1  error
        //      0   û���޸�
        //      1   �޸���
        int ChangeItems(List<string> Xmls,
            out string strWarning,
            out string strError)
        {
            strError = "";
            strWarning = "";

            // int nRet = 0;
            bool bChanged = false;

            if (this.ChangeItem == null)
            {
                strError = "ChangeItem�¼���δ�ҽ�";
                return -1;
            }

            ChangeItemEventArgs data_container = new ChangeItemEventArgs();
            data_container.InputItemBarcode = this.InputItemsBarcode;
            data_container.CreateCallNumber = this.CreateCallNumber;
            data_container.SeriesMode = true;
            for (int i = 0; i < Xmls.Count; i++)
            {
                string strXml = Xmls[i];

                XmlDocument item_dom = new XmlDocument();
                try
                {
                    item_dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "���¼ ��XMLװ��DOMʱ����: " + ex.Message;
                    return -1;
                }

                string strRefID = DomUtil.GetElementText(item_dom.DocumentElement,
                    "refID");

                ChangeItemData e = new ChangeItemData();

                e.Action = "neworchange";
                e.RefID = strRefID;
                e.Xml = strXml;

                data_container.DataList.Add(e);

                bChanged = true;
            } // end of for i

            if (data_container.DataList != null
                && data_container.DataList.Count > 0)
            {
                // �����ⲿ�ҽӵ��¼�
                this.ChangeItem(this, data_container);
                string strErrorText = "";

                if (String.IsNullOrEmpty(data_container.ErrorInfo) == false)
                {
                    strError = data_container.ErrorInfo;
                    return -1;
                }

                for (int i = 0; i < data_container.DataList.Count; i++)
                {
                    ChangeItemData data = data_container.DataList[i];
                    if (String.IsNullOrEmpty(data.ErrorInfo) == false)
                    {
                        strErrorText += data.ErrorInfo;
                    }
                    if (String.IsNullOrEmpty(data.WarningInfo) == false)
                    {
                        strWarning += data.WarningInfo;
                    }
                }

                if (String.IsNullOrEmpty(strErrorText) == false)
                {
                    strError = strErrorText;
                    return -1;
                }

                if (String.IsNullOrEmpty(data_container.WarningInfo) == false)
                    strWarning += data_container.WarningInfo;
            }

            if (bChanged == true)
                return 1;

            return 0;
        }

        // 
        // return:
        //      -1  ����
        //      0   û���Ƴ���
        //      >0  �Ƴ��ĸ���
        /// <summary>
        /// �Ƴ�publishtime�ظ�������
        /// </summary>
        /// <param name="Xmls">XML �ַ������ϡ���������л����ɾ������ʱ���ظ�����Щ�ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û��ɾ����; >0: ɾ���ĵĸ���</returns>
        public int RemoveDupPublishTime(ref List<string> Xmls,
            out string strError)
        {
            strError = "";
            // int nRet = 0;

            if (this.Items == null)
                this.Items = new IssueItemCollection();

            int nRemovedCount = 0;
            for (int i = 0; i < Xmls.Count; i++)
            {
                string strXml = Xmls[i];

                XmlDocument issue_dom = new XmlDocument();
                try
                {
                    issue_dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "�ڼ�¼ ��XMLװ��DOMʱ����: " + ex.Message;
                    return -1;
                }

                string strPublishTime = DomUtil.GetElementText(issue_dom.DocumentElement,
                    "publishTime");
                if (String.IsNullOrEmpty(strPublishTime) == true)
                {
                    Debug.Assert(String.IsNullOrEmpty(strPublishTime) == false, "");
                    strError = "���(��0��ʼ����)Ϊ " + (i+nRemovedCount).ToString() + " ���ڼ�¼XML��û��<publishTime>Ԫ��...";
                    return -1;
                }

                // �����Ƿ����Ѿ����ڵļ�¼
                IssueItem exist_item = this.Items.GetItemByPublishTime(strPublishTime, null);
                if (exist_item != null)
                {
                    Xmls.RemoveAt(i);
                    i--;
                    nRemovedCount++;

                    if (String.IsNullOrEmpty(strError) == false)
                        strError += ",";
                    strError += strPublishTime;
                }


            } // end of for i

            return nRemovedCount;
        }

        // 
        // TODO: ѭ���г���ʱ��Ҫ��������ȥ������ٱ���
        // return:
        //      -1  error
        //      0   succeed
        /// <summary>
        /// ������ XML ���ݣ����������޸��ڶ���
        /// </summary>
        /// <param name="Xmls">XML �ַ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-1: ����</para>
        /// <para>0 : �ɹ�</para>
        /// </returns>
        public int ChangeIssues(List<string> Xmls,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (this.Items == null)
                this.Items = new IssueItemCollection();

            for (int i = 0; i < Xmls.Count; i++)
            {
                string strXml = Xmls[i];

                XmlDocument issue_dom = new XmlDocument();
                try
                {
                    issue_dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "�ڼ�¼ ��XMLװ��DOMʱ����: " + ex.Message;
                    return -1;
                }

                string strRefID = DomUtil.GetElementText(issue_dom.DocumentElement,
                    "refID");
                if (String.IsNullOrEmpty(strRefID) == true)
                {
                    Debug.Assert(String.IsNullOrEmpty(strRefID) == false, "");
                    strError = "���Ϊ "+i.ToString()+" ���ڼ�¼XML��û��<refID>Ԫ��...";
                    return -1;
                }

                string strAction = "";

                // �����Ƿ����Ѿ����ڵļ�¼
                IssueItem exist_item = this.Items.GetItemByRefID(strRefID, null) as IssueItem;
                if (exist_item != null)
                    strAction = "change";
                else
                    strAction = "new";

                /*
                string strOperName = "";
                if (strAction == "new")
                    strOperName = "����";
                else if (strAction == "change")
                    strOperName = "�޸�";
                else if (strAction == "delete")
                    strOperName = "ɾ��";
                 * */

                IssueItem issue_item = null;

                if (strAction == "new")
                {
                    issue_item = new IssueItem();

                    // ����ȱʡֵ?
                }
                else
                    issue_item = exist_item;

                // Ϊ�˱���BuildRecord()����
                issue_item.Parent = Global.GetRecordID(this.BiblioRecPath);

                if (exist_item == null)
                {
                    nRet = issue_item.SetData(issue_item.RecPath,
                        issue_dom.OuterXml,
                        null,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (String.IsNullOrEmpty(issue_item.Parent) == true)
                    {
                        // �´����ļ�¼����û��.Parent���ݣ���Ҫ����
                        Debug.Assert(String.IsNullOrEmpty(this.BiblioRecPath) == false, "");
                        string strID = Global.GetRecordID(this.BiblioRecPath);
                        issue_item.Parent = strID;
                    }
                }
                else
                {
                    // ע: OldRecord/Timestamp��ϣ�����ı� 2010/3/22
                    string strOldXml = issue_item.OldRecord;

#if DEBUG
                    if (issue_item.ItemDisplayState != ItemDisplayState.New)
                    {
                        Debug.Assert(String.IsNullOrEmpty(issue_item.RecPath) == false, "");
                    }
#endif

                    nRet = issue_item.SetData(issue_item.RecPath,
                        issue_dom.OuterXml,
                        issue_item.Timestamp, 
                        out strError);
                    if (nRet == -1)
                        return -1;
                    issue_item.OldRecord = strOldXml;
                }

                if (exist_item == null)
                {
                    this.Items.Add(issue_item);
                    issue_item.ItemDisplayState = ItemDisplayState.New;
                    issue_item.AddToListView(this.listView);
                }
                else
                {
                    // �´������в��ܸ�Ϊchanged��
                    if (issue_item.ItemDisplayState != ItemDisplayState.New)
                        issue_item.ItemDisplayState = ItemDisplayState.Changed;
                }

                issue_item.Changed = true;    // ���򡰱��桱��ť����Enabled

                // ���ոռ�����������ɼ���Χ
                issue_item.HilightListViewItem(true);
                issue_item.RefreshListView(); // 2009/12/18 add

            } // end of for i

            return 0;
        }

        int DeleteIssueRecords(List<string> deleting_issue_refids,
            out List<string> deleted_ids,
            out string strError)
        {
            deleted_ids = new List<string>();
            strError = "";
            int nRet = 0;

            if (this.Items == null)
                this.Items = new IssueItemCollection();

            for (int i = 0; i < deleting_issue_refids.Count; i++)
            {
                string strRefID = deleting_issue_refids[i];
                Debug.Assert(String.IsNullOrEmpty(strRefID) == false, "");

                // �����Ƿ����Ѿ����ڵļ�¼
                IssueItem exist_item = this.Items.GetItemByRefID(strRefID, null) as IssueItem;
                if (exist_item == null)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                // ���ɾ������
                // return:
                //      0   ��Ϊ�в���Ϣ��δ�ܱ��ɾ��
                //      1   �ɹ�ɾ��
                nRet = MaskDeleteItem(exist_item,
                         this.m_bRemoveDeletedItem);
                if (nRet == 0)
                {
                    strError = "refidΪ '" + strRefID + "' ����������Ϊ�����в���Ϣ���޷�����ɾ��";
                    return -1;
                }

                deleted_ids.Add(strRefID);

            } // end of for i

            return 0;
        }
        
        // �ǵ�
        void menu_manageIssue_Click(object sender, EventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                // DoIssueManage();
                DoBinding("�ǵ�", "accepting");
            }
            else
                DoBinding("�ǵ�", "auto");    // 
        }

        void DoIssueManage()
        {
            string strError = "";
            int nRet = 0;

            // ����ǰ��׼������
            if (this.PrepareAccept != null)
            {
                PrepareAcceptEventArgs e = new PrepareAcceptEventArgs();
                e.SourceRecPath = this.BiblioRecPath;
                this.PrepareAccept(this, e);
                if (String.IsNullOrEmpty(e.ErrorInfo) == false)
                {
                    strError = e.ErrorInfo;
                    goto ERROR1;
                }

                if (e.Cancel == true)
                    return;

                this.TargetRecPath = e.TargetRecPath;
                this.AcceptBatchNo = e.AcceptBatchNo;
                this.InputItemsBarcode = e.InputItemsBarcode;
                this.SetProcessingState = e.SetProcessingState;
                this.CreateCallNumber = e.CreateCallNumber;

                if (String.IsNullOrEmpty(e.WarningInfo) == false)
                {
                    DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
                        "����: \r\n" + e.WarningInfo + "\r\n\r\n������������?",
                            "IssueControl",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                        return;
                }
            }


            // 
            if (this.Items == null)
                this.Items = new IssueItemCollection();

            Debug.Assert(this.Items != null, "");

            IssueManageForm dlg = new IssueManageForm();
            dlg.MainForm = this.MainForm;
            // 2009/2/15
            dlg.BiblioDbName = Global.GetDbName(this.BiblioRecPath);

            // �����е�����Ϣ��ӳ���Ի�����
            foreach (IssueItem item in this.Items)
            {
                // IssueItem item = this.IssueItems[i];

                if (item.ItemDisplayState == ItemDisplayState.Deleted)
                {
                    strError = "��ǰ���ڱ��ɾ����������������ύ����󣬲���ʹ���ڹ�����";
                    goto ERROR1;
                }

                string strIssueXml = "";
                nRet = item.BuildRecord(
                    true,   // Ҫ��� Parent ��Ա
                    out strIssueXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                IssueManageItem design_item =
                    dlg.AppendNewItem(strIssueXml, out strError);
                if (design_item == null)
                    goto ERROR1;

                design_item.Tag = (object)item; // �������ӹ�ϵ
            }

            dlg.Changed = false;

            dlg.GetOrderInfo -= new GetOrderInfoEventHandler(dlg_GetOrderInfo);
            dlg.GetOrderInfo += new GetOrderInfoEventHandler(dlg_GetOrderInfo);

            dlg.GetItemInfo -= new GetItemInfoEventHandler(dlg_GetItemInfo);
            dlg.GetItemInfo += new GetItemInfoEventHandler(dlg_GetItemInfo);

            dlg.GetValueTable -= new GetValueTableEventHandler(dlg_GetValueTable);
            dlg.GetValueTable += new GetValueTableEventHandler(dlg_GetValueTable);

            /*
            dlg.GenerateEntity -= new GenerateEntityEventHandler(dlg_GenerateEntity);
            dlg.GenerateEntity += new GenerateEntityEventHandler(dlg_GenerateEntity);
             * */

            MainForm.AppInfo.LinkFormState(dlg,
                "issue_manage_form_state");

            dlg.ShowDialog(this);

            MainForm.AppInfo.UnlinkFormState(dlg);


            if (dlg.DialogResult != DialogResult.OK)
                return;

            bool bOldChanged = this.Items.Changed;

            // ���漯���ڵ�����Ԫ��
            IssueItemCollection save_items = new IssueItemCollection();
            save_items.AddRange(this.Items);

            IssueItemCollection mask_delete_items = new IssueItemCollection();
            mask_delete_items.AddRange(this.Items);

            // ��������ڵ�����Ԫ��
            this.Items.Clear();

            List<IssueItem> changed_issueitems = new List<IssueItem>();

            List<IssueManageItem> items = dlg.Items;
            for (int i = 0; i < items.Count; i++)
            {
                IssueManageItem design_item = items[i];

                if (design_item.Changed == false)
                {
                    // ��ԭ
                    IssueItem issue_item = (IssueItem)design_item.Tag;
                    Debug.Assert(issue_item != null, "");
                    this.Items.Add(issue_item);
                    issue_item.AddToListView(this.listView);

                    mask_delete_items.Remove(issue_item);
                    continue;
                }

                IssueItem issueitem = new IssueItem();

                // ����ȫ�´�������
                if (design_item.Tag == null)
                {
                    // ��ʹ������׷�ӱ���
                    issueitem.RecPath = "";

                    issueitem.ItemDisplayState = ItemDisplayState.New;
                }
                else
                {
                    // ��ԭrecpath
                    IssueItem issue_item = (IssueItem)design_item.Tag;

                    // ��ԭһЩ��Ҫ��ֵ
                    issueitem.RecPath = issue_item.RecPath;
                    issueitem.Timestamp = issue_item.Timestamp;
                    issueitem.OldRecord = issue_item.OldRecord;

                    // issueitem.ItemDisplayState = ItemDisplayState.Changed;

                    // 2009/1/6 changed
                    issueitem.ItemDisplayState = issue_item.ItemDisplayState;

                    if (issueitem.ItemDisplayState != ItemDisplayState.New)
                    {
                        // ע: ״̬ΪNew�Ĳ����޸�ΪChanged������һ������
                        issueitem.ItemDisplayState = ItemDisplayState.Changed;
                    }

                    mask_delete_items.Remove(issue_item);
                }

                issueitem.Parent = Global.GetRecordID(this.BiblioRecPath);

                issueitem.PublishTime = design_item.PublishTime;
                issueitem.Issue = design_item.Issue;
                issueitem.Volume = design_item.Volume;
                issueitem.Zong = design_item.Zong;
                issueitem.OrderInfo = design_item.OrderInfo;
                issueitem.RefID = design_item.RefID;    // 2010/2/27 add

                changed_issueitems.Add(issueitem);

                // �ȼ����б�
                this.Items.Add(issueitem);

                issueitem.AddToListView(this.listView);
                issueitem.HilightListViewItem(true);

                issueitem.Changed = true;    // ��Ϊ�����������������ζ����޸Ĺ����������Ա��⼯����ֻ��һ�����������ʱ�򣬼��ϵ�changedֵ����
            }

            // ���ɾ��ĳЩԪ��
            foreach (IssueItem issue_item in mask_delete_items)
            {
                // IssueItem issue_item = mask_delete_items[i];

                // 2009/2/10
                bool bFound = false;
                // �����û�г��������ظ���״̬Ϊ������������?
                foreach (IssueItem temp in this.Items)
                {
                    // IssueItem temp = this.IssueItems[j];
                    if (issue_item.PublishTime == temp.PublishTime
                        && temp.ItemDisplayState == ItemDisplayState.New)
                    {
                        temp.ItemDisplayState = ItemDisplayState.Changed;
                        temp.Timestamp = issue_item.Timestamp;
                        temp.OldRecord = issue_item.OldRecord;
                        temp.RecPath = issue_item.RecPath;
                        temp.RefreshListView();
                        bFound = true;
                        break;
                    }
                }
                if (bFound == true)
                    continue;

                // �ȼ����б�
                this.Items.Add(issue_item);
                issue_item.AddToListView(this.listView);

                nRet = MaskDeleteItem(issue_item,
                        m_bRemoveDeletedItem);


            }

            if (this.GenerateEntity != null)
            {
                // ɾ��ʵ������
                if (dlg.DeletingIds.Count != 0)
                {
                    List<string> deleted_ids = null;
                    nRet = DeleteItemRecords(dlg.DeletingIds,
                        out deleted_ids,
                        out strError);
                    if (nRet == -1)
                    {
                        this.Items.Clear();
                        this.Items.AddRange(save_items);
                        // ˢ����ʾ
                        this.Items.AddToListView(this.listView);
                        goto ERROR1;
                    }
                }

                // �����������ݣ��Զ�����ʵ������
                nRet = GenerateEntities(changed_issueitems,
                    out strError);
                if (nRet == -1)
                {
                    // ��������ʵ���¼������ʵ���¼����ʧ�ܺ�Ӧ��ԭ�ڼ�¼���޸�ǰ״̬
                    this.Items.Clear();
                    this.Items.AddRange(save_items);
                    // ˢ����ʾ
                    this.Items.AddToListView(this.listView);
                    goto ERROR1;
                }
            }

#if NO
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = true;
                this.ContentChanged(this, e1);
            }
#endif
            TriggerContentChanged(bOldChanged, true);

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        void dlg_GetValueTable(object sender, GetValueTableEventArgs e)
        {
            string strError = "";
            string[] values = null;
            int nRet = MainForm.GetValueTable(e.TableName,
                e.DbName,
                out values,
                out strError);
            if (nRet == -1)
                MessageBox.Show(ForegroundWindow.Instance, strError);
            e.values = values;
        }

        /*
        void dlg_GenerateEntity(object sender, GenerateEntityEventArgs e)
        {
            if (this.GenerateEntity != null)
                this.GenerateEntity(sender, e);
        }*/

        // parameters:
        //      deleted_ids �Ѿ��ɹ�ɾ����id
        int DeleteItemRecords(List<string> ids,
            out List<string> deleted_ids,
            out string strError)
        {
            strError = "";
            deleted_ids = new List<string>();

            Debug.Assert(this.GenerateEntity != null, "");

            GenerateEntityEventArgs data_container = new GenerateEntityEventArgs();
            // data_container.InputItemBarcode = this.InputItemsBarcode;
            data_container.SeriesMode = true;

            for (int i = 0; i < ids.Count; i++)
            {
                GenerateEntityData e = new GenerateEntityData();

                e.Action = "delete";
                e.RefID = ids[i];
                e.Xml = "";

                data_container.DataList.Add(e);
            }

            if (data_container.DataList != null
    && data_container.DataList.Count > 0)
            {
                // �����ⲿ�ҽӵ��¼�
                this.GenerateEntity(this, data_container);
                string strErrorText = "";

                if (String.IsNullOrEmpty(data_container.ErrorInfo) == false)
                {
                    strError = data_container.ErrorInfo;
                    return -1;
                }

                for (int i = 0; i < data_container.DataList.Count; i++)
                {
                    GenerateEntityData data = data_container.DataList[i];
                    if (String.IsNullOrEmpty(data.ErrorInfo) == false)
                    {
                        strErrorText += data.ErrorInfo;
                    }
                    else
                        deleted_ids.Add(data.RefID);
                }

                if (String.IsNullOrEmpty(strErrorText) == false)
                {
                    strError = strErrorText;
                    return -1;
                }
            }

            return 0;
        }


        // �����������ݣ��Զ�����ʵ������
        // return:
        //      -1  error
        //      0   succeed
        int GenerateEntities(List<IssueItem> issueitems,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (this.GenerateEntity == null)
            {
                strError = "GenerateEntity�¼���δ�ҽ�";
                return -1;
            }

            GenerateEntityEventArgs data_container = new GenerateEntityEventArgs();
            data_container.InputItemBarcode = this.InputItemsBarcode;
            data_container.SetProcessingState = this.SetProcessingState;
            data_container.CreateCallNumber = this.CreateCallNumber;
            data_container.SeriesMode = true;

            for (int i = 0; i < issueitems.Count; i++)
            {
                IssueItem issue_item = issueitems[i];

                if (String.IsNullOrEmpty(issue_item.OrderInfo) == true)
                    continue;

                string strIssueXml = "";
                nRet = issue_item.BuildRecord(
                    true,   // Ҫ��� Parent ��Ա
                    out strIssueXml,
                    out strError);
                if (nRet == -1)
                    return -1;

                XmlDocument issue_dom = new XmlDocument();
                try
                {
                    issue_dom.LoadXml(strIssueXml);
                }
                catch (Exception ex)
                {
                    strError = "�ڼ�¼ '" + issue_item.PublishTime + "' ��XMLװ��DOMʱ����: " + ex.Message;
                    return -1;
                }

                bool bOrderChanged = false;

                // ���һ������ÿ��������¼��ѭ��
                XmlNodeList order_nodes = issue_dom.DocumentElement.SelectNodes("orderInfo/*");
                for (int j = 0; j < order_nodes.Count; j++)
                {
                    XmlNode order_node = order_nodes[j];

                    string strDistribute = DomUtil.GetElementText(order_node, "distribute");

                    LocationCollection locations = new LocationCollection();
                    nRet = locations.Build(strDistribute,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    bool bLocationChanged = false;

                    // Ϊÿ���ݲصص㴴��һ��ʵ���¼
                    for (int k = 0; k < locations.Count; k++)
                    {
                        Location location = locations[k];

                        // TODO: Ҫע�����㣺1) �Ѿ����չ����У��������*��refid���Ƿ�Ҫ�ٴδ����᣿����Ч����ʶ�������õ�ʱ���кô�
                        // 2) û���������ʱ���ǲ���Ҫ������������ѭ���ˣ����һ��

                        // �Ѿ����������������
                        if (location.RefID != "*")
                            continue;

                        GenerateEntityData e = new GenerateEntityData();

                        e.Action = "new";
                        e.RefID = Guid.NewGuid().ToString();
                        location.RefID = e.RefID;   // �޸ĵ��ݲصص��ַ�����

                        bLocationChanged = true;

                        XmlDocument dom = new XmlDocument();
                        dom.LoadXml("<root />");

                        // 2009/10/19
                        // ״̬
                        if (this.SetProcessingState == true)
                        {
                            // �������ӹ��С�ֵ
                            string strOldState = DomUtil.GetElementText(dom.DocumentElement,
                                "state");
                            DomUtil.SetElementText(dom.DocumentElement,
                                "state", Global.AddStateProcessing(strOldState));
                        }

                        // seller
                        string strSeller = DomUtil.GetElementText(order_node,
                            "seller");

                        // seller���ǵ���ֵ
                        DomUtil.SetElementText(dom.DocumentElement,
                            "seller", strSeller);

                        string strOldValue = "";
                        string strNewValue = "";

                        // source
                        string strSource = DomUtil.GetElementText(order_node,
                            "source");


                        // source�ڲ�����ֵ
                        // ���� "old[new]" �ڵ�����ֵ
                        OrderDesignControl.ParseOldNewValue(strSource,
                            out strOldValue,
                            out strNewValue);
                        DomUtil.SetElementText(dom.DocumentElement,
                            "source", strNewValue);

                        // price
                        string strPrice = DomUtil.GetElementText(order_node,
                            "price");

                        // price�ڲ�����ֵ
                        OrderDesignControl.ParseOldNewValue(strPrice,
                            out strOldValue,
                            out strNewValue);
                        DomUtil.SetElementText(dom.DocumentElement,
                            "price", strNewValue);

                        // location
                        string strLocation = location.Name;
                        DomUtil.SetElementText(dom.DocumentElement,
                            "location", strLocation);

                        // publishTime
                        DomUtil.SetElementText(dom.DocumentElement,
                            "publishTime", issue_item.PublishTime);

                        // volume ��ʵ�ǵ����ںš����ںš������һ���һ���ַ���
                        string strVolume = VolumeInfo.BuildItemVolumeString(
                            IssueUtil.GetYearPart(issue_item.PublishTime),
                            issue_item.Issue,
                            issue_item.Zong,
                            issue_item.Volume);
                        DomUtil.SetElementText(dom.DocumentElement,
                            "volume", strVolume);

                        // ���κ�
                        DomUtil.SetElementText(dom.DocumentElement,
                            "batchNo", this.AcceptBatchNo);

                        e.Xml = dom.OuterXml;

                        data_container.DataList.Add(e);
                    }

                    // �ݲصص��ַ����б仯����Ҫ��ӳ������
                    if (bLocationChanged == true)
                    {
                        strDistribute = locations.ToString();
                        DomUtil.SetElementText(order_node,
                            "distribute", strDistribute);
                        bOrderChanged = true;
                        // order_item.RefreshListView();
                    }

                } // end of for j

                if (bOrderChanged == true)
                {
                    issue_item.OrderInfo = DomUtil.GetElementInnerXml(issue_dom.DocumentElement,
                        "orderInfo");
                    issue_item.Changed = true;
                    issue_item.RefreshListView();
                }

            } // end of for i

            if (data_container.DataList != null
                && data_container.DataList.Count > 0)
            {
                // �����ⲿ�ҽӵ��¼�
                this.GenerateEntity(this, data_container);
                string strErrorText = "";

                if (String.IsNullOrEmpty(data_container.ErrorInfo) == false)
                {
                    strError = data_container.ErrorInfo;
                    return -1;
                }

                for (int i = 0; i < data_container.DataList.Count; i++)
                {
                    GenerateEntityData data = data_container.DataList[i];
                    if (String.IsNullOrEmpty(data.ErrorInfo) == false)
                    {
                        strErrorText += data.ErrorInfo;
                    }
                }

                if (String.IsNullOrEmpty(strErrorText) == false)
                {
                    strError = strErrorText;
                    return -1;
                }
            }

            return 0;
        }

        void dlg_GetItemInfo(object sender, GetItemInfoEventArgs e)
        {
            if (this.GetItemInfo != null)
                this.GetItemInfo(sender, e);
        }

        void dlg_GetOrderInfo(object sender, GetOrderInfoEventArgs e)
        {
            if (this.GetOrderInfo != null)
                this.GetOrderInfo(sender, e);
        }

        void menu_modifyIssue_Click(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "��δѡ��Ҫ�༭������");
                return;
            }
            IssueItem issueitem = (IssueItem)this.listView.SelectedItems[0].Tag;

            ModifyIssue(issueitem);
        }

        void menu_newIssue_Click(object sender, EventArgs e)
        {
            DoNewIssue();
        }


        // ����ɾ��һ��������
        void menu_undoDeleteIssue_Click(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "��δѡ��Ҫ����ɾ��������");
                return;
            }

            this.EnableControls(false);

            try
            {
                bool bOldChanged = this.Items.Changed;

                // ʵ��Undo
                List<ListViewItem> selectedItems = new List<ListViewItem>();
                foreach (ListViewItem item in this.listView.SelectedItems)
                {
                    selectedItems.Add(item);
                }

                string strNotUndoList = "";
                int nUndoCount = 0;
                foreach (ListViewItem item in selectedItems)
                {
                    IssueItem issueitem = (IssueItem)item.Tag;

                    bool bRet = this.Items.UndoMaskDeleteItem(issueitem);

                    if (bRet == false)
                    {
                        if (strNotUndoList != "")
                            strNotUndoList += ",";
                        strNotUndoList += issueitem.PublishTime;
                        continue;
                    }

                    nUndoCount++;
                }

                string strText = "";

                if (strNotUndoList != "")
                    strText += "����ʱ��Ϊ '" + strNotUndoList + "' ��������ǰ��δ�����ɾ����, ��������̸���ϳ���ɾ����\r\n\r\n";

                strText += "������ɾ�� " + nUndoCount.ToString() + " �";
                MessageBox.Show(ForegroundWindow.Instance, strText);

#if NO
                if (this.ContentChanged != null
    && bOldChanged != this.Items.Changed)
                {
                    ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                    e1.OldChanged = bOldChanged;
                    e1.CurrentChanged = this.Items.Changed;
                    this.ContentChanged(this, e1);
                }
#endif
                TriggerContentChanged(bOldChanged, this.Items.Changed);
            }
            finally
            {
                this.EnableControls(true);
            }
        }

        // ɾ��һ��������
        void menu_deleteIssue_Click(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "��δѡ��Ҫ���ɾ��������");
                return;
            }

            string strPublishTimeList = "";
            for (int i = 0; i < this.listView.SelectedItems.Count; i++)
            {
                if (i > 20)
                {
                    strPublishTimeList += "...(�� " + this.listView.SelectedItems.Count.ToString() + " ��)";
                    break;
                }
                string strPublishTime = this.listView.SelectedItems[i].Text;
                strPublishTimeList += strPublishTime + "\r\n";
            }

            string strWarningText = "����(��������)�ڽ������ɾ��: \r\n" + strPublishTimeList + "\r\n\r\nȷʵҪ���ɾ������?";

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
                bool bOldChanged = this.Items.Changed;

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
                    IssueItem issueitem = (IssueItem)item.Tag;

                    int nRet = MaskDeleteItem(issueitem,
                        m_bRemoveDeletedItem);

                    if (nRet == 0)
                    {
                        if (strNotDeleteList != "")
                            strNotDeleteList += ",";
                        strNotDeleteList += issueitem.PublishTime;
                        continue;
                    }

                    if (string.IsNullOrEmpty(issueitem.RecPath) == false)
                        deleted_recpaths.Add(issueitem.RecPath);

                    nDeleteCount++;
                }

                string strText = "";

                if (strNotDeleteList != "")
                    strText += "����ʱ��Ϊ '" + strNotDeleteList + "' ���ڰ����в���Ϣ, δ�ܼ��Ա��ɾ����\r\n\r\n";

                if (deleted_recpaths.Count == 0)
                    strText += "��ֱ��ɾ�� " + nDeleteCount.ToString() + " �";
                else if (nDeleteCount - deleted_recpaths.Count == 0)
                    strText += "�����ɾ�� "
                        + deleted_recpaths.Count.ToString()
                        + " �\r\n\r\n(ע�������ɾ�������Ҫ�����ύ����Ż������ӷ�����ɾ��)";
                else
                    strText += "�����ɾ�� "
    + deleted_recpaths.Count.ToString()
    + " �ֱ��ɾ�� "
    + (nDeleteCount - deleted_recpaths.Count).ToString()
    + " �\r\n\r\n(ע�������ɾ�������Ҫ�����ύ����Ż������ӷ�����ɾ��)";

                MessageBox.Show(ForegroundWindow.Instance, strText);

#if NO
                if (this.ContentChanged != null
                    && bOldChanged != this.Items.Changed)
                {
                    ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                    e1.OldChanged = bOldChanged;
                    e1.CurrentChanged = this.Items.Changed;
                    this.ContentChanged(this, e1);
                }
#endif
                TriggerContentChanged(bOldChanged, this.Items.Changed);

            }
            finally
            {
                this.EnableControls(true);
            }
        }

        // 
        // return:
        //      0   ��Ϊ�в���Ϣ��δ�ܱ��ɾ��
        //      1   �ɹ�ɾ��
        /// <summary>
        /// ���ɾ������
        /// </summary>
        /// <param name="issueitem">����</param>
        /// <param name="bRemoveDeletedItem">�Ƿ�� ListView ������������ʾ</param>
        /// <returns>0: ��ΪĳЩԭ��δ�ܱ��ɾ��; 1: �ɹ�ɾ��</returns>
        public override int MaskDeleteItem(IssueItem issueitem,
            bool bRemoveDeletedItem)
        {
            // TODO:����ж�һ�����������Ĳ���Ϣ��
            // ����˵����Ϣ��û����ͨ��Ϣ�Կ���ɾ����
            /*
            if (String.IsNullOrEmpty(issueitem.Borrower) == false)
                return 0;
             * */

            this.Items.MaskDeleteItem(bRemoveDeletedItem,
                issueitem);
            return 1;
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            menu_modifyIssue_Click(this, null);
        }

#if NO
        void EnableControls(bool bEnable)
        {
            if (this.EnableControlsEvent == null)
                return;

            EnableControlsEventArgs e = new EnableControlsEventArgs();
            e.bEnable = bEnable;
            this.EnableControlsEvent(this, e);
        }
#endif

        private void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int nClickColumn = e.Column;

            ColumnSortStyle sortStyle = ColumnSortStyle.LeftAlign;

            // 2009/2/16
            // ��4/5/6��Ϊ�������֣�����������
            if (nClickColumn == 3
                || nClickColumn == 4
                || nClickColumn == 5)
                sortStyle = ColumnSortStyle.RightAlign;
            else if (nClickColumn == 9)
                sortStyle = ColumnSortStyle.RecPath;

            this.SortColumns.SetFirstColumn(nClickColumn,
                sortStyle,
                this.listView.Columns,
                true);

            // ����
            this.listView.ListViewItemSorter = new SortColumnsComparer(this.SortColumns);

            this.listView.ListViewItemSorter = null;
        }

#if NO
        // 2010/4/27
        // �����ڼ�¼·�� ������ ��Ŀ��¼ ��ȫ�������ڼ�¼��װ�봰��
        // parameters:
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        public int DoSearchIssueByRecPath(string strIssueRecPath)
        {
            int nRet = 0;
            string strError = "";
            // �ȼ���Ƿ����ڱ�������?

            // �Ե�ǰ�����ڽ��в��¼·������
            if (this.Items != null)
            {
                IssueItem dupitem = this.Items.GetItemByRecPath(strIssueRecPath) as IssueItem;
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "�ڼ�¼ '" + strIssueRecPath + "' ����Ϊ������δ�ύ֮һɾ��������";
                    else
                        strText = "�ڿ���¼ '" + strIssueRecPath + "' �ڱ������ҵ���";

                    dupitem.HilightListViewItem(true);

                    MessageBox.Show(ForegroundWindow.Instance, strText);
                    return 1;
                }
            }

            // ��������ύ��������
            string strBiblioRecPath = "";

            // �����ڼ�¼·�����������������������Ŀ��¼·����
            nRet = SearchBiblioRecPath(strIssueRecPath,
                out strBiblioRecPath,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(ForegroundWindow.Instance, "���ڼ�¼·�� '" + strIssueRecPath + "' ���м����Ĺ����з�������: " + strError);
                return -1;
            }
            else if (nRet == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "û���ҵ�·��Ϊ '" + strIssueRecPath + "' ���ڼ�¼��");
                return 0;
            }
            else if (nRet == 1)
            {
                Debug.Assert(strBiblioRecPath != "", "");
                this.TriggerLoadRecord(strBiblioRecPath);

                // ѡ��������
                IssueItem result_item = HilightLineByItemRecPath(strIssueRecPath, true);
                return 1;
            }
            else if (nRet > 1) // ���з����ظ�
            {
                Debug.Assert(false, "���ڼ�¼·���������Բ��ᷢ���ظ�����");
            }

            return 0;
        }

#endif

#if NO
        // �����ڼ�¼·����������
        public IssueItem HilightLineByItemRecPath(string strItemRecPath,
                bool bClearOtherSelection)
        {
            IssueItem issueitem = null;

            if (bClearOtherSelection == true)
            {
                this.ListView.SelectedItems.Clear();
            }

            if (this.Items != null)
            {
                issueitem = this.Items.GetItemByRecPath(strItemRecPath) as IssueItem;
                if (issueitem != null)
                    issueitem.HilightListViewItem(true);
            }

            return issueitem;
        }
#endif

#if NO
        // �����ڼ�¼·�������������������Ŀ��¼·����
        int SearchBiblioRecPath(string strIssueRecPath,
            out string strBiblioRecPath,
            out string strError)
        {
            strError = "";
            strBiblioRecPath = "";

            string strItemText = "";
            string strBiblioText = "";

            byte[] item_timestamp = null;

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڼ����ڼ�¼ '" + strIssueRecPath + "' ����������Ŀ��¼·�� ...");
            Stop.BeginLoop();

            try
            {
                string strIndex = "@path:" + strIssueRecPath;
                string strOutputItemRecPath = "";

                long lRet = Channel.GetIssueInfo(
                    Stop,
                    strIndex,
                    // "", // strBiblioRecPath,
                    null,
                    out strItemText,
                    out strOutputItemRecPath,
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

#if NO
        // return:
        //      -1  �����Ѿ���MessageBox����
        //      0   û��װ��
        //      1   �ɹ�װ��
        public int DoLoadRecord(string strBiblioRecPath)
        {
            if (this.LoadRecord == null)
                return 0;

            LoadRecordEventArgs e = new LoadRecordEventArgs();
            e.BiblioRecPath = strBiblioRecPath;
            this.LoadRecord(this, e);
            return e.Result;
        }
#endif
    }

    /// <summary>
    /// ��ú��ֵ
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void GetMacroValueHandler(object sender,
    GetMacroValueEventArgs e);

    /// <summary>
    /// GetMacroValueHandler�Ĳ���
    /// </summary>
    public class GetMacroValueEventArgs : EventArgs
    {
        /// <summary>
        /// ����
        /// </summary>
        public string MacroName = "";
        /// <summary>
        /// ���ֵ
        /// </summary>
        public string MacroValue = "";
    }

#region �� IssueManageControl �ƶ�����

    /// <summary>
    /// ��ö�����Ϣ�¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void GetOrderInfoEventHandler(object sender,
        GetOrderInfoEventArgs e);

    /// <summary>
    /// ��ö�����Ϣ�¼��Ĳ���
    /// </summary>
    public class GetOrderInfoEventArgs : EventArgs
    {
        /// <summary>
        /// [in] ��Ŀ��¼·��
        /// </summary>
        public string BiblioRecPath = "";   // [in] ��Ŀ��¼·��

        /// <summary>
        /// [in] �ڵĳ���ʱ��
        /// </summary>
        public string PublishTime = ""; // [in] �ڵĳ���ʱ��

        /// <summary>
        /// [in] ��ǰ�û���Ͻ�ķֹݴ����б��ձ�ʾȫ����Ͻ 
        /// </summary>
        public string LibraryCodeList = ""; // [in] ��ǰ�û���Ͻ�ķֹݴ����б��ձ�ʾȫ����Ͻ 

        /// <summary>
        /// [out] ���������Ķ�����¼����
        /// </summary>
        public List<string> OrderXmls = new List<string>(); // [out] ���������Ķ�����¼����

        /// <summary>
        /// [out] ������Ϣ�����Ϊ�����ʾû���κδ���
        /// </summary>
        public string ErrorInfo = "";   // [out] ������Ϣ�����Ϊ�����ʾû���κδ���
    }

    // 2009/10/12
    /// <summary>
    /// ��ò���Ϣ�¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void GetItemInfoEventHandler(object sender,
        GetItemInfoEventArgs e);

    /// <summary>
    /// ��ò���Ϣ�¼��Ĳ���
    /// </summary>
    public class GetItemInfoEventArgs : EventArgs
    {
        /// <summary>
        /// [in] ��Ŀ��¼·��
        /// </summary>
        public string BiblioRecPath = "";   // [in] ��Ŀ��¼·��

        /// <summary>
        /// [in] �ڵĳ���ʱ��
        /// </summary>
        public string PublishTime = ""; // [in] �ڵĳ���ʱ��

        /// <summary>
        /// [out] ���������Ĳ��¼����
        /// </summary>
        public List<string> ItemXmls = new List<string>(); // [out] ���������Ĳ��¼����

        /// <summary>
        /// [out] ������Ϣ�����Ϊ�����ʾû���κδ���
        /// </summary>
        public string ErrorInfo = "";   // [out] ������Ϣ�����Ϊ�����ʾû���κδ���
    }



#endregion

    // �����������д����ͼ���������ֹ���
    /// <summary>
    /// IssueControl ��Ļ�����
    /// </summary>
    public class IssueControlBase : ItemControlBase<IssueItem, IssueItemCollection>
    {
    }


}
