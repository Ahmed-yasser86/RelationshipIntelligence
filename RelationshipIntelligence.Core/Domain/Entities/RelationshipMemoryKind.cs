namespace Entities
{
    public enum RelationshipMemoryKind
    {
        Fact = 0,
        RelationshipType = 1,
        Origin = 2,
        SharedProject = 3,
        Topic = 4,
        Commitment = 5,
        Goal = 6,
        Intent = 7,
        Preference = 8,
        Milestone = 9,
        /// <summary>
        /// How the user talks to this person (tone, style, avoid-list).
        /// Style signal for personalization — never a message topic.
        /// </summary>
        CommunicationStyle = 10,
        /// <summary>
        /// Actual previous message the user wrote to this person.
        /// Few-shot example for personalization — never a message topic.
        /// </summary>
        MessageExample = 11
    }
}
