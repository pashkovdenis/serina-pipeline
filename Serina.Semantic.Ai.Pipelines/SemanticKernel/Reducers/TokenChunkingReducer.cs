
using Microsoft.SemanticKernel.ChatCompletion;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Models;
using TiktokenSharp;

namespace Serina.Semantic.Ai.Pipelines.SemanticKernel.Reducers
{
    /// <summary>
    /// Universal reducer that:
    /// 1) Counts tokens in user messages using a provided tokenizer (or defaults to OpenAI BPE cl100k_base).
    /// 2) If a message exceeds the token limit, splits it into chunks and feeds each chunk sequentially to the model.
    /// 3) Replaces the original large user message in history with user–assistant pairs for each chunk, ensuring no duplicate or oversized messages persist.
    /// </summary>
    public sealed class TokenChunkingReducer : ISerinaReducer
    {
        private readonly int _tokenLimit;
        private readonly TikToken _tokenizer;

        /// <summary>
        /// Creates a TokenChunkingReducer using a universal tokenizer.
        /// If no tokenizer is provided, defaults to TiktokenSharp's cl100k_base encoding.
        /// </summary>
        /// <param name="tokenLimit">Maximum tokens per chunk.</param>
        /// <param name="tokenizer">Custom tokenizer for token counting and decoding.</param>
        public TokenChunkingReducer(int tokenLimit = 1024 )
        {
            _tokenLimit = tokenLimit;
            // Default BPE encoding for GPT family via TiktokenSharp
            _tokenizer =   TikToken.GetEncoding("cl100k_base") ;
        }

        public async ValueTask<ChatHistory> ReduceHistory(
            PipelineContext context,
            ChatHistory chatHistory,
            IChatCompletionService chatService)
        {
            var newHistory = new ChatHistory();
        
            foreach (var sys in chatHistory.Where(x => x.Role == AuthorRole.System))
            {
                newHistory.AddSystemMessage(sys.Content);
            }

            // Process user and assistant entries, replacing oversized user messages
            foreach (var entry in chatHistory)
            {
                if (entry.Role == AuthorRole.User)
                {
                    var tokens = _tokenizer.Encode(entry.Content);
                    if (tokens.Count <= _tokenLimit)
                    {
                        // Under limit: keep original user message
                        newHistory.AddUserMessage(entry.Content);
                    }
                    else
                    {
                        // Oversized: split into chunks and remove original to avoid loops
                        var chunks = ChunkTokens(tokens, _tokenLimit);

                        foreach (var chunk in chunks)
                        {
                            var chunkText = _tokenizer.Decode(chunk);

                            newHistory.AddUserMessage(chunkText);

                            var result =   chatService.GetStreamingChatMessageContentsAsync(
                                newHistory,
                                cancellationToken: CancellationToken.None
                            ) ;

                            var allResults = await result.ToListAsync();


                            var assistantText = string.Join(' ', allResults );


                            newHistory.AddAssistantMessage(assistantText);
                        }

                    }
                    // Explicitly skip re-adding the original message or fallback handling
                    continue;
                }
                if (entry.Role == AuthorRole.Assistant)
                {
                    // Skip original assistant responses to oversized messages
                    // Note: Assistant replies generated above for each chunk are already in newHistory
                    continue;
                }
                // Other roles (e.g., custom system/user-defined roles)
                newHistory.Add(entry);
            }

            return newHistory;
        }

        private static List<List<int>> ChunkTokens(IReadOnlyList<int> tokens, int size)
        {
            var list = new List<List<int>>();
            for (int i = 0; i < tokens.Count; i += size)
            {
                list.Add(tokens.Skip(i).Take(size).ToList());
            }
            return list;
        }
    }
 
}
