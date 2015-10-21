using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using DigitalPlatform.Text;

namespace DigitalPlatform.Z3950
{
    public class PolandNode : BerNode
    {
        public string m_strOrgExpression = "";
        // public stringLPSTR m_pszOrgExpression;
        public string m_strToken;			// ��ǰ�����token
        public int m_nType;				// token������

        #region ����

        public const int TYPE_AND	= 0;
        public const int TYPE_OR	= 1;
        public const int TYPE_NOT	= 2;
        public const int TYPE_OPERAND    = 3;
        public const int TYPE_NEAR	= 4;
        public const int TYPE_WITHIN	= 5;
        public const int TYPE_LEFTBRACKET = 6;		// ������
        public const int TYPE_RIGHTBRACKET = 7; 	// ������


        
        public const ushort z3950_Operand                       = 0;
        public const ushort z3950_Query                         = 1;
        public const ushort z3950_and                           = 0;
        public const ushort z3950_or                            = 1;
        public const ushort z3950_and_not                       = 2;
        public const ushort z3950_prox                          = 3;

/* ���Ʋ����ļ������� */
        public const ushort z3950_exclusion                     = 1;
        public const ushort z3950_distance                      = 2;
        public const ushort z3950_ordered                       = 3;
        public const ushort z3950_relationType                  = 4;
        public const ushort z3950_proximityUnitCode             = 5;

        public const ushort z3950_lessThan                      = 1;
        public const ushort z3950_lessThanOrEqual               = 2;
        public const ushort z3950_equal                         = 3;
        public const ushort z3950_greaterThanOrEqual            = 4;
        public const ushort z3950_greaterThan                   = 5;
        public const ushort z3950_notEqual                      = 6;
        public const ushort z3950_known                         = 1;
        public const ushort z3950_private                       = 2;

/* ��ѯ���� */
        public const ushort z3950_type_0                        = 0;
        public const ushort z3950_type_1                        = 1;
        public const ushort z3950_type_2                        = 2;
        public const ushort z3950_type_100                     =100;
        public const ushort z3950_type_101 = 101;

        #endregion


        TokenCollection m_OperatorArray = new TokenCollection();
        TokenCollection m_PolandArray = new TokenCollection();
        BerNodeStack m_StackArray = new BerNodeStack();

        public BerNode m_Subroot = new BerNode();

        public Encoding m_queryTermEncoding = Encoding.GetEncoding(936);

        int m_nOffs = 0;

        public char CurrentChar
        {
            get
            {
                if (this.m_nOffs >= this.m_strOrgExpression.Length)
                    return (char)0; // ����ԭ����ϰ��

                return this.m_strOrgExpression[this.m_nOffs];
            }
        }

        public bool ReachEnd
        {
            get
            {
                if (this.m_nOffs >= this.m_strOrgExpression.Length)
                    return true;
                return false;
            }
        }

        // ��ǰλ��ָ������ƶ�һ���ַ�
        // return:
        //      true    ����ĩβ
        //      false   û�е���ĩβ
        public bool MoveNext()
        {
            this.m_nOffs++;
            return this.ReachEnd;
        }

        public PolandNode(string strOrgExpression)
        {
        	m_strOrgExpression = strOrgExpression;
	        // m_pszOrgExpression = (char *)(LPCSTR)m_strOrgExpression;
            m_nOffs = 0;
        }


        // ��ԭʼ���ʽת��Ϊ�沨�����ʽ
        // parameters
        // return:
        //		NULL
        //		����
        public void ChangeOrgToRPN()
        {
            for (; ; )
            {
                GetAToken();
                HandlingAToken();
                if (m_strToken == "" && m_nType == -1)
                    break;
            }
            ChangeRPNToTree();
        }

        static bool IsWhite(char c)
        {
            if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                return true;
            else
                return false;
        }

        static bool IsDelim(char c)
        {
            if (" +*!()".IndexOf(c) != -1
                || c == 9 || c == '\r' || c == 0 || c == '\n' || c == ' ')
                return true;
            else
                return false;
        }


