using System;
using System.Collections.Generic;
using System.Linq;
using General;
using Images;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using ML.Data;
using ML.Helper;
using SixLabors.ImageSharp.PixelFormats;

namespace ML
{

    public class OnnxModelScorer
    {
        private string imagesFolder;
        private InferenceSession session;

        public OnnxModelScorer(string modelPath)
        {
            this.session = new InferenceSession(modelPath);
        }

        private ImageNetPrediction PredictDataUsingModel(Imager imgr)
        {
            Console.WriteLine($"Images location: {imagesFolder}");
            Console.WriteLine("");
            Console.WriteLine("=====Identify the objects in the images=====");
            Console.WriteLine("");

            imgr.Resize(SixLabors.ImageSharp.Processing.ResizeMode.Stretch);
            Tensor<float> input = new DenseTensor<float>(new[] { 1, 3, ImageNetSettings.imageHeight, ImageNetSettings.imageWidth });
            var mean = new[] { 0.485f, 0.456f, 0.406f };
            var stddev = new[] { 0.229f, 0.224f, 0.225f };
            for (int y = 0; y < imgr.AsImage.Height; y++)
            {
                Span<Rgb24> pixelSpan = imgr.AsImage.GetPixelRowSpan(y);
                for (int x = 0; x < imgr.AsImage.Width; x++)
                {
                    input[0, 0, y, x] = (pixelSpan[x].R);
                    input[0, 1, y, x] = (pixelSpan[x].G);
                    input[0, 2, y, x] = (pixelSpan[x].B);
                }
            }

            var inputs = new List<NamedOnnxValue>
                            {
                                NamedOnnxValue.CreateFromTensor("data", input)
                            };
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);

            ImageNetPrediction resultDict = new ImageNetPrediction();
            resultDict.PredictedLabels = results.First(x => x.Name == "detected_classes").AsTensor<Int64>().ToArray();
            var boxes = results.First(x => x.Name == "detected_boxes").AsTensor<Single>().ToArray();
            var convertedBoxes = boxes.Split(4).Select(x => x.ToArray()).ToArray();
            resultDict.PredictedBoxes = convertedBoxes;
            resultDict.PredictedScores = results.First(x => x.Name == "detected_scores").AsTensor<float>().ToArray();
            return resultDict;
        }

        public Imager RunDetection(string imagePath)
        {
            var imager = new Imager(imagePath);
            var prediction = PredictDataUsingModel(imager);
            var i = 0;
            var best = (from p in prediction.PredictedScores
                        select new { Index = i++, Prediction = p }).OrderByDescending(p => p.Prediction).Take(10);
            imager.Boxes = prediction.PredictedBoxes.Take(10).ToArray();

            return imager;
        }
    }
}