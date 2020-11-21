using Newtonsoft.Json.Linq;

namespace Temp
{
    partial class Program
    {
        class DetectionOutputPoco
        {
            public JArray Detections { get; set; }
            public string File { get; set; }
        }
    }
}
