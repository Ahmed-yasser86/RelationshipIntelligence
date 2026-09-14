using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    /// <summary>
    /// One auditable preference change. Rows are append-only: the service
    /// writes a row on every mutation (including removal) and never updates
    /// or deletes them. Scoped to the owning user like everything else.
    /// </summary>
    public class PreferenceAuditEntry
    {
        [Key]
        public Guid PreferenceAuditEntryId { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

        [Required]
        public Guid PersonId { get; set; }

        public Person? Person { get; set; }

        /// <summary>Which field changed, e.g. DesiredCadenceDays, ReminderIntervalDays, ReminderEnabled.</summary>
        [Required]
        [StringLength(60)]
        public string Field { get; set; } = string.Empty;

        /// <summary>Previous value rendered in human terms, null when previously unset.</summary>
        [StringLength(200)]
        public string? PreviousValue { get; set; }

        /// <summary>New value rendered in human terms, null when cleared/removed.</summary>
        [StringLength(200)]
        public string? NewValue { get; set; }

        [Required]
        public PreferenceChangeSource Source { get; set; }

        [Required]
        public Guid ChangedById { get; set; }

        public DateTime ChangedAtUtc { get; set; }
    }
}
