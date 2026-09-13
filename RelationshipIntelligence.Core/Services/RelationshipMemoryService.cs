using Entities;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs.MemoryDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Servicess
{
    public class RelationshipMemoryService : IRelationshipMemoryService
    {
        private readonly RelationshipMemoryRepositoryContract _entries;
        private readonly PersonRepositryContract _persons;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<RelationshipMemoryService> _logger;

        public RelationshipMemoryService(
            RelationshipMemoryRepositoryContract entries,
            PersonRepositryContract persons,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            ILogger<RelationshipMemoryService> logger)
        {
            _entries = entries;
            _persons = persons;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        private Guid OwnerId()
        {
            var id = _currentUser.UserId;
            if (id == null || id == Guid.Empty)
                throw new UnauthorizedAccessException("Cannot manage relationship memory without an authenticated user.");
            return id.Value;
        }

        private async Task<Person> RequireOwnedPersonAsync(Guid ownerId, Guid personId)
        {
            var person = await _persons.GetPersonById(personId);
            if (person == null || person.ApplicationUserId != ownerId)
                throw new KeyNotFoundException($"No person found with id '{personId}'.");
            return person;
        }

        public async Task<List<MemoryEntryResponse>> ListForPersonAsync(Guid personId)
        {
            using (Operation.Time("List relationship memory"))
            {
                var ownerId = OwnerId();
                await RequireOwnedPersonAsync(ownerId, personId);
                var entries = await _entries.ListForPersonAsync(ownerId, personId);
                return entries.Select(MemoryEntryResponse.FromEntry).ToList();
            }
        }

        public async Task<MemoryEntryResponse> CreateAsync(MemoryEntryCreateRequest request)
        {
            using (Operation.Time("Create relationship memory entry"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var title = (request.Title ?? string.Empty).Trim();
                if (title.Length == 0)
                    throw new ArgumentException("Memory entry title is required.", nameof(request.Title));
                if (title.Length > 200)
                    throw new ArgumentException("Memory entry title cannot exceed 200 characters.", nameof(request.Title));

                var detail = string.IsNullOrWhiteSpace(request.Detail) ? null : request.Detail.Trim();
                if (detail?.Length > 2000)
                    throw new ArgumentException("Memory entry detail cannot exceed 2000 characters.", nameof(request.Detail));

                var ownerId = OwnerId();
                await RequireOwnedPersonAsync(ownerId, request.PersonId);

                var now = DateTime.UtcNow;
                var entry = new RelationshipMemoryEntry
                {
                    MemoryEntryId = Guid.NewGuid(),
                    ApplicationUserId = ownerId,
                    PersonId = request.PersonId,
                    Kind = request.Kind,
                    Title = title,
                    Detail = detail,
                    Status = MemoryEntryStatus.Active,
                    Provenance = MemoryProvenance.User,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };

                await _entries.AddAsync(entry);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created memory entry {EntryId} for person {PersonId}", entry.MemoryEntryId, entry.PersonId);
                return MemoryEntryResponse.FromEntry(entry);
            }
        }

        public async Task<MemoryEntryResponse> SuggestEntryAsync(Guid personId, RelationshipMemoryKind kind, string title, string? detail)
        {
            using (Operation.Time("Suggest relationship memory entry"))
            {
                var cleanTitle = (title ?? string.Empty).Trim();
                if (cleanTitle.Length == 0 || cleanTitle.Length > 200)
                    throw new ArgumentException("Suggested title must be 1-200 characters.", nameof(title));

                var cleanDetail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim();
                if (cleanDetail?.Length > 2000)
                    throw new ArgumentException("Suggested detail cannot exceed 2000 characters.", nameof(detail));

                var ownerId = OwnerId();
                await RequireOwnedPersonAsync(ownerId, personId);

                var existing = await _entries.ListForPersonAsync(ownerId, personId);
                if (existing.Any(e => e.Status == 0 && Normalize(e.Title) == Normalize(cleanTitle)))
                    throw new InvalidOperationException("An active entry with the same title already exists.");

                var now = DateTime.UtcNow;
                var entry = new RelationshipMemoryEntry
                {
                    MemoryEntryId = Guid.NewGuid(),
                    ApplicationUserId = ownerId,
                    PersonId = personId,
                    Kind = kind,
                    Title = cleanTitle,
                    Detail = cleanDetail,
                    Status = MemoryEntryStatus.Active,
                    Provenance = MemoryProvenance.AiSuggested,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };

                await _entries.AddAsync(entry);
                await _unitOfWork.SaveChangesAsync();
                return MemoryEntryResponse.FromEntry(entry);
            }
        }

        private static string Normalize(string value) =>
            new string((value ?? string.Empty).Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        public async Task<List<MemoryEntryResponse>> ProposeGapsAsync(Guid personId)
        {
            using (Operation.Time("Propose relationship memory gaps"))
            {
                var ownerId = OwnerId();
                await RequireOwnedPersonAsync(ownerId, personId);

                var active = (await _entries.ListForPersonAsync(ownerId, personId))
                    .Where(e => e.Status == 0)
                    .ToList();
                var proposed = new List<MemoryEntryResponse>();

                if (!active.Any(e => e.Kind == RelationshipMemoryKind.Intent || e.Kind == RelationshipMemoryKind.Goal))
                {
                    proposed.Add(await SuggestEntryAsync(personId, RelationshipMemoryKind.Intent,
                        "Define what this relationship is for",
                        "No intent or goal is recorded. Stating one focuses future plans and outreach."));
                }

                if (!active.Any(e => e.Kind == RelationshipMemoryKind.Topic))
                {
                    proposed.Add(await SuggestEntryAsync(personId, RelationshipMemoryKind.Topic,
                        "Capture shared topics",
                        "No recurring topics are recorded. Noting what you usually discuss sharpens briefings and drafts."));
                }

                return proposed;
            }
        }

        public async Task<MemoryEntryResponse> UpdateAsync(MemoryEntryUpdateRequest request)
        {
            using (Operation.Time("Update relationship memory entry"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var title = (request.Title ?? string.Empty).Trim();
                if (title.Length == 0)
                    throw new ArgumentException("Memory entry title is required.", nameof(request.Title));
                if (title.Length > 200)
                    throw new ArgumentException("Memory entry title cannot exceed 200 characters.", nameof(request.Title));

                var detail = string.IsNullOrWhiteSpace(request.Detail) ? null : request.Detail.Trim();
                if (detail?.Length > 2000)
                    throw new ArgumentException("Memory entry detail cannot exceed 2000 characters.", nameof(request.Detail));

                var ownerId = OwnerId();
                var entry = await _entries.GetAsync(ownerId, request.MemoryEntryId);
                if (entry == null)
                    throw new KeyNotFoundException($"No memory entry found with id '{request.MemoryEntryId}'.");

                // A user edit is authoritative: whatever the provenance was, the entry
                // now carries user-confirmed content. Correction metadata preserves the trail.
                var wasAiDerived = entry.Provenance is MemoryProvenance.AiSuggested or MemoryProvenance.MeetingDerived;
                entry.Kind = request.Kind;
                entry.Title = title;
                entry.Detail = detail;
                entry.Status = request.Status;
                entry.Provenance = MemoryProvenance.User;
                entry.UpdatedAtUtc = DateTime.UtcNow;
                if (wasAiDerived || !string.IsNullOrWhiteSpace(request.CorrectionNote))
                {
                    entry.CorrectedAtUtc = DateTime.UtcNow;
                    entry.CorrectionNote = string.IsNullOrWhiteSpace(request.CorrectionNote)
                        ? null
                        : request.CorrectionNote.Trim();
                }

                await _unitOfWork.SaveChangesAsync();
                return MemoryEntryResponse.FromEntry(entry);
            }
        }

        public async Task DeleteAsync(Guid entryId)
        {
            using (Operation.Time("Delete relationship memory entry"))
            {
                var ownerId = OwnerId();
                var entry = await _entries.GetAsync(ownerId, entryId);
                if (entry == null)
                    throw new KeyNotFoundException($"No memory entry found with id '{entryId}'.");

                // Permanent deletion: no resurrection path. Later AI processing must not
                // recreate this fact from unsupported assumptions (enforced by dedupe rules).
                await _entries.RemoveAsync(entry);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deleted memory entry {EntryId}", entryId);
            }
        }

        public async Task<MemoryEntryResponse> AcceptSuggestionAsync(Guid entryId, string? correctionNote = null)
        {
            using (Operation.Time("Accept memory suggestion"))
            {
                var ownerId = OwnerId();
                var entry = await _entries.GetAsync(ownerId, entryId);
                if (entry == null)
                    throw new KeyNotFoundException($"No memory entry found with id '{entryId}'.");
                if (entry.Provenance != MemoryProvenance.AiSuggested)
                    throw new InvalidOperationException("Only AI-suggested entries can be accepted.");

                entry.Provenance = MemoryProvenance.AiConfirmed;
                entry.Status = MemoryEntryStatus.Active;
                entry.UpdatedAtUtc = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(correctionNote))
                {
                    entry.CorrectedAtUtc = DateTime.UtcNow;
                    entry.CorrectionNote = correctionNote.Trim();
                }

                await _unitOfWork.SaveChangesAsync();
                return MemoryEntryResponse.FromEntry(entry);
            }
        }

        public async Task RejectSuggestionAsync(Guid entryId)
        {
            using (Operation.Time("Reject memory suggestion"))
            {
                var ownerId = OwnerId();
                var entry = await _entries.GetAsync(ownerId, entryId);
                if (entry == null)
                    throw new KeyNotFoundException($"No memory entry found with id '{entryId}'.");
                if (entry.Provenance != MemoryProvenance.AiSuggested)
                    throw new InvalidOperationException("Only AI-suggested entries can be rejected.");

                await _entries.RemoveAsync(entry);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
