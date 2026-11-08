using System;
using System.Globalization;
using System.Linq;
using livelywpf.Voice;

internal static class BarqVoiceCommandTests
{
    private static void Assert(bool passed, string message)
    {
        if (!passed) throw new Exception(message);
    }

    public static int Main()
    {
        BarqVoiceCommand value;
        Assert(BarqVoiceCommands.TryParse("barq pause wallpapers", out value)
            && value == BarqVoiceCommand.PauseWallpapers, "Pause mapping failed");
        Assert(BarqVoiceCommands.TryParse("BARQ PLAY WALLPAPERS", out value)
            && value == BarqVoiceCommand.ResumeWallpapers, "Resume mapping failed");
        Assert(BarqVoiceCommands.TryParse("برق افتح المكتبة", out value)
            && value == BarqVoiceCommand.Library, "Arabic library mapping failed");
        Assert(BarqVoiceCommands.TryParse("برق افتح العمليات", out value)
            && value == BarqVoiceCommand.Processes, "Arabic processes mapping failed");
        Assert(BarqVoiceCommands.TryParse("barq apply selected wallpaper", out value)
            && value == BarqVoiceCommand.ApplySelected, "Apply mapping failed");
        Assert(!BarqVoiceCommands.TryParse("pause wallpapers", out value),
            "Unsafe command without wake word accepted");
        Assert(!BarqVoiceCommands.TryParse("barq kill all processes", out value),
            "Non-Lively command accepted");
        Assert(!BarqVoiceCommands.TryParse("", out value), "Empty phrase accepted");
        Assert(BarqVoiceCommands.PhrasesForCulture(new CultureInfo("en-US"))
            .All(x => x.StartsWith("barq ", StringComparison.OrdinalIgnoreCase)),
            "English wake word missing");
        Assert(BarqVoiceCommands.PhrasesForCulture(new CultureInfo("ar-SA"))
            .All(x => x.StartsWith("برق ", StringComparison.Ordinal)),
            "Arabic wake word missing");
        Console.WriteLine("BARQ voice grammar and safety tests passed.");
        return 0;
    }
}
