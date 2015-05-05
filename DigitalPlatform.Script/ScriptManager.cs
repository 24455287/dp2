using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

using System.Reflection;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System.CodeDom;
using System.CodeDom.Compiler;

using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.GUI;
using DigitalPlatform.Text;
using DigitalPlatform.CommonControl;


namespace DigitalPlatform.Script
{

	/// <summary>
	/// �ű�����
	/// </summary>
	public class ScriptManager
	{
		public event CreateDefaultContentEventHandler CreateDefaultContent = null;

		public ApplicationInfo	applicationInfo = null;

		public static int m_nLockTimeout = 5000;	// 5000=5��

		public string CfgFilePath = "";	// �����ļ���

		XmlDocument	dom = new XmlDocument();

		bool m_bChanged = false;	// DOM�Ƿ����޸�

		public string DefaultCodeFileDir = "";

        public string DataDir = ""; // ����Ŀ¼

		public ScriptManager()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		// ����
		public void Save()
		{
			if (m_bChanged == true &&
				CfgFilePath != "") 
			{
				dom.Save(CfgFilePath);
				m_bChanged = false;
			}

		}

		// �ڴ������Ƿ������ı�
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

		// ��xml�ļ���װ�����ݵ��ڴ�
		public void Load(bool bAutoCreate = true)
		{
			if (CfgFilePath == "") 
			{
				throw(new Exception("CfgFilePath��ԱֵΪ��"));
			}

            try
            {
                dom.Load(this.CfgFilePath);
            }
            catch (FileNotFoundException)
            {
                // 2011/11/13 �Զ�����
                if (bAutoCreate == false)
                    throw;

                /*
                // �����¼�
                if (this.CreateProjectXmlFile != null)
                {
                    AutoCreateProjectXmlFileEventArgs e1 = new AutoCreateProjectXmlFileEventArgs();
                    e1.Filename = scriptManager.CfgFilePath;
                    this.CreateProjectXmlFile(this, e1);
                }
                 * */

                ScriptManager.CreateDefaultProjectsXmlFile(this.CfgFilePath,
                    "clientcfgs");

                dom.Load(this.CfgFilePath);
            }
            catch (Exception ex)
            {
                throw ex;
            }

			// ȱʡ����Ŀ¼
			DefaultCodeFileDir = DomUtil.GetAttr(
				dom.DocumentElement, 
				".",
				"defaultCodeFileDir");
			if (DefaultCodeFileDir == "")
			{
				// ����Ϊִ�г���Ŀ¼֮��clientcfgsĿ¼
				DefaultCodeFileDir = 
					Environment.CurrentDirectory +"\\clientcfgs";

			}
			else 
			{
                string strPath = "";
                if (Path.IsPathRooted(DefaultCodeFileDir) == true)
                    strPath = DefaultCodeFileDir;
                else
                {
                    if (String.IsNullOrEmpty(this.DataDir) == true)
                        throw new Exception("��DefaultCodeFileDir="+DefaultCodeFileDir+"Ϊ���·��ʱ����Ҫ����this.DataDir����ȷ��ȫ·����");
                    strPath = PathUtil.MergePath(this.DataDir, DefaultCodeFileDir);
                }

				DirectoryInfo di = new DirectoryInfo(strPath);

				DefaultCodeFileDir = di.FullName;
			}
		}

        string MacroPath(string strPath)
        {
            if (String.IsNullOrEmpty(this.DefaultCodeFileDir) == true)
                return strPath;

            // ����strPath1�Ƿ�ΪstrPath2���¼�Ŀ¼���ļ�
            if (PathUtil.IsChildOrEqual(strPath, this.DefaultCodeFileDir) == true)
            {
                string strPart = strPath.Substring(this.DefaultCodeFileDir.Length);
                return "%default_code_file_dir%" + strPart;
            }

            return strPath;
        }

        string UnMacroPath(string strPath)
        {
            if (String.IsNullOrEmpty(this.DefaultCodeFileDir) == true)
                return strPath;

            return strPath.Replace("%default_code_file_dir%", this.DefaultCodeFileDir);
        }

		// �������ļ�����������TreeView
		public void FillTree(TreeView treeView)
		{
			/*
			if (CfgFilePath == "") 
			{
				throw(new Exception("CfgFilePath��ԱֵΪ��"));
			}

			dom.Load(CfgFilePath);
			*/
            if (this.dom.DocumentElement == null)
			    Load();

			treeView.Nodes.Clear();// �����ǰ�����

			FillOneLevel(treeView, null, dom.DocumentElement);

			/*
			// ȱʡ����Ŀ¼
			DefaultCodeFileDir = DomUtil.GetAttr(
				dom.DocumentElement, 
				".",
				"defaultCodeFileDir");
			if (DefaultCodeFileDir == "")
			{
				// ����Ϊִ�г���Ŀ¼֮��clientcfgsĿ¼
				DefaultCodeFileDir = 
					Environment.CurrentDirectory +"\\clientcfgs";

			}
			else 
			{
				DirectoryInfo di = new DirectoryInfo(DefaultCodeFileDir);

				DefaultCodeFileDir = di.FullName;
			}
			*/
		}

		// ˢ��ȫ����ʾ
		public void RefreshTree(TreeView treeView)
		{
			FillOneLevel(treeView, null, dom.DocumentElement);
		}

		// ���treeviewһ��(�Լ�����ȫ����)������
		public void FillOneLevel(TreeView treeView,
			TreeNode treeNode,
			XmlNode node)
		{
			XmlNode nodeChild = null;

			// �����ǰ�����
			if (treeNode == null)
				treeView.Nodes.Clear();
			else
				treeNode.Nodes.Clear();

			for(int i = 0; i < node.ChildNodes.Count; i++) 
			{
				nodeChild = node.ChildNodes[i];
				if (nodeChild.NodeType != XmlNodeType.Element)
					continue;

				if (nodeChild.Name == "dir") 
				{
					// ��node
					string strDirName = DomUtil.GetAttr(nodeChild, "name");

					TreeNode nodeNew = new TreeNode(strDirName, 0, 0);


					if (treeNode == null)
						treeView.Nodes.Add(nodeNew);
					else
						treeNode.Nodes.Add(nodeNew);

					// �ݹ�
					FillOneLevel(treeView,
						nodeNew,
						nodeChild);
				}
				else if (nodeChild.Name == "project")
				{
					// ��node
					string strProjectName = DomUtil.GetAttr(nodeChild, "name");

					TreeNode nodeNew = new TreeNode(strProjectName, 1, 1);


					if (treeNode == null)
						treeView.Nodes.Add(nodeNew);
					else
						treeNode.Nodes.Add(nodeNew);
				}

			}

		}

        // �г�ȫ���Ѿ���װ��URL
        public int GetInstalledUrls(out List<string> urls,
            out string strError)
        {
            strError = "";
            urls = new List<string>();

            XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("//project");

            foreach (XmlNode node in nodes)
            {
                string strLocate = DomUtil.GetAttr(node, "locate");
                strLocate = UnMacroPath(strLocate);

                XmlDocument metadata_dom = null;
                // ���(һ���Ѿ���װ��)����Ԫ����
                // parameters:
                //      dom ����Ԫ����XMLDOM
                // return:
                //      -1  ����
                //      0   û���ҵ�Ԫ�����ļ�
                //      1   �ɹ�
                int nRet = ScriptManager.GetProjectMetadata(strLocate,
                out metadata_dom,
                out strError);


                if (nRet == -1)
                    return -1;

                if (nRet == 0)
                {
                    continue;
                }

                string strUpdateUrl = DomUtil.GetAttr(metadata_dom.DocumentElement,
        "updateUrl");
                if (string.IsNullOrEmpty(strUpdateUrl) == false)
                {
                    urls.Add(strUpdateUrl);
                }
            }

            return 0;
        }

        public static string GetFileNameFromUrl(string strPath)
        {
            int nRet = strPath.LastIndexOf("/");
            if (nRet != -1)
                strPath = strPath.Substring(nRet+1);
            return strPath;
        }

        // ������һ�������ڵ��µ�ȫ������
        // parameters:
        //      dir_node    �����ڵ㡣��� == null ������ȫ������
        //      strSource   "!url"���ߴ���Ŀ¼���ֱ��ʾ����������£����ߴӴ��̼�����
        // return:
        //      -2  ȫ������
        //      -1  ����
        //      0   �ɹ�
        public int CheckUpdate(
            IWin32Window owner,
            XmlNode dir_node,
            string strSource,
            ref bool bHideMessageBox,
            ref bool bDontUpdate,
            ref int nUpdateCount,
            ref string strUpdateInfo,
            ref string strWarning,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            XmlNodeList nodes = null;
            if (dir_node == null)
                nodes = this.dom.DocumentElement.SelectNodes("//project");
            else
                nodes = dir_node.SelectNodes(".//project");

            foreach (XmlNode node in nodes)
            {
                // �����ڵ�
                // return:
                //      -2  �������еĸ���
                //      -1  ����
                //      0   û�и���
                //      1   �Ѿ�����
                //      2   ��ΪĳЩԭ���޷�������
                nRet = CheckUpdateOneProject(
                        owner,
                        node,
                        strSource,
                        ref bHideMessageBox,
                        ref bDontUpdate,
                        out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == -2)
                    return -2;
                if (nRet == 2)
                    strWarning += "���� " + DomUtil.GetAttr(node, "name") + " " + strError + ";\r\n";

                if (nRet == 1)
                {
                    nUpdateCount++;
                    strUpdateInfo += DomUtil.GetAttr(node, "name") + "\r\n";
                }
            }

            return 0;
        }

