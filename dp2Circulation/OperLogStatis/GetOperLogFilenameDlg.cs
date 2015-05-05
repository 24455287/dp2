using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DigitalPlatform.IO;

// 2013/3/16 ��� XML ע��

namespace dp2Circulation
{
    /// <summary>
    /// ���ڻ�õ�����־�ļ��������߷�Χ��־�ļ����ĶԻ���
    /// </summary>
    internal partial class GetOperLogFilenameDlg : Form
    {
        /// <summary>
        /// �Ƿ�Ϊ����ģʽ��
        /// </summary>
        public bool SingleMode = false; // 

        /// <summary>
        /// �����ѡ����ļ���
        /// </summary>
        public List<string> OperLogFilenames = new List<string>();

        /// <summary>
        /// ���캯��
        /// </summary>
        public GetOperLogFilenameDlg()
        {
            InitializeComponent();
        }

        private void GetOperLogFilenameDlg_Load(object sender, EventArgs e)
        {
            // ��ó�ʼ��ֵ
            if (this.OperLogFilenames != null
                && this.OperLogFilenames.Count >= 1)
            {
                string strDate = this.OperLogFilenames[0];
                if (strDate.Length > 8)
                    strDate = strDate.Substring(0, 8);

                try
                {
                    this.dateControl_start.Value = DateTimeUtil.Long8ToDateTime(strDate);
                }
                catch
                {
                }
            }

            if (this.SingleMode == true)
                label_start.Text = "��־��������(&D):";
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";

            OperLogFilenames.Clear();

            string strStartDate = "";
            
            if (this.dateControl_start.IsValueNull() == false)
                strStartDate = DateTimeUtil.DateTimeToString8(this.dateControl_start.Value);

            string strEndDate = "";
            
            if (this.dateControl_end.IsValueNull() == false)
                strEndDate = DateTimeUtil.DateTimeToString8(this.dateControl_end.Value);

            if (String.IsNullOrEmpty(strEndDate) == true
                && String.IsNullOrEmpty(strStartDate) == true)
            {
                strError = "��δָ��ʱ��";
                goto ERROR1;
            }

            if (String.IsNullOrEmpty(strEndDate) == true)
            {
                OperLogFilenames.Add(strStartDate + ".log");
                goto END1;
            }

            if (String.IsNullOrEmpty(strStartDate) == true)
            {
                OperLogFilenames.Add(strEndDate + ".log");
                goto END1;
            }

            string strWarning = "";
            List<string> LogFileNames = null;
            // �������ڷ�Χ��������־�ļ���
            // parameters:
            //      strStartDate    ��ʼ���ڡ�8�ַ�
            //      strEndDate  �������ڡ�8�ַ�
            // return:
            //      -1  ����
            //      0   �ɹ�
            int nRet = OperLogStatisForm.MakeLogFileNames(strStartDate,
                strEndDate,
                true,
                out LogFileNames,
                out strWarning,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (String.IsNullOrEmpty(strWarning) == false)
                MessageBox.Show(this, strWarning);

            this.OperLogFilenames = LogFileNames;

            END1:
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

        void VisibleEndControls(bool bVisible)
        {
            this.label_end.Visible = bVisible;
            this.dateControl_end.Visible = bVisible;
        }

        private void dateControl_start_DateTextChanged(object sender, EventArgs e)
        {
            if (this.SingleMode == false
                && this.dateControl_start.IsValueNull() == false)
            {
                VisibleEndControls(true);
            }
        }
    }
}