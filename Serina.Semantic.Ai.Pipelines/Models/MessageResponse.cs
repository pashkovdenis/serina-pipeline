using System.Text;

namespace Serina.Semantic.Ai.Pipelines.Models
{
    public sealed record MessageResponse
    {
        private readonly StringBuilder _responseBuilder = new StringBuilder();

        public List<RequestMessage> RequestMessages { get; set; }

        public DateTime Created { get; } = DateTime.Now;

        public string Content => _responseBuilder.ToString();

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public string ModelId { get; set; }

        public MessagePayload Payload { get; set; }


        public int TotalTokens { get; set; }

        public bool IsDone { get; set; }


        public void AddContent(string part)
        {
            _responseBuilder.Append(part);
        }

    }
}
