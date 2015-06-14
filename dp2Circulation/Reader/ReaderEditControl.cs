using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;

namespace dp2Circulation
{
    /// <summary>
    /// ������Ϣ�༭�ؼ�
    /// </summary>
    public partial class ReaderEditControl : ItemEditControlBase
    {
        /// <summary>
        /// ���ͼ��ݴ���
        /// </summary>
        public event GetLibraryCodeEventHandler GetLibraryCode = null;

        /// <summary>
        /// ����ƴ�����¼�
        /// </summary>
        public event EventHandler CreatePinyin = null;

        /// <summary>
        /// �༭Ȩ�޵��¼�
        /// </summary>
        public event EventHandler EditRights = null;

#if NO
        /// <summary>
        /// ���ֵ�б�
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;

        /// <summary>
        /// ���ݷ����ı�
        /// </summary>
        public event ContentChangedEventHandler ContentChanged = null;

        // Font ChangedTextFont = null;    // ��ʾ���Ķ��������ݵ�����

        XmlDocument RecordDom = null;

        bool m_bChanged = false;

        bool m_bInInitial = true;   // �Ƿ����ڳ�ʼ������֮��

        Color ColorChanged = Color.Yellow; // ��ʾ���ݸı������ɫ
        Color ColorDifference = Color.Blue; // ��ʾ�������ɫ
#endif

        #region ���ݳ�Ա

#if NO
        public string OldRecord = "";
        public byte[] Timestamp = null;
#endif

        /// <summary>
        /// ������ɫ
        /// </summary>
        [Category("Appearance")]
        [DescriptionAttribute("Back Color")]
        [DefaultValue(typeof(Color), "GhostWhite")]
        public override Color BackColor
        {
            get
            {
                return this.tableLayoutPanel_main.BackColor;
            }
            set
            {
                this.tableLayoutPanel_main.BackColor = value;
            }
        }

        /// <summary>
        /// ����֤�����
        /// </summary>
        public string Barcode
        {
            get
            {
                return this.textBox_barcode.Text;
            }
            set
            {
                this.textBox_barcode.Text = value;
            }
        }

        /// <summary>
        /// ֤��
        /// </summary>
        public string CardNumber
        {
            get
            {
                return this.textBox_cardNumber.Text;
            }
            set
            {
                this.textBox_cardNumber.Text = value;
            }
        }

        // ָ�������ַ���
        // base64���뷽ʽ
        /// <summary>
        /// ָ�������ַ�����
        /// base64���뷽ʽ
        /// </summary>
        public string Fingerprint
        {
            get
            {
                if (this.RecordDom == null)
                {
                    this.RecordDom = new XmlDocument();
                    this.RecordDom.LoadXml("<root />");
                }
                return DomUtil.GetElementText(this.RecordDom.DocumentElement, "fingerprint");
            }
            set 
            {
                if (this.RecordDom == null)
                {
                    this.RecordDom = new XmlDocument();
                    this.RecordDom.LoadXml("<root />");
                }
                DomUtil.SetElementText(this.RecordDom.DocumentElement, "fingerprint", value);

                // ���<fingerprint>Ԫ��
                if (string.IsNullOrEmpty(value) == true)
                {
                    XmlNode node = this.RecordDom.DocumentElement.SelectSingleNode("fingerprint");
                    if (node != null)
                    {
                        node.ParentNode.RemoveChild(node);
                    }
                }
            }
        }

        // ָ�������ַ����İ汾��
        /// <summary>
        /// ָ�������ַ����İ汾��
        /// </summary>
        public string FingerprintVersion
        {
            get
            {
                if (this.RecordDom == null)
                {
                    this.RecordDom = new XmlDocument();
                    this.RecordDom.LoadXml("<root />");
                }
                XmlNode node = this.RecordDom.DocumentElement.SelectSingleNode("fingerprint");
                if (node == null)
                    return "";
                return DomUtil.GetAttr(node, "version");
            }
            set
            {
                if (this.RecordDom == null)
                {
                    this.RecordDom = new XmlDocument();
                    this.RecordDom.LoadXml("<root />");
                }
                XmlNode node = this.RecordDom.DocumentElement.SelectSingleNode("fingerprint");
                if (node == null)
                {
                    if (string.IsNullOrEmpty(value) == true)
                        return; // ����,��ȻԪ�ز�����, �Ͳ���ɾ����
                    node = this.RecordDom.CreateElement("fingerprint");
                    this.RecordDom.DocumentElement.AppendChild(node);
                }

                DomUtil.SetAttr(node, "version", value);
            }
        }

        /// <summary>
        /// ���߼�¼״̬
        /// </summary>
        public string State
        {
            get
            {
                return this.comboBox_state.Text;
            }
            set
            {
                this.comboBox_state.Text = value;
            }
        }

        /// <summary>
        /// ע��
        /// </summary>
        public string Comment
        {
            get
            {
                return this.textBox_comment.Text;
            }
            set
            {
                this.textBox_comment.Text = value;
            }
        }

