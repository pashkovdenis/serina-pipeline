using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Redis;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory;

using System.Net;

using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Configuration;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.Cache;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.SemanticKernel;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.Handlers;


#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0020

namespace Serina.Semantic.Ai.Pipelines.Utils
{
    public sealed class PipelineBuilder
    {
        public static PipelineBuilder New() => new PipelineBuilder();

        private const int MaxTimeoutInSeconds = 600;

        private IPipelineStep Step;
        private IPipelineStep _initialStep;
        private IKernelBuilder _kernelBuilder;
        private DelegatingHandler _delegateHandler;
        private int _limit;
        private string _name;

        public bool _useLocalTextStore { get; set; }


        public PipelineBuilder New(IPipelineStep step)
        {
            Step = step;
            _initialStep = step;
            return this;
        }

        public PipelineBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public PipelineBuilder WithKernel(SemanticKernelOptions kernelOptions, IAIServiceSelector serviceSelector = null)
        {
            var builder = Kernel.CreateBuilder();
            var modelOption = kernelOptions.Models.First();

            //builder.Services.AddSingleton<IFunctionInvocationFilter>(new RetryFilter("FallbackModelId"));

            builder.Services.ConfigureHttpClientDefaults(c =>
            {

                c.ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(300));

                // Use a standard resiliency policy, augmented to retry on 401 Unauthorized, 502 Bad Gateway and on timeout for this example
                c.AddStandardResilienceHandler(o =>
                {
                    // Combine checks for status codes and timeout into one delegate to set as the ShouldHandle policy
                    o.Retry.ShouldHandle = args =>
                    {
                        // Check if the response has a status code that we should handle
                        var isStatusCodeToRetry = args.Outcome.Result?.StatusCode is HttpStatusCode.Unauthorized
                                                  || args.Outcome.Result?.StatusCode is HttpStatusCode.BadGateway;

                        // Check if the result was a timeout (typically when the result is null and an exception is a TaskCanceledException)
                        var isTimeout = args.Outcome.Exception?.InnerException is TaskCanceledException;

                        // Combine the checks
                        return ValueTask.FromResult(isStatusCodeToRetry || isTimeout);
                    };

                    // Configure retry with exponential backoff strategy
                    o.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                    o.Retry.Delay = TimeSpan.FromSeconds(30);
                    o.Retry.MaxRetryAttempts = 5;
                });
            });

            if (kernelOptions.Models.Count == 1)
            {
                AddChatCompletionByType(modelOption.Endpoint, modelOption.Name, modelOption.Key, (EngineType)modelOption.EngineType, builder);

            }
            else
            {
                foreach (var model in kernelOptions.Models)
                {
                    AddChatCompletionByType(model.Endpoint, model.Name, model.Key, (EngineType)model.EngineType, builder);
                }
            }

            if (serviceSelector != null)
            {
                builder.Services.AddSingleton(serviceSelector);
            }

            _kernelBuilder = builder;

            return this;
        }

        public PipelineBuilder WithDelegateHandler(DelegatingHandler handler)
        {
            _delegateHandler = handler;
            return this;
        }


        public PipelineBuilder AddOllamaChatCompletion(string endpoint, string model)
        {
            _kernelBuilder.AddOllamaChatCompletion(model, new Uri(endpoint));

            return this;
        }

        public PipelineBuilder AddWorker(ISerinaWorker worker)
        {
            Step.AddWorker(worker);

            return this;
        }


        public PipelineBuilder WithModelSelectStrategy(IAIServiceSelector serviceSelector)
        {
            if (_kernelBuilder == null)
            {
                throw new InvalidOperationException("Kernel is not added");
            }

            _kernelBuilder?.Services.AddSingleton(serviceSelector);

            return this;
        }

        public PipelineBuilder SetNext(IPipelineStep step)
        {

            if (Step == null)
            {
                Step = step;
                _initialStep = step;
                return this;

            }

            Step.SetNext(step);
            Step = step;

            return this;

        }

        public PipelineBuilder WithLimit(int limit)
        {

            _limit = limit;

            Step.SetLimitInSeconds(_limit);

            return this;

        }

        public PipelineBuilder AddFilter(IMessageFilter filter)
        {
            Step.AddMessageFilter(filter);

            return this;
        }

        public PipelineBuilder UseLocalTextStore()
        {
            _useLocalTextStore = true;
            return this;
        }






