using System.Collections.Generic;

namespace ServiceContracts.DTOs.OutreachDTOs
{
    public class BuildBatchFromSignalsRequest
    {
        public List<string> SignalFilters { get; set; } = new();

        public int TimeWindowDays { get; set; } = 7;

        public int MaxMembers { get; set; } = 12;

        public string Intent { get; set; } = "Reconnect";

        /// <summary>Organization for the companyMembers signal. Optional.</summary>
        public string? CompanyName { get; set; }
    }
}
