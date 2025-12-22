using System.Text;
using CsvToOfx.Core.Models;

namespace CsvToOfx.Writers.Ofx
{
    public sealed class OfxWriter : IOfxWriter
    {
        public string WriteInvestmentStatement(ParseResult result, bool includeSecurityList, string? currencyOverride = null)
        {
            var sb = new StringBuilder();

            // Header + root
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<?OFX OFXHEADER=\"200\" VERSION=\"211\" SECURITY=\"NONE\" OLDFILEUID=\"NONE\" NEWFILEUID=\"NONE\"?>");
            sb.AppendLine("<OFX>");
            sb.AppendLine("  <INVSTMTMSGSRSV1>");
            sb.AppendLine("    <INVSTMTTRNRS>");
            sb.AppendLine("      <INVSTMTRS>");
            sb.AppendLine($"        <INVACCTFROM><ACCTID>{Escape(result.Account.AccountId)}</ACCTID></INVACCTFROM>");
            sb.AppendLine("        <INVTRANLIST>");

            foreach (var t in result.Transactions)
            {
                switch (t.Action)
                {
                    case CanonicalAction.BuyStock:
                        sb.AppendLine("          <BUYSTOCK>");
                        sb.AppendLine("            <INVBUY>");
                        WriteInvTran(sb, t);
                        WriteSecId(sb, t.Security);
                        WriteSubacct(sb, t);
                        if (t.Units.HasValue)     sb.AppendLine($"              <UNITS>{FmtUnits(t.Units.Value)}</UNITS>");
                        if (t.UnitPrice.HasValue) sb.AppendLine($"              <UNITPRICE>{FmtPrice(t.UnitPrice.Value)}</UNITPRICE>");
                        sb.AppendLine($"              <TOTAL>{FmtAmount(t.Amount)}</TOTAL>");
                        sb.AppendLine("            </INVBUY>");
                        sb.AppendLine("          </BUYSTOCK>");
                        break;

                    case CanonicalAction.SellStock:
                        sb.AppendLine("          <SELLSTOCK>");
                        sb.AppendLine("            <INVSELL>");
                        WriteInvTran(sb, t);
                        WriteSecId(sb, t.Security);
                        WriteSubacct(sb, t);
                        if (t.Units.HasValue)     sb.AppendLine($"              <UNITS>{FmtUnits(t.Units.Value)}</UNITS>");
                        if (t.UnitPrice.HasValue) sb.AppendLine($"              <UNITPRICE>{FmtPrice(t.UnitPrice.Value)}</UNITPRICE>");
                        sb.AppendLine($"              <TOTAL>{FmtAmount(t.Amount)}</TOTAL>");
                        sb.AppendLine("            </INVSELL>");
                        sb.AppendLine("          </SELLSTOCK>");
                        break;

                    case CanonicalAction.Income:
                        sb.AppendLine("          <INCOME>");
                        WriteInvTran(sb, t);
                        WriteSecId(sb, t.Security);
                        WriteSubacct(sb, t);
                        sb.AppendLine("            <INCOMETYPE>DIV</INCOMETYPE>");  // matches your Python
                        sb.AppendLine($"            <TOTAL>{FmtAmount(t.Amount)}</TOTAL>");
                        sb.AppendLine("          </INCOME>");
                        break;

                    case CanonicalAction.MiscExpense:
                        sb.AppendLine("          <INVEXPENSE>");
                        WriteInvTran(sb, t);
                        WriteSecId(sb, t.Security);
                        WriteSubacct(sb, t);
                        sb.AppendLine($"            <TOTAL>{FmtAmount(t.Amount)}</TOTAL>");
                        sb.AppendLine("          </INVEXPENSE>");
                        break;

                    case CanonicalAction.StockSplit:
                        sb.AppendLine("          <STOCKSPLIT>");
                        WriteInvTran(sb, t);
                        WriteSecId(sb, t.Security);
                        WriteSubacct(sb, t);
                        // If you later carry Old/New units in NormalizedTransaction, write them here:
                        // sb.AppendLine($"            <OLDUNITS>{FmtUnits(oldUnits)}</OLDUNITS>");
                        // sb.AppendLine($"            <NEWUNITS>{FmtUnits(newUnits)}</NEWUNITS>");
                        sb.AppendLine("          </STOCKSPLIT>");
                        break;

                    case CanonicalAction.CashTransfer:
                        sb.AppendLine("          <INVBANKTRAN>");
                        WriteInvTran(sb, t);
                        // SUBACCTFUND for cash transfer
                        sb.AppendLine("            <SUBACCTFUND>CASH</SUBACCTFUND>");
                        // STMTTRN child (Moneydance expects this in bank tran aggregates)
                        sb.AppendLine("            <STMTTRN>");
                        sb.AppendLine($"              <DTPOSTED>{t.TradeDate:yyyyMMdd}</DTPOSTED>");
                        sb.AppendLine($"              <TRNAMT>{FmtAmount(t.Amount)}</TRNAMT>");
                        sb.AppendLine("              <TRNTYPE>XFER</TRNTYPE>");
                        sb.AppendLine($"              <FITID>{Escape(t.FitId)}</FITID>");
                        sb.AppendLine($"              <NAME>{Escape(t.Memo)}</NAME>");
                        sb.AppendLine($"              <MEMO>{Escape(t.Memo)}</MEMO>");
                        sb.AppendLine($"              <CURRENCY>{Escape(t.Currency)}</CURRENCY>");
                        sb.AppendLine("            </STMTTRN>");
                        sb.AppendLine("          </INVBANKTRAN>");
                        break;
                }
            }

