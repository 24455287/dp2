using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;

namespace DigitalPlatform.Library
{
    /// <summary>
    /// �ִκŹ�����
    /// </summary>
    public class SeedManager
    {
        /// <summary>
        /// 
        /// </summary>
        SearchPanel SearchPanel = null;

        /// <summary>
        /// ���ӿ���
        /// </summary>
        public string SeedDbName = "";

        /// <summary>
        /// ������URL
        /// </summary>
        public string ServerUrl = "";

        /// <summary>
        /// ʱ���
        /// </summary>
        public byte[] Timestamp = null;

        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="searchpanel"></param>
        /// <param name="strServerUrl"></param>
        /// <param name="strSeedDbName"></param>
        public void Initial(
                SearchPanel searchpanel,
                string strServerUrl,
                string strSeedDbName)
        {
            this.SearchPanel = searchpanel;

            /*
            this.SearchPanel.InitialStopManager(this.button_stop,
                this.label_message);
             */

            this.ServerUrl = strServerUrl;
            this.SeedDbName = strSeedDbName;
        }

        /// <summary>
        /// ���������Ӽ�¼·��
        /// </summary>
        /// <param name="strName">������</param>
        /// <param name="strPath">���ؼ�¼·��</param>
        /// <param name="strError">���صĳ�����Ϣ</param>
        /// <returns>-1����;0û���ҵ�;1�ҵ�</returns>
        int SearchRecPath(
            string strName,
            out string strPath,
            out string strError)
        {
            strError = "";
            strPath = "";

            if (this.ServerUrl == "")
            {
                strError = "��δָ��������URL";
                return -1;
            }

            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(this.SeedDbName + ":" + "��")       // 2007/9/14
                + "'><item><word>"
                + StringUtil.GetXmlStringSimple(strName)
                + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            this.SearchPanel.BeginLoop("������Կ� '" + this.SeedDbName + "' ���� '" + strName + "'");

            // ����һ�����н��
            // return:
            //		-1	һ�����
            //		0	not found
            //		1	found
            //		>1	���ж���һ��
            int nRet = this.SearchPanel.SearchOnePath(
                this.ServerUrl,
                strQueryXml,
                out strPath,
                out strError);

            this.SearchPanel.EndLoop();

            if (nRet == -1)
            {
                strError = "������ " + this.SeedDbName + " ʱ����: " + strError;
                return -1;
            }
            if (nRet == 0)
            {
                return 0;	// û���ҵ�
            }

            if (nRet > 1)
            {
                strError = "������ '" + strName + "' ������ " + this.SeedDbName + " ʱ���� " + Convert.ToString(nRet) + " �����޷�ȡ���ʵ���ֵ�����޸Ŀ� '" + this.SeedDbName + "' ����Ӧ��¼��ȷ��ͬһ����ֻ��һ����Ӧ�ļ�¼��";
                return -1;
            }

            return 1;
        }

        /// <summary>
        /// ��������ֵ
        /// </summary>
        /// <param name="strName"></param>
        /// <param name="strValue"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public int SetSeed(
    string strName,
    string strValue,
    out string strError)
        {
            strError = "";

            string strPath = "";
            int nRet = this.SearchRecPath(
                strName,
                out strPath,
                out strError);
            if (nRet == -1)
                return -1;

            if (nRet == 0)
            {
                // �´�����¼
                strPath = this.SeedDbName + "/?";

                // �����Ժ��Ǽ���
            }
            else
            {
                // ���Ǽ�¼
            }

            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);

            xw.WriteStartDocument();

            xw.WriteStartElement("r");
            xw.WriteAttributeString("n", strName);
            xw.WriteAttributeString("v", strValue);
            xw.WriteEndElement();

            xw.WriteEndDocument();
            xw.Close();

            string strXml = sw.ToString();


            byte[] baOutputTimestamp = null;

            REDO:
            // return:
            //		-2	ʱ�����ƥ��
            //		-1	һ�����
            //		0	����
            nRet = this.SearchPanel.SaveRecord(
                this.ServerUrl,
                strPath,
                strXml,
                this.Timestamp,
                false,
                out baOutputTimestamp,
                out strError);
            if (nRet == -1)
                return -1;

