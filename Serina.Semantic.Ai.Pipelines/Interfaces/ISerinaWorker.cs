using Serina.Semantic.Ai.Pipelines.Models;

namespace Serina.Semantic.Ai.Pipelines.Interfaces
{

    public interface ISerinaWorker
    {
        ValueTask TickAsync(PipelineContext context);
    }
}
