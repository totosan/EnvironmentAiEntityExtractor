namespace EntityExtractor.ML.Data
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.ML.Data;

    public class ImageNetData
    {
        [LoadColumn(0)]
        public string ImagePath;

        [LoadColumn(1)]
        public string Label;

        public static IEnumerable<ImageNetData> ReadFromFile(string imageFolder)
        {
            return Directory
                .GetFiles(imageFolder).TakeLast(10)
                .Where(filePath => Path.GetExtension(filePath) != ".md")
                .Select(filePath => new ImageNetData { ImagePath = filePath, Label = Path.GetFileName(filePath) });
        }
    }
}