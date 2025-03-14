using System.ComponentModel;
using System.Reflection;

namespace Serina.Semantic.Ai.Pipelines.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());

            if (field != null)
            {
                var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));

                if (attribute != null)
                {
                    return attribute.Description;
                }
            }
            return value.ToString(); // Return the enum name if no description is found
        }
    }
}
