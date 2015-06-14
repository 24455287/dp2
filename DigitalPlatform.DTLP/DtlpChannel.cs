using System;
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Windows.Forms;

using DigitalPlatform;
using DigitalPlatform.Marc;
using DigitalPlatform.Xml;

namespace DigitalPlatform.DTLP
{
	/// <summary>
	/// һ��tcpsͨѶͨ��
	/// </summary>
	public class DtlpChannel
	{

		#region DTLP API Definition
		// --------------------------------
		// DTLP API ������
		public const int GL_INVALIDPATH      = 0x0001;
		public const int GL_OUTOFRANGE       = 0x0003;
		public const int GL_INVALIDCHANNEL   = 0x0005;

		public const int GL_HANGUP           = 0x0021;
		public const int GL_NORESPOND        = 0x0023;
		public const int GL_NEEDPASS         = 0x0025;

		public const int GL_ACCESSDENY       = 0x0033;
		public const int GL_RAP              = 0x0035;
		public const int GL_NOTEXIST         = 0x0037;
		public const int GL_NOMEM            = 0x0039;
		public const int GL_NOCHANNEL        = 0x003B;

		public const int GL_ERRSIGNATURE     = 0x003D;
		public const int GL_NOTLOGIN         = 0x003F;

		public const int GL_OVERFLOW         = 0x0041;
		public const int GL_CONNECT          = 0x0042;
		public const int GL_SEND             = 0x0043;
		public const int GL_RECV             = 0x0044;
		public const int GL_PARATOPACKAGE    = 0x0045;
		public const int GL_PACKAGETOPARA    = 0x0046;
		public const int GL_PACKAGENTOH      = 0x0047;     // �ڽ��Ͱ�ʱntoh��������
		public const int GL_REENTER          = 0x0048;
		public const int GL_INTR             = 0x0049;      // TCP/IP�������ж�


		//
		public const int ReservedPaths       = 3;
		/* newapi access attribute */
		public const int AttrIsleaf          = 0x00000001;
		public const int AttrSearch          = 0x00000002;
		public const int AttrWildChar        = 0x00000004;
		public const int AttrExtend          = 0x00000008;
		public const int AttrTcps            = 0x00000010;
		public const int AttrRdOnly          = 0x00001000;
		/* newapi type attribute   */
		public const int TypeStdbase         = 0x00010000;
		public const int TypeSmdbase         = 0x00020000;
		public const int TypeStdfile         = 0x00040000;
		public const int TypeCfgfile         = 0x00080000;
		public const int TypeFrom            = 0x00100000;
		public const int TypeKernel          = 0x00200000;
		public const int TypeHome            = 0x00400000;
		public const int TypeBreakPoint      = 0x00800000;
		public const int TypeCdbase          = (TypeStdbase | AttrRdOnly);

		public const int TypeServerTime      =	0x01000000;

		//
		public const int FUNC_CREATECHANNEL  = 0x2100;
		public const int FUNC_DESTROYCHANNEL = 0x2300;
		public const int FUNC_CHDIR          = 0x2500;
		public const int FUNC_DIR            = 0x2700;
		public const int FUNC_SEARCH         = 0x2900;
		public const int FUNC_WRITE          = 0x2B00;
		public const int FUNC_GETLASTERRNO   = 0x2D00;
		public const int FUNC_ACCMANAGEMENT  = 0xD100;

		/* ; dbnames                 0x3*00   */
		public const int FUNC_GETAVAILABLEDBS = 0x3100;
		public const int FUNC_DBINIT         = 0x3200;
		public const int FUNC_DBOPEN         = 0x3300;
		public const int FUNC_DBCLOSE        = 0x3500;
		public const int FUNC_GETFROM        = 0x3700;
		public const int FUNC_GETLASTNUMBER  = 0x3900;
		public const int FUNC_MODSUBASE      = 0x3B00;
		public const int FUNC_DELSUBASE      = 0x3D00;
		/* errno */
		public const int DB_NOTEXIST         = 0x3001;
		public const int DB_LOCKED           = 0x3003;
		public const int DB_CONFLICT         = 0x3005;

		/* ; simple dbase            0x4*00   */
		public const int FUNC_SMALLDBASE     = 0x4100;
		public const int FUNC_SMDBINIT       = 0x4300;
		public const int FUNC_SMWRITERECORD  = 0x4500;
		public const int FUNC_SMGETRECORD    = 0x4700;
		public const int FUNC_SMDELETERECORD = 0x4900;

		/* ; search                  0x5*00   */
		public const int FUNC_HITRECORDNUMS  = 0x5100;

		/* ; get records             0x7*00   */
		public const int FUNC_GETRECORDS     = 0x7100;
		public const int FUNC_GETNEXTRECORD  = 0x7300;
		/* errno  */
		public const int GR_PARTMISSING      = 0x7001;
		public const int GR_PARTLOCKED       = 0x7002;
		public const int GR_PARTRDDISABLE    = 0x7004;

		/* ; write records           0x9*00   */
		public const int FUNC_WRITERECORD    = 0x9100;
		/* errno  */
		public const int WR_ACCOUNTFULL      = 0x9001;
		public const int WR_WTDISABLE        = 0x9003;
		public const int WR_LOCKED           = 0x9005;

		/* ; lock records            0xA*00   */
		public const int FUNC_LOCKRECORD     = 0xA100;
		public const int FUNC_LOGICLOCK      = 0xA300;
		/* errno  */
		public const int LR_OVERFLOW         = 0xA001;
		public const int LR_CONFLICT         = 0xA003;
		public const int LR_NOTEXIST         = 0xA005;

		/* ; delete records          0xB*00   */
		public const int FUNC_DELETERECORD   = 0xB100;
		/* errno  */
		public const int DR_NOTEXIST         = 0xB001;
		public const int DR_LOCKED           = 0xB003;

		/* ; account management      0xD*00   */
		public const int FUNC_MANAGEMENT  = 0xD100;
		/* errno */
		public const int LG_BADNAME          = 0xD001;
		public const int LG_BADPASS          = 0xD003;
		public const int AS_NOTEXIST         = 0xD005;
		public const int AS_DUPLICATE        = 0xD007;
		public const int AS_FULL             = 0xD009;
		public const int AS_OVERDRAFT        = 0xD00B;

		/* ; file access             0xE*00   */
		public const int FUNC_OPENHOSTFILE   = 0xE100;
		public const int FUNC_CLOSEHOSTFILE  = 0xE300;
		public const int FUNC_GETHOSTFILE    = 0xE500;
		public const int FUNC_PUTHOSTFILE    = 0xE700;

		/* ; config file             0xF*00   */
		public const int FUNC_GETCONFIGNAME  = 0xF100;
		public const int FUNC_OPENCONFIG     = 0xF300;
		public const int FUNC_CLOSECONFIG    = 0xF500;
		public const int FUNC_GETENTRY       = 0xF700;
		public const int FUNC_PUTENTRY       = 0xF900;

		/* errno */
		public const int CS_NOTFOUND         = 0xF001;

		// ------------------------------------
		// ����Search���
		public const int JH_STYLE            = 0x0001;
		public const int Z3950_BRIEF_STYLE	 = 0x0002;	 
		public const int XX_STYLE            = 0x0003;
		public const int CTRLNO_STYLE        = 0x0005;
		public const int ISO_STYLE           = 0x0007;
		public const int WOR_STYLE           = 0x0009;
		public const int SEED_STYLE          = 0x000B;
		public const int KEY_STYLE           = 0x000D;
		public const int RIZHI_STYLE         = 0x000F;
		public const int SIGMSG_STYLE        = 0x0011;
		// ����SearchStyle����õ�����
		public const int MASK_OF_STYLE		=	0x00FF;
		// ע:����???_STYLEֵ���жϣ�Ҫ�������淽ʽ��
		// if ( (lStyle & MASK_OF_STYLE) == ???_STYLE) {...}
		// **����**�������淽ʽ��
		// if (lStyle & ???_STYLE) {...}	// ���Ǵ�����÷�
		// -------------------------------------


		public const int PREV_RECORD         = 0x0100;
		public const int NEXT_RECORD         = 0x0200;
		public const int EXACT_RECORD        = 0x0400;
		public const int SAME_RECORD         = 0x0800;
		public const int FIRST_RECORD        = 0x1000;
		public const int LAST_RECORD         = 0x2000;
		public const int AMOUNT_RECORD       = 0x4000;
		public const int CONT_RECORD         = 0x8000;

		// ------------------------------------
		// ����Write���
		public const int APPEND_WRITE		=	0x0001;
		public const int REPLACE_WRITE		=	0x0003;
		public const int DELETE_WRITE		=	0x0005;
		public const int GETKEYS_WRITE		=	0x0007;
		public const int RIZHI_WRITE		=	0x0009;
		public const int REBUILD_WRITE		=	0x000B;
		public const int PATPASS_WRITE		=	0x000D;
		// ����Write����õ�����
		public const int MASK_OF_WRITE		=	0x000F;
		// ע:����???_WRITEֵ���жϣ�Ҫ�������淽ʽ��
		// if ( (lStyle & MASK_OF_WRITE) == ???_WRITE) {...}
		// **����**�������淽ʽ��
		// if (lStyle & ???_WRITE) {...}	// ���Ǵ�����÷�
		// -------------------------------------

		public const int WRITE_NO_LOG		=	0x0100;	// (DTLP 1.0����) ��������־


		//
		#endregion

		public DtlpChannelArray Container = null;

		public bool PreferUTF8 = true;

		public const int CHARSET_DBCS = 0;
		public const int CHARSET_UTF8 = 1;
		//int	m_nDTLPCharset = CHARSET_DBCS;
		//bool m_bForceDBCS = true;

		public int	m_lUsrID = 0;

		int	    m_lErrno = 0;
		//ArrayList	m_baSendBuffer = null;// ���������õĻ�����
		//ArrayList	m_baRecvBuffer = null;// ������Ӧ�õĻ�����

		// bool	m_bStop = false;			// �жϱ��

		HostArray	m_HostArray = new HostArray();	// ��������,ӵ��

		int		m_nResponseFuncNum = 0;

		public int	m_nResultMaxLen = 60000;

		public DtlpChannel()
		{
		}

		public void InitialHostArray()
		{
			m_HostArray.InitialHostArray(this.Container.appInfo);
			m_HostArray.Container = this;
		}


