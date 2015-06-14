using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Serialization;

using DigitalPlatform;	// Stop��
using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Script;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Marc;
using DigitalPlatform.Range;

using DigitalPlatform.Message;
using DigitalPlatform.rms.Client.rmsws_localhost;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// �������Ǻ�������������صĴ���
    /// </summary>
    public partial class LibraryApplication
    {
        // �Ӷϵ�����ļ��ж�����Ϣ
        // return:
        //      -1  error
        //      0   file not found
        //      1   found
        public int ReadBatchTaskBreakPointFile(string strTaskName,
            out string strText,
            out string strError)
        {
            strError = "";
            strText = "";

            string strFileName = this.LogDir + "\\" + strTaskName.Replace(" ", "_") + ".breakpoint";

            StreamReader sr = null;

            try
            {
                sr = new StreamReader(strFileName, Encoding.UTF8);
            }
            catch (FileNotFoundException /*ex*/)
            {
                return 0;   // file not found
            }
            catch (Exception ex)
            {
                strError = "open file '" +strFileName+ "' error : " + ex.Message;
                return -1;
            }
            try
            {
                sr.ReadLine();  // ����ʱ����
                strText = sr.ReadToEnd();// ��������
            }
            finally
            {
                sr.Close();
            }

            return 1;
        }

        // д��ϵ�����ļ�
        // parameters:
        //      strTaskName ��������������ֿո��ַ�
        public void WriteBatchTaskBreakPointFile(string strTaskName,
            string strText)
        {
            string strFileName = this.LogDir + "\\" + strTaskName.Replace(" ", "_") + ".breakpoint";
            string strTime = DateTime.Now.ToString();

            // ɾ��ԭ�����ļ�
            try
            {
                File.Delete(strFileName);
            }
            catch
            {
            }

            // д��������
            StreamUtil.WriteText(strFileName,
                strTime + "\r\n");
            StreamUtil.WriteText(strFileName,
                strText);
        }

        // ɾ���ϵ��ļ�
        public void RemoveBatchTaskBreakPointFile(string strTaskName)
        {
            string strFileName = this.LogDir + "\\" + strTaskName.Replace(" ", "_") + ".breakpoint";
            try
            {
                File.Delete(strFileName);
            }
            catch
            {
            }
        }

        public void StopAllBatchTasks()
        {
            for (int i = 0; i < this.BatchTasks.Count; i++)
            {
                BatchTask task = this.BatchTasks[i];
                task.Stop();
            }
        }

        // �������ǰ��Ϣ
        // ���̣߳���ȫ
        public BatchTaskInfo GetTaskInfo(string strText)
        {
            BatchTaskInfo info = new BatchTaskInfo();
            info.Name = "";
            info.State = "";

            info.ProgressText = strText;
            info.ResultText = null;
            info.ResultOffset = 0;
            info.ResultTotalLength = 0;
            info.ResultVersion = 0;

            return info;
        }

        // ������������һ������������(�����Զ�����)
        // return:
        //      -1  ����
        //      0   �����ɹ�
        //      1   ����ǰ�����Ѿ�����ִ��״̬�����ε��ü������������
        public int StartBatchTask(string strName,
            BatchTaskInfo param,
            out BatchTaskInfo info,
            out string strError)
        {
            strError = "";
            info = null;

            if (strName == "!continue")
            {
                this.PauseBatchTask = false;

                // 2013/11/23
                foreach (BatchTask current_task in this.BatchTasks)
                {
                    current_task.Activate();
                }

                info = GetTaskInfo("ȫ�������������Ѿ������ͣ");
                return 1;
            }

            // 2007/12/18
            if (this.HangupReason == HangupReason.LogRecover)
            {
                strError = "��ǰϵͳ������LogRecover����״̬���޷������µ�����������";
                return -1;
            }

            // 2012/2/4
            if (this.PauseBatchTask == true)
            {
                strError = "��ǰ���������������������ͣ״̬���޷������µ�����������";
                return -1;
            }

            BatchTask task = this.BatchTasks.GetBatchTask(strName);

            // �����µ�����
            if (task == null)
            {
                if (strName == "ԤԼ�������")
                    task = new ArriveMonitor(this, strName);
                else if (strName == "��־�ָ�")
                    task = new OperLogRecover(this, strName);
                else if (strName == "dp2Library ͬ��")
                    task = new LibraryReplication(this, strName);
                else if (strName == "�ؽ�������")
                    task = new RebuildKeys(this, strName);
                /*
            else if (strName == "����DTLP���ݿ�")
                task = new TraceDTLP(this, strName);
                 * */
                else if (strName == "��Ԫһ��ͨ������Ϣͬ��")
                    task = new ZhengyuanReplication(this, strName);
                else if (strName == "�Ͽ�Զ��һ��ͨ������Ϣͬ��")
                    task = new DkywReplication(this, strName);
                else if (strName == "������Ϣͬ��")
                    task = new PatronReplication(this, strName);
                else if (strName == "����֪ͨ")
                    task = new ReadersMonitor(this, strName);
                else if (strName == "��Ϣ���")
                    task = new MessageMonitor(this, strName);
                else
                {
                    strError = "ϵͳ����ʶ�������� '" + strName + "'";
                    return -1;
                }

                try
                {
                    this.BatchTasks.Add(task);
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }
            }
            else
            {
                bool bOldStoppedValue = task.Stopped;


                if (bOldStoppedValue == false)
                {
                    if (strName == "�ؽ�������")
                    {
                        task.StartInfos.Add(param.StartInfo);

                        task.AppendResultText("�������Ѽ���ȴ����У�\r\n---\r\n" + RebuildKeys.GetSummary(param.StartInfo) + "\r\n---\r\n\r\n");
                    }

                    else
                    {
                        // ��������ǰ�˷����Ĳ�����������
                        task.StartInfo = param.StartInfo;
                    }

                    // ���� 2007/10/10
                    task.eventActive.Set();
                    task.ManualStart = true;    // ��ʾΪ��������

                    strError = "���� " + task.Name + " �Ѿ��������У������ظ����������β����������������";
                    return 1;
                }
            }

            // ִ����־�ָ�����ǰ����Ҫ���ж�����ִ�е������κ�����
            // TODO: ��־�ָ� ���������ԭ���жϵ���Щ���񲢲����Զ�ȥ��������Ҫϵͳ����Ա�ֶ���������һ��Application
            if (strName == "��־�ָ�")
            {
                StopAllBatchTasks();
            }

            task.ManualStart = true;    // ��ʾΪ��������
            task.StartInfo = param.StartInfo;
            task.ClearProgressFile();   // ��������ļ�����
            task.StartWorkerThread();

            /*
            // ���� 2007/10/10
            task.eventActive.Set();
             * */

            info = task.GetCurrentInfo(param.ResultOffset,
                param.MaxResultBytes);

            return 0;
        }

        public int StopBatchTask(string strName,
            BatchTaskInfo param,
            out BatchTaskInfo info,
            out string strError)
        {
            strError = "";
            info = null;

            if (strName == "!pause")
            {
                this.PauseBatchTask = true;
                info = GetTaskInfo("ȫ�������������Ѿ�����ͣ");
                return 1;
            }

            BatchTask task = this.BatchTasks.GetBatchTask(strName);

            // �������Ͳ�����
            if (task == null)
            {
                strError = "���� '" + strName + "' ������";
                return -1;
            }

            task.Stop();

            info = task.GetCurrentInfo(param.ResultOffset,
                param.MaxResultBytes);

            return 1;
        }

        public int GetBatchTaskInfo(string strName,
            BatchTaskInfo param,
            out BatchTaskInfo info,
            out string strError)
        {
            strError = "";
            info = null;

            BatchTask task = this.BatchTasks.GetBatchTask(strName);

            // �������Ͳ�����
            if (task == null)
            {
                strError = "���� '" + strName + "' ������";
                return -1;
            }

            info = task.GetCurrentInfo(param.ResultOffset,
                param.MaxResultBytes);

            return 1;
        }
    }

    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class BatchTaskStartInfo
    {
        // ������ֹͣһ�����
        [DataMember]
        public string Param = "";   // ��ʽһ��ΪXML

        // ר�Ų���
        [DataMember]
        public string BreakPoint = ""; // �ϵ�  ��ʽΪ ���@�ļ���
        [DataMember]
        public string Start = ""; // ���  ��ʽΪ ���@�ļ���
        [DataMember]
        public string Count = ""; // ���� ������

        public string ToString()
        {
            Hashtable table = new Hashtable();
            if (string.IsNullOrEmpty(this.Param) == false)
                table["Param"] = this.Param;
            if (string.IsNullOrEmpty(this.BreakPoint) == false)
                table["BreakPoint"] = this.BreakPoint;
            if (string.IsNullOrEmpty(this.Start) == false)
                table["Start"] = this.Start;
            if (string.IsNullOrEmpty(this.Count) == false)
                table["Count"] = this.Count;

            return StringUtil.BuildParameterString(table, ',', ':');
        }

        public static BatchTaskStartInfo FromString(string strText)
        {
            BatchTaskStartInfo info = new BatchTaskStartInfo();
            Hashtable table = StringUtil.ParseParameters(strText, ',', ':');
            info.Param = (string)table["Param"] ;
            info.BreakPoint = (string)table["BreakPoint"];
            info.Start = (string)table["Start"];
            info.Count = (string)table["Count"];
            return info;
        }
    }

    // ������������Ϣ
    [DataContract(Namespace = "http://dp2003.com/dp2library/")]
    public class BatchTaskInfo
    {
        // ����
        [DataMember]
        public string Name = "";

        // ״̬
        [DataMember]
        public string State = "";

        // ��ǰ����
        [DataMember]
        public string ProgressText = "";

        // ������
        [DataMember]
        public int MaxResultBytes = 0;
        [DataMember]
        public byte[] ResultText = null;
        [DataMember]
        public long ResultOffset = 0;   // ���λ�õ�ResultText���ĩβ��
        [DataMember]
        public long ResultTotalLength = 0;  // ��������ļ��ĳ���

        [DataMember]
        public BatchTaskStartInfo StartInfo = null;

        [DataMember]
        public long ResultVersion = 0;  // ��Ϣ�ļ��汾
    }
}
