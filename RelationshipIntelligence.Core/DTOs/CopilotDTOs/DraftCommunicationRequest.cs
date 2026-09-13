using Entities;

namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class DraftCommunicationRequest
    {
        public PersonDraftContext Person { get; set; } = new();

        public DraftKind Kind { get; set; } = DraftKind.Message;

        public OutreachChannel Channel { get; set; } = OutreachChannel.Email;

        public string Intent { get; set; } = "Reconnect";

        public string? GlobalInstruction { get; set; }

        public string? CustomInstruction { get; set; }
    }
}
