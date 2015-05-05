using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

using DigitalPlatform;
using DigitalPlatform.GUI;
using DigitalPlatform.Text;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;

using DigitalPlatform.CirculationClient.localhost;
using System.Web;

// 2013/3/16 ��� XML ע��

namespace dp2Circulation
{
    /// <summary>
    /// ͨ�õ� MDI �Ӵ��ڻ��ࡣ�ṩ��ͨѶͨ���ʹ��ڳߴ�ά�ֵ�ͨ����ʩ
    /// </summary>
    public class MyForm : Form, IMdiWindow
    {

        /// <summary>
        /// �����Ƿ�Ϊ����״̬
        /// </summary>
        public virtual bool Floating
        {
            get;
            set;
        }
        /// <summary>
        /// ͨѶͨ��
        /// </summary>
        public LibraryChannel Channel = new LibraryChannel();

        /// <summary>
        /// ��������
        /// </summary>
        public string Lang = "zh";

        MainForm m_mainForm = null;

        /// <summary>
        /// ��ǰ�����������Ŀ�ܴ���
        /// </summary>
        public MainForm MainForm
        {
            get
            {
                if (this.MdiParent != null)
                    return (MainForm)this.MdiParent;
                return m_mainForm;
            }
            set
            {
                // Ϊ���ýű������ܼ���
                this.m_mainForm = value;
            }
        }
        
        internal DigitalPlatform.Stop stop = null;

        /// <summary>
        /// ��������ֹͣ��ť
        /// </summary>
        public Stop Progress
        {
            get
            {
                return this.stop;
            }
        }

        string FormName
        {
            get
            {
                return this.GetType().ToString();
            }
        }

        internal string FormCaption
        {
            get
            {
                return this.GetType().Name;
            }
        }

        /// <summary>
        /// ������߽�ֹ����ؼ����ڳ�����ǰ��һ����Ҫ��ֹ����ؼ���������ɺ�������
        /// </summary>
        /// <param name="bEnable">�Ƿ��������ؼ���true Ϊ���� false Ϊ��ֹ</param>
        public virtual void EnableControls(bool bEnable)
        {
            throw new Exception("��δʵ�� EnableControls() ");
        }

        internal Timer _timer = null;

        public virtual void OnSelectedIndexChanged()
        {
        }

        public void TriggerSelectedIndexChanged()
        {
            if (this._timer == null)
            {
                this._timer = new Timer();
                this._timer.Interval = 500;
                this._timer.Tick += new EventHandler(_timer_Tick);
            }
            this._timer.Start();
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            this._timer.Stop();
            OnSelectedIndexChanged();
        }

        /// <summary>
        /// ���� Load ʱ������
        /// </summary>
        public virtual void OnMyFormLoad()
        {
            if (this.MainForm == null)
                return;

            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            this.Channel.AfterLogin -= new AfterLoginEventHandle(Channel_AfterLogin);
            this.Channel.AfterLogin += new AfterLoginEventHandle(Channel_AfterLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
        }


        /// <summary>
        /// ���� Closing ʱ������
        /// </summary>
        /// <param name="e">�¼�����</param>
        public virtual void OnMyFormClosing(FormClosingEventArgs e)
        {
            if (stop != null)
            {
                if (stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }
            }
        }

        // �� base.OnFormClosed(e); ֮ǰ����
        /// <summary>
        /// ���� Closed ʱ���������� base.OnFormClosed(e) ֮ǰ������
        /// </summary>
        public virtual void OnMyFormClosed()
        {
            if (this.Channel != null)
                this.Channel.Close();   // TODO: �������һ��ʱ�䣬�������ʱ����Abort()

            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }

            if (this.MainForm != null && this.MainForm.AppInfo != null
                && Floating == false && this.SupressSizeSetting == false)
            {
                MainForm.AppInfo.SaveMdiChildFormStates(this,
                "mdi_form_state");
            }

            /*
            // ���MDI�Ӵ��ڲ���MainForm�ո�׼���˳�ʱ��״̬���ָ�����Ϊ�˼���ߴ���׼��
            if (this.WindowState != this.MainForm.MdiWindowState)
                this.WindowState = this.MainForm.MdiWindowState;
             * */
        }

        /// <summary>
        /// ͨѶͨ����¼ǰ������
        /// </summary>
        /// <param name="sender">������</param>
        /// <param name="e">�¼�����</param>
        public virtual void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }

        public virtual void Channel_AfterLogin(object sender, AfterLoginEventArgs e)
        {
            this.MainForm.Channel_AfterLogin(this, e);
        }

