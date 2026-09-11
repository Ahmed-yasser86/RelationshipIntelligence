using System.Collections.Generic;
using System.Linq;

namespace Servicess
{
    public static class PersonaPriors
    {
        public const double DefaultDays = 30;

        public static double DaysFor(IEnumerable<string>? roleTexts)
        {
            string text = string.Join(" ", roleTexts ?? Enumerable.Empty<string>()).ToLowerInvariant();
            if (text.Contains("recruit") || text.Contains("hiring"))
                return 14;
            if (text.Contains("client"))
                return 21;
            return DefaultDays;
        }
    }
}
