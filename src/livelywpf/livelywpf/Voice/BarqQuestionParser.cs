using System;
using System.Linq;

namespace livelywpf.Voice
{
    // Only wake-word-prefixed information questions can trigger an internet lookup.
    internal static class BarqQuestionParser
    {
        private static readonly string[] English = {
            "search for ", "look up ", "who is ", "what is ", "where is ",
            "when was ", "tell me about ", "find information about ", "how does ", "how to "
        };
        private static readonly string[] Arabic = {
            "ابحث عن ", "دور على ", "معلومات عن ", "مين هو ", "مين هي ", "مين ",
            "ايه هو ", "إيه هو ", "ايه ", "إيه ", "ما هو ", "ما هي ",
            "فين ", "امتى ", "إمتى ", "ازاي ", "إزاي ", "اشرح "
        };

        internal static bool TryExtract(string phrase, out string query)
        {
            query = null;
            if (String.IsNullOrWhiteSpace(phrase)) return false;
            string text = String.Join(" ", phrase.Trim().Split(
                new [] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            string body;
            string[] patterns;
            if (text.StartsWith("barq ", StringComparison.OrdinalIgnoreCase))
            {
                body = text.Substring(5);
                patterns = English;
            }
            else if (text.StartsWith("برق ", StringComparison.Ordinal))
            {
                body = text.Substring(4);
                patterns = Arabic;
            }
            else return false;

            string intent = patterns.FirstOrDefault(x => body.StartsWith(x,
                patterns == English ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
            if (intent == null) return false;

            string term = body.Substring(intent.Length).Trim(' ', '?', '؟', '!', '.', '،', ',');
            if (term.Length < 2 || term.Length > 140) return false;
            query = term; // Not the audio, transcript history, or a device identifier.
            return true;
        }
    }
}
