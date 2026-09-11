using ServiceContracts.DTOs;

namespace ServiceContracts
{
    public interface IPersonAdderService
    {
        Task<PersonRespones> AddPerson(PersonAddRequest? personAddRequest);
    }
}