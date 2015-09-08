using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;

using DigitalPlatform.Xml;
using DigitalPlatform.Text;

namespace DigitalPlatform.Script
{
    /// <summary>
    /// ISBN�ŷ���������������'-'
    /// </summary>
    public class IsbnSplitter
    {
        XmlDocument dom = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="strIsbnFileName">ISBN �����ļ���XML ��ʽ</param>
        public IsbnSplitter(string strIsbnFileName)
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
        ///  У�� ISBN ��һ�����Ƿ���ȷ
        /// </summary>
        /// <param name="strFirstPart">ISBN �ĵ�һ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ��ȷ</returns>
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

        // У�� ISBN �ַ���
        // ע������ -1 �� ���� 1 ������-1 ��ʾ���ù��̳�����ʾ�������� ISBN �ַ���Ӧ��Ԥ�ȼ�飬�������ϻ�����ʽҪ���������ñ�����
        // return:
        //      -1  ����
        //      0   У����ȷ
        //      1   У�鲻��ȷ����ʾ��Ϣ��strError��
        public static int VerifyISBN(string strISBNParam,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(strISBNParam) == true)
            {
                strError = "ISBN�ַ�������Ϊ��";
                return -1;
            }
            
            // 2015/9/7
            string strISBN = strISBNParam.Trim();
            if (string.IsNullOrEmpty(strISBN) == true)
            {
                strError = "ISBN�ַ�������Ϊ��(1)";
                return -1;
            }

            strISBN = strISBNParam.Replace("-", "").Replace(" ","");
            if (string.IsNullOrEmpty(strISBN) == true)
            {
                strError = "ISBN�ַ�������Ϊ��";
                return 1;
            }

            if (strISBN.Length != 10 && strISBN.Length != 13)
            {
                strError = "(���ַ�'-'�Ϳո���)ISBN�ַ����ĳ��ȼȲ���10λҲ����13λ";
                return 1;
            }

            if (strISBN.Length == 10)
            {
                try
                {
                    char c = GetIsbn10VerifyChar(strISBN);
                    if (c != strISBN[9])
                    {
                        strError = "ISBN '" + strISBN + "' У�鲻��ȷ";
                        return 1;
                    }
                }
                catch(ArgumentException ex)
                {
                    strError = "ISBN '" + strISBN + "' У�鲻��ȷ: " + ex.Message;
                    return 1;
                }
            }

