using Entities;
using System;

namespace ServiceContracts.DTOs.MemoryDTOs
{
    public class MemoryEntryResponse
    {
        public Guid MemoryEntryId { get; set; }

        public Guid PersonId { get; set; }

        public RelationshipMemoryKind Kind { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Detail { get; set; }

        public MemoryEntryStatus Status { get; set; }

        public MemoryProvenance Provenance { get; set; }

        public Guid? SourceMeetingId { get; set; }

        public Guid? SourceFindingId { get; set; }

        public string? SourceExcerpt { get; set; }

        public bool SourceMeetingDeleted { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public static MemoryEntryResponse FromEntry(RelationshipMemoryEntry entry) => new()
        {
            MemoryEntryId = entry.MemoryEntryId,
            PersonId = entry.PersonId,
            Kind = entry.Kind,
            Title = entry.Title,
            Detail = entry.Detail,
            Status = entry.Status,
            Provenance = entry.Provenance,
            SourceMeetingId = entry.SourceMeetingId,
            SourceFindingId = entry.SourceFindingId,
            SourceExcerpt = entry.SourceExcerpt,
            SourceMeetingDeleted = entry.SourceMeetingDeleted,
            CreatedAtUtc = entry.CreatedAtUtc,
            UpdatedAtUtc = entry.UpdatedAtUtc
        };
    }
}
