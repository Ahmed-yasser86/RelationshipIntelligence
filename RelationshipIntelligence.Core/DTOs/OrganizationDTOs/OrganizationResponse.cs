using ContactsManger.Core.Domain.Entities;
using System;

namespace ServiceContracts.DTOs.OrganizationDTOs
{
    public class OrganizationResponse
    {
        public Guid CircleId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int MemberCount { get; set; }

        public static OrganizationResponse FromCircle(Circle circle) => new()
        {
            CircleId = circle.CircleId,
            Name = circle.Name,
            MemberCount = circle.People?.Count ?? 0
        };
    }

    public class OrganizationCreateRequest
    {
        public string? Name { get; set; }
    }

    public class OrganizationRenameRequest
    {
        public Guid CircleId { get; set; }

        public string? Name { get; set; }
    }
}
