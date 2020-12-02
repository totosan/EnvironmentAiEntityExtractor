using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using ML.Data;
using ML.Helper;

namespace ML
{

    public class OnnxModelScorer
    {
        public string Path { get; }
        private readonly MLContext mlContext;
        private string imagesFolder;

        public OnnxModelScorer(string modelPath, string imagesFolder)
        {
            this.Path = modelPath;
            this.mlContext = new MLContext();
            this.imagesFolder = imagesFolder;
        }

        private ITransformer LoadModel(string modelLocation)
        {
            Console.WriteLine("Read model");
            Console.WriteLine($"Model location: {modelLocation}");
            Console.WriteLine($"Default parameters: image size=({ImageNetSettings.imageWidth},{ImageNetSettings.imageHeight})");
            var data = mlContext.Data.LoadFromEnumerable(new List<ImageNetData>());

            var pipeline = mlContext.Transforms.LoadImages(outputColumnName: "image", imageFolder: "", inputColumnName: nameof(ImageNetData.ImagePath))
                            .Append(mlContext.Transforms.ResizeImages(outputColumnName: "image", imageWidth: ImageNetSettings.imageWidth, imageHeight: ImageNetSettings.imageHeight, inputColumnName: "image"))
                            .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "data", inputColumnName: "image"))
                            .Append(mlContext.Transforms.ApplyOnnxModel(modelFile: modelLocation//,
                                    //outputColumnNames: ModelSettings.ModelOutputs.Split(','),
                                    //inputColumnNames: new[] { ModelSettings.ModelInput }
                                    ));

            var model = pipeline.Fit(data);
            return model;
        }

        private IEnumerable<float[]> PredictDataUsingModel(IDataView testData, ITransformer model)
        {
            Console.WriteLine($"Images location: {imagesFolder}");
            Console.WriteLine("");
            Console.WriteLine("=====Identify the objects in the images=====");
            Console.WriteLine("");
            IDataView scoredData = model.Transform(testData);
            IEnumerable<float[]> probabilities = scoredData.GetColumn<float[]>(ModelSettings.ModelOutputs.Split(',')[1]);
            IEnumerable<Int64[]> classes = scoredData.GetColumn<Int64[]>(ModelSettings.ModelOutputs.Split(',')[2]);
            IEnumerable<float[]> boxes = scoredData.GetColumn<float[]>(ModelSettings.ModelOutputs.Split(',')[0]);
            var classList = classes.ToList();
            var probsList = probabilities.ToList();
            var b = boxes.ToList();

            //var arranged= probsList.ToDictionary()
            return probabilities;
        }

        public IEnumerable<float[]> RunDetection()
        {
            IList<YoloBoundingBox> _boundingBoxes = new List<YoloBoundingBox>();
            IEnumerable<ImageNetData> images = ImageNetData.ReadFromFile(imagesFolder);
            IDataView imageDataView = mlContext.Data.LoadFromEnumerable(images);

            var model = LoadModel(this.Path);
            var probs = PredictDataUsingModel(imageDataView, model);
            YoloOutputParser parser = new YoloOutputParser();

            var boundingBoxes =
                probs
                .Select(probability => parser.ParseOutputs(probability))
                .Select(boxes => parser.FilterBoundingBoxes(boxes, 5, .5F));

            var _ = boundingBoxes.First();
            return probs;
        }
    }
}