using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using DigitalPlatform.IO;

namespace DigitalPlatform.LibraryServer
{
    /*
    [Serializable()]
    public class DupLine
    {
        public string Path = "";
        public int Weight = 0;

        public DupLine(string strPath,
            int nWeight)
        {
            this.Path = strPath;
            this.Weight = nWeight;
        }
    }*/

    // �ж���
    public class DupLineItem : Item
    {
        int m_nLength = 0;
        byte[] m_buffer = null;

        public string Path = "";
        public int Weight = 0;
        public int Threshold = 0;

        /*
        public DupLineItem(string strPath,
            int nWeight)
        {
            this.Weight = nWeight;
            this.Path = strPath;

        }
         * */

        public override void BuildBuffer()
        {
            //
            byte[] baWeight = BitConverter.GetBytes((Int32)this.Weight);
            Debug.Assert(baWeight.Length == 4, "");

            //
            byte[] baThreshold = BitConverter.GetBytes((Int32)this.Threshold);
            Debug.Assert(baThreshold.Length == 4, "");

            //
            byte[] baPath = Encoding.UTF8.GetBytes(this.Path);
            int nPathBytes = baPath.Length;

            // 
            byte[] baPathLength = BitConverter.GetBytes((Int32)nPathBytes);
            Debug.Assert(baPathLength.Length == 4, "");

            this.Length = 4/*weight*/ + 4/*threshold*/ +  4/*length of path content */ + nPathBytes;


            m_buffer = new byte[this.Length];
            Array.Copy(baWeight, m_buffer, baWeight.Length);
            Array.Copy(baThreshold, 0, m_buffer, 4, 4);
            Array.Copy(baPathLength, 0, m_buffer, 4 + 4, 4);
            Array.Copy(baPath, 0, m_buffer, 4 + 4 + 4, nPathBytes);
        }

        /*
        public DupLineItem FileLine
        {
            get
            {
                return m_line;
            }
            set
            {
                m_line = value;

                this.m_strLineKey = m_line.Text;
                byte[] baKey = Encoding.UTF8.GetBytes(this.m_strLineKey);
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
         * */

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

            // ����Weight
            byte[] weightbuffer = new byte[4];
            stream.Read(weightbuffer, 0, 4);
            this.Weight = BitConverter.ToInt32(weightbuffer, 0);

            // ����Threshold
            byte[] shresholdbuffer = new byte[4];
            stream.Read(shresholdbuffer, 0, 4);
            this.Threshold = BitConverter.ToInt32(shresholdbuffer, 0);

            // ����path length
            byte[] lengthbuffer = new byte[4];
            stream.Read(lengthbuffer, 0, 4);
            int nPathLength = BitConverter.ToInt32(lengthbuffer, 0);

            // ����path content
            if (nPathLength > 0)
            {
                byte[] pathbuffer = new byte[nPathLength];
                stream.Read(pathbuffer, 0, nPathLength);

                this.Path = Encoding.UTF8.GetString(pathbuffer);
            }
            else
                this.Path = "";
        }


        public override void ReadCompareData(Stream stream)
        {
            if (this.Length == 0)
                throw new Exception("length��δ��ʼ��");

            // ����Weight
            byte[] weightbuffer = new byte[4];
            stream.Read(weightbuffer, 0, 4);
            this.Weight = BitConverter.ToInt32(weightbuffer, 0);

            // ����Threshold
            byte[] shresholdbuffer = new byte[4];
            stream.Read(shresholdbuffer, 0, 4);
            this.Threshold = BitConverter.ToInt32(shresholdbuffer, 0);

            // ����path length
            byte[] lengthbuffer = new byte[4];
            stream.Read(lengthbuffer, 0, 4);
            int nPathLength = BitConverter.ToInt32(lengthbuffer, 0);

            // ����path content
            if (nPathLength > 0)
            {
                byte[] pathbuffer = new byte[nPathLength];
                stream.Read(pathbuffer, 0, nPathLength);

                this.Path = Encoding.UTF8.GetString(pathbuffer);
            }
            else
                this.Path = "";
        }

        public override void WriteData(Stream stream)
        {
            if (m_buffer == null)
                BuildBuffer();

            if (m_buffer == null)
            {
                throw (new Exception("m_buffer��δ��ʼ��"));
            }

            // д��Length��bytes������
            stream.Write(m_buffer, 0, this.Length);
        }

