using System.Globalization;
using CsvToOfx.Core.Services;

namespace CsvToOfx.Parsers.Abstractions;
public sealed class ParserContext
{
    public SecurityResolver SecurityResolver { get; init; } = default!;
    public DateParser DateParser { get; init; } = default!;
    public AmountParser AmountParser { get; init; } = default!;
    public FitIdGenerator FitIdGenerator { get; init; } = default!;
    public SubacctResolver SubacctResolver { get; init; } = default!;
    public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;
    public DateTime? StartDateFilter { get; init; }
    public string AccountId { get; init; } = "";
    public string Institution { get; init; } = "";
    public string CurrencyDefault { get; init; } = "USD";
}