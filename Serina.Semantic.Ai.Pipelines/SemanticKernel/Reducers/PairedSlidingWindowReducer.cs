using Microsoft.SemanticKernel.ChatCompletion;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Models;

namespace Serina.Semantic.Ai.Pipelines.SemanticKernel.Reducers
{
    public class PairedSlidingWindowReducer : ISerinaReducer
    {
        private readonly int _maxPairCount;


        public PairedSlidingWindowReducer() : this(10)
        {

        }


        public PairedSlidingWindowReducer(int maxPairCount)
        {
            _maxPairCount = maxPairCount;
        }

        public ValueTask<ChatHistory> ReduceHistory(PipelineContext context, ChatHistory chatHistory, IChatCompletionService chatService)
        {
            if (chatHistory == null || chatHistory.Count < 2)
                return ValueTask.FromResult(chatHistory);

            // Ensure a system message exists as the first message
            var systemMessage = chatHistory.FirstOrDefault();

            if (systemMessage == null || systemMessage.Role != AuthorRole.System)
            {
                throw new InvalidOperationException("Chat history must contain a system message as the first message.");
            }

            // Skip the system message and form user-bot pairs
            var messagePairs = chatHistory.Skip(1).Chunk(2);

            // Keep only the last `_maxPairCount` pairs
            var reducedPairs = messagePairs.Skip(Math.Max(0, messagePairs.Count() - _maxPairCount));

            // Flatten pairs and reinsert the system message at the start
            var reducedMessages = reducedPairs.SelectMany(pair => pair).Prepend(systemMessage).ToList();

            return ValueTask.FromResult(new ChatHistory(reducedMessages));
        }
    }
}
