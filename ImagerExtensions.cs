using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

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
    }
}