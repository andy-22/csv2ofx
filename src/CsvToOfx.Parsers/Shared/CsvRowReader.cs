using CsvHelper;
using CsvHelper.Configuration;
using CsvToOfx.Core.Models;
using System.Globalization;

namespace CsvToOfx.Parsers.Shared;
public sealed class CsvRowReader
{
    private readonly CsvConfiguration _conf = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        DetectColumnCountChanges = false,
        BadDataFound = null,
        TrimOptions = TrimOptions.Trim
    };

    public IEnumerable<IDictionary<string,string?>> ReadRows(Stream csv, IEnumerable<string>? requiredHeaders = null)
    {
        using var reader = new StreamReader(csv);
        using var csvr = new CsvReader(reader, _conf);

        string[]? headerRecord = null;
        var seenData = false;
        var requiredHeaderSet = requiredHeaders is null
            ? null
            : new HashSet<string>(requiredHeaders, StringComparer.OrdinalIgnoreCase);

        while (csvr.Read())
        {
            var record = csvr.Parser.Record;
            if (record is null || record.Length == 0)
                continue;

            var nonEmpty = record.Count(v => !string.IsNullOrWhiteSpace(v));

            if (nonEmpty == 0)
            {
                if (seenData)
                    yield break; // blank row after data marks start of disclaimer/footer

                continue; // skip leading blank lines
            }

            if (headerRecord is null)
            {
                if (!MatchesHeader(record, requiredHeaderSet))
                    continue;

                headerRecord = record.Select(v => v?.Trim() ?? string.Empty).ToArray();
                continue;
            }

            // drop rows that don't match header shape (e.g., disclaimers/footers)
            if (record.Length < headerRecord.Length)
                continue;

            var converted = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headerRecord.Length; i++)
                converted[headerRecord[i]] = i < record.Length ? record[i] : null;

            seenData = true;
            yield return converted;
        }
    }

    public CsvReadResult? ReadRows(Stream csv, IEnumerable<HeaderMap> headerMaps)
    {
        using var reader = new StreamReader(csv);
        using var csvr = new CsvReader(reader, _conf);

        var candidates = headerMaps.ToList();
        HeaderMap? matchedHeaderMap = null;
        string[]? headerRecord = null;
        var rows = new List<IDictionary<string, string?>>();
        var seenData = false;

        while (csvr.Read())
        {
            var record = csvr.Parser.Record;
            if (record is null || record.Length == 0)
                continue;

            var nonEmpty = record.Count(v => !string.IsNullOrWhiteSpace(v));
            if (nonEmpty == 0)
            {
                if (seenData)
                    break;

                continue;
            }

            if (matchedHeaderMap is null)
            {
                matchedHeaderMap = MatchHeader(record, candidates);
                if (matchedHeaderMap is null)
                    continue;

                headerRecord = record.Select(v => v?.Trim() ?? string.Empty).ToArray();
                continue;
            }

            if (headerRecord is null || record.Length < headerRecord.Length)
                continue;

            var converted = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headerRecord.Length; i++)
                converted[headerRecord[i]] = i < record.Length ? record[i] : null;

            seenData = true;
            rows.Add(converted);
        }

        return matchedHeaderMap is null ? null : new CsvReadResult(matchedHeaderMap, rows);
    }

    private static bool MatchesHeader(string[] record, HashSet<string>? requiredHeaders)
    {
        var normalized = record
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalized.Count == 0)
            return false;

        if (requiredHeaders is null || requiredHeaders.Count == 0)
            return true;

        return requiredHeaders.IsSubsetOf(normalized);
    }

    private static HeaderMap? MatchHeader(string[] record, IReadOnlyCollection<HeaderMap> headerMaps)
    {
        var normalized = record
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalized.Count == 0)
            return null;

        HeaderMap? bestMatch = null;
        var bestScore = -1;

        foreach (var headerMap in headerMaps)
        {
            var matchedFields = headerMap.Columns
                .Where(entry => normalized.Contains(entry.Key))
                .Select(entry => entry.Value)
                .ToHashSet();

            if (!headerMap.EffectiveRequiredFields.All(matchedFields.Contains))
                continue;

            var score = matchedFields.Count;
            if (score <= bestScore)
                continue;

            bestMatch = headerMap;
            bestScore = score;
        }

        return bestMatch;
    }
}
