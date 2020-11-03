namespace ML.Data
{
    using Microsoft.ML.Data;

    public class ImageNetPrediction
    {
        public long[] PredictedLabels;
        public float[] PredictedScores;
        public float[][] PredictedBoxes;

    }
}