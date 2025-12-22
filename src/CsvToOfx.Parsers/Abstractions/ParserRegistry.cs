namespace CsvToOfx.Parsers.Abstractions;
public static class ParserRegistry
{
    private static IReadOnlyDictionary<string, IStatementParser> _parsers = new Dictionary<string, IStatementParser>();
    public static void Initialize(IReadOnlyDictionary<string, IStatementParser> map) => _parsers = map;
    public static IStatementParser? Resolve(string code) => _parsers.TryGetValue(code, out var p) ? p : null;
    public static IEnumerable<string> List() => _parsers.Keys;
}