using ServiceContracts.DTOs.IngestionDTOs;
using System.Threading.Tasks;

namespace ServiceContracts
{
    /// <summary>
    /// Deterministic-shape extraction over raw text. Implementations return the
    /// strict ingestion contract; mapping to domain slots happens in
    /// IngestionService, never inside the extractor.
    /// </summary>
    public interface IIngestionExtractor
    {
        Task<IngestionExtraction> ExtractAsync(IngestionExtractionInput input);
    }
}