        /// <summary>
        /// ��������
        /// </summary>
        public string ReaderType
        {
            get
            {
                return this.comboBox_readerType.Text;
            }
            set
            {
                this.comboBox_readerType.Text = value;
            }
        }

        // ��������(RFC1123��ʽ)
        /// <summary>
        /// ��������(RFC1123��ʽ)
        /// </summary>
        public string CreateDate
        {
            get
            {
                return GetDateTimeString(this.dateControl_createDate.Value);
            }
            set
            {
                this.dateControl_createDate.Value = GetDateTime(value);
            }
        }

        // ʧЧ����(RFC1123��ʽ)
        /// <summary>
        /// ʧЧ����(RFC1123��ʽ)
        /// </summary>
        public string ExpireDate
        {
            get
            {
                return GetDateTimeString(this.dateControl_expireDate.Value);
            }
            set
            {
                this.dateControl_expireDate.Value = GetDateTime(value);
            }
        }

        // 2007/6/15
        // ���ʧЧ����(RFC1123��ʽ)
        /// <summary>
        /// ���ʧЧ����(RFC1123��ʽ)
        /// </summary>
        public string HireExpireDate
        {
            get
            {
                return GetDateTimeString(this.dateControl_hireExpireDate.Value);
            }
            set
            {
                this.dateControl_hireExpireDate.Value = GetDateTime(value);
            }
        }

        // 2007/6/15
        /// <summary>
        /// �������
        /// </summary>
        public string HirePeriod
        {
            get
            {
                return this.comboBox_hirePeriod.Text;
            }
            set
            {
                this.comboBox_hirePeriod.Text = value;
            }
        }

        // 2008/11/11
        /// <summary>
        /// Ѻ��
        /// </summary>
        public string Foregift
        {
            get
            {
                return this.textBox_foregift.Text;
            }
            set
            {
                this.textBox_foregift.Text = value;
            }
        }

        // ��������(RFC1123��ʽ)
        /// <summary>
        /// ��������(RFC1123��ʽ)
        /// </summary>
        public string DateOfBirth
        {
            get
            {
                return GetDateTimeString(this.dateControl_dateOfBirth.Value);
            }
            set
            {
                this.dateControl_dateOfBirth.Value = GetDateTime(value);
            }
        }

        /// <summary>
        /// ��������
        /// </summary>
        public string NameString
        {
            get
            {
                return this.textBox_name.Text;
            }
            set
            {
                this.textBox_name.Text = value;
            }
        }

        /// <summary>
        /// ��������ƴ��
        /// </summary>
        public string NamePinyin
        {
            get
            {
                return this.textBox_namePinyin.Text;
            }
            set
            {
                this.textBox_namePinyin.Text = value;
            }
        }

        /// <summary>
        /// �Ա�
        /// </summary>
        public string Gender
        {
            get
            {
                return this.comboBox_gender.Text;
            }
            set
            {
                this.comboBox_gender.Text = value;
            }
        }

        /// <summary>
        /// ���֤��
        /// </summary>
        public string IdCardNumber
        {
            get
            {
                return this.textBox_idCardNumber.Text;
            }
            set
            {
                this.textBox_idCardNumber.Text = value;
            }
        }

        /// <summary>
        /// ����
        /// </summary>
        public string Department
        {
            get
            {
                return this.textBox_department.Text;
            }
            set
            {
                this.textBox_department.Text = value;
            }
        }

        // 2009/7/17
        /// <summary>
        /// ְ��
        /// </summary>
        public string Post
        {
            get
            {
                return this.textBox_post.Text;
            }
            set
            {
                this.textBox_post.Text = value;
            }
        }

        /// <summary>
        /// ��ַ
        /// </summary>
        public string Address
        {
            get
            {
                return this.textBox_address.Text;
            }
            set
            {
                this.textBox_address.Text = value;
            }
        }

        /// <summary>
        /// �绰
        /// </summary>
        public string Tel
        {
            get
            {
                return this.textBox_tel.Text;
            }
            set
            {
                this.textBox_tel.Text = value;
            }
        }

        /// <summary>
        /// Email ��ַ
        /// </summary>
        public string Email
        {
            get
            {
                return this.textBox_email.Text;
            }
            set
            {
                this.textBox_email.Text = value;
            }
        }

        /// <summary>
        /// Ȩ��
        /// </summary>
        public string Rights
        {
            get
            {
                return this.textBox_rights.Text;
            }
            set
            {
                this.textBox_rights.Text = value;
            }
        }

        /// <summary>
        /// ��ȡ����
        /// </summary>
        public string Access
        {
            get
            {
                return this.textBox_access.Text;
            }
            set
            {
                this.textBox_access.Text = value;
            }
        }

        /// <summary>
        /// ��ի����
        /// </summary>
        public string PersonalLibrary
        {
            get
            {
                return this.textBox_personalLibrary.Text;
            }
            set
            {
                this.textBox_personalLibrary.Text = value;
            }
        }

