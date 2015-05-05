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

// 2013/3/16 ��� XML ע��

namespace dp2Circulation
{
    /// <summary>
    /// �������� dp2Library ��־�ָ� ��̨���� �ĶԻ���
    /// </summary>
    internal partial class StartLogRecoverDlg : Form
    {
        /// <summary>
        /// ��̨������������
        /// </summary>
        public BatchTaskStartInfo StartInfo = new BatchTaskStartInfo();

        /// <summary>
        /// ���캯��
        /// </summary>
        public StartLogRecoverDlg()
        {
            InitializeComponent();
        }

        private void StartLogRecoverDlg_Load(object sender, EventArgs e)
        {
            // ��ʼλ�ò���
            long index = 0;
            string strFileName = "";
            string strError = "";

            int nRet = ParseLogRecoverStart(this.StartInfo.Start,
                out index,
                out strFileName,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.textBox_startFileName.Text = strFileName;
            this.textBox_startIndex.Text = index.ToString();

            // ͨ����������
            string strRecoverLevel = "";
            bool bClearFirst = false;

            nRet = ParseLogRecoverParam(this.StartInfo.Param,
                out strRecoverLevel,
                out bClearFirst,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.comboBox_recoverLevel.Text = strRecoverLevel;
            this.checkBox_clearBefore.Checked = bClearFirst;


            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void StartLogRecoverDlg_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            if (this.comboBox_recoverLevel.Text == "")
            {
                MessageBox.Show(this, "��δָ�� �ָ�����");
                return;
            }


            // �ϳɲ���
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
                this.StartInfo.Start = index.ToString() + "@" + this.textBox_startFileName.Text;
            }


            // ͨ����������
            string strRecoverLevel = this.comboBox_recoverLevel.Text;
            int nRet = strRecoverLevel.IndexOf('(');
            if (nRet != -1)
                strRecoverLevel = strRecoverLevel.Substring(0, nRet).Trim();

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");
            DomUtil.SetAttr(dom.DocumentElement,
                "recoverLevel",
                strRecoverLevel);
            DomUtil.SetAttr(dom.DocumentElement,
                "clearFirst",
                (this.checkBox_clearBefore.Checked == true ? "yes" : "no") );

            this.StartInfo.Param = dom.OuterXml;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();

        }

        // ���� ��ʼ ����
        static int ParseLogRecoverStart(string strStart,
            out long index,
            out string strFileName,
            out string strError)
        {
            strError = "";
            index = 0;
            strFileName = "";

            if (String.IsNullOrEmpty(strStart) == true)
                return 0;

            int nRet = strStart.IndexOf('@');
            if (nRet == -1)
            {
                try
                {
                    index = Convert.ToInt64(strStart);
                }
                catch (Exception)
                {
                    strError = "�������� '" + strStart + "' ��ʽ����" + "���û��@����ӦΪ�����֡�";
                    return -1;
                }
                return 0;
            }

            try
            {
                index = Convert.ToInt64(strStart.Substring(0, nRet).Trim());
            }
            catch (Exception)
            {
                strError = "�������� '" + strStart + "' ��ʽ����'" + strStart.Substring(0, nRet).Trim() + "' ����Ӧ��Ϊ�����֡�";
                return -1;
            }


            strFileName = strStart.Substring(nRet + 1).Trim();
            return 0;
        }

        /// <summary>
        /// ������־�ָ�����
        /// </summary>
        /// <param name="strParam">�������Ĳ����ַ���</param>
        /// <param name="strRecoverLevel">��־�ָ�����</param>
        /// <param name="bClearFirst">�ڻָ�ǰ�Ƿ�������е����ݿ��¼</param>
        /// <param name="strError">������Ϣ������������������ʱ</param>
        /// <returns>-1: ����������Ϣ�� strError �����з��أ�0: �ɹ�</returns>
        public static int ParseLogRecoverParam(string strParam,
            out string strRecoverLevel,
            out bool bClearFirst,
            out string strError)
        {
            strError = "";
            bClearFirst = false;
            strRecoverLevel = "";

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

            /*
            Logic = 0,  // �߼�����
            LogicAndSnapshot = 1,   // �߼���������ʧ����ת�ÿ��ջָ�
            Snapshot = 3,   // ����ȫ�ģ�����
            Robust = 4, // ��ǿ׳���ݴ�ָ���ʽ
             * */

            strRecoverLevel = DomUtil.GetAttr(dom.DocumentElement,
                "recoverLevel");
            string strClearFirst = DomUtil.GetAttr(dom.DocumentElement,
                "clearFirst");
            if (strClearFirst.ToLower() == "yes"
                || strClearFirst.ToLower() == "true")
                bClearFirst = true;
            else
                bClearFirst = false;

            return 0;
        }
    }
}