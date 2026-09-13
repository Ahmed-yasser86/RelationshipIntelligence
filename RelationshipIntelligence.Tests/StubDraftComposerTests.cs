using Entities;
using FluentAssertions;
using Moq;
using ServiceContracts;
using ServiceContracts.DTOs.CopilotDTOs;
using RelationshipIntelligence.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class StubDraftComposerTests
    {
        private readonly StubCopilotService _stub;

        public StubDraftComposerTests()
        {
            _stub = new StubCopilotService(
                Mock.Of<IRelationshipScoringService>(),
                Mock.Of<IInteractionService>(),
                Mock.Of<IRelationshipMemoryService>(),
                Mock.Of<IEventService>(),
                Mock.Of<IPersonGetterService>());
        }

        private static PersonDraftContext Context(
            string name = "Aisha Bello",
            List<string>? interactions = null,
            List<string>? memory = null,
            List<string>? events = null,
            List<string>? commitments = null) => new()
            {
                PersonId = Guid.NewGuid(),
                Name = name,
                Band = "AtRisk",
                UrgencyScore = 74,
                CadenceLine = "Band AtRisk, urgency 74. Last contact 2026-08-01 vs ~30d rhythm.",
                RecentInteractions = interactions ?? new(),
                MemoryHighlights = memory ?? new(),
                UpcomingEvents = events ?? new(),
                OpenCommitments = commitments ?? new()
            };

        private Task<DraftCommunicationResult> DraftAsync(PersonDraftContext person,
            OutreachChannel channel = OutreachChannel.Email) =>
            _stub.DraftCommunicationAsync(new DraftCommunicationRequest
            {
                Person = person,
                Kind = DraftKind.Message,
                Channel = channel,
                Intent = "Reconnect"
            });

        private static void ShouldNotLeakInternalRepresentation(string body)
        {
            body.Should().NotContain("[");
            body.Should().NotContain("urgency");
            body.Should().NotContain("rhythm");
            body.Should().NotContain("Band");
            body.Should().NotMatchRegex(@"\d{4}-\d{2}-\d{2}");
        }

        [Fact]
        public async Task Draft_CommitmentFirst_FollowsUpInProseWithoutLabels()
        {
            var result = await DraftAsync(Context(commitments: new() { "Send the Phase-2 proposal" }));

            result.Body.Should().Contain("follow up on Send the Phase-2 proposal");
            result.Subject.Should().Contain("Following up");
            ShouldNotLeakInternalRepresentation(result.Body);
        }

        [Fact]
        public async Task Draft_EventFirst_ChecksInWithoutRawCountdown()
        {
            var result = await DraftAsync(Context(events: new() { "Meetup #43 in 5d" }));

            result.Body.Should().Contain("Meetup #43");
            result.Body.Should().Contain("in a few days");
            result.Body.Should().NotContain("in 5d");
            result.Subject.Should().Contain("Meetup #43");
            ShouldNotLeakInternalRepresentation(result.Body);
        }

        [Fact]
        public async Task Draft_RecentThread_ContinuesItWithoutTypeTags()
        {
            var result = await DraftAsync(Context(
                interactions: new() { "2026-08-20 [Call] Career advice: staff vs manager track" }));

            result.Body.Should().Contain("thinking about our career advice");
            result.Body.Should().NotContain("[Call]");
            ShouldNotLeakInternalRepresentation(result.Body);
        }

        [Fact]
        public async Task Draft_IntentMemory_RevisitsItWithoutBrackets()
        {
            var result = await DraftAsync(Context(
                memory: new() { "[Intent/User] Ask for an intro to the Riyadh fintech scene" }));

            result.Body.Should().Contain("One thing I wanted to raise: Ask for an intro to the Riyadh fintech scene");
            result.Body.Should().NotContain("[Intent/User]");
            ShouldNotLeakInternalRepresentation(result.Body);
        }

        [Fact]
        public async Task Draft_RecordedIntent_OutranksGenericThreadContinuity()
        {
            var result = await DraftAsync(Context(
                interactions: new() { "2026-08-20 [Call] Career advice: staff vs manager track" },
                memory: new() { "[Intent/User] Ask for an intro to the Riyadh fintech scene" }));

            result.Body.Should().Contain("One thing I wanted to raise: Ask for an intro to the Riyadh fintech scene");
            result.Body.Should().NotContain("pick up the thread");
        }

        [Fact]
        public async Task Draft_ThinContext_StaysConservativeAndFlagged()
        {
            var result = await DraftAsync(Context());

            result.Body.Should().Contain("been a while");
            result.LimitedContext.Should().BeTrue();
            result.Body.Should().NotContain("urgency");
        }

        [Fact]
        public async Task Draft_DifferentContexts_ProduceMateriallyDifferentBodies()
        {
            var commitment = await DraftAsync(Context(commitments: new() { "Send the Phase-2 proposal" }));
            var lunch = await DraftAsync(Context(
                interactions: new() { "2026-08-20 [Meeting] Lunch" }));

            commitment.Body.Should().NotBe(lunch.Body);
            lunch.Body.Should().Contain("lunch");
            lunch.Body.Should().NotContain("proposal");
        }

        [Fact]
        public async Task Draft_TextChannel_StaysShortAndGrounded()
        {
            var result = await DraftAsync(
                Context(commitments: new() { "Send the Phase-2 proposal" }),
                OutreachChannel.Text);

            result.Body.Should().StartWith("Hi Aisha Bello — ");
            result.Body.Should().Contain("proposal");
            result.Body.Split(". ", StringSplitOptions.None).Should().HaveCountLessThanOrEqualTo(2);
        }
    }
}
