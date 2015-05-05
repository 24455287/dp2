using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using DigitalPlatform;

namespace DigitalPlatform.Range
{
	/// <summary>
	/// RangeItem��RangeList���ϵĳ�Ա����ʾһ�������ķ�Χ
	/// </summary>
	public class RangeItem : IComparable
	{
		public long lStart = 0;	// ���Ϊ-1����ʾ��β�˳���ΪlLength��һ�Σ���Ϊ�ܳ���δ֪������lStartδ֪������-1����������״̬
		public long lLength = 0;	// ���Ϊ-1����ʾ��lStart��ʼһֱ��ĩβ��
		#region IComparable Members

		// ���thisС��obj������<0��ֵ
		public int CompareTo(object obj)
        {
            RangeItem item = (RangeItem)obj;
            /*
			if (this.lStart == item.lStart)
				return (int)(this.lLength - item.lLength);
			return (int)(this.lStart - item.lStart);
             * */

            // 2012/8/26 �޸�
            if (this.lStart == item.lStart)
            {
                long lDelta = this.lLength - item.lLength;
                if (lDelta == 0)
                    return 0;
                if (lDelta < 0)
                    return -1;
                return 1;
            }
            {
                long lDelta = this.lStart - item.lStart;
                if (lDelta == 0)
                    return 0;
                if (lDelta < 0)
                    return -1;
                return 1;
            }
        }

		#endregion


		public RangeItem()
		{
		}

		public RangeItem(RangeItem item)
		{
			lStart = item.lStart;
			lLength = item.lLength;
		}

		// ƴ��Ϊ��ʾ��Χ���ַ���
		public string GetContentRangeString()
		{
            Debug.Assert(this.lStart >= 0, "");
            Debug.Assert(this.lStart + this.lLength - 1 >= 0, "");

			if (lLength == 1)
				return Convert.ToString(lStart);

			return Convert.ToString(lStart) + "-" + Convert.ToString(lStart+lLength-1);
		}
	}

	/// <summary>
	/// ��ʾ��Χ����
	/// </summary>
	public class RangeList : List<RangeItem>
	{
		public string delimeters = ",";	// �ָ����������г����
		public string contentRange = "";	// ���淶Χ�ַ���

		// ���캯��
		public RangeList(string strContentRange) 
		{
			BuildItems(strContentRange);
		}

		public RangeList()
		{

		}


		public RangeList(string strContentRange,
			string delemParam)
		{
			delimeters = delemParam;

			BuildItems(strContentRange);
		}

		// ������������
		public void BuildItems(string strContentRange)
		{
			long lStart = 0;
			long lLength = 0;
			string [] split = null;

			char [] delimChars = delimeters.ToCharArray();

			split = strContentRange.Split(delimChars);

			for(int i = 0; i< split.Length; i++) 
			{
				if (split[i] == "")
					continue;
				// ����-���
				int nRet = split[i].IndexOf("-");
				if (nRet == -1) 
				{
                    lStart = 0; //  Convert.ToInt64(split[i]);

                    if (Int64.TryParse(split[i], out lStart) == false)
                        throw new Exception("���ַ��� '" + strContentRange + "' ����RangeListʱ�������� '" + split[i].ToString() + "' ��ʽ����ȷ");

					lLength = 1;
				}
				else 
				{
					string left = split[i].Substring(0, nRet);
					string right = split[i].Substring(nRet + 1);
					left = left.Trim();
					right = right.Trim();

                    if (left == "")
                    {
                        lStart = -1;
                        try
                        {
                            lLength = Convert.ToInt64(right);
                        }
                        catch
                        {
                            throw new Exception("���ַ��� '" + strContentRange + "' ����RangeListʱ�������� '" + right + "' ��ʽ����ȷ");
                        }
                        goto CONTINUE;
                    }
                    else
                    {
                        try
                        {
                            lStart = Convert.ToInt64(left);
                        }
                        catch
                        {
                            throw new Exception("���ַ��� '" + strContentRange + "' ����RangeListʱ�������� '" + left + "' ��ʽ����ȷ");
                        }

                    }

					if (right == "") 
					{
						lLength = -1;
						// ��ʱlStart����Ϊ-1
						goto CONTINUE;
					}
					else 
					{
						long lEnd = 0;  // Convert.ToInt64(right);
                        if (Int64.TryParse(right, out lEnd) == false)
                            throw new Exception("���ַ��� '" + strContentRange + "' ����RangeListʱ�������� '" + right.ToString() + "' ��ʽ����ȷ");
						if (lStart > lEnd) 
						{
                            // TODO: �������� MaxValue �����
							lLength = (lStart - lEnd) + 1;
							lStart = lEnd;
						}
						else 
						{
                            // TODO: �������� MaxValue �����
                            lLength = (lEnd - lStart) + 1;
						}
					}

				}
			CONTINUE:
				RangeItem item = new RangeItem();
				item.lStart = lStart;
				item.lLength = lLength;
				this.Add(item);
			}

			contentRange = strContentRange;	// ��������
		}

