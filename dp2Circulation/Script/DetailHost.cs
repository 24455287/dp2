#define SHITOUTANG  // ʯͷ�����෨�����ߺŵ�֧��

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using DigitalPlatform;
using DigitalPlatform.Marc;
using DigitalPlatform.CommonControl;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.GUI;
using DigitalPlatform.Text;
using DigitalPlatform.Script;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;

using DigitalPlatform.GcatClient;

namespace dp2Circulation
{
    /// <summary>
    /// ��ϸ�����ο���������
    /// �������ǰ�� Host ��
    /// </summary>
    public class DetailHost : IDetailHost
    {
        ScriptActionCollection _scriptActions = new ScriptActionCollection();
        IBiblioItemsWindow _detailWindow = null;

        #region IDetailHost �ӿ�Ҫ��

        public Form Form
        {
            get
            {
                return (this.DetailForm as Form);
            }
            set
            {
                this.DetailForm = (value as EntityForm);
            }
        }

        /// <summary>
        /// �ֲᴰ
        /// </summary>
        public IBiblioItemsWindow DetailWindow
        {
            get
            {
                return this._detailWindow;
            }
            set
            {
                this._detailWindow = value;
            }
        }

        /// <summary>
        /// �ű������� Assembly
        /// </summary>
        public Assembly Assembly
        {
            get;
            set;
        }

        /// <summary>
        /// Ctrl+A �������Ƶļ���
        /// </summary>
        public ScriptActionCollection ScriptActions
        {
            get
            {
                return _scriptActions;
            }
            set
            {
                _scriptActions = value;
            }
        }

        /// <summary>
        /// ����һ�� Ctrl+A ����
        /// </summary>
        /// <param name="strFuncName">������</param>
        /// <param name="sender">������</param>
        /// <param name="e">Ctrl+A �¼�����</param>
        public void Invoke(string strFuncName,
            object sender,
            // GenerateDataEventArgs e
            EventArgs e)
        {
            Type classType = this.GetType();

            while (classType != null)
            {
                try
                {
                    // �����������ĳ�Ա����
                    classType.InvokeMember(strFuncName,
                        BindingFlags.DeclaredOnly |
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.InvokeMethod
                        ,
                        null,
                        this,
                        new object[] { sender, e });
                    return;

                }
                catch (System.MissingMethodException/*ex*/)
                {
                    classType = classType.BaseType;
                    if (classType == null)
                        break;
                }
            }

            classType = this.GetType();

            while (classType != null)
            {
                try
                {
                    // ������ǰ����д���� -- û�в���
                    classType.InvokeMember(strFuncName,
                        BindingFlags.DeclaredOnly |
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.InvokeMethod
                        ,
                        null,
                        this,
                        null);
                    return;
                }
                catch (System.MissingMethodException/*ex*/)
                {
                    classType = classType.BaseType;
                }
            }

            throw new Exception("���� void " + strFuncName + "(object sender, GenerateDataEventArgs e) �� void " + strFuncName + "() û���ҵ�");
        }

        public virtual void CreateMenu(object sender, GenerateDataEventArgs e)
        {
            ScriptActionCollection actions = new ScriptActionCollection();

            if (sender is MarcEditor || sender == null)
            {
#if TESTING
            actions.NewItem("������", "������", "Test", false);
#endif

#if NO
                // ����ISBNΪ13
                actions.NewItem("����ΪISBN-13", "��010$a��ISBN���й���", "HyphenISBN_13", false);

                // ����ISBNΪ10
                actions.NewItem("����ΪISBN-10", "��010$a��ISBN���й���", "HyphenISBN_10", false);
#endif
            }

            if (sender is BinaryResControl || sender is MarcEditor)
            {
                // 856�ֶ�
                actions.NewItem("����ά��856�ֶ�", "����ά��856�ֶ�", "Manage856", false);
            }

            if (sender is EntityEditForm || sender is EntityControl || sender is BindingForm)
            {
                // ������ȡ��
                actions.NewItem("������ȡ��", "Ϊ���¼������ȡ��", "CreateCallNumber", false);

                // ������ȡ��
                actions.NewItem("������ȡ��", "Ϊ���¼������ȡ��", "ManageCallNumber", false);
            }

            this.ScriptActions = actions;
        }

        // ���ò˵�����״̬ -- 856�ֶ�
        void Manage856_setMenu(object sender, SetMenuEventArgs e)
        {
            Field curfield = this.DetailForm.MarcEditor.FocusedField;
            if (curfield != null && curfield.Name == "856")
                e.Action.Active = true;
            else
                e.Action.Active = false;
        }

        // ���ò˵�����״̬ -- ������ȡ��
        void CreateCallNumber_setMenu(object sender, SetMenuEventArgs e)
        {
            e.Action.Active = false;
            if (e.sender is EntityEditForm)
                e.Action.Active = true;
        }

        #endregion

        /// <summary>
        /// �ֲᴰ
        /// </summary>
        public EntityForm DetailForm = null;

        /// <summary>
        /// GCAT ͨѶͨ��
        /// </summary>
        DigitalPlatform.GcatClient.Channel GcatChannel = null;

        /// <summary>
        /// ���캯��
        /// </summary>
        public DetailHost()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        /// <summary>
        /// ��ں���
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void Main(object sender, GenerateDataEventArgs e/*HostEventArgs e*/)
        {

        }

        /// <summary>
        /// ����һ�� Ctrl+A ����
        /// </summary>
        /// <param name="strFuncName">������</param>
        public void Invoke(string strFuncName)
        {
            Type classType = this.GetType();

            // ���ó�Ա����
            classType.InvokeMember(strFuncName,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.InvokeMethod
                ,
                null,
                this,
                null);
        }

        // ���ݱ���ǰ�Ĵ�����
        /// <summary>
        /// ���ݱ���ǰ�Ĵ�����
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void BeforeSaveRecord(object sender,
            BeforeSaveRecordEventArgs e)
        {
            if (sender == null)
                return;

            int nRet = 0;
            string strError = "";
            bool bChanged = false;

            try
            {
                // ��MARC��¼���д���
                if (sender is MarcEditor)
                {
                    // ��Ŀ���κ�
                    string strBatchNo = this.GetFirstSubfield("998", "a");
                    if (string.IsNullOrEmpty(strBatchNo) == true)
                    {
                        string strValue = "";
                        // ��鱾�� %catalog_batchno% ���Ƿ����
                        // ��marceditor_macrotable.xml�ļ��н�����
                        // return:
                        //      -1  error
                        //      0   not found
                        //      1   found
                        nRet = MacroUtil.GetFromLocalMacroTable(
                            PathUtil.MergePath(this.DetailForm.MainForm.DataDir, "marceditor_macrotable.xml"),
                "catalog_batchno",
                false,
                out strValue,
                out strError);
                        if (nRet == -1)
                        {
                            e.ErrorInfo = strError;
                            return;
                        }
                        if (nRet == 1 && string.IsNullOrEmpty(strValue) == false)
                        {
                            this.SetFirstSubfield("998", "a", strValue);
                            bChanged = true;
                        }
                    }

                    // ��¼����ʱ��
                    string strCreateTime = this.GetFirstSubfield("998", "u");
                    if (string.IsNullOrEmpty(strCreateTime) == true)
                    {
                        DateTime now = DateTime.Now;
                        strCreateTime = now.ToString("u");
                        this.SetFirstSubfield("998", "u", strCreateTime);
                        bChanged = true;
                    }

                    // ��¼������
                    string strCreator = this.GetFirstSubfield("998", "z");
                    if (string.IsNullOrEmpty(strCreator) == true)
                    {
                        strCreator = this.DetailForm.Channel.UserName;
                        this.SetFirstSubfield("998", "z", strCreator);
                        bChanged = true;
                    }

                    e.Changed = bChanged;
                }
            }
            catch (Exception ex)
            {
                e.ErrorInfo = ex.Message;
            }
        }

        // ���մ������¼��Ĵ�����
        /// <summary>
        /// ���մ������¼��Ĵ�����
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void AfterCreateItems(object sender,
            AfterCreateItemsArgs e)
        {
            if (sender == null)
                return;
#if NO
            string strError = "";
            string strHtml = "";
            // �����Ѿ��Ƽ�������Ŀ����ע��Ϣ��HTML��ʽ
            int nRet = this.DetailForm.CommentControl.GetOrderSuggestionHtml(
                out strHtml,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            if (string.IsNullOrEmpty(strHtml) == true)
                return;

            strHtml = "<html><head>"
                +"<style media='screen' type='text/css'>"
                +"body, input, select { FONT-FAMILY: Microsoft YaHei, Verdana, ����; }"
                +"body { padding: 20px;	background-color: #White; }"
                + "table { width: 100%; font-size: 12pt; border-style: solid; border-width: 1pt; border-color: #000000;	border-collapse:collapse; border-width: 1pt; } "
                + "table td { padding : 8px; border-style: dotted; border-width: 1pt; border-color: #555555; } "
                + "table tr.column td { color: White; background-color: #999999; font-weight: bolder; } "
                + "</style>"
                +"</head>"
                +"<body>" + strHtml + "</body></html>";

            HtmlViewerForm dlg = new HtmlViewerForm();
            dlg.Text = "��������Ϣ";
            dlg.HtmlString = strHtml;
            this.DetailForm.MainForm.AppInfo.LinkFormState(dlg, "AfterCreateItems_dialog_state");
            dlg.ShowDialog(this.DetailForm);
            this.DetailForm.MainForm.AppInfo.UnlinkFormState(dlg);

            return;
        ERROR1:
            MessageBox.Show(this.DetailForm, strError);
#endif
        }


        // parameters:
        //      strIndicator    �ֶ�ָʾ���������null���ã����ʾ����ָʾ������ɸѡ
        // return:
        //      0   û���ҵ�ƥ�����������
        //      >=1 �ҵ��������ҵ��������������
        /// <summary>
        /// ��ú�һ���ֶ���ص�ƴ�����������
        /// </summary>
        /// <param name="cfg_dom">�洢��������Ϣ�� XmlDocument ����</param>
        /// <param name="strFieldName">�ֶ���</param>
        /// <param name="strIndicator">�ֶ�ָʾ��</param>
        /// <param name="cfg_items">����ƥ������������</param>
        /// <returns>0: û���ҵ�ƥ�����������; >=1: �ҵ���ֵΪ�����������</returns>
        public static int GetPinyinCfgLine(XmlDocument cfg_dom,
            string strFieldName,
            string strIndicator,
            out List<PinyinCfgItem> cfg_items)
        {
            cfg_items = new List<PinyinCfgItem>();

            XmlNodeList nodes = cfg_dom.DocumentElement.SelectNodes("item");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                PinyinCfgItem item = new PinyinCfgItem(node);

                if (item.FieldName != strFieldName)
                    continue;

                if (string.IsNullOrEmpty(item.IndicatorMatchCase) == false
                    && string.IsNullOrEmpty(strIndicator) == false)
                {
                    if (MarcUtil.MatchIndicator(item.IndicatorMatchCase, strIndicator) == false)
                        continue;
                }

                cfg_items.Add(item);
            }

            return cfg_items.Count;
        }

