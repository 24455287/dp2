using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.Win32.SafeHandles;

using DigitalPlatform.Xml;
using System.Web;


namespace dp2Circulation
{
    /// <summary>
    /// ƾ����ӡ������
    /// </summary>
    public class PrintHost
    {
        /// <summary>
        /// ͳ�Ʒ����洢Ŀ¼
        /// </summary>
        public string ProjectDir = "";  // ����Դ�ļ�����Ŀ¼

        /// <summary>
        /// ��ǰ�������е�ͳ�Ʒ���ʵ���Ķ�ռĿ¼��һ�����ڴ洢ͳ�ƹ����е���ʱ�ļ�
        /// </summary>
        public string InstanceDir = ""; // ��ǰʵ����ռ��Ŀ¼�����ڴ洢��ʱ�ļ�

        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// �ű������� Assembly
        /// </summary>
        public Assembly Assembly = null;

        /*
        // ������������Ľ軹�����й���Ϣ������OnPrint()ʱʹ��
        public string CurrentReaderBarcode = "";
        public string CurrentReaderSummary = "";
        public List<BorrowItemInfo> BorrowItems = new List<BorrowItemInfo>();
        public List<ReturnItemInfo> ReturnItems = new List<ReturnItemInfo>();
         * */
        /// <summary>
        /// ��ӡ��Ϣ
        /// </summary>
        public PrintInfo PrintInfo = new PrintInfo();

        /// <summary>
        /// �Ѿ���ӡ����Ϣ����
        /// </summary>
        public List<PrintInfo> PrintedInfos = new List<PrintInfo>();

        /// <summary>
        /// PrintedInfos ���ϵ����ߴ硣ȱʡΪ 100
        /// </summary>
        public int MaxPrintedInfos = 100;  // �������ߴ�

        /// <summary>
        /// ��δ��ӡ����Ϣ����
        /// </summary>
        public List<PrintInfo> UnprintInfos = new List<PrintInfo>();

