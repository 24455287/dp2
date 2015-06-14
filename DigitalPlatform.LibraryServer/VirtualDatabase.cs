using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using DigitalPlatform.Xml;
using DigitalPlatform.rms.Client;
using DigitalPlatform.rms.Client.rmsws_localhost;
using DigitalPlatform.Text;

namespace DigitalPlatform.LibraryServer
{

    /// <summary>
    /// һ���������ݿ�
    /// </summary>
    public class VirtualDatabase
    {
        internal XmlNode nodeDatabase = null;

        // �Ƿ���<all>֮��
        // ȱʡΪfalse��
        // �������������Ʋ�ϣ��������<all>��Χ�ڵ����ݿ⣬���硰�û�����
        public bool NotInAll
        {
            get
            {
                if (nodeDatabase == null)
                    return false;

                bool bValue = false;
                string strError = "";
                        // ��������͵����Բ���ֵ
        // return:
        //      -1  ��������nValue���Ѿ�����nDefaultValueֵ�����Բ��Ӿ����ֱ��ʹ��
        //      0   ���������ȷ����Ĳ���ֵ
        //      1   ����û�ж��壬��˴�����ȱʡ����ֵ����
                int nRet = DomUtil.GetBooleanParam(nodeDatabase,
                    "notInAll",
                    false,
                    out bValue,
                    out strError);

                return bValue;
            }
        }

        public bool IsVirtual
        {
            get
            {
                if (nodeDatabase == null)
                    return false;

                if (nodeDatabase.Name == "database")
                    return false;

                return true;
            }
        }

        // ����ض������µ�From�����б�
        public List<string> GetFroms(string strLang)
        {
            List<string> results = new List<string>();
            XmlNodeList nodes = this.nodeDatabase.SelectNodes("from");
            for (int i = 0; i < nodes.Count; i++)
            {
                string strName = DomUtil.GetCaption(strLang, nodes[i]);
                if (strName == null)
                {   // ����������û�ж���<caption>Ԫ�أ������<from>Ԫ�ص�name����ֵ
                    strName = DomUtil.GetAttr(nodes[i], "name");
                    if (String.IsNullOrEmpty(strName) == true)
                        continue;   // ʵ��û�У�ֻ������
                }
                results.Add(strName);
            }

            return results;
        }



        // ����ض������µ����ݿ���
        public string GetName(string strLang)
        {

            /*

            XmlNode node = this.nodeDatabase.SelectSingleNode("caption[@lang='" + strLang + "']");
            if (node == null)
            {
                string strLangLeft = "";
                string strLangRight = "";

                SplitLang(strLang,
                   out strLangLeft,
                   out strLangRight);

                // ����<caption>Ԫ��
                XmlNodeList nodes = this.nodeDatabase.SelectNodes("caption");

                for (int i = 0; i < nodes.Count; i++)
                {
                    string strThisLang = DomUtil.GetAttr(nodes[i], "lang");

                    if (strThisLang == strLangLeft)
                        return nodes[i].InnerText;
                }

                node = this.nodeDatabase.SelectSingleNode("caption");
                if (node != null)
                    return node.InnerText;
                return null;    // not found
            }

            return node.InnerText;
             */

            // ��һ��Ԫ�ص��¼�<caption>Ԫ����, ��ȡ���Է��ϵ�����ֵ
            string strCaption = DomUtil.GetCaption(strLang,
                this.nodeDatabase);

            if (String.IsNullOrEmpty(strCaption) == true)
            {
                if (IsVirtual == false)
                    return DomUtil.GetAttr(this.nodeDatabase, "name");
            }

            return strCaption;
        }

        // ��δָ�����Ե�����»��ȫ�����ݿ���
        // 2009/6/17
        public List<string> GetAllNames()
        {
            List<string> results = new List<string>();

            XmlNodeList nodes = this.nodeDatabase.SelectNodes("caption");
            for(int i=0;i<nodes.Count;i++)
            {
                results.Add(nodes[i].InnerText);
            }

            return results;
        }

