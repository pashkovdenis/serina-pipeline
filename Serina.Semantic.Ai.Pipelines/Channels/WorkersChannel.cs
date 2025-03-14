using System.Threading.Channels;

namespace Serina.Semantic.Ai.Pipelines.Channels
{
    public sealed class WorkersChannel<T> : IWorkingChannel<T> where T : class
    {
        private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

        private readonly List<IWorkConsumer<T>> _consumers = new List<IWorkConsumer<T>>();

        public async ValueTask PublishAsync(T message)
        {
            if (message != null)
            {
                await _channel.Writer.WriteAsync(message);
            }
        }

        public void RegisterConsumer(IWorkConsumer<T> consumer)
        {
            _consumers.Add(consumer);

            _ = consumer.StartConsumingAsync(_channel.Reader);
        }
    }
}
