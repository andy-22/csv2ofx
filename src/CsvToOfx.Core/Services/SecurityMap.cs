// C#
using System.Reflection;
using System.Collections.Concurrent;

namespace CsvToOfx.Core.Services
{
    internal static class SecurityMap
    {
        private static readonly Lazy<IReadOnlyDictionary<string,(string Cusip,string Name)>> Cache = new(() =>
        {
            var dict = new ConcurrentDictionary<string,(string,string)>(StringComparer.OrdinalIgnoreCase);
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("CsvToOfx.Core.Resources.SecurityMap.csv");
            if (stream == null) return dict;
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',', 3);
                if (parts.Length < 3) continue;
                var cusip = parts[0].Trim();
                var ticker = parts[1].Trim();
                var name = parts[2].Trim();
                if (!string.IsNullOrEmpty(ticker) && !string.IsNullOrEmpty(cusip))
                    dict[ticker] = (cusip, name);
            }
            return dict;
        });

        public static bool TryGetByTicker(string ticker, out (string Cusip,string Name) info) =>
            Cache.Value.TryGetValue(ticker, out info);
    }
}