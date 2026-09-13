using Entities;

namespace ServiceContracts.DTOs.OutreachDTOs
{
    public class BatchSettingsRequest
    {
        public OutreachChannel Channel { get; set; }

        public string? Intent { get; set; }

        public string? GlobalInstruction { get; set; }
    }
}
