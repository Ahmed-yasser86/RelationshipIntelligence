using System;
using System.Collections.Generic;

namespace ServiceContracts.DTOs
{
    public class DigestPayload
    {
        public Guid DeliveryId { get; set; }
        public DateTime WeekStartUtc { get; set; }
        public List<DigestEntry> Entries { get; set; } = new();
        public double NetworkHealth { get; set; }
    }
}
