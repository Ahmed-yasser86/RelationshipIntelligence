namespace ServiceContracts.DTOs.CopilotDTOs
{
    public class ChatTurnDto
    {
        public string Role { get; set; } = "user";

        public string Text { get; set; } = string.Empty;
    }
}
