using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace Images
{
    public class Imager
    {
        public Image Image { get; }
        public Rectangle Rect { get; set; }

        public Imager(string path)
        {
            if (!File.Exists(path))
            {
                throw new System.Exception("File not exists");
            }

            this.Image = GetBitmapFromPath(path);
        }

        private Image GetBitmapFromPath(string path)
        {
            return Bitmap.FromFile(path);
        }

        public byte[] CropAndConvertToBytes(){
            return this.Image.Crop(Rect);
        }
    }
}