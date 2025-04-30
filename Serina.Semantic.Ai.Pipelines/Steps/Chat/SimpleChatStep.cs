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
            
            await base.PrepareHistoryAsync(context);

            await base.ExecuteStepAsync(context, token);

        
            await base.ExecuteStepAsync(context, token);
  
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                ToolCallBehavior = context.EnableFunctions ? ToolCallBehavior.AutoInvokeKernelFunctions : null,
                Temperature = context.RequestMessage.Temperature,
                FunctionChoiceBehavior = context.AutoFunction ? FunctionChoiceBehavior.Auto() : null, 
            };

            context.ChatHistory = await ExecuteReduceAsync(context, context.ChatHistory, chatService);

            var message = await chatService.GetChatMessageContentAsync(context.ChatHistory, openAIPromptExecutionSettings, kernel: _kernel);

            context.Response = new MessageResponse();

            context.Response.AddContent(message.ToString());

            if (_next != null)
            {
                return await _next.ExecuteStepAsync(context, token);
            }

            return context;
        } 
    }
}