        /// <summary>
        /// ����
        /// </summary>
        public string Friends
        {
            get
            {
                return this.textBox_friends.Text;
            }
            set
            {
                this.textBox_friends.Text = value;
            }
        }

        /// <summary>
        /// ���߼�¼·��
        /// </summary>
        public string RecPath
        {
            get
            {
                return this.textBox_recPath.Text;
            }
            set
            {
                this.textBox_recPath.Text = value;
            }
        }

        #endregion

        /// <summary>
        /// ���캯��
        /// </summary>
        public ReaderEditControl()
        {
            InitializeComponent();

            base._tableLayoutPanel_main = this.tableLayoutPanel_main;

            AddEvents(true);
            /*
            Debug.Assert(false, "");
            this.dateTimePicker_birthday.Value = DateTimePicker.MinimumDateTime;    // new DateTime(0);
             * */
        }

        private void ReaderEditControl_SizeChanged(object sender, EventArgs e)
        {
            tableLayoutPanel_main.Size = this.Size;
        }

#if NO
        public bool Initializing
        {
            get
            {
                return this.m_bInInitial;
            }
            set
            {
                this.m_bInInitial = value;
            }
        }

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
                bool bOldValue = this.m_bChanged;

                this.m_bChanged = value;
                if (this.m_bChanged == false)
                    this.ResetColor();

                // �����¼�
                if (bOldValue != value && this.ContentChanged != null)
                {
                    ContentChangedEventArgs e = new ContentChangedEventArgs();
                    e.OldChanged = bOldValue;
                    e.CurrentChanged = value;
                    ContentChanged(this, e);
                }
            }
        }
#endif

        /// <summary>
        /// �� RFC1123 ʱ���ַ���ת��Ϊ��ʾ�õı���ʱ���ַ���
        /// </summary>
        /// <param name="strTime">RFC1123 ʱ���ַ���</param>
        /// <returns>����ʱ���ַ���</returns>
        public static DateTime GetDateTime(string strTime)
        {
            DateTime time = new DateTime((long)0);

            if (String.IsNullOrEmpty(strTime) == true)
                return time;
            try
            {
                time = DateTimeUtil.FromRfc1123DateTimeString(strTime);
            }
            catch
            {
                return new DateTime((long)0);
            }

            return time.ToLocalTime();
        }

#if NO
        internal override void ResetColor()
        {
            Color color = this.tableLayoutPanel_main.BackColor;

            this.label_barcode_color.BackColor = color;
            this.label_cardNumber_color.BackColor = color;
            this.label_readerType_color.BackColor = color;
            this.label_state_color.BackColor = color;
            this.label_comment_color.BackColor = color;

            this.label_createDate_color.BackColor = color;
            this.label_expireDate_color.BackColor = color;
            this.label_dateOfBirth_color.BackColor = color;

            this.label_hireExpireDate_color.BackColor = color;
            this.label_hirePeriod_color.BackColor = color;
            this.label_foregift_color.BackColor = color;

            this.label_name_color.BackColor = color;
            this.label_namePinyin_color.BackColor = color;

            this.label_gender_color.BackColor = color;

            this.label_idCardNumber_color.BackColor = color;

            this.label_department_color.BackColor = color;

            this.label_post_color.BackColor = color;

            this.label_address_color.BackColor = color;

            this.label_tel_color.BackColor = color;

            this.label_email_color.BackColor = color;

            this.label_rights_color.BackColor = color;

            this.label_personalLibrary_color.BackColor = color;
            this.label_access_color.BackColor = color;
            this.label_friends_color.BackColor = color;

            this.label_recPath_color.BackColor = color;
        }
#endif
        // parameters:
        //      time    �Ǳ���ʱ��
        /// <summary>
        /// ��� RFC1123 ʱ���ַ���
        /// </summary>
        /// <param name="time">����ʱ��</param>
        /// <returns>RDC1123 �ַ���</returns>
        public static string GetDateTimeString(DateTime time)
        {
            if (time == new DateTime((long)0))
                return "";

            string strValue = "";
            try
            {
                strValue = DateTimeUtil.Rfc1123DateTimeStringEx(time);
            }
            catch
            {
                return "";
            }
            return strValue;
        }

