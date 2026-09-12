using Entities;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs.EventDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Servicess
{
    public class EventService : IEventService
    {
        private readonly RelationshipEventRepositoryContract _events;
        private readonly PersonRepositryContract _persons;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<EventService> _logger;

        public EventService(
            RelationshipEventRepositoryContract events,
            PersonRepositryContract persons,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            ILogger<EventService> logger)
        {
            _events = events;
            _persons = persons;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        private Guid OwnerId()
        {
            var id = _currentUser.UserId;
            if (id == null || id == Guid.Empty)
                throw new UnauthorizedAccessException("Cannot manage events without an authenticated user.");
            return id.Value;
        }

        private async Task<Person> RequireOwnedPersonAsync(Guid ownerId, Guid personId)
        {
            var person = await _persons.GetPersonById(personId);
            if (person == null || person.ApplicationUserId != ownerId)
                throw new KeyNotFoundException($"No person found with id '{personId}'.");
            return person;
        }

        public async Task<List<EventResponse>> ListForPersonAsync(Guid personId)
        {
            using (Operation.Time("List relationship events"))
            {
                var ownerId = OwnerId();
                var person = await RequireOwnedPersonAsync(ownerId, personId);
                var events = await _events.ListForPersonAsync(ownerId, personId);
                return events.Select(e => EventResponse.FromEvent(e, person.Name)).ToList();
            }
        }

        public async Task<List<EventOccurrenceDto>> GetUpcomingAsync(int days)
        {
            using (Operation.Time("Get upcoming event occurrences"))
            {
                if (days < 1) days = 1;
                if (days > 366) days = 366;

                var ownerId = OwnerId();
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var events = await _events.ListForOwnerAsync(ownerId);

                var occurrences = new List<EventOccurrenceDto>();
                foreach (var relationshipEvent in events)
                {
                    var occurrence = NextOccurrence(relationshipEvent, today);
                    if (occurrence == null) continue;

                    var inDays = occurrence.Value.DayNumber - today.DayNumber;
                    if (inDays < 0 || inDays > days) continue;

                    occurrences.Add(new EventOccurrenceDto
                    {
                        EventId = relationshipEvent.EventId,
                        PersonId = relationshipEvent.PersonId,
                        Type = relationshipEvent.Type,
                        Title = relationshipEvent.Title,
                        OccurrenceDate = occurrence.Value,
                        InDays = inDays,
                        Importance = relationshipEvent.Importance
                    });
                }

                var personIds = occurrences.Select(o => o.PersonId).Distinct().ToHashSet();
                var names = new Dictionary<Guid, string?>();
                foreach (var personId in personIds)
                {
                    var person = await _persons.GetPersonById(personId);
                    names[personId] = person?.Name;
                }
                foreach (var occurrence in occurrences)
                    occurrence.PersonName = names.TryGetValue(occurrence.PersonId, out var name) ? name : null;

                return occurrences
                    .OrderBy(o => o.InDays)
                    .ThenByDescending(o => o.Importance)
                    .ToList();
            }
        }

        /// <summary>
        /// Next occurrence on or after today. Yearly events roll to the next year
        /// (Feb 29 lands on Feb 28 in non-leap years). One-off events occur once.
        /// </summary>
        public static DateOnly? NextOccurrence(RelationshipEvent relationshipEvent, DateOnly today)
        {
            if (!relationshipEvent.RepeatsYearly)
                return relationshipEvent.OccursOn >= today ? relationshipEvent.OccursOn : null;

            var year = today.Year;
            var candidate = SafeDate(year, relationshipEvent.OccursOn.Month, relationshipEvent.OccursOn.Day);
            if (candidate < today)
                candidate = SafeDate(year + 1, relationshipEvent.OccursOn.Month, relationshipEvent.OccursOn.Day);
            return candidate;
        }

        private static DateOnly SafeDate(int year, int month, int day)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            return new DateOnly(year, month, Math.Min(day, daysInMonth));
        }

        public async Task<EventResponse> CreateAsync(EventCreateRequest request)
        {
            using (Operation.Time("Create relationship event"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var title = (request.Title ?? string.Empty).Trim();
                if (title.Length == 0)
                    throw new ArgumentException("Event title is required.", nameof(request.Title));
                if (title.Length > 200)
                    throw new ArgumentException("Event title cannot exceed 200 characters.", nameof(request.Title));
                if (request.Importance < 1 || request.Importance > 3)
                    throw new ArgumentException("Importance must be 1, 2, or 3.", nameof(request.Importance));

                var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
                if (notes?.Length > 1000)
                    throw new ArgumentException("Event notes cannot exceed 1000 characters.", nameof(request.Notes));

                var ownerId = OwnerId();
                var person = await RequireOwnedPersonAsync(ownerId, request.PersonId);

                var now = DateTime.UtcNow;
                var relationshipEvent = new RelationshipEvent
                {
                    EventId = Guid.NewGuid(),
                    ApplicationUserId = ownerId,
                    PersonId = request.PersonId,
                    Type = request.Type,
                    Title = title,
                    OccursOn = request.OccursOn,
                    RepeatsYearly = request.RepeatsYearly,
                    Importance = request.Importance,
                    Notes = notes,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };

                await _events.AddAsync(relationshipEvent);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created event {EventId} for person {PersonId}", relationshipEvent.EventId, relationshipEvent.PersonId);
                return EventResponse.FromEvent(relationshipEvent, person.Name);
            }
        }

        public async Task<EventResponse> UpdateAsync(EventUpdateRequest request)
        {
            using (Operation.Time("Update relationship event"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var title = (request.Title ?? string.Empty).Trim();
                if (title.Length == 0)
                    throw new ArgumentException("Event title is required.", nameof(request.Title));
                if (title.Length > 200)
                    throw new ArgumentException("Event title cannot exceed 200 characters.", nameof(request.Title));
                if (request.Importance < 1 || request.Importance > 3)
                    throw new ArgumentException("Importance must be 1, 2, or 3.", nameof(request.Importance));

                var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
                if (notes?.Length > 1000)
                    throw new ArgumentException("Event notes cannot exceed 1000 characters.", nameof(request.Notes));

                var ownerId = OwnerId();
                var relationshipEvent = await _events.GetAsync(ownerId, request.EventId);
                if (relationshipEvent == null)
                    throw new KeyNotFoundException($"No event found with id '{request.EventId}'.");

                relationshipEvent.Type = request.Type;
                relationshipEvent.Title = title;
                relationshipEvent.OccursOn = request.OccursOn;
                relationshipEvent.RepeatsYearly = request.RepeatsYearly;
                relationshipEvent.Importance = request.Importance;
                relationshipEvent.Notes = notes;
                relationshipEvent.UpdatedAtUtc = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                var person = await _persons.GetPersonById(relationshipEvent.PersonId);
                return EventResponse.FromEvent(relationshipEvent, person?.Name);
            }
        }

        public async Task DeleteAsync(Guid eventId)
        {
            using (Operation.Time("Delete relationship event"))
            {
                var ownerId = OwnerId();
                var relationshipEvent = await _events.GetAsync(ownerId, eventId);
                if (relationshipEvent == null)
                    throw new KeyNotFoundException($"No event found with id '{eventId}'.");

                await _events.RemoveAsync(relationshipEvent);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deleted event {EventId}", eventId);
            }
        }
    }
}
