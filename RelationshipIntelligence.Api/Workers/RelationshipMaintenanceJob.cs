using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repositories;
using RepositryContracts;
using ServiceContracts;
using Servicess;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RelationshipIntelligence.Api.Workers
{
    public class RelationshipMaintenanceJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<RelationshipMaintenanceJob> _logger;

        public RelationshipMaintenanceJob(
            IServiceScopeFactory scopes,
            ILogger<RelationshipMaintenanceJob> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RecomputeAllAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Nightly relationship recompute failed");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task RecomputeAllAsync(CancellationToken token)
        {
            using var scope = _scopes.CreateScope();
            var provider = scope.ServiceProvider;
            var options = provider.GetRequiredService<DbContextOptions<AppDBContext>>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            List<Guid> ownerIds;
            using (var db = new AppDBContext(options, new FixedUserService(null)))
            {
                ownerIds = await db.Persons
                    .IgnoreQueryFilters()
                    .Select(p => p.ApplicationUserId)
                    .Distinct()
                    .ToListAsync(token);
            }

            foreach (var ownerId in ownerIds)
            {
                if (token.IsCancellationRequested)
                    return;

                using var db = new AppDBContext(options, new FixedUserService(ownerId));
                var scoring = new RelationshipScoringService(
                    new PersonRepository(db),
                    new RelationshipStateRepository(
                        db, loggerFactory.CreateLogger<RelationshipStateRepository>()),
                    new FixedUserService(ownerId),
                    new UnitOfWork(db),
                    loggerFactory.CreateLogger<RelationshipScoringService>());
                await scoring.RecomputeForOwnerAsync(ownerId);
            }

            _logger.LogInformation("Recomputed relationship states for {Count} owners", ownerIds.Count);
        }
    }
}
