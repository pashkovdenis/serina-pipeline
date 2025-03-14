using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.SemanticKernel;
using Serina.Semantic.Ai.Pipelines.ValueObject;

#pragma warning disable SKEXP0001  
#pragma warning disable SKEXP0050 
#pragma warning disable SKEXP0070 
namespace Serina.Semantic.Ai.Pipelines.Steps
{
    public abstract class BaseStep
    {
        protected Kernel _kernel;
        protected KernelPlugin _prompts;
        protected List<IMessageFilter> _filters = new List<IMessageFilter>();
        protected IPipelineStep _next;
        protected int _limit;

        protected InferenceOptions _inferenceOptions;

        protected List<ISerinaReducer> _reducers = new List<ISerinaReducer>();
        protected List<ISerinaWorker> _workers = new List<ISerinaWorker>();

        public virtual void AcceptKernel(Kernel kernel)
        {
            _kernel = kernel;
        }

        public virtual Kernel GetKernel() => _kernel;

        public virtual void AddOptions(InferenceOptions options) => _inferenceOptions = options;

        public virtual void AddReducer(ISerinaReducer reducer) => _reducers.Add(reducer);

        public virtual void AddMessageFilter(IMessageFilter filter) => _filters.Add(filter);

        public virtual ValueTask ConfigureAsync(Action<IPipelineStep> config)
        {
            return ValueTask.CompletedTask;
        }

        public virtual void Configure(object args)
        {
        }

        public void AddWorker(ISerinaWorker worker) => _workers.Add(worker);

        public virtual void SetLimitInSeconds(int limit)
        {
            _limit = limit * 1000;
        }

        public virtual void AddMessage(string message, MessageRole role, ChatHistory chatHistory)
        {
            if (role == MessageRole.Bot)
                chatHistory.AddAssistantMessage(message);

            if (role == MessageRole.User)
                chatHistory.AddUserMessage(message);

            if (role == MessageRole.System)
                chatHistory.AddSystemMessage(message);
        }

        public virtual async ValueTask<ChatHistory> ExecuteReduceAsync(
            PipelineContext context, 
            ChatHistory original, 
            IChatCompletionService chatService)
        {
            ChatHistory chatHistory = null;

            if (_reducers.Any())
            {
                foreach (var reducer in _reducers)
                {
                    chatHistory = await reducer.ReduceHistory(context, chatHistory ?? original, chatService);
                }
            }

            return chatHistory ?? original;
        }

        public virtual void SetNext(IPipelineStep step) => _next = step;

        public virtual async ValueTask<PipelineContext> ExecuteStepAsync(PipelineContext context, CancellationToken token)
        {
            if (_limit > 0)
            {
                await Task.Delay(_limit);
            }

            if (_filters.Any())
            {
                foreach (var filter in _filters)
                {
                    context.RequestMessage = context.RequestMessage
                        .CloneRecord(content: await filter.FilterAsync(context.RequestMessage.Content));

                }
            }

            if (_workers.Any())
            {
                foreach (var worker in _workers)
                {
                    await worker.TickAsync(context);
                }

            }

            return context;
        }

    }
}
