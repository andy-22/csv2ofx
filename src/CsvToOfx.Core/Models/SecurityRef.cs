namespace CsvToOfx.Core.Models;
public enum IdType { Cusip, Isin, Sedol, Ticker }
public sealed record SecurityRef(string Id, IdType IdType, string? Name = null, string? Ticker = null);