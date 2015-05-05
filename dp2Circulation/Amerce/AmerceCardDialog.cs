using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

using DigitalPlatform.CirculationClient;
using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.Interfaces;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels.Http;

namespace dp2Circulation
{
    internal partial class AmerceCardDialog : Form
    {
        public string InterfaceUrl = "";
        public AmerceForm AmerceForm = null;
        public AmerceItem[] AmerceItems = null;
        public List<OverdueItemInfo> OverdueInfos = null;

        bool m_bDone = false;   // �ۿ��Ƿ����

#if NO
        IpcClientChannel channel = new IpcClientChannel();
        DkywInterface obj = null;
#endif

        IChannel channel = null;    // new IpcClientChannel();
        ICardCenter obj = null;

        public AmerceCardDialog()
        {
            InitializeComponent();
        }

        private void AmerceCardDialog_Load(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = this.StartChannel(
                this.InterfaceUrl,
                out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);

            this.timer1.Start();
        }

        void DisplayRest()
        {
            if (obj != null)
            {
                string strError = "";
                string strNewPrice = "";

                try
                {
                    lock (this.obj)
                    {
                        int nRet = obj.Deduct(this.CardNumber,
                            "",
                            "",
                            out strNewPrice,
                            out strError);
                        if (nRet == 1)
                        {
                            this.label_cardInfo.Text = "���� " + this.CardNumber + " \r\n�ĵ�ǰ���Ϊ " + strNewPrice;
                            this.timer1.Stop(); // ֻ��ѯһ��
                            return;
                        }
                    }
                }
                catch
                {
                    this.timer1.Stop();
                }
            }

            this.label_cardInfo.Text = "";
        }

