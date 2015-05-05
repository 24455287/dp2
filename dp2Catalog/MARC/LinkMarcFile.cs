using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using DigitalPlatform.Marc;

namespace dp2Catalog
{
    /// <summary>
    /// ��MARC��¼�����ӵ�ISO2709�ļ���
    /// ����ǰ�󷭿���¼��
    /// </summary>
    public class LinkMarcFile
    {
        List<long> heads = new List<long>(); // ��¼ͷ��ƫ��������

        /// <summary>
        /// �ļ���
        /// </summary>
        public string FileName = "";


        Stream _file = null; // �ļ���

        /// <summary>
        /// �ļ��� Stream ����
        /// </summary>
        public Stream Stream
        {
            get
            {
                return this._file;
            }
        }

        /// <summary>
        /// MARC ��ʽ��
        /// Ϊ unimarc/usmarc ֮һ
        /// </summary>
        public string MarcSyntax = "unimarc";

        /// <summary>
        /// ���뷽ʽ
        /// </summary>
        public Encoding Encoding = Encoding.GetEncoding(936);   // GB2312

        /// <summary>
        /// ��ǰ��¼��������
        /// -1 ��ʾ��δ��ʼ��
        /// </summary>
        public int CurrentIndex = -1;  // ��ǰ��¼����

        // ���ļ�
        // return:
        //      -1  error
        //      0   succeed
        public int Open(string strFilename,
            out string strError)
        {
            strError = "";

            if (this._file != null)
                this.Close();

            this._file = File.Open(
                strFilename,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);
            /*
            this.file = File.Open(strFilename,
                FileMode.Open,
                FileAccess.Read);
             * */
            if (_file == null)
            {
                strError = "�ļ� '" + strFilename + "' ��ʧ��";
                return -1;
            }

            this.FileName = strFilename;

            return 0;
        }

        // �����һ����¼
        // return:
        //      -1  error
        //      0   succeed
        //      1   reach end(��ǰ���صļ�¼��Ч)
        //	    2	����(��ǰ���صļ�¼��Ч)
        public int NextRecord(out string strMARC,
            out byte [] baRecord,
            out string strError)
        {
            strError = "";
            strMARC = "";
            baRecord = null;

            int nRet = 0;

            int index = this.CurrentIndex + 1;

            if (index < this.heads.Count)
                _file.Position = heads[index];
            else
            {
                // ����ͷ��
                this.heads.Add(_file.Position);
            }

            // ��ISO2709�ļ��ж���һ��MARC��¼
            // return:
            //	-2	MARC��ʽ��
            //	-1	����
            //	0	��ȷ
            //	1	����(��ǰ���صļ�¼��Ч)
            //	2	����(��ǰ���صļ�¼��Ч)
            nRet = MarcUtil.ReadMarcRecord(_file,
                Encoding,
                true,	// bRemoveEndCrLf,
                true,	// bForce,
                out strMARC,
                out baRecord,
                out strError);
            if (nRet == -2 || nRet == -1)
            {
                strError = "����MARC��¼(" + index.ToString() + ")����: " + strError;
                return -1;
            }

#if NO
            if (nRet != 0 && nRet != 1)
                return 1;
#endif
            // 2013/5/26
            if (nRet == 1 || nRet == 2)
            {
                if (nRet == 2)
                    strError = "��β";
                return nRet;
            }

            this.CurrentIndex = index;

            return 0;
        }

        // �����һ����¼
        // return:
        //      -1  error
        //      0   succeed
        //      1   reach head
        public int PrevRecord(out string strMARC,
            out byte[] baRecord,
            out string strError)
        {
            strError = "";
            strMARC = "";
            baRecord = null;
            int nRet = 0;

            if (this.CurrentIndex <= 0)
            {
                strError = "��ͷ";
                return 1;
            }

            int index = this.CurrentIndex - 1;

            _file.Position = heads[index];

            // ��ISO2709�ļ��ж���һ��MARC��¼
            // return:
            //	-2	MARC��ʽ��
            //	-1	����
            //	0	��ȷ
            //	1	����(��ǰ���صļ�¼��Ч)
            //	2	����(��ǰ���صļ�¼��Ч)
            nRet = MarcUtil.ReadMarcRecord(_file,
                Encoding,
                true,	// bRemoveEndCrLf,
                true,	// bForce,
                out strMARC,
                out baRecord,
                out strError);
            if (nRet == -2 || nRet == -1)
            {
                strError = "����MARC��¼(" + index.ToString() + ")����: " + strError;
                return -1;
            }

            if (nRet != 0 && nRet != 1)
                return 1;

            this.CurrentIndex = index;

            return 0;
        }

        // ���»�õ�ǰ��¼
        // return:
        //      -1  error
        //      0   succeed
        //      1   reach head
        public int CurrentRecord(out string strMARC,
            out byte[] baRecord,
            out string strError)
        {
            strError = "";
            strMARC = "";
            baRecord = null;
            int nRet = 0;

            if (this.CurrentIndex < 0)
            {
                strError = "Խ��ͷ��";
                return 1;
            }

            int index = this.CurrentIndex;

            _file.Position = heads[index];

            // ��ISO2709�ļ��ж���һ��MARC��¼
            // return:
            //	-2	MARC��ʽ��
            //	-1	����
            //	0	��ȷ
            //	1	����(��ǰ���صļ�¼��Ч)
            //	2	����(��ǰ���صļ�¼��Ч)
            nRet = MarcUtil.ReadMarcRecord(_file,
                Encoding,
                true,	// bRemoveEndCrLf,
                true,	// bForce,
                out strMARC,
                out baRecord,
                out strError);
            if (nRet == -2 || nRet == -1)
            {
                strError = "����MARC��¼(" + index.ToString() + ")����: " + strError;
                return -1;
            }

            if (nRet != 0 && nRet != 1)
            {
                strError = "Խ��β��";
                return 1;
            }

            // this.CurrentIndex = index;
            return 0;
        }

        public void Close()
        {
            if (this._file != null)
            {
                _file.Close();
                _file = null;
            }
        }
    }
}
