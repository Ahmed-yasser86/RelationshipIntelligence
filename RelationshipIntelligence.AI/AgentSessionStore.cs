using ServiceContracts;
using ServiceContracts.DTOs.AgentDTOs;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RelationshipIntelligence.AI
{
    public sealed class AgentSessionStore : IAgentSessionStore
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

        private readonly ConcurrentDictionary<string, (AgentSession Session, DateTime Expires)> _sessions = new();

        private static string Key(Guid ownerId, Guid sessionId) => $"{ownerId:N}:{sessionId:N}";

        public Task<AgentSession> GetOrCreateAsync(Guid? sessionId, Guid ownerId)
        {
            var now = DateTime.UtcNow;
            foreach (var key in _sessions.Keys)
            {
                if (_sessions.TryGetValue(key, out var entry) && entry.Expires <= now)
                    _sessions.TryRemove(key, out _);
            }

            if (sessionId != null)
            {
                var key = Key(ownerId, sessionId.Value);
                if (_sessions.TryGetValue(key, out var existing) && existing.Expires > now)
                {
                    existing.Session.UpdatedAtUtc = now;
                    _sessions[key] = (existing.Session, now.Add(Lifetime));
                    return Task.FromResult(existing.Session);
                }
            }

            var session = new AgentSession
            {
                SessionId = Guid.NewGuid(),
                UpdatedAtUtc = now
            };
            _sessions[Key(ownerId, session.SessionId)] = (session, now.Add(Lifetime));
            return Task.FromResult(session);
        }

        public Task SaveAsync(Guid ownerId, AgentSession session)
        {
            session.UpdatedAtUtc = DateTime.UtcNow;
            _sessions[Key(ownerId, session.SessionId)] = (session, DateTime.UtcNow.Add(Lifetime));
            return Task.CompletedTask;
        }
    }
}