		// ƴ��Ϊ��ʾ��Χ���ַ���
		public string GetContentRangeString() 
		{
			string strResult = "";

			for(int i=0; i<Count; i++) 
			{
				RangeItem item = (RangeItem)this[i];
				if (i!=0)
					strResult += ",";
				strResult += item.GetContentRangeString();
			}

			return strResult;
		}

		// ƴ��Ϊ��ʾ��Χ���ַ���
		public string GetContentRangeString(int nStart, int nCount) 
		{
			string strResult = "";

			for(int i=nStart; i<this.Count && i<nStart+nCount; i++) 
			{
				RangeItem item = (RangeItem)this[i];
				if (strResult != "")
					strResult += ",";
				strResult += item.GetContentRangeString();
			}

			return strResult;
		}

		// ������߽�
		// ��ν���߽磬�Ƿ�Χ�г��ֵ�������֡��ǰ������������
		public long max()
		{
			long lValue = 0;
			for(int i=0; i<Count; i++) 
			{
				RangeItem item = (RangeItem)this[i];
				if (item.lLength == -1)
					return -1;	// ��ʾ��ȷ�����൱�������
				if (item.lStart + item.lLength + -1> lValue)
					lValue = item.lStart + item.lLength - 1;
			}

			return lValue;
		}

		// �����С�߽�
		// ��ν��С�߽磬�Ƿ�Χ�г��ֵ���С���֡�����������֡�
		public long min()
		{
			long lValue = 0;
            bool bFirst = true;
			for(int i=0; i<Count; i++) 
			{
				RangeItem item = (RangeItem)this[i];
                if (bFirst == true)
                {
                    lValue = item.lStart;
                    bFirst = false;
                }
                else
                {
                    if (item.lStart < lValue)
                        lValue = item.lStart;
                }
			}

			return lValue;
		}


		// bIsOrdered	true��ʾRangeList��������ģ��㷨���Ż�
		public bool IsInRange(long lNumber, 
			bool bIsOrdered)
		{
			for(int i=0;i<this.Count;i++) 
			{
				RangeItem item = (RangeItem)this[i];
				if (item.lLength == -1) 
				{
					if (lNumber >= item.lStart)
						return true;
				}
                else if (item.lStart <= lNumber && item.lStart + item.lLength > lNumber)   // BUG!!! item.lStart + item.lLength >= lNumber
					return true;
				if (bIsOrdered == true) 
				{
					if (item.lStart > lNumber)
						break;
				}
			}
			return false;
		}

		// �ϲ��ص�������
		// Ҫ���������򡣷����ܱ�֤������ȷ�ԡ�
		public int Merge()
		{
			for(int i=0; i<this.Count; i++) 
			{
				RangeItem item1 = (RangeItem)this[i];
				if (item1.lLength == 0) 
				{
					this.RemoveAt(i);
					i --;
					continue;
				}

				for(int j=i+1;j<this.Count;j++) 
				{
					RangeItem item2 = (RangeItem)this[j];

					if (item2.lStart == item1.lStart + item1.lLength)
					{
						// ����
						item1.lLength += item2.lLength;
						this.RemoveAt(j);
						j --;
						continue;
					}
					else if (item2.lStart >= item1.lStart
						&& item2.lStart <= item1.lStart + item1.lLength - 1)
					{
						// ���ص�
						long end1 = item1.lStart + item1.lLength;
						long end2 = item2.lStart + item2.lLength;
						if (end1 <= end2)
							item1.lLength = end2 - item1.lStart;
						else
							item1.lLength = end1 - item1.lStart;

						// item1.lLength = item2.lStart + item2.lLength - item1.lStart;
						this.RemoveAt(j);
						j --;
						continue;
					}
					else 
					{
						break;	// û���ص�
					}
					
				}

			}
			return 0;

		}

