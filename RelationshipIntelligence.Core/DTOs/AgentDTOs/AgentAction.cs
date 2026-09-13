namespace ServiceContracts.DTOs.AgentDTOs
{
    public class AgentAction
    {
        public string Kind { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string? Payload { get; set; }
    }
}
