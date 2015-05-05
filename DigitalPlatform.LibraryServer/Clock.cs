using System;
using System.Collections.Generic;
using System.Text;

using System.Threading;

using DigitalPlatform.IO;

namespace DigitalPlatform.LibraryServer
{
    public class Clock
    {
        public ReaderWriterLock m_lock = new ReaderWriterLock();
        public static int m_nLockTimeout = 5000;	// 5000=5��

        TimeSpan clockdelta = new TimeSpan(0);  // �߼��ϵ�һ������ͨʱ�ӡ��͵�ǰ����������ʱ�ӵĲ��


        // �ͱ���ʱ�ӵ�ƫ��Ticks��
        public long Delta
        {
            get
            {
                return clockdelta.Ticks;
            }
            set
            {
                clockdelta = new TimeSpan(value);
            }
        }

        // ������ͨʱ��
        // �ⲿʹ��
        // paramters:
        //      strTime RFC1123��ʽ
        public int SetClock(string strTime,
            out string strError)
        {
            strError = "";

            if (String.IsNullOrEmpty(strTime) == true)
            {
                this.m_lock.AcquireWriterLock(m_nLockTimeout);
                try
                {
                    // ��������
                    this.clockdelta = new TimeSpan(0);
                }
                finally
                {
                    this.m_lock.ReleaseWriterLock();
                }
                return 0;
            }

            DateTime time;

            try
            {
                time = DateTimeUtil.FromRfc1123DateTimeString(strTime);
            }
            catch
            {
                strError = "����ʱ���ַ��� '" + strTime + "' ��ʽ����Ӧ����RFC1123��ʽҪ��";
                return -1;
            }

            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                this.clockdelta = time - DateTime.UtcNow;
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

            return 0;
        }

        // �����ͨʱ�� RFC1123��ʽ ��ʱ����Ϣ
        // �ⲿʹ��
        public string GetClock()
        {
            DateTime time;
            this.m_lock.AcquireReaderLock(m_nLockTimeout);
            try
            {
                time = DateTime.Now + this.clockdelta;
            }
            finally
            {
                this.m_lock.ReleaseReaderLock();
            }

            return DateTimeUtil.Rfc1123DateTimeStringEx(time);
        }

        public void Reset()
        {
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
            try
            {
                this.clockdelta = new TimeSpan(0); 
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }

        }

        public DateTime Now
        {
            get
            {
                this.m_lock.AcquireReaderLock(m_nLockTimeout);
                try
                {
                    return DateTime.Now + this.clockdelta;
                }
                finally
                {
                    this.m_lock.ReleaseReaderLock();
                }
            }
        }

        public DateTime UtcNow
        {
            get
            {
                this.m_lock.AcquireReaderLock(m_nLockTimeout);
                try
                {
                    return DateTime.UtcNow + this.clockdelta;
                }
                finally
                {
                    this.m_lock.ReleaseReaderLock();
                }
            }
        }

    }
}
