using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;

using DigitalPlatform.Text;
using DigitalPlatform.Xml;
using DigitalPlatform.rms.Client;

// using DigitalPlatform.rms.Client.rmsws_localhost;   // Record

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// �������Ǻ� ʵ�ÿ� ������صĴ���
    /// </summary>
    public partial class LibraryApplication
    {
        // ����ʵ�ÿ���Ϣ
        //      strRootElementName  ��Ԫ���������Ϊ�գ�ϵͳ�Ի���<r>��Ϊ��Ԫ��
        //      strKeyAttrName  key�����������Ϊ�գ�ϵͳ�Զ�����k
        //      strValueAttrName    value�����������Ϊ�գ�ϵͳ�Զ�����v

        public LibraryServerResult SetUtilInfo(
            SessionInfo sessioninfo,
            string strAction,
            string strDbName,
            string strFrom,
            string strRootElementName,
            string strKeyAttrName,
            string strValueAttrName,
            string strKey,
            string strValue)
        {
            string strError = "";
            int nRet = 0;

            LibraryServerResult result = new LibraryServerResult();

            string strPath = "";
            string strXml = "";
            byte[] timestamp = null;

            bool bRedo = false;

            if (String.IsNullOrEmpty(strRootElementName) == true)
                strRootElementName = "r";   // ��򵥵�ȱʡģʽ

            if (String.IsNullOrEmpty(strKeyAttrName) == true)
                strKeyAttrName = "k";

            if (String.IsNullOrEmpty(strValueAttrName) == true)
                strValueAttrName = "v";

                // ����ʵ�ÿ��¼��·���ͼ�¼��
                // return:
                //      -1  error(ע���������ж���������������󷵻�)
                //      0   not found
                //      1   found
                nRet = SearchUtilPathAndRecord(
                    sessioninfo.Channels,
                    strDbName,
                    strKey,
                    strFrom,
                    out strPath,
                    out strXml,
                    out timestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // �������Ϊֱ������������¼
                if (strAction == "setrecord")
                {
                    if (nRet == 0)
                    {
                        strPath = strDbName + "/?";
                    }

                    strXml = strValue;
                }
                else
                {
                    // ����������Ϣ�������¼
                    if (nRet == 0)
                    {
                        strPath = strDbName + "/?";

                        // strXml = "<" + strRootElementName + " " + strKeyAttrName + "='" + strKey + "' " + strValueAttrName + "='" + strValue + "'/>";

                        // 2011/12/11
                        XmlDocument dom = new XmlDocument();
                        dom.LoadXml("<" + strRootElementName + "/>");
                        DomUtil.SetAttr(dom.DocumentElement, strKeyAttrName, strKey);
                        DomUtil.SetAttr(dom.DocumentElement, strValueAttrName, strValue);
                        strXml = dom.DocumentElement.OuterXml;
                    }
                    else
                    {
                        string strPartXml = "/xpath/<locate>@" + strValueAttrName + "</locate><create>@" + strValueAttrName + "</create>";
                        strPath += strPartXml;
                        strXml = strValue;
                    }
                }


            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            byte[] baOutputTimeStamp = null;
            string strOutputPath = "";
            int nRedoCount = 0;
        REDO:
            long lRet = channel.DoSaveTextRes(strPath,
                strXml,
                false,	// bInlucdePreamble
                "ignorechecktimestamp",	// style
                timestamp,
                out baOutputTimeStamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                if (bRedo == true)
                {
                    if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch
                        && nRedoCount < 10)
                    {
                        timestamp = baOutputTimeStamp;
                        nRedoCount++;
                        goto REDO;
                    }
                }

                goto ERROR1;
            }


            result.Value = 1;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }


        // ���ʵ�ÿ���Ϣ
        public LibraryServerResult GetUtilInfo(
            SessionInfo sessioninfo,
            string strAction,
            string strDbName,
            string strFrom,
            string strKey,
            string strValueAttrName,
            out string strValue)
        {
            string strError = "";
            strValue = "";
            int nRet = 0;

            LibraryServerResult result = new LibraryServerResult();

            /*
            if (String.IsNullOrEmpty(strKeyAttrName) == true)
                strKeyAttrName = "k";
             * */

            if (String.IsNullOrEmpty(strValueAttrName) == true)
                strValueAttrName = "v";


            /*
            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }*/

            string strPath = "";
            string strXml = "";
            byte[] timestamp = null;

            // ����ʵ�ÿ��¼��·���ͼ�¼��
            // return:
            //      -1  error(ע���������ж���������������󷵻�)
            //      0   not found
            //      1   found
            nRet = SearchUtilPathAndRecord(
                sessioninfo.Channels,
                strDbName,
                strKey,
                strFrom,
                out strPath,
                out strXml,
                out timestamp,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                result.ErrorCode = ErrorCode.NotFound;
                result.ErrorInfo = "����Ϊ '"+strDbName+"' ;��Ϊ '"+strFrom+"' ��ֵΪ '" + strKey + "' �ļ�¼û���ҵ�";
                result.Value = 0;
                return result;
            }

            // �������Ϊ���������¼
            if (strAction == "getrecord")
            {
                strValue = strXml;

                result.Value = 1;
                return result;
            }

            XmlDocument domRecord = new XmlDocument();
            try
            {
                domRecord.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "װ��·��Ϊ'" + strPath + "'��xml��¼ʱ����: " + ex.Message;
                goto ERROR1;
            }

            strValue = DomUtil.GetAttr(domRecord.DocumentElement, strValueAttrName);

            result.Value = 1;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorCode = ErrorCode.SystemError;
            result.ErrorInfo = strError;
            return result;
        }


        // ����ʵ�ÿ��¼��·���ͼ�¼��
        // return:
        //      -1  error(ע���������ж���������������󷵻�)
        //      0   not found
        //      1   found
        public int SearchUtilPathAndRecord(
            RmsChannelCollection Channels,
            string strDbName,
            string strKey,
            string strFrom,
            out string strPath,
            out string strXml,
            out byte[] timestamp,
            out string strError)
        {
            strError = "";
            strPath = "";
            strXml = "";
            timestamp = null;

            if (String.IsNullOrEmpty(strDbName) == true)
            {
                strError = "��δָ������";
                return -1;
            }

            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(strDbName + ":" + strFrom)       // 2007/9/14
                + "'><item><word>"
                + StringUtil.GetXmlStringSimple(strKey)
                + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            List<string> aPath = null;
            // ���ͨ�ü�¼
            // �������ɻ�ó���1�����ϵ�·��
            // return:
            //      -1  error
            //      0   not found
            //      1   ����1��
            //      >1  ���ж���1��
            int nRet = GetRecXml(
                Channels,
                strQueryXml,
                out strXml,
                2,
                out aPath,
                out timestamp,
                out strError);
            if (nRet == -1)
            {
                strError = "������ " + strDbName + " ʱ����: " + strError;
                return -1;
            }
            if (nRet == 0)
            {
                return 0;	// û���ҵ�
            }

            /*
            if (nRet > 1)
            {
                strError = "�Լ����� '" + strKey + "' ������ " + strDbName + " ʱ���� " + Convert.ToString(nRet) + " �������ڲ�������������޸Ŀ� '" + strDbName + "' ����Ӧ��¼��ȷ��ͬһ��ֵֻ��һ����Ӧ�ļ�¼��";
                return -1;
            }
             * */

            Debug.Assert(aPath.Count >= 1, "");
            strPath = aPath[0];

            return 1;
        }
    }

}