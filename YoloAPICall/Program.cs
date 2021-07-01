using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using Files;
using PipelineImplementations.BlockingCollection;
using System.IO;
using EntityExtractor.ML.Model;
using System.Linq;
using EntityExtractor;
using System.Diagnostics;

namespace Temp
{
    partial class Program
    {
        /*
            EntityExtractor.exe <input folder> <output folder>

            https://hub.docker.com/r/totosan/yolov3-tiny
            e.g.: docker run -it --rm -p 5000:8080 totosan/yolov3-tiny:latest
        */
        private const string Url = "http://localhost:5000/predict-raw";
        //private const string Url = "http://localhost:3031/image";
        private static ServiceProvider _svcProv;
        private static bool WithSubFolder = false;
        private static float CONFIDENCE = 0.3f;

        static async Task Main(string[] args)
        {
            _svcProv = Configure();
            var logger = _svcProv.GetService<ILogger<Program>>();

            if (args.Length >= 2)
            {
                string sourceFolder = args[0];
                string targetFolder = args[1];
                WithSubFolder = args.Length == 3 && ((string)args[2]) == "-s";

                await RunAsync(sourceFolder, targetFolder, logger);

                while (Console.ReadKey(true).Key != ConsoleKey.Escape)
                {
                    await Task.Delay(100);
                }
            }
            else
            {
                Console.WriteLine("Usage: EntityExtractor.exe <folder with pics> <folder for output> [-s]");
                Console.WriteLine("         <folder with pics>: source folder containing all images, for scanning");
                Console.WriteLine("         <folder for output>: target folder, to sort the images into there identified classes (Tags/ Labels)");
                Console.WriteLine("         -s : optional - arranges images in target folder into sub folder, that are named like the identified class ob object");
            }
        }


        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(Program).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
        }

        static async Task RunAsync(string path, string output, Microsoft.Extensions.Logging.ILogger logger)
        {
            await Task.Run(() =>
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                FilePutter filePutter = new FilePutter(output, WithSubFolder, logger);

                var grabber = new Files.FileGrabber(path);
                if (grabber.IsError)
                {
                    Console.WriteLine($"'{path}' is no folder or does not exists!");
                }

                var grabbedFiles = grabber.GetFiles(new DateTime(2021,05,01),FileGrabber.DateTimeScope.Month);

                var pipeline = GetObjectDetectionPipeline(filePutter);

                var tsk = System.Threading.Tasks.Task.Run(async () =>
               {

                   foreach (var file in grabbedFiles)
                   {
                       var lib = await pipeline.Execute(file);
                       //Console.WriteLine(lib?.Count);
                   }
               });
                tsk.Wait();

                watch.Stop();
                Console.WriteLine("Finished Pipeline in "+ watch.Elapsed.ToString());
            });
        }
        private static DetectionOutputPoco GetDetectionsStep(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                throw new ArgumentException("Filename not given!");
            }
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("threshold", "0.4");
            try
            {
                var resultString = ImageUpload.ImageUploader.UploadFilesToRemoteUrl(Url, new string[] { file }, nvc);
                resultString = resultString.Replace("\r", "").Replace("\n", "");
                if (resultString != "[]" && resultString != "{}")
                {
                    Console.Write($"{file} :");
                    JObject detection = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(resultString);
                    return new DetectionOutputPoco() { Detections = detection, File = file };
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return null;
        }

        private static WriteFileOutputPoco GetLabelsStep(DetectionOutputPoco detectionResult)
        {
            JObject detectorResult;
            string file;
            if (detectionResult == null)
                return null;

            detectorResult = detectionResult.Detections;
            file = detectionResult.File;

            if (detectorResult != null && detectorResult["predictions"].HasValues)
            {
                var libraryLocal = new Dictionary<string, List<DetectionItem>>();
                List<string>[] keys = new List<string>[2];
                for (var i = 0; i < ((JArray)detectorResult["predictions"]).Count; i++)
                {
                    var prediction = detectorResult["predictions"][i];
                    var score = prediction["score"].ToObject<float>();
                    if (score < CONFIDENCE)
                        continue;
                    var className = prediction["class"].ToString();
                    if (!libraryLocal.ContainsKey(className))
                    {
                        libraryLocal[className] = new List<DetectionItem>();
                    }
                    MyBoundingBox box = new MyBoundingBox((float)prediction["top"],
                        (float)prediction["left"],
                        (float)prediction["bottom"] - (float)prediction["top"],
                        (float)prediction["right"] - (float)prediction["left"]
                    );
                    libraryLocal[prediction["class"].ToString()].Add(new DetectionItem { BoundingBoxes = box, FileName = file, Scoring = score });
                    Console.Write($"\r\n {className} ({score.ToString("P0")})");
                }
                return new WriteFileOutputPoco() { Library = libraryLocal, File = file };
            }
            return null;
        }

        private static Dictionary<string, List<DetectionItem>> WriteFileOutputStep(FilePutter filePutter, WriteFileOutputPoco outputPoco)
        {
            if (outputPoco == null || outputPoco.Library.Count == 0)
            {
                return null;
            }
            if (WithSubFolder)
                filePutter.SaveImageToSeperateFolders(outputPoco.File, outputPoco.Library);
            else
                filePutter.SaveFileWithMetaData(outputPoco.File, outputPoco.Library);
            return outputPoco.Library;
        }

        private static IAwaitablePipeline<Dictionary<string, List<DetectionItem>>> GetObjectDetectionPipeline(FilePutter filePutter)
        {
            var builder = new CastingPipelineWithAwait<Dictionary<string, List<DetectionItem>>>();
            var parallel = 2;
            builder.AddStep(input => input as string, parallel, 2);
            builder.AddStep(input => GetDetectionsStep(input as string), parallel, 2);
            builder.AddStep(input => GetLabelsStep((input as DetectionOutputPoco)), parallel, 2);
            builder.AddStep(input => WriteFileOutputStep(filePutter, input as WriteFileOutputPoco), parallel, 10);
            var pipeline = builder.GetPipeline();
            return pipeline;
        }

        private static ServiceProvider Configure()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                                           .AddEnvironmentVariables()
                                           .Build();

            Log.Logger = new LoggerConfiguration()
                      .WriteTo.File("file-analytics.csv")
                      .CreateLogger();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            return serviceCollection.BuildServiceProvider();

        }

        private static void ConfigureServices(ServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(configure => configure.AddSerilog())
              .AddTransient<Files.FilePutter>();
        }
    }
}
