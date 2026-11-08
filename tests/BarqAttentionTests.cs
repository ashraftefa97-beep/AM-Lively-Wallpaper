using System;
using livelywpf.Voice;

internal static class AttentionTests
{
    private static void Check(bool valid, string label)
    {
        if (!valid) throw new Exception(label);
    }

    public static int Main()
    {
        var gate = new BarqAttentionGate();
        var now = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);
        string words;
        bool onlyWake;
        Check(!gate.TryHear("فتح الخلفيات", 0.94f, true, 40, 3, now,
            out words, out onlyWake), "Unsolicited background speech was accepted");
        Check(!gate.TryHear("برق", 0.50f, true, 30, 3, now,
            out words, out onlyWake), "Low confidence wake was accepted");
        Check(!gate.TryHear("برق", 0.86f, true, 4, 10, now,
            out words, out onlyWake), "Ambient noise mistaken for voice");
        Check(gate.TryHear("برق", 0.87f, true, 20, 3, now,
            out words, out onlyWake) && onlyWake, "Wake word not recognized");
        Check(gate.IsAwake(now.AddSeconds(8)), "Attention window not armed");
        Check(gate.TryHear("قولي معلومات عن القمر", 0.82f, true, 22, 2,
            now.AddSeconds(5), out words, out onlyWake) &&
            words == "قولي معلومات عن القمر", "Follow-up not recognized");
        Check(!gate.TryHear("كلام عشوائي", 0.87f, true, 30, 2,
            now.AddSeconds(25), out words, out onlyWake), "Attention timeout failed");
        Check(!BarqAttentionGate.StripWakeWord("barbecue chicken", out words),
            "Wake word must have a word boundary");
        Check(BarqAttentionGate.StripWakeWord("barq, who are you", out words)
            && words == "who are you", "English wake + question failed");
        Check(BarqAttentionGate.StripWakeWord("برء إيه الأخبار", out words)
            && words == "إيه الأخبار", "Name pronunciation alias failed");
        gate.Arm(now);
        gate.Extend(now.AddSeconds(10));
        Check(gate.IsAwake(now.AddSeconds(23)), "Follow-up extension failed");
        gate.Reset();
        Check(!gate.IsAwake(now.AddSeconds(11)), "Off must reset attention");
        Console.WriteLine("BARQ attention gate tests passed.");
        return 0;
    }
}
