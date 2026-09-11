using ServiceContracts.DTOs;

namespace ServiceContracts
{
    public interface ICountryGetterService
    {
        Task<List<CountryResponse>> Countries();
        Task<CountryResponse?> GetCountryByCountryId(Guid? ID);
    }

    public interface ICountryAdderService
    {
        Task<CountryResponse> AddCountryRequest(CountryAddRequest? countryAddRequest);
    }
}