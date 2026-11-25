using Entities;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts;
using ServiceContracts.DTOs.PreferenceDTOs;
using System;
using System.Threading.Tasks;

namespace ContactsManager.API.Controllers
{
    /// <summary>
    /// User-controlled relationship parameters in human terms: cadence,
    /// importance, priority, intentional contact, suggestion inclusion, and
    /// per-person reminders. Intention only — these endpoints never fabricate
    /// interaction history.
    /// </summary>
    public class PreferenceController : CustomWebController
    {
        private readonly IRelationshipPreferenceService _preferences;

        public PreferenceController(IRelationshipPreferenceService preferences)
        {
            _preferences = preferences;
        }

        [HttpGet]
        public async Task<IActionResult> GetPreference(Guid personId)
        {
            try
            {
                var preference = await _preferences.GetAsync(personId);
                if (preference == null)
                    return NotFound();
                return Ok(preference);
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        }

        [HttpPut]
        public async Task<IActionResult> PutPreference([FromBody] PreferenceSaveRequest request)
        {
            try
            {
                return Ok(await _preferences.SaveAsync(request));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
            catch (ArgumentException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostReminder([FromBody] ReminderSetRequest request)
        {
            try
            {
                return Ok(await _preferences.SetReminderAsync(request));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
            catch (ArgumentException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostSnooze(Guid personId, int days)
        {
            try
            {
                return Ok(await _preferences.SnoozeAsync(personId, days));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
            catch (ArgumentException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostSkip(Guid personId)
        {
            try
            {
                return Ok(await _preferences.SkipAsync(personId));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostComplete(Guid personId)
        {
            try
            {
                return Ok(await _preferences.CompleteAsync(personId));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
            catch (InvalidOperationException ex)
            {
                // Completion requires a real logged interaction: the client
                // must call the interaction pipeline, not this endpoint.
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletePreference(Guid personId, [FromQuery] string source = "User")
        {
            try
            {
                if (!Enum.TryParse<PreferenceChangeSource>(source, true, out var parsed))
                    return Problem(detail: "Source must be User, Copilot, Import, or Default.", statusCode: StatusCodes.Status400BadRequest);
                await _preferences.RemoveAsync(personId, parsed);
                return Ok(true);
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReminderState(Guid personId)
        {
            try
            {
                return Ok(await _preferences.GetReminderStateAsync(personId));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPreferenceHistory(Guid personId)
        {
            try
            {
                return Ok(await _preferences.GetHistoryAsync(personId));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGlobalDefaults()
        {
            return Ok(await _preferences.GetGlobalDefaultsAsync());
        }

        [HttpPut]
        public async Task<IActionResult> PutGlobalDefaults(
            [FromBody] GlobalDefaultsSaveRequest request, [FromQuery] string source = "User")
        {
            try
            {
                if (!Enum.TryParse<PreferenceChangeSource>(source, true, out var parsed))
                    return Problem(detail: "Source must be User, Copilot, Import, or Default.", statusCode: StatusCodes.Status400BadRequest);
                return Ok(await _preferences.SaveGlobalDefaultsAsync(request, parsed));
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

        [HttpDelete]
        public async Task<IActionResult> DeleteReminder(Guid personId)
        {
            try
            {
                return Ok(await _preferences.DisableReminderAsync(personId));
            }
            catch (KeyNotFoundException ex)
            {
                return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDueReminders()
        {
            return Ok(await _preferences.ListDueAsync());
        }
    }
}
