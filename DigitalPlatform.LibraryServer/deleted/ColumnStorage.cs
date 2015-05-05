using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Threading;

using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;	// DateTimeUtil
using DigitalPlatform.Text;


namespace DigitalPlatform.LibraryServer
{

#if NOOOOOOOOOOOOO
    // һ��������
    [Serializable()]
    public class Line
    {
        bool m_bInfoInitilized = false;
        // [NonSerialized]
        // public MainPage Container = null;

        // public string m_strRecID = "";
        public string m_strRecPath = "";

        // public string m_strParentID = "";

        // public string m_strArticleState = "";	// ����״̬��������...��
        // ��xml��¼��<state>Ԫ�ػ��

        // public string m_strArticleTitle = "";	// ���ӱ���
        // ��xml��¼��<title>Ԫ�ػ��


        // public string m_strAuthor = "";	// ����
        // ��xml��¼��<author>Ԫ�ػ��

        public DateTime m_timeCreate;	// �������Ӵ���ʱ��

        // public string m_strLastUpdate = "";	// ���и����У�������ʱ��
        public DateTime m_timeLastUpdate;
        // ��xml��¼�У�<tree>Ԫ���¼�����<rec>Ԫ���е��������Լ������

        // public string m_strSummary = "";

        public bool Initialized
        {
            get
            {
                return this.m_bInfoInitilized;
            }
        }

        // �ӷ������˵õ�XML���ݣ���ʼ�����ɱ���
        // parameters:
        //		page	���!=null�����������ж�
        // return:
        //		-1	����
        //		0	��������
        //		1	���û��ж�
        public int InitialInfo(
            System.Web.UI.Page page,
            RmsChannel channel,
            out string strError)
        {
            strError = "";

            Line line = this;

            if (this.m_bInfoInitilized == true)
                return 0;

            if (String.IsNullOrEmpty(this.m_strRecPath) == true)
            {
                strError = "m_strRecPath��δ��ʼ��";
                return -1;
            }

            string strStyle = "content,data";

            string strContent;
            string strMetaData;
            byte[] baTimeStamp;
            string strOutputPath;

            Debug.Assert(channel != null, "Channels.GetChannel �쳣");

            if (page != null
                && page.Response.IsClientConnected == false)	// �����ж�
                return 1;


            long nRet = channel.GetRes(this.m_strRecPath,
                strStyle,
                out strContent,
                out strMetaData,
                out baTimeStamp,
                out strOutputPath,
                out strError);
            if (nRet == -1)
            {
                strError = "��ȡ��¼ '" + this.m_strRecPath + "' ʱ����: " + strError;
                return -1;
            }

            if (page != null
                && page.Response.IsClientConnected == false)	// �����ж�
                return 1;

            // ��������
            nRet = line.ProcessXml(
                page,
                strContent,
                out strError);
            if (nRet == -1)
            {
                return -1;
            }

            this.m_bInfoInitilized = true;

            return 0;
        }