        // ������ı��ʽ�ָ�Ϊ�����ĵ�Ԫ
        // parameters
        int GetAToken()
        {
            this.m_strToken = "";
            this.m_nType = -1;


            while (IsWhite(this.CurrentChar) == true) //ȥ���ո�\t��\r��\n
            {
                ++this.m_nOffs;
            }

            // **1 �ļ���β��
            // ����������0�ַ�
            // ���أ�token��Ϊ�մ�
            //       token_type -1
            if (this.ReachEnd == true)
            {   //	���������β��
                m_strToken = "";
                m_nType = -1;
                return 0;
            }

            // **3 ����
            // ����������"()"֮һ
            // ���أ�token �����Ż�������
            //       token_type  �����Ż�������
            if (this.CurrentChar == '('
                || this.CurrentChar == ')')
            {
                m_strToken = new string(this.CurrentChar, 1);
                this.MoveNext();

                if (m_strToken == "(")
                    m_nType = TYPE_LEFTBRACKET;
                else
                    m_nType = TYPE_RIGHTBRACKET;
                return 0;
            }

            // **5 �ַ���
            // ����������"
            // ���أ�token �ַ�������ʾ����
            //       �����ַ�������Ϊ����
            if (this.CurrentChar == '"')
            {  // quoted string 
                /*
                LPSTR pTemp;
                LPTSTR pStr;
                int nLen = 0;
                 * */

                // �ҵ�������'"'
                int nRet = this.m_strOrgExpression.IndexOf('\"', this.m_nOffs + 1);
                if (nRet == -1)
                    throw new Exception("�Ƿ��ַ�������");

                nRet++;	// ����"����
                // ����Խ��"��������ķ�delimeter����
                for (; nRet < m_strOrgExpression.Length; nRet++)
                {
                    if (IsDelim(m_strOrgExpression[nRet]) == true)
                        break;
                }

                /*
                pTemp ++;	// ����"����
                while (!IsDelim(*pTemp))
                {
                    pTemp++;
                }*/

                this.m_strToken = this.m_strOrgExpression.Substring(this.m_nOffs,
                    nRet - this.m_nOffs);


                m_nType = TYPE_OPERAND;

                this.m_nOffs = nRet + 1;

                return 0;

            }

            // **6 �������ŵ��ַ���
            // ������������������������
            // ���أ�token �ַ�������ʾ���ӻ����
            //      �����ַ�������Ϊ����
            /*
            LPTSTR pStr;
            LPSTR pTemp;

            pTemp = m_pszOrgExpression;
            while (IsDelim(this.m_strOrgExpression[nStart]) == false)
            {
                nLen++;
                pTemp++;
            }
             * */
            int nLen = 0;

            for (int i = this.m_nOffs; i < this.m_strOrgExpression.Length; i++)
            {
                if (IsDelim(this.m_strOrgExpression[i]) == true)
                    break;
                nLen++;
            }

            this.m_strToken = this.m_strOrgExpression.Substring(this.m_nOffs, nLen);
            m_nType = GetReserveType(m_strToken);

            this.m_nOffs += nLen;

            return 0;

        }

        class RESERVEENTRY
        {
            public string m_strName;
            public int m_nType;

            public RESERVEENTRY(string strName, int nType)
            {
                m_strName = strName;
                m_nType = nType;
            }

        }


        RESERVEENTRY [] struReserve = new RESERVEENTRY[] {
	        new RESERVEENTRY("and",TYPE_AND),
	new RESERVEENTRY("or",TYPE_OR),
	new RESERVEENTRY("not",TYPE_NOT),
	new RESERVEENTRY("near",TYPE_NEAR),
	new RESERVEENTRY("within",TYPE_WITHIN),
	new RESERVEENTRY("",-1),
        };

        // ���ұ����ֱ��õ������ֵ���������
        // return:
        //		-1	not found
        //		����	�����ֵ���������
        int GetReserveType(string strName)
        {
            int i;
            Debug.Assert(strName != "", "");

            for (i = 0; ; i++)
            {
                if (struReserve[i].m_strName == "")
                    break;
                if (String.Compare(strName, struReserve[i].m_strName, true) == 0)
                    return struReserve[i].m_nType;
            }

            if (String.IsNullOrEmpty(strName) == true)
                return -1;
            else
                return TYPE_OPERAND;
        }


