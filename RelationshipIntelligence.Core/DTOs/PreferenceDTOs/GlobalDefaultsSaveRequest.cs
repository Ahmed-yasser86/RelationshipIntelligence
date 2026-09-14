namespace ServiceContracts.DTOs.PreferenceDTOs
{
    public class GlobalDefaultsSaveRequest
    {
        public int? DefaultCadenceDays { get; set; }

        public bool? DefaultReminderStrict { get; set; }
    }
}
