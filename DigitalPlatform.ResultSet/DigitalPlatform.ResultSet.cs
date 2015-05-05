using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;

using DigitalPlatform;
using DigitalPlatform.IO;
using DigitalPlatform.Text;


namespace DigitalPlatform.ResultSet
{
    //�����ͼ:
    //���DpResultSetManager�࣬��ArrayList�����ļ��ϣ����ϵĳ�ԱΪDpResultSet����
    //��DpResultSet���������
    public class DpResultSetManager : ArrayList
    {
        public string m_strDebugInfo = "";

        //����: �Ӽ����ҵ�ָ�����Ƶ�DpResultSet
        //myResultSetNameDpResultSet������
        //����ҵ�������Ե�DpResultSet�������û�ҵ�������null
        public DpResultSet GetResultSet(string myResultSetName)
        {
            DpResultSet foundSet = null;
            foreach (DpResultSet oneResultSet in this)
            {
                if (oneResultSet.Name == myResultSetName)
                    foundSet = oneResultSet;
            }
            return foundSet;
        }


        public const char OR = (char)0x01;
        public const char AND = (char)0x02;
        public const char FROM_LEAD = (char)0x03;
        public const char SPLIT = (char)0x04;

        static List<DpRecord> GetSames(DpResultSet resultset,
            int nStart,
            DpRecord start)
        {
            List<DpRecord> results = new List<DpRecord>();
            results.Add(start);
            for (int i=nStart+1; i<results.Count ; i++) // BUG !!!
            {
                DpRecord record = (DpRecord)resultset[i];
                if (record == null)
                    break;
                int ret = start.CompareTo(record);
                if (ret != 0)
                    break;
                results.Add(record);
            }

            return results;
        }

        static int OutputAND(List<DpRecord> left_sames,
            List<DpRecord> right_sames,
            DpResultSet target)
        {
            int nCount = 0;
            for (int i = 0; i < left_sames.Count; i++)
            {
                DpRecord left = left_sames[i];

                for (int j = 0; j < right_sames.Count; j++)
                {
                    DpRecord right = right_sames[j];

                    DpRecord temp_record = new DpRecord(left.ID);
                    temp_record.BrowseText = left.BrowseText
                        + new string(SPLIT, 1) + new string(AND, 1)
                        + right.BrowseText;
                    target.Add(temp_record);

                    nCount++;
                }
            }

            return nCount;
        }

        // һ�߸���һ���޸� ÿ8byte ֵ
        public static long DumpStream(Stream streamSource,
            Stream streamTarget,
            int nChunkSize,
            long lDelta)
        {
            Debug.Assert(nChunkSize % 8 == 0, "");
            byte[] bytes = new byte[nChunkSize];
            long lLength = 0;
            while (true)
            {
                int n = streamSource.Read(bytes, 0, nChunkSize);

                if (n != 0)
                {
                    // �Ի������ڵ�ÿ�� 8 byte ������ֵ������
                    if ((n % 8) != 0)
                        throw new Exception("����index�ļ������з���Ƭ�ϳ��� "+n.ToString()+" ���� 8 ��������");
                    int nCount = n / 8;
                    for (int i = 0; i < nCount; i++)
                    {
                        Int64 v = System.BitConverter.ToInt64(bytes, i * 8);
                        if (v < 0)
                            continue;   // ��ɾ����
                        Array.Copy(BitConverter.GetBytes((Int64)(v + lDelta)),
                                0,
                                bytes,
                                i * 8,
                                8);
                    }

                    streamTarget.Write(bytes, 0, n);
                }

                if (n <= 0)
                    break;

                lLength += n;
            }

            return lLength;
        }

        static void AddIndexFile(Stream target, Stream source, long lDelta)
        {
            // ��source��Ŀ����һ�߶���һ��д��target��β��
            if ((target.Length % 8) != 0)
            {
                throw new Exception("����ǰĿ��index�ļ����� " + target.Length.ToString() + " ���� 8 ��������");
            } 
            long lLength = source.Length;
            if ((lLength % 8) != 0)
            {
                throw new Exception("����ǰԴindex�ļ����� " + lLength.ToString() + " ���� 8 ��������");
            } 
            long lCount = lLength / 8;

            source.Seek(0, SeekOrigin.Begin);
            target.Seek(0, SeekOrigin.End);
            DumpStream(source,
                target,
                1024 * 8,
                lDelta);
        }

        // �۲�����������Ƿ��н���Ĳ���
        // ����ǰ����������������������
        // return:
        //      -1  ����
        //      0   û�н��沿��
        //      1   �н��沿��
        public static int IsCross(DpResultSet sourceLeft,
            DpResultSet sourceRight,
            out string strError)
        {
            strError = "";

            if (sourceLeft.Count == 0 || sourceRight.Count == 0)
                return 0;

            if (sourceLeft.m_streamSmall == null)
            {
                throw new Exception("sourceLeft���������δ������");
            }

            if (sourceRight.m_streamSmall == null)
            {
                throw new Exception("sourceRight���������δ������");
            }

            if (sourceLeft.Sorted == false)
            {
                strError = "����IsCross()ǰsourceLeft��δ����";
                return -1;
            }
            if (sourceRight.Sorted == false)
            {
                strError = "����IsCross()ǰsourceRight��δ����";
                return -1;
            }

            DpRecord leftMin = (DpRecord)sourceLeft[0];
            DpRecord leftMax = (DpRecord)sourceLeft[sourceLeft.Count - 1];

            // ����
            if (leftMin.CompareTo(leftMax) > 0)
            {
                DpRecord temp = leftMin;
                leftMin = leftMax;
                leftMax = temp;
            }

            DpRecord rightMin = (DpRecord)sourceRight[0];
            DpRecord rightMax = (DpRecord)sourceRight[sourceRight.Count - 1];

            // ����
            if (rightMin.CompareTo(rightMax) > 0)
            {
                DpRecord temp = rightMin;
                rightMin = rightMax;
                rightMax = temp;
            }

            // rightMin rightMax
            //    leftMin  leftMax
            if (leftMin.CompareTo(rightMax) <= 0 && rightMin.CompareTo(leftMax) <= 0)
                return 1;

            // leftMin  leftMax
            //    rightMin rightMax
            if (rightMin.CompareTo(leftMax) <= 0 && leftMin.CompareTo(rightMax) <= 0)
                return 1;

            return 0;
        }

        // �������������ǰ��ƴ�����
        // ע�⣺����ִ�й��̣����ܽ��� left �� right��Ҳ����˵���غ� left == right
        // TODO: ��;Ӧ�������ж�
        // TODO: ��������ǰ�������(����˳����ͬ)�����Ҳ����棬�ɾ���������Ӻ�Ҳ��Ȼ�����п��ܰ�sourceRight���ص�sourceLeft��
        public static int AddResults(ref DpResultSet sourceLeft,
            DpResultSet sourceRight,
            out string strError)
        {
            strError = "";

            bool bSorted = false;   // �Ƿ�ϲ�������ά������?
            if (sourceLeft.Sorted == true && sourceRight.Sorted == true
                && sourceLeft.Count > 0 && sourceRight.Count > 0
                && sourceLeft.Asc == sourceRight.Asc)
            {
                DpRecord leftMin = (DpRecord)sourceLeft[0];
                DpRecord leftMax = (DpRecord)sourceLeft[sourceLeft.Count - 1];

                DpRecord rightMin = (DpRecord)sourceRight[0];
                DpRecord rightMax = (DpRecord)sourceRight[sourceRight.Count - 1];

                bool bExchanged = false;
                bool bCross = false;
                if (sourceLeft.Asc == 1)
                {
                    // ����
                    if ((leftMin.CompareTo(rightMax) <= 0 && rightMin.CompareTo(leftMax) <= 0)
                        || (rightMin.CompareTo(leftMax) <= 0 && leftMin.CompareTo(rightMax) <= 0))
                        bCross = true;
                    else if (leftMin.CompareTo(rightMin) > 0)
                        bExchanged = true;
                }
                else
                {
                    // ����
                    if ((leftMax.CompareTo(rightMin) <= 0 && rightMax.CompareTo(leftMin) <= 0)
                        || (rightMax.CompareTo(leftMin) <= 0 && leftMax.CompareTo(rightMin) <= 0))
                        bCross = true;
                    else if (leftMax.CompareTo(rightMax) < 0)
                        bExchanged = true;

                }

                if (bExchanged == true)
                {
                    DpResultSet temp = sourceLeft;
                    sourceLeft = sourceRight;
                    sourceRight = temp;
                }

                if (bCross == false)
                    bSorted = true;
                else
                    bSorted = false;
            }


            long lStart = -1;
            // �������ݲ���
            if (sourceLeft.m_bufferBig != null
    && sourceRight.m_bufferBig != null)
            {
                lStart = sourceLeft.m_bufferBig.Length;
                // ����������ݲ���
                sourceLeft.m_bufferBig = ByteArray.Add(sourceLeft.m_bufferBig, sourceRight.m_bufferBig);
            }
            else
            {
                sourceLeft.WriteToDisk(true, true);
                sourceRight.WriteToDisk(true, true);

                if (sourceLeft.m_streamBig != null
                    && sourceRight.m_streamBig != null)
                {
                    lStart = sourceLeft.m_streamBig.Length;
                    sourceLeft.m_streamBig.Seek(0, SeekOrigin.End);
                    sourceRight.m_streamBig.Seek(0, SeekOrigin.Begin);

                    StreamUtil.DumpStream(sourceRight.m_streamBig,
                        sourceLeft.m_streamBig,
                        false);
                }
                else
                {
                    strError = "�����������������һ�� m_streamBig û�д�";
                    return -1;
                }
            }

            // ������������
            if (sourceLeft.m_bufferSmall != null
                && sourceRight.m_bufferSmall != null)
            {
                if ((sourceLeft.m_bufferSmall.Length % 8) != 0)
                {
                    strError = "����index������ǰ����Ŀ�껺�������� " + sourceLeft.m_bufferSmall.Length.ToString() + " ���� 8 ��������";
                    return -1;
                } 
                int nLength = sourceRight.m_bufferSmall.Length;
                if ((nLength % 8) != 0)
                {
                    strError = "����index������ǰ����Դ���������� " + nLength.ToString() + " ���� 8 ��������";
                    return -1;
                } 
                // ���������������
                sourceLeft.m_bufferSmall = ByteArray.Add(sourceLeft.m_bufferSmall, sourceRight.m_bufferSmall);
                Debug.Assert(sourceLeft.m_bufferSmall.Length == sourceRight.m_bufferSmall.Length + nLength, "");
                // ��lStartλ�ÿ�ʼ��Ϊÿ��������������
                int nCount = nLength / 8;
                for (int i = 0; i < nCount; i++)
                {
                    Int64 v = System.BitConverter.ToInt64(sourceLeft.m_bufferSmall, (int)lStart + i * 8);
                    if (v < 0)
                        continue;   // ��ɾ����
                    Array.Copy(BitConverter.GetBytes((Int64)(v + lStart)),
                            0,
                            sourceLeft.m_bufferSmall,
                            lStart + i * 8,
                            8);
                }
            }
            else
            {
                sourceLeft.WriteToDisk(true, true);
                sourceRight.WriteToDisk(true, true);

                if (sourceLeft.m_streamSmall != null
                    && sourceRight.m_streamSmall != null)
                {
                    AddIndexFile(sourceLeft.m_streamSmall, sourceRight.m_streamSmall, lStart);
                }
                else
                {
                    sourceLeft.CloseSmallFile();
                    sourceRight.CloseSmallFile();
                }
            }

            sourceLeft.RefreshCount();
            if (bSorted == false)
                sourceLeft.Sorted = false;
            return 0;
        }

