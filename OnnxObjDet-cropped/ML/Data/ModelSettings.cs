using System;

namespace EntityExtractor.ML.Data
{
    public struct ModelSettings
    {
        // for checking Tiny yolo2 Model input and  output  parameter names,
        //you can use tools like Netron, 
        // which is installed by Visual Studio AI Tools

        // input tensor name
        public const string ModelInput = "data";

        // output tensor names (separated by ,)
        public const string ModelOutputs = "detected_boxes,detected_scores,detected_classes";
    }
}