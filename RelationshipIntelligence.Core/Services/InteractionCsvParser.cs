using ContactsManger.Core.Domain.Entities.EEnums;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Servicess
{
    public static class InteractionCsvParser
    {
        public const int MaxRows = 500;

        public static List<ParsedInteractionRow> Parse(string? csvText)
        {
            if (string.IsNullOrWhiteSpace(csvText))
                throw new ArgumentException("CSV text is empty.");

            var lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > MaxRows + 1)
                throw new ArgumentException($"CSV exceeds the limit of {MaxRows} rows.");

            var rows = new List<ParsedInteractionRow>();
            int start = lines[0].TrimStart().StartsWith("date", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            for (int n = start; n < lines.Length; n++)
            {
                var parts = lines[n].Split(',');
                if (parts.Length < 3)
                    throw new ArgumentException($"Line {n + 1}: expected at least date, type and title.");

                if (!DateTime.TryParse(parts[0].Trim(), CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date))
                    throw new ArgumentException($"Line {n + 1}: invalid date '{parts[0].Trim()}'.");

                if (!Enum.TryParse<EnInteractionType>(parts[1].Trim(), true, out var type))
                    throw new ArgumentException($"Line {n + 1}: invalid type '{parts[1].Trim()}'.");

                if (date.ToUniversalTime() > DateTime.UtcNow.AddDays(1))
                    throw new ArgumentException($"Line {n + 1}: date is in the future.");

                string title = parts[2].Trim();
                if (string.IsNullOrEmpty(title) || title.Length > 100)
                    throw new ArgumentException($"Line {n + 1}: title is required and cannot exceed 100 characters.");

                string? description = parts.Length > 3
                    ? string.Join(",", parts, 3, parts.Length - 3).Trim()
                    : null;
                if (description != null && description.Length > 2000)
                    throw new ArgumentException($"Line {n + 1}: description exceeds 2000 characters.");

                rows.Add(new ParsedInteractionRow(
                    date.ToUniversalTime(), type, title,
                    string.IsNullOrEmpty(description) ? null : description));
            }

            return rows;
        }
    }
}
