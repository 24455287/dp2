using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using DigitalPlatform.Xml;
using DigitalPlatform.rms.Client;
using DigitalPlatform.rms.Client.rmsws_localhost;
using DigitalPlatform.Text;

namespace DigitalPlatform.LibraryServer
{
    // �ں����ݿ���Ϣ����
    public class KernelDbInfoCollection : List<KernelDbInfo>
    {
        // return:
        //      -1  ����
        //      0   �ɹ�
        public int Initial(RmsChannelCollection Channels,
            string strServerUrl,
            string strLang,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            ResInfoItem[] root_dir_results = null;

            RmsChannel channel = Channels.GetChannel(strServerUrl);

            // �г��������ݿ�
            root_dir_results = null;

            long lRet = channel.DoDir("",
                strLang,
                "alllang",
                out root_dir_results,
                out strError);
            if (lRet == -1)
                return -1;

            // ������ݿ��ѭ��
            for (int i = 0; i < root_dir_results.Length; i++)
            {
                ResInfoItem info = root_dir_results[i];
                if (info.Type != ResTree.RESTYPE_DB)
                    continue;

                ResInfoItem[] db_dir_result = null;

                lRet = channel.DoDir(info.Name,
                       strLang,
                       "alllang",
                       out db_dir_result,
                       out strError);
                if (lRet == -1)
                    return -1;

                KernelDbInfo db = new KernelDbInfo();
                nRet = db.Initial(info.Names, db_dir_result,
                    out strError);
                if (nRet == -1)
                    return -1;

                this.Add(db);
            }

            return 0;
        }

        // �������ݿ����ҵ�һ��KernelDbInfo���ݿ����
        // return:
        //      null   not found
        //      others  found
        public KernelDbInfo FindDb(string strCaption)
        {
            for (int i = 0; i < this.Count; i++)
            {
                KernelDbInfo db = this[i];

                if (db.MatchCaption(strCaption) == true)
                    return db;
            }
            return null;
        }

        // ���ض������ݿ���, ƥ��������ض�����б��from�б�
        // parameters:
        //      strFromStyle    from style���б�, �Զ��ŷָ
        //                      ���Ϊ�գ���ʾȫ��;��(2007/9/13 new add)
        // return:
        //      null    û���ҵ�
        //      �Զ��ŷָ��from���б�
        public string BuildCaptionListByStyleList(string strDbName,
            string strFromStyles,
            string strLang)
        {
            KernelDbInfo db = this.FindDb(strDbName);

            if (db == null)
                return null;

            string strResult = "";

            // 2007/9/13 new add
            if (String.IsNullOrEmpty(strFromStyles) == true
                || strFromStyles == "<ȫ��>" || strFromStyles.ToLower() == "<all>")
            {
                return "<all>";
                // strFromStyles = "<all>";
            }

            List<string> results = new List<string>();

            // ��ֳ�������style�ַ���
            string[] styles = strFromStyles.Split(new char[] {','});

            for (int i = 0; i < styles.Length; i++)
            {
                string strStyle = styles[i].Trim();
                if (String.IsNullOrEmpty(strStyle) == true)
                    continue;

                // 2012/5/16
                // ���� _time/_freetime,_rfc1123time/_utime�ȱ�ʾ�������Ե�style
                if (StringUtil.HasHead(strStyle, "_") == true
                    && StringUtil.HasHead(strStyle, "__") == false) // ���� __ ������Ҫ����ƥ��
                    continue;

                // ������ǰ���ݿ������form��styles
                for (int j = 0; j < db.Froms.Count; j++)
                {
                    string strStyles = db.Froms[j].Styles;

                    if (StringUtil.IsInList(strStyle, strStyles) == true
                        || strStyle == "<all>") // 2007/9/13 new add // ע�����������ں˱�����֧��<all>��from�������û�б�Ҫ�ˣ����Ǵ����Ա���
                    {
                        Caption tempCaption = db.Froms[j].GetCaption(strLang);
                        if (tempCaption == null)
                        {
                            // ����û���ҵ�������
                            tempCaption = db.Froms[j].GetCaption(null); // ����������Ե�caption
                            if (tempCaption == null)
                            {
                                throw new Exception("���ݿ� '" + db.Captions[0].Value + "' ��û���ҵ��±�Ϊ " + j.ToString() + " ��From������κ�Caption");
                            }
                        }

                        // ȫ��·������£�Ҫ������"__id";��
                        if (strStyle == "<all>"
                            && tempCaption.Value == "__id")
                            continue;

#if NO
                        if (strResult != "")
                            strResult += ",";

                        strResult += tempCaption.Value;
#endif
                        results.Add(tempCaption.Value);
                    }
                }

            }

            // return strResult;

            StringUtil.RemoveDupNoSort(ref results);
            return StringUtil.MakePathList(results);
        }

