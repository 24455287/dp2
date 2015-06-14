using System;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.Deployment.Application;

namespace DigitalPlatform.Xml
{
    /// <summary>
    /// �� XML �ļ��������ĸ���������Ϣ
    /// </summary>
	public class ApplicationInfo
	{
		public XmlDocument dom = new XmlDocument();
		public string PureFileName = "";
		public string CurrentDirectory = "";
		public string FileName = "";

		Hashtable titleTable = null;

		bool m_bFirstMdiOpened = false;

        public event EventHandler LoadMdiSize = null;
        public event EventHandler SaveMdiSize = null;

		public ApplicationInfo()
		{
		}

        /*
        public bool FirstMdiOpened
        {
            get
            {
                return m_bFirstMdiOpened;
            }
            set
            {
                m_bFirstMdiOpened = value;
            }
        }*/

		// ���캯��
		// ��������XML�ļ��е�����װ���ڴ档
		// parameters:
		//		strPureFileName	Ҫ�򿪵�XML�ļ�����ע������һ�����ļ�����������·�����֡��������Զ���ģ��ĵ�ǰĿ¼��װ�ش��ļ���
		public ApplicationInfo(string strPureFileName)
		{
			PrepareFileName(strPureFileName);

			string strErrorInfo;
			int nRet = Load(out strErrorInfo);
			if (nRet < 0) 
			{
				CreateBlank();
			}
		}

		// ���ڴ��е����ݱ����XML�ļ�
		public void Save()
		{
			if (FileName != "") 
			{
				string strErrorInfo;
				Save(out strErrorInfo);
			}
		}

        // parameters:
        //      strFileName �ļ����ַ���������Ǵ��ļ��������Զ����� ClickOnce ��װ����ɫ��װ�������Ŀ¼�������ȫ·������ֱ��ʹ�����·��
        public void PrepareFileName(string strFileName)
        {
            string strPureName = Path.GetFileName(strFileName);
            if (strPureName.ToUpper() != strFileName.ToUpper())
            {
                PureFileName = strPureName;
                FileName = strFileName;
            }
            else
            {
                PureFileName = strFileName;

                if (ApplicationDeployment.IsNetworkDeployed == true)
                {
                    CurrentDirectory = Application.LocalUserAppDataPath;
                }
                else
                {
                    CurrentDirectory = Environment.CurrentDirectory;
                }

                FileName = CurrentDirectory + "\\" + PureFileName;
            }
		}

		// ���ļ���װ����Ϣ
		public int Load(out string strErrorInfo)
		{
			this.dom.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue

			strErrorInfo = "";

			if (FileName == "") 
			{
				strErrorInfo = "FileNameΪ��...";
				return -1;
			}

			try 
			{
				dom.Load(FileName);
			}
			catch (FileNotFoundException ex) 
			{
				strErrorInfo = "�ļ�û���ҵ�: " + ex.Message;
				return -2;
			}
			catch (XmlException ex)
			{
				strErrorInfo = "װ���ļ� " + FileName + "ʱ����:" + ex.Message;
				return -1;
			}	

			return 0;
		}


		public int CreateBlank()
		{
			dom.LoadXml("<?xml version='1.0' encoding='utf-8' ?><root/>");
			return 0;
		}

		public int Save(out string strErrorInfo)
		{
			strErrorInfo = "";

			if (FileName == "") 
			{
				strErrorInfo = "FileNameΪ��...";
				return -1;
			}

			dom.Save(FileName);

			return 0;
		}


        // ���һ������ֵ
        // parameters:
        //		strPath	����·��
        //		strName	������
        //		bDefault	ȱʡֵ
        // return:
        //		����õĲ���ֵ
        public bool GetBoolean(string strPath,
            string strName,
            bool bDefault)
        {
            strPath = "/root/" + strPath;

            XmlNode node = dom.SelectSingleNode(strPath);
            string strText = null;

            if (node == null)
                return bDefault;

            strText = DomUtil.GetAttrOrDefault(node, strName, null);
            if (strText == null)
                return bDefault;

            if (String.Compare(strText, "true", true) == 0)
                return true;

            if (String.Compare(strText, "false", true) == 0)
                return false;

            return false;
        }


        // д��һ������ֵ
        // parameters:
        //		strPath	����·��
        //		strName	������
        //		bValue	Ҫд��Ĳ���ֵ
        public void SetBoolean(string strPath,
            string strName,
            bool bValue)
        {
            strPath = "/root/" + strPath;

            string[] aPath = strPath.Split(new char[] { '/' });
            XmlNode node = DomUtil.CreateNode(dom, aPath);

            if (node == null)
            {
                throw (new Exception("SetInt() set error ..."));
            }

            DomUtil.SetAttr(node,
                strName,
                (bValue == true ? "true" : "false"));
        }

