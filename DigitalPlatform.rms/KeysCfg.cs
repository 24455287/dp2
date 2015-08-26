using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Diagnostics;
using System.IO;
using System.Text;

using System.Reflection;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System.CodeDom;
using System.CodeDom.Compiler;

using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using DigitalPlatform.IO;

namespace DigitalPlatform.rms
{
    // KeysCfg ��ժҪ˵����
    public class KeysCfg : KeysBrowseBase
    {
        public string Prefix = "keys_";
        public StopwordCfg StopwordCfg = null;  // ӵ��

        // <key>Ԫ���¼���<table>Ԫ�� �� TableInfo����Ķ��ձ� 
        Hashtable tableTableInfoClient = new Hashtable();

        // ��Client���ù�<table>Ԫ�ص�xpath·�� �� TableInfo����Ķ��ձ�
        // <table>�������ڲ�ֱ�Ӷ���ģ�Ҳ�������ⲿ����ģ�������client���ù���
        // ����������ʱ���Ϳ����ӵ��ģ���������ʱ��ͨ��һ����Դ����TableInfoʱ��Ӧ�Ӹ�table���ң��������ظ��ġ�
        Hashtable tableTableInfoServer = new Hashtable();

        public List<TableInfo> m_aTableInfoForForm = null;

        public Assembly m_assembly = null;
        public string m_strAssemblyError = "";

        Hashtable m_exprCache = new Hashtable();

