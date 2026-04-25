namespace Csv2Ofx.Gui.Conversion;

internal sealed record ConversionResult(
    string OutputPath,
    int TransactionCount,
    int SecurityCount);