        //	����һ��Token(CString��)
        //	����ֵ����ȷ��δ������0;     ����-1;    ������1��
        //	���ʽ�����һ��Ҫ��һ��End �磺"#"
        int HandlingAToken()
        {

            //TokenΪһ��������
            if (m_nType == TYPE_LEFTBRACKET)
                return DoLeftBracket();

            //TokenΪһ��������
            if (m_nType == TYPE_RIGHTBRACKET)
                return DoRightBracket();

            //TokenΪ���ʽ������
            if (m_nType == -1)
                return DoEnd();//vv

            //TokenΪһ���������ӣ�������
            if (m_nType == TYPE_OPERAND)
                return PutTokenToArray(m_strToken, m_nType);

            //TokenΪһ�������
            return DoOperator(m_strToken, m_nType);
        }

        // ����������
        // parameters:
        int DoLeftBracket()
        {
            //	�����ջ�������������
            if (m_OperatorArray.Count == 0)
            {
                //	�ޣ���������"("��ʾ������һ����ʽ
               PutOperatorToArray("(",
                    TYPE_LEFTBRACKET);	//	������"("
            }

            PutOperatorToArray("(",
                TYPE_LEFTBRACKET);//	�����������

            return 0;
        }



        // ����������
        // parameters:
        int DoRightBracket()
        {
            int nRet;
            int index;
            for (; ; )
            {
                //	�����ջ���Ƿ�Ϊ"("��
                index = m_OperatorArray.Count - 1;
                if (index == -1)
                    return -1;

                Token token = m_OperatorArray[index]; //	�����ջ��ȡ��

                if (token.m_strToken == "(")
                {
                    m_OperatorArray.RemoveAt(index);//	ɾ����������ջ��Ԫ��
                    break;
                }

                nRet = PutTokenToArray(token.m_strToken,
                    token.m_nType);//	�������������PL
                if (nRet == -1)
                    return -1;//ֻҪ��ɾ������Ͳ��ص���delete ����

                m_OperatorArray.RemoveAt(index);//	ɾ����������ջ��Ԫ��
            }

            return 0;
        }

        // ���������
        // parameters:
        int DoEnd()
        {
            int nRet;
            int index;
            for (; ; )
            {
                //	�����ջ���Ƿ�Ϊ"("��

                index = m_OperatorArray.Count - 1;

                if (index == -1)
                {
                    return -1;
                }
                Token token = m_OperatorArray[index]; //	�����ջ��ȡ��

                if (token.m_strToken == "(")
                {
                    m_OperatorArray.RemoveAt(index);//	ɾ����������ջ��Ԫ��
                    break;
                }

                nRet = PutTokenToArray(token.m_strToken,
                    token.m_nType);//	�������������PL
                if (nRet == -1)
                    return -1;//��ʱ����delete token ,��ΪArrayδɾ�������ص���

                m_OperatorArray.RemoveAt(index);//	ɾ����������ջ��Ԫ��
            }

            index = m_OperatorArray.Count;
            if (index != -1)
                return -1;

            return 1;  //	����1����ʾ������
        }

        // ��token����m_PolandArray
        // parameters:
        int PutTokenToArray(string strToken,
            int nType)
        {
            Token token = null;

            token = new Token();
            m_PolandArray.Add(token);

            token.m_strToken = strToken;
            token.m_nType = nType;

            if (m_OperatorArray.Count == 0)
            {
                //	�ޣ���������"("��ʾ������һ����ʽ
                PutOperatorToArray("(",
                    TYPE_LEFTBRACKET);	//	������"("
            }

            return 0;
        }


        // ����operator
        // parameters:
        int DoOperator(string strToken,
            int nType)
        {
            int nRet = 0;

            //	�����ջ�������������
            if (m_OperatorArray.Count == 0)
            {
                //	�ޣ���������"("��ʾ������һ����ʽ
                PutOperatorToArray("(",
                    TYPE_LEFTBRACKET);	//	������"("

                PutOperatorToArray(strToken, nType); //	�����������
            }
            else	//	����(��ΪOP1)�����������strToken�����ȼ���
            //  ��YX(op1)���ڵ���YX(strToken)������OP1��PL��PolandArray)
            {
                int index = m_OperatorArray.Count - 1;
                if (index == -1)
                    return -1;

                Token token = m_OperatorArray[index]; //	�����ջ��ȡ��
                if (token.m_strToken != "(")
                {

                    if (Precedence(token.m_strToken) >= Precedence(strToken))
                    {
                        //	>=ʱ������OP1��PL������strToken��OP(OperatorArray)
                        nRet = PutTokenToArray(token.m_strToken, token.m_nType);
                        //	�����ջ����PL
                        if (nRet == -1)
                            return -1;//��ʱ����delete token ,��ΪArrayδɾ�������ص���

                        m_OperatorArray.RemoveAt(index);//	ɾ����������ջ��Ԫ��
                    }
                }

                PutOperatorToArray(strToken, nType); //	���������
            }

            return 0;
        }

