using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml.XPath;
using System.Xml;
using System.IO;

//using DigitalPlatform.XmlEditor;
using DigitalPlatform.GUI;
using DigitalPlatform.IO;
using DigitalPlatform.Range;
using DigitalPlatform.Xml;
using DigitalPlatform.rms;
using DigitalPlatform.rms.Client;
using DigitalPlatform;

namespace dp2rms
{
	public class ResFileList : ListViewNF
	{
		public XmlEditor editor = null;	// ��������������XmlEditor

		public const int COLUMN_ID = 0;
		public const int COLUMN_STATE = 1;
		public const int COLUMN_LOCALPATH = 2;
		public const int COLUMN_SIZE = 3;
		public const int COLUMN_MIME = 4;
		public const int COLUMN_TIMESTAMP = 5;

		bool bNotAskTimestampMismatchWhenOverwrite = false;

		public Delegate_DownloadFiles procDownloadFiles = null;
		public Delegate_DownloadOneMetaData procDownloadOneMetaData = null;


		Hashtable m_tableFileId = new Hashtable();

		bool m_bChanged = false;

		private System.Windows.Forms.ColumnHeader columnHeader_state;
		private System.Windows.Forms.ColumnHeader columnHeader_serverName;
		private System.Windows.Forms.ColumnHeader columnHeader_localPath;
		private System.Windows.Forms.ColumnHeader columnHeader_size;
		private System.Windows.Forms.ColumnHeader columnHeader_mime;
		private System.Windows.Forms.ColumnHeader columnHeader_timestamp;

		private System.ComponentModel.Container components = null;

