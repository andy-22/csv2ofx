using CsvToOfx.Core.Models;

namespace CsvToOfx.Core.Parsing;

public interface ISecurityResolver
{
    SecurityRef? Resolve(string symbol);
    SecurityRef? ResolveFromRow(IDictionary<string, string?> row);
}