		public static void CrossOper(RangeList source1,
			RangeList source2,
			RangeList targetLeft,
			RangeList targetMiddle,
			RangeList targetRight)
		{
			int i,j;

			RangeItem item1 = null;	// ��߶���
			RangeItem item2 = null;	// �ұ߶���

			RangeItem item = null;	// ��ʱ

			bool bFinished1 = false;
			bool bFinished2 = false;

			for(i=0,j=0;;) 
			{
				// ȡ����1
				if (item1 == null && bFinished1 == false
					&& i<source1.Count)
				{
					item1 = (RangeItem)source1[i];
					if (item1 == null) 
					{
						throw(new ArgumentException("source1������λ��"+Convert.ToString(i)+"(��0��ʼ����)������Ԫ��..."));
					}
					i++;
				}

				// ȡ����2
				if (item2 == null && bFinished2 == false
					&& j < source2.Count)
				{
					item2 = (RangeItem)source2[j];
					if (item2 == null) 
					{
						throw(new ArgumentException("source2������λ��"+Convert.ToString(j)+"(��0��ʼ����)������Ԫ��..."));
					}
					j ++;
				}

				if (item1 == null && item2 == null)
					break;	// ȫ�����������

				// �Ƚ�����Item
				if (item1 != null && item2 != null) 
				{
					// item1��ȫС��item2
					if (item1.lStart + item1.lLength <= item2.lStart)
					{
						item = new RangeItem(item1);
						if (targetLeft != null)
							targetLeft.Add(item);
						item1 = null;	// Ϊ����1ȡ��һ����׼��
						continue;
					}
					// item2��ȫС��item1
					if (item2.lStart + item2.lLength <= item1.lStart)
					{
						item = new RangeItem(item2);
						if (targetRight != null)
							targetRight.Add(item);
						item2 = null;	// Ϊ����2ȡ��һ����׼��
						continue;
					}
					// item1��item2�����ص�

					// item1��ǰ
					if (item1.lStart <= item2.lStart) 
					{
						// |item1     |
						//        |item2      |
						// |  A   | B |   C   |

						// item1 A����ȥtargetLeft
						if (item1.lStart != item2.lStart) 
						{
							item = new RangeItem();
							item.lStart = item1.lStart;
							item.lLength = item2.lStart - item1.lStart;
							if (targetLeft != null)
								targetLeft.Add(item);
						}

						// item1��item2�ص��Ĳ���B��ȥtargetMiddle
						item = new RangeItem();
						item.lStart = item2.lStart;

						/*
						long end1 = item1.lStart + item1.lLength;
						long end2 = item2.lStart + item2.lLength;

						if (end1 <= end2) 
						{
							item.lLength = end1 - item.lStart;
						}
						else 
						{
							item.lLength = end2 - item.lStart;
						}
						if (targetMiddle != null)
							targetMiddle.Add(item);

						// item2��Item2���ص���C���֣����������´�ѭ��

						if (end1 <= end2) 
						{
							if (end1 == end2) 
							{
								item1 = null;
								item2 = null;
								continue;
							}
							item2.lStart = end1;
							item2.lLength = end2 - end1;
							item1 = null;
							continue;
						}
						else 
						{
							item1.lStart = end2;
							item1.lLength = end1 - end2;
							item2 = null;
							continue;
						}
						*/


					} // item1��ǰ

					// item2��ǰ
					else // if (item1.lStart > item2.lStart) 
					{
						// |item2     |
						//        |item1      |
						// |  A   | B |   C   |

						// item2 A����ȥtargetRight
						item = new RangeItem();
						item.lStart = item2.lStart;
						item.lLength = item1.lStart - item2.lStart;
						if (targetRight != null)
							targetRight.Add(item);

						// item1��item2�ص��Ĳ���B��ȥtargetMiddle
						item = new RangeItem();
						item.lStart = item1.lStart;
						/*
						long end1 = item1.lStart + item1.lLength;
						long end2 = item2.lStart + item2.lLength;

						if (end1 <= end2) 
						{
							item.lLength = end1 - item.lStart;
						}
						else 
						{
							item.lLength = end2 - item.lStart;
						}
						if (targetMiddle != null)
							targetMiddle.Add(item);

						// item2��Item2���ص���C���֣����������´�ѭ��

						if (end1 <= end2) 
						{
							if (end1 == end2) 
							{
								item1 = null;
								item2 = null;
								continue;
							}
							item2.lStart = end1;
							item2.lLength = end2 - end1;
							item1 = null;
							continue;
						}
						else 
						{
							item1.lStart = end2;
							item1.lLength = end1 - end2;
							item2 = null;
							continue;
						}
						*/


					} // item2��ǰ

					if (true)
					{ // C����
						long end1 = item1.lStart + item1.lLength;
						long end2 = item2.lStart + item2.lLength;

						if (end1 <= end2) 
						{
							item.lLength = end1 - item.lStart;
						}
						else 
						{
							item.lLength = end2 - item.lStart;
						}
						if (targetMiddle != null)
							targetMiddle.Add(item);

						// item2��Item2���ص���C���֣����������´�ѭ��

						if (end1 <= end2) 
						{
							if (end1 == end2) 
							{
								item1 = null;
								item2 = null;
								continue;
							}
							item2.lStart = end1;
							item2.lLength = end2 - end1;
							item1 = null;
							continue;
						}
						else 
						{
							item1.lStart = end2;
							item1.lLength = end1 - end2;
							item2 = null;
							continue;
						}
					} // -- C����


					// continue;
				} // -- �Ƚ�����Item

				// ֻ��Item1�ǿ�
				if (item1 != null) 
				{
					if (targetLeft != null)
						targetLeft.Add(item1);
					item1 = null;
					continue;
				}
				// ֻ��Item2�ǿ�
				if (item2 != null) 
				{
					if (targetRight != null)
						targetRight.Add(item2);
					item2 = null;
					continue;
				}
			}
		}