        public delegate bool QueryStop(object param);


        // TODO: ������ offset ���бȽϣ��ӳٻ�� record  2013/2/13

        // TODO: �����жϻ��ơ�Ϊ�˽�����Դ���ģ�����ÿ100����1000����Ԫ���һ���ж�
        // ����: �ϲ���������
        // parameters:
        //		strLogicOper	������ OR , AND , SUB
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
        public static int Merge(LogicOper logicoper,
            DpResultSet sourceLeft,
            DpResultSet sourceRight,
            string strOutputStyle,
            DpResultSet targetLeft,
            DpResultSet targetMiddle,
            DpResultSet targetRight,
            bool bOutputDebugInfo,
            QueryStop query_stop,
            object param,
            out string strDebugInfo,
            out string strError)
        {
            strDebugInfo = "";
            strError = "";

            DateTime start_time = DateTime.Now;

            // 2010/5/11
            if (sourceLeft.Asc != sourceRight.Asc)
            {
                strError = "sourceLeft.Asc �� sourceRight.Asc ��һ��";
                return -1;
            }

            bool bOutputKeyCount = StringUtil.IsInList("keycount", strOutputStyle);
            bool bOutputKeyID = StringUtil.IsInList("keyid", strOutputStyle);

            int nAsc = sourceLeft.Asc;

            if (targetLeft != null)
                targetLeft.Asc = nAsc;
            if (targetMiddle != null)
                targetMiddle.Asc = nAsc;
            if (targetRight != null)
                targetRight.Asc = nAsc;

            // strLogicOper = strLogicOper.ToUpper();

            if (sourceLeft.m_streamSmall == null)
            {
                throw new Exception("sourceLeft���������δ������");
            }

            if (sourceRight.m_streamSmall == null)
            {
                throw new Exception("sourceRight���������δ������");
            }

            if (sourceLeft.Sorted == false)
            {
                strError = "����Merge()ǰsourceLeft��δ����";
                return -1;
            }
            if (sourceRight.Sorted == false)
            {
                strError = "����Merge()ǰsourceRight��δ����";
                return -1;
            }

            if (bOutputDebugInfo == true)
            {
                strDebugInfo += "strStyleֵ:" + logicoper.ToString() + "<br/>";
                strDebugInfo += "sourceLeft�����:" + sourceLeft.Dump() + "<br/>";
                strDebugInfo += "sourceRight�����:" + sourceRight.Dump() + "<br/>";
            }

            if (logicoper == LogicOper.OR)
            {
                // OR������Ӧʹ��targetLeft��targetRight����
                if (targetLeft != null || targetRight != null)
                {
                    Exception ex = new Exception("DpResultSetManager::Merge()���ǲ��ǲ����ô���?��strStyle����ֵΪ\"OR\"ʱ��targetLeft������targetRight��Ч��ֵӦΪnull");
                    throw (ex);
                }
            }

            // 2010/10/2 add
            if (logicoper == LogicOper.SUB)
            {
                // SUB������Ӧʹ��targetMiddle��targetRight����
                if (targetMiddle != null || targetRight != null)
                {
                    Exception ex = new Exception("DpResultSetManager::Merge()���ǲ��ǲ����ô���?��strStyle����ֵΪ\"SUB\"ʱ��targetMiddle������targetRight��Ч��ֵӦΪnull");
                    throw (ex);
                }
            }

            DpRecord left = null;
            DpRecord right = null;

            DpRecord old_left = null;
            DpRecord old_right = null;
            int old_ret = 0;

            int m_nLoopCount = 0;

            int i = 0;
            int j = 0;
            int ret = 0;
            while (true)
            {
                if (m_nLoopCount++ % 1000 == 0)
                {
                    Thread.Sleep(1);
                    if (query_stop != null)
                    {
                        if (query_stop(param) == true)
                        {
                            strError = "�û��ж�";
                            return -1;
                        }
                    }
                }

                old_left = left;
                old_right = right;
                old_ret = ret;

                // ׼��left right
                left = null;
                right = null;
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
                        left = (DpRecord)sourceLeft[i];
                        if (bOutputDebugInfo == true)
                        {
                            strDebugInfo += "ȡ��sourceLeft�����е�" + Convert.ToString(i) + "��Ԫ�أ�IDΪ" + left.ID + "<br/>";
                        }
                    }
                    catch (Exception e)
                    {
                        Exception ex = new Exception("ȡSourceLeft���ϳ���i=" + Convert.ToString(i) + "----Count=" + Convert.ToString(sourceLeft.Count) + ", internel error :" + e.Message+ "<br/>");
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
                        right = (DpRecord)sourceRight[j];
                        if (bOutputDebugInfo == true)
                        {
                            strDebugInfo += "ȡ��sourceRight�����е�" + Convert.ToString(j) + "��Ԫ�أ�IDΪ" + right.ID + "<br/>";
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

                if (left == null)
                {
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "dpRecordLeftΪnull����ret����1<br/>";
                    }
                    ret = 1;
                }
                else if (right == null)
                {
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "dpRecordRightΪnull����ret����-1<br/>";
                    }
                    ret = -1;
                }
                else
                {
                    ret = nAsc * left.CompareTo(right);  //MyCompareTo(oldOneKey); //��CompareTO
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "dpRecordLeft��dpRecordRight����Ϊnull���Ƚ�������¼�õ�ret����" + Convert.ToString(ret) + "<br/>";
                    }
                }



                if (logicoper == LogicOper.OR && targetMiddle != null)
                {
                    if (ret == 0)
                    {
                        if (bOutputKeyCount == false && bOutputKeyID == false)
                        {
                            // idֵ��ȫ��ͬ��ʱ��������
                            targetMiddle.Add(left);
                        }
                        else if (bOutputKeyCount == true)
                        {
                            // 2008/11/21 changed
                            // idֵ��ȫ��ͬ��ʱ�����һ����������߶�����¶�����IndexӦ��Ϊԭ�����������
                            DpRecord temp_record = new DpRecord(left.ID);
                            temp_record.Index = left.Index + right.Index;
                            targetMiddle.Add(temp_record);
                        }
                        else if (bOutputKeyID == true)
                        {
                            /*
                            // 2010/5/17 new add
                            // idֵ��ȫ��ͬ��ʱ�����һ����������߶�����¶�����BrowseTextӦ��Ϊԭ�������Ĵ���
                            DpRecord temp_record = new DpRecord(dpRecordLeft.ID);
                            temp_record.BrowseText = dpRecordLeft.BrowseText + new string(OR, 1) + dpRecordRight.BrowseText;
                            targetMiddle.Add(temp_record);
                             * */
                            // 2010/5/17 new add
                            // idֵ��ȫ��ͬ��ʱ�����Keys����ͬ����ͬʱ�����ߺ��ұ߶���
                            if (left.BrowseText != right.BrowseText)
                            {
                                targetMiddle.Add(left);
                                targetMiddle.Add(right);
                            }
                            else
                                targetMiddle.Add(left);
                        }

                        i++;
                        j++;
                    }
                    else if (ret < 0)
                    {
                        targetMiddle.Add(left);
                        i++;
                    }
                    else if (ret > 0)
                    {
                        targetMiddle.Add(right);
                        j++;
                    }
                    continue;
                }

                if (ret == 0)
                {
                    if (targetMiddle != null)
                    {
                        if (bOutputDebugInfo == true)
                        {
                            strDebugInfo += "ret����0,�ӵ�targetMiddle����<br/>";
                        }

                        if (logicoper != LogicOper.AND)
                            targetMiddle.Add(left);
                        else
                        {
                            Debug.Assert(logicoper == LogicOper.AND, "");

                            if (bOutputKeyCount == false && bOutputKeyID == false)
                            {
                                // idֵ��ȫ��ͬ��ʱ��������
                                targetMiddle.Add(left);
                            }
                            else if (bOutputKeyCount == true)
                            {
                                strError = "��keycount�����ʽ�£��޷����н����֮���AND����";
                                return -1;
                            }
                            else if (bOutputKeyID == true)
                            {
                                /*
                                // 2010/5/17 new add
                                // idֵ��ȫ��ͬ��ʱ����BrowseTextӦ��Ϊԭ�������Ĵ���
                                DpRecord temp_record = new DpRecord(left.ID);
                                temp_record.BrowseText = left.BrowseText
                                    + new string(SPLIT, 1) + new string(AND, 1)
                                    + right.BrowseText;
                                targetMiddle.Add(temp_record);
                                 * */

                                // ��ߺ��ұ���ͬ�Ľ������
                                List<DpRecord> left_sames = GetSames(sourceLeft,
                                    i,
                                    left);
                                Debug.Assert(left_sames.Count >= 1, "");
                                List<DpRecord> right_sames = GetSames(sourceRight,
                                    j,
                                    right);
                                Debug.Assert(right_sames.Count >= 1, "");
                                OutputAND(left_sames, right_sames, targetMiddle);

                                i += left_sames.Count;
                                j += right_sames.Count;
                                continue;
                            }
                        }

                    }   // endof if (targetMiddle != null)

                    // 2010/10/2 �ƶ�������
                    i++;
                    j++;
                }

                if (ret < 0)
                {
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "retС��0,�ӵ�targetLeft����<br/>";
                    }

                    if (targetLeft != null && left != null)
                        targetLeft.Add(left);
                    i++;
                }

                if (ret > 0)
                {
                    if (bOutputDebugInfo == true)
                    {
                        strDebugInfo += "ret����0,�ӵ�targetRight����<br/>";
                    }

                    if (targetRight != null && right != null)
                        targetRight.Add(right);

                    j++;
                }
            }

            if (targetLeft != null)
                targetLeft.Sorted = true;
            if (targetMiddle != null)
                targetMiddle.Sorted = true;
            if (targetRight != null)
                targetRight.Sorted = true;

            TimeSpan delta = DateTime.Now - start_time;
            Debug.WriteLine("Merge() " + logicoper.ToString() + " ��ʱ " + delta.ToString());

            return 0;
        }


    } //end of class DpResultSetManager


    // �������	
    public class DpResultSet : IEnumerable, IDisposable
    {
        public event GetTempFilenameEventHandler GetTempFilename = null;
        public string TempFileDir = ""; // ���ڴ����ʹ洢��ʱ�ļ���Ŀ¼

        int m_nLoopCount = 0;
        public event IdleEventHandler Idle = null;
        public object Param = null;

        public int Asc = 1; // 1 ���� -1 ����
        public bool Sorted = false; // �Ƿ��Ѿ��Ź���

        //��ʾ�����������
        protected string m_strName;

        public string m_strQuery; //����ʽXML�ַ���
        public int m_nStatus = 0; //0,��δ������;1,�Ѿ�������;-1,����ʧ��

        //��ʾ��Ž������������ΪDpResultManager���ͣ�����DpResultSet��DpResultSetManager�ǰ����Ĺ�ϵ
        //������ϵ:��һ����Ա�ֶΰ�������ʵ�����Ϳ���ʵ�ְ�����ϵ�������������ȫ���ƶԱ�������ĳ�Ա�ķ��ʣ�
        public DpResultSetManager m_container;

        //���ļ�������
        public string m_strBigFileName = "";
        internal Stream m_streamBig = null;
        static int nBigBufferSize = 4 * 1024 * 1024;
        internal byte[] m_bufferBig = null;

        //С�ļ���С��
        public string m_strSmallFileName = "";
        public Stream m_streamSmall = null;
        static int nSmallBufferSize = 4096 * 100;
        internal byte[] m_bufferSmall = null;

        public long m_count = 0;

        bool bDirty = false; //��ʼֵfalse,��ʾ�ɾ�

        public DateTime CreateTime = DateTime.Now;
        public DateTime LastUsedTime = DateTime.Now;

        public void Touch()
        {
            this.LastUsedTime = DateTime.Now;
        }

        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);

