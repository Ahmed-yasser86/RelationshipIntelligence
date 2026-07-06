using ContactsManger.Core.Domain.Entities;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;

namespace Repositories
{
    public class CircleRepository : CircleRepositryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<CircleRepository> _logger;

        public CircleRepository(AppDBContext db, ILogger<CircleRepository> logger)
        {
            _db = db;
            _logger = logger;
        }




        public async Task<IEnumerable<Circle>> GetCirclesByNames(IEnumerable<string> names)
        {
            var nameList = names?.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList()
                ?? new List<string>();

            using (Operation.Time("GetCirclesByNames database query for {Count} names", nameList.Count))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Names: {@Names}",
                    nameof(GetCirclesByNames), DateTime.UtcNow, nameList);

                try
                {
                    if (nameList.Count == 0)
                    {
                        _logger.LogDebug("GetCirclesByNames called with no valid names, returning empty result");
                        return Enumerable.Empty<Circle>();
                    }

                    var circles = await _db.Circles
                        .Where(c => nameList.Contains(c.Name))
                        .ToListAsync();

                    _logger.LogInformation("{MethodName} completed successfully. Found {FoundCount} of {RequestedCount} requested names",
                        nameof(GetCirclesByNames), circles.Count, nameList.Count);

                    return circles;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for names: {@Names}",
                        nameof(GetCirclesByNames), nameList);
                    throw;
                }
            }
        }

        public async Task<Circle> AddCircle(Circle circle)
        {
            using (Operation.Time("AddCircle database operation for Circle: {CircleName}", circle?.Name))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Circle: {@Circle}",
                    nameof(AddCircle), DateTime.UtcNow, circle);

                try
                {
                    if (circle == null)
                    {
                        _logger.LogWarning("AddCircle called with null circle parameter");
                        throw new ArgumentNullException(nameof(circle));
                    }

                    _db.Circles.Add(circle);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Successfully added circle with ID: {CircleId}, Name: {CircleName}",
                        circle.CircleId, circle.Name);

                    return circle;
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database update error while adding circle. Circle: {@Circle}. Error: {ErrorMessage}",
                        circle, ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in {MethodName} for circle: {@Circle}",
                        nameof(AddCircle), circle);
                    throw;
                }
            }
        }

        public async Task<Circle?> GetCircleById(Guid? id)
        {
            using (Operation.Time("GetCircleById database query for ID: {CircleId}", id))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Circle ID: {CircleId}",
                    nameof(GetCircleById), DateTime.UtcNow, id);

                try
                {
                    if (id == null || id == Guid.Empty)
                    {
                        _logger.LogWarning("GetCircleById called with invalid ID: {CircleId}", id);
                        return null;
                    }

                    var circle = await _db.Circles.FindAsync(id);

                    if (circle == null)
                        _logger.LogInformation("No circle found with ID: {CircleId}", id);
                    else
                        _logger.LogInformation("Successfully retrieved circle with ID: {CircleId}, Name: {CircleName}",
                            circle.CircleId, circle.Name);

                    return circle;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for circle ID: {CircleId}",
                        nameof(GetCircleById), id);
                    throw;
                }
            }
        }

        public async Task<Circle?> GetCircleByName(string name)
        {
            using (Operation.Time("GetCircleByName database query for name: {CircleName}", name))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}. Circle name: {CircleName}",
                    nameof(GetCircleByName), DateTime.UtcNow, name);

                try
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        _logger.LogWarning("GetCircleByName called with null or empty name");
                        return null;
                    }

                    var circle = await _db.Circles.FirstOrDefaultAsync(c => c.Name == name);

                    if (circle == null)
                        _logger.LogInformation("No circle found with name: {CircleName}", name);
                    else
                        _logger.LogInformation("Successfully retrieved circle with ID: {CircleId}, Name: {CircleName}",
                            circle.CircleId, circle.Name);

                    return circle;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} for circle name: {CircleName}",
                        nameof(GetCircleByName), name);
                    throw;
                }
            }
        }

        public async Task<IEnumerable<Circle>> GetAllCircles()
        {
            using (Operation.Time("GetAllCircles database operation"))
            {
                _logger.LogInformation("Executing {MethodName} method at {Timestamp}", nameof(GetAllCircles), DateTime.UtcNow);

                try
                {
                    var circles = await _db.Circles.ToListAsync();

                    _logger.LogInformation("{MethodName} completed successfully. Retrieved {Count} circles",
                        nameof(GetAllCircles), circles.Count);

                    return circles;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in {MethodName} method", nameof(GetAllCircles));
                    throw;
                }
            }
        }
    }
}