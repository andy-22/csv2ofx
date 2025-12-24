using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace CsvToOfx.Parsers.Shared;
public sealed class CsvRowReader
{
    private readonly CsvConfiguration _conf = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        DetectColumnCountChanges = false,
        BadDataFound = null,
        TrimOptions = TrimOptions.Trim
    };

    public IEnumerable<IDictionary<string,string?>> ReadRows(Stream csv)
    {
        using var reader = new StreamReader(csv);
        using var csvr = new CsvReader(reader, _conf);

        var seenData = false;
        while (csvr.Read())
        {
            var colCount = csvr.Parser.Count;
            if (colCount == 0) continue; // skip empty physical rows

            var dict = csvr.GetRecord<dynamic>() as IDictionary<string, object>;
            if (dict is null) continue;

            // drop rows that don't match header shape (e.g., disclaimers/footers)
            if (csvr.HeaderRecord is { Length: > 0 } && colCount < csvr.HeaderRecord.Length)
                continue;

            var converted = dict.ToDictionary(k => k.Key, v => v.Value?.ToString());
            var nonEmpty = converted.Values.Count(v => !string.IsNullOrWhiteSpace(v));

            if (nonEmpty == 0)
            {
                if (seenData)
                    yield break; // blank row after data marks start of disclaimer/footer

                continue; // skip leading blank lines
            }

            seenData = true;
            yield return converted;
        }
    }
}