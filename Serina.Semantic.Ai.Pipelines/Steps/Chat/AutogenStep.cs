using AutoGen.Core;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Serina.Semantic.Ai.Pipelines.Enumerations;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.ValueObject;

namespace Serina.Semantic.Ai.Pipelines.Steps.Chat
{
    public sealed class AutogenStep : BaseStep, IPipelineStep
    {

        private GroupChat Chat;
        private readonly IPipelineStream _pipeStream;
        private string TerminationCode;
        private int MaxRound = 10; 

        public AutogenStep(IPipelineStream pipeStream)
        {
            _pipeStream = pipeStream;
        }

        public override async ValueTask<PipelineContext> ExecuteStepAsync(PipelineContext context, CancellationToken token)
        {
            await base.ExecuteStepAsync(context, token);

            var taskMessage = new MessageEnvelope<ChatMessageContent>(new ChatMessageContent(AuthorRole.User, context.RequestMessage.Content)
            {
                
            }, from: "User");

            var closed = false;


            await foreach (var message in Chat.SendAsync([taskMessage], maxRound: MaxRound))
            { 
                var content = "";

                if (message is MessageEnvelope<ChatMessageContent> envm)
                {
                    content = envm.Content.ToString();
                }

                if (message is TextMessage txtm)
                {
                    content = txtm.GetContent();
                }
                 
                var part = new MessageResponse();

                part.AddContent(content ?? ""); 

                await _pipeStream.WriteChunk(part, context.Id);

                if (content.Contains(TerminationCode))
                {
                    _pipeStream.Complete(context.Id);

                    closed = true;
                    break;
                }
                
            }

            if (!closed)
            {
                _pipeStream.Complete(context.Id);
            }

            if (_next != null)
            {
                return await _next.ExecuteStepAsync(context, token);
            }

            return context;
        }

        public override void Configure(object args)
        {
            
            if (args is AutogenGroupChatConfig cfg)
            {
                Chat = cfg.Chat;
                TerminationCode = cfg.TerminationCode;
                MaxRound = cfg.MaxRound;
                return;
            }

            throw new InvalidOperationException("AutogenGroupChatConfig expected");

        }

        public sealed class AutogenGroupChatConfig()
        {
            public GroupChat Chat { get; set; }

            public string TerminationCode { get; set; }

            public int MaxRound { get; set; } = 10; 


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
