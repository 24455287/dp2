using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using System.Drawing.Drawing2D;


namespace DigitalPlatform.Marc
{
    // �ֶ�
    /// <summary>
    /// �ֶζ���
    /// </summary>
	public class Field
	{
        /// <summary>
        /// ������Ҳ���ǵ�ǰ�ֶζ������������ֶζ�������
        /// </summary>
		internal FieldCollection container = null;

		internal string m_strName = "";
		internal string m_strIndicator = "";
		internal string m_strValue = "";
		internal string m_strNameCaption = "�ֶ�˵��";

		internal int PureHeight = 20;

        /// <summary>
        /// ��ǰ�����Ƿ���ѡ��״̬
        /// </summary>
		public bool Selected = false;

        /// <summary>
        /// ���캯��
        /// </summary>
		public Field()
		{
		}

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="field_collection">ӵ�б�������ֶζ�������</param>
		public Field(FieldCollection field_collection)
		{
			this.container = field_collection;
		}

		// �ֶ���
        /// <summary>
        /// ��ȡ�������ֶ���
        /// </summary>
		public string Name
		{
			get
			{
				return this.m_strName;
			}
			set
			{
				if (this.m_strName != value)
				{
					this.m_strName = value;

                    if (this.container == null)
                        return;

					this.m_strNameCaption = this.container.MarcEditor.GetLabel(this.m_strName);

					if (this.container.MarcEditor.FocusedField == this
						&& this.container.MarcEditor.m_nFocusCol == 1)
					{
						this.container.MarcEditor.curEdit.Text = this.m_strName;
					}

					// ʧЧ???�ò����жϵ�ǰԪ����ĩβԪ�شӶ�ʹ��BoundsPortion.FieldAndBottom
					Rectangle rect = this.container.MarcEditor.GetItemBounds(this.container.IndexOf(this),
						1,
						BoundsPortion.Field);
					this.container.MarcEditor.Invalidate(rect);

					// �ĵ������ı�
					this.container.MarcEditor.FireTextChanged();

				}
			}
		}

        internal void RefreshNameCaption()
        {
            this.m_strNameCaption = this.container.MarcEditor.GetLabel(this.m_strName);
        }

		// �ֶ�ָʾ��
        /// <summary>
        /// ��ȡ�������ֶ�ָʾ��
        /// </summary>
		public string Indicator
		{
			get
			{
				return this.m_strIndicator;
			}
			set
			{
				if (this.m_strIndicator != value)
				{
					this.m_strIndicator = value;

                    if (this.container == null)
                        return;


					if (this.container != null
                        && this.container.MarcEditor.FocusedField == this
						&& this.container.MarcEditor.m_nFocusCol == 2
						&& Record.IsControlFieldName(this.Name) == false)
					{
						this.container.MarcEditor.curEdit.Text = this.m_strIndicator;
					}


					// ʧЧ???�ò����жϵ�ǰԪ����ĩβԪ�شӶ�ʹ��BoundsPortion.FieldAndBottom
					Rectangle rect = this.container.MarcEditor.GetItemBounds(this.container.IndexOf(this),
						1,
						BoundsPortion.Field);
					this.container.MarcEditor.Invalidate(rect);

					// �ĵ������ı�
					this.container.MarcEditor.FireTextChanged();
				}
			}
		}

		// �ֶ�ֵ��getû���滻^����
		internal string ValueKernel
		{
			get
			{
				return this.m_strValue;
			}
            /*
			set
			{
				string strValue = value;
                strValue = strValue.Replace(Record.SUBFLD, Record.KERNEL_SUBFLD);

                if (this.container == null)
                    return;
                if (this.container.MarcEditor == null)
                    return;

				if (this.m_strValue != strValue)
				{
					this.m_strValue = strValue;

					if (this.container.MarcEditor.FocusedField == this
						&& this.container.MarcEditor.m_nFocusCol == 3)
					{
						this.container.MarcEditor.curEdit.Text = this.m_strValue;
					}

					// ʧЧ???�ò����жϵ�ǰԪ����ĩβԪ�شӶ�ʹ��BoundsPortion.FieldAndBottom
					Rectangle rect = this.container.MarcEditor.GetItemBounds(this.container.IndexOf(this),
						-1,
						BoundsPortion.FieldAndBottom);
					this.container.MarcEditor.Invalidate(rect);

					// �ĵ������ı�
					this.container.MarcEditor.FireTextChanged();
				}
			}
             */
		}

