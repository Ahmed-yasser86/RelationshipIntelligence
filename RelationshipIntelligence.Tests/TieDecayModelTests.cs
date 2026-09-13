using FluentAssertions;
using Servicess;
using System;
using System.Collections.Generic;
using Xunit;

namespace CRUDTests
{
    public class TieDecayModelTests
    {
        private static readonly DateTime Now = new(2026, 9, 11, 12, 0, 0, DateTimeKind.Utc);

        private static List<DateTime> Events(params int[] daysAgo)
        {
            var list = new List<DateTime>();
            foreach (var d in daysAgo)
                list.Add(Now.AddDays(-d));
            return list;
        }

        [Fact]
        public void StrengthAt_NoEvents_IsZero()
        {
            TieDecayModel.StrengthAt(new List<DateTime>(), Now).Should().Be(0);
        }

        [Fact]
        public void StrengthAt_HalfLifeIdentity_HalvesStrength()
        {
            var single = Events(60);
            double atEvent = TieDecayModel.StrengthAt(single, Now.AddDays(-60));
            double afterHalfLife = TieDecayModel.StrengthAt(single, Now);
            afterHalfLife.Should().BeApproximately(atEvent / 2, 1e-9);
        }

        [Fact]
        public void StrengthAt_MonotonicDecay_WithSilence()
        {
            var events = Events(10);
            double early = TieDecayModel.StrengthAt(events, Now);
            double late = TieDecayModel.StrengthAt(events, Now.AddDays(30));
            late.Should().BeLessThan(early);
        }

        [Fact]
        public void StrengthAt_RecentEvent_ExceedsOldEvent()
        {
            TieDecayModel.StrengthAt(Events(1), Now)
                .Should().BeGreaterThan(TieDecayModel.StrengthAt(Events(100), Now));
        }

        [Fact]
        public void StrengthAt_BoostsAreAdditive()
        {
            double two = TieDecayModel.StrengthAt(Events(5, 5), Now);
            double one = TieDecayModel.StrengthAt(Events(5), Now);
            two.Should().BeApproximately(2 * one, 1e-9);
        }

        [Fact]
        public void CadenceReference_FewGaps_ReturnsPrior()
        {
            TieDecayModel.CadenceReference(new List<double> { 10, 12 }, 30).Should().Be(30);
        }

        [Fact]
        public void CadenceReference_ManyGaps_ShrinksTowardPrior()
        {
            double reference = TieDecayModel.CadenceReference(new List<double> { 10, 10, 10, 10 }, 30);
            reference.Should().BeInRange(10, 30);
            reference.Should().BeApproximately((4.0 * 10 + 3 * 30) / 7, 1e-9);
        }

        [Fact]
        public void SilenceDays_NullContact_ReturnsNull()
        {
            TieDecayModel.SilenceDays(null, Now).Should().BeNull();
        }

        [Fact]
        public void SilenceDays_FutureContact_ReturnsZero()
        {
            TieDecayModel.SilenceDays(Now.AddHours(5), Now).Should().Be(0);
        }

        [Fact]
        public void SilenceDays_FloorsFractionalDays_Canonical()
        {
            // 118.9d must show 118 everywhere — floor, never round/ceiling.
            // Off-by-one regression guard: 118.9d must show 118 everywhere.
            TieDecayModel.SilenceDays(Now.AddDays(-118.9), Now).Should().Be(118);
            TieDecayModel.SilenceDays(Now.AddDays(-119.0), Now).Should().Be(119);
            TieDecayModel.SilenceDays(Now.AddDays(-25.9), Now).Should().Be(25);
            TieDecayModel.SilenceDays(Now.AddDays(-6.1), Now).Should().Be(6);
        }

        [Fact]
        public void SilenceDays_NormalizesToUtc()
        {
            var local = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Local);
            var utc = local.ToUniversalTime();
            TieDecayModel.SilenceDays(local, Now)
                .Should().Be(TieDecayModel.SilenceDays(utc, Now));
        }

        [Fact]
        public void SilenceQuantile_NoHistory_ReturnsNull()
        {
            TieDecayModel.SilenceQuantile(new List<double>(), 50).Should().BeNull();
        }

        [Fact]
        public void SilenceQuantile_LongestSilence_IsOne()
        {
            TieDecayModel.SilenceQuantile(new List<double> { 5, 10, 15 }, 20).Should().Be(1.0);
        }

        [Fact]
        public void Urgency_ZeroStrength_IsHundred()
        {
            TieDecayModel.Urgency(0, 2.0).Should().Be(100);
        }

        [Fact]
        public void Urgency_MaxStrength_IsZero()
        {
            TieDecayModel.Urgency(2.0, 2.0).Should().Be(0);
        }

        [Fact]
        public void Ranking_StableAcrossHalfLifeGrid()
        {
            foreach (var halfLife in new[] { 30.0, 60.0, 90.0 })
            {
                double alpha = TieDecayModel.AlphaForHalfLife(halfLife);
                double recent = Math.Exp(-alpha * 5);
                double old = Math.Exp(-alpha * 90);
                recent.Should().BeGreaterThan(old, $"half-life {halfLife}");
            }
        }

        [Fact]
        public void BandFor_Thresholds()
        {
            TieDecayModel.BandFor(0).Should().Be(HealthBand.Healthy);
            TieDecayModel.BandFor(40).Should().Be(HealthBand.Drifting);
            TieDecayModel.BandFor(66).Should().Be(HealthBand.AtRisk);
            TieDecayModel.BandFor(86).Should().Be(HealthBand.Critical);
        }

        [Fact]
        public void PersonaPriors_KeywordMatching()
        {
            PersonaPriors.DaysFor(new[] { "Senior Recruiter" }).Should().Be(14);
            PersonaPriors.DaysFor(new[] { "Client Partner" }).Should().Be(21);
            PersonaPriors.DaysFor(new[] { "Friend" }).Should().Be(30);
            PersonaPriors.DaysFor(null).Should().Be(30);
        }
    }
}
