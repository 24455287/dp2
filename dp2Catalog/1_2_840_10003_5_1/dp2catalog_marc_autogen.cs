// dp2catalog MARC ����ͼ�� ��Ŀ�Զ���������C#�ű�����
// ����޸�ʱ�� 2013/9/17

// 1) 2011/8/21 public override void Main(object sender, HostEventArgs e)�޸�ΪGenerateDataEventArgs e
// 2) 2011/8/21 �޸�AddAuthorNumber()���������û����GetGcatAuthorNumber()����
// 3) 2011/8/22 �޸�AddAuthorNumber()������ʹ֮���к��Ե�һָʾ��Ϊ'A'��7XX�ֶ�$a���ֶε��������������������������ַ��������ַ������������
// 4) 2011/8/23 ��������AddAuthorNumber()�޸�ΪAddGcatAuthorNumber()
// 5) 2011/8/24 ����AddSjhmAuthorNumber()����ֻ����������905$e�м������ߺ�
// 6) 2011/8/29 Copy200gfTo7xxa()��Copy690aTo905d()�����޸ģ������˶��ֶβ�����ʱ���жϺ;���
// 7) 2012/1/18 AddZhongcihao()���������÷������������
// 8) 2013/9/17 ��ƴ���ܸ��� MainForm �� AutoSelPinyin �����仯Ч��


using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.Marc;
using DigitalPlatform.IO;
using DigitalPlatform.GcatClient;
using DigitalPlatform.Text;
using DigitalPlatform.Script;

using dp2Catalog;

public class MyHost : MarcDetailHost
{
    // DigitalPlatform.GcatClient.Channel GcatChannel = null;

    // �µļ�ƴ���ֶ����á�$9
    string PinyinCfgXml = "<root>"
    + "<item name='200' from='a' to='9' />"
    + "<item name='701' indicator='@[^A].' from='a' to='9' />"
    + "<item name='711' from='a' to='9' />"
    + "<item name='702' indicator='@[^A].' from='a' to='9' />"
    + "<item name='712' from='a' to='9' />"
    + "</root>";

    // �ϵļ�ƴ�����á�$A��
    string OldPinyinCfgXml = "<root>"
        + "<item name='200' from='aefhi' to='AEFHI' />"
        + "<item name='510' from='aei' to='AEI' />"
        + "<item name='512' from='aei' to='AEI' />"
        + "<item name='513' from='aei' to='AEI' />"
        + "<item name='514' from='aei' to='AEI' />"
        + "<item name='515' from='aei' to='AEI' />"
        + "<item name='516' from='aei' to='AEI' />"
        + "<item name='517' from='aei' to='AEI' />"
        + "<item name='520' from='aei' to='AEI' />"
        + "<item name='530' from='a' to='A' />"
        + "<item name='532' from='a' to='A' />"
        + "<item name='540' from='a' to='A' />"
        + "<item name='541' from='aei' to='AEI' />"
        + "<item name='700' from='a' to='A' />"
        + "<item name='701' indicator='@[^A].' from='a' to='A' />"
        + "<item name='711' from='a' to='A' />"
        + "<item name='702' indicator='@[^A].' from='a' to='A' />"
        + "<item name='712' from='a' to='A' />"
        + "<item name='720' from='a' to='A' />"
        + "<item name='721' from='a' to='A' />"
        + "<item name='722' from='a' to='A' />"
        + "</root>";


