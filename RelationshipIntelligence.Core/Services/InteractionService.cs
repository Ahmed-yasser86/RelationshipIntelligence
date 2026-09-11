using ContactsManger.Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs;
using Servicess.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Servicess
{
    public class InteractionService : IInteractionService
    {
        private readonly InteractionRepositoryContract _interactions;
        private readonly PersonRepositryContract _persons;
        private readonly IRelationshipScoringService _scoring;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InteractionService> _logger;

        public InteractionService(
            InteractionRepositoryContract interactions,
            PersonRepositryContract persons,
            IRelationshipScoringService scoring,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            ILogger<InteractionService> logger)
        {
            _interactions = interactions;
            _persons = persons;
            _scoring = scoring;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<InteractionResponse> LogAsync(InteractionAddRequest? request)
        {
            using (Operation.Time("Log interaction"))
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                ValidationHelpers.ValidationFunction(request);

                if (_currentUser.UserId == null)
                    throw new UnauthorizedAccessException("Cannot log an interaction without an authenticated user.");

                if (request.TimeOfInteraction!.Value.ToUniversalTime() > DateTime.UtcNow.AddDays(1))
                    throw new ValidationException("Interaction date cannot be in the future.");

                var person = await _persons.GetPersonById(request.PersonId);
                if (person == null)
                    throw new ArgumentException("Given person ID does not exist.");

                var saved = await _interactions.AddAsync(request.ToInteraction());
                await _unitOfWork.SaveChangesAsync();
                await _scoring.RecomputeForPairAsync(request.PersonId);
                return saved.ConvertToDto();
            }
        }

        public async Task<List<InteractionResponse>> ListForPersonAsync(Guid? personId)
        {
            var person = await _persons.GetPersonById(personId);
            if (person == null)
                return new List<InteractionResponse>();

            var rows = await _interactions.ListForPersonAsync(person.PersonId);
            return rows.ConvertToDtos();
        }

        public async Task<int> ImportCsvAsync(Guid? personId, string? csvText)
        {
            if (_currentUser.UserId == null)
                throw new UnauthorizedAccessException("Cannot import interactions without an authenticated user.");

            var person = await _persons.GetPersonById(personId);
            if (person == null)
                throw new ArgumentException("Given person ID does not exist.");

            var rows = InteractionCsvParser.Parse(csvText);
            foreach (var row in rows)
            {
                await _interactions.AddAsync(new Interaction
                {
                    InteractionId = Guid.NewGuid(),
                    PersonId = person.PersonId,
                    TimeOfInteraction = row.TimeOfInteraction,
                    InteractionType = row.InteractionType,
                    InteractionTitle = row.InteractionTitle,
                    InteractionDescription = row.InteractionDescription
                });
            }
            await _unitOfWork.SaveChangesAsync();
            await _scoring.RecomputeForPairAsync(person.PersonId);

            return rows.Count;
        }
    }
}
