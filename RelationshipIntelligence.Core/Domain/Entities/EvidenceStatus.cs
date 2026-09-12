namespace Entities
{
    /// <summary>
    /// Grades how much observed evidence backs a relationship score.
    /// NoHistory means the system has zero contact events for the pair and therefore
    /// no basis for any urgency claim; such rows must never be ranked or banded.
    /// Insufficient means scoring ran on priors more than on history (fewer than 3
    /// observed gaps); the score is an early estimate and must be labeled as such.
    /// Established means at least 3 observed gaps back the score.
    /// </summary>
    public enum EvidenceStatus
    {
        NoHistory = 0,
        Insufficient = 1,
        Established = 2
    }
}
