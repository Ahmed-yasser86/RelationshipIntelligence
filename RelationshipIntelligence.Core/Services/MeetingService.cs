using ContactsManger.Core.Domain.Entities.EEnums;
using Entities;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs;
using ServiceContracts.DTOs.MeetingDTOs;
using ServiceContracts.DTOs.MemoryDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Servicess
{
    public class MeetingService : IMeetingService
    {
        private const int MaxTranscriptChars = 50000;

        private readonly MeetingRepositoryContract _meetings;
        private readonly RelationshipMemoryRepositoryContract _memoryEntries;
        private readonly PersonRepositryContract _persons;
        private readonly IInteractionService _interactions;
        private readonly IRelationshipMemoryService _memory;
        private readonly IEventService _events;
        private readonly IRelationshipScoringService _scoring;
        private readonly IMeetingExtractor _extractor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<MeetingService> _logger;

        public MeetingService(
            MeetingRepositoryContract meetings,
            RelationshipMemoryRepositoryContract memoryEntries,
            PersonRepositryContract persons,
            IInteractionService interactions,
            IRelationshipMemoryService memory,
            IEventService events,
            IRelationshipScoringService scoring,
            IMeetingExtractor extractor,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            ILogger<MeetingService> logger)
        {
            _meetings = meetings;
            _memoryEntries = memoryEntries;
            _persons = persons;
            _interactions = interactions;
            _memory = memory;
            _events = events;
            _scoring = scoring;
            _extractor = extractor;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        private Guid OwnerId()
        {
            var id = _currentUser.UserId;
            if (id == null || id == Guid.Empty)
                throw new UnauthorizedAccessException("Cannot manage meetings without an authenticated user.");
            return id.Value;
        }

        private async Task<Meeting> RequireMeetingAsync(Guid ownerId, Guid meetingId)
        {
            var meeting = await _meetings.GetAsync(ownerId, meetingId);
            if (meeting == null)
                throw new KeyNotFoundException($"No meeting found with id '{meetingId}'.");
            return meeting;
        }

        private async Task RequireOwnedPersonAsync(Guid ownerId, Guid personId)
        {
            var person = await _persons.GetPersonById(personId);
            if (person == null || person.ApplicationUserId != ownerId)
                throw new KeyNotFoundException($"No person found with id '{personId}'.");
        }

        private async Task<Dictionary<Guid, string?>> NameMapAsync()
        {
            var people = await _persons.GetAllPersons();
            return people.Where(p => p != null).ToDictionary(p => p!.PersonId, p => p!.Name);
        }

        private async Task<MeetingResponse> RespondAsync(Meeting meeting)
        {
            var names = await NameMapAsync();
            return MeetingResponse.FromMeeting(meeting, id => names.TryGetValue(id, out var name) ? name : null);
        }

        private static string CleanTitle(string? title)
        {
            var clean = (title ?? string.Empty).Trim();
            if (clean.Length == 0)
                throw new ArgumentException("Meeting title is required.", nameof(title));
            if (clean.Length > 200)
                throw new ArgumentException("Meeting title cannot exceed 200 characters.", nameof(title));
            return clean;
        }

        private static string? CleanNullable(string? value, int max, string param)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var clean = value.Trim();
            if (clean.Length > max)
                throw new ArgumentException($"{param} cannot exceed {max} characters.", param);
            return clean;
        }

        private static List<MeetingPerson> PersonsFromNames(IEnumerable<string>? names)
        {
            return (names ?? Enumerable.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Where(n => n.Length <= 200)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(n => new MeetingPerson
                {
                    MeetingPersonId = Guid.NewGuid(),
                    DetectedName = n,
                    MatchStatus = PersonMatchStatus.Unmapped,
                    SelectedForLogging = false
                })
                .Take(50)
                .ToList();
        }

        public async Task<List<MeetingResponse>> ListAsync()
        {
            using (Operation.Time("List meetings"))
            {
                var ownerId = OwnerId();
                var meetings = await _meetings.ListAsync(ownerId);
                var names = await NameMapAsync();
                return meetings.Select(m => MeetingResponse.FromMeeting(m,
                    id => names.TryGetValue(id, out var name) ? name : null)).ToList();
            }
        }

        public async Task<List<MeetingResponse>> ListMeetingsForPersonAsync(Guid personId)
        {
            using (Operation.Time("List meetings for person"))
            {
                var ownerId = OwnerId();
                await RequireOwnedPersonAsync(ownerId, personId);
                var meetings = await _meetings.ListForMappedPersonAsync(ownerId, personId);
                var names = await NameMapAsync();
                return meetings.Select(m => MeetingResponse.FromMeeting(m,
                    id => names.TryGetValue(id, out var name) ? name : null)).ToList();
            }
        }

        public async Task<MeetingResponse> GetAsync(Guid meetingId)
        {
            var ownerId = OwnerId();
            return await RespondAsync(await RequireMeetingAsync(ownerId, meetingId));
        }

        public async Task<MeetingResponse> CreatePrepAsync(MeetingCreateRequest request)
        {
            using (Operation.Time("Create meeting preparation"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var ownerId = OwnerId();
                var now = DateTime.UtcNow;
                var meeting = new Meeting
                {
                    MeetingId = Guid.NewGuid(),
                    ApplicationUserId = ownerId,
                    Title = CleanTitle(request.Title),
                    OccurredAtUtc = request.OccurredAtUtc == default ? now : request.OccurredAtUtc.ToUniversalTime(),
                    Description = CleanNullable(request.Description, 2000, nameof(request.Description)),
                    Agenda = CleanNullable(request.Agenda, 2000, nameof(request.Agenda)),
                    UserInstructions = CleanNullable(request.UserInstructions, 1000, nameof(request.UserInstructions)),
                    Status = MeetingStatus.Preparation,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    People = PersonsFromNames(request.ParticipantNames)
                };

                var goal = CleanNullable(request.Goal, 1000, nameof(request.Goal));
                if (goal != null)
                {
                    meeting.Brief = new MeetingBrief
                    {
                        MeetingBriefId = Guid.NewGuid(),
                        MeetingId = meeting.MeetingId,
                        Goal = goal,
                        BriefJson = "{\"participants\":[]}",
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    };
                }

                await _meetings.AddAsync(meeting);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created meeting preparation {MeetingId}", meeting.MeetingId);
                return await RespondAsync(meeting);
            }
        }

        public async Task<MeetingResponse> CreateDraftAsync(MeetingCreateRequest request)
        {
            using (Operation.Time("Create meeting draft"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                if (!string.IsNullOrWhiteSpace(request.RawTranscript) && request.RawTranscript.Length > MaxTranscriptChars)
                    throw new ArgumentException($"Transcript cannot exceed {MaxTranscriptChars} characters.", nameof(request.RawTranscript));

                var ownerId = OwnerId();
                var now = DateTime.UtcNow;
                var meeting = new Meeting
                {
                    MeetingId = Guid.NewGuid(),
                    ApplicationUserId = ownerId,
                    Title = CleanTitle(request.Title),
                    OccurredAtUtc = request.OccurredAtUtc == default ? now : request.OccurredAtUtc.ToUniversalTime(),
                    Description = CleanNullable(request.Description, 2000, nameof(request.Description)),
                    Agenda = CleanNullable(request.Agenda, 2000, nameof(request.Agenda)),
                    UserInstructions = CleanNullable(request.UserInstructions, 1000, nameof(request.UserInstructions)),
                    RawTranscript = string.IsNullOrWhiteSpace(request.RawTranscript) ? null : request.RawTranscript,
                    RawNotes = CleanNullable(request.RawNotes, 10000, nameof(request.RawNotes)),
                    Status = MeetingStatus.Draft,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    People = PersonsFromNames(request.ParticipantNames)
                };

                await _meetings.AddAsync(meeting);
                await _unitOfWork.SaveChangesAsync();

                return await RespondAsync(meeting);
            }
        }

        public async Task<MeetingResponse> BeginLoggingAsync(Guid meetingId, DateTime? actualOccurredAtUtc = null)
        {
            using (Operation.Time("Begin meeting logging"))
            {
                var ownerId = OwnerId();
                var meeting = await RequireMeetingAsync(ownerId, meetingId);
                if (meeting.Status != MeetingStatus.Preparation)
                    throw new InvalidOperationException("Only meetings in Preparation can transition to logging.");

                meeting.Status = MeetingStatus.Draft;
                if (actualOccurredAtUtc != null)
                    meeting.ActualOccurredAtUtc = actualOccurredAtUtc.Value.ToUniversalTime();
                meeting.UpdatedAtUtc = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
                return await RespondAsync(meeting);
            }
        }

        public async Task<MeetingResponse> SetTranscriptAsync(MeetingTranscriptRequest request)
        {
            using (Operation.Time("Set meeting transcript"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var ownerId = OwnerId();
                var meeting = await RequireMeetingAsync(ownerId, request.MeetingId);
                if (meeting.Status != MeetingStatus.Draft && meeting.Status != MeetingStatus.Preparation)
                    throw new InvalidOperationException("Transcript can only be set on Preparation or Draft meetings.");

                if (!string.IsNullOrWhiteSpace(request.RawTranscript) && request.RawTranscript.Length > MaxTranscriptChars)
                    throw new ArgumentException($"Transcript cannot exceed {MaxTranscriptChars} characters.", nameof(request.RawTranscript));

                meeting.RawTranscript = string.IsNullOrWhiteSpace(request.RawTranscript) ? null : request.RawTranscript;
                meeting.RawNotes = CleanNullable(request.RawNotes, 10000, nameof(request.RawNotes));
                meeting.UserInstructions = CleanNullable(request.UserInstructions, 1000, nameof(request.UserInstructions)) ?? meeting.UserInstructions;
                meeting.UpdatedAtUtc = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
                return await RespondAsync(meeting);
            }
        }

        public async Task<MeetingResponse> ProcessAsync(Guid meetingId)
        {
            using (Operation.Time("Process meeting"))
            {
                var ownerId = OwnerId();
                var meeting = await RequireMeetingAsync(ownerId, meetingId);
                if (meeting.Status != MeetingStatus.Draft)
                    throw new InvalidOperationException("Only Draft meetings can be processed.");
                if (string.IsNullOrWhiteSpace(meeting.RawTranscript) && string.IsNullOrWhiteSpace(meeting.RawNotes))
                    throw new InvalidOperationException("A transcript or notes are required before processing.");

                meeting.Status = MeetingStatus.Processing;
                meeting.UpdatedAtUtc = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();

                try
                {
                    var people = await _persons.GetAllPersons();
                    var extraction = await _extractor.ExtractAsync(new MeetingExtractionInput
                    {
                        Title = meeting.Title,
                        OccurredAtUtc = meeting.OccurredAtUtc,
                        Description = meeting.Description,
                        UserInstructions = meeting.UserInstructions,
                        Transcript = meeting.RawTranscript,
                        Notes = meeting.RawNotes,
                        KnownPeople = people.Where(p => p != null).Take(500).Select(p => new KnownMeetingPerson
                        {
                            PersonId = p!.PersonId,
                            Name = p.Name ?? string.Empty
                        }).ToList(),
                        ActiveMemoryTitles = await ActiveMemoryTitlesAsync(people)
                    });
                    extraction = MeetingExtractionValidator.Validate(extraction);

                    meeting.ProcessedSummary = extraction.Summary;
                    await MergeDetectedPeopleAsync(meeting, extraction.DetectedPeople, people);
                    await PersistFindingsAsync(ownerId, meeting, extraction);

                    meeting.Status = MeetingStatus.Processed;
                    meeting.UpdatedAtUtc = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Processed meeting {MeetingId}: {Findings} findings", meetingId, extraction.Findings.Count);
                    return await RespondAsync(meeting);
                }
                catch
                {
                    meeting.Status = MeetingStatus.Draft;
                    meeting.UpdatedAtUtc = DateTime.UtcNow;
                    await _unitOfWork.SaveChangesAsync();
                    throw;
                }
            }
        }

        private async Task MergeDetectedPeopleAsync(Meeting meeting, List<string> detected, IEnumerable<Person?> people)
        {
            var owned = people.Where(p => p != null).ToList();
            foreach (var name in detected.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(50))
            {
                if (meeting.People.Any(p => string.Equals(p.DetectedName, name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var match = SuggestMatch(name, owned);
                // No duplicate rows for one canonical person (§17): a second
                // spelling variant pointing at the same person is skipped.
                if (match != null && meeting.People.Any(p => p.MappedPersonId != null && p.MappedPersonId == match.PersonId))
                    continue;
                var row = new MeetingPerson
                {
                    MeetingPersonId = Guid.NewGuid(),
                    MeetingId = meeting.MeetingId,
                    DetectedName = name,
                    MappedPersonId = match?.PersonId,
                    MatchStatus = match == null ? PersonMatchStatus.Unmapped : PersonMatchStatus.Suggested,
                    SelectedForLogging = false
                };
                // Explicit add only: relationship fixup attaches the row to
                // meeting.People. Touching the collection again would duplicate it.
                await _meetings.AddPersonAsync(row);
            }
        }

        // Entity resolution (§17/§20): exact full-name match only. Never guess
        // from a first name or prefix — the user chooses from candidates.
        private static Person? SuggestMatch(string detected, List<Person?> owned)
        {
            var exact = owned.Where(p => string.Equals(p?.Name?.Trim(), detected.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            if (exact.Count == 1) return exact[0];
            return null;
        }

        private async Task PersistFindingsAsync(Guid ownerId, Meeting meeting, MeetingExtraction extraction)
        {
            foreach (var topic in extraction.Topics)
                await AddFindingAsync(meeting, null, FindingKind.Topic, topic, null, null);
            foreach (var decision in extraction.Decisions)
                await AddFindingAsync(meeting, null, FindingKind.Decision, decision, null, null);

            foreach (var finding in extraction.Findings)
            {
                Guid? mappedPersonId = null;
                if (!string.IsNullOrWhiteSpace(finding.PersonName))
                {
                    var row = meeting.People.FirstOrDefault(p =>
                        string.Equals(p.DetectedName, finding.PersonName!.Trim(), StringComparison.OrdinalIgnoreCase));
                    mappedPersonId = row?.MappedPersonId;
                }

                if (mappedPersonId != null && await IsDuplicateOfActiveMemoryAsync(ownerId, mappedPersonId.Value, finding.Title))
                {
                    _logger.LogInformation("Suppressed duplicate finding '{Title}' for person {PersonId}", finding.Title, mappedPersonId);
                    continue;
                }

                await AddFindingAsync(meeting, mappedPersonId, finding.Kind, finding.Title, finding.Detail, finding.SourceExcerpt);
            }
        }

        private async Task AddFindingAsync(Meeting meeting, Guid? personId, FindingKind kind, string title, string? detail, string? excerpt)
        {
            var entity = new MeetingFinding
            {
                MeetingFindingId = Guid.NewGuid(),
                MeetingId = meeting.MeetingId,
                MappedPersonId = personId,
                Kind = kind,
                Title = title,
                Detail = detail,
                Status = FindingStatus.Suggested,
                SourceExcerpt = excerpt,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _meetings.AddFindingAsync(entity);
        }

        private async Task<bool> IsDuplicateOfActiveMemoryAsync(Guid ownerId, Guid personId, string title)
        {
            var person = await _persons.GetPersonById(personId);
            if (person == null || person.ApplicationUserId != ownerId)
                return false;

            var entries = await _memory.ListForPersonAsync(personId);
            var normalized = Normalize(title);
            return entries.Any(e => e.Status == 0 && Normalize(e.Title) == normalized);
        }

        private static string Normalize(string value) =>
            new string((value ?? string.Empty).Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        public async Task<MeetingResponse> UpdateMappingsAsync(Guid meetingId, List<PersonMappingRequest> mappings)
        {
            using (Operation.Time("Update meeting mappings"))
            {
                if (mappings == null)
                    throw new ArgumentNullException(nameof(mappings));

                var ownerId = OwnerId();
                var meeting = await RequireMeetingAsync(ownerId, meetingId);
                if (meeting.Status == MeetingStatus.Confirmed || meeting.Status == MeetingStatus.Discarded)
                    throw new InvalidOperationException("Mappings cannot change after confirmation.");

                foreach (var mapping in mappings)
                {
                    var row = meeting.People.FirstOrDefault(p => p.MeetingPersonId == mapping.MeetingPersonId);
                    if (row == null)
                        throw new KeyNotFoundException($"No meeting person found with id '{mapping.MeetingPersonId}'.");

                    if (mapping.MappedPersonId != null)
                    {
                        var person = await _persons.GetPersonById(mapping.MappedPersonId);
                        if (person == null || person.ApplicationUserId != ownerId)
                            throw new KeyNotFoundException($"No person found with id '{mapping.MappedPersonId}'.");
                    }
                    if (mapping.MatchStatus == PersonMatchStatus.Confirmed && mapping.MappedPersonId == null)
                        throw new ArgumentException("A confirmed mapping requires a person.", nameof(mapping.MappedPersonId));

                    row.MappedPersonId = mapping.MappedPersonId;
                    row.MatchStatus = mapping.MatchStatus;
                    row.SelectedForLogging = mapping.SelectedForLogging
                        && mapping.MappedPersonId != null
                        && mapping.MatchStatus == PersonMatchStatus.Confirmed;
                }

                meeting.UpdatedAtUtc = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
                return await RespondAsync(meeting);
            }
        }

        public async Task<MeetingResponse> ReviewFindingAsync(FindingReviewRequest request)
        {
            using (Operation.Time("Review meeting finding"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var ownerId = OwnerId();
                var meetings = await _meetings.ListAsync(ownerId);
                MeetingFinding? finding = null;
                Meeting? meeting = null;
                foreach (var candidate in meetings)
                {
                    var full = await _meetings.GetAsync(ownerId, candidate.MeetingId);
                    finding = full?.Findings.FirstOrDefault(f => f.MeetingFindingId == request.MeetingFindingId);
                    if (finding != null)
                    {
                        meeting = full;
                        break;
                    }
                }
                if (finding == null || meeting == null)
                    throw new KeyNotFoundException($"No finding found with id '{request.MeetingFindingId}'.");
                if (meeting.Status == MeetingStatus.Confirmed || meeting.Status == MeetingStatus.Discarded)
                    throw new InvalidOperationException("Findings cannot change after confirmation.");

                if (request.MappedPersonId != null)
                {
                    var person = await _persons.GetPersonById(request.MappedPersonId);
                    if (person == null || person.ApplicationUserId != ownerId)
                        throw new KeyNotFoundException($"No person found with id '{request.MappedPersonId}'.");
                    finding.MappedPersonId = request.MappedPersonId;
                }

                if (request.Status == FindingStatus.Accepted)
                {
                    var title = (request.Title ?? finding.Title ?? string.Empty).Trim();
                    if (title.Length == 0)
                        throw new ArgumentException("Accepted findings require a title.", nameof(request.Title));
                    finding.Title = title.Length > 200 ? title[..200] : title;
                    if (request.Detail != null)
                        finding.Detail = request.Detail.Trim().Length > 2000 ? request.Detail.Trim()[..2000] : request.Detail.Trim();
                    finding.Status = FindingStatus.Accepted;
                    finding.ResolutionNote = CleanNullable(request.ResolutionNote, 500, nameof(request.ResolutionNote));
                    // Approval model (§21): finding-Accept only stages the finding.
                    // Durable memory is written once at meeting Confirm, so
                    // unconfirmed-meeting facts never leak into Copilot/queue.
                    // (ConfirmAsync sweeps Accepted findings with null entry id.)
                }
                else
                {
                    finding.Status = FindingStatus.Rejected;
                    finding.ResolutionNote = CleanNullable(request.ResolutionNote, 500, nameof(request.ResolutionNote));
                }

                meeting.UpdatedAtUtc = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
                return await RespondAsync(meeting);
            }
        }

        // Every FindingKind maps somewhere: Accept must never silently drop
        // evidence (§19). Action-oriented kinds become commitments, open
        // questions stay visible as topics, date mentions persist as milestones.
        private static readonly Dictionary<FindingKind, RelationshipMemoryKind> FindingToMemoryKind = new()
        {
            { FindingKind.Commitment, RelationshipMemoryKind.Commitment },
            { FindingKind.ActionItem, RelationshipMemoryKind.Commitment },
            { FindingKind.FollowUp, RelationshipMemoryKind.Commitment },
            { FindingKind.PersonFact, RelationshipMemoryKind.Fact },
            { FindingKind.Project, RelationshipMemoryKind.SharedProject },
            { FindingKind.Topic, RelationshipMemoryKind.Topic },
            { FindingKind.Question, RelationshipMemoryKind.Topic },
            { FindingKind.Decision, RelationshipMemoryKind.Fact },
            { FindingKind.Event, RelationshipMemoryKind.Milestone },
            { FindingKind.DateMention, RelationshipMemoryKind.Milestone }
        };

        private async Task<Guid?> ConvertToMemoryAsync(Guid ownerId, Meeting meeting, MeetingFinding finding)
        {
            if (finding.MappedPersonId == null)
                return null;
            if (!FindingToMemoryKind.TryGetValue(finding.Kind, out var memoryKind))
                return null;
            if (await IsDuplicateOfActiveMemoryAsync(ownerId, finding.MappedPersonId.Value, finding.Title))
                return null;

            var entry = new RelationshipMemoryEntry
            {
                MemoryEntryId = Guid.NewGuid(),
                ApplicationUserId = ownerId,
                PersonId = finding.MappedPersonId.Value,
                Kind = memoryKind,
                Title = finding.Title,
                Detail = finding.Detail,
                Status = MemoryEntryStatus.Active,
                Provenance = MemoryProvenance.MeetingDerived,
                SourceMeetingId = meeting.MeetingId,
                SourceFindingId = finding.MeetingFindingId,
                SourceExcerpt = finding.SourceExcerpt,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            await _memoryEntries.AddAsync(entry);
            return entry.MemoryEntryId;
        }

        public async Task<MeetingResponse> SaveBriefAsync(BriefSaveRequest request)
        {
            using (Operation.Time("Save meeting brief"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var ownerId = OwnerId();
                var meeting = await RequireMeetingAsync(ownerId, request.MeetingId);
                if (meeting.Status == MeetingStatus.Confirmed || meeting.Status == MeetingStatus.Discarded)
                    throw new InvalidOperationException("The brief cannot change after confirmation.");

                if (request.BriefJson != null)
                    MeetingBriefValidator.Validate(request.BriefJson);

                var now = DateTime.UtcNow;
                if (meeting.Brief == null)
                {
                    if (request.BriefJson == null)
                        throw new ArgumentException("Brief content is required.", nameof(request.BriefJson));
                    var brief = new MeetingBrief
                    {
                        MeetingBriefId = Guid.NewGuid(),
                        MeetingId = meeting.MeetingId,
                        Goal = CleanNullable(request.Goal, 1000, nameof(request.Goal)),
                        BriefJson = request.BriefJson,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    };
                    await _meetings.AddBriefAsync(brief);
                    meeting.Brief = brief;
                }
                else
                {
                    if (request.BriefJson != null)
                        meeting.Brief.BriefJson = request.BriefJson;
                    if (request.Goal != null)
                        meeting.Brief.Goal = CleanNullable(request.Goal, 1000, nameof(request.Goal));
                    meeting.Brief.UpdatedAtUtc = now;
                }

                meeting.UpdatedAtUtc = now;
                await _unitOfWork.SaveChangesAsync();
                return await RespondAsync(meeting);
            }
        }

        public async Task<MeetingResponse> GenerateBriefAsync(Guid meetingId)
        {
            using (Operation.Time("Generate meeting brief"))
            {
                var ownerId = OwnerId();
                var meeting = await RequireMeetingAsync(ownerId, meetingId);
                if (meeting.Status == MeetingStatus.Confirmed || meeting.Status == MeetingStatus.Discarded)
                    throw new InvalidOperationException("The brief cannot change after confirmation.");

                var participantSections = new List<object>();
                foreach (var row in meeting.People.Where(p => p.MappedPersonId != null))
                {
                    var person = await _persons.GetPersonById(row.MappedPersonId);
                    if (person == null || person.ApplicationUserId != ownerId)
                        continue;

                    var queue = await _scoring.GetQueueAsync(200);
                    var state = queue.FirstOrDefault(q => q.PersonId == person.PersonId);
                    var orgs = person.Circles?.Select(c => c.Name).ToList() ?? new List<string>();
                    var entries = await ActiveEntriesAsync(person.PersonId);
                    var interactions = await _interactions.ListForPersonAsync(person.PersonId);
                    var recentTopics = interactions
                        .OrderByDescending(i => i.TimeOfInteraction)
                        .Take(3)
                        .Select(i => $"{i.InteractionTitle} ({i.TimeOfInteraction:yyyy-MM-dd})")
                        .ToList();
                    var personEvents = await _eventsForBriefAsync(person.PersonId);
                    var openCommitments = entries
                        .Where(e => e.Kind == RelationshipMemoryKind.Commitment)
                        .Select(e => e.Title)
                        .ToList();
                    var goals = entries
                        .Where(e => e.Kind == RelationshipMemoryKind.Goal || e.Kind == RelationshipMemoryKind.Intent)
                        .Select(e => e.Title)
                        .ToList();
                    var silenceLine = state == null || state.LastContactAtUtc == null
                        ? "No contact recorded."
                        : $"Last contact {state.LastContactAtUtc.Value:yyyy-MM-dd}" +
                          (state.CadenceReferenceDays == null ? "." : $" vs ~{Math.Round(state.CadenceReferenceDays.Value)}d rhythm.");

                    participantSections.Add(new
                    {
                        personId = person.PersonId,
                        displayName = row.DetectedName,
                        whoIsThis = person.Origin ?? (orgs.Count == 0 ? null : string.Join(", ", orgs)),
                        relationshipHistory = recentTopics,
                        lastInteraction = recentTopics.FirstOrDefault(),
                        state = state == null ? null : new
                        {
                            band = state.Band,
                            urgency = Math.Round(state.UrgencyScore),
                            lastContact = state.LastContactAtUtc,
                            cadenceDays = state.CadenceReferenceDays,
                            evidence = state.EvidenceStatus
                        },
                        recentTopics,
                        sharedProjects = entries
                            .Where(e => e.Kind == RelationshipMemoryKind.SharedProject)
                            .Select(e => e.Title)
                            .ToList(),
                        commitments = openCommitments,
                        relevantEvents = personEvents,
                        relevantGoals = goals,
                        thingsToRemember = openCommitments
                            .Select(c => $"Open commitment: {c}")
                            .Concat(personEvents.Select(e => $"{e} approaching."))
                            .Concat(new[] { silenceLine })
                            .Take(6)
                            .ToList(),
                        talkingPoints = recentTopics
                            .Select(t => $"Revisit: {t}")
                            .Concat(openCommitments.Select(c => $"Follow up on: {c}"))
                            .Take(6)
                            .ToList(),
                        questionsToAsk = openCommitments
                            .Select(c => $"What is the status of: {c}?")
                            .Concat(goals.Select(g => $"How is this going: {g}?"))
                            .Take(5)
                            .ToList()
                    });
                }

                var brief = new
                {
                    meeting = new
                    {
                        title = meeting.Title,
                        plannedAt = meeting.OccurredAtUtc,
                        agenda = meeting.Agenda,
                        goal = meeting.Brief?.Goal
                    },
                    participants = participantSections
                };
                var json = JsonSerializer.Serialize(brief);

                var now = DateTime.UtcNow;
                if (meeting.Brief == null)
                {
                    var briefEntity = new MeetingBrief
                    {
                        MeetingBriefId = Guid.NewGuid(),
                        MeetingId = meeting.MeetingId,
                        BriefJson = json,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    };
                    await _meetings.AddBriefAsync(briefEntity);
                    meeting.Brief = briefEntity;
                }
                else
                {
                    meeting.Brief.BriefJson = json;
                    meeting.Brief.UpdatedAtUtc = now;
                }

                meeting.UpdatedAtUtc = now;
                await _unitOfWork.SaveChangesAsync();
                return await RespondAsync(meeting);
            }
        }

        private async Task<List<string>> ActiveMemoryTitlesAsync(IEnumerable<Person?> people)
        {
            var titles = new List<string>();
            foreach (var person in people.Where(p => p != null).Take(100))
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
                titles.AddRange(entries.Where(e => e.Status == 0).Select(e => e.Title));
                if (titles.Count >= 200)
                    break;
            }
            return titles;
        }

        private async Task<List<MemoryEntryResponse>> ActiveEntriesAsync(Guid personId)
        {
            try
            {
                return (await _memory.ListForPersonAsync(personId))
                    .Where(e => e.Status == 0)
                    .ToList();
            }
            catch (KeyNotFoundException)
            {
                return new List<MemoryEntryResponse>();
            }
        }

        private async Task<List<string>> _eventsForBriefAsync(Guid personId)
        {
            var upcoming = await _events.GetUpcomingAsync(60);
            return upcoming
                .Where(e => e.PersonId == personId)
                .Select(e => $"{e.Title} in {e.InDays}d")
                .ToList();
        }

        private async Task<List<string>> ActiveCommitmentTitlesAsync(Guid personId)
        {
            return (await ActiveEntriesAsync(personId))
                .Where(e => e.Kind == RelationshipMemoryKind.Commitment)
                .Select(e => e.Title)
                .ToList();
        }

        public async Task<MeetingResponse> ConfirmAsync(MeetingConfirmRequest request)
        {
            using (Operation.Time("Confirm meeting"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var ownerId = OwnerId();
                var meeting = await RequireMeetingAsync(ownerId, request.MeetingId);
                if (meeting.Status != MeetingStatus.Processed)
                    throw new InvalidOperationException("Only Processed meetings can be confirmed.");

                var actualAt = (request.ActualOccurredAtUtc ?? meeting.OccurredAtUtc).ToUniversalTime();
                if (actualAt > DateTime.UtcNow.AddDays(1))
                    throw new ArgumentException("Actual meeting time cannot be in the future.", nameof(request.ActualOccurredAtUtc));

                meeting.ActualOccurredAtUtc = actualAt;

                var selected = meeting.People
                    .Where(p => p.SelectedForLogging)
                    .ToList();
                foreach (var row in selected)
                {
                    if (row.MappedPersonId == null || row.MatchStatus != PersonMatchStatus.Confirmed)
                        throw new InvalidOperationException($"Person '{row.DetectedName}' must have a confirmed mapping before logging.");
                }

                foreach (var row in selected)
                {
                    var personFindings = meeting.Findings
                        .Where(f => f.Status == FindingStatus.Accepted && f.MappedPersonId == row.MappedPersonId)
                        .Select(f => f.Title)
                        .ToList();
                    var description = new StringBuilder();
                    if (personFindings.Count > 0)
                    {
                        description.Append("From the meeting: ");
                        description.Append(string.Join("; ", personFindings.Take(5)));
                        description.Append(". ");
                    }
                    description.Append($"Source: Meeting — {meeting.Title} — {actualAt:yyyy-MM-dd}.");

                    await _interactions.LogAsync(new InteractionAddRequest
                    {
                        PersonId = row.MappedPersonId,
                        TimeOfInteraction = actualAt,
                        InteractionType = EnInteractionType.Meeting,
                        InteractionTitle = meeting.Title.Length > 100 ? meeting.Title[..100] : meeting.Title,
                        InteractionDescription = description.ToString().Length > 2000
                            ? description.ToString()[..2000]
                            : description.ToString(),
                        SourceMeetingId = meeting.MeetingId
                    });

                    foreach (var finding in meeting.Findings
                        .Where(f => f.Status == FindingStatus.Accepted
                            && f.MappedPersonId == row.MappedPersonId
                            && f.AcceptedAsEntryId == null))
                    {
                        finding.AcceptedAsEntryId = await ConvertToMemoryAsync(ownerId, meeting, finding);
                    }
                }

                meeting.Status = MeetingStatus.Confirmed;
                meeting.UpdatedAtUtc = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Confirmed meeting {MeetingId} for {Count} people", meeting.MeetingId, selected.Count);
                return await RespondAsync(meeting);
            }
        }

        public async Task DeleteAsync(Guid meetingId)
        {
            using (Operation.Time("Delete meeting"))
            {
                var ownerId = OwnerId();
                var meeting = await RequireMeetingAsync(ownerId, meetingId);
                if (meeting.Status == MeetingStatus.Confirmed)
                    throw new InvalidOperationException("Confirmed meetings are evidence and cannot be deleted.");

                var derived = await _memoryEntries.ListByMeetingAsync(ownerId, meetingId);
                foreach (var entry in derived)
                {
                    entry.SourceMeetingDeleted = true;
                    entry.UpdatedAtUtc = DateTime.UtcNow;
                }

                await _meetings.RemoveAsync(meeting);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deleted meeting {MeetingId}; {Count} derived entries preserved and stamped", meetingId, derived.Count);
            }
        }
    }
}
