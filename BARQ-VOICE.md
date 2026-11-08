# BARQ Voice + Gemini + voice attention (Windows 8.1 x86)

## What changed

BARQ uses Gemini 3.8 Flash (REST `generateContent`) for conversational replies. A tiny local attention gate processes speech/audio level events from Windows System.Speech before an utterance is ever sent to Gemini. The rest of AM Lively Wallpaper retains its original playback and process-safety behavior.

**This is not Gemini Live.** Gemini receives *text only*, never microphone audio. The app does not install a new Windows speech recognizer, speaker recognition system, microphone driver or voice model.

## Activating and configuring

1. Have an installed English or Arabic Windows Speech Recognition engine, a working microphone and a local text-to-speech voice. **Windows 8.1 may not provide an Arabic recognizer by default.**
2. From the AM Lively tray icon choose **BARQ - Configure Gemini API key**. Paste a Gemini API key created in [Google AI Studio](https://aistudio.google.com/apikey).
3. The key is encrypted for the current Windows user with DPAPI under `%APPDATA%\AM Lively Wallpaper\barq-gemini-key.bin`. Alternatively set the `GEMINI_API_KEY` environment variable yourself.
4. Choose **BARQ Voice - Start listening** to enable the microphone. It is **OFF by default**. Stop listening from the same tray menu. No microphone icon is added to the main UI.
5. Say "Barq" / "برق" and ask a question or a wallpaper command. After being addressed, BARQ remains attentive for ~16 seconds for brief follow-ups without repeating its name. The attention window is extended after each spoken response.

## Attention safety and noise

- The speech recognizer listens only after explicit tray activation. It requires the wake word before starting a conversation.
- Voice detection, the recent observed microphone audio level versus the ambient baseline, recognition confidence and a word boundary are all used to avoid treating background speech as a request.
- Silence or inactivity ends the active follow-up window. BARQ ignores recognized audio while its own text-to-speech engine is speaking.
- No speaker identity verification is provided; another person near the microphone who says "Barq" may wake the assistant. This is **not** speaker diarization or a biometric voice lock.
- A recognizer without dictation support can still use the predefined Lively commands but cannot do free-form Gemini speech.

## What Gemini can do

- Conversational questions, summaries and explanations with short spoken-friendly responses.
- Arabic questions get Egyptian colloquial Arabic text from Gemini, but **spoken Arabic requires an installed Arabic Windows TTS voice**.
- Explicit "search for..." / "ابحث عن..." questions can supplement Gemini context with public Bing RSS / Wikipedia snippets. This is not guaranteed real-time search; provider availability and response accuracy vary.
- For wallpaper commands such as pause, resume, import, library, settings, or apply selected, BARQ continues to call the app's existing, safeguarded handlers without involving Gemini.
- Process-stopping and restoration are not controlled by Gemini. The assistant cannot generate arbitrary system commands.

## API privacy, performance and pricing

- Audio is captured by System.Speech locally. Recognized questions and up to four previous chat pairs are sent to Google only when the user has explicitly activated BARQ and addressed it. History is kept **in RAM only** during that voice session.
- The source does not contain or ship any API key, and the key is never included in logs or GitHub Actions.
- The default model is `gemini-3.8-flash`; optionally set `BARQ_GEMINI_MODEL=gemini-3.1-flash-lite` to use a lighter model if it is available to your API key.
- **Gemini may incur charges** depending on your Google API billing setup and free-tier usage; availability, model policies and limits can change. The application does not purchase subscriptions or automatically enable paid tiers.
- Internet access and a Gemini API key are required for conversational answers. Basic Lively voice commands work without Gemini.
- Microphone and audio response are controlled by the app's existing tray lifecycle; no new Windows background service or telemetry is installed.

## Testing

CI compiles the unchanged .NET Framework 4.7.2 x86 WPF application and installer; runs existing wallpaper/process safety tests, fixed-command tests, internet snippet parser tests and pure attention gate tests. These checks **do not** establish real microphone or device compatibility.

Manual tests needed on a **real Windows 8.1 x86 device**:
- Wake on "Barq", follow-up without wake, timeout after 16 seconds, background TV noise, low microphone volume, short pauses and repeated commands.
- Check that BARQ does not respond to its own speech; verify no loop after speaking.
- Test configured Gemini API key, 401/403/429 and offline errors, web source queries and the notification display.
- Verify Arabic and English recognizers actually installed, and available Arabic TTS voice. Confirm behaviour when dictation is unsupported.
- Exit the application, stop listening, and verify the microphone is released, conversation history cleared and no extra background process continues.
- Test tray resource usage, DPI, wallpaper playback/fullscreen pause/multi-monitor, process permissions and restoration.
