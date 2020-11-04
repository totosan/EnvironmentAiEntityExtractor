using System;
using System.Collections.Generic;
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
    }
}