        /// <summary>
        /// ��ȡ������ �ֶ�ָʾ�������ֶ�����
        /// </summary>
        public string IndicatorAndValue
        {
            get
            {
                return this.Indicator + this.Value;
            }
            set
            {
                if (Record.IsControlFieldName(this.Name) == true)
                {
                    this.Value = value;
                    return;
                }

                if (value.Length >= 2)
                {
                    this.Indicator = value.Substring(0, 2);
                    this.Value = value.Substring(2);
                }
                else
                {
                    this.Indicator = value.PadRight(2, ' ');    // ���հ�
                    this.Value = "";
                }
            }
        }

        // �ֶ�ֵ��get�滻��^����
        /// <summary>
        /// ��ȡ������ �ֶ�ֵ��Ҳ�����ֶ�����
        /// </summary>
        public string Value
        {
            get
            {
                return this.m_strValue.Replace(Record.KERNEL_SUBFLD, Record.SUBFLD);
            }
            set
            {
                string strValue = value;
                strValue = strValue.Replace(Record.SUBFLD, Record.KERNEL_SUBFLD);

                if (this.container == null)
                    return;
                if (this.container.MarcEditor == null)
                    return;

                if (this.m_strValue != strValue)
                {
                    this.m_strValue = strValue;

                    this.CalculateHeight(null, false);  // ���¼���߶� 2014/11/4

                    if (this.container.MarcEditor.FocusedField == this
                        && this.container.MarcEditor.m_nFocusCol == 3)
                    {
                        this.container.MarcEditor.curEdit.Text = this.m_strValue;
                    }

                    // ʧЧ???�ò����жϵ�ǰԪ����ĩβԪ�شӶ�ʹ��BoundsPortion.FieldAndBottom
                    Rectangle rect = this.container.MarcEditor.GetItemBounds(this.container.IndexOf(this),
                        -1,
                        BoundsPortion.FieldAndBottom);
                    this.container.MarcEditor.Invalidate(rect);

                    // �ĵ������ı�
                    this.container.MarcEditor.FireTextChanged();
                }
            }
        }
		
		// �ֶ�����˵��
		internal string NameCaption
		{
			get
			{
				return this.m_strNameCaption;
			}
		}

		// ���ֶ��ܸ߶�
        /// <summary>
        /// ��ȡ���ֶε���ʾ����ĸ߶�
        /// </summary>
		public int TotalHeight
		{
			get
			{
				return this.container.record.GridHorzLineHeight 
					+ this.container.record.CellTopBlank 
					+ this.PureHeight
					+ this.container.record.CellBottomBlank;
			}
		}

        /// <summary>
        /// ��ȡ�����ñ��ֶε� MARC �ַ��� (���ڸ�ʽ)
        /// </summary>
		public string Text
		{
			get
			{
				return this.GetFieldMarc(false);
			}
			set
			{
				this.SetFieldMarc(value);

                if (this.container == null)
                    return;
                if (this.container.MarcEditor == null)
                    return;

				// ʧЧ???�ò����жϵ�ǰԪ����ĩβԪ�شӶ�ʹ��BoundsPortion.FieldAndBottom
				Rectangle rect = this.container.MarcEditor.GetItemBounds(this.container.IndexOf(this),
					-1,
					BoundsPortion.FieldAndBottom);

				// Ӧ��ʧЧ�����������������Ż�
				InvalidateRect iRect = new InvalidateRect();
				iRect.bAll = false;
				iRect.rect  = rect;
				this.container.MarcEditor.AfterDocumentChanged(ScrollBarMember.Vert,
					iRect);
			}
		}

