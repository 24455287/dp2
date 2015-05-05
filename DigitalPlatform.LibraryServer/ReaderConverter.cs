using System;
using System.Collections.Generic;
using System.Text;

using DigitalPlatform.Text;
using DigitalPlatform.IO;

namespace DigitalPlatform.LibraryServer
{
    // ��������
    // ������ʾ����ҳ���ϸ��
    public enum OperType
    {
        None = 0,   // �Ȳ��ǽ���Ҳ���ǻ���
        Borrow = 1, // ����
        Return = 2, // ����
    }

    /// <summary>
    /// dp2library�е���C#�ű�ʱ, ����ת��������Ϣxml->html�Ľű���Ļ���
    /// </summary>
    public class ReaderConverter
    {
        public LibraryApplication App = null;
        public SessionInfo SessionInfo = null;

        public string[] BorrowedItemBarcodes = null;
        public string CurrentItemBarcode = "";  // ��ǰ���ڲ����������
        public OperType OperType = OperType.None;
        public string RecPath = ""; // ���߼�¼·��

        public string LibraryCode = ""; // ���߼�¼�������Ķ��߿��ͼ��ݴ��� 2012/9/8

        public string Formats = ""; // �Ӹ�ʽ��Ϊ���ż�����ַ����б� 2013/12/4

        public static string LocalTime(string strRfc1123Time)
        {
            try
            {
                if (String.IsNullOrEmpty(strRfc1123Time) == true)
                    return "";
                return DateTimeUtil.Rfc1123DateTimeStringToLocal(strRfc1123Time, "G");
            }
            catch (Exception /*ex*/)    // 2008/10/28
            {
                return "ʱ���ַ��� '" + strRfc1123Time + "' ��ʽ���󣬲��ǺϷ���RFC1123��ʽ";
            }
        }

        public static string LocalDate(string strRfc1123Time)
        {
            try
            {
                if (String.IsNullOrEmpty(strRfc1123Time) == true)
                    return "";

                return DateTimeUtil.Rfc1123DateTimeStringToLocal(strRfc1123Time, "d"); // "yyyy-MM-dd"
            }
            catch (Exception /*ex*/)    // 2008/10/28
            {
                return "�����ַ��� '" + strRfc1123Time + "' ��ʽ���󣬲��ǺϷ���RFC1123��ʽ";
            }
        }

        // �����ͨ����
        public string GetParam(string strReaderType,
            string strBookType,
            string strParamName)
        {
            string strError = "";
            string strParamValue = "";
            MatchResult matchresult;
            int nRet = this.App.GetLoanParam(
                //null,
                this.LibraryCode,
                strReaderType, 
                strBookType,
                strParamName,
                out strParamValue,
                out matchresult,
                out strError);
            if (nRet == -1 || nRet == 0)
                return strError;

            // 2014/1/28
            // ���Ƕ������еĲ�����Ҫ����뷵��ֵ�� 3 ������
            if (string.IsNullOrEmpty(strBookType) == true && nRet < 3)
                return strError;
            if (string.IsNullOrEmpty(strBookType) == false && nRet < 4)
                return strError;

            return strParamValue;
        }

        // ʵ�ú���������һ��������Ƿ�Ϊ����Ѿ����Ĺ��Ĳ������
        public bool IsRecentBorrowedItem(string strBarcode)
        {
            if (BorrowedItemBarcodes == null)
                return false;

            for (int i = 0; i < BorrowedItemBarcodes.Length; i++)
            {
                if (strBarcode == this.BorrowedItemBarcodes[i])
                    return true;
            }

            return false;
        }

        public ReaderConverter()
        {

        }

        public virtual string Convert(string strXml)
        {

            return strXml;
        }
    }
}

