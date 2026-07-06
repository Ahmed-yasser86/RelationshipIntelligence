using ServiceContracts.DTOs;

namespace ServiceContracts
{
    public interface IPersonQuickAdderService
    {
        Task<PersonRespones> QuickAddPerson(PersonQuickAddRequest? personQuickAddRequest);
    }
}