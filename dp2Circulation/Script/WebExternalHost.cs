#define SINGLE_CHANNEL
// #define USE_LOCK

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Security.Permissions;
using System.Threading;
using System.Diagnostics;
using System.Xml;

using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.CommonControl;
using System.Web;
using System.IO;


namespace dp2Circulation
{
    /// <summary>
    /// ���ں�������ؼ��ӿڵ�������
    /// </summary>
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public class WebExternalHost : ThreadBase, IDisposable
    {
        /// <summary>
        /// ���������Ϣ���¼�
        /// </summary>
        public event OutputDebugInfoEventHandler OutputDebugInfo = null;

        /// <summary>
        /// ������ WebBrowser
        /// </summary>
        public WebBrowser WebBrowser = null;
        /// <summary>
        /// ������
        /// </summary>
        public bool DisplayMessage = true; 

        /// <summary>
        /// �����Դ�ı����ļ�·��
        /// </summary>
        public event GetLocalFilePathEventHandler GetLocalPath = null;

        /// <summary>
        /// ������ؼ��Ƿ����� Hover Window
        /// </summary>
        public bool IsBelongToHoverWindow = false; // �Լ��Ƿ������hover window

        bool m_bLoop = false;

        int m_inSearch = 0;

#if USE_LOCK
        internal ReaderWriterLock m_lock = new ReaderWriterLock();
        internal static int m_nLockTimeout = 5000;	// 5000=5��
#endif

        /// <summary>
        /// ͨѶͨ���Ƿ�����ʹ����
        /// </summary>
        public bool ChannelInUse
        {
            get
            {
                if (this.m_inSearch > 0)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// ��ǰ�Ƿ�����ѭ����
        /// </summary>
        public bool IsInLoop
        {
            get
            {
                return this.m_bLoop;
            }
            set
            {
                this.m_bLoop = value;
            }
        }

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;

#if SINGLE_CHANNEL
        /// <summary>
        /// ͨѶͨ��
        /// </summary>
        public LibraryChannel Channel = new LibraryChannel();
#else
        public LibraryChannelCollection Channels = null;
#endif

        /// <summary>
        /// ���Դ���
        /// </summary>
        public string Lang = "zh";

        /// <summary>
        /// �ͷ���Դ
        /// </summary>
        public void Dispose()
        {
            this.Clear();
            this.StopThread(true);
        }

        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="mainform">��ܴ���</param>
        /// <param name="bDisplayMessage">�Ƿ���ʾ��Ϣ��ȱʡΪ false</param>
        public void Initial(MainForm mainform,
            WebBrowser webBrowser,
            bool bDisplayMessage = false)
        {
            this.MainForm = mainform;
            this.WebBrowser = webBrowser;
#if SINGLE_CHANNEL
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            this.Channel.Idle -= new IdleEventHandler(Channel_Idle);
            this.Channel.Idle += new IdleEventHandler(Channel_Idle);
#else

            this.Channels = new LibraryChannelCollection();
            this.Channels.BeforeLogin -= new BeforeLoginEventHandle(Channels_BeforeLogin);
            this.Channels.BeforeLogin += new BeforeLoginEventHandle(Channels_BeforeLogin);
#endif

            this.DisplayMessage = bDisplayMessage;

            if (bDisplayMessage == true)
            {
                stop = new DigitalPlatform.Stop();
                stop.Register(MainForm.stopManager, true);	// ����������
            }

            // this.BeginThread();
        }

        void Channel_Idle(object sender, IdleEventArgs e)
        {
            e.bDoEvents = this._doEvents;
        }

        /// <summary>
        /// �ݻٱ�����
        /// </summary>
        public void Destroy()
        {

#if SINGLE_CHANNEL

            // 2008/5/11 new add
            if (this.Channel != null)
            {
                this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
                this.IsInLoop = false;  // 2008/10/29 new add
                // this.Channel.Abort();
                this.Channel.Close();   // 2012/3/28
                this.Channel = null;
            }
#else
            CloseAllChannels();
            this.Channels = null;
#endif
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }

            this.Clear();
            this.StopThread(false);
        }

        /// <summary>
        /// ֹͣͨѶ
        /// </summary>
        public void Stop()
        {
            // this.IsInLoop = false;  // 2008/10/29 new add

            this.Clear();

#if SINGLE_CHANNEL

            // 2008/5/11 new add
            if (this.Channel != null)
            {
                if (this.Channel.IsInSearching > 0)
                    this.Channel.Abort();
            }
#else
            CloseAllChannels();
            this.Channels = null;
#endif
        }

        /// <summary>
        /// ��ǰ�Ƿ���Ե����µ�������
        /// </summary>
        /// <param name="commander">Commander����</param>
        /// <param name="msg">��Ϣ</param>
        /// <returns>�Ƿ���Ե����µ�����</returns>
        public bool CanCallNew(Commander commander,
            int msg)
        {
            if (this.IsInLoop == true)
            {
                // ����֮��
                this.IsInLoop = false;
                commander.AddMessage(msg);
                return false;   // ����������
            }

            Debug.Assert(this.IsInLoop == false, "����ǰ������һ��ѭ����δֹͣ");

            if (this.ChannelInUse == true)
            {
                // ����֮��
                this.Stop();
                commander.AddMessage(msg);
                return false;   // ����������
            }

            Debug.Assert(this.ChannelInUse == false, "����ǰ����ͨ����δ�ͷ�");
            return true;    // ��������
        }

        /// <summary>
        /// ֹͣǰһ������
        /// </summary>
        public void StopPrevious()
        {
            this.IsInLoop = false;
            this.Stop();
        }

        void DoStop(object sender, StopEventArgs e)
        {
#if SINGLE_CHANNEL
            if (this.Channel != null)
                this.Channel.Abort();
#else
            CloseAllChannels();
#endif
        }

#if !SINGLE_CHANNEL

        void CloseAllChannels()
        {
            if (this.Channels == null)
                return;

            for (int i = 0; i < this.Channels.Count; i++)
            {
                LibraryChannel channel = this.Channels[i];

                channel.Abort();
            }
        }

        void ReleaseAllChannelsBut(string strID)
        {
            for (int i = 0; i < this.Channels.Count; i++)
            {
                LibraryChannel channel = this.Channels[i];

                string strCurrentID = (string)channel.Tag;
                if (strCurrentID == strID)
                    continue;

                channel.Abort();
                this.Channels.RemoveChannel(channel);
                i--;
            }

            // TODO: ��ζ�ͨ�����л������ã��Ƿ�����һ��ʱ����ĳ�Ա����һ��ʱ�������?
        }

        LibraryChannel GetChannelByID(string strID)
        {
            if (this.Channels == null)
                return null;
            LibraryChannel channel = null;
            for (int i = 0; i < this.Channels.Count; i++)
            {
                channel = this.Channels[i];
                string strCurrentID = (string)channel.Tag;
                if (strCurrentID == strID)
                    return channel;
            }
            channel = this.Channels.NewChannel(MainForm.LibraryServerUrl);
            channel.Tag = strID;
            return channel;
           
        }
#endif

#if SINGLE_CHANNEL

        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            MainForm.Channel_BeforeLogin(this, e);
        }
#else
        void Channels_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            /*
            LibraryChannel channel = (LibraryChannel)sender;

            string strUrl = channel.Url;

            e.LibraryServerUrl = strUrl;
             * */

            MainForm.Channel_BeforeLogin(this, e);
        }
#endif

