using CsvToOfx.Core.Models;
using CsvToOfx.Core.Services;
using CsvToOfx.Parsers.Abstractions;
using CsvToOfx.Writers.Ofx;

namespace Csv2Ofx.Gui.Conversion;

internal sealed class InvestmentConversionService
{
    private readonly DateParser _dateParser = new();
    private readonly AmountParser _amountParser = new();
    private readonly FitIdGenerator _fitIdGenerator = new();
    private readonly SubacctResolver _subacctResolver = new();
    private readonly SecurityResolver _securityResolver = new(preferCusip: true);
    private readonly IOfxWriter _ofxWriter = new OfxWriter();

    public InvestmentConversionService()
    {
        ParserRegistry.Initialize(ParserFactory.DiscoverParsers());
    }

    public async Task<ConversionResult> ConvertAsync(ConversionRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var parser = ParserRegistry.Resolve(request.Profile.ProviderCode);
        if (parser is null)
        {
            throw new InvalidOperationException($"No parser is registered for '{request.Profile.ProviderCode}'.");
        }

        Directory.CreateDirectory(request.OutputFolder);

        await using var stream = File.OpenRead(request.CsvPath);
        var raw = new RawStatement(request.Profile.ProviderCode, stream, ".csv");
        var ctx = new ParserContext
        {
            AccountId = request.AccountName.Trim(),
            Institution = request.Profile.ProviderCode,
            CurrencyDefault = "USD",
            DateParser = _dateParser,
            AmountParser = _amountParser,
            FitIdGenerator = _fitIdGenerator,
            SubacctResolver = _subacctResolver,
            SecurityResolver = _securityResolver
        };

        var result = parser.Parse(raw, ctx);
        var outputPath = ResolveOutputPath(request);
        var ofxText = _ofxWriter.WriteInvestmentStatement(result, includeSecurityList: true);

        await File.WriteAllTextAsync(outputPath, ofxText, cancellationToken);
        return new ConversionResult(outputPath, result.Transactions.Count, result.Securities?.Count ?? 0);
    }

    public string ResolveOutputPath(ConversionRequest request)
    {
        var csvName = Path.GetFileNameWithoutExtension(request.CsvPath);
        var baseName = string.IsNullOrWhiteSpace(request.AccountName)
            ? csvName
            : $"{csvName}-{SanitizeFileName(request.AccountName)}";

        var candidate = Path.Combine(request.OutputFolder, $"{baseName}{request.Profile.OutputExtension}");
        var counter = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(request.OutputFolder, $"{baseName}_{counter++}{request.Profile.OutputExtension}");
        }

        return candidate;
    }

    private static void ValidateRequest(ConversionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CsvPath))
        {
            throw new InvalidOperationException("Choose a CSV file first.");
        }

        if (!File.Exists(request.CsvPath))
        {
            throw new FileNotFoundException("The selected CSV file was not found.", request.CsvPath);
        }

        if (string.IsNullOrWhiteSpace(request.OutputFolder))
        {
            throw new InvalidOperationException("Choose an output folder.");
        }

        if (string.IsNullOrWhiteSpace(request.AccountName))
        {
            throw new InvalidOperationException("Enter an account name.");
        }
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Trim().Select(c => invalid.Contains(c) ? '-' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "account" : sanitized;
    }
}
