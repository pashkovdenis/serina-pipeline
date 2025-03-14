using Serina.Semantic.Ai.Pipelines.Interfaces;

namespace Serina.Semantic.Ai.Pipelines.Options
{
    public class PipelineConfiguration : IStorableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime Created { get; set; } = DateTime.Now;

        public string Name { get; set; }

        public Pipeline Config { get; set; }


    }
}
