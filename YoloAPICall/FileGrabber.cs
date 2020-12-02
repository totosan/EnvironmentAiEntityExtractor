using System.IO;

namespace Files
{
    public class FileGrabber
    {
        public string LookupPath { get; set; }
        public bool IsError { get; internal set; }

        public FileGrabber(string path)
        {
            if (Directory.Exists(path))
            {
                IsError = false;
            }
            else
            {
                IsError = true;
            }
            LookupPath = path;
        }

        public string[] GetFiles()
        {
            var files = Directory.GetFiles(LookupPath, "*.jpg");
            return files;
        }
    }
}