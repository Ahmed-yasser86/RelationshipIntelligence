using Entities;
using ServiceContracts;
using ServiceContracts.DTOs;
using RepositryContracts;
using Microsoft.Extensions.Logging;
using SerilogTimings;

namespace Servicess
{
    public class CountryAdderService : ICountryAdderService
    {
        private readonly CountryRepositryContract CountriesRipositry;
        private readonly ILogger<CountryAdderService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public CountryAdderService(
            CountryRepositryContract countriesRipositry,
            ILogger<CountryAdderService> logger,
            IUnitOfWork unitOfWork)
        {
            CountriesRipositry = countriesRipositry;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }



        public async Task<CountryResponse> AddCountryRequest(CountryAddRequest? countryAddRequest)
        {
            using (Operation.Time("Add country operation for: {CountryName}", countryAddRequest?.CountryName ?? "null"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Request data: {@CountryAddRequest}",
                    nameof(AddCountryRequest), DateTime.UtcNow, countryAddRequest);

                try
                {
                    if (countryAddRequest == null)
                    {
                        _logger.LogWarning("AddCountryRequest called with null request parameter");
                        throw new ArgumentNullException(nameof(countryAddRequest));
                    }

                    if (string.IsNullOrEmpty(countryAddRequest.CountryName))
                    {
                        _logger.LogWarning("AddCountryRequest called with empty or null CountryName");
                        throw new ArgumentException("Country name cannot be null or empty.", nameof(countryAddRequest.CountryName));
                    }

                    _logger.LogDebug("Checking if country '{CountryName}' already exists", countryAddRequest.CountryName);

                    var existingCountry = await CountriesRipositry.GetCountryByName(countryAddRequest.CountryName);
                    if (existingCountry != null)
                    {
                        _logger.LogWarning("Attempted to add duplicate country '{CountryName}'", countryAddRequest.CountryName);
                        throw new ArgumentException($"Country with name {countryAddRequest.CountryName} already exists.", nameof(countryAddRequest.CountryName));
                    }

                    _logger.LogDebug("Converting CountryAddRequest to Country entity");
                    Country country = countryAddRequest.ConvertToCountry();
                    country.CountryId = Guid.NewGuid();

                    _logger.LogDebug("Adding new country with ID: {CountryId}, Name: {CountryName}",
                        country.CountryId, country.CountryName);

                    await _unitOfWork.SaveChangesAsync();

                    var result = country.ConvertToDto();

                    _logger.LogInformation("Successfully added new country. ID: {CountryId}, Name: {CountryName}",
                        result.CountryId, result.CountryName);

                    return result;
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Validation error in AddCountryRequest for country name: {CountryName}",
                        countryAddRequest?.CountryName ?? "null");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in AddCountryRequest for country: {CountryName}",
                        countryAddRequest?.CountryName ?? "null");
                    throw;
                }
            }
        }
    }
}