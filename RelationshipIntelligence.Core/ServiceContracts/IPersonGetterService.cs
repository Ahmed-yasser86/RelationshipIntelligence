using ContactsManger.Core.DTOs.PersonDTOs;
using ServiceContracts.DTOs;

namespace ServiceContracts
{
    public interface IPersonGetterService
    {
        Task<List<PersonRespones>> GetAllPersons();

        Task<PersonRespones?> GetPersonByPersonId(Guid? personId);

        Task<PagedResult<PersonViewDTO>> GetPersonsViewBatched(int pageNumber, int pageSize);

    }
}