#if NO
        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="strXml">���߼�¼ XML</param>
        /// <param name="strRecPath">���߼�¼·��</param>
        /// <param name="timestamp">ʱ���</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int SetData(string strXml,
            string strRecPath,
            byte[] timestamp,
            out string strError)
        {
            strError = "";

            this.OldRecord = strXml;
            this.Timestamp = timestamp;

            this.RecordDom = new XmlDocument();

            try
            {
                this.RecordDom.LoadXml(strXml);
            }
            catch (Exception ex)
            {
                strError = "XML����װ�ص�DOMʱ����" + ex.Message;
                return -1;
            }

            this.Initializing = true;

            try
            {
                // this.Clear();    // �����ƺ�ȫ��һЩ�������Щû��ָ�����ֶΣ��ر��ǽ���������ReaderInfoControl�е����ֶ�����

                this.Barcode = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "barcode");
                this.CardNumber = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "cardNumber");

                this.State = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "state");

                this.Comment = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "comment");


                this.ReaderType = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "readerType");

                this.CreateDate = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "createDate");

                this.ExpireDate = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "expireDate");

                // 2007/6/15
                // ���
                XmlNode nodeHire = this.RecordDom.DocumentElement.SelectSingleNode("hire");
                if (nodeHire != null)
                {
                    this.HireExpireDate = DomUtil.GetAttr(nodeHire, "expireDate");
                    this.HirePeriod = DomUtil.GetAttr(nodeHire, "period");
                }
                else
                {
                    this.HireExpireDate = "";
                    this.HirePeriod = "";
                }

                // 2008/11/11
                // Ѻ��
                this.Foregift = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "foregift");

                this.NameString = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "name");

                this.Gender = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "gender");

                this.DateOfBirth = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "dateOfBirth");
                if (string.IsNullOrEmpty(this.DateOfBirth) == true)
                {
                    // ���ݾ�ϰ��
                    this.DateOfBirth = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                        "birthday");
                }

                this.IdCardNumber = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "idCardNumber");

                this.Department = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "department");

                this.Post = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "post");

                this.Address = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "address");

                this.Tel = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "tel");

                this.Email = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "email");

                this.RecPath = strRecPath;
            }
            finally
            {
                this.Initializing = false;
            }

            this.Changed = false;

            return 0;
        }
#endif
        internal override void DomToMember(string strRecPath)
        {
            this.Barcode = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "barcode");
            this.CardNumber = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "cardNumber");

            this.State = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "state");

            this.Comment = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "comment");


            this.ReaderType = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "readerType");

            this.CreateDate = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "createDate");

            this.ExpireDate = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "expireDate");

            // 2007/6/15
            // ���
            XmlNode nodeHire = this.RecordDom.DocumentElement.SelectSingleNode("hire");
            if (nodeHire != null)
            {
                this.HireExpireDate = DomUtil.GetAttr(nodeHire, "expireDate");
                this.HirePeriod = DomUtil.GetAttr(nodeHire, "period");
            }
            else
            {
                this.HireExpireDate = "";
                this.HirePeriod = "";
            }

            // 2008/11/11
            // Ѻ��
            this.Foregift = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "foregift");

            this.NameString = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "name");

            this.NamePinyin = DomUtil.GetElementText(this.RecordDom.DocumentElement,
    "namePinyin");

            this.Gender = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "gender");

            this.DateOfBirth = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "dateOfBirth");
            if (string.IsNullOrEmpty(this.DateOfBirth) == true)
            {
                // ���ݾ�ϰ��
                this.DateOfBirth = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                    "birthday");
            }

            this.IdCardNumber = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "idCardNumber");

            this.Department = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "department");

            this.Post = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "post");

            this.Address = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "address");

            this.Tel = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "tel");

            this.Email = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "email");

            this.Rights = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "rights");
            this.PersonalLibrary = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "personalLibrary");
            this.Access = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "access"); 
            this.Friends = DomUtil.GetElementText(this.RecordDom.DocumentElement,
                "friends");
            this.RecPath = strRecPath;
        }

        /// <summary>
        /// ����ؼ���ȫ������
        /// </summary>
        public override void Clear()
        {
            this.Initializing = true; // ��ֹ��Ϊ�������ɫ

            try
            {
                this.Barcode = "";

                this.CardNumber = "";

                this.State = "";

                this.Comment = "";

                this.ReaderType = "";

                this.CreateDate = "";

                this.ExpireDate = "";

                // 2007/6/15
                this.HirePeriod = "";
                this.HireExpireDate = "";

                this.Foregift = "";

                this.NameString = "";

                this.NamePinyin = "";

                this.Gender = "";

                this.DateOfBirth = "";

                this.IdCardNumber = "";

                this.Department = "";

                this.Post = "";

                this.Address = "";

                this.Tel = "";

                this.Email = "";

                this.Rights = "";

                this.PersonalLibrary = "";
                this.Access = "";

                this.Friends = "";

                this.ResetColor();
            }
            finally
            {
                this.Initializing = false;
            }

        }

#if NO
        public XmlDocument DataDom
        {
            get
            {
                // 2012/12/28
                if (this.RecordDom == null)
                {
                    this.RecordDom = new XmlDocument();
                    this.RecordDom.LoadXml("<root />");
                } 
                this.RefreshDom();
                return this.RecordDom;
            }
        }
