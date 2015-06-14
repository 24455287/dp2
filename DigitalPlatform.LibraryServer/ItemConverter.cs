using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;

using DigitalPlatform.IO;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// dp2library�е���C#�ű�ʱ, ����ת������Ϣxml->html�Ľű���Ļ���
    /// </summary>
    public class ItemConverter
    {
        public LibraryApplication App = null;

        public ItemConverter()
        {

        }

        public virtual void Begin(object sender,
    ItemConverterEventArgs e)
        {

        }

        public virtual void Item(object sender,
            ItemConverterEventArgs e)
        {

        }

        public virtual void End(object sender,
            ItemConverterEventArgs e)
        {

        }

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

        /*
        public static string LocalDate(string strRfc1123Time)
        {
            if (String.IsNullOrEmpty(strRfc1123Time) == true)
                return "";
            return DateTimeUtil.Rfc1123DateTimeStringToLocal(strRfc1123Time, "yyyy-MM-dd");
        }*/

        // ��RFC1123ʱ���ַ���ת��Ϊ����һ�������ַ���
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
    }

    public class ItemConverterEventArgs : EventArgs
    {
        public string Xml = "";
        public string RecPath = ""; // 2009/10/18
        public int Index = -1;
        public int Count = 0;
        public string ActiveBarcode = "";

        public string ResultString = "";
        public Control ParentControl = null;
    }
}

