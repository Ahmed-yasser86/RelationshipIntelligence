using ContactsManger.Core.Domain.Entities;
using System;

namespace RepositryContracts
{
    public interface ConnectionChannelRepositryContract
    {
        Task<ConnectionChannel> AddConnectionChannel(ConnectionChannel channel);

        Task<ConnectionChannel>? GetConnectionChannelById(Guid? id);

        Task<ConnectionChannel>? GetConnectionChannelByName(string name);

        Task<IEnumerable<ConnectionChannel>> GetAllConnectionChannels();
        Task<IEnumerable<ConnectionChannel>> GetConnectionChannelsByNames(IEnumerable<string> names);
    }
}