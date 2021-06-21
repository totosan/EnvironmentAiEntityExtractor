namespace EntityExtractor.ML.Data
{
    using System.Linq;

    public class ImageNetPrediction
    {
        public bool IsEmpty
        {
            get { return this.PredictedScores.Length == 0; }
        }

        public string[] PredictedLabels;
        public float[] PredictedScores;
        public float[][] PredictedBoxes;

        public ImageNetPrediction GetBestResults(int bestCount, float confidence)
        {
            if (this.PredictedScores != null)
            {
                var rearranged = this.PredictedScores!.Select((x, index) => new { Index = index, Prediction = x });
                var bestAll = rearranged!.Where(p => p.Prediction >= confidence).OrderByDescending(p => p.Prediction);
                var best = bestAll.Take(bestCount);
                return new ImageNetPrediction
                {
                    PredictedBoxes = best.Select(score => this.PredictedBoxes[score.Index]).ToArray(),
                    PredictedScores = best.Select(score => this.PredictedScores[score.Index]).ToArray(),
                    PredictedLabels = best.Select(score => this.PredictedLabels[score.Index]).ToArray()
                };
            }
            return new ImageNetPrediction{PredictedScores = new float[]{}};
        }
    }
}