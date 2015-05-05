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
    /// ��ע���ݿ⡣�����������������
    /// 2008/12/8
    /// </summary>
    public class CommentItemDatabase : ItemDatabase
    {
        // Ҫ��Ԫ�����б�
        static string[] core_comment_element_names = new string[] {
                "parent",   // 
                "index",    // ���
                "state",    // ״̬
                "type",    // ����
                "title",    // ����
                "creator",  // ������
                "subject",
                "summary",
                "content", // ��������
                "createTime",   // ����ʱ��
                //"lastModified",    // ����޸�ʱ��
                //"lastModifier",    // ����޸���
                "follow",   // �����ӵ�(����)��¼ID
                "refID",    // �ο�ID
                "operations", // 
                "orderSuggestion",  // 2010/11/8
                // "libraryCode",  // 2012/10/3
        };

        static string[] readerchangeable_comment_element_names = new string[] {
                "title",    // ����
                "subject",
                "summary",
                "content", // ��������
        };

        // DoOperChange()��DoOperMove()���¼�����
        // �ϲ��¾ɼ�¼
        // return:
        //      -1  ����
        //      0   ��ȷ
        //      1   �в����޸�û�ж��֡�˵����strError��
        public override int MergeTwoItemXml(
            SessionInfo sessioninfo,
            XmlDocument domExist,
            XmlDocument domNew,
            out string strMergedXml,
            out string strError)
        {
            strMergedXml = "";
            strError = "";

            string[] element_table = core_comment_element_names;

            if (sessioninfo != null
                && sessioninfo.Account != null
                && sessioninfo.UserType == "reader")
                element_table = readerchangeable_comment_element_names;

            // �㷨��Ҫ����, ��"�¼�¼"�е�Ҫ���ֶ�, ���ǵ�"�Ѵ��ڼ�¼"��

            /*
            // Ҫ��Ԫ�����б�
            string[] element_names = new string[] {
                "parent",   // ����¼ID��Ҳ��������������һ����ע��¼��id
                "index",    // ���
                "state",    // ״̬
                "title",    // ����
                "creator",  // ������
                "createTime",   // ����ʱ��
                "lastModifyTime",    // ����޸�ʱ��
                "root",   // ����¼ID��Ҳ��������������Ŀ��¼ID
                "content", // ��������
            };*/

            for (int i = 0; i < element_table.Length; i++)
            {
                /*
                string strTextNew = DomUtil.GetElementText(domNew.DocumentElement,
                    core_comment_element_names[i]);

                DomUtil.SetElementText(domExist.DocumentElement,
                    core_comment_element_names[i], strTextNew);
                 * */
                string strTextNew = DomUtil.GetElementOuterXml(domNew.DocumentElement,
                    element_table[i]);

                DomUtil.SetElementOuterXml(domExist.DocumentElement,
                    element_table[i], strTextNew);

            }

            /*
            // 2012/10/3
            // ��ǰ�û�����Ͻ�Ĺݴ���
            DomUtil.SetElementText(domExist.DocumentElement,
                "libraryCode",
                sessioninfo.LibraryCodeList);
             * */
            // �޸��߲��ܸı�����Ĺݴ���

            strMergedXml = domExist.OuterXml;

            return 0;
        }



        // (�������������)
        // �Ƚ�������¼, ����������Ҫ����Ϣ�йص��ֶ��Ƿ����˱仯
        // return:
        //      0   û�б仯
        //      1   �б仯
        public override int IsItemInfoChanged(XmlDocument domExist,
            XmlDocument domOldRec)
        {
            for (int i = 0; i < core_comment_element_names.Length; i++)
            {
                string strText1 = DomUtil.GetElementOuterXml(domExist.DocumentElement,
                    core_comment_element_names[i]);
                string strText2 = DomUtil.GetElementOuterXml(domOldRec.DocumentElement,
                    core_comment_element_names[i]);

                if (strText1 != strText2)
                    return 1;
            }

            return 0;
        }

        public CommentItemDatabase(LibraryApplication app) : base(app)
        {
        }

#if NO
        public int BuildLocateParam(
            string strBiblioRecPath,
            string strIndex,
            out List<string> locateParam,
            out string strError)
        {
            strError = "";
            locateParam = null;

            int nRet = 0;

            string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
            string strCommentDbName = "";

            // ������Ŀ����, �ҵ���Ӧ���������
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
            if (String.IsNullOrEmpty(strCommentDbName) == true)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' ��Ӧ��" + this.ItemName + "����û�ж���";
                goto ERROR1;
            }

            string strRootID = ResPath.GetRecordId(strBiblioRecPath);

            locateParam = new List<string>();
            locateParam.Add(strCommentDbName);
            locateParam.Add(strRootID);
            locateParam.Add(strIndex);

            return 0;
        ERROR1:
            return -1;
        }
