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
    /// �������Ǻ��ڿ�ҵ��(��)��صĴ���
    /// </summary>
    public partial class LibraryApplication
    {
#if NOOOOOOOOOOOO
        // ����ڿ���¼
        // �������ɻ�ó���1�����ϵ�·��
        // parameters:
        //      timestamp   �������еĵ�һ����timestamp
        // return:
        //      -1  error
        //      0   not found
        //      1   ����1��
        //      >1  ���ж���1��
        public int GetIssueRecXml(
            RmsChannelCollection channels,
            string strIssueDbName,
            string strParentID,
            string strPublishTime,
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

            LibraryApplication app = this;

            // �������ʽ
            // �������ʽ
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

            RmsChannel channel = channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            long lRet = channel.DoSearch(strQueryXml,
                "default",
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = "������� '" + strPublishTime + "' û���ҵ�";
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

            lRet = channel.GetRes(aPath[0],
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

        // ɾ���ڼ�¼�Ĳ���
        int DoIssueOperDelete(
            SessionInfo sessioninfo,
            RmsChannel channel,
            IssueInfo info,
            string strIssueDbName,
            string strParentID,
            string strOldPublishTime,
            string strNewPublishTime,   // TODO: �������Ƿ���Էϳ�?
            XmlDocument domOldRec,
            ref XmlDocument domOperLog,
            ref List<IssueInfo> ErrorInfos)
        {
            int nRedoCount = 0;
            IssueInfo error = null;
            int nRet = 0;
            long lRet = 0;
            string strError = "";

            // ���newrecpathΪ�յ���oldrecpath��ֵ������oldrecpath��ֵ
            // 2007/10/23
            if (String.IsNullOrEmpty(info.NewRecPath) == true)
            {
                if (String.IsNullOrEmpty(info.OldRecPath) == false)
                    info.NewRecPath = info.OldRecPath;
            }


            // �����¼·��Ϊ��, ���Ȼ�ü�¼·��
            if (String.IsNullOrEmpty(info.NewRecPath) == true)
            {
                List<string> aPath = null;

                if (String.IsNullOrEmpty(strOldPublishTime) == true)
                {
                    strError = "info.OldRecord�е�<publishTime>Ԫ���еĳ���ʱ�䣬��info.RecPath����ֵ������ͬʱΪ�ա�";
                    goto ERROR1;
                }

                // ������ֻ�������, ������ü�¼��
                // return:
                //      -1  error
                //      ����    ���м�¼����(������nMax�涨�ļ���)
                nRet = this.SearchIssueRecDup(
                    sessioninfo.Channels,
                    strIssueDbName,
                    strParentID,
                    strOldPublishTime,
                    100,
                    out aPath,
                    out strError);
                if (nRet == -1)
                {
                    strError = "ɾ�������г���ʱ����ؽ׶η�������:" + strError;
                    goto ERROR1;
                }


                if (nRet == 0)
                {
                    error = new IssueInfo(info);
                    error.ErrorInfo = "����¼IDΪ '"+strParentID+"', + ����ʱ��Ϊ '" + strOldPublishTime + "' ���ڼ�¼�Ѳ�����";
                    error.ErrorCode = ErrorCodeValue.NotFound;
                    ErrorInfos.Add(error);
                    return -1;
                }


                if (nRet > 1)
                {
                    string[] pathlist = new string[aPath.Count];
                    aPath.CopyTo(pathlist);

                    // ��ɾ�������У������ظ����Ǻ�ƽ�������顣ֻҪ
                    // info.OldRecPath�ܹ�������ָ��Ҫɾ������һ�����Ϳ���ִ��ɾ��
                    if (String.IsNullOrEmpty(info.OldRecPath) == false)
                    {
                        if (aPath.IndexOf(info.OldRecPath) == -1)
                        {
                            strError = "����ʱ�� '" + strOldPublishTime + "' �Ѿ������ж����ڼ�¼ʹ����: " + String.Join(",", pathlist) + "'������������info.OldRecPath��ָ��·�� '" + info.OldRecPath + "'��";
                            goto ERROR1;
                        }
                        info.NewRecPath = info.OldRecPath;
                    }
                    else
                    {

                        strError = "����ʱ�� '" + strOldPublishTime + "' �Ѿ������ж����ڼ�¼ʹ����: " + String.Join(",", pathlist) + "'������һ�����ص�ϵͳ���ϣ��뾡��֪ͨϵͳ����Ա����";
                        goto ERROR1;
                    }
                }
                else
                {
                    Debug.Assert(nRet == 1, "");

                    info.NewRecPath = aPath[0];
                }

                ///

                /*

                if (nRet > 1)
                {
                    string[] pathlist = new string[aPath.Count];
                    aPath.CopyTo(pathlist);

                    strError = "����ʱ�� '" + strOldPublishTime + "' �Ѿ������ж����ڼ�¼ʹ����: " + String.Join(",", pathlist) + "'������һ�����ص�ϵͳ���ϣ��뾡��֪ͨϵͳ����Ա����";
                    goto ERROR1;
                }

                info.NewRecPath = aPath[0];
                 * */
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
                    error = new IssueInfo(info);
                    error.ErrorInfo = "����ʱ��Ϊ '" + strOldPublishTime + "' ���ڼ�¼ '" + info.NewRecPath + "' �Ѳ�����";
                    error.ErrorCode = channel.OriginErrorCode;
                    ErrorInfos.Add(error);
                    return -1;
                }
                else
                {
                    error = new IssueInfo(info);
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

            // �۲��Ѿ����ڵļ�¼�У�����ʱ���Ƿ��strOldPublishTimeһ��
            if (String.IsNullOrEmpty(strOldPublishTime) == false)
            {
                string strExistingPublishTime = DomUtil.GetElementText(domExist.DocumentElement,
                    "publishTime");
                if (strExistingPublishTime != strOldPublishTime)
                {
                    strError = "·��Ϊ '" + info.NewRecPath + "' ���ڼ�¼��<publishTime>Ԫ���еĳ���ʱ�� '" + strExistingPublishTime + "' ��strOldXml��<publishTime>Ԫ���еĳ���ʱ�� '" + strOldPublishTime + "' ��һ�¡��ܾ�ɾ��(�������ɾ���������ɲ�����ɾ���˱���ڼ�¼��Σ��)��";
                    goto ERROR1;
                }
            }

            /*
            // �۲��Ѿ����ڵļ�¼�Ƿ�����ͨ��Ϣ
            if (IsIssueHasCirculationInfo(domExist) == true)
            {
                strError = "��ɾ�����ڼ�¼ '" + info.NewRecPath + "' �а�������ͨ��Ϣ������ɾ����";
                goto ERROR1;
            }*/

            // �Ƚ�ʱ���
            // �۲�ʱ����Ƿ����仯
            nRet = ByteArray.Compare(info.OldTimestamp, exist_timestamp);
            if (nRet != 0)
            {
                // ���ǰ�˸����˾ɼ�¼�����кͿ��м�¼���бȽϵĻ���
                if (String.IsNullOrEmpty(info.OldRecord) == false)
                {
                    // �Ƚ�������¼, ��������Ҫ����Ϣ�йص��ֶ��Ƿ����˱仯
                    // return:
                    //      0   û�б仯
                    //      1   �б仯
                    nRet = IsIssueInfoChanged(domExist,
                        domOldRec);
                    if (nRet == 1)
                    {

                        error = new IssueInfo(info);
                        error.NewTimestamp = exist_timestamp;   // ��ǰ��֪�����м�¼ʵ���Ϸ������仯
                        error.ErrorInfo = "���ݿ��м���ɾ�����ڼ�¼�Ѿ������˱仯��������װ�ء���ϸ�˶Ժ�����ɾ����";
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

                error = new IssueInfo(info);
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

                XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                    "oldRecord", strExistingXml);
                DomUtil.SetAttr(node, "recPath", info.NewRecPath);


                // ���ɾ���ɹ����򲻱�Ҫ�������з��ر�ʾ�ɹ�����ϢԪ����
            }

            return 0;
        ERROR1:
            error = new IssueInfo(info);
            error.ErrorInfo = strError;
            error.ErrorCode = ErrorCodeValue.CommonError;
            ErrorInfos.Add(error);
            return -1;
        }

        // ִ��SetIssues API�е�"move"����
        // 1) �����ɹ���, NewRecord����ʵ�ʱ�����¼�¼��NewTimeStampΪ�µ�ʱ���
        // 2) �������TimeStampMismatch����OldRecord���п��з����仯��ġ�ԭ��¼����OldTimeStamp����ʱ���
        // return:
        //      -1  ����
        //      0   �ɹ�
        int DoIssueOperMove(
            RmsChannel channel,
            IssueInfo info,
            ref XmlDocument domOperLog,
            ref List<IssueInfo> ErrorInfos)
        {
            // int nRedoCount = 0;
            IssueInfo error = null;
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
            // ��Ϊ�������move���и���Ŀ���¼���ܣ��򱻸��ǵļ�¼��Ԥɾ�����������ڽ�����һ��ע���������Ч�ò����ԣ���ǰ�˲�����Ա׼ȷ�ж���̬���Ժ������(���ҿ�������ע����Ҫ����Ĳ���Ȩ��)������
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
                        error = new IssueInfo(info);
                        error.ErrorInfo = "move������������, �ڶ��뼴�����ǵ�Ŀ��λ�� '" + info.NewRecPath + "' ԭ�м�¼�׶�:" + strError;
                        error.ErrorCode = channel.OriginErrorCode;
                        ErrorInfos.Add(error);
                        return -1;
                    }
                }
                else
                {
                    // �����¼���ڣ���Ŀǰ�����������Ĳ���
                    strError = "�ƶ�(move)�������ܾ�����Ϊ�ڼ������ǵ�Ŀ��λ�� '" + info.NewRecPath + "' �Ѿ������ڼ�¼������ɾ��(delete)������¼���ٽ����ƶ�(move)����";
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
                    error = new IssueInfo(info);
                    error.ErrorInfo = "���������������, �ڶ������ԭ��Դ��¼(·����info.OldRecPath) '" + info.OldRecPath + "' �׶�:" + strError;
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
                // ��Ҫ��info.OldRecord��strExistXml���бȽϣ��������ڼǵ��йص�Ԫ�أ�Ҫ��Ԫ�أ�ֵ�Ƿ����˱仯��
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

                // �Ƚ�������¼, �������ڼǵ��йص��ֶ��Ƿ����˱仯
                // return:
                //      0   û�б仯
                //      1   �б仯
                nRet = IsIssueInfoChanged(domOld,
                    domSourceExist);
                if (nRet == 1)
                {
                    error = new IssueInfo(info);
                    // ������Ϣ��, �������޸Ĺ���ԭ��¼����ʱ���
                    error.OldRecord = strExistSourceXml;
                    error.OldTimestamp = exist_source_timestamp;

                    if (bExist == false)
                        error.ErrorInfo = "���������������: ���ݿ��е�ԭ��¼�ѱ�ɾ����";
                    else
                        error.ErrorInfo = "���������������: ���ݿ��е�ԭ��¼�ѷ������޸�";
                    error.ErrorCode = ErrorCodeValue.TimestampMismatch;
                    ErrorInfos.Add(error);
                    return -1;
                }

                // exist_source_timestamp��ʱ�Ѿ���ӳ�˿��б��޸ĺ�ļ�¼��ʱ���
            }


            // �ϲ��¾ɼ�¼
            string strNewXml = "";
            nRet = MergeTwoIssueXml(domSourceExist,
                domNew,
                out strNewXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;


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
                strError = "WriteIssues()API move�����У��ڼ�¼ '" + info.OldRecPath + "' �Ѿ����ɹ��ƶ��� '" + strTargetPath + "' ������д��������ʱ��������: " + strError;

                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                {
                    // �����з�������
                    // ��ΪԴ�Ѿ��ƶ�������ܸ���
                }

                // ����д�������־���ɡ�û��Undo
                this.WriteErrorLog(strError);

                /*
                if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
                {
                    if (nRedoCount > 10)
                    {
                        strError = "��������(DoCopyRecord())������ʱ�����ͻ, ����10��������Ȼʧ��";
                        goto ERROR1;
                    }
                    // ����ʱ�����ƥ��
                    // �ظ�������ȡ�Ѵ��ڼ�¼\�ȽϵĹ���
                    nRedoCount++;
                    goto REDOLOAD;
                }*/


                error = new IssueInfo(info);
                error.ErrorInfo = "���������������:" + strError;
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
                error = new IssueInfo(info);
                error.NewTimestamp = output_timestamp;
                error.NewRecord = strNewXml;

                error.ErrorInfo = "��������ɹ���NewRecPath�з�����ʵ�ʱ����·��, NewTimeStamp�з������µ�ʱ�����NewRecord�з�����ʵ�ʱ�����¼�¼(���ܺ��ύ���¼�¼���в���)��";
                error.ErrorCode = ErrorCodeValue.NoError;
                ErrorInfos.Add(error);
            }

            return 0;

        ERROR1:
            error = new IssueInfo(info);
            error.ErrorInfo = strError;
            error.ErrorCode = ErrorCodeValue.CommonError;
            ErrorInfos.Add(error);
            return -1;
        }

        // ִ��SetIssues API�е�"change"����
        // 1) �����ɹ���, NewRecord����ʵ�ʱ�����¼�¼��NewTimeStampΪ�µ�ʱ���
        // 2) �������TimeStampMismatch����OldRecord���п��з����仯��ġ�ԭ��¼����OldTimeStamp����ʱ���
        // return:
        //      -1  ����
        //      0   �ɹ�
        static int DoIssueOperChange(
            RmsChannel channel,
            IssueInfo info,
            ref XmlDocument domOperLog,
            ref List<IssueInfo> ErrorInfos)
        {
            int nRedoCount = 0;
            IssueInfo error = null;
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
                    error = new IssueInfo(info);
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
                // ��Ҫ��info.OldRecord��strExistXml���бȽϣ��������ڼǵ��йص�Ԫ�أ�Ҫ��Ԫ�أ�ֵ�Ƿ����˱仯��
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

                // �Ƚ�������¼, �������ڼǵ��йص��ֶ��Ƿ����˱仯
                // return:
                //      0   û�б仯
                //      1   �б仯
                nRet = IsIssueInfoChanged(domOld,
                    domExist);
                if (nRet == 1)
                {
                    error = new IssueInfo(info);
                    // ������Ϣ��, �������޸Ĺ���ԭ��¼����ʱ���
                    error.OldRecord = strExistXml;
                    error.OldTimestamp = exist_timestamp;

                    if (bExist == false)
                        error.ErrorInfo = "���������������: ���ݿ��е�ԭ��¼�ѱ�ɾ����";
                    else
                        error.ErrorInfo = "���������������: ���ݿ��е�ԭ��¼�ѷ������޸�";
                    error.ErrorCode = ErrorCodeValue.TimestampMismatch;
                    ErrorInfos.Add(error);
                    return -1;
                }

                // exist_timestamp��ʱ�Ѿ���ӳ�˿��б��޸ĺ�ļ�¼��ʱ���
            }


            // �ϲ��¾ɼ�¼
            string strNewXml = "";
            nRet = MergeTwoIssueXml(domExist,
                domNew,
                out strNewXml,
                out strError);
            if (nRet == -1)
                goto ERROR1;


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

                error = new IssueInfo(info);
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
                error = new IssueInfo(info);
                error.NewTimestamp = output_timestamp;
                error.NewRecord = strNewXml;

                error.ErrorInfo = "��������ɹ���NewTimeStamp�з������µ�ʱ�����NewRecord�з�����ʵ�ʱ�����¼�¼(���ܺ��ύ���¼�¼���в���)��";
                error.ErrorCode = ErrorCodeValue.NoError;
                ErrorInfos.Add(error);
            }

            return 0;

        ERROR1:
            error = new IssueInfo(info);
            error.ErrorInfo = strError;
            error.ErrorCode = ErrorCodeValue.CommonError;
            ErrorInfos.Add(error);
            return -1;
        }

        // <DoIssueOperChange()���¼�����>
        // �Ƚ�������¼, �����ͼǵ��йص��ֶ��Ƿ����˱仯
        // return:
        //      0   û�б仯
        //      1   �б仯
        static int IsIssueInfoChanged(XmlDocument dom1,
            XmlDocument dom2)
        {
            // Ҫ��Ԫ�����б�
            string[] element_names = new string[] {
                "parent",
                "publishTime",
                "no",   // ���ں�
                "volume",
                "price",
                "comment",
                "batchNo"
            };

            for (int i = 0; i < element_names.Length; i++)
            {
                string strText1 = DomUtil.GetElementText(dom1.DocumentElement,
                    element_names[i]);
                string strText2 = DomUtil.GetElementText(dom2.DocumentElement,
                    element_names[i]);

                if (strText1 != strText2)
                    return 1;
            }

            return 0;
        }

        // <DoIssueOperChange()���¼�����>
        // �ϲ��¾ɼ�¼
        static int MergeTwoIssueXml(XmlDocument domExist,
            XmlDocument domNew,
            out string strMergedXml,
            out string strError)
        {
            strMergedXml = "";
            strError = "";

            // �㷨��Ҫ����, ��"�¼�¼"�е�Ҫ���ֶ�, ���ǵ�"�Ѵ��ڼ�¼"��

            // Ҫ��Ԫ�����б�
            string[] element_names = new string[] {
                "parent",
                "publishTime",
                "no",   // ���ں�
                "volume",
                "price",
                "comment",
                "batchNo"
            };

            for (int i = 0; i < element_names.Length; i++)
            {
                string strTextNew = DomUtil.GetElementText(domNew.DocumentElement,
                    element_names[i]);

                DomUtil.SetElementText(domExist.DocumentElement,
                    element_names[i], strTextNew);
            }

            strMergedXml = domExist.OuterXml;

            return 0;
        }


        // ������ʺϱ�������ڼ�¼
        static int BuildNewIssueRecord(string strOriginXml,
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strOriginXml);
            }
            catch (Exception ex)
            {
                strError = "װ��strOriginXml��DOMʱ����: " + ex.Message;
                return -1;
            }

            /*
            // ��ͨԪ�����б�
            string[] element_names = new string[] {
                "borrower",
                "borrowDate",
                "borrowPeriod",
                "borrowHistory",
            };

            for (int i = 0; i < element_names.Length; i++)
            {
                DomUtil.SetElementText(dom.DocumentElement,
                    element_names[i], "");
            }
             * */

            strXml = dom.OuterXml;

            return 0;
        }

        // ���� ����¼ID/����ʱ�� ���ڿ���в���
        // ������ֻ�������, ������ü�¼��
        // return:
        //      -1  error
        //      ����    ���м�¼����(������nMax�涨�ļ���)
        public int SearchIssueRecDup(
            RmsChannelCollection channels,
            string strIssueDbName,
            string strParentID,
            string strPublishTime,
            int nMax,
            out List<string> aPath,
            out string strError)
        {
            strError = "";
            aPath = null;

            LibraryApplication app = this;

            // �������ʽ
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

            RmsChannel channel = channels.GetChannel(app.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                return -1;
            }

            long lRet = channel.DoSearch(strQueryXml,
                "default",
                out strError);
            if (lRet == -1)
                goto ERROR1;

            // not found
            if (lRet == 0)
            {
                strError = "����ʱ��Ϊ '" + strPublishTime + "' ���� ����¼Ϊ '"+strParentID+"' �ļ�¼û���ҵ�";
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


        // ���¾��ڼ�¼�а����ĳ���ʱ����бȽ�, �����Ƿ����˱仯(��������Ҫ����)
        // ����ʱ�������<publishTime>Ԫ����
        // parameters:
        //      strOldPublishTime   ˳�㷵�ؾɼ�¼�еĳ���ʱ��
        //      strNewPublishTime   ˳�㷵���¼�¼�еĳ���ʱ��
        // return:
        //      -1  ����
        //      0   ���
        //      1   �����
        static int CompareTwoIssueNo(XmlDocument domOldRec,
            XmlDocument domNewRec,
            out string strOldPublishTime,
            out string strOldParentID,
            out string strNewPublishTime,
            out string strNewParentID,
            out string strError)
        {
            strError = "";

            strOldPublishTime = "";
            strNewPublishTime = "";

            strOldParentID = "";
            strNewParentID = "";

            strOldPublishTime = DomUtil.GetElementText(domOldRec.DocumentElement,
                "publishTime");

            strNewPublishTime = DomUtil.GetElementText(domNewRec.DocumentElement,
                "publishTime");

            strOldParentID = DomUtil.GetElementText(domOldRec.DocumentElement,
                "parent");

            strNewParentID = DomUtil.GetElementText(domNewRec.DocumentElement,
                "parent");

            if (strOldPublishTime != strNewPublishTime)
                return 1;   // �����

            return 0;   // ���
        }

        // TODO: ��Ҫ���޶��ڷ�Χ������
        // �������Ϣ
        // parameters:
        //      strBiblioRecPath    ��Ŀ��¼·����������������id����
        //      issues ���ص�����Ϣ����
        // Ȩ�ޣ���Ҫ��getissuesȨ��
        // return:
        //      Result.Value    -1���� 0û���ҵ� ���� ʵ���¼�ĸ���
        public Result GetIssues(
            SessionInfo sessioninfo,
            string strBiblioRecPath,
            out IssueInfo[] issues)
        {

            issues = null;

            Result result = new Result();

            // Ȩ���ַ���
            if (StringUtil.IsInList("getissues", sessioninfo.RightsOrigin) == false
        && StringUtil.IsInList("getissueinfo", sessioninfo.RightsOrigin) == false)
            {
                result.Value = -1;
                result.ErrorInfo = "�������Ϣ �������ܾ������߱�getissueinfo��getissuesȨ�ޡ�";
                result.ErrorCode = ErrorCode.AccessDenied;
                return result;
            }


            int nRet = 0;
            string strError = "";

            string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
            string strBiblioRecId = ResPath.GetRecordId(strBiblioRecPath);

            // �����Ŀ���Ӧ���ڿ���
            string strIssueDbName = "";
            nRet = this.GetIssueDbName(strBiblioDbName,
                 out strIssueDbName,
                 out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' û���ҵ�";
                goto ERROR1;
            }
            if (String.IsNullOrEmpty(strIssueDbName) == true)
            {
                strError = "��Ŀ���� '" +strBiblioDbName+ "' ��Ӧ���ڿ���û�ж���";
                goto ERROR1;
            }

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            // �����ڿ���ȫ���������ض�id�ļ�¼

            string strQueryXml = "<target list='"
                + StringUtil.GetXmlStringSimple(strIssueDbName + ":" + "����¼")       // 2007/9/14
                + "'><item><word>"
                + strBiblioRecId
                + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>" + "zh" + "</lang></target>";
            long lRet = channel.DoSearch(strQueryXml,
                "issues",
                out strError);
            if (lRet == -1)
                goto ERROR1;

            if (lRet == 0)
            {
                result.Value = 0;
                result.ErrorInfo = "û���ҵ�";
                return result;
            }

            int nResultCount = (int)lRet;

            if (nResultCount > 10000)
            {
                strError = "�����ڼ�¼�� " + nResultCount.ToString() + " ���� 10000, ��ʱ��֧��";
                goto ERROR1;
            }

            List<IssueInfo> issueinfos = new List<IssueInfo>();

            int nStart = 0;
            int nPerCount = 100;
            for (; ; )
            {
                List<string> aPath = null;
                lRet = channel.DoGetSearchResult(
                    "issues",
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
                    IssueInfo issueinfo = new IssueInfo();

                    if (lRet == -1)
                    {
                        issueinfo.OldRecPath = aPath[i];
                        issueinfo.ErrorCode = channel.OriginErrorCode;
                        issueinfo.ErrorInfo = channel.ErrorInfo;

                        issueinfo.OldRecord = "";
                        issueinfo.OldTimestamp = null;

                        issueinfo.NewRecPath = "";
                        issueinfo.NewRecord = "";
                        issueinfo.NewTimestamp = null;
                        issueinfo.Action = "";


                        goto CONTINUE;
                    }

                    issueinfo.OldRecPath = strOutputPath;
                    issueinfo.OldRecord = strXml;
                    issueinfo.OldTimestamp = timestamp;

                    issueinfo.NewRecPath = "";
                    issueinfo.NewRecord = "";
                    issueinfo.NewTimestamp = null;
                    issueinfo.Action = "";

                CONTINUE:
                    issueinfos.Add(issueinfo);
                }

                nStart += aPath.Count;
                if (nStart >= nResultCount)
                    break;
            }

            // �ҽӵ������
            issues = new IssueInfo[issueinfos.Count];
            for (int i = 0; i < issueinfos.Count; i++)
            {
                issues[i] = issueinfos[i];
            }

            result.Value = issues.Length;
            return result;

        ERROR1:
            result.Value = -1;
            result.ErrorInfo = strError;
            result.ErrorCode = ErrorCode.SystemError;
            return result;
        }


        // ����/��������Ϣ
        // parameters:
        //      strBiblioRecPath    ��Ŀ��¼·����������������id���֡�������������ȷ����Ŀ�⣬id���Ա�ʵ���¼��������<parent>Ԫ�����ݡ�������Ŀ������IssueInfo�е�NewRecPath�γ�ӳ�չ�ϵ����Ҫ��������Ƿ���ȷ��Ӧ
        //      issueinfos Ҫ�ύ�ĵ�����Ϣ����
        // Ȩ�ޣ���Ҫ��setissuesȨ��
        // �޸����: д���ڿ��еļ�¼, ��ȱ��<operator>��<operTime>�ֶ�
        public Result SetIssues(
            SessionInfo sessioninfo,
            string strBiblioRecPath,
            IssueInfo[] issueinfos,
            out IssueInfo[] errorinfos)
        {
            errorinfos = null;

            Result result = new Result();

            // Ȩ���ַ���
            if (StringUtil.IsInList("setissueinfo", sessioninfo.RightsOrigin) == false)
            {
                result.Value = -1;
                result.ErrorInfo = "��������Ϣ �������ܾ������߱�setissueinfoȨ�ޡ�";
                result.ErrorCode = ErrorCode.AccessDenied;
                return result;
            }


            int nRet = 0;
            long lRet = 0;
            string strError = "";

            string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
            string strBiblioRecId = ResPath.GetRecordId(strBiblioRecPath);

            // �����Ŀ���Ӧ���ڿ���
            string strIssueDbName = "";
            nRet = this.GetIssueDbName(strBiblioDbName,
                 out strIssueDbName,
                 out strError);
            if (nRet == -1)
                goto ERROR1;
            if (nRet == 0)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' û���ҵ�";
                goto ERROR1;
            }
            if (String.IsNullOrEmpty(strIssueDbName) == true)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' ��Ӧ���ڿ���û�ж���";
                goto ERROR1;
            }

            RmsChannel channel = sessioninfo.Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "get channel error";
                goto ERROR1;
            }

            byte[] output_timestamp = null;
            string strOutputPath = "";

            List<IssueInfo> ErrorInfos = new List<IssueInfo>();

            for (int i = 0; i < issueinfos.Length; i++)
            {
                IssueInfo info = issueinfos[i];

                // TODO: ������Ϊ"delete"ʱ���Ƿ��������ֻ����OldRecPath������������NewRecPath
                // ������������ã���Ҫ������Ϊһ�µġ�

                // ���·���еĿ�������
                if (String.IsNullOrEmpty(info.NewRecPath) == false)
                {
                    strError = "";


                    string strDbName = ResPath.GetDbName(info.NewRecPath);

                    if (String.IsNullOrEmpty(strDbName) == true)
                    {
                        strError = "NewRecPath�����ݿ�����ӦΪ��";
                    }

                    if (strDbName != strIssueDbName)
                    {
                        strError = "RecPath�����ݿ��� '" + strDbName + "' ����ȷ��ӦΪ '" + strIssueDbName + "'��(��Ϊ��Ŀ����Ϊ '" + strBiblioDbName + "'�����Ӧ���ڿ���ӦΪ '" + strIssueDbName + "' )";
                    }

                    if (strError != "")
                    {
                        IssueInfo error = new IssueInfo(info);
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

                    IssueInfo error = new IssueInfo(info);
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

                    IssueInfo error = new IssueInfo(info);
                    error.ErrorInfo = strError;
                    error.ErrorCode = ErrorCodeValue.CommonError;
                    ErrorInfos.Add(error);
                    continue;
                }

                string strOldPublishTime = "";
                string strNewPublishTime = "";

                string strOldParentID = "";
                string strNewParentID = "";

                // �Գ���ʱ�����?
                string strLockPublishTime = "";

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
                        nRet = CompareTwoIssueNo(domOldRec,
                            domNewRec,
                            out strOldPublishTime,
                            out strOldParentID,
                            out strNewPublishTime,
                            out strNewParentID,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "CompareTwoIssueNo() error : " + strError;
                            goto ERROR1;
                        }

                        if (info.Action == "new"
                            || info.Action == "change"
                            || info.Action == "move")
                            strLockPublishTime = strNewPublishTime;
                        else if (info.Action == "delete")
                        {
                            // ˳�����һЩ���
                            if (String.IsNullOrEmpty(strNewPublishTime) == false)
                            {
                                strError = "û�б�Ҫ��delete������IssueInfo��, ����NewRecord����...���෴��ע��һ��Ҫ��OldRecord�а�������ɾ����ԭ��¼";
                                goto ERROR1;
                            }
                            strLockPublishTime = strOldPublishTime;
                        }


                        // ����
                        if (String.IsNullOrEmpty(strLockPublishTime) == false)
                            this.EntityLocks.LockForWrite(strLockPublishTime);

                        // ���г���ʱ�����
                        // TODO: ���ص�ʱ��Ҫע�⣬�����������Ϊ��move�����������������info.OldRecPath�صģ���Ϊ��������ɾ��
                        if (nRet == 1   // �¾ɳ���ʱ�䲻�ȣ��Ų��ء����������������Ч�ʡ�
                            && (info.Action == "new"
                                || info.Action == "change"
                                || info.Action == "move")       // delete����������
                            && String.IsNullOrEmpty(strNewPublishTime) == false
                            )
                        {
                            string strParentID = strNewParentID;

                            if (String.IsNullOrEmpty(strParentID) == true)
                                strParentID = strOldParentID;

                            List<string> aPath = null;
                            // ���� ����¼ID+����ʱ�� ���ڿ���в���
                            // ������ֻ�������, ������ü�¼��
                            // return:
                            //      -1  error
                            //      ����    ���м�¼����(������nMax�涨�ļ���)
                            nRet = SearchIssueRecDup(
                                sessioninfo.Channels,
                                strIssueDbName,
                                strParentID,
                                strNewPublishTime,
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
                                string[] pathlist = new string[aPath.Count];
                                aPath.CopyTo(pathlist);

                                IssueInfo error = new IssueInfo(info);
                                error.ErrorInfo = "����ʱ�� '" + strNewPublishTime + "' �Ѿ��������ڼ�¼ʹ����: " + String.Join(",", pathlist);
                                error.ErrorCode = ErrorCodeValue.CommonError;
                                ErrorInfos.Add(error);
                                continue;
                            }
                        }
                    }

                    // ׼����־DOM
                    XmlDocument domOperLog = new XmlDocument();
                    domOperLog.LoadXml("<root />");
                    DomUtil.SetElementText(domOperLog.DocumentElement, "operation", "setIssue");

                    // ����һ������
                    if (info.Action == "new")
                    {
                        // ����¼�¼��·���е�id�����Ƿ���ȷ
                        // �������֣�ǰ���Ѿ�ͳһ������
                        strError = "";

                        if (String.IsNullOrEmpty(info.NewRecPath) == true)
                        {
                            info.NewRecPath = strIssueDbName + "/?";
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
                                IssueInfo error = new IssueInfo(info);
                                error.ErrorInfo = strError;
                                error.ErrorCode = ErrorCodeValue.CommonError;
                                ErrorInfos.Add(error);
                                continue;
                            }
                        }

                        // ������ʺϱ�������ڼ�¼
                        string strNewXml = "";
                        nRet = BuildNewIssueRecord(info.NewRecord,
                            out strNewXml,
                            out strError);
                        if (nRet == -1)
                        {
                            IssueInfo error = new IssueInfo(info);
                            error.ErrorInfo = strError;
                            error.ErrorCode = ErrorCodeValue.CommonError;
                            ErrorInfos.Add(error);
                            continue;
                        }

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
                            IssueInfo error = new IssueInfo(info);
                            error.NewTimestamp = output_timestamp;
                            error.ErrorInfo = "�����¼�¼�Ĳ�����������:" + strError;
                            error.ErrorCode = channel.OriginErrorCode;
                            ErrorInfos.Add(error);

                            domOperLog = null;  // ��ʾ����д����־
                        }
                        else // �ɹ�
                        {

                            DomUtil.SetElementText(domOperLog.DocumentElement, "action", "new");

                            // ������<oldRecord>Ԫ��

                            XmlNode node = DomUtil.SetElementText(domOperLog.DocumentElement,
                                "record", strNewXml);
                            DomUtil.SetAttr(node, "recPath", strOutputPath);

                            // �¼�¼����ɹ�����Ҫ������ϢԪ�ء���Ϊ��Ҫ�����µ�ʱ�����ʵ�ʱ���ļ�¼·��

                            IssueInfo error = new IssueInfo(info);
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
                        nRet = DoIssueOperChange(
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
                        nRet = DoIssueOperMove(
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
                        string strParentID = strNewParentID;

                        if (String.IsNullOrEmpty(strParentID) == true)
                            strParentID = strOldParentID;


                        // ɾ���ڼ�¼�Ĳ���
                        nRet = DoIssueOperDelete(
                            sessioninfo,
                            channel,
                            info,
                            strIssueDbName,
                            strParentID,
                            strOldPublishTime,
                            strNewPublishTime,
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
                        IssueInfo error = new IssueInfo(info);
                        error.ErrorInfo = "��֧�ֵĲ������� '" + info.Action + "'";
                        error.ErrorCode = ErrorCodeValue.CommonError;
                        ErrorInfos.Add(error);
                    }


                    // д����־
                    if (domOperLog != null)
                    {
                        string strOperTime = this.Clock.GetClock();
                        DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                            sessioninfo.UserID);   // ������
                        DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                            strOperTime);   // ����ʱ��

                        nRet = this.OperLog.WriteOperLog(domOperLog,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "SetIssues() API д����־ʱ��������: " + strError;
                            goto ERROR1;
                        }
                    }
                }
                finally
                {
                    if (String.IsNullOrEmpty(strLockPublishTime) == false)
                        this.EntityLocks.UnlockForWrite(strLockPublishTime);
                }

            }

            // ���Ƶ������
            errorinfos = new IssueInfo[ErrorInfos.Count];
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
#endif
    }

