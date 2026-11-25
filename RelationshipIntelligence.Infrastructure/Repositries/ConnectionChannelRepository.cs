using ContactsManger.Core.Domain.Entities;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using Servicess.Helpers;

namespace Repositories
{
    public class ConnectionChannelRepository : ConnectionChannelRepositryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<ConnectionChannelRepository> _logger;

        public ConnectionChannelRepository(AppDBContext db, ILogger<ConnectionChannelRepository> logger)
        {
            _db = db;
            _logger = logger;
        }




        public async Task<IEnumerable<ConnectionChannel>> GetConnectionChannelsByNames(IEnumerable<string> names)
        {
            var nameList = names?.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList()
                ?? new List<string>();

            using (Operation.Time("GetConnectionChannelsByNames database query for {Count} names", nameList.Count))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Names: {Names}",
                    nameof(GetConnectionChannelsByNames), DateTime.UtcNow, LogSanitizer.Sanitize(nameList));

                try
                {
                    if (nameList.Count == 0)
                    {
                        _logger.LogDebug("GetConnectionChannelsByNames called with no valid names, returning empty result");
                        return Enumerable.Empty<ConnectionChannel>();
                    }

                    var channels = await _db.ConnectionChannels
                        .Where(c => nameList.Contains(c.ConnectionChannelName))
                        .ToListAsync();

                    _logger.LogDebug("{MethodName} completed successfully. Found {FoundCount} of {RequestedCount} requested names",
                        nameof(GetConnectionChannelsByNames), LogSanitizer.Sanitize(channels.Count), LogSanitizer.Sanitize(nameList.Count));

                    return channels;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for names: {Names}",
                        nameof(GetConnectionChannelsByNames), LogSanitizer.Sanitize(nameList));
                    throw;
                }
            }
        }


        public async Task<ConnectionChannel> AddConnectionChannel(ConnectionChannel channel)
        {
            using (Operation.Time("AddConnectionChannel staged for: {ChannelName}", channel?.ConnectionChannelName))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Channel: {ConnectionChannel}",
                    nameof(AddConnectionChannel), DateTime.UtcNow, LogSanitizer.Sanitize(channel));

                try
                {
                    if (channel == null)
                    {
                        _logger.LogWarning("AddConnectionChannel called with null channel parameter");
                        throw new ArgumentNullException(nameof(channel));
                    }

                    _db.ConnectionChannels.Add(channel);

                    _logger.LogInformation("Successfully staged ConnectionChannel for insert. ID: {ConnectionChannelId}, Name: {ChannelName}",
                        LogSanitizer.Sanitize(channel.ConnectionChannelId), LogSanitizer.Sanitize(channel.ConnectionChannelName));

                    return channel;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in {MethodName} for channel: {ConnectionChannel}",
                        nameof(AddConnectionChannel), LogSanitizer.Sanitize(channel));
                    throw;
                }
            }
        }


        public async Task<ConnectionChannel?> GetConnectionChannelById(Guid? id)
        {
            using (Operation.Time("GetConnectionChannelById database query for ID: {ConnectionChannelId}", id))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. ConnectionChannelId: {ConnectionChannelId}",
                    nameof(GetConnectionChannelById), DateTime.UtcNow, LogSanitizer.Sanitize(id));

                try
                {
                    if (id == null || id == Guid.Empty)
                    {
                        _logger.LogWarning("GetConnectionChannelById called with invalid ID: {ConnectionChannelId}", LogSanitizer.Sanitize(id));
                        return null;
                    }

                    var channel = await _db.ConnectionChannels.FindAsync(id);

                    if (channel == null)
                        _logger.LogInformation("No ConnectionChannel found with ID: {ConnectionChannelId}", LogSanitizer.Sanitize(id));
                    else
                        _logger.LogInformation("Successfully retrieved ConnectionChannel with ID: {ConnectionChannelId}, Name: {ChannelName}",
                            LogSanitizer.Sanitize(channel.ConnectionChannelId), LogSanitizer.Sanitize(channel.ConnectionChannelName));

                    return channel;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for ConnectionChannelId: {ConnectionChannelId}",
                        nameof(GetConnectionChannelById), LogSanitizer.Sanitize(id));
                    throw;
                }
            }
        }

        public async Task<ConnectionChannel?> GetConnectionChannelByName(string name)
        {
            using (Operation.Time("GetConnectionChannelByName database query for name: {ChannelName}", name))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Channel name: {ChannelName}",
                    nameof(GetConnectionChannelByName), DateTime.UtcNow, LogSanitizer.Sanitize(name));

                try
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        _logger.LogWarning("GetConnectionChannelByName called with null or empty name");
                        return null;
                    }

                    var channel = await _db.ConnectionChannels
                        .FirstOrDefaultAsync(c => c.ConnectionChannelName == name);

                    if (channel == null)
                        _logger.LogInformation("No ConnectionChannel found with name: {ChannelName}", LogSanitizer.Sanitize(name));
                    else
                        _logger.LogInformation("Successfully retrieved ConnectionChannel with ID: {ConnectionChannelId}, Name: {ChannelName}",
                            LogSanitizer.Sanitize(channel.ConnectionChannelId), LogSanitizer.Sanitize(channel.ConnectionChannelName));

                    return channel;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for channel name: {ChannelName}",
                        nameof(GetConnectionChannelByName), LogSanitizer.Sanitize(name));
                    throw;
                }
            }
        }

        public async Task<IEnumerable<ConnectionChannel>> GetAllConnectionChannels()
        {
            using (Operation.Time("GetAllConnectionChannels database operation"))
            {
                _logger.LogDebug("Executing {MethodName} method at {Timestamp}", nameof(GetAllConnectionChannels), DateTime.UtcNow);

                try
                {
                    var channels = await _db.ConnectionChannels.ToListAsync();

                    _logger.LogDebug("{MethodName} completed successfully. Retrieved {Count} channels",
                        nameof(GetAllConnectionChannels), channels.Count);

                    return channels;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} method", nameof(GetAllConnectionChannels));
                    throw;
                }
            }
        }
    }
}