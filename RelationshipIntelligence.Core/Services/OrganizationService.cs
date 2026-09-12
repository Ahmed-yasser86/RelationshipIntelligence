using ContactsManger.Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs.OrganizationDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Servicess
{
    public class OrganizationService : IOrganizationService
    {
        private readonly CircleRepositryContract _circles;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrganizationService> _logger;

        public OrganizationService(
            CircleRepositryContract circles,
            IUnitOfWork unitOfWork,
            ILogger<OrganizationService> logger)
        {
            _circles = circles;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<OrganizationResponse>> GetAllAsync()
        {
            using (Operation.Time("List organizations"))
            {
                var circles = await _circles.GetCirclesWithMembers();
                return circles
                    .Select(OrganizationResponse.FromCircle)
                    .OrderBy(o => o.Name)
                    .ToList();
            }
        }

        public async Task<OrganizationResponse> CreateAsync(string? name)
        {
            using (Operation.Time("Create organization {Name}", name ?? "null"))
            {
                var clean = CleanName(name);

                var existing = await _circles.GetCircleByName(clean);
                if (existing != null)
                    throw new ArgumentException($"An organization named '{clean}' already exists.", nameof(name));

                var circle = new Circle { CircleId = Guid.NewGuid(), Name = clean };
                await _circles.AddCircle(circle);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created organization {Name} ({CircleId})", clean, circle.CircleId);
                return OrganizationResponse.FromCircle(circle);
            }
        }

        public async Task<OrganizationResponse> RenameAsync(Guid id, string? name)
        {
            using (Operation.Time("Rename organization {CircleId}", id))
            {
                var clean = CleanName(name);

                var circle = await _circles.GetCircleById(id);
                if (circle == null)
                    throw new KeyNotFoundException($"No organization found with id '{id}'.");

                var clash = await _circles.GetCircleByName(clean);
                if (clash != null && clash.CircleId != circle.CircleId)
                    throw new ArgumentException($"An organization named '{clean}' already exists.", nameof(name));

                circle.Name = clean;
                await _unitOfWork.SaveChangesAsync();

                var withMembers = (await _circles.GetCirclesWithMembers())
                    .FirstOrDefault(c => c.CircleId == circle.CircleId);
                return OrganizationResponse.FromCircle(withMembers ?? circle);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            using (Operation.Time("Delete organization {CircleId}", id))
            {
                var circles = await _circles.GetCirclesWithMembers();
                var circle = circles.FirstOrDefault(c => c.CircleId == id);
                if (circle == null)
                    throw new KeyNotFoundException($"No organization found with id '{id}'.");

                var members = circle.People?.Count ?? 0;
                if (members > 0)
                    throw new InvalidOperationException(
                        $"Cannot delete '{circle.Name}' while {members} contact(s) still belong to it. Move them to another organization first.");

                await _circles.RemoveCircle(circle);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deleted organization {Name} ({CircleId})", circle.Name, id);
            }
        }

        private static string CleanName(string? name)
        {
            var clean = (name ?? string.Empty).Trim();
            if (clean.Length == 0)
                throw new ArgumentException("Organization name is required.", nameof(name));
            if (clean.Length > 100)
                throw new ArgumentException("Organization name cannot exceed 100 characters.", nameof(name));
            return clean;
        }
    }
}
