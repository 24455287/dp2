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
    /// <summary>
    /// ����DTLP 
    /// �˶Ի����Ѿ�����ֹ
    /// </summary>
    internal partial class StartTraceDtlpDlg : Form
    {
        public BatchTaskStartInfo StartInfo = new BatchTaskStartInfo();

        public StartTraceDtlpDlg()
        {
            InitializeComponent();
        }

        private void StartTraceDtlpDlg_Load(object sender, EventArgs e)
        {
            // ��ʼλ�ò���
            int index = 0;
            string strFileName = "";
            string strError = "";
            int nRet = 0;

            if (this.StartInfo.Start == "!breakpoint")
                this.checkBox_startAtServerBreakPoint.Checked = true;
            else
            {
                string strTemp1;
                string strTemp2;
                string strLogStartOffset = "";
                nRet = ParseTraceDtlpStart(
                    this.StartInfo.Start,
                    out index,
                    out strLogStartOffset,
                    out strFileName,
                    out strTemp1,
                    out strTemp2,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                this.textBox_startFileName.Text = strFileName;
                this.textBox_startIndex.Text = index.ToString();
            }

            // ͨ����������
            bool bDump = false;
            bool bClearFirst = false;
            bool bLoop = true;

            nRet = ParseTraceDtlpParam(this.StartInfo.Param,
                out bDump,
                out bClearFirst,
                out bLoop,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.checkBox_dump.Checked = bDump;
            this.checkBox_clearBefore.Checked = bClearFirst;
            this.checkBox_loop.Checked = bLoop;

            // ���úó�ʼ��Enalbed״̬
            checkBox_startAtServerBreakPoint_CheckedChanged(this, null);
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void StartTraceDtlpDlg_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        // ����ϵ��ַ���
        static string MakeBreakPointString(
            long indexLog,
            string strLogStartOffset,
            string strLogFileName,
            string strRecordID,
            string strOriginDbName)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            // <dump>
            XmlNode nodeDump = dom.CreateElement("dump");
            dom.DocumentElement.AppendChild(nodeDump);

            DomUtil.SetAttr(nodeDump, "recordid", strRecordID);
            DomUtil.SetAttr(nodeDump, "origindbname", strOriginDbName);

            // <trace>
            XmlNode nodeTrace = dom.CreateElement("trace");
            dom.DocumentElement.AppendChild(nodeTrace);

            DomUtil.SetAttr(nodeTrace, "index", indexLog.ToString());
            DomUtil.SetAttr(nodeTrace, "startoffset", strLogStartOffset);
            DomUtil.SetAttr(nodeTrace, "logfilename", strLogFileName);

            return dom.OuterXml;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            // �ϳɲ���
            if (this.checkBox_startAtServerBreakPoint.Checked == true)
            {
                this.StartInfo.Start = "!breakpoint";
            }
            else
            {
                if (this.textBox_startFileName.Text == "")
                    this.StartInfo.Start = "";
                else
                {
                    long index = 0;
                    if (this.textBox_startIndex.Text != "")
                    {
                        try
                        {
                            index = Convert.ToInt64(this.textBox_startIndex.Text);
                        }
                        catch (Exception)
                        {
                            MessageBox.Show(this, "��¼������ '" + this.textBox_startIndex.Text + "' ����Ϊ������");
                            return;
                        }
                    }

                    this.StartInfo.Start = MakeBreakPointString(
                        index,
                        "",
                        this.textBox_startFileName.Text,
                        "", //strRecordID,
                        "" //strOriginDbName
                        );
                }

            }



            // ͨ����������
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            if (checkBox_startAtServerBreakPoint.Checked == true)
            {
                // ���÷������ϵ�ʱ��dumpֵ�������Ի��������clearFirst��Ϊno����Ϊclear�׶��ǲ������жϵģ���Ȼ�Ӷϵ㿪ʼ���ͱ�����ǰ�ɹ�ִ�й��ˣ������Ҫ�Ļ�������
                DomUtil.SetAttr(dom.DocumentElement,
                    "dump",
                    "no");
                DomUtil.SetAttr(dom.DocumentElement,
                    "clearFirst",
                    "no");
            }
            else
            {
                DomUtil.SetAttr(dom.DocumentElement,
                    "dump",
                    (this.checkBox_dump.Checked == true ? "yes" : "no"));
                DomUtil.SetAttr(dom.DocumentElement,
                    "clearFirst",
                    (this.checkBox_clearBefore.Checked == true ? "yes" : "no"));
            }

            DomUtil.SetAttr(dom.DocumentElement,
                "loop",
                (this.checkBox_loop.Checked == true ? "yes" : "no"));

            this.StartInfo.Param = dom.OuterXml;


            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();

        }

        // ����ͨ����������
        // ��ʽ
        /*
         * <root dump='...' clearFirst='...' loop='...'/>
         * dumpȱʡΪfalse
         * clearFirstȱʡΪfalse
         * loopȱʡΪtrue
         * 
         * 
         * */
        public static int ParseTraceDtlpParam(string strParam,
            out bool bDump,
            out bool bClearFirst,
            out bool bLoop,
            out string strError)
        {
            strError = "";
            bClearFirst = false;
            bDump = false;
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


            string strClearFirst = DomUtil.GetAttr(dom.DocumentElement,
                "clearFirst");
            if (strClearFirst.ToLower() == "yes"
                || strClearFirst.ToLower() == "true")
                bClearFirst = true;
            else
                bClearFirst = false;

            string strDump = DomUtil.GetAttr(dom.DocumentElement,
    "dump");
            if (strDump.ToLower() == "yes"
                || strDump.ToLower() == "true")
                bDump = true;
            else
                bDump = false;

            string strLoop = DomUtil.GetAttr(dom.DocumentElement,
"loop");
            if (strLoop.ToLower() == "yes"
                || strLoop.ToLower() == "true")
                bLoop = true;
            else
                bLoop = false;

            return 0;
        }


        static int ParseTraceDtlpStart(string strStart,
            out int indexLog,
            out string strLogStartOffset,
            out string strLogFileName,
            out string strRecordID,
            out string strOriginDbName,
            out string strError)
        {
            strError = "";
            indexLog = 0;
            strLogFileName = "";
            strLogStartOffset = "";
            strRecordID = "";
            strOriginDbName = "";

            // int nRet = 0;

            if (String.IsNullOrEmpty(strStart) == true)
            {
                DateTime now = DateTime.Now;
                // �������־�ļ����Ա���������
                strLogFileName = now.Year.ToString().PadLeft(4, '0')
                + now.Month.ToString().PadLeft(2, '0')
                + now.Day.ToString().PadLeft(2, '0');
                return 0;
            }

            if (strStart == "!breakpoint")
            {
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

            XmlNode nodeDump = dom.DocumentElement.SelectSingleNode("dump");
            if (nodeDump != null)
            {
                strRecordID = DomUtil.GetAttr(nodeDump, "recordid");
                strOriginDbName = DomUtil.GetAttr(nodeDump, "origindbname");
            }

            XmlNode nodeTrace = dom.DocumentElement.SelectSingleNode("trace");
            if (nodeTrace != null)
            {
                string strIndex = DomUtil.GetAttr(nodeTrace, "index");
                if (String.IsNullOrEmpty(strIndex) == true)
                    indexLog = 0;
                else
                {
                    try
                    {
                        indexLog = Convert.ToInt32(strIndex);
                    }
                    catch
                    {
                        strError = "<trace>Ԫ����index����ֵ '" + strIndex + "' ��ʽ����Ӧ��Ϊ������";
                        return -1;
                    }
                }

                strLogStartOffset = DomUtil.GetAttr(nodeTrace, "startoffs");
                strLogFileName = DomUtil.GetAttr(nodeTrace, "logfilename");
            }

            return 0;
        }

        private void checkBox_startAtServerBreakPoint_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_startAtServerBreakPoint.Checked == true)
            {
                this.textBox_startFileName.Enabled = false;
                this.textBox_startIndex.Enabled = false;
                this.checkBox_clearBefore.Enabled = false;
                this.checkBox_dump.Enabled = false;
            }
            else
            {
                this.textBox_startFileName.Enabled = true;
                this.textBox_startIndex.Enabled = true;
                this.checkBox_clearBefore.Enabled = true;
                this.checkBox_dump.Enabled = true;
            }

        }
    }
}