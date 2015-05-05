using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using DigitalPlatform.IO;
using DigitalPlatform.rms.Client;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// ӳ���ں������ļ�������
    /// </summary>
    public class CfgsMap
    {
        public string RootDir = "";

        public string ServerUrl = "";

        RecordLockCollection locks = new RecordLockCollection();

        public CfgsMap(string strRootDir,
            string strServerUrl)
        {
            this.RootDir = strRootDir;
            PathUtil.CreateDirIfNeed(this.RootDir);

            this.ServerUrl = strServerUrl;

        }

        public void Clear()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(this.RootDir);
                di.Delete(true);
            }
            catch
            {
            }
            PathUtil.CreateDirIfNeed(this.RootDir);
        }

        // ���ں����������ļ�ӳ�䵽����
        // return:
        //      -1  ����
        //      0   ������
        //      1   �ҵ�
        public int MapFileToLocal(
            RmsChannelCollection Channels,
            string strPath,
            out string strLocalPath,
            out string strError)
        {
            strLocalPath = "";
            strError = "";

            strLocalPath = this.RootDir + "/" + strPath;

            // ȷ��Ŀ¼����
            PathUtil.CreateDirIfNeed(Path.GetDirectoryName(strLocalPath));

            this.locks.LockForRead(strLocalPath);
            try {
                // ���������ļ��Ƿ����
                FileInfo fi = new FileInfo(strLocalPath);
                if (fi.Exists == true) {
                    if (fi.Length == 0)
                        return 0;   // not exist
                    return 1;
                }
            }
            finally
            {
                this.locks.UnlockForRead(strLocalPath);
            }

            // ȷ��Ŀ¼����
            PathUtil.CreateDirIfNeed(Path.GetDirectoryName(strLocalPath));


            this.locks.LockForWrite(strLocalPath);
            try
            {
                RmsChannel channel = Channels.GetChannel(this.ServerUrl);
                if (channel == null)
                {
                    strError = "GetChannel error";
                    return -1;
                }

                string strMetaData = "";
                byte[] baOutputTimestamp = null;
                string strOutputPath = "";

                long lRet = channel.GetRes(strPath,
                    strLocalPath,
                    (Stop)null,
                    out strMetaData,
                    out baOutputTimestamp,
                    out strOutputPath,
                    out strError);
                if (lRet == -1)
                {
                    if (channel.ErrorCode == ChannelErrorCode.NotFound)
                    {
                        // Ϊ�˱����Ժ��ٴδ������ȡ�ķ�ʱ��, ��Ҫ�ڱ���дһ��0�ֽڵ��ļ�
                        FileStream fs = File.Create(strLocalPath);
                        fs.Close();
                        return 0;
                    }
                    return -1;
                }

                return 1;
            }
            finally
            {
                this.locks.UnlockForWrite(strLocalPath);
            }
        }
    }
}
