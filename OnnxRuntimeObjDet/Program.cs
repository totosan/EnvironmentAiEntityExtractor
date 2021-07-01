using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO;
using System.Linq;
using System.Diagnostics;
using EntityExtractor.Images;
using SixLabors.ImageSharp;

namespace EntityExtractor
{
    class Program
    {
        /*
            EntityExtractor.exe <input folder> <output folder>
        
        */
        private static ServiceProvider _svcProv;

        protected static int origRow;
        protected static int origCol;

        protected static void WriteAt(string s, int x, int y)
        {
            try
            {
                Console.SetCursorPosition(origCol + x, origRow + y);
                Console.Write(s);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.Clear();
                Console.WriteLine(e.Message);
            }
        }

        static async Task Main(string[] args)
        {
            // args:  source-folder dest-folder [-s]
            //        -source-folder : path of images, that should be screened
            //        -dest-folder : folder, where to collect detected peaces
            //        -s : if set, for each found class a subfolder will be created

            _svcProv = Configure();
            var logger = _svcProv.GetService<ILogger<Program>>();
            origRow = Console.CursorTop;
            origCol = Console.CursorLeft;
            if (args.Length >= 2 && args.Length <= 3)
            {
                bool subFolder = args.Length == 3 && args[2] == "-s";

                await Task.Run(() =>
                {
                    var allFiles = Directory.GetFiles(args[0], "*2021-05-15*.jpg", SearchOption.AllDirectories).OrderByDescending(f=>f);
                    var detector = new ML.OnnxModelScorer(GetAbsolutePath("ML\\TomowArea_iter4.ONNX\\model.onnx"), GetAbsolutePath("ML\\TomowArea_iter4.ONNX\\labels.txt"));
                    detector.Confidence = 0.2f;
                    var timer = new Stopwatch();
                    timer.Start();
                    int cntr = 0;
                    Parallel.ForEach(allFiles, new ParallelOptions() { MaxDegreeOfParallelism = Convert.ToInt32(Environment.ProcessorCount * 0.5f) }, (file, state) =>
                              {
                                  // per Image
                                  Console.Clear();
                                  WriteAt($"{(cntr++).ToString()}/{allFiles.Count()}", 0, 0);
                                  WriteAt($"{file}", 10, 0);
                                  var imgr = new Imager(file);

                                  detector.RunDetection(imgr);

                                  if (!imgr.DetectionResults.IsEmpty)
                                  {
                                      var objects = imgr.GetDetectionsAsImages(cropped: false);

                                      var rootfolder = GetAbsolutePath(args[1]);
                                      if (subFolder) //sort into seperate folder
                                      {
                                          // per results (objects detected)
                                          foreach (var obj in objects)
                                          {
                                              var label = obj.Key;
                                              var folder = GetAbsolutePath(Path.Combine(rootfolder, label));
                                              if (!Directory.Exists(folder))
                                              {
                                                  Directory.CreateDirectory(folder);
                                              }
                                              foreach (var im in obj.Value)
                                              {
                                                  im.SaveAsJpeg(Path.Combine(folder, Path.GetFileNameWithoutExtension(imgr.PathOfFile) + Path.GetExtension(imgr.PathOfFile)));
                                                  
                                              }
                                          }
                                      }
                                      else
                                      {
                                          imgr.Save(Path.Combine(rootfolder, Path.GetFileName(imgr.PathOfFile)));
                                      }
                                  }
                                  if (cntr >= 20000)
                                      state.Break();
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