#endif

        /// <summary>
        /// ���ԤԼδȡ����
        /// </summary>
        /// <returns>���߼�¼�Ƿ������޸�</returns>
        public bool ClearOutofReservationCount()
        {
            if (this.RecordDom == null)
                return false;

            XmlNode root = this.RecordDom.DocumentElement.SelectSingleNode("outofReservations");
            if (root == null)
                return false;


            // �ۼƴ���
            string strCount = DomUtil.GetAttr(root, "count");
            if (String.IsNullOrEmpty(strCount) == true)
                return false;
            int nCount = 0;
            try
            {
                nCount = Convert.ToInt32(strCount);
            }
            catch
            {
            }
            if (nCount == 0)
                return false;

            DomUtil.SetAttr(root, "count", "0");
            this.Changed = true;
            return true;
        }

        internal override void RefreshDom()
        {
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "barcode", this.Barcode);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "cardNumber", this.CardNumber);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "state", this.State);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "comment", this.Comment);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "readerType", this.ReaderType);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "createDate", this.CreateDate);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "expireDate", this.ExpireDate);

            // 2007/6/15
            XmlNode nodeHire = null;
            nodeHire = this.RecordDom.DocumentElement.SelectSingleNode("hire");
            if (nodeHire == null)
            {
                nodeHire = this.RecordDom.CreateElement("hire");
                this.RecordDom.DocumentElement.AppendChild(nodeHire);
            }
            DomUtil.SetAttr(nodeHire, "expireDate", this.HireExpireDate);
            DomUtil.SetAttr(nodeHire, "period", this.HirePeriod);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "foregift", this.Foregift);



            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "name", this.NameString);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
    "namePinyin", this.NamePinyin);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "gender", this.Gender);

            // 2012/4/11
            // ���ݼ�¼���Ƿ��Ѿ���<dateOfBirth>Ԫ���������Ƿ�ʹ�����Ԫ�أ�����Ծɵ�dp2Library�汾д��¼�����ж�ʧ<dateOfBirth>Ԫ��
            XmlNode nodeExistBirthdate = this.RecordDom.DocumentElement.SelectSingleNode("dateOfBirth");    // BUG 2012/5/3 ԭ������.DocumentElement
            if (nodeExistBirthdate == null)
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "birthday", this.DateOfBirth);
            else
                DomUtil.SetElementText(this.RecordDom.DocumentElement,
                    "dateOfBirth", this.DateOfBirth);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "idCardNumber", this.IdCardNumber);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "department", this.Department);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "post", this.Post);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "address", this.Address);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "tel", this.Tel);

            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "email", this.Email);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "rights", this.Rights);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "personalLibrary", this.PersonalLibrary);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "access", this.Access);
            DomUtil.SetElementText(this.RecordDom.DocumentElement,
                "friends", this.Friends);
        }

        /// <summary>
        /// �������ʺ��ڱ���ļ�¼��Ϣ
        /// </summary>
        /// <param name="strXml">���ع���õĶ��߼�¼ XML</param>
        /// <param name="strError">���س�����Ϣ</param>
        /// <returns>-1: ����; 0: �ɹ�</returns>
        public int GetData(
            out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            if (this.RecordDom == null)
            {
                this.RecordDom = new XmlDocument();
                this.RecordDom.LoadXml("<root />");
            }


            /*
            if (this.Barcode == "")
            {
                strError = "Barcode��Ա��δ����";
                return -1;
            }*/

            this.RefreshDom();

            // ɾ����Ԫ��
            if (RemoveEmptyElement(ref this.RecordDom, out strError) == -1)
                return -1;

            strXml = this.RecordDom.OuterXml;
            return 0;
        }

        int RemoveEmptyElement(ref XmlDocument dom,
            out string strError)
        {
            strError = "";

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("*");
            foreach (XmlElement element in nodes)
            {
                string strInnerXml = element.InnerXml.Trim();
                if (string.IsNullOrEmpty(strInnerXml) == true
                    && element.Attributes.Count == 0)
                    element.ParentNode.RemoveChild(element);
            }

            return 0;
        }

#if NO
        /// <summary>
        /// ֻ��״̬���
        /// </summary>
        public enum ReadOnlyStyle
        {
            /// <summary>
            /// ���ȫ��ֻ��״̬���ָ��ɱ༭״̬
            /// </summary>
            Clear = 0,  // ���ȫ��ReadOnly״̬���ָ��ɱ༭״̬
            /// <summary>
            /// ȫ��ֻ��
            /// </summary>
            All = 1,    // ȫ����ֹ�޸�
            /// <summary>
            /// ͼ���һ�㹤����Ա�������޸�·��
            /// </summary>
            Librarian = 2,  // ͼ��ݹ�����Ա�������޸�·��
            /// <summary>
            /// ���ߡ������޸����������ֶ�
            /// </summary>
            Reader = 3, // ���ߡ������޸����������ֶ�
        }
