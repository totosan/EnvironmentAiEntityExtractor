using System.IO;
using Microsoft.Extensions.Logging;
using Images;

namespace Files
{
    public class FilePutter
    {
        readonly ILogger _log;
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

        public FilePutter(string path, ILogger log)
        {
            Path = path;
            _log = log;
        }

        public void Put(string filename, int counter, string className, float confidence, int[] rect)
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
    }
}