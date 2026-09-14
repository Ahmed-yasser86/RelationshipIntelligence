using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    /// <summary>
    /// Optional user-level defaults. One row per user at most. These sit in
    /// the middle of the precedence chain: per-person explicit preference
    /// wins, then these defaults, then the system default/inference.
    /// Everything stays human-termed (days, strict flag); no model internals.
    /// </summary>
    public class GlobalPreferenceDefaults
    {
        [Key]
        public Guid ApplicationUserId { get; set; }

        /// <summary>Fallback "stay in touch every N days" when a person has no explicit cadence.</summary>
        public int? DefaultCadenceDays { get; set; }

        /// <summary>Fallback reminder strictness for new reminders.</summary>
        public bool DefaultReminderStrict { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}
