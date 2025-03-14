using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Models;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Serina.Semantic.Ai.Pipelines.Streams
{
    public sealed class PipelineChannelStream : IPipelineStream
    {
        private static readonly ConcurrentDictionary<Guid, Channel<MessageResponse>> _channels = new();

        public async Task WriteChunk(MessageResponse response, Guid sessionId)
        {
            var channel = _channels.GetOrAdd(sessionId, _ => Channel.CreateUnbounded<MessageResponse>());
            await channel.Writer.WriteAsync(response);
        }

        public async IAsyncEnumerable<MessageResponse> ReadResponses(Guid sessionId)
        {
            var channel = _channels.GetOrAdd(sessionId, _ => Channel.CreateUnbounded<MessageResponse>());


            // Stream messages as soon as they arrive
            await foreach (var message in channel.Reader.ReadAllAsync())
            {
                yield return message;
            }

            // Cleanup when the channel is completed
            Reset(sessionId);
        }

        public void Reset(Guid id)
        {
            if (_channels.TryRemove(id, out var channel))
            {
                channel.Writer.TryComplete(); // Ensure no more writes happen
            }
        }

        public void Complete(Guid id)
        {
            if (_channels.TryGetValue(id, out var channel))
            {
                channel.Writer.Complete();
            }
        }
    }
}
