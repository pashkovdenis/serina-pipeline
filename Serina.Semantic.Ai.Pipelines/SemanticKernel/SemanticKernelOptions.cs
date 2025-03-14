namespace Serina.Semantic.Ai.Pipelines.SemanticKernel
{
    public sealed class SemanticKernelOptions
    {
        public List<SemanticModelOption> Models { get; set; } = new List<SemanticModelOption>();

        public int RequestsPerSecond { get; set; } = 10;

        public List<SemanticModelOption> GetOptionsByTag(string tag)
        {
            return Models.Where(x => x.Tag.Split(',')
            .Contains(tag, StringComparer.InvariantCultureIgnoreCase)).ToList();
        }
    }

    public sealed class SemanticModelOption
    {
        public string Name { get; set; }

        public string Endpoint { get; set; }

        public string FallbackEndpoint { get; set; }

        public string Key { get; set; }

        public string Tag { get; set; }

        public int EngineType { get; set; } = 0;

        public bool IsLocal { get; set; } // indicated if this model for development

        public override string ToString() => $"{Endpoint} - {Name} - {Tag}";
    }
}