        //

		// ���һ������ֵ
		// parameters:
		//		strPath	����·��
		//		strName	������
		//		nDefault	ȱʡֵ
		// return:
		//		����õ�����ֵ
		public int GetInt(string strPath,
			string strName,
			int nDefault)
		{

			strPath = "/root/" + strPath;

			XmlNode node = dom.SelectSingleNode(strPath);
			string strText = null;

			if (node == null)
				return nDefault;

			strText = DomUtil.GetAttrOrDefault(node, strName, null);
			if (strText == null)
				return nDefault;

			return Convert.ToInt32(strText);
		}


		// д��һ������ֵ
		// parameters:
		//		strPath	����·��
		//		strName	������
		//		nValue	Ҫд�������ֵ
        public void SetInt(string strPath,
            string strName,
            int nValue)
        {
            strPath = "/root/" + strPath;

            string[] aPath = strPath.Split(new char[] { '/' });
            XmlNode node = DomUtil.CreateNode(dom, aPath);

            if (node == null)
            {
                throw (new Exception("SetInt() set error ..."));
            }

            DomUtil.SetAttr(node,
                strName,
                Convert.ToString(nValue));
        }

		// ���һ���ַ���
		// parameters:
		//		strPath	����·��
		//		strName	������
		//		strDefalt	ȱʡֵ
		// return:
		//		Ҫ��õ��ַ���
		public string GetString(string strPath,
			string strName,
			string strDefault)
		{
			strPath = "/root/" + strPath;

			XmlNode node = dom.SelectSingleNode(strPath);

			if (node == null)
				return strDefault;

			return DomUtil.GetAttrOrDefault(node, strName, strDefault);
		}


		// ����һ���ַ���
		// parameters:
		//		strPath	����·��
		//		strName	������
		//		strValue	Ҫ���õ��ַ��������Ϊnull����ʾɾ���������
		public void SetString(string strPath,
			string strName,
			string strValue)
		{
			strPath = "/root/" + strPath;

			string[] aPath = strPath.Split(new char[]{'/'});
			XmlNode node = DomUtil.CreateNode(dom, aPath);

			if (node == null) 
			{
				throw(new Exception("SetString() error ..."));
			}

			DomUtil.SetAttr(node,
				strName,
				strValue);
		}


        ////
        // ���һ��������
        // parameters:
        //		strPath	����·��
        //		strName	������
        //		fDefault	ȱʡֵ
        // return:
        //		Ҫ��õ��ַ���
        public float GetFloat(string strPath,
            string strName,
            float fDefault)
        {
            strPath = "/root/" + strPath;

            XmlNode node = dom.SelectSingleNode(strPath);

            if (node == null)
                return fDefault;

            string strDefault = fDefault.ToString();

            string strValue = DomUtil.GetAttrOrDefault(node, 
                strName,
                strDefault);

            try
            {
                return (float)Convert.ToDouble(strValue);
            }
            catch
            {
                return fDefault;
            }
        }


        // ����һ��������
        // parameters:
        //		strPath	����·��
        //		strName	������
        //		fValue	Ҫ���õ��ַ���
        public void SetFloat(string strPath,
            string strName,
            float fValue)
        {
            strPath = "/root/" + strPath;

            string[] aPath = strPath.Split(new char[] { '/' });
            XmlNode node = DomUtil.CreateNode(dom, aPath);

            if (node == null)
            {
                throw (new Exception("SetString() error ..."));
            }

            DomUtil.SetAttr(node,
                strName,
                fValue.ToString());
        }

        // ��װ��İ汾
        public void LoadFormStates(Form form,
            string strCfgTitle)
        {
            LoadFormStates(form, strCfgTitle, FormWindowState.Normal);
        }

