using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace livelywpf.Voice
{
    /// <summary>
    /// A Gemini key is never included in GitHub, logs, or configuration JSON.
    /// DPAPI protects the user's local copy. GEMINI_API_KEY can also be used
    /// for headless configuration, but does not get copied to a file.
    /// </summary>
    internal static class BarqGeminiKeyStore
    {
        private static string FilePath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                    "AM Lively Wallpaper", "barq-gemini-key.bin");
            }
        }

        internal static string Read()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (!String.IsNullOrWhiteSpace(fromEnvironment))
                return fromEnvironment.Trim();

            try
            {
                if (!File.Exists(FilePath)) return null;
                var encrypted = File.ReadAllBytes(FilePath);
                var plain = ProtectedData.Unprotect(encrypted, null,
                    DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain).Trim();
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static void Save(string key)
        {
            if (String.IsNullOrWhiteSpace(key) || key.Trim().Length > 300)
                throw new ArgumentException("Enter a valid Gemini API key.");
            var folder = Path.GetDirectoryName(FilePath);
            Directory.CreateDirectory(folder);
            var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(key.Trim()),
                null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(FilePath, bytes);
        }

        internal static void Clear()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
    }
}
