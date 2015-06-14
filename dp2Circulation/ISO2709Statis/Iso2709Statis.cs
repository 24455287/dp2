using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Web;

using DigitalPlatform.Xml;
using DigitalPlatform.Script;

namespace dp2Circulation
{
    /// <summary>
    /// Iso2709StatisForm (ISO2709ͳ�ƴ�) ͳ�Ʒ�����������
    /// </summary>
    public class Iso2709Statis : StatisHostBase
    {
        /// <summary>
        /// �����ļ�����ȫ·��
        /// </summary>
        public string InputFilename = "";

        /// <summary>
        /// �������������� Iso2709StatisForm (ISO2709ͳ�ƴ�)
        /// </summary>
        public Iso2709StatisForm Iso2709StatisForm = null;	// ����

#if NO
        private bool disposed = false;

        public List<string> OutputFileNames = new List<string>(); // ��������html�ļ�

        int m_nFileNameSeed = 1;

        public WebBrowser Console = null;


        public string ProjectDir = "";  // ����Դ�ļ�����Ŀ¼
        public string InstanceDir = ""; // ��ǰʵ����ռ��Ŀ¼�����ڴ洢��ʱ�ļ�
#endif
        /// <summary>
        /// ��ǰ��¼�������е��±�
        /// </summary>
        public long CurrentRecordIndex = -1; // ��ǰXML��¼�������е�ƫ����

        string m_strMARC = "";    // MARC��¼��
        /// <summary>
        /// ��ǰ��Ŀ��¼�� MARC ���ڸ�ʽ�ַ���
        /// </summary>
        public string MARC
        {
            get
            {
                return this.m_strMARC;
            }
            set
            {
                this.m_strMARC = value;
            }
        }

#if NO
        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method 
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~Iso2709Statis()
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
                try // 2009/10/10
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
            string strFileNamePrefix = this.Iso2709StatisForm.MainForm.DataDir + "\\~xml_statis";

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
            return Path.Combine(this.Iso2709StatisForm.MainForm.DataDir, "~iso2709_statis");
        }
    }
}
