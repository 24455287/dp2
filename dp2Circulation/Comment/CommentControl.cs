using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using System.Web;

using DigitalPlatform;
using DigitalPlatform.IO;
using DigitalPlatform.GUI;
using DigitalPlatform.Xml;
using DigitalPlatform.CommonControl;

using DigitalPlatform.CirculationClient;
using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.Text;

namespace dp2Circulation
{
    // public partial class CommentControl : UserControl
    /// <summary>
    /// ��ע��¼�б�ؼ�
    /// </summary>
    public partial class CommentControl : CommentControlBase
    {
#if NO
        // �����ⲿ����
        WebExternalHost m_webExternalHost = null;
        public WebExternalHost WebExternalHost
        {
            get
            {
                return this.m_webExternalHost;
            }
            set
            {
                this.m_webExternalHost = value;
            }
        }
#endif
#if NO
        public event LoadRecordHandler LoadRecord = null;
        // ����������к�����
        SortColumns SortColumns = new SortColumns();

        public bool m_bRemoveDeletedItem = false;   // ��ɾ������ʱ, �Ƿ���Ӿ���Ĩ����Щ����(ʵ�����ڴ����滹�����м����ύ������)?

#endif

        CommentViewerForm m_commentViewer = null;

        /// <summary>
        /// ��������� (UNIMARC 610�ֶ�)
        /// </summary>
        public event AddSubjectEventHandler AddSubject = null;

#if NO
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

        /// <summary>
        /// ������� / ��ֹ״̬�����ı�
        /// </summary>
        public event EnableControlsHandler EnableControlsEvent = null;

        string m_strBiblioRecPath = "";

        public CommentItemCollection Items = null;

#endif

        /// <summary>
        /// ���캯��
        /// </summary>
        public CommentControl()
        {
            InitializeComponent();

            this.m_listView = this.listView;
            this.ItemType = "comment";
            this.ItemTypeName = "��ע";
        }
#if NO
        public int CommentCount
        {
            get
            {
                if (this.Items != null)
                    return this.Items.Count;

                return 0;
            }
        }

