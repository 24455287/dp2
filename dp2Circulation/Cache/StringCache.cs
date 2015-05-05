#define NEWLOCK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace dp2Circulation
{
    /// <summary>
    /// �ַ������ٻ���
    /// </summary>
    public class StringCache
    {
        /// <summary>
        /// ���������ɵ��������
        /// </summary>
        public int MaxItems = 1000;
        Hashtable items = new Hashtable();
#if NEWLOCK
        internal ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim();
#else
        internal ReaderWriterLock m_lock = new ReaderWriterLock();
        internal static int m_nLockTimeout = 5000;	// 5000=5��
#endif

        /// <summary>
        /// ����һ������
        /// </summary>
        /// <param name="strEntry">������</param>
        /// <returns>ֵ</returns>
        public StringCacheItem SearchItem(string strEntry)
        {
#if NEWLOCK
            this.m_lock.EnterReadLock();
#else
            this.m_lock.AcquireReaderLock(m_nLockTimeout);
#endif

            try
            {
                // return null; // ����
                return (StringCacheItem)items[strEntry];
            }
            finally
            {
#if NEWLOCK
                this.m_lock.ExitReadLock();
#else
                this.m_lock.ReleaseReaderLock();
#endif
            }
        }

        // �õ��ж�����������ڣ�����ʱ����һ��
        /// <summary>
        /// �õ��ж�����������ڣ�����ʱ����һ��
        /// </summary>
        /// <param name="strEntry">������</param>
        /// <returns>�Ѿ����ڵĻ����´����� StringCacheItem ����</returns>
        public StringCacheItem EnsureItem(string strEntry)
        {
#if NEWLOCK
            this.m_lock.EnterWriteLock();
#else
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
#endif

            try
            {
                if (items.Count > MaxItems)
                    this.items.Clear();

                // ���line�����Ƿ����
                StringCacheItem item = (StringCacheItem)items[strEntry];

                if (item == null)
                {
                    item = new StringCacheItem();
                    item.Key = strEntry;

                    items.Add(strEntry, item);
                }

                Debug.Assert(item != null, "line������Ӧ��!=null");

                return item;
            }
            finally
            {
#if NEWLOCK
                this.m_lock.ExitWriteLock();
#else
                this.m_lock.ReleaseWriterLock();
#endif
            }
        }

        /// <summary>
        /// ������������
        /// </summary>
        public void RemoveAll()
        {
#if NEWLOCK
            this.m_lock.EnterWriteLock();
#else
            this.m_lock.AcquireWriterLock(m_nLockTimeout);
#endif

            try
            {
                items.Clear();
            }
            finally
            {
#if NEWLOCK
                this.m_lock.ExitWriteLock();
#else
                this.m_lock.ReleaseWriterLock();
#endif
            }
        }
    }

    /// <summary>
    /// �ַ����������
    /// </summary>
    public class StringCacheItem
    {
        /// <summary>
        /// ��
        /// </summary>
        public string Key = "";
        /// <summary>
        /// ֵ
        /// </summary>
        public string Content = "";
    }
}