		// ��ApplicationInfo�ж�ȡ��Ϣ������form�ߴ�λ��״̬
		// parameters:
		//		form	Form����
		//		strCfgTitle	������Ϣ·�������������ô�ֵ��ΪGetString()��GetInt()��strPath����ʹ��
		public void LoadFormStates(Form form,
			string strCfgTitle,
            FormWindowState default_state)
		{
            // Ϊ���Ż��Ӿ�Ч��
            bool bVisible = form.Visible;

            if (bVisible == true)
                form.Visible = false;

			form.Width = this.GetInt(
				strCfgTitle, "width", form.Width);
			form.Height = this.GetInt(
				strCfgTitle, "height", form.Height);

			form.Location = new Point(
				this.GetInt(strCfgTitle, "x", form.Location.X),
				this.GetInt(strCfgTitle, "y", form.Location.Y));

            string strState = this.GetString(
				strCfgTitle,
                "window_state",
                "");
            if (String.IsNullOrEmpty(strState) == true)
            {
                form.WindowState = default_state;
            }
            else
            {
                form.WindowState = (FormWindowState)Enum.Parse(typeof(FormWindowState),
                    strState);
            }

            if (bVisible == true)
                form.Visible = true;

            /// form.Update();  // 2007/4/8
		}

        // װ��MDI�Ӵ��ڵ�������ԡ���Ҫ������һ��MDI�Ӵ��ڴ򿪺����
        public void LoadFormMdiChildStates(Form form,
            string strCfgTitle)
        {
            if (form.ActiveMdiChild == null)
                return;

            string strState = this.GetString(
                strCfgTitle,
                "mdi_child_window_state",
                "");
            if (String.IsNullOrEmpty(strState) == false)
            {
                form.ActiveMdiChild.WindowState = (FormWindowState)Enum.Parse(typeof(FormWindowState),
                    strState);
            }
        }

        // ��װ��İ汾
        public void LoadMdiChildFormStates(Form form,
            string strCfgTitle)
        {
            LoadMdiChildFormStates(form, strCfgTitle,
                600, 400);
        }



		// ��ApplicationInfo�ж�ȡ��Ϣ������MDI Child form�ߴ�λ��״̬
		// ��һ��Form��������,���޸�x,y��Ϣ
		// parameters:
		//		form	Form����
		//		strCfgTitle	������Ϣ·�������������ô�ֵ��ΪGetString()��GetInt()��strPath����ʹ��
		public void LoadMdiChildFormStates(Form form,
			string strCfgTitle,
            int nDefaultWidth,
            int nDefaultHeight)
		{
            // 2009/11/9
            FormWindowState savestate = form.WindowState;
            bool bStateChanged = false;
            if (form.WindowState != FormWindowState.Normal)
            {
                form.WindowState = FormWindowState.Normal;
                bStateChanged = true;
            }

			form.Width = this.GetInt(
                strCfgTitle, "width", nDefaultWidth);
			form.Height = this.GetInt(
                strCfgTitle, "height", nDefaultHeight);

            if (this.LoadMdiSize != null)
                this.LoadMdiSize(form, null);

            // 2009/11/9
            if (bStateChanged == true)
                form.WindowState = savestate;

			/*
			form.Location = new Point(
				this.GetInt(strCfgTitle, "x", 0),
				this.GetInt(strCfgTitle, "y", 0));
			*/

            /*
			if (m_bFirstMdiOpened == false) 
			{
                string strState = this.GetString(
					strCfgTitle,
                    "window_state",
                    "Normal");
				form.WindowState = (FormWindowState)Enum.Parse(typeof(FormWindowState),
                    strState);
				m_bFirstMdiOpened = true;
			}
             * */

		}

		// ����Mdi Child form�ߴ�λ��״̬��ApplicationInfo��
		// parameters:
		//		form	Form����
		//		strCfgTitle	������Ϣ·�������������ô�ֵ��ΪSetString()��SetInt()��strPath����ʹ��
		public void SaveMdiChildFormStates(Form form,
			string strCfgTitle)
		{
			FormWindowState savestate = form.WindowState;

#if NO
            // 2009/11/9
            bool bStateChanged = false;
            if (form.WindowState != FormWindowState.Normal)
            {
                form.WindowState = FormWindowState.Normal;
                bStateChanged = true;
            }
#endif

#if NO
			// form.WindowState = FormWindowState.Normal;	// �Ƿ������ش���?
			this.SetInt(
				strCfgTitle, "width", form.Width);
			this.SetInt(
				strCfgTitle, "height", form.Height);
#endif
            Size size = form.Size;
            Point location = form.Location;

            if (form.WindowState != FormWindowState.Normal)
            {
                size = form.RestoreBounds.Size;
                location = form.RestoreBounds.Location;
            }

            this.SetInt(strCfgTitle, "width", size.Width);
            this.SetInt(strCfgTitle, "height", size.Height);

			this.SetInt(strCfgTitle, "x", location.X);
			this.SetInt(strCfgTitle, "y", location.Y);

            if (this.SaveMdiSize != null)
                this.SaveMdiSize(form, null);

#if NO
            if (bStateChanged == true)
			    form.WindowState = savestate;
#endif
		}


