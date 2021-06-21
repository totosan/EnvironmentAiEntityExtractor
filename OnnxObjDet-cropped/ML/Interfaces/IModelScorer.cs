

using EntityExtractor.Images;

namespace EntityExtractor.ML.Interfaces
{
    
    public interface IModelScorer
    {
        string[] Lables { get; }

        float Confidence {get;set;}
        Imager RunDetection(Imager imgr);
    }
}