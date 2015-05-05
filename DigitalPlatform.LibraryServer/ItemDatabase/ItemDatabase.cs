using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

using DigitalPlatform;	// Stop��
using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Script;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Marc;
using DigitalPlatform.Range;

using DigitalPlatform.Message;
using DigitalPlatform.rms.Client.rmsws_localhost;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// �ᡢ�ڡ���������ע��Ĺ��û����ࡣ�������ص㣬���Ƕ���Ҫһ����ɾ�ĵ�API
    /// </summary>
    public class ItemDatabase
    {
        public LibraryApplication App = null;

        public ItemDatabase()
        {
        }

        // ���캯��
        public ItemDatabase(LibraryApplication app)
        {
            this.App = app;
        }

        // (�������������)
        // ������ʾ���������ơ����硰�ᡱ ���ڡ� ���ɹ���
        public virtual string ItemName
        {
            get
            {
                throw new Exception("ItemName ��δʵ��");
            }
        }

        // �����ڲ����ơ����� ��Item�� ��Issue�� ��Order��
        public virtual string ItemNameInternal
        {
            get
            {
                throw new Exception("ItemNameInternal ��δʵ��");
            }
        }

        // (�������������)
        // ����ʱ��ȱʡ��������ơ����硰entities�� ��issues�� ��orders��
        public virtual string DefaultResultsetName
        {
            get
            {
                throw new Exception("DefaultResultsetName ��δʵ��");
            }
        }

        // (�������������)
        // ׼��д����־��SetXXX�����ַ��������硰SetEntity�� ��SetIssue��
        public virtual string OperLogSetName
        {
            get
            {
                throw new Exception("OperLogSetName δʵ��");
            }
        }

        public virtual string SetApiName
        {
            get
            {
                throw new Exception("SetApiName δʵ��");
            }
        }

        public virtual string GetApiName
        {
            get
            {
                throw new Exception("GetApiName δʵ��");
            }
        }

        // (�������������)
        // ����������ݿ���
        // return:
        //      -1  error
        //      0   û���ҵ�(��Ŀ��)
        //      1   found
        public virtual int GetItemDbName(string strBiblioDbName,
            out string strItemDbName,
            out string strError)
        {
            strItemDbName = "";
            strError = "GetItemDbName() ��δʵ��";
            return -1;
        }

        // 2008/12/8 new add
        // ������ݿ����ǵ�ǰ��ɫô? (ע�����ض����ĸ���Ŀ���µĳ�Ա��)
        public virtual bool IsItemDbName(string strItemDbName)
        {
            throw new Exception("��δʵ�� IsItemDbName");
        }

        // 2008/12/8 new add
        // ͨ���������ݿ����ҵ���Ŀ����
        // return:
        //      -1  error
        //      0   û���ҵ�(�����)
        //      1   found
        public virtual int GetBiblioDbName(string strItemDbName,
            out string strBiblioDbName,
            out string strError)
        {
            strBiblioDbName = "";
            strError = "GetBiblioDbName() ��δʵ��";
            return -1;
        }

        // (�������������)
        // �۲��Ѵ��ڵļ�¼�У�Ψһ���ֶ��Ƿ��Ҫ���һ��
        // return:
        //      -1  ����
        //      0   һ��
        //      1   ��һ�¡�������Ϣ��strError��
        public virtual int IsLocateInfoCorrect(
            List<string> locateParams,
            XmlDocument domExist,
            out string strError)
        {
#if NO
            strError = "IsLocateInfoCorrect() ��δʵ��";
            return -1;
#endif
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 1)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ1��";
                return -1;
            }
            string strRefID = locateParams[0];

            if (String.IsNullOrEmpty(strRefID) == false)
            {
                string strExistingRefID = DomUtil.GetElementText(domExist.DocumentElement,
                    "refID");
                if (strExistingRefID != strRefID)
                {
                    strError = this.ItemName + "��¼��<refID>Ԫ���еĲο�ID '" + strExistingRefID + "' ��ͨ��ɾ����������ָ���Ĳο�ID '" + strRefID + "' ��һ�¡�";
                    return 1;
                }
            }

            return 0;
        }

        // �������������
        // �������ڻ�ȡ�����¼��XML����ʽ
        public virtual int MakeGetItemRecXmlSearchQuery(
            List<string> locateParams,
            int nMax,
            out string strQueryXml,
            out string strError)
        {
            strQueryXml = "";
            strError = "MakeGetItemRecXmlSearchQuery() ��δʵ��";
            return -1;
        }

        // �������������
        // ���춨λ��ʾ��Ϣ�����ڱ���
        public virtual int GetLocateText(
            List<string> locateParams,
            out string strText,
            out string strError)
        {
#if NO
            strText = "";
            strError = "MakeGetItemRecXmlSearchQuery() ��δʵ��";
            return 0;
#endif
            strText = "";
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 1)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ1��";
                return -1;
            }
            string strRefID = locateParams[0];

            strText = "�ο�IDΪ '" + strRefID + "'";
            return 0;
        }

        // (�������������)
        // ��λ����ֵ�Ƿ�Ϊ��?
        // return:
        //      -1  ����
        //      0   ��Ϊ��
        //      1   Ϊ��(��ʱ��Ҫ��strError�и�������˵������)
        public virtual int IsLocateParamNullOrEmpty(
            List<string> locateParams,
            out string strError)
        {
#if NO
            strError = "IsLocateParamNullOrEmpty() ��δʵ��";
            return 0;
#endif
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 1)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ1��";
                return -1;
            }
            string strRefID = locateParams[0];


            if (String.IsNullOrEmpty(strRefID) == true)
            {
                strError = "�ο�ID Ϊ��";
                return 1;
            }

            return 0;
        }

        // ���¾������¼�а����Ķ�λ��Ϣ���бȽ�, �����Ƿ����˱仯(��������Ҫ����)
        // parameters:
        //      oldLocateParam   ˳�㷵�ؾɼ�¼�еĶ�λ����
        //      newLocateParam   ˳�㷵���¼�¼�еĶ�λ����
        // return:
        //      -1  ����
        //      0   ���
        //      1   �����
        public virtual int CompareTwoItemLocateInfo(
            string strItemDbName,
            XmlDocument domOldRec,
            XmlDocument domNewRec,
            out List<string> oldLocateParam,
            out List<string> newLocateParam,
            out string strError)
        {
#if NO
            oldLocateParam = null;
            newLocateParam = null;

            strError = "CompareTwoItemLocateInfo() ��δʵ��";
            return -1;
#endif
            strError = "";

            // 2012/4/1 ����
            string strOldRefID = DomUtil.GetElementText(domOldRec.DocumentElement,
                "refID");

            string strNewRefID = DomUtil.GetElementText(domNewRec.DocumentElement,
                "refID");

            oldLocateParam = new List<string>();
            oldLocateParam.Add(strOldRefID);

            newLocateParam = new List<string>();
            newLocateParam.Add(strNewRefID);

            if (strOldRefID != strNewRefID)
                return 1;   // �����

            return 0;   // ��ȡ�
        }

        public virtual void LockItem(List<string> locateParam)
        {
            string strRefID = locateParam[0];

            this.App.EntityLocks.LockForWrite(
                this.ItemNameInternal + ":" + strRefID);
        }

        public virtual void UnlockItem(List<string> locateParam)
        {
            string strRefID = locateParam[0];

            this.App.EntityLocks.UnlockForWrite(
                this.ItemNameInternal + ":" + strRefID);
        }

        // (�������������)
        // �۲��Ѿ����ڵļ�¼�Ƿ�����ͨ��Ϣ
        // return:
        //      -1  ����
        //      0   û��
        //      1   �С�������Ϣ��strError��
        public virtual int HasCirculationInfo(XmlDocument domExist,
            out string strError)
        {
            strError = "";
            return 0;
        }

        // (�������������)
        // ��¼�Ƿ�����ɾ��?
        // return:
        //      -1  ����������ɾ����
        //      0   ������ɾ������ΪȨ�޲�����ԭ��ԭ����strError��
        //      1   ����ɾ��
        public virtual int CanDelete(
            SessionInfo sessioninfo,
            XmlDocument domExist,
            out string strError)
        {
            strError = "";
            return 1;
        }


        // (�������������)
        // �Ƚ�������¼, ����������Ҫ����Ϣ�йص��ֶ��Ƿ����˱仯
        // return:
        //      0   û�б仯
        //      1   �б仯
        public virtual int IsItemInfoChanged(XmlDocument domExist,
            XmlDocument domOldRec)
        {
            throw new Exception("IsItemInfoChanged() ��δʵ��");    // 2009/1/9 changed
        }


        // (�������������)
        // DoOperChange()��DoOperMove()���¼�����
        // �ϲ��¾ɼ�¼
        // parameters:
        // return:
        //      -1  ����
        //      0   ��ȷ
        //      1   �в����޸�û�ж��֡�˵����strError��
        public virtual int MergeTwoItemXml(
            SessionInfo sessioninfo,
            XmlDocument domExist,
            XmlDocument domNew,
            out string strMergedXml,
            out string strError)
        {
            strMergedXml = "";
            strError = "MergeTwoItemXml() ��δʵ��";

            return -1;
        }

        // �Ƿ��������¼�¼?
        // parameters:
        // return:
        //      -1  �����������޸ġ�
        //      0   ������������ΪȨ�޲�����ԭ��ԭ����strError��
        //      1   ���Դ���
        public virtual int CanCreate(
            SessionInfo sessioninfo,
            XmlDocument domNew,
            out string strError)
        {
            strError = "";

            return 1;
        }

        // (�������������)
        // DoOperChange()��DoOperMove()���¼�����
        // �Ƿ�����Ծɼ�¼�����޸�?
        // parameters:
        //      strAction   change/move
        // return:
        //      -1  �����������޸ġ�
        //      0   �������޸ģ���ΪȨ�޲�����ԭ��ԭ����strError��
        //      1   �����޸�
        public virtual int CanChange(
            SessionInfo sessioninfo,
            string strAction,
            XmlDocument domExist,
            XmlDocument domNew,
            out string strError)
        {
            strError = "";

            return 1;
        }

        // ������ʺϱ�����������¼
        // parameters:
        //      bForce  �Ƿ�Ϊǿ�Ʊ���?
        public virtual int BuildNewItemRecord(
            SessionInfo sessioninfo,
            bool bForce,
            string strBiblioRecId,
            string strOriginXml,
            out string strXml,
            out string strError)
        {
            strXml = "";
            strError = "BuildNewItemRecord() ��δʵ��";

            return -1;
        }

        // ��������¼
        // �������ɻ�ó���1�����ϵ�·��
        // parameters:
        //      timestamp   �������еĵ�һ����timestamp
        //      strStyle    ������� withresmetadata ,��ʾҪ��XML��¼�з���<dprms:file>Ԫ���ڵ� __xxx ����
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetItemRecXml(
            RmsChannelCollection channels,
            List<string> locateParams,
            string strStyle,
            out string strXml,
            int nMax,
            out List<string> aPath,
            out byte[] timestamp,
            out string strError)
        {
            aPath = null;

            strXml = "";
            strError = "";
            timestamp = null;


            // �������ʽ

            /*
            string strQueryXml = "<target list='"
                    + StringUtil.GetXmlStringSimple(strIssueDbName + ":" + "����ʱ��")
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strPublishTime)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            strQueryXml += "<operator value='AND'/>";


            strQueryXml += "<target list='"
                    + StringUtil.GetXmlStringSimple(strIssueDbName + ":" + "����¼")
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strParentID)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            strQueryXml = "<group>" + strQueryXml + "</group>";
             * */
            // �������ڻ�ȡ�����¼��XML����ʽ
            string strQueryXml = "";
            int nRet = MakeGetItemRecXmlSearchQuery(
                locateParams,
                nMax,
                out strQueryXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;


            RmsChannel channel = channels.GetChannel(this.App.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            long lRet = channel.DoSearch(strQueryXml,
                "default",
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                string strText = "";
                // ���춨λ��ʾ��Ϣ�����ڱ���
                nRet = GetLocateText(
                    locateParams,
                    out strText,
                    out strError);
                if (nRet == -1)
                {
                    strError = "��λ��Ϣû���ҵ�������GetLocateText()��������: " + strError;
                    return 0;
                }


                strError = strText + " ������û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            // List<string> aPath = null;
            lRet = channel.DoGetSearchResult(
                "default",
                0,
                Math.Min(nMax, lHitCount),
                "zh",
                null,
                out aPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            Debug.Assert(aPath != null, "");

            if (aPath.Count == 0)
            {
                strError = "DoGetSearchResult aPath error";
                goto ERROR1;
            }

            string strMetaData = "";
            string strOutputPath = "";
            string strGetStyle = "content,data,metadata,timestamp,outputpath";

            if (StringUtil.IsInList("withresmetadata", strStyle) == true)
                strGetStyle += ",withresmetadata";

            lRet = channel.GetRes(aPath[0],
                strGetStyle,
                out strXml,
                out strMetaData,
                out timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

        // ���� ��λ��Ϣ ���������в���
        // ������ֻ�������, ������ü�¼��
        // return:
        //      -1  error
        //      ����    ���м�¼����(������nMax�涨�ļ���)
        public int SearchItemRecDup(
            // RmsChannelCollection channels,
            RmsChannel channel,
            List<string> locateParams,
            /*
            string strIssueDbName,
            string strParentID,
            string strPublishTime,
             * */
            int nMax,
            out List<string> aPath,
            out string strError)
        {
            strError = "";
            aPath = null;

            // �������ʽ
            string strQueryXml = "";
            int nRet = MakeGetItemRecXmlSearchQuery(
                locateParams,
                100,
                out strQueryXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            /*
            RmsChannel channel = channels.GetChannel(this.App.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }
             * */
            Debug.Assert(channel != null, "");

            long lRet = channel.DoSearch(strQueryXml,
                "default",
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                string strText = "";
                // ���춨λ��ʾ��Ϣ�����ڱ���
                nRet = GetLocateText(
                    locateParams,
                    out strText,
                    out strError);
                if (nRet == -1)
                {
                    strError = "��λ��Ϣû���ҵ�������GetLocateText()��������: " + strError;
                    return 0;
                }


                strError = strText + " ������û���ҵ�";
                return 0;
            }

            long lHitCount = lRet;

            lRet = channel.DoGetSearchResult(
                "default",
                0,
                nMax,
                "zh",
                null,
                out aPath,
                out strError);
            if (lRet == -1)
                goto ERROR1;

            if (aPath.Count == 0)
            {
                strError = "DoGetSearchResult aPath error ��ǰ���Ѿ����е�����ì��";
                goto ERROR1;
            }

            return (int)lHitCount;
        ERROR1:
            return -1;
        }

        // ɾ�������¼�Ĳ���
        int DoOperDelete(
            SessionInfo sessioninfo,
            bool bForce,
            RmsChannel channel,
            EntityInfo info,
            List<string> oldLocateParams,
            /*
            string strIssueDbName,
            string strParentID,
            string strOldPublishTime,
             * */
            XmlDocument domOldRec,
            ref XmlDocument domOperLog,
            ref List<EntityInfo> ErrorInfos)
        {
            int nRedoCount = 0;
            EntityInfo error = null;
            int nRet = 0;
            long lRet = 0;
            string strError = "";

            /*
            // ���newrecpathΪ�յ���oldrecpath��ֵ������oldrecpath��ֵ
            // 2007/10/23 new add
            if (String.IsNullOrEmpty(info.NewRecPath) == true)
            {
                if (String.IsNullOrEmpty(info.OldRecPath) == false)
                    info.NewRecPath = info.OldRecPath;
            }*/

            // 2008/6/24 new add
            if (String.IsNullOrEmpty(info.NewRecPath) == false)
            {
                if (info.NewRecPath != info.OldRecPath)
                {
                    strError = "actionΪdeleteʱ, ���info.NewRecPath���գ��������ݱ����info.OldRecPathһ�¡�(info.NewRecPath='" + info.NewRecPath + "' info.OldRecPath='" + info.OldRecPath + "')";
                    return -1;
                }
            }
            else
            {
                info.NewRecPath = info.OldRecPath;
            }


            string strText = "";
            // ���춨λ��ʾ��Ϣ�����ڱ���
            nRet = GetLocateText(
                oldLocateParams,
                out strText,
                out strError);
            if (nRet == -1)
            {
                strError = "GetLocateText()��������: " + strError;
                goto ERROR1;
            }

            // �����¼·��Ϊ��, ���Ȼ�ü�¼·��
            if (String.IsNullOrEmpty(info.NewRecPath) == true)
            {
                List<string> aPath = null;

                nRet = IsLocateParamNullOrEmpty(
                    oldLocateParams,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 1)
                {
                    strError += "info.OldRecord�е�" + strError + " �� info.RecPath����ֵΪ�գ�ͬʱ���֣����ǲ������";
                    goto ERROR1;
                }

                /*
                RmsChannel channel = sessioninfo.Channels.GetChannel(this.App.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    goto ERROR1;
                }
                 * */

                // ������ֻ�������, ������ü�¼��
                // return:
                //      -1  error
                //      ����    ���м�¼����(������nMax�涨�ļ���)
                nRet = this.SearchItemRecDup(
                    //  sessioninfo.Channels,
                    channel,
                    oldLocateParams,
                    100,
                    out aPath,
                    out strError);
                if (nRet == -1)
                {
                    strError = "ɾ��������������ؽ׶η�������:" + strError;
                    goto ERROR1;
                }


                if (nRet == 0)
                {
                    error = new EntityInfo(info);
                    error.ErrorInfo = strText + " �ļ�¼�Ѳ�����";
                    error.ErrorCode = ErrorCodeValue.NotFound;
                    ErrorInfos.Add(error);
                    return -1;
                }

                if (nRet > 1)
                {
                    /*
                    string[] pathlist = new string[aPath.Count];
                    aPath.CopyTo(pathlist);
                     * */

                    strError = strText + " �Ѿ������ж��������¼ʹ����: " + StringUtil.MakePathList(aPath)/*String.Join(",", pathlist)*/ + "'������һ�����ص�ϵͳ���ϣ��뾡��֪ͨϵͳ����Ա����";
                    goto ERROR1;
                }

                info.NewRecPath = aPath[0];
            }

            Debug.Assert(String.IsNullOrEmpty(info.NewRecPath) == false, "");
            // Debug.Assert(strEntityDbName != "", "");

            byte[] exist_timestamp = null;
            string strOutputPath = "";
            string strMetaData = "";
            string strExistingXml = "";

        REDOLOAD:

            // �ȶ������ݿ��д�λ�õ����м�¼
            lRet = channel.GetRes(info.NewRecPath,
                out strExistingXml,
                out strMetaData,
                out exist_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                if (channel.ErrorCode == ChannelErrorCode.NotFound)
                {
                    error = new EntityInfo(info);
                    error.ErrorInfo = strText + " �������¼ '" + info.NewRecPath + "' �Ѳ�����";
                    error.ErrorCode = channel.OriginErrorCode;
                    ErrorInfos.Add(error);
                    return -1;
                }
                else
                {
                    error = new EntityInfo(info);
                    error.ErrorInfo = "ɾ��������������, �ڶ���ԭ�м�¼ '" + info.NewRecPath + "' �׶�:" + strError;
                    error.ErrorCode = channel.OriginErrorCode;
                    ErrorInfos.Add(error);
                    return -1;
                }
            }

            // �Ѽ�¼װ��DOM
            XmlDocument domExist = new XmlDocument();

            try
            {
                domExist.LoadXml(strExistingXml);
            }
            catch (Exception ex)
            {
                strError = "strExistXmlװ�ؽ���DOMʱ��������: " + ex.Message;
                goto ERROR1;
            }

            // �۲��Ѵ��ڵļ�¼�У�Ψһ���ֶ��Ƿ��Ҫ���һ��
        // return:
        //      -1  ����
        //      0   һ��
        //      1   ��һ�¡�������Ϣ��strError��
            nRet = IsLocateInfoCorrect(
                oldLocateParams,
                domExist,
                out strError);
            if (nRet != 0)
                goto ERROR1;

            if (bForce == false)
            {
                // �۲��Ѿ����ڵļ�¼�Ƿ�����ͨ��Ϣ
                // return:
                //      -1  ����
                //      0   û��
                //      1   �С�������Ϣ��strError��
                nRet = HasCirculationInfo(domExist,
                    out strError);
                if (nRet != 0)
                    goto ERROR1;
            }

            if (bForce == false)
            {
                // ��¼�Ƿ�����ɾ��?
                // return:
                //      -1  ����������ɾ����
                //      0   ������ɾ������ΪȨ�޲�����ԭ��ԭ����strError��
                //      1   ����ɾ��
                nRet = CanDelete(
                    sessioninfo,
                    domExist,
                    out strError);
                if (nRet != 1)
                    goto ERROR1;
            }


            // �Ƚ�ʱ���
            // �۲�ʱ����Ƿ����仯
            nRet = ByteArray.Compare(info.OldTimestamp, exist_timestamp);
            if (nRet != 0)
            {
                // 2008/10/19 new add
                if (bForce == true)
                {
                    error = new EntityInfo(info);
                    error.NewTimestamp = exist_timestamp;   // ��ǰ��֪�����м�¼ʵ���Ϸ������仯
                    error.ErrorInfo = "���ݿ��м���ɾ���Ĳ��¼�Ѿ������˱仯��������װ�ء���ϸ�˶Ժ�����ɾ����";
                    error.ErrorCode = ErrorCodeValue.TimestampMismatch;
                    ErrorInfos.Add(error);
                    return -1;
                }

                // ���ǰ�˸����˾ɼ�¼�����кͿ��м�¼���бȽϵĻ���
                if (String.IsNullOrEmpty(info.OldRecord) == false)
                {
                    // �Ƚ�������¼, ����������Ҫ����Ϣ�йص��ֶ��Ƿ����˱仯
                    // return:
                    //      0   û�б仯
                    //      1   �б仯
                    nRet = IsItemInfoChanged(domExist,
                        domOldRec);
                    if (nRet == 1)
                    {

                        error = new EntityInfo(info);
                        error.NewTimestamp = exist_timestamp;   // ��ǰ��֪�����м�¼ʵ���Ϸ������仯
                        error.ErrorInfo = "���ݿ��м���ɾ����" + this.ItemName + "��¼�Ѿ������˱仯��������װ�ء���ϸ�˶Ժ�����ɾ����";
                        error.ErrorCode = ErrorCodeValue.TimestampMismatch;
                        ErrorInfos.Add(error);
                        return -1;
                    }
                }

                info.OldTimestamp = exist_timestamp;
                info.NewTimestamp = exist_timestamp;
            }

            byte[] output_timestamp = null;

            lRet = channel.DoDeleteRes(info.NewRecPath,
                info.OldTimestamp,
                out output_timestamp,
                out strError);
            if (lRet == -1)
            {
                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                {
                    if (nRedoCount > 10)
                    {
                        strError = "����ɾ��������ʱ�����ͻ, ����10��������Ȼʧ��";
                        goto ERROR1;
                    }
                    // ����ʱ�����ƥ��
                    // �ظ�������ȡ�Ѵ��ڼ�¼\�ȽϵĹ���
                    nRedoCount++;
                    goto REDOLOAD;
                }

                error = new EntityInfo(info);
                error.NewTimestamp = output_timestamp;
                error.ErrorInfo = "ɾ��������������:" + strError;
                error.ErrorCode = channel.OriginErrorCode;
                ErrorInfos.Add(error);
                return -1;
            }
            else
            {
                // �ɹ�
                DomUtil.SetElementText(domOperLog.DocumentElement, "action", "delete");

                // ������<record>Ԫ��

                // ����<oldRecord>Ԫ��
                XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                    "oldRecord", strExistingXml);
                DomUtil.SetAttr(node, "recPath", info.NewRecPath);


                // ���ɾ���ɹ����򲻱�Ҫ�������з��ر�ʾ�ɹ�����ϢԪ����
            }

            return 0;
        ERROR1:
            error = new EntityInfo(info);
            error.ErrorInfo = strError;
            error.ErrorCode = ErrorCodeValue.CommonError;
            ErrorInfos.Add(error);
            return -1;
        }


        // ִ��API�е�"move"����
        // 1) �����ɹ���, NewRecord����ʵ�ʱ�����¼�¼��NewTimeStampΪ�µ�ʱ���
        // 2) �������TimeStampMismatch����OldRecord���п��з����仯��ġ�ԭ��¼����OldTimeStamp����ʱ���
        // return:
        //      -1  ����
        //      0   �ɹ�
        int DoOperMove(
            SessionInfo sessioninfo,
            // string strUserID,
            RmsChannel channel,
            EntityInfo info,
            ref XmlDocument domOperLog,
            ref List<EntityInfo> ErrorInfos)
        {
            EntityInfo error = null;
            bool bExist = true;    // info.RecPath��ָ�ļ�¼�Ƿ����?

            int nRet = 0;
            long lRet = 0;

            string strError = "";

            // ���·��
            if (info.OldRecPath == info.NewRecPath)
            {
                strError = "��actionΪ\"move\"ʱ��info.NewRecordPath·�� '" + info.NewRecPath + "' ��info.OldRecPath '" + info.OldRecPath + "' ���벻��ͬ";
                goto ERROR1;
            }

            // ��鼴�����ǵ�Ŀ��λ���ǲ����м�¼������У����������move������
            // ���Ҫ���д�����Ŀ��λ�ü�¼���ܵ�move������ǰ�˿�����ִ��һ��delete������Ȼ����ִ��move������
            // �����涨����Ϊ�˱�����ڸ��ӵ��ж��߼���Ҳ����ǰ�˲�������������ĺ����
            // ��Ϊ�������move���и���Ŀ���¼���ܣ��򱻸��ǵļ�¼��Ԥɾ�����������ڽ�����һ������ע���������Ч�ò����ԣ���ǰ�˲�����Ա׼ȷ�ж���̬���Ժ������(���ҿ�������ע����Ҫ����Ĳ���Ȩ��)������
            bool bAppendStyle = false;  // Ŀ��·���Ƿ�Ϊ׷����̬��
            string strTargetRecId = ResPath.GetRecordId(info.NewRecPath);

            if (strTargetRecId == "?" || String.IsNullOrEmpty(strTargetRecId) == true)
                bAppendStyle = true;

            string strOutputPath = "";
            string strMetaData = "";

            if (bAppendStyle == false)
            {
                string strExistTargetXml = "";
                byte[] exist_target_timestamp = null;

                // ��ȡ����Ŀ��λ�õ����м�¼
                lRet = channel.GetRes(info.NewRecPath,
                    out strExistTargetXml,
                    out strMetaData,
                    out exist_target_timestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.NotFound)
                    {
                        // �����¼������, ˵��������ɸ���̬��
                        /*
                        strExistSourceXml = "<root />";
                        exist_source_timestamp = null;
                        strOutputPath = info.NewRecPath;
                         * */
                    }
                    else
                    {
                        error = new EntityInfo(info);
                        error.ErrorInfo = "move������������, �����ڶ��뼴�����ǵ�Ŀ��λ�� '" + info.NewRecPath + "' ԭ�м�¼�׶�:" + strError;
                        error.ErrorCode = channel.OriginErrorCode;
                        ErrorInfos.Add(error);
                        return -1;
                    }
                }
                else
                {
                    // �����¼���ڣ���Ŀǰ�����������Ĳ���
                    strError = "�ƶ�(move)�������ܾ�����Ϊ�ڼ������ǵ�Ŀ��λ�� '" + info.NewRecPath + "' �Ѿ�����" + this.ItemName + "��¼������ɾ��(delete)������¼���ٽ����ƶ�(move)����";
                    goto ERROR1;
                }
            }


            string strExistSourceXml = "";
            byte[] exist_source_timestamp = null;

            // �ȶ������ݿ���Դλ�õ����м�¼
            // REDOLOAD:

            lRet = channel.GetRes(info.OldRecPath,
                out strExistSourceXml,
                out strMetaData,
                out exist_source_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                if (channel.ErrorCode == ChannelErrorCode.NotFound)
                {
                    /*
                    // �����¼������, ����һ���յļ�¼
                    bExist = false;
                    strExistSourceXml = "<root />";
                    exist_source_timestamp = null;
                    strOutputPath = info.NewRecPath;
                     * */
                    // �����������ſ��������صĸ����ã����Բ��÷ſ�
                    strError = "move������Դ��¼ '" + info.OldRecPath + "' �����ݿ��в����ڣ������޷������ƶ�������";
                    goto ERROR1;
                }
                else
                {
                    error = new EntityInfo(info);
                    error.ErrorInfo = "�ƶ�������������, �ڶ������ԭ��Դ��¼(·����info.OldRecPath) '" + info.OldRecPath + "' �׶�:" + strError;
                    error.ErrorCode = channel.OriginErrorCode;
                    ErrorInfos.Add(error);
                    return -1;
                }
            }

            // ��������¼װ��DOM

            XmlDocument domSourceExist = new XmlDocument();
            XmlDocument domNew = new XmlDocument();

            try
            {
                domSourceExist.LoadXml(strExistSourceXml);
            }
            catch (Exception ex)
            {
                strError = "strExistXmlװ�ؽ���DOMʱ��������: " + ex.Message;
                goto ERROR1;
            }

            try
            {
                domNew.LoadXml(info.NewRecord);
            }
            catch (Exception ex)
            {
                strError = "info.NewRecordװ�ؽ���DOMʱ��������: " + ex.Message;
                goto ERROR1;
            }


            // �۲�ʱ����Ƿ����仯
            nRet = ByteArray.Compare(info.OldTimestamp, exist_source_timestamp);
            if (nRet != 0)
            {
                // ʱ����������
                // ��Ҫ��info.OldRecord��strExistXml���бȽϣ������������йص�Ԫ�أ�Ҫ��Ԫ�أ�ֵ�Ƿ����˱仯��
                // �����ЩҪ��Ԫ�ز�δ�����仯���ͼ������кϲ������Ǳ������

                XmlDocument domOld = new XmlDocument();

                try
                {
                    domOld.LoadXml(info.OldRecord);
                }
                catch (Exception ex)
                {
                    strError = "info.OldRecordװ�ؽ���DOMʱ��������: " + ex.Message;
                    goto ERROR1;
                }

                // �Ƚ�������¼, �����������йص�Ҫ���ֶ��Ƿ����˱仯
                // return:
                //      0   û�б仯
                //      1   �б仯
                nRet = IsItemInfoChanged(domOld,
                    domSourceExist);
                if (nRet == 1)
                {
                    error = new EntityInfo(info);
                    // ������Ϣ��, �������޸Ĺ���ԭ��¼����ʱ���
                    error.OldRecord = strExistSourceXml;
                    error.OldTimestamp = exist_source_timestamp;

                    if (bExist == false)
                        error.ErrorInfo = "�ƶ�������������: ���ݿ��е�ԭ��¼ (·��Ϊ'" + info.OldRecPath + "') �ѱ�ɾ����";
                    else
                        error.ErrorInfo = "�ƶ�������������: ���ݿ��е�ԭ��¼ (·��Ϊ'" + info.OldRecPath + "') �ѷ������޸�";
                    error.ErrorCode = ErrorCodeValue.TimestampMismatch;
                    ErrorInfos.Add(error);
                    return -1;
                }

                // exist_source_timestamp��ʱ�Ѿ���ӳ�˿��б��޸ĺ�ļ�¼��ʱ���
            }

            // 2011/2/11
            nRet = CanChange(
sessioninfo,
"move",
domSourceExist,
domNew,
out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                error = new EntityInfo(info);
                error.ErrorInfo = strError;
                error.ErrorCode = ErrorCodeValue.AccessDenied;
                ErrorInfos.Add(error);
                return -1;
            }

            // 2010/4/8
            // 
            nRet = this.App.SetOperation(
                ref domNew,
                "moved",
                sessioninfo.UserID, // strUserID,
                "",
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string strWarning = "";
            // �ϲ��¾ɼ�¼
            // return:
            //      -1  ����
            //      0   ��ȷ
            //      1   �в����޸�û�ж��֡�˵����strError��
            string strNewXml = "";
            nRet = MergeTwoItemXml(
                sessioninfo,
                domSourceExist,
                domNew,
                out strNewXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 1)
                strWarning = strError;


            // �ƶ���¼
            byte[] output_timestamp = null;

            // TODO: Copy��Ҫдһ�Σ���ΪCopy����д���¼�¼��
            // ��ʵCopy���������ڴ�����Դ�����򻹲�����Save+Delete
            lRet = channel.DoCopyRecord(info.OldRecPath,
                info.NewRecPath,
                true,   // bDeleteSourceRecord
                out output_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                strError = "DoCopyRecord() error :" + strError;
                goto ERROR1;
            }

            // Debug.Assert(strOutputPath == info.NewRecPath);
            string strTargetPath = strOutputPath;

            lRet = channel.DoSaveTextRes(strTargetPath,
                strNewXml,
                false,   // include preamble?
                "content",
                output_timestamp,
                out output_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                strError = "�ƶ������У�" + this.ItemName + "��¼ '" + info.OldRecPath + "' �Ѿ����ɹ��ƶ��� '" + strTargetPath + "' ������д��������ʱ��������: " + strError;

                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                {
                    // �����з�������
                    // ��ΪԴ�Ѿ��ƶ�������ܸ���
                }

                // ����д�������־���ɡ�û��Undo
                this.App.WriteErrorLog(strError);

                error = new EntityInfo(info);
                error.ErrorInfo = "�ƶ�������������:" + strError;
                error.ErrorCode = channel.OriginErrorCode;
                ErrorInfos.Add(error);
                return -1;
            }
            else // �ɹ�
            {
                info.NewRecPath = strOutputPath;    // ���ֱ����λ�ã���Ϊ������׷����ʽ��·��

                DomUtil.SetElementText(domOperLog.DocumentElement, "action", "move");

                // �¼�¼
                XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                    "record", strNewXml);
                DomUtil.SetAttr(node, "recPath", info.NewRecPath);

                // �ɼ�¼
                node = DomUtil.SetElementText(domOperLog.DocumentElement,
                    "oldRecord", strExistSourceXml);
                DomUtil.SetAttr(node, "recPath", info.OldRecPath);

                // ����ɹ�����Ҫ������ϢԪ�ء���Ϊ��Ҫ�����µ�ʱ���
                error = new EntityInfo(info);
                error.NewTimestamp = output_timestamp;
                error.NewRecord = strNewXml;

                error.ErrorInfo = "�ƶ������ɹ���NewRecPath�з�����ʵ�ʱ����·��, NewTimeStamp�з������µ�ʱ�����NewRecord�з�����ʵ�ʱ�����¼�¼(���ܺ��ύ��Դ��¼���в���)��";
                if (string.IsNullOrEmpty(strWarning) == false)
                {
                    error.ErrorInfo = "�ƶ������ɹ�����" + strWarning;
                    error.ErrorCode = ErrorCodeValue.PartialDenied;
                }
                else
                    error.ErrorCode = ErrorCodeValue.NoError;
                ErrorInfos.Add(error);
            }

            return 0;

        ERROR1:
            error = new EntityInfo(info);
            error.ErrorInfo = strError;
            error.ErrorCode = ErrorCodeValue.CommonError;
            ErrorInfos.Add(error);
            return -1;
        }


        // ִ��API�е�"change"����
        // 1) �����ɹ���, NewRecord����ʵ�ʱ�����¼�¼��NewTimeStampΪ�µ�ʱ���
        // 2) �������TimeStampMismatch����OldRecord���п��з����仯��ġ�ԭ��¼����OldTimeStamp����ʱ���
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int DoOperChange(
            bool bForce,
            // string strUserID,
            SessionInfo sessioninfo,
            RmsChannel channel,
            EntityInfo info,
            ref XmlDocument domOperLog,
            ref List<EntityInfo> ErrorInfos)
        {
            int nRedoCount = 0;
            EntityInfo error = null;
            bool bExist = true;    // info.RecPath��ָ�ļ�¼�Ƿ����?

            int nRet = 0;
            long lRet = 0;

            string strError = "";

            // ���һ��·��
            if (String.IsNullOrEmpty(info.NewRecPath) == true)
            {
                strError = "info.NewRecPath�е�·������Ϊ��";
                goto ERROR1;
            }

            string strTargetRecId = ResPath.GetRecordId(info.NewRecPath);

            if (strTargetRecId == "?")
            {
                strError = "info.NewRecPath·�� '" + strTargetRecId + "' �м�¼ID���ֲ���Ϊ'?'";
                goto ERROR1;
            }
            if (String.IsNullOrEmpty(strTargetRecId) == true)
            {
                strError = "info.NewRecPath·�� '" + strTargetRecId + "' �м�¼ID���ֲ���Ϊ��";
                goto ERROR1;
            }

            if (info.OldRecPath != info.NewRecPath)
            {
                strError = "��actionΪ\"change\"ʱ��info.NewRecordPath·�� '" + info.NewRecPath + "' ��info.OldRecPath '" + info.OldRecPath + "' ������ͬ";
                goto ERROR1;
            }

            string strExistXml = "";
            byte[] exist_timestamp = null;
            string strOutputPath = "";
            string strMetaData = "";


            // �ȶ������ݿ��м�������λ�õ����м�¼
        REDOLOAD:

            lRet = channel.GetRes(info.NewRecPath,
                out strExistXml,
                out strMetaData,
                out exist_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {
                if (channel.ErrorCode == ChannelErrorCode.NotFound)
                {
                    // �����¼������, ����һ���յļ�¼
                    bExist = false;
                    strExistXml = "<root />";
                    exist_timestamp = null;
                    strOutputPath = info.NewRecPath;
                }
                else
                {
                    error = new EntityInfo(info);
                    error.ErrorInfo = "���������������, �ڶ���ԭ�м�¼�׶�:" + strError;
                    error.ErrorCode = channel.OriginErrorCode;
                    ErrorInfos.Add(error);
                    return -1;
                }
            }


            // ��������¼װ��DOM

            XmlDocument domExist = new XmlDocument();
            XmlDocument domNew = new XmlDocument();

            try
            {
                domExist.LoadXml(strExistXml);
            }
            catch (Exception ex)
            {
                strError = "strExistXmlװ�ؽ���DOMʱ��������: " + ex.Message;
                goto ERROR1;
            }

            try
            {
                domNew.LoadXml(info.NewRecord);
            }
            catch (Exception ex)
            {
                strError = "info.NewRecordװ�ؽ���DOMʱ��������: " + ex.Message;
                goto ERROR1;
            }


            // �۲�ʱ����Ƿ����仯
            nRet = ByteArray.Compare(info.OldTimestamp, exist_timestamp);
            if (nRet != 0)
            {
                // ʱ����������
                // ��Ҫ��info.OldRecord��strExistXml���бȽϣ�������ҵ���йص�Ԫ�أ�Ҫ��Ԫ�أ�ֵ�Ƿ����˱仯��
                // �����ЩҪ��Ԫ�ز�δ�����仯���ͼ������кϲ������Ǳ������

                XmlDocument domOld = new XmlDocument();

                try
                {
                    domOld.LoadXml(info.OldRecord);
                }
                catch (Exception ex)
                {
                    strError = "info.OldRecordװ�ؽ���DOMʱ��������: " + ex.Message;
                    goto ERROR1;
                }

                if (bForce == false)
                {
                    // �Ƚ�������¼, �����������йص��ֶ��Ƿ����˱仯
                    // return:
                    //      0   û�б仯
                    //      1   �б仯
                    nRet = IsItemInfoChanged(domOld,
                        domExist);
                }

                if (nRet == 1 || bForce == true)    // 2008/10/19 new add
                {
                    error = new EntityInfo(info);
                    // ������Ϣ��, �������޸Ĺ���ԭ��¼����ʱ���
                    error.OldRecord = strExistXml;
                    error.OldTimestamp = exist_timestamp;

                    if (bExist == false)
                        error.ErrorInfo = "���������������: ���ݿ��е�ԭ��¼ (·��Ϊ'" + info.OldRecPath + "') �ѱ�ɾ����";
                    else
                        error.ErrorInfo = "���������������: ���ݿ��е�ԭ��¼ (·��Ϊ'" + info.OldRecPath + "') �ѷ������޸�";
                    error.ErrorCode = ErrorCodeValue.TimestampMismatch;
                    ErrorInfos.Add(error);
                    return -1;
                }

                // exist_timestamp��ʱ�Ѿ���ӳ�˿��б��޸ĺ�ļ�¼��ʱ���
            }


            // �ϲ��¾ɼ�¼
            string strWarning = "";
            string strNewXml = "";
            if (bForce == false)
            {
                // 2011/2/11
                nRet = CanChange(
    sessioninfo,
    "change",
    domExist,
    domNew,
    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    error = new EntityInfo(info);
                    error.ErrorInfo = strError;
                    error.ErrorCode = ErrorCodeValue.AccessDenied;
                    ErrorInfos.Add(error);
                    return -1;
                }

                // 2010/4/8
                nRet = this.App.SetOperation(
                    ref domNew,
                    "lastModified",
                    sessioninfo.UserID,
                    "",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // return:
                //      -1  ����
                //      0   ��ȷ
                //      1   �в����޸�û�ж��֡�˵����strError��
                nRet = MergeTwoItemXml(
                    sessioninfo,
                    domExist,
                    domNew,
                    out strNewXml,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 1)
                    strWarning = strError;
            }
            else
            {
                // 2008/10/19 new add
                strNewXml = domNew.OuterXml;
            }


            // �����¼�¼
            byte[] output_timestamp = null;
            lRet = channel.DoSaveTextRes(info.NewRecPath,
                strNewXml,
                false,   // include preamble?
                "content",
                exist_timestamp,
                out output_timestamp,
                out strOutputPath,
                out strError);
            if (lRet == -1)
            {

                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                {
                    if (nRedoCount > 10)
                    {
                        strError = "�������������ʱ�����ͻ, ����10��������Ȼʧ��";
                        goto ERROR1;
                    }
                    // ����ʱ�����ƥ��
                    // �ظ�������ȡ�Ѵ��ڼ�¼\�ȽϵĹ���
                    nRedoCount++;
                    goto REDOLOAD;
                }

                error = new EntityInfo(info);
                error.ErrorInfo = "���������������:" + strError;
                error.ErrorCode = channel.OriginErrorCode;
                ErrorInfos.Add(error);
                return -1;
            }
            else // �ɹ�
            {
                DomUtil.SetElementText(domOperLog.DocumentElement, "action", "change");

                // �¼�¼
                XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                    "record", strNewXml);
                DomUtil.SetAttr(node, "recPath", info.NewRecPath);

                // �ɼ�¼
                node = DomUtil.SetElementText(domOperLog.DocumentElement,
                    "oldRecord", strExistXml);
                DomUtil.SetAttr(node, "recPath", info.OldRecPath);

                // ����ɹ�����Ҫ������ϢԪ�ء���Ϊ��Ҫ�����µ�ʱ���
                error = new EntityInfo(info);
                error.NewTimestamp = output_timestamp;
                error.NewRecord = strNewXml;

                error.ErrorInfo = "��������ɹ���NewTimeStamp�з������µ�ʱ�����NewRecord�з�����ʵ�ʱ�����¼�¼(���ܺ��ύ���¼�¼���в���)��";
                if (string.IsNullOrEmpty(strWarning) == false)
                {
                    error.ErrorInfo = "��������ɹ�����" + strWarning;
                    error.ErrorCode = ErrorCodeValue.PartialDenied;
                }
                else
                    error.ErrorCode = ErrorCodeValue.NoError;
                ErrorInfos.Add(error);
            }

            return 0;

        ERROR1:
            error = new EntityInfo(info);
            error.ErrorInfo = strError;
            error.ErrorCode = ErrorCodeValue.CommonError;
            ErrorInfos.Add(error);
            return -1;
        }


        // ����/����������Ϣ
        // parameters:
        //      strBiblioRecPath    ��Ŀ��¼·����������������id���֡�������������ȷ����Ŀ�⣬id���Ա�ʵ���¼��������<parent>Ԫ�����ݡ�������Ŀ������IssueInfo�е�NewRecPath�γ�ӳ�չ�ϵ����Ҫ��������Ƿ���ȷ��Ӧ
        //      issueinfos Ҫ�ύ�ĵ�����Ϣ����
        // Ȩ�ޣ���Ҫ��setissuesȨ��
        // �޸����: д���ڿ��еļ�¼, ��ȱ��<operator>��<operTime>�ֶ�
        // TODO: ��Ҫ��д������upgrade��ֱ��д�벻���ز������¼���־�Ĺ���
        // TODO: ��Ҫ��鶩����¼��<parent>Ԫ�������Ƿ�Ϸ�������Ϊ�ʺ�
        public LibraryServerResult SetItems(
            SessionInfo sessioninfo,
            string strBiblioRecPath,
            EntityInfo[] iteminfos,
            out EntityInfo[] errorinfos)
        {
            errorinfos = null;

            LibraryServerResult result = new LibraryServerResult();

            int nRet = 0;
            long lRet = 0;
            string strError = "";

            string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
            string strBiblioRecId = ResPath.GetRecordId(strBiblioRecPath);

            if (string.IsNullOrEmpty(strBiblioRecPath) == false)    // 2013/9/26
            {
                if (String.IsNullOrEmpty(strBiblioRecId) == true)
                {
                    strError = "��Ŀ��¼·�� '" + strBiblioRecPath + "' �еļ�¼ID���ֲ���Ϊ��";
                    goto ERROR1;
                }
                if (StringUtil.IsPureNumber(strBiblioRecId) == false)
                {
                    strError = "��Ŀ��¼·�� '" + strBiblioRecPath + "' �еļ�¼ID���� '" + strBiblioRecId + "' ��ʽ����ȷ��ӦΪ������";
                    goto ERROR1;
                }
            }

            // �����Ŀ���Ӧ���������
            string strItemDbName = "";
            nRet = this.GetItemDbName(strBiblioDbName,
                 out strItemDbName,
                 out strError);
            if (nRet == -1)
                goto ERROR1;
#if NO
            if (nRet == 0)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' û���ҵ�";
                goto ERROR1;
            }

            if (String.IsNullOrEmpty(strItemDbName) == true)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' ��Ӧ��"+this.ItemName+"����û�ж���";
                goto ERROR1;
            }
#endif

            // 2012/3/29
            if (sessioninfo == null)
            {
                strError = "sessioninfo == null";
                goto ERROR1;
            }
            if (sessioninfo.Channels == null)
            {
                strError = "sessioninfo.Channels == null";
                goto ERROR1;
            }

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.App.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            byte[] output_timestamp = null;
            string strOutputPath = "";

            List<EntityInfo> ErrorInfos = new List<EntityInfo>();

            if (iteminfos == null)
            {
                strError = "iteminfos == null";
                goto ERROR1;
            }

            for (int i = 0; i < iteminfos.Length; i++)
            {
                EntityInfo info = iteminfos[i];
                if (info == null)
                {
                    Debug.Assert(false, "");
                    continue;
                }

                string strAction = info.Action;

                bool bForce = false;    // �Ƿ�Ϊǿ�Ʋ���(ǿ�Ʋ�����ȥ��Դ��¼�е���ͨ��Ϣ�ֶ�����)
                bool bNoCheckDup = false;   // �Ƿ�Ϊ������?
                bool bNoEventLog = false;   // �Ƿ�Ϊ�������¼���־?

                string strStyle = info.Style;

                if (StringUtil.IsInList("force", info.Style) == true)
                {
                    if (sessioninfo.UserType == "reader")
                    {
                        result.Value = -1;
                        result.ErrorInfo = "���з�� 'force' ���޸�" + this.ItemName + "��Ϣ��" + strAction + "�������ܾ���������ݲ��ܽ��������Ĳ�����";
                        result.ErrorCode = ErrorCode.AccessDenied;
                        return result;
                    }

                    bForce = true;

                    if (StringUtil.IsInList("restore", sessioninfo.RightsOrigin) == false)
                    {
                        result.Value = -1;
                        result.ErrorInfo = "���з�� 'force' ���޸�"+this.ItemName+"��Ϣ��" + strAction + "�������ܾ������߱�restoreȨ�ޡ�";
                        result.ErrorCode = ErrorCode.AccessDenied;
                        return result;
                    }
                }

                if (StringUtil.IsInList("nocheckdup", info.Style) == true)
                {
                    bNoCheckDup = true;
                    if (StringUtil.IsInList("restore", sessioninfo.RightsOrigin) == false)
                    {
                        result.Value = -1;
                        result.ErrorInfo = "���з�� 'nocheckdup' ���޸�"+this.ItemName+"��Ϣ��" + strAction + "�������ܾ������߱�restoreȨ�ޡ�";
                        result.ErrorCode = ErrorCode.AccessDenied;
                        return result;
                    }
                }

                if (StringUtil.IsInList("noeventlog", info.Style) == true)
                {
                    bNoEventLog = true;
                    if (StringUtil.IsInList("restore", sessioninfo.RightsOrigin) == false)
                    {
                        result.Value = -1;
                        result.ErrorInfo = "���з�� 'noeventlog' ���޸�"+this.ItemName+"��Ϣ��" + strAction + "�������ܾ������߱�restoreȨ�ޡ�";
                        result.ErrorCode = ErrorCode.AccessDenied;
                        return result;
                    }
                }


                // ��info�ڵĲ������м�顣
                strError = "";

                if (iteminfos.Length > 1  // 2013/9/26 ֻ��һ����¼��ʱ�򣬲������� refid ��λ������Ϣ�����Ҳ�Ͳ���Ҫ���Ը������ RefID ��Ա��
                    && String.IsNullOrEmpty(info.RefID) == true)
                {
                    strError = "info.RefID û�и���";
                }

                if (string.IsNullOrEmpty(info.NewRecPath) == false
                    && info.NewRecPath.IndexOf(",") != -1)
                {
                    strError = "info.NewRecPathֵ '" + info.NewRecPath + "' �в��ܰ�������";
                }
                else if (string.IsNullOrEmpty(info.OldRecPath) == false
                    && info.OldRecPath.IndexOf(",") != -1)
                {
                    strError = "info.OldRecPathֵ '" + info.OldRecPath + "' �в��ܰ�������";
                }

                // TODO: ������Ϊ"delete"ʱ���Ƿ��������ֻ����OldRecPath������������NewRecPath
                // ������������ã���Ҫ������Ϊһ�µġ�
                if (info.Action == "delete")
                {
                    if (String.IsNullOrEmpty(info.NewRecord) == false)
                    {
                        strError = "strActionֵΪdeleteʱ, info.NewRecord��������Ϊ��";
                    }
                    else if (info.NewTimestamp != null)
                    {
                        strError = "strActionֵΪdeleteʱ, info.NewTimestamp��������Ϊ��";
                    }
                    // 2008/6/24 new add
                    else if (String.IsNullOrEmpty(info.NewRecPath) == false)
                    {
                        if (info.NewRecPath != info.OldRecPath)
                        {
                            strError = "strActionֵΪdeleteʱ, ���info.NewRecPath���գ��������ݱ����info.OldRecPathһ�¡�(info.NewRecPath='" + info.NewRecPath + "' info.OldRecPath='" + info.OldRecPath + "')";
                        }
                    }
                }
                else
                {
                    // ��delete��� info.NewRecord����벻Ϊ��
                    if (String.IsNullOrEmpty(info.NewRecord) == true)
                    {
                        strError = "strActionֵΪ" + info.Action + "ʱ, info.NewRecord��������Ϊ��";
                    }
                }

                if (info.Action == "new")
                {
                    if (String.IsNullOrEmpty(info.OldRecord) == false)
                    {
                        strError = "strActionֵΪnewʱ, info.OldRecord��������Ϊ��";
                    }
                    else if (info.OldTimestamp != null)
                    {
                        strError = "strActionֵΪnewʱ, info.OldTimestamp��������Ϊ��";
                    }

                }

                if (strError != "")
                {
                    EntityInfo error = new EntityInfo(info);
                    error.ErrorInfo = strError;
                    error.ErrorCode = ErrorCodeValue.CommonError;
                    ErrorInfos.Add(error);
                    continue;
                }


                // ���·���еĿ�������
                if (String.IsNullOrEmpty(info.NewRecPath) == false)
                {
                    strError = "";

                    string strDbName = ResPath.GetDbName(info.NewRecPath);

                    if (String.IsNullOrEmpty(strDbName) == true)
                    {
                        strError = "NewRecPath�����ݿ�����ӦΪ��";
                    }

                    if (string.IsNullOrEmpty(strItemDbName) == false    // �п���ǰ�� strBiblioRecPath Ϊ�գ��� strItemDbName ҲΪ��
                        && strDbName != strItemDbName)
                    {
                        // ����Ƿ�Ϊ�������Եĵ�ͬ����
                        // parameters:
                        //      strDbName   Ҫ�������ݿ���
                        //      strNeutralDbName    ��֪�������������ݿ���
                        if (this.App.IsOtherLangName(strDbName,
                            strItemDbName) == false)
                        {
                            if (strAction == "copy" || strAction == "move")
                            {
                                // �ٿ�strDbName�Ƿ�������һ��ʵ���
                                if (this.IsItemDbName(strDbName) == false)
                                    strError = "RecPath�����ݿ��� '" + strDbName + "' ����ȷ��ӦΪ"+this.ItemName+"����";
                            }
                            else
                                strError = "RecPath�����ݿ��� '" + strDbName + "' ����ȷ��ӦΪ '" + strItemDbName + "'��(��Ϊ��Ŀ����Ϊ '" + strBiblioDbName + "'�����Ӧ��" + this.ItemName + "����ӦΪ '" + strItemDbName + "' )";
                        }
                    }
                    else if (string.IsNullOrEmpty(strItemDbName) == true)   // 2013/9/26
                    {
                        // Ҫ��鿴�� strDbName �Ƿ�Ϊһ��ʵ�����
                        if (this.IsItemDbName(strDbName) == false)
                            strError = "RecPath�����ݿ��� '" + strDbName + "' ����ȷ��ӦΪ"+this.ItemName+"����";
                    }

                    if (strError != "")
                    {
                        EntityInfo error = new EntityInfo(info);
                        error.ErrorInfo = strError;
                        error.ErrorCode = ErrorCodeValue.CommonError;
                        ErrorInfos.Add(error);
                        continue;
                    }
                }

                // ��(ǰ�˷�������)�ɼ�¼װ�ص�DOM
                XmlDocument domOldRec = new XmlDocument();
                try
                {
                    // ��strOldRecord��Ŀ���ǲ���ı�info.OldRecord����, ��Ϊ���߿��ܱ����Ƶ������Ϣ��
                    string strOldRecord = info.OldRecord;
                    if (String.IsNullOrEmpty(strOldRecord) == true)
                        strOldRecord = "<root />";

                    domOldRec.LoadXml(strOldRecord);
                }
                catch (Exception ex)
                {
                    strError = "info.OldRecord XML��¼װ�ص�DOMʱ����: " + ex.Message;

                    EntityInfo error = new EntityInfo(info);
                    error.ErrorInfo = strError;
                    error.ErrorCode = ErrorCodeValue.CommonError;
                    ErrorInfos.Add(error);
                    continue;
                }

                // ��Ҫ������¼�¼װ�ص�DOM
                XmlDocument domNewRec = new XmlDocument();
                try
                {
                    // ��strNewRecord��Ŀ���ǲ���ı�info.NewRecord����, ��Ϊ���߿��ܱ����Ƶ������Ϣ��
                    string strNewRecord = info.NewRecord;

                    if (String.IsNullOrEmpty(strNewRecord) == true)
                        strNewRecord = "<root />";

                    domNewRec.LoadXml(strNewRecord);
                }
                catch (Exception ex)
                {
                    strError = "info.NewRecord XML��¼װ�ص�DOMʱ����: " + ex.Message;

                    EntityInfo error = new EntityInfo(info);
                    error.ErrorInfo = strError;
                    error.ErrorCode = ErrorCodeValue.CommonError;
                    ErrorInfos.Add(error);
                    continue;
                }

                // locateParam��Ԫ�� ׼��
                List<string> oldLocateParam = null;
                List<string> newLocateParam = null;

                /*
                string strOldPublishTime = "";
                string strNewPublishTime = "";

                string strOldParentID = "";
                string strNewParentID = "";
                 * */

                // �����õĲ���
                List<string> lockLocateParam = null;
                bool bLocked = false;

                try
                {


                    // ����new��change�Ĺ��в��� -- ����ʱ�����, Ҳ��Ҫ����
                    // delete����Ҫ����
                    if (info.Action == "new"
                        || info.Action == "change"
                        || info.Action == "delete"
                        || info.Action == "move")
                    {

                        // ����������ȡһ���³���ʱ��
                        // �����¾ɳ���ʱ���Ƿ��в���
                        // ��IssueInfo�е�OldRecord��NewRecord�а���������Ž��бȽ�, �����Ƿ����˱仯(��������Ҫ����)
                        // return:
                        //      -1  ����
                        //      0   ���
                        //      1   �����
                        nRet = CompareTwoItemLocateInfo(
                            strItemDbName,
                            domOldRec,
                            domNewRec,
                            out oldLocateParam,
                            out newLocateParam,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "CompareTwoIssueNo() error : " + strError;
                            goto ERROR1;
                        }

                        bool bIsOldNewLocateSame = false;
                        if (nRet == 0)
                            bIsOldNewLocateSame = true;
                        else
                            bIsOldNewLocateSame = false;


                        if (info.Action == "new"
                            || info.Action == "change"
                            || info.Action == "move")
                            lockLocateParam = newLocateParam;
                        else if (info.Action == "delete")
                        {
                            // ˳�����һЩ���
                            /*
                            if (String.IsNullOrEmpty(strNewPublishTime) == false)
                            {
                                strError = "û�б�Ҫ��delete������EntityInfo��, ����NewRecord����...���෴��ע��һ��Ҫ��OldRecord�а�������ɾ����ԭ��¼";
                                goto ERROR1;
                            }
                             * */
                            if (String.IsNullOrEmpty(info.NewRecord) == false)
                            {
                                strError = "û�б�Ҫ��delete������EntityInfo��, ����NewRecord����...���෴��ע��һ��Ҫ��OldRecord�а�������ɾ����ԭ��¼";
                                goto ERROR1;
                            }

                            lockLocateParam = oldLocateParam;
                        }

                        nRet = this.IsLocateParamNullOrEmpty(
                            lockLocateParam, 
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;


                        // ����
                        if (nRet == 0)
                        {
                            this.LockItem(lockLocateParam);
                            bLocked = true;
                        }

                        bool bIsNewLocateParamNull = false;
                        nRet = this.IsLocateParamNullOrEmpty(
                            newLocateParam,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 1)
                            bIsNewLocateParamNull = true;
                        else
                            bIsNewLocateParamNull = false;


                        if ((info.Action == "new"
        || info.Action == "change"
        || info.Action == "move")       // delete������У���¼
    && bNoCheckDup == false)
                        {
                            nRet = this.DoVerifyItemFunction(
                                sessioninfo,
                                strAction,
                                domNewRec,
                                out strError);
                            if (nRet != 0)
                            {
                                EntityInfo error = new EntityInfo(info);
                                error.ErrorInfo = strError;
                                error.ErrorCode = ErrorCodeValue.CommonError;
                                ErrorInfos.Add(error);
                                continue;
                            }
                        }


                        // ���г���ʱ�����
                        // TODO: ���ص�ʱ��Ҫע�⣬�����������Ϊ��move�����������������info.OldRecPath�صģ���Ϊ��������ɾ��
                        if (/*bIsOldNewLocateSame == false   // �¾ɳ���ʱ�䲻�ȣ��Ų��ء����������������Ч�ʡ�
                            &&*/ (info.Action == "new"
                                || info.Action == "change"
                                || info.Action == "move")       // delete����������
                            && bIsNewLocateParamNull == false
                            && bNoCheckDup == false)    // 2008/10/19 new add
                        {
                            /*
                            string strParentID = strNewParentID;

                            if (String.IsNullOrEmpty(strParentID) == true)
                                strParentID = strOldParentID;
                             * */

                            // TODO: �����ڼ�¼��oldLocateParm��newLocateParam�е�parentidӦ����ȣ�Ԥ�ȼ���

                            List<string> aPath = null;
                            // ���� ����¼ID+����ʱ�� ���ڿ���в���
                            // ������ֻ�������, ������ü�¼��
                            // return:
                            //      -1  error
                            //      ����    ���м�¼����(������nMax�涨�ļ���)
                            nRet = this.SearchItemRecDup(
                                // sessioninfo.Channels,
                                channel,
                                newLocateParam,
                                100,
                                out aPath,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            bool bDup = false;
                            if (nRet == 0)
                            {
                                bDup = false;
                            }
                            else if (nRet == 1) // ����һ��
                            {
                                if (aPath == null
                                    || aPath.Count == 0)
                                {
                                    strError = "aPath == null || aPath.Count == 0";
                                    goto ERROR1;
                                }

                                Debug.Assert(aPath.Count == 1, "");

                                if (info.Action == "new")
                                {
                                    if (aPath[0] == info.NewRecPath) // �������Լ�
                                        bDup = false;
                                    else
                                        bDup = true;// ��ļ�¼���Ѿ�ʹ������������

                                }
                                else if (info.Action == "change")
                                {
                                    Debug.Assert(info.NewRecPath == info.OldRecPath, "����������Ϊchangeʱ��info.NewRecPathӦ����info.OldRecPath��ͬ");
                                    if (aPath[0] == info.OldRecPath) // �������Լ�
                                        bDup = false;
                                    else
                                        bDup = true;// ��ļ�¼���Ѿ�ʹ������������
                                }
                                else if (info.Action == "move")
                                {
                                    if (aPath[0] == info.OldRecPath) // ������Դ��¼
                                        bDup = false;
                                    else
                                        bDup = true;// ��ļ�¼���Ѿ�ʹ������������
                                }
                                else
                                {
                                    Debug.Assert(false, "���ﲻ���ܳ��ֵ�info.Actionֵ '" + info.Action + "'");
                                }


                            } // end of if (nRet == 1)
                            else
                            {
                                Debug.Assert(nRet > 1, "");
                                bDup = true;

                                // ��Ϊmove����������Ŀ��λ�ô��ڼ�¼����������Ͳ��ٷ���������
                                // �������move��������Ŀ��λ�ô��ڼ�¼����������Ҫ�жϣ�����Դ����Ŀ��λ�÷���������أ��������ء�
                            }

                            // ����
                            if (bDup == true)
                            {
                                /*
                                string[] pathlist = new string[aPath.Count];
                                aPath.CopyTo(pathlist);
                                 * */

                                string strText = "";
                                // ���춨λ��ʾ��Ϣ�����ڱ���
                                nRet = GetLocateText(
                                    newLocateParam,
                                    out strText,
                                    out strError);
                                if (nRet == -1)
                                {
                                    strError = "��λ��Ϣ�ظ�������GetLocateText()��������: " + strError;
                                }
                                else
                                {
                                    strError = strText + " �Ѿ�������" + this.ItemName + "��¼ʹ����: " + StringUtil.MakePathList(aPath)/*String.Join(",", pathlist)*/;
                                }

                                EntityInfo error = new EntityInfo(info);
                                error.ErrorInfo = strError; // "����ʱ�� '" + strNewPublishTime + "' �Ѿ�������"+this.ItemName+"��¼ʹ����: " + String.Join(",", pathlist);
                                error.ErrorCode = ErrorCodeValue.CommonError;
                                ErrorInfos.Add(error);
                                continue;
                            }
                        }
                    }

                    // ׼����־DOM
                    XmlDocument domOperLog = new XmlDocument();
                    domOperLog.LoadXml("<root />");

                    Debug.Assert(String.IsNullOrEmpty(this.OperLogSetName) == false, "");
                    Debug.Assert(Char.IsLower(this.OperLogSetName[0]) == true, this.OperLogSetName + " �ĵ�һ���ַ�Ӧ��ΪСд��ĸ�����ǹ���");
                    // �͹ݴ���ģ���йء����Ҫд��ݴ��룬���Կ����ͺ�д��
                    DomUtil.SetElementText(domOperLog.DocumentElement,
                        "operation", 
                        OperLogSetName /*"setIssue"*/);

                    // ����һ������
                    if (info.Action == "new")
                    {
                        // ����¼�¼��·���е�id�����Ƿ���ȷ
                        // �������֣�ǰ���Ѿ�ͳһ������
                        strError = "";

                        if (String.IsNullOrEmpty(info.NewRecPath) == true)
                        {
                            info.NewRecPath = strItemDbName + "/?";
                        }
                        else
                        {

                            string strID = ResPath.GetRecordId(info.NewRecPath);
                            if (String.IsNullOrEmpty(strID) == true)
                            {
                                strError = "RecPath��id����Ӧ��Ϊ'?'";
                            }

                            if (strError != "")
                            {
                                EntityInfo error = new EntityInfo(info);
                                error.ErrorInfo = strError;
                                error.ErrorCode = ErrorCodeValue.CommonError;
                                ErrorInfos.Add(error);
                                continue;
                            }
                        }

                        // ������ʺϱ�����������¼
                        string strNewXml = "";
                        nRet = BuildNewItemRecord(
                            sessioninfo,
                            bForce,
                            strBiblioRecId,
                            info.NewRecord,
                            out strNewXml,
                            out strError);
                        if (nRet == -1)
                        {
                            EntityInfo error = new EntityInfo(info);
                            error.ErrorInfo = strError;
                            error.ErrorCode = ErrorCodeValue.CommonError;
                            ErrorInfos.Add(error);
                            continue;
                        }

                        {
                            XmlDocument domNew = new XmlDocument();
                            try
                            {
                                domNew.LoadXml(strNewXml);
                            }
                            catch (Exception ex)
                            {
                                EntityInfo error = new EntityInfo(info);
                                error.ErrorInfo = "���ⴴ����XML��¼װ��DOMʱ����" + ex.Message;
                                error.ErrorCode = ErrorCodeValue.CommonError;
                                ErrorInfos.Add(error);
                                continue;
                            }

                            // 2011/4/11
                            nRet = CanCreate(
                sessioninfo,
                domNew,
                out strError);
                            if (nRet == -1)
                            {
                                EntityInfo error = new EntityInfo(info);
                                error.ErrorInfo = strError;
                                error.ErrorCode = ErrorCodeValue.CommonError;
                                ErrorInfos.Add(error);
                                continue;
                            }
                            if (nRet == 0)
                            {
                                EntityInfo error = new EntityInfo(info);
                                error.ErrorInfo = strError;
                                error.ErrorCode = ErrorCodeValue.AccessDenied;
                                ErrorInfos.Add(error);
                                continue;
                            }
                        }


                        // 2010/4/8
                        XmlDocument temp = new XmlDocument();
                        temp.LoadXml(strNewXml);
                        nRet = this.App.SetOperation(
                            ref temp,
                            "create",
                            sessioninfo.UserID,
                            "",
                            out strError);
                        if (nRet == -1)
                        {
                            EntityInfo error = new EntityInfo(info);
                            error.ErrorInfo = strError;
                            error.ErrorCode = ErrorCodeValue.CommonError;
                            ErrorInfos.Add(error);
                            continue;
                        }
                        strNewXml = temp.DocumentElement.OuterXml;

                        lRet = channel.DoSaveTextRes(info.NewRecPath,
                            strNewXml,
                            false,   // include preamble?
                            "content",
                            info.OldTimestamp,
                            out output_timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            EntityInfo error = new EntityInfo(info);
                            error.NewTimestamp = output_timestamp;
                            error.ErrorInfo = "�����¼�¼�Ĳ�����������:" + strError;
                            error.ErrorCode = channel.OriginErrorCode;
                            ErrorInfos.Add(error);

                            domOperLog = null;  // ��ʾ����д����־
                        }
                        else // �ɹ�
                        {

                            DomUtil.SetElementText(domOperLog.DocumentElement,
                                "action",
                                "new");

                            // ������<oldRecord>Ԫ��

                            // ����<record>Ԫ��
                            XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                                "record", strNewXml);
                            DomUtil.SetAttr(node, "recPath", strOutputPath);

                            // �¼�¼����ɹ�����Ҫ������ϢԪ�ء���Ϊ��Ҫ�����µ�ʱ�����ʵ�ʱ���ļ�¼·��

                            EntityInfo error = new EntityInfo(info);
                            error.NewRecPath = strOutputPath;

                            error.NewRecord = strNewXml;    // ����������ļ�¼���������б仯, �����Ҫ���ظ�ǰ��
                            error.NewTimestamp = output_timestamp;

                            error.ErrorInfo = "�����¼�¼�Ĳ����ɹ���NewTimeStamp�з������µ�ʱ���, RecPath�з�����ʵ�ʴ���ļ�¼·����";
                            error.ErrorCode = ErrorCodeValue.NoError;
                            ErrorInfos.Add(error);
                        }
                    }
                    else if (info.Action == "change")
                    {
                        // ִ��SetIssues API�е�"change"����
                        nRet = DoOperChange(
                            bForce,
                            sessioninfo,
                            channel,
                            info,
                            ref domOperLog,
                            ref ErrorInfos);
                        if (nRet == -1)
                        {
                            // ʧ��
                            domOperLog = null;  // ��ʾ����д����־
                        }
                    }
                    else if (info.Action == "move")
                    {
                        // ִ��SetIssues API�е�"move"����
                        nRet = DoOperMove(
                            sessioninfo,
                            channel,
                            info,
                            ref domOperLog,
                            ref ErrorInfos);
                        if (nRet == -1)
                        {
                            // ʧ��
                            domOperLog = null;  // ��ʾ����д����־
                        }
                    }
                    else if (info.Action == "delete")
                    {
                        /*
                        string strParentID = strNewParentID;

                        if (String.IsNullOrEmpty(strParentID) == true)
                            strParentID = strOldParentID;
                         * */

                        // TODO: �����ڼ�¼��oldLocateParm��Ӧ������parentid��Ԥ�ȼ���

                        // ɾ���ڼ�¼�Ĳ���
                        nRet = DoOperDelete(
                            sessioninfo,
                            bForce,
                            channel,
                            info,
                            oldLocateParam,
                            domOldRec,
                            ref domOperLog,
                            ref ErrorInfos);
                        if (nRet == -1)
                        {
                            // ʧ��
                            domOperLog = null;  // ��ʾ����д����־
                        }
                    }
                    else
                    {
                        // ��֧�ֵ�����
                        EntityInfo error = new EntityInfo(info);
                        error.ErrorInfo = "��֧�ֵĲ������� '" + info.Action + "'";
                        error.ErrorCode = ErrorCodeValue.CommonError;
                        ErrorInfos.Add(error);
                    }


                    // д����־
                    if (domOperLog != null
                        && bNoEventLog == false)    // 2008/10/19 new add
                    {
                        string strOperTime = this.App.Clock.GetClock();
                        DomUtil.SetElementText(domOperLog.DocumentElement,
                            "operator",
                            sessioninfo.UserID);   // ������
                        DomUtil.SetElementText(domOperLog.DocumentElement, 
                            "operTime",
                            strOperTime);   // ����ʱ��

                        nRet = this.App.OperLog.WriteOperLog(domOperLog,
                            sessioninfo.ClientAddress,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = this.SetApiName + "() API д����־ʱ��������: " + strError;
                            goto ERROR1;
                        }
                    }
                }
                finally
                {
                    if (bLocked == true)
                        this.UnlockItem(lockLocateParam);
                }

            }

            // ���Ƶ������
            errorinfos = new EntityInfo[ErrorInfos.Count];
            for (int i = 0; i < ErrorInfos.Count; i++)
            {
                errorinfos[i] = ErrorInfos[i];
            }

            result.Value = ErrorInfos.Count;  // ������Ϣ������
            return result;
        ERROR1:
            // ����ı����ǱȽ����صĴ�������������в��ֵ��������Ĵ����������ﱨ������ͨ�����ش�����Ϣ����ķ�ʽ������
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        // ִ�нű����� VerifyItem
        // parameters:
        // return:
        //      -2  not found script
        //      -1  ����
        //      0   �ɹ�
        public int DoVerifyItemFunction(
            SessionInfo sessioninfo,
            string strAction,
            XmlDocument itemdom,
            out string strError)
        {
            strError = "";
            if (this.App.m_strAssemblyLibraryHostError != "")
            {
                strError = this.App.m_strAssemblyLibraryHostError;
                return -1;
            }

            if (this.App.m_assemblyLibraryHost == null)
            {
                strError = "δ����<script>�ű����룬�޷�У����¼��";
                return -2;
            }

            Type hostEntryClassType = ScriptManager.GetDerivedClassType(
                this.App.m_assemblyLibraryHost,
                "DigitalPlatform.LibraryServer.LibraryHost");
            if (hostEntryClassType == null)
            {
                strError = "<script>�ű���δ�ҵ�DigitalPlatform.LibraryServer.LibraryHost��������࣬�޷�У������š�";
                return -2;
            }

#if NO
            // �ٰ󶨼�������assembly��ʵʱѰ���ض����ֵĺ���
            MethodInfo mi = hostEntryClassType.GetMethod("VerifyItem");
            if (mi == null)
            {
                strError = "<script>�ű���DigitalPlatform.LibraryServer.LibraryHost����������У�û���ṩint VerifyItem(string strAction, XmlDocument itemdom, out string strError)����������޷�У����¼��";
                return -2;
            }
#endif

            LibraryHost host = (LibraryHost)hostEntryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);
            if (host == null)
            {
                strError = "���� DigitalPlatform.LibraryServer.LibraryHost ���������Ķ��󣨹��캯����ʧ�ܡ�";
                return -1;
            }

            host.App = this.App;
            host.SessionInfo = sessioninfo;

            // ִ�к���
            return VerifyItem(host,
                strAction,
                itemdom,
                out strError);
        }

        // return:
        //      -1  ���ó���
        //      0   У����ȷ
        //      1   У�鷢�ִ���
        public virtual int VerifyItem(
            LibraryHost host,
            string strAction,
            XmlDocument itemdom,
            out string strError)
        {
            strError = "";

            return 0;
        }

        // TODO: �����ڼ�¼����Ҫ���޶��ڷ�Χ������
        // ����������ȫ��������strBiblioRecPath�ļ�¼��Ϣ
        // ע��Ҫ��ÿ������ⶼ��һ��������¼������;��
        // parameters:
        //      strBiblioRecPath    ��Ŀ��¼·����������������id����
        //      strStyle    "onlygetpath"   ������ÿ��·��(OldRecPath)
        //                  "getfirstxml"   �Ƕ�onlygetpath�Ĳ��䣬����õ�һ��Ԫ�ص�XML��¼���������Ȼֻ����·��
        //      items ���ص�������Ϣ����
        // Ȩ�ޣ�Ȩ��Ҫ��API�����ж�(��Ҫ��get...sȨ��)��
        // return:
        //      Result.Value    -1���� 0û���ҵ� ���� ʵ���¼�ĸ���
        public LibraryServerResult GetItems(
            SessionInfo sessioninfo,
            string strBiblioRecPath,
            long lStart,
            long lCount,
            string strStyle,
            string strLang,
            out EntityInfo[] items)
        {
            items = null;

            LibraryServerResult result = new LibraryServerResult();

            int nRet = 0;
            string strError = "";

            // �淶������ֵ
            if (lCount == 0)
                lCount = -1;

            string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
            string strBiblioRecId = ResPath.GetRecordId(strBiblioRecPath);

            // �����Ŀ���Ӧ���������
            string strItemDbName = "";
            nRet = this.GetItemDbName(strBiblioDbName,
                 out strItemDbName,
                 out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' û���ҵ�";
                goto ERROR1;
            }

            if (String.IsNullOrEmpty(strItemDbName) == true)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' ��Ӧ��"+this.ItemName+"����û�ж���";
                goto ERROR1;
            }

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.App.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            // �����������ȫ���������ض�id�ļ�¼
            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(strItemDbName + ":" + "����¼")       // 2007/9/14 new add
                + "'><item><word>"
                + strBiblioRecId
                + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>" + "zh" + "</lang></target>";
            long lRet = channel.DoSearch(strQueryXml,
                this.DefaultResultsetName,
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
                goto ERROR1;

            if (lRet == 0)
            {
                result.Value = 0;
                result.ErrorInfo = "û���ҵ�";
                return result;
            }

            int MAXPERBATCH = 100;

            int nResultCount = (int)lRet;

            if (lCount == -1)
                lCount = nResultCount - (int)lStart;

            // lStart�Ƿ�Խ��
            if (lStart >= (long)nResultCount)
            {
                strError = "lStart����ֵ " + lStart.ToString() + " ���������н������β�������н������Ϊ " + nResultCount.ToString();
                goto ERROR1;
            }

            // ����lCount
            if (lStart + lCount > (long)nResultCount)
            {
                lCount = (long)nResultCount - lStart;
            }

            // �Ƿ񳬹�ÿ�����ֵ
            if (lCount > MAXPERBATCH)
                lCount = MAXPERBATCH;

            /*
            if (nResultCount > 10000)
            {
                strError = "����"+this.ItemName+"��¼�� " + nResultCount.ToString() + " ���� 10000, ��ʱ��֧��";
                goto ERROR1;
            }
             * */

            List<EntityInfo> iteminfos = new List<EntityInfo>();

            /*
            int nStart = 0;
            int nPerCount = 100;
             * */
            int nStart = (int)lStart;
            int nPerCount = Math.Min(MAXPERBATCH, (int)lCount);

            for (; ; )
            {
                List<string> aPath = null;
                lRet = channel.DoGetSearchResult(
                    this.DefaultResultsetName,
                    nStart,
                    nPerCount,
                    strLang,
                    null,
                    out aPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (aPath.Count == 0)
                {
                    strError = "aPath.Count == 0";
                    goto ERROR1;
                }

                bool bOnlyGetPath = StringUtil.IsInList("onlygetpath", strStyle);
                bool bGetFirstXml = StringUtil.IsInList("getfirstxml", strStyle);

                // ���ÿ����¼
                for (int i = 0; i < aPath.Count; i++)
                {
                    EntityInfo iteminfo = new EntityInfo();
                    if (bOnlyGetPath == true)
                    {
                        if (bGetFirstXml == false
                            || i > 0)
                        {
                            iteminfo.OldRecPath = aPath[i];
                            goto CONTINUE;
                        }
                    }
                    string strMetaData = "";
                    string strXml = "";
                    byte[] timestamp = null;
                    string strOutputPath = "";

                    lRet = channel.GetRes(aPath[i],
                        out strXml,
                        out strMetaData,
                        out timestamp,
                        out strOutputPath,
                        out strError);

                    if (lRet == -1)
                    {
                        iteminfo.OldRecPath = aPath[i];
                        iteminfo.ErrorCode = channel.OriginErrorCode;
                        iteminfo.ErrorInfo = channel.ErrorInfo;

                        iteminfo.OldRecord = "";
                        iteminfo.OldTimestamp = null;

                        iteminfo.NewRecPath = "";
                        iteminfo.NewRecord = "";
                        iteminfo.NewTimestamp = null;
                        iteminfo.Action = "";


                        goto CONTINUE;
                    }

                    iteminfo.OldRecPath = strOutputPath;
                    iteminfo.OldRecord = strXml;
                    iteminfo.OldTimestamp = timestamp;

                    iteminfo.NewRecPath = "";
                    iteminfo.NewRecord = "";
                    iteminfo.NewTimestamp = null;
                    iteminfo.Action = "";

                CONTINUE:
                    iteminfos.Add(iteminfo);
                }

                nStart += aPath.Count;
                if (nStart >= nResultCount)
                    break;

                if (iteminfos.Count >= lCount)
                    break;

                // ����nPerCount
                if (iteminfos.Count + nPerCount > lCount)
                    nPerCount = (int)lCount - iteminfos.Count;

            }

            // �ҽӵ������
            items = new EntityInfo[iteminfos.Count];
            for (int i = 0; i < iteminfos.Count; i++)
            {
                items[i] = iteminfos[i];
            }

            result.Value = nResultCount;    // items.Length;
            return result;

        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }


        // ���������
        public LibraryServerResult SearchItemDup(
            // RmsChannelCollection Channels,
            RmsChannel channel,
            List<string> locateParam,
            /*
            string strPublishTime,
            string strBiblioRecPath,
             * */
            int nMax,
            out string[] paths)
        {
            paths = null;

            LibraryServerResult result = new LibraryServerResult();
            int nRet = 0;
            string strError = "";

            List<string> aPath = null;

            nRet = this.SearchItemRecDup(
                // Channels,
                channel,
                locateParam,
                nMax,
                out aPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 0)
            {
                paths = new string[0];
                result.Value = 0;
                result.ErrorInfo = "û���ҵ�";
                result.ErrorCode = ErrorCode.NotFound;
                return result;
            }

            // ���Ƶ������
            paths = new string[aPath.Count];
            for (int i = 0; i < aPath.Count; i++)
            {
                paths[i] = aPath[i];
            }

            result.Value = paths.Length;
            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }

        // TODO: �Ƿ�����ͨ��Ϣ����Ҫ����ͨ����������
        // ������Ŀ��¼�����������¼������������Ҫ����Ϣ�������ṩ����ʵ��ɾ��ʱʹ��
        // parameters:
        //      strStyle    return_record_xml Ҫ��DeleteEntityInfo�ṹ�з���OldRecord����
        //                  check_circulation_info ����Ƿ������ͨ��Ϣ�����������ᱨ�� 2012/12/19 ��ȱʡ��Ϊ��Ϊ�˲���
        // return:
        //      -1  error
        //      0   not exist item dbname
        //      1   exist item dbname
        public int SearchChildItems(RmsChannel channel,
            string strBiblioRecPath,
            string strStyle,
            out long lHitCount,
            out List<DeleteEntityInfo> entityinfos,
            out string strError)
        {
            strError = "";
            lHitCount = 0;
            entityinfos = new List<DeleteEntityInfo>();

            int nRet = 0;

            bool bReturnRecordXml = StringUtil.IsInList("return_record_xml", strStyle);
            bool bCheckCirculationInfo = StringUtil.IsInList("check_circulation_info", strStyle);
            bool bOnlyGetCount = StringUtil.IsInList("only_getcount", strStyle);

            string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
            string strBiblioRecId = ResPath.GetRecordId(strBiblioRecPath);

            // �����Ŀ���Ӧ���������
            string strItemDbName = "";
            nRet = this.GetItemDbName(strBiblioDbName,
                 out strItemDbName,
                 out strError);
            if (nRet == -1)
                goto ERROR1;

            if (String.IsNullOrEmpty(strItemDbName) == true)
                return 0;


            // ����ʵ�����ȫ���������ض�id�ļ�¼

            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(strItemDbName + ":" + "����¼") 
                + "'><item><word>"
                + strBiblioRecId
                + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>" + "zh" + "</lang></target>";

            long lRet = channel.DoSearch(strQueryXml,
                "entities",
                "", // strOuputStyle
                out strError);
            if (lRet == -1)
                goto ERROR1;

            if (lRet == 0)
            {
                strError = "û���ҵ�������Ŀ��¼ '" + strBiblioRecPath + "' ���κ�"+this.ItemName+"��¼";
                return 0;
            }

            lHitCount = lRet;

            // ��������������
            if (bOnlyGetCount == true)
                return 0;

            int nResultCount = (int)lRet;
            int nMaxCount = 10000;
            if (nResultCount > nMaxCount)
            {
                strError = "����"+this.ItemName+"��¼�� " + nResultCount.ToString() + " ���� "+nMaxCount.ToString()+", ��ʱ��֧��������ǵ�ɾ������";
                goto ERROR1;
            }

            int nStart = 0;
            int nPerCount = 100;
            for (; ; )
            {
                List<string> aPath = null;
                lRet = channel.DoGetSearchResult(
                    "entities",
                    nStart,
                    nPerCount,
                    "zh",
                    null,
                    out aPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                if (aPath.Count == 0)
                {
                    strError = "aPath.Count == 0";
                    goto ERROR1;
                }

                // ���ÿ����¼
                for (int i = 0; i < aPath.Count; i++)
                {
                    string strMetaData = "";
                    string strXml = "";
                    byte[] timestamp = null;
                    string strOutputPath = "";

                    lRet = channel.GetRes(aPath[i],
                        out strXml,
                        out strMetaData,
                        out timestamp,
                        out strOutputPath,
                        out strError);
                    DeleteEntityInfo entityinfo = new DeleteEntityInfo();

                    if (lRet == -1)
                    {
                        /*
                        entityinfo.RecPath = aPath[i];
                        entityinfo.ErrorCode = channel.OriginErrorCode;
                        entityinfo.ErrorInfo = channel.ErrorInfo;

                        entityinfo.OldRecord = "";
                        entityinfo.OldTimestamp = null;
                        entityinfo.NewRecord = "";
                        entityinfo.NewTimestamp = null;
                        entityinfo.Action = "";
                         * */
                        if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            continue;

                        strError = "��ȡ"+this.ItemName+"��¼ '" + aPath[i] + "' ʱ��������: " + strError;
                        goto ERROR1;
                        // goto CONTINUE;
                    }

                    entityinfo.RecPath = strOutputPath;
                    entityinfo.OldTimestamp = timestamp;
                    if (bReturnRecordXml == true)
                        entityinfo.OldRecord = strXml;

                    if (bCheckCirculationInfo == true)
                    {
                        // ����Ƿ��н�����Ϣ
                        // �Ѽ�¼װ��DOM
                        XmlDocument domExist = new XmlDocument();

                        try
                        {
                            domExist.LoadXml(strXml);
                        }
                        catch (Exception ex)
                        {
                            strError = this.ItemName + "��¼ '" + aPath[i] + "' װ�ؽ���DOMʱ��������: " + ex.Message;
                            goto ERROR1;
                        }

                        /*
                        entityinfo.ItemBarcode = DomUtil.GetElementText(domExist.DocumentElement,
                            "barcode");
                         * */

                        // TODO: ����־�ָ��׶ε��ñ�����ʱ���Ƿ��б�Ҫ����Ƿ������ͨ��Ϣ���ƺ���ʱӦǿ��ɾ��Ϊ��

                        // �۲��Ѿ����ڵļ�¼�Ƿ�����ͨ��Ϣ
                        // return:
                        //      -1  ����
                        //      0   û��
                        //      1   �С�������Ϣ��strError��
                        nRet = this.HasCirculationInfo(domExist, out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 1)
                        {
                            strError = "��ɾ����" + this.ItemName + "��¼ '" + entityinfo.RecPath + "' ��" + strError + "(����������ܲ�������һ��)������ɾ�������ȫ��ɾ����������������";
                            goto ERROR1;
                        }

                    }

                    // CONTINUE:
                    entityinfos.Add(entityinfo);
                }

                nStart += aPath.Count;
                if (nStart >= nResultCount)
                    break;
            }

            return 1;
        ERROR1:
            return -1;
        }

        // ��������ͬһ��Ŀ��¼��ȫ��ʵ���¼
        // parameters:
        //      strAction   copy / move
        // return:
        //      -2  Ŀ��ʵ��ⲻ���ڣ��޷����и��ƻ���ɾ��
        //      -1  error
        //      >=0  ʵ�ʸ��ƻ����ƶ���ʵ���¼��
        public int CopyBiblioChildItems(RmsChannel channel,
            string strAction,
            List<DeleteEntityInfo> entityinfos,
            string strTargetBiblioRecPath,
            XmlDocument domOperLog,
            out string strError)
        {
            strError = "";

            if (entityinfos == null || entityinfos.Count == 0)
                return 0;

            int nOperCount = 0;

            XmlNode root = null;

            if (domOperLog != null)
            {
                root = domOperLog.CreateElement(strAction == "copy" ? "copy" + this.ItemNameInternal + "Records" : "move" + this.ItemNameInternal + "Records");
                domOperLog.DocumentElement.AppendChild(root);
            }

            // ���Ŀ����Ŀ��������ʵ�����
            string strTargetItemDbName = "";
            string strTargetBiblioDbName = ResPath.GetDbName(strTargetBiblioRecPath);
            // return:
            //      -1  ����
            //      0   û���ҵ�
            //      1   �ҵ�
            int nRet = this.GetItemDbName(strTargetBiblioDbName,
                out strTargetItemDbName,
                out strError);
            if (nRet == 0 || string.IsNullOrEmpty(strTargetItemDbName) == true)
            {
                return -2;   // Ŀ��ʵ��ⲻ����
            }

            string strParentID = ResPath.GetRecordId(strTargetBiblioRecPath);
            if (string.IsNullOrEmpty(strParentID) == true)
            {
                strError = "Ŀ����Ŀ��¼·�� '" + strTargetBiblioRecPath + "' ����ȷ���޷���ü�¼��";
                return -1;
            }



            List<string> newrecordpaths = new List<string>();
            List<string> oldrecordpaths = new List<string>();
            for (int i = 0; i < entityinfos.Count; i++)
            {
                DeleteEntityInfo info = entityinfos[i];

                byte[] output_timestamp = null;
                string strOutputRecPath = "";

                // this.EntityLocks.LockForWrite(info.ItemBarcode);
                try
                {
                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(info.OldRecord);
                    }
                    catch (Exception ex)
                    {
                        strError = "��¼ '" + info.RecPath + "' װ��XMLDOM��������: " + ex.Message;
                        goto ERROR1;
                    }
                    DomUtil.SetElementText(dom.DocumentElement,
                        "parent",
                        strParentID);

                    // ���Ƶ����
                    if (strAction == "copy")
                    {
                        // ����refID�ظ�
                        DomUtil.SetElementText(dom.DocumentElement,
                            "refID",
                            null);
                    }

                    long lRet = channel.DoCopyRecord(info.RecPath,
                         strTargetItemDbName + "/?",
                         strAction == "move" ? true : false,   // bDeleteSourceRecord
                         out output_timestamp,
                         out strOutputRecPath,
                         out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            continue;
                        strError = "����" + this.ItemName + "��¼ '" + info.RecPath + "' ʱ��������: " + strError;
                        goto ERROR1;
                    }

                    // 2011/5/24
                    // �޸�xml��¼��<parent>Ԫ�ط����˱仯
                    byte[] baOutputTimestamp = null;
                    string strOutputRecPath1 = "";
                    lRet = channel.DoSaveTextRes(strOutputRecPath,
                        dom.OuterXml,
                        false,
                        "content", // ,ignorechecktimestamp
                        output_timestamp,
                        out baOutputTimestamp,
                        out strOutputRecPath1,
                        out strError);
                    if (lRet == -1)
                        goto ERROR1;

                    oldrecordpaths.Add(info.RecPath);
                    newrecordpaths.Add(strOutputRecPath);
                }
                finally
                {
                    // this.EntityLocks.UnlockForWrite(info.ItemBarcode);
                }

                // ��������־DOM��
                if (domOperLog != null)
                {
                    Debug.Assert(root != null, "");

                    XmlNode node = domOperLog.CreateElement("record");
                    root.AppendChild(node);

                    DomUtil.SetAttr(node, "recPath", info.RecPath);
                    DomUtil.SetAttr(node, "targetRecPath", strOutputRecPath);
                }

                nOperCount++;
            }


            return nOperCount;
        ERROR1:
            // Undo�Ѿ����й��Ĳ���
            if (strAction == "copy")
            {
                string strWarning = "";

                foreach (string strRecPath in newrecordpaths)
                {
                    string strTempError = "";
                    byte[] timestamp = null;
                    byte[] output_timestamp = null;
                REDO_DELETE:
                    long lRet = channel.DoDeleteRes(strRecPath,
                        timestamp,
                        out output_timestamp,
                        out strTempError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {
                            if (timestamp == null)
                            {
                                timestamp = output_timestamp;
                                goto REDO_DELETE;
                            }
                        }
                        strWarning += strTempError + ";";
                    }

                }
                if (string.IsNullOrEmpty(strWarning) == false)
                    strError = strError + "����Undo�����У�����������: " + strWarning;
            }
            else if (strAction == "move")
            {
                string strWarning = "";
                for (int i = 0; i < newrecordpaths.Count; i++)
                {
                    byte[] output_timestamp = null;
                    string strOutputRecPath = "";
                    string strTempError = "";
                    long lRet = channel.DoCopyRecord(newrecordpaths[i],
         oldrecordpaths[i],
         true,   // bDeleteSourceRecord
         out output_timestamp,
         out strOutputRecPath,
         out strTempError);
                    if (lRet == -1)
                    {
                        strWarning += strTempError + ";";
                    }
                }
                if (string.IsNullOrEmpty(strWarning) == false)
                    strError = strError + "����Undo�����У�����������: " + strWarning;
            }
            return -1;
        }

        // ɾ������ͬһ��Ŀ��¼��ȫ��ʵ���¼
        // ������Ҫ�ṩEntityInfo����İ汾
        // return:
        //      -1  error
        //      0   û���ҵ�������Ŀ��¼���κ�ʵ���¼�����Ҳ���޴�ɾ��
        //      >0  ʵ��ɾ����ʵ���¼��
        public int DeleteBiblioChildItems(RmsChannelCollection Channels,
            List<DeleteEntityInfo> entityinfos,
            XmlDocument domOperLog,
            out string strError)
        {
            strError = "";

            if (entityinfos == null || entityinfos.Count == 0)
                return 0;

            int nDeletedCount = 0;

            XmlNode root = null;

            if (domOperLog != null)
            {
                root = domOperLog.CreateElement("deleted"+this.ItemNameInternal+"Records");
                domOperLog.DocumentElement.AppendChild(root);
            }

            RmsChannel channel = Channels.GetChannel(this.App.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            // ����ʵ��ɾ��
            for (int i = 0; i < entityinfos.Count; i++)
            {
                DeleteEntityInfo info = entityinfos[i];

                byte[] output_timestamp = null;
                int nRedoCount = 0;

            REDO_DELETE:

                // this.EntityLocks.LockForWrite(info.ItemBarcode);
                try
                {

                    long lRet = channel.DoDeleteRes(info.RecPath,
                        info.OldTimestamp,
                        out output_timestamp,
                        out strError);
                    if (lRet == -1)
                    {
                        if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            continue;

                        // ��������ԣ���ʱ�������¶������
                        // ���Ҫ���ԣ�Ҳ�ü������¶�����¼���ж������ж��޽軹��Ϣ����ɾ��

                        if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                        {
                            if (nRedoCount > 10)
                            {
                                strError = "������10�λ����С�ɾ��"+this.ItemName+"��¼ '" + info.RecPath + "' ʱ��������: " + strError;
                                goto ERROR1;
                            }
                            nRedoCount++;

                            // ���¶����¼
                            string strMetaData = "";
                            string strXml = "";
                            string strOutputPath = "";
                            string strError_1 = "";

                            lRet = channel.GetRes(info.RecPath,
                                out strXml,
                                out strMetaData,
                                out output_timestamp,
                                out strOutputPath,
                                out strError_1);
                            if (lRet == -1)
                            {
                                if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                    continue;

                                strError = "��ɾ��"+this.ItemName+"��¼ '" + info.RecPath + "' ʱ����ʱ�����ͻ�������Զ����»�ȡ��¼�����ַ�������: " + strError_1;
                                goto ERROR1;
                                // goto CONTINUE;
                            }

                            // ����Ƿ��н�����Ϣ
                            // �Ѽ�¼װ��DOM
                            XmlDocument domExist = new XmlDocument();

                            try
                            {
                                if (String.IsNullOrEmpty(strXml) == false)
                                    domExist.LoadXml(strXml);
                                else
                                    domExist.LoadXml("<root />");
                            }
                            catch (Exception ex)
                            {
                                strError = this.ItemName+"��¼ '" + info.RecPath + "' XMLװ�ؽ���DOMʱ��������: " + ex.Message;
                                goto ERROR1;
                            }

                            /*
                            info.ItemBarcode = DomUtil.GetElementText(domExist.DocumentElement,
                                "barcode");
                             * */

                            // �۲��Ѿ����ڵļ�¼�Ƿ�����ͨ��Ϣ
                            // return:
                            //      -1  ����
                            //      0   û��
                            //      1   �С�������Ϣ��strError��
                            int nRet = this.HasCirculationInfo(domExist, out strError);
                            if (nRet == -1)
                                goto ERROR1;
                            if (nRet == 1)
                            {
                                strError = "��ɾ����"+this.ItemName+"��¼ '" + info.RecPath + "' ��"+strError+"(����������ܲ�������һ��)������ɾ����";
                                goto ERROR1;
                            }

                            info.OldTimestamp = output_timestamp;
                            goto REDO_DELETE;
                        }

                        strError = "ɾ��"+this.ItemName+"��¼ '" + info.RecPath + "' ʱ��������: " + strError;
                        goto ERROR1;
                    }
                }
                finally
                {
                    // this.EntityLocks.UnlockForWrite(info.ItemBarcode);
                }

                // ��������־DOM��
                if (domOperLog != null)
                {
                    Debug.Assert(root != null, "");

                    XmlNode node = domOperLog.CreateElement("record");
                    root.AppendChild(node);

                    DomUtil.SetAttr(node, "recPath", info.RecPath);
                }

                nDeletedCount++;
            }


            return nDeletedCount;
        ERROR1:
            return -1;
        }

        // ɾ������ͬһ��Ŀ��¼��ȫ��ʵ���¼
        // ���Ǽ�����ɾ��һ�ν��еİ汾
        // return:
        //      -1  error
        //      0   û���ҵ�������Ŀ��¼���κ�ʵ���¼�����Ҳ���޴�ɾ��
        //      >0  ʵ��ɾ����ʵ���¼��
        public int DeleteBiblioChildItems(RmsChannelCollection Channels,
            string strBiblioRecPath,
            XmlDocument domOperLog,
            out string strError)
        {
            strError = "";

            RmsChannel channel = Channels.GetChannel(this.App.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }


            List<DeleteEntityInfo> entityinfos = null;
            long lHitCount = 0;

            int nRet = SearchChildItems(channel,
                strBiblioRecPath,
                "check_circulation_info", // ��DeleteEntityInfo�ṹ��*��*����OldRecord����
                out lHitCount,
                out entityinfos,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (entityinfos == null || entityinfos.Count == 0)
                return 0;

            nRet = DeleteBiblioChildItems(Channels,
                entityinfos,
                domOperLog,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            return 0;
        ERROR1:
            return -1;
        }

#if NOOOOOOOOOOOOOOOOOOOOOOOOOOOO

        // ���������Ϣ�����ָ�ʽ
        // parameters:
        //      strIndex  ��š���������£�����ʹ��"@path:"�����Ķ�����¼·��(ֻ��Ҫ������id��������)��Ϊ������ڡ�
        //      strBiblioRecPath    ָ����Ŀ��¼·��
        //      strResultType   ָ����Ҫ��strResult�����з��ص����ݸ�ʽ��Ϊ"xml" "html"֮һ��
        //                      ���Ϊ�գ����ʾstrResult�����в������κ����ݡ������������Ϊʲôֵ��strItemRecPath�ж��ط��ز��¼·��(��������˵Ļ�)
        //      strItemRecPath  ���ز��¼·��������Ϊ���ż�����б��������·��
        //      strBiblioType   ָ����Ҫ��strBiblio�����з��ص����ݸ�ʽ��Ϊ"xml" "html"֮һ��
        //                      ���Ϊ�գ����ʾstrBiblio�����в������κ����ݡ�
        //      strOutputBiblioRecPath  �������Ŀ��¼·������strIndex�ĵ�һ�ַ�Ϊ'@'ʱ��strBiblioRecPath����Ϊ�գ��������غ�strOutputBiblioRecPath�л������������Ŀ��¼·��
        // return:
        // Result.Value -1���� 0û���ҵ� 1�ҵ� >1���ж���1��
        public Result GetCommentInfo(
            List<string> locateParam,
            /*
            string strIndex,
            string strBiblioRecPath,
             * */
            string strResultType,
            out string strResult,
            out string strItemRecPath,
            out byte[] item_timestamp,
            string strBiblioType,
            out string strBiblio,
            out string strOutputBiblioRecPath)
        {
            strResult = "";
            strBiblio = "";
            strItemRecPath = "";
            item_timestamp = null;
            strOutputBiblioRecPath = "";

            Result result = new Result();

            int nRet = 0;
            long lRet = 0;

            string strXml = "";
            string strError = "";

            nRet = this.GetCommandItemRecPath(locateParam,
                out strItemRecPath,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (nRet == 1)
            {
                // ������״̬��ֱ�ӷ��ؼ�¼
                string strMetaData = "";
                string strTempOutputPath = "";

                RmsChannel channel = sessioninfo.Channels.GetChannel(app.WsUrl);
                if (channel == null)
                {
                    strError = "get channel error";
                    goto ERROR1;
                }

                lRet = channel.GetRes(strItemRecPath,
                    out strXml,
                    out strMetaData,
                    out item_timestamp,
                    out strTempOutputPath,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                // �������¼<parent>Ԫ����ȡ����Ŀ��¼��id��Ȼ��ƴװ����Ŀ��¼·������strOutputBiblioRecPath
                XmlDocument dom = new XmlDocument();
                try
                {
                    if (String.IsNullOrEmpty(strXml) == true)
                        dom.LoadXml("<root />");
                    else
                        dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "��¼ " + strItemRecPath + " ��XMLװ��DOMʱ����: " + ex.Message;
                    goto ERROR1;
                }

                string strItemDbName = ResPath.GetDbName(strItemRecPath);

                // ���ݶ�������, �ҵ���Ӧ����Ŀ����
                // return:
                //      -1  ����
                //      0   û���ҵ�
                //      1   �ҵ�
                nRet = this.GetBiblioDbName(strItemDbName,
                    out strBiblioDbName,
                    out strError);
                if (nRet == -1 || nRet == 0)
                    goto ERROR1;

                strRootID = DomUtil.GetElementText(dom.DocumentElement,
                    "root");
                if (String.IsNullOrEmpty(strRootID) == true)
                {
                    strRootID = DomUtil.GetElementText(dom.DocumentElement,
                        "parent");
                    strError = this.ItemName+"��¼ " + strItemRecPath + " ��û��<root>��<parent>Ԫ��ֵ������޷���λ���������Ŀ��¼";
                    goto ERROR1;
                }
                strBiblioRecPath = strBiblioDbName + "/" + strRootID;
                strOutputBiblioRecPath = strBiblioRecPath;

                result.ErrorInfo = "";
                result.Value = 1;
            }

            string strBiblioRecPath = locateParam[0];

            string strBiblioDbName = "";
            string strCommentDbName = "";
            string strRootID = "";

            {
                /*
                strOutputBiblioRecPath = strBiblioRecPath;

                strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
                // ������Ŀ����, �ҵ���Ӧ���ڿ���
                // return:
                //      -1  ����
                //      0   û���ҵ�(��Ŀ��)
                //      1   �ҵ�
                nRet = this.GetItemDbName(strBiblioDbName,
                    out strCommentDbName,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet == 0)
                {
                    strError = "��Ŀ�� '" + strBiblioDbName + "' û���ҵ�";
                    goto ERROR1;
                }

                strRootID = ResPath.GetRecordId(strBiblioRecPath);
                 * */

                List<string> PathList = null;

                nRet = this.GetItemRecXml(
                        sessioninfo.Channels,
                        locateParam,
                        out strXml,
                        100,
                        out PathList,
                        out item_timestamp,
                        out strError);

                if (nRet == 0)
                {
                    result.Value = 0;
                    result.ErrorInfo = "û���ҵ�";
                    result.ErrorCode = ErrorCode.NotFound;
                    return result;
                }

                if (nRet == -1)
                    goto ERROR1;

                /*
                Debug.Assert(PathList != null, "");
                // ����·���ַ��������ż��
                string[] paths = new string[PathList.Count];
                PathList.CopyTo(paths);

                strOrderRecPath = String.Join(",", paths);
                 * */
                strItemRecPath = StringUtil.MakePathList(PathList);

                result.ErrorInfo = strError;
                result.Value = nRet;    // ���ܻ����1��
            }



            // ����Ҫͬʱȡ���ּ�¼
            if (String.IsNullOrEmpty(strBiblioType) == false)
            {
                string strBiblioXml = "";

                if (String.Compare(strBiblioType, "recpath", true) == 0)
                {
                    // ���������Ҫ�����Ŀ��¼recpath������Ҫ�����Ŀ��¼
                    goto DOORDER;
                }

                RmsChannel channel = sessioninfo.Channels.GetChannel(app.WsUrl);
                if (channel == null)
                {
                    strError = "channel == null";
                    goto ERROR1;
                }
                string strMetaData = "";
                byte[] timestamp = null;
                string strTempOutputPath = "";
                lRet = channel.GetRes(strBiblioRecPath,
                    out strBiblioXml,
                    out strMetaData,
                    out timestamp,
                    out strTempOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    strError = "����ּ�¼ '" + strBiblioRecPath + "' ʱ����: " + strError;
                    goto ERROR1;
                }

                // ���ֻ��Ҫ�ּ�¼��XML��ʽ
                if (String.Compare(strBiblioType, "xml", true) == 0)
                {
                    strBiblio = strBiblioXml;
                    goto DOORDER;
                }


                // ��Ҫ���ں�ӳ������ļ�
                string strLocalPath = "";

                if (String.Compare(strBiblioType, "html", true) == 0)
                {
                    nRet = app.MapKernelScriptFile(
                        sessioninfo,
                        strBiblioDbName,
                        "./cfgs/loan_biblio.fltx",
                        out strLocalPath,
                        out strError);
                }
                else if (String.Compare(strBiblioType, "text", true) == 0)
                {
                    nRet = app.MapKernelScriptFile(
                        sessioninfo,
                        strBiblioDbName,
                        "./cfgs/loan_biblio_text.fltx",
                        out strLocalPath,
                        out strError);
                }
                else
                {
                    strError = "����ʶ���strBiblioType���� '" + strBiblioType + "'";
                    goto ERROR1;
                }

                if (nRet == -1)
                    goto ERROR1;

                // ���ּ�¼���ݴ�XML��ʽת��ΪHTML��ʽ
                string strFilterFileName = strLocalPath;    // app.CfgDir + "\\biblio.fltx";
                nRet = app.ConvertBiblioXmlToHtml(
                        strFilterFileName,
                        strBiblioXml,
                        strBiblioRecPath,
                        out strBiblio,
                        out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

        DOORDER:
            // ȡ�ö�����Ϣ
            if (String.IsNullOrEmpty(strResultType) == true
                || String.Compare(strResultType, "recpath", true) == 0)
            {
                strResult = ""; // �������κν��
            }
            else if (String.Compare(strResultType, "xml", true) == 0)
            {
                strResult = strXml;
            }
            else if (String.Compare(strResultType, "html", true) == 0)
            {
                // ��������¼���ݴ�XML��ʽת��ΪHTML��ʽ
                nRet = app.ConvertItemXmlToHtml(
                app.CfgDir + "\\orderxml2html.cs",
                app.CfgDir + "\\orderxml2html.cs.ref",
                    strXml,
                    out strResult,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            else if (String.Compare(strResultType, "text", true) == 0)
            {
                // ��������¼���ݴ�XML��ʽת��Ϊtext��ʽ
                nRet = app.ConvertItemXmlToHtml(
                    app.CfgDir + "\\orderxml2text.cs",
                    app.CfgDir + "\\orderxml2text.cs.ref",
                    strXml,
                    out strResult,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            else
            {
                strError = "δ֪�Ķ�����¼������� '" + strResultType + "'";
                goto ERROR1;
            }

            return result;
        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }
#endif
        // 2015/1/28
        public virtual int BuildLocateParam(// string strBiblioRecPath,
string strRefID,
out List<string> locateParam,
out string strError)
        {
            strError = "��������δʵ�� BuildLocateParam";
            locateParam = null;
            return -1;
        }

        // ����������е������¼·��
        // return:
        //      -1  error
        //      0   ����������
        //      1   ��������
        public virtual int GetCommandItemRecPath(
            List<string> locateParam,
            out string strItemRecPath,
            out string strError)
        {
            throw new Exception("GetCommandItemRecPath() û��ʵ��");
        }

        // �����������е������¼·��
        // return:
        //      -1  error
        //      0   ����������
        //      1   ��������
        public int ParseCommandItemRecPath(
            string strCommandLine,
            out string strItemRecPath,
            out string strError)
        {
            strError = "";
            strItemRecPath = "";

            // ����״̬
            if (strCommandLine[0] == '@')
            {
                // ��������¼��ͨ�������¼·��

                string strLead = "@path:";
                if (strCommandLine.Length <= strLead.Length)
                {
                    strError = "����ļ����ʸ�ʽ: '" + strCommandLine + "'";
                    return -1;
                }

                string strPart = strCommandLine.Substring(0, strLead.Length);
                if (strPart != strLead)
                {
                    strError = "��֧�ֵļ����ʸ�ʽ: '" + strCommandLine + "'��Ŀǰ��֧��'@path:'�����ļ�����";
                    return -1;
                }

                strItemRecPath = strCommandLine.Substring(strLead.Length);

                string strItemDbName = ResPath.GetDbName(strItemRecPath);
                // ��Ҫ���һ�����ݿ����Ƿ���������������֮��
                if (this.IsItemDbName(strItemDbName) == false)
                {
                    strError = this.ItemName + "��¼·�� '" + strItemRecPath + "' �е����ݿ��� '" + strItemDbName + "' �������õ�" + this.ItemName + "����֮��";
                    return -1;
                }

                return 1;   // ������״̬
            }

            return 0;   // ��������״̬
        }

        // �۲�һ���ݲط����ַ����������Ƿ��ڵ�ǰ�û���Ͻ��Χ��
        // return:
        //      -1  ����
        //      0   ������Ͻ��Χ��strError���н���
        //      1   �ڹ�Ͻ��Χ��
        public static int DistributeInControlled(string strDistribute,
            string strLibraryCodeList,
            out string strError)
        {
            strError = "";

            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == true)
                return 1;

            LocationCollection locations = new LocationCollection();
            int nRet = locations.Build(strDistribute, out strError);
            if (nRet == -1)
            {
                strError = "�ݲط����ַ��� '" + strDistribute + "' ��ʽ����ȷ";
                return -1;
            }

            foreach (Location location in locations)
            {
                // �յĹݲصص㱻��Ϊ���ڷֹ��û���Ͻ��Χ��
                if (string.IsNullOrEmpty(location.Name) == true)
                {
                    strError = "�ݴ��� '' ���ڷ�Χ '" + strLibraryCodeList + "' ��";
                    return 0;
                }

                string strLibraryCode = "";
                string strPureName = "";

                // ����
                LibraryApplication.ParseCalendarName(location.Name,
            out strLibraryCode,
            out strPureName);

                if (StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                {
                    strError = "�ݴ��� '" + strLibraryCode + "' ���ڷ�Χ '" + strLibraryCodeList + "' ��";
                    return 0;
                }
            }

            return 1;
        }
    }
}
