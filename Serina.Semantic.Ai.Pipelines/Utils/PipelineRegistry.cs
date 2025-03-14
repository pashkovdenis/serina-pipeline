using Serina.Semantic.Ai.Pipelines.Interfaces;

namespace Serina.Semantic.Ai.Pipelines.Utils
{
    public static class PipelineRegistry
    {

        private static Dictionary<string, IPipelineStep> _steps = new Dictionary<string, IPipelineStep>();


        public static bool Exists(string name) => _steps.ContainsKey(name);


        public static IPipelineStep Get(string name) => _steps[name];


        public static void Add(string name, IPipelineStep step) => _steps.Add(name, step);
    }
}