        internal void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }

        /// <summary>
        /// ��ʼһ��ѭ��
        /// </summary>
        /// <param name="strStyle">���������� "halfstop"����ʾֹͣ��ťʹ���º��жϷ�ʽ </param>
        /// <param name="strMessage">Ҫ��״̬����ʾ����Ϣ����</param>
        public void BeginLoop(string strStyle = "",
            string strMessage = "")
        {
            if (StringUtil.IsInList("halfstop", strStyle) == true)
                stop.Style = StopStyle.EnableHalfStop;

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial(strMessage);
            stop.BeginLoop();
        }

        /// <summary>
        /// ����һ��ѭ��
        /// </summary>
        public void EndLoop()
        {
            stop.EndLoop();
            stop.OnStop -= new StopEventHandler(this.DoStop);
            stop.Initial("");
            stop.HideProgress();
            stop.Style = StopStyle.None;
        }

        /// <summary>
        /// ѭ���Ƿ������
        /// </summary>
        /// <returns>true: ѭ���Ѿ�����; false: ѭ����δ����</returns>
        public bool IsStopped()
        {
            Application.DoEvents();	// ���ý������Ȩ

            if (this.stop != null)
            {
                if (stop.State != 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// ���ý������ϵ�������ʾ
        /// </summary>
        /// <param name="strMessage">Ҫ��ʾ���ַ���</param>
        public void SetProgressMessage(string strMessage)
        {
            stop.SetMessage(strMessage);
        }

        /// <summary>
        /// Ϊ��ǰ���ڻָ�ȱʡ����
        /// </summary>
        public void RestoreDefaultFont()
        {
            if (this.MainForm != null)
            {
                Size oldsize = this.Size; 
                if (this.MainForm.DefaultFont == null)
                    MainForm.SetControlFont(this, Control.DefaultFont);
                else
                    MainForm.SetControlFont(this, this.MainForm.DefaultFont);
                this.Size = oldsize;
            }

            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                this.MainForm.AppInfo.SetString(
                   this.FormName,
                   "default_font",
                   "");

                MainForm.AppInfo.SetString(
                    this.FormName,
                    "default_font_color",
                    "");
            }
        }

        // ��������
        /// <summary>
        /// ���û������塣�����һ���Ի���ѯ��Ҫ�趨������
        /// </summary>
        public void SetBaseFont()
        {
            FontDialog dlg = new FontDialog();

            dlg.ShowColor = true;
            dlg.Color = this.ForeColor;
            dlg.Font = this.Font;
            dlg.ShowApply = true;
            dlg.ShowHelp = true;
            dlg.AllowVerticalFonts = false;

            dlg.Apply -= new EventHandler(dlgFont_Apply);
            dlg.Apply += new EventHandler(dlgFont_Apply);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            Size oldsize = this.Size;

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            this.Font = dlg.Font;
            this.ForeColor = dlg.Color;

            this.Size = oldsize;

            //ReLayout(this);

            SaveFontSetting();
        }

        void dlgFont_Apply(object sender, EventArgs e)
        {
            FontDialog dlg = (FontDialog)sender;

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;

            this.Font = dlg.Font;
            this.ForeColor = dlg.Color;

            //ReLayout(this);

            // ���浽�����ļ�
            SaveFontSetting();
        }

        /*
        static void ReLayout(Control parent_control)
        {
            foreach (Control control in parent_control.Controls)
            {
                ReLayout(control);

                control.ResumeLayout(false);
                control.PerformLayout();
            }

            parent_control.ResumeLayout(false);
        }*/

        /// <summary>
        /// ��������������Ϣ�����ò����洢
        /// </summary>
        public void SaveFontSetting()
        {
            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                {
                    // Create the FontConverter.
                    System.ComponentModel.TypeConverter converter =
                        System.ComponentModel.TypeDescriptor.GetConverter(typeof(Font));

                    string strFontString = converter.ConvertToString(this.Font);

                    this.MainForm.AppInfo.SetString(
                        this.FormName,
                        "default_font",
                        strFontString);
                }

                {
                    // Create the ColorConverter.
                    System.ComponentModel.TypeConverter converter =
                        System.ComponentModel.TypeDescriptor.GetConverter(typeof(Color));

                    string strFontColor = converter.ConvertToString(this.ForeColor);

                    MainForm.AppInfo.SetString(
                        this.FormName,
                        "default_font_color",
                        strFontColor);

                }
            }
        }

        /// <summary>
        /// �����ò����洢��װ������������Ϣ
        /// </summary>
        public void LoadFontSetting()
        {
            if (this.MainForm == null)
                return;

            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {


                string strFontString = MainForm.AppInfo.GetString(
                    this.FormName,
                    "default_font",
                    "");  // "Arial Unicode MS, 12pt"

                if (String.IsNullOrEmpty(strFontString) == false)
                {
                    // Create the FontConverter.
                    System.ComponentModel.TypeConverter converter =
                        System.ComponentModel.TypeDescriptor.GetConverter(typeof(Font));

                    this.Font = (Font)converter.ConvertFromString(strFontString);
                }
                else
                {
                    // ����ϵͳ��ȱʡ����
                    if (this.MainForm != null)
                    {
                        MainForm.SetControlFont(this, this.MainForm.DefaultFont);
                    }
                }

                string strFontColor = MainForm.AppInfo.GetString(
                        this.FormName,
                    "default_font_color",
                    "");

                if (String.IsNullOrEmpty(strFontColor) == false)
                {
                    // Create the ColorConverter.
                    System.ComponentModel.TypeConverter converter =
                        System.ComponentModel.TypeDescriptor.GetConverter(typeof(Color));

                    this.ForeColor = (Color)converter.ConvertFromString(strFontColor);
                }
            }

            this.PerformLayout();
        }

        /// <summary>
        /// Form װ���¼�
        /// </summary>
        /// <param name="e">�¼�����</param>
        protected override void OnLoad(EventArgs e)
        {
            this.OnMyFormLoad();
            base.OnLoad(e);

            this.LoadFontSetting();

            // ���ô��ڳߴ�״̬
            // һ����������� EntityForm_Load() ������
            /*
            this.MainForm.AppInfo.LoadMdiSize += new EventHandler(AppInfo_LoadMdiSize);
            this.MainForm.AppInfo.SaveMdiSize += new EventHandler(AppInfo_SaveMdiSize);
             * ��������Ժ�һ�㴦��ߴ��ʼ���Ǳ�Ҫ��
             * 
             * * */
            if (this.MainForm != null && this.MainForm.AppInfo != null
                && Floating == false && this.SupressSizeSetting == false)
            {
                this.MainForm.AppInfo.LoadMdiChildFormStates(this,
                        "mdi_form_state");
            }
        }

        /// <summary>
        /// Form �ر��¼�
        /// </summary>
        /// <param name="e">�¼�����</param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            this.OnMyFormClosed();

            base.OnFormClosed(e);
        }

        /// <summary>
        /// �� FormClosing �׶Σ��Ƿ�ҪԽ�� this.OnMyFormClosing(e)
        /// </summary>
        public bool SupressFormClosing = false;

        /// <summary>
        /// �Ƿ���Ҫ���Գߴ��趨�Ĺ���
        /// </summary>
        public bool SupressSizeSetting = false;

        /// <summary>
        /// Form �����ر��¼�
        /// </summary>
        /// <param name="e">�¼�����</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (this.SupressFormClosing == false)
                this.OnMyFormClosing(e);
        }

        /// <summary>
        /// Form �����¼�
        /// </summary>
        /// <param name="e">�¼�����</param>
        protected override void OnActivated(EventArgs e)
        {
            // if (this.stop != null)
                this.MainForm.stopManager.Active(this.stop);

            this.MainForm.MenuItem_font.Enabled = true;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = true;

            base.OnActivated(e);
        }

