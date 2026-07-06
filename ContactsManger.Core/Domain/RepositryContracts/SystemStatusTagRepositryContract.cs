using ContactsManger.Core.Domain.Entities;
using ContactsManger.Core.Domain.Entities.EEnums;

namespace RepositryContracts
{
    public interface SystemStatusTagRepositryContract
    {
        /// <summary>
        /// SystemStatusTag rows are keyed by the EnSystemStatusTag enum value,
        /// not created ad hoc by users — this is reference/seed data (10 fixed
        /// rows). Get-by-enum is the only lookup needed; there's no "create a
        /// new tag" flow like there is for Circle/ContactItemRole.
        /// </summary>
        Task<SystemStatusTag>? GetSystemStatusTagByEnum(EnSystemStatusTag statusTagId);

        Task<IEnumerable<SystemStatusTag>> GetAllSystemStatusTags();
        Task<IEnumerable<SystemStatusTag>> GetSystemStatusTagsByEnums(IEnumerable<EnSystemStatusTag> statusTagIds);
    }
}