        class PRETABLE
        {
		    public string strPrec;
		    public int Precedence;

            public PRETABLE(string strPrec,
                int nPrecedence)
            {
                this.strPrec = strPrec;
                this.Precedence = nPrecedence;
            }
	    }

        public const int HUO_PRECLASS   = 1;
        public const int YU_PRECLASS	= 2;
        public const int FEI_PRECLASS	= 3;
        public const int NEAR_PRECLASS  = 1;
        public const int WITHIN_PRECLASS = 1;


	    PRETABLE [] table = 
            new PRETABLE[]
        {
		    new PRETABLE("or",HUO_PRECLASS),
		    new PRETABLE("and",YU_PRECLASS),
		    new PRETABLE("!",FEI_PRECLASS),
		    new PRETABLE("near",NEAR_PRECLASS),
		    new PRETABLE("within",WITHIN_PRECLASS),
		    new PRETABLE("",0),
	    };

        // �������������ȼ���
        // parameters:
        // return:
        //      -1  not found
        //      ����
        int Precedence(string strToken)
        {
            Debug.Assert(String.IsNullOrEmpty(strToken) == false, "");

            for (int i = 0; i < table.Length; i++)
            {
                if (table[i].strPrec == strToken)
                {
                    return table[i].Precedence;
                }
            }

            return -1;  // δ�ҵ�
        }

        // �����������m_OperatorArray
        // parameters:
        void PutOperatorToArray(string strToken,
            int nType)
        {
            Token token = null;

            token = new Token();
            token.m_strToken = strToken;
            token.m_nType = nType;

            m_OperatorArray.Add(token);
        }

        void ReleasePoland()
        {

            m_PolandArray.Clear();

            m_OperatorArray.Clear();

        }

        // ���沨�����ʽת��Ϊһ����
        // parameters:
        // return:
        //		NULL
        //		����
        BerNode ChangeRPNToTree()
        {
            BerNode param = null;
            int nRet;
            Token token = null;

            for (int i = 0; i < this.m_PolandArray.Count; i++)
            {
                token = m_PolandArray[i];
                Debug.Assert(token != null, "");

                if (token.m_nType == TYPE_OPERAND)
                {
                    nRet = HandleOperand(token.m_strToken);
                    if (nRet == -1)
                    {
                        throw new Exception("HandleOperand Fail!");
                        return null;
                    }
                }
                else
                {
                    nRet = HandleOperator(token.m_nType,
                        token.m_strToken);
                    if (nRet == -1)
                        return null;
                }
            }

            PopFromArray(out param);
            /*
            DeleteFile("polandtree.txt");
            pParam->DumpToFile("polandtree.txt");
            */

            // ���pParam->m_ChildArray.GetSize() > 1
            // ��ʾ����Ϊ21��������21������һ��1
            m_Subroot.AddSubtree(param);

            return param;
        }

        // ��������
        // parameters:
        // return:
        //		NULL
        //		����
        int HandleOperand(string strToken)
        {
            BerNode param = null;
            int nRet = 0;

            param = new BerNode();

            param.m_cClass = ASN1_CONTEXT;
            param.m_cForm = ASN1_CONSTRUCTED;
            param.m_uTag = 0;
            param.m_strDebugInfo = "operand [" + strToken + "]";

            nRet = BuildOperand(param, strToken);
            if (nRet == -1)
            {
                throw new Exception("BuildOperand fail!");
                return -1;
            }

            PushToArray(param);

            return 0;
        }

        // ������������
        // parameters:
        int BuildOperand(BerNode param,
            string strToken)
        {

            // int nRet;
            /*	// ��ʱע�͵�
            int nSearches;

            nSearches = g_ptrResultName.GetSize();
            //  �ж�operand�Ƿ�ΪResultSetName
            for(i=0; i<nSearches; i++) {
                if(strcmp((char *)g_ptrResultName[i], strToken)==0)
                {
                    nRet = BldResultSetId(pParam,strToken);
                    return nRet;
                }
            }
            */

            return BldAttributesPlusTerm(param, strToken);
        }