#endif
        public override int BuildLocateParam(// string strBiblioRecPath,
string strRefID,
out List<string> locateParam,
out string strError)
        {
            strError = "";
            locateParam = null;

            /*
            int nRet = 0;

            string strBiblioDbName = ResPath.GetDbName(strBiblioRecPath);
            string strCommentDbName = "";

            // ������Ŀ����, �ҵ���Ӧ���������
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
            if (String.IsNullOrEmpty(strCommentDbName) == true)
            {
                strError = "��Ŀ���� '" + strBiblioDbName + "' ��Ӧ��" + this.ItemName + "����û�ж���";
                goto ERROR1;
            }
             * */

            locateParam = new List<string>();
            locateParam.Add(strRefID);

            return 0;
            /*
        ERROR1:
            return -1;
             * */
        }


#if NO
        // �������ڻ�ȡ�����¼��XML����ʽ
        public override int MakeGetItemRecXmlSearchQuery(
            List<string> locateParams,
            int nMax,
            out string strQueryXml,
            out string strError)
        {
            strQueryXml = "";
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 3)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ3��";
                return -1;
            }

            string strCommentDbName = locateParams[0];
            string strRootID = locateParams[1];
            string strIndex = locateParams[2];

            strQueryXml = "<target list='"
        + StringUtil.GetXmlStringSimple(strCommentDbName + ":" + "���")
        + "'><item><word>"
        + StringUtil.GetXmlStringSimple(strIndex)
        + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            strQueryXml += "<operator value='AND'/>";

            strQueryXml += "<target list='"
                    + StringUtil.GetXmlStringSimple(strCommentDbName + ":" + "����¼")
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strRootID)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>-1</maxCount></item><lang>zh</lang></target>";

            strQueryXml = "<group>" + strQueryXml + "</group>";

            return 0;
        }
#endif
        // �������ڻ�ȡ�����¼��XML����ʽ
        public override int MakeGetItemRecXmlSearchQuery(
            List<string> locateParams,
            int nMax,
            out string strQueryXml,
            out string strError)
        {
            strQueryXml = "";
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 1)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ1��";
                return -1;
            }

            string strRefID = locateParams[0];

            // �������ʽ
            int nDbCount = 0;
            for (int i = 0; i < this.App.ItemDbs.Count; i++)
            {
                string strDbName = this.App.ItemDbs[i].CommentDbName;

                if (String.IsNullOrEmpty(strDbName) == true)
                    continue;

                string strOneDbQuery = "<target list='"
                    + StringUtil.GetXmlStringSimple(strDbName + ":" + "�ο�ID")
                    + "'><item><word>"
                    + StringUtil.GetXmlStringSimple(strRefID)
                    + "</word><match>exact</match><relation>=</relation><dataType>string</dataType><maxCount>" + nMax.ToString() + "</maxCount></item><lang>zh</lang></target>";

                if (nDbCount > 0)
                {
                    Debug.Assert(String.IsNullOrEmpty(strQueryXml) == false, "");
                    strQueryXml += "<operator value='OR'/>";
                }

                strQueryXml += strOneDbQuery;
                nDbCount++;
            }

            if (nDbCount > 0)
            {
                strQueryXml = "<group>" + strQueryXml + "</group>";
            }

            return 0;
        }


#if NO
        // ���춨λ��ʾ��Ϣ�����ڱ���
        public override int GetLocateText(
            List<string> locateParams,
            out string strText,
            out string strError)
        {
            strText = "";
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 3)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ3��";
                return -1;
            }
            string strCommentrDbName = locateParams[0];
            string strRootID = locateParams[1];
            string strIndex = locateParams[2];

            strText = "��ע��Ϊ '" + strCommentrDbName + "'������¼IDΪ '" + strRootID + "' ���Ϊ '" + strIndex + "'";
            return 0;
        }
#endif

