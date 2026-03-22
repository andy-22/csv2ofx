using System.Text;
using CsvToOfx.Core.Models;
using CsvToOfx.Core.Services;
using CsvToOfx.Parsers.Abstractions;
using CsvToOfx.Parsers.Providers.Fidelity;
using FluentAssertions;

namespace CsvToOfx.Parsers.Tests;

public class FidelityParserTests
{
    [Fact]
    public void Parse_SkipsLeadingBlankRows_AndStopsBeforeFooterDisclaimer()
    {
        const string csv = """


Run Date,Action,Symbol,Description,Type,Price,Quantity,Commission,Fees,Amount
3/1/26,You bought,ABC,Alpha Inc,Common Stock,10.50,2,0,0,-21.00
3/2/26,Dividend Received,XYZ,XYZ Dividend,Cash,0,0,0,0,5.25


The data in this file is for informational purposes only.
Please review your official statement for complete details.
""";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var parser = new FidelityParser();
        var ctx = new ParserContext
        {
            AccountId = "acct-1",
            Institution = "fidelity",
            CurrencyDefault = "USD",
            DateParser = new DateParser(),
            AmountParser = new AmountParser(),
            FitIdGenerator = new FitIdGenerator(),
            SubacctResolver = new SubacctResolver(),
            SecurityResolver = new SecurityResolver(preferCusip: false)
        };

        var result = parser.Parse(new RawStatement("fidelity", stream, ".csv"), ctx);

        result.Transactions.Should().HaveCount(2);
        result.Transactions[0].Security!.Id.Should().Be("ABC");
        result.Transactions[0].Amount.Should().Be(21.00m);
        result.Transactions[1].Security!.Id.Should().Be("XYZ");
        result.Transactions[1].Amount.Should().Be(5.25m);
    }
}
