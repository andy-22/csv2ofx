using System.Collections.Generic;

namespace CsvToOfx.Core.Models;

public sealed record HeaderMap(
    string Name,
    IReadOnlyDictionary<string, CanonicalField> Columns
);
