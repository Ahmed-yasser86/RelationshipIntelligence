using ServiceContracts.DTOs.OrganizationDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IOrganizationService
    {
        Task<List<OrganizationResponse>> GetAllAsync();

        Task<OrganizationResponse> CreateAsync(string? name);

        Task<OrganizationResponse> RenameAsync(Guid id, string? name);

        Task DeleteAsync(Guid id);
    }
}
