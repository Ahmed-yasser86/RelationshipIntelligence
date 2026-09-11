namespace ServiceContracts.DTOs
{
    public class DigestEntry
    {
        public RelationshipHealthResponse Health { get; set; } = new();
        public string Suggestion { get; set; } = string.Empty;
        public string ActionUrl { get; set; } = string.Empty;
        public string RemindUrl { get; set; } = string.Empty;
    }
}
