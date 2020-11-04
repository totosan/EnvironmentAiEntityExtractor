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
using System.Linq;

namespace EntityExtractor
{
    class Program
    {
        /*
            EntityExtractor.exe <input folder> <output folder>
        
        */
        private const string Url = "http://localhost:32083/detect";
        private static ServiceProvider _svcProv;
        static async Task Main(string[] args)
        {
            _svcProv = Configure();
            var logger = _svcProv.GetService<ILogger<Program>>();

            if (args.Length == 2)
            {
                await Task.Run(() =>
                {
                    var detector = new ML.OnnxModelScorer(GetAbsolutePath("ML\\TomowArea_iter4.ONNX\\model.onnx"), GetAbsolutePath("ML\\TomowArea_iter4.ONNX\\labels.txt"));
                    var allFiles = Directory.GetFiles(args[0], "ispy_2019-06-20_08*");
                    foreach (var file in allFiles)
                    {
                        var imgr = detector.RunDetection(file);
                        imgr.DrawDetections();
                        imgr.Save(Path.Combine(".\\testoutput\\", Path.GetFileName(imgr.PathOfFile)));
                    }
                });
                //await RunAsync(args[0], args[1], logger);

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
                var pipeline = GetCastingPipeline(filePutter);

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

        class DetectionOutputPoco
        {
            public JArray Detections { get; set; }
            public string File { get; set; }
        }
        private static DetectionOutputPoco GetDetections(string file)
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
                return new DetectionOutputPoco() { Detections = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(resultString), File = file };
            }
            return null;
        }


        class WriteFileOutputPoco
        {
            public string File { get; set; }
            public Dictionary<string, List<(int[], string)>> Library { get; set; }
        }

        private static WriteFileOutputPoco GetLabels(DetectionOutputPoco detectionResult)
        {
            JArray yoloResult;
            string file;
            if (detectionResult == null)
                return null;

            yoloResult = detectionResult.Detections;
            file = detectionResult.File;

            if (yoloResult != null && yoloResult.Count > 0)
            {
                var libraryLocal = new Dictionary<string, List<(int[], string)>>();
                for (var i = 0; i < yoloResult.Count; i++)
                {
                    if (!libraryLocal.ContainsKey(yoloResult[i][0].ToString()))
                        libraryLocal[yoloResult[i][0].ToString()] = new List<(int[], string)>();
                    libraryLocal[yoloResult[i][0].ToString()].Add((yoloResult[i][2].ToObject<int[]>(), file));
                    Console.Write($" {yoloResult[i][0]} ({yoloResult[i][1]})");
                }
                return new WriteFileOutputPoco() { Library = libraryLocal, File = file };
            }
            return null;
        }

        private static Dictionary<string, List<(int[], string)>> WriteFileOutput(FilePutter filePutter, WriteFileOutputPoco outputPoco)
        {
            if (outputPoco == null)
            {
                return null;
            }
            filePutter.WriteMetaData(outputPoco.File, outputPoco.Library);
            return outputPoco.Library;
        }

        private static IAwaitablePipeline<Dictionary<string, List<(int[], string)>>> GetCastingPipeline(FilePutter filePutter)
        {
            var builder = new CastingPipelineWithAwait<Dictionary<string, List<(int[], string)>>>();
            builder.AddStep(input => input as string, 2, 10);
            builder.AddStep(input => GetDetections(input as string), 3, 10);
            builder.AddStep(input => GetLabels((input as DetectionOutputPoco)), 1, 10);
            builder.AddStep(input => WriteFileOutput(filePutter, input as WriteFileOutputPoco), 1, 10);
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