        // ����һ��Ŀ¼�е�ȫ�� .projpack �ļ������� projects.xml �ļ�
        public static int BuildProjectsFile(string strDirectory,
            string strProjectsFilename,
            out string strError)
        {
            strError = "";

            List<ProjectInstallInfo> infos = new List<ProjectInstallInfo>();

            // �г������ļ�
            try
            {
                DirectoryInfo di = new DirectoryInfo(strDirectory);

                FileInfo[] fis = di.GetFiles("*.projpack");

                for (int i = 0; i < fis.Length; i++)
                {
                    string strFileName = fis[i].FullName;

                    List<ProjectInstallInfo> temp_infos = null;
                    // ��ȡprojpack�ļ��еķ��������ֺ�Host��Ϣ
                    int nRet = ScriptManager.GetInstallInfos(
                        strFileName,
                        out temp_infos,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // 2013/5/11
                    // ���� ProjectPath 
                    foreach (ProjectInstallInfo info in temp_infos)
                    {
                        info.ProjectPath = strFileName;
                    }

                    infos.AddRange(temp_infos);
                }

                // TODO: ���б��������

                XmlDocument dom = new XmlDocument();
                dom.LoadXml("<root />");

                foreach (ProjectInstallInfo info in infos)
                {
                    XmlNode node = dom.CreateElement("project");
                    dom.DocumentElement.AppendChild(node);

                    DomUtil.SetAttr(node, "name", info.ProjectName);
                    DomUtil.SetAttr(node, "host", info.Host);
                    DomUtil.SetAttr(node, "url", info.UpdateUrl);
                    DomUtil.SetAttr(node, "localFile", info.ProjectPath);   // 2013/5/11
                    DomUtil.SetAttr(node, "index", info.IndexInPack.ToString());
                }

                dom.Save(strProjectsFilename);
                return 0;
            }
            catch (Exception ex)
            {
                strError = "�г��ļ��Ĺ����г���: " + ex.Message;
                return -1;
            }
        }

        // ������һ������
        // parameters:
        //      strSource   "!url"���ߴ���Ŀ¼���ֱ��ʾ����������£����ߴӴ��̼�����
        // return:
        //      -2  �������еĸ���
        //      -1  ����
        //      0   û�и���
        //      1   �Ѿ�����
        //      2   ��ΪĳЩԭ���޷�������
        public int CheckUpdateOneProject(
            IWin32Window owner,
            XmlNode node,
            string strSource,
            ref bool bHideMessageBox,
            ref bool bDontUpdate,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // string strProjectNamePath = node.FullPath;
            string strLocate = "";
            string strIfModifySince = "";

            strIfModifySince = DomUtil.GetAttr(node, "lastModified");

            strLocate = DomUtil.GetAttr(node, "locate");
            strLocate = UnMacroPath(strLocate);

            string strNamePath = GetNodePathName(node);

            // �������URL

            XmlDocument metadata_dom = null;
            // ���(һ���Ѿ���װ��)����Ԫ����
            // parameters:
            //      dom ����Ԫ����XMLDOM
            // return:
            //      -1  ����
            //      0   û���ҵ�Ԫ�����ļ�
            //      1   �ɹ�
            nRet = ScriptManager.GetProjectMetadata(strLocate,
            out metadata_dom,
            out strError);

            if (nRet == -1)
                return -1;

            if (nRet == 0)
            {
                strError = "Ԫ�����ļ������ڣ�����޷�������";
                return 2;   // û��Ԫ�����ļ����޷�����
            }

            if (metadata_dom.DocumentElement == null)
            {
                strError = "Ԫ����DOM�ĸ�Ԫ�ز����ڣ�����޷�������";
                return 2;
            }

            string strUpdateUrl = DomUtil.GetAttr(metadata_dom.DocumentElement,
                "updateUrl");
            if (string.IsNullOrEmpty(strUpdateUrl) == true)
            {
                strError = "Ԫ����D��û�ж���updateUrl���ԣ�����޷�������";
                return 2;
            }

            List<string> protect_filenames = null;
            string strProtectList = DomUtil.GetAttr(metadata_dom.DocumentElement,
                "protectFiles");
            if (string.IsNullOrEmpty(strProtectList) == false)
            {
                protect_filenames = StringUtil.SplitList(strProtectList, ',');
            }

            string strLocalFileName = "";
            string strLastModified = "";
            // ��������ָ�����ں���¹����ļ�
            if (strSource == "!url")
            {

                Debug.Assert(string.IsNullOrEmpty(this.DataDir) == false, "");

                strLocalFileName = PathUtil.MergePath(this.DataDir, "~temp_project.projpack");
                string strTempFileName = PathUtil.MergePath(this.DataDir, "~temp_download_webfile");


                try
                {
                    File.Delete(strLocalFileName);
                }
                catch
                {
                }
                try
                {
                    File.Delete(strTempFileName);
                }
                catch
                {
                }

                nRet = WebFileDownloadDialog.DownloadWebFile(
                    owner,
                    strUpdateUrl,
                    strLocalFileName,
                    strTempFileName,
                    strIfModifySince,
                    out strLastModified,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    return 0;
                }
            }
            else
            {
                // ����Ŀ¼�е��ļ�

                string strPureFileName = GetFileNameFromUrl(strUpdateUrl);
                strLocalFileName = PathUtil.MergePath(strSource, strPureFileName);

                FileInfo fi = new FileInfo(strLocalFileName);
                if (fi.Exists == false)
                {
                    strError = "Ŀ¼ '" + strSource + "' ��û���ҵ��ļ� '" + strPureFileName + "'";
                    return 0;
                }
                strLastModified = DateTimeUtil.Rfc1123DateTimeString(fi.LastWriteTimeUtc);
            }

            if (string.IsNullOrEmpty(strIfModifySince) == false
                && string.IsNullOrEmpty(strLastModified) == false)
            {
                DateTime ifmodifiedsince = DateTimeUtil.FromRfc1123DateTimeString(strIfModifySince);
                DateTime lastmodified = DateTimeUtil.FromRfc1123DateTimeString(strLastModified);
                if (ifmodifiedsince == lastmodified)
                    return 0;
            }

            // ѯ���Ƿ����
            if (bHideMessageBox == false)
            {
                DialogResult result = MessageDialog.Show(owner,
                    "�Ƿ�Ҫ����ͳ�Ʒ��� '" + strNamePath + "' ?\r\n\r\n(�ǣ�����; ��: �����£������������������; ȡ��: �жϸ��¼�����)",
                    MessageBoxButtons.YesNoCancel,
                    bDontUpdate == true ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1,
                    "�Ժ�����ʾ�������ε�ѡ����",
                    ref bHideMessageBox);
                if (result == DialogResult.Cancel)
                {
                    strError = "����ȫ������";
                    return -2;
                }
                if (result == DialogResult.No)
                {
                    bDontUpdate = true;
                    return 0;
                }
            }

            if (bDontUpdate == true)
                return 0;

            nRet = UpdateProject(
                strLocalFileName,
                strLocate,
                protect_filenames,
                out strError);
            if (nRet == -1)
                return -1;

            DomUtil.SetAttr(node, "lastModified",
    strLastModified);

            this.m_bChanged = true;
            this.Save();

            return 1;
        }

        // ��ȡprojpack�ļ��еķ��������ֺ�Host��Ϣ
        public static int GetInstallInfos(
            string strFilename,
            out List<ProjectInstallInfo> infos,
            out string strError)
        {
            strError = "";
            infos = null;

            Stream stream = null;
            try
            {
                stream = File.Open(strFilename, FileMode.Open);
            }
            catch (FileNotFoundException)
            {
                strError = "�ļ� " + strFilename + "������...";
                return -1;
            }


            BinaryFormatter formatter = new BinaryFormatter();

            ProjectCollection projects = null;
            try
            {
                projects = (ProjectCollection)formatter.Deserialize(stream);
            }
            catch (SerializationException ex)
            {
                strError = "װ�ش���ļ�����" + ex.Message;
                return -1;
            }
            finally
            {
                stream.Close();
            }

            if (projects.Count == 0)
            {
                strError = ".projpack�ļ���û�а����κ�Project";
                return -1;
            }

            infos = new List<ProjectInstallInfo>();

            for (int i = 0; i < projects.Count; i++)
            {
                Project project = (Project)projects[i];

                string strPath = "";
                string strName = "";
                ScriptManager.SplitProjectPathName(project.NamePath,
    out strPath,
    out strName);

                ProjectInstallInfo info = new ProjectInstallInfo();
                info.ProjectPath = strPath;
                info.ProjectName = strName;
                info.IndexInPack = i;

                info.Host = project.GetHostName();
                info.UpdateUrl = project.GetUpdateUrl();

                infos.Add(info);
            }

            return 0;
        }

        // ��װProject
        // return:
        //      -1  ����
        //      0   û�а�װ����
        //      >0  ��װ�ķ�����
        public int InstallProject(
            IWin32Window owner,
            string strStatisWindowName,
            string strFilename,
            string strLastModified,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            int nCount = 0;

            Stream stream = null;
            try
            {
                stream = File.Open(strFilename, FileMode.Open);
            }
            catch (FileNotFoundException)
            {
                strError = "�ļ� " + strFilename + "������...";
                return -1;
            }


            BinaryFormatter formatter = new BinaryFormatter();

            ProjectCollection projects = null;
            try
            {
                projects = (ProjectCollection)formatter.Deserialize(stream);
            }
            catch (SerializationException ex)
            {
                strError = "װ�ش���ļ�����" + ex.Message;
                return -1;
            }
            finally
            {
                stream.Close();
            }

            if (projects.Count == 0)
            {
                strError = ".projpack�ļ���û�а����κ�Project";
                return -1;
            }

            /*
            if (projects.Count > 1)
            {
                strError = ".projpack�ļ��а����˶��������Ŀǰ�ݲ�֧�ִ����л�ȡ������";
                return -1;
            }
             * */

            for (int i = 0; i < projects.Count; i++)
            {
                Project project = (Project)projects[i];

                string strPath = "";
                string strName = "";
                ScriptManager.SplitProjectPathName(project.NamePath,
    out strPath,
    out strName);

                // ����Ŀ¼
                if (string.IsNullOrEmpty(strPath) == false)
                {
                    XmlNode xmlNode = this.LocateDirNode(
                        strPath);
                    if (xmlNode == null)
                    {
                        xmlNode = this.NewDirNode(
                            strPath);
                    }
                }

            REDO_CHECKDUP:

                // ����
                string strExistLocate;
                // ��÷�������
                // strProjectNamePath	������������·��
                // return:
                //		-1	error
                //		0	not found project
                //		1	found
                nRet = this.GetProjectData(
                    ScriptManager.MakeProjectPathName(strPath, strName),
                    out strExistLocate);
                if (nRet == -1)
                {
                    strError = "GetProjectData " + ScriptManager.MakeProjectPathName(strPath, strName) + " error";
                    return -1;
                }

                bool bOverwrite = false;
                if (nRet == 1)
                {
                    string strDirName = "";
                    if (string.IsNullOrEmpty(strPath) == false)
                        strDirName = "Ŀ¼ '"+strPath+"' ��";
                    DialogResult result = MessageBox.Show(owner,
    strStatisWindowName + " " + strDirName + "�Ѿ�����Ϊ '"+strName+"' �ķ�����\r\n\r\n�����Ƿ�Ҫ������?\r\n\r\n(Yes: ����  No: ������װ; Cancel: ������װ�˷�������������װ����ķ���)",
    "ScriptManager",
    MessageBoxButtons.YesNoCancel,
    MessageBoxIcon.Question,
    MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                    {
                        strName = InputDlg.GetInput(owner,
                            "��ָ��һ���µķ�����",
                            "������",
                            strName,
                            null);
                        if (strName == null)
                        {
                            continue;
                        }
                        goto REDO_CHECKDUP;
                    }

                    if (result == DialogResult.Cancel)
                        continue;

                    if (result == DialogResult.Yes)
                    {
                        bOverwrite = true;
                    }
                }

                if (bOverwrite == true)
                {
                    try
                    {
                        RemoveExistFiles(strExistLocate,
                            null);
                    }
                    catch
                    {
                    }
                }
                else
                {
                    int nPrefixNumber = -1;

                    string strLocatePrefix = "";
                    if (project.Locate == "")
                    {
                        strLocatePrefix = strName;
                    }
                    else
                    {
                        strLocatePrefix = PathUtil.PureName(project.Locate);
                    }

                    strExistLocate = this.NewProjectLocate(
                        strLocatePrefix,
                        ref nPrefixNumber);
                }

                try
                {
                    // ֱ��paste
                    project.WriteToLocate(strExistLocate, true);
                }
                catch (Exception ex)
                {
                    strError = "�����ļ����뷽��Ŀ¼ '" + strExistLocate + "' ʱ����: " + ex.Message;
                    return -1;
                }

                string strNamePath = ScriptManager.MakeProjectPathName(strPath, strName);

                // ʵ�ʲ���project����
                XmlNode projNode = this.NewProjectNode(
                    strNamePath,
                    strExistLocate,
                    false);	// false��ʾ����Ҫ����Ŀ¼��ȱʡ�ļ�
                DomUtil.SetAttr(projNode, "lastModified",
    strLastModified);

                nCount++;
            }

            if (nCount > 0)
            {
                this.m_bChanged = true;
                this.Save();
            }

            return nCount;
        }

        // ����Project
        // parameters:
        //      reserve_filenames   ��Ҫ�������ļ����б����Ŀ¼���Ѿ�����Щ�ļ�����Ҫ����
        private int UpdateProject(
            string strFilename,
            string strExistLocate,
            List<string> protect_filenames,
            out string strError)
        {
            strError = "";

            Project project = null;
            Stream stream = null;
            try
            {
                stream = File.Open(strFilename, FileMode.Open);
            }
            catch (FileNotFoundException)
            {
                strError = "�ļ� " + strFilename + "������...";
                return -1;
            }

            BinaryFormatter formatter = new BinaryFormatter();

            ProjectCollection projects = null;
            try
            {
                projects = (ProjectCollection)formatter.Deserialize(stream);
            }
            catch (SerializationException ex)
            {
                strError = "װ�ش���ļ�����" + ex.Message;
                return -1;
            }
            finally
            {
                stream.Close();
            }

            if (projects.Count == 0)
            {
                strError = ".projpack�ļ���û�а����κ�Project";
                return -1;
            }
            if (projects.Count > 1)
            {
                strError = ".projpack�ļ��а����˶��������Ŀǰ�ݲ�֧�ִ����л�ȡ������";
                return -1;
            }

            project = (Project)projects[0];

            try
            {
                RemoveExistFiles(strExistLocate,
                    protect_filenames);
            }
            catch
            {
            }

            try
            {
                // ֱ��paste
                project.WriteToLocate(strExistLocate,
                    false);
            }
            catch (Exception ex)
            {
                strError = "�����ļ����뷽��Ŀ¼ '"+strExistLocate+"' ʱ����: " + ex.Message;
                return -1;
            }

            return 0;
        }

        // parameters:
        //      reserve_filenames   ��Ҫ�������ļ����б����Ŀ¼���Ѿ�����Щ�ļ�����Ҫ���ǡ�Ҫ��������ļ�������Сд�ַ�
        static void RemoveExistFiles(string strExistLocate,
                    List<string> protect_filenames)
        {
            /*
            // ɾ������Ŀ¼�е�ȫ���ļ�
            try
            {
                Directory.Delete(strExistLocate, true);
            }
            catch (Exception ex)
            {
                strError = "ɾ��Ŀ¼ʱ����: " + ex.Message;
                return -1;
            }
            PathUtil.CreateDirIfNeed(strExistLocate);
            */
            // ��Ŀ¼�������ļ�������
            DirectoryInfo di = new DirectoryInfo(strExistLocate);

            FileInfo[] afi = di.GetFiles();

            for (int i = 0; i < afi.Length; i++)
            {
                string strFileName = afi[i].Name;
                if (strFileName.Length > 0
                    && strFileName[0] == '~')
                    continue;	// ������ʱ�ļ�

                if (protect_filenames != null
                    && protect_filenames.IndexOf(strFileName.ToLower()) != -1)
                    continue;

                try
                {
                    File.Delete(afi[i].FullName);
                }
                catch
                {
                }
            }
        }

		// ����Assembly
		// parameters:
		//		saAddtionalRef	���ӵ�refs�ļ�·����·���п��ܰ�����%installdir%
		//		strLibPaths	���ӵĿ�����·���ǣ���','�ָ���·���ַ���
		// return:
		//		-2	���������Ѿ���ʾ��������Ϣ�ˡ�
		//		-1	����
		public int BuildAssembly(
            string strHostName,
			string strProjectNamePath,
			string strSourceFileName,	// ������Ŀ¼���ֵĴ��ļ���
			string [] saAdditionalRef,	// ���ӵ�refs
			string strLibPaths,	
			string strOutputFileName,
			out string strError,
			out string strWarning)
		{
			strWarning = "";
			int nRet;
			string strLocate = "";
			string strCodeFileName;
			string [] saRef = null;

			// ��÷�������
			// strProjectNamePath	������������·��
			// return:
			//		-1	error
			//		0	not found project
			//		1	found
			nRet = GetProjectData(strProjectNamePath,
				out strLocate);
			if (nRet == -1) 
			{
				strError = "GetProjectData() error ...";
				return -1;
			}
			if (nRet == 0) 
			{
                strError = "���� " + strHostName + " ���� '" + strProjectNamePath + "' û���ҵ� ...";
				return -1;
			}

			// refs

			string strRefFileName = strLocate + "\\" + "references.xml";

			nRet = GetRefs(strRefFileName,
				out saRef,
				out strError);
			if (nRet == -1)
				return -1;


			if (saAdditionalRef != null)
			{
                if (saRef == null)
                    saRef = new string[0];

				string[] saTemp = new string[saRef.Length + saAdditionalRef.Length];
				Array.Copy(saRef,0, saTemp, 0, saRef.Length);
				Array.Copy(saAdditionalRef,0, saTemp, saRef.Length, saAdditionalRef.Length);
				saRef = saTemp;
			}

			// �滻%projectdir%��
			RemoveRefsProjectDirMacro(ref saRef, strLocate);

			strCodeFileName = strLocate + "\\" + strSourceFileName;

			StreamReader sr = null;
			
			try 
			{
				sr = new StreamReader(strCodeFileName, true);
			}
			catch (Exception ex)
			{
				strError = ex.Message;
				return -1;
			}
			string strCode = sr.ReadToEnd();
			sr.Close();



			nRet = CreateAssemblyFile(strCode,
				saRef,
				strLibPaths,
				strOutputFileName,
				out strError,
				out strWarning);

			if (strError != "") 
			{
				strError = "���� " +strHostName+ " ���� '"+strProjectNamePath
					+"' ���ļ� '" + strSourceFileName + "' ���뷢�ִ���򾯸�:\r\n" + strError;
				CompileErrorDlg dlg = new CompileErrorDlg();
                GuiUtil.AutoSetDefaultFont(dlg);
                dlg.applicationInfo = applicationInfo;
				dlg.Initial(strCodeFileName,
					strError);
			{
				string strTemp = strSourceFileName;
				if (strTemp.IndexOf(".fltx.",0) != -1)
					dlg.IsFltx = true;
			}
				dlg.ShowDialog();
				strError = "����ִ�б���ֹ�����޸�Դ���������ִ�С�";
				return -2;
			}

			if (strWarning != "") 
			{
                strWarning = "���� " + strHostName + " ���� '" + strProjectNamePath
					+"' ���ļ�" + strSourceFileName + "' ���뷢�־���:\r\n" + strWarning;
			}


			return nRet;
		}

        // ��xml�ַ����еõ�refs�ַ�������
        // return:
        //		-1	error
        //		0	��ȷ
        public static int GetRefsFromXml(string strRefXml,
            out string[] saRef,
            out string strError)
        {
            saRef = null;
            strError = "";
            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strRefXml);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            // ����ref�ڵ�
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("//ref");
            saRef = new string[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                saRef[i] = DomUtil.GetNodeText(nodes[i]);
            }

            return 0;
        }

		// ��references.xml�ļ��еõ�refs�ַ�������
		// return:
		//		-1	error
		//		0	not found file
		//		1	found file
		public static int GetRefs(string strXmlFileName,
			out string [] saRef,
			out string strError)
		{
			saRef = null;
			strError = "";
			XmlDocument dom = new XmlDocument();

			try 
			{
				dom.Load(strXmlFileName);
			}
			catch (FileNotFoundException ex)
			{
				strError = ex.Message;
				return 0;
			}
			catch (Exception ex)
			{
				strError = ex.Message;
				return -1;
			}


			// ����ref�ڵ�
			XmlNodeList nodes = dom.DocumentElement.SelectNodes("//ref");
			saRef = new string [nodes.Count];
			for(int i=0;i<nodes.Count;i++)
			{
				saRef[i] = DomUtil.GetNodeText(nodes[i]);
			}


			return 1;
		}

		// ��refs�ַ�������д��references.xml�ļ���
		// return:
		//		-1	error
		//		0	Suceed
		static int SaveRefs(string strXmlFileName,
			string [] saRef,
			out string strError)
		{
			strError = "";

			XmlTextWriter writer = null;   //XmlTextWriter����

			writer = new XmlTextWriter(strXmlFileName, Encoding.UTF8);
			writer.Formatting = Formatting.Indented;

			writer.WriteStartDocument();

			writer.WriteStartElement("root");

			for(int i=0;i<saRef.Length;i++) 
			{
				writer.WriteElementString("ref", saRef[i]);
			}

			writer.WriteEndElement();

			writer.WriteEndDocument();

			writer.Close();

			return 0;
		}

        static int SaveMetadata(string strXmlFileName,
            string strUpdateUrl,
            string strHostName,
            out string strError)
        {
            strError = "";

            XmlTextWriter writer = null;   //XmlTextWriter����

            writer = new XmlTextWriter(strXmlFileName, Encoding.UTF8);
            writer.Formatting = Formatting.Indented;

            writer.WriteStartDocument();

            writer.WriteStartElement("root");

            writer.WriteAttributeString("updateUrl", strUpdateUrl);
            writer.WriteAttributeString("host", strHostName);

            writer.WriteEndElement();

            writer.WriteEndDocument();

            writer.Close();

            return 0;
        }

		// ��λDir XmlNode�ڵ�
		public XmlNode LocateDirNode(string strDirNamePath)
		{
			string[] aName = strDirNamePath.Split(new Char [] {'/'});

			string strXPath = "";

			for(int i=0;i<aName.Length;i++) 
			{
				if (i != aName.Length - 1) 
				{
					strXPath += "dir[@name='" + aName[i] + "']";
					if (strXPath != "")
						strXPath += "/";
				}
				else
					strXPath += "dir[@name='" + aName[i] + "']";

			}

			return dom.DocumentElement.SelectSingleNode(strXPath);
		}

		// ��λDir XmlNode�ڵ�
		public XmlNode LocateAnyNode(string strNamePath)
		{
			string[] aName = strNamePath.Split(new Char [] {'/'});

			string strXPath = "";

			for(int i=0;i<aName.Length;i++) 
			{
				if (i != aName.Length - 1) 
				{
					strXPath += "dir[@name='" + aName[i] + "']";
					if (strXPath != "")
						strXPath += "/";
				}
				else
					strXPath += "*[@name='" + aName[i] + "']";

			}

			return dom.DocumentElement.SelectSingleNode(strXPath);
		}


		// ��Dir XmlNode����
		// return:
		//	0	not found
		//	1	found and changed
		public int RenameDir(string strDirNamePath,
			string strNewName)
		{
			XmlNode node = LocateDirNode(strDirNamePath);
			if (node == null)
				return 0;
			DomUtil.SetAttr(node, "name", strNewName);

			m_bChanged = true;

			return 1;
		}

		// ɾ��Dir XmlNode����������ȫ���ڵ�
		// return:
		//	-1	error
		//	0	not found
		//	1	found and changed
		public int DeleteDir(string strDirNamePath,
			out XmlNode parentXmlNode,
			out string strError)
		{
			strError = "";
			parentXmlNode = null;

			XmlNode node = LocateDirNode(strDirNamePath);
			if (node == null)
				return 0;

			// �г������¼�Project�ڵ㲢ɾ��
			//XmlNodeList nodes = node.SelectNodes("descendant::*");
			XmlNodeList nodes = node.SelectNodes("descendant::project");
			for(int i=0;i<nodes.Count;i++)
			{
				if (nodes[i].Name != "project")
					continue;
				XmlNode parent;
				int nRet = this.DeleteProject(
					this.GetNodePathName(nodes[i]),
					false,
					out parent,
					out strError);
				if (nRet == -1) 
				{
					// return -1;
				}
			}

			parentXmlNode = node.ParentNode;
			parentXmlNode.RemoveChild(node);

			m_bChanged = true;

			return 1;
		}

		// ��λProject XmlNode�ڵ�
		public XmlNode LocateProjectNode(string strProjectNamePath)
		{
            if (this.dom == null || this.dom.DocumentElement == null)
                return null;    // 2011/10/5

			string[] aName = strProjectNamePath.Split(new Char [] {'/'});

			string strXPath = "";

			for(int i=0;i<aName.Length;i++) 
			{
				if (i != aName.Length - 1) 
				{
					strXPath += "dir[@name='" + aName[i] + "']";
					if (strXPath != "")
						strXPath += "/";
				}
				else
					strXPath += "project[@name='" + aName[i] + "']";

			}

			return dom.DocumentElement.SelectSingleNode(strXPath);
		}

		// �޸�project����
		public int ChangeProjectData(string strProjectNamePath,
			string strProjectName,
			string strLocate,
			out string strError)
		{
			strError = "";
			XmlNode node = LocateProjectNode(strProjectNamePath);

			if (node == null) 
			{
				strError = "���� '" + strProjectNamePath + "' ������...";
				return -1;
			}

			if (strProjectName != null) 
			{
				DomUtil.SetAttr(node, "name", strProjectName);
				Changed = true;
			}

			if (strLocate != null)
			{
                // 2007/1/24 new add
                strLocate = MacroPath(strLocate);

				DomUtil.SetAttr(node, "locate", strLocate);
				Changed = true;
			}

			return 0;
		}

		// �������ķ���"����·��"��,����·������
		public static void SplitProjectPathName(string strProjectNamePath,
			out string strPath,
			out string strName)
		{

			int nRet = strProjectNamePath.LastIndexOf("/");
			if (nRet == -1) 
			{
				strName = strProjectNamePath;
				strPath = "";
			}
			else 
			{
				strName = strProjectNamePath.Substring(nRet+1);
				strPath = strProjectNamePath.Substring(0,nRet);
			}
		}

		// ����·�������ֲ��ֹ��������ķ�������·��
		public static string MakeProjectPathName(string strPath,
			string strName)
		{
			if (strPath == "")
				return strName;
			return strPath + "/" + strName;
		}

        // ��װ��İ汾
        // ��÷�������
        // parameters:
        //      strProjectNamePath	������������·��
        //      strLastModified     ���ط�������޸�ʱ�䡣RFC1123��ʽ
        // return:
        //		-1	error
        //		0	not found project
        //		1	found
        public int GetProjectData(string strProjectNamePath,
            out string strProjectLocate)
        {
            string strLastModified = "";
            return GetProjectData(strProjectNamePath,
                out strProjectLocate,
                out strLastModified);
        }

		// ��÷�������
        // parameters:
		//      strProjectNamePath	������������·��
        //      strLastModified     ���ط�������޸�ʱ�䡣RFC1123��ʽ
		// return:
		//		-1	error
		//		0	not found project
		//		1	found
		public int GetProjectData(string strProjectNamePath,
			out string strProjectLocate,
            out string strLastModified)
		{
			//saRef = null;
			strProjectLocate = "";
            strLastModified = "";

			XmlNode node = LocateProjectNode(strProjectNamePath);

			if (node == null)
				return 0;

			strProjectLocate = DomUtil.GetAttr(node, "locate");

            // 2011/11/5
            strLastModified = DomUtil.GetAttr(node, "lastModified");

            // 2007/1/24 new add
            strProjectLocate = UnMacroPath(strProjectLocate);

			return 1;
		}

        // return:
        //		-1	error
        //		0	not found project
        //		1	found and set
        public int SetProjectData(string strProjectNamePath,
            string strLastModified)
        {
            XmlNode node = LocateProjectNode(strProjectNamePath);

            if (node == null)
                return 0;

            DomUtil.SetAttr(node, "lastModified",
                strLastModified);

            this.m_bChanged = true;
            return 1;
        }

        // ���(һ���Ѿ���װ��)����Ԫ����
        // parameters:
        //      dom ����Ԫ����XMLDOM
        // return:
        //      -1  ����
        //      0   û���ҵ�Ԫ�����ļ�
        //      1   �ɹ�
        public static int GetProjectMetadata(string strLocate,
            out XmlDocument dom,
            out string strError)
        {
            strError = "";
            dom = null;

            string strFilePath = PathUtil.MergePath(strLocate, "metadata.xml");

            dom = new XmlDocument();
            try
            {
                dom.Load(strFilePath);
            }
            catch (FileNotFoundException ex)
            {
                strError = "Ԫ�����ļ� " + strFilePath + " ������";
                return 0;
            }
            catch (Exception ex)
            {
                strError = "װ��XML�ļ� "+strFilePath+" ��DOMʱ����: " + ex.Message;
                return -1;
            }

            return 1;
        }

		/*
		// ��÷�������
		// strProjectNamePath	������������·��
		// return:
		//		-1	error
		//		0	not found project
		//		1	found
		public int GetProjectData(string strProjectNamePath,
			out string strCodeFileName,
			out string [] saRef)
		{
			saRef = null;
			strCodeFileName = "";


			XmlNode node = LocateProjectNode(strProjectNamePath);

			if (node == null)
				return 0;

			strCodeFileName = DomUtil.GetAttr(node, "codeFileName");

			string strRef = DomUtil.GetAttr(node, "references");

			saRef = strRef.Split(new Char [] {','});

			return 1;
		}
		*/

		// ɾ��һ������
		// return:
		//	-1	error
		//	0	not found
		//	1	found and deleted
		//	2	canceld	���projectû�б�ɾ��
		public int DeleteProject(string strProjectNamePath,
			bool bWarning,
			out XmlNode parentXmlNode,
			out string strError)
		{
			strError = "";
			string strProjectLocate;
			parentXmlNode = null;

			XmlNode nodeThis = LocateProjectNode(strProjectNamePath);

			if (nodeThis == null)
				return 0;

			strProjectLocate = DomUtil.GetAttr(nodeThis, "locate");

            // 2007/1/24 new add
            strProjectLocate = UnMacroPath(strProjectLocate);


			// ��Dom��ɾ���ڵ�
			parentXmlNode = nodeThis.ParentNode;
			parentXmlNode.RemoveChild(nodeThis);
			m_bChanged = true;

			if (strProjectLocate != "") // ɾ��Ŀ¼
			{
				try 
				{
					Directory.Delete(strProjectLocate, true);
				}
				catch (Exception ex) 
				{
					strError = ex.Message;
					//return -1; // ��Ĭ
				}
			}

			return 1;
		}


		/*
		// ɾ��һ������
		// return:
		//	0	not found
		//	1	found and deleted
		//	2	canceld	���projectû�б�ɾ��
		public int DeleteProject(string strProjectNamePath,
			bool bWarning,
			out XmlNode parentXmlNode)
		{
			string strCodeFileName;
			bool bDeleteCodeFile = true;
			parentXmlNode = null;

			XmlNode nodeThis = LocateProjectNode(strProjectNamePath);

			if (nodeThis == null)
				return 0;

			strCodeFileName = DomUtil.GetAttr(nodeThis, "codeFileName");

			if (strCodeFileName != "") 
			{

				// ����Դ�ļ��Ƿ񱻶��project����
				ArrayList aFound = new ArrayList();
				XmlNodeList nodes = dom.DocumentElement.SelectNodes("//project");
				for(int i=0;i<nodes.Count;i++) 
				{
					string strFilePath = DomUtil.GetAttr(nodes[i], "codeFileName");

					if (String.Compare(strCodeFileName, strFilePath, true) == 0)
					{
						if (nodes[i] != nodeThis)
							aFound.Add(nodes[i]);
					}
				}

				if (aFound.Count > 0) 
				{
					if (bWarning == true) 
					{
						string strText = "ϵͳ���֣�Դ�����ļ� "
							+ strCodeFileName 
							+ " ���˱�������ɾ���ķ��� "+
							strProjectNamePath 
							+" ʹ���⣬�������з������ã�\r\n---\r\n";

						for(int i=0;i<aFound.Count;i++)
						{
							strText += GetNodePathName((XmlNode)aFound[i]) + "\r\n";
						}

						strText += "---\r\n\r\n����ζ�ţ����ɾ�����Դ�����ļ������������������������С�\r\n\r\n���ʣ���ɾ������" +strProjectNamePath+ "ʱ���Ƿ���Դ�����ļ� " + strCodeFileName + "?";

						DialogResult msgResult = MessageBox.Show(//this,
							strText,
							"script",
							MessageBoxButtons.YesNoCancel,
							MessageBoxIcon.Question,
							MessageBoxDefaultButton.Button1);

						if (msgResult == DialogResult.Yes)
							bDeleteCodeFile = false;
						if (msgResult == DialogResult.Cancel)
							return 2;	// cancel
					}
					else 
					{
						bDeleteCodeFile = false;	// �Զ�������յķ�ʽ
					}

				}
			}

			// ��Dom��ɾ���ڵ�
			parentXmlNode = nodeThis.ParentNode;
			parentXmlNode.RemoveChild(nodeThis);

			if (bDeleteCodeFile == true
				&& strCodeFileName != "")
				File.Delete(strCodeFileName);

			m_bChanged = true;

			return 1;
		}
		*/


		// �����ƶ��ڵ�
		// return:
		//	0	not found
		//	1	found and moved
		//	2	cant move
		public int MoveNode(string strNodeNamePath,
			bool bUp,
			out XmlNode parentXmlNode)
		{
			parentXmlNode = null;

			XmlNode nodeThis = LocateAnyNode(strNodeNamePath);

			if (nodeThis == null)
				return 0;

			XmlNode nodeInsert = null;
			
			
			if (bUp == true) 
			{
				nodeInsert = nodeThis.PreviousSibling;
				if (nodeInsert == null)
					return 2;
			}
			else 
			{
				nodeInsert = nodeThis.NextSibling;
				if (nodeInsert == null)
					return 2;
			}

			// ��Dom��ɾ���ڵ�
			parentXmlNode = nodeThis.ParentNode;
			parentXmlNode.RemoveChild(nodeThis);

			// ���뵽�ض�λ��
			if (bUp == true) 
			{
				parentXmlNode.InsertBefore(nodeThis, nodeInsert);
			}
			else 
			{
				parentXmlNode.InsertAfter(nodeThis, nodeInsert);
			}

			m_bChanged = true;

			return 1;
		}

		// ����project��dir XmlNode�õ�·����
		public string GetNodePathName(XmlNode nodeThis)
		{
			string strResult = "";

			strResult = DomUtil.GetAttr(nodeThis, "name");
			XmlNode node = nodeThis.ParentNode;

			while(node != null) 
			{
				if (node == dom.DocumentElement)
					break;
				strResult = DomUtil.GetAttr(node, "name") + "/" + strResult;

				node = node.ParentNode;
			}

			return strResult;
		}


		#region ��Assembly�йصļ�������

		/*
		 *
			string[] refs = new String[] {"System.dll",
											 "System.Xml.dll",
											 @"bin\DigitalPlatform.UI.dll",
											 @"bin\DigitalPlatform.Public.dll",
											 @"bin\DigitalPlatform.Xml_r.dll",
											 @"bin\DigitalPlatform.rms.db.dll"}; //�����õ�dll
		  
		 * 
		 */


		// ����Assembly
		// parameters:
		//	strCode:	�ű�����
		//	refs:	���ӵ��ⲿassembly
		// strResult:������Ϣ
		// objDb:���ݿ�����ڳ����getErrorInfo�õ�
		// ����ֵ:�����õ�Assembly
		public static Assembly CreateAssembly(string strCode,
			string[] refs,
			string strLibPaths,
			string strOutputFile,
			out string strErrorInfo,
			out string strWarningInfo)
		{
			// System.Reflection.Assembly compiledAssembly = null;
			strErrorInfo = "";
			strWarningInfo = "";
 
			// CompilerParameters����
			System.CodeDom.Compiler.CompilerParameters compilerParams;
			compilerParams = new CompilerParameters();

			compilerParams.GenerateInMemory = true; //Assembly is created in memory
			compilerParams.IncludeDebugInformation = true;  // 2007/1/15 new add

			if (strOutputFile != null && strOutputFile != "") 
			{
				compilerParams.GenerateExecutable = false;
				compilerParams.OutputAssembly = strOutputFile;
				// compilerParams.CompilerOptions = "/t:library";
			}

			if (strLibPaths != null && strLibPaths != "")	// bug
				compilerParams.CompilerOptions = "/lib:" + strLibPaths;

			compilerParams.TreatWarningsAsErrors = false;
			compilerParams.WarningLevel = 4;
 
			// ���滯·����ȥ������ĺ��ַ���
			RemoveRefsBinDirMacro(ref refs);

			compilerParams.ReferencedAssemblies.AddRange(refs);


			CSharpCodeProvider provider;

			// System.CodeDom.Compiler.ICodeCompiler compiler;
			System.CodeDom.Compiler.CompilerResults results = null;
			try 
			{
				provider = new CSharpCodeProvider();
				// compiler = provider.CreateCompiler();
                results = provider.CompileAssemblyFromSource(
					compilerParams, 
					strCode);
			}
			catch (Exception ex) 
			{
				strErrorInfo = "���� " + ex.Message;
				return null;
			}

			int nErrorCount = 0;

			if (results.Errors.Count != 0) 
			{
				string strErrorString = "";
				nErrorCount = getErrorInfo(results.Errors,
					out strErrorString);

				strErrorInfo = "��Ϣ����:" + Convert.ToString(results.Errors.Count) + "\r\n";
				strErrorInfo += strErrorString;

				if (nErrorCount == 0 && results.Errors.Count != 0) 
				{
					strWarningInfo = strErrorInfo;
					strErrorInfo = "";
				}
			}

			if (nErrorCount != 0)
				return null;

 
			return results.CompiledAssembly;
		}

		int GetErrorCount(CompilerErrorCollection errors)
		{
			int nCount = 0;
			foreach(CompilerError oneError in errors)
			{
				if (oneError.IsWarning == false)
					nCount ++;
			}

			return nCount;
		}

		// ȥ��·���еĺ�%projectdir%
		void RemoveRefsProjectDirMacro(ref string[] refs,
			string strProjectDir)
		{
            if (refs == null)
                return; // 2008/1/13 new add

			Hashtable macroTable = new Hashtable();

			macroTable.Add("%projectdir%", strProjectDir);

			for(int i=0;i<refs.Length;i++) 
			{
				string strNew = PathUtil.UnMacroPath(macroTable,
					refs[i], 
					false);	// ��Ҫ�׳��쳣����Ϊ���ܻ���%binddir%�����ڻ��޷��滻
				refs[i] = strNew;
			}

		}

	
		// ȥ��·���еĺ�%bindir%
		public static void RemoveRefsBinDirMacro(ref string[] refs)
		{
            if (refs == null)
                return; // 2008/1/13 new add

			Hashtable macroTable = new Hashtable();

			macroTable.Add("%bindir%", Environment.CurrentDirectory);

			for(int i=0;i<refs.Length;i++) 
			{
				string strNew = PathUtil.UnMacroPath(macroTable,
					refs[i],
					true);
				refs[i] = strNew;
			}

		}

		// parameters:
		//		refs	���ӵ�refs�ļ�·����·���п��ܰ�����%installdir%
		public static int CreateAssemblyFile(string strCode,
			string[] refs,
			string strLibPaths,
			string strOutputFile,
			out string strErrorInfo,
			out string strWarningInfo)
		{
			// System.Reflection.Assembly compiledAssembly = null;
			strErrorInfo = "";
			strWarningInfo = "";
 
			// CompilerParameters����
			System.CodeDom.Compiler.CompilerParameters compilerParams;
			compilerParams = new CompilerParameters();

			compilerParams.GenerateInMemory = true; //Assembly is created in memory
			compilerParams.IncludeDebugInformation = true;

			if (strOutputFile != null && strOutputFile != "") 
			{
				compilerParams.GenerateExecutable = false;
				compilerParams.OutputAssembly = strOutputFile;
				// compilerParams.CompilerOptions = "/t:library";
			}

			if (strLibPaths != null && strLibPaths != "")	// bug
				compilerParams.CompilerOptions = "/lib:" + strLibPaths;

			compilerParams.TreatWarningsAsErrors = false;
			compilerParams.WarningLevel = 4;
 
			// ���滯·����ȥ������ĺ��ַ���
			RemoveRefsBinDirMacro(ref refs);

            if (refs != null)
			    compilerParams.ReferencedAssemblies.AddRange(refs);


			CSharpCodeProvider provider;

			// System.CodeDom.Compiler.ICodeCompiler compiler;
			System.CodeDom.Compiler.CompilerResults results = null;
			try 
			{
				provider = new CSharpCodeProvider();
				// compiler = provider.CreateCompiler();
                results = provider.CompileAssemblyFromSource(
					compilerParams, 
					strCode);
			}
			catch (Exception ex) 
			{
				strErrorInfo = "���� " + ex.Message;
				return -1;
			}

			int nErrorCount = 0;

			if (results.Errors.Count != 0) 
			{
				string strErrorString = "";
				nErrorCount = getErrorInfo(results.Errors,
					out strErrorString);

				strErrorInfo = "��Ϣ����:" + Convert.ToString(results.Errors.Count) + "\r\n";
				strErrorInfo += strErrorString;

				if (nErrorCount == 0 && results.Errors.Count != 0) 
				{
					strWarningInfo = strErrorInfo;
					strErrorInfo = "";
				}
			}

			if (nErrorCount != 0)
				return -1;

 
			return 0;
		}

        // �� .ref ��ȡ���ӵĿ��ļ�·��
        public static int GetRef(string strCsFileName,
            ref string[] refs,
            out string strError)
        {
            strError = "";

            string strRefFileName = strCsFileName + ".ref";

            // .ref�ļ�����ȱʡ
            if (File.Exists(strRefFileName) == false)
                return 0;   // .ref �ļ�������

            string strRef = "";
            try
            {
                using (StreamReader sr = new StreamReader(strRefFileName, true))
                {
                    strRef = sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            // ��ǰ���
            string[] add_refs = null;
            int nRet = ScriptManager.GetRefsFromXml(strRef,
                out add_refs,
                out strError);
            if (nRet == -1)
            {
                strError = strRefFileName + " �ļ�����(ӦΪXML��ʽ)��ʽ����: " + strError;
                return -1;
            }

            // ���ֺ�
            if (add_refs != null)
            {
                for (int i = 0; i < add_refs.Length; i++)
                {
                    add_refs[i] = add_refs[i].Replace("%bindir%", Environment.CurrentDirectory);
                }
            }

            refs = StringUtil.Append(refs, add_refs);
            return 1;
        }

        // (��refs�еĺ겻���Դ���)
        // ֱ�ӱ��뵽�ڴ�
        // parameters:
        //		refs	���ӵ�refs�ļ�·������������·���п��ܰ�����%...%δ���Դ�����Ҫ�ں�������ǰ�ȴ����
        public static int CreateAssembly_1(string strCode,
            string[] refs,
            string strLibPaths,
            // AppDomain appDomain,
            out Assembly assembly,
            out string strErrorInfo,
            out string strWarningInfo)
        {
            assembly = null;
            strErrorInfo = "";
            strWarningInfo = "";

            // CompilerParameters����
            System.CodeDom.Compiler.CompilerParameters compilerParams;
            compilerParams = new CompilerParameters();

            compilerParams.GenerateInMemory = true; //Assembly is created in memory
            compilerParams.IncludeDebugInformation = true;

            if (String.IsNullOrEmpty(strLibPaths) == false)
                compilerParams.CompilerOptions = "/lib:" + strLibPaths;

            compilerParams.TreatWarningsAsErrors = false;
            compilerParams.WarningLevel = 4;


            /*
            // 2007/12/4 �ſ�ע��
            // ���滯·����ȥ������ĺ��ַ���
            RemoveRefsBinDirMacro(ref refs);
             * */

            compilerParams.ReferencedAssemblies.AddRange(refs);


            CSharpCodeProvider provider;

            System.CodeDom.Compiler.CompilerResults results = null;
            try
            {
                Dictionary<string, string> options = new Dictionary<string, string>
                {
                {"CompilerVersion","v3.5"}
                };
                provider = new CSharpCodeProvider();
                // compiler = provider.CreateCompiler();
                results = provider.CompileAssemblyFromSource(
                    compilerParams,
                    strCode);
            }
            catch (Exception ex)
            {
                strErrorInfo = "���� " + ex.Message;
                return -1;
            }

            int nErrorCount = 0;

            if (results.Errors.Count != 0)
            {
                string strErrorString = "";
                nErrorCount = getErrorInfo(results.Errors,
                    out strErrorString);

                strErrorInfo = "��Ϣ����:" + Convert.ToString(results.Errors.Count) + "\r\n";
                strErrorInfo += strErrorString;

                if (nErrorCount == 0 && results.Errors.Count != 0)
                {
                    strWarningInfo = strErrorInfo;
                    strErrorInfo = "";
                }
            }

            if (nErrorCount != 0)
                return -1;

            assembly = results.CompiledAssembly;

            return 0;
        }

		// ���������Ϣ�ַ���
		public static int getErrorInfo(CompilerErrorCollection errors,
			out string strResult)
		{
			strResult = "";
			int nCount = 0;

			if (errors == null)
			{
				strResult = "error����Ϊnull";
				return 0;
			}
   
 
			foreach(CompilerError oneError in errors)
			{
				strResult += "(" + Convert.ToString(oneError.Line) + "," + Convert.ToString(oneError.Column) + ") ";
				strResult += (oneError.IsWarning) ? "warning " : "error ";
				strResult += oneError.ErrorNumber + " ";
				strResult += ": " + oneError.ErrorText + "\r\n";

				if (oneError.IsWarning == false)
					nCount ++;

			}
			return nCount;
		}

		public static Type GetDerivedClassType(Assembly assembly,
			string strBaseTypeFullName)
		{
            if (assembly == null)
                return null;

			Type[] types = assembly.GetTypes();
			// string strText = "";

			for(int i=0;i<types.Length;i++) 
			{
				if (types[i].IsClass == false)
					continue;
				if (IsDeriverdFrom(types[i],
					strBaseTypeFullName) == true)
					return types[i];
			}


			return null;
		}
 

	    public string GetDebugInfo(Assembly assembly)
		{
			Type[] types = assembly.GetTypes();
			string strText = "";

			for(int i=0;i<types.Length;i++) 
			{
				strText += "Name:" + types[i].Name + "\r\n";
				strText += "baseClasses:" + GetBaseClasses(types[i]) + "\r\n";
				strText += "---\r\n";
			}


			return strText;
		}

		string GetBaseClasses(Type type)
		{
			// base type
			// StringCollection names = new StringCollection();

			string strText = "";

			Type curType = type;
			for(;;) 
			{
				if (curType == null 
					|| curType.Name == "Object")
					break;

				if (strText != "")
					strText += ",";

				strText += curType.FullName;	//curType.Namespace + "|" + curType.Name;
				curType = curType.BaseType;
			}

			return strText;
		}


		// �۲�type�Ļ������Ƿ�������ΪstrBaseTypeFullName���ࡣ
		public static bool IsDeriverdFrom(Type type,
			string strBaseTypeFullName)
		{
			Type curType = type;
			for(;;) 
			{
				if (curType == null 
					|| curType.FullName == "System.Object")
					return false;

				if (curType.FullName == strBaseTypeFullName)
					return true;

				curType = curType.BaseType;
			}

		}

		#endregion


		// *** �㷨���ԸĽ�һ��:�������ԭ����������󲿷�������,�����ۼ�������ֲ���
		// ���һ���µ�Project����Ŀ¼����
		// Ҫ���㣺1) �ڴ����ϲ�����
		// һ��������Ŀ¼�������ȴ���һ����Ŀ¼���Ա�ռ�����Ŀ¼����
		// ���ⱻ�����ظ���á����ǣ��Ժ������ʹ�����Ŀ¼���ǵ�ɾ��������Ȼ
		// ��Ϊ�������������Ŀ¼
		public string NewProjectLocate(string strPrefix,
			ref int nPrefixNumber)
		{

			// �滻strPrefix�е�ĳЩ�ַ�
			strPrefix = strPrefix.Replace(" ", "_");


			string strNewPath = "";
				
			strNewPath = this.DefaultCodeFileDir
				+ "\\"
				+ strPrefix;
			int i;
			bool bFound = false;


			// ��̽����Ŀ¼��ͬ���ļ�
			PathUtil.CreateDirIfNeed(this.DefaultCodeFileDir);
			DirectoryInfo di = new DirectoryInfo(this.DefaultCodeFileDir);

			FileSystemInfo[] fis = di.GetFileSystemInfos();

			for(;;nPrefixNumber ++) 
			{
				string strName = "";
				
				if (nPrefixNumber == -1) 
				{
					strName = strPrefix;
				}
				else 
				{
					strName = strPrefix + Convert.ToString(nPrefixNumber);
				}

				bFound = false;
				for(i=0;i<fis.Length;i++) 
				{
					string strExistName = fis[i].Name;

					if (String.Compare(strName, strExistName, true) == 0) 
					{
						bFound = true;
						break;
					}
				}
				if (bFound == false)
					break;
			}

			if (nPrefixNumber == -1) 
			{
				strNewPath = this.DefaultCodeFileDir
					+ "\\"
					+ strPrefix;
			}
			else 
			{
				strNewPath = this.DefaultCodeFileDir
					+ "\\"
					+ strPrefix + Convert.ToString(nPrefixNumber);
			}

			PathUtil.CreateDirIfNeed(strNewPath);

			return strNewPath;
		}


		// ���һ���µ�Դ�����ļ�����
		// Ҫ���㣺1) ������projectԪ�ص�codeFileName�����в��ظ�
		//		2) �ڴ����ϲ�����
		// һ���������ļ��������ȴ���һ��0byte�����ļ����Ա�ռ������ļ�����
		// ���ⱻ�����ظ���á����ǣ��Ժ������ʹ������ļ����ǵ�ɾ��������Ȼ
		// ��Ϊ�������������
		public string NewCodeFileName(string strPrefix,
			string strExt,	// �ļ���չ������'.'
			ref int nPrefixNumber)
		{

			// �滻strPrefix�е�ĳЩ�ַ�
			strPrefix = strPrefix.Replace(" ", "_");


			string strNewPath = this.DefaultCodeFileDir
				+ "\\"
				+ strPrefix;
			int i;
			bool bFound = false;

			// ��̽����codeFileName����
			XmlNodeList nodes = dom.DocumentElement.SelectNodes("//project");

			for(;;nPrefixNumber ++) 
			{
				strNewPath = this.DefaultCodeFileDir + "\\" + 
					strPrefix + Convert.ToString(nPrefixNumber) + strExt;

				bFound = false;
				for(i = 0;i<nodes.Count;i++)
				{
					string strExistPath = DomUtil.GetAttr(nodes[i],
						"codeFileName");

					if (strExistPath == "")
						continue;

					if (String.Compare(strExistPath, strNewPath) == 0) 
					{
						bFound = true;
						break;
					}
				}

				if (bFound == false)
					break;
			}


			// ��̽����Ŀ¼��ͬ���ļ�
			PathUtil.CreateDirIfNeed(this.DefaultCodeFileDir);
			DirectoryInfo di = new DirectoryInfo(this.DefaultCodeFileDir);

			FileInfo[] fis = di.GetFiles();

			for(;;nPrefixNumber ++) 
			{
				string strName = strPrefix + Convert.ToString(nPrefixNumber) + strExt;

				bFound = false;
				for(i=0;i<fis.Length;i++) 
				{
					string strExistName = fis[i].Name;


					if (String.Compare(strName, strExistName, true) == 0) 
					{
						bFound = true;
						break;
					}
				}
				if (bFound == false)
					break;
			}

			return strNewPath;
		}

		// ����һ���µ�Project XmlNode
        // parameters:
        //      bCreateDefault  �Ƿ�Ҫ����ȱʡ���ļ�
        //      strHostName     ����������bCreateDefault == trueʱ��������
		public XmlNode NewProjectNode(string strProjectNamePath,
			string strLocate,
			bool bCreateDefault,
            string strHostName = "")
		{
#if DEBUG
            if (bCreateDefault == true
                && string.IsNullOrEmpty(strHostName) == true)
            {
                Debug.Assert(false, "������ bCreateDefault Ϊtrue��ʱ�� strHostName����Ϊ��");
            }
#endif

			string[] aName = strProjectNamePath.Split(new Char [] {'/'});

			string strXPath = "";

			XmlNode nodeCurrent = dom.DocumentElement;

			XmlNode projectNode = null;

			// string strRef = String.Join(",", saRef);

			for(int i=0;i<aName.Length;i++) 
			{
				if (i != aName.Length - 1) 
				{
					strXPath = "dir[@name='" + aName[i] + "']";

					XmlNode node = nodeCurrent.SelectSingleNode(strXPath);

					if (node == null) // �������¼�Ŀ¼
					{
						node = dom.CreateElement("dir");
						nodeCurrent.AppendChild(node);
						DomUtil.SetAttr(node, "name", aName[i]);
					}

					nodeCurrent = node;
				}
				else 
				{
					strXPath = "project[@name='" + aName[i] + "']";

					XmlNode node = nodeCurrent.SelectSingleNode(strXPath);

					if (node == null) // �������¼�Ŀ¼
					{
						node = dom.CreateElement("project");
						nodeCurrent.AppendChild(node);
						DomUtil.SetAttr(node, "name", aName[i]);

                        // 2007/1/24 new add
                        strLocate = MacroPath(strLocate);

						DomUtil.SetAttr(node, "locate", strLocate);

						
						// ��������Ŀ¼ 
						if (string.IsNullOrEmpty(strLocate) == false
                            && bCreateDefault == true) 
						{

							CreateDefault(strLocate, strHostName);
							/*
							Directory.CreateDirectory(strLocate);

							// ����ȱʡ�ļ�

							// main.cs
							string strFileName = strLocate + "\\main.cs";

							CreateDefaultMainCsFile(strFileName);

							// reference.xml
							strFileName = strLocate + "\\references.xml";

							CreateDefaultReferenceXmlFile(strFileName);
							*/
						}


					}

					projectNode = node;

					nodeCurrent = node;
				}
			}

			m_bChanged = true;
			return projectNode;
		}

		public void OnCreateDefaultContent(string strFileName)
		{
			// ����ȱʡ�ļ�
			if (this.CreateDefaultContent != null)
			{
				CreateDefaultContentEventArgs e = new CreateDefaultContentEventArgs();
				e.FileName = strFileName;
				this.CreateDefaultContent(this, e);
				if (e.Created == false)
					CreateBlankContent(strFileName);
			}
			else 
			{
				// ����һ�����ļ�
				CreateBlankContent(strFileName);
			}
		}

		// ����һ������Ŀ¼�е�����ȱʡ�ļ�
		public int CreateDefault(string strLocate,
            string strHostName)
		{
			// ��������Ŀ¼ 
			if (strLocate == "")
				return -1;

			Directory.CreateDirectory(strLocate);

			// ����ȱʡ�ļ�
			if (this.CreateDefaultContent != null)
			{

				// main.cs
				string strFileName = strLocate + "\\main.cs";

				// CreateDefaultMainCsFile(strFileName);
				CreateDefaultContentEventArgs e = new CreateDefaultContentEventArgs();
				e.FileName = strFileName;
				this.CreateDefaultContent(this, e);
				if (e.Created == false)
					CreateBlankContent(strFileName);

				// reference.xml
				strFileName = strLocate + "\\references.xml";

				CreateDefaultReferenceXmlFile(strFileName);

                // metadata.xml
                strFileName = strLocate + "\\metadata.xml";

                CreateDefaultMetadataXmlFile(strFileName, strHostName);
			}

			return 0;
		}

		public void CreateBlankContent(string strFileName)
		{
			StreamWriter sw = new StreamWriter(strFileName);
			sw.WriteLine("");
			sw.Close();
		}
	

		// ����ȱʡ��references.xml�ļ�
        // TODO: Ӧ�޸�Ϊ�¼�����
		public static int CreateDefaultReferenceXmlFile(string strFileName)
		{
			string strError = "";
			//string strExe = Environment.CurrentDirectory + "\\dp1batch.exe";
			//string strDll = Environment.CurrentDirectory + "\\digitalplatform.marcdom.dll";
			string [] saRef = {"system.dll", "system.windows.forms.dll", "system.xml.dll"/*, strExe, strDll*/};
			SaveRefs(strFileName, saRef, out strError);

			return 0;
		}

        // ����ȱʡ��metadata.xml�ļ�
        // TODO: Ӧ�޸�Ϊ�¼�����
        public static int CreateDefaultMetadataXmlFile(string strFileName,
            string strHostName)
        {
            string strError = "";
            SaveMetadata(strFileName, 
                "",
                strHostName,
                out strError);

            return 0;
        }

		// �Ƿ�Ϊȱʡ�ļ���?
		// strFileName	���ļ���
		public static bool IsReservedFileName(string strFileName)
		{
			if (String.Compare(strFileName,
				"main.cs", true) == 0)
				return true;
			if (String.Compare(strFileName,
				"references.xml", true) == 0)
				return true;
			if (String.Compare(strFileName,
				"marcfilter.fltx", true) == 0)
				return true;

			return false;
		}

		/*
		public XmlNode NewProjectNode(string strProjectNamePath,
			string strCodeFileName,
			string[] saRef)
		{
			string[] aName = strProjectNamePath.Split(new Char [] {'/'});

			string strXPath = "";

			XmlNode nodeCurrent = dom.DocumentElement;

			XmlNode projectNode = null;

			string strRef = String.Join(",", saRef);

			for(int i=0;i<aName.Length;i++) 
			{
				if (i != aName.Length - 1) 
				{
					strXPath = "dir[@name='" + aName[i] + "']";

					XmlNode node = nodeCurrent.SelectSingleNode(strXPath);

					if (node == null) // �������¼�Ŀ¼
					{
						node = dom.CreateElement("dir");
						nodeCurrent.AppendChild(node);
						DomUtil.SetAttr(node, "name", aName[i]);
					}

					nodeCurrent = node;

				}
				else 
				{
					strXPath = "project[@name='" + aName[i] + "']";

					XmlNode node = nodeCurrent.SelectSingleNode(strXPath);

					if (node == null) // �������¼�Ŀ¼
					{
						node = dom.CreateElement("project");
						nodeCurrent.AppendChild(node);
						DomUtil.SetAttr(node, "name", aName[i]);

						DomUtil.SetAttr(node, "codeFileName", strCodeFileName);

						DomUtil.SetAttr(node, "references", strRef);
					}

					projectNode = node;

					nodeCurrent = node;

				}

			}

			m_bChanged = true;

			return projectNode;
		}
		*/

		// ����һ���µ�Dir xml�ڵ�
		public XmlNode NewDirNode(string strDirNamePath)
		{
			string[] aName = strDirNamePath.Split(new Char [] {'/'});

			string strXPath = "";

			XmlNode nodeCurrent = dom.DocumentElement;

			XmlNode dirNode = null;

			for(int i=0;i<aName.Length;i++) 
			{
				if (i != aName.Length - 1) 
				{
					strXPath = "dir[@name='" + aName[i] + "']";

					XmlNode node = nodeCurrent.SelectSingleNode(strXPath);

					if (node == null) // �������¼�Ŀ¼
					{
						node = dom.CreateElement("dir");
						nodeCurrent.AppendChild(node);
						DomUtil.SetAttr(node, "name", aName[i]);
					}

					nodeCurrent = node;

				}
				else 
				{
					strXPath = "dir[@name='" + aName[i] + "']";

					XmlNode node = nodeCurrent.SelectSingleNode(strXPath);

					if (node == null) // �������¼�Ŀ¼
					{
						node = dom.CreateElement("dir");
						nodeCurrent.AppendChild(node);
						DomUtil.SetAttr(node, "name", aName[i]);
					}

					dirNode = node;

					nodeCurrent = node;

				}

			}

			m_bChanged = true;

			return dirNode;
		}

		// ����һ��ȱʡ��projects.xml�ļ�
		// parameters:
		//		strDefaultCodeFileSubDir	ȱʡ�Ĵ�����Ŀ¼��һ��Ϊclientcfgs
		public static int CreateDefaultProjectsXmlFile(string strFileName,
			string strDefaultCodeFileSubDir)
		{
			StreamWriter sw = new StreamWriter(strFileName, false, Encoding.UTF8);

			sw.WriteLine("<?xml version='1.0' encoding='utf-8'?>");
			sw.WriteLine("<dir defaultCodeFileDir='" +strDefaultCodeFileSubDir+ "'>");
			sw.WriteLine("</dir>");
			sw.Close();

			return 0;
		}
	}

	// �����ļ�ȱʡ����
	public delegate void CreateDefaultContentEventHandler(object sender,
	CreateDefaultContentEventArgs e);

	public class CreateDefaultContentEventArgs: EventArgs
	{
		public string FileName = "";
		public bool Created = false;
	}

    public class ProjectInstallInfo
    {
        public string ProjectPath = ""; // projpack �ļ�ȫ·��
        public string ProjectName = "";
        public int IndexInPack = -1;    // ��projpack�ļ��е��±�
        public string Host = "";
        public string UpdateUrl = "";

    }
}

