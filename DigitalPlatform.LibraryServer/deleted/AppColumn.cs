using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Net.Mail;
using System.Web;

using DigitalPlatform;	// Stop��
using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Script;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Marc;
using DigitalPlatform.Range;

using DigitalPlatform.Message;
using DigitalPlatform.rms.Client.rmsws_localhost;

namespace DigitalPlatform.LibraryServer
{
#if NOOOOOOOOOOOOOOOOOO
    /// <summary>
    /// �������Ǻ���Ŀ�洢�����йصĴ���
    /// </summary>
    public partial class LibraryApplication
    {
        // CommentColumnר���������ƴ��������¡���ʾʱ��Դ洢�ṹ�ĺ�۴�ȡ����
        public ReaderWriterLock m_lockCommentColumn = new ReaderWriterLock();
        public int m_nCommentColumnLockTimeout = 5000;	// 5000=5��

        // �洢�ṹ
        public ColumnStorage CommentColumn = null;

        // �洢�ļ���
        string StorageFileName = "";

        private void CloseCommentColumn()
        {
            if (this.CommentColumn != null)
            {
                string strTemp1;
                string strTemp2;
                this.CommentColumn.Detach(out strTemp1,
                    out strTemp2);
                this.CommentColumn = null;
            }
        }

        // װ����Ŀ�洢
        private int LoadCommentColumn(
            string strStorageFileName,
            out string strError)
        {
            strError = "";

            this.StorageFileName = strStorageFileName;

            try
            {
                if (this.CommentColumn == null)
                    this.CommentColumn = new ColumnStorage();
                else
                {
                    // 2006/7/6
                    string strTemp1;
                    string strTemp2;
                    this.CommentColumn.Detach(out strTemp1,
                        out strTemp2);
                    this.CommentColumn = null;

                    this.CommentColumn = new ColumnStorage();
                }

                try
                {
                    this.CommentColumn.Attach(strStorageFileName,
                        strStorageFileName + ".index");
                }
                catch (Exception ex)
                {
                    strError = "Attach �ļ� " + strStorageFileName + " ������ʧ�� :" + ex.Message;
                    return -1;
                }
                return 0;
            }
            catch /*(System.ApplicationException ex)*/
            {
                strError = "��Ŀ��ʱ�����������Ժ����ԡ�";
                return -1;
            }
        }

        // [�ⲿ����]
        // �����ڴ������洢����
        public int CreateCommentColumn(
            SessionInfo sessioninfo,
            System.Web.UI.Page page,
            out string strError)
        {
            this.m_lockCommentColumn.AcquireWriterLock(m_nCommentColumnLockTimeout);
            try
            {
                strError = "";
                int nRet = 0;

                if (sessioninfo.Account == null
    || StringUtil.IsInList("managecache", sessioninfo.RightsOrigin) == false)
                {
                    strError = "��ǰ�ʻ����߱� managecache Ȩ�ޣ����ܴ�����Ŀ����";
                    return -1;
                }

                this.CloseCommentColumn();

                if (page != null
                    && page.Response.IsClientConnected == false)	// �����ж�
                {
                    strError = "�ж�";
                    return -1;
                }

                if (this.CommentColumn == null)
                    this.CommentColumn = new ColumnStorage();

                this.CommentColumn.ReadOnly = false;
                this.CommentColumn.m_strBigFileName = this.StorageFileName;
                this.CommentColumn.m_strSmallFileName = this.StorageFileName + ".index";

                this.CommentColumn.Open(true);
                this.CommentColumn.Clear();
                // ����
                nRet = SearchTopLevelArticles(
                    sessioninfo,
                    page,
                    out strError);
                if (nRet == -1)
                    return -1;

                // ����
                if (page != null)
                {
                    page.Response.Write("--- begin sort ...<br/>");
                    page.Response.Flush();
                }

                DateTime time = DateTime.Now;

                this.CommentColumn.Sort();

                if (page != null)
                {
                    TimeSpan delta = DateTime.Now - time;
                    page.Response.Write("sort end. time=" + delta.ToString() + "<br/>");
                    page.Response.Flush();
                }

                // ���������ļ�
                string strTemp1;
                string strTemp2;
                this.CommentColumn.Detach(out strTemp1,
                    out strTemp2);

                this.CommentColumn.ReadOnly = true;

                this.CloseCommentColumn();

                // ����װ��
                nRet = LoadCommentColumn(
                    this.StorageFileName,
                    out strError);
                if (nRet == -1)
                    return -1;

                return 0;
            }
            finally
            {
                this.m_lockCommentColumn.ReleaseWriterLock();
            }
        }

