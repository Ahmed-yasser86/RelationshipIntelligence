using System;

namespace Servicess
{
    public sealed record GraphNodeInput(
        Guid PersonId,
        string? CircleName,
        string? TagsJson,
        string? ChannelName);
}
