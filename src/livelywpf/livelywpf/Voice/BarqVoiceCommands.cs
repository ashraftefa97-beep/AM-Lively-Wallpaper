using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace livelywpf.Voice
{
    // Strict, wake-word-only commands: never generates system commands or processes speech as code.
    internal enum BarqVoiceCommand
    {
        Library, Settings, Processes, AddWallpaper, PauseWallpapers,
        ResumeWallpapers, ImportArchive, ApplySelected, About
    }

    internal static class BarqVoiceCommands
    {
        private static readonly Dictionary<string, BarqVoiceCommand> English =
            new Dictionary<string, BarqVoiceCommand>(StringComparer.OrdinalIgnoreCase)
            {
                { "barq open library", BarqVoiceCommand.Library },
                { "barq show library", BarqVoiceCommand.Library },
                { "barq open settings", BarqVoiceCommand.Settings },
                { "barq show settings", BarqVoiceCommand.Settings },
                { "barq open processes", BarqVoiceCommand.Processes },
                { "barq show processes", BarqVoiceCommand.Processes },
                { "barq add wallpaper", BarqVoiceCommand.AddWallpaper },
                { "barq open add wallpaper", BarqVoiceCommand.AddWallpaper },
                { "barq pause wallpapers", BarqVoiceCommand.PauseWallpapers },
                { "barq stop wallpapers", BarqVoiceCommand.PauseWallpapers },
                { "barq resume wallpapers", BarqVoiceCommand.ResumeWallpapers },
                { "barq play wallpapers", BarqVoiceCommand.ResumeWallpapers },
                { "barq import wallpaper", BarqVoiceCommand.ImportArchive },
                { "barq apply selected wallpaper", BarqVoiceCommand.ApplySelected },
                { "barq show about", BarqVoiceCommand.About }
            };

        // Only used when an Arabic-language Windows speech recognizer is installed.
        private static readonly Dictionary<string, BarqVoiceCommand> Arabic =
            new Dictionary<string, BarqVoiceCommand>(StringComparer.Ordinal)
            {
                { "برق افتح المكتبة", BarqVoiceCommand.Library },
                { "برق افتح الإعدادات", BarqVoiceCommand.Settings },
                { "برق افتح العمليات", BarqVoiceCommand.Processes },
                { "برق إضافة خلفية", BarqVoiceCommand.AddWallpaper },
                { "برق وقف الخلفيات", BarqVoiceCommand.PauseWallpapers },
                { "برق شغل الخلفيات", BarqVoiceCommand.ResumeWallpapers },
                { "برق استورد خلفية", BarqVoiceCommand.ImportArchive },
                { "برق طبق الخلفية", BarqVoiceCommand.ApplySelected },
                { "برق افتح حول البرنامج", BarqVoiceCommand.About }
            };

        internal static string[] PhrasesForCulture(CultureInfo culture)
        {
            return (culture.TwoLetterISOLanguageName == "ar" ? Arabic : English)
                .Keys.ToArray();
        }

        internal static bool TryParse(string phrase, out BarqVoiceCommand command)
        {
            command = default(BarqVoiceCommand);
            if (String.IsNullOrWhiteSpace(phrase)) return false;
            string normalized = String.Join(" ", phrase.Trim().Split(
                new [] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            return English.TryGetValue(normalized, out command)
                || Arabic.TryGetValue(normalized, out command);
        }
    }
}
