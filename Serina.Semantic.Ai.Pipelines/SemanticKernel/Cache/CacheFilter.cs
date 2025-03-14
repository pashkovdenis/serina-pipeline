using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0001
namespace Serina.Semantic.Ai.Pipelines.SemanticKernel.Cache
{
    public class CacheBaseFilter
    {
        /// <summary>
        /// Collection/table name in cache to use.
        /// </summary>
        protected const string CollectionName = "llm_responses";

        /// <summary>
        /// Metadata key in function result for cache record id, which is used to overwrite previously cached response.
        /// </summary>
        protected const string RecordIdKey = "CacheRecordId";
    }

    /// <summary>
    /// Filter which is executed during prompt rendering operation.
    /// </summary>
    public sealed class PromptCacheFilter(ISemanticTextMemory semanticTextMemory) : CacheBaseFilter, IPromptRenderFilter
    {
        private const double SimilarityScore = 0.9;
        public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
        {
            // Trigger prompt rendering operation
            await next(context);

            // Get rendered prompt
            var prompt = context.RenderedPrompt!;

            // Search for similar prompts in cache with provided similarity/relevance score
            var searchResult = await semanticTextMemory.SearchAsync(
                CollectionName,
                prompt,
                limit: 1,
                minRelevanceScore: SimilarityScore).FirstOrDefaultAsync();

            // If result exists, return it.
            if (searchResult is not null)
            {
                // Override function result. This will prevent calling LLM and will return result immediately.
                context.Result = new FunctionResult(context.Function, searchResult.Metadata.AdditionalMetadata)
                {
                    Metadata = new Dictionary<string, object?> { [RecordIdKey] = searchResult.Metadata.Id }
                };
            }
        }
    }

    /// <summary>
    /// Filter which is executed during function invocation.
    /// </summary>
    public sealed class FunctionCacheFilter(ISemanticTextMemory semanticTextMemory) : CacheBaseFilter, IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            // Trigger function invocation
            await next(context);

            // Get function invocation result
            var result = context.Result;

            // If there was any rendered prompt, cache it together with LLM result for future calls.
            if (!string.IsNullOrEmpty(context.Result.RenderedPrompt))
            {
                // Get cache record id if result was cached previously or generate new id.
                var recordId = context.Result.Metadata?.GetValueOrDefault(RecordIdKey, Guid.NewGuid().ToString()) as string;

                // Cache rendered prompt and LLM result.
                await semanticTextMemory.SaveInformationAsync(
                    CollectionName,
                    context.Result.RenderedPrompt,
                    recordId!,
                    additionalMetadata: result.ToString());
            }
        }
    }




}