		public ResFileList()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitComponent call
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.columnHeader_state = new System.Windows.Forms.ColumnHeader();
			this.columnHeader_serverName = new System.Windows.Forms.ColumnHeader();
			this.columnHeader_localPath = new System.Windows.Forms.ColumnHeader();
			this.columnHeader_size = new System.Windows.Forms.ColumnHeader();
			this.columnHeader_mime = new System.Windows.Forms.ColumnHeader();
			this.columnHeader_timestamp = new System.Windows.Forms.ColumnHeader();
			// 
			// columnHeader_state
			// 
			this.columnHeader_state.Text = "״̬";
			this.columnHeader_state.Width = 100;
			// 
			// columnHeader_serverName
			// 
			this.columnHeader_serverName.Text = "�������˱���";
			this.columnHeader_serverName.Width = 200;
			// 
			// columnHeader_localPath
			// 
			this.columnHeader_localPath.Text = "��������·��";
			this.columnHeader_localPath.Width = 200;
			// 
			// columnHeader_size
			// 
			this.columnHeader_size.Text = "�ߴ�";
			this.columnHeader_size.Width = 100;
			// 
			// columnHeader_mime
			// 
			this.columnHeader_mime.Text = "ý������";
			this.columnHeader_mime.Width = 200;
			// 
			// columnHeader_timestamp
			// 
			this.columnHeader_timestamp.Text = "ʱ���";
			this.columnHeader_timestamp.Width = 200;
			// 
			// ResFileList
			// 
			this.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																			  this.columnHeader_serverName,
																			  this.columnHeader_state,
																			  this.columnHeader_localPath,
																			  this.columnHeader_size,
																			  this.columnHeader_mime,
																			  this.columnHeader_timestamp});
			this.FullRowSelect = true;
			this.HideSelection = false;
			this.View = System.Windows.Forms.View.Details;
			this.DoubleClick += new System.EventHandler(this.ResFileList_DoubleClick);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ResFileList_MouseUp);

		}
		#endregion


		public bool Changed
		{
			get 
			{
				return m_bChanged;
			}
			set 
			{
				m_bChanged = value;
			}
		}


		// ��ʼ���б�����
		// parameters:
		public void Initial(XmlEditor editor)
		{
			//this.Items.Clear();
			m_tableFileId.Clear();

			this.editor = editor;

			this.editor.ItemCreated -=  new ItemCreatedEventHandle(this.ItemCreated);
			this.editor.ItemChanged -=  new ItemChangedEventHandle(this.ItemChanged);
			this.editor.BeforeItemCreate -=  new BeforeItemCreateEventHandle(this.BeforeItemCreate);
			this.editor.ItemDeleted -= new ItemDeletedEventHandle(this.ItemDeleted);


			this.editor.ItemCreated +=  new ItemCreatedEventHandle(this.ItemCreated);
			this.editor.ItemChanged +=  new ItemChangedEventHandle(this.ItemChanged);
			this.editor.BeforeItemCreate +=  new BeforeItemCreateEventHandle(this.BeforeItemCreate);
			this.editor.ItemDeleted += new ItemDeletedEventHandle(this.ItemDeleted);


		}

		#region �ӹܵ��¼�

		bool IsFileElement(DigitalPlatform.Xml.Item item)
		{
			if (!(item is ElementItem))
				return false;

			ElementItem element = (ElementItem)item;

			if (element.LocalName != "file")
				return false;

			if (element.NamespaceURI == DpNs.dprms )
				return true;

			return false;
		}

		// �ӹ�XmlEditor�и�����������<file>������¼�
		void BeforeItemCreate(object sender,
			DigitalPlatform.Xml.BeforeItemCreateEventArgs e)
		{
			/*
			if (!(e.item is ElementItem))
				return;

			if (IsFileElement(e.item) == false)
				return;

			ResObjectDlg dlg = new ResObjectDlg();
			dlg.ShowDialog(this);
			if (dlg.DialogResult != DialogResult.OK) 
			{
				e.Cancel = true;
				return;
			}

			SetItemProperty(editor, 
				(ElementItem)e.item,
				dlg.textBox_mime.Text,
				dlg.textBox_localPath.Text,
				dlg.textBox_size.Text);

			return;
			*/
		}


		void ItemCreated(object sender,
			DigitalPlatform.Xml.ItemCreatedEventArgs e)
		{
			if (e.item is AttrItem)
			{
				ElementItem parent = e.item.parent;

				if (parent == null)
					return;

				if (this.IsFileElement(parent) == false)
					return;

				/*
				string strId = parent.GetAttrValue("id");
				if (strId == null || strId == "")
					return;
				*/

				if (e.item.Name == "id") 
				{
					ChangeFileAttr((AttrItem)e.item,
						"",
						e.item.Value);
				}
				else 
				{
					ChangeFileAttr((AttrItem)e.item,
						null,
						e.item.Value);
				}

				return;
			}


			if (!(e.item is ElementItem))
				return;

			if (IsFileElement(e.item) == false)
				return;

			ElementItem element = (ElementItem)e.item;


			// ��������ʱ�Ƿ��Ѿ���id����
			string strID = element.GetAttrValue("id");

			// �ͻ���
			if (strID == null || strID == "")
			{
				NewLine(element,
					true);

				ResObjectDlg dlg = new ResObjectDlg();
                dlg.Font = GuiUtil.GetDefaultFont();
                dlg.ShowDialog(this);
				if (dlg.DialogResult != DialogResult.OK) 
				{
					// e.Cancel = true;
					// ɾ���ոմ�����element
					ElementItem parent = element.parent;
					parent.Remove(element);
					return;
				}

				// ֱ�Ӷ�xmleditor�����޸�
				element.SetAttrValue("__mime",dlg.textBox_mime.Text);
				element.SetAttrValue("__localpath",dlg.textBox_localPath.Text);
				element.SetAttrValue("__size",dlg.textBox_size.Text);

				strID =  NewFileId();

				// �õ���id
				if (m_tableFileId.Contains((object)strID) == false)
					m_tableFileId.Add(strID, (object)true);

				element.SetAttrValue("id", strID);

				/*
				SetItemProperty(editor, 
					(ElementItem)e.item,
					dlg.textBox_mime.Text,
					dlg.textBox_localPath.Text,
					dlg.textBox_size.Text);
				NewLine((ElementItem)e.item,
					true);
				*/

			}
			else // ���Է������˵�
			{

				string strState = element.GetAttrValue("__state");

				if (strState == null || strState == "")
				{
					NewLine(element,
						false);
					GetMetaDataParam(element);
				}
				else 
				{
					
					NewLine(element,
						IsNewFileState(strState));


					// ����ȫ��xml����
					ChangeLine(strID, 
						null,	// newid
						element.GetAttrValue("__state"),
						element.GetAttrValue("__localpath"),
						element.GetAttrValue("__mime"),
						element.GetAttrValue("__size"),
						element.GetAttrValue("__timestamp"));
				}
			}

		}

		
		
		void ItemDeleted(object sender,
			DigitalPlatform.Xml.ItemDeletedEventArgs e)
		{

			if (!(e.item is ElementItem))
				return;

			e.RecursiveChildEvents = true;
			e.RiseAttrsEvents = true;

			// e.item�л��д�ԭ�е�id
			if (IsFileElement(e.item) == false)
				return;

			string strID = ((ElementItem)e.item).GetAttrValue("id");

			// m_tableFileId.Remove((object)strID);	// �˾�������ɾ�����id�ظ�ʹ�õ�Ч��

			DeleteLineById(strID);

		}

	
		void ItemChanged(object sender,
			DigitalPlatform.Xml.ItemChangedEventArgs e)
		{

			if (!(e.item is AttrItem))
				return;	// ֻ�������Ըı�

			ElementItem parent = (ElementItem)e.item.parent;


			if (parent == null)
			{
				// �ڵ���δ����
				return;
			}

			// e.item�Ѿ���һ�����Խ��
			ChangeFileAttr((AttrItem)e.item,
				e.OldValue,
				e.NewValue);

		}


		void ChangeFileAttr(AttrItem attr,
			string strOldValue,
			string strNewValue)
		{
			ElementItem parent = attr.parent;

			string strID = parent.GetAttrValue("id");
			if (strID == null)
				strID = "";

			if (attr.Name == "id") 
			{
				ChangeLine(strOldValue, strNewValue, null, null, null, null, null);
			}

			else if (attr.Name == "__mime") 
			{

				ChangeLine(strID, null, null, null, strNewValue, null, null);
			}


			else if (attr.Name == "__localpath") 
			{
				ChangeLine(strID, null, null, strNewValue, null, null, null);
			}

			else if (attr.Name == "__state") 
			{
				ChangeLine(strID, null, strNewValue, null, null, null, null);
			}

			else if (attr.Name == "__size") 
			{
				ChangeLine(strID, null, null, null, null, strNewValue, null);
			}
			else if (attr.Name == "__timestamp") 
			{
				ChangeLine(strID, null, null, null, null, null, strNewValue);
			}
		}
		
		#endregion

		// ���б��м���һ���Ƿ����
		public ListViewItem SearchLine(string strID)
		{
			for(int i=0;i<this.Items.Count;i++)
			{
				if (ListViewUtil.GetItemText(this.Items[i], COLUMN_ID) == strID)
				{
					return this.Items[i];
				}
			}
			return null;
		}

		// ?
		// ��listview��һ��,��������Ѵ��ڣ����޸�������
		public void NewLine(DigitalPlatform.Xml.ElementItem fileitem,
			bool bIsNewFile)
		{
			string strID = fileitem.GetAttrValue("id");

			if (strID == null || strID == "")
			{
				Debug.Assert(bIsNewFile == true, "�����ǿͻ����ļ�������id����");
			}

			string strState;
			if (bIsNewFile == false)
				strState = this.ServerFileState;
			else
				strState = this.NewFileState;


			// ά��id��
			if (strID != null && strID != "") 
			{
				if (m_tableFileId.Contains((object)strID) == false)
					m_tableFileId.Add(strID, (object)true);
			}



			ListViewItem item = SearchLine(strID);
            if (item == null)
            {
                item = new ListViewItem(strID, 0);
                this.Items.Add(item);

                item.SubItems.Add("");
                item.SubItems.Add("");
                item.SubItems.Add("");
                item.SubItems.Add("");
                item.SubItems.Add("");
                m_bChanged = true;

            }
            else
            {
                // 2006/6/22
                // �ظ�����.
                // �������Ѿ����ֵ�����ǰ��
                int index = this.Items.IndexOf(item);

                item = new ListViewItem(strID, 0);
                this.Items.Insert(index, item);

                item.SubItems.Add("");
                item.SubItems.Add("");
                item.SubItems.Add("");
                item.SubItems.Add("");
                item.SubItems.Add("");
                m_bChanged = true;
            }

			string strOldState = fileitem.GetAttrValue("__state");
			if (strOldState != strState)
				fileitem.SetAttrValue("__state", strState);
		}

		// �ӷ��������Ԫ����
		public void GetMetaDataParam(DigitalPlatform.Xml.ElementItem element)
		{
			string strID = element.GetAttrValue("id");

			if (strID == null || strID == "")
			{
				Debug.Assert(false);
			}


			// ���û�йһص�����������
			if (this.procDownloadOneMetaData == null) 
			{
				element.SetAttrValue("__mime", "?mime");
				element.SetAttrValue("__localpath", "?localpath");
				element.SetAttrValue("__size", "?size");
				element.SetAttrValue("__timestamp", "?timestamp");
				m_bChanged = true;
			}
			else 
			{

				string strExistTimeStamp = element.GetAttrValue("__timestamp");



				if (strExistTimeStamp != null && strExistTimeStamp != "")
				{
					ChangeLine(strID, 
						null,	// newid
						null,	// state
						element.GetAttrValue("__localpath"),
						element.GetAttrValue("__mime"),
						element.GetAttrValue("__size"),
						element.GetAttrValue("__timestamp") );
					return;
				}

				m_bChanged = true;

				string strMetaData = "";
				string strError = "";
				byte [] timestamp = null;
				int nRet = this.procDownloadOneMetaData(
					strID, 
					out strMetaData,
					out timestamp,
					out strError);
				if (nRet == -1) 
				{
					element.SetAttrValue("__localpath",
						strError);
					return;
				}

				if (strMetaData == "")
				{
					return;
				}

				// ȡmetadata
				Hashtable values = rmsUtil.ParseMedaDataXml(strMetaData,
					out strError);
				if (values == null)
				{
					element.SetAttrValue("__localpath",
						strError);
					return;
				}

				string strTimeStamp = ByteArray.GetHexTimeStampString(timestamp);

				element.SetAttrValue("__mime",
					(string)values["mimetype"]);
				element.SetAttrValue("__localpath",
					(string)values["localpath"]);
				element.SetAttrValue("__size",
					(string)values["size"]);
				element.SetAttrValue("__timestamp",
					(string)strTimeStamp);
			}
		}


	// ɾ��һ��(��ʱ����)
		void DeleteLineById(string strId)
		{
			bool bFound = false;
			// 1.�ȸ��ݴ�����idɾ�������
			for(int i=0;i<this.Items.Count;i++)
			{
				if (ListViewUtil.GetItemText(this.Items[i], COLUMN_ID) == strId)
				{
					this.Items.RemoveAt(i);
					bFound = true;
					m_bChanged = true;
					break;
				}
			}

			if (bFound == false) 
			{
				Debug.Assert(false, "id[" + strId + "]��listview��û���ҵ�...");
			}
		}

		
		// �����¼����޸�listviewһ��
		void ChangeLine(string strID, 
			string strNewID,
			string strState,
			string strLocalPath,
			string strMime,
			string strSize,
			string strTimestamp)
		{

			for(int i=0;i<this.Items.Count;i++)
			{
				if (ListViewUtil.GetItemText(this.Items[i], COLUMN_ID) == strID) 
				{
					if (strNewID != null)
						ListViewUtil.ChangeItemText(this.Items[i], COLUMN_ID, 
							strNewID);
					if (strState != null)
						ListViewUtil.ChangeItemText(this.Items[i], COLUMN_STATE, 
							strState);
					if (strLocalPath != null)
						ListViewUtil.ChangeItemText(this.Items[i], COLUMN_LOCALPATH, 
							strLocalPath);
					if (strMime != null)
						ListViewUtil.ChangeItemText(this.Items[i], COLUMN_MIME, 
							strMime);
					if (strSize != null)
						ListViewUtil.ChangeItemText(this.Items[i], COLUMN_SIZE, 
							strSize);
					if (strTimestamp != null)
						ListViewUtil.ChangeItemText(this.Items[i], COLUMN_TIMESTAMP, 
							strTimestamp);

					m_bChanged = true;

					break;
				}
			}

		}




		private void ResFileList_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{

			if(e.Button != MouseButtons.Right)
				return;

			ContextMenu contextMenu = new ContextMenu();
			MenuItem menuItem = null;

			bool bSelected = this.SelectedItems.Count > 0;

			//
			menuItem = new MenuItem("�޸�(&M)");
			menuItem.Click += new System.EventHandler(this.button_modifyFile_Click);
			if (bSelected == false) 
			{
				menuItem.Enabled = false;
			}
			contextMenu.MenuItems.Add(menuItem);

			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);


			menuItem = new MenuItem("ɾ��(&D)");
			menuItem.Click += new System.EventHandler(this.DeleteLines_Click);
			if (bSelected == false)
				menuItem.Enabled = false;
			contextMenu.MenuItems.Add(menuItem);

			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);


			//
			menuItem = new MenuItem("����(&N)");
			menuItem.Click += new System.EventHandler(this.NewLine_Click);
			contextMenu.MenuItems.Add(menuItem);


			// ---
			menuItem = new MenuItem("-");
			contextMenu.MenuItems.Add(menuItem);

			//
			menuItem = new MenuItem("����(&D)");
			menuItem.Click += new System.EventHandler(this.DownloadLine_Click);
			bool bFound = false;
			if (bSelected == true) 
			{
				for(int i=0;i<this.SelectedItems.Count;i++) 
				{
					if (IsNewFileState(ListViewUtil.GetItemText(this.SelectedItems[i], COLUMN_STATE) ) == false) 
					{
						bFound = true;
						break;
					}
				}
			}


			if (bFound == false || procDownloadFiles == null)
				menuItem.Enabled = false;
			contextMenu.MenuItems.Add(menuItem);


			/*
			menuItem = new MenuItem("����");
			menuItem.Click += new System.EventHandler(this.Test_click);
			contextMenu.MenuItems.Add(menuItem);
			
			*/


			contextMenu.Show(this, new Point(e.X, e.Y) );		
			
		}

		public void Test_click(object sender, System.EventArgs e)
		{
			XmlNamespaceManager mngr = new XmlNamespaceManager(new NameTable());
			mngr.AddNamespace("dprms", DpNs.dprms);
			
			ItemList fileItems = this.editor.VirtualRoot.SelectItems("//dprms:file",
				mngr);

			MessageBox.Show("ѡ��" + Convert.ToString(fileItems.Count) + "��");

		}

		DigitalPlatform.Xml.ElementItem GetFileItem(string strID)
		{
			XmlNamespaceManager mngr = new XmlNamespaceManager(new NameTable());
			mngr.AddNamespace("dprms", DpNs.dprms);

			//StreamUtil.WriteText("I:\\debug.txt","dprms��ֵ:'" + DpNs.dprms + "'\r\n");

			//mngr.AddNamespace("abc","http://purl.org/dc/elements/1.1/");
			ItemList items = this.editor.VirtualRoot.SelectItems("//dprms:file[@id='" + strID + "']",
				mngr);
			if (items.Count == 0) 
				return null;

			return (ElementItem)items[0];
		}
		
		// �˵�:ɾ��һ�л��߶���
		void DeleteLines_Click(object sender, System.EventArgs e)
		{
			if (this.SelectedItems.Count == 0) 
			{
				MessageBox.Show(this, "��δѡ��Ҫɾ������...");
				return;
			}
			string[] ids = new string[this.SelectedItems.Count];

			for(int i=0;i<ids.Length;i++) 
			{
				ids[i] = this.SelectedItems[i].Text;
			}

			for(int i=0;i<ids.Length;i++) 
			{
				/*
				XmlNamespaceManager mngr = new XmlNamespaceManager(new NameTable());
				mngr.AddNamespace("dprms", DpNs.dprms);
				ItemList items = this.editor.SelectItems("//dprms:file[@id='" + ids[i]+ "']",
					mngr);
				if (items.Count == 0) 
				{
					MessageBox.Show(this, "����: idΪ[" +ids[i]+ "]��<dprms:file>Ԫ����editor�в�����...");
				}
				else 
				{
					this.editor.Remove(items[0]);	// ��Ȼ�ᴥ���¼�,����listview
				}
				*/

				DigitalPlatform.Xml.Item item = GetFileItem(ids[i]);
				if (item == null) 
				{
					MessageBox.Show(this, "����: idΪ[" +ids[i]+ "]��<dprms:file>Ԫ����editor�в�����...");
					continue;
				}

				ElementItem parent = item.parent;
				parent.Remove(item);	// ��Ȼ�ᴥ���¼�,����listview

				m_bChanged = true;
			}

		}

		// �˵����޸�һ��
		void button_modifyFile_Click(object sender, System.EventArgs e)
		{
			if (this.SelectedItems.Count == 0) 
			{
				MessageBox.Show(this, "��δѡ��Ҫ�޸ĵ���...");
				return ;
			}
			ResObjectDlg dlg = new ResObjectDlg();
            dlg.Font = GuiUtil.GetDefaultFont();
            dlg.textBox_serverName.Text = ListViewUtil.GetItemText(this.SelectedItems[0], COLUMN_ID);
			dlg.textBox_state.Text = ListViewUtil.GetItemText(this.SelectedItems[0], COLUMN_STATE);
			dlg.textBox_mime.Text = ListViewUtil.GetItemText(this.SelectedItems[0], COLUMN_MIME);
			dlg.textBox_localPath.Text = ListViewUtil.GetItemText(this.SelectedItems[0], COLUMN_LOCALPATH);
			dlg.textBox_size.Text = ListViewUtil.GetItemText(this.SelectedItems[0], COLUMN_SIZE);
			dlg.textBox_timestamp.Text = ListViewUtil.GetItemText(this.SelectedItems[0], COLUMN_TIMESTAMP);

			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
				return;

			Cursor cursorSave = Cursor;
			Cursor = Cursors.WaitCursor;
			this.Enabled = false;

			DigitalPlatform.Xml.ElementItem item = GetFileItem(dlg.textBox_serverName.Text);

			if (item != null) 
			{
				item.SetAttrValue("__mime", dlg.textBox_mime.Text);
				item.SetAttrValue("__localpath", dlg.textBox_localPath.Text);
				item.SetAttrValue("__state", this.NewFileState);
				item.SetAttrValue("__size", dlg.textBox_size.Text);
				item.SetAttrValue("__timestamp", dlg.textBox_timestamp.Text);

				m_bChanged = true;
			}
			else 
			{
				Debug.Assert(false, "xmleditor�о�Ȼ������idΪ["
					+ dlg.textBox_serverName.Text 
					+ "]��<dprms:file>Ԫ��");
			}

			this.Enabled = true;
			Cursor = cursorSave;


		}

		
		// �˵�������һ�л����
		void DownloadLine_Click(object sender, System.EventArgs e)
		{
			bool bFound = false;
			for(int i=0;i<this.SelectedItems.Count;i++) 
			{
				if (IsNewFileState(ListViewUtil.GetItemText(this.SelectedItems[i], COLUMN_STATE)) == false) 
				{
					bFound = true;
				}
			}

			if (bFound == false) 
			{
				MessageBox.Show(this, "��δѡ��Ҫ���ص����������ѡ���������û��״̬Ϊ'������'������...");
				return;
			}

			if (procDownloadFiles == null)
			{
				MessageBox.Show(this, "procDownloadFiles��δ����...");
				return;
			}

			procDownloadFiles();

		}

		// �˵�������һ��
		void NewLine_Click(object sender, System.EventArgs e)
		{
			/*
			ResObjectDlg dlg = new ResObjectDlg();
			dlg.ShowDialog(this);

			if (dlg.DialogResult != DialogResult.OK)
				return;
			*/

			DigitalPlatform.Xml.ElementItem fileitem = null;
			fileitem = CreateFileElementItem(editor);

			// string strError;

			try 
			{
				editor.DocumentElement.AutoAppendChild(fileitem);	
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}

			/*
			fileitem.SetAttrValue("__mime", dlg.textBox_mime.Text);
			fileitem.SetAttrValue("__localpath", dlg.textBox_localPath.Text);
			fileitem.SetAttrValue("__size", dlg.textBox_size.Text);

			*/
			m_bChanged = true;

			// -2Ϊ�û�ȡ����״̬
		}


		

		// ������Դ�ڵ㺯��
		static ElementItem CreateFileElementItem(XmlEditor editor)
		{
			ElementItem item = editor.CreateElementItem("dprms", 
				"file",
				DpNs.dprms);

			return item;
		}


		string NewFileId()
		{
			int nSeed = 0;
			string strID = "";
			for(;;) {
				strID = Convert.ToString(nSeed++);
				if (m_tableFileId.Contains((object)strID) == false)
					return strID;
			}

		}

		/*
		// ����<file>Ԫ�����е�����
		// ���½�Ԫ�ص�ʱ����˺���
		void SetItemProperty(XmlEditor editor,
			DigitalPlatform.Xml.ElementItem item,
			string strMime,
			string strLocalPath,
			string strSize)
		{
			AttrItem attr = null;

			// id����
			attr = editor.CreateAttrItem("id");
			attr.Value = NewFileId();	// editor.GetFileNo(null);
			item.AppendAttr(attr);

			// ��__mime����
			attr = editor.CreateAttrItem("__mime");
			attr.Value = strMime;
			item.AppendAttr(attr);


			// ��__localpath����
			attr = editor.CreateAttrItem("__localpath");
			attr.Value = strLocalPath;
			item.AppendAttr (attr);

			// __size����
			attr = editor.CreateAttrItem("__size");
			attr.Value = strSize;
			item.AppendAttr(attr);

			// __state����
			attr = editor.CreateAttrItem("__state");
			attr.Name = "__state";
			attr.Value = this.NewFileState;
			item.AppendAttr(attr);
			// ��������ڶ�Ӧ����Դ�������Ҫ�Ĺ���

			m_bChanged = true;
		}
		*/

