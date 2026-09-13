using Entities;
using System;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class FindingReviewRequest
    {
        public Guid MeetingFindingId { get; set; }

        public FindingStatus Status { get; set; }

        public Guid? MappedPersonId { get; set; }

        public string? Title { get; set; }

        public string? Detail { get; set; }

        public string? ResolutionNote { get; set; }
    }
}
