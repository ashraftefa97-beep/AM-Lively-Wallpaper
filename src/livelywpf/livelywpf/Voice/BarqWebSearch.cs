using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace livelywpf.Voice
{
    internal sealed class BarqWebAnswer
    {
        internal string Title;
        internal string Text;
        internal string Url;
        internal string Provider;
        internal bool Arabic;
        internal bool Found;
    }

    // Optional read-only public internet search. No paid API key or third-party DLL.
    internal sealed class BarqWebSearch : IDisposable
    {
        private readonly HttpClient client;
        internal BarqWebSearch()
        {
            client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "AM-Lively-Wallpaper/0.9.6.1 (VoiceSearch; local Windows desktop)");
        }

        internal static string BingUrl(string query)
        {
            return "https://www.bing.com/search?format=rss&q=" + Uri.EscapeDataString(query);
        }

        internal static string WikiUrl(string query, bool arabic)
        {
            return "https://" + (arabic ? "ar" : "en") +
                ".wikipedia.org/w/api.php?action=query&list=search&srlimit=1&format=xml&srsearch=" +
                Uri.EscapeDataString(query);
        }

        internal async Task<BarqWebAnswer> SearchAsync(string query, bool arabic)
        {
            // Unofficial public Bing RSS may be blocked; fall back to MediaWiki's documented API.
            try
            {
                BarqWebAnswer found = ParseBing(await client.GetStringAsync(BingUrl(query)), arabic);
                if (found != null) return found;
            }
            catch (Exception ex) when (ex is HttpRequestException ||
                                       ex is TaskCanceledException ||
                                       ex is XmlException || ex is InvalidOperationException) { }

            try
            {
                BarqWebAnswer found = ParseWikipedia(
                    await client.GetStringAsync(WikiUrl(query, arabic)), arabic);
                if (found != null) return found;
            }
            catch (Exception ex) when (ex is HttpRequestException ||
                                       ex is TaskCanceledException ||
                                       ex is XmlException || ex is InvalidOperationException) { }

            return new BarqWebAnswer {
                Found = false, Arabic = arabic, Provider = "None", Title = "No result",
                Text = arabic
                    ? "ملقتش نتيجة دلوقتي. اتأكد من اتصال الإنترنت وجرب تاني."
                    : "I could not retrieve a reliable search result. Try again later."
            };
        }

        private static XDocument ReadXmlSafely(string xml)
        {
            var settings = new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null
            };
            using (var sr = new StringReader(xml))
            using (var reader = XmlReader.Create(sr, settings))
                return XDocument.Load(reader);
        }

        private static string Short(string value)
        {
            return value.Length > 260 ? value.Substring(0, 257).TrimEnd() + "..." : value;
        }

        internal static string PlainText(string html)
        {
            if (String.IsNullOrWhiteSpace(html)) return "";
            string decoded = WebUtility.HtmlDecode(html);
            string stripped = Regex.Replace(decoded, "<[^>]*>", " ");
            return Regex.Replace(stripped, @"\s+", " ").Trim();
        }

        internal static BarqWebAnswer ParseBing(string xml, bool arabic)
        {
            var item = ReadXmlSafely(xml).Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "item");
            if (item == null) return null;
            string title = PlainText(item.Elements().FirstOrDefault(
                x => x.Name.LocalName == "title")?.Value);
            string snippet = PlainText(item.Elements().FirstOrDefault(
                x => x.Name.LocalName == "description")?.Value);
            string source = item.Elements().FirstOrDefault(
                x => x.Name.LocalName == "link")?.Value;
            if (String.IsNullOrWhiteSpace(title) || String.IsNullOrWhiteSpace(snippet) ||
                !IsSafeResultUrl(source)) return null;
            return new BarqWebAnswer {
                Found = true, Arabic = arabic, Title = title, Text = Short(snippet),
                Provider = "Bing RSS", Url = source
            };
        }

        internal static BarqWebAnswer ParseWikipedia(string xml, bool arabic)
        {
            var page = ReadXmlSafely(xml).Descendants().FirstOrDefault(
                x => x.Name.LocalName == "p" && x.Parent != null &&
                     x.Parent.Name.LocalName == "search");
            if (page == null) return null;
            string title = PlainText((string)page.Attribute("title"));
            if (String.IsNullOrWhiteSpace(title)) return null;
            string snippet = PlainText((string)page.Attribute("snippet"));
            if (String.IsNullOrWhiteSpace(snippet)) snippet = title;
            string link = "https://" + (arabic ? "ar" : "en") +
                ".wikipedia.org/wiki/" + Uri.EscapeDataString(title.Replace(' ', '_'));
            return new BarqWebAnswer {
                Found = true, Arabic = arabic, Title = title, Text = Short(snippet),
                Provider = "Wikipedia", Url = link
            };
        }

        internal static bool IsSafeResultUrl(string value)
        {
            Uri parsed;
            return Uri.TryCreate(value, UriKind.Absolute, out parsed) &&
                   parsed.Scheme == Uri.UriSchemeHttps &&
                   !String.IsNullOrWhiteSpace(parsed.Host);
        }

        public void Dispose() { client.Dispose(); }
    }
}
