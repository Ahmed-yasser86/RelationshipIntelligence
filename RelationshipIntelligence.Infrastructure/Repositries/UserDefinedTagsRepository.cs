using ContactsManger.Core.Domain.Entities;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;

namespace Repositories
{
    public class UserDefinedTagsRepository : UserDefinedTagsRepositryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<UserDefinedTagsRepository> _logger;

        public UserDefinedTagsRepository(AppDBContext db, ILogger<UserDefinedTagsRepository> logger)
        {
            _db = db;
            _logger = logger;
        }



        public async Task<IEnumerable<UserDefinedTags>> GetUserDefinedTagsByNames(IEnumerable<string> names)
        {
            var nameList = names?.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList()
                ?? new List<string>();

            using (Operation.Time("GetUserDefinedTagsByNames database query for {Count} names", nameList.Count))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Names: {@Names}",
                    nameof(GetUserDefinedTagsByNames), DateTime.UtcNow, nameList);

                try
                {
                    if (nameList.Count == 0)
                    {
                        _logger.LogDebug("GetUserDefinedTagsByNames called with no valid names, returning empty result");
                        return Enumerable.Empty<UserDefinedTags>();
                    }

                    var tags = await _db.UserDefinedTags
                        .Where(t => nameList.Contains(t.TagName))
                        .ToListAsync();

                    _logger.LogInformation("{MethodName} completed successfully. Found {FoundCount} of {RequestedCount} requested names",
                        nameof(GetUserDefinedTagsByNames), tags.Count, nameList.Count);

                    return tags;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for names: {@Names}",
                        nameof(GetUserDefinedTagsByNames), nameList);
                    throw;
                }
            }
        }


        public async Task<UserDefinedTags> AddUserDefinedTag(UserDefinedTags tag)
        {
            using (Operation.Time("AddUserDefinedTag staged for: {TagName}", tag?.TagName))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Tag: {@UserDefinedTags}",
                    nameof(AddUserDefinedTag), DateTime.UtcNow, tag);

                try
                {
                    if (tag == null)
                    {
                        _logger.LogWarning("AddUserDefinedTag called with null tag parameter");
                        throw new ArgumentNullException(nameof(tag));
                    }

                    _db.UserDefinedTags.Add(tag);

                    _logger.LogInformation("Successfully staged UserDefinedTag for insert. ID: {TagId}, Name: {TagName}",
                        tag.TagId, tag.TagName);

                    return tag;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in {MethodName} for tag: {@UserDefinedTags}",
                        nameof(AddUserDefinedTag), tag);
                    throw;
                }
            }
        }

        public async Task<UserDefinedTags?> GetUserDefinedTagById(Guid? id)
        {
            using (Operation.Time("GetUserDefinedTagById database query for ID: {TagId}", id))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. TagId: {TagId}",
                    nameof(GetUserDefinedTagById), DateTime.UtcNow, id);

                try
                {
                    if (id == null || id == Guid.Empty)
                    {
                        _logger.LogWarning("GetUserDefinedTagById called with invalid ID: {TagId}", id);
                        return null;
                    }

                    var tag = await _db.UserDefinedTags.FindAsync(id);

                    if (tag == null)
                        _logger.LogInformation("No UserDefinedTag found with ID: {TagId}", id);
                    else
                        _logger.LogInformation("Successfully retrieved UserDefinedTag with ID: {TagId}, Name: {TagName}",
                            tag.TagId, tag.TagName);

                    return tag;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for TagId: {TagId}",
                        nameof(GetUserDefinedTagById), id);
                    throw;
                }
            }
        }

        public async Task<UserDefinedTags?> GetUserDefinedTagByName(string name)
        {
            using (Operation.Time("GetUserDefinedTagByName database query for name: {TagName}", name))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Tag name: {TagName}",
                    nameof(GetUserDefinedTagByName), DateTime.UtcNow, name);

                try
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        _logger.LogWarning("GetUserDefinedTagByName called with null or empty name");
                        return null;
                    }

                    var tag = await _db.UserDefinedTags.FirstOrDefaultAsync(t => t.TagName == name);

                    if (tag == null)
                        _logger.LogInformation("No UserDefinedTag found with name: {TagName}", name);
                    else
                        _logger.LogInformation("Successfully retrieved UserDefinedTag with ID: {TagId}, Name: {TagName}",
                            tag.TagId, tag.TagName);

                    return tag;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for tag name: {TagName}",
                        nameof(GetUserDefinedTagByName), name);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<UserDefinedTags>> GetAllUserDefinedTags()
        {
            using (Operation.Time("GetAllUserDefinedTags database operation"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}", nameof(GetAllUserDefinedTags), DateTime.UtcNow);

                try
                {
                    var tags = await _db.UserDefinedTags.ToListAsync();

                    _logger.LogInformation("{MethodName} completed successfully. Retrieved {Count} tags",
                        nameof(GetAllUserDefinedTags), tags.Count);

                    return tags;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} method", nameof(GetAllUserDefinedTags));
                    throw;
                }
            }
        }
    }
}