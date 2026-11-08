// AM additions, Microsoft Public License. Original project notices retained.
using System;
using System.Collections.Generic;
using System.IO;
namespace livelywpf.ProcessManager
{
    public sealed class Verdict
    {
        public bool Allowed; public string Category; public string Reason;
        public Verdict(bool allowed, string category, string reason) { Allowed = allowed; Category = category; Reason = reason; }
    }
    public static class SafetyPolicy
    {
        static readonly Dictionary<string, string[]> Catalog = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
            {"Spotify", new[]{"Spotify AB"}}, {"Discord", new[]{"Discord Inc.", "Discord Inc"}},
            {"steam", new[]{"Valve Corp.", "Valve Corporation"}},
            {"EpicGamesLauncher", new[]{"Epic Games, Inc.", "Epic Games Inc."}}
        };
        static readonly HashSet<string> Core = new HashSet<string>(("system idle registry smss csrss wininit winlogon services lsass lsm svchost explorer dwm audiodg spoolsv fontdrvhost sihost taskhost taskhostw taskeng conhost rundll32 dllhost WmiPrvSE RuntimeBroker SearchIndexer SearchProtocolHost SearchFilterHost MsMpEng NisSrv SecurityHealthService SecurityHealthSystray sppsvc TrustedInstaller TiWorker AM-Lively-Wallpaper AM-Lively-Helper livelywpf livelySubProcess LivelyCefSharp CefSharp.BrowserSubprocess mpv").Split(' '), StringComparer.OrdinalIgnoreCase);
        public static bool IsUnder(string path, string directory)
        {
            if (String.IsNullOrWhiteSpace(path) || String.IsNullOrWhiteSpace(directory)) return false;
            return Path.GetFullPath(path).StartsWith(Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        public static Verdict Classify(string name, string path, string signer, bool trusted, bool sameSession, bool serviceHost, bool ownTree, string windows, string app, string data)
        {
            if (Core.Contains(name) || ownTree || serviceHost || !sameSession || IsUnder(path, windows) || IsUnder(path, app) || IsUnder(path, data))
                return new Verdict(false, "Critical (do not touch)", "Windows, service host, another session, or AM wallpaper process.");
            string[] publishers;
            if (!Catalog.TryGetValue(name, out publishers))
                return new Verdict(false, "Better to keep", "Unknown role; protected by default, including security and driver software.");
            if (String.IsNullOrEmpty(path) || !trusted || !Array.Exists(publishers, x => String.Equals(x, signer, StringComparison.OrdinalIgnoreCase)))
                return new Verdict(false, "Critical (do not touch)", "Executable identity or trusted publisher could not be verified.");
            return new Verdict(true, "Safe to pause", "Known optional app; closing may interrupt activity or lose unsaved work.");
        }
        public static Verdict Service(string name, string signer, bool trusted, bool stoppable, bool dependents)
        {
            // Only a known standalone updater is eligible; all other services stay protected.
            if (!String.Equals(name, "AdobeARMservice", StringComparison.OrdinalIgnoreCase))
                return new Verdict(false, "Critical (do not touch)", "System, security, network, audio, driver or unreviewed service; protected.");
            if (!trusted || !(signer == "Adobe Inc." || signer == "Adobe Systems Incorporated") || !stoppable || dependents)
                return new Verdict(false, "Critical (do not touch)", "Unverified updater, cannot stop, or has dependent services.");
            return new Verdict(true, "Safe to pause", "Optional Adobe updater; updates pause until restored. Startup type is unchanged.");
        }
    }
}