        // ��ʼ��KeysCfg���󣬰�dom׼���ã�������Hashtable׼����
        public int Initial(string strKeysCfgFileName,
            string strBinDir,
            string strKeysTableNamePrefix,
            out string strError)
        {
            int nRet = base.Initial(strKeysCfgFileName,
                strBinDir,
                out strError);
            if (nRet == -1)
                return -1;

            this.Prefix = strKeysTableNamePrefix;

            nRet = this.CreateTableInfoTableCache(
                out strError);
            if (nRet == -1)
                return -1;

            if (this.dom != null)
            {
                // ��ʼ��stopword
                XmlNode nodeStopword = dom.DocumentElement.SelectSingleNode("//stopword");
                if (nodeStopword != null)
                {
                    this.StopwordCfg = new StopwordCfg();
                    nRet = this.StopwordCfg.Initial(nodeStopword,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }
            }


            // ����assembly
            nRet = this.InitialAssembly(out strError);
            if (nRet == -1)
            {
                //strError = "����keys�����ļ��еĽű�����" + strError;
                //return -1;

                this.m_strAssemblyError = "����keys�����ļ��еĽű�����" + strError;
                //return 0;
            }

            return 0;
        }

        // ��ʼ��Assembly����,��Initial��
        // return:
        //		-1	����
        //		0	�ɹ�
        private int InitialAssembly(out string strError)
        {
            strError = "";
            int nRet = 0;

            if (this.dom == null)
            {
                this.m_assembly = null;
                return 0;
            }

            // �ҵ�<script>�ڵ�
            XmlNode nodeScript = this.dom.SelectSingleNode("//script");

            // <script>�ڵ㲻���ڵ�ʱ
            if (nodeScript == null)
            {
                this.m_assembly = null;
                return 0;
            }

            // <script>�ڵ��¼���CDATA�ڵ�
            if (nodeScript.ChildNodes.Count == 0)
            {
                this.m_assembly = null;
                return 0;
            }

            XmlNode firstNode = nodeScript.ChildNodes[0];

            //��һ�����ӽڵ㲻��CDATA�ڵ�ʱ
            if (firstNode.NodeType != XmlNodeType.CDATA)
            {
                this.m_assembly = null;
                return 0;
            }

            //~~~~~~~~~~~~~~~~~~
            // ����Assembly����

            string[] saRef = null;
            nRet = GetRefs(nodeScript,
                 out saRef,
                 out strError);
            if (nRet == -1)
                return -1;

            string[] saAddRef = {this.BinDir + "\\" + "digitalplatform.rms.dll",
                                this.BinDir + "\\" + "digitalplatform.text.dll"};

            string[] saTemp = new string[saRef.Length + saAddRef.Length];
            Array.Copy(saRef, 0, saTemp, 0, saRef.Length);
            Array.Copy(saAddRef, 0, saTemp, saRef.Length, saAddRef.Length);
            saRef = saTemp;

            RemoveRefsProjectDirMacro(ref saRef,
                this.BinDir);

            string strCode = firstNode.Value;

            if (strCode != "")
            {
                Assembly assembly = null;
                string strWarning = "";
                nRet = CreateAssembly(strCode,
                    saRef,
                    out assembly,
                    out strError,
                    out strWarning);
                if (nRet == -1)
                    return -1;

                this.m_assembly = assembly;
            }


            return 0;
        }



        // ��node�ڵ�õ�refs�ַ�������
        // return:
        //      -1  ����
        //      0   �ɹ�
        public static int GetRefs(XmlNode node,
            out string[] saRef,
            out string strError)
        {
            saRef = null;
            strError = "";

            // ����ref�ڵ�
            XmlNodeList nodes = node.SelectNodes("//ref");
            saRef = new string[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                saRef[i] = nodes[i].InnerText.Trim();
            }
            return 0;
        }

        // ȥ��·���еĺ�%projectdir%
        void RemoveRefsProjectDirMacro(ref string[] refs,
            string strBinDir)
        {
            Hashtable macroTable = new Hashtable();

            macroTable.Add("%bindir%", strBinDir);

            for (int i = 0; i < refs.Length; i++)
            {
                string strNew = PathUtil.UnMacroPath(macroTable,
                refs[i],
                false); // ��Ҫ�׳��쳣����Ϊ���ܻ���%binddir%�����ڻ��޷��滻
                refs[i] = strNew;
            }

        } 


        // ����Assembly
        // parameters:
        //		strCode:		�ű�����
        //		refs:			���ӵ��ⲿassembly
        //		strLibPaths:	���·��, ����Ϊ""����null,��˲�����Ч
        //		strOutputFile:	����ļ���, ����Ϊ""����null,��˲�����Ч
        //		strErrorInfo:	������Ϣ
        //		strWarningInfo:	������Ϣ
        // result:
        //		-1  ����
        //		0   �ɹ�
        public static int CreateAssembly(string strCode,
            string[] refs,
            out Assembly assembly,
            out string strError,
            out string strWarning)
        {
            strError = "";
            strWarning = "";
            assembly = null;

            // CompilerParameters����
            CompilerParameters compilerParams = new CompilerParameters();

            compilerParams.GenerateInMemory = true;
            compilerParams.IncludeDebugInformation = false;

            compilerParams.TreatWarningsAsErrors = false;
            compilerParams.WarningLevel = 4;

            compilerParams.ReferencedAssemblies.AddRange(refs);


            //CSharpCodeProvider provider = null;
            CodeDomProvider codeDomProvider = new CSharpCodeProvider();

            // ICodeCompiler compiler = null; // 2006/10/26 changed
            CompilerResults results = null;
            try
            {
                //provider = new CSharpCodeProvider();
                // compiler = codeDomProvider.CreateCompiler(); // 2006/10/26 changed

                results = codeDomProvider.CompileAssemblyFromSource(
                    compilerParams,
                    strCode);
            }
            catch (Exception ex)
            {
                strError = "CreateAssemblyFile() ���� " + ex.Message;
                return -1;
            }

            //return 0;  //��

            int nErrorCount = 0;
            if (results.Errors.Count != 0)
            {
                string strErrorString = "";
                nErrorCount = getErrorInfo(results.Errors,
                    out strErrorString);

                strError = "��Ϣ����:" + Convert.ToString(results.Errors.Count) + "\r\n";
                strError += strErrorString;

                if (nErrorCount == 0 && results.Errors.Count != 0)
                {
                    strWarning = strError;
                    strError = "";
                }
            }
            if (nErrorCount != 0)
                return -1;


            assembly = results.CompiledAssembly;// compilerParams.OutputAssembly;

            return 0;
        }

        // ���������Ϣ�ַ���
        // parameter:
        //		errors:    CompilerResults����
        //		strResult: out���������ع���ĳ����ַ���
        // result:
        //		������Ϣ������
        public static int getErrorInfo(CompilerErrorCollection errors,
            out string strResult)
        {
            strResult = "";
            int nCount = 0;
            if (errors == null)
            {
                strResult = "error����Ϊnull";
                return 0;
            }
            foreach (CompilerError oneError in errors)
            {
                strResult += "(" + Convert.ToString(oneError.Line) + "," + Convert.ToString(oneError.Column) + ")\r\n";
                strResult += (oneError.IsWarning) ? "warning " : "error ";
                strResult += oneError.ErrorNumber + " ";
                strResult += ":" + oneError.ErrorText + "\r\n";

                if (oneError.IsWarning == false)
                    nCount++;
            }
            return nCount;
        }

        


        // ����TableInfo����,��Initial��
        // return:
        //		-1	����
        //		0	�ɹ�
        private int CreateTableInfoTableCache(
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (this.dom == null)
                return 0;

            // �ҵ�<key>�¼�������<table>
            XmlNodeList nodeListTable = this.dom.DocumentElement.SelectNodes("//key/table");
            for (int i = 0; i < nodeListTable.Count; i++)
            {
                // ��ǰ<table>�ڵ�
                XmlNode nodeCurrentTable = nodeListTable[i];

                // Ŀ��<table>�ڵ�
                XmlNode nodeTargetTable = null;

                // return:
                //		-1	����
                //		0	û�ҵ�	strError�����г�����Ϣ
                //		1	�ҵ�
                nRet = FindTableTarget(nodeCurrentTable,
                    out nodeTargetTable,
                    out strError);
                if (nRet != 1)
                    return -1;

                // ȡ��Ŀ��<table>��·��
                string strPath = "";
                nRet = DomUtil.Node2Path(dom.DocumentElement,
                    nodeTargetTable,
                    out strPath,
                    out strError);
                if (nRet == -1)
                    return -1;

                TableInfo tableInfo = (TableInfo)this.tableTableInfoServer[strPath];
                if (tableInfo == null)
                {
                    // return:
                    //		-1	����
                    //		0	�ɹ�
                    nRet = this.GetTableInfo(nodeTargetTable,
                        out tableInfo,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    this.tableTableInfoServer[strPath] = tableInfo;
                }

                // �ӵ��ͻ���TableInfo����
                this.tableTableInfoClient[nodeCurrentTable] = tableInfo;
            }

            return 0;
        }

        // �ҵ����յ�Ŀ��<table>Ԫ��
        // return:
        //		-1	����
        //		0	δ�ҵ�
        //		1	�ҵ�
        private int FindTableTarget(XmlNode nodeCurrentTable,
            out XmlNode nodeTargetTable,
            out string strError)
        {
            Debug.Assert(nodeCurrentTable != null, "FindTableTarget()���ô���nodeTableCurrent��������Ϊnull��");

            nodeTargetTable = null;
            strError = "";

            string strRef = DomUtil.GetAttr(nodeCurrentTable, "ref");
            if (string.IsNullOrEmpty(strRef) == true)
            {
                nodeTargetTable = nodeCurrentTable;
                return 1;
            }

            string strTableName = "";
            string strTableID = "";

            // ����ref����ֵ����̬Ϊ "titlePinyin"������"titlePinyin, #311"����"#311"
            int nRet = ParseRefString(strRef,
            out strTableName,
            out strTableID,
            out strError);
            if (nRet == -1)
                return -1;

            string strXPath = "//table[@name='" + strRef + "']";
            if (string.IsNullOrEmpty(strTableName) == false
                && string.IsNullOrEmpty(strTableID) == false)
                strXPath = "//table[@name='" + strTableName + "' and @id='"+strTableID+"']";
            else if (string.IsNullOrEmpty(strTableName) == false)
                strXPath = "//table[@name='" + strTableName + "']";
            else if (string.IsNullOrEmpty(strTableID) == false)
                strXPath = "//table[@id='" + strTableID + "']";
            else
            {
                strError = "strTableName��strTableID��Ϊ��";
                return -1;
            }

            nodeTargetTable = nodeCurrentTable.SelectSingleNode(strXPath);
            if (nodeTargetTable != null)
                return 1;

            strError = "δ�ҵ���Ϊ '" + strRef + "' ��<table>Ԫ�ء�";
            return 0;
        }

        // ����ref����ֵ����̬Ϊ "titlePinyin"������"titlePinyin, #311"����"#311"
        static int ParseRefString(string strRef,
            out string strTableName,
            out string strTableID,
            out string strError)
        {
            strError = "";
            strTableName = "";
            strTableID = "";

            if (string.IsNullOrEmpty(strRef) == true)
            {
                strError = "strRef����Ϊ��";
                return -1;
            }

            string [] parts = strRef.Split(new char []{','});
            foreach(string part in parts)
            {
                string strText = part.Trim();
                if (String.IsNullOrEmpty(strText) == true)
                    continue;
                if (strText[0] == '#')
                    strTableID = strText.Substring(1);
                else
                    strTableName = strText;
            }

            return 0;
        }

        // parameters:
        // return:
        //		-1	����
        //		0	�ɹ�
        private int GetTableInfo(XmlNode nodeTable,
            out TableInfo tableInfo,
            out string strError)
        {
            strError = "";

            tableInfo = new TableInfo();

            int nRet = tableInfo.Initial(nodeTable,
                this.Prefix,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        // ���� table �� caption ���֣��ҵ���Ӧ�� key/from ֵ
        public static string GetFromValue(XmlElement table)
        {
            XmlElement key = null;
            // ���� table Ԫ�ص��ϼ��ǲ��� key
            if (table.ParentNode.Name == "key")
                key = table.ParentNode as XmlElement;
            else
            {
                string strTableName = table.GetAttribute("name");
                key = table.OwnerDocument.DocumentElement.SelectSingleNode("//key[./table[@ref='" + strTableName + "']]") as XmlElement;
                if (key == null)
                    return "";
            }

            {
                XmlElement from = key.SelectSingleNode("from") as XmlElement;
                if (from != null)
                    return from.InnerText.Trim();
                return "";
            }
        }


        // ����ָ����¼�ļ����㼯��
        // parameters:
        //		domData	��¼����dom ����Ϊnull
        //		strRecordID	��¼id ����Ϊnull���
        //		strLang	���԰汾
        //		strStyle	�����û������
        //		nKeySize	������ߴ�
        //		keys	out�������������ɵļ����㼯��
        //		strError	out������������Ϣ
        // return:
        //		-1	����
        //		0	�ɹ�
        public int BuildKeys(XmlDocument domData,
            string strRecordID,
            string strLang,
//             string strStyle,
            int nKeySize,
            out KeyCollection keys,
            out string strError)
        {
            strError = "";
            keys = new KeyCollection();

            if (this.dom == null)
                return 0;

            if (domData == null)
            {
                strError = "BuildKeys()���ô���domData��������Ϊnull��";
                Debug.Assert(false, strError);
                return -1;
            }

            // Debug.Assert(strRecordID != null && strRecordID != "", "BuildKeys()���ô���strRecordID��������Ϊnull��Ϊ�ա�");

            if (String.IsNullOrEmpty(strLang) == true)
            {
                strError = "BuildKeys()���ô���strLang��������Ϊnull��";
                Debug.Assert(false, strError);
                return -1;
            }

            /*
            if (String.IsNullOrEmpty(strStyle) == true)
            {
                strError = "BuildKeys()���ô���strStyle��������Ϊnull��";
                Debug.Assert(false, strError);
                return -1;
            }
             * */

            if (nKeySize < 0)
            {
                strError = "BuildKeys()���ô���nKeySize��������С��0��";
                Debug.Assert(false, strError);
                return -1;
            }

            int nRet = 0;

            // �ҵ�����<key>�ڵ�
            // TODO: <key> �Ƿ�����ȷ��λ�ã� �����Ϳ��Ա��� // ���ҡ�����Ԥ�Ȼ�������
            XmlNodeList keyList = dom.SelectNodes("//key");

            XPathNavigator nav = domData.CreateNavigator();

        CREATE_CACHE:
            // ����Cache
            if (m_exprCache.Count == 0 && keyList.Count > 0)
            {
                for (int i = 0; i < keyList.Count; i++)
                {
                    XmlNode nodeKey = keyList[i];

                    XmlElement nodeXPath = (XmlElement)nodeKey.SelectSingleNode("xpath");
                    if (nodeXPath == null)
                        continue;

                    string strScriptAttr = nodeXPath.GetAttribute("scripting");

                    if (String.Compare(strScriptAttr, "on", true) == 0)
                        continue;

                    string strXPath = nodeXPath.InnerText.Trim();
                    if (string.IsNullOrEmpty(strXPath) == true)
                        continue;

                    // strNstableName ���Ϊ null ��ʾ���Բ�����
                    string strNstableName = DomUtil.GetAttrDiff(nodeXPath, "nstable");

                    XmlNamespaceManager nsmgr = (XmlNamespaceManager)this.tableNsClient[nodeXPath];
#if DEBUG
                    if (nsmgr != null)
                    {
                        Debug.Assert(strNstableName != null, "����߱����ֿռ���󣬱���<xpath>Ԫ��Ӧ���� 'nstable' ���ԡ�");
                    }
                    else
                    {
                        Debug.Assert(strNstableName == null, "������߱����ֿռ���󣬱���<xpath>Ԫ�ر���û�ж��� 'nstable' ���ԡ�");
                    }
#endif

                    XPathExpression expr = nav.Compile(strXPath);
                    if (nsmgr != null)
                        expr.SetContext(nsmgr);

                    m_exprCache[nodeXPath] = expr;
                }
            }

            string strKey = "";
            string strKeyNoProcess = "";
            string strFromName = "";
            string strFromValue = "";
            string strSqlTableName = "";
            string strNum = "";

            for (int i = 0; i < keyList.Count; i++)
            {
                XmlElement nodeKey = (XmlElement)keyList[i];

                strKey = "";
                strKeyNoProcess = "";
                strFromName = "";
                strFromValue = "";
                strSqlTableName = "";
                strNum = "";

                // TODO: �� GetElementsByTagName �Ż�
                XmlNode nodeFrom = nodeKey.SelectSingleNode("from");
                if (nodeFrom != null)
                    strFromValue = nodeFrom.InnerText.Trim(); // 2012/2/16

                // �Ҳ���<key>�¼���<table>�ڵ�,��Ӧ�ñ���
                XmlNode nodeTable = nodeKey.SelectSingleNode("table");
                if (nodeTable == null)
                {
                    strError = "<key>�¼�δ����<table>�ڵ㡣";
                    return -1;
                }

                TableInfo tableInfo = (TableInfo)this.tableTableInfoClient[nodeTable];
                Debug.Assert(tableInfo != null, "��Hashtable��ȡ����tabInfo������Ϊnull��");



                strSqlTableName = tableInfo.SqlTableName.Trim();

                // �������԰汾�����Դ����
                strFromName = tableInfo.GetCaption(strLang);


                // ���еļ������ַ���
                List<string> aKey = new List<string>();


                XmlNode nodeXpath = nodeKey.SelectSingleNode("xpath");
                string strScriptAttr = "";
                if (nodeXpath != null)
                    strScriptAttr = DomUtil.GetAttr(nodeXpath, "scripting");

                
                if (String.Compare(strScriptAttr, "on", true) == 0)
                {
                    // ִ�нű��õ�������
                    //aKey.Add("abc");

                    //string strOutputString = "";
                    List<String> OutputStrings = null;
                    string strFunctionName = nodeXpath.InnerText.Trim();     // 2012/2/16
                    nRet = this.DoScriptFunction(domData,
                        strFunctionName,
                        "", //strInputString
                        // out strOutputString,
                        out OutputStrings,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // 2007/1/23
                    if (OutputStrings != null)
                    {
                        for (int j = 0; j < OutputStrings.Count; j++)
                        {
                            if (String.IsNullOrEmpty(OutputStrings[j]) == false)
                            {
                                aKey.Add(OutputStrings[j]);
                                // nCount++;
                            }
                        }
                    }

                }
                else
                {
                    string strXpath = "";
                    if (nodeXpath != null)
                        strXpath = nodeXpath.InnerText.Trim(); // 2012/2/16

                    string strNstableName = DomUtil.GetAttrDiff(nodeXpath, "nstable");
#if NO
                    XmlNamespaceManager nsmgr = (XmlNamespaceManager)this.tableNsClient[nodeXpath];
#if DEBUG
                    if (nsmgr != null)
                    {
                        Debug.Assert(strNstableName != null, "��ʱӦ��û�ж���'nstable'���ԡ�");
                    }
                    else
                    {
                        Debug.Assert(strNstableName == null, "��ʱ����û�ж���'nstable'���ԡ�");
                    }
#endif

                    XPathExpression expr = nav.Compile(strXpath);   // TODO �����Ż�
                    if (nsmgr != null)
                        expr.SetContext(nsmgr);
#endif
                    // 2012/7/20�Ż�
                    XPathExpression expr = (XPathExpression)m_exprCache[nodeXpath];

                    if (expr == null)
                    {
                        this.m_exprCache.Clear();
                        goto CREATE_CACHE;  // TODO: ���Ԥ����ѭ��?
                    }

                    string strMyKey = "";

                    if (expr.ReturnType == XPathResultType.Number)
                    {
                        strMyKey = nav.Evaluate(expr).ToString();//Convert.ToString((int)(nav.Evaluate(expr)));
                        aKey.Add(strMyKey);
                    }
                    else if (expr.ReturnType == XPathResultType.Boolean)
                    {
                        strMyKey = Convert.ToString((bool)(nav.Evaluate(expr)));
                        aKey.Add(strMyKey);
                    }
                    else if (expr.ReturnType == XPathResultType.String)
                    {
                        strMyKey = (string)(nav.Evaluate(expr));
                        aKey.Add(strMyKey);
                    }
                    else if (expr.ReturnType == XPathResultType.NodeSet)
                    {
                        // ????????xpath���ж���ڵ�ʱ���Ƿ񴴽����key
                        XPathNodeIterator iterator = null;
                        try
                        {
                            iterator = nav.Select(expr);
                        }
                        catch (Exception ex)
                        {
                            string strTempNstableName = "";
                            if (strNstableName == null)
                                strTempNstableName = "null";
                            else
                                strTempNstableName = "'" + strNstableName + "'";
                            strError = "��·��'" + strXpath + "'ѡ�ڵ�ʱ����" + ex.Message + " \r\nʹ�õ����ֿռ����Ϊ" + strTempNstableName + "��";
                            return -1;
                        }

                        if (iterator != null)
                        {
                            while (iterator.MoveNext())
                            {
                                XPathNavigator navigator = iterator.Current;
                                strMyKey = navigator.Value;
                                if (strMyKey == "")
                                    continue;

                                aKey.Add(strMyKey);
                            }
                        }
                    }
                    else
                    {
                        throw (new Exception("XPathExpression��ReturnTypeΪ'" + expr.ReturnType.ToString() + "'��Ч"));
                    }
                }


                for (int j = 0; j < aKey.Count; j++)
                {
                    strKey = aKey[j];
                    //???????ע�⣬���key����Ϊ�գ��Ƿ�ҲӦ������һ��key��?
                    if (strKey == "")
                    	continue;

                    strKeyNoProcess = strKey;
                    strNum = "-1";

                    List<string> outputKeys = new List<string>();
                    if (tableInfo.nodeConvertKeyString != null)
                    {
                        nRet = ConvertKeyWithStringNode(domData,
                            strKey,
                            tableInfo.nodeConvertKeyString,
                            out outputKeys,
                            out strError);
                        if (nRet == -1)
                            return -1;
                    }
                    else
                    {
                        outputKeys = new List<string>();
                        outputKeys.Add(strKey);
                    }

                    for (int k = 0; k < outputKeys.Count; k++)
                    {
                        string strOneKey = outputKeys[k];
                        //������������ý��д���,�õ�num
                        if (tableInfo.nodeConvertKeyNumber != null)
                        {
                            nRet = ConvertKeyWithNumberNode(
                                domData,
                                strOneKey,
                                tableInfo.nodeConvertKeyNumber,
                                out strNum,
                                out strError);
                            if (nRet == -1)
                                return -1;
                            if (nRet == 1)
                            {
                                // 2010/9/27
                                strOneKey = strError + " -- " + strOneKey;
                                strNum = "-1";
                            }

                            // 2010/11/20
                            if (String.IsNullOrEmpty(strNum) == true)
                                continue;
                        }

                        if (strOneKey.Length > nKeySize)
                            strOneKey = strOneKey.Substring(0, nKeySize);
                        if (strNum.Length >= 20)
                            strNum = strNum.Substring(0, 19);

                        KeyItem keyItem = new KeyItem(strSqlTableName,
                            strOneKey,
                            strFromValue,
                            strRecordID,
                            strNum,
                            strKeyNoProcess,
                            strFromName);

                        keys.Add(keyItem);
                    }
                }
            }


            return 0;
        }

        // ִ�нű�����
        // parameters:
        //      dataDom         ����dom
        //      strFunctionName ������
        //      strResultString out���������ؽ���ַ���
        //      strError        out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int DoScriptFunction(XmlDocument dataDom,
            string strFunctionName,
            ref List<string> aInputString,            
            out string strError)
        {
            strError = "";

            // Debug.Assert(dataDom != null,"DoScriptFunction()���ô���dataDom����ֵ����Ϊnull��");
            Debug.Assert(strFunctionName != null && strFunctionName != "", "DoScriptFunction()���ô���strFunctionName����ֵ����Ϊnull��");

            if (aInputString == null)
                return 0;

            int nRet = 0;

            List<string> resultstrings = new List<string>();

            for (int i = 0; i < aInputString.Count; i++)
            {
                string strInputString = aInputString[i];

                // string strOutputString = "";
                List<string> OutputStrings = null;
                nRet = this.DoScriptFunction(dataDom,
                    strFunctionName,
                    strInputString,
                    // out strOutputString,
                    out OutputStrings,
                    out strError);
                if (nRet == -1)
                    return -1;

                int nCount = 0;
                if (OutputStrings != null)
                {
                    for (int j = 0; j < OutputStrings.Count; j++)
                    {
                        if (String.IsNullOrEmpty(OutputStrings[j]) == true)
                            continue;
                        resultstrings.Add(OutputStrings[j]);
                        nCount++;
                    }

                }
                if (nCount == 0 && dataDom == null)
                    resultstrings.Add("");  // ��ֹ�ӹ������ʵ�ʱ�򱨴�
            }

            aInputString = resultstrings;

            return 0;
        }


        // ִ�нű�����
        // parameters:
        //      dataDom         ����dom
        //      strFunctionName ������
        //      strInputString  ����Ĵ�������ַ���
        //      strResultString out���������ؽ���ַ���
        //      strError        out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int DoScriptFunction(XmlDocument dataDom,
            string strFunctionName,
            string strInputString,
            // out string strOutputString,
            out List<string> output_strings,
            out string strError)
        {
            strError = "";
            output_strings = null;

            if (this.m_strAssemblyError != "")
            {
                strError = this.m_strAssemblyError;
                return -1;
            }

            if (this.m_assembly == null)
            {
                strError = "keys �����ļ� '"+this.CfgFileName+"' ��δ����ű����룬����޷�ʹ�ýű�����'" + strFunctionName + "'��";
                return -1;

                //strOutputString = "";
                //return 0;
            }

            Type hostEntryClassType = GetDerivedClassType(
                this.m_assembly,
                "DigitalPlatform.rms.KeysHost");    // TODO: ������Hashtable�Ż�
            if (hostEntryClassType == null)
            {
                strError = "��keys�����ļ��ű���δ�ҵ�DigitalPlatform.rms.KeysHost��������";
                return -1;
            }

            KeysHost host = (KeysHost)hostEntryClassType.InvokeMember(null,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
                null);
            if (host == null)
            {
                strError = "��Type�����ȡKeysHostʵ��Ϊnull��";
                return -1;
            }
            host.DataDom = dataDom;
            host.CfgDom = this.dom;
            host.InputString = strInputString;

            // ִ�к���
            try
            {
                host.Invoke(strFunctionName);
            }
            catch (Exception ex)
            {
                strError = "ִ�нű�����'" + strFunctionName + "'����" + ExceptionUtil.GetDebugText(ex);
                return -1;
            }

            output_strings = host.ResultStrings;

            if (output_strings == null)
                output_strings = new List<string>();

            if (String.IsNullOrEmpty(host.ResultString) == false)
                output_strings.Insert(0, host.ResultString);

            return 0;
        }


        //�õ�������
        //parameter:
        //		assembly            Assembly����
        //		strBaseTypeFullName ����ȫ����
        public static Type GetDerivedClassType(Assembly assembly,
            string strBaseTypeFullName)
        {
            Type[] types = assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].IsClass == false)
                    continue;
                if (IsDeriverdFrom(types[i],
                    strBaseTypeFullName) == true)
                    return types[i];
            }
            return null;
        }


