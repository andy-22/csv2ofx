using CsvToOfx.Core.Models;
using CsvToOfx.Core.Services;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;

namespace CsvToOfx.Core.Tests;

public class AmountParserTests
{
    private readonly AmountParser _parser = new();

    [Theory]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("-78.90", 78.90)]
    [InlineData("100", 100.0)]
    public void ParseAbsOrNull_ReturnsAbsoluteValue_ForValidStrings(string input, decimal expected)
    {
        _parser.ParseAbsOrNull(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ParseAbsOrNull_ReturnsNull_ForNullOrWhitespace(string? input)
    {
        _parser.ParseAbsOrNull(input).Should().BeNull();
    }

    [Fact]
    public void ParseAbsOrNull_ThrowsFormatException_ForInvalidInput()
    {
        Action act = () => _parser.ParseAbsOrNull("invalid");
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("-78.90", -78.90)]
    [InlineData("100", 100.0)]
    public void ParseSignedOrNull_PreservesSign_ForValidStrings(string input, decimal expected)
    {
        _parser.ParseSignedOrNull(input).Should().Be(expected);
    }
}

public class DateParserTests
{
    private readonly DateParser _parser = new();

    [Theory]
    [InlineData("2025-12-04", 2025, 12, 4)]
    [InlineData("12/04/2025", 2025, 12, 4)]
    [InlineData("12-04-2025", 2025, 12, 4)]
    [InlineData("12/04/25", 2025, 12, 4)]
    [InlineData("1/4/25", 2025, 1, 4)]
    public void ParseOrNull_ReturnsDate_ForValidFormats(string input, int y, int m, int d)
    {
        _parser.ParseOrNull(input).Should().Be(new DateTime(y, m, d));
    }

    [Fact]
    public void ParseOrNull_ThrowsFormatException_ForInvalidFormat()
    {
        Action act = () => _parser.ParseOrNull("twentyfive 12 04");
        act.Should().Throw<FormatException>();
    }
}

public class FitIdGeneratorTests
{
    private readonly FitIdGenerator _generator = new();

    [Fact]
    public void FromSortedRow_IsDeterministic()
    {
        var row1 = new Dictionary<string, string?> { ["A"] = "1", ["B"] = "2" };
        var row2 = new Dictionary<string, string?> { ["B"] = "2", ["A"] = "1" };

        var fitId1 = _generator.FromSortedRow(row1);
        var fitId2 = _generator.FromSortedRow(row2);

        fitId1.Should().Be(fitId2);
        fitId1.Should().HaveLength(12);
    }
}

public class OutputPathServiceTests
{
    private readonly OutputPathService _service = new();

    [Fact]
    public void ResolveOfxPath_GeneratesCorrectPath_WhenNotProvided()
    {
        var result = _service.ResolveOfxPath("/path/to/file.csv", null);
        result.Should().Be(Path.Combine("/path/to", "file.ofx"));
    }

    [Fact]
    public void ResolveOfxPath_ReturnsProvidedPath_WhenProvided()
    {
        var result = _service.ResolveOfxPath("/path/to/file.csv", "/another/path.ofx");
        result.Should().Be("/another/path.ofx");
    }
}

public class SplitRatioParserTests
{
    private readonly SplitRatioParser _parser = new();

    [Fact]
    public void Parse_ReturnsRatio_ForValidString()
    {
        var ratio = _parser.Parse("2 for 1");
        ratio.Should().Be(new SplitRatio(2, 1));
    }

    [Theory]
    [InlineData("1:2")]
    [InlineData("abc")]
    [InlineData(null)]
    public void Parse_ReturnsNull_ForInvalidString(string? input)
    {
        _parser.Parse(input).Should().BeNull();
    }
}

public class SubacctResolverTests
{
    private readonly SubacctResolver _resolver = new();

    [Fact]
    public void Resolve_ReturnsCash_ForNormalTransaction()
    {
        var row = new Dictionary<string, string?> { ["Action"] = "Buy" };
        _resolver.Resolve(row, 100).Should().Be("CASH");
    }

    [Fact]
    public void Resolve_ReturnsMargin_WhenTextContainsMargin()
    {
        var row = new Dictionary<string, string?> { ["Description"] = "stuff on margin" };
        _resolver.Resolve(row, 100).Should().Be("MARGIN");
    }

    [Fact]
    public void Resolve_ReturnsShort_ForNegativeUnits()
    {
        var row = new Dictionary<string, string?> { ["Action"] = "Sell" };
        _resolver.Resolve(row, -50).Should().Be("SHORT");
    }
}
