using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using DigitalPlatform.GUI;

namespace DigitalPlatform.rms.Client
{
	/// <summary>
	/// ���չ�ϵ
	/// </summary>
	public class OriginNotFoundDlg : System.Windows.Forms.Form
	{
		public ServerCollection Servers = null;
		public RmsChannelCollection	Channels =null;	

		// Mapһ���������ⲿ������checkBox_notAskWhenSameOrigin.Checked==false
		// ����£�Map���Զ�����һ���������ԭ��������ù�ϵ��Ȼ���޸��Լ���ӵ�е��¶���
		// �Ի��򷵻غ󣬶Ի����������ʹ��Map�еĶ���ָ�롣���this.Map��ʾ�Ķ������¶���
		// �ڶԻ���ݻ�ʱ����Ȼ��һͬ��������
		public DbNameMap Map = null;



		public string Message = "";

		public string Origin = "";

		private System.Windows.Forms.RadioButton radioButton_skip;
		private System.Windows.Forms.RadioButton radioButton_overwrite;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBox_appendDbName;
		private System.Windows.Forms.Button button_findAppendDbName;
		private System.Windows.Forms.Button button_findOverwriteDbName;
		private System.Windows.Forms.TextBox textBox_overwriteDbName;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button button_OK;
		private System.Windows.Forms.Button button_Cancel;
		private System.Windows.Forms.Button button_editMap;
		private System.Windows.Forms.Label label_message;
		private System.Windows.Forms.RadioButton radioButton_append;
		private System.Windows.Forms.CheckBox checkBox_notAskWhenSameOrigin;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public OriginNotFoundDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OriginNotFoundDlg));
            this.label_message = new System.Windows.Forms.Label();
            this.radioButton_skip = new System.Windows.Forms.RadioButton();
            this.radioButton_append = new System.Windows.Forms.RadioButton();
            this.radioButton_overwrite = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_appendDbName = new System.Windows.Forms.TextBox();
            this.button_findAppendDbName = new System.Windows.Forms.Button();
            this.button_findOverwriteDbName = new System.Windows.Forms.Button();
            this.textBox_overwriteDbName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.button_OK = new System.Windows.Forms.Button();
            this.button_Cancel = new System.Windows.Forms.Button();
            this.button_editMap = new System.Windows.Forms.Button();
            this.checkBox_notAskWhenSameOrigin = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label_message
            // 
            this.label_message.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label_message.Location = new System.Drawing.Point(8, 8);
            this.label_message.Name = "label_message";
            this.label_message.Size = new System.Drawing.Size(483, 130);
            this.label_message.TabIndex = 0;
            // 
            // radioButton_skip
            // 
            this.radioButton_skip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radioButton_skip.Location = new System.Drawing.Point(8, 170);
            this.radioButton_skip.Name = "radioButton_skip";
            this.radioButton_skip.Size = new System.Drawing.Size(208, 24);
            this.radioButton_skip.TabIndex = 1;
            this.radioButton_skip.Text = "����[�������κ����ݿ�] (&S)";
            this.radioButton_skip.CheckedChanged += new System.EventHandler(this.radioButton_skip_CheckedChanged);
            // 
            // radioButton_append
            // 
            this.radioButton_append.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radioButton_append.Location = new System.Drawing.Point(8, 194);
            this.radioButton_append.Name = "radioButton_append";
            this.radioButton_append.Size = new System.Drawing.Size(208, 23);
            this.radioButton_append.TabIndex = 2;
            this.radioButton_append.Text = "׷�ӵ��������ݿ�(&A)";
            this.radioButton_append.CheckedChanged += new System.EventHandler(this.radioButton_append_CheckedChanged);
            // 
            // radioButton_overwrite
            // 
            this.radioButton_overwrite.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radioButton_overwrite.Location = new System.Drawing.Point(8, 250);
            this.radioButton_overwrite.Name = "radioButton_overwrite";
            this.radioButton_overwrite.Size = new System.Drawing.Size(208, 23);
            this.radioButton_overwrite.TabIndex = 3;
            this.radioButton_overwrite.Text = "���ǵ��������ݿ�(&O)";
            this.radioButton_overwrite.CheckedChanged += new System.EventHandler(this.radioButton_overwrite_CheckedChanged);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.Location = new System.Drawing.Point(24, 217);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 23);
            this.label2.TabIndex = 4;
            this.label2.Text = "����(&D):";
            // 
            // textBox_appendDbName
            // 
            this.textBox_appendDbName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_appendDbName.Location = new System.Drawing.Point(88, 217);
            this.textBox_appendDbName.Name = "textBox_appendDbName";
            this.textBox_appendDbName.Size = new System.Drawing.Size(363, 21);
            this.textBox_appendDbName.TabIndex = 5;
            // 
            // button_findAppendDbName
            // 
            this.button_findAppendDbName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_findAppendDbName.Location = new System.Drawing.Point(451, 217);
            this.button_findAppendDbName.Name = "button_findAppendDbName";
            this.button_findAppendDbName.Size = new System.Drawing.Size(40, 22);
            this.button_findAppendDbName.TabIndex = 6;
            this.button_findAppendDbName.Text = "...";
            this.button_findAppendDbName.Click += new System.EventHandler(this.button_findAppendDbName_Click);
            // 
            // button_findOverwriteDbName
            // 
            this.button_findOverwriteDbName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_findOverwriteDbName.Location = new System.Drawing.Point(451, 273);
            this.button_findOverwriteDbName.Name = "button_findOverwriteDbName";
            this.button_findOverwriteDbName.Size = new System.Drawing.Size(40, 22);
            this.button_findOverwriteDbName.TabIndex = 9;
            this.button_findOverwriteDbName.Text = "...";
            this.button_findOverwriteDbName.Click += new System.EventHandler(this.button_findOverwriteDbName_Click);
            // 
            // textBox_overwriteDbName
            // 
            this.textBox_overwriteDbName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_overwriteDbName.Location = new System.Drawing.Point(88, 273);
            this.textBox_overwriteDbName.Name = "textBox_overwriteDbName";
            this.textBox_overwriteDbName.Size = new System.Drawing.Size(363, 21);
            this.textBox_overwriteDbName.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.Location = new System.Drawing.Point(24, 273);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(64, 23);
            this.label3.TabIndex = 7;
            this.label3.Text = "����(&D):";
            // 
            // button_OK
            // 
            this.button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_OK.Location = new System.Drawing.Point(336, 346);
            this.button_OK.Name = "button_OK";
            this.button_OK.Size = new System.Drawing.Size(75, 22);
            this.button_OK.TabIndex = 10;
            this.button_OK.Text = "����";
            this.button_OK.Click += new System.EventHandler(this.button_OK_Click);
            // 
            // button_Cancel
            // 
            this.button_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Cancel.Location = new System.Drawing.Point(415, 346);
            this.button_Cancel.Name = "button_Cancel";
            this.button_Cancel.Size = new System.Drawing.Size(75, 22);
            this.button_Cancel.TabIndex = 11;
            this.button_Cancel.Text = "ȡ��";
            this.button_Cancel.Click += new System.EventHandler(this.button_Cancel_Click);
            // 
            // button_editMap
            // 
            this.button_editMap.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_editMap.Enabled = false;
            this.button_editMap.Location = new System.Drawing.Point(371, 138);
            this.button_editMap.Name = "button_editMap";
            this.button_editMap.Size = new System.Drawing.Size(120, 21);
            this.button_editMap.TabIndex = 12;
            this.button_editMap.Text = "�۲���ձ�...";
            this.button_editMap.Click += new System.EventHandler(this.button_editMap_Click);
            // 
            // checkBox_notAskWhenSameOrigin
            // 
            this.checkBox_notAskWhenSameOrigin.Location = new System.Drawing.Point(8, 296);
            this.checkBox_notAskWhenSameOrigin.Name = "checkBox_notAskWhenSameOrigin";
            this.checkBox_notAskWhenSameOrigin.Size = new System.Drawing.Size(464, 24);
            this.checkBox_notAskWhenSameOrigin.TabIndex = 13;
            this.checkBox_notAskWhenSameOrigin.Text = "�Ժ�������ͬ�������ѯ��(&N)";
            // 
            // OriginNotFoundDlg
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(499, 377);
            this.Controls.Add(this.checkBox_notAskWhenSameOrigin);
            this.Controls.Add(this.button_editMap);
            this.Controls.Add(this.button_Cancel);
            this.Controls.Add(this.button_OK);
            this.Controls.Add(this.button_findOverwriteDbName);
            this.Controls.Add(this.textBox_overwriteDbName);
            this.Controls.Add(this.textBox_appendDbName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.button_findAppendDbName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.radioButton_overwrite);
            this.Controls.Add(this.radioButton_append);
            this.Controls.Add(this.radioButton_skip);
            this.Controls.Add(this.label_message);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "OriginNotFoundDlg";
            this.Text = "��ָ�����Ƿ�ʽ";
            this.Load += new System.EventHandler(this.OriginNotFoundDlg_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		private void OriginNotFoundDlg_Load(object sender, System.EventArgs e)
		{
		
			label_message.Text = this.Message;
			radioButton_skip.Checked = true;
			Checked();
		}

		private void button_editMap_Click(object sender, System.EventArgs e)
		{
		
		}

		private void button_findAppendDbName_Click(object sender, System.EventArgs e)
		{
			OpenResDlg dlg = new OpenResDlg();
            Font font = GuiUtil.GetDefaultFont();
            if (font != null)
                dlg.Font = font;

			dlg.Text = "��ѡ��Ҫ׷�ӵ�Ŀ�����ݿ�";
			dlg.EnabledIndices = new int[] { ResTree.RESTYPE_DB };
			//dlg.ap = this.applicationInfo;
			//dlg.ApCfgTitle = "pageimport_openresdlg";
			dlg.MultiSelect = false;
			dlg.Path = textBox_appendDbName.Text;
			dlg.Initial( this.Servers,
				this.Channels);	
			dlg.StartPosition = FormStartPosition.CenterScreen;
			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
				return;

			textBox_appendDbName.Text = dlg.Path;
		}

		private void button_findOverwriteDbName_Click(object sender, System.EventArgs e)
		{
			OpenResDlg dlg = new OpenResDlg();
            Font font = GuiUtil.GetDefaultFont();
            if (font != null)
                dlg.Font = font;

			dlg.Text = "��ѡ��Ҫ���ǵ�Ŀ�����ݿ�";
			dlg.EnabledIndices = new int[] { ResTree.RESTYPE_DB };
			//dlg.ap = this.applicationInfo;
			//dlg.ApCfgTitle = "pageimport_openresdlg";
			dlg.MultiSelect = false;
			dlg.Path = textBox_overwriteDbName.Text;
			dlg.Initial( this.Servers,
				this.Channels);	
			dlg.StartPosition = FormStartPosition.CenterScreen;
			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
				return;

			textBox_overwriteDbName.Text = dlg.Path;
		}

		private void button_OK_Click(object sender, System.EventArgs e)
		{
			string strTarget = "";
			string strStyle = "";
			if (radioButton_skip.Checked == true) 
			{
				strStyle = "skip";
			}

			if (radioButton_append.Checked == true)
			{
				if (this.textBox_appendDbName.Text == "") 
				{
					MessageBox.Show(this, "��ѡ����׷�ӷ�ʽ������£�����ѡ��Ŀ���...");
					return;
				}
				strTarget = this.textBox_appendDbName.Text;
				strStyle = "append";
			}

			if (radioButton_overwrite.Checked == true)
			{
				if (this.textBox_overwriteDbName.Text == "") 
				{
					MessageBox.Show(this, "��ѡ���˸��Ƿ�ʽ������£�����ѡ��Ŀ���...");
					return;
				}
				strTarget = this.textBox_overwriteDbName.Text;
				strStyle = "overwrite";
			}

			// ���Ҫ�������������ã���Ҫ���Map���Ա�Ի������ʹ�ú��Զ�����
			if (checkBox_notAskWhenSameOrigin.Checked == false) 
			{
				this.Map = this.Map.Clone();
			}

            string strError = "";
            if (Map.NewItem(Origin, strTarget, strStyle, out strError) == null)
            {
                MessageBox.Show(this, strError);
                return;
            }

			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void button_Cancel_Click(object sender, System.EventArgs e)
		{

			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void radioButton_skip_CheckedChanged(object sender, System.EventArgs e)
		{
			Checked();
		}

		private void radioButton_append_CheckedChanged(object sender, System.EventArgs e)
		{
			Checked();
		}

		private void radioButton_overwrite_CheckedChanged(object sender, System.EventArgs e)
		{
			Checked();
		}

		void Checked()
		{
			if (radioButton_skip.Checked == true)
			{
				textBox_appendDbName.Enabled = false;
				textBox_overwriteDbName.Enabled = false;

				button_findAppendDbName.Enabled = false;
				button_findOverwriteDbName.Enabled = false;
			}
			if (radioButton_append.Checked == true)
			{
				textBox_appendDbName.Enabled = true;
				button_findAppendDbName.Enabled = true;

				textBox_overwriteDbName.Enabled = false;
				button_findOverwriteDbName.Enabled = false;
			}
			if (radioButton_overwrite.Checked == true)
			{
				textBox_appendDbName.Enabled = false;
				button_findAppendDbName.Enabled = false;

				textBox_overwriteDbName.Enabled = true;
				button_findOverwriteDbName.Enabled = true;
			}
		}



	}
}
