using Entities;

namespace ServiceContracts.DTOs.MemoryDTOs
{
    public class MemoryEntryUpdateRequest
    {
        public Guid MemoryEntryId { get; set; }

        public RelationshipMemoryKind Kind { get; set; }

        public string? Title { get; set; }

        public string? Detail { get; set; }

        public MemoryEntryStatus Status { get; set; }

        public string? CorrectionNote { get; set; }
    }
}
