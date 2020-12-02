using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace Images
{
    public static class ImagerExtensions
    {
        public static byte[] Crop(this Image image, Rectangle rect){
            return image.Crop(rect.Width,rect.Height,rect.X,rect.Y);
        }

        public static byte[] Crop(this Image image, int Width, int Height, int X, int Y)
        {
            try
            {
                using (Image originalImage = image)
                {
                    using (Bitmap bmp = new Bitmap(Width, Height))
                    {
                        bmp.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);
                        using (Graphics Graphic = Graphics.FromImage(bmp))
                        {
                            Graphic.SmoothingMode = SmoothingMode.AntiAlias;
                            Graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            Graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            Graphic.DrawImage(originalImage, new Rectangle(0, 0, Width, Height), X, Y, Width, Height, GraphicsUnit.Pixel);
                            MemoryStream ms = new MemoryStream();
                            bmp.Save(ms, originalImage.RawFormat);
                            return ms.GetBuffer();
                        }
                    }
                }
            }
            catch (System.Exception Ex)
            {
                throw (Ex);
            }
        }

        public static Rectangle ConvertToRect(this int[] rect){
            //  center X [0] / center y [1] / height [2] / width [3]
            return new Rectangle((int)(rect[0]-rect[2]*0.5),(int)(rect[1]-rect[3]*0.5),rect[2],rect[3]);
        }
    
        public static Bitmap SetMetaValue(this Bitmap sourceBitmap, MetaProperty property, string value)  
        {  
            PropertyItem prop = sourceBitmap.PropertyItems[0];  
            int iLen = value.Length + 1;  
            byte[] bTxt = new Byte[iLen];  
            for (int i = 0; i < iLen - 1; i++)  
                bTxt[i] = (byte)value[i];  
            bTxt[iLen - 1] = 0x00;  
            prop.Id = (int)property;  
            prop.Type = 2;  
            prop.Value = bTxt;  
            prop.Len = iLen;  
            sourceBitmap.SetPropertyItem(prop);  
            return sourceBitmap;  
        }  

        public static Image SetMetaValue(this Image sourceImage, MetaProperty property, string value)  
        {  
            PropertyItem prop = sourceImage.PropertyItems[0];  
            int iLen = value.Length + 1;  
            byte[] bTxt = new Byte[iLen];  
            for (int i = 0; i < iLen - 1; i++)  
                bTxt[i] = (byte)value[i];  
            bTxt[iLen - 1] = 0x00;  
            prop.Id = (int)property;  
            prop.Type = 2;  
            prop.Value = bTxt;  
            prop.Len = iLen;  
            sourceImage.SetPropertyItem(prop);  
            return sourceImage;  
        } 

        public static string GetMetaValue(this Bitmap sourceBitmap, MetaProperty property)  
        {  
            PropertyItem[] propItems = sourceBitmap.PropertyItems;  
            var prop = propItems.FirstOrDefault(p => p.Id == (int)property);  
            if (prop != null)  
            {  
                return Encoding.UTF8.GetString(prop.Value);  
            }  
            else  
            {  
                return null;  
            }  
        }  
        public static string GetMetaValue(this Image sourceImage, MetaProperty property)  
        {  
            return ((Bitmap)sourceImage).GetMetaValue(property);
        } 
    }
}