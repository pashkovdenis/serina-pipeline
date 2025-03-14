using Microsoft.Extensions.DependencyInjection;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.ServiceSelectors;
using Serina.Semantic.Ai.Pipelines.Utils;
using System.Text.Json;

namespace Serina.Semantic.Ai.Pipelines.Options
{
    public interface IPipelineBuilderService
    {
        IPipelineStep Build(string json);
    }

    public sealed class PipelineBuilderService : IPipelineBuilderService
    {
        private readonly IServiceProvider _serviceProvider;

        public PipelineBuilderService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IPipelineStep Build(string json)
        {
            using var scope = _serviceProvider.CreateScope();

            var config = JsonSerializer.Deserialize<PipelineConfiguration>(json);

            if (config == null)
            {
                throw new ArgumentException("Invalid pipeline configuration JSON");
            }

            var builder = PipelineBuilder.New();


            foreach (var step in config.Config.Steps)
            {
                var stepObj = scope.ServiceProvider.GetRequiredService(Type.GetType(step.Type)!);

                builder.SetNext(stepObj as IPipelineStep);
                builder.WithDefaultRetryHandler(step.KernelOptions.SemanticOptions);
                builder.WithKernel(step.KernelOptions.SemanticOptions, step.KernelOptions.UseRandom ? new RandomServiceSelector() : null);

                builder.AttachKernel();

                if (step.KernelOptions.Plugins != default && step.KernelOptions.Plugins.Any())
                {
                    foreach (var pType in step.KernelOptions.Plugins)
                    {
                        builder.WithPlugin(scope.ServiceProvider.GetRequiredService(Type.GetType(pType)!));
                    }
                }

                if (step.Filters != null && step.Filters.Any())
                {
                    foreach (var fi in step.Filters)
                    {
                        var filterObj = scope.ServiceProvider.GetRequiredService(Type.GetType(fi.Type)!) as IMessageFilter;
                        builder.AddFilter(filterObj!);
                    }
                }

                if (step.Workers != default && step.Workers.Any())
                {
                    foreach (var w in step.Workers)
                    {
                        var workerObj = scope.ServiceProvider.GetRequiredService(Type.GetType(w.Type
                            )!) as ISerinaWorker;

                        builder.AddWorker(workerObj);
                    }
                }

                if (step.Reducers != default && step.Reducers.Any())
                {
                    foreach (var reducer in step.Reducers)
                    {
                        var reducerObj = scope.ServiceProvider.GetRequiredService(Type.GetType(reducer)!) as ISerinaReducer;

                        builder.AddReducer(reducerObj);
                    }
                }

                if (step.Config != null)
                {
                    builder.AddConfig(step.Config);
                }
            }

            if (config.Name != default)
            {
                builder.WithName(config.Name);
            }

            return builder.Build();
        }
    }
}
