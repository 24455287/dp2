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
    /// ���� ����ҳ
    /// </summary>
    public partial class ManagerForm
    {
        bool m_bDupChanged = false;

        XmlDocument m_dup_dom = null;

        /// <summary>
        /// ���ض����Ƿ��޸�
        /// </summary>
        public bool DupChanged
        {
            get
            {
                return this.m_bDupChanged;
            }
            set
            {
                this.m_bDupChanged = value;
                if (value == true)
                    this.toolStripButton_dup_save.Enabled = true;
                else
                    this.toolStripButton_dup_save.Enabled = false;
            }
        }

        // �г��ż���ϵ����
        int ListDup(out string strError)
        {
            strError = "";

            if (this.DupChanged == true)
            {
                // ������δ����
                DialogResult result = MessageBox.Show(this,
                    "��ǰ�����ڲ��ض��屻�޸ĺ���δ���档����ʱˢ�´������ݣ�����δ������Ϣ����ʧ��\r\n\r\nȷʵҪˢ��? ",
                    "ManagerForm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    return 0;
                }
            }

            this.listView_dup_projects.Items.Clear();
            this.listView_dup_defaults.Items.Clear();


            string strDupXml = "";

            // ����ִκ���ض���
            int nRet = GetDupInfo(out strDupXml,
                out strError);
            if (nRet == -1)
                return -1;

            this.m_dup_dom = new XmlDocument();
            this.m_dup_dom.LoadXml("<dup />");

            XmlDocumentFragment fragment = this.m_dup_dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = strDupXml;
            }
            catch (Exception ex)
            {
                strError = "fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                return -1;
            }

            this.m_dup_dom.DocumentElement.AppendChild(fragment);

            /*
 <dup>
        <project name="�ɹ�����" comment="ʾ������">
            <database name="������Ŀ��" threshold="60">
                <accessPoint name="����" weight="50" searchStyle="" />
                <accessPoint name="����" weight="70" searchStyle="" />
                <accessPoint name="�������" weight="10" searchStyle="" />
            </database>
            <database name="��Ŀ��" threshold="60">
                <accessPoint name="����" weight="50" searchStyle="" />
                <accessPoint name="����" weight="70" searchStyle="" />
                <accessPoint name="�������" weight="10" searchStyle="" />
            </database>
        </project>
        <project name="��Ŀ����" comment="���Ǳ�Ŀ����ʾ������">
            <database name="����ͼ��" threshold="100">
                <accessPoint name="������" weight="50" searchStyle="" />
                <accessPoint name="ISBN" weight="80" searchStyle="" />
                <accessPoint name="����" weight="20" searchStyle="" />
            </database>
            <database name="ͼ�����" threshold="100">
                <accessPoint name="������" weight="50" searchStyle="" />
                <accessPoint name="ISBN" weight="80" searchStyle="" />
                <accessPoint name="����" weight="20" searchStyle="" />
            </database>
        </project>
        <default origin="����ͼ��" project="��Ŀ����" />
        <default origin="ͼ�����" project="��Ŀ����" />
    </dup>
             * * */
            FillProjectNameList(this.m_dup_dom);
            FillDefaultList(this.m_dup_dom);

            this.DupChanged = false;

            return 1;
        }


        void FillProjectNameList(XmlDocument dom)
        {
            this.listView_dup_projects.Items.Clear();

            if (dom == null)
                return;

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("//project");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strName = DomUtil.GetAttr(node, "name");
                string strComment = DomUtil.GetAttr(node, "comment");

                ListViewItem item = new ListViewItem(strName, 0);
                item.SubItems.Add(strComment);
                this.listView_dup_projects.Items.Add(item);
            }
        }


        void FillDefaultList(XmlDocument dom)
        {
            this.listView_dup_defaults.Items.Clear();

            // ���ȫ��<sourceDatabase>Ԫ��name�����еķ���·��
            List<string> startpaths = new List<string>();
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("//default"); // "//defaultProject/sourceDatabase"
            for (int i = 0; i < nodes.Count; i++)
            {
                string strStartPath = DomUtil.GetAttr(nodes[i], "origin");
                if (String.IsNullOrEmpty(strStartPath) == true)
                    continue;
                startpaths.Add(strStartPath);
            }


            // �Ȱ��ղ��ط����������õ����ķ���·��(���ݿ�)�г�����
            List<string> database_names = GetAllDatabaseNames(dom);
            for (int i = 0; i < database_names.Count; i++)
            {
                string strDatabaseName = database_names[i];

                string strDefaultProject = "";
                // XmlNode nodeDefault = dom.DocumentElement.SelectSingleNode("//defaultProject/sourceDatabase[@name='" + strDatabaseName + "']");
                XmlNode nodeDefault = dom.DocumentElement.SelectSingleNode("//default[@origin='" + strDatabaseName + "']");
                if (nodeDefault != null)
                    strDefaultProject = DomUtil.GetAttr(nodeDefault, "project");

                ListViewItem item = new ListViewItem(strDatabaseName, 0);
                item.SubItems.Add(strDefaultProject);
                this.listView_dup_defaults.Items.Add(item);
                item.Tag = 1;   // ��ʾΪʵ��

                // ��startpaths�������Ѿ��ù���startpath
                startpaths.Remove(strDatabaseName);
            }

            // �ٰ��ղ��ط���������û���õ����ķ���·���г�����
            for (int i = 0; i < startpaths.Count; i++)
            {
                string strDatabaseName = startpaths[i];

                string strDefaultProject = "";
                // XmlNode nodeDefault = dom.DocumentElement.SelectSingleNode("//defaultProject/sourceDatabase[@name='" + strDatabaseName + "']");
                XmlNode nodeDefault = dom.DocumentElement.SelectSingleNode("//default[@origin='" + strDatabaseName + "']");
                if (nodeDefault != null)
                    strDefaultProject = DomUtil.GetAttr(nodeDefault, "project");

                ListViewItem item = new ListViewItem(strDatabaseName, 0);
                item.SubItems.Add(strDefaultProject);
                this.listView_dup_defaults.Items.Add(item);
                item.Tag = null;    // ��ʾΪ����

                item.ForeColor = SystemColors.GrayText; // ��ɫ���飬��ʾ������ݿ���û���ڲ��ط��������г��ֹ�
            }
        }

        // ���ȫ�������ݿ���
        static List<string> GetAllDatabaseNames(XmlDocument dom)
        {
            List<string> results = new List<string>();
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("//database");
            for (int i = 0; i < nodes.Count; i++)
            {
                results.Add(DomUtil.GetAttr(nodes[i], "name"));
            }

            results.Sort();

            // ȥ��
            StringUtil.RemoveDup(ref results);

            return results;
        }

        // ��ò��ض���
        int GetDupInfo(out string strDupXml,
            out string strError)
        {
            strError = "";
            strDupXml = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڻ�ȡ���ض��� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.GetSystemParameter(
                    stop,
                    "circulation",
                    "dup",
                    out strDupXml,
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

        // ������ض���
        // parameters:
        //      strDupXml   �ű�����XML��ע�⣬û�и�Ԫ��
        int SetDupDef(string strDupXml,
            out string strError)
        {
            strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڱ�����ض��� ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            try
            {
                long lRet = Channel.SetSystemParameter(
                    stop,
                    "circulation",
                    "dup",
                    strDupXml,
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
        int SubmitDupDef(out string strError)
        {
            strError = "";
            string strDupDef = "";
            int nRet = BuildDupDef(out strDupDef,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = this.SetDupDef(strDupDef,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        // �����ż���ϵ�����XMLƬ��
        // ע�����¼�Ƭ�϶��壬û��<dup>Ԫ����Ϊ����
        int BuildDupDef(out string strDupDef,
            out string strError)
        {
            strError = "";
            strDupDef = "";

            strDupDef = this.m_dup_dom.DocumentElement.InnerXml;
            return 0;
        }

        // �ڷ������б��У�ѡ��һ���ض������ֵ���
        void SelectProjectItem(string strProjectName)
        {
            for (int i = 0; i < this.listView_dup_projects.Items.Count; i++)
            {
                ListViewItem item = this.listView_dup_projects.Items[i];
                if (item.Text == strProjectName)
                    item.Selected = true;
                else
                    item.Selected = false;
            }
        }

        // ���ȫ����Ŀ�������б�
        List<string> GetAllBiblioDbNames()
        {
            List<string> results = new List<string>();

            if (String.IsNullOrEmpty(this.AllDatabaseInfoXml) == true)
                return results;

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
                return results;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strName = DomUtil.GetAttr(node, "name");
                string strType = DomUtil.GetAttr(node, "type");

                if ("biblio" == strType)
                    results.Add(strName);
            }

            return results;
        }


        // �����������ı�󣬶��ֵ��·���ȱʡ��ϵ�б���
        void ChangeDefaultProjectName(string strOldProjectName,
            string strNewProjectName)
        {
            if (strOldProjectName == strNewProjectName)
            {
                Debug.Assert(false, "");
                return;
            }

            bool bChanged = false;
            int nCount = 0;
            for (int i = 0; i < listView_dup_defaults.Items.Count; i++)
            {
                ListViewItem item = this.listView_dup_defaults.Items[i];

                // �����Ӿ��޸�
                string strProjectName = ListViewUtil.GetItemText(item, 1);
                if (strProjectName == strOldProjectName)
                {
                    ListViewUtil.ChangeItemText(item, 1, strNewProjectName);
                    bChanged = true;
                    nCount++;
                }

            }
            // ����DOM�޸�
            XmlNodeList nodes = this.m_dup_dom.DocumentElement.SelectNodes(
                "//default[@project='" + strOldProjectName + "']");
            Debug.Assert(nCount == nodes.Count, "������ĿӦ�����");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                if (String.IsNullOrEmpty(strNewProjectName) == true)
                {
                    // ɾ��
                    node.ParentNode.RemoveChild(node);
                }
                else
                {
                    DomUtil.SetAttr(node, "project", strNewProjectName);
                }
                bChanged = true;
            }

            if (bChanged == true)
                this.DupChanged = true;
        }
    }
}