        // ����styleֵȥ��
        // û��styleֵ��from����Ҫ����
        public static void RemoveDupByStyle(ref List<From> target)
        {
            for (int i = 0; i < target.Count; i++)
            {
                From from1 = target[i];

                // ��stylesΪ�յ������
                if (String.IsNullOrEmpty(from1.Styles) == true)
                {
                    target.RemoveAt(i);
                    i--;
                    continue;
                }

                for (int j = i + 1; j < target.Count; j++)
                {
                    From from2 = target[j];

                    if (from1.Styles == from2.Styles)
                    {
                        target.RemoveAt(j);
                        j--;
                    }
                }
            }
        }

        // ����captionȥ��(�����ض�����caption��ȥ��)
        public static void RemoveDupByCaption(ref List<From> target,
            string strLang = "zh")
        {
            if (string.IsNullOrEmpty(strLang) == true)
                strLang = "zh";

            for (int i = 0; i < target.Count; i++)
            {
                From from1 = target[i];

                List<Caption> captions1 = from1.GetCaptions(strLang);
                // ��caption(�ض�����)Ϊ�յ������
                if (captions1 == null || captions1.Count == 0)
                {
                    target.RemoveAt(i);
                    i--;
                    continue;
                }

                for (int j = i + 1; j < target.Count; j++)
                {
                    From from2 = target[j];
                    List<Caption> captions2 = from2.GetCaptions(strLang);

                    if (IsSame(captions1, captions2) == true)
                    {
                        target.RemoveAt(j);
                        j--;
                    }
                }
            }
        }

        // �ж�����caption�ͼ����Ƿ��й�ͬ��ֵ
        static bool IsSame(List<Caption> captions1, List<Caption> captions2)
        {
            foreach (Caption caption1 in captions1)
            {
                foreach (Caption caption2 in captions2)
                {
                    if (caption1.Value == caption2.Value)
                        return true;
                }
            }

            return false;
        }

        // 2014/8/29
        // ���һ�� from �� stylelist
        public string GetFromStyles(string strDbName,
    string strFrom,
    string strLang)
        {
            KernelDbInfo db = this.FindDb(strDbName);

            if (db == null)
                return null;

            // ������ǰ���ݿ������form
            foreach (From from in db.Froms)
            {
                // ע: ͬһ�����Դ���� caption��Ҳ�����в�ֹһ��
                List<Caption> captions = from.GetCaptions(strLang);
                if (captions == null || captions.Count == 0)
                {
                    // ����û���ҵ�������
                    Caption tempCaption = from.GetCaption(null); // ����������Ե�caption
                    if (tempCaption == null)
                        throw new Exception("���ݿ� '" + db.Captions[0].Value + "' ��û���ҵ�From���� " + from.ToString() + " ���κ�Caption");

                    captions.Add(tempCaption);
                }

                foreach (Caption caption in captions)
                {
                    if (caption.Value == strFrom)
                        return from.Styles;
                }
#if NO
                Caption tempCaption = from.GetCaption(strLang);
                if (tempCaption == null)
                {
                    // ����û���ҵ�������
                    tempCaption = from.GetCaption(null); // ����������Ե�caption
                    if (tempCaption == null)
                    {
                        throw new Exception("���ݿ� '" + db.Captions[0].Value + "' ��û���ҵ�From���� "+from.ToString()+" ���κ�Caption");
                    }
                }

                if (tempCaption.Value == strFrom)
                    return from.Styles;
#endif
            }

            return null;
        }
    }


    // �������ں����ݿ�ı�Ҫ��Ϣ
    public class KernelDbInfo
    {
        // �������йص�����
        public List<Caption> Captions = null;

        public List<From> Froms = null;

        public ResInfoItem[] db_dir_result = null;

        // ��db_dir_result�����ҵ�����ض����ݿ���������
        public static ResInfoItem GetDbItem(
    ResInfoItem[] root_dir_results,
    string strDbName)
        {
            for (int i = 0; i < root_dir_results.Length; i++)
            {
                ResInfoItem info = root_dir_results[i];

                if (info.Type != ResTree.RESTYPE_DB)
                    continue;

                if (info.Name == strDbName)
                    return info;

            }

            return null;
        }

        // �ҵ�һ��captionֵ(������������)
        public bool MatchCaption(string strCaption)
        {
            if (this.Captions == null)
                return false;

            for (int i = 0; i < this.Captions.Count; i++)
            {
                if (strCaption == this.Captions[i].Value)
                    return true;
            }

            return false;
        }

