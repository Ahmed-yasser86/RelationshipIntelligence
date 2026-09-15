using FluentAssertions;
using RelationshipIntelligence.AI;
using Xunit;

namespace CRUDTests
{
    /// <summary>
    /// Natural-language schedules map only to supported day intervals;
    /// anything else returns null so Copilot rejects honestly.
    /// </summary>
    public class PreferenceScheduleParserTests
    {
        [Theory]
        [InlineData("Remind me about Mohamed every 10 days.", 10)]
        [InlineData("every 10 days", 10)]
        [InlineData("daily", 1)]
        [InlineData("every day", 1)]
        [InlineData("weekly", 7)]
        [InlineData("I want to stay in touch with Sara twice a month.", 15)]
        [InlineData("twice a month", 15)]
        [InlineData("monthly", 30)]
        [InlineData("every 3 days", 3)]
        [InlineData("every 365 days", 365)]
        public void ParseReminder_SupportedPhrases_MapToDays(string input, int expected)
        {
            PreferenceScheduleParser.ParseReminder(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("every other blue moon")]
        [InlineData("hourly")]
        [InlineData("every 0 days")]
        [InlineData("every 999 days")]
        [InlineData("whenever I feel like it")]
        public void ParseReminder_UnsupportedPhrases_ReturnNull(string input)
        {
            PreferenceScheduleParser.ParseReminder(input).Should().BeNull();
        }
    }
}
