using System;
using System.Collections.Generic;
using System.Text;

using DigitalPlatform.Z3950;

namespace dp2Catalog
{
    /// <summary>
    /// ���ּ������Ĺ�������ӿ�
    /// �����ⲿͨ��һ�µĽӿڻ�ȡ����
    /// </summary>
    public interface ISearchForm
    {
        string CurrentProtocol
        {
            get;
        }

        string CurrentResultsetPath
        {
            get;
        }

        // ���һ��MARC/XML��¼
        // parameters:
        //      index   ע�⣬�����ڵ��ú���Ϊ��Ҫ�����ķָ���λ��
        // return:
        //      -1  error
        //      0   suceed
        //      1   Ϊ��ϼ�¼
        //      2   �ָ�������Ҫ����������¼
        int GetOneRecord(
            string strStyle,
            int nTest,  // ��ʱʹ��
            string strPathParam, // int index,
            string strParameters,   // bool bHilightBrowseLine,
            out string strSavePath,
            out string strMARC,
            out string strXmlFragment,
            out string strOutStyle,
            out byte[] baTimestamp,
            out long lVersion,
            out DigitalPlatform.Z3950.Record record,
            out Encoding currrentEncoding,
            out LoginInfo logininfo, 
            out string strError);

        // ˢ��һ��MARC��¼
        // parameters:
        //      strAction   refresh / delete
        // return:
        //      -2  ��֧��
        //      -1  error
        //      0   ��ش����Ѿ����٣�û�б�Ҫˢ��
        //      1   �Ѿ�ˢ��
        //      2   �ڽ������û���ҵ�Ҫˢ�µļ�¼
        int RefreshOneRecord(
            string strPathParam,
            string strAction,
            out string strError);

        // ���󡢴����Ƿ���Ч?
        bool IsValid();

        // TODO: �ص�������������Գ��ֶԻ���ѯ��
        // ͬ��һ�� MARC/XML ��¼
        // ��� Lversion �ȼ������еļ�¼�£����� strMARC ���ݸ��¼������ڵļ�¼
        // ��� lVersion �ȼ������еļ�¼��(Ҳ����˵ Lverion ��ֵƫС)����ô�� strMARC ��ȡ����¼���µ���¼��
        // parameters:
        //      lVersion    [in]��¼���� Version [out] �������ļ�¼ Version
        // return:
        //      -1  ����
        //      0   û�б�Ҫ����
        //      1   �Ѿ����µ� ������
        //      2   ��Ҫ�� strMARC ��ȡ�����ݸ��µ���¼��
        int SyncOneRecord(string strPath,
            ref long lVersion,
            ref string strSyntax,
            ref string strMARC,
            out string strError);
    }

    public class LoginInfo
    {
        public string UserName = "";
        public string Password = "";
    }
}
