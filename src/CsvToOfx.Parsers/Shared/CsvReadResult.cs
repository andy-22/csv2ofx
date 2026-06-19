using CsvToOfx.Core.Models;

namespace CsvToOfx.Parsers.Shared;

public sealed record CsvReadResult(
    HeaderMap HeaderMap,
    IReadOnlyList<IDictionary<string, string?>> Rows
);
