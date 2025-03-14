using Microsoft.SemanticKernel;
using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.SemanticKernel;

namespace Serina.Semantic.Ai.Pipelines.Interfaces
{
    public interface IPipelineStep
    {
        ValueTask<PipelineContext> ExecuteStepAsync(PipelineContext context, CancellationToken token);

        ValueTask ConfigureAsync(Action<IPipelineStep> config);

        void SetNext(IPipelineStep step);

        void AddMessageFilter(IMessageFilter filter);

        void AcceptKernel(Kernel kernel);

        void SetLimitInSeconds(int limit);

        void AddOptions(InferenceOptions options);

        void AddReducer(ISerinaReducer reducer);

        void AddWorker(ISerinaWorker worker);

        Kernel GetKernel();
        void Configure(object args);
    }
}