        // ����xml�е�����
        // parameters:
        //		page	���!=null�����������ж�
        // return:
        //		-1	����
        //		0	��������
        //		1	���û��ж�
        public int ProcessXml(
            System.Web.UI.Page page,
            string strXml,
            out string strError)
        {
            strError = "";

            if (page != null
                && page.Response.IsClientConnected == false)	// �����ж�
                return 1;


            XmlDocument dom = new XmlDocument();

            try
            {
                dom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            // this.m_strParentID = DomUtil.GetElementText(dom.DocumentElement, "parent");

            // ����״̬��������...��
            // this.m_strArticleState = DomUtil.GetElementText(dom.DocumentElement, "state");

            // ���ӱ���
            // this.m_strArticleTitle = DomUtil.GetElementText(dom.DocumentElement, "title");

            // ����
            // this.m_strAuthor = DomUtil.GetElementText(dom.DocumentElement, "creator");

            // ժҪ
            // this.m_strSummary = DomUtil.GetElementText(dom.DocumentElement, "description"); // ??

            XmlNode node = dom.DocumentElement.SelectSingleNode("operations/operation[@name='create']");
            if (node != null)
            {
                string strCreateTime = DomUtil.GetAttr(node, "time");
                if (String.IsNullOrEmpty(strCreateTime) == false)
                {
                    try
                    {
                        this.m_timeCreate = DateTimeUtil.FromRfc1123DateTimeString(strCreateTime);
                    }
                    catch
                    {
                    }
                }
            }

            node = dom.DocumentElement.SelectSingleNode("operations/operation[@name='lastContentModified']");
            if (node != null)
            {
                string strLastModifiedTime = DomUtil.GetAttr(node, "time");

                if (string.IsNullOrEmpty(strLastModifiedTime) == false)
                {
                    try
                    {
                        this.m_timeLastUpdate = DateTimeUtil.FromRfc1123DateTimeString(strLastModifiedTime);
                    }
                    catch
                    {
                    }
                }
            }
            else
                this.m_timeLastUpdate = this.m_timeCreate;


            return 0;
        }
    }


    public class TopArticleItem : Item
    {
        int m_nLength = 0;

        byte[] m_buffer = null;

        long m_ticks = 0;	// ר�Ŵ�m_line��Ա���������������
        // internal long m_id = 0;
        internal string m_strRecPath = "";

        long m_bColumnTop = 0;


