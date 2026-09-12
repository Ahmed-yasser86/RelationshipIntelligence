using ContactsManager.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts;
using ServiceContracts.DTOs;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RelationshipIntelligence.Api.Controllers
{
    public class DigestController : CustomWebController
    {
        private readonly IDigestService _digest;

        public DigestController(IDigestService digest)
        {
            _digest = digest;
        }

        [HttpGet]
        public async Task<IActionResult> GetWeeklyDigest()
        {
            var payload = await _digest.BuildAsync($"{Request.Scheme}://{Request.Host}");
            return Ok(payload);
        }

        [HttpGet]
        public async Task<IActionResult> GetDigestPreference()
        {
            return Ok(await _digest.GetPreferenceAsync());
        }

        [HttpPost]
        public async Task<IActionResult> PostDigestPreference([FromBody] DigestPreferenceRequest request)
        {
            await _digest.SetPreferenceAsync(request.Enabled, request.Threshold, request.Count);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetDigestPreview([FromQuery] string? customNote = null)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return BadRequest("No email address on file.");

            return Ok(await _digest.PreviewAsync($"{Request.Scheme}://{Request.Host}", email, customNote));
        }

        [HttpPost]
        public async Task<IActionResult> PostSendDigest([FromBody] DigestSendRequest? request)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return BadRequest("No email address on file.");

            var payload = await _digest.DeliverAsync(
                $"{Request.Scheme}://{Request.Host}", email, request?.CustomNote);
            if (payload == null)
                return Ok("Nothing needs attention right now.");

            return Ok(payload);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DigestAction(string token, string action)
        {
            bool ok = await _digest.HandleActionAsync(token, action);
            return Content(ok ? "Recorded. Thank you." : "This link is expired or invalid.");
        }
    }
}