        // ����form�ߴ�λ��״̬��ApplicationInfo��
        // parameters:
        //		form	Form����
        //		strCfgTitle	������Ϣ·�������������ô�ֵ��ΪSetString()��SetInt()��strPath����ʹ��
        public void SaveFormStates(Form form,
            string strCfgTitle)
        {
            // ���洰��״̬
            this.SetString(
                strCfgTitle, "window_state",
                Enum.GetName(typeof(FormWindowState),
                form.WindowState));

            Size size = form.Size;
            Point location = form.Location;

            if (form.WindowState != FormWindowState.Normal)
            {
                size = form.RestoreBounds.Size;
                location = form.RestoreBounds.Location;
            }

#if NO
            // �ߴ�
            form.WindowState = FormWindowState.Normal;	// �Ƿ������ش���?
#endif

            this.SetInt(
                strCfgTitle, "width", size.Width);  // form.Width
            this.SetInt(
                strCfgTitle, "height", size.Height);    // form.Height

            this.SetInt(strCfgTitle, "x", location.X); // form.Location.X
            this.SetInt(strCfgTitle, "y", location.Y); // form.Location.Y

            // ����MDI����״̬ -- �Ƿ���󻯣�
            if (form.ActiveMdiChild != null)
            {
                if (form.ActiveMdiChild.WindowState == FormWindowState.Minimized)
                    this.SetString(
                        strCfgTitle,
                        "mdi_child_window_state",
                        Enum.GetName(typeof(FormWindowState),
                        FormWindowState.Normal));
                else
                    this.SetString(
                        strCfgTitle,
                        "mdi_child_window_state",
                        Enum.GetName(typeof(FormWindowState),
                        form.ActiveMdiChild.WindowState));
            }
            else
            {
                this.SetString(
                    strCfgTitle,
                    "mdi_child_window_state",
                    "");
            }
        }

		// ���������Form������ϵ����Form Load��Closed�׶Σ����Զ���������
		// ������¼��������ָ��ͱ���Form�ߴ�λ�õ�״̬��
		// parameters:
		//		form	Form����
		//		strCfgTitle	������Ϣ·�������������ô�ֵ��Ϊ���GetString()��GetInt()��strPath����ʹ��
		public void LinkFormState(Form form, 
			string strCfgTitle)
		{
			if (titleTable == null)
				titleTable = new Hashtable();

			// titleTable.Add(form, strCfgTitle);
            titleTable[form] = strCfgTitle; // �ظ����벻���׳��쳣

			form.Load += new System.EventHandler(this.FormLoad);
            form.Closed += new System.EventHandler(this.FormClosed);
		}

        // ԭ���ⲿ��������һ�α�����������û�б�Ҫ�ˡ���ȷ�������ǣ����� LinkFormState() ���ɣ��Ի���ر�ʱ���Զ�����óߴ�
		public void UnlinkFormState(Form form)
		{
			if (titleTable == null)
				return;

			titleTable.Remove(form);
			// If the Hashtable does not contain an element with the specified key,
			// the Hashtable remains unchanged. No exception is thrown.

            // 2015/6/5
            form.Load -= new System.EventHandler(this.FormLoad);
            form.Closed -= new System.EventHandler(this.FormClosed);
		}

		private void FormLoad(object sender, System.EventArgs e)
		{
			Debug.Assert(sender != null, "sender����Ϊnull");
			Debug.Assert(sender is Form, "senderӦΪForm����");

			Debug.Assert(titleTable != null, "titleTableӦ���Ѿ���LinkFromState()��ʼ��");

			string strCfgTitle = (string)titleTable[sender];
			Debug.Assert(strCfgTitle != null , "strCfgTitle����Ϊnull");

			this.LoadFormStates((Form)sender, strCfgTitle);
		}

		private void FormClosed(object sender, System.EventArgs e)
		{
			Debug.Assert(sender != null, "sender����Ϊnull");
			Debug.Assert(sender is Form, "senderӦΪForm����");

			Debug.Assert(titleTable != null, "titleTableӦ���Ѿ���LinkFromState()��ʼ��");

			string strCfgTitle = (string)titleTable[sender];
            if (string.IsNullOrEmpty(strCfgTitle) == true)
                return;

			Debug.Assert(strCfgTitle != null , "strCfgTitle����Ϊnull");

			this.SaveFormStates((Form)sender, strCfgTitle);

			this.Save();
			this.UnlinkFormState((Form)sender);
		}

	}

}
