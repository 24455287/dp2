using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

using DigitalPlatform.IO;

namespace dp2Catalog
{
    /// <summary>
    /// �ַ������
    /// </summary>
    public class CharsetTable : ItemFileBase
    {
        public CharsetTable()
		{

		}


		public override Item NewItem()
		{
			return new CharsetItem();
		}

        // ���ַ�
        // ���ݸ�����Key�õ�Value
        // return:
        //      -1  not found
        public int Search(string strKeyParam,
            out string strValue)
        {
            strValue = "";

            int k;	// ������
            int m;	// ������
            int j = -1;	// ������
            string strKey;
            int nComp;

            k = 0;
            m = (int)this.Count - 1;
            while (k <= m)
            {
                j = (k + m) / 2;
                // ȡ��jλ�õ�ֵ

                CharsetItem item = (CharsetItem)this[j];

                strKey = item.Name;

                nComp = String.Compare(strKey, strKeyParam);
                if (nComp == 0)
                {
                    strValue = item.Value;
                    break;
                }

                if (nComp > 0)
                {	// strKeyParam��С
                    m = j - 1;
                }
                else
                {
                    k = j + 1;
                }

            }

            if (k > m)
                return -1;	// not found

            return j;
        }

        // ��EACC�ַ��������е��ַ�ת��ΪUnicode�ַ�
        // paramters:
        //		pszEACC	Ԥ��װ��EACC�ַ��Ļ�����(�м䲻���з�EACC�ַ�)
        //		nEACCBytes		�ֽ�����ע��Ӧ��Ϊ3�ı���
        //		pUnicode	���ڴ�Ž��Unicode�ַ��Ļ�����
        //		nMaxUnicodeBytes	���������ߴ�
        // return:
        //		-1	ʧ��
        //		������ת���ɵ�Unicode�ַ���
        public int EACCToUnicode(string strEACC,
            out string strUnicode,
            out string strError)
        {

            strUnicode = "";
            strError = "";

            int nMax;
            char ch0;
            int nValue;
            int i;
            int nRet;
            char ch1;
            char ch2;
            string strPart;
            string strValue;
            // int t = 0;


            int nEACCBytes = strEACC.Length;
            if (nEACCBytes % 3 != 0)
            {
                strError = "�μ�ת�����ֽ���Ӧ��Ϊ3�ı���(����Ϊ " + nEACCBytes.ToString() + ")";
                return -1;
            }

            nMax = nEACCBytes / 3;

            for (i = 0; i < nMax; i++)
            {


                ch0 = strEACC[i * 3];
                ch1 = strEACC[(i * 3) + 1];
                ch2 = strEACC[(i * 3) + 2];

                string strEACCPart = Convert.ToString((int)ch0, 16).PadLeft(2, '0');
                strEACCPart += Convert.ToString((int)ch1, 16).PadLeft(2, '0');
                strEACCPart += Convert.ToString((int)ch2, 16).PadLeft(2, '0');

                if (strEACCPart == "212321")
                {
                    strValue = "3000";
                    goto SKIP1;
                }

                nRet = Search(strEACCPart.ToUpper(),
                    out strValue);
                if (nRet == -1)
                {
                    // strUnicode += '*';
                    strUnicode += "{"+strEACCPart+"}";
                    continue;
                }

            SKIP1:
                strPart = strValue.Substring(0, 2);
                ch1 = (char)Convert.ToInt32(strPart, 16);

                strPart = strValue.Substring(2, 2);
                ch2 = (char)Convert.ToInt32(strPart, 16);

                nValue = 0xff00 & (((Int32)(ch1)) << 8);
                nValue += 0x00ff & ch2;
                strUnicode += (char)nValue;
            }

            return strUnicode.Length;
        }

