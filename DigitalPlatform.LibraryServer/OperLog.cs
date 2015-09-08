using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Xml;

using DigitalPlatform.IO;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;
using System.Runtime.Serialization;
using System.Collections;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// ������־
    /// </summary>
    public class OperLog
    {
        public LibraryApplication App = null;

        public OperLogFileCache Cache = new OperLogFileCache();

        string m_strDirectory = "";   // �ļ����Ŀ¼

        string m_strFileName = "";    // �ļ��� ����·������

        Stream m_stream = null;

        private ReaderWriterLock m_lock = new ReaderWriterLock();
        static int m_nLockTimeout = 5000;	// 5000=5��

        Stream m_streamSpare = null;
        string m_strSpareOperLogFileName = "";

        bool _bSmallFileMode = false;   // �Ƿ�ΪС�ļ�ģʽ��С�ļ�ģʽ��ÿ��д�붯����д��һ����������־�ļ�

        // ׼��������־�ļ�
        // ��ν������־�ļ������ǵ���ͨ��־�ļ�д�뷢�ֿռ䲻��ʱ����ʱ���õġ�Ԥ��׼���õ���һ�ļ�
        // parameters:
        //      strFileName ������־�ļ���������·���Ĵ��ļ���
        // return:
        //      -1  error
        int PrepareSpareOperLogFile(out string strError)
        {
            strError = "";
            if (String.IsNullOrEmpty(m_strDirectory) == true)
            {
                strError = "��δ����m_strDirectory��Աֵ";
                return -1;
            }

            string strFileName = PathUtil.MergePath(m_strDirectory,
                "spare_operlog.bin");

            m_strSpareOperLogFileName = strFileName;

            try
            {
                // ����ļ����ڣ��ʹ򿪣�����ļ������ڣ��ʹ���һ���µ�
                m_streamSpare = File.Open(
    strFileName,
    FileMode.OpenOrCreate,
    FileAccess.ReadWrite,
    FileShare.ReadWrite);
            }
            catch (Exception ex)
            {
                strError = "�򿪻򴴽��ļ� '" + strFileName + "' ��������: " + ex.Message;
                return -1;
            }

            // ��һ�δ���
            if (m_streamSpare.Length == 0)
            {
                // д��հ�����
                int nRet = ResetSpareFileContent(out strError);
                if (nRet == -1)
                    return -1;
            }


            return 0;
        }

        // �ѱ����ļ�������Ϊ�հ�
        int ResetSpareFileContent(out string strError)
        {
            strError = "";

            Debug.Assert(this.m_streamSpare != null, "");

            this.m_streamSpare.Seek(0, SeekOrigin.Begin);

            // д��հ�����
            byte[] buffer = new byte[4096];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0;
            }

            try
            {
                // һ��д��4M�հ�����
                for (int i = 0; i < 1024; i++)
                {
                    m_streamSpare.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                strError = "����ļ� " + m_strSpareOperLogFileName + " д��4M�հ���Ϣʱ����: " + ex.Message;
                return -1;
            }

            this.m_streamSpare.SetLength(this.m_streamSpare.Position);

            return 0;
        }

        // ��Ӧ��д����ʱ�ļ�����Ϣ��ת�뵱�������ļ�
        // return:
        //      -1  ����
        //      0   ��ͨ��������ûָ�
        //      1   �ѻָ�
        int DoRecover(out string strError)
        {
            strError = "";

            if (m_streamSpare == null)
                return 0;

            if (m_streamSpare.Length == 0)
                return 0;

            this.m_streamSpare.Seek(0, SeekOrigin.Begin);


            // �۲��Ƿ���Ӧ��д�������?
            byte [] length = new byte[8];
            int nRet = m_streamSpare.Read(length, 0, 8);
            if (nRet != 8)
            {
                strError = "Ӧ���ļ���ʽ����ȷ1";
                return -1;
            }

            long lLength = BitConverter.ToInt64(length, 0);

            if (lLength > m_streamSpare.Length - m_streamSpare.Position)
            {
                strError = "Ӧ���ļ���ʽ����ȷ2";
                return -1;
            }

            if (lLength == 0)
            {
                // Ϊ�ӱ����գ���ֹ�ļ�����ǰ����ȾӰ�������󣬴�ʱ�Ƿ���Ҫд��հ�����
                return 0;   // û��Ӧ��д�������
            }

            // �У������Դ���
            nRet = OpenCurrentStream(out strError);
            if (nRet == -1)
                return -1;
            Debug.Assert(m_stream != null, "");

            // ���浱����־�ļ���ԭʼ�ߴ磬�Ա�����ʱ�ضϻ�ȥ
            long lSaveLength = this.m_stream.Length;
            bool bSucceed = false;
            try
            {

                // �ѱ����ļ��е����ݣ����Ƶ�������־�ļ�β��
                m_streamSpare.Seek(0, SeekOrigin.Begin);
                for (int i = 0; ; i++)
                {
                    length = new byte[8];
                    nRet = m_streamSpare.Read(length, 0, 8);
                    if (nRet != 8)
                    {
                        if (i == 0)
                        {
                            strError = "Ӧ���ļ���ʽ����ȷ1";
                            return -1;
                        }
                        break;   // ������
                    }

                    lLength = BitConverter.ToInt64(length, 0);

                    if (lLength > m_streamSpare.Length - m_streamSpare.Position)
                    {
                        strError = "Ӧ���ļ���ʽ����ȷ2";
                        return -1;
                    }

                    if (lLength == 0)
                        break;   // û��Ӧ��д�������

                    // д�볤��
                    try
                    {
                        this.m_stream.Write(length, 0, 8);
                        this.m_stream.Flush();  // ��ʹ������Щ��¶
                    }
                    catch (Exception ex)
                    {
                        strError = "д�뵱����־�ļ�ʱ����: " + ex.Message;
                        return -1;
                    }


                    // �������ݣ�׷�ӵ������ļ�ĩβ
                    int nWrited = 0;
                    int nThisLen = 0;
                    for (; ; )
                    {
                        byte[] buffer = new byte[4096];
                        nThisLen = Math.Min((int)lLength - nWrited, 4096);
                        nRet = this.m_streamSpare.Read(buffer, 0, nThisLen);
                        if (nRet != nThisLen)
                        {
                            strError = "���뱸���ļ�ʱ����";
                            return -1;
                        }
                        try
                        {
                            this.m_stream.Write(buffer, 0, nThisLen);
                            this.m_stream.Flush();  // ��ʹ������Щ��¶
                        }
                        catch (Exception ex)
                        {
                            strError = "д�뵱����־�ļ�ʱ����: " + ex.Message;
                            return -1;
                        }

                        nWrited += nThisLen;
                        if (nWrited >= lLength)
                            break;
                    }
                }

                bSucceed = true;
            }
            finally
            {
                // �ض��ļ�
                if (bSucceed == false)
                {
                    // ֪ͨϵͳ����
                    this.App.HangupReason = HangupReason.OperLogError;

                    this.App.WriteErrorLog("ϵͳ����ʱ����ͼ��������־�ļ��е���Ϣд�뵱����־�ļ�����ʹϵͳ�ָ�������������һŬ��ʧ���ˡ�������Ϊ����Ŀ¼�ڳ����฻����̿ռ䣬Ȼ����������ϵͳ��");

                    this.m_stream.SetLength(lSaveLength);
                }
            }

            Debug.Assert(bSucceed == true, "");

            // �������ļ���Ϊ�հ�����
            nRet = ResetSpareFileContent(out strError);
            if (nRet == -1)
                return -1;

            // ֪ͨϵͳ���
            this.App.HangupReason = HangupReason.None;
            this.App.WriteErrorLog("ϵͳ����ʱ�����ֱ�����־�ļ������ϴν���д�����־��Ϣ�����Ѿ��ɹ����뵱����־�ļ���");

            return 1;   // �ָ��ɹ�
        }

        // ��־�ļ���ŵ�Ŀ¼
        public string Directory
        {
            get
            {
                return this.m_strDirectory;
            }
        }

        // ��ʼ������
        public int Initial(
            LibraryApplication app,
            string strDirectory,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            this.App = app;

            this.Close();

            // 2013/12/1
            ////Debug.WriteLine("begin write lock 4");
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {

                m_strDirectory = strDirectory;

                PathUtil.CreateDirIfNeed(m_strDirectory);

                // 2013/6/16
                nRet = this.VerifyLogFiles(true, out strError);
                if (nRet == -1)
                {
                    this.App.WriteErrorLog("У�������־ʱ����: " + strError);
                    return -1;
                }
                if (nRet == 1)
                {
                    this.App.WriteErrorLog("У�������־ʱ���ִ����Ѿ��Զ��޸���" + strError);
                }

                // ��ȫ��С�ļ��ϲ������ļ�
                // return:
                //      -1  ���г���
                //      0   û�д���
                //      1   �д���
                nRet = MergeTempLogFiles(true, out strError);
                if (nRet == -1)
                {
                    this.App.WriteErrorLog("�ϲ���ʱ��־�ļ�ʱ����: " + strError);
                    return -1;
                }
                if (nRet == 1)
                {
                    this.App.WriteErrorLog("�ϲ���ʱ��־�ļ�ʱ���ִ����Ѿ��Զ��޸���" + strError);
                }

                nRet = PrepareSpareOperLogFile(out strError);
                if (nRet == -1)
                    return -1;

                Debug.Assert(this.m_streamSpare != null, "");

                // return:
                //      -1  ����
                //      0   ��ͨ��������ûָ�
                //      1   �ѻָ�
                nRet = DoRecover(out strError);
                if (nRet == -1)
                {
                    // �ӱ����ļ��лָ���ʧ��
                    return -1;
                }

                // �ļ�ָ���账��ӭ���쳣��״̬
                this.m_streamSpare.Seek(0, SeekOrigin.Begin);

                // this._bSmallFileMode = true;    // ����
                return 0;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
                ////Debug.WriteLine("end write lock 4");
            }
        }

        public void CloseLogStream()
        {
            if (this.m_stream != null)
            {
                this.m_stream.Close();
                this.m_stream = null;
                this.m_strFileName = "";
            }
        }

        // parameters:
        //      bEnterSmallFileMode �Ƿ��ڱ�����رպ��Զ�����С�ļ�״̬
        public void Close(bool bEnterSmallFileMode = false)
        {
            // 2013/12/1
            ////Debug.WriteLine("begin write lock 5");
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                CloseLogStream();

                if (this.m_streamSpare != null)
                {
                    this.m_streamSpare.Close();
                    this.m_streamSpare = null;
                    this.m_strSpareOperLogFileName = "";
                }

                if (bEnterSmallFileMode == true)
                    this._bSmallFileMode = true;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
                ////Debug.WriteLine("end write lock 5");
            }
        }

        // д��һ������(string����)
        // parameters:
        //      bWrite  �Ƿ����д���ļ������Ϊ false����ʾ�������㼴��д��ĳ���
        // return:
        //      ���ز����д�볤��
        public static long WriteEntry(
            Stream stream,
            string strMetaData,
            string strBody,
            bool bWrite = true,
            long lTotalLength = 0)
        {
            // �������㳤��
            if (bWrite == false)
            {
                long lSize = 8; // �ܳ���

                lSize += 8;// metadata����

                // metadata
                if (String.IsNullOrEmpty(strMetaData) == false)
                {
                    lSize += Encoding.UTF8.GetByteCount(strMetaData);
                }

                lSize += 8;// strBody����

                // strBody
                lSize += Encoding.UTF8.GetByteCount(strBody);

                return lSize;
            }

            byte[] length = new byte[8];	// ��ʱд������

            if (lTotalLength != 0)
                length = BitConverter.GetBytes(lTotalLength - 8);

            // ������ʼλ��
            long lEntryStart = stream.Position;

            // �����ܳ���
            stream.Write(length, 0, 8);

            byte[] metadatabody = null;

            // metadata����
            if (String.IsNullOrEmpty(strMetaData) == false)
            {
                metadatabody = Encoding.UTF8.GetBytes(strMetaData);
                length = BitConverter.GetBytes((long)metadatabody.Length);
            }
            else
            {
                length = BitConverter.GetBytes((long)0);
            }

            stream.Write(length, 0, 8);	// metadata����

            // metadata����
            if (metadatabody != null)
            {
                stream.Write(metadatabody, 0, metadatabody.Length);
                // ���metadatabodyΪ��, ��˲��ֿ�ȱ
            }


            // strBody����
            byte[] xmlbody = Encoding.UTF8.GetBytes(strBody);

            length = BitConverter.GetBytes((long)xmlbody.Length);

            stream.Write(length, 0, 8);  // body����

            // xml body����
            stream.Write(xmlbody, 0, xmlbody.Length);

            // ������β
            long lEntryLength = stream.Position - lEntryStart - 8;

            if (lTotalLength == 0)
            {
                // д�뵥���ܳ���
                if (stream.Position != lEntryStart)
                {
                    // stream.Seek(lEntryStart, SeekOrigin.Begin);  // �ٶ���!
                    long lDelta = lEntryStart - stream.Position;
                    stream.Seek(lDelta, SeekOrigin.Current);
                }

                length = BitConverter.GetBytes((long)lEntryLength);

                stream.Write(length, 0, 8);

                // �ļ�ָ��ص�ĩβλ��
                stream.Seek(lEntryLength, SeekOrigin.Current);
            }

            return lEntryLength + 8;
        }