		// ��ͨ��׼��������
		// �����������û�У��ʹ��������ӡ������δ��¼����
		// �ͽ��е�¼��
		// ����ֵ��CHostEntry�����CHostEntry::m_lChannel��Ҳ�ɻ��Զ��Channel
		//		NULL��ʾʧ��
		HostEntry PrepareChannel(string strHostName)
		{
			HostEntry	entry = null;
			int nErrorNo = 0;
			int nRet;

	
			// Ѱ�����е�����
			entry = m_HostArray.MatchHostEntry(strHostName);
			if (entry == null) 
			{
				// �������������
				entry = new HostEntry();
				Debug.Assert(entry!=null, "new HostEntry Failed ...");
				if (entry == null)
					return null;
				m_HostArray.Add(entry);
				entry.Container = m_HostArray;

			}

			// ���TCP/IP������δ����
			if (entry.client == null) 
			{
				nRet = entry.ConnectSocket(strHostName,
					out nErrorNo);
				if (nRet == -1) 
				{
					// ��Ҫ����������
					m_lErrno = GL_CONNECT; // ��ϸԭ�������������ڣ�connect()ʧ�ܵ�
					return null;
				}
			}
			// �����δ��������¼��
			if (entry.m_lChannel == -1L) 
			{
				int nHandle;
		
				nRet = entry.RmtCreateChannel(m_lUsrID);
				if (nRet == -1)
					return null;

				Debug.Assert(entry.m_lChannel != -1,
					"entry.m_lChannelֵ����ȷ");

				nHandle = nRet;

				// �ַ���Э�̹���
				if (PreferUTF8 == true) 
				{
					byte [] baResult = null;
					string strPath = "";

					strPath = strHostName;
					// advstrPath += "/Initial/Charset/?";
					nRet = API_Management(strPath,
						"Initial",
						"Encoding/?",
						out baResult);
					if (nRet == -1) 
					{
						// ����ΪDTLP 0.9����֧���ַ���Э��
						entry.m_nDTLPCharset = CHARSET_DBCS;
						// m_bForceDBCS = true;
					}
					else 
					{
						strPath = strHostName;
						// advstrPath += "/Initial/Charset/UTF8";
						nRet = API_Management(strPath,
							"Initial",
							"Encoding/UTF8",
							out baResult);
						if (nRet != -1) 
						{
							// ֧���ַ����л�ΪUTF8
							entry.m_nDTLPCharset = CHARSET_UTF8;
							// �Ӵ��Ժ����Ҫʹ��UTF8����ͨѶ��
							// m_bForceDBCS = false;

						}
					}
			
				}
				else 
				{
					// m_bForceDBCS = true;
				}




			}
	
			return entry;
		}



		// �õ�������
		public int GetLastErrno()
		{
			return this.m_lErrno;
		}

		// �������ڽ��еĲ���
		public bool Cancel()
		{
			// this.m_bStop = true;
	
			this.m_HostArray.CloseAllSockets();

			return true;
		}

		int GetLocalCharset()
		{
			if (IsHostActiveUTF8("") == true)
				return CHARSET_UTF8;
			return CHARSET_DBCS;
		}

		// ���һ�����������ǲ���utf-8����
		public bool IsHostActiveUTF8(string strHostName)
		{
			if (strHostName == "") // ������!
			{
				if (PreferUTF8 == true)
					return true;
				else 
					return false;
			}

			HostEntry	entry = null;
	
			// Ѱ�����е�����
			entry = m_HostArray.MatchHostEntry(strHostName);
			if (entry == null) 
			{
				return false;	// not active
			}

			if (entry.m_nDTLPCharset == CHARSET_UTF8)
				return true;

			return false;
		}

		// ���һ��·��,����/�����Ƿ�Ϊutf-8����
		// return:
		//		false	is not UTF8 (is DBCS)
		//		true	is UTF8
		public bool API_IsActiveUTF8(string strPath)
		{
			string strMyPath = null;
			string strOtherPath = null;

			//if (nHandle == 0 && nHandle == -1)
			//	return -1;

			SplitPath(strPath, out strMyPath, out strOtherPath);

			if (strMyPath == "") 
			{
				if (PreferUTF8)
					return true;
				return false;
			}

			return IsHostActiveUTF8(strMyPath);
		}

		// ��API_IsActiveUTF8()��װһ��,����ʹ��
		public Encoding GetPathEncoding(string strPath)
		{
			if (API_IsActiveUTF8(strPath) == true)
				return Encoding.UTF8;
			return Encoding.GetEncoding(936);
		}


		// ���ֻ㱨����ĶԻ���
		public void ErrorBox(IWin32Window owner,
			string strTitle,
			string strText)
		{
			int nErrorCode = GetLastErrno();

			string strError = GetErrorString(nErrorCode);

			// string strHex = String.Format("0X{0,8:X}",nErrorCode);
            string strHex = "0X" + Convert.ToString(nErrorCode, 16).PadLeft(8, '0');

			strError =	strText
				+ "\n----------------------------------\n������"
				+ strHex + ", ԭ��: "
				+ strError;

			MessageBox.Show(owner, strError, strTitle);
		}

        public string GetErrorDescription()
        {
            int nErrorCode = GetLastErrno();

            string strError = GetErrorString(nErrorCode);

            string strHex = "0X" + Convert.ToString(nErrorCode, 16).PadLeft(8, '0');

            return "������"
                + strHex + ", ԭ��: "
                + strError;
        }

		public string GetErrorString(int lErrno)
		{
			switch (lErrno) 
			{
				case GL_INTR:
					return "TCP/IPͨѶ�ж� Communication Interrupted";
				case GL_INVALIDPATH:
					return "��Ч���� Invalid Parameter";
				case GL_INVALIDCHANNEL:
					return "��Чͨ�� Invalid Channel";
				case GL_REENTER:
					return "���� Re-Enter";
				case GL_OUTOFRANGE:
					return "����ֵ������Ч��Χ Parameter Out of Range";
				case GL_HANGUP:
					return "�������Ѿ����� Server Has Hang Up";
				case GL_NORESPOND:
					return "������û����Ӧ Server Not Respond ";
				case GL_ACCESSDENY:
					return "Ȩ�޲���, �ܾ���ȡ Not Enough Right, Access Denied";
				case GL_RAP:
					return "��Ҫ������� Need Reverse Path";
				case GL_NOTEXIST:
					return "������ Not Exist";
				case GL_NOMEM:
					return "���ز���Ԥ���ռ䲻�� Not Enough Parameter Memory";
				case GL_NOCHANNEL:
					return "ͨ������ Not Enough Channel";
				case GL_ERRSIGNATURE:
					return "Write()������ʱ�����ƥ�� Error Write() Signature";
				case GL_NOTLOGIN:
					return "��δ��¼ Not Login";
				case GL_OVERFLOW:
					return "��� Overflow";
				case GL_CONNECT:
					return "ͨѶ����ʧ�� Connect Fail";
				case GL_SEND:
					return "ͨѶ����ʧ�� Send Fail";
				case GL_RECV:
					return "ͨѶ����ʧ�� Recieve Fail";
				case GL_PARATOPACKAGE:
					return "��ת��ʧ�� ParaToPackage Fail";
				case GL_NEEDPASS:
					return "��Ҫ������Ϣ Need Authentication";
				case LG_BADNAME:
					return "������û��� Bad Username";
				case LG_BADPASS:
					return "����Ŀ����� Bad Password";
				case AS_NOTEXIST:
					return "�ʻ������� Account Not Exist";
				case AS_DUPLICATE:
					return "�ʻ��ظ� Account Duplicate";
				case AS_FULL:
					return "�ʻ����� Account Full";
				case WR_WTDISABLE:
					return "���߱�дȨ�� Not Allow Write";
				case DR_NOTEXIST:
					return "ɾ����¼ʱԭ��¼������ Record Not Exist";
				default:
				{
					return "δ֪���� unknown error [" +  String.Format("0X{0,8:X}",lErrno) +"]";
				}
			}
		}

		// *** API ChDir
		public int API_ChDir(string strUserName,
			string strPassword,
			string strPath,
			//		int lPathLen, // ������β�� 0 �ַ�
			out byte [] baResult)
		{
			// send:long Channel
			//      buff lpszUserName
			//      buff lpszPassword
			//      buff lpszPath
			//      long lPathLen
			//      long lResultMaxLen
			// recv:buff lpResult
			// return long
			baResult = null;
	
			DTLPParam param = new DTLPParam();
			int    lRet;
			int     nRet;
			string strMyPath = null;
			string strOtherPath = null;

			HostEntry entry = null;
			int nLen;
			int nErrorNo;
			byte [] baSendBuffer = null;
			byte [] baRecvBuffer = null;

			// int lResultMaxLen = 60000;
	
			SplitPath(strPath, out strMyPath, out strOtherPath);
	
			if (strMyPath == "") 
			{
		
				lRet = NullPackage(out baResult);
				return lRet;
			}
	
			entry = this.PrepareChannel(strMyPath);
	
			if (entry == null) 
			{ 
				//TRACE(_T("ChDir() CChannel::PrepareChannel(\"%hs\") return error\n"),
				//	(LPCSTR)strMyPath);
				return -1;
			}

			Debug.Assert(entry.m_lChannel!=-1,
				"m_lChannel����ȷ");
	
			param.Clear();
			param.ParaLong(entry.m_lChannel);
			param.ParaString(strUserName, entry.m_nDTLPCharset);
			param.ParaString(strPassword, entry.m_nDTLPCharset);

			int nDBCSLength = param.ParaString(strOtherPath, entry.m_nDTLPCharset);
			param.ParaLong(nDBCSLength);	// ParaString���÷���������ߴ�

			param.ParaLong(m_nResultMaxLen);
		
			m_lErrno = 0;	//
			lRet = param.ParaToPackage(FUNC_CHDIR,
				m_lErrno,
				out baSendBuffer);
	
	
			// EndPara((LPPARATBL)&ParaTbl);
			if (lRet == -1) 
			{
				m_lErrno = GL_PARATOPACKAGE;
				//TRACE(_T("ChDir() Channel[%08x] ParaToPackage() Errno[%08x]\n"),
				//	Channel,
				//	lpChannel->m_lErrno);
				return -1; // error
			}
	
	
			nRet = entry.SendTcpPackage(baSendBuffer,
				lRet, 
				out nErrorNo);
			if (nRet < 0) 
			{
				this.m_lErrno = GL_SEND;
				//TRACE(_T("ChDir() Channel[%08x] CHostEntry::SendTcpPackage() Errno[%08x]\n"),
				//	Channel,
				//	lpChannel->m_lErrno);
				return -1;
			}

			nRet = entry.RecvTcpPackage(out baRecvBuffer,
				out nLen,
				out nErrorNo);
			if (nRet < 0) 
			{
				//TRACE(_T("ChDir() Channel[%08x] CHostEntry::RecvTcpPackage() Errno[%08x]\n"),
				//	Channel,
				//	lpChannel->m_lErrno);
				this.m_lErrno = nErrorNo;
				return -1;
			}
	
			param.Clear();
			param.DefPara(Param.STYLE_LONG);
			param.DefPara(Param.STYLE_BUFF);

			int nFuncNum = 0;
			try 
			{
				param.PackageToPara(baRecvBuffer,
					ref m_lErrno,
					out nFuncNum);
			}
			catch 
			{
				this.m_lErrno = GL_PACKAGETOPARA;
				return -1;
			}
			this.m_nResponseFuncNum = nFuncNum;
			lRet = param.lValue(0);

	
			//TRACE(_T("ChDir() Channel[%08x] Errno[%08x]\n"),
			//	Channel,
			//	lpChannel->m_lErrno);
	
			if (lRet != -1) 
			{
				if (param.baValue(1) == null) 
				{
					lRet = -1;
					goto WAI;
				}
				int lRet1;
				lRet1 = NtohLvalueInResult(param.baValue(1));
				if (lRet1 == -1) 
				{
					lRet = -1;
					m_lErrno = GL_PACKAGENTOH;
					goto WAI;
				}
				lRet1 = AddCurPath(
					param.baValue(1),
					//param.lValue(0),
					strMyPath,
                    entry.m_nDTLPCharset,
					out baResult);
				if (lRet1 == -1) 
				{
					lRet = -1;
					m_lErrno = GL_NOMEM;
				}
			}
			WAI:
	
				return lRet;
		}

