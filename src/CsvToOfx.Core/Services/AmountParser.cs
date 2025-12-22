namespace CsvToOfx.Core.Services;
public sealed class AmountParser
{
    public decimal? ParseAbsOrNull(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (decimal.TryParse(s.Replace(",", ""), out var v)) return Math.Abs(v);
        throw new FormatException($"Amount '{s}' is not a valid number.");
    }
}