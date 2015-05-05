using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Xml;
using System.Diagnostics;

using DigitalPlatform.Xml;

namespace DigitalPlatform.Marc
{
	public class MarcXmlWriter 
	{
		public bool WriteMarcPrefix = true;  //�Ƿ�дǰ׺
		public bool WriteXsi = false;         //�Ƿ�дxsi
		public string MarcPrefix = "marc";    //ǰ׺
		public string MarcNameSpaceUri = DigitalPlatform.Xml.Ns.unimarcxml;	// "http://www.loc.gov/MARC21/slim"; //marc�������ռ�

		private XmlTextWriter writer = null;   //XmlTextWriter����
		public Formatting m_Formatting = Formatting.None; //��ʽ
		public int m_Indentation  = 2;  //������


		public MarcXmlWriter()
		{
		}

		public MarcXmlWriter(Stream w,	
			Encoding encoding)// : base(w, encoding)
		{
			writer = new XmlTextWriter(w, encoding);
		}

		public MarcXmlWriter(string filename,
			Encoding encoding)// : base(filename, encoding)
		{
			writer = new XmlTextWriter(filename, encoding);
		}

		/*
		public void Test()
		{
			writer.BaseStream.Seek(999*1024*1024, SeekOrigin.Current);

		}
		*/

		//�ر�write
		public void Close()
		{
			if (writer != null)
				writer.Close();
		}

		public void Flush()
		{
			if (writer != null)
				writer.Flush();
		}

		public Formatting Formatting
		{
			get 
			{
				return m_Formatting;
			}
			set
			{
				m_Formatting = value;
				if (writer != null) 
				{
					writer.Formatting = value;
				}
			}
		}

		public int Indentation
		{
			get
			{
				return m_Indentation;
			}
			set
			{
				m_Indentation = value;
				if (writer != null)
				{
					writer.Indentation = m_Indentation;
				}
			}
		}

		//д��ͷ������:
		//<? xml version='1.0' encoding='utf-8'?>
		//collection��Ԫ�أ�����������ж��Ƿ�������ռ�
		public int WriteBegin()
		{
			writer.WriteStartDocument();

			if (WriteMarcPrefix == false)
				writer.WriteStartElement("", "collection", MarcNameSpaceUri);
			else
				writer.WriteStartElement(MarcPrefix,
					"collection", MarcNameSpaceUri);

            // dprms���ֿռ� 2010/11/15
            writer.WriteAttributeString("xmlns", "dprms", null, DpNs.dprms);


			if (WriteXsi == true) 
			{
                /* 2010/10/28
                writer.WriteAttributeString("xmlns:xsi",
					"http://www.w3.org/2001/XMLSchema-instance");
                 * �����÷������⡣����ɴ����ظ���nsuri���ڡ�Ӧ�ò���������÷���
                    writer.WriteAttributeString("xmlns", "dc", null,
    "http://purl.org/dc/elements/1.1/");
                 * */
                writer.WriteAttributeString("xmlns","xsi",null,
					"http://www.w3.org/2001/XMLSchema-instance");
				writer.WriteAttributeString("xsi","schemaLocation",null,
					"http://www.loc.gov/MARC21/slim http://www.loc.gov/standards/marcxml/schema/MARC21slim.xsd");
			}
			return 0;
		}

		//�ر�collection
		public int WriteEnd()
		{
			writer.WriteEndElement();
			return 0;
		}

		public int WriteRecord(
			string strMARC,
			out string strError)
		{
			strError = "";
			int nRet = 0;
			string [] saField = null;
			nRet = MarcUtil.ConvertMarcToFieldArray(strMARC,
				out saField,
				out strError);
			if (nRet == -1)
				return -1;
			return WriteRecord(
				saField,
				out strError);
		}

