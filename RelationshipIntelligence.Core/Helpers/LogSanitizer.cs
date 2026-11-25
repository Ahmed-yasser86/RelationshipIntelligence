using System;

namespace Servicess.Helpers
{
    /// <summary>
    /// Removes characters that could forge or split log entries before a value is
    /// passed to ILogger. Also bounds the length so a single request cannot flood
    /// the log stream.
    /// </summary>
    public static class LogSanitizer
    {
        public const int MaxLength = 200;

        public static string Sanitize(object? value)
        {
            if (value == null)
                return string.Empty;

            var text = value.ToString() ?? string.Empty;
            var chars = text.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (char.IsControl(chars[i]) || chars[i] == '\u2028' || chars[i] == '\u2029')
                    chars[i] = ' ';
            }

            text = System.Text.RegularExpressions.Regex.Replace(new string(chars), @"\s{2,}", " ").Trim();
            return text.Length <= MaxLength ? text : text[..MaxLength];
        }
    }
}