		// ��strRange1�б�ʾ�ķ�Χ��ȥstrRange2�ķ�Χ������
		public static string Sub(string strRange1, string strRange2)
		{
			RangeList rl1 = new RangeList(strRange1);
			RangeList rl2 = new RangeList(strRange2);

			RangeList result = new RangeList();
			RangeList.CrossOper(rl1,
                rl2,
				result,
				null,
				null);
			return result.GetContentRangeString();
		}

		// ���ط�Χ�а������ָ���
		public static long GetNumberCount(string strRange)
		{
			RangeList rl = new RangeList(strRange);
			long lTotal = 0;
			for(int i=0; i<rl.Count; i++) 
			{
				RangeItem item = (RangeItem)rl[i];
				lTotal += item.lLength;
			}

			return lTotal;
		}

		// ��һ��contentrange�ַ������շֿ�ߴ��и�Ϊ���contentrange�ַ���
		// ԭ��
		// �������ֵĸ������и�����ֱ����ֵ�޹ء�
		// �����ÿ�������Ķ�����������ָ������չ���chunksize������ַ��������������
		// ��Ѷ������һ�����Ϊһ���ַ�����
		public static string[] ChunkRange(string strRange, long lChunkSize)
		{
			if (lChunkSize <= 0)
				throw(new ArgumentException("RangeList.ChunkRange(string strRange, long lChunkSize): lChunkSize�����������0"));

            string[] result = null;

            // �շ�Χ 2006/6/27
            if (String.IsNullOrEmpty(strRange) == true)
            {
                result = new string[1];
                result[0] = strRange;
                return result;
            }


			RangeList rl = new RangeList(strRange);

			ArrayList aText = new ArrayList();

			long lCurSize = 0;
			int nStartIndex = 0;

			for(int i=0; i<rl.Count; i++) 
			{
				RangeItem item = (RangeItem)rl[i];
				lCurSize += item.lLength;
				if (lCurSize >= lChunkSize) 
				{
					string strText = "";
					// ��nStart��i֮��ת��Ϊһ���ַ���
					if (nStartIndex < i) 
					{
						strText += rl.GetContentRangeString(nStartIndex, i - nStartIndex);
						strText += ",";
					}

					long lDelta = lCurSize - lChunkSize;
					// i����λ��chunk����ߵ�ת��Ϊһ���ַ���
					strText += Convert.ToString(item.lStart) + "-"
						+ Convert.ToString(item.lStart + item.lLength - 1 - lDelta);
					// ���µĲ�������д��iλ��item 
					if (lDelta > 0) 
					{
						nStartIndex = i;
						long lUsed = item.lLength - lDelta;
						item.lStart += lUsed;
						item.lLength -= lUsed;
						i --;
					}
					else 
					{
						nStartIndex = i+1;
					}
					aText.Add(strText);
					lCurSize = 0;
					continue;
				}

			}

			// ���һ��
			if (nStartIndex < rl.Count)
			{
				string strText = "";
				// ��nStart��i֮��ת��Ϊһ���ַ���
				strText += rl.GetContentRangeString(nStartIndex, rl.Count - nStartIndex);
				aText.Add(strText);
			}

			if (aText.Count > 0) 
			{
				result = new string[aText.Count];
				for(int j=0;j<aText.Count;j++) 
				{
					result[j] = (string)aText[j];
				}
			}
			else // ȷ������������һ��Ԫ��
			{
				result = new string[1];
				result[0] = strRange;
			}

			return result;
		}
		
