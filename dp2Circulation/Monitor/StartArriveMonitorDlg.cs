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
    internal partial class StartArriveMonitorDlg : Form
    {
        public BatchTaskStartInfo StartInfo = new BatchTaskStartInfo();

        public StartArriveMonitorDlg()
        {
            InitializeComponent();
        }

        private void StartArriveMonitorDlg_Load(object sender, EventArgs e)
        {
            // ��ʼλ�ò���
            string strRecordID = "";
            string strError = "";

            int nRet = ParseArriveMonitorStart(this.StartInfo.Start,
                out strRecordID,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.textBox_startIndex.Text = strRecordID;

            // ͨ����������
            bool bLoop = false;

            nRet = ParseArriveMonitorParam(this.StartInfo.Param,
                out bLoop,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.checkBox_loop.Checked = bLoop;

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void StartArriveMonitorDlg_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        // ���� ��ʼ ����
        // parameters:
        //      strStart    �����ַ�������ʽΪXML
        //                  ����Զ��ַ���Ϊ"!breakpoint"����ʾ�ӷ���������Ķϵ���Ϣ��ʼ
        int ParseArriveMonitorStart(string strStart,
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
        public static int ParseArriveMonitorParam(string strParam,
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
            // �ϳɲ���
            this.StartInfo.Start = MakeBreakPointString(this.textBox_startIndex.Text);

            // ͨ����������
            this.StartInfo.Param = MakeArriveMonitorParam(this.checkBox_loop.Checked);

            this.DialogResult = DialogResult.OK;
            this.Close();
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
    }
}