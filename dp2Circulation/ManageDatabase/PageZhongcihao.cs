using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Web;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;

using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// �ִκ� ����ҳ
    /// </summary>
    public partial class ManagerForm
    {
        bool m_bZhongcihaoChanged = false;

        /// <summary>
        /// �ִκŶ����Ƿ��޸�
        /// </summary>
        public bool ZhongcihaoChanged
        {
            get
            {
                return this.m_bZhongcihaoChanged;
            }
            set
            {
                this.m_bZhongcihaoChanged = value;
                if (value == true)
                    this.toolStripButton_zhongcihao_save.Enabled = true;
                else
                    this.toolStripButton_zhongcihao_save.Enabled = false;
            }
        }

        static string MakeZhongcihaoGroupNodeName(string strGroupName,
    string strZhongcihaoDbName)
        {
            return "��: " + strGroupName + " �ִκſ�='" + strZhongcihaoDbName + "'";
        }

        static string MakeZhongcihaoNstableNodeName(string strNsTableName)
        {
            return "���ֱ�: " + strNsTableName;
        }

        static string MakeZhongcihaoDatabaseNodeName(string strBiblioDbName)
        {
            return "��Ŀ��: " + strBiblioDbName;
        }

        int ListZhongcihao(out string strError)
        {
            strError = "";

            if (this.ZhongcihaoChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�������ִκŶ��屻�޸ĺ���δ���档����ʱˢ�´������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪˢ��? ",
                    "ManagerForm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    return 0;
                }
            }

            this.treeView_zhongcihao.Nodes.Clear();


            string strZhongcihaoXml = "";

            // ����ִκ���ض���
            int nRet = GetZhongcihaoInfo(out strZhongcihaoXml,
                out strError);
            if (nRet == -1)
                return -1;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<zhogncihao />");

            XmlDocumentFragment fragment = dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = strZhongcihaoXml;
            }
            catch (Exception ex)
            {
                strError = "fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                return -1;
            }

            dom.DocumentElement.AppendChild(fragment);

            /*
    <zhongcihao>
        <nstable name="nstable">
            <item prefix="marc" uri="http://dp2003.com/UNIMARC" />
        </nstable>
        <group name="������Ŀ" zhongcihaodb="�ִκ�">
            <database name="����ͼ��" leftfrom="��ȡ���" 

rightxpath="//marc:record/marc:datafield[@tag='905']/marc:subfield[@code='e']/text()" 

titlexpath="//marc:record/marc:datafield[@tag='200']/marc:subfield[@code='a']/text()" 

authorxpath="//marc:record/marc:datafield[@tag='200']/marc:subfield[@code='f' or @code='g']/text()" 

/>
        </group>
    </zhongcihao>
 * */
            XmlNodeList nstable_nodes = dom.DocumentElement.SelectNodes("nstable");
            for (int i = 0; i < nstable_nodes.Count; i++)
            {
                XmlNode node = nstable_nodes[i];

                string strNstableName = DomUtil.GetAttr(node, "name");

                string strNstableCaption = MakeZhongcihaoNstableNodeName(strNstableName);

                TreeNode nstable_treenode = new TreeNode(strNstableCaption,
                    TYPE_ZHONGCIHAO_NSTABLE, TYPE_ZHONGCIHAO_NSTABLE);
                nstable_treenode.Tag = node.OuterXml;

                this.treeView_zhongcihao.Nodes.Add(nstable_treenode);
            }

            XmlNodeList group_nodes = dom.DocumentElement.SelectNodes("group");
            for (int i = 0; i < group_nodes.Count; i++)
            {
                XmlNode node = group_nodes[i];

                string strGroupName = DomUtil.GetAttr(node, "name");
                string strZhongcihaoDbName = DomUtil.GetAttr(node, "zhongcihaodb");

                string strGroupCaption = MakeZhongcihaoGroupNodeName(strGroupName, strZhongcihaoDbName);
                TreeNode group_treenode = new TreeNode(strGroupCaption,
                    TYPE_ZHONGCIHAO_GROUP, TYPE_ZHONGCIHAO_GROUP);
                group_treenode.Tag = node.OuterXml;

                this.treeView_zhongcihao.Nodes.Add(group_treenode);

                // ����database�ڵ�
                XmlNodeList database_nodes = node.SelectNodes("database");
                for (int j = 0; j < database_nodes.Count; j++)
                {
                    XmlNode database_node = database_nodes[j];

                    string strDatabaseName = DomUtil.GetAttr(database_node, "name");

                    string strDatabaseCaption = MakeZhongcihaoDatabaseNodeName(strDatabaseName);

                    TreeNode database_treenode = new TreeNode(strDatabaseCaption,
                        TYPE_ZHONGCIHAO_DATABASE, TYPE_ZHONGCIHAO_DATABASE);
                    database_treenode.Tag = database_node.OuterXml;

                    group_treenode.Nodes.Add(database_treenode);
                }
            }

            this.treeView_zhongcihao.ExpandAll();
            this.ZhongcihaoChanged = false;

            return 1;
        }

        // ����ִκ���ض���
        int GetZhongcihaoInfo(out string strZhongcihaoXml,
            out string strError)
        {
            strError = "";
            strZhongcihaoXml = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ȡ�ִκŶ��� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.GetSystemParameter(
                    stop,
                    "circulation",
                    "zhongcihao",
                    out strZhongcihaoXml,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;
                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

        ERROR1:
            return -1;
        }

        // �����ִκŶ���
        // parameters:
        //      strZhongcihaoXml   �ű�����XML��ע�⣬û�и�Ԫ��
        int SetZhongcihaoDef(string strZhongcihaoXml,
            out string strError)
        {
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ����ִκŶ��� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.SetSystemParameter(
                    stop,
                    "circulation",
                    "zhongcihao",
                    strZhongcihaoXml,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                return (int)lRet;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

        ERROR1:
            return -1;
        }


        // ����ָ����prefix�Ƿ����
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int ExistingPrefix(string strPrefix,
            out string strError)
        {
            strError = "";

            // ������ǰ�Ƿ��Ѿ�����nstable�ڵ�
            TreeNode existing_node = FindExistNstableNode();
            if (existing_node == null)
            {
                strError = "��δ�������ֱ�ڵ�";
                return -1;
            }

            string strXml = (string)existing_node.Tag;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("item");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strCurrentPrefix = DomUtil.GetAttr(node, "prefix");
                if (strPrefix == strCurrentPrefix)
                    return 1;
            }

            return 0;
        }

        // �������ֿռ�URI���Ҷ�Ӧ��prefix
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int FindNamespacePrefix(string strUri,
            out string strPrefix,
            out string strError)
        {
            strPrefix = "";
            strError = "";

            // ������ǰ�Ƿ��Ѿ�����nstable�ڵ�
            TreeNode existing_node = FindExistNstableNode();
            if (existing_node == null)
            {
                strError = "��δ�������ֱ�ڵ㣬����޷����URI '" + strUri + "' ����Ӧ��prefix";
                return -1;
            }

            string strXml = (string)existing_node.Tag;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("item");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strCurrentUri = DomUtil.GetAttr(node, "uri");
                if (strUri.ToLower() == strCurrentUri.ToLower())
                {
                    strPrefix = DomUtil.GetAttr(node, "prefix");
                    return 1;
                }
            }

            return 0;
        }

        // �����Ŀ���syntax
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        int GetBiblioSyntax(string strBiblioDbName,
            out string strSyntax,
            out string strError)
        {
            strSyntax = "";
            strError = "";

            if (String.IsNullOrEmpty(strBiblioDbName) == true)
            {
                strError = "����strBiblioDbName��ֵ����Ϊ��";
                return -1;
            }

            if (String.IsNullOrEmpty(this.AllDatabaseInfoXml) == true)
            {
                return 0;
            }

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(this.AllDatabaseInfoXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strName = DomUtil.GetAttr(node, "name");
                string strType = DomUtil.GetAttr(node, "type");

                if (strName == strBiblioDbName)
                {
                    if (strType != "biblio")
                    {
                        strError = "���ݿ� '" + strBiblioDbName + "' ��������Ŀ�����ͣ����� " + strType + " ����";
                        return -1;
                    }

                    strSyntax = DomUtil.GetAttr(node, "syntax");
                    if (String.IsNullOrEmpty(strSyntax) == true)
                        strSyntax = "unimarc";

                    return 1;
                }
            }

            return 0;
        }

        // ���ָ�����ֵ���Ŀ���Ƿ��Ѿ�����
        // return:
        //      -2  ��ָ������Ŀ�����֣�ʵ������һ���Ѿ����ڵ��������͵Ŀ���
        //      -1  error
        //      0   ��û�д���
        //      1   �Ѿ�����
        int CheckBiblioDbCreated(string strBiblioDbName,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strBiblioDbName) == true)
            {
                strError = "����strBiblioDbName��ֵ����Ϊ��";
                return -1;
            }

            if (String.IsNullOrEmpty(this.AllDatabaseInfoXml) == true)
            {
                return 0;
            }

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(this.AllDatabaseInfoXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strName = DomUtil.GetAttr(node, "name");
                string strType = DomUtil.GetAttr(node, "type");

                if (strType == "biblio")
                {
                    if (strName == strBiblioDbName)
                        return 1;

                    string strEntityDbName = DomUtil.GetAttr(node, "entityDbName");
                    if (strEntityDbName == strBiblioDbName)
                    {
                        strError = "���ⶨ����Ŀ�����͵�ǰ�Ѿ����ڵ�ʵ����� '" + strEntityDbName + "' ������";
                        return -2;
                    }

                    string strOrderDbName = DomUtil.GetAttr(node, "orderDbName");
                    if (strOrderDbName == strBiblioDbName)
                    {
                        strError = "���ⶨ����Ŀ�����͵�ǰ�Ѿ����ڵĶ������� '" + strOrderDbName + "' ������";
                        return -2;
                    }

                    string strIssueDbName = DomUtil.GetAttr(node, "issueDbName");
                    if (strIssueDbName == strBiblioDbName)
                    {
                        strError = "���ⶨ����Ŀ�����͵�ǰ�Ѿ����ڵ��ڿ��� '" + strIssueDbName + "' ������";
                        return -2;
                    }

                }

                string strTypeName = GetTypeName(strType);
                if (strTypeName == null)
                    strTypeName = strType;

                if (strName == strBiblioDbName)
                {
                    strError = "���ⶨ����Ŀ�����͵�ǰ�Ѿ����ڵ�" + strTypeName + "���� '" + strName + "' ������";
                    return -2;
                }

            }

            return 0;
        }

        // ��ñ�ʾ������Ŀ������ֺ����͵�XML����
        // TODO: ��Ȼ����Ķ�����Ŀ�⣬���;��Ƕ������
        internal string GetAllBiblioDbInfoXml()
        {
            if (String.IsNullOrEmpty(this.AllDatabaseInfoXml) == true)
                return null;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(this.AllDatabaseInfoXml);
            }
            catch (Exception /*ex*/)
            {
                // strError = "XMLװ��DOMʱ����: " + ex.Message;
                // return -1;
                Debug.Assert(false, "");
                return "";
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strName = DomUtil.GetAttr(node, "name");
                string strType = DomUtil.GetAttr(node, "type");

                if ("biblio" == strType)
                    continue;

                node.ParentNode.RemoveChild(node);
            }

            return dom.OuterXml;
        }


        // ���treeview���Ѿ�ʹ�ù���ȫ����Ŀ����
        // parameters:
        //      exclude_node    Ҫ�ų���TreeNode�ڵ㡣Ҳ����˵����ڵ��ù�����Ŀ��������������
        List<string> Zhongcihao_GetAllUsedBiblioDbName(TreeNode exclude_node)
        {
            List<string> existing_dbnames = new List<string>();
            for (int i = 0; i < this.treeView_zhongcihao.Nodes.Count; i++)
            {
                TreeNode tree_node = this.treeView_zhongcihao.Nodes[i];
                if (tree_node.ImageIndex != TYPE_ZHONGCIHAO_GROUP)
                    continue;

                // ����group�ڵ���²�
                for (int j = 0; j < tree_node.Nodes.Count; j++)
                {
                    TreeNode database_tree_node = tree_node.Nodes[j];

                    if (database_tree_node == exclude_node)
                        continue;

                    string strXml = (string)database_tree_node.Tag;

                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strXml);
                    }
                    catch (Exception /*ex*/)
                    {
                        Debug.Assert(false, "");
                        continue;
                    }

                    string strDatabaseName = DomUtil.GetAttr(dom.DocumentElement, "name");

                    if (String.IsNullOrEmpty(strDatabaseName) == false)
                        existing_dbnames.Add(strDatabaseName);
                }
            }

            return existing_dbnames;
        }

        TreeNode FindExistNstableNode()
        {
            for (int i = 0; i < this.treeView_zhongcihao.Nodes.Count; i++)
            {
                TreeNode node = this.treeView_zhongcihao.Nodes[i];
                if (node.ImageIndex == TYPE_ZHONGCIHAO_NSTABLE)
                    return node;
            }

            return null;
        }

        // �ύ�ִκŶ����޸�
        int SubmitZhongcihaoDef(out string strError)
        {
            strError = "";
            string strZhongcihaoDef = "";
            int nRet = BuildZhongcihaoDef(out strZhongcihaoDef,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = this.SetZhongcihaoDef(strZhongcihaoDef,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }


        // �����ִκŶ����XMLƬ��
        // ע�����¼�Ƭ�϶��壬û��<zhongcihao>Ԫ����Ϊ����
        int BuildZhongcihaoDef(out string strZhongcihaoDef,
            out string strError)
        {
            strError = "";
            strZhongcihaoDef = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<zhongcihao />");

            for (int i = 0; i < this.treeView_zhongcihao.Nodes.Count; i++)
            {
                TreeNode item = this.treeView_zhongcihao.Nodes[i];

                if (item.ImageIndex == TYPE_ZHONGCIHAO_NSTABLE)
                {
                    string strFragmentXml = (string)item.Tag;
                    XmlDocumentFragment fragment = dom.CreateDocumentFragment();
                    try
                    {
                        fragment.InnerXml = strFragmentXml;
                    }
                    catch (Exception ex)
                    {
                        strError = "nstable fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                        return -1;
                    }

                    dom.DocumentElement.AppendChild(fragment);
                }
                else if (item.ImageIndex == TYPE_ZHONGCIHAO_GROUP)
                {
                    // ȡ��name��zhongcihaodb��������
                    string strXml = (string)item.Tag;

                    XmlDocument temp_dom = new XmlDocument();
                    try
                    {
                        temp_dom.LoadXml(strXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "group�ڵ��XMLװ��DOMʱ����: " + ex.Message;
                        return -1;
                    }

                    string strName = DomUtil.GetAttr(temp_dom.DocumentElement,
                        "name");
                    string strZhongcihaoDbName = DomUtil.GetAttr(temp_dom.DocumentElement,
                        "zhongcihaodb");

                    XmlNode group_node = dom.CreateElement("group");
                    DomUtil.SetAttr(group_node, "name", strName);
                    DomUtil.SetAttr(group_node, "zhongcihaodb", strZhongcihaoDbName);

                    dom.DocumentElement.AppendChild(group_node);

                    for (int j = 0; j < item.Nodes.Count; j++)
                    {
                        TreeNode database_treenode = item.Nodes[j];

                        string strXmlFragment = (string)database_treenode.Tag;

                        XmlDocumentFragment fragment = dom.CreateDocumentFragment();
                        try
                        {
                            fragment.InnerXml = strXmlFragment;
                        }
                        catch (Exception ex)
                        {
                            strError = "database fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                            return -1;
                        }

                        group_node.AppendChild(fragment);
                    }
                }
            }

            strZhongcihaoDef = dom.DocumentElement.InnerXml;

            return 0;
        }

        // ���ָ�����ֵ��ִκſ��Ƿ��Ѿ�����
        // return:
        //      -2  ��ָ�����ִκſ����֣�ʵ������һ���Ѿ����ڵ��������͵Ŀ���
        //      -1  error
        //      0   ��û�д���
        //      1   �Ѿ�����
        int CheckZhongcihaoDbCreated(string strZhongcihaoDbName,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strZhongcihaoDbName) == true)
            {
                strError = "����strZhongcihaoDbName��ֵ����Ϊ��";
                return -1;
            }

            if (String.IsNullOrEmpty(this.AllDatabaseInfoXml) == true)
            {
                return 0;
            }

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(this.AllDatabaseInfoXml);
            }
            catch (Exception ex)
            {
                strError = "XMLװ��DOMʱ����: " + ex.Message;
                return -1;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strName = DomUtil.GetAttr(node, "name");
                string strType = DomUtil.GetAttr(node, "type");

                if ("zhongcihao" == strType)
                {
                    if (strName == strZhongcihaoDbName)
                        return 1;
                }

                if (strType == "biblio")
                {
                    if (strName == strZhongcihaoDbName)
                    {
                        strError = "���ⶨ���ִκſ����͵�ǰ�Ѿ����ڵ�С��Ŀ���� '" + strName + "' ������";
                        return -2;
                    }

                    string strEntityDbName = DomUtil.GetAttr(node, "entityDbName");
                    if (strEntityDbName == strZhongcihaoDbName)
                    {
                        strError = "���ⶨ���ִκſ����͵�ǰ�Ѿ����ڵ�ʵ����� '" + strEntityDbName + "' ������";
                        return -2;
                    }

                    string strOrderDbName = DomUtil.GetAttr(node, "orderDbName");
                    if (strOrderDbName == strZhongcihaoDbName)
                    {
                        strError = "���ⶨ���ִκſ����͵�ǰ�Ѿ����ڵĶ������� '" + strOrderDbName + "' ������";
                        return -2;
                    }

                    string strIssueDbName = DomUtil.GetAttr(node, "issueDbName");
                    if (strIssueDbName == strZhongcihaoDbName)
                    {
                        strError = "���ⶨ���ִκſ����͵�ǰ�Ѿ����ڵ��ڿ��� '" + strIssueDbName + "' ������";
                        return -2;
                    }

                }

                string strTypeName = GetTypeName(strType);
                if (strTypeName == null)
                    strTypeName = strType;

                if (strName == strZhongcihaoDbName)
                {
                    strError = "���ⶨ���ִκſ����͵�ǰ�Ѿ����ڵ�" + strTypeName + "���� '" + strName + "' ������";
                    return -2;
                }

            }

            return 0;
        }

        string GetAllZhongcihaoDbInfoXml()
        {
            if (String.IsNullOrEmpty(this.AllDatabaseInfoXml) == true)
                return null;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(this.AllDatabaseInfoXml);
            }
            catch (Exception /*ex*/)
            {
                // strError = "XMLװ��DOMʱ����: " + ex.Message;
                // return -1;
                Debug.Assert(false, "");
                return "";
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strName = DomUtil.GetAttr(node, "name");
                string strType = DomUtil.GetAttr(node, "type");

                if ("zhongcihao" == strType)
                    continue;

                node.ParentNode.RemoveChild(node);
            }

            return dom.OuterXml;
        }

        // ���treeview_zhongcihao���Ѿ�ʹ�ù���ȫ���ִκ���
        // parameters:
        //      exclude_node    Ҫ�ų���TreeNode�ڵ㡣Ҳ����˵����ڵ��ù����ִκſⲻ��������
        List<string> GetAllUsedZhongcihaoDbName(TreeNode exclude_node)
        {
            List<string> existing_dbnames = new List<string>();
            for (int i = 0; i < this.treeView_zhongcihao.Nodes.Count; i++)
            {
                TreeNode tree_node = this.treeView_zhongcihao.Nodes[i];
                if (tree_node.ImageIndex != TYPE_ZHONGCIHAO_GROUP)
                    continue;
                if (tree_node == exclude_node)
                    continue;

                string strXml = (string)tree_node.Tag;

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strXml);
                }
                catch (Exception /*ex*/)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                string strZhongcihaoDbName = DomUtil.GetAttr(dom.DocumentElement, "zhongcihaodb");

                if (String.IsNullOrEmpty(strZhongcihaoDbName) == false)
                    existing_dbnames.Add(strZhongcihaoDbName);
            }

            return existing_dbnames;
        }

        void menu_zhongcihao_up_Click(object sender, EventArgs e)
        {
            ZhongcihaoMoveUpDown(true);
        }

        void menu_zhongcihao_down_Click(object sender, EventArgs e)
        {
            ZhongcihaoMoveUpDown(false);
        }

        void ZhongcihaoMoveUpDown(bool bUp)
        {
            string strError = "";
            // int nRet = 0;

            // ��ǰ��ѡ���node
            if (this.treeView_zhongcihao.SelectedNode == null)
            {
                MessageBox.Show("��δѡ��Ҫ���������ƶ��Ľڵ�");
                return;
            }

            TreeNodeCollection nodes = null;

            TreeNode parent = treeView_zhongcihao.SelectedNode.Parent;

            if (parent == null)
                nodes = this.treeView_zhongcihao.Nodes;
            else
                nodes = parent.Nodes;

            TreeNode node = treeView_zhongcihao.SelectedNode;

            int index = nodes.IndexOf(node);

            Debug.Assert(index != -1, "");

            if (bUp == true)
            {
                if (index == 0)
                {
                    strError = "�Ѿ���ͷ";
                    goto ERROR1;
                }

                nodes.Remove(node);
                index--;
                nodes.Insert(index, node);
            }
            if (bUp == false)
            {
                if (index >= nodes.Count - 1)
                {
                    strError = "�Ѿ���β";
                    goto ERROR1;
                }

                nodes.Remove(node);
                index++;
                nodes.Insert(index, node);

            }

            this.treeView_zhongcihao.SelectedNode = node;


            this.ZhongcihaoChanged = true;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

    }
}
