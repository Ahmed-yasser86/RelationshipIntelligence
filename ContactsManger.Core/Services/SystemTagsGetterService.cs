using ContactsManger.Core.Domain.Entities;
using ContactsManger.Core.DTOs;
using ContactsManger.Core.ServiceContracts;
using RepositryContracts;
using ServiceContracts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactsManger.Core.Services
{
    public class SystemTagsGetterService :  ISystemTagsGetter
    {
        private readonly SystemStatusTagRepositryContract _systemTagRepositryContract;
        public SystemTagsGetterService(SystemStatusTagRepositryContract systemTagRepositryContract) {

            _systemTagRepositryContract = systemTagRepositryContract;
        }
        public async Task<IEnumerable<SystemStatusTagResponse>> GetSystemTags()
        {
            IEnumerable<SystemStatusTag> systemStatusTags;
            try
            {
              systemStatusTags = await _systemTagRepositryContract.GetAllSystemStatusTags();
            }
            catch (Exception ex) {
                throw;
            }
            var sytemtags =  systemStatusTags.Select(t => t.ConvertToDto()).ToList();
            return sytemtags;
        }
    }
}
