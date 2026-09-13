using Entities;
using Microsoft.EntityFrameworkCore;
using RepositryContracts;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories
{
    public class MeetingRepository : MeetingRepositoryContract
    {
        private readonly AppDBContext _db;

        public MeetingRepository(AppDBContext db)
        {
            _db = db;
        }

        private IQueryable<Meeting> WithDetails() => _db.Meetings
            .Include(m => m.People)
            .Include(m => m.Findings)
            .Include(m => m.Brief);

        public async Task<List<Meeting>> ListAsync(Guid ownerId)
        {
            using (Operation.Time("List meetings"))
            {
                if (ownerId == Guid.Empty)
                    return new List<Meeting>();

                return await _db.Meetings
                    .Where(m => m.ApplicationUserId == ownerId)
                    .OrderByDescending(m => m.OccurredAtUtc)
                    .ToListAsync();
            }
        }

        public async Task<List<Meeting>> ListForMappedPersonAsync(Guid ownerId, Guid personId)
        {
            using (Operation.Time("List meetings for person"))
            {
                if (ownerId == Guid.Empty || personId == Guid.Empty)
                    return new List<Meeting>();

                return await WithDetails()
                    .Where(m => m.ApplicationUserId == ownerId
                        && m.People.Any(p => p.MappedPersonId == personId))
                    .OrderByDescending(m => m.OccurredAtUtc)
                    .ToListAsync();
            }
        }

        public async Task<Meeting?> GetAsync(Guid ownerId, Guid meetingId)
        {
            using (Operation.Time("Get meeting"))
            {
                if (ownerId == Guid.Empty)
                    return null;

                return await WithDetails()
                    .FirstOrDefaultAsync(m => m.ApplicationUserId == ownerId && m.MeetingId == meetingId);
            }
        }

        public async Task AddAsync(Meeting meeting)
        {
            if (meeting == null)
                throw new ArgumentNullException(nameof(meeting));

            if (meeting.MeetingId == Guid.Empty)
                meeting.MeetingId = Guid.NewGuid();

            await _db.Meetings.AddAsync(meeting);
        }

        public async Task RemoveAsync(Meeting meeting)
        {
            if (meeting == null)
                throw new ArgumentNullException(nameof(meeting));

            var tracked = await _db.Meetings
                .FirstOrDefaultAsync(m => m.ApplicationUserId == meeting.ApplicationUserId
                    && m.MeetingId == meeting.MeetingId);
            if (tracked != null)
                _db.Meetings.Remove(tracked);

            await Task.CompletedTask;
        }

        public async Task AddPersonAsync(MeetingPerson person)
        {
            if (person == null)
                throw new ArgumentNullException(nameof(person));

            if (person.MeetingPersonId == Guid.Empty)
                person.MeetingPersonId = Guid.NewGuid();

            await _db.MeetingPersons.AddAsync(person);
        }

        public async Task AddFindingAsync(MeetingFinding finding)
        {
            if (finding == null)
                throw new ArgumentNullException(nameof(finding));

            if (finding.MeetingFindingId == Guid.Empty)
                finding.MeetingFindingId = Guid.NewGuid();

            await _db.MeetingFindings.AddAsync(finding);
        }

        public async Task AddBriefAsync(MeetingBrief brief)
        {
            if (brief == null)
                throw new ArgumentNullException(nameof(brief));

            if (brief.MeetingBriefId == Guid.Empty)
                brief.MeetingBriefId = Guid.NewGuid();

            await _db.MeetingBriefs.AddAsync(brief);
        }
    }
}