		// *** ��װ���Dir�����Դ����Զ���¼
		public int Dir(string strPath,
			out byte [] baResult)
		{
			int nRet = 0;

			REDO:
				nRet = API_Dir(strPath,
					out baResult);
			if (nRet == -1) 
			{
				int nErrorCode = this.GetLastErrno();
				if (nErrorCode == DtlpChannel.GL_NOTLOGIN) 
				{
					if (this.Container.HasAskAccountInfoEventHandler == false)
						return nRet;	// �޷���ȡ�˻���Ϣ�����ֻ�ðѴ����Ͻ�

					// string strUserName, strPassword;
					// IWin32Window owner = null;
                    AskDtlpAccountInfoEventArgs e = new AskDtlpAccountInfoEventArgs();
                    e.Channel = this;
                    e.Path = strPath;

					// ���ȱʡ�ʻ���Ϣ
					int nProcRet = 0;
                INPUT:

                    // return:
                    //		2	already login succeed
                    //		1	dialog return OK
                    //		0	dialog return Cancel
                    //		-1	other error
                    /*
					nProcRet = this.Container.procAskAccountInfo(
						this, 
                        strPath, 
						out owner,
						out strUserName,
						out strPassword);
                     * */
                    this.Container.CallAskAccountInfo(this, e);
                    nProcRet = e.Result;

					if (nProcRet == 2)
						goto REDO;
					if (nProcRet == 1) 
					{
						byte[] baPackage = null;
						nRet = this.API_ChDir(e.UserName,   // strUserName, 
							e.Password, // strPassword,
							strPath,
							out baPackage);
						if (nRet > 0)
							goto REDO;
                        if (this.Container.GUI == true)
                        {
                            this.ErrorBox(e.Owner,  // owner,
                                "Search()",
                                "Login fail ...");
						goto INPUT;
                        }
					}
					return nRet;	// �����Ͻ�
				}
			}

			return nRet;
		}

		// *** ԭʼDTLP API ��Dir��û�����κΰ�װ
		public int API_Dir(string strPath,
			out byte [] baResult)
		{
	
			// send:long Channel
			//      buff lpszPath
			//      long lPathLen
			//      long lResultMaxLen
			// recv:buff lpResult
			// return long

			baResult = null;

			DTLPParam param = new DTLPParam();
			int    lRet;
			int     nRet;

			string strMyPath = null;
			string strOtherPath = null;

			HostEntry entry = null;

			int nLen;
			int nErrorNo;
	
			byte [] baSendBuffer = null;
			byte [] baRecvBuffer = null;

			// int lResultMaxLen = 60000;

	
			SplitPath(strPath, out strMyPath, out strOtherPath);
	
			if (strMyPath == "") 
			{
				// �һ�û�к�Զ������,��˲�֪�����˵��ַ���.
				// �����Լ����ַ���,��֪����.

				return this.LocalDir(GetLocalCharset(), out baResult);
			}
	
			entry = this.PrepareChannel(strMyPath);
			if (entry == null) 
			{ 
				return -1;
			}

			Debug.Assert(entry.m_lChannel!=-1,
				"m_lChannel����ȷ");
	
			param.Clear();
			param.ParaLong(entry.m_lChannel);
			int nPathLen = param.ParaString(strOtherPath, entry.m_nDTLPCharset);
			param.ParaLong(nPathLen);

			//param.ParaLong(lResultMaxLen-3*(strMyPath.GetLengthA()+1)); // new gai !!!!!
			param.ParaLong(m_nResultMaxLen);
	

			lRet = param.ParaToPackage(FUNC_DIR,
				m_lErrno,
				out baSendBuffer);
			if (lRet == -1) 
			{
				m_lErrno = GL_PARATOPACKAGE;
				return -1; // error
			}
	
			nRet = entry.SendTcpPackage(baSendBuffer,
				lRet, 
				out nErrorNo);
			if (nRet<0) 
			{
				this.m_lErrno = GL_SEND;
				//TRACE(_T("Dir() Channel[%08x] CHostEntry::SendTcpPackage() Errno[%08x]\n"),
				//	Channel,
				//	lpChannel->m_lErrno);
				return -1;
			}

			nRet = entry.RecvTcpPackage(out baRecvBuffer,
				out nLen,
				out nErrorNo);
			if (nRet<0) 
			{
				//TRACE(_T("Dir() Channel[%08x] CHostEntry::RecvTcpPackage() Errno[%08x]\n"),
				//	Channel,
				//	lpChannel->m_lErrno);
				this.m_lErrno = nErrorNo;
				return -1;
			}
	
			param.Clear();
			param.DefPara(Param.STYLE_LONG);
			param.DefPara(Param.STYLE_BUFF);
			int nFuncNum = 0;
			try 
			{
				param.PackageToPara(baRecvBuffer,
					ref m_lErrno,
					out nFuncNum);
			}
			catch 
			{
				this.m_lErrno = GL_PACKAGETOPARA;
				return -1;
			}	
			this.m_nResponseFuncNum = nFuncNum;
			lRet = param.lValue(0);
	
			if (lRet != -1) 
			{
				int lRet1;
				lRet1 = NtohLvalueInResult(param.baValue(1));
				if (lRet1 == -1) 
				{
					lRet = -1;
					m_lErrno = GL_PACKAGENTOH;
					goto WAI;
				}
				lRet1 = AddCurPath(
					param.baValue(1),
					//param.lValue(0),
					strMyPath,
					entry.m_nDTLPCharset,
					out baResult);
				if (lRet1 == -1) 
				{
					lRet = -1;
					m_lErrno = GL_NOMEM;
				}
			}
			WAI:
	
				return lRet;
		}

		// *** ��װ���Search�����Դ����Զ���¼
		public int Search(string strPath,
			int lStyle,
			out byte [] baResult)
		{
			int nRet = 0;

            int nRedoCount = 0;
			REDO:
				nRet = API_Search(strPath,
					lStyle,
					out baResult);
			if (nRet == -1) 
			{
				int nErrorCode = this.GetLastErrno();
				if (nErrorCode == DtlpChannel.GL_NOTLOGIN) 
				{
					if (this.Container.HasAskAccountInfoEventHandler == false)
						return nRet;	// �޷���ȡ�˻���Ϣ�����ֻ�ðѴ����Ͻ�

                    // 2007/7/5
                    if (nRedoCount > 10)
                        return nRet;	// �޷���ȡ�˻���Ϣ�����ֻ�ðѴ����Ͻ�


                    AskDtlpAccountInfoEventArgs e = new AskDtlpAccountInfoEventArgs();
                    e.Channel = this;
                    e.Path = strPath;
					// string strUserName, strPassword;
					// IWin32Window owner = null;
					// ���ȱʡ�ʻ���Ϣ
					int nProcRet = 0;
                INPUT:
                    // return:
                    //		2	already login succeed
                    //		1	dialog return OK
                    //		0	dialog return Cancel
                    //		-1	other error
                    /*
					nProcRet = this.Container.procAskAccountInfo(
						this, strPath, 
						out owner,
						out strUserName,
						out strPassword);
                     * */
                    this.Container.CallAskAccountInfo(this, e);
                    nProcRet = e.Result;

                    if (nProcRet == 2)
                    {
                        nRedoCount++;
                        goto REDO;
                    }
					if (nProcRet == 1) 
					{
						byte[] baPackage = null;
						nRet = this.API_ChDir(e.UserName,   // strUserName, 
							e.Password, // strPassword,
							strPath,
							out baPackage);
						if (nRet > 0)
							goto REDO;
                        if (this.Container.GUI == true)
                        {
                            this.ErrorBox(e.Owner,
                                "Search()",
                                "Login fail ...");
						    goto INPUT;
                        }

					}
					return nRet;	// �����Ͻ�
				}
			}

			return nRet;
		}

		// *** ��װ���Search�����Դ����Զ���¼
		public int Search(string strPath,
			byte[] baNext,
			int lStyle,
			out byte [] baResult)
		{
			int nRet = 0;

			REDO:
				nRet = API_Search(strPath,
					baNext,
					lStyle,
					out baResult);
			if (nRet == -1) 
			{
				int nErrorCode = this.GetLastErrno();
				if (nErrorCode == DtlpChannel.GL_NOTLOGIN) 
				{
					if (this.Container.HasAskAccountInfoEventHandler == false)
						return nRet;	// �޷���ȡ�˻���Ϣ�����ֻ�ðѴ����Ͻ�

                    AskDtlpAccountInfoEventArgs e = new AskDtlpAccountInfoEventArgs();
                    e.Channel = this;
                    e.Path = strPath;
					//string strUserName, strPassword;
					//IWin32Window owner = null;
					// ���ȱʡ�ʻ���Ϣ
					int nProcRet = 0;
                INPUT:
                    // return:
                    //		2	already login succeed
                    //		1	dialog return OK
                    //		0	dialog return Cancel
                    //		-1	other error
                    /*
					nProcRet = this.Container.procAskAccountInfo(
						this, strPath, 
						out owner,
						out strUserName,
						out strPassword);
                     * */
                    this.Container.CallAskAccountInfo(this, e);
                    nProcRet = e.Result;
					if (nProcRet == 2)
						goto REDO;
					if (nProcRet == 1) 
					{
						byte[] baPackage = null;
						nRet = this.API_ChDir(e.UserName,   // strUserName, 
							e.Password, // strPassword,
							strPath,
							out baPackage);
						if (nRet > 0)
							goto REDO;
                        if (this.Container.GUI == true)
                        {
                            this.ErrorBox(e.Owner,
                                "Search()",
                                "Login fail ...");
						goto INPUT;
                        }
                        // ԭ��goto INPUT; ������
					}
					return nRet;	// �����Ͻ�
				}
			}

			return nRet;
		}

		// *** ԭʼDTLP API ��Search��û�����κΰ�װ
		public int API_Search(string strPath,
			int lStyle,
			out byte [] baResult)
		{
			// send:long Channel
			//      buff lpszPath
			//      long lPathLen
			//      long lResultMaxLen
			//      long lStyle
			// recv:buff lpResult
			// return long

			baResult = null;

			DTLPParam param = new DTLPParam();
			int    lRet;
			int     nRet;

			string strMyPath = null;
			string strOtherPath = null;

			HostEntry entry = null;
			
			int nLen;
			int nErrorNo;
	
	
			byte [] baSendBuffer = null;
			byte [] baRecvBuffer = null;

			// int lResultMaxLen = 60000;
	
			SplitPath(strPath, out strMyPath, out strOtherPath);
	
			if (strMyPath == "") 
			{
				return this.LocalDir(GetLocalCharset(), out baResult);
			}
	
	
			entry = this.PrepareChannel(strMyPath);
			if (entry == null) 
			{ 
				return -1;
			}
	
	
			if (strOtherPath == "") 
			{
				return this.SingleDir(strMyPath,
					entry.m_nDTLPCharset,
					out baResult);
			}
	

			Debug.Assert(entry.m_lChannel!=-1,
				"m_lChannel����ȷ");
	
			param.Clear();
			param.ParaLong(entry.m_lChannel);
			int nPathLen = param.ParaString(strOtherPath, entry.m_nDTLPCharset);
			param.ParaLong(nPathLen);

			param.ParaLong(m_nResultMaxLen);
			param.ParaLong(lStyle);
	
			lRet = param.ParaToPackage(FUNC_SEARCH,
				m_lErrno,
				out baSendBuffer);
			if (lRet == -1) 
			{
				m_lErrno = GL_PARATOPACKAGE;
				return -1; // error
			}
	
			nRet = entry.SendTcpPackage(baSendBuffer,
				lRet, 
				out nErrorNo);
			if (nRet<0) 
			{
				this.m_lErrno = GL_SEND;
				//TRACE(_T("Dir() Channel[%08x] CHostEntry::SendTcpPackage() Errno[%08x]\n"),
				//	Channel,
				//	lpChannel->m_lErrno);
				return -1;
			}

			nRet = entry.RecvTcpPackage(out baRecvBuffer,
				out nLen,
				out nErrorNo);
			if (nRet<0) 
			{
				//TRACE(_T("Dir() Channel[%08x] CHostEntry::RecvTcpPackage() Errno[%08x]\n"),
				//	Channel,
				//	lpChannel->m_lErrno);
				this.m_lErrno = nErrorNo;
				return -1;
			}
	
			param.Clear();
			param.DefPara(Param.STYLE_LONG);
			param.DefPara(Param.STYLE_BUFF);
			int nFuncNum = 0;
			try 
			{
				param.PackageToPara(baRecvBuffer,
					ref m_lErrno,
					out nFuncNum);
			}
			catch 
			{
				this.m_lErrno = GL_PACKAGETOPARA;
				return -1;
			}			
			this.m_nResponseFuncNum = nFuncNum;
			lRet = param.lValue(0);
	
			if (lRet != -1) 
			{
				int lRet1;
				lRet1 = NtohLvalueInResult(param.baValue(1));
				if (lRet1 == -1) 
				{
					lRet = -1;
					m_lErrno = GL_PACKAGENTOH;
					goto WAI;
				}
				lRet1 = AddCurPath(
					param.baValue(1),
					//param.lValue(0),
					strMyPath,
					entry.m_nDTLPCharset,
					out baResult);
				if (lRet1 == -1) 
				{
					lRet = -1;
					m_lErrno = GL_NOMEM;
				}
			}
			WAI:
	
				return lRet;
		}


