using ContactsManger.Core.Domain.Entities.EEnums;
using System;

namespace Servicess
{
    public sealed record ParsedInteractionRow(
        DateTime TimeOfInteraction,
        EnInteractionType InteractionType,
        string InteractionTitle,
        string? InteractionDescription);
}
