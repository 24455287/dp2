using System;
using System.Collections.Generic;
using System.Text;

using DigitalPlatform.Text;

namespace dp2Circulation
{
    // һ������ľ�����Ϣ
    internal class VolumeInfo
    {
        public string Year = "";
        public string IssueNo = "";
        public string Zong = "";
        public string Volumn = "";

        public string GetString(/*bool bIncludeYear*/)
        {
            /*
            if (bIncludeYear == false)
            {
                // ������һ�������ڵĵ����ںš����ںš���ŵ��ַ���
                return BuildItemVolumeString(this.IssueNo,
                    this.Zong,
                    this.Volumn);
            }
            else
            {
             * */
                return BuildItemVolumeString(
                    this.Year,
                    this.IssueNo,
                    this.Zong,
                    this.Volumn);
            /*
            }
             * */
        }

        // ������һ�������ڵĵ����ںš����ںš���ŵ��ַ���
        public static string BuildItemVolumeString(
            string strYear,
            string strIssue,
            string strZong,
            string strVolume)
        {
            string strResult = "";
            if (String.IsNullOrEmpty(strYear) == false)
            {
                strResult += strYear;
            }

            if (String.IsNullOrEmpty(strIssue) == false)
            {
                if (String.IsNullOrEmpty(strResult) == false)
                    strResult += ",";
                strResult += "no." + strIssue;
            }

            if (String.IsNullOrEmpty(strZong) == false)
            {
                if (strResult != "")
                    strResult += ", ";
                strResult += "��." + strZong;
            }

            if (String.IsNullOrEmpty(strVolume) == false)
            {
                if (strResult != "")
                    strResult += ", ";
                strResult += "v." + strVolume;
            }

            return strResult;
        }

        /*
        // ������һ�������ڵĵ����ںš����ںš���ŵ��ַ���
        public static string BuildItemVolumeString(string strIssue,
            string strZong,
            string strVolume)
        {
            string strResult = "";
            if (String.IsNullOrEmpty(strIssue) == false)
                strResult += "no." + strIssue;

            if (String.IsNullOrEmpty(strZong) == false)
            {
                if (strResult != "")
                    strResult += ", ";
                strResult += "��." + strZong;
            }

            if (String.IsNullOrEmpty(strVolume) == false)
            {
                if (strResult != "")
                    strResult += ", ";
                strResult += "v." + strVolume;
            }

            return strResult;
        }
         * */

        // ����no.����
        public static int ExpandNoString(string strText,
            string strDefaultYear,
            out List<VolumeInfo> infos,
            out string strError)
        {
            strError = "";
            infos = new List<VolumeInfo>();

            string strCurrentYear = strDefaultYear;

            string[] no_parts = strText.Split(new char[] { ',', ':', ';', '��','��','��' });    // ':' ';' ��Ϊ�˼���ĳ���׶ε���ʱ�÷� 2001:no.1-2;2002:no.1-12
            for (int i = 0; i < no_parts.Length; i++)
            {
                string strPart = no_parts[i].Trim();
                if (String.IsNullOrEmpty(strPart) == true)
                    continue;
                if (StringUtil.IsNumber(strPart) == true
                    && strPart.Length == 4)
                {
                    strCurrentYear = strPart;
                    continue;
                }

                // ȥ��"no."����
                if (StringUtil.HasHead(strPart, "no.") == true)
                {
                    strPart = strPart.Substring(3).Trim();
                }

                // TODO: û��"no."��ͷ�ģ��Ƿ񾯸�?

                if (String.IsNullOrEmpty(strPart) == true)
                    continue;

                List<string> nos = null;

                try
                {
                    nos = ExpandSequence(strPart);
                }
                catch (Exception ex)
                {
                    strError = "���� '" + strPart + "' ��ʽ����:" + ex.Message;
                    return -1;
                }

                for (int j = 0; j < nos.Count; j++)
                {
                    string strNo = nos[j];

                    if (String.IsNullOrEmpty(strCurrentYear) == true)
                    {
                        strError = "������ '" + strNo + "' ��ʱ��û�б�Ҫ�������Ϣ���޷�����no.������Ϣ";
                        return -1;
                    }

                    VolumeInfo info = new VolumeInfo();
                    info.IssueNo = strNo;
                    info.Year = strCurrentYear;
                    infos.Add(info);
                }

            }

            return 0;
        }

