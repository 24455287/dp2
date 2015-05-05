using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

using DigitalPlatform.Xml;

namespace DigitalPlatform.CommonControl
{
    /// <summary>
    /// �༭����<from>Ԫ�صĿؼ�
    /// ��:           
    /*      <from style="title">
                <caption lang="zh-CN">����</caption>
                <caption lang="en">Title</caption>
            </from>
            <from style="author">
                <caption lang="zh-CN">����</caption>
                <caption lang="en">Author</caption>
            </from>
     * */
    /// </summary>
    public partial class FromEditControl : UserControl
    {
        [Category("New Event")]
        public event EventHandler SelectedIndexChanged = null;

        public FromElement LastClickElement = null;   // ���һ��clickѡ�����Element����

        int m_nInSuspend = 0;

        public List<FromElement> Elements = new List<FromElement>();

        bool m_bChanged = false;

        public FromEditControl()
        {
            InitializeComponent();
        }

        // captions�����Ƿ񵥶��б�����
        internal bool m_bHasCaptionsTitleLine = true;

        [Category("Appearance")]
        [DescriptionAttribute("HasCaptionsTitleLine")]
        [DefaultValue(typeof(bool), "true")]
        public bool HasCaptionsTitleLine
        {
            get
            {
                return this.m_bHasCaptionsTitleLine;
            }
            set
            {
                this.m_bHasCaptionsTitleLine = value;

                foreach (FromElement line in this.Elements)
                {
                    line.captions.HasTitleLine = this.m_bHasCaptionsTitleLine;
                }
            }
        }

