using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace CsvToOfx.Parsers.Shared;
public sealed class CsvRowReader
{
    private readonly CsvConfiguration _conf = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        DetectColumnCountChanges = true,
        BadDataFound = null,
        TrimOptions = TrimOptions.Trim
    };

    public IEnumerable<IDictionary<string,string?>> ReadRows(Stream csv)
    {
        using var reader = new StreamReader(csv);
        using var csvr = new CsvReader(reader, _conf);
        while (csvr.Read())
        {
            var dict = csvr.GetRecord<dynamic>() as IDictionary<string, object>;
            yield return dict!.ToDictionary(k => k.Key, v => v.Value?.ToString());
        }
    }
}