        public PipelineBuilder AttachKernel()
        {
            var kernel = _kernelBuilder.Build();

            if (_useLocalTextStore)
            {
                var memoryStore = new VolatileMemoryStore();
                var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
                var semanticTextMemory = new SemanticTextMemory(memoryStore, embeddingService);
                kernel.ImportPluginFromObject(new TextMemoryPlugin(semanticTextMemory));
            }

            Step.AcceptKernel(kernel);

            if (Config != null)
            {
                Step.Configure(Config);
                Config = null;
            }

            return this;
        }


        public Kernel GetKernel()
        {
            var kernel = _kernelBuilder.Build();

            if (_useLocalTextStore)
            {
                var memoryStore = new VolatileMemoryStore();
                var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
                var semanticTextMemory = new SemanticTextMemory(memoryStore, embeddingService);
                kernel.ImportPluginFromObject(new TextMemoryPlugin(semanticTextMemory));
            }


            return kernel;

        }

        public IPipelineStep Build()
        {

            if (_name != default)
            {
                PipelineRegistry.Add(_name, _initialStep);
            }




            return _initialStep;
        }

        public PipelineBuilder WithCache(string endpoint, string embedingModel)
        {
            // Add Azure OpenAI text embedding generation service
            _kernelBuilder.AddOllamaTextEmbeddingGeneration(embedingModel, new Uri(endpoint));

            // Add memory store for caching purposes (e.g. in-memory, Redis, Azure Cosmos DB)
            _kernelBuilder.Services.AddSingleton<IMemoryStore>(_ => new VolatileMemoryStore());

            // Add text memory service that will be used to generate embeddings and query/store data. 
            _kernelBuilder.Services.AddSingleton<ISemanticTextMemory, SemanticTextMemory>();

            // Add prompt render filter to query cache and check if rendered prompt was already answered.
            _kernelBuilder.Services.AddSingleton<IPromptRenderFilter, PromptCacheFilter>();

            // Add function invocation filter to cache rendered prompts and LLM results.
            _kernelBuilder.Services.AddSingleton<IFunctionInvocationFilter, FunctionCacheFilter>();


            return this;
        }


        public PipelineBuilder WithRedisCache(string endpoint, string embedingModel)
        {
            // Add Azure OpenAI text embedding generation service
            _kernelBuilder.AddOllamaTextEmbeddingGeneration(embedingModel, new Uri(endpoint));

            // Add memory store for caching purposes (e.g. in-memory, Redis, Azure Cosmos DB)
            _kernelBuilder.Services.AddSingleton<IMemoryStore>(_ => new RedisMemoryStore(endpoint, vectorSize: 1536));

            // Add text memory service that will be used to generate embeddings and query/store data. 
            _kernelBuilder.Services.AddSingleton<ISemanticTextMemory, SemanticTextMemory>();

            // Add prompt render filter to query cache and check if rendered prompt was already answered.
            _kernelBuilder.Services.AddSingleton<IPromptRenderFilter, PromptCacheFilter>();

            // Add function invocation filter to cache rendered prompts and LLM results.
            _kernelBuilder.Services.AddSingleton<IFunctionInvocationFilter, FunctionCacheFilter>();


            return this;
        }

        public PipelineBuilder WithPlugin(object plugin, string name = null)
        {
            _kernelBuilder.Plugins.AddFromObject(plugin, name);
            return this;
        }


        public PipelineBuilder WithDefaultRetryHandler(SemanticKernelOptions opts)
        {
            WithDelegateHandler(new SemanticKernelHandler(opts, new HttpClientHandler() { }));

            return this;
        }

        private void AddChatCompletionByType(string endpoint, string model, string key, EngineType type, IKernelBuilder builder)
        {
            var httpClient = new HttpClient(new ProxyOpenAIHandler(endpoint, key));

            httpClient.Timeout = TimeSpan.FromSeconds(MaxTimeoutInSeconds);

            switch (type)
            {
                case EngineType.OpenAi:



                    builder.AddOpenAIChatCompletion(model, key, serviceId: model, httpClient: _delegateHandler != default ? new HttpClient(_delegateHandler) { Timeout = TimeSpan.FromSeconds(100) } : null);


                    break;

                case EngineType.Azure:


                    builder.AddAzureOpenAIChatCompletion(model, endpoint, key, httpClient: _delegateHandler != default ? new HttpClient(_delegateHandler) { Timeout = TimeSpan.FromSeconds(100) } : null);

                    break;


                default:

                    builder.AddOllamaChatCompletion(model,
                        httpClient: _delegateHandler != default ? new HttpClient(_delegateHandler)
                        {
                            Timeout = TimeSpan.FromSeconds(100),
                            BaseAddress = new Uri(endpoint)
                        } :
                        new HttpClient() { BaseAddress = new Uri(endpoint) }, model);

                    break;
            }
        }