#if NO
        // �۲��Ѵ��ڵļ�¼�У�Ψһ���ֶ��Ƿ��Ҫ���һ��
        // return:
        //      -1  ����
        //      0   һ��
        //      1   ��һ�¡�������Ϣ��strError��
        public override int IsLocateInfoCorrect(
            List<string> locateParams,
            XmlDocument domExist,
            out string strError)
        {
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 3)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ3��";
                return -1;
            }
            string strCommentDbName = locateParams[0];
            string strRootID = locateParams[1];
            string strIndex = locateParams[2];

            if (String.IsNullOrEmpty(strIndex) == false)
            {
                string strExistingIndex = DomUtil.GetElementText(domExist.DocumentElement,
                    "index");
                if (strExistingIndex != strIndex)
                {
                    strError = "��ע��¼��<index>Ԫ���еı�� '" + strExistingIndex + "' ��ͨ��ɾ����������ָ���ı�� '" + strIndex + "' ��һ�¡�";
                    return 1;
                }
            }

            return 0;
        }
#endif

        // �۲��Ѿ����ڵļ�¼�Ƿ�����ͨ��Ϣ
        // return:
        //      -1  ����
        //      0   û��
        //      1   �С�������Ϣ��strError��
        public override int HasCirculationInfo(XmlDocument domExist,
            out string strError)
        {
            strError = "";
            return 0;
        }

        // �Ƿ��������¼�¼?
        // parameters:
        // return:
        //      -1  �����������޸ġ�
        //      0   ������������ΪȨ�޲�����ԭ��ԭ����strError��
        //      1   ���Դ���
        public override int CanCreate(
            SessionInfo sessioninfo,
            XmlDocument domNew,
            out string strError)
        {
            strError = "";

            if (sessioninfo == null)
            {
                strError = "sessioninfo == null";
                return -1;
            }

            if (sessioninfo.UserType == "reader")
            {
                string strReaderState = DomUtil.GetElementText(sessioninfo.Account.ReaderDom.DocumentElement,
                    "state");
                if (StringUtil.IsInList("ע��", strReaderState) == true)
                {
                    strError = "����֤״̬Ϊ ע���� ���ܴ�����ע��¼";
                    return 0;
                }
            }

            return 1;
        }

        // �Ƿ�����Ծɼ�¼�����޸�? 
        // parameters:
        // return:
        //      -1  �����������޸ġ�
        //      0   �������޸ģ���ΪȨ�޲�����ԭ��ԭ����strError��
        //      1   �����޸�
        public override int CanChange(
            SessionInfo sessioninfo,
            string strAction,
            XmlDocument domExist,
            XmlDocument domNew,
            out string strError)
        {
            strError = "";

            if (sessioninfo == null)
            {
                strError = "sessioninfo == null";
                return -1;
            }

            if (sessioninfo.UserType == "reader")
            {
                string strReaderState = DomUtil.GetElementText(sessioninfo.Account.ReaderDom.DocumentElement,
                    "state");
                if (StringUtil.IsInList("ע��", strReaderState) == true)
                {
                    strError = "����֤״̬Ϊ ע���� �����޸��κ���ע��¼";
                    return 0;
                }

                string strOperator = sessioninfo.UserID;
                string strOldUserID = DomUtil.GetElementText(domExist.DocumentElement,
                    "creator");
                if (strOperator != strOldUserID)
                {
                    strError = "������ݵ��û������޸��������û���������ע��¼";
                    return 0;
                }

                string strNewUserID = DomUtil.GetElementText(domNew.DocumentElement,
    "creator");
                if (strNewUserID != strOperator)
                {
                    strError = "������ݵ��û������޸�������ע��¼��<creator>Ԫ��";
                    return 0;
                }

                string strState = DomUtil.GetElementText(domExist.DocumentElement,
"state");
                if (StringUtil.IsInList("����", strState) == true)
                {
                    strError = "������ݵ��û������޸Ĵ�������״̬����ע��¼";
                    return 0;
                }
            }

            // 2012/10/4
            if (sessioninfo.GlobalUser == false)
            {
                string strLibraryCode = DomUtil.GetElementText(domExist.DocumentElement,
    "libraryCode");
                // ��鵱ǰ�û��Ƿ��Ͻ��ע��¼
                if (StringUtil.IsInList(strLibraryCode, sessioninfo.LibraryCodeList) == false)
                {
                    strError = "��ǰ��ע��¼(<libraryCode>Ԫ����)�Ĺݴ��� '"+strLibraryCode+"' ���ڵ�ǰ�û��Ĺݴ��� '"+sessioninfo.LibraryCodeList+"' ��Ͻ��Χ�ڡ��޸���ע��¼�Ĳ������ܾ�";
                    return 0;
                }
            }

            return 1;
        }

        // ��¼�Ƿ�����ɾ��?
        // return:
        //      -1  ����������ɾ����
        //      0   ������ɾ������ΪȨ�޲�����ԭ��ԭ����strError��
        //      1   ����ɾ��
        public override int CanDelete(
            SessionInfo sessioninfo,
            XmlDocument domExist,
            out string strError)
        {
            strError = "";

            if (sessioninfo == null)
            {
                strError = "sessioninfo == null";
                return -1;
            }

            if (sessioninfo.UserType == "reader")
            {
                string strReaderState = DomUtil.GetElementText(sessioninfo.Account.ReaderDom.DocumentElement,
    "state");
                if (StringUtil.IsInList("ע��", strReaderState) == true)
                {
                    strError = "����֤״̬Ϊ ע���� ����ɾ���κ���ע��¼";
                    return 0;
                }

                string strNewUserID = sessioninfo.UserID;
                string strOldUserID = DomUtil.GetElementText(domExist.DocumentElement,
                    "creator");
                if (strNewUserID != strOldUserID)
                {
                    strError = "������ݵ��û�����ɾ���������û���������ע��¼";
                    return 0;
                }

                string strState = DomUtil.GetElementText(domExist.DocumentElement,
                    "state");
                if (StringUtil.IsInList("����", strState) == true)
                {
                    strError = "������ݵ��û�����ɾ����������״̬����ע��¼";
                    return 0;
                }
            }

            // 2012/10/4
            if (sessioninfo.GlobalUser == false)
            {
                string strLibraryCode = DomUtil.GetElementText(domExist.DocumentElement,
    "libraryCode");
                // ��鵱ǰ�û��Ƿ��Ͻ��ע��¼
                if (StringUtil.IsInList(strLibraryCode, sessioninfo.LibraryCodeList) == false)
                {
                    strError = "��ǰ��ע��¼(<libraryCode>Ԫ����)�Ĺݴ��� '" + strLibraryCode + "' ���ڵ�ǰ�û��Ĺݴ��� '" + sessioninfo.LibraryCodeList + "' ��Ͻ��Χ�ڡ�ɾ����ע��¼�Ĳ������ܾ�";
                    return 0;
                }
            }

            return 1;
        }

