namespace CsvToOfx.Parsers.Abstractions;
[Flags]
public enum ParserCapabilities { None=0, Csv=1<<0, Ofx=1<<1, Xlsx=1<<2, Bank=1<<3, Brokerage=1<<4, CreditCard=1<<5 }