#if NOOOOOOOOOOOOOOOO
    // ����Ϣ
    public class IssueInfo
    {

        public string OldRecPath = "";  // ԭ��¼·��
        public string OldRecord = "";   // �ɼ�¼
        public byte[] OldTimestamp = null;  // �ɼ�¼��Ӧ��ʱ���

        public string NewRecPath = ""; // �¼�¼·��
        public string NewRecord = "";   // �¼�¼
        public byte[] NewTimestamp = null;  // �¼�¼��Ӧ��ʱ���

        public string Action = "";   // Ҫִ�еĲ���(getʱ��������) ֵΪnew change delete move 4��֮һ��changeҪ��OldRecPath��NewRecPathһ����move��Ҫ������һ������move�������г�������Ҫ��Ϊ����־ͳ�Ƶı�����
        public string ErrorInfo = "";   // ������Ϣ
        public ErrorCodeValue ErrorCode = ErrorCodeValue.NoError;   // �����루��ʾ���ں������͵Ĵ���

        public IssueInfo(IssueInfo info)
        {
            this.OldRecPath = info.OldRecPath;
            this.OldRecord = info.OldRecord;
            this.OldTimestamp = info.OldTimestamp;
            this.NewRecPath = info.NewRecPath;
            this.NewRecord = info.NewRecord;
            this.NewTimestamp = info.NewTimestamp;
            this.Action = info.Action;
            this.ErrorInfo = info.ErrorInfo;
            this.ErrorCode = info.ErrorCode;
        }

        public IssueInfo()
        {

        }
    }
#endif
}