        /*
        // �г����õ�From Style����
        public List<string> GetStyles()
        {
            List<string> results = new List<string>();

            XmlNodeList nodes = nodeDatabase.SelectNodes("from");
            for (int i = 0; i < nodes.Count; i++)
            {
                string strStyle = DomUtil.GetAttr(nodes[i], "style");
                results.Add(strStyle);
            }

            return results;
        }
         */

        // ��ʼ��From��һЩ����, �Ա㽫�������������ٷ���
        // ��<from>Ԫ����Ҫ��������<database>Ԫ��, ���Ǹ�From���õ����ݿ���б�
        // ��Щ��Ϣ���������ʼ���ķ���, �����˹�ȥ����
        // return:
        //      -1  ����
        //      0   ��DOMû���޸�
        //      1   ��DOM�������޸�
        public int InitialFromProperty(
//            ResInfoItem[] root_dir_results,
            Hashtable db_dir_results,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (nodeDatabase == null)
            {
                strError = "nodeDatabase��δ����ֵ";
                return -1;
            }

            if (this.IsVirtual != true)
            {
                strError = "�ú���ֻ������<virtualDatabase>Ԫ�صĳ�ʼ��";
                return -1;
            }

            bool bChanged = false;
            XmlNodeList dbnodes = this.nodeDatabase.SelectNodes("database");

            // �г�����<from>Ԫ��
            XmlNodeList fromnodes = this.nodeDatabase.SelectNodes("from");
            for (int i = 0; i < fromnodes.Count; i++)
            {
                string strFromName = DomUtil.GetAttr(fromnodes[i], "name");
                string strFromStyle = DomUtil.GetAttr(fromnodes[i], "style");

                // ɾ��ԭ���ӽڵ���<caption>�����Ԫ��<database>
                RemoveDatabaseChildren(fromnodes[i]);

                // �ӿ��õ����ݿ��б���, ��������������from�����ϵ�
                for (int j = 0; j < dbnodes.Count; j++)
                {
                    string strDbName = DomUtil.GetAttr(dbnodes[j], "name");
                    // BUG: string strStyle = DomUtil.GetAttr(dbnodes[j], "style");

                    nRet = MatchFromStyle(strDbName,
                        strFromStyle,   //strStyle,
                        db_dir_results);
                    if (nRet == 0)
                        continue;

                    // ��<from>Ԫ���¼���һ��<database>Ԫ��
                    XmlNode newnode = fromnodes[i].OwnerDocument.CreateElement("database");
                    fromnodes[i].AppendChild(newnode);
                    DomUtil.SetAttr(newnode, "name", strDbName);
                    bChanged = true;
                }

            }

            if (bChanged == true)
                return 1;

            return 0;
        }

