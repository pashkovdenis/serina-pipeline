using Microsoft.SemanticKernel.Agents.History;
using Microsoft.SemanticKernel.ChatCompletion;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.ValueObject;

namespace Serina.Semantic.Ai.Pipelines.SemanticKernel.Reducers
{
    public sealed class SummarizeReducer : ISerinaReducer
    {
        private int Min = 5;
        private int Max = 10;

        public SummarizeReducer()
        {
        }

        public SummarizeReducer(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public async ValueTask<ChatHistory> ReduceHistory(PipelineContext context, ChatHistory chatHistory,
            IChatCompletionService chatService)
        {
            var reducer = new ChatHistorySummarizationReducer(chatService, Min, Max);

            var reducedMessages = await reducer.ReduceAsync(chatHistory, default).ConfigureAwait(false);

            if (reducedMessages != default)
            {

                var system = context.RequestMessage.History.FirstOrDefault(x => x.Role == MessageRole.System);

                if (system != default)
                {
                    var rebuilded = new ChatHistory();

                    rebuilded.AddSystemMessage(system.Content);

                    rebuilded.AddRange(reducedMessages);

                    return rebuilded;
                }
            }

            var reducedHistory = reducedMessages is null ? chatHistory : new ChatHistory(reducedMessages);

            return reducedHistory;
        }
    }
}