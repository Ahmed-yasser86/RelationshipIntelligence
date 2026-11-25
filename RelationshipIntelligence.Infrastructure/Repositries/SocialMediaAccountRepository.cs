using ContactsManger.Core.Domain.Entities;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;

namespace Repositories
{
    public class SocialMediaAccountRepository : SocialMediaAccountRepositryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<SocialMediaAccountRepository> _logger;

        public SocialMediaAccountRepository(AppDBContext db, ILogger<SocialMediaAccountRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<SocialMediaAccount> AddSocialMediaAccount(SocialMediaAccount account)
        {
            using (Operation.Time("AddSocialMediaAccount staged"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}",
                    nameof(AddSocialMediaAccount), DateTime.UtcNow);

                try
                {
                    if (account == null)
                    {
                        _logger.LogWarning("AddSocialMediaAccount called with null account parameter");
                        throw new ArgumentNullException(nameof(account));
                    }

                    _db.SocialMediaAccounts.Add(account);

                    _logger.LogInformation("Successfully staged SocialMediaAccount for insert");

                    return account;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in {MethodName}",
                        nameof(AddSocialMediaAccount));
                    throw;
                }
            }
        }

        public async Task<bool> DeleteSocialMediaAccount(Guid socialMediaAccountId)
        {
            using (Operation.Time("DeleteSocialMediaAccount staged"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}",
                    nameof(DeleteSocialMediaAccount), DateTime.UtcNow);

                try
                {
                    if (socialMediaAccountId == Guid.Empty)
                    {
                        _logger.LogWarning("DeleteSocialMediaAccount called with an empty Guid.");
                        return false;
                    }

                    var account = await _db.SocialMediaAccounts
                        .FirstOrDefaultAsync(a => a.SocialMediaAccountId == socialMediaAccountId);

                    if (account == null)
                    {
                        _logger.LogInformation("No SocialMediaAccount found");
                        return false;
                    }

                    _db.SocialMediaAccounts.Remove(account);

                    _logger.LogInformation("Successfully staged SocialMediaAccount for deletion");

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in {MethodName}",
                        nameof(DeleteSocialMediaAccount));
                    throw;
                }
            }
        }

        public async Task<SocialMediaAccount?> GetSocialMediaAccountById(Guid? id)
        {
            using (Operation.Time("GetSocialMediaAccountById database query"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}",
                    nameof(GetSocialMediaAccountById), DateTime.UtcNow);

                try
                {
                    if (id == null || id == Guid.Empty)
                    {
                        _logger.LogWarning("GetSocialMediaAccountById called with invalid ID");
                        return null;
                    }

                    var account = await _db.SocialMediaAccounts.FindAsync(id);

                    if (account == null)
                        _logger.LogInformation("No SocialMediaAccount found");
                    else
                        _logger.LogInformation("Successfully retrieved SocialMediaAccount");

                    return account;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName}",
                        nameof(GetSocialMediaAccountById));
                    throw;
                }
            }
        }

        public async Task<IEnumerable<SocialMediaAccount>> GetAllSocialMediaAccountsForPerson(Guid personId)
        {
            using (Operation.Time("GetAllSocialMediaAccountsForPerson database operation"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}",
                    nameof(GetAllSocialMediaAccountsForPerson), DateTime.UtcNow);

                try
                {
                    var accounts = await _db.SocialMediaAccounts
                        .Where(a => a.People.Any(p => p.PersonId == personId))
                        .ToListAsync();

                    _logger.LogDebug("{MethodName} completed successfully. Retrieved {Count} accounts",
                        nameof(GetAllSocialMediaAccountsForPerson), accounts.Count);

                    return accounts;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName}",
                        nameof(GetAllSocialMediaAccountsForPerson));
                    throw;
                }
            }
        }
    }
}