        // parameters:
        //      strPrefix   Ҫ����ƴ�����ֶ�����ǰ����ǰ׺�ַ��������� {cr:NLC} �� {cr:CALIS}
        // return:
        //      -1  ���������жϵ����
        //      0   ����
        /// <summary>
        /// Ϊ MARC �༭���ڵļ�¼��ƴ��
        /// </summary>
        /// <param name="strCfgXml">ƴ������ XML</param>
        /// <param name="bUseCache">�Ƿ�ʹ�ü�¼����ǰ����Ľ����</param>
        /// <param name="style">���</param>
        /// <param name="strPrefix">ǰ׺�ַ�����ȱʡΪ��</param>
        /// <param name="bAutoSel">�Ƿ��Զ�ѡ�������</param>
        /// <returns>-1: ���������жϵ����; 0: ����</returns>
        public virtual int AddPinyin(string strCfgXml,
            bool bUseCache = true,
            PinyinStyle style = PinyinStyle.None,
            string strPrefix = "",
            bool bAutoSel = false)
        {
            string strError = "";
            XmlDocument cfg_dom = new XmlDocument();
            try
            {
                cfg_dom.LoadXml(strCfgXml);
            }
            catch (Exception ex)
            {
                strError = "strCfgXmlװ�ص�XMLDOMʱ����: " + ex.Message;
                goto ERROR1;
            }

            string strRuleParam = "";
            if (string.IsNullOrEmpty(strPrefix) == false)
            {
                string strCmd = StringUtil.GetLeadingCommand(strPrefix);
                if (string.IsNullOrEmpty(strCmd) == false
&& StringUtil.HasHead(strCmd, "cr:") == true)
                {
                    strRuleParam = strCmd.Substring(3);
                }
            }

            this.DetailForm.MarcEditor.Enabled = false;

            Hashtable old_selected = (bUseCache == true) ? this.DetailForm.GetSelectedPinyin() : new Hashtable();
            Hashtable new_selected = new Hashtable();

            try
            {
                // PinyinStyle style = PinyinStyle.None;	// �������޸�ƴ����Сд���

                for (int i = 0; i < DetailForm.MarcEditor.Record.Fields.Count; i++)
                {
                    Field field = DetailForm.MarcEditor.Record.Fields[i];

                    List<PinyinCfgItem> cfg_items = null;
                    int nRet = GetPinyinCfgLine(
                        cfg_dom,
                        field.Name,
                        field.Indicator,
                        out cfg_items);
                    if (nRet <= 0)
                        continue;

                    string strHanzi = "";
                    string strNextSubfieldName = "";

                    string strField = field.Text;


                    string strFieldPrefix = "";

                    // 2012/11/5
                    // �۲��ֶ�����ǰ��� {} ����
                    {
                        string strCmd = StringUtil.GetLeadingCommand(field.Value);
                        if (string.IsNullOrEmpty(strRuleParam) == false
                            && string.IsNullOrEmpty(strCmd) == false
                            && StringUtil.HasHead(strCmd, "cr:") == true)
                        {
                            string strCurRule = strCmd.Substring(3);
                            if (strCurRule != strRuleParam)
                                continue;
                        }
                        else if (string.IsNullOrEmpty(strCmd) == false)
                        {
                            strFieldPrefix = "{" + strCmd + "}";
                        }
                    }

                    // 2012/11/5
                    // �۲� $* ���ֶ�
                    {
                        //
                        string strSubfield = "";
                        string strNextSubfieldName1 = "";
                        // return:
                        //		-1	����
                        //		0	��ָ�������ֶ�û���ҵ�
                        //		1	�ҵ����ҵ������ֶη�����strSubfield������
                        nRet = MarcUtil.GetSubfield(strField,
                            ItemType.Field,
                            "*",    // "*",
                            0,
                            out strSubfield,
                            out strNextSubfieldName1);
                        if (nRet == 1)
                        {
                            string strCurStyle = strSubfield.Substring(1);
                            if (string.IsNullOrEmpty(strRuleParam) == false
                                && strCurStyle != strRuleParam)
                                continue;
                            else if (string.IsNullOrEmpty(strCurStyle) == false)
                            {
                                strFieldPrefix = "{cr:" + strCurStyle + "}";
                            }
                        }
                    }

                    foreach (PinyinCfgItem item in cfg_items)
                    {
                        for (int k = 0; k < item.From.Length; k++)
                        {
                            if (item.From.Length != item.To.Length)
                            {
                                strError = "�������� fieldname='" + item.FieldName + "' from='" + item.From + "' to='" + item.To + "' ����from��to����ֵ���ַ�������";
                                goto ERROR1;
                            }

                            string from = new string(item.From[k], 1);
                            string to = new string(item.To[k], 1);
                            for (int j = 0; ; j++)
                            {

                                // return:
                                //		-1	error
                                //		0	not found
                                //		1	found

                                nRet = MarcUtil.GetSubfield(strField,
                                    ItemType.Field,
                                    from,
                                    j,
                                    out strHanzi,
                                    out strNextSubfieldName);
                                if (nRet != 1)
                                    break;
                                if (strHanzi.Length <= 1)
                                    break;

                                strHanzi = strHanzi.Substring(1);

                                // 2013/6/13
                                if (DetailHost.ContainHanzi(strHanzi) == false)
                                    continue;

                                string strSubfieldPrefix = "";  // ��ǰ���ֶ����ݱ������е�ǰ׺

                                // �������ǰ�����ܳ��ֵ� {} ����
                                string strCmd = StringUtil.GetLeadingCommand(strHanzi);
                                if (string.IsNullOrEmpty(strRuleParam) == false
                                    && string.IsNullOrEmpty(strCmd) == false
                                    && StringUtil.HasHead(strCmd, "cr:") == true)
                                {
                                    string strCurRule = strCmd.Substring(3);
                                    if (strCurRule != strRuleParam)
                                        continue;   // ��ǰ���ֶ����ں�strPrefix��ʾ�Ĳ�ͬ�ı�Ŀ������Ҫ������������ƴ��
                                    strHanzi = strHanzi.Substring(strPrefix.Length); // ȥ�� {} ����
                                }
                                else if (string.IsNullOrEmpty(strCmd) == false)
                                {
                                    strHanzi = strHanzi.Substring(strCmd.Length + 2); // ȥ�� {} ����
                                    strSubfieldPrefix = "{" + strCmd + "}";
                                }

                                string strPinyin;

                                strPinyin = (string)old_selected[strHanzi];
                                if (string.IsNullOrEmpty(strPinyin) == true)
                                {
#if NO
                                    // ���ַ����еĺ��ֺ�ƴ������
                                    // return:
                                    //      -1  ����
                                    //      0   �û�ϣ���ж�
                                    //      1   ����
                                    if (string.IsNullOrEmpty(this.DetailForm.MainForm.PinyinServerUrl) == true
                                       || this.DetailForm.MainForm.ForceUseLocalPinyinFunc == true)
                                    {
                                        nRet = this.DetailForm.MainForm.HanziTextToPinyin(
                                            this.DetailForm,
                                            true,	// ���أ�����
                                            strHanzi,
                                            style,
                                            out strPinyin,
                                            out strError);
                                    }
                                    else
                                    {
                                        // �����ַ���ת��Ϊƴ��
                                        // ����������Ѿ�MessageBox������strError��һ�ַ���Ϊ�ո�
                                        // return:
                                        //      -1  ����
                                        //      0   �û�ϣ���ж�
                                        //      1   ����
                                        //      2   ����ַ�������û���ҵ�ƴ���ĺ���
                                        nRet = this.DetailForm.MainForm.SmartHanziTextToPinyin(
                                            this.DetailForm,
                                            strHanzi,
                                            style,
                                            bAutoSel,
                                            out strPinyin,
                                            out strError);
                                    }
#endif
                                    nRet = this.DetailForm.MainForm.GetPinyin(
                                        this.DetailForm,
                                        strHanzi,
                                        style,
                                        bAutoSel,
                                        out strPinyin,
                                        out strError);
                                    if (nRet == -1)
                                    {
                                        new_selected = null;
                                        goto ERROR1;
                                    }
                                    if (nRet == 0)
                                    {
                                        new_selected = null;
                                        strError = "�û��жϡ�ƴ�����ֶ����ݿ��ܲ�������";
                                        goto ERROR1;
                                    }
                                }

                                if (new_selected != null && nRet != 2)
                                    new_selected[strHanzi] = strPinyin;

                                nRet = MarcUtil.DeleteSubfield(
                                    ref strField,
                                    to,
                                    j);

                                string strContent = strPinyin;

                                if (string.IsNullOrEmpty(strPrefix) == false)
                                    strContent = strPrefix + strPinyin;
                                else if (string.IsNullOrEmpty(strSubfieldPrefix) == false)
                                    strContent = strSubfieldPrefix + strPinyin;
                                    /*
                                else if (string.IsNullOrEmpty(strFieldPrefix) == false)
                                    strContent = strFieldPrefix + strPinyin;
                                     * */

                                nRet = MarcUtil.InsertSubfield(
                                    ref strField,
                                    from,
                                    j,
                                    new string(MarcUtil.SUBFLD, 1) + to + strContent,
                                    1);
                                field.Text = strField;
                            }
                        }
                    }
                }

                if (new_selected != null)
                    this.DetailForm.SetSelectedPinyin(new_selected);
            }
            finally
            {
                this.DetailForm.MarcEditor.Enabled = true;
                this.DetailForm.MarcEditor.Focus();
            }
            return 0;
        ERROR1:
            if (string.IsNullOrEmpty(strError) == false)
            {
                if (strError[0] != ' ')
                    MessageBox.Show(this.DetailForm, strError);
            }
            return -1;
        }

        /// <summary>
        /// Ϊ MARC �༭���ڵļ�¼ɾ��ƴ��
        /// </summary>
        /// <param name="strCfgXml">ƴ������ XML</param>
        /// <param name="strPrefix">ǰ׺�ַ�����ȱʡΪ��</param>
        public virtual void RemovePinyin(string strCfgXml,
            string strPrefix = "")
        {
            string strError = "";
            XmlDocument cfg_dom = new XmlDocument();
            try
            {
                cfg_dom.LoadXml(strCfgXml);
            }
            catch (Exception ex)
            {
                strError = "strCfgXmlװ�ص�XMLDOMʱ����: " + ex.Message;
                goto ERROR1;
            }

            string strRuleParam = "";
            if (string.IsNullOrEmpty(strPrefix) == false)
            {
                string strCmd = StringUtil.GetLeadingCommand(strPrefix);
                if (string.IsNullOrEmpty(strCmd) == false
&& StringUtil.HasHead(strCmd, "cr:") == true)
                {
                    strRuleParam = strCmd.Substring(3);
                }
            }

            this.DetailForm.MarcEditor.Enabled = false;

            try
            {
                for (int i = 0; i < DetailForm.MarcEditor.Record.Fields.Count; i++)
                {
                    Field field = DetailForm.MarcEditor.Record.Fields[i];

                    List<PinyinCfgItem> cfg_items = null;
                    int nRet = GetPinyinCfgLine(
                        cfg_dom,
                        field.Name,
                        field.Indicator,    // TODO: ���Բ�����ָʾ�������������ɾ������Ѱ��Χ
                        out cfg_items);
                    if (nRet <= 0)
                        continue;

                    string strField = field.Text;

                    // 2012/11/6
                    // �۲��ֶ�����ǰ��� {} ����
                    if (string.IsNullOrEmpty(strRuleParam) == false)
                    {
                        string strCmd = StringUtil.GetLeadingCommand(field.Value);
                        if (string.IsNullOrEmpty(strRuleParam) == false
                            && string.IsNullOrEmpty(strCmd) == false
                            && StringUtil.HasHead(strCmd, "cr:") == true)
                        {
                            string strCurRule = strCmd.Substring(3);
                            if (strCurRule != strRuleParam)
                                continue;
                        }
                    }

                    // 2012/11/6
                    // �۲� $* ���ֶ�
                    if (string.IsNullOrEmpty(strRuleParam) == false)
                    {
                        //
                        string strSubfield = "";
                        string strNextSubfieldName1 = "";
                        // return:
                        //		-1	����
                        //		0	��ָ�������ֶ�û���ҵ�
                        //		1	�ҵ����ҵ������ֶη�����strSubfield������
                        nRet = MarcUtil.GetSubfield(strField,
                            ItemType.Field,
                            "*",    // "*",
                            0,
                            out strSubfield,
                            out strNextSubfieldName1);
                        if (nRet == 1)
                        {
                            string strCurStyle = strSubfield.Substring(1);
                            if (string.IsNullOrEmpty(strRuleParam) == false
                                && strCurStyle != strRuleParam)
                                continue;
                        }
                    }

                    bool bChanged = false;
                    foreach (PinyinCfgItem item in cfg_items)
                    {
                        for (int k = 0; k < item.To.Length; k++)
                        {
                            string to = new string(item.To[k], 1);
                            if (string.IsNullOrEmpty(strPrefix) == true)
                            {
                                for (; ; )
                                {
                                    // ɾ��һ�����ֶ�
                                    // ��ʵԭ����ReplaceSubfield()Ҳ���Ե���ɾ����ʹ��
                                    // return:
                                    //      -1  ����
                                    //      0   û���ҵ����ֶ�
                                    //      1   �ҵ���ɾ��
                                    nRet = MarcUtil.DeleteSubfield(
                                        ref strField,
                                        to,
                                        0);
                                    if (nRet != 1)
                                        break;
                                    bChanged = true;
                                }
                            }
                            else
                            {
                                // ֻɾ�������ض�ǰ׺�����ݵ����ֶ�
                                int nDeleteIndex = 0;
                                for (; ; )
                                {
                                    // ɾ��ǰҪ�۲�ƴ�����ֶε�����
                                    bool bDelete = false;
                                    string strContent = "";
                                    string strNextSubfieldName = "";
                                    // return:
                                    //		-1	error
                                    //		0	not found
                                    //		1	found
                                    nRet = MarcUtil.GetSubfield(strField,
                                        ItemType.Field,
                                        to,
                                        nDeleteIndex,
                                        out strContent,
                                        out strNextSubfieldName);
                                    if (nRet != 1)
                                        break;
                                    if (strContent.Length <= 1)
                                        bDelete = true; // �����ݵ����ֶ�Ҫɾ��
                                    else
                                    {
                                        strContent = strContent.Substring(1);
                                        if (StringUtil.HasHead(strContent, strPrefix) == true)
                                            bDelete = true;
                                    }

                                    if (bDelete == false)
                                    {
                                        nDeleteIndex++; // ��������ͬ�����ֶκ����ʵ��
                                        continue;
                                    }

                                    // ɾ��һ�����ֶ�
                                    // ��ʵԭ����ReplaceSubfield()Ҳ���Ե���ɾ����ʹ��
                                    // return:
                                    //      -1  ����
                                    //      0   û���ҵ����ֶ�
                                    //      1   �ҵ���ɾ��
                                    nRet = MarcUtil.DeleteSubfield(
                                        ref strField,
                                        to,
                                        nDeleteIndex);
                                    if (nRet != 1)
                                        break;
                                    bChanged = true;
                                }
                            }
                        }
                    }
                    if (bChanged == true)
                        field.Text = strField;
                }
            }
            finally
            {
                this.DetailForm.MarcEditor.Enabled = true;
                this.DetailForm.MarcEditor.Focus();
            }
            return;
        ERROR1:
            MessageBox.Show(this.DetailForm, strError);
        }
        /*
        // ���һ�����ֶ�
        // ��һ��ĳ�ֶεĵ�һ��ĳ���ֶ�
        // return:
        //	-1	error
        //	0	not found
        //	1	succeed
        public static int GetFirstSubfield(
            MarcEditor MarcEditor,
            string strFieldName,
            string strSubfieldName,
            out string strValue,
            out string strError)
        {
            strError = "";
            strValue = "";

            Field field = MarcEditor.Record.Fields.GetOneField(strFieldName, 0);
            SubfieldCollection subfields = field.Subfields;

            Subfield subfield = subfields[strSubfieldName];

            if (subfield == null)
            {
                strError = "MARC���ֶ� " + strFieldName + "$" + strSubfieldName + " ������";
                return 0;
            }

            strValue = subfield.Value;
            return 1;
        }
         * */

        // �ҵ���һ�����ֶ�����
        // parameters:
        //      location    �ַ������顣ÿ��Ԫ��Ϊ4�ַ���ǰ�����ַ�Ϊ�ֶ��������һ���ַ�Ϊ���ֶ���
        /// <summary>
        /// �ҵ���һ�����ֶ����ݡ��ҵ���һ��ƥ��ķǿ����ֶ����ݾͷ���
        /// </summary>
        /// <param name="location">�ַ������顣������ÿ��Ԫ��Ϊ 4 �ַ���ǰ�����ַ�Ϊ�ֶ��������һ���ַ�Ϊ���ֶ���</param>
        /// <returns>���ֶ���������</returns>
        public string GetFirstSubfield(List<string> location)
        {
            for (int i = 0; i < location.Count; i++)
            {
                string strFieldName = location[i].Substring(0, 3);
                string strSubfieldName = location[i].Substring(3, 1);
                string strValue = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield(
                    strFieldName,
                    strSubfieldName);
                if (String.IsNullOrEmpty(strValue) == false)
                    return strValue;
            }

            return null;
        }

        /// <summary>
        /// ��õ�һ�����ֶ�����
        /// </summary>
        /// <param name="strFieldName">�ֶ���</param>
        /// <param name="strSubfieldName">���ֶ���</param>
        /// <returns>���ֶ���������</returns>
        public string GetFirstSubfield(string strFieldName,
            string strSubfieldName)
        {
            return this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield(
                    strFieldName,
                    strSubfieldName);
        }

        /// <summary>
        /// ���õ�һ�����ֶε���������
        /// </summary>
        /// <param name="strFieldName">�ֶ���</param>
        /// <param name="strSubfieldName">���ֶ���</param>
        /// <param name="strSubfieldValue">Ҫ���õ������ַ���</param>
        public void SetFirstSubfield(string strFieldName,
            string strSubfieldName,
            string strSubfieldValue)
        {
            this.DetailForm.MarcEditor.Record.Fields.SetFirstSubfield(
                    strFieldName,
                    strSubfieldName,
                    strSubfieldValue);
        }

        // 2011/8/9
        /// <summary>
        /// �����һ�����ֶ�����
        /// </summary>
        /// <param name="strFieldName">�ֶ���</param>
        /// <param name="strSubfieldName">���ֶ���</param>
        /// <param name="strIndicatorMatch">�ֶ�ָʾ��ƥ���������ǺŴ��������ַ��������һ�ַ�Ϊ '@'����ʾʹ��������ʽ</param>
        /// <returns>���ֶ���������</returns>
        public string GetFirstSubfield(string strFieldName,
            string strSubfieldName,
            string strIndicatorMatch)
        {
            return this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield(
                    strFieldName,
                    strSubfieldName,
                    strIndicatorMatch);
        }

        // 2011/8/10
        /// <summary>
        /// ����������ֶ�����
        /// </summary>
        /// <param name="strFieldName">�ֶ���</param>
        /// <param name="strSubfieldName">���ֶ���</param>
        /// <param name="strIndicatorMatch">�ֶ�ָʾ��ƥ���������ǺŴ��������ַ��������һ�ַ�Ϊ '@'����ʾʹ��������ʽ</param>
        /// <returns>�ַ�������</returns>
        public List<string> GetSubfields(string strFieldName,
            string strSubfieldName,
            string strIndicatorMatch = "**")
        {
            return this.DetailForm.MarcEditor.Record.Fields.GetSubfields(
                    strFieldName,
                    strSubfieldName,
                    strIndicatorMatch);
        }

        void DoGcatStop(object sender, StopEventArgs e)
        {
            if (this.GcatChannel != null)
                this.GcatChannel.Abort();
        }

        bool bMarcEditorFocued = false;

