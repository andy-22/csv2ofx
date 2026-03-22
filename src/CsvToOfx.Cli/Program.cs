using CsvToOfx.Core.Models;
using CsvToOfx.Core.Services;
using CsvToOfx.Parsers.Abstractions;
using CsvToOfx.Writers.Ofx;
using Microsoft.Extensions.DependencyInjection;

// -------------------------------
// 1) DI container
// -------------------------------
var preferCusip = true;
var securityArg = GetArg("security-id-type", args);
if (!string.IsNullOrWhiteSpace(securityArg) && securityArg.Equals("ticker", StringComparison.OrdinalIgnoreCase))
    preferCusip = false;

var services = new ServiceCollection()
    // Core services (make sure these exist in CsvToOfx.Core/Services)
    .AddSingleton<DateParser>()
    .AddSingleton<AmountParser>()
    .AddSingleton<FitIdGenerator>()
    .AddSingleton<SubacctResolver>()
    .AddSingleton<OutputPathService>()
    .AddSingleton<SecurityResolver>(_ => new SecurityResolver(preferCusip))
    // Writers
    .AddSingleton<IOfxWriter, OfxWriter>()
    .BuildServiceProvider();

// -------------------------------
// 2) Discover & init parsers in CsvToOfx.Parsers
// -------------------------------
ParserRegistry.Initialize(ParserFactory.DiscoverParsers());

// -------------------------------
// 3) Argument parsing helper
//    (supports both --key=value and --key value)
// -------------------------------
static string GetArg(string key, string[] args)
{
    var kv = args.FirstOrDefault(a => a.StartsWith($"--{key}="));
    if (kv is not null) return kv.Split('=', 2)[1];
    for (int i = 0; i < args.Length; i++)
        if (args[i] == $"--{key}" && i + 1 < args.Length)
            return args[i + 1];
    return "";
}

// -------------------------------
// 4) Read args
// -------------------------------
var source   = GetArg("source",   args); if (string.IsNullOrWhiteSpace(source)) source = "fidelity";
var csvPath  = GetArg("csv",      args); if (string.IsNullOrWhiteSpace(csvPath)) { Console.Error.WriteLine("Missing --csv"); return 1; }
var acctId   = GetArg("acct-id",  args); if (string.IsNullOrWhiteSpace(acctId)) { Console.Error.WriteLine("Missing --acct-id"); return 1; }
var ofxPath  = GetArg("ofx",      args);
var startStr = GetArg("start-date", args);

// -------------------------------
// 5) Resolve parser
// -------------------------------
var parser = ParserRegistry.Resolve(source);
if (parser is null) { Console.Error.WriteLine($"Unknown provider '{source}'."); return 1; }

// -------------------------------
// 6) Build context
// -------------------------------
var dateParser   = services.GetRequiredService<DateParser>();
var amountParser = services.GetRequiredService<AmountParser>();
var fitidGen     = services.GetRequiredService<FitIdGenerator>();
var subacct      = services.GetRequiredService<SubacctResolver>();
var secResolver  = services.GetRequiredService<SecurityResolver>();
var outPathSvc   = services.GetRequiredService<OutputPathService>();

var startDate    = string.IsNullOrWhiteSpace(startStr) ? (DateTime?)null : dateParser.ParseOrNull(startStr);
var ofxOutPath   = outPathSvc.ResolveOfxPath(csvPath, ofxPath);

// -------------------------------
// 7) Parse and write OFX
// -------------------------------
using var fs = File.OpenRead(csvPath);
var raw = new RawStatement(source, fs, ".csv");

var ctx = new ParserContext {
    AccountId        = acctId,
    Institution      = source,
    StartDateFilter  = startDate,
    CurrencyDefault  = "USD",
    DateParser       = dateParser,
    AmountParser     = amountParser,
    FitIdGenerator   = fitidGen,
    SubacctResolver  = subacct,
    SecurityResolver = secResolver
};

var result  = parser.Parse(raw, ctx);
var ofxText = services.GetRequiredService<IOfxWriter>().WriteInvestmentStatement(result, includeSecurityList: true);

await File.WriteAllTextAsync(ofxOutPath, ofxText);
Console.WriteLine($"✅ OFX written: {ofxOutPath}");
return 0;