        // ��ʼ�����ݿ��From��һЩ����, �Ա㽫�������������ٷ���
        // ��<database>Ԫ����Ҫ��������<from>Ԫ��
        // ��Щ��Ϣ���������ʼ���ķ���, �����˹�ȥ����
        // return:
        //      -1  ����
        //      0   ��DOMû���޸�
        //      1   ��DOM�������޸�
        public int InitialAllProperty(
            ResInfoItem[] root_dir_results,
            Hashtable db_dir_results,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (nodeDatabase == null)
            {
                strError = "nodeDatabase��δ����ֵ";
                return -1;
            }

            if (this.IsVirtual != false)
            {
                strError = "�ú���ֻ������<database>Ԫ�صĳ�ʼ��";
                return -1;
            }

            bool bChanged = false;

            string strDbName = DomUtil.GetAttr(nodeDatabase, "name");

            RemoveChildren(nodeDatabase);
            bChanged = true;

            ResInfoItem dbitem = KernelDbInfo.GetDbItem(
                root_dir_results,
                strDbName);
            if (dbitem == null)
            {
                strError = "���ݿ��ں˸�Ŀ¼��û���ҵ�����Ϊ '" +strDbName+ "' �����ݿ������������ݿ��Ѿ���ɾ�������޸�dp2Library��library.xml�ļ���<virtualDatabases>Ԫ���µ��й�����";
                return -1;
            }
                
            // ���¼�����<caption>Ԫ��
            for (int i = 0; i < dbitem.Names.Length; i++)
            {
                string strText = dbitem.Names[i];
                nRet = strText.IndexOf(":");
                if (nRet == -1)
                {
                    strError = "names�ַ��� '" +strText+ "' ��ʽ����ȷ��";
                    return -1;
                }
                string strLang = strText.Substring(0, nRet);
                string strName = strText.Substring(nRet + 1);

                XmlNode newnode = nodeDatabase.OwnerDocument.CreateElement("caption");
                newnode = nodeDatabase.AppendChild(newnode);
                DomUtil.SetAttr(newnode, "lang", strLang);
                DomUtil.SetNodeText(newnode, strName);
                bChanged = true;
            }

            // 
            ResInfoItem [] fromitems = (ResInfoItem[])db_dir_results[strDbName];
            if (fromitems == null)
            {
                strError = "db_dir_results��û���ҵ����� '" + strDbName + "' ���¼�Ŀ¼����";
                return -1;
            }

            for (int i = 0; i < fromitems.Length; i++)
            {
                ResInfoItem item = fromitems[i];
                if (item.Type != ResTree.RESTYPE_FROM)
                    continue;

                // ����<from>Ԫ��
                XmlNode fromnode = nodeDatabase.OwnerDocument.CreateElement("from");
                fromnode = nodeDatabase.AppendChild(fromnode);
                DomUtil.SetAttr(fromnode, "name", item.Name);    // ��ǰ���������µ�����

                // ��ǰ������
                DomUtil.SetAttr(fromnode, "style", item.TypeString);    // 2011/1/21
                bChanged = true;

                if (item.Names == null)
                    continue;

                // ����caption
                for (int j = 0; j < item.Names.Length; j++)
                {
                    string strText = item.Names[j];
                    nRet = strText.IndexOf(":");
                    if (nRet == -1)
                    {
                        strError = "names�ַ��� '" + strText + "' ��ʽ����ȷ��";
                        return -1;
                    }

                    string strLang = strText.Substring(0, nRet);
                    string strName = strText.Substring(nRet + 1);

                    XmlNode newnode = fromnode.OwnerDocument.CreateElement("caption");
                    newnode = fromnode.AppendChild(newnode);
                    DomUtil.SetAttr(newnode, "lang", strLang);
                    DomUtil.SetNodeText(newnode, strName);
                    bChanged = true;
                }

            }

            if (bChanged == true)
                return 1;
            return 0;
        }

        /*
        // ��Ŀ¼�����л��ָ�����ֵ�����
        static ResInfoItem GetDbItem(
            ResInfoItem [] root_dir_results,
            string strDbName)
        {
            for (int i = 0; i < root_dir_results.Length; i++)
            {
                ResInfoItem info = root_dir_results[i];

                if (info.Type != ResTree.RESTYPE_DB)
                    continue;

                if (info.Name == strDbName)
                    return info;

            }

            return null;
        }
         * */

        // ɾ���¼���ȫ��Ԫ��
        static void RemoveChildren(XmlNode parent)
        {
            for (int i = 0; i < parent.ChildNodes.Count; i++)
            {
                XmlNode node = parent.ChildNodes[i];
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                parent.RemoveChild(node);
                i--;
            }
        }


        static void RemoveDatabaseChildren(XmlNode parent)
        {
            for (int i = 0; i < parent.ChildNodes.Count; i++)
            {
                XmlNode node = parent.ChildNodes[i];
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                if (node.Name == "database")
                {
                    parent.RemoveChild(node);
                    i--;
                }
                    
            }
        }

        static int MatchFromStyle(string strDbName,
            string strFromStyle,
            Hashtable db_dir_results)
        {
            ResInfoItem[] infos = (ResInfoItem[])db_dir_results[strDbName];
            if (infos == null)
                return 0;
            for (int i = 0; i < infos.Length; i++)
            {
                if (infos[i].Type != ResTree.RESTYPE_FROM)
                    continue;
                if (StringUtil.IsInList(strFromStyle, infos[i].TypeString) == true)
                    return 1;
            }

            return 0;
        }