        /// <summary>
        /// ��ʼ���� GCAT ͨѶ����
        /// </summary>
        /// <param name="strMessage">Ҫ��״̬����ʾ����ʾ��Ϣ</param>
        public void BeginGcatLoop(string strMessage)
        {
            bMarcEditorFocued = this.DetailForm.MarcEditor.Focused;
            this.DetailForm.EnableControls(false);

            Stop stop = this.DetailForm.Progress;

            stop.OnStop += new StopEventHandler(this.DoGcatStop);
            stop.Initial(strMessage);
            stop.BeginLoop();

            this.DetailForm.Update();
            this.DetailForm.MainForm.Update();
        }

        /// <summary>
        /// ���� GCAT ͨѶ����
        /// </summary>
        public void EndGcatLoop()
        {
            Stop stop = this.DetailForm.Progress;
            stop.EndLoop();
            stop.OnStop -= new StopEventHandler(this.DoGcatStop);
            stop.Initial("");

            this.DetailForm.EnableControls(true);
            if (bMarcEditorFocued == true)
                this.DetailForm.MarcEditor.Focus();
        }

        // 2012/4/1
        // ���˳�����Ӣ�ĵ��ַ���
        /// <summary>
        /// ���˳�����Ӣ�ĵ��ַ���
        /// </summary>
        /// <param name="authors">Դ�ַ�������</param>
        /// <returns>���˺���ַ�������</returns>
        public static List<string> NotContainHanzi(List<string> authors)
        {
            List<string> results = new List<string>();
            foreach (string strAuthor in authors)
            {
                if (ContainHanzi(strAuthor) == false)
                    results.Add(strAuthor);
            }

            return results;
        }

        // ���˳��������ֵ��ַ���
        /// <summary>
        /// ���˳��������ֵ��ַ���
        /// </summary>
        /// <param name="authors">Դ�ַ�������</param>
        /// <returns>���˺���ַ�������</returns>
        public static List<string> ContainHanzi(List<string> authors)
        {
            List<string> results = new List<string>();
            foreach (string strAuthor in authors)
            {
                if (ContainHanzi(strAuthor) == true)
                    results.Add(strAuthor);
            }

            return results;
        }

        /// <summary>
        /// �ж�һ���ַ������Ƿ��������
        /// </summary>
        /// <param name="strAuthor">Ҫ�жϵ��ַ���</param>
        /// <returns>�Ƿ��������</returns>
        public static bool ContainHanzi(string strAuthor)
        {
            strAuthor = strAuthor.Trim();
            if (string.IsNullOrEmpty(strAuthor) == true)
                return false;

            string strError = "";
            string strResult = "";
            int nRet = PrepareSjhmAuthorString(strAuthor,
                out strResult,
                out strError);
            if (string.IsNullOrEmpty(strResult) == true)
                return false;
            return true;
        }

        // �Լ���ȡ�ĽǺ���������ַ�������Ԥ�ӹ�������ȥ�����зǺ����ַ�
        /// <summary>
        /// �Լ���ȡ�ĽǺ���������ַ�������Ԥ�ӹ�������ȥ�����зǺ����ַ�
        /// </summary>
        /// <param name="strAuthor">Դ�ַ���</param>
        /// <param name="strResult">���ؽ���ַ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ����</returns>
        public static int PrepareSjhmAuthorString(string strAuthor,
            out string strResult,
            out string strError)
        {
            strResult = "";
            strError = "";

            // string strSpecialChars = "���������������������������������ۣݡ����������������ܣ�������������";

            for (int i = 0; i < strAuthor.Length; i++)
            {
                char ch = strAuthor[i];

                if (StringUtil.IsHanzi(ch) == false)
                    continue;

                // �����Ƿ��������
                if (StringUtil.SpecialChars.IndexOf(ch) != -1)
                {
                    continue;
                }

                // ����
                strResult += ch;
            }

            return 0;
        }

        // 2011/12/18
        // ������ߺ� -- Cutter-Sanborn Three-Figure
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        /// <summary>
        /// ��ÿ��� (Cutter-Sanborn Three-Figure) ���ߺš����������Ա��ű�����
        /// </summary>
        /// <param name="strAuthor">�����ַ���</param>
        /// <param name="strAuthorNumber">�������ߺ�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public virtual int GetCutterAuthorNumber(string strAuthor,
            out string strAuthorNumber,
            out string strError)
        {
            strError = "";
            strAuthorNumber = "";

            int nRet = this.DetailForm.MainForm.LoadQuickCutter(true, out strError);
            if (nRet == -1)
                return -1;

            string strText = "";
            string strNumber = "";
        // return:
        //      -1  error
        //      0   not found
        //      1   found
            nRet = this.DetailForm.MainForm.QuickCutter.GetEntry(strAuthor,
                out strText,
                out strNumber,
                out strError);
            if (nRet == -1 || nRet == 0)
                return -1;

            strAuthorNumber = strText[0] + strNumber;
            return 1;
        }