		// *** ԭʼDTLP API ��Search��û�����κΰ�װ
		// ����汾������byte[]���͵�next�ַ���
		public int API_Search(string strPath,
			byte[] baNext,
			int lStyle,
			out byte [] baResult)
		{
			// send:long Channel
			//      buff lpszPath
			//      long lPathLen
			//      long lResultMaxLen
			//      long lStyle
			// recv:buff lpResult
			// return long

			baResult = null;

			DTLPParam param = new DTLPParam();
			int    lRet;
			int     nRet;

			string strMyPath = null;
			string strOtherPath = null;

			HostEntry entry = null;
			
			int nLen;
			int nErrorNo;
	
	
			byte [] baSendBuffer = null;
			byte [] baRecvBuffer = null;

			// int lResultMaxLen = 60000;
	
			SplitPath(strPath, out strMyPath, out strOtherPath);
	
			if (strMyPath == "") 
			{
				return this.LocalDir(GetLocalCharset(), out baResult);
			}
	
	
			entry = this.PrepareChannel(strMyPath);
			if (entry == null) 
			{ 
				return -1;
			}
	
	
			if (strOtherPath == "" 
				&& 
				(baNext == null || baNext.Length == 0) 
				)
			{
				return this.SingleDir(strMyPath,
					entry.m_nDTLPCharset,
					out baResult);
			}
	

			Debug.Assert(entry.m_lChannel!=-1,
				"m_lChannel����ȷ");
	
			param.Clear();
			param.ParaLong(entry.m_lChannel);
			int nPathLen = param.ParaPathString(strOtherPath,
				entry.m_nDTLPCharset,
				baNext);
			param.ParaLong(nPathLen);

			param.ParaLong(m_nResultMaxLen);
			param.ParaLong(lStyle);
	
			lRet = param.ParaToPackage(FUNC_SEARCH,
				m_lErrno,
				out baSendBuffer);
			if (lRet == -1) 
			{
				m_lErrno = GL_PARATOPACKAGE;
				return -1; // error
			}
	
			nRet = entry.SendTcpPackage(baSendBuffer,
				lRet, 
				out nErrorNo);
			if (nRet<0) 
			{
				this.m_lErrno = GL_SEND;
				//TRACE(_T("Dir() Channel[%08x] CHostEntry::SendTcpPackage() Errno[%08x]\n"),
				//	Channel,
				//	lpChannel->m_lErrno);
				return -1;
			}

			nRet = entry.RecvTcpPackage(out baRecvBuffer,
				out nLen,
				out nErrorNo);
			if (nRet<0) 
			{
				//TRACE(_T("Dir() Channel[%08x] CHostEntry::RecvTcpPackage() Errno[%08x]\n"),
				//	Channel,
				//	lpChannel->m_lErrno);
				this.m_lErrno = nErrorNo;
				return -1;
			}
	
			param.Clear();
			param.DefPara(Param.STYLE_LONG);
			param.DefPara(Param.STYLE_BUFF);
			int nFuncNum = 0;
			try 
			{
				param.PackageToPara(baRecvBuffer,
					ref m_lErrno,
					out nFuncNum);
			}
			catch 
			{
				this.m_lErrno = GL_PACKAGETOPARA;
				return -1;
			}			
			this.m_nResponseFuncNum = nFuncNum;
			lRet = param.lValue(0);
	
			if (lRet != -1) 
			{
				int lRet1;
				lRet1 = NtohLvalueInResult(param.baValue(1));
				if (lRet1 == -1) 
				{
					lRet = -1;
					m_lErrno = GL_PACKAGENTOH;
					goto WAI;
				}
				lRet1 = AddCurPath(
					param.baValue(1),
					//param.lValue(0),
					strMyPath,
					entry.m_nDTLPCharset,
					out baResult);
				if (lRet1 == -1) 
				{
					lRet = -1;
					m_lErrno = GL_NOMEM;
				}
			}
			WAI:
	
				return lRet;
		}

		public int API_Management(string strPath,
			string strSource1,
//							 long  lSourceMaxLen1,
			string strSource2,
//							 long  lSourceMaxLen2,
			out byte [] baResult)
		{
			// send:long Channel
			//      buff lpszPath
			//      long lPathLen
			//      buff lpSource1
			//      long lSourceMaxLen1
			//      buff lpSource2
			//      long lSourceMaxLen2
			//      long lResultMaxLen
			// recv:buff lpResult
			// return long

			baResult = null;
	
			DTLPParam param = new DTLPParam();
			int    lRet;
			int     nRet;

			string strMyPath = null;
			string strOtherPath = null;

			HostEntry entry = null;

			int nLen;
			int nErrorNo;
	
			byte [] baSendBuffer = null;
			byte [] baRecvBuffer = null;
	
	
			SplitPath(strPath, out strMyPath, out strOtherPath);
	
			if (strMyPath == "") 
			{
				// ָ������ԭ��
				return -1;
			}
	
			entry = this.PrepareChannel(strMyPath);
			if (entry == null) 
			{ 
				return -1;
			}
	
			Debug.Assert(entry.m_lChannel!=-1,
				"m_lChannel����ȷ");

			param.Clear();
			param.ParaLong(entry.m_lChannel);

			int nPathLen = param.ParaString(strOtherPath, entry.m_nDTLPCharset);
			param.ParaLong(nPathLen);

			int nSource1Len = param.ParaString(strSource1, entry.m_nDTLPCharset);
			param.ParaLong(nSource1Len);

			int nSource2Len = param.ParaString(strSource2, entry.m_nDTLPCharset);
			param.ParaLong(nSource2Len);

			param.ParaLong(m_nResultMaxLen);
	
			lRet = param.ParaToPackage(FUNC_ACCMANAGEMENT,
				m_lErrno,
				out baSendBuffer);
			if (lRet == -1) 
			{
				m_lErrno = GL_PARATOPACKAGE;
				return -1; // error
			}
	
			nRet = entry.SendTcpPackage(baSendBuffer,
				lRet, 
				out nErrorNo);
			if (nRet<0) 
			{
				this.m_lErrno = GL_SEND;
				return -1;
			}

			nRet = entry.RecvTcpPackage(out baRecvBuffer,
				out nLen,
				out nErrorNo);
			if (nRet<0) 
			{
				this.m_lErrno = nErrorNo;
				return -1;
			}
	
			param.Clear();
			param.DefPara(Param.STYLE_LONG);
			param.DefPara(Param.STYLE_BUFF);
			int nFuncNum = 0;
			try 
			{
				param.PackageToPara(baRecvBuffer,
					ref m_lErrno,
					out nFuncNum);
			}
			catch 
			{
				this.m_lErrno = GL_PACKAGETOPARA;
				return -1;
			}			
			this.m_nResponseFuncNum = nFuncNum;
			lRet = param.lValue(0);
	
			if (lRet != -1) 
			{
				int lRet1;
				lRet1 = NtohLvalueInResult(param.baValue(1));
				if (lRet1 == -1) 
				{
					lRet = -1;
					m_lErrno = GL_PACKAGENTOH;
					goto WAI;
				}
				lRet1 = AddCurPath(
					param.baValue(1),
					//param.lValue(0),
					strMyPath,
					entry.m_nDTLPCharset,
					out baResult);
				if (lRet1 == -1) 
				{
					lRet = -1;
					m_lErrno = GL_NOMEM;
				}
			}
			WAI:
				return lRet;
		}

		public int API_Write(string strPath,
			byte [] baBuffer,
			out byte[] baResult,
			int lStyle)
		{
			// send:long Channel
			//      buff lpszPath
			//      long lPathLen
			//      buff lpBuffer
			//      long lBufferLen
			//      long lResultMaxLen
			//      long lStyle
			// recv:buff lpResult
			// return long

			baResult = null;

	
			DTLPParam param = new DTLPParam();
			int    lRet;
			int     nRet;

			string strMyPath = null;
			string strOtherPath = null;

			HostEntry entry = null;

			int nLen;
			int nErrorNo;
	
			byte [] baSendBuffer = null;
			byte [] baRecvBuffer = null;
	
	
			SplitPath(strPath, out strMyPath, out strOtherPath);
	
			if (strMyPath == "") 
			{
				// ָ������ԭ��
				this.m_lErrno = GL_ACCESSDENY;
				return -1;
			}

	
			entry = this.PrepareChannel(strMyPath);
			if (entry == null) 
			{ 
				return -1;
			}

	
			if (strOtherPath == "") 
			{
				this.m_lErrno = GL_ACCESSDENY;
				return -1;
			}
	
			Debug.Assert(entry.m_lChannel!=-1,
				"m_lChannel����ȷ");

			param.Clear();
			param.ParaLong(entry.m_lChannel);

			int nPathLen = param.ParaString(strOtherPath, entry.m_nDTLPCharset);
			param.ParaLong(nPathLen);

			param.ParaBuff(baBuffer, baBuffer.Length);
			param.ParaLong(baBuffer.Length);

			param.ParaLong(m_nResultMaxLen);

			param.ParaLong(lStyle);

			lRet = param.ParaToPackage(FUNC_WRITE,
				m_lErrno,
				out baSendBuffer);
			if (lRet == -1) 
			{
				m_lErrno = GL_PARATOPACKAGE;
				return -1; // error
			}
	
	
			nRet = entry.SendTcpPackage(baSendBuffer,
				lRet, 
				out nErrorNo);
			if (nRet<0) 
			{
				this.m_lErrno = GL_SEND;
				return -1;
			}

			nRet = entry.RecvTcpPackage(out baRecvBuffer,
				out nLen,
				out nErrorNo);
			if (nRet<0) 
			{
				this.m_lErrno = nErrorNo;
				return -1;
			}
	
			param.Clear();
			param.DefPara(Param.STYLE_LONG);
			param.DefPara(Param.STYLE_BUFF);
			int nFuncNum = 0;
			try 
			{
				param.PackageToPara(baRecvBuffer,
					ref m_lErrno,
					out nFuncNum);
			}
			catch 
			{
				this.m_lErrno = GL_PACKAGETOPARA;
				return -1;
			}			
			this.m_nResponseFuncNum = nFuncNum;
			lRet = param.lValue(0);
	
			if (lRet != -1) 
			{
				int lRet1;
				lRet1 = NtohLvalueInResult(param.baValue(1));
				if (lRet1 == -1) 
				{
					lRet = -1;
					m_lErrno = GL_PACKAGENTOH;
					goto WAI;
				}
				lRet1 = AddCurPath(
					param.baValue(1),
					//param.lValue(0),
					strMyPath,
					entry.m_nDTLPCharset,
					out baResult);
				if (lRet1 == -1) 
				{
					lRet = -1;
					m_lErrno = GL_NOMEM;
				}
			}
			WAI:
	
				return lRet;
		}