    public void CreateMenu(object sender, GenerateDataEventArgs e)
    {
        ScriptActionCollection actions = new ScriptActionCollection();

        if (sender is MarcEditor || sender == null)
        {
            // ��ƴ��
            actions.NewItem("��ƴ��", "��ȫ����������ֶμ�ƴ��", "AddPinyin", false, 'P');

            // ɾ��ƴ��
            actions.NewItem("ɾ��ƴ��", "ɾ��ȫ��ƴ�����ֶ�", "RemovePinyin", false);

            // ���ƴ������
            actions.NewItem("���ƴ������", "����洢����ǰѡ����ĺ��ֺ�ƴ�����չ�ϵ", "ClearPinyinCache", false);

            // �ָ���
            actions.NewSeperator();

            // ����ISBNΪ13
            actions.NewItem("����ΪISBN-13", "��010$a��ISBN���й���", "HyphenISBN_13", false);

            // ����ISBNΪ10
            actions.NewItem("����ΪISBN-10", "��010$a��ISBN���й���", "HyphenISBN_10", false);

            // �ָ���
            actions.NewSeperator();


            // 102���Ҵ��� ��������
            actions.NewItem("102$a$b <-- 010$a", "����010$a��ISBN���������, �Զ�����102�ֶ�$a���Ҵ���$b��������", "Add102", false);

            // 410 <-- 225
            actions.NewItem("410 <-- 225", "��225$a���ݼ���410  $1200  $a", "Copy225To410", false);

            // 7*1$a <-- 200$f
            actions.NewItem("7*1$a <-- 200$f", "��200$f���ݼ���701/711�ֶ�$a", "Copy200fTo7x1a", false);

            // 7*2$a <-- 200$g
            actions.NewItem("7*2$a <-- 200$g", "��200$g���ݼ���702/712�ֶ�$a", "Copy200gTo7x2a", false);


            // 905$d <-- 690$a
            actions.NewItem("905$d <-- 690$a", "��690$a���ݼ���905�ֶ�$d", "Copy690aTo905d", false);


            // ����GCA���ߺ�
            actions.NewItem("����GCAT���ߺ�", "����701/711/702/712$a����, ����905$e", "AddGcatAuthorNumber", false);

            // �����ĽǺ������ߺ�
            actions.NewItem("�����ĽǺ������ߺ�", "����701/711/702/712$a����, ����905$e", "AddSjhmAuthorNumber", false);

            // �����ִκ�
            actions.NewItem("�����ִκ�", "����905$d����, ����905$e", "AddZhongcihao", false);

            //  ά���ִκ�
            actions.NewItem("ά���ִκ�", "����905$d�����е����, ����ά���ִκŵĽ���", "ManageZhongcihao", false);

            // �����
            actions.NewItem("210$a$c <-- 010$a", "����010$a��ISBN���������, �Զ��������������ֶ�210$a$c", "AddPublisher", false);

            // �ָ���
            actions.NewSeperator();

            // ά�� 102 ���Ҵ��� ��������
            actions.NewItem("ά��102���ձ�", "ISBN��������� �� 102�ֶ�$a���Ҵ���$b�������� �Ķ��ձ�", "Manage102", false);

            // ά�� 210 ����� ������
            actions.NewItem("ά��210���ձ�", "ISBN��������� �� 210�ֶ�$a�����$c�������� �Ķ��ձ�", "Manage210", false);

            // �ָ���
            actions.NewSeperator();

        }

        this.ScriptActions = actions;
    }

    #region ���ò˵�����״̬

    // ���ò˵�����״̬ -- ����ISBNΪ13
    void HyphenISBN_13_setMenu(object sender, SetMenuEventArgs e)
    {
        Field curfield = this.DetailForm.MarcEditor.FocusedField;
        if (curfield == null || curfield.Name != "010")
        {
            e.Action.Active = false;
            return;
        }
        Subfield a = curfield.Subfields["a"];
        if (a == null)
        {
            e.Action.Active = false;
            return;
        }

        string strISBN = a.Value;
        if (string.IsNullOrEmpty(strISBN) == true)
        {
            e.Action.Active = false;
            return;
        }

        if (IsbnSplitter.IsIsbn13(strISBN) == true)
        {
            e.Action.Active = false;
            return;
        }

        e.Action.Active = true;
    }

    // ���ò˵�����״̬ -- ����ISBNΪ10
    void HyphenISBN_10_setMenu(object sender, SetMenuEventArgs e)
    {
        Field curfield = this.DetailForm.MarcEditor.FocusedField;
        if (curfield == null || curfield.Name != "010")
        {
            e.Action.Active = false;
            return;
        }
        Subfield a = curfield.Subfields["a"];
        if (a == null)
        {
            e.Action.Active = false;
            return;
        }

        string strISBN = a.Value;
        if (string.IsNullOrEmpty(strISBN) == true)
        {
            e.Action.Active = false;
            return;
        }

        if (IsbnSplitter.IsIsbn13(strISBN) == true)
        {
            e.Action.Active = true;
            return;
        }

        e.Action.Active = false;
    }

    // ���ò˵�����״̬ -- 102���Ҵ��� ��������
    void Add102_setMenu(object sender, SetMenuEventArgs e)
    {
        Field curfield = this.DetailForm.MarcEditor.FocusedField;
        if (curfield != null && curfield.Name == "102")
            e.Action.Active = true;
        else
            e.Action.Active = false;
    }

    // ���ò˵�����״̬ -- 410 <-- 225
    void Copy225To410_setMenu(object sender, SetMenuEventArgs e)
    {
        Field curfield = this.DetailForm.MarcEditor.FocusedField;
        if (curfield != null &&
            (curfield.Name == "225"
            || curfield.Name == "410"))
            e.Action.Active = true;
        else
            e.Action.Active = false;
    }

    // ���ò˵�����״̬ -- 7*1$a <-- 200$f
    void Copy200fTo7x1a_setMenu(object sender, SetMenuEventArgs e)
    {
        Field curfield = this.DetailForm.MarcEditor.FocusedField;
        if (curfield != null &&
                    (curfield.Name == "701"
                    || curfield.Name == "711"))
            e.Action.Active = true;
        else
            e.Action.Active = false;
    }