        Line m_line = null;	// Line����?
        public Line Line
        {
            get
            {
                return m_line;
            }
            set
            {
                m_line = value;

                // ��ʼ������������
                MemoryStream s = new MemoryStream();
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(s, m_line);

                this.Length = (int)s.Length + 8 * 2 + (4 + Encoding.UTF8.GetByteCount(m_line.m_strRecPath));

                m_buffer = new byte[(int)s.Length];
                s.Seek(0, SeekOrigin.Begin);
                s.Read(m_buffer, 0, m_buffer.Length);
                s.Close();

                m_ticks = m_line.m_timeLastUpdate.Ticks;

                /*
                if (StringUtil.IsInList("columntop", m_line.m_strArticleState) == true)
                    m_bColumnTop = 1;
                else
                    m_bColumnTop = 0;
                 * */
                m_bColumnTop = 0;

                if (this.m_ticks == 0)
                    throw (new Exception("ticks����Ϊ0"));

                this.m_strRecPath = m_line.m_strRecPath;
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

            // ����ticks
            byte[] ticksbuffer = new byte[8];
            stream.Read(ticksbuffer, 0, 8);
            this.m_ticks = BitConverter.ToInt64(ticksbuffer, 0);

            if (this.m_ticks == 0)
                throw (new Exception("ticks����Ϊ0"));

            // 
            stream.Read(ticksbuffer, 0, 8);
            this.m_bColumnTop = BitConverter.ToInt64(ticksbuffer, 0);


            // length of path
            byte[] lengthbuffer = new byte[4];
            stream.Read(lengthbuffer, 0, 4);
            int nLength = BitConverter.ToInt32(lengthbuffer, 0);

            Debug.Assert(nLength >= 0 && nLength < 100, "");
            byte[] textbuffer = new byte[nLength];
            stream.Read(textbuffer, 0, nLength);

            this.m_strRecPath = System.Text.Encoding.UTF8.GetString(textbuffer);


            // ����Length��bytes������
            byte[] buffer = new byte[this.Length - 8 * 2 - (nLength + 4)];
            stream.Read(buffer, 0, buffer.Length);

            // ��ԭ�ڴ����
            MemoryStream s = new MemoryStream(buffer);

            BinaryFormatter formatter = new BinaryFormatter();

            m_line = (Line)formatter.Deserialize(s);
            s.Close();
        }


        public override void ReadCompareData(Stream stream)
        {
            if (this.Length == 0)
                throw new Exception("length��δ��ʼ��");

            // ����ticks
            byte[] ticksbuffer = new byte[8];
            stream.Read(ticksbuffer, 0, 8);
            this.m_ticks = BitConverter.ToInt64(ticksbuffer, 0);

            if (this.m_ticks == 0)
                throw (new Exception("ticks����Ϊ0"));

            // 
            stream.Read(ticksbuffer, 0, 8);
            this.m_bColumnTop = BitConverter.ToInt64(ticksbuffer, 0);


            // length of path
            byte[] lengthbuffer = new byte[4];
            stream.Read(lengthbuffer, 0, 4);
            int nLength = BitConverter.ToInt32(lengthbuffer, 0);

            Debug.Assert(nLength >= 0 && nLength < 100, "");
            byte[] textbuffer = new byte[nLength];
            stream.Read(textbuffer, 0, nLength);

            this.m_strRecPath = System.Text.Encoding.UTF8.GetString(textbuffer);

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

            if (this.m_ticks == 0)
                throw (new Exception("ticks����Ϊ0"));


            // ����д��ʱ��ticks
            byte[] buffer = BitConverter.GetBytes(this.m_ticks);
            stream.Write(buffer, 0, buffer.Length);

            buffer = BitConverter.GetBytes(this.m_bColumnTop);
            stream.Write(buffer, 0, buffer.Length);

            /*
            buffer = BitConverter.GetBytes(this.m_id);
            stream.Write(buffer, 0, buffer.Length);
             * */
            byte[] bufferLength = new byte[4];
            byte[] bufferText = Encoding.UTF8.GetBytes(this.m_strRecPath);
            bufferLength = System.BitConverter.GetBytes((Int32)bufferText.Length);
            Debug.Assert(bufferLength.Length == 4, "");
            stream.Write(bufferLength, 0, 4);
            stream.Write(bufferText, 0, bufferText.Length);

            // д��Length��bytes������
            stream.Write(m_buffer, 0, this.Length - 8 * 2 - (bufferText.Length + 4));
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
            TopArticleItem item = (TopArticleItem)obj;

            if (this.m_ticks == 0)
                throw (new Exception("this.ticks����Ϊ0"));

            if (item.m_ticks == 0)
                throw (new Exception("item.ticks����Ϊ0"));

            if (this.m_bColumnTop != item.m_bColumnTop)
            {
                if (this.m_bColumnTop != 0)
                    return (-1) * 1;
                return (-1) * (-1);
            }


            long delta = this.m_ticks - item.m_ticks;

            if (delta != 0)
            {
                if (delta < 0)
                    return (-1) * (-1);
                else
                    return (-1) * 1;
            }

            /*
            delta = this.m_id - item.m_id;
            if (delta != 0)
            {
                if (delta < 0)
                    return (-1) * (-1);
                else
                    return (-1) * 1;
            }
             * */

            return 0;
        }
    }



    /// <summary>
    /// һ����Ŀ�Ĵ�������洢�ṹ
    /// </summary>
    public class ColumnStorage : ItemFileBase
    {

        public ColumnStorage()
        {
            this.ReadOnly = true;
        }

        public override Item NewItem()
        {
            return new TopArticleItem();
        }

        // �Ƿ񱻳ɹ���?
        public bool Opened
        {
            get
            {
                if (this.m_streamSmall != null
                    && this.m_streamBig != null)
                    return true;
                return false;
            }
        }

        // ���ٻ������ļ�¼·��
        public string GetItemRecPath(Int64 nIndex)
        {
            // �Ӷ���
            this.m_lock.AcquireReaderLock(m_nLockTimeout);
            try
            {

                TopArticleItem item = (TopArticleItem)this.GetCompareItem(nIndex, false);

                if (item == null)
                    return "";	// error

                return item.m_strRecPath;
            }
            finally
            {
                this.m_lock.ReleaseReaderLock();
            }
        }

    }

#endif

}
