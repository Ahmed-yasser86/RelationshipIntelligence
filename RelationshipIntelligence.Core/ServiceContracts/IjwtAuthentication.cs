using ContactsManger.Core.DTOs;
using System;

namespace ContactsManger.Core.ServiceContracts
{
    public interface IjwtAuthentication
    {
        public AuthentocationRespones Authenticate(Guid userId, string? email, string? personName);
    }
}
