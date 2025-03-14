using Microsoft.SemanticKernel.Text;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using System.Text;

namespace Serina.Semantic.Ai.Pipelines.Filters
{
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public sealed class TextChunkerFilter : IMessageFilter
    {
        private readonly int _tokenLimit = 100;

        public TextChunkerFilter()
        {
        }

        public TextChunkerFilter(int tokenLimit)
        {
            _tokenLimit = tokenLimit;
        }

        public ValueTask<string> FilterAsync(string message)
        {
            var paragrpahs = TextChunker.SplitPlainTextLines(message, _tokenLimit);

            return ValueTask.FromResult(string.Join('\n', paragrpahs));
        }
    }
}