#if NO
        // ����������ֱ��д��� byte []
        public static byte [] BuildEntry(
    Stream stream,
    string strMetaData,
    string strBody)
        {
            List<byte> result = new List<byte>();

            byte[] length = new byte[8];

            // ȱ���ܳ���

            byte[] metadatabody = null;

            // metadata����
            if (String.IsNullOrEmpty(strMetaData) == false)
            {
                metadatabody = Encoding.UTF8.GetBytes(strMetaData);
                length = BitConverter.GetBytes((long)metadatabody.Length);
            }
            else
            {
                length = BitConverter.GetBytes((long)0);
            }

            result.AddRange(length);
            if (metadatabody != null)
                result.AddRange(metadatabody);

            // strBody����
            byte[] xmlbody = Encoding.UTF8.GetBytes(strBody);
            length = BitConverter.GetBytes((long)xmlbody.Length);

            result.AddRange(length);
            result.AddRange(xmlbody);

            // ������β
            length = BitConverter.GetBytes((long)result.Count); // �ܳ��Ȳ� 8 bytes
            result.InsertRange(0, length);

            byte[] array = new byte[result.Count];
            result.CopyTo(array);
            return array;
        }
#endif

        // ע��������ҪԤ��֪�� stream �ĳ����ƺ���΢������һЩ
        // д��һ������(Stream����)
        // parameters:
        //      streamBody  �������ݵ��������ñ�����ǰ��Ҫ��֤�ļ�ָ�������ݿ�ʼλ�ã���������һֱ���ж�ȡ���ݵ�����ĩβ
        public static long WriteEntry(
            Stream stream,
            string strMetaData,
            Stream streamBody,
            bool bWrite = true,
            long lTotalLength = 0)
        {
            // �������㳤��
            if (bWrite == false)
            {
                long lSize = 8; // �ܳ���

                lSize += 8;// metadata����

                // metadata
                if (String.IsNullOrEmpty(strMetaData) == false)
                {
                    lSize += Encoding.UTF8.GetByteCount(strMetaData);
                }

                lSize += 8;// body ����

                // body
                long lStremBodyLength = 0;
                if (streamBody != null)
                    lStremBodyLength = (streamBody.Length - streamBody.Position);
                lSize += lStremBodyLength;

                return lSize;
            }

            {
                byte[] length = new byte[8];	// ��ʱд������
                if (lTotalLength != 0)
                    length = BitConverter.GetBytes(lTotalLength - 8);

                // ����entry��ʼλ��
                long lEntryStart = stream.Position;

                // �����ܳ���
                stream.Write(length, 0, 8);

                byte[] metadatabody = null;

                // metadata����
                if (String.IsNullOrEmpty(strMetaData) == false)
                {
                    metadatabody = Encoding.UTF8.GetBytes(strMetaData);
                    length = BitConverter.GetBytes((long)metadatabody.Length);
                }
                else
                {
                    length = BitConverter.GetBytes((long)0);
                }

                stream.Write(length, 0, 8);	// metadata����

                // metadata����
                if (metadatabody != null)
                {
                    stream.Write(metadatabody, 0, metadatabody.Length);
                    // ���metadatabodyΪ��, ��˲��ֿ�ȱ
                }

                // ����stream��ʼλ��
                long lStreamStart = stream.Position;

                // stream������֪
                long lStremBodyLength = 0;
                if (streamBody != null)
                    lStremBodyLength = (streamBody.Length - streamBody.Position);
                length = BitConverter.GetBytes((long)lStremBodyLength);
                stream.Write(length, 0, 8);

                if (streamBody != null)
                {
                    // stream����
                    int chunk_size = 4096;
                    byte[] chunk = new byte[chunk_size];
                    for (; ; )
                    {
                        int nReaded = streamBody.Read(chunk, 0, chunk_size);
                        if (nReaded > 0)
                            stream.Write(chunk, 0, nReaded);

                        if (nReaded < chunk_size)
                            break;
                    }
                }

                // �����������֪
                long lEntryLength = stream.Position - lEntryStart - 8;


                // stream����������֪
                long lStreamLength = stream.Position - lStreamStart - 8;

                if (lTotalLength == 0)
                {
                    if (stream.Position != lStreamStart)
                    {
                        // stream.Seek(lStreamStart, SeekOrigin.Begin);      // �ٶ���!
                        long lDelta = lStreamStart - stream.Position;
                        stream.Seek(lDelta, SeekOrigin.Current);
                    }

                    length = BitConverter.GetBytes((long)lStreamLength);

                    stream.Write(length, 0, 8);

                    // ������β

                    // д�뵥���ܳ���
                    if (stream.Position != lEntryStart)
                    {
                        // stream.Seek(lEntryStart, SeekOrigin.Begin);      // �ٶ���!
                        long lDelta = lEntryStart - stream.Position;
                        stream.Seek(lDelta, SeekOrigin.Current);
                    }

                    length = BitConverter.GetBytes((long)lEntryLength);

                    stream.Write(length, 0, 8);

                    // �ļ�ָ��ص�ĩβλ��
                    stream.Seek(lEntryLength, SeekOrigin.Current);
                }

                return lEntryLength + 8;
            }
        }

        string GetCurrentLogFileName()
        {
            DateTime now = DateTime.Now;    // ���ñ���ʱ������Ҫ�Ƿ����ڰ�ҹ12���ʱ���л���־�ļ�����һ��ͼ����ڰ�ҹ���ǲ����ݡ�
            // DateTime.UtcNow;
            return Path.Combine(this.m_strDirectory, now.ToString("yyyyMMdd") + ".log");
        }

        // �򿪵�����־�ļ����������ļ�ָ������ļ�β��
        int OpenCurrentStream(out string strError)
        {
            strError = "";

            string strFileName = GetCurrentLogFileName();

            if (strFileName == this.m_strFileName)
            {
                // ����ļ������ڣ�����ҲӦ����
                Debug.Assert(this.m_stream != null, "");
            }
            else
            {
                this.CloseLogStream();   // �ȹر��Ѿ����ڵ���

                try
                {
                    // ����ļ����ڣ��ʹ򿪣�����ļ������ڣ��ʹ���һ���µ�
                    m_stream = File.Open(
        strFileName,
        FileMode.OpenOrCreate,
        FileAccess.ReadWrite,
        FileShare.ReadWrite);
                }
                catch (Exception ex)
                {
                    strError = "�򿪻򴴽��ļ� '" + strFileName + "' ��������: " + ex.Message;
                    return -1;
                }

                m_strFileName = strFileName;

                m_stream.Seek(0, SeekOrigin.End);
            }

            return 0;
        }

        string _strPrevTime = "";   // ǰһ�β�����־�ļ�����ʱ�䲿��
        long _lSeed = 0;    // ����С�ļ��������ӡ��µ�һ�뿪ʼ��Ҫ��λ���¿�ʼ

        // ���С�ļ�����������Ŀ����Ҫ��Ϊ�������Ӳ��ᷢ�����ظ��ĺ���
        string GetSmallLogFileName()
        {
            ////Debug.WriteLine("begin write lock 6");
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                DateTime now = DateTime.Now;    // ���ñ���ʱ������Ҫ�Ƿ����ڰ�ҹ12���ʱ���л���־�ļ�����һ��ͼ����ڰ�ҹ���ǲ����ݡ�
                string strTime = now.ToString("yyyyMMdd_HHmmss");

                if (strTime != _strPrevTime)
                    this._lSeed = 0;
                else
                    this._lSeed++;

                string strFileName = "";
                if (this._lSeed == 0)
                    strFileName = strTime + ".tlog";
                else
                    strFileName = strTime + "_" + _lSeed.ToString().PadLeft(4, '0') + ".tlog";

                this._strPrevTime = strTime;

                return Path.Combine(this.m_strDirectory, strFileName);
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
                ////Debug.WriteLine("end write lock 6");
            }
        }

        // ����־�ļ���д��һ����־��¼
        // parameters:
        //      attachment  ���������Ϊ null����ʾû�и���
        public int WriteEnventLog(string strXmlBody,
            Stream attachment,
            out string strError)
        {
            strError = "";

            ////Debug.WriteLine("begin write lock");
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                // ��������Χ���ж���������������Ƚϰ�ȫ
                // ��������Χ����ǰ���жϣ����ܻ����������;�����������޸ĵ����������©��������Ĵ���
                if (this._bSmallFileMode == true)
                    goto SMALL_MODE;

                int nRet = OpenCurrentStream(out strError);
                if (nRet == -1)
                    return -1;

                // д����
                // ������̿ռ�����Ҫ������β

                long lStart = this.m_stream.Position;	// ������ʼλ��

                try
                {

                    /*
                    byte[] length = new byte[8];

                    this.m_stream.Write(length, 0, 8);	// ��ʱд������,ռ�ݼ�¼�ܳ���λ��


                    // д��xml����
                    WriteEntry(this.m_stream,
                        null,
                        strXmlBody);

                    // д��attachment����
                    WriteEntry(this.m_stream,
                        null,
                        attachment);

                    long lRecordLength = this.m_stream.Position - lStart - 8;

                    // д���¼�ܳ���
                    this.m_stream.Seek(lStart, SeekOrigin.Begin);

                    length = BitConverter.GetBytes((long)lRecordLength);

                    this.m_stream.Write(length, 0, 8);

                    // ��ʹд�������ļ�
                    this.m_stream.Flush();

                    // �ļ�ָ��ص�ĩβλ��
                    this.m_stream.Seek(lRecordLength, SeekOrigin.Current);
                     * */
                    // ����־д���ļ�
                    // �������쳣
                    WriteEnventLog(
                        this.m_stream,
                        strXmlBody,
                        attachment);

                }
                catch (Exception ex)
                {
                    // ��ô֪���ǿռ���?
                    this.App.WriteErrorLog("���ش���д����־�ļ�ʱ����������" + ex.Message + "����־�ļ��ϵ�Ϊ: " + lStart.ToString());

                    // ֪ͨϵͳ����
                    this.App.HangupReason = HangupReason.OperLogError;
                    this.App.WriteErrorLog("ϵͳ��˹�����������Ŀ¼�Ƿ����㹻�ĸ�����̿ռ䡣������������ϵͳ��");

                    // ת��д�뱸���ļ�

                    try
                    {
                        // �������쳣
                        WriteEnventLog(
                            this.m_streamSpare,
                            strXmlBody,
                            attachment);
                    }
                    catch (Exception ex0)
                    {
                        this.App.WriteErrorLog("�������󣺵�д����־�ļ����������ת��д�뱸����־�ļ���������Ҳ�����쳣��" + ex0.Message);
                    }


                    // ����׳��쳣�����������Ƚض��ļ�
                    this.m_stream.SetLength(lStart);
                    // ��ʹд�������ļ�
                    this.m_stream.Flush();

                    throw ex;
                }
                return 0;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
                ////Debug.WriteLine("end write lock");
            }

        SMALL_MODE:
            // С��־�ļ�ģʽ
            // ���Բ�������Ϊ�ļ����Ѿ��ϸ�������
            {
                // ���С��־�ļ���
                string strFileName = GetSmallLogFileName();
                try
                {
                    // ����ļ����ڣ��ʹ򿪣�����ļ������ڣ��ʹ���һ���µ�
                    using (Stream stream = File.Open(
        strFileName,
        FileMode.OpenOrCreate,
        FileAccess.ReadWrite,
        FileShare.ReadWrite))
                    {
                        stream.Seek(0, SeekOrigin.End);
                        try
                        {
                            // ����־д���ļ�
                            // �������쳣
                            WriteEnventLog(
                                stream,
                                strXmlBody,
                                attachment);
                        }
                        catch (Exception ex)
                        {
                            // ��ô֪���ǿռ���?
                            this.App.WriteErrorLog("���ش���д����ʱ��־�ļ� '" + strFileName + "' ʱ����������" + ex.Message);

                            // ֪ͨϵͳ����
                            this.App.HangupReason = HangupReason.OperLogError;
                            this.App.WriteErrorLog("ϵͳ��˹�����������Ŀ¼�Ƿ����㹻�ĸ�����̿ռ䡣������������ϵͳ��");
                            throw ex;
                        }
                    }
                }
                catch (Exception ex)
                {
                    strError = "�򿪻򴴽��ļ� '" + strFileName + "' ��������: " + ex.Message;
                    return -1;
                }
                return 0;
            }
        }

