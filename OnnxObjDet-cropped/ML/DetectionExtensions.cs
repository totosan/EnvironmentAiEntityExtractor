using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using EntityExtractor.ML.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using EntityExtractor.Extensions;
using OnnxObjectDetection;
namespace EntityExtractor.ML
{
    public static class DetectionExtensions
    {
        public static List<NamedOnnxValue> GetInputsFromImage(this Image<Rgb24> image)
        {
            Tensor<float> input = new DenseTensor<float>(new[] { 1, 3, ImageNetSettings.imageHeight, ImageNetSettings.imageWidth });
            for (int y = 0; y < image.Height; y++)
            {
                Span<Rgb24> pixelSpan = image.GetPixelRowSpan(y);
                for (int x = 0; x < image.Width; x++)
                {
                    input[0, 0, y, x] = (pixelSpan[x].R);
                    input[0, 1, y, x] = (pixelSpan[x].G);
                    input[0, 2, y, x] = (pixelSpan[x].B);
                }
            }

            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", input) };
            return inputs;
        }

        public static float[] GetAsFloatArray(this IDisposableReadOnlyCollection<DisposableNamedOnnxValue> values)
        {
            var floatValues = values.First(x => x.Name == "model_outputs0").AsEnumerable<float>();
            return floatValues.ToArray();
        }
        public static string[] GetLabelResult(this List<BoundingBox> values)
        {
            return values.Select(l=>l.Label).ToArray();
        }
        public static float[][] GetBoxesResult(this List<BoundingBox> values)
        {
            return values.Select(b=>new float[]{b.Rect.X,b.Rect.Y,b.Rect.Right,b.Rect.Bottom}).ToArray();
        }
        public static float[] GetScoreResult(this List<BoundingBox> values)
        {
            return values.Select(x => x.Confidence).ToArray();
        }
    }
}