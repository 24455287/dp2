using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Web;
using System.Diagnostics;

using DigitalPlatform.Xml;
using DigitalPlatform.Text;

using DigitalPlatform.dp2.Statis;
using DigitalPlatform.Script;

namespace dp2Circulation
{
    /// <summary>
    /// OperLogStatisForm (��־ͳ�ƴ�) ͳ�Ʒ�����������
    /// </summary>
    public class OperLogStatis : StatisHostBase
    {
        // ����ͳ�Ʊ��(ȫ��Χһ����)

        /// <summary>
        /// �������������� OperLogStatisForm (��־ͳ�ƴ�)
        /// </summary>
        public OperLogStatisForm OperLogStatisForm = null;	// ����

        /// <summary>
        /// ��ʼ����
        /// </summary>
        public DateTime StartDate = new DateTime(0);

        /// <summary>
        /// ��������
        /// </summary>
        public DateTime EndDate = new DateTime(0);

        /// <summary>
        /// ��ñ�ʾ���ڷ�Χ���ַ���
        /// </summary>
        /// <returns>���ڷ�Χ�ַ���</returns>
        public string GetTimeRangeString()
        {
            string strStart = StartDate.ToLongDateString();
            string strEnd = EndDate.ToLongDateString();

            if (strStart == strEnd)
                return strStart;

            return strStart + "-" + strEnd;
        }

        // ��ǰ���ڣ��������в��ϱ䶯
        /// <summary>
        /// ��ǰ���ڴ��������
        /// </summary>
        public DateTime CurrentDate = new DateTime(0);

#if NO
        private bool disposed = false;
        public WebBrowser Console = null;
        public string ProjectDir = "";  // ����Դ�ļ�����Ŀ¼
        public string InstanceDir = ""; // ��ǰʵ����ռ��Ŀ¼�����ڴ洢��ʱ�ļ�

        public List<string> OutputFileNames = new List<string>(); // ��������html�ļ�

        int m_nFileNameSeed = 1;
#endif

        /// <summary>
        /// ��ǰ���ڴ������־�ļ��������ļ���
        /// </summary>
        public string CurrentLogFileName = "";    // ��ǰ��־�ļ���(���ļ���)

        /// <summary>
        /// ��ǰ��־��¼���ļ��е��±� (�� 0 ��ʼ����)
        /// </summary>
        public long CurrentRecordIndex = -1; // ��ǰ��־��¼���ļ��е�ƫ����

        string m_strXml = "";    // ��־��¼��
        /// <summary>
        /// ��ǰ���ڴ������־��¼ XML ��¼���ַ�������
        /// </summary>
        public string Xml
        {
            get
            {
                return this.m_strXml;
            }
            set
            {
                this.m_strXml = value;
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public OperLogStatis()
		{
			//
			// TODO: Add constructor logic here
			//
		}

        internal override string GetOutputFileNamePrefix()
        {
            return Path.Combine(this.OperLogStatisForm.MainForm.DataDir, "~operlog_statis");
        }

#if NO
        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method 
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~OperLogStatis()      
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }


        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue 
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }


        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the 
        // runtime from inside the finalizer and you should not reference 
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                }

                // ɾ����������ļ�
                if (this.OutputFileNames != null)
                {
                    Global.DeleteFiles(this.OutputFileNames);
                    this.OutputFileNames = null;
                }

                /*
                // Call the appropriate methods to clean up 
                // unmanaged resources here.
                // If disposing is false, 
                // only the following code is executed.
                CloseHandle(handle);
                handle = IntPtr.Zero;
                 * */
                try // 2009/10/21
                {
                    this.FreeResources();
                }
                catch
                {
                }
            }
            disposed = true;
        }

        // 2009/10/21
        public virtual void FreeResources()
        {
        }

        // ��ʼ��
        public virtual void OnInitial(object sender, StatisEventArgs e)
        {

        }

        // ��ʼ
        public virtual void OnBegin(object sender, StatisEventArgs e)
        {

        }

        // ÿһ��¼����
        public virtual void OnRecord(object sender, StatisEventArgs e)
        {

        }

        // ����
        public virtual void OnEnd(object sender, StatisEventArgs e)
        {

        }

        // ��ӡ���
        public virtual void OnPrint(object sender, StatisEventArgs e)
        {

        }


        public void ClearConsoleForPureTextOutputing()
        {
            Global.ClearForPureTextOutputing(this.Console);
        }

        public void WriteToConsole(string strText)
        {
            Global.WriteHtml(this.Console, strText);
        }

        public void WriteTextToConsole(string strText)
        {
            Global.WriteHtml(this.Console, HttpUtility.HtmlEncode(strText));
        }

        // ���һ���µ�����ļ���
        public string NewOutputFileName()
        {
            string strFileNamePrefix = this.OperLogStatisForm.MainForm.DataDir + "\\~statis";

            string strFileName = strFileNamePrefix + "_" + this.m_nFileNameSeed.ToString() + ".html";

            this.m_nFileNameSeed++;

            this.OutputFileNames.Add(strFileName);

            return strFileName;
        }

        // ���ַ�������д���ı��ļ�
        public void WriteToOutputFile(string strFileName,
            string strText,
            Encoding encoding)
        {
            StreamWriter sw = new StreamWriter(strFileName,
                false,	// append
                encoding);
            sw.Write(strText);
            sw.Close();
        }

        // ɾ��һ������ļ�
        public void DeleteOutputFile(string strFileName)
        {
            int nIndex = this.OutputFileNames.IndexOf(strFileName);
            if (nIndex != -1)
                this.OutputFileNames.RemoveAt(nIndex);

            try
            {
                File.Delete(strFileName);
            }
            catch
            {
            }
        }

