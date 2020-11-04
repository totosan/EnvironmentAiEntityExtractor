using System;
using System.Collections.Generic;
using System.IO;
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
using EntityExtractor;

namespace ML
{

    public class OnnxModelScorer
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

            imgr.Resize(SixLabors.ImageSharp.Processing.ResizeMode.Stretch);
            var inputs = imgr.AsImage.GetInputsFromImage();
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);

            ImageNetPrediction resultDict = new ImageNetPrediction();
            resultDict.PredictedLabels = results.GetLabelResult(this.Lables);
            resultDict.PredictedBoxes = results.GetBoxesResult();
            resultDict.PredictedScores = results.GetScoreResult();
            return resultDict;
        }

        public Imager RunDetection(string imagePath)
        {
            var imager = new Imager(imagePath);
            var prediction = PredictDataUsingModel(imager);
            imager.DetectionResults = prediction.GetBestResults(3);
            if (!imager.DetectionResults.IsEmpty)
                Console.WriteLine($"{imager.DetectionResults.PredictedLabels[0]} -> {imager.DetectionResults.PredictedScores[0]}");
            return imager;
        }
    }
}