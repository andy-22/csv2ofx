namespace CsvToOfx.Core.Services;
public sealed class OutputPathService
{
    public string ResolveOfxPath(string csvPath, string? provided)
    {
        if (!string.IsNullOrWhiteSpace(provided)) return provided!;
        var baseNoExt = Path.Combine(Path.GetDirectoryName(csvPath)!, Path.GetFileNameWithoutExtension(csvPath));
        var ofx = $"{baseNoExt}.ofx";
        int counter = 1;
        while (File.Exists(ofx)) ofx = $"{baseNoExt}_{counter++}.ofx";
        return ofx;
    }
}