namespace Serina.Semantic.Ai.Pipelines.Channels
{
    public interface IWorkingChannel<T> where T : class
    {
        ValueTask PublishAsync(T message);
        void RegisterConsumer(IWorkConsumer<T> consumer);
    }
}