        // ������������
        // return:
        //		-1	error
        //		���� ������
        private int SearchTopLevelArticles(
            SessionInfo sessioninfo,
            System.Web.UI.Page page,
            out string strError)
        {
            strError = "";

            if (page != null
                && page.Response.IsClientConnected == false)	// �����ж�
            {
                strError = "�ж�";
                return -1;
            }

            // ����ȫ����ע���� һ��ʱ�䷶Χ�ڵ�?
            List<string> dbnames = new List<string>();
            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                ItemDbCfg cfg = this.ItemDbs[i];
                string strDbName = cfg.CommentDbName;
                if (String.IsNullOrEmpty(strDbName) == true)
                    continue;
                dbnames.Add(strDbName);
            }

            DateTime now = DateTime.Now;
            DateTime oneyearbefore = now - new TimeSpan(365, 0, 0, 0);
            string strTime = DateTimeUtil.Rfc1123DateTimeString(oneyearbefore.ToUniversalTime());

            string strQueryXml = "";
            for (int i = 0; i < dbnames.Count; i++)
            {
                string strDbName = dbnames[i];
                string strOneQueryXml = "<target list='" + strDbName + ":" + "����޸�ʱ��'><item><word>"    // <order>DESC</order>
                    + strTime + "</word><match>exact</match><relation>" + StringUtil.GetXmlStringSimple(">=") + "</relation><dataType>number</dataType><maxCount>"
                    + "-1"// Convert.ToString(m_nMaxLineCount)
                    + "</maxCount></item><lang>zh</lang></target>";

                if (i > 0)
                    strQueryXml += "<operator value='OR' />";
                strQueryXml += strOneQueryXml;
            }

            if (dbnames.Count > 0)
                strQueryXml = "<group>" + strQueryXml + "</group>";

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            Debug.Assert(channel != null, "Channels.GetChannel �쳣");

            if (page != null)
            {
                page.Response.Write("--- begin search ...<br/>");
                page.Response.Flush();
            }

            DateTime time = DateTime.Now;

            long nRet = channel.DoSearch(strQueryXml,
                "default",
                out strError);
            if (nRet == -1)
            {
                strError = "����ʱ����: " + strError;
                return -1;
            }

                TimeSpan delta = DateTime.Now - time;
            if (page != null)
            {
                page.Response.Write("search end. hitcount=" + nRet.ToString() + ", time=" + delta.ToString() + "<br/>");
                page.Response.Flush();
            }


            if (nRet == 0)
                return 0;	// not found



            if (page != null
                && page.Response.IsClientConnected == false)	// �����ж�
            {
                strError = "�ж�";
                return -1;
            }
            if (page != null)
            {
                page.Response.Write("--- begin get search result ...<br/>");
                page.Response.Flush();
            }

            time = DateTime.Now;

            List<string> aPath = null;
            nRet = channel.DoGetSearchResult(
                "default",
                -1,
                "zh",
                null,	// stop,
                out aPath,
                out strError);
            if (nRet == -1)
            {
                strError = "��ü������ʱ����: " + strError;
                return -1;
            }

            if (page != null)
            {
                delta = DateTime.Now - time;
                page.Response.Write("get search result end. lines=" + aPath.Count.ToString() + ", time=" + delta.ToString() + "<br/>");
                page.Response.Flush();
            }


            if (aPath.Count == 0)
            {
                strError = "��ȡ�ļ������Ϊ��";
                return -1;
            }