#endif
        // �°汾
        // ���һ��modifyprice����amerce�����͵���־��¼�е��޸�ΥԼ���ϸ��
        // return:
        //      -1  error
        //      0   �ɹ�
        /// <summary>
        /// ���һ��modifyprice����amerce�����͵���־��¼�е��޸�ΥԼ���ϸ��
        /// </summary>
        /// <param name="domOperLog">��־��¼ XmlDocument ����</param>
        /// <param name="prices">���Եļ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����  0: �ɹ�</returns>
        public static int ComputeAmerceModifiedPrice(XmlDocument domOperLog,
            out List<PricePair> prices,
            out string strError)
        {
            strError = "";
            prices = new List<PricePair>();

            string strAction = DomUtil.GetElementText(domOperLog.DocumentElement,
                "action");

            if (strAction != "modifyprice"
                && strAction != "amerce")
            {
                strError = "���������� '" + strAction + "', �������ڵ���ComputeAmerceModifiedPrice()������Ӧ����modifyprice/amerce����";
                return -1;
            }

            List<IdPrice> list = null;
            int nRet = 0;

#if NO
            // ������־��¼�е�<oldReaderRecord>Ԫ����Ƕ�ı�(����һ��XML��¼)�е�<overdue>Ԫ�ش���ID-�۸��б�
            nRet = BuildIdPriceListFromOverdueTag(
                domOperLog,
                "oldReaderRecord",
                out list,
                out strError);
                        if (nRet == 0)
                return 0;
#endif
            nRet = BuildIdPriceListFromAmerceRecordTag(
    domOperLog,
    true,
    out list,
    out strError);
            if (nRet == -1)
                return -1;
            if (list.Count == 0)
                return 0;


            XmlNodeList nodes = domOperLog.DocumentElement.SelectNodes("amerceItems/amerceItem");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strID = DomUtil.GetAttr(node, "id");
                string strNewPrice = DomUtil.GetAttr(node, "newPrice");

                // 2012/3/23
                if (string.IsNullOrEmpty(strNewPrice) == true)
                    continue;

                // 
                string strPureNewPrice = PriceUtil.GetPurePrice(strNewPrice);

                // 2012/3/23
                if (string.IsNullOrEmpty(strPureNewPrice) == true)
                    continue;


                // 
                string strDebug = "";
                string strOldPrice = GetPriceByID(list, strID, out strDebug);
                if (strOldPrice == null)
                {
                    strError = "��־�ļ���ʽ����: ����id '" + strID + "' ����־��¼<oldReaderRecord>Ԫ���ı���<overdue>Ԫ����û���ҵ���Ӧ�����debug: " + strDebug;
                    return -1;
                }

                PricePair pair = new PricePair();
                pair.OldPrice = strOldPrice;
                pair.NewPrice = strNewPrice;

                prices.Add(pair);
            }

            return 0;
        }

        // ��װ��İ汾��Ϊ�˼�����ǰ�İ汾
        /// <summary>
        /// ����һ��amerce��undo����������־��¼�е����ΥԼ���ܶ������ֹ�İ汾
        /// </summary>
        /// <param name="domOperLog">��־��¼ XmlDocument ����</param>
        /// <param name="prices">����ַ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����  0: �ɹ�</returns>
        public static int ComputeAmerceOrUndoPrice(XmlDocument domOperLog,
    out List<string> prices,
    out string strError)
        {
            return ComputeAmerceOrUndoPrice(domOperLog,
                null,
                out prices,
                out strError);
        }

        /*
amerce ����

<root>
<operation>amerce</operation> ��������
<action>amerce</action> ���嶯������amerce undo modifyprice
<readerBarcode>...</readerBarcode> ����֤����
<!-- <idList>...<idList> ID�б����ż�� �ѷ�ֹ -->
<amerceItems>
<amerceItem id="..." newPrice="..." comment="..." />
...
</amerceItems>
<amerceRecord recPath='...'><root><itemBarcode>0000001</itemBarcode><readerBarcode>R0000002</readerBarcode><state>amerced</state><id>632958375041543888-1</id><over>31day</over><borrowDate>Sat, 07 Oct 2006 09:04:28 GMT</borrowDate><borrowPeriod>30day</borrowPeriod><returnDate>Thu, 07 Dec 2006 09:04:27 GMT</returnDate><returnOperator>test</returnOperator></root></amerceRecord> �ڷ�����д������¼�¼��ע��<amerceRecord>Ԫ�ؿ����ظ���<amerceRecord>Ԫ�����������<itemBarcode><readerBarcode><id>�Ⱦ߱����㹻����Ϣ��
<operator>test</operator> ������
<operTime>Fri, 08 Dec 2006 10:09:36 GMT</operTime> ����ʱ��
  
<readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼
</root>

<root>
<operation>amerce</operation> 
<action>undo</action> 
<readerBarcode>...</readerBarcode> ����֤����
<!-- <idList>...<idList> ID�б����ż�� �ѷ�ֹ -->
<amerceItems>
<amerceItem id="..." newPrice="..."/>
...
</amerceItems>
<amerceRecord recPath='...'><root><itemBarcode>0000001</itemBarcode><readerBarcode>R0000002</readerBarcode><state>amerced</state><id>632958375041543888-1</id><over>31day</over><borrowDate>Sat, 07 Oct 2006 09:04:28 GMT</borrowDate><borrowPeriod>30day</borrowPeriod><returnDate>Thu, 07 Dec 2006 09:04:27 GMT</returnDate><returnOperator>test</returnOperator></root></amerceRecord> Undo��ȥ���ķ�����¼
<operator>test</operator> 
<operTime>Fri, 08 Dec 2006 10:12:20 GMT</operTime> 
  
<readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼

</root>

 * */
        // �°汾�������ַ�������
        // ����һ��amerce��undo����������־��¼�е����ΥԼ���ܶ�
        /// <summary>
        /// ����һ��amerce��undo����������־��¼�е����ΥԼ���ܶ�
        /// </summary>
        /// <param name="domOperLog">��־��¼ XmlDocument ����</param>
        /// <param name="strReasonHead">��ʾ�������ɵ������ַ���</param>
        /// <param name="prices">����ַ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����  0: �ɹ�</returns>
        public static int ComputeAmerceOrUndoPrice(XmlDocument domOperLog,
            string strReasonHead,
            out List<string> prices,
            out string strError)
        {
            strError = "";
            prices = new List<string>();

            string strAction = DomUtil.GetElementText(domOperLog.DocumentElement,
                "action");

            List<IdPrice> list = null;
            int nRet = 0;

            if (strAction == "modifyprice")
            {
                strError = "����������modifyprice�������ɱ���������Ӧ���� ComputeAmerceModifiedPrice() ��������";
                return -1;
            }

            // ������־��¼�е� <amerceRecord> Ԫ�ش���ID-�۸��б�
            nRet = BuildIdPriceListFromAmerceRecordTag(domOperLog,
                false,
                out list,
                out strError);
            if (nRet == -1)
                return -1;

            XmlNodeList nodes = domOperLog.DocumentElement.SelectNodes("amerceItems/amerceItem");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strID = DomUtil.GetAttr(node, "id");
                string strNewPrice = DomUtil.GetAttr(node, "newPrice");

                string strDebug = "";

                if (string.IsNullOrEmpty(strReasonHead) == false)
                {
                    string strReason = GetReasonByID(list, strID, out strDebug);
                    if (StringUtil.HasHead(strReason, strReasonHead) == false)
                        continue;
                }

                if (strAction == "amerce")
                {
                    // ���û���¼۸񣬾��ҵ��ɼ۸�
                    if (String.IsNullOrEmpty(strNewPrice) == true)
                    {
                        strNewPrice = GetPriceByID(list, strID, out strDebug);
                        if (strNewPrice == null)
                        {
                            strError = "��־�ļ���ʽ����: ����id '" + strID + "' ��<amerceRecord>Ԫ����û���ҵ���Ӧ�����";
                            return -1;
                        }
                    }
                }
                if (strAction == "undo")
                {
                    strNewPrice = GetPriceByID(list, strID, out strDebug);
                    if (strNewPrice == null)
                    {
                        strError = "��־�ļ���ʽ����: ����id '" + strID + "' ��<amerceRecord>Ԫ����û���ҵ���Ӧ�����";
                        return -1;
                    }

                    // �ҵ�ID�б�

                    // �ڶ��߼�¼�е�<overdues>���ҵ���Ӧ��<overdue>Ԫ�أ�Ȼ����priceԪ���еõ��۸�

                    // Ҳ���Ա�����־��¼��<amerceRecord>Ԫ�أ��ҵ�idƥ��ļ�¼��
                }

                prices.Add(strNewPrice);
            }

            return 0;
        }

        /*
amerce ����

<root>
  <operation>amerce</operation> ��������
  <action>amerce</action> ���嶯������amerce undo modifyprice
  <readerBarcode>...</readerBarcode> ����֤����
  <!-- <idList>...<idList> ID�б����ż�� �ѷ�ֹ -->
  <amerceItems>
	<amerceItem id="..." newPrice="..." comment="..." />
	...
  </amerceItems>
  <amerceRecord recPath='...'><root><itemBarcode>0000001</itemBarcode><readerBarcode>R0000002</readerBarcode><state>amerced</state><id>632958375041543888-1</id><over>31day</over><borrowDate>Sat, 07 Oct 2006 09:04:28 GMT</borrowDate><borrowPeriod>30day</borrowPeriod><returnDate>Thu, 07 Dec 2006 09:04:27 GMT</returnDate><returnOperator>test</returnOperator></root></amerceRecord> �ڷ�����д������¼�¼��ע��<amerceRecord>Ԫ�ؿ����ظ���<amerceRecord>Ԫ�����������<itemBarcode><readerBarcode><id>�Ⱦ߱����㹻����Ϣ��
  <operator>test</operator> ������
  <operTime>Fri, 08 Dec 2006 10:09:36 GMT</operTime> ����ʱ��
  
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼
</root>

<root>
  <operation>amerce</operation> 
  <action>undo</action> 
  <readerBarcode>...</readerBarcode> ����֤����
  <!-- <idList>...<idList> ID�б����ż�� �ѷ�ֹ -->
  <amerceItems>
	<amerceItem id="..." newPrice="..."/>
	...
  </amerceItems>
  <amerceRecord recPath='...'><root><itemBarcode>0000001</itemBarcode><readerBarcode>R0000002</readerBarcode><state>amerced</state><id>632958375041543888-1</id><over>31day</over><borrowDate>Sat, 07 Oct 2006 09:04:28 GMT</borrowDate><borrowPeriod>30day</borrowPeriod><returnDate>Thu, 07 Dec 2006 09:04:27 GMT</returnDate><returnOperator>test</returnOperator></root></amerceRecord> Undo��ȥ���ķ�����¼
  <operator>test</operator> 
  <operTime>Fri, 08 Dec 2006 10:12:20 GMT</operTime> 
  
  <readerRecord recPath='...'>...</readerRecord>	���¶��߼�¼

</root>

         * */
        // ����һ��amerce��undo����������־��¼�е����ΥԼ���ܶ�
        /// <summary>
        /// ����һ��amerce��undo����������־��¼�е����ΥԼ���ܶ������ֹ�İ汾
        /// </summary>
        /// <param name="domOperLog">��־��¼ XmlDocument ����</param>
        /// <param name="nCount">������</param>
        /// <param name="total_price">�ܽ��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����  0: �ɹ�</returns>
        public static int ComputeAmerceOrUndoPrice(XmlDocument domOperLog,
            out int nCount,
            out decimal total_price,
            out string strError)
        {
            nCount = 0;
            total_price = 0;
            strError = "";

            string strAction = DomUtil.GetElementText(domOperLog.DocumentElement,
                "action");

            List<IdPrice> list = null;
            int nRet = 0;

            if (strAction == "modifyprice")
            {
                strError = "����������modifyprice�������ɱ���������Ӧ����ComputeAmerceModifiedPrice()��������";
                return -1;
            }



            // ������־��¼�е�<amerceRecord>Ԫ�ش���ID-�۸��б�
            nRet = BuildIdPriceListFromAmerceRecordTag(domOperLog,
                false,
                out list,
                out strError);
            if (nRet == -1)
                return -1;

            XmlNodeList nodes = domOperLog.DocumentElement.SelectNodes("amerceItems/amerceItem");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strID = DomUtil.GetAttr(node, "id");
                string strNewPrice = DomUtil.GetAttr(node, "newPrice");

                string strDebug = "";

                if (strAction == "amerce")
                {
                    // ���û���¼۸񣬾��ҵ��ɼ۸�
                    if (String.IsNullOrEmpty(strNewPrice) == true)
                    {
                        strNewPrice = GetPriceByID(list, strID, out strDebug);
                        if (strNewPrice == null)
                        {
                            strError = "��־�ļ���ʽ����: ����id '" + strID + "' ��<amerceRecord>Ԫ����û���ҵ���Ӧ�����";
                            return -1;
                        }
                    }
                }
                if (strAction == "undo")
                {
                    strNewPrice = GetPriceByID(list, strID, out strDebug);
                    if (strNewPrice == null)
                    {
                        strError = "��־�ļ���ʽ����: ����id '" + strID + "' ��<amerceRecord>Ԫ����û���ҵ���Ӧ�����";
                        return -1;
                    }

                    // �ҵ�ID�б�

                    // �ڶ��߼�¼�е�<overdues>���ҵ���Ӧ��<overdue>Ԫ�أ�Ȼ����priceԪ���еõ��۸�

                    // Ҳ���Ա�����־��¼��<amerceRecord>Ԫ�أ��ҵ�idƥ��ļ�¼��
                }

                // �ۼ�strNewPrice
                string strPurePrice = PriceUtil.GetPurePrice(strNewPrice);
                decimal price = 0;
                try
                {
                    price = Convert.ToDecimal(strPurePrice);
                }
                catch
                {
                    strError = "�۸��ַ��� '" + strPurePrice + "' ��ʽ����1��";
                    return -1;
                }
                total_price += price;
            }

            nCount = nodes.Count;

            return 0;
        }

        // �ɰ汾
        // ����һ��modifyprice����amerce�����͵���־��¼�е��޸�ΥԼ���ܶ�
        // return:
        //      -1  error
        //      9   û���ҵ����Ԫ��
        //      1   �ɹ�
        /// <summary>
        /// ����һ��modifyprice����amerce�����͵���־��¼�е��޸�ΥԼ���ܶ
        /// ���Ǽ�����ֹ�İ汾
        /// </summary>
        /// <param name="domOperLog">��ֹ��¼ XmlDocument ����</param>
        /// <param name="nCount">�����������</param>
        /// <param name="inc_price">���ӵĽ��</param>
        /// <param name="dec_price">���ٵĽ��</param>
        /// <param name="total_delta_price">����ı䶯���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û���ҵ��� 1: �ҵ�</returns>
        public static int ComputeAmerceModifiedPrice(XmlDocument domOperLog,
            out int nCount,
            out decimal inc_price,
            out decimal dec_price,
            out decimal total_delta_price,
            out string strError)
        {
            nCount = 0;
            inc_price = 0;
            dec_price = 0;
            total_delta_price = 0;
            strError = "";

            string strAction = DomUtil.GetElementText(domOperLog.DocumentElement,
                "action");

            if (strAction != "modifyprice"
                && strAction != "amerce")
            {
                strError = "���������� '" + strAction + "', �������ڵ���ComputeAmerceModifiedPrice()������Ӧ����modifyprice/amerce����";
                return -1;
            }

            List<IdPrice> list = null;
            int nRet = 0;

            /*
            // ������־��¼�е�<oldReaderRecord>Ԫ����Ƕ�ı�(����һ��XML��¼)�е�<overdue>Ԫ�ش���ID-�۸��б�
            nRet = BuildIdPriceListFromOverdueTag(
                domOperLog,
                "oldReaderRecord",
                out list,
                out strError);
            if (nRet == 0)
                return 0;
             * */
            nRet = BuildIdPriceListFromAmerceRecordTag(
domOperLog,
true,
out list,
out strError);
            if (nRet == -1)
                return -1;
            if (list.Count == 0)
                return 0;


            XmlNodeList nodes = domOperLog.DocumentElement.SelectNodes("amerceItems/amerceItem");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strID = DomUtil.GetAttr(node, "id");
                string strNewPrice = DomUtil.GetAttr(node, "newPrice");

                // 2012/3/23
                if (string.IsNullOrEmpty(strNewPrice) == true)
                    continue;

                // 
                string strPureNewPrice = PriceUtil.GetPurePrice(strNewPrice);

                // 2012/3/23
                if (string.IsNullOrEmpty(strPureNewPrice) == true)
                    continue;


                decimal new_price = 0;
                try
                {
                    new_price = Convert.ToDecimal(strPureNewPrice);
                }
                catch
                {
                    strError = "�۸��ַ��� '" + strPureNewPrice + "' ��ʽ����2��";
                    return -1;
                }

                // 
                string strDebug = "";
                string strOldPrice = GetPriceByID(list, strID, out strDebug);
                if (strOldPrice == null)
                {
                    strError = "��־�ļ���ʽ����: ����id '" + strID + "' ����־��¼<oldReaderRecord>Ԫ���ı���<overdue>Ԫ����û���ҵ���Ӧ�����debug: " + strDebug;
                    return -1;
                }

                string strOldPurePrice = PriceUtil.GetPurePrice(strOldPrice);

                decimal old_price = 0;
                try
                {
                    old_price = Convert.ToDecimal(strOldPurePrice);
                }
                catch
                {
                    strError = "�۸��ַ��� '" + strOldPurePrice + "' ��ʽ����3��";
                    return -1;
                }

                decimal delta = new_price - old_price;

                if (delta > 0)
                {
                    inc_price += delta;
                }

                if (delta < 0)
                {
                    dec_price += delta;
                }


                total_delta_price += delta;
            }

            nCount = nodes.Count;

            return 1;
        }

        // ������־��¼�е�<amerceRecord>Ԫ�ش���ID-�۸��б�
        // parameters:
        //      bGetOriginPrice �Ƿ�Ҫ������� originPrice Ԫ������
        static int BuildIdPriceListFromAmerceRecordTag(
            XmlDocument domOperLog,
            bool bGetOriginPrice,
            out List<IdPrice> list,
            out string strError)
        {
            strError = "";
            list = new List<IdPrice>();

            XmlNodeList nodes = domOperLog.DocumentElement.SelectNodes("amerceRecord");
            for (int i = 0; i < nodes.Count; i++)
            {
                string strXml = nodes[i].InnerText;

                XmlDocument dom = new XmlDocument();
                try
                {
                    dom.LoadXml(strXml);
                }
                catch (Exception ex)
                {
                    strError = "��־��¼��<amerceRecord>�ı�װ��DOMʧ��: " + ex.Message;
                    return -1;
                }

                string strOriginPrice = DomUtil.GetElementText(dom.DocumentElement,
                    "originPrice").Trim();


                string strPrice = DomUtil.GetElementText(dom.DocumentElement,
                    "price").Trim();
                string strID = DomUtil.GetElementText(dom.DocumentElement,
                    "id").Trim();
                string strReason = DomUtil.GetElementText(dom.DocumentElement,
                    "reason").Trim();

                IdPrice item = new IdPrice();
                item.ID = strID;
                // �����ԭʼ������ʹ��ԭʼ���
                if (bGetOriginPrice == true && string.IsNullOrEmpty(strOriginPrice) == false)
                    item.Price = strOriginPrice;
                else
                    item.Price = strPrice;
                item.Reason = strReason;

                list.Add(item);
            }

            return 0;
        }