        // ������ΪResultSetIdʱ
        // parameters:
        int BldResultSetId(BerNode param,
            string strToken)
        {
            param.NewChildCharNode(BerTree.z3950_ResultSetId,
                ASN1_CONTEXT,
                Encoding.UTF8.GetBytes(strToken));

            return 0;
        }


        // ������ΪAttributesPlusTermʱ
        // parameters:
        // return:
        //		NULL
        //		����
        int BldAttributesPlusTerm(BerNode param,
            string strToken)
        {
            BerNode subparam = null;
            // string strQuery = "";
            int three = 3;
            string strTerm = "";
            string strAttrType = "";
            string strAttrValue = "";

            param = param.NewChildConstructedNode(BerTree.z3950_AttributesPlusTerm,
                ASN1_CONTEXT);
            subparam = param.NewChildConstructedNode(
                BerTree.z3950_AttributeList,
                ASN1_CONTEXT);

            DivideToken(strToken,
                out strTerm,
                out strAttrType,
                out strAttrValue);

            // ȱʡֵ
            if (strAttrType == "")
                strAttrType = "1";
            if (strAttrValue == "")
                strAttrValue = "4";
            /*
            strMessage.Format("term[%s] attrtype[%s] attrvalue[%s]",
                strTerm,
                strAttrType,
                strAttrValue);
            */
            try
            {
                HandleQuery(param,
                    subparam,
                    strTerm,
                    strAttrType,
                    strAttrValue);
            }
            catch(Exception ex)
            {
                throw new Exception("BldAttributesPlusTerm() ���� token '" + strToken + "' �����г����쳣", ex);
            }

            if (strToken.IndexOf('/', 0) == -1)
            {
                BerNode seq = null;
                seq = subparam.NewChildConstructedNode(
                    ASN1_SEQUENCE,
                    ASN1_UNIVERSAL);
                // TRACE("pSeq->m_uTag=%d",pSeq->m_uTag);
                seq.NewChildIntegerNode(BerTree.z3950_AttributeType,
                    ASN1_CONTEXT,
                    BitConverter.GetBytes((Int16)three));  /* position */
                // һ����
                seq.NewChildIntegerNode(BerTree.z3950_AttributeValue,
                    ASN1_CONTEXT,
                    BitConverter.GetBytes((Int16)three));  /* position */
            }

            return 0;
        }

#if NOOOOOOOOOOOOO
// ��������ʵõ�һ��term��AttributesList
// parameters:
//		strQuery	[out]
int DivideToken(LPSTR &pszQuery,
						 CString &strQuery)
{
	BOOL bInQuote = FALSE;
	_TCHAR ch;

	strQuery.Empty();
	
	
	while(IsWhite(*pszQuery)) //ȥ���ո�\t��\r��\n
		++(pszQuery);


// **1 �ļ���β��
// ����������0�ַ�
	if (*pszQuery == 0) {   //	���������β��
		strQuery = "";
		return 0;
	}

// **3 '/'��'='
// ����������"/="֮һ
	/*
	if (strchr(" /=",*pszQuery))  { 
		strQuery = *pszQuery++;
	}
	*/
	bInQuote = FALSE;
	while(1) {
		ch = *pszQuery;
		if (ch == '\"') {
			if (bInQuote == TRUE)
				bInQuote = FALSE;
			else
				bInQuote = TRUE;
			pszQuery ++;
			continue;
		}
		if (ch == ' ' && bInQuote == FALSE)
			break;
		if (ch == '/' || ch == '=')
			break;
		pszQuery ++;
	}
	strQuery = *pszQuery ++;
	return 0;
			
// **5 term
// ������������������������
	LPTSTR pStr;
	LPSTR  pTemp;
	int nLen = 0;
	
	/*
	pTemp = pszQuery;
	while ((strchr(" /=",*pTemp) ==NULL) 
			&& (*pTemp!=0))
	{
//		printf("pTemp=%c\n",*pTemp);
		nLen++;
		pTemp++;
	}
	*/
	pStr = strQuery.GetBuffer(nLen + 1);
	memmove(pStr,pszQuery,nLen);

	strQuery.ReleaseBuffer(nLen);

	pszQuery = pTemp;

	return 0;

}
#endif 

