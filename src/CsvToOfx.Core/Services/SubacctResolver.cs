namespace CsvToOfx.Core.Services;

public sealed class SubacctResolver
{
    public string Resolve(IDictionary<string, string?> row, decimal? units)
    {
        row.TryGetValue("Action", out var action);
        row.TryGetValue("Description", out var description);

        var txt = $"{action ?? ""} {description ?? ""}".ToLowerInvariant();

        if (txt.Contains("short") || (units.HasValue && units.Value < 0)) return "SHORT";
        if (txt.Contains("margin")) return "MARGIN";
        return "CASH";
    }
}