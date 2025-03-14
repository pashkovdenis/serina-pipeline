using Serina.Semantic.Ai.Pipelines.Interfaces;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Serina.Semantic.Ai.Pipelines.Filters
{
    public sealed class ClearTextFilter : IMessageFilter
    {
        private const int MaxMessageLength = 4096;

        private static readonly Regex UnwantedSymbolsRegex = new(
                   @"[^\p{L}\p{N}\s.,!?;:'""()\-\/:]", // Includes Hebrew (\p{L}) implicitly 
                   RegexOptions.Compiled);

        private static readonly Regex MultipleSpacesRegex = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex MultipleLineBreaksRegex = new(@"(\r\n|\r|\n)+", RegexOptions.Compiled);
        private static readonly Regex RepeatedPunctuationRegex = new(@"([!?.,])\1+", RegexOptions.Compiled);


        public ValueTask<string> FilterAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new ValueTask<string>(string.Empty);
            }

            string cleanedMessage = UnwantedSymbolsRegex.Replace(message, string.Empty)
                                                         .Normalize(NormalizationForm.FormC);
            cleanedMessage = MultipleSpacesRegex.Replace(cleanedMessage, " ");
            cleanedMessage = MultipleLineBreaksRegex.Replace(cleanedMessage, "\n");
            cleanedMessage = RepeatedPunctuationRegex.Replace(cleanedMessage, "$1");
            cleanedMessage = cleanedMessage.Trim();

            if (cleanedMessage.Length > MaxMessageLength)
            {
                int charsToKeep = MaxMessageLength / 2;
                string startPart = cleanedMessage.Substring(0, charsToKeep);
                string endPart = cleanedMessage[^charsToKeep..];
                cleanedMessage = $"{startPart}...{endPart}";
            }

            return new ValueTask<string>(cleanedMessage);
        }
    }
}