        // ������ߺ� -- �ĽǺ���
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        /// <summary>
        /// ����ֽк������ߺš����������Ա��ű�����
        /// </summary>
        /// <param name="strAuthor">�����ַ���</param>
        /// <param name="strAuthorNumber">�������ߺ�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public virtual int GetSjhmAuthorNumber(string strAuthor,
            out string strAuthorNumber,
            out string strError)
        {
            strError = "";
            strAuthorNumber = "";

            string strResult = "";
            int nRet = PrepareSjhmAuthorString(strAuthor,
            out strResult,
            out strError);
            if (nRet == -1)
                return -1;
            if (String.IsNullOrEmpty(strResult) == true)
            {
                strError = "�����ַ��� '" + strAuthor + "' ����û�а�����Ч�ĺ����ַ�";
                return -1;
            }

            List<string> sjhms = null;
            // ���ַ����еĺ���ת��Ϊ�ĽǺ���
            // parameters:
            //      bLocal  �Ƿ�ӱ��ػ�ȡ�ĽǺ���
            // return:
            //      -1  ����
            //      0   �û�ϣ���ж�
            //      1   ����
            nRet = this.DetailForm.MainForm.HanziTextToSjhm(
                true,
                strResult,
                out sjhms,
                out strError);
            if (nRet != 1)
                return nRet;

            if (strResult.Length != sjhms.Count)
            {
                strError = "�����ַ��� '" + strResult + "' ������ַ���(" + strResult.Length.ToString() + ")��ȡ�ĽǺ����Ľ��������� " + sjhms.Count.ToString() + " ����";
                return -1;
            }

            // 1����������Ϊһ���ߣ�ȡ���ֵ��ĽǺ��롣�磺Ф=9022
            if (strResult.Length == 1)
            {
                strAuthorNumber = sjhms[0].Substring(0, 4);
                return 1;
            }
            // 2����������Ϊ�����ߣ��ֱ�ȡ�����ֵ����ϽǺ����Ͻǡ��磺����=0287
            if (strResult.Length == 2)
            {
                strAuthorNumber = sjhms[0].Substring(0, 2) + sjhms[1].Substring(0, 2);
                return 1;
            }

            // 3����������Ϊ�����ߣ�����ȡ�������ϡ��������Ǻͺ����ֵ����Ͻǡ��磺�޹���=6075
            if (strResult.Length == 3)
            {
                strAuthorNumber = sjhms[0].Substring(0, 2)
                    + sjhms[1].Substring(0, 1)
                    + sjhms[2].Substring(0, 1);
                return 1;
            }

            // 4����������Ϊ�����ߣ�����ȡ���ֵ����Ͻǡ��磺����Ӣ��=5645
            // 5�����ּ����������ߣ�����ǰ����ȡ�ţ�����ͬ�ϡ��磺��˹�����˹��=2423
            if (strResult.Length >= 4)
            {
                strAuthorNumber = sjhms[0].Substring(0, 1)
                    + sjhms[1].Substring(0, 1)
                    + sjhms[2].Substring(0, 1)
                    + sjhms[3].Substring(0, 1);
                return 1;
            }

            strError = "error end";
            return -1;
        }

        // ������ߺ�
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        /// <summary>
        /// ��� GCAT (ͨ�ú������ߺ����) ���ߺ�
        /// </summary>
        /// <param name="strGcatWebServiceUrl">GCAT Webservice URL ��ַ</param>
        /// <param name="strAuthor">�����ַ���</param>
        /// <param name="strAuthorNumber">�������ߺ�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public int GetGcatAuthorNumber(string strGcatWebServiceUrl,
            string strAuthor,
            out string strAuthorNumber,
            out string strError)
        {
            strError = "";
            strAuthorNumber = "";

            if (String.IsNullOrEmpty(strGcatWebServiceUrl) == true)
                strGcatWebServiceUrl = "http://dp2003.com/gcatserver/";  //  "http://dp2003.com/dp2libraryws/gcat.asmx";

            if (strGcatWebServiceUrl.IndexOf(".asmx") != -1)
            {

                if (this.GcatChannel == null)
                    this.GcatChannel = new DigitalPlatform.GcatClient.Channel();

                string strDebugInfo = "";

                BeginGcatLoop("���ڻ�ȡ '" + strAuthor + "' �����ߺţ��� " + strGcatWebServiceUrl + " ...");
                try
                {
                    // return:
                    //      -1  error
                    //      0   canceled
                    //      1   succeed
                    int nRet = this.GcatChannel.GetNumber(
                        this.DetailForm.Progress,
                        this.DetailForm,
                        strGcatWebServiceUrl,
                        strAuthor,
                        true,	// bSelectPinyin
                        true,	// bSelectEntry
                        true,	// bOutputDebugInfo
                        new DigitalPlatform.GcatClient.BeforeLoginEventHandle(gcat_channel_BeforeLogin),
                        out strAuthorNumber,
                        out strDebugInfo,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ȡ ���� '" + strAuthor + "' ֮����ʱ���� : " + strError;
                        return -1;
                    }

                    return nRet;
                }
                finally
                {
                    EndGcatLoop();
                }
            }
            else
            {
                // �µ�WebService

                string strID = this.DetailForm.MainForm.AppInfo.GetString("DetailHost", "gcat_id", "");
                bool bSaveID = this.DetailForm.MainForm.AppInfo.GetBoolean("DetailHost", "gcat_saveid", false);

                Hashtable question_table = (Hashtable)this.DetailForm.MainForm.ParamTable["question_table"];
                if (question_table == null)
                    question_table = new Hashtable();

            REDO_GETNUMBER:
                string strDebugInfo = "";

                BeginGcatLoop("���ڻ�ȡ '" + strAuthor + "' �����ߺţ��� " + strGcatWebServiceUrl + " ...");
                try
                {
                    // return:
                    //      -1  error
                    //      0   canceled
                    //      1   succeed
                    int nRet = GcatNew.GetNumber(
                        ref question_table,
                        this.DetailForm.Progress,
                        this.DetailForm,
                        strGcatWebServiceUrl,
                        strID, // ID
                        strAuthor,
                        true,	// bSelectPinyin
                        true,	// bSelectEntry
                        true,	// bOutputDebugInfo
                        out strAuthorNumber,
                        out strDebugInfo,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "ȡ ���� '" + strAuthor + "' ֮����ʱ���� : " + strError;
                        return -1;
                    }
                    if (nRet == -2)
                    {
                        IdLoginDialog login_dlg = new IdLoginDialog();
                        GuiUtil.SetControlFont(login_dlg, this.DetailForm.MainForm.DefaultFont, false);
                        login_dlg.Text = "������ߺ� -- "
                            + ((string.IsNullOrEmpty(strID) == true) ? "������ID" : strError);
                        login_dlg.ID = strID;
                        login_dlg.SaveID = bSaveID;
                        login_dlg.StartPosition = FormStartPosition.CenterScreen;
                        if (login_dlg.ShowDialog(this.DetailForm) == DialogResult.Cancel)
                        {
                            return -1;
                        }

                        strID = login_dlg.ID;
                        bSaveID = login_dlg.SaveID;
                        if (login_dlg.SaveID == true)
                        {
                            this.DetailForm.MainForm.AppInfo.SetString("DetailHost", "gcat_id", strID);
                        }
                        else
                        {
                            this.DetailForm.MainForm.AppInfo.SetString("DetailHost", "gcat_id", "");
                        }
                        this.DetailForm.MainForm.AppInfo.SetBoolean("DetailHost", "gcat_saveid", bSaveID);
                        goto REDO_GETNUMBER;
                    }

                    this.DetailForm.MainForm.ParamTable["question_table"] = question_table;

                    return nRet;
                }
                finally
                {
                    EndGcatLoop();
                }
            }
        }

        // ��һ����ȡ���ַ�����������һ��
        // parameters:
        //      strCallNumberStyle  ��ȡ����̬��Ϊ ��ȡ���+���ֺ�/�ݲش���+��ȡ���+���ֺ� ֮һ��ȱʡΪǰ��
        /// <summary>
        /// ��һ����ȡ���ַ��������� �ݲش��� ��
        /// </summary>
        /// <param name="strCallNumberStyle">��ȡ����̬��Ϊ "��ȡ���+���ֺ�" "�ݲش���+��ȡ���+���ֺ�" ֮һ��ȱʡΪǰ��</param>
        /// <param name="strCallNumber">��ȡ���ַ���</param>
        /// <returns>�ݲش��� �С������ȡ����û����һ�У��ͷ��� null</returns>
        public virtual string GetHeadLinePart(
            string strCallNumberStyle,
            string strCallNumber)
        {
#if NO
            string[] parts = strCallNumber.Split(new char[] { '/' });

            if (string.IsNullOrEmpty(strCallNumberStyle) == true
                || strCallNumberStyle == "����"
                || strCallNumberStyle == "����")
            {
                return null;
            }
            else if (strCallNumberStyle == "�ݲش���+��ȡ���+���ֺ�"
                || strCallNumberStyle == "����")
            {
                if (parts.Length > 0)
                    return parts[0].Trim();
            }
            return null;
#endif
            // ����ȡ���ݴ��Ը���
            return StringUtil.GetCallNumberHeadLine(strCallNumber);
        }

        // ��һ����ȡ���ַ�����������Ų���
        // ����ȡ����HeadLineʱ����Ҫ���ر�������ȡ�ڶ���Ϊ��ȡ��š�ע�������ż���Ϣ�󣬾Ͳ������ر�������
        // parameters:
        //      strCallNumberStyle  ��ȡ����̬��Ϊ ��ȡ���+���ֺ�/�ݲش���+��ȡ���+���ֺ� ֮һ��ȱʡΪǰ��
        /// <summary>
        /// ��һ����ȡ���ַ�����������Ų���
        /// </summary>
        /// <param name="strCallNumberStyle">��ȡ����̬��Ϊ "��ȡ���+���ֺ�" "�ݲش���+��ȡ���+���ֺ�" ֮һ��ȱʡΪǰ��</param>
        /// <param name="strCallNumber">��ȡ���ַ���</param>
        /// <returns>��ȡ��� ��</returns>
        public virtual string GetClassPart(
            string strCallNumberStyle,
            string strCallNumber)
        {
#if NO
            string[] parts = strCallNumber.Split(new char[] {'/'});

            if (string.IsNullOrEmpty(strCallNumberStyle) == true
    || strCallNumberStyle == "��ȡ���+���ֺ�"
    || strCallNumberStyle == "����"
                || strCallNumberStyle == "����")
            {
                if (parts.Length > 0)
                    return parts[0].Trim();
            }
            else if (strCallNumberStyle == "�ݲش���+��ȡ���+���ֺ�"
                || strCallNumberStyle == "����")
            {
                if (parts.Length > 1)
                    return parts[1].Trim();
            }
#endif
            // ����ȡ���ݴ��Ը���
            strCallNumber = StringUtil.BuildLocationClassEntry(strCallNumber);

            string[] parts = strCallNumber.Split(new char[] { '/' });
            if (parts.Length > 0)
                return parts[0].Trim();

            return "";
        }

        // (��MARC�༭����)�����ȡ����Ų���
        // ����˵������������Ͻ��strClassTypeӳ�䵽��ȡ����Ų��֡�
        //      ���������滹������GetCallNumberClassSource()
        //      ��������˱����������л���ı�strClassType����Դ�ֶ���/���ֶ�����ӳ���߼����Լ���MARC�༭���ڻ�ȡ��ȡ����ַ������߼�
        // return:
        //      -1  error
        //      0   MARC��¼��û���ҵ��������Դ�ֶ�/���ֶ�����
        //      1   succeed
        /// <summary>
        /// ��MARC�༭���л����ȡ����Ų���
        /// ����˵������������Ͻ�� strClassType ӳ�䵽��ȡ����Ų��֡�
        ///      �������ڲ������� GetCallNumberClassSource() ʵ�ֹ���
        ///      ��������˱����������л���ı� strClassType ����Դ�ֶ���/���ֶ�����ӳ���߼����Լ��� MARC �༭���ڻ�ȡ��ȡ����ַ������߼�
        /// </summary>
        /// <param name="strClassType">�������</param>
        /// <param name="strClass">�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: MARC ��¼��û���ҵ��������Դ�ֶ�/���ֶ�����; 1: �ɹ�</returns>
        public virtual int GetCallNumberClassPart(
            string strClassType,
            out string strClass,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            strClass = "";

            string strFieldName = "";
            string strSubfieldName = "";

            // �����ȡ����Ų��ֵ���Դ�ֶ��������ֶ���
            // return:
            //      -1  error
            //      1   succeed
            nRet = GetCallNumberClassSource(
                strClassType,
                out strFieldName,
                out strSubfieldName,
                out strError);
            if (nRet == -1)
            {
                strError = "��ȡ��ȡ����Ų�����Դ�ֶκ����ֶ���ʱ����: " + strError;
                return -1;
            }

            strClass = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield(
                strFieldName,
                strSubfieldName);
            if (String.IsNullOrEmpty(strClass) == true)
            {
                strError = "MARC��¼�� " + strFieldName + "$" + strSubfieldName + " û���ҵ�������޷������ȡ���";
                return 0;
            }

            return 1;
        }

        // �����ȡ����Ų��ֵ���Դ�ֶ��������ֶ���
        // ����˵������������Ͻ��strClassTypeӳ�䵽��ȡ����Ų��ֵ���Դ�ֶ���/���ֶ�����
        //      ��������ر�������������ı�(�ϲ㺯��GetCallNumberClassPart())��MARC�༭���ڻ�ȡ��ȡ����ַ������߼�
        // return:
        //      -1  error
        //      1   succeed
        /// <summary>
        /// �����ȡ����Ų��ֵ���Դ�ֶ��������ֶ���
        /// ����˵������������Ͻ�� strClassType ӳ�䵽��ȡ����Ų��ֵ���Դ�ֶ���/���ֶ�����
        ///      ��������ر�������������ı�(�ϲ㺯��GetCallNumberClassPart())�� MARC �༭���ڻ�ȡ��ȡ����ַ������߼�
        /// </summary>
        /// <param name="strClassType">�������</param>
        /// <param name="strFieldName">�����ֶ���</param>
        /// <param name="strSubfieldName">�������ֶ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public virtual int GetCallNumberClassSource(
            string strClassType,
            out string strFieldName,
            out string strSubfieldName,
            out string strError)
        {
            strError = "";
            strFieldName = "";
            strSubfieldName = "";

            string strMarcSyntax = this.DetailForm.GetCurrentMarcSyntax();

            if (strMarcSyntax == "unimarc")
            {
                if (strClassType == "��ͼ��")
                {
                    strFieldName = "690";
                    strSubfieldName = "a";
                }
                else if (strClassType == "��ͼ��")
                {
                    strFieldName = "692";
                    strSubfieldName = "a";
                }
                else if (strClassType == "�˴�")
                {
                    strFieldName = "694";
                    strSubfieldName = "a";
                }
                else if (strClassType == "����")
                {
                    strFieldName = "686";
                    strSubfieldName = "a";
                }
#if SHITOUTANG
                else if (strClassType == "ʯͷ�����෨"
                    || strClassType == "ʯͷ�������"
                    || strClassType == "ʯͷ��")
                {
                    strFieldName = "687";
                    strSubfieldName = "a";
                }
#endif
                else
                {
                    strError = "UNIMARC��δ֪�ķ��෨ '" + strClassType + "'";
                    return -1;
                }
            }
            else if (strMarcSyntax == "usmarc")
            {
                if (strClassType == "����ʮ�������"
                    || strClassType == "����ʮ�����෨"
                    || strClassType == "DDC")
                {
                    strFieldName = "082";
                    strSubfieldName = "a";
                }
                else if (strClassType == "����ʮ�������"
                    || strClassType == "����ʮ�����෨"
                    || strClassType == "UDC")
                {
                    strFieldName = "080";
                    strSubfieldName = "a";
                }
                else if (strClassType == "����ͼ��ݷ��෨"
                    || strClassType == "��������ͼ��ݷ��෨"
                    || strClassType == "LCC")
                {
                    strFieldName = "050";
                    strSubfieldName = "a";
                }
                else if (strClassType == "��ͼ��")
                {
                    strFieldName = "093";
                    strSubfieldName = "a";
                }
                else if (strClassType == "��ͼ��")
                {
                    strFieldName = "094";
                    strSubfieldName = "a";
                }
                else if (strClassType == "�˴�")
                {
                    strFieldName = "095";
                    strSubfieldName = "a";
                }
                else if (strClassType == "����")
                {
                    strFieldName = "084";
                    strSubfieldName = "a";
                }
#if SHITOUTANG
                else if (strClassType == "ʯͷ�����෨"
                    || strClassType == "ʯͷ�������"
                    || strClassType == "ʯͷ��")
                {
                    strFieldName = "087";
                    strSubfieldName = "a";
                }
#endif
                else
                {
                    strError = "USMARC��δ֪�ķ��෨ '" + strClassType + "'";
                    return -1;
                }
            }
            else
            {
                strError = "δ֪��MARC��ʽ '" + strMarcSyntax + "'";
                return -1;
            }

            return 1;
        }

        // ������ȡ��
        // senderΪEntityControl����ʱ����ѡ�����ֻ��Ϊ1��
        /// <summary>
        /// ������ȡ��
        /// </summary>
        /// <param name="sender">�¼������ߡ��� sender Ϊ EntityControl ����ʱ����ѡ�����ֻ��Ϊ 1 ��</param>
        /// <param name="e">�¼�����</param>
        public virtual void ManageCallNumber(object sender,
            GenerateDataEventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (sender == null)
            {
                strError = "senderΪnull";
                goto ERROR1;
            }

            string strLocation = "";
            string strClass = "";
            string strItemRecPath = "";

            List<CallNumberItem> callnumber_items = null;

            ArrangementInfo info = null;

            if (sender is EntityEditForm)
            {
                EntityEditForm edit = (EntityEditForm)sender;

                // ȡ�ùݲصص�
                strLocation = edit.entityEditControl_editing.LocationString;
                strLocation = StringUtil.GetPureLocationString(strLocation);  // 2009/3/29 new add

                /*
                if (String.IsNullOrEmpty(strLocation) == true)
                {
                    strError = "��������ݲصص㡣�����޷�������ȡ��";
                    goto ERROR1;
                }*/


                // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
                nRet = this.DetailForm.MainForm.GetArrangementInfo(strLocation,
                    out info,
                    out strError);
                if (nRet == 0)
                {
                    strError = "û�й��ڹݲصص� '" + strLocation + "' ���ż���ϵ������Ϣ���޷�������ȡ��";
                    goto ERROR1;
                }
                if (nRet == -1)
                    goto ERROR1;


                // ������е����
                strClass = GetClassPart(
                    info.CallNumberStyle,
                    edit.entityEditControl_editing.AccessNo);

                strItemRecPath = edit.entityEditControl_editing.RecPath;

                callnumber_items = edit.Items.GetCallNumberItems();
            }
            else if (sender is EntityControl)
            {
                EntityControl control = (EntityControl)sender;

                if (control.ListView.SelectedIndices.Count == 0)
                {
                    strError = "����ѡ��Ҫ��ע���С������޷�������ȡ��";
                    goto ERROR1;
                }

                if (control.ListView.SelectedIndices.Count > 1)
                {
                    strError = "��ǰѡ���ж���1��(Ϊ " + control.ListView.SelectedIndices.Count + " ��)���޷���������ȡ�š���ֻѡ��һ�У�Ȼ����ʹ�ñ�����";
                    goto ERROR1;
                }

                BookItem book_item = control.GetVisibleItemAt(control.ListView.SelectedIndices[0]);
                Debug.Assert(book_item != null, "");

                strLocation = book_item.Location;
                strLocation = StringUtil.GetPureLocationString(strLocation);  // 2009/3/29 new add

                /*
                if (String.IsNullOrEmpty(strLocation) == true)
                {
                    strError = "��������ݲصص㡣�����޷�������ȡ��";
                    goto ERROR1;
                }*/

                // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
                nRet = this.DetailForm.MainForm.GetArrangementInfo(strLocation,
                    out info,
                    out strError);
                if (nRet == 0)
                {
                    strError = "û�й��ڹݲصص� '" + strLocation + "' ���ż���ϵ������Ϣ���޷�������ȡ��";
                    goto ERROR1;
                }
                if (nRet == -1)
                    goto ERROR1;

                // ������е����
                strClass = GetClassPart(
                    info.CallNumberStyle,
                    book_item.AccessNo);

                strItemRecPath = book_item.RecPath;

                callnumber_items = control.Items.GetCallNumberItems();
            }
            else if (sender is BindingForm)
            {
                BindingForm binding = (BindingForm)sender;

                // ȡ�ùݲصص�
                strLocation = binding.EntityEditControl.LocationString;
                strLocation = StringUtil.GetPureLocationString(strLocation);  // 2009/3/29 new add

                // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
                nRet = this.DetailForm.MainForm.GetArrangementInfo(strLocation,
                    out info,
                    out strError);
                if (nRet == 0)
                {
                    strError = "û�й��ڹݲصص� '" + strLocation + "' ���ż���ϵ������Ϣ���޷�������ȡ��";
                    goto ERROR1;
                }
                if (nRet == -1)
                    goto ERROR1;


                // ������е����
                strClass = GetClassPart(
                    info.CallNumberStyle,
                    binding.EntityEditControl.AccessNo);

                strItemRecPath = binding.EntityEditControl.RecPath;

                callnumber_items = binding.GetCallNumberItems();    // ???
            }
            else
            {
                strError = "sender������EntityEditForm��EntityControl��BindingForm����(��ǰΪ"+sender.GetType().ToString()+")";
                goto ERROR1;
            }


            // �����ǰ���¼�в�������ȡ����ţ���ȥMARC��¼����
            if (String.IsNullOrEmpty(strClass) == true)
            {
                // (��MARC�༭����)�����ȡ����Ų���
                // return:
                //      -1  error
                //      0   MARC��¼��û���ҵ��������Դ�ֶ�/���ֶ�����
                //      1   succeed
                nRet = GetCallNumberClassPart(
                    info.ClassType,
                    out strClass,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�����ȡ���ʱ����: " + strError;
                    goto ERROR1;
                }
                if (nRet == 0)
                {
                    goto ERROR1;
                }

                Debug.Assert(nRet == 1, "");

                MessageBox.Show(this.DetailForm, "��ǰ���¼��û����ȡ�ţ��Զ�����Ŀ��¼��ȡ����ȡ��� '" + strClass + "'������������ȡ�Ź���");
            }

            CallNumberForm dlg = new CallNumberForm();

            // dlg.MdiParent = this.DetailForm.MainForm;   // ��ΪMDI�Ӵ���
            dlg.MainForm = this.DetailForm.MainForm;
            // dlg.TopMost = true;  // ��Ϊ��ģʽ�Ի���
            dlg.MyselfItemRecPath = strItemRecPath;
            dlg.MyselfParentRecPath = this.DetailForm.BiblioRecPath;

            dlg.MyselfCallNumberItems = callnumber_items;   // 2009/6/4 new add

            dlg.ClassNumber = strClass;
            dlg.LocationString = strLocation;
            dlg.AutoBeginSearch = true;

            dlg.Floating = true;

            dlg.FormClosed -= new FormClosedEventHandler(dlg_FormClosed);
            dlg.FormClosed += new FormClosedEventHandler(dlg_FormClosed);

            this.DetailForm.MainForm.AppInfo.LinkFormState(dlg, "callnumber_floating_state");
            dlg.Show();

            return;
        ERROR1:
            MessageBox.Show(this.DetailForm, strError);
        }

        void dlg_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (sender != null)
            {
                // this.DetailForm.MainForm.AppInfo.UnlinkFormState(sender as Form);
            }
        }
