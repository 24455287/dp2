using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

using DigitalPlatform;
using DigitalPlatform.Range;

namespace dp2Circulation
{
    /// <summary>
    /// ��ӡ HTML ���ݵĴ���
    /// </summary>
    public partial class HtmlPrintForm : Form
    {
        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;

        AutoResetEvent eventPrintComplete = new AutoResetEvent(false);	// true : initial state is signaled 

        // �ļ�������
        /// <summary>
        /// �ļ������ϡ������趨��Ҫ��ӡ����Щ HTML �ļ���
        /// </summary>
        public List<string> Filenames = new List<string>();

        int m_nCurrenPageNo = 0;  // ��ǰ��ʾҳ

        bool m_bShowDialog = false; // ��һҳ��ӡ��ʱ���Ƿ���ִ�ӡ�Ի���

        /// <summary>
        /// ���캯��
        /// </summary>
        public HtmlPrintForm()
        {
            InitializeComponent();
        }

        private void HtmlPrintForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
            // �ѵ�һҳװ��
            this.LoadPageFile();

            this.EnableButtons();

            DisplayPageInfoLine();

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
        }

        private void HtmlPrintForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }
        }

        private void button_prevPage_Click(object sender, EventArgs e)
        {
            if (this.Filenames == null)
                return;

            if (this.m_nCurrenPageNo == 0)
                return;

            this.m_nCurrenPageNo--;
            this.LoadPageFile();

            this.EnableButtons();
            this.DisplayPageInfoLine();

        }

        private void button_nextPage_Click(object sender, EventArgs e)
        {
            if (this.Filenames == null)
                return;

            if (this.m_nCurrenPageNo >= this.Filenames.Count - 1)
                return;

            this.m_nCurrenPageNo++;
            this.LoadPageFile();

            this.EnableButtons();
            this.DisplayPageInfoLine();

        }

        private void button_firstPage_Click(object sender, EventArgs e)
        {
            if (this.Filenames == null)
                return;

            if (this.m_nCurrenPageNo == 0)
                return;

            this.m_nCurrenPageNo = 0;
            this.LoadPageFile();

            this.EnableButtons();
            this.DisplayPageInfoLine();
        }

        private void button_lastPage_Click(object sender, EventArgs e)
        {
            if (this.Filenames == null)
                return;

            if (this.m_nCurrenPageNo >= this.Filenames.Count - 1)
                return;

            this.m_nCurrenPageNo = this.Filenames.Count - 1;
            this.LoadPageFile();

            this.EnableButtons();
            this.DisplayPageInfoLine();
        }

        // ��ӡ
        private void button_print_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (Control.ModifierKeys == Keys.Control)
                this.m_bShowDialog = true;  // ǿ�Ƴ��ִ�ӡ�Ի���
            else
                this.m_bShowDialog = false;

            RangeList rl = null;    // rl==null��ʾȫ����ӡ

            if (String.IsNullOrEmpty(this.textBox_printRange.Text) == false)
            {
                try
                {
                    rl = new RangeList(this.textBox_printRange.Text);
                }
                catch (Exception ex)
                {
                    strError = "��ӡ��Χ�ַ�����ʽ����: " + ex.Message;
                    goto ERROR1;
                }
            }

            int nCopies = 1;

            try
            {
                nCopies = Convert.ToInt32(this.textBox_copies.Text);
            }
            catch
            {
                strError = "����ֵ��ʽ����";
                goto ERROR1;
            }

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("���ڴ�ӡ ...");
            stop.BeginLoop();
            this.Update();
            this.MainForm.Update();

            int nPrinted = 0;

            try
            {

                this.webBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser1_DocumentCompleted);
                this.eventPrintComplete.Reset();

                // Debug.Assert(false, "");

                for (int c = 0; c < nCopies; c++)
                {

                    // ��ӡ����ҳ
                    for (int i = 0; i < this.Filenames.Count; i++)
                    {
                        Application.DoEvents();	// ���ý������Ȩ


                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�";
                                goto ERROR1;
                            }
                        }


                        if (rl == null
                            || rl.IsInRange(i + 1, false) == true)
                        {
                            // MessageBox.Show(this, "once");
                            nPrinted++;

                            stop.SetMessage("���ڴ�ӡ�� " + (i + 1).ToString() + " ҳ...");

                            this.m_nCurrenPageNo = i;

                            this.LoadPageFile();    // ͨ��completed�¼���������ӡ��

                            while (true)
                            {
                                Application.DoEvents();	// ���ý������Ȩ
                                if (eventPrintComplete.WaitOne(100, true) == true)
                                    break;
                            }
                        }
                    }
                }

                this.webBrowser1.DocumentCompleted -= new WebBrowserDocumentCompletedEventHandler(webBrowser1_DocumentCompleted);

            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("��ӡ��ɡ�����ӡ " + nPrinted.ToString() + "ҳ��");

                EnableControls(true);
            }

            if (nPrinted == 0)
            {
                MessageBox.Show(this, "����ָ���Ĵ�ӡҳ�뷶Χ '" 
                    +this.textBox_printRange.Text + "' û���ҵ�ƥ���ҳ��");
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
            return;
        }

        void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            // Debug.Assert(false, "");
            this.EnableButtons();
            this.DisplayPageInfoLine();

            this.Update();

            if (this.m_bShowDialog == true)
            {
                this.webBrowser1.ShowPrintDialog();
                m_bShowDialog = false;
            }
            else
                this.webBrowser1.Print();

            this.eventPrintComplete.Set();
        }

        void DoStop(object sender, StopEventArgs e)
        {
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public void EnableControls(bool bEnable)
        {
            this.button_print.Enabled = bEnable;
            this.textBox_printRange.Enabled = bEnable;
            this.textBox_copies.Enabled = bEnable;

            if (bEnable == false)
            {
                this.button_nextPage.Enabled = false;
                this.button_prevPage.Enabled = false;
            }
            else
            {
                this.EnableButtons();
            }
        }

        void LoadPageFile()
        {
            if (this.Filenames == null || this.Filenames.Count == 0)
            {
                Global.ClearHtmlPage(this.webBrowser1, this.MainForm.DataDir);
                return;
            }
            this.webBrowser1.Navigate(this.Filenames[this.m_nCurrenPageNo]);
        }

        void DisplayPageInfoLine()
        {
            this.label_pageInfo.Text = (this.m_nCurrenPageNo + 1).ToString() 
                + " / "
                + (this.Filenames == null ? "0" : this.Filenames.Count.ToString());
        }

        void EnableButtons()
        {
            if (this.Filenames == null || this.Filenames.Count == 0)
            {
                this.button_prevPage.Enabled = false;
                this.button_nextPage.Enabled = false;

                this.button_firstPage.Enabled = false;
                this.button_lastPage.Enabled = false;

                this.button_print.Enabled = false;
                return;
            }

            this.button_print.Enabled = true;

            if (this.m_nCurrenPageNo == 0)
            {
                this.button_firstPage.Enabled = false;
                this.button_prevPage.Enabled = false;
            }
            else
            {
                this.button_firstPage.Enabled = true;
                this.button_prevPage.Enabled = true;
            }


            if (this.m_nCurrenPageNo >= this.Filenames.Count - 1)
            {
                this.button_lastPage.Enabled = false;
                this.button_nextPage.Enabled = false;
            }
            else
            {
                this.button_lastPage.Enabled = true;
                this.button_nextPage.Enabled = true;
            }
        }

        // 
        /// <summary>
        /// ��HTML�ַ�����ӡ����
        /// </summary>
        /// <param name="strHtml">HTML �ַ���</param>
        public void PrintHtmlString(string strHtml)
        {
            Global.SetHtmlString(this.webBrowser1, strHtml);
            this.webBrowser1.Print();
        }
    }
}