using System.Security.Cryptography;
using System.Text;

namespace CsvToOfx.Core.Services;
public sealed class FitIdGenerator
{
    public string FromSortedRow(IDictionary<string, string?> row)
    {
        var sb = new StringBuilder();
        foreach (var kv in row.OrderBy(k => k.Key, StringComparer.Ordinal))
            sb.Append($"{kv.Key}={kv.Value ?? ""}||");
        using var sha1 = SHA1.Create();
        var digest = sha1.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(digest)[..12];
    }
}