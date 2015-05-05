using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DigitalPlatform.LibraryServer
{
    public class BatchTaskCollection : List<BatchTask>
    {
        // ������
        internal ReaderWriterLock m_lock = new ReaderWriterLock();
        internal static int m_nLockTimeout = 5000;	// 5000=5��


        public new void Clear()
        {
            this.Close();
        }

        public void Close()
        {
            for (int i = 0; i < this.Count; i++)
            {
                BatchTask task = this[i];
                task.Close();
            }

            base.Clear();
        }

        // �������������һ���������
        // ��װ�汾
        // ������ȫ�汾
        // ���̣߳���ȫ
        public BatchTask GetBatchTask(string strName)
        {
            return GetBatchTask(strName, true);
        }

        // �ڲ��汾
        internal BatchTask GetBatchTask(string strName,
            bool bLock)
        {
            if (bLock == true)
                this.m_lock.AcquireReaderLock(m_nLockTimeout);

            try
            {

                for (int i = 0; i < this.Count; i++)
                {
                    BatchTask task = this[i];
                    if (task.Name == strName)
                        return task;
                }

                return null;
            }
            finally
            {
                if (bLock == true)
                    this.m_lock.ReleaseReaderLock();
            }
        }

        // ����һ�������������
        // ���̣߳���ȫ
        public new void Add(BatchTask task)
        {
            this.m_lock.AcquireWriterLock(m_nLockTimeout);

            if (GetBatchTask(task.Name, false) != null)
                throw new Exception("���� '" + task.Name + "' ���ظ�����������");

            try
            {
                base.Add(task);
            }
            finally
            {
                this.m_lock.ReleaseWriterLock();
            }
        }
    }
}