        // �������ڷ�Χ���С����硰2001,no.1-12=��.101-112=v.25*12��
        public static int BuildVolumeInfos(string strText,
            out List<VolumeInfo> infos,
            out string strError)
        {
            int nRet = 0;
            strError = "";
            infos = new List<VolumeInfo>();

            string strYearString = "";
            string strNoString = "";
            string strVolumnString = "";
            string strZongString = "";

            List<string> notdef_segments = new List<string>();

            string[] segments = strText.Split(new char[] { '=' });
            for (int i = 0; i < segments.Length; i++)
            {
                string strSegment = segments[i].Trim();
                if (String.IsNullOrEmpty(strSegment) == true)
                    continue;
                if (strSegment.IndexOf("y.") != -1)
                    strYearString = strSegment;
                else if (strSegment.IndexOf("no.") != -1)
                    strNoString = strSegment;
                else if (strSegment.IndexOf("v.") != -1)
                    strVolumnString = strSegment;
                else if (strSegment.IndexOf("��.") != -1)
                    strZongString = strSegment;
                else
                {
                    notdef_segments.Add(strSegment);
                }
            }

            // 2012/4/25
            // �����ں����к���Ҫ�����ȱ�ˣ��������ںź;���ǲ��е�
            if (string.IsNullOrEmpty(strNoString) == true
                && (string.IsNullOrEmpty(strZongString) == false || string.IsNullOrEmpty(strVolumnString) == false))
            {
                strError = "�����ں����в���ʡȴ��'" + strText + "'";
                if (notdef_segments.Count > 0)
                    strError += "���ַ����г������޷�ʶ�������: " + StringUtil.MakePathList(notdef_segments, "=");
                return -1;
            }

            if (String.IsNullOrEmpty(strNoString) == false)
            {
                // ȥ��"y."����
                if (StringUtil.HasHead(strYearString, "y.") == true)
                {
                    strYearString = strYearString.Substring(2).Trim();
                }

                // ����no.����
                nRet = ExpandNoString(strNoString,
                    strYearString,
                    out infos,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�������� '" + strNoString + "' (���'" + strYearString + "')ʱ��������: " + strError;
                    return -1;
                }
            }

            // ȥ��"��."����
            if (StringUtil.HasHead(strZongString, "��.") == true)
            {
                strZongString = strZongString.Substring(2).Trim();
            }

            if (String.IsNullOrEmpty(strZongString) == false)
            {
                List<string> zongs = null;

                try
                {
                    zongs = ExpandSequence(strZongString);
                }
                catch (Exception ex)
                {
                    strError = "��. ���� '" + strZongString + "' ��ʽ����:" + ex.Message;
                    return -1;
                }

                for (int i = 0; i < infos.Count; i++)
                {
                    VolumeInfo info = infos[i];
                    if (i < zongs.Count)
                        info.Zong = zongs[i];
                    else
                        break;
                }
            }

            // ȥ��"v."����
            if (StringUtil.HasHead(strVolumnString, "v.") == true)
            {
                strVolumnString = strVolumnString.Substring(2).Trim();
            }

            if (String.IsNullOrEmpty(strVolumnString) == false)
            {
                List<string> volumes = null;

                try
                {
                    volumes = ExpandSequence(strVolumnString);
                }
                catch (Exception ex)
                {
                    strError = "v.���� '" + strVolumnString + "' ��ʽ����:" + ex.Message;
                    return -1;
                }

                string strLastValue = "";
                for (int i = 0; i < infos.Count; i++)
                {
                    VolumeInfo info = infos[i];
                    if (i < volumes.Count)
                    {
                        info.Volumn = volumes[i];
                        strLastValue = info.Volumn; // �������һ��
                    }
                    else
                        info.Volumn = strLastValue; // �������һ��
                }
            }

            // 2015/5/8 ��� strText ����Ϊ���̱ʲɷ硱֮��ģ����޷�����������
            if (infos.Count == 0 && notdef_segments.Count > 0)
            {
                strError += "���ڷ�Χ�ַ����г������޷�ʶ�������: " + StringUtil.MakePathList(notdef_segments, "=");
                return -1;
            }

            return 0;
        }

        // չ�������ַ���
        // �����׳��쳣
        public static List<string> ExpandSequence(string strText)
        {
            List<string> results = new List<string>();
            string[] parts = strText.Split(new char[] { ',','��' });
            for (int i = 0; i < parts.Length; i++)
            {
                string strPart = parts[i];
                if (String.IsNullOrEmpty(strPart) == true)
                {
                    results.Add(strPart);
                    continue;
                }

                // -
                int nRet = strPart.IndexOf("-");
                if (nRet != -1)
                {
                    string strStart = strPart.Substring(0, nRet);
                    string strEnd = strPart.Substring(nRet + 1);

                    int start = Convert.ToInt32(strStart);
                    int end = Convert.ToInt32(strEnd);

                    for (int j = start; j <= end; j++)
                    {
                        results.Add(j.ToString());
                    }

                    continue;
                }

                // *
                nRet = strPart.IndexOf("*");
                if (nRet != -1)
                {
                    string strValue = strPart.Substring(0, nRet);
                    string strCount = strPart.Substring(nRet + 1);

                    int count = Convert.ToInt32(strCount);
                    for (int j = 0; j < count; j++)
                    {
                        results.Add(strValue);
                    }

                    continue;
                }

                results.Add(strPart);
            }

            return results;
        }

        // ���������ںš����ںš���ŵ��ַ���
        public static void ParseItemVolumeString(string strVolumeString,
            out string strIssue,
            out string strZong,
            out string strVolume)
        {
            strIssue = "";
            strZong = "";
            strVolume = "";

            string[] segments = strVolumeString.Split(new char[] { ';', ',', '=','��','��','��' });    // ',','='Ϊ2010/2/24����
            for (int i = 0; i < segments.Length; i++)
            {
                string strSegment = segments[i].Trim();

                if (StringUtil.HasHead(strSegment, "no.") == true)
                    strIssue = strSegment.Substring(3).Trim();
                else if (StringUtil.HasHead(strSegment, "��.") == true)
                    strZong = strSegment.Substring(2).Trim();
                else if (StringUtil.HasHead(strSegment, "v.") == true)
                    strVolume = strSegment.Substring(2).Trim();
            }
        }

        public static int CheckIssueNo(
            string strName,
            string strIssueNo,
            out string strError)
        {
            strError = "";

            if (strIssueNo.IndexOfAny(new char[] {'-','*',',',';','=','?','��','��','��','��','��','��' }) != -1)
            {
                strError = strName + "�ַ����в��ܰ��������ַ�: '-','*',',',';','=','?'";
                return -1;
            }

            return 0;
        }

    }

    internal class IssueUtil
    {
        // ��ó������ڵ���ݲ���
        public static string GetYearPart(string strPublishTime)
        {
            if (String.IsNullOrEmpty(strPublishTime) == true)
                return strPublishTime;

            if (strPublishTime.Length <= 4)
                return strPublishTime;

            return strPublishTime.Substring(0, 4);
        }
    }
}
