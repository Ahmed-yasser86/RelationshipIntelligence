using ContactsManger.Core.Domain.Entities;
using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class DigestServiceTests
    {
        private const string Secret = "test-secret-for-digest-signer";
        private readonly Guid _userA = Guid.NewGuid();
        private readonly Mock<IRelationshipScoringService> _scoringMock = new();
        private readonly Mock<PersonRepositryContract> _personsMock = new();
        private readonly Mock<InteractionRepositoryContract> _interactionsMock = new();
        private readonly Mock<DigestRepositoryContract> _digestsMock = new();
        private readonly Mock<RelationshipStateRepositoryContract> _statesMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();
        private readonly Mock<IEmailSender> _emailMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        private DigestService Service() => new(
            _scoringMock.Object,
            _personsMock.Object,
            _interactionsMock.Object,
            _digestsMock.Object,
            _statesMock.Object,
            _userMock.Object,
            _emailMock.Object,
            _unitOfWorkMock.Object,
            Secret,
            Mock.Of<ILogger<DigestService>>());

        private RelationshipHealthResponse Entry(Guid id, string name, double urgency, DateTime? last)
            => new() { PersonId = id, Name = name, UrgencyScore = urgency, LastContactAtUtc = last, Band = "AtRisk" };

        private void ArrangeOwner()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _digestsMock.Setup(d => d.GetPreferenceAsync(_userA))
                .ReturnsAsync(new DigestPreference { ApplicationUserId = _userA, Enabled = true, Threshold = 50, Count = 5 });
            _digestsMock.Setup(d => d.FindDeliveryAsync(_userA, It.IsAny<DateTime>()))
                .ReturnsAsync((DigestDelivery?)null);
        }

        [Fact]
        public async Task BuildAsync_AppliesThresholdSuppressionAndCount()
        {
            ArrangeOwner();
            var now = DateTime.UtcNow;
            var wanted = Entry(Guid.NewGuid(), "Hot", 90, now.AddDays(-30));
            var tooHealthy = Entry(Guid.NewGuid(), "Fine", 20, now.AddDays(-30));
            var justContacted = Entry(Guid.NewGuid(), "Fresh", 95, now.AddDays(-1));
            _scoringMock.Setup(s => s.GetQueueAsync(50))
                .ReturnsAsync(new List<RelationshipHealthResponse> { justContacted, wanted, tooHealthy });
            _interactionsMock.Setup(r => r.ListForPersonAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Interaction>());

            var payload = await Service().BuildAsync("https://app.test");

            payload.Entries.Should().ContainSingle();
            payload.Entries[0].Health.Name.Should().Be("Hot");
            payload.Entries[0].ActionUrl.Should().Contain("token=");
            payload.Entries[0].Suggestion.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task BuildAsync_FreshStates_SkipsRecompute()
        {
            ArrangeOwner();
            var personId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person>
            {
                new() { PersonId = personId, ApplicationUserId = _userA, Name = "P" }
            }.AsEnumerable());
            _statesMock.Setup(r => r.ListForOwnerAsync(_userA)).ReturnsAsync(new List<RelationshipState>
            {
                new() { PersonId = personId, ApplicationUserId = _userA, UpdatedAtUtc = DateTime.UtcNow }
            });
            _scoringMock.Setup(s => s.GetQueueAsync(50))
                .ReturnsAsync(new List<RelationshipHealthResponse>());

            await Service().BuildAsync("https://app.test");

            _scoringMock.Verify(s => s.RecomputeForCurrentUserAsync(), Times.Never);
        }

        [Fact]
        public async Task BuildAsync_StaleStates_Recomputes()
        {
            ArrangeOwner();
            var personId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetAllPersons()).ReturnsAsync(new List<Person>
            {
                new() { PersonId = personId, ApplicationUserId = _userA, Name = "P" }
            }.AsEnumerable());
            _statesMock.Setup(r => r.ListForOwnerAsync(_userA)).ReturnsAsync(new List<RelationshipState>
            {
                new() { PersonId = personId, ApplicationUserId = _userA, UpdatedAtUtc = DateTime.UtcNow.AddDays(-2) }
            });
            _scoringMock.Setup(s => s.GetQueueAsync(50))
                .ReturnsAsync(new List<RelationshipHealthResponse>());

            await Service().BuildAsync("https://app.test");

            _scoringMock.Verify(s => s.RecomputeForCurrentUserAsync(), Times.Once);
        }

        [Fact]
        public async Task BuildAsync_SecondCallSameWeek_ReusesDelivery()
        {
            ArrangeOwner();
            var existing = new DigestDelivery { DigestDeliveryId = Guid.NewGuid(), ApplicationUserId = _userA };
            _digestsMock.Setup(d => d.FindDeliveryAsync(_userA, It.IsAny<DateTime>()))
                .ReturnsAsync(existing);
            _scoringMock.Setup(s => s.GetQueueAsync(50))
                .ReturnsAsync(new List<RelationshipHealthResponse>());

            var first = await Service().BuildAsync("https://app.test");
            var second = await Service().BuildAsync("https://app.test");

            first.DeliveryId.Should().Be(existing.DigestDeliveryId);
            second.DeliveryId.Should().Be(existing.DigestDeliveryId);
            _digestsMock.Verify(d => d.AddDeliveryAsync(It.IsAny<DigestDelivery>()), Times.Never);
        }

        [Fact]
        public async Task BuildAsync_DisabledPreference_ReturnsEmpty()
        {
            _userMock.Setup(u => u.UserId).Returns(_userA);
            _digestsMock.Setup(d => d.GetPreferenceAsync(_userA))
                .ReturnsAsync(new DigestPreference { ApplicationUserId = _userA, Enabled = false });

            var payload = await Service().BuildAsync("https://app.test");

            payload.Entries.Should().BeEmpty();
            _scoringMock.Verify(s => s.RecomputeForCurrentUserAsync(), Times.Never);
        }

        [Fact]
        public async Task DeliverAsync_NoEntries_SendsNothing()
        {
            ArrangeOwner();
            _scoringMock.Setup(s => s.GetQueueAsync(50))
                .ReturnsAsync(new List<RelationshipHealthResponse>());

            var result = await Service().DeliverAsync("https://app.test", "user@test.com");

            result.Should().BeNull();
            _emailMock.Verify(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleActionAsync_ValidToken_CreatesDigestInteraction()
        {
            var personId = Guid.NewGuid();
            var deliveryId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetPersonByIdIgnoringFilters(personId))
                .ReturnsAsync(new Person { PersonId = personId, ApplicationUserId = _userA, Name = "P" });
            Interaction? saved = null;
            _interactionsMock.Setup(r => r.AddAsync(It.IsAny<Interaction>()))
                .Callback<Interaction>(i => saved = i)
                .ReturnsAsync((Interaction i) => i);

            string token = DigestActionSigner.Create(Secret, _userA, personId, deliveryId);
            var ok = await Service().HandleActionAsync(token, "reached-out");

            ok.Should().BeTrue();
            saved.Should().NotBeNull();
            saved!.PersonId.Should().Be(personId);
        }

        [Fact]
        public async Task HandleActionAsync_TamperedToken_ReturnsFalse()
        {
            var ok = await Service().HandleActionAsync("not-valid-base64!!", "reached-out");
            ok.Should().BeFalse();
            _interactionsMock.Verify(r => r.AddAsync(It.IsAny<Interaction>()), Times.Never);
        }

        [Fact]
        public async Task HandleActionAsync_ExpiredToken_ReturnsFalse()
        {
            var token = DigestActionSigner.Create(Secret, _userA, Guid.NewGuid(), Guid.NewGuid(),
                DateTime.UtcNow.AddDays(-8), TimeSpan.FromDays(7));

            var ok = await Service().HandleActionAsync(token, "reached-out");

            ok.Should().BeFalse();
        }

        [Fact]
        public async Task HandleActionAsync_ForeignPerson_ReturnsFalse()
        {
            var personId = Guid.NewGuid();
            _personsMock.Setup(r => r.GetPersonByIdIgnoringFilters(personId))
                .ReturnsAsync(new Person { PersonId = personId, ApplicationUserId = Guid.NewGuid(), Name = "Other" });

            string token = DigestActionSigner.Create(Secret, _userA, personId, Guid.NewGuid());
            var ok = await Service().HandleActionAsync(token, "reached-out");

            ok.Should().BeFalse();
        }

        [Fact]
        public void Signer_RoundTrip()
        {
            var owner = Guid.NewGuid();
            var person = Guid.NewGuid();
            var delivery = Guid.NewGuid();

            string token = DigestActionSigner.Create(Secret, owner, person, delivery);
            DigestActionSigner.TryVerify("wrong-secret", token, out _, out _, out _)
                .Should().BeFalse();

            bool ok = DigestActionSigner.TryVerify(Secret, token, out Guid o, out Guid p, out Guid d);
            ok.Should().BeTrue();
            o.Should().Be(owner);
            p.Should().Be(person);
            d.Should().Be(delivery);
        }
    }
}
