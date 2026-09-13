using Entities;

namespace ServiceContracts.DTOs.OutreachDTOs
{
    public class DraftReviewRequest
    {
        public DraftStatus Status { get; set; }

        public string? Subject { get; set; }

        public string? Body { get; set; }
    }
}