        /// <summary>
        /// ��ʼѭ��
        /// </summary>
        public void BeginLoop()
        {
            this.m_bLoop = true;
        }

        /// <summary>
        /// ����ѭ��
        /// </summary>
        public void EndLoop()
        {
            this.m_bLoop = false;
        }

        int m_nInHoverProperty = 0;

        /// <summary>
        /// ��ʾ������
        /// </summary>
        /// <param name="strItemBarcode">�������</param>
        public void HoverItemProperty(string strItemBarcode)
        {
            this.m_nInHoverProperty++;
            if (this.m_nInHoverProperty > 1)
            {
                this.m_nInHoverProperty--;
                return;
            }

            try
            {
                if (this.IsBelongToHoverWindow == true)
                    return;

                if (this.MainForm.CanDisplayItemProperty() == false)
                    return;

                if (this.MainForm.GetItemPropertyTitle() == strItemBarcode)
                    return; // �Ż�

                if (string.IsNullOrEmpty(strItemBarcode) == true)
                {
                    this.MainForm.DisplayItemProperty("",
        "",
        "");
                    return;
                }

                string strError = "";

                if (stop != null)
                {
                    stop.OnStop += new StopEventHandler(this.DoStop);
                    stop.SetMessage("���ڻ�ȡ����Ϣ '" + strItemBarcode + "' ...");
                    stop.BeginLoop();
                }

                try
                {
                    // Application.DoEvents();

                    this.m_inSearch++;

#if SINGLE_CHANNEL
                    // ��Ϊ������ֻ��һ��Channelͨ��������Ҫ����ʹ��
                    if (this.m_inSearch > 1)
                    {
                        /*
                        strError = "Channel��ռ��";
                        goto ERROR1;
                         * */
                        this.m_inSearch--;
                        return;
                    }
                    //// LibraryChannel channel = this.Channel;
#else
                LibraryChannel channel = GetChannelByID(strIdString);
#endif

                    try
                    {
                        string strItemText = "";
                        string strBiblioText = "";

                        string strItemRecPath = "";
                        string strBiblioRecPath = "";

                        byte[] item_timestamp = null;

                        long lRet = this.Channel.GetItemInfo(
                            stop,
                            strItemBarcode,
                            "html",
                            out strItemText,
                            out strItemRecPath,
                            out item_timestamp,
                            "",
                            out strBiblioText,
                            out strBiblioRecPath,
                            out strError);
                        if (lRet == 0)
                        {
                            strError = "������� '" + strItemBarcode + "' û���ҵ�";
                            goto ERROR1;
                        }
                        if (lRet == -1)
                            goto ERROR1;

                        string strXml = "";
                        lRet = this.Channel.GetItemInfo(
        stop,
        strItemBarcode,
        "xml",
        out strXml,
        out strItemRecPath,
        out item_timestamp,
        "",
        out strBiblioText,
        out strBiblioRecPath,
        out strError);

                        this.MainForm.DisplayItemProperty(strItemBarcode,
                            strItemText,
                            strXml);

                        return;
                    }
                    catch
                    {
                        // return "GetObjectFilePath()�쳣: " + ex.Message;
                        throw;
                    }
                    finally
                    {
                        this.m_inSearch--;
                    }

                }
                finally
                {
                    if (stop != null)
                    {
                        stop.EndLoop();
                        stop.OnStop -= new StopEventHandler(this.DoStop);
                        stop.Initial("");
                    }
                }

            ERROR1:

                this.MainForm.DisplayItemProperty("error",
                    strError,
                    "");
            }
            finally
            {
                this.m_nInHoverProperty--;
            }
        }

