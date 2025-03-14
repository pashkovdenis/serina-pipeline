using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents.History;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp.Models.Chat;
using OpenAI.Chat;
using Serina.Semantic.Ai.Pipelines.Enumerations;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.Steps;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0110

namespace Serina.Semantic.Ai.Pipelines.Steps.Chat
{

    public sealed class AgentStreamingChat : BaseStep, IPipelineStep
    {
        private readonly IPipelineStream _pipeStream;

        AgentGroupChat _chat;

        private bool ResetOnInput = false;

        public AgentStreamingChat(IPipelineStream pipeStream)
        {
            _pipeStream = pipeStream;

        }

        public override async ValueTask<PipelineContext> ExecuteStepAsync(PipelineContext context, CancellationToken token)
        {
            context = await base.ExecuteStepAsync(context, token);

            if (_chat == default)
            {
                throw new InvalidOperationException("Chat is not configured");
            }

            if (ResetOnInput)
            {
                await _chat.ResetAsync();
            }


            _chat.AddChatMessage(new Microsoft.SemanticKernel.ChatMessageContent(AuthorRole.User, context.RequestMessage.Content));

            var closed = false;

            await foreach (var chatUpdate in _chat.InvokeStreamingAsync())
            {
                var messageResponse = GetMessageResponseFromUpdate(chatUpdate);

                await _pipeStream.WriteChunk(messageResponse, context.Id);

                if (messageResponse.IsDone)
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

            return context;
        }


        public override void Configure(object args)
        {
            if (args is AgentChatConfiguration cfg)
            {
                cfg.Validate();

                ResetOnInput = cfg.ResetOnInput;

                var agents = new List<ChatCompletionAgent>();

                foreach (var df in cfg.AgentDefinition)
                {
                    var agnt = new ChatCompletionAgent()
                    {
                        Name = df.Key,
                        Instructions = df.Value,
                        Kernel = _kernel.Clone(),
                        Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings()
                        {
                            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new FunctionChoiceBehaviorOptions
                            {
                                AllowConcurrentInvocation = true,
                                AllowParallelCalls = true
                            }),
                        })

                    };

                    agents.Add(agnt);
                }

                if (agents.Count == 1)
                {
                    _chat = new AgentGroupChat([.. agents]);
                    return;
                }

                const string TerminationToken = "yes";
                ChatHistoryTruncationReducer historyReducer = new(cfg.HistoryReducerLimit);

                _chat = new([.. agents])
                {
                    ExecutionSettings = new AgentGroupChatSettings
                    {
                        SelectionStrategy =
                            new KernelFunctionSelectionStrategy(cfg.StrategySelectionFunction, _kernel)
                            {
                                // Always start with the editor agent.
                                InitialAgent = agents.First(),
                                // Save tokens by only including the final response
                                HistoryReducer = historyReducer,
                                // The prompt variable name for the history argument.
                                HistoryVariableName = "lastmessage",
                                // Returns the entire result value as a string.
                                ResultParser = (result) => result.GetValue<string>() ?? agents.First()!.Name
                            },
                        TerminationStrategy =
                            new KernelFunctionTerminationStrategy(cfg.TerminationFunction, _kernel)
                            {
                                // Only evaluate for editor's response
                                Agents = [agents.Last()],
                                // Save tokens by only including the final response
                                HistoryReducer = historyReducer,
                                // The prompt variable name for the history argument.
                                HistoryVariableName = "lastmessage",
                                // Limit total number of turns
                                MaximumIterations = 12,
                                // Customer result parser to determine if the response is "yes"
                                ResultParser = (result) => result.GetValue<string>()?.Contains(TerminationToken, StringComparison.OrdinalIgnoreCase) ?? false
                            }
                    }
                };
            }
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

                messageResponse.AddContent(ollamaUpdate?.Message.Content ?? "");

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



        public sealed class AgentChatConfiguration
        {
            public Dictionary<string, string> AgentDefinition { get; set; } = new Dictionary<string, string>();

            public int HistoryReducerLimit { get; set; } = 1;

            public KernelFunction TerminationFunction { get; set; }

            public KernelFunction StrategySelectionFunction { get; set; }


            public bool ResetOnInput { get; set; } = false;

            public void Validate()
            {
                if (AgentDefinition == null || !AgentDefinition.Any())
                {
                    throw new InvalidOperationException("Agent definition empty");
                }

                if (AgentDefinition.Count > 1 && (TerminationFunction == default || StrategySelectionFunction == default))
                {
                    throw new InvalidOperationException("Termination and Strategy functions are required");
                }
            }
        }
    }
}