#if NO
        // ��λ����ֵ�Ƿ�Ϊ��?
        // return:
        //      -1  ����
        //      0   ��Ϊ��
        //      1   Ϊ��(��ʱ��Ҫ��strError�и�������˵������)
        public override int IsLocateParamNullOrEmpty(
            List<string> locateParams,
            out string strError)
        {
            strError = "";

            // ��������̬�Ĳ�����ԭ
            if (locateParams.Count != 3)
            {
                strError = "locateParams�����ڵ�Ԫ�ر���Ϊ3��";
                return -1;
            }
            string strCommentDbName = locateParams[0];
            string strRootID = locateParams[1];
            string strIndex = locateParams[2];

            if (String.IsNullOrEmpty(strIndex) == true)
            {
                strError = "<index>Ԫ���еı��Ϊ��";
                return 1;
            }

            return 0;
        }
#endif

        // �������ơ�
        public override string ItemName
        {
            get
            {
                return "��ע";
            }
        }

        // �������ơ�
        public override string ItemNameInternal
        {
            get
            {
                return "Comment";
            }
        }

        public override string DefaultResultsetName
        {
            get
            {
                return "comments";
            }
        }

        // ׼��д����־��SetXXX�����ַ��������硰SetEntity�� ��SetIssue��
        public override string OperLogSetName
        {
            get
            {
                return "setComment";
            }
        }

        public override string SetApiName
        {
            get
            {
                return "SetComments";
            }
        }

        public override string GetApiName
        {
            get
            {
                return "GetComments";
            }
        }

        // ������ʺϱ�����������¼
        public override int BuildNewItemRecord(
            SessionInfo sessioninfo,
            bool bForce,
            string strBiblioRecId,
            string strOriginXml,
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

            // 2010/4/2
            DomUtil.SetElementText(dom.DocumentElement,
                "parent",
                strBiblioRecId);

            // 2012/10/3
            // ��ǰ�û�����Ͻ�Ĺݴ���
            DomUtil.SetElementText(dom.DocumentElement,
                "libraryCode",
                sessioninfo.LibraryCodeList);

            strXml = dom.OuterXml;

            return 0;
        }

        // ����������ݿ���
        // return:
        //      -1  error
        //      0   û���ҵ�(��Ŀ��)
        //      1   found
        public override int GetItemDbName(string strBiblioDbName,
            out string strItemDbName,
            out string strError)
        {
            return this.App.GetCommentDbName(strBiblioDbName,
                out strItemDbName,
                out strError);
        }

        // 2012/4/27
        public override bool IsItemDbName(string strItemDbName)
        {
            return this.App.IsCommentDbName(strItemDbName);
        }

