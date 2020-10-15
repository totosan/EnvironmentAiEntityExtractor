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

namespace Temp
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
                await RunAsync(args[0], args[1], logger);

/*                while (Console.ReadKey(true).Key != ConsoleKey.Escape)
                {
                    await Task.Delay(100);
                }*/
            }
            else
            {
                Console.WriteLine("Usage: EntityExtractor.exe <folder with pics> <folder for output>");
            }
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
                var pipeline = CreatePipelineAwait(filePutter, library);

                int cnt = 0;
                var tsk = System.Threading.Tasks.Task.Run(async () =>
                {
                    foreach (var file in grabbedFiles)
                    {
                        await pipeline.Execute(file);
                        cnt++;
                        if (cnt == 1000)
                            break;
                    }
                });
                tsk.Wait();

                Console.WriteLine("Finished Pipeline");
            });
        }


        private static (JArray,string) GetDetections(string file)
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("threshold", "0.4");
            var resultString = ImageUpload.ImageUploader.UploadFilesToRemoteUrl(Url, new string[] { file }, nvc);
            resultString = resultString.Replace("\r", "").Replace("\n", "");
            if (resultString != "[]")
            {
                Console.Write($"{file} :");
                return (Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(resultString),file);
            }
            return (null,file);
        }

        private static (Dictionary<string, List<(int[], string)>>,string) GetLabels(JArray yoloResult, string file)
        {
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
                return (libraryLocal,file);
            }
            return (null,file);
        }

        private static Dictionary<string, List<(int[], string)>> WriteFileOutput(FilePutter filePutter, string file, Dictionary<string, List<(int[], string)>> libraryLocal)
        {
            if (libraryLocal != null)
            {
                filePutter.WriteMetaData(file, libraryLocal);
            }
            return libraryLocal;
        }
        private static void DoWork(FilePutter filePutter, Dictionary<string, List<(int[], string)>> library, string file)
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("threshold", "0.4");
            var resultString = ImageUpload.ImageUploader.UploadFilesToRemoteUrl(Url, new string[] { file }, nvc);
            resultString = resultString.Replace("\r", "").Replace("\n", "");
            if (resultString != "[]")
            {
                Console.Write($"{file} :");
                JArray yoloResult = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(resultString);
                if (yoloResult.Count > 0)
                {
                    var libraryLocal = new Dictionary<string, List<(int[], string)>>();
                    for (var i = 0; i < yoloResult.Count; i++)
                    {
                        if (!libraryLocal.ContainsKey(yoloResult[i][0].ToString()))
                            libraryLocal[yoloResult[i][0].ToString()] = new List<(int[], string)>();
                        libraryLocal[yoloResult[i][0].ToString()].Add((yoloResult[i][2].ToObject<int[]>(), file));
                        Console.Write($" {yoloResult[i][0]} ({yoloResult[i][1]})");
                        //filePutter.Put(file,i, yoloResult[i][0].ToString(), Convert.ToSingle(yoloResult[i][1]), yoloResult[i][2].ToObject<int[]>());
                    }
                    filePutter.WriteMetaData(file, libraryLocal);

                    foreach (var d in libraryLocal)
                    {
                        if (library.ContainsKey(d.Key))
                        {
                            library[d.Key].AddRange(d.Value);
                        }
                        else
                            library.Add(d.Key, d.Value);
                    }
                    Console.WriteLine();
                }
            }
        }

        private static GenericBCPipelineAwait<string, Dictionary<string, List<(int[], string)>>> CreatePipelineAwait(FilePutter filePutter, Dictionary<string, List<(int[], string)>> library)
        {
            var pipeline = new GenericBCPipelineAwait<string, Dictionary<string, List<(int[], string)>>>((inputFirst, builder) =>
                 inputFirst
                        .Step2(builder, input => GetDetections(input))
                        .Step2(builder, input => GetLabels(input.Item1, input.Item2))
                        .Step2(builder, input => WriteFileOutput(filePutter, input.Item2, input.Item1)));
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
