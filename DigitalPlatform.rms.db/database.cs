//#define DEBUG_LOCK

using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.IO;
using DigitalPlatform.ResultSet;
using DigitalPlatform.Range;

namespace DigitalPlatform.rms
{
    // ���ݿ����
    public class Database
    {
        public RecordIDStorage RebuildIDs = null;

        internal bool m_bTailNoVerified = false; // ���ݿ�β���Ƿ�У�����
        // ���ݿ���
        protected MyReaderWriterLock m_db_lock = new MyReaderWriterLock();            // �������m_lock

        // β����,��GetNewTailNo() �� SetIfGreaten
        protected MyReaderWriterLock m_TailNolock = new MyReaderWriterLock();

        // ��¼��
        internal RecordLockCollection m_recordLockColl = new RecordLockCollection();

        // ����ʱ��ʱ��
        internal int m_nTimeOut = 5 * 1000; //5�� 

        internal DatabaseCollection container; // ����

        // ���ݿ���ڵ�
        internal XmlNode m_selfNode = null;

        // ���ݿ����Խڵ�
        XmlNode m_propertyNode = null;
        Hashtable m_captionTable = new Hashtable();
        public XmlNode PropertyNode
        {
            get
            {
                return this.m_propertyNode;
            }
            set
            {
                this.m_propertyNode = value;
                m_captionTable.Clear();
            }
        }

        //�������ݿ�ID,ǰ������@
        public string PureID = "";

        internal int KeySize = 0;

        public int m_nTimestampSeed = 0;

        private KeysCfg m_keysCfg = null;
        // private BrowseCfg m_browseCfg = null;
        // private bool m_bHasBrowse = true;
        Hashtable browse_table = new Hashtable();


        public bool InRebuildingKey = false;   // �Ƿ����ؽ��������״̬

        public int FastAppendTaskCount = 0; // ���� fast mode ��Ƕ�״���
        // public bool IsDelayWriteKey = false;    // �Ƿ����ӳ�д��keys��״̬ 2013/2/16

        internal Database(DatabaseCollection container)
        {
            this.container = container;
        }

        // ��ʼ�����ݿ���
        // parameters:
        //      node    ���ݿ����ýڵ�<database>
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        internal virtual int Initial(XmlNode node,
            out string strError)
        {
            strError = "";
            return 0;
        }

        // �ر����ݿ����
        internal virtual void Close()
        {
        }

        // ��Connection��Transaction Commit
        internal virtual void Commit()
        {
        }

