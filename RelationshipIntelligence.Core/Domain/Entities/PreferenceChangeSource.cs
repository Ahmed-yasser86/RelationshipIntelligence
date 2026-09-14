namespace Entities
{
    /// <summary>
    /// Where a preference change originated. Persisted on every audit row so
    /// any change can answer "who changed what, when, from what, to what,
    /// and through which path".
    /// </summary>
    public enum PreferenceChangeSource
    {
        User = 0,
        Copilot = 1,
        Import = 2,
        Default = 3
    }
}
