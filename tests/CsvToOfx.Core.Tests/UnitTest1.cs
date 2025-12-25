using CsvToOfx.Core.Models;
using CsvToOfx.Core.Parsing;
using CsvToOfx.Core.Services;
using FluentAssertions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace CsvToOfx.Core.Tests
{
    public class TransactionCsvParserTests
    {
        [Fact]
        public void Parse_SkipsBlankLines_ReturnsTransactions()
        {
            var headerMap = new HeaderMap("Test",
                new Dictionary<string, CanonicalField>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Run Date"] = CanonicalField.TradeDate,
                    ["Action"] = CanonicalField.Action,
                    ["Symbol"] = CanonicalField.Symbol,
                    ["Description"] = CanonicalField.Description,
                    ["Price"] = CanonicalField.Price,
                    ["Quantity"] = CanonicalField.Quantity,
                    ["Amount"] = CanonicalField.Amount,
                    ["Currency"] = CanonicalField.Currency
                });

            var csv = "\n" +
                      "Run Date,Action,Symbol,Description,Price,Quantity,Amount,Currency\n" +
                      "2025-01-01,Buy,ABC,Alpha,10.5,2,21,USD\n" +
                      "\n" +
                      "2025-01-02,Dividend,XYZ,Beta,,,5,USD\n" +
                      "\n\n";

            var parser = new TransactionCsvParser(headerMap, new StubSecurityResolver(), new StubActionResolver());
            var results = parser.Parse(new StringReader(csv)).ToList();

            results.Should().HaveCount(2);
            results[0].TradeDate.Should().Be(DateTime.Parse("2025-01-01"));
            results[0].Action.Should().Be(CanonicalAction.BuyStock);
            results[0].Security!.Id.Should().Be("ABC");
            results[0].Units.Should().Be(2);
            results[0].UnitPrice.Should().Be(10.5m);
            results[0].Amount.Should().Be(21m);

            results[1].Action.Should().Be(CanonicalAction.Income);
            results[1].Security!.Id.Should().Be("XYZ");
            results[1].Units.Should().Be(0);
            results[1].UnitPrice.Should().Be(0);
            results[1].Amount.Should().Be(5m);
            results[1].Fees.Should().Be(0);
        }

        [Fact]
        public void Parse_ReturnsNulls_WhenOptionalColumnsMissing()
        {
            var headerMap = new HeaderMap("Test",
                new Dictionary<string, CanonicalField>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Run Date"] = CanonicalField.TradeDate,
                    ["Action"] = CanonicalField.Action,
                    ["Symbol"] = CanonicalField.Symbol,
                    ["Amount"] = CanonicalField.Amount
                });

            var csv = "Run Date,Action,Symbol,Amount\n" +
                      "2025-01-01,Buy,ABC,21\n";

            var parser = new TransactionCsvParser(headerMap, new StubSecurityResolver(), new StubActionResolver());
            var results = parser.Parse(new StringReader(csv)).ToList();
            var resultsCount = results.Count;
            resultsCount.Should().Be(1);
            results[0].Units.Should().Be(0);
            results[0].UnitPrice.Should().Be(0);
            results[0].Currency.Should().Be("USD");
            results[0].Fees.Should().Be(0);
        }
    }

    public class SecurityResolverTests
    {
        [Fact]
        public void ResolveFromRow_UsesTicker_WhenNoCusip()
        {
            var resolver = new SecurityResolver(preferCusip: false);
            var row = new Dictionary<string, string?> { ["Symbol"] = "ABC", ["Description"] = "Alpha" };

            var sec = resolver.ResolveFromRow(row);

            sec.Should().NotBeNull();
            sec!.Id.Should().Be("ABC");
            sec.IdType.Should().Be(IdType.Ticker);
            sec.Name.Should().Be("Alpha (ABC)");
            sec.Ticker.Should().Be("ABC");
        }

        [Fact]
        public void ResolveFromRow_DetectsCusip_WhenSymbolLooksLikeCusip()
        {
            var resolver = new SecurityResolver(preferCusip: false);
            var row = new Dictionary<string, string?> { ["Symbol"] = "123456789" };

            var sec = resolver.ResolveFromRow(row);

            sec.Should().NotBeNull();
            sec!.IdType.Should().Be(IdType.Cusip);
            sec.Name.Should().Be("123456789");
            sec.Ticker.Should().BeNull();
        }

        [Fact]
        public void Resolve_FallsBackToRowResolution()
        {
            var resolver = new SecurityResolver(preferCusip: false);
            var sec = resolver.Resolve("XYZ");

            sec.Should().NotBeNull();
            sec!.Id.Should().Be("XYZ");
            sec.Name.Should().Be("XYZ");
        }
    }

    public class ActionResolverAdapterTests
    {
        private readonly ActionResolverAdapter _adapter = new();

        [Theory]
        [InlineData("Dividend", CanonicalAction.Income)]
        [InlineData("You bought", CanonicalAction.BuyStock)]
        [InlineData("Sold shares", CanonicalAction.SellStock)]
        [InlineData("Transfer", CanonicalAction.CashTransfer)]
        [InlineData("Fee charged", CanonicalAction.MiscExpense)]
        public void Resolve_MapsCommonActions(string input, CanonicalAction expected)
        {
            _adapter.Resolve(input).Should().Be(expected);
        }
    }

    // Test doubles
    file sealed class StubSecurityResolver : ISecurityResolver
    {
        public SecurityRef? Resolve(string symbol) => new(symbol, IdType.Ticker, symbol, symbol);
        public SecurityRef? ResolveFromRow(IDictionary<string, string?> row)
        {
            row.TryGetValue("Symbol", out var symbol);
            return symbol is null ? null : Resolve(symbol);
        }
    }

    file sealed class StubActionResolver : IActionResolver
    {
        public CanonicalAction Resolve(string? actionText)
        {
            var text = (actionText ?? "").ToLowerInvariant();
            if (text.Contains("div")) return CanonicalAction.Income;
            if (text.Contains("buy")) return CanonicalAction.BuyStock;
            return CanonicalAction.CashTransfer;
        }
    }
}