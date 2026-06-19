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
    public void Parse_SkipsLeadingBlankRows_AndStopsBeforeFooterDisclaimer_ForTradingHeader()
    {
        const string csv = """


Run Date,Action,Symbol,Description,Type,Exchange Quantity,Exchange Currency,Currency,Price,Quantity,Exchange Rate,Commission,Fees,Accrued Interest,Amount,Cash Balance,Settlement Date
3/1/26,You bought,ABC,Alpha Inc,Common Stock,,,USD,10.50,2,,0,0,0,-21.00,100.00,3/3/26
3/2/26,Dividend Received,XYZ,XYZ Dividend,Cash,,,USD,0,0,,0,0,0,5.25,105.25,3/2/26


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
        result.Transactions[0].Currency.Should().Be("USD");
    }

    [Fact]
    public void Parse_HandlesIraHeaderVariant_AndDefaultsCurrency()
    {
        const string csv = """

Run Date,Action,Symbol,Description,Type,Price ($),Quantity,Commission ($),Fees ($),Accrued Interest ($),Amount ($),Cash Balance ($),Settlement Date
3/10/26,You bought,VOO,Vanguard 500 Index Fund,Mutual Fund,510.25,1.5,0,0,0,-765.38,1200.00,3/11/26
3/12/26,Dividend Received,VOO,Vanguard 500 Index Fund,Cash,0,0,0,0,0,7.42,1207.42,3/12/26
""";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var parser = new FidelityParser();
        var ctx = new ParserContext
        {
            AccountId = "acct-ira",
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
        result.Transactions[0].Security!.Id.Should().Be("VOO");
        result.Transactions[0].UnitPrice.Should().Be(510.25m);
        result.Transactions[0].Amount.Should().Be(765.38m);
        result.Transactions[0].Currency.Should().Be("USD");
        result.Transactions[1].Amount.Should().Be(7.42m);
        result.Transactions[1].Memo.Should().Be("Dividend Received - VOO");
    }

    [Fact]
    public void Parse_StillHandlesSimplifiedTradingHeaders()
    {
        const string csv = """
Run Date,Action,Symbol,Description,Type,Price,Quantity,Commission,Fees,Amount
3/15/26,You sold,MSFT,Microsoft Corp,Common Stock,420.10,1,0,0,420.10
""";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var parser = new FidelityParser();
        var ctx = new ParserContext
        {
            AccountId = "acct-simple",
            Institution = "fidelity",
            CurrencyDefault = "USD",
            DateParser = new DateParser(),
            AmountParser = new AmountParser(),
            FitIdGenerator = new FitIdGenerator(),
            SubacctResolver = new SubacctResolver(),
            SecurityResolver = new SecurityResolver(preferCusip: false)
        };

        var result = parser.Parse(new RawStatement("fidelity", stream, ".csv"), ctx);

        result.Transactions.Should().HaveCount(1);
        result.Transactions[0].Security!.Id.Should().Be("MSFT");
        result.Transactions[0].Action.Should().Be(CanonicalAction.SellStock);
        result.Transactions[0].Currency.Should().Be("USD");
    }
}
