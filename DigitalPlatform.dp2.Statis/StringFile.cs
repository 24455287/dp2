using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using DigitalPlatform.IO;

namespace DigitalPlatform.dp2.Statis
{
    [Serializable()]
    public class FileLine
    {
        public string Text = "";

        public FileLine(string strText)
        {
            this.Text = strText;
        }
    }
    // �ж���
    public class LineItem : Item
    {
        int m_nLength = 0;
        FileLine m_line = null;	// Line����?

        byte[] m_buffer = null;

        string  m_strLineKey = "";	// ר�Ŵ�m_line��Ա���������������
        /*
        long m_nKeyBytes = 0;
         * */

        public FileLine FileLine
        {
            get
            {
                return m_line;
            }
            set
            {
                m_line = value;

                this.m_strLineKey = m_line.Text;
                byte [] baKey = Encoding.UTF8.GetBytes(this.m_strLineKey);
                int nKeyBytes = baKey.Length;

                // ��ʼ������������
                MemoryStream s = new MemoryStream();
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(s, m_line);

                this.Length = (int)s.Length + 4 + nKeyBytes;	// ������length��ռbytes

                m_buffer = new byte[(int)s.Length];
                s.Seek(0, SeekOrigin.Begin);
                s.Read(m_buffer, 0, m_buffer.Length);
                s.Close();
            }
        }

        public override int Length
        {
            get
            {
                return m_nLength;
            }
            set
            {
                m_nLength = value;
            }
        }

        public override void ReadData(Stream stream)
        {
            if (this.Length == 0)
                throw new Exception("length��δ��ʼ��");

            // ����m_lKeyBytes
            byte[] bytesbuffer = new byte[4];
            stream.Read(bytesbuffer, 0, 4);
            int nKeyBytes = BitConverter.ToInt32(bytesbuffer, 0);

            // ����m_strLineKey
            byte [] keybuffer = new byte[nKeyBytes];
            stream.Read(keybuffer, 0, keybuffer.Length);

            this.m_strLineKey = Encoding.UTF8.GetString(keybuffer);

            // ����Length��bytes������
            byte[] buffer = new byte[this.Length - 4 - keybuffer.Length];
            stream.Read(buffer, 0, buffer.Length);

            // ��ԭ�ڴ����
            MemoryStream s = new MemoryStream(buffer);

            BinaryFormatter formatter = new BinaryFormatter();

            m_line = (FileLine)formatter.Deserialize(s);
            s.Close();
        }


        public override void ReadCompareData(Stream stream)
        {
            if (this.Length == 0)
                throw new Exception("length��δ��ʼ��");

            // ����m_nKeyBytes
            byte[] bytesbuffer = new byte[4];
            stream.Read(bytesbuffer, 0, bytesbuffer.Length);
            int nKeyBytes = BitConverter.ToInt32(bytesbuffer, 0);

            // ����m_strLineKey
            byte[] keybuffer = new byte[nKeyBytes];
            stream.Read(keybuffer, 0, keybuffer.Length);
            this.m_strLineKey = Encoding.UTF8.GetString(keybuffer);

            m_line = null;	// ��ʾline���󲻿���
        }

        public override void WriteData(Stream stream)
        {
            if (m_line == null)
            {
                throw (new Exception("m_line��δ��ʼ��"));
            }

            if (m_buffer == null)
            {
                throw (new Exception("m_buffer��δ��ʼ��"));
            }

            Debug.Assert(this.m_strLineKey == m_line.Text, "");
            byte [] keybytes = Encoding.UTF8.GetBytes(this.m_strLineKey);
            int nKeyBytes = keybytes.Length;

            // ����д��
            byte[] buffer = BitConverter.GetBytes(nKeyBytes);
            stream.Write(buffer, 0, buffer.Length);

            // key����
            stream.Write(keybytes, 0, keybytes.Length);


            // д��Length��bytes������
            stream.Write(m_buffer, 0, this.Length - 4 - nKeyBytes);
        }

        // ʵ��IComparable�ӿڵ�CompareTo()����,
        // ����ID�Ƚ���������Ĵ�С���Ա�����
        // ���Ҷ��뷽ʽ�Ƚ�
        // obj: An object to compare with this instance
        // ����ֵ A 32-bit signed integer that indicates the relative order of the comparands. The return value has these meanings:
        // Less than zero: This instance is less than obj.
        // Zero: This instance is equal to obj.
        // Greater than zero: This instance is greater than obj.
        // �쳣: ArgumentException,obj is not the same type as this instance.
        public override int CompareTo(object obj)
        {
            LineItem item = (LineItem)obj;

            return String.Compare(this.m_strLineKey, item.m_strLineKey);
        }
    }

    public class StringFile : ItemFileBase
    {
        public StringFile()
		{

		}


		public override Item NewItem()
		{
			return new LineItem();
		}

        // ��������ķ��ظ�������
        public long GetNoDupCount()
        {
            long lResult = 0;

            this.Sort();

            long lCount = this.Count;
            string strPrevText = "";
            for (long i = 0; i < lCount; i++)
            {
                LineItem line_item = (LineItem)this[i];
                string strCurText = line_item.FileLine.Text;
                if (strPrevText != strCurText)
                {
                    lResult++;
                    strPrevText = strCurText;
                }
            }

            return lResult;
        }
    }
}
