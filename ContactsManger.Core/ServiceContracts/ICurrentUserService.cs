using System;

namespace ServiceContracts
{

    public interface ICurrentUserService
    {
        Guid? UserId { get; }
    }
}