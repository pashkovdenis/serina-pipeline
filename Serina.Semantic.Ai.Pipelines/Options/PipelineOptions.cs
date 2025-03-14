namespace Serina.Semantic.Ai.Pipelines.Options
{
    using Serina.Semantic.Ai.Pipelines.SemanticKernel;
    using System.Collections.Generic;

    public class Pipeline
    {
        public string Name { get; set; }
        public List<Step> Steps { get; set; }
    }

    public class Step
    {
        public KernelOptions KernelOptions { get; set; }
        public string Type { get; set; }
        public List<Worker> Workers { get; set; }
        public List<Filter> Filters { get; set; }
        public List<string> Reducers { get; set; }
        public object Config { get; set; }

        public int MemoryMode { get; set; } = 0;

    }

    public class KernelOptions
    {
        public SemanticKernelOptions SemanticOptions { get; set; }
        public List<string> Plugins { get; set; }
        public bool UseRandom { get; set; }
    }

    public class Worker
    {
        public string Type { get; set; }
    }

    public class Filter
    {
        public string Type { get; set; }
    }
}
