using System;
using System.Collections.Generic;

namespace Servicess
{
    public sealed record GraphAnalysis(
        IReadOnlyList<GraphEdge> Edges,
        IReadOnlyDictionary<Guid, int> Degrees,
        IReadOnlySet<Guid> Bridges,
        IReadOnlyList<IReadOnlyList<Guid>> Clusters);
}
