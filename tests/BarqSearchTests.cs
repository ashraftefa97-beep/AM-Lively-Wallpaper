using System;
using livelywpf.Voice;

internal static class BarqSearchTests
{
    static void Check(bool pass, string message)
    {
        if (!pass) throw new Exception(message);
    }

    public static int Main()
    {
        string query;
        Check(BarqQuestionParser.TryExtract("barq who is Alan Turing", out query) &&
              query == "Alan Turing", "English question parsing failed");
        Check(BarqQuestionParser.TryExtract("برق ابحث عن الأقمار الصناعية", out query) &&
              query == "الأقمار الصناعية", "Arabic search parsing failed");
        Check(BarqQuestionParser.TryExtract("برق مين محمد علي", out query) &&
              query == "محمد علي", "Egyptian colloquial parsing failed");
        Check(!BarqQuestionParser.TryExtract("who is Alan Turing", out query), "Wake word omitted");
        Check(!BarqQuestionParser.TryExtract("barq kill all processes", out query), "Unsafe search intent accepted");
        Check(!BarqQuestionParser.TryExtract("barq search for ", out query), "Empty query accepted");
        Check(BarqWebSearch.BingUrl("C# & WPF").Contains("C%23%20%26%20WPF"), "Query URL encoding");

        var bing = BarqWebSearch.ParseBing(
            "<rss><channel><item><title>Article</title>" +
            "<description>Useful &amp; checked &lt;b&gt;information&lt;/b&gt;</description>" +
            "<link>https://example.org/info</link></item></channel></rss>", false);
        Check(bing != null && bing.Found && bing.Text.Contains("Useful & checked information"), "Bing snippet sanitization");
        var wiki = BarqWebSearch.ParseWikipedia(
            "<api><query><search><p title=\"Egypt\" snippet=\"A country in &lt;b&gt;Africa&lt;/b&gt;\" /></search></query></api>",
            false);
        Check(wiki != null && wiki.Url == "https://en.wikipedia.org/wiki/Egypt", "Wikipedia source");
        Check(!BarqWebSearch.IsSafeResultUrl("http://example.org") &&
              !BarqWebSearch.IsSafeResultUrl("file:///etc/passwd"), "Unsafe source accepted");
        Console.WriteLine("BARQ web question parser and source tests passed.");
        return 0;
    }
}
