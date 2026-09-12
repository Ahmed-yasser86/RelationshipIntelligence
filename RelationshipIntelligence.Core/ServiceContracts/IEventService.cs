using ServiceContracts.DTOs.EventDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IEventService
    {
        Task<List<EventResponse>> ListForPersonAsync(Guid personId);

        Task<List<EventOccurrenceDto>> GetUpcomingAsync(int days);

        Task<EventResponse> CreateAsync(EventCreateRequest request);

        Task<EventResponse> UpdateAsync(EventUpdateRequest request);

        Task DeleteAsync(Guid eventId);
    }
}
