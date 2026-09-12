using System;
using System.Collections.Generic;

namespace RepositryContracts
{
    public sealed record PersonAffinity(
        Guid PersonId,
        string? Name,
        IReadOnlyList<string> CircleNames,
        IReadOnlyList<string> TagNames,
        IReadOnlyList<string> ChannelNames,
        IReadOnlyList<string> SystemTagNames,
        DateTime? LastInteractionAtUtc);
}
