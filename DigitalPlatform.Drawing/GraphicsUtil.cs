#if NO


using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;


namespace DigitalPlatform.Drawing
{
	public class GraphicsUtil
	{
        // ����Font���õ��ַ����Ŀ��
        // parameter:
        //		font	Font����
        //		strText	�ַ���
        // return:
        //		�ַ����Ŀ��
        public static int GetWidth(
            Font font,
            string strText)
        {
            Size proposedSize = new Size(60000, 1000);
            Size size = TextRenderer.MeasureText(strText,
                font,
                proposedSize,
                TextFormatFlags.SingleLine | TextFormatFlags.TextBoxControl);
            return size.Width;
        }

		// ����Font���õ��ַ����Ŀ��
		// parameter:
		//		g	Graphics����
		//		font	Font����
		//		strText	�ַ���
		// return:
		//		�ַ����Ŀ��
		public static int GetWidth(Graphics g,
			Font font,
			string strText)
		{
			SizeF sizef = new SizeF();
			sizef = g.MeasureString(
				strText,
				font);
			return sizef.ToSize().Width + 6;    // ΢��
		}

        // ��Сͼ��
        // parameters:
        //		nNewWidth0	���(0��ʾ���仯)
        //		nNewHeight0	�߶�
        //      bRatio  �Ƿ񱣳��ݺ����
        // return:
        //      -1  ����
        //      0   û�б�Ҫ����(objBitmapδ����)
        //      1   �Ѿ�����
        public static int ShrinkPic(ref Image objBitmap,
            int nNewWidth0,
            int nNewHeight0,
            bool bRatio,
            out string strError)
        {
            strError = "";

            int nNewWidth = nNewWidth0;
            int nNewHeight = nNewHeight0;

            // ����Ҫ����
            if (nNewHeight0 == 0 && nNewWidth0 == 0)
            {
                return 0;
            }

            if (nNewWidth == 0 && nNewHeight == 0) // ���߶�������
                goto NONEED;
            else if (nNewWidth == 0) // ��Ȳ�����
            {
                if (objBitmap.Height <= nNewHeight)
                    goto NONEED;
                float ratio = (float)nNewHeight / (float)objBitmap.Height;

                nNewWidth = (int)(ratio * (float)objBitmap.Width);
                if (bRatio == true)
                    nNewHeight = (int)(ratio * (float)objBitmap.Height);
            }
            else if (nNewHeight == 0)	// �߶Ȳ�����
            {
                if (objBitmap.Width <= nNewWidth)
                    goto NONEED;

                float ratio = (float)nNewWidth / (float)objBitmap.Width;
                nNewHeight = (int)(ratio * (float)objBitmap.Height);
                if (bRatio == true)
                    nNewWidth = (int)(ratio * (float)objBitmap.Width);	// ��������ݺ����
            }
            else // ��ȸ߶ȶ�����
            {
                float wratio = 1.0F;
                float hratio = 1.0F;

                if (objBitmap.Height > nNewHeight)
                {
                    hratio = (float)nNewHeight / (float)objBitmap.Height;
                }
                if (objBitmap.Width > nNewWidth)
                {
                    wratio = (float)nNewWidth / (float)objBitmap.Width;
                }

                if (bRatio == true)
                {
                    float ratio = Math.Min(wratio, hratio);

                    if (ratio != 1.0)
                    {
                        nNewHeight = (int)(ratio * (float)objBitmap.Height);
                        nNewWidth = (int)(ratio * (float)objBitmap.Width);
                    }
                    else
                    {
                        nNewHeight = objBitmap.Height;
                        nNewWidth = objBitmap.Width;
                    }
                }
                else
                {
                    nNewHeight = (int)(hratio * (float)objBitmap.Height);
                    nNewWidth = (int)(wratio * (float)objBitmap.Width);
                }
            }

            Bitmap BitmapDest = new Bitmap(nNewWidth, nNewHeight);

            Graphics objGraphics = Graphics.FromImage(BitmapDest);
            Rectangle compressionRectangle = new Rectangle(0,
                0, nNewWidth, nNewHeight);

            /*
            using (Brush trans_brush = new SolidBrush(Color.White))
            {
                objGraphics.FillRectangle(trans_brush, compressionRectangle);
            }
             * */

            // set Drawing Quality 
            objGraphics.InterpolationMode = InterpolationMode.High;
            objGraphics.DrawImage(objBitmap, compressionRectangle);

            objBitmap.Dispose();
            objBitmap = BitmapDest;
            return 1;	//  �ɹ�����
        ERROR1:
            return -1;
        NONEED:
            return 0;
        }

