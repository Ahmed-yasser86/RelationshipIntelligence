using ContactsManger.Core.DTOs.PersonDTOs;
using Entities;
using ServiceContracts.DTOs;

namespace ServiceContracts
{
    public interface IPersonSearcherService
    {

        public Task<PagedResult<PersonViewDTO>> SearchSortedPeopleBy(int page, int size, string sortBy);


        Task<PagedResult<PersonViewDTO>> SearchPersonsByCompositeFilter(
    PersonCompositeFilter filter, int pageNumber, int pageSize);

        public Task<PagedResult<PersonViewDTO>> SearchPersonsBy_Batched(string? PersonParamter, string SearchBy, int pageNumber, int pageSize);
        Task<List<PersonRespones>> SearchPersonsBy(
            string? SearchBy,
            string SearchString
        );
    }
}