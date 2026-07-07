using ContactsManger.Core.DTOs.PersonDTOs;
using ServiceContracts.DTOs;

namespace ServiceContracts
{
    public interface IPersonSearcherService
    {

        public Task<PagedResult<PersonViewDTO>> SearchPersonsBy_Batched(string? PersonParamter, string SearchBy, int pageNumber, int pageSize);
        Task<List<PersonRespones>> SearchPersonsBy(
            string? SearchBy,
            string SearchString
        );
    }
}