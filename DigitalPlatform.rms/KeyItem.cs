using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
// using System.Xml.XPath;

using DigitalPlatform.Xml;

namespace DigitalPlatform.rms
{

	//�����ͼ��
	//Ϊ��ͨ���ؼ��ּ�����¼����Ҫ��һ����¼�����йؼ�����ȡ������ŵ����ݿ�keys����.
	//��¼��ؼ�����ӵ�еĹ�ϵ�������¼ʱͬʱ��ؼ��֣�ɾ��¼ʱͬʱɾ�ؼ���
	//���ݿ���ÿ���ؼ��ְ����������ݣ�
	//keystring: ������ı�
	//fromstring: ���ڻ�û���õ�
	//idstring: ��Ӧ�����ݼ�¼ID
	//�������ݼ�¼��keys�����ļ�����DpKeys����
	//�ؼ��������ļ���������:
	// <key>
	//   <xpath>/description/title</xpath>    :ͨ��xpath������dom���ҵ��������ݴ�ŵ�keystring
	//   <from>title</from>                   :ֱ����ȡ���ݴ���fromstring
	//   <table name="title"/>                :�����ĸ���
	// </key>
	// DpKeys��ArrayList�̳У���ԱΪDpKeys�������ڴ���ؼ��ֲ��֡�
	public class KeyCollection : List<KeyItem> 
	{


		//�����ͼ:
		//���Ǽ�¼ʱ�������¼�¼�õ�һЩ�µ�key����ԭ�ɼ�¼Ҳ����һЩ�ɵ�key��
		//�����ñ��취ɾ��ԭ�ɼ�¼���е�key���������¼�¼���е�key��
		// 
		//���¾ɼ�¼������һЩ�ظ�key�����õķ��������¾ɼ�¼������key���бȽϣ�
		//����ֳ������֣�
		//1.ֻ���¼�¼���ֵ�key
		//2.ֻ�ھɼ�¼���ֵ�key
		//3.�ظ���key��
		// 
		//������ִ�и���ʱ�����ӵ�һ���֣�ɾ���ڶ����֣��ظ��ı��ֲ��䣬���Ծͽ�ʡ��ʱ��
		// 
		//ע����������ǰ��ȷ���������Ź����
		// 
		//ԭ��newKeys��oldKeys�������Ͷ���ref,����Ϊ�û��඼���������ͣ�����û������ref����
		// parameters:
		//		newKeys	�¼�¼��key����
		//		oldKeys	�ɼ�¼��key����
		// return:
		//		�ظ���key����
		public static KeyCollection Merge(KeyCollection newKeys,
			KeyCollection oldKeys)
		{
			KeyCollection dupKeys = new KeyCollection();
			if (newKeys.Count == 0)
				return dupKeys;
			if (oldKeys.Count == 0)
				return dupKeys;

			KeyItem newOneKey;
			KeyItem oldOneKey;
			int i = 0;    //i,j����-1ʱ��ʾ��Ӧ�ļ��Ͻ���
			int j = 0;
			int ret;

			//������ѭ��������һ�����Ͻ������±��Ϊ-1������ѭ��
			while (true)
			{
				if (i >= newKeys.Count)
				{
					i = -1;
					//strInfo += "�����<br/>";
				}

				if (j >= oldKeys.Count)
				{
					j = -1;
					//strInfo += "�ҽ���<br/>";
				}

				//�������϶�û�н���ʱ��ִ�бȽϣ���������ѭ��������һ�����Ͻ�����
				if (i != -1 && j != -1)
				{
					newOneKey = (KeyItem)newKeys[i];
					oldOneKey = (KeyItem)oldKeys[j];

					ret = newOneKey.CompareTo(oldOneKey);  //MyCompareTo(oldOneKey); //��CompareTO

					//strInfo += "��-��,����"+Convert.ToString(ret)+"<br/>";

					if (ret == 0)  //������0ʱ,i,j���ı�
					{
						newKeys.Remove(newOneKey);          //��ΪRemoveAt()
						oldKeys.Remove(oldOneKey);
						dupKeys.Add(oldOneKey);
					}


					//��һ��С����һ�������ƶ�

					if (ret<0)  
						i++;

					if (ret>0)
						j++;

					//strInfo += "i=" + Convert.ToString(i) + "j=" + Convert.ToString(j) + "<br/>";
				}
				else
				{
					break;
				}
			}
			return dupKeys;
		}

