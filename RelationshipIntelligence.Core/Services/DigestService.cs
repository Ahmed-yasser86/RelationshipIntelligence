using ContactsManger.Core.Domain.Entities;
using Entities;
using Microsoft.Extensions.Logging;
using RepositryContracts;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Servicess
{
    public class DigestService : IDigestService
    {
        public const double DefaultThreshold = 50;
        public const int DefaultCount = 5;

        private readonly IRelationshipScoringService _scoring;
        private readonly PersonRepositryContract _persons;
        private readonly InteractionRepositoryContract _interactions;
        private readonly DigestRepositoryContract _digests;
        private readonly RelationshipStateRepositoryContract _states;
        private readonly ICurrentUserService _currentUser;
        private readonly IEmailSender _email;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _secret;
        private readonly ILogger<DigestService> _logger;
        private readonly RelationshipPreferenceRepositoryContract? _preferences;

        public DigestService(
            IRelationshipScoringService scoring,
            PersonRepositryContract persons,
            InteractionRepositoryContract interactions,
            DigestRepositoryContract digests,
            RelationshipStateRepositoryContract states,
            ICurrentUserService currentUser,
            IEmailSender email,
            IUnitOfWork unitOfWork,
            string digestSecret,
            ILogger<DigestService> logger,
            RelationshipPreferenceRepositoryContract? preferences = null)
        {
            _scoring = scoring;
            _persons = persons;
            _interactions = interactions;
            _digests = digests;
            _states = states;
            _currentUser = currentUser;
            _email = email;
            _unitOfWork = unitOfWork;
            _secret = digestSecret;
            _logger = logger;
            _preferences = preferences;
        }

        public async Task<DigestPreference> GetPreferenceAsync()
        {
            var ownerId = _currentUser.UserId;
            if (ownerId == null || ownerId == Guid.Empty)
                throw new UnauthorizedAccessException("Cannot read digest preferences without an authenticated user.");

            return await _digests.GetPreferenceAsync(ownerId.Value)
                ?? new DigestPreference { ApplicationUserId = ownerId.Value };
        }

        public async Task SetPreferenceAsync(bool enabled, double threshold, int count)
        {
            var ownerId = _currentUser.UserId;
            if (ownerId == null || ownerId == Guid.Empty)
                throw new UnauthorizedAccessException("Cannot save digest preferences without an authenticated user.");

            await _digests.UpsertPreferenceAsync(new DigestPreference
            {
                ApplicationUserId = ownerId.Value,
                Enabled = enabled,
                Threshold = Math.Min(100, Math.Max(0, threshold)),
                Count = Math.Min(7, Math.Max(1, count))
            });
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<DigestPayload> BuildAsync(string baseUrl)
        {
            using (Operation.Time("Build digest"))
            {
                var ownerId = _currentUser.UserId;
                var payload = new DigestPayload { WeekStartUtc = WeekStart(DateTime.UtcNow) };
                if (ownerId == null || ownerId == Guid.Empty)
                    return payload;

                var preference = await GetPreferenceAsync();
                if (!preference.Enabled)
                    return payload;

                await RefreshStaleStatesAsync(ownerId.Value);
                // 200 covers the full network so digest selection is over every
                // ranked relationship, not a capped subset.
                var queue = await _scoring.GetQueueAsync(200);
                var now = DateTime.UtcNow;

                // Proactive-suggestion exclusion is user intent: excluded
                // people never appear in the digest, but stay in Attention.
                HashSet<Guid> excluded = new();
                if (_preferences != null)
                {
                    try
                    {
                        excluded = (await _preferences.ListForOwnerAsync(ownerId.Value))
                            .Where(p => p.ExcludeFromSuggestions)
                            .Select(p => p.PersonId)
                            .ToHashSet();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Suggestion exclusion skipped for digest.");
                    }
                }

                var selected = queue
                    .Where(q => !excluded.Contains(q.PersonId))
                    .Where(q => q.UrgencyScore >= preference.Threshold)
                    .Where(q => q.LastContactAtUtc == null || q.LastContactAtUtc < now.AddDays(-7))
                    .Take(preference.Count)
                    .ToList();

                var delivery = await _digests.FindDeliveryAsync(ownerId.Value, payload.WeekStartUtc);
                if (delivery == null)
                {
                    delivery = new DigestDelivery
                    {
                        ApplicationUserId = ownerId.Value,
                        WeekStartUtc = payload.WeekStartUtc,
                        PersonIdsJson = JsonSerializer.Serialize(selected.Select(s => s.PersonId).ToList()),
                        CreatedAtUtc = now
                    };
                    await _digests.AddDeliveryAsync(delivery);
                    await _unitOfWork.SaveChangesAsync();
                }

                payload.DeliveryId = delivery.DigestDeliveryId;
                if (selected.Count > 0)
                    payload.NetworkHealth = Math.Round(selected.Average(s => 100 - s.UrgencyScore), 1);

                foreach (var item in selected)
                {
                    string token = DigestActionSigner.Create(
                        _secret, ownerId.Value, item.PersonId, delivery.DigestDeliveryId);
                    string encoded = Uri.EscapeDataString(token);
                    var lastLogs = await _interactions.ListForPersonAsync(item.PersonId);
                    string? summary = lastLogs.FirstOrDefault()?.InteractionTitle;

                    payload.Entries.Add(new DigestEntry
                    {
                        Health = item,
                        Suggestion = string.IsNullOrWhiteSpace(summary)
                            ? "Good time to reconnect."
                            : $"Your last conversation was about {summary}. A good re-engagement: ask how it progressed.",
                        ActionUrl = $"{baseUrl.TrimEnd('/')}/api/Digest/DigestAction?token={encoded}&action=reached-out",
                        RemindUrl = $"{baseUrl.TrimEnd('/')}/api/Digest/DigestAction?token={encoded}&action=remind"
                    });
                }

                return payload;
            }
        }

        public const string DefaultSubject = "Relationships to protect this week";

        public async Task<DigestEmailPreview> PreviewAsync(
            string baseUrl, string recipientEmail, string? customNote)
        {
            var payload = await BuildAsync(baseUrl);
            var (subject, html, text) = RenderEmail(payload, customNote);
            return new DigestEmailPreview
            {
                To = recipientEmail,
                Subject = subject,
                HtmlBody = html,
                TextBody = text,
                EntryCount = payload.Entries.Count
            };
        }

        public async Task<DigestPayload?> DeliverAsync(
            string baseUrl, string recipientEmail, string? customNote = null)
        {
            var payload = await BuildAsync(baseUrl);
            if (payload.Entries.Count == 0)
                return null;

            var ownerId = _currentUser.UserId!.Value;
            var (subject, html, text) = RenderEmail(payload, customNote);
            await _email.SendAsync(recipientEmail, subject, html, text);

            foreach (var entry in payload.Entries)
            {
                await _digests.AddMetricAsync(new DigestMetric
                {
                    DeliveryId = payload.DeliveryId,
                    ApplicationUserId = ownerId,
                    PersonId = entry.Health.PersonId
                });
            }
            await _unitOfWork.SaveChangesAsync();

            return payload;
        }

        private static (string subject, string html, string text) RenderEmail(
            DigestPayload payload, string? customNote)
        {
            var html = new StringBuilder();
            html.Append("<html><body><h2>Relationships to protect this week</h2>");
            var text = new StringBuilder();
            text.AppendLine("Relationships to protect this week:");
            text.AppendLine();

            if (!string.IsNullOrWhiteSpace(customNote))
            {
                string note = customNote.Trim();
                html.Append("<p><em>").Append(System.Net.WebUtility.HtmlEncode(note)).Append("</em></p>");
                text.AppendLine(note);
                text.AppendLine();
            }

            html.Append("<ul>");
            foreach (var entry in payload.Entries)
            {
                html.Append("<li><strong>").Append(entry.Health.Name).Append("</strong> - ")
                    .Append(entry.Health.Band).Append(" (").Append(entry.Health.UrgencyScore.ToString("F0"))
                    .Append(")<br>").Append(entry.Suggestion).Append("<br><a href=\"")
                    .Append(entry.ActionUrl).Append("\">I reached out</a></li>");
                text.AppendLine($"- {entry.Health.Name} ({entry.Health.Band}): {entry.Suggestion}");
            }
            html.Append("</ul></body></html>");

            return (DefaultSubject, html.ToString(), text.ToString());
        }

        public async Task<bool> HandleActionAsync(string? token, string action)
        {
            if (!DigestActionSigner.TryVerify(_secret, token,
                    out Guid ownerId, out Guid personId, out Guid deliveryId))
                return false;

            var person = await _persons.GetPersonByIdIgnoringFilters(personId);
            if (person == null || person.ApplicationUserId != ownerId)
                return false;

            if (action == "reached-out")
            {
                await _interactions.AddAsync(new Interaction
                {
                    InteractionId = Guid.NewGuid(),
                    PersonId = personId,
                    TimeOfInteraction = DateTime.UtcNow,
                    InteractionType = ContactsManger.Core.Domain.Entities.EEnums.EnInteractionType.Email,
                    InteractionTitle = "Logged from weekly digest"
                });
            }

            await _digests.AddMetricAsync(new DigestMetric
            {
                DeliveryId = deliveryId,
                ApplicationUserId = ownerId,
                PersonId = personId,
                ActionTaken = action == "reached-out",
                ActionType = action
            });
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public static DateTime WeekStart(DateTime utcNow)
        {
            var date = utcNow.Date;
            int diff = ((int)date.DayOfWeek + 6) % 7;
            return date.AddDays(-diff);
        }

        private async Task RefreshStaleStatesAsync(Guid ownerId)
        {
            var affinities = (await _persons.ListAffinitiesAsync()) ?? new List<PersonAffinity>();
            var states = (await _states.ListForOwnerAsync(ownerId) ?? new List<RelationshipState>())
                .ToDictionary(s => s.PersonId);

            var needsRecompute = new List<Guid>();
            foreach (var affinity in affinities)
            {
                if (affinity == null)
                    continue;
                if (!states.TryGetValue(affinity.PersonId, out var state)
                    || (affinity.LastInteractionAtUtc.HasValue
                        && (state.EvidenceStatus == EvidenceStatus.NoHistory
                            || !state.LastContactAtUtc.HasValue
                            || affinity.LastInteractionAtUtc.Value > state.LastContactAtUtc.Value)))
                {
                    needsRecompute.Add(affinity.PersonId);
                }
            }

            if (needsRecompute.Count > 0)
                await _scoring.RecomputePairsAsync(needsRecompute);
        }
    }
}
