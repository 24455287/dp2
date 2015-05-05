using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace DigitalPlatform.CommonControl
{
    /// <summary>
    /// W3CDTFʱ��ؼ�
    /// </summary>
    public partial class W3cDtfControl : UserControl
    {
        public W3cDtfControl()
        {
            InitializeComponent();
        }

        [Category("Appearance")]
        [DescriptionAttribute("Border style of the control")]
        [DefaultValue(typeof(System.Windows.Forms.BorderStyle), "None")]
        public new BorderStyle BorderStyle
        {
            get
            {
                return base.BorderStyle;
            }
            set
            {
                base.BorderStyle = value;
            }
        }

        public string ValueString
        {
            get
            {
                this.maskedTextBox_date.TextMaskFormat = MaskFormat.ExcludePromptAndLiterals;
                this.maskedTextBox_timeZone.TextMaskFormat = MaskFormat.ExcludePromptAndLiterals;

                string strResult = "";
                string strError = "";

                string strTimeZone = "";
                if (String.IsNullOrEmpty(this.maskedTextBox_timeZone.Text) == false)
                    strTimeZone = this.label_eastWest.Text
                    + this.maskedTextBox_timeZone.Text;

                int nRet = BuildW3cDtfString(this.maskedTextBox_date.Text,
                    strTimeZone,
                    out strResult,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);

                return strResult;
            }
            set
            {
                this.maskedTextBox_date.TextMaskFormat = MaskFormat.ExcludePromptAndLiterals;
                this.maskedTextBox_timeZone.TextMaskFormat = MaskFormat.ExcludePromptAndLiterals;

                string strDateTimeString = "";
                string strTimeZoneString = "";
                string strError = "";

                int nRet = ParseW3cDtfString(value,
                    out strDateTimeString,
                    out strTimeZoneString,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);

                this.maskedTextBox_date.Text = strDateTimeString;

                if (strTimeZoneString != "")
                {
                    this.label_eastWest.Text = strTimeZoneString.Substring(0, 1);
                    this.maskedTextBox_timeZone.Text = strTimeZoneString.Substring(1);
                }
                else
                {
                    this.label_eastWest.Text = "+";
                    this.maskedTextBox_timeZone.Text = "";
                }

            }
        }

        static bool IsAllBlank(string strText)
        {
            bool bFound = false;    // �Ƿ����˷ǿո��ַ���
            for (int i = 0; i < strText.Length; i++)
            {
                if (strText[i] != ' ')
                {
                    bFound = true;
                    break;
                }
            }
            if (bFound == true)
                return false;

            return true;
        }

        // ��W3CDTF�ַ�������Ϊ �ܼ���̬�� ʱ�� �� ʱ�� �ַ���
        /*
W3CDTF�ǻ���ISO8601��ʽ�������¶��ǺϷ��ģ�
   Year:
      YYYY (eg 1997)
   Year and month:
      YYYY-MM (eg 1997-07)
   Complete date:
      YYYY-MM-DD (eg 1997-07-16)
   Complete date plus hours and minutes:
      YYYY-MM-DDThh:mmTZD (eg 1997-07-16T19:20+01:00)
   Complete date plus hours, minutes and seconds:
      YYYY-MM-DDThh:mm:ssTZD (eg 1997-07-16T19:20:30+01:00)
   Complete date plus hours, minutes, seconds and a decimal fraction of a
second
      YYYY-MM-DDThh:mm:ss.sTZD (eg 1997-07-16T19:20:30.45+01:00)
         * */
        int ParseW3cDtfString(string strW3cDtfString,
            out string strDateTimeString,
            out string strTimeZoneString,
            out string strError)
        {
            strError = "";
            strDateTimeString = "";
            strTimeZoneString = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(strW3cDtfString) == true)
            {
                return 0;   // ���ؿ�ֵ
            }

            if (strW3cDtfString.Length < 4)
            {
                strError = "���Ȳ���4�ַ�";
                return -1;
            }

            string strYear = "";
            if (strW3cDtfString.Length >= 4)
            {
                strYear = strW3cDtfString.Substring(0, 4);

                nRet = CheckNumberRange(strYear,
                    "0000",
                    "9999",
                    "��",
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            if (strW3cDtfString.Length == 4)
            {
                // Year:
                //      YYYY (eg 1997)
                strDateTimeString = strYear;
                return 0;
            }

            if (strW3cDtfString.Length > 4 && strW3cDtfString.Length < 7)
            {
                strError = "�·ݲ��ָ�ʽ����";
                return -1;
            }

            string strMonth = "";
            if (strW3cDtfString.Length >= 7)
            {
                if (strW3cDtfString[4] != '-')
                {
                    strError = "��5�ַ�Ӧ��Ϊ'-'";
                    return -1;
                }

                strMonth = strW3cDtfString.Substring(5, 2);

                nRet = CheckNumberRange(strMonth,
                    "01",
                    "12",
                    "��",
                    out strError);
                if (nRet == -1)
                    return -1;

            }

            if (strW3cDtfString.Length == 7)
            {
                //   Year and month:
                //      YYYY-MM (eg 1997-07)
                strDateTimeString = strYear + strMonth;
                return 0;
            }

            if (strW3cDtfString.Length > 7 && strW3cDtfString.Length < 10)
            {
                strError = "��ֵ���ָ�ʽ����";
                return -1;
            }


            string strDay = "";
            if (strW3cDtfString.Length >= 10)
            {
                if (strW3cDtfString[7] != '-')
                {
                    strError = "��8�ַ�Ӧ��Ϊ'-'";
                    return -1;
                }

                strDay = strW3cDtfString.Substring(8, 2);


                nRet = CheckNumberRange(strDay,
                    "01",
                    "31",
                    "��",
                    out strError);
                if (nRet == -1)
                    return -1;

                // TODO: ����Ҫ��ȷ��鵱ʱ�Ǹ��µ�����ֵ��Χ
            }

            if (strW3cDtfString.Length == 10)
            {
                //   Complete date:
                //      YYYY-MM-DD (eg 1997-07-16)
                strDateTimeString = strYear + strMonth + strDay;
                return 0;
            }

            string strTimeSegment = ""; // ʱ���
            string strTimeZoneSegment = ""; // ʱ����

            nRet = strW3cDtfString.IndexOf("T");
            if (nRet != -1)
            {
                strTimeSegment = strW3cDtfString.Substring(nRet + 1);
                nRet = strTimeSegment.IndexOfAny(new char[] { '+', '-' });
                if (nRet != -1)
                {
                    strTimeZoneSegment = strTimeSegment.Substring(nRet);
                    strTimeSegment = strTimeSegment.Substring(0, nRet);   // ȥ����������TimeZone����
                }
            }

            // ϸ�ڽ���ʱ���
            if (strTimeSegment != "")
            {
                string strHour = "";
                string strMinute = "";
                string strSecond = "";
                string strSecondDecimal = "";
                // T19:20:30.45

                nRet = ParseTimeSegment(strTimeSegment,
                    out strHour,
                    out strMinute,
                    out strSecond,
                    out strSecondDecimal,
                    out strError);
                if (nRet == -1)
                    return -1;

                // װ��Ϊ������̬
                strTimeSegment = strHour;
                strTimeSegment += strMinute;
                if (String.IsNullOrEmpty(strSecond) == true)
                    strTimeSegment += "  ";
                else
                    strTimeSegment += strSecond;

                if (String.IsNullOrEmpty(strSecondDecimal) == true)
                    strTimeSegment += "  ";
                else
                    strTimeSegment += strSecondDecimal;

            }

            // ϸ�ڽ���ʱ����

            if (strTimeZoneSegment != "")
            {
                string strEastWest = "";
                string strTzdHour = "";
                string strTzdMinute = "";

                nRet = ParseTimeZoneSegment(strTimeZoneSegment,
                    out strEastWest,
                    out strTzdHour,
                    out strTzdMinute,
                    out strError);
                if (nRet == -1)
                    return -1;

                // װ��Ϊ������̬
                strTimeZoneSegment = strEastWest;
                strTimeZoneSegment += strTzdHour;
                strTimeZoneSegment += strTzdMinute;
            }

            if (strTimeSegment != ""
                && strTimeZoneSegment != "")
            {
                //      Complete date plus hours and minutes:
                //      YYYY-MM-DDThh:mmTZD (eg 1997-07-16T19:20+01:00)
                strDateTimeString = strYear + strMonth + strDay + strTimeSegment;
                strTimeZoneString = strTimeZoneSegment;
                return 0;
            }

            if (strTimeSegment != ""
                && strTimeZoneSegment == "")
            {
                strError = "�߱�ʱ���(T�����Ĳ���)�ͱ���߱�ʱ����(+��-�����Ĳ���)";
                return -1;
            }



            return 0;
        }

        // return:
        //      0   û�д���
        //      -1  �д���
        static int CheckNumberRange(string strText,
            string strMin,
            string strMax,
            string strName,
            out string strError)
        {
            strError = "";

            if (strText.IndexOf(" ") != -1)
            {
                strError = strName + "ֵ '" + strText + "' �в�Ӧ�����ո�";
                return -1;
            }

            for (int i = 0; i < strText.Length; i++)
            {
                char ch = strText[i];
                if (ch < '0' || ch > '9')
                {
                    strError = strName + "ֵ '" +strText+ "' ���Ǵ�����";
                    return -1;
                }
            }

            if (String.Compare(strText, strMin) < 0)
            {
                strError = strName + "ֵ��ӦС�� '" + strMin + "'";
                return -1;
            }

            if (String.Compare(strText, strMax) > 0)
            {
                strError = strName + "ֵ��Ӧ���� '" + strMax + "'";
                return -1;
            }

            return 0;
        }

        // ϸ�ڽ���ʱ���
        // 19:20:30.45
        static int ParseTimeSegment(string strSegment,
            out string strHour,
            out string strMinute,
            out string strSecond,
            out string strSecondDecimal,
            out string strError)
        {
            strHour = "";
            strMinute = "";
            strSecond = "";
            strSecondDecimal = "";
            strError = "";
            int nRet = 0;

            if (strSegment.Length != 5
                && strSegment.Length != 8
                && strSegment.Length != 11)
            {
                strError = "ʱ���ַ��� '" + strSegment + "' ��ʽ����ȷ������ӦΪ5 8 11�ַ�";
                return -1;
            }


            if (strSegment.Length >= 5)
            {
                strHour = strSegment.Substring(0, 2);
                strMinute = strSegment.Substring(3, 2);

                // �����ֵ��Χ
                nRet = CheckNumberRange(strHour,
                    "00",
                    "23",
                    "Сʱ",
                    out strError);
                if (nRet == -1)
                    return -1;

                nRet = CheckNumberRange(strMinute,
                    "00",
                    "59",
                    "��",
                    out strError);
                if (nRet == -1)
                    return -1;

            }

            if (strSegment.Length >= 8)
            {
                strSecond = strSegment.Substring(6, 2);

                nRet = CheckNumberRange(strSecond,
                    "00",
                    "59",
                    "��",
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            if (strSegment.Length >= 11)
            {
                strSecondDecimal = strSegment.Substring(9, 2);

                nRet = CheckNumberRange(strSecondDecimal,
                    "00",
                    "99",
                    "�ٷ���",
                    out strError);
                if (nRet == -1)
                    return -1;

            }


            return 0;
        }

        // ϸ�ڽ���ʱ����
        // +01:00
        int ParseTimeZoneSegment(string strSegment,
            out string strEastWest,
            out string strHour,
            out string strMinute,
            out string strError)
        {
            strHour = "";
            strMinute = "";
            strEastWest = "";
            strError = "";
            int nRet = 0;

            if (strSegment.Length != 6)
            {
                strError = "ʱ���ַ��� '" + strSegment + "' ��ʽ����ȷ������ӦΪ6�ַ�";
                return -1;
            }

            strEastWest = strSegment.Substring(0, 1);
            if (strEastWest != "+"
                && strEastWest != "-")
            {
                strError = "ʱ���ַ��� '" + strSegment + "' ��һ�ַ�'"+strEastWest+"'��ʽ����ȷ��ӦΪ+ -֮һ";
                return -1;
            }

            strSegment = strSegment.Substring(1);

            if (strSegment.Length >= 5)
            {
                strHour = strSegment.Substring(0, 2);
                strMinute = strSegment.Substring(3, 2);

                // TODO: �����ֵ��Χ
                
                nRet = CheckNumberRange(strHour,
                    "00",
                    strEastWest == "-" ? "12" : "13",
                    "ʱ�� Сʱ",
                    out strError);
                if (nRet == -1)
                    return -1;

                nRet = CheckNumberRange(strMinute,
                    "00",
                    "59",
                    "ʱ�� ��",
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return 0;
        }

        // ��01:00�任0100
        static string GetPureHourMinte(string strText)
        {
            return strText.Substring(0, 2)
            + strText.Substring(3, 2);
        }

        // ���ܼ���̬��ʱ���ʱ��ֵ �任ΪW3CDTF��̬
        int BuildW3cDtfString(string strDateTimeString,
            string strTimeZoneString,
            out string strResult,
            out string strError)
        {
            strResult = "";
            strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(strDateTimeString) == true
                && String.IsNullOrEmpty(strTimeZoneString) == true)
            {
                return 0;   // ���ؿ�ֵ
            }
            
            // ȡ�����
            string strYearSegment = "";
            if (strDateTimeString.Length >= 4)
            {
                strYearSegment = strDateTimeString.Substring(0, 4);
            }

            // ������
            string strYear = "";
            if (strYearSegment != "")
            {
                if (strYearSegment.Length < 4)
                {
                    strError = "���Ӧ��Ϊ4λ����";
                    goto ERROR1;
                }

                strYear = strYearSegment;

                nRet = CheckNumberRange(strYear,
                    "0000",
                    "9999",
                    "��",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            if (strYear == "")
            {
                strError = "����ҲҪ����4λ���ֵ����ֵ";
                goto ERROR1;
            }

            // ȡ���¶�
            string strMonthSegment = "";
            if (strDateTimeString.Length >= 6)
            {
                strMonthSegment = strDateTimeString.Substring(4, 2);
            }

            if (strDateTimeString.Length > 4 && strDateTimeString.Length < 6)
            {
                strError = "�·�ֵӦΪ2λ����";
                goto ERROR1;
            }

            // ������
            string strMonth = "";
            if (strMonthSegment != "")
            {
                if (strMonthSegment.Length < 2)
                {
                    strError = "�·�Ӧ��Ϊ2λ����";
                    goto ERROR1;
                }

                strMonth = strMonthSegment;
                nRet = CheckNumberRange(strMonth,
                    "01",
                    "12",
                    "�·�",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }


            // ȡ���ն�
            string strDaySegment = "";
            if (strDateTimeString.Length >= 8)
            {
                strDaySegment = strDateTimeString.Substring(6, 2);
            }

            if (strDateTimeString.Length > 6 && strDateTimeString.Length < 8)
            {
                strError = "��ֵӦΪ2λ����";
                goto ERROR1;
            }

            // ������
            string strDay = "";
            if (strDaySegment != "")
            {
                // 8
                if (strDaySegment.Length < 2)
                {
                    strError = "��ֵӦ��Ϊ2λ����";
                    goto ERROR1;
                }

                strDay = strDaySegment;
                nRet = CheckNumberRange(strDay,
                    "01",
                    "31",
                    "��",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;


            }

            if (strYear == "")
            {
                if (strMonth != "" || strDay != "")
                {
                    strError = "���������ֵ";
                    goto ERROR1;
                }
            }

            if (strMonth == "")
            {
                if (strDay != "")
                {
                    strError = "�������·�ֵ";
                    goto ERROR1;
                }
            }


            // ȡ��ʱ�ֶ�
            string strHourMinuteSegment = "";
            if (strDateTimeString.Length >= 12)
            {
                strHourMinuteSegment = strDateTimeString.Substring(8, 4);
            }

            if (strDateTimeString.Length > 8 && strDateTimeString.Length < 12)
            {
                strError = "ʱ����ֵӦΪ4λ����";
                goto ERROR1;
            }

            // ����ʱ����
            string strHour = "";
            string strMinute = "";
            if (strHourMinuteSegment != ""
                && IsAllBlank(strHourMinuteSegment) == false)
            {
                if (strHourMinuteSegment.Length < 4)
                {
                    strError = "ʱ����ֵӦ��Ϊ4λ����";
                    goto ERROR1;
                }

                strHour = strHourMinuteSegment.Substring(0, 2);
                nRet = CheckNumberRange(strHour,
                    "00",
                    "23",
                    "Сʱ",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                strMinute = strHourMinuteSegment.Substring(2, 2);
                nRet = CheckNumberRange(strMinute,
                    "00",
                    "59",
                    "��",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                /*
                int hour = Convert.ToInt32(strHour);
                if (hour < 0 || hour > 23)
                {
                    strError = "ʱֵӦ����00-23֮��";
                    goto ERROR1;
                }

                int minute = Convert.ToInt32(strMinute);
                if (minute < 0 || minute > 59)
                {
                    strError = "��ֵӦ����00-59֮��";
                    goto ERROR1;
                }
                 * */
            }

            // ȡ���롢�ٷ����
            string strSecondSegment = "";
            if (strDateTimeString.Length >= 16)
            {
                strSecondSegment = strDateTimeString.Substring(12, 4);
            }
            else if (strDateTimeString.Length >= 14)
            {
                strSecondSegment = strDateTimeString.Substring(12, 2);
            }

            if (strDateTimeString.Length > 12 && strDateTimeString.Length < 14)
            {
                strError = "��ӦΪ2λ����";
                goto ERROR1;
            }

            // �����롢�ٷ���
            string strSecond = "";
            string strSecondDecimal = "";
            if (strSecondSegment != ""
                && IsAllBlank(strSecondSegment) == false)
            {
                if (strSecondSegment.Length < 2)
                {
                    strError = "��ֵӦ��Ϊ2λ����";
                    goto ERROR1;
                }

                if (strSecondSegment.Length == 2)
                {
                    strSecond = strSecondSegment;
                    strSecondDecimal = "";
                }
                else
                {
                    if (strSecondSegment.Length != 4)
                    {
                        strError = "��ֵӦ��Ϊ4λ����(�����ٷ���ʱ)";
                        goto ERROR1;
                    }

                    strSecond = strSecondSegment.Substring(0, 2);
                    strSecondDecimal = strSecondSegment.Substring(2, 2);

                    if (strSecondDecimal == "  ")
                        strSecondDecimal = "";
                    else
                    {
                        nRet = CheckNumberRange(strSecondDecimal,
                            "00",
                            "99",
                            "�ٷ���",
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                }

                /*
                if (strSecond.IndexOf(" ") != -1)
                {
                    strError = "��ֵ�в�Ӧ�������ո��ַ�";
                    goto ERROR1;
                }

                int second = Convert.ToInt32(strSecond);
                if (second < 0 || second > 59)
                {
                    strError = "��ֵӦ����00-59֮��";
                    goto ERROR1;
                }
                */
                nRet = CheckNumberRange(strSecond,
                    "00",
                    "59",
                    "��",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

            }

            // ȡ��ʱ����
            string strTimeZoneSegment = "";
            if (String.IsNullOrEmpty(strTimeZoneString) == false)
            {
                if (strTimeZoneString.Length < 5)
                {
                    strError = "ʱ��ӦΪ5λ�ַ�(һ��+/-����λ��4λ����)";
                    goto ERROR1;
                }
                strTimeZoneSegment = strTimeZoneString;
            }

            // ����ʱ����
            string strTzdHour = "";
            string strTzdMinute = "";
            string strTzdDirection = "";
            if (strTimeZoneSegment != ""
                && IsAllBlank(strTimeZoneSegment) == false)
            {
                if (strTimeZoneSegment.Length < 5)
                {
                    strError = "ʱ�� ���š�ʱ����ֵӦ��Ϊ5λ�ַ�";
                    goto ERROR1;
                }

                strTzdDirection = strTimeZoneSegment.Substring(0, 1);
                if (strTzdDirection != "+" && strTzdDirection != "-")
                {
                    strError = "ʱ�� ����ֵ Ӧ��Ϊ +/-֮һ";
                    goto ERROR1;
                }

                /*
                strTzdHour = strTimeZoneSegment.Substring(1, 2);
                if (strTzdHour.IndexOf(" ") != -1)
                {
                    strError = "ʱ�� ʱֵ�в�Ӧ�������ո��ַ�";
                    goto ERROR1;
                }

                strTzdMinute = strTimeZoneSegment.Substring(3, 2);
                if (strTzdMinute.IndexOf(" ") != -1)
                {
                    strError = "ʱ�� ��ֵ�в�Ӧ�������ո��ַ�";
                    goto ERROR1;
                }

                int hour = Convert.ToInt32(strTzdHour);
                if (hour < 0 || hour > 23)
                {
                    strError = "ʱ�� ʱֵӦ����00-23֮��";
                    goto ERROR1;
                }

                int minute = Convert.ToInt32(strTzdMinute);
                if (minute < 0 || minute > 59)
                {
                    strError = "ʱ�� ��ֵӦ����00-59֮��";
                    goto ERROR1;
                }
                 * */

                strTzdHour = strTimeZoneSegment.Substring(1, 2);

                nRet = CheckNumberRange(strTzdHour,
                    "00",
                    strTzdDirection == "-" ? "12" : "13",
                    "ʱ�� Сʱ",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                strTzdMinute = strTimeZoneSegment.Substring(3, 2);

                nRet = CheckNumberRange(strTzdMinute,
                    "00",
                    "59",
                    "ʱ�� ��",
                     out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            // �·ݻ�������ȱʡ
            if (strMonth == "" && strDay == "")
            {
                if (strTimeZoneSegment != "")
                {
                    strError = "��û������ʱ����ֵ������£�����������ʱ��ֵ";
                    goto ERROR1;
                }

                strResult = strYear;
                return 0;
            }

            if (strDay == "")
            {
                if (strTimeZoneSegment != "")
                {
                    strError = "��û������ʱ����ֵ������£�����������ʱ��ֵ";
                    goto ERROR1;
                }

                strResult = strYear + "-" + strMonth;
                return 0;
            }

            Debug.Assert(strYear != ""
                && strMonth != ""
                && strDay != "", "");

            // �����ն���ȫ������£���������ֵ���Ч��
            try
            {
                DateTime date = new DateTime(Convert.ToInt32(strYear),
                    Convert.ToInt32(strMonth),
                    Convert.ToInt32(strDay));
            }
            catch // (Exception ex)
            {
                strError = strYear + "��" + strMonth + "�²�����" + strDay + "��";
                goto ERROR1;
            }
 

            if (strHourMinuteSegment == "")
            {
                if (strSecondSegment != "")
                {
                    strError = "��������ֵ���ͱ���Ҳ����ʱ����ֵ";
                    goto ERROR1;
                }

                if (strTimeZoneSegment != "")
                {
                    strError = "��û������ʱ����ֵ������£�����������ʱ��ֵ";
                    goto ERROR1;
                }

                strResult = strYear + "-" + strMonth + "-" + strDay;
                return 0;
            }

            if (strHourMinuteSegment != ""
                && strSecondSegment != "")
            {
                strResult = strYear + "-" + strMonth + "-" + strDay + "T" + strHour + ":" + strMinute + ":" + strSecond;

                if (strSecondDecimal != "")
                {
                    // Complete date plus hours, minutes, seconds and a decimal fraction of a second
                    // YYYY-MM-DDThh:mm:ss.sTZD (eg 1997-07-16T19:20:30.45+01:00)
                    strResult += "." + strSecondDecimal;
                }
                else
                {
                    // Complete date plus hours, minutes and seconds:
                    // YYYY-MM-DDThh:mm:ssTZD (eg 1997-07-16T19:20:30+01:00)
                }

                if (strTimeZoneSegment != "")
                    strResult += strTzdDirection + strTzdHour + ":" + strTzdMinute;
                else
                    strResult += "+00:00";

                return 0;
            }


            if (strHourMinuteSegment != "")
            {
                strResult = strYear + "-" + strMonth + "-" + strDay + "T" + strHour + ":" + strMinute;

                if (strTimeZoneSegment != "")
                    strResult += strTzdDirection + strTzdHour + ":" + strTzdMinute;
                else
                    strResult += "+00:00";

                // Complete date plus hours and minutes:
                // YYYY-MM-DDThh:mmTZD (eg 1997-07-16T19:20+01:00)
                return 0;
            }

            strError = "��ʽ����";
            goto ERROR1;
            // return 0;

        ERROR1:
            return -1;
        }

        // ���ֲ˵�
        private void label_eastWest_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;



            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            //
            menuItem = new MenuItem("+\t����ʱ��");
            menuItem.Click += new System.EventHandler(this.menu_east_Click);
            if (this.label_eastWest.Text == "+")
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("-\t����ʱ��");
            menuItem.Click += new System.EventHandler(this.menu_west_Click);
            if (this.label_eastWest.Text == "-")
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            contextMenu.Show(this.label_eastWest, new Point(e.X, e.Y));
        }

        // toggle
        private void label_eastWest_DoubleClick(object sender, EventArgs e)
        {
            if (this.label_eastWest.Text == "+")
                this.label_eastWest.Text = "-";
            else
                this.label_eastWest.Text = "+";
        }

        void menu_east_Click(object sender, EventArgs e)
        {
            this.label_eastWest.Text = "+";
        }

        void menu_west_Click(object sender, EventArgs e)
        {
            this.label_eastWest.Text = "-";
        }

        /*
http://read.newbooks.com.cn/info/180524.html
(GMT) ����������������ά�ǣ��׿���δ��
(GMT) �������α�׼ʱ��: ������, ������, �׶�, ��˹��
(GMT+01:00) �з�����
(GMT+01:00) ��³�������籾��������������
(GMT+01:00) �������ѣ�˹�������ɳ�������ղ�
(GMT+01:00) ���������£�������˹������������˹��¬�������ǣ�������
(GMT+01:00) ��ķ˹�ص������֣������ᣬ����˹�¸��Ħ��άҲ��
(GMT+02:00) �����ף�����������
(GMT+02:00) ����
(GMT+02:00) ����
(GMT+02:00) ��˹��
(GMT+02:00) �µúͿ�
(GMT+02:00) Ү·����
(GMT+02:00) ��³��
(GMT+02:00) �ն���������������ӣ������ǣ����֣�ά��Ŧ˹
(GMT+02:00) �ŵ䣬������˹�أ���˹̹����
(GMT+03:00) ���ޱ�
(GMT+03:00) �͸��
(GMT+03:00) �����أ����ŵ�
(GMT+03:00) �ڱ���˹
(GMT+03:00) Ī˹�ƣ�ʥ�˵ñ�, �����Ӹ���
(GMT+03:30) �º���
(GMT+04:00) ������
(GMT+04:00) �Ϳ�
(GMT+04:00) �������ȣ���˹����
(GMT+04:00) �߼�����׼ʱ��
(GMT+04:30) ������
(GMT+05:00) ��˹�����������棬��ʲ��
(GMT+05:00) Ҷ�����ձ�
(GMT+05:30) ˹����ǻ���������
(GMT+05:30) �����˹���Ӷ����������µ���
(GMT+05:45) �ӵ�����
(GMT+06:00) ����ľͼ������������
(GMT+06:00) ��˹���ɣ��￨
(GMT+06:30) ����
(GMT+07:00) ����˹ŵ�Ƕ�˹��
(GMT+07:00) ���ȣ����ڣ��żӴ�
(GMT+08:00) ������Ŀˣ�������ͼ
(GMT+08:00) ���������죬����ر�����������³ľ��
(GMT+08:00) ̨��
(GMT+08:00) ��¡�£��¼���
(GMT+08:00) ��˹
(GMT+09:00) ���࣬���ϣ�����
(GMT+09:00) ����
(GMT+09:00) �ſ�Ŀ�
(GMT+09:30) �����
(GMT+09:30) ��������
(GMT+10:00) �ص���Ī���ȱȸ�
(GMT+10:00) ��������ī������Ϥ��
(GMT+10:00) ����˹��
(GMT+10:00) ��������˹�п�
(GMT+10:00) ������
(GMT+11:00) ��ӵ���������Ⱥ�����¿��������
(GMT+12:00) �¿����������
(GMT+12:00) 쳼ã�����Ӱ뵺�����ܶ�Ⱥ��
(GMT+13:00) Ŭ�Ⱒ�巨
(GMT-01:00) ���ٶ�Ⱥ��
(GMT-01:00) ��ý�Ⱥ��
(GMT-02:00) �д�����
(GMT-03:00) ��������
(GMT-03:00) ����ŵ˹����˹�����ζ�
(GMT-03:00) ������
(GMT-03:00) �ɵ�ά����
(GMT-03:30) Ŧ����
(GMT-04:00) ʥ���Ǹ�
(GMT-04:00) ������ʱ��(���ô�)
(GMT-04:00) ����˹
(GMT-04:00) ���˹
(GMT-04:30) ������˹
(GMT-05:00) ����ʱ��(�����ͼ��ô�)
(GMT-05:00) ӡ�ذ�����(����)
(GMT-05:00) �����������²��ʿ�
(GMT-06:00) ������
(GMT-06:00) �в�ʱ��(�����ͼ��ô�)
(GMT-06:00) �ϴ���������ī����ǣ�������(��)
(GMT-06:00) �ϴ���������ī����ǣ�������(��)
(GMT-06:00) ��˹������
(GMT-07:00) ����ɣ��
(GMT-07:00) �����ߣ�����˹����������(��)
(GMT-07:00) �����ߣ�����˹����������(��)
(GMT-07:00) ɽ��ʱ��(�����ͼ��ô�)
(GMT-08:00) ̫ƽ��ʱ��(�����ͼ��ô�)
(GMT-08:00) �ٻ��ɣ��¼�����������
(GMT-09:00) ����˹��
(GMT-10:00) ������
(GMT-11:00) ��;������Ħ��Ⱥ��
(GMT-12:00) �ս�����
 
         * */

    }
}