        // ����ֶε�MARC��ʽ
        // parameters:
        //      bAddFLDEND  �Ƿ���ֶν�����
        // return:
        //      �ֶε�MARC�ַ���
        /// <summary>
        /// ��ȡ���ֶε� MARC �ַ��� (���ڸ�ʽ)
        /// </summary>
        /// <param name="bAddFLDEND">�Ƿ�����ֶν�����</param>
        /// <returns>MARC �ַ���</returns>
		public string GetFieldMarc(bool bAddFLDEND)
		{
			if (this.Name == "###") // ͷ����
			{
                this.m_strValue = this.m_strValue.PadRight(24, '?');
				return this.m_strValue;
			}


			if (Record.IsControlFieldName(this.m_strName) == true) // �����ֶ�
			{
				if (this.Indicator != "")
				{
					//Debug.Assert(false,"�����ܵ�����������ֶ����ֶ�ָʾ��");
					this.m_strIndicator = "";
				}
			}
			else
			{
				if(this.Indicator.Length != 2)
				{
					//Debug.Assert(false,"�����ܵ�������ֶ�ָʾ��1��������λ");

					if (this.m_strIndicator.Length > 2)
						this.m_strIndicator = this.m_strIndicator.Substring(0,2);
					else
						this.m_strIndicator = this.m_strIndicator.PadLeft(2,' ');
				}

			}
			string strFieldMarc = this.m_strName + this.m_strIndicator + this.m_strValue;

			if (bAddFLDEND == true)
				strFieldMarc += Record.FLDEND;


            strFieldMarc = strFieldMarc.Replace(Record.KERNEL_SUBFLD, Record.SUBFLD);
#if BIDI_SUPPORT
           strFieldMarc = strFieldMarc.Replace("\x200e", "");
#endif
            return strFieldMarc;
		}

                /// <summary>
        /// ���ñ��ֶε� MARC �ַ��� (���ڸ�ʽ)
        /// </summary>
        /// <param name="strFieldMarc">���ֶε� MARC �ַ���</param>
        public void SetFieldMarc(string strFieldMarc)
        {
            SetFieldMarc(strFieldMarc, true);
        }

        /// <summary>
        /// ���ñ��ֶε� MARC �ַ��� (���ڸ�ʽ)
        /// </summary>
        /// <param name="strFieldMarc">���ֶε� MARC �ַ���</param>
        /// <param name="bFlushEdit">�Ƿ��Զ�ˢ��С Edit</param>
		internal void SetFieldMarc(string strFieldMarc, bool bFlushEdit)
		{
            if (this.container == null)
                return;

            int index = this.container.IndexOf(this);

			if (index == 0)
			{
				this.m_strValue = strFieldMarc;
                // 2011/4/21
                this.CalculateHeight(null,
    true);
				return;
			}

			if (strFieldMarc.Length < 3)
				strFieldMarc = strFieldMarc + new string(' ',3-strFieldMarc.Length);

			string strName = "";
			string strIndicator = "";
			string strValue = "";

			strName = strFieldMarc.Substring(0,3);
			if (Record.IsControlFieldName(strName) == true)
			{
				strIndicator = "";
				strValue = strFieldMarc.Substring(3);
			}
			else
			{
				if (strFieldMarc.Length < 5)
					strFieldMarc = strFieldMarc + new string(' ',5-strFieldMarc.Length);

				strIndicator = strFieldMarc.Substring(3,2);

				strValue = strFieldMarc.Substring(5);
			}

			string strCaption = this.container.MarcEditor.GetLabel(strName);

            strValue = strValue.Replace(Record.SUBFLD, Record.KERNEL_SUBFLD);
			this.m_strNameCaption = strCaption;
			this.m_strName = strName;
			this.m_strIndicator = strIndicator;
			this.m_strValue = strValue;
			if (this.container.MarcEditor.FocusedField == this)
			{
                /*
                if (this.container.MarcEditor.m_nFocusCol == 2)
    				this.container.MarcEditor.curEdit.Text = this.m_strValue;
                 * */
                if (bFlushEdit == true) // 2014/7/10
                    this.container.MarcEditor.ItemTextToEditControl();  // 2009/3/6 changed
			}
			this.CalculateHeight(null,
				true);

			this.container.MarcEditor.FireTextChanged();
		}

