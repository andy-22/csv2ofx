namespace CsvToOfx.Core.Services
{
    public sealed class DateParser
    {
        // Accept both padded and non-padded month/day; and 2- or 4-digit years.
        private static readonly string[] Formats =
        {
            "yyyy-MM-dd",     // 2025-12-04
            "MM/dd/yyyy",     // 12/04/2025
            "MM-dd-yyyy",     // 12-04-2025
            "MM/dd/yy",       // 12/04/25
            "M/d/yy",         // 12/4/25   <-- your CSV case
            "M/d/yyyy",       // 12/4/2025
            "MM/d/yy",        // 12/4/25
            "M/dd/yy",        // 12/04/25
            "MM/d/yyyy",      // 12/4/2025
            "M/dd/yyyy"       // 12/04/2025
        };

        public DateTime? ParseOrNull(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            foreach (var fmt in Formats)
            {
                if (DateTime.TryParseExact(s, fmt, null,
                        System.Globalization.DateTimeStyles.None, out var dt))
                    return dt.Date;
            }
            // Optional: as a last resort, try a culture-aware parse (US-style M/d/y)
            if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt2))
                return dt2.Date;

            throw new FormatException($"Date string '{s}' is not recognized.");
        }
    }
}