		// �ϲ�����contentrange�ַ���Ϊһ���´�
		// parameters:
		//		strS1	��һ����Χ�ַ���
		//		strS2	�ڶ�����Χ�ַ���
		//		lWholeLength	���ļ��ĳߴ硣������Ȿ�κϲ�����ַ����Ƿ��Ѿ���ȫ���������ļ���Χ
		//		strResult	out���������غϲ�����ַ���
		// return
		//		-1	���� 
		//		0	����δ���ǵĲ��� 
		//		1	�����Ѿ���ȫ����
		public static int MergeContentRangeString(string strS1, 
			string strS2,
			long lWholeLength,
			out string strResult,
            out string strError)
		{
            strError = "";

			RangeList rl1 = new RangeList(strS1);

			RangeList rl2 = new RangeList(strS2);

			// �������RangeList
			rl1.AddRange(rl2);

			// ����
			rl1.Sort();

			// �ϲ�����
			rl1.Merge();

            // ������!
            // Debug.Assert(rl1.Count == 1, "");

			// ���غϲ����contentrange�ַ���
			strResult = rl1.GetContentRangeString();

			if (rl1.Count == 1) 
			{
				RangeItem item = (RangeItem)rl1[0];

                if (item.lLength > lWholeLength)
                {
                    strError = "Ψһһ������ĳ��� " + item.lLength.ToString() + " ��Ȼ�������峤�� " + lWholeLength.ToString();
                    return -1;	// Ψһһ������ĳ��Ⱦ�Ȼ�������ĳ��ȣ�ͨ�������������������
                }

				if (item.lStart == 0
					&& item.lLength == lWholeLength)
					return 1;	// ��ʾ��ȫ����
			}

			return 0;	// ����δ���ǵĲ���
		}

