using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Domain.Entities;

namespace ChatQueue.Infrastructure.Data.Repositories
{
    public class InMemoryPollingRepository : IPollingRepository
    {
        private readonly Dictionary<Guid, PollSession> _pollQueue = [];

        public void RegisterPoll(Guid sessionId, DateTime updateDate)
        {
            if (!_pollQueue.TryGetValue(sessionId, out _))
            {
                _pollQueue[sessionId] = new PollSession(
                    Id: sessionId,
                    UpdateAt: updateDate,
                    Count: 0
                );
            }
        }

        public void UpdatePoll(Guid sessionId, DateTime updateDate)
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
        }

        public DateTime? GetLasteUpdateDateTime(Guid sessionId)
        {
            if (_pollQueue.TryGetValue(sessionId, out var session))
            {
                return session.UpdateAt;
            }
            return null;
        }

        public bool IsInactive(Guid sessionId, int Count)
        {
            if (_pollQueue.TryGetValue(sessionId, out var session))
            {
                return session.Count < Count;
            }
            else
            {
                return true;
            }
        }
    }
}