            sb.AppendLine("        </INVTRANLIST>");
            sb.AppendLine("      </INVSTMTRS>");
            sb.AppendLine("    </INVSTMTTRNRS>");
            sb.AppendLine("  </INVSTMTMSGSRSV1>");

            if (includeSecurityList)
                WriteSecListMessageSet(sb, result);

            sb.AppendLine("</OFX>");

            return sb.ToString();
        }

        // ----- helpers -----
        private static void WriteInvTran(StringBuilder sb, NormalizedTransaction t)
        {
            sb.AppendLine("            <INVTRAN>");
            sb.AppendLine($"              <FITID>{Escape(t.FitId)}</FITID>");
            sb.AppendLine($"              <DTTRADE>{t.TradeDate:yyyyMMdd}</DTTRADE>");
            if (!string.IsNullOrWhiteSpace(t.Memo))
                sb.AppendLine($"              <MEMO>{Escape(t.Memo)}</MEMO>");
            sb.AppendLine("            </INVTRAN>");
        }

        private static void WriteSecId(StringBuilder sb, SecurityRef? s)
        {
            if (s is null) return;
            var idType = s.IdType.ToString().ToUpperInvariant(); // CUSIP or TICKER
            sb.AppendLine("            <SECID>");
            sb.AppendLine($"              <UNIQUEID>{Escape(s.Id)}</UNIQUEID>");
            sb.AppendLine($"              <UNIQUEIDTYPE>{idType}</UNIQUEIDTYPE>");
            sb.AppendLine("            </SECID>");
        }

        private static void WriteSecListMessageSet(StringBuilder sb, ParseResult result)
        {
            if (result.Securities is null || result.Securities.Count == 0) return;
            sb.AppendLine("  <SECLISTMSGSRSV1>");
            sb.AppendLine("    <SECLIST>");
            foreach (var s in result.Securities)
            {
                sb.AppendLine("      <STOCKINFO>");
                sb.AppendLine("        <SECINFO>");
                WriteSecId(sb, s);
                if (!string.IsNullOrWhiteSpace(s.Name))
                    sb.AppendLine($"          <SECNAME>{Escape(s.Name)}</SECNAME>");
                if (!string.IsNullOrWhiteSpace(s.Ticker))
                    sb.AppendLine($"          <TICKER>{Escape(s.Ticker)}</TICKER>");
                sb.AppendLine("        </SECINFO>");
                sb.AppendLine("      </STOCKINFO>");
            }
            sb.AppendLine("    </SECLIST>");
            sb.AppendLine("  </SECLISTMSGSRSV1>");
        }

        private static void WriteSubacct(StringBuilder sb, NormalizedTransaction t)
        {
            // We default both to CASH for now; you can set SHORT/MARGIN via parser context later
            sb.AppendLine("            <SUBACCTSEC>CASH</SUBACCTSEC>");
            sb.AppendLine("            <SUBACCTFUND>CASH</SUBACCTFUND>");
        }

        private static string Escape(string? s) =>
            (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private static string FmtAmount(decimal v) => v.ToString("0.##");
        private static string FmtUnits(decimal v)  => v.ToString("0.######");
        private static string FmtPrice(decimal v)  => v.ToString("0.######");
    }
}
