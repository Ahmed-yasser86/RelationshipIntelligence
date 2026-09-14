using ServiceContracts.DTOs.PreferenceDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IRelationshipPreferenceService
    {
        Task<RelationshipPreferenceDto?> GetAsync(Guid personId);

        Task<RelationshipPreferenceDto> SaveAsync(PreferenceSaveRequest request);

        Task<RelationshipPreferenceDto> SetReminderAsync(ReminderSetRequest request);

        Task<RelationshipPreferenceDto> SnoozeAsync(Guid personId, int days);

        Task<RelationshipPreferenceDto> SkipAsync(Guid personId);

        Task<RelationshipPreferenceDto> CompleteAsync(Guid personId);

        Task<RelationshipPreferenceDto> DisableReminderAsync(Guid personId);

        Task<List<ReminderDueDto>> ListDueAsync();
    }
}
