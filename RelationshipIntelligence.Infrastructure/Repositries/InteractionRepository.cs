using ContactsManger.Core.Domain.Entities;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories
{
    public class InteractionRepository : InteractionRepositoryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<InteractionRepository> _logger;

        public InteractionRepository(AppDBContext db, ILogger<InteractionRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<Interaction> AddAsync(Interaction interaction)
        {
            using (Operation.Time("AddInteraction database operation"))
            {
                if (interaction == null)
                    throw new ArgumentNullException(nameof(interaction));

                await _db.Interactions.AddAsync(interaction);
                return interaction;
            }
        }

        public async Task<List<Interaction>> ListForPersonAsync(Guid personId)
        {
            using (Operation.Time("ListForPerson database operation"))
            {
                return await _db.Interactions
                    .Where(i => i.PersonId == personId)
                    .OrderByDescending(i => i.TimeOfInteraction)
                    .ToListAsync();
            }
        }
    }
}
