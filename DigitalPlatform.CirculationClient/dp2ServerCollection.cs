using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using DigitalPlatform;
using DigitalPlatform.Text;


namespace DigitalPlatform.CirculationClient
{
    [Serializable()]
    public class dp2Server
    {
        public string Name = "";    // ��������

        public string Url = "";	// ������URL,Ӧ��webservice endpoint

        public string DefaultUserName = "";

        // [NonSerialized]
        string StorageDefaultPassword = Cryptography.Encrypt("", "dp2rms");

        public bool SavePassword = false;

        [NonSerialized]
        public bool Verified = false;   // ����֤�����к�?

        public dp2Server()
        {
        }

        // �������캯��
        public dp2Server(dp2Server refServer)
        {
            this.Name = refServer.Name;
            this.Url = refServer.Url;
            this.DefaultUserName = refServer.DefaultUserName;
            this.StorageDefaultPassword = refServer.StorageDefaultPassword;
            this.SavePassword = refServer.SavePassword;
            this.Verified = refServer.Verified;
        }

        public string DefaultPassword
        {
            get
            {
                if (SavePassword == false)
                    return "";
                return Cryptography.Decrypt(StorageDefaultPassword, "dp2rms");
            }
            set
            {
                StorageDefaultPassword = Cryptography.Encrypt(value, "dp2rms");
            }
        }
    }


    /// <summary>
    /// �����������Ϣ��������
    /// ����ȡ��HostList
    /// </summary>
    [Serializable()]
    public class dp2ServerCollection : ArrayList
    {
        [NonSerialized]
        string m_strFileName = "";

        [NonSerialized]
        bool m_bChanged = false;

        [NonSerialized]
        public IWin32Window ownerForm = null;

        public event dp2ServerChangedEventHandle ServerChanged = null;


