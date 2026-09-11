using System;

namespace Servicess
{
    public sealed record GraphEdge(Guid From, Guid To, string Reason, double Weight);
}
