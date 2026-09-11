using ContactsManger.Core.Domain.IdentityEntities;
using ContactsManger.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactsManger.Core.ServiceContracts
{
    public interface IjwtAuthentication
    {
        public AuthentocationRespones Authenticate(ApplicationUser user);

    }
}
