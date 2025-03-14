using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serina.Semantic.Ai.Pipelines.Enumerations;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.Steps;
using Serina.Semantic.Ai.Pipelines.ValueObject;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070 

namespace Serina.Semantic.Ai.Pipelines.Steps.Chat
{
    public sealed class SimpleChatStep : BaseStep, IPipelineStep
    {
        public override async ValueTask<PipelineContext> ExecuteStepAsync(PipelineContext context, CancellationToken token)
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();


            await base.ExecuteStepAsync(context, token);


            var chatHistory = new ChatHistory();

            await base.ExecuteStepAsync(context, token);

            if (context.RequestMessage.History != null && context.RequestMessage.History.Any())
            {
                AddMessage(context, chatHistory);

            }
            else
            {



                if (context.RequestMessage.Role == MessageRole.Bot)
                    AddMessage(context.RequestMessage.Content, MessageRole.Bot, chatHistory);

                if (context.RequestMessage.Role == MessageRole.User)
                    AddMessage(context.RequestMessage.Content, MessageRole.User, chatHistory);

                if (context.RequestMessage.Role == MessageRole.System)
                    AddMessage(context.RequestMessage.Content, MessageRole.System, chatHistory);


            }


            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                ToolCallBehavior = context.EnableFunctions ? ToolCallBehavior.AutoInvokeKernelFunctions : null,
                Temperature = context.RequestMessage.Temperature,
                FunctionChoiceBehavior = context.AutoFunction ? FunctionChoiceBehavior.Auto() : null,

            };

            chatHistory = await ExecuteReduceAsync(context, chatHistory, chatService);

            var message = await chatService.GetChatMessageContentAsync(chatHistory, openAIPromptExecutionSettings, kernel: _kernel);

            context.Response = new MessageResponse();

            context.Response.AddContent(message.ToString());

            if (_next != null)
            {
                return await _next.ExecuteStepAsync(context, token);
            }

            return context;
        }






        private void AddMessage(PipelineContext context, ChatHistory chatHistory)
        {
            foreach (var h in context.RequestMessage.History)
            {
                AddMessage(h.Content, h.Role, h.Payload, chatHistory);
            }
        }

        private void AddMessage(string message, MessageRole role, MessagePayload payload = null, ChatHistory chatHistory = null)
        {
            switch (role)
            {
                case MessageRole.Bot:
                    chatHistory.AddAssistantMessage(message);
                    break;

                case MessageRole.User:
                    var userMessage = new ChatMessageContentItemCollection { new TextContent(message) };

                    if (payload?.Type == PayloadType.Image)
                        userMessage.Add(new ImageContent(payload.Uri));

                    chatHistory.AddUserMessage(userMessage);
                    break;

                case MessageRole.System:
                    chatHistory.AddSystemMessage(message);
                    break;
            }
        }
    }
}
