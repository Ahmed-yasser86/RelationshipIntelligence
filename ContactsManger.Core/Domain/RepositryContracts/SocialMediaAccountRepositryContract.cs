using ContactsManger.Core.Domain.Entities;
using System;

namespace RepositryContracts
{
    public interface SocialMediaAccountRepositryContract
    {
        Task<SocialMediaAccount> AddSocialMediaAccount(SocialMediaAccount account);

        Task<SocialMediaAccount>? GetSocialMediaAccountById(Guid? id);
        public Task<bool> DeleteSocialMediaAccount(Guid socialMediaAccountId);


        Task<IEnumerable<SocialMediaAccount>> GetAllSocialMediaAccountsForPerson(Guid personId);
    }
}