    // ���ò˵�����״̬ -- 7*2$a <-- 200$g
    void Copy200gTo7x2a_setMenu(object sender, SetMenuEventArgs e)
    {
        Field curfield = this.DetailForm.MarcEditor.FocusedField;
        if (curfield != null &&
                    (curfield.Name == "702"
                    || curfield.Name == "712"))
            e.Action.Active = true;
        else
            e.Action.Active = false;
    }

    // ���ò˵�����״̬ -- 905$d <-- 690$a
    void Copy690aTo905d_setMenu(object sender, SetMenuEventArgs e)
    {
        Field curfield = this.DetailForm.MarcEditor.FocusedField;
        if (curfield != null &&
                    (curfield.Name == "905" || curfield.Name == "690"))
            e.Action.Active = true;
        else
            e.Action.Active = false;
    }

    // ���ò˵�����״̬ -- ����GCAT���ߺ�
    void AddGcatAuthorNumber_setMenu(object sender, SetMenuEventArgs e)
    {
        Field curfield = this.DetailForm.MarcEditor.FocusedField;
        if (curfield != null && curfield.Name == "905")
            e.Action.Active = true;
        else
            e.Action.Active = false;
    }

    // ���ò˵�����״̬ -- �����ĽǺ������ߺ�
    void AddSjhmAuthorNumber_setMenu(object sender, SetMenuEventArgs e)
    {
        Field curfield = this.DetailForm.MarcEditor.FocusedField;
        if (curfield != null && curfield.Name == "905")
            e.Action.Active = true;
        else
            e.Action.Active = false;
    }

    // ���ò˵�����״̬ -- �����ִκ�
    void AddZhongcihao_setMenu(object sender, SetMenuEventArgs e)
    {
        Field curfield = this.DetailForm.MarcEditor.FocusedField;
        if (curfield != null && curfield.Name == "905"
            && this.DetailForm.MarcEditor.FocusedSubfieldName == 'd')
            e.Action.Active = true;
        else
            e.Action.Active = false;
    }

    // ���ò˵�����״̬ -- �����
    void AddPublisher_setMenu(object sender, SetMenuEventArgs e)
    {
        Field curfield = this.DetailForm.MarcEditor.FocusedField;
        if (curfield != null && curfield.Name == "210")
            e.Action.Active = true;
        else
            e.Action.Active = false;
    }

    #endregion

#if OLD // ��ֹ
    public override void Main(object sender, HostEventArgs e)
	{
		Field curfield = this.DetailForm.MarcEditor.FocusedField;

		ScriptActionCollection actions = new ScriptActionCollection();

		bool bActive = false;


		// ��ƴ��
		actions.NewItem("��ƴ��", "��.....��ƴ��", "AddPinyin", false);

		// 7*1$a <-- 200$f
		if (curfield != null &&
			(curfield.Name == "701"
			|| curfield.Name == "711") )
			bActive = true;
		else
			bActive = false;
			
		actions.NewItem("7*1$a <-- 200$f", "��200$f���ݼ���701/711�ֶ�$a", "Copy200fTo7x1a", bActive);

		// 7*2$a <-- 200$g
		if (curfield != null &&
			(curfield.Name == "702"
			|| curfield.Name == "712") )
			bActive = true;
		else
			bActive = false;
		actions.NewItem("7*2$a <-- 200$g", "��200$g���ݼ���702/712�ֶ�$a", "Copy200gTo7x2a", bActive);

		// 410 <-- 225
		if (curfield != null &&
			(curfield.Name == "225"
			|| curfield.Name == "410") )
			bActive = true;
		else
			bActive = false;
		actions.NewItem("410 <-- 225", "��225$a���ݼ���410  $1200  $a", "Copy225To410", bActive);



		// �������ߺ�
		if (curfield != null && curfield.Name == "905")
			bActive = true;
		else
			bActive = false;

		actions.NewItem("�������ߺ�", "����701/711/702/712$a����, ����905$e", "AddAuthorNumber", bActive);

		// �����ִκ�
		if (curfield != null && curfield.Name == "905" && this.DetailForm.MarcEditor.FocusedSubfieldName == 'd')
			bActive = true;
		else
			bActive = false;
		actions.NewItem("�����ִκ�", "����905$d����, ����905$e", "AddZhongcihao", bActive);

		//  ά���ִκ�
		actions.NewItem("ά���ִκ�", "����905$d�����е����, ����ά���ִκŵĽ���", "ManageZhongcihao", false);

		// �����
		if (curfield != null && curfield.Name == "210")
			bActive = true;
		else
			bActive = false;
		actions.NewItem("210$a$c <-- 010$a", "����010$a��ISBN���������, �Զ��������������ֶ�210$a$c", "AddPublisher", bActive);


		// ����ISBNΪ13
		if (curfield != null && curfield.Name == "010")
			bActive = true;
		else
			bActive = false;
		actions.NewItem("����ISBN-13", "��010$a��ISBN���й���", "HyphenISBN_13", bActive);

		// ����ISBNΪ10
		if (curfield != null && curfield.Name == "010")
			bActive = true;
		else
			bActive = false;
		actions.NewItem("����ISBN-10", "��010$a��ISBN���й���", "HyphenISBN_10", bActive);

		// 102���Ҵ��� ��������
		if (curfield != null && curfield.Name == "102")
			bActive = true;
		else
			bActive = false;
		actions.NewItem("102$a$b <-- 010$a", "����010$a��ISBN���������, �Զ�����102�ֶ�$a���Ҵ���$b��������", "Add102", bActive);


		ScriptActionMenuDlg dlg = new ScriptActionMenuDlg();

		dlg.Actions = actions;
		if ((Control.ModifierKeys & Keys.Alt)== Keys.Alt)
			dlg.AutoRun = false;
		else
			dlg.AutoRun = this.DetailForm.MainForm.AppInfo.GetBoolean("detailform", "gen_auto_run", false);
		// dlg.StartPosition = FormStartPosition.CenterScreen;

		this.DetailForm.MainForm.AppInfo.LinkFormState(dlg, "gen_data_dlg_state");
		dlg.ShowDialog();
		this.DetailForm.MainForm.AppInfo.UnlinkFormState(dlg);


		this.DetailForm.MainForm.AppInfo.SetBoolean("detailform", "gen_auto_run", dlg.AutoRun);

		if (dlg.DialogResult == DialogResult.OK)
		{
			this.Invoke(dlg.SelectedAction.ScriptEntry);
		}
	}
#endif

