using CsvToOfx.Core.Models;

namespace CsvToOfx.Core.Parsing;

public interface IActionResolver
{
    CanonicalAction Resolve(string? actionText);
}

