# BARQ Voice for AM Lively Wallpaper — Windows 8.1 x86

## What this does

BARQ Voice is optional, starts only from the Windows system-tray icon, and uses Windows' local speech recognition and text-to-speech via `System.Speech` on .NET Framework 4.7.2.

It can execute the existing Lively-only commands and, with a *dictation-capable recognizer*, answer short factual questions from internet search snippets **out loud**. It is not BARQ's full conversational AI or a locally-running large language model.

## Start and stop

Right-click the AM Lively Wallpaper icon near the clock and choose **BARQ Voice - Start listening**. Select **Stop listening** to release the microphone. Voice stays OFF by default after app launch, with no new microphone icon in the main UI. Exit disposes audio resources.

Arabic speech recognition **requires an actual Arabic speech recognition engine installed in Windows**. The Arabic display language is insufficient. English speech recognition is used as a fallback. If dictation grammar is unsupported, Lively's fixed commands still work, but questions cannot be recognized.

Spoken replies use an installed TTS voice. A male voice matching the recognizer language is preferred; **Egyptian male speech is not guaranteed**. Arabic spoken answers require Arabic TTS. Without a matching voice, only the written answer is displayed and a brief English explanation is spoken.

## Examples

- «برق وقف الخلفيات» — pauses wallpaper playback
- «برق شغل الخلفيات» — resumes playback
- «برق افتح المكتبة» — opens Library
- «برق ابحث عن الأقمار الصناعية» — searches for a public result and speaks a short sourced answer
- “Barq who is Alan Turing?” — searches and speaks a short result
- “Barq open search result” / «برق افتح نتيجة البحث» — opens the last source URL in the browser when explicitly requested

Wallpaper and process logic is unchanged: processes cannot be killed by voice; applying a wallpaper keeps its original confirmation prompts.

## How internet search works

1. The recognizer checks that the phrase starts with **Barq / برق** and matches an information-question prefix.
2. Only the resulting question text (maximum 140 characters) is sent over HTTPS to Bing's public RSS search.
3. If RSS is unavailable, the MediaWiki public Wikipedia search API is used as an alternative.
4. A brief result snippet with its provider is spoken and shown in a notification; it is not a synthesized AI explanation or independently verified fact.
5. Results can be incomplete or wrong. The user can explicitly request opening the source in a browser.

There is no API key, subscription or audio upload. The two public search services require an internet connection, and availability is not guaranteed. No transcript history is saved by this feature.

## Compatibility and test matrix

- Target: Windows 8.1 x86, .NET Framework 4.7.2, MahApps.Metro 1.6.5.1.
- Code: System.Speech, System.Net.Http and System.Xml.Linq from .NET Framework; no new NuGet dependency.
- CI: project/installer build, existing process safety tests, wake-word parser tests, question parser tests, offline Bing RSS and Wikipedia XML tests.
- Manual validation still needed on Windows 8.1: microphone, installed speech grammars, installed voices, full/partial playback, actual HTTPS lookup, tray hide/exit, CPU/RAM, multiple monitors, and process-approval prompts.
- About page: the bulky license viewer is removed, but the actual **License.txt** and **NOTICE-AM.md** remain in the installation; two compact buttons open them.

No guarantee of Egyptian Arabic recognition, native Arabic TTS availability, real-time web search uptime or full natural conversation is implied by a successful build.
