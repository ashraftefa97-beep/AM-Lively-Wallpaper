using System;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;

namespace livelywpf.Voice
{
    // Speech output uses only the installed Windows voices (no Windows 10-only voice API).
    internal sealed class BarqVoiceSpeaker : IDisposable
    {
        private readonly SpeechSynthesizer synth;
        private readonly Action<bool> suppressRecognition;
        private bool disposed;
        internal bool SpeaksArabic { get; private set; }

        internal BarqVoiceSpeaker(CultureInfo recognizerLanguage, Action<bool> suppressRecognition)
        {
            this.suppressRecognition = suppressRecognition;
            synth = new SpeechSynthesizer();
            try
            {
                var voices = synth.GetInstalledVoices().Where(x => x.Enabled).ToList();
                if (voices.Count == 0)
                    throw new InvalidOperationException(
                        "No Windows text-to-speech voice is installed. Configure a local voice first.");
                string lang = recognizerLanguage.TwoLetterISOLanguageName;
                var selected = voices.FirstOrDefault(x =>
                        x.VoiceInfo.Culture.TwoLetterISOLanguageName == lang &&
                        x.VoiceInfo.Gender == VoiceGender.Male)
                    ?? voices.FirstOrDefault(x => x.VoiceInfo.Culture.TwoLetterISOLanguageName == lang)
                    ?? voices.FirstOrDefault(x => x.VoiceInfo.Culture.TwoLetterISOLanguageName == "en")
                    ?? voices.First();
                synth.SelectVoice(selected.VoiceInfo.Name);
                SpeaksArabic = selected.VoiceInfo.Culture.TwoLetterISOLanguageName == "ar";
                synth.SetOutputToDefaultAudioDevice();
                synth.Rate = 0;
                synth.Volume = 90;
                synth.SpeakCompleted += (s, e) =>
                {
                    if (!disposed && synth.State != SynthesizerState.Speaking)
                        suppressRecognition(false);
                };
            }
            catch { synth.Dispose(); throw; }
        }

        internal void Say(string english, string arabic = null)
        {
            if (disposed) return;
            string line = SpeaksArabic && !String.IsNullOrWhiteSpace(arabic) ? arabic : english;
            if (String.IsNullOrWhiteSpace(line)) return;
            try
            {
                suppressRecognition(true);
                synth.SpeakAsyncCancelAll();
                synth.SpeakAsync(line);
            }
            catch
            {
                suppressRecognition(false);
                throw;
            }
        }

        internal void SayAnswer(BarqWebAnswer result)
        {
            if (result.Arabic && !SpeaksArabic)
            {
                Say("I found a result but the Arabic text to speech voice is not installed. " +
                    "You can read the result in the notification.");
                return;
            }
            Say("According to " + result.Provider + ": " + result.Text,
                "حسب " + result.Provider + ": " + result.Text);
        }

        public void Dispose()
        {
            disposed = true;
            try { synth.SpeakAsyncCancelAll(); }
            catch (InvalidOperationException) { }
            finally { synth.Dispose(); suppressRecognition(false); }
        }
    }
}