		// �����еĸ߶�
		//		g	Graphics�������Ϊnull�����Զ���
		//		bIgnoreEdit	�Ƿ����Сedit�ؼ� false������
		internal void CalculateHeight(Graphics g, bool bIgnoreEdit)
		{
			if (g == null)
				g = Graphics.FromHwnd(this.container.MarcEditor.Handle);

			Font font = this.container.MarcEditor.Font;//.DefaultTextFont;

            Font fixedfont = this.container.MarcEditor.FixedSizeFont;

			//IntPtr hFontOld = IntPtr.Zero;

			// ����Name
            /*
			SizeF size = g.MeasureString(this.m_strName,
				font, 
				this.container.record.NamePureWidth, 
				new StringFormat());
             */
            SizeF size = TextRenderer.MeasureText(g,
                this.m_strName,
                fixedfont,
                new Size(container.record.NamePureWidth, -1),
                MarcEditor.editflags);


			int h1 = (int)size.Height;

			// ����Indicator1
            /*
			size = g.MeasureString(this.m_strIndicator,
				font, 
				container.record.IndicatorPureWidth,
				new StringFormat());
             */
            size = TextRenderer.MeasureText(g,
                this.m_strIndicator,
                fixedfont,
                new Size(container.record.IndicatorPureWidth, -1),
                MarcEditor.editflags);

			int h2 = (int)size.Height;

			if (h1 < h2)
				h1 = h2;


			// ����m_strValue
            /*
			size = g.MeasureString(this.m_strValue,
				font, 
				container.record.ValuePureWidth,
				new StringFormat());
             */
#if BIDI_SUPPORT
            string strValue = this.m_strValue.Replace(new string(Record.KERNEL_SUBFLD, 1), "\x200e" + new string(Record.KERNEL_SUBFLD, 1));
#endif
            size = TextRenderer.MeasureText(g,
#if BIDI_SUPPORT
                strValue == "" ? "lg" : strValue,
#else
                this.m_strValue == "" ? "lg" : this.m_strValue,
#endif
                font,
                new Size(container.record.ValuePureWidth, -1),
                MarcEditor.editflags);

			int h3 = (int)size.Height;

			if (h1 < h3)
				h1 = h3;

			// ע���������û��NameCaption�ĸ߶�
			/*
			// ����NameCaption
			size = g.MeasureString(this.strNameCaption,
				font, 
				container.NameCaptionPureWidth,
				new StringFormat());
			int h4 = (int)size.Height;

			if (h1 < h4)
				h1 = h4;
			*/

			if (this.container.MarcEditor.SelectedFieldIndices.Count == 1)
			{
				Field FocusedField = this.container.MarcEditor.FocusedField;
				// ��� bIgnoreEdit == true�������ǵ�ǰ��edit�ؼ��ļ��и߶�
				if (bIgnoreEdit == false
					&& FocusedField != null
					&& this == FocusedField) 
				{
					int h5 = this.container.MarcEditor.curEdit.Height;
					if (h1 < h5)
						h1 = h5;
				}
			}

			

			this.PureHeight = h1;
		}

		// �ѱ��л��Ƴ���
		// parameters:
		//		pe	��PaintEventArgs���������Graphics�����Ŀ����Ϊ������ClipRectangle��Ա������ʹ�����Ż�
		//		nBaseX	x����
		//		nBaseY	y����
		// return:
		//		void
		internal void Paint(PaintEventArgs pe, 
			int nBaseX, 
			int nBaseY)
		{

			// -----------------------------------------
			// ��������е��ܹ�����
			// ÿ�������а������ϵ��ߣ����������µ���
			Rectangle totalRect = new Rectangle(
				nBaseX,
				nBaseY,
				this.container.record.TotalLineWidth,
				this.TotalHeight);

            // �Ż�
			if (totalRect.IntersectsWith(pe.ClipRectangle )== false)
				return;


			// -----------------------------------------
			// ÿ����Ԫ��������ϵ��ߣ����������µ���

			// ��NameCaption
			Rectangle nameCaptionRect = new Rectangle(
				nBaseX,
				nBaseY,
				this.container.record.NameCaptionTotalWidth,
				this.TotalHeight);
			if (nameCaptionRect.IntersectsWith(pe.ClipRectangle )== true)
			{
				this.DrawCell(pe.Graphics,
					0,
					nameCaptionRect);
			}

			// ��Name
			Rectangle nameRect = new Rectangle(
				nBaseX + this.container.record.NameCaptionTotalWidth,
				nBaseY,
				this.container.record.NameTotalWidth,
				this.TotalHeight);
			if (nameRect.IntersectsWith(pe.ClipRectangle )== true)
			{
				this.DrawCell(pe.Graphics,
					1,
					nameRect);
			}

			// ��Indicator
			Rectangle indicatorRect = new Rectangle(
				nBaseX + this.container.record.NameCaptionTotalWidth + this.container.record.NameTotalWidth,
				nBaseY,
				this.container.record.IndicatorTotalWidth,
				this.TotalHeight);
			if (indicatorRect.IntersectsWith(pe.ClipRectangle )== true)
			{
				this.DrawCell(pe.Graphics,
					2,
					indicatorRect);
			}
			// ��m_strValue
			Rectangle valueRect = new Rectangle(
				nBaseX + this.container.record.NameCaptionTotalWidth + this.container.record.NameTotalWidth + this.container.record.IndicatorTotalWidth /*+ this.container.Indicator2TotalWidth*/,
				nBaseY,
				this.container.record.ValueTotalWidth,
				this.TotalHeight);
			if (valueRect.IntersectsWith(pe.ClipRectangle )== true)
			{
				this.DrawCell(pe.Graphics,
					3,
					valueRect);
			}
		}

