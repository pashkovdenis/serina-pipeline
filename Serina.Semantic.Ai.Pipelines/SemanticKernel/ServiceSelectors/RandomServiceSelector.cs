using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Services;
using System.Diagnostics.CodeAnalysis;

namespace Serina.Semantic.Ai.Pipelines.SemanticKernel.ServiceSelectors
{
    public sealed class RandomServiceSelector : IAIServiceSelector
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

            // Select a random service from the list
            var random = new Random();
            int randomIndex = random.Next(allServices.Count);

            service = allServices[randomIndex];

            return true;
        }
    }
}