#if NO
		public static void ChangeMetaData(ref string strMetaData,
			string strID,
			string strLocalPath,
			string strMimeType,
			string strLastModified,
			string strPath,
			string strTimestamp)
		{
			XmlDocument dom = new XmlDocument();

			if (strMetaData == "")
				strMetaData = "<file/>";

			dom.LoadXml(strMetaData);

			if (strID != null)
				DomUtil.SetAttr(dom.DocumentElement, "id", strID);

			if (strLocalPath != null)
				DomUtil.SetAttr(dom.DocumentElement, "localpath", strLocalPath);

			if (strMimeType != null)
				DomUtil.SetAttr(dom.DocumentElement, "mimetype", strMimeType);

			if (strLastModified != null)
				DomUtil.SetAttr(dom.DocumentElement, "lastmodified", strLastModified);

			if (strPath != null)
				DomUtil.SetAttr(dom.DocumentElement, "path", strPath);

			if (strTimestamp != null)
				DomUtil.SetAttr(dom.DocumentElement, "timestamp", strTimestamp);


			strMetaData = dom.OuterXml;
		}
#endif

		// �Ӵ����в��localpath
		public string GetLocalFileName(string strID)
		{
			for(int i=0;i<this.Items.Count;i++)
			{
				string strCurID = ListViewUtil.GetItemText(this.Items[i], COLUMN_ID);
				string strLocalFileName = ListViewUtil.GetItemText(this.Items[i], COLUMN_LOCALPATH);

				if (strID == strCurID)
					return strLocalFileName;
			}

			return "";
		}

		// ������Դ�����浽�����ļ�
		public int DoSaveResToBackupFile(
			Stream outputfile,
			string strXmlRecPath,
			RmsChannel channel,
			DigitalPlatform.Stop stop,
			out string strError)
		{
			strError = "";

			string strTempFileName = Path.GetTempFileName();
			try 
			{
				long lRet;

				for(int i=0;i<this.Items.Count;i++)
				{
					string strState = ListViewUtil.GetItemText(this.Items[i], COLUMN_STATE);
					string strID = ListViewUtil.GetItemText(this.Items[i], COLUMN_ID);
					string strResPath = strXmlRecPath + "/object/" + ListViewUtil.GetItemText(this.Items[i], COLUMN_ID);
					string strLocalFileName = ListViewUtil.GetItemText(this.Items[i], COLUMN_LOCALPATH);
					string strMime =  ListViewUtil.GetItemText(this.Items[i], COLUMN_MIME);

					string strMetaData;

					// ���������ļ�
					if (IsNewFileState(strState) == false) 
					{
						if (stop != null)
							stop.SetMessage("�������� " + strResPath);

						byte [] baOutputTimeStamp = null;
						string strOutputPath;

						lRet = channel.GetRes(strResPath,
							strTempFileName,
							stop,
							out strMetaData,
							out baOutputTimeStamp,
							out strOutputPath,
							out strError);
						if (lRet == -1)
							return -1;

						ResPath respath = new ResPath();
						respath.Url = channel.Url;
						respath.Path = strResPath;

						// strMetaData��Ҫ������Դid?
						ExportUtil.ChangeMetaData(ref strMetaData,
							strID,
							null,
							null,
							null,
							respath.FullPath,
							ByteArray.GetHexTimeStampString(baOutputTimeStamp));


						lRet = Backup.WriteOtherResToBackupFile(outputfile,
							strMetaData,
							strTempFileName);

					}
					else // �������ļ�
					{
						if (stop != null)
							stop.SetMessage("���ڸ��� " + strLocalFileName);

						// strMetaData = "<file mimetype='"+ strMime+"' localpath='"+strLocalPath+"' id='"+strID+"'></file>";

						ResPath respath = new ResPath();
						respath.Url = channel.Url;
						respath.Path = strResPath;

						strMetaData = "";
						FileInfo fi = new FileInfo(strLocalFileName);
						ExportUtil.ChangeMetaData(ref strMetaData,
							strID,
							strLocalFileName,
							strMime,
							fi.LastWriteTimeUtc.ToString(),
							respath.FullPath,
							"");

						lRet = Backup.WriteOtherResToBackupFile(outputfile,
							strMetaData,
							strLocalFileName);

					}

				}

				if (stop != null)
					stop.SetMessage("������Դ�������ļ�ȫ�����");

			}
			finally 
			{

				if (strTempFileName != "")
					File.Delete(strTempFileName);
			}

			return 0;
		}


		// ������Դ
		// return:
		//		-1	error
		//		>=0 ʵ�����ص���Դ������
		public int DoUpload(
			string strXmlRecPath,
			RmsChannel channel,
			DigitalPlatform.Stop stop,
			out string strError)
		{
			strError = "";
            
			int nUploadCount = 0;

            string strLastModifyTime = DateTime.UtcNow.ToString("u");

			bNotAskTimestampMismatchWhenOverwrite = false;

			for(int i=0;i<this.Items.Count;i++)
			{
				string strState = ListViewUtil.GetItemText(this.Items[i], COLUMN_STATE);
				string strID = ListViewUtil.GetItemText(this.Items[i], COLUMN_ID);
				string strResPath = strXmlRecPath + "/object/" + ListViewUtil.GetItemText(this.Items[i], COLUMN_ID);
				string strLocalFileName = ListViewUtil.GetItemText(this.Items[i], COLUMN_LOCALPATH);
				string strMime =  ListViewUtil.GetItemText(this.Items[i], COLUMN_MIME);
				string strTimeStamp =  ListViewUtil.GetItemText(this.Items[i], COLUMN_TIMESTAMP);

				if (IsNewFileState(strState) == false)
					continue;

				// ����ļ��ߴ�
				FileInfo fi = new FileInfo(strLocalFileName);


				if (fi.Exists == false) 
				{
					strError = "�ļ� '" + strLocalFileName + "' ������...";
					return -1;
				}

				string[] ranges = null;

				if (fi.Length == 0)	
				{ // ���ļ�
					ranges = new string[1];
					ranges[0] = "";
				}
				else 
				{
					string strRange = "";
					strRange = "0-" + Convert.ToString(fi.Length-1);

					// ����100K��Ϊһ��chunk
					ranges = RangeList.ChunkRange(strRange,
						100*1024);
				}

				byte [] timestamp = ByteArray.GetTimeStampByteArray(strTimeStamp);
				byte [] output_timestamp = null;

				nUploadCount ++;

			REDOWHOLESAVE:
				string strWarning = "";


				for(int j=0;j<ranges.Length;j++) 
				{
				REDOSINGLESAVE:

					Application.DoEvents();	// ���ý������Ȩ

					if (stop.State != 0)
					{
						strError = "�û��ж�";
						goto ERROR1;
					}

					string strWaiting = "";
					if (j == ranges.Length - 1)
						strWaiting = " �����ĵȴ�...";

					string strPercent = "";
					RangeList rl = new RangeList(ranges[j]);
					if (rl.Count >= 1) 
					{
						double ratio = (double)((RangeItem)rl[0]).lStart / (double)fi.Length;
						strPercent = String.Format("{0,3:N}",ratio * (double)100) + "%";
					}

					if (stop != null)
						stop.SetMessage("�������� " + ranges[j] + "/"
							+ Convert.ToString(fi.Length)
							+ " " + strPercent + " " + strLocalFileName + strWarning + strWaiting);

					/*
					if (stop != null)
						stop.SetMessage("�������� " + ranges[j] + "/" + Convert.ToString(fi.Length) + " " + strLocalFileName);
					*/

					long lRet = channel.DoSaveResObject(strResPath,
						strLocalFileName,
						strLocalFileName,
						strMime,
                        strLastModifyTime,
						ranges[j],
						j == ranges.Length - 1 ? true : false,	// ��βһ�β��������ѵײ�ע�����������WebService API��ʱʱ��
						timestamp,
						out output_timestamp,
						out strError);
					timestamp = output_timestamp;
					ListViewUtil.ChangeItemText(this.Items[i], COLUMN_TIMESTAMP, ByteArray.GetHexTimeStampString(timestamp));

					strWarning = "";

					if (lRet == -1) 
					{
						if (channel.ErrorCode == ChannelErrorCode.TimestampMismatch)
						{

							if (this.bNotAskTimestampMismatchWhenOverwrite == true) 
							{
								timestamp = new byte[output_timestamp.Length];
								Array.Copy(output_timestamp, 0, timestamp, 0, output_timestamp.Length);
								strWarning = " (ʱ�����ƥ��, �Զ�����)";
								if (ranges.Length == 1 || j==0) 
									goto REDOSINGLESAVE;
								goto REDOWHOLESAVE;
							}


							DialogResult result = MessageDlg.Show(this, 
								"���� '" + strLocalFileName + "' (Ƭ��:" + ranges[j] + "/�ܳߴ�:"+Convert.ToString(fi.Length)
								+") ʱ����ʱ�����ƥ�䡣��ϸ������£�\r\n---\r\n"
								+ strError + "\r\n---\r\n\r\n�Ƿ�����ʱ���ǿ������?\r\nע��(��)ǿ������ (��)���Ե�ǰ��¼����Դ���أ�����������Ĵ��� (ȡ��)�ж�����������",
								"dp2batch",
								MessageBoxButtons.YesNoCancel,
								MessageBoxDefaultButton.Button1,
								ref this.bNotAskTimestampMismatchWhenOverwrite);
							if (result == DialogResult.Yes) 
							{
								timestamp = new byte[output_timestamp.Length];
								Array.Copy(output_timestamp, 0, timestamp, 0, output_timestamp.Length);
								strWarning = " (ʱ�����ƥ��, Ӧ�û�Ҫ������)";
								if (ranges.Length == 1 || j==0) 
									goto REDOSINGLESAVE;
								goto REDOWHOLESAVE;
							}

							if (result == DialogResult.No) 
							{
								goto END1;	// �������������Դ
							}

							if (result == DialogResult.Cancel) 
							{
								strError = "�û��ж�";
								goto ERROR1;	// �ж���������
							}
						}

						goto ERROR1;
					}
					//timestamp = output_timestamp;

				}


				DigitalPlatform.Xml.ElementItem item = GetFileItem(strID);

				if (item != null) 
				{
					item.SetAttrValue("__state", this.ServerFileState);
				}
				else 
				{
					Debug.Assert(false, "xmleditor�о�Ȼ������idΪ[" + strID + "]��<dprms:file>Ԫ��");
				}


			}

			END1:
			if (stop != null)
				stop.SetMessage("������Դȫ�����");

			return nUploadCount;
			ERROR1:
				return -1;
		}

		// ��õ�ǰ��ѡ��Ŀ����������ص�ȫ��id
		public string[] GetSelectedDownloadIds()
		{
			ArrayList aText = new ArrayList();

			int i=0;
			for(i=0;i<this.SelectedItems.Count;i++) 
			{
				if (IsNewFileState(ListViewUtil.GetItemText(this.SelectedItems[i], COLUMN_STATE)) == false) 
				{
					aText.Add(ListViewUtil.GetItemText(this.SelectedItems[i], COLUMN_ID));
				}
			}

			string[] result = new string[aText.Count];
			for(i=0;i<aText.Count;i++)
			{
				result[i] = (string)aText[i];
			}

			return result;
		}

		// ��XML�������Ƴ������õ���ʱ����
        // parameters:
        //      bHasUploadedFile    �Ƿ�����������Դ��<file>Ԫ��?
		public static int RemoveWorkingAttrs(string strXml,
			out string strResultXml,
            out bool bHasUploadedFile,
			out string strError)
		{
            bHasUploadedFile = false;
			strResultXml = strXml;
			strError = "";
			XmlDocument dom = new XmlDocument();

			try 
			{
				dom.LoadXml(strXml);
			}
			catch (Exception ex)
			{
				strError = ex.Message;
				return -1;
			}

			XmlNamespaceManager mngr = new XmlNamespaceManager(dom.NameTable);
			mngr.AddNamespace("dprms", DpNs.dprms);
			
			XmlNodeList nodes = dom.DocumentElement.SelectNodes("//dprms:file", mngr);

			for(int i=0; i<nodes.Count; i++)
			{
				XmlNode node = nodes[i];

                string strState = DomUtil.GetAttr(node, "__state");
                if (IsNewFileState(strState) == false)
                    bHasUploadedFile = true;


				DomUtil.SetAttr(node, "__mime", null);
				DomUtil.SetAttr(node, "__localpath", null);
				DomUtil.SetAttr(node, "__state", null);
				DomUtil.SetAttr(node, "__size", null);

				DomUtil.SetAttr(node, "__timestamp", null);

			}

			strResultXml = dom.OuterXml;	// ??

			return 0;
		}


