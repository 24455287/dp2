using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// dp2library�е���C#�ű�ʱ, ����ת��һ���¼��Ϣxml->html�Ľű���Ļ���
    /// </summary>
    public class RecordConverter
    {
        public LibraryApplication App = null;

        public string RecPath = ""; // 2009/10/18 new add

        public RecordConverter()
        {

        }

        public virtual string Convert(string strXml)
        {

            return strXml;
        }
    }
}
