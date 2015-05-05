using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;

using System.IO;
using System.Xml;

using DigitalPlatform.Xml;
using DigitalPlatform.IO;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// ͳ�ƶ��󡣸����ڴ滺��ͳ����Ϣ
    /// </summary>
    public class Statis
    {
        public LibraryApplication App = null;

        // ����ͳ�Ƽ�¼���ڴ�DOM
        public XmlDocument TodayDom = new XmlDocument();
        // ��ǰDOM����Ӧ������
        public string CurrentDate = "";

        int m_nUnsavedCount = 0;

        internal ReaderWriterLock m_lock = new ReaderWriterLock();
        internal static int m_nLockTimeout = 5000;	// 5000=5��

        // ������һ���Ƿ���ڶ�Ӧ��ͳ���ļ�
        public bool ExistStatisFile(DateTime date)
        {
            string strFilename = this.App.StatisDir + "\\" + DateTimeUtil.DateTimeToString8(date) + ".xml";

            // 2008/11/24 changed
            FileInfo fi = new FileInfo(strFilename);

            if (fi.Exists == true && fi.Length > 0)
                return true;

            return false;
            /*
            if (File.Exists(strFilename) == true)
                return true;

            return false;
             * */
        }

        public int Initial(
            LibraryApplication app,
            out string strError)
        {
            strError = "";

            this.App = app;

            // 2013/5/11
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            { 
                LoadCurrentFile();
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            } 
            return 0;
        }

        public void Close()
        {
            if (this.TodayDom != null
                && this.CurrentDate != null)
            {
                SaveDom(true);
            }

            this.TodayDom = null;
            this.CurrentDate = "";
        }

        public static string GetCurrentDate()
        {
            DateTime now = DateTime.Now;

            return now.Year.ToString().PadLeft(4, '0')
            + now.Month.ToString().PadLeft(2, '0')
            + now.Day.ToString().PadLeft(2, '0');
        }

        void LoadCurrentFile()
        {
            DateTime now = DateTime.Now;

            this.CurrentDate = GetCurrentDate();

            string strStatisFileName = this.App.StatisDir + "\\"
                + this.CurrentDate + ".xml";

            if (this.TodayDom == null)
            {
                this.TodayDom = new XmlDocument();
            }

            try
            {
                this.TodayDom.Load(strStatisFileName);
            }
            catch(FileNotFoundException)
            {
                this.TodayDom.LoadXml("<root />");
                // ������ʼʱ��
                DomUtil.SetElementText(this.TodayDom.DocumentElement,
                    "startTime", DateTime.Now.ToString());
                // ���ý���ʱ��
                DomUtil.SetElementText(this.TodayDom.DocumentElement,
                    "endTime", DateTime.Now.ToString());
            }
            catch(Exception ex) // 2013/5/11
            {
                this.TodayDom.LoadXml("<root />");
                // ������ʼʱ��
                DomUtil.SetElementText(this.TodayDom.DocumentElement,
                    "startTime", DateTime.Now.ToString());
                // ���ý���ʱ��
                DomUtil.SetElementText(this.TodayDom.DocumentElement,
                    "endTime", DateTime.Now.ToString());

                string strErrorText = "Statis::LoadCurrentFile() �����쳣: " + ExceptionUtil.GetDebugText(ex);
                DomUtil.SetElementText(this.TodayDom.DocumentElement,
                    "error", DateTime.Now.ToString() + " " + strErrorText);
                if (this.App != null)
                    this.App.WriteErrorLog(strErrorText);
            }
        }

        void SaveDom(bool bForce)
        {
            if (bForce || m_nUnsavedCount > 100)
            {
                m_nUnsavedCount = 0;

                Debug.Assert(this.CurrentDate != "", "");

                string strStatisFileName = this.App.StatisDir + "\\"
                + this.CurrentDate + ".xml";

                this.TodayDom.Save(strStatisFileName);
                return;
            }

            m_nUnsavedCount++;
        }

        // ��ָ����<category>Ԫ����д��<item>Ԫ��
        double WriteItem(XmlNode nodeCategory,
            string strName,
            double fValue)
        {
            XmlNode nodeItem = nodeCategory.SelectSingleNode("item[@name='" + strName + "']");
            if (nodeItem == null)
            {
                nodeItem = this.TodayDom.CreateElement("item");
                nodeCategory.AppendChild(nodeItem);
                DomUtil.SetAttr(nodeItem, "name", strName);
            }

            string strOldValue = DomUtil.GetAttr(nodeItem, "value");
            double fOldValue = 0;

            if (string.IsNullOrEmpty(strOldValue) == false)
            {
                try
                {
                    fOldValue = Convert.ToDouble(strOldValue);
                }
                catch
                {
                }
            }

            double fNewValue = fOldValue + fValue;

            DomUtil.SetAttr(nodeItem, "value", fNewValue.ToString());

            return fNewValue;
        }

        // ��ͳ���ļ���д��һ��ֵ
        // �ڸ���д��<category>��Ȼ����<library>��д��<category>
        // parameters:
        //      strLibraryCode  ���Ϊ�գ���ֻ�ڸ���д��<category>�����Ϊ�ǿգ���Ҫ��<library>Ԫ����д��<category>
        // return:
        //      ���strLibraryCodeΪ�գ��򷵻�Ψһ�ۼ�ֵ�����strLibraryCodeΪ�ǿգ��򷵻�<library>Ԫ���µ�<category>�е��ۼ�ֵ
        public double IncreaseEntryValue(
            string strLibraryCode,
            string strCategory,
            string strName,
            double fValue)
        {
            if (this.TodayDom.DocumentElement == null)
            {
                throw new Exception("Statis dom not initialized");
            }

            if (this.CurrentDate == "")
            {
                throw new Exception("Statis CurrentDate not initialized");
            }

            this.m_lock.AcquireWriterLock(m_nLockTimeout);

            try
            {
                if (GetCurrentDate() != this.CurrentDate)
                {
                    SaveDom(true);
                    this.CurrentDate = GetCurrentDate();
                    this.TodayDom.LoadXml("<root />");  // ���ԭ��������
                    // ������ʼʱ��
                    DomUtil.SetElementText(this.TodayDom.DocumentElement,
                        "startTime", DateTime.Now.ToString());
                    // ���ý���ʱ��
                    DomUtil.SetElementText(this.TodayDom.DocumentElement,
                        "endTime", DateTime.Now.ToString());
                }

                if (String.IsNullOrEmpty(strCategory) == true)
                    strCategory = "default";

                XmlNode nodeCategory = this.TodayDom.DocumentElement.SelectSingleNode("category[@name='" + strCategory + "']");
                if (nodeCategory == null)
                {
                    nodeCategory = this.TodayDom.CreateElement("category");
                    this.TodayDom.DocumentElement.AppendChild(nodeCategory);
                    DomUtil.SetAttr(nodeCategory, "name", strCategory);
                }

                // ��ָ����<category>Ԫ����д��<item>Ԫ��
                double fNewValue = WriteItem(nodeCategory,
                    strName,
                    fValue);
#if NO
                XmlNode nodeItem = nodeCategory.SelectSingleNode("item[@name='" + strName + "']");
                if (nodeItem == null)
                {
                    nodeItem = this.TodayDom.CreateElement("item");
                    nodeCategory.AppendChild(nodeItem);
                    DomUtil.SetAttr(nodeItem, "name", strName);
                }

                string strOldValue = DomUtil.GetAttr(nodeItem, "value");
                double fOldValue = 0;

                if (string.IsNullOrEmpty(strOldValue) == false)
                {
                    try
                    {
                        fOldValue = Convert.ToDouble(strOldValue);
                    }
                    catch
                    {
                    }
                }

                double fNewValue = fOldValue + fValue;

                DomUtil.SetAttr(nodeItem, "value", fNewValue.ToString());

#endif
                if (string.IsNullOrEmpty(strLibraryCode) == false)
                {
                    XmlNode nodeLibrary = this.TodayDom.DocumentElement.SelectSingleNode("library[@code='" + strLibraryCode + "']");
                    if (nodeLibrary == null)
                    {
                        nodeLibrary = this.TodayDom.CreateElement("library");
                        this.TodayDom.DocumentElement.AppendChild(nodeLibrary);
                        DomUtil.SetAttr(nodeLibrary, "code", strLibraryCode);

                        nodeCategory = null;
                    }
                    else
                    {
                        nodeCategory = nodeLibrary.SelectSingleNode("category[@name='" + strCategory + "']");
                    }

                    if (nodeCategory == null)
                    {
                        nodeCategory = this.TodayDom.CreateElement("category");
                        nodeLibrary.AppendChild(nodeCategory);
                        DomUtil.SetAttr(nodeCategory, "name", strCategory);
                    }

                    // ��ָ����<category>Ԫ����д��<item>Ԫ��
                    fNewValue = WriteItem(nodeCategory,
                        strName,
                        fValue);
                }

                // ���ý���ʱ��
                DomUtil.SetElementText(this.TodayDom.DocumentElement,
                    "endTime", DateTime.Now.ToString());

                SaveDom(false);

                return fNewValue;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
        }

        public int IncreaseEntryValue(
            string strLibraryCode,
            string strCategory,
            string strName,
            int nValue)
        {
            double fNewValue = IncreaseEntryValue(
                strLibraryCode,
                strCategory,
                strName,
                (double)nValue);

            return (int)fNewValue;
        }

        // �쳣�����ܻ��׳��쳣
        public void Flush()
        {
            this.m_lock.AcquireWriterLock(m_nLockTimeout);

            try
            {
                if (this.TodayDom != null
                    && this.CurrentDate != null)
                {
                    SaveDom(true);
                }
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

        }

    }

    /*
     * Ŀǰ�õ���ͳ��ָ�����ơ���������̬�����ơ�
     * 
����
	��������������ظ�����
	������
	���
	��������������ظ�����
	��������������ظ����޶���֤����Ÿ����жϴ���
	��������������ظ�������֤�����Ҳ�޷�ȥ�ش���
	��������������ظ������ݶ���֤����ųɹ�ȥ�ش���
	������
	����
	������ʧ
	�����ڲ�
	ԤԼ�����
	��ͣ������������
	��ͣ���������
	��������������
	ԤԼ��
	ԤԼ�����
ΥԼ��
	ȡ����
	ȡ��Ԫ
	�޸Ĵ�
	������
	����Ԫ
ΥԼ��֮ע��
	�޸Ĵ�
�޸�������Ϣ
	���߲����
	ʵ������
����˴�
	������֮����
Ѻ��
	�������������
���
	�������������
�޸Ķ�����Ϣ
	�����¼�¼��
	�޸ļ�¼��
	ɾ����¼��
�޸Ķ�����Ϣ֮״̬
	
�޸Ķ�����Ϣ֮Ѻ��
	����
��Ϣ���
	ɾ��������Ϣ����
����֪ͨ
	dpmail����֪ͨ����
	email����֪ͨ����
����DTLP
	��ʼ�����ݿ����
	���Ǽ�¼����
	ɾ����¼����
     * * */

}
