// C#
using CsvToOfx.Core.Models;

namespace CsvToOfx.Core.Services
{
    public sealed class SecurityResolver
    {
        private readonly bool _preferCusip;

        public SecurityResolver(bool preferCusip = true)
        {
            _preferCusip = preferCusip;
        }

        public SecurityRef? ResolveFromRow(IDictionary<string, string?> row)
        {
            if (!row.TryGetValue("Symbol", out var tickerRaw)) return null;
            var ticker = tickerRaw?.Trim();
            if (string.IsNullOrEmpty(ticker)) return null;

            if (_preferCusip && SecurityMap.TryGetByTicker(ticker, out var mapped))
            {
                var name = string.IsNullOrWhiteSpace(mapped.Name) ? ticker : $"{mapped.Name} ({ticker})";
                return new SecurityRef(mapped.Cusip, IdType.Cusip, name, ticker);
            }

            row.TryGetValue("Security Name", out var nameRaw);
            var nameFallback = nameRaw?.Trim();
            var displayName = string.IsNullOrWhiteSpace(nameFallback) ? ticker : $"{nameFallback} ({ticker})";
            return new SecurityRef(ticker, IdType.Ticker, displayName, ticker);
        }
    }
}
