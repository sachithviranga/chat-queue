using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Domain.Entities;
using System.Collections.Concurrent;

namespace ChatQueue.Infrastructure.Data.Repositories
{
    public sealed class InMemoryAssignmentRepository : IAssignmentRepository
    {
        private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, byte>> _agentToSessions = new();
        private readonly ConcurrentDictionary<Guid, Guid> _sessionToAgent = new();

        public Task<bool> TryAssignAsync(Guid agentId, Guid sessionId, CancellationToken ct = default)
        {
            if (!_sessionToAgent.TryAdd(sessionId, agentId))
                return Task.FromResult(false);

            var bucket = _agentToSessions.GetOrAdd(agentId, _ => new ConcurrentDictionary<Guid, byte>());
            bucket.TryAdd(sessionId, 0);
            return Task.FromResult(true);
        }

        public Task<bool> ReleaseAsync(Guid sessionId, CancellationToken ct = default)
        {
            if (_sessionToAgent.TryRemove(sessionId, out var agentId))
            {
                if (_agentToSessions.TryGetValue(agentId, out var sessions))
                    sessions.TryRemove(sessionId, out _);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<int> GetLoadAsync(Guid agentId, CancellationToken ct = default)
            => Task.FromResult(_agentToSessions.TryGetValue(agentId, out var s) ? s.Count : 0);

        public Task<IReadOnlyCollection<Guid>> GetAssignedSessionsAsync(Guid agentId, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyCollection<Guid>)
                (_agentToSessions.TryGetValue(agentId, out var s) ? s.Keys.ToArray() : Array.Empty<Guid>()));

        public Task<Guid?> GetAssignedAgentAsync(Guid sessionId, CancellationToken ct = default)
            => Task.FromResult(_sessionToAgent.TryGetValue(sessionId, out var a) ? (Guid?)a : null);
    }
}
