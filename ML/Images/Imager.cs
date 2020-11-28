using System;
using System.Collections.Generic;
using System.IO;
using EntityExtractor.Extensions;
using EntityExtractor.ML.Data;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace EntityExtractor.Images
{
    public class Imager
    {
        public Image<Rgb24> AsImage { get; set; }
        public Image<Rgb24> AsOriginalImage { get; set; }
        public Rectangle Rect { get; set; }

        public ImageNetPrediction DetectionResults { get; set; }
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
            this.AsOriginalImage = this.AsImage.Clone();
        }
        public void MarkDetectionsInImage()
        {
            int i = 0;
            foreach (var box in this.DetectionResults.PredictedBoxes)
            {
                var width = box[2] - box[0];
                var height = box[3] - box[1];
                var rect = new RectangleF(box[0] * this.AsImage.Width, box[1] * this.AsImage.Height, width * this.AsImage.Width, height * this.AsImage.Height);
                this.AsImage.Mutate(x =>
                {
                    x.Draw(Rgba32.ParseHex("2222dd"), 2, rect);
                    FontCollection collection = new FontCollection();
                    FontFamily family = collection.Install("C:\\Program Files\\Microsoft Office\\root\\vfs\\Fonts\\private\\ARIALN.TTF");
                    Font font = family.CreateFont(12, FontStyle.Regular);
                    var text = $"{this.DetectionResults.PredictedLabels[i].ToString()} {this.DetectionResults.PredictedScores[i].ToString("p")}";
                    x.DrawText(text, font, Color.AliceBlue, new PointF(rect.X, rect.Y));
                    i++;
                });
            }
            this.AsImage.SetMetaValue(string.Join(',', this.DetectionResults.PredictedLabels));
        }

        public Dictionary<string, List<Image<Rgb24>>> GetDetectionsAsImages()
        {
            if (this.DetectionResults.PredictedBoxes == null || this.DetectionResults.PredictedBoxes.Length == 0)
                return null;
            var lib = new Dictionary<string, List<Image<Rgb24>>>();
            int i = 0;

            foreach (var box in this.DetectionResults.PredictedBoxes)
            {
                string label = this.DetectionResults.PredictedLabels[i].ToString();
                if (!lib.ContainsKey(label))
                {
                    lib.Add(label, new List<Image<Rgb24>>());
                }

                lib[label].Add(CloneCropped(box.ConvertResizedToRect(this.AsImage.Width, this.AsImage.Height)));
            }
            return lib;
        }
        public void Save(string path)
        {
            this.AsImage.SaveAsJpeg(path);
        }
        public void CropSquared()
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

        public Image<Rgb24> CloneCropped(RectangleF rect)
        {
            return this.AsImage.Clone(x => x.Crop((Rectangle)rect));
        }
    }
}