#if NO
        #region ��ɽͼ����ĽǺ��롣��������֤�á�ʵ��Ӧ�õ�ʱ����Ҫд�ڽű���

        // ����ִκ�������������ֺţ���Ҫ�����ߺ�
        // return:
        //      -1  error
        //      0   not found��ע���ʱҲҪ����strErrorֵ
        //      1   found
        public int LstsgGetAuthorNumber(string strQufenhaoType,
                out string strQufenhao,
                out string strError)
        {
            strError = "";
            strQufenhao = "";

            if (strQufenhaoType == "�ĽǺ���")
            {
                bool bPerson = false;

                List<string> results = null;
                // 700��710��720
                results = GetSubfields("700", "a", "@[^A].");    // ָʾ��
                results = ContainHanzi(results);
                if (results.Count > 0)
                {
                    bPerson = true;
                    goto FOUND;
                }
                results = GetSubfields("710", "a");
                results = ContainHanzi(results);
                if (results.Count > 0)
                {
                    bPerson = false;
                    goto FOUND;
                }
                results = GetSubfields("720", "a");
                results = ContainHanzi(results);
                if (results.Count > 0)
                {
                    bPerson = false;
                    goto FOUND;
                }

                // 701/711/702/712
                results = GetSubfields("701", "a", "@[^A].");   // ָʾ��
                results = ContainHanzi(results);
                if (results.Count > 0)
                {
                    bPerson = true;
                    goto FOUND;
                }

                results = GetSubfields("711", "a");
                results = ContainHanzi(results);
                if (results.Count > 0)
                {
                    bPerson = false;
                    goto FOUND;
                }

                results = GetSubfields("702", "a", "@[^A].");   // ָʾ��
                results = ContainHanzi(results);
                if (results.Count > 0)
                {
                    bPerson = true;
                    goto FOUND;
                }

                results = GetSubfields("712", "a");
                results = ContainHanzi(results);
                if (results.Count > 0)
                {
                    bPerson = false;
                    goto FOUND;
                }

                strError = "MARC��¼�� 700/710/720/701/711/702/712�о�δ���ְ������ֵ� $a ���ֶ����ݣ��޷���������ַ���";
                return -1;
            FOUND:
                Debug.Assert(results.Count > 0, "");
                if (bPerson == true)
                {
                    // ������ߺ�
                    // return:
                    //      -1  error
                    //      0   canceled
                    //      1   succeed
                    int nRet = LstsgGetSjhmAuthorNumber(
                        results[0],
                        out strQufenhao,
                        out strError);
                    if (nRet != 1)
                        return nRet;
                    return 1;
                }

                string strISBN = GetFirstSubfield("010", "a");
                // ���������ߣ���¼��û��ISBN
                if (String.IsNullOrEmpty(strISBN) == true)
                {
                    // ������ߺ�
                    // return:
                    //      -1  error
                    //      0   canceled
                    //      1   succeed
                    int nRet = LstsgGetSjhmAuthorNumber(
                        results[0],
                        out strQufenhao,
                        out strError);
                    if (nRet != 1)
                        return nRet;
                    return 1;
                }

                // ���������ߣ���¼����ISBN
                if (strISBN.IndexOf("-") == -1)
                {
                    strError = "����������ȡ���ߺŵ�ʱ����Ҫ�õ�ISBN�����ǵ�ǰISBN '"+strISBN+"' ��û�з���'-'������ΪISBN����'-'�Ժ���ȡ���ߺ�";
                    return -1;
                }
                try
                {
                    string strPublisher = IsbnSplitter.GetPublisherCode(strISBN);

                    if (string.IsNullOrEmpty(strPublisher) == true)
                    {
                        strError = "ISBN '"+strISBN+"' �е���������벿�ָ�ʽ����ȷ";
                        return -1;
                    }
                    // ���������Ϊ�������ģ���������������㣬���������Ϊ������ģ�ȡǰ����λ��
                    if (strPublisher.Length < 4)
                        strQufenhao = strPublisher.PadRight(4-strPublisher.Length, '0');
                    else if (strPublisher.Length >= 5)
                        strQufenhao = strPublisher.Substring(0, 4);
                    else
                        strQufenhao = strPublisher;
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }

                return 1;
            }
            else
            {
                strError = "LstsgGetAuthorNumber() ֻ֧���ĽǺ������͵����ߺ�";
                return -1;
            }

        }


        // ������ߺ� -- �ĽǺ���
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        public int LstsgGetSjhmAuthorNumber(string strAuthor,
            out string strAuthorNumber,
            out string strError)
        {
            strError = "";
            strAuthorNumber = "";

            string strResult = "";
            int nRet = PrepareSjhmAuthorString(strAuthor,
            out strResult,
            out strError);
            if (nRet == -1)
                return -1;
            if (String.IsNullOrEmpty(strResult) == true)
            {
                strError = "�����ַ��� '" + strAuthor + "' ����û�а�����Ч�ĺ����ַ�";
                return -1;
            }

            List<string> sjhms = null;
            // ���ַ����еĺ���ת��Ϊ�ĽǺ���
            // parameters:
            //      bLocal  �Ƿ�ӱ��ػ�ȡ�ĽǺ���
            // return:
            //      -1  ����
            //      0   �û�ϣ���ж�
            //      1   ����
            nRet = this.DetailForm.MainForm.HanziTextToSjhm(
                true,
                strResult,
                out sjhms,
                out strError);
            if (nRet != 1)
                return nRet;

            if (strResult.Length != sjhms.Count)
            {
                strError = "�����ַ��� '" + strResult + "' ������ַ���(" + strResult.Length.ToString() + ")��ȡ�ĽǺ����Ľ��������� " + sjhms.Count.ToString() + " ����";
                return -1;
            }

            // 1����������Ϊһ���ߣ�Ϊһ���ߣ�ȡ�����Ͻǣ������������
            if (strResult.Length == 1)
            {
                strAuthorNumber = sjhms[0].Substring(0, 2) + "00";
                return 1;
            }
            // 2����������Ϊ�����ߣ���ȡ�����Ͻ�
            if (strResult.Length == 2)
            {
                strAuthorNumber = sjhms[0].Substring(0, 2) + sjhms[1].Substring(0, 2);
                return 1;
            }

            // 3����������Ϊ�����ߣ���һ����ȡ�����Ͻǣ���������ָ�ȡ���Ͻ�
            if (strResult.Length == 3)
            {
                strAuthorNumber = sjhms[0].Substring(0, 2)
                    + sjhms[1].Substring(0, 1)
                    + sjhms[2].Substring(0, 1);
                return 1;
            }

            // 4����������Ϊ�����ߣ�ȡ�ĸ��ֵ����Ͻ�
            if (strResult.Length >= 4)
            {
                strAuthorNumber = sjhms[0].Substring(0, 1)
                    + sjhms[1].Substring(0, 1)
                    + sjhms[2].Substring(0, 1)
                    + sjhms[3].Substring(0, 1);
                return 1;
            }

            // 5����������ϵ�ֻȡǰ������
            if (strResult.Length >= 5)
            {
                strAuthorNumber = sjhms[0].Substring(0, 2)
                    + sjhms[1].Substring(0, 1)
                    + sjhms[2].Substring(0, 1);
                return 1;
            } 
            
            strError = "error end";
            return -1;
        }

        #endregion

#endif

        class AuthorLevel
        {
            /// <summary>
            /// �����ַ���
            /// </summary>
            public string Author = "";
            /// <summary>
            /// �����ַ����ļ��� -1 ������Ϣ, 0 û���ҵ����ߵ���ʾ��Ϣ, 1 ����, 2 ����
            /// </summary>
            public float Level = 0;

            /// <summary>
            /// ���ֺ�����
            /// </summary>
            public string Type = "";
        }

        class AuthorLevelComparer : IComparer<AuthorLevel>
        {
            int IComparer<AuthorLevel>.Compare(AuthorLevel x, AuthorLevel y)
            {
                // ���Ծ�ȷ�� 0.01
                return (int)(-100 * (x.Level - y.Level)); // ����ǰ
            }
        }


        // ����ִκ�������������ֺţ���Ҫ�����ߺ�
        // return:
        //      -1  error
        //      0   not found��ע���ʱҲҪ����strErrorֵ
        //      1   found
        /// <summary>
        /// ����ִκ�������������ֺţ���Ҫ�����ߺ�
        /// </summary>
        /// <param name="strQufenhaoTypes">���ֺ����͡�������һ�����ֺ����ͣ�Ҳ�����Ƕ��ż�������ɸ����ֺ�����</param>
        /// <param name="strQufenhao">�������ֺ�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û���ҵ�(ע���ʱ strError ��Ҳ����������); 1: �ҵ�</returns>
        public virtual int GetAuthorNumber(string strQufenhaoTypes,
            out string strQufenhao,
            out string strError)
        {
            strError = "";
            strQufenhao = "";
            int nRet = 0;

            List<string> types = StringUtil.SplitList(strQufenhaoTypes);

            List<AuthorLevel> authors = new List<AuthorLevel>();

            // *** ��һ�׶Σ�������ȡÿ�����ֺ����͵������ַ���
            foreach (string type in types)
            {
                string strAuthor = "";
                float fLevel = 0;

                AuthorLevel author = new AuthorLevel();
                author.Type = type;
                if (type == "GCAT"
                    || type == "�ĽǺ���"
                    || type == "Cutter-Sanborn Three-Figure"
#if SHITOUTANG

 || type == "ʯͷ�����ߺ�"
                    || type == "ʯͷ��"
#endif
)
                {
                    // �������ֺ����ʹ�MARC��¼�л�������ַ���
                    // return:
                    //      -1  error
                    //      0   not found
                    //      1   found
                    nRet = GetAuthor(type,
                        out strAuthor,
                        out fLevel,
                        out strError);

#if DEBUG
                    if (nRet == 0)
                    {
                        Debug.Assert(String.IsNullOrEmpty(strAuthor) == true, "");
                    }

                    if (nRet == 1)
                    {
                        Debug.Assert(String.IsNullOrEmpty(strAuthor) == false, "");
                    }
#endif

                    if (nRet == -1 || nRet == 0)
                        author.Level = nRet;
                    else
                        author.Level = fLevel;
                    if (nRet == 1)
                        author.Author = strAuthor;
                    else
                        author.Author = strError;
                    authors.Add(author);
                    continue;
                }
                else if (type == "�ֶ�")
                {
                    author.Level = 1;
                    author.Author = "?";
                    authors.Add(author);
                    continue;
                }
                else if (type == "<��>")
                {
                    author.Level = 1;
                    author.Author = "";
                    authors.Add(author);
                    continue;
                }
                else
                {
                    strError = "δ֪�����ֺ����� '" + type + "'";
                    goto ERROR1;
                }
            }

            // *** �ڶ��׶Σ�ѡ��һ�� level ��ߵ�������Ϣ
            AuthorLevel one = null;
            if (authors.Count == 0)
            {
                strError = "û��ָ���κ����ֺ����ͣ��޷���������ַ���";
                return 0;
            }
            else if (authors.Count == 1)
            {
                one = authors[0];
            }
            if (authors.Count > 1)
            {
                // TODO: ͬ level �����򣬾���ҪһЩ��ʾ��Ϣ�������¼����ĳ�����ݿ⡣UNIMARC ��ʽ�Ŀ⣬���������ĵ��ַ�����΢�����һ��
                authors.Sort(new AuthorLevelComparer());

                one = authors[0];
                if (one.Level <= 0)
                {
                    string strWarning = "";
                    string strErrorText = "";
                    foreach (AuthorLevel author in authors)
                    {
                        if (author.Level == -1)
                        {
                            if (string.IsNullOrEmpty(strErrorText) == false)
                                strErrorText += "; ";
                            strErrorText += author.Author;
                        }
                        if (author.Level == 0)
                        {
                            if (string.IsNullOrEmpty(strWarning) == false)
                                strWarning += "; ";
                            strWarning += author.Author;
                        } 
                    }

                    if (string.IsNullOrEmpty(strErrorText) == false)
                    {
                        strError = strErrorText;
                        return -1;
                    }

                    strError = strWarning;
                    return 0;
                }
            }

            if (one.Level == -1)
            {
                strError = one.Author;
                return -1;
            }
            if (one.Level == 0)
            {
                strError = one.Author;
                return 0;
            }

            // 2014/4/15
            if (one.Type == "<��>"
                || one.Type == "�ֶ�")
            {
                strQufenhao = one.Author;
                return 1;
            }

            // *** �����׶Σ��������ַ����������ߺ�
            {
                string type = one.Type;
                string strAuthor = one.Author;
                Debug.Assert(String.IsNullOrEmpty(strAuthor) == false, "");

                if (type == "GCAT")
                {
                    // ������ߺ�
                    string strGcatWebServiceUrl = this.DetailForm.MainForm.GcatServerUrl;   // "http://dp2003.com/dp2libraryws/gcat.asmx";

                    // ������ߺ�
                    // return:
                    //      -1  error
                    //      0   canceled
                    //      1   succeed
                    nRet = GetGcatAuthorNumber(strGcatWebServiceUrl,
                        strAuthor,
                        out strQufenhao,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // �����ش�����
                    if (nRet == 0)
                    {
                        if (string.IsNullOrEmpty(strError) == true)
                            strError = "������ GCAT ȡ��";
                        return 0;
                    }

                    return 1;
                }
                else if (type == "�ĽǺ���")
                {
                    // ������ߺ�
                    // return:
                    //      -1  error
                    //      0   canceled
                    //      1   succeed
                    nRet = GetSjhmAuthorNumber(
                        strAuthor,
                        out strQufenhao,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // �����ش�����
                    if (nRet == 0)
                    {
                        if (string.IsNullOrEmpty(strError) == true)
                            strError = "�������ĽǺ���ȡ��";
                        return 0;
                    }
                    return 1;
                }
                else if (type == "Cutter-Sanborn Three-Figure")
                {
                    // ������ߺ�
                    // return:
                    //      -1  error
                    //      0   canceled
                    //      1   succeed
                    nRet = GetCutterAuthorNumber(
                        strAuthor,
                        out strQufenhao,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // �����ش�����
                    if (nRet == 0)
                    {
                        if (string.IsNullOrEmpty(strError) == true)
                            strError = "�����ӿ��ر�ȡ��";
                        return 0;
                    }

                    return 1;
                }
#if SHITOUTANG
                else if (type == "ʯͷ�����ߺ�"
                    || type == "ʯͷ��")
                {
                    strQufenhao = strAuthor;
                    return 1;
                }
#endif
                else if (type == "�ֶ�")
                {
                    strQufenhao = "?";
                    return 1;
                }
                else if (type == "<��>")
                {
                    strQufenhao = "";
                    return 1;
                }
                else
                {
                    strError = "δ֪�����ֺ����� '" + type + "'";
                    goto ERROR1;
                }
            }
            return 0;
        ERROR1:
            return -1;
        }

        // �������ֺ����ʹ�MARC��¼�л�������ַ���
        // return:
        //      -1  error
        //      0   not found��ע���ʱҲҪ����strErrorֵ
        //      1   found
        /// <summary>
        /// �������ֺ����ʹ� MARC ��¼�л�������ַ���
        /// </summary>
        /// <param name="strQufenhaoType">���ֺ����͡�ע��������һ�����ֺ�����</param>
        /// <param name="strAuthor">���������ַ���</param>
        /// <param name="fLevel">�������ҵ��������ַ����ļ���1 ��ʾ����, 2��ʾ���ߡ�û���ҵ����ߴ��������������ֵ��Ч</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: û���ҵ�(ע���ʱ strError ��Ҳ����������); 1: �ҵ�</returns>
        public virtual int GetAuthor(string strQufenhaoType,
            out string strAuthor,
            out float fLevel,
            out string strError)
        {
            strError = "";
            strAuthor = "";
            fLevel = 2;

            string strMarcSyntax = this.DetailForm.GetCurrentMarcSyntax();

            if (strQufenhaoType == "GCAT")
            {
                if (strMarcSyntax == "unimarc")
                {
#if NO
                    List<string> locations = new List<string>();
                    locations.Add("701a");
                    locations.Add("711a");
                    locations.Add("702a");
                    locations.Add("712a");
                    strAuthor = GetFirstSubfield(locations);
#endif
                    List<string> results = null;
                    // 700��710��720
                    results = GetSubfields("700", "a", "@[^A].");    // ָʾ��
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }
                    results = GetSubfields("710", "a");
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }
                    results = GetSubfields("720", "a");
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    // 701/711/702/712
                    results = GetSubfields("701", "a", "@[^A].");   // ָʾ��
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    results = GetSubfields("711", "a");
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    results = GetSubfields("702", "a", "@[^A].");   // ָʾ��
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    results = GetSubfields("712", "a");
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    results = GetSubfields("200", "a");
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        fLevel = 1.1F;
                        goto FOUND;
                    }


                    strError = "MARC��¼�� 700/710/720/701/711/702/712�о�δ���ְ������ֵ� $a ���ֶ����ݣ��޷���������ַ���";
                    return 0;
                FOUND:
                    Debug.Assert(results.Count > 0, "");
                    strAuthor = results[0];
                }
                else if (strMarcSyntax == "usmarc")
                {
                    List<string> locations = new List<string>();
                    locations.Add("100a");
                    locations.Add("110a");
                    locations.Add("111a");
                    locations.Add("700a");
                    locations.Add("710a");
                    locations.Add("711a");
                    strAuthor = GetFirstSubfield(locations);
                }
                else
                {
                    strError = "δ֪��MARC��ʽ '" + strMarcSyntax + "'";
                    return -1;
                }

                if (String.IsNullOrEmpty(strAuthor) == false)
                    return 1;   // found

                // TODO: �ҵ��к��ֵ�
                // ����� 245$a level 1.0F

                strError = "MARC��¼�� 701/711/702/712�о�δ����&a �޷���������ַ���";
                fLevel = 0;
                return 0;   // not found
            }
            else if (strQufenhaoType == "�ĽǺ���")
            {
                if (strMarcSyntax == "unimarc")
                {
                    /*
                    List<string> locations = new List<string>();
                    locations.Add("701a");
                    locations.Add("711a");
                    locations.Add("702a");
                    locations.Add("712a");
                    strAuthor = GetFirstSubfield(locations);
                     * */
                    List<string> results = null;
                    // 700��710��720
                    results = GetSubfields("700", "a", "@[^A].");    // ָʾ��
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }
                    results = GetSubfields("710", "a");
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }
                    results = GetSubfields("720", "a");
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    // 701/711/702/712
                    results = GetSubfields("701", "a", "@[^A].");   // ָʾ��
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    results = GetSubfields("711", "a");
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    results = GetSubfields("702", "a", "@[^A].");   // ָʾ��
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    results = GetSubfields("712", "a");
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    results = GetSubfields("200", "a");
                    results = ContainHanzi(results);
                    if (results.Count > 0)
                    {
                        fLevel = 1.1F;
                        goto FOUND;
                    }

                    strError = "MARC��¼�� 700/710/720/701/711/702/712�о�δ���ְ������ֵ� $a ���ֶ����ݣ��޷���������ַ���";
                    fLevel = 0;
                    return 0;
                FOUND:
                    Debug.Assert(results.Count > 0, "");
                    strAuthor = results[0];
                }
                else if (strMarcSyntax == "usmarc")
                {
                    List<string> locations = new List<string>();
                    locations.Add("100a");
                    locations.Add("110a");
                    locations.Add("111a");
                    locations.Add("700a");
                    locations.Add("710a");
                    locations.Add("711a");
                    strAuthor = GetFirstSubfield(locations);
                }
                else
                {
                    strError = "δ֪��MARC��ʽ '" + strMarcSyntax + "'";
                    return -1;
                }

                if (String.IsNullOrEmpty(strAuthor) == false)
                    return 1;   // found

                strError = "MARC��¼�� 701/711/702/712�о�δ����&a �޷���������ַ���";
                fLevel = 0;
                return 0;   // not found
            }
            else if (strQufenhaoType == "Cutter-Sanborn Three-Figure")
            {
                if (strMarcSyntax == "unimarc")
                {
                    List<string> results = null;
                    // 700��710��720
                    results = GetSubfields("700", "a", "@[^A].");    // ָʾ��
                    results = NotContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }
                    results = GetSubfields("710", "a");
                    results = NotContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }
                    results = GetSubfields("720", "a");
                    results = NotContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    // 701/711/702/712
                    results = GetSubfields("701", "a", "@[^A].");   // ָʾ��
                    results = NotContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    results = GetSubfields("711", "a");
                    results = NotContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    results = GetSubfields("702", "a", "@[^A].");   // ָʾ��
                    results = NotContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    results = GetSubfields("712", "a");
                    results = NotContainHanzi(results);
                    if (results.Count > 0)
                    {
                        goto FOUND;
                    }

                    results = GetSubfields("200", "a");
                    results = NotContainHanzi(results);
                    if (results.Count > 0)
                    {
                        fLevel = 1.0F;   // unimarc ��ʽ���ҵ������ַ�����Ӣ�ģ���Ϊ���ر���;��Ҫ��һЩ
                        goto FOUND;
                    }

                    strError = "MARC��¼�� 700/710/720/701/711/702/712�о�δ���ֲ������ֵ� $a ���ֶ����ݣ��޷�������������ַ���";
                    fLevel = 0;
                    return 0;
                FOUND:
                    Debug.Assert(results.Count > 0, "");
                    strAuthor = results[0];
                }
                else if (strMarcSyntax == "usmarc")
                {
                    List<string> locations = new List<string>();
                    locations.Add("100a");
                    locations.Add("110a");
                    locations.Add("111a");
                    locations.Add("700a");
                    locations.Add("710a");
                    locations.Add("711a");
                    strAuthor = GetFirstSubfield(locations);
                }
                else
                {
                    strError = "δ֪��MARC��ʽ '" + strMarcSyntax + "'";
                    return -1;
                }

                if (String.IsNullOrEmpty(strAuthor) == false)
                    return 1;   // found

                // TODO: 245$a ���ҵ���Ӣ�ĵ������ַ�����ҪǿһЩ��level 1.1

                strError = "MARC��¼���޷���������ַ���";
                fLevel = 0;
                return 0;   // not found
            }