            if (page != null
                && page.Response.IsClientConnected == false)	// �����ж�
            {
                strError = "�ж�";
                return -1;
            }


            if (page != null)
            {
                page.Response.Write("--- begin build storage ...<br/>");
                page.Response.Flush();
            }

            time = DateTime.Now;


            this.CommentColumn.Clear();	// ��ռ���

            // �������ж������ж����У�ֻ��ʼ����m_strRecPath����
            for (int i = 0; i < Math.Min(aPath.Count, 1000000); i++)	// <Math.Min(aPath.Count, 10)
            {
                Line line = new Line();
                // line.Container = this;
                line.m_strRecPath = aPath[i];

                nRet = line.InitialInfo(
                    page,
                    channel,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 1)
                    return -1;	// �����ж�


                TopArticleItem item = new TopArticleItem();
                item.Line = line;
                this.CommentColumn.Add(item);

                if (page != null 
                    && (i % 100) == 0)
                {
                    page.Response.Write("process " + Convert.ToString(i) + "<br/>");
                    page.Response.Flush();
                }

            }

            if (page != null)
            {
                delta = DateTime.Now - time;
                page.Response.Write("build storage end. time=" + delta.ToString() + "<br/>");
                page.Response.Flush();
            }

            return 1;
        }

        // [�ⲿ����]
        // �޸���ע��¼�󣬸�����Ŀ�洢�ṹ
        // parameters:
        //      strAction   ������change/delete/new
        // return:
        //      -2   ��Ŀ������δ����,����޴Ӹ���
        //		-1	error
        //		0	not found line object
        //		1	succeed
        public int UpdateLine(
            System.Web.UI.Page page,
            string strAction,
            string strRecPath,
            string strXml,
            out string strError)
        {
            strError = "";

            if (this.CommentColumn == null
    || this.CommentColumn.Opened == false)
            {
                strError = "��δ������Ŀ����...";
                return -2;
            }

            this.m_lockCommentColumn.AcquireWriterLock(m_nCommentColumnLockTimeout);
            try
            {


                int nIndex = -1;
                int i = 0;
                Line line = null;

                if (strAction == "change" || strAction == "delete")
                {
                    // ��Storage����
                    // ��Ҫд����
                    for (i = 0; i < this.CommentColumn.Count; i++)
                    {
                        string strCurrentRecPath = this.CommentColumn.GetItemRecPath(i);
                        if (strCurrentRecPath == strRecPath)
                        {
                            nIndex = i;
                            line = ((TopArticleItem)this.CommentColumn[nIndex]).Line;
                            Debug.Assert(line.m_strRecPath == strRecPath, "");

                            this.CommentColumn.RemoveAt(nIndex);

                            if (strAction == "delete")
                                return 1;
                            break;  //  goto FOUND;
                        }
                    }

                    if (strAction == "delete")
                        return 0;
                }
                else if (strAction == "new")
                {
                    line = new Line();
                    line.m_strRecPath = strRecPath;
                }
                else
                {
                    strError = "δ֪��strActionֵ '" + strAction + "'";
                    return -1;
                }

            // FOUND:
                if (strAction == "delete")
                    return 1;

                int nRet = line.ProcessXml(
                    null,	// page,	����������Ҫ���������������ж�
                    strXml,
                    out strError);
                if (nRet == -1)
                {
                    // �������д�������־
                    this.WriteErrorLog("��UpdateLine()�����У�����line.ProcessXml()��������, ��¼·��=" + line.m_strRecPath + ")���⽫������Ŀ��ҳ���У��ü�¼����ʾ�ж�ʧ����ϸԭ��" + strError);
                    return -1;
                }

                {
                    // �������λ����Storage��Χ��
                    TopArticleItem item = new TopArticleItem();
                    item.Line = line;
                    this.CommentColumn.Insert(0,
                        item);
                }

                return 1;
            }
            finally
            {
                this.m_lockCommentColumn.ReleaseWriterLock();
            }
        }
    }

#endif
}
