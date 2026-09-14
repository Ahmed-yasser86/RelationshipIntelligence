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
                return NotFound(ex.Message);
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
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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
                return NotFound(ex.Message);
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
                return NotFound(ex.Message);
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
                return NotFound(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDueReminders()
        {
            return Ok(await _preferences.ListDueAsync());
        }
    }
}
