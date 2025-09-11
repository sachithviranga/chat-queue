using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Domain.Entities;

namespace ChatQueue.Infrastructure.Data.Repositories
{
    public class InMemoryPollingRepository : IPollingRepository
    {
        private readonly Dictionary<Guid, PollSession> _pollQueue = [];

        public Task RegisterPollAsync(Guid sessionId, DateTime updateDate, CancellationToken ct = default)
        {
            if (!_pollQueue.TryGetValue(sessionId, out _))
            {
                _pollQueue[sessionId] = new PollSession(
                    Id: sessionId,
                    UpdateAt: updateDate,
                    Count: 0
                );
            }
            return Task.CompletedTask;
        }

        public Task UpdatePollAsync(Guid sessionId, DateTime updateDate, CancellationToken ct = default)
        {
            if (_pollQueue.TryGetValue(sessionId, out var session))
            {
                _pollQueue[sessionId] = session with
                {
                    Count = session.Count + 1,
                    UpdateAt = updateDate
                };
            }
            else
            {
                _pollQueue[sessionId] = new PollSession(
                    Id: sessionId,
                    UpdateAt: updateDate,
                    Count: 1
                );
            }
            return Task.CompletedTask;
        }

        public Task<DateTime?> GetLasteUpdateDateTimeAsync(Guid sessionId, CancellationToken ct = default)
        {
            DateTime? lasteUpdateDateTime = null;

            if (_pollQueue.TryGetValue(sessionId, out var session))
            {
                lasteUpdateDateTime = session.UpdateAt;
            }
            return Task.FromResult(lasteUpdateDateTime);
        }

        public Task<bool> IsInactiveAsync(Guid sessionId, int Count, CancellationToken ct = default)
        {
            bool result = true;
            if (_pollQueue.TryGetValue(sessionId, out var session))
            {
                result = session.Count < Count;
            }
            return Task.FromResult(result);
        }
    }
}
