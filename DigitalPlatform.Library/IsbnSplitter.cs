using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;

using DigitalPlatform.Xml;

namespace DigitalPlatform.Library
{
    /// <summary>
    /// ISBN�ŷ���������������'-'
    /// </summary>
    public class IsbnSplitter1
    {
        XmlDocument dom = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="strIsbnFileName"></param>
        public IsbnSplitter1(string strIsbnFileName)
        {
            dom = new XmlDocument();
            dom.Load(strIsbnFileName);
        }

        static bool InRange(string strValue,
            string strStart,
            string strEnd)
        {
            if (String.Compare(strValue, strStart) < 0)
                return false;
            if (String.Compare(strValue, strEnd) > 0)
                return false;

            return true;
        }

        static bool IsNumber(string strText)
        {
            for (int i = 0; i < strText.Length; i++)
            {
                if (strText[0] < '0' || strText[0] > '9')
                    return false;
            }

            return true;
        }

        
        /// <summary>
        ///  У��ISBN��һ�����Ƿ���ȷ
        /// </summary>
        /// <param name="strFirstPart"></param>
        /// <param name="strError"></param>
        /// <returns>return:-1	error,0	correct</returns>
        public static int VerifyIsbnFirstPart(string strFirstPart,
                        out string strError)
        {
            strError = "";

            if (IsNumber(strFirstPart) == false)
            {
                strError = "ISBN��һ����Ӧ��Ϊ������";
                goto WRONG;
            }


            if (String.IsNullOrEmpty(strFirstPart) == true)
            {
                strError = "ISBN��һ�����ַ�������Ϊ0";
                goto WRONG;
            }
            if (strFirstPart.Length == 1)
            {
                if (InRange(strFirstPart, "0", "7") == true)
                    goto CORRECT;
                else
                {
                    strError = "���ISBN��һ����('" + strFirstPart + "')Ϊ1�ַ�����ȡֵ��ΧӦ��Ϊ 0-7";
                    goto WRONG;
                }
            }
            else if (strFirstPart.Length == 2)
            {
                if (InRange(strFirstPart, "80", "94") == true)
                    goto CORRECT;
                else
                {
                    strError = "���ISBN��һ����('"
                        + strFirstPart + "')Ϊ2�ַ�����ȡֵ��ΧӦ��Ϊ 80-94";
                    goto WRONG;
                }
            }

            else if (strFirstPart.Length == 3)
            {
                if (InRange(strFirstPart, "950", "994") == true)
                    goto CORRECT;
                else
                {
                    strError = "���ISBN��һ����('" + strFirstPart + "')Ϊ3�ַ�����ȡֵ��ΧӦ��Ϊ 950-994";
                    goto WRONG;
                }
            }

            else if (strFirstPart.Length == 4)
            {
                if (InRange(strFirstPart, "9950", "9989") == true)
                    goto CORRECT;
                else
                {
                    strError = "���ISBN��һ����('" + strFirstPart + "')Ϊ4�ַ�����ȡֵ��ΧӦ��Ϊ 9950-9989";
                    goto WRONG;
                }
            }

            else if (strFirstPart.Length == 5)
            {
                if (InRange(strFirstPart, "99900", "99999") == true)
                    goto CORRECT;
                else
                {
                    strError = "���ISBN��һ����('" + strFirstPart + "')Ϊ5�ַ�����ȡֵ��ΧӦ��Ϊ 99900-99999";
                    goto WRONG;
                }
            }

            strError = "ISBN��һ�����ַ������ܳ���5";
        WRONG:
            return -1;
        CORRECT:
            return 0;
        }

        /// <summary>
        /// �����У��λ
        /// </summary>
        /// <param name="strISBN"></param>
        /// <returns></returns>
        public static char GetIsbnVerifyChar(string strISBN)
        {
            strISBN = strISBN.Trim();
            strISBN = strISBN.Replace("-", "");
            strISBN = strISBN.Replace(" ", "");


            if (strISBN.Length < 9)
                throw new Exception("���ڼ���У��λ��ISBN��������Ҫ��9λ��������(�������������)");

            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                sum += (strISBN[i] - '0') * (i + 1);
            }
            int v = sum % 11;

