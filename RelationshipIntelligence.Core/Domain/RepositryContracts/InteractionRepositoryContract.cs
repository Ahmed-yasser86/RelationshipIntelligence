using ContactsManger.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface InteractionRepositoryContract
    {
        Task<Interaction> AddAsync(Interaction interaction);

        Task<List<Interaction>> ListForPersonAsync(Guid personId);
    }
}
