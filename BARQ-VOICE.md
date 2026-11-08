# BARQ Voice — AM Lively Wallpaper (Windows 8.1 x86)

This is an **opt-in, offline, fixed-command voice controller** for AM Lively Wallpaper, not BARQ AI's full conversational assistant. It uses the Windows speech engine available to **.NET Framework 4.7.2** through `System.Speech`, with no downloads, external voice APIs, credentials or background service.

## Activate

1. Connect a microphone and configure the Windows Speech Recognition engine, if available.
2. Start AM Lively Wallpaper. Right-click its icon near the Windows clock.
3. Select **BARQ Voice - Start listening**. The menu shows the selected recognition language.
4. Say the wake word and one complete command. Choose **Stop listening** to release the microphone.
5. Listening is OFF by default, and stops on app exit. Closing the window to tray retains the opt-in session until explicitly stopped or exit.

## Supported phrases

| English recognizer | Arabic recognizer (only if installed) | Action |
|---|---|---|
| barq open library | برق افتح المكتبة | Open Library |
| barq open settings | برق افتح الإعدادات | Open Settings |
| barq show processes | برق افتح العمليات | Open Processes (no process stopping) |
| barq add wallpaper | برق إضافة خلفية | Open the wallpaper-type page |
| barq pause wallpapers | برق وقف الخلفيات | Pause all wallpapers |
| barq resume wallpapers | برق شغل الخلفيات | Resume all wallpapers |
| barq import wallpaper | برق استورد خلفية | Open the existing ZIP import dialog |
| barq apply selected wallpaper | برق طبق الخلفية | Apply selected wallpaper using existing dialogs and warnings |
| barq show about | برق افتح حول البرنامج | Open About |

Alternate English phrases are in `Voice/BarqVoiceCommands.cs`.

**Known limits:** This implementation is not conversational, does not identify speakers, and does not synthesize a male Egyptian voice. Arabic command recognition requires an *installed Arabic speech recognition engine*, not merely the Arabic Windows UI language or .NET Arabic language pack. If unavailable, the app falls back to installed English recognition. If neither is installed, voice cannot start and the tray menu reports an error. There is no automatic install of engines or cloud service.

The Processes tab continues to require its own existing safety approvals. Voice cannot kill processes, change protected settings, execute arbitrary commands or download software.

## Test checklist for actual Windows 8.1 x86 device

- Confirm Windows Speech recognizer availability with microphone. Check enabled/disabled state in tray.
- Test one recognized command in its own installed language; ensure commands without "barq"/"برق" are ignored.
- Verify pause/resume twice each (idempotency) while wallpapers are active, and while window is hidden.
- Verify Library, Add wallpaper, Settings, Processes and About navigation.
- Import a ZIP; use Apply Selected on wallpaper requiring a confirmation and confirm the original prompt appears.
- Stop listening and verify the microphone is released. Exit the app and verify no voice process remains.
- Regression: wallpaper playback, fullscreen pause, multi-monitor, process safety/restore, and CPU/RAM usage in tray.

GitHub Actions compiles the app and runs parser tests, **but does not test microphone behavior or Windows 8.1 hardware**.
