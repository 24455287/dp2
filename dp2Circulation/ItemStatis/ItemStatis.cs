using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Web;

using DigitalPlatform.Xml;
using DigitalPlatform.Marc;
using DigitalPlatform.dp2.Statis;

// 2013/3/26 ��� XML ע��

namespace dp2Circulation
{
    /// <summary>
    /// ItemStatisForm (��ͳ�ƴ�) ͳ�Ʒ�����������
    /// </summary>
    public class ItemStatis : StatisHostBase
    {
        // private bool disposed = false;

        /// <summary>
        /// �ݲصص��б�
        /// </summary>
        public string LocationNames = "";

        /// <summary>
        /// ��ǰ���¼��ʱ���
        /// </summary>
        public byte[] Timestamp = null; // ��ǰ���¼��ʱ��� 2009/9/26 new add

        // public WebBrowser Console = null;

        /// <summary>
        /// �������������� ItemStatisForm (��ͳ�ƴ�)
        /// </summary>
        public ItemStatisForm ItemStatisForm = null;	// ����

        /// <summary>
        /// ��ǰ���¼·��
        /// </summary>
        public string CurrentRecPath = "";    // ��ǰ���¼·��
        /// <summary>
        /// ��ǰ���¼�������е��±ꡣ�� 0 ��ʼ���������Ϊ -1����ʾ��δ��ʼ����
        /// </summary>
        public long CurrentRecordIndex = -1; // ��ǰ���¼�������е�ƫ����

        /// <summary>
        /// ��ǰ���¼����������Ŀ��¼·��
        /// </summary>
        public string CurrentBiblioRecPath = "";    // ��ǰ��Ŀ��¼·����ָ���¼��������Ŀ��¼��

#if NO
        public string ProjectDir = "";  // ����Դ�ļ�����Ŀ¼
        public string InstanceDir = ""; // ��ǰʵ����ռ��Ŀ¼�����ڴ洢��ʱ�ļ�

        public List<string> OutputFileNames = new List<string>(); // ��������html�ļ�

        int m_nFileNameSeed = 1;
#endif
        /// <summary>
        /// ��ǰ���ڴ���Ĳ� XML ��¼��XmlDocument ����
        /// </summary>
        public XmlDocument ItemDom = null;    // Xmlװ��XmlDocument

        string m_strXml = "";    // ���¼��
        /// <summary>
        /// ��ǰ���ڴ���Ĳ� XML ��¼���ַ�������
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

        internal string m_strBiblioXml = "";
        /// <summary>
        /// ��ǰ���ڴ���Ĳ��¼����������Ŀ XML ��¼���ַ�������
        /// </summary>
        public string BiblioXml
        {
            get
            {
                if (string.IsNullOrEmpty(this.m_strBiblioXml) == false)
                    return this.m_strBiblioXml;

                if (string.IsNullOrEmpty(this.CurrentBiblioRecPath) == true)
                    throw new Exception("CurrentBiblioRecPathΪ�գ��޷�ȡ����Ŀ��¼");

                string strBiblioXml = "";
                string strError = "";
                int nRet = this.ItemStatisForm.GetBiblioInfo(this.CurrentBiblioRecPath,
                    "xml",
                    out strBiblioXml,
                    out strError);
                if (nRet == -1)
                    throw new Exception("�����Ŀ��¼ʱ����: " + strError);

                this.m_strBiblioXml = strBiblioXml;
                return this.m_strBiblioXml;
            }
        }

        internal XmlDocument m_biblioDom = null;

        /// <summary>
        /// ��ǰ���ڴ���Ĳ��¼����������Ŀ XML ��¼��XmlDocument ����
        /// </summary>
        public XmlDocument BiblioDom
        {
            get
            {
                if (this.m_biblioDom != null)
                    return this.m_biblioDom;

                this.m_biblioDom = new XmlDocument();
                try
                {
                    this.m_biblioDom.LoadXml(this.BiblioXml);
                }
                catch (Exception ex)
                {
                    this.m_biblioDom = null;
                    throw ex;
                }

                return this.m_biblioDom;
            }
        }

        internal string m_strMarcRecord = "";
        internal string m_strMarcSyntax = "";
        /// <summary>
        /// ��ǰ���ڴ���Ĳ��¼����������Ŀ��¼�� MARC �ַ���
        /// </summary>
        public string MarcRecord
        {
            get
            {
                if (string.IsNullOrEmpty(this.m_strMarcRecord) == false)
                    return this.m_strMarcRecord;

                // ��XML��Ŀ��¼ת��ΪMARC��ʽ
                string strOutMarcSyntax = "";
                string strMarc = "";
                string strError = "";

                // ��MARCXML��ʽ��xml��¼ת��Ϊmarc���ڸ�ʽ�ַ���
                // parameters:
                //		bWarning	== true, ��������ת��,���ϸ�Դ�����; = false, �ǳ��ϸ�Դ�����,��������󲻼���ת��
                //		strMarcSyntax	ָʾmarc�﷨,���==""�����Զ�ʶ��
                //		strOutMarcSyntax	out����������marc�����strMarcSyntax == ""�������ҵ�marc�﷨�����򷵻����������strMarcSyntax��ͬ��ֵ
                int nRet = MarcUtil.Xml2Marc(this.BiblioXml,
                    true,   // 2013/1/12 �޸�Ϊtrue
                    "", // strMarcSyntax
                    out strOutMarcSyntax,
                    out strMarc,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);

                this.m_strMarcSyntax = strOutMarcSyntax;
                this.m_strMarcRecord = strMarc;
                return this.m_strMarcRecord;
            }
        }

        /// <summary>
        /// ��ǰ���ڴ���Ĳ��¼����������Ŀ��¼�� MARC ��ʽ��usmarc / unimarc
        /// </summary>
        public string MarcSyntax
        {
            get
            {
                // ��ʹMARC��ʽ�����
                if (string.IsNullOrEmpty(this.m_strMarcSyntax) == true)
                {
                    string strTemp = this.MarcRecord;
                }

                return this.m_strMarcSyntax;
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public ItemStatis()
        {
            //
            // TODO: Add constructor logic here
            //
        }

#if NO
        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method 
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~ItemStatis()
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

                try // 2008/11/28 new add
                {
                    this.FreeResources();
                }
                catch
                {
                }
            }
            disposed = true;
        }

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
            string strFileNamePrefix = this.ItemStatisForm.MainForm.DataDir + "\\~item_statis";

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
        internal override string GetOutputFileNamePrefix()
        {
            return Path.Combine(this.ItemStatisForm.MainForm.DataDir, "~item_statis");
        }

        /// <summary>
        /// ��Ե�ǰ���¼����������Ŀ��¼ִ�� MARC ������
        /// </summary>
        public void DoMarcFilter()
        {
            string strError = "";
            string strMarcRecord = this.MarcRecord;
            if (string.IsNullOrEmpty(strMarcRecord) == false)
            {
                this.ItemStatisForm.DoMarcFilter(
                    (int)this.CurrentRecordIndex,
                    strMarcRecord,
                    this.MarcSyntax,
                    out strError);
            }
        }
    }

}

