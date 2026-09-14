using Entities;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs.IngestionDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Servicess
{
    /// <summary>
    /// Unified ingestion: raw text in, reviewed findings out, approved changes
    /// applied through the existing memory/person/event paths with provenance.
    /// Extraction never writes durable state; approval is the only write path.
    /// </summary>
    public class IngestionService : IIngestionService
    {
        public const int MaxRawChars = 50000;

        private static readonly string[] SupportedFields =
            { "role", "organization", "location", "email", "phone", "origin", "context" };

        private static readonly string[] SupportedRelations =
            { "introduced", "working-with", "reports-to", "referred-by", "colleague-of" };

        private readonly IngestionRepositoryContract _batches;
        private readonly MeetingRepositoryContract _meetings;
        private readonly PersonRepositryContract _persons;
        private readonly IPersonSearcherService _searcher;
        private readonly IPersonQuickAdderService _quickAdd;
        private readonly IPersonUpdaterService _updater;
        private readonly RelationshipMemoryRepositoryContract _memoryRepo;
        private readonly IRelationshipMemoryService _memory;
        private readonly IEventService _events;
        private readonly IIngestionExtractor _extractor;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IngestionService> _logger;

        public IngestionService(
            IngestionRepositoryContract batches,
            MeetingRepositoryContract meetings,
            PersonRepositryContract persons,
            IPersonSearcherService searcher,
            IPersonQuickAdderService quickAdd,
            IPersonUpdaterService updater,
            RelationshipMemoryRepositoryContract memoryRepo,
            IRelationshipMemoryService memory,
            IEventService events,
            IIngestionExtractor extractor,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            ILogger<IngestionService> logger)
        {
            _batches = batches;
            _meetings = meetings;
            _persons = persons;
            _searcher = searcher;
            _quickAdd = quickAdd;
            _updater = updater;
            _memoryRepo = memoryRepo;
            _memory = memory;
            _events = events;
            _extractor = extractor;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private Guid OwnerId()
        {
            var id = _currentUser.UserId;
            if (id == null || id == Guid.Empty)
                throw new UnauthorizedAccessException("Ingestion requires an authenticated user.");
            return id.Value;
        }

        public static string Normalize(string value) =>
            new string((value ?? string.Empty).Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        public static string HashSource(string rawText)
        {
            var normalized = string.Join(" ",
                (rawText ?? string.Empty).Trim().ToLowerInvariant()
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        }

        public async Task<IngestionBatchDto> SubmitAsync(IngestionSubmitRequest request)
        {
            using (Operation.Time("Submit ingestion"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));
                var raw = (request.RawText ?? string.Empty).Trim();
                if (raw.Length == 0)
                    throw new ArgumentException("Raw text is required.", nameof(request.RawText));
                if (raw.Length > MaxRawChars)
                    throw new ArgumentException($"Raw text cannot exceed {MaxRawChars} characters.", nameof(request.RawText));

                var ownerId = OwnerId();
                if (request.SourceMeetingId != null)
                    await RequireOwnedMeetingAsync(ownerId, request.SourceMeetingId.Value);

                var hash = HashSource(raw);
                var existing = await _batches.FindByHashAsync(ownerId, request.SourceType, request.SourceMeetingId, hash);
                if (existing != null)
                    return await ToDtoAsync(existing);

                var now = DateTime.UtcNow;
                var batch = new IngestionBatch
                {
                    IngestionBatchId = Guid.NewGuid(),
                    ApplicationUserId = ownerId,
                    SourceType = request.SourceType,
                    SourceMeetingId = request.SourceMeetingId,
                    SourceTextHash = hash,
                    RawText = raw.Length > MaxRawChars ? raw[..MaxRawChars] : raw,
                    Status = IngestionStatus.Pending,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                await _batches.AddBatchAsync(batch);
                await _unitOfWork.SaveChangesAsync();
                return await ToDtoAsync(batch);
            }
        }

        private async Task RequireOwnedMeetingAsync(Guid ownerId, Guid meetingId)
        {
            var meeting = await _meetings.GetAsync(ownerId, meetingId);
            if (meeting == null)
                throw new KeyNotFoundException($"No meeting found with id '{meetingId}'.");
        }

        public async Task<IngestionBatchDto> SubmitForMeetingAsync(Guid meetingId)
        {
            using (Operation.Time("Submit meeting ingestion"))
            {
                var ownerId = OwnerId();
                var meeting = await _meetings.GetAsync(ownerId, meetingId);
                if (meeting == null)
                    throw new KeyNotFoundException($"No meeting found with id '{meetingId}'.");
                // Actual evidence only: transcript + notes recorded when the
                // meeting happened. Planned content (agenda, preparation notes,
                // description) is intention and must never become evidence.
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(meeting.RawTranscript))
                    parts.Add(meeting.RawTranscript.Trim());
                if (!string.IsNullOrWhiteSpace(meeting.RawNotes))
                    parts.Add(meeting.RawNotes.Trim());
                var raw = string.Join("\n\n", parts).Trim();
                if (raw.Length == 0)
                    throw new InvalidOperationException("Log what happened first — a transcript or notes are required.");
                if (raw.Length > MaxRawChars)
                    throw new ArgumentException($"Meeting content cannot exceed {MaxRawChars} characters.");
                return await SubmitAsync(new IngestionSubmitRequest
                {
                    SourceType = IngestionSourceType.MeetingText,
                    RawText = raw,
                    SourceMeetingId = meetingId
                });
            }
        }

        public async Task<IngestionBatchDto> ProcessAsync(Guid batchId)
        {
            using (Operation.Time("Process ingestion"))
            {
                var ownerId = OwnerId();
                var batch = await RequireBatchAsync(ownerId, batchId);
                if (batch.Findings.Count > 0 || batch.IsNoOp)
                    return await ToDtoAsync(batch);

                var people = await _persons.GetAllPersons();
                var extraction = await _extractor.ExtractAsync(new IngestionExtractionInput
                {
                    RawText = batch.RawText,
                    KnownPeople = people
                        .Where(p => p != null)
                        .Take(200)
                        .Select(p => new KnownIngestionPerson
                        {
                            PersonId = p!.PersonId,
                            Name = p.Name ?? "contact",
                            Organization = p.Circles.FirstOrDefault()?.Name,
                            Role = p.ContactItemRoles.FirstOrDefault()?.Role
                        })
                        .ToList()
                });

                var now = DateTime.UtcNow;
                if (extraction.NoOp || extraction.Findings.Count == 0)
                {
                    batch.IsNoOp = true;
                    batch.NoOpReason = string.IsNullOrWhiteSpace(extraction.NoOpReason)
                        ? "No meaningful relationship information found."
                        : extraction.NoOpReason.Trim();
                    batch.UpdatedAtUtc = now;
                    await _unitOfWork.SaveChangesAsync();
                    return await ToDtoAsync(batch);
                }

                var directory = people.Where(p => p != null).Select(p => p!).ToList();
                foreach (var item in extraction.Findings.Take(50))
                    await _batches.AddFindingAsync(await MapFindingAsync(ownerId, batch, item, extraction.Entities, directory, now));

                batch.UpdatedAtUtc = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
                return await GetAsync(batchId);
            }
        }

        private static FindingConfidence ParseConfidence(string? value) =>
            Enum.TryParse<FindingConfidence>(value, true, out var parsed) ? parsed : FindingConfidence.Medium;

        private async Task<IngestionFinding> MapFindingAsync(
            Guid ownerId, IngestionBatch batch, ExtractedIngestionFinding item,
            List<ExtractedIngestionEntity> entities, List<Person> directory, DateTime now)
        {
            var title = (item.Title ?? string.Empty).Trim();
            if (title.Length == 0)
                title = "(untitled finding)";
            if (title.Length > 200)
                title = title[..200];

            var finding = new IngestionFinding
            {
                IngestionFindingId = Guid.NewGuid(),
                IngestionBatchId = batch.IngestionBatchId,
                ApplicationUserId = ownerId,
                Title = title,
                Detail = CleanNullable(item.Detail, 2000),
                SourceExcerpt = CleanNullable(item.Excerpt, 500),
                Confidence = ParseConfidence(item.Confidence),
                UncertaintyReason = CleanNullable(item.UncertaintyReason, 500),
                Status = IngestionFindingStatus.Pending,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            var entity = entities.FirstOrDefault(e =>
                string.Equals(e.Name?.Trim(), item.Subject?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (entity != null && entity.Ignore)
            {
                finding.Status = IngestionFindingStatus.Rejected;
                finding.ReviewNote = "Marked not-a-person during extraction.";
                return finding;
            }

            ResolveSubject(finding, entity, item, directory);
            MapSlots(finding, item);
            await AnalyzeConflictAsync(ownerId, finding);
            return finding;
        }

        private static void ResolveSubject(
            IngestionFinding finding, ExtractedIngestionEntity? entity,
            ExtractedIngestionFinding item, List<Person> directory)
        {
            var subjectName = (entity?.Name ?? item.Subject ?? string.Empty).Trim();
            finding.SubjectName = subjectName.Length > 100 ? subjectName[..100] : subjectName;

            if (entity?.ExistingPersonId != null
                && directory.Any(p => p.PersonId == entity.ExistingPersonId))
            {
                finding.SubjectPersonId = entity.ExistingPersonId;
                finding.SubjectIsNew = false;
                return;
            }
            if (entity != null && entity.IsNew)
            {
                finding.SubjectIsNew = true;
                return;
            }

            var matches = directory
                .Where(p => (p.Name ?? string.Empty).Contains(subjectName, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
            if (matches.Count == 1 && string.Equals(matches[0].Name?.Trim(), subjectName, StringComparison.OrdinalIgnoreCase))
            {
                finding.SubjectPersonId = matches[0].PersonId;
                finding.SubjectIsNew = false;
                return;
            }
            if (matches.Count == 0 && entity == null)
            {
                finding.SubjectIsNew = true;
                return;
            }
            finding.Status = IngestionFindingStatus.Unresolved;
            finding.Confidence = FindingConfidence.Unresolved;
            finding.UncertaintyReason = matches.Count == 0
                ? $"No contact matches '{subjectName}'. Pick one, create them, or ignore."
                : $"Several contacts match '{subjectName}'. Pick the right one.";
        }

        private static void MapSlots(IngestionFinding finding, ExtractedIngestionFinding item)
        {
            var relation = (item.Relation ?? string.Empty).Trim().ToLowerInvariant().Replace("_", "-");
            if (!string.IsNullOrWhiteSpace(relation))
            {
                if (!SupportedRelations.Contains(relation))
                {
                    finding.Status = IngestionFindingStatus.Unresolved;
                    finding.Confidence = FindingConfidence.Unresolved;
                    finding.UncertaintyReason = $"Unsupported relation '{item.Relation}'. Supported: {string.Join(", ", SupportedRelations)}.";
                    return;
                }
                finding.RelationKind = relation;
                finding.ObjectName = CleanNullable(item.Object, 100);
                finding.ObjectOrg = CleanNullable(item.ObjectOrg, 100);
                if (string.IsNullOrWhiteSpace(finding.ObjectName) && string.IsNullOrWhiteSpace(finding.ObjectOrg))
                {
                    finding.Status = IngestionFindingStatus.Unresolved;
                    finding.Confidence = FindingConfidence.Unresolved;
                    finding.UncertaintyReason = "Relation needs a target person or organization.";
                }
                return;
            }

            if (!string.IsNullOrWhiteSpace(item.EventKind))
            {
                if (!Enum.TryParse<RelationshipEventType>(item.EventKind.Trim(), true, out _))
                {
                    finding.Status = IngestionFindingStatus.Unresolved;
                    finding.Confidence = FindingConfidence.Unresolved;
                    finding.UncertaintyReason = $"Unsupported event kind '{item.EventKind}'.";
                    return;
                }
                finding.EventKind = item.EventKind.Trim();
                return;
            }

            if (!string.IsNullOrWhiteSpace(item.TargetField))
            {
                var field = item.TargetField.Trim().ToLowerInvariant();
                if (!SupportedFields.Contains(field))
                {
                    finding.Status = IngestionFindingStatus.Unresolved;
                    finding.Confidence = FindingConfidence.Unresolved;
                    finding.UncertaintyReason = $"Unsupported field '{item.TargetField}'. Supported: {string.Join(", ", SupportedFields)}.";
                    return;
                }
                finding.TargetField = field;
                return;
            }

            if (!string.IsNullOrWhiteSpace(item.MemoryKind))
            {
                if (!Enum.TryParse<RelationshipMemoryKind>(item.MemoryKind.Trim(), true, out var kind))
                {
                    finding.Status = IngestionFindingStatus.Unresolved;
                    finding.Confidence = FindingConfidence.Unresolved;
                    finding.UncertaintyReason = $"Unsupported memory type '{item.MemoryKind}'.";
                    return;
                }
                finding.MemoryKind = kind;
                return;
            }

            finding.Status = IngestionFindingStatus.Unresolved;
            finding.Confidence = FindingConfidence.Unresolved;
            finding.UncertaintyReason = "No supported target slot. Map to a field, memory type, or event.";
        }

        private async Task AnalyzeConflictAsync(Guid ownerId, IngestionFinding finding)
        {
            if (finding.SubjectPersonId == null || finding.SubjectIsNew)
                return;
            if (finding.MemoryKind == null && finding.TargetField == null)
                return;

            if (finding.MemoryKind != null)
            {
                var active = (await _memory.ListForPersonAsync(finding.SubjectPersonId.Value))
                    .Where(e => e.Status == 0 && e.Kind == finding.MemoryKind)
                    .ToList();
                var same = active.FirstOrDefault(e => Normalize(e.Title) == Normalize(finding.Title));
                if (same != null)
                {
                    finding.ConflictType = FindingConflictType.Duplicate;
                    finding.ExistingValue = same.Title;
                    return;
                }
                var latest = active.OrderByDescending(e => e.UpdatedAtUtc).FirstOrDefault();
                if (latest != null)
                {
                    finding.ConflictType = FindingConflictType.Update;
                    finding.ProposalAction = FindingProposalAction.Update;
                    finding.ExistingValue = latest.Title +
                        (string.IsNullOrWhiteSpace(latest.Detail) ? "" : $" — {latest.Detail}");
                }
                return;
            }

            var person = await _persons.GetPersonById(finding.SubjectPersonId);
            if (person == null || person.ApplicationUserId != ownerId)
                return;
            var current = ReadPersonField(person, finding.TargetField!);
            if (current == null)
                return;
            if (Normalize(current) == Normalize(finding.Title))
            {
                finding.ConflictType = FindingConflictType.Duplicate;
                finding.ExistingValue = current;
                return;
            }
            finding.ConflictType = finding.TargetField is "organization" or "role"
                ? FindingConflictType.None
                : FindingConflictType.Update;
            finding.ExistingValue = current;
            if (finding.ConflictType == FindingConflictType.Update)
                finding.ProposalAction = FindingProposalAction.Update;
        }

        private static string? ReadPersonField(Person person, string field) => field switch
        {
            "role" => person.ContactItemRoles.FirstOrDefault()?.Role,
            "organization" => person.Circles.FirstOrDefault()?.Name,
            "location" => person.Address,
            "email" => person.email,
            "phone" => person.phone,
            "origin" => person.Origin,
            "context" => person.ContextMemory,
            _ => null
        };

        private static string? CleanNullable(string? value, int max) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length > max ? value.Trim()[..max] : value.Trim();

        private async Task<IngestionBatch> RequireBatchAsync(Guid ownerId, Guid batchId)
        {
            var batch = await _batches.GetAsync(ownerId, batchId);
            if (batch == null)
                throw new KeyNotFoundException($"No ingestion found with id '{batchId}'.");
            return batch;
        }

        public async Task<IngestionBatchDto> GetAsync(Guid batchId)
        {
            var ownerId = OwnerId();
            return await ToDtoAsync(await RequireBatchAsync(ownerId, batchId));
        }

        public async Task<List<IngestionBatchDto>> ListAsync()
        {
            var ownerId = OwnerId();
            var batches = await _batches.ListAsync(ownerId);
            return batches.Select(ToSummaryDto).ToList();
        }

        public async Task<List<IngestionFindingDto>> ListPendingAsync()
        {
            var ownerId = OwnerId();
            var directory = (await _persons.GetAllPersons()).Where(p => p != null).Select(p => p!).ToList();
            var findings = await _batches.ListPendingAsync(ownerId);
            var names = directory.ToDictionary(p => p.PersonId, p => p.Name);
            return findings.Select(f => ToFindingDto(f, directory, names)).ToList();
        }

        public async Task DiscardAsync(Guid batchId)
        {
            using (Operation.Time("Discard ingestion"))
            {
                var ownerId = OwnerId();
                var batch = await RequireBatchAsync(ownerId, batchId);
                if (batch.Status == IngestionStatus.Applied)
                    throw new InvalidOperationException("An applied ingestion cannot be discarded.");
                batch.Status = IngestionStatus.Discarded;
                batch.UpdatedAtUtc = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<IngestionFindingDto> ReviewAsync(IngestionFindingReviewRequest request)
        {
            using (Operation.Time("Review ingestion finding"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));
                var ownerId = OwnerId();
                var finding = await _batches.GetFindingAsync(ownerId, request.FindingId);
                if (finding == null)
                    throw new KeyNotFoundException($"No finding found with id '{request.FindingId}'.");
                var batch = await RequireBatchAsync(ownerId, finding.IngestionBatchId);
                if (batch.Status == IngestionStatus.Applied || batch.Status == IngestionStatus.Discarded)
                    throw new InvalidOperationException("Findings cannot change after the batch is closed.");

                if (request.Status != IngestionFindingStatus.Approved
                    && request.Status != IngestionFindingStatus.Rejected
                    && request.Status != IngestionFindingStatus.Pending)
                    throw new ArgumentException("Review sets Approved, Rejected, or back to Pending.", nameof(request.Status));

                if (request.Title != null)
                {
                    var title = request.Title.Trim();
                    if (title.Length == 0)
                        throw new ArgumentException("Finding title cannot be empty.", nameof(request.Title));
                    finding.Title = title.Length > 200 ? title[..200] : title;
                    finding.Status = IngestionFindingStatus.Edited;
                }
                if (request.Detail != null)
                {
                    finding.Detail = CleanNullable(request.Detail, 2000);
                    finding.Status = IngestionFindingStatus.Edited;
                }
                if (request.SubjectEmail != null)
                    finding.SubjectEmail = CleanNullable(request.SubjectEmail, 100);
                if (request.EventDate != null)
                    finding.EventDate = request.EventDate.Value.ToUniversalTime();

                if (request.SubjectPersonId != null)
                {
                    var person = await _persons.GetPersonById(request.SubjectPersonId);
                    if (person == null || person.ApplicationUserId != ownerId)
                        throw new KeyNotFoundException($"No person found with id '{request.SubjectPersonId}'.");
                    finding.SubjectPersonId = request.SubjectPersonId;
                    finding.SubjectIsNew = false;
                    finding.SubjectName = person.Name;
                    finding.ConflictType = FindingConflictType.None;
                    finding.ExistingValue = null;
                    finding.ProposalAction = FindingProposalAction.Add;
                    await AnalyzeConflictAsync(ownerId, finding);
                    if (finding.Status != IngestionFindingStatus.Edited)
                        finding.Status = IngestionFindingStatus.Pending;
                }
                else if (request.SubjectIsNew)
                {
                    finding.SubjectPersonId = null;
                    finding.SubjectIsNew = true;
                    if (!string.IsNullOrWhiteSpace(request.SubjectName))
                        finding.SubjectName = request.SubjectName.Trim()[..Math.Min(100, request.SubjectName.Trim().Length)];
                    finding.ConflictType = FindingConflictType.None;
                    finding.ExistingValue = null;
                    if (finding.Status != IngestionFindingStatus.Edited)
                        finding.Status = IngestionFindingStatus.Pending;
                }

                // Approved marks intent; the Approve* endpoints do the durable
                // write. Edited (title/detail changed) also counts as reviewed.
                if (request.Status == IngestionFindingStatus.Approved)
                    finding.Status = finding.Status == IngestionFindingStatus.Edited
                        ? IngestionFindingStatus.Edited
                        : IngestionFindingStatus.Approved;
                else if (request.Status == IngestionFindingStatus.Rejected)
                    finding.Status = IngestionFindingStatus.Rejected;
                else if (request.Status == IngestionFindingStatus.Pending && finding.Status != IngestionFindingStatus.Edited)
                    finding.Status = IngestionFindingStatus.Pending;

                finding.ReviewNote = CleanNullable(request.ReviewNote, 500);
                finding.ReviewedById = ownerId;
                finding.ReviewedAtUtc = DateTime.UtcNow;
                finding.UpdatedAtUtc = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
                return await ToFindingDtoAsync(finding);
            }
        }

        public async Task<IngestionApplyResult> ApproveFindingAsync(Guid findingId)
        {
            var ownerId = OwnerId();
            var finding = await _batches.GetFindingAsync(ownerId, findingId);
            if (finding == null)
                throw new KeyNotFoundException($"No finding found with id '{findingId}'.");
            var result = new IngestionApplyResult();
            await ApplyFindingAsync(ownerId, finding, new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase), result);
            await RefreshBatchStatusAsync(ownerId, finding.IngestionBatchId);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        public async Task<IngestionApplyResult> ApprovePersonAsync(Guid batchId, Guid? personId, string? personName, bool isNew)
        {
            var ownerId = OwnerId();
            var batch = await RequireBatchAsync(ownerId, batchId);
            var findings = batch.Findings
                .Where(f => f.Status == IngestionFindingStatus.Approved || f.Status == IngestionFindingStatus.Edited)
                .Where(f => isNew
                    ? f.SubjectIsNew && string.Equals(f.SubjectName, personName, StringComparison.OrdinalIgnoreCase)
                    : f.SubjectPersonId == personId)
                .ToList();
            var result = new IngestionApplyResult();
            var created = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var finding in findings)
                await ApplyFindingAsync(ownerId, finding, created, result);
            await RefreshBatchStatusAsync(ownerId, batchId);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        public async Task<IngestionApplyResult> ApproveBatchAsync(Guid batchId)
        {
            var ownerId = OwnerId();
            var batch = await RequireBatchAsync(ownerId, batchId);
            var findings = batch.Findings
                .Where(f => f.Status == IngestionFindingStatus.Approved || f.Status == IngestionFindingStatus.Edited)
                .ToList();
            var result = new IngestionApplyResult();
            var created = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var finding in findings)
                await ApplyFindingAsync(ownerId, finding, created, result);
            await RefreshBatchStatusAsync(ownerId, batchId);
            await _unitOfWork.SaveChangesAsync();
            return result;
        }

        private async Task RefreshBatchStatusAsync(Guid ownerId, Guid batchId)
        {
            var batch = await RequireBatchAsync(ownerId, batchId);
            var open = batch.Findings.Any(f =>
                f.Status == IngestionFindingStatus.Pending || f.Status == IngestionFindingStatus.Unresolved);
            var applied = batch.Findings.Any(f => f.Status == IngestionFindingStatus.Approved);
            batch.Status = !open && applied ? IngestionStatus.Applied
                : applied ? IngestionStatus.PartiallyApplied
                : batch.Status;
            batch.UpdatedAtUtc = DateTime.UtcNow;
        }

        private async Task ApplyFindingAsync(
            Guid ownerId, IngestionFinding finding,
            Dictionary<string, Guid> createdPeople, IngestionApplyResult result)
        {
            if (finding.Status != IngestionFindingStatus.Approved && finding.Status != IngestionFindingStatus.Edited)
            {
                result.SkippedCount++;
                result.Skipped.Add($"{finding.Title}: not approved.");
                return;
            }
            if (finding.Confidence == FindingConfidence.Unresolved
                || finding.Confidence == FindingConfidence.Contradictory
                || finding.Status == IngestionFindingStatus.Unresolved)
            {
                result.SkippedCount++;
                result.Skipped.Add($"{finding.Title}: uncertain — resolve it first.");
                return;
            }

            var personId = finding.SubjectPersonId;
            if (personId == null && finding.SubjectIsNew)
                personId = await EnsureNewPersonAsync(ownerId, finding, createdPeople, result);
            if (personId == null)
            {
                result.SkippedCount++;
                result.Skipped.Add($"{finding.Title}: no resolved person.");
                return;
            }

            var person = await _persons.GetPersonById(personId);
            if (person == null || person.ApplicationUserId != ownerId)
            {
                result.SkippedCount++;
                result.Skipped.Add($"{finding.Title}: person is no longer available.");
                return;
            }

            if (finding.MemoryKind != null)
            {
                await ApplyMemoryFindingAsync(ownerId, personId.Value, finding, result);
                return;
            }
            if (!string.IsNullOrWhiteSpace(finding.TargetField))
            {
                await ApplyFieldFindingAsync(ownerId, person, finding, result);
                return;
            }
            if (!string.IsNullOrWhiteSpace(finding.EventKind))
            {
                await ApplyEventFindingAsync(ownerId, personId.Value, finding, result);
                return;
            }
            if (!string.IsNullOrWhiteSpace(finding.RelationKind))
            {
                await ApplyRelationFindingAsync(ownerId, personId.Value, finding, result);
                return;
            }
            result.SkippedCount++;
            result.Skipped.Add($"{finding.Title}: no supported target slot.");
        }

        private async Task<Guid?> EnsureNewPersonAsync(
            Guid ownerId, IngestionFinding finding,
            Dictionary<string, Guid> createdPeople, IngestionApplyResult result)
        {
            var key = (finding.SubjectName ?? string.Empty).Trim().ToLowerInvariant();
            if (createdPeople.TryGetValue(key, out var existing))
            {
                finding.SubjectPersonId = existing;
                finding.SubjectIsNew = false;
                finding.AppliedPersonId = existing;
                return existing;
            }
            if (string.IsNullOrWhiteSpace(finding.SubjectName))
            {
                result.SkippedCount++;
                result.Skipped.Add("New person needs a name.");
                return null;
            }
            if (string.IsNullOrWhiteSpace(finding.SubjectEmail))
            {
                result.SkippedCount++;
                result.Skipped.Add($"{finding.SubjectName}: add an email before creating this contact.");
                return null;
            }
            var created = await _quickAdd.QuickAddPerson(new ServiceContracts.DTOs.PersonQuickAddRequest
            // namespace: ServiceContracts.DTOs (verified)
            {
                Name = finding.SubjectName.Trim(),
                email = finding.SubjectEmail.Trim(),
                Origin = $"Added from ingestion on {DateTime.UtcNow:yyyy-MM-dd}."
            });
            finding.SubjectPersonId = created.PersonId;
            finding.SubjectIsNew = false;
            finding.AppliedPersonId = created.PersonId;
            createdPeople[key] = created.PersonId;
            result.AppliedCount++;
            result.Applied.Add($"Created contact {created.Name}.");
            _logger.LogInformation("Ingestion created person {PersonId} from finding {FindingId}", created.PersonId, finding.IngestionFindingId);
            return created.PersonId;
        }

        private async Task ApplyMemoryFindingAsync(Guid ownerId, Guid personId, IngestionFinding finding, IngestionApplyResult result)
        {
            var active = (await _memory.ListForPersonAsync(personId))
                .Where(e => e.Status == 0 && e.Kind == finding.MemoryKind)
                .ToList();
            if (active.Any(e => Normalize(e.Title) == Normalize(finding.Title)))
            {
                finding.ConflictType = FindingConflictType.Duplicate;
                result.SkippedCount++;
                result.Skipped.Add($"{finding.Title}: already recorded.");
                return;
            }
            if (finding.ConflictType == FindingConflictType.Update || finding.ProposalAction == FindingProposalAction.Update)
            {
                var superseded = active
                    .OrderByDescending(e => e.UpdatedAtUtc)
                    .FirstOrDefault(e => finding.ExistingValue == null || Normalize(e.Title) == Normalize(finding.ExistingValue.Split('—')[0]));
                if (superseded != null)
                {
                    var full = await _memoryRepo.GetAsync(ownerId, superseded.MemoryEntryId);
                    if (full != null)
                    {
                        full.Status = MemoryEntryStatus.Done;
                        full.CorrectionNote = $"Superseded by approved update on {DateTime.UtcNow:yyyy-MM-dd}.";
                        full.UpdatedAtUtc = DateTime.UtcNow;
                    }
                }
            }
            var now = DateTime.UtcNow;
            var entry = new RelationshipMemoryEntry
            {
                MemoryEntryId = Guid.NewGuid(),
                ApplicationUserId = ownerId,
                PersonId = personId,
                Kind = finding.MemoryKind!.Value,
                Title = finding.Title,
                Detail = finding.Detail,
                Status = MemoryEntryStatus.Active,
                Provenance = MemoryProvenance.IngestionDerived,
                SourceExcerpt = finding.SourceExcerpt,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            await _memoryRepo.AddAsync(entry);
            finding.Status = IngestionFindingStatus.Approved;
            finding.AppliedMemoryEntryId = entry.MemoryEntryId;
            finding.ReviewedById = ownerId;
            finding.ReviewedAtUtc = now;
            finding.UpdatedAtUtc = now;
            result.AppliedCount++;
            result.Applied.Add($"{finding.Title}.");
        }

        private async Task ApplyFieldFindingAsync(Guid ownerId, Person person, IngestionFinding finding, IngestionApplyResult result)
        {
            var field = (finding.TargetField ?? string.Empty).ToLowerInvariant();
            if (field is not ("role" or "organization"))
            {
                var current = ReadPersonField(person, field);
                if (current != null && Normalize(current) == Normalize(finding.Title))
                {
                    result.SkippedCount++;
                    result.Skipped.Add($"{finding.Title}: already recorded.");
                    return;
                }
            }

            // PersonUpdaterService validates the full DTO ([Required] Name,
            // Email), so unchanged values must ride along with the partial
            // update. Only the targeted slot changes; the rest is preserved.
            var request = new ServiceContracts.DTOs.PersonUpdateRequest
            {
                PersonId = person.PersonId,
                Name = person.Name,
                email = person.email
            };
            switch (field)
            {
                case "role":
                    var roles = person.ContactItemRoles.Select(r => r.Role).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
                    if (roles.Any(r => string.Equals(r.Trim(), finding.Title.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        result.SkippedCount++;
                        result.Skipped.Add($"{finding.Title}: already recorded.");
                        return;
                    }
                    roles.Add(finding.Title.Trim());
                    request.CurrentRoles = roles;
                    break;
                case "organization":
                    var orgs = person.Circles.Select(c => c.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    if (orgs.Any(o => string.Equals(o.Trim(), finding.Title.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        result.SkippedCount++;
                        result.Skipped.Add($"{finding.Title}: already recorded.");
                        return;
                    }
                    orgs.Add(finding.Title.Trim());
                    request.Organizations = orgs;
                    break;
                case "location": request.Address = finding.Title.Trim(); break;
                case "email": request.email = finding.Title.Trim(); break;
                case "phone": request.phone = finding.Title.Trim(); break;
                case "origin": request.Origin = finding.Title.Trim(); break;
                case "context": request.ContextMemory = finding.Title.Trim(); break;
                default:
                    result.SkippedCount++;
                    result.Skipped.Add($"{finding.Title}: unsupported field.");
                    return;
            }
            await _updater.UpdatePerson(request);
            finding.Status = IngestionFindingStatus.Approved;
            finding.ReviewedById = ownerId;
            finding.ReviewedAtUtc = DateTime.UtcNow;
            finding.UpdatedAtUtc = DateTime.UtcNow;
            result.AppliedCount++;
            result.Applied.Add($"{finding.Title}.");
        }

        private async Task ApplyEventFindingAsync(Guid ownerId, Guid personId, IngestionFinding finding, IngestionApplyResult result)
        {
            if (!Enum.TryParse<RelationshipEventType>(finding.EventKind, true, out var type))
            {
                result.SkippedCount++;
                result.Skipped.Add($"{finding.Title}: unsupported event kind.");
                return;
            }
            if (finding.EventDate == null)
            {
                result.SkippedCount++;
                result.Skipped.Add($"{finding.Title}: confirm a date first.");
                return;
            }
            await _events.CreateAsync(new ServiceContracts.DTOs.EventDTOs.EventCreateRequest
            {
                PersonId = personId,
                Type = type,
                Title = finding.Title,
                OccursOn = DateOnly.FromDateTime(finding.EventDate.Value),
                RepeatsYearly = false,
                Importance = 2,
                Notes = finding.Detail
            });
            finding.Status = IngestionFindingStatus.Approved;
            finding.ReviewedById = ownerId;
            finding.ReviewedAtUtc = DateTime.UtcNow;
            finding.UpdatedAtUtc = DateTime.UtcNow;
            result.AppliedCount++;
            result.Applied.Add($"{finding.Title}.");
        }

        private async Task ApplyRelationFindingAsync(Guid ownerId, Guid personId, IngestionFinding finding, IngestionApplyResult result)
        {
            var target = finding.ObjectName ?? finding.ObjectOrg ?? "them";
            var now = DateTime.UtcNow;
            var entry = new RelationshipMemoryEntry
            {
                MemoryEntryId = Guid.NewGuid(),
                ApplicationUserId = ownerId,
                PersonId = personId,
                Kind = finding.ObjectOrg != null
                    ? RelationshipMemoryKind.SharedProject
                    : RelationshipMemoryKind.Fact,
                Title = $"{finding.Title} ({target})",
                Detail = finding.Detail,
                Status = MemoryEntryStatus.Active,
                Provenance = MemoryProvenance.IngestionDerived,
                SourceExcerpt = finding.SourceExcerpt,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            await _memoryRepo.AddAsync(entry);
            finding.Status = IngestionFindingStatus.Approved;
            finding.AppliedMemoryEntryId = entry.MemoryEntryId;
            finding.ReviewedById = ownerId;
            finding.ReviewedAtUtc = now;
            finding.UpdatedAtUtc = now;
            result.AppliedCount++;
            result.Applied.Add($"{finding.Title}.");
        }

        private static IngestionBatchDto ToSummaryDto(IngestionBatch batch) => new()
        {
            IngestionBatchId = batch.IngestionBatchId,
            SourceType = batch.SourceType,
            SourceMeetingId = batch.SourceMeetingId,
            Status = batch.Status.ToString(),
            IsNoOp = batch.IsNoOp,
            NoOpReason = batch.NoOpReason,
            FindingCount = batch.Findings.Count,
            PendingCount = batch.Findings.Count(f =>
                f.Status == IngestionFindingStatus.Pending || f.Status == IngestionFindingStatus.Unresolved),
            CreatedAtUtc = batch.CreatedAtUtc
        };

        private async Task<IngestionBatchDto> ToDtoAsync(IngestionBatch batch)
        {
            var dto = ToSummaryDto(batch);
            var directory = (await _persons.GetAllPersons()).Where(p => p != null).Select(p => p!).ToList();
            var names = directory.ToDictionary(p => p.PersonId, p => p.Name);
            dto.Findings = batch.Findings.Select(f => ToFindingDto(f, directory, names)).ToList();
            return dto;
        }

        private async Task<IngestionFindingDto> ToFindingDtoAsync(IngestionFinding finding)
        {
            var directory = (await _persons.GetAllPersons()).Where(p => p != null).Select(p => p!).ToList();
            var names = directory.ToDictionary(p => p.PersonId, p => p.Name);
            return ToFindingDto(finding, directory, names);
        }

        private static IngestionFindingDto ToFindingDto(
            IngestionFinding finding, List<Person> directory, Dictionary<Guid, string?> names)
        {
            var dto = new IngestionFindingDto
            {
                IngestionFindingId = finding.IngestionFindingId,
                IngestionBatchId = finding.IngestionBatchId,
                SubjectPersonId = finding.SubjectPersonId,
                SubjectPersonName = finding.SubjectPersonId != null && names.TryGetValue(finding.SubjectPersonId.Value, out var name)
                    ? name
                    : null,
                SubjectIsNew = finding.SubjectIsNew,
                SubjectName = finding.SubjectName,
                ObjectName = finding.ObjectName,
                ObjectOrg = finding.ObjectOrg,
                RelationKind = finding.RelationKind,
                TargetField = finding.TargetField,
                MemoryKind = finding.MemoryKind?.ToString(),
                EventKind = finding.EventKind,
                Title = finding.Title,
                Detail = finding.Detail,
                SourceExcerpt = finding.SourceExcerpt,
                Confidence = finding.Confidence.ToString(),
                UncertaintyReason = finding.UncertaintyReason,
                ConflictType = finding.ConflictType.ToString(),
                ExistingValue = finding.ExistingValue,
                ProposalAction = finding.ProposalAction.ToString(),
                Status = finding.Status.ToString()
            };
            if (finding.SubjectPersonId == null && !finding.SubjectIsNew && !string.IsNullOrWhiteSpace(finding.SubjectName))
            {
                dto.Candidates = directory
                    .Where(p => (p.Name ?? string.Empty).Contains(finding.SubjectName!, StringComparison.OrdinalIgnoreCase))
                    .Take(5)
                    .Select(p => new ResolutionCandidateDto
                    {
                        PersonId = p.PersonId,
                        Name = p.Name ?? "contact",
                        Organization = p.Circles.FirstOrDefault()?.Name,
                        Role = p.ContactItemRoles.FirstOrDefault()?.Role,
                        Evidence = "Matched by: " + string.Join(" +", new[]
                        {
                            "name",
                            p.Circles.FirstOrDefault()?.Name,
                            p.ContactItemRoles.FirstOrDefault()?.Role
                        }.Where(s => !string.IsNullOrWhiteSpace(s)))
                    })
                    .ToList();
            }
            return dto;
        }
    }
}
