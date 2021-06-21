using System;

using System.IO;
using System.Linq;
using System.Text;
using EntityExtractor.ML.Helper;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace EntityExtractor.Extensions
{
    public static class ImagerExtensions2
    {
        public static byte[] Crop(this System.Drawing.Image image, Rectangle rect)
        {
            return image.Crop(rect.Width, rect.Height, rect.X, rect.Y);
        }

        public static byte[] Crop(this System.Drawing.Image image, int Width, int Height, int X, int Y)
        {
            try
            {
                using (System.Drawing.Image originalImage = image)
                {
                    using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(Width, Height))
                    {
                        bmp.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);
                        using (System.Drawing.Graphics Graphic = System.Drawing.Graphics.FromImage(bmp))
                        {
                            Graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                            Graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            Graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            Graphic.DrawImage(originalImage, new System.Drawing.Rectangle(0, 0, Width, Height), X, Y, Width, Height, System.Drawing.GraphicsUnit.Pixel);
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

        public static Rectangle ConvertToRect(this int[] rect)
        {
            //  center X [0] / center y [1] / height [2] / width [3]
            return new Rectangle((int)(rect[0] - rect[2] * 0.5), (int)(rect[1] - rect[3] * 0.5), rect[2], rect[3]);
        }
        public static RectangleF ConvertAndResizedToRectF(this float[] box, int tWidth, int tHeight)
        {
            box[0] = box[0] < 0.0 ? 0.0f : box[0] > 1.0 ? 0.99f : box[0];
            box[1] = box[1] < 0.0 ? 0.0f : box[1] > 1.0 ? 0.99f : box[1];
            box[2] = box[2] < 0.0 ? 0.0f : box[2] > 1.0 ? 0.99f : box[2];
            box[3] = box[3] < 0.0 ? 0.0f : box[3] > 1.0 ? 0.99f : box[3];

            var width = box[2] - box[0];
            var height = box[3] - box[1];
            var rect = new RectangleF(box[0] * tWidth, box[1] * tHeight, width * tWidth, height * tHeight);
            return rect;
        }

        public static Image SetMetaValue(this Image sourceBitmap, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (sourceBitmap.Metadata.ExifProfile == null)
                    sourceBitmap.Metadata.ExifProfile = new ExifProfile();
                sourceBitmap.Metadata.ExifProfile.SetValue(ExifTag.ImageDescription, value);
            }
            return sourceBitmap;
        }

        public static System.Drawing.Bitmap SetMetaValue(this System.Drawing.Bitmap sourceBitmap, MetaProperty property, string value)
        {
            System.Drawing.Imaging.PropertyItem prop = sourceBitmap.PropertyItems[0];
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

        public static System.Drawing.Image SetMetaValue(this System.Drawing.Image sourceImage, MetaProperty property, string value)
        {
            System.Drawing.Imaging.PropertyItem prop = sourceImage.PropertyItems[0];
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

        public static string GetMetaValue(this System.Drawing.Bitmap sourceBitmap, MetaProperty property)
        {
            System.Drawing.Imaging.PropertyItem[] propItems = sourceBitmap.PropertyItems;
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
        public static string GetMetaValue(this System.Drawing.Image sourceImage, MetaProperty property)
        {
            return ((System.Drawing.Bitmap)sourceImage).GetMetaValue(property);
        }
    }
}