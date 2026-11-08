using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace livelywpf.Voice
{
    internal sealed class BarqPanelStatus
    {
        internal bool Listening;
        internal bool GeminiConfigured;
        internal bool CanDictate;
        internal string Language;
    }

    /// <summary>
    /// Only a visual control panel; closing it does not switch off an explicitly
    /// enabled voice session. No additional process, listener or polling timer.
    /// </summary>
    public partial class BarqVoicePanel : MetroWindow
    {
        private readonly Func<BarqPanelStatus> statusProvider;
        private readonly Action toggleVoice;
        private readonly Action configureGemini;
        private readonly Action testSpeaker;
        private readonly Action<BarqVoiceCommand> executeCommand;
        private readonly Action<string> speakAnswer;
        private readonly Action onPanelClosed;
        private readonly BarqGeminiClient typedGemini = new BarqGeminiClient();
        private readonly CancellationTokenSource closingToken = new CancellationTokenSource();
        private bool isClosing;
        private bool requestActive;

        internal BarqVoicePanel(
            Func<BarqPanelStatus> statusProvider,
            Action toggleVoice, Action configureGemini, Action testSpeaker,
            Action<BarqVoiceCommand> executeCommand, Action<string> speakAnswer,
            Action onPanelClosed)
        {
            this.statusProvider = statusProvider;
            this.toggleVoice = toggleVoice;
            this.configureGemini = configureGemini;
            this.testSpeaker = testSpeaker;
            this.executeCommand = executeCommand;
            this.speakAnswer = speakAnswer;
            this.onPanelClosed = onPanelClosed;
            InitializeComponent();
            RefreshStatus();
        }

        internal void RefreshStatus()
        {
            if (isClosing) return;
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(RefreshStatus));
                return;
            }
            var state = statusProvider();
            bool listening = state != null && state.Listening;
            StatusBadge.Text = listening ? "MIC ON" : "MIC OFF";
            MicrophoneState.Text = listening ? "Listening for BARQ" : "Off - private";
            EngineState.Text = listening ? (state.Language ?? "Windows speech recognition") :
                "Starts after enabling the microphone";
            DictationState.Text = listening
                ? (state.CanDictate ? "Available for Gemini conversations" :
                   "Not supported by the installed recognizer; fixed commands only")
                : "Not checked yet";
            GeminiState.Text = state != null && state.GeminiConfigured
                ? "API key configured" : "API key not configured";
            StartStopButton.Content = listening ? "Stop listening" : "Start listening";
        }

        internal void AddActivity(string source, string message)
        {
            if (isClosing || String.IsNullOrWhiteSpace(message)) return;
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => AddActivity(source, message)));
                return;
            }
            string entry = Environment.NewLine + Environment.NewLine +
                "[" + DateTime.Now.ToString("HH:mm") + "] " + source + ": " + message;
            string next = ConversationLog.Text + entry;
            if (next.Length > 8000) next = next.Substring(next.Length - 8000);
            ConversationLog.Text = next;
            ConversationLog.CaretIndex = ConversationLog.Text.Length;
            ConversationLog.ScrollToEnd();
        }

        private void StartStopClick(object sender, RoutedEventArgs e)
        {
            if (isClosing) return;
            toggleVoice();
            RefreshStatus();
        }

        private void ConfigureKeyClick(object sender, RoutedEventArgs e)
        {
            configureGemini();
            RefreshStatus();
        }

        private void TestSpeakerClick(object sender, RoutedEventArgs e)
        {
            if (!statusProvider().Listening)
            {
                AddActivity("SYSTEM", "Start listening to activate the local speaker.");
                return;
            }
            testSpeaker();
            AddActivity("SYSTEM", "Speaker test requested.");
        }

        private void OpenLibraryClick(object sender, RoutedEventArgs e)
        {
            executeCommand(BarqVoiceCommand.Library);
            AddActivity("ACTION", "Open wallpaper library.");
        }

        private void PauseWallpaperClick(object sender, RoutedEventArgs e)
        {
            executeCommand(BarqVoiceCommand.PauseWallpapers);
            AddActivity("ACTION", "Pause wallpapers.");
        }

        private void ResumeWallpaperClick(object sender, RoutedEventArgs e)
        {
            executeCommand(BarqVoiceCommand.ResumeWallpapers);
            AddActivity("ACTION", "Resume wallpapers.");
        }

        private void QuestionInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                AskQuestion();
            }
        }

        private void AskClick(object sender, RoutedEventArgs e) { AskQuestion(); }

        private async void AskQuestion()
        {
            if (requestActive || isClosing) return;
            string question = QuestionInput.Text.Trim();
            if (question.Length == 0) return;
            if (question.Length > 1200)
            {
                AddActivity("SYSTEM", "Please keep the question under 1200 characters.");
                return;
            }
            if (!statusProvider().GeminiConfigured)
            {
                AddActivity("SYSTEM", "Configure a Gemini API key first.");
                RefreshStatus();
                return;
            }

            requestActive = true;
            AskButton.IsEnabled = false;
            BusyIndicator.Visibility = Visibility.Visible;
            QuestionInput.Clear();
            AddActivity("YOU", question);

            try
            {
                string answer = await typedGemini.AskAsync(question, null, closingToken.Token);
                if (isClosing) return;
                AddActivity("BARQ", answer);
                if (statusProvider().Listening && speakAnswer != null)
                    speakAnswer(answer);
            }
            catch (OperationCanceledException)
            {
                if (!isClosing) AddActivity("SYSTEM", "Request cancelled.");
            }
            catch (Exception ex)
            {
                if (!isClosing)
                {
                    // Never show raw HTTP payloads or credentials.
                    string message = ex is InvalidOperationException
                        ? ex.Message : "Gemini couldn't answer. Check the connection.";
                    AddActivity("ERROR", message);
                }
            }
            finally
            {
                requestActive = false;
                if (!isClosing)
                {
                    AskButton.IsEnabled = true;
                    BusyIndicator.Visibility = Visibility.Collapsed;
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            isClosing = true;
            closingToken.Cancel();
            typedGemini.Dispose();
            closingToken.Dispose();
            if (onPanelClosed != null) onPanelClosed();
            base.OnClosed(e);
        }
    }
}
