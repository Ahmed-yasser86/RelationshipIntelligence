using ContactsManger.Core.Domain.Entities;
using System;

namespace RepositryContracts
{
    public interface ContactItemRoleRepositryContract
    {
        Task<ContactItemRole> AddContactItemRole(ContactItemRole role);

        Task<ContactItemRole>? GetContactItemRoleById(Guid? id);

        /// <summary>
        /// Roles are scoped per-person (Role text isn't globally unique — two
        /// different people can both have a "Manager" role as separate rows),
        /// so lookups need both the person and the role text.
        /// </summary>
        Task<ContactItemRole>? GetContactItemRoleByPersonAndRole(Guid personId, string role);

        Task<IEnumerable<ContactItemRole>> GetAllContactItemRolesForPerson(Guid personId);
    }
}