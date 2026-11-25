using ContactsManger.Core.Domain.Entities;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using Servicess.Helpers;

namespace Repositories
{
    public class ContactItemRoleRepository : ContactItemRoleRepositryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<ContactItemRoleRepository> _logger;

        public ContactItemRoleRepository(AppDBContext db, ILogger<ContactItemRoleRepository> logger)
        {
            _db = db;
            _logger = logger;
        }


        public async Task<ContactItemRole> AddContactItemRole(ContactItemRole role)
        {
            using (Operation.Time("AddContactItemRole staged for Role: {Role}", role?.Role))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Role: {ContactItemRole}",
                    nameof(AddContactItemRole), DateTime.UtcNow, LogSanitizer.Sanitize(role));

                try
                {
                    if (role == null)
                    {
                        _logger.LogWarning("AddContactItemRole called with null role parameter");
                        throw new ArgumentNullException(nameof(role));
                    }

                    _db.ContactItemRoles.Add(role);

                    _logger.LogInformation("Successfully staged ContactItemRole for insert. ID: {ContactsRoleId}, Role: {Role}",
                        LogSanitizer.Sanitize(role.ContactsRoleId), LogSanitizer.Sanitize(role.Role));

                    return role;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in {MethodName} for role: {ContactItemRole}",
                        nameof(AddContactItemRole), LogSanitizer.Sanitize(role));
                    throw;
                }
            }
        }

        public async Task<ContactItemRole?> GetContactItemRoleById(Guid? id)
        {
            using (Operation.Time("GetContactItemRoleById database query for ID: {ContactsRoleId}", id))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. ContactsRoleId: {ContactsRoleId}",
                    nameof(GetContactItemRoleById), DateTime.UtcNow, LogSanitizer.Sanitize(id));

                try
                {
                    if (id == null || id == Guid.Empty)
                    {
                        _logger.LogWarning("GetContactItemRoleById called with invalid ID: {ContactsRoleId}", LogSanitizer.Sanitize(id));
                        return null;
                    }

                    var role = await _db.ContactItemRoles.FindAsync(id);

                    if (role == null)
                        _logger.LogInformation("No ContactItemRole found with ID: {ContactsRoleId}", LogSanitizer.Sanitize(id));
                    else
                        _logger.LogInformation("Successfully retrieved ContactItemRole with ID: {ContactsRoleId}, Role: {Role}",
                            LogSanitizer.Sanitize(role.ContactsRoleId), LogSanitizer.Sanitize(role.Role));

                    return role;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for ContactsRoleId: {ContactsRoleId}",
                        nameof(GetContactItemRoleById), LogSanitizer.Sanitize(id));
                    throw;
                }
            }
        }

        public async Task<ContactItemRole?> GetContactItemRoleByPersonAndRole(Guid personId, string role)
        {
            using (Operation.Time("GetContactItemRoleByPersonAndRole database query for PersonId: {PersonId}, Role: {Role}", personId, role))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. PersonId: {PersonId}, Role: {Role}",
                    nameof(GetContactItemRoleByPersonAndRole), DateTime.UtcNow, LogSanitizer.Sanitize(personId), LogSanitizer.Sanitize(role));

                try
                {
                    if (personId == Guid.Empty || string.IsNullOrEmpty(role))
                    {
                        _logger.LogWarning("GetContactItemRoleByPersonAndRole called with invalid PersonId or Role");
                        return null;
                    }

                    var existingRole = await _db.ContactItemRoles
                        .FirstOrDefaultAsync(r => r.PersonId == personId && r.Role == role);

                    if (existingRole == null)
                        _logger.LogInformation("No ContactItemRole found for PersonId: {PersonId}, Role: {Role}", LogSanitizer.Sanitize(personId), LogSanitizer.Sanitize(role));
                    else
                        _logger.LogInformation("Successfully retrieved ContactItemRole with ID: {ContactsRoleId}", LogSanitizer.Sanitize(existingRole.ContactsRoleId));

                    return existingRole;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for PersonId: {PersonId}, Role: {Role}",
                        nameof(GetContactItemRoleByPersonAndRole), LogSanitizer.Sanitize(personId), LogSanitizer.Sanitize(role));
                    throw;
                }
            }
        }

        public async Task<IEnumerable<ContactItemRole>> GetAllContactItemRolesForPerson(Guid personId)
        {
            using (Operation.Time("GetAllContactItemRolesForPerson database operation for PersonId: {PersonId}", personId))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. PersonId: {PersonId}",
                    nameof(GetAllContactItemRolesForPerson), DateTime.UtcNow, LogSanitizer.Sanitize(personId));

                try
                {
                    var roles = await _db.ContactItemRoles
                        .Where(r => r.PersonId == personId)
                        .ToListAsync();

                    _logger.LogDebug("{MethodName} completed successfully. Retrieved {Count} roles for PersonId: {PersonId}",
                        nameof(GetAllContactItemRolesForPerson), LogSanitizer.Sanitize(roles.Count), LogSanitizer.Sanitize(personId));

                    return roles;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for PersonId: {PersonId}",
                        nameof(GetAllContactItemRolesForPerson), LogSanitizer.Sanitize(personId));
                    throw;
                }
            }
        }
    }
}