#endif

        /// <summary>
        /// ����ֻ��״̬
        /// </summary>
        /// <param name="strStyle">�������ֻ��״̬��
        /// "all" ��ʾȫ��Ϊֻ����
        /// "librarian" ��ʾֻ�м�¼·����ʧЧʱ��Ϊֻ��������Ϊ�ɸ�д;
        /// "reader" ��ʾֻ�������ȼ���Ϊ�ɸ�д������Ϊֻ��;
        /// "clear" ��ʾ���ȫ��ֻ��״̬��ҵ��ȫ�����ǿɸ�д״̬
        /// </param>
        public override void SetReadOnly(string strStyle)
        {
            if (strStyle == "all")
            {
                this.textBox_barcode.ReadOnly = true;
                this.textBox_cardNumber.ReadOnly = true;
                this.comboBox_readerType.Enabled = false;
                this.comboBox_state.Enabled = false;
                this.textBox_comment.Enabled = false;

                this.dateControl_createDate.Enabled = false;

                this.dateControl_expireDate.Enabled = false;

                // 2007/6/15
                this.dateControl_hireExpireDate.Enabled = false;
                this.comboBox_hirePeriod.Enabled = false;

                this.textBox_foregift.ReadOnly = true;

                this.dateControl_dateOfBirth.Enabled = false;

                this.textBox_name.ReadOnly = true;
                this.textBox_namePinyin.ReadOnly = true;

                this.comboBox_gender.Enabled = false;

                this.textBox_idCardNumber.ReadOnly = true;

                this.textBox_department.ReadOnly = true;

                this.textBox_post.ReadOnly = true;

                this.textBox_address.ReadOnly = true;

                this.textBox_tel.ReadOnly = true;

                this.textBox_email.ReadOnly = true;
                this.textBox_rights.ReadOnly = true;
                this.textBox_personalLibrary.ReadOnly = true;
                this.textBox_access.ReadOnly = true;
                this.textBox_friends.ReadOnly = true;

                this.textBox_recPath.ReadOnly = true;
                return;
            }

            // �����
            this.textBox_barcode.ReadOnly = false;
            this.textBox_cardNumber.ReadOnly = false;
            this.comboBox_readerType.Enabled = true;
            this.comboBox_state.Enabled = true;
            this.textBox_comment.ReadOnly = false;
            this.dateControl_createDate.Enabled = true;
            this.dateControl_expireDate.Enabled = true;

            // 2007/6/15
            this.dateControl_hireExpireDate.Enabled = true;
            this.comboBox_hirePeriod.Enabled = true;

            this.textBox_foregift.ReadOnly = false;

            this.dateControl_dateOfBirth.Enabled = true;
            this.textBox_name.ReadOnly = false;
            this.textBox_namePinyin.ReadOnly = false;
            this.comboBox_gender.Enabled = true;
            this.textBox_idCardNumber.ReadOnly = false;
            this.textBox_department.ReadOnly = false;
            this.textBox_post.ReadOnly = false;
            this.textBox_address.ReadOnly = false;
            this.textBox_tel.ReadOnly = false;
            this.textBox_email.ReadOnly = false;
            this.textBox_rights.ReadOnly = false;
            this.textBox_personalLibrary.ReadOnly = false;
            this.textBox_access.ReadOnly = false;
            this.textBox_friends.ReadOnly = false;

            this.textBox_recPath.ReadOnly = false;

            if (strStyle == "librarian")
            {
                this.textBox_recPath.ReadOnly = true;

                // 2007/6/15
                this.dateControl_hireExpireDate.Enabled = false;

            }
            else if (strStyle == "reader")
            {
                this.textBox_barcode.ReadOnly = true;
                this.textBox_cardNumber.ReadOnly = true;
                this.comboBox_readerType.Enabled = false;
                this.comboBox_state.Enabled = false;
                this.textBox_comment.ReadOnly = true;

                this.dateControl_createDate.Enabled = false;

                this.dateControl_expireDate.Enabled = false;
                this.textBox_foregift.ReadOnly = true;

                // 2007/6/15
                this.dateControl_hireExpireDate.Enabled = false;
                this.comboBox_hirePeriod.Enabled = false;

                this.textBox_recPath.ReadOnly = true;
            }
            else if (strStyle == "clear")
            {
                // ǰ���Ѿ����
            }
        }

        // �Ƚ��Լ���refControl�����ݲ��죬��������ɫ��ʾ�����ֶ�
        /// <summary>
        /// �Ƚ��Լ���refControl�����ݲ��죬��������ɫ��ʾ�����ֶ�
        /// </summary>
        /// <param name="r">Ҫ���Լ����бȽϵĿؼ�����</param>
        public override void HighlightDifferences(ItemEditControlBase r)
        {
            var refControl = r as ReaderEditControl;

            if (this.Barcode != refControl.Barcode)
                this.label_barcode_color.BackColor = this.ColorDifference;

            if (this.CardNumber != refControl.CardNumber)
                this.label_cardNumber_color.BackColor = this.ColorDifference;

            if (this.ReaderType != refControl.ReaderType)
                this.label_readerType_color.BackColor = this.ColorDifference;

            if (this.State != refControl.State)
                this.label_state_color.BackColor = this.ColorDifference;

            if (this.Comment != refControl.Comment)
                this.label_comment_color.BackColor = this.ColorDifference;


            if (this.CreateDate != refControl.CreateDate)
                this.label_createDate_color.BackColor = this.ColorDifference;

            if (this.ExpireDate != refControl.ExpireDate)
                this.label_expireDate_color.BackColor = this.ColorDifference;

            // 2007/6/15
            if (this.HireExpireDate != refControl.HireExpireDate)
                this.label_hireExpireDate_color.BackColor = this.ColorDifference;

            if (this.HirePeriod != refControl.HirePeriod)
                this.label_hirePeriod_color.BackColor = this.ColorDifference;

            if (this.Foregift != refControl.Foregift)
                this.label_foregift_color.BackColor = this.ColorDifference;

            if (this.NameString != refControl.NameString)
                this.label_name_color.BackColor = this.ColorDifference;

            if (this.NamePinyin != refControl.NamePinyin)
                this.label_namePinyin_color.BackColor = this.ColorDifference;

            if (this.Gender != refControl.Gender)
                this.label_gender_color.BackColor = this.ColorDifference;

            if (this.DateOfBirth != refControl.DateOfBirth)
                this.label_dateOfBirth_color.BackColor = this.ColorDifference;

            if (this.IdCardNumber != refControl.IdCardNumber)
                this.label_idCardNumber_color.BackColor = this.ColorDifference;

            if (this.Department != refControl.Department)
                this.label_department_color.BackColor = this.ColorDifference;

            if (this.Post != refControl.Post)
                this.label_post_color.BackColor = this.ColorDifference;


            if (this.Address != refControl.Address)
                this.label_address_color.BackColor = this.ColorDifference;

            if (this.Tel != refControl.Tel)
                this.label_tel_color.BackColor = this.ColorDifference;

            if (this.Email != refControl.Email)
                this.label_email_color.BackColor = this.ColorDifference;

            if (this.Rights != refControl.Rights)
                this.label_rights_color.BackColor = this.ColorDifference;

            if (this.PersonalLibrary != refControl.PersonalLibrary)
                this.label_personalLibrary_color.BackColor = this.ColorDifference;

            if (this.Access != refControl.Access)
                this.label_access_color.BackColor = this.ColorDifference;

            if (this.Friends != refControl.Friends)
                this.label_friends_color.BackColor = this.ColorDifference;

            if (this.RecPath != refControl.RecPath)
                this.label_recPath_color.BackColor = this.ColorDifference;
        }

