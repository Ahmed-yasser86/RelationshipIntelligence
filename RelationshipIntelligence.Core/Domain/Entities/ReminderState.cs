namespace Entities
{
    /// <summary>
    /// Canonical reminder state machine, derived from the persisted fields on
    /// RelationshipPreference — never stored separately, so the state can
    /// never drift from the data. Snoozed and skipped are time-bound pauses,
    /// not terminal states; completion is recorded, not assumed.
    /// </summary>
    public enum ReminderState
    {
        Disabled = 0,
        Idle = 1,
        Due = 2,
        Snoozed = 3,
        Skipped = 4,
        Completed = 5
    }
}
