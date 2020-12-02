using Newtonsoft.Json.Linq;

namespace Temp
{
    partial class Program
    {
        class DetectionOutputPoco
        {
            public JObject Detections { get; set; }
            public string File { get; set; }
        }
    }
}