#if NO
            // Take yourself off the Finalization queue 
            // to prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
#endif
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // release managed resources if any
                }

                // release unmanaged resource
                this.Close();

                // Note that this is not thread safe.
                // Another thread could start disposing the object
                // after the managed resources are disposed,
                // but before the disposed flag is set to true.
                // If thread safety is necessary, it must be
                // implemented by the client.
            }
            disposed = true;
        }

        ~DpResultSet()
        {
            Dispose(false);
        }
#if NO
        ~DpResultSet()
        {
            Close();
        }
#endif

        //���캯������ʼ��m_strName��m_container
        //strName����Ľ���������ַ���
        //objResultSetManager�������������
        public DpResultSet(string strName,
            DpResultSetManager objResultSetManager)
        {
            m_strName = strName;
            m_container = objResultSetManager;
            Open(false);  //��������
        }

        public DpResultSet(bool bOpen,
            bool bCreateIndex)
        {
            if (bOpen == true)
            {
                Open(bCreateIndex);
            }
        }


        public DpResultSet(bool bCreateIndex)
        {
            Open(bCreateIndex);
        }


        public DpResultSet()
        {
            Open(false);
        }

        public DpResultSet(Delegate_getTempFileName procGetTempFileName)
        {
            Open(false, procGetTempFileName);
        }

        //��Ӧm_strName����ʾ��������ƣ��ṩ���ⲿ����ʹ��
        public string Name
        {
            get
            {
                return m_strName;
            }
        }

        // ȷ������������
        public void EnsureCreateIndex()
        {
            if (this.m_streamSmall == null)
            {
                DateTime start_time = DateTime.Now;
                this.CreateSmallFile();
                TimeSpan delta = DateTime.Now - start_time;
                Debug.WriteLine("EnsureCreateIndex() ��ʱ " + delta.ToString());
            }
            else
            {
                // Debugʱ����У��һ��index�ļ��ߴ��Count�Ĺ�ϵ�Ƿ���ȷ
            }
        }

        /// <summary>
        /// ������Ƿ��Ѿ����ر�
        /// </summary>
        public bool IsClosed
        {
            get
            {
                return this.m_streamBig == null;
            }
        }

        //����:����һ���Ἧ�����Լ�
        public int Copy(DpResultSet sourceResultSet)
        {
            // ��������Լ�
            if (sourceResultSet == this)
                return 0;

            this.Clear();

            if (sourceResultSet.Count == 0)
                goto END1;
            /*
            foreach (DpRecord record in sourceResultSet)
            {
                this.Add(record);
            }
             * */
            // 2007/1/1
            bool bFirst = true;
            long lPos = 0;
            DpRecord record = null;
            for (; ; )
            {
                if (bFirst == true)
                {
                    record = sourceResultSet.GetFirstRecord(
                        0,
                        false,
                        out lPos);
                }
                else
                {
                    // ȡԪ�ر�[]�����ٶȿ�
                    record = sourceResultSet.GetNextRecord(
                        ref lPos);
                }

                bFirst = false;

                if (record == null)
                    break;

                this.Add(record);
            }

        END1:
            this.Sorted = sourceResultSet.Sorted;
            return 0;
        }

        //���
        public void Clear()
        {
            if (m_streamBig != null && m_streamBig.Length > 0)
                m_streamBig.SetLength(0);
            if (m_streamSmall != null && m_streamSmall.Length > 0)
                m_streamSmall.SetLength(0);
            m_count = 0;

            // 2011/1/7 add
            this.m_bufferSmall = null;
            this.m_bufferBig = null;

            this.Sorted = false;
        }

        //��¼��
        public long Count
        {
            get
            {
                return m_count;
            }
        }

        // 2010/10/11
        // ��������Ϊд����׼��
        public void Create(string strBigFilename,
            string strSmallFilename)
        {
            CloseAndDeleteBigFile();
            CloseAndDeleteSmallFile();

            File.Delete(strBigFilename);
            File.Delete(strSmallFilename);

            this.m_strBigFileName = strBigFilename;
            this.m_strSmallFileName = strSmallFilename;

            if (String.IsNullOrEmpty(this.m_strSmallFileName) == false)
                Open(true);
            else
                Open(false);
        }

        // �����ʱ�ļ���
        string DoGetTempFilename()
        {
            if (string.IsNullOrEmpty(this.TempFileDir) == false)
            {
                Debug.Assert(string.IsNullOrEmpty(this.TempFileDir) == false, "");
                while (true)
                {
                    string strFilename = Path.Combine(this.TempFileDir, Guid.NewGuid().ToString());
                    if (File.Exists(strFilename) == false)
                    {
                        using (FileStream s = File.Create(strFilename))
                        {
                        }
                        return strFilename;
                    }
                }
            }

            if (this.GetTempFilename == null)
                return Path.GetTempFileName();

            GetTempFilenameEventArgs e = new GetTempFilenameEventArgs();
            this.GetTempFilename(this, e);
            if (String.IsNullOrEmpty(e.TempFilename) == true)
            {
                Debug.Assert(false, "��Ȼ�ӹ����¼�������û��ʵ���Զ���");
                return Path.GetTempFileName();
            }

            Debug.Assert(string.IsNullOrEmpty(e.TempFilename) == false , "");
            return e.TempFilename;
        }

        public delegate string Delegate_getTempFileName();

        // 
        public void Open(bool bCreateIndex, 
            Delegate_getTempFileName procGetTempFileName = null)
        {
            if (m_streamBig == null)
            {
                if (m_strBigFileName == "")
                {
                    // ����ʹ����ʱ�ĺ��� ������ʱ�ļ�
                    if (procGetTempFileName != null)
                        m_strBigFileName = procGetTempFileName();

                    if (string.IsNullOrEmpty(m_strBigFileName) == true)
                        m_strBigFileName = DoGetTempFilename();
                }
                this.m_streamBig = File.Open(m_strBigFileName,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite);   // 2010/10/11 changed
            }
            if (bCreateIndex == true)
            {
                CreateIndex();
            }
        }

        //��������
        public void CreateIndex()
        {
            if (m_streamSmall != null)
            {
                m_streamSmall.Close();
                m_streamSmall = null;
                if (m_strSmallFileName != "")
                {
                    File.Delete(m_strSmallFileName);
                    m_strSmallFileName = "";
                }
            }
            if (m_streamSmall == null)
            {
                if (m_strSmallFileName == "")
                    m_strSmallFileName = DoGetTempFilename();

                m_streamSmall = File.Open(m_strSmallFileName,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite);   // 2010/10/11 changed
            }
        }

        public void Flush()
        {
            if (m_streamBig != null)
            {
                if (string.IsNullOrEmpty(m_strBigFileName) == false)
                {
                    // ���´�
                    m_streamBig.Close();
                    m_streamBig = File.Open(m_strBigFileName,
                        FileMode.Open,
                        FileAccess.ReadWrite,
                        FileShare.ReadWrite);
                }
                else
                {
                    // ˢ������
                    m_streamBig.Flush();
                }
            }

            if (m_streamSmall != null)
            {
                if (string.IsNullOrEmpty(m_strSmallFileName) == false)
                {
                    // ���´�
                    m_streamSmall.Close();
                    m_streamSmall = File.Open(m_strSmallFileName,
                       FileMode.Open,
                       FileAccess.ReadWrite,
                       FileShare.ReadWrite);
                }
                else
                {
                    // ˢ������
                    m_streamSmall.Flush();
                }
            }
        }

        public void Close()
        {
            if (m_streamBig != null)
            {
                m_streamBig.Close();
                m_streamBig = null;
            }

            if (string.IsNullOrEmpty(m_strBigFileName) == false)
            {
                try
                {
                    File.Delete(m_strBigFileName);
                }
                catch
                {
                }
                m_strBigFileName = "";
            }

            if (m_streamSmall != null)
            {
                m_streamSmall.Close();
                m_streamSmall = null;
            }

            if (string.IsNullOrEmpty(m_strSmallFileName) == false)
            {
                try
                {
                    File.Delete(m_strSmallFileName);
                }
                catch
                {
                }
                m_strSmallFileName = "";
            }
        }

        public void CloseBigFile()
        {
            if (m_streamBig != null)
            {
                m_streamBig.Close();
                m_streamBig = null;
            }
            m_strBigFileName = "";
        }

        public void CloseAndDeleteBigFile()
        {
            if (m_streamBig != null)
            {
                m_streamBig.Close();
                m_streamBig = null;
            }
            if (m_strBigFileName != "")
                File.Delete(m_strBigFileName);
            m_strBigFileName = "";
        }

        // 2010/10/11
        public void CloseSmallFile()
        {
            if (m_streamSmall != null)
            {
                m_streamSmall.Close();
                m_streamSmall = null;
            }
        }

        public void CloseAndDeleteSmallFile()
        {
            if (m_streamSmall != null)
            {
                m_streamSmall.Close();
                m_streamSmall = null;
            }
            if (string.IsNullOrEmpty(m_strSmallFileName) == false)
                File.Delete(m_strSmallFileName);
            // m_strBigFileName = ""; BUG!!!
            this.m_strSmallFileName = "";   // 2010/10/11
        }

        // ����С�ļ��ĳߴ���������
        public static long GetCount(string strSmallFilename)
        {
            try
            {
                FileInfo fi = new FileInfo(strSmallFilename);
                return fi.Length / 8;
            }
            catch
            {
                return -1;
            }
        }

        // 2010/10/11
        // ���ļ��ҽӵ��������
        // parameters:
        //      strBigFileName ���ļ�����
        public void Attach(string strBigFileName,
            string strSmallFileName)
        {
            CloseAndDeleteBigFile();

            m_strBigFileName = strBigFileName;
            m_streamBig = File.Open(m_strBigFileName,
                FileMode.Open,
                FileAccess.ReadWrite,
                    FileShare.ReadWrite);

            CloseAndDeleteSmallFile();

            if (String.IsNullOrEmpty(strSmallFileName) == false)
            {
                this.m_strSmallFileName = strSmallFileName;

                m_streamSmall = File.Open(m_strSmallFileName, 
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite);

                this.m_count = m_streamSmall.Length / 8;  //m_count;

            }
            else
            {
                m_count = GetCount();
            }
        }

        // ��ĳ���ļ��ҽӵ��������
        // parameters:
        //      strFileName ���ļ�����
        public void Attach(string strFileName)
        {
            CloseAndDeleteBigFile();

            m_strBigFileName = strFileName;
            m_streamBig = File.Open(m_strBigFileName, 
                FileMode.OpenOrCreate,  // ???
                FileAccess.ReadWrite,
                    FileShare.ReadWrite);   // 2010/10/11 changed

            m_count = GetCount();
        }

        public void RefreshCount()
        {
            if (this.m_streamSmall != null)
                this.m_count = m_streamSmall.Length / 8;
            else
                this.m_count = GetCount();
        }

        // ���������ļ��õ���¼��
        public long GetCount()
        {
            if (m_streamBig.Position != 0)  // 2012/2/15 ***
                m_streamBig.Seek(0, SeekOrigin.Begin);

            int i = 0;
            long nLength;

            while (true)
            {
                //�����ֽ�����
                byte[] bufferLength = new byte[4];
                int n = m_streamBig.Read(bufferLength, 0, 4);
                if (n < 4)   //��ʾ�ļ���β
                    break;

                nLength = System.BitConverter.ToInt32(bufferLength, 0);
                if (nLength < 0)  //ɾ����
                {
                    nLength = (int)GetRealValue(nLength);
                    goto CONTINUE;
                }
                i++;

            CONTINUE:
                m_streamBig.Seek(nLength, SeekOrigin.Current);
            }
            return i;
        }

        // 2010/10/11
        // �������ļ��Ͷ����ѹ�
        // parameters:
        // return:
        //	�����ļ���
        public void Detach(out string strDataFileName,
            out string strIndexFileName)
        {
            strDataFileName = m_strBigFileName;
            CloseBigFile();

            m_strBigFileName = "";	// ������������ȥɾ��

            strIndexFileName = this.m_strSmallFileName;
            CloseSmallFile();

            this.m_strSmallFileName = "";	// ������������ȥɾ��
        }

        // Detach()
        public string Detach()
        {
            string strFileName = m_strBigFileName;
            CloseBigFile();
            CloseAndDeleteSmallFile();
            return strFileName;
        }

        // �����ڴ棬Ϊ������׼��
        void ReadToMemory()
        {
            if (this.m_streamBig != null &&
                this.m_streamBig.Length <= nBigBufferSize)
            {
                m_bufferBig = new byte[this.m_streamBig.Length];
                if (m_streamBig.Position != 0)  // 2012/2/15 ***
                    m_streamBig.Seek(0, SeekOrigin.Begin);
                m_streamBig.Read(m_bufferBig, 0, m_bufferBig.Length);
            }

            if (this.m_streamSmall != null &&
                this.m_streamSmall.Length <= nSmallBufferSize)
            {
                m_bufferSmall = new byte[this.m_streamSmall.Length];
                if (m_streamSmall.Position != 0)  // 2012/2/15 ***
                    m_streamSmall.Seek(0, SeekOrigin.Begin);
                m_streamSmall.Read(m_bufferSmall, 0, m_bufferSmall.Length);
            }
        }

        // д���ļ���Ϊ������β
        internal void WriteToDisk(bool bWriteSmall,
            bool bWriteBig)
        {
            if (bWriteBig == true &&
                this.m_streamBig != null &&
                this.m_bufferBig != null)
            {
                if (m_streamBig.Position != 0)  // 2012/2/15 ***
                    m_streamBig.Seek(0, SeekOrigin.Begin);
                m_streamBig.Write(m_bufferBig, 0, m_bufferBig.Length);
            }

            if (bWriteSmall == true &&
                this.m_streamSmall != null &&
                this.m_bufferSmall != null)
            {
                if (m_streamSmall.Position != 0)  // 2012/2/15 ***
                    m_streamSmall.Seek(0, SeekOrigin.Begin);
                m_streamSmall.Write(m_bufferSmall, 0, m_bufferSmall.Length);
            }
        }

        //�õ�������λ�û򳤶�
        public long GetRealValue(long lPositionOrLength)
        {
            if (lPositionOrLength < 0)
            {
                lPositionOrLength = -lPositionOrLength;
                lPositionOrLength--;
            }
            return lPositionOrLength;
        }

        //�õ�ɾ�����ʹ�õ�λ�û򳤶�
        public long GetDeletedValue(long lPositionOrLength)
        {
            if (lPositionOrLength >= 0)
            {
                lPositionOrLength++;
                lPositionOrLength = -lPositionOrLength;
            }
            return lPositionOrLength;
        }


        public long GetPhysicalCount()
        {
            Debug.Assert(this.m_streamSmall != null, "");

            return this.m_streamSmall.Length / 8;
        }

        // ��RemoveDup()֮ǰ����������
        // ��;�ᴥ��Idle�¼�
        // 2013/2/13 �Ż�
        public void RemoveDup()
        {
            if (this.Sorted == false)
                throw new Exception("��RemoveDup()֮ǰ����������");

            if (this.Count <= 1)
                return;

            long physicalCount = GetPhysicalCount();

            m_nLoopCount = 0;

            long prevOffset = -1;

            for (int i = 0; i < physicalCount; i++)
            {
                if (m_nLoopCount++ % 1000 == 0)
                {
                    Thread.Sleep(1);
                    if (this.Idle != null)
                    {
                        IdleEventArgs e = new IdleEventArgs();
                        this.Idle(this, e);
                    }
                }

                long curOffset = GetBigOffsetBySmall(i);
                if (curOffset < 0)
                    continue;

                if (prevOffset != -1)
                {
                    if (Compare(curOffset, prevOffset) == 0)
                    {
                        RemoveAtPhysical(i);
                        m_count--;
                        bDirty = true;
                    }
                }

                prevOffset = curOffset;
            }

            // Ϊ���Ժ���ٷ��ʣ��������ļ��е�ɾ����ǵļ�¼ѹ����
            this.CompressIndex();
        }


        //���ɾ��һ����¼
        public void RemoveAtPhysical(int nIndex)
        {
            //�Գ�8�ķ�ʽ��С�ļ��еõ����ļ���ƫ����
            long lBigOffset = GetBigOffsetBySmall(nIndex);
            lBigOffset = GetDeletedValue(lBigOffset);

            byte[] bufferBigOffset = new byte[8];
            bufferBigOffset = BitConverter.GetBytes((long)lBigOffset);

            if (m_streamSmall.Position != nIndex * 8)  // 2012/2/15 ***
                m_streamSmall.Seek(nIndex * 8, SeekOrigin.Begin);
            Debug.Assert(bufferBigOffset.Length == 8, "");
            m_streamSmall.Write(bufferBigOffset, 0, 8);
        }

