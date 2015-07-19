using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace dp2Circulation
{
    static class Program
    {
        static bool bExiting = false;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Begin();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        static void Begin()
        {
            Application.ThreadException += Application_ThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

#if NO
        static void End()
        {
            Application.ThreadException -= Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        }
#endif

        static void CurrentDomain_UnhandledException(object sender,
            UnhandledExceptionEventArgs e)
        {
            if (bExiting == true)
                return;

            Exception ex = (Exception)e.ExceptionObject;
            string strError = "����δ������쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
            MainForm main_form = Form.ActiveForm as MainForm;
            if (main_form != null)
                main_form.WriteErrorLog(strError);
            else
                WriteWindowsLog(strError, EventLogEntryType.Error);

            // TODO: ����Ϣ�ṩ������ƽ̨�Ŀ�����Ա���Ա����
            // TODO: ��ʾΪ��ɫ���ڣ���ʾ�������˼
            bool bSendReport = true;
            DialogResult result = MessageDlg.Show(main_form,
    "dp2Circulation ����δ֪���쳣:\r\n\r\n" + strError + "\r\n---\r\n\r\n�㡰�رա����رճ���",
    "dp2Circulation ����δ֪���쳣",
    MessageBoxButtons.OK,
    MessageBoxDefaultButton.Button1,
    ref bSendReport,
    new string[] { "�ر�" },
    "����Ϣ���͸�������");
#if NO
            if (result == DialogResult.Yes)
            {
                    bExiting = true;
                    Application.Exit();
            }
#endif

            // �����쳣����
            if (bSendReport)
                CrashReport(strError);
        }

        static void Application_ThreadException(object sender, 
            ThreadExceptionEventArgs e)
        {
            if (bExiting == true)
                return;

            Exception ex = (Exception)e.Exception;
            string strError = "����δ����Ľ����߳��쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
            MainForm main_form = Form.ActiveForm as MainForm;
            if (main_form != null)
                main_form.WriteErrorLog(strError);
            else
                WriteWindowsLog(strError, EventLogEntryType.Error);

            bool bSendReport = true;
            DialogResult result = MessageDlg.Show(main_form,
    "dp2Circulation ����δ֪���쳣:\r\n\r\n" + strError + "\r\n---\r\n\r\n�Ƿ�رճ���?",
    "dp2Circulation ����δ֪���쳣",
    MessageBoxButtons.YesNo,
    MessageBoxDefaultButton.Button2,
    ref bSendReport,
    new string[] { "�ر�", "����" },
    "����Ϣ���͸�������");
            {
                if (bSendReport)
                    CrashReport(strError);
            }
            if (result == DialogResult.Yes)
            {
                //End();
                bExiting = true;
                Application.Exit();
            }
        }

        static void CrashReport(string strText)
        {
            MainForm main_form = Form.ActiveForm as MainForm;

            MessageBar _messageBar = null;

            _messageBar = new MessageBar();
            _messageBar.TopMost = false;
            //_messageBar.BackColor = SystemColors.Info;
            //_messageBar.ForeColor = SystemColors.InfoText;
            _messageBar.Text = "dp2Circulation �����쳣";
            _messageBar.MessageText = "������ dp2003.com �����쳣���� ...";
            _messageBar.StartPosition = FormStartPosition.CenterScreen;
            _messageBar.Show(main_form);
            _messageBar.Update();

            int nRet = 0;
            string strError = "";
            try
            {
                string strSender = "";
                if (main_form != null)
                    strSender = main_form.GetCurrentUserName() + "@" + main_form.ServerUID;
                // ��������
                nRet = LibraryChannel.CrashReport(
                    strSender,
                    "dp2circulation",
                    strText,
                    out strError);
            }
            catch (Exception ex)
            {
                strError = "CrashReport() ���̳����쳣: " + ExceptionUtil.GetDebugText(ex);
                nRet = -1;
            }
            finally
            {
                _messageBar.Close();
                _messageBar = null;
            }

            if (nRet == -1)
            {
                strError = "�� dp2003.com �����쳣����ʱ����δ�ܷ��ͳɹ�����ϸ���: " + strError;
                MessageBox.Show(main_form, strError);
                // д�������־
                if (main_form != null)
                    main_form.WriteErrorLog(strError);
                else
                    WriteWindowsLog(strError, EventLogEntryType.Error);
            }
        }

        // д��Windowsϵͳ��־
        public static void WriteWindowsLog(string strText,
            EventLogEntryType type)
        {
            EventLog Log = new EventLog("Application");
            Log.Source = "dp2Circulation";
            Log.WriteEntry(strText, type);
        }
    }
}