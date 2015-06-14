using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Diagnostics;

using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.IO;

namespace DigitalPlatform.CirculationClient
{
    /// <summary>
    /// �����ļ�ǰ�˻���
    /// </summary>
    public class CfgCache
    {
        XmlDocument dom = null;

        string m_strXmlFileName = "";	// �洢���������Ϣ��xml�ļ�

        bool m_bChanged = false;

        string m_strTempDir = "";

        bool m_bAutoSave = true;

        public CfgCache()
        {
        }

        // ��û�������ʱ�ļ�Ŀ¼
        // �����������ʱ�ļ�Ŀ¼, ������Ҫ������ʱ�ļ���ʱ��, �Զ�������ϵͳ��ʱ�ļ�Ŀ¼��
        public string TempDir
        {
            get
            {
                return m_strTempDir;
            }
            set
            {
                m_strTempDir = value;
                // ����Ŀ¼
                if (m_strTempDir != "")
                    PathUtil.CreateDirIfNeed(m_strTempDir);
            }
        }

        // �Ƿ����޸ĺ��������浽�ļ�
        public bool InstantSave
        {
            get
            {
                return m_bAutoSave;
            }
            set
            {
                m_bAutoSave = value;
            }
        }

        // ���һ����ʱ�ļ���
        // ��ʱ�ļ������� m_strTempDirĿ¼��
        string NewTempFileName(bool bUseMacro = true)
        {
            if (m_strTempDir == "")
            {
                // 2007/12/10
                // TODO: ���˹�ɾ������Щ.file�ļ���GetTempFileName() API�ͻ��õ�ԭ�����ù�����Щ��ţ�����ļ��Ź������
                // ���鱾�����һ�����Ӽ�����������ʵ�ֻ����ʱ�ļ��ĺ���
                return Path.GetTempFileName();
            }

            string strFileName = "";
            for (int i = 0; ; i++)
            {
                strFileName = PathUtil.MergePath(m_strTempDir, Convert.ToString(i) + ".file");

                FileInfo fi = new FileInfo(strFileName);
                if (fi.Exists == false)
                {
                    // ����һ��0 byte���ļ�
                    FileStream f = File.Create(strFileName);
                    f.Close();
                    if (bUseMacro == true)
                        return "%cfgcachedir%/" + PathUtil.PureName(strFileName);
                    return strFileName;
                }
            }
        }

        public int Load(string strXmlFileName,
            out string strError)
        {
            strError = "";
            dom = new XmlDocument();

            m_strXmlFileName = strXmlFileName;	// �����Ҳ��Ҫ

            try
            {
                dom.Load(strXmlFileName);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                dom.LoadXml("<root/>");	// ��Ȼ���س���,����dom����ȷ��ʼ���˵�
                return -1;
            }

            return 0;
        }

        // ������������ǰ�Ĳ����ļ�
        // ��Ҫ�� Load() �Ժ����
        public void Upgrade()
        {
            double value = 0;

            if (dom.DocumentElement != null)
            {
                string strVersion = dom.DocumentElement.GetAttribute("version");
                if (string.IsNullOrEmpty(strVersion) == false)
                {
                    // �����Ͱ汾��
                    if (double.TryParse(strVersion, out value) == false)
                        value = 0;
                }
            }

            // ������ 2
            if (value < 2 && dom.DocumentElement != null)
            {
                dom.DocumentElement.SetAttribute("version", "2");
                m_bChanged = true;

                this.ClearCfgCache();
            }
        }

        public void AutoSave()
        {
            if (m_bChanged == false || m_bAutoSave == false)
                return;

            string strError;
            Save(null, out strError);
        }

        // parameters:
        //		strXmlFileName	����Ϊnull
        public int Save(string strXmlFileName,
            out string strError)
        {
            strError = "";

            if (strXmlFileName == null)
                strXmlFileName = m_strXmlFileName;

            if (strXmlFileName == null)
            {
                strError = "m_strXmlFileName��δ��ʼ��...";
                return -1;
            }

            dom.Save(strXmlFileName);
            m_bChanged = false;

            return 0;
        }

