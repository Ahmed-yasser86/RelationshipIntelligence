using Entities;

namespace ServiceContracts.DTOs.MemoryDTOs
{
    public class MemoryEntryCreateRequest
    {
        public Guid PersonId { get; set; }

        public RelationshipMemoryKind Kind { get; set; }

        public string? Title { get; set; }

        public string? Detail { get; set; }
    }
}