#if NO
        // TODO: �����Ԥ��֪����������ĳ��ȣ��ڿ�ͷ��д�ó���λ���������ļ�ָ���������ٶȾ͸�����
        // ����־д���ļ�
        // �������쳣
        // parameters:
        //      attachment  ���������Ϊ null����ʾû�и���
        static void WriteEnventLog(
            Stream stream,
            string strXmlBody,
            Stream attachment)
        {
            long lStart = stream.Position;	// ������ʼλ��

            byte[] length = new byte[8];

            // ���
            for (int i = 0; i < length.Length; i++)
            {
                length[i] = 0;
            }

            stream.Write(length, 0, 8);	// ��ʱд������,ռ�ݼ�¼�ܳ���λ��

            // д��xml����
            WriteEntry(
                stream,
                null,
                strXmlBody);

            // д��attachment����
            WriteEntry(
                stream,
                null,
                attachment);

            long lRecordLength = stream.Position - lStart - 8;

            // д���¼�ܳ���
            if (stream.Position != lStart)
            {
                // stream.Seek(lStart, SeekOrigin.Begin);  // �ٶ���!
                long lDelta = lStart - stream.Position;
                stream.Seek(lDelta, SeekOrigin.Current);
            }

            length = BitConverter.GetBytes((long)lRecordLength);

            stream.Write(length, 0, 8);

            // ��ʹд�������ļ�
            stream.Flush();

            // �ļ�ָ��ص�ĩβλ��
            stream.Seek(lRecordLength, SeekOrigin.Current);
        }