            if (strISBN.Length == 13)
            {
                //
                char c = GetIsbn13VerifyChar(strISBN);
                if (c != strISBN[12])
                {
                    strError = "ISBN '" + strISBN + "' У�鲻��ȷ";
                    return 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// ����� ISBN-10 У��λ
        /// </summary>
        /// <param name="strISBN">ISBN �ַ���</param>
        /// <returns>У��λ�ַ�</returns>
        public static char GetIsbn10VerifyChar(string strISBN)
        {
            strISBN = strISBN.Trim();
            strISBN = strISBN.Replace("-", "");
            strISBN = strISBN.Replace(" ", "");

            if (strISBN.Length < 9)
                throw new ArgumentException("���ڼ���У��λ��ISBN-10��������Ҫ��9λ��������(�������������)");

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
        /// ����� ISBN-13 У��λ
        /// </summary>
        /// <param name="strISBN">ISBN �ַ���</param>
        /// <returns>У��λ�ַ�</returns>
        public static char GetIsbn13VerifyChar(string strISBN)
        {
            strISBN = strISBN.Trim();
            strISBN = strISBN.Replace("-", "");
            strISBN = strISBN.Replace(" ", "");


            if (strISBN.Length < 12)
                throw new Exception("���ڼ���У��λ��ISBN-13��������Ҫ��12λ��������(�������������)");

            int m = 0;
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                if ((i % 2) == 0)
                    m = 1;
                else
                    m = 3;

                sum += (strISBN[i] - '0') * m;
            }

            // ע���������5��������Ϊ0����У����Ϊ0��
            if ((sum % 10) == 0)
                return '0';

            int v = 10 - (sum % 10);

            return (char)('0' + v);
        }


        /// <summary>
        /// �� ISBN �ַ������ʵ���λ�ò���'-'����
        /// ����ṩ��ISBN�ַ�����������978ǰ׺����ô����Խ�����ǰ׺�����������û�У��������Ҳû�С�
        /// </summary>
        /// <param name="strISBN">ISBN �ַ���</param>
        /// <param name="strStyle">������force10/force13/auto/remainverifychar/strict</param>
        /// <param name="strTarget">���ش�����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1:����; 0:δ�޸�У��λ; 1:�޸���У��λ</returns>
        public int IsbnInsertHyphen(
            string strISBN,
            string strStyle,
            out string strTarget,
            out string strError)
        {
            strTarget = "";
            strError = "";

            string strSource;
            // int nFirstLen = 0;
            int nSecondLen;

            // Debug.Assert(false, "");

            strSource = strISBN;
            strSource = strSource.Trim();

            bool bHasRemovePrefix978 = false; // �Ƿ���978ǰ׺

            bool bForce10 = StringUtil.IsInList("force10", strStyle);
            bool bForce13 = StringUtil.IsInList("force13", strStyle);
            bool bAuto = StringUtil.IsInList("auto", strStyle);
            bool bRemainVerifyChar = StringUtil.IsInList("remainverifychar", strStyle); // �Ƿ�Ҫ���¼���У��λ
            bool bStrict = StringUtil.IsInList("strict", strStyle); // �Ƿ��ϸ�Ҫ��strISBN�������Ϊ10��13λ

            int nCount = 0;
            if (bForce10 == true)
                nCount++;
            if (bForce13 == true)
                nCount++;
            if (bAuto == true)
                nCount++;

            if (nCount > 1)
            {
                strError = "strStyleֵ '"+strStyle+"' �е�force10/force13/auto 3�ַ���ǻ����ų⣬����ͬʱ�߱���";
                return -1;
            }

            strSource = strSource.Replace("-", "");
            strSource = strSource.Replace(" ", "");

            bool bAdjustLength = false; // �Ƿ�����������strISBN�ĳ���

            if (bStrict == false)
            {
                if (strSource.Length == 9)
                {
                    strSource += '0';
                    bRemainVerifyChar = false;  // ����Ҫ���¼���У��λ��
                    bAdjustLength = true;
                }
                else if (strSource.Length == 12)
                {
                    strSource += '0';
                    bRemainVerifyChar = false;  // ����Ҫ���¼���У��λ��
                    bAdjustLength = true;
                }
            }

            string strPrefix = "978";

            // 13λ����-��ǰ׺Ϊ978
            if (strSource.Length == 13
                && strSource.IndexOf("-") == -1
                && (strSource.Substring(0, 3) == "978" || strSource.Substring(0, 3) == "979")
                )
            {
                if (strSource.Length >= 3)
                    strPrefix = strSource.Substring(0, 3);

                strSource = strSource.Substring(3, 10); // ����ǰ3λ����������У��λ

                bHasRemovePrefix978 = true;
            }

            if (strSource.Length != 10
                && strSource.Length != 13)
            {
                strError = "ISBN��(��'-'����)ӦΪ10λ��13λ��Ч�ַ�(" + strSource + " " + Convert.ToString(strSource.Length) + ")";
                return -1;
            }

            if (bForce10 && strPrefix == "979")
            {
                strError = "979 ǰ׺�� ISBN ���ܱ�Ϊ ISBN-10 ��̬";
                return -1;
            }

            string strFirstPart = "";   // ��һ����

            XmlElement hit_prefix = null;

            XmlNodeList prefix_nodes = dom.DocumentElement.SelectNodes("RegistrationGroups/Group/Prefix");
            if (prefix_nodes.Count == 0)
            {
                strError = "ISBN �����ļ���ʽ�����޷�ѡ���κ� RegistrationGroups/Group/Prefix Ԫ��";
                return -1;
            }
            string strTemp = strPrefix + strSource;
            foreach (XmlElement prefix in prefix_nodes)
            {
                string strCurrent = prefix.InnerText.Trim().Replace("-", "");
                if (strTemp.StartsWith(strCurrent) == true)
                {
                    hit_prefix = prefix;
                    strFirstPart = strCurrent.Substring(3);
                    // nFirstLen = strFirstPart.Length;
                    break;
                }
            }

            if (hit_prefix == null)
            {
                strError = "prefix ���ָ�ʽ����";    // �Ƿ���Ҫ����һ��?
                return -1;
            }

            XmlNodeList nodes = hit_prefix.ParentNode.SelectNodes("Rules/Rule");
            if (nodes.Count == 0)
            {
                strError = "ISBN ������ û���ҵ� prefix='" + strFirstPart + "'�� Rules/Rule Ԫ�� ...";
                return -1;
            }

            string strSecondPart = "";

            foreach(XmlElement node in nodes)
            {
                Range range = GetRangeValue(node);
                if (range == null)
                    continue;   // TODO: ��Ҫ����

#if NO
                if (strLeft.Length != strRight.Length)
                {
                    strError = "���ݽڵ� " + node.OuterXml + "��ʽ����, valueֵ'" + strValue + "'���������ֿ�Ȳ��ȡ�";
                    return -1;
                }
#endif

                int nWidth = range.Left.Length;

                if (nWidth == 0)
                    continue;   // ���������д���? 

                if (nWidth != strSecondPart.Length)
                    strSecondPart = strSource.Substring(strFirstPart.Length, nWidth);

                if (InRange(strSecondPart, range.Left, range.Right) == true)
                {
                    nSecondLen = nWidth;
                    goto FINISH;
                }

            }

            strError = "�ڶ����ָ�ʽ���� nFirstLen=[" + Convert.ToString(strFirstPart.Length) + "]";
            return -1;

        FINISH:
            strTarget = strSource;

        strTarget = strTarget.Insert(strFirstPart.Length, "-");
        strTarget = strTarget.Insert(strFirstPart.Length + nSecondLen + 1, "-");
            strTarget = strTarget.Insert(9 + 1 + 1, "-");

            if (bForce13 == true)
            {
                if (strTarget.Length == 13)
                    strTarget = strPrefix + "-" + strTarget;
            }
            else if (bAuto == true && bHasRemovePrefix978 == true)
            {
                strTarget = strPrefix + "-" + strTarget;
            }

            bool bVerifyChanged = false;

            // ���¼���У����
            // �������ISBN-10��У��λ����Ϊ�������ISBN-13У��λ�㷨��ͬ��
            if (bRemainVerifyChar == false)
            {
                if (strTarget.Length == 13)
                {
                    char old_ver = strTarget[12];
                    strTarget = strTarget.Substring(0, strTarget.Length - 1);
                    char v = GetIsbn10VerifyChar(strTarget);
                    strTarget += new string(v, 1);

                    if (old_ver != v)
                        bVerifyChanged = true;
                }
                else if (strTarget.Length == 17)
                {
                    char old_ver = strTarget[16];

                    strTarget = strTarget.Substring(0, strTarget.Length - 1);
                    char v = GetIsbn13VerifyChar(strTarget);
                    strTarget += new string(v, 1);

                    if (old_ver != v)
                        bVerifyChanged = true;
                }
            }

            if (bHasRemovePrefix978 == true
                && bForce10 == true)
                return 0;   // ����978��У��λ�϶�Ҫ�����仯����˲�֪ͨ���ֱ仯

            if (bAdjustLength == false
                && bForce13 == true
                && strISBN.Trim().Replace("-", "").Length == 10)
                return 0;   // ������ǰ׺��У��λ�϶�Ҫ�����仯����˲�֪ͨ���ֱ仯

            if (bVerifyChanged == true)
                return 1;

            return 0;
        }

        class Range
        {
            public string Left = "";
            public string Right = "";
        }

        static Range GetRangeValue(XmlElement element)
        {
            string strRange = "";
            XmlElement range = element.SelectSingleNode("Range") as XmlElement;
            if (range == null)
                return null;

            strRange = range.InnerText.Trim();

            string strLength = "";
            XmlElement length = element.SelectSingleNode("Length") as XmlElement;
            if (length == null)
                return null;

            strLength = length.InnerText.Trim();

            int nLength = 0;
            if (int.TryParse(strLength, out nLength) == false)
                return null;

            string strLeft = "";
            string strRight = "";

            StringUtil.ParseTwoPart(strRange, "-", out strLeft,
                out strRight);
            Range result = new Range();
            result.Left = strLeft.Substring(0, nLength);
            result.Right = strRight.Substring(0, nLength);

            return result;
        }

        // ��ISBN���ַ����任Ϊͼ���������̬��ISBN�ַ���
        // ���裺
        // 1)ȥ�����е�'-'
        // 2)���ǲ�����ǰ׺'978'�����û�У��ͼ���
        // 3)���¼���У��λ
        public static string GetISBnBarcode(string strPureISBN)
        {
            string strText = strPureISBN.Replace("-", "");
            if (strText.Length < 3)
                return strText; // error

            string strHead = strPureISBN.Substring(0, 3);

            if (strHead == "978" || strHead == "979")
            {
            }
            else
            {
                strText = "978" + strText;
            }

            try
            {
                char v = GetIsbn13VerifyChar(strText);
                strText = strText.Substring(0, 12);
                strText += v;

                return strText;
            }
            catch
            {
                return strText; // error
            }

        }

        public static bool IsIsbn13(string strSource)
        {
            if (string.IsNullOrEmpty(strSource) == true)
                return false;
            strSource = strSource.Replace("-", "").Trim();
            if (string.IsNullOrEmpty(strSource) == true)
                return false;

            // 13λ����-��ǰ׺Ϊ978
            if (strSource.Length == 13
                && strSource.IndexOf("-") == -1
                && ( strSource.Substring(0, 3) == "978" || strSource.Substring(0, 3) == "979")
                )
                return true;

            return false;
        }

        public static string GetPublisherCode(string strSource)
        {
            if (strSource.IndexOf("-") == -1)
            {
                throw new Exception("ISBN '" + strSource + "' ��û�з���'-'���޷�ȡ��������벿�֡�����ΪISBN����'-'");
            }

            string[] parts = strSource.Split(new char[] { '-' });
            if (IsIsbn13(strSource) == true)
            {
                if (parts.Length >= 3)
                    return parts[2].Trim();
            }
            else
            {
                if (parts.Length >= 2)
                    return parts[1].Trim();
            }

            throw new Exception("ISBN '" + strSource + "' ��ʽ����ȷ������'-'��Ŀ����");
        }
    }
}
