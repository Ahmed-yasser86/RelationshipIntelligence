using System;

namespace ServiceContracts.DTOs.PreferenceDTOs
{
    public class ReminderSetRequest
    {
        public Guid PersonId { get; set; }

        public int IntervalDays { get; set; }

        public bool Strict { get; set; }
    }
}
