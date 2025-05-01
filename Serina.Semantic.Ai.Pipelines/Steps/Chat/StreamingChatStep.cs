using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp.Models.Chat;
using OpenAI.Chat;
using Serina.Semantic.Ai.Pipelines.Enumerations;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.ValueObject;
using System.Diagnostics;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070 

namespace Serina.Semantic.Ai.Pipelines.Steps.Chat
{

    public sealed class StreamingChatStep : BaseStep, IPipelineStep
    {
        private readonly IPipelineStream _pipeStream;

        public StreamingChatStep(IPipelineStream pipeStream)
        {
            _pipeStream = pipeStream;
        }

        public override async ValueTask<PipelineContext> ExecuteStepAsync(PipelineContext context, CancellationToken token)
        {

            await base.PrepareHistoryAsync(context);

            await base.ExecuteStepAsync(context, token);

          
            

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();


            // create inference options 
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = _inferenceOptions?.ToExecutionSettings() ?? new()
            {
                ToolCallBehavior = context.EnableFunctions ? ToolCallBehavior.AutoInvokeKernelFunctions : null,
                Temperature = context.RequestMessage.Temperature,
                ModelId = context.RequestMessage.model ?? null,
                FunctionChoiceBehavior = context.AutoFunction ? FunctionChoiceBehavior.Auto(options: new FunctionChoiceBehaviorOptions
                {
                    AllowConcurrentInvocation = true,
                    AllowParallelCalls = false
                }) : null,

               
            };
            


            context.ChatHistory = await ExecuteReduceAsync(context, context.ChatHistory, chatService);


            await foreach (var chatUpdate in chatService.GetStreamingChatMessageContentsAsync(context.ChatHistory,
                openAIPromptExecutionSettings, kernel: _kernel))
            {
                var messageResponse = GetMessageResponseFromUpdate(chatUpdate);

                await _pipeStream.WriteChunk(messageResponse, context.Id);

                Debug.WriteLine(messageResponse.Content);

                if (messageResponse.IsDone)
                {
                    _pipeStream.Complete(context.Id);

                    break;
                }
            }
             
            if (_next != null)
            {
                return await _next.ExecuteStepAsync(context, token);
            }


            return context;
        }


        private MessageResponse GetMessageResponseFromUpdate(StreamingChatMessageContent updates)
        {
            if (updates.InnerContent is StreamingChatCompletionUpdate openAiResponse)
            {

                var update = openAiResponse.ContentUpdate.LastOrDefault();

                var messageResponse = new MessageResponse();

                messageResponse.AddContent(update?.Text ?? "");

                if (update != null && update.Kind == ChatMessageContentPartKind.Image)
                {
                    messageResponse.Payload = new MessagePayload
                    {
                        Type = PayloadType.Image,
                        Data = update.ImageBytes.ToArray(),
                        ContentType = "image/png",
                        Uri = update.ImageUri.OriginalString
                    };
                }

                if (openAiResponse.Usage is not null)
                {
                    messageResponse.TotalTokens = openAiResponse.Usage.TotalTokenCount;
                }

                if (openAiResponse.FinishReason == ChatFinishReason.Stop)
                {
                    messageResponse.IsDone = true;
                }

                return messageResponse;
            }


            if (updates.InnerContent is ChatResponseStream ollamaUpdate)
            {

                var messageResponse = new MessageResponse();

                messageResponse.AddContent(ollamaUpdate?.Message?.Content ?? "");

                if (ollamaUpdate?.Message?.Images != default)
                {
                    messageResponse.Payload = new MessagePayload
                    {
                        Type = PayloadType.Image,
                        // Data = update.ImageBytes.ToArray(),
                        ContentType = "image/png",
                        Uri = ollamaUpdate?.Message?.Images[0]
                    };
                }

                messageResponse.IsDone = ollamaUpdate.Done;
                return messageResponse;
            }



            return default;
        }


    

    }
}
