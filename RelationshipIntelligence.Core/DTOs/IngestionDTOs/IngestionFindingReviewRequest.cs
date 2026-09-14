using Entities;
using System;

namespace ServiceContracts.DTOs.IngestionDTOs
{
    public class IngestionFindingReviewRequest
    {
        public Guid FindingId { get; set; }

        public IngestionFindingStatus Status { get; set; }

        public string? Title { get; set; }

        public string? Detail { get; set; }

        public Guid? SubjectPersonId { get; set; }

        public bool SubjectIsNew { get; set; }

        public string? SubjectName { get; set; }

        public string? SubjectEmail { get; set; }

        public DateTime? EventDate { get; set; }

        public string? ReviewNote { get; set; }
    }
}
