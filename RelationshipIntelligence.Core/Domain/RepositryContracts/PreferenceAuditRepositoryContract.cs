using Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface PreferenceAuditRepositoryContract
    {
        Task AddAsync(PreferenceAuditEntry entry);

        Task<List<PreferenceAuditEntry>> ListForPersonAsync(Guid ownerId, Guid personId);
    }
}
