namespace CsvToOfx.Parsers.Abstractions;
public static class ParserFactory
{
    public static IReadOnlyDictionary<string, IStatementParser> DiscoverParsers()
    {
        var dict = new Dictionary<string, IStatementParser>(StringComparer.OrdinalIgnoreCase);
        var asm = typeof(ParserFactory).Assembly;
        var types = asm.GetTypes()
            .Where(t => !t.IsAbstract && typeof(IStatementParser).IsAssignableFrom(t));
        foreach (var t in types)
        {
            var attr = t.GetCustomAttributes(typeof(ProviderAttribute), false).Cast<ProviderAttribute>().FirstOrDefault();
            if (attr is null) continue;
            dict[attr.Code] = (IStatementParser)Activator.CreateInstance(t)!;
        }
        return dict;
    }
}