#if NO
        private void textBox_barcode_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_barcode_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_cardNumber_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_cardNumber_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void comboBox_readerType_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_readerType_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void comboBox_state_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_state_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_comment_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_comment_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void dateControl_createDate_DateTextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_createDate_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void dateControl_expireDate_DateTextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_expireDate_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        // 2007/6/15
        private void comboBox_hirePeriod_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_hirePeriod_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void dateControl_hireExpireDate_DateTextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_hireExpireDate_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }

        }

        private void textBox_foregift_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_foregift_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_name_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_name_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_namePinyin_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_namePinyin_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void comboBox_gender_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_gender_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void dateControl_dateOfBirth_DateTextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_dateOfBirth_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_idCardNumber_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_idCardNumber_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_department_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_department_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_post_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_post_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_address_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_address_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_tel_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_tel_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_email_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_email_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_rights_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_rights_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_personalLibrary_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_personalLibrary_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_access_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_access_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }

        private void textBox_friends_TextChanged(object sender, EventArgs e)
        {
            if (m_bInInitial == false)
            {
                this.label_friends_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
        }
#endif

        private void textBox_recPath_TextChanged(object sender, EventArgs e)
        {
            // ��ʹ�����Զ����»���б�ֵ
            this.comboBox_readerType.Items.Clear();
            this.comboBox_state.Items.Clear();
            this.comboBox_hirePeriod.Items.Clear();

#if NO
            if (m_bInInitial == false)
            {
                this.label_recPath_color.BackColor = this.ColorChanged;
                this.Changed = true;
            }
#endif
        }

        private void ReaderEditControl_Load(object sender, EventArgs e)
        {
            // this.ChangedTextFont = new Font(this.textBox_barcode.Font, FontStyle.Bold);
        }

        // 2009/7/19
        int m_nInDropDown = 0;

        private void comboBox_readerType_DropDown(object sender, EventArgs e)
        {
            ComboBox combobox = (ComboBox)sender;
            if (combobox.Items.Count > 0
    /*|| this.GetValueTable == null*/)
                return;

            // ��ֹ����
            if (this.m_nInDropDown > 0)
                return;

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {
                GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                e1.DbName = Global.GetDbName(this.RecPath);

                if (combobox == this.comboBox_readerType)
                    e1.TableName = "readerType";
                else if (combobox == this.comboBox_state)
                    e1.TableName = "readerState";
                else if (combobox == this.comboBox_hirePeriod)
                    e1.TableName = "hirePeriod";
                else
                {
                    Debug.Assert(false, "��֧�ֵ�sender");
                    return;
                }

                // this.GetValueTable(this, e1);
                this.OnGetValueTable(this, e1);

                if (e1.values != null)
                {
                    List<string> results = null;

                    string strRecPath = this.textBox_recPath.Text;
                    string strDbName = Global.GetDbName(strRecPath);
                    if (string.IsNullOrEmpty(strDbName) == false
                        && this.GetLibraryCode != null)
                    {
                        GetLibraryCodeEventArgs e2 = new GetLibraryCodeEventArgs();
                        e2.DbName = strDbName;
                        this.GetLibraryCode(this, e2);
                        string strLibraryCode = e2.LibraryCode;
                        // ���˳����Ϲܴ������Щֵ�ַ���
                        results = Global.FilterValuesWithLibraryCode(strLibraryCode,
                            StringUtil.FromStringArray(e1.values));
                    }
                    else
                    {
                        results = StringUtil.FromStringArray(e1.values);
                    }

                    foreach (string s in results)
                    {
                        combobox.Items.Add(s);
                    }
                }
                else
                {
                    combobox.Items.Add("<not found>");
                }

            }
            finally
            {
                this.Cursor = oldCursor;
                this.m_nInDropDown--;
            }
        }

        private void comboBox_state_DropDown(object sender, EventArgs e)
        {
            comboBox_readerType_DropDown(sender, e);

            // ����ȱʡֵ 2014/9/7
            if (this.comboBox_state.Items.Count == 0)
            {
                List<string> values = StringUtil.SplitList("ע��,ͣ��,��ʧ");
                foreach (string s in values)
                {
                    this.comboBox_state.Items.Add(s);
                }
            }
        }

        private void tableLayoutPanel_main_BackColorChanged(object sender, EventArgs e)
        {
            ResetColor();
        }

        // 2007/6/15
        private void comboBox_hirePeriod_DropDown(object sender, EventArgs e)
        {
            comboBox_readerType_DropDown(sender, e);
        }

        private void button_foregiftSum_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (this.textBox_foregift.Text == "")
            {
                strError = "���Ϊ��";
                goto ERROR1;
            }

            List<string> results = null;
            // ������"-123.4+10.55-20.3"�ļ۸��ַ����鲢����
            int nRet = PriceUtil.SumPrices(this.textBox_foregift.Text,
                out results,
                out strError);
            if (nRet == -1)
                goto ERROR1;

            string strText = "";
            for (int i = 0; i < results.Count; i++)
            {
                strText += results[i] + "\r\n";
            }

            MessageBox.Show(this, "���ܺ�Ľ��Ϊ: \r\n" + strText);

            return;
        ERROR1:
            MessageBox.Show(this, strError);

        }

