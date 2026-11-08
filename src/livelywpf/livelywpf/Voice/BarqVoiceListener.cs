using System;
using System.Linq;
using System.Speech.Recognition;

namespace livelywpf.Voice
{
    /// <summary>
    /// Opt-in, offline command listener using only the Windows speech recognizers already installed.
    /// Does not run without user activation. Never sends audio to a remote service.
    /// </summary>
    internal sealed class BarqVoiceListener : IDisposable
    {
        private SpeechRecognitionEngine engine;
        private readonly Action<BarqVoiceCommand> onCommand;
        private readonly Action<string> onQuestion;
        internal volatile bool SuppressRecognition;
        internal bool DictationAvailable { get; private set; }
        internal System.Globalization.CultureInfo RecognizerCulture { get; private set; }
        private volatile bool enabled;

        internal BarqVoiceListener(Action<BarqVoiceCommand> onCommand, Action<string> onQuestion)
        {
            if (onCommand == null) throw new ArgumentNullException("onCommand");
            this.onCommand = onCommand;
            this.onQuestion = onQuestion;
        }

        internal string Start()
        {
            if (engine != null) return engine.RecognizerInfo.Culture.DisplayName;
            // Choose Arabic when supported; English is the fallback.
            var installed = SpeechRecognitionEngine.InstalledRecognizers();
            var info = installed.FirstOrDefault(x => x.Culture.TwoLetterISOLanguageName == "ar")
                       ?? installed.FirstOrDefault(x => x.Culture.TwoLetterISOLanguageName == "en");
            if (info == null)
                throw new InvalidOperationException(
                    "No Windows Speech Recognition engine for Arabic or English is installed. " +
                    "Install a speech recognizer and configure a microphone in Windows, then try again.");

            SpeechRecognitionEngine candidate = new SpeechRecognitionEngine(info);
            try
            {
                var choices = new Choices();
                choices.Add(BarqVoiceCommands.PhrasesForCulture(info.Culture));
                var builder = new GrammarBuilder { Culture = info.Culture };
                builder.Append(choices);
                candidate.LoadGrammar(new Grammar(builder) { Name = "BARQ Lively commands" });
                // Dictation is optional: some Windows 8.1 speech engines only accept fixed grammars.
                try
                {
                    candidate.LoadGrammar(new DictationGrammar { Name = "BARQ information questions" });
                    DictationAvailable = true;
                }
                catch (Exception)
                {
                    DictationAvailable = false;
                }
                candidate.SpeechRecognized += Recognized;
                candidate.SetInputToDefaultAudioDevice();
                enabled = true;
                candidate.RecognizeAsync(RecognizeMode.Multiple);
                engine = candidate;
                RecognizerCulture = info.Culture;
                return info.Culture.DisplayName;
            }
            catch
            {
                enabled = false;
                candidate.SpeechRecognized -= Recognized;
                candidate.Dispose();
                throw;
            }
        }

        private void Recognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (!enabled || SuppressRecognition || e.Result == null) return;
            BarqVoiceCommand command;
            if (e.Result.Confidence >= 0.84f &&
                BarqVoiceCommands.TryParse(e.Result.Text, out command))
            {
                onCommand(command);
                return;
            }
            if (DictationAvailable && onQuestion != null &&
                e.Result.Confidence >= 0.62f && e.Result.Grammar != null &&
                e.Result.Grammar.Name == "BARQ information questions")
            {
                string query;
                if (BarqQuestionParser.TryExtract(e.Result.Text, out query))
                    onQuestion(query);
            }
        }

        public void Dispose()
        {
            enabled = false;
            var old = engine;
            engine = null;
            if (old == null) return;
            old.SpeechRecognized -= Recognized;
            try { old.RecognizeAsyncCancel(); }
            catch (InvalidOperationException) { }
            finally { old.Dispose(); }
        }
    }
}