		// ����Ԫ�񣬰������������� �� ��������
		// parameter:
		//		g	Graphics����
		//		nCol	�к� 
		//				0 �ֶ�˵��;
		//				1 �ֶ���;
		//				2 �ֶ�ָʾ�� 
		//				3 �ֶ�����
		//		rect	���� ���Ϊnull�����Զ������кż��� ��Ŀǰ��֧��
		// return:
		//		void
		internal void DrawCell(Graphics g,
			int nCol,
			Rectangle rect)
		{
			Debug.Assert(g != null,"g��������Ϊnull");

			string strText = "";
			Brush brush = null;
			int nWidth = 0;

            bool bEnabled = this.container.MarcEditor.Enabled;
            bool bReadOnly = this.container.MarcEditor.ReadOnly;

			if (nCol == 0)
			{
				// NameCaption

				Color backColor;

                if (bEnabled == false || bReadOnly == true)
                    backColor = SystemColors.Control;
                else
                    backColor = this.container.MarcEditor.defaultNameCaptionBackColor;


				// �������Ϊ��ǰ��У������Ʋ��ָ�����ʾ
				if (this.Selected == true)//this.container.marcEditor.CurField == this)
				{
                    if (backColor.GetBrightness() < 0.5f)
					    backColor = ControlPaint.Light(backColor);
                    else
                        backColor = ControlPaint.Dark(backColor);

				}

				strText = this.m_strNameCaption;
				nWidth = this.container.record.NameCaptionPureWidth;
				brush = new SolidBrush(backColor);
			}
			else if (nCol == 1)
			{
				// Name
                Color backColor;

                if (bEnabled == false || bReadOnly == true)
                    backColor = SystemColors.Control;
                else
                    backColor = this.container.MarcEditor.defaultNameBackColor;

				if (this.Name == "###")
					backColor = this.container.record.marcEditor.defaultNameCaptionBackColor;

				// �������Ϊ��ǰ��У������Ʋ��ָ�����ʾ
				if (this.Selected == true)//this.container.marcEditor.FocusedField == this)
				{
					backColor = ControlPaint.Light(backColor);
				}

				strText = this.m_strName;
				nWidth = this.container.record.NamePureWidth;
				brush = new SolidBrush(backColor);
			}
			else if (nCol == 2)
			{
				// Indicator
                Color backColor;

                if (bEnabled == false || bReadOnly == true)
                    backColor = SystemColors.Control;
                else
                    backColor = this.container.MarcEditor.defaultIndicatorBackColor;

				if (Record.IsControlFieldName(this.Name) == true)
					backColor = this.container.MarcEditor.defaultIndicatorBackColorDisabled;

				// �������Ϊ��ǰ��У������Ʋ��ָ�����ʾ
				if (this.Selected == true)//this.container.marcEditor.FocusedField == this)
				{
					backColor = ControlPaint.Light(backColor);
				}

				strText = this.m_strIndicator;
				nWidth = this.container.record.IndicatorPureWidth;
				brush = new SolidBrush(backColor);
			}			
			else if (nCol == 3)
			{
				// m_strValue
				strText = this.m_strValue;
				nWidth = this.container.record.ValuePureWidth + 0;  // 1Ϊ΢��,����!
                if (bEnabled == false || bReadOnly == true)
                    brush = new SolidBrush(SystemColors.Control);
                else
                    brush = new SolidBrush(this.container.MarcEditor.defaultContentBackColor);
			}
			else
			{
				Debug.Assert(false,"nCol��ֵ'" + Convert.ToString(nCol)+ "'���Ϸ�");
			}

//               new Point(-this.container.MarcEditor.DocumentOrgX + 0, -this.container.MarcEditor.DocumentOrgY + this.container.MarcEditor.DocumentHeight),
   //new Point(-this.container.MarcEditor.DocumentOrgX + this.container.MarcEditor.DocumentWidth, - this.container.MarcEditor.DocumentOrgY + 0),

            
            LinearGradientBrush linGrBrush = new LinearGradientBrush(
   new Point(0, 0),
   new Point(this.container.MarcEditor.DocumentWidth, 0),
   Color.FromArgb(255, 240, 240, 240),  // 240, 240, 240
   Color.FromArgb(255, 255, 255, 255)   // Opaque red
   );  // Opaque blue

            linGrBrush.GammaCorrection = true;



			// --------������----------------------------

            if ((nCol == 1 || nCol == 2 || nCol == 3)
                && (bEnabled == true && bReadOnly == false))
            {
                g.FillRectangle(linGrBrush, rect);
            }
            else
                g.FillRectangle(brush, rect);


			// --------������----------------------------

            // ֻ���ϣ���

			// ���Ϸ�������
			Field.DrawLines(g,
				rect,
				this.container.record.GridHorzLineHeight,
				0,
				0,
				0,
				this.container.record.marcEditor.defaultHorzGridColor);

			// ���󷽵�����
			int nGridWidth = 0;
			if (nCol == 1)
				nGridWidth = this.container.record.GridVertLineWidthForSplit;
			else
				nGridWidth = this.container.record.GridVertLineWidth;

            // indicator��ߵ����߶�һ��
            if (nCol == 2)
            {
                rect.Y += 2;
                rect.Height = this.container.record.NamePureHeight;
            }
	
			Field.DrawLines(g,
				rect,
				0,
				0,
				nGridWidth,//this.container.GridVertLineWidth,
				0,
				this.container.MarcEditor.defaultVertGridColor);

            if (nCol == 2)  // ��ԭ
            {
                rect.Y -= 2;
            }

			// --------������----------------------------

			if (nWidth > 0)
			{
				Rectangle textRect = new Rectangle(
					rect.X + nGridWidth/*this.container.GridVertLineWidth*/ + this.container.record.CellLeftBlank,
					rect.Y + this.container.record.GridHorzLineHeight + this.container.record.CellTopBlank,
					nWidth,
					this.PureHeight);

                Font font = null;
                if (nCol == 0)
                {
                    font = this.container.MarcEditor.CaptionFont;
                    Debug.Assert(font != null, "");
                }
                else if (nCol == 1 || nCol == 2)
                {
                    font = this.container.MarcEditor.FixedSizeFont;
                    Debug.Assert(font != null, "");
                }
                else 
                {
                    font = this.container.MarcEditor.Font;
                    Debug.Assert(font != null, "");
                }

                // System.Drawing.Text.TextRenderingHint oldrenderhint = g.TextRenderingHint;
                // g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                if (nCol == 0)  // �ֶ�����ʾ
                {
                    /*
                    StringFormat format = StringFormat.GenericDefault; //new StringFormat();
                    g.DrawString(strText,
                        font,
                        brush,	// System.Drawing.Brushes.Blue,
                        textRect,
                        format);
                     */

                    Color textcolor = this.container.MarcEditor.defaultNameCaptionTextColor;

                    if (this.Selected == true)
                    {
                        textcolor = ReverseColor(textcolor);
                    }

                    TextRenderer.DrawText(
                        g,
                        strText,
                        font,
                        textRect,
                        textcolor,
                        TextFormatFlags.EndEllipsis);

                }
                else if (nCol == 1)    // �ֶ���
                {
                    TextRenderer.DrawText(
                        g,
                        strText,
                        font,
                        textRect,
                        this.container.MarcEditor.defaultNameTextColor,
                        MarcEditor.editflags);  // TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                }
                else if (nCol == 2)    // ָʾ��
                {
                    TextRenderer.DrawText(
                        g,
                        strText,
                        font,
                        textRect,
                        this.container.MarcEditor.defaultIndicatorTextColor,
                        MarcEditor.editflags);  // TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                }
                else
                {   // ����
#if BIDI_SUPPORT
                    strText = strText.Replace(new string(Record.KERNEL_SUBFLD, 1), "\x200e" + new string(Record.KERNEL_SUBFLD, 1));
#endif
                    TextRenderer.DrawText(
                        g,
                        strText,
                        font,
                        textRect,
                        this.container.MarcEditor.m_contentTextColor,
                        MarcEditor.editflags);  // TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

                    /*
                    // ��ԭʼAPI����
                    DigitalPlatform.RECT rect0 = new RECT();
                    rect0.left = textRect.Left;
                    rect0.right = textRect.Right;
                    rect0.top = textRect.Top;
                    rect0.bottom = textRect.Bottom;
                    IntPtr hdc = g.GetHdc();

                    int oldbkmode = API.SetBkMode(hdc, API.TRANSPARENT);

                    // ����Ҫѡ�����壬�ʣ�ˢ�ӵȣ�
                    IntPtr oldfont = API.SelectObject(hdc, font.ToHfont());
                    API.DrawTextW(hdc,
                        strText,
                        strText.Length,
                        ref rect0,
                        API.DT_EDITCONTROL | API.DT_WORDBREAK);
                    API.SelectObject(hdc, oldfont);

                    API.SetBkMode(hdc, oldbkmode);

                    g.ReleaseHdc(hdc);
                     */
                }

                // g.TextRenderingHint = oldrenderhint;
			}

			//font.Dispose();
			brush.Dispose();
		}

        // ��÷�����ɫ
        static Color ReverseColor(Color color)
        {
            return Color.FromArgb(255-color.R, 255-color.G, 255-color.B);
        }
		
		// ������
		internal static void DrawLines(Graphics g,
			Rectangle myRect,
			int nTopBorderHeight,
			int nBottomBorderHeight,
			int nLeftBorderWidth,
			int nRightBorderWidth,
			Color color)
		{
			if (nTopBorderHeight < 0
				|| nBottomBorderHeight < 0
				|| nLeftBorderWidth < 0
				|| nRightBorderWidth < 0)
			{
				return;
			}

			if (nTopBorderHeight > myRect.Height 
				|| nBottomBorderHeight > myRect.Height)
			{
				return;
			}

			if (nLeftBorderWidth > myRect.Width
				|| nRightBorderWidth > myRect.Width)
			{
				return;
			}


			//��ߴ�ֱ�ֱ�
			Pen penLeft = new Pen(color,nLeftBorderWidth);
			//�ұߴ�ֱ�ֱ�
			Pen penRight = new Pen(color,nRightBorderWidth);
			//�Ϸ���ˮƽ�ֱ�
			Pen penTop = new Pen(color,nTopBorderHeight);
			//�·���ˮƽ�ֱ�
			Pen penBottom = new Pen(color,nBottomBorderHeight);

			int nLeftDelta = nLeftBorderWidth / 2;
			int nRightDelta = nRightBorderWidth / 2;
			int nTopDelta = nTopBorderHeight / 2;
			int nBottomDelta = nBottomBorderHeight / 2 ;

			int nLeftMode = nLeftBorderWidth % 2;
			int nRightMode = nRightBorderWidth % 2;
			int nTopMode = nTopBorderHeight % 2;
			int nBottomMode = nBottomBorderHeight % 2;

			Rectangle rectMiddle = new Rectangle(0,0,0,0);
			if (nTopBorderHeight == 0
				&& nBottomBorderHeight == 0
				&& nLeftBorderWidth == 0)
			{
				rectMiddle = new Rectangle(
					myRect.X,
					myRect.Y,
					myRect.Width - nRightDelta,
					myRect.Height);
			}
			else if (nLeftBorderWidth == 0
				&& nRightBorderWidth == 0
				&& nTopBorderHeight == 0)
			{
				rectMiddle = new Rectangle(
					myRect.X,
					myRect.Y,
					myRect.Width,
					myRect.Height - nBottomDelta);
			}
			else
			{
				rectMiddle = new Rectangle(
					myRect.X + nLeftDelta,
					myRect.Y + nTopDelta,
					myRect.Width  - nLeftDelta - nRightDelta,
					myRect.Height  - nTopDelta - nBottomDelta);

			}
		
			//�Ϸ�
			if (nTopBorderHeight > 0)
			{
				if (nLeftBorderWidth == 0
					&& nRightBorderWidth == 0
					&& nBottomBorderHeight == 0)
				{
					if (nTopBorderHeight == 1)
					{
						g.DrawLine(penTop,
							rectMiddle.Left ,rectMiddle.Top,
							rectMiddle.Right ,rectMiddle.Top);
					}
					else
					{
						g.DrawLine(penTop,
							rectMiddle.Left ,rectMiddle.Top ,
							rectMiddle.Right + 1,rectMiddle.Top );
					}
				}
				else
				{
					g.DrawLine(penTop,
						rectMiddle.Left ,rectMiddle.Top ,
						rectMiddle.Right ,rectMiddle.Top );
				}
			}

			//�·�
			if (nBottomBorderHeight > 0)
			{
				if (nLeftBorderWidth == 0
					&& nRightBorderWidth == 0
					&& nTopBorderHeight == 0)
				{
					if (nBottomBorderHeight == 1)
					{
						g.DrawLine(penBottom,
							rectMiddle.Left,rectMiddle.Bottom ,
							rectMiddle.Right -1,rectMiddle.Bottom);
					}
					else
					{
						g.DrawLine(penBottom,
							rectMiddle.Left,rectMiddle.Bottom - nBottomMode,
							rectMiddle.Right,rectMiddle.Bottom - nBottomMode);
					}
				}
				else
				{
					g.DrawLine(penBottom,
						rectMiddle.Left,rectMiddle.Bottom ,
						rectMiddle.Right,rectMiddle.Bottom);
				}
			}

			int nLeftTemp = nLeftDelta + nLeftMode;
			if (nLeftBorderWidth == 1)
			{
				if (nLeftMode == 0)
					nLeftTemp = nLeftDelta -1;
				else
					nLeftTemp = nLeftDelta;
			}
			//��
			if (nLeftBorderWidth > 0)
			{
				if (nTopBorderHeight == 0
					&& nBottomBorderHeight == 0
					&& nRightBorderWidth == 0)
				{
					if (nLeftBorderWidth == 1)
					{
						g.DrawLine (penRight,
							rectMiddle.Left ,rectMiddle.Top - nLeftDelta,
							rectMiddle.Left ,rectMiddle.Bottom);					}
					else
					{
						g.DrawLine (penLeft,
							rectMiddle.Left,rectMiddle.Top,
							rectMiddle.Left,rectMiddle.Bottom + 1);
					}
				}
				else
				{
					g.DrawLine (penLeft,
						rectMiddle.Left ,rectMiddle.Top,
						rectMiddle.Left ,rectMiddle.Bottom);
				}
			}

			int nRightTemp = nRightDelta + nRightMode;
			if (nRightBorderWidth == 1)
			{
				if (nRightMode == 0)
					nRightTemp = nRightDelta -1;
				else
					nRightTemp = nRightDelta;
			}
			//�ҷ�
			if (nRightBorderWidth > 0)
			{
				if (nTopBorderHeight == 0
					&& nBottomBorderHeight == 0
					&& nLeftBorderWidth == 0)
				{
					if (nRightBorderWidth == 1)
					{
						g.DrawLine (penRight,
							rectMiddle.Right ,rectMiddle.Top - nRightDelta,
							rectMiddle.Right ,rectMiddle.Bottom - 1);	
					}
					else
					{
						g.DrawLine(penRight,
							rectMiddle.Right -nRightMode ,rectMiddle.Top,
							rectMiddle.Right -nRightMode ,rectMiddle.Bottom);
					}
				}
				else
				{
					g.DrawLine (penRight,
						rectMiddle.Right ,rectMiddle.Top - nRightDelta,
						rectMiddle.Right ,rectMiddle.Bottom + nRightTemp);
				}
			}

			penLeft.Dispose ();
			penRight.Dispose ();
			penTop.Dispose ();
			penBottom.Dispose ();
		}

        // ���ֶμ���
        // ͨ��get�õ��ļ��ϣ�remove���е�subfield����field�в��ܶ��֡�
        // ��Ҫset�Ǹ�remove��ļ��ϻ��������ܶ���
        /// <summary>
        /// ��ȡ���������ֶζ��󼯺�
        /// </summary>
		public SubfieldCollection Subfields 
		{
			get 
			{
				if (Record.IsControlFieldName(this.m_strName) == true)
					return null;
				return SubfieldCollection.BuildSubfields(this);
			}
			set 
			{
                if (value != null)
                {
                    value.Container = this;

                    value.Flush();  // Flush()�бض��������this������
                }
			}
		}
	}
}