#if NO
        // ���¾������¼�а����Ķ�λ��Ϣ���бȽ�, �����Ƿ����˱仯(��������Ҫ����)
        // parameters:
        //      oldLocateParam   ˳�㷵�ؾɼ�¼�еĶ�λ����
        //      newLocateParam   ˳�㷵���¼�¼�еĶ�λ����
        // return:
        //      -1  ����
        //      0   ���
        //      1   �����
        public override int CompareTwoItemLocateInfo(
            string strItemDbName,
            XmlDocument domOldRec,
            XmlDocument domNewRec,
            out List<string> oldLocateParam,
            out List<string> newLocateParam,
            out string strError)
        {
            strError = "";


            string strOldIndex = DomUtil.GetElementText(domOldRec.DocumentElement,
                "index");

            string strNewIndex = DomUtil.GetElementText(domNewRec.DocumentElement,
                "index");


            string strOldRootID = DomUtil.GetElementText(domOldRec.DocumentElement,
                "root");

            string strNewRootID = DomUtil.GetElementText(domNewRec.DocumentElement,
                "root");

            oldLocateParam = new List<string>();
            oldLocateParam.Add(strItemDbName);
            oldLocateParam.Add(strOldRootID);
            oldLocateParam.Add(strOldIndex);

            newLocateParam = new List<string>();
            newLocateParam.Add(strItemDbName);
            newLocateParam.Add(strNewRootID);
            newLocateParam.Add(strNewIndex);

            if (strOldIndex != strNewIndex)
                return 1;   // �����

            return 0;   // ��ȡ�
        }
#endif

#if NO
        public override void LockItem(List<string> locateParam)
        {
            string strItemDbName = locateParam[0];
            string strRootID = locateParam[1];
            string strIndex = locateParam[2];

            this.App.EntityLocks.LockForWrite(
                "comment:" + strItemDbName + "|" + strRootID + "|" + strIndex);
        }

        public override void UnlockItem(List<string> locateParam)
        {
            string strItemDbName = locateParam[0];
            string strRootID = locateParam[1];
            string strIndex = locateParam[2];

            this.App.EntityLocks.UnlockForWrite(
                "comment:" + strItemDbName + "|" + strRootID + "|" + strIndex);
        }
#endif

        // ����������е������¼·��
        // return:
        //      -1  error
        //      0   ����������
        //      1   ��������
        public override int GetCommandItemRecPath(
            List<string> locateParam,
            out string strItemRecPath,
            out string strError)
        {
            strItemRecPath = "";
            strError = "";

            if (locateParam.Count < 3)
            {
                strError = "locateParam��������Ϊ3��Ԫ�����ϡ���3��Ԫ��Ϊ���ܵ�������";
                return -1;
            }

            string strCommandLine = locateParam[3];

            // �����������е������¼·��
            // return:
            //      -1  error
            //      0   ����������
            //      1   ��������
            return ParseCommandItemRecPath(
                strCommandLine,
                out strItemRecPath,
                out strError);
        }

        public override int VerifyItem(
LibraryHost host,
string strAction,
XmlDocument itemdom,
out string strError)
        {
            strError = "";

            // ִ�к���
            try
            {
                return host.VerifyComment(strAction,
                    itemdom,
                    out strError);
            }
            catch (Exception ex)
            {
                strError = "ִ�нű����� '" + "VerifyComment" + "' ʱ����" + ex.Message;
                return -1;
            }

            return 0;
        }

    }
}
