namespace Csv2Ofx.Gui.Conversion;

internal sealed record ConversionRequest(
    string CsvPath,
    string OutputFolder,
    string AccountName,
    ConversionProfile Profile);
