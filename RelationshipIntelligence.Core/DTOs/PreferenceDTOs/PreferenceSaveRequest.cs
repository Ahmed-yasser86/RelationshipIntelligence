using System;

namespace ServiceContracts.DTOs.PreferenceDTOs
{
    public class PreferenceSaveRequest
    {
        public Guid PersonId { get; set; }

        public int? DesiredCadenceDays { get; set; }

        public int? Importance { get; set; }

        public int? Priority { get; set; }

        public bool? KeepInTouchIntentionally { get; set; }

        public bool? ExcludeFromSuggestions { get; set; }
    }
}
