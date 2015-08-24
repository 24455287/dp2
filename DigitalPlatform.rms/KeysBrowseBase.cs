using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.IO;
using System.Diagnostics;

using DigitalPlatform.Xml;

namespace DigitalPlatform.rms
{
    // KeysCfg ��ժҪ˵����
    public class KeysBrowseBase
    {
        public XmlDocument dom = null;

        // <xpath>Ԫ�� �� XmlNamespaceManager����Ķ��ձ� 
        internal Hashtable tableNsClient = new Hashtable();

        // <nstable>Ԫ�ص�xpath·�� �� XmlNamespaceManager����Ķ��ձ�
        internal Hashtable tableNsServer = new Hashtable();

        public string BinDir = "";

        // 2015/8/24
        // �����ļ�ȫ·��
        internal string CfgFileName
        {
            get;
            set;
        }


        // ��ʼ��KeysBrowseBase���󣬰�dom׼���ã�������Hashtable׼����
        public virtual int Initial(string strCfgFileName,
            string strBinDir,
            out string strError)
        {
            strError = "";

            this.BinDir = strBinDir;

            // ���
            this.Clear();

            if (File.Exists(strCfgFileName) == false)
            {
                strError = "�����ļ�'" + strCfgFileName + "'�ڱ��ز�����";
                return -1;
            }

#if NO
            string strText = "";
            // ���keys�ļ�������Ϊ�գ��򲻴�����������������
            StreamReader sw = new StreamReader(strCfgFileName, Encoding.UTF8);
            try
            {
                strText = sw.ReadToEnd();
            }
            finally
            {
                sw.Close();
            }

            if (strText == "")
                return 0;
#endif
            // 2012/2/17
            FileInfo fi = new FileInfo(strCfgFileName);
            if (fi.Length == 0)
                return 0;

            this.CfgFileName = strCfgFileName;

            dom = new XmlDocument();
            try
            {
                dom.Load(strCfgFileName);
            }
            catch (Exception ex)
            {
                strError = "���������ļ� '" + strCfgFileName + "' �� XMLDOM ʱ����" + ex.Message;
                return -1;
            }

            // ����NsTable����,��Initial��
            // return:
            //		-1	����
            //		0	�ɹ�
            int nRet = this.CreateNsTableCache(out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }


        // ����NsTable����,��Initial��
        // return:
        //		-1	����
        //		0	�ɹ�
        private int CreateNsTableCache(out string strError)
        {
            strError = "";
            int nRet = 0;

            // û�������ļ�ʱ
            if (this.dom == null)
                return 0;

            // �ҵ����е�<xpath>Ԫ��
            XmlNodeList xpathNodeList = dom.DocumentElement.SelectNodes("//xpath[@nstable]");
            for (int i = 0; i < xpathNodeList.Count; i++)
            {
                XmlNode nodeXpath = xpathNodeList[i];
                XmlNode nodeNstable = null;
                // return:
                //		-1	����
                //		0	û�ҵ�	strError�����г�����Ϣ
                //		1	�ҵ�
                //		2	������ʹ��nstable
                nRet = FindNsTable(nodeXpath,
                    out nodeNstable,
                    out strError);
                if (nRet == 2)
                    Debug.Assert(false, "�������Ҳ�����");
                if (nRet != 1)
                    return -1;

                // ����ȷʵ����nstable
                if (nodeNstable != null)
                {
                    // ȡ��nodeNstable��·��
                    string strPath = "";
                    nRet = DomUtil.Node2Path(dom.DocumentElement,
                        nodeNstable,
                        out strPath,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    XmlNamespaceManager nsmgr = (XmlNamespaceManager)this.tableNsServer[strPath];
                    if (nsmgr == null)
                    {
                        nRet = GetNsManager(nodeNstable,
                            out nsmgr,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        this.tableNsServer[strPath] = nsmgr;
                    }

                    // �ӵ��ͻ��˱�
                    this.tableNsClient[nodeXpath] = nsmgr;
                }
            }
            return 0;
        }

        // ����<nstable>��������ݵõ�XmlNamespaceManager����
        // return
        //      -1  ����
        //      0   �ɹ�
        private int GetNsManager(XmlNode nodeNstable,
            out XmlNamespaceManager nsmgr,
            out string strError)
        {
            strError = "";
            nsmgr = new XmlNamespaceManager(nodeNstable.OwnerDocument.NameTable);

            XmlNodeList nodeListItem = nodeNstable.SelectNodes("item");
            for (int i = 0; i < nodeListItem.Count; i++)
            {
                XmlNode nodeItem = nodeListItem[i];

                if (nodeItem.ChildNodes.Count > 0)
                {
                    strError = "�����ļ��Ǿɰ汾��<item>Ԫ�ز�֧���¼�Ԫ�ء�";
                    return -1;
                }

                string strPrefix = DomUtil.GetAttr(nodeItem, "prefix");
                string strUrl = DomUtil.GetAttr(nodeItem, "url");

                //???���ǰ׺Ϊ����ʲô�����urlΪ����ʲô�����
                if (strPrefix == "" && strUrl == "")
                    continue;

                nsmgr.AddNamespace(strPrefix, strUrl);
            }

            return 0;
        }

        // ����<xpath>��Ӧ��<nstable>
        // return:
        //		-1	����
        //		0	û�ҵ�	strError�����г�����Ϣ
        //		1	�ҵ�
        //		2	��ʹ��
        private static int FindNsTable(XmlNode nodeXpath,
            out XmlNode nodeNstable,
            out string strError)
        {
            nodeNstable = null;
            strError = "";

            string strNstableName = DomUtil.GetAttrDiff(nodeXpath, "nstable");
            if (strNstableName == null)
                return 2;

            string strXPath = "";
            // ������
            if (strNstableName == "")
                strXPath = ".//nstable";
            else
                strXPath = ".//nstable[@name='" + strNstableName + "']";

            nodeNstable = nodeXpath.SelectSingleNode(strXPath);
            if (nodeNstable != null)
            {
                return 1;
            }

            // ������
            if (strNstableName == "")
                strXPath = "//nstable[@name=''] | //nstable[not(@name)]";  //???������ֵΪ�գ���δ���������
            else
                strXPath = "//nstable[@name='" + strNstableName + "']";

            nodeNstable = nodeXpath.SelectSingleNode(strXPath);
            if (nodeNstable != null)
                return 1;

            strError = "û�ҵ����ֽ�'" + strNstableName + "'��<nstable>�ڵ㡣";
            return 0;
        }

        // ��ն���
        public virtual void Clear()
        {
            this.tableNsClient.Clear();
            this.tableNsServer.Clear();
        }
    }

}
