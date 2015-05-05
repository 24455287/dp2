using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Collections;


using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.Text.SectionPropertyString;

namespace DigitalPlatform.rms
{
    // ����Ȩ��
    public class CfgRights
    {
        public XmlNode nodeRoot = null; // Ȩ�޶����Ԫ��
        public ArrayList MacroRights = null; // ��Ȩ������

        // ��ʼ��
        // parameters:
        //      node   Ȩ�޶�����ڵ� 
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int Initial(XmlNode node,
            out string strError)
        {
            strError = "";

            this.nodeRoot = node;

            // ���Ȩ������
            this.InitialMacroRights();

            return 0;
        }

        // ��ʼ����Ȩ������
        private void InitialMacroRights()
        {
            MacroRights = new ArrayList();

            this.MacroRights.Add(new MacroRightItem(
                "write",
                "overwrite,delete,create"));

            string strManagementRights =
                "list,"
                + "read,"
                + "overwrite,delete,create,"
                + "clear,"
                + "changepassword,";

            this.MacroRights.Add(new MacroRightItem(
                "management",
                strManagementRights));
        }


        // ���Ȩ��
        // parameters:
        //      strPath     ��Դ·��
        //      resType     ��Դ����
        //      strRights   �����ҵ�Ȩ��
        //      strExistRights  out����,�����Ѵ��ڵ�Ȩ��
        //      resultType  out����,���ز��ҽ��
        //                  Minus = -1, // ��
        //                  None = 0,   // û�ж���    
        //                  Plus = 1,   // ��
        //      strError    out����,���س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int CheckRights(
            string strPath,
            List<string> aOwnerDbName,
            string strUserName,
            ResType resType,
            string strQueryRights,
            out string strExistRights,
            out ResultType resultType,
            out string strError)
        {
            strError = "";
            strExistRights = "";

            resultType = ResultType.None;

            Debug.Assert(resType != ResType.None, "resType��������ΪResType.None");

            List<string> aRights = null;
            int nRet = this.BuildRightArray(
                strPath,
                aOwnerDbName,
                strUserName,
                out aRights,
                out strError);
            if (nRet == -1)
                return -1;

            string strResType = "";
            if (resType == ResType.Database)
                strResType = "database";
            else if (resType == ResType.Directory)
                strResType = "directory";
            else if (resType == ResType.File)
                strResType = "leaf";
            else if (resType == ResType.Record)
                strResType = "record";
            else
                strResType = "leaf";

            for (int i = aRights.Count - 1; i >= 0; i--)
            {
                string strOneRights = aRights[i];

                string strSectionName = "";


                string strRealRights = "";
                if (i == aRights.Count - 1)
                {
                    strRealRights = strOneRights;
                }
                else if (i == aRights.Count - 2)
                {
                    strSectionName = "children_" + strResType;
                    strRealRights = this.GetSectionRights(strOneRights,
                        strSectionName);

                    if (strRealRights == "" && strResType != "database")
                    {
                        strSectionName = "descendant_" + strResType;
                        strRealRights = this.GetSectionRights(strOneRights,
                            strSectionName);
                    }
                }
                else
                {
                    strSectionName = "descendant_" + strResType;
                    strRealRights = this.GetSectionRights(strOneRights,
                        strSectionName);
                }

                string strPureRights = this.GetSectionRights(strRealRights, "this");

                if (strPureRights != "")
                {
                    if (strExistRights != "")
                        strExistRights = strExistRights + ",";
                    strExistRights += strPureRights;
                }


                // ��鵱ǰȨ���ַ������Ƿ����ָ����Ȩ��,�ӣ���������
                resultType = this.CheckRights(strQueryRights,
                    strPureRights);
                if (resultType != ResultType.None)
                    return 0;
            }

            return 0;
        }


