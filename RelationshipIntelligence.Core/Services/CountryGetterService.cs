using Entities;
using ServiceContracts;
using ServiceContracts.DTOs;
using RepositryContracts;
using Microsoft.Extensions.Logging;
using SerilogTimings;

namespace Servicess
{
    public class CountryGetterService : ICountryGetterService
    {
        private readonly CountryRepositryContract CountriesRipositry;
        private readonly ILogger<CountryGetterService> _logger;

        public CountryGetterService(CountryRepositryContract countriesRipositry, ILogger<CountryGetterService> logger)
        {
            CountriesRipositry = countriesRipositry;
            _logger = logger;
        }

        public async Task<List<CountryResponse>> Countries()
        {
            using (Operation.Time("Get all countries operation"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}",
                    nameof(Countries), DateTime.UtcNow);

                try
                {
                    var countries = await CountriesRipositry.GetAllCountries();
                    var result = countries.Select(country => country.ConvertToDto()).ToList();

                    _logger.LogInformation("{MethodName} completed successfully. Retrieved {Count} countries",
                        nameof(Countries), result.Count);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} method", nameof(Countries));
                    throw;
                }
            }
        }

        public async Task<CountryResponse?> GetCountryByCountryId(Guid? ID)
        {
            using (Operation.Time("Get country by ID operation for: {CountryId}", ID))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Looking for country with ID: {CountryId}",
                    nameof(GetCountryByCountryId), DateTime.UtcNow, ID);

                try
                {
                    if (ID == null)
                    {
                        _logger.LogWarning("GetCountryByCountryId called with null ID parameter");
                        return null;
                    }

                    _logger.LogDebug("Retrieving country with ID: {CountryId}", ID);

                    Country country = await CountriesRipositry.GetCountryById(ID);

                    if (country == null)
                    {
                        _logger.LogWarning("No country found with ID: {CountryId}", ID);
                        return null;
                    }

                    var result = country.ConvertToDto();

                    _logger.LogInformation("Successfully retrieved country with ID: {CountryId}, Name: {CountryName}",
                        result.CountryId, result.CountryName);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in GetCountryByCountryId for ID: {CountryId}", ID);
                    throw;
                }
            }
        }
    }
}