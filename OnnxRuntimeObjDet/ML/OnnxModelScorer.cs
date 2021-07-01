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
using OnnxObjectDetection;
namespace EntityExtractor.ML
{
    public class OnnxModelScorer : IModelScorer
    {

        public string[] Lables { get; }
        public float Confidence { get; set; }

        private InferenceSession session;

        public OnnxModelScorer(string modelPath, string labelPath)
        {
            this.Lables = File.ReadLines(labelPath).ToArray();
            this.session = new InferenceSession(modelPath);

        }

        private ImageNetPrediction PredictDataUsingModel(Imager imgr)
        {
            //imgr.Resize(SixLabors.ImageSharp.Processing.ResizeMode.BoxPad);
            imgr.CropSquared();
            //imgr.Resize(SixLabors.ImageSharp.Processing.ResizeMode.Stretch);
            var inputs = imgr.AsImage.GetInputsFromImage();
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);

            OnnxOutputParser outputParser = new OnnxOutputParser(new CustomVisionModel("ML\\TomowArea_iter4.ONNX\\"));
            var values = results.GetAsFloatArray();
            var bboxes= outputParser.ParseOutputs(values, 0.1f);

            ImageNetPrediction resultDict = new ImageNetPrediction();
            resultDict.PredictedBoxes = bboxes.GetBoxesResult();
            resultDict.PredictedLabels = bboxes.GetLabelResult();
            resultDict.PredictedScores = bboxes.GetScoreResult();
            return resultDict;
        }

        public Imager RunDetection(Imager imager)
        {
            var prediction = PredictDataUsingModel(imager);
            imager.DetectionResults = prediction.GetBestResults(5, 0.1f);
            //            if (!imager.DetectionResults.IsEmpty)
            //                Console.WriteLine($"{imager.DetectionResults.PredictedLabels[0]} -> {imager.DetectionResults.PredictedScores[0]}");
            return imager;
        }
    }
}