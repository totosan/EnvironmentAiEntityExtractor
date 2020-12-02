

using EntityExtractor.Images;

namespace EntityExtractor.ML.Interfaces
{
    
    public interface IModelScorer
    {
        string[] Lables { get; }
        Imager RunDetection(Imager imgr);
    }
}