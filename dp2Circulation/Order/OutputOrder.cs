using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Web;

using DigitalPlatform.Xml;
using DigitalPlatform.dp2.Statis;

using DigitalPlatform.CirculationClient;
using DigitalPlatform.CirculationClient.localhost;

namespace dp2Circulation
{
    /// <summary>
    /// ���������������
    /// ���� PrintOrderForm (��ӡ������)
    /// </summary>
    public class OutputOrder : StatisHostBase0
    {
        /// <summary>
        /// �������������� PrintOrderForm (��ӡ������)
        /// </summary>
        public PrintOrderForm PrintOrderForm = null;	// [in]��ӡ������

#if NO
        private bool disposed = false;
        /// <summary>
        /// ͳ�Ʒ����洢Ŀ¼
        /// </summary>
        public string ProjectDir = "";  // [in]��ǰ�����������������Ŀ¼

#endif

        /// <summary>
        /// ��������Ŀ¼
        /// </summary>
        public string DataDir = ""; // [in]����ǰ�˵�����Ŀ¼


        /// <summary>
        /// ������
        /// </summary>
        public string Seller = "";  // [in]������

        /// <summary>
        /// ���� XML �ļ���
        /// </summary>
        public string XmlFilename = ""; // [in]���õ�XML��ʽ�����ļ����Ѿ��������ˡ��ļ���ȫ·��

        /// <summary>
        /// �������Ŀ¼
        /// </summary>
        public string OutputDir = "";   // [in]�������Ŀ¼

        /// <summary>
        /// ��ǰ�������Ѿ���ѡ���ĳ��������͡�ֵΪ��ͼ�顱�����������֮һ
        /// </summary>
        public string PubType = ""; // [in]��ǰ�������Ѿ���ѡ���ĳ��������͡�ֵΪ��ͼ�顱�����������֮һ

        /// <summary>
        /// ���캯��
        /// </summary>
        public OutputOrder()
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
        ~OutputOrder()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }


        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        /// <summary>
        /// Dispose
        /// </summary>
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

                /*
                // ɾ����������ļ�
                if (this.OutputFileNames != null)
                {
                    Global.DeleteFiles(this.OutputFileNames);
                    this.OutputFileNames = null;
                }*/

                /*
                // Call the appropriate methods to clean up 
                // unmanaged resources here.
                // If disposing is false, 
                // only the following code is executed.
                CloseHandle(handle);
                handle = IntPtr.Zero;
                 * */
                try // 2008/6/26 new add
                {
                    this.FreeResources();
                }
                catch
                {
                }
            }
            disposed = true;
        }

        // �ͷ���Դ
        /// <summary>
        /// �ͷ���Դ
        /// </summary>
        public virtual void FreeResources()
        {
        }

#endif

        internal override string GetOutputFileNamePrefix()
        {
            return Path.Combine(this.PrintOrderForm.MainForm.DataDir, "~outputorder_statis");
        }

	    // ��ʼ��
        // return:
        //      false   ��ʼ��ʧ�ܡ�������Ϣ��strError��
        //      true    ��ʼ���ɹ�
        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="strError">������Ϣ</param>
        /// <returns>�Ƿ��ʼ���ɹ�</returns>
        public virtual bool Initial(out string strError)
        {
            strError = "";
            return true;
        }

        // ��ں���
        /// <summary>
        /// ��ں��������ش˷�����ʵ�ֽű�����
        /// </summary>
        public virtual void Output()
        {

        }
    }
}

