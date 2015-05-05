using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using DigitalPlatform.Xml;

namespace dp2Circulation
{
    /// <summary>
    /// ����ѡ�����������ݿ��From�ĶԻ���
    /// </summary>
    internal partial class ImportFromsDialog : Form
    {
        /// <summary>
        /// ϵͳ����
        /// </summary>
        public ManagerForm ManagerForm = null;

        List<string> m_dbnames = new List<string>();

        string m_strFromsXml = "";

        /// <summary>
        /// ���ر�ʾѡ���ļ���;���� XML
        /// </summary>
        public string SelectedFromsXml = "";

        /// <summary>
        /// ���캯��
        /// </summary>
        public ImportFromsDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="managerform">ϵͳ����</param>
        /// <param name="dbnames">���ݿ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����  0: ����</returns>
        public int Initial(ManagerForm managerform,
            List<string> dbnames,
            out string strError)
        {
            strError = "";

            this.ManagerForm = managerform;
            this.m_dbnames = dbnames;

            // �ϲ���Ľ��
            XmlDocument dom_total = new XmlDocument();
            dom_total.LoadXml("<root />");

            // ���ȫ�����ݿⶨ��
            for (int i = 0; i < this.m_dbnames.Count; i++)
            {
                string strDbName = this.m_dbnames[i];

                string strOutputInfo = "";
                // �����ͨ���ݿⶨ��
                int nRet = this.ManagerForm.GetDatabaseInfo(
                    strDbName,
                    out strOutputInfo,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                {
                    strError = "���ݿ� '" + strDbName + "' ������";
                    return -1;
                }

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strOutputInfo);
                }
                catch (Exception ex)
                {
                    strError = "XMLװ��DOMʱ����: " + ex.Message;
                    return -1;
                }

                XmlNodeList nodes = dom.DocumentElement.SelectNodes("from");
                for (int j = 0; j < nodes.Count; j++)
                {
                    XmlNode node = nodes[j];
                    string strFromStyle = DomUtil.GetAttr(node, "style");

                    // ����û��style���Ե�<from>
                    if (String.IsNullOrEmpty(strFromStyle) == true)
                        continue;

                    // ��������¼�����š�style����Ϊ���<from>Ԫ����û��<caption>Ԫ�أ��������鷳
                    if (strFromStyle == "recid")
                        continue;

                    XmlNode nodeExist = dom_total.DocumentElement.SelectSingleNode("from[@style='" + strFromStyle + "']");
                    if (nodeExist != null)
                    {
                        // style�Ѿ����ڣ���captions���кϲ�����
                        MergeCaptions(nodeExist,
                            node);
                        continue;
                    }

                    // ����
                    XmlNode new_node = dom_total.CreateElement("from");
                    dom_total.DocumentElement.AppendChild(new_node);
                    DomUtil.SetAttr(new_node, "style", strFromStyle);
                    new_node.InnerXml = node.InnerXml;
                }
            }

            this.m_strFromsXml = dom_total.DocumentElement.InnerXml;

            return 0;
        }

        // ������<from>Ԫ���µ�����<caption>�������Դ���ȥ�غϲ�
        // �����Ǵ�nodeSource�ϲ���nodeTarget��
        // �����Դ����Ƿ��أ��������жϷ�ʽ��һ������ȫһ���Ž��أ�һ������߲���һ���ͽ��ء�Ŀǰ����ǰ�ߣ�������ȫ��ĺϲ�Ч��
        /// <summary>
        /// ������ from Ԫ���µ����� caption Ԫ�ذ������Դ���ȥ�غϲ�
        /// �����Ǵ�nodeSource�ϲ���nodeTarget��
        /// �����Դ����Ƿ��أ��������жϷ�ʽ��һ������ȫһ���Ž��أ�һ������߲���һ���ͽ��ء�Ŀǰ����ǰ�ߣ�������ȫ��ĺϲ�Ч��
        /// </summary>
        /// <param name="nodeTarget">Ŀ��ڵ�</param>
        /// <param name="nodeSource">Դ�ڵ�</param>
        public static void MergeCaptions(XmlNode nodeTarget,
            XmlNode nodeSource)
        {
            for (int i = 0; i < nodeSource.ChildNodes.Count; i++)
            {
                XmlNode nodeSourceCaption = nodeSource.ChildNodes[i];
                if (nodeSource.NodeType != XmlNodeType.Element)
                    continue;
                if (nodeSource.Name != "caption")
                    continue;

                string strSourceLang = DomUtil.GetAttr(nodeSource, "lang");
                bool bFound = false;
                for (int j = 0; j < nodeTarget.ChildNodes.Count; j++)
                {
                    XmlNode nodeTargetCaption = nodeTarget.ChildNodes[j];
                    if (nodeTargetCaption.NodeType != XmlNodeType.Element)
                        continue;
                    if (nodeTargetCaption.Name != "caption")
                        continue;
                    string strTargetLang = DomUtil.GetAttr(nodeTarget, "lang");
                    if (strSourceLang == strTargetLang)
                    {
                        bFound = true;
                        break;
                    }
                }

                if (bFound == true)
                    continue;

                // ����
                XmlNode new_caption = nodeTarget.OwnerDocument.CreateElement("caption");
                nodeTarget.ParentNode.AppendChild(new_caption);

                DomUtil.SetAttr(new_caption, "lang", strSourceLang);
                new_caption.InnerText = nodeSourceCaption.InnerText;
            }
        }

        private void ImportFromsDialog_Load(object sender, EventArgs e)
        {
            this.fromEditControl1.Xml = this.m_strFromsXml;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            // ��鵱ǰ�Ƿ���ѡ�����
            if (this.fromEditControl1.SelectedElements.Count == 0)
            {
                MessageBox.Show(this, "��δѡ���κμ���;������");
                return;
            }

            try
            {
                this.SelectedFromsXml = this.fromEditControl1.SelectedXml;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();

        }

        private void button_selectAll_Click(object sender, EventArgs e)
        {
            this.fromEditControl1.SelectAll();
        }

        private void button_unSelectAll_Click(object sender, EventArgs e)
        {
            this.fromEditControl1.ClearAllSelect();

        }

        private void fromEditControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.fromEditControl1.SelectedElements.Count > 0)
                this.button_OK.Enabled = true;
            else
                this.button_OK.Enabled = false;
        }
    }
}