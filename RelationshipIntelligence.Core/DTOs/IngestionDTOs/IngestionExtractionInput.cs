using System.Collections.Generic;

namespace ServiceContracts.DTOs.IngestionDTOs
{
    public class IngestionExtractionInput
    {
        public string RawText { get; set; } = string.Empty;

        public List<KnownIngestionPerson> KnownPeople { get; set; } = new();
    }
}
