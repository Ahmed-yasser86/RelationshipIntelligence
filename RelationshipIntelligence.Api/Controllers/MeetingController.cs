using Microsoft.AspNetCore.Mvc;
using ServiceContracts;
using ServiceContracts.DTOs.MeetingDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContactsManager.API.Controllers
{
    public class MeetingController : CustomWebController
    {
        private readonly IMeetingService _meetings;

        public MeetingController(IMeetingService meetings)
        {
            _meetings = meetings;
        }

        [HttpGet]
        public async Task<IActionResult> GetMeetings()
        {
            return Ok(await _meetings.ListAsync());
        }

        [HttpGet]
        [ServiceFilter(typeof(MeetingOwnershipFilter))]
        public async Task<IActionResult> GetMeeting(Guid id)
        {
            try
            {
                return Ok(await _meetings.GetAsync(id));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostMeetingPrep([FromBody] MeetingCreateRequest request)
        {
            try
            {
                return Ok(await _meetings.CreatePrepAsync(request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostMeetingDraft([FromBody] MeetingCreateRequest request)
        {
            try
            {
                return Ok(await _meetings.CreateDraftAsync(request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [ServiceFilter(typeof(MeetingOwnershipFilter))]
        public async Task<IActionResult> PostMeetingBeginLogging(Guid id)
        {
            try
            {
                return Ok(await _meetings.BeginLoggingAsync(id));
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

        [HttpPut]
        [ServiceFilter(typeof(MeetingOwnershipFilter))]
        public async Task<IActionResult> PutMeetingTranscript([FromBody] MeetingTranscriptRequest request)
        {
            try
            {
                return Ok(await _meetings.SetTranscriptAsync(request));
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
        [ServiceFilter(typeof(MeetingOwnershipFilter))]
        public async Task<IActionResult> PostMeetingProcess(Guid id)
        {
            try
            {
                return Ok(await _meetings.ProcessAsync(id));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
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
        [ServiceFilter(typeof(MeetingOwnershipFilter))]
        public async Task<IActionResult> PutMeetingPersons(Guid id, [FromBody] List<PersonMappingRequest> mappings)
        {
            try
            {
                return Ok(await _meetings.UpdateMappingsAsync(id, mappings));
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
        public async Task<IActionResult> PutMeetingFinding([FromBody] FindingReviewRequest request)
        {
            try
            {
                return Ok(await _meetings.ReviewFindingAsync(request));
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
        [ServiceFilter(typeof(MeetingOwnershipFilter))]
        public async Task<IActionResult> PutMeetingBrief([FromBody] BriefSaveRequest request)
        {
            try
            {
                return Ok(await _meetings.SaveBriefAsync(request));
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
        [ServiceFilter(typeof(MeetingOwnershipFilter))]
        public async Task<IActionResult> PostMeetingBrief(Guid id)
        {
            try
            {
                return Ok(await _meetings.GenerateBriefAsync(id));
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

        [HttpPost]
        public async Task<IActionResult> PostMeetingConfirm([FromBody] MeetingConfirmRequest request)
        {
            try
            {
                return Ok(await _meetings.ConfirmAsync(request));
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

        [HttpDelete]
        [ServiceFilter(typeof(MeetingOwnershipFilter))]
        public async Task<IActionResult> DeleteMeeting(Guid id)
        {
            try
            {
                await _meetings.DeleteAsync(id);
                return Ok(true);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }
    }
}
