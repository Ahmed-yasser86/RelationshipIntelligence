using FluentAssertions;
using RelationshipIntelligence.AI;
using Xunit;

namespace CRUDTests
{
    /// <summary>
    /// Typo tolerance for Copilot name resolution: near-matches suggest,
    /// never auto-attach. Unrelated names must stay near zero so the agent
    /// does not offer "Omar" for "Sara".
    /// </summary>
    public class PersonNameExtractorTests
    {
        [Theory]
        [InlineData("Dina Smair", "Dina Samir")]
        [InlineData("Mohamed Farok", "Mohamed Farouk")]
        [InlineData("dina samir", "Dina Samir")]
        public void Similarity_CloseTypo_ScoresHigh(string typed, string actual)
        {
            PersonNameExtractor.Similarity(typed, actual).Should().BeGreaterThanOrEqualTo(0.55);
        }

        [Theory]
        [InlineData("Omar", "Sara")]
        [InlineData("Mohamed", "Dina")]
        [InlineData("Proceedit", "Dina Samir")]
        [InlineData("", "Dina Samir")]
        public void Similarity_UnrelatedNames_ScoresLow(string a, string b)
        {
            PersonNameExtractor.Similarity(a, b).Should().BeLessThan(0.55);
        }

        [Fact]
        public void Similarity_Identical_IsOne()
        {
            PersonNameExtractor.Similarity("Dina Samir", "Dina Samir").Should().Be(1);
        }
    }
}