    void AddPinyin()
    {
        AddPinyin(this.PinyinCfgXml,
            true,
            PinyinStyle.None,
            "",
            this.DetailForm.MainForm.AutoSelPinyin);
    }

    void RemovePinyin()
    {
        RemovePinyin(this.PinyinCfgXml);
        RemovePinyin(this.OldPinyinCfgXml);
    }

    void ClearPinyinCache()
    {
        this.DetailForm.SetSelectedPinyin(null);
    }


    void Copy200fTo7x1a()
    {
        Copy200gfTo7xxa("f", "701");
    }

    void Copy200gTo7x2a()
    {
        Copy200gfTo7xxa("g", "702");
    }

    void Copy200gfTo7xxa(string strFromSubfield, string strToField)
    {
        Field field_200 = this.DetailForm.MarcEditor.Record.Fields.GetOneField("200", 0);
        if (field_200 == null)
        {
            MessageBox.Show(this.DetailForm, "200�ֶβ�����");
            return;
        }

        SubfieldCollection subfields_200 = field_200.Subfields;

        Subfield subfield_f = subfields_200[strFromSubfield];

        if (subfield_f == null)
        {
            MessageBox.Show(this.DetailForm, "200$" + strFromSubfield + "������");
            return;
        }

        string strContent = subfield_f.Value;

        // ������ǰ��ֶ��ǲ���701
        Field field_701 = null;

        field_701 = this.DetailForm.MarcEditor.FocusedField;
        if (field_701 != null)
        {
            if (field_701.Name != strToField)
                field_701 = null;
        }

        if (field_701 == null)
        {
            field_701 = this.DetailForm.MarcEditor.Record.Fields.GetOneField(strToField, 0);

            if (field_701 == null)
            {
                field_701 = this.DetailForm.MarcEditor.Record.Fields.Add(strToField, "  ", "", true);
            }
        }

        if (field_701 == null)
            throw (new Exception("error ..."));

        Subfield subfield_701a = field_701.Subfields["a"];
        if (subfield_701a == null)
        {
            subfield_701a = new Subfield();
            subfield_701a.Name = "a";
        }

        subfield_701a.Value = strContent;
        field_701.Subfields["a"] = subfield_701a;
    }