        // new version
        // ���������ַ������������ֽ�Ϊ3������
        // ����:	�й�/1=4
        //			���� "�й�"/1=4
        // parameters:
        //		strToken	���������ַ���
        //		strTerm		[out]������
        //		strAttrType	[out]��������
        //		strAttrValue [out]����ֵ
        int DivideToken(string strToken,
                    out string strTerm,
                    out string strAttrType,
                    out string strAttrValue)
        {
            strTerm = "";
            strAttrType = "";
            strAttrValue = "";

            bool bInQuote;
            int nLen;
            //LPTSTR pszQuery;
            char ch;
            int nRet;

            int nOffs = 0;

            // ����termĩβ
            bInQuote = false;
            // pszQuery = (LPTSTR)(LPCTSTR)strToken;
            while (nOffs < strToken.Length)
            {
                ch = strToken[nOffs];
                if (ch == '\"')
                {
                    if (bInQuote == true)
                        bInQuote = false;
                    else
                        bInQuote = true;
                    nOffs++;
                    continue;
                }
                if (ch == ' ' && bInQuote == false)
                    break;
                if (ch == '/' || ch == '=')
                    break;
                nOffs++;
            }

            nLen = nOffs;
            strTerm = strToken.Substring(0, nLen);
            UnQuoteString(ref strTerm);
            strTerm = StringUtil.UnescapeString(strTerm);   // 2015/10/21

            if (nLen >= strToken.Length)
                return 0;   // 2006/11/2 add

            nRet = strToken.IndexOf("=", nLen + 1);
            if (nRet == -1)
                return 0;

            strAttrType = strToken.Substring(nLen + 1, nRet - (nLen + 1));
            strAttrValue = strToken.Substring(nRet + 1);
            return 0;
        }

        // �������ַ��������"ȥ��
        // return:
        //		TRUE	�ɹ�
        //		FALSE	���ǳ����ַ���("û�л��߲����)
        bool UnQuoteString(ref string strString)
        {
            if (strString.Length < 2)
                return false;	// �������С��2�ַ�(����byte!)����ֱ�ӷ���

            char first = (char)0;
            if (strString[0] == '\"')
            {
                first = strString[0];
            }
            else
                return false;

            if (strString[strString.Length - 1] != first)
                return false;

            strString = strString.Substring(1);
            strString = strString.Substring(0, strString.Length - 1);
            return true;
        }

