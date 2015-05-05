using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DigitalPlatform;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// �������նԻ���
    /// </summary>
    internal partial class OrderArriveForm : Form
    {
        /// <summary>
        /// ��ܴ���
        /// </summary>
        public MainForm MainForm = null;

        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        // ��������
        public List<DigitalPlatform.CommonControl.Item> Items
        {
            get
            {
                return this.orderDesignControl1.Items;
            }
        }

        public OrderArriveForm()
        {
            InitializeComponent();
        }

        private void OrderArriveForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
            this.orderDesignControl1.GetValueTable -= new DigitalPlatform.GetValueTableEventHandler(orderCrossControl1_GetValueTable);
            this.orderDesignControl1.GetValueTable += new DigitalPlatform.GetValueTableEventHandler(orderCrossControl1_GetValueTable);

            // ������ڴ򿪵�ʱ�򣬷���һ������Ҳû�У�����Ҫ����һ���հ�����Ա��û��ڴ˻����Ͻ��б༭
            if (this.orderDesignControl1.Items.Count == 0)
            {
                this.orderDesignControl1.RemoveMultipleZeroCopyItem();
            }
        }

        void orderCrossControl1_GetValueTable(object sender, DigitalPlatform.GetValueTableEventArgs e)
        {
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

        // ����Ŀ���¼·��
        public string TargetRecPath
        {
            get
            {
                return this.orderDesignControl1.TargetRecPath;
            }
            set
            {
                this.orderDesignControl1.TargetRecPath = value;
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
    }
}