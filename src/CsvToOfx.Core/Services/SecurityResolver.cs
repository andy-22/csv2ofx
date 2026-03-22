// C#
using CsvToOfx.Core.Models;
using CsvToOfx.Core.Parsing;

namespace CsvToOfx.Core.Services
{
    public sealed class SecurityResolver : ISecurityResolver
    {
        private readonly bool _preferCusip;

        public SecurityResolver(bool preferCusip = true)
        {
            _preferCusip = preferCusip;
        }

        public SecurityRef? ResolveFromRow(IDictionary<string, string?> row)
        {
            if (!row.TryGetValue("Symbol", out var tickerRaw)) return null;
            var symbol = tickerRaw?.Trim();
            if (string.IsNullOrEmpty(symbol)) return null;

            // Prefer mapped CUSIP when symbol is a ticker we recognize
            if (_preferCusip && SecurityMap.TryGetByTicker(symbol, out var mapped))
            {
                var mappedName = string.IsNullOrWhiteSpace(mapped.Name) ? symbol : $"{mapped.Name} ({symbol})";
                return new SecurityRef(mapped.Cusip, IdType.Cusip, mappedName, symbol);
            }

            // Fallback: build name from provided fields (Security Name or Description)
            row.TryGetValue("Security Name", out var nameRaw);
            row.TryGetValue("Description", out var descRaw);
            var nameBase = (descRaw ?? nameRaw)?.Trim();
            if (string.IsNullOrWhiteSpace(nameBase)) nameBase = symbol;

            // Only append ticker when it adds information
            var displayName = nameBase;
            if (!string.IsNullOrWhiteSpace(symbol) && !string.Equals(nameBase, symbol, StringComparison.OrdinalIgnoreCase))
                displayName = $"{nameBase} ({symbol})";

            var idType = LooksLikeCusip(symbol) ? IdType.Cusip : IdType.Ticker;
            var tickerField = idType == IdType.Ticker ? symbol : null;
            return new SecurityRef(symbol, idType, displayName, tickerField);
        }

        public SecurityRef? Resolve(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return null;
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Symbol"] = symbol
            };
            return ResolveFromRow(row);
        }

        private static bool LooksLikeCusip(string value)
        {
            if (value.Length != 9) return false;
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (!char.IsLetterOrDigit(c)) return false;
            }
            return true;
        }
    }
}
