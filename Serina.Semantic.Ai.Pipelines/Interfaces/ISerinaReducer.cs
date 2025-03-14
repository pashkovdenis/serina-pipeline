using Microsoft.SemanticKernel.ChatCompletion;
using Serina.Semantic.Ai.Pipelines.Models;

namespace Serina.Semantic.Ai.Pipelines.Interfaces
{
    public interface ISerinaReducer
    {

        ValueTask<ChatHistory> ReduceHistory(PipelineContext context, ChatHistory chatHistory, IChatCompletionService chatService);

    }
}
