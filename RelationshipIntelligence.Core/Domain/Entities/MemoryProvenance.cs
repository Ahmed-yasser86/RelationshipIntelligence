namespace Entities
{
    public enum MemoryProvenance
    {
        User = 0,
        AiSuggested = 1,
        AiConfirmed = 2,
        MeetingDerived = 3,
        /// <summary>Applied from an approved unified-ingestion finding.</summary>
        IngestionDerived = 4
    }
}
