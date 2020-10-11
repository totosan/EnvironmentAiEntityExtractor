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

namespace Temp
{
    class Program
    {
        /*
            EntityExtractor.exe <input folder> <output folder>
        
        */
        private const string Url = "http://localhost:8080/detect";
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

        static async Task RunAsync(string path, string output, Microsoft.Extensions.Logging.ILogger logger)
        {
            await Task.Run(() =>
            {
                FilePutter filePutter = new FilePutter(output, logger);

                NameValueCollection nvc = new NameValueCollection();
                nvc.Add("threshold", "0.4");

                var grabber = new Files.FileGrabber(path);
                if (grabber.IsError)
                {
                    Console.WriteLine($"'{path}' is no folder or does not exists!");
                }

                var grabbedFiles = grabber.GetFiles();
                var library = new Dictionary<string, (int[], string)>();
                foreach (var file in grabbedFiles)
                {
                    var resultString = ImageUpload.ImageUploader.UploadFilesToRemoteUrl(Url, new string[] { file }, nvc);
                    resultString = resultString.Replace("\r", "").Replace("\n", "");
                    if (resultString != "[]")
                    {
                        Console.Write($"{file} :");
                        JArray yoloResult = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(resultString);
                        if (yoloResult.Count > 0)
                        {
                            for (var i = 0; i < yoloResult.Count; i++)
                            {
                                library[yoloResult[i][0].ToString()] = (yoloResult[i][2].ToObject<int[]>(), file);
                                Console.Write($" {yoloResult[i][0]} ({yoloResult[i][1]})");

                                filePutter.Put(file,i, yoloResult[i][0].ToString(), Convert.ToSingle(yoloResult[i][1]), yoloResult[i][2].ToObject<int[]>());
                            }
                            Console.WriteLine();
                        }
                    }
                }
            });
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
