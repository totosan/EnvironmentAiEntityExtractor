using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EntityExtractor.Images;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using EntityExtractor.ML.Data;
using EntityExtractor.ML.Helper;
using SixLabors.ImageSharp.PixelFormats;
using EntityExtractor;
using EntityExtractor.ML.Interfaces;

namespace EntityExtractor.ML
{
    public class OnnxModelScorer: IModelScorer
    {
        private string imagesFolder;

        public string[] Lables { get; }

        private InferenceSession session;

        public OnnxModelScorer(string modelPath, string labelPath)
        {
            this.Lables = File.ReadLines(labelPath).ToArray();
            this.session = new InferenceSession(modelPath);
        }

        private ImageNetPrediction PredictDataUsingModel(Imager imgr)
        {
            Console.WriteLine($"Images location: {imgr.PathOfFile}");
            Console.WriteLine("");
            Console.WriteLine("=====Identify the objects in the images=====");
            Console.WriteLine("");

            imgr.CropSquared();
            //imgr.Resize(SixLabors.ImageSharp.Processing.ResizeMode.Stretch);
            var inputs = imgr.AsImage.GetInputsFromImage();
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);

            ImageNetPrediction resultDict = new ImageNetPrediction();
            resultDict.PredictedLabels = results.GetLabelResult(this.Lables);
            resultDict.PredictedBoxes = results.GetBoxesResult();
            resultDict.PredictedScores = results.GetScoreResult();
            return resultDict;
        }

        public Imager RunDetection(Imager imager)
        {
            var prediction = PredictDataUsingModel(imager);
            imager.DetectionResults = prediction.GetBestResults(5, 0.2f);
            if (!imager.DetectionResults.IsEmpty)
                Console.WriteLine($"{imager.DetectionResults.PredictedLabels[0]} -> {imager.DetectionResults.PredictedScores[0]}");
            return imager;
        }
    }
}