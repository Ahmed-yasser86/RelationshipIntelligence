using Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositryContracts
{
    public interface DigestRepositoryContract
    {
        Task<DigestDelivery?> FindDeliveryAsync(Guid ownerId, DateTime weekStartUtc);

        Task AddDeliveryAsync(DigestDelivery delivery);

        Task AddMetricAsync(DigestMetric metric);

        Task<List<DigestMetric>> ListMetricsAsync(Guid deliveryId);

        Task<DigestPreference?> GetPreferenceAsync(Guid ownerId);

        Task UpsertPreferenceAsync(DigestPreference preference);
    }
}
