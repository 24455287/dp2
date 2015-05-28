using System;
using System.Net;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;

using DigitalPlatform.GcatClient.gcat_ws;
using DigitalPlatform.GUI;



namespace DigitalPlatform.GcatClient
{
	public delegate void BeforeLoginEventHandle(object sender,
	BeforeLoginEventArgs e);

	public class BeforeLoginEventArgs: EventArgs
	{
		public string GcatServerUrl = "";
		public string UserName = "";
		public string Password = "";
		public bool SavePassword = false;
		public bool Failed = false;
		public bool Cancel = false;
	}

	public class Channel
	{
		public string Url = "";

		gcat m_ws = null;	// ӵ��

		public string UserName = "";
		public string Password = "";
//		System.Windows.Forms.IWin32Window parent = null;

		// IAsyncResult soapresult = null;

		public CookieContainer Cookies = new System.Net.CookieContainer();

		public event BeforeLoginEventHandle BeforeLogin;

        object resultParam = null;
        AutoResetEvent eventComplete = new AutoResetEvent(false);

		public gcat ws 
		{
			get 
			{
				if (m_ws == null) 
				{
					m_ws = new gcat();
					// m_ws.Timeout = 1*60*1000;	// 5���� //1000;
					// m_ws.RequestSoapContext.Security.Timestamp.TtlInSeconds = 3600*10;

				}
				// m_ws.RequestSoapContext.Security.Timestamp.TtlInSeconds = 3600*10;

				Debug.Assert(this.Url != "", "Urlֵ��ʱӦ�������ڿ�");

				m_ws.Url = this.Url;
				m_ws.CookieContainer = this.Cookies;

				return m_ws;
			}
		}

        public void Abort()
        {
            // ws.Abort();
            ws.CancelAsync(null);
        }

		public int Login(string strUserName,
			string strPassword,
			out string strError)
		{
			strError = "";

			int nRet = ws.Login(strUserName,
				strPassword,
				out strError);
			if (nRet == -1)
				return -1;


			return nRet;
		}

        // �첽�汾�������ж�
        // return:
        //		-3	��Ҫ�ش�����
        //      -2  ��δ��¼(info.UserIDΪ��)
        //      -1  ����
        //      0   �ɹ�
        public int GetNumber(
            DigitalPlatform.Stop stop,
            string strAuthor,
            bool bSelectPinyin,
            bool bSelectEntry,
            bool bOutputDebugInfo,
            out string strNumber,
            out string strDebugInfo,
            out string strError)
        {
            strNumber = "";
            strDebugInfo = "";
            strError = "";

        REDO:
            ws.GetNumberCompleted +=new GetNumberCompletedEventHandler(ws_GetNumberCompleted);

            try
            {

                this.eventComplete.Reset();
                ws.GetNumberAsync(strAuthor,
                    bSelectPinyin,
                    bSelectEntry,
                    bOutputDebugInfo);

                while (true)
                {
                    Application.DoEvents();	// ���ý������Ȩ

                    if (stop != null)
                    {
                        if (stop.State != 0)
                        {
                            strError = "�û��ж�1";
                            return -1;
                        }
                    }

                    bool bRet = this.eventComplete.WaitOne(10, true);
                    if (bRet == true)
                        break;
                }

            }
            finally
            {
                ws.GetNumberCompleted -= new GetNumberCompletedEventHandler(ws_GetNumberCompleted);
            }

            GetNumberCompletedEventArgs e = (GetNumberCompletedEventArgs)this.resultParam;

            if (e.Error != null)
            {
                strError = e.Error.Message;
                return -1;
            }

            if (e.Cancelled == true)
                strError = "�û��ж�2";
            else
                strError = e.strError;

            strNumber = e.strNumber;
            strDebugInfo = e.strDebugInfo;

            if (e.Result == -2)
            {
                if (DoNotLogin(ref strError) == 1)
                    goto REDO;
            }

            return e.Result;
        }

        void ws_GetNumberCompleted(object sender, GetNumberCompletedEventArgs e)
        {
            this.resultParam = e;
            this.eventComplete.Set();
        }

