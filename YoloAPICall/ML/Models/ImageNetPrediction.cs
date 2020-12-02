namespace ML.Data
{
    using Microsoft.ML.Data;

    public class ImageNetPrediction
    {
        [ColumnName("grid")]
        public float[] PredictedLabels;
    }
}