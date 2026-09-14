using System;

namespace ServiceContracts.DTOs.IngestionDTOs
{
    public class ResolutionCandidateDto
    {
        public Guid? PersonId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Organization { get; set; }

        public string? Role { get; set; }

        public string? Evidence { get; set; }

        public bool IsNew { get; set; }
    }
}
