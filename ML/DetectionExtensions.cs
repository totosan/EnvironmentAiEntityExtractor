using System;
using System.Collections.Generic;
using System.Linq;
using General;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using ML.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace EntityExtractor
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

        public static string[] GetLabelResult(this IDisposableReadOnlyCollection<DisposableNamedOnnxValue> values, string[] labels)
        {
            var labelNumbers = values.First(x => x.Name == "detected_classes").AsTensor<Int64>();
            return labelNumbers.Select(l=>labels[l]).ToArray();
        }
        public static float[][] GetBoxesResult(this IDisposableReadOnlyCollection<DisposableNamedOnnxValue> values)
        {
            var boxes = values.First(x => x.Name == "detected_boxes").AsTensor<Single>().ToArray();
            var convertedBoxes = boxes.Split(4).Select(x => x.ToArray()).ToArray();
            return convertedBoxes;
        }
        public static float[] GetScoreResult(this IDisposableReadOnlyCollection<DisposableNamedOnnxValue> values)
        {
            return values.First(x => x.Name == "detected_scores").AsTensor<float>().ToArray();
        }
    }
}