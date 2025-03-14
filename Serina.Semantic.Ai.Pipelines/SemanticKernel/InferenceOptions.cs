using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Serina.Semantic.Ai.Pipelines.SemanticKernel
{
    public sealed class InferenceOptions
    {
        public double Temperature { get; }
        public bool AutoFunctions { get; }
        public bool EnableFunctions { get; }
        public double? PresencePenalty { get; }
        public double? FrequencyPenalty { get; }
        public int? MaxTokens { get; }
        public IList<string>? StopSequences { get; }
        public long? Seed { get; }
        public string? ChatSystemPrompt { get; }
        public IDictionary<int, int>? TokenSelectionBiases { get; }

        public InferenceOptions(
            double temperature,
            bool autoFunctions,
            bool enableFunctions,
            double? presencePenalty = null,
            double? frequencyPenalty = null,
            int? maxTokens = null,
            IList<string>? stopSequences = null,
            long? seed = null,
            string? chatSystemPrompt = null,
            IDictionary<int, int>? tokenSelectionBiases = null)
        {
            Temperature = temperature;
            AutoFunctions = autoFunctions;
            EnableFunctions = enableFunctions;
            PresencePenalty = presencePenalty;
            FrequencyPenalty = frequencyPenalty;
            MaxTokens = maxTokens;
            StopSequences = stopSequences;
            Seed = seed;
            ChatSystemPrompt = chatSystemPrompt;
            TokenSelectionBiases = tokenSelectionBiases;
        }

        /// <summary>
        /// Converts the struct into OpenAIPromptExecutionSettings.
        /// </summary>
        public OpenAIPromptExecutionSettings ToExecutionSettings()
        {
            return new OpenAIPromptExecutionSettings
            {
                Temperature = Temperature,
                PresencePenalty = PresencePenalty,
                FrequencyPenalty = FrequencyPenalty,
                MaxTokens = MaxTokens,
                StopSequences = StopSequences,
                Seed = Seed,
                ChatSystemPrompt = ChatSystemPrompt,
                TokenSelectionBiases = TokenSelectionBiases,
                ToolCallBehavior = EnableFunctions ? ToolCallBehavior.AutoInvokeKernelFunctions : null,
                FunctionChoiceBehavior = AutoFunctions ? FunctionChoiceBehavior.Auto() : null,
            };
        }
    }
}
