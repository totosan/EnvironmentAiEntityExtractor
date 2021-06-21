namespace EntityExtractor
{


    using EntityExtractor.ML.Model;
    public class DetectionItem
    {
        public MyBoundingBox BoundingBoxes { get; set; }
        public string FileName { get; set; }
        public float Scoring { get; set; }
    }
}