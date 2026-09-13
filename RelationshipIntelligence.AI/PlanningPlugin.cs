using Microsoft.SemanticKernel;
using ServiceContracts;
using ServiceContracts.DTOs.CopilotDTOs;
using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace RelationshipIntelligence.AI
{
    /// <summary>
    /// Preparation capabilities: briefs, batches, drafts, plans, and memory
    /// suggestions. Everything produced here is a reviewable preview or a
    /// pending suggestion — nothing here contacts anyone or rewrites memory.
    /// </summary>
    public sealed class PlanningPlugin
    {
        private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

        private readonly IMeetingService _meetings;
        private readonly IOutreachService _outreach;
        private readonly ICopilotService _copilot;
        private readonly IRelationshipMemoryService _memory;

        public PlanningPlugin(
            IMeetingService meetings,
            IOutreachService outreach,
            ICopilotService copilot,
            IRelationshipMemoryService memory)
        {
            _meetings = meetings;
            _outreach = outreach;
            _copilot = copilot;
            _memory = memory;
        }

        [KernelFunction, Description("Generate a preparation brief for a meeting from mapped participants and stored context. Persists the brief as a reviewable draft artifact.")]
        public async Task<string> PrepareMeetingBriefAsync(
            [Description("The meeting id (Guid).")] string meetingId)
        {
            if (!Guid.TryParse(meetingId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid meeting id." }, Json);
            try
            {
                var meeting = await _meetings.GenerateBriefAsync(id);
                return JsonSerializer.Serialize(new { meeting.MeetingId, meeting.Title, briefed = meeting.Brief != null }, Json);
            }
            catch (KeyNotFoundException)
            {
                return JsonSerializer.Serialize(new { error = "No such meeting." }, Json);
            }
            catch (InvalidOperationException ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Prepare a call brief for one person: who they are, state, topics, commitments, events, questions. Returns a preview, persists nothing.")]
        public async Task<string> PrepareCallBriefAsync(
            [Description("The person's id (Guid).")] string personId,
            [Description("What the call is about.")] string intent = "Check in")
        {
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            try
            {
                var batch = await _outreach.BuildFromPersonsAsync(new ServiceContracts.DTOs.OutreachDTOs.BuildBatchFromPersonsRequest
                {
                    PersonIds = new System.Collections.Generic.List<Guid> { id },
                    Intent = string.IsNullOrWhiteSpace(intent) ? "Prepare call" : intent.Trim()
                });
                await _outreach.UpdateSettingsAsync(batch.OutreachBatchId, new ServiceContracts.DTOs.OutreachDTOs.BatchSettingsRequest
                {
                    Channel = Entities.OutreachChannel.CallPrep,
                    Intent = string.IsNullOrWhiteSpace(intent) ? "Prepare call" : intent.Trim()
                });
                var withDrafts = await _outreach.GenerateDraftsAsync(batch.OutreachBatchId);
                var draft = withDrafts.Drafts.Find(d => d.PersonId == id);
                if (draft == null)
                    return JsonSerializer.Serialize(new { error = "Could not prepare the brief." }, Json);
                return JsonSerializer.Serialize(new { draft.CommunicationDraftId, draft.Body, batchId = withDrafts.OutreachBatchId }, Json);
            }
            catch (KeyNotFoundException)
            {
                return JsonSerializer.Serialize(new { error = "No such contact." }, Json);
            }
        }

        [KernelFunction, Description("Build an outreach batch preview from signal filters: members with reasons. Creates a Draft batch for review; drafts nothing yet.")]
        public async Task<string> PrepareOutreachBatchAsync(
            [Description("Comma-separated signal filters: outsideCadence, recentMeetings, neglected, attentionQueue, upcomingEvents, pendingCommitments.")] string signals,
            [Description("Time window in days.")] int timeWindowDays = 7)
        {
            try
            {
                var batch = await _outreach.BuildFromSignalsAsync(new ServiceContracts.DTOs.OutreachDTOs.BuildBatchFromSignalsRequest
                {
                    SignalFilters = new System.Collections.Generic.List<string>(
                        (signals ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
                    TimeWindowDays = Math.Clamp(timeWindowDays <= 0 ? 7 : timeWindowDays, 1, 60),
                    Intent = "Reconnect"
                });
                return JsonSerializer.Serialize(new
                {
                    batch.OutreachBatchId,
                    members = batch.Members.ConvertAll(m => new { m.PersonId, m.PersonName, m.Reason })
                }, Json);
            }
            catch (ArgumentException ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Draft one communication for one person without persisting it. Returns the preview with grounding. Use the outreach batch flow when the user wants to review, edit, and approve.")]
        public async Task<string> DraftCommunicationAsync(
            [Description("The person's id (Guid).")] string personId,
            [Description("Channel: Email, LinkedIn, Text, or CallPrep.")] string channel = "Email",
            [Description("Communication intent, e.g. Reconnect.")] string intent = "Reconnect",
            [Description("Global instruction applied to the wording.")] string instruction = "")
        {
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            if (!Enum.TryParse<Entities.OutreachChannel>(channel, true, out var parsedChannel))
                return JsonSerializer.Serialize(new { error = "Channel must be Email, LinkedIn, Text, or CallPrep." }, Json);
            try
            {
                var preview = await _outreach.PreviewDraftAsync(
                    id,
                    parsedChannel,
                    string.IsNullOrWhiteSpace(intent) ? "Reconnect" : intent.Trim(),
                    string.IsNullOrWhiteSpace(instruction) ? null : instruction.Trim());
                return JsonSerializer.Serialize(new
                {
                    preview.Subject,
                    preview.Body,
                    contextUsed = preview.ContextUsed,
                    limitedContext = preview.LimitedContext
                }, Json);
            }
            catch (KeyNotFoundException)
            {
                return JsonSerializer.Serialize(new { error = "No such contact." }, Json);
            }
            catch (InvalidOperationException ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message }, Json);
            }
        }

        [KernelFunction, Description("Suggest a relationship plan for one person from intent, history, cadence, events, and goals. Advisory only.")]
        public async Task<string> SuggestRelationshipPlanAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            try
            {
                var plan = await _copilot.SuggestPlanAsync(id, null);
                return JsonSerializer.Serialize(plan, Json);
            }
            catch (KeyNotFoundException)
            {
                return JsonSerializer.Serialize(new { error = "No such contact." }, Json);
            }
        }

        [KernelFunction, Description("Propose memory updates for one person based on gaps in recorded context. Each proposal is stored as a pending suggestion the user must accept; nothing becomes memory silently.")]
        public async Task<string> SuggestMemoryUpdatesAsync(
            [Description("The person's id (Guid).")] string personId)
        {
            if (!Guid.TryParse(personId, out var id))
                return JsonSerializer.Serialize(new { error = "Invalid person id." }, Json);
            try
            {
                var proposed = await _memory.ProposeGapsAsync(id);
                if (proposed.Count == 0)
                    return JsonSerializer.Serialize(new { note = "No gaps found — intent, goals, and topics are already recorded." }, Json);
                return JsonSerializer.Serialize(proposed.Select(p => new
                {
                    p.MemoryEntryId,
                    p.Kind,
                    p.Title,
                    p.Detail,
                    status = "pending — needs user accept"
                }), Json);
            }
            catch (KeyNotFoundException)
            {
                return JsonSerializer.Serialize(new { error = "No such contact." }, Json);
            }
            catch (InvalidOperationException ex)
            {
                return JsonSerializer.Serialize(new { note = ex.Message }, Json);
            }
        }
    }
}