        // ���������ļ�����·������Ӧ�ı����ļ�
        // return:
        //		0	not found
        //		1	found
        public int FindLocalFile(string strCfgPath,
            out string strLocalName,
            out string strTimeStamp)
        {
            strCfgPath = strCfgPath.ToLower();	// ���´�Сд������

            XmlNode node = dom.DocumentElement.SelectSingleNode("cfg[@path='" + strCfgPath + "']");

            if (node == null)
            {
                strLocalName = "";
                strTimeStamp = "";

                return 0;	// not found
            }

            strLocalName = DomUtil.GetAttr(node, "localname");

            if (string.IsNullOrEmpty(strLocalName) == true)
                goto DELETE;

            // �滻�ļ����еĺ�
            strLocalName = strLocalName.Replace("%cfgcachedir%", this.m_strTempDir);

            // ��鱾���ļ��Ƿ����
            FileInfo fi = new FileInfo(strLocalName);
            if (fi.Exists == false)
                goto DELETE;

            strTimeStamp = DomUtil.GetAttr(node, "timestamp");
            return 1;

        DELETE:

            strLocalName = "";
            strTimeStamp = "";

            // ɾ�������Ϣ�������Ľڵ�
            dom.DocumentElement.RemoveChild(node);
            m_bChanged = true;
            AutoSave();
            return 0;	// not found
        }

        // Ϊһ������·��׼�������ļ�
        public int PrepareLocalFile(string strCfgPath,
            out string strLocalName)
        {
            strCfgPath = strCfgPath.ToLower();	// ���´�Сд������

            XmlNode node = dom.DocumentElement.SelectSingleNode("cfg[@path='" + strCfgPath + "']");

            if (node != null)
            {
                // �ڵ��Ѿ�����
                strLocalName = DomUtil.GetAttr(node, "localname");
                Debug.Assert(strLocalName != "", "�Ѿ����ڵĽڵ���localname����Ϊ��");
            }
            else
            {
                node = dom.CreateElement("cfg");
                DomUtil.SetAttr(node, "path", strCfgPath);
                strLocalName = NewTempFileName();
                DomUtil.SetAttr(node, "localname", strLocalName);

                node = dom.DocumentElement.AppendChild(node);
                m_bChanged = true;
                AutoSave();
            }

            // �滻�ļ����еĺ�
            strLocalName = strLocalName.Replace("%cfgcachedir%", this.m_strTempDir);
            return 1;
        }

        // Ϊ�Ѿ����ڵĽڵ�����ʱ���ֵ
        public int SetTimeStamp(string strCfgPath,
            string strTimeStamp,
            out string strError)
        {
            strError = "";

            strCfgPath = strCfgPath.ToLower();	// ���´�Сд������

            XmlNode node = dom.DocumentElement.SelectSingleNode("cfg[@path='" + strCfgPath + "']");

            if (node == null)
            {
                strError = "����pathֵΪ '" + strCfgPath + "'��<cfg>Ԫ�ز�����...";
                return -1;
            }

            DomUtil.SetAttr(node, "timestamp", strTimeStamp);
            m_bChanged = true;
            AutoSave();
            return 0;
        }

        // ���ȫ���ڵ�
        public void ClearCfgCache()
        {
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("cfg");

            for (int i = 0; i < nodes.Count; i++)
            {
                string strLocalName = DomUtil.GetAttr(nodes[i], "localname");

                if (string.IsNullOrEmpty(strLocalName) == false)
                {
                    // �滻�ļ����еĺ�
                    strLocalName = strLocalName.Replace("%cfgcachedir%", this.m_strTempDir);
                    try // 2008/3/27
                    {
                        File.Delete(strLocalName);
                    }
                    catch
                    {
                    }
                }
            }

            // ɾ������<cfg>�ڵ�
            for (int i = 0; i < nodes.Count; i++)
            {
                dom.DocumentElement.RemoveChild(nodes[i]);
            }
            m_bChanged = true;
            AutoSave();

            // 2013/4/12
            // ɾ�� cfgacache ��Ŀ¼�Ժ��ؽ�
            if (string.IsNullOrEmpty(this.m_strTempDir) == false)
            {
                PathUtil.DeleteDirectory(this.m_strTempDir);
                PathUtil.CreateDirIfNeed(this.m_strTempDir);
            }
        }

        public int Delete(string strCfgPath,
            out string strError)
        {
            strError = "";

            strCfgPath = strCfgPath.ToLower();	// ���´�Сд������

            XmlNode node = dom.DocumentElement.SelectSingleNode("cfg[@path='" + strCfgPath + "']");

            if (node == null)
            {
                strError = "����pathֵΪ '" + strCfgPath + "'��<cfg>Ԫ�ز�����...";
                return -1;
            }
            string strLocalName = DomUtil.GetAttr(node, "localname");
            if (string.IsNullOrEmpty(strLocalName) == false)
            {
                // �滻�ļ����еĺ�
                strLocalName = strLocalName.Replace("%cfgcachedir%", this.m_strTempDir);

                File.Delete(strLocalName);
            }

            dom.DocumentElement.RemoveChild(node);

            m_bChanged = true;
            AutoSave();
            return 0;
        }
    }
}
