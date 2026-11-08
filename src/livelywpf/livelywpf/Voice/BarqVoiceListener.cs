using System;
using System.Linq;
using System.Speech.Recognition;

namespace livelywpf.Voice
{
    /// <summary>
    /// Local speech attention on Windows 8.1: listens only after opt-in,
    /// and ignores ambient noise, uncertain speech and output from BARQ's own TTS.
    /// Wake word arms a short follow-up window for natural consecutive utterances.
    /// </summary>
    internal sealed class BarqVoiceListener : IDisposable
    {
        private SpeechRecognitionEngine engine;
        private readonly Action<BarqVoiceCommand> onCommand;
        private readonly Action<string> onConversation;
        private readonly Action onWake;
        private readonly BarqAttentionGate attention = new BarqAttentionGate();
        internal volatile bool SuppressRecognition;
        internal bool DictationAvailable { get; private set; }
        internal System.Globalization.CultureInfo RecognizerCulture { get; private set; }
        private volatile bool enabled;
        private bool speechDetected;
        private volatile bool hadWakeInteraction;
        private int peakLevel;
        private double noiseFloor;

        internal BarqVoiceListener(Action<BarqVoiceCommand> onCommand,
                                   Action<string> onConversation,
                                   Action onWake)
        {
            if (onCommand == null) throw new ArgumentNullException("onCommand");
            this.onCommand = onCommand;
            this.onConversation = onConversation;
            this.onWake = onWake;
        }

        internal string Start()
        {
            if (engine != null) return engine.RecognizerInfo.Culture.DisplayName;
            var installed = SpeechRecognitionEngine.InstalledRecognizers();
            var info = installed.FirstOrDefault(x => x.Culture.TwoLetterISOLanguageName == "ar")
                       ?? installed.FirstOrDefault(x => x.Culture.TwoLetterISOLanguageName == "en");
            if (info == null)
                throw new InvalidOperationException(
                    "No installed Arabic/English Windows speech recognizer is available.");

            var candidate = new SpeechRecognitionEngine(info);
            try
            {
                var choices = new Choices();
                choices.Add(BarqVoiceCommands.PhrasesForCulture(info.Culture));
                var builder = new GrammarBuilder { Culture = info.Culture };
                builder.Append(choices);
                candidate.LoadGrammar(new Grammar(builder) { Name = "BARQ Lively commands" });

                try
                {
                    candidate.LoadGrammar(new DictationGrammar { Name = "BARQ conversation" });
                    DictationAvailable = true;
                }
                catch (Exception)
                {
                    DictationAvailable = false;
                }

                candidate.SpeechRecognized += Recognized;
                candidate.SpeechRecognitionRejected += Rejected;
                candidate.SpeechDetected += Detected;
                candidate.AudioLevelUpdated += LevelUpdated;
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
                candidate.SpeechRecognitionRejected -= Rejected;
                candidate.SpeechDetected -= Detected;
                candidate.AudioLevelUpdated -= LevelUpdated;
                candidate.Dispose();
                throw;
            }
        }

        private void LevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {
            if (SuppressRecognition) return;
            if (speechDetected)
            {
                peakLevel = Math.Max(peakLevel, e.AudioLevel);
            }
            else
            {
                // Slow moving background-noise baseline; no extra DSP thread.
                noiseFloor = noiseFloor == 0
                    ? e.AudioLevel
                    : noiseFloor * 0.96 + e.AudioLevel * 0.04;
            }
        }

        private void Detected(object sender, SpeechDetectedEventArgs e)
        {
            if (SuppressRecognition) return;
            speechDetected = true;
            peakLevel = 0;
        }

        private void Rejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            speechDetected = false;
            peakLevel = 0;
        }

        private void Recognized(object sender, SpeechRecognizedEventArgs e)
        {
            bool sawSpeech = speechDetected;
            int peak = peakLevel;
            speechDetected = false;
            peakLevel = 0;
            if (!enabled || SuppressRecognition || e.Result == null) return;

            float confidence = e.Result.Confidence;
            var text = e.Result.Text;
            BarqVoiceCommand command;
            if (confidence >= 0.84f &&
                BarqVoiceCommands.TryParse(text, out command))
            {
                hadWakeInteraction = true;
                attention.Arm(DateTime.UtcNow);
                onCommand(command);
                return;
            }
            if (!DictationAvailable || onConversation == null ||
                e.Result.Grammar == null ||
                e.Result.Grammar.Name != "BARQ conversation") return;

            string utterance;
            bool wakeOnly;
            if (!attention.TryHear(text, confidence, sawSpeech, peak,
                                   noiseFloor, DateTime.UtcNow,
                                   out utterance, out wakeOnly))
                return;

            hadWakeInteraction = true;
            if (wakeOnly)
            {
                if (onWake != null) onWake();
                return;
            }

            // During an attention window, users may give existing Lively commands
            // without saying BARQ again. These are never passed to Gemini.
            string recognizedCommand;
            string prefix;
            if (BarqAttentionGate.StripWakeWord(text, out prefix))
                recognizedCommand = text;
            else
                recognizedCommand = (RecognizerCulture.TwoLetterISOLanguageName == "ar"
                    ? "برق " : "barq ") + utterance;

            if (confidence >= 0.80f &&
                BarqVoiceCommands.TryParse(recognizedCommand, out command))
            {
                onCommand(command);
                return;
            }
            onConversation(utterance);
        }

        internal void RefreshAttention()
        {
            // Re-arm when TTS finishes even if Gemini took longer than the old window.
            if (hadWakeInteraction) attention.Arm(DateTime.UtcNow);
        }

        public void Dispose()
        {
            enabled = false;
            hadWakeInteraction = false;
            attention.Reset();
            var old = engine;
            engine = null;
            if (old == null) return;
            old.SpeechRecognized -= Recognized;
            old.SpeechRecognitionRejected -= Rejected;
            old.SpeechDetected -= Detected;
            old.AudioLevelUpdated -= LevelUpdated;
            try { old.RecognizeAsyncCancel(); }
            catch (InvalidOperationException) { }
            finally { old.Dispose(); }
        }
    }
}