        // ��Сͼ��
        // parameters:
		//      oFile   ������ļ���
		//      strContentType  �ļ������ַ���
		//		nNewWidth0	���(0��ʾ���仯)
		//		nNewHeight	�߶�
		//      oTargetFile:Ŀ����
        // return:
        //      -1  ����
        //      0   û�б�Ҫ����(oTargetδ����)
        //      1   �Ѿ�����
        public static int ShrinkPic(Stream oFile,
            string strContentType,
            int nNewWidth0,
            int nNewHeight0,
            bool bRatio,
            Stream oTargetFile,
            out string strError)
        {
            strError = "";

            int nNewWidth = nNewWidth0;
            int nNewHeight = nNewHeight0;

            // ����Ҫ����
            if (nNewHeight0 == 0 && nNewWidth0 == 0)
            {
                return 0;
            }

            Bitmap objBitmap = null;

            try
            {
                objBitmap = new Bitmap(oFile);
            }
            catch (Exception ex)
            {
                strError = "����bitmap����: " + ex.Message;
                goto ERROR1;
            }

            if (nNewWidth == 0 && nNewHeight == 0) // ���߶�������
                goto NONEED;
            else if (nNewWidth == 0) // ��Ȳ�����
            {
                if (objBitmap.Height <= nNewHeight)
                    goto NONEED;
                float ratio = (float)nNewHeight / (float)objBitmap.Height;

                nNewWidth = (int)(ratio * (float)objBitmap.Width);
                if (bRatio == true)
                    nNewHeight = (int)(ratio * (float)objBitmap.Height);
            }
            else if (nNewHeight == 0)	// �߶Ȳ�����
            {
                if (objBitmap.Width <= nNewWidth)
                    goto NONEED;

                float ratio = (float)nNewWidth / (float)objBitmap.Width;
                nNewHeight = (int)(ratio * (float)objBitmap.Height);
                if (bRatio == true)
                    nNewWidth = (int)(ratio * (float)objBitmap.Width);	// ��������ݺ����
            }
            else // ��ȸ߶ȶ�����
            {
                float wratio = 1.0F;
                float hratio = 1.0F;

                if (objBitmap.Height > nNewHeight)
                {
                    hratio = (float)nNewHeight / (float)objBitmap.Height;
                }
                if (objBitmap.Width > nNewWidth)
                {
                    wratio = (float)nNewWidth / (float)objBitmap.Width;
                }

                if (bRatio == true)
                {
                    float ratio = Math.Min(wratio, hratio);

                    if (ratio != 1.0)
                    {
                        nNewHeight = (int)(ratio * (float)objBitmap.Height);
                        nNewWidth = (int)(ratio * (float)objBitmap.Width);
                    }
                    else
                    {
                        goto NONEED;    // 2012/5/23
                        // nNewHeight = objBitmap.Height;
                        // nNewWidth = objBitmap.Width;
                    }
                }
                else
                {
                    nNewHeight = (int)(hratio * (float)objBitmap.Height);
                    nNewWidth = (int)(wratio * (float)objBitmap.Width);
                }
            }

            Bitmap BitmapDest = new Bitmap(nNewWidth, nNewHeight);

            Graphics objGraphics = Graphics.FromImage(BitmapDest);
            Rectangle compressionRectangle = new Rectangle(0,
                0, nNewWidth, nNewHeight);

            // set Drawing Quality 
            objGraphics.InterpolationMode = InterpolationMode.High;
            objGraphics.DrawImage(objBitmap, compressionRectangle);

            try
            {
                BitmapDest.Save(
                    oTargetFile,
                    GetImageType(strContentType));	// System.drawing.Imaging.ImageFormat.Jpeg)
            }
            catch (Exception ex)
            {
                BitmapDest.Dispose();
                objBitmap.Dispose();
                // 2010/12/29 add
                strError = "BitmapDest.Save()�׳��쳣 strContentType='"+strContentType+"' : " + ExceptionUtil.GetDebugText(ex);
                goto ERROR1;
            }

            BitmapDest.Dispose();
            objBitmap.Dispose();
            return 1;	//  �ɹ�����
        ERROR1:
            if (objBitmap != null)
                objBitmap.Dispose();
            return -1;
        NONEED:
            if (objBitmap != null)
                objBitmap.Dispose();
            return 0;

        }

	
		static System.Drawing.Imaging.ImageFormat GetImageType(string strContentType)
		{
			strContentType = strContentType.ToString().ToLower();

			switch (strContentType)
			{
				case "image/pjpeg":
					return System.Drawing.Imaging.ImageFormat.Jpeg;
                case "image/jpeg":
                    return System.Drawing.Imaging.ImageFormat.Jpeg;
				case "image/gif":
					return System.Drawing.Imaging.ImageFormat.Gif;
				case "image/bmp":
					return System.Drawing.Imaging.ImageFormat.Bmp;
				case "image/tiff":
					return System.Drawing.Imaging.ImageFormat.Tiff;
				case "image/x-icon":
					return System.Drawing.Imaging.ImageFormat.Icon;
				case "image/x-png":
                case "image/png":
                    return System.Drawing.Imaging.ImageFormat.Png;
				case "image/x-emf":
					return System.Drawing.Imaging.ImageFormat.Emf;
				case "image/x-exif":
					return System.Drawing.Imaging.ImageFormat.Exif;
				case "image/x-wmf":
					return System.Drawing.Imaging.ImageFormat.Wmf;
			}
			return System.Drawing.Imaging.ImageFormat.MemoryBmp;
		}

	}

}

#endif