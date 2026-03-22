using System;
using System.Collections.Generic;
using CsvToOfx.Core.Models;

namespace CsvToOfx.Core.Parsing.HeaderMaps;

public static class FidelityIraHeaderMap
{
    public static HeaderMap Instance { get; } = new HeaderMap(
        "Fidelity-IRA",
        new Dictionary<string, CanonicalField>(StringComparer.OrdinalIgnoreCase)
        {
            ["Run Date"] = CanonicalField.TradeDate,
            ["Action"] = CanonicalField.Action,
            ["Symbol"] = CanonicalField.Symbol,
            ["Description"] = CanonicalField.Description,
            ["Type"] = CanonicalField.Type,
            ["Price ($)"] = CanonicalField.Price,
            ["Quantity"] = CanonicalField.Quantity,
            ["Commission ($)"] = CanonicalField.Commission,
            ["Fees ($)"] = CanonicalField.Fees,
            ["Accrued Interest ($)"] = CanonicalField.AccruedInterest,
            ["Amount ($)"] = CanonicalField.Amount,
            ["Cash Balance ($)"] = CanonicalField.CashBalance,
            ["Settlement Date"] = CanonicalField.SettlementDate
        });
}