        // ���������From�������ʵ��From�������From�ǹ��������µ�����
        // ���ƥ����From���������ַ������Զ��ŷָ� 2007/7/8����
        public string GetRealFromName(
            Hashtable db_dir_results,
            string strRealDbName,
            string strVirtualFromName)
        {
            List<string> styles = new List<string>();

            if (String.IsNullOrEmpty(strVirtualFromName) == true
                || strVirtualFromName == "<ȫ��>"
                || strVirtualFromName.ToLower() == "<all>")
            {
                XmlNodeList nodes = this.nodeDatabase.SelectNodes("from");

                for (int i = 0; i < nodes.Count; i++)
                {
                    string strStyle = DomUtil.GetAttr(nodes[i], "style");

                    styles.Add(strStyle);
                }

            }
            else
            {

                XmlNode node = null;
                XmlNodeList nodes = this.nodeDatabase.SelectNodes("from/caption");
                for (int i = 0; i < nodes.Count; i++)
                {
                    node = nodes[i];
                    if (strVirtualFromName == node.InnerText.Trim())
                        goto FOUND;
                }
                return null;    // not found

                FOUND:

                string strStyle = DomUtil.GetAttr(node.ParentNode, "style");

                styles.Add(strStyle);
            }

            List<string> results = new List<string>();

            for (int i = 0; i < styles.Count; i++)
            {
                string strStyle = styles[i];

                // ��������From�����У��ҵ�style���ϵ�
                ResInfoItem[] froms = (ResInfoItem[])db_dir_results[strRealDbName];

                if (froms == null)
                {
                    // return null;    // fromĿ¼�����Ȼû���ҵ�
                    continue;
                }

                for (int j = 0; j < froms.Length; j++)
                {
                    ResInfoItem item = froms[j];
                    string strStyles = item.TypeString;
                    /*
                    if (StringUtil.IsInList(strStyle, strStyles) == true)
                    {
                        return item.Name;
                    }
                     * */
                    if (StringUtil.IsInList(strStyle, strStyles) == true)
                    {
                        results.Add(item.Name);
                    }

                }
            }

            if (results.Count == 0)
                return null;    // styleû�з���ƥ���

            string[] list = new string[results.Count];
            results.CopyTo(list);

            return String.Join(",", list);
        }

        // ���������������ʵ���ݿ���
        public List<string> GetRealDbNames()
        {
            List<string> results = new List<string>();

            XmlNodeList nodes = this.nodeDatabase.SelectNodes("descendant-or-self::database");
            for (int i = 0; i < nodes.Count; i++)
            {
                results.Add(DomUtil.GetAttr(nodes[i], "name"));
            }

            return results;
        }

    }


    /// <summary>
    /// �������ݿ�ļ���
    /// </summary>
    public class VirtualDatabaseCollection : List<VirtualDatabase>
    {
        public string ServerUrl = "";
        public string Lang = "zh";

        public ResInfoItem[] root_dir_results = null;  // ��Ŀ¼��Ϣ
        public Hashtable db_dir_results = null;    // ����Ŀ¼��Ϣ

        // 2009/6/17 changed
        public VirtualDatabase this[string strDbName]
        {
            get
            {
                for (int i = 0; i < this.Count; i++)
                {
                    VirtualDatabase vdb = this[i];

                    // �����Ƿ�����⣬��Ҫ������<caption>
                    {
                        XmlNodeList nodes = vdb.nodeDatabase.SelectNodes("caption");
                        for (int j = 0; j < nodes.Count; j++)
                        {
                            if (nodes[j].InnerText == strDbName)
                                return vdb;
                        }
                    }

                    // �����������⣬���ж�һ��
                    if (vdb.IsVirtual == false)
                    {
                        if (vdb.GetName(null) == strDbName)
                            return vdb;
                    }

                }

                return null;
            }
        }


#if NOOOOOOOOOOOOOOOOOOO
        public VirtualDatabase this[string strDbName]
        {
            get
            {
                for (int i = 0; i < this.Count; i++)
                {
                    VirtualDatabase vdb = this[i];

                    if (vdb.IsVirtual == true)
                    {
                        XmlNodeList nodes = vdb.nodeDatabase.SelectNodes("caption");
                        for (int j = 0; j < nodes.Count; j++)
                        {
                            if (nodes[j].InnerText == strDbName)
                                return vdb;
                        }
                    }
                    else
                    {
                        /*
                        if (vdb.GetName(null) == strDbName)
                            return vdb;
                         * */
                        // TODO: ��Сд�������⣿
                        List<string> all_names = vdb.GetAllNames();
                        if (all_names.IndexOf(strDbName) != -1)
                            return vdb;
                    }

                }

                return null;
            }
        }
#endif

