using ContactsManger.Core.DTOs.PersonDTOs;
using ServiceContracts.DTOs;
using ServiceContracts.DTOs.Enums;

namespace ServiceContracts
{
    public interface IPersonSorterService
    {
        Task<List<PersonViewDTO>> getPersonsSorted(
            List<PersonViewDTO> persons,
            string? sortBy,
            sortedListOp sortOrder
        );
    }
}