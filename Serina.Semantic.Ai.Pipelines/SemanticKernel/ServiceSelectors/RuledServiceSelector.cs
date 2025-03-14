using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel;
using System.Diagnostics.CodeAnalysis;

namespace Serina.Semantic.Ai.Pipelines.SemanticKernel.ServiceSelectors
{

    public sealed class RuledServiceSelector : IAIServiceSelector
    {
        public bool TrySelectAIService<T>(
        Kernel kernel, KernelFunction function, KernelArguments arguments,
        [NotNullWhen(true)] out T? service, out PromptExecutionSettings? serviceSettings) where T : class, IAIService
        {
            var allServices = kernel.GetAllServices<T>().ToList();
            service = null;
            serviceSettings = null;

            if (allServices.Count == 0)
            {
                return false; // No services available
            }

            if (arguments != null && arguments.ContainsName("service"))
            {
                var requiredService = arguments["service"] as string;
                var serivce = allServices.FirstOrDefault(s => s.Attributes.ContainsKey("ModelId")
                && s.Attributes["ModelId"]!.ToString() == requiredService);

                if (serivce != null)
                {
                    service = serivce;
                }
            }

            return service != null;
        }
    }
}
