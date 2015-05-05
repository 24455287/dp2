using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DigitalPlatform.rms
{
    /// <summary>
    /// �����õ���
    /// </summary>
    public class TestReaderWriterLock
    {
        ReaderWriterLock m_lock = new ReaderWriterLock();
        int nLockCount = 0;

        public void AcquireReaderLock(int timeout)
        {
            if (this.nLockCount != 0)
                throw new Exception("�ظ������ˣ�");
            m_lock.AcquireReaderLock(timeout);
            this.nLockCount++;
        }
        public void AcquireWriterLock(int timeout)
        {
            if (this.nLockCount != 0)
                throw new Exception("�ظ������ˣ�");
            m_lock.AcquireWriterLock(timeout);
            this.nLockCount++;
        }

        public void ReleaseReaderLock()
        {
            m_lock.ReleaseReaderLock();
            this.nLockCount--;
        }

        public void ReleaseWriterLock()
        {
            m_lock.ReleaseWriterLock();
            this.nLockCount--;
        }
    }
}