        /*
        BorderStyle borderStyle = BorderStyle.Fixed3D;

        [Category("Appearance")]
        [DescriptionAttribute("Border style of the control")]
        [DefaultValue(typeof(System.Windows.Forms.BorderStyle), "None")]
        public BorderStyle BorderStyle
        {
            get
            {
                return borderStyle;
            }
            set
            {
                borderStyle = value;

                // Get Styles using Win32 calls
                int style = API.GetWindowLong(Handle, API.GWL_STYLE);
                int exStyle = API.GetWindowLong(Handle, API.GWL_EXSTYLE);

                // Modify Styles to match the selected border style
                BorderStyleToWindowStyle(ref style, ref exStyle);

                // Set Styles using Win32 calls
                API.SetWindowLong(Handle, API.GWL_STYLE, style);
                API.SetWindowLong(Handle, API.GWL_EXSTYLE, exStyle);

                // Tell Windows that the frame changed
                API.SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, 0, 0,
                    API.SWP_NOACTIVATE | API.SWP_NOMOVE | API.SWP_NOSIZE |
                    API.SWP_NOZORDER | API.SWP_NOOWNERZORDER |
                    API.SWP_FRAMECHANGED);
            }
        }
        */

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                return this.m_bChanged;
            }
            set
            {

                if (this.m_bChanged != value)
                {
                    this.m_bChanged = value;

                    if (value == false)
                        ResetLineColor();
                }
            }
        }

        public TableLayoutPanelCellBorderStyle CellBorderStyle
        {
            get
            {
                return this.tableLayoutPanel_main.CellBorderStyle;
            }
            set
            {
                this.tableLayoutPanel_main.CellBorderStyle = value;
            }
        }

        /*
        // ����������б�
        public void FillLanguageList(ComboBox list,
            string[] languages)
        {
            list.Items.Clear();
            if (languages == null
                || languages.Length == 0)
                return;

            foreach (string value in languages)
            {
                list.Items.Add(value);
            }
        }*/

        void SetElementsHeight()
        {
            this.DisableUpdate();
            try
            {
                foreach (FromElement line in this.Elements)
                {
                    line.SetCaptionsHeight(true);
                }

            }
            finally
            {
                this.EnableUpdate();
            }
        }

        void ResetLineColor()
        {
            for (int i = 0; i < this.Elements.Count; i++)
            {
                FromElement element = this.Elements[i];
                element.State = ElementState.Normal;
            }
        }

        private void DcEditor_SizeChanged(object sender, EventArgs e)
        {
            this.DisableUpdate();
            try
            {
                tableLayoutPanel_main.Size = this.Size;
                // ���µ���textbox�߶�
                SetElementsHeight();
            }
            finally
            {
                this.EnableUpdate();
            }
        }

        [Category("Data")]
        [DescriptionAttribute("Xml")]
        [DefaultValue("")]
        public string Xml
        {
            get
            {
                string strXml = "";
                string strError = "";
                int nRet = GetXml(
                    this.Elements,
                    out strXml,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);
                return strXml;
            }
            set
            {
                string strError = "";
                int nRet = this.SetXml(value,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);
            }
        }

        public string SelectedXml
        {
            get
            {
                if (this.SelectedElements.Count == 0)
                    return "";

                string strXml = "";
                string strError = "";
                int nRet = GetXml(
                    this.SelectedElements,
                    out strXml,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);
                return strXml;
            }

        }


        // ��鵱ǰ������ʽ���Ƿ�Ϸ�
        // return:
        //      -1  �����̱������
        //      0   ��ʽ�д���
        //      1   ��ʽû�д���
        public int Verify(out string strError)
        {
            strError = "";

            if (this.Elements.Count == 0)
            {
                strError = "��һ�����ݶ�û��";
                return 0;
            }

            List<string> styles = new List<string>();

            for (int i = 0; i < this.Elements.Count; i++)
            {
                FromElement element = this.Elements[i];

                string strStyle = element.Style;
                string strCaptionsXml = element.CaptionsXml;

                if (String.IsNullOrEmpty(strStyle) == true
                    && String.IsNullOrEmpty(strCaptionsXml) == true)
                {
                    strError = "�� " + (i + 1).ToString() + " ��Ϊ���У�������ã�Ӧ����ɾ��";
                    return 0;
                }

                if (String.IsNullOrEmpty(strStyle) == true)
                {
                    strError = "�� " + (i + 1).ToString() + " �е� ��� ��δָ��";
                    return 0;
                }

                if (String.IsNullOrEmpty(strCaptionsXml) == true)
                {
                    strError = "�� " + (i + 1).ToString() + " �� ��ʾ���� ��δָ��";
                    return -1;
                }

                int nRet = element.captions.Verify(out strError);
                if (nRet <= 0)
                {
                    strError = "�� " + (i + 1).ToString() + " �� ��ʾ���� ��ʽ������: " + strError;
                    return nRet;
                }

                int index = styles.IndexOf(strStyle);
                if (index != -1)
                {
                    strError = "�� " + (i + 1).ToString() + " �е� ���ֵ '" + strStyle + "' �� �� " + (index + 1).ToString() + " �е��ظ���";
                    return 0;
                }

                styles.Add(strStyle);
            }

            return 1;
        }

        // parameters:
        static int GetXml(
            List<FromElement> elements,
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            for (int i = 0; i < elements.Count; i++)
            {
                FromElement element = elements[i];

                string strStyle = element.Style;
                string strCaptionsXml = element.CaptionsXml;

                if (String.IsNullOrEmpty(strStyle) == true
                    && String.IsNullOrEmpty(strCaptionsXml) == true)
                    continue;

                if (String.IsNullOrEmpty(strStyle) == true)
                {
                    strError = "�� " + (i+1).ToString() + " �е�style��δָ��";
                    return -1;
                }

                if (String.IsNullOrEmpty(strCaptionsXml) == true)
                {
                    strError = "�� " + (i + 1).ToString() + " ����δ����captions";
                    return -1;
                }

                XmlNode node = dom.CreateElement("from");
                dom.DocumentElement.AppendChild(node);

                DomUtil.SetAttr(node, "style", strStyle);
                try
                {
                    node.InnerXml = strCaptionsXml;
                }
                catch (Exception ex)
                {
                    strError = "set innerxml error: " + ex.Message;
                    return -1;
                }
            }

            strXml = dom.DocumentElement.InnerXml;

            return 0;
        }

        int SetXml(string strXml,
            out string strError)
        {
            strError = "";

            // clear linesԭ������
            this.Clear();
            this.LastClickElement = null;

            if (String.IsNullOrEmpty(strXml) == true)
                return 0;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            XmlDocumentFragment fragment = dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = strXml;
            }
            catch (Exception ex)
            {
                strError = "fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                return -1;
            }

            dom.DocumentElement.AppendChild(fragment);

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("from");

            this.DisableUpdate();

            try
            {

                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    FromElement element = this.AppendNewElement();

                    element.Style = DomUtil.GetAttr(node, "style");
                    try
                    {
                        element.CaptionsXml = node.InnerXml;
                    }
                    catch (Exception ex)
                    {
                        strError = "set element CaptionXml error: " + ex.Message;
                        return -1;
                    }
                }

                this.SetElementsHeight();

            }
            finally
            {
                this.EnableUpdate();
            }

            return 0;
        }

        public FromElement AppendNewElement()
        {
            this.tableLayoutPanel_main.RowCount += 1;
            this.tableLayoutPanel_main.RowStyles.Add(new System.Windows.Forms.RowStyle());

            FromElement line = new FromElement(this);

            line.AddToTable(this.tableLayoutPanel_main, this.Elements.Count + 1);

            this.Elements.Add(line);

            return line;
        }

        public FromElement InsertNewElement(int index)
        {
            this.tableLayoutPanel_main.RowCount += 1;
            this.tableLayoutPanel_main.RowStyles.Insert(index + 1, new System.Windows.Forms.RowStyle());

            FromElement line = new FromElement(this);

            line.InsertToTable(this.tableLayoutPanel_main, index);

            this.Elements.Insert(index, line);

            line.State = ElementState.New;

            SelectElement(line, true);  // 2008/6/10

            return line;
        }

        public void RemoveElement(int index)
        {
            FromElement line = this.Elements[index];

            line.RemoveFromTable(this.tableLayoutPanel_main, index);

            this.Elements.Remove(line);

            if (this.LastClickElement == line)
                this.LastClickElement = null;

            this.Changed = true;
        }

        public void RemoveElement(FromElement line)
        {
            int index = this.Elements.IndexOf(line);

            if (index == -1)
                return;

            line.RemoveFromTable(this.tableLayoutPanel_main, index);

            this.Elements.Remove(line);

            if (this.LastClickElement == line)
                this.LastClickElement = null;

            this.Changed = true;
        }

        public void DisableUpdate()
        {
            if (this.m_nInSuspend == 0)
            {
                this.tableLayoutPanel_main.SuspendLayout();
            }

            this.m_nInSuspend++;
        }

        // parameters:
        //      bOldVisible ���Ϊtrue, ��ʾ���Ҫ����
        public void EnableUpdate()
        {
            this.m_nInSuspend--;


            if (this.m_nInSuspend == 0)
            {
                this.tableLayoutPanel_main.ResumeLayout(false);
                this.tableLayoutPanel_main.PerformLayout();
            }
        }

        public void Clear()
        {
            this.DisableUpdate();

            try
            {

                for (int i = 0; i < this.Elements.Count; i++)
                {
                    FromElement element = this.Elements[i];
                    ClearOneElementControls(this.tableLayoutPanel_main,
                        element);
                }

                this.Elements.Clear();
                this.tableLayoutPanel_main.RowCount = 2;    // Ϊʲô��2��
                for (; ; )
                {
                    if (this.tableLayoutPanel_main.RowStyles.Count <= 2)
                        break;
                    this.tableLayoutPanel_main.RowStyles.RemoveAt(2);
                }

            }
            finally
            {
                this.EnableUpdate();
            }

        }

        // ���һ��FromElement�����Ӧ��Control
        public void ClearOneElementControls(
            TableLayoutPanel table,
            FromElement line)
        {
            // color
            Label label = line.label_color;
            table.Controls.Remove(label);

            // style
            TextBox style = line.textBox_style;
            table.Controls.Remove(style);

            // captions
            CaptionEditControl captions = line.captions;
            table.Controls.Remove(captions);

        }

        public void SelectAll()
        {
            bool bSelectedChanged = false;

            for (int i = 0; i < this.Elements.Count; i++)
            {
                FromElement cur_element = this.Elements[i];
                if ((cur_element.State & ElementState.Selected) == 0)
                {
                    cur_element.State |= ElementState.Selected;
                    bSelectedChanged = true;
                }
            }

            if (bSelectedChanged == true)
                this.OnSelectedIndexChanged();
        }

        public void ClearAllSelect()
        {
            bool bSelectedChanged = false;

            for (int i = 0; i < this.Elements.Count; i++)
            {
                FromElement cur_element = this.Elements[i];
                if ((cur_element.State & ElementState.Selected) != 0)
                {
                    cur_element.State -= ElementState.Selected;
                    bSelectedChanged = true;
                }
            }

            this.LastClickElement = null;

            if (bSelectedChanged == true)
                this.OnSelectedIndexChanged();
        }

        public List<FromElement> SelectedElements
        {
            get
            {
                List<FromElement> results = new List<FromElement>();

                for (int i = 0; i < this.Elements.Count; i++)
                {
                    FromElement cur_element = this.Elements[i];
                    if ((cur_element.State & ElementState.Selected) != 0)
                        results.Add(cur_element);
                }

                return results;
            }
        }

        public List<int> SelectedIndices
        {
            get
            {
                List<int> results = new List<int>();

                for (int i = 0; i < this.Elements.Count; i++)
                {
                    FromElement cur_element = this.Elements[i];
                    if ((cur_element.State & ElementState.Selected) != 0)
                        results.Add(i);
                }

                return results;
            }
        }

        public void SelectElement(FromElement element,
            bool bClearOld)
        {
            bool bSelectedChanged = false;

            if (bClearOld == true)
            {
                for (int i = 0; i < this.Elements.Count; i++)
                {
                    FromElement cur_element = this.Elements[i];

                    if (cur_element == element)
                        continue;   // ��ʱ������ǰ��

                    if ((cur_element.State & ElementState.Selected) != 0)
                    {
                        cur_element.State -= ElementState.Selected;
                        bSelectedChanged = true;
                    }
                }
            }

            // ѡ�е�ǰ��
            if ((element.State & ElementState.Selected) == 0)
            {
                element.State |= ElementState.Selected;
                bSelectedChanged = true;
            }

            this.LastClickElement = element;

            if (bClearOld == true)
            {
                // ����focus�ǲ����Ѿ�����һ���ϣ�
                // ������ڣ���Ҫ�л�����
                if (element.IsSubControlFocused() == false)
                    element.textBox_style.Focus();
            }

            if (bSelectedChanged == true)
                OnSelectedIndexChanged();
        }

        public void ToggleSelectElement(FromElement element)
        {
            // ѡ�е�ǰ��
            if ((element.State & ElementState.Selected) == 0)
                element.State |= ElementState.Selected;
            else
                element.State -= ElementState.Selected;

            this.LastClickElement = element;

            this.OnSelectedIndexChanged();
        }

        void OnSelectedIndexChanged()
        {
            if (this.SelectedIndexChanged != null)
            {
                this.SelectedIndexChanged(this, new EventArgs());
            }
        }

        public void RangeSelectElement(FromElement element)
        {
            bool bSelectedChanged = false;

            FromElement start = this.LastClickElement;

            int nStart = this.Elements.IndexOf(start);
            if (nStart == -1)
                return;

            int nEnd = this.Elements.IndexOf(element);

            if (nStart > nEnd)
            {
                // ����
                int nTemp = nStart;
                nStart = nEnd;
                nEnd = nTemp;
            }

            for (int i = nStart; i <= nEnd; i++)
            {
                FromElement cur_element = this.Elements[i];

                if ((cur_element.State & ElementState.Selected) == 0)
                {
                    cur_element.State |= ElementState.Selected;
                    bSelectedChanged = true;
                }
            }

            // �������λ��
            for (int i = 0; i < nStart; i++)
            {
                FromElement cur_element = this.Elements[i];

                if ((cur_element.State & ElementState.Selected) != 0)
                {
                    cur_element.State -= ElementState.Selected;
                    bSelectedChanged = true;
                }
            }

            for (int i = nEnd + 1; i < this.Elements.Count; i++)
            {
                FromElement cur_element = this.Elements[i];

                if ((cur_element.State & ElementState.Selected) != 0)
                {
                    cur_element.State -= ElementState.Selected;
                    bSelectedChanged = true;
                }
            }

            if (bSelectedChanged == true)
                this.OnSelectedIndexChanged();
        }

        public void DeleteSelectedElements()
        {
            bool bSelectedChanged = false;

            List<FromElement> selected_lines = this.SelectedElements;

            if (selected_lines.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫɾ������");
                return;
            }
            string strText = "";

            if (selected_lines.Count == 1)
                strText = "ȷʵҪɾ���� '" + selected_lines[0].Style + "'? ";
            else
                strText = "ȷʵҪɾ����ѡ���� " + selected_lines.Count.ToString() + " ����?";

            DialogResult result = MessageBox.Show(this,
                strText,
                "CaptionEditControl",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                return;
            }


            this.DisableUpdate();
            try
            {
                for (int i = 0; i < selected_lines.Count; i++)
                {
                    this.RemoveElement(selected_lines[i]);
                    bSelectedChanged = true;
                }
            }
            finally
            {
                this.EnableUpdate();
            }

            if (bSelectedChanged == true)
                this.OnSelectedIndexChanged();
        }

        // �����ѡ��Ĳ���Ԫ�ص�XML
        public int GetFragmentXml(
            List<FromElement> selected_lines,
            out string strXml,
            out string strError)
        {
            strXml = "";
            strError = "";

            if (selected_lines.Count == 0)
                return 0;

            XmlDocument dom = new XmlDocument();

            dom.LoadXml("<root />");

            for (int i = 0; i < selected_lines.Count; i++)
            {
                FromElement line = selected_lines[i];

                string strStyle = line.Style;

                if (String.IsNullOrEmpty(strStyle) == true)
                {
                    if (String.IsNullOrEmpty(line.CaptionsXml) == true)
                        continue;
                    else
                    {
                        strError = "��ʽ����captions����Ϊ '" + line.CaptionsXml + "' ����û��ָ��style����";
                        return -1;
                    }
                }

                XmlNode element = dom.CreateElement("from");
                dom.DocumentElement.AppendChild(element);

                DomUtil.SetAttr(element, "style", strStyle);
                element.InnerXml = line.CaptionsXml;

            }

            strXml = dom.DocumentElement.InnerXml;
            return 0;
        }

        // ��Ƭ��XML�а�����Ԫ�أ��滻ָ����������
        // ���selected_lines.Count == 0�����ʾ��nInsertPos��ʼ����
        public int ReplaceElements(
            int nInsertPos,
            List<FromElement> selected_lines,
            string strFragmentXml,
            out string strError)
        {
            strError = "";
            bool bSelectedChanged = false;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            XmlDocumentFragment fragment = dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = strFragmentXml;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            dom.DocumentElement.AppendChild(fragment);


            XmlNode root = dom.DocumentElement;

            int index = 0;  // selected_lines�±� ѡ��lines�����еĵڼ���

            int nTailPos = nInsertPos;   // ����������һ��line�������������е�λ��

            this.DisableUpdate();

            try
            {

                // ���������¼�Ԫ��
                for (int i = 0; i < root.ChildNodes.Count; i++)
                {
                    XmlNode node = root.ChildNodes[i];
                    if (node.NodeType != XmlNodeType.Element)
                        continue;

                    // ���Բ��Ƕ������ֿռ��Ԫ��
                    if (node.Name != "from")
                        continue;

                    FromElement line = null;
                    if (selected_lines != null && index < selected_lines.Count)
                    {
                        line = selected_lines[index];
                        index++;
                    }
                    else
                    {
                        // �����λ�ú������
                        line = this.InsertNewElement(nTailPos);
                    }

                    // ѡ���޸Ĺ���line
                    line.State |= ElementState.Selected;
                    bSelectedChanged = true;

                    nTailPos = this.Elements.IndexOf(line) + 1;

                    line.Style = DomUtil.GetAttr(node, "style");
                    line.CaptionsXml = node.InnerXml;

                    line.SetCaptionsHeight(true);

                }

                // Ȼ���selected_lines�ж����lineɾ��
                if (selected_lines != null)
                {
                    for (int i = index; i < selected_lines.Count; i++)
                    {
                        this.RemoveElement(selected_lines[i]);
                    }
                }
            }
            finally
            {
                this.EnableUpdate();
            }

            if (bSelectedChanged == true)
                this.OnSelectedIndexChanged();

            return 0;
        }

        // ���Ͻǵ�һ��label��������popupmenu
        private void label_topleft_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            bool bHasClipboardObject = false;
            IDataObject ido = Clipboard.GetDataObject();
            if (ido.GetDataPresent(DataFormats.Text) == true)
                bHasClipboardObject = true;

            //
            menuItem = new MenuItem("�������(&A)");
            menuItem.Click += new System.EventHandler(this.menu_appendElement_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("����������(&C)");
            menuItem.Click += new System.EventHandler(this.menu_copyRecord_Click);
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ճ���滻������(&P)");
            menuItem.Click += new System.EventHandler(this.menu_pasteRecord_Click);
            if (bHasClipboardObject == true)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.label_topleft, new Point(e.X, e.Y));

        }

        // �����׷��һ������
        void menu_appendElement_Click(object sender, EventArgs e)
        {
            NewElement(true);
        }

        // �����׷��һ������
        // parameters:
        //      bSetFirstBlankCaption   �Ƿ����õ�һ���յ�caption��?
        public void NewElement(bool bSetFirstBlankCaption)
        {
            FromElement element = this.InsertNewElement(this.Elements.Count);

            if (bSetFirstBlankCaption == true)
                element.CaptionsXml = "<caption lang='zh'></caption><caption lang='en'></caption>";

            // ����ɼ���Χ��
            element.ScrollIntoView();

            // ѡ����
            element.Select(1);
        }

        // ����������¼
        void menu_copyRecord_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strXml = "";
            int nRet = GetXml(
                this.Elements,
                out strXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }

            /*
            string strOutXml = "";
            nRet = DomUtil.GetIndentXml(strXml,
                out strOutXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }*/

            Clipboard.SetDataObject(strXml);
        }

        // ճ���滻������¼
        void menu_pasteRecord_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strXml = ClipboardUtil.GetClipboardText();
            int nRet = this.SetXml(strXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }
        }

    }


    public class FromElement
    {
        public FromEditControl Container = null;

        // ��ɫ��popupmenu
        public Label label_color = null;

        // style
        public TextBox textBox_style = null;

        // Captions
        public CaptionEditControl captions = null;

        ElementState m_state = ElementState.Normal;

        public ElementState State
        {
            get
            {
                return this.m_state;
            }
            set
            {
                if (this.m_state != value)
                {
                    this.m_state = value;
                    SetLineColor();
                }
            }
        }

        void SetLineColor()
        {
            if ((this.m_state & ElementState.Selected) != 0)
            {
                this.label_color.BackColor = SystemColors.Highlight;
                return;
            }
            if ((this.m_state & ElementState.New) != 0)
            {
                this.label_color.BackColor = Color.Yellow;
                return;
            }
            if ((this.m_state & ElementState.Changed) != 0)
            {
                this.label_color.BackColor = Color.LightGreen;
                return;
            }

            this.label_color.BackColor = SystemColors.Window;
        }

        public FromElement(FromEditControl container)
        {
            this.Container = container;

            label_color = new Label();
            label_color.Dock = DockStyle.Fill;
            label_color.Size = new Size(6, 28);

            // style
            textBox_style = new TextBox();
            textBox_style.BorderStyle = BorderStyle.None;
            textBox_style.Dock = DockStyle.Fill;
            textBox_style.MinimumSize = new Size(100, 28);
            textBox_style.Margin = new Padding(6, 3, 6, 0);

            textBox_style.ForeColor = this.Container.tableLayoutPanel_main.ForeColor;


            // captions
            captions = new CaptionEditControl();
            // captions.MaximumSize = new Size(200, 28);
            captions.Size = new Size(150, 3 * 28);
            // captions.MinimumSize = new Size(50, 28);
            captions.Dock = DockStyle.Fill;

            /*
            captions.AutoSize = true;
            captions.AutoSizeMode = AutoSizeMode.GrowOnly;
            captions.AutoScroll = false;
             * */


            // �Ƿ��е����ı�����?
            captions.HasTitleLine = this.Container.m_bHasCaptionsTitleLine;

            captions.ForeColor = this.Container.tableLayoutPanel_main.ForeColor;
        }

        public void SetCaptionsHeight(bool bResetHeightFirst)
        {
            CaptionEditControl captions = this.captions;

            captions.Size = new Size(captions.Size.Width, (28 + 2) * (captions.Elements.Count + (captions.HasTitleLine == true ? 1 : 0)));

            // captions.Size = new Size(captions.Size.Width, this.Container.tableLayoutPanel_main.DisplayRectangle.Height);
        }

        public string CaptionsXml
        {
            get
            {
                return this.captions.Xml;
            }
            set
            {
                this.captions.Xml = value;
            }
        }

        public string Style
        {
            get
            {
                return this.textBox_style.Text;
            }
            set
            {
                this.textBox_style.Text = value;
            }
        }

        public void AddToTable(TableLayoutPanel table,
            int nRow)
        {
            table.Controls.Add(this.label_color, 0, nRow);
            table.Controls.Add(this.textBox_style, 1, nRow);
            table.Controls.Add(this.captions, 2, nRow);

            AddEvents();
        }

        void AddEvents()
        {
            // events

            // label_color
            this.label_color.MouseUp -= new MouseEventHandler(label_color_MouseUp);
            this.label_color.MouseUp += new MouseEventHandler(label_color_MouseUp);

            this.label_color.MouseClick -= new MouseEventHandler(label_color_MouseClick);
            this.label_color.MouseClick += new MouseEventHandler(label_color_MouseClick);

            // style
            this.textBox_style.KeyUp -= new KeyEventHandler(textBox_value_KeyUp);
            this.textBox_style.KeyUp += new KeyEventHandler(textBox_value_KeyUp);

            this.textBox_style.TextChanged -= new EventHandler(textBox_value_TextChanged);
            this.textBox_style.TextChanged += new EventHandler(textBox_value_TextChanged);

            this.textBox_style.Enter -= new EventHandler(textBox_value_Enter);
            this.textBox_style.Enter += new EventHandler(textBox_value_Enter);


            // captions
            this.captions.ElementCountChanged -= new EventHandler(captions_ElementCountChanged);
            this.captions.ElementCountChanged += new EventHandler(captions_ElementCountChanged);
            /*
            this.comboBox_language.DropDown -= new EventHandler(comboBox_language_DropDown);
            this.comboBox_language.DropDown += new EventHandler(comboBox_language_DropDown);

            this.comboBox_language.TextChanged -= new EventHandler(comboBox_language_TextChanged);
            this.comboBox_language.TextChanged += new EventHandler(comboBox_language_TextChanged);

            this.comboBox_language.Enter -= new EventHandler(comboBox_language_Enter);
            this.comboBox_language.Enter += new EventHandler(comboBox_language_Enter);
            */
        }

        void captions_ElementCountChanged(object sender, EventArgs e)
        {
            this.SetCaptionsHeight(false);
        }

        void label_color_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            int nSelectedCount = this.Container.SelectedIndices.Count;
            bool bHasClipboardObject = false;
            IDataObject ido = Clipboard.GetDataObject();
            if (ido.GetDataPresent(DataFormats.Text) == true)
                bHasClipboardObject = true;

            //
            menuItem = new MenuItem("ǰ��(&I)");
            menuItem.Click += new System.EventHandler(this.menu_insertElement_Click);
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("���(&A)");
            menuItem.Click += new System.EventHandler(this.menu_appendElement_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("ɾ��(&D)");
            menuItem.Click += new System.EventHandler(this.menu_deleteElements_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("����(&T)");
            menuItem.Click += new System.EventHandler(this.menu_cut_Click);
            if (nSelectedCount > 0)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("����(&C)");
            menuItem.Click += new System.EventHandler(this.menu_copy_Click);
            if (nSelectedCount > 0)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ճ������[ǰ](&P)");
            menuItem.Click += new System.EventHandler(this.menu_pasteInsert_Click);
            if (bHasClipboardObject == true
                && nSelectedCount > 0)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ճ������[��](&P)");
            menuItem.Click += new System.EventHandler(this.menu_pasteInsertAfter_Click);
            if (bHasClipboardObject == true
                && nSelectedCount > 0)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("ճ���滻(&R)");
            menuItem.Click += new System.EventHandler(this.menu_pasteReplace_Click);
            if (bHasClipboardObject == true
                && nSelectedCount > 0)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);



            menuItem = new MenuItem("ȫѡ(&A)");
            menuItem.Click += new System.EventHandler(this.menu_selectAll_Click);
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.label_color, new Point(e.X, e.Y));
        }

        void menu_insertElement_Click(object sender, EventArgs e)
        {
            int nPos = this.Container.Elements.IndexOf(this);

            if (nPos == -1)
                throw new Exception("not found myself");

            FromElement element = this.Container.InsertNewElement(nPos);
            // ���ÿհ׵�captions��
            element.CaptionsXml = "<caption lang='zh'></caption><caption lang='en'></caption>";
        }

        void menu_appendElement_Click(object sender, EventArgs e)
        {
            int nPos = this.Container.Elements.IndexOf(this);
            if (nPos == -1)
            {
                throw new Exception("not found myself");
            }

            FromElement element = this.Container.InsertNewElement(nPos + 1);
            // ���ÿհ׵�captions��
            element.CaptionsXml = "<caption lang='zh'></caption><caption lang='en'></caption>";
        }

        // ȫѡ
        void menu_selectAll_Click(object sender, EventArgs e)
        {
            this.Container.SelectAll();
        }

        // ɾ����ǰԪ��
        void menu_deleteElements_Click(object sender, EventArgs e)
        {
            this.Container.DeleteSelectedElements();
        }

        // ����
        void menu_cut_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strXml = "";

            List<FromElement> selected_lines = this.Container.SelectedElements;


            // �����ѡ��Ĳ���Ԫ�ص�XML
            int nRet = this.Container.GetFragmentXml(
                selected_lines,
                out strXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this.Container, strError);
                return;
            }

            Clipboard.SetDataObject(strXml);


            this.Container.DisableUpdate();
            try
            {
                for (int i = 0; i < selected_lines.Count; i++)
                {
                    this.Container.RemoveElement(selected_lines[i]);
                }
            }
            finally
            {
                this.Container.EnableUpdate();
            }
        }

        // ����
        void menu_copy_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strXml = "";
            // �����ѡ��Ĳ���Ԫ�ص�XML
            int nRet = this.Container.GetFragmentXml(
                this.Container.SelectedElements,
                out strXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this.Container, strError);
                return;
            }

            Clipboard.SetDataObject(strXml);
        }

        // ճ������[ǰ]
        void menu_pasteInsert_Click(object sender, EventArgs e)
        {
            string strError = "";

            List<FromElement> selected_lines = this.Container.SelectedElements;

            int nInsertPos = 0;

            if (selected_lines.Count == 0)
                nInsertPos = 0;
            else
                nInsertPos = this.Container.SelectedIndices[0];

            string strFragmentXml = ClipboardUtil.GetClipboardText();

            int nRet = this.Container.ReplaceElements(
                nInsertPos,
                null,
                strFragmentXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this.Container, strError);
                return;
            }

            // ��ԭ��ѡ�е�Ԫ�ر�Ϊû��ѡ��״̬
            for (int i = 0; i < selected_lines.Count; i++)
            {
                FromElement line = selected_lines[i];
                if ((line.State & ElementState.Selected) != 0)
                    line.State -= ElementState.Selected;
            }

        }

        // ճ������[��]
        void menu_pasteInsertAfter_Click(object sender, EventArgs e)
        {
            string strError = "";

            List<FromElement> selected_lines = this.Container.SelectedElements;

            int nInsertPos = 0;

            if (selected_lines.Count == 0)
                nInsertPos = this.Container.Elements.Count;
            else
                nInsertPos = this.Container.SelectedIndices[0] + 1;

            string strFragmentXml = ClipboardUtil.GetClipboardText();

            int nRet = this.Container.ReplaceElements(
                nInsertPos,
                null,
                strFragmentXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this.Container, strError);
                return;
            }

            // ��ԭ��ѡ�е�Ԫ�ر�Ϊû��ѡ��״̬
            for (int i = 0; i < selected_lines.Count; i++)
            {
                FromElement line = selected_lines[i];
                if ((line.State & ElementState.Selected) != 0)
                    line.State -= ElementState.Selected;
            }

        }


        // ճ���滻
        void menu_pasteReplace_Click(object sender, EventArgs e)
        {
            string strError = "";

            string strFragmentXml = ClipboardUtil.GetClipboardText();

            int nRet = this.Container.ReplaceElements(
                0,
                this.Container.SelectedElements,
                strFragmentXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this.Container, strError);
                return;
            }
        }



        // ����ɫlabel�ϵ������
        void label_color_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // MessageBox.Show(this.Container, "left click");
                if (Control.ModifierKeys == Keys.Control)
                {
                    this.Container.ToggleSelectElement(this);
                }
                else if (Control.ModifierKeys == Keys.Shift)
                    this.Container.RangeSelectElement(this);
                else
                {
                    this.Container.SelectElement(this, true);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // �����ǰ�ж���ѡ���򲻱���ʲôl
                // �����ǰΪ����һ��ѡ�����0��ѡ����ѡ��ǰ����
                // ��������Ŀ���Ƿ������
                if (this.Container.SelectedIndices.Count < 2)
                {
                    this.Container.SelectElement(this, true);
                }
            }
        }

        void comboBox_language_TextChanged(object sender, EventArgs e)
        {
            if ((this.State & ElementState.New) == 0)
                this.State |= ElementState.Changed;

            this.Container.Changed = true;
        }

        void textBox_value_Enter(object sender, EventArgs e)
        {
            this.Container.SelectElement(this, true);
        }

        void comboBox_language_Enter(object sender, EventArgs e)
        {
            this.Container.SelectElement(this, true);
        }

        void textBox_value_KeyUp(object sender, KeyEventArgs e)
        {
        }

        void textBox_value_TextChanged(object sender, EventArgs e)
        {
            this.Container.Changed = true;

            if ((this.State & ElementState.New) == 0)
                this.State |= ElementState.Changed;
        }


        // ��Ԫ���������Ŀؼ�ӵ���˽�����ô?
        public bool IsSubControlFocused()
        {
            if (this.textBox_style.Focused == true)
                return true;

            if (this.captions.Focused == true)
                return true;

            return false;
        }

        // ���뱾Line��ĳ�С�����ǰ��table.RowCount�Ѿ�����
        // parameters:
        //      nRow    ��0��ʼ����
        public void InsertToTable(TableLayoutPanel table,
            int nRow)
        {
            this.Container.DisableUpdate();

            try
            {

                Debug.Assert(table.RowCount ==
                    this.Container.Elements.Count + 3, "");

                // ���ƶ��󷽵�
                for (int i = (table.RowCount - 1) - 3; i >= nRow; i--)
                {
                    FromElement line = this.Container.Elements[i];

                    // color
                    Label label = line.label_color;
                    table.Controls.Remove(label);
                    table.Controls.Add(label, 0, i + 1 + 1);

                    // text
                    TextBox text = line.textBox_style;
                    table.Controls.Remove(text);
                    table.Controls.Add(text, 1, i + 1 + 1);

                    // captions
                    CaptionEditControl captions = line.captions;
                    table.Controls.Remove(captions);
                    table.Controls.Add(captions, 2, i + 1 + 1);


                }

                table.Controls.Add(this.label_color, 0, nRow + 1);
                table.Controls.Add(this.textBox_style, 1, nRow + 1);
                table.Controls.Add(this.captions, 2, nRow + 1);

            }
            finally
            {
                this.Container.EnableUpdate();
            }

            // events
            AddEvents();
        }

        // �Ƴ���Element
        // parameters:
        //      nRow    ��0��ʼ����
        public void RemoveFromTable(TableLayoutPanel table,
            int nRow)
        {
            this.Container.DisableUpdate();

            try
            {

                // �Ƴ�������صĿؼ�
                table.Controls.Remove(this.label_color);
                table.Controls.Remove(this.textBox_style);
                table.Controls.Remove(this.captions);

                Debug.Assert(this.Container.Elements.Count ==
                    table.RowCount - 2, "");

                // Ȼ��ѹ���󷽵�
                for (int i = (table.RowCount - 2) - 1; i >= nRow + 1; i--)
                {
                    FromElement line = this.Container.Elements[i];

                    // color
                    Label label = line.label_color;
                    table.Controls.Remove(label);
                    table.Controls.Add(label, 0, i - 1 + 1);

                    // text
                    TextBox text = line.textBox_style;
                    table.Controls.Remove(text);
                    table.Controls.Add(text, 1, i - 1 + 1);

                    // captions
                    CaptionEditControl captions = line.captions;
                    table.Controls.Remove(captions);
                    table.Controls.Add(captions, 2, i - 1 + 1);


                }

                table.RowCount--;
                table.RowStyles.RemoveAt(nRow);

            }
            finally
            {
                this.Container.EnableUpdate();
            }
        }

        // ����ɼ���Χ
        public void ScrollIntoView()
        {
            this.Container.tableLayoutPanel_main.ScrollControlIntoView(this.textBox_style);
        }

        // ��ѡ��Ԫ��
        // parameters:
        //      nCol    1 style��; 2: captions��
        public void Select(int nCol)
        {

            if (nCol == 1)
            {
                this.textBox_style.SelectAll();
                this.textBox_style.Focus();
                return;
            }

            if (nCol == 2)
            {
                this.captions.SelectAll();
                this.captions.Focus();
                return;
            }

            this.Container.SelectElement(this, true);
        }
    }
}
