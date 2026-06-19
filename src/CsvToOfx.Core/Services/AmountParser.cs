namespace CsvToOfx.Core.Services;
public sealed class AmountParser
{
    public decimal? ParseSignedOrNull(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (decimal.TryParse(s.Replace(",", ""), out var v)) return v;
        throw new FormatException($"Amount '{s}' is not a valid number.");
    }

    public decimal? ParseAbsOrNull(string? s)
    {
        var value = ParseSignedOrNull(s);
        return value.HasValue ? Math.Abs(value.Value) : null;
    }
}
