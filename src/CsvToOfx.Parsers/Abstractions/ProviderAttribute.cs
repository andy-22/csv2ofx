namespace CsvToOfx.Parsers.Abstractions;
[AttributeUsage(AttributeTargets.Class)]
public sealed class ProviderAttribute : Attribute
{
    public ProviderAttribute(string code) => Code = code;
    public string Code { get; }
}