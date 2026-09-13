using ServiceContracts.DTOs.MeetingDTOs;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IMeetingExtractor
    {
        Task<MeetingExtraction> ExtractAsync(MeetingExtractionInput input);
    }
}
