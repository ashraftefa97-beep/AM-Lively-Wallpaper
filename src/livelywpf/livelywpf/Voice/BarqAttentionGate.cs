using System;

namespace livelywpf.Voice
{
    /// <summary>
    /// Lightweight wake-word / follow-up gate. This is not speaker identification.
    /// No timer threads or continuous polling; expiry is checked when speech is recognized.
    /// </summary>
    internal sealed class BarqAttentionGate
    {
        private DateTime awakeUntilUtc = DateTime.MinValue;
        internal readonly TimeSpan FollowUp = TimeSpan.FromSeconds(16);

        internal bool IsAwake(DateTime utc) { return utc <= awakeUntilUtc; }
        internal void Arm(DateTime utc) { awakeUntilUtc = utc.Add(FollowUp); }
        internal void Extend(DateTime utc)
        {
            if (IsAwake(utc)) awakeUntilUtc = utc.Add(FollowUp);
        }
        internal void Reset() { awakeUntilUtc = DateTime.MinValue; }

        internal bool TryHear(string speech, float confidence, bool speechDetected,
                              int peakLevel, double noiseFloor, DateTime utc,
                              out string content, out bool wakeOnly)
        {
            content = null;
            wakeOnly = false;
            if (String.IsNullOrWhiteSpace(speech) || !speechDetected || confidence < 0.66f)
                return false;
            // Accept exceptionally clear quiet speech; require speech energy above the measured
            // ambient floor for normal-confidence matches. AudioLevel is 0..100.
            if (peakLevel < Math.Max(5.0, noiseFloor + 4.0) && confidence < 0.90f)
                return false;

            string phrase = speech.Trim();
            string remainder;
            bool hasWake = StripWakeWord(phrase, out remainder);
            if (hasWake)
            {
                if (confidence < 0.78f) return false;
                awakeUntilUtc = utc.Add(FollowUp);
                if (String.IsNullOrWhiteSpace(remainder))
                {
                    wakeOnly = true;
                    return true;
                }
                content = remainder;
                return true;
            }
            if (!IsAwake(utc) || phrase.Length < 2 || phrase.Length > 240)
                return false;
            content = phrase;
            awakeUntilUtc = utc.Add(FollowUp);
            return true;
        }

        internal static bool StripWakeWord(string input, out string remainder)
        {
            remainder = null;
            if (String.IsNullOrWhiteSpace(input)) return false;
            string s = input.Trim();
            foreach (var word in new[] { "barq", "برق", "برء" })
            {
                if (!s.StartsWith(word, word == "barq" ? StringComparison.OrdinalIgnoreCase :
                                                       StringComparison.Ordinal)) continue;
                if (s.Length > word.Length && !Char.IsWhiteSpace(s[word.Length]) &&
                    s[word.Length] != ',' && s[word.Length] != '،' && s[word.Length] != ':')
                    continue;
                remainder = s.Substring(word.Length).Trim(' ', ',', '،', ':', '!', '؟', '?');
                return true;
            }
            return false;
        }
    }
}
