using CsvToOfx.Core.Models;

namespace CsvToOfx.Parsers.Abstractions;
public interface IStatementParser
{
    string ProviderCode { get; }
    ParserCapabilities Capabilities { get; }
    ParseResult Parse(RawStatement input, ParserContext ctx);
}