using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories;
using RepositryContracts;
using ServiceContracts;
using Servicess;
using System;

namespace RelationshipIntelligence.Infrastructure.Services
{
    /// <summary>
    /// Creates the infrastructure objects needed by the nightly maintenance job.
    /// Construction of concrete repositories/DbContexts belongs to the
    /// Infrastructure layer; the Api worker only consumes the resulting service.
    /// </summary>
    public sealed class MaintenanceScoringFactory
    {
        private readonly DbContextOptions<AppDBContext> _options;
        private readonly ILoggerFactory _loggerFactory;

        public MaintenanceScoringFactory(
            DbContextOptions<AppDBContext> options,
            ILoggerFactory loggerFactory)
        {
            _options = options;
            _loggerFactory = loggerFactory;
        }

        public AppDBContext CreateEnumerationContext() =>
            new AppDBContext(_options, new FixedUserService(null));

        public (AppDBContext Context, IRelationshipScoringService Scoring) CreateForOwner(Guid? ownerId)
        {
            var db = new AppDBContext(_options, new FixedUserService(ownerId));
            var scoring = new RelationshipScoringService(
                new PersonRepository(db),
                new RelationshipStateRepository(db, _loggerFactory.CreateLogger<RelationshipStateRepository>()),
                new FixedUserService(ownerId),
                new UnitOfWork(db),
                _loggerFactory.CreateLogger<RelationshipScoringService>());
            return (db, scoring);
        }
    }
}
