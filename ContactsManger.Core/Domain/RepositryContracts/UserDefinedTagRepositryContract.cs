using ContactsManger.Core.Domain.Entities;
using System;

namespace RepositryContracts
{
    public interface UserDefinedTagsRepositryContract
    {
        Task<UserDefinedTags> AddUserDefinedTag(UserDefinedTags tag);

        Task<UserDefinedTags>? GetUserDefinedTagById(Guid? id);

        Task<UserDefinedTags>? GetUserDefinedTagByName(string name);

        Task<IEnumerable<UserDefinedTags>> GetAllUserDefinedTags();

        Task<IEnumerable<UserDefinedTags>> GetUserDefinedTagsByNames(IEnumerable<string> names);
    }
}