        /*
        public int Text_e2u(string strSource,
            out string strTarget)
        {
            strTarget = "";

            int nEscCount = 0;
            bool bInEsc = false;
            bool bInCJK = false;
            bool bInMultiple = false;
            string strPart = "";

            string strError = "";

            for (int i = 0; i < strSource.Length; )
            {
                // char ch = strSource[i];
                if (strSource[i] == 0x1b && nEscCount == 0)
                {	// escape code
                    bInEsc = true;
                    nEscCount = 1;

                    i++;

                    // ����strPart���Ƿ��л��۵�����
                    if (strPart != "")
                    {
                        if ((strPart.Length % 3) != 0)
                        {
                            strTarget += strPart;
                            goto CONTINUE1;
                        }
                        Debug.Assert((strPart.Length % 3) == 0, "");

                        string strTemp = "";
                        // strPart�б����ŷ�Unicode�ַ���
                        int nRet = EACCToUnicode(strPart,
                            out strTemp,
                            out strError);
                        if (nRet == -1)
                        {
                            strTarget += "[EACCToUnicode error:"
                                + strError + "][" + strPart + "]";

                            goto CONTINUE1;
                        }
                        strTarget += strTemp;

                    }
                CONTINUE1:
                    strPart = "";
                    continue;
                }

                if (bInEsc && nEscCount == 1)
                {
                    if (strSource[i] == 0x28 || strSource[i] == 0x2c)
                        bInMultiple = false;
                    else if (strSource[i] == 0x24)
                        bInMultiple = true;
                }

                if (bInEsc && nEscCount == 2)
                {
                    if (strSource[i] == 0x24 && bInMultiple == true)
                    {	// ����ͼ���$$1���
                        i++;
                        continue;	// nEscCount���䣬��������
                    }
                    if (strSource[i] == 0x28 && bInMultiple == false)
                    { // ����ͼ���((B���
                        i++;
                        continue;
                    }
                    if (strSource[i] == 0x31)
                        bInCJK = true;
                    else
                        bInCJK = false;
                    bInEsc = false;
                    nEscCount = 0;
                    i++;
                    continue;
                }


                if (bInEsc)
                    nEscCount++;

                if (bInEsc == false)
                {
                    if (bInCJK == true)
                    {
                        strPart += strSource[i];
                    }
                    else
                        strTarget += strSource[i];
                }


                i++;
            }



            // ����strPart���Ƿ��л��۵�����
            if (strPart != "")
            {
                if ((strPart.Length % 3) != 0)
                {
                    strTarget += strPart;
                    return 0;
                }
                Debug.Assert((strPart.Length % 3) == 0, "");

                string strTemp = "";

                // strPart�б����ŷ�Unicode�ַ���
                int nRet = EACCToUnicode(strPart,
                    out strTemp,
                    out strError);
                if (nRet == -1)
                {
                    strTarget += "[EACCToUnicode error:"
                        + strError + "][" + strPart + "]";
                    return 0;
                }
                strTarget += strTemp;

            }

            return 0;
        }
         * */




    }


    public class CharsetItem : Item
    {
        int m_nLength = 0;
        byte[] m_buffer = null;

        public string Name
        {
            get
            {
                string strValue = this.Content;
                int nRet = strValue.IndexOf('\t');
                if (nRet == -1)
                    return strValue;
                return strValue.Substring(0, nRet);
            }
        }

        public string Value
        {
            get
            {
                string strValue = this.Content;
                int nRet = strValue.IndexOf('\t');
                if (nRet == -1)
                    return null;
                return strValue.Substring(nRet+1);
            }
        }

        public string Content
        {
            get
            {
                return Encoding.UTF8.GetString(this.m_buffer);
            }
            set
            {
                m_buffer = Encoding.UTF8.GetBytes(value);
                this.Length = m_buffer.Length;
            }
        }

        public override int Length
        {
            get
            {
                return m_nLength;
            }
            set
            {
                m_nLength = value;
            }
        }

        public override void ReadData(Stream stream)
        {
            if (this.Length == 0)
                throw new Exception("length��δ��ʼ��");


            // ����Length��bytes������
            m_buffer = new byte[this.Length];
            stream.Read(m_buffer, 0, m_buffer.Length);
        }


        public override void ReadCompareData(Stream stream)
        {
            if (this.Length == 0)
                throw new Exception("length��δ��ʼ��");


            // ����Length��bytes������
            m_buffer = new byte[this.Length];
            stream.Read(m_buffer, 0, m_buffer.Length);
        }

        public override void WriteData(Stream stream)
        {
            if (m_buffer == null)
            {
                throw (new Exception("m_buffer��δ��ʼ��"));
            }


            // д��Length��bytes������
            stream.Write(m_buffer, 0, this.Length);
        }

        // ʵ��IComparable�ӿڵ�CompareTo()����,
        // ����ID�Ƚ���������Ĵ�С���Ա�����
        // ���Ҷ��뷽ʽ�Ƚ�
        // obj: An object to compare with this instance
        // ����ֵ A 32-bit signed integer that indicates the relative order of the comparands. The return value has these meanings:
        // Less than zero: This instance is less than obj.
        // Zero: This instance is equal to obj.
        // Greater than zero: This instance is greater than obj.
        // �쳣: ArgumentException,obj is not the same type as this instance.
        public override int CompareTo(object obj)
        {
            CharsetItem item = (CharsetItem)obj;

            return String.Compare(this.Name, item.Name);
        }
    }
}
