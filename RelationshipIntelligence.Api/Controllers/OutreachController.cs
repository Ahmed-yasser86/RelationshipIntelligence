using Microsoft.AspNetCore.Mvc;
using ServiceContracts;
using ServiceContracts.DTOs.CopilotDTOs;
using ServiceContracts.DTOs.OutreachDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContactsManager.API.Controllers
{
    public class OutreachController : CustomWebController
    {
        private readonly IOutreachService _outreach;
        private readonly ICopilotService _copilot;

        public OutreachController(IOutreachService outreach, ICopilotService copilot)
        {
            _outreach = outreach;
            _copilot = copilot;
        }

        [HttpGet]
        public async Task<IActionResult> GetBatches()
        {
            return Ok(await _outreach.ListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> GetBatch(Guid id)
        {
            try
            {
                return Ok(await _outreach.GetAsync(id));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostBatchFromSignals([FromBody] BuildBatchFromSignalsRequest request)
        {
            try
            {
                return Ok(await _outreach.BuildFromSignalsAsync(request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostBatchFromPersons([FromBody] BuildBatchFromPersonsRequest request)
        {
            try
            {
                return Ok(await _outreach.BuildFromPersonsAsync(request));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostBatchFromNL([FromBody] BatchFromNlRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Text is required.");

            try
            {
                var intent = await _copilot.ParseOutreachIntentAsync(request.Text);
                if (intent.NeedsClarification)
                    return BadRequest(intent.ClarificationPrompt ?? "The request needs clarification.");
                return Ok(await _outreach.BuildFromIntentAsync(intent));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (RelationshipIntelligence.AI.CopilotNotConfiguredException ex)
            {
                return Conflict(ex.Message);
            }
            catch (RelationshipIntelligence.AI.CopilotUnavailableException ex)
            {
                return StatusCode(503, ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> PutBatchSettings(Guid id, [FromBody] BatchSettingsRequest request)
        {
            try
            {
                return Ok(await _outreach.UpdateSettingsAsync(id, request));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> PutBatchMember(Guid id, Guid memberId, [FromBody] MemberOverrideRequest request)
        {
            try
            {
                return Ok(await _outreach.UpdateMemberAsync(id, memberId, request));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostBatchDrafts(Guid id)
        {
            try
            {
                return Ok(await _outreach.GenerateDraftsAsync(id));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (RelationshipIntelligence.AI.CopilotNotConfiguredException ex)
            {
                return Conflict(ex.Message);
            }
            catch (RelationshipIntelligence.AI.CopilotUnavailableException ex)
            {
                return StatusCode(503, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> PutDraft(Guid id, [FromBody] DraftReviewRequest request)
        {
            try
            {
                return Ok(await _outreach.ReviewDraftAsync(id, request));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostDraftRegenerate(Guid id, [FromBody] DraftRegenerateRequest? request)
        {
            try
            {
                return Ok(await _outreach.RegenerateDraftAsync(id, request?.CustomInstruction));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (RelationshipIntelligence.AI.CopilotNotConfiguredException ex)
            {
                return Conflict(ex.Message);
            }
            catch (RelationshipIntelligence.AI.CopilotUnavailableException ex)
            {
                return StatusCode(503, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostBatchApprove(Guid id, [FromBody] BatchApproveRequest? request)
        {
            try
            {
                return Ok(await _outreach.ApproveAsync(id, request?.DraftIds));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteBatch(Guid id)
        {
            try
            {
                await _outreach.DiscardBatchAsync(id);
                return Ok(true);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