#endif

        // �Ľ���汾
        // TODO: �����Ԥ��֪����������ĳ��ȣ��ڿ�ͷ��д�ó���λ���������ļ�ָ���������ٶȾ͸�����
        // ����־д���ļ�
        // �������쳣
        // parameters:
        //      attachment  ���������Ϊ null����ʾû�и���
        static void WriteEnventLog(
            Stream stream,
            string strXmlBody,
            Stream attachment)
        {
            long lStart = stream.Position;	// ������ʼλ��

            byte[] length = new byte[8];

            // ���
            for (int i = 0; i < length.Length; i++)
            {
                length[i] = 0;
            }

            // ��� XML ���ֵĳ���
            long lXmlBodyLength = WriteEntry(
                stream,
                null,
                strXmlBody, 
                false,
                0);
            // ��� attachment ���ֵĳ���
            long lAttachmentLength = WriteEntry(
                stream,
                null,
                attachment,
                false,
                0);
            length = BitConverter.GetBytes(lXmlBodyLength + lAttachmentLength);

            stream.Write(length, 0, 8);	// д���ܳ���

            // ����д�� XML ����
            WriteEntry(
                stream,
                null,
                strXmlBody,
                true,
                lXmlBodyLength);

            // ����д�� attachment ����
            WriteEntry(
                stream,
                null,
                attachment,
                true,
                lAttachmentLength);

            long lRecordLength = stream.Position - lStart - 8;

            Debug.Assert(lRecordLength == lXmlBodyLength + lAttachmentLength, "");

#if NO
            // д���¼�ܳ���
            if (stream.Position != lStart)
            {
                // stream.Seek(lStart, SeekOrigin.Begin);  // �ٶ���!
                long lDelta = lStart - stream.Position;
                stream.Seek(lDelta, SeekOrigin.Current);
            }

            length = BitConverter.GetBytes((long)lRecordLength);

            stream.Write(length, 0, 8);

            // ��ʹд�������ļ�
            stream.Flush();

            // �ļ�ָ��ص�ĩβλ��
            stream.Seek(lRecordLength, SeekOrigin.Current);
#endif
            // ��ʹд�������ļ�
            stream.Flush();
        }

        void ReOpen()
        {
            if (this.m_stream != null)
            {
                this.m_stream.Close();

                this.m_stream = null;
                this.m_stream = File.Open(
                    this.m_strFileName,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite);
                this.m_stream.Seek(0, SeekOrigin.End);

            }
        }

        // ��װ��İ汾
        public int WriteOperLog(XmlDocument dom,
            string strClientAddress,
            out string strError)
        {
            string strRefID = "";
            return WriteOperLog(dom, strClientAddress, new DateTime(0), out strRefID, out strError);
        }

        // д��һ��������־
        // parameters:
        //      start_time  ������ʼ��ʱ�䡣������������������������ķѵ�ʱ�䡣��� ticks == 0����ʾ��ʹ�����ֵ
        public int WriteOperLog(XmlDocument dom,
            string strClientAddress,
            DateTime start_time,
            out string strRefID,
            out string strError)
        {
            strRefID = "";

            // 2013/11/20
            if (this._bSmallFileMode == false
                && this.m_streamSpare == null)
            {
                strError = "��־�����ļ�δ��ȷ��ʼ��";
                return -1;
            }

#if DEBUG
            if (this._bSmallFileMode == false)
            {
                Debug.Assert(this.m_streamSpare != null, "m_streamSpare == null");
            }
#endif

            WriteClientAddress(dom, strClientAddress);
            // 1.01 (2014/3/8) �޸��� operation=amerce;action=expire ��¼��Ԫ���� oldReeaderRecord Ϊ oldReaderRecord
            DomUtil.SetElementText(dom.DocumentElement, "version", "1.01");

            if (start_time != new DateTime(0))
            {
                XmlElement time = dom.CreateElement("time");
                dom.DocumentElement.AppendChild(time);
                DateTime now = DateTime.Now;
                time.SetAttribute("start", start_time.ToString("s"));
                time.SetAttribute("end", now.ToString("s"));
                time.SetAttribute("seconds", (now - start_time).TotalSeconds.ToString());

                // ��־��¼��Ψһ ID
                strRefID = Guid.NewGuid().ToString();
                DomUtil.SetElementText(dom.DocumentElement, "uid", strRefID);
            }

            int nRet = WriteEnventLog(dom.OuterXml,
                null,
                out strError);
            if (nRet == -1)
                return -1;

            // ReOpen();          


            return 0;
        }

        static void WriteClientAddress(XmlDocument dom,
            string strClientAddress)
        {
            if (string.IsNullOrEmpty(strClientAddress) == true)
                return;

            string strVia = "";
            int nRet = strClientAddress.IndexOf("@");
            if (nRet != -1)
            {
                strVia = strClientAddress.Substring(nRet + 1);
                strClientAddress = strClientAddress.Substring(0, nRet);
            }
            XmlNode node = DomUtil.SetElementText(dom.DocumentElement,
                "clientAddress",
                strClientAddress);
            if (string.IsNullOrEmpty(strVia) == false)
                DomUtil.SetAttr(node, "via", strVia);
        }

        // д��һ��������־(��һ�汾)
        public int WriteOperLog(XmlDocument dom,
            string strClientAddress,
            Stream attachment,
            out string strError)
        {
            Debug.Assert(this.m_streamSpare != null, "");

            WriteClientAddress(dom, strClientAddress);

            int nRet = WriteEnventLog(dom.OuterXml,
                attachment,
                out strError);
            if (nRet == -1)
                return -1;

            // ReOpen();          

            return 0;
        }


        // ԭ�Ȱ汾
        // ���һ����־��¼
        // parameters:
        //      strLibraryCodeList  ��ǰ�û���Ͻ�Ĺݴ����б�
        //      strFileName ���ļ���,����·�����֡���Ҫ����".log"���֡�
        //      lIndex  ��¼��š���0��ʼ������lIndexΪ-1ʱ���ñ���������ʾϣ����������ļ��ߴ�ֵ����������lHintNext�С�
        //      lHint   ��¼λ�ð�ʾ�Բ���������һ��ֻ�з������������׺����ֵ������ǰ����˵�ǲ�͸���ġ�
        //              Ŀǰ�ĺ����Ǽ�¼��ʼλ�á�
        // return:
        //      -1  error
        //      0   file not found
        //      1   succeed
        //      2   ������Χ
        public int GetOperLog(
            string strLibraryCodeList,
            string strFileName,
            long lIndex,
            long lHint,
            string strStyle,
            string strFilter,
            out long lHintNext,
            out string strXml,
            ref Stream attachment,
            out string strError)
        {
            strError = "";
            strXml = "";
            lHintNext = -1;

            int nRet = 0;

            Stream stream = null;

            if (string.IsNullOrEmpty(this.m_strDirectory) == true)
            {
                strError = "��־Ŀ¼ m_strDirectory ��δ��ʼ��";
                return -1;
            }
            Debug.Assert(this.m_strDirectory != "", "");

            string strFilePath = this.m_strDirectory + "\\" + strFileName;

            try
            {
                stream = File.Open(
                    strFilePath,
                    FileMode.Open,
                    FileAccess.ReadWrite, // Read������޷��� 2007/5/22
                    FileShare.ReadWrite);   
            }
            catch (FileNotFoundException /*ex*/)
            {
                strError = "��־�ļ� " + strFileName + "û���ҵ�";
                lHintNext = 0;
                return 0;   // file not found
            }
            catch (Exception ex)
            {
                strError = "����־�ļ� '" + strFileName + "' ��������: " + ex.Message;
                return -1;
            }

            try
            {
                long lFileSize = 0;
                // ��λ

                // ����
                // �ڻ���ļ��������ȵĹ����У�����ҪС��������Ⲣ�������ڶ��ļ�����д�Ĳ���
                bool bLocked = false;

                // �����ȡ���ǵ�ǰ����д����ȵ���־�ļ�������Ҫ������������
                if (PathUtil.IsEqual(strFilePath, this.m_strFileName) == true)
                {
                    ////Debug.WriteLine("begin read lock 1");
                    this.m_lock.AcquireReaderLock(m_nLockTimeout);
                    bLocked = true;
                }

                try
                {   // begin of lock try
                    lFileSize = stream.Length;
                }   // end of lock try
                finally
                {
                    if (bLocked == true)
                    {
                        this.m_lock.ReleaseReaderLock();
                        ////Debug.WriteLine("end read lock 1");
                    }
                }
                    // lIndex == -1��ʾϣ������ļ������ĳߴ�
                    if (lIndex == -1)
                    {
                        lHintNext = lFileSize;  //  stream.Length;
                        return 1;   // �ɹ�
                    }

                    // û�а�ʾ��ֻ�ܴ�ͷ��ʼ��
                    if (lHint == -1 || lIndex == 0)
                    {
                        // return:
                        //      -1  error
                        //      0   �ɹ�
                        //      1   �����ļ�ĩβ���߳���
                        nRet = LocationRecord(stream,
                            lFileSize,
                            lIndex,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet == 1)
                            return 2;
                    }
                    else
                    {
                        // ���ݰ�ʾ�ҵ�
                        if (lHint == stream.Length)
                            return 2;

                        if (lHint > stream.Length)
                        {
                            strError = "lHint����ֵ����ȷ";
                            return -1;
                        }
                        if (stream.Position != lHint)
                            stream.Seek(lHint, SeekOrigin.Begin);
                    }

                    //////

                // MemoryStream attachment = null; // new MemoryStream();
                // TODO: �Ƿ�����Ż�Ϊ���ȶ���XML���֣������Ҫ�ٶ���attachment? ����attachment���԰�������ֶ�
                // return:
                //      1   ����
                //      0   �ɹ�
                //      1   �ļ����������ζ�����Ч
                nRet = ReadEnventLog(
                    stream,
                    out strXml,
                    ref attachment,
                    out strError);
                if (nRet == -1)
                    return -1;

                // ���Ƽ�¼�۲췶Χ
                if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                {
                    // ���͹�����־XML��¼
                    // return:
                    //      -1  ����
                    //      0   �������ص�ǰ��־��¼
                    //      1   ����Χ��ǰ��־��¼
                    nRet = FilterXml(
                        strLibraryCodeList,
                        strStyle,
                        strFilter,
                        ref strXml,
                        out strError);
                    if (nRet == -1)
                    {
                        nRet = 1;   // ֻ�÷���

                        // return -1;
                    }
                    if (nRet == 0)
                    {
                        strXml = "";    // ��գ���ǰ�˿���������
                        attachment.SetLength(0);    // ��ո���
                    }
                }
                else
                {
                    // ��Ȼ��ȫ���û���ҲҪ���Ƽ�¼�ߴ�
                    nRet = ResizeXml(
                        strStyle,
                        strFilter,
                        ref strXml,
                        out strError);
                    if (nRet == -1)
                    {
                        nRet = 1;   // ֻ�÷���
                        // return -1;
                    }
                }

                lHintNext = stream.Position;
                return 1;
            }
            finally
            {
                stream.Close();
            }
        }

        // �����ϸ����
        // return:
        //      0   ȫ��
        //      1   ɾ�� ���߼�¼�Ͳ��¼
        //      2   ɾ�� ���߼�¼�Ͳ��¼�е� <borrowHistory>
        static int GetLevel(string strStyle)
        {
            // 2013/11/6
            if (string.IsNullOrEmpty(strStyle) == true)
                return 0;

            string [] parts = strStyle.Split(new char [] {','}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in parts)
            {
                if (StringUtil.HasHead(s, "level-") == true)
                {
                    string strNumber = s.Substring("level-".Length).Trim();
                    int v = 0;
                    Int32.TryParse(strNumber, out v);
                    return v;
                }
            }

            return 0;
        }

        // return:
        //      -1  ����
        //      0   û�иı�
        //      1   �����˸ı�
        static int ResizeXml(
            string strStyle,
            string strFilter,
            ref string strXml,
            out string strError)
        {
            strError = "";

            int nLevel = -1;
            // �ȼ��һ�Σ��������ĳЩ����µ������ٶ�
            if (string.IsNullOrEmpty(strFilter) == true)
            {
                // �����ϸ����
                // return:
                //      0   ȫ��
                //      1   ɾ�� ���߼�¼�Ͳ��¼
                //      2   ɾ�� ���߼�¼�Ͳ��¼�е� <borrowHistory>
                nLevel = GetLevel(strStyle);
                if (nLevel == 0)
                    return 0;
            }

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "��־��¼XML����װ��XMLDOMʱ����: " + ex.Message;
                return -1;
            }

            string strOperation = DomUtil.GetElementText(dom.DocumentElement, "operation");
            // 2013/11/22
            if (string.IsNullOrEmpty(strFilter) == false
                && StringUtil.IsInList(strOperation, strFilter) == false)
            {
                strXml = "";
                return 1;
            }

            if (nLevel == -1)
            {
                // �����ϸ����
                // return:
                //      0   ȫ��
                //      1   ɾ�� ���߼�¼�Ͳ��¼
                //      2   ɾ�� ���߼�¼�Ͳ��¼�е� <borrowHistory>
                nLevel = GetLevel(strStyle);
                if (nLevel == 0)
                    return 0;
            }

            {
#if NO
                // ҲҪ���ٳߴ�
                if (strOperation == "borrow")
                {
                    ResizeBorrow(nLevel, ref dom);
                }
                else if (strOperation == "return")
                {
                    ResizeReturn(nLevel, ref dom);
                }
                else if (strOperation == "setEntity")
                {
                    ResizeSetEntity(nLevel, ref dom);
                }
                else if (strOperation == "setReaderInfo")
                {
                    ResizeSetReaderInfo(nLevel, ref dom);
                }
                else if (strOperation == "amerce")
                {
                    ResizeAmerce(nLevel, ref dom);
                } 
#endif
                // ���ٳߴ�
                ResizeXml(strOperation,
                    nLevel,
                    ref dom);
                strXml = dom.DocumentElement.OuterXml;
            }

            return 1;
        }

        // ���͹�����־XML��¼
        // return:
        //      -1  ����
        //      0   �������ص�ǰ��־��¼
        //      1   �����ص�ǰ��־��¼
        static int FilterXml(
            string strLibraryCodeList,
            string strStyle,
            string strFilter,
            ref string strXml,
            out string strError)
        {
            strError = "";

            // �����ϸ����
            // return:
            //      0   ȫ��
            //      1   ɾ�� ���߼�¼�Ͳ��¼
            //      2   ɾ�� ���߼�¼�Ͳ��¼�е� <borrowHistory>
            int nLevel = GetLevel(strStyle);

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "��־��¼XML����װ��XMLDOMʱ����: " + ex.Message;
                return -1;
            }

            // �ֹ��û��������� setUser ������Ϣ
            string strOperation = DomUtil.GetElementText(dom.DocumentElement, "operation");
            if (strOperation == "setUser")
            {
                if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    return 0;
            }

            // 2013/11/22
            if (string.IsNullOrEmpty(strFilter) == false
                && StringUtil.IsInList(strOperation, strFilter) == false)
                return 0;

            XmlNode node = null;
            string strLibraryCodes = DomUtil.GetElementText(dom.DocumentElement, "libraryCode", out node);
            if (node == null)
                return 1;  // ����Ҫ����
            string strSourceLibraryCode = "";
            string strTargetLibraryCode = "";
            ParseLibraryCodes(strLibraryCodes,
out strSourceLibraryCode,
out strTargetLibraryCode);

            // source�ڹ�Ͻ��Χ�ڣ�target���ڹ�Ͻ��Χ��
            // ��־��¼��Ҫ�任���൱�ڸ��߷����ߣ�������¼�������ˣ������޸ĵ���Ϣ��Ҫ͸¶
            if (strSourceLibraryCode != strTargetLibraryCode
                && StringUtil.IsInList(strSourceLibraryCode, strLibraryCodeList) == true
                && StringUtil.IsInList(strTargetLibraryCode, strLibraryCodeList) == false)
            {

                if (strOperation == "devolveReaderInfo")
                {
                    FilterDovolveReaderInfo(ref dom);
                }
                else if (strOperation == "setEntity")
                {
                    FilterSetEntity(// nLevel, 
                        ref dom);
                }
                else if (strOperation == "setReaderInfo")
                {
                    FilterSetReaderInfo(// nLevel, 
                        ref dom);
                }
#if NO
                else if (strOperation == "borrow" && nLevel > 0)
                {
                    ResizeBorrow(nLevel, ref dom);
                }
                else if (strOperation == "return" && nLevel > 0)
                {
                    ResizeReturn(nLevel, ref dom);
                }
                else if (strOperation == "setEntity" && nLevel > 0)
                {
                    ResizeSetEntity(nLevel, ref dom);
                }
                else if (strOperation == "setReaderInfo" && nLevel > 0)
                {
                    ResizeSetReaderInfo(nLevel, ref dom);
                }
                else if (strOperation == "amerce" && nLevel > 0)
                {
                    ResizeAmerce(nLevel, ref dom);
                }
#endif

                {
                    // ���ٳߴ�
                    ResizeXml(strOperation,
                        nLevel,
                        ref dom);
                    strXml = dom.DocumentElement.OuterXml;
                } 
                return 1;
            }

            if (StringUtil.IsInList(strTargetLibraryCode, strLibraryCodeList) == false)
                return 0;

            {
                // ���ٳߴ�
                ResizeXml(strOperation,
                    nLevel,
                    ref dom);
                strXml = dom.DocumentElement.OuterXml;
            }

            // ��ɷ�����־��¼
            return 1;
        }

        // ����־��¼���м�С�ߴ�Ĳ���
        static void ResizeXml(string strOperation,
            int nLevel,
            ref XmlDocument dom)
        {
            if (strOperation == "borrow" && nLevel > 0)
            {
                ResizeBorrow(nLevel, ref dom);
            }
            else if (strOperation == "return" && nLevel > 0)
            {
                ResizeReturn(nLevel, ref dom);
            }
            else if (strOperation == "setEntity" && nLevel > 0)
            {
                ResizeSetEntity(nLevel, ref dom);
            }
            else if (strOperation == "setOrder" && nLevel > 0)
            {
                ResizeSetOrder(nLevel, ref dom);
            }
            else if (strOperation == "setIssue" && nLevel > 0)
            {
                ResizeSetIssue(nLevel, ref dom);
            }
            else if (strOperation == "setComment" && nLevel > 0)
            {
                ResizeSetComment(nLevel, ref dom);
            }
            else if (strOperation == "setReaderInfo" && nLevel > 0)
            {
                ResizeSetReaderInfo(nLevel, ref dom);
            }
            else if (strOperation == "amerce" && nLevel > 0)
            {
                ResizeAmerce(nLevel, ref dom);
            }
            else if (strOperation == "hire" && nLevel > 0)
            {
                ResizeHire(nLevel, ref dom);
            }
            else if (strOperation == "foregift" && nLevel > 0)
            {
                ResizeForegift(nLevel, ref dom);
            }
            else if (strOperation == "settlement" && nLevel > 0)
            {
                ResizeSettlement(nLevel, ref dom);
            }
            else if (strOperation == "changeReaderPassword" && nLevel > 0)
            {
                ResizeChangeReaderPassword(nLevel, ref dom);
            }
            else if (strOperation == "setBiblioInfo" && nLevel > 0)
            {
                ResizeSetBiblioInfo(nLevel, ref dom);
            }
        }

        // ����ת�ƶ��ߵ���־��¼
        static void FilterDovolveReaderInfo(ref XmlDocument dom)
        {
            // ɾ��<targetReaderRecord>Ԫ��
            DomUtil.DeleteElement(dom.DocumentElement, "targetReaderRecord");
        }

        // ��������ʵ�����־��¼
        static void FilterSetEntity(// int nLevel,
            ref XmlDocument dom)
        {
            // ɾ��<record>Ԫ��
            DomUtil.DeleteElement(dom.DocumentElement, "record");
            // ResizeSetEntity(nLevel, ref dom);
        }

        // �������ö��߼�¼����־��¼
        static void FilterSetReaderInfo(// int nLevel, 
            ref XmlDocument dom)
        {
            // ɾ��<record>Ԫ��
            DomUtil.DeleteElement(dom.DocumentElement, "record");
            // ResizeSetReaderInfo(nLevel, ref dom);
        }

        // ��Ŀ
        static void RemoveBiblioRecord(int nLevel,
    string strElementName,
    ref XmlDocument dom)
        {
            if (nLevel == 0)
                return;

            {
                string strBiblioReacord = DomUtil.GetElementText(dom.DocumentElement, strElementName);
                if (string.IsNullOrEmpty(strBiblioReacord) == false)
                {
                    if (nLevel == 2)
                    {
                        DomUtil.SetElementText(dom.DocumentElement, strElementName, "");
                        return;
                    }

                }
            }
        }

        static string GetParentID(string strRecord)
        {
            XmlDocument reader_dom = new XmlDocument();
            try
            {
                reader_dom.LoadXml(strRecord);
                return DomUtil.GetElementText(reader_dom.DocumentElement, "parent");
            }
            catch
            {
                return null;
            }
        }

        // ��
        static void RemoveEntityRecord(int nLevel,
    string strElementName,
    ref XmlDocument dom)
        {
            if (nLevel == 0)
                return;

            {
                XmlNode record_node = null;
                string strItemReacord = DomUtil.GetElementText(dom.DocumentElement, strElementName, out record_node);
                if (string.IsNullOrEmpty(strItemReacord) == false)
                {
                    if (nLevel == 2)
                    {
                        // ���� parent_id ���ԣ���� InnerXml
                        if (record_node != null)
                        {
                            string strParentID = (record_node as XmlElement).GetAttribute("parent_id");
                            if (string.IsNullOrEmpty(strParentID) == true)
                            {
                                strParentID = GetParentID(strItemReacord);

                                if (string.IsNullOrEmpty(strParentID) == false)
                                    (record_node as XmlElement).SetAttribute("parent_id", strParentID);
                            }

                            record_node.InnerXml = "";
                        }

                        // DomUtil.SetElementText(dom.DocumentElement, strElementName, "");
                        return;
                    }

                    // nLevel == 1
                    XmlDocument reader_dom = new XmlDocument();
                    try
                    {
                        reader_dom.LoadXml(strItemReacord);
                        XmlNode node = reader_dom.DocumentElement.SelectSingleNode("borrowHistory");
                        if (node != null)
                            node.ParentNode.RemoveChild(node);
                        DomUtil.SetElementText(dom.DocumentElement, strElementName, reader_dom.DocumentElement.OuterXml);
                    }
                    catch
                    {
                    }
                }
            }
        }

        // ���� �� ��ע
        static void RemoveItemRecord(int nLevel, 
            string strElementName,
            ref XmlDocument dom)
        {
            if (nLevel == 0)
                return;

            {
                XmlNode node = null;
                string strItemReacord = DomUtil.GetElementText(dom.DocumentElement, strElementName, out node);
                if (string.IsNullOrEmpty(strItemReacord) == false)
                {
                    if (nLevel == 2)
                    {
                        // ���� parent_id ���ԣ���� InnerXml
                        if (node != null)
                        {
                            string strParentID = (node as XmlElement).GetAttribute("parent_id");
                            if (string.IsNullOrEmpty(strParentID) == true)
                            {
                                strParentID = GetParentID(strItemReacord);

                                if (string.IsNullOrEmpty(strParentID) == false)
                                    (node as XmlElement).SetAttribute("parent_id", strParentID);
                            }

                            node.InnerXml = "";
                        }

                        // DomUtil.SetElementText(dom.DocumentElement, strElementName, "");
                        return;
                    }
                }
            }
        }

        static void RemoveReaderRecord(int nLevel,
            string strElementName,
            ref XmlDocument dom)
        {
            if (nLevel == 0)
                return;

            {
                string strReaderReacord = DomUtil.GetElementText(dom.DocumentElement, strElementName);
                if (string.IsNullOrEmpty(strReaderReacord) == false)
                {
                    if (nLevel == 2)
                    {
                        DomUtil.SetElementText(dom.DocumentElement, strElementName, "");
                        return;
                    }

                    // nLevel == 1
                    XmlDocument reader_dom = new XmlDocument();
                    try
                    {
                        reader_dom.LoadXml(strReaderReacord);
                        XmlNode node = reader_dom.DocumentElement.SelectSingleNode("borrowHistory");
                        if (node != null)
                            node.ParentNode.RemoveChild(node);
                        DomUtil.SetElementText(dom.DocumentElement, strElementName, reader_dom.DocumentElement.OuterXml);
                    }
                    catch
                    {
                    }
                }
            }
        }

        static void RemoveAmerceRecord(int nLevel,
    string strElementName,
    ref XmlDocument dom)
        {
            if (nLevel == 0)
                return;

            {
                string strReaderReacord = DomUtil.GetElementText(dom.DocumentElement, strElementName);
                if (string.IsNullOrEmpty(strReaderReacord) == false)
                {
                    if (nLevel == 2)
                    {
                        DomUtil.SetElementText(dom.DocumentElement, strElementName, "");
                        return;
                    }
                }
            }
        }

        // ���˽��ĵ���־��¼
        static void ResizeBorrow(int nLevel, ref XmlDocument dom)
        {
            RemoveReaderRecord(nLevel, "readerRecord", ref dom);
            RemoveEntityRecord(nLevel, "itemRecord", ref dom);
        }

        // ���˻������־��¼
        static void ResizeReturn(int nLevel, ref XmlDocument dom)
        {
            RemoveReaderRecord(nLevel, "readerRecord", ref dom);
            RemoveEntityRecord(nLevel, "itemRecord", ref dom);
        }

        static void ResizeSetEntity(int nLevel, ref XmlDocument dom)
        {
            // �� <record> Ԫ�����ɾ�� <borrowHistory>
            RemoveEntityRecord(nLevel > 1 ? 1 : nLevel, "record", ref dom);
            RemoveEntityRecord(nLevel, "oldRecord", ref dom);
        }

        static void ResizeSetOrder(int nLevel, ref XmlDocument dom)
        {
            RemoveItemRecord(nLevel, "oldRecord", ref dom);
        }

        static void ResizeSetIssue(int nLevel, ref XmlDocument dom)
        {
            RemoveItemRecord(nLevel, "oldRecord", ref dom);
        }

        static void ResizeSetComment(int nLevel, ref XmlDocument dom)
        {
            RemoveItemRecord(nLevel, "oldRecord", ref dom);
        }

        static void ResizeSetReaderInfo(int nLevel, ref XmlDocument dom)
        {
            // �� <record> Ԫ�����ɾ�� <borrowHistory>
            RemoveReaderRecord(nLevel > 1 ? 1 : nLevel, "record", ref dom);

            // ��� <record> �м�¼Ϊ�գ�����Ҫ�� <oldRecord> ��Ų��
            if (nLevel > 1)
            {
                string strAction = DomUtil.GetElementText(dom.DocumentElement, "action");
                if (strAction == "move" || strAction == "copy")
                {
                    string strReaderReacord = DomUtil.GetElementText(dom.DocumentElement, "record");
                    if (string.IsNullOrEmpty(strReaderReacord) == true)
                    {
                        RemoveReaderRecord(nLevel > 1 ? 1 : nLevel, "oldRecord", ref dom);
                        return;
                    }
                }
            }

            RemoveReaderRecord(nLevel, "oldRecord", ref dom);
        }

        static void ResizeSetBiblioInfo(int nLevel, ref XmlDocument dom)
        {
            RemoveBiblioRecord(nLevel > 1 ? 1 : nLevel, "record", ref dom);

            // ��� <record> �м�¼Ϊ�գ�����Ҫ�� <oldRecord> ��Ų��
            if (nLevel > 1)
            {
                string strAction = DomUtil.GetElementText(dom.DocumentElement, "action");
                if (strAction == "move" || strAction == "copy"
                    || strAction == "onlymovebiblio" || strAction == "onlycopybiblio")
                {
                    string strBiblioReacord = DomUtil.GetElementText(dom.DocumentElement, "record");
                    if (string.IsNullOrEmpty(strBiblioReacord) == true)
                    {
                        RemoveBiblioRecord(nLevel > 1 ? 1 : nLevel, "oldRecord", ref dom);
                        return;
                    }
                }
            }

            RemoveBiblioRecord(nLevel, "oldRecord", ref dom);
        }

        static void ResizeAmerce(int nLevel, ref XmlDocument dom)
        {
            RemoveReaderRecord(nLevel, "readerRecord", ref dom);
            RemoveReaderRecord(nLevel, "oldReaderRecord", ref dom);
        }

        static void ResizeHire(int nLevel, ref XmlDocument dom)
        {
            RemoveReaderRecord(nLevel, "readerRecord", ref dom);
        }

        static void ResizeForegift(int nLevel, ref XmlDocument dom)
        {
            RemoveReaderRecord(nLevel, "readerRecord", ref dom);
        }

        static void ResizeSettlement(int nLevel, ref XmlDocument dom)
        {
            RemoveAmerceRecord(nLevel, "oldAmerceRecord", ref dom);
        }

        static void ResizeChangeReaderPassword(int nLevel, ref XmlDocument dom)
        {
            RemoveReaderRecord(nLevel, "readerRecord", ref dom);
        }

        // 2012/9/23
        // ���һ����־��¼�ĸ���Ƭ��
        // parameters:
        //      strLibraryCodeList  ��ǰ�û���Ͻ�Ĺݴ����б�
        //      strFileName ���ļ���,����·�����֡���Ҫ����".log"���֡�
        //      lIndex  ��¼��š���0��ʼ������lIndexΪ-1ʱ���ñ���������ʾϣ����������ļ��ߴ�ֵ����������lHintNext�С�
        //      lHint   ��¼λ�ð�ʾ�Բ���������һ��ֻ�з������������׺����ֵ������ǰ����˵�ǲ�͸���ġ�
        //              Ŀǰ�ĺ����Ǽ�¼��ʼλ�á�
        // return:
        //      -1  error
        //      0   file not found
        //      1   succeed
        //      2   ������Χ
        public int GetOperLogAttachment(
            string strLibraryCodeList,
            string strFileName,
            long lIndex,
            long lHint,
            long lAttachmentFragmentStart,
            int nAttachmentFragmentLength,
            out byte[] attachment_data,
            out long lAttachmentLength,
            out string strError)
        {
            strError = "";
            attachment_data = null;
            long lHintNext = -1;
            lAttachmentLength = 0;

            int nRet = 0;

            Stream stream = null;

            if (string.IsNullOrEmpty(this.m_strDirectory) == true)
            {
                strError = "��־Ŀ¼ m_strDirectory ��δ��ʼ��";
                return -1;
            }
            Debug.Assert(this.m_strDirectory != "", "");

            string strFilePath = this.m_strDirectory + "\\" + strFileName;

            try
            {
                stream = File.Open(
                    strFilePath,
                    FileMode.Open,
                    FileAccess.ReadWrite, // Read������޷��� 2007/5/22
                    FileShare.ReadWrite);
            }
            catch (FileNotFoundException /*ex*/)
            {
                strError = "��־�ļ� " + strFileName + "û���ҵ�";
                return 0;   // file not found
            }
            catch (Exception ex)
            {
                strError = "����־�ļ� '" + strFileName + "' ��������: " + ex.Message;
                return -1;
            }

            try
            {
                long lFileSize = 0;
                // ��λ

                // ����
                // �ڻ���ļ��������ȵĹ����У�����ҪС��������Ⲣ�������ڶ��ļ�����д�Ĳ���
                bool bLocked = false;

                // �����ȡ���ǵ�ǰ����д����ȵ���־�ļ�������Ҫ������������
                if (PathUtil.IsEqual(strFilePath, this.m_strFileName) == true)
                {
                    ////Debug.WriteLine("begin read lock 2");
                    this.m_lock.AcquireReaderLock(m_nLockTimeout);
                    bLocked = true;
                }

                try
                {   // begin of lock try
                    lFileSize = stream.Length;

                }   // end of lock try
                finally
                {
                    if (bLocked == true)
                    {
                        this.m_lock.ReleaseReaderLock();
                        ////Debug.WriteLine("end read lock 2");
                    }
                }
                    // lIndex == -1��ʾϣ������ļ������ĳߴ�
                    if (lIndex == -1)
                    {
                        lHintNext = lFileSize;  // stream.Length;
                        return 1;   // �ɹ�
                    }

                    // û�а�ʾ��ֻ�ܴ�ͷ��ʼ��
                    if (lHint == -1 || lIndex == 0)
                    {
                        // return:
                        //      -1  error
                        //      0   �ɹ�
                        //      1   �����ļ�ĩβ���߳���
                        nRet = LocationRecord(stream,
                            lFileSize,
                            lIndex,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet == 1)
                            return 2;
                    }
                    else
                    {
                        // ���ݰ�ʾ�ҵ�
                        if (lHint == stream.Length)
                            return 2;

                        if (lHint > stream.Length)
                        {
                            strError = "lHint����ֵ����ȷ";
                            return -1;
                        }
                        if (stream.Position != lHint)
                            stream.Seek(lHint, SeekOrigin.Begin);
                    }


                // return:
                //      -1  ����
                //      >=0 ���������ĳߴ�
                lAttachmentLength = ReadEnventLogAttachment(
                    stream,
                    lAttachmentFragmentStart,
                    nAttachmentFragmentLength,
                    out attachment_data, 
                    out strError);
                if (nRet == -1)
                    return -1;

                // �޷����Ƽ�¼�۲췶Χ
            END1:
                lHintNext = stream.Position;

                return 1;
            }
            finally
            {
                stream.Close();
            }
        }

        const int MAX_FILENAME_COUNT = 100;

        // parameters:
        //      nCount  ����ϣ����ȡ�ļ�¼�������==-1����ʾϣ�������ܶ�ػ�ȡ
        // return:
        //      -1  error
        //      0   file not found
        //      1   succeed
        //      2   ������Χ�����ε�����Ч
        public int GetOperLogs(
            string strLibraryCodeList,
            string strFileName,
            long lIndex,
            long lHint,
            int nCount,
            string strStyle,
            string strFilter,
            out OperLogInfo[] records,
            out string strError)
        {
            records = null;
            strError = "";
            List<OperLogInfo> results = new List<OperLogInfo>();

            if (strStyle == "getfilenames")
            {
                DirectoryInfo di = new DirectoryInfo(this.m_strDirectory);
                FileInfo[] fis = di.GetFiles("????????.log");

                if (fis.Length == 0)
                    return 0;   // һ���ļ�Ҳû��

                // ����С����ǰ
                Array.Sort(fis, new FileInfoCompare(true));

                int nStart = (int)lIndex;
                int nEnd = fis.Length;
                if (nCount == -1)
                    nEnd = fis.Length;
                else
                    nEnd = Math.Min(nStart + nCount, fis.Length);

                // һ�β��ó����������
                if (nEnd - nStart > MAX_FILENAME_COUNT)
                    nEnd = nStart + MAX_FILENAME_COUNT;
                for (int i = nStart; i < nEnd; i++)
                {
                    OperLogInfo info = new OperLogInfo();
                    info.Index = i;
                    info.Xml = fis[i].Name;
                    info.AttachmentLength = fis[i].Length;
                    results.Add(info);
                }

                records = new OperLogInfo[results.Count];
                results.CopyTo(records);
                return 1;
            }

            int nPackageLength = 0;

            string strXml = "";
            long lAttachmentLength = 0;
            long lHintNext = -1;
            for (int i = 0; i < nCount || nCount == -1; i++)
            {
                // return:
                //      -1  error
                //      0   file not found
                //      1   succeed
                //      2   ������Χ
                int nRet = GetOperLog(
                    strLibraryCodeList,
                    strFileName,
                    lIndex,
                    lHint,
                    strStyle,
                    strFilter,
                    out lHintNext,
                    out strXml,
                    out lAttachmentLength,
                    out strError);
                if (nRet == -1)
                    return -1;
                if (nRet == 0)
                    return 0;
                if (nRet == 2)
                {
                    if (i == 0)
                        return 2;   // ���ε�����Ч
                    break;
                }

                nPackageLength += strXml.Length + 100;  // �߽ǳߴ�

                if (nPackageLength > 500 * 1024
                    && i > 0)
                    break;

                OperLogInfo info = new OperLogInfo();
                info.Index = lIndex;
                info.HintNext = lHintNext;
                info.Xml = strXml;
                info.AttachmentLength = lAttachmentLength;
                results.Add(info);


                lIndex++;
                lHint = lHintNext;
            }

            records = new OperLogInfo[results.Count];
            results.CopyTo(records);
            return 1;
        }


        // ���һ����־��¼
        // parameters:
        //      strLibraryCodeList  ��ǰ�û���Ͻ�Ĺݴ����б�
        //      strFileName ���ļ���,����·�����֡���Ҫ����".log"���֡�
        //      lIndex  ��¼��š���0��ʼ������lIndexΪ-1ʱ���ñ���������ʾϣ����������ļ��ߴ�ֵ����������lHintNext�С�
        //      lHint   ��¼λ�ð�ʾ�Բ���������һ��ֻ�з������������׺����ֵ������ǰ����˵�ǲ�͸���ġ�
        //              Ŀǰ�ĺ����Ǽ�¼��ʼλ�á�
        // return:
        //      -1  error
        //      0   file not found
        //      1   succeed
        //      2   ������Χ�����ε�����Ч
        public int GetOperLog(
            string strLibraryCodeList,
            string strFileName,
            long lIndex,
            long lHint,
            string strStyle,
            string strFilter,
            out long lHintNext,
            out string strXml,
            out long lAttachmentLength,
            out string strError)
        {
            strError = "";
            strXml = "";
            lHintNext = -1;
            lAttachmentLength = 0;

            int nRet = 0;

            CacheFileItem cache_item = null;

            if (string.IsNullOrEmpty(this.m_strDirectory) == true)
            {
                strError = "��־Ŀ¼ m_strDirectory ��δ��ʼ��";
                return -1;
            } 
            Debug.Assert(this.m_strDirectory != "", "");

            string strFilePath = this.m_strDirectory + "\\" + strFileName;

            // �Ƿ���Ҫ����ܼ�¼��
            bool bGetCount = StringUtil.IsInList("getcount", strStyle) == true;

            try
            {
                cache_item = this.Cache.Open(strFilePath);
                /*
                stream = File.Open(
                    strFilePath,
                    FileMode.Open,
                    FileAccess.ReadWrite, // Read������޷��� 2007/5/22
                    FileShare.ReadWrite);
                 * */
            }
            catch (FileNotFoundException /*ex*/)
            {
                strError = "��־�ļ� " + strFileName + "û���ҵ�";
                lHintNext = 0;
                return 0;   // file not found
            }
            catch (Exception ex)
            {
                strError = "����־�ļ� '" + strFileName + "' ��������: " + ex.Message;
                return -1;
            }

            try
            {
                long lFileSize = 0;
                // ��λ

                // ����
                // �ڻ���ļ��������ȵĹ����У�����ҪС��������Ⲣ�������ڶ��ļ�����д�Ĳ���
                bool bLocked = false;

                // �����ȡ���ǵ�ǰ����д����ȵ���־�ļ�������Ҫ������������
                if (PathUtil.IsEqual(strFilePath, this.m_strFileName) == true)
                {
                    ////Debug.WriteLine("begin read lock 3");
                    this.m_lock.AcquireReaderLock(m_nLockTimeout);
                    bLocked = true;
                }
                try
                {   // begin of lock try
                    lFileSize = cache_item.Stream.Length;
                }   // end of lock try
                finally
                {
                    if (bLocked == true)
                    {
                        this.m_lock.ReleaseReaderLock();
                        ////Debug.WriteLine("end read lock 3");
                    }
                }


                    // lIndex == -1��ʾϣ������ļ������ĳߴ�
                    if (lIndex == -1)
                    {
                        if (bGetCount == false)
                        {
                            lHintNext = lFileSize;  // cache_item.Stream.Length;
                            return 1;   // �ɹ�
                        }

                        // ��ü�¼����
                        // parameters:
                        // return:
                        //      -1  error
                        //      >=0 ��¼����
                        lHintNext = GetRecordCount(cache_item.Stream,
                            lFileSize,
                            out strError);
                        if (lHintNext == -1)
                            return -1;

                        return 1;   // �ɹ�
                    }

                    // û�а�ʾ��ֻ�ܴ�ͷ��ʼ��
                    if (lHint == -1 || lIndex == 0)
                    {
                        // return:
                        //      -1  error
                        //      0   �ɹ�
                        //      1   �����ļ�ĩβ���߳���
                        nRet = LocationRecord(cache_item.Stream,
                            lFileSize,
                            lIndex,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet == 1)
                            return 2;
                    }
                    else
                    {
                        // ���ݰ�ʾ�ҵ�
                        if (lHint == cache_item.Stream.Length)
                            return 2;

                        if (lHint > cache_item.Stream.Length)
                        {
                            strError = "lHint����ֵ����ȷ";
                            return -1;
                        }
                        if (cache_item.Stream.Position != lHint)
                            cache_item.Stream.Seek(lHint, SeekOrigin.Begin);
                    }

                    /////

                // MemoryStream attachment = null; // new MemoryStream();
                // TODO: �Ƿ�����Ż�Ϊ���ȶ���XML���֣������Ҫ�ٶ���attachment? ����attachment���԰�������ֶ�
                // return:
                //      1   ����
                //      0   �ɹ�
                //      1   �ļ����������ζ�����Ч
                nRet = ReadEnventLog(
                    cache_item.Stream,
                    lFileSize,
                    true,
                    out strXml,
                    out lAttachmentLength,
                    out strError);
                if (nRet == -1)
                    return -1;

                // ���Ƽ�¼�۲췶Χ
                if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                {
#if NO
                    XmlDocument dom = new XmlDocument();
                    try
                    {
                        dom.LoadXml(strXml);
                    }
                    catch (Exception ex)
                    {
                        strError = "strXml����װ��XMLDOMʱ����: " + ex.Message;
                        return -1;
                    }
                    XmlNode node = null;
                    string strLibraryCodes = DomUtil.GetElementText(dom.DocumentElement, "libraryCode", out node);
                    if (node == null)
                        goto END1;  // ����Ҫ����
                    string strSourceLibraryCode = "";
                    string strTargetLibraryCode = "";
                    ParseLibraryCodes(strLibraryCodes,
    out strSourceLibraryCode,
    out strTargetLibraryCode);
                    if (StringUtil.IsInList(strTargetLibraryCode, strLibraryCodeList) == false)
                    {
                        strXml = "";    // ��գ���ǰ�˿���������
                        lAttachmentLength = 0;    // ��ո���
                    }
#endif
                    // ���͹�����־XML��¼
                    // return:
                    //      -1  ����
                    //      0   �������ص�ǰ��־��¼
                    //      1   �����ص�ǰ��־��¼
                    nRet = FilterXml(
                        strLibraryCodeList,
                        strStyle,
                        strFilter,
                        ref strXml,
                        out strError);
                    if (nRet == -1)
                    {
                        nRet = 1;   // ֻ�÷���
                        // return -1;
                    }
                    if (nRet == 0)
                    {
                        strXml = "";    // ��գ���ǰ�˿���������
                    }
                }
                else
                {
                    // ��Ȼ��ȫ���û���ҲҪ���Ƽ�¼�ߴ�
                    nRet = ResizeXml(
                        strStyle,
                        strFilter,
                        ref strXml,
                        out strError);
                    if (nRet == -1)
                    {
                        nRet = 1;   // ֻ�÷���
                        // return -1;
                    }
                }

            // END1:
                lHintNext = cache_item.Stream.Position;

                return 1;
            }
            finally
            {
                // stream.Close();
                this.Cache.Close(cache_item);
            }
        }

        // ����������������
        // ͼ���1,ͼ���2
        static void ParseLibraryCodes(string strText,
            out string strSource,
            out string strTarget)
        {
            strSource = "";
            strTarget = "";

            strText = strText.Trim();

            int nRet = strText.IndexOf(",");
            if (nRet == -1)
            {
                strSource = strText;
                strTarget = strText;
                return;
            }

            strSource = strText.Substring(0, nRet).Trim();
            strTarget = strText.Substring(nRet + 1).Trim();
        }

        // ���ݼ�¼��ţ���λ����¼��ʼλ��
        // parameters:
        //      lMaxFileSize    �ļ����ߴ硣���Ϊ -1����ʾ�����ơ������Ϊ -1����ʾ��Ҫ�������Χ��̽��
        // return:
        //      -1  error
        //      0   �ɹ�
        //      1   �����ļ�ĩβ���߳���
        static int LocationRecord(Stream stream,
            long lMaxFileSize,
            long lIndex,
            out string strError)
        {
            strError = "";

            if (stream.Position != 0)
                stream.Seek(0, SeekOrigin.Begin);

            for (long i = 0; i < lIndex; i++)
            {
                if (lMaxFileSize != -1
    && stream.Position >= lMaxFileSize)
                    return 1; 

                byte[] length = new byte[8];

                int nRet = stream.Read(length, 0, 8);
                if (nRet < 8)
                {
                    strError = "��ʼλ�ò���ȷ";
                    return -1;
                }

                Int64 lLength = BitConverter.ToInt64(length, 0);

                stream.Seek(lLength, SeekOrigin.Current);
            }

            if (lMaxFileSize != -1
&& stream.Position >= lMaxFileSize)
                return 1; 
            if (stream.Position >= stream.Length)
                return 1;

            return 0;
        }

        // 2013/11/21
        // ��ü�¼����
        // parameters:
        //      lMaxFileSize    �ļ����ߴ硣���Ϊ -1����ʾ�����ơ������Ϊ -1����ʾ��Ҫ�������Χ��̽��
        // return:
        //      -1  error
        //      >=0 ��¼����
        static long GetRecordCount(Stream stream,
            long lMaxFileSize,
            out string strError)
        {
            strError = "";

            if (stream.Position != 0)
                stream.Seek(0, SeekOrigin.Begin);

            for (long i = 0; ; i++)
            {
                if (lMaxFileSize != -1
                    && stream.Position >= lMaxFileSize)
                    return i; 
                
                byte[] length = new byte[8];

                int nRet = stream.Read(length, 0, 8);
                if (nRet == 0)
                    return i;

                if (nRet < 8)
                {
                    strError = "��ʼλ�ò���ȷ";
                    return -1;
                }

                Int64 lLength = BitConverter.ToInt64(length, 0);

                stream.Seek(lLength, SeekOrigin.Current);
            }
        }

        // ����־�ļ���ǰλ�ö���һ����־��¼
        // Ҫ��������
        // return:
        //      1   ����
        //      0   �ɹ�
        //      1   �ļ����������ζ�����Ч
        public static int ReadEnventLog(
            Stream stream,
            out string strXmlBody,
            ref Stream attachment,
            out string strError)
        {
            strError = "";
            strXmlBody = "";

            long lStart = stream.Position;	// ������ʼλ��

            byte[] length = new byte[8];

            int nRet = stream.Read(length, 0, 8);
            if (nRet == 0)
                return 1;
            if (nRet < 8)
            {
                strError = "ReadEnventLog()��ƫ���� "+lStart.ToString()+" ��ʼ��ͼ����8��byte������ֻ������ "+nRet.ToString()+" ������ʼλ�ò���ȷ";
                return -1;
            }

            Int64 lRecordLength = BitConverter.ToInt64(length, 0);

            if (lRecordLength == 0)
            {
                strError = "ReadEnventLog()��ƫ���� " + lStart.ToString() + " ��ʼ������8��byte��������ֵΪ0��������־�ļ������˴���";
                return -1;
            }

            Debug.Assert(lRecordLength != 0, "");

            string strMetaData = "";

            // ����xml����
            nRet = ReadEntry(stream,
                true,
                out strMetaData,
                out strXmlBody,
                out strError);
            if (nRet == -1)
                return -1;

            // ����attachment����
            nRet = ReadEntry(
                stream,
                out strMetaData,
                ref attachment,
                out strError);
            if (nRet == -1)
                return -1;

            // �ļ�ָ����Ȼָ��ĩβλ��
            // this.m_stream.Seek(lRecordLength, SeekOrigin.Current);

            // �ļ�ָ���ʱ��Ȼ��ĩβ
            if (stream.Position - lStart != lRecordLength + 8)
            {
                // Debug.Assert(false, "");
                strError = "Record���Ⱦ����鲻��ȷ: stream.Position - lStart ["
                    + (stream.Position - lStart).ToString()
                    + "] ������ lRecordLength + 8 ["
                    + (lRecordLength + 8).ToString()
                    + "]";
                return -1;
            }

            return 0;
        }

        // 2012/9/23
        // ����־�ļ���ǰλ�ö���һ����־��¼
        // ֻ̽�⸽���ĳ��ȣ�������������
        // parameters:
        //      lMaxFileSize    �ļ����ߴ硣���Ϊ -1����ʾ�����ơ������Ϊ -1����ʾ��Ҫ�������Χ��̽��
        //      bRead   �Ƿ�����Ҫ������Ϣ�� == false ��ʾ��������Ϣ��ֻ����֤һ�½ṹ
        // return:
        //      1   ����
        //      0   �ɹ�
        //      1   �ļ����������ζ�����Ч
        public static int ReadEnventLog(
            Stream stream,
            long lMaxFileSize,
            bool bRead,
            out string strXmlBody,
            out long lAttachmentLength,
            out string strError)
        {
            strError = "";
            strXmlBody = "";
            lAttachmentLength = 0;

            long lStart = stream.Position;	// ������ʼλ��

            byte[] length = new byte[8];

            int nRet = stream.Read(length, 0, 8);
            if (nRet == 0)
                return 1;
            if (lMaxFileSize != -1
    && stream.Position >= lMaxFileSize)
                return 1; 
            if (nRet < 8)
            {
                strError = "ReadEnventLog()��ƫ���� " + lStart.ToString() + " ��ʼ��ͼ����8��byte������ֻ������ " + nRet.ToString() + " ������ʼλ�ò���ȷ";
                return -1;
            }

            Int64 lRecordLength = BitConverter.ToInt64(length, 0);

            if (lRecordLength == 0)
            {
                strError = "ReadEnventLog()��ƫ���� " + lStart.ToString() + " ��ʼ������8��byte��������ֵΪ0��������־�ļ������˴���";
                return -1;
            }

            Debug.Assert(lRecordLength != 0, "");

            string strMetaData = "";

            // ����xml����
            nRet = ReadEntry(stream,
                bRead,
                out strMetaData,
                out strXmlBody,
                out strError);
            if (nRet == -1)
                return -1;

            // ����attachment����
            nRet = ReadEntry(
                stream,
                bRead,
                out strMetaData,
                out lAttachmentLength,
                out strError);
            if (nRet == -1)
                return -1;

            // �ļ�ָ����Ȼָ��ĩβλ��
            // this.m_stream.Seek(lRecordLength, SeekOrigin.Current);

            // �ļ�ָ���ʱ��Ȼ��ĩβ
            if (stream.Position - lStart != lRecordLength + 8)
            {
                // Debug.Assert(false, "");
                strError = "Record���Ⱦ����鲻��ȷ: stream.Position - lStart ["
                    + (stream.Position - lStart).ToString()
                    + "] ������ lRecordLength + 8 ["
                    + (lRecordLength + 8).ToString()
                    + "]";
                return -1;
            }

            return 0;
        }

        // 2012/9/23
        // ����־�ļ���ǰλ�ö���һ����־��¼�ĸ�������
        // return:
        //      -1  ����
        //      >=0 ���������ĳߴ�
        public static long ReadEnventLogAttachment(
            Stream stream,
            long lAttachmentFragmentStart,
            int nAttachmentFragmentLength,
            out byte[] attachment_data,
            out string strError)
        {
            strError = "";
            attachment_data = null;
            long lAttachmentLength = 0;

            long lStart = stream.Position;	// ������ʼλ��

            byte[] length = new byte[8];

            int nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "ReadEnventLog()��ƫ���� " + lStart.ToString() + " ��ʼ��ͼ����8��byte������ֻ������ " + nRet.ToString() + " ������ʼλ�ò���ȷ";
                return -1;
            }

            Int64 lRecordLength = BitConverter.ToInt64(length, 0);

            if (lRecordLength == 0)
            {
                strError = "ReadEnventLog()��ƫ���� " + lStart.ToString() + " ��ʼ������8��byte��������ֵΪ0��������־�ļ������˴���";
                return -1;
            }

            Debug.Assert(lRecordLength != 0, "");

            string strMetaData = "";
            string strXmlBody = "";

            // ����xml����
            nRet = ReadEntry(stream,
                true,
                out strMetaData,
                out strXmlBody,
                out strError);
            if (nRet == -1)
                return -1;

            // ����attachment����
            // return:
            //      -1  ����
            //      >=0 ���������ĳߴ�
            lAttachmentLength = ReadEntry(
                stream,
                out strMetaData,
                lAttachmentFragmentStart,
                nAttachmentFragmentLength,
                out attachment_data,
                out strError);
            if (lAttachmentLength == -1)
                return -1;

            // �ļ�ָ����Ȼָ��ĩβλ��
            // this.m_stream.Seek(lRecordLength, SeekOrigin.Current);

            // �ļ�ָ���ʱ��Ȼ��ĩβ
            if (stream.Position - lStart != lRecordLength + 8)
            {
                // Debug.Assert(false, "");
                strError = "Record���Ⱦ����鲻��ȷ: stream.Position - lStart ["
                    + (stream.Position - lStart).ToString()
                    + "] ������ lRecordLength + 8 ["
                    + (lRecordLength + 8).ToString()
                    + "]";
                return -1;
            }

            return lAttachmentLength;
        }

        // ����һ������(string����)
        // parameters:
        public static int ReadEntry(
            Stream stream,
            bool bRead,
            out string strMetaData,
            out string strBody,
            out string strError)
        {
            strMetaData = "";
            strBody = "";
            strError = "";

            long lStart = stream.Position;  // ������ʼλ��

            byte[] length = new byte[8];

            int nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "��ʼλ�ò���ȷ";
                return -1;
            }

            Int64 lEntryLength = BitConverter.ToInt64(length, 0);

            // metadata����
            nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "metadata����λ�ò���8bytes";
                return -1;
            }

            Int64 lMetaDataLength = BitConverter.ToInt64(length, 0);

            if (lMetaDataLength > 100 * 1024)
            {
                strError = "��¼��ʽ����ȷ��metadata���ȳ���100K";
                return -1;
            }

            if (lMetaDataLength > 0)
            {
                if (bRead)
                {
                    byte[] metadatabody = new byte[(int)lMetaDataLength];

                    nRet = stream.Read(metadatabody, 0, (int)lMetaDataLength);
                    if (nRet < (int)lMetaDataLength)
                    {
                        strError = "metadata�����䳤�ȶ���";
                        return -1;
                    }

                    strMetaData = Encoding.UTF8.GetString(metadatabody);
                }
                else
                    stream.Seek(lMetaDataLength, SeekOrigin.Current);
            }




            // strBody����
            nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "strBody����λ�ò���8bytes";
                return -1;
            }

            Int64 lBodyLength = BitConverter.ToInt64(length, 0);

            if (lBodyLength > 1000 * 1024)
            {
                strError = "��¼��ʽ����ȷ��body���ȳ���1000K";
                return -1;
            }

            if (lBodyLength > 0)
            {
                if (bRead)
                {
                    byte[] xmlbody = new byte[(int)lBodyLength];

                    nRet = stream.Read(xmlbody, 0, (int)lBodyLength);
                    if (nRet < (int)lBodyLength)
                    {
                        strError = "body�����䳤�ȶ���";
                        return -1;
                    }

                    strBody = Encoding.UTF8.GetString(xmlbody);
                }
                else
                    stream.Seek(lBodyLength, SeekOrigin.Current);

            }

            // �ļ�ָ���ʱ��Ȼ��ĩβ
            if (stream.Position - lStart != lEntryLength + 8)
            {
                // Debug.Assert(false, "");
                strError = "entry���Ⱦ����鲻��ȷ";
                return -1;
            }

            return 0;
        }

        // ����һ������(Stream����)
        public static int ReadEntry(
            Stream stream,
            out string strMetaData,
            ref Stream streamBody,
            out string strError)
        {
            strError = "";
            strMetaData = "";

            long lStart = stream.Position;  // ������ʼλ��

            byte[] length = new byte[8];

            int nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "��ʼλ�ò���ȷ";
                return -1;
            }

            Int64 lEntryLength = BitConverter.ToInt64(length, 0);

            // metadata����
            nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "metadata����λ�ò���8bytes";
                return -1;
            }

            Int64 lMetaDataLength = BitConverter.ToInt64(length, 0);

            if (lMetaDataLength > 100 * 1024)
            {
                strError = "��¼��ʽ����ȷ��metadata���ȳ���100K";
                return -1;
            }

            if (lMetaDataLength > 0)
            {
                byte[] metadatabody = new byte[(int)lMetaDataLength];

                nRet = stream.Read(metadatabody, 0, (int)lMetaDataLength);
                if (nRet < (int)lMetaDataLength)
                {
                    strError = "metadata�����䳤�ȶ���";
                    return -1;
                }

                strMetaData = Encoding.UTF8.GetString(metadatabody);
            }

            // body����
            nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "strBody����λ�ò���8bytes";
                return -1;
            }

            Int64 lBodyLength = BitConverter.ToInt64(length, 0);

            if (lBodyLength > stream.Length - stream.Position)
            {
                strError = "��¼��ʽ����ȷ��body���ȳ����ļ�ʣ�ಿ�ֳߴ�";
                return -1;
            }

            if (lBodyLength > 0)
            {
                if (streamBody == null)
                {
                    // �Ż�
                    stream.Seek(lBodyLength, SeekOrigin.Current);
                }
                else
                {
                    // ������dump���������
                    int chunk_size = 4096;
                    byte[] chunk = new byte[chunk_size];
                    long writed_length = 0;
                    for (; ; )
                    {
                        int nThisSize = Math.Min(chunk_size, (int)(lBodyLength - writed_length));
                        int nReaded = stream.Read(chunk, 0, nThisSize);
                        if (nReaded < nThisSize)
                        {
                            strError = "���벻��";
                            return -1;
                        }

                        if (streamBody != null)
                            streamBody.Write(chunk, 0, nReaded);

                        writed_length += nReaded;
                        if (writed_length >= lBodyLength)
                            break;
                    }
                }

            }

            // �ļ�ָ���ʱ��Ȼ��ĩβ
            if (stream.Position - lStart != lEntryLength + 8)
            {
                // Debug.Assert(false, "");
                strError = "entry���Ⱦ����鲻��ȷ";
                return -1;
            }

            return 0;
        }

        // 2012/9/23
        // ����һ������(byte []����)
        // parameters:
        // return:
        //      -1  ����
        //      >=0 ���������ĳߴ�
        public static long ReadEntry(
            Stream stream,
            out string strMetaData,
            long lAttachmentFragmentStart,
            int nAttachmentFragmentLength,
            out byte[] attachment_data, 
            out string strError)
        {
            strError = "";
            strMetaData = "";
            attachment_data = null;

            long lStart = stream.Position;  // ������ʼλ��

            byte[] length = new byte[8];

            int nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "��ʼλ�ò���ȷ";
                return -1;
            }

            Int64 lEntryLength = BitConverter.ToInt64(length, 0);

            // metadata����
            nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "metadata����λ�ò���8bytes";
                return -1;
            }

            Int64 lMetaDataLength = BitConverter.ToInt64(length, 0);

            if (lMetaDataLength > 100 * 1024)
            {
                strError = "��¼��ʽ����ȷ��metadata���ȳ���100K";
                return -1;
            }

            if (lMetaDataLength > 0)
            {
                byte[] metadatabody = new byte[(int)lMetaDataLength];

                nRet = stream.Read(metadatabody, 0, (int)lMetaDataLength);
                if (nRet < (int)lMetaDataLength)
                {
                    strError = "metadata�����䳤�ȶ���";
                    return -1;
                }

                strMetaData = Encoding.UTF8.GetString(metadatabody);
            }

            // body����
            nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "strBody����λ�ò���8bytes";
                return -1;
            }

            Int64 lBodyLength = BitConverter.ToInt64(length, 0);

            if (lBodyLength > stream.Length - stream.Position)
            {
                strError = "��¼��ʽ����ȷ��body���ȳ����ļ�ʣ�ಿ�ֳߴ�";
                return -1;
            }

            if (lBodyLength > 0)
            {
                if (nAttachmentFragmentLength > 0)
                {
                    // ���������
                    if (nAttachmentFragmentLength == -1)
                    {
                        long lTemp = (lBodyLength - lAttachmentFragmentStart);
                        // �����Ƿ񳬹�ÿ�ε����Ƴߴ�
                        nAttachmentFragmentLength = (int)Math.Min((long)(100 * 1024), lTemp);
                    }

                    attachment_data = new byte[nAttachmentFragmentLength];
                    stream.Seek(lAttachmentFragmentStart, SeekOrigin.Current);
                    int nReaded = stream.Read(attachment_data, 0, nAttachmentFragmentLength);
                    if (nReaded < nAttachmentFragmentLength)
                    {
                        strError = "���벻��";
                        return -1;
                    }

                    if (lAttachmentFragmentStart + nAttachmentFragmentLength < lBodyLength)
                    {
                        // ȷ���ļ�ָ���ڶ����λ��
                        stream.Seek(lBodyLength - (lAttachmentFragmentStart + nAttachmentFragmentLength), SeekOrigin.Current);
                    }
                }
                else
                    stream.Seek(lBodyLength, SeekOrigin.Current);

            }

            // �ļ�ָ���ʱ��Ȼ��ĩβ
            if (stream.Position - lStart != lEntryLength + 8)
            {
                // Debug.Assert(false, "");
                strError = "entry���Ⱦ����鲻��ȷ";
                return -1;
            }

            return lBodyLength;
        }

        // 2012/9/23
        // ����һ������(ֻ�۲쳤��)
        public static int ReadEntry(
            Stream stream,
            bool bRead,
            out string strMetaData,
            out long lBodyLength,
            out string strError)
        {
            strError = "";
            strMetaData = "";
            lBodyLength = 0;

            long lStart = stream.Position;  // ������ʼλ��

            byte[] length = new byte[8];

            int nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "��ʼλ�ò���ȷ";
                return -1;
            }

            Int64 lEntryLength = BitConverter.ToInt64(length, 0);

