using Serina.Semantic.Ai.Pipelines.Enumerations;

namespace Serina.Semantic.Ai.Pipelines.Models
{
    public sealed class MessagePayload
    {
        public byte[] Data { get; set; }

        public PayloadType Type { get; set; }

        public string ContentType { get; set; }

        public string Uri { get; set; }
    }
}