#if NO
        //���ɾ��һ����¼
        public void RemoveAt(int nIndex)
        {
            if (nIndex < 0 || nIndex >= m_count)
            {
                throw (new Exception("�±� " + Convert.ToString(nIndex) + " Խ��(Count=" + Convert.ToString(m_count) + ")"));
            }

            int nRet = RemoveAtA(nIndex);
            if (nRet == -1)
            {
                throw (new Exception("��ɾ�����ʧ��"));
            }


            //�ܼ�¼����һ��bDirty��Ϊtrue;
            m_count--;
            bDirty = true;
        }
#endif

        public int RemoveAtA(int nIndex)
        {
            int nRet = -1;
            if (m_streamSmall != null) //��С�ļ�ʱ
            {
                nRet = RemoveAtS(nIndex);
            }
            else  //С�ļ�������ʱ�� �Ӵ��ļ���ɾ��
            {
                nRet = RemoveAtB(nIndex);
            }
            return nRet;
        }

        //��λС��
        public long LocateS(int nIndex)
        {
            long lPositionS = 0;
            if (bDirty == false)
            {
                lPositionS = nIndex * 8;
                if (lPositionS >= m_streamSmall.Length || nIndex < 0)
                {
                    throw (new Exception("�±�Խ��..."));
                }
                if (m_streamSmall.Position != lPositionS)  // 2012/2/15 ***
                    m_streamSmall.Seek(lPositionS, SeekOrigin.Begin);
                return lPositionS;
            }
            else
            {
                if (m_streamSmall.Position != 0)  // 2012/2/15 ***
                    m_streamSmall.Seek(0, SeekOrigin.Begin);
                long lBigOffset;
                int i = 0;
                while (true)
                {
                    //��8���ֽڣ��õ�λ��
                    byte[] bufferBigOffset = new byte[8];
                    int n = m_streamSmall.Read(bufferBigOffset, 0, 8);
                    if (n < 8)   //��ʾ�ļ���β
                        break;
                    lBigOffset = System.BitConverter.ToInt64(bufferBigOffset, 0);

                    //Ϊ����ʱ����
                    if (lBigOffset < 0)
                    {
                        goto CONTINUE;
                    }

                    //��ʾ������ҵ�
                    if (i == nIndex)
                    {
                        if (m_streamSmall.Position != lPositionS)  // 2012/2/15 ***
                            m_streamSmall.Seek(lPositionS, SeekOrigin.Begin);
                        return lPositionS;
                    }
                    i++;

                CONTINUE:
                    lPositionS += 8;
                }
            }
            return -1;
        }

        //��С�ļ���ɾ��
        public int RemoveAtS(int nIndex)
        {
            int nRet;

            //lBigOffset��ʾ���ļ��ı�������-1��ʾ����
            long lBigOffset = GetBigOffsetS(nIndex, false);
            if (lBigOffset == -1)
                return -1;

            lBigOffset = GetDeletedValue(lBigOffset);

            byte[] bufferBigOffset = new byte[8];
            bufferBigOffset = BitConverter.GetBytes((long)lBigOffset);

            nRet = (int)LocateS(nIndex);
            if (nRet == -1)
                return -1;
            Debug.Assert(bufferBigOffset.Length == 8, "");
            m_streamSmall.Write(bufferBigOffset, 0, 8);

            return 0;
        }

        //�Ӵ��ļ���ɾ��
        public int RemoveAtB(int nIndex)
        {
            //�õ����ļ�ƫ����
            long lBigOffset = GetBigOffsetB(nIndex, false);
            if (lBigOffset == -1)
                return -1;

            if (lBigOffset >= m_streamBig.Length)
            {
                throw (new Exception("�ڲ�����λ�ô����ܳ���"));
                //return null;
            }

            if (m_streamBig.Position != lBigOffset)  // 2012/2/15 ***
                m_streamBig.Seek(lBigOffset, SeekOrigin.Begin);
            //�����ֽ�����
            byte[] bufferLength = new byte[4];
            int n = m_streamBig.Read(bufferLength, 0, 4);
            if (n < 4)   //��ʾ�ļ���β
            {
                throw (new Exception("�ڲ�����:Read error"));
                //return null;
            }

            int nLength = System.BitConverter.ToInt32(bufferLength, 0);
            nLength = (int)GetDeletedValue(nLength);

            bufferLength = BitConverter.GetBytes((Int32)nLength);

            m_streamBig.Seek(-4, SeekOrigin.Current);

            Debug.Assert(bufferLength.Length == 4, "");
            m_streamBig.Write(bufferLength, 0, 4);

            return 0;
        }



        //�Զ����ش��ļ��ı�����,С�ļ�����ʱ����С�ļ��õ���������ʱ���Ӵ��ļ��õ�
        //bContainDeleted����false��������ɾ���ļ�¼��Ϊtrue,������
        //����ֵ
        //>=0:����
        //-1:��bContainDeletedΪfalseʱ:��ʾ����������trueʱ��ʾ�����ĸ�ֵ
        public long GetBigOffsetA(long nIndex, bool bContainDeleted)
        {
            if (m_streamSmall != null)
            {
                return GetBigOffsetS(nIndex, bContainDeleted);
            }
            else
            {
                return GetBigOffsetB(nIndex, bContainDeleted);
            }
        }

        //����С�ļ����ش��ļ���ƫ����
        //����ֵΪ���ļ��ĳ���
        //��bContainDeletedΪfalseʱ-1:��ʾ����������trueʱ��ʾ�����ĸ�ֵ
        public long GetBigOffsetS(long nIndex, bool bContainDeleted)
        {
            if (m_streamSmall == null)
            {
                throw (new Exception("С�ļ�Ϊnull,GetBigOffsetAndSmallOffset()�������Ŀ�ģ�ֻ��С�ļ��в��ҡ�"));
            }

            long lBigOffset = 0;
            //�ɾ�
            if (bDirty == false)
            {
                if (nIndex * 8 >= m_streamSmall.Length || nIndex < 0)
                {
                    throw (new Exception("nIndex=" + Convert.ToString(nIndex) + "  m_streamSmall.Length=" + Convert.ToString(m_streamSmall.Length) + " �±�Խ��"));
                }
                //�޸�λ��Ϊ����
                lBigOffset = GetBigOffsetBySmall(nIndex);
                return lBigOffset;
            }
            else
            {
                if (m_streamSmall.Position != 0)  // 2012/2/15 ***
                    m_streamSmall.Seek(0, SeekOrigin.Begin);
                int i = 0;
                while (true)
                {
                    //��8���ֽڣ��õ�λ��
                    byte[] bufferBigOffset = new byte[8];
                    int n = m_streamSmall.Read(bufferBigOffset, 0, 8);
                    if (n < 8)   //��ʾ�ļ���β
                        break;
                    lBigOffset = System.BitConverter.ToInt32(bufferBigOffset, 0);

                    if (bContainDeleted == false)
                    {
                        //Ϊ����ʱ����
                        if (lBigOffset < 0)
                        {
                            continue;
                        }
                    }
                    //��ʾ������ҵ�
                    if (i == nIndex)
                    {
                        return lBigOffset;
                    }
                    i++;
                }
            }
            return -1;
        }

        // �Ӵ��ļ��еõ���һ����¼�ĵ�ַ�����㱻��ɾ����ǵļ�¼
        public long GetNextOffsetOfBigFile(long lPos)
        {
            if (m_streamBig.Position != lPos)  // 2012/2/15 ***
                m_streamBig.Seek(lPos, SeekOrigin.Begin);
            long lOffset = lPos;

            int nLength;
            while (true)
            {
                //��4���ֽڣ��õ�����
                byte[] bufferLength = new byte[4];
                int n = m_streamBig.Read(bufferLength, 0, 4);
                if (n < 4)   //��ʾ�ļ���β
                    break;
                nLength = System.BitConverter.ToInt32(bufferLength, 0);

                // ��ʾ�Ѵ�ɾ����ǵļ�¼
                if (nLength < 0)
                {
                    //ת��Ϊʵ�ʵĳ��ȣ���seek
                    long lTemp = GetRealValue(nLength);

                    m_streamBig.Seek(lTemp, SeekOrigin.Current);

                    lOffset += (4 + lTemp);
                    continue;
                }
                else
                {
                    return lOffset;
                }
            }

            return -1;
        }

        //���ݴ��ļ����ش��ļ���ƫ����
        //����ֵΪ���ļ��ĳ���
        //��bContainDeletedΪfalseʱ-1:��ʾ����������trueʱ��ʾ�����ĸ�ֵ
        public long GetBigOffsetB(long nIndex, bool bContainDeleted)
        {
            if (m_streamBig.Position != 0)  // 2012/2/15 ***
                m_streamBig.Seek(0, SeekOrigin.Begin);
            long lBigOffset = 0;

            int nLength;
            int i = 0;
            while (true)
            {
                //��4���ֽڣ��õ�����
                byte[] bufferLength = new byte[4];
                int n = m_streamBig.Read(bufferLength, 0, 4);
                if (n < 4)   //��ʾ�ļ���β
                    break;
                nLength = System.BitConverter.ToInt32(bufferLength, 0);

                if (bContainDeleted == false)
                {
                    if (nLength < 0)
                    {
                        //ת��Ϊʵ�ʵĳ��ȣ���seek
                        long lTemp = GetRealValue(nLength);
                        m_streamBig.Seek(lTemp, SeekOrigin.Current);

                        lBigOffset += (4 + lTemp);
                        continue;
                    }
                }

                if (i == nIndex)
                {
                    return lBigOffset;
                }
                else
                {
                    m_streamBig.Seek(nLength, SeekOrigin.Current);
                }

                lBigOffset += (4 + nLength);

                i++;
            }

            return -1;
        }

        //ͨ��this[i]�Ҽ�¼
        public DpRecord this[long nIndex]
        {
            get
            {
                return GetRecord(nIndex, false);
            }
        }


        //record
        //0:����
        //1:�ҵ�
        //-1:����
        public DpRecord GetRecord(long nIndex,
            bool bContainDeleted)
        {
            DpRecord record = null;
            long lBigOffset;

            //�Զ����ش��ļ��ı�����,С�ļ�����ʱ����С�ļ��õ���������ʱ���Ӵ��ļ��õ�
            //bContainDeleted����false��������ɾ���ļ�¼��Ϊtrue,������
            //����ֵ
            //>=0:����
            //-1:��bContainDeletedΪfalseʱ:��ʾ����������trueʱ��ʾ�����ĸ�ֵ
            lBigOffset = GetBigOffsetA(nIndex, bContainDeleted);

            //��bContainDeletedΪfalseʱ����������ɾ����¼ʱ������ֵ-1����ʾû�ҵ�
            if (bContainDeleted == false)
            {
                if (lBigOffset == -1)
                    return null;
            }
            record = GetRecordByOffset(lBigOffset);

            return record;
        }

        // �õ���һ����¼,�𵽶�λ����
        // return:
        //		null	�ļ�����
        //		����	��¼����
        public DpRecord GetFirstRecord(long nIndex,
            bool bContainDeleted,
            out long lPos)
        {
            lPos = -1;
            DpRecord record = null;
            long lBigOffset;

            //�Զ����ش��ļ��ı�����,С�ļ�����ʱ����С�ļ��õ���������ʱ���Ӵ��ļ��õ�
            //bContainDeleted����false��������ɾ���ļ�¼��Ϊtrue,������
            //����ֵ
            //>=0:����
            //-1:��bContainDeletedΪfalseʱ:��ʾ����������trueʱ��ʾ�����ĸ�ֵ
            lBigOffset = GetBigOffsetA(nIndex, bContainDeleted);

            //��bContainDeletedΪfalseʱ����������ɾ����¼ʱ������ֵ-1����ʾû�ҵ�
            if (bContainDeleted == false)
            {
                if (lBigOffset == -1)
                    return null;
            }
            record = GetRecordByOffset(lBigOffset);

            if (this.m_streamSmall != null)
            {
                lPos = nIndex + 1;
            }
            else
            {
                lPos = m_streamBig.Position;	// Seek (0, SeekOrigin.Current);
            }
            Debug.Assert(lPos > 0, "�ļ�ָ�벻��ȷ");
            return record;
        }


        // ˳�εõ���һ����¼.��һ��ʹ�ñ�����֮ǰ������һ��GetFirstRecord()����
        // return:
        //		null	�ļ�����
        //		����	��¼����
        // lPos����С�ļ�ʱ����ʾ���������ţ�����С�ļ�ʱ��Ϊ���ļ���ƫ����
        public DpRecord GetNextRecord(
            ref long lPos)
        {
            if (lPos < 0)
                return null;	// ������ǰ���GetFirstRecord()����ʧ��

            if (this.m_streamSmall == null)
            {
                if (lPos >= m_streamBig.Length)
                    return null;	// ����
            }
            else
            {
                if (lPos >= this.Count)
                    return null;
            }

            DpRecord record = null;

            if (m_streamSmall != null)
            {
                // ����С�ļ�ʱ��lPos������ʾ��������
                long lDataPos = GetBigOffsetS(lPos, false);
                record = GetRecordByOffset(lDataPos);
                lPos++;
                return record;
            }



            // ���������ɾ����ǵļ�¼
            {

                //��4���ֽڣ��õ�����
                byte[] bufferLength = new byte[4];
                int n = m_streamBig.Read(bufferLength, 0, 4);
                if (n < 4)
                    return null;
                int nLength = System.BitConverter.ToInt32(bufferLength, 0);

                // ��ʾ��ɾ���ļ�¼
                if (nLength < 0)
                {
                    throw new Exception("Ŀǰ�����ܳ����������");
                    /*
                    // ��������ɾ���ļ�¼
                    lPos = this.GetNextOffsetOfBigFile(lPos);
                    if (lPos == -1)
                        return null;
                        */
                }
            }

            record = GetRecordByOffset(lPos);
            lPos = m_streamBig.Position;	// Seek (0, SeekOrigin.Current);
            Debug.Assert(lPos > 0, "�ļ�ָ�벻��ȷ");
            return record;
        }


        // ����汾
        public DpRecord GetRecordByOffsetEx(long lPos)
        {
            DpRecord record = null;

            if (lPos != m_streamBig.Position)
            {
                throw new Exception("error");
            }

            //�����ֽ�����
            byte[] bufferLength = new byte[4];
            int n = m_streamBig.Read(bufferLength, 0, 4);
            if (n < 4)   //��ʾ�ļ���β
            {
                throw (new Exception("�ڲ�����:Read error"));
                //return null;
            }

            int nLength = System.BitConverter.ToInt32(bufferLength, 0);

            string strID = ReadString(ref nLength);

            string strIndex = "-1";
            Debug.Assert(strID != null, "");
            int nPosition = strID.IndexOf(",");
            if (nPosition >= 0)
            {
                strIndex = strID.Substring(nPosition + 1);
                strID = strID.Substring(0, nPosition);
            }

            //������¼
            record = new DpRecord(strID);
            record.Index = Convert.ToInt32(strIndex);

            if (nLength <= 0)
                return record;

            /*
            record.m_strDom = ReadString(ref nLength);

            if (nLength <= 0)
                return record;
             * */

            record.BrowseText = ReadString(ref nLength);

            return record;
        }

        //����4�ֽڵõ��ĳ��ȣ������ַ�����ͬʱ�޸��ܳ���
        string ReadStringFromMemory(long lOffset,
            ref int nMaxLength)
        {
            Debug.Assert(this.m_bufferBig != null, "");

            byte[] bufferLength = new byte[4];
            byte[] bufferText;
            int nLength;

            // int n;

            //����length����
            Array.Copy(this.m_bufferBig,
    lOffset,
    bufferLength,
    0,
    4);

            nLength = System.BitConverter.ToInt32(bufferLength, 0);
            bufferText = new byte[nLength];

            lOffset += 4;

            /*
            try
            {
             * */
                Array.Copy(this.m_bufferBig,
        lOffset,
        bufferText,
        0,
        nLength);
            /*
            }
            catch (Exception ex)
            {
                int k = 0;
                k++;
            }
             * */

            if (4 + nLength > nMaxLength)
            {
                throw (new Exception("��ǰС���ĳ���(4+" + nLength.ToString() + ")���������Ƴ��� " + nMaxLength.ToString()));
            }

            nMaxLength = nMaxLength - (4 + nLength);

            return System.Text.Encoding.UTF8.GetString(bufferText);
        }

        //����4�ֽڵõ��ĳ��ȣ������ַ�����ͬʱ�޸��ܳ���
        string ReadString(ref int nMaxLength)
        {
            byte[] bufferLength = new byte[4];
            byte[] bufferText;
            int nLength;

            int n;

            //����ID
            n = m_streamBig.Read(bufferLength, 0, 4);
            if (n < 4)
            {
                throw (new Exception("�ڲ�����:�����ĳ���С��4"));
            }

            nLength = System.BitConverter.ToInt32(bufferLength, 0);
            bufferText = new byte[nLength];

            m_streamBig.Read(bufferText, 0, nLength);

            if (4 + nLength > nMaxLength)
            {
                throw (new Exception("��ǰС���ĳ���(4+"+nLength.ToString()+")���������Ƴ��� " + nMaxLength.ToString()));
            }

            nMaxLength = nMaxLength - (4 + nLength);

            return System.Text.Encoding.UTF8.GetString(bufferText);
        }


        //GetRecordByOffset()�����������������ҵ���¼������ʱ��ע�⣬�������Ҫ�õ���ɾ���ļ�¼���������ж�
        public DpRecord GetRecordByOffset(long lOffset)
        {
            DpRecord record = null;

            if (lOffset < 0)
            {
                lOffset = GetRealValue(lOffset);
            }

            if (lOffset >= m_streamBig.Length)
            {
                throw (new Exception("�ڲ�����λ�ô����ܳ���"));
                //return null;
            }

            // ������ļ��Ѿ����ڴ�
            if (this.m_bufferBig != null)
            {
                //�����ֽ�����
                byte[] bufferLength = new byte[4];
                /*
                try
                {
                 * */
                    Array.Copy(this.m_bufferBig,
                        lOffset,
                        bufferLength,
                        0,
                        4);
                /*
                }
                catch (Exception ex)
                {
                    int i = 0;
                    i++;
                }
                 * */

                int nLength = System.BitConverter.ToInt32(bufferLength, 0);

                lOffset += 4;
                int nOldLength = nLength;
                string strID = ReadStringFromMemory(lOffset, ref nLength);
                int nDelta = nOldLength - nLength;

                string strIndex = "-1";
                int nPosition = strID.LastIndexOf(","); // IndexOf BUG!!!
                if (nPosition >= 0)
                {
                    strIndex = strID.Substring(nPosition + 1);
                    strID = strID.Substring(0, nPosition);
                }

                //������¼
                record = new DpRecord(strID);

                record.Index = Convert.ToInt32(strIndex);   // �����׳��쳣


                if (nLength <= 0)
                    return record;

                lOffset += nDelta;
                record.BrowseText = ReadStringFromMemory(lOffset,
                    ref nLength);

                return record;
            }
            else
            {
                if (m_streamBig.Position != lOffset)    // 2012/2/15 ***
                    m_streamBig.Seek(lOffset, SeekOrigin.Begin);

                //�����ֽ�����
                byte[] bufferLength = new byte[4];
                int n = m_streamBig.Read(bufferLength, 0, 4);
                if (n < 4)   //��ʾ�ļ���β
                {
                    throw (new Exception("�ڲ�����:Read error"));
                    //return null;
                }

                int nLength = System.BitConverter.ToInt32(bufferLength, 0);


                string strID = ReadString(ref nLength);

                string strIndex = "-1";
                int nPosition = strID.LastIndexOf(","); // IndexOf BUG!!! �ٶ�?
                if (nPosition >= 0)
                {
                    strIndex = strID.Substring(nPosition + 1);
                    strID = strID.Substring(0, nPosition);
                }

                //������¼
                record = new DpRecord(strID);

                record.Index = Convert.ToInt32(strIndex);   // �����׳��쳣

                if (nLength <= 0)
                    return record;

                /*
                record.m_strDom = ReadString(ref nLength);
                if (nLength <= 0)
                    return record;
                 * */

                record.BrowseText = ReadString(ref nLength);

                return record;
            }
        }


        //��*8�ķ����㵽С�ļ���λ�ã�������ɾ���ļ�¼����ȡ�����ļ��ı�����
        public long GetBigOffsetBySmall(long nIndex)
        {
            if (m_streamSmall == null)
            {
                throw (new Exception("m_streamSmall����Ϊ��"));
            }

            if (nIndex * 8 >= m_streamSmall.Length || nIndex < 0)
            {
                throw (new Exception("�±�Խ��"));
            }

            byte[] bufferOffset = new byte[8];

            // ���С�ļ��Ѿ����ڴ�
            if (this.m_bufferSmall != null)
            {
                Array.Copy(this.m_bufferSmall,
                    nIndex * 8,
                    bufferOffset,
                    0,
                    8);
                return System.BitConverter.ToInt64(bufferOffset, 0);
            }

            if (m_streamSmall.Position != nIndex * 8)    // 2012/2/15 ***
                m_streamSmall.Seek(nIndex * 8, SeekOrigin.Begin);

            int n = m_streamSmall.Read(bufferOffset, 0, 8);
            if (n <= 0)
            {
                throw (new Exception("ʵ�����ĳ���" + Convert.ToString(m_streamSmall.Length) + "\r\n"
                    + "ϣ��Seek����λ��" + Convert.ToString(nIndex * 8) + "\r\n"
                    + "ʵ�ʶ��ĳ���" + Convert.ToString(n)));
            }
            long lOffset = System.BitConverter.ToInt64(bufferOffset, 0);

            return lOffset;
        }

        public int CreateSmallFile()
        {
            int nLength;

            Debug.Assert(this.m_streamBig != null, "");

            CreateIndex();
            if (m_streamBig.Position != 0)  // 2012/2/15 ***
                m_streamBig.Seek(0, SeekOrigin.Begin);
            m_streamSmall.Seek(0, SeekOrigin.End);

            int i = 0;
            long lPosition = 0;
            int nDeleteCount = 0;
            for (i = 0; ; i++)
            {
                //�����ֽ�����
                byte[] bufferLength = new byte[4];
                int n = m_streamBig.Read(bufferLength, 0, 4);
                if (n < 4)   //��ʾ�ļ���β
                    break;


                nLength = System.BitConverter.ToInt32(bufferLength, 0);
                if (nLength < 0)  //ɾ����
                {
                    nDeleteCount++;
                    nLength = (int)GetRealValue(nLength);
                    goto CONTINUE;
                }

                byte[] bufferOffset = new byte[8];
                bufferOffset = System.BitConverter.GetBytes((long)lPosition);
                Debug.Assert(bufferOffset.Length == 8, "");

                m_streamSmall.Write(bufferOffset, 0, 8);

            CONTINUE:

                m_streamBig.Seek(nLength, SeekOrigin.Current);
                lPosition += (4 + nLength);
            }

            return 0;
        }


        //����
        public void Sort()
        {
            if (this.Count <= 1)
            {
                this.Sorted = true;
                Debug.WriteLine("QuickSort() ��ʱ 0 (�Ż�)");
                return;
            }

            DateTime start_time = DateTime.Now;

            CreateSmallFile();

            TimeSpan delta = DateTime.Now - start_time;
            Debug.WriteLine("CreateSmallFile() ��ʱ " + delta.ToString());

            start_time = DateTime.Now;

            ReadToMemory();

            try
            {

                // QuickSort();
                QuickSort1(0, this.Count - 1);
                WriteToDisk(true, false);

                this.Sorted = true;

            }
            finally
            {


                this.m_bufferSmall = null;
                this.m_bufferBig = null;

            }


            delta = DateTime.Now - start_time;
            Debug.WriteLine("QuickSort() ��ʱ " + delta.ToString());
        }

        //����:�г������е�������
        //����ֵ: ���ؼ��ϳ�Ա��ɵı���ַ���
        public string Dump()
        {
            string strTable = "";

            strTable = "<table border='1'><tr><td>id</td></tr>";
            // TODO: foreach�ٶ�����ע�����ΪGetFirstRecord/GetNextRecord
            foreach (DpRecord eachRecord in this)
            {
                strTable += "<tr><td>" + eachRecord.ID + "</td></tr>";
            }
            strTable += "</table>";
            return strTable;
        }

        public string DumpAll()
        {
            if (m_streamSmall == null)
            {
                throw (new Exception("С�ļ�������"));
            }
            string strResult = "";
            int nm_count = (int)(m_streamSmall.Length / 8);
            for (int i = 0; i < nm_count; i++)
            {
                //strResult += "��ַ:"+Convert.ToString (GetOffset(i))+"\r\n";
                DpRecord record = GetRecord(i, true);
                strResult += record.ID + "\r\n";
                // strResult += record.m_strDom + "\r\n";
                strResult += record.BrowseText + "\r\n\r\n";
            }
            return strResult;
        }

        public int CompressIndex()
        {
            if (this.m_streamSmall == null)
                return 0;

            if (this.bDirty == false)
                return 0;

            this.Compress(this.m_streamSmall);
            return 1;
        }

        /*
		private int Compress(Stream oStream)
		{
			if (oStream == null)
			{
				return -1;
			}
			long lDeletedStart = 0;  //ɾ�������ʼλ��
			long lDeletedEnd = 0;    //ɾ����Ľ���λ��

			long lDeletedLength = 0;  //ɾ������
			bool bDeleted = false;   //�Ƿ��ѳ���ɾ����

			long lUseablePartLength = 0;    //����������ĳ���
			bool bUseablePart = false;    //�Ƿ��ѳ���������

			bool bEnd = false;
			long lValue = 0;

			oStream.Seek (0,SeekOrigin.Begin);
			while(true)
			{
				int nRet;
				byte[] bufferValue = new byte[8];
				nRet = oStream.Read(bufferValue,0,8);
				if (nRet != 8 && nRet != 0)  
				{
					throw(new Exception ("�ڲ�����:�����ĳ��Ȳ�����8"));
					//break;
				}
				if (nRet == 0)//��ʾ����
				{
					if(bUseablePart == false)
						break;

					lValue = -1;
					bEnd = true;
					//break;
				}

				if (bEnd != true)
				{
					lValue = BitConverter.ToInt64(bufferValue,0);
				}

				if (lValue < 0)
				{
					if (bDeleted == true && bUseablePart == true)
					{
						lDeletedEnd = lDeletedStart + lDeletedLength;
						//��MovePart(lDeletedStart,lDeletedEnd,lUseablePartLength)

						StreamUtil.Move(oStream,
							lDeletedEnd,
							lUseablePartLength,
							lDeletedStart);

						//���¶�λdeleted����ʼλ��
						lDeletedStart = lUseablePartLength-lDeletedLength+lDeletedEnd;
						lDeletedEnd = lDeletedStart+lDeletedLength;

						oStream.Seek (lDeletedEnd+lUseablePartLength,SeekOrigin.Begin);
					}

					bDeleted = true;
					bUseablePart = false;
					lDeletedLength += 8;  //����λ�ü�8
				}
				else if (lValue >= 0)
				{
					//�����ֹ�ɾ����ʱ���ֽ����µ����ÿ�ʱ��ǰ�������ÿ鲻�ƣ����¼��㳤��
					//|  useable  | ........ |  userable |
					//|  ........  | useable |
					if (bDeleted == true && bUseablePart == false)
					{
						lUseablePartLength = 0;
					}

					bUseablePart = true;
					lUseablePartLength += 8;
					
					if (bDeleted == false)
					{
						lDeletedStart += 8;  //��������ɾ����ʱ��ɾ����ʼλ�ü�8
					}
				}

				if (bEnd == true)
				{
					break;
				}
			}

			//ֻʣβ���ı�ɾ����¼
			if (bDeleted == true && bUseablePart == false)
			{
				//lDeletedEnd = lDeletedStart + lDeletedLength;
				oStream.SetLength(lDeletedStart);
			}

			bDirty = false;
			return 0;
		}
         */

        // ѹ������
        private int Compress(Stream oStream)
        {
            if (oStream == null)
            {
                return -1;
            }

            int nRet;
            long lRestLength = 0;
            long lDeleted = 0;
            long lCount = 0;

            if (oStream.Position != 0)  // 2012/2/15 ***
                oStream.Seek(0, SeekOrigin.Begin);
            lCount = oStream.Length / 8;
            for (long i = 0; i < lCount; i++)
            {
                byte[] bufferValue = new byte[8];
                nRet = oStream.Read(bufferValue, 0, 8);
                if (nRet != 8 && nRet != 0)
                {
                    throw (new Exception("�ڲ�����:�����ĳ��Ȳ�����8"));
                }

                long lValue = BitConverter.ToInt64(bufferValue, 0);

                if (nRet == 0)//��ʾ����
                {
                    break;
                }

                if (lValue < 0)
                {
                    // ��ʾ��Ҫɾ������Ŀ
                    lRestLength = oStream.Length - oStream.Position;

                    Debug.Assert(oStream.Position - 8 >= 0, "");


                    long lSavePosition = oStream.Position;

                    StreamUtil.Move(oStream,
                        oStream.Position,
                        lRestLength,
                        oStream.Position - 8);

                    if (oStream.Position != lSavePosition - 8)  // 2012/2/15 ***
                        oStream.Seek(lSavePosition - 8, SeekOrigin.Begin);

                    lDeleted++;
                }
            }

            if (lDeleted > 0)
            {
                oStream.SetLength((lCount - lDeleted) * 8);
            }

            bDirty = false;
            return 0;
        }

        //����4�ֽڵõ��ĳ��ȣ������ַ�����ͬʱ�޸��ܳ���
        void WriteString(string strText, ref int nMaxLength)
        {
            byte[] bufferLength = new byte[4];
            byte[] bufferText;

            bufferText = Encoding.UTF8.GetBytes(strText);

            bufferLength = System.BitConverter.GetBytes((Int32)bufferText.Length);

            Debug.Assert(bufferLength.Length == 4, "");
            m_streamBig.Write(bufferLength, 0, 4);

            m_streamBig.Write(bufferText, 0, bufferText.Length);

            nMaxLength += 4;
            nMaxLength += bufferText.Length;
        }

        public void WriteBuffer(ByteList target,
            byte[] source,
            ref int lLength)
        {
            target.AddRange(source);
            //lLength += source.Length ;
        }


        public void WriteBuffer(ByteList target,
            string strSource,
            ref int lLength)
        {
            byte[] bufferLength = new byte[4];
            byte[] bufferText;
            bufferText = Encoding.UTF8.GetBytes(strSource);

            bufferLength = System.BitConverter.GetBytes((Int32)bufferText.Length);
            Debug.Assert(bufferLength.Length == 4, "");

            target.AddRange(bufferLength);
            target.AddRange(bufferText);

            lLength += (4 + bufferText.Length);
        }

        public void ReplaceBuffer(ByteList aBuffer,
            int lStart,
            byte[] buffer)
        {
            int lEnd = lStart + buffer.Length;
            for (int i = lStart; i < lEnd; i++)
            {
                aBuffer[i] = buffer[i - lStart];
            }
        }


        //ȷ������ָ��ŵ�ǡ��λ��
        public virtual void Add(DpRecord record)  //virtual
        {
            m_streamBig.Seek(0, SeekOrigin.End);

            if (m_streamSmall != null)
            {
                m_streamSmall.Seek(0, SeekOrigin.End);
                long nPosition = m_streamBig.Position;

                byte[] bufferPosition = new byte[8];
                bufferPosition = System.BitConverter.GetBytes((long)nPosition);   // ԭ��ȱ��(long), ��һ��bug. 2006/10/1 �޸�

                Debug.Assert(bufferPosition.Length == 8, "");
                m_streamSmall.Write(bufferPosition, 0, 8);
            }

            ByteList aBuffer = new ByteList(4096);

            int nLength = 0;
            byte[] bufferLength = new byte[4];

            //ռλ4�ֽڣ�����д�ܳ���
            //m_streamBig.Write(bufferLength,0,4);
            WriteBuffer(aBuffer, bufferLength, ref nLength);

            //дID
            //WriteString(record.ID,ref nLength);
            WriteBuffer(aBuffer, record.ID + "," + Convert.ToString(record.Index), ref nLength);

            //д���
            //WriteBuffer(aBuffer,"," + Convert.ToString (record.Index ),ref nLength);

            //дm_strDom
            //WriteString(record.m_strDom,ref nLength);

            // дBrowseText
            // WriteString(record.BrowseText,ref nLength);
            if (String.IsNullOrEmpty(record.BrowseText) == false)
            {
                WriteBuffer(aBuffer, record.BrowseText, ref nLength);
            }

            //д�ܳ���
            bufferLength = System.BitConverter.GetBytes((Int32)nLength);    // 4bytes!
            Debug.Assert(bufferLength.Length == 4, "");
            ReplaceBuffer(aBuffer, 0, bufferLength);

            //m_streamBig.Seek (-(nLength+4),SeekOrigin.Current  );
            byte[] bufferAll = new byte[aBuffer.Count];
            /*
            for (int i = 0; i < aBuffer.Count; i++)
            {
                bufferAll[i] = (byte)aBuffer[i];
            }
             * */
            // 2010/5/17
            aBuffer.CopyTo(bufferAll);

            m_streamBig.Write(bufferAll, 0, bufferAll.Length);
            m_count++;
        }




        // 2011/1/1
        // �������ļ���ֱ��������������ƫ������
        //	��Ȼ�������ٶȺ���
        // return:
        //		-1	��bContainDeletedΪfalseʱ-1��ʾ����������bContainDeletedΪtrueʱ��ʾ�����ĸ�ֵ
        long GetDataOffsetFromDataFile(long nIndex, bool bContainDeleted)
        {
            if (m_streamBig.Position != 0)  // 2012/2/15 ***
                m_streamBig.Seek(0, SeekOrigin.Begin);
            long lBigOffset = 0;

            int nLength;
            int i = 0;
            while (true)
            {
                //��4���ֽڣ��õ�����
                byte[] bufferLength = new byte[4];
                int n = m_streamBig.Read(bufferLength, 0, 4);
                if (n < 4)   //��ʾ�ļ���β
                    break;
                nLength = System.BitConverter.ToInt32(bufferLength, 0);

                if (bContainDeleted == false)
                {
                    if (nLength < 0)
                    {
                        //ת��Ϊʵ�ʵĳ��ȣ���seek
                        long lTemp = GetRealValue(nLength);
                        m_streamBig.Seek(lTemp, SeekOrigin.Current);

                        lBigOffset += (4 + lTemp);
                        continue;
                    }
                }

                if (i == nIndex)
                {
                    return lBigOffset;
                }
                else
                {
                    m_streamBig.Seek(nLength, SeekOrigin.Current);
                }

                lBigOffset += (4 + nLength);

                i++;
            }

            return -1;
        }


        // 2011/1/1
        // �Զ�ѡ��Ӻδ�ɾ��
        int RemoveAtAuto(int nIndex)
        {
            int nRet = -1;
            if (m_streamSmall != null) // �������ļ�ʱ
            {
                // nRet = RemoveAtIndex(nIndex);
                nRet = CompressRemoveAtIndex(nIndex, 1);
            }
            else  // �����ļ�������ʱ�� �������ļ���ɾ��
            {
                nRet = RemoveAtData(nIndex);
            }
            return nRet;
        }

        // 2011/1/1
        //�Ӵ��ļ���ɾ��
        public int RemoveAtData(int nIndex)
        {
            //�õ����ļ�ƫ����
            long lBigOffset = GetDataOffsetFromDataFile(nIndex, false);
            if (lBigOffset == -1)
                return -1;

            if (lBigOffset >= m_streamBig.Length)
            {
                throw (new Exception("�ڲ�����λ�ô����ܳ���"));
                //return null;
            }

            if (m_streamBig.Position != lBigOffset)  // 2012/2/15 ***
                m_streamBig.Seek(lBigOffset, SeekOrigin.Begin);
            //�����ֽ�����
            byte[] bufferLength = new byte[4];
            int n = m_streamBig.Read(bufferLength, 0, 4);
            if (n < 4)   //��ʾ�ļ���β
            {
                throw (new Exception("�ڲ�����:Read error"));
                //return null;
            }

            int nLength = System.BitConverter.ToInt32(bufferLength, 0);
            nLength = (int)GetDeletedValue(nLength);

            bufferLength = BitConverter.GetBytes((Int32)nLength);
            m_streamBig.Seek(-4, SeekOrigin.Current);
            Debug.Assert(bufferLength.Length == 4);
            m_streamBig.Write(bufferLength, 0, 4);

            return 0;
        }

        // 2011/1/1
        // ���ɾ��һ����¼
        public void RemoveAt(int nIndex)
        {
            if (nIndex < 0 || nIndex >= m_count)
            {
                throw (new Exception("�±� " + Convert.ToString(nIndex) + " Խ��(Count=" + Convert.ToString(m_count) + ")"));
            }
            int nRet = RemoveAtAuto(nIndex);
            if (nRet == -1)
            {
                throw (new Exception("RemoveAtAuto fail"));
            }

            m_count--;
            // bDirty = true;	// ��ʾ�Ѿ��б��ɾ����������

        }

        // 2011/1/1
        //���ɾ��������¼
        public void RemoveAt(int nIndex,
            int nCount)
        {

                if (nIndex < 0 || nIndex + nCount > m_count)
                {
                    throw (new Exception("�±� " + Convert.ToString(nIndex) + " Խ��(Count=" + Convert.ToString(m_count) + ")"));
                }

                int nRet = 0;
                if (m_streamSmall != null) // �������ļ�ʱ
                {
                    // nRet = RemoveAtIndex(nIndex);
                    nRet = CompressRemoveAtIndex(nIndex, nCount);
                }
                else
                {
                    throw (new Exception("��ʱ��û�б�д"));

                }


                if (nRet == -1)
                {
                    throw (new Exception("RemoveAtAuto fail"));
                }

                m_count -= nCount;
                // bDirty = true;	// ��ʾ�Ѿ��б��ɾ����������
        }

        // 2011/1/1
        // �������ļ��м�ѹʽɾ��һ������
        public int CompressRemoveAtIndex(int nIndex,
            int nCount)
        {
            if (m_streamSmall == null)
                throw new Exception("�����ļ���δ��ʼ��");

            long lStart = (long)nIndex * 8;
            StreamUtil.Move(m_streamSmall,
                    lStart + 8 * nCount,
                    m_streamSmall.Length - lStart - 8 * nCount,
                    lStart);

            m_streamSmall.SetLength(m_streamSmall.Length - 8 * nCount);

            return 0;
        }

        // 2011/1/1
        // ����һ������
        public virtual void Insert(int nIndex,
            DpRecord record)
        {

            // �������������ļ�
            if (m_streamSmall == null)
                throw (new Exception("�ݲ�֧���������ļ���ʽ�µĲ������"));


            // �������ļ�ָ������β��
            m_streamBig.Seek(0,
                SeekOrigin.End);

            // �����������ļ�
            if (m_streamSmall != null)
            {
                // ����һ����index��Ŀ
                long lStart = (long)nIndex * 8;
                StreamUtil.Move(m_streamSmall,
                    lStart,
                    m_streamSmall.Length - lStart,
                    lStart + 8);

                if (m_streamSmall.Position != lStart)  // 2012/2/15 ***
                    m_streamSmall.Seek(lStart, SeekOrigin.Begin);
                long nPosition = m_streamBig.Position;

                byte[] bufferPosition = new byte[8];
                bufferPosition = System.BitConverter.GetBytes((long)nPosition); // ԭ��ȱ��(long), ��һ��bug. 2006/10/1 �޸�
                Debug.Assert(bufferPosition.Length == 8, "");
                m_streamSmall.Write(bufferPosition, 0, 8);
            }

            /*
                byte[] bufferLength = System.BitConverter.GetBytes((Int32)item.Length);
                Debug.Assert(bufferLength.Length == 4, "");
                m_streamBig.Write(bufferLength, 0, 4);

                item.WriteData(m_streamBig);
                m_count++;             * */

            ByteList aBuffer = new ByteList(4096);

            int nLength = 0;
            byte[] bufferLength = new byte[4];

            //ռλ4�ֽڣ�����д�ܳ���
            //m_streamBig.Write(bufferLength,0,4);
            WriteBuffer(aBuffer, bufferLength, ref nLength);

            //дID
            //WriteString(record.ID,ref nLength);
            WriteBuffer(aBuffer, record.ID + "," + Convert.ToString(record.Index), ref nLength);

            // дBrowseText
            // WriteString(record.BrowseText,ref nLength);
            if (String.IsNullOrEmpty(record.BrowseText) == false)
            {
                WriteBuffer(aBuffer, record.BrowseText, ref nLength);
            }

            //д�ܳ���
            bufferLength = System.BitConverter.GetBytes((Int32)nLength);    // 4bytes!
            Debug.Assert(bufferLength.Length == 4, "");
            ReplaceBuffer(aBuffer, 0, bufferLength);

            byte[] bufferAll = new byte[aBuffer.Count];
            aBuffer.CopyTo(bufferAll);

            m_streamBig.Write(bufferAll, 0, bufferAll.Length);
            m_count++;
        }


        /// <summary>
        /// ����
        /// </summary>
        /// <param name="left">�����һ��Ԫ������Index</param>
        /// <param name="right">�������һ��Ԫ������Index</param>
        void QuickSort1(
            long left,
            long right)
        {
            //�������С���ұߣ���δ�������
            if (left >= right)
                return;

            //ȡ�м��Ԫ����Ϊ�Ƚϻ�׼��С������������ƣ������������ұ���
            long i = left - 1;
            long j = right + 1;

            {
                // int middle = numbers[(left + right) / 2];
                long pMiddle = GetBigOffsetBySmall((left + right) / 2);   //GetRowPtr(nMiddle);
                while (true)
                {
                    // while (numbers[++i] < middle && i < right) ;
                    while (i < right)
                    {
                        long pTemp = GetBigOffsetBySmall(++i);
                        int nRet = this.Asc * Compare(pTemp, pMiddle);
                        if (nRet >= 0)
                            break;
                    }


                    // while (numbers[--j] > middle && j > 0) ;
                    while (j > 0)
                    {
                        long pTemp = GetBigOffsetBySmall(--j);
                        int nRet = this.Asc * Compare(pTemp, pMiddle);
                        if (nRet <= 0)
                            break;
                    }


                    if (i >= j)
                        break;
                    // Swap(numbers, i, j);
                    {
                        long pTemp_i = GetBigOffsetBySmall(i);
                        long pTemp_j = GetBigOffsetBySmall(j);
                        SetRowPtr(j, pTemp_i);
                        SetRowPtr(i, pTemp_j);
                    }

                }
            }
            QuickSort1(left, i - 1);
            QuickSort1(j + 1, right);

        }

        void Push(List<long> stack,
            long lStart,
            long lEnd,
            ref int nStackTop)
        {
            if (nStackTop < 0)
            {
                throw (new Exception("nStackTop����С��0"));
            }
            if (lStart < 0)
            {
                throw (new Exception("nStart����С��0"));
            }

            if (nStackTop * 2 != stack.Count)
            {
                throw (new Exception("nStackTop*2������stack.m_count"));
            }

            stack.Add(lStart);
            stack.Add(lEnd);

            nStackTop++;
        }

        void Pop(List<long> stack,
            ref long lStart,
            ref long lEnd,
            ref int nStackTop)
        {
            if (nStackTop <= 0)
            {
                throw (new Exception("pop��ǰ,nStackTop����С�ڵ���0"));
            }

            if (nStackTop * 2 != stack.Count)
            {
                throw (new Exception("nStackTop*2������stack.m_count"));
            }

            lStart = (long)stack[(nStackTop - 1) * 2];
            lEnd = (long)stack[(nStackTop - 1) * 2 + 1];

            stack.RemoveRange((nStackTop - 1) * 2, 2);

            nStackTop--;
        }

        // ��������
        // �����ʾ�������? ͷ�۵����顣�ɷ��ö�ջ��ȱ�ʾ����?
        // ��Ҫ�����ȫ����Ĳ����У�item������������Щ���ִ���item
        // ������ȥ�������ǽ���ָʾ�����ݡ�
        // return:
        //  0 succeed
        //  1 interrupted
        public int QuickSort()
        {
            List<long> stack = new List<long>(); // ��ջ
            int nStackTop = 0;
            long nMaxRow = m_streamSmall.Length / 8;  //m_count;
            long k = 0;
            long j = 0;
            long i = 0;

            if (nMaxRow == 0)
                return 0;

            /*
            if (nMaxRow >= 10) // ����
             nMaxRow = 10;
            */

            Push(stack, 0, nMaxRow - 1, ref nStackTop);
            while (nStackTop > 0)
            {
                Pop(stack, ref k, ref j, ref nStackTop);
                while (k < j)
                {
                    Split(k, j, ref i);
                    Push(stack, i + 1, j, ref nStackTop);
                    j = i - 1;
                }
            }


            return 0;
        }


        void Split(long nStart,
            long nEnd,
            ref long nSplitPos)
        {
            // ȡ������
            long pStart = 0;
            long pEnd = 0;
            long pMiddle = 0;
            long pSplit = 0;
            long nMiddle;
            long m, n, i, j, k;
            long T = 0;
            int nRet;
            long nSplit;


            nMiddle = (nStart + nEnd) / 2;

            pStart = GetBigOffsetBySmall(nStart);
            pEnd = GetBigOffsetBySmall(nEnd);

            // �������յ��Ƿ��������
            if (nStart + 1 == nEnd)
            {
                nRet = this.Asc * Compare(pStart, pEnd);
                if (nRet > 0)
                { // ����
                    T = pStart;
                    SetRowPtr(nStart, pEnd);
                    SetRowPtr(nEnd, T);
                }
                nSplitPos = nStart;
                return;
            }


            pMiddle = GetBigOffsetBySmall(nMiddle);   //GetRowPtr(nMiddle);

            nRet = this.Asc * Compare(pStart, pEnd);
            if (nRet <= 0)
            {
                nRet = this.Asc * Compare(pStart, pMiddle);
                if (nRet <= 0)
                {
                    pSplit = pMiddle;
                    nSplit = nMiddle;
                }
                else
                {
                    pSplit = pStart;
                    nSplit = nStart;
                }
            }
            else
            {
                nRet = this.Asc * Compare(pEnd, pMiddle);
                if (nRet <= 0)
                {
                    pSplit = pMiddle;
                    nSplit = nMiddle;
                }
                else
                {
                    pSplit = pEnd;
                    nSplit = nEnd;
                }

            }

            // 
            k = nSplit;
            m = nStart;
            n = nEnd;

            T = GetBigOffsetBySmall(k);
            // (m)-->(k)
            SetRowPtr(k, GetBigOffsetBySmall(m));
            i = m;
            j = n;
            while (i != j)
            {
                // Thread.Sleep(0);
                while (true)
                {
                    nRet = this.Asc * Compare(GetBigOffsetBySmall(j), T);
                    if (nRet >= 0 && i < j)
                        j = j - 1;
                    else
                        break;
                }
                if (i < j)
                {
                    // (j)-->(i)
                    SetRowPtr(i, GetBigOffsetBySmall(j) /*GetRowPtr(j)*/);
                    i = i + 1;
                    while (true)
                    {
                        nRet = this.Asc * Compare(/*GetRowPtr(i)*/ GetBigOffsetBySmall(i), T);
                        if (nRet <= 0 && i < j)
                            i = i + 1;
                        else
                            break;
                    }
                    if (i < j)
                    {
                        // (i)--(j)
                        SetRowPtr(j, GetBigOffsetBySmall(i) /*GetRowPtr(i)*/);
                        j = j - 1;
                    }
                }
            }
            SetRowPtr(i, T);
            nSplitPos = i;
        }


        public void SetRowPtr(long nIndex, long lPtr)
        {
            byte[] bufferOffset;

            //�õ�ֵ
            bufferOffset = new byte[8];
            bufferOffset = BitConverter.GetBytes((long)lPtr);

            // ���С�ļ��Ѿ����ڴ�
            if (this.m_bufferSmall != null)
            {
                Array.Copy(bufferOffset,
                    0,
                    this.m_bufferSmall,
                    nIndex * 8,
                    8);
                return;
            }

            //����ֵ
            if (m_streamSmall.Position != nIndex * 8)  // 2012/2/15 ***
                m_streamSmall.Seek(nIndex * 8, SeekOrigin.Begin);

            Debug.Assert(bufferOffset.Length == 8, "");
            m_streamSmall.Write(bufferOffset, 0, 8);

        }

        public int Compare(long lPtr1, long lPtr2)
        {
            if (lPtr1 < 0 && lPtr2 < 0)
                return 0;
            else if (lPtr1 >= 0 && lPtr2 < 0)
                return 1;
            else if (lPtr1 < 0 && lPtr2 >= 0)
                return -1;

            if (m_nLoopCount++ % 1000 == 0)
            {
                Thread.Sleep(1);
                if (this.Idle != null)
                {
                    IdleEventArgs e = new IdleEventArgs();
                    this.Idle(this, e);
                }
            }

            DpRecord record1 = GetRecordByOffset(lPtr1);
            DpRecord record2 = GetRecordByOffset(lPtr2);

            return record1.CompareTo(record2);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new DpResultSetEnumerator(this);
        }
    }


    public class DpResultSetEnumerator : IEnumerator
    {
        DpResultSet m_resultSet = null;
        long m_index = -1;

        public DpResultSetEnumerator(DpResultSet resultSet)
        {
            m_resultSet = resultSet;
        }

        public void Reset()
        {
            m_index = -1;
        }
        public bool MoveNext()
        {
            m_index++;
            if (m_index >= m_resultSet.Count)
                return false;
            return true;
        }
        public object Current
        {
            get
            {
                return (object)m_resultSet[m_index];
            }
        }
    }


    //�����ͼ:�����������¼�����ͣ���ΪDpResultSet�ĳ�Ա
    [Serializable]
    public class DpRecord : IComparable
    {
        public int Index = 0;
        public string m_strDebugInfo = "";

        //˽���ֶγ�Ա����ż�¼���߼�����ID����ʽΪ:"ͼ���:0000000001"
        public string m_id;

        //˽���ֶγ�Ա����ż�¼�����HTML
        string m_strBrowseText = "";

        //�����ֶγ�Ա����ż�¼��Ӧ������dom
        //public XmlDocument m_dom = null;
        // public string m_strDom = "";

        public DpRecord()
        {
        }

        //��Ĭ�Ϲ��캯������m_id��ֵ
        //myid: ���ص������߼�ID����
        public DpRecord(string myid)
        {
            m_id = myid;
        }

        //����ID���ԣ���ʾ��¼�����߼�ID���ṩ���ⲿ�������
        public string ID
        {
            get
            {
                return m_id;
            }
        }


        //����BrowseText���ԣ���ʾ��¼���HTML�ı����ṩ���ⲿ�������
        public string BrowseText
        {
            get
            {
                return m_strBrowseText;
            }
            set
            {
                m_strBrowseText = value;
            }
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
        public int CompareTo(object obj)
        {
            DpRecord myRecord = (DpRecord)obj;

            //m_strDebugInfo += strID1 + "---------" + strID2;

            //ͨ��String��ľ�̬����Compare�Ƚ������ַ����Ĵ�С������ֵΪС��0������0������0
            return String.Compare(this.ID, myRecord.ID);
        }

    }//end of class DpRecord

    public delegate void GetTempFilenameEventHandler(object sender,
            GetTempFilenameEventArgs e);

    public class GetTempFilenameEventArgs : EventArgs
    {
        public string TempFilename = "";
    }

    public class ByteList : List<byte>  // ArrayList   // List<byte>
    {
        public ByteList(int nCapacity)
        {
            this.Capacity = nCapacity;
        }
    }

    public enum LogicOper
    {
        OR = 0, // 
        AND = 1,
        SUB = 2,
    }
}