        public int DeleteMarcRecord(string strPath,
            byte[] baTimestamp,
            out string strError)
        {
            int nRet = 0;
            strError = "";
            int nStyle = DELETE_WRITE;

            if (baTimestamp == null)
            {
                strError = "baTimeStamp��������Ϊnull";
                return -1;
            }

            if (baTimestamp.Length < 9)
            {
                strError = "baTimeStamp���ݵĳ��Ȳ���С��9 bytes";
                return -1;
            }


            byte[] baResult = null;

        REDO:

            nRet = API_Write(strPath,
                baTimestamp,
                out baResult,
                nStyle);

            if (nRet == -1)
            {
                int nErrorCode = this.GetLastErrno();
                if (nErrorCode == DtlpChannel.GL_NOTLOGIN)
                {
                    if (this.Container.HasAskAccountInfoEventHandler == false)
                        return nRet;	// �޷���ȡ�˻���Ϣ�����ֻ�ðѴ����Ͻ�


                    AskDtlpAccountInfoEventArgs e = new AskDtlpAccountInfoEventArgs();
                    e.Channel = this;
                    e.Path = strPath;
                    // ���ȱʡ�ʻ���Ϣ
                    int nProcRet = 0;
                INPUT:
                    this.Container.CallAskAccountInfo(this, e);
                    nProcRet = e.Result;
                    if (nProcRet == 2)
                        goto REDO;
                    if (nProcRet == 1)
                    {
                        byte[] baPackage = null;
                        nRet = this.API_ChDir(e.UserName,   // strUserName, 
                            e.Password, // strPassword,
                            strPath,
                            out baPackage);
                        if (nRet > 0)
                            goto REDO;
                        if (this.Container.GUI == true)
                        {
                            this.ErrorBox(e.Owner,
                                "Search()",
                                "Login fail ...");
                        goto INPUT;
                        }
                    }
                    return nRet;	// �����Ͻ�
                } // end if not login

                if (nErrorCode == GL_ERRSIGNATURE)
                {
                    // ʱ�����ƥ��
                }

                return nRet;
            }

            return nRet;
        }

        // 
		public int WriteMarcRecord(string strPath,
			int nStyle,
			string strRecord,
			byte[] baTimeStamp,
			out string strError)
		{
			int nRet = 0;
			strError = "";

			if (baTimeStamp == null) 
			{
				strError = "baTimeStamp��������Ϊnull";
				return -1;
			}

			if (baTimeStamp.Length < 9) 
			{
				strError = "baTimeStamp���ݵĳ��Ȳ���С��9 bytes";
				return -1;
			}


			// ����Ҫд�������
			Encoding encoding = this.GetPathEncoding(strPath);

            CanonicalizeMARC(ref strRecord);

			byte[] baMARC = encoding.GetBytes(strRecord);

			byte[] baBuffer = null;

			baBuffer = ByteArray.EnsureSize(baBuffer, baMARC.Length + 9);
			Array.Copy(baTimeStamp,0, baBuffer, 0, 9);
			Array.Copy(baMARC, 0, baBuffer, 9, baMARC.Length);

			byte[] baResult = null;

			REDO:

			nRet = API_Write(strPath,
				baBuffer,
				out baResult,
				nStyle);

			if (nRet == -1) 
			{
				int nErrorCode = this.GetLastErrno();
				if (nErrorCode == DtlpChannel.GL_NOTLOGIN) 
				{
					if (this.Container.HasAskAccountInfoEventHandler == false)
						return nRet;	// �޷���ȡ�˻���Ϣ�����ֻ�ðѴ����Ͻ�


                    AskDtlpAccountInfoEventArgs e = new AskDtlpAccountInfoEventArgs();
                    e.Channel = this;
                    e.Path = strPath;
					//string strUserName, strPassword;
					//IWin32Window owner = null;
					// ���ȱʡ�ʻ���Ϣ
					int nProcRet = 0;
                INPUT:
                    // return:
                    //		2	already login succeed
                    //		1	dialog return OK
                    //		0	dialog return Cancel
                    //		-1	other error
                    /*
					nProcRet = this.Container.procAskAccountInfo(
						this, strPath, 
						out owner,
						out strUserName,
						out strPassword);
                     * */
                    this.Container.CallAskAccountInfo(this, e);
                    nProcRet = e.Result;
					if (nProcRet == 2)
						goto REDO;
					if (nProcRet == 1) 
					{
						byte[] baPackage = null;
						nRet = this.API_ChDir(e.UserName,   // strUserName, 
							e.Password, // strPassword,
							strPath,
							out baPackage);
						if (nRet > 0)
							goto REDO;
                        if (this.Container.GUI == true)
                        {
                            this.ErrorBox(e.Owner,
                                "Search()",
                                "Login fail ...");
						goto INPUT;
                        }
					}
					return nRet;	// �����Ͻ�
				} // end if not login

				if (nErrorCode == GL_ERRSIGNATURE)
				{
					// ʱ�����ƥ��
				}

				return nRet;
			}

			return nRet;
		}

        // ��MARC��¼���滯���Ա㱣�浽DTLP������
        // ����¼ĩβ�Ƿ���30 29�����������û�У�������
        public static void CanonicalizeMARC(ref string strMARC)
        {
            if (strMARC.Length == 0)
            {
                strMARC = "012345678901234567890123001---" + new string(MarcUtil.FLDEND, 1) + new string(MarcUtil.RECEND, 1);
                return;
            }

            int nTail = strMARC.Length - 1;
            if (strMARC[nTail] != MarcUtil.RECEND)
            {
                if (strMARC[nTail] == MarcUtil.FLDEND)
                {
                    strMARC = strMARC + new string(MarcUtil.RECEND, 1);
                    return;
                }
                else
                {
                    strMARC = strMARC + new string(MarcUtil.FLDEND, 1) + new string(MarcUtil.RECEND, 1);
                    return;
                }
            }

            return;
        }

        // ��һ�汾
        public int WriteMarcRecord(string strPath,
            int nStyle,
            string strRecord,
            byte[] baTimeStamp,
            out string strOutputRecord,
            out string strOutputPath,
            out byte [] baOutputTimestamp,
            out string strError)
        {
            int nRet = 0;
            strError = "";
            strOutputRecord = "";
            strOutputPath = "";
            baOutputTimestamp = null;

            if (baTimeStamp == null)
            {
                strError = "baTimeStamp��������Ϊnull";
                return -1;
            }

            if (baTimeStamp.Length < 9)
            {
                strError = "baTimeStamp���ݵĳ��Ȳ���С��9 bytes";
                return -1;
            }


            // ����Ҫд�������
            Encoding encoding = this.GetPathEncoding(strPath);

            CanonicalizeMARC(ref strRecord);

            byte[] baMARC = encoding.GetBytes(strRecord);

            byte[] baBuffer = null;

            baBuffer = ByteArray.EnsureSize(baBuffer, baMARC.Length + 9);
            Array.Copy(baTimeStamp, 0, baBuffer, 0, 9);
            Array.Copy(baMARC, 0, baBuffer, 9, baMARC.Length);

            byte[] baResult = null;

        REDO:

            nRet = API_Write(strPath,
                baBuffer,
                out baResult,
                nStyle);

            if (nRet == -1)
            {
                int nErrorCode = this.GetLastErrno();
                if (nErrorCode == DtlpChannel.GL_NOTLOGIN)
                {
                    if (this.Container.HasAskAccountInfoEventHandler == false)
                        return nRet;	// �޷���ȡ�˻���Ϣ�����ֻ�ðѴ����Ͻ�


                    AskDtlpAccountInfoEventArgs e = new AskDtlpAccountInfoEventArgs();
                    e.Channel = this;
                    e.Path = strPath;
                    //string strUserName, strPassword;
                    //IWin32Window owner = null;
                    // ���ȱʡ�ʻ���Ϣ
                    int nProcRet = 0;
                INPUT:
                    // return:
                    //		2	already login succeed
                    //		1	dialog return OK
                    //		0	dialog return Cancel
                    //		-1	other error
                    /*
                    nProcRet = this.Container.procAskAccountInfo(
                        this, strPath,
                        out owner,
                        out strUserName,
                        out strPassword);
                     * */
                    this.Container.CallAskAccountInfo(this, e);
                    nProcRet = e.Result;
                    if (nProcRet == 2)
                        goto REDO;
                    if (nProcRet == 1)
                    {
                        byte[] baPackage = null;
                        nRet = this.API_ChDir(e.UserName,   //strUserName,
                            e.Password, // strPassword,
                            strPath,
                            out baPackage);
                        if (nRet > 0)
                            goto REDO;
                        if (this.Container.GUI == true)
                        {
                            this.ErrorBox(e.Owner,
                                "Search()",
                                "Login fail ...");
                        goto INPUT;
                        }
                    }
                    return nRet;	// �����Ͻ�
                } // end if not login

                if (nErrorCode == GL_ERRSIGNATURE)
                {
                    // ʱ�����ƥ��
                }

                // 
                strError = "API_Write()����:\r\n"
                    + "·��: " + strPath + "\r\n"
                    + "������: " + nErrorCode + "\r\n"
                    + "������Ϣ: " + GetErrorString(nErrorCode) + "\r\n";

                return nRet;
            }

            int nResult = nRet;

            // �������������صļ�¼
            Package package = new Package();
            package.LoadPackage(baResult,
                encoding);
            nRet = package.Parse(PackageFormat.Binary);
            if (nRet == -1)
            {
                strError = "Package::Parse() error";
                goto ERROR1;
            }

            byte[] content = null;
            nRet = package.GetFirstBin(out content);
            if (nRet == -1)
            {
                strError = "Package::GetFirstBin() error";
                goto ERROR1;
            }

            if (content == null
                || content.Length < 9)
            {
                strError = "content length < 9";
                goto ERROR1;
            }

            baOutputTimestamp = new byte[9];
            Array.Copy(content, baOutputTimestamp, 9);

            byte[] marc = new byte[content.Length - 9];
            Array.Copy(content,
                9,
                marc,
                0,
                content.Length - 9);

            strOutputRecord = encoding.GetString(marc);

            strOutputPath = package.GetFirstPath();

            return nResult;
        ERROR1:
            return -1;
        }