		// return:
		//		-2	��δ��¼
		//		-1	����
		//		0	�ɹ�
		public int GetNumber(string strAuthor,
			bool bSelectPinyin,
			bool bSelectEntry,
			bool bOutputDebugInfo,
			out string strNumber,
			out string strDebugInfo,
			out string strError)
		{
			REDO:
			int nRet = ws.GetNumber(strAuthor,
				bSelectPinyin,
				bSelectEntry,
				bOutputDebugInfo,
				out strNumber,
				out strDebugInfo,
				out strError);
			if (nRet == -2)
			{
				if (this.BeforeLogin == null)
					return -2;

				BeforeLoginEventArgs newargs = new BeforeLoginEventArgs();
				newargs.GcatServerUrl = this.Url;
			REDOLOGIN:
				this.BeforeLogin(this, newargs);
				if (newargs.Cancel == true)
				{
					strError = "�û�����";
					return -1;
				}
				if (newargs.UserName == "")
				{
					strError = "UserName����Ϊ��";
					return -1;
				}

				nRet = Login(newargs.UserName,
					newargs.Password,
					out strError);
				if (nRet != 1)
				{
					newargs.Failed = true;
					if (newargs.SavePassword == false)
						newargs.Password = "";
					goto REDOLOGIN;
					/*
					if (nRet == 0)
					{
						return -1;
					}
					return nRet;
					*/
				}

				goto REDO;

			}

			return nRet;
		}

        // �����¼����
        // 2007/6/29 new add
        int DoNotLogin(ref string strError)
        {
            if (this.BeforeLogin != null)
            {
                BeforeLoginEventArgs newargs = new BeforeLoginEventArgs();
                newargs.GcatServerUrl = this.Url;
            REDOLOGIN:
                this.BeforeLogin(this, newargs);
                if (newargs.Cancel == true)
                {
                    strError = "�û�����";
                    return -1;
                }
                if (newargs.UserName == "")
                {
                    strError = "UserName����Ϊ��";
                    return -1;
                }

                int nRet = Login(newargs.UserName,
                    newargs.Password,
                    out strError);
                if (nRet != 1)
                {
                    newargs.Failed = true;
                    if (newargs.SavePassword == false)
                        newargs.Password = "";
                    goto REDOLOGIN;

                }

                return 1;   // ��¼�ɹ�,��������API������
            }

            return -1;
        }

		public void Clear()
		{
            ws.Clear();
		}

#if NO
        string ConvertWebError(Exception ex0)
        {
            if (ex0 is WebException)
            {
                WebException ex = (WebException)ex0;

                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    return "����ʱ(��ǰ��ʱ����Ϊ" + Convert.ToString(this.ws.Timeout) + ")";
                }
                if (ex.Status == WebExceptionStatus.RequestCanceled)
                {
                    return "�û��ж�";
                }

                return ex.Message;
            }

            return ex0.Message;
        }
#endif

		public int GetQuestion(out string strQuestion,
			out string strError)
		{
			strError = "";

			int nRet = ws.GetQuestion(out strQuestion,
				out strError);

			return nRet;
		}

		public int Answer(
			string strQuestion,
			string strAnswer,
			out string strError)
		{
			strError = "";

			int nRet = ws.Answer(strQuestion,
				strAnswer,
				out strError);

			return nRet;
		}


