namespace ML.Data
{
    using System.Linq;

    public class ImageNetPrediction
    {
        public long[] PredictedLabels;
        public float[] PredictedScores;
        public float[][] PredictedBoxes;

        public ImageNetPrediction GetBestResults(int bestCount)
        {
            var rearranged= this.PredictedScores.Select((x,index)=> new { Index = index, Prediction = x });
            var bestAll = rearranged.OrderByDescending(p => p.Prediction);
            var best= bestAll.Take(bestCount);
            return new ImageNetPrediction
            {
                PredictedBoxes = best.Select(score => this.PredictedBoxes[score.Index]).ToArray(),
                PredictedScores = best.Select(score => this.PredictedScores[score.Index]).ToArray(),
                PredictedLabels = best.Select(score => this.PredictedLabels[score.Index]).ToArray()
            };
        }
    }
}