#if NO
		// �õ�Xml��¼������<file>Ԫ�ص�id����ֵ
		public static int GetFileIds(string strXml,
			out string[] ids,
			out string strError)
		{
			ids = null;
			strError = "";
			XmlDocument dom = new XmlDocument();

			try 
			{
				dom.LoadXml(strXml);
			}
			catch (Exception ex)
			{
				strError = "װ�� XML ���� DOM ʱ����: " + ex.Message;
				return -1;
			}

			XmlNamespaceManager mngr = new XmlNamespaceManager(dom.NameTable);
			mngr.AddNamespace("dprms", DpNs.dprms);
			
			XmlNodeList nodes = dom.DocumentElement.SelectNodes("//dprms:file", mngr);

			ids = new string[nodes.Count];
			for(int i=0; i<nodes.Count; i++)
			{
				XmlNode node = nodes[i];

				ids[i] = DomUtil.GetAttr(node, "id");
			}
			return 0;
		}
#endif

		static bool IsNewFileState(string strState)
		{
			if (String.IsNullOrEmpty(strState) == true)
				return false;
			if (strState == "������")
				return false;
			if (strState == "��δ����")
				return true;

			// Debug.Assert(false, "δ�����״̬");
			return false;
		}

		private void ResFileList_DoubleClick(object sender, System.EventArgs e)
		{
			if (this.SelectedItems.Count == 0)
				return;

			button_modifyFile_Click(null, null);
		}

		string ServerFileState
		{
			get 
			{
				return "������";
			}
		}
		string NewFileState
		{
			get 
			{
				return "��δ����";
			}
		}

	}

	public delegate void Delegate_DownloadFiles(); 

	// strID:	��Դid��
	public delegate int Delegate_DownloadOneMetaData(string strID,
	out string strResultXml,
	out byte [] timestamp,
	out string strError);

}

