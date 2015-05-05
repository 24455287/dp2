using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// �ʻ����󼯺ϡ�����������ֹһ���û���ε�¼ռ�ö��session��Ҳ������������ÿ���ʻ���token
    /// </summary>
    public class AccountTable : List<Account>
    {
        public Account FindAccount(string strToken)
        {
            for (int i = 0; i < this.Count; i++)
            {
                Account account = this[i];
                if (account.Token == strToken)
                    return account;
            }

            return null;
        }
    }

}
