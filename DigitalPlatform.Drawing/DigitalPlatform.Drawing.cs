using System;
using System.Net;
using System.Drawing;
using System.IO;

namespace DigitalPlatform.Drawing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class DrawingUtil
	{

		public static MemoryStream MakeTextPic(
			string strText,
			string strFace,
			int nSize,
			Color colorBack)
		{
            SizeF size;
            Font font = null;

			Bitmap bitmapTemp = new Bitmap(1,1);

            using (bitmapTemp)
            {
                Graphics graphicsTemp = Graphics.FromImage(bitmapTemp);

                font = new Font(strFace, nSize, FontStyle.Bold);
                size = graphicsTemp.MeasureString(
                    strText,
                    font);
                size.Height = (int)((double)size.Height * 1.5F);
                size.Width = (int)((double)size.Width * 1.2F);

            }
			// bitmapTemp.Dispose();

            MemoryStream stream = null;

			// ��ʽ��ͼ��
			Bitmap bitmapDest = new Bitmap((int)size.Width, (int)size.Height);

            using (bitmapDest)
            {

                Graphics objGraphics = Graphics.FromImage(bitmapDest);

                objGraphics.Clear(colorBack/*Color.DarkGray*/);// Color.Teal

                objGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

                StringFormat stringFormat = new StringFormat();


                // �������һ����б�Ƕ�
                Random random = new Random(unchecked((int)DateTime.Now.Ticks));
                int angle = random.Next(-10, 10);

                objGraphics.RotateTransform(angle);

                stringFormat.Alignment = StringAlignment.Near;
                if (angle > 0)
                    stringFormat.LineAlignment = StringAlignment.Near;
                else
                    stringFormat.LineAlignment = StringAlignment.Far;



                // Color.FromArgb(128, 100, 100, 100)
                SolidBrush objBrush = new SolidBrush(Color.Black); // ͸����ɫ ' Color.Black
                RectangleF rect = new RectangleF(0, 0, size.Width, size.Height);
                objGraphics.DrawString(strText,
                    font,
                    objBrush,
                    rect,
                    stringFormat);

                stream = new MemoryStream();

                bitmapDest.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
			// bitmapDest.Dispose();

			return stream;
		}

		public static int MakeTextPic(
			string strText,
			string strFace,
			int nSize,
			string strOutputFileName)
		{
			Bitmap bitmapTemp = new Bitmap(1,1);
			Graphics graphicsTemp = Graphics.FromImage(bitmapTemp);

			Font font = new Font(strFace, nSize, FontStyle.Bold);
			SizeF size = graphicsTemp.MeasureString(
				strText,
				font);

			bitmapTemp.Dispose();

			// ��ʽ��ͼ��
			Bitmap bitmapDest = new Bitmap((int)size.Width, (int)size.Height);

			Graphics objGraphics = Graphics.FromImage(bitmapDest);

 
			objGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
 
			StringFormat stringFormat = new StringFormat();

			stringFormat.Alignment = StringAlignment.Near;
			stringFormat.LineAlignment = StringAlignment.Near;
 
			SolidBrush objBrush= new SolidBrush( Color.FromArgb(128, 100, 100, 100)); // ͸����ɫ ' Color.Black
			RectangleF rect = new RectangleF(0,0, size.Width, size.Height);
			objGraphics.DrawString(strText,
				font, 
				objBrush,
				rect,
				stringFormat);
 
			bitmapDest.Save(strOutputFileName, System.Drawing.Imaging.ImageFormat.Jpeg);
			bitmapDest.Dispose();

			return 0;
		}


        // ��URI��ȡͼ�����
        // parameters:
        //      strUrl  ͼ��·��
        //      image   out����������Image����
        //      strError    out���������س�����Ϣ
        // return:
        //      -1  ����
        //      0   �ɹ�
        public static int GetImageFormUrl(string strUrl,
            out Image image,
            out string strError)
        {
            strError = "";
            image = null;

            try
            {
                // ͨ��URL���ͼ���ļ��Ĺ��̿����Ż��� ����ΪEditor����һ���ļ�cache
                WebRequest request = WebRequest.Create(strUrl);
                WebResponse response = request.GetResponse();

                // Create image.
                image = Image.FromStream(response.GetResponseStream());//.FromFile("SampImag.jpg");
                return 0;
            }
            catch(Exception ex)
            {
                strError = "��'" + strUrl + "'��ȡͼ�����ԭ��" + ex.Message;
                return -1;
            }
        }
	}
}
