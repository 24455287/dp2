using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;

using DigitalPlatform.Xml;
using DigitalPlatform.Text;

namespace DigitalPlatform.rms
{
    public class TableInfo : IComparable
    {
        private XmlNode m_node = null;	// XmlNode�ڵ�
        Hashtable m_captionTable = new Hashtable();
        public XmlNode Node
        {
            get
            {
                return this.m_node;
            }
            set
            {
                this.m_node = value;
                m_captionTable.Clear();
            }
        }

        public string SqlTableName = "";	// Sql����
        public string ID = "";			// ��ID
        public string TypeString = "";        // �����ͣ����
        public string ExtTypeString = "";       // _time �ȼ�������

        public XmlNode nodeConvertQueryString = null;	// ��������ʵ��ַ�����̬�����ýڵ� 
        public XmlNode nodeConvertQueryNumber = null;	// ��������ʵ�������̬�����ýڵ�

        public XmlNode nodeConvertKeyString = null;		// �����������ַ�����̬���ýڵ�
        public XmlNode nodeConvertKeyNumber = null;		// ����������������̬�����ýڵ�

        public bool Dup = false;

        public int OriginPosition = -1;  //δ��ʼ��
        public bool m_bQuery = false;

        // parameters:
        //		node	<table>�ڵ�
        //      strKeysTableNamePrefix  ����ǰ׺�ַ�������� == null����ʾʹ��"keys_"�������Ҫǰ׺��Ӧ����""
        public int Initial(XmlNode node,
            string strKeysTableNamePrefix,
            out string strError)
        {
            Debug.Assert(node != null, "Initial()���ô���node����ֵ����Ϊnull��");
            strError = "";

            this.Node = node;

            string strPartSqlTableName = DomUtil.GetAttr(this.m_node, "name").Trim();
            if (strPartSqlTableName == "")
            {
                strError = "δ���� 'name' ���ԡ�";
                return -1;
            }

            if (string.Compare(strPartSqlTableName, "records", true) == 0)
            {
                strError = "'name' �����еı�������Ϊ 'records'����Ϊ����һ��ϵͳ�ı����֡�";
                return -1;
            }

            if (strKeysTableNamePrefix == null)
                strKeysTableNamePrefix = "keys_";

            this.SqlTableName = strKeysTableNamePrefix + strPartSqlTableName;

            if (node != null)
            {
                this.ID = DomUtil.GetAttr(node, "id");
                this.TypeString = DomUtil.GetAttr(node, "type");
            }

            if (this.ID == "")
            {
                strError = "δ����'id'���ԡ�";
                return -1;
            }

            XmlNode nodeConvert = node.SelectSingleNode("convert");
            if (nodeConvert != null)
            {
                string strStopwordTable = DomUtil.GetAttrDiff(nodeConvert, "stopwordTable");
                if (strStopwordTable != null)
                {
                    strError = "keys�����ļ��Ǿɰ汾����Ŀǰ<convert>Ԫ���Ѿ���֧��'stopwordTable'���ԡ����޸������ļ�";
                    return -1;
                }
            }

            XmlNode nodeConvertQuery = node.SelectSingleNode("convertquery");
            if (nodeConvertQuery != null)
            {
                string strStopwordTable = DomUtil.GetAttrDiff(nodeConvertQuery, "stopwordTable");
                if (strStopwordTable != null)
                {
                    strError = "keys�����ļ��Ǿɰ汾����Ŀǰ<convertquery>Ԫ���Ѿ���֧��'stopwordTable'���ԡ����޸������ļ�";
                    return -1;
                }
            }

            this.nodeConvertKeyString = node.SelectSingleNode("convert/string");
            this.nodeConvertKeyNumber = node.SelectSingleNode("convert/number");

            this.nodeConvertQueryString = node.SelectSingleNode("convertquery/string");
            this.nodeConvertQueryNumber = node.SelectSingleNode("convertquery/number");

            SetExtTypeString();
            return 0;
        }

        // 2012/5/16
        void SetExtTypeString()
        {
            if (this.nodeConvertKeyNumber != null
                && this.nodeConvertQueryNumber != null)
            {
                string strExtStyle = "";
                string strStyleKey = DomUtil.GetAttr(this.nodeConvertKeyNumber, "style");
                string strStyleQuery = DomUtil.GetAttr(this.nodeConvertKeyNumber, "style");
                if (StringUtil.IsInList("freetime", strStyleQuery) == true)
                {
                    StringUtil.SetInList(ref strExtStyle, "_time", true);
                    StringUtil.SetInList(ref strExtStyle, "_freetime", true);
                }

                if (StringUtil.IsInList("rfc1123time", strStyleQuery) == true)
                {
                    StringUtil.SetInList(ref strExtStyle, "_time", true);
                    StringUtil.SetInList(ref strExtStyle, "_rfc1123time", true);
                }

                if (StringUtil.IsInList("utime", strStyleQuery) == true)
                {
                    StringUtil.SetInList(ref strExtStyle, "_time", true);
                    StringUtil.SetInList(ref strExtStyle, "_utime", true);
                }

                this.ExtTypeString = strExtStyle;
            }
        }


