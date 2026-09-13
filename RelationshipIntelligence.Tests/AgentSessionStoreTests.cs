using FluentAssertions;
using RelationshipIntelligence.AI;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class AgentSessionStoreTests
    {
        [Fact]
        public async Task GetOrCreate_NewSession_ReturnsEmptyWorkingState()
        {
            var store = new AgentSessionStore();
            var owner = Guid.NewGuid();

            var session = await store.GetOrCreateAsync(null, owner);

            session.SessionId.Should().NotBeEmpty();
            session.SelectedPersonIds.Should().BeEmpty();
            session.CurrentTask.Should().BeNull();
        }

        [Fact]
        public async Task GetOrCreate_SameId_ReturnsSameSession()
        {
            var store = new AgentSessionStore();
            var owner = Guid.NewGuid();

            var first = await store.GetOrCreateAsync(null, owner);
            first.CurrentTask = "outreach";
            await store.SaveAsync(owner, first);

            var second = await store.GetOrCreateAsync(first.SessionId, owner);

            second.SessionId.Should().Be(first.SessionId);
            second.CurrentTask.Should().Be("outreach");
        }

        [Fact]
        public async Task GetOrCreate_SameSessionIdDifferentOwner_IsIsolated()
        {
            var store = new AgentSessionStore();
            var ownerA = Guid.NewGuid();
            var ownerB = Guid.NewGuid();

            var sessionA = await store.GetOrCreateAsync(null, ownerA);
            sessionA.CurrentTask = "outreach";
            await store.SaveAsync(ownerA, sessionA);

            var sessionB = await store.GetOrCreateAsync(sessionA.SessionId, ownerB);

            sessionB.CurrentTask.Should().BeNull();
        }
    }
}