        // 
        /// <summary>
        /// �� MDI �Ӵ���
        /// </summary>
        /// <param name="strFormName">�������ơ�ItemInfoForm / EntityForm / ReaderInfoForm</param>
        /// <param name="strParameter">�����ַ���</param>
        /// <param name="bOpenNew">�Ƿ���µĴ���</param>
        public void OpenForm(string strFormName, 
            string strParameter,
            bool bOpenNew)
        {
            if (strFormName == "ItemInfoForm")
            {
                ItemInfoForm form = null;
                if (bOpenNew == false)
                {
                    form = this.MainForm.EnsureItemInfoForm();
                    Global.Activate(form);
                }
                else
                {
                    form = new ItemInfoForm();
                    form.MainForm = this.MainForm;
                    form.MdiParent = this.MainForm;
                    form.Show();
                }
                form.LoadRecord(strParameter);  // �ò������װ��
                return;
            }

            if (strFormName == "EntityForm")
            {
                EntityForm form = null;
                if (bOpenNew == false)
                {
                    form = this.MainForm.EnsureEntityForm();
                    Global.Activate(form);
                }
                else
                {
                    form = new EntityForm();
                    form.MainForm = this.MainForm;
                    form.MdiParent = this.MainForm;
                    form.Show();
                }
                form.LoadItemByBarcode(strParameter, false);  // �ò������װ��
                return;
            }

            if (strFormName == "ReaderInfoForm")
            {
                ReaderInfoForm form = null;
                if (bOpenNew == false)
                {
                    form = this.MainForm.EnsureReaderInfoForm();
                    Global.Activate(form);
                }
                else
                {
                    form = new ReaderInfoForm();
                    form.MainForm = this.MainForm;
                    form.MdiParent = this.MainForm;
                    form.Show();
                }
                form.LoadRecord(strParameter,
                    false); 
                return;
            }
        }

        public void AsyncGetObjectFilePath(string strPatronBarcode,
            string strUsage,
            string strCallBackFuncName,
            object element)
        {
            // this.WebBrowser.Document.InvokeScript(strCallBackFuncName, new object[] { "state", o, "result" });
            AsyncCall call = new AsyncCall();
            call.FuncType = "AsyncGetObjectFilePath";
            call.InputParameters = new object[] { strPatronBarcode, strUsage, strCallBackFuncName, element};
            this.AddCall(call);
        }

        List<string> _tempfilenames = new List<string>();

        string GetTempFileName()
        {
            string strTempFilePath = Path.Combine(this.MainForm.UserTempDir, "~res_" + Guid.NewGuid().ToString());

            lock (this._tempfilenames)
            {
                _tempfilenames.Add(strTempFilePath);
            }

            return strTempFilePath;
        }

        // 2015/1/4
        void DeleteAllTempFiles()
        {

            foreach (string filename in this._tempfilenames)
            {
                try
                {
                    File.Delete(filename);
                }
                catch
                {
                }
            }

            lock (this._tempfilenames)
            {
                this._tempfilenames.Clear();
            }
        }