        // ��listview�е���ע�����޸�Ϊnew״̬
        public void ChangeAllItemToNewState()
        {
            foreach (CommentItem commentitem in this.Items)
            {
                // CommentItem commentitem = this.CommentItems[i];

                if (commentitem.ItemDisplayState == ItemDisplayState.Normal
                    || commentitem.ItemDisplayState == ItemDisplayState.Changed
                    || commentitem.ItemDisplayState == ItemDisplayState.Deleted)   // ע��δ�ύ��deletedҲ��Ϊnew��
                {
                    commentitem.ItemDisplayState = ItemDisplayState.New;
                    commentitem.RefreshListView();
                    commentitem.Changed = true;    // ��һ�������ʹ�ܺ���������رմ��ڣ��Ƿ�ᾯ��(ʵ���޸�)���ݶ�ʧ
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

            this.SortColumns.Clear();
            SortColumns.ClearColumnSortDisplay(this.ListView.Columns);

            if (this.m_commentViewer != null)
                this.m_commentViewer.Clear();

            this.pieChartControl1.Values = new decimal[0];
        }

        // �����ע�й���Ϣ
        public void ClearComments()
        {
            this.Clear();
            this.Items = new CommentItemCollection();
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }

        public int CountOfVisibleCommentItems()
        {
            return this.ListView.Items.Count;
        }

        public int IndexOfVisibleCommentItems(CommentItem commentitem)
        {
            for (int i = 0; i < this.ListView.Items.Count; i++)
            {
                CommentItem cur = (CommentItem)this.ListView.Items[i].Tag;

                if (cur == commentitem)
                    return i;
            }

            return -1;
        }

        public CommentItem GetAtVisibleCommentItems(int nIndex)
        {
            return (CommentItem)this.ListView.Items[nIndex].Tag;
        }
#endif

        // 
        // return:
        //      -1  ����
        //      0   û��װ��
        //      1   �Ѿ�װ��
        /// <summary>
        /// ���һ����Ŀ��¼������ȫ����ע��¼·��
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
        public static int GetCommentRecPaths(
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

                long lRet = channel.GetComments(
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
                        strError = "·��Ϊ '" + entities[i].OldRecPath + "' ����ע��¼װ���з�������: " + entities[i].ErrorInfo;  // NewRecPath
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
        // װ����ע��¼
        // return:
        //      -1  ����
        //      0   û��װ��
        //      1   �Ѿ�װ��
        public int LoadCommentRecords(string strBiblioRecPath,
            out string strError)
        {
            this.BiblioRecPath = strBiblioRecPath;

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("����װ����ע��Ϣ ...");
            Stop.BeginLoop();

            this.Update();

            try
            {
                long lStart = 0;
                long lResultCount = 0;
                long lCount = -1;
                this.ClearComments();

                // 2012/5/9 ��дΪѭ����ʽ
                for (; ; )
                {
                    EntityInfo[] comments = null;

                    long lRet = Channel.GetComments(
                        Stop,
                        strBiblioRecPath,
                        lStart,
                        lCount,
                        "",
                        "zh",
                        out comments,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;


                    if (lRet == 0)
                        return 0;

                    lResultCount = lRet;

                    Debug.Assert(comments != null, "");

                    this.ListView.BeginUpdate();
                    try
                    {
                        for (int i = 0; i < comments.Length; i++)
                        {
                            if (comments[i].ErrorCode != ErrorCodeValue.NoError)
                            {
                                strError = "·��Ϊ '" + comments[i].OldRecPath + "' ����ע��¼װ���з�������: " + comments[i].ErrorInfo;  // NewRecPath
                                return -1;
                            }

                            // ����һ����עxml��¼��ȡ���й���Ϣ����listview��
                            CommentItem commentitem = new CommentItem();

                            int nRet = commentitem.SetData(comments[i].OldRecPath, // NewRecPath
                                     comments[i].OldRecord,
                                     comments[i].OldTimestamp,
                                     out strError);
                            if (nRet == -1)
                                return -1;

                            if (comments[i].ErrorCode == ErrorCodeValue.NoError)
                                commentitem.Error = null;
                            else
                                commentitem.Error = comments[i];

                            this.Items.Add(commentitem);

                            commentitem.AddToListView(this.ListView);
                        }
                    }
                    finally
                    {
                        this.ListView.EndUpdate();
                    }

                    lStart += comments.Length;
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

            RefreshOrderSuggestionPie();

            return 1;
        ERROR1:
            return -1;
        }

#endif

        /// <summary>
        /// װ�� Item ��¼
        /// </summary>
        /// <param name="strBiblioRecPath">��Ŀ��¼·��</param>
        /// <param name="strStyle">װ�ط��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û��װ��; 1: �Ѿ�װ��</returns>
        public override int LoadItemRecords(
            string strBiblioRecPath,
            // bool bDisplayOtherLibraryItem,
            string strStyle,
            out string strError)
        {
            int nRet = base.LoadItemRecords(
                strBiblioRecPath,
                strStyle,
                out strError);
            if (nRet == -1)
                return nRet;

            RefreshOrderSuggestionPie();
            return nRet;
        }

        /// <summary>
        /// ���ݴ�����Ͽ��б�
        /// </summary>
        /// <param name="strLibraryCodeList">�ܴ����б�</param>
        public void SetLibraryCodeFilter(string strLibraryCodeList)
        {
            this.comboBox_libraryCodeFilter.Items.Clear();

            this.comboBox_libraryCodeFilter.Items.Add("<ȫ���ֹ�>");

            if (Global.IsGlobalUser(strLibraryCodeList) == true)
            {
                return;
            }

            this.comboBox_libraryCodeFilter.Items.Add(strLibraryCodeList);
        }

        /// <summary>
        /// ˢ�¶���ͳ�Ʊ�ͼ
        /// </summary>
        public void RefreshOrderSuggestionPie()
        {
            int nYes = 0;
            int nNo = 0;
            int nNull = 0;
            int nOther = 0;

            this.pieChartControl1.Values = new decimal[0];

            string strFilter = this.comboBox_libraryCodeFilter.Text;
            if (strFilter == "<ȫ���ֹ�>")
                strFilter = "";

            // parameters:
            //      strLibraryCodeList  �ݴ����б����ڹ��ˡ���ͳ������б��еġ����Ϊnull��ʾȫ��ͳ��
            //      nYes    ���鶩��������
            //      nNo     ���鲻����������
            //      nNull   û�б�̬����Ҳ�ǡ�������ѯ��������
            //      nOther  "������ѯ"���������
            this.Items.GetOrderSuggestion(
                strFilter, // strLibraryCodeList,
                out nYes,
                out nNo,
                out nNull,
                out nOther);

            int nTotal = nYes + nNo + nNull;
            if (nTotal > 0)
            {
                List<decimal> values = new List<decimal>();
                values.Add(nYes);
                values.Add(nNo);
                values.Add(nNull);

                List<float> displacements = new List<float>();
                displacements.Add(0.0F);    // 0.2F
                displacements.Add(0.0F);
                displacements.Add(0.0F);

                List<string> texts = new List<string>();
                texts.Add(
                    nYes != 0 ?
                    "�� " + nYes.ToString() + " = " + GetPercent(nYes, nTotal) + "" : "");
                texts.Add(
                    nNo != 0 ?
                    "�� " + nNo.ToString() + " = " + GetPercent(nNo, nTotal) + "" : "");
                texts.Add(
        nNull != 0 ?
        "�� " + nNull.ToString() + " = " + GetPercent(nNull, nTotal) + "" : "");

                List<Color> colors = new List<Color>();
                colors.Add(Color.FromArgb(200, 100, 200, 50));    // green
                colors.Add(Color.FromArgb(200, 230, 0, 0)); // red
                colors.Add(Color.FromArgb(200, 200, 200, 200)); // white


                this.pieChartControl1.Values = values.ToArray();
                this.pieChartControl1.Texts = texts.ToArray();
                this.pieChartControl1.Colors = colors.ToArray();
                this.pieChartControl1.SliceRelativeDisplacements = displacements.ToArray();
                this.pieChartControl1.SliceRelativeHeight = 0.05F;

                /*
                int nMargin = Math.Min(this.pieChartControl1.Width, this.pieChartControl1.Height) / 5;
                this.pieChartControl1.LeftMargin = nMargin;
                this.pieChartControl1.RightMargin = nMargin;
                this.pieChartControl1.TopMargin = nMargin;
                this.pieChartControl1.BottomMargin = nMargin;
                 * */
                SetPieChartMargin();


                this.pieChartControl1.ShadowStyle = System.Drawing.PieChart.ShadowStyle.UniformShadow;
                this.pieChartControl1.EdgeColorType = System.Drawing.PieChart.EdgeColorType.DarkerThanSurface;
            }

        }

        static string GetPercent(double v1, double v2)
        {
            double ratio = v1 / v2;
            // return String.Format("{0,3:N}", ratio * (double)100) + "%";
            return String.Format("{0:0%}", ratio);
        }

#if NO
        // �������б���
        // return:
        //      -2  �Ѿ�����(���ֳɹ�������ʧ��)
        //      -1  ����
        //      0   ����ɹ���û�д���;���
        int SaveComments(EntityInfo[] comments,
            out string strError)
        {
            strError = "";

            bool bWarning = false;
            EntityInfo[] errorinfos = null;

            int nBatch = 100;
            for (int i = 0; i < (comments.Length / nBatch) + ((comments.Length % nBatch) != 0 ? 1 : 0); i++)
            {
                int nCurrentCount = Math.Min(nBatch, comments.Length - i * nBatch);
                EntityInfo[] current = EntityControl.GetPart(comments, i * nBatch, nCurrentCount);

                int nRet = SaveCommentRecords(this.BiblioRecPath,
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

        // �ύ��ע��������
        // return:
        //      -1  ����
        //      0   û�б�Ҫ����
        //      1   ����ɹ�
        public int DoSaveComments()
        {
            if (this.Items == null)
                return 0;

            EnableControls(false);

            try
            {
                string strError = "";
                int nRet = 0;

                if (this.Items == null)
                {
                    return 0;
                }

                // ���ȫ�������Parentֵ�Ƿ��ʺϱ���
                // return:
                //      -1  �д��󣬲��ʺϱ���
                //      0   û�д���
                nRet = this.Items.CheckParentIDForSave(out strError);
                if (nRet == -1)
                {
                    strError = "������ע��Ϣʧ�ܣ�ԭ��" + strError;
                    goto ERROR1;
                }

                EntityInfo[] comments = null;

                // ������Ҫ�ύ����ע��Ϣ����
                nRet = BuildSaveComments(
                    out comments,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (comments == null || comments.Length == 0)
                    return 0; // û�б�Ҫ����

#if NO
                EntityInfo[] errorinfos = null;
                nRet = SaveCommentRecords(this.BiblioRecPath,
                    comments,
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
                nRet = SaveComments(comments, out strError);
                if (nRet == -2)
                    return -1;  // SaveComments()�Ѿ�MessageBox()��ʾ����
                if (nRet == -1)
                    goto ERROR1;

                this.Changed = false;
                this.MainForm.StatusBarMessage = "��ע��Ϣ �ύ / ���� �ɹ�";
                DoViewComment(false);
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

        // ������ע��¼·����������
        public CommentItem HilightLineByItemRecPath(string strItemRecPath,
                bool bClearOtherSelection)
        {
            CommentItem commentitem = null;

            if (bClearOtherSelection == true)
            {
                this.ListView.SelectedItems.Clear();
            }

            if (this.Items != null)
            {
                commentitem = this.Items.GetItemByRecPath(strItemRecPath) as CommentItem;
                if (commentitem != null)
                    commentitem.HilightListViewItem(true);
            }

            return commentitem;
        }

#endif

#if NO
        // 2011/6/30 new add
        // ������ע��¼·�� ������ ��Ŀ��¼ ��ȫ��������ע��¼��װ�봰��
        // parameters:
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        public int DoSearchCommentByRecPath(string strCommentRecPath)
        {
            int nRet = 0;
            string strError = "";
            // �ȼ���Ƿ����ڱ�������?

            // �Ե�ǰ�����ڽ��в��¼·������
            if (this.Items != null)
            {
                CommentItem dupitem = this.Items.GetItemByRecPath(strCommentRecPath) as CommentItem;
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "��ע��¼ '" + strCommentRecPath + "' ����Ϊ������δ�ύ֮һɾ����ע����";
                    else
                        strText = "��ע��¼ '" + strCommentRecPath + "' �ڱ������ҵ���";

                    dupitem.HilightListViewItem(true);

                    MessageBox.Show(ForegroundWindow.Instance, strText);
                    return 1;
                }
            }

            // ��������ύ��������
            string strBiblioRecPath = "";


            // ������ע��¼·�����������������������Ŀ��¼·����
            nRet = SearchBiblioRecPath(strCommentRecPath,
                out strBiblioRecPath,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(ForegroundWindow.Instance, "����ע��¼·�� '" + strCommentRecPath + "' ���м����Ĺ����з�������: " + strError);
                return -1;
            }
            else if (nRet == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "û���ҵ�·��Ϊ '" + strCommentRecPath + "' ����ע��¼��");
                return 0;
            }
            else if (nRet == 1)
            {
                Debug.Assert(strBiblioRecPath != "", "");
                this.TriggerLoadRecord(strBiblioRecPath);

                // ѡ����ע����
                CommentItem result_item = HilightLineByItemRecPath(strCommentRecPath, true);
                return 1;
            }
            else if (nRet > 1) // ���з����ظ�
            {
                Debug.Assert(false, "����ע��¼·���������Բ�Ӧ�����ظ����� -- ���Ǿ�Ȼ������");
            }

            return 0;
        }
#endif

#if NO
        // ������ע��¼·�������������������Ŀ��¼·����
        int SearchBiblioRecPath(string strCommentRecPath,
            out string strBiblioRecPath,
            out string strError)
        {
            strError = "";
            strBiblioRecPath = "";

            string strItemText = "";
            string strBiblioText = "";

            byte[] item_timestamp = null;

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڼ�����ע��¼ '" + strCommentRecPath + "' ����������Ŀ��¼·�� ...");
            Stop.BeginLoop();

            try
            {
                string strIndex = "@path:" + strCommentRecPath;
                string strOutputItemRecPath = "";

                long lRet = Channel.GetCommentInfo(
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

        // 
        /// <summary>
        /// �����Ѿ��Ƽ�������Ŀ����ע��Ϣ��HTML��ʽ
        /// </summary>
        /// <param name="strHtml">���� HTML �ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int GetOrderSuggestionHtml(out string strHtml,
            out string strError)
        {
            strHtml = "";
            strError = "";

            if (this.Items == null)
                return 0;

            StringBuilder result = new StringBuilder(4096);

            foreach (CommentItem comment in this.Items)
            {
                // �������Ƕ�����ѯ���͵ļ�¼
                if (comment.TypeString != "������ѯ")
                    continue;
                // �������� �Ƽ����� ����Щ��Ŀ
                if (comment.OrderSuggestion != "yes")
                    continue;

                result.Append("<tr class='content'>");

                result.Append("<td class='title'>" + HttpUtility.HtmlEncode(comment.Title) + "</td>");
                result.Append("<td class='creator'>" + HttpUtility.HtmlEncode(comment.Creator) + "</td>");
                result.Append("<td class='content'>" + HttpUtility.HtmlEncode(comment.Content).Replace("\\r", "<br/>") + "</td>");

                result.Append("</tr>");
            }

            if (result.Length == 0)
                return 0;

            StringBuilder columntitle = new StringBuilder(4096);
            columntitle.Append("<tr class='column'>");
            columntitle.Append("<td class='title'>����</td>");
            columntitle.Append("<td class='creator'>����</td>");
            columntitle.Append("<td class='content'>����</td>");
            columntitle.Append("</tr>");

            strHtml = "<table class='comments'>" + columntitle.ToString() + result.ToString() + "</table>";
            return 0;
        }

#if NO
        // �������ڱ������ע��Ϣ����
        int BuildSaveComments(
            out EntityInfo[] comments,
            out string strError)
        {
            strError = "";
            comments = null;
            int nRet = 0;

            Debug.Assert(this.Items != null, "");

            List<EntityInfo> commentArray = new List<EntityInfo>();

            foreach (CommentItem commentitem in this.Items)
            {
                // CommentItem commentitem = this.CommentItems[i];

                if (commentitem.ItemDisplayState == ItemDisplayState.Normal)
                    continue;

                EntityInfo info = new EntityInfo();

                if (String.IsNullOrEmpty(commentitem.RefID) == true)
                {
                    commentitem.RefID = Guid.NewGuid().ToString();
                    commentitem.RefreshListView();
                }

                info.RefID = commentitem.RefID;  // 2008/2/17 new add

                string strXml = "";
                nRet = commentitem.BuildRecord(out strXml,
                        out strError);
                if (nRet == -1)
                    return -1;

                if (commentitem.ItemDisplayState == ItemDisplayState.New)
                {
                    info.Action = "new";
                    info.NewRecPath = "";
                    info.NewRecord = strXml;
                    info.NewTimestamp = null;
                }

                if (commentitem.ItemDisplayState == ItemDisplayState.Changed)
                {
                    info.Action = "change";
                    info.OldRecPath = commentitem.RecPath;
                    info.NewRecPath = commentitem.RecPath;

                    info.NewRecord = strXml;
                    info.NewTimestamp = null;

                    info.OldRecord = commentitem.OldRecord;
                    info.OldTimestamp = commentitem.Timestamp;
                }

                if (commentitem.ItemDisplayState == ItemDisplayState.Deleted)
                {
                    info.Action = "delete";
                    info.OldRecPath = commentitem.RecPath; // NewRecPath

                    info.NewRecord = "";
                    info.NewTimestamp = null;

                    info.OldRecord = commentitem.OldRecord;
                    info.OldTimestamp = commentitem.Timestamp;
                }

                commentArray.Add(info);
            }

            // ���Ƶ�Ŀ��
            comments = new EntityInfo[commentArray.Count];
            for (int i = 0; i < commentArray.Count; i++)
            {
                comments[i] = commentArray[i];
            }

            return 0;
        }

        // ���������޸Ĺ�������Ϣ����
        // ���strNewBiblioPath�е���Ŀ���������仯������ע��¼��Ҫ����ע��֮���ƶ�����Ϊ��ע�����Ŀ����һ���������ϵ��
        int BuildChangeParentRequestComments(
            List<CommentItem> commentitems,
            string strNewBiblioRecPath,
            out EntityInfo[] entities,
            out string strError)
        {
            strError = "";
            entities = null;
            int nRet = 0;

            string strSourceBiblioDbName = Global.GetDbName(this.BiblioRecPath);
            string strTargetBiblioDbName = Global.GetDbName(strNewBiblioRecPath);

            // ���һ��Ŀ����Ŀ�����ǲ��ǺϷ�����Ŀ����
            if (MainForm.IsValidBiblioDbName(strTargetBiblioDbName) == false)
            {
                strError = "Ŀ����� '" + strTargetBiblioDbName + "' ����ϵͳ�������Ŀ����֮��";
                return -1;
            }

            // ���Ŀ����Ŀ��¼id
            string strTargetBiblioRecID = Global.GetRecordID(strNewBiblioRecPath);   // !!!
            if (String.IsNullOrEmpty(strTargetBiblioRecID) == true)
            {
                strError = "��Ŀ����Ŀ��¼·�� '" + strNewBiblioRecPath + "' ��û�а���ID���֣��޷����в���";
                return -1;
            }
            if (strTargetBiblioRecID == "?")
            {
                strError = "Ŀ����Ŀ��¼·�� '" + strNewBiblioRecPath + "' �м�¼ID��ӦΪ�ʺ�";
                return -1;
            }
            if (Global.IsPureNumber(strTargetBiblioRecID) == false)
            {
                strError = "Ŀ����Ŀ��¼·�� '" + strNewBiblioRecPath + "' �м�¼IDӦΪ������";
                return -1;
            }

            bool bMove = false; // �Ƿ���Ҫ�ƶ���ע��¼
            string strTargetCommentDbName = "";  // Ŀ����ע����

            if (strSourceBiblioDbName != strTargetBiblioDbName)
            {
                // ��Ŀ�ⷢ���˸ı䣬���б�Ҫ�ƶ�����������޸���ע��¼��<parent>����
                bMove = true;
                strTargetCommentDbName = MainForm.GetCommentDbName(strTargetBiblioDbName);

                if (String.IsNullOrEmpty(strTargetCommentDbName) == true)
                {
                    strError = "��Ŀ�� '" + strTargetBiblioDbName + "' ��û�д�������ע�ⶨ�塣����ʧ��";
                    return -1;
                }
            }

            Debug.Assert(commentitems != null, "");

            List<EntityInfo> entityArray = new List<EntityInfo>();

            for (int i = 0; i < commentitems.Count; i++)
            {
                CommentItem commentitem = commentitems[i];

                EntityInfo info = new EntityInfo();

                if (String.IsNullOrEmpty(commentitem.RefID) == true)
                {
                    Debug.Assert(false, "commentitem.RefIDӦ��Ϊֻ�������Ҳ�����Ϊ��");
                    /*
                    commentitem.RefID = Guid.NewGuid().ToString();
                    commentitem.RefreshListView();
                     * */
                }

                info.RefID = commentitem.RefID;
                commentitem.Parent = strTargetBiblioRecID;

                string strXml = "";
                nRet = commentitem.BuildRecord(out strXml,
                        out strError);
                if (nRet == -1)
                    return -1;

                info.OldRecPath = commentitem.RecPath;
                if (bMove == false)
                {
                    info.Action = "change";
                    info.NewRecPath = commentitem.RecPath;
                }
                else
                {
                    info.Action = "move";
                    Debug.Assert(String.IsNullOrEmpty(strTargetCommentDbName) == false, "");
                    info.NewRecPath = strTargetCommentDbName + "/?";  // ����ע��¼�ƶ�����һ����ע���У�׷�ӳ�һ���¼�¼�����ɼ�¼�Զ���ɾ��
                }

                info.NewRecord = strXml;
                info.NewTimestamp = null;

                info.OldRecord = commentitem.OldRecord;
                info.OldTimestamp = commentitem.Timestamp;

                entityArray.Add(info);
            }

            // ���Ƶ�Ŀ��
            entities = new EntityInfo[entityArray.Count];
            for (int i = 0; i < entityArray.Count; i++)
            {
                entities[i] = entityArray[i];
            }

            return 0;
        }

        // ������ע��¼
        // ������ˢ�½���ͱ���
        int SaveCommentRecords(string strBiblioRecPath,
            EntityInfo[] comments,
            out EntityInfo[] errorinfos,
            out string strError)
        {
            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڱ�����ע��Ϣ ...");
            Stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.SetComments(
                    Stop,
                    strBiblioRecPath,
                    comments,
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

        // �ѱ�����Ϣ�еĳɹ������״̬�޸Ķ���
        // ���ҳ���ȥ��û�б���ġ�ɾ����CommentItem����ڴ���Ӿ��ϣ�
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
                CommentItem commentitem = null;

                string strError = "";

                if (String.IsNullOrEmpty(errorinfos[i].RefID) == true)
                {
                    MessageBox.Show(ForegroundWindow.Instance, "���������ص�EntityInfo�ṹ��RefIDΪ��");
                    return true;
                }

                nRet = LocateCommentItem(
                    errorinfos[i].RefID,
                    OrderControl.GetOneRecPath(errorinfos[i].NewRecPath, errorinfos[i].OldRecPath),
                    out commentitem,
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
                    commentitem.Index,    // strIndex,
                    errorinfos[i].NewRecPath,
                    errorinfos[i].RefID);

                // ������Ϣ����
                if (errorinfos[i].ErrorCode == ErrorCodeValue.NoError)
                {
                    if (errorinfos[i].Action == "new")
                    {
                        commentitem.OldRecord = errorinfos[i].NewRecord;
                        nRet = commentitem.ResetData(
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
                        commentitem.OldRecord = errorinfos[i].NewRecord;

                        nRet = commentitem.ResetData(
                            errorinfos[i].NewRecPath,
                            errorinfos[i].NewRecord,
                            errorinfos[i].NewTimestamp,
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(ForegroundWindow.Instance, strError);

                        commentitem.ItemDisplayState = ItemDisplayState.Normal;
                    }

                    // ���ڱ�����ò������ڱ��ֵģ�Ҫ��listview������
                    if (String.IsNullOrEmpty(commentitem.RecPath) == false)
                    {
                        string strTempCommentDbName = Global.GetDbName(commentitem.RecPath);
                        string strTempBiblioDbName = this.MainForm.GetBiblioDbNameFromCommentDbName(strTempCommentDbName);

                        Debug.Assert(String.IsNullOrEmpty(strTempBiblioDbName) == false, "");
                        // TODO: ����Ҫ���汨��

                        string strTempBiblioRecPath = strTempBiblioDbName + "/" + commentitem.Parent;

                        if (strTempBiblioRecPath != this.BiblioRecPath)
                        {
                            this.Items.PhysicalDeleteItem(commentitem);
                            continue;
                        }
                    }

                    commentitem.Error = null;   // ������ʾ ��?

                    commentitem.Changed = false;
                    commentitem.RefreshListView();
                    continue;
                }

                // ������
                commentitem.Error = errorinfos[i];
                commentitem.RefreshListView();

                strWarning += strLocationSummary + "���ύ��ע��������з������� -- " + errorinfos[i].ErrorInfo + "\r\n";
            }


            // ����û�б���ģ���Щ�ɹ�ɾ����������ڴ���Ӿ���Ĩ��
            for (int i = 0; i < this.Items.Count; i++)
            {
                CommentItem commentitem = this.Items[i] as CommentItem;
                if (commentitem.ItemDisplayState == ItemDisplayState.Deleted)
                {
                    if (commentitem.ErrorInfo == "")
                    {
                        this.Items.PhysicalDeleteItem(commentitem);
                        i--;
                    }
                }
            }

            // �޸�Changed״̬
            if (this.ContentChanged != null)
            {
                ContentChangedEventArgs e1 = new ContentChangedEventArgs();
                e1.OldChanged = bOldChanged;
                e1.CurrentChanged = this.Items.Changed;
                this.ContentChanged(this, e1);
            }

            // 
            if (String.IsNullOrEmpty(strWarning) == false)
            {
                strWarning += "\r\n��ע���޸���ע��Ϣ�������ύ����";
                MessageBox.Show(ForegroundWindow.Instance, strWarning);
                return true;
            }

            return false;
        }


        // ��������ƺ�
        static string GetLocationSummary(
            string strIndex,
            string strRecPath,
            string strRefID)
        {
            if (String.IsNullOrEmpty(strIndex) == false)
                return "���Ϊ '" + strIndex + "' ������";
            if (String.IsNullOrEmpty(strRecPath) == false)
                return "��¼·��Ϊ '" + strRecPath + "' ������";
            if (String.IsNullOrEmpty(strRefID) == false)
                return "�ο�IDΪ '" + strRefID + "' ������";


            return "���κζ�λ��Ϣ������";
        }

#endif

        // ��������ƺ�
        internal override string GetLocationSummary(CommentItem bookitem)
        {
            string strIndex = bookitem.Index;

            if (String.IsNullOrEmpty(strIndex) == false)
                return "���Ϊ '" + strIndex + "' ������";

            string strRecPath = bookitem.RecPath;

            if (String.IsNullOrEmpty(strRecPath) == false)
                return "��¼·��Ϊ '" + strRecPath + "' ������";

            string strRefID = bookitem.RefID;
            // 2008/6/24 new add
            if (String.IsNullOrEmpty(strRefID) == false)
                return "�ο�IDΪ '" + strRefID + "' ������";

            return "���κζ�λ��Ϣ������";
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

#if NO
        // ��this.commentitems�ж�λ��strRefID����������
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int LocateCommentItem(
            string strRefID,
            string strRecPath,
            out CommentItem commentitem,
            out string strError)
        {
            strError = "";

            // �����ü�¼·������λ
            if (string.IsNullOrEmpty(strRecPath) == false
                && Global.IsAppendRecPath(strRecPath) == false)
            {
                commentitem = this.Items.GetItemByRecPath(strRecPath) as CommentItem;
                if (commentitem != null)
                    return 1;   // found
            }

            // Ȼ���òο�ID����λ
            commentitem = this.Items.GetItemByRefID(strRefID, null) as CommentItem;

            if (commentitem != null)
                return 1;   // found

            strError = "û���ҵ� ��¼·��Ϊ '" + strRecPath + "'������ �ο�ID Ϊ '" + strRefID + "' ��CommentItem����";
            return 0;
        }

#endif

        private void ListView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            bool bHasBillioLoaded = false;

            if (String.IsNullOrEmpty(this.BiblioRecPath) == false)
                bHasBillioLoaded = true;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            menuItem = new MenuItem("�鿴(&V)");
            menuItem.Click += new System.EventHandler(this.menu_viewComment_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            menuItem.DefaultItem = true;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�޸�(&M)");
            menuItem.Click += new System.EventHandler(this.menu_modifyComment_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("����(&N)");
            menuItem.Click += new System.EventHandler(this.menu_newComment_Click);
            if (bHasBillioLoaded == false)
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

            menuItem = new MenuItem("װ���¿�����ע��(&E)");
            menuItem.Click += new System.EventHandler(this.menu_loadToNewItemForm_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("װ���Ѿ��򿪵���ע��(&E)");
            menuItem.Click += new System.EventHandler(this.menu_loadToExistItemForm_Click);
            if (this.listView.SelectedItems.Count == 0
                || this.MainForm.GetTopChildWindow<ItemInfoForm>() == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�쿴��ע��¼�ļ����� (&K)");
            menuItem.Click += new System.EventHandler(this.menu_getKeys_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("�������ɴ�(&S)");
            menuItem.Click += new System.EventHandler(this.menu_addSubject_Click);
            if (this.listView.SelectedItems.Count == 0 || this.AddSubject == null)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // -----
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("���ɾ��(&D)");
            menuItem.Click += new System.EventHandler(this.menu_deleteComment_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("����ɾ��(&U)");
            menuItem.Click += new System.EventHandler(this.menu_undoDeleteComment_Click);
            if (this.listView.SelectedItems.Count == 0)
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.listView, new Point(e.X, e.Y));		
        }

        // �������ɴ�
        void menu_addSubject_Click(object sender, EventArgs e)
        {
            if (this.AddSubject == null)
            {
                MessageBox.Show(this, "CommentControlû�йҽ�AddSubject�¼�");
                return;
            }

            List<string> new_subjects = new List<string>();
            List<string> hidden_subjects = new List<string>();
            foreach (ListViewItem item in this.listView.SelectedItems)
            {
                CommentItem comment_item = (CommentItem)item.Tag;
                if (comment_item == null)
                {
                    Debug.Assert(false, "");
                    continue;
                }
                List<string> temp = StringUtil.SplitList(comment_item.Content.Replace("\\r", "\n"), '\n');

                hidden_subjects.AddRange(temp);

                if (StringUtil.IsInList("�Ѵ���", comment_item.State) == false)
                    new_subjects.AddRange(temp);
            }

            StringUtil.RemoveDupNoSort(ref hidden_subjects);
            StringUtil.RemoveBlank(ref hidden_subjects);
            StringUtil.RemoveDupNoSort(ref new_subjects);
            StringUtil.RemoveBlank(ref new_subjects);

            {
                AddSubjectEventArgs e1 = new AddSubjectEventArgs();
                e1.FocusedControl = this.listView;
                e1.NewSubjects = new_subjects;
                e1.HiddenSubjects = hidden_subjects;
                this.AddSubject(this, e1);

                if (string.IsNullOrEmpty(e1.ErrorInfo) == false && e1.ShowErrorBox == false)
                    MessageBox.Show(this, e1.ErrorInfo);

                if (e1.Canceled == true)
                    return;
            }

            bool bOldChanged = this.Items.Changed;

            // �޸���ע��¼��״̬
            foreach (ListViewItem item in this.listView.SelectedItems)
            {
                CommentItem comment_item = (CommentItem)item.Tag;
                if (comment_item == null)
                {
                    Debug.Assert(false, "");
                    continue;
                }
                string strState = comment_item.State;
                string strOldState = strState;

                Global.ModifyStateString(ref strState, "�Ѵ���", "");

                if (strState == strOldState)
                    continue;   // û�б�Ҫ�޸�

                comment_item.State = strState;
                comment_item.RefreshListView();
                comment_item.Changed = true;
            }

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

        /// <summary>
        /// װ���¿�����ע��
        /// </summary>
        /// <param name="sender">������</param>
        /// <param name="e">�¼�����</param>
        void menu_loadToNewItemForm_Click(object sender, EventArgs e)
        {
#if NO
            string strError = "";

            if (this.ListView.SelectedItems.Count == 0)
            {
                strError = "��δѡ��Ҫ����������";
                goto ERROR1;
            }

            CommentItem cur = (CommentItem)this.ListView.SelectedItems[0].Tag;

            if (cur == null)
            {
                strError = "CommentItem == null";
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

            form.DbType = "comment";

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

            CommentItem cur = (CommentItem)this.ListView.SelectedItems[0].Tag;

            if (cur == null)
            {
                strError = "CommentItem == null";
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
                strError = "��ǰ��û���Ѿ��򿪵���ע��";
                goto ERROR1;
            }
            form.DbType = "comment";
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

        void menu_viewComment_Click(object sender, EventArgs e)
        {
            DoViewComment(true);
        }

        // �ڼ�¼������<_recPath>Ԫ��
        /*public*/ static int AddRecPath(ref string strXml,
            string strRecPath,
            out string strError)
        {
            strError = "";

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XML�ַ���װ��DOMʱ����: " + ex.Message;
                return -1;
            }

            DomUtil.SetElementText(dom.DocumentElement,
                "_recPath",
                strRecPath);
            strXml = dom.DocumentElement.OuterXml;
            return 0;
        }

        void DoViewComment(bool bOpenWindow)
        {
            string strError = "";
            string strHtml = "";
            string strXml = "";

            // �Ż���������ν�ؽ��з���������
            if (bOpenWindow == false)
            {
                if (this.MainForm.PanelFixedVisible == false
                    && (m_commentViewer == null || m_commentViewer.Visible == false) )
                    return;
            }

            /*
            if (this.ListView.SelectedItems.Count == 0)
                return;
             * */
            if (this.listView.SelectedItems.Count != 1)
            {
                // 2012/10/8
                if (this.m_commentViewer != null)
                    this.m_commentViewer.Clear();

                return;
            }

            CommentItem commentitem = (CommentItem)this.listView.SelectedItems[0].Tag;
            //if (String.IsNullOrEmpty(commentitem.RecPath) == true)
            //    return;

            int nRet = commentitem.BuildRecord(
                true,   // Ҫ��� Parent ��Ա
                out strXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            // 2012/12/28
            // �ڼ�¼������<_recPath>Ԫ��
            nRet = AddRecPath(ref strXml,
                commentitem.RecPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            nRet = GetCommentHtml(strXml,
                out strHtml,
                out strError);
            if (nRet == -1)
                goto ERROR1;


            bool bNew = false; 
            if (this.m_commentViewer == null
                || (bOpenWindow == true && this.m_commentViewer.Visible == false))
            {
                m_commentViewer = new CommentViewerForm();
                MainForm.SetControlFont(m_commentViewer, this.Font, false);
                bNew = true;
            }

            m_commentViewer.MainForm = this.MainForm;  // �����ǵ�һ��

            if (bNew == true)
                m_commentViewer.InitialWebBrowser();

            m_commentViewer.Text = "��ע '" + commentitem.RecPath + "'";
            m_commentViewer.HtmlString = strHtml;
            m_commentViewer.XmlString = strXml;
            m_commentViewer.FormClosed -= new FormClosedEventHandler(m_viewer_FormClosed);
            m_commentViewer.FormClosed += new FormClosedEventHandler(m_viewer_FormClosed);
            // this.MainForm.AppInfo.LinkFormState(m_viewer, "comment_viewer_state");
            // m_viewer.ShowDialog(this);
            // this.MainForm.AppInfo.UnlinkFormState(m_viewer);
            if (bOpenWindow == true)
            {
                if (m_commentViewer.Visible == false)
                {
                    this.MainForm.AppInfo.LinkFormState(m_commentViewer, "comment_viewer_state");
                    m_commentViewer.Show(this);
                    m_commentViewer.Activate();

                    this.MainForm.CurrentPropertyControl = null;
                }
                else
                {
                    if (m_commentViewer.WindowState == FormWindowState.Minimized)
                        m_commentViewer.WindowState = FormWindowState.Normal;
                    m_commentViewer.Activate();
                }
            }
            else
            {
                if (m_commentViewer.Visible == true)
                {

                }
                else
                {
                    if (this.MainForm.CurrentPropertyControl != m_commentViewer.MainControl)
                        m_commentViewer.DoDock(false); // �����Զ���ʾFixedPanel
                }
            }
            return;
        ERROR1:
            MessageBox.Show(this, "DoViewComment() ����: " + strError);
        }

        void m_viewer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_commentViewer != null)
            {
                this.MainForm.AppInfo.UnlinkFormState(m_commentViewer);
                this.m_commentViewer = null;
            }
        }

        /*public*/ int GetCommentHtml(string strXml,
    out string strHtml,
    out string strError)
        {
            strError = "";
            strHtml = "";

            Stop.OnStop += new StopEventHandler(this.DoStop);
            Stop.Initial("���ڻ����ע HTML ��Ϣ ...");
            Stop.BeginLoop();

            this.Update();

            try
            {
                string strOutputCommentRecPath = "";
                byte[] baTimestamp = null;
                string strBiblio = "";
                string strOutputBiblioRecPath = "";

                long lRet = Channel.GetCommentInfo(
                    Stop,
                    strXml,
                    // "",
                    "html",
                    out strHtml,
                    out strOutputCommentRecPath,
                    out baTimestamp,
                    "",
                    out strBiblio,
                    out strOutputBiblioRecPath,
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

#if NO
        public int GetCommentHtml(string strCommentRecPath,
            out string strHtml,
            out string strXml,
            out string strError)
        {
            strError = "";
            strHtml = "";
            strXml = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("����װ����ע HTML/XML ��Ϣ ...");
            stop.BeginLoop();

            this.Update();

            try
            {
                string strOutputCommentRecPath = "";
                byte [] baTimestamp = null;
                string strBiblio = "";
                string strOutputBiblioRecPath = "";
               
                long lRet = Channel.GetCommentInfo(
                    stop,
                    string.IsNullOrEmpty(strCommentRecPath) == false && strCommentRecPath[0] == '<' ? strCommentRecPath : "@path:" + strCommentRecPath,
                    // "",
                    "html",
                    out strHtml,
                    out strOutputCommentRecPath,
                    out baTimestamp,
                    "",
                    out strBiblio,
                    out strOutputBiblioRecPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
                lRet = Channel.GetCommentInfo(
    stop,
    "@path:" + strCommentRecPath,
    // "",
    "xml",
    out strXml,
    out strOutputCommentRecPath,
    out baTimestamp,
    "",
    out strBiblio,
    out strOutputBiblioRecPath,
    out strError);
                if (lRet == -1)
                    goto ERROR1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");
            }

            return 1;
        ERROR1:
            return -1;
        }
#endif

        void menu_modifyComment_Click(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "��δѡ��Ҫ�༭������");
                return;
            }
            CommentItem commentitem = (CommentItem)this.listView.SelectedItems[0].Tag;

            ModifyComment(commentitem);
        }

#if NO
        // ΪCommentItem��������ȱʡֵ
        // parameters:
        //      strCfgEntry Ϊ"comment_normalRegister_default"��"comment_quickRegister_default"
        int SetCommentItemDefaultValues(
            string strCfgEntry,
            CommentItem commentitem,
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

            int nRet = commentitem.SetData("",
                strNewDefault,
                null,
                out strError);
            if (nRet == -1)
                return -1;

            commentitem.Parent = "";
            commentitem.RecPath = "";

            return 0;
        }
#endif

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

        void ModifyComment(CommentItem commentitem)
        {
            Debug.Assert(commentitem != null, "");

            bool bOldChanged = this.Items.Changed;

            string strOldIndex = commentitem.Index;

            CommentEditForm edit = new CommentEditForm();

            edit.BiblioDbName = Global.GetDbName(this.BiblioRecPath);
            edit.MainForm = this.MainForm;
            edit.ItemControl = this;
            string strError = "";
            int nRet = edit.InitialForEdit(commentitem,
                this.Items,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(ForegroundWindow.Instance, strError);
                return;
            }
            edit.StartItem = null;  // ���ԭʼ������

        REDO:
            this.MainForm.AppInfo.LinkFormState(edit, "CommentEditForm_state");
            edit.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(edit);

            if (edit.DialogResult != DialogResult.OK)
                return;

            RefreshOrderSuggestionPie();

            DoViewComment(false);

#if NO
            // CommentItem�����Ѿ����޸�
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
                if (strOldIndex != commentitem.Index) // ��Ÿı��˵�����²Ų���
                {
                    // ��Ҫ�ų����Լ�: commentitem��
                    List<CommentItem> excludeItems = new List<CommentItem>();
                    excludeItems.Add(commentitem);


                    // �Ե�ǰ�����ڽ��б�Ų���
                    CommentItem dupitem = this.Items.GetItemByIndex(
                        commentitem.Index,
                        excludeItems);
                    if (dupitem != null)
                    {
                        string strText = "";
                        if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                            strText = "��� '" + commentitem.Index + "' �ͱ�����δ�ύ֮һɾ��������ء�����ȷ������ť�������룬���˳��Ի���������ύ����֮�޸ġ�";
                        else
                            strText = "��� '" + commentitem.Index + "' �ڱ������Ѿ����ڡ�����ȷ������ť�������롣";

                        MessageBox.Show(ForegroundWindow.Instance, strText);
                        goto REDO;
                    }

                    // ��(����)������ע��¼���б�Ų���
                    if (edit.AutoSearchDup == true
                        && string.IsNullOrEmpty(commentitem.RefID) == false)
                    {
                        // Debug.Assert(false, "");

                        string[] paths = null;
                        // ��Ų��ء�
                        // parameters:
                        //      strOriginRecPath    ������¼��·����
                        //      paths   �������е�·��
                        // return:
                        //      -1  error
                        //      0   not dup
                        //      1   dup
                        nRet = SearchCommentRefIdDup(commentitem.RefID,
                            // this.BiblioRecPath,
                            commentitem.RecPath,
                            out paths,
                            out strError);
                        if (nRet == -1)
                            MessageBox.Show(ForegroundWindow.Instance, "�Բο�ID '" + commentitem.RefID + "' ���в��صĹ����з�������: " + strError);

                        else if (nRet == 1) // �����ظ�
                        {
                            string pathlist = String.Join(",", paths);

                            string strText = "�ο�ID '" + commentitem.RefID + "' �����ݿ��з����Ѿ���(���������ֵ�)������ע��¼��ʹ�á�\r\n" + pathlist + "\r\n\r\n����ȷ������ť���±༭��ע��Ϣ�����߸�����ʾ����ע��¼·����ȥ�޸�������ע��¼��Ϣ��";
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
        // �ο�ID���ء�����(������)�ɲο�ID���ء�
        // �����������Զ��ų��͵�ǰ·��strOriginRecPath�ظ�֮����
        // parameters:
        //      strOriginRecPath    ������¼��·����
        //      paths   �������е�·��
        // return:
        //      -1  error
        //      0   not dup
        //      1   dup
        int SearchCommentRefIdDup(string strRefID,
            string strBiblioRecPath,
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

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڶԲο�ID '" + strRefID + "' ���в��� ...");
            stop.BeginLoop();

            try
            {
                long lRet = Channel.SearchCommentDup(
                    stop,
                    strRefID,
                    strBiblioRecPath,
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
                        strError = "ϵͳ����: SearchCommentDup() API����ֵΪ1������paths����ĳߴ�ȴ����1, ���� " + paths.Length.ToString();
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
        int SearchCommentRefIdDup(string strRefID,
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
                long lRet = Channel.SearchComment(
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
                        strError = "ϵͳ����: SearchComment() API����ֵΪ1������paths����ĳߴ�ȴ����1, ���� " + paths.Length.ToString();
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


        void menu_newComment_Click(object sender, EventArgs e)
        {
            DoNewComment();
        }

        // ����һ����ע���Ҫ�򿪶Ի�����������ϸ��Ϣ
        void DoNewComment(/*string strIndex*/)
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
                this.Items = new CommentItemCollection();

            Debug.Assert(this.Items != null, "");

            bool bOldChanged = this.Items.Changed;

#if NO
            if (String.IsNullOrEmpty(strIndex) == false)
            {

                // �Ե�ǰ�����ڽ��б�Ų���
                CommentItem dupitem = this.CommentItems.GetItemByIndex(
                    strIndex,
                    null);
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "�������ı�� '" + strIndex + "' �ͱ�����δ�ύ֮һɾ��������ء��������ύ����֮�޸ģ��ٽ�������ע������";
                    else
                        strText = "�������ı�� '" + strIndex + "' �ڱ������Ѿ����ڡ�";

                    // ������δ����
                    DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
        strText + "\r\n\r\nҪ�������Ѵ��ڱ�Ž����޸���",
        "CommentControl",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button2);

                    // תΪ�޸�
                    if (result == DialogResult.Yes)
                    {
                        ModifyComment(dupitem);
                        return;
                    }

                    // ͻ����ʾ���Ա������Ա�۲������Ѿ����ڵļ�¼
                    dupitem.HilightListViewItem(true);
                    return;
                }

                // ��(����)������ע��¼���б�Ų���
                if (true)
                {
                    string strCommentText = "";
                    string strBiblioText = "";
                    nRet = SearchCommentIndex(strIndex,
                        this.BiblioRecPath,
                        out strCommentText,
                        out strBiblioText,
                        out strError);
                    if (nRet == -1)
                        MessageBox.Show(ForegroundWindow.Instance, "�Ա�� '" + strIndex + "' ���в��صĹ����з�������: " + strError);
                    else if (nRet == 1) // �����ظ�
                    {
                        OrderIndexFoundDupDlg dlg = new OrderIndexFoundDupDlg();
                        MainForm.SetControlFont(dlg, this.Font, false);
                        dlg.MainForm = this.MainForm;
                        dlg.BiblioText = strBiblioText;
                        dlg.OrderText = strCommentText;
                        dlg.MessageText = "�������ı�� '" + strIndex + "' �����ݿ��з����Ѿ����ڡ�����޷�������";
                        dlg.ShowDialog(this);
                        return;
                    }
                }

            } // end of ' if (String.IsNullOrEmpty(strIndex) == false)
#endif

            CommentItem commentitem = new CommentItem();

            // ����ȱʡֵ
            nRet = SetItemDefaultValues(
                "comment_normalRegister_default",
                true,
                commentitem,
                out strError);
            if (nRet == -1)
            {
                strError = "����ȱʡֵ��ʱ��������: " + strError;
                goto ERROR1;
            }

#if NO
            commentitem.Index = strIndex;
#endif
            commentitem.Parent = Global.GetRecordID(this.BiblioRecPath);

            // �ȼ����б�
            this.Items.Add(commentitem);
            commentitem.ItemDisplayState = ItemDisplayState.New;
            commentitem.AddToListView(this.listView);
            commentitem.HilightListViewItem(true);

            commentitem.Changed = true;    // ��Ϊ�����������������ζ����޸Ĺ����������Ա��⼯����ֻ��һ�����������ʱ�򣬼��ϵ�changedֵ����


            CommentEditForm edit = new CommentEditForm();

            edit.BiblioDbName = Global.GetDbName(this.BiblioRecPath);
            edit.Text = "������ע����";
            edit.MainForm = this.MainForm;
            nRet = edit.InitialForEdit(commentitem,
                this.Items,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            //REDO:
            this.MainForm.AppInfo.LinkFormState(edit, "CommentEditForm_state");
            edit.ShowDialog(this);
            this.MainForm.AppInfo.UnlinkFormState(edit);

            if (edit.DialogResult != DialogResult.OK
                && edit.Item == commentitem    // ������δǰ���ƶ��������ƶ��ص���㣬Ȼ��Cancel
                )
            {
                this.Items.PhysicalDeleteItem(commentitem);

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

            RefreshOrderSuggestionPie();
            DoViewComment(false);

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



            // Ҫ�Ա��ֺ��������ʵ�����б�Ų��ء�
            // ������ˣ�Ҫ���ִ��ڣ��Ա��޸ġ�����������Ƕȣ���������ڶԻ���ر�ǰ����
            // �������´򿪶Ի���
            string strRefID = commentitem.RefID;
            if (String.IsNullOrEmpty(strRefID) == false)
            {

                // ��Ҫ�ų����ռ�����Լ�: commentitem��
                List<BookItemBase> excludeItems = new List<BookItemBase>();
                excludeItems.Add(commentitem);

                // �Ե�ǰ�����ڽ��б�Ų���
                CommentItem dupitem = this.Items.GetItemByRefID(
                    strRefID,
                    excludeItems) as CommentItem;
                if (dupitem != null)
                {
                    string strText = "";
                    if (dupitem.ItemDisplayState == ItemDisplayState.Deleted)
                        strText = "�������Ĳο�ID '" + strRefID + "' �ͱ�����δ�ύ֮һɾ���ο�ID���ء��������ύ����֮�޸ģ��ٽ���������ע������";
                    else
                        strText = "�������Ĳο�ID '" + strRefID + "' �ڱ������Ѿ����ڡ�";

                    // ������δ����
                    DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
        strText + "\r\n\r\nҪ�������¼�¼�Ĳο�ID�����޸���\r\n(Yes �����޸�; No ���޸ģ��÷����ظ����¼�¼�����б�; Cancel �����ոմ������¼�¼)",
        "CommentControl",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button1);

                    // תΪ�޸�
                    if (result == DialogResult.Yes)
                    {
                        ModifyComment(commentitem);
                        return;
                    }

                    // �����ոմ����ļ�¼
                    if (result == DialogResult.Cancel)
                    {
                        this.Items.PhysicalDeleteItem(commentitem);

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
            } // end of ' if (String.IsNullOrEmpty(strIndex) == false)

            return;

        ERROR1:
            MessageBox.Show(ForegroundWindow.Instance, strError);
            return;
        }

#if NO
        // ������ע��š������±�Ų��ء�
        // ע������strRefID�޷������ע��¼�����������Ŀ��¼·������
        int SearchCommentRefID(string strRefID,
            string strBiblioRecPath,
            out string strOrderText,
            out string strBiblioText,
            out string strError)
        {
            strError = "";
            strOrderText = "";
            strBiblioText = "";

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڶԲο�ID '" + strRefID + "' ���в��� ...");
            stop.BeginLoop();

            try
            {
                byte[] comment_timestamp = null;
                string strCommentRecPath = "";
                string strOutputBiblioRecPath = "";

                long lRet = Channel.GetCommentInfo(
                    stop,
                    strRefID,
                    strBiblioRecPath,
                    "html",
                    out strOrderText,
                    out strCommentRecPath,
                    out comment_timestamp,
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

#if NO
        // �ı����
        // ���޸���ע��Ϣ��<parent>Ԫ�����ݣ�ʹָ������һ����Ŀ��¼
        void menu_changeParent_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (this.ListView.SelectedItems.Count == 0)
            {
                strError = "��δָ��Ҫ�޸Ĺ���������";
                goto ERROR1;
            }

            // TODO: �������δ�����,�Ƿ�Ҫ�����ȱ���?

            string strNewBiblioRecPath = InputDlg.GetInput(
                this,
                "��ָ���µ���Ŀ��¼·��",
                "��Ŀ��¼·��(��ʽ'����/ID'): ",
                "",
            this.MainForm.DefaultFont);

            if (strNewBiblioRecPath == null)
                return;

            // TODO: ��ü��һ�����·���ĸ�ʽ���Ϸ�����Ŀ����������MainForm���ҵ�

            if (String.IsNullOrEmpty(strNewBiblioRecPath) == true)
            {
                strError = "��δָ���µ���Ŀ��¼·������������";
                goto ERROR1;
            }

            if (strNewBiblioRecPath == this.BiblioRecPath)
            {
                strError = "ָ��������Ŀ��¼·���͵�ǰ��Ŀ��¼·����ͬ����������";
                goto ERROR1;
            }

            List<CommentItem> selectedcommentitems = new List<CommentItem>();
            foreach (ListViewItem item in this.ListView.SelectedItems)
            {
                CommentItem commentitem = (CommentItem)item.Tag;

                selectedcommentitems.Add(commentitem);
            }

            EntityInfo[] comments = null;

            nRet = BuildChangeParentRequestComments(
                selectedcommentitems,
                strNewBiblioRecPath,
                out comments,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (comments == null || comments.Length == 0)
                return; // û�б�Ҫ����

#if NO
            EntityInfo[] errorinfos = null;
            nRet = SaveCommentRecords(strNewBiblioRecPath,
                entities,
                out errorinfos,
                out strError);

            // �ѳ�����������Ҫ����״̬��������ֵ���ʾ���ڴ�
            // �Ƿ��������ѹ����Ѿ��ı�������ų���listview?
            RefreshOperResult(errorinfos);

            if (nRet == -1)
            {
                goto ERROR1;
            }
#endif
            nRet = SaveComments(comments, out strError);
            if (nRet == -1)
                goto ERROR1;

            this.MainForm.StatusBarMessage = "��ע��Ϣ �޸Ĺ��� �ɹ�";
            return;
        ERROR1:
            MessageBox.Show(ForegroundWindow.Instance, strError);
        }
#endif

        // ɾ��һ��������ע����
        void menu_deleteComment_Click(object sender, EventArgs e)
        {
            if (this.listView.SelectedIndices.Count == 0)
            {
                MessageBox.Show(ForegroundWindow.Instance, "��δѡ��Ҫ���ɾ��������");
                return;
            }

            string strIndexList = "";
            for (int i = 0; i < this.listView.SelectedItems.Count; i++)
            {
                if (i > 20)
                {
                    strIndexList += "...(�� " + this.listView.SelectedItems.Count.ToString() + " ��)";
                    break;
                }
                string strIndex = this.listView.SelectedItems[i].Text;
                strIndexList += strIndex + "\r\n";
            }

            string strWarningText = "����(��ŵ�)��ע��������ɾ��: \r\n" + strIndexList + "\r\n\r\nȷʵҪ���ɾ������?";

            // ����
            DialogResult result = MessageBox.Show(ForegroundWindow.Instance,
                strWarningText,
                "CommentControl",
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
                    CommentItem commentitem = (CommentItem)item.Tag;

                    int nRet = MaskDeleteItem(commentitem,
                        m_bRemoveDeletedItem);

                    if (nRet == 0)
                    {
                        if (strNotDeleteList != "")
                            strNotDeleteList += ",";
                        strNotDeleteList += commentitem.Index;
                        continue;
                    }

                    if (string.IsNullOrEmpty(commentitem.RecPath) == false)
                        deleted_recpaths.Add(commentitem.RecPath);

                    nDeleteCount++;
                }

                string strText = "";

                if (strNotDeleteList != "")
                    strText += "���Ϊ '" + strNotDeleteList + "' ����ע����δ�ܼ��Ա��ɾ����\r\n\r\n";

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

#if NO
        // ���ɾ������
        // return:
        //      0   ��Ϊ�в���Ϣ��δ�ܱ��ɾ��
        //      1   �ɹ�ɾ��
        int MaskDeleteItem(CommentItem commentitem,
            bool bRemoveDeletedItem)
        {
            this.Items.MaskDeleteItem(bRemoveDeletedItem,
                commentitem);
            return 1;
        }
#endif

        // ����ɾ��һ��������ע����
        void menu_undoDeleteComment_Click(object sender, EventArgs e)
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
                    CommentItem commentitem = (CommentItem)item.Tag;

                    bool bRet = this.Items.UndoMaskDeleteItem(commentitem);

                    if (bRet == false)
                    {
                        if (strNotUndoList != "")
                            strNotUndoList += ",";
                        strNotUndoList += commentitem.Index;
                        continue;
                    }

                    nUndoCount++;
                }

                string strText = "";

                if (strNotUndoList != "")
                    strText += "���Ϊ '" + strNotUndoList + "' ��������ǰ��δ�����ɾ����, ��������̸���ϳ���ɾ����\r\n\r\n";

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
                if (bOldChanged != this.Items.Changed)
                TriggerContentChanged(bOldChanged, this.Items.Changed);

            }
            finally
            {
                this.EnableControls(true);
            }
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            // menu_modifyComment_Click(this, null);
            DoViewComment(true);
        }

        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            DoViewComment(false);
        }

        private void comboBox_libraryCodeFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshOrderSuggestionPie();
        }

        private void pieChartControl1_SizeChanged(object sender, EventArgs e)
        {
            SetPieChartMargin();
        }

        // ����PieChart�ؼ��ߴ�����margin����
        void SetPieChartMargin()
        {
            int nMinWidth = Math.Min(this.pieChartControl1.Width, this.pieChartControl1.Height);
            int nMargin = nMinWidth / 8;
            nMinWidth -= nMargin * 2;
            int nHorzMargin = (this.pieChartControl1.Width - nMinWidth) / 2;
            int nVertMargin = (this.pieChartControl1.Height - nMinWidth) / 2;
            this.pieChartControl1.LeftMargin = nHorzMargin;
            this.pieChartControl1.RightMargin = nHorzMargin;
            this.pieChartControl1.TopMargin = nVertMargin;
            this.pieChartControl1.BottomMargin = nVertMargin;
        }

        private void comboBox_libraryCodeFilter_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_libraryCodeFilter.Invalidate();
        }

        public override string ErrorInfo
        {
            get
            {
                return base.ErrorInfo;
            }
            set
            {
                base.ErrorInfo = value;
                if (this.splitContainer_main != null)
                {
                    if (string.IsNullOrEmpty(value) == true)
                        this.splitContainer_main.Visible = true;
                    else
                        this.splitContainer_main.Visible = false;
                }
            }
        }

    }

    /// <summary>
    /// ������ɴ��¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void AddSubjectEventHandler(object sender,
        AddSubjectEventArgs e);

    /// <summary>
    /// ������ɴ��¼��Ĳ���
    /// </summary>
    public class AddSubjectEventArgs : EventArgs
    {
        // 
        /// <summary>
        /// [in] ��ǰ�������ڵ��ӿؼ�
        /// </summary>
        public object FocusedControl = null;    // [in]

        /// <summary>
        /// [in] ��ע��¼�д�������������ʣ�������״̬����"�Ѵ���"�ġ����ʵ�����������ݿ��ܻᱻ�޸�
        /// </summary>
        public List<string> NewSubjects = null;    // [in] ��ע��¼�д�������������ʣ�������״̬����"�Ѵ���"�ġ����ʵ�����������ݿ��ܻᱻ�޸�
        
        /// <summary>
        /// [in] ��ע��¼�д���������ȫ������ʣ�����״̬����"�Ѵ���"�ġ�
        /// </summary>
        public List<string> HiddenSubjects = null;    // [in] ��ע��¼�д���������ȫ������ʣ�����״̬����"�Ѵ���"�ġ�

        /// <summary>
        /// [out] �Ƿ�Ҫ���� CommentControl �ĺ��������� ��������ָ�޸���ע��¼״̬�Ĳ���
        /// </summary>
        public bool Canceled = false;           // [out] �Ƿ�Ҫ���� CommentControl �ĺ��������� ��������ָ�޸���ע��¼״̬�Ĳ���

        /// <summary>
        /// [in] �Ƿ�Ҫ���¼������������ʾ���� MessageBox
        /// </summary>
        public bool ShowErrorBox = true;        // [in]�Ƿ�Ҫ���¼������������ʾ����MessageBox

        /// <summary>
        /// [out] ������Ϣ���¼���������з�������ʱ��ʹ�ô˳�Ա
        /// </summary>
        public string ErrorInfo = "";           // [out]������Ϣ���¼���������з�������
    }

    // �����������д����ͼ���������ֹ���
    /// <summary>
    /// ConmentControl ��Ļ�����
    /// </summary>
    public class CommentControlBase : ItemControlBase<CommentItem, CommentItemCollection>
    {
    }
}
