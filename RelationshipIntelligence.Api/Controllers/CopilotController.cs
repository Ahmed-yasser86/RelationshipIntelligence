using Microsoft.AspNetCore.Mvc;
using RelationshipIntelligence.AI;
using ServiceContracts;
using ServiceContracts.DTOs.CopilotDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContactsManager.API.Controllers
{
    public class CopilotController : CustomWebController
    {
        private readonly ICopilotService _copilot;
        private readonly IAiProviderSettingsService _providerSettings;

        public CopilotController(ICopilotService copilot, IAiProviderSettingsService providerSettings)
        {
            _copilot = copilot;
            _providerSettings = providerSettings;
        }

        public class AskRequest
        {
            public string? Question { get; set; }

            public Guid? PersonId { get; set; }

            public List<ChatTurnDto>? History { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> PostAsk([FromBody] AskRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
                return BadRequest("Question is required.");

            try
            {
                return Ok(await _copilot.AskAsync(request.Question.Trim(), request.PersonId, request.History));
            }
            catch (CopilotNotConfiguredException ex)
            {
                return Conflict(ex.Message);
            }
            catch (CopilotUnavailableException ex)
            {
                return StatusCode(503, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostSummarize([FromBody] AskRequest request)
        {
            if (request?.PersonId == null)
                return BadRequest("PersonId is required.");

            try
            {
                return Ok(await _copilot.SummarizePersonAsync(request.PersonId.Value));
            }
            catch (CopilotNotConfiguredException ex)
            {
                return Conflict(ex.Message);
            }
            catch (CopilotUnavailableException ex)
            {
                return StatusCode(503, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostBriefing()
        {
            try
            {
                return Ok(await _copilot.BuildBriefingAsync());
            }
            catch (CopilotNotConfiguredException ex)
            {
                return Conflict(ex.Message);
            }
            catch (CopilotUnavailableException ex)
            {
                return StatusCode(503, ex.Message);
            }
        }

        public class PlanRequest
        {
            public Guid PersonId { get; set; }

            public Guid? IntentEntryId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> PostSuggestPlan([FromBody] PlanRequest request)
        {
            if (request == null || request.PersonId == Guid.Empty)
                return BadRequest("PersonId is required.");

            try
            {
                return Ok(await _copilot.SuggestPlanAsync(request.PersonId, request.IntentEntryId));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (CopilotNotConfiguredException ex)
            {
                return Conflict(ex.Message);
            }
            catch (CopilotUnavailableException ex)
            {
                return StatusCode(503, ex.Message);
            }
        }

        public class ParseIntentRequest
        {
            public string? Text { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> PostParseOutreachIntent([FromBody] ParseIntentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Text is required.");

            try
            {
                return Ok(await _copilot.ParseOutreachIntentAsync(request.Text));
            }
            catch (CopilotNotConfiguredException ex)
            {
                return Conflict(ex.Message);
            }
            catch (CopilotUnavailableException ex)
            {
                return StatusCode(503, ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAiSettings()
        {
            try
            {
                return Ok(await _providerSettings.GetAsync());
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> PutAiSettings([FromBody] AiProviderSettingsSaveRequest request)
        {
            if (request == null)
                return BadRequest("Settings are required.");

            try
            {
                return Ok(await _providerSettings.SaveAsync(request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}
