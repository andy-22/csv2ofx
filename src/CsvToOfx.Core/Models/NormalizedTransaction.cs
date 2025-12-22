namespace CsvToOfx.Core.Models;
public sealed record NormalizedTransaction(
    DateTime TradeDate,
    CanonicalAction Action,
    SecurityRef? Security,
    decimal? Units,
    decimal? UnitPrice,
    decimal Amount,     // normalized sign
    string Currency,
    string? Memo,
    decimal? Fees,
    string? FitId
);