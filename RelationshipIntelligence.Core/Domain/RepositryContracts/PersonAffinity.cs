using System;
using System.Collections.Generic;

namespace RepositryContracts
{
    public sealed record PersonAffinity(
        Guid PersonId,
        IReadOnlyList<string> CircleNames,
        IReadOnlyList<string> TagNames,
        IReadOnlyList<string> ChannelNames);
}
