using ContactsManger.Core.Domain.Entities;
using ContactsManger.Core.Domain.Entities.EEnums;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;

namespace Repositories
{
    public class SystemStatusTagRepository : SystemStatusTagRepositryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<SystemStatusTagRepository> _logger;

        public SystemStatusTagRepository(AppDBContext db, ILogger<SystemStatusTagRepository> logger)
        {
            _db = db;
            _logger = logger;
        }


        public async Task<IEnumerable<SystemStatusTag>> GetSystemStatusTagsByEnums(IEnumerable<EnSystemStatusTag> statusTagIds)
        {
            var idList = statusTagIds?.Distinct().ToList() ?? new List<EnSystemStatusTag>();

            using (Operation.Time("GetSystemStatusTagsByEnums database query for {Count} values", idList.Count))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. StatusTagIds: {@StatusTagIds}",
                    nameof(GetSystemStatusTagsByEnums), DateTime.UtcNow, idList);

                try
                {
                    if (idList.Count == 0)
                    {
                        _logger.LogDebug("GetSystemStatusTagsByEnums called with no values, returning empty result");
                        return Enumerable.Empty<SystemStatusTag>();
                    }

                    var tags = await _db.SystemStatusTags
                        .Where(t => idList.Contains(t.StatusTagId))
                        .ToListAsync();

                    if (tags.Count < idList.Count)
                    {
                        var foundIds = tags.Select(t => t.StatusTagId).ToHashSet();
                        var missing = idList.Where(id => !foundIds.Contains(id));
                        _logger.LogWarning("Some SystemStatusTag reference rows are missing for enum values: {@MissingIds}. Seed data may need to be applied.", missing);
                    }

                    _logger.LogInformation("{MethodName} completed successfully. Found {FoundCount} of {RequestedCount} requested values",
                        nameof(GetSystemStatusTagsByEnums), tags.Count, idList.Count);

                    return tags;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for values: {@StatusTagIds}",
                        nameof(GetSystemStatusTagsByEnums), idList);
                    throw;
                }
            }
        }



        public async Task<SystemStatusTag?> GetSystemStatusTagByEnum(EnSystemStatusTag statusTagId)
        {
            using (Operation.Time("GetSystemStatusTagByEnum database query for: {StatusTagId}", statusTagId))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. StatusTagId: {StatusTagId}",
                    nameof(GetSystemStatusTagByEnum), DateTime.UtcNow, statusTagId);

                try
                {
                    var tag = await _db.SystemStatusTags
                        .FirstOrDefaultAsync(t => t.StatusTagId == statusTagId);

                    if (tag == null)
                        _logger.LogWarning("No SystemStatusTag row found for enum value: {StatusTagId}. Reference data may not be seeded.", statusTagId);
                    else
                        _logger.LogInformation("Successfully retrieved SystemStatusTag: {StatusTagId}, Name: {Name}", tag.StatusTagId, tag.Name);

                    return tag;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for StatusTagId: {StatusTagId}",
                        nameof(GetSystemStatusTagByEnum), statusTagId);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<SystemStatusTag>> GetAllSystemStatusTags()
        {
            using (Operation.Time("GetAllSystemStatusTags database operation"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}", nameof(GetAllSystemStatusTags), DateTime.UtcNow);

                try
                {
                    var tags = await _db.SystemStatusTags.ToListAsync();

                    _logger.LogInformation("{MethodName} completed successfully. Retrieved {Count} tags",
                        nameof(GetAllSystemStatusTags), tags.Count);

                    return tags;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} method", nameof(GetAllSystemStatusTags));
                    throw;
                }
            }
        }
    }
}