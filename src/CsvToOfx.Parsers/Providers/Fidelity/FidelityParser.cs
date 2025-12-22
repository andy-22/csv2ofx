using CsvToOfx.Core.Models;
using CsvToOfx.Parsers.Abstractions;
using CsvToOfx.Parsers.Shared;

namespace CsvToOfx.Parsers.Providers.Fidelity;

[Provider("fidelity")]
public sealed class FidelityParser : IStatementParser
{
    public string ProviderCode => "fidelity";
    public ParserCapabilities Capabilities => ParserCapabilities.Csv | ParserCapabilities.Brokerage;

    private static readonly string[] RequiredFields = {
        "Run Date","Action","Symbol","Description","Type","Price","Quantity","Commission","Fees","Amount"
    };

    public ParseResult Parse(RawStatement input, ParserContext ctx)
    {
        var reader = new CsvRowReader();
        var account = new AccountRef("Fidelity", ctx.AccountId, "Brokerage");
        var transactions = new List<NormalizedTransaction>();
        var securities = new Dictionary<string, SecurityRef>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in reader.ReadRows(input.Content))
        {
            // skip empty rows
            var nonEmpty = row.Values.Count(v => !string.IsNullOrWhiteSpace(v));
            if (nonEmpty <= 1) continue;

            // date filter
            var dt = ctx.DateParser.ParseOrNull(Get(row, "Run Date"));
            if (ctx.StartDateFilter.HasValue && dt.HasValue && dt.Value < ctx.StartDateFilter.Value) continue;

            var action = FidelityActionMap.Normalize(Get(row, "Action"));
            var symbol = (Get(row, "Symbol") ?? "").Trim();
            var security = ctx.SecurityResolver.ResolveFromRow(row);
            if (security is not null && !securities.ContainsKey(security.Id))
                securities[security.Id] = security;

            var units = TryParseDecimal(Get(row, "Quantity"));
            var unitPrice = TryParseDecimal(Get(row, "Price"));
            var amount = ctx.AmountParser.ParseAbsOrNull(Get(row, "Amount")) ?? 0m;
            var memo = BuildMemo(action, row);
            var fees = TryParseDecimal(Get(row, "Fees"));
            var currency = (Get(row, "Currency") ?? ctx.CurrencyDefault).Trim();
            var fitid = ctx.FitIdGenerator.FromSortedRow(row);

            transactions.Add(new NormalizedTransaction(
                TradeDate: dt ?? default,
                Action: action,
                Security: security,
                Units: units,
                UnitPrice: unitPrice,
                Amount: amount,
                Currency: currency,
                Memo: memo,
                Fees: fees,
                FitId: fitid
            ));
        }
        return new ParseResult(account, transactions, securities.Values.ToList());
    }

    private static string? Get(IDictionary<string, string?> row, string key)
        => row.TryGetValue(key, out var value) ? value : null;

    private static decimal? TryParseDecimal(string? s)
        => decimal.TryParse((s ?? "").Replace(",", ""), out var v) ? v : null;

    private static string? BuildMemo(CanonicalAction action, IDictionary<string, string?> row)
    {
        var symbol = (Get(row, "Symbol") ?? "").Trim();
        return action switch
        {
            CanonicalAction.Income       => string.IsNullOrWhiteSpace(symbol) ? "Dividend Received" : $"Dividend Received - {symbol}",
            CanonicalAction.CashTransfer => (Get(row, "Action") ?? "").Trim(),
            CanonicalAction.BuyStock     => string.IsNullOrWhiteSpace(symbol) ? "You Bought" : $"You Bought - {symbol}",
            CanonicalAction.SellStock    => string.IsNullOrWhiteSpace(symbol) ? "You Sold" : $"You Sold - {symbol}",
            _                            => Get(row, "Description")
        };
    }
}