        // ���һ����¼�ļ�����
        public int GetAccessPoint(string strPath,
            string strRecord,
            out List<string> results,
            out string strError)
        {
            int nRet = 0;
            strError = "";
            results = new List<string>();

            int nStyle = GETKEYS_WRITE;
            byte[] baTimeStamp = new byte[9];

            // ����Ҫд�������
            Encoding encoding = this.GetPathEncoding(strPath);

            CanonicalizeMARC(ref strRecord);

            byte[] baMARC = encoding.GetBytes(strRecord);

            byte[] baBuffer = null;

            baBuffer = ByteArray.EnsureSize(baBuffer, baMARC.Length + 9);
            Array.Copy(baTimeStamp, 0, baBuffer, 0, 9);
            Array.Copy(baMARC, 0, baBuffer, 9, baMARC.Length);

            byte[] baResult = null;

        REDO:
            nRet = API_Write(strPath,
                baBuffer,
                out baResult,
                nStyle);
            if (nRet == -1)
            {
                int nErrorCode = this.GetLastErrno();
                if (nErrorCode == DtlpChannel.GL_NOTLOGIN)
                {
                    if (this.Container.HasAskAccountInfoEventHandler == false)
                        return nRet;	// �޷���ȡ�˻���Ϣ�����ֻ�ðѴ����Ͻ�


                    AskDtlpAccountInfoEventArgs e = new AskDtlpAccountInfoEventArgs();
                    e.Channel = this;
                    e.Path = strPath;
                    //string strUserName, strPassword;
                    //IWin32Window owner = null;
                    // ���ȱʡ�ʻ���Ϣ
                    int nProcRet = 0;
                INPUT:
                    // return:
                    //		2	already login succeed
                    //		1	dialog return OK
                    //		0	dialog return Cancel
                    //		-1	other error
                    /*
                    nProcRet = this.Container.procAskAccountInfo(
                        this, strPath,
                        out owner,
                        out strUserName,
                        out strPassword);
                     * */
                    this.Container.CallAskAccountInfo(this, e);
                    nProcRet = e.Result;
                    if (nProcRet == 2)
                        goto REDO;
                    if (nProcRet == 1)
                    {
                        byte[] baPackage = null;
                        nRet = this.API_ChDir(e.UserName,   //strUserName,
                            e.Password, // strPassword,
                            strPath,
                            out baPackage);
                        if (nRet > 0)
                            goto REDO;
                        if (this.Container.GUI == true)
                        {
                            this.ErrorBox(e.Owner,
                                "Search()",
                                "Login fail ...");
                            goto INPUT;
                        }
                    }
                    return nRet;	// �����Ͻ�
                } // end if not login

                if (nErrorCode == GL_ERRSIGNATURE)
                {
                    // ʱ�����ƥ��
                }

                // 
                strError = "API_Write:\r\n"
                    + "·��: " + strPath + "\r\n"
                    + "������: " + nErrorCode + "\r\n"
                    + "������Ϣ: " + GetErrorString(nErrorCode) + "\r\n";
                return nRet;
            }

            int nResult = nRet;

            // �������������صļ�¼
            Package package = new Package();
            package.LoadPackage(baResult,
                encoding);
            nRet = package.Parse(PackageFormat.String);
            if (nRet == -1)
            {
                strError = "Package::Parse() error";
                goto ERROR1;
            }

            for (int i = 0; i < package.Count; i++)
            {
                Cell cell = (Cell)package[i];
                results.Add(cell.Content);
            }

            return nResult;
        ERROR1:
            return -1;
        }

		// ����һ��MARC��¼
		// return:
		//		-1	error
		//		0	not found
		//		1	found
		public int GetRecord(
			string strPath,
			int nStyle,
			out string strRecPath,
			out string strRecord,
			out byte[] baTimeStamp,	// ����9�ַ��ռ�
			out string strError)
		{
			strRecPath = "";
			strRecord = "";
			baTimeStamp = null;
			strError = "";

			byte[] baPackage;
			byte[] baMARC;
	
			int nRet = this.Search(strPath,
				DtlpChannel.XX_STYLE | nStyle,
				out baPackage);

			if (nRet == -1L) 
			{
				int nErrorCode = this.GetLastErrno();

				if (nErrorCode == DtlpChannel.GL_NOTEXIST) 
					return 0;	// not found

				string strText = this.GetErrorString(nErrorCode);

				string strHex = String.Format("0X{0,8:X}",nErrorCode);

				strError = "������"
					+ strHex + ", ԭ��: "
					+ strText;
				goto ERROR1;
			}

			Package package = new Package();

			package.LoadPackage(baPackage, this.GetPathEncoding(strPath));
			nRet = package.Parse(PackageFormat.Binary);
			if (nRet == -1) 
			{
				Debug.Assert(false, "Package::Parse() error");
				strError = "Package::Parse() error";
				goto ERROR1;
			}

			nRet = package.GetFirstBin(out baMARC);
			if (nRet == -1 || nRet == 0) 
			{
				Debug.Assert(false, "Package::GetFirstBin() error");
				strError = "Package::GetFirstBin() error";
				goto ERROR1;
			}

			if (baMARC.Length >= 9) 
			{
				baTimeStamp = new byte[9];
				Array.Copy(baMARC, 0, baTimeStamp, 0, 9);

				byte [] baBody = new byte[baMARC.Length - 9];
				Array.Copy(baMARC, 9, baBody, 0, baMARC.Length - 9);
				baMARC = baBody;
			}
			else 
			{
				// ��¼�����⣬����һ���ռ�¼?
			}

			strRecord = GetPathEncoding(strPath).GetString(baMARC);	// ?????

			strRecPath = package.GetFirstPath();

            return 1;	// found
        ERROR1:
            return -1;
        }

        // ���浽(��������)�����ļ�
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int WriteCfgFile(string strCfgFilePath,
            string strContent,
            out string strError)
        {
            strError = "";

            /*
            if (baTimeStamp == null)
            {
                strError = "baTimeStamp��������Ϊnull";
                return -1;
            }

            if (baTimeStamp.Length < 9)
            {
                strError = "baTimeStamp���ݵĳ��Ȳ���С��9 bytes";
                return -1;
            }*/

            // ����Ҫд�������
            Encoding encoding = this.GetPathEncoding(strCfgFilePath);

            strContent = strContent.Replace("\r\n", "\r");

            byte[] baContent = encoding.GetBytes(strContent);

            for (int i = 0; i < baContent.Length; i++)
            {
                if (baContent[i] == (char)'\r')
                    baContent[i] = 0;
            }

            byte[] baResult = null;

            int nStyle = 0;

        REDO:

            int nRet = API_Write(strCfgFilePath,
                baContent,
                out baResult,
                nStyle);

            if (nRet == -1)
            {
                int nErrorCode = this.GetLastErrno();
                if (nErrorCode == DtlpChannel.GL_NOTLOGIN)
                {
                    if (this.Container.HasAskAccountInfoEventHandler == false)
                        return nRet;	// �޷���ȡ�˻���Ϣ�����ֻ�ðѴ����Ͻ�


                    AskDtlpAccountInfoEventArgs e = new AskDtlpAccountInfoEventArgs();
                    e.Channel = this;
                    e.Path = strCfgFilePath;
                    //string strUserName, strPassword;
                    //IWin32Window owner = null;
                    // ���ȱʡ�ʻ���Ϣ
                    int nProcRet = 0;
                INPUT:
                    this.Container.CallAskAccountInfo(this, e);
                    nProcRet = e.Result;
                    if (nProcRet == 2)
                        goto REDO;
                    if (nProcRet == 1)
                    {
                        byte[] baPackage = null;
                        nRet = this.API_ChDir(e.UserName,   // strUserName, 
                            e.Password, // strPassword,
                            strCfgFilePath,
                            out baPackage);
                        if (nRet > 0)
                            goto REDO;
                        if (this.Container.GUI == true)
                        {
                            this.ErrorBox(e.Owner,
                                "Search()",
                                "Login fail ...");
                            goto INPUT;
                        }
                    }
                    return nRet;	// �����Ͻ�
                } // end if not login

                if (nErrorCode == GL_ERRSIGNATURE)
                {
                    // ʱ�����ƥ��
                }

                string strText = this.GetErrorString(nErrorCode);
                string strHex = String.Format("0X{0,8:X}", nErrorCode);

                strError = "���������ļ� '" + strCfgFilePath + " ' ʱ��������: "
                    + "������"
                    + strHex + ", ԭ��: "
                    + strText;
                return nRet;
            }

            return nRet;
        }

        public string GetErrorString()
        {
            int nErrorCode = this.GetLastErrno();

            string strText = this.GetErrorString(nErrorCode);
            string strHex = String.Format("0X{0,8:X}", nErrorCode);

            return "������ "
                + strHex + ", ԭ��: "
                + strText;
        }

        // ���(��������)�����ļ�����
        // return:
        //      -1  ����
        //      0   �ļ�������
        //      1   �ɹ�
        public int GetCfgFile(string strCfgFilePath,
            out string strContent,
            out string strError)
        {
            strError = "";
            strContent = "";
            int nRet;
            byte[] baPackage = null;

            bool bFirst = true;

            byte[] baNext = null;
            int nStyle = DtlpChannel.XX_STYLE;

            byte[] baContent = null;

            Encoding encoding = this.GetPathEncoding(strCfgFilePath);


            for (; ; )
            {
                if (bFirst == true)
                {
                    nRet = this.Search(strCfgFilePath,
                        DtlpChannel.XX_STYLE,
                        out baPackage);
                }
                else
                {
                    nRet = this.Search(strCfgFilePath,
                        baNext,
                        DtlpChannel.XX_STYLE,
                        out baPackage);
                }

                if (nRet == -1)
                {
                    int nErrorCode = this.GetLastErrno();

                    if (nErrorCode == DtlpChannel.GL_NOTEXIST)
                        return 0;	// not found

                    string strText = this.GetErrorString(nErrorCode);

                    string strHex = String.Format("0X{0,8:X}", nErrorCode);

                    strError = "��ȡ�����ļ� '" + strCfgFilePath + " ' ʱ��������: "
                        +"������"
                        + strHex + ", ԭ��: "
                        + strText;
                    goto ERROR1;
                }

                Package package = new Package();
                package.LoadPackage(baPackage, encoding);
                package.Parse(PackageFormat.Binary);

                bFirst = false;

                byte[] baPart = null;
                package.GetFirstBin(out baPart);
                if (baContent == null)
                    baContent = baPart;
                else
                    baContent = ByteArray.Add(baContent, baPart);

                if (package.ContinueString != "")
                {
                    nStyle |= DtlpChannel.CONT_RECORD;
                    baNext = package.ContinueBytes;
                }
                else
                {
                    break;
                }
            }

            if (baContent != null)
            {
                for (int i = 0; i < baContent.Length; i++)
                {
                    if (baContent[i] == 0)
                        baContent[i] = (byte)'\r';
                }
                strContent = encoding.GetString(baContent).Replace("\r", "\r\n");
            }

            return 1;
        ERROR1:
            return -1;
        }

		public static void SetInt32Value(Int32 v,
			byte [] baBuffer,
			int offs)
		{
			byte [] va = BitConverter.GetBytes(v);

			for(int i =0; i<va.Length; i++)
			{
				baBuffer.SetValue(va[i], offs + i);
			}
		}


