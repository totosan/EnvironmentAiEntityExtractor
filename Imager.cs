using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Images
{
    public class Imager
    {
        public Image<Rgb24> AsImage { get; set; }
        public Rectangle Rect { get; set; }

        public float[][] Boxes { get; set; }

        public string PathOfFile { get; set; }

        public IImageFormat ImageFormat { get => imageFormat; }
        private IImageFormat imageFormat;

        public Imager(string path)
        {
            if (!File.Exists(path))
            {
                throw new System.Exception("File not exists");
            }
            this.PathOfFile = path;
            this.AsImage = Image.Load<Rgb24>(path, out imageFormat);
        }

        public void DrawDetections()
        {

            foreach (var box in Boxes)
            {
                var width = box[2] - box[0];
                var height = box[3] - box[1];
                var rect = new RectangleF(box[0] * this.AsImage.Width, box[1] * this.AsImage.Height, width * this.AsImage.Width, height * this.AsImage.Height);
                this.AsImage.Mutate(x => x.Draw(
                    Rgba32.ParseHex("ff22dd"),
                    2,
                    rect));
            }

        }

        public void Save(string path)
        {
            this.AsImage.SaveAsJpeg(path);
        }
        public void CropAndResize()
        {
            this.AsImage.Mutate(x =>
                                        {
                                            x.Resize(new ResizeOptions
                                            {
                                                Size = new Size(ML.Data.ImageNetSettings.imageWidth, ML.Data.ImageNetSettings.imageHeight),
                                                Mode = ResizeMode.Crop
                                            });
                                        });
        }

        public void Resize(ResizeMode mode)
        {
            this.AsImage.Mutate(x =>
                                        {
                                            x.Resize(new ResizeOptions
                                            {
                                                Size = new Size(ML.Data.ImageNetSettings.imageWidth, ML.Data.ImageNetSettings.imageHeight),
                                                Mode = mode
                                            });
                                        });
        }
    }
}