using Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface MeetingRepositoryContract
    {
        Task<List<Meeting>> ListAsync(Guid ownerId);

        Task<List<Meeting>> ListForMappedPersonAsync(Guid ownerId, Guid personId);

        Task<Meeting?> GetAsync(Guid ownerId, Guid meetingId);

        Task AddAsync(Meeting meeting);

        Task RemoveAsync(Meeting meeting);

        Task AddPersonAsync(MeetingPerson person);

        Task AddFindingAsync(MeetingFinding finding);

        Task AddBriefAsync(MeetingBrief brief);
    }
}