#if NO
            if (lEntryLength == 0)
            {
                // Debug.Assert(false, "");
                // �ļ�ָ���ʱ��Ȼ��ĩβ
                if (stream.Position - lStart != lEntryLength + 8)
                {
                    strError = "entry���Ⱦ����鲻��ȷ 1";
                    return -1;
                }

                return 0;
            }
#endif

            // metadata����
            nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "metadata����λ�ò���8bytes";
                return -1;
            }

            Int64 lMetaDataLength = BitConverter.ToInt64(length, 0);

            if (lMetaDataLength > 100 * 1024)
            {
                strError = "��¼��ʽ����ȷ��metadata���ȳ���100K";
                return -1;
            }

            if (lMetaDataLength > 0)
            {
                if (bRead)
                {
                    byte[] metadatabody = new byte[(int)lMetaDataLength];

                    nRet = stream.Read(metadatabody, 0, (int)lMetaDataLength);
                    if (nRet < (int)lMetaDataLength)
                    {
                        strError = "metadata�����䳤�ȶ���";
                        return -1;
                    }

                    strMetaData = Encoding.UTF8.GetString(metadatabody);
                }
                else
                    stream.Seek(lMetaDataLength, SeekOrigin.Current);
            }

            // body����
            nRet = stream.Read(length, 0, 8);
            if (nRet < 8)
            {
                strError = "strBody����λ�ò���8bytes";
                return -1;
            }

            lBodyLength = BitConverter.ToInt64(length, 0);

            if (lBodyLength > stream.Length - stream.Position)
            {
                strError = "��¼��ʽ����ȷ��body���ȳ����ļ�ʣ�ಿ�ֳߴ�";
                return -1;
            }

            if (lBodyLength > 0)
            {
                // ��Ȼ�������ݣ����ļ�ָ��Ҫ��λ
                stream.Seek(lBodyLength, SeekOrigin.Current);
            }

            // �ļ�ָ���ʱ��Ȼ��ĩβ
            if (stream.Position - lStart != lEntryLength + 8)
            {
                // Debug.Assert(false, "");
                strError = "entry���Ⱦ����鲻��ȷ";
                return -1;
            }

            return 0;
        }

        public class FileInfoCompare : IComparer
        {
            public bool Asc = true;

            public FileInfoCompare(bool bAsc)
            {
                this.Asc = bAsc;
            }

            // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
            int IComparer.Compare(Object x, Object y)
            {
                return (this.Asc == false ? -1 : 1) * ((new CaseInsensitiveComparer()).Compare(((FileInfo)x).Name, ((FileInfo)y).Name));
            }
        }

        // return:
        //      -1  ���г���
        //      0   û�д���
        //      1   �д���
        public int VerifyLogFiles(
            bool bRepair,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(this.m_strDirectory) == true)
            {
                strError = "��δָ��������־Ŀ¼";
                return -1;
            }

            // �г�������־�ļ�
            DirectoryInfo di = new DirectoryInfo(this.m_strDirectory);

            FileInfo[] fis = di.GetFiles("????????.log");
            if (fis.Length == 0)
                return 0;

            // ���ڴ�����ǰ
            Array.Sort(fis, new FileInfoCompare(false));

            DateTime now = DateTime.Now;
            string strToday = now.ToString("yyyyMMdd") + ".log";
            bool bFound = false;

            List<string> filenames = new List<string>();

            // ��ÿ�ǰ����������ļ���
            foreach (FileInfo fi in fis)
            {
                if (strToday == fi.Name)
                    bFound = true;
                filenames.Add(fi.FullName);

                if (filenames.Count >= 2)
                    break;
            }

            // ���뵱�����־�ļ���
            // ���Ŀ¼�д���һ�����������ļ��������뵱�����־�ļ���������ǿ�ɿ���
            if (bFound == false)
            {
                string strFileName = Path.Combine(this.m_strDirectory, strToday);
                if (File.Exists(strFileName) == true)
                    filenames.Add(strFileName);
            }

            string strErrorText = "";
            foreach (string strFileName in filenames)
            {
                // return:
                //      -1  ����
                //      0   û�д���
                //      1   �д���
                int nRet = VerifyLogFile(strFileName,
                    bRepair,
                    out strError);
                if (nRet == -1)
                {
                    strError = "��֤������־�ļ� '" + strFileName + "' ʱ�������д���: " + strError;
                    return -1;
                }
                if (nRet == 1)
                    strErrorText += strError + " ";
            }

            if (string.IsNullOrEmpty(strErrorText) == false)
            {
                strError = strErrorText;
                return 1;
            }

            return 0;
        }

