using ServiceContracts.DTOs;

namespace ServiceContracts
{
    public interface IPersonUpdaterService
    {
        Task<PersonRespones?> UpdatePerson(PersonUpdateRequest? personUpdateRequest);
    }
}