        // �õ��Զ��ŷָ����������԰汾��Ϣ
        public string GetAllCaption()
        {
            string strCaptions = "";
            XmlNodeList nodeList = this.m_node.SelectNodes("caption");
            foreach (XmlNode node in nodeList)
            {
                if (strCaptions != "")
                    strCaptions += ",";
                strCaptions += node.InnerText.Trim();    // 2012/2/16
            }

            if (strCaptions != "")
                strCaptions += ",";
            strCaptions += "@" + this.ID;

            return strCaptions;
        }

        // ����������Դ���ı�ǩ
        // ÿ���ַ���, ��������Դ���, ���һ��ð��, �ұ�����������
        public List<string> GetAllLangCaption()
        {
            List<string> results = new List<string>();

            XmlNode node = this.m_node;

            XmlNodeList nodes = node.SelectNodes("caption");
            for (int i = 0; i < nodes.Count; i++)
            {
                string strLang = DomUtil.GetAttr(nodes[i], "lang");
                string strText = nodes[i].InnerText;

                results.Add(strLang + ":" + strText);
            }

            return results;
        }

        // �л���İ汾
        public string GetCaption(string strLang)
        {
            string strResult = (string)this.m_captionTable[strLang == null ? "<null>" : strLang];
            if (strResult != null)
                return strResult;

            strResult = GetCaptionInternal(strLang);
            this.m_captionTable[strLang == null ? "<null>" : strLang] = strResult;
            return strResult;
        }

        // ȡһ���ڵ�ָ����ĳ�����Դ���ı�ǩ��Ϣ
        public string GetCaptionInternal(string strLang)
        {
            XmlNode node = this.m_node;

            XmlNode nodeCaption = null;
            string strCaption = "";
            string strXPath = "";

            if (strLang == null)
                goto END1;

            strLang = strLang.Trim();
            if (strLang == "")
                goto END1;

            // 1.�Ⱦ�ȷ��
            strXPath = "caption[@lang='" + strLang + "']";
            nodeCaption = node.SelectSingleNode(strXPath);
            if (nodeCaption != null)
                strCaption = nodeCaption.InnerText.Trim();   // 2012/2/16
            if (string.IsNullOrEmpty(strCaption) == false)
                return strCaption;


            // 2.�����԰汾�س����ַ���ȷ��
            if (strLang.Length >= 2)
            {
                string strShortLang = strLang.Substring(0, 2);
                strXPath = "caption[@lang='" + strShortLang + "']";
                nodeCaption = node.SelectSingleNode(strXPath);
                if (nodeCaption != null)
                    strCaption = nodeCaption.InnerText.Trim(); // 2012/2/16
                if (string.IsNullOrEmpty(strCaption) == false)
                    return strCaption;

                // 3.��ǰ�����ַ���ͬ�����ڵ�һ�汾
                strXPath = "caption[(substring(@lang,1,2)='" + strShortLang + "')]";
                nodeCaption = node.SelectSingleNode(strXPath);
                if (nodeCaption != null)
                    strCaption = nodeCaption.InnerText.Trim(); // 2012/2/16
                if (string.IsNullOrEmpty(strCaption) == false)
                    return strCaption;

            }

        END1:
            // 4.�����ڵ�һλ��caption
            strXPath = "caption";
            nodeCaption = node.SelectSingleNode(strXPath);
            if (nodeCaption != null)
                strCaption = nodeCaption.InnerText.Trim(); // 2012/2/16
            if (string.IsNullOrEmpty(strCaption) == false)
                return strCaption;


            // 5.��󷵻�@id
            string strID = "";
            if (node != null)
                strID = DomUtil.GetAttr(node, "id");
            if (strID == "")
                throw new Exception("��������id������Ϊnull��");

            return "@" + strID;

        }


        public int CompareTo(object myObject)
        {
            TableInfo tableInfo = (TableInfo)myObject;

            int nRet = 0;

            int nThisID = 0;
            bool bError = false;
            try
            {
                nThisID = Convert.ToInt32(this.ID);
            }
            catch
            {
                bError = true;
            }

            int nObjectID = 0;
            try
            {
                nObjectID = Convert.ToInt32(tableInfo.ID);
            }
            catch 
            {
                bError = true;
            }

            if (bError == false)
            {
                nRet = nThisID - nObjectID;
            }
            else
            {
                nRet = String.Compare(this.ID, tableInfo.ID);
            }

            if (nRet != 0)
                return nRet;

            if (this.m_bQuery != tableInfo.m_bQuery)
            {
                if (this.m_bQuery == true)
                    return -1;
                else
                    return 1;
            }

            // ??? �����Զ�ǲ��ȵ�
            return this.OriginPosition - tableInfo.OriginPosition;
        }
    }
}