        // ????Ŀǰ��֧�����ݿ�Ķ����԰汾
        // ΪCheckRights()����ĵײ㺯��
        // ������Դ·������Ȩ������
        // parameters:
        //      strPath     ��Դ·��
        //      aRights     out����,����Ȩ�������
        //      strError    out����,���س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        private int BuildRightArray(
            string strPath,
            List<string> aOwnerDbName,
            string strUserName,
            out List<string> aRights,
            out string strError)
        {
            strError = "";

            aRights = new List<string>();

            string strRights = "";

            // �Ѹ������Ȩ�޼ӵ�������
            strRights = DomUtil.GetAttr(this.nodeRoot, "rights");
            aRights.Add(strRights);

            if (strPath == "")
                return 0;

            string[] paths = strPath.Split(new char[] { '/' });
            Debug.Assert(paths.Length > 0, "��ʱ���鳤�Ȳ�����Ϊ0��");
            if (paths[0] == "" || paths[paths.Length - 1] == "")
            {
                strError = "·��'" + strPath + "'���Ϸ�����β����Ϊ'/'��";
                return -1;
            }


            XmlNode nodeCurrent = this.nodeRoot;
            // ѭ���¼�
            for (int i = 0; i < paths.Length; i++)
            {
                string strName = paths[i];
                bool bFound = false;

                if (nodeCurrent == null)
                {
                    aRights.Add("");
                    continue;
                }

                foreach (XmlNode child in nodeCurrent.ChildNodes)
                {
                    if (child.NodeType != XmlNodeType.Element)
                        continue;

                    string strChildName = DomUtil.GetAttr(child, "name");



                    if (String.Compare(strName, strChildName, true) == 0)
                    {
                        bFound = true;
                        nodeCurrent = child;
                        break;
                    }
                }

                bool bDbo = false;
                if (i == 0)   // ���ݿ���
                {
                    if (aOwnerDbName.IndexOf(strName) != -1)
                        bDbo = true;
                }

                strRights = "";

                // Ϊdbo��������Ȩ��
                if (bDbo == true)
                {
                    strRights += "this:management;children_database:management;children_directory:management;children_leaf:management;descendant_directory:management;descendant_record:management;descendant_leaf:management";
                }

                if (bFound == false)
                {
                    aRights.Add(strRights);
                    nodeCurrent = null;
                    continue;
                }

                // ʵ�ʶ����Ȩ��
                if (nodeCurrent != null)
                {
                    string strTemp = DomUtil.GetAttr(nodeCurrent, "rights");
                    if (String.IsNullOrEmpty(strTemp) == false)
                    {
                        if (strRights != "")
                            strRights += ";";
                        strRights += strTemp;
                    }
                }

                aRights.Add(strRights);
            }
            return 0;
        }

        // ΪCheckRights()����ĵײ㺯��
        // parameters:
        //      strRights   �����ҵ�Ȩ��
        //      strAllRights    �Ѵ��ڵ�ȫ��Ȩ��
        // return:
        //      ResultType����
        //          Minus = -1, // ��
        //          None = 0,   // û�ж���    
        //          Plus = 1,   // ��
        private ResultType CheckRights(string strRights,
            string strAllRights)
        {
            if (strAllRights == "")
                return ResultType.None;

            strAllRights = this.CanonicalizeRightString(strAllRights);

            string[] rights = strAllRights.Split(new char[] {','});
            for (int i = rights.Length -1; i >= 0; i--)
            {
                string strOneRight = rights[i];
                if (strOneRight == "")
                    continue;

                string strFirstChar = strOneRight.Substring(0, 1);
                
                // ǰ����+ , - �ŵ����
                if (strFirstChar == "+" || strFirstChar == "-")
                {
                    strOneRight = strOneRight.Substring(1);
                }

                if (String.Compare(strRights, strOneRight, true) == 0
                    || strOneRight == "*")
                {
                    if (strFirstChar == "-")
                        return ResultType.Minus;
                    else
                        return ResultType.Plus;
                }
            }

            return ResultType.None;            
        }

        // �淶��Ȩ���ַ���
        private string CanonicalizeRightString(string strRights)
        {
            for (int i = 0; i < this.MacroRights.Count; i++)
            {
                MacroRightItem item = (MacroRightItem)this.MacroRights[i];

                strRights = strRights.Replace(item.MacroRight, item.RealRight);
            }
            return strRights;
        }

        // �õ�ָ��С�ڵ�Ȩ��
        private string GetSectionRights(string strRights,
            string strCategory)
        {
            DigitalPlatform.Text.SectionPropertyString.PropertyCollection propertyColl =
                new DigitalPlatform.Text.SectionPropertyString.PropertyCollection("this",
                strRights,
                DelimiterFormat.Semicolon);
            Section section = propertyColl[strCategory];
            if (section == null)
                return "";

            return section.Value;
        }

    }

    public enum ResultType
    {
        Minus = -1, // ��
        None = 0,   // û�ж���    
        Plus = 1,   // ��
    }

    // ��Դ����
    public enum ResType
    {
        None = 0,
        Server = 1,
        Database = 2,
        Record = 3,
        Directory = 4,
        File = 5,
    }

    // ��Ȩ�޶���
    public class MacroRightItem
    {
        public string MacroRight = "";
        public string RealRight = "";

        public MacroRightItem(string strMacroRight,
            string strRealRight)
        {
            this.MacroRight = strMacroRight;
            this.RealRight = strRealRight;
        }
    }
}
