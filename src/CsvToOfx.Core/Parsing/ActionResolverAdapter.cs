using CsvToOfx.Core.Models;

namespace CsvToOfx.Core.Parsing;

public sealed class ActionResolverAdapter : IActionResolver
{
    public CanonicalAction Resolve(string? actionText)
    {
        var a = (actionText ?? string.Empty).Trim().ToLowerInvariant();
        if (a.Contains("transfer") || a.Contains("transferred")) return CanonicalAction.CashTransfer;
        if (a.Contains("reverse split") || a.Contains("stock split") || a.Contains("split r/s") || a.Contains("r/s to") || a.Contains("r/s from")) return CanonicalAction.StockSplit;
        if (a.Contains("dividend") || a.Contains("interest") || a.Contains("return of capital") || a.Contains("in lieu")) return CanonicalAction.Income;
        if (a.Contains("bought") || a.Contains("buy") || a.Contains("purchase") || a.Contains("reinvestment")) return CanonicalAction.BuyStock;
        if (a.Contains("sold") || a.Contains("sell")) return CanonicalAction.SellStock;
        if (a.Contains("fee charged") || a.Contains("adr fee") || a.Contains("adr pass-through fee")
            || a.Contains("foreign tax paid") || a.Contains("withholding tax") || a.Contains("tax withheld")) return CanonicalAction.MiscExpense;
        return CanonicalAction.CashTransfer;
    }
}
