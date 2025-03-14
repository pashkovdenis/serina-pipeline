using System.ComponentModel;

namespace Serina.Semantic.Ai.Pipelines.ValueObject
{
    public enum MessageRole
    {

        [Description("User")]
        User = -1,

        [Description("Bot")]
        Bot,

        [Description("System")]
        System
    }
}
