using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories
{
    public class DigestRepository : DigestRepositoryContract
    {
        private readonly AppDBContext _db;
        private readonly ILogger<DigestRepository> _logger;

        public DigestRepository(AppDBContext db, ILogger<DigestRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<DigestDelivery?> FindDeliveryAsync(Guid ownerId, DateTime weekStartUtc)
        {
            if (ownerId == Guid.Empty)
                return null;

            return await _db.DigestDeliveries
                .FirstOrDefaultAsync(d => d.ApplicationUserId == ownerId && d.WeekStartUtc == weekStartUtc);
        }

        public async Task AddDeliveryAsync(DigestDelivery delivery)
        {
            if (delivery == null)
                throw new ArgumentNullException(nameof(delivery));

            if (delivery.DigestDeliveryId == Guid.Empty)
                delivery.DigestDeliveryId = Guid.NewGuid();
            await _db.DigestDeliveries.AddAsync(delivery);
        }

        public async Task AddMetricAsync(DigestMetric metric)
        {
            if (metric == null)
                throw new ArgumentNullException(nameof(metric));

            if (metric.DigestMetricId == Guid.Empty)
                metric.DigestMetricId = Guid.NewGuid();
            await _db.DigestMetrics.AddAsync(metric);
        }

        public async Task<List<DigestMetric>> ListMetricsAsync(Guid deliveryId)
        {
            return await _db.DigestMetrics
                .Where(m => m.DeliveryId == deliveryId)
                .ToListAsync();
        }

        public async Task<DigestPreference?> GetPreferenceAsync(Guid ownerId)
        {
            if (ownerId == Guid.Empty)
                return null;

            return await _db.DigestPreferences
                .FirstOrDefaultAsync(p => p.ApplicationUserId == ownerId);
        }

        public async Task UpsertPreferenceAsync(DigestPreference preference)
        {
            if (preference == null)
                throw new ArgumentNullException(nameof(preference));

            var existing = await _db.DigestPreferences
                .FirstOrDefaultAsync(p => p.ApplicationUserId == preference.ApplicationUserId);

            if (existing == null)
                await _db.DigestPreferences.AddAsync(preference);
            else
            {
                existing.Enabled = preference.Enabled;
                existing.Threshold = preference.Threshold;
                existing.Count = preference.Count;
            }
        }
    }
}