#if NO
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
        }
#endif

        // ��ʽУ�������
        // return:
        //      -2  ������û������У�鷽�����޷�У��
        //      -1  error
        //      0   ���ǺϷ��������
        //      1   �ǺϷ��Ķ���֤�����
        //      2   �ǺϷ��Ĳ������
        /// <summary>
        /// ��ʽУ�������
        /// </summary>
        /// <param name="strBarcode">ҪУ��������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>
        /// <para>-2  ������û������У�鷽�����޷�У��</para>
        /// <para>-1  ����</para>
        /// <para>0   ���ǺϷ��������</para>
        /// <para>1   �ǺϷ��Ķ���֤�����</para>
        /// <para>2   �ǺϷ��Ĳ������</para>
        /// </returns>
        public virtual int VerifyBarcode(
            string strLibraryCodeList,
            string strBarcode,
            out string strError)
        {
            strError = "";

            // EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("������֤����� " + strBarcode + "...");
            stop.BeginLoop();

            try
            {
                return this.MainForm.VerifyBarcode(
                    stop,
                    Channel,
                    strLibraryCodeList,
                    strBarcode,
                    EnableControls,
                    out strError);
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                // EnableControls(true);
            }
        }

        /// <summary>
        /// �����̨��� HTML
        /// </summary>
        /// <param name="strHtml">Ҫ����� HTML �ַ���</param>
        public void OutputHtml(string strHtml)
        {
            this.MainForm.OperHistory.AppendHtml(strHtml);
        }

        // parameters:
        //      nWarningLevel   0 �����ı�(��ɫ����) 1 �����ı�(��ɫ����) >=2 �����ı�(��ɫ����)
        /// <summary>
        /// �����̨������ı�
        /// </summary>
        /// <param name="strText">Ҫ����Ĵ��ı��ַ���</param>
        /// <param name="nWarningLevel">���漶��0 �����ı�(��ɫ����) 1 �����ı�(��ɫ����) >=2 �����ı�(��ɫ����)</param>
        public void OutputText(string strText, int nWarningLevel = 0)
        {
            string strClass = "normal";
            if (nWarningLevel == 1)
                strClass = "warning";
            else if (nWarningLevel >= 2)
                strClass = "error";
            this.MainForm.OperHistory.AppendHtml("<div class='debug " + strClass + "'>" + HttpUtility.HtmlEncode(strText).Replace("\r\n", "<br/>") + "</div>");
        }

        /// <summary>
        /// MDI�Ӵ��ڱ�֪ͨ�¼�����
        /// </summary>
        /// <param name="e">�¼�����</param>
        public virtual void OnNotify(ParamChangedEventArgs e)
        {

        }
    }
}