        private void button_writeCard_Click(object sender, EventArgs e)
        {
            string strError = "";
            // ��ֹ����
            if (this.m_nIn > 0)
            {
                strError = "������ͻ���Ժ�����";
                goto ERROR1;
            }

            bool bSucceed = false;
            int nRet = 0;

            this.m_nIn++;
            this.button_writeCard.Enabled = false;
            try
            {

                // ��������ݿ����
                nRet = this.AmerceForm.Submit(
                    this.AmerceItems,
                    this.OverdueInfos,
                    false,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                // string strUsedCardNumber = "";
                string strNewPrice = "";
                // int nErrorCode = 0;
                string strPassword = "";

                int nRedoCount = 0;
            REDO:

                lock (this.obj)
                {
                    // �ӿ����Ŀۿ�
                    // parameters:
                    //      strRecord   ����XML��¼����������㹻�ı�ʾ�ֶμ���
                    //      strPriceString  ����ַ�����һ��Ϊ���ơ�CNY12.00����������ʽ
                    //      strRest    �ۿ������
                    // return:
                    //      -2  ���벻��ȷ
                    //      -1  ����(���ó��������ԭ��)
                    //      0   �ۿ�ɹ�(��Ϊ�������ͨԭ��)��ע��strError��Ӧ�����ز��ɹ���ԭ��
                    //      1   �ۿ�ɹ�
                    nRet = obj.Deduct(this.CardNumber,
                        this.SubmitPrice,
                        strPassword, // new
                        out strNewPrice, // new
                        out strError);
                }

#if NO
                // �ۿ�
                // parameters:
                //      strCardNumber   Ҫ��Ŀ��š����Ϊ�գ����ʾ��Ҫ�󿨺ţ�ֱ�Ӵӵ�ǰ���Ͽۿ�
                //      strSubMoney Ҫ�۵Ŀ����磺"0.01"
                //      strUsedCardNumber   ʵ�ʿۿ�Ŀ���
                //      strPrice    �ۿ������
                //      nErrorCode ԭʼ������
                //          -1:���Ӵ��ڴ���;
                //          -2:û�з��ֿ�Ƭ;
                //          -3:�޷���ȡ����Ψһ���к�; 
                //          -4:װ����Կ����;
                //          -5:��������;
                //          -6:���ѹ�����Ч��;
                //          -7:�������
                //          -8:����Ľ��̫��;
                //          -9:д��ʧ��;
                // return:
                //      -1  ����
                //      0   û�п�
                //      1   �ɹ��ۿ�ͻ����Ϣ
                //      2   ��Ȼ�ۿ�ɹ��������ϴ���ˮʧ��
                nRet = obj.SubCardMoney(this.CardNumber,
                    this.SubmitPrice,
                    strPassword,
                    out strUsedCardNumber,
                    out strNewPrice,
                    out nErrorCode,
                    out strError);
                if (nRet == 0)
                {
                    strError = "�����IC���������޷��ۿ�";
                    goto ERROR1;
                }
#endif

                if (nRet == -2)
                {
                    CardPasswordDialog dlg = new CardPasswordDialog();
                    MainForm.SetControlFont(dlg, this.Font, false);

                    if (nRedoCount == 0)
                        dlg.MessageText = "��(�ֿ���)����IC������";
                    else
                        dlg.MessageText = strError;

                    dlg.CardNumber = this.CardNumber;
                    dlg.StartPosition = FormStartPosition.CenterScreen;
                    dlg.ShowDialog(this);

                    if (dlg.DialogResult != DialogResult.OK)
                        return; // �����ۿ�

                    strPassword = dlg.Password;
                    nRedoCount++;
                    goto REDO;
                }

                if (nRet != 1)
                {
                    strError = "�ۿ����:" + strError;
                    goto ERROR1;
                }

                // this.label_cardInfo.Text = "����: " + strCardNumber + "\r\n" + "���Ͻ��: " + strNewPrice;

                this.m_bDone = true;
                this.button_writeCard.Enabled = false;  // �����ٴοۿ�
                bSucceed = true;
                MessageBox.Show(this, "�ۿ� " + this.SubmitPrice + " �ɹ�������� " + strNewPrice);

                if (nRet == 2)
                {
                    MessageBox.Show(this, strError);
                }
            }
            catch (Exception ex)
            {
                strError = "����:" + ex.Message;
                goto ERROR1;
            }
            finally
            {
                if (bSucceed == false)
                {
                    string strError_1 = "";
                    nRet = this.AmerceForm.RollBack(out strError_1);
                    if (nRet == -1)
                    {
                        strError_1 = "��Խ��Ѳ�����Rollbackʧ��: " + strError_1 + "\r\n��ϵͳ����Ա�����ֶ�����";
                        MessageBox.Show(this, strError_1);
                    }
                }

                this.m_nIn--;

                if (this.m_bDone == false)
                    this.button_writeCard.Enabled = true;
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

        int m_nIn = 0;

        void StopChannel()
        {
            this.EndChannel();
        }

        private void AmerceCardDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.timer1.Stop();

            this.StopChannel();
        }

        int StartChannel(
    string strUrl,
    out string strError)
        {
            strError = "";

            Uri uri = new Uri(strUrl);
            string strScheme = uri.Scheme.ToLower();

            if (strScheme == "ipc")
                channel = new IpcClientChannel();
            else if (strScheme == "tcp")
                channel = new TcpClientChannel();
            else if (strScheme == "http")
                channel = new HttpClientChannel();
            else
            {
                strError = "URL '" + strUrl + "' �а������޷�ʶ���Scheme '" + strScheme + "'��ֻ��ʹ�� ipc tcp http ֮һ";
                return -1;
            }

            //Register the channel with ChannelServices.
            ChannelServices.RegisterChannel(channel, false);

            try
            {
                this.obj = (ICardCenter)Activator.GetObject(typeof(ICardCenter),
                    strUrl);
                if (this.obj == null)
                {
                    strError = "�޷����ӵ� Remoting ������ " + strUrl;
                    return -1;
                }
            }
            finally
            {
            }

            return 0;
        }

        void EndChannel()
        {
            ChannelServices.UnregisterChannel(this.channel);
        }

#if NO
        int StartChannel(out string strError)
        {
            strError = "";

            //Register the channel with ChannelServices.
            ChannelServices.RegisterChannel(channel, false);

            try
            {
                obj = (DkywInterface)Activator.GetObject(typeof(DkywInterface),
                    "ipc://CardChannel/DkywInterface");
                if (obj == null)
                {
                    strError = "could not locate Card Server";
                    return -1;
                }
            }
            finally
            {
            }

            return 0;
        }

        void EndChannel()
        {
            ChannelServices.UnregisterChannel(channel);
        }
#endif
        string m_strSubmitPrice = "";

        // ����Ҫ�ۿ��ֵ�������֣�û�л��ҵ�λ�����Ϊ��������ʾ�ۿ�
        public string SubmitPrice
        {
            get
            {
                return this.m_strSubmitPrice;
            }
            set
            {
                this.m_strSubmitPrice = value;
                this.label_thisPrice.Text = "������ۿ�: " + value;
            }
        }

        // ȷ���Ŀ��ţ���ǰ�����ߵĿ�

        string m_strCardNumber = "";

        public string CardNumber
        {
            get
            {
                return this.m_strCardNumber;
            }
            set
            {
                this.m_strCardNumber = value;
            }
        }

        // ���ÿ�Ƭ��ʾ�ռ����ɫ
        // parameters:
        //      nState  0 ���� 1 �������� 2 û�п�
        void SetColor(int nState)
        {
            if (nState == 0)
            {
                this.label_cardInfo.BackColor = Color.LightYellow;
                this.label_cardInfo.ForeColor = Color.Black;
                if (this.m_bDone == true)
                    this.button_writeCard.Enabled = false;
                else
                    this.button_writeCard.Enabled = true;
                return;
            }

            if (nState == 1)
            {
                this.label_cardInfo.BackColor = Color.LightYellow;
                this.label_cardInfo.ForeColor = Color.Red;
                this.button_writeCard.Enabled = false;
                return;
            }

            if (nState == 2)
            {
                this.label_cardInfo.BackColor = Color.LightGray;
                this.label_cardInfo.ForeColor = Color.Black;
                this.button_writeCard.Enabled = false;
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            DisplayRest();
        }


    }
}