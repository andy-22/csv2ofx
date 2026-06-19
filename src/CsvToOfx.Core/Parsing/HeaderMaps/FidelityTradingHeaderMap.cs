using System;
using System.Collections.Generic;
using CsvToOfx.Core.Models;

namespace CsvToOfx.Core.Parsing.HeaderMaps;

public static class FidelityTradingHeaderMap
{
    private static readonly CanonicalField[] RequiredFields =
    {
        CanonicalField.TradeDate,
        CanonicalField.Action,
        CanonicalField.Symbol,
        CanonicalField.Description,
        CanonicalField.Price,
        CanonicalField.Quantity,
        CanonicalField.Fees,
        CanonicalField.Amount
    };

    public static HeaderMap Instance { get; } = new HeaderMap(
        "Fidelity-Trading",
        new Dictionary<string, CanonicalField>(StringComparer.OrdinalIgnoreCase)
        {
            ["Run Date"] = CanonicalField.TradeDate,
            ["Action"] = CanonicalField.Action,
            ["Symbol"] = CanonicalField.Symbol,
            ["Description"] = CanonicalField.Description,
            ["Type"] = CanonicalField.Type,
            ["Exchange Quantity"] = CanonicalField.ExchangeQuantity,
            ["Exchange Currency"] = CanonicalField.ExchangeCurrency,
            ["Currency"] = CanonicalField.Currency,
            ["Price"] = CanonicalField.Price,
            ["Quantity"] = CanonicalField.Quantity,
            ["Exchange Rate"] = CanonicalField.ExchangeRate,
            ["Commission"] = CanonicalField.Commission,
            ["Fees"] = CanonicalField.Fees,
            ["Accrued Interest"] = CanonicalField.AccruedInterest,
            ["Amount"] = CanonicalField.Amount,
            ["Cash Balance"] = CanonicalField.CashBalance,
            ["Settlement Date"] = CanonicalField.SettlementDate
        },
        RequiredFields);
}