		// ��ͨѶ��������network byteorder������ת��Ϊlocal byteorder
		public static int NtohLvalueInResult(byte [] baBuffer)
		{
			if (baBuffer == null)
				return -1;
			//  long packageLength;
			Int32 paraLength, pathLength;
			Int32 dataLength, maskLength, Mask;
	
			//  long rtnValue, myErrno;
			Int32 offs/*, len, Temp*/;
			Int32 lLength = 4;
	
			offs = 0;

			/* result package */
			paraLength = BitConverter.ToInt32(baBuffer, offs);
			paraLength = IPAddress.NetworkToHostOrder((Int32)paraLength);
			if(paraLength < lLength)
				return -1;
	
			SetInt32Value(paraLength, baBuffer, offs);

			if (paraLength == lLength)
				return 0;
			offs += lLength;
	
			while(true)
			{
				/* 1. pathlen + path */
				pathLength = BitConverter.ToInt32(baBuffer, offs);
				pathLength = IPAddress.NetworkToHostOrder((Int32)pathLength);
				if(pathLength + offs > paraLength) 
					break;
		
				SetInt32Value(pathLength, baBuffer, offs);
				offs += pathLength;
				if(offs >= paraLength) 
					break;
		
				/* 2. datalen + masklen + mask + data */
				dataLength = BitConverter.ToInt32(baBuffer, offs);
				dataLength = IPAddress.NetworkToHostOrder((Int32)dataLength);
				if(dataLength + offs > paraLength) 
				{ 
					//fnprintf(_T("tcps.log"), _T("\r\n*** NtoH Package Error"));
					return -1;  // jia
				}
		
				SetInt32Value(dataLength, baBuffer, offs);
	
				maskLength = BitConverter.ToInt32(baBuffer, offs + lLength);
				maskLength = IPAddress.NetworkToHostOrder((Int32)maskLength);
				SetInt32Value(maskLength, baBuffer, offs + lLength);

				Mask = BitConverter.ToInt32(baBuffer, offs + 2 * lLength);
				Mask = IPAddress.NetworkToHostOrder((Int32)Mask);
				SetInt32Value(Mask, baBuffer, offs + 2 * lLength);
	
				offs += dataLength;
				if(offs >= paraLength) 
					break;

			} /* each path */
	
			return 0;
		}

		// �ƺ������õ�������?

		// �����ذ�lpPackage�е�����·������lpCurPath����һ����
		// ���������lpResult�С�
		static int AddCurPath(
			byte [] baPackage,
			// int	lPackageLen,
			string strCurPath,
			int nCharset,
			out byte [] baResult)
		{
			Int32 lPackageLen = 0;
			Int32 lPathLen;
			Int32 lBaoLen;
			int nCurPathLen;
			int s,t,i;

			baResult = null;
	
			if (baPackage == null)
				return -1;
	
			if (baPackage.Length == 0)
				return 0;
	
			lPackageLen = BitConverter.ToInt32(baPackage, 0);
			if (lPackageLen <= 0)
				return -1; 

			byte[] curPathBuffer = DTLPParam.GetEncoding(nCharset).GetBytes(strCurPath);

	
			nCurPathLen = curPathBuffer.Length;
			s = 4;
			t = 4;
	
			for(i=0; i<10; i++) // < 10?
			{ 
				// pathlen
				lPathLen = BitConverter.ToInt32(baPackage, s);	// *((long *)(lpPackage + s));
		
				if (lPathLen < 4) 
				{
					return -1;	// ԭʼ����ʽ����
				}

				s += 4;
				//*((long *)(lpResult+t)) = lPathLen +(long)nCurPathLen+1L;  // gai
				baResult = ByteArray.EnsureSize(baResult, t + 4);
				SetInt32Value(lPathLen + nCurPathLen + 1,
					baResult, t);

				t += 4;
		
				// path body

				//memcpy(lpResult + t,
				//	lpCurPath,
				//	nCurPathLen);
				baResult = ByteArray.EnsureSize( baResult, t + nCurPathLen);
				Array.Copy(curPathBuffer,
					0,
					baResult,
					t,
					nCurPathLen);

				t += nCurPathLen;
				//*(lpResult+t)='/';
				baResult = ByteArray.EnsureSize( baResult, t + 1);
				baResult.SetValue((byte)'/', t);
				t++;
		
				//memmove(lpResult + t,lpPackage + s,(int)lPathLen-4);
				baResult = ByteArray.EnsureSize( baResult, t + lPathLen-4);
				Array.Copy(baPackage,
					s,
					baResult,
					t,
					lPathLen-4);

                string strOldPath = Encoding.UTF8.GetString(baResult, t, lPathLen - 4);


				s +=(int)lPathLen-4;
				t +=(int)lPathLen-4;
		
		
				if (s >= (int)lPackageLen)
					break;
		
				// bao length
		
				//lBaoLen = *((long *)(lpPackage + s));
				lBaoLen = BitConverter.ToInt32(baPackage, s);
				s += 4;

				//*((long *)(lpResult+t)) = lBaoLen;
				baResult = ByteArray.EnsureSize( baResult, t + 4);	//
				SetInt32Value(lBaoLen,
					baResult, 
					t);

				t += 4;

				// bao body

				// memmove(lpResult + t,lpPackage + s,(int)lBaoLen-4);
				baResult = ByteArray.EnsureSize( baResult, t + lBaoLen - 4);
				Array.Copy(baPackage,
					s,
					baResult,
					t,
					lBaoLen-4);

				s +=(int)lBaoLen-4;
				t +=(int)lBaoLen-4;
				if (s >= (int)lPackageLen)
					break;
				//if (t >= (int)lResultMaxLen)
				//	break;
			}

			SetInt32Value((Int32)t,
				baResult, 
				0);

			// *((long *)(lpResult)) = (long)t;
			return t;
		}


		// ��·���и�Ϊǰ���������֡�
		public static void SplitPath(string strPath,
			out string strFirstPart,
			out string strOtherPart)
		{
			//string strTemp;
			int nRet;

			Debug.Assert(strPath != null, 
				"strPath��������Ϊnull");

			nRet = strPath.IndexOf("/",0);
			if (nRet == -1) 
			{
				strFirstPart = strPath;
				strOtherPart = "";
				return;
			}

			strFirstPart = strPath.Substring(0, nRet);
			strOtherPart = strPath.Substring(nRet+1);

		}

	
		// ������Ŀ¼������
		int LocalDir(
			int nCharset,
			out byte [] baResult)
		{
			int lPackageLen;
			int lBaoLen;
			int lPathLen;
			int lMask;
			int lMaskLen;

			// LPSTR lpBuffer;
			int cur=0;
	
			baResult = new byte [4096];
	
			if (baResult == null) 
			{
				m_lErrno = GL_OVERFLOW; // �ڴ治��
				return -1;
			}
	
			cur=4;
	
			// Path
			lPathLen = 5;
			Array.Copy(BitConverter.GetBytes((Int32)lPathLen), 
				0,
				baResult,
				cur,
				4);

			baResult = ByteArray.EnsureSize( baResult, cur + 4);

			cur += 4;

			// memmove(lpResult+cur,(char far *)"",1);
			baResult.SetValue((byte)0, cur);
	
			// Bao
			baResult = ByteArray.EnsureSize( baResult, cur + 1);

			cur+=1;

			byte [] baBuffer = null;

			lBaoLen = PackDirEntry(nCharset,
				out baBuffer);


			lBaoLen += 12;
			Array.Copy(BitConverter.GetBytes((Int32)lBaoLen), 
				0,
				baResult,
				cur,
				4);

			baResult = ByteArray.EnsureSize( baResult, cur + 4);

			cur += 4;

			lMaskLen = 8;

			Array.Copy(BitConverter.GetBytes((Int32)lMaskLen), 
				0,
				baResult,
				cur,
				4);

			baResult = ByteArray.EnsureSize( baResult, cur + 4);

			cur += 4;

			lMask = TypeKernel | AttrExtend;  // mask

			Array.Copy(BitConverter.GetBytes((Int32)lMask), 
				0,
				baResult,
				cur,
				4);

	
			baResult = ByteArray.EnsureSize( baResult, cur + 4);
			cur += 4;

			baResult = ByteArray.EnsureSize( baResult, cur+lBaoLen-12);

			if (baBuffer != null) 
			{
				Array.Copy(baBuffer, 
					0,
					baResult,
					cur,
					lBaoLen-12);
			}

			cur += (lBaoLen-12);
	
			// Package Len

			lPackageLen = (Int32)cur;

			Array.Copy(BitConverter.GetBytes((Int32)lPackageLen), 
				0,
				baResult,
				0,
				4);

			return lPackageLen;
		}

		int SingleDir(string strCurDir,
			int nCharset,
			out byte [] baResult)
		{
			int nCurDirLen;
			int t;

			baResult = null;

			byte[] buffer = DTLPParam.GetEncoding(nCharset).GetBytes(strCurDir);
			// Ϊ�ַ���ĩβ����һ��0�ַ�
			buffer = ByteArray.EnsureSize(buffer, buffer.Length + 1);
			buffer[buffer.Length -1] = 0;

			nCurDirLen = buffer.Length;
	
			t = 4;
			baResult = ByteArray.EnsureSize(baResult, t + 4);
			SetInt32Value(nCurDirLen+4,
				baResult, t);
			// *((long *)(lpResult + t))=(long)(nCurDirLen+1+4);
			t += 4;

			baResult = ByteArray.EnsureSize(baResult, t + nCurDirLen);
			Array.Copy(buffer, 0, baResult, t, nCurDirLen);
			// memmove(lpResult + t,(LPSTR)lpCurDir,nCurDirLen+1);
	
			t = 0;
			Int32 v = nCurDirLen+4  +4;
			SetInt32Value(v,
				baResult, t);
			// *((long *)(lpResult + t))=(long)(nCurDirLen+1+4  +4);
	
			return v;
		}

		int PackDirEntry(
			int nCharset,
			out byte [] baResult)
		{
			int i;
			// LPSTR pp;
			int cur = 0;
			int len;
			HostEntry entry = null;
			// CAdvString advstrText;

			baResult = null;
	
			for(i=0,cur=0;i<m_HostArray.Count;i++) 
			{
				entry = (HostEntry)m_HostArray[i];
				Debug.Assert(entry != null,
					"HostArray�г����˿յ�Ԫ��");

				if (entry.m_strHostName == "")
					continue;

				// Unicode --> GB
				// �ٶ������������β��0�ַ�
				byte[] buffer = 
					DTLPParam.GetEncoding(nCharset).GetBytes(entry.m_strHostName);
				// GB-2312

				/*
				byte[] buffer = Encoding.Convert(
					Encoding.Unicode,
					Encoding.GetEncoding(936),	// GB-2312
					entry.m_strHostName.ToCharArray());
				*/

		
				len = buffer.Length;
				if (len==0)
					continue;
		
				baResult = ByteArray.EnsureSize( baResult, cur+len+1);
				Array.Copy(buffer, 
					0,
					baResult,
					cur,
					len);

				// ��β0
				baResult.SetValue((byte)0, baResult.Length - 1);
				cur += len+1;
			}
	
			return cur;
		}


		// ����һ���յķ��ذ�
		static int NullPackage(out byte [] baResult)
		{
			int t = 0;
			Int32 l;

			baResult = null;

			t += 4;

			l = 4 + 1;   // path len

			baResult = ByteArray.EnsureSize( baResult, t + 4);
			Array.Copy(BitConverter.GetBytes((Int32)l), 
				0,
				baResult,
				t,
				4);
			t += 4;

			// path

			baResult = ByteArray.EnsureSize( baResult, t + 1);
			baResult.SetValue((byte)0, t);
			t += 1;


			l = 4 * 3;  // Bao Len

			Array.Copy(BitConverter.GetBytes((Int32)l), 
				0,
				baResult,
				t,
				4);
			t += 4;

			l = 4 * 2;   // Mask Len
			Array.Copy(BitConverter.GetBytes((Int32)l), 
				0,
				baResult,
				t,
				4);
			t += 4;

			l = AttrTcps | AttrExtend;   // Mask
			Array.Copy(BitConverter.GetBytes((Int32)l), 
				0,
				baResult,
				t,
				4);
			t += 4;

			l = (Int32)t;
			Array.Copy(BitConverter.GetBytes((Int32)l), 
				0,
				baResult,
				0,
				4);

			return t;
        }