#if NO
        // ������־��¼�е�ĳԪ�ص���Ƕ�ı���<overdue>Ԫ�ش���ID-�۸��б�
        // oldReaderRecord Ԫ�� ���� readerRecord Ԫ��
        // parameters:
        // return:
        //      -1  error
        //      0   Ҫ�ҵ�XMLԪ�ز�����
        //      1   �ɹ�
        static int BuildIdPriceListFromOverdueTag(
            XmlDocument domOperLog,
            string strElementTag,
            out List<IdPrice> list,
            out string strError)
        {
            strError = "";
            list = new List<IdPrice>();

            XmlNode nodeRecord = domOperLog.DocumentElement.SelectSingleNode(strElementTag);
            if (nodeRecord == null)
            {
                strError = "Ԫ�� <" +strElementTag+ "> ����־��¼�в�����";
                return 0;
            }

            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(nodeRecord.InnerText);
            }
            catch (Exception ex)
            {
                strError = "Ԫ�� <" + strElementTag + "> ���ı�װ��XMLDOMʱ��������: " + ex.Message;
                return -1;
            }

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("overdues/overdue");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strPrice = DomUtil.GetAttr(node,
                    "price").Trim();
                string strID = DomUtil.GetAttr(node,
                    "id").Trim();
                string strReason = DomUtil.GetAttr(node,
                    "reason").Trim();

                IdPrice item = new IdPrice();
                item.ID = strID;
                item.Price = strPrice;
                item.Reason = strReason;

                list.Add(item);
            }

            return 1;
        }
