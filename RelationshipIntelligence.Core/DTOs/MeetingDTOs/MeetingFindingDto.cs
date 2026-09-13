using Entities;
using System;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class MeetingFindingDto
    {
        public Guid MeetingFindingId { get; set; }

        public Guid? MappedPersonId { get; set; }

        public string? MappedPersonName { get; set; }

        public FindingKind Kind { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Detail { get; set; }

        public FindingStatus Status { get; set; }

        public string? SourceExcerpt { get; set; }

        public Guid? AcceptedAsEntryId { get; set; }
    }
}