        //
        // TODO: ��ö��߼�¼XMLʱ��������Cache��Cache�Ķ��߼�¼��<dprms:file>һ�㲻���б䶯
        /// <summary>
        ///  ��ö��󱾵��ļ�·��
        /// </summary>
        /// <param name="strPatronBarcode">����֤����ţ����������ַ���</param>
        /// <param name="strUsage">��;</param>
        /// <returns>�����ļ�·��</returns>
        public string GetObjectFilePath(string strPatronBarcode,
            string strUsage)
        {

            // this.WebBrowser.Document.InvokeScript("test", new object [] {"test1", "test2"});

            /*
            if (this.IsInLoop == false)
                throw new Exception("�Ѿ�����ѭ����");
             * */
            long lRet = 0;

            string strNoneFilePath = this.MainForm.DataDir + "/nonephoto.png";

            // 2012/1/6
            if (string.IsNullOrEmpty(strPatronBarcode) == true)
                return strNoneFilePath;

            // ��ñ���ͼ����Դ
            if (strPatronBarcode == "?")
            {
                // return this.MainForm.DataDir + "/~current_unsaved_patron_photo.png";
                if (this.GetLocalPath != null)
                {
                    GetLocalFilePathEventArgs e = new GetLocalFilePathEventArgs();
                    e.Name = "PatronCardPhoto";
                    this.GetLocalPath(this, e);
                    if (e.LocalFilePath == null)
                        return strNoneFilePath;
                    if (string.IsNullOrEmpty(e.LocalFilePath) == false)
                        return e.LocalFilePath;
                }
            }

            if (this.DisplayMessage == true && stop == null)
            {
                return null;
            }

#if !SINGLE_CHANNEL

            if (this.Channels == null)
            {
                return "channels closed 2...";
            }
            ReleaseAllChannelsBut(strIdString);
#endif

            string strError = "";

#if USE_LOCK
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
#endif
            if (stop != null)
            {
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.SetMessage("���ڻ�ȡ������Ƭ '" + strPatronBarcode + "' ...");
                stop.BeginLoop();
            }

            try
            {
                // Application.DoEvents();

#if SINGLE_CHANNEL
                // ��Ϊ������ֻ��һ��Channelͨ��������Ҫ����ʹ��
                if (this.m_inSearch > 0)
                {
                    return "Channel��ռ��";
                }
                //// LibraryChannel channel = this.Channel;
#else
                LibraryChannel channel = GetChannelByID(strIdString);
#endif

                this.m_inSearch++;
                try
                {
                    string strResPath = "";
                    if (StringUtil.HasHead(strPatronBarcode, "object-path:") == true)
                    {
                        // ����ֱ�ӻ��ͼ�����
                        strResPath = strPatronBarcode.Substring("object-path:".Length);
                        if (string.IsNullOrEmpty(strResPath) == true)
                            return strNoneFilePath;
                    }
                    else
                    {
                        // ��Ҫ��ȡ�ö��߼�¼Ȼ���ٻ��ͼ�����
                        string strXml = "";
                        string strOutputPath = "";

                        // ��û����еĶ��߼�¼XML
                        // return:
                        //      -1  ����
                        //      0   û���ҵ�
                        //      1   �ҵ�
                        int nRet = this.MainForm.GetCachedReaderXml(strPatronBarcode,
                            "",
        out strXml,
        out strOutputPath,
        out strError);
                        if (nRet == -1)
                        {
                            throw new Exception(strError);
                            // return strError;
                        }

                        if (nRet == 0)
                        {

                            string[] results = null;
                            byte[] baTimestamp = null;

                            lRet = Channel.GetReaderInfo(stop,
                                strPatronBarcode,
                                "xml",
                                out results,
                                out strOutputPath,
                                out baTimestamp,
                                out strError);
                            if (lRet == -1)
                            {
                                throw new Exception(strError);
                                // return strError;
                            }
                            else if (lRet > 1)
                            {
                                strError = "����֤����� " + strPatronBarcode + " ���ظ���¼ " + lRet.ToString() + "��";
                                throw new Exception(strError);
                                // return strError;
                            }

                            Debug.Assert(results.Length > 0, "");
                            strXml = results[0];

                            // ���뵽����
                            this.MainForm.SetReaderXmlCache(strPatronBarcode,
                                "",
                                strXml,
                                strOutputPath);
                        }

                        XmlDocument dom = new XmlDocument();
                        try
                        {
                            dom.LoadXml(strXml);
                        }
                        catch (Exception ex)
                        {
                            strError = "���߼�¼XMLװ��DOMʱ����: " + ex.Message;
                            throw new Exception(strError);
                            // return strError;
                        }


                        XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
                        nsmgr.AddNamespace("dprms", DpNs.dprms);

                        XmlNodeList nodes = null;
                        if (string.IsNullOrEmpty(strUsage) == false)
                            nodes = dom.DocumentElement.SelectNodes("//dprms:file[@usage='" + strUsage + "']", nsmgr);
                        else
                            nodes = dom.DocumentElement.SelectNodes("//dprms:file", nsmgr);

                        if (nodes.Count == 0)
                        {
                            return strNoneFilePath;
                        }

                        string strID = DomUtil.GetAttr(nodes[0], "id");
                        if (string.IsNullOrEmpty(strID) == true)
                            return null;

                        strResPath = strOutputPath + "/object/" + strID;
                        strResPath = strResPath.Replace(":", "/");

                    }

                    // string strTempFilePath = this.MainForm.DataDir + "/~temp_obj";

                    string strTempFilePath = GetTempFileName();
                    // TODO: �Ƿ���Խ������� cache ����

                    byte[] baOutputTimeStamp = null;

                    // EnableControlsInLoading(true);

                    string strMetaData = "";
                    string strTempOutputPath = "";

                    lRet = this.Channel.GetRes(
                        stop,
                        strResPath,
                        strTempFilePath,
                        out strMetaData,
                        out baOutputTimeStamp,
                        out strTempOutputPath,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "������Դ�ļ�ʧ�ܣ�ԭ��: " + strError;
                        throw new Exception(strError);
                        // return strError;
                    }

                    return strTempFilePath;
                }
                catch/*(Exception ex)*/
                {
                    // return "GetObjectFilePath()�쳣: " + ex.Message;
                    throw;
                }
                finally
                {
                    this.m_inSearch--;
                }
            }
            finally
            {
                if (stop != null)
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                }
            }

#if USE_LOCK
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
#endif
        }

        public void AsyncGetPatronSummary(string strPatronBarcode,
            string strCallBackFuncName,
            object element)
        {
            AsyncCall call = new AsyncCall();
            call.FuncType = "AsyncGetPatronSummary";
            call.InputParameters = new object[] { strPatronBarcode, strCallBackFuncName, element };
            this.AddCall(call);
        }

        // 
        /// <summary>
        /// ��ö���ժҪ
        /// </summary>
        /// <param name="strPatronBarcode">����֤����ţ����������ַ���</param>
        /// <returns>����ժҪ</returns>
        public string GetPatronSummary(string strPatronBarcode)
        {
            if (this.IsInLoop == false)
                throw new Exception("�Ѿ�����ѭ����");

            if (this.DisplayMessage == true && stop == null)
            {
                return "channels closed 1...";
            }

#if !SINGLE_CHANNEL

            if (this.Channels == null)
            {
                return "channels closed 2...";
            }
            ReleaseAllChannelsBut(strIdString);
#endif

            string strError = "";
            string strSummary = "";

            int nRet = strPatronBarcode.IndexOf("|");
            if (nRet != -1)
                return "֤������ַ��� '"+strPatronBarcode+"' �в�Ӧ���������ַ�";


            // ����cache���Ƿ��Ѿ�����
            StringCacheItem item = null;
            item = this.MainForm.SummaryCache.SearchItem(
                "P:" + strPatronBarcode);   // ǰ׺��Ϊ�˺Ͳ����������
            if (item != null)
            {
                // Application.DoEvents();
                strSummary = item.Content;
                return strSummary;
            }

            /*
            int nRet = strItemBarcodeUnionPath.IndexOf("|");
            if (nRet == -1)
            {
                strItemBarcode = strItemBarcodeUnionPath.Trim();
            }
            else
            {
                strItemBarcode = strItemBarcodeUnionPath.Substring(0, nRet).Trim();
                strConfirmReaderRecPath = strItemBarcodeUnionPath.Substring(nRet + 1).Trim();

                nRet = strConfirmReaderRecPath.IndexOf("||");
                if (nRet != -1)
                    strConfirmReaderRecPath = strConfirmReaderRecPath.Substring(0, nRet);
            }*/

#if USE_LOCK
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
#endif
            if (stop != null)
            {
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.SetMessage("���ڻ�ȡ����ժҪ '" + strPatronBarcode + "' ...");
                stop.BeginLoop();
            }

                try
                {
                    // Application.DoEvents();

#if SINGLE_CHANNEL
                    // ��Ϊ������ֻ��һ��Channelͨ��������Ҫ����ʹ��
                    if (this.m_inSearch > 0)
                    {
                        return "Channel��ռ��";
                    }
                    //// LibraryChannel channel = this.Channel;
#else
                LibraryChannel channel = GetChannelByID(strIdString);
#endif

                    this.m_inSearch++;
                    try
                    {
                        string strXml = "";
                        string[] results = null;
                        long lRet = Channel.GetReaderInfo(stop,
                            strPatronBarcode,
                            "xml",
                            out results,
                            out strError);
                        if (lRet == -1)
                        {
                            strSummary = strError;
                            return strSummary;
                        }
                        else if (lRet > 1)
                        {
                            strSummary = "����֤����� " + strPatronBarcode + " ���ظ���¼ " + lRet.ToString() + "��";
                            return strSummary;
                        }

                        // 2012/10/1
                        if (lRet == 0)
                            return "";  // not found

                        Debug.Assert(results.Length > 0, "");
                        strXml = results[0];

                        XmlDocument dom = new XmlDocument();
                        try
                        {
                            dom.LoadXml(strXml);
                        }
                        catch (Exception ex)
                        {
                            strSummary = "���߼�¼XMLװ��DOMʱ����: " + ex.Message;
                            return strSummary;
                        }

                        // ��������
                        strSummary = DomUtil.GetElementText(dom.DocumentElement,
                            "name");
                    }
                    catch (Exception ex)
                    {
                        return "GetPatronSummary()�쳣: " + ex.Message;
                    }
                    finally
                    {
                        this.m_inSearch--;
                    }

                    // ���cache��û�У������cache
                    item = this.MainForm.SummaryCache.EnsureItem(
                        "P:" + strPatronBarcode);
                    item.Content = strSummary;
                }
                finally
                {
                    if (stop != null)
                    {
                        stop.EndLoop();
                        stop.OnStop -= new StopEventHandler(this.DoStop);
                        stop.Initial("");
                    }
                }
                return strSummary;
#if USE_LOCK
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
#endif
        }

        public void AsyncGetSummary(String strItemBarcodeUnionPath,
            bool bCutting,
            string strCallBackFuncName,
            object element)
        {
            AsyncCall call = new AsyncCall();
            call.FuncType = "AsyncGetSummary";
            call.InputParameters = new object[] { strItemBarcodeUnionPath, bCutting, strCallBackFuncName, element };
            this.AddCall(call);
        }

        // ��ǰ�İ汾�������ĿժҪ
        // TODO: �Ƿ���Ա������ж�?
        /// <summary>
        /// �����ĿժҪ
        /// </summary>
        /// <param name="strItemBarcodeUnionPath">������ţ����������ַ�����xxxxx  B:xxxxx  BC:xxxxx ��������</param>
        /// <param name="bCutting">�Ƿ�ضϹ����Ľ���ַ���</param>
        /// <returns>��ĿժҪ</returns>
        public string GetSummary(String strItemBarcodeUnionPath,
            bool bCutting = true)
        {
            if (string.IsNullOrEmpty(strItemBarcodeUnionPath) == true)
                return "strItemBarcodeUnionPathΪ��";

            if (this.IsInLoop == false)
                throw new Exception("�Ѿ�����ѭ����");
            // Debug.WriteLine("id=" + strIdString);

            if (this.DisplayMessage == true && stop == null)
            {
                return "channels closed 1...";
            }


#if !SINGLE_CHANNEL

            if (this.Channels == null)
            {
                return "channels closed 2...";
            }
            ReleaseAllChannelsBut(strIdString);
#endif


            // MessageBox.Show(message, "client code");
            string strConfirmItemRecPath = "";
            string strError = "";
            string strSummary = "";
            string strBiblioRecPath = "";
            string strItemBarcode = "";

            // test Thread.Sleep(1000);

            // ����cache���Ƿ��Ѿ�����
            StringCacheItem item = null;

            item = this.MainForm.SummaryCache.SearchItem(strItemBarcodeUnionPath);
            if (item != null)
            {
                // Application.DoEvents();
                strSummary = item.Content;
                goto END1;  // ��Ҫ�ض�
            }

            int nRet = strItemBarcodeUnionPath.IndexOf("|");
            if (nRet == -1)
            {
                strItemBarcode = strItemBarcodeUnionPath.Trim();
            }
            else
            {
                strItemBarcode = strItemBarcodeUnionPath.Substring(0, nRet).Trim();
                strConfirmItemRecPath = strItemBarcodeUnionPath.Substring(nRet + 1).Trim();

                nRet = strConfirmItemRecPath.IndexOf("||");
                if (nRet != -1)
                    strConfirmItemRecPath = strConfirmItemRecPath.Substring(0, nRet);
            }

            // ���� B:xxxxxx ��̬ 2014/12/27 
            bool bContainCover = false;
            {
                string strPrefix = "";
                string strContent = "";
                StringUtil.ParseTwoPart(strItemBarcode, ":", out strPrefix, out strContent);
                if (string.IsNullOrEmpty(strContent) == true)
                    strContent = strPrefix;
                else
                {
                    if (strPrefix.ToUpper() == "BC")
                        bContainCover = true;
                }
                strItemBarcode = strContent;
            }

#if USE_LOCK
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
#endif
            if (stop != null)
            {
                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.SetMessage("���ڻ�ȡ��ĿժҪ '" + strItemBarcode + "' ...");
                stop.BeginLoop();
            }

            try
            {
                // Application.DoEvents();

#if SINGLE_CHANNEL
                // ��Ϊ������ֻ��һ��Channelͨ��������Ҫ����ʹ��
                if (this.m_inSearch > 0)
                {
                    return "Channel��ռ��";
                }
                //// LibraryChannel channel = this.Channel;
#else
                LibraryChannel channel = GetChannelByID(strIdString);
#endif

                this.m_inSearch++;
                try
                {

                    long lRet = this.Channel.GetBiblioSummary(
                        stop,
                        strItemBarcode,
                        strConfirmItemRecPath,
                        bContainCover == false ? null : "coverimage",
                        out strBiblioRecPath,
                        out strSummary,
                        out strError);
                    if (lRet == -1)
                    {
                        return strError;    // 2009/10/20 changed
                        // return -1;
                    }

                }
                catch (Exception ex)
                {
                    return "GetBiblioSummary()�쳣: " + ex.Message;
                }
                finally
                {
                    this.m_inSearch--;
                }

                // ���cache��û�У������cache
                item = this.MainForm.SummaryCache.EnsureItem(strItemBarcodeUnionPath);
                item.Content = strSummary;
            }
            finally
            {
                if (stop != null)
                {
                    stop.EndLoop();
                    stop.OnStop -= new StopEventHandler(this.DoStop);
                    stop.Initial("");
                }
            }
#if USE_LOCK
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
#endif

        END1:

            if (bCutting == true)
            {
                if (string.IsNullOrEmpty(strSummary) == false && strSummary[0] == '<')
                {
                    string strXml = "<root>" + strSummary + "</root>";
                    XmlDocument temp_dom = new XmlDocument();
                    try
                    {
                        temp_dom.LoadXml(strXml);
                    }
                    catch
                    {
                        goto END2;
                    }

                    XmlNode text = null;
                    foreach (XmlNode node in temp_dom.DocumentElement.ChildNodes)
                    {
                        if (node.NodeType == XmlNodeType.Text)
                            text = node;
                    }
                    if (text != null)
                    {
                        string strInnerText = text.Value;
                        // �ض�
                        if (strInnerText.Length > 25)
                            strInnerText = strInnerText.Substring(0, 25) + "...";

                        if (strInnerText.Length > 12)
                        {
                            text.Value = strInnerText.Substring(0, 12);
                            XmlNode br = text.ParentNode.InsertAfter(temp_dom.CreateElement("br"), text);

                            XmlNode new_text = temp_dom.CreateTextNode(strInnerText.Substring(12));
                            text.ParentNode.InsertAfter(new_text, br);
                        }
                        else
                            text.Value = strInnerText;
                    }

                    strSummary = temp_dom.DocumentElement.InnerXml;
                }
                else
                {
                    // �ض�
                    if (strSummary.Length > 25)
                        strSummary = strSummary.Substring(0, 25) + "...";

                    if (strSummary.Length > 12)
                        strSummary = strSummary.Insert(12, "<br/>");
                }
            }
            END2:
            return strSummary;
        }

        #region Thread support

        bool _doEvents = true;
        internal ReaderWriterLock m_lock = new ReaderWriterLock();
        internal static int m_nLockTimeout = 5000;	// 5000=5��

        List<AsyncCall> m_calls = new List<AsyncCall>();

        void AddCall(AsyncCall call)
        {
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                this.m_calls.Add(call);
                //Debug.WriteLine("AddCall " + call.FuncType);
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

            if (this._thread == null)
                this.BeginThread();
            else
                Activate();
        }



        public void Clear()
        {
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                this.m_calls.Clear();
                //Debug.WriteLine("Clear All Calls 1");

                this.DeleteAllTempFiles();  // 2015/1/4
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

            _doingCalls.Clear();
        }

        List<AsyncCall> _doingCalls = new List<AsyncCall>();

        // �����߳�ÿһ��ѭ����ʵ���Թ���
        public override void Worker()
        {
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                for (int i = 0; i < this.m_calls.Count; i++)
                {
                    if (this.Stopped == true)
                        return;
                    if (this.IsInLoop == false)
                        return;

                    AsyncCall call = this.m_calls[i];

                    _doingCalls.Add(call);
                }

                this.m_calls.Clear();
                //Debug.WriteLine("Clear All Calls 2");
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

            try
            {
                for (int i = 0;i < _doingCalls.Count;i++)
                {
                    AsyncCall call = _doingCalls[i];

                    if (this.Stopped == true)
                        return;
                    if (this.IsInLoop == false)
                        return;

                    this._doEvents = false; // ֻҪ���ù�һ���첽���ܣ��Ӵ˾Ͳ����ÿ���Ȩ

                    //Debug.WriteLine("Call " + call.FuncType);
                    if (call.FuncType == "AsyncGetObjectFilePath")
                    {
                        string strResult = "";
                        try
                        {
                            strResult = GetObjectFilePath((string)call.InputParameters[0], (string)call.InputParameters[1]);
                        }
                        catch (Exception ex)
                        {
                            strResult = ex.Message;
                            // 2014/9/9
                            DoOutputDebugInfo("�쳣��" + ex.Message);
                            goto CONTINUE;
                        }

                        // this.WebBrowser.Document.InvokeScript((string)call.InputParameters[2], new object[] { (object)call.InputParameters[3], (object)strResult });
                        this.MainForm.BeginInvokeScript(this.WebBrowser,
                            (string)call.InputParameters[2],
                            new object[] { (object)call.InputParameters[3], (object)strResult });
                    }
                    if (call.FuncType == "AsyncGetSummary")
                    {
                        string strResult = GetSummary((string)call.InputParameters[0], (bool)call.InputParameters[1]);
                        // this.WebBrowser.Document.InvokeScript((string)call.InputParameters[2], new object[] { (object)call.InputParameters[3], (object)strResult });
                        this.MainForm.BeginInvokeScript(this.WebBrowser,
                            (string)call.InputParameters[2],
                            new object[] { (object)call.InputParameters[3], (object)strResult });
                    }
                    if (call.FuncType == "AsyncGetPatronSummary")
                    {
                        string strResult = GetPatronSummary((string)call.InputParameters[0]);
                        // this.WebBrowser.Document.InvokeScript((string)call.InputParameters[2], new object[] { (object)call.InputParameters[3], (object)strResult });
                        this.MainForm.BeginInvokeScript(this.WebBrowser,
                            (string)call.InputParameters[1],
                            new object[] { (object)call.InputParameters[2], (object)strResult });
                    }

                    CONTINUE:
                    _doingCalls.RemoveAt(0);
                    i--;
                }
                //     _doingCalls.Clear();
            }
            catch(Exception ex)
            {
                /*
                if (this.m_bStopThread == false)
                    throw;
                 * */
                // 2014/9/9
                // Ҫ��һ������̨�����Щ�쳣��Ϣ���������
                DoOutputDebugInfo("�쳣��" + ex.Message);
            }
        }

        void DoOutputDebugInfo(string strText)
        {
            if (this.OutputDebugInfo != null)
            {
                OutputDebugInfoEventArgs e = new OutputDebugInfoEventArgs();
                e.Text = strText;
                this.OutputDebugInfo(this, e);
            }
        }

        #endregion

        /// <summary>
        /// ���� HTML ҳ����
        /// �Զ�ֹͣ��ǰ���첽����
        /// </summary>
        /// <param name="strHtml">HTML �ַ���</param>
        /// <param name="strTempFileType">��ʱ�ļ�ǰ׺</param>
        public void SetHtmlString(string strHtml,
            string strTempFileType)
        {
            this.StopPrevious();
            this.WebBrowser.Stop();

            this.DeleteAllTempFiles();  // 2015/1/4

            Global.SetHtmlString(this.WebBrowser,
                strHtml,
                this.MainForm.DataDir,
                strTempFileType);
        }

        /// <summary>
        /// ���� �ı� ҳ����
        /// �Զ�ֹͣ��ǰ���첽����
        /// </summary>
        /// <param name="strText">HTML �ַ���</param>
        /// <param name="strTempFileType">��ʱ�ļ�ǰ׺</param>
        public void SetTextString(string strText,
            string strTempFileType = "")
        {
            this.StopPrevious();
            this.WebBrowser.Stop();

            if (string.IsNullOrEmpty(strTempFileType) == true)
                strTempFileType = "temp_text";

            // TODO: ���־�����ʾ
            string strHtml = @"<html>
<head>
<style type='text/css'>
body {
background-color: #999999;
}
div {
background-color: #ffff99;
margin: 32px;
padding: 32px;
border-style: solid;
border-width: 1px;
border-color: #aaaaaa;

text-align: center;
}
</style>

</head>
<body style='font-family: Microsoft YaHei, Tahoma, Arial, Helvetica, sans-serif; font-size=36px;'>
<div>%text%</div>
</body</html>";
            strHtml = strHtml.Replace("%text%", HttpUtility.HtmlEncode(strText));

            Global.SetHtmlString(this.WebBrowser,
                strHtml,
                this.MainForm.DataDir,
                strTempFileType);
        }

        /// <summary>
        /// ��� HTML ҳ
        /// �Զ�ֹͣ��ǰ���첽����
        /// </summary>
        public void ClearHtmlPage()
        {
            Global.ClearHtmlPage(this.WebBrowser,
                this.MainForm.DataDir);
        }
    }

    class AsyncCall
    {
        public string FuncType = "";    // ��������
        // object ContextObject = null;    // �����Ķ���

        public object[] InputParameters = null;
    }

    // 
    /// <summary>
    /// �����Դ(���ص�������ʱ�ļ�)�ı���·���¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void GetLocalFilePathEventHandler(object sender,
GetLocalFilePathEventArgs e);

    /// <summary>
    /// �����Դ(���ص�������ʱ�ļ�)�ı���·���¼��Ĳ���
    /// </summary>
    public class GetLocalFilePathEventArgs : EventArgs
    {
        /// <summary>
        /// ����
        /// </summary>
        public string Name = "";
        /// <summary>
        /// ���ر����ļ�·��
        /// </summary>
        public string LocalFilePath = "";   // ��Դ����·�����������null����ʾû�������Դ�����߶����Ѿ�����
    }

    /// <summary>
    /// ���������Ϣ�¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void OutputDebugInfoEventHandler(object sender,
OutputDebugInfoEventArgs e);

    /// <summary>
    /// ���������Ϣ�¼��Ĳ���
    /// </summary>
    public class OutputDebugInfoEventArgs : EventArgs
    {
        /// <summary>
        /// ������Ϣ�ı�����
        /// ����ÿ����ʾΪ�µ�һ��
        /// </summary>
        public string Text = "";
    }
}