        // ������ݿ�β��
        // parameters:
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        // �ߣ���ȫ��
        public int CheckTailNo(out string strError)
        {
            strError = "";

            if (this.m_bTailNoVerified == true)
                return 0;

            string strRealTailNo = "";
            int nRet = 0;

            //********�����ݿ�Ӷ���**************
            m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK
			this.container.WriteDebugInfo("CheckTailNo()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            try
            {
                nRet = this.UpdateStructure(out strError);
                if (nRet == -1)
                    return -1;

                // return:
                //		-1  ����
                //      0   δ�ҵ�
                //      1   �ҵ�
                nRet = this.GetRecordID("-1",
                    "prev",
                    out strRealTailNo,
                    out strError);
                if (nRet == -1)
                {
                    strError = "������ݿ� '" + this.GetCaption("zh") + "' ������¼��ʱ����: " + strError;
                    return -1;
                }
            }
            catch (Exception ex)
            {
                // ???�п��ܻ�δ��ʼ�����ݿ�
                strError = "������ݿ� '"+this.GetCaption("zh")+"' ������¼��ʱ�����쳣��" + ex.Message;
                return -1;
            }
            finally
            {
                //***********�����ݿ�����***************
                m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK
				this.container.WriteDebugInfo("CheckTailNo()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }

            //this.container.WriteErrorLog("�ߵ�'" + this.GetCaption("zh-CN") + "'���ݿ��CheckTailNo()������鵽����¼��Ϊ'" + strRealTailNo + "'��");

            if (nRet == 1)
            {
                //��SetIfGreaten()�������������¼�Ŵ���β��,�Զ��ƶ�β��Ϊ���
                //����������������Ǹ��ļ�¼�ų���β��ʱ
                bool bPushTailNo = false;
                bPushTailNo = this.AdjustTailNo(Convert.ToInt32(strRealTailNo),
                    false);
            }

            this.m_bTailNoVerified = true;

            return 0;
        }

        // ����strStyle���,�õ���Ӧ�ļ�¼��
        // prev:ǰһ��,next:��һ��,���strID == ? ��prevΪ��һ��,nextΪ���һ��
        // ���������prev��next���ܵ��˺���
        // parameter:
        //		strCurrentRecordID	��ǰ��¼ID
        //		strStyle	        ���
        //      strOutputRecordID   out�����������ҵ��ļ�¼��
        //      strError            out���������س�����Ϣ
        // return:
        //		-1  ����
        //      0   δ�ҵ�
        //      1   �ҵ�
        // �ߣ�����ȫ
        internal virtual int GetRecordID(string strCurrentRecordID,
            string strStyle,
            out string strOutputRecordID,
            out string strError)
        {
            strOutputRecordID = "";
            strError = "";
            return 0;
        }

        // �������ݿ�ṹ�������Ҫ
        internal virtual int UpdateStructure(out string strError)
        {
            strError = "";
            return 0;
        }

        // �õ��������ڴ����
        // ������0ʱ��keysCfg����Ϊnull
        public int GetKeysCfg(out KeysCfg keysCfg,
            out string strError)
        {
            strError = "";
            keysCfg = null;

            // �Ѵ���ʱ
            if (this.m_keysCfg != null)
            {
                keysCfg = this.m_keysCfg;
                return 0;
            }

            int nRet = 0;

            string strKeysFileName = "";
     
            string strDbName = this.GetCaption("zh");
            // return:
            //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
            //		-2	û�ҵ��ڵ�
            //		-3	localname����δ�����Ϊֵ��
            //		-4	localname�ڱ��ز�����
            //		-5	���ڶ���ڵ�
            //		0	�ɹ�
            nRet = this.container.GetFileCfgItemLocalPath(strDbName + "/cfgs/keys",
                out strKeysFileName,
                out strError);

            // δ����keys���󣬰������������
            if (nRet == -2)
                return 0;

            // keys�ļ��ڱ��ز����ڣ��������������
            if (nRet == -4)
                return 0;

            if (nRet != 0)
                return -1;


            this.m_keysCfg = new KeysCfg();
            nRet = this.m_keysCfg.Initial(strKeysFileName,
                this.container.BinDir,
                this is SqlDatabase && this.container.SqlServerType == SqlServerType.Oracle ? "" : null,
                out strError);
            if (nRet == -1)
            {
                this.m_keysCfg = null;
                return -1;
            }

            keysCfg = this.m_keysCfg;
            return 0;
        }

        // �õ������ʽ�ڴ����
        // parameters:
        //      strBrowseName   ����ļ����ļ�������ȫ·��
        //                      ���� cfgs/browse ����Ϊ�Ѿ������ض������ݿ��л�������ļ�����������ָ����������
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        public int GetBrowseCfg(
            string strBrowseName,
            out BrowseCfg browseCfg,
            out string strError)
        {
            strError = "";
            browseCfg = null;

            strBrowseName = strBrowseName.ToLower();
            /*
            if (this.m_bHasBrowse == false)
                return 0;
             * */

            /*
            // �Ѵ���ʱ
            if (this.m_browseCfg != null)
            {
                browseCfg = this.m_browseCfg;
                return 0;
            }
             * */
            browseCfg = (BrowseCfg)this.browse_table[strBrowseName];
            if (browseCfg != null)
            {
                return 1;
            }

/*
            string strDbName = this.GetCaption("zh");

            // strDbName + "/cfgs/browse"
 * */
            string strBrowsePath = this.GetCaption("zh") + "/" + strBrowseName;

            string strBrowseFileName = "";
            // return:
            //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
            //		-2	û�ҵ��ڵ�
            //		-3	localname����δ�����Ϊֵ��
            //		-4	localname�ڱ��ز�����
            //		-5	���ڶ���ڵ�
            //		0	�ɹ�
            int nRet = this.container.GetFileCfgItemLocalPath(strBrowsePath,
                out strBrowseFileName,
                out strError);
            if (nRet == -2 || nRet == -4)
            {
                // this.m_bHasBrowse = false;
                return 0;
            }
            else
            {
                // this.m_bHasBrowse = true;
            }

            if (nRet != 0)
            {
                return -1;
            }


            browseCfg = new BrowseCfg();
            nRet = browseCfg.Initial(strBrowseFileName,
                this.container.BinDir,
                out strError);
            if (nRet == -1)
            {
                browseCfg = null;
                return -1;
            }

            this.browse_table[strBrowseName] = browseCfg;
            return 1;
        }

        // ʱ�������
        public long GetTimestampSeed()
        {
            return this.m_nTimestampSeed++;
        }

        // �õ����ݿ�ID��ע��ǰ���"@"
        // return:
        //		���ݿ�ID,��ʽΪ:@ID
        // ��: ����ȫ
        public string FullID
        {
            get
            {
                return "@" + this.PureID;
            }
        }

        // �õ����ݿ�������߼������ŵ�һ���ַ���������
        public LogicNameItem[] GetLogicNames()
        {
            ArrayList aLogicName = new ArrayList();
            XmlNodeList captionList = this.PropertyNode.SelectNodes("logicname/caption");
            for (int i = 0; i < captionList.Count; i++)
            {
                XmlNode captionNode = captionList[i];
                string strLang = DomUtil.GetAttr(captionNode, "lang");
                string strValue = captionNode.InnerText.Trim(); // 2012/2/16

                // �п���δ�������ԣ���δ����ֵ������ô��������
                LogicNameItem item = new LogicNameItem();
                item.Lang = strLang;
                item.Value = strValue;
                aLogicName.Add(item);
            }

            LogicNameItem[] logicNames = new LogicNameItem[aLogicName.Count];

            for (int i = 0; i < aLogicName.Count; i++)
            {
                LogicNameItem item = (LogicNameItem)aLogicName[i];
                logicNames[i] = item;
            }
            return logicNames;
        }

        // �л���İ汾
        public string GetCaption(string strLang)
        {
            string strResult = (string)this.m_captionTable[strLang == null ? "<null>" : strLang];
            if (strResult != null)
                return strResult;

            strResult = GetCaptionInternal(strLang);
            this.m_captionTable[strLang == null ? "<null>" : strLang] = strResult;
            return strResult;
        }

        // �õ�ĳ���Ե����ݿ���
        // parameters:
        //      strLang ���==null����ʾʹ�����ݿ����ʵ�ʶ���ĵ�һ������
        string GetCaptionInternal(string strLang)
        {
            XmlNode nodeCaption = null;
            string strCaption = "";

            if (String.IsNullOrEmpty(strLang) == true)
                goto END1;

            strLang = strLang.Trim();
            if (String.IsNullOrEmpty(strLang) == true)
                goto END1;

            // 1.�����԰汾��ȷ��
            nodeCaption = this.PropertyNode.SelectSingleNode("logicname/caption[@lang='" + strLang + "']");
            if (nodeCaption != null)
            {
                // strCaption = DomUtil.GetNodeText(nodeCaption).Trim();
                strCaption = nodeCaption.InnerText.Trim();  // 2012/2/15
                if (String.IsNullOrEmpty(strCaption) == false)
                    return strCaption;
            }

            // �����԰汾�س����ַ���
            if (strLang.Length >= 2)
            {
                string strShortLang = strLang.Substring(0, 2);//

                // 2. ��ȷ��2�ַ���
                nodeCaption = this.PropertyNode.SelectSingleNode("logicname/caption[@lang='" + strShortLang + "']");
                if (nodeCaption != null)
                {
                    // strCaption = DomUtil.GetNodeText(nodeCaption).Trim();
                    strCaption = nodeCaption.InnerText.Trim();  // 2012/2/15
                    if (String.IsNullOrEmpty(strCaption) == false)
                        return strCaption;
                }

                // 3. ��ֻ��ǰ�����ַ���ͬ��
                // xpathʽ�Ӿ�������֤��xpath�±�ϰ�ߴ�1��ʼ
                nodeCaption = this.PropertyNode.SelectSingleNode("logicname/caption[(substring(@lang,1,2)='" + strShortLang + "')]");
                if (nodeCaption != null)
                {
                    // strCaption = DomUtil.GetNodeText(nodeCaption).Trim();
                    strCaption = nodeCaption.InnerText.Trim();  // 2012/2/15
                    if (String.IsNullOrEmpty(strCaption) == false)
                        return strCaption;
                }

            }

        END1:
            // 4.��������ڵ�һλ��caption
            nodeCaption = this.PropertyNode.SelectSingleNode("logicname/caption");
            if (nodeCaption != null)
            {
                // strCaption = DomUtil.GetNodeText(nodeCaption).Trim();
                strCaption = nodeCaption.InnerText.Trim();  // 2012/2/15

            }
            if (string.IsNullOrEmpty(strCaption) == false)
                return strCaption;


            // 5.���һ�����԰汾��Ϣ��û��ʱ���������ݿ��id
            return this.FullID; // TODO: �ǲ��ǻ�Ҫ���Ϸ�����֮��?
        }

        // �������ԣ��õ����ݵı�ǩ��
        // parameter:
        //		strLang ���԰汾
        // return:
        //		�ҵ��������ַ���
        //		û�ҵ�,�᷵�ؿ��ַ���""
        // ��: ��ȫ��
        public string GetCaptionSafety(string strLang)
        {
            //***********�����ݿ�Ӷ���******GetCaption���ܻ��׳��쳣��������try,catch
            m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK
			this.container.WriteDebugInfo("GetCaptionSafety(strLang)����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            try
            {
                return this.GetCaption(strLang);

            }
            finally
            {
                m_db_lock.ReleaseReaderLock();
                //*****************�����ݿ�����*********
#if DEBUG_LOCK
				this.container.WriteDebugInfo("GetCaptionSafety(strLang)����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }
        }


        // �õ�ȫ�����Ե�caption��ÿ��Ԫ���ڵĸ�ʽ ���Դ���:����
        public List<string> GetAllLangCaption()
        {
            List<string> result = new List<string>();

            XmlNodeList listCaption =
                this.PropertyNode.SelectNodes("logicname/caption");
            foreach (XmlNode nodeCaption in listCaption)
            {
                string strLang = DomUtil.GetAttr(nodeCaption, "lang");
                string strText = nodeCaption.InnerText.Trim(); // 2012/2/16

                result.Add(strLang + ":" + strText);
            }

            return result;
        }

        // ��: ��ȫ��
        public List<string> GetAllLangCaptionSafety()
        {
            //***********�����ݿ�Ӷ���******GetCaption���ܻ��׳��쳣��������try,catch
            m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK		
			this.container.WriteDebugInfo("GetAllLangCaptionSafety()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            try
            {
                return GetAllLangCaption();
            }
            finally
            {
                m_db_lock.ReleaseReaderLock();
                //*****************�����ݿ�����*********
#if DEBUG_LOCK
				this.container.WriteDebugInfo("GetAllLangCaptionSafety()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }
        }

        // �õ�ȫ����caption�����ֵ֮���÷ֺ�(?)�ָ�
        public string GetAllCaption()
        {
            StringBuilder strResult = new StringBuilder(4096);
            XmlNodeList listCaption =
                this.PropertyNode.SelectNodes("logicname/caption");
            foreach (XmlNode nodeCaption in listCaption)
            {
                if (strResult.Length > 0)
                    strResult.Append(",");
                strResult.Append(nodeCaption.InnerText.Trim()); // 2012/2/16
            }
            return strResult.ToString();
        }

        // �õ�����caption��ֵ,�Զ��ŷָ�
        // ��: ��ȫ��
        public string GetCaptionsSafety()
        {
            //***********�����ݿ�Ӷ���******GetCaption���ܻ��׳��쳣��������try,catch
            m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK		
			this.container.WriteDebugInfo("GetCaptionSafety()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            try
            {
                return GetAllCaption();
            }
            finally
            {
                m_db_lock.ReleaseReaderLock();
                //*****************�����ݿ�����*********
#if DEBUG_LOCK
				this.container.WriteDebugInfo("GetCaptionSafety()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }
        }

        // �õ�����caption��ֵ��ID,�Զ��ŷָ�
        // ��: ��ȫ��
        public string GetAllNameSafety()
        {
            //***********�����ݿ�Ӷ���******GetCaption���ܻ��׳��쳣��������try,catch
            m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK
			this.container.WriteDebugInfo("GetAllNameSafety()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            try
            {
                string strAllName = GetAllCaption();
                if (strAllName != "")
                    strAllName += ",";
                strAllName += this.FullID;
                return strAllName;
            }
            finally
            {
                m_db_lock.ReleaseReaderLock();
                //*****************�����ݿ�����********
#if DEBUG_LOCK
				this.container.WriteDebugInfo("GetAllNameSafety()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }
        }

        // �������ݿ�dbo
        // return:
        //		string���ͣ�dbo�û���
        // ��: ����ȫ��
        internal string GetDbo()
        {
            string strDboValue = "";
            if (this.m_selfNode != null)
                strDboValue = DomUtil.GetAttr(this.m_selfNode, "dbo").Trim();
            return strDboValue;
        }

        // ��: ��ȫ��  ����ԭ��:�����ݿ����ø��ڵ������
        public string DboSafety
        {
            get
            {
                //***********�����ݿ�Ӷ���******GetDbo�������죬���Բ��ü�try,catch
                m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK
				this.container.WriteDebugInfo("TypeSafety���ԣ���'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
                try
                {
                    return GetDbo();
                }
                finally
                {
                    //**********�����ݿ�����************
                    m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK
					this.container.WriteDebugInfo("TypeSafety���ԣ���'" + this.GetCaption("zh-CN") + "'���ݿ�������");

#endif
                }
            }
        }

        // ˽��GetType����: �������ݿ�����
        // return:
        //		string���ͣ����ݿ�����
        // ��: ����ȫ��
        internal string GetDbType()
        {
            string strType = "";
            if (this.m_selfNode != null)
                strType = DomUtil.GetAttr(this.m_selfNode, "type").Trim();
            return strType;
        }



        // ��: ��ȫ��  ����ԭ��:�����ݿ����ø��ڵ������
        public string TypeSafety
        {
            get
            {
                //***********�����ݿ�Ӷ���******GetDbType�������죬���Բ��ü�try,catch
                m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK
				this.container.WriteDebugInfo("TypeSafety���ԣ���'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
                try
                {
                    return GetDbType();
                }
                finally
                {
                    //**********�����ݿ�����************
                    m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK
					this.container.WriteDebugInfo("TypeSafety���ԣ���'" + this.GetCaption("zh-CN") + "'���ݿ�������");

#endif
                }
            }
        }

        // ���ݿ�մ򿪵�ʱ����ΪУ���β�ţ�����֪�����ݿ��ʵ��β����ʲô
        // �����ÿ���������ݿ��¼��λ�ö�ά�ֺ�һ���ڴ��ʵ��β�ţ���ô�Ϳ���������������ڴ�Ƚϣ�
        // ��Ϊ select �� records ���������е�һ����ɸ��ֻ�е�С�����ʵ��β�ŵļ�¼���б�Ҫȥ select��������������ٶ�
        // ��������һ���취�������������׷�ӵģ�Ҳ�����ʺ������ŵ������������ȥ insert records �� �ȳ����˱����Ÿ�Ϊ���Ǵ���
        // 
        // ��Ϊ���ݿ�򿪵�ʱ��У���β�ţ�����׷�ӵ�ʱ���ڻ�õ�β��λ�û���ͻȻ���ֵ��Ѿ����ڵ��еļ���̫С��
        // ���������������а��������ݿ��������������ļ��ʾ͸�С��
        // �����о�һ����һ�����ύ������ insert ����У���������Щ�������Ѿ����ڵļ�¼��Ȼ��ý�������


        // �õ�����ֵ
        // return
        //      ��¼β��
        // ��: ����ȫ
        // �쳣������ַ�����ʽ���Կ��ܻ��׳��쳣
        private int GetTailNo()
        {
            XmlNode nodeSeed =
                this.PropertyNode.SelectSingleNode("seed");
            if (nodeSeed == null)
            {
                throw new Exception("�����������ļ�����,δ�ҵ�<seed>Ԫ�ء�");
            }

            return System.Convert.ToInt32(nodeSeed.InnerText.Trim()); // 2012/2/16
        }

        // �޸����ݿ��β��
        // parameter:
        //		 nSeed  �����β����
        // ��: ����ȫ
        protected void ChangeTailNo(int nSeed)  //��Ϊprotected����Ϊ�ڳ�ʼ��ʱ������
        {
            XmlNode nodeSeed =
                this.PropertyNode.SelectSingleNode("seed");

            // DomUtil.SetNodeText(nodeSeed, Convert.ToString(nSeed));
            nodeSeed.InnerText = Convert.ToString(nSeed);   // 2012/2/16

            this.container.Changed = true;
        }

        // �������ؼ������ݿ�����β��
        // ��ν�����ǣ�strDeletedID �������պ��ǵ�ǰ��βһ������
        internal bool TryRecycleTailNo(string strDeletedID)
        {
            if (this.m_bTailNoVerified == false)
                return false;

            //****************�����ݿ�β�ż�д��***********
            this.m_TailNolock.AcquireWriterLock(m_nTimeOut);
#if DEBUG_LOCK
			this.container.WriteDebugInfo("TryDecreaseTailNo()����'" + this.GetCaption("zh-CN") + "'���ݿ�β�ż�д����");
#endif
            try
            {
                int nNumber = 0;
                if (Int32.TryParse(strDeletedID, out nNumber) == false)
                    return false;

                int nTemp = GetTailNo();   //����������ַ���������ֱ����Seed++
                if (nNumber == nTemp)
                {
                    ChangeTailNo(nTemp - 1);
                    return true;
                }

                return false;
            }
            finally
            {
                //***************�����ݿ�β�Ž�д��************
                this.m_TailNolock.ReleaseWriterLock();
#if DEBUG_LOCK
				this.container.WriteDebugInfo("TryDecreaseTailNo()����'" + this.GetCaption("zh-CN") + "'���ݿ�β�Ž�д����");
#endif
            }
        }

        // �õ���¼��IDβ�ţ��ȼ�1�ٷ��أ�,
        // ��: ��ȫ
        // ����ԭ�򣬼�д�����޸���nodeSeed���ݣ���ʼ�ձ������ӣ����Դ�ʱ���������ٶ���д
        // return:
        //      -1  ����ԭ���ǵ�ǰ���ݿ�����β����δ����У��
        //      ����  ��������ID
        protected int GetNewTailNo()
        {
            //****************�����ݿ�β�ż�д��***********
            this.m_TailNolock.AcquireWriterLock(m_nTimeOut);
#if DEBUG_LOCK
			this.container.WriteDebugInfo("GetNewTailNo()����'" + this.GetCaption("zh-CN") + "'���ݿ�β�ż�д����");
#endif
            try
            {
                if (this.m_bTailNoVerified == false)
                    return -1;

                int nTemp = GetTailNo();   //����������ַ���������ֱ����Seed++
                nTemp++;
                ChangeTailNo(nTemp);
                return nTemp; //GetTailNo();
            }
            finally
            {
                //***************�����ݿ�β�Ž�д��************
                this.m_TailNolock.ReleaseWriterLock();
#if DEBUG_LOCK
				this.container.WriteDebugInfo("GetNewTailNo()����'" + this.GetCaption("zh-CN") + "'���ݿ�β�Ž�д����");
#endif
            }
        }

        // �ƶ����ݿ�����β�ţ������Ҫ
        // ����û��ּ�����ļ�¼�Ŵ��ڵ����ݿ��β�ţ�
        // ��Ҫ�޸�β�ţ���ʱ����������Ϊд����
        // �޸�����ٽ���Ϊ����
        // parameter:
        //		nID         ����ID
        //		isExistReaderLock   �Ƿ��Ѵ��ڶ��������Ѿ������򱾺����Ͳ�������
        // ��: ��ȫ��
        // return:
        //      �Ƿ������ƶ�β�ŵ����
        protected bool AdjustTailNo(int nID,
            bool isExistReaderLock)
        {
            bool bTailNoChanged = false;

            if (isExistReaderLock == false)
            {
                //*********�����ݿ�β�żӶ���*************
                // this.m_TailNolock.AcquireReaderLock(m_nTimeOut);
                this.m_TailNolock.AcquireUpgradeableReaderLock(m_nTimeOut);

#if DEBUG_LOCK
				this.container.WriteDebugInfo("SetIfGreaten()����'" + this.GetCaption("zh-CN") + "'���ݿ�β�żӶ�����");
#endif
            }
            else
            {
                // ��Χ�Ѿ�������ע��Ӧ���� AcquireUpgradeableReaderLock()
            }

            try
            {
                int nSavedNo = GetTailNo();
                if (nID > nSavedNo)
                {
                    // 2006/12/8 ע�͵�
                    // д��־
                    // this.container.WriteErrorLog("�������ݿ�'" + this.GetCaption("zh-CN") + "'��ʵ��β��'" + Convert.ToString(nID) + "'���ڱ����β��'" + Convert.ToString(nSavedNo) + "'���ƶ�β�š�");
                    bTailNoChanged = true;

                    //*********��������Ϊд��************
                    LockCookie lc = m_TailNolock.UpgradeToWriterLock(m_nTimeOut);
#if DEBUG_LOCK
					this.container.WriteDebugInfo("SetIfGreaten()����'" + this.GetCaption("zh-CN") + "'���ݿ�β�Ŷ�������Ϊд����");
#endif
                    try
                    {
                        ChangeTailNo(nID);
                    }
                    finally
                    {
                        //*************��д��Ϊ����*************
                        m_TailNolock.DowngradeFromWriterLock(ref lc);
#if DEBUG_LOCK
						this.container.WriteDebugInfo("SetIfGreaten()����'" + this.GetCaption("zh-CN") + "'���ݿ�β��д���½�Ϊ������");
#endif
                    }
                }

                return bTailNoChanged;
            }
            finally
            {
                if (isExistReaderLock == false)
                {
                    //*******�����ݿ�β�Ž����********
                    // this.m_TailNolock.ReleaseReaderLock();
                    this.m_TailNolock.ReleaseUpgradeableReaderLock();

#if DEBUG_LOCK					
					this.container.WriteDebugInfo("SetIfGreaten()����'" + this.GetCaption("zh-CN") + "'���ݿ�β�Ž������");
#endif
                }
            }
        }

#if  NO
        // �����ݿ�����ı�ǩ��Ϣ��ת����TableInfo��������
        // ��������;�����Ƿ����id���������;��Ϊ�գ���ʾ��ȫ����������м���(������id)
        // parameter:
        //		strTableNames   ����;�����ƣ�֮���ö��ŷָ�
        //      bHasID          out����������;�����Ƿ���id
        //      aTableInfo      out����������TableInfo����
        //      strError        out���������س�����Ϣ
        // returns:
        //      -1  ����
        //      0   �ɹ�
        // ��: ����ȫ
        protected int TableNames2aTableInfo(string strTableNames,
            out bool bHasID,
            out List<TableInfo> aTableInfo,
            out string strError)
        {
            strError = "";
            bHasID = false;
            aTableInfo = new List<TableInfo>();

            strTableNames = strTableNames.Trim();

            int nRet = 0;

            KeysCfg keysCfg = null;
            nRet = this.GetKeysCfg(out keysCfg,
                out strError);
            if (nRet == -1)
                return -1;


            //���strTableListΪ��,�򷵻����еı�,��������ͨ��id����
            if (strTableNames == "")
            {
                if (keysCfg != null)
                {
                    nRet = keysCfg.GetTableInfosRemoveDup(out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }
                return 0;
            }

            string[] aTableName = strTableNames.Split(new Char[] { ',' });
            for (int i = 0; i < aTableName.Length; i++)
            {
                string strTableName = aTableName[i].Trim();

                if (strTableName == "")
                    continue;

                if (strTableName == "__id")
                {
                    bHasID = true;
                    continue;
                }

                if (keysCfg != null)
                {
                    TableInfo tableInfo = null;
                    nRet = keysCfg.GetTableInfo(strTableName,
                        out tableInfo,
                        out strError);
                    if (nRet == 0)
                        continue;
                    if (nRet != 1)
                        return -1;
                    aTableInfo.Add(tableInfo);
                }
            }
            return 0;
        }
#endif

        // 2007/9/14�����汾
        // �����ݿ�����ı�ǩ��Ϣ��ת����TableInfo��������
        // ��������;�����Ƿ����id���������;��Ϊ�գ���ʾ��ȫ����������м���(������id)
        // parameter:
        //		strTableNames   ����;�����ƣ�֮���ö��ŷָ������Ϊ��,���ʾ�������еı���м���(��������ͨ��id;������)
        //      bHasID          out����������;�����Ƿ���id
        //      aTableInfo      out����������TableInfo����
        //      strError        out���������س�����Ϣ
        // returns:
        //      -1  ����
        //      0   �ɹ�
        // ��: ����ȫ
        protected int TableNames2aTableInfo(string strTableNames,
            out bool bHasID,
            out List<TableInfo> aTableInfo,
            out string strError)
        {
            strError = "";
            bHasID = false;
            aTableInfo = new List<TableInfo>();

            strTableNames = strTableNames.Trim();

            int nRet = 0;

            KeysCfg keysCfg = null;
            nRet = this.GetKeysCfg(out keysCfg,
                out strError);
            if (nRet == -1)
                return -1;


            //���strTableNamesΪ��,�򷵻����еı�,��������ͨ��id����
            if (strTableNames == ""
                || strTableNames.ToLower() == "<all>"
                || strTableNames == "<ȫ��>")
            {
                if (keysCfg != null)
                {
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }
                return 0;
            }

            // 2007/9/14 ����������ִ���ٶ�
            List<TableInfo> ref_table_infos = new List<TableInfo>();
            nRet = keysCfg.GetTableInfosRemoveDup(out ref_table_infos,
                out strError);
            if (nRet == -1)
                return -1;

            string[] aTableName = strTableNames.Split(new Char[] { ',' });
            for (int i = 0; i < aTableName.Length; i++)
            {
                string strTableName = aTableName[i].Trim();

                if (strTableName == "")
                    continue;

                if (strTableName == "__id")
                {
                    bHasID = true;
                    continue;
                }

                if (keysCfg != null)
                {
                    TableInfo tableInfo = null;
                    nRet = keysCfg.GetTableInfo(strTableName,
                        ref_table_infos, // 2007/9/14 ����������ִ���ٶ�
                        out tableInfo,
                        out strError);
                    if (nRet == 0)
                        continue;
                    if (nRet != 1)
                        return -1;
                    aTableInfo.Add(tableInfo);
                }
            }

            return 0;
        }

        // ����
        // parameter:
        //      strOutpuStyle   ����ķ�����Ϊkeycount��������鲢ͳ�ƺ��key+count�����򣬻���ȱʡ��Ϊ��ͳ�������¼id
        //		searchItem  SearchItem����
        //		isConnected IsConnection���������ж�ͨѶ�Ƿ�����
        //      resultSet   ��������󣬴�����м�¼�������������ڼ���ǰ��ս��������ˣ���ͬһ�����������ִ�б�����������԰����н��׷����һ��
        //		nWarningLevel   �����漶�� 0����ʾ�ر�ǿ�ң����־���Ҳ��������1����ʾ��ǿ�ң������س�������ִ��
        //		strError    out���������س�����Ϣ
        //		strWarning  out���������ؾ�����Ϣ
        // return:
        //		-1  ����
        //		>=0 ���м�¼��
        // ��: ��ȫ��
        internal virtual int SearchByUnion(
            string strOutputStyle,
            SearchItem searchItem,
            ChannelHandle handle,
            // Delegate_isConnected isConnected,
            DpResultSet resultSet,
            int nWarningLevel,
            out string strError,
            out string strWarning)
        {
            strError = "";
            strWarning = "";
            return 0;
        }

        // ��ʼ�����ݿ⣬ע���麯������Ϊprivate
        // parameter:
        //		strError    out���������س�����Ϣ
        // return:
        //		-1  ����
        //		0   �ɹ�
        // ��: ��ȫ��,�������������
        public virtual int InitialPhysicalDatabase(out string strError)
        {
            strError = "";
            return 0;
        }

        // 2008/11/14
        // ˢ�����ݿ�(SQL��)���壬ע���麯������Ϊprivate
        // parameter:
        //		strError    out���������س�����Ϣ
        // return:
        //		-1  ����
        //		0   �ɹ�
        // ��: ��ȫ��,�������������
        public virtual int RefreshPhysicalDatabase(
            bool bClearAllKeyTables,
            out string strError)
        {
            strError = "";
            return 0;
        }

        // parameters:
        //      strAction   delete/create/rebuild/disable/rebuildall/disableall
        public virtual int ManageKeysIndex(
            string strAction,
            out string strError)
        {
            strError = "";
            return 0;
        }

        // ɾ�����ݿ�
        // return:
        //      -1  ����
        //      0   �ɹ�
        public virtual int Delete(out string strError)
        {
            strError = "";
            return 0;
        }

        // ��ָ����Χ��Xml
        // parameter:
        //		strID				��¼ID
        //		nStart				��Ŀ����Ŀ�ʼλ��
        //		nLength				���� -1:��ʼ������
        //		nMaxLength			���Ƶ���󳤶�
        //		strStyle			���,data:ȡ���� prev:ǰһ����¼ next:��һ����¼
        //							withresmetadata���Ա�ʾ����Դ��Ԫ�����body���
        //							ͬʱע��ʱ��������ߺϲ����ʱ���
        //		destBuffer			out�����������ֽ�����
        //		strMetadata			out����������Ԫ����
        //		strOutputResPath	out������������ؼ�¼��·��
        //		outputTimestamp		out����������ʱ���
        //		strError			out���������س�����Ϣ
        // return:
        //		-1  ����
        //		-4  δ�ҵ���¼
        //      -10 ��¼�ֲ�δ�ҵ�
        //		>=0 ��Դ�ܳ���
        //      nAdditionError -50 ��һ�������¼���Դ��¼������
        // ��: ��ȫ��
        public virtual long GetXml(string strID,
            string strXPath,
            long lStart,
            int nLength,
            int nMaxLength,
            string strStyle,
            out byte[] destBuffer,
            out string strMetadata,
            out string strOutputResPath,
            out byte[] outputTimestamp,
            bool bCheckAccount,
            out int nAdditionError,
            out string strError)
        {
            destBuffer = null;
            strMetadata = "";
            strOutputResPath = "";
            outputTimestamp = null;
            strError = "";
            nAdditionError = 0;

            return 0;
        }


        // ��ָ����Χ������
        // strRecordID  �����ļ�¼ID
        // strObjectID  ��Դ����ID
        // ��������GetXml(),��strOutputResPath����
        // ��: ��ȫ��
        public virtual long GetObject(string strRecordID,
            string strObjectID,
            long lStart,
            int nLength,
            int nMaxLength,
            string strStyle,
            out byte[] destBuffer,
            out string strMetadata,
            out byte[] outputTimestamp,
            out string strError)
        {
            destBuffer = null;
            strMetadata = "";
            outputTimestamp = null;
            strError = "";
            return 0;
        }

        // �õ�xml����
        // ��:��ȫ��,���ⲿ��
        // return:
        //      -1  ����
        //      -4  ��¼������
        //      0   ��ȷ
        public int GetXmlDataSafety(string strRecordID,
            out string strXml,
            out string strError)
        {
            strXml = "";
            strError = "";

            //********�����ݿ�Ӷ���**************
            this.m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK
			this.container.WriteDebugInfo("GetXmlDataSafety()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            try
            {
                strRecordID = DbPath.GetID10(strRecordID);
                //*******************�Լ�¼�Ӷ���************************
                m_recordLockColl.LockForRead(strRecordID,
                    m_nTimeOut);
#if DEBUG_LOCK
				this.container.WriteDebugInfo("GetXmlDataSafety()����'" + this.GetCaption("zh-CN") + "/" + strID + "'��¼�Ӷ�����");
#endif

                try
                {
                    // return:
                    //      -1  ����
                    //      -4  ��¼������
                    //      0   ��ȷ
                    return this.GetXmlData(strRecordID,
                        out strXml,
                        out strError);
                }
                finally
                {
                    //************�Լ�¼�����*************
                    m_recordLockColl.UnlockForRead(strRecordID);
#if DEBUG_LOCK
					this.container.WriteDebugInfo("GetXmlDataSafety()����'" + this.GetCaption("zh-CN") + "/" + strID + "'��¼�������");
#endif
                }
            }

            finally
            {
                //*********�����ݿ�����****************
                m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK
				this.container.WriteDebugInfo("GetXmlDataSafety()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }
        }

        // ��ȡ��¼��xml����
        // return:
        //      -1  ����
        //      -4  ��¼������
        //      0   ��ȷ
        public virtual int GetXmlData(string strRecordID,
            out string strXml,
            out string strError)
        {
            strXml = "";
            strError = "";

            return 0;
        }

        // ���ݻ���� ID �б��ؽ���Щ��¼�ļ�����
        //      strStyle    "fastmode,deletekeys" ��˼�Ǹ������ݿ���һ���ּ�¼�ļ����㣬�����Ҫ����ɾ�� keys�����Ҫ��ʼ�׶�keys��� B+ ����Ҫɾ��������Ϊ�� buikcopy ����ɾ�� B+ ��
        //                  ""  ��˼������ģʽ�����������غ���Ҫ�κκ�������
        // return:
        //      -1  ����
        //      >=0 ����� keys ����
        public virtual int RebuildKeys(
            string strStyle,
            out string strError)
        {
            strError = "";

            if (this.RebuildIDs == null || this.RebuildIDs.Count == 0)
                return 0;

            bool bFastMode = StringUtil.IsInList("fastmode", strStyle);

            string strSubStyle = "rebuildkeys";
            if (bFastMode == false)
                strSubStyle += ",deletekeys";

            if (string.IsNullOrEmpty(strStyle) == false)
                strSubStyle += "," + strStyle;  // ��� strStyle �б������� deletekeys��Ҳ�������

            this.RebuildIDs.Seek(0);    // ��ָ��ŵ���ͷλ��
            // TODO: Ӧ�������������ļ�ָ�벻�ٸı� ?

            // List<string> ids = new List<string>();
            List<RecordBody> records = new List<RecordBody>();

            int nTotalCount = 0;

            string strID = "";
            while (this.RebuildIDs.Read(out strID))
            {
                if (string.IsNullOrEmpty(strID) == true)
                    continue;   // �����Ѿ����ɾ���� ID

                if (records.Count > 100)
                {
                    List<RecordBody> outputs = null;

                    int nRet = WriteRecords(
                        null,   // User oUser,
                        records,
                        strSubStyle,
                        out outputs,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    // TODO: ���ƴ�����һ��Ӧ�������ܼ���������ȥ����������һ��(�����Bulkcopy����Ҫ���������Ƿ���ظ�����keys��Ϣ�е�����)

                    // TODO: �Ƿ�ʱ�����Щ�Ѿ������ ID?

                    nTotalCount += nRet;
                    records.Clear();
                }

                RecordBody record = new RecordBody();
                record.Path = "./" + strID;
                records.Add(record);
            }

            // ���һ��
            if (records.Count > 0)
            {
                List<RecordBody> outputs = null;

                int nRet = WriteRecords(
                    null,   // User oUser,
                    records,
                    strSubStyle,
                    out outputs,
                    out strError);
                if (nRet == -1)
                    return -1;
                // TODO: ���ƴ�����һ��Ӧ�������ܼ���������ȥ����������һ��(�����Bulkcopy����Ҫ���������Ƿ���ظ�����keys��Ϣ�е�����)
                // TODO: �Ƿ�ʱ�����Щ�Ѿ������ ID?
                nTotalCount += nRet;
                records.Clear();
            }

            return nTotalCount;
        }

        public virtual long BulkCopy(
            string strAction,
            out string strError)
        {
            strError = "";

            return 0;
        }

        internal class WriteResInfo
        {
            public string ID = "";
            public string XPath = "";
        }

        // д��һ�� XML ��¼
        // �������� WriteXml ʵ���˻������ܣ����ٶ�û�еõ��Ż��������������д�˺���������������ٶ�
        // return:
        //      -1  ����
        //      >=0 ����� rebuildkeys���򷵻��ܹ������ keys ����
        public virtual int WriteRecords(
            // SessionInfo sessininfo,
            User oUser,
            List<RecordBody> records,
            string strStyle,
            out List<RecordBody> outputs,
            out string strError)
        {
            strError = "";
            outputs = new List<RecordBody>();

            if (StringUtil.IsInList("rebuildkeys", strStyle) == true)
            {
                strError = "Ŀǰ Database::WriteRecords() ��δʵ���ؽ�������Ĺ���";
                return -1;
            }

            foreach (RecordBody record in records)
            {
                string strPath = record.Path;   // �������ݿ�����·��

                string strDbName = StringUtil.GetFirstPartPath(ref strPath);

                bool bObject = false;
                string strRecordID = "";
                string strObjectID = "";
                string strXPath = "";

                string strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                //***********�Ե���2��*************
                // ����Ϊֹ��strPath������¼�Ų��ˣ��¼�������ж�
                strRecordID = strFirstPart;
                // ֻ����¼�Ų��·��
                if (strPath == "")
                {
                    bObject = false;
                }
                else
                {
                    strFirstPart = StringUtil.GetFirstPartPath(ref strPath);
                    //***********�Ե���2��*************
                    // ����Ϊֹ��strPath����object��xpath�� strFirstPart������object �� xpath

                    if (strFirstPart != "object"
        && strFirstPart != "xpath")
                    {
                        record.Result.Value = -1;
                        record.Result.ErrorString = "��Դ·�� '" + record.Path + "' ���Ϸ�, ��3�������� 'object' �� 'xpath' ";
                        record.Result.ErrorCode = ErrorCodeValue.PathError; // -7;
                        continue;
                    }
                    if (string.IsNullOrEmpty(strPath) == true)  //object��xpath�¼�������ֵ
                    {
                        record.Result.Value = -1;
                        record.Result.ErrorString = "��Դ·�� '" + record.Path + "' ���Ϸ�,����3���� 'object' �� 'xpath' ʱ����4������������";
                        record.Result.ErrorCode = ErrorCodeValue.PathError; // -7;
                        continue;
                    }

                    if (strFirstPart == "object")
                    {
                        strObjectID = strPath;
                        bObject = true;
                    }
                    else
                    {
                        strXPath = strPath;
                        bObject = false;
                    }
                }

                if (bObject == true)
                {
                    record.Result.Value = -1;
                    record.Result.ErrorString = "Ŀǰ�������� WriteRecords д�������Դ";
                    record.Result.ErrorCode = ErrorCodeValue.CommonError;
                    continue;
                }

                byte[] baContent = Encoding.UTF8.GetBytes(record.Xml);
                string strRanges = "0-" + (baContent.Length - 1).ToString();

                byte[] outputTimestamp = null;
                string strOutputID = "";
                string strOutputValue = "";

                int nRet = WriteXml(oUser,
                strRecordID,
                strXPath,
                strRanges,
                baContent.Length,
                baContent,
                record.Metadata,
                strStyle,
                record.Timestamp,
            out outputTimestamp,
            out strOutputID,
            out strOutputValue,
            false,
            out strError);
                if (nRet <= -1)
                {
                    record.Result.Value = -1;
                    record.Result.ErrorCode = KernelApplication.Ret2ErrorCode(nRet);
                    record.Result.ErrorString = strError;
                }
                else
                {
                    if (string.IsNullOrEmpty(strXPath) == true)
                        record.Path = strDbName + "/" + strOutputID;
                    else
                        record.Path = strDbName + "/" + strOutputID + "/xpath/" + strXPath;
                    record.Result.Value = nRet;
                    record.Result.ErrorCode = ErrorCodeValue.NoError;
                    record.Result.ErrorString = strOutputValue;
                }

                record.Timestamp = outputTimestamp;
                record.Metadata = null;
                record.Xml = null;

                outputs.Add(record);
            }

            return 0;
        }

        // дxml����
        // parameter:
        //		strRecordID     ��¼ID
        //		strRanges       д���Ƭ�Ϸ�Χ
        //		nTotalLength    �����ܳ���
        //		sourceBuffer    д��������ֽ�����
        //		strMetadata     Ԫ����
        //		intputTimestamp �����ʱ���
        //		outputTimestamp out���������ص�ʱ���,����ʱ,Ҳ����ʱ���
        //		strOutputID     out���������صļ�¼ID,׷�Ӽ�¼ʱ,����
        //		strError        out���������س�����Ϣ
        // return:
        // return:
        //		-1  ����
        //		-2  ʱ�����ƥ��
        //      -4  ��¼������
        //      -6  Ȩ�޲���
        //		0   �ɹ�
        // ˵��,�ܳ�����Դ����� != null,��д������,Ƭ�Ϻϲ�������Ƭ�ϼǵ�����,�������,����Ƭ��Ϊ���ַ���
        // ��: ��ȫ��
        public virtual int WriteXml(User oUser,
            string strID,
            string strXPath,
            string strRanges,
            long lTotalLength,
            byte[] baSource,
            // Stream streamSource,
            string strMetadata,
            string strStyle,
            byte[] inputTimestamp,
            out byte[] outputTimestamp,
            out string strOutputID,
            out string strOutputValue,
            bool bCheckAccount,
            out string strError)
        {
            outputTimestamp = null;
            strOutputID = "";
            strOutputValue = "";
            strError = "";

            return 0;
        }


        // parameters:
        //      strRecordID  ��¼ID
        //      strObjectID  ����ID
        //      ����ͬWriteXml,��strOutputID����
        // return:
        //		-1  ����
        //		-2  ʱ�����ƥ��
        //      -4  ��¼�������Դ������
        //      -6  Ȩ�޲���
        //		0   �ɹ�
        // ��: ��ȫ��
        public virtual int WriteObject(User user,
            string strRecordID,
            string strObjectID,
            string strRanges,
            long lTotalLength,
            byte[] baSource,
            // Stream streamSource,
            string strMetadata,
            string strStyle,
            byte[] intputTimestamp,
            out byte[] outputTimestamp,
            out string strError)
        {
            outputTimestamp = intputTimestamp;
            strError = "";
            return 0;
        }

        // ɾ����¼
        // ������,����ͨ�޷�ɾ��ʱ,ʹ��ǿ�Ʒ���
        // parameter:
        //		strRecordID     ��¼ID
        //      strStyle        �ɰ��� fastmode
        //		inputTimestamp  �����ʱ���
        //		outputTimestamp out����������ʱ���,������ȷʱ��Ч
        //		strError        out���������س�����Ϣ
        // return:
        //		-1  һ���Դ���
        //		-2  ʱ�����ƥ��
        //      -4  δ�ҵ���¼
        //		0   �ɹ�
        // ��: ��ȫ��,��д��,�������������
        public virtual int DeleteRecord(
            string strID,
            byte[] timestamp,
            string strStyle,
            out byte[] outputTimestamp,
            out string strError)
        {
            outputTimestamp = null;
            strError = "";
            return 0;
        }

        // 2008/11/14
        // �ؽ���¼��keys
        // parameter:
        //		strRecordID     ��¼ID
        //      strStyle    next prev outputpath forcedeleteoldkeys
        //                  forcedeleteoldkeys Ҫ�ڴ�����keysǰǿ��ɾ��һ�¾��е�keys? ���Ϊ��������ǿ��ɾ��ԭ�е�keys�����Ϊ������������̽�Ŵ����µ�keys������оɵ�keys���´��㴴����keys�غϣ��ǾͲ��ظ�����������ɵ�keys�в���û�б�ɾ����Ҳ����������
        //                          ���� һ�����ڵ�����¼�Ĵ��������� һ������Ԥ��ɾ��������keys����������Ժ���ѭ���ؽ�����ÿ����¼��������ʽ
        //		strError        out���������س�����Ϣ
        // return:
        //		-1  һ���Դ���
        //		-2  ʱ�����ƥ��
        //      -4  δ�ҵ���¼
        //		0   �ɹ�
        // ��: ��ȫ��,��д��,�������������
        public virtual int RebuildRecordKeys(string strRecordID,
            string strStyle,
            out string strOutputRecordID,
            out string strError)
        {
            strError = "";
            strOutputRecordID = "";

            return 0;
        }

        Hashtable m_xpath_table = null;

        // �õ�һ����¼�������ʽ��һ���ַ�������
        // parameter:
        //      strFormat   �����ʽ���塣���Ϊ�գ���ʾ �൱�� cfgs/browse
        //                  ����� @coldef: ��������ʾΪ XPath ��̬���ж��壬���� @def://parent|//title �����߱�ʾ�еļ��
        //                  �����ʾ browse �����ļ������ƣ����� cfgs/browse_temp ֮��
        //		strRecordID	һ������λ���ļ�¼�ţ���10λ���ֵļ�¼��
        //      strXml  ��¼�塣���Ϊ�գ����������Զ���ȡ��¼��
        //      nStartCol   ��ʼ���кš�һ��Ϊ0
        //		cols	out���������������ʽ����
        // ������ʱ,������ϢҲ����������
        // return:
        //      cols�а������ַ�����
        public int GetCols(
            string strFormat,
            string strRecordID,
            string strXml,
            int nStartCol,
            out string[] cols)
        {
            cols = null;
            int nRet = 0;

            if (string.IsNullOrEmpty(strFormat) == true)
                strFormat = "cfgs/browse";

            //**********�����ݿ�Ӷ���**************
            this.m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK
			this.container.WriteDebugInfo("GetCols()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            try
            {
                string strError = "";
                BrowseCfg browseCfg = null;
                string strColDef = "";

                // �ж���
                if (strFormat[0] == '@')
                {
                    if (StringUtil.HasHead(strFormat, "@coldef:") == true)
                        strColDef = strFormat.Substring("@coldef:".Length).Trim();
                    else
                    {
                        strError = "�޷�ʶ��������ʽ '" + strFormat + "'";
                        goto ERROR1;
                    }
                }
                else
                {
                    // �����ʽ�����ļ�

                    // string strBrowseName = this.GetCaption("zh") + "/" + strFormat; // TODO: Ӧ��֧��./cfgs/xxxx ��
                    string strBrowseName = strFormat; // ���� cfgs/browse ����Ϊ�Ѿ������ض������ݿ��л�������ļ�����������ָ����������
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   found
                    nRet = this.GetBrowseCfg(
                        strBrowseName,
                        out browseCfg,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    if (browseCfg == null)
                    {
                        // return 0;

                        // 2013/1/14
                        cols = new string[1];
                        cols[0] = strError;
                        return -1;
                    }
                }

                // string strXml;
                
                // ��ü�¼��
                if (string.IsNullOrEmpty(strXml) == true)   // 2012/1/5
                {
                    strRecordID = DbPath.GetID10(strRecordID);
                    //*******************�Լ�¼�Ӷ���************************
                    m_recordLockColl.LockForRead(strRecordID, m_nTimeOut);
#if DEBUG_LOCK
				this.container.WriteDebugInfo("GetCols()����'" + this.GetCaption("zh-CN") + "/" + strRecordID + "'��¼�Ӷ�����");
#endif
                    try
                    {
                        // return:
                        //      -1  ����
                        //      -4  ��¼������
                        //      0   ��ȷ
                        nRet = this.GetXmlData(strRecordID,
                            out strXml,
                            out strError);
                        if (nRet <= -1)
                            goto ERROR1;
                    }
                    finally
                    {
                        //*******************�Լ�¼�����************************
                        m_recordLockColl.UnlockForRead(strRecordID);
#if DEBUG_LOCK
				this.container.WriteDebugInfo("GetCols()����'" + this.GetCaption("zh-CN") + "/" + strRecordID + "'��¼�������");
#endif
                    }
                }

                XmlDocument domData = new XmlDocument();
                try
                {
                    domData.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "����'" + this.GetCaption("zh-CN") + "'���'" + strRecordID + "'��¼��domʱ����,ԭ��: " + ex.Message;
                    goto ERROR1;
                }

                if (browseCfg != null)
                {
                    // return:
                    //		-1	����
                    //		>=0	�ɹ�������ֵ����ÿ���а������ַ���֮��
                    nRet = browseCfg.BuildCols(domData,
                        nStartCol,
                        out cols,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }
                else
                {
                    // �ж���

                    // Ԥ�� Hashtable ��ù���
                    if (m_xpath_table != null && m_xpath_table.Count > 1000)
                    {
                        lock (m_xpath_table)
                        {
                            m_xpath_table.Clear();
                        }
                    }

                    nRet = BuildCols(
                        ref m_xpath_table,
                        strColDef,
                        domData,
                        nStartCol,
                        out cols,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }

                return nRet;

            ERROR1:
                cols = new string[1];
                cols[0] = strError;
                return strError.Length;
            }
            finally
            {
                //****************�����ݿ�����**************
                this.m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK		
				this.container.WriteDebugInfo("GetCols()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }
        }

#if NO
        // �õ���¼��ʱ���
        // return:
        //		-1  ����
        //		-4  δ�ҵ���¼
        //      0   �ɹ�
        public virtual int GetTimestampFromDb(string strID,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            baOutputTimestamp = null;
            strError = "";


            return 0;
        }
#endif

        // TODO: XPathExpression���Ի����������ӿ��ٶ�
        // ����ָ����¼�������ʽ����
        // parameters:
        //		domData	    ��¼����dom ����Ϊnull
        //      nStartCol   ��ʼ���кš�һ��Ϊ0
        //      cols        �����ʽ����
        //		strError	out������������Ϣ
        // return:
        //		-1	����
        //		>=0	�ɹ�������ֵ����ÿ���а������ַ���֮��
        static int BuildCols(
            ref Hashtable xpath_table,
            string strColDef,
            XmlDocument domData,
            int nStartCol,
            out string[] cols,
            out string strError)
        {
            strError = "";
            cols = new string[0];

            Debug.Assert(domData != null, "BuildCols()���ô���domData��������Ϊnull��");

            if (xpath_table == null)
                xpath_table = new Hashtable();

            int nResultLength = 0;

            string[] xpaths = strColDef.Split(new char[] { '|' });

            XPathNavigator nav = domData.CreateNavigator();

            List<string> col_array = new List<string>();

            for (int i = 0; i < xpaths.Length; i++)
            {
                string strSegment = xpaths[i];
                string strXpath = "";
                string strConvert = "";
                StringUtil.ParseTwoPart(strSegment, "->", out strXpath, out strConvert);
                if (string.IsNullOrEmpty(strXpath) == true)
                {
                    col_array.Add("");  // �յ� XPath �����յ�һ��
                    continue;
                }

                XPathExpression expr = (XPathExpression)xpath_table[strXpath];

                if (expr == null)
                {
                    // ����Cache
                    expr = nav.Compile(strXpath);
                        /*
                        if (nsmgr != null)
                            expr.SetContext(nsmgr);
                         * */

                    lock (xpath_table)
                    {
                        xpath_table[strXpath] = expr;
                    }
                }

                Debug.Assert(expr != null, "");

                string strText = "";

                if (expr.ReturnType == XPathResultType.Number)
                {
                    strText = nav.Evaluate(expr).ToString();//Convert.ToString((int)(nav.Evaluate(expr)));
                }
                else if (expr.ReturnType == XPathResultType.Boolean)
                {
                    strText = Convert.ToString((bool)(nav.Evaluate(expr)));
                }
                else if (expr.ReturnType == XPathResultType.String)
                {
                    strText = (string)(nav.Evaluate(expr));
                }
                else if (expr.ReturnType == XPathResultType.NodeSet)
                {
                    XPathNodeIterator iterator = nav.Select(expr);

                    while (iterator.MoveNext())
                    {
                        XPathNavigator navigator = iterator.Current;
                        string strOneText = navigator.Value;
                        if (strOneText == "")
                            continue;

                        strText += strOneText;
                    }
                }
                else
                {
                    strError = "XPathExpression��ReturnTypeΪ'" + expr.ReturnType.ToString() + "'��Ч";
                    return -1;
                }

                if (string.IsNullOrEmpty(strConvert) == false)
                {
                    List<string> convert_methods = StringUtil.SplitList(strConvert);
                    strText = BrowseCfg.ConvertText(convert_methods, strText);
                }

                // ������ҲҪ����һ��
                col_array.Add(strText);
                nResultLength += strText.Length;
            }

            // ��col_arrayת��cols��
            cols = new string[col_array.Count + nStartCol];
            col_array.CopyTo(cols, nStartCol);
            return nResultLength;
        }

        // ��дxml���ݣ��õ������㼯��
        // parameter:
        //		strXml	xml����
        //		strID	��¼ID,�����������
        //		strLang	���԰汾
        //		strStyle	��񣬿��Ʒ���ֵ
        //		keyColl	    out����,���ؼ����㼯�ϵ�
        //		strError	out���������س�����Ϣ
        // return:
        //		-1	����
        //		0	�ɹ�
        // ��: ��ȫ��
        public int API_PretendWrite(string strXml,
            string strRecordID,
            string strLang,
            // string strStyle,
            out KeyCollection keys,
            out string strError)
        {
            keys = null;
            strError = "";
            //**********�����ݿ�Ӷ���**************
            this.m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK

			this.container.WriteDebugInfo("PretendWrite()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");

#endif
            try
            {
                //�������ݵ�DOM
                XmlDocument domData = new XmlDocument();
                domData.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue
                try
                {
                    domData.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "PretendWrite()����ز����е�xml���ݳ���ԭ��:" + ex.Message;
                    return -1;
                }

                KeysCfg keysCfg = null;
                int nRet = this.GetKeysCfg(out keysCfg,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (keysCfg != null)
                {
                    //����������
                    keys = new KeyCollection();
                    nRet = keysCfg.BuildKeys(domData,
                        strRecordID,
                        strLang,
                        // strStyle,
                        this.KeySize,
                        out keys,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    //����ȥ��
                    keys.Sort();
                    keys.RemoveDup();
                }
                return 0;
            }
            finally
            {
                //****************�����ݿ�����**************
                this.m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK		
				this.container.WriteDebugInfo("PretendWrite()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }
        }

        // �ϲ�������
        // parameters:
        //      strNewXml   �¼�¼��XML������Ϊ""����null
        //      strOldXml   �ɼ�¼��XML������Ϊ""����null
        //      bOutputDom  �Ƿ�����newDom/oldDom˳�����DOM?
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int MergeKeys(string strID,
            string strNewXml,
            string strOldXml,
            bool bOutputDom,
            out KeyCollection newKeys,
            out KeyCollection oldKeys,
            out XmlDocument newDom,
            out XmlDocument oldDom,
            out string strError)
        {
            newKeys = null;
            oldKeys = null;
            newDom = null;
            oldDom = null;
            strError = "";

            int nRet;

            KeysCfg keysCfg = null;

            nRet = this.GetKeysCfg(out keysCfg,
                out strError);
            if (nRet == -1)
                return -1;


            // ������xml����������
            newKeys = new KeyCollection();

            if (String.IsNullOrEmpty(strNewXml) == false)
            {
                newDom = new XmlDocument();
                newDom.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue

                try
                {
                    newDom.LoadXml(strNewXml);
                }
                catch (Exception ex)
                {
                    strError = "���������ݵ�domʱ����" + ex.Message;
                    return -1;
                }

                if (keysCfg != null)
                {
                    nRet = keysCfg.BuildKeys(newDom,
                        strID,
                        "zh",//strLang,
                        // "",//strStyle,
                        this.KeySize,
                        out newKeys,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    newKeys.Sort();
                    newKeys.RemoveDup();
                }
            }

            oldKeys = new KeyCollection();

            if (String.IsNullOrEmpty(strOldXml) == false
                && strOldXml.Length > 1)    // 2012/1/31
            {
                oldDom = new XmlDocument();
                oldDom.PreserveWhitespace = true; //��PreserveWhitespaceΪtrue

                try
                {
                    oldDom.LoadXml(strOldXml);
                }
                catch (Exception ex)
                {
                    strError = "���ؾ����ݵ�domʱ����" + ex.Message;
                    return -1;
                }

                if (keysCfg != null)
                {
                    nRet = keysCfg.BuildKeys(oldDom,
                        strID,
                        "zh",//strLang,
                        // "",//strStyle,
                        this.KeySize,
                        out oldKeys,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    oldKeys.Sort();
                    oldKeys.RemoveDup();
                }
            }

            // �¾ɼ�������
            KeyCollection dupKeys = new KeyCollection();
            dupKeys = KeyCollection.Merge(newKeys,
                oldKeys);

            if (bOutputDom == false)
            {
                newDom = null;
                oldDom = null;
            }
            return 0;
        }

        // Ϊ���ݿ��еļ�¼����ʱ���
        public string CreateTimestampForDb()
        {
            long lTicks = System.DateTime.UtcNow.Ticks;
            byte[] baTime = BitConverter.GetBytes(lTicks);

            byte[] baSeed = BitConverter.GetBytes(this.GetTimestampSeed());
            Array.Reverse(baSeed);

            byte[] baTimestamp = new byte[baTime.Length + baSeed.Length];
            Array.Copy(baTime,
                0,
                baTimestamp,
                0,
                baTime.Length);
            Array.Copy(baSeed,
                0,
                baTimestamp,
                baTime.Length,
                baSeed.Length);

            return ByteArray.GetHexTimeStampString(baTimestamp);
        }


        // ��ÿ��õ� ID�����ƶ�ϵͳ���ص�β��
        // parameters:
        //      strID   ����� ID ���Ϊ "-1"����ʾϣ��ϵͳ���ݵ�ǰ������β�Ÿ���һ�����õ� ID��
        // exceptions:
        //      Exception   ���ݿ�β��δ����У�飻��������β�Ų��Ϸ�
        public bool EnsureID(ref string strID)
        {
            // bool bTailNoChanged = false;    // ���ݿ�����β���Ƿ����˱仯?
            if (this.m_bTailNoVerified == false)
                throw (new Exception("���ݿ� '" + this.GetCaption("zh") + "' ����β����δ����У�飬�޷�����д�����"));

            if (strID == "-1") // ׷�Ӽ�¼,GetNewTailNo()�ǰ�ȫ��
            {
                strID = Convert.ToString(GetNewTailNo());// �ӵÿ�д��
                if (strID == "-1")
                    throw (new Exception("���ݿ� '" + this.GetCaption("zh") + "' ����β����δ����У�飬�޷�����׷���¼�¼��д�����"));

                strID = DbPath.GetID10(strID);
                return true;
            }

            //*******�����ݿ�Ӷ���**********************
            m_TailNolock.AcquireUpgradeableReaderLock(m_nTimeOut);
#if DEBUG_LOCK
			this.container.WriteDebugInfo("EnsureID()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            try
            {
                strID = DbPath.GetID10(strID);
                if (StringUtil.RegexCompare(@"\B[0123456789]+", strID) == false)
                {
                    throw (new Exception("��¼�� '" + strID + "' ���Ϸ�"));
                }

                // ��SetIfGreaten()�������������¼�Ŵ���β��,�Զ��ƶ�β��Ϊ���
                // ����������������Ǹ��ļ�¼�ų���β��ʱ
                return AdjustTailNo(Convert.ToInt32(strID),
                    true);
            }
            finally
            {
                //***********�����ݿ�����**********
                this.m_TailNolock.ReleaseUpgradeableReaderLock();
#if DEBUG_LOCK
				this.container.WriteDebugInfo("EnsureID()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }
        }

        // �õ�һ�������Ϣ
        // parameters:
        //      strStyle            �����Щ�������? all��ʾȫ�� �ֱ�ָ������logicnames/type/sqldbname/keystext/browsetext
        //		logicNames	    �߼���������
        //		strType	        ���ݿ����� �Զ��ŷָ����ַ���
        //		strSqlDbName	���ݿ��������ƣ�������ݿ�Ϊ��Ϊ�ļ������ݿ⣬�򷵻�����ԴĿ¼������
        //		strKeyText	    �������ļ�����
        //		strBrowseText	�������ļ�����
        //		strError	    ������Ϣ
        // return:
        //		-1	����
        //		0	����
        public int GetInfo(
            string strStyle,
            out LogicNameItem[] logicNames,
            out string strType,
            out string strSqlDbName,
            out string strKeysText,
            out string strBrowseText,
            out string strError)
        {
            logicNames = null;
            strType = "";
            strSqlDbName = "";
            strKeysText = "";
            strBrowseText = "";
            strError = "";

            // ���滯strStyle������,���ں��洦��
            if (String.IsNullOrEmpty(strStyle) == true
                || StringUtil.IsInList("all", strStyle) == true)
            {
                strStyle = "logicnames,type,sqldbname,keystext,browsetext";
            }

            //**********�����ݿ�Ӷ���**************
            this.m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK
			this.container.WriteDebugInfo("GetInfo()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            try
            {
                logicNames = this.GetLogicNames();

                // ���type
                if (StringUtil.IsInList("type", strStyle) == true)
                    strType = this.GetDbType();

                // ���sqldbname
                if (StringUtil.IsInList("sqldbname", strStyle) == true)
                {
                    // �������ĺ������õ�����Դ��Ϣ��������ʵ������
                    strSqlDbName = this.GetSourceName();

                    if (this.container.InstanceName != "" && strSqlDbName.Length > this.container.InstanceName.Length)
                    {
                        string strPart = strSqlDbName.Substring(0, this.container.InstanceName.Length);
                        if (strPart == this.container.InstanceName)
                        {
                            strSqlDbName = strSqlDbName.Substring(this.container.InstanceName.Length + 1); //rmsService_Guestbook
                        }
                    }
                }

                string strDbName = "";
                int nRet = 0;

                // ���keystext
                if (StringUtil.IsInList("keystext", strStyle) == true)
                {
                    string strKeysFileName = "";
                    strDbName = this.GetCaption("zh");

                    // return:
                    //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
                    //		-2	û�ҵ��ڵ�
                    //		-3	localname����δ�����Ϊֵ��
                    //		-4	localname�ڱ��ز�����
                    //		-5	���ڶ���ڵ�
                    //		0	�ɹ�
                    nRet = this.container.GetFileCfgItemLocalPath(strDbName + "/cfgs/keys",
                        out strKeysFileName,
                        out strError);
                    if (nRet != 0)
                    {
                        if (nRet != -4)
                            return -1;
                    }

                    if (File.Exists(strKeysFileName) == true)
                    {
                        StreamReader sr = new StreamReader(strKeysFileName,
                            Encoding.UTF8);
                        strKeysText = sr.ReadToEnd();
                        sr.Close();
                    }

                    /*
                                    // keys�ļ�
                                    KeysCfg keysCfg = null;
                                    int nRet = this.GetKeysCfg(out keysCfg,
                                        out strError);
                                    if (nRet == -1)
                                        return -1;
                                    if (keysCfg != null)
                                    {
                                        if (keysCfg.dom != null)
                                        {
                                            TextWriter tw = new StringWriter();
                                            keysCfg.dom.Save(tw);
                                            tw.Close();
                                            strKeysText = tw.ToString();
                                        }
                                    }
                    */

                }

                // ���browsetext
                if (StringUtil.IsInList("browsetext", strStyle) == true)
                {
                    string strBrowseFileName = "";
                    strDbName = this.GetCaption("zh");
                    // return:
                    //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
                    //		-2	û�ҵ��ڵ�
                    //		-3	localname����δ�����Ϊֵ��
                    //		-4	localname�ڱ��ز�����
                    //		-5	���ڶ���ڵ�
                    //		0	�ɹ�
                    nRet = this.container.GetFileCfgItemLocalPath(strDbName + "/cfgs/browse",
                        out strBrowseFileName,
                        out strError);
                    if (nRet != 0)
                    {
                        if (nRet != -4)
                            return -1;
                    }

                    if (File.Exists(strBrowseFileName) == true)
                    {
                        StreamReader sr = new StreamReader(strBrowseFileName,
                            Encoding.UTF8);
                        strBrowseText = sr.ReadToEnd();
                        sr.Close();
                    }
                    /*
                                    // browse�ļ�
                                    BrowseCfg browseCfg = null;
                                    nRet = this.GetBrowseCfg(out browseCfg,
                                        out strError);
                                    if (nRet == -1)
                                        return -1;
                                    if (browseCfg != null)
                                    {
                                        if (browseCfg.dom != null)
                                        {
                                            TextWriter tw = new StringWriter();
                                            browseCfg.dom.Save(tw);
                                            tw.Close();
                                            strBrowseText = tw.ToString();
                                        }
                                    }
                    */
                }

                return 0;
            }
            finally
            {
                //****************�����ݿ�����**************
                this.m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK		
				this.container.WriteDebugInfo("GetInfo()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }
        }

        public virtual string GetSourceName()
        {
            return "";
        }

        // 2008/5/7
        // ˢ����dom�е�logicnameƬ��
        // TODO�����ԸĽ�Ϊ����ˢ����DOM�е�Ƭ�Σ�Ȼ����������m_propertyNode����selfNode?��
        // ���ԣ��ѱ���ɾ����̽���õ�һ�����Գ��ִ���Ĺ̶��������̣�Ȼ�����¼��뱾�Σ��������Ƿ���ʧ
        int RefreshLognames(string strID,
            string strLogicNames,
            out string strError)
        {
            strError = "";

            XmlNode nodeDatabase = this.container.NodeDbs.SelectSingleNode("database[@id='"+strID+"']");
            if (nodeDatabase == null)
            {
                strError = "idΪ'" + strID + "' ��<database>Ԫ��û���ҵ�";
                return -1;
            }

            XmlNode nodeLogicName = nodeDatabase.SelectSingleNode("property/logicname");
            if (nodeLogicName == null)
            {
                strError = "idΪ'" + strID + "' ��<database>Ԫ����û���ҵ�property/logicnameԪ��";
                return -1;
            }

            nodeLogicName.InnerXml = strLogicNames;
            m_captionTable.Clear(); // 2012/3/17

            return 0;
        }

        // �������ݿ�Ļ�����Ϣ
        // parameters:
        //		logicNames	        LogicNameItem���飬���µ��߼����������滻ԭ�����߼���������
        //		strType	            ���ݿ�����,�Զ��ŷָ���������file,accout��Ŀǰ��Ч����Ϊ�漰�����ļ��⣬����sql�������
        //		strSqlDbName	    ָ������Sql���ݿ����ƣ�Ŀǰ��Ч����������ݿ�Ϊ��Ϊ�ļ������ݿ⣬�򷵻�����ԴĿ¼������
        //		strkeysDefault	    keys������Ϣ�����Ϊnull����ʾ������Ч��(ע�����Ϊ""���ʾҪ���ļ��������)
        //		strBrowseDefault	browse������Ϣ�����Ϊnull����ʾ������Ч��(ע�����Ϊ""���ʾҪ���ļ��������)
        // return:
        //		-1	����
        //      -2  �Ѵ���ͬ�������ݿ�
        //		0	�ɹ�
        public int SetInfo(LogicNameItem[] logicNames,
            string strType,
            string strSqlDbName,
            string strKeysText,
            string strBrowseText,
            out string strError)
        {
            strError = "";

            //****************�����ݿ��д��***********
            m_TailNolock.AcquireWriterLock(m_nTimeOut);
#if DEBUG_LOCK
			this.container.WriteDebugInfo("SetInfo()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");

#endif
            try
            {
                // 2008/4/30 changed
                // "" �� null���岻ͬ�����߱�ʾ��ʹ���������
                /*
                if (strKeysText == null)
                    strKeysText = "";
                if (strBrowseText == null)
                    strBrowseText = "";
                 * */

                if (String.IsNullOrEmpty(strKeysText) == false)
                {
                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strKeysText);
                    }
                    catch (Exception ex)
                    {
                        strError = "����keys�����ļ����ݵ�dom����(1)��ԭ��:" + ex.Message;
                        return -1;
                    }
                }

                if (String.IsNullOrEmpty(strBrowseText) == false)
                {
                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strBrowseText);
                    }
                    catch (Exception ex)
                    {
                        strError = "����browse�����ļ����ݵ�dom����ԭ��:" + ex.Message;
                        return -1;
                    }
                }

                // ����һ���߼�����Ҳû�У�������
                string strLogicNames = "";
                for (int i = 0; i < logicNames.Length; i++)
                {
                    string strLang = logicNames[i].Lang;
                    string strLogicName = logicNames[i].Value;

                    if (strLang.Length != 2
                        && strLang.Length != 5)
                    {
                        strError = "���԰汾�ַ�������ֻ����2λ����5λ,'" + strLang + "'���԰汾���Ϸ�";
                        return -1;
                    }

                    if (this.container.IsExistLogicName(strLogicName, this) == true)
                    {
                        strError = "���ݿ����Ѵ���'" + strLogicName + "'�߼�����";
                        return -2;
                    }
                    strLogicNames += "<caption lang='" + strLang + "'>" + strLogicName + "</caption>";
                }

                // �޸�LogicName��ʹ��ȫ���滻�ķ�ʽ
                XmlNode nodeLogicName = this.PropertyNode.SelectSingleNode("logicname");
                nodeLogicName.InnerXml = strLogicNames;

                int nRet = 0;

                // 2008/5/7
                nRet = RefreshLognames(this.PureID,
                    strLogicNames,
                    out strError);
                if (nRet == -1)
                    return -1;

                // Ŀǰ��֧���޸�strType,strSqlDbName

                if (strKeysText != null)  // 2008/4/30
                {
                    string strKeysFileName = "";//this.GetFixedCfgFileName("keys");
                    string strDbName = this.GetCaption("zh");

                    // string strDbName = this.GetCaption("zh");

                    // return:
                    //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
                    //		-2	û�ҵ��ڵ�
                    //		-3	localname����δ�����Ϊֵ��
                    //		-4	localname�ڱ��ز�����
                    //		-5	���ڶ���ڵ�
                    //		0	�ɹ�
                    nRet = this.container.GetFileCfgItemLocalPath(strDbName + "/cfgs/keys",
                        out strKeysFileName,
                        out strError);
                    if (nRet != 0)
                    {
                        if (nRet != -2 && nRet != -4)
                            return -1;
                        else if (nRet == -2)
                        {
                            // return:
                            //		-1	����
                            //		0	�ɹ�
                            nRet = this.container.SetFileCfgItem(
                                false,
                                this.GetCaption("zh") + "/cfgs",
                                null,
                                "keys",
                                out strError);
                            if (nRet == -1)
                                return -1;

                            // return:
                            //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
                            //		-2	û�ҵ��ڵ�
                            //		-3	localname����δ�����Ϊֵ��
                            //		-4	localname�ڱ��ز�����
                            //		-5	���ڶ���ڵ�
                            //		0	�ɹ�
                            nRet = this.container.GetFileCfgItemLocalPath(this.GetCaption("zh") + "/cfgs/keys",
                                out strKeysFileName,
                                out strError);
                            if (nRet != 0)
                            {
                                if (nRet != -4)
                                    return -1;
                            }
                        }
                    }

                    if (File.Exists(strKeysFileName) == false)
                    {
                        Stream s = File.Create(strKeysFileName);
                        s.Close();
                    }

                    nRet = DatabaseUtil.CreateXmlFile(strKeysFileName,
                        strKeysText,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // �ѻ������
                    this.m_keysCfg = null;
                }

                if (strBrowseText != null)  // 2008/4/30
                {
                    string strDbName = this.GetCaption("zh");

                    string strBrowseFileName = "";

                    // return:
                    //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
                    //		-2	û�ҵ��ڵ�
                    //		-3	localname����δ�����Ϊֵ��
                    //		-4	localname�ڱ��ز�����
                    //		-5	���ڶ���ڵ�
                    //		0	�ɹ�
                    nRet = this.container.GetFileCfgItemLocalPath(strDbName + "/cfgs/browse",
                        out strBrowseFileName,
                        out strError);
                    if (nRet != 0)
                    {
                        if (nRet != -2 && nRet != -4)
                            return -1;
                        else if (nRet == -2)
                        {
                            // return:
                            //		-1	����
                            //		0	�ɹ�
                            nRet = this.container.SetFileCfgItem(
                                false,
                                this.GetCaption("zh") + "/cfgs",
                                null,
                                "browse",
                                out strError);
                            if (nRet == -1)
                                return -1;

                            // return:
                            //		-1	һ���Դ��󣬱�����ô��󣬲������Ϸ���
                            //		-2	û�ҵ��ڵ�
                            //		-3	localname����δ�����Ϊֵ��
                            //		-4	localname�ڱ��ز�����
                            //		-5	���ڶ���ڵ�
                            //		0	�ɹ�
                            nRet = this.container.GetFileCfgItemLocalPath(this.GetCaption("zh") + "/cfgs/browse",
                                out strBrowseFileName,
                                out strError);
                            if (nRet != 0)
                            {
                                if (nRet != -4)
                                    return -1;
                            }
                        }
                    }

                    if (File.Exists(strBrowseFileName) == false)
                    {
                        Stream s = File.Create(strBrowseFileName);
                        s.Close();
                    }

                    nRet = DatabaseUtil.CreateXmlFile(strBrowseFileName,
                        strBrowseText,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // �ѻ������
                    // this.m_browseCfg = null;
                    // this.m_bHasBrowse = true; // ȱʡֵ
                    this.browse_table.Clear();
                }

                return 0;
            }
            finally
            {
                //***************�����ݿ��д��************
                m_TailNolock.ReleaseWriterLock();
#if DEBUG_LOCK
				this.container.WriteDebugInfo("SetInfo()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
            }
        }

        // �õ����ݿ������ʾ���¼�
        // parameters:
        //		oUser	�ʻ�����
        //		db	���ݿ����
        //		strLang	���԰汾
        //		aItem	out�������������ݿ������ʾ���¼�
        //		strError	out���������س�����Ϣ
        // return:
        //		-1	����
        //		0	�ɹ�
        public int GetDirableChildren(User user,
            string strLang,
            string strStyle,
            out ArrayList aItem,
            out string strError)
        {
            aItem = new ArrayList();
            strError = "";

            //**********�����ݿ�Ӷ���**************
            this.m_db_lock.AcquireReaderLock(m_nTimeOut);
#if DEBUG_LOCK
			this.container.WriteDebugInfo("Dir()����'" + this.GetCaption("zh-CN") + "'���ݿ�Ӷ�����");
#endif
            try
            {
                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // 1.��������

                foreach (XmlNode child in this.m_selfNode.ChildNodes)
                {
                    string strElementName = child.Name;
                    if (String.Compare(strElementName, "dir", true) != 0
                        && String.Compare(strElementName, "file", true) != 0)
                    {
                        continue;
                    }


                    string strChildName = DomUtil.GetAttr(child, "name");
                    if (strChildName == "")
                        continue;
                    string strCfgPath = this.GetCaption("zh-CN") + "/" + strChildName;
                    string strExistRights;
                    bool bHasRight = false;

                    ResInfoItem resInfoItem = new ResInfoItem();
                    resInfoItem.Name = strChildName;
                    if (child.Name == "dir")
                    {
                        bHasRight = user.HasRights(strCfgPath,
                            ResType.Directory,
                            "list",
                            out strExistRights);
                        if (bHasRight == false)
                            continue;

                        resInfoItem.HasChildren = true;
                        resInfoItem.Type = 4;   // Ŀ¼

                        resInfoItem.TypeString = DomUtil.GetAttr(child, "type");    // xietao 2006/6/5
                    }
                    else
                    {
                        bHasRight = user.HasRights(strCfgPath,
                            ResType.File,
                            "list",
                            out strExistRights);
                        if (bHasRight == false)
                            continue;
                        resInfoItem.HasChildren = false;
                        resInfoItem.Type = 5;   // �ļ�

                        resInfoItem.TypeString = DomUtil.GetAttr(child, "type"); // xietao 2006/6/5 add

                    }
                    aItem.Add(resInfoItem);
                }


                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // 2.����;��

                KeysCfg keysCfg = null;
                int nRet = this.GetKeysCfg(out keysCfg,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (keysCfg != null)
                {
                    List<TableInfo> aTableInfo = null;
                    nRet = keysCfg.GetTableInfosRemoveDup(
                        out aTableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // ���ڼ���;����ȫ������Ȩ��
                    for (int i = 0; i < aTableInfo.Count; i++)
                    {
                        TableInfo tableInfo = aTableInfo[i];
                        Debug.Assert(tableInfo.Dup == false, "�����������ظ����ˡ�");

                        ResInfoItem resInfoItem = new ResInfoItem();
                        resInfoItem.Type = 1;
                        resInfoItem.Name = tableInfo.GetCaption(strLang);
                        resInfoItem.HasChildren = false;

                        resInfoItem.TypeString = tableInfo.TypeString;  // xietao 2006/6/5 add

                        // 2012/5/16
                        if (string.IsNullOrEmpty(tableInfo.ExtTypeString) == false)
                        {
                            if (string.IsNullOrEmpty(resInfoItem.TypeString) == false)
                                resInfoItem.TypeString += ",";

                            resInfoItem.TypeString += tableInfo.ExtTypeString;
                        }

                        // �����Ҫ, �г����������µ�����
                        if (StringUtil.IsInList("alllang", strStyle) == true)
                        {
                            List<string> results = tableInfo.GetAllLangCaption();
                            string[] names = new string[results.Count];
                            results.CopyTo(names);
                            resInfoItem.Names = names;
                        }

                        aItem.Add(resInfoItem);
                    }
                }

                // ��__id
                ResInfoItem resInfoItemID = new ResInfoItem();
                resInfoItemID.Type = 1;
                resInfoItemID.Name = "__id";
                resInfoItemID.HasChildren = false;
                resInfoItemID.TypeString = "recid";

                aItem.Add(resInfoItemID);

                return 0;
            }
            finally
            {
                //***************�����ݿ�����************
                this.m_db_lock.ReleaseReaderLock();
#if DEBUG_LOCK
				this.container.WriteDebugInfo("Dir()����'" + this.GetCaption("zh-CN") + "'���ݿ�������");
#endif
            }

        }


        // д�����ļ�
        // parameters:
        //      strCfgItemPath  ȫ·����������
        // return:
        //		-1  һ���Դ���
        //      -2  ʱ�����ƥ��
        //		0	�ɹ�
        // �̣߳��Կ⼯���ǲ���ȫ��
        internal int WriteFileForCfgItem(
            bool bNeedLock,
            string strCfgItemPath,
            string strFilePath,
            string strRanges,
            long lTotalLength,
            byte[] baSource,
            // Stream streamSource,
            string strMetadata,
            string strStyle,
            byte[] baInputTimestamp,
            out byte[] baOutputTimestamp,
            out string strError)
        {

            if (bNeedLock == true)
            {
                //**********�����ݿ��д��**************
                this.m_db_lock.AcquireWriterLock(m_nTimeOut);
#if DEBUG_LOCK
			this.container.WriteDebugInfo("Dir()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
            }
            try
            {
                // return:
                //		-1	һ���Դ���
                //		-2	ʱ�����ƥ��
                //		0	�ɹ�
                int nRet = this.container.WriteFileForCfgItem(strFilePath,
                    strRanges,
                    lTotalLength,
                    baSource,
                    // streamSource,
                    strMetadata,
                    strStyle,
                    baInputTimestamp,
                    out baOutputTimestamp,
                    out strError);
                if (nRet <= -1)
                    return nRet;

                int nPosition = strCfgItemPath.IndexOf("/");
                if (nPosition == -1)
                {
                    strError = "'" + strCfgItemPath + "'·��������'/'�����Ϸ���";
                    return -1;
                }
                if (nPosition == strCfgItemPath.Length - 1)
                {
                    strError = "'" + strCfgItemPath + "'·�������'/'�����Ϸ���";
                    return -1;
                }
                string strPathWithoutDbName = strCfgItemPath.Substring(nPosition + 1);
                // ���Ϊkeys������Ѹÿ��KeysCfg�е�dom���
                if (strPathWithoutDbName == "cfgs/keys")
                {
                    this.m_keysCfg = null;
                }

                // ���Ϊbrowse����
                /*
                if (strPathWithoutDbName == "cfgs/browse")
                {
                    this.m_browseCfg = null;
                    this.m_bHasBrowse = true; // ȱʡֵ
                }
                 * */
                if (this.browse_table[strPathWithoutDbName.ToLower()] != null)
                {
                    this.browse_table.Remove(strPathWithoutDbName.ToLower());
                }

                return 0;
            }
            finally
            {
                if (bNeedLock == true)
                {

                    //**********�����ݿ��д��**************
                    this.m_db_lock.ReleaseWriterLock();
#if DEBUG_LOCK
			this.container.WriteDebugInfo("Dir()����'" + this.GetCaption("zh-CN") + "'���ݿ��д����");
#endif
                }
            }

        }


        // �淶����¼��
        // parameters:
        //      strInputRecordID    ����ļ�¼�ţ�ֻ��Ϊ '-1','?'���ߴ�����(��С�ڵ���10λ)
        //      strOututRecordID    out���������ع淶����ļ�¼��
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int CanonicalizeRecordID(string strInputRecordID,
            out string strOutputRecordID,
            out string strError)
        {
            strOutputRecordID = "";
            strError = "";

            if (strInputRecordID.Length > 10)
            {
                strError = "��¼�Ų��Ϸ������ȳ���10λ��";
                return -1;
            }

            if (strInputRecordID == "?" || strInputRecordID == "-1")
            {
                strOutputRecordID = "-1";
                return 0;
            }

            if (StringUtil.IsPureNumber(strInputRecordID) == false)
            {
                strError = "��¼�� '" + strInputRecordID + "' ���Ϸ���";
                return -1;
            }

            strOutputRecordID = DbPath.GetID10(strInputRecordID);
            return 0;
        }
    }
}