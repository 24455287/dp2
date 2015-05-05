using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using DigitalPlatform.Xml;
using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    internal partial class StartDkywReplicationDlg : Form
    {
        public BatchTaskStartInfo StartInfo = new BatchTaskStartInfo();

        public StartDkywReplicationDlg()
        {
            InitializeComponent();
        }

        private void StartDkywReplicationDlg_Load(object sender, EventArgs e)
        {
            // ��ʼλ�ò���
            string strRecordID = "";
            string strError = "";

            int nRet = ParseDkywReplicationStart(this.StartInfo.Start,
                out strRecordID,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (strRecordID == "!breakpoint")
            {
                this.textBox_startIndex.Text = "";
                this.checkBox_startAtServerBreakPoint.Checked = true;
                // this.textBox_startIndex.Enabled = false;
            }
            else
            {
                this.textBox_startIndex.Text = strRecordID;
                this.checkBox_startAtServerBreakPoint.Checked = false;
            }

            // ͨ����������
            bool bLoop = false;

            nRet = ParseDkywReplicationParam(this.StartInfo.Param,
                out bLoop,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.checkBox_loop.Checked = bLoop;

            checkBox_startAtServerBreakPoint_CheckedChanged(null, null);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void StartDkywReplicationDlg_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        // ���� ��ʼ ����
        // parameters:
        //      strStart    �����ַ�������ʽΪXML
        //                  ����Զ��ַ���Ϊ"!breakpoint"����ʾ�ӷ���������Ķϵ���Ϣ��ʼ
        // return:
        //      -1  ����
        //      0   ��ȷ
        int ParseDkywReplicationStart(string strStart,
            out string strRecordID,
            out string strError)
        {
            strError = "";
            strRecordID = "";

            // int nRet = 0;

            if (String.IsNullOrEmpty(strStart) == true)
            {
                strRecordID = "1";
                return 0;
            }

            // 2009/7/16 new add
            if (strStart == "!breakpoint")
            {
                strRecordID = strStart;
                return 0;
            }

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strStart);
            }
            catch (Exception ex)
            {
                strError = "װ��XML�ַ�������DOMʱ��������: " + ex.Message;
                return -1;
            }

            XmlNode nodeLoop = dom.DocumentElement.SelectSingleNode("loop");
            if (nodeLoop != null)
            {
                strRecordID = DomUtil.GetAttr(nodeLoop, "recordid");
            }

            return 0;
        }

        // ����ͨ����������
        // ��ʽ
        /*
         * <root loop='...'/>
         * loopȱʡΪtrue
         * 
         * */
        public static int ParseDkywReplicationParam(string strParam,
            out bool bLoop,
            out string strError)
        {
            strError = "";
            bLoop = true;

            if (String.IsNullOrEmpty(strParam) == true)
                return 0;

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strParam);
            }
            catch (Exception ex)
            {
                strError = "strParam����װ��XML DOMʱ����: " + ex.Message;
                return -1;
            }

            // ȱʡΪtrue
            string strLoop = DomUtil.GetAttr(dom.DocumentElement,
    "loop");
            if (strLoop.ToLower() == "no"
                || strLoop.ToLower() == "false")
                bLoop = false;
            else
                bLoop = true;

            return 0;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";

            // �ϳɲ���
            if (this.checkBox_startAtServerBreakPoint.Checked == true)
            {
                this.StartInfo.Start = "!breakpoint";
                // ͨ����������
                this.StartInfo.Param = MakeArriveMonitorParam(this.checkBox_loop.Checked);
            }
            else
            {
                if (this.textBox_startIndex.Text == "")
                {
                    strError = "��δָ������¼��";
                    this.textBox_startIndex.Focus();
                    goto ERROR1;
                }

                DialogResult result = MessageBox.Show(this,
                    "ָ������¼�ŵķ�ʽ���׵��������ظ����٣���ϵͳ�������в���������Ӱ�졣һ������£�ѡ��ӷ����������Ķϵ㿪ʼ����Ϊ�á�\r\n\r\nȷʵҪ����(����ָ������¼�ŵķ�ʽ���д���)?",
                    "StartDkywReplicationDlg",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                    return;

                this.StartInfo.Start = MakeBreakPointString(this.textBox_startIndex.Text);

                // ͨ����������
                this.StartInfo.Param = MakeArriveMonitorParam(this.checkBox_loop.Checked);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // ����ϵ��ַ���
        static string MakeBreakPointString(
            string strRecordID)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            // <loop>
            XmlNode nodeLoop = dom.CreateElement("loop");
            dom.DocumentElement.AppendChild(nodeLoop);

            DomUtil.SetAttr(nodeLoop, "recordid", strRecordID);

            return dom.OuterXml;
        }

        public static string MakeArriveMonitorParam(
bool bLoop)
        {
            XmlDocument dom = new XmlDocument();

            dom.LoadXml("<root />");

            DomUtil.SetAttr(dom.DocumentElement, "loop",
                bLoop == true ? "yes" : "no");

            return dom.OuterXml;
        }

        private void checkBox_startAtServerBreakPoint_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_startAtServerBreakPoint.Checked == true)
            {
                this.textBox_startIndex.Enabled = false;
            }
            else
            {
                this.textBox_startIndex.Enabled = true;
            }

        }
    }
}