    void AddGcatAuthorNumber()
    {
        string strAuthor = "";

#if NO
        strAuthor = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield("701", "a");

        if (strAuthor != "")
            goto BEGIN;

        strAuthor = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield("711", "a");

        if (strAuthor != "")
            goto BEGIN;

        strAuthor = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield("702", "a");

        if (strAuthor != "")
            goto BEGIN;

        strAuthor = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield("712", "a");
        if (strAuthor == "")
        {
            MessageBox.Show(this.DetailForm, "701/711/702/712�о�δ����&a,�޷�����");
            return;
        }
#endif
        string strError = "";
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

        strError = "MARC��¼�� 700/710/720/701/711/702/712�о�δ���ְ������ֵ� $a ���ֶ����ݣ��޷���������ַ���";
        goto ERROR1;
    FOUND:
        Debug.Assert(results.Count > 0, "");
        strAuthor = results[0];

        // BEGIN:
        string strGcatWebServiceUrl = this.DetailForm.MainForm.GcatServerUrl;   // "http://dp2003.com/dp2libraryws/gcat.asmx";

        string strNumber = "";

        // ������ߺ�
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        int nRet = GetGcatAuthorNumber(strGcatWebServiceUrl,
            strAuthor,
            out strNumber,
            out strError);
        if (nRet == -1)
            goto ERROR1;

        this.DetailForm.MarcEditor.Record.Fields.SetFirstSubfield("905", "e", strNumber);
        return;
    ERROR1:
        MessageBox.Show(this.DetailForm, strError);
    }

    void AddSjhmAuthorNumber()
    {
        string strError = "";

        string strAuthor = "";
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

        strError = "MARC��¼�� 700/710/720/701/711/702/712�о�δ���ְ������ֵ� $a ���ֶ����ݣ��޷���������ַ���";
        goto ERROR1;
    FOUND:
        Debug.Assert(results.Count > 0, "");
        strAuthor = results[0];

        string strNumber = "";

        // ����ĽǺ������ߺ�
        // return:
        //      -1  error
        //      0   canceled
        //      1   succeed
        int nRet = GetSjhmAuthorNumber(
            strAuthor,
            out strNumber,
            out strError);
        if (nRet == -1)
            goto ERROR1;

        this.DetailForm.MarcEditor.Record.Fields.SetFirstSubfield("905", "e", strNumber);
        return;
    ERROR1:
        MessageBox.Show(this.DetailForm, strError);
    }

