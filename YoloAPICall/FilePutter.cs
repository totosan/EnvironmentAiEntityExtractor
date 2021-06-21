using System.IO;
using Microsoft.Extensions.Logging;
using Images;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using EntityExtractor;

namespace Files
{
    public class FilePutter
    {
        readonly ILogger _log;
        private readonly bool WithSubFolder;
        private string _path;

        public bool IsError { get; set; }
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                _path = value;
                if (string.IsNullOrWhiteSpace(_path) && Directory.Exists(_path))
                {
                    IsError = true;
                }
                else
                {
                    IsError = false;
                }
            }
        }

        public FilePutter(string path, bool subFolder, ILogger log)
        {
            Path = path;
            _log = log;
            WithSubFolder = subFolder;
        }

        public void SaveCroppedImageToSeperateFolders(string filename, int counter, string className, float confidence, int[] rect)
        {
            var imgr = new Images.Imager(filename);
            imgr.Rect = rect.ConvertToRect();
            var croppedImgAsBytes = imgr.CropAndConvertToBytes();

            var newPath = System.IO.Path.Combine(Path, className);
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            var filepath = System.IO.Path.Combine(newPath, $"{System.IO.Path.GetFileNameWithoutExtension(filename)}_{counter.ToString()}({confidence.ToString("P0")}){System.IO.Path.GetExtension(filename)}");
            File.WriteAllBytes(filepath, croppedImgAsBytes);
            _log.LogInformation($", {filename}, {className}, {confidence}");
        }

        public void SaveImageToSeperateFolders(string filename, Dictionary<string, List<DetectionItem>> value)
        {
            var imgr = new Images.Imager(filename);

            foreach (var item in value)
            {
                var className = item.Key;
                foreach (var detection in item.Value)
                {
                    var newPath = System.IO.Path.Combine(Path, className);
                    if (!Directory.Exists(newPath))
                    {
                        Directory.CreateDirectory(newPath);
                    }

                    var filepath = System.IO.Path.Combine(newPath, $"{System.IO.Path.GetFileNameWithoutExtension(filename)}_{detection.Scoring.ToString("P0")}_{System.IO.Path.GetExtension(filename)}");
                    imgr.Image.Save(filepath);
                    _log.LogInformation($", {System.IO.Path.GetFileName(filename)}, {className}, {detection.Scoring.ToString("P0")}");
                }
            }

        }

        public void SaveFileWithMetaData(string filename, Dictionary<string, List<DetectionItem>> value)
        {
            var imgr = new Images.Imager(filename);
            var commentField = MetaProperty.ImageDescription;

            var lables = string.Join(',', value.Select(x => x.Key));
            imgr.Image.SetMetaValue(commentField, lables);

            var filepath = System.IO.Path.Combine(Path, System.IO.Path.GetFileName(filename));
            imgr.Image.Save(filepath);
            //File.WriteAllBytes(filepath, imgr.Image.Crop(imgr.Image.Size.Width, imgr.Image.Size.Height,0,0));             
        }
    }
}