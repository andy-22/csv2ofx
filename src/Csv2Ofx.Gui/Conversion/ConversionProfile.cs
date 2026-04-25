namespace Csv2Ofx.Gui.Conversion;

internal sealed record ConversionProfile(
    ConversionKind Kind,
    string DisplayName,
    string ProviderCode,
    string OutputExtension)
{
    public override string ToString() => DisplayName;
}