    // new 
    void AddZhongcihao()
    {
        string strError = "";
        ZhongcihaoForm dlg = new ZhongcihaoForm();

        try
        {
            string strClass = "";
            string strNumber = "";
            int nRet = 0;

            strClass = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield("905", "d");

            if (strClass == "")
            {
                MessageBox.Show(this.DetailForm, "��¼�в�����905$d���ֶ�,����޷����ִκ�");
                return;
            }

            string strExistNumber = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield("905", "e");

            dlg.MainForm = this.DetailForm.MainForm;
            dlg.TopMost = true;
            dlg.MyselfBiblioRecPath = this.DetailForm.BiblioRecPath;
            dlg.LibraryServerName = this.DetailForm.ServerName;

            dlg.Show();
            // dlg.WindowState = FormWindowState.Minimized;

            // return:
            //      -1  error
            //      0   canceled
            //      1   succeed
            nRet = dlg.GetNumber(
                ZhongcihaoStyle.Seed,
                strClass,
                this.DetailForm.BiblioDbName,
                out strNumber,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            this.DetailForm.MarcEditor.Record.Fields.SetFirstSubfield("905", "e", strNumber);
            return;
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

    ERROR1:
        MessageBox.Show(this.DetailForm, strError);
    }

    // ά���ִκ�
    void ManageZhongcihao()
    {
        string strError = "";
        ZhongcihaoForm dlg = new ZhongcihaoForm();

        string strClass = "";
        int nRet = 0;

        strClass = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield("905", "d");

        dlg.MdiParent = this.DetailForm.MainForm;
        dlg.MainForm = this.DetailForm.MainForm;
        dlg.TopMost = true;
        dlg.AutoBeginSearch = true;

        dlg.ClassNumber = strClass;
        dlg.BiblioDbName = this.DetailForm.BiblioDbName;

        dlg.Show();
    }

    // �������ء�������
    void AddPublisher()
    {
        string strError = "";
        string strISBN = "";

        int nRet = 0;

        strISBN = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield("010", "a");

        if (strISBN.Trim() == "")
        {
            strError = "��¼�в�����010$a���ֶ�,����޷��ӳ��������ֶ�";
            goto ERROR1;
        }



        // �и�� ������ ���벿��
        string strPublisherNumber = "";
        nRet = this.DetailForm.MainForm.GetPublisherNumber(strISBN,
            out strPublisherNumber,
            out strError);
        if (nRet == -1)
        {
            goto ERROR1;
        }

        string strValue = "";

        nRet = this.DetailForm.GetPublisherInfo(strPublisherNumber,
            out strValue,
            out strError);
        if (nRet == -1)
            goto ERROR1;

        if (nRet == 0 || strValue == "")
        {
            // ��������Ŀ
            strValue = InputDlg.GetInput(
                this.DetailForm,
                null,
                "������ISBN������� '" + strPublisherNumber + "' ��Ӧ�ĳ���������(��ʽ �����:��������):",
                "�����:��������");
            if (strValue == null)
                return;	// ������������

            nRet = this.DetailForm.SetPublisherInfo(strPublisherNumber,
                strValue,
                out strError);
            if (nRet == -1)
                goto ERROR1;

        }

        // MessageBox.Show(this.DetailForm, strValue);

        // ��ȫ��ð���滻Ϊ��ǵ���̬
        strValue = strValue.Replace("��", ":");

        string strName = "";
        string strCity = "";
        nRet = strValue.IndexOf(":");
        if (nRet == -1)
        {
            strName = strValue;
        }
        else
        {
            strCity = strValue.Substring(0, nRet);
            strName = strValue.Substring(nRet + 1);
        }

        this.DetailForm.MarcEditor.Record.Fields.SetFirstSubfield("210", "a", strCity);
        this.DetailForm.MarcEditor.Record.Fields.SetFirstSubfield("210", "c", strName);


        return;

    ERROR1:
        MessageBox.Show(this.DetailForm, strError);


    }

    void HyphenISBN_13()
    {
        HyphenISBN(true);
    }


    void HyphenISBN_10()
    {
        HyphenISBN(false);
    }


    void HyphenISBN(bool bForce13)
    {
        string strError = "";
        string strISBN = "";
        int nRet = 0;

        strISBN = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield("010", "a");

        if (strISBN.Trim() == "")
        {
            MessageBox.Show(this.DetailForm, "��¼�в�����010$a���ֶ�,����޷����й���");
            return;
        }

        nRet = this.DetailForm.MainForm.LoadIsbnSplitter(true, out strError);
        if (nRet == -1)
            goto ERROR1;

        string strResult = "";

        nRet = this.DetailForm.MainForm.IsbnSplitter.IsbnInsertHyphen(strISBN,
            bForce13 == true ? "force13,strict" : "force10,strict",
                    out strResult,
                    out strError);
        if (nRet == -1)
            goto ERROR1;

        if (nRet == 1)
        {
            DialogResult result = MessageBox.Show(this.DetailForm,
                "ԭISBN '" + strISBN + "'�ӹ��� '" + strResult + "' ����У��λ�б仯��\r\n\r\n�Ƿ�����޸�?",
                "����ISBN",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
                return;

        }

        this.DetailForm.MarcEditor.Record.Fields.SetFirstSubfield("010", "a", strResult);

        return;

    ERROR1:
        MessageBox.Show(this.DetailForm, strError);

    }

    void Add102()
    {
        string strError = "";
        string strISBN = "";
        int nRet = 0;

        strISBN = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield("010", "a");

        if (strISBN.Trim() == "")
        {
            strError = "��¼�в�����010$a���ֶ�,����޷���102$a$b";
            goto ERROR1;
        }

        // �и�� ������ ���벿��
        string strPublisherNumber = "";
        nRet = this.DetailForm.MainForm.GetPublisherNumber(strISBN,
            out strPublisherNumber,
            out strError);
        if (nRet == -1)
        {
            goto ERROR1;
        }

        string strValue = "";

        nRet = this.DetailForm.Get102Info(strPublisherNumber,
            out strValue,
            out strError);
        if (nRet == -1)
            goto ERROR1;

        if (nRet == 0 || strValue == "")
        {
            // ��������Ŀ
            strValue = InputDlg.GetInput(
                this.DetailForm,
                null,
                "������ISBN��������� '" + strISBN + "' ��Ӧ��UNIMARC 102$a$b����(��ʽ ���Ҵ���[2λ]:���д���[6λ]):",
                "���Ҵ���[2λ]:���д���[6λ]");
            if (strValue == null)
                return;	// ������������

            nRet = this.DetailForm.Set102Info(strPublisherNumber,
                strValue,
                out strError);
            if (nRet == -1)
                goto ERROR1;

        }

        // MessageBox.Show(this.DetailForm, strValue);

        // ��ȫ��ð���滻Ϊ��ǵ���̬
        strValue = strValue.Replace("��", ":");

        string strCountryCode = "";
        string strCityCode = "";
        nRet = strValue.IndexOf(":");
        if (nRet == -1)
        {
            strCountryCode = strValue;

            if (strCountryCode.Length != 2)
            {
                strError = "���Ҵ��� '" + strCountryCode + "' Ӧ��Ϊ2�ַ�";
                goto ERROR1;
            }
        }
        else
        {
            strCountryCode = strValue.Substring(0, nRet);
            strCityCode = strValue.Substring(nRet + 1);
            if (strCountryCode.Length != 2)
            {
                strError = "ð��ǰ��Ĺ��Ҵ��벿�� '" + strCountryCode + "' Ӧ��Ϊ2�ַ�";
                goto ERROR1;
            }
            if (strCityCode.Length != 6)
            {
                strError = "ð�ź���ĳ��д��벿�� '" + strCityCode + "' Ӧ��Ϊ6�ַ�";
                goto ERROR1;
            }
        }

        this.DetailForm.MarcEditor.Record.Fields.SetFirstSubfield("102", "a", strCountryCode);
        this.DetailForm.MarcEditor.Record.Fields.SetFirstSubfield("102", "b", strCityCode);


        return;

    ERROR1:
        MessageBox.Show(this.DetailForm, strError);


    }

    void Copy225To410()
    {
        Field field_225 = this.DetailForm.MarcEditor.Record.Fields.GetOneField("225", 0);

        if (field_225 == null)
        {
            MessageBox.Show(this.DetailForm, "225�ֶβ�����");
            return;
        }

        SubfieldCollection subfields_225 = field_225.Subfields;



        Subfield subfield_a = subfields_225["a"];

        if (subfield_a == null)
        {
            MessageBox.Show(this.DetailForm, "225$" + "a" + "������");
            return;
        }

        string strContent = subfield_a.Value;

        // ������ǰ��ֶ��ǲ���410
        Field field_410 = null;

        field_410 = this.DetailForm.MarcEditor.FocusedField;
        if (field_410 != null)
        {
            if (field_410.Name != "410")
                field_410 = null;
        }

        bool bInitial410Value = false;	// 410�ֶε�ֵ�Ƿ��ʼ����

        if (field_410 == null)
        {
            field_410 = this.DetailForm.MarcEditor.Record.Fields.GetOneField("410", 0);

            if (field_410 == null)
            {
                field_410 = this.DetailForm.MarcEditor.Record.Fields.Add("410", "  ", new string((char)31, 1) + "1200  " + new string((char)31, 1) + "a", true);
                bInitial410Value = true;
            }


        }


        if (bInitial410Value == false)
        {
            field_410.Value = new string((char)31, 1) + "1200  " + new string((char)31, 1) + "a" + field_410.Value;
        }

        if (field_410 == null)
            throw (new Exception("error ..."));


        Subfield subfield_410a = field_410.Subfields["a"];
        if (subfield_410a == null)
        {
            subfield_410a = new Subfield();
            subfield_410a.Name = "a";
        }

        subfield_410a.Value = strContent;
        field_410.Subfields["a"] = subfield_410a;
    }

    // ά��210���չ�ϵ
    // 2008/10/17
    void Manage210()
    {
        string strError = "";
        string strISBN = "";
        int nRet = 0;

        strISBN = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield("010", "a").Trim();

        string strPublisherNumber = "";

        if (String.IsNullOrEmpty(strISBN) == false)
        {
            // �и�� ������ ���벿��
            nRet = this.DetailForm.MainForm.GetPublisherNumber(strISBN,
                out strPublisherNumber,
                out strError);
            if (nRet == -1)
            {
                goto ERROR1;
            }
        }

        if (String.IsNullOrEmpty(strPublisherNumber) == true)
            strPublisherNumber = "978-7-?";

        strPublisherNumber = InputDlg.GetInput(
                this.DetailForm,
                "ά��210���ձ� -- ��1��",
                "������ISBN�г�������벿��:",
                strPublisherNumber);
        if (strPublisherNumber == null)
            return;	// ������������

        string strValue = "";

        nRet = this.DetailForm.GetPublisherInfo(strPublisherNumber,
            out strValue,
            out strError);
        if (nRet == -1)
            goto ERROR1;

        if (nRet == 0 || strValue == "")
        {
            strValue = "�����:��������";
        }

        // ��������Ŀ
        strValue = InputDlg.GetInput(
            this.DetailForm,
            "ά��210���ձ� -- ��2��",
            "������ISBN��������� '" + strPublisherNumber + "' ��Ӧ��UNIMARC 210$a$c����(��ʽ �����:��������):",
            strValue);
        if (strValue == null)
            return;	// ������������

        if (strValue == "")
            goto DOSAVE;

        // MessageBox.Show(this.DetailForm, strValue);

        // ��ȫ��ð���滻Ϊ��ǵ���̬
        strValue = strValue.Replace("��", ":");

        string strName = "";
        string strCity = "";
        nRet = strValue.IndexOf(":");
        if (nRet == -1)
        {
            strError = "�����������ȱ��ð��";
            goto ERROR1;
            // strName = strValue;
        }
        else
        {
            strCity = strValue.Substring(0, nRet);
            strName = strValue.Substring(nRet + 1);
        }

        strValue = strCity + ":" + strName;

    DOSAVE:
        nRet = this.DetailForm.SetPublisherInfo(strPublisherNumber,
            strValue,
            out strError);
        if (nRet == -1)
            goto ERROR1;
        return;
    ERROR1:
        MessageBox.Show(this.DetailForm, strError);
    }

    // ά��102���չ�ϵ
    void Manage102()
    {
        string strError = "";
        string strISBN = "";
        int nRet = 0;

        strISBN = this.DetailForm.MarcEditor.Record.Fields.GetFirstSubfield("010", "a").Trim();

        string strPublisherNumber = "";

        if (String.IsNullOrEmpty(strISBN) == false)
        {
            // �и�� ������ ���벿��
            nRet = this.DetailForm.MainForm.GetPublisherNumber(strISBN,
                out strPublisherNumber,
                out strError);
            if (nRet == -1)
                goto ERROR1;
        }

        if (String.IsNullOrEmpty(strPublisherNumber) == true)
            strPublisherNumber = "978-7-?";

        strPublisherNumber = InputDlg.GetInput(
                this.DetailForm,
                "ά��102���ձ� -- ��1��",
                "������ISBN�г�������벿��:",
                strPublisherNumber);
        if (strPublisherNumber == null)
            return;	// ������������

        string strValue = "";

        nRet = this.DetailForm.Get102Info(strPublisherNumber,
            out strValue,
            out strError);
        if (nRet == -1)
            goto ERROR1;

        if (nRet == 0 || strValue == "")
        {
            strValue = "���Ҵ���[2λ]:���д���[6λ]";
        }

        // ��������Ŀ
        strValue = InputDlg.GetInput(
            this.DetailForm,
            "ά��102���ձ� -- ��2��",
            "������ISBN��������� '" + strPublisherNumber + "' ��Ӧ��UNIMARC 102$a$b����(��ʽ���Ҵ���[2λ]:���д���[6λ]):",
            strValue);
        if (strValue == null)
            return;	// ������������

        if (strValue == "")
            goto DOSAVE;

        // MessageBox.Show(this.DetailForm, strValue);

        // ��ȫ��ð���滻Ϊ��ǵ���̬
        strValue = strValue.Replace("��", ":");

        string strCountryCode = "";
        string strCityCode = "";
        nRet = strValue.IndexOf(":");
        if (nRet == -1)
        {
            strCountryCode = strValue;

            if (strCountryCode.Length != 2)
            {
                strError = "���Ҵ��� '" + strCountryCode + "' Ӧ��Ϊ2�ַ�";
                goto ERROR1;
            }
        }
        else
        {
            strCountryCode = strValue.Substring(0, nRet);
            strCityCode = strValue.Substring(nRet + 1);
            if (strCountryCode.Length != 2)
            {
                strError = "ð��ǰ��Ĺ��Ҵ��벿�� '" + strCountryCode + "' Ӧ��Ϊ2�ַ�";
                goto ERROR1;
            }
            if (strCityCode.Length != 6)
            {
                strError = "ð�ź���ĳ��д��벿�� '" + strCityCode + "' Ӧ��Ϊ6�ַ�";
                goto ERROR1;
            }
        }

        strValue = strCountryCode + ":" + strCityCode;

    DOSAVE:
        nRet = this.DetailForm.Set102Info(strPublisherNumber,
            strValue,
            out strError);
        if (nRet == -1)
            goto ERROR1;
        return;
    ERROR1:
        MessageBox.Show(this.DetailForm, strError);
    }

    void Copy690aTo905d()
    {
        Copy690aTo905d("a", "905");
    }

    void Copy690aTo905d(string strFromSubfield, string strToField)
    {
        Field field_690 = this.DetailForm.MarcEditor.Record.Fields.GetOneField("690", 0);
        if (field_690 == null)
        {
            MessageBox.Show(this.DetailForm, "690�ֶβ�����");
            return;
        }

        SubfieldCollection subfields_690 = field_690.Subfields;

        Subfield subfield_a = subfields_690[strFromSubfield];

        if (subfield_a == null)
        {
            MessageBox.Show(this.DetailForm, "690$" + strFromSubfield + "������");
            return;
        }

        string strContent = subfield_a.Value;

        // ������ǰ��ֶ��ǲ���905
        Field field_905 = null;

        field_905 = this.DetailForm.MarcEditor.FocusedField;
        if (field_905 != null)
        {
            if (field_905.Name != strToField)
                field_905 = null;
        }

        if (field_905 == null)
        {
            field_905 = this.DetailForm.MarcEditor.Record.Fields.GetOneField(strToField, 0);

            if (field_905 == null)
            {
                field_905 = this.DetailForm.MarcEditor.Record.Fields.Add(strToField, "  ", "", true);
            }
        }


        if (field_905 == null)
            throw (new Exception("error ..."));

        Subfield subfield_905d = field_905.Subfields["d"];
        if (subfield_905d == null)
        {
            subfield_905d = new Subfield();
            subfield_905d.Name = "d";
        }

        subfield_905d.Value = strContent;
        field_905.Subfields["d"] = subfield_905d;
    }
}