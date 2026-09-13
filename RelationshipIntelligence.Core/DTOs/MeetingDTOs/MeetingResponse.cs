using Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceContracts.DTOs.MeetingDTOs
{
    public class MeetingResponse
    {
        public Guid MeetingId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime OccurredAtUtc { get; set; }

        public DateTime? ActualOccurredAtUtc { get; set; }

        public string? Description { get; set; }

        public string? Agenda { get; set; }

        public string? UserInstructions { get; set; }

        public bool HasTranscript { get; set; }

        public bool HasNotes { get; set; }

        public string? ProcessedSummary { get; set; }

        public MeetingStatus Status { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public List<MeetingPersonDto> People { get; set; } = new();

        public List<MeetingFindingDto> Findings { get; set; } = new();

        public MeetingBriefDto? Brief { get; set; }

        public static MeetingResponse FromMeeting(Meeting meeting, Func<Guid, string?> nameOf) => new()
        {
            MeetingId = meeting.MeetingId,
            Title = meeting.Title,
            OccurredAtUtc = meeting.OccurredAtUtc,
            ActualOccurredAtUtc = meeting.ActualOccurredAtUtc,
            Description = meeting.Description,
            Agenda = meeting.Agenda,
            UserInstructions = meeting.UserInstructions,
            HasTranscript = !string.IsNullOrWhiteSpace(meeting.RawTranscript),
            HasNotes = !string.IsNullOrWhiteSpace(meeting.RawNotes),
            ProcessedSummary = meeting.ProcessedSummary,
            Status = meeting.Status,
            CreatedAtUtc = meeting.CreatedAtUtc,
            UpdatedAtUtc = meeting.UpdatedAtUtc,
            People = meeting.People.Select(p => new MeetingPersonDto
            {
                MeetingPersonId = p.MeetingPersonId,
                DetectedName = p.DetectedName,
                MappedPersonId = p.MappedPersonId,
                MappedPersonName = p.MappedPersonId == null ? null : nameOf(p.MappedPersonId.Value),
                MatchStatus = p.MatchStatus,
                SelectedForLogging = p.SelectedForLogging
            }).ToList(),
            Findings = meeting.Findings.Select(f => new MeetingFindingDto
            {
                MeetingFindingId = f.MeetingFindingId,
                MappedPersonId = f.MappedPersonId,
                MappedPersonName = f.MappedPersonId == null ? null : nameOf(f.MappedPersonId.Value),
                Kind = f.Kind,
                Title = f.Title,
                Detail = f.Detail,
                Status = f.Status,
                SourceExcerpt = f.SourceExcerpt,
                AcceptedAsEntryId = f.AcceptedAsEntryId
            }).ToList(),
            Brief = meeting.Brief == null ? null : new MeetingBriefDto
            {
                Goal = meeting.Brief.Goal,
                BriefJson = meeting.Brief.BriefJson,
                UpdatedAtUtc = meeting.Brief.UpdatedAtUtc
            }
        };
    }
}
