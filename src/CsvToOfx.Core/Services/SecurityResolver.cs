
// src/CsvToOfx.Core/Services/SecurityResolver.cs
using System.Text.RegularExpressions;
using CsvToOfx.Core.Models;

namespace CsvToOfx.Core.Services
{
    /// <summary>
    /// Resolves a security identifier (CUSIP or Ticker) from a parsed CSV row.
    /// Behavior:
    ///  - If Symbol looks like a 9-char alphanumeric -> treat as CUSIP.
    ///  - Else, treat as Ticker and optionally upgrade to CUSIP using a provided map.
    ///  - Falls back gracefully to Ticker if no mapping is available.
    /// </summary>
    public sealed class SecurityResolver
    {
        private readonly IReadOnlyDictionary<string, string> _tickerToCusip;

        /// <param name="tickerToCusip">
        /// Optional dictionary mapping UPPERCASE tickers -> CUSIP (9 chars).
        /// Pass an empty dictionary if you don't have a map yet.
        /// </param>
        public SecurityResolver(IReadOnlyDictionary<string, string> tickerToCusip)
        {
            _tickerToCusip = tickerToCusip ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Resolve the <see cref="SecurityRef"/> from a row dictionary.
        /// Expected keys: "Symbol", but if missing or non-standard you can extend this
        /// to look into other fields like "Description" later.
        /// </summary>
        /// <param name="row">CSV row with string values (headers -> values).</param>
        public SecurityRef ResolveFromRow(IDictionary<string, string?> row)
        {
            // 1) Extract 'Symbol' if present.
            var symbol = (row.TryGetValue("Symbol", out var sym) ? sym : "")?.Trim() ?? "";

            // 2) If the symbol looks like a CUSIP (strict 9 alphanumeric), prefer it.
            if (IsCusip(symbol))
            {
                return new SecurityRef(
                    Id: symbol,
                    IdType: IdType.Cusip,
                    Name: null,
                    Ticker: null
                );
            }

            // 3) Otherwise treat it as a ticker (uppercase) and attempt to upgrade via mapping.
            var ticker = symbol.ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(ticker) && _tickerToCusip.TryGetValue(ticker, out var cusip) && IsCusip(cusip))
            {
                // Mapping found: return CUSIP as primary id while preserving ticker.
                return new SecurityRef(
                    Id: cusip,
                    IdType: IdType.Cusip,
                    Name: null,
                    Ticker: ticker
                );
            }

            // 4) Fallback: return ticker as the primary identifier.
            // If the symbol is empty, you'll still get a TICKER entry with an empty Id.
            return new SecurityRef(
                Id: ticker,
                IdType: IdType.Ticker,
                Name: null,
                Ticker: string.IsNullOrWhiteSpace(ticker) ? null : ticker
            );
        }

        /// <summary>
        /// Utility: strict CUSIP check (9 chars, uppercase letters or digits).
        /// You can relax/augment this if needed (e.g., allow spaces or hyphens and strip them).
        /// </summary>
        private static bool IsCusip(string? s)
            => !string.IsNullOrWhiteSpace(s) && Regex.IsMatch(s, @"^[A-Z0-9]{9}$");
    }
}