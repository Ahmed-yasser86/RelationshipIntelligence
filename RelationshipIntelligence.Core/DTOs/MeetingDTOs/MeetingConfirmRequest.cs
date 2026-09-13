using System;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class MeetingConfirmRequest
    {
        public Guid MeetingId { get; set; }

        public DateTime? ActualOccurredAtUtc { get; set; }
    }
}
