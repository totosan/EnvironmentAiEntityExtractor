﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace EntityExtractor
{
    class Program
    {
        /*
            EntityExtractor.exe <input folder> <output folder>
        
        */
        private static ServiceProvider _svcProv;
        static async Task Main(string[] args)
        {
            _svcProv = Configure();
            var logger = _svcProv.GetService<ILogger<Program>>();

            if (args.Length >= 2 && args.Length <= 3)
            {
                bool subFolder = args.Length == 3 && args[2]=="-s";

                await Task.Run(() =>
                {
                    var allFiles = Directory.GetFiles(args[0]);
                    var detector = new ML.OnnxModelScorer(GetAbsolutePath("ML\\TomowArea_iter4.ONNX\\model.onnx"), GetAbsolutePath("ML\\TomowArea_iter4.ONNX\\labels.txt"));
                    var timer = new Stopwatch();
                    timer.Start();
                    Parallel.ForEach(allFiles, new ParallelOptions() { MaxDegreeOfParallelism = Convert.ToInt32(Environment.ProcessorCount * 0.5f) }, (file) =>
                              {
                                  var imgr = detector.RunDetection(file);
                                  imgr.DrawDetections();
                                  if (!imgr.DetectionResults.IsEmpty)
                                  {
                                      var rootfolder = GetAbsolutePath(args[1]);
                                      if (subFolder) //sort into seperate folder
                                      {
                                          foreach (var label in imgr.DetectionResults.PredictedLabels)
                                          {
                                              var folder = GetAbsolutePath(Path.Combine(rootfolder, label));
                                              if (!Directory.Exists(folder))
                                              {
                                                  Directory.CreateDirectory(folder);
                                              }
                                              imgr.Save(Path.Combine(folder, Path.GetFileName(imgr.PathOfFile)));
                                          }
                                      }
                                      else
                                      {
                                          imgr.Save(Path.Combine(rootfolder, Path.GetFileName(imgr.PathOfFile)));
                                      }
                                  }
                              });
                    timer.Stop();
                    Console.WriteLine($"Completed run in {timer.ElapsedMilliseconds / 1000f}s with {allFiles.Count()} files");
                });

                while (Console.ReadKey(true).Key != ConsoleKey.Escape)
                {
                    await Task.Delay(100);
                }
            }
            else
            {
                Console.WriteLine("Usage: EntityExtractor.exe <folder with pics> <folder for output> [-s]");
                Console.WriteLine(" <folder with pics>: source folder containing images");
                Console.WriteLine(" <folder for output>: target folder, where to put the rendered pics wiht detections");
                Console.WriteLine(" -s for creating sub folders with images sorted by detection in image\r\n\telse, images will be saved directly to target folder");
            }
        }


        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(Program).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
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
            serviceCollection.AddLogging(configure => configure.AddSerilog());
        }
    }
}