        /// <summary>
        /// UnprintInfos ���ϵ����ߴ硣ȱʡΪ 100
        /// </summary>
        public int MaxUnprintInfos = 100;    // �������ߴ�

        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void OnInitial(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// ��ӡ
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void OnPrint(object sender, PrintEventArgs e)
        {

        }

        /// <summary>
        /// ���Դ�ӡ
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void OnTestPrint(object sender, PrintEventArgs e)
        {

        }

        /// <summary>
        /// �����ӡ������
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void OnClearPrinterPreference(object sender, PrintEventArgs e)
        {

        }

        // ���󼴽����ر�
        // �����������ڲ��������Ƿ�����δ��ӡ���������
        /// <summary>
        /// ���󼴽����رա������������ڲ��������Ƿ�����δ��ӡ���������
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void OnClose(object sender, EventArgs e)
        {

        }

        // 
        /// <summary>
        /// ÿһ��ɨ�����֤������ɺ󴥷�һ��
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void OnReaderBarcodeScaned(object sender, ReaderBarcodeScanedEventArgs e)
        {

        }

        // 
        /// <summary>
        /// ÿһ�ν���ɺ󴥷�һ��
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void OnBorrowed(object sender, BorrowedEventArgs e)
        {
            if (e.ReaderBarcode != this.PrintInfo.CurrentReaderBarcode)
            {
                // ǰ����۵Ĵ�ӡ��Ϣ�����͵���Ӧ�Ķ����У�������ʱ��ѡ�����������´�ӡ
                if (String.IsNullOrEmpty(this.PrintInfo.CurrentReaderBarcode) == false
                    && this.PrintInfo.HasContent() == true
                    )
                {
                    if (this.PrintInfo.PrintedCount == 0)
                    {
                        while (this.UnprintInfos.Count >= this.MaxUnprintInfos)
                            this.UnprintInfos.RemoveAt(0);

                        this.UnprintInfos.Add(this.PrintInfo);
                    }
                    else
                    {
                        while (this.PrintedInfos.Count >= this.MaxPrintedInfos)
                            this.PrintedInfos.RemoveAt(0);

                        this.PrintedInfos.Add(this.PrintInfo);
                    }
                }

                this.PrintInfo = new PrintInfo();
            }

            this.PrintInfo.CurrentReaderBarcode = e.ReaderBarcode;
            if (String.IsNullOrEmpty(this.PrintInfo.CurrentReaderSummary) == true)
                this.PrintInfo.CurrentReaderSummary = e.ReaderSummary;

            this.PrintInfo.PrintedCount = 0;    // �Ѵ�ӡ������Ϊ0����ʾ�����Ѿ��仯���°汾(�������)������δ��ӡ

            BorrowItemInfo info = new BorrowItemInfo();
            info.OperName = e.OperName;
            info.ItemBarcode = e.ItemBarcode;
            info.BiblioSummary = e.BiblioSummary;
            info.LatestReturnDate = e.LatestReturnDate;
            info.Period = e.Period;
            info.BorrowCount = e.BorrowCount;
            info.TimeSpan = e.TimeSpan;

            info.BorrowOperator = e.BorrowOperator;
            this.PrintInfo.BorrowItems.Add(info);
        }

        // return:
        //      -1  error
        //      0   δ������
        //      1    �Ѿ�����
        internal int PushCurrentToQueue(out string strError)
        {
            strError = "";

            if (this.PrintInfo.HasContent() == false)
            {
                strError = "��ǰû�����ݣ���������";
                return 0; // ��ǰû�����ݵľͲ�������
            }

            if (this.PrintInfo.PrintedCount != 0)
            {
                strError = "��ǰ�����Ѵ����ڡ��Ѵ�ӡ���С���";
                return 0;
            }


            while (this.UnprintInfos.Count >= this.MaxUnprintInfos)
                this.UnprintInfos.RemoveAt(0);

            this.UnprintInfos.Add(this.PrintInfo);

            this.PrintInfo = new PrintInfo();
            strError = "��ǰ���ݱ����͵���δ��ӡ���С���";
            return 1;
        }

        // 
        /// <summary>
        /// ÿһ�λ���ɺ󴥷�һ��
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void OnReturned(object sender, 
            ReturnedEventArgs e)
        {
            if (e.ReaderBarcode != this.PrintInfo.CurrentReaderBarcode)
            {
                // ǰ����۵Ĵ�ӡ��Ϣ�����͵���Ӧ�Ķ����У�������ʱ��ѡ�����������´�ӡ
                if (String.IsNullOrEmpty(this.PrintInfo.CurrentReaderBarcode) == false
                    && this.PrintInfo.HasContent() == true
                    )
                {
                    if (this.PrintInfo.PrintedCount == 0)
                    {
                        while (this.UnprintInfos.Count >= this.MaxUnprintInfos)
                            this.UnprintInfos.RemoveAt(0);

                        this.UnprintInfos.Add(this.PrintInfo);
                    }
                    else
                    {
                        while (this.PrintedInfos.Count >= this.MaxPrintedInfos)
                            this.PrintedInfos.RemoveAt(0);

                        this.PrintedInfos.Add(this.PrintInfo);
                    }
                }

                this.PrintInfo = new PrintInfo();
            }

            this.PrintInfo.CurrentReaderBarcode = e.ReaderBarcode;
            if (String.IsNullOrEmpty(this.PrintInfo.CurrentReaderSummary) == true)
                this.PrintInfo.CurrentReaderSummary = e.ReaderSummary;

            this.PrintInfo.PrintedCount = 0;    // �Ѵ�ӡ������Ϊ0����ʾ�����Ѿ��仯���°汾(�������)������δ��ӡ

            // �Խ���������в���
            bool bFoundDup = false;
            for (int i = 0; i < this.PrintInfo.BorrowItems.Count; i++)
            {
                BorrowItemInfo borrow = this.PrintInfo.BorrowItems[i];
                if (borrow.ItemBarcode == e.ItemBarcode)
                {
                    this.PrintInfo.BorrowItems.RemoveAt(i);
                    bFoundDup = true;
                    break;
                }
            }

            if (bFoundDup == true)
                return;

            ReturnItemInfo info = new ReturnItemInfo();
            info.OperName = e.OperName;
            info.ItemBarcode = e.ItemBarcode;
            info.BiblioSummary = e.BiblioSummary;
            info.BorrowDate = e.BorrowDate;
            info.LatestReturnDate = e.LatestReturnDate;
            info.Period = e.Period;
            info.BorrowCount = e.BorrowCount;
            info.TimeSpan = e.TimeSpan;

            info.BorrowOperator = e.BorrowOperator;
            info.ReturnOperator = e.ReturnOperator;
            string strError = "";
            int nRet = info.BuildOverdueItems(e.OverdueString, out strError);
            if (nRet == -1)
                throw new Exception("BuildOverdueItems error: " + strError);

            this.PrintInfo.ReturnItems.Add(info);
        }

        // 
        /// <summary>
        /// ÿһ�ν�������ɺ󴥷�һ��
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void OnAmerced(object sender, AmercedEventArgs e)
        {
            if (e.ReaderBarcode != this.PrintInfo.CurrentReaderBarcode)
            {
                // ǰ����۵Ĵ�ӡ��Ϣ�����͵���Ӧ�Ķ����У�������ʱ��ѡ�����������´�ӡ
                if (String.IsNullOrEmpty(this.PrintInfo.CurrentReaderBarcode) == false
                    && this.PrintInfo.HasContent() == true
                    )
                {
                    if (this.PrintInfo.PrintedCount == 0)
                    {
                        while (this.UnprintInfos.Count >= this.MaxUnprintInfos)
                            this.UnprintInfos.RemoveAt(0);

                        this.UnprintInfos.Add(this.PrintInfo);
                    }
                    else
                    {
                        while (this.PrintedInfos.Count >= this.MaxPrintedInfos)
                            this.PrintedInfos.RemoveAt(0);

                        this.PrintedInfos.Add(this.PrintInfo);
                    }
                }

                this.PrintInfo = new PrintInfo();
            }

            this.PrintInfo.CurrentReaderBarcode = e.ReaderBarcode;
            if (String.IsNullOrEmpty(this.PrintInfo.CurrentReaderSummary) == true)
                this.PrintInfo.CurrentReaderSummary = e.ReaderSummary;

            this.PrintInfo.PrintedCount = 0;    // �Ѵ�ӡ������Ϊ0����ʾ�����Ѿ��仯���°汾(�������)������δ��ӡ

            this.PrintInfo.OverdueItems.AddRange(e.OverdueInfos);
        }


        // ���ַ������չ涨����󳤶Ƚض�
        // parameters:
        //      nMaxBytes   ���������ַ���������ٶ�ÿ�������ַ�����ʾ���Ϊ�����ַ���2��
        //      strTruncatedMask    �����ض�ʱ��β����ӵı�־������"..."
        /// <summary>
        /// ���ַ������չ涨����󳤶Ƚضϡ��ٶ�һ�����ֵ������������ַ����
        /// </summary>
        /// <param name="strText">ԭʼ�ַ���</param>
        /// <param name="nMaxBytes">���������ַ���������ٶ�ÿ�������ַ�����ʾ���Ϊ�����ַ���2��</param>
        /// <param name="strTruncatedMask">�����ض�ʱ��β����ӵı�־������"..."</param>
        /// <returns>���ص��ַ���</returns>
        public static string LimitByteWidth(string strText,
            int nMaxBytes,
            string strTruncatedMask)
        {
            string strResult = "";
            int nByteCount = 0;
            for (int i = 0; i < strText.Length; i++)
            {
                char c = strText[i];

                int v = (int)c;

                if (v < 256)
                {
                    nByteCount++;
                }
                else
                {
                    nByteCount += 2;
                }

                strResult += c;
                if (nByteCount >= nMaxBytes)
                {
                    if (String.IsNullOrEmpty(strTruncatedMask) == false)
                        strResult += strTruncatedMask;
                    break;
                }
            }

            return strResult;
        }

        // ���һ���Ҷ�����ַ���
        /// <summary>
        /// ���һ���Ҷ�����ַ������ٶ�һ�����ֵ������������ַ����
        /// </summary>
        /// <param name="strText">ԭʼ�ַ���</param>
        /// <param name="nLineBytes">һ���ڵ����ַ���</param>
        /// <returns>���ص��ַ���</returns>
        public static string RightAlignString(string strText,
            int nLineBytes)
        {
            int nRet = GetBytesWidth(strText);

            if (nRet >= nLineBytes)
                return strText; // �����Ѿ��������޷������Ҷ���

            int nDelta = nLineBytes - nRet;
            return new string(' ', nDelta) + strText;
        }

        // ���һ�����е��ַ���
        /// <summary>
        /// ���һ�����е��ַ������ٶ�һ�����ֵ������������ַ����
        /// </summary>
        /// <param name="strText">ԭʼ�ַ���</param>
        /// <param name="nLineBytes">һ���ڵ����ַ���</param>
        /// <returns>���ص��ַ���</returns>
        public static string CenterAlignString(string strText,
            int nLineBytes)
        {
            int nRet = GetBytesWidth(strText);

            if (nRet >= nLineBytes)
                return strText; // �����Ѿ��������޷����о��ж���

            int nDelta = nLineBytes - nRet;
            return new string(' ', nDelta/2) + strText;
        }

        // ��һ���ַ����ڵ��൱�������ַ����bytes��
        /// <summary>
        /// ����ַ����ڵ��൱�������ַ����ַ������ٶ�һ�����ֵ������������ַ����
        /// </summary>
        /// <param name="strText">�ַ���</param>
        /// <returns>�ַ���</returns>
        public static int GetBytesWidth(string strText)
        {
            int nByteCount = 0;
            for (int i = 0; i < strText.Length; i++)
            {
                char c = strText[i];

                int v = (int)c;

                if (v < 256)
                {
                    nByteCount++;
                }
                else
                {
                    nByteCount += 2;
                }
            }

            return nByteCount;
        }

        // ���ַ������Ϊ�̶��г����������޶������ַ���
        // ע��������ݲ���nMaxLines��������������ж����о��Ƕ�����
        // ע��ÿһ�У��������һ��ĩβ�����лس�����
        // parameters:
        //      nFirstLineMaxBytes  �������������ַ���������ٶ�ÿ�������ַ�����ʾ���Ϊ�����ַ���2��
        //      nOtherLineMaxBytes  �����е�ÿ�������ַ�������
        /// <summary>
        /// ���ַ������Ϊ�̶��г����������޶������ַ���
        /// ע��������ݲ���nMaxLines��������������ж����о��Ƕ�����
        /// ע��ÿһ�У��������һ��ĩβ�����лس�����
        /// </summary>
        /// <param name="strText">ԭʼ�ַ��ֺ�</param>
        /// <param name="nFirstLineMaxBytes">�������������ַ���������ٶ�ÿ�������ַ�����ʾ���Ϊ�����ַ���2��</param>
        /// <param name="nOtherLineMaxBytes">�����е�ÿ�������ַ�������</param>
        /// <param name="strOtherLinePrefix">�����������ǰ��������ַ���</param>
        /// <param name="nMaxLines">�������</param>
        /// <returns>���ص��ַ���</returns>
        public static string SplitLines(string strText,
            int nFirstLineMaxBytes,
            int nOtherLineMaxBytes,
            string strOtherLinePrefix,
            int nMaxLines)
        {
            string strResult = "";
            int nByteCount = 0;
            int nLineCount = 0;


            for (int i = 0; i < strText.Length; i++)
            {
                char c = strText[i];

                int v = (int)c;

                /*
                if (Char.IsLetterOrDigit(c) == true
                    || Char.IsSymbol(c) == true)
                 * */
                if (v < 256)
                {
                    nByteCount++;
                }
                else
                {
                    nByteCount += 2;
                }

                strResult += c;
                if (nLineCount == 0)    // ��һ��
                {
                    if (nByteCount >= nFirstLineMaxBytes)
                    {
                        nLineCount++;
                        if (nLineCount >= nMaxLines)
                            break;

                        if (i == strText.Length - 1)
                        {
                            strResult += "\r\n";    // ��ĩһ�к󣬲���Ҫǰ׺
                        }
                        else
                        {
                            strResult += "\r\n" + strOtherLinePrefix;
                        }

                        nByteCount = 0;

                    }
                }
                else
                {
                    // ������
                    if (nByteCount >= nOtherLineMaxBytes)
                    {
                        nLineCount++;
                        if (nLineCount >= nMaxLines)
                            break;

                        if (i == strText.Length - 1)
                        {
                            strResult += "\r\n";    // ��ĩһ�к󣬲���Ҫǰ׺
                        }
                        else
                        {
                            strResult += "\r\n" + strOtherLinePrefix;
                        }
                        nByteCount = 0;
                    }
                }
            }

            // ������ĩβû�лس����У�����
            if (strResult.Length > 0)
            {
                if (strResult[strResult.Length - 1] != '\n')
                    strResult += "\r\n";
            }

            return strResult;
        }

        // ���ַ������Ϊ�̶��г����̶��������ַ���
        // ע��������ݲ���nFixLines���������������������ֱ�������������������Ҫ��Ϊ����Ӧ�̶��߶ȴ�ӡ������
        // ע��ÿһ�У��������һ��ĩβ�����лس�����
        // parameters:
        //      nLineMaxBytes   ÿ�����������ַ���������ٶ�ÿ�������ַ�����ʾ���Ϊ�����ַ���2��
        /// <summary>
        /// ���ַ������Ϊ�̶��г����̶��������ַ���
        /// ע��������ݲ���nFixLines���������������������ֱ�������������������Ҫ��Ϊ����Ӧ�̶��߶ȴ�ӡ������
        /// ע��ÿһ�У��������һ��ĩβ�����лس�����
        /// </summary>
        /// <param name="strText">ԭʼ�ַ���</param>
        /// <param name="nLineMaxBytes">ÿ�����������ַ���������ٶ�ÿ�������ַ�����ʾ���Ϊ�����ַ���2��</param>
        /// <param name="nFixLines">�̶�������</param>
        /// <returns>���ص��ַ���</returns>
        public static string FixLines(string strText,
            int nLineMaxBytes,
            int nFixLines)
        {
            string strResult = "";
            int nByteCount = 0;
            int nLineCount = 0;
            for (int i = 0; i < strText.Length; i++)
            {
                char c = strText[i];

                int v = (int)c;

                /*
                if (Char.IsLetterOrDigit(c) == true
                    || Char.IsSymbol(c) == true)
                 * */
                if (v < 256)
                {
                    nByteCount++;
                }
                else
                {
                    nByteCount += 2;
                }

                strResult += c;
                if (nByteCount >= nLineMaxBytes)
                {
                    nLineCount++;
                    if (nLineCount >= nFixLines)
                        break;
                    strResult += "\r\n";
                    nByteCount = 0;
                }
            }

            // ����ʣ��Ŀ���
            if (nLineCount < nFixLines)
            {
                for (; nLineCount >= nFixLines; nLineCount++)
                {
                    strResult += "\r\n";
                }
            }

            return strResult;
        }

        [DllImport("kernel32.dll", CharSet=CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(string lpFileName,
            int dwDesiredAccess, 
            int dwShareMode, 
            int lpSecurityAttributes,
            int dwCreationDisposition ,
            int dwFlagsAndAttributes ,
            int hTemplateFile);

        const int OPEN_EXISTING = 3;

        // parameters:
        //      strPrinterName  "LPT1"
        /// <summary>
        /// ��ô����ӡ���� StreamWriter ����
        /// </summary>
        /// <param name="strPrinterName">��ӡ������</param>
        /// <param name="encoding">���뷽ʽ</param>
        /// <param name="stream">���� StreamWriter ����</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ���� 0: �ɹ�</returns>
        public int GetPrinterStream(string strPrinterName,
            Encoding encoding,
            out StreamWriter stream,
            out string strError)
        {
            strError = "";

            SafeFileHandle iHandle = CreateFile(strPrinterName,
                0x40000000, 0, 0, OPEN_EXISTING, 0, 0);
            // If the handle is invalid,
            // get the last Win32 error 
            // and throw a Win32Exception.
            if (iHandle.IsInvalid)
            {
                // Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
                stream = null;
                strError = "û�����Ӵ�ӡ�����ߴ�ӡ���˿ڲ���" + strPrinterName + "��������: " + Marshal.GetLastWin32Error().ToString();
                return -1;
            }

            // TODO: ��˵������캯�����ϳ��ˣ�
            FileStream fs = new FileStream(iHandle, FileAccess.ReadWrite);
            stream = new StreamWriter(fs, encoding); // ����д�ı�

            return 0;
        }

        // 
        /// <summary>
        /// ��ô�ӡ���˿ںš�ȱʡΪ LPT1
        /// </summary>
        public string PrinterName
        {
            get
            {
                return this.MainForm.AppInfo.GetString(
                    "charging_print",
                    "prnPort",
                    "LPT1");
            }
        }

        // 
        /// <summary>
        /// ��ǰ�Ƿ�Ϊ��ͣ��ӡ״̬
        /// </summary>
        public bool PausePrint
        {
            get
            {
                return this.MainForm.AppInfo.GetBoolean(
                    "charging_print",
                    "pausePrint",
                    false);

            }
        }
    }

    /// <summary>
    /// �����¼��Ĳ���
    /// </summary>
    public class BorrowedEventArgs : EventArgs
    {
        /// <summary>
        /// ������ʾ�Ĳ������ơ�Ϊ ���� ���� ֮һ
        /// </summary>
        public string OperName = "";    // ��ʾ������ ���� ���� ���� ��ʧ
        
        /// <summary>
        /// ��ĿժҪ
        /// </summary>
        public string BiblioSummary = "";   // ��ĿժҪ

        /// <summary>
        /// ����ժҪ
        /// </summary>
        public string ReaderSummary = "";   // ����ժҪ

        /// <summary>
        /// �������
        /// </summary>
        public string ItemBarcode = ""; // �������

        /// <summary>
        /// ����֤�����
        /// </summary>
        public string ReaderBarcode = "";   // ����֤�����

        // --- ����Ϊ�ͽ����йصĲ�����Ϣ
        /// <summary>
        /// Ӧ������
        /// </summary>
        public DateTime LatestReturnDate = new DateTime(0);  // Ӧ������

        // �������ޡ����硰20day��
        /// <summary>
        /// ���ޡ����硰20day��
        /// </summary>
        public string Period = "";

        // ��ǰΪ����ĵڼ��Σ�0��ʾ���ν���
        /// <summary>
        /// ��ǰΪ����ĵڼ��Σ�0��ʾ���ν���
        /// </summary>
        public long BorrowCount = 0;

        /// <summary>
        /// �����ķѵ�ʱ��
        /// </summary>
        public TimeSpan TimeSpan = new TimeSpan(0); // �����ķѵ�ʱ��

        // 2008/5/9 new add
        /// <summary>
        /// ���¼ XML �ַ���
        /// </summary>
        public string ItemXml = ""; // ���¼XML������OnInitial()���Ƿ�������this.MainForm.ChargingNeedReturnItemXml = true�����ֵ����Ϊ��

        // 2011/6/26
        /// <summary>
        /// ���������
        /// </summary>
        public string BorrowOperator = "";

        /// <summary>
        /// ���ɴ�
        /// </summary>
        public IChargingForm ChargingForm = null;
    }

    /// <summary>
    /// �����¼��Ĳ���
    /// </summary>
    public class ReturnedEventArgs : EventArgs
    {
        /// <summary>
        /// ������ʾ�Ĳ������ơ�Ϊ ���� ��ʧ ֮һ
        /// </summary>
        public string OperName = "";    // ��ʾ������ ���� ���� ���� ��ʧ

        /// <summary>
        /// ��ĿժҪ
        /// </summary>
        public string BiblioSummary = "";   // ��ĿժҪ

        /// <summary>
        /// ����ժҪ
        /// </summary>
        public string ReaderSummary = "";   // ����ժҪ

        /// <summary>
        /// �������
        /// </summary>
        public string ItemBarcode = ""; // ������

        /// <summary>
        /// ����֤�����
        /// </summary>
        public string ReaderBarcode = "";   // ����֤����

        /// <summary>
        /// �����ķѵ�ʱ��
        /// </summary>
        public TimeSpan TimeSpan = new TimeSpan(0); // �����ķѵ�ʱ��

        // --- ����Ϊ�ͻ����йصĲ�����Ϣ
        // ��������
        /// <summary>
        /// ��������
        /// </summary>
        public DateTime BorrowDate = new DateTime(0);

        // Ӧ������
        /// <summary>
        /// Ӧ������
        /// </summary>
        public DateTime LatestReturnDate = new DateTime(0); 

        // �������ޡ����硰20day��
        /// <summary>
        /// �������ޡ����硰20day��
        /// </summary>
        public string Period = "";

        // Ϊ����ĵڼ��Σ�0��ʾ���ν���
        /// <summary>
        /// Ϊ����ĵڼ��Σ�0��ʾ���ν���
        /// </summary>
        public long BorrowCount = 0;

        // ΥԼ�������ַ�����XML��ʽ
        /// <summary>
        /// ΥԼ������ XML �ַ���
        /// </summary>
        public string OverdueString = "";

        // 2008/5/9 new add
        /// <summary>
        /// ���¼ XML �ַ���
        /// </summary>
        public string ItemXml = ""; // ���¼XML������OnInitial()���Ƿ�������this.MainForm.ChargingNeedReturnItemXml = true�����ֵ����Ϊ��

        // 2011/6/26
        /// <summary>
        /// ���������
        /// </summary>
        public string BorrowOperator = "";

        /// <summary>
        /// ���������
        /// </summary>
        public string ReturnOperator = "";

        // 2013/4/2
        /// <summary>
        /// �ݲصص�
        /// </summary>
        public string Location = "";

        /// <summary>
        /// ͼ������
        /// </summary>
        public string BookType = "";

        /// <summary>
        /// ���ɴ�
        /// </summary>
        public IChargingForm ChargingForm = null;
    }

    /// <summary>
    /// �����¼��Ĳ���
    /// </summary>
    public class AmercedEventArgs : EventArgs
    {
        /// <summary>
        /// ������ʾ�Ĳ������ơ�Ϊ�����ѡ�
        /// </summary>
        public string OperName = "";    // ��ʾ������ ����

        /// <summary>
        /// ����֤�����
        /// </summary>
        public string ReaderBarcode = "";   // ����֤����

        /// <summary>
        /// ����ժҪ
        /// </summary>
        public string ReaderSummary = "";   // ����ժҪ

        /// <summary>
        /// �����ķѵ�ʱ��
        /// </summary>
        public TimeSpan TimeSpan = new TimeSpan(0); // �����ķѵ�ʱ��

        // --- ����Ϊ�ͽ������йصĲ�����Ϣ
        /// <summary>
        /// ������Ϣ����
        /// </summary>
        public List<OverdueItemInfo> OverdueInfos = new List<OverdueItemInfo>();

        // 2011/6/26
        /// <summary>
        /// ���Ѳ�����
        /// </summary>
        public string AmerceOperator = "";  // ���β�����
    }

    /// <summary>
    /// ����֤�����ɨ���¼��Ĳ���
    /// </summary>
    public class ReaderBarcodeScanedEventArgs : EventArgs
    {
        /// <summary>
        /// ����֤�����
        /// </summary>
        public string ReaderBarcode = "";
    }

    /// <summary>
    /// ��ӡ�¼��Ĳ���
    /// </summary>
    public class PrintEventArgs : EventArgs
    {
        /// <summary>
        /// ��ӡ��Ϣ
        /// </summary>
        public PrintInfo PrintInfo = null;  // [in]

        /// <summary>
        /// ��ӡ�������͡�Ϊ print/create ֮һ
        /// </summary>
        public string Action = "print"; // [in] �������� print--��ӡ create-������������

        /// <summary>
        /// ���ش�ӡ����ַ���
        /// </summary>
        public string ResultString = "";    // [out] ��ӡ����ַ���

        /// <summary>
        /// ���ش�ӡ����ַ����ĸ�ʽ��Ϊ text html ֮һ
        /// </summary>
        public string ResultFormat = "text";    // [out] ��ӡ����ַ����ĸ�ʽ text html
    }

    // ����һ������й���Ϣ
    /// <summary>
    /// ����һ������й���Ϣ
    /// </summary>
    public class BorrowItemInfo
    {
        /// <summary>
        /// ������ʾ�Ĳ�������Ϊ ���� ���� ֮һ
        /// </summary>
        public string OperName = "";    // ��ʾ������ ���� ���� ���� ��ʧ

        /// <summary>
        /// �������
        /// </summary>
        public string ItemBarcode = ""; // �������

        /// <summary>
        /// ��ĿժҪ
        /// </summary>
        public string BiblioSummary = "";   // ��ĿժҪ

        // Ӧ������
        /// <summary>
        /// Ӧ������
        /// </summary>
        public DateTime LatestReturnDate = new DateTime(0);

        // �������ޡ����硰20day��
        /// <summary>
        /// �������ޡ����硰20day��
        /// </summary>
        public string Period = "";

        // ��ǰΪ����ĵڼ��Σ�0��ʾ���ν���
        /// <summary>
        /// ��ǰΪ����ĵڼ��Σ�0��ʾ���ν���
        /// </summary>
        public long BorrowCount = 0;

        /// <summary>
        /// �����ķѵ�ʱ��
        /// </summary>
        public TimeSpan TimeSpan = new TimeSpan(0); // �����ķѵ�ʱ��

        /// <summary>
        /// ���������
        /// </summary>
        public string BorrowOperator = "";  // 2011/6/27

    }

    // ����һ������й���Ϣ
    /// <summary>
    /// ����һ������й���Ϣ
    /// </summary>
    public class ReturnItemInfo
    {
        /// <summary>
        /// ������ʾ�Ĳ�������Ϊ ���� ��ʧ ֮һ
        /// </summary>
        public string OperName = "";    // ��ʾ������ ���� ���� ���� ��ʧ

        /// <summary>
        /// �������
        /// </summary>
        public string ItemBarcode = ""; // �������

        /// <summary>
        /// ��ĿժҪ
        /// </summary>
        public string BiblioSummary = "";   // ��ĿժҪ


        // --- ����Ϊ�ͻ����йصĲ�����Ϣ
        // ��������
        /// <summary>
        /// ��������
        /// </summary>
        public DateTime BorrowDate = new DateTime(0);

        // Ӧ������
        /// <summary>
        /// Ӧ������
        /// </summary>
        public DateTime LatestReturnDate = new DateTime(0);


        // �������ޡ����硰20day��
        /// <summary>
        /// �������ޡ����硰20day��
        /// </summary>
        public string Period = "";

        // Ϊ����ĵڼ��Σ�0��ʾ���ν���
        /// <summary>
        /// Ϊ����ĵڼ��Σ�0��ʾ���ν���
        /// </summary>
        public long BorrowCount = 0;

        /// <summary>
        /// �����ķѵ�ʱ��
        /// </summary>
        public TimeSpan TimeSpan = new TimeSpan(0); // �����ķѵ�ʱ��
        /*
        // ΥԼ�������ַ�����XML��ʽ
        public string OverdueString = "";
         * */

        /// <summary>
        /// ���������
        /// </summary>
        public string BorrowOperator = "";  // 2011/6/27

        /// <summary>
        /// ���������
        /// </summary>
        public string ReturnOperator = "";  // 2011/6/27

        /// <summary>
        /// ���������
        /// </summary>
        public List<OverdueItemInfo> OverdueItems = new List<OverdueItemInfo>();

        // ����XMLƬ�Σ�����OverdueItems��������
        /// <summary>
        /// ���� XML Ƭ�ϣ��������������
        /// </summary>
        /// <param name="strOverdueString">XMLƬ��</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ���� 0: �ɹ�</returns>
        public int BuildOverdueItems(string strOverdueString,
            out string strError)
        {
            strError = "";
            this.OverdueItems.Clear();

            if (String.IsNullOrEmpty(strOverdueString) == true)
                return 0;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root/>");

            XmlDocumentFragment fragment = dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = strOverdueString;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            // ���뵽��ǰ��
            DomUtil.InsertFirstChild(dom.DocumentElement, fragment);

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("overdue");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                OverdueItemInfo info = new OverdueItemInfo();
                info.ItemBarcode = DomUtil.GetAttr(node, "barcode");
                info.RecPath = DomUtil.GetAttr(node, "recPath");
                info.Reason = DomUtil.GetAttr(node, "reason");
                info.Price = DomUtil.GetAttr(node, "price");
                info.BorrowDate = DomUtil.GetAttr(node, "borrowDate");
                info.BorrowPeriod = DomUtil.GetAttr(node, "borrowPeriod");
                info.ReturnDate = DomUtil.GetAttr(node, "returnDate");
                info.BorrowOperator = DomUtil.GetAttr(node, "borrowOperator");
                info.ReturnOperator = DomUtil.GetAttr(node, "operator");
                info.ID = DomUtil.GetAttr(node, "id");

                // 2008/11/15 new add
                info.Comment = DomUtil.GetAttr(node, "comment");

                this.OverdueItems.Add(info);
            }

            return 0;
        }

    }

    // ����������Ϣ
    /// <summary>
    /// ����������Ϣ
    /// </summary>
    public class OverdueItemInfo
    {
        /// <summary>
        /// �������
        /// </summary>
        public string ItemBarcode = ""; // barcode

        /// <summary>
        /// ���¼·��
        /// </summary>
        public string RecPath = ""; // recPath

        /// <summary>
        /// ����
        /// </summary>
        public string Reason = "";  // reason

        /// <summary>
        /// ���
        /// </summary>
        public string Price = "";   // price

        /// <summary>
        /// ��ʼʱ��
        /// </summary>
        public string BorrowDate = "";  // borrowDate

        /// <summary>
        /// ����
        /// </summary>
        public string BorrowPeriod = "";    // borrowPeriod

        /// <summary>
        /// ����ʱ��
        /// </summary>
        public string ReturnDate = "";  // returnDate

        /// <summary>
        /// ���������
        /// </summary>
        public string BorrowOperator = "";  // borrowOperator

        /// <summary>
        /// ���������
        /// </summary>
        public string ReturnOperator = "";    // operator

        /// <summary>
        /// �������� ID
        /// </summary>
        public string ID = "";  // id

        /// <summary>
        /// ע��
        /// </summary>
        public string Comment = ""; // comment 2008/11/15 new add

        /// <summary>
        /// ���Ѳ�����
        /// </summary>
        public string AmerceOperator = "";  // ֻ��C#�ű���ʹ��

        static string GetLineText(string strCaption,
            string strValue,
            string strLink = "")
        {
            if (string.IsNullOrEmpty(strValue) == true)
                return "";

            StringBuilder text = new StringBuilder(4096);
            if (string.IsNullOrEmpty(strValue) == false)
            {
                text.Append("<tr>");
                text.Append("<td class='name'>" + HttpUtility.HtmlEncode(strCaption) + "</td><td class='value'>"
                    + (string.IsNullOrEmpty(strLink) == true ? HttpUtility.HtmlEncode(strValue) : strLink)
                    + "</td>");
                text.Append("</tr>");
            }

            return text.ToString();
        }

        public string ToHtmlString(string strItemLink = "")
        {
            StringBuilder text = new StringBuilder(4096);
            text.Append("<table class='amerce_item'>");

            text.Append(GetLineText("����", this.Reason));
            text.Append(GetLineText("���", this.Price));
            text.Append(GetLineText("�������", this.ItemBarcode, strItemLink));

            text.Append(GetLineText("��ʼʱ��", this.BorrowDate));
            text.Append(GetLineText("��ʼ������", this.BorrowOperator));
            text.Append(GetLineText("����", this.BorrowPeriod));
            text.Append(GetLineText("����ʱ��", this.ReturnDate));
            text.Append(GetLineText("����������", this.ReturnOperator));
            text.Append(GetLineText("�������� ID", this.ID));
            text.Append(GetLineText("ע��", this.Comment));
            text.Append(GetLineText("���Ѳ�����", this.AmerceOperator));

            text.Append("</table>");

            return text.ToString();
        }
    }

    // ������������Ľ軹�����й���Ϣ������OnPrint()ʱʹ��
    /// <summary>
    /// ��ӡ��Ϣ
    /// </summary>
    public class PrintInfo
    {
        /// <summary>
        /// ����ʱ��
        /// </summary>
        public DateTime CreateTime = DateTime.Now;

        /// <summary>
        /// ��ǰ����֤�����
        /// </summary>
        public string CurrentReaderBarcode = "";

        /// <summary>
        /// ��ǰ����ժҪ
        /// </summary>
        public string CurrentReaderSummary = "";

        /// <summary>
        /// ������Ϣ����
        /// </summary>
        public List<BorrowItemInfo> BorrowItems = new List<BorrowItemInfo>();

        /// <summary>
        /// ������Ϣ����
        /// </summary>
        public List<ReturnItemInfo> ReturnItems = new List<ReturnItemInfo>();

        /// <summary>
        /// ������Ϣ����
        /// </summary>
        public List<OverdueItemInfo> OverdueItems = new List<OverdueItemInfo>();

        /// <summary>
        /// �Ѿ���ӡ���Ĵ���
        /// </summary>
        public int PrintedCount = 0;    // �Ѿ���ӡ���Ĵ���

        /// <summary>
        /// ���ȫ������
        /// </summary>
        public void Clear()
        {
            this.CurrentReaderBarcode = "";
            this.CurrentReaderSummary = "";
            this.BorrowItems.Clear();
            this.ReturnItems.Clear();
            this.OverdueItems.Clear();
            this.PrintedCount = 0;
        }

        /// <summary>
        /// ��ǰ�Ƿ��пɴ�ӡ������
        /// </summary>
        /// <returns>�Ƿ�</returns>
        public bool HasContent()
        {
            if (this.BorrowItems.Count > 0
                || this.ReturnItems.Count > 0
                || this.OverdueItems.Count > 0)
                return true;

            return false;
        }
    }
}