        // �۲�type�Ļ������Ƿ�������ΪstrBaseTypeFullName���ࡣ
        public static bool IsDeriverdFrom(Type type,
            string strBaseTypeFullName)
        {
            Type curType = type;
            for (; ; )
            {
                if (curType == null
                    || curType.FullName == "System.Object")
                    return false;

                if (curType.FullName == strBaseTypeFullName)
                    return true;

                curType = curType.BaseType;
            }
        }
        
        // ��ն���
        public override void Clear()
        {
            this.tableNsClient.Clear();
            this.tableNsServer.Clear();

            this.tableTableInfoClient.Clear();
            this.tableTableInfoServer.Clear();

            m_exprCache.Clear();
        }

        // ���ݱ����õ����������Ϣ
        // parameters:
        // return:
        //		-1	����
        //		0	δ�ҵ�
        //		1	�ҵ�
        public int GetTableInfo(string strTableName,
            List<TableInfo> aTableInfo,
            out TableInfo tableInfo,
            out string strError)
        {
            tableInfo = null;
            strError = "";

            // �������aTableInfo == null����ʾҪ���ϻ�ȡ�����!=null����ʾ��������������ֳ�����
            if (aTableInfo == null)
            {
                int nRet = this.GetTableInfos(
                    out aTableInfo,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            for (int i = 0; i < aTableInfo.Count; i++)
            {
                TableInfo oneTableInfo = aTableInfo[i];
                if (StringUtil.IsInList(strTableName, oneTableInfo.GetAllCaption()) == true)
                {
                    tableInfo = oneTableInfo;
                    return 1;
                }
            }
            strError = "δ�ҵ��߼���'" + strTableName + "'��Ӧ��<table>����";
            return 0;
        }

        // �õ������ļ��ж���TableInfo���飬���ظ���
        // parameters:
        //      aTableInfo  out����������TableInfo��������
        //      strError    out���������س�����Ϣ
        // return
        //		-1	����
        //		0	�ɹ�
        public int GetTableInfos(
            out List<TableInfo> aTableInfo,
            out string strError)
        {
            strError = "";
            aTableInfo = new List<TableInfo>();

            if (this.m_aTableInfoForForm != null)
            {
                aTableInfo = this.m_aTableInfoForForm;
                return 0;
            }

            if (this.dom == null)
                return 0;


            int nRet = 0;

            // �ҵ�<key>�¼������в���ref���Ե� <table>
            string strXpath = "//table[not(@ref)]";
            XmlNodeList nodeListTable = this.dom.DocumentElement.SelectNodes(strXpath);//"//key/table");
            for (int i = 0; i < nodeListTable.Count; i++)
            {
                XmlNode nodeTable = nodeListTable[i];

                TableInfo tableInfo = null;
                // return:
                //		-1	����
                //		0	�ɹ�
                nRet = this.GetTableInfo(nodeTable,
                    out tableInfo,
                    out strError);
                if (nRet == -1)
                    return -1;

                tableInfo.OriginPosition = i + 1; //ԭʼ��

                string strStyle = DomUtil.GetAttr(nodeTable, "style");
                if (StringUtil.IsInList("query", strStyle) == true)
                    tableInfo.m_bQuery = true;
                else
                    tableInfo.m_bQuery = false;



                aTableInfo.Add(tableInfo);
            }

            // aTableInfo.Sort();   // �������򵽵��ǰ���ʲô���ŵģ�Ī������

            nRet = this.MaskDup(aTableInfo,
                out strError);
            if (nRet == -1)
                return -1;

            this.m_aTableInfoForForm = aTableInfo;

            return 0;
        }


        // �õ�ȥ�ص�TableInfo���飬�����б��
        // parameters:
        //      aTableInfo  out����������TableInfo��������
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int GetTableInfosRemoveDup(
            out List<TableInfo> aTableInfo,
            out string strError)
        {
            aTableInfo = new List<TableInfo>();
            strError = "";

            List<TableInfo> aTempTableInfo = null;
            int nRet = this.GetTableInfos(
                out aTempTableInfo,
                out strError);
            if (nRet == -1)
                return -1;

            for (int i = 0; i < aTempTableInfo.Count; i++)
            {
                TableInfo tableInfo = aTempTableInfo[i];
                if (tableInfo.Dup == true)
                    continue;
                aTableInfo.Add(tableInfo);
            }

            return 0;
        }

#if NO
        // �Ա����ϵĳ�Ա����ȥ�أ���ȥ�ر��
        // parameters:
        //      aTableInfo  TableInfo����
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int MaskDup(List<TableInfo> aTableInfo,
            out string strError)
        {
            strError = "";

            TableInfo holdTableInfo = null;
            for (int i = 0; i < aTableInfo.Count; i++)
            {
                TableInfo tableInfo = aTableInfo[i];
                if (holdTableInfo == null)
                {
                    holdTableInfo = tableInfo;
                    continue;
                }

                if (tableInfo.CompareTo(holdTableInfo) == 0)
                {
                    tableInfo.Dup = true;
                    if (tableInfo.SqlTableName != holdTableInfo.SqlTableName)
                    {
                        strError = "���(��1��ʼ����)Ϊ '" + Convert.ToString(tableInfo.OriginPosition) + "' ��<table>Ԫ�������Ϊ '" + Convert.ToString(holdTableInfo.OriginPosition) + "' ��<table>Ԫ�ص� 'id' ������ͬ���� 'name' ���Բ�ͬ�����ǲ��Ϸ��ġ�";
                        return -1;
                    }
                }
                else
                {
                    holdTableInfo = tableInfo;
                }
            }
            return 0;
        }
#endif

        // �Ա�����ͬ�Ĵ����ظ���ǡ�����ǰ����Ҫ����
        // parameters:
        //      aTableInfo  TableInfo����
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int MaskDup(List<TableInfo> aTableInfo,
            out string strError)
        {
            strError = "";

            Hashtable name_table = new Hashtable();
            for (int i = 0; i < aTableInfo.Count; i++)
            {
                TableInfo tableInfo = aTableInfo[i];

                string strTableName = tableInfo.SqlTableName.ToLower();
                if (name_table[strTableName] == null)
                {
                    name_table[strTableName] = 1;
                }
                else
                    tableInfo.Dup = true;
            }
            return 0;
        }


        #region �ӹ��ַ����ľ�̬����



        // ���������͵ļ�������мӹ�
        // parameter:
        //		strText	���ӹ����ַ���
        //		stringNode	number�ڵ�
        //		strKey	out �ӹ���ļ������ַ���
        //		strError	out ������Ϣ
        // return:
        //		-1	����
        //		0	�ɹ�
        //      1   ת��Ϊ���ֵĹ���ʧ�� strError���б�����Ϣ 2010/9/27
        public int ConvertKeyWithNumberNode(
            XmlDocument dataDom,
            string strText,
            XmlNode numberNode,
            out string strKey,
            out string strError)
        {
            strKey = "";
            strError = "";

            if (numberNode == null)
            {
                strError = "ConvertKeyWithNumberNode(),numberNode��������Ϊnull";
                return -1;
            }

            strKey = strText;

            // ��Ϊmoneyʱ,��չ��λ��
            string strPrecision = DomUtil.GetAttr(numberNode, "precision");

            string strStyles = DomUtil.GetAttr(numberNode, "style");
            string[] styles = strStyles.Split(new char[] { ',' });
            foreach (string strOneStyleParam in styles)
            {
                string strOneStyle = strOneStyleParam.Trim();

                if (String.IsNullOrEmpty(strOneStyle) == true)
                    continue;

                string strOneStyleLower = strOneStyle.ToLower();

                if (strOneStyleLower == "money")
                {
                    if (strPrecision == "")
                        strPrecision = "0";
                    strKey = StringUtil.ExtendByPrecision(
                        strKey,
                        strPrecision);
                }
                else if (strOneStyleLower == "integer")
                {
                    strKey = StringUtil.ExtendByPrecision(
                        strKey,
                        "0");
                }
                else if (strOneStyleLower == "rfc1123time")
                {
                    if (string.IsNullOrEmpty(strKey) == true)
                    {
                        // 2012/3/30
                        strKey = "";
                    } 
                    else if (strKey == "0")
                    {
                    }
                    else if (strKey == "9999999999")
                    {
                        strKey = DateTime.MaxValue.Ticks.ToString();
                    }
                    else
                    {
                        long nTicks = -1; //ȱʡֵ-1
                        try
                        {
                            DateTime time = DateTimeUtil.FromRfc1123DateTimeString(strKey);
                            nTicks = time.Ticks;
                        }
                        catch
                        {
                            strError = "ʱ���ַ��� '" + strKey + "' ���ǺϷ���rfc1123��ʽ";
                            return 1;
                        }

                        strKey = Convert.ToString(nTicks);
                    }
                }
                else if (strOneStyleLower == "utime")// 2010/2/12
                {
                    if (string.IsNullOrEmpty(strKey) == true)
                    {
                        // 2012/3/29
                        strKey = "";
                    }
                    else if (strKey == "0")
                    {
                    }
                    else if (strKey == "9999999999")
                    {
                        strKey = DateTime.MaxValue.Ticks.ToString();
                    }
                    else
                    {
                        // 2010-01-01 12:01:01Z
                        // ����дΪ
                        // 2010/01/01 12:01:01Z
                        strKey = strKey.Replace("/", "-");

                        long nTicks = -1; //ȱʡֵ-1
                        try
                        {
                            DateTime time = DateTimeUtil.FromUTimeString(strKey);
                            nTicks = time.Ticks;
                        }
                        catch
                        {
                            strError = "ʱ���ַ��� '" + strKey + "' ���ǺϷ���utime��ʽ";
                            return 1;
                        }

                        strKey = Convert.ToString(nTicks);
                    }
                }
                else if (strOneStyleLower == "freetime")// 2012/5/15
                {
                    if (string.IsNullOrEmpty(strKey) == true)
                    {
                        strKey = "";
                    }
                    else if (strKey == "0")
                    {
                    }
                    else if (strKey == "9999999999")
                    {
                        strKey = DateTime.MaxValue.Ticks.ToString();
                    }
                    else
                    {
                        long nTicks = -1; //ȱʡֵ-1
                        try
                        {
                            DateTime time = DateTimeUtil.ParseFreeTimeString(strKey);
                            nTicks = time.Ticks;
                        }
                        catch
                        {
                            strError = "ʱ���ַ��� '" + strKey + "' ���ǺϷ���freetime��ʽ";
                            return 1;
                        }

                        strKey = Convert.ToString(nTicks);
                    }
                }
                else
                {
                    // 2010/11/20


                    // ����C#�ű���������
                    string strFirstChar = "";
                    if (strOneStyle.Length > 0)
                        strFirstChar = strOneStyle.Substring(0, 1);

                    // �ű�����
                    if (strFirstChar == "#")
                    {
                        string strFunctionName = strOneStyle.Substring(1);
                        if (strFunctionName == "")
                        {
                            strError = "�ӹ�������ʱ�����������ַ��'" + strOneStyle + "'δд�ű���������";
                            return -1;

                        }
                        List<String> keys = new List<string>();
                        keys.Add(strKey);
                        int nRet = this.DoScriptFunction(dataDom,
                            strFunctionName,
                            ref keys,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        if (keys.Count > 0)
                            strKey = keys[0];
                        else
                            strKey = null;
                    }
                    else
                    {
                        strError = "�ӹ�������ʱ,������ֵ����ʱ����֧��'" + strOneStyle + "'��񣬱�����'money','integer','rfc1123time','utime'����'#...'";
                        return -1;
                    }

                    /*
                    strError = "�ӹ�������ʱ,������ֵ����ʱ����֧��'" + strOneStyle + "'��񣬱�����'money','integer','rfc1123time','utime'";
                    return -1;
                     * */
                }
            }
            return 0;
        }

        // ���ַ������͵ļ�������мӹ�
        // parameter:
        //		strText	���ӹ����ַ���
        //		stringNode	string�ڵ�
        //		keys	out �ӹ���ļ���������
        //		strError	out ������Ϣ
        // return:
        //		-1	����
        //		0	�ɹ�
        public int ConvertKeyWithStringNode(
            XmlDocument dataDom,
            string strText,
            XmlNode stringNode,
            out List<string> keys,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            keys = null;

            if (stringNode == null)
            {
                strError = "ConvertKeyWithStringNode(),stringNode��������Ϊnull";
                return -1;
            }

            keys = new List<string>();


            // �Ѵ�����ַ�����Ϊ��һ����
            keys.Add(strText);

            // �õ������
            string strStyles = DomUtil.GetAttr(stringNode, "style");
            string[] styles = strStyles.Split(new char[] { ',' });
            bool bHasFoundStopword = false;
            string strStopwordTableName = DomUtil.GetAttr(stringNode, "stopwordTable"); // BUG !!! 2012/4/18 ��ǰΪstopwordtable
            foreach (string strOneStyleParam in styles)
            {
                string strOneStyle = strOneStyleParam.Trim();

                if (String.IsNullOrEmpty(strOneStyle) == true)
                    continue;

                string strOneStyleLower = strOneStyle.ToLower();

                if (strOneStyleLower == "upper")
                {
                    // ��һ���ַ�����������ݶ���ɴ�д
                    KeysCfg.DoUpper(ref keys);
                }
                else if (strOneStyleLower == "lower")
                {
                    // ��һ���ַ�����������ݶ����Сд
                    KeysCfg.DoLower(ref keys);
                }
                else if (strOneStyleLower == "removeblank")
                {
                    // ȥ���ո�
                    KeysCfg.RemoveBlank(ref keys);
                }
                else if (strOneStyleLower == "removecmdcr")
                {
                    // 2012/11/6
                    // ȥ�� {cr:...} �����
                    for (int i = 0; i < keys.Count; i++)
                    {
                        string strKey = keys[i];
                        string strCmd = StringUtil.GetLeadingCommand(strKey);
                        if (string.IsNullOrEmpty(strCmd) == false
                            && StringUtil.HasHead(strCmd, "cr:") == true)
                        {
                            strKey = strKey.Substring(strCmd.Length + 2);
                            if (string.IsNullOrEmpty(strKey) == true)
                            {
                                keys.RemoveAt(i);
                                i--;
                                continue;
                            }

                            keys[i] = strKey;
                        }
                    }
                }
                else if (strOneStyleLower == "pinyinab")
                {
                    // ƴ����д��ͷ
                    KeysCfg.DoPinyinAb(ref keys);
                }
                else if (strOneStyleLower == "simplify")
                {
                    // ��һ���ַ�����������ݶ���ɼ���
                    KeysCfg.DoSimplify(ref keys);
                }
                else if (strOneStyleLower == "traditionalize")
                {
                    // ��һ���ַ�����������ݶ���ɷ���
                    KeysCfg.DoTraditionalize(ref keys);
                }
                else if (strOneStyleLower == "fulltext")
                {
                    List<string> result = new List<string>();
                    for (int i = 0; i < keys.Count; i++)
                    {
                        List<string> lines = SplitFullTextContent(keys[i]);
                        result.AddRange(lines);
                    }

                    keys = result;
                }
                else if (strOneStyleLower == "split")
                {
                    if (bHasFoundStopword == true)
                    {
                        bool bInStopword = false;

                        nRet = this.StopwordCfg.IsInStopword(",",
                            strStopwordTableName,
                            out bInStopword,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        if (bInStopword == true)
                        {
                            strError = "�ӹ�������,��ʹ����'stopword'ȥ�����ֹ���,�ҷ������а���','����ô��ʹ��'split'����������塣";
                            return -1;
                        }
                    }
                    /*
                    if (keys.Length != 1)
                    {
                        strError = "�ӹ�������ʱ,����split��ǰ�����Ա�ɶ��������";
                        return -1;
                    }
                     */

                    List<string> result = new List<string>();
                    for (int i = 0; i < keys.Count; i++)
                    {
                        string[] tempKeys = keys[i].Split(new char[] { ',','��',' ', '��'});   // ��Ǻ�ȫ�ǵĶ��źͿո�
                        result.AddRange(tempKeys);
                    }

                    keys = result;
                }
                else if (strOneStyleLower == "stopword")
                {
                    if (this.StopwordCfg == null)
                    {
                        strError = "�ڼ������������ʹ����stopword����StopwordCfg���󲻴��ڡ�";
                        return -1;
                    }

                    // ��һ���ַ����������ȥ������
                    // parameter:
                    //		texts	���ӹ����ַ�������
                    //		strStopwordTable	����ʹ�÷������ĸ��� Ϊ""��null��ʾȡ��һ����
                    //		strError	out ������Ϣ
                    // return:
                    //		-1	����
                    //		0	�ɹ�
                    nRet = this.StopwordCfg.DoStopword(strStopwordTableName,
                        ref keys,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    bHasFoundStopword = true;
                }
                else if (strOneStyleLower == "distribute_refids")
                {
                    // 2008/10/22

                    List<string> results = new List<string>();
                    
                    
                    for (int i = 0; i < keys.Count; i++)
                    {
                        List<string> temp_ids = GetLocationRefIDs(keys[i]);
                        if (temp_ids.Count > 0)
                            results.AddRange(temp_ids);
                    }

                    keys = results;
                }
                else
                { 
                    // ����C#�ű���������
                    string strFirstChar = "";
                    if (strOneStyle.Length > 0)
                        strFirstChar = strOneStyle.Substring(0, 1);

                    // �ű�����
                    if (strFirstChar == "#")
                    {
                        string strFunctionName = strOneStyle.Substring(1);
                        if (strFunctionName == "")
                        {
                            strError = "�ӹ�������ʱ���������ַ������'" + strOneStyle + "'δд�ű���������";
                            return -1;

                        }

                        nRet = this.DoScriptFunction(dataDom,
                            strFunctionName,
                            ref keys,
                            out strError);
                        if (nRet == -1)
                            return -1;

                    }
                    else
                    {
                        strError = "�ӹ�������ʱ,�����ַ�������ʱ����֧��'" + strOneStyle + "'���";
                        return -1;
                    }
                }
            }

            return 0;
        }

        // ���ջس��������и���������С���򰴶��š���š���̾�š��ʺ������ŵ�����һ���и�
        static List<string> SplitFullTextContent(string strContent)
        {
            List<string> results = new List<string>();
            string [] parts = strContent.Split(new char []{',',
                ' ',
                '��',    // ȫ�ǿո�
                '.',
                '��',
                ':','��',
                ';','��',
                '!','��',
                '?','��',
                '��','��',
                '��','��',
                '/','\\',
                '(',')',
                '\r',
                '\n'
            });
            StringBuilder line = new StringBuilder(4096);
            for (int i = 0; i < parts.Length; i++)
            {
                string strPart = parts[i];
                if (strPart.Length + line.Length < 200)
                {
                    line.Append(strPart);
                }
                else
                {
                    results.Add(line.ToString());
                    line = new StringBuilder(strPart);
                }
            }

            if (line.Length > 0)
                results.Add(line.ToString());

            return results;
        }

        // ���ɹ��ݲ��ַ����е�refid��������
        // ��������,������ԭ���ķָ�refid�ַ��ֺ�֮��ķ��ţ�'|'�����ڷָ�refid�ķ���
        public static List<string> GetLocationRefIDs(string strText)
        {
            List<string> results = new List<string>();

            if (String.IsNullOrEmpty(strText) == true)
                return results;

            int nStart = 0;
            int nEnd = 0;
            int nPos = 0;
            for (; ; )
            {
                nStart = strText.IndexOf("{", nPos);
                if (nStart == -1)
                    break;
                nPos = nStart + 1;
                nEnd = strText.IndexOf("}", nPos);
                if (nEnd == -1)
                    break;
                nPos = nEnd + 1;
                if (nEnd <= nStart + 1)
                    continue;
                string strPart = strText.Substring(nStart + 1, nEnd - nStart - 1).Trim();

                if (String.IsNullOrEmpty(strPart) == true)
                    continue;

                string[] ids = strPart.Split(new char[] { ',', '|' });  // '|' 2010/12/6 add
                for (int j = 0; j < ids.Length; j++)
                {
                    string strID = ids[j].Trim();
                    if (String.IsNullOrEmpty(strID) == true)
                        continue;

                    results.Add(strID);
                }
            }

            return results;
        }


        // ��һ���ַ�����������ݶ���ɴ�д
        public static void DoUpper(ref List<string> texts)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                texts[i] = texts[i].ToUpper();
            }
        }

        // ��һ���ַ�����������ݶ����Сд
        public static void DoLower(ref List<string> texts)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                texts[i] = texts[i].ToLower();
            }
        }

        // ��һ���ַ�����������ݶ����Сд
        public static void RemoveBlank(ref List<string> texts)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                texts[i] = texts[i].Replace(" ", "");
                texts[i] = texts[i].Replace("��", "");
            }
        }

