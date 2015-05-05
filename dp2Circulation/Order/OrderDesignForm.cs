using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DigitalPlatform;
using DigitalPlatform.CommonControl;
using DigitalPlatform.IO;

namespace dp2Circulation
{
    /// <summary>
    /// �滮�����Ի���
    /// �������䶩���ĸ�����
    /// </summary>
    internal partial class OrderDesignForm : Form
    {
        public DateTime? FocusedTime = null;

        const int WM_SETCARETPOS = API.WM_USER + 201;


        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        /// <summary>
        /// ���ȱʡ��¼
        /// </summary>
        public event GetDefaultRecordEventHandler GetDefaultRecord = null;
        // 2012/10/4
        /// <summary>
        /// ���ݴ����Ƿ��ڹ�Ͻ��Χ��
        /// </summary>
        public event VerifyLibraryCodeEventHandler VerifyLibraryCode = null;

        // ��������
        public List<DigitalPlatform.CommonControl.Item> Items 
        {
            get
            {
                return this.orderDesignControl1.Items;
            }
        }

        public OrderDesignForm()
        {
            InitializeComponent();
        }

        private void OrderDesignForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
            this.orderDesignControl1.GetValueTable -= new DigitalPlatform.GetValueTableEventHandler(orderCrossControl1_GetValueTable);
            this.orderDesignControl1.GetValueTable += new DigitalPlatform.GetValueTableEventHandler(orderCrossControl1_GetValueTable);

            this.orderDesignControl1.GetDefaultRecord -= new GetDefaultRecordEventHandler(orderCrossControl1_GetDefaultRecord);
            this.orderDesignControl1.GetDefaultRecord += new GetDefaultRecordEventHandler(orderCrossControl1_GetDefaultRecord);

            this.orderDesignControl1.VerifyLibraryCode -= new VerifyLibraryCodeEventHandler(orderDesignControl1_VerifyLibraryCode);
            this.orderDesignControl1.VerifyLibraryCode += new VerifyLibraryCodeEventHandler(orderDesignControl1_VerifyLibraryCode);

            // ������ڴ򿪵�ʱ�򣬷���һ������Ҳû�У�����Ҫ����һ���հ�����Ա��û��ڴ˻����Ͻ��б༭
            if (this.orderDesignControl1.Items.Count == 0)
            {
                // TODO: ��Ҫɾ��ȱʡ���������copyΪ0��Ψһ���Ȼ������һ��copyΪ0����������ӵ�����������κŵ���Ϣ��
                this.orderDesignControl1.InsertNewItem(0);  // this.orderDesignControl1.Items.Count

                this.orderDesignControl1.RemoveMultipleZeroCopyItem();
            }
            if (this.FocusedTime != null)
                API.PostMessage(this.Handle, WM_SETCARETPOS, 0, 0);
        }

        void orderDesignControl1_VerifyLibraryCode(object sender, VerifyLibraryCodeEventArgs e)
        {
            if (this.VerifyLibraryCode != null)
                this.VerifyLibraryCode(sender, e);
        }

        /// <summary>
        /// ȱʡ���ڹ���
        /// </summary>
        /// <param name="m">��Ϣ</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_SETCARETPOS:
                    {
                        EnsureCurrentVisible(this.FocusedTime);
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }

        // ȷ���͵�ǰ�����йص����������Ұ
        public void EnsureCurrentVisible(DateTime? time)
        {
            if (time == null)
                return;

            if (this.orderDesignControl1.Items.Count > 0)
            {
                string strTime = DateTimeUtil.DateTimeToString8((DateTime)time);
                int nCount = 0;
                foreach (DigitalPlatform.CommonControl.Item item in this.orderDesignControl1.Items)
                {
                    if (item.InRange(strTime) == true)
                    {
                        this.orderDesignControl1.EnsureVisible(item);
                        this.orderDesignControl1.SelectItem(item, nCount == 0 ? true : false);
                        nCount ++;
                    }
                    // TODO: ���û�о�ȷƥ��ģ������Լ�����͵�ǰʱ����������
                    // ���ʱ�䷶ΧΪ�գ������Կ�����ʱ��
                }

                if (nCount == 0)
                {
                    DigitalPlatform.CommonControl.Item item = this.orderDesignControl1.Items[this.orderDesignControl1.Items.Count - 1];
                    this.orderDesignControl1.EnsureVisible(item);
                    this.orderDesignControl1.SelectItem(item, true);
                }
            }
        }

        void orderCrossControl1_GetDefaultRecord(object sender, GetDefaultRecordEventArgs e)
        {
            if (this.GetDefaultRecord != null)
                this.GetDefaultRecord(sender, e);
        }

        void orderCrossControl1_GetValueTable(object sender, DigitalPlatform.GetValueTableEventArgs e)
        {
            if (this.GetValueTable != null)
                this.GetValueTable(sender, e);
        }

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                return this.orderDesignControl1.Changed;
            }
            set
            {
                this.orderDesignControl1.Changed = value;
            }
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";
            // ���м��
            // return:
            //      -1  �������г���
            //      0   ���û�з��ִ���
            //      1   ��鷢���˴���
            int nRet = this.orderDesignControl1.Check(out strError);
            if (nRet != 0)
            {
                if (nRet == 1)
                {
                    strError = "����鷢�����ݲ��淶����:\r\n\r\n" + strError;
                }
                goto ERROR1;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // ��װ���еĺ���
        public DigitalPlatform.CommonControl.Item AppendNewItem(string strOrderXml,
            out string strError)
        {
            return this.orderDesignControl1.AppendNewItem(strOrderXml, out strError);
        }

        // ��װ���еĺ���
        public void ClearAllItems()
        {
            this.orderDesignControl1.Clear();
        }


        public bool SeriesMode
        {
            get
            {
                return this.orderDesignControl1.SeriesMode;
            }
            set
            {
                this.orderDesignControl1.SeriesMode = value;
            }
        }

        // ��ȡֵ�б�ʱ��Ϊ���������ݿ���
        public string BiblioDbName
        {
            get
            {
                return this.orderDesignControl1.BiblioDbName;
            }
            set
            {
                this.orderDesignControl1.BiblioDbName = value;
            }
        }

        public bool CheckDupItem
        {
            get
            {
                return this.orderDesignControl1.CheckDupItem;
            }
            set
            {
                this.orderDesignControl1.CheckDupItem = value;
            }
        }
    }
}