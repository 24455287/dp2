using DigitalPlatform;
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
             //AppDomain currentDomain = AppDomain.CurrentDomain;
  //currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
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
            string strError = "����δ������쳣: " + ExceptionUtil.GetDebugText(ex);
            MainForm main_form = Form.ActiveForm as MainForm;
            if (main_form != null)
                main_form.WriteErrorLog(strError);
            else
                WriteWindowsLog(strError, EventLogEntryType.Error);

            // TODO: ����Ϣ�ṩ������ƽ̨�Ŀ�����Ա���Ա����
            // TODO: ��ʾΪ��ɫ���ڣ���ʾ�������˼
            bool bTemp = false;
            DialogResult result = MessageDlg.Show(main_form,
    "dp2Circulation ����δ֪���쳣:\r\n\r\n" + strError + "\r\n---\r\n\r\n�㡰�رա����رճ���",
    "dp2Circulation ����δ֪���쳣",
    MessageBoxButtons.OK,
    MessageBoxDefaultButton.Button1,
    ref bTemp,
    new string[] { "�ر�" });
#if NO
            if (result == DialogResult.Yes)
            {
                    bExiting = true;
                    Application.Exit();
            }
#endif
        }

        static void Application_ThreadException(object sender, 
            ThreadExceptionEventArgs e)
        {
            if (bExiting == true)
                return;

            Exception ex = (Exception)e.Exception;
            string strError = "����δ����Ľ����߳��쳣: " + ExceptionUtil.GetDebugText(ex);
            MainForm main_form = Form.ActiveForm as MainForm;
            if (main_form != null)
                main_form.WriteErrorLog(strError);
            else
                WriteWindowsLog(strError, EventLogEntryType.Error);

            bool bTemp = false;
            DialogResult result = MessageDlg.Show(main_form,
    "dp2Circulation ����δ֪���쳣:\r\n\r\n" + strError + "\r\n---\r\n\r\n�Ƿ�رճ���?",
    "dp2Circulation ����δ֪���쳣",
    MessageBoxButtons.YesNo,
    MessageBoxDefaultButton.Button2,
    ref bTemp,
    new string[] { "�ر�", "����" });
            if (result == DialogResult.Yes)
            {
                //End();
                bExiting = true;
                Application.Exit();
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