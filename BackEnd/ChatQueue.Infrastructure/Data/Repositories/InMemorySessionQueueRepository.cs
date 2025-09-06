using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;

namespace ChatQueue.Infrastructure.Data.Repositories
{
    public class InMemorySessionQueueRepository : ISessionQueueRepository
    {
        private readonly List<AssignedChatSession> _assignedQueue = [];

        public void Add(AssignedChatSession session)
        {
            if (session is null) throw new ArgumentNullException(nameof(session));
            if (session.Status != ChatSessionStatus.Assigned)
                throw new InvalidOperationException("Only Assigned sessions can be enqueued.");

            _assignedQueue.Add(session);
        }

        public int Count() => _assignedQueue.Count(a => a.Status != ChatSessionStatus.Inactive);

        public bool Inactive(Guid sessionId)
        {
            var isInactived = false;
            if (_assignedQueue.Exists(a => a.Id == sessionId))
            {
                for (int i = 0; i < _assignedQueue.Count; i++)
                {
                    if (_assignedQueue[i].Id == sessionId)
                    {
                        _assignedQueue[i] = _assignedQueue[i] with { Status = ChatSessionStatus.Inactive };
                        isInactived = true;
                        break;
                    }
                }
            }
            return isInactived;
        }

        public IReadOnlyList<AssignedChatSession> Snapshot() => _assignedQueue;

    }
}
