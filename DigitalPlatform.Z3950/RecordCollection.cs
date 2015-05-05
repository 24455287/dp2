using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalPlatform.Z3950
{
    public class Record 
    {
        public byte [] m_baRecord = null;   // ԭʼ��̬������
        public string m_strSyntaxOID = "";
	    public string m_strDBName = "";
        public string m_strElementSetName = ""; // B / F

	    // �����Ϣ
	    public int m_nDiagCondition = 0;    // 0��ʾû�������Ϣ
	    public string m_strDiagSetID = "";
	    public string m_strAddInfo = "";

        public string AutoDetectedSyntaxOID = "";   // �Զ�ʶ���OID������ʹ��
    }


    public class RecordCollection : List<Record>
    {
    }
}
