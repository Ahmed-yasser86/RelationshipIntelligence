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
            using (Operation.Time("AddSocialMediaAccount database operation for URL: {Url}", account?.Url))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Account: {@SocialMediaAccount}",
                    nameof(AddSocialMediaAccount), DateTime.UtcNow, account);

                try
                {
                    if (account == null)
                    {
                        _logger.LogWarning("AddSocialMediaAccount called with null account parameter");
                        throw new ArgumentNullException(nameof(account));
                    }

                    _db.SocialMediaAccounts.Add(account);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Successfully added SocialMediaAccount with ID: {SocialMediaAccountId}, Platform: {Platform}",
                        account.SocialMediaAccountId, account.Platform);

                    return account;
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database update error while adding SocialMediaAccount. Account: {@SocialMediaAccount}. Error: {ErrorMessage}",
                        account, ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in {MethodName} for account: {@SocialMediaAccount}",
                        nameof(AddSocialMediaAccount), account);
                    throw;
                }
            }
        }

        public async Task<SocialMediaAccount?> GetSocialMediaAccountById(Guid? id)
        {
            using (Operation.Time("GetSocialMediaAccountById database query for ID: {SocialMediaAccountId}", id))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. SocialMediaAccountId: {SocialMediaAccountId}",
                    nameof(GetSocialMediaAccountById), DateTime.UtcNow, id);

                try
                {
                    if (id == null || id == Guid.Empty)
                    {
                        _logger.LogWarning("GetSocialMediaAccountById called with invalid ID: {SocialMediaAccountId}", id);
                        return null;
                    }

                    var account = await _db.SocialMediaAccounts.FindAsync(id);

                    if (account == null)
                        _logger.LogInformation("No SocialMediaAccount found with ID: {SocialMediaAccountId}", id);
                    else
                        _logger.LogInformation("Successfully retrieved SocialMediaAccount with ID: {SocialMediaAccountId}", account.SocialMediaAccountId);

                    return account;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for SocialMediaAccountId: {SocialMediaAccountId}",
                        nameof(GetSocialMediaAccountById), id);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<SocialMediaAccount>> GetAllSocialMediaAccountsForPerson(Guid personId)
        {
            using (Operation.Time("GetAllSocialMediaAccountsForPerson database operation for PersonId: {PersonId}", personId))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. PersonId: {PersonId}",
                    nameof(GetAllSocialMediaAccountsForPerson), DateTime.UtcNow, personId);

                try
                {
                    var accounts = await _db.SocialMediaAccounts
                        .Where(a => a.People.Any(p => p.PersonId == personId))
                        .ToListAsync();

                    _logger.LogInformation("{MethodName} completed successfully. Retrieved {Count} accounts for PersonId: {PersonId}",
                        nameof(GetAllSocialMediaAccountsForPerson), accounts.Count, personId);

                    return accounts;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for PersonId: {PersonId}",
                        nameof(GetAllSocialMediaAccountsForPerson), personId);
                    throw;
                }
            }
        }
    }
}