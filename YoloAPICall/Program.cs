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

namespace Temp
{
    partial class Program
    {
        /*
            EntityExtractor.exe <input folder> <output folder>
        
        */
        private const string Url = "http://localhost:3031/image";
        private static ServiceProvider _svcProv;
        static async Task Main(string[] args)
        {
            _svcProv = Configure();
            var logger = _svcProv.GetService<ILogger<Program>>();

            if (args.Length == 2)
            {
                await RunAsync(args[0], args[1], logger);

                while (Console.ReadKey(true).Key != ConsoleKey.Escape)
                {
                    await Task.Delay(100);
                }
            }
            else
            {
                Console.WriteLine("Usage: EntityExtractor.exe <folder with pics> <folder for output>");
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
                FilePutter filePutter = new FilePutter(output, logger);

                var grabber = new Files.FileGrabber(path);
                if (grabber.IsError)
                {
                    Console.WriteLine($"'{path}' is no folder or does not exists!");
                }

                var grabbedFiles = grabber.GetFiles();

                var library = new Dictionary<string, List<(int[], string)>>();
                var pipeline = GetObjectDetectionPipeline(filePutter);

                int cnt = 0;
                var tsk = System.Threading.Tasks.Task.Run(async () =>
               {

                   foreach (var file in grabbedFiles)
                   {
                       var lib = await pipeline.Execute(file);
                       //Console.WriteLine(lib?.Count);
                   }
               });
                tsk.Wait();

                Console.WriteLine("Finished Pipeline");
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
            var resultString = ImageUpload.ImageUploader.UploadFilesToRemoteUrl(Url, new string[] { file }, nvc);
            resultString = resultString.Replace("\r", "").Replace("\n", "");
            if (resultString != "[]")
            {
                Console.Write($"{file} :");
                return new DetectionOutputPoco() { Detections = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(resultString), File = file };
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
                var libraryLocal = new Dictionary<string, List<(int[], string)>>();
                for (var i = 0; i < ((JArray)detectorResult["predictions"]).Count; i++)
                {
                    var prediction = detectorResult["predictions"][i];
                    if (prediction["probability"].ToObject<float>() < 0.30)
                        continue;
                    if (!libraryLocal.ContainsKey(prediction["tagName"].ToString()))
                        libraryLocal[prediction["tagName"].ToString()] = new List<(int[], string)>();
                    libraryLocal[prediction["tagName"].ToString()].Add((prediction["boundingBox"].ToObject<MyBoundingBox>().ConvertToIntArray(), file));
                    Console.Write($" {prediction["tagName"]} ({prediction["probability"]})");
                }
                return new WriteFileOutputPoco() { Library = libraryLocal, File = file };
            }
            return null;
        }

        private static Dictionary<string, List<(int[], string)>> WriteFileOutputStep(FilePutter filePutter, WriteFileOutputPoco outputPoco)
        {
            if (outputPoco == null || outputPoco.Library.Count==0)
            {
                return null;
            }
            filePutter.WriteMetaData(outputPoco.File, outputPoco.Library);
            return outputPoco.Library;
        }

        private static IAwaitablePipeline<Dictionary<string, List<(int[], string)>>> GetObjectDetectionPipeline(FilePutter filePutter)
        {
            var builder = new CastingPipelineWithAwait<Dictionary<string, List<(int[], string)>>>();
            var parallel = 2;
            builder.AddStep(input => input as string, parallel, 10);
            builder.AddStep(input => GetDetectionsStep(input as string), parallel, 10);
            builder.AddStep(input => GetLabelsStep((input as DetectionOutputPoco)), parallel, 10);
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