        // ʵ��IComparable�ӿڵ�CompareTo()����,
        // obj: An object to compare with this instance
        // ����ֵ A 32-bit signed integer that indicates the relative order of the comparands. The return value has these meanings:
        // Less than zero: This instance is less than obj.
        // Zero: This instance is equal to obj.
        // Greater than zero: This instance is greater than obj.
        // �쳣: ArgumentException,obj is not the same type as this instance.
        public override int CompareTo(object obj)
        {
            DupLineItem item = (DupLineItem)obj;

            // С��ǰ
            return String.Compare(this.Path, item.Path);
        }

        // ����Ȩֵ����
        public int CompareWeightTo(object obj)
        {
            DupLineItem item = (DupLineItem)obj;

            int delta = this.Weight - item.Weight;
            if (delta != 0)
                return -1*delta;    // ����ǰ

            // ��Ȩֵ��ͬ���ٰ���·������
            // С��ǰ
            return String.Compare(this.Path, item.Path);
        }

        // ���ղ��������ν������Ȩֵ����ֵ�Ĳ��
        public int CompareOverThresholdTo(object obj)
        {
            DupLineItem item = (DupLineItem)obj;

            int over1 = this.Weight - this.Threshold;
            int over2 = item.Weight - item.Threshold;

            int delta = over1 - over2;
            if (delta != 0)
                return -1*delta;    // ����ǰ

            // ������ͬ���ٰ���·������
            // С��ǰ
            return String.Compare(this.Path, item.Path);
        }
    }

    /// <summary>
    /// ���ڲ��صĽ�����ļ�����
    /// ��Ҫ�����ǣ�ÿ���������һ��Ȩֵ�����ֶ�
    /// </summary>
    public class DupResultSet : ItemFileBase
    {
        // ������
        public DupResultSetSortStyle SortStyle = DupResultSetSortStyle.Path;


        public DupResultSet()
        {

        }


        public override Item NewItem()
        {
            return new DupLineItem();
        }

        public string Dump()
        {
            return "";
        }

        // ʹ�ÿ��԰��ն��ַ������
        public override int Compare(long lPtr1, long lPtr2)
        {
            if (lPtr1 < 0 && lPtr2 < 0)
                return 0;
            else if (lPtr1 >= 0 && lPtr2 < 0)
                return 1;
            else if (lPtr1 < 0 && lPtr2 >= 0)
                return -1;

            DupLineItem item1 = (DupLineItem)GetCompareItemByOffset(lPtr1);
            DupLineItem item2 = (DupLineItem)GetCompareItemByOffset(lPtr2);

            if (this.SortStyle == DupResultSetSortStyle.Path)
                return item1.CompareTo(item2);
            else if (this.SortStyle == DupResultSetSortStyle.Weight)
                return item1.CompareWeightTo(item2);
            else if (this.SortStyle == DupResultSetSortStyle.OverThreshold)
                return item1.CompareOverThresholdTo(item2);
            else
            {
                Debug.Assert(false, "invalid sort style");
                return 0;
            }

        }


        // ����: �ϲ���������
        // parameters:
        //		strStyle	������ OR , AND , SUB
        //		sourceLeft	Դ��߽����
        //		sourceRight	Դ�ұ߽����
        //		targetLeft	Ŀ����߽����
        //		targetMiddle	Ŀ���м�����
        //		targetRight	Ŀ���ұ߽����
        //		bOutputDebugInfo	�Ƿ����������Ϣ
        //		strDebugInfo	������Ϣ
        // return
        //		-1	����
        //		0	�ɹ�
        public static int Merge(string strStyle,
            DupResultSet sourceLeft,
            DupResultSet sourceRight,
            DupResultSet targetLeft,
            DupResultSet targetMiddle,
            DupResultSet targetRight,
            bool bOutputDebugInfo,
            out string strDebugInfo,
            out string strError)
        {
            strDebugInfo = "";
            strError = "";

            if (sourceLeft.m_streamSmall == null)
            {
                throw new Exception("sourceLeft���������δ������");
            }

            if (sourceRight.m_streamSmall == null)
            {
                throw new Exception("sourceRight���������δ������");
            }


            if (bOutputDebugInfo == true)
            {
                strDebugInfo += "strStyleֵ:" + strStyle + "<br/>";
                strDebugInfo += "sourceLeft�����:" + sourceLeft.Dump() + "<br/>";
                strDebugInfo += "sourceRight�����:" + sourceRight.Dump() + "<br/>";
            }

            if (String.Compare(strStyle, "OR", true) == 0)
            {
                if (targetLeft != null || targetRight != null)
                {
                    Exception ex = new Exception("DpResultSetManager::Merge()���ǲ��ǲ����ô���?��strStyle����ֵΪ\"OR\"ʱ��targetLeft������targetRight��Ч��ֵӦΪnull");
                    throw (ex);
                }
            }

            DupLineItem dpRecordLeft;
            DupLineItem dpRecordRight;
            int i = 0;
            int j = 0;
            int ret;
            while (true)
            {
                dpRecordLeft = null;
                dpRecordRight = null;
                if (i >= sourceLeft.Count)
                {
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "i���ڵ���sourceLeft�ĸ�������i��Ϊ-1<br/>";
                    }
                    i = -1;
                }
                else if (i != -1)
                {
                    try
                    {
                        dpRecordLeft = (DupLineItem)sourceLeft[i];
                        if (bOutputDebugInfo == true)
                        {
                            strDebugInfo += "ȡ��sourceLeft�����е�" + Convert.ToString(i) + "��Ԫ�أ�PathΪ" + dpRecordLeft.Path + "<br/>";
                        }
                    }
                    catch (Exception e)
                    {
                        Exception ex = new Exception("ȡSourceLeft���ϳ���i=" + Convert.ToString(i) + "----Count=" + Convert.ToString(sourceLeft.Count) + ", internel error :" + e.Message + "<br/>");
                        throw (ex);
                    }
                }
                if (j >= sourceRight.Count)
                {
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "j���ڵ���sourceRight�ĸ�������j��Ϊ-1<br/>";
                    }
                    j = -1;
                }
                else if (j != -1)
                {
                    try
                    {
                        dpRecordRight = (DupLineItem)sourceRight[j];
                        if (bOutputDebugInfo == true)
                        {
                            strDebugInfo += "ȡ��sourceRight�����е�" + Convert.ToString(j) + "��Ԫ�أ�PathΪ" + dpRecordRight.Path + "<br/>";
                        }
                    }
                    catch
                    {
                        Exception ex = new Exception("j=" + Convert.ToString(j) + "----Count=" + Convert.ToString(sourceLeft.Count) + sourceRight.GetHashCode() + "<br/>");
                        throw (ex);
                    }
                }
                if (i == -1 && j == -1)
                {
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "i,j������-1����<br/>";
                    }
                    break;
                }

                if (dpRecordLeft == null)
                {
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "dpRecordLeftΪnull����ret����1<br/>";
                    }
                    ret = 1;
                }
                else if (dpRecordRight == null)
                {
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "dpRecordRightΪnull����ret����-1<br/>";
                    }
                    ret = -1;
                }
                else
                {
                    ret = dpRecordLeft.CompareTo(dpRecordRight);  //MyCompareTo(oldOneKey); //��CompareTO
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "dpRecordLeft��dpRecordRight����Ϊnull���Ƚ�������¼�õ�ret����" + Convert.ToString(ret) + "<br/>";
                    }
                }


                if (String.Compare(strStyle, "OR", true) == 0
                    && targetMiddle != null)
                {
                    if (ret == 0)
                    {
                        // ��������ȡһ���Ϳ��ԣ�����Ҫ����Ȩֵ 2007/7/2
                        dpRecordLeft.Weight += dpRecordRight.Weight;

                        targetMiddle.Add(dpRecordLeft);
                        i++;
                        j++;
                    }
                    else if (ret < 0)
                    {
                        targetMiddle.Add(dpRecordLeft);
                        i++;
                    }
                    else if (ret > 0)
                    {
                        targetMiddle.Add(dpRecordRight);
                        j++;
                    }
                    continue;
                }

                if (ret == 0 && targetMiddle != null)
                {
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "ret����0,�ӵ�targetMiddle����<br/>";
                    }

                    // ��������ȡһ���Ϳ��ԣ�����Ҫ����Ȩֵ 2007/7/2
                    dpRecordLeft.Weight += dpRecordRight.Weight;

                    targetMiddle.Add(dpRecordLeft);
                    i++;
                    j++;
                }

                if (ret < 0)
                {
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "retС��0,�ӵ�targetLeft����<br/>";
                    }

                    if (targetLeft != null && dpRecordLeft != null)
                        targetLeft.Add(dpRecordLeft);
                    i++;
                }

                if (ret > 0)
                {
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "ret����0,�ӵ�targetRight����<br/>";
                    }

                    if (targetRight != null && dpRecordRight != null)
                        targetRight.Add(dpRecordRight);

                    j++;
                }
            }
            return 0;
        }
    }

    public enum DupResultSetSortStyle
    {
        Path = 0,   // ����·������
        Weight = 1, // ����Ȩֵ�������Ȩֵ��ͬ����·������
        OverThreshold = 2,  // ����Ȩֵ����ֵ�Ĳ�������������������ͬ����·������
    }
}