        public PipelineBuilder AddReducer(ISerinaReducer reducer)
        {
            Step.AddReducer(reducer);

            return this;
        }

        public PipelineBuilder AddOptions(InferenceOptions options)
        {
            Step.AddOptions(options);

            return this;

        }

        private object Config;

        public PipelineBuilder AddConfig(object cfg)
        {
            Config = cfg;

            return this;
        }


        public PipelineBuilder WithKernelMemoryOllama(string textModel, string embedingModel, string endPoint, TagCollection tags = null, string qdrantHost = null)
        {
            var config = new OllamaConfig
            {
                Endpoint = endPoint,
                TextModel = new OllamaModelConfig(textModel, 5000),
                EmbeddingModel = new OllamaModelConfig(embedingModel, 2048),

            };

            var memoryBuilder = new KernelMemoryBuilder()

                .WithOllamaTextGeneration(config, new CL100KTokenizer())
                .WithOllamaTextEmbeddingGeneration(config, new CL100KTokenizer())
                
                .Configure(builder => builder.Services.AddLogging(l =>
                {
                    l.AddDebug();
                    l.AddConsole();
                }));



            if (qdrantHost != default)
            {
                memoryBuilder
                        .WithQdrantMemoryDb(new QdrantConfig
                        {
                            Endpoint = qdrantHost
                        });
            }

            var memory = memoryBuilder.Build();


            var plugin = new MemoryPlugin(memory, waitForIngestionToComplete: true, defaultIngestionTags: tags, defaultRetrievalTags: tags);

            if (_name != default)
            {
                MemoryRegister.Add(_name, memory);
            }
            else
            {
                if (!MemoryRegister.Exists(endPoint))
                {
                    MemoryRegister.Add(endPoint, memory);
                }
            }

            return WithPlugin(plugin, "memory");
        }

        public PipelineBuilder WithKernelMemoryAzure(string textModel, string embedingModel, string endPoint, string key,
            TagCollection tags = null, string qdrantHost = null)
        {

            var azureConfigEmbeding = new AzureOpenAIConfig
            {
                Endpoint = endPoint,
                APIKey = key,
                Deployment = textModel,
                Auth = AzureOpenAIConfig.AuthTypes.APIKey,
                APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,

            };

            var azureConfigText = new AzureOpenAIConfig
            {
                Endpoint = endPoint,
                APIKey = key,
                Deployment = textModel,
                Auth = AzureOpenAIConfig.AuthTypes.APIKey,
                APIType = AzureOpenAIConfig.APITypes.TextCompletion,

            };

            var memoryBuilder = new KernelMemoryBuilder()

                .WithAzureOpenAITextGeneration(azureConfigText)
                .WithAzureOpenAITextEmbeddingGeneration(azureConfigEmbeding)
                   .WithCustomTextPartitioningOptions(
                        new TextPartitioningOptions
                        {
                            MaxTokensPerParagraph = 1000,
                            OverlappingTokens = 50
                        })
                .Configure(builder => builder.Services.AddLogging(l =>
                {
                    l.AddDebug();
                    l.AddConsole();
                }));



            if (qdrantHost != default)
            {
                memoryBuilder
                        .WithQdrantMemoryDb(new QdrantConfig
                        {
                            Endpoint = qdrantHost
                        });
            }

            var memory = memoryBuilder.Build();


            var plugin = new MemoryPlugin(memory,
                waitForIngestionToComplete: true,
                defaultIngestionTags: tags,
                defaultRetrievalTags: tags);

            MemoryRegister.Add(_name, memory);

            return WithPlugin(plugin, "memory");
        }

        public PipelineBuilder WithKernelOpenAi(string model, string key)
        {
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(model, key, serviceId: model);

            _kernelBuilder = builder;

            return this;
        }

    }
}