#if NO
        // return:
        //      -1  ����
        //      0   û�д���
        //      1   �д���
        int VerifyLogFile(string strSourceFilename,
            bool bRepair,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(strSourceFilename) == true)
            {
                strError = "Դ�ļ�������Ϊ��";
                return -1;
            }

            try
            {
                using (Stream source = File.Open(
                        strSourceFilename,
                        FileMode.Open,
                        FileAccess.ReadWrite, // Read������޷��� 2007/5/22
                        FileShare.ReadWrite))
                {
                    long lStart = 0;
                    for (long i = 0; ; i++)
                    {
                        lStart = source.Position;

                        byte[] length = new byte[8];

                        nRet = source.Read(length, 0, 8);
                        if (nRet == 0)
                            break;
                        if (nRet < 8)
                        {
                            strError = "ʣ��ߴ粻�� 8 bytes��";
                            if (bRepair == true)
                            {
                                source.SetLength(lStart);
                                strError += "�Ѿ����ļ���λ�� "+lStart.ToString()+" �ضϡ�";
                            }
                            return 1;
                        }

                        Int64 lLength = BitConverter.ToInt64(length, 0);

                        if (source.Position + lLength > source.Length)
                        {
                            strError = "ͷ�� 8 bytes �洢������̫�󣬳����ļ���ǰβ����";
                            if (bRepair == true)
                            {
                                source.SetLength(lStart);
                                strError += "�Ѿ����ļ���λ�� " + lStart.ToString() + " �ضϡ�";
                            }
                            return 1;
                        }

                        source.Seek(lLength, SeekOrigin.Current);
                    }
                }
            }
            catch (FileNotFoundException /*ex*/)
            {
                strError = "Դ��־�ļ� " + strSourceFilename + "û���ҵ�";
                return -1;   // file not found
            }
            catch (Exception ex)
            {
                strError = "����Դ��־�ļ� '" + strSourceFilename + "' ʱ��������: " + ex.Message;
                return -1;
            }

            return 0;
        }