#endif

        // ��list�и���id�ҵ���Ӧ�ļ۸��ַ���
        // return:
        //      null    û���ҵ�
        //      ����    �ҵ��ļ۸��ַ���
        static string GetPriceByID(List<IdPrice> list,
            string strID,
            out string strDebug)
        {
            strDebug = "";

            strID = strID.Trim();

            for (int i = 0; i < list.Count; i++)
            {
                IdPrice item = list[i];

                strDebug += "item[" + i.ToString() + "] id=" + item.ID + ", price=" + item.Price + ", reason="+item.Reason+";\r\n";

                if (strID == item.ID)
                    return item.Price;
            }

            return null;    // not found
        }

        // ��list�и���id�ҵ���Ӧ�� Reason �ַ���
        // return:
        //      null    û���ҵ�
        //      ����    �ҵ��ļ۸��ַ���
        static string GetReasonByID(List<IdPrice> list,
            string strID,
            out string strDebug)
        {
            strDebug = "";

            strID = strID.Trim();

            for (int i = 0; i < list.Count; i++)
            {
                IdPrice item = list[i];

                strDebug += "item[" + i.ToString() + "] id=" + item.ID + ", price=" + item.Price + ", reason=" + item.Reason + ";\r\n";

                if (strID == item.ID)
                    return item.Reason;
            }

            return null;    // not found
        }

        // ���ܼ۸��
        // NewPrice ��ȥ OldPrice
        /// <summary>
        /// ���ܽ��ԡ��㷨�� NewPrice ��ȥ OldPrice
        /// </summary>
        /// <param name="pairs">Ҫ���ܵĽ��Լ���</param>
        /// <param name="strResult">����ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����  0: �ɹ�</returns>
        public static int TotalPricePair(
            List<PricePair> pairs,
            out string strResult,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            strResult = "";

            List<string> prices = new List<string>();

            foreach (PricePair pair in pairs)
            {
                if (string.IsNullOrEmpty(pair.NewPrice) == false)
                    prices.Add(pair.NewPrice);

                if (string.IsNullOrEmpty(pair.OldPrice) == false)
                {
                    string strResultPrice = "";
                    // ������"-123.4+10.55-20.3"�ļ۸��ַ�����ת������
                    // parameters:
                    //      bSum    �Ƿ�Ҫ˳�����? true��ʾҪ����
                    nRet = PriceUtil.NegativePrices(pair.OldPrice,
                        true,
                        out strResultPrice,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    prices.Add(strResultPrice);
                }

            }

            nRet = PriceUtil.TotalPrice(prices,
                out strResult,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }
    }

    // ID-�۸��Ԫ��
    internal class IdPrice
    {
        public string ID = "";
        public string Price = "";
        public string Reason = "";  // 2013/6/14
    }

    /// <summary>
    /// ����ʱ�䷶Χ���Եı��ļ���
    /// </summary>
    public class TimeRangedStatisTableCollection : List<TimeRangedStatisTable>
    {
        bool m_bAllInOne = false;
        bool m_bYear = false;
        bool m_bMonth = false;
        bool m_bDay = false;
        int m_nColumnsHint = 0;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="nColumnHint">��Ŀ����ʾ</param>
        /// <param name="bAllInOne">�� ��ֹ��Χ </param>
        /// <param name="bYear">�� ÿһ��</param>
        /// <param name="bMonth">�� ÿһ��</param>
        /// <param name="bDay">�� ÿһ��</param>
        public TimeRangedStatisTableCollection(
            int nColumnHint,
            bool bAllInOne,
            bool bYear,
            bool bMonth,
            bool bDay)
        {
            this.m_nColumnsHint = nColumnHint;
            this.m_bAllInOne = bAllInOne;
            this.m_bYear = bYear;
            this.m_bMonth = bMonth;
            this.m_bDay = bDay;
        }

        /// <summary>
        /// д��һ����Ԫ��ֵ
        /// </summary>
        /// <param name="currentTime">��ǰʱ��</param>
        /// <param name="strEntry">������</param>
        /// <param name="nColumn">�к�</param>
        /// <param name="value">ֵ</param>
        public void SetValue(
            DateTime currentTime,
            string strEntry,
            int nColumn,
            object value)
        {
            if (this.m_bAllInOne == true)
            {
                TimeRangedStatisTable table = GetTable(currentTime, "", this.m_nColumnsHint);
                table.Table.SetValue(strEntry,
                    nColumn, value);
            }
            if (this.m_bYear == true)
            {
                TimeRangedStatisTable table = GetTable(currentTime, "year", this.m_nColumnsHint);
                table.Table.SetValue(strEntry,
                    nColumn, value);
            }
            if (this.m_bMonth == true)
            {
                TimeRangedStatisTable table = GetTable(currentTime, "month", this.m_nColumnsHint);
                table.Table.SetValue(strEntry,
                    nColumn, value);
            }
            if (this.m_bDay == true)
            {
                TimeRangedStatisTable table = GetTable(currentTime, "day", this.m_nColumnsHint);
                table.Table.SetValue(strEntry,
                    nColumn, value);
            }
        }

        static void Inc(TimeRangedStatisTable table, 
            string strEntry,
            int nColumn,
            string strPrice)
        {
            Line line = table.Table.EnsureLine(strEntry);
            string strOldValue = (string)line[nColumn];
            if (string.IsNullOrEmpty(strOldValue) == true)
            {
                line.SetValue(nColumn, strPrice);
                return;
            }

            // ���������۸��ַ���
            string strPrices = PriceUtil.JoinPriceString(strOldValue,
                    strPrice);

            string strError = "";
            List<string> prices = null;
            // ������"-123.4+10.55-20.3"�ļ۸��ַ����и�Ϊ�����ļ۸��ַ����������Դ���������
            // return:
            //      -1  error
            //      0   succeed
            int nRet = PriceUtil.SplitPrices(strPrices,
                out prices,
                out strError);
            if (nRet == -1)
                throw new Exception(strError);

            string strResult = "";
            nRet = PriceUtil.TotalPrice(prices,
out strResult,
out strError);
            if (nRet == -1)
                throw new Exception(strError);

            line.SetValue(nColumn, strResult);
        }

        /// <summary>
        /// ����һ����Ԫ�Ľ��
        /// </summary>
        /// <param name="currentTime">��ǰʱ��</param>
        /// <param name="strEntry">������</param>
        /// <param name="nColumn">�к�</param>
        /// <param name="strPrice">����ַ���</param>
        public void IncPrice(
            DateTime currentTime,
            string strEntry,
            int nColumn,
            string strPrice)
        {
            // 2013/6/14
            if (string.IsNullOrEmpty(strPrice) == true)
                return;

            TimeRangedStatisTable table = null;
            if (this.m_bAllInOne == true)
            {
                table = GetTable(currentTime, "", this.m_nColumnsHint);
                Inc(table, strEntry, nColumn, strPrice);
            }
            if (this.m_bYear == true)
            {
                table = GetTable(currentTime, "year", this.m_nColumnsHint);
                Inc(table, strEntry, nColumn, strPrice);
            }
            if (this.m_bMonth == true)
            {
                table = GetTable(currentTime, "month", this.m_nColumnsHint);
                Inc(table, strEntry, nColumn, strPrice);
            }
            if (this.m_bDay == true)
            {
                table = GetTable(currentTime, "day", this.m_nColumnsHint);
                Inc(table, strEntry, nColumn, strPrice);
            }
        }

        /// <summary>
        /// ����һ����Ԫ������ֵ
        /// </summary>
        /// <param name="currentTime">��ǰʱ��</param>
        /// <param name="strEntry">������</param>
        /// <param name="nColumn">�к�</param>
        /// <param name="createValue">����ֵ</param>
        /// <param name="incValue">����ֵ</param>
        public void IncValue(
            DateTime currentTime,
            string strEntry,
            int nColumn,
            Int64 createValue,
            Int64 incValue)
        {
            if (this.m_bAllInOne == true)
            {
                TimeRangedStatisTable table = GetTable(currentTime, "", this.m_nColumnsHint);
                table.Table.IncValue(strEntry,
                    nColumn, createValue, incValue);
            }
            if (this.m_bYear == true)
            {
                TimeRangedStatisTable table = GetTable(currentTime, "year", this.m_nColumnsHint);
                table.Table.IncValue(strEntry,
                    nColumn, createValue, incValue);
            }
            if (this.m_bMonth == true)
            {
                TimeRangedStatisTable table = GetTable(currentTime, "month", this.m_nColumnsHint);
                table.Table.IncValue(strEntry,
                    nColumn, createValue, incValue);
            }
            if (this.m_bDay == true)
            {
                TimeRangedStatisTable table = GetTable(currentTime, "day", this.m_nColumnsHint);
                table.Table.IncValue(strEntry,
                    nColumn, createValue, incValue);
            }

        }

        // 
        /// <summary>
        /// ���һ���ʵ��ı�������ǰ�����ڣ����Զ�����
        /// </summary>
        /// <param name="currentTime">��ǰʱ��</param>
        /// <param name="strStyle">�������Ϊ���ַ������� year/month/day</param>
        /// <param name="nColumnsHint">��Ŀ����ʾ</param>
        /// <returns>TimeRangedStatisTable ���͵ı�����</returns>
        public TimeRangedStatisTable GetTable(DateTime currentTime,
            string strStyle,
            int nColumnsHint)
        {
            int nCurYear = currentTime.Year;

            if (strStyle != ""
                && strStyle != "year"
                && strStyle != "month"
                && strStyle != "day")
            {
                throw new Exception("�޷�ʶ���strStyle����ֵ'" + strStyle + "'");
            }


            for (int i = 0; i < this.Count; i++)
            {
                TimeRangedStatisTable table = this[i];

                if (strStyle == "year")
                {
                    if (table.Style == "year"
                        && table.StartTime.Year == currentTime.Year)
                        return table;
                }
                else if (strStyle == "month")
                {
                    if (table.Style == "month"
                        && table.StartTime.Year == currentTime.Year
                        && table.StartTime.Month == currentTime.Month)
                        return table;
                }
                else if (strStyle == "day")
                {
                    if (table.Style == "day"
                        && table.StartTime.Year == currentTime.Year
                        && table.StartTime.Month == currentTime.Month
                        && table.StartTime.Day == currentTime.Day)
                        return table;
                }
                else if (strStyle == "")
                {
                    if (table.Style == "")
                    {
                        if (currentTime > table.EndTime)
                            table.EndTime = currentTime;
                        return table;
                        // ע�����ֻ����һ���ͳ�ƣ���EndTime�ͻ�Ϊ��ֵ
                    }
                }

            }

            // û���ҵ�������һ���µı�
            TimeRangedStatisTable newTable = new TimeRangedStatisTable();
            newTable.StartTime = currentTime;
            newTable.Table = new Table(nColumnsHint);
            newTable.Style = strStyle;

            this.Add(newTable);
            return newTable;
        }

    }

    // 
    /// <summary>
    /// ����ʱ�䷶Χ���Եı��
    /// </summary>
    public class TimeRangedStatisTable
    {
        /// <summary>
        /// ��ʼʱ��
        /// </summary>
        public DateTime StartTime = new DateTime(0);

        /// <summary>
        /// ����ʱ��
        /// </summary>
        public DateTime EndTime = new DateTime(0);

        /// <summary>
        /// ʱ���и�����"year" "month" "day" "" 4��
        /// </summary>
        public string Style = "";   // ʱ���и�����"year" "month" "day" "" 4��

        // 
        /// <summary>
        /// ���ʱ�䷶Χ��
        /// </summary>
        public string TimeRangeName
        {
            get
            {
                if (this.Style == "year")
                    return StartTime.Year.ToString().PadLeft(4, '0') + "��";

                if (this.Style == "month")
                    return StartTime.Year.ToString().PadLeft(4, '0') + "��"
                        + StartTime.Month.ToString() + "��";

                if (this.Style == "day")
                    return StartTime.Year.ToString().PadLeft(4, '0') + "��"
                        + StartTime.Month.ToString() + "��"
                        + StartTime.Day.ToString() + "��";

                if (this.EndTime == new DateTime(0))
                {
                    return StartTime.Year.ToString().PadLeft(4, '0') + "��"
        + StartTime.Month.ToString() + "��"
        + StartTime.Day.ToString() + "��";
                }

                return StartTime.Year.ToString().PadLeft(4, '0') + "��"
                        + StartTime.Month.ToString() + "��"
                        + StartTime.Day.ToString() + "�� - "
                        + EndTime.Year.ToString().PadLeft(4, '0') + "��"
                        + EndTime.Month.ToString() + "��"
                        + EndTime.Day.ToString() + "��";
            }
        }

        /// <summary>
        /// �ں��� Table ���Ͷ���
        /// </summary>
        public Table Table = null;

        // 
        /// <summary>
        /// �Ƿ�Ϊ�յ�ʱ�䷶Χ?
        /// </summary>
        public bool IsNullTimeRange
        {
            get
            {
                if (this.StartTime == new DateTime(0)
                    && this.EndTime == new DateTime(0))
                    return true;
                return false;
            }
        }

        // 
        /// <summary>
        /// �Ƿ��������һ���ʱ�䷶Χ?
        /// </summary>
        public bool IsOneDayRange
        {
            get
            {
                if (this.StartTime == new DateTime(0)
                    && this.EndTime == new DateTime(0))
                    return false;

                if (this.EndTime == new DateTime(0))
                    return true;
                return false;
            }
        }
    }

    /// <summary>
    /// һ�Խ��
    /// </summary>
    public class PricePair
    {
        /// <summary>
        /// �½��
        /// </summary>
        public string NewPrice = "";
        /// <summary>
        /// �ɽ��
        /// </summary>
        public string OldPrice = "";
    }
}
