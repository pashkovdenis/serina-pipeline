using Serina.Semantic.Ai.Pipelines.Models;

namespace Serina.Semantic.Ai.Pipelines.Interfaces
{
    public interface IPipelineStream
    {
        void Complete(Guid id);
        IAsyncEnumerable<MessageResponse> ReadResponses(Guid sessionId);
        void Reset(Guid id);
        Task WriteChunk(MessageResponse response, Guid sessionId);
    }
}
