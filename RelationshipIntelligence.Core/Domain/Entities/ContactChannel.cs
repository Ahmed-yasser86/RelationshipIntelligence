using ContactsManger.Core.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace Entities
{
    /// <summary>
    /// Explicit join between a <see cref="Person"/> and a <see cref="ConnectionChannel"/>.
    /// Carries the per-contact handle for that channel — e.g. the phone number
    /// behind "Phone", the username behind "Telegram", the URL behind "LinkedIn".
    /// The channel row itself stays a shared directory entry (name only).
    /// </summary>
    public class ContactChannel
    {
        public Guid PersonId { get; set; }

        public Person Person { get; set; } = null!;

        public Guid ConnectionChannelId { get; set; }

        public ConnectionChannel Channel { get; set; } = null!;

        [StringLength(200)]
        public string? Value { get; set; }
    }
}
