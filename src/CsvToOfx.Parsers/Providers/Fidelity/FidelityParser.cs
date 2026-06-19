using CsvToOfx.Core.Models;
using CsvToOfx.Core.Parsing.HeaderMaps;
using CsvToOfx.Parsers.Abstractions;
using CsvToOfx.Parsers.Shared;

namespace CsvToOfx.Parsers.Providers.Fidelity;

[Provider("fidelity")]
public sealed class FidelityParser : IStatementParser
{
    public string ProviderCode => "fidelity";
    public ParserCapabilities Capabilities => ParserCapabilities.Csv | ParserCapabilities.Brokerage;

    private static readonly HeaderMap[] HeaderMaps =
    {
        FidelityTradingHeaderMap.Instance,
        FidelityIraHeaderMap.Instance
    };

    public ParseResult Parse(RawStatement input, ParserContext ctx)
    {
        var reader = new CsvRowReader();
        var account = new AccountRef("Fidelity", ctx.AccountId, "Brokerage");
        var transactions = new List<NormalizedTransaction>();
        var securities = new Dictionary<string, SecurityRef>(StringComparer.OrdinalIgnoreCase);
        var readResult = reader.ReadRows(input.Content, HeaderMaps);
        if (readResult is null)
            return new ParseResult(account, transactions, securities.Values.ToList());

        var columnsByField = BuildFieldLookup(readResult.HeaderMap);

        foreach (var row in readResult.Rows)
        {
            // skip empty rows
            var nonEmpty = row.Values.Count(v => !string.IsNullOrWhiteSpace(v));
            if (nonEmpty <= 1) continue;

            if (ShouldSkipSyntheticSweepReinvestment(row, columnsByField))
                continue;

            // date filter
            var dt = ctx.DateParser.ParseOrNull(Get(row, columnsByField, CanonicalField.TradeDate));
            if (ctx.StartDateFilter.HasValue && dt.HasValue && dt.Value < ctx.StartDateFilter.Value) continue;

            var action = FidelityActionMap.Normalize(Get(row, columnsByField, CanonicalField.Action));
            var symbol = (Get(row, columnsByField, CanonicalField.Symbol) ?? "").Trim();
            var security = ctx.SecurityResolver.ResolveFromRow(row);
            if (security is not null)
            {
                if (!securities.TryGetValue(security.Id, out var existing))
                {
                    securities[security.Id] = security;
                }
                else
                {
                    var name = !string.IsNullOrWhiteSpace(security.Name) ? security.Name : existing.Name;
                    // if existing name is just the ID/CUSIP, prefer incoming name even if equal length
                    if (!string.IsNullOrWhiteSpace(security.Name) && string.Equals(existing.Name, existing.Id, StringComparison.OrdinalIgnoreCase))
                        name = security.Name;

                    var ticker = !string.IsNullOrWhiteSpace(existing.Ticker) ? existing.Ticker : security.Ticker;
                    if (string.IsNullOrWhiteSpace(ticker))
                        ticker = security.Ticker;

                    securities[security.Id] = existing with { Name = name, Ticker = ticker };
                }
            }

            var units = TryParseDecimal(Get(row, columnsByField, CanonicalField.Quantity));
            var unitPrice = TryParseDecimal(Get(row, columnsByField, CanonicalField.Price));
            var amount = ParseAmount(ctx, action, row, columnsByField);
            var memo = BuildMemo(action, row, columnsByField);
            var fees = TryParseDecimal(Get(row, columnsByField, CanonicalField.Fees));
            var currency = (Get(row, columnsByField, CanonicalField.Currency) ?? ctx.CurrencyDefault).Trim();
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

    private static Dictionary<CanonicalField, string> BuildFieldLookup(HeaderMap headerMap)
    {
        var lookup = new Dictionary<CanonicalField, string>();
        foreach (var field in headerMap.Columns.Values.Distinct())
        {
            if (headerMap.TryGetColumnName(field, out var columnName))
                lookup[field] = columnName;
        }

        return lookup;
    }

    private static string? Get(
        IDictionary<string, string?> row,
        IReadOnlyDictionary<CanonicalField, string> columnsByField,
        CanonicalField field)
    {
        if (!columnsByField.TryGetValue(field, out var columnName))
            return null;

        return row.TryGetValue(columnName, out var value) ? value : null;
    }

    private static decimal? TryParseDecimal(string? s)
        => decimal.TryParse((s ?? "").Replace(",", ""), out var v) ? v : null;

    private static decimal ParseAmount(
        ParserContext ctx,
        CanonicalAction action,
        IDictionary<string, string?> row,
        IReadOnlyDictionary<CanonicalField, string> columnsByField)
    {
        var rawAmount = Get(row, columnsByField, CanonicalField.Amount);
        var amount = action == CanonicalAction.CashTransfer
            ? ctx.AmountParser.ParseSignedOrNull(rawAmount)
            : ctx.AmountParser.ParseAbsOrNull(rawAmount);

        return amount ?? 0m;
    }

    private static bool ShouldSkipSyntheticSweepReinvestment(
        IDictionary<string, string?> row,
        IReadOnlyDictionary<CanonicalField, string> columnsByField)
    {
        var action = (Get(row, columnsByField, CanonicalField.Action) ?? "").Trim();
        if (!action.Contains("reinvestment", StringComparison.OrdinalIgnoreCase))
            return false;

        var symbol = (Get(row, columnsByField, CanonicalField.Symbol) ?? "").Trim();
        if (!FidelityCorePositionSymbols.Contains(symbol))
            return false;

        var type = (Get(row, columnsByField, CanonicalField.Type) ?? "").Trim();
        var description = (Get(row, columnsByField, CanonicalField.Description) ?? "").Trim();

        return type.Contains("cash", StringComparison.OrdinalIgnoreCase)
            || description.Contains("money market", StringComparison.OrdinalIgnoreCase)
            || description.Contains("core", StringComparison.OrdinalIgnoreCase);
    }

    private static string? BuildMemo(
        CanonicalAction action,
        IDictionary<string, string?> row,
        IReadOnlyDictionary<CanonicalField, string> columnsByField)
    {
        var symbol = (Get(row, columnsByField, CanonicalField.Symbol) ?? "").Trim();
        return action switch
        {
            CanonicalAction.Income       => string.IsNullOrWhiteSpace(symbol) ? "Dividend Received" : $"Dividend Received - {symbol}",
            CanonicalAction.CashTransfer => (Get(row, columnsByField, CanonicalField.Action) ?? "").Trim(),
            CanonicalAction.BuyStock     => string.IsNullOrWhiteSpace(symbol) ? "You Bought" : $"You Bought - {symbol}",
            CanonicalAction.SellStock    => string.IsNullOrWhiteSpace(symbol) ? "You Sold" : $"You Sold - {symbol}",
            _                            => Get(row, columnsByField, CanonicalField.Description)
        };
    }
}