            if (v == 10)
                return 'X';

            return (char)('0' + v);
        }

        /// <summary>
        /// ��ISBN�ӷ������ʵ���λ�ò���'-'����
        /// </summary>
        /// <param name="strISBN"></param>
        /// <param name="strTarget"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public int IsbnInsertHyphen(
            string strISBN,
            out string strTarget,
            out string strError)
        {
            strTarget = "";
            strError = "";

            string strSource;
            int nFirstLen;
            int nSecondLen;

            strSource = strISBN;
            strSource = strSource.Trim();

            // �Ƿ�Ϊ�����
            if (strSource.Length == 13
                && strSource.IndexOf("-") == -1
                && strSource.Substring(0, 3) == "978")
            {
                strSource = strSource.Substring(3, 9);  // ����ǰ��λ�������һλ

                // ���У��λ
                char v = GetIsbnVerifyChar(strSource);
                strSource += new string(v, 1);
            }

            strSource = strSource.Replace("-", "");
            strSource = strSource.Replace(" ", "");

            if (strSource.Length != 10)
            {
                strError = "ISBN��(��'-'����)ӦΪ10λ��Ч�ַ�(" + strSource + " " + Convert.ToString(strSource.Length) + ")";
                return -1;
            }

            // �۲��һ����
            string strFirstPart = strSource.Substring(0, 1);
            if (InRange(strFirstPart, "0", "7") == true)
            {
                nFirstLen = 1;
                goto DOSECOND;
            }

            strFirstPart = strSource.Substring(0, 2);
            if (InRange(strFirstPart, "80", "94") == true)
            {
                nFirstLen = 2;
                goto DOSECOND;
            }

            strFirstPart = strSource.Substring(0, 3);
            if (InRange(strFirstPart, "950", "994") == true)
            {
                nFirstLen = 3;
                goto DOSECOND;
            }

            strFirstPart = strSource.Substring(0, 4);
            if (InRange(strFirstPart, "9950", "9989") == true)
            {
                nFirstLen = 4;
                goto DOSECOND;
            }

            strFirstPart = strSource.Substring(0, 5);
            if (InRange(strFirstPart, "99900", "99999") == true)
            {
                nFirstLen = 5;
                goto DOSECOND;
            }

            strError = "��һ���ָ�ʽ����";    // �Ƿ���Ҫ����һ��?
            return -1;

        DOSECOND:

            XmlNodeList nodes = this.dom.DocumentElement.SelectNodes("agency/group[@name='" + strFirstPart + "']/range");
            if (nodes.Count == 0)
            {
                strError = "ISBN������û���ҵ�name='" + strFirstPart + "'��<group>Ԫ�� ...";
                return -1;
            }



            string strSecondPart = "";

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strValue = DomUtil.GetAttr(node, "value").Trim();

                int nRet = strValue.IndexOf('-');
                if (nRet == -1)
                {
                    strError = "���ݽڵ� " + node.OuterXml + "��ʽ����, valueֵ����'-'";
                    return -1;
                }

                string strLeft = strValue.Substring(0, nRet).Trim();
                string strRight = strValue.Substring(nRet + 1).Trim();

                if (strLeft.Length != strRight.Length)
                {
                    strError = "���ݽڵ� " + node.OuterXml + "��ʽ����, valueֵ'" + strValue + "'���������ֿ�Ȳ��ȡ�";
                    return -1;
                }

                int nWidth = strLeft.Length;

                if (nWidth != strSecondPart.Length)
                    strSecondPart = strSource.Substring(nFirstLen, nWidth);


                if (InRange(strSecondPart, strLeft, strRight) == true)
                {
                    nSecondLen = nWidth;
                    goto FINISH;
                }

            }

            strError = "�ڶ����ָ�ʽ���� nFirstLen=[" + Convert.ToString(nFirstLen);
            return -1;

        FINISH:
            strTarget = strSource;

            strTarget = strTarget.Insert(nFirstLen, "-");
            strTarget = strTarget.Insert(nFirstLen + nSecondLen + 1, "-");
            strTarget = strTarget.Insert(9 + 1 + 1, "-");

            return 0;
        }

    }
}
