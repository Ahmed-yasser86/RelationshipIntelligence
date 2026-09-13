using Entities;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs;
using ServiceContracts.DTOs.CopilotDTOs;
using ServiceContracts.DTOs.MemoryDTOs;
using ServiceContracts.DTOs.OutreachDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Servicess
{
    public class OutreachService : IOutreachService
    {
        private readonly OutreachRepositoryContract _batches;
        private readonly PersonRepositryContract _persons;
        private readonly IRelationshipScoringService _scoring;
        private readonly IInteractionService _interactions;
        private readonly IRelationshipMemoryService _memory;
        private readonly IEventService _events;
        private readonly ICopilotService _copilot;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<OutreachService> _logger;

        public OutreachService(
            OutreachRepositoryContract batches,
            PersonRepositryContract persons,
            IRelationshipScoringService scoring,
            IInteractionService interactions,
            IRelationshipMemoryService memory,
            IEventService events,
            ICopilotService copilot,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            ILogger<OutreachService> logger)
        {
            _batches = batches;
            _persons = persons;
            _scoring = scoring;
            _interactions = interactions;
            _memory = memory;
            _events = events;
            _copilot = copilot;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        private Guid OwnerId()
        {
            var id = _currentUser.UserId;
            if (id == null || id == Guid.Empty)
                throw new UnauthorizedAccessException("Cannot manage outreach without an authenticated user.");
            return id.Value;
        }

        private async Task<OutreachBatch> RequireBatchAsync(Guid ownerId, Guid batchId)
        {
            var batch = await _batches.GetBatchAsync(ownerId, batchId);
            if (batch == null)
                throw new KeyNotFoundException($"No outreach batch found with id '{batchId}'.");
            return batch;
        }

        private async Task<Dictionary<Guid, string?>> NameMapAsync()
        {
            var people = await _persons.GetAllPersons();
            return people.Where(p => p != null).ToDictionary(p => p!.PersonId, p => p!.Name);
        }

        private static OutreachBatchResponse ToResponse(OutreachBatch batch, Dictionary<Guid, string?> names) => new()
        {
            OutreachBatchId = batch.OutreachBatchId,
            Intent = batch.Intent,
            Channel = batch.Channel,
            GlobalInstruction = batch.GlobalInstruction,
            Status = batch.Status,
            CreatedAtUtc = batch.CreatedAtUtc,
            Members = batch.Members.Select(m => new OutreachBatchMemberDto
            {
                OutreachBatchMemberId = m.OutreachBatchMemberId,
                PersonId = m.PersonId,
                PersonName = names.TryGetValue(m.PersonId, out var name) ? name : null,
                Reason = m.Reason,
                ChannelOverride = m.ChannelOverride,
                IntentOverride = m.IntentOverride,
                CustomInstruction = m.CustomInstruction,
                Excluded = m.Excluded,
                SkipFutureSuggestions = m.SkipFutureSuggestions
            }).ToList(),
            Drafts = batch.Drafts.Select(d => new CommunicationDraftDto
            {
                CommunicationDraftId = d.CommunicationDraftId,
                PersonId = d.PersonId,
                PersonName = names.TryGetValue(d.PersonId, out var name) ? name : null,
                Kind = d.Kind,
                Channel = d.Channel,
                Subject = d.Subject,
                Body = d.Body,
                ContextUsed = d.ContextUsed,
                LimitedContext = d.LimitedContext,
                IsAiGenerated = d.IsAiGenerated,
                Status = d.Status
            }).ToList()
        };

        private async Task<OutreachBatchResponse> RespondAsync(OutreachBatch batch)
        {
            return ToResponse(batch, await NameMapAsync());
        }

        public async Task<List<OutreachBatchResponse>> ListAsync()
        {
            var ownerId = OwnerId();
            var batches = await _batches.ListBatchesAsync(ownerId);
            var names = await NameMapAsync();
            return batches.Select(b => ToResponse(b, names)).ToList();
        }

        public async Task<OutreachBatchResponse> GetAsync(Guid batchId)
        {
            var ownerId = OwnerId();
            return await RespondAsync(await RequireBatchAsync(ownerId, batchId));
        }

        public async Task<OutreachBatchResponse> BuildFromSignalsAsync(BuildBatchFromSignalsRequest request)
        {
            using (Operation.Time("Build outreach batch from signals"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var signals = (request.SignalFilters ?? new List<string>())
                    .Where(s => BatchIntentDto.KnownSignals.Contains(s))
                    .Distinct()
                    .ToList();
                if (signals.Count == 0)
                    throw new ArgumentException("At least one known signal filter is required.", nameof(request.SignalFilters));

                var window = Math.Clamp(request.TimeWindowDays <= 0 ? 7 : request.TimeWindowDays, 1, 60);
                var max = Math.Clamp(request.MaxMembers <= 0 ? 12 : request.MaxMembers, 1, 50);
                var intent = string.IsNullOrWhiteSpace(request.Intent) ? "Reconnect" : request.Intent.Trim();

                var members = await ResolveMembersAsync(signals, window, max);
                return await PersistBatchAsync(intent, OutreachChannel.Email, null, members);
            }
        }

        public async Task<OutreachBatchResponse> BuildFromPersonsAsync(BuildBatchFromPersonsRequest request)
        {
            using (Operation.Time("Build outreach batch from persons"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var ownerId = OwnerId();
                var ids = (request.PersonIds ?? new List<Guid>()).Where(id => id != Guid.Empty).Distinct().Take(50).ToList();
                if (ids.Count == 0)
                    throw new ArgumentException("At least one person is required.", nameof(request.PersonIds));

                var queue = await _scoring.GetQueueAsync(200);
                var byId = queue.ToDictionary(q => q.PersonId);
                var members = new List<(Guid PersonId, string Reason)>();
                foreach (var id in ids)
                {
                    var person = await _persons.GetPersonById(id);
                    if (person == null || person.ApplicationUserId != ownerId)
                        throw new KeyNotFoundException($"No person found with id '{id}'.");
                    members.Add((id, byId.TryGetValue(id, out var row) ? QueueReason(row) : "Selected from your network."));
                }

                var intent = string.IsNullOrWhiteSpace(request.Intent) ? "Follow up" : request.Intent.Trim();
                return await PersistBatchAsync(intent, OutreachChannel.Email, null, members);
            }
        }

        public async Task<OutreachBatchResponse> BuildFromIntentAsync(BatchIntentDto intent)
        {
            using (Operation.Time("Build outreach batch from intent"))
            {
                if (intent == null)
                    throw new ArgumentNullException(nameof(intent));
                if (intent.NeedsClarification)
                    throw new ArgumentException(intent.ClarificationPrompt ?? "The request needs clarification.", nameof(intent));

                var signals = (intent.SignalFilters ?? new List<string>())
                    .Where(s => BatchIntentDto.KnownSignals.Contains(s))
                    .Distinct()
                    .ToList();
                if (signals.Count == 0)
                    throw new ArgumentException("No usable signals in the request.", nameof(intent.SignalFilters));

                var channel = ParseChannel(intent.Channel);
                var members = await ResolveMembersAsync(signals, Math.Clamp(intent.TimeWindowDays <= 0 ? 7 : intent.TimeWindowDays, 1, 60), 12);
                var text = string.IsNullOrWhiteSpace(intent.IntentText) ? "Reconnect" : intent.IntentText.Trim();
                return await PersistBatchAsync(text, channel, text, members);
            }
        }

        private static OutreachChannel ParseChannel(string? channel) => channel switch
        {
            "LinkedIn" => OutreachChannel.LinkedIn,
            "Text" => OutreachChannel.Text,
            "CallPrep" => OutreachChannel.CallPrep,
            _ => OutreachChannel.Email
        };

        private static string QueueReason(RelationshipHealthResponse row)
        {
            // Prefer server-canonical SilenceDays; fall back to canonical calc.
            var days = row.SilenceDays ?? TieDecayModel.SilenceDays(row.LastContactAtUtc, DateTime.UtcNow);
            var silence = days == null ? "no contact recorded" : $"{days}d silent";
            var rhythm = row.CadenceReferenceDays == null ? "no measured rhythm" : $"normally ~{Math.Round(row.CadenceReferenceDays.Value)}d";
            return $"{row.Band} — urgency {Math.Round(row.UrgencyScore)}. {rhythm}, now {silence}.";
        }

        private async Task<List<(Guid PersonId, string Reason)>> ResolveMembersAsync(List<string> signals, int window, int max)
        {
            var ownerId = OwnerId();
            var queue = await _scoring.GetQueueAsync(200);
            var ordered = new List<(Guid PersonId, string Reason)>();
            var seen = new HashSet<Guid>();

            void Add(Guid personId, string reason)
            {
                if (seen.Contains(personId) || ordered.Count >= max) return;
                seen.Add(personId);
                ordered.Add((personId, reason));
            }

            var skipped = await SkippedPersonIdsAsync(ownerId);

            foreach (var signal in signals)
            {
                if (signal == "attentionQueue")
                {
                    foreach (var row in queue.Take(max))
                    {
                        if (skipped.Contains(row.PersonId)) continue;
                        Add(row.PersonId, QueueReason(row));
                    }
                }
                else if (signal == "outsideCadence")
                {
                    foreach (var row in queue.Where(q => q.CadenceReferenceDays != null && q.LastContactAtUtc != null))
                    {
                        var silenceExact = (DateTime.UtcNow.ToUniversalTime() - row.LastContactAtUtc!.Value.ToUniversalTime()).TotalDays;
                        if (silenceExact <= row.CadenceReferenceDays!.Value) continue;
                        if (skipped.Contains(row.PersonId)) continue;
                        var silenceDays = row.SilenceDays ?? TieDecayModel.SilenceDays(row.LastContactAtUtc, DateTime.UtcNow);
                        Add(row.PersonId, $"Normally ~{Math.Round(row.CadenceReferenceDays.Value)}d, now {silenceDays}d silent.");
                    }
                }
                else if (signal == "neglected")
                {
                    foreach (var row in queue.Where(q => q.InteractionCount >= 2 && q.CadenceReferenceDays != null && q.LastContactAtUtc != null)
                        .OrderByDescending(q => (DateTime.UtcNow.ToUniversalTime() - q.LastContactAtUtc!.Value.ToUniversalTime()).TotalDays / Math.Max(1, q.CadenceReferenceDays!.Value)))
                    {
                        var ratio = (DateTime.UtcNow.ToUniversalTime() - row.LastContactAtUtc!.Value.ToUniversalTime()).TotalDays / Math.Max(1, row.CadenceReferenceDays!.Value);
                        if (ratio < 2) continue;
                        if (skipped.Contains(row.PersonId)) continue;
                        Add(row.PersonId, $"Neglected: {ratio:F1}x past the usual rhythm ({row.InteractionCount} past interactions).");
                    }
                }
                else if (signal == "recentMeetings")
                {
                    var cutoff = DateTime.UtcNow.AddDays(-window);
                    foreach (var row in queue.Take(100))
                    {
                        if (skipped.Contains(row.PersonId)) continue;
                        var interactions = await _interactions.ListForPersonAsync(row.PersonId);
                        var recent = interactions
                            .Where(i => i.InteractionType == ContactsManger.Core.Domain.Entities.EEnums.EnInteractionType.Meeting
                                && i.TimeOfInteraction >= cutoff)
                            .OrderByDescending(i => i.TimeOfInteraction)
                            .FirstOrDefault();
                        if (recent == null) continue;
                        Add(row.PersonId, $"Met on {recent.TimeOfInteraction:yyyy-MM-dd} ({recent.InteractionTitle}) — follow-up due.");
                    }
                }
                else if (signal == "upcomingEvents")
                {
                    var occurrences = await _events.GetUpcomingAsync(window);
                    foreach (var occurrence in occurrences)
                    {
                        if (skipped.Contains(occurrence.PersonId)) continue;
                        var line = occurrence.InDays == 0 ? "today" : $"in {occurrence.InDays}d";
                        Add(occurrence.PersonId, $"{occurrence.Title} {line}.");
                    }
                }
                else if (signal == "pendingCommitments")
                {
                    var commitments = await ActiveCommitmentsAsync();
                    foreach (var (personId, title, name) in commitments)
                    {
                        if (skipped.Contains(personId)) continue;
                        Add(personId, $"Open commitment for {name}: {title}.");
                    }
                }

                if (ordered.Count >= max) break;
            }

            return ordered;
        }

        private async Task<HashSet<Guid>> SkippedPersonIdsAsync(Guid ownerId)
        {
            var recent = await _batches.ListRecentBatchesAsync(ownerId, DateTime.UtcNow.AddDays(-30));
            return recent
                .SelectMany(b => b.Members)
                .Where(m => m.SkipFutureSuggestions)
                .Select(m => m.PersonId)
                .ToHashSet();
        }

        private async Task<List<(Guid PersonId, string Title, string Name)>> ActiveCommitmentsAsync()
        {
            var people = await _persons.GetAllPersons();
            var result = new List<(Guid, string, string)>();
            foreach (var person in people.Where(p => p != null).Take(200))
            {
                List<MemoryEntryResponse> entries;
                try
                {
                    entries = await _memory.ListForPersonAsync(person!.PersonId);
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
                result.AddRange(entries
                    .Where(e => e.Status == 0 && e.Kind == Entities.RelationshipMemoryKind.Commitment)
                    .Select(e => (person!.PersonId, e.Title, person.Name ?? "contact")));
                if (result.Count >= 30) break;
            }
            return result;
        }

        private async Task<OutreachBatchResponse> PersistBatchAsync(
            string intent, OutreachChannel channel, string? globalInstruction, List<(Guid PersonId, string Reason)> members)
        {
            var ownerId = OwnerId();
            var now = DateTime.UtcNow;
            var batch = new OutreachBatch
            {
                OutreachBatchId = Guid.NewGuid(),
                ApplicationUserId = ownerId,
                Intent = intent.Length > 500 ? intent[..500] : intent,
                Channel = channel,
                GlobalInstruction = string.IsNullOrWhiteSpace(globalInstruction)
                    ? null
                    : globalInstruction.Trim().Length > 1000 ? globalInstruction.Trim()[..1000] : globalInstruction.Trim(),
                Status = BatchStatus.Draft,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Members = members.Select(m => new OutreachBatchMember
                {
                    OutreachBatchMemberId = Guid.NewGuid(),
                    PersonId = m.PersonId,
                    Reason = m.Reason.Length > 500 ? m.Reason[..500] : m.Reason,
                    Excluded = false,
                    SkipFutureSuggestions = false,
                    AddedAtUtc = now
                }).ToList()
            };

            await _batches.AddBatchAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Built outreach batch {BatchId} with {Count} members", batch.OutreachBatchId, batch.Members.Count);
            return await RespondAsync(batch);
        }

        public async Task<OutreachBatchResponse> UpdateSettingsAsync(Guid batchId, BatchSettingsRequest request)
        {
            using (Operation.Time("Update outreach batch settings"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var ownerId = OwnerId();
                var batch = await RequireBatchAsync(ownerId, batchId);
                if (batch.Status != BatchStatus.Draft && batch.Status != BatchStatus.Ready)
                    throw new InvalidOperationException("Settings cannot change after approval.");

                batch.Channel = request.Channel;
                if (request.Intent != null)
                {
                    var intent = request.Intent.Trim();
                    if (intent.Length == 0)
                        throw new ArgumentException("Intent cannot be empty.", nameof(request.Intent));
                    batch.Intent = intent.Length > 500 ? intent[..500] : intent;
                }
                batch.GlobalInstruction = string.IsNullOrWhiteSpace(request.GlobalInstruction)
                    ? null
                    : request.GlobalInstruction.Trim().Length > 1000 ? request.GlobalInstruction.Trim()[..1000] : request.GlobalInstruction.Trim();
                batch.UpdatedAtUtc = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
                return await RespondAsync(batch);
            }
        }

        public async Task<OutreachBatchResponse> UpdateMemberAsync(Guid batchId, Guid memberId, MemberOverrideRequest request)
        {
            using (Operation.Time("Update outreach batch member"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var ownerId = OwnerId();
                var batch = await RequireBatchAsync(ownerId, batchId);
                if (batch.Status != BatchStatus.Draft && batch.Status != BatchStatus.Ready)
                    throw new InvalidOperationException("Members cannot change after approval.");

                var member = batch.Members.FirstOrDefault(m => m.OutreachBatchMemberId == memberId);
                if (member == null)
                    throw new KeyNotFoundException($"No batch member found with id '{memberId}'.");

                if (request.ClearChannelOverride)
                    member.ChannelOverride = null;
                else if (request.ChannelOverride != null)
                    member.ChannelOverride = request.ChannelOverride;

                member.IntentOverride = string.IsNullOrWhiteSpace(request.IntentOverride)
                    ? null
                    : request.IntentOverride.Trim().Length > 500 ? request.IntentOverride.Trim()[..500] : request.IntentOverride.Trim();
                member.CustomInstruction = string.IsNullOrWhiteSpace(request.CustomInstruction)
                    ? null
                    : request.CustomInstruction.Trim().Length > 1000 ? request.CustomInstruction.Trim()[..1000] : request.CustomInstruction.Trim();
                member.Excluded = request.Excluded;
                member.SkipFutureSuggestions = request.SkipFutureSuggestions;
                batch.UpdatedAtUtc = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
                return await RespondAsync(batch);
            }
        }

        public async Task<DraftCommunicationResult> PreviewDraftAsync(Guid personId, OutreachChannel channel, string intent, string? instruction)
        {
            var ownerId = OwnerId();
            var person = await _persons.GetPersonById(personId);
            if (person == null || person.ApplicationUserId != ownerId)
                throw new KeyNotFoundException($"No person found with id '{personId}'.");

            return await GenerateForAsync(
                personId,
                person.Name,
                channel,
                channel == OutreachChannel.CallPrep ? DraftKind.CallPrep : DraftKind.Message,
                string.IsNullOrWhiteSpace(intent) ? "Reconnect" : intent.Trim(),
                instruction);
        }

        private async Task<DraftCommunicationResult> GenerateForAsync(
            Guid personId, string? name, OutreachChannel channel, DraftKind kind, string intent, string? instruction)
        {
            var queue = await _scoring.GetQueueAsync(200);
            var context = await BuildDraftContextAsync(personId, name, queue.ToDictionary(q => q.PersonId));
            var generated = await _copilot.DraftCommunicationAsync(new DraftCommunicationRequest
            {
                Person = context,
                Kind = kind,
                Channel = channel,
                Intent = intent,
                GlobalInstruction = instruction,
                CustomInstruction = instruction
            });
            ValidateGenerated(kind, generated);
            return generated;
        }

        public async Task<OutreachBatchResponse> GenerateDraftsAsync(Guid batchId)
        {
            using (Operation.Time("Generate outreach drafts"))
            {
                var ownerId = OwnerId();
                var batch = await RequireBatchAsync(ownerId, batchId);
                if (batch.Status != BatchStatus.Draft && batch.Status != BatchStatus.Ready)
                    throw new InvalidOperationException("Drafts cannot be generated after approval.");

                foreach (var member in batch.Members.Where(m => !m.Excluded))
                {
                    var person = await _persons.GetPersonById(member.PersonId);
                    if (person == null || person.ApplicationUserId != ownerId)
                        continue;

                    foreach (var stale in batch.Drafts
                        .Where(d => d.PersonId == member.PersonId && (d.Status == DraftStatus.Draft || d.Status == DraftStatus.Rejected || d.Status == DraftStatus.Discarded))
                        .ToList())
                    {
                        stale.Status = DraftStatus.Discarded;
                    }

                    var channel = member.ChannelOverride ?? batch.Channel;
                    var kind = channel == OutreachChannel.CallPrep ? DraftKind.CallPrep : DraftKind.Message;
                    var generated = await GenerateForAsync(
                        member.PersonId,
                        person.Name,
                        channel,
                        kind,
                        string.IsNullOrWhiteSpace(member.IntentOverride) ? batch.Intent : member.IntentOverride!.Trim(),
                        string.IsNullOrWhiteSpace(member.CustomInstruction) ? batch.GlobalInstruction : member.CustomInstruction);

                    var now = DateTime.UtcNow;
                    var draft = new CommunicationDraft
                    {
                        CommunicationDraftId = Guid.NewGuid(),
                        ApplicationUserId = ownerId,
                        OutreachBatchId = batch.OutreachBatchId,
                        PersonId = member.PersonId,
                        Kind = kind,
                        Channel = channel,
                        Subject = channel == OutreachChannel.Email ? CleanNullable(generated.Subject, 200) : null,
                        Body = generated.Body.Trim(),
                        ContextUsed = generated.ContextUsed.Count == 0
                            ? null
                            : string.Join("; ", generated.ContextUsed.Take(8)),
                        LimitedContext = generated.LimitedContext,
                        IsAiGenerated = true,
                        Status = DraftStatus.Draft,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    };
                    await _batches.AddDraftAsync(draft);
                }

                batch.Status = BatchStatus.Ready;
                batch.UpdatedAtUtc = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Generated drafts for batch {BatchId}", batchId);
                return await RequireBatchReloadAsync(ownerId, batchId);
            }
        }

        private static string? CleanNullable(string? value, int max) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length > max ? value.Trim()[..max] : value.Trim();

        private static void ValidateGenerated(DraftKind kind, DraftCommunicationResult generated)
        {
            if (generated == null)
                throw new InvalidOperationException("Draft generation produced no output.");
            if (string.IsNullOrWhiteSpace(generated.Body))
                throw new InvalidOperationException("Draft generation produced an empty body.");
            if (generated.Body.Length > 4000)
                throw new InvalidOperationException("Draft body exceeds 4000 characters.");
            if (generated.ContextUsed.Count == 0 && !generated.LimitedContext)
                throw new InvalidOperationException("Drafts must cite person context or declare limited context.");
            if (kind == DraftKind.CallPrep)
            {
                var lower = generated.Body.ToLowerInvariant();
                if (!lower.Contains("before") || !lower.Contains("during") || !lower.Contains("after"))
                    throw new InvalidOperationException("Call preparation must contain Before, During, and After sections.");
            }
        }

        private async Task<PersonDraftContext> BuildDraftContextAsync(Guid personId, string? name, Dictionary<Guid, RelationshipHealthResponse> states)
        {
            states.TryGetValue(personId, out var state);
            var interactions = await _interactions.ListForPersonAsync(personId);
            var memory = await _memory.ListForPersonAsync(personId);
            var upcoming = await _events.GetUpcomingAsync(21);

            return new PersonDraftContext
            {
                PersonId = personId,
                Name = name ?? "contact",
                Band = state?.Band,
                UrgencyScore = state?.UrgencyScore,
                CadenceLine = state == null
                    ? "No measured rhythm."
                    : $"Band {state.Band}, urgency {Math.Round(state.UrgencyScore)}. " +
                      (state.LastContactAtUtc == null ? "No contact recorded." :
                      $"Last contact {state.LastContactAtUtc.Value:yyyy-MM-dd}" +
                      (state.CadenceReferenceDays == null ? "." : $" vs ~{Math.Round(state.CadenceReferenceDays.Value)}d rhythm.")),
                RecentInteractions = interactions
                    .OrderByDescending(i => i.TimeOfInteraction)
                    .Take(5)
                    .Select(i => $"{i.TimeOfInteraction:yyyy-MM-dd} [{i.InteractionType}] {i.InteractionTitle}")
                    .ToList(),
                MemoryHighlights = memory
                    .Where(m => m.Status == 0
                        && m.Kind != Entities.RelationshipMemoryKind.CommunicationStyle
                        && m.Kind != Entities.RelationshipMemoryKind.MessageExample)
                    .Take(6)
                    .Select(m => $"[{m.Kind}/{m.Provenance}] {m.Title}")
                    .ToList(),
                // Personalization signals: style guidance and examples
                // travel in dedicated fields — never as message topics.
                CommunicationStyle = memory
                    .Where(m => m.Status == 0 && m.Kind == Entities.RelationshipMemoryKind.CommunicationStyle)
                    .Take(4)
                    .Select(m => m.Detail == null ? m.Title : $"{m.Title} — {m.Detail}")
                    .ToList(),
                MessageExamples = memory
                    .Where(m => m.Status == 0 && m.Kind == Entities.RelationshipMemoryKind.MessageExample)
                    .Take(3)
                    .Select(m => m.Detail == null ? m.Title : $"{m.Title} — {m.Detail}")
                    .ToList(),
                StyleNotes = memory
                    .Where(m => m.Status == 0
                        && m.Kind == Entities.RelationshipMemoryKind.Preference
                        && (m.Provenance == Entities.MemoryProvenance.AiConfirmed
                            || m.Provenance == Entities.MemoryProvenance.User)
                        && m.Title.StartsWith("Style:", StringComparison.OrdinalIgnoreCase))
                    .Take(4)
                    .Select(m => m.Detail == null ? m.Title : $"{m.Title} — {m.Detail}")
                    .ToList(),
                UpcomingEvents = upcoming
                    .Where(e => e.PersonId == personId)
                    .Select(e => $"{e.Title} in {e.InDays}d")
                    .ToList(),
                OpenCommitments = memory
                    .Where(m => m.Status == 0 && m.Kind == Entities.RelationshipMemoryKind.Commitment)
                    .Select(m => m.Title)
                    .ToList()
            };
        }

        public async Task<CommunicationDraftDto> ReviewDraftAsync(Guid draftId, DraftReviewRequest request)
        {
            using (Operation.Time("Review communication draft"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var ownerId = OwnerId();
                var draft = await FindDraftAsync(ownerId, draftId);

                if (request.Status != DraftStatus.Edited
                    && request.Status != DraftStatus.Approved
                    && request.Status != DraftStatus.Rejected
                    && request.Status != DraftStatus.Discarded)
                    throw new ArgumentException("Invalid draft status.", nameof(request.Status));

                if (request.Body != null)
                {
                    var body = request.Body.Trim();
                    if (body.Length == 0)
                        throw new ArgumentException("Draft body cannot be empty.", nameof(request.Body));
                    if (body.Length > 4000)
                        throw new ArgumentException("Draft body cannot exceed 4000 characters.", nameof(request.Body));
                    if (body != draft.Body)
                    {
                        // Preserve history — stash the AI original once, never
                        // overwrite it. The edit becomes a personalization signal.
                        var aiBody = draft.OriginalBody ?? (draft.IsAiGenerated ? draft.Body : null);
                        if (aiBody != null && draft.OriginalBody == null)
                            draft.OriginalBody = aiBody;
                        draft.Body = body;
                        draft.IsAiGenerated = false;
                        if (aiBody != null && !string.Equals(Normalize(aiBody), Normalize(body), StringComparison.Ordinal))
                            await SuggestStyleFromEditAsync(draft.PersonId, draft.Channel, aiBody, body);
                    }
                }
                if (request.Subject != null)
                    draft.Subject = CleanNullable(request.Subject, 200);
                draft.Status = request.Status;
                draft.UpdatedAtUtc = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                var names = await NameMapAsync();
                return ToDraftDto(draft, names);
            }
        }

        /// <summary>
        /// Derives conservative style signals from a user edit and stores
        /// each as an AI-SUGGESTED preference — never a silent global rule.
        /// The user accepts/rejects/edits them like any other suggestion, so a
        /// one-off edit never becomes a permanent preference unreviewed.
        /// </summary>
        private async Task SuggestStyleFromEditAsync(Guid personId, OutreachChannel channel, string aiBody, string userBody)
        {
            var signals = new List<string>();
            var ai = aiBody.Trim();
            var user = userBody.Trim();
            if (ai.Length > 0)
            {
                double ratio = (double)user.Length / ai.Length;
                if (ratio < 0.6)
                    signals.Add("Style: prefers shorter messages than drafted");
                else if (ratio > 1.6)
                    signals.Add("Style: adds more detail than drafted");
            }
            if (StartsWithGreeting(ai) && !StartsWithGreeting(user))
                signals.Add("Style: skips formal greetings");
            if (HasSignOff(ai) && !HasSignOff(user))
                signals.Add("Style: no formal sign-off");

            foreach (var signal in signals.Distinct().Take(3))
            {
                try
                {
                    await _memory.SuggestEntryAsync(
                        personId,
                        Entities.RelationshipMemoryKind.Preference,
                        signal,
                        $"Observed when editing a {channel} draft. Accept to apply to future drafts for this person.");
                }
                catch (InvalidOperationException)
                {
                    // Same signal already suggested or recorded — nothing to learn.
                }
            }
        }

        private static bool StartsWithGreeting(string text)
        {
            var first = text.TrimStart().Split('\n')[0].Trim();
            return first.StartsWith("Hi ", StringComparison.OrdinalIgnoreCase)
                || first.StartsWith("Hi,", StringComparison.OrdinalIgnoreCase)
                || first.StartsWith("Hey ", StringComparison.OrdinalIgnoreCase)
                || first.StartsWith("Hello ", StringComparison.OrdinalIgnoreCase)
                || first.StartsWith("Dear ", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasSignOff(string text)
        {
            var lower = text.ToLowerInvariant();
            return lower.Contains("\nbest,") || lower.Contains("\nbest ")
                || lower.Contains("\nthanks") || lower.Contains("\nregards")
                || lower.Contains("\ncheers") || lower.Contains("\nwarmly");
        }

        private static string Normalize(string value) =>
            new string((value ?? string.Empty).Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        public async Task<CommunicationDraftDto> RegenerateDraftAsync(Guid draftId, string? customInstruction)
        {
            using (Operation.Time("Regenerate communication draft"))
            {
                var ownerId = OwnerId();
                var draft = await FindDraftAsync(ownerId, draftId);
                var batch = await RequireBatchAsync(ownerId, draft.OutreachBatchId);
                if (batch.Status != BatchStatus.Draft && batch.Status != BatchStatus.Ready)
                    throw new InvalidOperationException("Drafts cannot change after approval.");

                var member = batch.Members.FirstOrDefault(m => m.PersonId == draft.PersonId && !m.Excluded);
                if (member == null)
                    throw new InvalidOperationException("The person is no longer in the batch.");

                var person = await _persons.GetPersonById(draft.PersonId);
                if (person == null || person.ApplicationUserId != ownerId)
                    throw new KeyNotFoundException($"No person found with id '{draft.PersonId}'.");

                var queue = await _scoring.GetQueueAsync(200);
                var generated = await GenerateForAsync(
                    draft.PersonId,
                    person.Name,
                    member.ChannelOverride ?? batch.Channel,
                    draft.Kind,
                    string.IsNullOrWhiteSpace(member.IntentOverride) ? batch.Intent : member.IntentOverride!.Trim(),
                    string.IsNullOrWhiteSpace(customInstruction) ? (string.IsNullOrWhiteSpace(member.CustomInstruction) ? batch.GlobalInstruction : member.CustomInstruction) : customInstruction!.Trim());

                draft.Body = generated.Body.Trim();
                draft.Subject = draft.Channel == OutreachChannel.Email ? CleanNullable(generated.Subject, 200) : null;
                draft.ContextUsed = generated.ContextUsed.Count == 0 ? null : string.Join("; ", generated.ContextUsed.Take(8));
                draft.LimitedContext = generated.LimitedContext;
                draft.IsAiGenerated = true;
                draft.Status = DraftStatus.Draft;
                draft.UpdatedAtUtc = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                var names = await NameMapAsync();
                return ToDraftDto(draft, names);
            }
        }

        public async Task<OutreachBatchResponse> ApproveAsync(Guid batchId, List<Guid>? draftIds)
        {
            using (Operation.Time("Approve outreach batch"))
            {
                var ownerId = OwnerId();
                var batch = await RequireBatchAsync(ownerId, batchId);
                if (batch.Status != BatchStatus.Draft && batch.Status != BatchStatus.Ready)
                    throw new InvalidOperationException("Batch cannot be approved in its current state.");

                var targets = batch.Drafts
                    .Where(d => draftIds == null || draftIds.Count == 0 || draftIds.Contains(d.CommunicationDraftId))
                    .Where(d => d.Status == DraftStatus.Draft || d.Status == DraftStatus.Edited)
                    .ToList();
                if (targets.Count == 0)
                    throw new InvalidOperationException("No reviewable drafts to approve.");

                foreach (var draft in targets)
                {
                    draft.Status = DraftStatus.Approved;
                    draft.UpdatedAtUtc = DateTime.UtcNow;
                }
                batch.Status = BatchStatus.Approved;
                batch.UpdatedAtUtc = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Approved {Count} drafts in batch {BatchId}. Approval is not contact: no interaction was recorded.", targets.Count, batchId);
                return await RequireBatchReloadAsync(ownerId, batchId);
            }
        }

        public async Task DiscardBatchAsync(Guid batchId)
        {
            using (Operation.Time("Discard outreach batch"))
            {
                var ownerId = OwnerId();
                var batch = await RequireBatchAsync(ownerId, batchId);
                batch.Status = BatchStatus.Discarded;
                batch.UpdatedAtUtc = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private async Task<CommunicationDraft> FindDraftAsync(Guid ownerId, Guid draftId)
        {
            var batches = await _batches.ListRecentBatchesAsync(ownerId, DateTime.UtcNow.AddYears(-5));
            foreach (var batch in batches)
            {
                var draft = batch.Drafts.FirstOrDefault(d => d.CommunicationDraftId == draftId);
                if (draft != null)
                    return draft;
            }
            throw new KeyNotFoundException($"No draft found with id '{draftId}'.");
        }

        private async Task<OutreachBatchResponse> RequireBatchReloadAsync(Guid ownerId, Guid batchId)
        {
            return await RespondAsync(await RequireBatchAsync(ownerId, batchId));
        }

        private static CommunicationDraftDto ToDraftDto(CommunicationDraft draft, Dictionary<Guid, string?> names) => new()
        {
            CommunicationDraftId = draft.CommunicationDraftId,
            PersonId = draft.PersonId,
            PersonName = names.TryGetValue(draft.PersonId, out var name) ? name : null,
            Kind = draft.Kind,
            Channel = draft.Channel,
            Subject = draft.Subject,
            Body = draft.Body,
            OriginalBody = draft.OriginalBody,
            ContextUsed = draft.ContextUsed,
            LimitedContext = draft.LimitedContext,
            IsAiGenerated = draft.IsAiGenerated,
            Status = draft.Status
        };
    }
}