        // ���ƴ����ͷ��д
        public static void DoPinyinAb(ref List<string> texts)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                string strText = texts[i];

                texts[i] = PinyinAb(strText);
            }
        }

        // ���ƴ����ͷ��д
        public static string PinyinAb(string strText)
        {
            string strResult = "";
            string[] words = strText.Split(new char[] {' ','��',',','��','-','��','_','��','.','��',';','��',':','��','��','?','��','!','��','\'','\"','��','��','��','��','[',']','��','��','(',')','��','��','@','��'});
            for (int i = 0; i < words.Length; i++)
            {
                string strWord = words[i].Trim();
                if (strWord.Length == 0)
                    continue;
                char ch = strWord[0];
                if (ch < 'a' && ch > 'z')
                    continue;
                if (ch < 'A' && ch > 'Z')
                    continue;
                strResult += ch;
            }

            return strResult;
        }

        // ��һ���ַ�����������ݶ���ɼ���
        public static void DoSimplify(ref List<string> texts)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                texts[i] = API.ChineseT2S(texts[i]);
            }
        }

        // ��һ���ַ�����������ݶ���ɷ���
        public static void DoTraditionalize(ref List<string> texts)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                texts[i] = API.ChineseS2T(texts[i]);
            }
        }


        #endregion

    }


    public class KeysHost
    {
        public XmlDocument DataDom = null;
        public XmlDocument CfgDom = null;

        public string InputString = "";

        public string ResultString = "";

        public List<string> ResultStrings = new List<string>();

        public KeysHost()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public void Invoke(string strFuncName)
        {
            Type classType = this.GetType();

            // newһ��Host��������
            classType.InvokeMember(strFuncName,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.InvokeMethod
                ,
                null,
                this,
                null);

        }

    }
}