		//�г������е�������,����ʹ��
		//���ر���ַ���
		public string Dump()
		{
			string strResult = "";

			foreach(KeyItem keyItem in this)
			{
				strResult += "SqlTableName=" + keyItem.SqlTableName + " Key=" + keyItem.Key + " FormValue=" + keyItem.FromValue + " RecordID=" + keyItem.RecordID + " Num=" + keyItem.Num + " KeyNoProcess=" + keyItem.KeyNoProcess + " FromName=" + keyItem.FromName + "\r\n";
			}
			return strResult;
		}

        // �Լ��Ͻ���ȥ�أ�ȥ��֮ǰ����DpKeys.Sort()��������
        public void RemoveDup()
        {
            KeyItem prev = null;
            for (int i = 0; i < this.Count; i++)
            {
                KeyItem current = this[i];

                if (prev != null && current.CompareTo(prev) == 0)
                {
                    this.RemoveAt(i);
                    i--;
                    continue;
                }

                prev = current;
            }
        }

#if NO
		//�Լ��Ͻ���ȥ�أ�ȥ��֮ǰ����DpKeys.Sort()��������
		public void RemoveDup()
		{
			for(int i=0;i<this.Count;i++)
			{
				for(int j=i+1;j<this.Count;j++)
				{
					KeyItem Itemi = (KeyItem)this[i];
					KeyItem Itemj = (KeyItem)this[j];

					if(Itemi.CompareTo(Itemj) == 0)  //MyCompareTo(Itemj) == 0)  //��CompareTo
					{
						this.RemoveAt(j);
						j--;//??????
					}
					else
					{
						break;
					}
				}
			}
		}
#endif

	}


	//�����ͼ:��ʾ����key
	//�̳�IComparable�ӿ�
	public class KeyItem : IComparable<KeyItem>
	{
		public string SqlTableName;	// ��Ӧ��Sql Server����
		public string Key;				// key	��ӦSql Server���е�keystring�ֶ�
		public string FromValue;		// <from>������	��ӦSql Server���е�fromstring�ֶ�
		public string RecordID;		// ��¼ID	��ӦSql Server���е�idstring�ֶ�
		public string Num;				// key��int���ͣ������һ��ר�ŵ��ֶ�����11>2������	��ӦSql Server���е�keystringnum�ֶ�

		public string KeyNoProcess;	// δ�����key
		public string FromName;		// ��Դ�����������԰汾��ȷ��


		
		// parameters:
		//		strSqlTableName	��Ӧ��SQL Server����
		//		strKey			keystring�ַ���
		//		strFromValue	<from>�е�ֵ
		//		strRecordID		��¼ID
		//		strNum			key��������ʽ
		//		strKeyNoProcess	δ�����key
		//		strFromName		��Դ�������ݴ���key�����Դ���ȷ����
		// ˵��:��strKeyNoProcess�⣬��ÿһ��ȥǰ��հ�
		public KeyItem(string strSqlTableName,
			string strKey,
			string strFromValue,
			string strRecordID, 
			string strNum,
			string strKeyNoProcess,
			string strFromName)
		{
			this.SqlTableName = strSqlTableName.Trim();
			
			// ����ֶ�д������
			this.Key = strKey.Trim().Replace("\n","");
			this.FromValue = strFromValue.Trim();
			this.RecordID = strRecordID.Trim();
			this.Num = strNum;

			this.KeyNoProcess = strKeyNoProcess;
			this.FromName = strFromName.Trim();
		}


		//��ʽִ�У�����ֱ��ͨ��DpKey�Ķ���ʵ��������
		//obj: �ȽϵĶ���
		//0��ʾ��ȣ�������ʾ����
        public int CompareTo(KeyItem keyItem)
		{
            // 2013/2/18
            // tablename
            int nRet = String.CompareOrdinal(this.SqlTableName, keyItem.SqlTableName);
            if (nRet != 0)
                return nRet;

			// ��Key����
			nRet = String.Compare(this.Key,keyItem.Key);
            if (nRet != 0)
                return nRet;

			// ������¼������
            nRet = String.Compare(this.RecordID, keyItem.RecordID);
            if (nRet != 0)
                return nRet;

            nRet = String.Compare(this.FromName, keyItem.FromName);
            if (nRet != 0)
                return nRet;


			// ���������key����
            nRet = String.Compare(this.Num, keyItem.Num);
            return nRet;
		}
	} 
}
