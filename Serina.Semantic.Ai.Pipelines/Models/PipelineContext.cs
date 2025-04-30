using Microsoft.SemanticKernel.ChatCompletion;

namespace Serina.Semantic.Ai.Pipelines.Models
{
    public sealed class PipelineContext
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public List<ContextAttributes> Attributes { get; set; } = new List<ContextAttributes>();

        public RequestMessage RequestMessage { get; set; }

        public MessageResponse Response { get; set; }

        public bool EnableFunctions { get; set; }

        public bool AutoFunction { get; set; }


        public ChatHistory ChatHistory { get; set; } = new();

    }


    /// <summary>
    /// Session attribute  such as address time context mood etc 
    /// </summary>
    public sealed class ContextAttributes
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public override string ToString() => $"{Key} => {Value}";
    }
}
