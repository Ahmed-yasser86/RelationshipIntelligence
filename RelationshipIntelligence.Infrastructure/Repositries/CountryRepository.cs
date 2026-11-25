using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using Servicess.Helpers;

namespace Repositories
{
    public class CountryRepository : CountryRepositryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<CountryRepository> _logger;

        public CountryRepository(AppDBContext db, ILogger<CountryRepository> logger)
        {
            _db = db;
            _logger = logger;
        }


        public async Task<Country> AddCountry(Country country)
        {
            using (Operation.Time("AddCountry staged for Country: {CountryName}", country?.CountryName))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Country: {Country}",
                    nameof(AddCountry), DateTime.UtcNow, LogSanitizer.Sanitize(country));

                try
                {
                    if (country == null)
                    {
                        _logger.LogWarning("AddCountry called with null country parameter");
                        throw new ArgumentNullException(nameof(country));
                    }

                    _logger.LogDebug("Staging country with ID: {CountryId}, Name: {CountryName} for insert",
                        LogSanitizer.Sanitize(country.CountryId), LogSanitizer.Sanitize(country.CountryName));

                    _db.Countries.Add(country);

                    _logger.LogInformation("Successfully staged country for insert. ID: {CountryId}, Name: {CountryName}",
                        LogSanitizer.Sanitize(country.CountryId), LogSanitizer.Sanitize(country.CountryName));

                    return country;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in {MethodName} for country: {Country}",
                        nameof(AddCountry), LogSanitizer.Sanitize(country));
                    throw;
                }
            }
        }

        public async Task<Country> UpdateCountry(Country country)
        {
            using (Operation.Time("UpdateCountry staged for Country: {CountryName}", country?.CountryName))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Country: {Country}",
                    nameof(UpdateCountry), DateTime.UtcNow, LogSanitizer.Sanitize(country));

                try
                {
                    if (country == null)
                    {
                        _logger.LogWarning("UpdateCountry called with null country parameter");
                        throw new ArgumentNullException(nameof(country));
                    }

                    var existingCountry = await _db.Countries.FindAsync(country.CountryId);

                    if (existingCountry == null)
                    {
                        _logger.LogWarning("UpdateCountry called for non-existent Country ID: {CountryId}", LogSanitizer.Sanitize(country.CountryId));
                        throw new InvalidOperationException($"Country with ID {country.CountryId} does not exist.");
                    }

                    _db.Entry(existingCountry).CurrentValues.SetValues(country);

                    _logger.LogInformation("Successfully staged country update. ID: {CountryId}, Name: {CountryName}",
                        LogSanitizer.Sanitize(existingCountry.CountryId), LogSanitizer.Sanitize(existingCountry.CountryName));

                    return existingCountry;
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in {MethodName} for country: {Country}",
                        nameof(UpdateCountry), LogSanitizer.Sanitize(country));
                    throw;
                }
            }
        }

        public async Task<IEnumerable<Country>> GetAllCountries()
        {
            using (Operation.Time("GetAllCountries database operation"))
            {
                _logger.LogDebug("Executing {MethodName} method at {Timestamp}",
                    nameof(GetAllCountries), DateTime.UtcNow);

                try
                {
                    var countries = await _db.Countries.ToListAsync();

                    _logger.LogDebug("{MethodName} completed successfully. Retrieved {Count} countries",
                        nameof(GetAllCountries), countries.Count);

                    return countries;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} method", nameof(GetAllCountries));
                    throw;
                }
            }
        }

        public async Task<Country?> GetCountryByName(string name)
        {
            using (Operation.Time("GetCountryByName database query for name: {CountryName}", name))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Country name: {CountryName}",
                    nameof(GetCountryByName), DateTime.UtcNow, LogSanitizer.Sanitize(name));

                try
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        _logger.LogWarning("GetCountryByName called with null or empty name");
                        return null;
                    }

                    var country = await _db.Countries.FirstOrDefaultAsync(c => c.CountryName == name);

                    if (country == null)
                    {
                        _logger.LogInformation("No country found with name: {CountryName}", LogSanitizer.Sanitize(name));
                    }
                    else
                    {
                        _logger.LogInformation("Successfully retrieved country with ID: {CountryId}, Name: {CountryName}",
                            LogSanitizer.Sanitize(country.CountryId), LogSanitizer.Sanitize(country.CountryName));
                    }

                    return country;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for country name: {CountryName}",
                        nameof(GetCountryByName), LogSanitizer.Sanitize(name));
                    throw;
                }
            }
        }

        public async Task<Country?> GetCountryById(Guid? id)
        {
            using (Operation.Time("GetCountryById database query for ID: {CountryId}", id))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Country ID: {CountryId}",
                    nameof(GetCountryById), DateTime.UtcNow, LogSanitizer.Sanitize(id));

                try
                {
                    if (id == null || id == Guid.Empty)
                    {
                        _logger.LogWarning("GetCountryById called with invalid ID: {CountryId}", LogSanitizer.Sanitize(id));
                        return null;
                    }

                    var country = await _db.Countries.FindAsync(id);

                    if (country == null)
                    {
                        _logger.LogInformation("No country found with ID: {CountryId}", LogSanitizer.Sanitize(id));
                    }
                    else
                    {
                        _logger.LogInformation("Successfully retrieved country with ID: {CountryId}, Name: {CountryName}",
                            LogSanitizer.Sanitize(country.CountryId), LogSanitizer.Sanitize(country.CountryName));
                    }

                    return country;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for country ID: {CountryId}",
                        nameof(GetCountryById), LogSanitizer.Sanitize(id));
                    throw;
                }
            }
        }


    }
}