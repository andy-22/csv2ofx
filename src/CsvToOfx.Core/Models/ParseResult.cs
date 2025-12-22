namespace CsvToOfx.Core.Models;
public sealed record ParseResult(
    AccountRef Account,
    IReadOnlyList<NormalizedTransaction> Transactions,
    IReadOnlyList<SecurityRef>? Securities = null
);
