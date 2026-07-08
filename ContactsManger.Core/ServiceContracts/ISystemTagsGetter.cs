using ContactsManger.Core.Domain.Entities;
using ServiceContracts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactsManger.Core.ServiceContracts
{
    public interface ISystemTagsGetter
    {

        public Task<IEnumerable<SystemStatusTagResponse>> GetSystemTags();
       

    }
}
