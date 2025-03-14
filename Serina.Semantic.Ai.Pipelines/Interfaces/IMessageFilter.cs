namespace Serina.Semantic.Ai.Pipelines.Interfaces
{
    public interface IMessageFilter
    {
        ValueTask<string> FilterAsync(string message);
    }
}
