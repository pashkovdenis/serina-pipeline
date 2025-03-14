using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Serina.Semantic.Ai.Pipelines.Utils;
using Serina.Semantic.Ai.Pipelines.ValueObject;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.SemanticKernel;
using Serina.Semantic.Ai.Pipelines.Steps;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.Handlers;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.ServiceSelectors;
using Serina.Semantic.Ai.Pipelines.Models;

namespace Serina.Semantic.Ai.Pipelines.Filters
{

    public sealed class ReduceAndChunkMessageFilter : IMessageFilter
    {
        public int Thresshold { get; set; } = 5000;

        private readonly IOptions<SemanticKernelOptions> _semanticOptions;

        private readonly ILogger _logger;

        private readonly IOptions<SemanticKernelOptions> _modelOptions;

        public ReduceAndChunkMessageFilter(
            IOptions<SemanticKernelOptions> semanticOptions,
            IOptions<SemanticKernelOptions> modelOptions,
            ILogger<ReduceAndChunkMessageFilter> logger
            )
        {
            _semanticOptions = semanticOptions;
            _logger = logger;
            _modelOptions = modelOptions;
        }


        public async ValueTask<string> FilterAsync(string message)
        {
            if (ApproximateTokenCount(message) > Thresshold)
            {
                _logger.LogWarning("Running summary filter for the message " + message);

                var pipeline = PipelineBuilder.New().New(new DialogSummaryStep())
                            .WithDelegateHandler(
                                          new SemanticKernelHandler(_modelOptions.Value!, new HttpClientHandler() { }))
                                         .WithKernel(new SemanticKernelOptions
                                         {
                                             Models = _semanticOptions.Value.Models.ToList()

                                         }, new RandomServiceSelector())

                                         .WithName("summary")
                                         .Build();

                var result = await pipeline.ExecuteStepAsync(new PipelineContext
                {
                    RequestMessage = new RequestMessage(message, MessageRole.User, Guid.NewGuid())
                }, default);

                return result?.Response?.Content ?? message;
            }

            _logger.LogInformation("No summarization required for message filtering");

            return message;
        }


        int ApproximateTokenCount(string text)
        {
            return text.Length / 4;
        }
    }
}
