namespace Csv2Ofx.Gui.Conversion;

internal static class ConversionProfileCatalog
{
    public static IReadOnlyList<ConversionProfile> All { get; } =
    [
        new ConversionProfile(
            ConversionKind.Investments,
            "Investments",
            "fidelity",
            ".ofx")
    ];
}
