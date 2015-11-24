using System;
using System.Collections;
using System.IO;
using System.Xml;

using DigitalPlatform.IO;
using DigitalPlatform.Xml;

namespace DigitalPlatform.Script
{

	// �ڴ��е�һ���ļ�
	[Serializable()]
	public class ProjectFile
	{
		public string PureName = "";	// ���ļ���

		public byte[] Content = null;	// �ļ�����

        // Exceptions:
        //      Exception   �ļ�̫��ʱ(>1024*1024)�׳����쳣
		public static ProjectFile MakeProjectFile(string strFileName)
		{
			ProjectFile file = new ProjectFile();

            file.PureName = PathUtil.PureName(strFileName);

            using (FileStream fs = File.Open(strFileName, FileMode.Open))
            {

                if (fs.Length > 1024 * 1024)
                {
                    throw (new Exception("file " + strFileName + " too larg"));
                }

                file.Content = new byte[fs.Length];

                int nRet = fs.Read(file.Content, 0, (int)fs.Length);
                if (nRet != fs.Length)
                {
                    throw (new Exception("read file " + strFileName + " error"));
                }
                return file;
            }
        }

		// д��ָ��Ŀ¼
		public void WriteToLocate(string strLocate,
            bool bOverwrite)
		{
			string strFileName = strLocate + "\\" + this.PureName;

            if (bOverwrite == true)
                File.Delete(strFileName);
            else
            {
                if (File.Exists(strFileName) == true)
                    return;
            }

            using (FileStream fs = File.Open(strFileName, FileMode.Create))
            {
                if (this.Content != null)
                {
                    fs.Write(this.Content, 0, this.Content.Length);
                }
            }
		}

        // 2011/11/25
        public MemoryStream GetMemoryStream()
        {
            MemoryStream fs = new MemoryStream();

            if (this.Content != null)
            {
                fs.Write(this.Content, 0, this.Content.Length);
            }

            fs.Seek(0, SeekOrigin.Begin);
            return fs;
        }
	}

	/// <summary>
	/// �洢�ͽ���Project��Ϣ�İ�װ��
	/// </summary>
	[Serializable()]
	public class Project : ArrayList
	{
		public string NamePath = "";	// ��Ŀ¼·��������

		public string Locate = "";		// �������ڴ���Ŀ¼

		public Project()
		{
		}

		// ����һ��Project����
		// strLocate	�������ڴ���Ŀ¼
        // Exceptions:
        //      Exception   MakeProjectFile()�����׳����쳣
		static public Project MakeProject(string strNamePath,
			string strLocate)
		{
			Project project = new Project();

			project.NamePath = strNamePath;
			project.Locate = strLocate;

			// ��Ŀ¼�������ļ�������
			DirectoryInfo di = new DirectoryInfo(strLocate);

			FileInfo[] afi = di.GetFiles();

			for(int i=0;i<afi.Length;i++)
			{
				string strFileName = afi[i].Name;
				if (strFileName.Length > 0
					&& strFileName[0] == '~')
					continue;	// ������ʱ�ļ�

				project.Add(ProjectFile.MakeProjectFile(afi[i].FullName));
			}

			return project;
		}

        // 2011/11/25
        public string GetHostName()
        {
            XmlDocument dom = this.GetMetadataDom();
            if (dom == null)
                return null;
            return DomUtil.GetAttr(dom.DocumentElement,
                "host");
        }

        public string GetUpdateUrl()
        {
            XmlDocument dom = this.GetMetadataDom();
            if (dom == null)
                return null;
            return DomUtil.GetAttr(dom.DocumentElement,
                "updateUrl");
        }

        // 2011/11/25
        public XmlDocument GetMetadataDom()
        {
            for (int i = 0; i < this.Count; i++)
            {
                ProjectFile file = (ProjectFile)this[i];

                if (file.PureName.ToLower() == "metadata.xml")
                {
                    using (Stream stream = file.GetMemoryStream())
                    {
                        XmlDocument dom = new XmlDocument();
                        dom.Load(stream);
                        return dom;
                    }
                }
            }

            return null;    // not found
        }

		public void WriteToLocate(string strLocateParam,
            bool bOverwrite)
		{
			string strLocate = null;

			if (strLocateParam != null)
				strLocate = strLocateParam;
			else
				strLocate = this.Locate;

			// �Ƿ���ɾ��Ŀ¼��ԭ����ȫ���ļ�?

			PathUtil.CreateDirIfNeed(strLocate);

			for(int i = 0;i<this.Count;i++)
			{
				ProjectFile file = (ProjectFile)this[i];

				file.WriteToLocate(strLocate, bOverwrite);
			}
		}
	}


	// Project����
	[Serializable()]
	public class ProjectCollection : ArrayList
	{

	}
}