        public int Initial(
            string [] names,
            ResInfoItem[] db_dir_result,
            out string strError)
        {
            strError = "";

            List<Caption> captions = null;
            int nRet = BuildCaptions(names,
                out captions,
                out strError);
            if (nRet == -1)
                return -1;

            this.Captions = captions;


            this.Froms = new List<From>();

            for (int i = 0; i < db_dir_result.Length; i++)
            {
                ResInfoItem info = db_dir_result[i];
                if (info.Type != ResTree.RESTYPE_FROM)
                    continue;

                From from = new From();

                if (info.Names != null)
                {
                    captions = null;
                    nRet = BuildCaptions(info.Names,
                        out captions,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    from.Captions = captions;
                }
                else
                {
                    if (String.IsNullOrEmpty(info.Name) == true)
                    {
                        strError = "������һ��ResInfoItem���Names��Name��Ϊ�գ����ǲ��Ϸ���";
                        return -1;
                    }
                    // ����һ����������������
                    from.Captions = new List<Caption>();
                    from.Captions.Add(new Caption(null, info.Name));
                }

                from.Styles = info.TypeString;

                this.Froms.Add(from);

            }

            return 0;
        }

        // ���ݴӷ�������õ��������飬����Ϊ�������ʹ�õ�List<Caption>����
        public static int BuildCaptions(string[] names,
            out List<Caption> captions,
            out string strError)
        {
            strError = "";
            captions = new List<Caption>();

            for (int i = 0; i < names.Length; i++)
            {
                string strName = names[i];

                int nRet = strName.IndexOf(':');
                string strLang = "";
                string strValue = "";

                if (nRet != -1)
                {
                    strLang = strName.Substring(0, nRet);
                    strValue = strName.Substring(nRet + 1);
                }
                else
                {
                    strLang = strName;
                    strValue = "";
                    strError = "���ִ�������� '" + strName + "'���м�ȱ��ð�� ";
                    return -1;
                }

                Caption caption = new Caption(strLang,
                    strValue);

                captions.Add(caption);
            }

            return 0;
        }
    }


    // һ���������ֱ�־������
    public class Caption
    {
        public string Lang = "";
        public string Value = "";

        public Caption(string strLang,
            string strValue)
        {
            this.Lang = strLang;
            this.Value = strValue;
        }
    }

    public class From
    {
        public List<Caption> Captions = null;

        // ���
        public string Styles = "";

        // 2012/2/8
        // �������ƥ�������ȫƥ�������Caption
        public List<Caption> GetCaptions(string strLang)
        {
            List<Caption> results = new List<Caption>();
            for (int i = 0; i < this.Captions.Count; i++)
            {
                Caption caption = this.Captions[i];

                if (String.IsNullOrEmpty(strLang) == true)  // �������������Ϊδ֪, ��ֱ�ӷ��ص�һ��caption
                {
                    results.Add(caption);
                    return results;
                }

                int nRet = CompareLang(caption.Lang, strLang);

                if (nRet == 2 // ƥ���2��
                    || String.IsNullOrEmpty(caption.Lang) == true)  // ����Ϊ��������
                    results.Add(caption);
                else if (nRet == 1)
                    results.Add(caption);
            }

            return results; 
        }


        public Caption GetCaption(string strLang)
        {
            Caption OneCaption = null;  // ƥ���н�����1�ֵĵ�һ������
            for (int i = 0; i < this.Captions.Count; i++)
            {
                Caption caption = this.Captions[i];

                if (String.IsNullOrEmpty(strLang) == true)  // �������������Ϊδ֪, ��ֱ�ӷ��ص�һ��caption
                    return caption;

                int nRet = CompareLang(caption.Lang, strLang);

                if (nRet == 2 // ƥ���2��
                    || String.IsNullOrEmpty(caption.Lang) == true)  // ����Ϊ��������
                    return caption;

                if (nRet == 1 && OneCaption == null)
                    OneCaption = caption;
            }

            return OneCaption;  // �����1��ƥ��Ļ�
        }

        // �Ƚ��������Դ���
        // ��ν���Դ���, ������"zh-cn"�������ַ������������ʡ�ԡ�
        // return:
        //      0   ��ƥ��
        //      1   ���ƥ�䣬�����Ҷβ�ƥ��
        //      2   ���ξ�ƥ��
        static int CompareLang(string strRequest,
            string strValue)
        {
            if (String.IsNullOrEmpty(strRequest) == true
                && String.IsNullOrEmpty(strValue) == true)
                return 2;

            if (String.IsNullOrEmpty(strRequest) == true
                || String.IsNullOrEmpty(strValue) == true)
                return 0;

            string strRequestLeft = "";
            string strRequestRight = "";

            SplitLang(strRequest,
                out strRequestLeft,
                out strRequestRight);

            string strValueLeft = "";
            string strValueRight = "";

            SplitLang(strValue,
                out strValueLeft,
                out strValueRight);

            if (strRequestLeft == strValueLeft
                && strRequestRight == strValueRight)
                return 2;
            if (strRequestLeft == strValueLeft)
                return 1;

            return 0;
        }

        static void SplitLang(string strLang,
            out string strLangLeft,
            out string strLangRight)
        {
            strLangLeft = "";
            strLangRight = "";

            int nRet = strLang.IndexOf("-");
            if (nRet == -1)
                strLangLeft = strLang;
            else
            {
                strLangLeft = strLang.Substring(0, nRet);
                strLangRight = strLang.Substring(nRet + 1);
            }
        }
    }
}
