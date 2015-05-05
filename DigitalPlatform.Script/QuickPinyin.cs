using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using DigitalPlatform.Xml;
using System.Collections;

namespace DigitalPlatform.Script
{
    /// <summary>
    /// ���ٲ���ƴ��
    /// </summary>
    public class QuickPinyin
    {
        XmlDocument _dom = null;

        public QuickPinyin(string strFileName)
        {
            _dom = new XmlDocument();
            _dom.Load(strFileName);

            LoadToCache();
        }

        Hashtable _cacheTable = null;

        // 2014/10/21
        // ������ȫ��װ�� Hashtable
        public void LoadToCache()
        {
            this._cacheTable = new Hashtable();
            XmlNodeList nodes = _dom.DocumentElement.SelectNodes("p");
            foreach (XmlElement node in nodes)
            {
                string strHanzi = node.GetAttribute("h");
                string strPinyins = node.GetAttribute("p");
                _cacheTable[strHanzi] = strPinyins;
            }

            this._dom = null;    // �ͷ� XmlDocument ��ռ�ռ�
        }

        // ���ƴ��
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        public int GetPinyin(string strHanzi,
            out string strPinyins,
            out string strError)
        {
            strPinyins = "";
            strError = "";

            if (this._cacheTable != null)
            {
                strPinyins = (string)this._cacheTable[strHanzi];
                if (string.IsNullOrEmpty(strPinyins) == true)
                    return 0;
                return 1;
            }
            else
            {
                if (_dom == null)
                {
                    strError = "��δװ��ƴ���ļ�����";
                    return -1;
                }

                XmlNode node = _dom.DocumentElement.SelectSingleNode("p[@h='" + strHanzi + "']");
                if (node == null)
                    return 0;
                strPinyins = DomUtil.GetAttr(node, "p");
                return 1;
            }
        }
    }
}

