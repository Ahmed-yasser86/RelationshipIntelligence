using Entities;
using ServiceContracts.DTOs.PreferenceDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IRelationshipPreferenceService
    {
        Task<RelationshipPreferenceDto?> GetAsync(Guid personId);

        Task<RelationshipPreferenceDto> SaveAsync(PreferenceSaveRequest request, PreferenceChangeSource source = PreferenceChangeSource.User);

        Task<RelationshipPreferenceDto> SetReminderAsync(ReminderSetRequest request, PreferenceChangeSource source = PreferenceChangeSource.User);

        Task<RelationshipPreferenceDto> SnoozeAsync(Guid personId, int days);

        Task<RelationshipPreferenceDto> SkipAsync(Guid personId);

        /// <summary>
        /// Marks the reminder done. This records the user's action only — it
        /// never creates an interaction. Relationship state still moves
        /// exclusively through logged interactions in the canonical pipeline.
        /// To complete via a real interaction, log it (PersonDetail "Log
        /// interaction", Copilot, or InteractionService) and the reminder
        /// clears itself on the next due computation.
        /// </summary>
        Task<RelationshipPreferenceDto> CompleteAsync(Guid personId);

        Task<RelationshipPreferenceDto> DisableReminderAsync(Guid personId, PreferenceChangeSource source = PreferenceChangeSource.User);

        /// <summary>
        /// Removes the per-person preference row entirely (cadence, importance,
        /// priority, intent, suggestion flag, reminder). The relationship
        /// reverts to global defaults, then system inference. Audited.
        /// </summary>
        Task RemoveAsync(Guid personId, PreferenceChangeSource source = PreferenceChangeSource.User);

        Task<List<ReminderDueDto>> ListDueAsync();

        Task<string> GetReminderStateAsync(Guid personId);

        Task<List<PreferenceAuditDto>> GetHistoryAsync(Guid personId);

        Task<GlobalDefaultsDto> GetGlobalDefaultsAsync();

        Task<GlobalDefaultsDto> SaveGlobalDefaultsAsync(GlobalDefaultsSaveRequest request, PreferenceChangeSource source = PreferenceChangeSource.User);
    }
}
