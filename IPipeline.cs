using System;
using System.Threading.Tasks;

namespace PipelineImplementations.BlockingCollection
{
public interface IPipeline
{
    void Execute(object input);
    event Action<object> Finished;
}

public interface IAwaitablePipeline<TOutput>
{
    Task<TOutput> Execute(object input);
}
}