            if (nRet == -2)
            {
                    this.Timestamp = baOutputTimestamp;
                    goto REDO;
            }

            this.Timestamp = baOutputTimestamp;

            return 0;
        }

        /// <summary>
        /// ��������ֵ
        /// </summary>
        /// <param name="strName"></param>
        /// <param name="strDefaultValue"></param>
        /// <param name="strValue"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public int IncSeed(
            string strName,
            string strDefaultValue,
            out string strValue,
            out string strError)
        {
            strError = "";
            strValue = "";

            string strPath = "";
            int nRet = this.SearchRecPath(
                strName,
                out strPath,
                out strError);
            if (nRet == -1)
                return -1;

            string strXml = "";
            bool bNewRecord = false;

            if (nRet == 0)
            {
                // �´�����¼

                strPath = this.SeedDbName + "/?";

                StringWriter sw = new StringWriter();
                XmlTextWriter xw = new XmlTextWriter(sw);

                xw.WriteStartDocument();

                xw.WriteStartElement("r");
                xw.WriteAttributeString("n", strName);
                xw.WriteAttributeString("v", strDefaultValue);
                xw.WriteEndElement();

                xw.WriteEndDocument();
                xw.Close();

                strXml = sw.ToString();

                bNewRecord = true;
            }
            else
            {
                string strPartXml = "/xpath/<locate>@v</locate><action>AddInteger+</action>";   // +AddIntegerΪ����ֵ������ٷ��ؼ��˵�ֵ; AddIntegerΪȡֵ���ټ�ֵ���.
                strPath += strPartXml;
                strXml = "1";

                bNewRecord = false;
            }


            byte[] baOutputTimestamp = null;


            // return:
            //		-2	ʱ�����ƥ��
            //		-1	һ�����
            //		0	����
            nRet = this.SearchPanel.SaveRecord(
                this.ServerUrl,
                strPath,
                strXml,
                this.Timestamp,
                true,
                out baOutputTimestamp,
                out strError);
            if (nRet < 0)
            {
                return -1;
            }

            this.Timestamp = baOutputTimestamp;

            if (bNewRecord == true)
            {
                strValue = strDefaultValue;
            }
            else
            {
                strValue = strError;
            }


            return 0;
        }

        /// <summary>
        /// �������ֵ
        /// </summary>
        /// <param name="strName"></param>
        /// <param name="strValue"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public int GetSeed(
            string strName,
            out string strValue,
            out string strError)
        {
            strValue = "";
            strError = "";

            string strPath = "";
            int nRet = this.SearchRecPath(
                strName,
                out strPath,
                out strError);
            if (nRet == -1)
                return -1;
            if (nRet == 0)
                return 0;

            XmlDocument tempdom = null;
            byte[] baTimeStamp = null;
            // ��ȡ��¼
            // return:
            //		-1	error
            //		0	not found
            //		1	found
            nRet = this.SearchPanel.GetRecord(
                this.ServerUrl,
                strPath,
                out tempdom,
                out baTimeStamp,
                out strError);
            if (nRet != 1)
                return -1;

            this.Timestamp = baTimeStamp;

            strValue = DomUtil.GetAttr(tempdom.DocumentElement, "v");
            return 1;
        }

        /*
        //
        public int SetSeed(ChannelCollection Channels,
            string strServerUrl,
            string strSeedDbName,
            string strName,
            string strValue,
            out string strError)
        {
            strError = "";

            return 0;
        }

        public int GetSeed(ChannelCollection Channels,
            string strServerUrl,
            string strSeedDbName,
            string strName,
            out string strValue,
            out string strError)
        {
            strError = "";
            strValue = "";

            return 1;
        }

        public int IncSeed(ChannelCollection Channels,
            string strServerUrl,
            string strSeedDbName,
            string strName,
            out string strValue,
            out string strError)
        {
            strError = "";
            strValue = "";


            return 1;
        }
         * */

    }
}
