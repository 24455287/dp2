using System;
using System.Collections;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Text;

namespace DigitalPlatform.rms.Client
{

	[Serializable()]
	public class Server
	{
		public string Url = "";	// ������URL,Ӧ��webservice asmxȫ��

		public string DefaultUserName = "";

		// [NonSerialized]
		string StorageDefaultPassword = Cryptography.Encrypt("", "dp2rms");

		public bool SavePassword = false;

		public Server()
		{
		}

  

		// �������캯��
		public Server(Server refServer)
		{
			this.Url = refServer.Url;
			this.DefaultUserName = refServer.DefaultUserName;
			this.StorageDefaultPassword = refServer.StorageDefaultPassword;
			this.SavePassword = refServer.SavePassword;
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
	public class ServerCollection : ArrayList
	{
		[NonSerialized]
		string m_strFileName = "";

		[NonSerialized]
		bool m_bChanged = false;

		[NonSerialized]
		public IWin32Window ownerForm = null;

        public event ServerChangedEventHandle ServerChanged = null;


		public ServerCollection()
		{
		}

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
		public Server this[string strUrl]
		{
			get 
			{
				return this.GetServer(strUrl);
			}

		}

		Server GetServer(string strUrl)
		{
			Server server = null;
			for(int i=0; i<this.Count; i++) 
			{
				server = (Server)this[i];
				if ( String.Compare(server.Url, strUrl, true) == 0)
					return server;
			}

			return null;
		}

		// ��¡��
		// �������еĶ�����ȫ���´����ġ�
		public ServerCollection Dup()
		{
			ServerCollection newServers = new ServerCollection();

			for(int i=0;i<this.Count;i++)
			{
				Server newServer = new Server((Server)this[i]);
				newServers.Add(newServer);
			}

			newServers.m_strFileName = this.m_strFileName;
			newServers.m_bChanged = this.m_bChanged;
			newServers.ownerForm = this.ownerForm;

			return newServers;
		}

        // ����һ������������ݹ��뱾����
        public void Import(ServerCollection servers)
        {
            this.Clear();
            this.AddRange(servers);
            this.m_bChanged = true;

            // �����ӵĶ���
            ServerChangedEventArgs e = new ServerChangedEventArgs();
            e.Url = "";
            e.ServerChangeAction = ServerChangeAction.Import;
            OnServerChanged(this, e);

        }

		// ����һ���µ�Server����
		// return:
		//		-1	����
		//		0	������
		//		1	�����ظ���û�м���
		public int NewServer(string strUrl,
			int nInsertPos)
		{
			Server server = null;
			/*
			server = this.GetServer(strUrl);

			if (server != null)
				return 1;
			*/	// ��ʱ��ȥ��

			server = new Server();
			server.Url = strUrl;

			if (nInsertPos == -1)
				this.Add(server);
			else 
				this.Insert(nInsertPos, server);

			m_bChanged = true;

            ServerChangedEventArgs e = new ServerChangedEventArgs();
            e.Url = strUrl;
            e.ServerChangeAction = ServerChangeAction.Add;
            OnServerChanged(this, e);

			return 0;
		}


        public void OnServerChanged(object sender, ServerChangedEventArgs e)
        {
            if (this.ServerChanged != null)
            {
                this.ServerChanged(sender, e);
            }

        }

		// ����һ���µ�Server����
		// return:
		//		-1	����
		//		0	������
		//		1	�����ظ���û�м���
		public Server NewServer(int nInsertPos)
		{
			Server server = null;
			server = new Server();

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
		public static ServerCollection Load(
			string strFileName,
			bool bIgnorFileNotFound)
		{
			Stream stream = null;
			ServerCollection servers = null;

			try 
			{
				stream = File.Open(strFileName, FileMode.Open);
			}
			catch (FileNotFoundException ex)
			{
				if (bIgnorFileNotFound == false)
					throw ex;

				servers = new ServerCollection();
				servers.m_strFileName = strFileName;

				// �õ�����һ���µĿն������
				return servers;
			}


			BinaryFormatter formatter = new BinaryFormatter();

			servers = (ServerCollection)formatter.Deserialize(stream);
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
				throw(new Exception("ServerCollection.Save()û��ָ�������ļ���"));
			}

			Stream stream = File.Open(strFileName,
				FileMode.Create);

			BinaryFormatter formatter = new BinaryFormatter();

			formatter.Serialize(stream, this);
			stream.Close();
		}

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
            dlg.Font = GuiUtil.GetDefaultFont();

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
                RmsChannel channel = e.Channel; // 2013/2/14
                if (channel == null)
                    channel = e.Channels.GetChannel(dlg.textBox_serverAddr.Text);

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

        /*
		// ���ȱʡ�ʻ���Ϣ
		// return:
		//		2	already login succeed
		//		1	dialog return OK
		//		0	dialog return Cancel
		//		-1	other error
		public int AskAccountInfo(ChannelCollection Channels, 
			string strComment,
			string strUrl,
			string strPath,
			LoginStyle loginStyle,
			out IWin32Window owner,	// �����Ҫ���ֶԻ������ﷵ�ضԻ��������Form
			out string strUserName,
			out string strPassword)
		{
			bool bFirst = true;

			bool bAutoLogin = ( loginStyle & LoginStyle.AutoLogin ) == LoginStyle.AutoLogin;
			bool bFillDefault = ( loginStyle & LoginStyle.FillDefaultInfo ) == LoginStyle.FillDefaultInfo;

			owner = ownerForm;
			strUserName = "";
			strPassword = "";

			LoginDlg dlg = new LoginDlg();

			Server server = this[strUrl];

			dlg.textBox_serverAddr.Text = strUrl;
			if (bFillDefault == true)
			{
				if (server != null) 
				{
					dlg.textBox_userName.Text = ( server.DefaultUserName == "" ? "public" : server.DefaultUserName );
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

			if (strComment != null)
				dlg.textBox_comment.Text = strComment;

			DOLOGIN:
			if (Channels != null) 
			{
				if (bAutoLogin == false && bFirst == true)
					goto REDOINPUT;

				// �ҵ�Channel
				Channel channel = Channels.GetChannel(dlg.textBox_serverAddr.Text);

				Debug.Assert(channel != null, "Channels.GetChannel()�쳣...");


				string strError;
				// ��¼
				int nRet = channel.Login(dlg.textBox_userName.Text,
					dlg.textBox_password.Text,
					out strError);
		
				if (nRet != 1) 
				{
					if (ownerForm != null) 
					{
						MessageBox.Show(ownerForm, "���û��� '" + dlg.textBox_userName.Text + "' ��¼�� '" + dlg.textBox_serverAddr.Text + "' ʧ��: " + strError);
					}

					goto REDOINPUT;
				}
				else // ��¼�ɹ�
				{
					if (String.Compare(strUrl, dlg.textBox_serverAddr.Text, true) != 0) 
					{
                        // ����һ���µ�Server����
                        // return:
                        //		-1	����
                        //		0	������
                        //		1	�����ظ���û�м���
						nRet = this.NewServer( dlg.textBox_serverAddr.Text, -1);
					}

					server = this[dlg.textBox_serverAddr.Text];

					Debug.Assert(server != null, "��ʱserver������Ϊnull");

					server.DefaultUserName = dlg.textBox_userName.Text;
					server.DefaultPassword = dlg.textBox_password.Text;
					server.SavePassword = dlg.checkBox_savePassword.Checked;
					m_bChanged = true;

					return 2;
				}
			}


			REDOINPUT:
				bFirst = false;

				dlg.ShowDialog(ownerForm);

			if (dlg.DialogResult != DialogResult.OK)
				return 0;

			if (Channels == null) 
			{
				strUserName = dlg.textBox_userName.Text;
				strPassword = dlg.textBox_password.Text;

				return 1;
			}


			goto DOLOGIN;
		}
         */
	}

    // �¼�: �������ɾ���˷�����
    public delegate void ServerChangedEventHandle(object sender,
    ServerChangedEventArgs e);

    public class ServerChangedEventArgs : EventArgs
    {
        public string Url = ""; // ������URL
        public ServerChangeAction ServerChangeAction = ServerChangeAction.None; // �������ĸı�����
    }

    public enum ServerChangeAction
    {
        None = 0,
        Add = 1,
        Remove = 2,
        Import = 3,
    }
}
