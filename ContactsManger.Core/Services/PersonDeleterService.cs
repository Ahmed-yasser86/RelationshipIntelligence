using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceContracts;
using RepositryContracts;
using SerilogTimings;

namespace Servicess
{


    //when i remove person i have to remove all the related data like contact items and roles and circles (if not linked to any enitity )
    public class PersonDeleterService : IPersonDeleterService
    {
        private readonly PersonRepositryContract PersonRipository;
        private readonly ILogger<PersonDeleterService> _logger;

        public PersonDeleterService(PersonRepositryContract personRipository, ILogger<PersonDeleterService> logger)
        {
            PersonRipository = personRipository;
            _logger = logger;
        }

        public async Task<bool> DeletePersonByPersonId(Guid? personId)
        {
            using (Operation.Time("Delete person operation for ID: {PersonId}", personId))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. PersonId: {PersonId}",
                    nameof(DeletePersonByPersonId), DateTime.UtcNow, personId);

                try
                {
                    if (personId == Guid.Empty || personId == null)
                    {
                        _logger.LogWarning("DeletePersonByPersonId called with invalid PersonId: {PersonId}", personId);
                        return false;
                    }

                    _logger.LogDebug("Attempting to delete person with ID: {PersonId}", personId);
                    var result = await PersonRipository.DeletePerson(personId);

                    if (result)
                    {
                        _logger.LogInformation("Successfully deleted person with ID: {PersonId}", personId);
                    }
                    else
                    {
                        _logger.LogWarning("No person found to delete with ID: {PersonId}", personId);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in DeletePersonByPersonId for PersonId: {PersonId}", personId);
                    throw;
                }
            }
        }
    }
}