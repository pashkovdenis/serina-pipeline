using System.Threading.Channels;

namespace Serina.Semantic.Ai.Pipelines.Channels
{
    public interface IWorkConsumer<T> where T : class
    {
        Task StartConsumingAsync(ChannelReader<T> reader);
    }
}