		// ��Դ�ļ���ָ����Ƭ�����ݸ��Ƶ�Ŀ���ļ���
		// ��strContentRange��ֵΪ""ʱ����ʾ���������ļ�
		// ����ֵ��-1 ���� ���� ���Ƶ��ܳߴ�
		public static long CopyFragment(
			string strSourceFileName,
			string strContentRange,
			string strTargetFileName,
			out string strErrorInfo)
		{
			long lTotalBytes = 0;
			strErrorInfo = "";

			FileInfo fi = new FileInfo(strSourceFileName);
			if (fi.Length == 0)
				return 0;
			// ��ʾ��Χ���ַ���Ϊ�գ�ǡǡ��ʾҪ����ȫ����Χ
			if (strContentRange == "") 
			{
				strContentRange = "0-" + Convert.ToString(fi.Length - 1);
			}

			// ����RangeList��������ⷶΧ�ַ���
			RangeList rl = new RangeList(strContentRange);


			// ���strContentRangeָ���������С�߽��Դ�ļ���ʵ������Ƿ�ì��
			long lMax = rl.max();
			if (fi.Length <= lMax) 
			{
				strErrorInfo = "�ļ�" +strSourceFileName+ "�ļ��ߴ�ȷ�Χ" + strContentRange + "�ж�������߽�"
					+ Convert.ToString(lMax) + "С...";
				return -1;
			}

			long lMin = rl.min();
			if (fi.Length <= lMin) 
			{
				strErrorInfo = "�ļ�" +strSourceFileName+ "�ļ��ߴ�ȷ�Χ" + strContentRange + "�ж������С�߽�"
					+ Convert.ToString(lMax) + "С...";
				return -1;
			}

			FileStream fileTarget = File.Create(strTargetFileName);
			FileStream fileSource = File.Open(strSourceFileName,
				FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			// ѭ��������ÿ������Ƭ��
			for(int i=0; i<rl.Count; i++) 
			{
				RangeItem ri = (RangeItem)rl[i];

				fileSource.Seek(ri.lStart,SeekOrigin.Begin);
				DumpStream(fileSource, fileTarget, ri.lLength, true);

				lTotalBytes += ri.lLength;
			}


			fileTarget.Close();
			fileSource.Close();
		
			return lTotalBytes;
		}

		public static long DumpStream(Stream streamSource, 
			Stream streamTarget,
			long lLength,
			bool bFlush)
		{
			long lWrited = 0;
			long lThisRead = 0;
			int nChunkSize = 8192;
			byte[] bytes = new byte[nChunkSize];
			while (true) 
			{
				long lLeft = lLength - lWrited;
				if (lLeft > nChunkSize)
					lThisRead = nChunkSize;
				else
					lThisRead = lLeft;
				long n = streamSource.Read(bytes,0,(int)lThisRead);

				if (n != 0) // 2005/6/8
				{
					streamTarget.Write(bytes,0,(int)n);
				}

				if (bFlush == true)
					streamTarget.Flush();


				//if (n<nChunkSize)
				//	break;
				if (n <= 0)
					break;

				lWrited += n;
			}

			return lWrited;
		}

		// ��Դ�ļ���ָ����Ƭ�����ݸ��Ƶ�Ŀ���ļ���
		// ��strContentRange��ֵΪ""ʱ����ʾ���������ļ�
		// ����ֵ��-1 ���� ���� ���Ƶ��ܳߴ�
		public static long CopyFragment(
			string strSourceFileName,
			string strContentRange,
			out byte[] baResult,
			out string strErrorInfo)
		{
			baResult = null;
			strErrorInfo = "";

			FileStream fileSource = File.Open(
				strSourceFileName,
				FileMode.Open,
				FileAccess.Read, 
				FileShare.ReadWrite);
			try 
			{
				FileInfo fi = new FileInfo(strSourceFileName);
				
				if (fi.Length == 0)
					return 0;
				

				long lRet = CopyFragment(fileSource,
					fi.Length,
					strContentRange,
					out baResult,
					out strErrorInfo);
				return lRet;

			}
			finally 
			{
				fileSource.Close();
			}
		}

		// ��Դ�ļ���ָ����Ƭ�����ݸ��Ƶ�Ŀ���ļ���
		// ��strContentRange��ֵΪ""ʱ����ʾ���������ļ�
		// ����ֵ��-1 ���� ���� ���Ƶ��ܳߴ�
		public static long CopyFragment(
			Stream fileSource,
			long lTotalLength,
			string strContentRange,
			out byte[] baResult,
			out string strErrorInfo)
		{
			long lTotalBytes = 0;
			strErrorInfo = "";
			baResult = null;

			/*
			FileInfo fi = new FileInfo(strSourceFileName);
			if (fi.Length == 0)
				return 0;
			*/

			long lFileStart = fileSource.Position;

			// ��ʾ��Χ���ַ���Ϊ�գ�ǡǡ��ʾҪ����ȫ����Χ
			if (strContentRange == "") 
			{
				if (lTotalLength == 0) // 2005/6/24
				{
					baResult = new byte[0];
					return 0;
				}

				strContentRange = "0-" + Convert.ToString(lTotalLength - 1);
			}

			// ����RangeList��������ⷶΧ�ַ���
			RangeList rl = new RangeList(strContentRange);

			// ���strContentRangeָ���������С�߽��Դ�ļ���ʵ������Ƿ�ì��
			long lMax = rl.max();
			if (lTotalLength <= lMax) 
			{
				strErrorInfo = "�ļ��ߴ�ȷ�Χ" + strContentRange + "�ж�������߽�"
					+ Convert.ToString(lMax) + "С...";
				return -1;
			}

			long lMin = rl.min();
			if (lTotalLength <= lMin) 
			{
				strErrorInfo = "�ļ��ߴ�ȷ�Χ" + strContentRange + "�ж������С�߽�"
					+ Convert.ToString(lMax) + "С...";
				return -1;
			}

			/*
			FileStream fileSource = File.Open(
				strSourceFileName,
				FileMode.Open,
				FileAccess.Read, 
				FileShare.ReadWrite);
			*/

			//			int nStart = 0;

			// ѭ��������ÿ������Ƭ��
			for(int i=0,nStart=0; i<rl.Count; i++) 
			{
				RangeItem ri = (RangeItem)rl[i];

				fileSource.Seek(ri.lStart + lFileStart,SeekOrigin.Begin);
				baResult = ByteArray.EnsureSize(baResult, nStart + (int)ri.lLength);
				nStart += fileSource.Read(baResult, nStart, (int)ri.lLength);

				lTotalBytes += ri.lLength;
			}

			// fileSource.Close();
		
			return lTotalBytes;
		}
	} // end of class RangeList
}