        /*
����δ����Ľ����߳��쳣: 
Type: System.FormatException
Message: ݔ���ִ���ʽ�����_��
Stack:
� System.Number.StringToNumber(String str, NumberStyles options, NumberBuffer& number, NumberFormatInfo info, Boolean parseDecimal)
� System.Number.ParseInt32(String s, NumberStyles style, NumberFormatInfo info)
� System.Int16.Parse(String s, NumberStyles style, NumberFormatInfo info)
� System.Convert.ToInt16(String value)
� DigitalPlatform.Z3950.PolandNode.HandleQuery(BerNode param, BerNode subparam, String strTerm, String strAttrType, String strAttrValue)
� DigitalPlatform.Z3950.PolandNode.BldAttributesPlusTerm(BerNode param, String strToken)
� DigitalPlatform.Z3950.PolandNode.BuildOperand(BerNode param, String strToken)
� DigitalPlatform.Z3950.PolandNode.HandleOperand(String strToken)
� DigitalPlatform.Z3950.PolandNode.ChangeRPNToTree()
� DigitalPlatform.Z3950.PolandNode.ChangeOrgToRPN()
� DigitalPlatform.Z3950.BerTree.make_type_1(String strQuery, Encoding queryTermEncoding, BerNode subroot)
� DigitalPlatform.Z3950.BerTree.SearchRequest(SEARCH_REQUEST struSearch_request, Byte[]& baPackage)
� dp2Catalog.ZConnection.DoSearchAsync()
� dp2Catalog.ZConnection.ZConnection_InitialComplete(Object sender, EventArgs e)
� dp2Catalog.ZConnection.BeginCommands(List`1 commands)
� dp2Catalog.ZSearchForm.DoSearchOneServer(TreeNode nodeServerOrDatabase, String& strError)
� dp2Catalog.ZSearchForm.DoSearch()
� dp2Catalog.ZSearchForm.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.ContainerControl.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.SplitContainer.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.Control.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.ContainerControl.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.SplitContainer.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.Control.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.ContainerControl.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.SplitContainer.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.Control.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.ContainerControl.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.Control.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.TextBoxBase.ProcessDialogKey(Keys keyData)
� System.Windows.Forms.Control.PreProcessMessage(Message& msg)
� System.Windows.Forms.Control.PreProcessControlMessageInternal(Control target, Message& msg)
� System.Windows.Forms.Application.ThreadContext.PreTranslateMessage(MSG& msg)

         * */
        // new version
        // ����term��AttributesList
        // parameters:
        void HandleQuery(BerNode param,
            BerNode subparam,
            string strTerm,
            string strAttrType,
            string strAttrValue)
        {
            BerNode seq = null;

            seq = subparam.NewChildConstructedNode(
                ASN1_SEQUENCE,
                ASN1_UNIVERSAL);

            // ����term��attributeType��attributeValue

            //    ����attributeType
            try
            {
                Int16 i = Convert.ToInt16(strAttrType);

                seq.NewChildIntegerNode(BerTree.z3950_AttributeType,
                    ASN1_CONTEXT,
                    BitConverter.GetBytes(i));
            }
            catch(Exception ex)
            {
                throw new Exception("strAttrType = '"+strAttrType+"' ӦΪ���֡�", ex);
            }

            //		����attributeValue 
            try
            {
                Int16 i = Convert.ToInt16(strAttrValue);
                seq.NewChildIntegerNode(BerTree.z3950_AttributeValue,
                    ASN1_CONTEXT,
                    BitConverter.GetBytes(i));
            }
            catch (Exception ex)
            {
                throw new Exception("strAttrValue = '" + strAttrValue + "' ӦΪ���֡�", ex);
            }
            // TODO: Ϊ�����ﱻ����������?

            // term
            {
                BerNode tempnode = param.NewChildCharNode(BerTree.z3950_Term,		//	����term
                    ASN1_CONTEXT,
                    //Encoding.GetEncoding(936).GetBytes(strTerm));
                    this.m_queryTermEncoding.GetBytes(strTerm));
                tempnode.m_strDebugInfo = "term [" + strTerm + "]";
            }

            // return 0;
        }

        int PushToArray(BerNode param)
        {
            this.m_StackArray.Add(param);
            return 0;
        }

        int PopFromArray(out BerNode subparam)
        {
            subparam = null;

            if (m_StackArray.Count == 0)
            {
                Debug.Assert(false, "��ջ�ѿ�");
                return -1;
            }

            subparam = m_StackArray[m_StackArray.Count - 1];
            m_StackArray.RemoveAt(m_StackArray.Count - 1);

            return 0;
        }

        // ����operator
        // parameters:
        // return:
        //		NULL
        //		����
        int HandleOperator(int nType,
            string strToken)
        {
            BerNode param = null;
            BerNode subparam = null;
            int nRet;
            int i;

            param = new BerNode();

            param.m_cClass = ASN1_CONTEXT;
            param.m_cForm = ASN1_CONSTRUCTED;
            param.m_uTag = 1;
            param.m_strDebugInfo = "operator [" + strToken + "]";

            // �Ӷ�ջ�е�����������������
            for (i = 0; i < 2; i++)
            {
                nRet = PopFromArray(out subparam);
                if (nRet == -1)
                    return -1;
                param.AddSubtree(subparam);
            }

            // ����op
            BldOperator(param,
                nType,
                strToken);

            // �������ջ
            PushToArray(param);

            return 0;
        }


        // ����operator����
        // parameters:
        int BldOperator(BerNode param,
                                 int nType,
                                 string strToken)
        {
            BerNode subparam = null;

            // pSubparam = pParam->NewChildconstructedNode(1,ASN1_CONTEXT);
            // �˲��ƺ�Ϊ����ģ����Դ����ע�͵��Ժ��Ч��
            // 2000/11/26 changed


            subparam = param/*pSubparam*/.NewChildConstructedNode(BerTree.z3950_Operator,
                ASN1_CONTEXT);

            if (nType == TYPE_WITHIN)
                BuildWithin(subparam, strToken);
            else if (nType == TYPE_NEAR)
                BuildNear(subparam, strToken);
            else
                BuildGeneral(subparam, strToken, nType);

            return 0;

        }