#if NO
        private void comboBox_readerType_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_readerType.Invalidate();
        }

        private void comboBox_state_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_state.Invalidate();
        }

        private void comboBox_hirePeriod_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_hirePeriod.Invalidate();
        }

        private void comboBox_gender_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_gender.Invalidate();
        }
#endif

#if NO
        delegate void Delegate_filterValue(ComboBox combobox);

        // ���˵� {} ��Χ�Ĳ���
        void FileterValue(ComboBox combobox)
        {
            string strText = Global.GetPureSeletedValue(combobox.Text);
            if (combobox.Text != strText)
                combobox.Text = strText;
        }
#endif

        private void comboBox_readerType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(FileterValue);
            this.BeginInvoke(d, new object[] { sender });
#endif
        }

        private void comboBox_state_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(FileterValue);
            this.BeginInvoke(d, new object[] { sender });
#endif
        }

        private void comboBox_hirePeriod_SelectedIndexChanged(object sender, EventArgs e)
        {
            Global.FilterValue(this, (Control)sender);
#if NO
            Delegate_filterValue d = new Delegate_filterValue(FileterValue);
            this.BeginInvoke(d, new object[] { sender });
#endif
        }

        private void button_createNamePinyin_Click(object sender, EventArgs e)
        {
            if (this.CreatePinyin != null)
            {
                this.CreatePinyin(this, new EventArgs());
            }
        }

        // ֵ�б��汻�����ˡ�Ҫ������ list.Items
        internal void OnValueTableCacheCleared()
        {
            this.comboBox_readerType.Items.Clear();
            this.comboBox_state.Items.Clear();
            this.comboBox_hirePeriod.Items.Clear();
        }

        // �༭Ȩ��
        private void button_editRights_Click(object sender, EventArgs e)
        {
            if (this.EditRights != null)
            {
                this.EditRights(this, new EventArgs());
            }
        }





    }
// 
    /// <summary>
    /// ���ͼ��ݴ����¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void GetLibraryCodeEventHandler(object sender,
    GetLibraryCodeEventArgs e);

    /// <summary>
    /// ���ͼ��ݴ����¼��Ĳ���
    /// </summary>
    public class GetLibraryCodeEventArgs : EventArgs
    {
        /// <summary>
        /// [in] ���ݿ���
        /// </summary>
        public string DbName = "";  // [in] ���ݿ���

        /// <summary>
        /// [out] ͼ��ݴ���
        /// </summary>
        public string LibraryCode = ""; // [out] ͼ��ݴ���
    }
}