        // ���һ����ͨ���ݿ�Ķ���(�������ݿ���captions��froms name captions)
        // ��ʽΪ
        /*
        <database>
            <caption lang="zh-cn">����ͼ��</caption>
            <caption lang="en">Chinese book</caption>
            <from style="title">
                <caption lang="zh-cn">����</caption>
                <caption lang="en">Title</caption>
            </from>
            ...
            <from name="__id" />
        </database>         * */
        // return:
        //      -1  error
        //      0   not found such database
        //      1   found and succeed
        public int GetDatabaseDef(
            string strDbName,
            out string strDef,
            out string strError)
        {
            strError = "";
            strDef = "";

            int nRet = 0;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<database />");


            {
                ResInfoItem dbitem = KernelDbInfo.GetDbItem(
                    this.root_dir_results,
                    strDbName);
                if (dbitem == null)
                {
                    strError = "��Ŀ¼��û���ҵ�����Ϊ '" + strDbName + "' �����ݿ�Ŀ¼����";
                    return 0;
                }

                // �ڸ��¼���<caption>Ԫ��
                for (int i = 0; i < dbitem.Names.Length; i++)
                {
                    string strText = dbitem.Names[i];
                    nRet = strText.IndexOf(":");
                    if (nRet == -1)
                    {
                        strError = "names�ַ��� '" + strText + "' ��ʽ����ȷ��";
                        return -1;
                    }
                    string strLang = strText.Substring(0, nRet);
                    string strName = strText.Substring(nRet + 1);

                    XmlNode newnode = dom.CreateElement("caption");
                    dom.DocumentElement.AppendChild(newnode);
                    DomUtil.SetAttr(newnode, "lang", strLang);
                    DomUtil.SetNodeText(newnode, strName);
                }
            }

            // 
            ResInfoItem[] fromitems = (ResInfoItem[])this.db_dir_results[strDbName];
            if (fromitems == null)
            {
                strError = "db_dir_results��û���ҵ����� '" + strDbName + "' ���¼�Ŀ¼����";
                return 0;
            }

            for (int i = 0; i < fromitems.Length; i++)
            {
                ResInfoItem item = fromitems[i];
                if (item.Type != ResTree.RESTYPE_FROM)
                    continue;

                // ����<from>Ԫ��
                XmlNode fromnode = dom.CreateElement("from");
                dom.DocumentElement.AppendChild(fromnode);
                DomUtil.SetAttr(fromnode, "style", item.TypeString);    // style

                if (item.Names == null)
                    continue;

                // ����caption
                for (int j = 0; j < item.Names.Length; j++)
                {
                    string strText = item.Names[j];
                    nRet = strText.IndexOf(":");
                    if (nRet == -1)
                    {
                        strError = "names�ַ��� '" + strText + "' ��ʽ����ȷ��";
                        return -1;
                    }

                    string strLang = strText.Substring(0, nRet);
                    string strName = strText.Substring(nRet + 1);

                    XmlNode newnode = dom.CreateElement("caption");
                    fromnode.AppendChild(newnode);
                    DomUtil.SetAttr(newnode, "lang", strLang);
                    DomUtil.SetNodeText(newnode, strName);
                }

            }

            strDef = dom.OuterXml;

            return 1;
        }