        public dp2ServerCollection()
        {
        }

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                return m_bChanged;
            }
            set
            {
                m_bChanged = value;
            }
        }

        public string FileName
        {
            get
            {
                return m_strFileName;
            }
            set
            {
                this.m_strFileName = value;
            }
        }

        // ������ �ַ���������
        public dp2Server this[string strUrl]
        {
            get
            {
                return this.GetServer(strUrl);
            }

        }

        /// <summary>
        /// ���� URL ��� dp2Server ����
        /// </summary>
        /// <param name="strUrl">������ URL</param>
        /// <returns>dp2Server ����</returns>
        public dp2Server GetServer(string strUrl)
        {
            strUrl = StringUtil.CanonicalizeHostUrl(strUrl);

            dp2Server server = null;
            for (int i = 0; i < this.Count; i++)
            {
                server = (dp2Server)this[i];
                string strCurrentUrl = StringUtil.CanonicalizeHostUrl(server.Url);
                if (String.Compare(strCurrentUrl, strUrl, true) == 0)
                    return server;
            }

            return null;
        }

        // 2007/9/14
        public dp2Server GetServerByName(string strServerName)
        {
            dp2Server server = null;
            for (int i = 0; i < this.Count; i++)
            {
                server = (dp2Server)this[i];
                if (server.Name == strServerName)
                    return server;
            }

            return null;
        }

        // ��¡��
        // �������еĶ�����ȫ���´����ġ�
        public dp2ServerCollection Dup()
        {
            dp2ServerCollection newServers = new dp2ServerCollection();

            for (int i = 0; i < this.Count; i++)
            {
                dp2Server newServer = new dp2Server((dp2Server)this[i]);
                newServers.Add(newServer);
            }

            newServers.m_strFileName = this.m_strFileName;
            newServers.m_bChanged = this.m_bChanged;
            newServers.ownerForm = this.ownerForm;

            return newServers;
        }

        // ����һ������������ݹ��뱾����
        public void Import(dp2ServerCollection servers)
        {
            this.Clear();
            this.AddRange(servers);
            this.m_bChanged = true;

            // �����ӵĶ���
            dp2ServerChangedEventArgs e = new dp2ServerChangedEventArgs();
            e.Url = "";
            e.ServerChangeAction = dp2ServerChangeAction.Import;
            OnServerChanged(this, e);

        }

        // ����һ���µ�Server����
        // return:
        //		-1	����
        //		0	������
        //		1	�����ظ���û�м���
        public int NewServer(
            string strName,
            string strUrl,
            int nInsertPos)
        {
            dp2Server server = null;
            // ��ʱ��ȥ��

            server = new dp2Server();
            server.Url = strUrl;
            server.Name = strName;

            if (nInsertPos == -1)
                this.Add(server);
            else
                this.Insert(nInsertPos, server);

            m_bChanged = true;

            dp2ServerChangedEventArgs e = new dp2ServerChangedEventArgs();
            e.Url = strUrl;
            e.ServerChangeAction = dp2ServerChangeAction.Add;
            OnServerChanged(this, e);

            return 0;
        }


        public void OnServerChanged(object sender, dp2ServerChangedEventArgs e)
        {
            if (this.ServerChanged != null)
            {
                this.ServerChanged(sender, e);
            }

        }

        // ����һ���µ�Server����
        // return:
        public dp2Server NewServer(int nInsertPos)
        {
            dp2Server server = null;
            server = new dp2Server();

            if (nInsertPos == -1)
                this.Add(server);
            else
                this.Insert(nInsertPos, server);

            m_bChanged = true;

            return server;
        }

        // ���ļ���װ�ش���һ��ServerCollection����
        // parameters:
        //		bIgnorFileNotFound	�Ƿ��׳�FileNotFoundException�쳣��
        //							���==true������ֱ�ӷ���һ���µĿ�ServerCollection����
        // Exception:
        //			FileNotFoundException	�ļ�û�ҵ�
        //			SerializationException	�汾Ǩ��ʱ���׳���
        public static dp2ServerCollection Load(
            string strFileName,
            bool bIgnorFileNotFound)
        {
            Stream stream = null;
            dp2ServerCollection servers = null;

            try
            {
                stream = File.Open(strFileName, FileMode.Open);
            }
            catch (FileNotFoundException ex)
            {
                if (bIgnorFileNotFound == false)
                    throw ex;

                servers = new dp2ServerCollection();
                servers.m_strFileName = strFileName;

                // �õ�����һ���µĿն������
                return servers;
            }


            BinaryFormatter formatter = new BinaryFormatter();

            servers = (dp2ServerCollection)formatter.Deserialize(stream);
            stream.Close();
            servers.m_strFileName = strFileName;


            return servers;
        }

        // ���浽�ļ�
        // parameters:
        //		strFileName	�ļ��������==null,��ʾʹ��װ��ʱ������Ǹ��ļ���
        public void Save(string strFileName)
        {
            if (m_bChanged == false)
                return;

            if (strFileName == null)
                strFileName = m_strFileName;

            if (strFileName == null)
            {
                throw (new Exception("ServerCollection.Save()û��ָ�������ļ���"));
            }

            Stream stream = File.Open(strFileName,
                FileMode.Create);

            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, this);
            stream.Close();
        }

        public void SetAllVerified(bool bVerified)
        {
            for (int i = 0; i < this.Count; i++)
            {
                dp2Server server = (dp2Server)this[i];
                server.Verified = bVerified;
            }
        }

        /*
        // ���ȱʡ�ʻ���Ϣ
        // return:
        //		2	already login succeed
        //		1	dialog return OK
        //		0	dialog return Cancel
        //		-1	other error
        public void OnAskAccountInfo(object sender,
            AskAccountInfoEventArgs e)
        {
            bool bFirst = true;

            bool bAutoLogin = (e.LoginStyle & LoginStyle.AutoLogin) == LoginStyle.AutoLogin;
            bool bFillDefault = (e.LoginStyle & LoginStyle.FillDefaultInfo) == LoginStyle.FillDefaultInfo;

            e.Owner = this.ownerForm;
            e.UserName = "";
            e.Password = "";

            LoginDlg dlg = new LoginDlg();

            Server server = this[e.Url];

            dlg.textBox_serverAddr.Text = e.Url;
            if (bFillDefault == true)
            {
                if (server != null)
                {
                    dlg.textBox_userName.Text = (server.DefaultUserName == "" ? "public" : server.DefaultUserName);
                    dlg.textBox_password.Text = server.DefaultPassword;
                    dlg.checkBox_savePassword.Checked = server.SavePassword;
                }
                else
                {
                    dlg.textBox_userName.Text = "public";
                    dlg.textBox_password.Text = "";
                    dlg.checkBox_savePassword.Checked = false;
                }
            }

            if (e.Comment != null)
                dlg.textBox_comment.Text = e.Comment;

        DOLOGIN:
            if (e.Channels != null)
            {
                if (bAutoLogin == false && bFirst == true)
                    goto REDOINPUT;

                // �ҵ�Channel
                RmsChannel channel = e.Channels.GetChannel(dlg.textBox_serverAddr.Text);

                Debug.Assert(channel != null, "Channels.GetChannel()�쳣...");


                string strError;
                // ��¼
                int nRet = channel.Login(dlg.textBox_userName.Text,
                    dlg.textBox_password.Text,
                    out strError);

                if (nRet != 1)
                {
                    strError = "���û��� '" + dlg.textBox_userName.Text + "' ��¼�� '" + dlg.textBox_serverAddr.Text + "' ʧ��: " + strError;

                    if (this.ownerForm != null)
                    {
                        MessageBox.Show(this.ownerForm, strError);
                    }
                    else
                    {
                        e.ErrorInfo = strError;
                        e.Result = -1;
                    }

                    goto REDOINPUT;
                }
                else // ��¼�ɹ�
                {
                    if (String.Compare(e.Url, dlg.textBox_serverAddr.Text, true) != 0)
                    {
                        // ����һ���µ�Server����
                        // return:
                        //		-1	����
                        //		0	������
                        //		1	�����ظ���û�м���
                        nRet = this.NewServer(dlg.textBox_serverAddr.Text, -1);
                        if (nRet == 0)
                            e.Url = channel.Url;
                    }

                    server = this[dlg.textBox_serverAddr.Text];

                    if (server == null) // 2006/8/19 add
                    {
                        // ����һ���µ�Server����
                        // return:
                        //		-1	����
                        //		0	������
                        //		1	�����ظ���û�м���
                        nRet = this.NewServer(dlg.textBox_serverAddr.Text, -1);
                        if (nRet == 0)
                            e.Url = channel.Url;

                        server = this[dlg.textBox_serverAddr.Text];

                    }

                    Debug.Assert(server != null, "��ʱserver������Ϊnull");

                    server.DefaultUserName = dlg.textBox_userName.Text;
                    server.DefaultPassword = dlg.textBox_password.Text;
                    server.SavePassword = dlg.checkBox_savePassword.Checked;
                    this.m_bChanged = true;

                    e.Result = 2;
                    return;
                }
            }


        REDOINPUT:
            bFirst = false;

            dlg.ShowDialog(ownerForm);

            if (dlg.DialogResult != DialogResult.OK)
            {
                e.Result = 0;
                return;
            }

            if (e.Channels == null)
            {
                e.UserName = dlg.textBox_userName.Text;
                e.Password = dlg.textBox_password.Text;

                e.Result = 1;
                return;
            }


            goto DOLOGIN;
        }
        */
    }

    // �¼�: �������ɾ���˷�����
    public delegate void dp2ServerChangedEventHandle(object sender,
    dp2ServerChangedEventArgs e);

    public class dp2ServerChangedEventArgs : EventArgs
    {
        public string Url = ""; // ������URL
        public dp2ServerChangeAction ServerChangeAction = dp2ServerChangeAction.None; // �������ĸı�����
    }

    public enum dp2ServerChangeAction
    {
        None = 0,
        Add = 1,
        Remove = 2,
        Import = 3,
    }
}
