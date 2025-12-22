using System.Text.RegularExpressions;

namespace CsvToOfx.Core.Services;
public sealed record SplitRatio(int Numerator, int Denominator);

public sealed class SplitRatioParser
{
    public SplitRatio? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var m = Regex.Match(text.ToLowerInvariant(), @"(\d+)\s*[- ]?\s*for\s*[- ]?\s*(\d+)");
        return m.Success ? new SplitRatio(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)) : null;
    }
}