		// return:
		//		0	�ɹ�
		//		-1	����
		public int WriteRecord(
			string[] saField,
			out string strError)
		{
			string strFieldName = null;
			int nRet;

			strError = "";

			long lStart = writer.BaseStream.Position;
			Debug.Assert(writer.BaseStream.CanSeek == true, "writer.BaseStream.CanSeek != true");

			//try 
			//{
			//����WriteMarcPrefix��ֵ��ȷ���Ƿ��Ԫ��record�������ռ�
			if (WriteMarcPrefix == false)
				writer.WriteStartElement("record");
			else
				writer.WriteStartElement(MarcPrefix,
					"record", MarcNameSpaceUri);

            if (String.IsNullOrEmpty(writer.LookupPrefix("dprms")) == true)
            {
                // dprms���ֿռ� 2010/11/15
                writer.WriteAttributeString("xmlns", "dprms", null, DpNs.dprms);
            }

			//ѭ����дͷ������ÿ���Ӷ�
			for(int i=0;i<saField.Length;i++) 
			{
				string strLine = saField[i];
				string strInd1 = null;
				string strInd2 = null;
				string strContent = null;

				// ͷ����
				if (i == 0) 
				{
					//�������
					if (strLine.Length > 24)
						strLine = strLine.Substring(0,24);
					else 
					{
						while(strLine.Length < 24 ) 
						{
							strLine += " ";
						}
					}

					if (WriteMarcPrefix == false)
						writer.WriteElementString("leader", strLine);
					else
						writer.WriteElementString("leader", MarcNameSpaceUri, strLine);

					continue;
				}

				Debug.Assert(strLine != null, "");

				//���Ϸ����ֶ�,������
				if (strLine.Length < 3)
					continue;

				strFieldName = strLine.Substring(0,3);
				if (strLine.Length >= 3)
					strContent = strLine.Substring(3);
				else
					strContent = "";

				// control field  001-009û�����ֶ�
				if ( (String.Compare(strFieldName, "001") >= 0
					&& String.Compare(strFieldName, "009") <= 0 )
					|| String.Compare(strFieldName, "-01") == 0)
				{
					if (WriteMarcPrefix == false)
						writer.WriteStartElement("controlfield");
					else
						writer.WriteStartElement(MarcPrefix,
							"controlfield", MarcNameSpaceUri);


					writer.WriteAttributeString("tag", strFieldName);

					writer.WriteString(strContent);
					writer.WriteEndElement();
					continue;
				}

				if (strLine.Length == 3)
				{
					strInd1 = " ";
					strInd2 = " ";
					strContent = "";
				}
					//�ֶγ��ȵ���4�����,��������Ϊ�˷�ֹԽ��
				else if (strLine.Length == 4) 
				{
					strInd1 = strContent[0].ToString();
					strInd2 = " ";
					strContent = "";
				}
				else 
				{
					strInd1 = strContent[0].ToString();
					strInd2 = strContent[1].ToString();
					strContent = strContent.Substring(2);
				}

				// ��ͨ�ֶ�
				if (WriteMarcPrefix == false)
					writer.WriteStartElement("datafield");
				else
					writer.WriteStartElement(MarcPrefix,
						"datafield", MarcNameSpaceUri);

				writer.WriteAttributeString("tag", strFieldName);
				writer.WriteAttributeString("ind1", strInd1);
				writer.WriteAttributeString("ind2", strInd2);

				//�õ����ֶ�����
                /*
				string[] aSubfield = null;
				nRet = MarcUtil.GetSubfield(strContent,
					out aSubfield);
				if (nRet == -1)  //GetSubfield()����
				{
					continue;
				}
                 * */

                string[] aSubfield = strContent.Split(new char[] { (char)31 });
                if (aSubfield == null)
                {
                    // ��̫���ܷ���
                    continue;
                }


				//ѭ��д���ֶ�
				for(int j=0;j<aSubfield.Length;j++) 
				{
                    string strValue = aSubfield[j];
                    string strSubfieldName = "";
                    string strSubfieldContent = "";

                    if (j == 0)
                    {
                        // ��һ�����ַ���Ҫ������������ģ���������ʱ����������һ�������� 31 �ַ�
                        if (string.IsNullOrEmpty(aSubfield[0]) == true)
                            continue;
                        strSubfieldName = null; // ��ʾ���治Ҫ����code����
                        strSubfieldContent = strValue;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(strValue) == false)
                            strSubfieldName = strValue.Substring(0, 1);
                        if (string.IsNullOrEmpty(strValue) == false)
                            strSubfieldContent = strValue.Substring(1);
                    }

					if (WriteMarcPrefix == false)
						writer.WriteStartElement("subfield");
					else
						writer.WriteStartElement(MarcPrefix,
							"subfield", MarcNameSpaceUri);

                    if (strSubfieldName != null)
					    writer.WriteAttributeString("code", strSubfieldName);
                    writer.WriteString(strSubfieldContent); //ע�������Ƿ���Խ���Σ��
					writer.WriteEndElement();
				}

				writer.WriteEndElement();
			}

			writer.WriteEndElement();
			return 0;

			
			/*
			}

			catch (Exception ex)
			{
				//writer.BaseStream.Seek(lStart, SeekOrigin.Begin);
				writer.BaseStream.SetLength(lStart);
				writer.BaseStream.Seek(0, SeekOrigin.End);

				strError = ex.Message;
				return -1;
			}
			*/
			


		}

	}


}
