using System;
using System.Collections.Generic;
using System.Linq;

namespace Servicess
{
    public static class TieDecayModel
    {
        public const double DefaultHalfLifeDays = 60;
        public const double EqualBoost = 1.0;

        public static double AlphaForHalfLife(double halfLifeDays)
        {
            return Math.Log(2) / halfLifeDays;
        }

        public static double StrengthAt(
            IReadOnlyList<DateTime> eventTimesUtc,
            DateTime nowUtc,
            double? halfLifeDays = null)
        {
            double alpha = AlphaForHalfLife(halfLifeDays ?? DefaultHalfLifeDays);

            double strength = 0;
            foreach (var time in eventTimesUtc)
            {
                double ageDays = Math.Max(0, (nowUtc - time.ToUniversalTime()).TotalDays);
                strength += EqualBoost * Math.Exp(-alpha * ageDays);
            }
            return strength;
        }

        public static double CadenceReference(IReadOnlyList<double> gapsDays, double priorDays)
        {
            if (gapsDays == null || gapsDays.Count < 3)
                return priorDays;

            var sorted = gapsDays.OrderBy(g => g).ToList();
            double median = sorted.Count % 2 == 1
                ? sorted[sorted.Count / 2]
                : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2;

            double reference = (sorted.Count * median + 3 * priorDays) / (sorted.Count + 3);
            return Math.Min(180, Math.Max(3, reference));
        }

        /// <summary>
        /// Canonical current-silence definition (§11). Single source of truth for
        /// "days since last contact": floor of elapsed UTC days, null when there is
        /// no contact, 0 for future timestamps. All backend and frontend surfaces
        /// must use this (frontend mirrors it in lib/format.ts daysSince).
        /// Using floor (not round, not ceiling) and UTC on both ends eliminates the
        /// 118d-vs-119d class of off-by-one disagreements.
        /// </summary>
        public static int? SilenceDays(DateTime? lastContactUtc, DateTime nowUtc)
        {
            if (lastContactUtc == null)
                return null;
            var last = lastContactUtc.Value.ToUniversalTime();
            var now = nowUtc.ToUniversalTime();
            double totalDays = (now - last).TotalDays;
            if (totalDays <= 0)
                return 0;
            return (int)Math.Floor(totalDays);
        }

        public static double? SilenceQuantile(IReadOnlyList<double> gapsDays, double currentSilenceDays)
        {
            if (gapsDays == null || gapsDays.Count == 0)
                return null;

            return (double)gapsDays.Count(g => g <= currentSilenceDays) / gapsDays.Count;
        }

        public static double Urgency(double strength, double maxUserStrength)
        {
            double reference = maxUserStrength > 0 ? maxUserStrength : 1.0;
            double urgency = 100 * (1 - strength / reference);
            return Math.Min(100, Math.Max(0, urgency));
        }

        public static HealthBand BandFor(double urgency)
        {
            if (urgency > 85) return HealthBand.Critical;
            if (urgency > 65) return HealthBand.AtRisk;
            if (urgency >= 40) return HealthBand.Drifting;
            return HealthBand.Healthy;
        }
    }
}
