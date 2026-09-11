using System.ComponentModel.DataAnnotations;

namespace ServiceContracts.DTOs
{
    public class CsvImportRequest
    {
        [Required]
        public string? CsvText { get; set; }
    }
}
