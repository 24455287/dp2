using System;
using System.Collections;
using System.Collections.Generic;

namespace DigitalPlatform.Script
{
    // �ű���������
    public class ScriptAction
    {
        public string Name = "";
        public string Comment = "";
        public string ScriptEntry = "";	// �ű���ں�����

        public bool Active = false;

        public char ShortcutKey = (char)0;  // ��ݼ� 2011/8/3
    }

    /// <summary>
    /// Ctrl+A �������Ƽ���
    /// </summary>
    public class ScriptActionCollection : List<ScriptAction>
    {
        public ScriptActionCollection()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        // ����һ��������
        public ScriptAction NewItem(string strName,
            string strComment,
            string strScriptEntry,
            bool bActive,
            char chShortcutKey = (char)0)
        {
            ScriptAction item = new ScriptAction();
            item.Name = strName;
            item.Comment = strComment;
            item.ScriptEntry = strScriptEntry;
            item.Active = bActive;
            item.ShortcutKey = chShortcutKey;

            this.Add(item);
            return item;
        }

        // 2011/8/3
        // ����һ���ָ���
        public ScriptAction NewSeperator()
        {
            ScriptAction item = new ScriptAction();
            item.Name = "-";
            this.Add(item);
            return item;
        }
    }
}
