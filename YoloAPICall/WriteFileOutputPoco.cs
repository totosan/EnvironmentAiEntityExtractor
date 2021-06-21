using System.Collections.Generic;
using EntityExtractor;

namespace Temp
{
    partial class Program
    {
        class WriteFileOutputPoco
        {
            public string File { get; set; }
            public Dictionary<string, List<DetectionItem>> Library { get; set; }
        }
    }
}