		// ���ű�����
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        public int GetNumber(
			System.Windows.Forms.IWin32Window parent,
			string strUrl,
			string strAuthor,
			bool bSelectPinyin,
			bool bSelectEntry,
			bool bOutputDebugInfo,
			BeforeLoginEventHandle e,
			out string strNumber,
			out string strDebugInfo,
			out string strError)
		{
			strError = "";
			strDebugInfo = "";

			Channel channel = this;

			// this.parent = parent;

			// channel.BeforeLogin -= new BeforeLoginEventHandle(channel_BeforeLogin);
			// channel.BeforeLogin += new BeforeLoginEventHandle(channel_BeforeLogin);

			channel.BeforeLogin -= e;
			channel.BeforeLogin += e;


			channel.Url = strUrl;

			strNumber = "";

			int nRet = 0;

			channel.Clear();

			for(;;) 
			{
                // return:
                //		-3	��Ҫ�ش�����
                //      -2  ��δ��¼(info.UserIDΪ��)
                //      -1  ����
                //      0   �ɹ�
				nRet = channel.GetNumber(strAuthor,
					bSelectPinyin,
					bSelectEntry,
					bOutputDebugInfo,
					out strNumber,
					out strDebugInfo,
					out strError);
                if (nRet == 0)
                    return 1;
                if (nRet == -2)
                    return -1;
                if (nRet != -3)
                {
                    return -1;
                    //break;
                }

				Debug.Assert(nRet == -3, "");

				string strTitle = strError;

				string strQuestion = "";

				nRet = channel.GetQuestion(out strQuestion,
					out strError);
				if (nRet != 1)
				{
                    return -1;
                    /*
					nRet = -1;
					break;
                     * */
				}

				QuestionDlg dlg = new QuestionDlg();
                GuiUtil.AutoSetDefaultFont(dlg);    // 2015/5/28
                dlg.StartPosition = FormStartPosition.CenterScreen;
				dlg.label_messageTitle.Text = strTitle;
				dlg.textBox_question.Text = strQuestion.Replace("\n","\r\n");
				dlg.ShowDialog(parent);

				if (dlg.DialogResult != DialogResult.OK)
				{
                    return 0;   // ��ʾ������(�����ش�����)
                    /*
					nRet = 0;
					break;
                     * */
				}

				nRet = channel.Answer(strQuestion,
					dlg.textBox_result.Text,
					out strError);
				if (nRet != 1) 
				{
                    /*
					nRet = -1;
					break;
                     * */
                    return -1;
				}
			}

            /*
			if (nRet == -1)
				return -1;
	
			return 1;
             * */
		}

        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        public int GetNumber(
            Stop stop,
            System.Windows.Forms.IWin32Window parent,
            string strUrl,
            string strAuthor,
            bool bSelectPinyin,
            bool bSelectEntry,
            bool bOutputDebugInfo,
            BeforeLoginEventHandle e,
            out string strNumber,
            out string strDebugInfo,
            out string strError)
        {
            strError = "";
            strDebugInfo = "";

            Channel channel = this;

            // this.parent = parent;

            // channel.BeforeLogin -= new BeforeLoginEventHandle(channel_BeforeLogin);
            // channel.BeforeLogin += new BeforeLoginEventHandle(channel_BeforeLogin);

            channel.BeforeLogin -= e;
            channel.BeforeLogin += e;


            channel.Url = strUrl;

            strNumber = "";

            int nRet = 0;

            try
            {
                channel.Clear();
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            for (; ; )
            {
                // �����������catch ͨѶ�� exeption������
                nRet = channel.GetNumber(
                    stop,
                    strAuthor,
                    bSelectPinyin,
                    bSelectEntry,
                    bOutputDebugInfo,
                    out strNumber,
                    out strDebugInfo,
                    out strError);
                if (nRet != -3)
                    break;

                Debug.Assert(nRet == -3, "");

                string strTitle = strError;

                string strQuestion = "";

                nRet = channel.GetQuestion(out strQuestion,
                    out strError);
                if (nRet != 1)
                {
                    nRet = -1;
                    break;
                }

                QuestionDlg dlg = new QuestionDlg();
                GuiUtil.AutoSetDefaultFont(dlg);    // 2015/5/28
                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.label_messageTitle.Text = strTitle;
                dlg.textBox_question.Text = strQuestion.Replace("\n", "\r\n");
                dlg.ShowDialog(parent);

                if (dlg.DialogResult != DialogResult.OK)
                {
                    nRet = 0;
                    break;
                }

                nRet = channel.Answer(strQuestion,
                    dlg.textBox_result.Text,
                    out strError);
                if (nRet != 1)
                {
                    nRet = -1;
                    break;
                }
            }

            if (nRet == -1)
                return -1;

            return 1;
        }

	}
}
