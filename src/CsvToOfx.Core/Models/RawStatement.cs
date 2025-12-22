namespace CsvToOfx.Core.Models;
public sealed record RawStatement(string SourceName, Stream Content, string MimeOrExtension);