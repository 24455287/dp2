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
    /// �ż���ϵ ����ҳ
    /// </summary>
    public partial class ManagerForm
    {
        bool m_bArrangementChanged = false;

        /// <summary>
        /// �ż���ϵ�����Ƿ��޸�
        /// </summary>
        public bool ArrangementChanged
        {
            get
            {
                return this.m_bArrangementChanged;
            }
            set
            {
                this.m_bArrangementChanged = value;
                if (value == true)
                    this.toolStripButton_arrangement_save.Enabled = true;
                else
                    this.toolStripButton_arrangement_save.Enabled = false;
            }
        }

        static string MakeArrangementGroupNodeName(string strGroupName,
            string strClassType,
            string strQufenhaoType,
            string strZhongcihaoDbName,
            string strCallNumberStyle)
        {
            string strResult = "�ż���ϵ: " + strGroupName + " ���=" + strClassType + " ���ֺ�=" + strQufenhaoType;

            if (String.IsNullOrEmpty(strZhongcihaoDbName) == false)
                strResult += " �ִκſ�='" + strZhongcihaoDbName + "'";

            if (string.IsNullOrEmpty(strCallNumberStyle) == false)
                strResult += " ��ȡ����̬='" + strCallNumberStyle + "'";

            return strResult;
        }

        static string MakeArrangementLocationNodeName(string strLocationName)
        {
            /*
            if (String.IsNullOrEmpty(strLocationName) == true)
                return "<��>";

            return strLocationName;
             * */
            return ArrangementLocationDialog.GetDisplayString(strLocationName);
        }

        // �г��ż���ϵ����
        int ListArrangement(out string strError)
        {
            strError = "";

            if (this.ArrangementChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�������ż���ϵ���屻�޸ĺ���δ���档����ʱˢ�´������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪˢ��? ",
                    "ManagerForm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    return 0;
                }
            }

            this.treeView_arrangement.Nodes.Clear();


            string strArrangementXml = "";

            // ����ִκ���ض���
            int nRet = GetArrangementInfo(out strArrangementXml,
                out strError);
            if (nRet == -1)
                return -1;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<callNumber />");

            XmlDocumentFragment fragment = dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = strArrangementXml;
            }
            catch (Exception ex)
            {
                strError = "fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                return -1;
            }

            dom.DocumentElement.AppendChild(fragment);

            /*
    <callNumber>
        <group name="����" classType="��ͼ��" qufenhaoType="GCAT" zhongcihaodb="�ִκ�">
            <location name="���ؿ�" />
            <location name="��ͨ��" />
        </group>
        <group name="Ӣ��" classType="��ͼ��" qufenhaoType="zhongcihao" zhongcihaodb="���ִκſ�">
            <location name="Ӣ�Ļ��ؿ�" />
            <location name="Ӣ����ͨ��" />
        </group>
    </callNumber>
 * */
            XmlNodeList group_nodes = dom.DocumentElement.SelectNodes("group");
            for (int i = 0; i < group_nodes.Count; i++)
            {
                XmlNode node = group_nodes[i];

                string strGroupName = DomUtil.GetAttr(node, "name");
                string strClassType = DomUtil.GetAttr(node, "classType");
                string strQufenhaoType = DomUtil.GetAttr(node, "qufenhaoType");
                string strZhongcihaoDbName = DomUtil.GetAttr(node, "zhongcihaodb");
                string strCallNumberStyle = DomUtil.GetAttr(node, "callNumberStyle");

                string strGroupCaption = MakeArrangementGroupNodeName(strGroupName,
                    strClassType,
                    strQufenhaoType,
                    strZhongcihaoDbName,
                    strCallNumberStyle);
                TreeNode group_treenode = new TreeNode(strGroupCaption,
                    TYPE_ARRANGEMENT_GROUP, TYPE_ARRANGEMENT_GROUP);
                group_treenode.Tag = node.OuterXml;

                this.treeView_arrangement.Nodes.Add(group_treenode);

                // ����location�ڵ�
                XmlNodeList location_nodes = node.SelectNodes("location");
                for (int j = 0; j < location_nodes.Count; j++)
                {
                    XmlNode location_node = location_nodes[j];

                    string strLocationName = DomUtil.GetAttr(location_node, "name");

                    string strLocationCaption = MakeArrangementLocationNodeName(strLocationName);

                    TreeNode location_treenode = new TreeNode(strLocationCaption,
                        TYPE_ARRANGEMENT_LOCATION, TYPE_ARRANGEMENT_LOCATION);
                    location_treenode.Tag = location_node.OuterXml;

                    group_treenode.Nodes.Add(location_treenode);
                }
            }

            this.treeView_arrangement.ExpandAll();
            this.ArrangementChanged = false;

            return 1;
        }

        // ����ż���ϵ��ض���
        int GetArrangementInfo(out string strArrangementXml,
            out string strError)
        {
            strError = "";
            strArrangementXml = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ȡ�ż���ϵ���� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.GetSystemParameter(
                    stop,
                    "circulation",
                    "callNumber",
                    out strArrangementXml,
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

        // �����ż���ϵ����
        // parameters:
        //      strArrangementXml   �ű�����XML��ע�⣬û�и�Ԫ��
        int SetArrangementDef(string strArrangementXml,
            out string strError)
        {
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ����ż���ϵ���� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.SetSystemParameter(
                    stop,
                    "circulation",
                    "callNumber",
                    strArrangementXml,
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

        // �ύ�ż���ϵ�����޸�
        int SubmitArrangementDef(out string strError)
        {
            strError = "";
            string strArrangementDef = "";
            int nRet = BuildArrangementDef(out strArrangementDef,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = this.SetArrangementDef(strArrangementDef,
                out strError);
            if (nRet == -1)
                return -1;

            this.MainForm.GetCallNumberInfo();  // 2009/6/5 ˢ���ڴ��в����ľɶ�����Ϣ

            return 0;
        }

        // �����ż���ϵ�����XMLƬ��
        // ע�����¼�Ƭ�϶��壬û��<callNumber>Ԫ����Ϊ����
        int BuildArrangementDef(out string strArrangementDef,
            out string strError)
        {
            strError = "";
            strArrangementDef = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<callNumber />");

            for (int i = 0; i < this.treeView_arrangement.Nodes.Count; i++)
            {
                TreeNode item = this.treeView_arrangement.Nodes[i];

                if (item.ImageIndex == TYPE_ARRANGEMENT_GROUP)
                {
                    // ȡ��name/classType/qufenhaoType/zhongcihaodb����
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
                    string strClassType = DomUtil.GetAttr(temp_dom.DocumentElement,
                        "classType");
                    string strQufenhaoType = DomUtil.GetAttr(temp_dom.DocumentElement,
                        "qufenhaoType");
                    string strZhongcihaoDbName = DomUtil.GetAttr(temp_dom.DocumentElement,
                        "zhongcihaodb");
                    string strCallNumberStyle = DomUtil.GetAttr(temp_dom.DocumentElement,
    "callNumberStyle");

                    XmlNode group_node = dom.CreateElement("group");
                    DomUtil.SetAttr(group_node, "name", strName);
                    DomUtil.SetAttr(group_node, "classType", strClassType);
                    DomUtil.SetAttr(group_node, "qufenhaoType", strQufenhaoType);
                    DomUtil.SetAttr(group_node, "zhongcihaodb", strZhongcihaoDbName);
                    DomUtil.SetAttr(group_node, "callNumberStyle", strCallNumberStyle);

                    dom.DocumentElement.AppendChild(group_node);

                    for (int j = 0; j < item.Nodes.Count; j++)
                    {
                        TreeNode location_treenode = item.Nodes[j];

                        string strXmlFragment = (string)location_treenode.Tag;

                        XmlDocumentFragment fragment = dom.CreateDocumentFragment();
                        try
                        {
                            fragment.InnerXml = strXmlFragment;
                        }
                        catch (Exception ex)
                        {
                            strError = "location fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                            return -1;
                        }

                        group_node.AppendChild(fragment);
                    }
                }
            }

            strArrangementDef = dom.DocumentElement.InnerXml;

            return 0;
        }

        // ���treeview_arrangement���Ѿ�ʹ�ù���ȫ���ִκ���
        // parameters:
        //      exclude_node    Ҫ�ų���TreeNode�ڵ㡣Ҳ����˵����ڵ��ù����ִκſⲻ��������
        List<string> GetArrangementAllUsedZhongcihaoDbName(TreeNode exclude_node)
        {
            List<string> existing_dbnames = new List<string>();
            for (int i = 0; i < this.treeView_arrangement.Nodes.Count; i++)
            {
                TreeNode tree_node = this.treeView_arrangement.Nodes[i];
                if (tree_node.ImageIndex != TYPE_ARRANGEMENT_GROUP)
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



        // ���treeview_arrangement���Ѿ�ʹ�ù���location��
        // parameters:
        //      exclude_node    Ҫ�ų���TreeNode�ڵ㡣Ҳ����˵����ڵ��ù���location����������
        List<string> GetArrangementAllUsedLocationName(TreeNode exclude_node)
        {
            List<string> existing_locationnames = new List<string>();
            for (int i = 0; i < this.treeView_arrangement.Nodes.Count; i++)
            {
                TreeNode tree_node = this.treeView_arrangement.Nodes[i];
                if (tree_node.ImageIndex != TYPE_ARRANGEMENT_GROUP)
                    continue;

                // ����group�ڵ���²�
                for (int j = 0; j < tree_node.Nodes.Count; j++)
                {
                    TreeNode location_tree_node = tree_node.Nodes[j];

                    if (location_tree_node == exclude_node)
                        continue;

                    string strXml = (string)location_tree_node.Tag;

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

                    string strLocationName = DomUtil.GetAttr(dom.DocumentElement, "name");

                    existing_locationnames.Add(strLocationName);    // ��Ҳ�������
                }
            }

            return existing_locationnames;
        }


        void menu_arrangement_up_Click(object sender, EventArgs e)
        {
            ArrangementMoveUpDown(true);
        }

        void menu_arrangement_down_Click(object sender, EventArgs e)
        {
            ArrangementMoveUpDown(false);
        }

        void ArrangementMoveUpDown(bool bUp)
        {
            string strError = "";
            // int nRet = 0;

            // ��ǰ��ѡ���node
            if (this.treeView_arrangement.SelectedNode == null)
            {
                MessageBox.Show("��δѡ��Ҫ���������ƶ��Ľڵ�");
                return;
            }

            TreeNodeCollection nodes = null;

            TreeNode parent = treeView_arrangement.SelectedNode.Parent;

            if (parent == null)
                nodes = this.treeView_arrangement.Nodes;
            else
                nodes = parent.Nodes;

            TreeNode node = treeView_arrangement.SelectedNode;

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

            this.treeView_arrangement.SelectedNode = node;


            this.ArrangementChanged = true;
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }
    }
}
