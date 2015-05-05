using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.IO;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Install;

using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.IO;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.CirculationClient.localhost;
using System.Net;

namespace DigitalPlatform.OPAC
{
    public partial class InstallOpacParamDlg : Form
    {
        public string LibraryReportDir = "";

        public bool NeedAppendRights = false;

        // �����û������ڴ��������ʻ�
        public string SupervisorUserName = "supervisor";
        public string SupervisorPassword = "";

        public string ManageAccountRights = "";

        // bool SavePassword = true;

        // public string RootDir = "";

        public InstallOpacParamDlg()
        {
            InitializeComponent();
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            EnableControls(false);
            this.Update();

            try
            {
                string strError = "";

                if (this.Dp2LibraryUrl == "")
                {
                    MessageBox.Show(this, "��δ���� dp2Library �������� URL");
                    return;
                }

                if (this.ManageUserName == "")
                {
                    MessageBox.Show(this, "��δָ�������ʻ��û�����");
                    return;
                }

                if (this.ManageUserName == "reader"
    || this.ManageUserName == "public"
    || this.ManageUserName == "ͼ���")
                {
                    strError = "�����ʻ����û�������Ϊ 'reader' 'public' 'ͼ���' ֮һ����Ϊ��Щ���� dp2Library ϵͳ�ھ����ض���;�ı����ʻ���";
                    MessageBox.Show(this, strError);
                    return;
                }

                if (this.textBox_managePassword.Text != this.textBox_confirmManagePassword.Text)
                {
                    strError = "�����ʻ� ���� �� �ٴ��������� ��һ�¡����������롣";
                    MessageBox.Show(this, strError);
                    return;
                }

                // ��֤�����ʻ��û��Ƿ��Ѿ����ã�
                // return:
                //       -1  ����
                //      0   ������
                //      1   ����, ������һ��
                //      2   ����, �����벻һ��
                int nRet = DetectManageUser(out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, "��֤�����ʻ�ʱ��������: " + strError + "\r\n\r\n��ȷ�������ʻ��Ѿ���ȷ����");
                    return;
                }

                if (nRet == 1)
                {
                    // ����Ȩ��
                    this.NeedAppendRights = true;
                }
                else if (nRet == 2)
                {
                    string strText = "�����ʻ��Ѿ�����, ����������͵�ǰ����������õ����벻һ�¡�\r\n\r\n�Ƿ�Ҫ����������?\r\n\r\n(��(Yes): �������벢������װ����(No): ���������벢������װ��ȡ��(Cancel): �����������)";
                    DialogResult result = MessageBox.Show(this,
                        strText,
                        "setup_dp2opac",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                    if (result == DialogResult.Yes)
                    {
                        nRet = ResetManageUserPassword(out strError);
                        if (nRet == -1)
                        {
                            MessageBox.Show(this, "��������ʻ�����ʱ����: " + strError + "\r\n\r\n��ȷ�������ʻ��Ѿ���ȷ����");
                            return;
                        }
                    }

                    if (result == DialogResult.Cancel)
                        return; // �������

                    // ����Ȩ��
                    this.NeedAppendRights = true;
                }

                // �����ʻ�������
                else if (nRet == 0)
                {
                    // �Զ�����?
                    string strText = "�����ʻ� '" + this.textBox_manageUserName.Text + "' ��δ����, �Ƿ񴴽�֮?\r\n\r\n(ȷ��(OK): ������ ȡ��(Cancel): �������������������)";
                    DialogResult result = MessageBox.Show(this,
                        strText,
                        "setup_dp2opac",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                    if (result == DialogResult.Cancel)
                        return;

                    nRet = CreateManageUser(out strError);
                    if (nRet == -1)
                    {
                        MessageBox.Show(this, strError);
                        return;
                    }
                    this.NeedAppendRights = false;
                }

                // ��ñ���Ŀ¼·��
                if (IsLocalHostUrl(this.textBox_dp2LibraryUrl.Text) == true)
                {
                    string strDataDir = "";
                    nRet = GetLibraryDataDir(out strDataDir, out strError);
                    if (nRet == -1)
                    {
                        MessageBox.Show(this, "��� dp2Library ����Ŀ¼����ʱ��������: " + strError + "\r\n\r\n���ڰ�װ�ɹ����ֶ����� opac.xml �ļ�");
                    }

                    if (string.IsNullOrEmpty(strDataDir) == false)
                        this.LibraryReportDir = Path.Combine(strDataDir, "upload\\reports");
                    else
                        this.LibraryReportDir = "";
                }
                else
                    this.LibraryReportDir = "";
            }
            finally
            {
                EnableControls(true);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public static bool IsLocalHostUrl(string strUrl)
        {
            Uri uri = new Uri(strUrl);
            if (uri.Scheme.ToLower() == "net.pipe")
                return true;
            return IsLocalIpAddress(uri.Host);
        }

        // ���һ����ַ�Ƿ��ͬ localhost
        // http://stackoverflow.com/questions/11834091/how-to-check-if-localhost
        public static bool IsLocalIpAddress(string host)
        {
            try
            { // get host IP addresses
                IPAddress[] hostIPs = Dns.GetHostAddresses(host);
                // get local IP addresses
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                // test if any host IP equals to any local IP or to localhost
                foreach (IPAddress hostIP in hostIPs)
                {
                    // is localhost
                    if (IPAddress.IsLoopback(hostIP))
                        return true;
                    // is local address
                    foreach (IPAddress localIP in localIPs)
                    {
                        if (hostIP.Equals(localIP))
                            return true;
                    }
                }
            }
            catch 
            {
            }
            return false;
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public string Dp2LibraryUrl
        {
            get
            {
                return this.textBox_dp2LibraryUrl.Text;
            }
            set
            {
                this.textBox_dp2LibraryUrl.Text = value;
            }
        }

        // �����û���
        public string ManageUserName
        {
            get
            {
                return this.textBox_manageUserName.Text;
            }
            set
            {
                this.textBox_manageUserName.Text = value;
            }
        }

        // �����û�����
        public string ManagePassword
        {
            get
            {
                return this.textBox_managePassword.Text;
            }
            set
            {
                this.textBox_managePassword.Text = value;
                this.textBox_confirmManagePassword.Text = value;
            }
        }

        // �������û��Ƿ��Ѿ�����?
        // return:
        //       -1  ����
        //      0   ������
        //      1   ����, ������һ��
        //      2   ����, �����벻һ��
        int DetectManageUser(out string strError)
        {
            strError = "";
            if (this.textBox_dp2LibraryUrl.Text == "")
            {
                strError = "��δָ�� dp2Library ������ URL";
                return -1;
            }

            if (this.textBox_manageUserName.Text == "")
            {
                strError = "��δָ�������ʻ����û���";
                return -1;
            }

            if (this.textBox_managePassword.Text != this.textBox_confirmManagePassword.Text)
            {
                strError = "�����ʻ� ���� �� �ٴ��������� ��һ�¡����������롣";
                return -1;
            }

            LibraryChannel channel = new LibraryChannel();
            channel.Url = this.textBox_dp2LibraryUrl.Text;

            // Debug.Assert(false, "");
            string strParameters = "location=#setup,type=worker";
            long nRet = channel.Login(this.textBox_manageUserName.Text,
                this.textBox_managePassword.Text,
                strParameters,
                out strError);
            if (nRet == -1)
            {
                strError = "���û��� '" + this.textBox_manageUserName.Text + "' �������¼ʧ��: " + strError;
                return -1;
            }

            if (nRet == 1)
                this.ManageAccountRights = channel.Rights;

            channel.Logout(out strError);

            if (nRet == 0)
            {
                channel.BeforeLogin -= new BeforeLoginEventHandle(channel_BeforeLogin);
                channel.BeforeLogin += new BeforeLoginEventHandle(channel_BeforeLogin);

                strError = "Ϊȷ�ϴ����ʻ��Ƿ����, ���ó����û���ݵ�¼��";
                nRet = channel.DoNotLogin(ref strError);
                if (nRet == -1 || nRet == 0)
                {
                    strError = "�Գ����û���ݵ�¼ʧ��: " + strError + "\r\n\r\n����޷�ȷ�������ʻ��Ƿ����";
                    return -1;
                }

                UserInfo[] users = null;
                nRet = channel.GetUser(
                    null,
                    "list",
                    this.textBox_manageUserName.Text,
                    0,
                    -1,
                    out users,
                    out strError);
                if (nRet == -1)
                {
                    strError = "��ȡ�û� '" + this.textBox_manageUserName.Text + "' ��Ϣʱ��������: " + strError + "\r\n\r\n����޷�ȷ�������ʻ��Ƿ���ڡ�";
                    return -1;
                }
                if (nRet == 1)
                {
                    Debug.Assert(users != null, "");
                    strError = "�����ʻ� '" + this.textBox_manageUserName.Text + "' �Ѿ�����, ��������͵�ǰ��������õ����벻һ�¡�";
                    return 2;
                }
                if (nRet >= 1)
                {
                    Debug.Assert(users != null, "");
                    strError = "�� '" + this.textBox_manageUserName.Text + "' Ϊ�û��� ���û���¼���ڶ���������һ�����ش�����ϵͳ����Ա����dp2circulation���������˴���";
                    return -1;
                }

                return 0;
            }

            return 1;
        }

        void channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            if (e.FirstTry == true)
            {
                e.UserName = this.SupervisorUserName;
                e.Password = this.SupervisorPassword;

                e.Parameters = "location=#setup,type=worker";

                if (String.IsNullOrEmpty(e.UserName) == false)
                    return; // ��������, �Ա�����һ�� ������ �Ի�����Զ���¼
            }

            // 
            IWin32Window owner = null;

            if (sender is IWin32Window)
                owner = (IWin32Window)sender;
            else
                owner = this;

            CirculationLoginDlg dlg = new CirculationLoginDlg();
            GuiUtil.AutoSetDefaultFont(dlg);
            // dlg.Text = "";
            dlg.ServerUrl = this.textBox_dp2LibraryUrl.Text;
            dlg.Comment = e.ErrorInfo;
            dlg.UserName = e.UserName;
            dlg.SavePasswordShort = false;
            dlg.SavePasswordLong = false;
            dlg.Password = e.Password;
            dlg.IsReader = false;
            dlg.OperLocation = "#setup";
            dlg.StartPosition = FormStartPosition.CenterScreen;

            dlg.ShowDialog(owner);

            if (dlg.DialogResult == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            e.UserName = dlg.UserName;
            e.Password = dlg.Password;
            e.SavePasswordShort = dlg.SavePasswordShort;
            e.Parameters = "location=#setup,type=worker";

            e.SavePasswordLong = dlg.SavePasswordLong;
            e.LibraryServerUrl = dlg.ServerUrl;

            this.SupervisorUserName = e.UserName;
            this.SupervisorPassword = e.Password;
        }

#if NO
        // ����û���¼
        // return:
        //      -1  error
        //      0   not found
        //      >=1   �������е�����
        public static int GetUserRecord(
            RmsChannel channel,
            string strUserName,
            out string strRecPath,
            out string strXml,
            out byte[] baTimeStamp,
            out string strError)
        {
            strError = "";

            strXml = "";
            strRecPath = "";
            baTimeStamp = null;

            if (strUserName == "")
            {
                strError = "�û���Ϊ��";
                return -1;
            }

            string strQueryXml = "<target list='" + Defs.DefaultUserDb.Name
                + ":" + Defs.DefaultUserDb.SearchPath.UserName + "'><item><word>"
                + strUserName + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>10</maxCount></item><lang>chi</lang></target>";

            long nRet = channel.DoSearch(strQueryXml,
                "default",
                "", // strOutputStyle
                out strError);
            if (nRet == -1)
            {
                strError = "�����ʻ���ʱ����: " + strError;
                return -1;
            }

            if (nRet == 0)
                return 0;	// not found

            long nSearchCount = nRet;

            List<string> aPath = null;
            nRet = channel.DoGetSearchResult(
                "default",
                1,
                "zh",
                null,	// stop,
                out aPath,
                out strError);
            if (nRet == -1)
            {
                strError = "����ע���û����ȡ�������ʱ����: " + strError;
                return -1;
            }
            if (aPath.Count == 0)
            {
                strError = "����ע���û����ȡ�ļ������Ϊ��";
                return -1;
            }

            // strRecID = ResPath.GetRecordId((string)aPath[0]);
            strRecPath = (string)aPath[0];

            string strStyle = "content,data,timestamp,withresmetadata";
            string strMetaData;
            string strOutputPath;

            nRet = channel.GetRes((string)aPath[0],
                strStyle,
                out strXml,
                out strMetaData,
                out baTimeStamp,
                out strOutputPath,
                out strError);
            if (nRet == -1)
            {
                strError = "��ȡע���û����¼��ʱ����: " + strError;
                return -1;
            }


            return (int)nSearchCount;
        }

#endif

        // ���������ʻ�
        int CreateManageUser(out string strError)
        {
            strError = "";
            if (this.textBox_dp2LibraryUrl.Text == "")
            {
                strError = "��δָ��dp2Library������URL";
                return -1;
            }

            if (this.textBox_manageUserName.Text == "")
            {
                strError = "��δָ�������ʻ����û���";
                return -1;
            }

            if (this.textBox_manageUserName.Text == "reader"
                || this.textBox_manageUserName.Text == "public"
                || this.textBox_manageUserName.Text == "ͼ���")
            {
                strError = "�����ʻ����û�������Ϊ 'reader' 'public' 'ͼ���' ֮һ����Ϊ��Щ���� dp2Library ϵͳ�ھ����ض���;�ı����ʻ���";
                return -1;
            }

            if (this.textBox_managePassword.Text != this.textBox_confirmManagePassword.Text)
            {
                strError = "������ ���� �� �ٴ��������� ��һ�¡����������롣";
                return -1;
            }

            LibraryChannel channel = new LibraryChannel();
            channel.Url = this.textBox_dp2LibraryUrl.Text;

            channel.BeforeLogin -= new BeforeLoginEventHandle(channel_BeforeLogin);
            channel.BeforeLogin += new BeforeLoginEventHandle(channel_BeforeLogin);

            strError = "���ó����û���ݵ�¼���Ա㴴�������ʻ���";
            int nRet = channel.DoNotLogin(ref strError);
            if (nRet == -1 || nRet == 0)
            {
                strError = "�Գ����û���ݵ�¼ʧ��: " + strError;
                return -1;
            }
            UserInfo user = new UserInfo();
            user.UserName = this.textBox_manageUserName.Text;
            user.Password = this.textBox_managePassword.Text;
            user.SetPassword = true;
            user.Rights = "getsystemparameter,getres,search,getbiblioinfo,setbiblioinfo,getreaderinfo,writeobject,getbibliosummary,listdbfroms,simulatereader,simulateworker";

            /*
�����ʻ�:
getsystemparameter
getres
search
getbiblioinfo
getreaderinfo
writeobject * */

            long lRet = channel.SetUser(
    null,
    "new",
    user,
    out strError);
            if (lRet == -1)
            {
                strError = "���������ʻ�ʱ��������: " + strError;
                return -1;
            }

            channel.Logout(out strError);
            return 0;
        }

        // �����ô����ʻ�����
        int ResetManageUserPassword(out string strError)
        {
            strError = "";
            if (this.textBox_dp2LibraryUrl.Text == "")
            {
                strError = "��δָ��dp2Library������URL";
                return -1;
            }

            if (this.textBox_manageUserName.Text == "")
            {
                strError = "��δָ�������ʻ����û���";
                return -1;
            }

            if (this.textBox_managePassword.Text != this.textBox_confirmManagePassword.Text)
            {
                strError = "�����ʻ� ���� �� �ٴ��������� ��һ�¡����������롣";
                return -1;
            }

            LibraryChannel channel = new LibraryChannel();
            channel.Url = this.textBox_dp2LibraryUrl.Text;

            channel.BeforeLogin -= new BeforeLoginEventHandle(channel_BeforeLogin);
            channel.BeforeLogin += new BeforeLoginEventHandle(channel_BeforeLogin);

            strError = "���ó����û���ݵ�¼���Ա���������ʻ����롣";
            int nRet = channel.DoNotLogin(ref strError);
            if (nRet == -1 || nRet == 0)
            {
                strError = "�Գ����û���ݵ�¼ʧ��: " + strError;
                return -1;
            }

            if (StringUtil.IsInList("changeuserpassword", channel.Rights) == false)
            {
                strError = "����ʹ�õĳ����û� '" + this.SupervisorUserName + "' ���߱� changeuserpassword Ȩ�ޣ��޷�����(Ϊ�����ʻ� '" + this.textBox_manageUserName.Text + "' )��������Ĳ���";
                return -1;
            }

            UserInfo user = new UserInfo();
            user.UserName = this.textBox_manageUserName.Text;
            user.Password = this.textBox_managePassword.Text;

            long lRet = channel.SetUser(
                null,
                "resetpassword",
                user,
                out strError);
            if (lRet == -1)
            {
                strError = "��������ʱ��������: " + strError;
                return -1;
            }

            channel.Logout(out strError);
            return 0;
        }

        // ��� dp2Library ����Ŀ¼
        int GetLibraryDataDir(out string strDataDir, out string strError)
        {
            strError = "";
            strDataDir = "";

            if (this.textBox_dp2LibraryUrl.Text == "")
            {
                strError = "��δָ�� dp2Library ������ URL";
                return -1;
            }

            if (this.textBox_manageUserName.Text == "")
            {
                strError = "��δָ�������ʻ����û���";
                return -1;
            }

            if (this.textBox_managePassword.Text != this.textBox_confirmManagePassword.Text)
            {
                strError = "�����ʻ� ���� �� �ٴ��������� ��һ�¡����������롣";
                return -1;
            }

            LibraryChannel channel = new LibraryChannel();
            channel.Url = this.textBox_dp2LibraryUrl.Text;

            // Debug.Assert(false, "");
            string strParameters = "location=#setup,type=worker";
            long nRet = channel.Login(this.textBox_manageUserName.Text,
                this.textBox_managePassword.Text,
                strParameters,
                out strError);
            if (nRet == -1)
            {
                strError = "���û��� '" + this.textBox_manageUserName.Text + "' �������¼ʧ��: " + strError;
                return -1;
            }

            try
            {
                if (nRet == 0 || StringUtil.IsInList("getsystemparameter", channel.Rights) == false)
                {
                    channel.BeforeLogin -= new BeforeLoginEventHandle(channel_BeforeLogin);
                    channel.BeforeLogin += new BeforeLoginEventHandle(channel_BeforeLogin);

                    strError = "Ϊ��ȡ dp2Library ����Ŀ¼������Ϣ, ���ó����û���ݵ�¼��";
                    nRet = channel.DoNotLogin(ref strError);
                    if (nRet == -1 || nRet == 0)
                    {
                        strError = "�Գ����û���ݵ�¼ʧ��: " + strError + "\r\n\r\n����޷���ȡ dp2Library ����Ŀ¼������Ϣ";
                        return -1;
                    }
                }

                nRet = channel.GetSystemParameter(
        null,
        "cfgs",
        "getDataDir",
        out strDataDir,
        out strError);
                if (nRet == -1)
                    return -1;
            }
            finally
            {
                channel.Logout(out strError);
            }

            return 0;
        }

#if NO
        void channels_AskAccountInfo(object sender, AskAccountInfoEventArgs e)
        {
            e.Owner = this;

            LoginDlg dlg = new LoginDlg();

            dlg.textBox_serverAddr.Text = this.textBox_dp2LibraryUrl.Text;
            dlg.textBox_serverAddr.ReadOnly = true;
            dlg.textBox_comment.Text = e.Comment;
            dlg.textBox_userName.Text = this.ManagerUserName;
            dlg.textBox_password.Text = this.ManagerPassword;
            dlg.checkBox_savePassword.Checked = this.SavePassword;
            dlg.ShowDialog(this);

            if (dlg.DialogResult != DialogResult.OK)
            {
                e.Result = 0;
                return;
            }

            this.ManagerPassword = dlg.textBox_userName.Text;

            if (dlg.checkBox_savePassword.Checked == true)
                this.ManagerPassword = dlg.textBox_password.Text;
            else
                this.ManagerPassword = "";

            e.UserName = dlg.textBox_userName.Text;
            e.Password = dlg.textBox_password.Text;

            e.Result = 1;
        }
#endif

#if NO
        int BuildUserRecord(out string strXml,
    out string strError)
        {
            strXml = "";
            strError = "";

            XmlDocument UserRecDom = new XmlDocument();
            UserRecDom.LoadXml("<record><name /><password /><server /></record>");


            // �����û���
            DomUtil.SetElementText(UserRecDom.DocumentElement,
                "name",
                this.textBox_manageUserName.Text);


            // ����
            DomUtil.SetElementText(UserRecDom.DocumentElement,
               "password",
                Cryptography.GetSHA1(this.textBox_managePassword.Text));

            XmlNode nodeServer = UserRecDom.DocumentElement.SelectSingleNode("server");
            if (nodeServer == null)
            {
                Debug.Assert(false, "�����ܵ����");
                return -1;
            }

            DomUtil.SetAttr(nodeServer, "rights", "children_database:create,list");

            strXml = UserRecDom.OuterXml;

            return 0;
        }

#endif

        void EnableControls(bool bEnable)
        {
            this.textBox_confirmManagePassword.Enabled = bEnable;
            this.textBox_managePassword.Enabled = bEnable;
            this.textBox_manageUserName.Enabled = bEnable;
            this.textBox_dp2LibraryUrl.Enabled = bEnable;

            this.button_OK.Enabled = bEnable;
            this.button_Cancel.Enabled = bEnable;
            this.button_createManageUser.Enabled = bEnable;
            this.button_detectManageUser.Enabled = bEnable;
            this.button_resetManageUserPassword.Enabled = bEnable;

        }

        // �������ʻ��Ƿ���ڣ���¼�Ƿ���ȷ
        private void button_detectManageUser_Click(object sender, EventArgs e)
        {
            EnableControls(false);
            try
            {
                if (string.IsNullOrEmpty(this.textBox_manageUserName.Text) == true)
                {
                    MessageBox.Show(this, "�����������ָ��Ҫ���Ĵ����ʻ���");
                    return;
                }
                string strError = "";
                // return:
                //       -1  ����
                //      0   ������
                //      1   ����, ������һ��
                //      2   ����, �����벻һ��
                int nRet = DetectManageUser(out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                }
                else if (nRet == 0)
                {
                    MessageBox.Show(this, "�����ʻ� '" + this.textBox_manageUserName.Text + "' Ŀǰ�в����ڡ�");
                }
                else if (nRet == 2)
                {
                    MessageBox.Show(this, "�����ʻ� '" + this.textBox_manageUserName.Text + "' ��������ڣ���������͵�ǰ�������������벻һ�¡�");
                }
                else
                {
                    Debug.Assert(nRet == 1, "");
                    MessageBox.Show(this, "�����ʻ� '" + this.textBox_manageUserName.Text + "' ��������ڡ�");
                }
            }
            finally
            {
                EnableControls(true);
            }
        }

        // ����һ���µĹ����ʻ�����Ҫ�� root Ȩ�޵�¼���ܴ���
        private void button_createManageUser_Click(object sender, EventArgs e)
        {
            EnableControls(false);

            try
            {
                string strError = "";
                int nRet = CreateManageUser(out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                }
                else
                {
                    MessageBox.Show(this, "�����ʻ������ɹ���");
                }
            }
            finally
            {
                EnableControls(true);
            }
        }

        private void button_resetManageUserPassword_Click(object sender, EventArgs e)
        {
            // �����ô����ʻ�����
            EnableControls(false);
            try
            {
                string strError = "";
                int nRet = ResetManageUserPassword(out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                }
                else
                {
                    MessageBox.Show(this, "��������ʻ�����ɹ���");
                }
            }
            finally
            {
                EnableControls(true);
            }
        }

        private void textBox_manageUserName_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.textBox_manageUserName.Text) == true)
            {
                this.button_detectManageUser.Enabled = false;
                this.button_createManageUser.Enabled = false;
                this.button_resetManageUserPassword.Enabled = false;
            }
            else
            {
                this.button_detectManageUser.Enabled = true;
                this.button_createManageUser.Enabled = true;
                this.button_resetManageUserPassword.Enabled = true;
            }
        }

    }
}