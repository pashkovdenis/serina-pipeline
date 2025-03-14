using Serina.Semantic.Ai.Pipelines.ValueObject;

namespace Serina.Semantic.Ai.Pipelines.Models
{

    public record RequestMessage(
    string Content,
    MessageRole Role,
    Guid ChatId,
    string model = "glm4",
    double Temperature = 0.3,
    RequestMessage[] History = null,
    MessagePayload Payload = null,
    string ServiceId = null)
    {
        public string GetLastMessage(MessageRole role = MessageRole.User)
        {
            if (Content != null) return Content;

            if (History != null && History.Any(x => x.Role == role))
            {
                return History.Last(r => r.Role == role).Content;
            }
            return string.Empty;
        }

        // Clone method to create a copy with optional custom values
        public RequestMessage CloneRecord(
            string content = null,
            MessageRole? role = null,
            Guid? chatId = null,
            string model = null,
            double? temperature = null,
            RequestMessage[] history = null,
            MessagePayload payload = null,
            string serviceId = null)
        {
            return new RequestMessage(
                Content: content ?? Content,
                Role: role ?? Role,
                ChatId: chatId ?? ChatId,
                model: model ?? this.model,
                Temperature: temperature ?? Temperature,
                History: history ?? History,
                Payload: payload ?? Payload,
                ServiceId: serviceId ?? ServiceId
            );
        }
    }

}
