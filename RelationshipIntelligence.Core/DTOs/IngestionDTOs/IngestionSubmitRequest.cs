using Entities;
using System;

namespace ServiceContracts.DTOs.IngestionDTOs
{
    public class IngestionSubmitRequest
    {
        public IngestionSourceType SourceType { get; set; }

        public string RawText { get; set; } = string.Empty;

        public Guid? SourceMeetingId { get; set; }
    }
}