        #region ��dt1000��־�����йص�ʵ�ú���

        // ����־��¼·������Ϊ���ڡ���š�ƫ��
        // һ����־��¼·��������Ϊ:
        // /ip/log/19991231/0@1234~5678
        // parameters:
        //		strLogPath		����������־��¼·��
        //		strDate			������������
        //		nRecID			�������ļ�¼��
        //		strOffset		�������ļ�¼ƫ�ƣ�����1234~5678
        // return:
        //		-1		����
        //		0		��ȷ
        public static int ParseLogPath(string strLogPath,
            out string strDate,
            out int nRecID,
            out string strOffset,
            out string strError)
        {
            strError = "";
            strDate = "";
            nRecID = -1;
            strOffset = "";

            int nRet = 0;
            string strPath = "";

            strPath = strLogPath;

            nRet = strPath.LastIndexOf('@');
            if (nRet != -1)
            {
                strOffset = strPath.Substring(nRet + 1);
                strPath = strPath.Substring(0, nRet);
            }
            else
                strOffset = "";

            // number
            nRet = strPath.LastIndexOf('/');
            if (nRet != -1)
            {
                string strNumber;
                strNumber = strPath.Substring(nRet + 1);
                try
                {
                    nRecID = Convert.ToInt32(strNumber);
                }
                catch
                {
                    strError = "·�� '" + strLogPath + "' ��'" + strNumber + "'Ӧ��Ϊ������";
                    return -1;
                }
                strPath = strPath.Substring(0, nRet);
            }
            else
            {
                nRecID = 0;
            }

            // date
            nRet = strPath.LastIndexOf('/');
            if (nRet != -1)
            {
                strDate = strPath.Substring(nRet + 1);
                Debug.Assert(strDate.Length == 8, "");
                strPath = strPath.Substring(0, nRet);
            }
            else
            {
                strDate = "";
            }

            return 0;
        }

        // ��dt1000��������ʽ����־��¼��ת��ΪMARC���ڸ�ʽ
        public static string GetDt1000LogRecord(byte[] baContent,
            Encoding encoding)
        {
            string strRecord = encoding.GetString(baContent);
            // int nRet = 0;

            // ����Ȼ�����滻ΪISO2709ר�÷���
            /*
            strRecord = strRecord.Replace("\r\n***\r\n",
                new string(MarcUtil.FLDEND, 1) + new string(MarcUtil.RECEND, 1));
            if (strRecord[strRecord.Length - 1] != MarcUtil.RECEND)
            {
                // ����
                strRecord += new string(MarcUtil.RECEND, 1);
            }*/
            strRecord = strRecord.Replace("\r\n***\r\n",
                new string(MarcUtil.FLDEND, 1));



            strRecord = strRecord.Replace("\r\n", new string(MarcUtil.FLDEND, 1));
            if (strRecord.Length >= 25)
            {
                strRecord = strRecord.Remove(24, 1);	// ɾ��ͷ�������һ��FLDEND
            }

            /*
            // ��������ڶ����ַ�����FLDEND�������һ��
            int nLen;
            nLen = strRecord.Length;
            if (nLen >= 2)
            {
                if (strRecord[nLen - 2] != MarcUtil.FLDEND)
                    strRecord = strRecord.Insert(nLen - 1, new string(MarcUtil.FLDEND, 1));
            }*/

            // ���������һ���ַ�����FLDEND�������һ��
            if (strRecord[strRecord.Length - 1] != MarcUtil.FLDEND)
            {
                strRecord += new string(MarcUtil.FLDEND, 1);
            }

            return strRecord;
        }

        // ����dt1000��־��¼�еĵ�Ҫ������
        /*
#define LOG_APPEND		0	// ��ʾ׷�Ӽ�¼
#define LOG_OVERWRITED	1	// ��ʾ���ǲ���ɾ���ľɼ�¼
#define LOG_DELETE		2	// ��ʾɾ����¼
#define LOG_DESTROY_DB	12	// ��ʾ��ʼ�����ݿ�
 * */
        // parameters:
        //      strOperPath .rz�ֶ�$a���ֶ����ݣ�·�������������strOperCodeΪ��12��ʱ��strOperPath�з��ص�ֻ�����ݿ���
        public static int ParseDt1000LogRecord(string strMARC,
            out string strOperCode,
            out string strOperComment,
            out string strOperPath,
            out string strError)
        {
            strError = "";

            strOperComment = "";
            strOperPath = "";
            strOperCode = "";

            string strField = "";
            string strNextFieldName = "";

            // return:
            //		-1	����
            //		0	��ָ�����ֶ�û���ҵ�
            //		1	�ҵ����ҵ����ֶη�����strField������
            int nRet = MarcUtil.GetField(strMARC,
                ".rz",
                0,
                out strField,
                out strNextFieldName);
            if (nRet == -1)
            {
                strError = "get field '.rz' failed...";
                goto ERROR1;
            }
            if (nRet == 0)
            {
                strError = "field '.rz' not found...";
                goto ERROR1;
            }


            if (strField.Length < 5)
            {
                strError = "'.rz'�ֶγ���С��5...";
                goto ERROR1;
            }

            strOperCode = strField.Substring(3, 2);


            if (strOperCode == "00")
                strOperComment = "׷�Ӽ�¼";
            else if (strOperCode == "01")
                strOperComment = "�����ǵļ�¼";
            else if (strOperCode == "02")
                strOperComment = "ɾ����¼";
            else if (strOperCode == "12")
                strOperComment = "��ʼ�����ݿ�";
            else
            {
                strOperComment = "����ʶ��Ĳ������� '" + strOperCode + "'";
                goto ERROR1;
            }

            string strSubfield = "";
            string strNextSubfieldName = "";

            // ���$a���ֶ�

            // ���ֶλ����ֶ����еõ�һ�����ֶ�
            // parameters:
            //		strText		�ֶ����ݣ��������ֶ������ݡ�
            //		textType	��ʾstrText�а��������ֶ����ݻ��������ݡ���ΪItemType.Field����ʾstrText������Ϊ�ֶΣ���ΪItemType.Group����ʾstrText������Ϊ���ֶ��顣
            //		strSubfieldName	���ֶ���������Ϊ1λ�ַ������==null����ʾ�������ֶ�
            //					��ʽΪ'a'�����ġ�
            //		nIndex			��Ҫ���ͬ�����ֶ��еĵڼ�������0��ʼ���㡣
            //		strSubfield		[out]������ֶΡ����ֶ���(1�ַ�)�����ֶ����ݡ�
            //		strNextSubfieldName	[out]��һ�����ֶε����֣�����һ���ַ�
            // return:
            //		-1	����
            //		0	��ָ�������ֶ�û���ҵ�
            //		1	�ҵ����ҵ������ֶη�����strSubfield������

            nRet = MarcUtil.GetSubfield(strField,
                DigitalPlatform.Marc.ItemType.Field,
                "a",
                0,
                out strSubfield,
                out strNextSubfieldName);
            if (nRet == -1)
            {
                strError = "���.rz�ֶ��е�$a���ֶ�ʱ����";
                goto ERROR1;
            }
            if (nRet == 0)
            {
                strError = ".rz�ֶ��е�$a���ֶ�û���ҵ�";
                goto ERROR1;
            }

            if (strSubfield.Length < 1)
            {
                strError = ".rz�ֶ��е�$a���ֶ�����Ϊ��";
                goto ERROR1;
            }

            string strContent = strSubfield.Substring(1);

            if (strOperCode == "12")
            {
                nRet = strContent.IndexOf("/");

                string strDbName = "";

                // ����
                if (nRet == -1)
                {
                    // ��������Ƿ�Ҫ�������ݲ�ȱ����
                    strDbName = strContent;
                }
                else
                    strDbName = strContent.Substring(0, nRet);

                // '/'��������ݿ��ڲ����ű���������
                strOperPath = strDbName;
            }
            else
            {
                strOperPath = strContent;
            }

            return 0;
        ERROR1:

            return -1;
        }


        #endregion

        // ��������·��
        // return:
        //      -1  ����
        //      0   �ɹ�
        public static int ParseWritePath(string strPathParam,
            out string strServerAddr,
            out string strDbName,
            out string strNumber,
            out string strError)
        {
            strError = "";
            strServerAddr = "";
            strDbName = "";
            strNumber = "";

            string strPath = strPathParam;

            int nRet = strPath.IndexOf('/');
            if (nRet == -1)
            {
                strServerAddr = strPath;
                return 0;
            }

            strServerAddr = strPath.Substring(0, nRet);

            strPath = strPath.Substring(nRet + 1);

            // ����
            nRet = strPath.IndexOf('/');
            if (nRet == -1)
            {
                strDbName = strPath;
                return 0;
            }

            strDbName = strPath.Substring(0, nRet);

            strPath = strPath.Substring(nRet + 1);


            string strTemp = "";

            // '��¼������'����
            nRet = strPath.IndexOf('/');
            if (nRet == -1)
            {
                strTemp = strPath;
                return 0;
            }

            strTemp = strPath.Substring(0, nRet);

            if (strTemp != "ctlno" && strTemp != "��¼������")
            {
                strError = "·�� '" + strPathParam + "' ��ʽ����ȷ";
                return -1;
            }

            strPath = strPath.Substring(nRet + 1);

            // ����
            nRet = strPath.IndexOf('/');
            if (nRet == -1)
            {
                strNumber = strPath;
                return 0;
            }

            strNumber = strPath.Substring(0, nRet);

            return 0;
        }

        // ���滯����·��
        // return:
        //      -1  error
        //      0   Ϊ���Ƿ�ʽ��·��
        //      1   Ϊ׷�ӷ�ʽ��·��
        public static int CanonicalizeWritePath(string strPath,
            out string strOutPath,
            out string strError)
        {
            strError = "";
            strOutPath = "";

            string strServerAddr = "";
            string strDbName = "";
            string strNumber = "";
            int nRet = ParseWritePath(strPath,
                out strServerAddr,
                out strDbName,
                out strNumber,
                out strError);
            if (nRet == -1)
                return -1;

            if (strServerAddr == "")
            {
                strError = "ȱ��������������";
                return -1;
            }

            if (strDbName == "")
            {
                strError = "ȱ�����ݿ�������";
                return -1;
            }

            if (strNumber == "")
            {
                strNumber = "?";    // ��ʾ׷��
            }

            if (strNumber == "?")
            {
                // Ϊ�˱���dt1000/dt1500ĳ���汾�ı���󷵻ص�·���������ʺţ�����Ҫ���������û���ʺ���ʽ��·��
                strOutPath = strServerAddr + "/" + strDbName;
                return 1;   // ��ʾΪ׷�ӷ�ʽ��·��
            }
            else
                strOutPath = strServerAddr + "/" + strDbName + "/��¼������/" + strNumber;

            return 0;   // ��ʾΪ��ͨ���Ƿ�ʽ��·��
        }
    }



}
