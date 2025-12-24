using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvToOfx.Core.Models;

namespace CsvToOfx.Core.Parsing;

public sealed class TransactionCsvParser
{
    private readonly HeaderMap _headerMap;
    private readonly ISecurityResolver _securityResolver;
    private readonly IActionResolver _actionResolver;

    public TransactionCsvParser(
        HeaderMap headerMap,
        ISecurityResolver securityResolver,
        IActionResolver actionResolver)
    {
        _headerMap = headerMap;
        _securityResolver = securityResolver;
        _actionResolver = actionResolver;
    }

    public IEnumerable<NormalizedTransaction> Parse(TextReader reader)
    {
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        });

        if (!csv.Read() || !csv.ReadHeader())
            yield break;

        var indexMap = BuildIndexMap(csv.HeaderRecord);
        if (indexMap.Count == 0)
            yield break;

        while (csv.Read())
        {
            if (string.IsNullOrWhiteSpace(csv.Parser.RawRecord))
                continue;

            DateTime tradeDate = csv.GetField<DateTime>(indexMap[CanonicalField.TradeDate]);
            var action = _actionResolver.Resolve(csv.GetField(indexMap[CanonicalField.Action]));
            var security = ResolveSecurity(csv, indexMap);
            decimal? units = GetOptional<decimal>(csv, indexMap, CanonicalField.Quantity);
            decimal? price = GetOptional<decimal>(csv, indexMap, CanonicalField.Price);
            decimal amount = csv.GetField<decimal>(indexMap[CanonicalField.Amount]);
            string currency = GetOptional<string>(csv, indexMap, CanonicalField.Currency) ?? "USD";
            string? memo = GetOptional<string>(csv, indexMap, CanonicalField.Description);
            decimal? fees = GetOptional<decimal>(csv, indexMap, CanonicalField.Fees);
            string? fitId = null;

            yield return new NormalizedTransaction(
                tradeDate,
                action,
                security,
                units,
                price,
                amount,
                currency,
                memo,
                fees,
                fitId);
        }
    }

    private Dictionary<CanonicalField, int> BuildIndexMap(string[] headerRecord)
    {
        var indexMap = new Dictionary<CanonicalField, int>();
        for (int i = 0; i < headerRecord.Length; i++)
        {
            var header = headerRecord[i].Trim();
            if (_headerMap.Columns.TryGetValue(header, out var field))
                indexMap[field] = i;
        }
        return indexMap;
    }

    private T? GetOptional<T>(CsvReader csv, Dictionary<CanonicalField, int> indexMap, CanonicalField field)
    {
        if (!indexMap.TryGetValue(field, out var idx))
            return default;
        var raw = csv.GetField(idx);
        if (string.IsNullOrWhiteSpace(raw))
            return default;
        return csv.GetField<T>(idx);
    }

    private SecurityRef? ResolveSecurity(CsvReader csv, Dictionary<CanonicalField, int> indexMap)
    {
        if (!indexMap.TryGetValue(CanonicalField.Symbol, out var idx))
            return null;
        var symbol = csv.GetField(idx);
        return _securityResolver.Resolve(symbol);
    }
}

