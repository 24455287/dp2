using System;
using System.IO;
using System.Collections;
using System.Diagnostics;

using DigitalPlatform.IO;
using DigitalPlatform.Text;


namespace DigitalPlatform.rms.Client
{
	/// <summary>
	/// ���ݿ����ö�������ǰ�˴洢�͹���
	/// </summary>
	[Serializable()]
	public class DatabaseObject
	{
		public DatabaseObject Parent = null;
		public ArrayList Children = new ArrayList();
		public string Name = "";
		public int Type = -1;	// -1 Ϊδ����
        public int Style = 0;   // 0Ϊδ����
		public byte[] Content = null;	// �ļ�����
		public byte[] TimeStamp = null;	// ʱ���
		public string Metadata = "";	// Ԫ����
		public bool Changed = false;

		public static bool IsDefaultFile(string strPath)
		{
			if (strPath == "")
				return false;
			string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);

			if (String.Compare(strPath, "cfgs/browse") == 0)
				return true;
			if (String.Compare(strPath, "cfgs/keys") == 0)
				return true;

			return false;
		}

		public bool IsDefaultFile()
		{
			if (this.Parent == null)
				return false;
			if (this.Parent.Type != ResTree.RESTYPE_FOLDER)
				return false;
			if (this.Parent.Name.ToLower() != "cfgs")
				return false;
			if (this.Parent.Parent != null 
				&& !( this.Parent.Parent.Type == -1 || this.Parent.Parent.Type == ResTree.RESTYPE_DB) 
				)
				return false;
			if (this.Name.ToLower() == "keys" || this.Name.ToLower() == "browse")
				return true;
			return false;
		}

		// �������ݿ����·��
		public string MakePath(string strDbName)
		{
			string strPath = "";
			bool bOccurDb = false;

			DatabaseObject cur = this;
			while(cur != null)
			{
				if (cur.Type == -1)
				{
					Debug.Assert( cur.Parent == null , "ֻ���������type==-1");
					goto CONTINUE;
				}


				if (strPath != "")
					strPath = "/" + strPath;

			CONTINUE:
				if (cur.Type == ResTree.RESTYPE_DB)
				{
					strPath = strDbName + strPath;
					bOccurDb = true;
				}
				else
					strPath = cur.Name + strPath;

				cur = cur.Parent;
			}

			if (bOccurDb == false)
			{
				if (strPath != "")
					strPath = "/" + strPath;
				strPath = strDbName + strPath;
			}

			return strPath;
		}

		// ��¡
		public DatabaseObject Clone()
		{
			DatabaseObject newObj = new DatabaseObject();
			newObj.Name = this.Name;
			newObj.Type = this.Type;
            newObj.Style = this.Style;
			newObj.Content = ByteArray.GetCopy(this.Content);
			newObj.TimeStamp = ByteArray.GetCopy(this.TimeStamp);

			// �ݹ�
			for(int i=0;i<this.Children.Count;i++)
			{
				DatabaseObject newChild = ((DatabaseObject)this.Children[i]).Clone();
				newChild.Parent = this;
				newObj.Children.Add(newChild);
			}

			return newObj;
		}


		// dump
		public string Dump()
		{
			string strTab = "";

			DatabaseObject cur = this;
			for(;;)
			{
				if (cur.Parent == null)
					break;
				strTab += "\t";
				cur = cur.Parent;
			}

			string strText = "";

			strText += strTab + "name:" + this.Name + ", type:" + Convert.ToString(this.Type);

			if (this.Children.Count > 0)
			{
				strText += "\r\n" + strTab + "{\r\n";

				// �ݹ�
				for(int i=0;i<this.Children.Count;i++)
				{
					DatabaseObject child = (DatabaseObject)this.Children[i];
					strText += child.Dump() + "\r\n";
				}
				strText += strTab + "}";

			}

			return strText;
		}

		public DatabaseObject LocateObject(string strPath)
		{
			if (strPath == "" && this.Type == -1)
				return this;


			string[] aName = strPath.Split(new Char [] {'/'});

			if (this.Name != aName[0])
				return null;	// ��һ����ƥ��

			if (aName.Length == 1)
				return this;

			DatabaseObject curobj = this;
			for(int i=1;i<aName.Length;i++)
			{
				string strName = aName[i];

				DatabaseObject child = null;
				for(int j=0;j<curobj.Children.Count;j++)
				{
					child = (DatabaseObject)curobj.Children[j];
					if (child.Name == strName)
						goto FOUND;
				}

				return null;	// not found

			FOUND:
				curobj = child;

			}

			return curobj;
		}

		public void SetData(Stream fs)
		{
			fs.Seek(0, SeekOrigin.Begin);

			this.Content = new byte[fs.Length];

			int nRet =  fs.Read(this.Content, 0, (int)fs.Length);
			if (nRet != fs.Length) 
			{
				throw(new Exception("read stream error"));
			}
		}

		// ����һ�������ļ����͵Ķ���
		public static DatabaseObject BuildFileObject(
			string strName,
			string strFileName)
		{
			DatabaseObject obj = new DatabaseObject();

			FileStream fs = File.Open(strFileName, FileMode.Open);

			if (fs.Length > 1024*1024)
			{
				throw(new Exception("obj " +strFileName+ " too larg"));
			}

			obj.Content = new byte[fs.Length];
			obj.Name = strName;
			obj.Type = ResTree.RESTYPE_FILE;

			int nRet =  fs.Read(obj.Content, 0, (int)fs.Length);
			if (nRet != fs.Length) 
			{
				throw(new Exception("read obj " +strFileName+ " error"));
			}
			fs.Close();

			return obj;
		}

		// ����һ�������ļ����͵Ķ���
		public static DatabaseObject BuildFileObject(
			string strName,
			Stream fs)
		{
			DatabaseObject obj = new DatabaseObject();

			fs.Seek(0, SeekOrigin.Begin);

			obj.Content = new byte[fs.Length];
			obj.Name = strName;
			obj.Type = ResTree.RESTYPE_FILE;

			int nRet =  fs.Read(obj.Content, 0, (int)fs.Length);
			if (nRet != fs.Length) 
			{
				throw(new Exception("read stream error"));
			}

			return obj;
		}


		// ����һ��Ŀ¼���͵Ķ���
		public static DatabaseObject BuildDirObject(
			string strName)
		{
			DatabaseObject obj = new DatabaseObject();

			obj.Content = null;
			obj.Name = strName;
			obj.Type = ResTree.RESTYPE_FOLDER;

			return obj;
		}

	

	}

	public class ObjEventCollection : ArrayList
	{
	}

	public enum ObjEventOper
	{
		None = 0,
		New = 1,	// �´���
		Change = 2,	// �޸�����
		Delete = 3,	// ɾ��
	}

	// �����޸���־
	public class ObjEvent
	{
		public DatabaseObject Obj = null;

		public ObjEventOper Oper = ObjEventOper.None;

		public string Path = "";	// ����·��
	}
}