        // ����within�����������
        // parameters:
        // return:
        //		NULL
        //		����
        int BuildWithin(BerNode param,
            string strToken)
        {
            Debug.Assert(false, "��δʵ��");
            /*
            BerNode seq = null;
            BerNode subparam = null;
            int distance = 1 , unit = 2;
            char *ind;
            int zero=0, one=1, two=2, three=3, four=4, five=5;
	
            if ((ind = (char *)strstr(strToken,"/")) != NULL)
            {
                sscanf(ind+1,"%d", &distance);
                if ((ind = strstr(ind+1, "/"))!=NULL)
                    sscanf(ind+1,"%d", &unit);
            }
	
            pSeq = pParam->NewChildconstructedNode(z3950_prox,
                ASN1_CONTEXT);
	
            pSeq->NewChildintegerNode(z3950_exclusion, ASN1_CONTEXT,
                (CHAR*)&zero,sizeof(zero));   // ���ų� 
	
            pSeq->NewChildintegerNode(z3950_distance, ASN1_CONTEXT,
                (CHAR*)&distance,sizeof(distance));   
	
            pSeq->NewChildintegerNode(z3950_ordered, ASN1_CONTEXT,
                (CHAR*)&one,sizeof(one));   // ordered 

            pSeq->NewChildintegerNode(z3950_relationType, 
                ASN1_CONTEXT,(CHAR*)&two,
                sizeof(two));   // z3950_lessThanOrEqual 
	
            pSubparam = pSeq->NewChildconstructedNode(
                z3950_proximityUnitCode,
                ASN1_CONTEXT);
	
            pSeq->NewChildintegerNode(z3950_known, ASN1_CONTEXT,
                (CHAR*)&unit,sizeof(unit));  

            */

            return 0;

        }


        // ����near�����������
        // parameters:
        // return:
        //		NULL
        //		����
        int BuildNear(BerNode param,
            string strToken)
        {
            Debug.Assert(false, "��δʵ��");
            /*
            CBERNode *pSeq;
            CBERNode *pSubparam;
            int distance = 1, unit = 2;
            char *ind;
            int zero=0, two=2;
            //int one=1, three=3, four=4, five=5;


            if ((ind = (char *)strstr(strToken,"/"))!=NULL)
            {
                sscanf(ind+1,"%d", &distance);
                if ((ind = strstr(ind+1, "/"))!=NULL)
                    sscanf(ind+1,"%d", &unit);
            }
	
            pSeq = pParam->NewChildconstructedNode(z3950_prox,
                ASN1_CONTEXT);

            pSeq->NewChildintegerNode(z3950_exclusion, 
                ASN1_CONTEXT,
                (CHAR*)&zero,sizeof(zero));   // ���ų� 

            pSeq->NewChildintegerNode(z3950_distance, 
                ASN1_CONTEXT,
                (CHAR*)&distance,sizeof(distance)); 
	
            pSeq->NewChildintegerNode(z3950_ordered, 
                ASN1_CONTEXT,
                (CHAR*)&zero,sizeof(zero));   // not ordered 

            pSeq->NewChildintegerNode(z3950_relationType, 
                ASN1_CONTEXT,
                (CHAR*)&two,sizeof(two));   // z3950_lessThanOrEqual 
	
            pSubparam = pSeq->NewChildconstructedNode(
                z3950_proximityUnitCode,
                ASN1_CONTEXT);
            pSeq->NewChildintegerNode(z3950_known, 
                ASN1_CONTEXT,
                (CHAR*)&unit,sizeof(unit)); 
            */
            return 0;

        }


        // �����near��within�����������
        // parameters:
        int BuildGeneral(BerNode param,
            string strToken,
            int nType)
        {
            Debug.Assert(nType <= 0xffff, "");	// ����ת��Ϊunsigned short intʱ�������

            param.NewChildCharNode((ushort)nType,
                ASN1_CONTEXT,
                Encoding.UTF8.GetBytes(strToken)); // ,0);?????

            return 0;
        }




    }

    public class Token
    {
        public string m_strToken = "";
        public int m_nType = 0;
    }

    public class TokenCollection : List<Token>
    {
    }

    public class BerNodeStack : List<BerNode>
    {

    }
}
