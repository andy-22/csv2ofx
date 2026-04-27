using System.Collections.Generic;
using System.Linq;

namespace CsvToOfx.Core.Models;

public sealed record HeaderMap(
    string Name,
    IReadOnlyDictionary<string, CanonicalField> Columns,
    IReadOnlyCollection<CanonicalField>? RequiredFields = null)
{
    public IEnumerable<CanonicalField> EffectiveRequiredFields =>
        RequiredFields is { Count: > 0 } ? RequiredFields : Columns.Values.Distinct();

    public bool TryGetColumnName(CanonicalField field, out string columnName)
    {
        foreach (var entry in Columns)
        {
            if (entry.Value != field)
                continue;

            columnName = entry.Key;
            return true;
        }

        columnName = string.Empty;
        return false;
    }
}
