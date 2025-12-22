using CsvToOfx.Core.Models;
namespace CsvToOfx.Writers.Ofx;
public interface IOfxWriter
{
    string WriteInvestmentStatement(ParseResult result, bool includeSecurityList, string? currencyOverride = null);
}