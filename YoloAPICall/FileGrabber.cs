using System;
using System.IO;
using System.Linq;

namespace Files
{
    public class FileGrabber
    {
        public enum DateTimeScope
        {
            Month,
            Day,
            Minutes,
            Seconds
        }

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
        public string[] GetFiles(DateTime filterDate, DateTimeScope scope)
        {
            var files = Directory.GetFiles(LookupPath, "*.jpg");
            var filterDateString = "";
            switch (scope)
            {
                case DateTimeScope.Month:
                    filterDateString = filterDate.ToString("yyyy-MM");
                    break;
                case DateTimeScope.Day:
                    filterDateString = filterDate.ToString("yyyy-MM-dd");
                    break;
                case DateTimeScope.Minutes:
                    filterDateString = filterDate.ToString("yyyy-MM-dd_hh-mm");
                    break;
                case DateTimeScope.Seconds:
                    filterDateString = filterDate.ToString("yyyy-MM-dd_hh-mm-ss");
                    break;
                default:
                    filterDateString = filterDate.ToString("yyyy-");
                    break;
            }
            files = files.Where(f => Path.GetFileName(f).StartsWith($"{filterDateString}")).ToArray();
            return files;
        }
    }
}