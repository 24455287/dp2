using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace dp2Circulation
{
    static class Program
    {
        static bool bExiting = false;

        static MainForm _mainForm = null;
        // ������ _mainForm �洢���ڶ��󣬲���ȡ Form.ActiveForm ��ȡ�ķ�ʽ��ԭ������
        // http://stackoverflow.com/questions/17117372/form-activeform-occasionally-works
        // Form.ActiveForm occasionally works

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (IsDevelopMode() == false)
                PrepareCatchException();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            _mainForm = new MainForm();
            Application.Run(_mainForm);
        }

        public static bool IsDevelopMode()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = 0;
            foreach(string arg in args)
            {
                if (i > 0 && arg == "develop")
                    return true;
                i++;
            }

            return false;
        }

        // ׼���ӹ�δ������쳣
        static void PrepareCatchException()
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
            // MainForm main_form = Form.ActiveForm as MainForm;
#if NO
            string strError = "����δ������쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
            if (main_form != null)
                main_form.WriteErrorLog(strError);
            else
                WriteWindowsLog(strError, EventLogEntryType.Error);
#endif
            string strError = GetExceptionText(ex, "");

            // TODO: ����Ϣ�ṩ������ƽ̨�Ŀ�����Ա���Ա����
            // TODO: ��ʾΪ��ɫ���ڣ���ʾ�������˼
            bool bSendReport = true;
            DialogResult result = MessageDlg.Show(_mainForm,
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

        static string GetExceptionText(Exception ex, string strType)
        {
            // Exception ex = (Exception)e.Exception;
            string strError = "����δ�����"+strType+"�쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
            Assembly myAssembly = Assembly.GetAssembly(typeof(Program));
            strError += "\r\ndp2Circulation �汾: " + myAssembly.FullName;
            strError += "\r\n����ϵͳ��" + Environment.OSVersion.ToString();

            // TODO: ��������ϵͳ��һ����Ϣ

            // MainForm main_form = Form.ActiveForm as MainForm;
            if (_mainForm != null)
            {
                try
                {
                    _mainForm.WriteErrorLog(strError);
                }
                catch
                {
                    WriteWindowsLog(strError, EventLogEntryType.Error);
                }
            }
            else
                WriteWindowsLog(strError, EventLogEntryType.Error);

            return strError;
        }

        static void Application_ThreadException(object sender, 
            ThreadExceptionEventArgs e)
        {
            if (bExiting == true)
                return;

            Exception ex = (Exception)e.Exception;
            // MainForm main_form = Form.ActiveForm as MainForm;
#if NO
            string strError = "����δ����Ľ����߳��쳣: \r\n" + ExceptionUtil.GetDebugText(ex);
            if (main_form != null)
                main_form.WriteErrorLog(strError);
            else
                WriteWindowsLog(strError, EventLogEntryType.Error);

            Assembly myAssembly = Assembly.GetAssembly(typeof(Program));
            strError += "\r\n\r\ndp2Circulation �汾:" + myAssembly.FullName;
#endif
            string strError = GetExceptionText(ex, "�����߳�");

            bool bSendReport = true;
            DialogResult result = MessageDlg.Show(_mainForm,
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
            // MainForm main_form = Form.ActiveForm as MainForm;

            MessageBar _messageBar = null;

            _messageBar = new MessageBar();
            _messageBar.TopMost = false;
            //_messageBar.BackColor = SystemColors.Info;
            //_messageBar.ForeColor = SystemColors.InfoText;
            _messageBar.Text = "dp2Circulation �����쳣";
            _messageBar.MessageText = "������ dp2003.com �����쳣���� ...";
            _messageBar.StartPosition = FormStartPosition.CenterScreen;
            _messageBar.Show(_mainForm);
            _messageBar.Update();

            int nRet = 0;
            string strError = "";
            try
            {
                string strSender = "";
                if (_mainForm != null)
                    strSender = _mainForm.GetCurrentUserName() + "@" + _mainForm.ServerUID;
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
                MessageBox.Show(_mainForm, strError);
                // д�������־
                if (_mainForm != null)
                    _mainForm.WriteErrorLog(strError);
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