        // ���캯��
        // ����XML�����ļ�, �ӷ�������ȡĿ¼��Ϣ, ��ʼ�����ݽṹ
        // parameters:
        //      biblio_dbs_root <itemdbgroup>Ԫ��
        public int Initial(XmlNode root,
            RmsChannelCollection Channels,
            string strServerUrl,
            XmlNode biblio_dbs_root,
            out string strError)
        {
            strError = "";

            this.ServerUrl = strServerUrl;

            this.root_dir_results = null;
            this.db_dir_results = null;

        // �г�Ŀ¼��Ϣ
        // �г�2�����ڶ�����Hashtable��
            int nRet = GetDirInfo(Channels,
                strServerUrl,
                out root_dir_results,
                out db_dir_results,
                out strError);
            if (nRet == -1)
                return -1;

            // �г����������XML�ڵ�
            XmlNodeList virtualnodes = root.SelectNodes("virtualDatabase");
            for (int i = 0; i < root.ChildNodes.Count; i++)
            {
                XmlNode node = root.ChildNodes[i];

                if (node.NodeType != XmlNodeType.Element)
                    continue;

                if (node.Name == "virtualDatabase")
                {

                    // �����������ݿ����
                    VirtualDatabase vdb = new VirtualDatabase();
                    vdb.nodeDatabase = node;

                    // ��ʼ��From��һЩ����, �Ա㽫�������������ٷ���
                    // ��<from>Ԫ����Ҫ��������<database>Ԫ��, ���Ǹ�From���õ����ݿ���б�
                    // ��Щ��Ϣ���������ʼ���ķ���, �����˹�ȥ����
                    nRet = vdb.InitialFromProperty(
                        db_dir_results,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    this.Add(vdb);
                    continue;
                }

                if (node.Name == "database")    // ��ͨ��
                {

                    // ������ͨ���ݿ����
                    VirtualDatabase vdb = new VirtualDatabase();
                    vdb.nodeDatabase = node;

                    // Ҫ������ݿ�����<itemdbgroup>�����Ƿ����
                    string strDbName = DomUtil.GetAttr(node, "name");
                    if (biblio_dbs_root != null)
                    {
                        XmlNode nodeBiblio = biblio_dbs_root.SelectSingleNode("database[@biblioDbName='"+strDbName+"']");
                        if (nodeBiblio == null)
                        {
                            strError = "��Ŀ�� '"+strDbName+"' ��<itemdbgroup>�ڲ����ڶ��壬��ȴ��<virtualDatabases>�ڴ��ڡ������<virtualDatabases>����¾��ˣ���Ҫ���ù����ܼ��������޸ģ�����ֱ����library.xml���޸�";
                            return -1;
                        }
                    }

                    nRet = vdb.InitialAllProperty(
                        root_dir_results,
                        db_dir_results,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    this.Add(vdb);
                    continue;
                }
            }

            return 0;
        }

        // �г�Ŀ¼��Ϣ
        // �г�2�����ڶ�����Hashtable��
        int GetDirInfo(RmsChannelCollection Channels,
            string strServerUrl,
            out ResInfoItem[] root_dir_results,
            out Hashtable db_dir_results,
            out string strError)
        {
            root_dir_results = null;
            db_dir_results = null;

            RmsChannel channel = Channels.GetChannel(this.ServerUrl);

            // �г��������ݿ�
            root_dir_results = null;

            long lRet = channel.DoDir("",
                this.Lang,
                "alllang",
                out root_dir_results,
                out strError);
            if (lRet == -1)
                return -1;

            db_dir_results = new Hashtable();

            for (int i = 0; i < root_dir_results.Length; i++)
            {
                ResInfoItem info = root_dir_results[i];
                if (info.Type != ResTree.RESTYPE_DB)
                    continue;

                ResInfoItem[] db_dir_result = null;

                lRet = channel.DoDir(info.Name,
                       this.Lang,
                       "alllang",
                       out db_dir_result,
                       out strError);
                if (lRet == -1)
                    return -1;

                db_dir_results[info.Name] = db_dir_result;
            }


            return 0;
        }

        /*
        // �������ݿ��ԭʼ����
        int FindDatabaseOriginDef(string strDatabaseName,
            ResInfoItem[] dir_results,
            out string strError)
        {
            strError = "";


            return 1;
        }
         */

    }
}
