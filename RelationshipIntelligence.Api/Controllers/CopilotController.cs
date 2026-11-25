using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RelationshipIntelligence.AI;
using ServiceContracts;
using ServiceContracts.DTOs.AgentDTOs;
using ServiceContracts.DTOs.CopilotDTOs;
using System;
using System.Threading.Tasks;

namespace ContactsManager.API.Controllers
{
    public class CopilotController : CustomWebController
    {
        private readonly ICopilotService _copilot;
        private readonly ICopilotAgent _agent;
        private readonly IAiProviderSettingsService _providerSettings;

        public CopilotController(ICopilotService copilot, ICopilotAgent agent, IAiProviderSettingsService providerSettings)
        {
            _copilot = copilot;
            _agent = agent;
            _providerSettings = providerSettings;
        }

        [HttpPost]
        public async Task<IActionResult> PostAgentChat([FromBody] AgentChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return Problem(detail: "Message is required.", statusCode: StatusCodes.Status400BadRequest);

            try
            {
                return Ok(await _agent.ChatAsync(request));
            }
            catch (ArgumentException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
            }
            catch (CopilotNotConfiguredException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
            catch (CopilotUnavailableException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostAsk([FromBody] CopilotAskRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
                return Problem(detail: "Question is required.", statusCode: StatusCodes.Status400BadRequest);

            try
            {
                return Ok(await _copilot.AskAsync(request.Question.Trim(), request.PersonId, request.History));
            }
            catch (CopilotNotConfiguredException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
            catch (CopilotUnavailableException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostSummarize([FromBody] CopilotAskRequest request)
        {
            if (request?.PersonId == null)
                return Problem(detail: "PersonId is required.", statusCode: StatusCodes.Status400BadRequest);

            try
            {
                return Ok(await _copilot.SummarizePersonAsync(request.PersonId.Value));
            }
            catch (CopilotNotConfiguredException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
            catch (CopilotUnavailableException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
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
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
            catch (CopilotUnavailableException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostSuggestPlan([FromBody] PlanRequest request)
        {
            if (request == null || request.PersonId == Guid.Empty)
                return Problem(detail: "PersonId is required.", statusCode: StatusCodes.Status400BadRequest);

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
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
            catch (CopilotUnavailableException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostParseOutreachIntent([FromBody] ParseIntentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                return Problem(detail: "Text is required.", statusCode: StatusCodes.Status400BadRequest);

            try
            {
                return Ok(await _copilot.ParseOutreachIntentAsync(request.Text));
            }
            catch (CopilotNotConfiguredException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
            catch (CopilotUnavailableException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
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
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
            }
        }

        [HttpPut]
        public async Task<IActionResult> PutAiSettings([FromBody] AiProviderSettingsSaveRequest request)
        {
            if (request == null)
                return Problem(detail: "Settings are required.", statusCode: StatusCodes.Status400BadRequest);

            try
            {
                return Ok(await _providerSettings.SaveAsync(request));
            }
            catch (ArgumentException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status401Unauthorized);
            }
        }
    }
}
