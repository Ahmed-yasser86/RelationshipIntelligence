using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ServiceContracts;
using ServiceContracts.DTOs.IngestionDTOs;
using System;
using System.Threading.Tasks;

namespace ContactsManager.API.Controllers
{
    /// <summary>
    /// Unified ingestion: every entry point (person text, conversation,
    /// group text, meeting evidence) converges here. Extraction never writes
    /// durable state; only reviewed approvals mutate relationships.
    /// </summary>
    public class IngestionController : CustomWebController
    {
        private readonly IIngestionService _ingestion;

        public IngestionController(IIngestionService ingestion)
        {
            _ingestion = ingestion;
        }

        [HttpGet]
        public async Task<IActionResult> GetBatches()
        {
            return Ok(await _ingestion.ListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> GetBatch(Guid id)
        {
            try
            {
                return Ok(await _ingestion.GetAsync(id));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPending()
        {
            return Ok(await _ingestion.ListPendingAsync());
        }

        [HttpPost]
        public async Task<IActionResult> PostSubmit([FromBody] IngestionSubmitRequest request)
        {
            try
            {
                return Ok(await _ingestion.SubmitAsync(request));
            }
            catch (ArgumentException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostSubmitForMeeting(Guid meetingId)
        {
            try
            {
                return Ok(await _ingestion.SubmitForMeetingAsync(meetingId));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
            catch (ArgumentException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (InvalidOperationException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        // NOTE: POST with an empty body is rejected by the JSON input
        // formatter (415) because of the global Consumes("application/json")
        // filter. The client must send '{}'. Documented here so the next
        // person does not "fix" this by removing the filter.
        [HttpPost]
        public async Task<IActionResult> PostProcess(Guid id)
        {
            try
            {
                return Ok(await _ingestion.ProcessAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
            catch (RelationshipIntelligence.AI.CopilotUnavailableException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (RelationshipIntelligence.AI.CopilotNotConfiguredException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
        }

        [HttpPut]
        public async Task<IActionResult> PutReview([FromBody] IngestionFindingReviewRequest request)
        {
            try
            {
                return Ok(await _ingestion.ReviewAsync(request));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
            catch (ArgumentException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (InvalidOperationException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostApproveFinding(Guid id)
        {
            try
            {
                return Ok(await _ingestion.ApproveFindingAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostApprovePerson([FromBody] IngestionApprovePersonRequest request)
        {
            try
            {
                return Ok(await _ingestion.ApprovePersonAsync(
                    request.BatchId, request.PersonId, request.PersonName, request.IsNew));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostApproveBatch(Guid id)
        {
            try
            {
                return Ok(await _ingestion.ApproveBatchAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteBatch(Guid id)
        {
            try
            {
                await _ingestion.DiscardAsync(id);
                return Ok(true);
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
            catch (InvalidOperationException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
        }
    }
}