#endif

        // return:
        //      -1  ����
        //      0   û�д���
        //      1   �д���
        int VerifyLogFile(string strSourceFilename,
            bool bRepair,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(strSourceFilename) == true)
            {
                strError = "Դ�ļ�������Ϊ��";
                return -1;
            }

            try
            {
                using (Stream source = File.Open(
                        strSourceFilename,
                        FileMode.Open,
                        FileAccess.ReadWrite, // Read������޷��� 2007/5/22
                        FileShare.ReadWrite))
                {
                    long lStart = 0;
                    for (long i = 0; ; i++)
                    {
                        lStart = source.Position;

                        string strXmlBody = "";
                        long lAttachmentLength = 0;

                        // ����־�ļ���ǰλ�ö���һ����־��¼
                        // ֻ̽�⸽���ĳ��ȣ�������������
                        // return:
                        //      1   ����
                        //      0   �ɹ�
                        //      1   �ļ����������ζ�����Ч
                        nRet = ReadEnventLog(
            source,
            -1,
            false,  // ����������
            out strXmlBody,
            out lAttachmentLength,
            out strError);
                        if (nRet == -1)
                        {
                            if (bRepair == true)
                            {
                                source.SetLength(lStart);
                                strError = "�ļ� "+strSourceFilename+" "+strError+" �Ѿ����ļ���λ�� " + lStart.ToString() + " �ضϡ�";
                            }
                            return 1;   // TODO: ���������ļ������˵Ŀ����Ժ�С?
                        }
                        if (nRet == 1)
                            break;
                    }
                }
            }
            catch (FileNotFoundException /*ex*/)
            {
                strError = "Դ��־�ļ� " + strSourceFilename + "û���ҵ�";
                return -1;   // file not found
            }
            catch (Exception ex)
            {
                strError = "����Դ��־�ļ� '" + strSourceFilename + "' ʱ��������: " + ex.Message;
                return -1;
            }

            return 0;
        }

        // ��ȫ��С�ļ��ϲ������ļ�
        // return:
        //      -1  ���г���
        //      0   û�д���
        //      1   �д���
        public int MergeTempLogFiles(
            bool bVerifySmallFiles,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (string.IsNullOrEmpty(this.m_strDirectory) == true)
            {
                strError = "��δָ��������־Ŀ¼";
                return -1;
            }

            // �г�������־�ļ�
            DirectoryInfo di = new DirectoryInfo(this.m_strDirectory);

            FileInfo[] fis = di.GetFiles("*.tlog");
            if (fis.Length == 0)
                return 0;

            List<string> filenames = new List<string>();
            foreach (FileInfo fi in fis)
            {
                filenames.Add(fi.FullName);
            }

            // ����С����ǰ
            filenames.Sort();

            string strErrorText = "";
            foreach (string strFileName in filenames)
            {
                if (bVerifySmallFiles == true)
                {
                    // return:
                    //      -1  ����
                    //      0   û�д���
                    //      1   �д���
                    nRet = VerifyLogFile(strFileName,
                        true,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "��֤������־�ļ� '" + strFileName + "' ʱ�������д���: " + strError;
                        return -1;
                    }
                    if (nRet == 1)
                        strErrorText += strError + " ";
                }

                string strPureFileName = Path.GetFileNameWithoutExtension(strFileName);
                if (strPureFileName.Length < 8)
                    continue;
                string strBigFileName = Path.Combine(Path.GetDirectoryName(strFileName), strPureFileName.Substring(0, 8) + ".log");


                try
                {
                    using (Stream target = File.Open(
    strBigFileName,
    FileMode.OpenOrCreate,
    FileAccess.ReadWrite,
    FileShare.ReadWrite))
                    {
                        target.Seek(0, SeekOrigin.End);

                        using (Stream source = File.Open(
    strFileName,
    FileMode.OpenOrCreate,
    FileAccess.ReadWrite,
    FileShare.ReadWrite))
                        {
                            StreamUtil.DumpStream(source, target);
                        }
                    }

                    File.Delete(strFileName);

                    this.App.WriteErrorLog("�ɹ��ϲ���ʱ��־�ļ� " + Path.GetFileName(strFileName) + "  �� " + Path.GetFileName(strBigFileName));
                }
                catch (Exception ex)
                {
                    strError = "�ϲ���ʱ��־�ļ� " + strFileName + "  �� " + strBigFileName + " �Ĺ����г����쳣��" + ex.Message;

                    // ֪ͨϵͳ����
                    this.App.HangupReason = HangupReason.OperLogError;

                    this.App.WriteErrorLog("ϵͳ����ʱ����ͼ�ϲ���ʱ��־�ļ���������һŬ��ʧ���� ["+strError+"]��������Ϊ����Ŀ¼�ڳ����฻����̿ռ䣬Ȼ����������ϵͳ��");
                    return -1;
                }
            }


            if (string.IsNullOrEmpty(strErrorText) == false)
            {
                strError = strErrorText;
                return 1;
            }

            return 0;
        }


    }

    // API GetOperLogs()��ʹ�õĽṹ
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class OperLogInfo
    {
        [DataMember]
        public long Index = -1; // ��־��¼���
        [DataMember]
        public long HintNext = -1; // ��һ��¼��ʾ

        [DataMember]
        public string Xml = ""; // ��־��¼XML
        [DataMember]
        public long AttachmentLength = 0;   // �����ߴ�
    }

}
