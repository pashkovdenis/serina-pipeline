using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Text;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070 
namespace Serina.Semantic.Ai.Pipelines.Steps
{

    public sealed class DialogSummaryStep : BaseStep, IPipelineStep
    {
        private int MaxTokensPerParagpraph = 1800;

        private int MaxNumberOfParagraphs = 20;

        private double Temperature = .5d;

        private int MaxSentences = 10;

        public string DefaultSummarizationPrompt =
       $@"
        Provide a concise and complete summarization of the entire dialog that does not exceed 10 sentences

        This summary must always:
        - Consider both user and assistant interactions
        - Maintain continuity for the purpose of further dialog
        - Include details from any existing summary
        - Focus on the most significant aspects of the dialog

        This summary must never:
        - Critique, correct, interpret, presume, or assume
        - Identify faults, mistakes, misunderstanding, or correctness
        - Analyze what has not occurred
        - Exclude details from any existing summary

        If Previous summarization provided take it into account when building summary.

        ``````````````
        ";


        public override ValueTask ConfigureAsync(Action<IPipelineStep> config)
        {
            config.Invoke(this);
            return base.ConfigureAsync(config);
        }

        public override async ValueTask<PipelineContext> ExecuteStepAsync(PipelineContext context, CancellationToken token)
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            await base.ExecuteStepAsync(context, token);

            var paragrpahs = TextChunker.SplitPlainTextLines(context.RequestMessage.GetLastMessage()
                , MaxTokensPerParagpraph).Take(MaxNumberOfParagraphs);

            var overallSummary = "";


            foreach (var paragraph in paragrpahs)
            {
                ChatHistory chatHistory = new ChatHistory();

                chatHistory.AddSystemMessage(DefaultSummarizationPrompt + " " + overallSummary);

                chatHistory.AddUserMessage(paragraph);

                chatHistory = await ExecuteReduceAsync(context, chatHistory, chatService);

                var message = await chatService.GetChatMessageContentAsync(chatHistory, new OpenAIPromptExecutionSettings
                {
                    Temperature = Temperature
                }, kernel: _kernel);

                overallSummary = message.ToString();

            }


            context.Response = new MessageResponse();

            context.Response.AddContent(overallSummary);

            if (_next != null)
            {
                return await _next.ExecuteStepAsync(context, token);
            }

            return context;
        }
    }
}
