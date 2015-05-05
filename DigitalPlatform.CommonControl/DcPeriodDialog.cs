using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DigitalPlatform.CommonControl
{
    /// <summary>
    /// DC������Ϊdcterms:Period��ʱ�䷶Χֵ�ı༭�Ի���
    /// </summary>
    public partial class DcPeriodDialog : Form
    {
        // throw:
        //      Exception
        public string Value
        {
            get
            {
                string strError = "";
                string strValue = "";
                int nRet = GetValue(out strValue,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);

                return strValue;
            }
            set
            {
                string strError = "";
                string strStart = "";
                string strEnd = "";
                string strName = "";
                string strScheme = "";
                int nRet = ParseValue(value,
                    out strStart,
                    out strEnd,
                    out strName,
                    out strScheme,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);

                this.textBox_name.Text = strName;
                this.w3cDtfControl_start.ValueString = strStart;
                this.w3cDtfControl_end.ValueString = strEnd;
                this.comboBox_scheme.Text = strScheme;
            }
        }

        public DcPeriodDialog()
        {
            InitializeComponent();
        }

        static string GetParameter(string strText,
            string strParamName)
        {
            string strValue = "";
            int nRet = strText.IndexOf(strParamName + "=");
            if (nRet == -1)
                return "";  // not found

            strValue = strText.Substring(nRet + strParamName.Length + 1).Trim();

            nRet = strValue.IndexOf(";");
            if (nRet != -1)
                strValue = strValue.Substring(0, nRet).Trim();

            return strValue;
        }

        // ��������Ƿ�����˱�֧�ֵ� ʱ���ַ�������
        public static bool IsSupportType(string strValue)
        {
            string strScheme = GetParameter(strValue, "scheme");

            // ȱʡ������
            if (String.IsNullOrEmpty(strScheme) == true)
            {
                strScheme = "W3C-DTF";
            }

            if (strScheme != "W3C-DTF")
            {
                return false;
            }

            return true;
        }

        static int ParseValue(string strValue,
            out string strStart,
            out string strEnd,
            out string strName,
            out string strScheme,
            out string strError)
        {
            strStart = "";
            strEnd = "";
            strName = "";
            strScheme = "";
            strError = "";

            strStart = GetParameter(strValue, "start");
            strEnd = GetParameter(strValue, "end");
            strName = GetParameter(strValue, "name");
            strScheme = GetParameter(strValue, "scheme");

            // ȱʡ������
            if (String.IsNullOrEmpty(strScheme) == true)
            {
                strScheme = "W3C-DTF";
            }

            if (strScheme != "W3C-DTF")
            {
                strError = "Ŀǰ�в�֧�ֱ༭ '" + strScheme + "' ���͵�ʱ���ַ���";
                return -1;
            }

            return 0;
        }

        int GetValue(out string strValue,
            out string strError)
        {
            strValue = "";
            strError = "";

            string strDate = "";

            try
            {
                strDate = this.w3cDtfControl_start.ValueString;
            }
            catch (Exception ex)
            {
                strError = "��ʼʱ���ʽ�д�: " + ex.Message;
                return -1;
            }

            if (String.IsNullOrEmpty(strDate) == false)
            {
                strValue += "start=" + strDate + "; ";
            }


            try
            {
                strDate = this.w3cDtfControl_end.ValueString;
            }
            catch (Exception ex)
            {
                strError = "����ʱ���ʽ�д�: " + ex.Message;
                return -1;
            }

            if (String.IsNullOrEmpty(strDate) == false)
            {
                strValue += "end=" + strDate + "; ";
            }

            if (String.IsNullOrEmpty(this.textBox_name.Text) == false)
            {
                strValue += "name=" + this.textBox_name.Text + "; ";
            }

            if (String.IsNullOrEmpty(this.comboBox_scheme.Text) == false)
            {
                strValue += "scheme=" + this.comboBox_scheme.Text + "; ";
            }

            return 0;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strValue = "";
            string strError = "";
            int nRet = GetValue(out strValue,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
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

    }
}