#if SHITOUTANG
            else if (strQufenhaoType == "ʯͷ�����ߺ�"
                || strQufenhaoType == "ʯͷ��")
            {
                MarcRecord record = new MarcRecord(this.DetailForm.MarcEditor.Marc);

                if (strMarcSyntax == "unimarc")
                {
                    MarcNodeList fields = record.select("field[@name='700' or @name='701' or @name='702' or @name='710' or @name='711' or @name='712']");

                    // ��ѡ�����ֶ��л��ʯͷ�������ַ���
                    // return:
                    //      -1  ����
                    //      0   û���ҵ�
                    //      1   �ҵ�
                    int nRet = GetShitoutangAuthorString(fields,
                        out strAuthor,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 1)
                    {
                        fLevel = 2;
                        return 1;
                    }
#if NO
                    if (nRet == 0)
                    {
                        strError = "MARC��¼�� 700/710/720/701/711/702/712�о�δ���������ַ���";
                        nLevel = 0;
                        return 0;

                    }
#endif

                    fields = record.select("field[@name='200']");

                    // ��ѡ�����ֶ��л��ʯͷ�������ַ���
                    // return:
                    //      -1  ����
                    //      0   û���ҵ�
                    //      1   �ҵ�
                    nRet = GetShitoutangAuthorString(fields,
                        out strAuthor,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                    {
                        strError = "UNIMARC��¼ 700/710/720/701/711/702/712 �ֶξ�δ���������ַ���, 200�ֶ���Ҳδ��������";
                        fLevel = 0;
                        return 0;
                    }
                    fLevel = 1;
                    return 1;
                }
                else if (strMarcSyntax == "usmarc")
                {
                    MarcNodeList fields = record.select("field[@name='100' or @name='110' or @name='111' or @name='700' or @name='710' or @name='711']");

                    // ��ѡ�����ֶ��л��ʯͷ�������ַ���
                    // return:
                    //      -1  ����
                    //      0   û���ҵ�
                    //      1   �ҵ�
                    int nRet = GetShitoutangAuthorString(fields,
                        out strAuthor,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 1)
                    {
                        fLevel = 2;
                        return 1;
                    }
#if NO
                    if (nRet == 0)
                    {
                        strError = "MARC��¼�� 100/110/111/700/710/711 �о�δ���������ַ���";
                        nLevel = 0;
                        return 0;
                    }
#endif


                    fields = record.select("field[@name='245']");

                    // ��ѡ�����ֶ��л��ʯͷ�������ַ���
                    // return:
                    //      -1  ����
                    //      0   û���ҵ�
                    //      1   �ҵ�
                    nRet = GetShitoutangAuthorString(fields,
                        out strAuthor,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                    {
                        strError = "USMARC��¼ 700/710/720/701/711/702/712 �ֶξ�δ���������ַ���, 245�ֶ���Ҳδ��������";
                        fLevel = 0;
                        return 0;
                    }
                    fLevel = 1;
                    return 1;
                }
                else
                {
                    strError = "δ֪��MARC��ʽ '" + strMarcSyntax + "'";
                    return -1;
                }
                fLevel = 0;
                return 0;
            }
#endif

            strError = "GetAuthor()ʱδ֪�����ֺ����� '" + strQufenhaoType + "'";
            fLevel = -1;
            return -1;
        }

        #if SHITOUTANG

        #region ʯͷ�����ߺ�

        static string FirstContent(MarcNodeList nodes)
        {
            if (nodes.count == 0)
                return "";
            return nodes[0].Content;
        }

        // �ѵ���������ߵ�����
        static string Reverse(string strText)
        {
            int nRet = strText.IndexOf(",");
            if (nRet == -1)
                return strText;
            string strLeft = strText.Substring(0, nRet).Trim();
            string strRight = strText.Substring(nRet + 1).Trim();
            return strRight + ", " + strLeft;
        }

        // ��ÿ�ͷ���ϸɸ�ƴ����ͷ
        // ���ܻ��׳� ArgumentException �쳣
        static string GetPinyinHead(string strText, int nCount)
        {
            string strResult = "";

            if (string.IsNullOrEmpty(strText) == true)
                return "";

            strText = strText.Trim();
            if (string.IsNullOrEmpty(strText) == true)
                return "";

            if (strText[0] == '(' || strText[0] == '��')
                throw new ArgumentException("ƴ���ַ��� '"+strText+"' �о������ţ������Ϲ淶Ҫ��", "strText");

            string[] parts = strText.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in parts)
            {
                if (string.IsNullOrEmpty(s) == true)
                    continue;
                char ch = s[0];
                if (char.IsLetter(ch) == false)
                    continue;

                strResult += ch;
                if (strResult.Length >= nCount)
                    return strResult.ToUpper();
            }

            return strResult.ToUpper();
        }

        // ��õ�һ���ڶ���ƴ����ͷ
        // ����ж��ţ��ڶ���ƴ����ͷ��ָ���ź���ĵ�һ��
        static string GetShitoutangHead(string strText)
        {
            int nRet = strText.IndexOf(",");
            if (nRet != -1)
            {
                // ����ж��ţ���ȡ���εĵ�һ��ƴ����ͷ
                string strLeft = strText.Substring(0, nRet).Trim();
                string strRight = strText.Substring(nRet + 1).Trim();
                return GetPinyinHead(strLeft, 1) + GetPinyinHead(strRight, 1);
            }

            return GetPinyinHead(strText, 2);
        }

        // Ϊ��ȡ��ʯͷ�����ߺţ��Ѻ���תΪƴ��
        // ע: ƴ���Ĵ�Сдû�й���
        int HanziToPinyin(ref string strText,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            string strPinyin = "";

#if NO
            // ���ַ����еĺ��ֺ�ƴ������
            // return:
            //      -1  ����
            //      0   �û�ϣ���ж�
            //      1   ����
            if (string.IsNullOrEmpty(this.DetailForm.MainForm.PinyinServerUrl) == true
               || this.DetailForm.MainForm.ForceUseLocalPinyinFunc == true)
            {
                nRet = this.DetailForm.MainForm.HanziTextToPinyin(
                    this.DetailForm,
                    true,	// ���أ�����
                    strText,
                    PinyinStyle.None,
                    out strPinyin,
                    out strError);
            }
            else
            {
                // �����ַ���ת��Ϊƴ��
                // ����������Ѿ�MessageBox������strError��һ�ַ���Ϊ�ո�
                // return:
                //      -1  ����
                //      0   �û�ϣ���ж�
                //      1   ����
                //      2   ����ַ�������û���ҵ�ƴ���ĺ���
                nRet = this.DetailForm.MainForm.SmartHanziTextToPinyin(
                    this.DetailForm,
                    strText,
                    PinyinStyle.None,
                    false,  // auto sel
                    out strPinyin,
                    out strError);
            }
#endif
            nRet = this.DetailForm.MainForm.GetPinyin(
                this.DetailForm,
                strText,
                PinyinStyle.None,
                false,
                out strPinyin,
                out strError);

            if (nRet == -1)
                return -1;
            if (nRet == 0)
            {
                strError = "�û��жϡ�ƴ�����ֶ����ݿ��ܲ�������";
                return -1;
            }

            strText = strPinyin;
            return 0;
        }

        // �����ֻ���Ӣ�ĵ������ַ�����ȡ��ʯͷ�����ߺŵ�������ĸ
        int AutoGetShitoutangHead(ref string strText,
            out string strError)
        {
            strError = "";

            if (ContainHanzi(strText) == false)
            {
                strText = GetShitoutangHead(strText).ToUpper();
                return 0;
            }
            // ȡƴ��
            int nRet = HanziToPinyin(ref strText,
            out strError);
            if (nRet == -1)
                return -1;

            strText = GetShitoutangHead(strText).ToUpper();
            return 0;
        }

        // �������ַ������滯
        // �滻����ȫ�����ţ�����Ϊ�����̬
        // ���ܻ��׳� ArgumentException �쳣
        static string CanonicalizeAuthorString(string strText)
        {
            if (string.IsNullOrEmpty(strText) == true)
                return "";

            strText = strText.Trim();
            if (string.IsNullOrEmpty(strText) == true)
                return "";

            if (strText[0] == '(' || strText[0] == '��')
                throw new ArgumentException("�����ַ��� '" + strText + "' ���׸��ַ�Ϊ���ţ������Ϲ淶Ҫ��", "strText");


            return strText.Replace("��", "(").Replace("��", ")").Replace("��", ",");
        }

        // ȥ����Χ������
        // (Hatzel, Richardson)
        static string Unquote(string strValue)
        {
            if (string.IsNullOrEmpty(strValue) == true)
                return "";
            if (strValue[0] == '(')
                strValue = strValue.Substring(1);
            if (string.IsNullOrEmpty(strValue) == true)
                return "";
            if (strValue[strValue.Length - 1] == ')')
                return strValue.Substring(0, strValue.Length - 1);

            return strValue;
        }

        // ��ѡ�����ֶ��л��ʯͷ�������ַ���
        // TODO: �����ַ������� $b
        // return:
        //      -1  ����
        //      0   û���ҵ�
        //      1   �ҵ�
        int GetShitoutangAuthorString(MarcNodeList fields,
            out string strAuthor,
            out string strError)
        {
            strAuthor = "";
            strError = "";
            int nRet = 0;

            foreach (MarcNode field in fields)
            {
#if NO
                // ��ȡ�� $g
                string strText = FirstContent(field.select("subfield[@name='g']"));
                if (string.IsNullOrEmpty(strText) == false)
                {
                    strText = Unquote(strText);
                    strText = CanonicalizeAuthorString(strText);

                    // �۲�ָʾ��
                    if (field.Indicator2 == '1')
                        strAuthor = Reverse(strText);
                    else
                        strAuthor = strText;

                    // �����ֻ���Ӣ�ĵ������ַ�����ȡ��ʯͷ�����ߺŵ�������ĸ
                    nRet = AutoGetShitoutangHead(ref strAuthor,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    return 1;
                }
#endif

                // $a $b ͬʱ�߱������
                string a = FirstContent(field.select("subfield[@name='a']"));
                string b = FirstContent(field.select("subfield[@name='b']"));
                if (string.IsNullOrEmpty(a) == false && string.IsNullOrEmpty(b) == false)
                {
                    try
                    {
                        a = CanonicalizeAuthorString(a);
                        b = CanonicalizeAuthorString(b);
                    }
                    catch (ArgumentException ex)
                    {
                        strError = ex.Message;
                        return -1;
                    }

                    // �����Ƿ��� &9
                    string sub_9 = FirstContent(field.select("subfield[@name='9']"));
                    if (string.IsNullOrEmpty(sub_9) == false)
                    {
                        // ֻ�õ� $b ��ƴ������
                        nRet = HanziToPinyin(ref b,
                            out strError);
                        if (nRet == -1)
                            return -1;

                        strAuthor = sub_9 + ", " + b;

                        try
                        {
                            strAuthor = GetShitoutangHead(strAuthor);
                            return 1;
                        }
                        catch (ArgumentException)
                        {
                            // ��������û��ƴ����������ʱ����ƴ��
                        }
                    }
                    else
                    {
                        // û�� $9
                        if (ContainHanzi(a) == true)
                        {
                            strError = "�ֶ� "+field.Name+" ������ $a ��û�� $9�����ȴ��� $9 ���ֶ�";
                            return -1;
                        }
                    }

                    strAuthor = a + ", " + b;

                    // �����ֻ���Ӣ�ĵ������ַ�����ȡ��ʯͷ�����ߺŵ�������ĸ
                    nRet = AutoGetShitoutangHead(ref strAuthor,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    return 1;
                }

                // ֻ�� $a
                if (string.IsNullOrEmpty(a) == false)
                {
                    try
                    {
                        a = CanonicalizeAuthorString(a);
                    }
                    catch (ArgumentException ex)
                    {
                        strError = ex.Message;
                        return -1;
                    }

                    // �����Ƿ��� &9
                    string sub_9 = FirstContent(field.select("subfield[@name='9']"));
                    if (string.IsNullOrEmpty(sub_9) == false)
                    {
                        try
                        {
                            strAuthor = GetShitoutangHead(sub_9);
                            return 1;
                        }
                        catch (ArgumentException)
                        {
                            // ��������û��ƴ����������ʱ����ƴ��
                        }
                    }
                    else
                    {
                        // û�� $9
                        if (ContainHanzi(a) == true)
                        {
                            strError = "�ֶ� " + field.Name + " ������ $a ��û�� $9�����ȴ��� $9 ���ֶ�";
                            return -1;
                        }
                    }

                    strAuthor = a;

                    // �����ֻ���Ӣ�ĵ������ַ�����ȡ��ʯͷ�����ߺŵ�������ĸ
                    nRet = AutoGetShitoutangHead(ref strAuthor,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    return 1;
                }
            }

            return 0;   // û���ҵ�
        }

#endregion

#endif

        // �����ȡ�ŵĵ�һ��
        // parameters:
        //      strHeadLine ���ص�һ�е����ݡ�����null��ʾ��Ҫ��һ�С�ע�������в�Ҫ����{}ָ��������ĵ����߻��Զ�������
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        /// <summary>
        /// �����ȡ�ŵĵ�һ��
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        /// <param name="index">�±�</param>
        /// <param name="info">�ż���ϵ��Ϣ</param>
        /// <param name="strHeadLine">���ص�һ�С����� null ��ʾ��Ҫ��һ�С�ע�������в�Ҫ����{}ָ��������ĵ����߻��Զ�������</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public virtual int GetCallNumberHeadLine(
            object sender,
            GenerateDataEventArgs e,
            int index,
            ArrangementInfo info,
            out string strHeadLine,
            out string strError)
        {
            strError = "";
            /*
            strHeadLine = null;   // ȱʡ��Ч��Ϊ����Ҫ��һ��

            if (info != null)
            {
                if (string.IsNullOrEmpty(info.CallNumberStyle) == true
                    || info.CallNumberStyle == "��ȡ���+���ֺ�"
                    || info.CallNumberStyle == "����"
                    || info.CallNumberStyle == "����")
                {
                    strHeadLine = null;
                    return 1;
                }
                else if (info.CallNumberStyle == "�ݲش���+��ȡ���+���ֺ�"
                    || info.CallNumberStyle == "����")
                {
                    strHeadLine = "{ns}�ݲش���";
                }
            }
             * */
            strHeadLine = "�ݲش���";

            return 1;
        }

#if NO
        // �����ȡ�ŵĵ�һ��
        // parameters:
        //      strHeadLine ���ص�һ�е����ݡ�����null��ʾ��Ҫ��һ��
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        public virtual int GetCallNumberHeadLine(
            object sender,
            GenerateDataEventArgs e,
            int index,
            out string strHeadLine,
            out string strError)
        {
            strError = "";
            strHeadLine = null;   // ȱʡ��Ч��Ϊ����Ҫ��һ��

#if NOOOOOOOOOO
            int nRet = 0;
            string strLocation = "";
            string strClass = "";
            string strItemRecPath = "";

            EntityEditForm edit = null;
            EntityControl control = null;
            BookItem book_item = null;

            List<CallNumberItem> callnumber_items = null;

            string strBookType = "";

            if (sender is EntityEditForm)
            {
                edit = (EntityEditForm)sender;

                // ȡ�ùݲصص�
                strLocation = edit.entityEditControl_editing.LocationString;
                strLocation = Global.GetPureLocation(strLocation);  // 2009/3/29 new add

                /*
                if (String.IsNullOrEmpty(strLocation) == true)
                {
                    strError = "��������ݲصص㡣�����޷�������ȡ��";
                    goto ERROR1;
                }*/

                strHeadLine = GetHeadLinePart(edit.entityEditControl_editing.AccessNo);

                // ������е����
                strClass = GetClassPart(edit.entityEditControl_editing.AccessNo);

                strBookType = edit.entityEditControl_editing.BookType;

                strItemRecPath = edit.entityEditControl_editing.RecPath;

                callnumber_items = edit.BookItems.GetCallNumberItems();

                edit.Enabled = false;
                edit.Update();
            }
            else if (sender is EntityControl)
            {
                control = (EntityControl)sender;

                if (control.ListView.SelectedIndices.Count == 0)
                {
                    strError = "����ѡ��Ҫ��ע���С������޷�������ȡ��";
                    return -1;
                }

                Debug.Assert(index >= 0 && index < control.ListView.SelectedIndices.Count, "");

                book_item = control.GetVisibleBookItemAt(control.ListView.SelectedIndices[index]);
                Debug.Assert(book_item != null, "");

                strLocation = book_item.Location;
                strLocation = Global.GetPureLocation(strLocation);  // 2009/3/29 new add

                /*
                if (String.IsNullOrEmpty(strLocation) == true)
                {
                    strError = "��������ݲصص㡣�����޷�������ȡ��";
                    goto ERROR1;
                }*/

                strHeadLine = GetHeadLinePart(book_item.AccessNo);

                // ������е����
                strClass = GetClassPart(book_item.AccessNo);

                strBookType = book_item.BookType;

                strItemRecPath = book_item.RecPath;

                callnumber_items = control.BookItems.GetCallNumberItems();

                control.Enabled = false;
                control.Update();
            }
            else
            {
                strError = "sender������EntityEditForm��EntityControl����(��ǰΪ" + sender.GetType().ToString() + ")";
                return -1;
            }

            try
            {
                string strArrangeGroupName = "";
                string strZhongcihaoDbname = "";
                string strClassType = "";
                string strQufenhaoType = "";

                // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
                nRet = this.DetailForm.MainForm.GetCallNumberInfo(strLocation,
                    out strArrangeGroupName,
                    out strZhongcihaoDbname,
                    out strClassType,
                    out strQufenhaoType,
                    out strError);
                if (nRet == 0)
                {
                    strError = "û�й��ڹݲصص� '" + strLocation + "' ���ż���ϵ������Ϣ���޷������ȡ��";
                    return -1;
                }
                if (nRet == -1)
                    return -1;


                // ���������
                if (strBookType == "������")
                {
                    if (strClassType == "��ͼ��")
                    {
                        strHeadLine = "(C)";
                        return 1;
                    }
                    else if (strClassType == "����")
                    {
                        strHeadLine = "C";
                        return 1;
                    }
                    else
                    {
                        strError = "����������ʱ���޷�����ķ��෨ '" + strClassType + "'";
                        return -1;
                    }
                }

                // ���ǹ���������

                // ��MARCEDIT��ȡ��101$a
                string strLangCode = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield(
                    "101",
                    "a");
                if (String.IsNullOrEmpty(strLangCode) == true)
                {
                    strError = "MARC��¼�� 101$a û���ҵ����޷����������Ա�����ȡ�ŵ�һ��";
                    return -1;
                }

                /*
��һ��	���ִ���	ע��
(1)	rus	����
(2)	eng	Ӣ��
(3)	ger	����
(4)	fre	����
(40892)		epo	������
chi	����	��������ģ���Ϊ�ա�
                 * 
������Ҫ�������ִ����Сд�����е����⡣
                 * */
                strLangCode = strLangCode.ToLower();
                if (strLangCode == "rus")
                {
                    strHeadLine = "(1)";
                    return 1;
                }
                if (strLangCode == "eng")
                {
                    strHeadLine = "(2)";
                    return 1;
                }
                if (strLangCode == "ger")
                {
                    strHeadLine = "(3)";
                    return 1;
                }
                if (strLangCode == "fre")
                {
                    strHeadLine = "(4)";
                    return 1;
                }
                if (strLangCode == "epo")
                {
                    strHeadLine = "(40892)";
                    return 1;
                } if (strLangCode == "chi")
                {
                    strHeadLine = "";
                    return 1;
                }

                strError = "��101$a���Դ���Ϊ'" + strLangCode + "'���޷�������ȡ�ŵ�һ��";
                return -1;
            }
            finally
            {
                if (sender is EntityEditForm)
                {
                    edit.Enabled = true;
                }
                else if (sender is EntityControl)
                {
                    control.Enabled = true;
                }
                else
                {
                    Debug.Assert(false, "");
                }
            }
#endif
            return 1;
        }
#endif

        // ����һ����ȡ��
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        /// <summary>
        /// ����һ����ȡ��
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        /// <param name="index">�±�</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: ����; 1: �ɹ�</returns>
        public virtual int CreateOneCallNumber(
            object sender,
            GenerateDataEventArgs e,
            int index,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (sender == null)
            {
                strError = "senderΪnull";
                goto ERROR1;
            }

            string strLocation = "";
            string strClass = "";
            string strItemRecPath = "";

            BindingForm binding = null;
            EntityEditForm edit = null;
            EntityControl control = null;
            BookItem book_item = null;

            List<CallNumberItem> callnumber_items = null;

            ArrangementInfo info = null;

            if (sender is EntityEditForm)
            {
                edit = (EntityEditForm)sender;

                // ȡ�ùݲصص�
                strLocation = edit.entityEditControl_editing.LocationString;
                strLocation = StringUtil.GetPureLocationString(strLocation);  // 2009/3/29 new add

                /*
                if (String.IsNullOrEmpty(strLocation) == true)
                {
                    strError = "��������ݲصص㡣�����޷�������ȡ��";
                    goto ERROR1;
                }*/

                // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
                nRet = this.DetailForm.MainForm.GetArrangementInfo(strLocation,
                    out info,
                    out strError);
                if (nRet == 0)
                {
                    strError = "û�й��ڹݲصص� '" + strLocation + "' ���ż���ϵ������Ϣ���޷������ȡ��";
                    goto ERROR1;
                }
                if (nRet == -1)
                    goto ERROR1;

                // ������е����
                strClass = GetClassPart(
                    info.CallNumberStyle,
                    edit.entityEditControl_editing.AccessNo);

                strItemRecPath = edit.entityEditControl_editing.RecPath;

                // callnumber_items = edit.BookItems.GetCallNumberItems();
                callnumber_items = edit.GetCallNumberItems();

                edit.Enabled = false;
                edit.Update();
            }
            else if (sender is EntityControl)
            {
                control = (EntityControl)sender;

                if (control.ListView.SelectedIndices.Count == 0)
                {
                    strError = "����ѡ��Ҫ��ע���С������޷�������ȡ��";
                    goto ERROR1;
                }

                Debug.Assert(index >= 0 && index < control.ListView.SelectedIndices.Count, "");

                book_item = control.GetVisibleItemAt(control.ListView.SelectedIndices[index]);
                Debug.Assert(book_item != null, "");

                strLocation = book_item.Location;
                strLocation = StringUtil.GetPureLocationString(strLocation);  // 2009/3/29 new add

                /*
                if (String.IsNullOrEmpty(strLocation) == true)
                {
                    strError = "��������ݲصص㡣�����޷�������ȡ��";
                    goto ERROR1;
                }*/

                // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
                nRet = this.DetailForm.MainForm.GetArrangementInfo(strLocation,
                    out info,
                    out strError);
                if (nRet == 0)
                {
                    strError = "û�й��ڹݲصص� '" + strLocation + "' ���ż���ϵ������Ϣ���޷������ȡ��";
                    goto ERROR1;
                }
                if (nRet == -1)
                    goto ERROR1;

                // ������е����
                strClass = GetClassPart(
                    info.CallNumberStyle,
                    book_item.AccessNo);

                strItemRecPath = book_item.RecPath;

                callnumber_items = control.Items.GetCallNumberItems();

                control.Enabled = false;
                control.Update();
            }
            else if (sender is BindingForm)
            {
                binding = (BindingForm)sender;

                // ȡ�ùݲصص�
                strLocation = binding.EntityEditControl.LocationString;
                strLocation = StringUtil.GetPureLocationString(strLocation);  // 2009/3/29 new add

                // ��ù���һ���ض��ݲصص����ȡ��������Ϣ
                nRet = this.DetailForm.MainForm.GetArrangementInfo(strLocation,
                    out info,
                    out strError);
                if (nRet == 0)
                {
                    strError = "û�й��ڹݲصص� '" + strLocation + "' ���ż���ϵ������Ϣ���޷������ȡ��";
                    goto ERROR1;
                }
                if (nRet == -1)
                    goto ERROR1;

                // ������е����
                strClass = GetClassPart(
                    info.CallNumberStyle,
                    binding.EntityEditControl.AccessNo);

                strItemRecPath = binding.EntityEditControl.RecPath;

                // callnumber_items = edit.BookItems.GetCallNumberItems();
                callnumber_items = binding.GetCallNumberItems();

                binding.Enabled = false;
                binding.Update();
            }
            else
            {
                strError = "sender������EntityEditForm��EntityControl��BindingForm����(��ǰΪ" + sender.GetType().ToString() + ")";
                goto ERROR1;
            }


            try
            {
                /*
                string strFieldName = "";
                string strSubfieldName = "";

                if (strClassType == "��ͼ��")
                {
                    strFieldName = "690";
                    strSubfieldName = "a";
                }
                else if (strClassType == "��ͼ��")
                {
                    strFieldName = "692";
                    strSubfieldName = "a";
                }
                else if (strClassType == "�˴�")
                {
                    strFieldName = "694";
                    strSubfieldName = "a";
                }
                else
                {
                    strError = "δ֪�ķ��෨ '" + strClassType + "'";
                    goto ERROR1;
                }

                // ���Ǵ�MARCEDIT��ȡ����ȡ���
                strClass = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield(
                    strFieldName,
                    strSubfieldName);
                if (String.IsNullOrEmpty(strClass) == true)
                {
                    strError = "MARC��¼�� " + strFieldName + "$" + strSubfieldName + " û���ҵ����޷������ȡ���";
                    goto ERROR1;
                }
                 * */

                string strHeadLine = null;

                if (info.CallNumberStyle == "�ݲش���+��ȡ���+���ֺ�"
                    || info.CallNumberStyle == "����")
                {
                    // �����ȡ�ŵĵ�һ��
                    // return:
                    //      -1  error
                    //      0   canceled
                    //      1   succeed
                    nRet = GetCallNumberHeadLine(
                        sender,
                        e,
                        index,
                        info,
                        out strHeadLine,
                        out strError);
                    if (nRet == -1 || nRet == 0)
                        return nRet;

                    if (strHeadLine != null)
                        strHeadLine = "{ns}" + strHeadLine;
                }

                // ���Ǵ�MARCEDIT��ȡ����ȡ���

                // (��MARC�༭����)�����ȡ����Ų���
                // return:
                //      -1  error
                //      0   MARC��¼��û���ҵ��������Դ�ֶ�/���ֶ�����
                //      1   succeed
                nRet = GetCallNumberClassPart(
                    info.ClassType,
                    out strClass,
                    out strError);
                if (nRet == -1)
                {
                    strError = "�����ȡ���ʱ����: " + strError;
                    goto ERROR1;
                }
                if (nRet == 0)
                {
                    goto ERROR1;
                }

                Debug.Assert(nRet == 1, "");

                // �������Ѿ���õ���ȡ��Ų���
                if (sender is EntityEditForm)
                {
                    // edit.entityEditControl_editing.AccessNo = strClass;
                    edit.entityEditControl_editing.AccessNo =
                        (strHeadLine != null ? strHeadLine + "/" : "")
                        + strClass;
                }
                else if (sender is EntityControl)
                {
                    // book_item.AccessNo = strClass;

                    book_item.AccessNo =
    (strHeadLine != null ? strHeadLine + "/" : "")
    + strClass;


                    book_item.RefreshListView();

                    // 2011/11/10
                    EntityControl entity_control = (EntityControl)sender;
                    entity_control.Changed = true;
                }
                else if (sender is BindingForm)
                {
                    binding.EntityEditControl.AccessNo =
                        (strHeadLine != null ? strHeadLine + "/" : "")
                        + strClass;
                }
                else
                {
                    Debug.Assert(false, "");
                }

                string strQufenhao = "";

                if (info.QufenhaoType == "zhongcihao"
                    || info.QufenhaoType == "�ִκ�")
                {
                    // ����ִκ�
                    CallNumberForm dlg = new CallNumberForm();

                    try
                    {
                        dlg.MainForm = this.DetailForm.MainForm;
                        // dlg.TopMost = true;
                        if (sender is Form)
                            dlg.Owner = (Form)sender;
                        dlg.MyselfItemRecPath = strItemRecPath;
                        dlg.MyselfParentRecPath = this.DetailForm.BiblioRecPath;
                        dlg.MyselfCallNumberItems = callnumber_items;   // 2009/6/4 new add

                        dlg.Show();

                        ZhongcihaoStyle style = ZhongcihaoStyle.Seed;

                        if (String.IsNullOrEmpty(info.ZhongcihaoDbname) == true)
                            style = ZhongcihaoStyle.Biblio; // û�������ִκſ⣬ֻ�ô���Ŀ���ݿ���ͳ�ƻ������
                        else
                        {
                            style = ZhongcihaoStyle.BiblioAndSeed;   // ����β�ſ⣬������Ŀ+β��

                            // style = ZhongcihaoStyle.Seed;   // ����β�ſ⣬����β��
                        }

                        // TODO: ���ﾡ����������ͳ�Ƴ��������ţ�������β��
                        // ��Ҫע�⣬���ͬһ��Ŀ��¼���Ѿ�����һ���ִκţ���ֱ������
                        // ���Ҫ��Seed��񣬿���ʵ����autogen�ű��У�����������ʵ��

                        // return:
                        //      -1  error
                        //      0   canceled
                        //      1   succeed
                        nRet = dlg.GetZhongcihao(
                            style,
                            strClass,
                            strLocation,
                            out strQufenhao,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet == 0)
                            return 0;
                    }
                    catch (Exception ex)
                    {
                        strError = ex.Message;
                        goto ERROR1;
                    }
                    finally
                    {
                        dlg.Close();
                    }
                }
                else
                {
                    // ����ִκ�������������ֺ�
                    // return:
                    //      -1  error
                    //      0   not found��ע���ʱҲҪ����strErrorֵ
                    //      1   found
                    nRet = GetAuthorNumber(info.QufenhaoType,
                        out strQufenhao,
                        out strError);
                    if (nRet == -1 || nRet == 0)
                        goto ERROR1;
                }

                // ���������������ȡ���
                if (sender is EntityEditForm)
                {
                    edit.entityEditControl_editing.AccessNo =
                        (strHeadLine != null ? strHeadLine + "/" : "")
                        + strClass +
                        (string.IsNullOrEmpty(strQufenhao) == false ? 
                        "/" + strQufenhao : "");
                }
                else if (sender is EntityControl)
                {
                    book_item.AccessNo = 
                        (strHeadLine != null ? strHeadLine + "/" : "")
                        + strClass +
                        (string.IsNullOrEmpty(strQufenhao) == false ?
                        "/" + strQufenhao : "");
                    book_item.RefreshListView();
                    // 2011/11/10
                    EntityControl entity_control = (EntityControl)sender;
                    entity_control.Changed = true;
                }
                else if (sender is BindingForm)
                {
                    binding.EntityEditControl.AccessNo =
                        (strHeadLine != null ? strHeadLine + "/" : "")
                        + strClass +
                        (string.IsNullOrEmpty(strQufenhao) == false ?
                        "/" + strQufenhao : "");
                }
                else
                {
                    Debug.Assert(false, "");
                }
            }
            finally
            {
                if (sender is EntityEditForm)
                {
                    edit.Enabled = true;
                }
                else if (sender is EntityControl)
                {
                    control.Enabled = true;
                }
                else if (sender is BindingForm)
                {
                    binding.Enabled = true;
                }
                else
                {
                    Debug.Assert(false, "");
                }
            }

            return 1;
        ERROR1:
            return -1;
        }



        // ������ȡ��
        /// <summary>
        /// ������ȡ��
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void CreateCallNumber(object sender,
            GenerateDataEventArgs e)
        {
            string strError = "";
            int nRet = 0;

            if (sender == null)
            {
                strError = "senderΪnull";
                goto ERROR1;
            }
            if (sender is EntityEditForm)
            {
                nRet = CreateOneCallNumber(sender,
                    e,
                    0,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            else if (sender is EntityControl)
            {
                EntityControl control = (EntityControl)sender;
                if (control.ListView.SelectedIndices.Count == 0)
                {
                    strError = "����ѡ��Ҫ��ע���С������޷�������ȡ��";
                    goto ERROR1;
                }

                for (int i = 0; i < control.ListView.SelectedIndices.Count; i++)
                {
                    nRet = CreateOneCallNumber(sender,
                        e,
                        i,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    // �����ش����⡣�Ƿ���Ϊ�жϲ�������˼?
                    if (nRet == 0)
                    {
                        strError = "��;����";
                        goto ERROR1;
                    }
                }
            }
            else if (sender is BindingForm)
            {
                nRet = CreateOneCallNumber(sender,
                    e,
                    0,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            else
            {
                strError = "sender������EntityEditForm��EntityControl��BindingForm����(��ǰΪ" + sender.GetType().ToString() + ")";
                goto ERROR1;
            }
            return;
        ERROR1:
            e.ErrorInfo = strError;
            if (e.ShowErrorBox == true)
                MessageBox.Show(this.DetailForm, strError);
        }

        // GCATͨ����¼
        internal void gcat_channel_BeforeLogin(object sender,
            DigitalPlatform.GcatClient.BeforeLoginEventArgs e)
        {
            string strUserName = (string)this.DetailForm.MainForm.ParamTable["author_number_account_username"];
            string strPassword = (string)this.DetailForm.MainForm.ParamTable["author_number_account_password"];

            if (String.IsNullOrEmpty(strUserName) == true)
            {
                strUserName = "test";
                strPassword = "";
            }

            // ֱ����̽
            if (!(e.UserName == strUserName && e.Failed == true)
                && strUserName != "")
            {
                e.UserName = strUserName;
                e.Password = strPassword;
                return;
            }

            LoginDlg dlg = new LoginDlg();
            GuiUtil.SetControlFont(dlg, this.DetailForm.MainForm.DefaultFont, false);

            if (e.Failed == true)
                dlg.textBox_comment.Text = "��¼ʧ�ܡ������ߺ��빦����Ҫ���µ�¼";
            else
                dlg.textBox_comment.Text = "�����ߺ��빦����Ҫ��¼";

            dlg.textBox_serverAddr.Text = e.GcatServerUrl;
            dlg.textBox_userName.Text = strUserName;
            dlg.textBox_password.Text = strPassword;
            dlg.checkBox_savePassword.Checked = true;

            dlg.textBox_serverAddr.Enabled = false;
            dlg.TopMost = true; // 2009/11/12 new add ��ΪShowDialog(null)��Ϊ�˷�ֹ�Ի��򱻷��ڷǶ���
            dlg.ShowDialog(null);
            if (dlg.DialogResult != DialogResult.OK)
            {
                e.Cancel = true;    // 2009/11/12 new add ���ȱ��һ�䣬�����Cancel����Ȼ���µ�����¼�Ի���
                return;
            }

            strUserName = dlg.textBox_userName.Text;
            strPassword = dlg.textBox_password.Text;

            e.UserName = strUserName;
            e.Password = strPassword;

            this.DetailForm.MainForm.ParamTable["author_number_account_username"] = strUserName;
            this.DetailForm.MainForm.ParamTable["author_number_account_password"] = strPassword;
        }

        /// <summary>
        /// ͨ����Դ ID �ҵ���Ӧ�� 856 �ֶ�
        /// </summary>
        /// <param name="editor">MARC �༭��</param>
        /// <param name="strID">��Դ ID</param>
        /// <returns>�ֶζ��󼯺�</returns>
        public static List<Field> Find856ByResID(MarcEditor editor,
            string strID)
        {
            List<Field> results = new List<Field>();

            for (int i = 0; i < editor.Record.Fields.Count; i++)
            {
                Field field = editor.Record.Fields[i];

                if (field.Name == "856")
                {
                    // �ҵ�$8
                    for(int j=0;j<field.Subfields.Count;j++)
                    {
                        Subfield subfield = field.Subfields[j];
                        if (subfield.Name == LinkSubfieldName)
                        {
                            string strValue = subfield.Value;
                            if (StringUtil.HasHead(strValue, "uri:") == true)
                                strValue = strValue.Substring("uri:".Length);

                            if (strValue == strID)
                            {
                                results.Add(field);
                            }
                        }
                    }
                }
            }

            return results;
        }

        public static string LinkSubfieldName = "u";

        /// <summary>
        /// ���� 856 �ֶ�
        /// </summary>
        /// <param name="sender">�¼�������</param>
        /// <param name="e">�¼�����</param>
        public virtual void Manage856(object sender,
            GenerateDataEventArgs e)
        {
            string strID = "";
            Field field_856 = null;

            string SUBFLD = new String((char)31, 1);

            // ������ܴӶ���ؼ�����
            // ��Ҫ�ѺͶ���ؼ��е�ǰѡ����id�йص�856�ֶ���Ϣװ�룬���й���
            if (sender is BinaryResControl)
            {
                BinaryResControl control = (BinaryResControl)sender;
                if (control.ListView.SelectedIndices.Count >= 1)
                {
                    // ��õ�ǰѡ���е�id
                    strID = ListViewUtil.GetItemText(control.ListView.SelectedItems[0], 0);
                }
            }

            // ��MARC�༭�����ҵ�����id��856�ֶ�
            if (String.IsNullOrEmpty(strID) == false)
            {
                List<Field> fields = Find856ByResID(DetailForm.MarcEditor,
                    strID);

                if (fields.Count == 1)
                    field_856 = fields[0];
                else if (fields.Count > 1)
                {
                    DialogResult result = MessageBox.Show(this.DetailForm,
                        "��ǰMARC�༭�����Ѿ����� " + fields.Count.ToString() + " ��856�ֶ���$" + LinkSubfieldName + "���ֶι����˶���ID '" + strID + "' ���Ƿ�Ҫ�༭���еĵ�һ��856�ֶ�?\r\n\r\n(ע���ɸ���MARC�༭����ѡ��һ�������856�ֶν��б༭)\r\n\r\n(OK: �༭���еĵ�һ��856�ֶ�; Cancel: ȡ������",
                        "DetailHost",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.Cancel)
                        return;
                    field_856 = fields[0];
                }
            }

            // �����������MARC�༭��
            if (field_856 == null 
                && !(sender is BinaryResControl) )
            {
                // ������ǰ��ֶ��ǲ���856
                field_856 = this.DetailForm.MarcEditor.FocusedField;
                if (field_856 != null)
                {
                    if (field_856.Name != "856")
                        field_856 = null;
                }
            }

            if (field_856 != null)
            {
                this.DetailForm.MarcEditor.FocusedField = field_856;
                this.DetailForm.MarcEditor.EnsureVisible();
            }

            Field856Dialog dlg = new Field856Dialog();
            GuiUtil.SetControlFont(dlg, this.DetailForm.MainForm.DefaultFont, false);

            if (field_856 != null)
            {
                dlg.Text = "�޸�856�ֶ�";
                dlg.Value = field_856.IndicatorAndValue;
            }
            else
            {
                dlg.Text = "�����µ�856�ֶ�";
                dlg.Value = "72";   // ȱʡֵ

                if (String.IsNullOrEmpty(strID) == false)
                {
                    dlg.Value += SUBFLD + LinkSubfieldName + strID + SUBFLD + "2dp2res";
                    dlg.AutoFollowIdSet = true; // ������������������ֶ�
                    dlg.MessageText = "�в����ںͶ���ID '" + strID + "' ������856�ֶΣ������봴��...";
                }
            }

            dlg.GetResInfo -= new GetResInfoEventHandler(dlg_GetResInfo);
            dlg.GetResInfo += new GetResInfoEventHandler(dlg_GetResInfo);

        REDO_INPUT:
            this.DetailForm.MainForm.AppInfo.LinkFormState(dlg, "ctrl_a_field856dialog_state");
            dlg.ShowDialog(this.DetailForm);
            this.DetailForm.MainForm.AppInfo.UnlinkFormState(dlg);


            if (dlg.DialogResult != DialogResult.OK)
                return;

            // �´�������£����һ��id�Ƿ��Ѿ����ڣ������ʵ�����
            if (field_856 == null
                && String.IsNullOrEmpty(dlg.Subfield_u) == false)
            {
                List<Field> dup_fields = Find856ByResID(DetailForm.MarcEditor, dlg.Subfield_u);

                if (dup_fields.Count > 0)
                {
                    DialogResult result = MessageBox.Show(this.DetailForm,
                        "��ǰMARC�༭�����Ѿ����� " + dup_fields.Count + " ��856�ֶ���$" + LinkSubfieldName + "���ֶι����˶���ID '" + dlg.Subfield_u + "' ��ȷʵҪ�ٴ��´���һ�������˶���ID����856�ֶ�?\r\n\r\n(ע�������Ҫ�����856�ֶ��ǿ��Թ���ͬһ����ID��)\r\n\r\n(Yes: ��������; No: ���������رնԻ�����������ݶ�ʧ; Cancel: ���´򿪶Ի����Ա��һ���޸�)",
                        "DetailHost",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button3);
                    if (result == DialogResult.No)
                        return;
                    if (result == DialogResult.Cancel)
                        goto REDO_INPUT;
                }
            }

            {
                // �����ǰ��ֶβ���856���򴴽�һ���µ�856�ֶ�
                if (field_856 == null)
                {
                    // this.DetailForm.MarcEditor.Flush();
                    field_856 = this.DetailForm.MarcEditor.Record.Fields.Add("856", "  ", "", true);
                }

                field_856.IndicatorAndValue = dlg.Value;
                this.DetailForm.MarcEditor.EnsureVisible();
            }
        }

        void dlg_GetResInfo(object sender, GetResInfoEventArgs e)
        {
            this.DetailForm.GetResInfo(sender, e);
        }

    }

    /// <summary>
    /// ƴ����������
    /// </summary>
    public class PinyinCfgItem
    {
        /// <summary>
        /// �ֶ���
        /// </summary>
        public string FieldName = "";

        /// <summary>
        /// ָʾ��ƥ�䷽ʽ
        /// </summary>
        public string IndicatorMatchCase = "";

        /// <summary>
        /// ��ʲô���ֶΡ�ÿ���ַ���ʾһ�����ֶ���
        /// </summary>
        public string From = "";

        /// <summary>
        /// ��ʲô���ֶΡ�ÿ���ַ���ʾһ�����ֶ���
        /// ��ʲô���ֶΡ�
        /// </summary>
        public string To = "";

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="nodeItem">һ��Ԫ�ؽڵ�</param>
        public PinyinCfgItem(XmlNode nodeItem)
        {
            this.FieldName = DomUtil.GetAttr(nodeItem, "name");
            this.IndicatorMatchCase = DomUtil.GetAttr(nodeItem, "indicator");
            this.From = DomUtil.GetAttr(nodeItem, "from");
            this.To = DomUtil.GetAttr(nodeItem, "to");
        }
    }
}
